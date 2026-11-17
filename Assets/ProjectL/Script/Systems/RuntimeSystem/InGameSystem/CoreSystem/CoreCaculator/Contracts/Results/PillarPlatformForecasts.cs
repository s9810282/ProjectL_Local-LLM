using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    /// <summary>가상 공격의 낙하·잔해·연쇄 근거를 사건 순서대로 읽는다.</summary>
    public sealed class PillarFallPreview
    {
        public EntityId PillarId { get; }
        public CellCoord PillarCell { get; }
        public CellCoord Direction { get; }
        public int FallLength { get; }
        public IReadOnlyList<CellCoord> ImpactCells { get; }
        public IReadOnlyList<EntityId> CrushedUnitIds { get; }
        public IReadOnlyList<EntityId> DestroyedObstacleIds { get; }
        public IReadOnlyList<PillarTerrainPreview> TerrainChanges { get; }
        public IReadOnlyList<CombatObstacleReactionEvent> ChainReactions { get; }

        internal PillarFallPreview(CombatPillarFallEvent fall, IReadOnlyList<CombatEvent> events)
        {
            PillarId = fall.PillarId;
            PillarCell = fall.PillarCell;
            Direction = fall.Direction;
            FallLength = fall.FallLength;
            ImpactCells = fall.ImpactCells;
            CrushedUnitIds = fall.CrushedUnitIds;
            DestroyedObstacleIds = fall.DestroyedObstacleIds;
            TerrainChanges = Array.AsReadOnly(events.OfType<CombatPillarTerrainEvent>()
                .Where(e => e.Fall == fall).Select(e => new PillarTerrainPreview(e.Final.Cell, e.Final.Kind)).ToArray());

            // 내구도 사건의 원인 연결을 따라 동일 명령의 연쇄만 모은다.
            var contexts = new HashSet<CombatEvent> { fall, fall.Reaction };
            var reactions = new List<CombatObstacleReactionEvent>();
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i] is CombatObstacleDurabilityEvent changed && contexts.Contains(changed.ContextEvent) &&
                    i + 1 < events.Count && events[i + 1] is CombatObstacleReactionEvent reaction &&
                    reaction.ObstacleId == changed.ObstacleId)
                {
                    reactions.Add(reaction);
                    contexts.Add(reaction);
                }
                else if (events[i] is CombatPillarFallEvent child && contexts.Contains(child.Reaction))
                    contexts.Add(child);
            }
            ChainReactions = reactions.AsReadOnly();
        }
    }

    /// <summary>미리보기의 지형 결과다. 실제 원본 ID는 게시된 지형 사건에서 읽는다.</summary>
    public readonly struct PillarTerrainPreview
    {
        public CellCoord Cell { get; }
        public StaticTerrainKind Kind { get; }

        internal PillarTerrainPreview(CellCoord cell, StaticTerrainKind kind)
        {
            Cell = cell;
            Kind = kind;
        }
    }

    public enum PlatformForecastCoverage
    {
        Exact = 1,
        KnownInformationConditional
    }

    /// <summary>예측의 입력 판과 계산 대상 경계다. 최신 조회와 대조하여 이전 응답을 버린다.</summary>
    public readonly struct PlatformForecastSource
    {
        public long BattleInstanceId { get; }
        public long QueryRevision { get; }
        public long ControllerTurnSequence { get; }
        public long CompletedControllerTurnCount { get; }
        public long LastPlatformBoundaryCount { get; }

        internal PlatformForecastSource(long battleInstanceId, long queryRevision, CombatCommandState state)
        {
            BattleInstanceId = battleInstanceId;
            QueryRevision = queryRevision;
            ControllerTurnSequence = state.Turn.ControllerTurnSequence;
            CompletedControllerTurnCount = state.Turn.CompletedControllerTurnCount;
            LastPlatformBoundaryCount = state.LastPlatformBoundaryCount;
        }

        public bool Matches(long battleInstanceId, long queryRevision, CombatCommandState state) => state != null &&
            BattleInstanceId == battleInstanceId && QueryRevision == queryRevision &&
            ControllerTurnSequence == state.Turn.ControllerTurnSequence &&
            CompletedControllerTurnCount == state.Turn.CompletedControllerTurnCount &&
            LastPlatformBoundaryCount == state.LastPlatformBoundaryCount;
    }

    /// <summary>지금 읽을 수 있는 입력으로 계산한 다음 플랫폼 사전 이동의 결과다.</summary>
    public sealed class PlatformForecastResult
    {
        public PlatformForecastSource Source { get; }
        public long? ProjectedBoundaryCount { get; }
        public PlatformForecastCoverage Coverage { get; }
        public IReadOnlyList<PlatformForecastPath> Paths { get; }

        internal PlatformForecastResult(PlatformForecastSource source, long? projectedBoundaryCount,
            PlatformForecastCoverage coverage, IEnumerable<CombatPlatformMotionEvent> motions)
        {
            Source = source;
            ProjectedBoundaryCount = projectedBoundaryCount;
            Coverage = coverage;
            Paths = Array.AsReadOnly(motions.Select(m => new PlatformForecastPath(m)).ToArray());
        }
    }

    public sealed class PlatformForecastPath
    {
        public long PlatformSourceId { get; }
        public int OrderIndex { get; }
        public CellCoord InitialAnchor { get; }
        public CellCoord FinalAnchor { get; }
        public int InitialSign { get; }
        public int FinalSign { get; }
        public PlatformMotionStopReason StopReason { get; }
        public IReadOnlyList<PlatformForecastStep> Steps { get; }

        internal PlatformForecastPath(CombatPlatformMotionEvent motion)
        {
            PlatformSourceId = motion.PlatformSourceId;
            OrderIndex = motion.OrderIndex;
            InitialAnchor = motion.Expected.Cell;
            FinalAnchor = motion.Final.Cell;
            InitialSign = motion.Expected.TravelSign;
            FinalSign = motion.Final.TravelSign;
            StopReason = motion.StopReason;
            Steps = Array.AsReadOnly(motion.Steps.Select(step => new PlatformForecastStep(step)).ToArray());
        }
    }

    public sealed class PlatformForecastStep
    {
        public PlatformStepKind Kind { get; }
        public CellCoord From { get; }
        public CellCoord To { get; }
        public int TravelSign { get; }
        public IReadOnlyList<PlatformPassengerStep> Passengers { get; }

        internal PlatformForecastStep(CombatPlatformStepEvent step)
        {
            Kind = step.StepKind;
            From = step.Expected.Cell;
            To = step.Final.Cell;
            TravelSign = step.Final.TravelSign;
            Passengers = step.Passengers;
        }
    }
}
