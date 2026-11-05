using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraCatalogSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.UnityCameraCatalogSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.ScreenTransitionSystem;
using UnityEngine;

namespace ProjectL.Systems
{
    public sealed partial class TestRuner
    {
        [Header("Screen Transition Tests: After / Before")]
        [SerializeField] private bool screenTransitionTestsEnabled;
        [SerializeField] private bool showScreenTransitionTestPanel = true;
        [SerializeField] private string afterRenderCameraName = "Day Camera";
        [SerializeField] private Color afterRenderBorderColor = Color.cyan;
        [SerializeField] private Color beforeRenderBorderColor = Color.magenta;
        [SerializeField, Min(0.01f)] private float screenTransitionChangeSpeed = 12f;
        [SerializeField, Min(0.01f)] private float screenTransitionTargetAmount = 100f;

        private IScreenTransitionSystemService _screenTransitionService;
        private IScreenTransitionScop _screenTransitionScop;
        private IScreenTransitionScop _lastReturnedTransitionScop;
        private IUnityCameraCatalogHandle _transitionMain;
        private ICameraCatalogHandle _transitionAfterCamera;
        private UnityCameraRenderState _transitionInitialMain;
        private int _transitionInitialAfterPriority;
        private int _afterRenderMask;
        private int _beforeRenderMask;
        private bool _screenTransitionAvailable;
        private bool _transitionHasMainSnapshot;
        private bool _transitionHasAfterSnapshot;
        private bool _transitionHasRequest;
        private bool _transitionLastTargetIsAfter;
        private bool _transitionCompletionReported;
        private string _screenTransitionStatus = "Runner not started";

        private bool ScreenTransitionTestsReady => isRunning && screenTransitionTestsEnabled &&
            _screenTransitionAvailable && _screenTransitionService != null;

        private void InitializeScreenTransitionTests(IGlobalSystemGroupService global)
        {
            if (!screenTransitionTestsEnabled)
                return;

            _afterRenderMask = RequireTransitionTestLayer("AfterRendering");
            // 프로젝트에 등록된 기존 레이어 식별자를 사용합니다.
            _beforeRenderMask = RequireTransitionTestLayer("BefalRendering");
            var render = global.RenderServiceCommand;
            _screenTransitionService = render.ViewGroup.ScreenTransitionSystemServices;
            var cameras = render.CoreGroup.CameraGroup;
            _transitionMain = cameras.UnityCameraCatalogSystem.GetHandle(ScreenTransitionInitModule.MainCameraName);
            _transitionInitialMain = _transitionMain.Capture();
            _transitionHasMainSnapshot = true;
            _transitionAfterCamera = cameras.CameraCatalogSystem.GetHandle(afterRenderCameraName);
            _transitionInitialAfterPriority = _transitionAfterCamera.Priority;
            _transitionHasAfterSnapshot = true;
            _screenTransitionAvailable = true;
            _screenTransitionStatus = "Ready: AfterRendering / BefalRendering";
        }

        private static int RequireTransitionTestLayer(string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
                throw new InvalidOperationException($"테스트 대상 레이어가 없습니다: {layerName}");
            return 1 << layer;
        }

        [ContextMenu("Screen Transition/Play AfterRendering")]
        public void TestTransitionToAfterRender() => PlayScreenTransitionTest(true);

        [ContextMenu("Screen Transition/Play BeforeRendering (BefalRendering)")]
        public void TestTransitionToBeforeRender() => PlayScreenTransitionTest(false);

        private void PlayScreenTransitionTest(bool toAfter)
        {
            if (!Application.isPlaying || !ScreenTransitionTestsReady)
            {
                Debug.LogWarning("[TestRuner] Play 모드에서 Screen Transition Tests를 켜고 Runner를 시작하세요.", this);
                return;
            }

            try
            {
                if (_screenTransitionScop == null)
                    RentScreenTransitionScop();

                var data = new ScreenTransitionData
                {
                    Layer = toAfter ? _afterRenderMask : _beforeRenderMask,
                    TargetVirtureCamera = toAfter ? afterRenderCameraName : null,
                    StencileMaskeColor = toAfter ? afterRenderBorderColor : beforeRenderBorderColor,
                    ChangeSpeed = screenTransitionChangeSpeed,
                    TargetChangeAmount = screenTransitionTargetAmount
                };
                _screenTransitionScop.Play(data);
                _transitionHasRequest = true;
                _transitionLastTargetIsAfter = toAfter;
                _transitionCompletionReported = false;
                _screenTransitionStatus = "Playing: " + (toAfter ? "AfterRendering" : "Before (BefalRendering)");
                Debug.Log($"[TestRuner] {_screenTransitionStatus}, VCam={data.TargetVirtureCamera}, " +
                    $"speed={data.ChangeSpeed}, target={data.TargetChangeAmount}", this);
            }
            catch (Exception exception)
            {
                _screenTransitionStatus = "FAIL: " + exception.Message;
                Debug.LogException(exception, this);
            }
        }

        [ContextMenu("Screen Transition/Rent Scop Without Playing")]
        public void RentScreenTransitionScop()
        {
            if (!ScreenTransitionTestsReady || _screenTransitionScop != null)
                return;
            _screenTransitionScop = _screenTransitionService.CreateScop();
            bool reused = ReferenceEquals(_lastReturnedTransitionScop, _screenTransitionScop);
            _lastReturnedTransitionScop = null;
            _transitionHasRequest = false;
            _screenTransitionStatus = "Scop rented. Same pooled object: " + reused;
            Debug.Log("[TestRuner] " + _screenTransitionStatus, this);
        }

        [ContextMenu("Screen Transition/Restart Same Scop")]
        public void RestartScreenTransitionTest()
        {
            if (_screenTransitionScop != null && _transitionHasRequest)
                PlayScreenTransitionTest(_transitionLastTargetIsAfter);
        }

        [ContextMenu("Screen Transition/Dispose Scop")]
        public void DisposeScreenTransitionScop()
        {
            IScreenTransitionScop scop = _screenTransitionScop;
            if (scop == null)
                return;

            bool completed = scop.IsCompleted;
            _screenTransitionScop = null;
            _transitionHasRequest = false;
            _transitionCompletionReported = false;
            try
            {
                scop.Dispose();
                // 이 참조는 다음 대여 때 객체 동일성 비교에만 사용합니다.
                _lastReturnedTransitionScop = scop;
                _screenTransitionStatus = completed ? "Completed Scop returned; final mask kept" : "Cancelled and returned";
            }
            catch (Exception exception)
            {
                _screenTransitionStatus = "Dispose failed: " + exception.Message;
                Debug.LogException(exception, this);
            }
        }

        private void UpdateScreenTransitionTests()
        {
            if (!ScreenTransitionTestsReady || _screenTransitionScop == null || !_transitionHasRequest ||
                _transitionCompletionReported || !_screenTransitionScop.IsCompleted)
                return;

            _transitionCompletionReported = true;
            int target = _transitionLastTargetIsAfter ? _afterRenderMask : _beforeRenderMask;
            int other = _transitionLastTargetIsAfter ? _beforeRenderMask : _afterRenderMask;
            int actual = _transitionMain.Capture().CullingMask;
            bool passed = (actual & target) == target && (actual & other) == 0;
            _screenTransitionStatus = passed ? "PASS: completed; Scop retained until Dispose" : "FAIL: final layer mask";
            if (passed)
                Debug.Log("[TestRuner] " + _screenTransitionStatus, this);
            else
                Debug.LogError("[TestRuner] " + _screenTransitionStatus, this);
        }

        [ContextMenu("Screen Transition/Reset Test Environment")]
        public void ResetScreenTransitionTestEnvironment()
        {
            if (!ScreenTransitionTestsReady)
                return;
            DisposeScreenTransitionScop();
            RestoreScreenTransitionTestEnvironment();
            _screenTransitionStatus = "Initial camera masks and priorities restored";
        }

        private void RestoreScreenTransitionTestEnvironment()
        {
            if (_transitionHasMainSnapshot)
                _transitionMain.Restore(_transitionInitialMain);
            if (_transitionHasAfterSnapshot)
                _transitionAfterCamera.SetPriority(_transitionInitialAfterPriority);
        }

        private void StopScreenTransitionTests()
        {
            DisposeScreenTransitionScop();
            try
            {
                RestoreScreenTransitionTestEnvironment();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
            finally
            {
                _screenTransitionAvailable = false;
                _transitionHasMainSnapshot = false;
                _transitionHasAfterSnapshot = false;
                _transitionMain?.Dispose();
                _transitionAfterCamera?.Dispose();
                _transitionMain = null;
                _transitionAfterCamera = null;
                _screenTransitionService = null;
                _lastReturnedTransitionScop = null;
            }
        }

        private void OnGUI()
        {
            if (!Application.isPlaying || !screenTransitionTestsEnabled || !showScreenTransitionTestPanel)
                return;

            GUILayout.BeginArea(new Rect(Mathf.Max(12f, Screen.width - 412f), 12f,
                400f, Mathf.Min(520f, Screen.height - 24f)), GUI.skin.box);
            GUILayout.Label("Screen Transition | After / Before");
            GUILayout.Label(_screenTransitionStatus);
            GUILayout.Label($"Speed: {screenTransitionChangeSpeed:0.##} / Target radius: {screenTransitionTargetAmount:0.##}");
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && ScreenTransitionTestsReady;
            if (GUILayout.Button("Play AfterRendering -> " + afterRenderCameraName)) TestTransitionToAfterRender();
            if (GUILayout.Button("Play Before (BefalRendering) / keep current camera")) TestTransitionToBeforeRender();
            if (GUILayout.Button("Rent Scop only / check pool reuse")) RentScreenTransitionScop();
            GUI.enabled = previousEnabled && ScreenTransitionTestsReady && _screenTransitionScop != null;
            if (GUILayout.Button("Restart same Scop (Inspector values)")) RestartScreenTransitionTest();
            if (GUILayout.Button("Dispose Scop / cancel while running")) DisposeScreenTransitionScop();
            GUI.enabled = previousEnabled && ScreenTransitionTestsReady;
            if (GUILayout.Button("Reset test environment")) ResetScreenTransitionTestEnvironment();
            GUI.enabled = previousEnabled;

            if (ScreenTransitionTestsReady)
            {
                if (_screenTransitionScop != null)
                {
                    float target = _screenTransitionScop.Data.TargetChangeAmount;
                    float amount = _screenTransitionScop.CurrentChangeAmount;
                    GUILayout.Label($"Progress: {amount:0.##} / {target:0.##} | Completed: {_screenTransitionScop.IsCompleted}");
                    if (_transitionHasRequest && amount >= target && !_screenTransitionScop.IsCompleted)
                        GUILayout.Label("Waiting for the final stencil render pass...");
                }
                int mainMask = _transitionMain.Capture().CullingMask;
                GUILayout.Label($"Main mask: 0x{unchecked((uint)mainMask):X8}");
                GUILayout.Label($"Priority: After={_transitionAfterCamera.Priority}");
            }
            GUILayout.Label("Edit colors / speed / target radius in Inspector.");
            GUILayout.EndArea();
        }
    }
}
