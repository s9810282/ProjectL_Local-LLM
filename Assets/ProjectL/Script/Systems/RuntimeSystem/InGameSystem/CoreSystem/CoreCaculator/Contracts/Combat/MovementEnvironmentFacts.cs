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
            var ids = new HashSet<long>();
            foreach (var tile in copy) ValidateTile(tile, ids);
            Tiles = Array.AsReadOnly(copy);
        }

        // 가시성 계산이 완전히 검증한 전용 목록만 받는다. 호출자는 목록을 다시 변경하지 않는다.
        private MovementEnvironmentFacts(List<MovementTileFact> tiles) => Tiles = tiles.AsReadOnly();

        private static void ValidateTile(MovementTileFact tile, HashSet<long> ids)
        {
            if (tile == null || !ids.Add(tile.Id))
                throw new ArgumentException("타일 원본은 null 또는 중복 ID일 수 없습니다.", "tiles");
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
            MovementEnvironmentFacts result = null;
            foreach (var _ in VisibleSteps(observer, value => result = value)) { }
            return result;
        }

        internal IEnumerable<string> VisibleSteps(Owner observer, Action<MovementEnvironmentFacts> completed, bool preservePublic = false)
        {
            if (observer != Owner.Player && observer != Owner.Enemy)
                throw new ArgumentOutOfRangeException(nameof(observer));
            var visible = new List<MovementTileFact>();
            foreach (var tile in Tiles)
            {
                if (tile.KnownTo.Contains(observer))
                    visible.Add(preservePublic && tile.KnownTo.Contains(Owner.Player) && tile.KnownTo.Contains(Owner.Enemy)
                        ? tile : tile.WithKnownTo(new[] { observer }));
                yield return "EnvironmentVisibleTile";
            }
            var ids = new HashSet<long>();
            foreach (var tile in visible)
            {
                ValidateTile(tile, ids);
                yield return "EnvironmentVisibleValidation";
            }
            completed(new MovementEnvironmentFacts(visible));
            yield return "EnvironmentVisibleComplete";
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

        /// <summary>기존 원본의 형식이 바뀌었거나 Object가 허용 밖으로 바뀌었을 때 쓰는 문구다.</summary>
        protected const string IdentityChangeFailure = "기존 환경 원본의 기능·식별 내용 변경 또는 재활성화는 허용하지 않습니다.";

        /// <summary>
        /// Buffer 저장 검사 전용: 이 원본이 같은 ID의 <paramref name="final"/>로 바뀌어도 되면 null, 아니면 거절 문구다.
        /// 기본은 형식만 본다. 종류마다 바뀌면 안 되는 값이 있으면 그 종류가 override한다. Core 계산은 이것을 부르지 않는다.
        /// </summary>
        public virtual string SuccessorFailure(MovementTileFact final) =>
            GetType() != final.GetType() ? IdentityChangeFailure : null;
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
