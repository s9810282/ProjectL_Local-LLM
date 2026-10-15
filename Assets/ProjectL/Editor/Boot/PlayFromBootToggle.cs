using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    /// <summary>
    /// 어느 씬을 열어 둔 채 Play해도 Build Settings 0번 씬(Boot)부터 시작하게 하는 에디터 토글입니다.
    /// 런타임은 Boot 씬의 GameBootstrap이 띄우므로, 다른 씬에서 바로 Play하면 런타임이 없습니다.
    /// </summary>
    /// <remarks>
    /// 켜고 끈 상태는 이 PC의 EditorPrefs에만 저장되며 기본값은 꺼짐입니다. 다른 작업자의 Play 동작은 바뀌지 않습니다.
    /// 시작 씬은 경로를 코드에 적지 않고 Build Settings에서 첫 번째로 켜진 씬(빌드 번호 0)을 씁니다.
    /// Play를 멈추면 Unity가 원래 편집하던 씬으로 돌아갑니다. 게임 코드는 바꾸지 않습니다.
    /// </remarks>
    [InitializeOnLoad]
    internal static class PlayFromBootToggle
    {
        private const string MenuPath = "ProjectL/Play From Boot";
        private const string PrefsKey = "ProjectL.PlayFromBoot";

        static PlayFromBootToggle()
        {
            // 도메인 리로드마다 playModeStartScene이 초기화되므로 다시 적용한다.
            EditorBuildSettings.sceneListChanged += Apply;
            EditorApplication.delayCall += Apply;
        }

        internal static bool IsEnabled
        {
            get => EditorPrefs.GetBool(PrefsKey, false);
            set => EditorPrefs.SetBool(PrefsKey, value);
        }

        [MenuItem(MenuPath, false, 100)]
        private static void Toggle()
        {
            IsEnabled = !IsEnabled;
            Apply();
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, IsEnabled);
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        internal static void Apply()
        {
            if (!IsEnabled)
            {
                EditorSceneManager.playModeStartScene = null;
                return;
            }

            SceneAsset bootScene = FindFirstEnabledBuildScene();
            if (bootScene == null)
                Debug.LogWarning("[PlayFromBootToggle] Build Settings에 켜진 씬이 없어 Play 시작 씬을 지정하지 못했습니다.");

            EditorSceneManager.playModeStartScene = bootScene;
        }

        private static SceneAsset FindFirstEnabledBuildScene()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].enabled)
                    return AssetDatabase.LoadAssetAtPath<SceneAsset>(scenes[i].path);
            }

            return null;
        }
    }
}
