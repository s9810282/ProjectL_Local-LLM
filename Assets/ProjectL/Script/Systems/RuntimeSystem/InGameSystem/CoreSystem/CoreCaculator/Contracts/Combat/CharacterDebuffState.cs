namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>보유 특성과 분리된 피격 대상의 불변 디버프 값입니다.</summary>
    public sealed class CharacterDebuffState
    {
        public static CharacterDebuffState Empty { get; } = new CharacterDebuffState();
        public IntimidationDebuffState Intimidation { get; }
        public DefenseReductionDebuffState DefenseReduction { get; }
        public HuntersMarkState HuntersMark { get; }
        public DisarmState Disarm { get; }
        public AnkleBreakerState AnkleBreaker { get; }
        public ArmorBreakState ArmorBreak { get; }

        public CharacterDebuffState(IntimidationDebuffState intimidation = null,
            DefenseReductionDebuffState defenseReduction = null, HuntersMarkState huntersMark = null,
            DisarmState disarm = null, AnkleBreakerState ankleBreaker = null, ArmorBreakState armorBreak = null)
        {
            Intimidation = intimidation;
            DefenseReduction = defenseReduction;
            HuntersMark = huntersMark;
            Disarm = disarm;
            AnkleBreaker = ankleBreaker;
            ArmorBreak = armorBreak;
        }

        public CharacterDebuffState WithRestrictions(DisarmState disarm, AnkleBreakerState ankleBreaker) =>
            new CharacterDebuffState(Intimidation, DefenseReduction, HuntersMark, disarm, ankleBreaker, ArmorBreak);

        public CharacterDebuffState WithArmorBreak(ArmorBreakState armorBreak) =>
            new CharacterDebuffState(Intimidation, DefenseReduction, HuntersMark, Disarm, AnkleBreaker, armorBreak);

        public bool SameValues(CharacterDebuffState other) =>
            other != null && (Intimidation == null ? other.Intimidation == null : Intimidation.SameValues(other.Intimidation)) &&
            (DefenseReduction == null ? other.DefenseReduction == null : DefenseReduction.SameValues(other.DefenseReduction)) &&
            (HuntersMark == null ? other.HuntersMark == null : HuntersMark.SameValues(other.HuntersMark)) &&
            (Disarm == null ? other.Disarm == null : Disarm.SameValues(other.Disarm)) &&
            (AnkleBreaker == null ? other.AnkleBreaker == null : AnkleBreaker.SameValues(other.AnkleBreaker)) &&
            (ArmorBreak == null ? other.ArmorBreak == null : ArmorBreak.SameValues(other.ArmorBreak));
    }
}
