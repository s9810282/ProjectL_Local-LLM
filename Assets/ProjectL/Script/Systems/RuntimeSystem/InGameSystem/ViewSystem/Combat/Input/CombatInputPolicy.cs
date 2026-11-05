using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input
{
    /// <summary>
    /// 현재 사실만 보고 입력을 판단하는 무상태 정책입니다(설계 §5.5·§5.6·§7.1, W13~W15).
    /// 종료 판정, 목표 Layer, 명령·미리보기 허용을 이 한 곳에서 정하고, 호출하는 쪽은 같은 조건식을 복제하지 않습니다.
    /// </summary>
    /// <remarks>
    /// Buffer·Core 서비스·Unity를 참조하지 않고 값을 저장하지 않습니다. 실제 Bind·Layer 변경·기록은 호출하는 쪽이 합니다.
    /// View 입력 시점의 검사일 뿐이며 배치 가능 여부·행동 비용 같은 Core 규칙을 대신하지 않습니다.
    /// </remarks>
    internal static class CombatInputPolicy
    {
        /// <summary>
        /// 전투가 끝나 새 입력을 받지 않는 상태인지입니다(설계 §5.6). 사실은 "승패 기록됨"(완료 있음) 하나입니다(C 단계 설계 C-4).
        /// Core는 종료 뒤에도 HasBattle을 유지하므로 HasBattle은 보지 않습니다.
        /// 전투 Plan의 제출 정지 줄(<c>CombatPlan.Decide</c> 1행)도 같은 사실로 판단합니다.
        /// </summary>
        /// <remarks>
        /// <c>Finished</c> 국면은 따로 보지 않습니다. Core는 받아들인 명령의 "끝났음"과 <c>Finished</c>를 같게 강제하고(CombatCommandTransition),
        /// 끝난 명령은 완료 제안 없이 게시하지 않으며 완료와 같은 거래로 설치합니다(BattleTransactions.Apply). 포기는 국면을 두고 완료만 남깁니다.
        /// 그래서 <c>Finished</c>면 언제나 완료가 있습니다.
        /// </remarks>
        internal static bool IsTerminal(in CombatInputFacts facts)
        {
            return facts.HasCompletion;
        }

        /// <summary>
        /// 새 입력을 닫아야 하는지입니다. 전투 골격이 제출을 닫았거나 사용자가 포기를 확인했으면 닫습니다(View 단계 설계 W2).
        /// 입력 Layer·명령·미리보기·배치 확인창 판단이 이 판정 하나를 읽습니다.
        /// </summary>
        internal static bool IsInputClosed(in CombatInputFacts facts)
        {
            return facts.IsSubmissionSuspended || facts.IsAbandonRequested;
        }

        /// <summary>
        /// 다른 입력을 막아야 하는지입니다. 입력이 닫혔거나 확인창·진입 연출이 남아 있으면 막습니다.
        /// 덱·가방 UI 입구, BattleChanger·가방·행동 버튼의 보임이 이 판정 하나를 읽습니다. 확인창 자신의 판단은 이것을 쓰지 않습니다.
        /// </summary>
        internal static bool IsInputBlocked(in CombatInputFacts facts)
        {
            return IsInputClosed(facts) || facts.IsModalOpen || facts.IsEntryPresentationPending;
        }

        /// <summary>
        /// 지금 연결해야 할 입력 Layer입니다. 입력을 받지 않아야 하면 기본값(Layer 없음)입니다(설계 §5.5 표).
        /// 종료·입력 닫힘이 다른 조건보다 우선하고, 그다음 Core의 배치/전투 단계를 따릅니다.
        /// </summary>
        internal static InputLayer GetDesiredLayer(in CombatInputFacts facts)
        {
            if (!facts.HasBattle || !facts.IsBattleDisplayed || IsTerminal(facts) || IsInputClosed(facts) || facts.IsEntryPresentationPending)
                return default;

            return LayerOf(facts.StagePhase);
        }

        /// <summary>
        /// 명령 하나를 지금 기록해도 되는지입니다(설계 §7.1).
        /// 공통: 최초 표시 뒤·전투 있음·종료 아님·입력 닫힘 아님·대기 없음·후처리 아님.
        /// 그리고 PlayerInput, Core 단계와 실제 Layer가 명령의 단계와 같음, 확인하지 않은 결과 없음(P &lt; S 아님, 규칙 원본 <c>CombatProgressFacts.IsUnpresented</c>).
        /// P &gt; S는 계약 위반이라 Tick의 결과 관측(<c>CombatViewRefresh</c>)이 오류로 보고합니다.
        /// EndPlayerTurn은 선택 유무와 무관하며 선행 미처리 명령이 없어야 합니다.
        /// </summary>
        internal static bool CanSubmit(in CombatInputFacts facts, bool hasPendingCommand, CombatViewCommandKind kind)
        {
            StagePhase stage = StageOf(kind);
            if (!AllowsTutorialCommand(facts, kind)
                || !PassesCommon(facts, hasPendingCommand) || facts.CurrentLayer != LayerOf(stage))
                return false;

            if (facts.Phase != CombatFlowPhase.PlayerInput
                || facts.StagePhase != stage
                || CombatProgressFacts.IsUnpresented(facts.PresentedResultSequence, facts.LatestResultSequence))
                return false;

            return kind != CombatViewCommandKind.EndPlayerTurn
                || !facts.HasUnhandledCommand;
        }

        /// <summary>
        /// 새 미리보기 요청이나 미리보기를 만드는 모드 선택을 해도 되는지입니다(설계 §5.4·§7.1).
        /// 일반 연출 중(S > P)은 허용하지만 후처리 중 외부 입력과 Automatic·대기·종료는 막습니다.
        /// 국면 무관 종류(유닛 공격 범위 조회)는 지금 국면을 그 종류의 단계로 봅니다(호버 공격 범위 설계 UR3).
        /// </summary>
        internal static bool CanPreview(in CombatInputFacts facts, bool hasPendingCommand, CombatViewPreviewKind kind)
        {
            StagePhase stage = IsAnyStage(kind) ? facts.StagePhase : StageOf(kind);
            if (!AllowsTutorialPreview(facts, kind)
                || !PassesCommon(facts, hasPendingCommand) || facts.CurrentLayer != LayerOf(stage))
                return false;

            return facts.Phase == CombatFlowPhase.PlayerInput && facts.StagePhase == stage;
        }

        /// <summary>
        /// 유닛 호버로 공격 범위를 조회·표시해도 되는지의 유일한 식입니다(호버 공격 범위 설계 UR3).
        /// 유닛 공격 범위 조회 미리보기를 낼 수 있고, 결과 재생 중이 아니어야 합니다(재생 중에는 유닛이 움직인다).
        /// </summary>
        internal static bool CanInspectUnit(in CombatInputFacts facts, bool hasPendingCommand)
        {
            return CanPreview(facts, hasPendingCommand, CombatViewPreviewKind.UnitAttackRange) && !facts.IsPresenting;
        }

        /// <summary>
        /// 배치·전투의 표시 사실을 정보 대상으로 선택해도 되는지입니다. 전투의 적 자동 행동 중에도 명령/Preview 없이 조회합니다.
        /// 공격 모드·경로 지정·UI 가림은 현재 상태를 가진 호출자가 검사합니다.
        /// </summary>
        internal static bool CanSelectUnit(in CombatInputFacts facts)
        {
            if (!facts.IsInputAvailable || !facts.IsBattleDisplayed || !facts.HasBattle || IsTerminal(facts)
                || IsInputBlocked(facts) || facts.IsFinalizing
                || (facts.StagePhase != StagePhase.Combat && facts.StagePhase != StagePhase.Deployment)
                || facts.CurrentLayer != LayerOf(facts.StagePhase))
                return false;

            // 자동 행동 중에는 완료 표시 목록만 선택합니다. 이동·공격·미리보기 허가는 그대로 별도 정책에 둡니다.
            if (facts.StagePhase == StagePhase.Combat && facts.Phase == CombatFlowPhase.Automatic)
                return true;

            return facts.Phase == CombatFlowPhase.PlayerInput && !facts.HasPendingCommand
                && !facts.IsPresenting && !facts.HasUnhandledCommand;
        }

        /// <summary>선택 가용성을 조회할 수 있는 안정 상태입니다. 실행 허가나 비용 판정은 하지 않습니다.</summary>
        internal static bool CanQueryActionAvailability(in CombatInputFacts facts) =>
            facts.IsInputAvailable && !IsInputBlocked(facts) && !facts.IsPresenting && !facts.HasUnhandledCommand
                && CanPreview(facts, facts.HasPendingCommand, CombatViewPreviewKind.ActionAvailability);

        /// <summary>튜토리얼 제한에 따른 명령 허용의 원본입니다. 기존 전투 자격 검사는 별도로 유지합니다.</summary>
        internal static bool AllowsTutorialCommand(in CombatInputFacts facts, CombatViewCommandKind kind)
        {
            return facts.CombatInputRestriction == TutorialCombatInputRestriction.None
                || (facts.CombatInputRestriction == TutorialCombatInputRestriction.MoveOnly
                    && (kind == CombatViewCommandKind.Move
                        || kind == CombatViewCommandKind.EndPlayerTurn
                        || (kind == CombatViewCommandKind.EnterCombat && facts.StagePhase == StagePhase.Deployment
                            && facts.IsTutorialPracticeGoalPending)));
        }

        /// <summary>튜토리얼 제한에 따른 미리보기·모드 진입 허용의 원본입니다. 조회만으로 안내를 제출하지 않습니다.</summary>
        internal static bool AllowsTutorialPreview(in CombatInputFacts facts, CombatViewPreviewKind kind)
        {
            return facts.CombatInputRestriction == TutorialCombatInputRestriction.None
                || (facts.CombatInputRestriction == TutorialCombatInputRestriction.MoveOnly
                    && (kind == CombatViewPreviewKind.Move || kind == CombatViewPreviewKind.MoveRange
                        || kind == CombatViewPreviewKind.ActionAvailability));
        }

        // 배치·전투 어느 국면에서나 내는 미리보기 종류입니다. 판별은 CanPreview 한 곳에서만 합니다.
        private static bool IsAnyStage(CombatViewPreviewKind kind) => kind == CombatViewPreviewKind.UnitAttackRange;

        /// <summary>
        /// 환경 설명 호버를 보일 수 있는 사실인지입니다. 표시가 끝난 결과만 설명합니다(전투 있음·최초 표시 뒤·재생·후처리 아님·종료 아님).
        /// </summary>
        /// <remarks>
        /// 이 식만으로 보이지 않습니다. 수신 가능(입력 연결과 <see cref="IsInputBlocked"/>)과 포인터 대상
        /// (<see cref="CombatInputFacts.PointerTarget"/>)은 호출자가 따로 확인합니다(C 단계 설계 C-8). 대상 판정은 입력 기능 묶음의 <c>CombatInteraction.ResolvePointerTarget</c> 한 곳입니다(평가 C4-13).
        /// </remarks>
        internal static bool CanShowEnvironmentHover(in CombatInputFacts facts)
        {
            return facts.IsBattleDisplayed
                && facts.HasBattle
                && !facts.IsPresenting
                && !facts.IsFinalizing
                && !IsTerminal(facts);
        }

        /// <summary>
        /// 플레이어가 카메라를 움직여도(이동·줌) 되는지입니다(카메라 조작 설계 CC2). 입력이 연결돼 있고, 전투를 그렸고, 결과 화면(종료)이 아니고,
        /// 확인창이 닫혀 있고, 실제 Layer가 카메라 맵을 켜는 스테이지 Layer(배치·전투)일 때만 참입니다.
        /// 명령 대기·재생 중·적 턴은 막지 않습니다.
        /// </summary>
        /// <remarks>
        /// Layer를 보는 까닭: 설정 창 등 다른 Layer로 바뀌면 포인터 이동이 오지 않아 마지막 포인터가 가장자리에 남을 수 있습니다.
        /// 입력이 닫히면(제출 정지·포기 요청) 목표 Layer가 없어지므로(<see cref="GetDesiredLayer"/>) 같은 줄로 멈춥니다.
        /// </remarks>
        internal static bool CanMoveCamera(in CombatInputFacts facts)
        {
            return facts.IsInputAvailable
                && facts.IsBattleDisplayed
                && !IsTerminal(facts)
                && !facts.IsModalOpen
                && !facts.IsEntryPresentationPending
                && (facts.CurrentLayer == StageInputLayers.Deployment || facts.CurrentLayer == StageInputLayers.Combat);
        }

        /// <summary>
        /// 이동 경로를 편집·견적·제출해도 되는지의 공통 정책입니다(구조 개선 VR1).
        /// 이동 명령 기록 조건(<see cref="CanSubmit"/>, 대기는 facts의 값) + 연출·후처리 중 아님 + 선행 미처리 명령 없음입니다.
        /// </summary>
        /// <remarks>
        /// 이 식만으로 입력을 허용하지 않습니다. 입력 가용성(<see cref="CombatInputFacts.IsInputAvailable"/>)·대상
        /// (<see cref="CombatInputFacts.PointerTarget"/>이 Path)·전투·행위자·요청 번호·Core context 일치는 경로 입력 입구가 따로 확인합니다.
        /// </remarks>
        internal static bool CanEditPath(in CombatInputFacts facts)
        {
            return CanSubmit(facts, facts.HasPendingCommand, CombatViewCommandKind.Move)
                && !facts.IsPresenting
                && !facts.HasUnhandledCommand;
        }

        /// <summary>
        /// 포기 요청을 받을 수 있는지입니다(포기 UI 설계 C2). 전투 있음·최초 표시 뒤 · 종료 아님 · 이탈 전입니다.
        /// 대기 중 명령·연출 중·배치 단계에서도 허용합니다(전투 Plan이 남은 처리·표시를 끝낸 뒤 포기한다, 재구성 설계 J8).
        /// </summary>
        /// <param name="exitIdle">아직 포기를 요청하지 않았고 이탈(새 제출 정지)도 시작되지 않았는지입니다(<see cref="CombatViewProgress.CanMarkAbandon"/>).</param>
        /// <remarks>
        /// View 쪽 입구 판단입니다. 받은 뒤에는 요청 상태만 남고, 실제 포기 시점은 전투 Plan이 정합니다.
        /// 그 사이 실제 승패가 먼저 기록되면 Plan은 그 승패로 이탈합니다(K22 A4).
        /// </remarks>
        internal static bool CanRequestAbandon(in CombatInputFacts facts, bool exitIdle)
        {
            return facts.HasBattle
                && facts.IsBattleDisplayed
                && !IsTerminal(facts)
                && !facts.IsEntryPresentationPending
                && exitIdle;
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
                // 아이템 장착·교체·해제는 배치 국면 명령이다(Core 장착 규칙, 가방 설계 P4).
                case CombatViewCommandKind.EquipItem:
                case CombatViewCommandKind.ReplaceItem:
                case CombatViewCommandKind.UnequipItem:
                    return StagePhase.Deployment;
                case CombatViewCommandKind.Move:
                case CombatViewCommandKind.Attack:
                case CombatViewCommandKind.UndoMove:
                case CombatViewCommandKind.EndPlayerTurn:
                    return StagePhase.Combat;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "단계가 없는 명령 종류입니다.");
            }
        }

        /// <summary>
        /// 미리보기가 속한 배치/전투 단계입니다. 국면 무관 종류(<see cref="CombatViewPreviewKind.UnitAttackRange"/>)는
        /// 단계가 없어 예외이며, 그 허용은 <see cref="CanPreview"/>가 지금 국면으로 판단합니다.
        /// </summary>
        internal static StagePhase StageOf(CombatViewPreviewKind kind)
        {
            switch (kind)
            {
                case CombatViewPreviewKind.MainCharacterPlacement:
                case CombatViewPreviewKind.UnitPlacement:
                    return StagePhase.Deployment;
                case CombatViewPreviewKind.MoveRange:
                case CombatViewPreviewKind.ActionAvailability:
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
                && !IsInputClosed(facts)
                && !facts.IsEntryPresentationPending
                && !hasPendingCommand
                && !facts.IsFinalizing;
        }
    }
}
