using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// 적 전용 자폭 특성과 넉백 시체 밀기(Phase C)입니다.
    /// 기본 배치: 플레이어 공격자 1 (3,3) 공격력 20, 자폭 적 2 (4,3) 체력 5·공격력 20, 이웃 적 3 (5,3), 먼 적 4 (6,6). 플레이어 턴입니다.
    /// </summary>
    public sealed class SelfDestructTraitTests
    {
        private static readonly IMovementTileRule[] NoRules = Array.Empty<IMovementTileRule>();
        private static readonly TargetingMethodSpec SingleNoSplash =
            new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0);
        private static readonly CellCoord BlastCell = new CellCoord(4, 3);

        [TestCase(1, 10)]
        [TestCase(2, 15)]
        [TestCase(3, 22)]
        public void AttackKill_DamagesSurroundingCharactersByTheLevelPercentOfPreDeathAttack(int level, int damage)
        {
            var input = Input(level);
            var result = CombatCommandCalculator.Calculate(input, NoRules);

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Final(result, input, 2).Health.IsDead, Is.True);
            Assert.That(Final(result, input, 1).Health.CurrentHealth, Is.EqualTo(100 - damage));
            Assert.That(Final(result, input, 3).Health.CurrentHealth, Is.EqualTo(100 - damage));
            Assert.That(Final(result, input, 4).Health.CurrentHealth, Is.EqualTo(100));

            var blast = Activations(result, TraitActivationKind.SelfDestructExploded).Single();
            Assert.That(blast.SourceUnitId, Is.EqualTo(new EntityId(2)));
            Assert.That(blast.Magnitude, Is.EqualTo(damage));
            Assert.That(blast.TargetIds, Is.EqualTo(new[] { new EntityId(1), new EntityId(3) }));
            Assert.That(blast.TargetCell, Is.EqualTo(BlastCell));
            Assert.That(blast.TargetCells.Count, Is.EqualTo(8));
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ReadAll(result.Events));
        }

        [Test]
        public void ExplosionKill_IsCreditedToTheExploder()
        {
            var input = Input(1, neighbor: Enemy(3, new CellCoord(5, 3), 10));
            var result = CombatCommandCalculator.Calculate(input, NoRules);

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Final(result, input, 3).Health.IsDead, Is.True);
            var died = result.Events.OfType<CombatCharacterEvent>().Single(e => e.Kind == CombatEventKind.Died && e.Final.EntityId == new EntityId(3));
            Assert.That(died.DamageOrigins.Select(o => o.CreditedCharacterId), Is.EqualTo(new EntityId?[] { new EntityId(2) }));
        }

        [Test]
        public void ChainedExplosion_RunsInTheNextRound()
        {
            var second = SelfDestructUnit(3, new CellCoord(5, 3), 10, 1);
            var input = Input(1, neighbor: second, far: Enemy(4, new CellCoord(6, 4), 100));
            var result = CombatCommandCalculator.Calculate(input, NoRules);

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Activations(result, TraitActivationKind.SelfDestructExploded).Select(a => a.SourceUnitId),
                Is.EqualTo(new[] { new EntityId(2), new EntityId(3) }));
            Assert.That(Final(result, input, 1).Health.CurrentHealth, Is.EqualTo(90));
            Assert.That(Final(result, input, 4).Health.CurrentHealth, Is.EqualTo(90));
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ReadAll(result.Events));
        }

        [Test]
        public void PublicPreview_DoesNotExplode()
        {
            var input = Input(2);
            CombatCommandCalculator.Preview(input, NoRules, out var projection);
            Assert.That(projection.Accepted, Is.True, projection.Reason);
            Assert.That(Activations(projection, TraitActivationKind.SelfDestructExploded), Is.Empty);
            Assert.That(Final(projection, input, 1).Health.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void StageEnd_StopsTheRemainingDeathTraits()
        {
            // 첫 폭발이 본부를 부수고 공포 보유자를 죽인다. 승패가 정해졌으므로 공포는 실행하지 않는다(설계 P6).
            var fear = CombatCommandTestData.With(CombatCommandTestData.Character(3, EntityKey.Unit_Sniper, new CellCoord(4, 4), Owner.Enemy),
                hp: 5, traits: new TraitStatusSnapshot[] { new FearTraitStatusSnapshot() });
            var basis = Input(1, neighbor: fear, extraProfile: new CombatAttackProfile(
                CombatCommandTestData.Spec(EntityKey.Unit_Sniper, 1, CategoryKey.Main, new FearTraitSpec()), SingleNoSplash));
            var input = CombatCommandTestData.With(basis, state: new CombatCommandState(8, 8, basis.State.Phase,
                new[] { Headquarters(new CellCoord(5, 4), 5) }, basis.State.ThornFields, turn: basis.State.Turn));
            var result = CombatCommandCalculator.Calculate(input, NoRules);

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Outcome.Outcome, Is.EqualTo(StageOutcome.Victory));
            Assert.That(Final(result, input, 3).Health.IsDead, Is.True);
            Assert.That(result.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits).Where(t => t.TraitKey == TraitKey.Fear), Is.Empty);
        }

        [Test]
        public void FallDeath_DoesNotExplode_ButADamageDeathOnTheSameProjectionDoes()
        {
            var fell = Input(1);
            var projection = MovementProjection.ForArrival(fell, fell.Characters.ToDictionary(c => c.EntityId), new List<CombatEvent>());
            projection.ApplyFall(new EntityId(2), 1, BlastCell);
            var afterFall = projection.Complete();
            Assert.That(Activations(afterFall, TraitActivationKind.SelfDestructExploded), Is.Empty);
            Assert.That(Final(afterFall, fell, 1).Health.CurrentHealth, Is.EqualTo(100));

            var hurt = Input(1);
            var damaged = MovementProjection.ForArrival(hurt, hurt.Characters.ToDictionary(c => c.EntityId), new List<CombatEvent>());
            damaged.ChangeHealth(new MovementHealthRequest(new EntityId(2), 50), false);
            var afterDamage = damaged.Complete();
            Assert.That(Activations(afterDamage, TraitActivationKind.SelfDestructExploded).Single().SourceUnitId, Is.EqualTo(new EntityId(2)));
            Assert.That(Final(afterDamage, hurt, 1).Health.CurrentHealth, Is.EqualTo(90));
        }

        [Test]
        public void KnockbackKill_MovesTheBlastToTheEndCellOfThePath()
        {
            var input = Input(1, neighbor: Enemy(3, new CellCoord(1, 6), 100), far: Enemy(4, new CellCoord(7, 3), 100), knockback: 2);
            var result = CombatCommandCalculator.Calculate(input, NoRules);

            Assert.That(result.Accepted, Is.True, result.Reason);
            var corpse = Activations(result, TraitActivationKind.CorpseDisplaced).Single();
            Assert.That(corpse.SourceUnitId, Is.EqualTo(new EntityId(2)));
            Assert.That(corpse.TargetCells, Is.EqualTo(new[] { BlastCell, new CellCoord(5, 3), new CellCoord(6, 3) }));
            var blast = Activations(result, TraitActivationKind.SelfDestructExploded).Single();
            Assert.That(blast.TargetCell, Is.EqualTo(new CellCoord(6, 3)));
            Assert.That(Final(result, input, 4).Health.CurrentHealth, Is.EqualTo(90));
            Assert.That(Final(result, input, 1).Health.CurrentHealth, Is.EqualTo(100));
            var events = result.Events.ToList();
            Assert.That(events.FindIndex(e => IsActivation(e, TraitActivationKind.CorpseDisplaced)),
                Is.LessThan(events.FindIndex(e => IsActivation(e, TraitActivationKind.SelfDestructExploded))));
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ReadAll(result.Events));
        }

        [Test]
        public void KnockbackKill_BlockedPath_KeepsTheBlastOnTheDeathCell()
        {
            var input = Input(1, knockback: 2);
            var result = CombatCommandCalculator.Calculate(input, NoRules);

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Activations(result, TraitActivationKind.CorpseDisplaced), Is.Empty);
            Assert.That(Activations(result, TraitActivationKind.SelfDestructExploded).Single().TargetCell, Is.EqualTo(BlastCell));
        }

        [Test]
        public void KnockbackKill_OfAPlainUnit_DisplacesNoCorpse()
        {
            var input = Input(1, neighbor: Enemy(3, new CellCoord(1, 6), 100), knockback: 2, holder: false);
            var result = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Final(result, input, 2).Health.IsDead, Is.True);
            Assert.That(Activations(result, TraitActivationKind.CorpseDisplaced), Is.Empty);
        }

        [Test]
        public void KillChain_FinishesBeforeTheExplosion_AndDoesNotCountExplosionKills()
        {
            // 처치 연쇄 대상은 무작위다(3 또는 4). 어느 쪽이든 연쇄는 폭발보다 먼저 끝나고, 원 공격 결과는 폭발과 섞이지 않는다(검토 초점 R1).
            var basis = Input(1, neighbor: Enemy(3, new CellCoord(5, 3), 5), far: Enemy(4, new CellCoord(6, 6), 100));
            var actor = CombatCommandTestData.With(basis.Characters[0], traits: new TraitStatusSnapshot[] { new KillChainAttackTraitStatusSnapshot() });
            var profiles = basis.Profiles.ToArray();
            profiles[0] = new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20, CategoryKey.Main,
                new KillChainAttackTraitSpec()), SingleNoSplash);
            var input = CombatCommandTestData.With(basis, basis.Characters.Select(c => c.EntityId == actor.EntityId ? actor : c).ToArray(),
                profiles: profiles);
            var result = CombatCommandCalculator.Calculate(input, NoRules);

            Assert.That(result.Accepted, Is.True, result.Reason);
            var attack = result.Attack.Value;
            Assert.That(attack.Hits.ToArray().Select(h => (h.TargetId, h.TargetHealthAfter)), Is.EqualTo(new[] { (new EntityId(2), 0) }));
            var events = result.Events.ToList();
            int blast = events.FindIndex(e => IsActivation(e, TraitActivationKind.SelfDestructExploded));
            var chain = events.Select((e, i) => (e, i)).Where(p => p.e is CombatEffectsEvent f && f.Traits.Any(t => t.TraitKey == TraitKey.KillChainAttack))
                .Select(p => p.i).ToArray();
            Assert.That(blast, Is.GreaterThanOrEqualTo(0));
            Assert.That(chain, Is.Not.Empty);
            Assert.That(chain.All(i => i < blast), Is.True);
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ReadAll(result.Events));
        }

        private static CombatCommandInput Input(int level, CharacterCombatFacts neighbor = null, CharacterCombatFacts far = null,
            int knockback = 0, bool holder = true, CombatAttackProfile extraProfile = null)
        {
            var input = CombatCommandTestData.Input(SingleNoSplash, AttackAim.ForCell(BlastCell), empty: true);
            var actor = CombatCommandTestData.With(input.Characters[0], position: new CellCoord(3, 3),
                traits: knockback > 0 ? new TraitStatusSnapshot[] { new KnockbackTraitStatusSnapshot(knockback) } : null);
            var bomber = holder ? SelfDestructUnit(2, BlastCell, 5, level)
                : CombatCommandTestData.With(CombatCommandTestData.Character(2, EntityKey.Unit_Gravekeeper, BlastCell, Owner.Enemy, 20), hp: 5);
            var characters = new[]
            {
                actor, input.Characters[1], bomber,
                neighbor ?? Enemy(3, new CellCoord(5, 3), 100),
                far ?? Enemy(4, new CellCoord(6, 6), 100),
            };
            var profiles = input.Profiles.ToArray();
            if (knockback > 0)
                profiles[0] = new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20, CategoryKey.Main,
                    new KnockbackTraitSpec(knockback)), SingleNoSplash);
            var all = profiles.Append(new CombatAttackProfile(holder
                ? CombatCommandTestData.Spec(EntityKey.Unit_Gravekeeper, 20, CategoryKey.Main, new SelfDestructTraitSpec(level))
                : CombatCommandTestData.Spec(EntityKey.Unit_Gravekeeper, 20), SingleNoSplash));
            if (extraProfile != null) all = all.Append(extraProfile);
            return CombatCommandTestData.With(input, characters, profiles: all.ToArray());
        }

        private static CharacterCombatFacts SelfDestructUnit(int id, CellCoord cell, int hp, int level) =>
            CombatCommandTestData.With(CombatCommandTestData.Character(id, EntityKey.Unit_Gravekeeper, cell, Owner.Enemy, 20),
                hp: hp, traits: new TraitStatusSnapshot[] { new SelfDestructTraitStatusSnapshot(level) });

        private static CharacterCombatFacts Enemy(int id, CellCoord cell, int hp) =>
            CombatCommandTestData.With(CombatCommandTestData.Character(id, EntityKey.Unit_Demon, cell, Owner.Enemy), hp: hp);

        private static StructureCombatFacts Headquarters(CellCoord cell, int hp) => new StructureCombatFacts(new EntityId(20),
            new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, hp, false, 0),
            new HealthStateSnapshot(hp, hp), cell, Rotation4.R0, new[] { cell }, Owner.Enemy);

        private static CharacterCombatFacts Final(CombatCommandTransition result, CombatCommandInput input, int id)
        {
            var entityId = new EntityId(id);
            return result.Characters.Characters.SingleOrDefault(c => c.Final.EntityId == entityId)?.Final ??
                input.Characters.Single(c => c.EntityId == entityId);
        }

        private static bool IsActivation(CombatEvent e, TraitActivationKind kind) =>
            e is CombatEffectsEvent effects && effects.Traits.Any(t => t.TraitActivationKind == kind);

        private static TraitActivationSnapshot[] Activations(CombatCommandTransition result, TraitActivationKind kind) =>
            result.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits).Where(t => t.TraitActivationKind == kind).ToArray();
    }
}
