using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>폭발에 맞은 대상의 정산 완료 체력과 재구성 발생 여부다.</summary>
    public readonly struct ThornBombHealthPreview
    {
        public int HealthAfter { get; }
        public bool Reconstructed { get; }

        internal ThornBombHealthPreview(int healthAfter, bool reconstructed)
        {
            HealthAfter = healthAfter;
            Reconstructed = reconstructed;
        }
    }

    /// <summary>연쇄 순서대로 나열할 한 폭발의 중심과 대상별 결과다.</summary>
    public sealed class ThornBombExplosionPreview
    {
        private readonly IReadOnlyDictionary<EntityId, IReadOnlyList<CellCoord>> _knockbackOccupied;

        public ThornBombSnapshot Bomb { get; }
        public CellCoord Center { get; }
        public ThornBombTrigger Trigger { get; }
        public IReadOnlyList<(EntityId TargetId, int Damage)> Hits { get; }
        public IReadOnlyList<AttackKnockbackSnapshot> Knockbacks { get; }

        /// <summary>이 폭발이 맞히는 판 안의 칸이다(폭심지 포함). 범위 규칙의 원본은 Core 규칙이고, View는 이 목록을 그대로 그린다.</summary>
        public IReadOnlyList<CellCoord> BlastCells { get; }

        /// <summary>넉백 기록 대상이 밀리기 직전에 점유한 칸이다. 여러 칸 유닛의 도착 칸 표시가 쓴다.</summary>
        public IReadOnlyList<CellCoord> KnockbackOccupiedCellsOf(EntityId targetId) =>
            _knockbackOccupied.TryGetValue(targetId, out var cells) ? cells
                : throw new KeyNotFoundException($"이 폭발의 넉백 기록 대상이 아닙니다: {targetId}");

        public ThornBombExplosionPreview(ThornBombSnapshot bomb, CellCoord center, ThornBombTrigger trigger,
            IReadOnlyList<(EntityId TargetId, int Damage)> hits, IReadOnlyList<AttackKnockbackSnapshot> knockbacks,
            IReadOnlyList<CellCoord> blastCells, IReadOnlyDictionary<EntityId, IReadOnlyList<CellCoord>> knockbackOccupiedCells)
        {
            if (!Enum.IsDefined(typeof(ThornBombTrigger), trigger)) throw new ArgumentOutOfRangeException(nameof(trigger));
            Bomb = bomb ?? throw new ArgumentNullException(nameof(bomb));
            var hitCopy = (hits ?? throw new ArgumentNullException(nameof(hits))).ToArray();
            var knockbackCopy = (knockbacks ?? throw new ArgumentNullException(nameof(knockbacks))).ToArray();
            var blastCopy = (blastCells ?? throw new ArgumentNullException(nameof(blastCells))).ToArray();
            if (knockbackOccupiedCells == null) throw new ArgumentNullException(nameof(knockbackOccupiedCells));
            if (!blastCopy.Contains(center) || blastCopy.Distinct().Count() != blastCopy.Length)
                throw new ArgumentException("폭발 칸은 중복 없이 폭심지를 포함해야 합니다.", nameof(blastCells));
            var targets = new HashSet<EntityId>();
            foreach (var hit in hitCopy)
                if (hit.TargetId.IsEmpty || hit.Damage < 0 || !targets.Add(hit.TargetId))
                    throw new ArgumentException("폭발 대상은 중복될 수 없고 피해는 0 이상이어야 합니다.", nameof(hits));
            var occupied = new Dictionary<EntityId, IReadOnlyList<CellCoord>>(knockbackCopy.Length);
            foreach (var knockback in knockbackCopy)
            {
                if (knockback == null || !targets.Remove(knockback.TargetId))
                    throw new ArgumentException("넉백은 동일 폭발에 맞은 대상별로 한 번만 기록합니다.", nameof(knockbacks));
                if (!knockbackOccupiedCells.TryGetValue(knockback.TargetId, out var cells) || cells == null || cells.Count == 0)
                    throw new ArgumentException("넉백 대상마다 시작 점유 칸이 있어야 합니다.", nameof(knockbackOccupiedCells));
                occupied.Add(knockback.TargetId, Array.AsReadOnly(cells.ToArray()));
            }
            if (knockbackOccupiedCells.Count != occupied.Count)
                throw new ArgumentException("점유 칸은 넉백 기록 대상에만 둡니다.", nameof(knockbackOccupiedCells));

            Center = center;
            Trigger = trigger;
            Hits = Array.AsReadOnly(hitCopy);
            Knockbacks = Array.AsReadOnly(knockbackCopy);
            BlastCells = Array.AsReadOnly(blastCopy);
            _knockbackOccupied = occupied;
        }
    }
}
