using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks
{
    /// <summary>게시 사건의 경고·경로·접촉 근거를 검사한다. 피해·도착 처리는 다시 실행하지 않는다.</summary>
    public static class LavaChainGimmickEvidenceValidator
    {
        public static void Validate(CombatCommandTransition transition)
        {
            if (!transition.Events.Any(e => e is CombatLavaChainEvent or CombatLavaChainContactEvent)) return;
            var initial = transition.Input.State.Gimmicks.Entries.OfType<LavaChainGimmickState>().SingleOrDefault();
            Require(initial != null, "없는 사슬의 사건");
            var spec = transition.Input.Battle?.GimmickDefinitions[initial.Id.Value - 1] as LavaChainGimmickSpec;
            Require(spec != null, "사슬 정의");
            new Validation(transition, initial, spec).Run();
        }

        private sealed class Validation
        {
            private readonly CombatCommandTransition _transition;
            private readonly LavaChainGimmickState _initial;
            private readonly LavaChainGimmickSpec _spec;
            private readonly GimmickEvidenceWorld _world;
            private readonly List<CellCoord> _path;
            private readonly HashSet<CellCoord> _reservations;
            private readonly Queue<EntityId> _expectedVictims = new();
            private HashSet<EntityId> _defeatRecipients;
            private CombatCommandState _state;
            private readonly EnvironmentRandomTrace _random;
            private CombatLavaChainEvent _warning;
            private CombatLavaChainContactEvent _pending;
            private int _contactCount;
            private bool _pushStarted;
            private bool _pushInterrupted;
            private CellCoord _pushCell;
            private CellCoord _pushStep;
            private CellCoord? _attempted;
            private bool _stopped;
            private bool _resolved;
            private int _advanceCount;

            internal Validation(CombatCommandTransition transition, LavaChainGimmickState initial, LavaChainGimmickSpec spec)
            {
                _transition = transition;
                _initial = initial;
                _spec = spec;
                _world = new GimmickEvidenceWorld(transition.Input);
                _state = transition.Input.State;
                _random = new EnvironmentRandomTrace(_state.EnvironmentProgress.RandomState);
                _path = new List<CellCoord> { initial.HeadCell };
                _reservations = new HashSet<CellCoord> { initial.HeadCell };
            }

            internal void Run()
            {
                foreach (var occurrence in _transition.Events)
                {
                    if (occurrence is CombatLavaChainContactEvent contact) ValidateContact(contact);
                    else if (occurrence is CombatLavaChainEvent change) ValidateAdvance(change);
                    ObservePush(occurrence);
                    ObserveContext(occurrence);
                    _world.Apply(occurrence);
                }
                Require(_advanceCount == 1 && _pending == null && !_attempted.HasValue, "완료되지 않은 사슬 접촉");
                // 경고 뒤에 난수를 쓴 사건이 없으면 전이의 최종 난수는 경고가 남긴 값이다.
                Require(_warning == null || _random.EndsWith(_warning, _transition.FinalState.EnvironmentProgress.RandomState),
                    "경고 뒤 최종 난수");
            }

            // 사건 직전의 판: 턴은 사건으로 따라가고, 기믹 점유는 공용 공간 투영이 따라간다.
            private CombatCommandState Current => _state.With(gimmicks: _world.Gimmicks);

            private void ValidateContact(CombatLavaChainContactEvent contact)
            {
                FinishContact();
                Require(!_resolved && _initial.IsWarning && _initial.Heading.HasValue && contact.GimmickId == _initial.Id,
                    "경고된 사슬의 접촉");
                if (!_attempted.HasValue || contact.PathIndex != _path.Count)
                {
                    FinishStep();
                    AdvanceToContact(contact.PathIndex);
                }
                while (_expectedVictims.Count > 0 && _world.Characters[_expectedVictims.Peek()].Health.IsDead) _expectedVictims.Dequeue();
                Require(!_stopped && _attempted == contact.ContactCell && _expectedVictims.Count > 0 &&
                    _expectedVictims.Dequeue() == contact.RecipientId, "같은 단계에서 순서대로 한 번씩 처리한 접촉");
                var recipient = _world.Characters[contact.RecipientId];
                Require(recipient.Anchor == contact.From && recipient.OccupiedCells.Contains(contact.ContactCell), "접촉 직전 위치");
                var current = _world.CreateInput(_state);
                var landing = LavaChainGimmickRule.FindLanding(current, recipient, _initial.Heading.Value, _spec.PushDistance,
                    _reservations, out var failure);
                Require(landing == contact.LandingCell && failure == contact.Failure, "수직 밀림 우선순위와 전체 모양의 착지");
                _pending = contact;
                _contactCount++;
                _pushStarted = false;
                _pushInterrupted = false;
                _pushCell = contact.From;
                if (landing.HasValue) _pushStep = new CellCoord(Math.Sign(landing.Value.X - _pushCell.X), Math.Sign(landing.Value.Y - _pushCell.Y));
            }

            private void ValidateAdvance(CombatLavaChainEvent change)
            {
                FinishContact();
                _advanceCount++;
                Require(_advanceCount == 1 && change.Expected.SameValues(_initial) &&
                    change.Boundary.CompletedControllerTurnCount == _initial.NextBoundaryCount &&
                    _transition.Events.OfType<CombatTurnEvent>().Any(turn =>
                        (turn.Kind == CombatEventKind.PlayerTurnEnded && change.Boundary.EndedSide == Owner.Player ||
                         turn.Kind == CombatEventKind.EnemyPhaseEnded && change.Boundary.EndedSide == Owner.Enemy) &&
                        turn.Final.CompletedControllerTurnCount == change.Boundary.CompletedControllerTurnCount), "실제 종료 경계");
                change.Final.ValidateBoard(_state.BoardWidth, _state.BoardHeight);

                if (change.Step == LavaChainGimmickStep.Warning) ValidateWarning(change);
                else ValidateRetraction(change);
            }

            private void ValidateWarning(CombatLavaChainEvent change)
            {
                Require(!_initial.IsWarning && _contactCount == 0 && change.Final.IsWarning &&
                    change.Final.HeadCell == _initial.HeadCell && change.HeadPath.Count == 1 &&
                    change.Final.NextBoundaryCount == checked(_initial.NextBoundaryCount + _spec.WarningLeadControllerTurns), "경고 일정");
                uint random = change.ExpectedRandomState.Value;
                Require(_random.Matches(random), "경고 시작 난수");
                var selected = LavaChainGimmickRule.ChooseHeading(_state, _initial.HeadCell,
                    LavaChainGimmickRule.HardBlockers(Current, _world.Environment, _world.Bindings, _world.Structures.Values), ref random);
                Require(selected == change.Final.Heading && random == change.FinalRandomState, "경고 방향과 최종 난수");
                _warning = change;
            }

            private void ValidateRetraction(CombatLavaChainEvent change)
            {
                Require(_initial.IsWarning && !change.Final.IsWarning &&
                    change.Final.NextBoundaryCount == checked(_initial.NextBoundaryCount + _spec.CycleControllerTurns - _spec.WarningLeadControllerTurns),
                    "발동 뒤 일정");
                FinishStep();
                if (_initial.Heading.HasValue)
                {
                    var blocked = LavaChainGimmickRule.HardBlockers(Current, _world.Environment, _world.Bindings, _world.Structures.Values);
                    while (!_stopped)
                    {
                        var next = _path[_path.Count - 1] + _initial.Heading.Value;
                        if (!_state.IsInside(next) || blocked.Contains(next)) break;
                        Require(!LivingAt(next).Any(), "누락된 머리 접촉");
                        _path.Add(next);
                    }
                }
                Require(_path.SequenceEqual(change.HeadPath) && _path[_path.Count - 1] == change.Final.HeadCell, "머리 확장 순서와 수축 위치");
                Require(!LivingAt(change.Final.HeadCell).Any(), "최종 머리와 생존자 중첩");
                _resolved = true;
            }

            private void ObservePush(CombatEvent occurrence)
            {
                if (_pending?.LandingCell == null) return;
                if (occurrence is CombatCharacterEvent change && change.Final.EntityId == _pending.RecipientId)
                {
                    if (change.Kind is CombatEventKind.Moved or CombatEventKind.Relocated)
                    {
                        bool nextPush = change.Kind == CombatEventKind.Moved &&
                            change.MovementCause?.Agency == CombatMovementAgency.Forced &&
                            change.Expected.Anchor == _pushCell && change.Final.Anchor == _pushCell + _pushStep;
                        if (!_pushStarted) Require(nextPush, "접촉 직후 실제 수직 밀림");
                        if (nextPush && !_pushInterrupted && _pushCell != _pending.LandingCell)
                        {
                            _pushStarted = true;
                            _pushCell += _pushStep;
                        }
                        else _pushInterrupted = true;
                    }
                    else if (change.Kind == CombatEventKind.Died) _pushInterrupted = true;
                }
                else if (occurrence is CombatFallEvent fall && fall.EntityId == _pending.RecipientId)
                    _pushInterrupted = true;
            }

            private void FinishContact()
            {
                if (_pending == null) return;
                var actual = _world.Characters[_pending.RecipientId];
                Require(_pending.Survived == !actual.Health.IsDead && _pending.FinalCell == (actual.Health.IsDead ? null : actual.Anchor),
                    "접촉 결과와 실제 개체 위치·생존");
                if (_pending.Failure == LavaChainPushFailure.None)
                    Require(_pushStarted && (_pushCell == _pending.LandingCell || _pushInterrupted), "누락된 수직 밀림 사건");
                if (_pending.Failure != LavaChainPushFailure.None)
                    Require(DefeatRecipients.Contains(_pending.RecipientId), "즉사·재구성 체력 근거");
                _pending = null;
            }

            private void FinishStep()
            {
                if (!_attempted.HasValue) return;
                while (_expectedVictims.Count > 0 && _world.Characters[_expectedVictims.Peek()].Health.IsDead) _expectedVictims.Dequeue();
                Require(_expectedVictims.Count == 0, "누락된 같은 칸의 접촉");
                _stopped = LivingAt(_attempted.Value).Any();
                if (!_stopped) _path.Add(_attempted.Value);
                _attempted = null;
            }

            private void AdvanceToContact(int index)
            {
                Require(!_stopped && index >= _path.Count, "멈춘 머리 또는 역순 접촉");
                var blocked = LavaChainGimmickRule.HardBlockers(Current, _world.Environment, _world.Bindings, _world.Structures.Values);
                while (_path.Count <= index)
                {
                    var next = _path[_path.Count - 1] + _initial.Heading.Value;
                    Require(_state.IsInside(next) && !blocked.Contains(next), "머리의 차단물 통과");
                    _reservations.Add(next);
                    if (_path.Count == index)
                    {
                        _attempted = next;
                        foreach (var victim in LivingAt(next).OrderBy(c => c.EntityId)) _expectedVictims.Enqueue(victim.EntityId);
                        break;
                    }
                    Require(!LivingAt(next).Any(), "건너뛴 접촉");
                    _path.Add(next);
                }
            }

            private IEnumerable<CharacterCombatFacts> LivingAt(CellCoord cell) => _world.Characters.Values.Where(c =>
                !c.Health.IsDead && c.Area == CharacterRuntimeArea.Field && c.OccupiedCells.Contains(cell));

            // 실패한 접촉이 있을 때만 출처별 즉사 근거를 한 번 색인한다.
            private HashSet<EntityId> DefeatRecipients => _defeatRecipients ??= _transition.HealthExecutions
                .SelectMany(e => e.Steps).Where(step => step.Source.DamageCause == CombatDamageCause.InstantDefeat &&
                    step.Source.EffectOrigin is var origin && origin.HasValue &&
                    origin.Value.DefinitionKey == (int)StageGimmickKey.LavaChain && origin.Value.OwnerInstanceId == _initial.Id.Value)
                .Select(step => step.RecipientId).ToHashSet();

            // 사슬의 시간 근거는 여기서 따라가고, 기믹 점유는 공용 공간 투영이, 난수 흐름은 공용 추적이 따라간다.
            private void ObserveContext(CombatEvent occurrence)
            {
                if (occurrence is CombatTurnEvent change)
                    _state = _state.With(turn: change.Final, phase: change.Final.StagePhase == StagePhase.Deployment ||
                        change.Final.ControllerPhase == CombatControllerPhase.Player ? CombatFlowPhase.PlayerInput : CombatFlowPhase.Automatic);
                _random.Observe(occurrence);
            }
        }

        private static void Require(bool condition, string fact)
        {
            if (!condition) throw new InvalidOperationException("사슬 게시 근거가 잘못되었습니다. " + fact);
        }
    }
}
