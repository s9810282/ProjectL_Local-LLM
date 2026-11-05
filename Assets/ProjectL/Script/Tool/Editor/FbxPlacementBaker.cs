using System;
using System.IO;
using ProjectL.Tool.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ProjectL.Tool.Editor
{
    /// <summary>일반 씬을 보존하고 맵만 있는 씬에서 비동기 베이크한 뒤 프리팹에 연결합니다.</summary>
    public sealed class FbxPlacementBaker
    {
        public static FbxPlacementBaker Current { get; private set; }
        public static bool IsRunning => Current != null;
        public float Progress => Lightmapping.isRunning ? Lightmapping.buildProgress : 0;
        public GameObject Root { get; private set; }
        public string OutputFolder { get; private set; }
        private SceneSetup[] _previousScenes;
        private Scene _bakeScene;
        private GameObject _bakeRoot;
        private string _prefabPath;
        private string _fingerprint;
        private bool _completed;
        private bool _cancelled;
        private bool _finishing;
        private bool _reloadLocked;
        private int _idleFrames;
        private UnityEditor.Tool _previousTool;
        private Lightmapping.BakeOnSceneLoadMode _previousBakeOnLoad;
        private Action<bool, string> _onFinished;

        public static bool Start(GameObject root, string prefabPath, FbxPlacementBakeSettings settings,
            Action<bool, string> onFinished)
        {
            if (IsRunning || Lightmapping.isRunning) throw new InvalidOperationException("이미 다른 베이크가 진행 중입니다.");
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("재생 또는 컴파일 중에는 베이크할 수 없습니다.");
            FbxPlacementBakeData.Validate(root);
            string path = (prefabPath ?? "").Replace('\\', '/');
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || path.Contains("/../") ||
                path.Contains("/./") || !path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) ||
                !AssetDatabase.IsValidFolder(Path.GetDirectoryName(path)?.Replace('\\', '/')))
                throw new ArgumentException("Assets 폴더 안의 맵 프리팹 저장 경로를 지정하세요.", nameof(prefabPath));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            MapBakedLighting.ValidateSceneMode(settings.directionalMode);
            LightingSettings lightingSettings = settings.CreateLightingSettings();
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Object.DestroyImmediate(lightingSettings);
                return false;
            }
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded || (!scene.isDirty && !string.IsNullOrEmpty(scene.path))) continue;
                Object.DestroyImmediate(lightingSettings);
                throw new InvalidOperationException("원래 씬으로 돌아오려면 열려 있는 씬을 모두 저장해야 합니다. 씬을 저장한 뒤 다시 베이크하세요.");
            }
            var job = new FbxPlacementBaker
            {
                Root = root, _prefabPath = path, _onFinished = onFinished,
                _previousScenes = EditorSceneManager.GetSceneManagerSetup(),
                _previousTool = Tools.current, _previousBakeOnLoad = Lightmapping.bakeOnSceneLoad
            };
            Current = job;
            try
            {
                EditorApplication.LockReloadAssemblies();
                job._reloadLocked = true;
                Lightmapping.bakeOnSceneLoad = Lightmapping.BakeOnSceneLoadMode.Never;
                Tools.current = UnityEditor.Tool.View;
                root.GetComponent<MapBakedLighting>()?.Release();
                job._fingerprint = FbxPlacementBakeData.Fingerprint(root);
                string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
                string folderName = Path.GetFileNameWithoutExtension(path) + "_Lighting";
                string container = parent + "/" + folderName;
                if (!AssetDatabase.IsValidFolder(container)) AssetDatabase.CreateFolder(parent, folderName);
                string runName = "Bake_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N").Substring(0, 6);
                AssetDatabase.CreateFolder(container, runName);
                job.OutputFolder = container + "/" + runName;
                AssetDatabase.CreateAsset(lightingSettings, job.OutputFolder + "/LightingSettings.lighting");
                job._bakeScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(job._bakeScene);
                job._bakeRoot = Object.Instantiate(root);
                job._bakeRoot.name = root.name;
                MapBakedLighting oldBinding = job._bakeRoot.GetComponent<MapBakedLighting>();
                if (oldBinding != null) Object.DestroyImmediate(oldBinding);
                // 볼륨은 화면 후처리용이며 GI 베이크에는 필요하지 않습니다.
                foreach (Volume volume in job._bakeRoot.GetComponentsInChildren<Volume>(true)) Object.DestroyImmediate(volume);
                Lightmapping.lightingSettings = lightingSettings;
                RenderSettings.skybox = null;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = settings.ambientColor;
                RenderSettings.ambientIntensity = 1;
                string scenePath = job.OutputFolder + "/Bake.unity";
                if (!EditorSceneManager.SaveScene(job._bakeScene, scenePath))
                    throw new InvalidOperationException("전용 베이크 씬을 저장하지 못했습니다.");
                // 다른 씬이 광원·차폐물로 섞이지 않도록 저장된 베이크 씬만 엽니다.
                EditorSceneManager.CloseScene(job._bakeScene, true);
                job._bakeScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                job._bakeRoot = job._bakeScene.GetRootGameObjects()[0];
                Lightmapping.bakeCompleted += job.OnCompleted;
                Lightmapping.bakeCancelled += job.OnCancelled;
                EditorApplication.update += job.Update;
                EditorApplication.quitting += job.Cancel;
                EditorApplication.playModeStateChanged += job.OnPlayModeChanged;
                if (!Lightmapping.BakeAsync()) throw new InvalidOperationException("Unity가 베이크를 시작하지 못했습니다. Console을 확인하세요.");
                return true;
            }
            catch
            {
                if (!EditorUtility.IsPersistent(lightingSettings)) Object.DestroyImmediate(lightingSettings);
                job._cancelled = true;
                job.Finish(false, "베이크를 시작하지 못했습니다.", false);
                throw;
            }
        }

        private void OnCompleted() => _completed = true;
        private void OnCancelled() => _cancelled = true;

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;
            EditorApplication.isPlaying = false;
            Cancel();
        }

        public void Cancel()
        {
            if (_finishing) return;
            _cancelled = true;
            if (Lightmapping.isRunning) Lightmapping.Cancel();
            if (!Lightmapping.isRunning) Finish(false, "베이크를 취소했습니다. 이전 결과를 유지합니다.");
        }

        private void Update()
        {
            if (_finishing) return;
            if (Lightmapping.isRunning) { _idleFrames = 0; SceneView.RepaintAll(); return; }
            if (_cancelled) { Finish(false, "베이크를 취소했습니다. 이전 결과를 유지합니다."); return; }
            if (!_completed)
            {
                if (++_idleFrames > 10) Finish(false, "베이크가 완료되지 않았습니다. Unity Console을 확인하세요.");
                return;
            }
            Finish(true, "");
        }

        private void Finish(bool success, string message, bool notify = true)
        {
            if (_finishing) return;
            _finishing = true;
            FbxPlacementBakeData.Result result = null;
            Lightmapping.bakeCompleted -= OnCompleted;
            Lightmapping.bakeCancelled -= OnCancelled;
            EditorApplication.update -= Update;
            EditorApplication.quitting -= Cancel;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            try
            {
                if (success)
                {
                    if (!FbxPlacementWorkspace.IsWorkspaceRoot(Root) || FbxPlacementBakeData.Fingerprint(Root) != _fingerprint)
                        throw new InvalidOperationException("베이크 중 맵 배치나 라이트가 바뀌었습니다. 다시 베이크하세요.");
                    AssetDatabase.SaveAssets();
                    result = FbxPlacementBakeData.Capture(Root, _bakeRoot, LightmapSettings.lightmaps, LightmapSettings.lightmapsMode);
                    if (!EditorSceneManager.SaveScene(_bakeScene)) throw new InvalidOperationException("베이크 씬을 저장하지 못했습니다.");
                }
            }
            catch (Exception exception) { success = false; message = exception.Message; }
            finally
            {
                try
                {
                    if (_previousScenes != null && _previousScenes.Length > 0)
                        EditorSceneManager.RestoreSceneManagerSetup(_previousScenes);
                    else if (_bakeScene.IsValid() && _bakeScene.isLoaded)
                        EditorSceneManager.CloseScene(_bakeScene, true);
                    if (success && result != null)
                    {
                        MapBakedLightingDataSO data = result.CreateAsset(Root, OutputFolder + "/BakedLighting.asset");
                        MapAssembler.Apply(Root, data);
                        FbxPlacementMapAssets.Save(Root, _prefabPath);
                        message = "베이크 완료 · 라이트맵 " + result.atlases.Length + "장 / 메시 " + result.surfaces.Length + "개\n맵 SO와 베이크 정보 SO에 저장했습니다.";
                    }
                    else if (Root != null) Root.GetComponent<MapBakedLighting>()?.Apply();
                }
                catch (Exception exception)
                {
                    success = false;
                    message = "베이크 복구/저장 실패: " + exception.Message;
                }
                finally
                {
                    Lightmapping.bakeOnSceneLoad = _previousBakeOnLoad;
                    Tools.current = _previousTool;
                    Current = null;
                    if (_reloadLocked) EditorApplication.UnlockReloadAssemblies();
                    SceneView.RepaintAll();
                }
            }
            if (notify) _onFinished?.Invoke(success, message);
        }
    }
}
