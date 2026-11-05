using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players
{
    /// <summary>
    /// 활성 출처(공격·포효)가 사건 완료를 알려 남은 연출과 겹쳐도 되는지입니다(연출 조립 설계 B12 보완 3).
    /// 원본은 공격 부품의 <see cref="CombatHitGate.AllowsOverlap"/>이고, 공격 재생기가 구현합니다.
    /// </summary>
    internal interface ICombatSourceOverlap
    {
        /// <summary>지금 값입니다. 시작 조건이 씁니다.</summary>
        bool AllowsOverlap { get; }

        /// <summary>이 프레임 시작에 공격 재생기가 한 번 기록한 값입니다. 프레임 진행 규칙이 씁니다.</summary>
        bool AllowedAtFrameStart { get; }
    }
}
