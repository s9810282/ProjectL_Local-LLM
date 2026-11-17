using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public enum PlatformStepKind { Translation = 1, Turnaround }
    public enum PlatformMotionStopReason { BudgetConsumed = 1, Blocked, NontransportableOccupant, Terminal }

    /// <summary>한 이동 단계의 실제 운반과 그 단계 효과 뒤의 운반 종료 여부다.</summary>
    public sealed class PlatformPassengerStep
    {
        public EntityId EntityId { get; }
        public CellCoord From { get; }
        public CellCoord To { get; }
        public bool TransportEnded { get; }

        public PlatformPassengerStep(EntityId entityId, CellCoord from, CellCoord to, bool transportEnded)
        {
            if (entityId.IsEmpty || from == to) throw new ArgumentException("실제로 옮긴 탑승자의 식별자와 두 칸이 필요합니다.");
            EntityId = entityId;
            From = from;
            To = to;
            TransportEnded = transportEnded;
        }
    }

    /// <summary>사전 이동과 그 파생 효과가 기존 환경 난수에서 소비한 한 번의 선택이다.</summary>
    public sealed class CombatEnvironmentRandomEvent : CombatEvent
    {
        public uint ExpectedState { get; }
        public uint FinalState { get; }
        public int ChoiceCount { get; }
        public int ChoiceIndex { get; }

        public CombatEnvironmentRandomEvent(uint expectedState, uint finalState, int choiceCount, int choiceIndex)
            : base(CombatEventKind.EnvironmentRandomSelected)
        {
            if (choiceCount < 2 || choiceIndex < 0 || choiceIndex >= choiceCount || expectedState == finalState)
                throw new ArgumentException("환경 난수 사건은 둘 이상 후보의 실제 선택이어야 합니다.");
            uint calculatedState = expectedState;
            if (EnvironmentRandomSequence.NextIndex(ref calculatedState, choiceCount) != choiceIndex || calculatedState != finalState)
                throw new ArgumentException("환경 난수의 결과 상태와 선택값이 시작 상태에 맞지 않습니다.");
            ExpectedState = expectedState;
            FinalState = finalState;
            ChoiceCount = choiceCount;
            ChoiceIndex = choiceIndex;
        }
    }

    /// <summary>한 진영 시작 전 처리의 경계와 전체 플랫폼 순서를 기록한다. 연출 대기를 저장하지 않는다.</summary>
    public sealed class CombatPlatformBoundaryEvent : CombatEvent
    {
        public long BoundaryCount { get; }
        public IReadOnlyList<long> OrderedPlatformIds { get; }
        public IReadOnlyList<CombatEnvironmentRandomEvent> OrderingDraws { get; }

        public CombatPlatformBoundaryEvent(long boundaryCount, IReadOnlyList<long> orderedPlatformIds,
            IReadOnlyList<CombatEnvironmentRandomEvent> orderingDraws) : base(CombatEventKind.PlatformPrelude)
        {
            if (boundaryCount < 1) throw new ArgumentOutOfRangeException(nameof(boundaryCount));
            var ids = EnvironmentSemanticBindings.CopySourceIds(orderedPlatformIds, nameof(orderedPlatformIds));
            if (ids.Length == 0) throw new ArgumentException("사전 이동에는 플랫폼이 하나 이상 필요합니다.", nameof(orderedPlatformIds));
            var draws = (orderingDraws ?? throw new ArgumentNullException(nameof(orderingDraws))).ToArray();
            if (draws.Any(d => d == null) || draws.Distinct().Count() != draws.Length)
                throw new ArgumentException("순서 추첨 근거는 null이나 중복일 수 없습니다.", nameof(orderingDraws));
            BoundaryCount = boundaryCount;
            OrderedPlatformIds = Array.AsReadOnly(ids);
            OrderingDraws = Array.AsReadOnly(draws);
        }
    }

    /// <summary>플랫폼 원본 변경과 같은 위치 프레임의 탑승자 이동을 연결한다. 진입 효과는 이 사건 뒤에 온다.</summary>
    public sealed class CombatPlatformStepEvent : CombatEvent
    {
        public long BoundaryCount { get; }
        public int OrderIndex { get; }
        public int StepIndex { get; }
        public PlatformStepKind StepKind { get; }
        public CombatMovementSourceEvent SourceChange { get; }
        public MovingPlatformFact Expected => (MovingPlatformFact)SourceChange.Expected;
        public MovingPlatformFact Final => (MovingPlatformFact)SourceChange.Final;
        public long PlatformSourceId => Expected.Id;
        public IReadOnlyList<PlatformPassengerStep> Passengers { get; }
        public CombatEvent CompletionEvent { get; }

        public CombatPlatformStepEvent(long boundaryCount, int orderIndex, int stepIndex,
            PlatformStepKind stepKind, CombatMovementSourceEvent sourceChange, IReadOnlyList<PlatformPassengerStep> passengers,
            CombatEvent completionEvent)
            : base(CombatEventKind.PlatformStepped)
        {
            SourceChange = sourceChange ?? throw new ArgumentNullException(nameof(sourceChange));
            CompletionEvent = completionEvent ?? throw new ArgumentNullException(nameof(completionEvent));
            if (sourceChange.Expected is not MovingPlatformFact before || sourceChange.Final is not MovingPlatformFact after ||
                !before.Spec.SameValues(after.Spec) || !before.KnownTo.SequenceEqual(after.KnownTo))
                throw new ArgumentException("같은 플랫폼 정의와 공개 범위를 보존하는 단계가 필요합니다.", nameof(sourceChange));
            if (boundaryCount < 1 || orderIndex < 0 || stepIndex < 0) throw new ArgumentOutOfRangeException(nameof(stepIndex));
            if (!Enum.IsDefined(typeof(PlatformStepKind), stepKind)) throw new ArgumentOutOfRangeException(nameof(stepKind));
            var offset = after.Cell - before.Cell;
            bool translation = before.Spec.TravelAxis == Contracts.Commands.PlatformAxis.Horizontal
                ? offset == new CellCoord(before.TravelSign, 0) : offset == new CellCoord(0, before.TravelSign);
            if (stepKind == PlatformStepKind.Translation
                ? !translation || before.TravelSign != after.TravelSign
                : offset != CellCoord.Zero || before.TravelSign != -after.TravelSign)
                throw new ArgumentException("플랫폼 단계는 진행 방향 한 칸 이동 또는 제자리 반전이어야 합니다.");
            var riders = (passengers ?? throw new ArgumentNullException(nameof(passengers))).ToArray();
            if (riders.Any(r => r == null) || riders.Select(r => r.EntityId).Distinct().Count() != riders.Length ||
                !riders.Select(r => r.EntityId).SequenceEqual(riders.Select(r => r.EntityId).OrderBy(id => id)) ||
                stepKind == PlatformStepKind.Turnaround && riders.Length != 0 ||
                riders.Any(r => r.To - r.From != offset || !before.Occupies(r.From) || !after.Occupies(r.To)))
                throw new ArgumentException("탑승자는 ID 순으로 같은 플랫폼 이동량만큼 옮겨야 합니다.", nameof(passengers));
            BoundaryCount = boundaryCount;
            OrderIndex = orderIndex;
            StepIndex = stepIndex;
            StepKind = stepKind;
            Passengers = Array.AsReadOnly(riders);
        }
    }

    /// <summary>한 플랫폼의 순서 있는 이동·반전 단계를 요약한다. 원래 칸으로 돌아와도 단계는 보존한다.</summary>
    public sealed class CombatPlatformMotionEvent : CombatEvent
    {
        public long BoundaryCount { get; }
        public long PlatformSourceId => Expected.Id;
        public int OrderIndex { get; }
        public MovingPlatformFact Expected { get; }
        public MovingPlatformFact Final { get; }
        public IReadOnlyList<CombatPlatformStepEvent> Steps { get; }
        public PlatformMotionStopReason StopReason { get; }

        public CombatPlatformMotionEvent(long boundaryCount, int orderIndex, MovingPlatformFact expected,
            MovingPlatformFact final, IReadOnlyList<CombatPlatformStepEvent> steps, PlatformMotionStopReason stopReason)
            : base(CombatEventKind.PlatformMotionResolved)
        {
            Expected = expected ?? throw new ArgumentNullException(nameof(expected));
            Final = final ?? throw new ArgumentNullException(nameof(final));
            if (boundaryCount < 1 || orderIndex < 0) throw new ArgumentOutOfRangeException(nameof(orderIndex));
            if (!expected.Spec.SameValues(final.Spec) || !expected.KnownTo.SequenceEqual(final.KnownTo))
                throw new ArgumentException("플랫폼 이동은 정의와 공개 범위를 보존해야 합니다.");
            if (!Enum.IsDefined(typeof(PlatformMotionStopReason), stopReason)) throw new ArgumentOutOfRangeException(nameof(stopReason));
            var path = (steps ?? throw new ArgumentNullException(nameof(steps))).ToArray();
            if (path.Any(s => s == null) || path.Length > expected.Spec.Length * 2 + 1 ||
                path.Where((s, i) => s.BoundaryCount != boundaryCount || s.OrderIndex != orderIndex ||
                    s.StepIndex != i || s.PlatformSourceId != expected.Id).Any())
                throw new ArgumentException("플랫폼 단계의 경계·순서·길이가 올바르지 않습니다.", nameof(steps));
            var current = expected;
            foreach (var step in path)
            {
                if (!current.SameValues(step.Expected)) throw new ArgumentException("플랫폼 단계가 연속하지 않습니다.", nameof(steps));
                current = step.Final;
            }
            if (!current.SameValues(final)) throw new ArgumentException("플랫폼 최종값이 단계 경로와 다릅니다.", nameof(final));
            BoundaryCount = boundaryCount;
            OrderIndex = orderIndex;
            Steps = Array.AsReadOnly(path);
            StopReason = stopReason;
        }
    }
}
