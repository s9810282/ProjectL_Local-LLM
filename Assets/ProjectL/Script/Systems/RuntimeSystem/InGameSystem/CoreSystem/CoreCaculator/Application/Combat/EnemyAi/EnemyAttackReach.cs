using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Diagnostics;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi
{
    /// <summary>
    /// 탐색 한 번 동안 "그 칸에 서면 대상에 피해를 주는 조준이 있나"를 판정한다(적 추적 성능 설계 D1~D3).
    /// 결과는 공격 범위 전체를 조회해 조준마다 전달을 확인하던 판정과 같다(설계 §4). 대상에 닿을 수 있는 조준 후보가 없는 칸은
    /// 피해량·지정 준비를 만들지 않고 끝난다. 칸과 무관한 값(프로필·공격자 조건·벽·고저)은 처음 필요할 때 한 번만 만든다.
    /// </summary>
    internal sealed class EnemyAttackReach
    {
        private readonly CombatCommandInput _input;
        private readonly CharacterCombatFacts _actor;
        private readonly EntityId _targetId;
        private readonly MovementEnvironmentFacts _environment;
        private readonly HashSet<CellCoord> _targetCells = new HashSet<CellCoord>();
        private bool _hasDestinationModifiers;
        private bool _prepared;
        private bool _attackerAllowed;
        private CombatAttackProfile _profile;
        private WallGeometry _walls;
        private AttackElevation _elevation;
        private readonly Relocations _relocations;

        /// <param name="environment">행위자 진영에서 본 환경이다(경로 찾기가 만든 값을 그대로 쓴다).</param>
        /// <param name="geometry">결정의 관측 기하다. 주면 벽·고저를 다시 만들지 않는다(적 행위자는 같은 진영 관측이라 결과가 같다).</param>
        /// <param name="relocations">같은 행위자의 여러 대상 판정이 칸마다 옮긴 행위자와 투영 입력을 함께 쓰는 보관소다.</param>
        internal EnemyAttackReach(CombatCommandInput input, CharacterCombatFacts actor, EntityId targetId, MovementEnvironmentFacts environment,
            EnemyDecisionContext.ObservedGeometry geometry = null, Relocations relocations = null)
        {
            relocations?.RequireScope(input, actor, environment, geometry);
            _input = input;
            _actor = actor;
            _targetId = targetId;
            _environment = environment;
            _walls = geometry?.Walls;
            _elevation = geometry?.Elevation;
            _relocations = relocations;
        }

        /// <summary>하나의 교전 탐색에서 같은 행위자의 대상과 무관한 칸별 값을 나눈다.</summary>
        internal sealed class Relocations
        {
            private readonly CombatCommandInput _input;
            private readonly CharacterCombatFacts _actor;
            private readonly EnemyDecisionContext.ObservedGeometry _geometry;
            private readonly int _deliveryCapacity;

            internal readonly Dictionary<CellCoord, CharacterCombatFacts> Moved = new Dictionary<CellCoord, CharacterCombatFacts>();
            internal readonly Dictionary<CellCoord, CombatCommandInput> Projected = new Dictionary<CellCoord, CombatCommandInput>();
            internal readonly Dictionary<CellCoord, (int damage, CombatAttackQueries.SelectionPreparation selection)> Prerequisites =
                new Dictionary<CellCoord, (int, CombatAttackQueries.SelectionPreparation)>();
            internal readonly Dictionary<(CellCoord cell, AttackAim aim), ResolvedAttackDelivery> Deliveries =
                new Dictionary<(CellCoord, AttackAim), ResolvedAttackDelivery>();

            internal Relocations(CombatCommandInput input, CharacterCombatFacts actor, EnemyDecisionContext.ObservedGeometry geometry,
                int deliveryCapacity = 256)
            {
                _input = input ?? throw new ArgumentNullException(nameof(input));
                _actor = actor ?? throw new ArgumentNullException(nameof(actor));
                _geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
                _deliveryCapacity = deliveryCapacity >= 0 ? deliveryCapacity : throw new ArgumentOutOfRangeException(nameof(deliveryCapacity));
            }

            /// <summary>반복자가 끝난 전달만 저장한다. 용량이 차면 새 키는 저장하지 않고 기존 값을 유지한다.</summary>
            internal void StoreDelivery(CellCoord cell, AttackAim aim, ResolvedAttackDelivery delivery)
            {
                if (Deliveries.Count < _deliveryCapacity) Deliveries.TryAdd((cell, aim), delivery);
            }

            internal void RequireScope(CombatCommandInput input, CharacterCombatFacts actor, MovementEnvironmentFacts environment,
                EnemyDecisionContext.ObservedGeometry geometry)
            {
                if (!ReferenceEquals(_input, input) || !ReferenceEquals(_actor, actor) || !ReferenceEquals(_geometry, geometry) ||
                    !ReferenceEquals(_geometry.Environment, environment))
                    throw new InvalidOperationException("교전 탐색의 입력·행위자·관측 기하·환경이 다릅니다.");
            }
        }

        internal IEnumerable<int> TargetSteps()
        {
            // 추적 대상을 옮기지 않으므로 이 칸은 탐색 내내 재사용한다.
            foreach (var unit in _input.Characters)
            {
                if (unit.EntityId == _targetId)
                    foreach (var cell in unit.OccupiedCells)
                    {
                        _targetCells.Add(cell);
                        yield return 0;
                    }
                yield return 0;
            }
            foreach (var structure in _input.State.Structures)
            {
                if (structure.EntityId == _targetId)
                    foreach (var cell in structure.OccupiedCells)
                    {
                        _targetCells.Add(cell);
                        yield return 0;
                    }
                yield return 0;
            }
            // 도착 보정이 있는 공개 원본을 찾을 때까지만 읽는다. 없으면 후보마다 환경을 다시 순회할 필요가 없다.
            foreach (var tile in _environment.Tiles)
            {
                _hasDestinationModifiers = tile.Active && tile is DestinationTileModifierFact;
                yield return 0;
                if (_hasDestinationModifiers) break;
            }
        }

        internal bool From(CellCoord cell)
        {
            using var sample = CoreProfiling.Sample(CoreProfileSection.EnemyCanAttackFrom);
            bool canAttack = false;
            foreach (var _ in FromSteps(cell, value => canAttack = value)) { }
            return canAttack;
        }

        internal IEnumerable<string> FromSteps(CellCoord cell, Action<bool> completed)
        {
            if (_relocations == null || !_relocations.Moved.TryGetValue(cell, out var moved))
            {
                moved = MovedTo(cell);
                _relocations?.Moved.Add(cell, moved);
            }
            yield return "PursuitActorPlacement";
            CombatCommandInput projected = null;
            if (!_prepared)
            {
                projected = ProjectAt(cell, moved);
                yield return "PursuitWorldCopy";
                _profile = CombatCommandCalculator.RequiredProfile(projected, moved);
                _attackerAllowed = CombatAttackQueries.RangeActor(projected, out _) == AttackFailureReason.None;
                _prepared = true;
                yield return "PursuitProfileAdmission";
            }
            if (!_attackerAllowed) { completed(false); yield break; }

            // 실제 조준 후보가 있는 칸에서만 피해량·지정 준비를 만든다.
            int? commonDamage = null;
            CombatAttackQueries.SelectionPreparation preparation = null;
            using var aims = CombatAttackQueries.EnumerateAimsThatCanHit(_input, moved, _profile, _targetCells, _environment);
            while (aims.MoveNext())
            {
                yield return "PursuitAimCandidate";
                if (!(aims.Current is AttackAim aim)) continue;
                if (projected == null)
                {
                    projected = ProjectAt(cell, moved);
                    yield return "PursuitWorldCopy";
                }
                if (!commonDamage.HasValue)
                {
                    if (_relocations != null && _relocations.Prerequisites.TryGetValue(cell, out var saved))
                    {
                        commonDamage = saved.damage;
                        preparation = saved.selection;
                    }
                    else
                    {
                        commonDamage = CombatAttackCalculator.CommonAttackDamage(projected, moved, _profile, CombatDamageKind.OrdinaryAttack);
                        yield return "PursuitCommonDamage";
                    }
                }
                if (_walls == null)
                    foreach (var phase in WallGeometryBuilder.BuildSteps(_input.State.BoardWidth, _input.State.BoardHeight,
                        _input.State.Environment, _input.EnvironmentBindings, _input.State.WallBlocking, value => _walls = value))
                        yield return phase;
                if (_elevation == null)
                    foreach (var phase in AttackElevation.BuildSteps(_environment, _input.EnvironmentBindings, value => _elevation = value))
                        yield return phase;
                if (_relocations != null && preparation == null)
                {
                    preparation = CombatAttackQueries.PrepareSelection(projected, moved, _profile, _environment);
                    _relocations.Prerequisites[cell] = (commonDamage.Value, preparation);
                    yield return "PursuitSelectionPreparation";
                }
                ResolvedAttackDelivery delivery = null;
                if (_relocations == null || !_relocations.Deliveries.TryGetValue((cell, aim), out delivery))
                {
                    foreach (var phase in CombatAttackQueries.DeliverySteps(projected, moved, _profile, aim,
                        commonDamage.Value, _environment, _walls, _elevation, value => delivery = value, preparation)) yield return phase;
                    // 콜백이 값을 넘겨도 마지막 yield 뒤에 취소될 수 있으므로 완전히 끝난 뒤에만 게시한다.
                    _relocations?.StoreDelivery(cell, aim, delivery);
                }
                bool delivers = false;
                foreach (var phase in CombatAttackQueries.DeliveryHitsTargetSteps(delivery, _targetId, value => delivers = value))
                    yield return phase;
                if (delivers) { completed(true); yield break; }
            }
            completed(false);
        }

        private CharacterCombatFacts MovedTo(CellCoord cell)
        {
            var moved = _actor;
            if (cell != _actor.Anchor.Value)
            {
                var departed = PositionTileModifierCalculator.Replace(_actor, PositionTileModifier.None);
                moved = CombatMovementCalculator.MoveTo(departed, cell);
                if (_hasDestinationModifiers)
                    moved = PositionTileModifierCalculator.Replace(moved, PositionTileModifierCalculator.ResolveAt(cell, _environment));
            }
            // 추적 목적지의 공격 기하만 계산합니다. 현재 공격 봉쇄가 자발적 이동까지 막지는 않습니다.
            return moved.Debuffs.Disarm == null ? moved :
                moved.With(debuffs: moved.Debuffs.WithRestrictions(null, moved.Debuffs.AnkleBreaker));
        }

        private CombatCommandInput ProjectAt(CellCoord cell, CharacterCombatFacts moved)
        {
            if (_relocations == null) return Project(moved);
            if (!_relocations.Projected.TryGetValue(cell, out var projected))
            {
                projected = Project(moved);
                _relocations.Projected.Add(cell, projected);
            }
            return projected;
        }

        private CombatCommandInput Project(CharacterCombatFacts moved)
        {
            var characters = new CharacterCombatFacts[_input.Characters.Count];
            for (int i = 0; i < characters.Length; i++)
            {
                var unit = _input.Characters[i];
                characters[i] = unit.EntityId == _actor.EntityId ? moved : unit;
            }
            return _input.WithWorld(_input.State, characters, _input.Main, _input.SoulCounts, _input.Souls,
                _input.EnvironmentBindings).WithIntent(CombatCommandKind.Attack, default, default, null, null, sweepSide: null);
        }
    }
}
