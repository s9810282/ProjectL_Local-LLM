using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Map.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.StageSystem;

namespace ProjectL.Tests.Framework
{
    internal sealed class ChapterContinuationTestDatabase : CoreStageCombatCycleTests.CycleDatabase
    {
        internal Action<string> OnQuery;
        internal Func<int, StageRootData> RootOverride;
        internal Func<int, StageMapData> MapOverride;
        private readonly StageRootData[] _roots = {
            Stage(101, 1, StageMapRole.Regular), Stage(102, 1, StageMapRole.Boss),
            Stage(201, 2, StageMapRole.Regular), Stage(202, 2, StageMapRole.Boss),
            Stage(301, 3, StageMapRole.Regular), Stage(302, 3, StageMapRole.Boss) };

        public override StageRootData GetStage(int key)
        {
            OnQuery?.Invoke("root:" + key);
            return RootOverride?.Invoke(key) ?? _roots.Single(x => x.StageKey == key);
        }
        public override IReadOnlyList<StageRootData> GetStages(int key)
        {
            OnQuery?.Invoke("stages:" + key);
            return _roots.Where(x => x.MapKey == key).ToArray();
        }
        public override StageMapData GetStageMap(int key)
        {
            OnQuery?.Invoke("map:" + key);
            return MapOverride?.Invoke(key) ?? Map(key);
        }
        public override IReadOnlyCollection<StageMapData> GetAllStageMaps() => new[] { Map(1), Map(2), Map(3) };
        private static StageMapData Map(int key) => new(key, 3, 1, 0, StageConnectionAlgorithm.Nearest, 1, 1, 42, false);
    }

    internal sealed class ChapterContinuationTestSource : IChapterContinuationSource
    {
        internal ChapterContinuationDefinition Definition = ChapterContinuationDefinition.Next(1, 2);
        internal Action BeforeRead;
        internal Exception Failure;
        internal int Reads;
        public ChapterContinuationDefinition Read(int chapterKey)
        {
            Reads++;
            BeforeRead?.Invoke();
            if (Failure != null) throw Failure;
            return Definition;
        }
    }
}
