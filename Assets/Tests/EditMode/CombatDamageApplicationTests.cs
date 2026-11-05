using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;

namespace ProjectL.Tests.Core
{
    /// <summary>공유 피해 적용 경계의 계약: 공유 사전 직접 반영, 회복 기록과 고정, 투영과의 준비 순번 일치.</summary>
    public sealed class CombatDamageApplicationTests
    {
        private static readonly ItemDefinition Lantern = new ItemDefinition(4, new LanternItemSpec(5));

        [Test]
        public void Apply_WritesHolderAndEffectTargetWithoutCallerMerge()
        {
            var input = CombatCommandTestData.Input();
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            var holder = new EntityId(2);
            characters[holder] = CombatItemDirectDamageTests.Equip(characters[holder], 100, 4);
            var damage = new CombatDamageApplication(new[] { Lantern }, 0, characters, new List<CombatEvent>());

            var outcome = damage.Apply(holder, CombatDamageKind.OrdinaryAttack,
                new CombatDamageOrigin(CellCoord.Zero, input.ActorId, 1), 0);

            Assert.That(characters[holder], Is.SameAs(outcome.After));
            Assert.That(characters[holder].Items.EquippedItemKey, Is.Null);
            Assert.That(characters[input.ActorId].Items.Effects.Single().ItemKey, Is.EqualTo(4));
            Assert.That(outcome.Recovery, Is.Zero);
        }

        [Test]
        public void CaptureRecovery_RecordsActualRecoveryAndIsASnapshot()
        {
            var input = CombatCommandTestData.Input();
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            foreach (int id in new[] { 2, 3 })
                characters[new EntityId(id)] = CombatItemDirectDamageTests.Equip(characters[new EntityId(id)], 60, 1);
            var damage = new CombatDamageApplication(CombatItemDirectDamageTests.Definitions, 0, characters, new List<CombatEvent>());

            var first = damage.Apply(new EntityId(2), CombatDamageKind.EnvironmentDamage, new CombatDamageOrigin(null, null, 1), 15);
            var captured = damage.CaptureRecovery();
            damage.Apply(new EntityId(3), CombatDamageKind.EnvironmentDamage, new CombatDamageOrigin(null, null, 2), 15);

            Assert.That((first.ActualHealthLoss, first.Recovery, first.After.Health.CurrentHealth), Is.EqualTo((15, 10, 55)));
            Assert.That(captured.Keys, Is.EqualTo(new[] { new EntityId(2) }));
            Assert.That(captured[new EntityId(2)], Is.EqualTo(10));
            Assert.That(damage.CaptureRecovery()[new EntityId(3)], Is.EqualTo(10));
        }

        [Test]
        public void AttackHitResult_ReportsRecoveryRecordedByTheBoundary()
        {
            var input = CombatItemDirectDamageTests.Input(15, 60, 1);
            var result = CombatCommandCalculator.Calculate(input, CombatItemDirectDamageTests.NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var hit = result.Attack.Value.Hits.Span[0];
            Assert.That((hit.TargetHealthBefore, hit.TargetHealthAfter), Is.EqualTo((60, 55)));
            Assert.That(hit.HealthRecovery, Is.EqualTo(10));
            Assert.That(hit.DamageDealt, Is.EqualTo(15));
        }

        [Test]
        public void SharedProjection_RejectsBoundaryWithDifferentPreparationIndex()
        {
            var input = CombatCommandTestData.Input();
            var damage = new CombatDamageApplication(input.ItemDefinitions, input.State.Turn.PreparationIndex + 1,
                input.Characters.ToDictionary(c => c.EntityId), new List<CombatEvent>());
            Assert.Throws<ArgumentException>(() => new MovementProjection(input, input.State, damage,
                input.State.Structures.ToDictionary(s => s.EntityId), input.State.ThornFields.ToDictionary(f => f.Cell),
                input.SoulCounts.ToDictionary(p => p.Key, p => p.Value), input.Souls, new List<long>(),
                new Dictionary<EntityId, CellCoord>(), new SoulEffectCalculator(input.Artifacts)));
        }
    }
}
