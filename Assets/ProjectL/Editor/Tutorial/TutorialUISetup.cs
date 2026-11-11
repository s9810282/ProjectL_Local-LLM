using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;

namespace ProjectL.Tool.Editor
{
    /// <summary>
    /// 튜토리얼 화면 프리팹을 만들고 Combat 씬에 하나 둡니다(튜토리얼 구현 계획 §12.6 사용자 에디터 작업을 대신함).
    /// </summary>
    /// <remarks>
    /// 설명 박스·실습 안내·막힌 조작 안내는 UI 작업자의 공용 안내 패널(<c>GlobalUICanvas.prefab</c>의 <c>NoticePannel</c>)을 복사해 쓰고,
    /// 복사본에서는 그 패널의 화면 스크립트와 Y·N 버튼을 지웁니다(남기면 <c>UIId.Notice</c>가 두 번 등록됨). 원본 패널은 바꾸지 않습니다.
    /// 프리팹이 이미 있으면 다시 만들지 않고(손으로 고친 값 보존) 씬 배치만 맞춥니다. 위치·크기는 만든 뒤 에디터에서 조정합니다.
    /// </remarks>
    public static class TutorialUISetup
    {
        private const string PrefabFolder = "Assets/ProjectL/Prefabs/UI/Tutorial";
        private const string PrefabPath = PrefabFolder + "/TutorialUI.prefab";
        private const string GlobalCanvasPath = "Assets/ProjectL/Prefabs/UI/GlobalUICanvas.prefab";
        private const string NoticePanelName = "NoticePannel";
        private const string TextBoxSpritePath = "Assets/ProjectL/Sprites/TextBox/Box.png";
        private const string CombatScenePath = "Assets/Scenes/Combat.unity";
        // 전역 UI(GlobalUICanvas 정렬 5·99) 위에서 클릭을 막는 정렬이다(설계 §15.4.3, TutorialViewUI가 100 이상을 검사).
        private const int SortingOrder = 110;

        [MenuItem("ProjectL/Tutorial/튜토리얼 UI 프리팹 만들고 Combat 씬에 두기")]
        public static void CreateAndPlace()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("[TutorialUISetup] 재생을 멈춘 뒤 실행해 주세요.");

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            bool created = prefab == null;
            if (created)
                prefab = CreatePrefab();
            else
                prefab = EnsureFocusBorder(prefab);

            int placed = PlaceInCombatScene(prefab);
            Debug.Log($"[TutorialUISetup] 프리팹 {(created ? "만듦" : "이미 있음")}: {PrefabPath}, Combat 씬 인스턴스 {(placed > 0 ? "추가함" : "이미 있음")}.");
        }

        private static GameObject CreatePrefab()
        {
            GameObject global = PrefabUtility.LoadPrefabContents(GlobalCanvasPath);
            Scene preview = EditorSceneManager.NewPreviewScene();
            try
            {
                Transform notice = FindDeep(global.transform, NoticePanelName)
                    ?? throw new InvalidOperationException($"[TutorialUISetup] {GlobalCanvasPath}에 {NoticePanelName}이 없습니다.");
                int uiLayer = LayerMask.NameToLayer("UI");

                var root = new GameObject("TutorialCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                    typeof(GraphicRaycaster));
                SceneManager.MoveGameObjectToScene(root, preview);
                root.layer = uiLayer;
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = SortingOrder;
                // Combat 씬 Canvas와 같은 배율 설정이다.
                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0f;

                GameObject uiObject = Child(root.transform, "TutorialUI", uiLayer);
                Stretch((RectTransform)uiObject.transform);
                TutorialViewUI ui = uiObject.AddComponent<TutorialViewUI>();

                GameObject overlayObject = Child(uiObject.transform, "Overlay", uiLayer);
                Stretch((RectTransform)overlayObject.transform);
                TutorialViewOverlay overlay = overlayObject.AddComponent<TutorialViewOverlay>();
                overlay.color = new Color(0f, 0f, 0f, 0.95f);

                GameObject blockerObject = Child(uiObject.transform, "Blocker", uiLayer);
                Stretch((RectTransform)blockerObject.transform);
                Image blocker = blockerObject.AddComponent<Image>();
                blocker.color = Color.clear;
                blocker.raycastTarget = true;

                GameObject explanation = CopyNotice(notice, uiObject.transform, "ExplanationBox", new Vector2(0f, -220f),
                    new Vector2(560f, 144f));
                ConfigureTextBoxBackground(explanation);
                GameObject practice = CopyNotice(notice, uiObject.transform, "PracticeGuide", new Vector2(-24f, -136f),
                    new Vector2(520f, 112f), alignRight: true);
                ConfigureTextBoxBackground(practice);
                float practiceHeight = ((RectTransform)practice.transform).sizeDelta.y;
                GameObject blocked = CopyNotice(notice, uiObject.transform, "BlockedNotice",
                    new Vector2(-24f, -136f - practiceHeight - 16f), new Vector2(520f, 64f), alignRight: true);
                // 튜토리얼 안내 상자 셋은 모두 같은 배경(Box.png)을 씁니다. 복사한 공용 패널의 배경을 그대로 두지 않습니다.
                ConfigureTextBoxBackground(blocked);

                using (var data = new SerializedObject(ui))
                {
                    data.FindProperty("overlay").objectReferenceValue = overlay;
                    data.FindProperty("focusBorder").objectReferenceValue = CreateFocusBorder(uiObject.transform, explanation.transform);
                    data.FindProperty("targetBorder").objectReferenceValue = CreateTargetBorder(uiObject.transform, explanation.transform);
                    data.FindProperty("blocker").objectReferenceValue = blocker;
                    data.FindProperty("explanationRoot").objectReferenceValue = explanation;
                    data.FindProperty("explanationText").objectReferenceValue = MessageText(explanation);
                    data.FindProperty("practiceRoot").objectReferenceValue = practice;
                    data.FindProperty("practiceText").objectReferenceValue = MessageText(practice);
                    data.FindProperty("blockedRoot").objectReferenceValue = blocked;
                    data.FindProperty("blockedText").objectReferenceValue = MessageText(blocked);
                    data.ApplyModifiedPropertiesWithoutUndo();
                }

                // 열 때 UISystem이 켠다. 처음에는 꺼 둔다.
                explanation.SetActive(false);
                practice.SetActive(false);
                blocked.SetActive(false);
                overlayObject.SetActive(false);
                blockerObject.SetActive(false);
                uiObject.SetActive(false);

                EnsureFolder(PrefabFolder);
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success);
                if (!success || saved == null)
                    throw new InvalidOperationException($"[TutorialUISetup] 프리팹을 저장하지 못했습니다: {PrefabPath}");
                return saved;
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
                PrefabUtility.UnloadPrefabContents(global);
            }
        }

        private static TutorialFocusBorderGraphic CreateFocusBorder(Transform parent, Transform explanation)
        {
            GameObject border = Child(parent, "FocusBorder", parent.gameObject.layer);
            Stretch((RectTransform)border.transform);
            border.transform.SetSiblingIndex(explanation.GetSiblingIndex());
            TutorialFocusBorderGraphic graphic = border.AddComponent<TutorialFocusBorderGraphic>();
            graphic.raycastTarget = false;
            return graphic;
        }

        private static TutorialTargetBorderGraphic CreateTargetBorder(Transform parent, Transform explanation)
        {
            GameObject border = Child(parent, "TargetBorder", parent.gameObject.layer);
            Stretch((RectTransform)border.transform);
            border.transform.SetSiblingIndex(explanation.GetSiblingIndex());
            TutorialTargetBorderGraphic graphic = border.AddComponent<TutorialTargetBorderGraphic>();
            graphic.raycastTarget = false;
            return graphic;
        }

        // 기존 프리팹은 점선 참조가 없을 때만 보완합니다. 문구·크기·스타일 등 수동 조정값은 유지합니다.
        private static GameObject EnsureFocusBorder(GameObject prefab)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                TutorialViewUI[] screens = contents.GetComponentsInChildren<TutorialViewUI>(true);
                if (screens.Length != 1)
                    throw new InvalidOperationException("[TutorialUISetup] UIId.Tutorial 화면은 프리팹에 하나 있어야 합니다.");
                using var data = new SerializedObject(screens[0]);
                SerializedProperty reference = data.FindProperty("focusBorder");
                SerializedProperty targetReference = data.FindProperty("targetBorder");
                if (reference.objectReferenceValue != null && targetReference.objectReferenceValue != null) return prefab;
                var explanation = data.FindProperty("explanationRoot").objectReferenceValue as GameObject;
                if (explanation == null)
                    throw new InvalidOperationException("[TutorialUISetup] UIId.Tutorial의 explanationRoot 참조가 없습니다.");
                if (reference.objectReferenceValue == null)
                {
                    TutorialFocusBorderGraphic existing = screens[0].GetComponentInChildren<TutorialFocusBorderGraphic>(true);
                    reference.objectReferenceValue = existing != null ? existing
                        : CreateFocusBorder(screens[0].transform, explanation.transform);
                }
                if (targetReference.objectReferenceValue == null)
                {
                    TutorialTargetBorderGraphic existing = screens[0].GetComponentInChildren<TutorialTargetBorderGraphic>(true);
                    targetReference.objectReferenceValue = existing != null ? existing
                        : CreateTargetBorder(screens[0].transform, explanation.transform);
                }
                var focus = (TutorialFocusBorderGraphic)reference.objectReferenceValue;
                var target = (TutorialTargetBorderGraphic)targetReference.objectReferenceValue;
                MoveBefore(focus.transform, explanation.transform);
                MoveBefore(target.transform, explanation.transform);
                data.ApplyModifiedPropertiesWithoutUndo();
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath, out bool success);
                if (!success || saved == null)
                    throw new InvalidOperationException("[TutorialUISetup] UIId.Tutorial의 점선 연결을 저장하지 못했습니다.");
                return saved;
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }

        private static void MoveBefore(Transform item, Transform next)
        {
            int index = next.GetSiblingIndex();
            if (item.GetSiblingIndex() < index) index--;
            item.SetSiblingIndex(index);
        }

        // 공용 안내 패널을 복사해 지정한 상단 위치에 둡니다. 화면 스크립트·버튼은 지우고 클릭을 끕니다.
        private static GameObject CopyNotice(Transform notice, Transform parent, string name, Vector2 position, Vector2 size,
            bool alignRight = false)
        {
            GameObject copy = UnityEngine.Object.Instantiate(notice.gameObject, parent, false);
            copy.name = name;
            foreach (UIBase screen in copy.GetComponentsInChildren<UIBase>(true))
                UnityEngine.Object.DestroyImmediate(screen);
            foreach (Button button in copy.GetComponentsInChildren<Button>(true))
                UnityEngine.Object.DestroyImmediate(button.gameObject);
            foreach (Graphic graphic in copy.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;

            var rect = (RectTransform)copy.transform;
            var anchor = new Vector2(alignRight ? 1f : 0.5f, 1f);
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            copy.SetActive(true);
            if (MessageText(copy) == null)
                throw new InvalidOperationException($"[TutorialUISetup] 복사한 {NoticePanelName}에 글자(TMP)가 없습니다.");
            // 본문이 길면 표시 계층에서 높이를 늘립니다. 작은 글씨로 줄여서 숨기지 않습니다.
            MessageText(copy).enableAutoSizing = false;
            MessageText(copy).fontSize = 24f;
            return copy;
        }

        private static void ConfigureTextBoxBackground(GameObject panel)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TextBoxSpritePath)
                ?? throw new InvalidOperationException($"[TutorialUISetup] 안내 배경 Sprite가 없습니다: {TextBoxSpritePath}");
            Image background = panel.GetComponent<Image>();
            TMP_Text text = MessageText(panel);
            if (background == null || text == null)
                throw new InvalidOperationException($"[TutorialUISetup] UIId.Tutorial의 {panel.name}에는 배경 Image와 본문 TMP_Text가 필요합니다.");
            background.sprite = sprite;
            background.type = Image.Type.Sliced;
            background.pixelsPerUnitMultiplier = 2f;
            background.preserveAspect = false;
            background.color = Color.white;
            text.rectTransform.offsetMin = new Vector2(32f, 32f);
            text.rectTransform.offsetMax = new Vector2(-32f, -32f);
        }

        private static TMP_Text MessageText(GameObject panel) => panel.GetComponentInChildren<TMP_Text>(true);

        // Combat 씬에 튜토리얼 화면이 하나 있게 한다. 이미 있으면 두지 않는다. 추가하면 1, 아니면 0이다.
        private static int PlaceInCombatScene(GameObject prefab)
        {
            Scene scene = SceneManager.GetSceneByPath(CombatScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (!opened && scene.isDirty)
                throw new InvalidOperationException("[TutorialUISetup] Combat 씬의 기존 변경을 먼저 저장해 주세요.");
            if (opened)
                scene = EditorSceneManager.OpenScene(CombatScenePath, OpenSceneMode.Additive);

            try
            {
                int existing = 0;
                foreach (GameObject root in scene.GetRootGameObjects())
                    existing += root.GetComponentsInChildren<TutorialViewUI>(true).Length;
                if (existing > 1)
                    throw new InvalidOperationException($"[TutorialUISetup] Combat 씬에 튜토리얼 화면이 {existing}개 있습니다. 하나만 남겨 주세요.");
                if (existing == 1)
                    return 0;

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.transform.SetAsLastSibling();
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("[TutorialUISetup] Combat 씬을 저장하지 못했습니다.");
                return 1;
            }
            finally
            {
                if (opened)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static GameObject Child(Transform parent, string name, int layer)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.layer = layer;
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeep(root.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }
    }
}
