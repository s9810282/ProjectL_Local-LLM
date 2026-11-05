using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>Core가 연속 게시한 두 배치 사건의 불변 참조입니다. 복원 규칙이나 세계 사본을 보관하지 않습니다.</summary>
    internal sealed class CombatPlacementBatchPlan
    {
        internal sealed class Pair
        {
            internal Pair(CombatCharacterEvent first, CombatCharacterEvent partner)
            {
                First = first;
                Partner = partner;
            }
            internal CombatCharacterEvent First { get; }
            internal CombatCharacterEvent Partner { get; }
        }

        /// <summary>원본 쌍·발동 사건·선행 걸음의 대응입니다. 사건을 제거하거나 새 최종 사실을 계산하지 않습니다.</summary>
        internal sealed class PositionSwap
        {
            internal PositionSwap(Pair pair, CombatEffectsEvent effects, int activationIndex, CombatCharacterEvent[] actorSteps)
            {
                Pair = pair;
                Effects = effects;
                ActivationIndex = activationIndex;
                ActorSteps = Array.AsReadOnly(actorSteps);
            }

            internal Pair Pair { get; }
            internal CombatEffectsEvent Effects { get; }
            internal int ActivationIndex { get; }
            internal IReadOnlyList<CombatCharacterEvent> ActorSteps { get; }
        }

        private readonly Dictionary<CombatCharacterEvent, Pair> _first = new();
        private readonly Dictionary<CombatCharacterEvent, Pair> _partners = new();
        private readonly CombatResult _result;
        private CombatPlacementBatchPlan(CombatResult result) => _result = result;

        internal static CombatPlacementBatchPlan Build(CombatResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            var plan = new CombatPlacementBatchPlan(result);
            for (int i = 0; i < result.Events.Count; i++)
            {
                if (result.Events[i] is not CombatCharacterEvent first || first.Kind != CombatEventKind.Moved ||
                    first.MovementCause?.TraitKey != TraitKey.PositionSwap) continue;
                if (first.MovementCause.Agency != CombatMovementAgency.Voluntary || i + 1 >= result.Events.Count ||
                    result.Events[i + 1] is not CombatCharacterEvent partner || partner.Kind != CombatEventKind.Moved ||
                    partner.MovementCause?.Agency != CombatMovementAgency.Forced || partner.MovementCause.TraitKey != TraitKey.PositionSwap ||
                    first.Final.Anchor != partner.Expected.Anchor)
                    throw new InvalidOperationException("[CombatPlacementBatch] 위치 교환의 주체/승객 사건이 연속 대응하지 않습니다.");
                plan.Add(first, partner);
                i++;
            }
            if (result.UndoMovement?.Participants.Count == 2)
            {
                var returns = result.Events.OfType<CombatCharacterEvent>().Where(e => e.Kind == CombatEventKind.Returned).ToArray();
                if (returns.Length != 2)
                    throw new InvalidOperationException("[CombatPlacementBatch] 쌍 복원에는 두 Returned가 필요합니다.");
                int index = -1;
                for (int i = 0; i < result.Events.Count; i++)
                    if (ReferenceEquals(result.Events[i], returns[0])) index = i;
                if (index < 0 || index + 1 >= result.Events.Count ||
                    !ReferenceEquals(result.Events[index + 1], returns[1]))
                    throw new InvalidOperationException("[CombatPlacementBatch] 쌍 복원의 두 Returned가 연속이어야 합니다.");
                plan.Add(returns[0], returns[1]);
            }
            return plan;
        }

        private void Add(CombatCharacterEvent first, CombatCharacterEvent partner)
        {
            if (first.Final.EntityId == partner.Final.EntityId || first.Expected.Area != CharacterRuntimeArea.Field ||
                first.Final.Area != CharacterRuntimeArea.Field || partner.Expected.Area != CharacterRuntimeArea.Field ||
                partner.Final.Area != CharacterRuntimeArea.Field || first.Final.Anchor == partner.Final.Anchor ||
                !_first.TryAdd(first, new Pair(first, partner)))
                throw new InvalidOperationException("[CombatPlacementBatch] 두 필드 참여자의 배치가 유효해야 합니다.");
            _partners.Add(partner, _first[first]);
        }

        internal Session Begin() => new Session(this);

        /// <summary>전용 표시가 켜진 경우에만 호출합니다. 원본 쌍·발동·선행 걸음을 대조하며 선택적 명령 메타데이터에는 의존하지 않습니다.</summary>
        internal IReadOnlyList<PositionSwap> PositionSwaps()
        {
            var swaps = new List<PositionSwap>();
            var steps = new List<CombatCharacterEvent>();
            Pair pending = null;
            foreach (CombatEvent item in _result.Events)
            {
                if (item is CombatCharacterEvent step && step.Kind == CombatEventKind.Moved)
                {
                    if (_first.TryGetValue(step, out Pair pair))
                    {
                        if (pending != null)
                            throw new InvalidOperationException("[CombatPlacementBatch] 위치 교환 쌍은 발동과 하나씩 대응해야 합니다.");
                        pending = pair;
                    }
                    if (CombatMovementKinds.IsSelfDriven(CombatMovementKinds.Of(step)))
                        steps.Add(step);
                }
                if (item is not CombatEffectsEvent effects)
                    continue;
                for (int index = 0; index < effects.Traits.Count; index++)
                {
                    var activation = effects.Traits[index];
                    if (activation.TraitKey != TraitKey.PositionSwap || activation.TraitActivationKind != TraitActivationKind.MovementChanged)
                        continue;
                    if (pending == null || activation.SourceUnitId != pending.First.Final.EntityId ||
                        activation.TargetIds.Count != 2 || !activation.TargetIds.Contains(pending.First.Final.EntityId) ||
                        !activation.TargetIds.Contains(pending.Partner.Final.EntityId))
                        throw new InvalidOperationException("[CombatPlacementBatch] 위치 교환 발동의 출처·두 대상이 원본 쌍과 대응하지 않습니다.");
                    CombatCharacterEvent[] actorSteps = steps.Where(candidate => candidate.Final.EntityId == pending.First.Final.EntityId).ToArray();
                    if (actorSteps.Length == 0 || !ReferenceEquals(actorSteps[actorSteps.Length - 1], pending.First) ||
                        actorSteps[0].Expected.Anchor != pending.Partner.Final.Anchor)
                        throw new InvalidOperationException("[CombatPlacementBatch] 위치 교환의 선행 경로·도착점이 원본 쌍과 대응하지 않습니다.");
                    swaps.Add(new PositionSwap(pending, effects, index, actorSteps));
                    steps.RemoveAll(candidate => candidate.Final.EntityId == pending.First.Final.EntityId);
                    pending = null;
                }
            }
            if (pending != null)
                throw new InvalidOperationException("[CombatPlacementBatch] 위치 교환 쌍에 대응하는 MovementChanged 발동이 없습니다.");
            return swaps.AsReadOnly();
        }

        /// <summary>현재 결과에서 시작한 쌍만 추적합니다. Cancel/다음 결과에서 버립니다.</summary>
        internal sealed class Session
        {
            private readonly CombatPlacementBatchPlan _plan;
            private readonly HashSet<Pair> _started = new();
            internal Session(CombatPlacementBatchPlan plan) => _plan = plan;
            internal bool TryBegin(CombatCharacterEvent first, out Pair pair)
            {
                if (!_plan._first.TryGetValue(first, out pair)) return false;
                if (!_started.Add(pair)) throw new InvalidOperationException("[CombatPlacementBatch] 같은 쌍을 두 번 시작했습니다.");
                return true;
            }
            internal bool IsPartner(CombatCharacterEvent item)
            {
                if (!_plan._partners.TryGetValue(item, out var pair)) return false;
                if (!_started.Contains(pair)) throw new InvalidOperationException("[CombatPlacementBatch] 주체보다 승객 사건을 먼저 소비했습니다.");
                return true;
            }
        }
    }
}
