using System;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    public sealed class ShaderComposerPropertiesWindow : EditorWindow
    {
        [SerializeField] private ShaderComposerWindow composer;
        private ShaderComposerWindow _subscribedComposer;
        private Vector2 _scroll;

        [MenuItem("ProjectL/Shader Composer Properties")]
        private static void OpenFromMenu()
        {
            ShaderComposerWindow.Open();
            Open(GetWindow<ShaderComposerWindow>());
        }

        internal static void Open(ShaderComposerWindow owner)
        {
            var window = GetWindow<ShaderComposerPropertiesWindow>();
            window.Bind(owner);
            window.Show();
            window.Focus();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Shader Properties");
            minSize = new Vector2(360, 300);
            Bind(composer);
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Bind(ShaderComposerWindow owner)
        {
            Unsubscribe();
            composer = owner;
            _subscribedComposer = owner;
            if (_subscribedComposer != null) _subscribedComposer.RecipeChanged += OnRecipeChanged;
            Repaint();
        }

        private void Unsubscribe()
        {
            if (_subscribedComposer != null) _subscribedComposer.RecipeChanged -= OnRecipeChanged;
            _subscribedComposer = null;
        }

        private void OnRecipeChanged()
        {
            Repaint();
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(composer != null ? composer.Recipe.shaderName : "Shader Properties", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                if (composer != null && GUILayout.Button("조립 창 보기", EditorStyles.toolbarButton)) composer.Focus();
                if (GUILayout.Button("사용 설명서", EditorStyles.toolbarButton)) ShaderComposerHelpWindow.Open();
            }
            if (composer == null)
            {
                EditorGUILayout.HelpBox("연결된 조립 창이 없습니다. 조립 창을 열어 프로퍼티를 편집하세요.", MessageType.Info);
                if (GUILayout.Button("조립 창 열기")) OpenFromMenu();
                return;
            }

            ShaderComposerRecipe recipe = composer.Recipe;
            EditorGUILayout.LabelField("여기서 만든 프로퍼티를 조립 창의 입력에서 참조합니다.", EditorStyles.wordWrappedMiniLabel);
            Undo.RecordObject(composer, "셰이더 프로퍼티 변경");
            EditorGUI.BeginChangeCheck();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            bool changedList = DrawProperties(recipe);
            EditorGUILayout.EndScrollView();
            if (EditorGUI.EndChangeCheck() || changedList) composer.NotifyRecipeChanged();
        }

        private static bool DrawProperties(ShaderComposerRecipe recipe)
        {
            bool changedList = false;
            for (int i = 0; i < recipe.properties.Count; i++)
            {
                ShaderComposerProperty property = recipe.properties[i];
                bool removed = false;
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        property.expanded = EditorGUILayout.Foldout(property.expanded, property.label + "  (" + property.name + ")", true);
                        if (GUILayout.Button("삭제", GUILayout.Width(42)))
                        {
                            ShaderComposerProperties.Detach(recipe, property);
                            recipe.properties.RemoveAt(i);
                            removed = true;
                            changedList = true;
                        }
                    }
                    if (property.expanded && !removed)
                    {
                        property.name = EditorGUILayout.TextField("Shader 이름", property.name);
                        property.label = EditorGUILayout.TextField("표시 이름", property.label);
                        int selected = EditorGUILayout.Popup("타입", Array.IndexOf(ShaderComposerProperties.Kinds, property.kind), ShaderComposerProperties.Labels);
                        if (property.kind != ShaderComposerProperties.Kinds[selected])
                        {
                            property.kind = ShaderComposerProperties.Kinds[selected];
                            ShaderComposerProperties.DetachIncompatible(recipe, property);
                        }
                        if (property.kind == ShaderComposerParameterKind.Range)
                        {
                            property.minimum = EditorGUILayout.FloatField("최솟값", property.minimum);
                            property.maximum = EditorGUILayout.FloatField("최댓값", property.maximum);
                        }
                        if (property.kind == ShaderComposerParameterKind.Color)
                            property.hdr = EditorGUILayout.Toggle("HDR", property.hdr);
                        ShaderComposerParameter parameter = property.Parameter();
                        if (property.kind == ShaderComposerParameterKind.Range &&
                            (property.minimum >= property.maximum || float.IsNaN(property.minimum) || float.IsInfinity(property.minimum) ||
                            float.IsNaN(property.maximum) || float.IsInfinity(property.maximum)))
                            parameter = new ShaderComposerParameter(property.name, "기본값", ShaderComposerParameterKind.Number);
                        ShaderComposerWindow.DrawValue(parameter, property.value, "기본값");
                    }
                }
                if (removed) break;
            }
            if (GUILayout.Button("+ 프로퍼티 추가", GUILayout.Height(25)))
            {
                int suffix = recipe.properties.Count + 1;
                while (recipe.properties.Exists(property => property.name == "_Property" + suffix)) suffix++;
                recipe.properties.Add(new ShaderComposerProperty
                {
                    id = Guid.NewGuid().ToString("N"), name = "_Property" + suffix
                });
                changedList = true;
            }
            return changedList;
        }
    }
}
