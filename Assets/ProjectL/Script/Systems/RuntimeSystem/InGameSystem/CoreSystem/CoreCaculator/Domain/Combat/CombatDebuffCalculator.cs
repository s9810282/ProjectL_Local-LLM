using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat
{
    /// <summary>대상이 받은 디버프 값을 계산합니다. 적용 시점과 공격 종료는 호출자가 결정합니다.</summary>
    internal static class CombatDebuffCalculator
    {
        internal static CharacterDebuffState ApplyIntimidation(CharacterDebuffState current, IntimidationTraitSpec incoming)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (incoming == null) throw new ArgumentNullException(nameof(incoming));
            if (current.Intimidation != null) return current;
            return new CharacterDebuffState(new IntimidationDebuffState(incoming.AttackReductionPercent),
                current.DefenseReduction, current.HuntersMark, current.Disarm, current.AnkleBreaker, current.ArmorBreak);
        }
        internal static CharacterDebuffState ClearIntimidation(CharacterDebuffState current)
        {
            if (current == null)
                throw new ArgumentNullException(nameof(current));

            return current.Intimidation == null ? current : new CharacterDebuffState(
                defenseReduction: current.DefenseReduction, huntersMark: current.HuntersMark,
                disarm: current.Disarm, ankleBreaker: current.AnkleBreaker, armorBreak: current.ArmorBreak);
        }

        internal static int EvaluateAttackPower(int attackPower, CharacterDebuffState current)
        {
            if (current == null)
                throw new ArgumentNullException(nameof(current));
            if (attackPower < 0)
                throw new ArgumentOutOfRangeException(nameof(attackPower));

            return AppliedStatusRules.ApplyAttackDecrease(attackPower, CharacterStatusCompatibility.Import(current, null));
        }

        internal static CharacterDebuffState ApplyDefenseReduction(CharacterDebuffState current, int percent, bool ownsActiveTurn)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (percent == 0) return current;
            return new CharacterDebuffState(current.Intimidation,
                defenseReduction: new DefenseReductionDebuffState(percent, ownsActiveTurn ? 2 : 1), huntersMark: current.HuntersMark,
                disarm: current.Disarm, ankleBreaker: current.AnkleBreaker, armorBreak: current.ArmorBreak);
        }

        internal static int EvaluatePhysicalDefense(int defense, CharacterDebuffState current)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (defense < 0) throw new ArgumentOutOfRangeException(nameof(defense));
            return checked((int)((long)defense * (100 - (current.DefenseReduction?.ReductionPercent ?? 0)) / 100));
        }

    }
}
