using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;

namespace ProjectL.Tests.Core
{
    public sealed class KillChainSacrificeRecipientRegressionTests
    {
        [TestCase(TraitKey.HuntersMark, true)]
        [TestCase(TraitKey.Intimidation, true)]
        [TestCase(TraitKey.HuntersMark, false)]
        [TestCase(TraitKey.Intimidation, false)]
        public void ExtraAttack_AppliesDebuffOnlyToActualRecipient(TraitKey trait, bool sacrifice)
        {
            var input = Input(trait, sacrifice);
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitCombatFixtures.Final(result, 3).Health.IsDead, Is.True,
                "실제 첫 공격 처치가 추가 공격을 만들어야 합니다.");
            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.TargetId),
                Is.EqualTo(new[] { new EntityId(3) }));

            var activation = result.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits)
                .Single(t => t.TraitKey == TraitKey.KillChainAttack && t.ChildAttack != null);
            var child = activation.ChildAttack;
            var recipientId = new EntityId(sacrifice ? 5 : 2);
            Assert.That(activation.TargetId, Is.EqualTo(new EntityId(2)),
                "고정 난수의 추가 공격 지정 대상은 2번이어야 합니다.");
            Assert.That(child.Hits.Single().TargetId, Is.EqualTo(recipientId));
            if (sacrifice)
            {
                Assert.That(child.Reactions.Single().Interception.ProtectedId, Is.EqualTo(new EntityId(2)));
                Assert.That(TraitCombatFixtures.Final(result, 2).Health.CurrentHealth, Is.EqualTo(5));
                Assert.That(TraitCombatFixtures.Final(result, 5).Health.CurrentHealth, Is.EqualTo(80));
            }
            else
            {
                Assert.That(child.Reactions, Is.Empty);
                Assert.That(TraitCombatFixtures.Final(result, 2).Health.CurrentHealth, Is.EqualTo(80));
                Assert.That(TraitCombatFixtures.Final(result, 5).Health.CurrentHealth, Is.EqualTo(100));
            }

            var origin = TraitEffectOrigin.Create(input.ActorId, trait);
            var recipient = TraitCombatFixtures.Final(result, recipientId.Value);
            var untouched = TraitCombatFixtures.Final(result, sacrifice ? 2 : 5);
            var status = recipient.StatusEffects.Entries.SingleOrDefault(s => s.Origin == origin);
            Assert.That(status, Is.Not.Null, "추가 공격을 실제로 받은 유닛이 디버프를 받아야 합니다.");
            Assert.That(untouched.StatusEffects.Entries.Any(s => s.Origin == origin), Is.False,
                "피해를 받지 않은 유닛에게 디버프를 넘기면 안 됩니다.");
            if (trait == TraitKey.HuntersMark)
                Assert.That(((WeaknessExposureStatus)status).IncreasePercent, Is.EqualTo(30));
            else
                Assert.That(((AttackDecreaseStatus)status).ReductionPercent, Is.EqualTo(25));
        }

        private static CombatCommandInput Input(TraitKey trait, bool sacrifice)
        {
            TraitSpec debuff = trait == TraitKey.HuntersMark
                ? new HuntersMarkTraitSpec(30, 2)
                : new IntimidationTraitSpec(25);
            TraitStatusSnapshot debuffStatus = trait == TraitKey.HuntersMark
                ? new HuntersMarkTraitStatusSnapshot(30, 2)
                : new IntimidationTraitStatusSnapshot(25);
            var input = SacrificeTraitTests.Input();
            // 매개변수가 필요한 특성 상태는 정의와 같은 값으로 직접 구성합니다.
            input = CombatCommandTestData.With(input,
                characters: input.Characters.Select(c => c.EntityId == input.ActorId
                    ? c.With(stats: c.Stats.With(traitStatuses: new TraitStatusSnapshot[]
                        { new KillChainAttackTraitStatusSnapshot(), debuffStatus })) : c).ToArray(),
                profiles: input.Profiles.Select(p => p.Definition.Key == EntityKey.Unit_Akimbo
                    ? new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20, CategoryKey.Main,
                        new KillChainAttackTraitSpec(), debuff), p.Method, p.OwnedId) : p).ToArray());
            if (!sacrifice)
            {
                input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Shielder);
                input = ThornHeartTestData.Replace(input, 2, c => CombatCommandTestData.With(c, hp: 100));
            }

            // 첫 처치 대상은 수호자 (3,4)의 반경 밖이다. 추가 공격만 희생을 소비하도록 분리합니다.
            var firstTarget = new CellCoord(2, 1);
            input = ThornHeartTestData.Replace(input, 3,
                c => CombatCommandTestData.With(c, hp: 5, position: firstTarget));
            var progress = input.State.EnvironmentProgress;
            // 기존 희생 추가 공격 시험과 같은 난수로 남은 후보 2·5·10 중 2번을 지정합니다.
            input = CombatCommandTestData.With(input, state: input.State.With(environmentProgress:
                new EnvironmentProgressFacts(progress.NextObjectEntryOrdinal, progress.NextSourceId, 1)));
            return CombatCommandComposition.Intent(input, CombatCommandKind.Attack, AttackAim.ForCell(firstTarget));
        }
    }
}
