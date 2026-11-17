using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>타일 원본에서 최종 위치 기여분을 계산한다. 진입 효과나 전투 명령을 실행하지 않는다.</summary>
    public static class PositionTileModifierCalculator
    {
        public static PositionTileModifier ResolveAt(CellCoord finalStop, MovementEnvironmentFacts environment)
        {
            if (environment == null) throw new ArgumentNullException(nameof(environment));
            DestinationTileModifierFact source = null;
            foreach (var tile in environment.Tiles)
            {
                if (!tile.Active || tile.Cell != finalStop || tile is not DestinationTileModifierFact modifier) continue;
                if (source != null)
                    throw new NotSupportedException("중첩 목적지 보정에는 작성된 조합 정책이 필요합니다.");
                source = modifier;
            }
            return source == null ? PositionTileModifier.None :
                new PositionTileModifier(finalStop, new[] { source.Id }, source.AttackBonus, source.MaxHealthBonus);
        }

        public static CharacterCombatFacts Replace(CharacterCombatFacts actor, PositionTileModifier next)
        {
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            if (next == null) throw new ArgumentNullException(nameof(next));
            if (actor.Stats.PositionAttackBonus != actor.PositionModifier.AttackBonus)
                throw new InvalidOperationException("능력치의 위치 공격력 기여분이 원본 기록과 다릅니다.");
            if (actor.PositionModifier.SameValues(next)) return actor;
            int maxHealth = checked(actor.Health.MaxHealth - actor.PositionModifier.MaxHealthBonus + next.MaxHealthBonus);
            var health = new HealthStateSnapshot(Math.Min(actor.Health.CurrentHealth, maxHealth), maxHealth);
            var s = actor.Stats;
            var stats = s.With(effectiveAttackPower: checked(s.AttackPower + s.ArtifactAttackBonus + next.AttackBonus + s.TraitAttackBonus),
                positionAttackBonus: next.AttackBonus);
            return RampageApplication.ReconcileChange(actor, actor.With(health: health, stats: stats, positionModifier: next));
        }

        internal static void RecordNormalization(List<CombatEvent> events, CharacterCombatFacts before,
            CharacterCombatFacts after, List<CombatHealthExecution> executions = null)
        {
            if (before.Health.Equals(after.Health)) return;
            var evidence = new CombatCharacterEvent(CombatEventKind.HealthNormalized, before, after);
            events.Add(evidence);
            if (executions != null)
                CombatHealthRecording.Add(executions, before.EntityId, before.Health, after.Health,
                    new CombatHealthSource(CombatHealthOperationKind.Normalization, contextEvent: evidence), evidence,
                    normalization: before.Health.CurrentHealth - after.Health.CurrentHealth);
        }

        internal static MainCharacterPersistentChange MainHealthChange(CombatCommandInput input, CharacterCombatFacts final)
        {
            if (final.OwnershipRole != CharacterOwnershipRole.Main || final.Health.CurrentHealth == input.Main.CurrentHealth)
                return null;
            return new MainCharacterPersistentChange(input.MainId, input.Main,
                input.Main.WithCurrentHealth(final.Health.CurrentHealth));
        }
    }
}
