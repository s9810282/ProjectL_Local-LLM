using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// 포기 버튼·확인창의 표시 계약입니다(포기 UI 설계, SOLID·SSOT 개선 설계 D2).
    /// <see cref="CombatAbandonController"/>는 전투 표시 전체가 아니라 이 계약만 받습니다.
    /// </summary>
    /// <remarks>구현: <see cref="CombatSceneDisplay"/>(<see cref="ICombatBattleDisplay"/>를 통해), 테스트 가짜.</remarks>
    internal interface ICombatAbandonDisplay
    {
        /// <summary>
        /// 포기 버튼·확인창의 입구를 연결합니다. 모두 null이면 연결을 끊습니다.
        /// 씬 연결 해제 뒤 새 연결은 무시하지만 연결 해제는 항상 전달합니다.
        /// </summary>
        void BindAbandon(Action open, Action confirm, Action cancel);

        /// <summary>포기 버튼을 보일지·누를 수 있을지를 적용합니다. 같은 값이면 다시 쓰지 않습니다.</summary>
        void SetAbandonAvailable(bool visible, bool interactable);

        void ShowAbandonConfirm();

        void HideAbandonConfirm();
    }
}
