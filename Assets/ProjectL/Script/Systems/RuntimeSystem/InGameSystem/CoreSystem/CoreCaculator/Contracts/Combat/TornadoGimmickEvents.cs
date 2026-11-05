using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>정지 영역 진입은 -1, 순회는 출발 중심 0부터 실제 도달한 중심 순번을 사용한다.</summary>
    public sealed class TornadoContactOrigin
    {
        public StageGimmickId GimmickId { get; }
        public long SourceId { get; }
        public CellCoord Center { get; }
        public int StepIndex { get; }

        public TornadoContactOrigin(StageGimmickId gimmickId, long sourceId, CellCoord center, int stepIndex)
        {
            if (gimmickId.IsEmpty || sourceId < 1 || center.X < 0 || center.Y < 0 || stepIndex < -1)
                throw new ArgumentException("토네이도 접촉의 원본·중심·순번이 잘못되었습니다.");
            GimmickId = gimmickId;
            SourceId = sourceId;
            Center = center;
            StepIndex = stepIndex;
        }
    }

    public sealed class TornadoMemberAdvance
    {
        public TornadoMemberState Expected { get; }
        public TornadoMemberState Final { get; }
        public int SideLength { get; }
        public IReadOnlyList<CellCoord> Path { get; }
        public bool IsTravel => Expected.Phase == TornadoGimmickPhase.Warning;

        public TornadoMemberAdvance(TornadoMemberState expected, TornadoMemberState final, int sideLength, IReadOnlyList<CellCoord> path)
        {
            Expected = expected ?? throw new ArgumentNullException(nameof(expected));
            Final = final ?? throw new ArgumentNullException(nameof(final));
            if (expected.SourceId != final.SourceId || sideLength < 1 || sideLength % 2 == 0)
                throw new ArgumentException("토네이도 진행의 원본·영역이 잘못되었습니다.");
            var copy = (path ?? throw new ArgumentNullException(nameof(path))).ToArray();
            if (copy.Length == 0 || copy[0] != expected.Center || copy[copy.Length - 1] != final.Center ||
                copy.Any(c => c.X < 0 || c.Y < 0) || !IsTravel && copy.Length != 1)
                throw new ArgumentException("토네이도 진행에는 실제 중심의 접두 경로가 필요합니다.");
            SideLength = sideLength;
            Path = Array.AsReadOnly(copy);
        }
    }

    /// <summary>한 완료 경계의 그룹 변경 하나다. 실제 도달·착지 사건은 이 기록 뒤에 순서대로 나온다.</summary>
    public sealed class CombatTornadoGimmickEvent : CombatEvent
    {
        public TornadoGimmickState Expected { get; }
        public TornadoGimmickState Final { get; }
        public StageGimmickBoundary Boundary { get; }
        public IReadOnlyList<TornadoMemberAdvance> Advances { get; }

        public CombatTornadoGimmickEvent(TornadoGimmickState expected, TornadoGimmickState final,
            StageGimmickBoundary boundary, IReadOnlyList<TornadoMemberAdvance> advances) : base(CombatEventKind.TornadoAdvanced)
        {
            Expected = expected ?? throw new ArgumentNullException(nameof(expected));
            Final = final ?? throw new ArgumentNullException(nameof(final));
            var copy = (advances ?? throw new ArgumentNullException(nameof(advances))).ToArray();
            if (expected.Id != final.Id || !expected.Members.Select(m => m.SourceId).SequenceEqual(final.Members.Select(m => m.SourceId)) ||
                copy.Length == 0 || copy.Any(a => a == null) ||
                !copy.Select(a => a.Expected.SourceId).SequenceEqual(copy.Select(a => a.Expected.SourceId).Distinct().OrderBy(id => id)))
                throw new ArgumentException("토네이도 그룹 사건의 식별자·원본 순서가 잘못되었습니다.");
            Boundary = boundary;
            Advances = Array.AsReadOnly(copy);
        }
    }

    public sealed class CombatTornadoCenterEvent : CombatEvent
    {
        public TornadoContactOrigin Origin { get; }
        public CellCoord Center => Origin.Center;
        public CombatTornadoCenterEvent(TornadoContactOrigin origin) : base(CombatEventKind.TornadoCenterReached)
        {
            Origin = origin ?? throw new ArgumentNullException(nameof(origin));
            if (origin.StepIndex < 0) throw new ArgumentException("순회 중심에는 실제 순번이 필요합니다.");
        }
    }

    /// <summary>실제 착지 정산 이후의 원본 완료 기록이다. 그룹 요약만으로 후속 경고를 꾸밀 수 없게 한다.</summary>
    public sealed class CombatTornadoMemberEvent : CombatEvent
    {
        public StageGimmickId GimmickId { get; }
        public TornadoMemberAdvance Advance { get; }
        public CombatTornadoMemberEvent(StageGimmickId gimmickId, TornadoMemberAdvance advance) : base(CombatEventKind.TornadoMemberAdvanced)
        {
            if (gimmickId.IsEmpty) throw new ArgumentException("원본 완료의 그룹 식별자가 필요합니다.");
            GimmickId = gimmickId;
            Advance = advance ?? throw new ArgumentNullException(nameof(advance));
        }
    }

    /// <summary>독립적인 실제 진입 체인의 시작이다. 중첩 착지는 이 체인의 중복 방지 집합을 공유한다.</summary>
    public sealed class CombatTornadoEntryEvent : CombatEvent
    {
        public StageGimmickId GimmickId { get; }
        public EntityId EntrantId { get; }
        public CellCoord Cell { get; }
        public CombatTornadoEntryEvent(StageGimmickId gimmickId, EntityId entrantId, CellCoord cell) : base(CombatEventKind.TornadoEntryStarted)
        {
            if (gimmickId.IsEmpty || entrantId.IsEmpty || cell.X < 0 || cell.Y < 0) throw new ArgumentException("실제 진입 식별자·칸이 필요합니다.");
            GimmickId = gimmickId;
            EntrantId = entrantId;
            Cell = cell;
        }
    }
}
