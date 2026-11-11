using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem
{
    /// <summary>
    /// 전투 첫 동작에 쓰는 Core 계산 경로를 상태를 바꾸지 않고 한 번씩 미리 실행합니다(첫 동작 Core 준비 계획 FU-P1).
    /// Combat 씬 로딩 준비 단계의 전투 골격 Job만 부릅니다. Buffer·사건·난수 원본은 바뀌지 않습니다.
    /// </summary>
    public interface ICombatFirstUsePreparation : IServiceModule
    {
        /// <summary>남은 준비 단계 하나를 실행합니다. 더 할 단계가 남았으면 true입니다. 이미 끝났으면 바로 false입니다.</summary>
        bool PrepareNext();
    }
}
