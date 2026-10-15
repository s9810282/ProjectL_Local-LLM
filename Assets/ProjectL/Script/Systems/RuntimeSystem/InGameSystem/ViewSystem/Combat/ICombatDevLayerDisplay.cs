using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// RecordInputOnly 개발 Layer 패널의 표시 계약입니다(설계 §8.1, W20, SOLID·SSOT 개선 설계 D3).
    /// <see cref="CombatDevLayerController"/>는 전투 표시 전체가 아니라 이 계약만 받습니다.
    /// </summary>
    /// <remarks>구현: <see cref="CombatSceneDisplay"/>(<see cref="ICombatBattleDisplay"/>를 통해), 테스트 가짜.</remarks>
    internal interface ICombatDevLayerDisplay
    {
        /// <summary>패널을 켜고 버튼에 입구를 연결합니다. 이미 연결돼 있으면 다시 연결하지 않습니다.</summary>
        void ShowDevLayerPanel(Action onDeployment, Action onCombat);

        /// <summary>패널의 연결을 끊고 끕니다. 멱등적입니다.</summary>
        void HideDevLayerPanel();
    }
}
