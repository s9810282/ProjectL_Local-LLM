using ProjectL.Tool.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectL.Tool.Editor
{
    public sealed partial class FbxPlacementWindow
    {
        [SerializeField] private bool showBakeCatalog;
        [SerializeField] private FbxPlacementBakeSettings bakeSettings = new FbxPlacementBakeSettings();
        private Vector2 _bakeScroll;

        private void DrawBakeCatalog()
        {
            using var area = new GUILayout.AreaScope(new Rect(8, 6, CatalogWidth - 16, CatalogContentHeight - 12));
            using var scroll = new EditorGUILayout.ScrollViewScope(_bakeScroll);
            _bakeScroll = scroll.scrollPosition;
            float previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 92;
            try
            {
                GUILayout.Label("맵 라이트 베이크", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("완료하면 베이크된 맵을 이 작업실에 표시합니다.", EditorStyles.wordWrappedMiniLabel);
                if (FbxPlacementBaker.IsRunning)
                {
                    Rect progress = GUILayoutUtility.GetRect(10, 24);
                    EditorGUI.ProgressBar(progress, FbxPlacementBaker.Current.Progress, "베이크 중…");
                    EditorGUILayout.HelpBox("완료하면 원래 씬으로 돌아옵니다. 베이크 중에는 배치를 변경하지 마세요.", MessageType.Info);
                    if (GUILayout.Button("베이크 취소")) DeferEditorAction(() => FbxPlacementBaker.Current?.Cancel());
                    return;
                }
                using (new EditorGUI.DisabledScope(!HasBakedResult))
                    if (GUILayout.Button("베이크 완성본 보기", GUILayout.Height(26))) DeferEditorAction(ShowBakedResult);
                if (GUILayout.Button("저장된 맵 불러오기…")) DeferEditorAction(BrowseMapDefinition);
                int targets = FbxPlacementBakeData.GetTargets(workspaceRoot).Count;
                GUILayout.Space(8);
                GUILayout.Label("1. 대상과 UV2", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("베이크 대상", targets + "개");
                if (GUILayout.Button("전체 메시 대상 설정"))
                    Run(() => { FbxPlacementBakeData.SetTargets(workspaceRoot, true); Changed(); });
                EditorGUILayout.LabelField("움직일 오브젝트는 선택 후 '베이크 대상'을 꺼 주세요.", EditorStyles.wordWrappedMiniLabel);
                if (GUILayout.Button("누락 UV2 자동 생성"))
                    DeferEditorAction(() =>
                    {
                        FbxPlacementBakeData.GenerateMissingUvs(workspaceRoot);
                        SetNotice("UV2 확인/생성 완료", MessageType.Info);
                    });
                EditorGUILayout.LabelField("UV2 생성은 원본 FBX의 Import Settings에도 적용됩니다.", EditorStyles.wordWrappedMiniLabel);
                GUILayout.Space(8);
                GUILayout.Label("2. 품질", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("빠른 확인"))
                    {
                        bakeSettings.resolution = 4;
                        bakeSettings.directSamples = 8;
                        bakeSettings.indirectSamples = 32;
                        bakeSettings.environmentSamples = 16;
                    }
                    if (GUILayout.Button("기본 품질")) bakeSettings = new FbxPlacementBakeSettings();
                }
                int mapper = bakeSettings.lightmapper == LightingSettings.Lightmapper.ProgressiveCPU ? 1 : 0;
                bakeSettings.lightmapper = EditorGUILayout.Popup("Lightmapper", mapper, new[] { "Progressive GPU", "Progressive CPU" }) == 0
                    ? LightingSettings.Lightmapper.ProgressiveGPU : LightingSettings.Lightmapper.ProgressiveCPU;
                bakeSettings.resolution = EditorGUILayout.Slider("해상도", bakeSettings.resolution, 1, 40);
                bakeSettings.atlasSize = EditorGUILayout.IntPopup("아틀라스", bakeSettings.atlasSize,
                    new[] { "256", "512", "1024", "2048", "4096" }, new[] { 256, 512, 1024, 2048, 4096 });
                bakeSettings.directSamples = Mathf.Max(1, EditorGUILayout.IntField("직접광 샘플", bakeSettings.directSamples));
                bakeSettings.indirectSamples = Mathf.Max(1, EditorGUILayout.IntField("간접광 샘플", bakeSettings.indirectSamples));
                bakeSettings.environmentSamples = Mathf.Max(1, EditorGUILayout.IntField("환경광 샘플", bakeSettings.environmentSamples));
                bakeSettings.mixedMode = (MixedLightingMode)EditorGUILayout.EnumPopup("Mixed 모드", bakeSettings.mixedMode);
                bakeSettings.directionalMode = (LightmapsMode)EditorGUILayout.EnumPopup("Directional", bakeSettings.directionalMode);
                bakeSettings.ambientColor = EditorGUILayout.ColorField("환경광", bakeSettings.ambientColor);
                GUILayout.Space(8);
                GUILayout.Label("3. 베이크", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("라이트 Mode를 Baked 또는 Mixed로 설정하세요. 원래 씬을 저장한 뒤 전용 베이크 씬으로 잠시 전환합니다.", MessageType.Info);
                using (new EditorGUI.DisabledScope(Lightmapping.isRunning || targets == 0))
                    if (GUILayout.Button("베이크 + 맵 SO 저장", GUILayout.Height(30))) DeferEditorAction(StartWorkspaceBake);
                MapBakedLighting binding = workspaceRoot.GetComponent<MapBakedLighting>();
                if (binding != null && binding.AtlasCount > 0)
                {
                    EditorGUILayout.LabelField("저장된 결과", binding.AtlasCount + "장 / " + binding.SurfaceCount + "메시");
                    EditorGUILayout.LabelField("배치·라이트·재질을 바꾸면 다시 베이크하세요.", EditorStyles.wordWrappedMiniLabel);
                }
                EditorGUILayout.LabelField("맵 SO가 배치 프리팹과 베이크 정보 SO를 참조합니다. DB에는 맵 SO를 등록하세요.", EditorStyles.wordWrappedMiniLabel);
            }
            finally
            {
                EditorGUIUtility.labelWidth = previousLabelWidth;
            }
        }

        private void StartWorkspaceBake()
        {
            FbxPlacementBakeData.Validate(workspaceRoot);
            if (!TrySave(false, true)) return;
            FbxPlacementBaker.Start(workspaceRoot, savePath, bakeSettings, (success, message) =>
            {
                if (this == null) return;
                if (success)
                {
                    hasUnsavedChanges = false;
                    useWorkspaceLighting = sceneLighting = true;
                    mapDefinition = AssetDatabase.LoadAssetAtPath<MapDefinitionSO>(FbxPlacementMapAssets.DefinitionPath(savePath));
                    mapPrefab = mapDefinition.Prefab;
                    ShowBakedResult();
                }
                SetNotice(message, success ? MessageType.Info : MessageType.Warning);
            });
        }

    }
}
