using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.UnityBridge;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.Factory.Runtime.Global
{
    #region 시스템 모듈

    internal static class InputSystemFactory
    {
        /// <summary>
        /// 입력 라우터와 Unity 연결 계층을 만듭니다. Controller와 Layer 목록은 루트 조립 지점이 넘깁니다.
        /// 브리지를 InputSystem 다음에 등록하여, 역순 종료 때 브리지가 먼저 맵을 끄고 마지막 Canceled를 전달하게 합니다.
        /// </summary>
        public static IExportSystemServices Create(
            IReadOnlyList<IInputController> controllers,
            IReadOnlyList<InputLayer> layers)
        {
            SystemBuilderHelper builder = new();

            InputSystem inputSystem = new();
            inputSystem.ExportServices(builder);

            new UnityInputBridge(inputSystem, controllers, layers).ExportServices(builder);

            return new InputSystemGroup(builder.BuildSystemReader());
        }
    }

    #endregion
}
