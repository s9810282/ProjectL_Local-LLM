using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction
{
    /// <summary>
    /// 포인터 칸의 범위를 보이는 입력 기능입니다(호버 공격 범위 설계 UR5·§8 HZ4). 유닛이 우선이고, 유닛이 없으면 그 칸의 환경 위험(지뢰·화약통)입니다.
    /// 유닛은 아군·적·차례가 아닌 유닛 모두 대상이고, 이동 칸과 그 위 공격 범위 칸을 Core의 유닛 공격 범위 조회 응답만으로 그립니다
    /// (View 계산 없음, 호버 이동 범위 설계 EM2). 아군은 공격 모드와 같은
    /// 공격 범위 색, 적은 적 공격 범위 색입니다(설계 Q-UR3 B, 진영은 Buffer의 소유자). 환경 위험은 Core 공개 입구로 피해 칸을 바로 그리고
    /// 미리보기 슬롯을 쓰지 않습니다(<see cref="CombatHazardLookup"/>, 위험 범위 색).
    /// 이동 모드인 행위자는 호버하지 않습니다(설계 §9). 그 유닛의 이동 범위는 이동 모드 범위가 이미 그립니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 마지막 포인터 좌표, 원하는 유닛, 자기 요청 번호, 답을 받은 유닛, 그린 위험 칸, 건너뛴 이동 모드 행위자, 표시 중 여부만 소유합니다. 모두 표시 캐시이며 원본이 아닙니다.
    /// 다른 입력 소유자(배치·아이템 Drag, 공격 모드, 경로 지정)가 활성인지는 묶음(<see cref="CombatInteraction"/>)이 판단해 넘깁니다.
    /// 그 동안에는 표시·요청을 내려놓고(<see cref="Suspend"/>), 다시 비활성이 되면 마지막 포인터로 다시 판단합니다.
    /// </para>
    /// <para>
    /// 요청은 미리보기 슬롯의 배경 요청 규칙(<see cref="CombatPreviewSlotRule"/>)을 따릅니다. 답을 받지 않은 남의 요청이 있으면 기다렸다가
    /// 폴링에서 요청하고, 자기 응답을 받으면 슬롯을 비웁니다. 자기가 그렸을 때만 지우고, 슬롯은 최신 요청이 자기 번호일 때만 비웁니다.
    /// 포인터 콜백은 칸 집기·유닛 찾기·위험 찾기만 하고, 요청은 유닛이 바뀔 때만 냅니다. 같은 위험 칸 안에서는 다시 찾지 않습니다(위험 범위 계획 P7).
    /// 지뢰가 터지거나 화약통이 부서지는 일은 결과 재생에서만 생기고, 재생 전에 내려놓은 뒤 다시 집으므로 그린 위험 칸이 낡지 않습니다.
    /// </para>
    /// </remarks>
    internal sealed class CombatUnitRangeHover
    {
        private readonly ICombatViewBufferWriteSink _input;
        private readonly ICombatViewBufferReadSink _requests;
        private readonly ICombatStateBufferReadSink _state;
        private readonly ICombatPointerPicker _picker;
        private readonly ICombatPointerOcclusion _occlusion;
        private readonly ICombatUnitRangeFeedback _feedback;
        private readonly ICombatMoveModeReader _moveMode;
        private readonly CombatCommandGate _gate;

        private bool _hasPointer;
        private float _screenX;
        private float _screenY;
        // 다른 소유자 활성·결과 재생 뒤 마지막 포인터로 다시 판단해야 하는지입니다.
        private bool _needsPick;
        private EntityId _wanted;
        // 원하는 유닛의 색조입니다(아군·적, 설계 Q-UR3 B). 집을 때 Buffer의 소유자로 정합니다.
        private CombatRangeTone _wantedTone = CombatRangeTone.Ally;
        private int _requestNumber;
        private EntityId _requestUnit;
        // 지금 사실로 답을 받은 유닛입니다(성공·실패 모두). 같은 유닛이면 다시 요청하지 않습니다(R-b).
        private EntityId _answeredUnit;
        // 환경 위험의 피해 칸 작업 목록(재사용)과 마지막으로 위험을 찾은 유닛 없는 칸입니다(설계 §8 HZ4, 계획 P7). 찾은 결과가 없어도 같은 칸이면 다시 찾지 않습니다.
        private readonly List<CellCoord> _hazardCells = new(9);
        private bool _hazardChecked;
        private CellCoord _hazardCell;
        // 포인터 칸에 있었지만 이동 모드 행위자라 건너뛴 유닛입니다(설계 §9). 그 유닛이 이동 모드를 벗어나면 포인터가 그대로여도 다시 판단합니다.
        private EntityId _skippedActor;
        private bool _shown;

        internal CombatUnitRangeHover(
            ICombatViewBufferWriteSink input,
            ICombatViewBufferReadSink requests,
            ICombatStateBufferReadSink state,
            ICombatPointerPicker picker,
            ICombatPointerOcclusion occlusion,
            ICombatUnitRangeFeedback feedback,
            ICombatMoveModeReader moveMode,
            CombatCommandGate gate)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _requests = requests ?? throw new ArgumentNullException(nameof(requests));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _picker = picker ?? throw new ArgumentNullException(nameof(picker));
            _occlusion = occlusion ?? throw new ArgumentNullException(nameof(occlusion));
            _feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));
            _moveMode = moveMode ?? throw new ArgumentNullException(nameof(moveMode));
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
            InputOwner = new OwnerAdapter(this);
        }

        /// <summary>입력 기능 묶음의 공통 수명에 참여하는 창구입니다(View 2라운드 W5-1). 배경 소유자입니다.</summary>
        internal CombatInputOwner InputOwner { get; }

        /// <summary>호버 범위(유닛 공격 범위 또는 위험 피해 칸)를 그리고 있는지입니다(읽기 전용).</summary>
        internal bool IsShown => _shown;

        /// <summary>
        /// 포인터가 움직였습니다. 좌표는 언제나 기억하고, 다른 소유자가 활성이면(<paramref name="ownersIdle"/> false) 내려놓기만 합니다.
        /// </summary>
        internal void PointerMoved(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters,
            float screenX, float screenY, bool ownersIdle)
        {
            _hasPointer = true;
            _screenX = screenX;
            _screenY = screenY;
            if (!ownersIdle)
            {
                Suspend();
                return;
            }
            Pick(facts, characters);
        }

        /// <summary>
        /// 매 Tick 맞춥니다. 허용이 아니면 내려놓고, 내려놓은 뒤거나 이동 모드 행위자가 바뀌어 지난 판단이 낡았으면 마지막 포인터로 다시 판단하고,
        /// 원하는 유닛의 요청이 슬롯을 기다리던 중이면 슬롯이 한가할 때 요청합니다(R-a).
        /// </summary>
        internal void Poll(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, bool ownersIdle)
        {
            if (!ownersIdle || !CombatInputPolicy.CanInspectUnit(facts, _gate.HasPending))
            {
                Suspend();
                return;
            }
            if (_needsPick || IsMoveModeStale())
            {
                Pick(facts, characters);
                return;
            }
            TryRequest(facts);
        }

        /// <summary>
        /// Core가 현재 요청에 답했습니다(<see cref="CombatInputFacts.PreviewAnswer"/>). 자기 요청의 응답이면 반영하고 true입니다.
        /// 이동 칸은 아군·적 모두 응답대로 그리고(호버 이동 범위 설계 EM2·P4), 공격 칸은 성공일 때만 그 위에 그립니다.
        /// 공격 실패(기본 공격 불가 등)여도 이동 칸은 응답에 담겨 옵니다(P8). 둘 다 비면 그리지 않습니다. 어느 쪽이든 그 유닛은 답을 받은 것으로 둡니다.
        /// </summary>
        internal bool PreviewAnswered(in CombatInputFacts facts)
        {
            CombatPreviewResult answer = facts.PreviewAnswer;
            if (answer == null || _requestNumber == 0 || answer.RequestNumber != _requestNumber)
                return false;

            EntityId unit = _requestUnit;
            ClearOwnedRequest();
            if (!answer.UnitRange.HasValue || answer.UnitRange.Value.UnitId != unit || unit != _wanted
                || !CombatInputPolicy.CanInspectUnit(facts, _gate.HasPending) || unit == _moveMode.MoveModeActorId)
                return true;

            _answeredUnit = unit;
            var range = answer.UnitRange.Value;
            CellCoord[] attackCells = range.Succeeded ? range.RangeCells.ToArray() : Array.Empty<CellCoord>();
            CellCoord[] moveCells = range.MoveCells.ToArray();
            if (attackCells.Length > 0 || moveCells.Length > 0)
            {
                _feedback.ShowHoverRange(attackCells, moveCells, _wantedTone, _wanted);
                _shown = true;
            }
            else
            {
                Hide();
            }
            return true;
        }

        /// <summary>
        /// 표시와 자기 요청을 내려놓습니다. 포인터는 남기고, 다시 허용되면 마지막 포인터로 판단합니다
        /// (다른 소유자 활성·결과 재생 전·표현 정리). 내려놓을 것이 없으면 아무것도 하지 않습니다.
        /// </summary>
        internal void Suspend()
        {
            Release();
            _needsPick = _hasPointer;
        }

        /// <summary>내려놓고 포인터도 잊습니다(포커스 상실·강제 선택 종료·정지). 새 포인터 입력 전에는 다시 판단하지 않습니다.</summary>
        internal void Clear()
        {
            Release();
            _hasPointer = false;
            _needsPick = false;
        }

        private void Pick(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters)
        {
            _needsPick = false;
            if (!CombatInputPolicy.CanInspectUnit(facts, _gate.HasPending) || characters == null
                || _occlusion.IsPointerOverUi(_screenX, _screenY)
                || !_picker.TryPickCell(_screenX, _screenY, out CellCoord cell))
            {
                Release();
                return;
            }

            CharacterData unit = CombatCharacterQueries.FindAnyAt(characters, cell);
            if (unit == null)
            {
                PickHazard(cell);
                return;
            }
            if (unit.EntityId == _moveMode.MoveModeActorId)
            {
                Release();
                _skippedActor = unit.EntityId;
                return;
            }
            if (_hazardChecked || unit.EntityId != _wanted)
            {
                Release();
                _wanted = unit.EntityId;
                _wantedTone = unit.Owner == Owner.Enemy ? CombatRangeTone.Enemy : CombatRangeTone.Ally;
            }
            TryRequest(facts);
        }

        // 유닛이 없는 칸입니다. 같은 칸을 이미 찾았으면 그대로 두고, 아니면 앞의 표시·요청을 내려놓고 그 칸의 위험 피해 칸을 바로 그립니다.
        private void PickHazard(CellCoord cell)
        {
            if (_hazardChecked && cell == _hazardCell)
                return;
            Release();
            _hazardChecked = true;
            _hazardCell = cell;

            CombatStateData state = _state.HasBattle ? _state.Current : null;
            if (state == null || !CombatHazardLookup.Collect(state.Environment, state.EnvironmentBindings, state.Structures,
                    state.ObstacleContents, state.BoardWidth, state.BoardHeight, cell, _hazardCells))
                return;
            _feedback.ShowHoverRange(_hazardCells, Array.Empty<CellCoord>(), CombatRangeTone.Hazard);
            _shown = true;
        }

        private void TryRequest(in CombatInputFacts facts)
        {
            if (_wanted.IsEmpty)
                return;
            ForgetLostRequest();
            bool hasPreview = _requests.HasPreview;
            if (!CombatPreviewSlotRule.ShouldRequestBackground(_answeredUnit == _wanted, _requestNumber != 0,
                    hasPreview, hasPreview ? _requests.Preview.Number : 0, _state.AnsweredPreviewNumber)
                || !CombatInputPolicy.CanInspectUnit(facts, _gate.HasPending))
                return;

            _requestNumber = _input.SetPreview(CombatViewPreviewData.UnitAttackRange(_wanted));
            _requestUnit = _wanted;
        }

        // 자기 요청이 슬롯에서 밀려났으면 기다림을 끝냅니다. 슬롯이 다시 한가할 때 새로 요청합니다(R-a).
        private void ForgetLostRequest()
        {
            if (_requestNumber != 0 && (!_requests.HasPreview || _requests.Preview.Number != _requestNumber))
            {
                _requestNumber = 0;
                _requestUnit = EntityId.Empty;
            }
        }

        private void Release()
        {
            Hide();
            ClearOwnedRequest();
            _wanted = EntityId.Empty;
            _wantedTone = CombatRangeTone.Ally;
            _answeredUnit = EntityId.Empty;
            _hazardChecked = false;
            _hazardCells.Clear();
            _skippedActor = EntityId.Empty;
        }

        // 이동 모드 행위자가 바뀌어 지난 판단이 낡았는지입니다(설계 §9). 그리던 유닛이 이동 모드 행위자가 됐거나(호버 중 그 유닛을 고름),
        // 건너뛴 행위자가 이동 모드를 벗어났으면(공격 모드 진입 없이 선택이 바뀜 등) 다시 판단합니다. 둘 다 없으면 이동 모드를 묻지 않습니다.
        private bool IsMoveModeStale()
        {
            if (_wanted.IsEmpty && _skippedActor.IsEmpty)
                return false;
            EntityId actor = _moveMode.MoveModeActorId;
            return (!_wanted.IsEmpty && _wanted == actor) || (!_skippedActor.IsEmpty && _skippedActor != actor);
        }

        private void Hide()
        {
            if (!_shown)
                return;
            _shown = false;
            _feedback.ClearHoverRange();
        }

        // 자기 요청이 슬롯에 있을 때만 비웁니다(판단은 Buffer의 번호 있는 비우기).
        private void ClearOwnedRequest()
        {
            _input.ClearPreview(_requestNumber);
            _requestNumber = 0;
            _requestUnit = EntityId.Empty;
        }

        /// <summary>
        /// 공통 수명의 단계마다 호버의 어느 메서드인지만 답합니다(배경 소유자). 표시를 쉬는 단계(전경 진입·결과 재생 전·표현 정리)는
        /// 포인터를 남기는 <see cref="Suspend"/>, 입력이 끊기는 단계(입력 정리·정지·강제 선택 종료)는 포인터도 잊는 <see cref="Clear"/>입니다.
        /// </summary>
        private sealed class OwnerAdapter : CombatInputOwner
        {
            private readonly CombatUnitRangeHover _hover;

            internal OwnerAdapter(CombatUnitRangeHover hover) => _hover = hover;

            internal override bool IsBackground => true;

            internal override void ObservePointer(in CombatPointerInput input, bool canShow) =>
                _hover.PointerMoved(input.Facts, input.Characters, input.ScreenX, input.ScreenY, canShow);

            internal override void ObserveTick(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, bool canShow) =>
                _hover.Poll(facts, characters, canShow);

            internal override bool PreviewAnswered(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters) =>
                _hover.PreviewAnswered(facts);

            internal override void YieldToForeground() => _hover.Suspend();
            internal override void BeforeResult() => _hover.Suspend();
            internal override void CancelPresentation() => _hover.Suspend();
            internal override void CancelInputs() => _hover.Clear();
            internal override void Stop() => _hover.Clear();
            internal override void SelectCanceled() => _hover.Clear();
        }
    }
}
