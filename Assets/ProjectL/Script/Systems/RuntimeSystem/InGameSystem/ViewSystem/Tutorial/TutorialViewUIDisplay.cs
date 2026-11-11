using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Tutorial.Item;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial
{
    /// <summary>
    /// 튜토리얼 표시를 Combat 씬의 튜토리얼 화면(<see cref="TutorialViewUI"/>)으로 그립니다(튜토리얼 설계 §15.4, 구현 계획 T-6).
    /// </summary>
    /// <remarks>
    /// 실행 소유자와 같이 런타임 수명이고, 씬 객체는 Combat 화면 연결 동안만 잡습니다. 무엇을 띄울지는 정의의 그 단계에서 읽고,
    /// 보일 대상의 위치는 <see cref="TutorialViewFocusLocator"/>가 구합니다. 표시 요청은 즉시 완료하며 실습 점멸의 수명과 분리합니다.
    /// 화면·대상 배치 갱신은 표시 완료와 별개로 처리해 단계 진행에 영향을 주지 않습니다.
    /// </remarks>
    internal sealed class TutorialViewUIDisplay : ITutorialViewDisplay
    {
        private readonly ICharacterBufferReadSink _characters;
        private readonly ICombatStateBufferReadSink _combat;
        private readonly ITutorialStateBufferReadSink _tutorial;
        private readonly Func<CombatInputWindow> _readInputWindow;
        private readonly TutorialFocusFrame _frame = new();
        private readonly TutorialModelHighlight _models = new();
        private readonly List<Rect> _obstacles = new();
        private TutorialViewUI _ui;
        private TutorialViewFocusLocator _locator;
        private TutorialStepData _explanation;
        private TutorialStepData _practice;
        private TutorialSessionSnapshot _practiceSession;

        internal TutorialViewUIDisplay(ICharacterBufferReadSink characters, ICombatStateBufferReadSink combat,
            ITutorialStateBufferReadSink tutorial, Func<CombatInputWindow> readInputWindow)
        {
            _characters = characters ?? throw new ArgumentNullException(nameof(characters));
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            _tutorial = tutorial ?? throw new ArgumentNullException(nameof(tutorial));
            _readInputWindow = readInputWindow ?? throw new ArgumentNullException(nameof(readInputWindow));
        }

        public void Attach()
        {
            if (_ui != null)
                throw new InvalidOperationException("[TutorialViewUIDisplay] 이미 튜토리얼 화면에 연결돼 있습니다.");

            TutorialViewUI ui = FindSingle<TutorialViewUI>("튜토리얼 화면(TutorialViewUI)");
            CombatSceneRoot root = FindSingle<CombatSceneRoot>("CombatSceneRoot");
            ui.ValidateReferences();
            _locator = new TutorialViewFocusLocator(root, _characters, _combat, () => ui.HighlightStyle.ModelWidth);
            ui.BindHighlightCleanup(ClearModelHighlights);
            _ui = ui;
        }

        public void Detach()
        {
            try
            {
                Close();
            }
            finally
            {
                if (_ui != null) _ui.BindHighlightCleanup(null);
                _ui = null;
                _locator = null;
            }
        }

        public void Present(TutorialViewDisplayRequest request, Action completed)
        {
            if (completed == null)
                throw new ArgumentNullException(nameof(completed));

            TutorialViewUI ui = RequireUI();
            TutorialStepData step = FindStep(request.Definition, request.StepKey);
            _practice = null;
            _practiceSession = null;
            switch (request.Kind)
            {
                case TutorialViewPresentationKind.Explanation:
                    _explanation = step;
                    _frame.Clear();
                    ui.ShowExplanation(step.ExplanationText, _frame.Holes, request.Definition.OverlayColor);
                    // 본문을 연 첫 프레임의 Canvas 레이아웃을 완료한 뒤 표시 완료를 알립니다.
                    Canvas.ForceUpdateCanvases();
                    RefreshLayout();
                    break;
                case TutorialViewPresentationKind.Practice:
                    _explanation = null;
                    TutorialSessionSnapshot session = ReadActiveSession();
                    if (session == null || session.Status != TutorialSessionStatus.Running
                        || session.StepKind != TutorialStepKind.Practice || session.CurrentStepKey != step.StepKey
                        || session.TutorialKey != request.Definition.TutorialKey)
                        throw new InvalidOperationException("[TutorialViewUIDisplay] 실습 표시 요청과 현재 세션의 단계가 일치하지 않습니다.");
                    _practice = step;
                    _practiceSession = session;
                    ui.ShowPractice(step.PracticeGuideText);
                    break;
                case TutorialViewPresentationKind.Transition:
                    // 설명을 닫는 전환이거나, 실습을 통과해 안내를 닫는 전환이다. 덮개·막은 다음 표시가 정한다.
                    _explanation = null;
                    ui.HideExplanation();
                    ui.HidePractice();
                    break;
                default:
                    throw new InvalidOperationException($"[TutorialViewUIDisplay] 알 수 없는 표시 종류입니다: {request.Kind}");
            }

            completed();
        }

        public void RefreshLayout()
        {
            if (_ui == null || !_ui.IsShown)
            {
                if (_ui != null) _ui.ClearHighlights();
                else ClearModelHighlights();
                return;
            }
            _ui.RefreshPracticeLayout();

            try
            {
                if (_practice != null && _ui.IsPracticeShown)
                {
                    RefreshPracticeHighlights();
                    return;
                }
                if (_explanation == null || !_ui.IsExplanationShown) return;
                TutorialHighlightStyle style = _ui.HighlightStyle;
                style.Validate();
                _locator.Locate(_explanation, _frame);
                _locator.LocateObstacles(_obstacles);
                _ui.RefreshExplanationLayout(_frame, _explanation.ExplanationPlacement, _obstacles);
                _models.Sync(_frame.Models, style);
            }
            catch (Exception error)
            {
                try { _ui.ClearHighlights(); }
                catch (Exception cleanup) { throw new AggregateException(error, cleanup); }
                throw;
            }
        }

        private void RefreshPracticeHighlights()
        {
            TutorialSessionSnapshot session = ReadActiveSession();
            if (session == null || _practiceSession == null
                || session.SessionId != _practiceSession.SessionId || session.ContextId != _practiceSession.ContextId
                || session.BattleInstanceId != _practiceSession.BattleInstanceId || session.TutorialKey != _practiceSession.TutorialKey
                || session.CurrentStepKey != _practiceSession.CurrentStepKey || session.StepRevision != _practiceSession.StepRevision
                || session.Status != TutorialSessionStatus.Running || session.StepKind != TutorialStepKind.Practice
                || session.WaitReason != TutorialWaitReason.PracticeGoal)
            {
                _ui.ClearHighlights();
                return;
            }
            CombatInputWindow window = _readInputWindow();
            if (!window.CanShowGuidance || window.BattleInstanceId != session.BattleInstanceId)
            {
                _ui.ClearHighlights();
                return;
            }
            _locator.LocatePractice(_practice, window.StagePhase, _frame);
            _ui.RefreshPracticeHighlights(_frame);
        }

        private TutorialSessionSnapshot ReadActiveSession()
        {
            int id = _tutorial.Context.ActiveSessionId;
            if (id == 0) return null;
            if (!_tutorial.TryGetSession(id, out TutorialSessionSnapshot session))
                throw new InvalidOperationException($"[TutorialViewUIDisplay] 활성 튜토리얼 세션 {id}의 기록이 없습니다.");
            return session;
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
            _practice = null;
            _practiceSession = null;
            _frame.Clear();
            _obstacles.Clear();
            List<Exception> errors = null;
            CleanupErrors.Try(ClearModelHighlights, ref errors);
            if (_ui != null)
                CleanupErrors.Try(_ui.CloseAll, ref errors);
            CleanupErrors.ThrowIfAny(errors, "[TutorialViewUIDisplay] 튜토리얼 표시 정리에 실패했습니다.");
        }

        private void ClearModelHighlights()
        {
            _locator?.ClearModelBounds();
            _models.Clear();
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
