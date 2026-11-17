using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.ActionButtons;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Hud
{
    /// <summary>
    /// 전투 행동 버튼(공격·턴 종료·이동 되돌리기) 패널의 표시입니다(씬 표시 나누기 설계 DS4, 행동 버튼 설계 B1).
    /// </summary>
    /// <remarks>
    /// 버튼 연결은 조정자가, 보임·누름 가능 맞춤은 입력 표면이 이 계약(<see cref="ICombatActionDisplay"/>)으로만 다룹니다.
    /// 공유 수명이 해제되면 새 연결과 가용성은 무시하지만, 연결 해제(모두 null)는 정리 콜백이라 늘 전달합니다.
    /// 숨기기(<see cref="Hide"/>)·연결 끊기(<see cref="Unbind"/>)는 표시의 정리·해제 목록이 부르므로 해제 여부를 보지 않습니다.
    /// </remarks>
    internal sealed class CombatActionButtonsDisplay : ICombatActionDisplay
    {
        private readonly CombatActionPanel _actions;
        private readonly CombatDisplayLifetime _lifetime;

        internal CombatActionButtonsDisplay(CombatActionPanel actions, CombatDisplayLifetime lifetime)
        {
            _actions = actions != null ? actions : throw new ArgumentNullException(nameof(actions));
            _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
        }

        public void BindActions(Action attack, Action endTurn, Action returnToCheckpoint, Action skill)
        {
            // 해제 뒤 새 연결은 무시하지만, 연결 해제(모두 null)는 정리 콜백이므로 항상 전달한다.
            bool unbind = attack == null && endTurn == null && returnToCheckpoint == null && skill == null;
            if (_lifetime.IsReleased && !unbind)
                return;
            _actions.Bind(attack, endTurn, returnToCheckpoint, skill);
        }

        public void SetActionsAvailable(in CombatActionButtons buttons)
        {
            if (!_lifetime.IsReleased)
                _actions.SetAvailable(buttons);
        }

        /// <summary>전투 정리·씬 연결 해제 때 버튼을 숨깁니다. 해제 목록에서도 불리므로 해제 여부를 보지 않습니다.</summary>
        internal void Hide() => _actions.SetAvailable(default);

        /// <summary>씬 연결 해제 때 세 입구 연결을 끊습니다.</summary>
        internal void Unbind() => _actions.Bind(null, null, null, null);
    }
}
