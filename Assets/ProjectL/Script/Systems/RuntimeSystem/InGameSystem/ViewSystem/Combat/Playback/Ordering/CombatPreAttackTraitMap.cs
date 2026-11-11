using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering
{
    /// <summary>
    /// 최종 결과의 선행 발동과 공격 요약을 참조로 대응합니다. 결과당 한 번 만들고 재생 수명 안에서만 보관합니다.
    /// 새 시작 모드의 연결 경계에서 사용하며 기존 순차 재생을 위한 필수 검사는 아닙니다.
    /// </summary>
    internal sealed class CombatPreAttackTraitMap
    {
        private readonly Dictionary<TraitActivationSnapshot, CombatPreAttackTraitBinding> _bindings;
        private readonly Dictionary<int, CombatPreAttackTraitBinding> _pullBindings;

        private CombatPreAttackTraitMap(Dictionary<TraitActivationSnapshot, CombatPreAttackTraitBinding> bindings,
            Dictionary<int, CombatPreAttackTraitBinding> pullBindings)
        {
            _bindings = bindings;
            _pullBindings = pullBindings;
        }

        /// <summary>지원하지 않거나 공격 뒤에 있는 발동은 정상적으로 false입니다. 깨진 소속은 Build에서 오류입니다.</summary>
        internal bool TryGetBinding(TraitActivationSnapshot trait, out CombatPreAttackTraitBinding binding)
        {
            if (trait == null)
                throw new ArgumentNullException(nameof(trait));
            return _bindings.TryGetValue(trait, out binding);
        }

        /// <summary>이 결과의 당김 사건 실행 시작에서 원래 발동을 찾습니다. 사건 목록을 다시 검색하지 않습니다.</summary>
        internal bool TryGetPullBinding(int eventIndex, out CombatPreAttackTraitBinding binding) =>
            _pullBindings.TryGetValue(eventIndex, out binding);

        internal static CombatPreAttackTraitMap Build(CombatResult result, int generation)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            var occurrences = new Dictionary<TraitActivationSnapshot, (int Event, int Activation)>(
                ReferenceComparer<TraitActivationSnapshot>.Instance);
            var pulls = new Dictionary<AttackPullSnapshot, int>(ReferenceComparer<AttackPullSnapshot>.Instance);
            IndexOccurrences(result, occurrences, pulls);

            var bindings = new Dictionary<TraitActivationSnapshot, CombatPreAttackTraitBinding>(
                ReferenceComparer<TraitActivationSnapshot>.Instance);
            var pullBindings = new Dictionary<int, CombatPreAttackTraitBinding>();
            var owners = new Dictionary<TraitActivationSnapshot, (int Event, int Summary)>(
                ReferenceComparer<TraitActivationSnapshot>.Instance);
            var pullOwners = new Dictionary<AttackPullSnapshot, int>(ReferenceComparer<AttackPullSnapshot>.Instance);

            for (int attackIndex = 0; attackIndex < result.Events.Count; attackIndex++)
            {
                // 최상위 result.Attack은 요약 사본이므로 별도 출처로 등록하지 않습니다.
                if (!TryGetAttack(result.Events[attackIndex], out AttackResult attack))
                    continue;

                var key = new CombatPreAttackSourceKey(result, generation, attackIndex);
                ReadOnlySpan<TraitActivationSnapshot> traits = attack.TraitActivations.Span;
                bool hasPullBinding = false;
                for (int summaryIndex = 0; summaryIndex < traits.Length; summaryIndex++)
                {
                    TraitActivationSnapshot trait = traits[summaryIndex];
                    if (!CombatPreAttackTraitKinds.Supports(trait.TraitKey, trait.TraitActivationKind))
                        continue;

                    if (owners.TryGetValue(trait, out var owner))
                        throw Invalid(result, $"공격 사건 {attackIndex}, 요약 {summaryIndex}: 발동이 공격 사건 {owner.Event}, 요약 {owner.Summary}에 중복 소속됩니다.");
                    owners.Add(trait, (attackIndex, summaryIndex));

                    if (!occurrences.TryGetValue(trait, out var occurrence))
                        throw Invalid(result, $"공격 사건 {attackIndex}, 요약 {summaryIndex}({trait.TraitKey}/{trait.TraitActivationKind}): 동일 참조의 발동 사건이 없습니다.");
                    if (occurrence.Event >= attackIndex)
                        continue;

                    int? pullIndex = null;
                    if (trait.TraitActivationKind == TraitActivationKind.PullApplied)
                    {
                        pullIndex = ResolvePull(result, attackIndex, attack, trait, occurrence, pulls, pullOwners);
                        hasPullBinding = true;
                    }

                    var binding = new CombatPreAttackTraitBinding(key, trait,
                        occurrence.Event, occurrence.Activation, pullIndex);
                    bindings.Add(trait, binding);
                    if (pullIndex.HasValue)
                        pullBindings.Add(pullIndex.Value, binding);
                }

                if (attack.Pull != null && !hasPullBinding)
                    throw Invalid(result, $"공격 사건 {attackIndex}: 당김 요약에 대응하는 선행 PullApplied 발동이 없습니다.");
            }

            return new CombatPreAttackTraitMap(bindings, pullBindings);
        }

        private static void IndexOccurrences(CombatResult result,
            Dictionary<TraitActivationSnapshot, (int Event, int Activation)> occurrences,
            Dictionary<AttackPullSnapshot, int> pulls)
        {
            for (int eventIndex = 0; eventIndex < result.Events.Count; eventIndex++)
            {
                if (result.Events[eventIndex] is CombatEffectsEvent effects)
                {
                    for (int activationIndex = 0; activationIndex < effects.Traits.Count; activationIndex++)
                    {
                        TraitActivationSnapshot trait = effects.Traits[activationIndex];
                        if (!CombatPreAttackTraitKinds.Supports(trait.TraitKey, trait.TraitActivationKind))
                            continue;
                        if (occurrences.TryGetValue(trait, out var previous))
                            throw Invalid(result, $"사건 {eventIndex}, 발동 {activationIndex}: 동일 참조가 사건 {previous.Event}, 발동 {previous.Activation}에도 있습니다.");
                        occurrences.Add(trait, (eventIndex, activationIndex));
                    }
                }
                else if (result.Events[eventIndex] is CombatPullEvent pull)
                {
                    if (pulls.TryGetValue(pull.Movement, out int previous))
                        throw Invalid(result, $"당김 사건 {eventIndex}: 동일 이동 참조가 사건 {previous}에도 있습니다.");
                    pulls.Add(pull.Movement, eventIndex);
                }
            }
        }

        private static int ResolvePull(CombatResult result, int attackIndex, AttackResult attack,
            TraitActivationSnapshot trait, (int Event, int Activation) occurrence,
            Dictionary<AttackPullSnapshot, int> pulls, Dictionary<AttackPullSnapshot, int> owners)
        {
            AttackPullSnapshot movement = attack.Pull;
            string location = $"공격 사건 {attackIndex}, 선행 사건 {occurrence.Event}, 발동 {occurrence.Activation}";
            if (movement == null || !pulls.TryGetValue(movement, out int pullIndex))
                throw Invalid(result, $"{location}: 동일 참조의 당김 이동 사건이 없습니다.");
            if (pullIndex <= occurrence.Event || pullIndex >= attackIndex)
                throw Invalid(result, $"{location}: 당김 사건 {pullIndex}가 발동과 공격 사이에 없습니다.");
            if (owners.TryGetValue(movement, out int previous))
                throw Invalid(result, $"{location}: 당김 사건 {pullIndex}가 공격 사건 {previous}에 이미 연결됐습니다.");
            if (trait.SourceUnitId != attack.AttackerId || trait.TargetIds.Count != 1 ||
                trait.TargetIds[0] != movement.TargetId || trait.TargetCells.Count != movement.Path.Count)
                throw Invalid(result, $"{location}: 당김 사건 {pullIndex}의 행위자·대상·경로와 발동이 일치하지 않습니다.");
            for (int i = 0; i < movement.Path.Count; i++)
            {
                if (trait.TargetCells[i] != movement.Path[i])
                    throw Invalid(result, $"{location}: 당김 사건 {pullIndex}의 경로 {i}와 발동 칸이 일치하지 않습니다.");
            }
            owners.Add(movement, attackIndex);
            return pullIndex;
        }

        internal static bool TryGetAttack(CombatEvent source, out AttackResult attack)
        {
            switch (source)
            {
                case CombatAttackEvent attacked:
                    attack = attacked.Attack;
                    return true;
                case CombatRoarEvent roar when roar.Attack.HasValue:
                    attack = roar.Attack.Value;
                    return true;
                default:
                    // 추가 공격 발동과 가로채기 반응을 부모 공격의 시작 기준으로 편입하지 않습니다.
                    attack = default;
                    return false;
            }
        }

        private static InvalidOperationException Invalid(CombatResult result, string message) =>
            new($"[CombatPreAttackTraitMap] 전투 {result.BattleInstanceId}, 결과 {result.Sequence}: {message}");

        private sealed class ReferenceComparer<T> : IEqualityComparer<T> where T : class
        {
            internal static readonly ReferenceComparer<T> Instance = new();

            public bool Equals(T x, T y) => ReferenceEquals(x, y);

            public int GetHashCode(T value) => RuntimeHelpers.GetHashCode(value);
        }
    }
}
