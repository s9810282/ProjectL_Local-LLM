using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>기존 Combat Buffer가 보관하는 런타임 타일 원본이다.</summary>
    public sealed class MovementEnvironmentFacts
    {
        public static MovementEnvironmentFacts Empty { get; } =
            new MovementEnvironmentFacts(Array.Empty<MovementTileFact>());
        public IReadOnlyList<MovementTileFact> Tiles { get; }

        public MovementEnvironmentFacts(IReadOnlyList<MovementTileFact> tiles)
        {
            if (tiles == null) throw new ArgumentNullException(nameof(tiles));
            var copy = tiles.ToArray();
            if (copy.Any(tile => tile == null) || copy.Select(tile => tile.Id).Distinct().Count() != copy.Length)
                throw new ArgumentException("타일 원본은 null 또는 중복 ID일 수 없습니다.", nameof(tiles));
            Tiles = Array.AsReadOnly(copy);
        }

        public bool SameValues(MovementEnvironmentFacts other)
        {
            if (other == null || Tiles.Count != other.Tiles.Count) return false;
            for (int i = 0; i < Tiles.Count; i++)
                if (!Tiles[i].SameValues(other.Tiles[i])) return false;
            return true;
        }

        public MovementEnvironmentFacts VisibleTo(Owner observer)
        {
            if (observer != Owner.Player && observer != Owner.Enemy)
                throw new ArgumentOutOfRangeException(nameof(observer));
            return new MovementEnvironmentFacts(Tiles.Where(tile => tile.KnownTo.Contains(observer))
                .Select(tile => tile.WithKnownTo(new[] { observer })).ToArray());
        }
    }

    /// <summary>구체 규칙의 매개변수는 불변 형식으로 정의하며 임의 객체나 사전을 허용하지 않는다.</summary>
    public abstract class MovementTileFact
    {
        public long Id { get; }
        public CellCoord Cell { get; }
        public string RuleKey { get; }
        public int Priority { get; }
        public bool Active { get; }
        public bool IsMine { get; }
        public IReadOnlyList<Owner> KnownTo { get; }

        protected MovementTileFact(long id, CellCoord cell, string ruleKey, int priority,
            bool active, IReadOnlyList<Owner> knownTo, bool isMine = false)
        {
            if (id < 1) throw new ArgumentOutOfRangeException(nameof(id));
            if (string.IsNullOrWhiteSpace(ruleKey)) throw new ArgumentException("규칙 키가 필요합니다.", nameof(ruleKey));
            if (knownTo == null) throw new ArgumentNullException(nameof(knownTo));
            var visibility = knownTo.ToArray();
            if (visibility.Any(owner => owner != Owner.Player && owner != Owner.Enemy) ||
                visibility.Distinct().Count() != visibility.Length)
                throw new ArgumentException("가시성 소유자 값은 유효하며 중복될 수 없습니다.", nameof(knownTo));
            Id = id;
            Cell = cell;
            RuleKey = ruleKey;
            Priority = priority;
            Active = active;
            IsMine = isMine;
            KnownTo = Array.AsReadOnly(visibility.OrderBy(owner => owner).ToArray());
        }

        protected bool SameSourceValues(MovementTileFact other) => other != null &&
            GetType() == other.GetType() && Id == other.Id && Cell == other.Cell &&
            RuleKey == other.RuleKey && Priority == other.Priority && Active == other.Active && IsMine == other.IsMine &&
            KnownTo.SequenceEqual(other.KnownTo);

        public abstract bool SameValues(MovementTileFact other);
        /// <summary>일반 진입 규칙(위험·회복 단계)을 실행하는 원본인지 나타낸다. 위치·존재로만 작용하는 원본은 false다.</summary>
        public virtual bool TriggersArrivalRule => true;
        /// <summary>공개 사본은 해당 관찰자의 가시성만 남긴다. 다른 원본 값은 그대로 복사한다.</summary>
        public abstract MovementTileFact WithKnownTo(IReadOnlyList<Owner> knownTo);
    }

    /// <summary>최종 정지 셀의 위치 보정 원본이다. 적용된 캐릭터 기여분을 저장하지 않는다.</summary>
    public sealed class DestinationTileModifierFact : MovementTileFact
    {
        public const string Key = "destination-modifier";
        public int AttackBonus { get; }
        public int MaxHealthBonus { get; }
        public override bool TriggersArrivalRule => false;

        public DestinationTileModifierFact(long id, CellCoord cell, bool active,
            IReadOnlyList<Owner> knownTo, int attackBonus, int maxHealthBonus, int priority = 0)
            : base(id, cell, Key, priority, active, knownTo)
        {
            AttackBonus = attackBonus;
            MaxHealthBonus = maxHealthBonus;
        }

        public override bool SameValues(MovementTileFact other) => SameSourceValues(other) &&
            other is DestinationTileModifierFact modifier &&
            AttackBonus == modifier.AttackBonus && MaxHealthBonus == modifier.MaxHealthBonus;

        public override MovementTileFact WithKnownTo(IReadOnlyList<Owner> knownTo) =>
            new DestinationTileModifierFact(Id, Cell, Active, knownTo, AttackBonus, MaxHealthBonus, Priority);
    }
}
