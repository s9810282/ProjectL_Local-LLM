using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.DelaySystem.View;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.EventBuse.View;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.Factory.Runtime.Global
{
    #region 시스템 모듈

    public static class UtilitySystemFactory
    {
        public static IExportSystemServices Create() 
        {
            // 서비스 및 모듈 등록을 도우미 여기서 다 모아줌
            SystemBuilderHelper systemBuilderHelper = new();

            // 여기서 서비스 및 모듈을 등록함
            var DelaySystem = new DelaySystem();
            DelaySystem.ExportServices(systemBuilderHelper);

            var EventSystem = new EventSystem();
            EventSystem.ExportServices(systemBuilderHelper);

            // 서비스 와 모듈 Store를 통해서 가공하기
            // 서비스는 조회가능한 형태로 바꿔주고
            // 모듈은 어떤 시스템이 등록되어 있는 파악
            UtilitySystemGroup UtilitySystemGroup = new UtilitySystemGroup(systemBuilderHelper.BuildSystemReader());

            return UtilitySystemGroup;
        }
    }

    #endregion
}
