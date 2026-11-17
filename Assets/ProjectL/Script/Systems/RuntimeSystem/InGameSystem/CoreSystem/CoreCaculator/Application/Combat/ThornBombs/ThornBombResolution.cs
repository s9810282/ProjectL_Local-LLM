using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Tiles;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.ThornBombs
{
    /// <summary>명령별 활성 폭탄과 예약을 소유한다. 도착 투영은 같은 인스턴스를 쓰고 게시할 때만 복사한다.</summary>
    internal sealed class ThornBombResolution
    {
        private readonly Dictionary<EntityId, ThornBombSnapshot> _bombs;
        private readonly Queue<(ThornBombSnapshot Bomb, ThornBombTrigger Trigger)> _queue = new();
        private readonly HashSet<long> _scheduled = new();
        private readonly Dictionary<long, CellCoord> _deathCells = new();
        private readonly List<ThornBombExplosionPreview> _explosions = new();
        private readonly HashSet<(long Group, EntityId OriginalTarget)> _attackHits = new();
        private readonly HashSet<EntityId> _killed = new();
        private long _nextBombId;
        private bool _resolving;

        internal IReadOnlyList<ThornBombExplosionPreview> Explosions => _explosions;

        /// <summary>
        /// 이 명령에서 폭발이 진행되는 동안(피해·넉백·도착 효과) 죽은 캐릭터다.
        /// 처치 연쇄는 이 사망을 원래 공격의 처치로 세지 않는다(Phase 2 결정).
        /// </summary>
        internal IReadOnlyCollection<EntityId> KilledIds => _killed;

        internal ThornBombResolution(ThornBombStateSet initial)
        {
            if (initial == null) throw new ArgumentNullException(nameof(initial));
            _bombs = initial.Bombs.ToDictionary(bomb => bomb.TargetId);
            _nextBombId = initial.NextBombId;
        }

        internal long NextBombId()
        {
            long id = _nextBombId;
            _nextBombId = checked(id + 1);
            return id;
        }

        internal void Attach(ThornBombSnapshot bomb)
        {
            if (bomb == null) throw new ArgumentNullException(nameof(bomb));
            if (bomb.BombId != _nextBombId - 1 || _bombs.ContainsKey(bomb.TargetId))
                throw new InvalidOperationException("방금 발급한 폭탄만 빈 대상에 부착할 수 있습니다.");
            _bombs.Add(bomb.TargetId, bomb);
        }

        internal bool TrySchedule(EntityId targetId, ThornBombTrigger reason)
        {
            if (!_bombs.TryGetValue(targetId, out var bomb) || !_scheduled.Add(bomb.BombId)) return false;
            _queue.Enqueue((bomb, reason));
            return true;
        }

        /// <summary>실제로 명중한 공격만 호출한다. 희생 수신자의 기폭과 원래 중심의 부착 자격을 분리한다.</summary>
        internal void RecordAttackHit(long attackGroup, EntityId originalTarget, EntityId recipient)
        {
            _attackHits.Add((attackGroup, originalTarget));
            TrySchedule(recipient, ThornBombTrigger.AttackHit);
        }

        internal bool WasHit(long attackGroup, EntityId originalTarget) => _attackHits.Contains((attackGroup, originalTarget));

        internal void ScheduleDue(Owner owner, long controllerTurnSequence)
        {
            foreach (var bomb in _bombs.Values.Where(b => b.SourceOwner == owner && b.DueControllerTurnSequence <= controllerTurnSequence)
                .OrderBy(b => b.TargetId).ThenBy(b => b.BombId))
                TrySchedule(bomb.TargetId, ThornBombTrigger.DueControllerTurn);
        }

        /// <summary>
        /// 죽은 부착 대상의 폭탄을 사망 칸 기폭으로 예약한다. 사망 칸은 실제 체력 사건의 사망 직전 배치이며 시체 연출용
        /// DeathAnchors는 쓰지 않는다. 낙사한 대상의 폭탄은 터지지 않고 사라진다(설계 R15). 앞서 명중으로 예약됐어도 같다.
        /// </summary>
        internal void ObserveDeaths(MovementProjection projection)
        {
            // 모든 사망 정산 경계가 부르므로 폭탄이 없으면 정렬·할당 없이 끝낸다.
            if (_bombs.Count == 0) return;
            HashSet<EntityId> fallen = null;
            foreach (var bomb in _bombs.Values.OrderBy(b => b.TargetId).ThenBy(b => b.BombId).ToArray())
            {
                if (!projection.Character(bomb.TargetId).Health.IsDead) continue;
                // 낙사 판정은 사망 특성의 낙사 제외(DeathTraitApplication)와 같이 같은 투영의 낙사 사건으로 한다.
                fallen ??= projection.Events.OfType<CombatFallEvent>().Select(e => e.EntityId).ToHashSet();
                if (fallen.Contains(bomb.TargetId))
                {
                    // 예약 큐에 남은 항목은 활성 폭탄이 없어 꺼낼 때 건너뛴다.
                    _bombs.Remove(bomb.TargetId);
                    _deathCells.Remove(bomb.BombId);
                    projection.Events.Add(new CombatThornBombDiscardedEvent(bomb));
                    continue;
                }
                if (!_deathCells.ContainsKey(bomb.BombId))
                {
                    var death = PreDeathFact(projection, bomb.TargetId) ??
                        throw new InvalidOperationException("부착 대상의 실제 사망 위치 증거가 없습니다.");
                    _deathCells.Add(bomb.BombId, ResolveCell(projection.Input, death, bomb.AttachmentLocalCell));
                }
                TrySchedule(bomb.TargetId, ThornBombTrigger.TargetDeath);
            }
        }

        /// <summary>
        /// 이번 명령의 체력 사건 중 그 캐릭터가 죽은 마지막 사건의 직전 사실이다. 없으면 null이다.
        /// 사망 정산(<c>Died</c> 사건)보다 앞선 폭발 피해 직후에도 읽어야 해서
        /// <see cref="CommandDeathSettlement.PreDeathFact"/> 대신 체력 사건을 본다. 시체 연출용 DeathAnchors는 쓰지 않는다.
        /// </summary>
        internal static CharacterCombatFacts PreDeathFact(MovementProjection projection, EntityId id) =>
            projection.Events.OfType<CombatCharacterEvent>().LastOrDefault(e =>
                e.Final.EntityId == id && !e.Expected.Health.IsDead && e.Final.Health.IsDead)?.Expected;

        internal CellCoord Center(MovementProjection projection, ThornBombSnapshot bomb) =>
            _deathCells.TryGetValue(bomb.BombId, out var cell) ? cell :
                ResolveCell(projection.Input, projection.Character(bomb.TargetId), bomb.AttachmentLocalCell);

        internal IEnumerable<string> ResolveSteps(MovementProjection projection, IReadOnlyList<IMovementTileRule> rules)
        {
            if (_resolving) yield break;
            _resolving = true;
            try
            {
                ObserveDeaths(projection);
                List<EntityId> living = null;
                while (_queue.Count > 0 && !projection.Terminal)
                {
                    if (rules == null) throw new InvalidOperationException("폭탄 연쇄의 강제 이동에는 명령의 지형 규칙 목록이 필요합니다.");
                    var next = _queue.Dequeue();
                    if (!_bombs.TryGetValue(next.Bomb.TargetId, out var active) || active.BombId != next.Bomb.BombId) continue;
                    // 실제로 폭발할 때만 생존자를 한 번 기록한다. 폭발 없는 공격은 할당하지 않는다.
                    living ??= projection.Damage.Characters.Values.Where(c => !c.Health.IsDead).Select(c => c.EntityId).ToList();
                    _bombs.Remove(active.TargetId);
                    foreach (var phase in ThornBombExplosion.ExecuteSteps(projection, active, next.Trigger, this, rules))
                        yield return phase;
                    _scheduled.Remove(active.BombId);
                    _deathCells.Remove(active.BombId);
                    ObserveDeaths(projection);
                }
                if (living != null)
                    foreach (var id in living)
                        if (projection.Character(id).Health.IsDead) _killed.Add(id);
                if (projection.Terminal)
                {
                    _queue.Clear();
                    _scheduled.Clear();
                    _deathCells.Clear();
                    _bombs.Clear();
                }
            }
            finally { _resolving = false; }
        }

        internal void Record(ThornBombExplosionPreview explosion) => _explosions.Add(explosion);
        internal ThornBombStateSet Snapshot() => new(_nextBombId, _bombs.Values.ToArray());

        internal static CellCoord CaptureLocalCell(CombatCommandInput input, CharacterCombatFacts target, CellCoord cell)
        {
            var footprint = Footprint(input, target);
            for (int i = 0; i < footprint.CellCount; i++)
                if (footprint.GetOccupiedCell(i, target.Anchor.Value, target.Rotation.Value) == cell)
                    return footprint.GetLocalCell(i);
            throw new InvalidOperationException("공격 중심이 부착 대상의 점유 칸에 없습니다.");
        }

        private static CellCoord ResolveCell(CombatCommandInput input, CharacterCombatFacts target, CellCoord localCell)
        {
            var footprint = Footprint(input, target);
            for (int i = 0; i < footprint.CellCount; i++)
                if (footprint.GetLocalCell(i) == localCell)
                    return footprint.GetOccupiedCell(i, target.Anchor.Value, target.Rotation.Value);
            throw new InvalidOperationException("부착한 로컬 칸이 대상의 모양에 없습니다.");
        }

        private static TileFootprint Footprint(CombatCommandInput input, CharacterCombatFacts target)
        {
            var spec = CombatCommandCalculator.RequiredProfile(input, target).Definition;
            return new TileFootprint(spec.FootprintWidth, spec.FootprintHeight, spec.FootprintLocalCells.ToArray());
        }
    }
}
