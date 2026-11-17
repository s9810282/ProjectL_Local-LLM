#if UNITY_EDITOR || DEVELOPMENT_BUILD
using CombatInputProfiler = global::ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.CombatInputProfiler;
#endif
using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.PlayerSkill.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Assist;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction
{
    /// <summary>
    /// 플레이어 액티브 스킬 모드의 입력 소유자입니다(전투 스킬 View 설계 SK1). 모든 스킬이 같은 단계를 지납니다:
    /// 가용성 → 선택(점 목록) → 남은 점 수에 따른 좌클릭(점 추가·마지막 점 격발·영역 격발) → 예보 → 제출.
    /// </summary>
    /// <remarks>
    /// <para>View는 스킬 종류를 모릅니다(설계 §2). 무엇을 고를 수 있는지·다 골랐는지·쓸 수 있는지는 Core 답이고, 좌클릭 갈래는 선택 답의
    /// 남은 점 수(<c>RequiredPointCount − SelectedPoints.Count</c>) 하나로 정합니다(계획 TP-8, 설계 P17). 점이 없는 스킬도 선택 요청을 한 번 거칩니다(P1).</para>
    /// <para>행위자는 선택 유닛이 아니라 스킬 상태의 행위자(메인)입니다(P2). 요청은 미리보기 슬롯 하나를 쓰므로 마지막 요청 하나만 기억하고,
    /// 답은 번호·답 종류·출처(전투·턴·조회 판·행위자)·점 목록을 모두 대조한 뒤에만 씁니다. 제출은 그 예보의 출처를 그대로 싣습니다(Core 인계 §4).</para>
    /// <para>예보가 오기 전의 격발은 버리지 않고 그 점 목록의 예보를 물어 답이 오면 제출합니다(격발 대기, 설계 P14·계획 TP-9). 결과 뒤 수락이면 모드를 끝내고,
    /// 거절(낡은 출처 포함)이면 가용성부터 다시 묻습니다(P8).</para>
    /// </remarks>
    internal sealed class CombatPlayerSkillInteraction
    {
        private enum RequestKind
        {
            None,
            Availability,
            Selection,
            Forecast
        }

        private readonly ICombatViewBufferWriteSink _input;
        private readonly ICombatViewBufferReadSink _requests;
        private readonly ICombatStateBufferReadSink _state;
        private readonly ICombatPointerPicker _picker;
        private readonly ICombatPlayerSkillFeedback _feedback;
        private readonly CombatHoverTracker _hover;
        private readonly CombatCommandGate _gate;
        private readonly ICombatActionRejectionSink _notices;
        private readonly IPlayerSkillVisualTableQuery _visuals;
        private readonly CombatInputSounds _sounds;

        private bool _active;
        private int _generation;
        private EntityId _actor;
        private long _battle;
        private long _turn;
        private PlayerSkillVisualLook _look;

        // 슬롯의 마지막 자기 요청입니다. 슬롯에는 요청이 하나뿐이라 새 요청은 옛 요청을 대신합니다.
        private int _request;
        private RequestKind _requestKind;
        private bool _requestAnswered;
        private readonly List<CellCoord> _requestPoints = new();

        // 확정한 점과 마지막 선택 답입니다.
        private readonly List<CellCoord> _points = new();
        private bool _selectionAnswered;
        private int _remaining;
        private readonly List<CellCoord> _candidates = new();
        private readonly HashSet<CellCoord> _candidateSet = new();

        // 그려진 예보입니다. 격발은 이 예보의 출처를 싣습니다.
        private PlayerSkillForecast _forecast;
        private readonly List<CellCoord> _forecastPoints = new();
        private readonly CombatPlayerSkillForecastView _forecastView = new();

        // 격발 대기: 그 점 목록(요청 점 목록)의 예보 답이 오면 제출합니다.
        private bool _fireIntent;
        // 이 모드가 낸 명령 번호와 수락 여부입니다. 결과 후처리에서 모드를 끝낼지 정합니다.
        private int _submitted;
        private bool _accepted;

        // 요청할 점 목록 작업 칸입니다(호버 예보마다 새 목록을 만들지 않음).
        private readonly List<CellCoord> _scratch = new();

        internal CombatPlayerSkillInteraction(ICombatViewBufferWriteSink input, ICombatViewBufferReadSink requests,
            ICombatStateBufferReadSink state, ICombatPointerPicker picker, ICombatPlayerSkillFeedback feedback,
            ICombatHoverCellFeedback hover, CombatCommandGate gate, ICombatActionRejectionSink notices,
            IPlayerSkillVisualTableQuery visuals, CombatInputSounds sounds)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _requests = requests ?? throw new ArgumentNullException(nameof(requests));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _picker = picker ?? throw new ArgumentNullException(nameof(picker));
            _feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));
            _hover = new CombatHoverTracker(hover ?? throw new ArgumentNullException(nameof(hover)), CombatHoverKind.Attack);
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
            _notices = notices ?? throw new ArgumentNullException(nameof(notices));
            _visuals = visuals ?? throw new ArgumentNullException(nameof(visuals));
            _sounds = sounds ?? throw new ArgumentNullException(nameof(sounds));
            InputOwner = new OwnerAdapter(this);
        }

        /// <summary>입력 기능 묶음의 공통 수명에 참여하는 창구입니다.</summary>
        internal CombatInputOwner InputOwner { get; }

        /// <summary>스킬 모드인지입니다.</summary>
        internal bool IsActive => _active;

        /// <summary>
        /// 스킬 모드에 들어갈 수 있는지입니다. 스킬 상태가 있고(메인에 스킬이 묶임), 고른 유닛이 그 메인이고(2026-11-17 사용자, 계획 §6.6),
        /// 버튼의 배경 가용성 조회와 같은 안정 상태(재생 아님·처리 안 된 명령 없음)여야 합니다(계획 TP-10, 키와 버튼이 같은 조건 — 묶음 끝 검토 M2).
        /// 실제로 쓸 수 있는지는 들어간 뒤 가용성 답이 정합니다.
        /// </summary>
        internal bool CanEnter(in CombatInputFacts facts, out EntityId actor)
        {
            actor = ActorOf(_state);
            return !actor.IsEmpty && facts.SelectedActorId == actor && CombatInputPolicy.CanQuerySkillAvailability(facts)
                && CombatInputPolicy.CanPreview(facts, _gate.HasPending, CombatViewPreviewKind.SkillAvailability);
        }

        /// <summary>실제 진입 시도에서만 부릅니다. 스킬이 묶여 있지 않으면 안내합니다. 다른 이유(메인을 고르지 않음·입력 막힘 등)는 조용히 지나갑니다(공격 키와 같이).</summary>
        internal void NotifyEntryRejected(in CombatInputFacts facts)
        {
            if (ActorOf(_state).IsEmpty && facts.HasBattle && facts.StagePhase == StagePhase.Combat)
                _notices.SkillRejected(PlayerSkillFailureReason.NoSkillConfigured);
        }

        /// <summary>이전 모드를 정리하고 새 행위자로 모드에 들어가 가용성을 묻습니다. 다른 소유자 정리는 호출자(<c>CombatInteraction</c>)가 먼저 합니다.</summary>
        internal void Enter(in CombatInputFacts facts, EntityId actor)
        {
            int generation = ++_generation;
            Exit(generation);
            if (generation != _generation)
                return;
            PlayerSkillBattleState skill = _state.HasBattle ? _state.Current.CommandState.PlayerSkill : null;
            if (skill == null)
                return;
            // 외형 줄은 전투 조립 때 모두 있는지 확인했습니다(CombatDisplayTables).
            _look = _visuals.Get(skill.Definition.Key, skill.UpgradeKey);
            _active = true;
            _actor = actor;
            _battle = facts.BattleInstanceId;
            _turn = facts.ControllerTurnSequence;
            RequestAvailability(facts);
        }

        /// <summary>모드를 끝냅니다(취소 효과음). R·스킬 버튼·점이 없을 때의 우클릭이 부릅니다.</summary>
        internal void Cancel(IReadOnlyList<CharacterData> characters)
        {
            if (!_active)
                return;
            CharacterData caster = characters == null ? null : CombatCharacterQueries.Find(characters, _actor);
            if (caster != null)
                _sounds.Play(UnitEvent.UnitCancel, _sounds.PresetOf(caster));
            Exit();
        }

        /// <summary>
        /// 우클릭입니다: 마지막 점 하나를 빼고 선택을 다시 묻습니다. 점이 없으면 모드를 끝냅니다(모든 스킬 같음, 설계 SK1).
        /// 제출한 뒤와 격발 대기 중에는 받지 않습니다(계획 TP-9 — 대기 중 우클릭이 이미 확정한 점을 빼지 않게). 그때 모드를 끝내는 것은 R·스킬 버튼입니다.
        /// </summary>
        internal void Back(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters)
        {
            if (!_active || _submitted != 0 || _fireIntent)
                return;
            if (_points.Count == 0)
            {
                Cancel(characters);
                return;
            }
            _points.RemoveAt(_points.Count - 1);
            ClearForecast();
            RequestSelection(facts);
        }

        internal void PointerMoved(in CombatInputFacts facts, float screenX, float screenY)
        {
            if (!_active)
                return;
            if (!_picker.TryPickCell(screenX, screenY, out CellCoord cell))
            {
                _hover.Clear();
                if (CanAim && _remaining == 1)
                    LeaveHoverForecast();
                return;
            }
            if (_hover.HasHover && _hover.Cell == cell)
                return;

            _hover.Set(cell, StateOf(cell));
            // 남은 점 1개일 때만 호버로 예보를 묻습니다(설계 P15). 2개 이상은 점을 찍어야 다음 범위가 나오고, 0개는 예보가 포인터와 무관합니다.
            if (!CanAim || _remaining != 1)
                return;
            if (_candidateSet.Contains(cell))
                RequestHoverForecast(facts, cell);
            else
                LeaveHoverForecast();
        }

        /// <summary>좌클릭 뗌입니다(계획 TP-11). 갈래는 남은 점 수 하나로 정합니다(Q-SK2, 계획 TP-8).</summary>
        internal void SelectReleased(in CombatInputFacts facts, float screenX, float screenY)
        {
            if (!_active || !CanAim || !_picker.TryPickCell(screenX, screenY, out CellCoord cell))
                return;

            if (_remaining >= 2)
            {
                if (!_candidateSet.Contains(cell))
                    return;
                _points.Add(cell);
                ClearForecast();
                RequestSelection(facts);
                return;
            }

            if (_remaining == 1)
            {
                if (!_candidateSet.Contains(cell))
                    return;
                _scratch.Clear();
                _scratch.AddRange(_points);
                _scratch.Add(cell);
                Fire(facts, _scratch);
                return;
            }

            // 남은 점 0: 그려진 영향 칸 안이면 격발합니다(설계 P13). 예보가 오기 전에는 영역을 몰라 무시합니다.
            if (_forecast != null && _forecastView.Contains(cell))
                Fire(facts, _points);
        }

        /// <summary>자기 요청의 답이면 반영하고 true입니다. 번호·답 종류·출처·점 목록이 모두 맞을 때만 씁니다.</summary>
        internal bool PreviewAnswered(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters)
        {
            CombatPreviewResult answer = facts.PreviewAnswer;
            if (!_active || answer == null || _request == 0 || _requestAnswered || answer.RequestNumber != _request
                || !_requests.HasPreview || _requests.Preview.Number != answer.RequestNumber)
                return false;

            _requestAnswered = true;
            switch (_requestKind)
            {
                case RequestKind.Availability:
                    ApplyAvailability(facts, answer.SkillAvailability);
                    break;
                case RequestKind.Selection:
                    ApplySelection(facts, answer.SkillSelection);
                    break;
                case RequestKind.Forecast:
                    ApplyForecast(facts, characters, answer.SkillForecast);
                    break;
            }
            return true;
        }

        /// <summary>결과 재생 전입니다. 예보·선택 그림을 지웁니다(실제 비행·폭발 위에 겹치지 않게). 모드는 결과 후처리가 정합니다.</summary>
        internal void BeforeResult()
        {
            if (!_active)
                return;
            ClearForecast();
            _feedback.ClearSkillSelection();
        }

        /// <summary>표시가 끝난 결과를 받습니다. 이 모드가 낸 명령이면 수락 여부를 기억합니다.</summary>
        internal void CommandResolved(CombatResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (!_active || _submitted == 0 || result.Kind != CombatProcessingKind.Command || result.CommandNumber != _submitted)
                return;
            _submitted = 0;
            _accepted = result.Accepted;
        }

        /// <summary>결과 후처리입니다. 수락이거나 모드가 맞지 않으면 끝내고, 아니면 가용성부터 다시 묻습니다(설계 P8).</summary>
        internal void AfterResult(in CombatInputFacts facts)
        {
            if (!_active)
                return;
            if (_accepted || !IsModeStillValid(facts))
            {
                Exit();
                return;
            }
            ResetSelection();
            RequestAvailability(facts);
        }

        /// <summary>스킬 모드·요청·그림을 정리합니다. 모드가 아니면 아무것도 하지 않습니다.</summary>
        internal void Exit() => Exit(++_generation);

        private void Exit(int generation)
        {
            if (!_active)
                return;
            _active = false;
            _actor = EntityId.Empty;
            _look = default;
            _submitted = 0;
            _accepted = false;
            // 외부 표시 콜백 전에 옛 요청을 놓습니다.
            if (_request != 0)
                _input.ClearPreview(_request);
            _request = 0;
            _requestKind = RequestKind.None;
            _requestAnswered = false;
            _requestPoints.Clear();
            CleanupErrors.RunAll("[CombatPlayerSkillInteraction] 스킬 모드 정리 중 실패했습니다.",
                () =>
                {
                    if (generation == _generation)
                        ResetSelection();
                },
                () =>
                {
                    if (generation == _generation)
                        _hover.Clear();
                });
        }

        // 선택·예보·격발 상태와 그 그림을 비웁니다(요청 번호는 그대로).
        private void ResetSelection()
        {
            _fireIntent = false;
            _points.Clear();
            _selectionAnswered = false;
            _remaining = 0;
            _candidates.Clear();
            _candidateSet.Clear();
            ClearForecast();
            _feedback.ClearSkillSelection();
        }

        private void ApplyAvailability(in CombatInputFacts facts, PlayerSkillAvailability availability)
        {
            if (availability == null || !IsCurrent(availability.Source, facts))
            {
                Exit();
                return;
            }
            if (!availability.CanUse)
            {
                _notices.SkillRejected(availability.FailureReason);
                Exit();
                return;
            }
            RequestSelection(facts);
        }

        private void ApplySelection(in CombatInputFacts facts, PlayerSkillSelectionResult selection)
        {
            if (selection == null || !IsCurrent(selection.Source, facts) || !SamePoints(selection.SelectedPoints, _points))
                return;
            // 후보에서만 점을 고르므로 앞 점이 틀리면 사실이 바뀐 것입니다. 마지막 점을 빼고 다시 묻고, 점이 없으면 이유를 안내하고 끝냅니다.
            if (!selection.PrefixIsValid)
            {
                if (_points.Count == 0)
                {
                    _notices.SkillRejected(selection.FailureReason);
                    Exit();
                    return;
                }
                _points.RemoveAt(_points.Count - 1);
                RequestSelection(facts);
                return;
            }

            _selectionAnswered = true;
            _remaining = selection.RequiredPointCount - selection.SelectedPoints.Count;
            _candidates.Clear();
            _candidateSet.Clear();
            for (int i = 0; i < selection.NextCandidateCells.Count; i++)
            {
                CellCoord cell = selection.NextCandidateCells[i];
                if (_candidateSet.Add(cell))
                    _candidates.Add(cell);
            }
            _feedback.ShowSkillSelection(_actor, _candidates, _points, _look.Attack);
            if (_hover.HasHover)
                _hover.SetStateIfHovered(StateOf(_hover.Cell));

            if (_remaining <= 0)
                RequestForecast(facts, _points, fire: false);
            else if (_remaining == 1 && _hover.HasHover && _candidateSet.Contains(_hover.Cell))
                RequestHoverForecast(facts, _hover.Cell);
        }

        private void ApplyForecast(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, PlayerSkillForecast forecast)
        {
            bool fire = _fireIntent;
            _fireIntent = false;
            if (forecast == null || !IsCurrent(forecast.Source, facts) || !SamePoints(forecast.SelectedPoints, _requestPoints))
                return;
            if (!forecast.CanUse)
            {
                ClearForecast();
                // 호버 예보의 거절은 그 칸이 안 된다는 뜻이라 조용히 지웁니다. 누른 격발과 영역 예보의 거절은 이유를 안내합니다.
                if (fire || _remaining <= 0)
                    _notices.SkillRejected(forecast.FailureReason);
                return;
            }

            _forecastView.Read(forecast, characters);
            _forecast = forecast;
            _forecastPoints.Clear();
            _forecastPoints.AddRange(_requestPoints);
            _feedback.ShowSkillForecast(_actor, _forecastView, _look.Attack);
            if (fire)
                Submit(facts, forecast, _forecastPoints);
        }

        // 그 점 목록의 지금 예보가 있으면 바로 제출하고, 없으면 그 점 목록의 예보를 묻고 답이 오면 제출합니다(격발 대기).
        private void Fire(in CombatInputFacts facts, IReadOnlyList<CellCoord> points)
        {
            if (_forecast != null && SamePoints(_forecastPoints, points) && IsCurrent(_forecast.Source, facts))
            {
                Submit(facts, _forecast, points);
                return;
            }
            // 같은 점 목록의 예보를 이미 묻는 중이면(호버 예보) 그 답을 기다려 제출합니다.
            if (_requestKind == RequestKind.Forecast && !_requestAnswered && SamePoints(_requestPoints, points))
            {
                _fireIntent = true;
                return;
            }
            RequestForecast(facts, points, fire: true);
        }

        private void Submit(in CombatInputFacts facts, PlayerSkillForecast forecast, IReadOnlyList<CellCoord> points)
        {
            int number = _gate.TrySubmit(facts, CombatViewCommandData.ForPlayerSkill(_actor, points, forecast.Source));
            if (number > 0)
                _submitted = number;
        }

        private void RequestAvailability(in CombatInputFacts facts)
        {
            _requestPoints.Clear();
            SetRequest(facts, CombatViewPreviewData.ForPlayerSkillAvailability(_actor), RequestKind.Availability);
        }

        private void RequestSelection(in CombatInputFacts facts)
        {
            _selectionAnswered = false;
            _requestPoints.Clear();
            _requestPoints.AddRange(_points);
            SetRequest(facts, CombatViewPreviewData.ForPlayerSkillSelection(_actor, _requestPoints), RequestKind.Selection);
        }

        // 남은 점 1개의 호버 예보입니다. 같은 점 목록을 이미 물었거나 그렸으면 다시 묻지 않습니다(칸이 바뀔 때만).
        private void RequestHoverForecast(in CombatInputFacts facts, CellCoord cell)
        {
            _scratch.Clear();
            _scratch.AddRange(_points);
            _scratch.Add(cell);
            if (_forecast != null && SamePoints(_forecastPoints, _scratch))
                return;
            if (_requestKind == RequestKind.Forecast && !_requestAnswered && SamePoints(_requestPoints, _scratch))
                return;
            ClearForecast();
            RequestForecast(facts, _scratch, fire: false);
        }

        private void RequestForecast(in CombatInputFacts facts, IReadOnlyList<CellCoord> points, bool fire)
        {
            if (!ReferenceEquals(points, _requestPoints))
            {
                _requestPoints.Clear();
                _requestPoints.AddRange(points);
            }
            if (SetRequest(facts, CombatViewPreviewData.ForPlayerSkillForecast(_actor, _requestPoints), RequestKind.Forecast))
                _fireIntent = fire;
        }

        // 요청을 슬롯에 씁니다. 지금 물을 수 없으면 쓰지 않고 false입니다(결과 재생 뒤 결과 후처리가 다시 시작함).
        private bool SetRequest(in CombatInputFacts facts, CombatViewPreviewData preview, RequestKind kind)
        {
            if (!CombatInputPolicy.CanPreview(facts, _gate.HasPending, preview.Kind))
                return false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _request = CombatInputProfiler.SetPreview(_input, preview);
#else
            _request = _input.SetPreview(preview);
#endif
            _requestKind = kind;
            _requestAnswered = false;
            return true;
        }

        // 남은 점 1개에서 포인터가 후보를 떠났습니다. 그린 예보와 함께, 아직 답이 없는 호버 예보 요청도 놓습니다
        // (늦게 온 지나간 칸의 예보가 포인터 없는 칸 위에 그려지지 않게 — 설계 SK1, 묶음 끝 검토 M1). 격발 대기의 요청은 놓지 않습니다.
        private void LeaveHoverForecast()
        {
            ClearForecast();
            if (_request == 0 || _requestKind != RequestKind.Forecast || _requestAnswered || _fireIntent)
                return;
            int number = _request;
            _request = 0;
            _requestKind = RequestKind.None;
            _requestPoints.Clear();
            _input.ClearPreview(number);
        }

        // 그린 예보를 지웁니다. 그림이 없으면 아무것도 하지 않습니다.
        private void ClearForecast()
        {
            if (_forecast == null)
                return;
            _forecast = null;
            _forecastPoints.Clear();
            _forecastView.Clear();
            _feedback.ClearSkillForecast();
        }

        // 고를 수 있는 상태인지입니다: 선택 답을 받았고, 격발을 기다리지 않고, 제출한 결과를 기다리지 않습니다.
        private bool CanAim => _selectionAnswered && !_fireIntent && _submitted == 0;

        private CombatHoverState StateOf(CellCoord cell)
        {
            if (!_selectionAnswered)
                return CombatHoverState.Neutral;
            if (_remaining <= 0)
                return _forecastView.Contains(cell) ? CombatHoverState.Valid : CombatHoverState.Neutral;
            return _candidateSet.Contains(cell) ? CombatHoverState.Valid : CombatHoverState.Invalid;
        }

        private bool IsCurrent(PlayerSkillSourceStamp source, in CombatInputFacts facts) =>
            IsSource(source, _actor, facts.BattleInstanceId, facts.ControllerTurnSequence, facts.QueryRevision);

        private bool IsModeStillValid(in CombatInputFacts facts) =>
            facts.CurrentLayer == CombatInputPolicy.LayerOf(StagePhase.Combat)
            && !_actor.IsEmpty && ActorOf(_state) == _actor
            && facts.BattleInstanceId == _battle && facts.ControllerTurnSequence == _turn;

        /// <summary>
        /// 스킬 행위자는 선택 유닛이 아니라 스킬 상태의 행위자(메인)입니다(설계 P2). 스킬이 묶여 있지 않으면 Empty입니다.
        /// 스킬 모드와 버튼 배경 조회(<see cref="CombatPlayerSkillAvailabilityTracker"/>)가 같이 씁니다.
        /// </summary>
        internal static EntityId ActorOf(ICombatStateBufferReadSink state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (!state.HasBattle)
                return EntityId.Empty;
            return state.Current.CommandState.PlayerSkill?.ActorId ?? EntityId.Empty;
        }

        /// <summary>스킬 답의 출처가 그 행위자·전투·턴·조회 판의 것인지입니다(Core 인계 §4의 네 칸). 스킬 모드와 버튼 배경 조회가 같이 씁니다.</summary>
        internal static bool IsSource(PlayerSkillSourceStamp source, EntityId actor, long battle, long turn, long queryRevision) =>
            source.ActorId == actor && source.BattleInstanceId == battle
            && source.ControllerTurnSequence == turn && source.QueryRevision == queryRevision;

        private static bool SamePoints(IReadOnlyList<CellCoord> left, IReadOnlyList<CellCoord> right)
        {
            if (left.Count != right.Count)
                return false;
            for (int i = 0; i < left.Count; i++)
            {
                if (left[i] != right[i])
                    return false;
            }
            return true;
        }

        /// <summary>공통 수명의 단계마다 스킬 모드의 어느 메서드인지만 답합니다. 점·격발은 뗄 때라 누름에는 할 일이 없습니다.</summary>
        private sealed class OwnerAdapter : CombatInputOwner
        {
            private readonly CombatPlayerSkillInteraction _skill;

            internal OwnerAdapter(CombatPlayerSkillInteraction skill) => _skill = skill;

            internal override CombatInputRouter.PointerTarget Target => CombatInputRouter.PointerTarget.Skill;
            internal override bool IsActive => _skill.IsActive;

            internal override void PointerMoved(in CombatPointerInput input) =>
                _skill.PointerMoved(input.Facts, input.ScreenX, input.ScreenY);

            internal override void SelectReleased(in CombatPointerInput input) =>
                _skill.SelectReleased(input.Facts, input.ScreenX, input.ScreenY);

            internal override bool PreviewAnswered(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters) =>
                _skill.PreviewAnswered(facts, characters);

            internal override void BeforeResult() => _skill.BeforeResult();

            internal override void AfterResult(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters) =>
                _skill.AfterResult(facts);

            internal override void CancelPresentation() => _skill.Exit();
        }
    }
}
