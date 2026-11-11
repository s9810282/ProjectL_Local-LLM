using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Turn;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Hud
{
    /// <summary>
    /// 결과 끝에 그리는 화면 공간 UI(HUD 골드·턴 표시·가방 재고·정보창)입니다(씬 표시 나누기 설계 DS4). 가방 끌기 그림도 가방에 넘깁니다.
    /// </summary>
    /// <remarks>
    /// 그리기를 <see cref="RenderStatus"/>·<see cref="RenderPanels"/> 둘로 나눈 까닭: 그 사이에 진입 배치 표시가 같은 턴 표시 UI·덱 카드를 다루므로
    /// 표시(<see cref="CombatSceneDisplay"/>)가 그 차례를 지킵니다(설계 §3.8). 정보창 비우기의 주인은 여기 하나입니다.
    /// 가방 끌기 그림은 공유 수명이 해제되면 무시합니다. 가방(<see cref="ICombatPackDisplay"/>)은 다른 작업자 UI라 계약을 바꾸지 않고 넘기기만 합니다(설계 P6).
    /// </remarks>
    internal sealed class CombatHudSurfaces : ICombatItemDragFeedback
    {
        private readonly CombatHud _hud;
        private readonly CombatTurnDisplayPresenter _turnDisplay;
        // 가방 표시입니다(가방 설계 P1). 씬 루트 참조 검사가 연결을 보장합니다.
        private readonly ICombatPackDisplay _pack;
        private readonly CombatUnitInfo _unitInfo;
        private readonly CombatDisplayLifetime _lifetime;

        internal CombatHudSurfaces(CombatHud hud, CombatTurnDisplayPresenter turnDisplay, ICombatPackDisplay pack, CombatUnitInfo unitInfo,
            CombatDisplayLifetime lifetime)
        {
            _hud = hud != null ? hud : throw new ArgumentNullException(nameof(hud));
            _turnDisplay = turnDisplay ?? throw new ArgumentNullException(nameof(turnDisplay));
            _pack = pack ?? throw new ArgumentNullException(nameof(pack));
            _unitInfo = unitInfo != null ? unitInfo : throw new ArgumentNullException(nameof(unitInfo));
            _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
        }

        /// <summary>전투 설치 때 HUD를 엽니다.</summary>
        internal void Present() => _hud.Present();

        /// <summary>HUD 골드 → 턴 표시입니다.</summary>
        internal void RenderStatus(int runGold, CombatTurnFacts turn, CombatFlowPhase phase)
        {
            _hud.Render(runGold);
            _turnDisplay.Render(turn, phase);
        }

        /// <summary>가방 재고 → 정보창입니다. 재고는 골드와 같은 결과 끝 그리기에서 넘기고, 가방은 받은 스냅숏만 그립니다(가방 설계 P1).</summary>
        internal void RenderPanels(ItemInventorySnapshot inventory, StagePhase stage)
        {
            _pack.ShowInventory(inventory);
            _unitInfo.Render(stage);
        }

        internal void ClearHud() => _hud.Clear();

        internal void ClearTurn() => _turnDisplay.Clear();

        // 정리 경로는 씬이 내려가며 이미 파괴된 Unity 객체를 만날 수 있어 Unity의 null 비교를 남깁니다(생성자에서는 필수).
        internal void ClearUnitInfo()
        {
            if (_unitInfo != null) _unitInfo.Clear();
        }

        // 끄는 아이템 그림은 가방이 그린다(가방 설계 P4).
        void ICombatItemDragFeedback.ShowItemDrag(int itemKey)
        {
            if (!_lifetime.IsReleased) _pack.ShowItemDrag(itemKey);
        }

        void ICombatItemDragFeedback.MoveItemDrag(float screenX, float screenY)
        {
            if (!_lifetime.IsReleased) _pack.MoveItemDrag(screenX, screenY);
        }

        void ICombatItemDragFeedback.HideItemDrag()
        {
            if (!_lifetime.IsReleased) _pack.HideItemDrag();
        }
    }
}
