using System;
using UnityEngine;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    // 기존 씬 표시 구현과의 호환을 위한 조립 계약이다. 소비자는 필요한 작은 계약만 받는다.
    public interface ICombatMovementPresenter : ICombatMovementFeedback, ICombatMovementPlaybackPresenter
    {
    }

    public interface ICombatPointerMapping
    {
        float InnerCellInset { get; }
        bool TryMapPointer(Vector2 screen, out Vector2 grid, out bool overCancelArea);
    }

    // 개별 비이동 표현은 기존/저작 Presenter가 구현한다. 이동 계층은 이 정책을 구현하지 않는다.
    public interface ICombatResultPresenter
    {
        bool CanPresent(CombatEvent combatEvent);
        // 해당 사건의 표시가 끝난 뒤 호출한다. View 소유자가 중복·늦은 콜백의 결과 확인을 차단한다.
        void BeginEvent(CombatEvent combatEvent, Action completed);
    }
}
