namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display
{
    /// <summary>
    /// 씬 연결 한 번의 표시가 해제됐는지입니다(씬 표시 나누기 설계 DS1). 해제 뒤 규칙(D9)의 원본은 이 사실 하나입니다.
    /// </summary>
    /// <remarks>
    /// 표시 해제(<see cref="CombatSceneDisplay.Release"/>)가 첫 줄에서 세우고 되돌리지 않습니다. 연결마다 Factory가 하나 만들어
    /// 표시와 하위 객체(고스트·공격 피드백, 선택 표시, HUD 표면, 행동 버튼)에 같은 것을 넘깁니다(계획 TP-12).
    /// 이 사실로 막는 것은 입력 표시 의도·집기·새 연결뿐입니다. 숨기기·정리·결과 끝 그리기는 막지 않습니다(설계 DS1).
    /// 포인터 매퍼·이동 초안·필드의 자기 해제 상태는 "자기 자원을 놓았음"이라는 다른 사실이라 그대로 둡니다.
    /// </remarks>
    internal sealed class CombatDisplayLifetime
    {
        /// <summary>표시 해제가 시작됐는지입니다.</summary>
        internal bool IsReleased { get; private set; }

        /// <summary>표시 해제가 시작됐다고 적습니다. 여러 번 불려도 안전합니다.</summary>
        internal void MarkReleased() => IsReleased = true;
    }
}
