using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Trait.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>결과의 구성만 보관하는 불변 계획입니다. 재생 중 실행 여부는 세션이 소유합니다.</summary>
    internal sealed class CombatTraitFeedbackPlan
    {
        internal sealed class Occurrence
        {
            internal Occurrence(int id, int eventIndex, int activationIndex, TraitActivationSnapshot trait, int? cells,
                CombatActivationDirection direction = CombatActivationDirection.Any, CombatActivationRow row = default)
            {
                Id = id;
                EventIndex = eventIndex;
                ActivationIndex = activationIndex;
                Trait = trait;
                Cells = cells;
                Direction = direction;
                Row = row;
            }

            /// <summary>이 발동 값의 방향입니다(결속 방어력 오를 때·내릴 때, 이펙트 끝 규칙 설계 EL7). 모르거나 방향이 없는 종류면 Any입니다.</summary>
            internal CombatActivationDirection Direction { get; }

            /// <summary>이 발동에 쓸 연출 줄입니다(방향 줄, 없으면 Any 줄). 피드백·특성 처리기가 같은 줄을 씁니다.</summary>
            internal CombatActivationRow Row { get; }

            internal int Id { get; }
            internal int EventIndex { get; }
            internal int ActivationIndex { get; }
            internal TraitActivationSnapshot Trait { get; }
            internal int? Cells { get; }
        }

        internal readonly struct Binding
        {
            internal Binding(Occurrence occurrence, EntityId unit)
            {
                Occurrence = occurrence;
                Unit = unit;
            }

            internal Occurrence Occurrence { get; }
            internal TraitActivationSnapshot Trait => Occurrence.Trait;
            internal EntityId Unit { get; }
        }

        private readonly Dictionary<(CombatEffectsEvent, int), Occurrence> _occurrences;
        private readonly Dictionary<CombatTraitStateEvent, IReadOnlyList<Binding>> _states = new();
        private CombatTraitFeedbackPlan(Dictionary<(CombatEffectsEvent, int), Occurrence> occurrences, Dictionary<CombatTraitStateEvent, List<Binding>> states, List<string> issues)
        {
            _occurrences = new Dictionary<(CombatEffectsEvent, int), Occurrence>(occurrences);
            foreach (var pair in states)
                _states.Add(pair.Key, Array.AsReadOnly(pair.Value.ToArray()));
            Issues = Array.AsReadOnly(issues.ToArray());
        }

        internal IReadOnlyList<string> Issues { get; }

        internal bool TryGetOccurrence(CombatEffectsEvent source, int index, out Occurrence occurrence) => _occurrences.TryGetValue((source, index), out occurrence);
        internal bool TryGetBindings(CombatTraitStateEvent state, out IReadOnlyList<Binding> bindings) => _states.TryGetValue(state, out bindings);
        internal static CombatTraitFeedbackPlan Build(CombatResult result, CombatActivationRows rows, CombatTraitStateEvidenceRegistry evidence)
        {
            if (rows == null)
                throw new ArgumentNullException(nameof(rows));
            Func<TraitActivationSnapshot, CombatActivationDirection, CombatActivationRow> rowOf = rows.Of;
            return Build(result, rowOf, evidence);
        }

        internal static CombatTraitFeedbackPlan Build(CombatResult result, Func<TraitActivationSnapshot, CombatActivationDirection, CombatActivationRow> rowOf, CombatTraitStateEvidenceRegistry evidence)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            if (rowOf == null)
                throw new ArgumentNullException(nameof(rowOf));
            var occurrences = new Dictionary<(CombatEffectsEvent, int), Occurrence>();
            var states = new Dictionary<CombatTraitStateEvent, List<Binding>>();
            var issues = new List<string>();
            for (int i = 0; i < result.Events.Count; i++)
            {
                if (result.Events[i] is not CombatEffectsEvent effects)
                    continue;
                for (int traitIndex = 0; traitIndex < effects.Traits.Count; traitIndex++)
                {
                    TraitActivationSnapshot trait = effects.Traits[traitIndex];
                    CombatActivationDirection direction = DirectionOf(result.Events, i, trait);
                    CombatActivationRow row = rowOf(trait, direction);
                    int? cells = null;
                    if (row.Text?.Count == CombatBodyTextCount.CompletedMovementCells)
                    {
                        int count = CombatTraitMovementCount.Before(result.Events, i, trait.SourceUnitId);
                        if (result.CommandKind == CombatCommandKind.Move && result.ActorId == trait.SourceUnitId && result.PathMovement != null && result.PathMovement.DistanceSpent != count)
                        {
                            issues.Add($"{trait.TraitKey}: 실제 이동 사건 수와 경로 결과 거리가 일치하지 않습니다.");
                            count = 0;
                        }

                        cells = count > 0 ? count : null;
                        if (count == 0)
                            issues.Add($"{trait.TraitKey}: 이번 자발 이동 사건이 없어 횟수를 표시할 수 없습니다.");
                    }

                    var occurrence = new Occurrence(occurrences.Count, i, traitIndex, trait, cells, direction, row);
                    occurrences.Add((effects, traitIndex), occurrence);
                    if (!row.Exists || row.Timing != CombatActivationCueTiming.StateApplied)
                        continue;
                    if (evidence == null)
                        throw new InvalidOperationException("[CombatTraitFeedback] 상태 근거 처리기가 연결되지 않았습니다.");
                    IReadOnlyList<EntityId> targets = trait.TargetIds;
                    if (row.Text?.Target == CombatBodyTextTarget.Source && !targets.Contains(trait.SourceUnitId))
                        issues.Add($"{trait.TraitKey}: Source 텍스트에 대응하는 상태 수신자가 없습니다.");
                    foreach (EntityId target in targets)
                    {
                        CombatTraitStateEvent found = null;
                        int candidates = 0;
                        for (int j = i + 1; j < result.Events.Count; j++)
                        {
                            CombatEvent next = result.Events[j];
                            if (CombatEventTypeTable.IsActionStart(next) || next.Kind == CombatEventKind.Returned || next is CombatEffectsEvent other && other.Traits.Any(t => t.SourceUnitId == trait.SourceUnitId && t.TraitKey == trait.TraitKey))
                                break;
                            if (next is CombatTraitStateEvent state && evidence.Matches(trait, target, state))
                            {
                                found = state;
                                candidates++;
                            }
                        }

                        if (candidates != 1)
                        {
                            issues.Add($"{trait.TraitKey}: 대상 {target}의 이후 상태 변경을 유일하게 연결할 수 없습니다({candidates}건). 같은 묶음에 Activation → State 순서로 작성하세요.");
                            continue;
                        }

                        if (!states.TryGetValue(found, out var list))
                            states.Add(found, list = new());
                        list.Add(new Binding(occurrence, target));
                    }

                    if (targets.Count == 0)
                        issues.Add($"{trait.TraitKey}: StateApplied에 상태 수신자가 없습니다.");
                }
            }

            return new CombatTraitFeedbackPlan(occurrences, states, issues);
        }

        /// <summary>
        /// 발동 값의 방향입니다(이펙트 끝 규칙 설계 EL7, 계획 Ee.2). 방향을 고를 수 있는 종류(결속 <c>DefenseChanged</c>)만 보고, 그 발동 뒤 발동 유닛의 사실 사건
        /// (특성 상태 사건 또는 캐릭터 사건)의 앞뒤 결속 방어력 보너스를 비교합니다. 처음으로 값이 바뀐 사건이 정하고, 다음 행동 시작이나 같은 특성의 다음 발동 사건에서 멈춥니다.
        /// 찾지 못하면 Any(방향 모름)입니다.
        /// </summary>
        private static CombatActivationDirection DirectionOf(IReadOnlyList<CombatEvent> events, int index, TraitActivationSnapshot trait)
        {
            if (!TraitActivationVisual.AllowsDirection(trait.TraitActivationKind))
                return CombatActivationDirection.Any;
            for (int j = index + 1; j < events.Count; j++)
            {
                CombatEvent next = events[j];
                if (CombatEventTypeTable.IsActionStart(next)
                    || next is CombatEffectsEvent other && other.Traits.Any(t => t.SourceUnitId == trait.SourceUnitId && t.TraitKey == trait.TraitKey && t.TraitActivationKind == trait.TraitActivationKind))
                    break;
                CharacterCombatFacts before;
                CharacterCombatFacts after;
                if (next is CombatTraitStateEvent state)
                {
                    before = state.Expected;
                    after = state.Final;
                }
                else if (next is CombatCharacterEvent character)
                {
                    before = character.Expected;
                    after = character.Final;
                }
                else
                    continue;
                if (after == null || after.EntityId != trait.SourceUnitId)
                    continue;
                int was = before?.TraitEffects?.CohesionDefenseBonus ?? 0;
                int now = after.TraitEffects?.CohesionDefenseBonus ?? 0;
                if (now > was)
                    return CombatActivationDirection.Up;
                if (now < was)
                    return CombatActivationDirection.Down;
            }
            return CombatActivationDirection.Any;
        }
    }
}
