using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Configuration;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.ServiceKeys.Runtime;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View
{
    /// <summary>공용 ESC 액션을 UI 서비스로 전달합니다. 종료 때 입력·오디오 서비스를 해제하기 전에 옵션을 닫습니다.</summary>
    internal sealed class OptionInputRouter : ISystemPostBindService, ISystemInitialize,
        ISystemShutdown, IExportSystemServices, IOptionCloseRequestService
    {
        private IUISystemService _ui;
        private bool _isInitialized;
        private IOptionInputReceiver _receiver;

        internal IInputController OpenController { get; }
        internal IInputController CloseController { get; }

        internal OptionInputRouter()
        {
            OpenController = new OptionController(GlobalInputNames.CommonMap, GlobalInputNames.OpenSettings, Open);
            CloseController = new OptionController(GlobalInputNames.SettingsMap, GlobalInputNames.CloseSettings, Close);
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            _ui = resolver.GetService(SystemGroupsService.Global).UIServiceCommand;
        }

        public void Initialize() => _isInitialized = true;
        private void Open() { if (_isInitialized) _receiver?.RequestOpen(); }
        private void Close() { if (_isInitialized) _receiver?.RequestClose(); }

        public void Bind(IOptionInputReceiver receiver)
        {
            if (receiver == null) throw new ArgumentNullException(nameof(receiver));
            if (_receiver != null && !ReferenceEquals(_receiver, receiver))
                throw new InvalidOperationException("옵션 닫기 수신 대상이 이미 연결되어 있습니다.");
            _receiver = receiver;
        }

        public void Unbind(IOptionInputReceiver receiver)
        {
            if (ReferenceEquals(_receiver, receiver)) _receiver = null;
        }

        public void Shutdown()
        {
            if (_isInitialized) _ui.CloseUI(UIId.Setting);
            _isInitialized = false;
            _ui = null;
            _receiver = null;
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            if (sink == null) throw new ArgumentNullException(nameof(sink));
            sink.AddService<IOptionCloseRequestService>(this);
            sink.AddModule<ISystemModule>(this);
        }

        private sealed class OptionController : InputController
        {
            private readonly string _action;
            private readonly Action _receiver;
            public override string ActionMap { get; }

            internal OptionController(string map, string action, Action receiver)
            {
                ActionMap = map;
                _action = action;
                _receiver = receiver;
            }

            protected override void Configure(InputBindings input) => input.Press(_action, _receiver);
        }
    }
}
