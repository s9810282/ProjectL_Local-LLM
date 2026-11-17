using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks
{
    internal interface IStageGimmickRule
    {
        StageGimmickKey Key { get; }
        bool RunsAt(StageGimmickBoundaryPhase phase);

        /// <summary>호출자의 지역 난수만 진행시키며, 상태가 없는 기믹은 null을 반환합니다.</summary>
        StageGimmickState Initialize(CombatCommandState initialState, StageGimmickSpec definition,
            StageGimmickId id, ref uint randomState);

        IEnumerator<HazardCellFact?> EnumerateForecast(CombatCommandInput input, StageGimmickSpec definition,
            StageGimmickId id, long lastBoundaryCount);
        CombatCommandTransition TryAdvance(CombatCommandInput input, StageGimmickSpec definition,
            StageGimmickId id, StageGimmickBoundary? boundary);
    }

    /// <summary>
    /// 유닛이 칸에 도착한 순간(일반·강제 이동, 합법 배치)에 반응하는 기믹이다. 이동 계산은
    /// <see cref="StageGimmickComposition.ApplyEntry"/>만, 이동 되돌리기는
    /// <see cref="StageGimmickComposition.CollectEntryReceipts"/>만 부르고 개별 기믹을 알지 않는다.
    /// 적용과 되돌리기 인정은 같은 규칙이 맡는다.
    /// </summary>
    internal interface IStageGimmickEntryRule
    {
        /// <summary>도착자의 현재 투영 사실을 읽어 효과를 적용한다. 반응이 없으면 아무것도 바꾸지 않는다.</summary>
        void ApplyEntry(MovementProjection projection, StageGimmickSpec definition, StageGimmickId id, EntityId arriver);

        /// <summary>
        /// 이동 전 <paramref name="before"/>와 이동 뒤 <paramref name="after"/> 사이에서 이 기믹의 도착 효과가 바꾼 몫을
        /// 되돌림 영수증으로 더한다. <paramref name="requireContact"/>이면 <paramref name="after"/> 위치에서 다시 생길 효과만 인정한다.
        /// <paramref name="input"/>은 이동 전 사실이다.
        /// </summary>
        void CollectEntryReceipts(CombatCommandInput input, StageGimmickSpec definition, StageGimmickId id,
            MovementEffectId movement, CharacterCombatFacts before, CharacterCombatFacts after, bool requireContact,
            ICollection<MovementEffectReceipt> receipts);
    }
}
