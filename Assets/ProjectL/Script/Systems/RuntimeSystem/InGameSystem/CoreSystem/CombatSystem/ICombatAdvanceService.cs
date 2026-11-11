using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem
{
    /// <summary>자동 진행 국면의 다음 단계를 처리한다.</summary>
    public interface ICombatAdvanceService : IServiceModule
    {
        /// <summary>처리해 게시했으면 true다. 자동 국면이 아니거나 Run이 끝났으면 정상 false다.</summary>
        bool AdvanceCombat();

        /// <summary>준비를 실행하지 않고 현재 원본에 대한 준비 상태를 읽는다.</summary>
        AdvancePreparationState ReadPreparationState();
        /// <summary>이번 프레임의 준비 예산을 한 번 사용한다. 전투 결과를 게시하지 않는다.</summary>
        AdvancePreparationState PrepareAdvanceSlice();
        /// <summary>새 프레임에서 준비한 행동 하나만 정산한다. 준비나 대안을 동기 실행하지 않는다.</summary>
        PreparedAdvanceResult AdvancePreparedCombat();
        /// <summary>전투 상태를 바꾸지 않고 보관 중인 준비를 해제한다.</summary>
        void CancelPreparation();
    }
}
