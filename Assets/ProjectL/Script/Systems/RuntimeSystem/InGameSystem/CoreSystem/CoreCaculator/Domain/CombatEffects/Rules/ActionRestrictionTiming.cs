using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules
{
    /// <summary>
    /// 행동 제한을 거는 순간, 이번 처리 뒤에 행동 제한을 1 줄일 진영(종료 감소가 아직 남은 진영)을 판단합니다.
    /// 그 진영에 거는 제한은 곧 1이 줄어 바로 풀리지 않도록 1을 더해야 합니다.
    /// </summary>
    internal static class ActionRestrictionTiming
    {
        /// <summary>
        /// 진영 턴 중(턴 시작 처리 포함)이면 그 진영, 진영 종료 기믹 창(<c>StartingEnemy</c>·전투 뒤 <c>StartingPlayer</c>)이면 방금 끝난 진영,
        /// 그 밖(배치·전투 진입)이면 없음입니다. 종료 기믹 창의 <see cref="CombatTurnFacts.Controller"/>는 다음 진영이라 쓰지 않습니다.
        /// </summary>
        internal static Owner? PendingEndSide(CombatTurnFacts turn)
        {
            if (turn == null) throw new ArgumentNullException(nameof(turn));
            if (turn.StagePhase != StagePhase.Combat) return null;
            return turn.ControllerPhase switch
            {
                CombatControllerPhase.Player => Owner.Player,
                CombatControllerPhase.Enemy => Owner.Enemy,
                CombatControllerPhase.StartingEnemy => Owner.Player,
                // 전투 진입 직후의 StartingPlayer는 아직 끝난 진영이 없다.
                CombatControllerPhase.StartingPlayer => turn.CompletedControllerTurnCount > 0 ? Owner.Enemy : (Owner?)null,
                _ => null
            };
        }

        /// <summary>기본 기간에 종료 감소가 남은 진영이면 1을 더한 행동 제한 기간입니다.</summary>
        internal static int Duration(int baseAffectedSideTurns, Owner affectedSide, CombatTurnFacts turn)
        {
            if (baseAffectedSideTurns < 1) throw new ArgumentOutOfRangeException(nameof(baseAffectedSideTurns));
            return checked(baseAffectedSideTurns + (PendingEndSide(turn) == affectedSide ? 1 : 0));
        }
    }
}
