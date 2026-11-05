using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using System;
using System.Collections.Generic;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Selection;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.ServiceKeys.Runtime;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Hud
{
    /// <summary>전투 선택의 정보 표시와 페이즈별 덱 전환을 맡습니다. 선택 원본은 Display에서 빌립니다.</summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display")]
    [DisallowMultipleComponent]
    public sealed class CombatUnitInfo : UIBase
    {
        public CombatUnitInfo() => Policy = UIOpenPolicy.Exclusive;
        public override UIId ID => UIId.CombatUnitInfo;
        public override UIId ParentID => UIId.Deck;
        public override void BindUISystemService(IUISystemService service)
        {
            base.BindUISystemService(service);
            _ui = service;
        }

        private void Present() => _ui.OpenChildUI(ParentID, ID);
        private void Dismiss() => _ui.CloseUI(ID);

        private const float SlideDuration = 0.12f;
        private RectTransform _panelRect;
        private Vector2 _restPosition;
        private Func<CombatInspectionInfo> _readInspection;
        private bool _shownCharacter;
        internal void BindInspection(Func<CombatInspectionInfo> read) => _readInspection = read;
        private float _slideFromX;
        private float _slideTargetX;
        private float _slideElapsed;
        private bool _sliding;
        private bool _slidingOut;
        [SerializeField] private CombatUnitInfoView view;
        private IUISystemService _ui;
        private ICombatSelectionReader _selection;
        private CombatActionAvailability _availability;
        private bool _allowMove;
        private bool _allowAttack;
        private EntityId _shown;
        private StagePhase? _stage;

        public override void ExportUIService(IUIRegistry registry)
        {
            ValidateView();
            base.ExportUIService(registry);
        }

        public override void BindUIService(ISystemServiceStoreReader resolver)
        {
            base.BindUIService(resolver);
            // 특성 설명은 공용 Hover 패널에 띄웁니다. 다른 화면처럼 Global Context를 거쳐 받습니다(OptionUI와 같은 방식).
            view.BindHoverText(resolver.GetService(SystemGroupsService.Global).UITextPresentation);
        }

        private void ValidateView()
        {
            if (view == null || (view.transform != transform && !view.transform.IsChildOf(transform)))
                throw new InvalidOperationException($"[CombatUnitInfo] UIId.{ID}, '{name}': 같은 객체 또는 자식의 view 참조가 필요합니다.");
            view.ValidateReferences();
        }

        internal void Configure(IUnitVisualTableQuery units, ITraitVisualTableQuery traits, IUISystemService ui,
            ICategoryVisualTableQuery categories, ICombatSelectionReader selection, CombatUnitStatuses statuses)
        {
            if (units == null) throw new ArgumentNullException(nameof(units));
            if (traits == null) throw new ArgumentNullException(nameof(traits));
            if (ui == null) throw new ArgumentNullException(nameof(ui));
            if (categories == null) throw new ArgumentNullException(nameof(categories));
            if (selection == null) throw new ArgumentNullException(nameof(selection));
            ValidateView();
            if (_panelRect == null)
            {
                _panelRect = transform as RectTransform ??
                    throw new InvalidOperationException($"[CombatUnitInfo] UIId.{ID}, '{name}': RectTransform이 필요합니다.");
                _restPosition = _panelRect.anchoredPosition;
            }
            view.BindOwner(this);
            view.Prepare();
            _selection = selection;
            if (statuses == null) throw new ArgumentNullException(nameof(statuses));
            _ui = ui;
            Clear();
        }

        internal void Render(StagePhase stage)
        {
            // 외부 UI 호출이 Clear로 재진입해도 표시 목록을 다시 붙잡지 않도록 먼저 적용합니다.

            StagePhase? previousStage = _stage;
            bool phaseChanged = previousStage != stage;
            if (phaseChanged)
            {
                _stage = stage;
                if (stage == StagePhase.Combat)
                    _ui.CloseUI(UIId.UnitCard);
                // 최초 목록 상태는 Bootstrap이 맡고, 이후 배치 국면 복귀에서만 엽니다.
                else if (stage == StagePhase.Deployment && previousStage.HasValue)
                    _ui.OpenChildUI(UIId.Deck, UIId.UnitCard);
                if (stage != StagePhase.Combat && stage != StagePhase.Deployment)
                    Dismiss();
            }
            // 값 갱신만으로 사용자가 연 카드 목록을 다시 닫지 않는다.
            if ((stage == StagePhase.Combat || stage == StagePhase.Deployment) && (phaseChanged || gameObject.activeSelf))
                ShowSelection();
        }

        internal void ShowSelection()
        {
            if (_stage != StagePhase.Combat && _stage != StagePhase.Deployment) return;
            CombatInspectionInfo info = _readInspection?.Invoke();
            if (info == null)
            {
                if (gameObject.activeSelf && !_slidingOut) SlideToHidden();
                return;
            }
            if (!gameObject.activeSelf)
            {
                ApplyInfo(info);
                SetPanelX(HiddenX);
                Present();
                if (_readInspection?.Invoke()?.Id == info.Id) SlideTo(_restPosition.x, false);
            }
            else if (_shown != info.Id)
            {
                if (!_slidingOut) SlideToHidden();
            }
            else
            {
                ApplyInfo(info);
                if (_slidingOut) SlideTo(_restPosition.x, false);
            }
        }

        private void ApplyInfo(CombatInspectionInfo info)
        {
            if (_shown != info.Id) view.Clear();
            _shown = info.Id;
            _shownCharacter = info.Character != null;
            view.ShowInspection(info);
            ApplyActionAvailability();
        }
        /// <summary>입력 측이 검증한 현재 선택의 응답만 받습니다. 대기·차단 상태는 null입니다.</summary>
        internal void SetActionAvailability(CombatActionAvailability availability, bool allowMove = true, bool allowAttack = true)
        {
            _availability = availability;
            _allowMove = allowMove;
            _allowAttack = allowAttack;
            ApplyActionAvailability();
        }

        private void ApplyActionAvailability()
        {
            bool matches = _shownCharacter && _availability != null && _availability.ActorId == _shown &&
                _selection != null && _selection.SelectedEntityId == _shown;
            view.SetActionAvailability(matches && _allowMove && _availability.CanMove, matches && _allowAttack && _availability.CanAttack);
        }

        // 현재 씬의 좌측 고정 앵커를 기준으로 패널 오른쪽 끝까지 화면 밖으로 숨깁니다.
        private float HiddenX => -_panelRect.rect.width * (1f - _panelRect.pivot.x) - 20f;

        private void SlideToHidden()
        {
            view.ResetHover();
            SlideTo(HiddenX, true);
        }

        private void SlideTo(float targetX, bool slidingOut)
        {
            _slideFromX = _panelRect.anchoredPosition.x;
            _slideTargetX = targetX;
            _slideElapsed = 0f;
            _slidingOut = slidingOut;
            _sliding = true;
        }

        private void Update() => AnimateTransition(Time.unscaledDeltaTime);

        /// <summary>대상 전환의 퇴장·내용 반영·입장을 순서대로 진행합니다.</summary>
        internal void AnimateTransition(float deltaTime)
        {
            if (!_sliding) return;
            _slideElapsed = Mathf.Min(_slideElapsed + deltaTime, SlideDuration);
            SetPanelX(Mathf.Lerp(_slideFromX, _slideTargetX,
                Mathf.SmoothStep(0f, 1f, _slideElapsed / SlideDuration)));
            if (_slideElapsed < SlideDuration) return;
            _sliding = false;
            if (!_slidingOut) return;
            _slidingOut = false;
            CombatInspectionInfo next = _readInspection?.Invoke();
            if (next == null)
            {
                _shown = EntityId.Empty;
                view.Clear();
                Dismiss();
                return;
            }
            ApplyInfo(next);
            SlideTo(_restPosition.x, false);
        }

        private void SetPanelX(float x)
        {
            Vector2 position = _panelRect.anchoredPosition;
            position.x = x;
            _panelRect.anchoredPosition = position;
        }

        protected override void OnUIHidden()
        {
            ResetTransition();
            // 씬 등록 전 Hide도 있으므로 아직 연결되지 않은 view의 정리는 생략합니다.
            if (view != null) view.ResetHover();
        }

        private void ResetTransition()
        {
            _sliding = _slidingOut = false;

            if (_panelRect != null) SetPanelX(_restPosition.x);
        }

        internal void Clear()
        {
            ResetTransition();
            _shown = EntityId.Empty;
            _stage = null;
            _availability = null;
            view.Clear();
            Dismiss();
        }
    }

}
