using System;
using System.Collections.Generic;
using System.IO;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectL.Tool.Editor
{
    /// <summary>
    /// StageSelect 덱 편집 팝업(<see cref="DeckEditUI"/>)의 프리팹을 만들고 씬에 배치·연결합니다(런 덱 연결 계획 RD-4).
    /// 이미 있는 프리팹·씬 인스턴스·참조는 유지합니다. 다시 만들려면 프리팹을 지운 뒤 실행합니다.
    /// </summary>
    public static class StageDeckEditUISetup
    {
        private const string ScenePath = "Assets/Scenes/StageSelect.unity";
        private const string DeckFolder = "Assets/ProjectL/Prefabs/UI/Deck";
        private const string PrefabPath = DeckFolder + "/DeckEditUI.prefab";
        private const string SlotPath = DeckFolder + "/DeckSlot.prefab";
        private const string OwnedSlotPath = DeckFolder + "/OwnedUnitSlot.prefab";
        private const string FontPath = "Assets/ProjectL/Resources/Font/함렡_Extra Bold SDF.asset";
        private const string FallbackFontPath = "Assets/ProjectL/Resources/Font/Hahmlet-Bold SDF.asset";
        private const int DeckSlots = 4;

        [MenuItem("ProjectL/Stage/Ensure StageSelect Deck Edit UI")]
        public static void EnsureStageSelectDeckEditUI()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("[StageDeckEditUISetup] Play Mode를 멈춘 뒤 실행하세요.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("[StageDeckEditUISetup] 열린 씬의 변경을 먼저 저장하세요.");
            }

            // 씬을 Single로 열면 Unity가 쓰이지 않는 에셋을 내려 그 전에 불러 둔 참조가 파괴됩니다.
            // 그래서 씬을 먼저 열고 글꼴·카드·프리팹은 그 뒤에 불러옵니다(상점 도구와 같은 순서).
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null) font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackFontPath);
            if (font == null)
                throw new InvalidOperationException("[StageDeckEditUISetup] 한국어 TMP 글꼴을 찾지 못했습니다: " + FontPath);
            GameObject slot = AssetDatabase.LoadAssetAtPath<GameObject>(SlotPath);
            if (slot == null || slot.GetComponent<DeckEditSlotView>() == null)
                throw new InvalidOperationException("[StageDeckEditUISetup] DeckEditSlotView가 있는 DeckSlot 프리팹이 필요합니다: " + SlotPath);

            EnsureFolder(DeckFolder);
            DeckEditUI prefab = EnsurePrefab(font, slot);
            Validate(prefab);

            StageMapSceneRoot[] roots = Components<StageMapSceneRoot>(scene);
            if (roots.Length != 1)
                throw new InvalidOperationException("[StageDeckEditUISetup] StageMapSceneRoot가 하나 있어야 합니다.");
            StageMapCanvasView canvasView;
            using (var serialized = new SerializedObject(roots[0]))
            {
                canvasView = serialized.FindProperty("ui")?.objectReferenceValue as StageMapCanvasView;
            }
            if (canvasView == null || canvasView.gameObject.scene != scene)
                throw new InvalidOperationException("[StageDeckEditUISetup] StageMapSceneRoot.ui에 같은 씬의 StageMapCanvasView가 필요합니다.");
            StageSelectPanelUI selection;
            Canvas canvas;
            using (var serialized = new SerializedObject(canvasView))
            {
                selection = serialized.FindProperty("selection")?.objectReferenceValue as StageSelectPanelUI;
                canvas = serialized.FindProperty("canvas")?.objectReferenceValue as Canvas;
            }
            if (canvas == null || canvas.gameObject != canvasView.gameObject ||
                selection == null || selection.gameObject.scene != scene ||
                selection.transform.parent != canvasView.transform)
                throw new InvalidOperationException("[StageDeckEditUISetup] CanvasView의 canvas와 같은 씬의 자식 selection 연결이 필요합니다.");
            DeckEditUI[] existing = Components<DeckEditUI>(scene);
            if (existing.Length > 1)
                throw new InvalidOperationException("[StageDeckEditUISetup] DeckEditUI가 여러 개라 직접 확인이 필요합니다.");

            bool changed = false;
            DeckEditUI screen;
            if (existing.Length == 0)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, scene);
                instance.transform.SetParent(selection.transform, false);
                screen = instance.GetComponent<DeckEditUI>();
                changed = true;
            }
            else
            {
                screen = existing[0];
            }
            Validate(screen);
            if (screen.transform.parent != selection.transform ||
                screen.GetComponentInParent<Canvas>(true) != canvas)
                throw new InvalidOperationException("[StageDeckEditUISetup] DeckEditUI 루트는 selection의 실제 자식이며 공통 Canvas를 사용해야 합니다.");
            screen.ValidateChildCanvases();

            using (var serialized = new SerializedObject(selection))
            {
                SerializedProperty reference = serialized.FindProperty("deckEditUI")
                    ?? throw new InvalidOperationException("[StageDeckEditUISetup] StageSelectPanelUI에 deckEditUI 칸이 없습니다.");
                if (reference.objectReferenceValue != null && reference.objectReferenceValue != screen)
                    throw new InvalidOperationException("[StageDeckEditUISetup] deckEditUI가 다른 오브젝트를 가리킵니다.");
                if (reference.objectReferenceValue == null)
                {
                    reference.objectReferenceValue = screen;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("[StageDeckEditUISetup] StageSelect를 저장하지 못했습니다.");
            }
            Debug.Log("[StageDeckEditUISetup] StageSelect 덱 편집 UI를 확인했습니다. " +
                (changed ? "없던 화면·참조를 추가했습니다." : "기존 화면·참조를 유지했습니다."));
        }

        private static DeckEditUI EnsurePrefab(TMP_FontAsset font, GameObject slotPrefab)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null)
            {
                DeckEditUI found = existing.GetComponent<DeckEditUI>();
                if (found == null)
                    throw new InvalidOperationException("[StageDeckEditUISetup] 기존 프리팹에 DeckEditUI가 없습니다: " + PrefabPath);
                return found;
            }

            GameObject root = Rect("DeckEditUI", null, new Vector2(1920, 1080), Vector2.zero).gameObject;
            try
            {
                var rootRect = (RectTransform)root.transform;
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;
                DeckEditUI ui = root.AddComponent<DeckEditUI>();

                UnityEngine.UI.Button open = Button("OpenButton", root.transform, new Vector2(240, 72), Vector2.zero);
                Anchor((RectTransform)open.transform, new Vector2(1, 1), new Vector2(-150, -60));
                Label("Label", open.transform, font, "덱 편집", 30, new Vector2(220, 64), Vector2.zero);

                RectTransform panel = Rect("Panel", root.transform, Vector2.zero, Vector2.zero);
                panel.anchorMin = Vector2.zero;
                panel.anchorMax = Vector2.one;
                panel.offsetMin = panel.offsetMax = Vector2.zero;
                // 패널 배경이 맵 클릭을 막습니다.
                Image(panel.gameObject, new Color(.06f, .07f, .1f, .96f), true);

                Label("Title", panel, font, "덱 편집", 48, new Vector2(700, 70), new Vector2(0, 490));
                Label("DeckLabel", panel, font, "덱 (Main · Heavy · Rider · Cannon)", 26, new Vector2(900, 44), new Vector2(0, 410));
                var deckSlots = new DeckEditSlotView[DeckSlots];
                for (int i = 0; i < DeckSlots; i++)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab, panel);
                    instance.name = "DeckSlot" + i;
                    var rect = (RectTransform)instance.transform;
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                    rect.anchoredPosition = new Vector2((i - (DeckSlots - 1) * .5f) * 420f, 100f);
                    deckSlots[i] = instance.GetComponent<DeckEditSlotView>();
                }

                Label("OwnedLabel", panel, font, "미편성 유닛 (같은 병과 슬롯에 드래그)", 26, new Vector2(1000, 44), new Vector2(0, 10));
                GameObject ownedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OwnedSlotPath);
                if (ownedPrefab == null || ownedPrefab.GetComponent<OwnedUnitSlot>() == null)
                    throw new InvalidOperationException("[StageDeckEditUISetup] OwnedUnitSlot 프리팹이 필요합니다: " + OwnedSlotPath);
                RectTransform scrollRoot = Rect("OwnedScrollView", panel, new Vector2(1400, 200), new Vector2(0, -190));
                var scroll = scrollRoot.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
                RectTransform viewport = Rect("Viewport", scrollRoot, Vector2.zero, Vector2.zero);
                viewport.anchorMin = Vector2.zero;
                viewport.anchorMax = Vector2.one;
                Image(viewport.gameObject, Color.white, true);
                viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
                RectTransform content = Rect("Content", viewport, new Vector2(1400, 200), Vector2.zero);
                content.anchorMin = content.anchorMax = content.pivot = new Vector2(0, 1);
                scroll.content = content;
                scroll.viewport = viewport;
                scroll.horizontal = true;
                scroll.vertical = false;

                UnityEngine.UI.Button reevaluate = Button("ReevaluateButton", panel, new Vector2(240, 64), new Vector2(-300, -490));
                Label("Label", reevaluate.transform, font, "다시 평가", 28, new Vector2(220, 56), Vector2.zero);
                UnityEngine.UI.Button confirm = Button("ConfirmButton", panel, new Vector2(240, 64), new Vector2(0, -490));
                Label("Label", confirm.transform, font, "확정", 28, new Vector2(220, 56), Vector2.zero);
                UnityEngine.UI.Button close = Button("CloseButton", panel, new Vector2(240, 64), new Vector2(300, -490));
                Label("Label", close.transform, font, "닫기", 28, new Vector2(220, 56), Vector2.zero);
                // 확정 가능 여부는 화면이 표시 값으로 정합니다. 나머지 버튼은 늘 누를 수 있습니다.
                open.interactable = reevaluate.interactable = close.interactable = true;
                confirm.interactable = false;

                DeckSlotInfoUI slotInfo = CreateSlotInfo(panel, font);
                SetReferences(ui, ("panel", panel.gameObject), ("openButton", open), ("closeButton", close),
                    ("confirmButton", confirm), ("reevaluateButton", reevaluate), ("ownedScrollView", scroll),
                    ("ownedSlotPrefab", ownedPrefab.GetComponent<OwnedUnitSlot>()), ("slotInfoPanel", slotInfo));
                SetArray(ui, "deckSlots", deckSlots);
                AddCombatDeparture(ui, panel, font);
                panel.gameObject.SetActive(false);
                Validate(ui);

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success);
                if (!success || saved == null)
                    throw new InvalidOperationException("[StageDeckEditUISetup] 프리팹을 저장하지 못했습니다.");
                return saved.GetComponent<DeckEditUI>();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void AddCombatDeparture(DeckEditUI ui, RectTransform panel, TMP_FontAsset font)
        {
            UnityEngine.UI.Button start = Button("StartCombatButton", panel, new Vector2(240, 64), new Vector2(0, -490));
            Label("Label", start.transform, font, "출전", 28, new Vector2(220, 56), Vector2.zero);
            start.gameObject.SetActive(false);
            RectTransform modal = Rect("DeckCombatConfirm", panel, Vector2.zero, Vector2.zero);
            modal.anchorMin = Vector2.zero;
            modal.anchorMax = Vector2.one;
            modal.offsetMin = modal.offsetMax = Vector2.zero;
            Image(modal.gameObject, new Color(.06f, .07f, .1f, .96f), true);
            Label("Message", modal, font, "덱에 빈 칸이 있습니다. 이대로 출전하시겠습니까?", 28,
                new Vector2(1100, 100), new Vector2(0, 50));
            UnityEngine.UI.Button accept = Button("AcceptButton", modal, new Vector2(240, 64), new Vector2(-150, -60));
            Label("Label", accept.transform, font, "출전", 28, new Vector2(220, 56), Vector2.zero);
            UnityEngine.UI.Button cancel = Button("CancelButton", modal, new Vector2(240, 64), new Vector2(150, -60));
            Label("Label", cancel.transform, font, "취소", 28, new Vector2(220, 56), Vector2.zero);
            DeckCombatConfirmUI confirmation = modal.gameObject.AddComponent<DeckCombatConfirmUI>();
            SetReferences(confirmation, ("acceptButton", accept), ("cancelButton", cancel));
            SetReferences(ui, ("startCombatButton", start), ("combatConfirmUI", confirmation));
            modal.gameObject.SetActive(false);
        }

        private static DeckSlotInfoUI CreateSlotInfo(Transform parent, TMP_FontAsset font)
        {
            RectTransform rect = Rect("SlotInfoPannel", parent, new Vector2(440, 500), Vector2.zero);
            var image = Image(rect.gameObject, new Color(.12f, .14f, .2f, 1), true);
            var close = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            close.targetGraphic = image;
            TMP_Text description = Label("Description", rect, font, string.Empty, 26, new Vector2(380, 280), new Vector2(0, 70));
            var result = rect.gameObject.AddComponent<DeckSlotInfoUI>();
            SetReferences(result, ("closeButton", close), ("description", description));
            using (var data = new SerializedObject(result))
            {
                SerializedProperty abilities = data.FindProperty("abilitySlots");
                abilities.arraySize = 2;
                for (int i = 0; i < 2; i++)
                {
                    var button = Button("Ability" + (i + 1), rect, new Vector2(80, 80), new Vector2(i == 0 ? -70 : 70, -160));
                    RectTransform iconRect = Rect("Icon", button.transform, new Vector2(70, 70), Vector2.zero);
                    var icon = Image(iconRect.gameObject, Color.white, true);
                    abilities.GetArrayElementAtIndex(i).FindPropertyRelative("button").objectReferenceValue = button;
                    abilities.GetArrayElementAtIndex(i).FindPropertyRelative("icon").objectReferenceValue = icon;
                }
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            rect.gameObject.SetActive(false);
            return result;
        }

        private static void Validate(DeckEditUI ui)
        {
            // 런타임 Prepare는 입력 연결까지 하므로 에디터에서는 같은 필수 참조만 확인합니다.
            using var serialized = new SerializedObject(ui);
            foreach (string field in new[] { "panel", "openButton", "closeButton", "confirmButton", "startCombatButton", "combatConfirmUI", "reevaluateButton", "ownedScrollView", "ownedSlotPrefab", "slotInfoPanel" })
            {
                if (serialized.FindProperty(field)?.objectReferenceValue == null)
                    throw new InvalidOperationException("[StageDeckEditUISetup] 필수 참조가 비었습니다: " + field);
            }
            SerializedProperty deck = serialized.FindProperty("deckSlots");
            if (deck == null || deck.arraySize != DeckSlots)
                throw new InvalidOperationException("[StageDeckEditUISetup] 고정 병과 슬롯 네 칸이 필요합니다.");
            for (int i = 0; i < deck.arraySize; i++)
            {
                var view = deck.GetArrayElementAtIndex(i).objectReferenceValue as DeckEditSlotView;
                if (view == null)
                    throw new InvalidOperationException($"[StageDeckEditUISetup] deckSlots[{i}]에 DeckEditSlotView가 필요합니다.");
                using var slotData = new SerializedObject(view);
                foreach (string field in new[] { "info", "portrait", "categoryIcon", "nameText", "levelText", "healthValue", "attackValue", "defenseValue", "infoButton", "changeButton", "deleteButton", "background", "emptyText" })
                    if (slotData.FindProperty(field)?.objectReferenceValue == null)
                        throw new InvalidOperationException($"[StageDeckEditUISetup] deckSlots[{i}].{field} 참조가 필요합니다.");
            }
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.layer = LayerMask.NameToLayer("UI");
            var rect = (RectTransform)gameObject.transform;
            if (parent != null) rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        private static void Anchor(RectTransform rect, Vector2 anchor, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position;
        }

        private static UnityEngine.UI.Image Image(GameObject target, Color color, bool raycast)
        {
            var image = target.AddComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private static UnityEngine.UI.Button Button(string name, Transform parent, Vector2 size, Vector2 position)
        {
            RectTransform rect = Rect(name, parent, size, position);
            UnityEngine.UI.Image image = Image(rect.gameObject, new Color(.25f, .29f, .36f, 1f), true);
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            // Awake에서 UIInputBinding을 연결하므로 Inspector UnityEvent는 비워 둡니다.
            return button;
        }

        private static TMP_Text Label(string name, Transform parent, TMP_FontAsset font, string value,
            float fontSize, Vector2 size, Vector2 position)
        {
            RectTransform rect = Rect(name, parent, size, position);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        private static void SetReferences(UnityEngine.Object target, params (string field, UnityEngine.Object value)[] references)
        {
            using var serialized = new SerializedObject(target);
            foreach (var reference in references)
            {
                SerializedProperty property = serialized.FindProperty(reference.field)
                    ?? throw new InvalidOperationException("[StageDeckEditUISetup] 직렬화 칸이 없습니다: " + reference.field);
                property.objectReferenceValue = reference.value;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(UnityEngine.Object target, string field, IReadOnlyList<UnityEngine.Object> values)
        {
            using var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field)
                ?? throw new InvalidOperationException("[StageDeckEditUISetup] 직렬화 칸이 없습니다: " + field);
            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static T[] Components<T>(Scene scene) where T : Component
        {
            var result = new List<T>();
            foreach (GameObject root in scene.GetRootGameObjects())
                result.AddRange(root.GetComponentsInChildren<T>(true));
            return result.ToArray();
        }
    }
}
