using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ProjectL.Tool.Editor
{
    [Serializable]
    internal sealed class ShaderComposerPreviewLight
    {
        [SerializeField] internal bool enabled = true;
        [SerializeField] internal bool expanded = true;
        [SerializeField] internal LightType type = LightType.Directional;
        [SerializeField] internal Color color = Color.white;
        [SerializeField] internal float intensity = 1;
        [SerializeField] internal Vector3 position = new Vector3(0, 1, -2);
        [SerializeField] internal Vector3 rotation = new Vector3(30, 0, 0);
        [SerializeField] internal float range = 6;
        [SerializeField] internal float spotAngle = 60;
        [SerializeField] internal float innerSpotAngle = 35;
    }

    [Serializable]
    internal sealed class ShaderComposerPreviewSettings
    {
        internal const int MaximumLights = 8;
        private static readonly Color DefaultBackground = new Color(0.17f, 0.17f, 0.17f);
        private static readonly Color DefaultAmbient = new Color(0.25f, 0.25f, 0.25f);
        [SerializeField] internal Color background = DefaultBackground;
        [SerializeField] internal Color ambient = DefaultAmbient;
        [SerializeField] internal bool showLightOverlay = true;
        [SerializeField] internal bool showLightRanges = true;
        [SerializeField] internal bool postProcessing = true;
        [SerializeField] internal VolumeProfile volumeProfile;
        [SerializeField] internal List<ShaderComposerPreviewLight> lights = DefaultLights();

        private static List<ShaderComposerPreviewLight> DefaultLights()
        {
            return new List<ShaderComposerPreviewLight>
            {
                new ShaderComposerPreviewLight { intensity = 1.1f, rotation = new Vector3(45, 35, 0) },
                new ShaderComposerPreviewLight { intensity = 0.5f, rotation = new Vector3(340, 210, 0) }
            };
        }

        internal void Reset()
        {
            background = DefaultBackground;
            ambient = DefaultAmbient;
            lights = DefaultLights();
        }
    }

    internal static class ShaderComposerPreviewSettingsGUI
    {
        private static readonly string[] Types = { "Directional", "Point", "Spot" };
        private static readonly LightType[] LightTypes = { LightType.Directional, LightType.Point, LightType.Spot };

        internal static bool Draw(ShaderComposerPreviewSettings settings, ref Vector2 scroll)
        {
            EditorGUI.BeginChangeCheck();
            bool changedList = false;
            EditorGUILayout.LabelField("프리뷰 설정", EditorStyles.boldLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            settings.background = EditorGUILayout.ColorField("배경색", settings.background);
            settings.ambient = EditorGUILayout.ColorField("환경광", settings.ambient);
            settings.showLightOverlay = EditorGUILayout.Toggle("라이트 오버레이", settings.showLightOverlay);
            using (new EditorGUI.DisabledScope(!settings.showLightOverlay))
                settings.showLightRanges = EditorGUILayout.Toggle("영향 범위 표시", settings.showLightRanges);
            settings.postProcessing = EditorGUILayout.Toggle("URP 후처리", settings.postProcessing);
            using (new EditorGUI.DisabledScope(!settings.postProcessing))
                settings.volumeProfile = (VolumeProfile)EditorGUILayout.ObjectField("Volume Profile", settings.volumeProfile, typeof(VolumeProfile), false);
            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset))
                EditorGUILayout.HelpBox("URP가 활성화된 프로젝트에서 후처리를 미리볼 수 있습니다.", MessageType.Info);
            else if (settings.postProcessing && settings.volumeProfile == null)
                EditorGUILayout.HelpBox("프로필을 넣으면 해당 후처리를 적용합니다. 비어 있으면 URP 기본 Volume 설정을 사용합니다.", MessageType.Info);
            EditorGUILayout.HelpBox("라이트 위치는 모델 중심 기준이며, 위치·범위의 1은 모델 반경입니다. 실제 동시 반영 수는 URP 설정을 따릅니다.", MessageType.Info);
            if (GUILayout.Button("기본 조명 복원"))
            {
                settings.Reset();
                changedList = true;
            }
            for (int i = 0; i < settings.lights.Count; i++)
            {
                ShaderComposerPreviewLight light = settings.lights[i];
                bool removed = false;
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        light.enabled = EditorGUILayout.Toggle(light.enabled, GUILayout.Width(18));
                        light.expanded = EditorGUILayout.Foldout(light.expanded, "라이트 " + (i + 1), true);
                        if (GUILayout.Button("삭제", GUILayout.Width(42)))
                        {
                            settings.lights.RemoveAt(i);
                            changedList = removed = true;
                        }
                    }
                    if (!removed && light.expanded)
                    {
                        using (new EditorGUI.DisabledScope(!light.enabled))
                        {
                            int current = Mathf.Max(0, Array.IndexOf(LightTypes, light.type));
                            light.type = LightTypes[EditorGUILayout.Popup("종류", current, Types)];
                            light.color = EditorGUILayout.ColorField("색", light.color);
                            light.intensity = Mathf.Max(0, EditorGUILayout.FloatField("강도", light.intensity));
                            if (light.type != LightType.Directional)
                            {
                                light.position = EditorGUILayout.Vector3Field("위치 (반경 배수)", light.position);
                                light.range = Mathf.Max(0.01f, EditorGUILayout.FloatField("범위 (반경 배수)", light.range));
                            }
                            if (light.type != LightType.Point)
                                light.rotation = EditorGUILayout.Vector3Field("회전 (도)", light.rotation);
                            if (light.type == LightType.Spot)
                            {
                                light.spotAngle = EditorGUILayout.Slider("바깥 각도", light.spotAngle, 1, 179);
                                light.innerSpotAngle = EditorGUILayout.Slider("안쪽 각도", light.innerSpotAngle, 0, light.spotAngle);
                            }
                        }
                    }
                }
                if (removed) break;
            }
            using (new EditorGUI.DisabledScope(settings.lights.Count >= ShaderComposerPreviewSettings.MaximumLights))
            {
                if (GUILayout.Button("+ 라이트 추가"))
                {
                    settings.lights.Add(new ShaderComposerPreviewLight());
                    changedList = true;
                }
            }
            EditorGUILayout.EndScrollView();
            return EditorGUI.EndChangeCheck() || changedList;
        }
    }
}
