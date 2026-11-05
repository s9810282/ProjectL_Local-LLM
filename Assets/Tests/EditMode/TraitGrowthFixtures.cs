using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.Core
{
    internal static class TraitGrowthFixtures
    {
        internal static readonly TraitSpec[] Normal = { new KnockbackTraitSpec(1), new SoloActionTraitSpec(1, 1, 0, 1),
            new HuntersMarkTraitSpec(20, 1), new VanguardTraitSpec(2, 1), new TrackingShotTraitSpec(50, 10), new GroundCompactionTraitSpec(1, 1) };
        internal static readonly TraitGrowthDefinition[] Definitions = Normal.Select(t => new TraitGrowthDefinition(t,
            t.TraitKey == TraitKey.Knockback ? new KnockbackTraitSpec(3) : t.TraitKey == TraitKey.SoloAction ? new SoloActionTraitSpec(3, 1, 0, 1) :
            t.TraitKey == TraitKey.HuntersMark ? new HuntersMarkTraitSpec(40, 1) : t.TraitKey == TraitKey.Vanguard ? new VanguardTraitSpec(4, 2) : null,
            "normal", "description", "enhanced", "enhanced description")).ToArray();
        internal static TraitGrowthCatalog Catalog(TraitGrowthStepSpec[] steps = null, CategoryKey category = CategoryKey.Heavy,
            TraitGrowthDefinition[] definitions = null) => new(TraitGrowthCatalog.Disabled.Categories.Select(c => c.Category == category
                ? new CategoryTraitProgressionSpec(category, steps ?? new[] { new TraitGrowthStepSpec(10, 2, TraitGrowthKind.Acquire),
                    new TraitGrowthStepSpec(20, 2, TraitGrowthKind.Enhance) }, (definitions ?? Definitions).Select(d => d.Key).ToArray()) : c).ToArray(), definitions ?? Definitions);
        internal static CombatAttackProfile Profile(CategoryKey category = CategoryKey.Heavy, params TraitSpec[] native) => new(
            new UnitSpec(category == CategoryKey.Main ? EntityKey.Unit_Akimbo : EntityKey.Unit_Shielder,
                category == CategoryKey.Main ? UnitRole.MainCharacter : UnitRole.Regular, 1, 1, new[] { CellCoord.Zero },
                10, MoveMethod.LiftAndPlace, 2, category, 5, 1, 1, native, UnitOwnerEligibility.PlayerOnly),
            new TargetingMethodSpec(TargetingShape.CardinalLine, TargetingChoiceMode.Single, TargetingSplash.None, 0));
        internal static OwnedCharacterRecord Owned(CombatAttackProfile profile, int level = 8, OwnedTraitGrowth growth = null, int id = 2) =>
            new(new CharacterInstanceId(id), profile.Definition.Key, profile.Definition.Role == UnitRole.MainCharacter ? CharacterOwnershipRole.Main : CharacterOwnershipRole.Sub,
                level, 7, growth);
        internal static TraitGrowthOffer Offer(OwnedCharacterRecord owned, TraitGrowthStepSpec step, params TraitKey[] keys) => new(
            new RunId(new Guid("11111111-2222-3333-4444-555555555555")), 1, owned.Id, step.StepId, step.Kind,
            keys.Select(k => new TraitGrowthChoice(k, step.Kind, "name", "description")).ToArray());
        internal static TraitGrowthChoiceRequest Request(TraitGrowthOffer offer, TraitKey key) => new(offer.RunId, offer.OfferId, offer.OwnedId, offer.StepId, key);
    }
}
