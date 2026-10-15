using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using UnityEngine.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display
{
    /// <summary>
    /// Combat 씬 Canvas의 스테이지 골드·드로우·이동/복귀 견적과 전투 완료를 표시합니다.
    /// </summary>
    /// <remarks>
    /// 값은 CombatViewSystem이 Buffer에서 읽어 넘깁니다. 남은 드로우는 저장값에서 바로 계산한 값입니다(P7).
    /// 같은 값이면 텍스트를 다시 쓰지 않습니다. UISystem 화면이 아니라 씬 Canvas입니다(설계 §13.3, DR-010).
    /// 복귀·완료 확인 버튼 입력은 <see cref="UIInputBinding"/>이 이 HUD(<see cref="IUIInputReceiver"/>)에 슬롯으로 전달합니다
    /// (Assets/AGENTS.md, 개선 설계 D1). <c>Button.onClick</c>에는 아무것도 연결하지 않습니다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class CombatHud : MonoBehaviour, IUIInputReceiver
    {
        private const int ReturnSlot = 0;
        private const int CompletionSlot = 1;

        [Tooltip("이 전투의 스테이지 골드(드로우 전용)입니다. Run 재화는 전역 UI가 보여 줍니다.")]
        [SerializeField, FormerlySerializedAs("currencyText")] private TMP_Text stageGoldText;
        [SerializeField] private TMP_Text remainingDrawText;

        [SerializeField] private TMP_Text movementText;
        [SerializeField] private Button returnButton;
        [SerializeField] private TMP_Text returnText;
        private Action _returnRequested;
        [SerializeField] private Button completionButton;
        [SerializeField] private TMP_Text completionText;
        // 완료 확인 입구입니다. 한 번 누르면 비우므로 같은 표시에서 두 번 부르지 않습니다.
        private Action _completionConfirmed;
        private bool _isInputAttached;
        private bool _hasMovement, _hasReturn;
        private (bool HasPreview, bool PointerValid, bool CanMove, int Distance, int Cost, MovementFailureReason Reason) _shownMovement;
        private (bool HasPreview, bool CanReturn, int Distance, int Cost, MovementFailureReason Reason) _shownReturn;

        private void OnEnable() => InvalidateShownValues();
        private void OnDisable() => InvalidateShownValues();

        private void InvalidateShownValues()
        {
            _hasShown = false;
            _hasMovement = false;
            _hasReturn = false;
        }

        internal void ShowCompletion(BattleCompletionChanges completion, Action confirmed)
        {
            if (confirmed == null)
                throw new ArgumentNullException(nameof(confirmed));
            HideCompletion();
            AttachInputOnce();
            // 자발적 포기는 패배로 기록되지만 문구를 따로 보여 준다(포기 UI 설계 C5·P4).
            completionText.text = completion.Outcome.Outcome == StageOutcome.Victory ? "승리 · 결과 확인"
                : completion.Outcome.Reason == StageOutcomeReason.VoluntaryAbandonment ? "포기 · 결과 확인"
                : "패배 · 결과 확인";
            _completionConfirmed = confirmed;
            completionButton.interactable = true;
            completionButton.gameObject.SetActive(true);
        }

        internal void HideCompletion()
        {
            _completionConfirmed = null;
            completionButton.gameObject.SetActive(false);
        }

        /// <summary>복귀 버튼의 입구를 정합니다. null이면 해제합니다. 누를 수 있는지는 복귀 견적(<see cref="ShowReturn"/>)이 정합니다.</summary>
        internal void BindReturn(Action requested)
        {
            _hasReturn = false;
            if (requested != null)
                AttachInputOnce();
            _returnRequested = requested;
            returnButton.interactable = false;
        }

        void IUIInputReceiver.Click(int slot)
        {
            if (slot == ReturnSlot)
                _returnRequested?.Invoke();
            else if (slot == CompletionSlot)
                ConfirmCompletion();
        }

        void IUIInputReceiver.HoverEnter(int slot)
        {
        }

        void IUIInputReceiver.HoverExit(int slot)
        {
        }

        /// <summary>완료 확인은 표시 한 번에 한 번만 전달하고, 누른 뒤에는 버튼을 끕니다.</summary>
        private void ConfirmCompletion()
        {
            Action confirmed = _completionConfirmed;
            if (confirmed == null)
                return;

            _completionConfirmed = null;
            completionButton.interactable = false;
            confirmed();
        }

        private void AttachInputOnce()
        {
            if (_isInputAttached)
                return;

            UIInputBinding.Attach(returnButton, this, ReturnSlot);
            UIInputBinding.Attach(completionButton, this, CompletionSlot);
            _isInputAttached = true;
        }
        internal void ShowMovement(MovePreview? preview, bool pointerValid)
        {
            var shown = (preview.HasValue, pointerValid, preview?.CanMove == true,
                preview?.QuotedDistance ?? 0, preview?.QuotedActionCost ?? 0, preview?.FailureReason ?? MovementFailureReason.None);
            if (_hasMovement && _shownMovement == shown)
                return;

            // Core 거절 이유가 있으면 먼저 보인다. 포인터 무효는 UI·필드 밖이나 칸 건너뛰기로 경로가 멈춘 상태다.
            movementText.text = preview.HasValue && !preview.Value.CanMove ? $"이동 불가: {preview.Value.FailureReason}" :
                !pointerValid ? "끝점에서 이어 그리세요" : !preview.HasValue ? "" :
                $"거리 {preview.Value.QuotedDistance} · 비용 {preview.Value.QuotedActionCost}";
            _shownMovement = shown;
            _hasMovement = true;
        }
        internal void ShowReturn(CheckpointReturnPreview preview)
        {
            returnButton.interactable = preview?.CanReturn == true;
            var shown = (preview != null, preview?.CanReturn == true, preview?.Return?.DistanceRefunded ?? 0,
                preview?.Return?.ActionCostRefunded ?? 0, preview?.FailureReason ?? MovementFailureReason.None);
            if (_hasReturn && _shownReturn == shown)
                return;

            returnText.text = preview == null ? "복귀" : preview.CanReturn ? $"복귀 · 비용 +{preview.Return.ActionCostRefunded} / 거리 +{preview.Return.DistanceRefunded}" : $"복귀 불가: {preview.FailureReason}";
            _shownReturn = shown;
            _hasReturn = true;
        }

        private bool _hasShown;
        private int _shownStageGold;
        private int _shownRemainingDraws;

        internal void Render(int stageGold, int remainingDraws)
        {
            if (_hasShown && _shownStageGold == stageGold && _shownRemainingDraws == remainingDraws)
                return;

            stageGoldText.SetText("{0}", stageGold);
            remainingDrawText.SetText("{0}", remainingDraws);
            _hasShown = true;
            _shownStageGold = stageGold;
            _shownRemainingDraws = remainingDraws;
        }

        internal void Clear()
        {
            HideCompletion();
            InvalidateShownValues();
            stageGoldText.SetText(string.Empty);
            remainingDrawText.SetText(string.Empty);
        }

        internal string FindMissingReference()
        {
            if (completionButton == null) return $"{nameof(CombatHud)}.{nameof(completionButton)}";
            if (completionText == null) return $"{nameof(CombatHud)}.{nameof(completionText)}";
            if (movementText == null) return $"{nameof(CombatHud)}.{nameof(movementText)}";
            if (returnButton == null) return $"{nameof(CombatHud)}.{nameof(returnButton)}";
            if (returnText == null) return $"{nameof(CombatHud)}.{nameof(returnText)}";
            if (stageGoldText == null)
                return $"{nameof(CombatHud)}.{nameof(stageGoldText)}";
            if (remainingDrawText == null)
                return $"{nameof(CombatHud)}.{nameof(remainingDrawText)}";
            return null;
        }
    }
}
