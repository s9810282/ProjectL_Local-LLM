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

        void ChangeLayer(InputLayer nextLayer);

        /// <summary>현재 Layer를 해제하여 모든 입력을 받지 않는 상태로 둡니다.</summary>
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
        ISystemShutdown,
        IExportSystemServices
    {
        private IInputLayerRuntime _layerRuntime;
        private InputRouteTable _routeTable;

        private bool _isConfigured;
        private bool _isShutdown;

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

            _layerRuntime.ChangeLayer(
                nextLayer);
        }

        public void ClearLayer()
        {
            EnsureAvailable();

            _layerRuntime.ClearLayer();
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
