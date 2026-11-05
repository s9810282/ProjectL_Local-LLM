using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    public sealed class EnhancementActionCostTests
    {
        private static readonly IMovementTileRule[] NoRules = Array.Empty<IMovementTileRule>();
        internal static CombatCommandInput Input(bool enhanced, TraitSpec selectedTrait = null,
            EntityKey characterKey = EntityKey.Unit_Akimbo)
        {
            var seed = CombatCommandTestData.Input();
            TraitKey selectedKey = selectedTrait?.TraitKey ?? TraitKey.None;
            TraitSpec[] traits = { new KillChainAttackTraitSpec() };
            var basis = CombatCommandTestData.Spec(characterKey, 10, CategoryKey.Main, traits);
            var definitions = new List<TraitGrowthDefinition>();
            var steps = new List<TraitGrowthStepSpec>();
            var selections = new List<TraitGrowthSelection>();
            if (selectedTrait == null)
            {
                // 수치가 없는 고유 특성의 강화 이력으로 준비와 소유 통계의 일치를 검증한다.
                definitions.Add(new TraitGrowthDefinition(traits[0], new KillChainAttackTraitSpec(),
                    "기본", "기본", "강화", "강화"));
                steps.Add(new TraitGrowthStepSpec(1, 2, TraitGrowthKind.Enhance));
                selections.Add(new TraitGrowthSelection(1, TraitKey.KillChainAttack));
            }
            else
            {
                definitions.Add(new TraitGrowthDefinition(selectedTrait, null, "획득", "획득", "", ""));
                steps.Add(new TraitGrowthStepSpec(1, 2, TraitGrowthKind.Acquire));
                selections.Add(new TraitGrowthSelection(1, selectedKey));

            }
            var growth = new TraitGrowthCatalog(TraitGrowthCatalog.Disabled.Categories.Select(c => c.Category == basis.CategoryKey
                ? new CategoryTraitProgressionSpec(c.Category, steps, definitions.Select(d => d.Key).ToArray()) : c).ToArray(), definitions);
            var mainSpec = new UnitSpec(EntityKey.Unit_Gravekeeper, UnitRole.MainCharacter, 1, 1, new[] { CellCoord.Zero },
                20, MoveMethod.LiftAndPlace, 1, CategoryKey.Main, 5,
                0, 10, Array.Empty<TraitSpec>(), UnitOwnerEligibility.PlayerOnly);
            var owned = new[] {
                new OwnedCharacterRecord(new CharacterInstanceId(1), mainSpec.Key, CharacterOwnershipRole.Main),
                new OwnedCharacterRecord(new CharacterInstanceId(2), basis.Key, CharacterOwnershipRole.Sub, 2, 0,
                    enhanced ? new OwnedTraitGrowth(selections) : null),
                new OwnedCharacterRecord(new CharacterInstanceId(3), basis.Key, CharacterOwnershipRole.Sub, 2) };
            var main = new MainCharacterPersistentState(20, 20, 5);
            var souls = owned.ToDictionary(o => o.Id, _ => 0);
            var categories = new[] {
                new CategorySpec(CategoryKey.Main,Array.Empty<CategoryRuleSpec>(),3),
                new CategorySpec(CategoryKey.Rider,Array.Empty<CategoryRuleSpec>(),4),
                new CategorySpec(CategoryKey.Heavy,Array.Empty<CategoryRuleSpec>(),3),
                new CategorySpec(CategoryKey.Cannon,new CategoryRuleSpec[] { new SafeBombardmentCategoryRuleSpec() },2) };
            var init = new BattleInitialization(new StageConfig(8, 8, 8, 2, 2, 42, Array.Empty<InitialEnemyPlacementSpec>()),
                CellCoord.Zero, Rotation4.R0, new[] { mainSpec, basis }, Array.Empty<StructureSpec>(), categories)
                .WithTraitGrowth(growth);
            var prepared = BattlePreparationCalculator.Prepare(init, owned, main, souls,
                new InitialDrawPlan(new[] { owned[1].Id, owned[2].Id }), Array.Empty<CharacterCombatFacts>());
            var stats = OwnedCharacterStatsCalculator.Calculate(UnitSpecTestFactory.Profiles(init.Units), categories, owned, main, souls, init.TraitGrowth);
            foreach (var c in prepared.Characters)
            {
                var query = stats.Single(s => s.OwnedId == c.OwnedId);
                Assert.That((c.Stats.Category.Key, c.Stats.AttackPower, c.Health.MaxHealth),
                    Is.EqualTo((query.Stats.CategoryKey, query.Stats.AttackPower, query.Stats.MaxHealth)));
            }
            var actor = prepared.Characters.Single(c => c.OwnedId == owned[1].Id);
            CharacterCombatFacts Place(CharacterCombatFacts c, CellCoord cell) => new(c.EntityId, c.OwnedId,
                c.CharacterKey, c.Owner, c.OwnershipRole, CharacterRuntimeArea.Field, c.Health,
                c.Stats, null, cell, Rotation4.R0, new[] { cell }, UnitActionUse.Available);
            var characters = prepared.Characters.Select(c => c.EntityId == actor.EntityId ? Place(c, new CellCoord(1, 3))
                : c.OwnershipRole == CharacterOwnershipRole.Main ? Place(c, new CellCoord(7, 7)) : c).ToArray();
            characters = characters.Concat(new[] { CombatCommandTestData.Character(100, EntityKey.Unit_Demon, new CellCoord(4, 3), Owner.Enemy) }).ToArray();
            var method = seed.Profiles[0].Method;
            var profiles = seed.Profiles.Where(p => p.Definition.Key != EntityKey.Unit_Akimbo && p.Definition.Key != EntityKey.Unit_MainCharacter)
                .Append(new CombatAttackProfile(mainSpec, new TargetingMethodSpec(TargetingShape.RadialSquare,
                    TargetingChoiceMode.Single, TargetingSplash.None, 0)))
                .Append(new CombatAttackProfile(basis, method)).ToArray();
            var state = new CombatCommandState(8,8,CombatFlowPhase.PlayerInput,false,                Array.Empty<StructureCombatFacts>(),Array.Empty<ThornFieldCellSnapshot>(),turn: new CombatTurnFacts(StagePhase.Combat, controllerPhase: (CombatFlowPhase.PlayerInput) == CombatFlowPhase.Automatic ? CombatControllerPhase.Enemy : CombatControllerPhase.Player, controllerTurnSequence: 1));
            return new CombatCommandInput(1, CombatCommandKind.Attack, actor.EntityId, AttackAim.ForCell(new CellCoord(4, 3)),
                state, characters, OwnedUnitSpecResolver.BuildProfiles(profiles, owned, init.TraitGrowth), main, owned, souls,
                seed.Souls, seed.Artifacts,
                expectedBattleInstanceId: 0, expectedControllerTurnSequence: (state).Turn.ControllerTurnSequence);
        }


        [Test]
        public void AkimboWithoutDiscount_StatsPreviewAiAndExecutionAgree()
        {
            var input = Input(true);
            var range = CombatCommandCalculator.GetAttackRange(input);
            var policy = new EnemyAiPolicy(new HostilityPolicy(), NoRules);
            Assert.That(policy.TrySelectAttackAim(input, range, out var aim), Is.True);
            input = CombatAttackQueries.WithAim(input, aim);
            var preview = CombatCommandCalculator.Preview(input, NoRules);
            var result = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(preview.CanAttack, Is.True);
            var poor = CombatCommandTestData.With(input, state: input.State.With());
            Assert.That(policy.TrySelectAttackAim(poor, CombatCommandCalculator.GetAttackRange(poor), out _), Is.True);
            Assert.That(CombatCommandCalculator.Preview(poor, NoRules).CanAttack, Is.True);
            Assert.That(CombatCommandCalculator.Calculate(poor, NoRules).Accepted, Is.True);
        }

        [Test]
        public void CategoryGrowthRemainsOriginal_AndMovementUsesTheMoveThenAttackRule()
        {
            var input = Input(true);
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var ordinary = input.Characters.Single(c => c.OwnedId == new CharacterInstanceId(3));
            Assert.That(actor.Stats.Category.Key, Is.EqualTo(CategoryKey.Main));
            Assert.That((actor.Stats.AttackPower, actor.Health.MaxHealth), Is.EqualTo((ordinary.Stats.AttackPower, ordinary.Health.MaxHealth)));
            Assert.That(actor.Stats.TraitStatuses.ToArray().Select(t => t.Key),
                Is.EqualTo(new[] { TraitKey.KillChainAttack }));
            var path = new MovementPath(new[] { actor.Anchor.Value, new CellCoord(2, 3) });
            var move = CombatCommandComposition.Intent(input, CombatCommandKind.Move, path: path);
            var moved = CombatCommandCalculator.Calculate(move, NoRules);
            Assert.That(moved.Accepted, Is.True, moved.Reason);
            var afterMove = CombatCommandComposition.Project(moved);
            // 이동 뒤에도 공격할 수 있으며 개별 사용 상태가 이어진다.
            var attack = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(afterMove, CombatCommandKind.Attack, input.Aim), NoRules);
            Assert.That(attack.Accepted, Is.True, attack.Reason);
            Assert.That(attack.Attack.Value.ActionUseSettlement.BeforeActionUse, Is.EqualTo(UnitActionUse.Moved));
        }

        [TestCase(TraitKey.StationaryFire)]
        [TestCase(TraitKey.TrackingShot)]
        [TestCase(TraitKey.FocusedBombardment)]
        public void SelectedDamageTrait_SurvivesRealPreparation_AndSameTypeOrdinaryRemainsBase(TraitKey key)
        {
            TraitSpec spec = key == TraitKey.StationaryFire ? new StationaryFireTraitSpec(100)
                : key == TraitKey.TrackingShot ? new TrackingShotTraitSpec(50, 100)
                : new FocusedBombardmentTraitSpec(1, 100);
            var characterKey = EntityKey.Unit_Ballista;
            foreach (bool enhanced in new[] { false, true })
            {
                var input = Input(enhanced, spec, characterKey);
                var victim = CombatTraitTestFixtures.Stats(CombatCommandTestData.With(
                    input.Characters.Single(c => c.EntityId.Value == 100), position: new CellCoord(2, 3)), defense: 50);
                input = CombatAttackQueries.WithAim(CombatCommandTestData.With(input,
                    input.Characters.Select(c => c.EntityId == victim.EntityId ? victim : c).ToArray()), AttackAim.ForCell(victim.Anchor.Value));
                var result = CombatCommandCalculator.Calculate(input, NoRules);
                Assert.That(result.Accepted, Is.True, result.Reason);
                Assert.That(result.Attack.Value.Hits.ToArray().Single(h => h.TargetId == victim.EntityId).DamageDealt,
                    Is.EqualTo(enhanced ? 10 : 5));
                var ordinary = input.Characters.Single(c => c.OwnedId == new CharacterInstanceId(3));
                Assert.That(ordinary.Stats.TraitStatuses.ToArray().Any(t => t.Key == spec.TraitKey), Is.False);
            }
        }
    }
}
