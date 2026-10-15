using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.GlobalSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.Factory.Runtime.Global
{
    #region 시스템 모듈

    internal static class GlobalSystemFactory
    {
        /// <param name="inputControllers">입력 경로표에 한 번 등록할 Controller 목록입니다.</param>
        /// <param name="inputLayers">입력 연결 계층이 전환할 수 있는 Layer 목록입니다.</param>
        public static IExportSystemServices Create(
            IReadOnlyList<IInputController> inputControllers,
            IReadOnlyList<InputLayer> inputLayers)
        {
            SystemBuilderHelper builder = new();

            RenderSystemFactory.Create().ExportServices(builder);
            UtilitySystemFactory.Create().ExportServices(builder);
            AudioSystemFactory.Create().ExportServices(builder);
            InputSystemFactory.Create(inputControllers, inputLayers).ExportServices(builder);
            LodingFactory.Create().ExportServices(builder);

            var uiSystem = new UISystem();
            uiSystem.ExportServices(builder);

            JobSystemFactory.Create().ExportServices(builder);

            return new GlobalSystemGroup(builder.BuildSystemReader());
        }
    }

    #endregion
}
