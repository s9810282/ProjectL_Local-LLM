#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev.TraitPreview.Scenario
{
    /// <summary>컴파일 중에만 보유하는 합성 사실입니다. 운영 Buffer에는 연결하지 않습니다.</summary>
    internal sealed class PreviewScenarioLedger
    {
        private readonly Dictionary<string, CharacterCombatFacts> _facts = new();
        private readonly List<string> _order = new();
        private long _attack;
        private readonly Dictionary<string, (string Target, StatusEffectKey Key, EffectOriginKey Origin, MovementEffectId? Movement)> _effectSources = new();
        internal void EffectSource(string id, string target, StatusEffectKey key, EffectOriginKey? origin, bool remove, MovementEffectId? movement = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("효과 Source ID가 필요합니다.");
            if (_effectSources.TryGetValue(id, out var old))
            {
                if (old.Target != target || old.Key != key || (!remove && (old.Origin != origin || old.Movement != movement)))
                    throw new ArgumentException($"Source ID {id}의 대상/키/출처/이동 식별은 제거 전 바꿀 수 없습니다.");
                if (remove) _effectSources.Remove(id);
            }
            else if (remove) throw new ArgumentException($"제거할 Source ID가 없습니다: {id}");
            else _effectSources.Add(id, (target, key, origin.Value, movement));
        }

        internal PreviewScenarioLedger(IEnumerable<PreviewScenarioUnit> units)
        {
            foreach (var input in units)
            {
                var unit = input.unit;
                var cell = Cell(unit.cell.x, unit.cell.y);
                if (unit.health < 1)
                    throw new ArgumentException($"초기 유닛은 살아 있어야 합니다: {input.name}");
                var statuses = new List<TraitStatusSnapshot>();
                if (input.ownsPositionSwap) statuses.Add(new PositionSwapTraitStatusSnapshot());
                if (input.ownsReconSquad) statuses.Add(new ReconSquadTraitStatusSnapshot(input.reconIncreasePercent, input.reconRadius));
                if (input.ownsAcceleration)
                    statuses.Add(new AccelerationTraitStatusSnapshot());
                if (input.ownsCleanSweep)
                    statuses.Add(new CleanSweepTraitStatusSnapshot());
                if (input.ownsRegeneration)
                    statuses.Add(new RegenerationTraitStatusSnapshot(input.healAmount, input.healEveryOwnSideTurnEnds, input.completedEligibleTurnEnds));
                var stats = new UnitStatsSnapshot(MoveMethod.LiftAndPlace, 4, 1, unit.attack, unit.defense, 1, statuses.ToArray(), CategorySnapshot.CreateStateless(CategoryKey.Rider));
                int id = _facts.Count + 1;
                _facts.Add(input.id, new CharacterCombatFacts(new EntityId(id), unit.enemy ? null : new CharacterInstanceId(id), unit.key, unit.enemy ? Owner.Enemy : Owner.Player, unit.enemy ? null : CharacterOwnershipRole.Sub, CharacterRuntimeArea.Field, new HealthStateSnapshot(unit.health, unit.maxHealth), stats, null, cell, unit.direction, new[] { cell }, actionUse: input.actionUse, stageRecord: CharacterStageRecord.Empty.Add(distanceMoved: input.distanceMoved)));
                _order.Add(input.id);
            }

            if (_facts.Values.Select(f => f.Anchor.Value).Distinct().Count() != _facts.Count)
                throw new ArgumentException("초기 유닛의 칸이 겹칩니다.");
        }

        internal CharacterCombatFacts Get(string id) => id != null && _facts.TryGetValue(id, out var value) ? value : throw new ArgumentException($"유닛 참조가 없습니다: {id}");
        internal CharacterCombatFacts Alive(string id)
        {
            var value = Get(id);
            if (value.Health.IsDead)
                throw new ArgumentException($"사망 유닛은 행동/상태 대상이 될 수 없습니다: {id}");
            return value;
        }

        internal void Set(string id, CharacterCombatFacts value)
        {
            if (!Get(id).SameIdentity(value))
                throw new ArgumentException("블록이 유닛 식별자를 바꿨습니다.");
            _facts[id] = value;
        }

        internal CharacterCombatFacts[] Snapshot() => _order.Select(Get).ToArray();
        internal long NextAttack() => ++_attack;
        internal static CellCoord Cell(int x, int y)
        {
            if (x < 0 || y < 0 || x >= 8 || y >= 8)
                throw new ArgumentException($"8×8 필드 밖의 칸: ({x}, {y})");
            return new CellCoord(x, y);
        }
    }
}
#endif
