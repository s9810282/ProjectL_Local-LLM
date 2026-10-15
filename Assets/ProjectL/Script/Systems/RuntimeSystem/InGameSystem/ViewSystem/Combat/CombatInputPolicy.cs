using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// 현재 사실만 보고 입력을 판단하는 무상태 정책입니다(설계 §5.5·§5.6·§7.1·§8.1, W13~W15·W20).
    /// 종료 판정, 목표 Layer, 명령·미리보기·개발 Layer 허용을 이 한 곳에서 정하고, 호출하는 쪽은 같은 조건식을 복제하지 않습니다.
    /// </summary>
    /// <remarks>
    /// Buffer·Core 서비스·Unity를 참조하지 않고 값을 저장하지 않습니다. 실제 Bind·Layer 변경·기록은 호출하는 쪽이 합니다.
    /// View 입력 시점의 검사일 뿐이며 배치 가능 여부·행동 비용 같은 Core 규칙을 대신하지 않습니다.
    /// </remarks>
    internal static class CombatInputPolicy
    {
        /// <summary>
        /// 전투가 끝나 새 입력을 받지 않는 상태인지입니다(설계 §5.6).
        /// Core는 종료 뒤에도 HasBattle을 유지하므로 HasBattle은 보지 않습니다.
        /// </summary>
        internal static bool IsTerminal(in CombatInputFacts facts)
        {
            return facts.Phase == CombatFlowPhase.Finished || facts.HasCompletion;
        }

        /// <summary>
        /// 지금 연결해야 할 입력 Layer입니다. 입력을 받지 않아야 하면 기본값(Layer 없음)입니다(설계 §5.5 표).
        /// 종료가 다른 조건보다 우선하고, 그다음 Core의 배치/전투 단계를 따릅니다.
        /// </summary>
        internal static InputLayer GetDesiredLayer(in CombatInputFacts facts)
        {
            if (!facts.HasBattle || !facts.IsBattleDisplayed || IsTerminal(facts))
                return default;

            return LayerOf(facts.StagePhase);
        }

        /// <summary>
        /// 명령 하나를 지금 기록해도 되는지입니다(설계 §7.1).
        /// 공통: 최초 표시 뒤·전투 있음·종료 아님·대기 없음·후처리 아님.
        /// Live: PlayerInput, Core 단계와 실제 Layer가 명령의 단계와 같음, S == P.
        /// EndUnitTurn은 활성 행위자가 있고 선행 미처리 명령이 없어야 합니다.
        /// RecordInputOnly: Live의 국면·행위자·S/P·선행 명령 조건 대신 실제 Layer만 봅니다(설계 §8.1).
        /// </summary>
        internal static bool CanSubmit(in CombatInputFacts facts, bool hasPendingCommand, CombatViewCommandKind kind)
        {
            StagePhase stage = StageOf(kind);
            if (!PassesCommon(facts, hasPendingCommand) || facts.CurrentLayer != LayerOf(stage))
                return false;
            if (facts.ExecutionMode == CombatExecutionMode.RecordInputOnly)
                return true;

            if (facts.Phase != CombatFlowPhase.PlayerInput
                || facts.StagePhase != stage
                || facts.LatestResultSequence != facts.PresentedResultSequence)
                return false;

            return kind != CombatViewCommandKind.EndUnitTurn
                || (!facts.ActiveActorId.IsEmpty && !facts.HasUnhandledCommand);
        }

        /// <summary>
        /// 새 미리보기 요청이나 미리보기를 만드는 모드 선택을 해도 되는지입니다(설계 §5.4·§7.1).
        /// 일반 연출 중(S > P)은 허용하지만 후처리 중 외부 입력과 Automatic·대기·종료는 막습니다.
        /// </summary>
        internal static bool CanPreview(in CombatInputFacts facts, bool hasPendingCommand, CombatViewPreviewKind kind)
        {
            StagePhase stage = StageOf(kind);
            if (!PassesCommon(facts, hasPendingCommand) || facts.CurrentLayer != LayerOf(stage))
                return false;
            if (facts.ExecutionMode == CombatExecutionMode.RecordInputOnly)
                return true;

            return facts.Phase == CombatFlowPhase.PlayerInput && facts.StagePhase == stage;
        }

        /// <summary>
        /// 이동 경로를 편집·견적·제출해도 되는지의 공통 정책입니다(구조 개선 VR1).
        /// 이동 명령 기록 조건(<see cref="CanSubmit"/>, 대기는 facts의 값) + 연출·후처리 중 아님 + 선행 미처리 명령 없음입니다.
        /// </summary>
        /// <remarks>
        /// 이 식만으로 입력을 허용하지 않습니다. 입력 가용성(<see cref="CombatInputFacts.IsInputAvailable"/>)·대상
        /// (<see cref="CombatInputFacts.IsPathTarget"/>)·전투·행위자·요청 번호·Core context 일치는 경로 입력 입구가 따로 확인합니다.
        /// </remarks>
        internal static bool CanEditPath(in CombatInputFacts facts)
        {
            return CanSubmit(facts, facts.HasPendingCommand, CombatViewCommandKind.Move)
                && !facts.IsPresenting
                && !facts.HasUnhandledCommand;
        }

        /// <summary>
        /// 포기 버튼을 누를 수 있는지입니다(포기 UI 설계 C2). Live · 전투 있음·최초 표시 뒤 · 종료 아님 · 이탈 Idle입니다.
        /// 대기 중 명령·연출 중·배치 단계에서도 허용합니다(골격이 남은 처리·표시를 끝낸 뒤 포기한다, 골격 설계 §6.5).
        /// </summary>
        /// <remarks>View 쪽 버튼 판단일 뿐이며, 최종 허용은 골격 RequestAbandon이 다시 판단합니다(C4).</remarks>
        internal static bool CanRequestAbandon(in CombatInputFacts facts, bool exitIdle)
        {
            return facts.ExecutionMode == CombatExecutionMode.Live
                && facts.HasBattle
                && facts.IsBattleDisplayed
                && !IsTerminal(facts)
                && exitIdle;
        }

        /// <summary>
        /// RecordInputOnly의 개발 Layer 패널 버튼으로 Layer를 바꿔도 되는지입니다(설계 §8.1, W20).
        /// 대기·오류·종료·후처리 중에는 막습니다. Live에서는 항상 거부합니다.
        /// </summary>
        internal static bool CanChangeDevLayer(in CombatInputFacts facts, bool hasPendingCommand)
        {
            return facts.ExecutionMode == CombatExecutionMode.RecordInputOnly && PassesCommon(facts, hasPendingCommand);
        }

        /// <summary>명령이 속한 배치/전투 단계입니다.</summary>
        internal static StagePhase StageOf(CombatViewCommandKind kind)
        {
            switch (kind)
            {
                case CombatViewCommandKind.PlaceMainCharacter:
                case CombatViewCommandKind.PlaceUnit:
                case CombatViewCommandKind.ReturnUnit:
                case CombatViewCommandKind.EnterCombat:
                case CombatViewCommandKind.DrawCharacter:
                    return StagePhase.Deployment;
                case CombatViewCommandKind.ReturnToCheckpoint:
                case CombatViewCommandKind.Move:
                case CombatViewCommandKind.Attack:
                case CombatViewCommandKind.EndUnitTurn:
                    return StagePhase.Combat;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "단계가 없는 명령 종류입니다.");
            }
        }

        /// <summary>미리보기가 속한 배치/전투 단계입니다.</summary>
        internal static StagePhase StageOf(CombatViewPreviewKind kind)
        {
            switch (kind)
            {
                case CombatViewPreviewKind.MainCharacterPlacement:
                case CombatViewPreviewKind.UnitPlacement:
                    return StagePhase.Deployment;
                case CombatViewPreviewKind.MoveRange:
                case CombatViewPreviewKind.ReturnToCheckpoint:
                case CombatViewPreviewKind.Move:
                case CombatViewPreviewKind.AttackRange:
                case CombatViewPreviewKind.Attack:
                    return StagePhase.Combat;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "단계가 없는 미리보기 종류입니다.");
            }
        }

        /// <summary>단계에 대응하는 입력 Layer입니다.</summary>
        internal static InputLayer LayerOf(StagePhase stage)
        {
            switch (stage)
            {
                case StagePhase.Deployment:
                    return StageInputLayers.Deployment;
                case StagePhase.Combat:
                    return StageInputLayers.Combat;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stage), stage, "알 수 없는 Core 단계입니다.");
            }
        }

        private static bool PassesCommon(in CombatInputFacts facts, bool hasPendingCommand)
        {
            return facts.IsBattleDisplayed
                && facts.HasBattle
                && !IsTerminal(facts)
                && !hasPendingCommand
                && !facts.IsFinalizing;
        }
    }
}
