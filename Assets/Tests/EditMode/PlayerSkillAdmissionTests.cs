using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class PlayerSkillAdmissionTests
    {
        [Test]
        public void AfterOrdinaryAttackAndUnderBind_CanStillCastWithoutChangingFacts()
        {
            var original = PlayerSkillTestData.Input();
            var actor = original.Characters.Single(c => c.EntityId == original.ActorId);
            var bound = new ActionRestrictionStatus(StatusEffectKey.Bind,
                new EffectOriginKey(EffectOwnerKind.Item, 2, 1, "bind"), 1);
            var input = ReplaceActor(original, actor.With(actionUse: UnitActionUse.Attacked)
                .WithStatusEffects(new StatusEffectSet(new AppliedStatus[] { bound })));

            var result = Query(input);

            Assert.That(result.CanUse, Is.True);
            Assert.That(result.SkillKey, Is.EqualTo(PlayerSkillKey.PowderBlossom));
            Assert.That(result.MainInstanceId, Is.EqualTo(PlayerSkillTestData.MainOwner));
            Assert.That(result.StageUseLimit, Is.EqualTo(2));
            Assert.That(result.RemainingUses, Is.EqualTo(2));
            Assert.That(result.Source, Is.EqualTo(input.PlayerSkillSource.Value));
            Assert.That(input.State.PlayerSkill, Is.SameAs(original.State.PlayerSkill));
            Assert.That(input.State.ItemRandomState, Is.EqualTo(original.State.ItemRandomState));
            Assert.That(input.Characters.Single(c => c.EntityId == input.ActorId).ActionUse, Is.EqualTo(UnitActionUse.Attacked));
        }

        [TestCase("disarmed", PlayerSkillFailureReason.Disarmed)]
        [TestCase("enemy turn", PlayerSkillFailureReason.NotPlayerCombatTurn)]
        [TestCase("deployment", PlayerSkillFailureReason.NotPlayerCombatTurn)]
        [TestCase("absent", PlayerSkillFailureReason.ActorUnavailable)]
        [TestCase("dead", PlayerSkillFailureReason.ActorUnavailable)]
        [TestCase("off field", PlayerSkillFailureReason.ActorUnavailable)]
        [TestCase("sub", PlayerSkillFailureReason.NotSelectedMain)]
        [TestCase("no skill", PlayerSkillFailureReason.NoSkillConfigured)]
        [TestCase("no battle", PlayerSkillFailureReason.NoActiveBattle)]
        [TestCase("spent stage", PlayerSkillFailureReason.StageUsesExhausted)]
        [TestCase("used turn", PlayerSkillFailureReason.UsedThisTurn)]
        [TestCase("stale revision", PlayerSkillFailureReason.SourceChanged)]
        [TestCase("different battle", PlayerSkillFailureReason.BattleChanged)]
        public void AdmissionReportsTheSpecificReason(string context, PlayerSkillFailureReason expected)
        {
            var input = PlayerSkillTestData.Input();
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var completed = input.State.PlayerSkill;
            var stamp = input.PlayerSkillSource.Value;
            switch (context)
            {
                case "disarmed":
                    var disarm = new ActionRestrictionStatus(StatusEffectKey.Disarm,
                        new EffectOriginKey(EffectOwnerKind.Item, 2, 1, "disarm"), 1);
                    input = ReplaceActor(input, actor.WithStatusEffects(new StatusEffectSet(new AppliedStatus[] { disarm })));
                    break;
                case "enemy turn":
                    input = WithState(input, input.State.With(phase: CombatFlowPhase.Automatic,
                        turn: new CombatTurnFacts(StagePhase.Combat, CombatControllerPhase.Enemy, 1)));
                    break;
                case "deployment":
                    input = WithState(input, input.State.With(turn: new CombatTurnFacts(StagePhase.Deployment)));
                    stamp = new PlayerSkillSourceStamp(1, 0, 7, input.ActorId);
                    break;
                case "absent":
                    input = input.WithActor(new EntityId(99));
                    stamp = new PlayerSkillSourceStamp(1, 1, 7, input.ActorId);
                    break;
                case "dead":
                    input = ReplaceActor(input, actor.WithHealth(new HealthStateSnapshot(0, actor.Health.MaxHealth)));
                    break;
                case "off field":
                    input = ReplaceActor(input, actor.WithPlacement(CharacterRuntimeArea.Hand, 0, null, null, Array.Empty<CellCoord>()));
                    break;
                case "sub":
                    input = input.WithActor(new EntityId(1));
                    stamp = new PlayerSkillSourceStamp(1, 1, 7, input.ActorId);
                    break;
                case "no skill":
                    input = PlayerSkillTestData.Input(configured: false);
                    break;
                case "no battle":
                    input = PlayerSkillTestData.Input(battleId: 0);
                    stamp = default;
                    break;
                case "spent stage":
                    input = WithState(input, input.State.With(playerSkill: completed.WithUsage(0, 0)));
                    break;
                case "used turn":
                    input = WithState(input, input.State.With(playerSkill: completed.WithUsage(1, 1)));
                    break;
                case "stale revision":
                    stamp = new PlayerSkillSourceStamp(1, 1, 6, input.ActorId);
                    break;
                case "different battle":
                    stamp = new PlayerSkillSourceStamp(2, 1, 7, input.ActorId);
                    break;
                default:
                    Assert.Fail(context);
                    break;
            }

            var result = PlayerSkillQueries.Availability(input, stamp);
            Assert.That(result.CanUse, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo(expected));
            Assert.That(result.UsedThisTurn, Is.EqualTo(context == "used turn"));
        }

        internal static PlayerSkillAvailability Query(CombatCommandInput input) =>
            PlayerSkillQueries.Availability(input, input.PlayerSkillSource.Value);

        internal static CombatCommandInput ReplaceActor(CombatCommandInput input, CharacterCombatFacts actor) =>
            input.WithWorld(input.State, input.Characters.Select(c => c.EntityId == actor.EntityId ? actor : c).ToArray(),
                input.Main, input.SoulCounts, input.Souls, input.EnvironmentBindings);

        private static CombatCommandInput WithState(CombatCommandInput input, CombatCommandState state) =>
            input.WithWorld(state, input.Characters, input.Main, input.SoulCounts, input.Souls, input.EnvironmentBindings);
    }
}
