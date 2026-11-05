using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>
    /// 아직 칸이 정해지지 않은 최초 토네이도 배치 의도다. 칸은 전투 준비가 한 번 정하며,
    /// 이 명세는 런타임 사실이 아니므로 Buffer에 게시하지 않는다.
    /// </summary>
    public sealed class InitialRandomTornadoPlacementSpec
    {
        public long SourceId { get; }
        public IReadOnlyList<Owner> KnownTo { get; }
        public long FirstActivationAfterUnitTurns { get; }
        public int RepeatIntervalUnitTurns { get; }

        public InitialRandomTornadoPlacementSpec(long sourceId, IReadOnlyList<Owner> knownTo,
            long firstActivationAfterUnitTurns = TornadoSourceFact.Interval,
            int repeatIntervalUnitTurns = TornadoSourceFact.Interval)
        {
            // 값 검증은 런타임 사실의 생성자 한 곳을 따른다. 검증용 사실은 보관하지 않는다.
            var validated = new TornadoSourceFact(sourceId, CellCoord.Zero,
                knownTo ?? throw new ArgumentNullException(nameof(knownTo)),
                firstActivationAfterUnitTurns, true, repeatIntervalUnitTurns);
            SourceId = validated.Id;
            KnownTo = validated.KnownTo;
            FirstActivationAfterUnitTurns = validated.NextDueCompletedTurn;
            RepeatIntervalUnitTurns = validated.RepeatIntervalUnitTurns;
        }

        /// <summary>정해진 칸의 활성 원본을 만든다. 최초 생성이므로 첫 발동 시점을 그대로 둔다.</summary>
        public TornadoSourceFact CreateSource(CellCoord cell) =>
            new(SourceId, cell, KnownTo, FirstActivationAfterUnitTurns, true, RepeatIntervalUnitTurns);
    }
}
