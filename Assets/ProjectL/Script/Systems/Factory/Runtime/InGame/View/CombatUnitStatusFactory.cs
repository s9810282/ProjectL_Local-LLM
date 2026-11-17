using ProjectL.Systems.DataBaseSystem.Data.View.Table.Rockfall.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;

namespace ProjectL.Systems.Factory.Runtime.InGame.View
{
    /// <summary>효과별 표시 변환과 기존 외형 조회만 조립합니다. 실행 서비스는 등록하지 않습니다.</summary>
    internal static class CombatUnitStatusFactory
    {
        internal static CombatUnitStatuses Create(IUnitStatusVisualTableQuery visuals, ITraitVisualTableQuery traits, RockfallVisualData rockfall)
        {
            var formatters = new CombatEffectFormatters(new ICombatEffectFormatter[]
            {
                new CombatEffectFormatter<AttackIncreaseEffectSnapshot>(StatusEffectKey.AttackIncrease, s => s.TotalAmount, s => s.AppliedAmount, s => s.HeldForAttackAmount),
                new CombatEffectFormatter<AttackDecreaseEffectSnapshot>(StatusEffectKey.AttackDecrease, s => s.ReductionPercent),
                new CombatEffectFormatter<DefenseIncreaseEffectSnapshot>(StatusEffectKey.DefenseIncrease, s => s.Amount),
                new CombatEffectFormatter<DefenseDecreaseEffectSnapshot>(StatusEffectKey.DefenseDecrease, s => s.EffectiveDefense),
                new CombatEffectFormatter<ShieldEffectSnapshot>(StatusEffectKey.Shield, s => s.RemainingEligibleBudget),
                new CombatEffectFormatter<ActionRestrictionEffectSnapshot>(StatusEffectKey.Disarm),
                new CombatEffectFormatter<ActionRestrictionEffectSnapshot>(StatusEffectKey.Bind),
                // 기절은 이동·공격·스킬을 모두 막는 행동 제한입니다(관통·죽음의 손 View 설계 §3.10, 거는 곳 = 스킬 단죄).
                new CombatEffectFormatter<ActionRestrictionEffectSnapshot>(StatusEffectKey.Stun),
                new CombatEffectFormatter<WeaknessExposureEffectSnapshot>(StatusEffectKey.WeaknessExposure, s => s.EffectiveIncreasePercent),
                new CombatEffectFormatter<TauntedEffectSnapshot>(StatusEffectKey.Taunted),
                new CombatEffectFormatter<FireEffectSnapshot>(StatusEffectKey.Fire, s => s.Damage, s => s.RemainingAffectedSideStarts),
                new CombatEffectFormatter<BleedingEffectSnapshot>(StatusEffectKey.Bleeding, s => s.Stacks.Count, s => CombatStatusDescriptionFormatter.BleedingDamage(s.Stacks), s => s.RemainingAffectedSideStarts),
                new CombatEffectFormatter<MovementRangeDecreaseEffectSnapshot>(StatusEffectKey.MovementRangeDecrease, s => s.Magnitude),
                new CombatEffectFormatter<RampageEffectSnapshot>(StatusEffectKey.Rampage, s => s.AttackBonus, s => s.MovementBonus),
                new CombatEffectFormatter<ReconstructionEffectSnapshot>(StatusEffectKey.Reconstructing, s => s.RemainingOwnTurnStarts)
            }, visuals);
            return new CombatUnitStatuses(visuals, formatters, new CombatStatusDescriptionFormatter(traits, rockfall));
        }
    }
}
