using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Configuration;
using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.GlobalSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem;

namespace ProjectL.Systems.Factory.Runtime.Global
{
    #region 시스템 모듈

    internal static class GlobalSystemFactory
    {
        /// <param name="inputControllers">입력 경로표에 한 번 등록할 Controller 목록입니다.</param>
        /// <param name="inputLayers">입력 연결 계층이 전환할 수 있는 Layer 목록입니다.</param>
        public static IExportSystemServices Create(
            IReadOnlyList<IInputController> inputControllers,
            IReadOnlyList<InputLayer> inputLayers,
            IReadOnlyList<KeyBindingDefinition> keyBindings = null,
            RuntimeFaultBoundary faultBoundary = null)
            => CreateWithInputRegistration(
                new InputRegistrationSet(inputControllers, inputLayers, keyBindings), faultBoundary);

        /// <summary>화면 입력 등록 정보에 Global 소유 옵션 입력을 합쳐 한 번 조립합니다.</summary>
        public static IExportSystemServices CreateWithInputRegistration(
            InputRegistrationSet inputRegistration,
            RuntimeFaultBoundary faultBoundary = null)
        {
            if (inputRegistration == null)
                throw new ArgumentNullException(nameof(inputRegistration));

            var optionInput = new OptionInputRouter();
            InputRegistrationSet input = InputRegistrationSet.Combine(inputRegistration,
                new InputRegistrationSet(
                    new IInputController[] { optionInput.OpenController, optionInput.CloseController },
                    new[] { GlobalInputNames.Settings }));
            SystemBuilderHelper builder = new();

            RenderSystemFactory.Create().ExportServices(builder);
            var utility = UtilitySystemFactory.Create();
            if (!(utility is IUtilitySystemServiceContext utilityContext))
                throw new InvalidOperationException("[GlobalSystemFactory] Utility 서비스 Context가 필요합니다.");
            utility.ExportServices(builder);
            AudioSystemFactory.Create().ExportServices(builder);
            InputSystemFactory.Create(input.Controllers, input.Layers, input.KeyBindings).ExportServices(builder);
            LodingFactory.Create().ExportServices(builder);

            var uiSystem = new UISystem(faultBoundary);
            uiSystem.ExportServices(builder);
            new GlobalUIPresentation(uiSystem, uiSystem.GlobalUIs).ExportServices(builder);
            var text = new UITextPresentation(uiSystem, uiSystem.GlobalUIs, utilityContext.delayService);
            builder.AddService<IUITextPresentation>(text);
            // Global 역순 해제에서 UISystem보다 먼저 지연 콜백을 무효화합니다.
            builder.AddModule<ISystemModule>(text);

            JobSystemFactory.Create().ExportServices(builder);

            // 옵션은 UI·입력·오디오 서비스가 살아 있을 때 먼저 정리합니다.
            optionInput.ExportServices(builder);

            return new GlobalSystemGroup(builder.BuildSystemReader());
        }
    }

    #endregion
}
