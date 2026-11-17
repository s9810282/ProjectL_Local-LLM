using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering
{
    /// <summary>
    /// 최종 결과에서 공격마다의 선행 구간을 사건 → 공격으로 대응합니다(클립 칸 다시 짜기 설계 CS11, 계획 TP-13). 결과당 한 번 만들고 재생 수명 안에서만 보관합니다.
    /// 선행 구간 = 그 공격 결과의 발동 중 공격 사건보다 앞에 있는 것(+ 휩쓸기 머리) 가운데 가장 앞 사건부터 공격 사건 직전까지이고, 특성 종류는 보지 않습니다.
    /// 사이에 다른 행동 시작이 있으면 그 뒤부터입니다.
    /// </summary>
    /// <remarks>
    /// 끌어오기·휩쓸기 소속 검사(같은 참조의 이동·머리 사건이 발동과 공격 사이에 있음, 행위자·대상·경로 일치)는 지금처럼 남깁니다. 깨진 소속은 Build에서 오류입니다.
    /// 공격 사건 목록에서 찾지 못한 발동은 선행 구간 계산에서 빠질 뿐 오류가 아닙니다.
    /// </remarks>
    internal sealed class CombatPreAttackLeadMap
    {
        private readonly Dictionary<CombatEvent, CombatPreAttackLead> _leads;

        private CombatPreAttackLeadMap(Dictionary<CombatEvent, CombatPreAttackLead> leads)
        {
            _leads = leads;
        }

        /// <summary>그 사건이 어느 공격의 선행 구간 안이면 참입니다. 사건 목록을 다시 검색하지 않습니다(할당 없음).</summary>
        internal bool TryGet(CombatEvent item, out CombatPreAttackLead lead)
        {
            if (item == null)
            {
                lead = default;
                return false;
            }
            return _leads.TryGetValue(item, out lead);
        }

        /// <param name="includes">선행 구간을 만들 공격 사건 위치인지입니다. 시작 기준이 WithLead인 공격만 넣습니다.</param>
        internal static CombatPreAttackLeadMap Build(CombatResult result, int generation, Func<int, bool> includes)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            if (includes == null)
                throw new ArgumentNullException(nameof(includes));

            var occurrences = new Dictionary<TraitActivationSnapshot, (int Event, int Activation)>(
                ReferenceComparer<TraitActivationSnapshot>.Instance);
            var pulls = new Dictionary<AttackPullSnapshot, int>(ReferenceComparer<AttackPullSnapshot>.Instance);
            var sweeps = new Dictionary<AttackSweepSnapshot, int>(ReferenceComparer<AttackSweepSnapshot>.Instance);
            IndexOccurrences(result, occurrences, pulls, sweeps);

            var leads = new Dictionary<CombatEvent, CombatPreAttackLead>(ReferenceComparer<CombatEvent>.Instance);
            var pullOwners = new Dictionary<AttackPullSnapshot, int>(ReferenceComparer<AttackPullSnapshot>.Instance);
            var sweepOwners = new HashSet<int>();
            for (int attackIndex = 0; attackIndex < result.Events.Count; attackIndex++)
            {
                // 최상위 result.Attack은 요약 사본이므로 별도 출처로 등록하지 않습니다.
                if (!TryGetAttack(result.Events[attackIndex], out AttackResult attack) || !includes(attackIndex))
                    continue;

                int leadStart = attackIndex;
                bool hasPull = false;
                ReadOnlySpan<TraitActivationSnapshot> traits = attack.TraitActivations.Span;
                for (int summaryIndex = 0; summaryIndex < traits.Length; summaryIndex++)
                {
                    TraitActivationSnapshot trait = traits[summaryIndex];
                    if (!occurrences.TryGetValue(trait, out var occurrence) || occurrence.Event >= attackIndex)
                        continue;
                    leadStart = Math.Min(leadStart, occurrence.Event);
                    if (trait.TraitActivationKind == TraitActivationKind.PullApplied)
                    {
                        ResolvePull(result, attackIndex, attack, trait, occurrence, pulls, pullOwners);
                        hasPull = true;
                    }
                }

                if (attack.Pull != null && !hasPull)
                    throw Invalid(result, $"공격 사건 {attackIndex}: 당김 요약에 대응하는 선행 PullApplied 발동이 없습니다.");

                // 휩쓸기는 발동이 없어 공격 결과와 머리 사건의 같은 휩쓸기 참조로 잇습니다(인계 문서: 사건과 결과의 Sweep은 같은 객체).
                if (attack.Sweep != null)
                {
                    if (!sweeps.TryGetValue(attack.Sweep, out int headerIndex) || headerIndex >= attackIndex)
                        throw Invalid(result, $"공격 사건 {attackIndex}: 공격 앞에 같은 참조의 휩쓸기 머리 사건이 없습니다.");
                    if (attack.Sweep.ActorId != attack.AttackerId)
                        throw Invalid(result, $"공격 사건 {attackIndex}: 휩쓸기 행위자가 공격자와 다릅니다.");
                    if (!sweepOwners.Add(headerIndex))
                        throw Invalid(result, $"휩쓸기 머리 사건 {headerIndex}가 공격 둘에 연결됩니다.");
                    leadStart = Math.Min(leadStart, headerIndex);
                }

                var key = new CombatPreAttackSourceKey(result, generation, attackIndex);
                // 공격 사건 바로 앞부터 거꾸로 채웁니다. 사이에 다른 행동 시작이 있으면 그 뒤까지만입니다(앞 행동의 사건을 이 공격 선행으로 세지 않음).
                for (int i = attackIndex - 1; i >= leadStart; i--)
                {
                    CombatEvent item = result.Events[i];
                    if (CombatEventTypeTable.IsActionStart(item))
                        break;
                    leads.TryAdd(item, new CombatPreAttackLead(key, i));
                }
            }

            return new CombatPreAttackLeadMap(leads);
        }

        private static void IndexOccurrences(CombatResult result,
            Dictionary<TraitActivationSnapshot, (int Event, int Activation)> occurrences,
            Dictionary<AttackPullSnapshot, int> pulls, Dictionary<AttackSweepSnapshot, int> sweeps)
        {
            for (int eventIndex = 0; eventIndex < result.Events.Count; eventIndex++)
            {
                if (result.Events[eventIndex] is CombatEffectsEvent effects)
                {
                    // 같은 발동 참조가 둘이면 앞 사건을 씁니다(선행 구간의 시작은 가장 앞 사건이라 결과가 같음).
                    for (int activationIndex = 0; activationIndex < effects.Traits.Count; activationIndex++)
                        occurrences.TryAdd(effects.Traits[activationIndex], (eventIndex, activationIndex));
                }
                else if (result.Events[eventIndex] is CombatPullEvent pull)
                {
                    if (pulls.TryGetValue(pull.Movement, out int previous))
                        throw Invalid(result, $"당김 사건 {eventIndex}: 동일 이동 참조가 사건 {previous}에도 있습니다.");
                    pulls.Add(pull.Movement, eventIndex);
                }
                else if (result.Events[eventIndex] is CombatSweepEvent sweep)
                {
                    if (sweeps.TryGetValue(sweep.Sweep, out int previous))
                        throw Invalid(result, $"휩쓸기 사건 {eventIndex}: 동일 휩쓸기 참조가 사건 {previous}에도 있습니다.");
                    sweeps.Add(sweep.Sweep, eventIndex);
                }
            }
        }

        private static void ResolvePull(CombatResult result, int attackIndex, AttackResult attack,
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
            new($"[CombatPreAttackLeadMap] 전투 {result.BattleInstanceId}, 결과 {result.Sequence}: {message}");

        private sealed class ReferenceComparer<T> : IEqualityComparer<T> where T : class
        {
            internal static readonly ReferenceComparer<T> Instance = new();

            public bool Equals(T x, T y) => ReferenceEquals(x, y);

            public int GetHashCode(T value) => RuntimeHelpers.GetHashCode(value);
        }
    }
}
