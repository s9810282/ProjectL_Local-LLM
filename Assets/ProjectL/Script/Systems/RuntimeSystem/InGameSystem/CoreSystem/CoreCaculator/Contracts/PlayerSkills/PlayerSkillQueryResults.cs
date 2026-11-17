using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills
{
    public sealed class PlayerSkillAvailability
    {
        public PlayerSkillSourceStamp Source { get; }
        public PlayerSkillKey SkillKey { get; }
        public PlayerSkillUpgradeKey UpgradeKey { get; }
        public CharacterInstanceId MainInstanceId { get; }
        public int RemainingUses { get; }
        public int StageUseLimit { get; }
        public bool UsedThisTurn { get; }
        public bool CanUse => FailureReason == PlayerSkillFailureReason.None;
        public PlayerSkillFailureReason FailureReason { get; }

        public PlayerSkillAvailability(PlayerSkillSourceStamp source, PlayerSkillBattleState skill,
            bool usedThisTurn, PlayerSkillFailureReason failureReason)
        {
            if (skill == null && (failureReason == PlayerSkillFailureReason.None || usedThisTurn))
                throw new ArgumentException("사용 정보에는 결합된 스킬이 필요합니다.", nameof(skill));

            Source = source;
            SkillKey = skill?.Definition.Key ?? PlayerSkillKey.None;
            UpgradeKey = skill?.UpgradeKey ?? PlayerSkillUpgradeKey.None;
            MainInstanceId = skill?.MainInstanceId ?? CharacterInstanceId.Empty;
            RemainingUses = skill?.RemainingUses ?? 0;
            StageUseLimit = skill?.Definition.StageUseLimit ?? 0;
            UsedThisTurn = usedThisTurn;
            FailureReason = failureReason;
        }
    }

    public sealed class PlayerSkillSelectionResult
    {
        public PlayerSkillSourceStamp Source { get; }
        public IReadOnlyList<CellCoord> SelectedPoints { get; }
        public int RequiredPointCount { get; }
        public bool PrefixIsValid => FailureReason == PlayerSkillFailureReason.None;
        public bool IsComplete => PrefixIsValid && SelectedPoints.Count == RequiredPointCount;
        public IReadOnlyList<CellCoord> NextCandidateCells { get; }
        public PlayerSkillFailureReason FailureReason { get; }

        public PlayerSkillSelectionResult(PlayerSkillSourceStamp source, IReadOnlyList<CellCoord> selectedPoints,
            int requiredPointCount, IReadOnlyList<CellCoord> nextCandidateCells, PlayerSkillFailureReason failureReason)
        {
            if (requiredPointCount < 0) throw new ArgumentOutOfRangeException(nameof(requiredPointCount));
            Source = source;
            SelectedPoints = Array.AsReadOnly((selectedPoints ?? throw new ArgumentNullException(nameof(selectedPoints))).ToArray());
            RequiredPointCount = requiredPointCount;
            NextCandidateCells = Array.AsReadOnly((nextCandidateCells ?? throw new ArgumentNullException(nameof(nextCandidateCells))).ToArray());
            FailureReason = failureReason;
            if (PrefixIsValid && SelectedPoints.Count > requiredPointCount)
                throw new ArgumentException("유효한 선택은 필요 지점 수를 넘을 수 없습니다.", nameof(selectedPoints));
            if ((!PrefixIsValid || IsComplete) && NextCandidateCells.Count != 0)
                throw new ArgumentException("불가하거나 완료된 선택은 다음 후보가 없습니다.", nameof(nextCandidateCells));
        }
    }

    public enum PlayerSkillForecastCoverage
    {
        None = 0,
        KnownInformationConditional = 1
    }

    public sealed class PlayerSkillForecast
    {
        public PlayerSkillSourceStamp Source { get; }
        public bool CanUse => FailureReason == PlayerSkillFailureReason.None;
        public PlayerSkillFailureReason FailureReason { get; }
        public PlayerSkillForecastCoverage Coverage { get; }
        public PlayerSkillResult Prediction { get; }
        public IReadOnlyList<CellCoord> SelectedPoints { get; }

        public PlayerSkillForecast(PlayerSkillSourceStamp source, PlayerSkillFailureReason failureReason,
            PlayerSkillForecastCoverage coverage, PlayerSkillResult prediction, IReadOnlyList<CellCoord> selectedPoints = null)
        {
            bool accepted = failureReason == PlayerSkillFailureReason.None;
            if (accepted ? coverage != PlayerSkillForecastCoverage.KnownInformationConditional || prediction == null :
                coverage != PlayerSkillForecastCoverage.None || prediction != null)
                throw new ArgumentException("예보의 범위와 결과가 사용 판정에 일치해야 합니다.", nameof(coverage));
            Source = source;
            FailureReason = failureReason;
            Coverage = coverage;
            Prediction = prediction;
            SelectedPoints = Array.AsReadOnly((selectedPoints ?? Array.Empty<CellCoord>()).ToArray());
        }
    }
}
