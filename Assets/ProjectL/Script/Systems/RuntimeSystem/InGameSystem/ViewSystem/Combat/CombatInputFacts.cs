using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// 입력 판단 한 번에 쓰는 현재 사실 묶음입니다(설계 §7.1, UML §2의 facts).
    /// 호출할 때마다 Buffer·수명 상태에서 새로 읽어 만들며, 저장하거나 다음 입력까지 들고 있지 않습니다.
    /// </summary>
    /// <remarks>
    /// 판단 시점의 일시적 사본입니다. 시작/턴 종료 대기의 원본은 <see cref="CombatCommandGate"/>이고 여기에는 읽은 값만 담습니다.
    /// 같은 입력 처리 안에서 대기를 기록하거나 외부 표시 콜백을 부른 뒤에는 이 사본으로 다시 판단하지 않고 새로 읽습니다.
    /// 선행 미처리 명령 여부는 EndTurn과 경로 편집 판단 때만 읽습니다(그 밖에는 false).
    /// </remarks>
    internal readonly struct CombatInputFacts
    {
        internal CombatInputFacts(
            bool isBattleDisplayed,
            bool hasBattle,
            CombatFlowPhase phase,
            StagePhase stagePhase,
            bool hasCompletion,
            CombatExecutionMode executionMode = CombatExecutionMode.Live,
            InputLayer currentLayer = default,
            int latestResultSequence = 0,
            int presentedResultSequence = 0,
            bool isFinalizing = false,
            EntityId activeActorId = default,
            bool hasUnhandledCommand = false,
            CombatPreviewResult previewAnswer = null,
            bool hasPendingCommand = false,
            bool isPresenting = false,
            bool isInputAvailable = false,
            bool isPathTarget = false)
        {
            PreviewAnswer = previewAnswer;
            HasPendingCommand = hasPendingCommand;
            IsPresenting = isPresenting;
            IsInputAvailable = isInputAvailable;
            IsPathTarget = isPathTarget;
            IsBattleDisplayed = isBattleDisplayed;
            HasBattle = hasBattle;
            Phase = phase;
            StagePhase = stagePhase;
            HasCompletion = hasCompletion;
            ExecutionMode = executionMode;
            CurrentLayer = currentLayer;
            LatestResultSequence = latestResultSequence;
            PresentedResultSequence = presentedResultSequence;
            IsFinalizing = isFinalizing;
            ActiveActorId = activeActorId;
            HasUnhandledCommand = hasUnhandledCommand;
        }

        /// <summary>씬에 연결된 뒤 현재 전투를 한 번 그렸는지입니다.</summary>
        internal bool IsBattleDisplayed { get; }

        internal bool HasBattle { get; }

        /// <summary>Core의 처리 국면입니다.</summary>
        internal CombatFlowPhase Phase { get; }

        /// <summary>Core의 배치/전투 단계입니다. <see cref="HasBattle"/>가 true일 때만 의미가 있습니다.</summary>
        internal StagePhase StagePhase { get; }

        /// <summary>Core가 전투 완료(Completion)를 기록했는지입니다.</summary>
        internal bool HasCompletion { get; }

        /// <summary>조립 때 고정한 실행 모드입니다. RecordInputOnly는 Live의 국면 조건 대신 실제 Layer를 따릅니다(설계 §8.1).</summary>
        internal CombatExecutionMode ExecutionMode { get; }

        /// <summary>실제 입력 Layer(<c>IInputLayerService.CurrentLayer</c>)입니다. 없으면 기본값입니다.</summary>
        internal InputLayer CurrentLayer { get; }

        /// <summary>Core의 결과 발행 순번 S입니다.</summary>
        internal int LatestResultSequence { get; }

        /// <summary>View가 확인한 결과 순번 P입니다.</summary>
        internal int PresentedResultSequence { get; }

        /// <summary>외부 입력을 막는 결과 후처리 중인지입니다. 후처리 안의 내부 재요청 판단에서는 false로 만듭니다.</summary>
        internal bool IsFinalizing { get; }

        /// <summary>Core의 현재 행위자입니다. 없으면 Empty입니다.</summary>
        internal EntityId ActiveActorId { get; }

        /// <summary>처리 번호 뒤에 아직 처리되지 않은 명령이 있는지입니다. EndTurn과 경로 편집 판단에만 씁니다.</summary>
        internal bool HasUnhandledCommand { get; }

        /// <summary>시작/턴 종료 결과를 기다리는 중인지입니다. 읽은 시점의 <see cref="CombatCommandGate.HasPending"/> 값입니다.</summary>
        internal bool HasPendingCommand { get; }

        /// <summary>결과를 재생·후처리 중이거나 확인하지 않은 결과가 남았는지입니다(Playback busy 또는 S &gt; P).</summary>
        internal bool IsPresenting { get; }

        /// <summary>입력 수신이 연결돼 있고 오류 정지 전이며 전투가 활성인지입니다. 경로 입력의 Tick 갱신 입구에서도 씁니다.</summary>
        internal bool IsInputAvailable { get; }

        /// <summary>
        /// 포인터 입력의 대상이 경로 입력인지입니다(공격 모드가 아니고 실제 Layer가 전투).
        /// 대상 판정의 원본은 CombatViewSystem이며(VR2부터 Router) 여기에는 관측값만 담습니다.
        /// </summary>
        internal bool IsPathTarget { get; }

        /// <summary>
        /// 입력 Buffer의 현재 미리보기 요청에 대한 Core 응답입니다(번호가 같을 때만, 설계 §5.4·§21.1). 없으면 null입니다.
        /// </summary>
        internal CombatPreviewResult PreviewAnswer { get; }
    }
}
