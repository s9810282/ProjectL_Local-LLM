using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact
{
    internal static class ArtifactEffectCompiler
    {
        private const int HealingChaliceAmount = 3;
        private const int LastStandAmount = 5;
        private const string AttackStatId = "Attack";

        internal static CompiledArtifactLoadout Compile(ArtifactLoadoutSnapshot snapshot)
        {
            ReadOnlySpan<ArtifactPlacedEffectSnapshot> effects = snapshot.Effects.Span;
            var rules = new CompiledArtifactRule[effects.Length];
            var origins = new HashSet<EffectOriginKey>();

            for (int i = 0; i < effects.Length; i++)
            {
                ArtifactPlacedEffectSnapshot placed = effects[i];
                ValidatePlacedIdentity(placed, snapshot);
                ArtifactSchema schema = ValidateEffect(placed, snapshot);

                var origin = new EffectOriginKey(
                    EffectOwnerKind.Artifact,
                    placed.InstanceId,
                    placed.ArtifactId,
                    placed.Effect.entryId);
                if (origins.Add(origin) == false)
                    throw Reject(placed, "entryId", "duplicate Origin", snapshot);

                ICombatEffectRule rule = CreateRule(schema, placed.Effect);
                rules[i] = new CompiledArtifactRule(origin, rule);
            }

            Array.Sort(rules, (left, right) => left.Origin.CompareTo(right.Origin));
            return new CompiledArtifactLoadout(snapshot.Revision, rules);
        }

        private static void ValidatePlacedIdentity(
            ArtifactPlacedEffectSnapshot placed,
            ArtifactLoadoutSnapshot snapshot)
        {
            if (placed.ArtifactId < 0)
                throw Reject(placed, "ArtifactId", "must be zero or greater", snapshot);
            if (placed.InstanceId < 0)
                throw Reject(placed, "InstanceId", "must be zero or greater", snapshot);
            if (string.IsNullOrWhiteSpace(placed.Effect.entryId))
                throw Reject(placed, "entryId", "must not be empty", snapshot);
        }

        private static ArtifactSchema ValidateEffect(
            ArtifactPlacedEffectSnapshot placed,
            ArtifactLoadoutSnapshot snapshot)
        {
            ArtifactEffectData data = placed.Effect;

            RequireDefined(placed, data.trigger, "trigger", snapshot);
            RequireDefined(placed, data.source.faction, "source.faction", snapshot);
            RequireDefined(placed, data.source.category, "source.category", snapshot);
            RequireDefined(placed, data.condition.type, "condition.type", snapshot);
            RequireDefined(placed, data.condition.comparison, "condition.comparison", snapshot);
            RequireDefined(placed, data.target.type, "target.type", snapshot);
            RequireDefined(placed, data.target.faction, "target.faction", snapshot);
            RequireDefined(placed, data.target.category, "target.category", snapshot);
            RequireDefined(placed, data.location.type, "location.type", snapshot);
            RequireDefined(placed, data.effect.type, "effect.type", snapshot);
            RequireDefined(placed, data.value.type, "value.type", snapshot);
            RequireDefined(placed, data.value.basis, "value.basis", snapshot);

            if (float.IsNaN(data.target.maxHpRatio) || float.IsInfinity(data.target.maxHpRatio))
                throw Reject(placed, "target.maxHpRatio", "must be finite", snapshot);
            if (float.IsNaN(data.value.amount) || float.IsInfinity(data.value.amount))
                throw Reject(placed, "value.amount", "must be finite", snapshot);

            Require(placed,
                data.target.category == CategoryKey.None,
                "target.category", "Artifact target cannot use a category", snapshot);
            Require(placed,
                data.target.excludeCategory == false,
                "target.excludeCategory", "category exclusion is unused", snapshot);
            Require(placed,
                data.target.useHpRatioFilter == false,
                "target.useHpRatioFilter", "HP ratio filtering is unused", snapshot);
            Require(placed,
                data.target.maxHpRatio == 0f,
                "target.maxHpRatio", "unused HP ratio must be zero", snapshot);
            Require(placed,
                data.effect.knockbackDistance == 0,
                "effect.knockbackDistance", "knockback distance is unused", snapshot);

            switch (data.trigger)
            {
                case ArtifactTriggerType.StageCleared:
                    ValidateHealingChalice(placed, data, snapshot);
                    return ArtifactSchema.HealingChalice;
                case ArtifactTriggerType.StageActive:
                    ValidateLastStand(placed, data, snapshot);
                    return ArtifactSchema.LastStand;
                case ArtifactTriggerType.UnitDied:
                    ValidateSoulAltar(placed, data, snapshot);
                    return ArtifactSchema.SoulAltar;
                case ArtifactTriggerType.UnitDamaged:
                    ValidateThornHeart(placed, data, snapshot);
                    return ArtifactSchema.ThornHeart;
                default:
                    throw Reject(placed, "trigger", "unsupported Artifact schema", snapshot);
            }
        }

        private static void ValidateHealingChalice(
            ArtifactPlacedEffectSnapshot placed,
            ArtifactEffectData data,
            ArtifactLoadoutSnapshot snapshot)
        {
            ValidateSource(placed, data, ArtifactFactionFilter.Any, snapshot);
            Require(placed, data.location.type == ArtifactLocationType.None,
                "location.type", "Healing Chalice requires no location", snapshot);
            Require(placed, data.condition.type == ArtifactConditionType.None,
                "condition.type", "Healing Chalice requires no condition", snapshot);
            Require(placed, data.condition.comparison == ArtifactComparisonType.LessThan,
                "condition.comparison", "unused comparison must keep its default value", snapshot);
            Require(placed, data.target.type == ArtifactTargetType.MainCharacter,
                "target.type", "Healing Chalice targets MainCharacter", snapshot);
            Require(placed, data.target.faction == ArtifactFactionFilter.Ally,
                "target.faction", "MainCharacter target must be Ally", snapshot);
            Require(placed, data.effect.type == ArtifactEffectType.Heal,
                "effect.type", "Healing Chalice requires Heal", snapshot);
            Require(placed, string.IsNullOrEmpty(data.effect.statId),
                "effect.statId", "statId is unused for Heal", snapshot);
            ValidateUnusedSoulGrowth(placed, data, snapshot);
            Require(placed, data.value.type == ArtifactValueType.Flat,
                "value.type", "Healing Chalice requires Flat", snapshot);
            Require(placed, data.value.basis == ArtifactValueBasis.None,
                "value.basis", "Flat value requires None", snapshot);
            Require(placed,
                data.value.amount >= 0f && data.value.amount == (float)Math.Truncate(data.value.amount),
                "value.amount", "must be a non-negative whole number", snapshot);
            Require(placed,
                data.value.amount == HealingChaliceAmount,
                "value.amount", "Healing Chalice amount must be 3", snapshot);
            Require(placed,
                data.durationTurns == 0,
                "durationTurns", "Healing Chalice is immediate", snapshot);
        }

        private static void ValidateLastStand(
            ArtifactPlacedEffectSnapshot placed,
            ArtifactEffectData data,
            ArtifactLoadoutSnapshot snapshot)
        {
            ValidateSource(placed, data, ArtifactFactionFilter.Any, snapshot);
            Require(placed, data.location.type == ArtifactLocationType.None,
                "location.type", "Last Stand requires no location", snapshot);
            Require(placed, data.condition.type == ArtifactConditionType.CompareUnitCounts,
                "condition.type", "Last Stand compares unit counts", snapshot);
            Require(placed, data.condition.comparison == ArtifactComparisonType.LessThan,
                "condition.comparison", "Last Stand requires LessThan", snapshot);
            Require(placed, data.target.type == ArtifactTargetType.AllMatchingUnits,
                "target.type", "Last Stand targets all matching units", snapshot);
            Require(placed, data.target.faction == ArtifactFactionFilter.Ally,
                "target.faction", "Last Stand targets allies", snapshot);
            Require(placed, data.effect.type == ArtifactEffectType.ModifyStat,
                "effect.type", "Last Stand modifies a stat", snapshot);
            Require(placed, string.Equals(data.effect.statId, AttackStatId, StringComparison.Ordinal),
                "effect.statId", "Last Stand supports only Attack", snapshot);
            ValidateUnusedSoulGrowth(placed, data, snapshot);
            Require(placed, data.value.type == ArtifactValueType.UnitCountDifference,
                "value.type", "Last Stand requires UnitCountDifference", snapshot);
            Require(placed, data.value.basis == ArtifactValueBasis.None,
                "value.basis", "UnitCountDifference requires None", snapshot);
            Require(placed, data.value.amount == LastStandAmount,
                "value.amount", "Last Stand amount must be 5", snapshot);
            Require(placed, data.durationTurns == 0,
                "durationTurns", "Last Stand is active only while equipped", snapshot);
        }

        private static void ValidateSoulAltar(
            ArtifactPlacedEffectSnapshot placed,
            ArtifactEffectData data,
            ArtifactLoadoutSnapshot snapshot)
        {
            ValidateSource(placed, data, ArtifactFactionFilter.Enemy, snapshot);
            Require(placed, data.condition.type == ArtifactConditionType.IsNight,
                "condition.type", "Soul Altar requires IsNight", snapshot);
            Require(placed, data.condition.comparison == ArtifactComparisonType.LessThan,
                "condition.comparison", "unused comparison must keep its default value", snapshot);
            Require(placed, data.target.type == ArtifactTargetType.Collector,
                "target.type", "Soul Altar targets the Collector", snapshot);
            Require(placed, data.target.faction == ArtifactFactionFilter.Ally,
                "target.faction", "Soul Altar collector must be Ally", snapshot);
            Require(placed, data.location.type == ArtifactLocationType.DeathPosition,
                "location.type", "Soul Altar uses DeathPosition", snapshot);
            Require(placed, data.effect.type == ArtifactEffectType.SpawnSoul,
                "effect.type", "Soul Altar requires SpawnSoul", snapshot);
            Require(placed, string.IsNullOrEmpty(data.effect.statId),
                "effect.statId", "statId is unused for SpawnSoul", snapshot);
            ValidateSoulGrowth(placed, data, snapshot);
            Require(placed, data.value.type == ArtifactValueType.Flat,
                "value.type", "Soul Altar keeps an unused Flat value", snapshot);
            Require(placed, data.value.basis == ArtifactValueBasis.None,
                "value.basis", "Soul Altar keeps an unused None basis", snapshot);
            Require(placed, data.value.amount == 0f,
                "value.amount", "Soul Altar value amount is unused and must be zero", snapshot);
            Require(placed, data.durationTurns == 5,
                "durationTurns", "Soul Altar souls last five Unit turns", snapshot);
        }

        private static void ValidateThornHeart(
            ArtifactPlacedEffectSnapshot placed,
            ArtifactEffectData data,
            ArtifactLoadoutSnapshot snapshot)
        {
            ValidateSource(placed, data, ArtifactFactionFilter.Enemy, snapshot);
            Require(placed, data.condition.type == ArtifactConditionType.IsNight,
                "condition.type", "Thorn Heart requires IsNight", snapshot);
            Require(placed, data.condition.comparison == ArtifactComparisonType.LessThan,
                "condition.comparison", "unused comparison must keep its default value", snapshot);
            Require(placed, data.target.type == ArtifactTargetType.LastAttacker,
                "target.type", "Thorn Heart targets this attack's attacker", snapshot);
            Require(placed, data.target.faction == ArtifactFactionFilter.Enemy,
                "target.faction", "Thorn Heart attacker must be Enemy", snapshot);
            Require(placed, data.location.type == ArtifactLocationType.None,
                "location.type", "Thorn Heart requires no location", snapshot);
            Require(placed, data.effect.type == ArtifactEffectType.Damage,
                "effect.type", "Thorn Heart requires Damage", snapshot);
            Require(placed, string.IsNullOrEmpty(data.effect.statId),
                "effect.statId", "statId is unused for Damage", snapshot);
            ValidateUnusedSoulGrowth(placed, data, snapshot);
            Require(placed, data.value.type == ArtifactValueType.Flat,
                "value.type", "Thorn Heart requires Flat", snapshot);
            Require(placed, data.value.basis == ArtifactValueBasis.None,
                "value.basis", "Flat value requires None", snapshot);
            ToNonNegativeWholeInt(placed, data.value.amount, "value.amount", snapshot);
            Require(placed, data.durationTurns == 0,
                "durationTurns", "Thorn Heart is immediate", snapshot);
        }

        private static void ValidateSource(
            ArtifactPlacedEffectSnapshot placed,
            ArtifactEffectData data,
            ArtifactFactionFilter faction,
            ArtifactLoadoutSnapshot snapshot)
        {
            Require(placed, data.source.faction == faction,
                "source.faction", $"requires {faction}", snapshot);
            Require(placed, data.source.category == CategoryKey.None,
                "source.category", "source category is unused", snapshot);
        }

        private static void ValidateSoulGrowth(
            ArtifactPlacedEffectSnapshot placed,
            ArtifactEffectData data,
            ArtifactLoadoutSnapshot snapshot)
        {
            Require(placed, data.effect.soulGrowth.attackBonus == SoulGrowthRules.AttackBonusPerSoul,
                "effect.soulGrowth.attackBonus", "must be one per soul", snapshot);
            Require(placed, data.effect.soulGrowth.maxHealthBonus == SoulGrowthRules.MaxHealthBonusPerSoul,
                "effect.soulGrowth.maxHealthBonus", "must be one per soul", snapshot);
            Require(placed, data.effect.soulGrowth.healAmount >= 0,
                "effect.soulGrowth.healAmount", "must be zero or greater", snapshot);
        }

        private static void ValidateUnusedSoulGrowth(
            ArtifactPlacedEffectSnapshot placed,
            ArtifactEffectData data,
            ArtifactLoadoutSnapshot snapshot)
        {
            Require(placed, data.effect.soulGrowth.attackBonus == 0,
                "effect.soulGrowth", "soul growth is unused", snapshot);
            Require(placed, data.effect.soulGrowth.maxHealthBonus == 0,
                "effect.soulGrowth", "soul growth is unused", snapshot);
            Require(placed, data.effect.soulGrowth.healAmount == 0,
                "effect.soulGrowth", "soul growth is unused", snapshot);
        }

        private static int ToNonNegativeWholeInt(
            ArtifactPlacedEffectSnapshot placed,
            float value,
            string field,
            ArtifactLoadoutSnapshot snapshot)
        {
            double exact = value;
            Require(placed,
                exact >= 0d && exact <= int.MaxValue && exact == Math.Truncate(exact),
                field, "must be a non-negative whole number within Int32 range", snapshot);
            return checked((int)exact);
        }

        private static ICombatEffectRule CreateRule(ArtifactSchema schema, ArtifactEffectData data)
        {
            switch (schema)
            {
                case ArtifactSchema.HealingChalice:
                    return new HealingChaliceRule(HealingChaliceAmount);
                case ArtifactSchema.LastStand:
                    return new LastStandRule(LastStandAmount);
                case ArtifactSchema.SoulAltar:
                    return new SoulAltarRule(
                        new SoulGrowthAmounts(
                            data.effect.soulGrowth.attackBonus,
                            data.effect.soulGrowth.maxHealthBonus,
                            data.effect.soulGrowth.healAmount),
                        data.durationTurns);
                case ArtifactSchema.ThornHeart:
                    return new ThornHeartRule(checked((int)(double)data.value.amount));
                default:
                    throw new ArgumentOutOfRangeException(nameof(schema), schema, null);
            }
        }

        private enum ArtifactSchema
        {
            HealingChalice,
            LastStand,
            SoulAltar,
            ThornHeart,
        }

        private static void RequireDefined<TEnum>(
            ArtifactPlacedEffectSnapshot placed,
            TEnum value,
            string field,
            ArtifactLoadoutSnapshot snapshot)
            where TEnum : struct, Enum
        {
            if (Enum.IsDefined(typeof(TEnum), value) == false)
                throw Reject(placed, field, $"undefined {typeof(TEnum).Name} value", snapshot);
        }

        private static void Require(
            ArtifactPlacedEffectSnapshot placed,
            bool condition,
            string field,
            string reason,
            ArtifactLoadoutSnapshot snapshot)
        {
            if (condition == false) throw Reject(placed, field, reason, snapshot);
        }

        private static ArgumentException Reject(
            ArtifactPlacedEffectSnapshot placed,
            string field,
            string reason,
            ArtifactLoadoutSnapshot snapshot)
        {
            string entryId = string.IsNullOrWhiteSpace(placed.Effect.entryId)
                ? "<empty>"
                : placed.Effect.entryId;
            return new ArgumentException(
                $"Artifact compile rejected: ArtifactId={placed.ArtifactId}, " +
                $"InstanceId={placed.InstanceId}, entryId={entryId}, " +
                $"field={field}, reason={reason}.",
                nameof(snapshot));
        }
    }
}
