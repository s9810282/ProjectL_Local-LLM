using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Tutorial.Item;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial
{
    /// <summary>
    /// 튜토리얼 표시를 Combat 씬의 튜토리얼 화면(<see cref="TutorialViewUI"/>)으로 그립니다(튜토리얼 설계 §15.4, 구현 계획 T-6).
    /// </summary>
    /// <remarks>
    /// 실행 소유자와 같이 런타임 수명이고, 씬 객체는 Combat 화면 연결 동안만 잡습니다. 무엇을 띄울지는 정의의 그 단계에서 읽고,
    /// 보일 대상의 위치는 <see cref="TutorialViewFocusLocator"/>가 구합니다. 지금 표시는 애니메이션이 없어 요청한 자리에서 끝납니다.
    /// 화면·대상 배치 갱신은 표시 완료와 별개로 처리해 진행과 설명 타이머에 영향을 주지 않습니다.
    /// </remarks>
    internal sealed class TutorialViewUIDisplay : ITutorialViewDisplay
    {
        private readonly ICharacterBufferReadSink _characters;
        private readonly ICombatStateBufferReadSink _combat;
        private readonly Action<bool> _focusChanged;
        private readonly List<Rect> _holes = new();
        private readonly List<Rect> _obstacles = new();
        private readonly TutorialViewCardVisibility _cards;
        private TutorialViewUI _ui;
        private TutorialViewFocusLocator _locator;
        private TutorialStepData _explanation;

        /// <param name="focusChanged">게임 창 활성 변경을 받을 곳입니다(실행 소유자의 <c>NotifyFocusChanged</c>).</param>
        internal TutorialViewUIDisplay(ICharacterBufferReadSink characters, ICombatStateBufferReadSink combat,
            IUISystemService ui, Action<bool> focusChanged)
        {
            _characters = characters ?? throw new ArgumentNullException(nameof(characters));
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            _focusChanged = focusChanged ?? throw new ArgumentNullException(nameof(focusChanged));
            _cards = new TutorialViewCardVisibility(ui);
        }

        public void Attach()
        {
            if (_ui != null)
                throw new InvalidOperationException("[TutorialViewUIDisplay] 이미 튜토리얼 화면에 연결돼 있습니다.");

            TutorialViewUI ui = FindSingle<TutorialViewUI>("튜토리얼 화면(TutorialViewUI)");
            CombatSceneRoot root = FindSingle<CombatSceneRoot>("CombatSceneRoot");
            ui.ValidateReferences();
            _locator = new TutorialViewFocusLocator(root, _characters, _combat);
            _ui = ui;
            _ui.FocusNotifier.FocusChanged += _focusChanged;
        }

        public void Detach()
        {
            TutorialViewUI ui = _ui;
            try
            {
                Close();
            }
            finally
            {
                _ui = null;
                _locator = null;
                if (ui != null && ui.FocusNotifier != null)
                    ui.FocusNotifier.FocusChanged -= _focusChanged;
            }
        }

        public void Present(TutorialViewDisplayRequest request, Action completed)
        {
            if (completed == null)
                throw new ArgumentNullException(nameof(completed));

            TutorialViewUI ui = RequireUI();
            TutorialStepData step = FindStep(request.Definition, request.StepKey);
            switch (request.Kind)
            {
                case TutorialViewPresentationKind.Explanation:
                    if (NeedsCards(step))
                        _cards.Prepare();
                    _explanation = step;
                    _holes.Clear();
                    ui.ShowExplanation(step.ExplanationText, _holes, request.Definition.OverlayColor);
                    // 카드·본문을 연 첫 프레임의 Canvas 레이아웃을 완료한 뒤 표시 완료를 알립니다.
                    Canvas.ForceUpdateCanvases();
                    RefreshLayout();
                    break;
                case TutorialViewPresentationKind.Practice:
                    _explanation = null;
                    ui.ShowPractice(step.PracticeGuideText);
                    break;
                case TutorialViewPresentationKind.Transition:
                    // 설명을 닫는 전환이거나, 실습을 통과해 안내를 닫는 전환이다. 덮개·막은 다음 표시가 정한다.
                    _explanation = null;
                    ui.HideExplanation();
                    ui.HidePractice();
                    if (step.Kind == TutorialStepKind.Practice && step.PracticeGoal == TutorialPracticeGoal.EnterCombat)
                        _cards.Release();
                    break;
                default:
                    throw new InvalidOperationException($"[TutorialViewUIDisplay] 알 수 없는 표시 종류입니다: {request.Kind}");
            }

            completed();
        }

        public void RefreshLayout()
        {
            if (_ui == null || !_ui.IsShown)
                return;
            _ui.RefreshPracticeLayout();
            if (_explanation == null || !_ui.IsExplanationShown)
                return;

            _locator.Locate(_explanation.FocusTargets, _holes);
            _locator.LocateObstacles(_obstacles);
            // 기준 대상 외의 강조 영역도 설명 박스에 가려지지 않게 합니다.
            _obstacles.AddRange(_holes);
            Rect? anchor = _locator.TryLocateTarget(_explanation.ExplanationAnchor, out Rect area) ? area : null;
            _ui.RefreshExplanationLayout(_holes, anchor, _explanation.ExplanationPlacement, _obstacles);
        }

        public void ShowBlockedInputNotice(TutorialDefinitionData definition, int stepKey)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            RequireUI().ShowBlockedNotice(FindStep(definition, stepKey).BlockedInputText);
        }

        public void Close()
        {
            _explanation = null;
            _holes.Clear();
            _obstacles.Clear();
            List<Exception> errors = null;
            if (_ui != null)
                CleanupErrors.Try(_ui.CloseAll, ref errors);
            CleanupErrors.Try(_cards.Detach, ref errors);
            CleanupErrors.ThrowIfAny(errors, "[TutorialViewUIDisplay] 튜토리얼 표시 정리에 실패했습니다.");
        }

        private static bool NeedsCards(TutorialStepData step)
        {
            foreach (TutorialFocusTarget target in step.FocusTargets)
            {
                if (target == TutorialFocusTarget.Hand || target == TutorialFocusTarget.MainCharacterCard)
                    return true;
            }
            return false;
        }

        private TutorialViewUI RequireUI()
        {
            return _ui != null
                ? _ui
                : throw new InvalidOperationException("[TutorialViewUIDisplay] Combat 화면에 연결되기 전에는 튜토리얼을 표시할 수 없습니다.");
        }

        private static TutorialStepData FindStep(TutorialDefinitionData definition, int stepKey)
        {
            foreach (TutorialStepData step in definition.Steps)
            {
                if (step != null && step.StepKey == stepKey)
                    return step;
            }

            throw new InvalidOperationException($"[TutorialViewUIDisplay] 정의 {definition.TutorialKey}에 단계 {stepKey}가 없습니다.");
        }

        private static T FindSingle<T>(string name) where T : UnityEngine.Object
        {
            T[] found = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (found.Length != 1)
                throw new InvalidOperationException($"[TutorialViewUIDisplay] Combat 씬에는 {name}가 하나 있어야 합니다. 찾은 수: {found.Length}");
            return found[0];
        }
    }
}
