using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev.TraitPreview;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev.TraitPreview.Scenario;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    /// <summary>도메인 리로드 동안에도 테스트 실행의 시작 씬 소유권과 원래 값을 보존합니다.</summary>
    [InitializeOnLoad]
    internal static class PreviewPlayStartScene
    {
        private const string ActiveKey = "ProjectL.TraitPreview.StartScene";
        private const string PreviousKey = "ProjectL.TraitPreview.PreviousStartScene";
        private const string PendingKey = "ProjectL.TraitPreview.PendingInput";
        private const string ErrorKey = "ProjectL.TraitPreview.StartError";
        private const string ScenarioKey = "ProjectL.TraitPreview.PendingScenario";
        internal static bool IsActive => !string.IsNullOrEmpty(SessionState.GetString(ActiveKey, ""));
        internal static string Error => SessionState.GetString(ErrorKey, "");

        static PreviewPlayStartScene()
        {
            // Game View 최대화로 입력 창이 비활성화돼도 첫 재생 요청은 계속 전달합니다.
            EditorApplication.update += DispatchPendingPlay;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                    Restore();
            };
            EditorApplication.delayCall += () =>
            {
                if (IsActive && !EditorApplication.isPlayingOrWillChangePlaymode)
                    Restore();
            };
        }

        internal static void BeginPlay(TraitActivationPreviewInput input, string path) => BeginJson(JsonUtility.ToJson(input ?? throw new ArgumentNullException(nameof(input))), false, path);
        internal static void BeginPlay(TraitPreviewScenarioInput input, string path) => BeginJson(JsonUtility.ToJson(input ?? throw new ArgumentNullException(nameof(input))), true, path);
        private static void BeginJson(string json, bool scenario, string path)
        {
            try
            {
                Acquire(path);
                SessionState.SetString(PendingKey, json);
                SessionState.SetBool(ScenarioKey, scenario);
                EditorApplication.isPlaying = true;
            }
            catch
            {
                CancelPending();
                Restore();
                throw;
            }
        }

        internal static void CancelPending()
        {
            SessionState.EraseString(PendingKey);
            SessionState.EraseBool(ScenarioKey);
        }

        internal static void CloseAfterLayout()
        {
            // OnDestroy 시점에는 MaximizePrepare가 아직 진행 중일 수 있습니다.
            // 레이아웃 완료 뒤 재생 창 복원/최대화를 확인하고 실제 사용자 닫기만 종료합니다.
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlaying && (EditorWindow.HasOpenInstances<TraitActivationPreviewWindow>() || Array.Exists(Resources.FindObjectsOfTypeAll<EditorWindow>(), window => window.maximized)))
                    return;
                CancelPending();
                TraitActivationPreviewBridge.Host?.Shutdown();
                if (IsActive && EditorApplication.isPlaying)
                    EditorApplication.isPlaying = false;
                else
                    Restore();
            };
        }

        internal static void ClearError() => SessionState.EraseString(ErrorKey);
        internal static void ReportError(Exception error)
        {
            SessionState.SetString(ErrorKey, error.Message);
            Debug.LogException(error);
        }

        private static void DispatchPendingPlay()
        {
            if (!IsActive || !EditorApplication.isPlaying)
                return;
            string pending = SessionState.GetString(PendingKey, "");
            TraitActivationPreviewHost host = TraitActivationPreviewBridge.Host;
            if (pending.Length == 0 || host == null)
                return;
            // Play 중 에셋 조회가 다시 Editor 콜백을 불러도 같은 요청은 한 번만 소비합니다.
            bool scenario = SessionState.GetBool(ScenarioKey, false);
            CancelPending();
            try
            {
                if (scenario)
                    host.Play(JsonUtility.FromJson<TraitPreviewScenarioInput>(pending));
                else
                    host.Play(JsonUtility.FromJson<TraitActivationPreviewInput>(pending));
            }
            catch (Exception error)
            {
                ReportError(error);
            }
        }

        internal static void Acquire(string path)
        {
            if (!IsActive)
                SessionState.SetString(PreviousKey, AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            SessionState.SetString(ActiveKey, path);
            Apply();
        }

        internal static void Apply()
        {
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(ActiveKey, ""));
        }

        internal static void Restore()
        {
            CancelPending();
            if (!IsActive)
                return;
            string path = SessionState.GetString(PreviousKey, "");
            SessionState.EraseString(ActiveKey);
            SessionState.EraseString(PreviousKey);
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
        }
    }
}
