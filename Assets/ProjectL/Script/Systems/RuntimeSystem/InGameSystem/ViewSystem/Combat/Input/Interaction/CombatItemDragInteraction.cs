using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction
{
    /// <summary>
    /// 배치 국면에 가방 아이템을 끌어 유닛에 놓는 입력입니다(가방 설계 P4, 결정 ①).
    /// 집은 아이템 키와 드래그 상태를 소유하고, 놓으면 장착 칸이 비었는지만 보고 장착·교체 명령을 Gate로 제출합니다.
    /// </summary>
    /// <remarks>
    /// 장착 가부·재고 차감은 Core가 판정합니다. 가방 수량은 결과가 온 뒤 표시 갱신으로 바뀝니다. 대기는 기록하지 않습니다.
    /// 놓을 대상 강조는 사실(아군 필드 메인·서브)만 보는 표시이고 규칙 판정이 아닙니다(Q-P2 A).
    /// </remarks>
    internal sealed class CombatItemDragInteraction
    {
        private readonly ICombatPointerPicker _picker;
        private readonly ICombatItemDragFeedback _drag;
        private readonly CombatCommandGate _gate;
        private readonly Func<int, int> _stock;
        private readonly CombatHoverTracker _hover;

        private bool _dragging;
        private int _itemKey;

        /// <param name="stock">아이템 키의 지금 재고 수입니다. 조정자가 Buffer에서 읽습니다.</param>
        internal CombatItemDragInteraction(ICombatPointerPicker picker, ICombatInteractionFeedback feedback,
            ICombatItemDragFeedback drag, CombatCommandGate gate, Func<int, int> stock)
        {
            _picker = picker ?? throw new ArgumentNullException(nameof(picker));
            if (feedback == null) throw new ArgumentNullException(nameof(feedback));
            _drag = drag ?? throw new ArgumentNullException(nameof(drag));
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
            _stock = stock ?? throw new ArgumentNullException(nameof(stock));
            _hover = new CombatHoverTracker(feedback, CombatHoverKind.Default);
            InputOwner = new OwnerAdapter(this);
        }

        /// <summary>입력 기능 묶음의 공통 수명에 참여하는 창구입니다(View 2라운드 W5-1).</summary>
        internal CombatInputOwner InputOwner { get; }

        /// <summary>아이템을 집고 있는지입니다.</summary>
        internal bool IsDragging => _dragging;

        /// <summary>집은 아이템 키입니다. 집지 않았으면 0입니다.</summary>
        internal int ItemKey => _itemKey;

        /// <summary>지금 그 아이템을 집을 수 있는지입니다: 장착 명령을 기록할 수 있고 재고가 1 이상입니다.</summary>
        internal bool CanSelect(in CombatInputFacts facts, int itemKey)
        {
            return itemKey > 0
                && CombatInputPolicy.CanSubmit(facts, _gate.HasPending, CombatViewCommandKind.EquipItem)
                && _stock(itemKey) > 0;
        }

        /// <summary>아이템을 집습니다. 집을 수 없으면 아무것도 바꾸지 않고 false입니다.</summary>
        internal bool Select(in CombatInputFacts facts, int itemKey)
        {
            if (!CanSelect(facts, itemKey))
                return false;
            End();
            _dragging = true;
            _itemKey = itemKey;
            _drag.ShowItemDrag(itemKey);
            return true;
        }

        internal void PointerMoved(IReadOnlyList<CharacterData> characters, float screenX, float screenY)
        {
            if (!_dragging)
                return;
            _drag.MoveItemDrag(screenX, screenY);
            if (_picker.TryPickCell(screenX, screenY, out CellCoord cell))
                _hover.Set(cell, FindTarget(characters, cell) != null ? CombatHoverState.Valid : CombatHoverState.Invalid);
            else
                _hover.Clear();
        }

        /// <summary>
        /// 놓습니다. 포인터 칸에 아군 필드 메인·서브가 있으면 장착 칸이 비었으면 장착, 차 있으면 교체를 제출합니다.
        /// 대상이 없으면 제출하지 않습니다. 드래그는 끝내되, 앞 결과가 재생되는 동안(P &lt; S) 대상 위에서 놓았으면 명령이 거절되므로
        /// 끝내지 않고 손에 남깁니다(평가 G3-01). 재생이 끝난 뒤 다음 뗌에서 다시 놓습니다. 제출했으면 true입니다.
        /// </summary>
        internal bool SelectReleased(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, float screenX, float screenY)
        {
            if (!_dragging)
                return false;
            CharacterData target = _picker.TryPickCell(screenX, screenY, out CellCoord cell) ? FindTarget(characters, cell) : null;
            if (target != null && facts.IsPresenting)
                return false;
            int itemKey = _itemKey;
            End();
            if (target == null)
                return false;

            CombatViewCommandData command = target.Items.EquippedItemKey.HasValue
                ? CombatViewCommandData.ReplaceItem(target.EntityId, itemKey)
                : CombatViewCommandData.EquipItem(target.EntityId, itemKey);
            return _gate.TrySubmit(facts, command) != 0;
        }

        /// <summary>
        /// HUD 아이템 아이콘 우클릭 해제입니다(가방 설계 P6, 결정 ②⑥). 해제 명령을 기록할 수 있고, 그 유닛이 필드의 살아 있는 아군 메인·서브이며
        /// 장착 칸이 차 있으면 해제를 제출합니다. 조건이 맞지 않으면(적 포함) 아무것도 하지 않습니다. 제출했으면 true입니다.
        /// </summary>
        /// <remarks>
        /// 메인은 손패로 돌아가지 않아 이 입력이 유일한 해제 경로입니다. 재고 반환은 Core가 하고 가방 수량은 결과 뒤 갱신으로 바뀝니다.
        /// 끄는 중인지는 보지 않습니다. 같은 우클릭이 입력 시스템 취소로도 들어와 드래그를 끝내며, 두 입력의 순서는 정해져 있지 않습니다.
        /// </remarks>
        internal bool RequestUnequip(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, EntityId unitId)
        {
            if (unitId.IsEmpty || characters == null
                || !CombatInputPolicy.CanSubmit(facts, _gate.HasPending, CombatViewCommandKind.UnequipItem))
                return false;
            CharacterData unit = CombatCharacterQueries.FindPlayerInArea(characters, unitId, CharacterRuntimeArea.Field);
            if (unit == null || !unit.Items.EquippedItemKey.HasValue || !IsEquipRole(unit))
                return false;
            return _gate.TrySubmit(facts, CombatViewCommandData.UnequipItem(unitId)) != 0;
        }

        /// <summary>결과 뒤: 배치 국면이 아니거나 집은 아이템의 재고가 없어졌으면 드래그를 끝냅니다.</summary>
        internal void AfterResult(in CombatInputFacts facts)
        {
            if (_dragging && (facts.StagePhase != StagePhase.Deployment || _stock(_itemKey) <= 0))
                End();
        }

        /// <summary>놓지 않고 끝냅니다. 집지 않았으면 아무것도 하지 않습니다.</summary>
        internal void Cancel()
        {
            if (_dragging)
                End();
        }

        private void End()
        {
            if (!_dragging)
                return;
            _dragging = false;
            _itemKey = 0;
            _hover.Clear();
            _drag.HideItemDrag();
        }

        // 장착을 받을 수 있는 유닛은 필드의 살아 있는 아군 메인·서브다(Core 장착 대상과 같은 사실).
        private static CharacterData FindTarget(IReadOnlyList<CharacterData> characters, CellCoord cell)
        {
            if (characters == null)
                return null;
            CharacterData unit = CombatCharacterQueries.FindPlayerAt(characters, cell);
            return unit != null && IsEquipRole(unit) ? unit : null;
        }

        private static bool IsEquipRole(CharacterData unit) =>
            unit.OwnershipRole == CharacterOwnershipRole.Main || unit.OwnershipRole == CharacterOwnershipRole.Sub;

        /// <summary>공통 수명의 단계마다 아이템 드래그의 어느 메서드인지만 답합니다. 표현 정리·강제 선택 종료는 드래그를 놓지 않고 끝냅니다.</summary>
        private sealed class OwnerAdapter : CombatInputOwner
        {
            private readonly CombatItemDragInteraction _item;

            internal OwnerAdapter(CombatItemDragInteraction item) => _item = item;

            internal override CombatInputRouter.PointerTarget Target => CombatInputRouter.PointerTarget.Item;
            internal override bool IsActive => _item.IsDragging;

            internal override void PointerMoved(in CombatPointerInput input) =>
                _item.PointerMoved(input.Characters, input.ScreenX, input.ScreenY);

            internal override void SelectReleased(in CombatPointerInput input) =>
                _item.SelectReleased(input.Facts, input.Characters, input.ScreenX, input.ScreenY);

            internal override void SelectCanceled() => _item.Cancel();

            internal override void AfterResult(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters) =>
                _item.AfterResult(facts);

            internal override void CancelPresentation() => _item.Cancel();
        }
    }
}
