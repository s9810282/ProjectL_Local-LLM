using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;

namespace ProjectL.Tests.Core
{
    /// <summary>레벨로 성장한 전투 값이 기존 전투 경로(영혼 수집·피해 평가·소환)에 그대로 쓰이는지 확인한다.</summary>
    public sealed class UnitLevelGrowthCombatTests
    {
        [Test]
        public void LeveledSub_CollectsSoulFromBattleBaseMaxHealth()
        {
            var (input, actor, owned) = LeveledSubInput(level: 3);
            Assert.That((actor.Stats.AttackPower, actor.Health.MaxHealth), Is.EqualTo((22, 102)), "Rider 레벨 3: 정의 20·100");

            var projected = input.Characters.ToDictionary(c => c.EntityId);
            var counts = input.SoulCounts.ToDictionary(p => p.Key, p => p.Value);
            var field = new SoulFieldSnapshot(2, new[] { new SoulFieldFacts(1,
                new EffectOriginKey(EffectOwnerKind.Artifact, 4, 11, "level-soul"), actor.Anchor.Value, 3, 5) });
            var consumed = new List<long>();
            CombatCommandCalculator.Collect(input, new SoulEffectCalculator(input.Artifacts), projected, counts, actor,
                SoulArrivalKind.SettledVoluntary, ref field, consumed);

            var final = projected[actor.EntityId];
            Assert.That(consumed, Is.EqualTo(new[] { 1L }));
            Assert.That(counts[owned.Id], Is.EqualTo(1));
            Assert.That((final.Health.CurrentHealth, final.Health.MaxHealth), Is.EqualTo((103, 103)));
            Assert.That((final.Stats.AttackPower, final.Stats.SoulAttackBonus, final.Stats.EffectiveAttackPower),
                Is.EqualTo((22, 1, 23)));
        }

        [Test]
        public void LeveledAttacker_DamageEvaluationStartsFromGrownAttack()
        {
            var (input, first, _) = LeveledSubInput(level: 1);
            var (_, third, _) = LeveledSubInput(level: 3);
            var souls = new SoulEffectCalculator(input.Artifacts);

            Assert.That(souls.EvaluateAttackPower(first, 0, new[] { first }), Is.EqualTo(20));
            Assert.That(souls.EvaluateAttackPower(third, 0, new[] { third }), Is.EqualTo(22));
        }
        /// <summary>Rider 상승치를 가진 정의로 만든 레벨 서브를 행위자로 둔 명령 입력이다.</summary>
        private static (CombatCommandInput Input, CharacterCombatFacts Actor, OwnedCharacterRecord Owned) LeveledSubInput(int level)
        {
            var source = CombatCommandTestData.Input();
            var original = source.Characters[0];
            var owned = new OwnedCharacterRecord(original.OwnedId.Value, original.CharacterKey, CharacterOwnershipRole.Sub, level);
            var spec = CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20, CategoryKey.Rider);
            UnitDefinition definition = new UnitCatalog(new[] { spec }, TraitRegistry.CreateDefault(),
                new CategoryRegistry(UnitSpecTestFactory.CreateGrowthCategoryCatalog())).GetRequired(spec.Key);
            var cell = original.Anchor.Value;
            var actor = BattleCharacterCalculator.CreateOwned(original.EntityId, definition, owned, null, 0, null,
                cell, Rotation4.R0, new[] { cell }, TraitRegistry.CreateDefault());
            var input = new CombatCommandInput(source.Number, source.Kind, source.ActorId, source.Aim, source.State,
                source.Characters.Skip(1).Prepend(actor).ToArray(), source.Profiles.ToArray(), source.Main,
                source.Owned.Select(o => o.Id == owned.Id ? owned : o).ToArray(), source.SoulCounts, source.Souls,
                source.Artifacts,
                expectedBattleInstanceId: source.ExpectedBattleInstanceId, expectedControllerTurnSequence: source.ExpectedControllerTurnSequence,
                battleInstanceId: source.BattleInstanceId);
            return (input, actor, owned);
        }
    }
}
