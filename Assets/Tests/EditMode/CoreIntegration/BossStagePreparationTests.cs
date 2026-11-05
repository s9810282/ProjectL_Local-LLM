using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.Factory.Runtime.InGame.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.ServiceKeys.Runtime;
using ProjectL.Systems.ServiceKeys.Runtime.InGame;

namespace ProjectL.Tests.CoreIntegration
{
    // 테스트가 구성한 보스 입력을 실제 전투 정의 공급원과 전투 준비가 처리하는지 확인합니다.
    public sealed class BossStagePreparationTests
    {
        [TestCase(2101)]
        [TestCase(3407)]
        public void ConfiguredBossStage_PreparesAndPlacesBoss(int stageKey)
        {
            var database = new CoreIntegrationTestData(ConfiguredMovementBattle.Config);
            database.SetUnit(new UnitData(EntityKey.Unit_Stage1Boss, UnitRole.Regular, UnitOwnerEligibility.EnemyOnly,
                1, 1, 123, 7, 0, CategoryKey.Boss, 0, 1, new UnitTraitData[] { new RoarTraitData(3) },
                targetingMethodKey: 1, behavior: new UnitBehaviorSpec(false, false, false), killReward: UnitKillRewardSpec.Excluded));
            var bossAnchor = new CellCoord(3, 4);
            database.SetStage(new StageRootData(stageKey, 1, StageMapRole.Boss,
                new CombatStageDetail(42, "field", 1, 0, 1,
                    new[] { new EntityPlacement(EntityKey.Unit_Stage1Boss, bossAnchor, Rotation4.R180, isVictoryTarget: true) },
                    Array.Empty<EntityKey>(), Array.Empty<EntityPlacement>(), EntityKey.None,
                    CellCoord.Zero, Rotation4.R0, CellCoord.Zero, Rotation4.R0, null,
                    objectiveKind: BattleObjectiveKind.DesignatedBossDefeated)));
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            InGameCoreSystemFactory.Create().ExportServices(builder);
            var runtime = new FrameworkRuntime(builder.BuildSystemReader());
            try
            {
                runtime.InitData(database);
                runtime.PostBindService();
                var core = runtime.GetService(InGameSystemGroupServiceKey.Core);
                var buffers = runtime.GetService(BufferSystemKey.Buffer);
                Assert.That(core.Characters.InitializeRoster(1).Succeeded, Is.True);
                CoreIntegrationTestData.InstallDeck(buffers);

                // 전투 진입 서비스와 같은 Buffer 사실로 준비 입력을 만든다.
                var characters = buffers.CharacterReadSink;
                Assert.That(buffers.RunDeckReadSink.TryGetCurrent(out var deck), Is.True);
                var owned = deck.OrderedIds.Select(id => characters.TryGetOwned(id, out var record) ? record : null).ToArray();
                Assert.That(owned, Has.None.Null);
                Assert.That(characters.TryGetMainState(deck.MainId, out var main), Is.True);
                var souls = owned.ToDictionary(o => o.Id, o => characters.GetSoulCount(o.Id));
                var draw = new InitialDrawPlan(owned.Where(o => o.Role == CharacterOwnershipRole.Sub).Select(o => o.Id).ToArray());

                var initialization = CoreIntegrationTestData.DatabaseSource(buffers).Read(database, 1, new StageKey(stageKey));
                BattlePreparation prepared = null;
                Assert.DoesNotThrow(() => prepared = BattlePreparationCalculator.Prepare(initialization, owned, main, souls, draw,
                    System.Array.Empty<CharacterCombatFacts>(), deck));

                var boss = prepared.Characters.Single(c => c.CharacterKey == EntityKey.Unit_Stage1Boss);
                Assert.That(boss.Owner, Is.EqualTo(Owner.Enemy));
                Assert.That(boss.Anchor.HasValue, Is.True);
                Assert.That(boss.Anchor.Value, Is.EqualTo(bossAnchor));
                Assert.That(prepared.CommandState.Objective.SameValues(BattleObjective.DesignatedBoss(boss.EntityId)), Is.True);
                var authored = initialization.Units.Single(u => u.Key == EntityKey.Unit_Stage1Boss);
                Assert.That((authored.Behavior.CanBasicAttack, authored.Behavior.CanMoveVoluntarily), Is.EqualTo((false, false)));
                Assert.That(authored.Traits.ToArray().Select(t => t.TraitKey), Has.Member(TraitKey.Roar));
                Assert.Throws<KeyNotFoundException>(() => database.GetStage(1101));
            }
            finally
            {
                runtime.Shutdown();
            }
        }
    }
}
