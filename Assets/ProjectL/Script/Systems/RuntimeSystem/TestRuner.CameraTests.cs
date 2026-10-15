using System;
using System.Collections;
using Cinemachine;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraCatalogSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraEffectSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectL.Systems
{
    public sealed partial class TestRuner
    {
        public enum CameraTest
        {
            MovePlayCamera,
            ResetPlayPose,
            ZoomIn,
            ResetZoom,
            SwitchToCinematic,
            SwitchToPlay,
            ResetAll,
            Shake,
            ZoomAndShake,
            ShakeBurst,
            ReplaceZoom,
            DisposeDuringEffects
        }

        [Header("Camera Event Tests")]
        [SerializeField] private string playCameraName = "PlayCamera";
        [SerializeField] private string cinematicCameraName = "CinematicCamera";
        [SerializeField] private bool cameraTestsEnabled = true;
        [SerializeField] private bool showCameraTestPanel = true;
        [SerializeField] private bool enableCameraWasd = true;
        [SerializeField, Min(0f)] private float cameraMoveSpeed = 6f;
        [SerializeField] private Vector3 cameraMoveStep = new Vector3(2f, 0f, 0f);
        [SerializeField, Range(0.1f, 1f)] private float cameraZoomFactor = 0.65f;
        [SerializeField, Min(0f)] private float cameraZoomDuration = 0.35f;
        [SerializeField] private Vector3 cameraShakeVelocity = new Vector3(0f, -0.35f, 0f);
        [SerializeField, Min(0.01f)] private float cameraShakeDuration = 0.6f;
        [SerializeField] private int cameraShakeChannel = 1;
        [SerializeField] private CinemachineImpulseDefinition.ImpulseShapes cameraShakeShape =
            CinemachineImpulseDefinition.ImpulseShapes.Explosion;
        [SerializeField, Range(1, 10)] private int cameraShakeBurstCount = 3;
        [SerializeField] private Vector3 cinematicTestPosition = new Vector3(-4.5f, 8f, -4.5f);
        [SerializeField] private Vector3 cinematicTestEuler = new Vector3(50f, 45f, 0f);
        [SerializeField] private CameraTest selectedCameraTest = CameraTest.ZoomIn;
        [SerializeField, Min(0.1f)] private float cameraTestStepDelay = 1.25f;
        [SerializeField] private CameraTest[] cameraTestList =
        {
            CameraTest.MovePlayCamera, CameraTest.ResetPlayPose,
            CameraTest.ZoomIn, CameraTest.ResetZoom, CameraTest.Shake,
            CameraTest.ZoomAndShake, CameraTest.ResetZoom,
            CameraTest.ShakeBurst, CameraTest.ReplaceZoom, CameraTest.ResetZoom,
            CameraTest.DisposeDuringEffects,
            CameraTest.SwitchToCinematic,
            CameraTest.ZoomAndShake, CameraTest.ResetZoom, CameraTest.SwitchToPlay, CameraTest.ResetAll
        };

        private ICameraCatalogService catalog;
        private ICameraEffectService cameraEffects;
        private ICameraCatalogHandle playHandle;
        private ICameraCatalogHandle cinematicHandle;
        private CinemachineVirtualCamera playTestCamera;
        private CinemachineVirtualCamera cinematicTestCamera;
        private bool cameraTestAvailable;
        private string selectedCameraName;
        private LensSettings initialPlayLens;
        private LensSettings initialCinematicLens;
        private Coroutine cameraTestRoutine;
        private Coroutine cameraLifecycleRoutine;
        private Vector3 initialPlayPosition;
        private Quaternion initialPlayRotation;
        private string cameraTestStatus = "Ready";
        private string lastCameraEventData = "None";

        private bool CameraTestsReady => isRunning && cameraTestsEnabled && cameraTestAvailable &&
            catalog != null && cameraEffects != null && playTestCamera != null && cinematicTestCamera != null;

        private void InitializeCameraTests(IGlobalSystemGroupService global)
        {
            catalog = global.RenderServiceCommand.CoreGroup.CameraGroup.CameraCatalogSystem;
            cameraEffects = global.RenderServiceCommand.CoreGroup.CameraGroup.CameraEffectSystem;
            cameraTestAvailable = false;
            if (!cameraTestsEnabled) return;
            playHandle = catalog.GetHandle(playCameraName);
            cinematicHandle = catalog.GetHandle(cinematicCameraName);
            // 테스트의 현재값/원본 읽기만 직접 수행하고, 변경은 핸들에 struct를 전달한다.
            var cameras = UnityEngine.Object.FindObjectsByType<CinemachineVirtualCamera>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var camera in cameras)
            {
                if (camera.gameObject.name == playCameraName) playTestCamera = camera;
                if (camera.gameObject.name == cinematicCameraName) cinematicTestCamera = camera;
            }
            var play = playTestCamera;
            var cinematic = cinematicTestCamera;
            if (play == null || cinematic == null)
                throw new InvalidOperationException("Camera test objects were not found in the loaded scene.");
            initialPlayPosition = play.transform.position;
            initialPlayRotation = play.transform.rotation;
            initialPlayLens = play.m_Lens;
            initialCinematicLens = cinematic.m_Lens;
            cameraTestAvailable = true;
            SwitchCameraTest(playCameraName);
            cameraTestStatus = "Camera events ready. WASD: XZ movement";
        }

        private void SwitchCameraTest(string objectName)
        {
            var handle = objectName == playCameraName ? playHandle : cinematicHandle;
            var camera = objectName == playCameraName ? playTestCamera : cinematicTestCamera;
            var data = new CameraCatalogData(camera) { Active = true };
            handle.Customize(data);
            playHandle.SetPriority(0);
            cinematicHandle.SetPriority(0);
            handle.SetPriority(10);
            selectedCameraName = objectName;
        }

        private void SetCameraTestPose(CinemachineVirtualCamera camera, Vector3 position, Quaternion rotation)
        {
            var handle = camera == playTestCamera ? playHandle : cinematicHandle;
            var data = new CameraCatalogData(camera) { Position = position, Rotation = rotation };
            handle.Customize(data);
        }

        private void ResetCameraTestZoom(CinemachineVirtualCamera camera)
        {
            var handle = camera == playTestCamera ? playHandle : cinematicHandle;
            var lens = camera == playTestCamera ? initialPlayLens : initialCinematicLens;
            var data = new CameraCatalogData(camera) { Lens = lens };
            handle.Customize(data);
            DispatchCameraEvent(new CameraZoomData
            {
                CameraName = camera.gameObject.name,
                To = lens.Orthographic ? lens.OrthographicSize : lens.FieldOfView
            });
        }

        private void UpdateCameraTests()
        {
            if (!CameraTestsReady || !enableCameraWasd || cameraTestRoutine != null || cameraLifecycleRoutine != null ||
                selectedCameraName != playCameraName)
                return;
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            var direction = new Vector3(
                (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
                0f,
                (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f));
            if (direction.sqrMagnitude == 0f) return;
            var camera = playTestCamera;
            SetCameraTestPose(camera,
                camera.transform.position + direction.normalized * cameraMoveSpeed * Time.deltaTime,
                initialPlayRotation);
        }

        [ContextMenu("Camera/Run Selected Test")]
        public void RunSelectedCameraTest() => ExecuteCameraTest(selectedCameraTest);

        [ContextMenu("Camera/Zoom In")]
        public void TestCameraZoomIn() => ExecuteCameraTest(CameraTest.ZoomIn);

        [ContextMenu("Camera/Shake")]
        public void TestCameraShake() => ExecuteCameraTest(CameraTest.Shake);

        [ContextMenu("Camera/Zoom And Shake")]
        public void TestCameraZoomAndShake() => ExecuteCameraTest(CameraTest.ZoomAndShake);

        [ContextMenu("Camera/Shake Burst")]
        public void TestCameraShakeBurst() => ExecuteCameraTest(CameraTest.ShakeBurst);

        [ContextMenu("Camera/Replace Running Zoom")]
        public void TestCameraReplaceZoom() => ExecuteCameraTest(CameraTest.ReplaceZoom);

        [ContextMenu("Camera/Dispose During Effects")]
        public void TestCameraDisposeEffects() => ExecuteCameraTest(CameraTest.DisposeDuringEffects);

        [ContextMenu("Camera/Reset Zoom")]
        public void TestCameraResetZoom() => ExecuteCameraTest(CameraTest.ResetZoom);

        [ContextMenu("Camera/Switch To Cinematic")]
        public void TestCameraSwitchToCinematic() => ExecuteCameraTest(CameraTest.SwitchToCinematic);

        [ContextMenu("Camera/Switch To Play")]
        public void TestCameraSwitchToPlay() => ExecuteCameraTest(CameraTest.SwitchToPlay);

        [ContextMenu("Camera/Move Play Camera")]
        public void TestCameraMovePlay() => ExecuteCameraTest(CameraTest.MovePlayCamera);

        [ContextMenu("Camera/Reset All")]
        public void TestCameraResetAll()
        {
            StopCameraTestList();
            ExecuteCameraTest(CameraTest.ResetAll);
        }

        public void ExecuteCameraTest(CameraTest test)
        {
            if (!Application.isPlaying || !CameraTestsReady)
            {
                Debug.LogWarning("[TestRuner] Enter Play Mode with a collected camera catalog before testing.", this);
                return;
            }
            try
            {
                if (cameraLifecycleRoutine != null)
                {
                    Debug.LogWarning("[TestRuner] Wait for the Dispose test, or stop the test list first.", this);
                    return;
                }
                switch (test)
                {
                    case CameraTest.MovePlayCamera:
                        var play = playTestCamera;
                        var step = cameraMoveStep;
                        step.y = 0f;
                        SetCameraTestPose(play, play.transform.position + step, initialPlayRotation);
                        break;
                    case CameraTest.ResetPlayPose:
                        SetCameraTestPose(playTestCamera, initialPlayPosition, initialPlayRotation);
                        break;
                    case CameraTest.ZoomIn:
                        StartCameraZoom();
                        break;
                    case CameraTest.Shake:
                        DispatchCameraShake(cameraShakeDuration);
                        break;
                    case CameraTest.ZoomAndShake:
                        StartCameraZoom();
                        DispatchCameraShake(cameraShakeDuration);
                        break;
                    case CameraTest.ShakeBurst:
                        for (int i = 0; i < Mathf.Clamp(cameraShakeBurstCount, 1, 10); i++)
                            DispatchCameraShake(cameraShakeDuration);
                        break;
                    case CameraTest.ReplaceZoom:
                        StartCameraZoom();
                        var selected = selectedCameraName == playCameraName ? playTestCamera : cinematicTestCamera;
                        var baseline = selected == playTestCamera ? initialPlayLens : initialCinematicLens;
                        float baseZoom = baseline.Orthographic ? baseline.OrthographicSize : baseline.FieldOfView;
                        DispatchCameraEvent(new CameraZoomData
                        {
                            CameraName = selectedCameraName,
                            From = CurrentCameraZoom(selected),
                            To = baseline.Orthographic ? Mathf.Max(0.01f, baseZoom * 0.85f)
                                : Mathf.Clamp(baseZoom * 0.85f, 1f, 179f),
                            Duration = Mathf.Max(0.1f, cameraZoomDuration)
                        });
                        break;
                    case CameraTest.DisposeDuringEffects:
                        cameraLifecycleRoutine = StartCoroutine(RunCameraDisposeTest());
                        break;
                    case CameraTest.ResetZoom:
                        CancelCameraZoom();
                        ResetCameraTestZoom(selectedCameraName == playCameraName ? playTestCamera : cinematicTestCamera);
                        break;
                    case CameraTest.SwitchToCinematic:
                        SetCameraTestPose(cinematicTestCamera,
                            cinematicTestPosition, Quaternion.Euler(cinematicTestEuler));
                        SwitchCameraTest(cinematicCameraName);
                        break;
                    case CameraTest.SwitchToPlay:
                        SwitchCameraTest(playCameraName);
                        break;
                    case CameraTest.ResetAll:
                        ((ISystemBindScene)cameraEffects).BindScene();
                        CancelCameraZoom();
                        ResetCameraTestZoom(playTestCamera);
                        ResetCameraTestZoom(cinematicTestCamera);
                        SetCameraTestPose(playTestCamera, initialPlayPosition, initialPlayRotation);
                        SwitchCameraTest(playCameraName);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(test));
                }
                cameraTestStatus = test.ToString();
                Debug.Log($"[TestRuner] Camera test: {test}", this);
            }
            catch (Exception exception)
            {
                cameraTestStatus = exception.Message;
                Debug.LogException(exception, this);
                StopCameraTestList();
                CancelCameraZoom();
            }
        }

        private void StartCameraZoom()
        {
            CancelCameraZoom();
            var camera = selectedCameraName == playCameraName ? playTestCamera : cinematicTestCamera;
            ResetCameraTestZoom(camera);
            var lens = camera.m_Lens;
            bool orthographic = lens.Orthographic;
            float from = orthographic ? lens.OrthographicSize : lens.FieldOfView;
            float to = orthographic ? Mathf.Max(0.01f, from * cameraZoomFactor)
                : Mathf.Clamp(from * cameraZoomFactor, 1f, 179f);
            DispatchCameraEvent(new CameraZoomData
            {
                CameraName = selectedCameraName,
                From = from,
                To = to,
                Duration = cameraZoomDuration
            });
        }

        private void CancelCameraZoom()
        {
            if (!CameraTestsReady || string.IsNullOrEmpty(selectedCameraName)) return;
            var camera = selectedCameraName == playCameraName ? playTestCamera : cinematicTestCamera;
            if (camera == null) return;
            var lens = camera.m_Lens;
            float value = lens.Orthographic ? lens.OrthographicSize : lens.FieldOfView;
            // 같은 카메라에 즉시 적용 요청을 보내 진행 중인 줌을 대체한다.
            DispatchCameraEvent(new CameraZoomData { CameraName = selectedCameraName, To = value });
        }

        private void DispatchCameraEvent(ICameraEventData data)
        {
            cameraEffects.Dispatch(data);
            lastCameraEventData = data.GetType().Name;
        }

        private void DispatchCameraShake(float duration)
        {
            var camera = selectedCameraName == playCameraName ? playTestCamera : cinematicTestCamera;
            var listener = camera.GetComponent<CinemachineImpulseListener>();
            if (listener == null || !listener.enabled || listener.m_Gain == 0f ||
                (listener.m_ChannelMask & cameraShakeChannel) == 0)
                throw new InvalidOperationException("Selected camera needs an enabled ImpulseListener with matching channel and nonzero gain.");
            DispatchCameraEvent(new CameraShakeData
            {
                Position = camera.transform.position,
                Velocity = cameraShakeVelocity,
                Duration = duration,
                Channel = cameraShakeChannel,
                Shape = cameraShakeShape
            });
        }

        private static float CurrentCameraZoom(CinemachineVirtualCamera camera)
            => camera.m_Lens.Orthographic ? camera.m_Lens.OrthographicSize : camera.m_Lens.FieldOfView;

        private IEnumerator RunCameraDisposeTest()
        {
            // 첫 yield 전에 예외가 나면 실행한 Coroutine 핸들이 남지 않도록 다음 프레임에 시작한다.
            yield return null;
            var camera = selectedCameraName == playCameraName ? playTestCamera : cinematicTestCamera;
            float from = CurrentCameraZoom(camera);
            const float duration = 2f;
            try
            {
                DispatchCameraEvent(new CameraZoomData
                {
                    CameraName = selectedCameraName,
                    From = from,
                    To = camera.m_Lens.Orthographic ? Mathf.Max(0.01f, from * cameraZoomFactor)
                        : Mathf.Clamp(from * cameraZoomFactor, 1f, 179f),
                    Duration = duration
                });
                DispatchCameraShake(duration);
            }
            catch (Exception exception)
            {
                ((ISystemBindScene)cameraEffects).BindScene();
                cameraTestStatus = "FAIL: " + exception.Message;
                Debug.LogException(exception, this);
                cameraLifecycleRoutine = null;
                yield break;
            }
            yield return new WaitForSeconds(0.25f);
            float frozenZoom = CurrentCameraZoom(camera);
            // 내부에서 기존 이벤트 Dispose + 새 이벤트 등록. 카탈로그 핸들은 유지된다.
            ((ISystemBindScene)cameraEffects).BindScene();
            cameraTestStatus = "Disposed events; checking old Delay callbacks...";
            yield return new WaitForSeconds(duration + 0.1f);
            bool passed = camera != null && Mathf.Approximately(CurrentCameraZoom(camera), frozenZoom);
            cameraTestStatus = passed ? "PASS: Dispose cancelled zoom; old callbacks ignored" : "FAIL: zoom changed after Dispose";
            if (passed) Debug.Log("[TestRuner] " + cameraTestStatus, this);
            else Debug.LogError("[TestRuner] " + cameraTestStatus, this);
            cameraLifecycleRoutine = null;
        }

        [ContextMenu("Camera/Run Test List")]
        public void RunCameraTestList()
        {
            if (!CameraTestsReady || !Application.isPlaying) return;
            StopCameraTestList();
            cameraTestRoutine = StartCoroutine(RunCameraTests());
        }

        private IEnumerator RunCameraTests()
        {
            yield return null;
            if (cameraTestList != null)
            {
                for (int i = 0; i < cameraTestList.Length && CameraTestsReady; i++)
                {
                    ExecuteCameraTest(cameraTestList[i]);
                    while (cameraLifecycleRoutine != null && CameraTestsReady) yield return null;
                    float delay = Mathf.Max(0.1f, cameraTestStepDelay, cameraZoomDuration, cameraShakeDuration);
                    float elapsed = 0f;
                    while (elapsed < delay)
                    {
                        elapsed += Time.deltaTime;
                        yield return null;
                    }
                }
            }
            cameraTestStatus = "Test list complete";
            cameraTestRoutine = null;
        }

        [ContextMenu("Camera/Stop Test List")]
        public void StopCameraTestList()
        {
            if (cameraTestRoutine != null) StopCoroutine(cameraTestRoutine);
            cameraTestRoutine = null;
            if (cameraLifecycleRoutine != null)
            {
                StopCoroutine(cameraLifecycleRoutine);
                cameraLifecycleRoutine = null;
                if (CameraTestsReady) ((ISystemBindScene)cameraEffects).BindScene();
            }
        }

        private void StopCameraTests()
        {
            StopCameraTestList();
            CancelCameraZoom();
            cameraTestAvailable = false;
            selectedCameraName = null;
            playHandle?.Dispose();
            cinematicHandle?.Dispose();
            playHandle = null;
            cinematicHandle = null;
            playTestCamera = null;
            cinematicTestCamera = null;
            catalog = null;
            cameraEffects = null;
        }

        private void OnGUI()
        {
            if (!Application.isPlaying || !showCameraTestPanel || !cameraTestsEnabled) return;
            GUILayout.BeginArea(new Rect(12f, 12f, 340f, Mathf.Min(640f, Screen.height - 24f)), GUI.skin.box);
            GUILayout.Label("Camera Event Tests | WASD: XZ");
            GUILayout.Label(cameraTestStatus);
            if (CameraTestsReady) GUILayout.Label($"Camera: {selectedCameraName}");
            GUILayout.Label($"Last data: {lastCameraEventData}");
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && CameraTestsReady && cameraLifecycleRoutine == null;
            if (GUILayout.Button("Move Play Camera (+XZ step)")) TestCameraMovePlay();
            if (GUILayout.Button("Reset Play Pose")) ExecuteCameraTest(CameraTest.ResetPlayPose);
            if (GUILayout.Button("Zoom In")) TestCameraZoomIn();
            if (GUILayout.Button("Reset Zoom")) TestCameraResetZoom();
            if (GUILayout.Button("Shake (CameraShakeData)")) TestCameraShake();
            if (GUILayout.Button("Zoom + Shake")) TestCameraZoomAndShake();
            if (GUILayout.Button("Shake Burst")) TestCameraShakeBurst();
            if (GUILayout.Button("Replace Running Zoom")) TestCameraReplaceZoom();
            if (GUILayout.Button("Dispose During Effects (2 sec test)")) TestCameraDisposeEffects();
            if (GUILayout.Button("Switch To Cinematic")) TestCameraSwitchToCinematic();
            if (GUILayout.Button("Switch To Play")) TestCameraSwitchToPlay();
            if (GUILayout.Button("Reset All")) TestCameraResetAll();
            if (GUILayout.Button("Run Inspector Test List")) RunCameraTestList();
            GUI.enabled = previousEnabled && CameraTestsReady;
            if (GUILayout.Button("Stop Test List")) StopCameraTestList();
            GUI.enabled = previousEnabled;
            GUILayout.EndArea();
        }
    }
}
