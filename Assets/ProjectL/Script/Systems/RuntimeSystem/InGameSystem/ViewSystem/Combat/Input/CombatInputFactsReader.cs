using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Assist;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input
{
    /// <summary>판단 한 번의 입력 사실을 기존 원본에서 읽습니다. 캐시·Buffer 쓰기·입력 정책 판정은 하지 않습니다. 다만 빈 이동 범위 근거를 읽을 때 사실이 바뀌었으면 그 근거를 지웁니다(<c>CombatMovementRangeTracker.HasAcceptedEmptyRange</c>).</summary>
    internal sealed class CombatInputFactsReader
    {
        private readonly ICombatStateBufferReadSink _state;
        private readonly ICombatViewBufferReadSink _inputRead;
        private readonly CombatInputConnection _inputConnection;
        private readonly CombatResultPlayback _playback;
        private readonly CombatViewProgress _progress;
        private readonly CombatBlockedInputReport _blockedInput;
        private readonly Func<CombatViewConnection> _readConnection;
        private readonly Func<bool> _isInputReady;
        private readonly Func<bool> _hasPendingCommand;
        private readonly Func<bool> _isModalOpen;

        internal CombatInputFactsReader(ICombatStateBufferReadSink state, ICombatViewBufferReadSink inputRead,
            CombatInputConnection inputConnection, CombatResultPlayback playback, CombatViewProgress progress,
            CombatBlockedInputReport blockedInput, Func<CombatViewConnection> readConnection,
            Func<bool> isInputReady, Func<bool> hasPendingCommand, Func<bool> isModalOpen)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _inputRead = inputRead ?? throw new ArgumentNullException(nameof(inputRead));
            _inputConnection = inputConnection ?? throw new ArgumentNullException(nameof(inputConnection));
            _playback = playback ?? throw new ArgumentNullException(nameof(playback));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _blockedInput = blockedInput ?? throw new ArgumentNullException(nameof(blockedInput));
            _readConnection = readConnection ?? throw new ArgumentNullException(nameof(readConnection));
            _isInputReady = isInputReady ?? throw new ArgumentNullException(nameof(isInputReady));
            _hasPendingCommand = hasPendingCommand ?? throw new ArgumentNullException(nameof(hasPendingCommand));
            _isModalOpen = isModalOpen ?? throw new ArgumentNullException(nameof(isModalOpen));
        }

        /// <summary>매 호출 새 사실을 읽고 후처리/미처리 명령의 기존 투영만 구별합니다.</summary>
        internal CombatInputFacts Read(bool includeUnhandledCommand = false, bool followUp = false)
        {
            bool hasBattle = _state.HasBattle;
            CombatCommandState command = hasBattle ? _state.Current.CommandState : null;
            // 서비스 수명에 특정 표시/선택을 붙잡지 않고 현재 연결을 읽습니다.
            CombatViewConnection connection = _readConnection();
            CombatSceneInput input = connection?.Input;
            EntityId selectedActor = hasBattle && connection != null ? connection.Selection.SelectedActorId : EntityId.Empty;
            // 포인터 대상을 사실의 Layer와 같은 값으로 정하려고 한 번만 읽습니다(평가 C4-13).
            InputLayer layer = _inputConnection.CurrentLayer;
            return new CombatInputFacts(
                connection != null && connection.Shown,
                hasBattle,
                _state.Phase,
                hasBattle ? command.Turn.StagePhase : StagePhase.Deployment,
                _state.Completion != null,
                layer,
                _state.LatestResultSequence,
                _inputRead.PresentedResultSequence,
                !followUp && _playback.IsFinalizing,
                selectedActor,
                includeUnhandledCommand && _inputRead.TryGetNextCommand(_state.HandledCommandNumber, out _),
                ReadCurrentPreviewAnswer(),
                hasPendingCommand: _hasPendingCommand(),
                isPresenting: CombatProgressFacts.IsUnpresented(_inputRead.PresentedResultSequence, _state.LatestResultSequence),
                isInputAvailable: _isInputReady(),
                isAttackActive: input != null && input.IsAttackActive,
                combatInputRestriction: _blockedInput.Restriction,
                hasCurrentAcceptedEmptyMovementRange: input != null && input.HasCurrentAcceptedEmptyMovementRange(selectedActor),
                isSubmissionSuspended: _progress.IsSubmissionSuspended,
                isAbandonRequested: _progress.IsAbandonRequested,
                isModalOpen: _isModalOpen(),
                isItemDragActive: input != null && input.IsItemDragActive,
                battleInstanceId: hasBattle ? _state.BattleInstanceId : 0,
                controllerTurnSequence: command?.Turn.ControllerTurnSequence ?? 0,
                isTutorialPracticeGoalPending: hasBattle && _blockedInput.IsPracticeGoalPending(_state.BattleInstanceId), queryRevision: _state.QueryRevision,
                isEntryPresentationPending: _progress.IsEntryPresentationPending,
                // 대상 판정은 입력 기능 묶음 한 곳입니다(평가 C4-13). 연결 입력이 없으면 공격 모드·아이템 드래그·스킬 모드가 없는 것으로 봅니다.
                pointerTarget: input != null
                    ? input.ResolvePointerTarget(layer)
                    : CombatInputRouter.ResolvePointerTarget(layer, false, false, false));
        }

        /// <summary>현재 입력 요청 번호와 일치하는 응답만 돌려줍니다.</summary>
        private CombatPreviewResult ReadCurrentPreviewAnswer()
        {
            return _inputRead.HasPreview
                && _state.TryGetLatestPreview(out CombatPreviewResult answer)
                && answer.RequestNumber == _inputRead.Preview.Number
                ? answer
                : null;
        }
    }
}
