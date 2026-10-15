using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact
{
    /// <summary>Calculates from Buffer facts. Retains only immutable compiled rules, never gameplay state.</summary>
    public sealed class SoulEffectCalculator
    {
        private readonly CompiledArtifactLoadout _loadout;
        private readonly CombatEffectEngine _engine = new CombatEffectEngine();

        public SoulEffectCalculator(ArtifactLoadoutSnapshot loadout)
        {
            _loadout = ArtifactEffectCompiler.Compile(loadout);
        }

        internal void RequireSupportedAttackSettlement()
        {
            foreach (var rule in _loadout.Rules.Span)
                if (rule.Rule is ThornHeartRule)
                    throw new NotSupportedException("반사 Artifact의 공격 정산 순서는 기획 확정 후 연결해야 합니다.");
        }

        /// <summary>경로 투영의 최종 생존 도착, 정산된 자발적 도착 또는 강제 도착에서만 호출한다.</summary>
        public SoulActionChanges Collect(SoulCollectorInput input, SoulFieldSnapshot field, bool movementSucceeded)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (field == null) throw new ArgumentNullException(nameof(field));
            CharacterCombatFacts character = input.Character;
            if (!movementSucceeded || character.Health.IsDead || character.Area != CharacterRuntimeArea.Field)
                return ArtifactActionProjection.ProjectCollection(input, character, input.SoulCount,
                    new SoulFieldChange(field, field), Array.Empty<long>());
            if (input.ArrivalKind == SoulArrivalKind.SettledVoluntary && (character.PendingMoveActionCost != 0 ||
                character.PendingMoveDistance != 0 ||
                character.MovementCheckpointAnchor != character.Anchor.Value))
                throw new InvalidOperationException("Soul collection requires settled voluntary movement.");
            if (character.Stats.SoulAttackBonus != input.SoulCount ||
                character.Stats.SoulMaxHealthBonus != input.SoulCount)
                throw new InvalidOperationException("Soul bonuses must be derived from the owned count.");

            var occupied = new HashSet<CellCoord>(character.OccupiedCells);
            var remaining = new List<SoulFieldFacts>();
            var consumed = new List<long>();
            HealthStateSnapshot health = character.Health;
            int count = input.SoulCount;
            // Snapshot entries are ID ordered; growth and capped healing preserve that order.
            foreach (SoulFieldFacts soul in field.Entries)
            {
                if (!occupied.Contains(soul.Cell)) { remaining.Add(soul); continue; }
                (health, count) = SoulGrowthMath.Collect(health, count, input.BaseMaxHealth,
                    character.TemporaryMaxHealthBonus, soul.HealAmount, character.PositionModifier.MaxHealthBonus);
                consumed.Add(soul.Id);
            }
            if (consumed.Count == 0)
                return ArtifactActionProjection.ProjectCollection(input, character, count,
                    new SoulFieldChange(field, field), consumed);

            UnitStatsSnapshot stats = character.Stats;
            int otherBonus = checked(stats.ArtifactAttackBonus - input.SoulCount);
            if (otherBonus < character.TemporaryAttackBonus)
                throw new InvalidOperationException("Temporary attack bonus is missing from the source stats.");
            int bonus = checked(count * SoulGrowthRules.AttackBonusPerSoul + otherBonus);
            var finalStats = new UnitStatsSnapshot(stats.MoveMethod, stats.MoveRange, stats.MoveCost,
                stats.AppointRange, stats.AttackPower, stats.PhysicalDefense,
                stats.MaxActionCost, stats.AttackCost, stats.RemainingMoveRange, stats.CurrentActionCost,
                stats.Priority, stats.Delay, stats.TraitStatuses.ToArray(), stats.Category, bonus,
                checked(stats.AttackPower + bonus + stats.PositionAttackBonus), count * SoulGrowthRules.AttackBonusPerSoul,
                count * SoulGrowthRules.MaxHealthBonusPerSoul, stats.PositionAttackBonus);
            var final = new CharacterCombatFacts(character.EntityId, character.OwnedId, character.CharacterKey,
                character.Owner, character.OwnershipRole, character.Area, health, finalStats,
                character.ReserveSlotIndex, character.Anchor, character.Rotation, character.OccupiedCells,
                character.MovementCheckpointAnchor, character.CommittedMoveDistance, character.PendingMoveActionCost,
                character.TemporaryAttackBonus, character.TemporaryMaxHealthBonus,
                character.PendingMoveDistance, character.AttackRefundEligibleMoveCost, character.PositionModifier, character.HeldEnvironment);
            return ArtifactActionProjection.ProjectCollection(input, final, count,
                new SoulFieldChange(field, new SoulFieldSnapshot(field.NextSoulId, remaining)), consumed);
        }

        /// <summary>Spawn after projected deaths, then optionally apply the existing turn-end countdown.</summary>
        public SoulFieldChange PlanDeaths(SoulFieldSnapshot field,
            IReadOnlyList<SoulDeathFacts> deaths, bool isNight, bool countdown)
        {
            SoulFieldChange spawned = StageSoulService.PlanDeaths(field, deaths, isNight, _loadout);
            return countdown ? new SoulFieldChange(field, PlanCountdown(spawned.Final).Final) : spawned;
        }

        public SoulFieldChange PlanCountdown(SoulFieldSnapshot field) => SoulFieldState.PlanCountdown(field);

        public int EvaluateAttackPower(CharacterCombatFacts attacker, int soulCount,
            IReadOnlyList<CharacterCombatFacts> characters, List<CombatEvent> events = null) => ArtifactAttackPowerEvaluator.Evaluate(
                attacker, soulCount, ArtifactCombatFacts.Capture(characters), _loadout, _engine, events);
    }
}
