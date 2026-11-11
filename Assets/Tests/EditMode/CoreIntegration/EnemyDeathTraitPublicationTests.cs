using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.CoreIntegration
{
    /// <summary>
    /// 적 전용 사망 특성(공포·자폭·재구성)이 실제 명령 처리와 Buffer 게시를 지나는지 확인합니다(Phase F.2).
    /// 배치는 <see cref="ConfiguredMovementBattle"/> 그대로이고, 적 Demon을 (0,3)·(1,3)에 두고 카우보이(0,1)가 (0,3)을 칩니다.
    /// </summary>
    public sealed class EnemyDeathTraitPublicationTests
    {
        private static readonly CellCoord Target = new CellCoord(0, 3);
        private static readonly CellCoord Neighbor = new CellCoord(1, 3);

        [Test]
        public void Fear_PublishesRestrictionsOnTheSurvivingNeighbor()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config,
                new DeathTraitSource(new FearTraitSpec(), 10, Target, Neighbor));
            var target = DemonAt(battle, Target).EntityId;
            var neighbor = DemonAt(battle, Neighbor).EntityId;
            battle.Submit(battle.Attack(battle.Actor, Target));

            Assert.That(IsGone(battle, target), Is.True);
            var fear = Facts(battle, neighbor).StatusEffects.Entries.OfType<ActionRestrictionStatus>().Where(s => s.Origin.HasValue &&
                s.Origin.Value.Kind == EffectOwnerKind.Trait && s.Origin.Value.DefinitionKey == (int)TraitKey.Fear &&
                s.Origin.Value.OwnerInstanceId == target.Value).ToArray();
            Assert.That(fear.Select(s => s.Key), Is.EquivalentTo(new[] { StatusEffectKey.Disarm, StatusEffectKey.Bind }));
        }

        [Test]
        public void SelfDestruct_PublishesTheExplosionDamageOnTheNeighbor()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config,
                new DeathTraitSource(new SelfDestructTraitSpec(1), 30, Target, Neighbor));
            var target = DemonAt(battle, Target).EntityId;
            var neighbor = DemonAt(battle, Neighbor).EntityId;
            int actorHealth = battle.Actor.Health.CurrentHealth;
            battle.Submit(battle.Attack(battle.Actor, Target));

            Assert.That(IsGone(battle, target), Is.True);
            // 사망 직전 공격력 20의 50%. 카우보이(0,1)는 (0,3) 주변 8칸 밖이다.
            Assert.That(Facts(battle, neighbor).Health.CurrentHealth, Is.EqualTo(20));
            Assert.That(battle.Actor.Health.CurrentHealth, Is.EqualTo(actorHealth));
        }

        [Test]
        public void Reconstruction_PublishesStatusAndUsed_ThenRevivesOnTheSecondEnemyTurn()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config,
                new DeathTraitSource(new ReconstructionTraitSpec(1), 30, Target));
            var id = DemonAt(battle, Target).EntityId;
            battle.Submit(battle.Attack(battle.Actor, Target));

            var reconstructed = Facts(battle, id);
            Assert.That(reconstructed.Health.CurrentHealth, Is.EqualTo(30));
            Assert.That(Reconstruction(reconstructed).RemainingOwnTurnStarts, Is.EqualTo(2));
            Assert.That(TraitStatus(reconstructed).Used, Is.True);

            // 첫 적 턴 시작에 1로 줄고, 재구성 중이라 AI는 이 유닛을 움직이지 않는다.
            battle.Submit(battle.EndPlayerTurn());
            battle.AdvanceToPlayerOrDay();
            var waiting = Facts(battle, id);
            Assert.That(Reconstruction(waiting).RemainingOwnTurnStarts, Is.EqualTo(1));
            Assert.That(waiting.Anchor, Is.EqualTo(Target));

            // 다음 적 턴 시작에 부활한다. 사용함 표시는 남는다.
            battle.Submit(battle.EndPlayerTurn());
            battle.AdvanceToPlayerOrDay();
            var revived = Facts(battle, id);
            Assert.That(revived.Health.IsDead, Is.False);
            Assert.That(Reconstruction(revived), Is.Null);
            Assert.That(TraitStatus(revived).Used, Is.True);
        }

        private static CharacterCombatFacts DemonAt(ConfiguredMovementBattle battle, CellCoord cell) =>
            battle.Buffers.CharacterReadSink.Characters.Select(c => c.ToFacts())
                .Single(c => c.CharacterKey == EntityKey.Unit_Demon && c.Anchor == cell);

        // 죽은 적은 게시에서 빠지거나 죽은 사실로 남는다. 어느 쪽이든 사망이다.
        private static bool IsGone(ConfiguredMovementBattle battle, EntityId id) =>
            battle.Buffers.CharacterReadSink.Characters.SingleOrDefault(c => c.EntityId == id)?.ToFacts().Health.IsDead ?? true;

        private static CharacterCombatFacts Facts(ConfiguredMovementBattle battle, EntityId id) =>
            battle.Buffers.CharacterReadSink.Characters.Single(c => c.EntityId == id).ToFacts();

        private static ReconstructionStatus Reconstruction(CharacterCombatFacts character) =>
            character.StatusEffects.Entries.OfType<ReconstructionStatus>().SingleOrDefault();

        private static ReconstructionTraitStatusSnapshot TraitStatus(CharacterCombatFacts character) =>
            character.Stats.TraitStatuses.ToArray().OfType<ReconstructionTraitStatusSnapshot>().Single();

        /// <summary>적 Demon에 사망 특성 하나를 주고(최대 체력·공격력 20·방어 0), 카우보이 공격력을 50으로 올린 전투 정의입니다.</summary>
        private sealed class DeathTraitSource : IBattleInitializationSource
        {
            private readonly TraitSpec _trait;
            private readonly int _demonHealth;
            private readonly CellCoord[] _demons;

            internal DeathTraitSource(TraitSpec trait, int demonHealth, params CellCoord[] demons)
            {
                _trait = trait;
                _demonHealth = demonHealth;
                _demons = demons;
            }

            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var basis = new ConfiguredMovementBattle.EncounterSource().Read(database, chapterKey, stageKey);
                var units = basis.Units.Select(s => new UnitSpec(s.Key, s.Role, s.FootprintWidth, s.FootprintHeight, s.FootprintLocalCells.ToArray(),
                    s.Key == EntityKey.Unit_Demon ? _demonHealth : s.MaxHealth, s.MoveMethod, s.AttackRange, s.CategoryKey,
                    s.Key == EntityKey.Unit_Demon ? 20 : s.Key == EntityKey.Unit_Cowboy ? 50 : s.AttackPower,
                    s.Key == EntityKey.Unit_Demon ? 0 : s.PhysicalDefense, s.Priority,
                    s.Key == EntityKey.Unit_Demon ? new[] { _trait } : s.Traits.ToArray(),
                    s.Key == EntityKey.Unit_Demon ? UnitOwnerEligibility.EnemyOnly : s.OwnerEligibility,
                    s.Occlusion, s.Behavior, s.KillReward)).ToArray();
                var stage = new StageConfig(8, 6, 8, 2, 3, 42,
                    _demons.Select(cell => new InitialEnemyPlacementSpec(EntityKey.Unit_Demon, cell, Rotation4.R180)).ToArray(),
                    Array.Empty<CellCoord>(), Array.Empty<StageGimmickSpec>(),
                    new[] { new InitialEnemyStructurePlacementSpec(EntityKey.Structure_Headquarters, new CellCoord(3, 5), Rotation4.R180) });
                return new BattleInitialization(stage, basis.MainAnchor, basis.MainRotation, units, basis.Structures, basis.Categories,
                    basis.AttackProfiles.Select(p => new CombatAttackProfile(units.Single(u => u.Key == p.Definition.Key), p.Method, p.OwnedId)).ToArray());
            }
        }
    }
}
