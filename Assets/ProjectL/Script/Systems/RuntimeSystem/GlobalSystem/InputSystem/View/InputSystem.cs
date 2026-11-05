using System;
using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Configuration;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Core;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View
{
    #region Public Contracts

    #region 서비스

    public interface IInputLayerProvider
    {
        InputLayer CurrentLayer { get; }
    }

    public interface IInputLayerRuntime :
        IInputLayerProvider
    {
        void ChangeLayer(InputLayer nextLayer);

        /// <summary>현재 Layer를 해제하여 모든 ActionMap을 끕니다. 해제 뒤 CurrentLayer는 default입니다.</summary>
        void ClearLayer();
    }

    public interface IInputLayerService :
        IServiceModule
    {
        InputLayer CurrentLayer { get; }

        /// <summary>입력 독점 중에는 복귀할 Layer만 갱신하고, 그 외에는 즉시 전환합니다.</summary>
        void ChangeLayer(InputLayer nextLayer);

        /// <summary>일반 입력을 해제합니다. 독점 중에는 독점 Layer를 유지하고, 독점 해제 후 default로 복귀합니다.</summary>
        void ClearLayer();
    }

    public interface IInputSystemService :
        IInputLayerService
    {
        void Configure(
            IInputLayerRuntime layerRuntime,
            InputConfiguration configuration);

        void Submit(
            string actionMap,
            string actionName,
            InputPhase phase,
            InputValueData value,
            double time);
    }

    #endregion

    #endregion

    #region 시스템 모듈

    internal sealed class InputSystem :
        IInputSystemService,
        IInputLayerOverrideService,
        ISystemShutdown,
        IExportSystemServices
    {
        private IInputLayerRuntime _layerRuntime;
        private InputRouteTable _routeTable;

        private bool _isConfigured;
        private bool _isShutdown;
        private LayerOverride _layerOverride;
        private InputLayer _requestedLayer;

        #region System Service

        public InputLayer CurrentLayer
        {
            get
            {
                if (_isConfigured == false ||
                    _isShutdown)
                {
                    return default;
                }

                return _layerRuntime.CurrentLayer;
            }
        }

        public void ChangeLayer(
            InputLayer nextLayer)
        {
            EnsureAvailable();

            if (nextLayer.IsValid == false)
            {
                throw new ArgumentException(
                    "Input Layer is invalid.",
                    nameof(nextLayer));
            }

            _requestedLayer = nextLayer;
            if (_layerOverride == null)
                _layerRuntime.ChangeLayer(nextLayer);
        }

        public void ClearLayer()
        {
            EnsureAvailable();

            _requestedLayer = default;
            if (_layerOverride == null)
                _layerRuntime.ClearLayer();
        }

        public IDisposable OverrideLayer(InputLayer layer)
        {
            EnsureAvailable();
            if (!layer.IsValid) throw new ArgumentException("입력 Layer가 유효하지 않습니다.", nameof(layer));
            if (_layerOverride != null) throw new InvalidOperationException("입력 Layer가 이미 독점 중입니다.");
            _requestedLayer = _layerRuntime.CurrentLayer;
            var scope = new LayerOverride(this);
            _layerOverride = scope;
            try
            {
                _layerRuntime.ChangeLayer(layer);
                return scope;
            }
            catch
            {
                ReleaseOverride(scope);
                throw;
            }
        }

        private void ReleaseOverride(LayerOverride scope)
        {
            if (!ReferenceEquals(_layerOverride, scope)) return;
            _layerOverride = null;
            if (_requestedLayer.IsValid) _layerRuntime.ChangeLayer(_requestedLayer);
            else _layerRuntime.ClearLayer();
        }

        private sealed class LayerOverride : IDisposable
        {
            private InputSystem _owner;

            internal LayerOverride(InputSystem owner) => _owner = owner;

            public void Dispose()
            {
                InputSystem owner = _owner;
                _owner = null;
                owner?.ReleaseOverride(this);
            }
        }

        public void Submit(
            string actionMap,
            string actionName,
            InputPhase phase,
            InputValueData value,
            double time)
        {
            // A late Canceled callback can arrive after
            // the Framework has already shut down.
            if (_isShutdown)
            {
                return;
            }

            if (_isConfigured == false)
            {
                throw new InvalidOperationException(
                    "InputSystem is not configured.");
            }

            ValidateSubmission(
                actionMap,
                actionName,
                phase,
                value);

            InputLayer currentLayer =
                _layerRuntime.CurrentLayer;

            if (currentLayer.IsValid == false ||
                currentLayer.Allows(
                    actionMap) == false)
            {
                return;
            }

            InputSnapshot snapshot =
                new InputSnapshot(
                    currentLayer,
                    actionMap,
                    actionName,
                    phase,
                    value,
                    time);

            _routeTable.TryDispatch(
                actionMap,
                actionName,
                snapshot);
        }

        #endregion

        #region Configuration

        public void Configure(
            IInputLayerRuntime layerRuntime,
            InputConfiguration configuration)
        {
            EnsureCanConfigure();

            if (layerRuntime == null)
            {
                throw new ArgumentNullException(
                    nameof(layerRuntime));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(
                    nameof(configuration));
            }

            _layerRuntime = layerRuntime;
            _routeTable = configuration.RouteTable;
            _isConfigured = true;
        }

        #endregion

        #region System Module

        public void Shutdown()
        {
            if (_isShutdown)
            {
                return;
            }

            _isShutdown = true;
            _isConfigured = false;
            _layerOverride = null;
            _requestedLayer = default;

            _routeTable = null;
            _layerRuntime = null;
        }

        public void ExportServices(
            ISystemWriteSink sink)
        {
            if (sink == null)
            {
                throw new ArgumentNullException(
                    nameof(sink));
            }

            sink.AddService<IInputSystemService>(this);
            sink.AddService<IInputLayerOverrideService>(this);
            sink.AddModule<ISystemModule>(this);
        }

        #endregion

        #region Internal

        private void EnsureCanConfigure()
        {
            if (_isShutdown)
            {
                throw new ObjectDisposedException(
                    nameof(InputSystem));
            }

            if (_isConfigured)
            {
                throw new InvalidOperationException(
                    "InputSystem is already configured.");
            }
        }

        private void EnsureAvailable()
        {
            if (_isShutdown)
            {
                throw new ObjectDisposedException(
                    nameof(InputSystem));
            }

            if (_isConfigured == false)
            {
                throw new InvalidOperationException(
                    "InputSystem is not configured.");
            }
        }

        private static void ValidateSubmission(
            string actionMap,
            string actionName,
            InputPhase phase,
            InputValueData value)
        {
            if (string.IsNullOrWhiteSpace(
                    actionMap))
            {
                throw new ArgumentException(
                    "Action Map name is required.",
                    nameof(actionMap));
            }

            if (string.IsNullOrWhiteSpace(
                    actionName))
            {
                throw new ArgumentException(
                    "Input action name is required.",
                    nameof(actionName));
            }

            if (phase != InputPhase.Started &&
                phase != InputPhase.Performed &&
                phase != InputPhase.Canceled)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(phase),
                    phase,
                    "Unsupported input phase.");
            }

            if (value.IsValid == false)
            {
                throw new ArgumentException(
                    "Input value is invalid.",
                    nameof(value));
            }
        }

        #endregion
    }

    #endregion
}
