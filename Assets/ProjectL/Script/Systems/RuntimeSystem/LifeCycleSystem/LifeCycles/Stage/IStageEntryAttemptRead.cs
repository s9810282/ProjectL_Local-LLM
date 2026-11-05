using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage
{
    /// <summary>맵 입력 번호와 기존 전투 진입 요청 참조만 읽습니다. 변경·취소 권한은 제공하지 않습니다.</summary>
    public interface IStageEntryAttemptRead : IServiceModule
    {
        bool TryGetCombatEntry(out int mapCommandNumber, out CombatEntryRequest request);
    }
}
