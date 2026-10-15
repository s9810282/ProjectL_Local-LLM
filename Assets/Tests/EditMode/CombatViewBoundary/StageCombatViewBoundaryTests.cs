using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.CombatViewBoundary
{
    public sealed class StageCombatViewBoundaryTests
    {
        [Test]
        public void CurrentCalculationBoundary_UsesOnlyPublicContractValues()
        {
            var assembly = typeof(CombatCommandCalculator).Assembly;
            Assert.That(typeof(CombatResult).IsPublic, Is.True);
            Assert.That(assembly.GetType("ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.StageRun.StageSession"), Is.Null);
            foreach (var method in typeof(CombatCommandCalculator).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                Assert.That(method.ReturnType.Namespace, Does.Not.Contain(".Domain"));
                foreach (var parameter in method.GetParameters())
                    Assert.That(parameter.ParameterType.Namespace, Does.Not.Contain(".Domain"));
            }
        }

        [Test]
        public void PublishedResult_PreservesItsEventsAfterLaterResults()
        {
            var firstEvent = new CombatEffectsEvent(Array.Empty<TraitActivationSnapshot>(), Array.Empty<CategoryActivationSnapshot>());
            CombatEvent[] events = { firstEvent };
            var first = new CombatResult(1, CombatProcessingKind.Command, 1, true, "", events: events);
            events[0] = null;
            var later = new CombatResult(2, CombatProcessingKind.Command, 2, false, "Rejected");
            Assert.That(first.Events.Single(), Is.SameAs(firstEvent));
            Assert.That(first.Accepted, Is.True);
            Assert.That(later.Events, Is.Empty);
            Assert.That(first.Sequence, Is.EqualTo(1));
        }
    }
}
