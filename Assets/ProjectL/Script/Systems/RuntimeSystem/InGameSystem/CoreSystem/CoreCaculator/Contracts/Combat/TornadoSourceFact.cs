using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>위치·반복 간격·다음 진영 턴 완료 발동 시점을 한 원본에 보관하는 불변 지형 사실이다.</summary>
    public sealed class TornadoSourceFact : MovementTileFact
    {
        public const string Key = "terrain-tornado";
        /// <summary>반복 간격을 생략한 기존 호출의 기본값이다.</summary>
        public const int Interval = 3;
        /// <summary>진영 턴 완료 기준 반복 간격이다. 기존 공개 식별자는 호환성을 위해 유지한다.</summary>
        public int RepeatIntervalUnitTurns { get; }
        public long NextDueCompletedTurn { get; }
        // 일반 타일 규칙 대신 TornadoCalculator가 진입·주기 발동과 환경 난수를 함께 처리한다.
        public override bool TriggersArrivalRule => false;
        public TornadoSourceFact(long id, CellCoord cell, IReadOnlyList<Owner> knownTo,
            long nextDueCompletedTurn = Interval, bool active = true, int repeatIntervalUnitTurns = Interval)
            : base(id, cell, Key, 0, active, knownTo)
        {
            if (nextDueCompletedTurn < 1) throw new ArgumentOutOfRangeException(nameof(nextDueCompletedTurn));
            if (repeatIntervalUnitTurns < 1) throw new ArgumentOutOfRangeException(nameof(repeatIntervalUnitTurns));
            NextDueCompletedTurn = nextDueCompletedTurn;
            RepeatIntervalUnitTurns = repeatIntervalUnitTurns;
        }
        public TornadoSourceFact AfterActivation(CellCoord cell) =>
            new(Id, cell, KnownTo, checked(NextDueCompletedTurn + RepeatIntervalUnitTurns), Active, RepeatIntervalUnitTurns);
        public override bool SameValues(MovementTileFact other) => SameSourceValues(other) &&
            other is TornadoSourceFact tornado && NextDueCompletedTurn == tornado.NextDueCompletedTurn &&
            RepeatIntervalUnitTurns == tornado.RepeatIntervalUnitTurns;
        public override MovementTileFact WithKnownTo(IReadOnlyList<Owner> knownTo) =>
            new TornadoSourceFact(Id, Cell, knownTo, NextDueCompletedTurn, Active, RepeatIntervalUnitTurns);

        /// <summary>준비 때 확정한 반복 간격은 전투 중 바뀌지 않는다. 다음 발동 시점·위치는 Core 계산이 바꾼다.</summary>
        public override string SuccessorFailure(MovementTileFact final) =>
            final is TornadoSourceFact next && RepeatIntervalUnitTurns != next.RepeatIntervalUnitTurns
                ? "전투 중 토네이도 반복 간격을 변경할 수 없습니다."
                : base.SuccessorFailure(final);
    }
}
