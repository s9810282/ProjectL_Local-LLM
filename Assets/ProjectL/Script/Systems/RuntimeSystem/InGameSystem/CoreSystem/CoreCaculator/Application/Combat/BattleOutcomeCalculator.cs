using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>모든 사망 경로가 같은 전투 목표로 승패를 판정한다. 목표 없는 판정은 제공하지 않는다.</summary>
    public static class BattleOutcomeCalculator
    {
        public static StageOutcomeSnapshot Resolve(IReadOnlyList<CharacterCombatFacts> characters,
            IReadOnlyList<StructureCombatFacts> structures, BattleObjective objective)
        {
            if (characters == null || structures == null || objective == null) throw new ArgumentNullException();
            CharacterCombatFacts main = null;
            CharacterCombatFacts target = null;
            foreach (var character in characters)
            {
                if (character.OwnershipRole == CharacterOwnershipRole.Main)
                {
                    if (main != null) throw new InvalidOperationException("메인은 유일해야 합니다.");
                    main = character;
                }
                if (character.EntityId == objective.TargetEntityId) target = character;
            }
            if (main == null) throw new InvalidOperationException("메인 사실이 필요합니다.");
            bool achieved;
            if (objective.Kind == BattleObjectiveKind.DesignatedBossDefeated)
            {
                // 대상이 사실 목록에 없으면 승리로 추측하지 않는다.
                if (target == null) throw new InvalidOperationException("지정 보스 목표의 대상 사실이 필요합니다.");
                achieved = target.Health.IsDead;
            }
            else
            {
                achieved = false;
                foreach (var structure in structures)
                    if (structure.Owner == Owner.Enemy && structure.Definition.Key == EntityKey.Structure_Headquarters && structure.Health.IsDead)
                        achieved = true;
            }
            return Resolve(main.Health.IsDead, achieved, objective.Kind);
        }

        /// <summary>메인 사망 패배가 목표 달성보다 우선한다(G11).</summary>
        internal static StageOutcomeSnapshot Resolve(bool mainDead, bool objectiveAchieved, BattleObjectiveKind kind) => mainDead
            ? new(StageOutcome.Defeat, StageOutcomeReason.PlayerMainCharacterDied)
            : objectiveAchieved
                ? new(StageOutcome.Victory, kind == BattleObjectiveKind.DesignatedBossDefeated
                    ? StageOutcomeReason.DesignatedBossDefeated : StageOutcomeReason.EnemyHeadquartersDestroyed)
                : new(StageOutcome.InProgress, StageOutcomeReason.None);
    }
}
