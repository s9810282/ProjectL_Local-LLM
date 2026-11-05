using System;
using ProjectL.Render.Shader.View.CustomShader.UI.Integration;
using ProjectL.Render.Shader.View.CustomShader.UI.Runtime;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Render.Shader.View.CustomShader.UI.Editor
{
    /// <summary>Boot가 생성하는 공용 UI 프리팹에 요청형 로딩 화면을 조립한다.</summary>
    public static class JourneyLoadingInstaller
    {
        public const string GlobalPrefabPath = "Assets/ProjectL/Prefabs/UI/GlobalUICanvas.prefab";

        [MenuItem("ProjectL/Render/UI/Install Journey Loading In Global Canvas %&g")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("로딩 UI 조립은 재생을 끈 상태에서 실행하세요.");
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(JourneyPackageBuilder.PrefabPath);
            if (source == null) throw new InvalidOperationException("SandstormJourney 프리팹이 없습니다.");
            source.GetComponent<SandstormTransitionVisual>().ValidateConfiguration();
            GameObject contents = PrefabUtility.LoadPrefabContents(GlobalPrefabPath);
            try
            {
                SandstormLoadingUI owner = null;
                foreach (var ui in contents.GetComponentsInChildren<ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base.UIBase>(true))
                {
                    if (ui.ID != UIId.Loading) continue;
                    if (owner != null || ui is not SandstormLoadingUI candidate)
                        throw new InvalidOperationException("GlobalUICanvas의 UIId.Loading 소유자가 중복되거나 호환되지 않습니다.");
                    owner = candidate;
                }
                if (owner == null)
                {
                    var screen = new GameObject("SandstormLoading", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
                    screen.SetActive(false);
                    screen.layer = LayerMask.NameToLayer("UI");
                    screen.transform.SetParent(contents.transform, false);
                    RectTransform rect = screen.GetComponent<RectTransform>();
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                    Canvas canvas = screen.GetComponent<Canvas>();
                    canvas.overrideSorting = true;
                    canvas.sortingOrder = 32000;
                    GameObject widget = (GameObject)PrefabUtility.InstantiatePrefab(source, screen.transform);
                    SandstormTransitionVisual visual = widget.GetComponent<SandstormTransitionVisual>();
                    visual.UseExternalClock = false;
                    RectTransform widgetRect = widget.GetComponent<RectTransform>();
                    widgetRect.anchorMin = Vector2.zero;
                    widgetRect.anchorMax = Vector2.one;
                    widgetRect.offsetMin = widgetRect.offsetMax = Vector2.zero;
                    widgetRect.localScale = Vector3.one;
                    owner = screen.AddComponent<SandstormLoadingUI>();
                    var serialized = new SerializedObject(owner);
                    serialized.FindProperty("visual").objectReferenceValue = visual;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(visual);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(widgetRect);
                }
                owner.ValidateConfiguration();
                owner.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(contents, GlobalPrefabPath);
                Debug.Log("[JourneyUI] 공용 로딩 UI 연결 완료. LodingService.Load마다 덮기/로딩/걷어내기를 재사용합니다.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }
    }
}
