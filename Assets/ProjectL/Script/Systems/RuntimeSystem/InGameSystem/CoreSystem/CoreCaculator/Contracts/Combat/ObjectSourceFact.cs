using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>타일 위 Object 원본이다. 게임 처리 순서는 식별자·규칙 우선순위와 분리한다.</summary>
    public abstract class ObjectSourceFact : MovementTileFact
    {
        public long EntryOrdinal { get; }
        protected ObjectSourceFact(long id, CellCoord cell, string key, long entryOrdinal, bool active,
            IReadOnlyList<Owner> knownTo, int priority = 0, bool isMine = false)
            : base(id, cell, key, priority, active, knownTo, isMine)
        {
            if (entryOrdinal < 1) throw new ArgumentOutOfRangeException(nameof(entryOrdinal));
            EntryOrdinal = entryOrdinal;
        }
        protected bool SameObjectValues(ObjectSourceFact other) => SameSourceValues(other) && EntryOrdinal == other.EntryOrdinal;
        public abstract ObjectSourceFact WithActive(bool active);
    }

    /// <summary>중심을 포함한 3×3에 동일한 원시 피해를 주는 불변 지뢰 원본이다.</summary>
    public sealed class MineSourceFact : ObjectSourceFact
    {
        public const string Key = "object-mine";
        public int Damage { get; }
        public MineSourceFact(long id, CellCoord cell, long entryOrdinal, int damage, bool active,
            IReadOnlyList<Owner> knownTo, int priority = 0)
            : base(id, cell, Key, entryOrdinal, active, knownTo, priority, isMine: true)
        {
            if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
            Damage = damage;
        }
        public override bool SameValues(MovementTileFact other) =>
            other is MineSourceFact mine && SameObjectValues(mine) && Damage == mine.Damage;
        public override ObjectSourceFact WithActive(bool active) =>
            new MineSourceFact(Id, Cell, EntryOrdinal, Damage, active, KnownTo, Priority);
        public override MovementTileFact WithKnownTo(IReadOnlyList<Owner> knownTo) =>
            new MineSourceFact(Id, Cell, EntryOrdinal, Damage, Active, knownTo, Priority);
    }

    public sealed class FlagSourceFact : ObjectSourceFact
    {
        public const string Key = "object-flag";
        public int PriorityBonus { get; }
        public FlagSourceFact(long id, CellCoord cell, long entryOrdinal, int priorityBonus, bool active,
            IReadOnlyList<Owner> knownTo, int priority = 0) : base(id, cell, Key, entryOrdinal, active, knownTo, priority)
        {
            if (priorityBonus < 0) throw new ArgumentOutOfRangeException(nameof(priorityBonus));
            PriorityBonus = priorityBonus;
        }
        public override bool SameValues(MovementTileFact other) => other is FlagSourceFact flag &&
            SameObjectValues(flag) && PriorityBonus == flag.PriorityBonus;
        public override ObjectSourceFact WithActive(bool active) =>
            new FlagSourceFact(Id, Cell, EntryOrdinal, PriorityBonus, active, KnownTo, Priority);
        public override MovementTileFact WithKnownTo(IReadOnlyList<Owner> knownTo) =>
            new FlagSourceFact(Id, Cell, EntryOrdinal, PriorityBonus, Active, knownTo, Priority);
    }

    public sealed class SheriffBadgeSourceFact : ObjectSourceFact
    {
        public const string Key = "object-sheriff-badge";
        public SheriffBadgeSourceFact(long id, CellCoord cell, long entryOrdinal, bool active,
            IReadOnlyList<Owner> knownTo, int priority = 0) : base(id, cell, Key, entryOrdinal, active, knownTo, priority) { }
        public override bool SameValues(MovementTileFact other) => other is SheriffBadgeSourceFact badge && SameObjectValues(badge);
        public override ObjectSourceFact WithActive(bool active) => new SheriffBadgeSourceFact(Id, Cell, EntryOrdinal, active, KnownTo, Priority);
        public override MovementTileFact WithKnownTo(IReadOnlyList<Owner> knownTo) => new SheriffBadgeSourceFact(Id, Cell, EntryOrdinal, Active, knownTo, Priority);
    }
}
