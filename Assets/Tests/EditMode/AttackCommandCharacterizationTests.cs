using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// 공격 명령 리팩토링 전의 동작을 고정한다(설계 2026-10-12 §4 I-1~I-9, 계획 R0 G1~G10).
    /// 사건 순서·피격 결과·미리보기·최종 체력을 한 문자열로 요약해 비교한다. 동작을 바꾸지 않는 리팩토링에서는 이 값이 그대로여야 한다.
    /// </summary>
    public sealed class AttackCommandCharacterizationTests
    {
        private static readonly IMovementTileRule[] NoRules = Array.Empty<IMovementTileRule>();

        /// <summary>C1: 아티팩트 공통 피해 사건, 배지 보유 주 대상, 일반 스플래시(G1, G2, G9).</summary>
        [Test]
        public void C1_OrdinarySplash_ArtifactBonusAndBadgeHolder()
        {
            var i = CombatCommandTestData.Input();
            var chars = i.Characters.ToArray();
            chars[2] = HeldEnvironmentEffects.AcquireBadge(chars[2], 41);
            var input = CombatCommandTestData.With(i, chars, artifacts: new ArtifactLoadoutSnapshot(0,
                new[] { new ArtifactPlacedEffectSnapshot(12, 4, ArtifactLastStandRuleTests.LastStandEffect()) }));
            Pin(input, NoRules, C1Expected);
        }

        /// <summary>C2: 스플래시가 보급품·화약통을 직접 맞히고, 폭발이 직접 맞은 적과 다른 화약통에 이어진다(G3, G4, G10).</summary>
        [Test]
        public void C2_OrdinarySplash_DirectObstacleHitsAndChainedBlast()
        {
            var i = CombatCommandTestData.Input();
            var barrel = ObstacleReactionTests.Barrel(51, new CellCoord(5, 3), 1, 20);
            var chained = ObstacleReactionTests.Barrel(53, new CellCoord(6, 3), 3, 20);
            var supply = new StructureCombatFacts(new EntityId(52), new StructureSpec(EntityKey.Structure_Supplies,
                1, 1, new[] { CellCoord.Zero }, 1, false, 0), new ObstacleDurabilitySnapshot(2, 2),
                new CellCoord(4, 2), Rotation4.R0, new[] { new CellCoord(4, 2) }, Owner.Neutral);
            var input = ObstacleReactionTests.Copy(i,
                characters: i.Characters.Where(c => c.EntityId != new EntityId(4)).ToArray(),
                structures: new[] { barrel.Structure, supply, chained.Structure },
                profiles: new[] { barrel.Profile, new ObstacleContentProfile(supply.EntityId, ObstacleContentKind.Supplies, 0, 2, 5),
                    chained.Profile });
            Pin(input, NoRules, C2Expected);
        }

        /// <summary>C3: 밀치기로 배지 오브젝트 칸에 도착한다.</summary>
        [Test]
        public void C3_Knockback_OntoObjectCell()
        {
            var destination = new CellCoord(5, 3);
            var input = CliffFallTests.WithEnvironment(CliffFallTests.Knockback(),
                new SheriffBadgeSourceFact(72, destination, 1, true, new[] { Owner.Player }));
            Pin(input, TornadoEnvironmentTests.Rules, C3Expected);
        }
        /// <summary>C5: 관통. ID 순서(2, 7, 9)와 진행 순서(9, 2, 7)가 다르고 아티팩트 공통 피해 사건이 있다(G6, G1).</summary>
        [Test]
        public void C5_Piercing_EncounterOrderDiffersFromIdOrder()
        {
            var input = PiercingShotTestData.Input(damage: 0);
            input = CombatCommandTestData.With(input, artifacts: new ArtifactLoadoutSnapshot(0,
                new[] { new ArtifactPlacedEffectSnapshot(12, 4, ArtifactLastStandRuleTests.LastStandEffect()) }));
            Pin(input, NoRules, C5Expected);
        }

        /// <summary>C6: 직접 피해 사망과 밀치기 도착 오브젝트 사망의 영혼 사건 시점.</summary>
        [Test]
        public void C6_DirectDeathAndKnockbackArrivalDeath()
        {
            var input = CombatCommandEffectTests.ActorEffect(new KnockbackTraitSpec(1), new KnockbackTraitStatusSnapshot(1));
            var chars = input.Characters.ToArray();
            chars[2] = CombatCommandTestData.With(chars[2], hp: 25);
            chars[4] = CombatCommandTestData.With(chars[4], hp: 1);
            var state = EnvironmentDamageTests.WithDamageObjects(new CombatCommandState(8,8,input.State.Phase,                input.State.Structures,turn: input.State.Turn), (new CellCoord(5, 3), 7));
            input = CombatCommandTestData.With(input, chars, state, artifacts: new ArtifactLoadoutSnapshot(0,
                new[] { new ArtifactPlacedEffectSnapshot(11, 4, ArtifactSoulSchemaTests.SoulEffect()) }));
            var result = CombatCommandCalculator.Calculate(input, EnvironmentDamageTests.DamageRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var events = result.Events.ToList();
            int directDeath = events.FindIndex(e => e is CombatCharacterEvent c && c.Kind == CombatEventKind.Died &&
                c.Final.EntityId == new EntityId(4));
            int movement = events.FindIndex(e => e is CombatCharacterEvent c && c.Kind == CombatEventKind.Moved &&
                c.Final.EntityId == new EntityId(2));
            int arrivalDeath = events.FindIndex(e => e is CombatCharacterEvent c && c.Kind == CombatEventKind.Died &&
                c.Final.EntityId == new EntityId(2));
            Assert.That(directDeath, Is.GreaterThanOrEqualTo(0));
            Assert.That(movement, Is.GreaterThan(directDeath));
            Assert.That(arrivalDeath, Is.GreaterThan(movement));
            Assert.That(result.Attack.Value.Hits.ToArray().Single(h => h.TargetId == new EntityId(2)).DamageDealt, Is.EqualTo(20));
            Assert.That(result.Field.Final.Entries, Is.Empty);
        }

        /// <summary>C7: 연쇄 공격이 직접 맞고 살아남은 스플래시 대상을 다시 때린다. 공격 결과에는 연쇄 피해가 없다(G8, I-4).</summary>
        [Test]
        public void C7_KillChain_HitsSurvivingSplashTarget()
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.Radial, 70);
            var source = CombatCommandTestData.Input(method, AttackAim.ForCell(new CellCoord(4, 3)), empty: true);
            var actor = CombatCommandTestData.With(source.Characters[0],
                traits: new TraitStatusSnapshot[] { new KillChainAttackTraitStatusSnapshot() });
            var chars = new[] { actor, source.Characters[1],
                CombatCommandTestData.With(CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(4, 3), Owner.Enemy), hp: 5),
                CombatCommandTestData.With(CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(4, 4), Owner.Enemy), hp: 100) };
            var profiles = source.Profiles.ToArray();
            profiles[0] = new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20, CategoryKey.Main,
                new KillChainAttackTraitSpec()), method);
            Pin(CombatCommandTestData.With(source, chars, profiles: profiles), NoRules, C7Expected);
        }

        /// <summary>
        /// C8: 밤 공격. 직접 피해로 죽은 대상이 있고, 밀려난 대상이 오브젝트를 발동해 도착 중 사망 정산이 일어난다(G7, I-3).
        /// 도착 투영 생성 시점에 이미 죽은 대상은 도착 중이 아니라 마지막 Finish에서 영혼을 남긴다.
        /// </summary>
        [Test]
        public void C8_NightKnockback_ObjectArrivalSettlesOnlyLaterDeaths()
        {
            // 연쇄 공격이 도착 뒤에 사건을 더 남기므로, 영혼 정산이 도착 중(잘못)인지 Finish(정상)인지 순서로 구분된다.
            var destination = new CellCoord(5, 3);
            var seed = CliffFallTests.Knockback();
            var actor = CombatCommandTestData.With(seed.Characters[0], traits: new TraitStatusSnapshot[] {
                new KnockbackTraitStatusSnapshot(1), new KillChainAttackTraitStatusSnapshot() });
            var profiles = seed.Profiles.Select(p => p.Definition.Key == actor.CharacterKey ? new CombatAttackProfile(
                CombatCommandTestData.Spec(actor.CharacterKey, 20, CategoryKey.Main, new KnockbackTraitSpec(1), new KillChainAttackTraitSpec()),
                p.Method) : p).ToArray();
            var doomed = CombatCommandTestData.With(CombatCommandTestData.Character(5, EntityKey.Unit_Demon, new CellCoord(3, 2), Owner.Enemy), hp: 1);
            var input = CliffFallTests.WithEnvironment(CombatCommandTestData.With(seed,
                    new[] { actor }.Concat(seed.Characters.Skip(1)).Concat(new[] { doomed }).ToArray(), profiles: profiles),
                new SheriffBadgeSourceFact(72, destination, 1, true, new[] { Owner.Player }));
            var s = input.State;
            input = CombatCommandTestData.With(input, state: new CombatCommandState(s.BoardWidth,s.BoardHeight,                s.Phase,s.Structures,s.Turn,s.Environment),
                artifacts: new ArtifactLoadoutSnapshot(0, new[] { new ArtifactPlacedEffectSnapshot(11, 4, ArtifactSoulSchemaTests.SoulEffect()) }));
            Pin(input, TornadoEnvironmentTests.Rules, C8Expected);
        }

        private const string C1Expected =
            "events: ArtifactActivated, Attacked, Held:2, HealthChanged:3, HealthChanged:4\n" +
            "previewEvents: ArtifactActivated, Attacked, Held:2, HealthChanged:3, HealthChanged:4\n" +
            "hits: 2:Primary:100:0:100, 3:Additional:70:17:83, 4:Additional:70:17:83\n" +
            "previewTargets: 2:Primary:0:0:100, 3:Additional:17:0:83, 4:Additional:17:0:83\n" +
            "affected: (3,2), (4,2), (5,2), (3,3), (4,3), (5,3), (3,4), (4,4), (5,4)\n" +
            "final: C1:100, C2:100, C3:83, C4:83, C10:100, souls:0";

        private const string C2Expected =
            "events: Attacked, HealthChanged:2, HealthChanged:3, Durability:51:0, ObstacleDestroyed:51, HealthChanged:2, HealthChanged:3, Durability:52:1, ObstacleHit:52, Held:1, Durability:53:0, ObstacleDestroyed:53, Durability:52:0, ObstacleDestroyed:52, Held:1\n" +
            "previewEvents: Attacked, HealthChanged:2, HealthChanged:3, Durability:51:0, ObstacleDestroyed:51, HealthChanged:2, HealthChanged:3, Durability:52:1, ObstacleHit:52, Held:1, Durability:53:0, ObstacleDestroyed:53, Durability:52:0, ObstacleDestroyed:52, Held:1\n" +
            "hits: 2:Primary:100:40:60, 3:Additional:70:34:66, 51:Additional:70:0:0, 52:Additional:70:0:0\n" +
            "previewTargets: 2:Primary:40:20:60, 3:Additional:34:20:66, 51:Additional:0:0:0, 52:Additional:0:0:0\n" +
            "affected: (3,2), (4,2), (5,2), (3,3), (4,3), (5,3), (3,4), (4,4), (5,4)\n" +
            "final: C1:100, C2:60, C3:66, C10:100, S51:0, S52:0, S53:0, souls:0";

        private const string C3Expected =
            "events: Attacked, HealthChanged:2, HealthChanged:3, Effects:Knockback/c0, Effects:Knockback/c0, Moved:2, Moved:3, Object:72:2, Held:2, Source:72\n" +
            "previewEvents: Attacked, HealthChanged:2, HealthChanged:3, Effects:Knockback/c0, Effects:Knockback/c0, Moved:2, Moved:3, Object:72:2, Held:2, Source:72\n" +
            "hits: 2:Primary:100:20:80, 3:Additional:70:14:86\n" +
            "previewTargets: 2:Primary:20:0:80, 3:Additional:14:0:86\n" +
            "affected: (3,2), (4,2), (5,2), (3,3), (4,3), (5,3), (3,4), (4,4), (5,4)\n" +
            "final: C1:100, C2:80, C3:86, C10:100, souls:0";

        private const string C4Expected =
            "events: Attacked, HealthChanged:2, HealthChanged:4\n" +
            "previewEvents: Attacked, HealthChanged:2, HealthChanged:4\n" +
            "hits: 2:Primary:100:20:80, 4:Additional:0:5:95\n" +
            "previewTargets: 2:Primary:20:0:80, 4:Additional:5:5:95\n" +
            "affected: (4,3)\n" +
            "final: C1:100, C2:80, C3:100, C4:95, C10:100, souls:0";

        private const string C5Expected =
            "events: ArtifactActivated, Attacked, Effects:PiercingFalloff/c0, Effects:PiercingFalloff/c0, HealthChanged:9, HealthChanged:2, HealthChanged:7\n" +
            "previewEvents: ArtifactActivated, Attacked, Effects:PiercingFalloff/c0, Effects:PiercingFalloff/c0, HealthChanged:9, HealthChanged:2, HealthChanged:7\n" +
            "hits: 2:Primary:100:4:96, 7:Effect:100:2:98, 9:Effect:100:5:95\n" +
            "previewTargets: 2:Primary:4:0:96, 7:Effect:2:0:98, 9:Effect:5:0:95\n" +
            "affected: (2,3), (3,3), (4,3), (5,3), (6,3), (7,3)\n" +
            "final: C1:100, C2:96, C7:98, C9:95, C10:100, souls:0";

        private const string C7Expected =
            "events: Attacked, Died:2, HealthChanged:3, Effects:KillChainAttack/c0, HealthChanged:3\n" +
            "previewEvents: Attacked, Died:2, HealthChanged:3\n" +
            "hits: 2:Primary:100:5:0, 3:Additional:70:14:86\n" +
            "previewTargets: 2:Primary:20:0:0, 3:Additional:14:0:86\n" +
            "affected: (3,2), (4,2), (5,2), (3,3), (4,3), (5,3), (3,4), (4,4), (5,4)\n" +
            "final: C1:100, C2:0, C3:66, C10:100, souls:0";

        private const string C8Expected =
            "events: Attacked, HealthChanged:2, HealthChanged:3, Died:5, Effects:Knockback/c0, Effects:Knockback/c0, Moved:2, Moved:3, Object:72:2, Held:2, Source:72, Effects:KillChainAttack/c0, Held:2\n" +
            "previewEvents: Attacked, HealthChanged:2, HealthChanged:3, Died:5, Effects:Knockback/c0, Effects:Knockback/c0, Moved:2, Moved:3, Object:72:2, Held:2, Source:72\n" +
            "hits: 2:Primary:100:20:80, 3:Additional:70:14:86, 5:Additional:70:1:0\n" +
            "previewTargets: 2:Primary:20:0:80, 3:Additional:14:0:86, 5:Additional:14:0:0\n" +
            "affected: (3,2), (4,2), (5,2), (3,3), (4,3), (5,3), (3,4), (4,4), (5,4)\n" +
            "final: C1:100, C2:80, C3:86, C5:0, C10:100, souls:0";

        private static void Pin(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules, string expected)
        {
            string actual = Describe(input, rules);
            // 기대값이 없으면 현재 동작을 출력한다. 검토를 거친 뒤에만 기대값으로 고정한다.
            if (expected == null) Assert.Fail("ACTUAL<<\n" + actual + "\n>>ACTUAL");
            Assert.That(actual, Is.EqualTo(expected));
        }

        private static string Describe(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)
        {
            var execution = CombatCommandCalculator.Calculate(input, rules);
            Assert.That(execution.Accepted, Is.True, execution.Reason);
            var preview = CombatCommandCalculator.Preview(input, rules, out var projection);
            Assert.That(preview.FailureReason, Is.EqualTo(AttackFailureReason.None));
            var attack = execution.Attack.Value;
            var characters = CombatCommandComposition.Project(execution).Characters;
            return string.Join("\n",
                "events: " + string.Join(", ", execution.Events.Select(Signature)),
                "previewEvents: " + string.Join(", ", projection.Events.Select(Signature)),
                "hits: " + string.Join(", ", attack.Hits.ToArray().Select(h =>
                    $"{h.TargetId.Value}:{h.Role}:{h.AppliedSplashPercent}:{h.DamageDealt}:{h.TargetHealthAfter}")),
                "previewTargets: " + string.Join(", ", preview.Targets.ToArray().Select(t =>
                    $"{t.TargetId.Value}:{t.Role}:{t.ExpectedDamage}:{t.SecondaryDamage}:{t.TargetHealthAfter}")),
                "affected: " + string.Join(", ", attack.AffectedCells.ToArray().Select(c => $"({c.X},{c.Y})")),
                "final: " + string.Join(", ", characters.OrderBy(c => c.EntityId).Select(c => $"C{c.EntityId.Value}:{c.Health.CurrentHealth}")
                    .Concat(execution.FinalState.Structures.OrderBy(st => st.EntityId).Select(st => $"S{st.EntityId.Value}:{st.Health.CurrentHealth}")))
                    + $", souls:{execution.Field.Final.Entries.Count}");
        }

        /// <summary>사건 종류와 대상 식별자만 남긴 요약이다. 순서 비교에 쓴다.</summary>
        private static string Signature(CombatEvent e) => e switch
        {
            CombatCharacterEvent c => $"{c.Kind}:{c.Final.EntityId.Value}",
            CombatStructureEvent s => $"Structure{s.Kind}:{s.Final.EntityId.Value}",
            CombatObstacleDurabilityEvent d => $"Durability:{d.ObstacleId.Value}:{d.Final.ObstacleDurability.RemainingHits}",
            CombatHeldEnvironmentEvent h => $"Held:{h.CharacterId.Value}",
            CombatObstacleReactionEvent o => $"Obstacle{o.Reaction}:{o.ObstacleId.Value}",
            CombatMovementSourceEvent m => $"Source:{(m.Final ?? m.Expected).Id}",
            CombatObjectActivationEvent a => $"Object:{a.Source.Id}:{a.ActivatorId.Value}",
            CombatEffectsEvent f => "Effects:" + string.Join("+", f.Traits.Select(t => t.TraitKey.ToString())) + $"/c{f.Categories.Count}",
            CombatFallEvent f => $"Fell:{f.EntityId.Value}",
            _ => e.Kind.ToString(),
        };
    }
}
