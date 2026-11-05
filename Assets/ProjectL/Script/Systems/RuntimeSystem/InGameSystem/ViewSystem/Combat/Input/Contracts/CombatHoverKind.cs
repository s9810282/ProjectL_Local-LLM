using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts
{
    /// <summary>
    /// 호버 칸을 띄운 입력의 종류입니다. 같은 가능 여부라도 종류마다 색을 따로 고를 수 있게 합니다.
    /// 종류는 호버를 가진 입력 소유자(<see cref="CombatHoverTracker"/>)가 정하고, 색은 표시 쪽 <c>FieldHighlightLayers.HoverColor</c> 한 곳이 고릅니다.
    /// </summary>
    internal enum CombatHoverKind
    {
        /// <summary>색을 따로 두지 않은 호버입니다(배치·아이템 Drag, 이동 끝점).</summary>
        Default = 0,
        /// <summary>공격 조준 호버입니다. 영향 칸 채우기와 막힌 벽 층도 이 종류의 색을 씁니다.</summary>
        Attack = 1
    }
}
