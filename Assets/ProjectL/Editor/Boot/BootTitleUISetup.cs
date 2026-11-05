using System;
using System.IO;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Boot;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Boot;
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
    /// <summary>기존 Boot 오브젝트를 유지하고 게임 시작 화면과 명시적인 버튼 참조만 연결합니다.</summary>
    public static class BootTitleUISetup
    {
        private const string ScenePath = "Assets/Scenes/Boot.unity";
        private const string FontPath = "Assets/ProjectL/Resources/Font/Hahmlet-Bold SDF.asset";

        [MenuItem("ProjectL/Boot/Ensure Start Button")]
        public static void EnsureStartButton()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("[BootTitleUISetup] 재생을 멈춘 뒤 실행해 주세요.");
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (!opened && scene.isDirty)
                throw new InvalidOperationException("[BootTitleUISetup] Boot 씬의 기존 변경을 먼저 저장해 주세요.");
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null) throw new InvalidOperationException("[BootTitleUISetup] 기존 한글 폰트가 필요합니다: " + FontPath);
            string snapshot = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                ".superpowers/sdd/2026-10-19-boot-start-button/Boot-before.unity");
            Directory.CreateDirectory(Path.GetDirectoryName(snapshot));
            if (!File.Exists(snapshot)) File.Copy(ScenePath, snapshot);
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                GameBootstrap bootstrap = Single<GameBootstrap>(scene);
                TitleUI title = OptionalSingle<TitleUI>(scene);
                if (title == null)
                {
                    var canvasObject = new GameObject("TitleCanvas", typeof(RectTransform), typeof(Canvas),
                        typeof(CanvasScaler), typeof(GraphicRaycaster));
                    SceneManager.MoveGameObjectToScene(canvasObject, scene);
                    canvasObject.layer = LayerMask.NameToLayer("UI");
                    canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                    CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920f, 1080f);
                    scaler.matchWidthOrHeight = .5f;
                    title = canvasObject.AddComponent<TitleUI>();
                    var buttonObject = new GameObject("GameStartButton", typeof(RectTransform), typeof(Image), typeof(Button));
                    buttonObject.layer = canvasObject.layer;
                    buttonObject.transform.SetParent(canvasObject.transform, false);
                    RectTransform rect = (RectTransform)buttonObject.transform;
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                    rect.sizeDelta = new Vector2(320f, 88f);
                    Image image = buttonObject.GetComponent<Image>();
                    image.color = new Color(.16f, .18f, .22f, 1f);
                    Button button = buttonObject.GetComponent<Button>();
                    button.targetGraphic = image;
                    button.interactable = false;
                    var textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                    textObject.layer = canvasObject.layer;
                    textObject.transform.SetParent(buttonObject.transform, false);
                    var textRect = (RectTransform)textObject.transform;
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.offsetMin = textRect.offsetMax = Vector2.zero;
                    var text = textObject.GetComponent<TextMeshProUGUI>();
                    text.font = font;
                    text.text = "게임 시작";
                    text.fontSize = 36f;
                    text.alignment = TextAlignmentOptions.Center;
                    text.color = Color.white;
                    text.raycastTarget = false;
                    using var titleData = new SerializedObject(title);
                    titleData.FindProperty("startButton").objectReferenceValue = button;
                    titleData.ApplyModifiedPropertiesWithoutUndo();
                }
                Button quitButton = null;
                foreach (Button button in title.GetComponentsInChildren<Button>(true))
                {
                    if (button.name != "QuitButton") continue;
                    if (quitButton != null)
                        throw new InvalidOperationException("[BootTitleUISetup] TitleUI 내부의 QuitButton이 중복되어 있습니다.");
                    quitButton = button;
                }
                if (quitButton == null)
                {
                    var quitObject = new GameObject("QuitButton", typeof(RectTransform), typeof(Image), typeof(Button));
                    quitObject.layer = title.gameObject.layer;
                    quitObject.transform.SetParent(title.transform, false);
                    var rect = (RectTransform)quitObject.transform;
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                    rect.anchoredPosition = new Vector2(0f, -120f);
                    rect.sizeDelta = new Vector2(320f, 88f);
                    quitButton = quitObject.GetComponent<Button>();
                    quitButton.targetGraphic = quitObject.GetComponent<Image>();
                    var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                    label.layer = quitObject.layer;
                    label.transform.SetParent(quitObject.transform, false);
                    var labelRect = (RectTransform)label.transform;
                    labelRect.anchorMin = Vector2.zero;
                    labelRect.anchorMax = Vector2.one;
                    labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
                    var text = label.GetComponent<TextMeshProUGUI>();
                    text.font = font;
                    text.text = "게임 종료";
                    text.fontSize = 36f;
                    text.color = Color.black;
                    text.alignment = TextAlignmentOptions.Center;
                    text.raycastTarget = false;
                }
                using var titleBindings = new SerializedObject(title);
                titleBindings.FindProperty("quitButton").objectReferenceValue = quitButton;
                titleBindings.ApplyModifiedPropertiesWithoutUndo();
                using var data = new SerializedObject(bootstrap);
                SerializedProperty reference = data.FindProperty("titleUI");
                if (reference.objectReferenceValue != null && reference.objectReferenceValue != title)
                    throw new InvalidOperationException("[BootTitleUISetup] Bootstrap이 다른 TitleUI를 참조하고 있습니다.");
                reference.objectReferenceValue = title;
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("[BootTitleUISetup] Boot 씬을 저장하지 못했습니다.");
            }
            finally
            {
                if (opened && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static T Single<T>(Scene scene) where T : Component => OptionalSingle<T>(scene)
            ?? throw new InvalidOperationException("[BootTitleUISetup] Boot 씬에 " + typeof(T).Name + " 하나가 필요합니다.");

        private static T OptionalSingle<T>(Scene scene) where T : Component
        {
            T result = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (T component in root.GetComponentsInChildren<T>(true))
                {
                    if (result != null)
                        throw new InvalidOperationException("[BootTitleUISetup] Boot 씬에 " + typeof(T).Name + "이 중복되어 있습니다.");
                    result = component;
                }
            }
            return result;
        }
    }
}
