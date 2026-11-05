using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class ThornHeartReflectionContractTests
    {
        [Test]
        public void LegacyArtifactEvent_HasNoReflectionContext()
        {
            var entry = new CombatArtifactEvent(new[] { Activation() });
            Assert.That(entry.Reflection, Is.Null);
            Assert.That(entry.Kind, Is.EqualTo(CombatEventKind.ArtifactActivated));
        }

        [Test]
        public void ReflectionContext_PreservesStruckAllyAndDistinctGroups()
        {
            var context = Context(new EntityId(7), 1, 2);
            var entry = Event(new[] { Activation() }, context);
            Assert.That(context.StruckAllyId, Is.EqualTo(new EntityId(7)));
            Assert.That(context.TriggeringAttackGroup, Is.EqualTo(1L));
            Assert.That(context.ReflectionAttackGroup, Is.EqualTo(2L));
            Assert.That(entry.Reflection, Is.SameAs(context));
            Assert.That(entry.Activations[0].TargetId, Is.EqualTo(new EntityId(2)));
        }

        [TestCase(0, 1, 2)]
        [TestCase(7, 0, 2)]
        [TestCase(7, -1, 2)]
        [TestCase(7, 1, 0)]
        [TestCase(7, 1, -1)]
        [TestCase(7, 1, 1)]
        public void ReflectionContext_RejectsEmptyIdOrInvalidGroups(int ally, long trigger, long reflection)
        {
            Assert.Catch<ArgumentException>(() => Context(ally == 0 ? EntityId.Empty : new EntityId(ally),
                trigger, reflection));
        }

        [TestCase("empty")]
        [TestCase("multiple")]
        [TestCase("trait")]
        [TestCase("null-entry")]
        public void ReflectionEvent_RequiresOneArtifactActivation(string invalidCase)
        {
            var context = Context(new EntityId(7), 1, 2);
            ArtifactActivationSnapshot[] entries = invalidCase == "empty" ? Array.Empty<ArtifactActivationSnapshot>()
                : invalidCase == "multiple" ? new[] { Activation(), Activation() }
                : invalidCase == "trait" ? new[] { Activation(EffectOwnerKind.Trait) }
                : new ArtifactActivationSnapshot[] { null };
            Assert.Catch<ArgumentException>(() => Event(entries, context));
        }

        [Test]
        public void ReflectionEvent_CopiesCallerActivationArray()
        {
            var original = Activation();
            var entries = new[] { original };
            var entry = Event(entries, Context(new EntityId(7), 1, 2));
            entries[0] = Activation();
            Assert.That(entry.Activations[0], Is.SameAs(original));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<ArtifactActivationSnapshot>)entry.Activations)[0] = Activation());
        }

        private static ArtifactActivationSnapshot Activation(EffectOwnerKind kind = EffectOwnerKind.Artifact)
        {
            return new ArtifactActivationSnapshot(new EffectOriginKey(kind, 13, 1002, "night_reflect_damage"),
                new EntityId(2), 3, 3);
        }

        private static ThornHeartReflectionContext Context(EntityId ally, long trigger, long reflection)
        {
            return new ThornHeartReflectionContext(ally, trigger, reflection);
        }

        private static CombatArtifactEvent Event(ArtifactActivationSnapshot[] entries, ThornHeartReflectionContext context)
        {
            return new CombatArtifactEvent(entries, context);
        }
    }
}
