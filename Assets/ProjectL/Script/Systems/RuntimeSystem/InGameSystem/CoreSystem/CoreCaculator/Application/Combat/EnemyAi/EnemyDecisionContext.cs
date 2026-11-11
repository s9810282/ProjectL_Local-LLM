using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi
{
    /// <summary>입력의 공개 유닛과 구조물만 결정 수명 안에서 색인한다.</summary>
    internal sealed class EnemyDecisionContext
    {
        internal CombatCommandInput Input { get; }
        internal EnemyDecisionSettings Settings { get; }
        internal int PreviewCount { get; private set; }
        private readonly Dictionary<EntityId, int> _actorPreviews = new Dictionary<EntityId, int>();
        private EnemyThreatField _threat;
        internal EnemyDecisionDiagnostics Diagnostics { get; }
        private ObservedGeometry _geometry;
        private SortedDictionary<(int kind, int id), HostileTarget> _targets;
        private bool _indexed;

        internal bool CanChargePreview(EntityId actor) =>
            _actorPreviews.GetValueOrDefault(actor) < Math.Min(8, Settings.ActorPreviewLimit) &&
            PreviewCount < Math.Min(16, Settings.DecisionPreviewLimit);

        internal bool TryChargePreview(EntityId actor)
        {
            _actorPreviews.TryGetValue(actor, out int count);
            if (!CanChargePreview(actor))
            {
                if (Diagnostics != null) Diagnostics.CapHits++;
                return false;
            }
            _actorPreviews[actor] = count + 1;
            PreviewCount++;
            if (Diagnostics != null) Diagnostics.PreviewCalls++;
            return true;
        }
        internal Dictionary<EntityId, CharacterCombatFacts> Units { get; private set; } = new Dictionary<EntityId, CharacterCombatFacts>();
        internal Dictionary<EntityId, StructureCombatFacts> Structures { get; private set; } = new Dictionary<EntityId, StructureCombatFacts>();
        internal SortedDictionary<EntityId, CharacterCombatFacts> Actors { get; private set; } = new SortedDictionary<EntityId, CharacterCombatFacts>();

        /// <summary>적 진영 캐시다(설계 6.8). 없으면 결정마다 다시 만든다. 후속 공격 추정처럼 투영 입력으로 만든 문맥에는 넘기지 않는다.</summary>
        internal EnemyDecisionSideCache SideCache { get; }
        private StageHazardForecast _forecast;

        internal EnemyDecisionContext(CombatCommandInput input, EnemyDecisionSettings settings, EnemyDecisionDiagnostics diagnostics = null,
            EnemyDecisionSideCache sideCache = null)
        {
            Input = input;
            Settings = settings;
            Diagnostics = diagnostics;
            SideCache = sideCache;
            sideCache?.EnterScope(input);
        }

        /// <summary>결정의 위험 예보다. 진영 캐시의 예보가 같은 읽기 집합이면 그것을 쓴다(<see cref="StageHazardForecast.ReadSet"/>).</summary>
        internal IEnumerable<int> ForecastSteps(Action<StageHazardForecast> completed)
        {
            if (_forecast == null)
            {
                StageHazardForecast.ReadSet reads = null;
                if (SideCache != null)
                {
                    reads = StageHazardForecast.ReadSet.Capture(Input);
                    if (SideCache.TryGetForecast(reads, out var cached)) _forecast = cached;
                }
                if (_forecast == null)
                {
                    using (var builder = new StageHazardForecast.Builder(Input))
                    {
                        yield return 0;
                        while (!builder.IsComplete) { builder.Step(); yield return 0; }
                        _forecast = builder.Build();
                    }
                    if (reads != null) SideCache.StoreForecast(reads, _forecast);
                }
            }
            completed(_forecast);
        }

        /// <summary>결정의 플레이어 위협 필드다. 진영 캐시의 필드가 같은 읽기 집합이면 그것을 쓴다(<see cref="EnemyThreatField.ReadSet"/>).</summary>
        internal IEnumerable<int> ThreatSteps(Action<EnemyThreatField> completed)
        {
            if (_threat == null)
            {
                EnemyThreatField.ReadSet reads = null;
                if (SideCache != null)
                {
                    reads = EnemyThreatField.ReadSet.Capture(Input);
                    if (SideCache.TryGetThreat(reads, out var cached)) _threat = cached;
                }
                if (_threat == null)
                {
                    using (var builder = new EnemyThreatField.Builder(this))
                    {
                        yield return 0;
                        while (!builder.IsComplete)
                        {
                            builder.Step();
                            yield return 0;
                        }
                        _threat = builder.Build();
                    }
                    if (reads != null) SideCache.StoreThreat(reads, _threat);
                }
            }
            completed(_threat);
        }

        internal IEnumerable<int> GeometrySteps(Action<ObservedGeometry> completed)
        {
            if (_geometry == null)
            {
                MovementEnvironmentFacts visible = null;
                foreach (var _ in Input.State.Environment.VisibleSteps(Owner.Enemy, value => visible = value)) yield return 0;
                WallGeometry walls = null;
                // 벽은 두 진영에 항상 공개된 원본에서 검증하고, 고저와 사거리는 적이 관측한 지형으로 계산한다.
                foreach (var _ in WallGeometryBuilder.BuildSteps(Input.State.BoardWidth, Input.State.BoardHeight,
                    Input.State.Environment, Input.EnvironmentBindings, Input.State.WallBlocking, value => walls = value)) yield return 0;
                AttackElevation elevation = null;
                foreach (var _ in AttackElevation.BuildSteps(visible, Input.EnvironmentBindings, value => elevation = value)) yield return 0;
                _geometry = new ObservedGeometry(visible, walls, elevation);
            }
            completed(_geometry);
        }

        internal IEnumerable<int> TargetSteps(Action<IReadOnlyDictionary<(int kind, int id), HostileTarget>> completed)
        {
            if (_targets == null)
            {
                foreach (var _ in IndexSteps()) yield return 0;
                var targets = new SortedDictionary<(int kind, int id), HostileTarget>();
                var hostility = new HostilityPolicy();
                foreach (var unit in Units.Values)
                {
                    if (hostility.AreHostile(Owner.Enemy, unit.Owner))
                        targets.Add((0, unit.EntityId.Value), new HostileTarget(unit.EntityId, unit.Anchor.Value, unit.OccupiedCells, true));
                    yield return 0;
                }
                foreach (var structure in Structures.Values)
                {
                    if (!structure.Health.IsDead && structure.Definition.Key == EntityKey.Structure_Headquarters &&
                        hostility.AreHostile(Owner.Enemy, structure.Owner))
                        targets.Add((1, structure.EntityId.Value),
                            new HostileTarget(structure.EntityId, structure.Anchor, structure.OccupiedCells, false));
                    yield return 0;
                }
                _targets = targets;
            }
            completed(_targets);
        }

        internal IEnumerable<int> IndexSteps()
        {
            if (_indexed) yield break;
            var units = new Dictionary<EntityId, CharacterCombatFacts>();
            var structures = new Dictionary<EntityId, StructureCombatFacts>();
            var actors = new SortedDictionary<EntityId, CharacterCombatFacts>();
            foreach (var unit in Input.Characters)
            {
                if (unit.Area == CharacterRuntimeArea.Field && !unit.Health.IsDead)
                {
                    units.Add(unit.EntityId, unit);
                    if (unit.Owner == Owner.Enemy && UnitActionUseRules.CanStartAttack(unit.ActionUse))
                        actors.Add(unit.EntityId, unit);
                }
                yield return 0;
            }
            foreach (var structure in Input.State.Structures)
            {
                structures.Add(structure.EntityId, structure);
                yield return 0;
            }
            Units = units;
            Structures = structures;
            Actors = actors;
            _indexed = true;
        }

        internal sealed class ObservedGeometry
        {
            internal MovementEnvironmentFacts Environment { get; }
            internal WallGeometry Walls { get; }
            internal AttackElevation Elevation { get; }

            internal ObservedGeometry(MovementEnvironmentFacts environment, WallGeometry walls, AttackElevation elevation)
            { Environment = environment; Walls = walls; Elevation = elevation; }
        }

        internal sealed class HostileTarget
        {
            internal EntityId Id { get; }
            internal CellCoord Anchor { get; }
            internal IReadOnlyCollection<CellCoord> Cells { get; }
            internal bool IsUnit { get; }

            internal HostileTarget(EntityId id, CellCoord anchor, IReadOnlyCollection<CellCoord> cells, bool isUnit)
            { Id = id; Anchor = anchor; Cells = cells; IsUnit = isUnit; }
        }
    }
}
