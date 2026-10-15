using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;

namespace ProjectL.Tests.Framework
{
    /// <summary>
    /// LifeCycle 그룹 없이 JobRegister를 조립하던 테스트용 빈 그룹입니다. JobRegister는 바인딩 때 LifeCycle 그룹을 받습니다
    /// (전투 재구성 설계 J14). 이 그룹의 서비스를 실제로 쓰는 전투 진행 테스트는 실제 LifeCycle 그룹을 조립합니다.
    /// </summary>
    internal sealed class TestLifeCycleGroup : ILifeCycleSystemGroupService
    {
        public ILifeCycleManager LifeCycleManager => null;
        public ICombatEntryService CombatEntry => null;
        public ICombatExitService CombatExit => null;
        public ICombatProgressReader CombatProgress => null;

        internal static void Add(ISystemWriteSink sink)
        {
            sink.AddService<ILifeCycleSystemGroupService>(new TestLifeCycleGroup());
        }
    }
}
