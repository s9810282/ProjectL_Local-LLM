using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Feedback;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Selection
{
    /// <summary>
    /// 선택과 전투 목표의 표시입니다(씬 표시 나누기 설계 DS3). 정보창 선택·유닛 HUD 선택·선택 강조·목표 강조·행동 커서를 맞추고,
    /// 정보창 누름(<see cref="InspectUnit"/>)을 받습니다.
    /// </summary>
    /// <remarks>
    /// 언제 부를지는 표시(<see cref="CombatSceneDisplay"/>)가 정합니다. 결과 끝 그리기는 <see cref="Synchronize"/> → <see cref="SetObjective"/>
    /// → (사실 그리기) → <see cref="AfterFacts"/> → (HUD·패널) → <see cref="RenderHighlights"/> 차례입니다(설계 §3.8).
    /// 선택이 바뀔 때 알림(<see cref="RefreshInspectionSelection"/>)은 Factory가 정보창 Presenter에 겁니다.
    /// 정보창 누름은 공유 수명이 해제되면 무시하고, 정보창 UI 콜백 뒤에도 한 번 더 봅니다(그 콜백이 연결을 놓을 수 있음).
    /// </remarks>
    internal sealed class CombatSelectionDisplay : ICombatUnitInspection
    {
        private readonly CombatSelectionState _selection;
        private readonly CombatInspectionPresenter _inspection;
        private readonly UnitHUDLayer _unitHudLayer;
        private readonly FieldView _field;
        private readonly FieldUnitPresenter _units;
        private readonly ICombatInspectionFactsReader _facts;
        private readonly CombatDisplaySnapshot _snapshot;
        private readonly CombatPointerMapper _pointer;
        private readonly CombatDisplayLifetime _lifetime;
        // 전투 목표(지정 보스)입니다. 결과 끝 그리기가 Buffer 원본으로 정하고, 강조는 보인 사실로 그립니다(평가 C1-14).
        private EntityId _objectiveTarget;

        /// <param name="facts">선택·목표가 읽는 보인 사실입니다(사건 Presenter가 가짐). 결과 재생 중 사건마다 바뀝니다.</param>
        /// <param name="snapshot">행동 커서가 읽는 완료 표시 목록입니다.</param>
        /// <param name="pointer">정보창 누름의 UI 가림·칸 집기입니다.</param>
        internal CombatSelectionDisplay(CombatSelectionState selection, CombatInspectionPresenter inspection, UnitHUDLayer unitHudLayer,
            FieldView field, FieldUnitPresenter units, ICombatInspectionFactsReader facts, CombatDisplaySnapshot snapshot,
            CombatPointerMapper pointer, CombatDisplayLifetime lifetime)
        {
            _selection = selection ?? throw new ArgumentNullException(nameof(selection));
            _inspection = inspection ?? throw new ArgumentNullException(nameof(inspection));
            _unitHudLayer = unitHudLayer != null ? unitHudLayer : throw new ArgumentNullException(nameof(unitHudLayer));
            _field = field != null ? field : throw new ArgumentNullException(nameof(field));
            _units = units != null ? units : throw new ArgumentNullException(nameof(units));
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
            _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            _pointer = pointer ?? throw new ArgumentNullException(nameof(pointer));
            _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
        }

        public ICombatSelectionReader Selection => _selection;

        public void ApplyResult(CombatResult result)
        {
            if (_lifetime.IsReleased || !result.Accepted || result.Kind != CombatProcessingKind.Command)
                return;

            // 배치 확정 뒤 최종 표시가 빈 선택을 읽어 정보창을 닫습니다.
            if (result.CommandKind == CombatCommandKind.PlaceMainCharacter || result.CommandKind == CombatCommandKind.PlaceUnit)
                _selection.SelectCell(null);
        }

        /// <summary>정보창 사실을 이번 결과의 단계·보유 기록·특성 설정으로 맞춥니다.</summary>
        internal void Synchronize(StagePhase stage, IReadOnlyList<OwnedCharacterRecord> owned, BattleTraitSettings traitSettings) =>
            _inspection.Synchronize(stage, owned, traitSettings);

        /// <summary>
        /// 전투 목표 대상을 정합니다. 전투 동안 바뀌지 않지만 Buffer 원본을 매번 따릅니다(보스 활성화 설계 D3).
        /// 사실 그리기 전에 불러, 그 그리기가 부르는 강조 맞춤(<see cref="RefreshInspectionSelection"/>)이 이번 목표를 쓰게 합니다(평가 C1-14).
        /// </summary>
        internal void SetObjective(BattleObjective objective) => _objectiveTarget = CombatObjectiveDisplay.TargetOf(objective);

        /// <summary>사실 그리기 뒤: 행동 커서를 맞추고 유닛 HUD 선택을 맞춥니다.</summary>
        internal void AfterFacts()
        {
            RefreshActionCursors();
            _unitHudLayer.SetSelectedEntity(_selection.SelectedEntityId);
        }

        /// <summary>
        /// 결과 끝 그리기의 마지막: 선택 아군과 전투 목표(보스 활성화 설계 D3)가 점유한 칸을 강조합니다. 둘 다 최종 사실로 바뀐 보인 사실에서 읽습니다.
        /// </summary>
        internal void RenderHighlights()
        {
            RenderSelectedActor();
            RenderObjectiveTarget();
        }

        /// <summary>정보창 선택이 바뀔 때마다: 유닛 HUD 선택 → 선택 강조 → 행동 커서 → 목표 강조입니다.</summary>
        internal void RefreshInspectionSelection()
        {
            _unitHudLayer.SetSelectedEntity(_selection.SelectedEntityId);
            RenderSelectedActor();
            RefreshActionCursors();
            // 전투 목표 강조도 보인 사실로 사건마다 맞춥니다(평가 C1-14). 선택이 바뀔 때도 불리지만 칸이 같으면 다시 그리지 않습니다.
            RenderObjectiveTarget();
        }

        /// <summary>정보창 선택·유닛 HUD 선택·전투 목표를 비웁니다. 선택 바뀜 알림은 내지 않습니다.</summary>
        internal void Clear()
        {
            _inspection.Clear();
            _unitHudLayer.SetSelectedEntity(EntityId.Empty);
            _objectiveTarget = EntityId.Empty;
        }

        /// <summary>정보창 Presenter를 해제합니다(표시 해제 목록).</summary>
        internal void ReleaseInspection() => _inspection.Release();

        public void Deselect()
        {
            if (!_lifetime.IsReleased)
                _inspection.SelectCell(null);
        }

        public void InspectUnit(float screenX, float screenY)
        {
            if (_lifetime.IsReleased || _pointer.IsPointerOverUi(screenX, screenY)) return;
            _inspection.SelectCell(_pointer.TryPickCell(screenX, screenY, out CellCoord cell) ? cell : (CellCoord?)null);
            _unitHudLayer.SetSelectedEntity(_selection.SelectedEntityId);
            // 정보창의 UI 콜백에서 씬 연결이 해제될 수 있습니다.
            if (_lifetime.IsReleased) return;
            RenderSelectedActor();
        }

        private void RefreshActionCursors()
        {
            if (_snapshot.Turn != null)
                _units.RefreshActionCursors(_snapshot.Characters, _snapshot.Turn, _snapshot.Phase, _selection.SelectedActorId);
        }

        /// <summary>선택 아군이 점유한 칸을 보인 사실(선택 reader)에서 읽어 강조합니다. 선택 아군이 없으면 강조를 지웁니다.</summary>
        private void RenderSelectedActor()
        {
            IReadOnlyList<CellCoord> cells = _selection.SelectedActorCells;
            if (cells.Count == 0)
                _field.ClearLayer(FieldHighlightLayer.ActiveActor);
            else
                _field.SetLayerCells(FieldHighlightLayer.ActiveActor, cells);
        }

        /// <summary>
        /// 전투 목표가 점유한 칸을 목표 층에 둡니다. 선택 강조와 같은 보인 사실에서 읽어, 결과 재생 중 넉백·이동·사망을 사건마다 따라갑니다(평가 C1-14).
        /// 목표가 없거나, 필드에 없거나(손패·낮 대기 영역·제외), 죽었으면(보인 사실이 거름) 층을 지웁니다. 같은 ID가 구조물이면 그리지 않습니다.
        /// </summary>
        private void RenderObjectiveTarget()
        {
            if (_facts.TryGet(_objectiveTarget, out CombatInspectionFacts facts) && facts.Character != null)
                _field.SetLayerCells(FieldHighlightLayer.ObjectiveTarget, facts.Cells);
            else
                _field.ClearLayer(FieldHighlightLayer.ObjectiveTarget);
        }
    }
}
