using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Chapter;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.StageMap;

namespace ProjectL.Tests.Core
{
    public sealed class ChapterOwnershipTests
    {
        [TestCase("StageMapRunState")]
        [TestCase("StageMapSelectionPlan")]
        public void Domain_DoesNotRetainASecondChapterProgressOwner(string type)
        {
            Assert.That(typeof(ChapterNavigationRules).Assembly.GetType(
                "ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.StageMap." + type), Is.Null);
        }

        [TestCase("StageMap.StageMapSession")]
        [TestCase("Run.RunProgressionService")]
        public void Application_DoesNotRetainASecondChapterProgressOwner(string type)
        {
            Assert.That(typeof(ChapterProgressionCommands).Assembly.GetType(
                "ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application." + type), Is.Null);
        }
    }
}
