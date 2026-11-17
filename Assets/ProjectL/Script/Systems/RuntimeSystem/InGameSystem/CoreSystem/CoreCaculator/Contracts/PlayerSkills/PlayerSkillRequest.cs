using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills
{
    public enum PlayerSkillFailureReason
    {
        None = 0,
        NoActiveBattle,
        NotPlayerCombatTurn,
        NotSelectedMain,
        ActorUnavailable,
        NoSkillConfigured,
        Disarmed,
        Stunned,
        StageUsesExhausted,
        UsedThisTurn,
        WrongPointCount,
        PointOutsideBoard,
        PointOnWall,
        PointOutOfRange,
        InvalidSegmentDirection,
        SegmentBlockedByWall,
        SourceChanged,
        BattleChanged
    }

    public sealed class PlayerSkillRequest
    {
        public IReadOnlyList<CellCoord> Points { get; }
        public PlayerSkillSourceStamp? ExpectedSource { get; }

        public PlayerSkillRequest(IReadOnlyList<CellCoord> points, PlayerSkillSourceStamp? expectedSource = null)
        {
            Points = Array.AsReadOnly((points ?? throw new ArgumentNullException(nameof(points))).ToArray());
            if (expectedSource.HasValue && expectedSource.Value.BattleInstanceId == 0)
                throw new ArgumentException("기대 출처에는 유효한 전투 식별자가 필요합니다.", nameof(expectedSource));
            ExpectedSource = expectedSource;
        }
    }
}
