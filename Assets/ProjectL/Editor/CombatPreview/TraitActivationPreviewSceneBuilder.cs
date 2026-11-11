using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev.TraitPreview;
using Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ProjectL.Tool.Editor
{
    /// <summary>운영 씬의 표시 설정만 복사하여 게임 진행 UI가 없는 테스트 씬을 만듭니다.</summary>
    public static class TraitActivationPreviewSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Dev/TraitActivationPreview.unity";
        public const string CameraPrefabPath = "Assets/ProjectL/Prefabs/Stage/CombatCamera.prefab";

        public static void EnsureScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) return;
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var source = EditorSceneManager.OpenScene("Assets/Scenes/Combat.unity", OpenSceneMode.Additive);
                var boot = EditorSceneManager.OpenScene("Assets/Scenes/Boot.unity", OpenSceneMode.Additive);
                GameObject sourceAudio = boot.GetRootGameObjects().Single(o => o.GetComponent<AudioManager>() != null);
                FieldView sourceField = source.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<FieldView>(true)).Single();
                FieldUnitPresenter sourceUnits = source.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<FieldUnitPresenter>(true)).Single();
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
                var root = new GameObject("TraitActivationPreview");
                UnityEngine.Object.Instantiate(sourceAudio).name = "AudioManager";
                var host = root.AddComponent<TraitActivationPreviewHost>();
                var fieldRoot = new GameObject("Field");
                fieldRoot.transform.SetParent(root.transform, false);
                var field = fieldRoot.AddComponent<FieldView>();
                EditorUtility.CopySerialized(sourceField, field);
                var cells = new GameObject("Cells");
                cells.transform.SetParent(fieldRoot.transform, false);
                Set(field, "cellsRoot", cells.transform);
                var unitRoot = new GameObject("Units");
                unitRoot.transform.SetParent(root.transform, false);
                var units = unitRoot.AddComponent<FieldUnitPresenter>();
                EditorUtility.CopySerialized(sourceUnits, units);
                Set(units, "unitsRoot", unitRoot.transform);
                var sourceHealth = new SerializedObject(sourceUnits).FindProperty("healthLabelTemplate").objectReferenceValue as Component;
                if (sourceHealth == null) throw new InvalidOperationException("운영 유닛 표시의 체력 템플릿 참조가 없습니다.");
                GameObject health = UnityEngine.Object.Instantiate(sourceHealth.gameObject, unitRoot.transform);
                health.SetActive(false);
                Set(units, "healthLabelTemplate", health.GetComponent(sourceHealth.GetType()));
                var cameraRoot = new GameObject("TraitPreviewOutput");
                cameraRoot.tag = "MainCamera";
                var output = cameraRoot.AddComponent<Camera>();
                output.orthographic = true;
                output.orthographicSize = 8;
                output.nearClipPlane = 0.1f;
                output.farClipPlane = 500;
                cameraRoot.transform.position = new Vector3(4, 12, -8);
                cameraRoot.transform.LookAt(new Vector3(4, 0, 4));
                output.GetUniversalAdditionalCameraData().renderPostProcessing = true;
                cameraRoot.AddComponent<CinemachineBrain>();
                cameraRoot.AddComponent<AudioListener>();
                cameraRoot.AddComponent<FMODUnity.StudioListener>();
                GameObject cameraPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CameraPrefabPath);
                if (cameraPrefab == null)
                    throw new InvalidOperationException($"공용 전투 카메라 프리팹이 없습니다: {CameraPrefabPath}");
                GameObject combatCamera = (GameObject)PrefabUtility.InstantiatePrefab(cameraPrefab, scene);
                CombatSceneCameraBinding cameraBinding = combatCamera.GetComponent<CombatSceneCameraBinding>();
                if (cameraBinding == null)
                    throw new InvalidOperationException("공용 전투 카메라 프리팹에 CombatSceneCameraBinding이 없습니다.");
                var light = new GameObject("Preview Light").AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1;
                light.transform.rotation = Quaternion.Euler(45, -30, 0);
                Set(host, "catalog", AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/ProjectL/DataBaseSO/DatabaseCatalogSO.asset"));
                Set(host, "field", field);
                Set(host, "units", units);
                Set(host, "output", output);
                Set(host, "sceneCamera", cameraBinding);
                if (!AssetDatabase.IsValidFolder("Assets/Scenes/Dev")) AssetDatabase.CreateFolder("Assets/Scenes", "Dev");
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(previous); }
            AssetDatabase.SaveAssets();
        }

        private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field)
                ?? throw new InvalidOperationException($"필수 씬 참조 필드가 없습니다: {target.name}.{field}");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
