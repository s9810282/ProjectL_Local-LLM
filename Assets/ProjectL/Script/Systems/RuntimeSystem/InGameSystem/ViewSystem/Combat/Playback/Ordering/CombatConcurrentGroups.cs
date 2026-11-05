using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering
{
    /// <summary>
    /// 결과의 사건을 "같은 순간" 묶음으로 나눕니다(동시 처리 설계 C1, 사용자 결정 Q-C1 A). 근거는 Core 사실뿐입니다.
    /// 같은 행동 구간(<see cref="CombatEventTypeTable.IsActionStart"/> 사이)에서 피해 근원 열쇠(<see cref="CombatEventSubjects.OriginKey"/>)가 같은 사건과,
    /// 그 출처의 이동 후속(넉백 등, <see cref="CombatHitFollowUps"/>)이 한 묶음입니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 근원이 없거나 섞인 사건, 주체가 없는 사건(영혼·효과·원본 변경·턴·기믹 등), 행동 사건은 묶음 밖(-1)입니다.
    /// 기차 직후의 직접 사망은 근원이 없어 묶음 밖이며 기차 전용 재생이 맡습니다(설계 C7, Q-C3 A).
    /// </para>
    /// <para>
    /// 한 대상의 넉백 경로(그 묶음의 첫 이동 ~ 마지막 이동) 안에 다른 사건(넉백 가시 피해·중간 칸 발동 등 묶음 밖이나 다른 묶음)이 끼면
    /// 그 대상의 이동은 묶음 밖으로 둡니다(C-4 검토, 사용자 결정 (a)). 함께 출발했다가 낀 사건이 앞 대상 경로 끝을 기다리느라 경로 중간에 멈추지 않고,
    /// 그 대상은 옛 순서대로 앞 사건이 모두 끝난 뒤 걷습니다. 경로가 끊기지 않은 대상은 계속 함께 걷습니다.
    /// 약한 지반 진입 기록은 바로 앞 이동의 묶음에 붙으므로 경로를 끊지 않습니다(<see cref="AttachEntryRecords"/>).
    /// </para>
    /// <para>색인 기준이라 같은 사건 인스턴스가 결과에 두 번 있어도 예외가 나지 않습니다. 결과 시작 때 한 번 해석합니다.</para>
    /// </remarks>
    internal sealed class CombatConcurrentGroups
    {
        private readonly CombatHitFollowUps _followUps = new();
        private readonly List<int> _groups = new();
        private readonly List<EntityId> _subjects = new();
        // 사건마다 속한 행동 구간 번호입니다. 아이템 사건을 같은 구간의 소지자 묶음에 붙일 때 씁니다.
        private readonly List<int> _segments = new();
        private readonly Dictionary<(int Segment, CombatEventSubjects.OriginKey Key), int> _keys = new();
        private readonly Dictionary<(int Group, EntityId Subject), (int First, int Last)> _paths = new();
        private readonly List<(int Group, EntityId Subject)> _interrupted = new();

        /// <summary>해석한 사건 수입니다.</summary>
        internal int Count => _groups.Count;

        internal void Begin(CombatResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            Clear();
            _followUps.Begin(result);
            int segment = 0;
            for (int i = 0; i < result.Events.Count; i++)
            {
                CombatEvent item = result.Events[i];
                if (CombatEventTypeTable.IsActionStart(item))
                {
                    segment++;
                    Add(-1, EntityId.Empty, segment);
                    continue;
                }
                if (!CombatEventTypeTable.TryGetSubject(item, out EntityId subject) || !TryGetKey(item, out CombatEventSubjects.OriginKey key))
                {
                    Add(-1, EntityId.Empty, segment);
                    continue;
                }
                if (!_keys.TryGetValue((segment, key), out int group))
                {
                    group = _keys.Count;
                    _keys.Add((segment, key), group);
                }
                Add(group, subject, segment);
            }
            AttachItemEvents(result);
            AttachEntryRecords(result);
            AttachRockfallRecipients(result);
            SplitInterruptedPaths(result);
        }

        /// <summary>
        /// 낙석에 맞은 유닛의 체력·속박 사건은 피해 근원이 없어 그대로 두면 한 명씩 차례로 나옵니다. 충돌 바로 뒤에 이어지는 근원 없는 캐릭터·상태 사건 중
        /// 주체가 수신자인 것을 충돌마다 새 묶음으로 묶어 함께 반응하게 합니다(낙석 View 계획 RP-8). 다른 사건을 만나면 멈춥니다. 같은 유닛의 순서는 <see cref="CanOverlap"/>이 지킵니다.
        /// </summary>
        private void AttachRockfallRecipients(CombatResult result)
        {
            int group = _keys.Count;
            for (int i = 0; i < result.Events.Count; i++)
            {
                if (result.Events[i] is not CombatRockfallEvent { Step: RockfallGimmickStep.Impact } rockfall)
                    continue;
                for (int next = i + 1; next < result.Events.Count; next++)
                {
                    CombatEvent item = result.Events[next];
                    if (_groups[next] >= 0 || item is not (CombatCharacterEvent or CombatTraitStateEvent)
                        || !CombatEventTypeTable.TryGetSubject(item, out EntityId subject) || !rockfall.RecipientIds.Contains(subject)
                        || CombatEventTypeTable.TryGetOrigins(item, out _))
                        break;
                    _groups[next] = group;
                    _subjects[next] = subject;
                }
                group++;
            }
        }

        /// <summary>
        /// 약한 지반 진입 기록(종류가 그대로인 원본 변경)은 주체가 없지만, Core가 강제 이동 바로 뒤에 그 칸 진입으로 기록합니다(약한 지반 설계 WG1).
        /// 그대로 두면 묶음 밖 장벽이 되어 여러 대상 동시 넉백이 차례 재생으로 바뀌므로, 바로 앞 이동 사건과 같은 묶음·주체에 둡니다(약한 지반 검토 R2).
        /// 바로 앞이 묶음 안의 이동이 아니면(자발적 이동 등) 지금처럼 묶음 밖입니다. 종류가 바뀐 붕괴 기록은 명령 끝의 장벽으로 남습니다.
        /// </summary>
        private void AttachEntryRecords(CombatResult result)
        {
            for (int i = 1; i < result.Events.Count; i++)
            {
                if (_groups[i] >= 0 || !CombatTerrainChanges.IsSameKindEntry(result.Events[i]))
                    continue;
                int previous = i - 1;
                if (_groups[previous] < 0 || _segments[previous] != _segments[i] || !CombatEventTypeTable.IsMovement(result.Events[previous]))
                    continue;
                _groups[i] = _groups[previous];
                _subjects[i] = _subjects[previous];
            }
        }

        /// <summary>
        /// 아이템 사건은 피해 근원이 없지만 Core가 소지자의 피해 처리 안에서 기록합니다(가방 설계 P7). 그대로 두면 묶음 밖 장벽이 되어
        /// 여러 대상 피격 사이의 발동이 다른 대상의 피격을 막으므로, 같은 행동 구간에서 가장 가까운 같은 소지자의 묶음 사건과 같은 묶음에 둡니다.
        /// 피해 앞 단계(로켓)는 뒤 사건을 먼저, 그 밖은 앞 사건을 먼저 봅니다. 찾지 못하면 지금처럼 묶음 밖입니다.
        /// 발동 바로 뒤에 오는 같은 소지자의 근원 없는 캐릭터 사건(위스키 회복 체력, <c>CombatDamageApplication.Apply</c>)도 같은 묶음에 둡니다.
        /// </summary>
        private void AttachItemEvents(CombatResult result)
        {
            for (int i = 0; i < result.Events.Count; i++)
            {
                if (_groups[i] >= 0 || result.Events[i] is not CombatItemEvent item || item.HolderId.IsEmpty)
                    continue;
                int group = item.Stage == ItemEventStage.BeforeDamage
                    ? FindHolderGroup(i, item.HolderId, forwardFirst: true)
                    : FindHolderGroup(i, item.HolderId, forwardFirst: false);
                if (group < 0)
                    continue;
                _groups[i] = group;
                _subjects[i] = item.HolderId;

                int next = i + 1;
                if (item.Activated && next < result.Events.Count && _groups[next] < 0 && _segments[next] == _segments[i]
                    && result.Events[next] is CombatCharacterEvent recovery
                    && CombatEventTypeTable.TryGetSubject(recovery, out EntityId recovered) && recovered == item.HolderId
                    && !CombatEventTypeTable.TryGetOrigins(recovery, out _))
                {
                    _groups[next] = group;
                    _subjects[next] = item.HolderId;
                }
            }
        }

        private int FindHolderGroup(int index, EntityId holder, bool forwardFirst)
        {
            int first = ScanHolderGroup(index, holder, forwardFirst ? +1 : -1);
            return first >= 0 ? first : ScanHolderGroup(index, holder, forwardFirst ? -1 : +1);
        }

        private int ScanHolderGroup(int index, EntityId holder, int step)
        {
            int segment = _segments[index];
            for (int j = index + step; j >= 0 && j < _groups.Count && _segments[j] == segment; j += step)
            {
                if (_groups[j] >= 0 && _subjects[j] == holder)
                    return _groups[j];
            }
            return -1;
        }

        /// <summary>그 사건의 묶음 번호입니다. 묶음 밖·범위 밖이면 -1입니다.</summary>
        internal int GroupOf(int index) => index >= 0 && index < _groups.Count ? _groups[index] : -1;

        /// <summary>그 사건의 주체입니다. 묶음 밖이면 비어 있습니다.</summary>
        internal EntityId SubjectOf(int index) => index >= 0 && index < _subjects.Count ? _subjects[index] : EntityId.Empty;

        /// <summary>
        /// 앞 사건(<paramref name="earlier"/>)이 끝나기 전에 뒤 사건(<paramref name="later"/>)을 시작해도 되는지입니다(설계 C2 규칙 2).
        /// 같은 묶음이고 주체가 다를 때만 true입니다. 같은 유닛의 사건 순서와 묶음 밖 사건 순서는 지금과 같습니다.
        /// </summary>
        internal bool CanOverlap(int earlier, int later)
        {
            int group = GroupOf(earlier);
            return group >= 0 && group == GroupOf(later) && SubjectOf(earlier) != SubjectOf(later);
        }

        /// <summary>그 사건이 묶음 밖인지입니다. 묶음 밖의 미완료 사건은 뒤 사건 누구도 지나갈 수 없습니다.</summary>
        internal bool IsBarrier(int index) => GroupOf(index) < 0;

        /// <summary>
        /// 그 사건이 <paramref name="source"/>의 맞는 순간 후속이고 주체가 <paramref name="unit"/>인지입니다(유닛 행동 계획 TP-13). 출처 전체 종료 때 풀리는 후속은 아닙니다.
        /// 출처는 같은 사건 인스턴스(참조)로 가립니다.
        /// </summary>
        internal bool IsHitFollowUpOf(CombatEvent item, CombatEvent source, EntityId unit) =>
            item != null && _followUps.TryGet(item, out CombatHitFollowUps.FollowUp followUp) && followUp.ReleasedByHit &&
            ReferenceEquals(followUp.Source, source) && followUp.Target == unit;

        internal void Clear()
        {
            _followUps.Clear();
            _groups.Clear();
            _subjects.Clear();
            _segments.Clear();
            _keys.Clear();
            _paths.Clear();
            _interrupted.Clear();
        }

        // 넉백 경로 안에 다른 사건이 낀 대상의 이동을 묶음 밖으로 둡니다(클래스 설명). 판정을 모두 마친 뒤 바꿔 경로끼리 서로의 판정에 닿지 않게 합니다.
        private void SplitInterruptedPaths(CombatResult result)
        {
            _paths.Clear();
            _interrupted.Clear();
            for (int i = 0; i < _groups.Count; i++)
            {
                if (_groups[i] < 0 || !CombatEventTypeTable.IsMovement(result.Events[i])) continue;
                var path = (_groups[i], _subjects[i]);
                _paths[path] = _paths.TryGetValue(path, out var span) ? (span.First, i) : (i, i);
            }
            foreach (KeyValuePair<(int Group, EntityId Subject), (int First, int Last)> path in _paths)
            {
                for (int i = path.Value.First + 1; i < path.Value.Last; i++)
                {
                    if (_groups[i] == path.Key.Group) continue;
                    _interrupted.Add(path.Key);
                    break;
                }
            }
            foreach ((int group, EntityId subject) in _interrupted)
            {
                (int first, int last) = _paths[(group, subject)];
                for (int i = first; i <= last; i++)
                {
                    if (_groups[i] != group || _subjects[i] != subject || !CombatEventTypeTable.IsMovement(result.Events[i])) continue;
                    _groups[i] = -1;
                    _subjects[i] = EntityId.Empty;
                }
            }
        }

        // 피해 근원이 있으면 그 열쇠, 출처의 이동 후속이면 출처의 열쇠(공격 그룹, 공격자)입니다. 출처는 추가 공격 시작일 수도 있습니다(특성 시스템 View 계획 TP-2).
        private bool TryGetKey(CombatEvent item, out CombatEventSubjects.OriginKey key)
        {
            if (CombatEventTypeTable.IsMovement(item))
            {
                key = default;
                if (!_followUps.TryGet(item, out CombatHitFollowUps.FollowUp followUp) ||
                    !CombatEventTypeTable.TryGetAttackSource(followUp.Source, out CombatAttackSource source) ||
                    source.AttackGroup == 0)
                    return false;
                key = source.Key;
                return true;
            }
            return CombatEventSubjects.TryGetOriginKey(item, out key);
        }

        private void Add(int group, EntityId subject, int segment)
        {
            _groups.Add(group);
            _subjects.Add(subject);
            _segments.Add(segment);
        }
    }
}
