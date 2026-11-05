using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks
{
    public sealed class TornadoMemberSpec
    {
        public long SourceId { get; }
        public IReadOnlyList<Owner> KnownTo { get; }
        public CellCoord? InitialCenter { get; }
        public int? InitialHorizontalSign { get; }
        public int SideLength { get; }
        public int DiagonalDistance { get; }
        public long FirstActivationAfterControllerTurns { get; }
        public int RepeatIntervalControllerTurns { get; }
        public int WarningLeadControllerTurns { get; }

        public TornadoMemberSpec(long sourceId, IReadOnlyList<Owner> knownTo, CellCoord? initialCenter = null,
            int? initialHorizontalSign = null, int sideLength = 3, int diagonalDistance = 3,
            long firstActivationAfterControllerTurns = 2, int repeatIntervalControllerTurns = 2,
            int warningLeadControllerTurns = 1)
        {
            if (sourceId < 1) throw new ArgumentOutOfRangeException(nameof(sourceId));
            var visibility = (knownTo ?? throw new ArgumentNullException(nameof(knownTo))).ToArray();
            if (visibility.Any(o => o != Owner.Player && o != Owner.Enemy) || visibility.Distinct().Count() != visibility.Length)
                throw new ArgumentException("토네이도 가시성은 중복 없는 진영 목록이어야 합니다.", nameof(knownTo));
            if (initialHorizontalSign.HasValue && initialHorizontalSign != -1 && initialHorizontalSign != 1)
                throw new ArgumentOutOfRangeException(nameof(initialHorizontalSign));
            if (sideLength < 1 || sideLength % 2 == 0) throw new ArgumentOutOfRangeException(nameof(sideLength));
            if (diagonalDistance < 1) throw new ArgumentOutOfRangeException(nameof(diagonalDistance));
            if (firstActivationAfterControllerTurns < 1) throw new ArgumentOutOfRangeException(nameof(firstActivationAfterControllerTurns));
            if (repeatIntervalControllerTurns < 1) throw new ArgumentOutOfRangeException(nameof(repeatIntervalControllerTurns));
            if (warningLeadControllerTurns < 1 || warningLeadControllerTurns > Math.Min(firstActivationAfterControllerTurns, repeatIntervalControllerTurns))
                throw new ArgumentOutOfRangeException(nameof(warningLeadControllerTurns));
            SourceId = sourceId;
            KnownTo = Array.AsReadOnly(visibility.OrderBy(o => o).ToArray());
            InitialCenter = initialCenter;
            InitialHorizontalSign = initialHorizontalSign;
            SideLength = sideLength;
            DiagonalDistance = diagonalDistance;
            FirstActivationAfterControllerTurns = firstActivationAfterControllerTurns;
            RepeatIntervalControllerTurns = repeatIntervalControllerTurns;
            WarningLeadControllerTurns = warningLeadControllerTurns;
        }
    }

    public sealed class TornadoGimmickSpec : StageGimmickSpec
    {
        public IReadOnlyList<TornadoMemberSpec> Members { get; }

        public TornadoGimmickSpec(IReadOnlyList<TornadoMemberSpec> members) : base(StageGimmickKey.Tornado)
        {
            var copy = (members ?? throw new ArgumentNullException(nameof(members))).ToArray();
            if (copy.Length == 0 || copy.Any(m => m == null) || copy.Select(m => m.SourceId).Distinct().Count() != copy.Length)
                throw new ArgumentException("토네이도 그룹은 중복 없는 원본이 하나 이상 필요합니다.", nameof(members));
            Members = Array.AsReadOnly(copy.OrderBy(m => m.SourceId).ToArray());
        }
    }
}
