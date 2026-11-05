using System;
using System.Collections.Generic;
using System.IO;
using ProjectL.Framework.SystemBase;
using ProjectL.Render.Shader.View.CustomShader.UI.Editor;
using ProjectL.Render.Shader.View.CustomShader.UI.Integration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectL.Render.Shader.View.CustomShader.UI.Tests.Editor
{
    /// <summary>편집 씬을 저장/교체하지 않고 별도 Play 시작 씬으로 실제 비동기 로딩을 검사한다.</summary>
    [InitializeOnLoad]
    internal static class JourneyLoadingPlayVerification
    {
        private const string Key = "ProjectL.JourneyLoadingVerification.";
        private const string Folder = JourneyPackageBuilder.Root + "/Verification";
        private const string SceneA = Folder + "/LoadingProbeA.unity";
        private const string SceneB = Folder + "/LoadingProbeB.unity";
        private static JourneyLoadingFixture _fixture;
        private static int _cycle;
        private static int _target;
        private static int _lastFrame = -1;
        private static int _screenId;
        private static double _started;
        private static readonly List<string> Report = new List<string>();

        [Serializable]
        private sealed class SettingsSnapshot
        {
            public string[] paths;
            public bool[] enabled;
        }

        static JourneyLoadingPlayVerification()
        {
            EditorApplication.playModeStateChanged += OnPlayState;
            EditorApplication.update += Update;
        }

        [MenuItem("ProjectL/Render/UI/Verify Journey Actual Scene Loading %#&j")]
        private static void Begin()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(Key + "active", false))
                throw new InvalidOperationException("재생/로딩 검증이 진행 중입니다.");
            JourneyLoadingInstaller.Install();
            var original = EditorBuildSettings.scenes;
            var snapshot = new SettingsSnapshot { paths = new string[original.Length], enabled = new bool[original.Length] };
            for (int i = 0; i < original.Length; i++)
            {
                snapshot.paths[i] = original[i].path;
                snapshot.enabled[i] = original[i].enabled;
            }
            SessionState.SetString(Key + "build", JsonUtility.ToJson(snapshot));
            SessionState.SetString(Key + "start", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            SessionState.SetBool(Key + "boot", EditorPrefs.GetBool("ProjectL.PlayFromBoot", false));
            SessionState.SetBool(Key + "active", true);
            try
            {
                BuildScene(SceneA, new Color(0.04f, 0.13f, 0.28f));
                BuildScene(SceneB, new Color(0.19f, 0.06f, 0.12f));
                var scenes = new List<EditorBuildSettingsScene>(original)
                {
                    new EditorBuildSettingsScene(SceneA, true), new EditorBuildSettingsScene(SceneB, true)
                };
                EditorPrefs.SetBool("ProjectL.PlayFromBoot", false);
                EditorBuildSettings.scenes = scenes.ToArray();
                EditorApplication.delayCall += () =>
                {
                    EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SceneA);
                    EditorApplication.isPlaying = true;
                };
            }
            catch
            {
                Restore();
                throw;
            }
        }

        private static void BuildScene(string path, Color color)
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                var cameraObject = new GameObject("Probe Camera", typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = color;
                camera.transform.position = new Vector3(0, 0, -5);
                if (!EditorSceneManager.SaveScene(scene, path))
                    throw new InvalidOperationException("검증 씬 저장 실패: " + path);
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void OnPlayState(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Key + "active", false)) return;
            if (state == PlayModeStateChange.ExitingPlayMode && _fixture != null)
            {
                File.WriteAllText(Folder + "/Loading-PlayMode.txt", "CANCELLED: 검증 도중 재생 종료\n");
                _fixture.Dispose();
                _fixture = null;
            }
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                Restore();
                return;
            }
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            try
            {
                if (SceneManager.GetActiveScene().path != SceneA)
                    throw new InvalidOperationException("검증 시작 씬이 다릅니다. 사용자 씬에서 로딩을 실행하지 않습니다.");
                Report.Clear();
                Report.Add("Unity " + Application.unityVersion + " / 실제 Play 비동기 씬 로드");
                _started = EditorApplication.timeSinceStartup;
                _cycle = 0;
                _fixture = new JourneyLoadingFixture(true);
                _screenId = _fixture.Screen.GetInstanceID();
                var nodes = _fixture.Context.LodingRegisterService;
                foreach (string path in new[] { SceneA, SceneB })
                {
                    int index = SceneUtility.GetBuildIndexByScenePath(path);
                    nodes.RegisterSceneEnterLoaded(index, new ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem.LodingNode(-100,
                        () => ((ISystemBindScene)_fixture.UI).BindScene()));
                }
                nodes.RegisterBeforeLoad(new ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem.LodingNode(0, () =>
                {
                    Require(_fixture.Screen.IsCovered, "씬 교체 전에 모래/여정 표시 완료");
                    ScreenCapture.CaptureScreenshot(Folder + "/Loading-cycle-" + _cycle + "-covered.png");
                }));
                nodes.RegisterAfterLoad(new ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem.LodingNode(0, () =>
                {
                    Require(_fixture.Screen.GetInstanceID() == _screenId && _fixture.Screen.gameObject.activeSelf, "씬 재바인딩 뒤 동일 UI 유지");
                    Require(UnityEngine.Object.FindObjectsByType<SandstormLoadingUI>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 1, "Loading UI 중복 없음");
                }));
                RequestNext();
            }
            catch (Exception exception) { Fail(exception); }
        }

        private static void Update()
        {
            if (_fixture == null || !EditorApplication.isPlaying || _lastFrame == Time.frameCount) return;
            _lastFrame = Time.frameCount;
            try
            {
                if (EditorApplication.timeSinceStartup - _started > 35)
                    throw new TimeoutException("로딩 재생 검증 시간 초과");
                _fixture.Tick();
                if (_fixture.Loading.IsLoading) return;
                Require(_fixture.Loading.CurrentSceneIndex == _target, "요청 씬 진입 완료");
                Require(!_fixture.Screen.gameObject.activeSelf && _fixture.Visual.IsHidden, "걷어내기 뒤 UI/포인터 차단 해제");
                Report.Add("PASS cycle " + _cycle + ", scene " + _target + ", timeScale " + Time.timeScale);
                if (++_cycle < 3) RequestNext();
                else
                {
                    var image = new Texture2D(2, 2);
                    try
                    {
                        image.LoadImage(File.ReadAllBytes(Folder + "/Loading-cycle-0-covered.png"));
                        Color corner = image.GetPixel(10, 10);
                        Require(corner.r > 0.4f && corner.r > corner.b + 0.15f, "완전 가림 캡처의 화면 모서리까지 모래 표시");
                    }
                    finally { UnityEngine.Object.Destroy(image); }
                    File.WriteAllText(Folder + "/Loading-PlayMode.txt", string.Join("\n", Report) + "\nPASS: 다른 씬 왕복 + 동일 씬, timeScale 0, 동일 Global UI 재바인딩\n");
                    Debug.Log("[JourneyUI] 실제 씬 로딩 3회 재생 검증 통과.");
                    Stop();
                }
            }
            catch (Exception exception) { Fail(exception); }
        }

        private static void RequestNext()
        {
            Time.timeScale = _cycle == 1 ? 0f : 1f;
            _target = SceneUtility.GetBuildIndexByScenePath(_cycle == 0 ? SceneB : SceneA);
            _fixture.Loading.Load(_target);
        }

        private static void Require(bool value, string description)
        {
            if (!value) throw new InvalidOperationException(description);
        }

        private static void Fail(Exception exception)
        {
            File.WriteAllText(Folder + "/Loading-PlayMode.txt", "FAIL\n" + exception);
            Debug.LogException(exception);
            Stop();
        }

        private static void Stop()
        {
            try { _fixture?.Dispose(); }
            finally
            {
                _fixture = null;
                Time.timeScale = 1f;
                EditorApplication.isPlaying = false;
            }
        }

        private static void Restore()
        {
            var snapshot = JsonUtility.FromJson<SettingsSnapshot>(SessionState.GetString(Key + "build", ""));
            var scenes = new EditorBuildSettingsScene[snapshot.paths.Length];
            for (int i = 0; i < scenes.Length; i++) scenes[i] = new EditorBuildSettingsScene(snapshot.paths[i], snapshot.enabled[i]);
            EditorBuildSettings.scenes = scenes;
            EditorPrefs.SetBool("ProjectL.PlayFromBoot", SessionState.GetBool(Key + "boot", false));
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(Key + "start", ""));
            SessionState.EraseBool(Key + "active");
        }
    }
}
