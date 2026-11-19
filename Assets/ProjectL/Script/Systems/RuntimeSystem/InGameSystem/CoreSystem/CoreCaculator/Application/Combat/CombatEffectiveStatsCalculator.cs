using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>현재 기본값과 보유 기여분에 대상의 디버프를 적용합니다.</summary>
    public static class CombatEffectiveStatsCalculator
    {
        /// <summary>임시 기여를 포함한 공개 공격력에 보급 기여와 위압을 반영합니다. 피해 전용 배율은 포함하지 않습니다.</summary>
        public static int AttackPower(CharacterCombatFacts character)
        {
            if (character == null) throw new ArgumentNullException(nameof(character));
            int power = checked(character.Stats.EffectiveAttackPower +
                character.HeldEnvironment.SuppliesAttackBonus);
            return AppliedStatusRules.ApplyAttackDecrease(power, character.StatusEffects);
        }

        /// <summary>기존 원본에서 특성 공격 기여분을 계산합니다.</summary>
        public static int TraitAttackBonus(CharacterCombatFacts character, int hunterAttackBonus = 0)
        {
            if (character == null) throw new ArgumentNullException(nameof(character));
            if (hunterAttackBonus < 0) throw new ArgumentOutOfRangeException(nameof(hunterAttackBonus));
            return checked(AppliedStatusRules.AttackIncrease(character.StatusEffects) + hunterAttackBonus +
                AccelerationTraitRule.Held(character.Stats) + RampageTraitRule.AttackBonus(character));
        }

        /// <summary>특성 기여분만 다시 계산하며 기본·영혼·아이템·위치 값은 보존합니다.</summary>
        public static CharacterCombatFacts ProjectTraitAttack(CharacterCombatFacts character, int hunterAttackBonus = 0)
        {
            int bonus = TraitAttackBonus(character, hunterAttackBonus);
            return character.With(stats: character.Stats.With(traitAttackBonus: bonus));
        }

        /// <summary>출처별 기여분을 추가하고 기존 원본에서 공격 투영을 다시 계산합니다.</summary>
        internal static CharacterCombatFacts GrantTraitAttack(CharacterCombatFacts character, AttackIncreaseStatus grant, int hunterAttackBonus)
        {
            if (character == null) throw new ArgumentNullException(nameof(character));
            if (grant == null) throw new ArgumentNullException(nameof(grant));
            return ProjectTraitAttack(character.WithStatusEffects(AppliedStatusRules.Apply(character.StatusEffects, grant)), hunterAttackBonus);
        }

        public static int PhysicalDefense(CharacterCombatFacts character)
        {
            if (character == null) throw new ArgumentNullException(nameof(character));
            return AppliedStatusRules.ApplyDefenseDecrease(ApplyItemPenalty(character, ItemStatKind.PhysicalDefense, checked(character.Stats.PhysicalDefense + character.TraitEffects.CohesionDefenseBonus), 0), character.StatusEffects);
        }

        public static int PhysicalDefense(CombatCommandInput input, CharacterCombatFacts character)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            return PhysicalDefense(input, character, input.Characters);
        }

        internal static int PhysicalDefense(CombatCommandInput input, CharacterCombatFacts character,
            System.Collections.Generic.IReadOnlyList<CharacterCombatFacts> characters)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (character == null) throw new ArgumentNullException(nameof(character));
            if (characters == null) throw new ArgumentNullException(nameof(characters));
            int bonus = CohesionDefenseBonus(input, character, characters);
            return AppliedStatusRules.ApplyDefenseDecrease(ApplyItemPenalty(character, ItemStatKind.PhysicalDefense,
                checked(character.Stats.PhysicalDefense + bonus), 0), character.StatusEffects);
        }

        internal static int CohesionDefenseBonus(CombatCommandInput input, CharacterCombatFacts character,
            System.Collections.Generic.IReadOnlyList<CharacterCombatFacts> characters, CombatProfileReadSet profileReads = null)
        {
            if (!global::ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.TraitStatusScan.Has(character.Stats.TraitStatuses, TraitKey.Cohesion)) return 0;
            var spec = CombatCommandCalculator.RequiredProfile(input, character, profileReads).Definition.Traits.ToArray().OfType<CohesionTraitSpec>().Single();
            return new CohesionTraitRule().Defense(new CohesionTraitDefinition(spec.DefensePerAdjacentAlly, spec.ShieldAmountPerAlly,
                spec.RadiusCells, spec.ResetEveryControllerTurnEnds), character, characters);
        }

        internal static CombatEffectiveStatsSnapshot Snapshot(CharacterCombatFacts character) =>
            new(AttackPower(character), PhysicalDefense(character));

        internal static int ItemReduction(CharacterCombatFacts before, CharacterCombatFacts after, ItemStatKind stat) =>
            stat == ItemStatKind.Priority ? Priority(before) - Priority(after) : PhysicalDefense(before) - PhysicalDefense(after);

        private static int ApplyItemPenalty(CharacterCombatFacts character, ItemStatKind stat, int value, int minimum)
        {
            // 단계마다 하한을 적용하므로 많은 효과를 합산해도 오버플로가 발생하지 않는다.
            foreach (var effect in character.Items.Effects)
                if (effect.Stat == stat) value = (int)Math.Max(minimum, (long)value - effect.Magnitude);
            return Math.Max(minimum, value);
        }

        public static int Priority(CharacterCombatFacts character)
        {
            if (character == null) throw new ArgumentNullException(nameof(character));
            return character.Stats.Priority;
        }
    }
}
