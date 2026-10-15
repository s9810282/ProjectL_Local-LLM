using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageField.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.AreaOutlineSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// Combat 씬의 Presenter(필드·유닛·손패·HUD·고스트·포기 패널)에 값과 표시 의도를 나눠 주는 구현입니다(계획 D.5·E).
    /// 정적 필드 구성은 설치 때 한 번, 동적 값은 결과마다 반영합니다. 개발 Layer 패널 연결도 맡습니다.
    /// 화면 좌표의 칸·손패 집기와 격자 매핑은 내부 <see cref="CombatPointerMapper"/>에 위임합니다(구조 개선 VR4).
    /// </summary>
    /// <remarks>
    /// 씬 연결(Attach) 하나에 하나 만들어지고 Release 뒤 다시 쓰지 않습니다. Buffer를 읽지 않고 받은 값만 씁니다.
    /// 외형·배경 프랍은 View DB, 색은 Render ColorSystem을 씁니다. 사건 사실·보간은 내부 사건 Presenter에 위임합니다.
    /// 사건 시작은 Install 전·Clear/Release 후에 거절합니다. Tick과 취소는 진행 중인 사건이 없으면 아무것도 하지 않습니다.
    /// </remarks>
    internal sealed class CombatSceneDisplay : ICombatBattleDisplay, IUIInputReceiver
    {
        // 개발 Layer 패널 버튼 슬롯입니다. 입력은 UIInputBinding이 이 표시에 전달합니다(Assets/AGENTS.md, 개선 설계 D1).
        private const int DevDeploymentSlot = 0;
        private const int DevCombatSlot = 1;

        private readonly FieldView _field;
        private readonly FieldUnitPresenter _units;
        private readonly HandPresenter _hand;
        private readonly CombatHud _hud;
        private readonly PlacementGhost _ghost;
        private readonly CombatAbandonPanel _abandon;
        // 화면 좌표 → 칸·손패·격자 변환과 UI 가림 Raycast(구조 개선 VR4). Install 때 연결, Clear 때 해제, Release 때 최종 해제.
        private readonly CombatPointerMapper _pointer;
        private readonly GameObject _devPanel;
        private readonly Button _devDeploymentButton;
        private readonly Button _devCombatButton;
        private readonly StageFieldViewData _fieldConfig;
        private readonly IColorSystemService _colors;
        private readonly IAreaOutlineSystemService _areaOutlines;
        private readonly CombatSceneEventPresenter _events;
        private readonly StageViewPropPresenter _props;

        private Action _onDevDeployment;
        private Action _onDevCombat;
        private bool _isDevInputAttached;
        private bool _isReleased;
        private EntityId _hudPreviewActor;
        private readonly List<CellCoord> _blockedWallCells = new();
        private readonly HashSet<CellCoord> _blockedWallSet = new();
        public float InnerCellInset => _pointer.InnerCellInset;
        public bool IsMovementComplete => _events.IsMovementComplete;

        internal CombatSceneDisplay(
            CombatSceneRoot root,
            StageFieldViewData fieldConfig,
            IUnitVisualTableQuery unitVisuals,
            IStageViewPropTableQuery stageProps,
            IColorSystemService colors,
            ITraitVisualTableQuery traitVisuals,
            IAreaOutlineSystemService areaOutlines)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            _fieldConfig = fieldConfig ?? throw new ArgumentNullException(nameof(fieldConfig));
            if (unitVisuals == null)
                throw new ArgumentNullException(nameof(unitVisuals));
            // 특성 외형은 유닛 HUD 표시에 필요한 필수 의존성이다(구조 개선 VR5). 기본값 null로 숨기지 않는다.
            if (traitVisuals == null)
                throw new ArgumentNullException(nameof(traitVisuals));
            _colors = colors ?? throw new ArgumentNullException(nameof(colors));
            // GridOutline 표시 모드가 칸 테두리를 그릴 때 쓴다. CellPrefab 모드에서도 같은 조립 경로라 필수로 받는다.
            _areaOutlines = areaOutlines ?? throw new ArgumentNullException(nameof(areaOutlines));

            root.ValidateReferences();
            _field = root.Field;
            _units = root.FieldUnits;
            _hand = root.Hand;
            _hud = root.Hud;
            _ghost = root.Ghost;
            _abandon = root.AbandonPanel;
            _devPanel = root.DevLayerPanel;
            _devDeploymentButton = root.DevDeploymentButton;
            _devCombatButton = root.DevCombatButton;
            _pointer = new CombatPointerMapper(_field, _hand, root.WorldCamera, root.InnerCellInset);

            _units.Configure(unitVisuals, fieldConfig.FieldUnitScale, traitVisuals, root.WorldCamera);
            _events = new CombatSceneEventPresenter(_units, root.MovementStepSeconds);
            _hand.Configure(unitVisuals);
            _props = new StageViewPropPresenter(stageProps, _field.CellsRoot);
            _ghost.Configure(unitVisuals, fieldConfig, colors);
        }

        #region 표시

        public void Install(in CombatDisplayValues values)
        {
            ThrowIfReleased();
            // Build 전 실패라도 이전 Mapper로 사건 표시·좌표 변환이 재개되지 않도록 먼저 연결을 끊는다.
            _events.Clear();
            _pointer.Unbind();
            try
            {
                _hud.HideCompletion();
                CombatStateData state = values.State;
                _field.Build(state.BoardWidth, state.BoardHeight, _fieldConfig, _colors, _areaOutlines);
                _events.Bind(_field.Mapper);
                _pointer.Bind(_field.Mapper);
                _field.ApplyStaticHighlights(state.PlayerDeploymentDepth, state.EnemyDeploymentDepth, state.RailCells);
                _props.Show(values.StageKey);
                Refresh(values);
            }
            catch
            {
                // 연결됐던 대상까지 끊는다. 다음 Install에서 다시 연결한다.
                _events.Clear();
                _pointer.Unbind();
                throw;
            }
        }

        public void Refresh(in CombatDisplayValues values)
        {
            ThrowIfReleased();
            CombatStateData state = values.State;
            _units.SetHudTimeline(state.CommandState.ActiveActorId, values.Timeline);
            _events.Refresh(values.Characters, state.Structures);
            // 손패 카드는 배치 단계에서만 보인다(손패 카드화 설계 C8). 표시 조건은 Core 국면을 따른다.
            _hand.Render(values.Characters,
                state.CommandState.Turn.StagePhase == StagePhase.Deployment && state.Phase != CombatFlowPhase.Finished);
            _hud.Render(values.StageGold, values.RemainingDraws);
            RenderActiveActor(values);
        }

        /// <summary>현재 행위자가 점유한 칸을 강조합니다(설계 §21.1). 행위자가 없거나 필드에 없으면 지웁니다.</summary>
        private void RenderActiveActor(in CombatDisplayValues values)
        {
            EntityId actorId = values.State.CommandState.ActiveActorId;
            if (!actorId.IsEmpty)
            {
                IReadOnlyList<CharacterData> characters = values.Characters;
                for (int i = 0; i < characters.Count; i++)
                {
                    CharacterData character = characters[i];
                    if (character.EntityId != actorId || character.Area != CharacterRuntimeArea.Field)
                        continue;

                    _field.SetLayerCells(FieldHighlightLayer.ActiveActor, character.OccupiedCells);
                    return;
                }
            }

            _field.ClearLayer(FieldHighlightLayer.ActiveActor);
        }

        public void Clear()
        {
            ThrowIfReleased();
            ClearHudCostPreview();
            _events.Clear();
            _pointer.Unbind();
            RunAll(_ghost.Hide, _units.Release, _hand.Release, _hud.Clear, _props.Release, _field.Release);
        }

        public void Release()
        {
            if (_isReleased)
                return;

            ClearHudCostPreview();
            _isReleased = true;
            _pointer.Release();
            // 씬 오브젝트는 씬과 함께 사라지므로 풀 반환과 참조 정리만 한다. 셀 색은 되돌리지 않는다.
            _events.Clear();
            RunAll(() => _hud.BindReturn(null), _hud.HideCompletion, HideDevLayerPanelCore,
                () => _abandon.Bind(null, null, null), _abandon.HideConfirm,
                _ghost.Hide, _units.Release, _hand.Release, _props.Release, _field.Release);
        }

        public void ShowCompletion(BattleCompletionChanges completion, Action confirmed)
        {
            ThrowIfReleased();
            _hud.ShowCompletion(completion, confirmed);
        }

        #endregion

        #region 개발 Layer 패널 (W20)

        public void ShowDevLayerPanel(Action onDeployment, Action onCombat)
        {
            if (onDeployment == null)
                throw new ArgumentNullException(nameof(onDeployment));
            if (onCombat == null)
                throw new ArgumentNullException(nameof(onCombat));
            ThrowIfReleased();
            if (_onDevDeployment != null)
                return;

            if (!_isDevInputAttached)
            {
                UIInputBinding.Attach(_devDeploymentButton, this, DevDeploymentSlot);
                UIInputBinding.Attach(_devCombatButton, this, DevCombatSlot);
                _isDevInputAttached = true;
            }

            _onDevDeployment = onDeployment;
            _onDevCombat = onCombat;
            if (!_devPanel.activeSelf)
                _devPanel.SetActive(true);
        }

        public void HideDevLayerPanel()
        {
            HideDevLayerPanelCore();
        }

        void IUIInputReceiver.Click(int slot)
        {
            if (_isReleased)
                return;

            if (slot == DevDeploymentSlot)
                _onDevDeployment?.Invoke();
            else if (slot == DevCombatSlot)
                _onDevCombat?.Invoke();
        }

        void IUIInputReceiver.HoverEnter(int slot)
        {
        }

        void IUIInputReceiver.HoverExit(int slot)
        {
        }

        private void HideDevLayerPanelCore()
        {
            _onDevDeployment = null;
            _onDevCombat = null;

            if (_devPanel != null && _devPanel.activeSelf)
                _devPanel.SetActive(false);
        }

        #endregion

        #region 포기 버튼·확인창 (포기 UI 설계)

        public void BindAbandon(Action open, Action confirm, Action cancel)
        {
            // 해제 뒤 새 연결은 무시하지만, 연결 해제(모두 null)는 정리 콜백이므로 항상 전달한다(BindReturn과 같은 규칙).
            bool unbind = open == null && confirm == null && cancel == null;
            if (_isReleased && !unbind)
                return;
            _abandon.Bind(open, confirm, cancel);
        }

        public void SetAbandonAvailable(bool visible, bool interactable)
        {
            if (!_isReleased)
                _abandon.SetAvailable(visible, interactable);
        }

        public void ShowAbandonConfirm()
        {
            if (!_isReleased)
                _abandon.ShowConfirm();
        }

        public void HideAbandonConfirm()
        {
            if (!_isReleased)
                _abandon.HideConfirm();
        }

        #endregion

        #region 집기·좌표 변환 (ICombatPointerPicker·ICombatPointerMapping, PointerMapper에 위임)

        public bool TryPickCell(float screenX, float screenY, out CellCoord cell) => _pointer.TryPickCell(screenX, screenY, out cell);

        public bool TryPickHandUnit(float screenX, float screenY, out EntityId entityId) =>
            _pointer.TryPickHandUnit(screenX, screenY, out entityId);

        public bool IsOverHandDropZone(float screenX, float screenY) => _pointer.IsOverHandDropZone(screenX, screenY);

        public bool TryMapPointer(Vector2 screen, out Vector2 grid, out bool overCancelArea) =>
            _pointer.TryMapPointer(screen, out grid, out overCancelArea);

        #endregion

        #region 입력 중 표시 의도 (ICombatInteractionFeedback)

        public void ShowGhost(EntityKey entityKey)
        {
            if (_isReleased)
                return;

            _ghost.Show(entityKey);
            // 고스트가 보이는 동안이 배치 드래그다(격자 아웃라인 설계 F6). 입력 쪽 계약은 바꾸지 않는다.
            _field.SetPlacementActive(true);
        }

        public void MoveGhostToCell(CellCoord cell)
        {
            FieldGridMapper mapper = _field.Mapper;
            if (!_isReleased && mapper != null && mapper.IsInside(cell))
                _ghost.MoveTo(mapper.CellCenterToWorld(cell));
        }

        public void MoveGhostToPointer(float screenX, float screenY)
        {
            if (!_isReleased && _pointer.TryRayToFieldPlane(screenX, screenY, out Vector3 point))
                _ghost.MoveTo(point);
        }

        public void HideGhost()
        {
            if (_isReleased)
                return;

            _ghost.Hide();
            _field.SetPlacementActive(false);
        }

        public void HideOriginal(EntityId entityId)
        {
            if (_isReleased)
                return;

            // 개체를 가진 Presenter만 실제로 숨긴다. 부르는 쪽은 어디에 있는지 알 필요가 없다(설계 §3).
            _units.SetHiddenEntity(entityId);
            _hand.SetHiddenEntity(entityId);
        }

        public void ShowOriginal()
        {
            if (_isReleased)
                return;

            _units.ClearHiddenEntity();
            _hand.ClearHiddenEntity();
        }

        public void SetGhostPlaceable(bool placeable)
        {
            if (!_isReleased)
                _ghost.SetPlaceable(placeable);
        }

        public void SetHoverCell(CellCoord cell, CombatHoverState state)
        {
            if (!_isReleased)
                _field.SetHoverCell(cell, state);
        }

        public void ClearHoverCell()
        {
            if (!_isReleased)
                _field.ClearHoverCell();
        }

        public void ShowRangeCells(IReadOnlyList<CellCoord> cells)
        {
            if (!_isReleased)
                _field.SetLayerCells(FieldHighlightLayer.Range, cells);
        }

        public void ClearRangeCells()
        {
            if (!_isReleased)
                _field.ClearLayer(FieldHighlightLayer.Range);
        }

        public void ShowAffectedCells(IReadOnlyList<CellCoord> cells)
        {
            if (!_isReleased)
                _field.SetLayerCells(FieldHighlightLayer.Affected, cells);
        }

        public void ClearAffectedCells()
        {
            if (!_isReleased)
            {
                _field.ClearLayer(FieldHighlightLayer.Affected);
                ClearHudCostPreview();
            }
        }

        public void ShowAttackCostPreview(EntityId actorId, int cost)
        {
            if (_isReleased) return;
            ClearHudCostPreview();
            _hudPreviewActor = actorId;
            _units.ShowHudCostPreview(actorId, cost);
        }

        public void ShowAttackBlockers(IReadOnlyList<AttackBlockedContact> contacts)
        {
            if (_isReleased) return;
            if (contacts == null) throw new ArgumentNullException(nameof(contacts));
            _blockedWallCells.Clear();
            _blockedWallSet.Clear();
            foreach (var contact in contacts)
                foreach (var cell in contact.WallCells)
                    if (_blockedWallSet.Add(cell)) _blockedWallCells.Add(cell);
            _field.SetLayerCells(FieldHighlightLayer.BlockedWall, _blockedWallCells);
        }

        public void ClearAttackBlockers()
        {
            if (_isReleased) return;
            _blockedWallCells.Clear();
            _blockedWallSet.Clear();
            _field.ClearLayer(FieldHighlightLayer.BlockedWall);
        }

        private void ClearHudCostPreview()
        {
            _units.ClearHudPreview(_hudPreviewActor);
            _hudPreviewActor = EntityId.Empty;
        }

        #endregion

        /// <summary>
        /// Return 버튼 연결입니다. 해제 뒤 새 연결은 무시하지만, 연결 해제(null)는 정리 콜백이므로 항상 전달합니다.
        /// </summary>
        public void BindReturn(Action requested)
        {
            if (_isReleased && requested != null)
                return;
            _hud.BindReturn(requested);
        }

        public void ShowDraft(MovementPath path, MovePreview? preview, bool pointerValid, CellCoord? candidate = null)
        {
            // 해제 뒤 늦게 도착한 표시 호출은 무시한다(다른 표시 의도 메서드와 같은 계약).
            if (_isReleased)
                return;

            _field.SetLayerCells(FieldHighlightLayer.MovementDraft, path.Cells);
            CombatHoverState endpoint = !pointerValid ? CombatHoverState.Invalid
                : !preview.HasValue ? CombatHoverState.Neutral
                : preview.Value.CanMove ? CombatHoverState.Valid : CombatHoverState.Invalid;
            _field.SetHoverCell(candidate ?? path.Endpoint, endpoint);
            _hud.ShowMovement(preview, pointerValid);
            ClearHudCostPreview();
            if (pointerValid && preview?.CanMove == true && preview.Value.ProjectedActor != null)
            {
                _hudPreviewActor = preview.Value.ProjectedActor.EntityId;
                _units.ShowHudCostPreview(_hudPreviewActor, preview.Value.QuotedActionCost);
            }
        }

        public void ClearDraft()
        {
            if (_isReleased)
                return;

            _field.ClearLayer(FieldHighlightLayer.MovementDraft);
            _field.ClearHoverCell();
            _hud.ShowMovement(null, true);
            ClearHudCostPreview();
        }

        public void SetPathPlanning(bool planning)
        {
            // 격자 아웃라인 모드는 경로 지정 중에 격자를 켠다(시작 칸을 고르기 전부터).
            if (!_isReleased)
                _field.SetPathPlanning(planning);
        }

        public void SetReturnState(CheckpointReturnPreview preview)
        {
            if (!_isReleased)
                _hud.ShowReturn(preview);
        }

        public void BeginStep(CombatCharacterEvent step) => _events.BeginStep(step);
        public void ShowReturn(CombatCharacterEvent returned) => _events.ShowReturn(returned);
        public void CancelMovement() => _events.CancelMovement();
        public void TickPresentation(float deltaTime) => _events.TickPresentation(deltaTime);
        public void CancelPresentation() => _events.CancelPresentation();
        public bool CanPresent(CombatEvent e) => _events.CanPresent(e);
        public void BeginEvent(CombatEvent e, Action completed) => _events.BeginEvent(e, completed);

        private void ThrowIfReleased()
        {
            if (_isReleased)
                throw new InvalidOperationException("[CombatSceneDisplay] 씬 연결이 해제된 표시입니다.");
        }

        /// <summary>하나가 실패해도 나머지를 실행하고 실패를 모아 던집니다.</summary>
        private static void RunAll(params Action[] actions)
        {
            List<Exception> errors = null;
            for (int i = 0; i < actions.Length; i++)
            {
                try
                {
                    actions[i]();
                }
                catch (Exception exception)
                {
                    (errors ??= new List<Exception>()).Add(exception);
                }
            }

            if (errors != null)
                throw new AggregateException("[CombatSceneDisplay] 표시 정리 중 실패했습니다.", errors);
        }
    }
}
