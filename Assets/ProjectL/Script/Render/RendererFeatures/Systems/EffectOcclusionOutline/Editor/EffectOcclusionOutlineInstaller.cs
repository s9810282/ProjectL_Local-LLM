using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ProjectL.Render.RendererFeatures.Systems.EffectOcclusionOutline.Editor
{
    /// <summary>선택한 Renderer Data에 기능과 빌드용 셰이더 참조를 조립한다.</summary>
    internal static class EffectOcclusionOutlineInstaller
    {
        internal const string Root = "Assets/ProjectL/Script/Render/RendererFeatures/Systems/EffectOcclusionOutline";

        [MenuItem("ProjectL/Render/Include Outline Layers In Selected Renderer %&l")]
        private static void IncludeSourceLayers()
        {
            if (Selection.activeObject is not UniversalRendererData renderer)
            {
                throw new InvalidOperationException("Project 창에서 Universal Renderer Data 에셋을 선택하세요.");
            }
            EffectOcclusionOutlineFeature outline = null;
            foreach (ScriptableRendererFeature feature in renderer.rendererFeatures)
            {
                if (feature is EffectOcclusionOutlineFeature candidate)
                {
                    outline = candidate;
                    break;
                }
            }
            if (outline == null || outline.Settings.characterLayers.value == 0 || outline.Settings.effectLayers.value == 0)
            {
                throw new InvalidOperationException(renderer.name + ": 윤곽선 기능의 Character Layers / Effect Layers를 먼저 지정하세요.");
            }
            if ((outline.Settings.characterLayers.value & outline.Settings.effectLayers.value) != 0)
            {
                throw new InvalidOperationException(renderer.name + ": Character Layers와 Effect Layers는 겹칠 수 없다.");
            }
            Undo.RecordObject(renderer, "윤곽선 원본 레이어 렌더 복구");
            // 일반 렌더의 포함 비트만 더한다. 기존의 다른 마스크 비트는 보존한다.
            renderer.opaqueLayerMask |= outline.Settings.characterLayers.value | outline.Settings.effectLayers.value;
            renderer.transparentLayerMask |= outline.Settings.effectLayers.value;
            renderer.SetDirty();
            EditorUtility.SetDirty(renderer);
            AssetDatabase.SaveAssetIfDirty(renderer);
            AssetDatabase.SaveAssetIfDirty(outline);
            Debug.Log("[Effect Outline] " + renderer.name + ": 캐릭터/이펙트의 기본 렌더 레이어 포함 및 설정 저장 완료.", renderer);
        }

        [MenuItem("ProjectL/Render/Add Effect Occlusion Outline To Selected Renderer")]
        private static void Install()
        {
            if (Selection.activeObject is not UniversalRendererData renderer)
            {
                throw new InvalidOperationException("Project 창에서 Universal Renderer Data 에셋을 선택하세요.");
            }
            foreach (ScriptableRendererFeature existing in renderer.rendererFeatures)
            {
                if (existing is EffectOcclusionOutlineFeature)
                {
                    Selection.activeObject = existing;
                    Debug.Log("[Effect Outline] 이미 설치되어 있다. Character/Effect Layers를 설정하세요.", existing);
                    return;
                }
            }
            UnityEngine.Shader shader = AssetDatabase.LoadAssetAtPath<UnityEngine.Shader>(Root + "/EffectOcclusionOutline.shader");
            if (shader == null)
            {
                throw new InvalidOperationException("EffectOcclusionOutline.shader 참조를 찾을 수 없다.");
            }
            Undo.RecordObject(renderer, "이펙트 가림 윤곽선 추가");
            var feature = ScriptableObject.CreateInstance<EffectOcclusionOutlineFeature>();
            feature.name = "Effect Occlusion Outline";
            feature.SetShader(shader);
            Undo.RegisterCreatedObjectUndo(feature, "이펙트 가림 윤곽선 추가");
            AssetDatabase.AddObjectToAsset(feature, renderer);
            // URP 에디터와 같은 방식으로 참조 목록과 로컬 파일 ID 복구 맵을 함께 저장한다.
            var serialized = new SerializedObject(renderer);
            SerializedProperty features = serialized.FindProperty("m_RendererFeatures");
            SerializedProperty featureMap = serialized.FindProperty("m_RendererFeatureMap");
            features.arraySize++;
            features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;
            featureMap.arraySize = features.arraySize;
            for (int i = 0; i < features.arraySize; i++)
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(features.GetArrayElementAtIndex(i).objectReferenceValue,
                    out string _, out long localId);
                featureMap.GetArrayElementAtIndex(i).longValue = localId;
            }
            serialized.ApplyModifiedProperties();
            renderer.SetDirty();
            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(feature);
            AssetDatabase.SaveAssetIfDirty(renderer);
            Selection.activeObject = renderer;
            Debug.Log("[Effect Outline] 설치 완료. 서로 다른 Character Layers / Effect Layers를 지정하세요. 씬 레이어는 자동 변경하지 않았다.", renderer);
        }
    }
}
