using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules
{
    /// <summary>피해자 자신의 실제 턴 종료에서만 방어력 감소 수명을 줄입니다.</summary>
    internal sealed class DefenseReductionStatusRule : ICharacterStatusLifecycleRule
    {
        public CharacterDebuffState OnTurnEnded(CharacterDebuffState state)
        {
            var active = state.DefenseReduction;
            if (active == null)
                return state;
            return new CharacterDebuffState(state.Intimidation, defenseReduction: active.RemainingOwnTurnEnds == 1 ? null :
                new DefenseReductionDebuffState(active.ReductionPercent, active.RemainingOwnTurnEnds - 1), huntersMark: state.HuntersMark,
                disarm: state.Disarm, ankleBreaker: state.AnkleBreaker, armorBreak: state.ArmorBreak);
        }
    }
}
