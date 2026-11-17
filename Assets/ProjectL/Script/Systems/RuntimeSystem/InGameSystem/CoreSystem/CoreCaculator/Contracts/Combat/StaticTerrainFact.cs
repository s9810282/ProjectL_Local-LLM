using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public enum StaticTerrainKind
    {
        HighGround = 1,
        Sandstorm = 2,
        Wall = 3,
        WeakGround = 4,
        Cliff = 5,
        FilledCliff = 6
    }

    /// <summary>지형의 종류와 약한 지반 한 칸의 누적 진입 상태를 보관하는 불변 사실이다.</summary>
    public sealed class StaticTerrainFact : MovementTileFact
    {
        public const string Key = "static-terrain";
        public StaticTerrainKind Kind { get; }
        public int EntryThreshold { get; }
        public long EntryCount { get; }
        public int RemainingEntries => Kind != StaticTerrainKind.WeakGround || EntryCount >= EntryThreshold
            ? 0 : EntryThreshold - (int)EntryCount;
        public bool PendingCollapse => Active && Kind == StaticTerrainKind.WeakGround && EntryCount >= EntryThreshold;
        public override bool TriggersArrivalRule => false;

        public StaticTerrainFact(long id, CellCoord cell, StaticTerrainKind kind, bool active,
            IReadOnlyList<Owner> knownTo, int entryThreshold = 0, long entryCount = 0)
            : base(id, cell, Key, 0, active, knownTo)
        {
            if (!Enum.IsDefined(typeof(StaticTerrainKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (entryCount < 0)
                throw new ArgumentOutOfRangeException(nameof(entryCount));
            if (kind == StaticTerrainKind.WeakGround)
            {
                if (entryThreshold < 1)
                    throw new ArgumentOutOfRangeException(nameof(entryThreshold));
            }
            else if (entryThreshold != 0 || entryCount != 0)
            {
                throw new ArgumentException("약한 지반 이외의 지형에는 진입 한도와 누적 횟수를 지정할 수 없습니다.");
            }
            Kind = kind;
            EntryThreshold = entryThreshold;
            EntryCount = entryCount;
        }

        /// <summary>실제 캐릭터 진입 한 번을 누적하며 붕괴 처리는 행동 종료 경계에 맡긴다.</summary>
        public StaticTerrainFact RegisterEntry()
        {
            if (!Active || Kind != StaticTerrainKind.WeakGround)
                throw new InvalidOperationException("활성 약한 지반에만 진입 횟수를 누적할 수 있습니다.");
            return new StaticTerrainFact(Id, Cell, Kind, Active, KnownTo, EntryThreshold, checked(EntryCount + 1));
        }

        /// <summary>붕괴 예정 지반을 같은 식별자와 공개 범위를 가진 낭떠러지로 바꾼다.</summary>
        public StaticTerrainFact Collapse()
        {
            if (!PendingCollapse)
                throw new InvalidOperationException("붕괴 예정인 약한 지반만 낭떠러지로 바꿀 수 있습니다.");
            return new StaticTerrainFact(Id, Cell, StaticTerrainKind.Cliff, Active, KnownTo);
        }

        /// <summary>
        /// 이 약한 지반이 <paramref name="final"/>로 바뀌어도 되는지다.
        /// <paramref name="singleEntry"/>면 진입 한 번(+1)만, 아니면 같은 구성에서 누적이 늘어난 것을 허용한다.
        /// 붕괴 예정이면 낭떠러지로의 붕괴도 허용한다.
        /// Buffer의 지형 저장 검사가 부른다(옛 CombatStateBuffer.IsGroundSuccessor를 그대로 옮긴 것).
        /// </summary>
        public bool IsGroundSuccessor(StaticTerrainFact final, bool singleEntry)
        {
            if (!Active || Kind != StaticTerrainKind.WeakGround) return false;
            if (final.Kind == StaticTerrainKind.Cliff)
                return PendingCollapse && Collapse().SameValues(final);
            if (final.Kind != StaticTerrainKind.WeakGround || Id != final.Id || Cell != final.Cell ||
                Active != final.Active || !KnownTo.SequenceEqual(final.KnownTo) ||
                EntryThreshold != final.EntryThreshold || final.EntryCount <= EntryCount)
                return false;
            return !singleEntry || EntryCount < long.MaxValue && final.EntryCount == EntryCount + 1;
        }

        /// <summary>기둥에 메워진 낭떠러지는 같은 원본 ID와 공개 범위를 유지한다.</summary>
        public StaticTerrainFact FillCliff()
        {
            if (!Active || Kind != StaticTerrainKind.Cliff)
                throw new InvalidOperationException("활성 낭떠러지만 기둥으로 메울 수 있습니다.");
            return new StaticTerrainFact(Id, Cell, StaticTerrainKind.FilledCliff, Active, KnownTo);
        }

        public bool IsPillarFillSuccessor(StaticTerrainFact final) =>
            Active && Kind == StaticTerrainKind.Cliff && final != null && FillCliff().SameValues(final);

        public override bool SameValues(MovementTileFact other) =>
            SameSourceValues(other) && other is StaticTerrainFact terrain && Kind == terrain.Kind &&
            EntryThreshold == terrain.EntryThreshold && EntryCount == terrain.EntryCount;

        public override MovementTileFact WithKnownTo(IReadOnlyList<Owner> knownTo) =>
            new StaticTerrainFact(Id, Cell, Kind, Active, knownTo, EntryThreshold, EntryCount);
    }
}
