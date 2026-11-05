using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement
{
    /// <summary>
    /// 걸음 한 칸의 높이 모양입니다(고지대 View 설계 HG-3·계획 P11). 걸음 높이 식의 원본은 이 클래스 하나입니다.
    /// 걷기 모양은 <see cref="At"/>을(높이가 같으면 평평), 뛰기 모양은 <see cref="Arc"/>를(늘 호) 씁니다(걸음 모양 설계 ST2).
    /// 호는 두 높이를 이으면서 위로 볼록하게 더해 칸 경계(진행률 0.5)에서 높은 칸보다 꼭대기 값만큼 위를 지나고,
    /// 그 뒤로 높은 칸 아래로 내려가지 않습니다(블록 모서리를 뚫지 않음).
    /// </summary>
    internal static class CombatStepHeight
    {
        /// <summary>
        /// 걷기 걸음의 진행률 높이(필드 로컬 단위)입니다. 높이가 같으면 시작 높이 그대로이고, 다르면 <see cref="Arc"/>입니다.
        /// 할당과 예외가 없는 식이라 걸음 진행 중 매 프레임 불러도 됩니다.
        /// 입력 조건(높이 유한·진행률 0~1·<paramref name="hop"/> 0 이상)은 부르는 쪽이 지킵니다. hop은 설정 표가, 진행률은 걸음 재생이 보장합니다.
        /// </summary>
        internal static float At(float fromHeight, float toHeight, float progress, float hop)
        {
            if (toHeight - fromHeight == 0f)
                return fromHeight;

            return Arc(fromHeight, toHeight, progress, hop);
        }

        /// <summary>
        /// 늘 호를 더한 진행률 높이(필드 로컬 단위)입니다. 꼭대기(진행률 0.5)는 두 높이 중 높은 쪽보다 <paramref name="peakAboveHigher"/>만큼 위이고,
        /// 높이가 같으면 시작 높이에서 그만큼 오르내리는 포물선입니다. 입력 조건은 <see cref="At"/>과 같습니다.
        /// </summary>
        internal static float Arc(float fromHeight, float toHeight, float progress, float peakAboveHigher)
        {
            float rise = toHeight - fromHeight;
            float arc = (Math.Abs(rise) * .5f) + peakAboveHigher;
            return fromHeight + (rise * progress) + (arc * 4f * progress * (1f - progress));
        }
    }
}
