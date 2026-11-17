using System.Collections.Generic;
using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Diagnostics;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi
{
    /// <summary>공개 사실과 기존 미리보기로 적의 공격과 추적 우선순위를 평가한다.</summary>
    internal sealed class EnemyAiPolicy
    {
        private readonly HostilityPolicy _hostilityPolicy;
        private readonly IReadOnlyList<IMovementTileRule> _movementRules;
        private readonly EnemyDecisionDiagnostics _diagnostics;

        internal EnemyAiPolicy(HostilityPolicy hostilityPolicy, IReadOnlyList<IMovementTileRule> movementRules,
            EnemyDecisionDiagnostics diagnostics = null)
        {
            _hostilityPolicy = hostilityPolicy ?? throw new ArgumentNullException(nameof(hostilityPolicy));
            _movementRules = movementRules ?? throw new ArgumentNullException(nameof(movementRules));
            _diagnostics = diagnostics;
        }

        internal bool TrySelectAttackAim(CombatCommandInput input, AttackRangeResult range, out AttackAim selected)
            => TrySelectAttackAim(input, range, out selected, out _);

        internal bool TrySelectAttackAim(CombatCommandInput input, AttackRangeResult range, out AttackAim selected, out EntityId appliedTaunt)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            selected = default;
            appliedTaunt = default;
            if (!range.Succeeded) return false;
            if (range.AttackerId != input.ActorId) throw new ArgumentException(nameof(range));
            var context = new EnemyDecisionContext(input, Contracts.Combat.EnemyAi.EnemyDecisionSettings.Default);
            foreach (var _ in context.IndexSteps()) { }
            AttackAim? best = null;
            EntityId taunt = default;
            using var sample = CoreProfiling.Sample(CoreProfileSection.EnemyAttackAim);
            foreach (var _ in AttackSteps(input, context.Units[input.ActorId], context, range.AttackAims.ToArray(),
                (aim, source) => { best = aim; taunt = source; })) { }
            selected = best.GetValueOrDefault();
            appliedTaunt = taunt;
            return best.HasValue;
        }

        internal IEnumerable<string> AttackSteps(CombatCommandInput input, CharacterCombatFacts actor,
            EnemyDecisionContext context, IReadOnlyList<AttackAim> suppliedAims, Action<AttackAim?, EntityId> completed)
        {
            if (suppliedAims == null && CombatAttackQueries.RangeActor(input, out _) != AttackFailureReason.None)
            {
                completed(null, default);
                yield break;
            }
            var profile = CombatCommandCalculator.RequiredProfile(input, actor);
            yield return "DecisionAttackProfile";
            MovementEnvironmentFacts environment = null;
            foreach (var phase in input.State.Environment.VisibleSteps(actor.Owner, value => environment = value)) yield return phase;
            WallGeometry walls = null;
            foreach (var phase in WallGeometryBuilder.BuildSteps(input.State.BoardWidth, input.State.BoardHeight,
                input.State.Environment, input.EnvironmentBindings, input.State.WallBlocking, value => walls = value)) yield return phase;
            AttackElevation elevation = null;
            foreach (var phase in AttackElevation.BuildSteps(environment, input.EnvironmentBindings, value => elevation = value)) yield return phase;

            IReadOnlyCollection<CharacterCombatFacts> taunters = null;
            foreach (var _ in TauntTargeting.SourceSteps(input, actor, value => taunters = value)) yield return "DecisionTauntSources";
            int commonDamage = CombatAttackCalculator.CommonAttackDamage(input, actor, profile, CombatDamageKind.OrdinaryAttack);
            var aims = new List<AttackAim>();
            var candidates = suppliedAims == null
                ? CombatAttackQueries.AiRangeCandidates(input, actor, profile, environment)
                : suppliedAims.Select(aim => (AttackAim?)aim);
            foreach (var candidate in candidates)
            {
                if (candidate is AttackAim aim)
                {
                    AttackSelectionEvaluation selection = default;
                    if (suppliedAims == null)
                        foreach (var phase in CombatAttackQueries.SelectionSteps(input, actor, profile, aim, environment, walls,
                            elevation, value => selection = value)) yield return phase;
                    if (suppliedAims != null || selection.FailureReason == AttackFailureReason.None) aims.Add(aim);
                }
                yield return "DecisionAttackCandidate";
            }

            EntityId required = default;
            var allowed = new HashSet<AttackAim>();
            foreach (var source in taunters)
            {
                foreach (var aim in aims)
                {
                    bool delivers = false;
                    foreach (var phase in CombatAttackQueries.DeliveryCheckSteps(input, actor, profile, aim, source.EntityId,
                        commonDamage, environment, walls, elevation, value => delivers = value)) yield return phase;
                    if (delivers) allowed.Add(aim);
                    yield return "DecisionTauntAdmission";
                }
                if (allowed.Count == 0) continue;
                required = source.EntityId;
                break;
            }
            // 사거리 안에서 도발을 지킬 수 없고 걸어갈 수 있으면 공격보다 추적을 먼저 한다.
            if (required.IsEmpty && taunters.Count > 0)
            {
                EntityId reachable = default;
                foreach (var _ in ReachableTaunterSteps(input, taunters, value => reachable = value)) yield return "DecisionTauntPursuit";
                if (!reachable.IsEmpty) { completed(null, default); yield break; }
            }

            AttackAim? best = null;
            (int allies, int targetKind, long damage, int hits, int y, int x, int direction) bestRank = default;
            foreach (var aim in aims)
            {
                if (!required.IsEmpty && !allowed.Contains(aim)) { yield return "DecisionTauntAimExcluded"; continue; }
                if (_diagnostics != null)
                {
                    _diagnostics.WorkUnits++;
                    _diagnostics.GeneratedAttacks++;
                    _diagnostics.PreviewCalls++;
                    _diagnostics.RecordAlternative(actor.EntityId, aim, null);
                }
                using var preview = CombatCommandCalculator.BeginPreview(CombatAttackQueries.WithAim(input, aim), _movementRules);
                yield return "DecisionPreviewBegin";
                while (!preview.IsComplete)
                {
                    preview.Step();
                    yield return preview.Phase;
                }
                if (!preview.Preview.CanAttack) continue;
                var damageByTarget = new Dictionary<EntityId, long>();
                var targets = new HashSet<EntityId>();
                foreach (var entry in preview.Transition.Events)
                {
                    var (id, loss) = ProjectedDamage(entry);
                    if (loss > 0)
                    {
                        damageByTarget[id] = checked(damageByTarget.GetValueOrDefault(id) + loss);
                        targets.Add(id);
                    }
                    yield return "DecisionScoreEvent";
                }
                for (int i = 0; i < preview.Preview.Targets.Length; i++)
                {
                    targets.Add(preview.Preview.Targets.Span[i].TargetId);
                    yield return "DecisionScoreTarget";
                }

                foreach (var explosion in preview.Preview.ThornBombExplosions)
                    foreach (var hit in explosion.Hits)
                        targets.Add(hit.TargetId);

                int allies = 0, hits = 0, unitHits = 0;
                long damage = 0;
                foreach (var id in targets)
                {
                    bool unit = context.Units.TryGetValue(id, out var target);
                    Owner owner = unit ? target.Owner : context.Structures[id].Owner;
                    if (unit && owner == actor.Owner) allies++;
                    bool kind = unit || context.Structures[id].Definition.Key == EntityKey.Structure_Headquarters;
                    // 피해량이 0이어도 합법적인 적대 대상은 선택할 수 있다.
                    if (kind && _hostilityPolicy.AreHostile(actor.Owner, owner))
                    {
                        damage += damageByTarget.GetValueOrDefault(id);
                        hits++;
                        if (unit) unitHits++;
                    }
                    yield return "DecisionScoreRankTarget";
                }
                var rank = (allies, unitHits > 0 ? 0 : 1, -damage, -hits, aim.Cell?.Y ?? 0, aim.Cell?.X ?? 0, (int)aim.Direction);
                if (hits > 0 && (!best.HasValue || rank.CompareTo(bestRank) < 0))
                {
                    best = aim;
                    bestRank = rank;
                }
                yield return "DecisionScoreRank";
            }
            completed(best, best.HasValue ? required : default);
        }

        /// <summary>공개 투영의 피해 사건만 점수에 포함한다.</summary>
        private static (EntityId id, int loss) ProjectedDamage(CombatEvent entry)
        {
            switch (entry)
            {
                case CombatCharacterEvent character when
                    (character.Kind == CombatEventKind.HealthChanged || character.Kind == CombatEventKind.Died) &&
                    character.DamageOrigins.Count > 0:
                    return (character.Final.EntityId, character.Expected.Health.CurrentHealth - character.Final.Health.CurrentHealth);
                case CombatStructureEvent structure when structure.DamageOrigins.Count > 0:
                    return (structure.Final.EntityId, structure.Expected.Health.CurrentHealth - structure.Final.Health.CurrentHealth);
                default:
                    return default;
            }
        }

        internal bool TrySelectPursuitTarget(CombatCommandInput input, out EntityId selected)
            => TrySelectPursuitTarget(input, out selected, out _);

        internal bool TrySelectPursuitTarget(CombatCommandInput input, out EntityId selected, out EntityId appliedTaunt)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            EntityId target = default, taunt = default;
            using var sample = CoreProfiling.Sample(CoreProfileSection.EnemyPursuitTarget);
            foreach (var _ in PursuitTargetSteps(input, actor, (id, source) => { target = id; taunt = source; })) { }
            selected = target;
            appliedTaunt = taunt;
            return !selected.IsEmpty;
        }

        internal IEnumerable<int> PursuitTargetSteps(CombatCommandInput input, CharacterCombatFacts actor,
            Action<EntityId, EntityId> completed)
        {
            IReadOnlyCollection<CharacterCombatFacts> sources = null;
            foreach (var _ in TauntTargeting.SourceSteps(input, actor, value => sources = value)) yield return 0;
            EntityId taunt = default;
            foreach (var _ in ReachableTaunterSteps(input, sources, value => taunt = value)) yield return 0;
            if (!taunt.IsEmpty) { completed(taunt, taunt); yield break; }

            EntityId best = default;
            var rank = (long.MaxValue, int.MaxValue);
            foreach (var unit in input.Characters)
            {
                if (unit.Area == CharacterRuntimeArea.Field && !unit.Health.IsDead && _hostilityPolicy.AreHostile(actor.Owner, unit.Owner))
                    Consider(unit.EntityId, unit.Anchor.Value);
                yield return 0;
            }
            if (best.IsEmpty)
                foreach (var structure in input.State.Structures)
                {
                    if (!structure.Health.IsDead && structure.Definition.Key == EntityKey.Structure_Headquarters &&
                        _hostilityPolicy.AreHostile(actor.Owner, structure.Owner)) Consider(structure.EntityId, structure.Anchor);
                    yield return 0;
                }
            completed(best, default);

            void Consider(EntityId id, CellCoord anchor)
            {
                var origin = actor.Anchor.Value;
                var candidate = (Math.Abs((long)origin.X - anchor.X) + Math.Abs((long)origin.Y - anchor.Y), id.Value);
                if (candidate.CompareTo(rank) >= 0) return;
                rank = candidate;
                best = id;
            }
        }

        private IEnumerable<int> ReachableTaunterSteps(CombatCommandInput input, IEnumerable<CharacterCombatFacts> sources,
            Action<EntityId> completed)
        {
            foreach (var source in sources)
            {
                using var search = new EnemyPursuitSearch(input, source.EntityId, _diagnostics);
                yield return 0;
                while (!search.IsComplete)
                {
                    search.Step();
                    yield return 0;
                }
                if (search.TryGetPath(out _)) { completed(source.EntityId); yield break; }
            }
            completed(default);
        }
    }
}
