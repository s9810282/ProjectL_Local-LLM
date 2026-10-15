using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public sealed class StageSelectionResult
    {
        public bool Succeeded { get; }
        public StageSelectionFailureReason FailureReason { get; }
        public StageKey SelectedStageKey { get; }
        public StageMapSnapshot CurrentMap { get; }

        private StageSelectionResult(bool succeeded, StageSelectionFailureReason failureReason, StageKey stageKey, StageMapSnapshot currentMap)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
            SelectedStageKey = stageKey;
            CurrentMap = currentMap;
        }

        public static StageSelectionResult Success(StageKey selectedStageKey, StageMapSnapshot currentMap)
        {
            if (selectedStageKey.IsEmpty)
                throw new ArgumentException(nameof(selectedStageKey));
            if (currentMap == null)
                throw new ArgumentNullException(nameof(currentMap));

            return new StageSelectionResult(true, StageSelectionFailureReason.None, selectedStageKey, currentMap);
        }

        public static StageSelectionResult Failure(StageSelectionFailureReason failureReason, StageMapSnapshot currentMap)
        {
            if (failureReason == StageSelectionFailureReason.None ||
                Enum.IsDefined(typeof(StageSelectionFailureReason), failureReason) == false)
            {
                throw new ArgumentOutOfRangeException(nameof(failureReason));
            }
            if (currentMap == null)
                throw new ArgumentNullException(nameof(currentMap));

            return new StageSelectionResult(false, failureReason, StageKey.Empty, currentMap);
        }
    }
}
