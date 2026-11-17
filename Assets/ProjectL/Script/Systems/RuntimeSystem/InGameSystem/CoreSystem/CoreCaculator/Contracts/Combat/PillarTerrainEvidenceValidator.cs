using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>기둥 영향과 압사·파괴·지형 사건의 연결을 순서대로 보존한다. 각도나 피해 산식을 다시 계산하지 않는다.</summary>
    public static class PillarTerrainEvidenceValidator
    {
        public static void Validate(CombatCommandTransition transition)
        {
            if (transition == null) throw new ArgumentNullException(nameof(transition));
            var oldIds = transition.Input.State.Environment.Tiles.Select(t => t.Id).ToHashSet();
            bool hasNewWall = transition.FinalState.Environment.Tiles.OfType<StaticTerrainFact>()
                .Any(t => t.Kind == StaticTerrainKind.Wall && !oldIds.Contains(t.Id));
            if (!hasNewWall && !transition.Events.Any(e => e is CombatPillarFallEvent or CombatPillarTerrainEvent ||
                e is CombatObstacleDurabilityEvent d && d.Final.Definition.Key == EntityKey.Structure_Pillar &&
                    d.Final.IsDestroyed && d.Cause != ObstacleDestructionCause.Fall ||
                e is CombatCharacterEvent c && c.DamageCause == CombatDamageCause.PillarCrush)) return;
            new Validation(transition).Run();
        }

        private sealed class Validation
        {
            private readonly CombatCommandTransition _transition;
            private readonly Dictionary<long, MovementTileFact> _sources;
            private readonly Dictionary<EntityId, StructureCombatFacts> _structures;
            private readonly Dictionary<EntityId, CharacterCombatFacts> _characters;
            private readonly HashSet<CombatEvent> _seen = new();
            private readonly Dictionary<CombatPillarFallEvent, HashSet<CellCoord>> _terrain = new();
            private readonly Dictionary<CombatPillarFallEvent, List<EntityId>> _crushed = new();
            private readonly Dictionary<CombatPillarFallEvent, List<EntityId>> _destroyed = new();
            private readonly HashSet<EntityId> _fallenPillars = new();
            private readonly Dictionary<long, StaticTerrainFact> _changed = new();
            // 계산기와 같이 현재 바인딩의 지형만 본다. 이 검사 범위에서 지형 바인딩은 새 잔해 벽으로만 늘어난다.
            private EnvironmentSemanticBindings _bindings;

            internal Validation(CombatCommandTransition transition)
            {
                _transition = transition;
                _bindings = transition.Input.EnvironmentBindings;
                _sources = transition.Input.State.Environment.Tiles.ToDictionary(t => t.Id);
                _structures = transition.Input.State.Structures.ToDictionary(s => s.EntityId);
                _characters = transition.Input.Characters.ToDictionary(c => c.EntityId);
            }

            internal void Run()
            {
                foreach (var occurrence in _transition.Events)
                {
                    switch (occurrence)
                    {
                        case CombatPillarFallEvent fall:
                            BeginFall(fall);
                            break;
                        case CombatPillarTerrainEvent terrain:
                            ChangeTerrain(terrain);
                            break;
                        case CombatCharacterEvent character:
                            if (character.DamageCause == CombatDamageCause.PillarCrush) RecordCrush(character);
                            _characters[character.Final.EntityId] = character.Final;
                            break;
                        case CombatObstacleDurabilityEvent obstacle:
                            if (obstacle.Cause == ObstacleDestructionCause.PillarCrush)
                            {
                                var parent = obstacle.ContextEvent as CombatPillarFallEvent;
                                Require(parent != null && _destroyed.TryGetValue(parent, out _), "장애물 압사 앞의 낙하 원본");
                                _destroyed[parent].Add(obstacle.ObstacleId);
                            }
                            _structures[obstacle.ObstacleId] = obstacle.Final;
                            break;
                        case CombatStructureEvent structure:
                            _structures[structure.Final.EntityId] = structure.Final;
                            break;
                        case CombatMovementSourceEvent source:
                            _sources[source.Final.Id] = source.Final;
                            if (_changed.ContainsKey(source.Final.Id) && source.Final is StaticTerrainFact final)
                                _changed[source.Final.Id] = final;
                            break;
                        case CombatObjectCreatedEvent created:
                            _sources[created.Source.Id] = created.Source;
                            break;
                        case CombatSummonEvent summon:
                            _characters[summon.Created.EntityId] = summon.Created;
                            break;
                        case CombatReinforcementEvent reinforcement:
                            _characters[reinforcement.Created.EntityId] = reinforcement.Created;
                            break;
                    }
                    _seen.Add(occurrence);
                }
                var expectedFalls = _transition.Events.OfType<CombatObstacleDurabilityEvent>().Where(e =>
                    e.Final.Definition.Key == EntityKey.Structure_Pillar && e.Final.IsDestroyed &&
                    e.Cause != ObstacleDestructionCause.Fall).Select(e => e.ObstacleId);
                Require(_fallenPillars.SetEquals(expectedFalls), "파괴 기둥당 한 번의 낙하");
                foreach (var fall in _terrain.Keys)
                {
                    Require(_terrain[fall].Count == 0, "낙하 영향에 필요한 지형 사건 누락");
                    Require(_crushed[fall].SequenceEqual(fall.CrushedUnitIds) &&
                        _destroyed[fall].SequenceEqual(fall.DestroyedObstacleIds), "낙하 수신자의 정확히 한 번 정산");
                }
                var finalSources = _transition.FinalState.Environment.Tiles.ToDictionary(t => t.Id);
                foreach (var pair in _changed)
                    Require(finalSources.TryGetValue(pair.Key, out var next) && pair.Value.SameValues(next),
                        "기둥 지형 사건과 최종 환경 불일치");
                var oldIds = _transition.Input.State.Environment.Tiles.Select(t => t.Id).ToHashSet();
                Require(_transition.FinalState.Environment.Tiles.OfType<StaticTerrainFact>()
                    .Where(t => t.Kind == StaticTerrainKind.Wall && !oldIds.Contains(t.Id)).All(t => _changed.ContainsKey(t.Id)),
                    "낙하 근거 없는 새 벽");
            }

            private void BeginFall(CombatPillarFallEvent fall)
            {
                Require(_seen.Contains(fall.Destruction) && _seen.Contains(fall.Reaction) &&
                    _fallenPillars.Add(fall.PillarId) &&
                    _structures.TryGetValue(fall.PillarId, out var source) && source.SameValues(fall.Destruction.Final),
                    "실제 파괴 뒤의 유일한 기둥 낙하");
                var profile = _transition.Input.ObstacleContents.SingleOrDefault(p => p.EntityId == fall.PillarId);
                Require(profile != null && profile.Kind == ObstacleContentKind.Pillar && profile.FallLength == fall.FallLength &&
                    fall.Origin.PhysicalCell != fall.PillarCell &&
                    fall.Reaction.Origin.CreditedCharacterId == fall.Origin.CreditedCharacterId, "낙하 정의와 물리 출처");
                var previous = fall.PillarCell;
                foreach (var cell in fall.ImpactCells)
                {
                    var delta = cell - previous;
                    Require(_transition.Input.State.IsInside(cell) && delta == fall.Direction, "연속한 한 방향의 보드 내 영향 칸");
                    previous = cell;
                }
                if (fall.ImpactCells.Count < fall.FallLength)
                    Require(!_transition.Input.State.IsInside(previous + fall.Direction), "보드 경계에서만 낙하 길이 절단");
                var units = PillarImpactRules.CrushedUnits(_characters.Values, fall.ImpactCells);
                var obstacles = PillarImpactRules.DestroyedObstacles(_structures.Values, fall.ImpactCells);
                Require(units.Select(c => c.EntityId).SequenceEqual(fall.CrushedUnitIds) &&
                    obstacles.Select(s => s.EntityId).SequenceEqual(fall.DestroyedObstacleIds), "거리·ID 순의 낙하 수신자");
                _crushed.Add(fall, new List<EntityId>());
                _destroyed.Add(fall, new List<EntityId>());
                _terrain.Add(fall, fall.ImpactCells.Where(CanReceiveDebris).ToHashSet());
            }

            private bool CanReceiveDebris(CellCoord cell) =>
                PillarImpactRules.CanReceiveDebris(_sources.Values, _bindings, _structures.Values, cell);

            private void ChangeTerrain(CombatPillarTerrainEvent change)
            {
                var fall = change.Fall;
                Require(_seen.Contains(fall) && _terrain.TryGetValue(fall, out var pending) &&
                    pending.Count > 0 && fall.ImpactCells.First(pending.Contains) == change.Final.Cell &&
                    pending.Remove(change.Final.Cell), "낙하 영향 순서대로 한 번의 지형 변경");
                Require(_crushed[fall].SequenceEqual(fall.CrushedUnitIds) &&
                    _destroyed[fall].SequenceEqual(fall.DestroyedObstacleIds), "압사·장애물 파괴 뒤의 지형 변경");
                Require(CanReceiveDebris(change.Final.Cell), "기존 벽·메운 바닥·플랫폼·생존 구조물 보존");
                if (change.Expected == null)
                {
                    Require(!_sources.ContainsKey(change.Final.Id) && !_sources.Values.OfType<StaticTerrainFact>().Any(t =>
                        t.Active && t.Cell == change.Final.Cell && t.Kind == StaticTerrainKind.Cliff),
                        "일반 바닥의 새 벽 원본");
                    _bindings = new EnvironmentSemanticBindings(_bindings.TerrainSourceIds.Append(change.Final.Id).ToArray(),
                        _bindings.ObjectSourceIds, _bindings.ObstacleEntityIds);
                }
                else
                {
                    Require(_sources.TryGetValue(change.Expected.Id, out var current) && current.SameValues(change.Expected) &&
                        change.Expected.IsPillarFillSuccessor(change.Final), "현재 낭떠러지의 같은 ID 메우기");
                }
                _sources[change.Final.Id] = change.Final;
                _changed[change.Final.Id] = change.Final;
            }

            private void RecordCrush(CombatCharacterEvent character)
            {
                var steps = _transition.HealthExecutions.SelectMany(e => e.Steps)
                    .Where(s => ReferenceEquals(s.EvidenceEvent, character)).ToArray();
                Require(steps.Length == 1, "압사 체력 실행의 단일 근거");
                var step = steps[0];
                var fall = step.Source.ContextEvent as CombatPillarFallEvent;
                Require(fall != null && _seen.Contains(fall) && _crushed.ContainsKey(fall) &&
                    fall.CrushedUnitIds.Contains(character.Final.EntityId) &&
                    step.Source.DamageCause == CombatDamageCause.PillarCrush &&
                    character.Kind == CombatEventKind.Died && !character.Expected.Health.IsDead &&
                    character.Final.Health.IsDead && step.ActualRecovery == 0 &&
                    step.ActualLoss == character.Expected.Health.CurrentHealth &&
                    character.DamageOrigins.Count == 1 && ReferenceEquals(character.DamageOrigins[0], fall.Reaction.Origin),
                    "생존 처리 없는 실제 압사와 낙하 출처");
                _crushed[fall].Add(character.Final.EntityId);
            }

            private static void Require(bool condition, string reason)
            {
                if (!condition) throw new InvalidOperationException("기둥 지형·압사 근거가 올바르지 않습니다: " + reason);
            }
        }
    }
}
