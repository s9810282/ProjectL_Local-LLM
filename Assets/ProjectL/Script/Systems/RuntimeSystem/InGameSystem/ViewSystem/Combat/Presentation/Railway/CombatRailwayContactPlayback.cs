using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Railway
{
    /// <summary>
    /// Railway 직후의 직접 사망을 접촉 예약으로 소비하고 열차·사망의 완료를 묶습니다. 사망은 열차 앞면이 대상 몸에 처음 닿는 시각에 시작합니다(히트 타이밍 설계 H7).
    /// 기차 사망의 해제 원본은 이 객체 하나이며, 공격 출처의 해제(<see cref="CombatHitGate"/>)와 사건 집합이 겹치지 않습니다(설계 H1).
    /// 기차 앞면이 필드에 들어오는 순간과 빠져나가는 순간도 알립니다(카메라 연출 조각 설계 K9·K10). 이 객체는 시각만 알고, 그 순간 무엇을 할지(흔들림·줌)는 받은 콜백이 정합니다.
    /// </summary>
    internal sealed class CombatRailwayContactPlayback
    {
        private sealed class Contact
        {
            internal readonly CombatCharacterEvent Death;
            internal readonly float Time;
            internal bool Completed;
            internal Contact(CombatCharacterEvent death, float time)
            {
                Death = death;
                Time = time;
            }
        }

        private sealed class Group
        {
            internal readonly Contact[] Contacts;
            // 필드 진입·이탈 알림 시각입니다. 알림이 없으면 무한대라 부르지 않습니다.
            internal readonly float EntryTime;
            internal readonly float ExitTime;
            internal bool Entered;
            internal bool Left;
            internal Action Completed;
            internal int Next;
            internal int Pending;
            internal int DispatchDepth;
            internal bool TrainCompleted;
            internal bool Ready;
            internal Group(Contact[] contacts, float entryTime, float exitTime)
            {
                Contacts = contacts;
                EntryTime = entryTime;
                ExitTime = exitTime;
            }
        }

        private readonly Action<CombatCharacterEvent, Action> _beginDeath;
        private readonly Action<RailwayGimmickActivationSnapshot, Action, Action<float, float>> _beginTrain;
        // 사망 직전 사실(Expected)에서 접촉 몸을 만듭니다(히트 타이밍 설계 H4·H7). 모양·배치 변환만 받고 표시 Presenter·DB는 모릅니다.
        private readonly Func<CharacterCombatFacts, FieldGridMapper, CombatBody> _bodyOf;
        // 기차 앞면이 필드에 들어올 때·빠져나갈 때 부릅니다(설계 K9·K10). 둘 다 없으면 필드 구간을 계산하지 않습니다.
        private readonly Action _fieldEntered;
        private readonly Action _fieldLeft;
        private readonly Dictionary<CombatGimmickEvent, Group> _groups = new();
        private readonly Dictionary<CombatCharacterEvent, Group> _deaths = new();
        private Group _active;
        private int _version;
        private bool _failed;

        /// <param name="fieldEntered">
        /// 기차 앞면이 필드에 들어오는 순간 묶음마다 한 번 부르는 알림입니다(카메라 흔들림·줌인, 설계 K9·K10). 같은 경과 알림의 접촉 사망보다 먼저 부릅니다.
        /// </param>
        /// <param name="fieldLeft">
        /// 기차 앞면이 마지막 레일 칸을 빠져나가는 순간 묶음마다 한 번 부르는 알림입니다(줌 복귀, 설계 K10). 진입 알림 뒤, 같은 경과의 접촉 사망 뒤에 부릅니다.
        /// 재생 시간 안에 빠져나가지 못하면 재생 끝에 부르고, 그 전에 기차가 끝나거나 취소되면 부르지 않습니다.
        /// </param>
        internal CombatRailwayContactPlayback(Action<CombatCharacterEvent, Action> beginDeath,
            Action<RailwayGimmickActivationSnapshot, Action, Action<float, float>> beginTrain,
            Func<CharacterCombatFacts, FieldGridMapper, CombatBody> bodyOf, Action fieldEntered = null, Action fieldLeft = null)
        {
            _beginDeath = beginDeath ?? throw new ArgumentNullException(nameof(beginDeath));
            _beginTrain = beginTrain ?? throw new ArgumentNullException(nameof(beginTrain));
            _bodyOf = bodyOf ?? throw new ArgumentNullException(nameof(bodyOf));
            _fieldEntered = fieldEntered;
            _fieldLeft = fieldLeft;
        }

        internal bool IsPlaying => _active != null || _failed;
        internal bool IsComplete => !IsPlaying && _groups.Count == 0 && _deaths.Count == 0;

        internal void Prepare(CombatResult result, IReadOnlyList<RailwayTravelPath> paths, FieldGridMapper mapper)
        {
            if (!IsComplete) throw new InvalidOperationException("이전 기차 접촉 묶음이 남아 있습니다.");
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (paths == null) throw new ArgumentNullException(nameof(paths));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            // 전체 대응을 확인한 뒤 게시합니다. 뒤쪽 묶음의 오류도 첫 열차가 출발하기 전에 발견합니다.
            var groups = new Dictionary<CombatGimmickEvent, Group>();
            var deaths = new Dictionary<CombatCharacterEvent, Group>();
            for (int i = 0; i < result.Events.Count; i++)
            {
                if (result.Events[i] is not CombatGimmickEvent item ||
                    item.Activation is not RailwayGimmickActivationSnapshot activation) continue;
                var ids = new HashSet<EntityId>();
                foreach (var id in activation.KilledUnitIds.Span)
                    if (id.IsEmpty || !ids.Add(id)) throw new InvalidOperationException("기차 사망 대상 ID가 비어 있거나 중복됩니다.");
                var byId = new Dictionary<EntityId, CombatCharacterEvent>();
                var targets = new List<RailwayContactSchedule.Target>();
                int end = i + 1;
                while (end < result.Events.Count && result.Events[end] is CombatCharacterEvent death && death.Kind == CombatEventKind.Died)
                {
                    var expected = death.Expected;
                    if (!ids.Remove(death.Final.EntityId) || expected == null || expected.EntityId != death.Final.EntityId ||
                        expected.Health.IsDead || !death.Final.Health.IsDead || expected.Area != CharacterRuntimeArea.Field || !expected.Anchor.HasValue)
                        throw new InvalidOperationException("기차 직후의 직접 사망 사건이 대상 목록과 일치하지 않습니다.");
                    byId.Add(expected.EntityId, death);
                    targets.Add(new RailwayContactSchedule.Target(expected.EntityId, expected.OccupiedCells, _bodyOf(expected, mapper)));
                    end++;
                }
                if (ids.Count != 0) throw new InvalidOperationException("기차 직후에 직접 사망 사건이 모두 있어야 합니다.");
                var schedule = RailwayContactSchedule.Create(paths, targets);
                var contacts = new Contact[schedule.Entries.Count];
                for (int j = 0; j < contacts.Length; j++)
                {
                    var entry = schedule.Entries[j];
                    contacts[j] = new Contact(byId[entry.Id], entry.Time);
                }
                FieldSpanOf(paths, mapper, contacts, out float entryTime, out float exitTime);
                var group = new Group(contacts, entryTime, exitTime);
                groups.Add(item, group);
                foreach (var contact in contacts) deaths.Add(contact.Death, group);
                i = end - 1;
            }
            ++_version;
            foreach (var pair in groups) _groups.Add(pair.Key, pair.Value);
            foreach (var pair in deaths) _deaths.Add(pair.Key, pair.Value);
        }

        internal void Begin(CombatGimmickEvent item, Action completed)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (completed == null) throw new ArgumentNullException(nameof(completed));
            if (IsPlaying || !_groups.TryGetValue(item, out var group))
                throw new InvalidOperationException("현재 결과에서 준비한 기차 사건만 한 번 시작할 수 있습니다.");
            _groups.Remove(item);
            _active = group;
            group.Completed = completed;
            int version = _version;
            group.DispatchDepth++;
            try
            {
                _beginTrain((RailwayGimmickActivationSnapshot)item.Activation, () =>
                {
                    if (!IsCurrent(group, version)) return;
                    group.TrainCompleted = true;
                    TryFinish(group, version);
                }, (previous, current) => Advance(group, version, previous, current));
            }
            catch
            {
                Fail(group, version);
                throw;
            }
            finally
            {
                group.DispatchDepth--;
            }
            TryFinish(group, version);
        }

        private void Advance(Group group, int version, float previous, float current)
        {
            if (!IsCurrent(group, version)) return;
            group.DispatchDepth++;
            try
            {
                if (!float.IsFinite(previous) || !float.IsFinite(current) || previous < 0f || current < previous)
                    throw new InvalidOperationException("기차 경과 시간이 유효하지 않습니다.");
                // 필드 진입 알림을 같은 경과의 접촉 사망보다 먼저 부릅니다. 사망 흔들림이 먼저 나가면 겹침 규칙이 더 약한 기차 흔들림을 막습니다(설계 K9).
                if (!group.Entered && group.EntryTime <= current)
                {
                    group.Entered = true;
                    _fieldEntered?.Invoke();
                }
                while (IsCurrent(group, version) && group.Next < group.Contacts.Length && group.Contacts[group.Next].Time <= current)
                {
                    var contact = group.Contacts[group.Next++];
                    group.Pending++;
                    _beginDeath(contact.Death, () =>
                    {
                        if (!IsCurrent(group, version)) return;
                        if (contact.Completed) throw new InvalidOperationException("사망 완료가 중복 통지됐습니다.");
                        contact.Completed = true;
                        group.Pending--;
                        TryFinish(group, version);
                    });
                }
                // 이탈 알림은 진입 뒤, 같은 경과의 접촉 사망 뒤입니다(설계 K10).
                if (IsCurrent(group, version) && group.Entered && !group.Left && group.ExitTime <= current)
                {
                    group.Left = true;
                    _fieldLeft?.Invoke();
                }
            }
            catch
            {
                Fail(group, version);
                throw;
            }
            finally
            {
                group.DispatchDepth--;
            }
            TryFinish(group, version);
        }

        /// <summary>
        /// 필드 진입·이탈 알림 시각입니다. 진입은 앞면이 레일 칸 몸에 처음 닿는 시각과 첫 접촉 사망 시각 중 이른 쪽이라 접촉 사망보다 늦지 않습니다.
        /// 이탈은 앞면이 레일 칸을 다 지나는 시각입니다(<see cref="RailwayContactSchedule.TryGetFieldSpan"/>).
        /// 알림이 없거나 구할 수 없으면 무한대(부르지 않음)입니다. 접촉은 시각 순으로 정렬돼 있습니다.
        /// </summary>
        private void FieldSpanOf(IReadOnlyList<RailwayTravelPath> paths, FieldGridMapper mapper, Contact[] contacts,
            out float entryTime, out float exitTime)
        {
            entryTime = float.PositiveInfinity;
            exitTime = float.PositiveInfinity;
            if (_fieldEntered == null && _fieldLeft == null) return;
            if (RailwayContactSchedule.TryGetFieldSpan(paths, mapper.CellBody, out float entry, out float exit))
            {
                entryTime = entry;
                exitTime = exit;
            }
            if (contacts.Length > 0) entryTime = Math.Min(entryTime, contacts[0].Time);
        }

        private void TryFinish(Group group, int version)
        {
            if (!IsCurrent(group, version) || group.DispatchDepth != 0 || !group.TrainCompleted ||
                group.Next != group.Contacts.Length || group.Pending != 0) return;
            var completed = group.Completed;
            group.Completed = null;
            group.Ready = true;
            _active = null;
            // 상태를 닫은 뒤 통지합니다. 다음 묶음을 시작한 콜백의 실패로 새 수명을 취소하지 않습니다.
            completed();
        }

        internal bool TryConsumeDeath(CombatEvent item)
        {
            if (item is not CombatCharacterEvent death || !_deaths.TryGetValue(death, out var group)) return false;
            if (!group.Ready) throw new InvalidOperationException("기차와 모든 접촉 사망이 끝나기 전에 후속 사망을 소비할 수 없습니다.");
            _deaths.Remove(death);
            return true;
        }

        internal void Cancel()
        {
            ++_version;
            if (_active != null) _active.Completed = null;
            _active = null;
            _groups.Clear();
            _deaths.Clear();
            _failed = false;
        }

        private bool IsCurrent(Group group, int version) => version == _version && ReferenceEquals(_active, group) && !_failed;

        private void Fail(Group group, int version)
        {
            if (!IsCurrent(group, version)) return;
            Cancel();
            _failed = true;
        }
    }
}
