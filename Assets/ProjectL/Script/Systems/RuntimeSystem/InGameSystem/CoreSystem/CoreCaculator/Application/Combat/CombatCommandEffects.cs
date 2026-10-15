using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>Buffer 조회값을 기존 효과 규칙의 입력과 최종 조회값으로 변환한다.</summary>
    internal static class CombatCommandEffects
    {
        internal static EffectRuleBinding[] Capture(CharacterCombatFacts character, CombatAttackProfile profile)
        {
            var registry = TraitRegistry.CreateDefault();
            var bindings = new List<EffectRuleBinding>();
            var statuses = character.Stats.TraitStatuses.ToArray();
            if (statuses.Length != profile.Definition.Traits.Length)
                throw new InvalidOperationException("특성 정의와 공개 상태의 개수가 다릅니다.");
            foreach (var spec in profile.Definition.Traits.ToArray())
            {
                var status = statuses.Single(s => s.Key == spec.TraitKey);
                var registration = registry.GetRequiredRegistration(spec.TraitKey);
                var definition = registration.TraitDefinitionFactory.BuildDefinition(spec);
                bindings.Add(new EffectRuleBinding(TraitEffectOrigin.Create(character.EntityId, spec.TraitKey),
                    registration.EffectRuleFactory(definition), Capture(status)));
            }
            var category = character.Stats.Category;
            if (category.Key != profile.Definition.CategoryKey)
                throw new InvalidOperationException("캐릭터와 정의의 Category가 다릅니다.");
            if (category.Key == CategoryKey.Heavy)
                bindings.Add(new EffectRuleBinding(CategoryEffectOrigin.Create(character.EntityId, category.Key,
                    CategoryRuleKey.DirectionalArmor), new DirectionalArmorCategoryRule(new DirectionalArmorCategoryRuleDefinition(
                    category.DamageReductionPercent.Value, category.DefenseFacing.Value)),
                    new DirectionalArmorEffectStateSnapshot(category.DefenseFacing.Value, category.MovedThisTurn.Value)));
            if (category.Key == CategoryKey.Rider)
                bindings.Add(new EffectRuleBinding(CategoryEffectOrigin.Create(character.EntityId, category.Key,
                    CategoryRuleKey.ManeuverRefund), new ManeuverRefundCategoryRule(new ManeuverRefundCategoryRuleDefinition()),
                    new ManeuverRefundEffectStateSnapshot(category.RefundUsedThisTurn.Value)));
            if (category.Key == CategoryKey.Cannon)
                bindings.Add(new EffectRuleBinding(CategoryEffectOrigin.Create(character.EntityId, category.Key,
                    CategoryRuleKey.SafeBombardment),
                    CategoryRuleFactories.CreateRule(new SafeBombardmentCategoryRuleDefinition()),
                    StatelessEffectStateSnapshot.Instance));
            return bindings.ToArray();
        }

        private static IEffectStateSnapshot Capture(TraitStatusSnapshot status) => status switch
        {
            FocusTraitStatusSnapshot f => new FocusEffectStateSnapshot(f.ActiveStacks, f.GainScheduled, f.MovedThisTurn),
            MoveAttackDiscountTraitStatusSnapshot d => new MoveAttackDiscountEffectStateSnapshot(d.FirstMoveConsumed, d.DiscountArmed),
            PiercingFalloffTraitStatusSnapshot => new PiercingFalloffEffectStateSnapshot(),
            ThornFieldTraitStatusSnapshot => new ThornFieldEffectStateSnapshot(),
            KnockbackTraitStatusSnapshot => new KnockbackEffectStateSnapshot(),
            DistanceDamageScaleTraitStatusSnapshot => new DistanceDamageScaleEffectStateSnapshot(),
            KillChainAttackTraitStatusSnapshot => new KillChainAttackEffectStateSnapshot(),
            _ => throw new InvalidOperationException("공개 특성 상태를 효과 입력으로 변환할 수 없습니다.")
        };

        internal static UnitStatsSnapshot Apply(UnitStatsSnapshot source, CombatEffectPlan plan)
        {
            var traits = source.TraitStatuses.ToArray();
            var category = source.Category;
            foreach (var change in plan.Changes)
            {
                if (change.Origin.Kind == EffectOwnerKind.Trait)
                {
                    int index = Array.FindIndex(traits, t => (int)t.Key == change.Origin.DefinitionKey);
                    if (index < 0 || !Capture(traits[index]).ValueEquals(change.ExpectedState))
                        throw new InvalidOperationException("효과 변경의 특성 원본이 다릅니다.");
                    traits[index] = change.FinalState switch
                    {
                        FocusEffectStateSnapshot f => new FocusTraitStatusSnapshot(f.ActiveStacks, f.GainScheduled, f.MovedThisTurn),
                        MoveAttackDiscountEffectStateSnapshot d => new MoveAttackDiscountTraitStatusSnapshot(d.FirstMoveConsumed, d.DiscountArmed),
                        _ => throw new InvalidOperationException("변경 가능한 특성 상태가 아닙니다.")
                    };
                }
                else if (change.FinalState is DirectionalArmorEffectStateSnapshot armor)
                    category = CategorySnapshot.CreateHeavy(armor.DefenseFacing, armor.MovedThisTurn, category.DamageReductionPercent.Value);
                else if (change.FinalState is ManeuverRefundEffectStateSnapshot refund)
                    category = CategorySnapshot.CreateRider(refund.RefundUsedThisTurn);
                else throw new InvalidOperationException("변경 가능한 Category 상태가 아닙니다.");
            }
            return With(source, traits: traits, category: category);
        }

        internal static UnitStatsSnapshot With(UnitStatsSnapshot s, int? ap = null,
            TraitStatusSnapshot[] traits = null, CategorySnapshot? category = null, int? remainingMoveRange = null,
            MoveMethod? moveMethod = null) => new(
            moveMethod ?? s.MoveMethod, s.MoveRange, s.MoveCost, s.AppointRange, s.AttackPower,
            s.PhysicalDefense, s.MaxActionCost, s.AttackCost, remainingMoveRange ?? s.RemainingMoveRange, ap ?? s.CurrentActionCost,
            s.Priority, s.Delay, traits ?? s.TraitStatuses.ToArray(), category ?? s.Category,
            s.ArtifactAttackBonus, s.EffectiveAttackPower, s.SoulAttackBonus, s.SoulMaxHealthBonus, s.PositionAttackBonus);

        internal static void AppendActivations(CombatEffectPlan plan, List<TraitActivationSnapshot> traits,
            List<CategoryActivationSnapshot> categories)
        {
            foreach (var a in plan.Activations)
            {
                var id = new EntityId(a.Origin.OwnerInstanceId);
                EntityId? target = a.TargetId.IsEmpty ? null : a.TargetId;
                if (a.Origin.Kind == EffectOwnerKind.Trait)
                    traits.Add(new TraitActivationSnapshot(id, target, TraitEffectOrigin.GetRequiredTraitKey(a.Origin, id),
                        (TraitActivationKind)a.Code, a.Magnitude));
                else if (a.Origin.Kind == EffectOwnerKind.Category)
                    categories.Add(new CategoryActivationSnapshot(id, target, CategoryEffectOrigin.GetRequiredCategoryKey(a.Origin, id),
                        CategoryEffectOrigin.GetRequiredRuleKey(a.Origin, id), (CategoryActivationKind)a.Code, a.Magnitude));
            }
        }
    }
}
