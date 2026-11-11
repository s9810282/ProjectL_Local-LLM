using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input
{
    /// <summary>
    /// 입력 판단 한 번에 쓰는 현재 사실 묶음입니다(설계 §7.1, UML §2의 facts).
    /// 호출할 때마다 Buffer·수명 상태에서 새로 읽어 만들며, 저장하거나 다음 입력까지 들고 있지 않습니다.
    /// </summary>
    /// <remarks>
    /// 판단 시점의 일시적 사본입니다. 시작/턴 종료/이동 되돌리기 대기의 원본은 <see cref="CombatCommandGate"/>이고 여기에는 읽은 값만 담습니다.
    /// 같은 입력 처리 안에서 대기를 기록하거나 외부 표시 콜백을 부른 뒤에는 이 사본으로 다시 판단하지 않고 새로 읽습니다.
    /// 선행 미처리 명령 여부는 EndTurn·경로 편집·필드 선택 판단 때 읽습니다(그 밖에는 false).
    /// 생성자 인자는 모두 필수입니다. 사실을 더하면 읽는 곳(<see cref="CombatInputFactsReader"/>)이 넘기기를 빠뜨렸을 때 컴파일이 막습니다(평가 C5-02).
    /// </remarks>
    internal readonly struct CombatInputFacts
    {
        internal CombatInputFacts(
            bool isBattleDisplayed,
            bool hasBattle,
            CombatFlowPhase phase,
            StagePhase stagePhase,
            bool hasCompletion,
            InputLayer currentLayer,
            int latestResultSequence,
            int presentedResultSequence,
            bool isFinalizing,
            EntityId selectedActorId,
            bool hasUnhandledCommand,
            CombatPreviewResult previewAnswer,
            bool hasPendingCommand,
            bool isPresenting,
            bool isInputAvailable,
            bool isAttackActive,
            TutorialCombatInputRestriction combatInputRestriction,
            bool hasCurrentAcceptedEmptyMovementRange,
            long battleInstanceId, long controllerTurnSequence,
            bool isSubmissionSuspended,
            bool isAbandonRequested,
            bool isModalOpen,
            bool isItemDragActive,
            bool isTutorialPracticeGoalPending, long queryRevision,
            bool isEntryPresentationPending,
            CombatInputRouter.PointerTarget pointerTarget)
        {
            PointerTarget = pointerTarget;
            QueryRevision = queryRevision;
            IsEntryPresentationPending = isEntryPresentationPending;
            IsTutorialPracticeGoalPending = isTutorialPracticeGoalPending;
            BattleInstanceId = battleInstanceId;
            ControllerTurnSequence = controllerTurnSequence;
            IsItemDragActive = isItemDragActive;
            IsSubmissionSuspended = isSubmissionSuspended;
            IsAbandonRequested = isAbandonRequested;
            IsModalOpen = isModalOpen;
            PreviewAnswer = previewAnswer;
            HasPendingCommand = hasPendingCommand;
            IsPresenting = isPresenting;
            IsInputAvailable = isInputAvailable;
            IsAttackActive = isAttackActive;
            IsBattleDisplayed = isBattleDisplayed;
            HasBattle = hasBattle;
            Phase = phase;
            StagePhase = stagePhase;
            HasCompletion = hasCompletion;
            CurrentLayer = currentLayer;
            LatestResultSequence = latestResultSequence;
            PresentedResultSequence = presentedResultSequence;
            IsFinalizing = isFinalizing;
            SelectedActorId = selectedActorId;
            HasUnhandledCommand = hasUnhandledCommand;
            CombatInputRestriction = combatInputRestriction;
            HasCurrentAcceptedEmptyMovementRange = hasCurrentAcceptedEmptyMovementRange;
        }

        /// <summary>Core 조회 사실의 현재 개정 번호입니다. 전투가 없으면 0입니다.</summary>
        internal long QueryRevision { get; }
        internal long BattleInstanceId { get; }
        internal long ControllerTurnSequence { get; }

        /// <summary>같은 전투의 실행 중 실습이 아직 목표를 기다리는지입니다. 통과 뒤 다음 설명을 기다리는 제한 유지 구간은 제외합니다.</summary>
        internal bool IsTutorialPracticeGoalPending { get; }

        /// <summary>씬에 연결된 뒤 현재 전투를 한 번 그렸는지입니다.</summary>
        internal bool IsBattleDisplayed { get; }

        /// <summary>보드·초기 적 등장 연출이 남았는지입니다. 진행 객체의 현재 값을 읽습니다.</summary>
        internal bool IsEntryPresentationPending { get; }

        internal bool HasBattle { get; }

        /// <summary>Core의 처리 국면입니다.</summary>
        internal CombatFlowPhase Phase { get; }

        /// <summary>Core의 배치/전투 단계입니다. <see cref="HasBattle"/>가 true일 때만 의미가 있습니다.</summary>
        internal StagePhase StagePhase { get; }

        /// <summary>Core가 전투 완료(Completion)를 기록했는지입니다.</summary>
        internal bool HasCompletion { get; }

        /// <summary>실제 입력 Layer(<c>IInputLayerService.CurrentLayer</c>)입니다. 없으면 기본값입니다.</summary>
        internal InputLayer CurrentLayer { get; }

        /// <summary>Core의 결과 발행 순번 S입니다.</summary>
        internal int LatestResultSequence { get; }

        /// <summary>View가 확인한 결과 순번 P입니다.</summary>
        internal int PresentedResultSequence { get; }

        /// <summary>외부 입력을 막는 결과 후처리 중인지입니다. 후처리 안의 내부 재요청 판단에서는 false로 만듭니다.</summary>
        internal bool IsFinalizing { get; }

        /// <summary>완료 표시 목록의 선택 상태에서 고른 아군입니다. 없거나 적을 골랐으면 Empty입니다.</summary>
        internal EntityId SelectedActorId { get; }

        /// <summary>처리 번호 뒤에 아직 처리되지 않은 명령이 있는지입니다. EndTurn·경로 편집·필드 선택 판단에 씁니다.</summary>
        internal bool HasUnhandledCommand { get; }

        /// <summary>시작/턴 종료/이동 되돌리기 결과를 기다리는 중인지입니다. 읽은 시점의 <see cref="CombatCommandGate.HasPending"/> 값입니다.</summary>
        internal bool HasPendingCommand { get; }

        /// <summary>
        /// 확인하지 않은 결과가 남았는지(P &lt; S)입니다. 재생·후처리는 이때만 하므로 "재생 중"도 이것과 같습니다.
        /// 골격과 같은 규칙 원본 <c>CombatProgressFacts.IsUnpresented</c>로 읽은 값입니다.
        /// </summary>
        internal bool IsPresenting { get; }

        /// <summary>입력 판단에 쓸 수 있는지입니다: 씬 연결 활성(오류 정지 전·진입 활성·표시 연결) ∧ 입력 슬롯 연결 ∧ 진입 연출 완료(조정자 <c>IsInputReady</c>). 경로 입력의 Tick 갱신 입구에서도 씁니다.</summary>
        internal bool IsInputAvailable { get; }

        /// <summary>
        /// 공격 모드인지입니다(공격 입력 소유자의 관측값). 포인터 대상은 이 값으로 다시 정하지 않고 <see cref="PointerTarget"/>을 읽습니다(평가 C4-13).
        /// </summary>
        internal bool IsAttackActive { get; }

        /// <summary>이 판단 시점에 Tutorial Buffer에서 읽은 제한입니다.</summary>
        internal TutorialCombatInputRestriction CombatInputRestriction { get; }

        /// <summary>현재 전투·행위자·시작 칸·결과 순번에 맞는 성공한 빈 이동 범위의 기존 진단 사실입니다. 수동 턴 종료 자격에는 쓰지 않습니다.</summary>
        internal bool HasCurrentAcceptedEmptyMovementRange { get; }

        /// <summary>
        /// 입력 Buffer의 현재 미리보기 요청에 대한 Core 응답입니다(번호가 같을 때만, 설계 §5.4·§21.1). 없으면 null입니다.
        /// </summary>
        internal CombatPreviewResult PreviewAnswer { get; }

        /// <summary>전투 골격이 새 제출을 닫았는지입니다(진행 사실 <c>CombatViewProgress.IsSubmissionSuspended</c>).</summary>
        internal bool IsSubmissionSuspended { get; }

        /// <summary>사용자가 포기를 확인했는지입니다(진행 사실 <c>CombatViewProgress.IsAbandonRequested</c>).</summary>
        internal bool IsAbandonRequested { get; }

        /// <summary>전투 입력을 막는 확인창(배치 확인창)이 열려 있는지입니다.</summary>
        internal bool IsModalOpen { get; }

        /// <summary>
        /// 가방 아이템을 끄는 중인지입니다(입력 기능 묶음의 값). 포인터 대상은 이 값으로 다시 정하지 않고 <see cref="PointerTarget"/>을 읽습니다(평가 C4-13).
        /// </summary>
        internal bool IsItemDragActive { get; }

        /// <summary>
        /// 이 판단 시점에 포인터 입력을 받을 대상입니다(평가 C4-13). 입력 사실 읽기가 입력 기능 묶음의 <c>CombatInteraction.ResolvePointerTarget</c>
        /// (판정 한 곳)로 채우고, 연결 입력이 없으면 공격 모드·아이템 드래그가 없는 것으로 정합니다. <c>default(CombatInputFacts)</c>이면 None입니다.
        /// 사실만 받는 곳(경로 입력·환경 호버)이 읽습니다. Router와 감시 단계는 이 사본 대신 묶음에 바로 묻습니다.
        /// </summary>
        internal CombatInputRouter.PointerTarget PointerTarget { get; }
    }
}
