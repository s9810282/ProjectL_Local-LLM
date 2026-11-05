using System;
using System.IO;
using ProjectL.Render.Shader.View.CustomShader.UI.Integration;
using ProjectL.Render.Shader.View.CustomShader.UI.Runtime;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectL.Render.Shader.View.CustomShader.UI.Editor
{
    /// <summary>열린 씬의 기존 로딩 Canvas에 실사용 여정 프리팹을 연결한다.</summary>
    public static class JourneySceneInstaller
    {
        [MenuItem("ProjectL/Render/UI/Apply Journey To Current Scene %&y")]
        public static void ApplyToCurrentScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("씬 적용은 재생을 종료한 뒤 실행해야 한다.");
            }
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrEmpty(scene.path))
            {
                throw new InvalidOperationException("저장 경로가 있는 현재 씬이 필요하다.");
            }
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(JourneyPackageBuilder.PrefabPath);
            if (source == null)
            {
                throw new InvalidOperationException("SandstormJourney 사용 프리팹이 없다.");
            }
            source.GetComponent<SandstormTransitionVisual>().ValidateConfiguration();
            Canvas canvas = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Canvas candidate in root.GetComponentsInChildren<Canvas>(true))
                {
                    if (candidate.name != "LodingCanvas" && candidate.name != "LoadingCanvas")
                    {
                        continue;
                    }
                    if (canvas != null)
                    {
                        throw new InvalidOperationException(scene.name + ": 로딩 Canvas가 둘 이상이다.");
                    }
                    canvas = candidate;
                }
            }
            if (canvas == null)
            {
                throw new InvalidOperationException(scene.name + ": LodingCanvas 또는 LoadingCanvas가 필요하다.");
            }
            SandstormLoadingUI owner = canvas.GetComponent<SandstormLoadingUI>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (UIBase ui in root.GetComponentsInChildren<UIBase>(true))
                {
                    if (ui.ID == UIId.Loading && ui != owner)
                    {
                        throw new InvalidOperationException(scene.name + ": UIId.Loading이 이미 등록 대상이다: " + ui.name);
                    }
                }
            }
            SandstormTransitionVisual[] existing = canvas.GetComponentsInChildren<SandstormTransitionVisual>(true);
            if (existing.Length > 1)
            {
                throw new InvalidOperationException(canvas.name + ": 여정 위젯이 둘 이상이다.");
            }
            string backup = JourneyPackageBuilder.Root + "/Verification/" + scene.name + "-before-apply.unity";
            if (!File.Exists(backup) && !EditorSceneManager.SaveScene(scene, backup, true))
            {
                throw new InvalidOperationException("현재 미저장 작업의 적용 전 사본을 보존하지 못했다.");
            }

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Apply Sandstorm Journey UI");
            try
            {
                Undo.RecordObject(canvas, "Set Loading Canvas Order");
                canvas.sortingOrder = 32000;
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    Undo.RecordObject(scaler, "Set Loading Canvas Scale");
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    scaler.matchWidthOrHeight = 0.5f;
                }
                SandstormTransitionVisual visual;
                if (existing.Length == 0)
                {
                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source, canvas.transform);
                    Undo.RegisterCreatedObjectUndo(instance, "Add Sandstorm Journey");
                    visual = instance.GetComponent<SandstormTransitionVisual>();
                }
                else
                {
                    visual = existing[0];
                }
                Undo.RecordObject(visual, "Use Automatic Journey Clock");
                visual.UseExternalClock = false;
                Undo.RecordObject(visual.gameObject, "Enable Journey Widget");
                visual.gameObject.SetActive(true);
                RectTransform rect = visual.GetComponent<RectTransform>();
                Undo.RecordObject(rect, "Fit Journey To Loading Canvas");
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.identity;
                rect.SetAsLastSibling();
                if (owner == null)
                {
                    owner = Undo.AddComponent<SandstormLoadingUI>(canvas.gameObject);
                }
                SerializedObject serialized = new SerializedObject(owner);
                serialized.FindProperty("visual").objectReferenceValue = visual;
                serialized.ApplyModifiedProperties();
                owner.ValidateConfiguration();
                foreach (Transform child in canvas.transform)
                {
                    if (child.name == "LodingImage" || child.name == "LoadingImage")
                    {
                        Undo.RecordObject(child.gameObject, "Keep Original Loading Image Disabled");
                        child.gameObject.SetActive(false);
                    }
                }
                PrefabUtility.RecordPrefabInstancePropertyModifications(visual);
                PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException("여정 UI를 적용한 현재 씬을 저장하지 못했다.");
                }
                Undo.CollapseUndoOperations(group);
                Selection.activeGameObject = owner.gameObject;
                File.WriteAllText(JourneyPackageBuilder.Root + "/Verification/Scene-application.txt",
                    "씬: " + scene.path + "\n소유자: " + owner.name + " / UIId.Loading / Parallel\n" +
                    "위젯: " + visual.name + " (prefab instance)\n기존 흰 이미지: 보존·비활성\n" +
                    "자동 unscaled 시간원, Canvas 정렬 32000, 씬 저장 완료\n적용 전 사본: " + backup + "\n");
                Debug.Log("[JourneyUI] 현재 씬 적용·저장 완료: " + scene.path, owner);
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                throw;
            }
        }
    }
}
