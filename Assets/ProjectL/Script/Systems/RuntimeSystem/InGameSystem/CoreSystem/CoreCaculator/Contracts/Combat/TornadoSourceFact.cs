using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>위치와 다음 유닛 턴 발동 시점을 한 원본에 보관하는 불변 지형 사실이다.</summary>
    public sealed class TornadoSourceFact : MovementTileFact
    {
        public const string Key = "terrain-tornado";
        public const int Interval = 3;
        public long NextDueCompletedTurn { get; }
        public override bool TriggersArrivalRule => false;
        public TornadoSourceFact(long id, CellCoord cell, IReadOnlyList<Owner> knownTo,
            long nextDueCompletedTurn = Interval, bool active = true) : base(id, cell, Key, 0, active, knownTo)
        {
            if (nextDueCompletedTurn < 1) throw new ArgumentOutOfRangeException(nameof(nextDueCompletedTurn));
            NextDueCompletedTurn = nextDueCompletedTurn;
        }
        public TornadoSourceFact AfterActivation(CellCoord cell) =>
            new(Id, cell, KnownTo, checked(NextDueCompletedTurn + Interval), Active);
        public override bool SameValues(MovementTileFact other) => SameSourceValues(other) &&
            other is TornadoSourceFact tornado && NextDueCompletedTurn == tornado.NextDueCompletedTurn;
        public override MovementTileFact WithKnownTo(IReadOnlyList<Owner> knownTo) =>
            new TornadoSourceFact(Id, Cell, knownTo, NextDueCompletedTurn, Active);
    }
}
