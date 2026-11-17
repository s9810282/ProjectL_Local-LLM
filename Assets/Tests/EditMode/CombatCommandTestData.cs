using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    internal static class CombatCommandTestData
    {
        /// <summary>유효한 지정 의도를 칸 집합으로 바꾼다. 지정이 실패하면 테스트를 실패시킨다.</summary>
        internal static AttackCellSelection ResolvedCells(TargetingMethodSpec method, int range, CellCoord origin, AttackAim aim)
        {
            bool resolved = new AttackSelectionResolver().TryResolve(method, range, origin, aim, out var cells, out var failure);
            Assert.That(resolved, Is.True, failure.ToString());
            return cells;
        }

        internal static CombatCommandInput Input(TargetingMethodSpec method = null, AttackAim? aim = null,
            bool empty = false, bool friendly = false, CategoryKey category = CategoryKey.Main)
        {
            method ??= new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.Radial, 70);
            var main = new MainCharacterPersistentState(100, 100, 1);
            var characters = new List<CharacterCombatFacts>
            {
                With(Character(1, EntityKey.Unit_Akimbo, new CellCoord(1, 3), Owner.Player, 20),
                    category: CategorySnapshot.CreateStateless(category)),
                Character(10, EntityKey.Unit_MainCharacter, new CellCoord(7, 7), Owner.Player, 1, main: true),
            };
            if (!empty)
            {
                characters.Add(Character(2, EntityKey.Unit_Demon, friendly ? new CellCoord(3, 3) : new CellCoord(4, 3), friendly ? Owner.Player : Owner.Enemy));
                characters.Add(Character(3, EntityKey.Unit_Demon, new CellCoord(4, 4), Owner.Enemy));
                characters.Add(Character(4, EntityKey.Unit_Demon, new CellCoord(5, 3), Owner.Enemy));
            }
            var state = new CombatCommandState(8,8,CombatFlowPhase.PlayerInput,                Array.Empty<StructureCombatFacts>(),new CombatTurnFacts(StagePhase.Combat, controllerPhase: CombatControllerPhase.Player, controllerTurnSequence: 1));
            var profiles = new[]
            {
                new CombatAttackProfile(Spec(EntityKey.Unit_Akimbo, 20, category), method),
                new CombatAttackProfile(Spec(EntityKey.Unit_Demon, 1), new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0)),
                new CombatAttackProfile(Spec(EntityKey.Unit_MainCharacter, 1), new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0)),
            };
            return new CombatCommandInput(1, CombatCommandKind.Attack, new EntityId(1),
                aim ?? AttackAim.ForCell(new CellCoord(4, 3)), state, characters, profiles, main,
                characters.Where(c => c.OwnedId.HasValue).Select(c => new OwnedCharacterRecord(c.OwnedId.Value,
                    c.CharacterKey, c.OwnershipRole.Value)).ToArray(),
                characters.Where(c => c.OwnedId.HasValue).ToDictionary(c => c.OwnedId.Value, c => 0),
                SoulFieldSnapshot.Empty, new ArtifactLoadoutSnapshot(0, Array.Empty<ArtifactPlacedEffectSnapshot>()),
                battleInstanceId: 1, expectedBattleInstanceId: 1, expectedControllerTurnSequence: 1);
        }

        internal static CharacterCombatFacts Character(int id, EntityKey key, CellCoord cell,
            Owner owner, int attack = 1, bool main = false, UnitActionUse actionUse = UnitActionUse.Available)
        {
            var stats = new UnitStatsSnapshot(MoveMethod.LiftAndPlace,3,4,                attack,0,100,Array.Empty<TraitStatusSnapshot>(),CategorySnapshot.CreateStateless(CategoryKey.Main));
            CharacterInstanceId? owned = owner == Owner.Player ? new CharacterInstanceId(main ? 1 : id + 1) : null;
            return new CharacterCombatFacts(new EntityId(id), owned,
                key, owner, owned.HasValue ? (main ? CharacterOwnershipRole.Main : CharacterOwnershipRole.Sub) : null, CharacterRuntimeArea.Field,
                new HealthStateSnapshot(100, 100), stats, null, cell, Rotation4.R0, new[] { cell },
                actionUse);
        }

        internal static UnitSpec Spec(EntityKey key, int attack, CategoryKey category = CategoryKey.Main,
            params TraitSpec[] traits)
        {
            return UnitSpecTestFactory.Create(key, 1, 1, new[] { CellCoord.Zero }, 100, MoveMethod.LiftAndPlace,
                4,
                attack, 100, traits, categoryKey: category);
        }

        internal static CombatCommandInput With(CombatCommandInput input,
            CharacterCombatFacts[] characters = null, CombatCommandState state = null,
            CombatAttackProfile[] profiles = null, SoulFieldSnapshot souls = null,
            ArtifactLoadoutSnapshot? artifacts = null, MainCharacterPersistentState main = null) => new CombatCommandInput(input.Number, input.Kind,
                input.ActorId, input.Aim, state ?? input.State, characters ?? input.Characters.ToArray(),
                profiles ?? input.Profiles.ToArray(), main ?? input.Main, input.Owned, input.SoulCounts,
                souls ?? input.Souls, artifacts ?? input.Artifacts, input.TargetCell, input.Rotation,
                input.Battle, input.ProcessingKind, input.Path, input.MovementContext, input.BattleInstanceId, input.ArtifactItems,
                EnvironmentDamageTests.WithDamageObjectBindings(input.EnvironmentBindings, state ?? input.State), input.ObstacleContents, input.ItemKey, input.ItemInventory, input.ItemDefinitions,
                input.GoldBalance, input.FameBalance, input.Membership, input.ExpectedBattleInstanceId, input.ExpectedControllerTurnSequence, input.ExpectedMoveCommandNumber);

        /// <summary>새 입력 요청은 현재 공개 전투·진영 식별자를 포착한다. 기존 요청의 사본은 With를 쓴다.</summary>
        internal static CombatCommandInput FreshCommand(CombatCommandInput input, CombatCommandKind kind,
            EntityId? actor = null, AttackAim? aim = null) => new CombatCommandInput(1, kind, actor ?? input.ActorId,
                aim ?? input.Aim, input.State, input.Characters, input.Profiles, input.Main, input.Owned,
                input.SoulCounts, input.Souls, input.Artifacts, input.TargetCell, input.Rotation, input.Battle,
                CombatProcessingKind.Command, input.Path, input.MovementContext, input.BattleInstanceId, input.ArtifactItems,
                input.EnvironmentBindings, input.ObstacleContents, input.ItemKey, input.ItemInventory, input.ItemDefinitions,
                input.GoldBalance, input.FameBalance, input.Membership, input.BattleInstanceId, input.State.Turn.ControllerTurnSequence);

        /// <summary>연속 행동 검사는 새 플레이어 시작을 실제 계산하고 새 요청 식별자를 포착한다.</summary>
        internal static CombatCommandInput NextPlayerTurn(CombatCommandInput input)
        {
            var begun = CombatTurnCalculator.BeginControllerTurn(
                EnemyControllerPhaseTests.Input(input, CombatControllerPhase.StartingPlayer), Array.Empty<IMovementTileRule>());
            return FreshCommand(CombatCommandComposition.Project(begun), CombatCommandKind.Attack, input.ActorId, input.Aim);
        }

        /// <summary>이동 뒤 공격 검사는 다음 적 진영 시작으로 행동 자격을 새로 받는다.</summary>
        internal static CombatCommandInput NextEnemyTurn(CombatCommandInput input)
        {
            var begun = CombatTurnCalculator.BeginControllerTurn(
                EnemyControllerPhaseTests.Input(input, CombatControllerPhase.StartingEnemy), Array.Empty<IMovementTileRule>());
            return CombatCommandComposition.Intent(CombatCommandComposition.Project(begun),
                CombatCommandKind.Attack, input.Aim).WithActor(input.ActorId);
        }

        /// <summary>같은 원인의 후속 공격은 일반 명령의 두 번째 행동 대신 실제 반응 계산 경로를 쓴다.</summary>
        internal static CombatCommandTransition Reaction(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules) =>
            CombatAttackCalculator.Reaction(input, rules, input.Characters.Single(c => c.EntityId == input.ActorId), input.Aim);

        internal static CharacterCombatFacts With(CharacterCombatFacts c, int? hp = null,
            TraitStatusSnapshot[] traits = null, CategorySnapshot category = null, CellCoord? position = null,
            UnitActionUse? actionUse = null, int? appointRange = null)
        {
            var s = c.Stats;
            var definition = category != null && category.Key != s.Category.Key
                ? CategoryRegistry.CreateDefault().GetRequired(category.Key) : null;
            var stats = new UnitStatsSnapshot(s.MoveMethod,definition?.MovementAllowance ?? s.MoveRange,appointRange ?? s.AppointRange,                s.AttackPower,s.PhysicalDefense,                s.Priority,traits ?? s.TraitStatuses.ToArray(),category ?? s.Category,                s.ArtifactAttackBonus,s.EffectiveAttackPower,s.SoulAttackBonus,s.SoulMaxHealthBonus,s.PositionAttackBonus);
            var cell = position ?? c.Anchor.Value;
            return new CharacterCombatFacts(c.EntityId, c.OwnedId, c.CharacterKey, c.Owner, c.OwnershipRole,
                c.Area, new HealthStateSnapshot(hp ?? c.Health.CurrentHealth, c.Health.MaxHealth), stats,
                c.ReserveSlotIndex, cell, c.Rotation, new[] { cell }, actionUse ?? c.ActionUse, c.TemporaryAttackBonus, c.TemporaryMaxHealthBonus,
                c.PositionModifier, c.HeldEnvironment, c.Debuffs, c.Items, c.Experience, c.StageRecord);
        }
    }
}
