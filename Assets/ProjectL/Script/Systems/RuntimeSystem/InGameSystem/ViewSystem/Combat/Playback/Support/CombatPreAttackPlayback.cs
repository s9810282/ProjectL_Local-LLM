using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>선행 연출의 첫 시작 예약과 원래 공격 사건의 합류를 소유합니다. 클립 진행·대여는 기존 조각 실행에 맡깁니다.</summary>
    internal sealed class CombatPreAttackPlayback
    {
        private sealed class Reservation
        {
            internal CombatPreAttackSourceKey Key;
            internal CombatAttackEvent Event;
            internal CombatAttackShape Shape;
            internal CombatDeferredAttackSource Source;
            internal CombatPieceRun Run;
            internal double Due;
            internal bool WaitForEntry;
            internal bool JoinRequested;
            internal bool Joined;
            internal bool Discarded;
            internal bool DelayWarned;
            internal bool EventCompleted;
            internal Action Completed;
        }

        private readonly CombatAttackStart _start;
        private readonly CombatPieceRunPlayer _pieces;
        private readonly CombatAttackPlayback _attacks;
        private readonly Func<int> _generation;
        private readonly Func<EntityId, bool> _isAlive;
        private readonly Func<CombatEffectsEvent, EntityId, bool> _mayUseUnitAction;
        private readonly Dictionary<CombatEvent, Reservation> _reservations = new();
        private readonly HashSet<CombatEvent> _entered = new();
        private readonly List<Reservation> _active = new();
        private CombatResult _result;
        private double _time;
        private bool _flushing;

        internal CombatPreAttackPlayback(CombatAttackStart start, CombatPieceRunPlayer pieces, CombatAttackPlayback attacks,
            Func<int> generation, Func<EntityId, bool> isAlive, Func<CombatEffectsEvent, EntityId, bool> mayUseUnitAction)
        {
            _start = start ?? throw new ArgumentNullException(nameof(start));
            _pieces = pieces ?? throw new ArgumentNullException(nameof(pieces));
            _attacks = attacks ?? throw new ArgumentNullException(nameof(attacks));
            _generation = generation ?? throw new ArgumentNullException(nameof(generation));
            _isAlive = isAlive ?? throw new ArgumentNullException(nameof(isAlive));
            _mayUseUnitAction = mayUseUnitAction ?? throw new ArgumentNullException(nameof(mayUseUnitAction));
        }

        internal bool IsComplete => _active.Count == 0;

        internal void Begin(CombatResult result)
        {
            Cancel();
            _result = result ?? throw new ArgumentNullException(nameof(result));
            _time = 0;
        }

        /// <summary>
        /// 선행 구간 사건 하나의 연출이 시작됐습니다(클립 칸 다시 짜기 설계 CS11). 그 공격의 첫 시작만 예약하고, 같은 구간의 다음 사건은 시간을 다시 잡지 않습니다.
        /// 결과 도착 시각은 사용하지 않습니다.
        /// </summary>
        /// <param name="delay">선행 연출 시작부터 공격 동작 시작까지의 초입니다(<see cref="CombatClipAttackData.MotionStartDelay"/>). 음수는 바로 시작과 같습니다.</param>
        /// <param name="leadPending">그 사건이 아직 처리되지 않았는지입니다. 앞당김(사건을 붙잡기 전에 예약)에서만 참이고, 그 사건 자신도 의존 검사에 넣습니다.</param>
        internal void Reserve(CombatPreAttackLead lead, float delay, bool leadPending)
        {
            if (!ReferenceEquals(_result, lead.Attack.Result) || lead.Attack.Generation != _generation()) return;
            // 포효·추가 공격은 기존 전용 조립을 유지합니다. 이 설정은 일반 공격 클립의 입력입니다.
            if (_result.Events[lead.Attack.EventIndex] is not CombatAttackEvent attack ||
                !attack.Attack.Succeeded || _entered.Contains(attack) || _reservations.ContainsKey(attack)) return;
            var shape = CombatAttackShape.Of(attack);
            var reservation = new Reservation
            {
                Key = lead.Attack,
                Event = attack,
                Shape = shape,
                Source = new CombatDeferredAttackSource(_attacks, _generation),
                Due = _time + delay,
                // 가로채기 배치·공격자 자신의 선행 사실·다른 출처를 조기 대여로 막지 않습니다.
                WaitForEntry = shape.Reactions.Count != 0 || HasPrecedingDependency(lead, attack, leadPending),
            };
            _reservations.Add(attack, reservation);
            _active.Add(reservation);
        }

        /// <summary>
        /// 그 공격의 예약이 조기 동작을 이미 시작했는지입니다(실행이 있고 버리지 않음). 앞당김이 선행 사건을 붙잡기 전에 묻습니다 —
        /// 시작하지 못했는데 붙잡으면 사건만 늦고 동작은 보이지 않습니다(묶음 끝 검토 M2). 지난 결과·세대면 거짓입니다.
        /// </summary>
        internal bool HasStarted(CombatPreAttackLead lead)
        {
            if (_result == null || !ReferenceEquals(_result, lead.Attack.Result) || lead.Attack.Generation != _generation()) return false;
            return _result.Events[lead.Attack.EventIndex] is CombatAttackEvent attack &&
                _reservations.TryGetValue(attack, out Reservation reservation) && !reservation.Discarded && reservation.Run != null;
        }

        internal void AdvanceTime(float deltaTime)
        {
            if (CombatPresentationValueRules.IsFinite(deltaTime) && deltaTime > 0f)
                _time += deltaTime;
        }

        /// <summary>예약이 없으면 기존 경로입니다. 예약이 있으면 남은 지연을 포함해 같은 실행의 완료만 기다립니다.</summary>
        internal bool TryJoin(CombatAttackEvent attack, Action completed, int version)
        {
            _entered.Add(attack);
            if (!_reservations.TryGetValue(attack, out var reservation)) return false;
            if (version != reservation.Key.Generation || version != _generation()) return false;
            if (reservation.JoinRequested)
                throw new InvalidOperationException("공격 사건은 조기 실행에 한 번만 합류할 수 있습니다.");
            reservation.JoinRequested = true;
            reservation.Completed = completed ?? throw new ArgumentNullException(nameof(completed));
            if (reservation.Discarded)
                Complete(reservation);
            else
                Flush();
            return true;
        }

        /// <summary>사건 시작 처리가 끝난 뒤 또는 기존 재생 Tick 뒤 호출합니다. 통지 콜백 안에서 실행을 재진입하지 않습니다.</summary>
        internal void Flush()
        {
            if (_flushing || _result == null) return;
            _flushing = true;
            int version = _generation();
            try
            {
                for (int i = 0; i < _active.Count && version == _generation();)
                {
                    Reservation reservation = _active[i];
                    if (!reservation.Discarded && !_isAlive(reservation.Event.Attack.AttackerId))
                        Discard(reservation);
                    if (version != _generation()) return;
                    if (!reservation.Discarded && reservation.Run == null && _time >= reservation.Due)
                    {
                        bool blocked = _pieces.HasActionRun || _attacks.HasSource || _attacks.IsPlaying ||
                            reservation.WaitForEntry && !reservation.JoinRequested;
                        if (!blocked)
                        {
                            if (_start.TryBeginDeferred(reservation.Shape, reservation.Source, version,
                                _ => Discard(reservation), out CombatPieceRun run))
                                reservation.Run = run;
                            else
                                blocked = true;
                        }
                        if (blocked && !reservation.DelayWarned)
                        {
                            reservation.DelayWarned = true;
                            Debug.LogWarning($"[CombatPreAttackPlayback] 선행 처리/대여 반환을 기다려 공격 클립 시작을 늦춥니다. 결과 {reservation.Key.Result.Sequence}, 공격 사건 {reservation.Key.EventIndex}.");
                        }
                    }
                    if (version != _generation()) return;
                    if (!reservation.Discarded && reservation.JoinRequested && !reservation.Joined && reservation.Run != null &&
                        !_pieces.HasActionRun && !_attacks.HasSource && !_attacks.IsPlaying)
                    {
                        _pieces.JoinDeferredAttack(reservation.Run);
                        reservation.Joined = true;
                        reservation.Source.Join(() => Complete(reservation));
                    }
                    if (version != _generation()) return;
                    if (reservation.Discarded || reservation.EventCompleted && reservation.Run?.IsActive != true)
                        _active.RemoveAt(i);
                    else
                        i++;
                }
            }
            finally
            {
                _flushing = false;
            }
        }

        /// <summary>사망/필드 제거 사실을 그리기 전에 조기 실행의 대여를 돌려줍니다.</summary>
        internal void BeforeEvent(CombatEvent item)
        {
            if (item is not CombatCharacterEvent character || character.Final == null ||
                !character.Final.Health.IsDead && character.Final.Area == CharacterRuntimeArea.Field) return;
            int version = _generation();
            for (int i = 0; i < _active.Count && version == _generation(); i++)
                if (_active[i].Event.Attack.AttackerId == character.Final.EntityId)
                    Discard(_active[i]);
        }

        internal void Cancel()
        {
            _result = null;
            List<Exception> errors = null;
            foreach (Reservation reservation in _active)
            {
                reservation.Discarded = true;
                reservation.Completed = null;
                if (reservation.Run != null)
                    CleanupErrors.Try(reservation.Run.Cancel, ref errors);
            }
            _active.Clear();
            _reservations.Clear();
            _entered.Clear();
            CleanupErrors.ThrowIfAny(errors, "조기 공격 실행 정리에 실패했습니다.");
        }

        private void Discard(Reservation reservation)
        {
            if (reservation.Discarded) return;
            reservation.Discarded = true;
            List<Exception> errors = null;
            if (reservation.Run != null)
                CleanupErrors.Try(reservation.Run.Cancel, ref errors);
            CleanupErrors.Try(() => reservation.Source.AbortSource(reservation.Event), ref errors);
            CleanupErrors.ThrowIfAny(errors, "조기 공격 예약 취소에 실패했습니다.");
            if (reservation.JoinRequested)
                Complete(reservation);
        }

        private void Complete(Reservation reservation)
        {
            if (reservation.EventCompleted) return;
            reservation.EventCompleted = true;
            Action completed = reservation.Completed;
            reservation.Completed = null;
            if (ReferenceEquals(_result, reservation.Key.Result) && reservation.Key.Generation == _generation())
                completed?.Invoke();
        }

        private bool HasPrecedingDependency(CombatPreAttackLead lead, CombatAttackEvent attack, bool leadPending)
        {
            // 연출이 시작된 사건 앞의 발동·사실 처리는 이미 끝났으므로 그 사건 뒤만 확인합니다(끌어오기면 이동 사건 뒤).
            // 앞당김은 그 사건을 붙잡기 전에 예약하므로 그 사건 자신부터 봅니다.
            for (int i = leadPending ? lead.EventIndex : lead.EventIndex + 1; i < lead.Attack.EventIndex; i++)
            {
                CombatEvent item = lead.Attack.Result.Events[i];
                if (CombatEventTypeTable.IsActionStart(item) || CombatEventTypeTable.OpensAttackSource(item) ||
                    item is CombatCharacterEvent character && character.Final?.EntityId == attack.Attack.AttackerId ||
                    item is CombatEffectsEvent effects && _mayUseUnitAction(effects, attack.Attack.AttackerId))
                    return true;
            }
            return false;
        }
    }
}
