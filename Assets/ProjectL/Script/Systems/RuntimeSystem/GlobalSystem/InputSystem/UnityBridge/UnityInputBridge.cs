using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Configuration;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.UnityBridge
{
    #region 시스템 모듈

    /// <summary>
    /// Unity Input System의 액션 콜백을 Framework InputSystem.Submit으로 넘기는 연결 계층입니다.
    /// InitData에서 View DB의 입력 설정 에셋을 복제·검증하고 InputSystem을 한 번 Configure합니다.
    /// Initialize에서 구독을 시작하며, Layer가 없는 상태라 모든 맵은 꺼진 채로 시작합니다.
    /// 입력 설정이 없거나 맞지 않으면 초기화를 예외로 멈춥니다.
    /// </summary>
    internal sealed class UnityInputBridge :
        ISystemInitData<IHasViewQuery>,
        ISystemInitialize,
        ISystemShutdown,
        IExportSystemServices
    {
        private readonly IInputSystemService _router;
        private readonly IInputController[] _controllers;
        private readonly InputLayer[] _layers;
        private readonly Dictionary<InputAction, InputValueKind> _valueKinds = new();
        private readonly Action<InputAction.CallbackContext> _actionTriggeredHandler;

        private InputActionAsset _runtimeActions;
        private UnityInputLayerRuntime _layerRuntime;

        private bool _isDataInitialized;
        private bool _isSubscribed;
        private bool _isActive;
        private bool _isShutdownStarted;

        internal UnityInputBridge(
            IInputSystemService router,
            IReadOnlyList<IInputController> controllers,
            IReadOnlyList<InputLayer> layers)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _controllers = CopyWithoutNull(controllers, nameof(controllers));
            _layers = CopyLayers(layers);
            _actionTriggeredHandler = OnActionTriggered;
        }

        #region 시스템 모듈

        public void InitData(IHasViewQuery data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (_isShutdownStarted)
                throw new ObjectDisposedException(nameof(UnityInputBridge));
            if (_isDataInitialized)
                throw new InvalidOperationException("UnityInputBridge InitData has already been called.");

            InputActionAsset source = ResolveActions(data);

            // 바인딩 등록은 Unity 자원이 필요 없으므로 복제 전에 끝내 둡니다.
            InputConfiguration configuration = InputConfiguration.Create(_controllers);

            _runtimeActions = UnityEngine.Object.Instantiate(source);
            _runtimeActions.name = $"{source.name} (Runtime)";
            _runtimeActions.Disable();

            try
            {
                InputConfigurationValidator.ValidateOrThrow(_runtimeActions, _layers, configuration);
                CacheValueKinds(configuration);
                _layerRuntime = new UnityInputLayerRuntime(_runtimeActions, _layers);
                _router.Configure(_layerRuntime, configuration);
            }
            catch
            {
                // 실패한 초기화는 복제 에셋만 정리하고 원본은 건드리지 않습니다. 재Configure로 복구하지 않습니다.
                _layerRuntime?.Dispose();
                _layerRuntime = null;
                _valueKinds.Clear();
                ReleaseRuntimeActions();
                throw;
            }

            _isDataInitialized = true;
        }

        public void Initialize()
        {
            if (_isShutdownStarted)
                throw new ObjectDisposedException(nameof(UnityInputBridge));
            if (_isDataInitialized == false)
                throw new InvalidOperationException("UnityInputBridge requires InitData before Initialize.");
            if (_isActive)
                return;

            Subscribe();

            try
            {
                _layerRuntime.Resume();
                _isActive = true;
            }
            catch
            {
                try
                {
                    _layerRuntime.Suspend();
                }
                finally
                {
                    Unsubscribe();
                }

                throw;
            }
        }

        public void Shutdown()
        {
            if (_isShutdownStarted)
                return;

            _isShutdownStarted = true;

            try
            {
                // 구독과 라우터가 살아 있는 동안 맵을 꺼야 마지막 Canceled가 Controller까지 전달됩니다.
                if (_isActive)
                    _layerRuntime.Suspend();
            }
            finally
            {
                _isActive = false;
                Unsubscribe();
                _layerRuntime?.Dispose();
                _layerRuntime = null;
                _valueKinds.Clear();
                ReleaseRuntimeActions();
            }
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            if (sink == null)
                throw new ArgumentNullException(nameof(sink));

            sink.AddModule<ISystemModule>(this);
        }

        #endregion

        #region Unity Callback

        private void Subscribe()
        {
            if (_isSubscribed)
                return;

            var maps = _runtimeActions.actionMaps;
            for (int i = 0; i < maps.Count; i++)
                maps[i].actionTriggered += _actionTriggeredHandler;

            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (_isSubscribed == false || _runtimeActions == null)
                return;

            var maps = _runtimeActions.actionMaps;
            for (int i = 0; i < maps.Count; i++)
                maps[i].actionTriggered -= _actionTriggeredHandler;

            _isSubscribed = false;
        }

        private void OnActionTriggered(InputAction.CallbackContext context)
        {
            if (_runtimeActions == null || context.action == null || context.action.actionMap == null)
                return;

            if (InputActionValueReader.TryConvertPhase(context.phase, out InputPhase phase) == false)
                return;

            // Controller가 등록하지 않은 액션(예: Common.OpenSettings)은 넘기지 않습니다.
            if (_valueKinds.TryGetValue(context.action, out InputValueKind valueKind) == false)
                return;

            InputValueData value = InputActionValueReader.ReadValue(context, phase, valueKind);

            _router.Submit(
                context.action.actionMap.name,
                context.action.name,
                phase,
                value,
                context.time);
        }

        #endregion

        #region Internal

        private static InputActionAsset ResolveActions(IHasViewQuery data)
        {
            var viewQuery = data.ViewQuery
                ?? throw new InvalidOperationException("[UnityInputBridge] ViewQuery is required.");
            var table = viewQuery.InputSettingsTableQuery
                ?? throw new InvalidOperationException("[UnityInputBridge] Input settings table is required.");
            var settings = table.Config
                ?? throw new InvalidOperationException("[UnityInputBridge] Input settings item is required.");

            return settings.Actions
                ?? throw new InvalidOperationException("[UnityInputBridge] Input action asset is required.");
        }

        private void CacheValueKinds(InputConfiguration configuration)
        {
            _valueKinds.Clear();

            IReadOnlyList<InputBindingDescriptor> bindings = configuration.Bindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                InputBindingDescriptor binding = bindings[i];
                InputAction action = _runtimeActions
                    .FindActionMap(binding.ActionMap, true)
                    .FindAction(binding.ActionName, true);

                if (_valueKinds.TryGetValue(action, out InputValueKind registeredKind))
                {
                    if (registeredKind != binding.ValueKind)
                        throw new InvalidOperationException(
                            $"Input Action value type is registered inconsistently: " +
                            $"{binding.ActionMap}.{binding.ActionName}");

                    continue;
                }

                _valueKinds.Add(action, binding.ValueKind);
            }
        }

        private void ReleaseRuntimeActions()
        {
            if (_runtimeActions == null)
                return;

            _runtimeActions.Disable();

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(_runtimeActions);
            else
                UnityEngine.Object.DestroyImmediate(_runtimeActions);

            _runtimeActions = null;
        }

        private static IInputController[] CopyWithoutNull(
            IReadOnlyList<IInputController> controllers, string paramName)
        {
            if (controllers == null)
                throw new ArgumentNullException(paramName);
            if (controllers.Count == 0)
                throw new ArgumentException("At least one Input Controller is required.", paramName);

            var copy = new IInputController[controllers.Count];
            for (int i = 0; i < copy.Length; i++)
            {
                copy[i] = controllers[i]
                    ?? throw new ArgumentException($"Input Controller cannot be null. Index={i}", paramName);
            }

            return copy;
        }

        private static InputLayer[] CopyLayers(IReadOnlyList<InputLayer> layers)
        {
            if (layers == null)
                throw new ArgumentNullException(nameof(layers));

            var copy = new InputLayer[layers.Count];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = layers[i];

            // 빈 목록·잘못된 Layer·중복은 InitData의 검증에서 에셋과 함께 한 번에 알립니다.
            return copy;
        }

        #endregion
    }

    #endregion
}
