using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProjectL.Tool.Editor
{
    public sealed class ShaderComposerWindow : EditorWindow
    {
        [SerializeField] private ShaderComposerRecipe recipe = new ShaderComposerRecipe();
        [SerializeField] private bool createMaterial = true;
        [SerializeField] private bool showPasses;
        [SerializeField] private bool showCode;
        [FormerlySerializedAs("previewObject")]
        [SerializeField] private UnityEngine.Object previewFbx;
        [SerializeField] private ShaderComposerPreviewSettings previewSettings = new ShaderComposerPreviewSettings();
        private Vector2 _previewSettingsScroll;
        private string _previewInputError;
        private Vector2 _scroll;
        private Vector2 _codeScroll;
        private string _source;
        private string _error;
        private string _status;
        private string _structure;
        private bool _dirty;
        private double _nextPreview;
        private double _nextRepaint;
        private Shader _previewShader;
        private Material _previewMaterial;
        private ShaderComposerObjectPreview _objectPreview;

        internal ShaderComposerRecipe Recipe => recipe;
        internal event Action RecipeChanged;

        internal void NotifyRecipeChanged()
        {
            MarkDirty();
            Repaint();
        }

        [MenuItem("ProjectL/Shader Composer")]
        public static void Open()
        {
            var window = GetWindow<ShaderComposerWindow>();
            window.titleContent = new GUIContent("Shader Composer");
            window.minSize = new Vector2(1160, 680);
            window.Show();
        }

        private void OnEnable()
        {
            if (recipe == null) recipe = new ShaderComposerRecipe();
            if (previewSettings == null) previewSettings = new ShaderComposerPreviewSettings();
            minSize = new Vector2(1160, 680);
            ShaderComposerCatalog.Normalize(recipe);
            if (previewFbx != null)
            {
                if (ShaderComposerObjectPreview.TryGetFbxModel(previewFbx, out GameObject model)) previewFbx = model;
                else previewFbx = null;
            }
            EditorApplication.update += UpdatePreview;
            Undo.undoRedoPerformed += OnUndo;
            MarkDirty();
        }

        private void OnDisable()
        {
            EditorApplication.update -= UpdatePreview;
            Undo.undoRedoPerformed -= OnUndo;
            ReleasePreview();
            _objectPreview?.Dispose();
            _objectPreview = null;
        }

        private void OnUndo()
        {
            MarkDirty();
            Repaint();
        }

        private void MarkDirty()
        {
            _dirty = true;
            _nextPreview = EditorApplication.timeSinceStartup + 0.25;
            RecipeChanged?.Invoke();
        }

        internal void SetRecipe(ShaderComposerRecipe input)
        {
            ShaderComposerGenerator.Validate(input);
            recipe = input;
            _structure = null;
            MarkDirty();
        }

        private static int ModuleIndex(ShaderComposerModule[] modules, string key)
        {
            return Array.FindIndex(modules, module => module.Key == key);
        }

        private void OnGUI()
        {
            Undo.RecordObject(this, "셰이더 조립 변경");
            EditorGUI.BeginChangeCheck();
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("새 조립", EditorStyles.toolbarButton))
                {
                    recipe = new ShaderComposerRecipe();
                    ShaderComposerCatalog.Normalize(recipe);
                    _structure = null;
                    MarkDirty();
                }
                if (GUILayout.Button("조립한 Shader 열기", EditorStyles.toolbarButton)) ReadShader();
                if (GUILayout.Button("프로퍼티 창", EditorStyles.toolbarButton)) ShaderComposerPropertiesWindow.Open(this);
                if (GUILayout.Button("사용 설명서", EditorStyles.toolbarButton)) ShaderComposerHelpWindow.Open();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("미리보기 새로고침", EditorStyles.toolbarButton))
                {
                    _structure = null;
                    MarkDirty();
                }
            }
            recipe.shaderName = EditorGUILayout.TextField("Shader 이름", recipe.shaderName);
            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(position.width * 0.34f)))
                {
                    _scroll = EditorGUILayout.BeginScrollView(_scroll);
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField("1. 라이트 모델", EditorStyles.boldLabel);
                        for (int i = 0; i < recipe.lighting.Count; i++)
                        {
                            if (DrawLightingLayer(i)) break;
                        }
                        using (new EditorGUI.DisabledScope(recipe.lighting.Count >= ShaderComposerCatalog.MaximumLightingLayers))
                        {
                            if (GUILayout.Button("+ 라이트 모델 추가"))
                            {
                                recipe.lighting.Add(new ShaderComposerLightingLayer { id = recipe.nextLightingId++ });
                                ShaderComposerCatalog.Normalize(recipe);
                                MarkDirty();
                            }
                        }
                        EditorGUILayout.HelpBox("활성 모델의 가중 평균으로 혼합합니다. 가중치 1:1은 반씩 혼합하며 모두 0이면 검정입니다.", MessageType.Info);
                    }
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField("2. 기본 표면", EditorStyles.boldLabel);
                        DrawParameters(ShaderComposerCatalog.Base, recipe.baseValues);
                    }
                    EditorGUILayout.LabelField("3. 효과와 마스크", EditorStyles.boldLabel);
                    for (int i = 0; i < recipe.layers.Count; i++)
                    {
                        if (DrawLayer(i)) break;
                    }
                    using (new EditorGUI.DisabledScope(recipe.layers.Count >= ShaderComposerCatalog.MaximumLayers))
                    {
                        if (GUILayout.Button("+ 효과 추가", GUILayout.Height(28)))
                        {
                            recipe.layers.Add(new ShaderComposerLayer { id = recipe.nextLayerId++ });
                            ShaderComposerCatalog.Normalize(recipe);
                            MarkDirty();
                        }
                    }
                    showPasses = EditorGUILayout.Foldout(showPasses, "추가 출력", true);
                    if (showPasses)
                    {
                        recipe.shadowPass = EditorGUILayout.Toggle("그림자", recipe.shadowPass);
                        recipe.depthPass = EditorGUILayout.Toggle("깊이", recipe.depthPass);
                        recipe.normalsPass = EditorGUILayout.Toggle("카메라 노멀", recipe.normalsPass);
                        recipe.outlineNormalsPass = EditorGUILayout.Toggle("아웃라인용 노멀", recipe.outlineNormalsPass);
                        recipe.metaPass = EditorGUILayout.Toggle("GI 베이크", recipe.metaPass);
                        recipe.disableFog = !EditorGUILayout.Toggle("안개 적용", !recipe.disableFog);
                        recipe.unityPassNames = EditorGUILayout.Toggle("Unity 표준 Pass 이름", recipe.unityPassNames);
                    }
                    EditorGUILayout.EndScrollView();
                }
                bool recipeChanged = GUI.changed;
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField("미리보기", EditorStyles.boldLabel);
                    UnityEngine.Object selectedFbx = EditorGUILayout.ObjectField("FBX 모델", previewFbx, typeof(UnityEngine.Object), false);
                    if (selectedFbx != previewFbx) SetPreviewFbx(selectedFbx);
                    bool resetObject = false;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("FBX 파일 선택")) SelectPreviewFbxFile();
                        bool selectedIsFbx = ShaderComposerObjectPreview.TryGetFbxModel(Selection.activeObject, out GameObject selectedModel);
                        using (new EditorGUI.DisabledScope(!selectedIsFbx))
                        {
                            if (GUILayout.Button("선택 FBX 사용")) SetPreviewFbx(selectedModel);
                        }
                        if (GUILayout.Button("보기 초기화")) resetObject = true;
                    }
                    if (!string.IsNullOrEmpty(_previewInputError)) EditorGUILayout.HelpBox(_previewInputError, MessageType.Warning);
                    if (_objectPreview == null) _objectPreview = new ShaderComposerObjectPreview();
                    _objectPreview.SetSource(previewFbx, resetObject);
                    float previewHeight = Mathf.Max(120, position.height - 310 - (showCode ? 184 : 0));
                    Rect rect = GUILayoutUtility.GetRect(240, previewHeight, GUILayout.ExpandWidth(true));
                    if (_objectPreview.Draw(rect, _previewMaterial, previewSettings)) Repaint();
                    if (!string.IsNullOrEmpty(_error)) EditorGUILayout.HelpBox(_error, MessageType.Error);
                    else if (!string.IsNullOrEmpty(_status)) EditorGUILayout.HelpBox(_status, MessageType.Info);
                    createMaterial = EditorGUILayout.ToggleLeft("머티리얼도 함께 만들기", createMaterial);
                    using (new EditorGUI.DisabledScope(_previewShader == null || !string.IsNullOrEmpty(_error) || _dirty))
                    {
                        if (GUILayout.Button("Shader 파일로 내보내기", GUILayout.Height(36))) ExportShader();
                    }
                }
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(300)))
                {
                    float labelWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 110;
                    try
                    {
                        if (ShaderComposerPreviewSettingsGUI.Draw(previewSettings, ref _previewSettingsScroll)) Repaint();
                    }
                    finally { EditorGUIUtility.labelWidth = labelWidth; }
                }
                GUI.changed = recipeChanged;
            }
            showCode = EditorGUILayout.Foldout(showCode, "생성 코드 보기", true);
            if (showCode)
            {
                _codeScroll = EditorGUILayout.BeginScrollView(_codeScroll, GUILayout.Height(180));
                EditorGUILayout.SelectableLabel(_source ?? string.Empty, EditorStyles.textArea,
                    GUILayout.Height(Mathf.Max(180, (_source ?? string.Empty).Count(c => c == '\n') * 14)));
                EditorGUILayout.EndScrollView();
            }
            if (EditorGUI.EndChangeCheck()) MarkDirty();
        }

        private void SetPreviewFbx(UnityEngine.Object candidate)
        {
            if (candidate == null)
            {
                previewFbx = null;
                _previewInputError = null;
                return;
            }
            if (ShaderComposerObjectPreview.TryGetFbxModel(candidate, out GameObject model))
            {
                previewFbx = model;
                _previewInputError = null;
            }
            else _previewInputError = "Project에 임포트된 .fbx 파일을 넣으세요. 씬 오브젝트와 일반 프리팹은 사용할 수 없습니다.";
        }

        private void SelectPreviewFbxFile()
        {
            string absolute = EditorUtility.OpenFilePanel("미리보기 FBX 선택", Application.dataPath, "fbx");
            if (string.IsNullOrEmpty(absolute)) return;
            string path = FileUtil.GetProjectRelativePath(absolute.Replace('\\', '/'));
            if (!path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                _previewInputError = "외부 FBX는 먼저 Unity Project의 Assets 폴더에 임포트한 뒤 선택하세요.";
                return;
            }
            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null)
            {
                _previewInputError = "이 FBX는 아직 임포트되지 않았습니다. Project에서 임포트가 끝난 뒤 선택하세요.";
                return;
            }
            SetPreviewFbx(asset);
        }

        private bool DrawLightingLayer(int index)
        {
            ShaderComposerLightingLayer light = recipe.lighting[index];
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    light.enabled = EditorGUILayout.Toggle(light.enabled, GUILayout.Width(18));
                    light.expanded = EditorGUILayout.Foldout(light.expanded, "모델 " + (index + 1), true);
                    using (new EditorGUI.DisabledScope(recipe.lighting.Count <= 1))
                    {
                        if (GUILayout.Button("삭제", GUILayout.Width(42)))
                        {
                            recipe.lighting.RemoveAt(index);
                            MarkDirty();
                            return true;
                        }
                    }
                }
                if (!light.expanded) return false;
                using (new EditorGUI.DisabledScope(!light.enabled))
                {
                    int selected = EditorGUILayout.Popup("모델", ModuleIndex(ShaderComposerCatalog.Models, light.model), ShaderComposerCatalog.Models.Select(module => module.Label).ToArray());
                    string model = ShaderComposerCatalog.Models[selected].Key;
                    if (light.model != model)
                    {
                        light.model = model;
                        ShaderComposerCatalog.Normalize(recipe);
                    }
                    DrawParameters(ShaderComposerCatalog.LightingBlend, light.blendValues);
                    DrawParameters(ShaderComposerCatalog.Find(ShaderComposerCatalog.Models, light.model), light.values);
                }
            }
            return false;
        }

        private bool DrawLayer(int index)
        {
            ShaderComposerLayer layer = recipe.layers[index];
            bool changedList = false;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    layer.enabled = EditorGUILayout.Toggle(layer.enabled, GUILayout.Width(18));
                    layer.expanded = EditorGUILayout.Foldout(layer.expanded, "효과 " + (index + 1), true);
                    using (new EditorGUI.DisabledScope(index == 0))
                    {
                        if (GUILayout.Button("↑", GUILayout.Width(25)))
                        {
                            recipe.layers.RemoveAt(index); recipe.layers.Insert(index - 1, layer); changedList = true;
                        }
                    }
                    using (new EditorGUI.DisabledScope(index == recipe.layers.Count - 1))
                    {
                        if (GUILayout.Button("↓", GUILayout.Width(25)))
                        {
                            recipe.layers.RemoveAt(index); recipe.layers.Insert(index + 1, layer); changedList = true;
                        }
                    }
                    if (GUILayout.Button("삭제", GUILayout.Width(42)))
                    {
                        recipe.layers.Remove(layer); changedList = true;
                    }
                }
                if (layer.expanded && !changedList)
                {
                    using (new EditorGUI.DisabledScope(!layer.enabled))
                    {
                        int effect = EditorGUILayout.Popup("효과", ModuleIndex(ShaderComposerCatalog.Effects, layer.effect), ShaderComposerCatalog.Effects.Select(module => module.Label).ToArray());
                        int mask = EditorGUILayout.Popup("마스크", ModuleIndex(ShaderComposerCatalog.Masks, layer.mask), ShaderComposerCatalog.Masks.Select(module => module.Label).ToArray());
                        if (layer.effect != ShaderComposerCatalog.Effects[effect].Key || layer.mask != ShaderComposerCatalog.Masks[mask].Key)
                        {
                            layer.effect = ShaderComposerCatalog.Effects[effect].Key;
                            layer.mask = ShaderComposerCatalog.Masks[mask].Key;
                            ShaderComposerCatalog.Normalize(recipe);
                        }
                        EditorGUILayout.Space(3);
                        DrawParameters(ShaderComposerCatalog.Find(ShaderComposerCatalog.Effects, layer.effect), layer.effectValues);
                        if (layer.mask != "none")
                        {
                            EditorGUILayout.LabelField("마스크 설정", EditorStyles.miniBoldLabel);
                            DrawParameters(ShaderComposerCatalog.Find(ShaderComposerCatalog.Masks, layer.mask), layer.maskValues);
                            if (layer.mask == "light")
                                EditorGUILayout.HelpBox("메인광·추가 조명의 방향, 밝기, 거리와 그림자를 반영합니다. 환경광·베이크 GI는 제외하며 실시간 효과는 GI에 굽지 않습니다. 디졸브에는 사용할 수 없습니다.", MessageType.Info);
                        }
                    }
                }
            }
            if (changedList) MarkDirty();
            return changedList;
        }

        private void DrawParameters(ShaderComposerModule module, System.Collections.Generic.List<ShaderComposerValue> values)
        {
            foreach (ShaderComposerParameter parameter in module.Parameters)
            {
                ShaderComposerValue value = values.Find(item => item.key == parameter.Key);
                var candidates = recipe.properties.Where(property => ShaderComposerProperties.Compatible(parameter.Kind, property.kind)).ToList();
                if (candidates.Count > 0 || !string.IsNullOrEmpty(value.referenceId))
                {
                    var choices = candidates.Select(property => property.label + " (" + property.name + ")").ToList();
                    choices.Insert(0, "직접 값");
                    int current = string.IsNullOrEmpty(value.referenceId) ? 0 : candidates.FindIndex(property => property.id == value.referenceId) + 1;
                    if (!string.IsNullOrEmpty(value.referenceId) && current == 0)
                    {
                        current = choices.Count;
                        choices.Add("연결 없음 / 타입 불일치");
                    }
                    int selected = EditorGUILayout.Popup(parameter.Label + " 입력", current, choices.ToArray());
                    if (selected != current)
                        value.referenceId = selected == 0 ? null : candidates[selected - 1].id;
                }
                if (string.IsNullOrEmpty(value.referenceId)) DrawValue(parameter, value, parameter.Label);
            }
        }

        internal static void DrawValue(ShaderComposerParameter parameter, ShaderComposerValue value, string label)
        {
            switch (parameter.Kind)
            {
                case ShaderComposerParameterKind.Color:
                    value.color = EditorGUILayout.ColorField(new GUIContent(label), value.color, true, false, parameter.Hdr); break;
                case ShaderComposerParameterKind.Texture:
                    Texture2D current = ShaderComposerExport.ResolveTexture(value.textureGuid);
                    Texture2D texture = (Texture2D)EditorGUILayout.ObjectField(label, current, typeof(Texture2D), false);
                    if (texture != current) value.textureGuid = texture == null ? string.Empty : AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(texture));
                    break;
                case ShaderComposerParameterKind.Range:
                    value.number = parameter.Integer
                        ? EditorGUILayout.IntSlider(label, Mathf.RoundToInt(value.number), Mathf.RoundToInt(parameter.Minimum), Mathf.RoundToInt(parameter.Maximum))
                        : EditorGUILayout.Slider(label, value.number, parameter.Minimum, parameter.Maximum);
                    break;
                case ShaderComposerParameterKind.Toggle:
                    value.number = EditorGUILayout.Toggle(label, value.number >= 0.5f) ? 1 : 0; break;
                case ShaderComposerParameterKind.Choice:
                    value.number = EditorGUILayout.Popup(label, Mathf.RoundToInt(value.number), parameter.Choices); break;
                case ShaderComposerParameterKind.Vector2:
                    Vector2 pair = EditorGUILayout.Vector2Field(label, new Vector2(value.vector.x, value.vector.y));
                    value.vector = new Vector4(pair.x, pair.y, 0, 0); break;
                case ShaderComposerParameterKind.Vector4:
                    value.vector = EditorGUILayout.Vector4Field(label, value.vector); break;
                case ShaderComposerParameterKind.UV:
                    Vector2 scale = EditorGUILayout.Vector2Field("타일링", new Vector2(value.vector.x, value.vector.y));
                    Vector2 offset = EditorGUILayout.Vector2Field("이동", new Vector2(value.vector.z, value.vector.w));
                    value.vector = new Vector4(scale.x, scale.y, offset.x, offset.y); break;
                default:
                    value.number = EditorGUILayout.FloatField(label, value.number); break;
            }
        }

        private string StructureKey()
        {
            return string.Join("|", recipe.lighting.Where(light => light.enabled).Select(light => light.id + ":" + light.model)) + ":" +
                recipe.shadowPass + recipe.depthPass + recipe.normalsPass + recipe.outlineNormalsPass + recipe.metaPass +
                recipe.unityPassNames + recipe.disableFog + ":" +
                string.Join("|", recipe.layers.Where(layer => layer.enabled).Select(layer => layer.id + ":" + layer.effect + ":" + layer.mask)) + ":" +
                string.Join("|", recipe.properties.Select(property => property.id + ":" + property.name + ":" + property.label + ":" +
                    property.kind + ":" + property.minimum + ":" + property.maximum + ":" + property.hdr)) + ":" +
                string.Join("|", ShaderComposerProperties.Inputs(recipe).Select(value => value.referenceId));
        }

        private void UpdatePreview()
        {
            if (_previewMaterial != null && EditorApplication.timeSinceStartup >= _nextRepaint)
            {
                _nextRepaint = EditorApplication.timeSinceStartup + 0.1;
                Repaint();
            }
            if (!_dirty || EditorApplication.timeSinceStartup < _nextPreview || EditorApplication.isCompiling ||
                EditorApplication.isUpdating || GUIUtility.hotControl != 0) return;
            _dirty = false;
            bool async = ShaderUtil.allowAsyncCompilation;
            try
            {
                ShaderComposerGenerator.Validate(recipe);
                _source = ShaderComposerGenerator.Generate(recipe);
                string key = StructureKey();
                if (_previewShader == null || key != _structure)
                {
                    ReleasePreview();
                    ShaderUtil.allowAsyncCompilation = false;
                    MethodInfo create = typeof(ShaderUtil).GetMethod("CreateShaderAsset", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                        null, new[] { typeof(string), typeof(bool) }, null);
                    if (create == null) throw new InvalidOperationException("현재 Unity에서 미리보기 Shader 생성 API를 찾을 수 없다.");
                    string source = ShaderComposerGenerator.Generate(recipe, "Hidden/ProjectL/ShaderComposerPreview_" + GetInstanceID());
                    _previewShader = (Shader)create.Invoke(null, new object[] { source, false });
                    _previewShader.hideFlags = HideFlags.HideAndDontSave;
                    _previewMaterial = new Material(_previewShader) { hideFlags = HideFlags.HideAndDontSave };
                    ShaderComposerExport.ApplyValues(_previewMaterial, recipe);
                    for (int pass = 0; pass < _previewMaterial.passCount; pass++) ShaderUtil.CompilePass(_previewMaterial, pass, true);
                    var errors = ShaderUtil.GetShaderMessages(_previewShader).Where(message => message.severity.ToString() == "Error").ToArray();
                    if (errors.Length > 0) throw new InvalidOperationException(string.Join("\n", errors.Select(message => message.message)));
                    _structure = key;
                }
                else ShaderComposerExport.ApplyValues(_previewMaterial, recipe);
                _error = null;
                _status = "라이트 모델을 가중 평균으로 혼합한 뒤 효과를 위에서 아래 순서로 적용합니다.";
            }
            catch (Exception exception)
            {
                _error = exception.GetBaseException().Message;
            }
            finally
            {
                ShaderUtil.allowAsyncCompilation = async;
                Repaint();
            }
        }

        private void ReleasePreview()
        {
            if (_previewMaterial != null) DestroyImmediate(_previewMaterial);
            if (_previewShader != null) DestroyImmediate(_previewShader);
            _previewMaterial = null;
            _previewShader = null;
            _structure = null;
        }

        private void ReadShader()
        {
            string path = EditorUtility.OpenFilePanel("조립한 Shader 열기", Application.dataPath, "shader");
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                ShaderComposerRecipe loaded = ShaderComposerGenerator.ReadRecipe(File.ReadAllText(path));
                SetRecipe(loaded);
            }
            catch (Exception exception) { EditorUtility.DisplayDialog("Shader 열기", exception.Message, "확인"); }
        }

        private void ExportShader()
        {
            string leaf = recipe.shaderName.Trim().Split('/').Last().Replace(' ', '_');
            string path = EditorUtility.SaveFilePanelInProject("Shader 내보내기", leaf, "shader", "Shader를 저장할 위치를 선택하세요.", "Assets/ProjectL/Script/Render/Shader/View");
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                string previousName = recipe.shaderName.Trim();
                Shader shader = ShaderComposerExport.Write(recipe, path, createMaterial);
                var errors = ShaderUtil.GetShaderMessages(shader).Where(message => message.severity.ToString() == "Error").ToArray();
                if (errors.Length > 0) throw new InvalidOperationException(string.Join("\n", errors.Select(message => message.message)));
                recipe.shaderName = shader.name;
                _source = ShaderComposerGenerator.Generate(recipe);
                RecipeChanged?.Invoke();
                Selection.activeObject = shader;
                EditorGUIUtility.PingObject(shader);
                _status = "내보냈습니다: " + path + (previousName == recipe.shaderName ? string.Empty :
                    "\n이름 중복을 피하도록 Shader 내부 이름을 '" + recipe.shaderName + "'(으)로 조정했습니다.");
            }
            catch (Exception exception) { EditorUtility.DisplayDialog("Shader 내보내기", exception.Message, "확인"); }
        }
    }
}
