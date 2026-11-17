using System;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 전투 연출 수 값 규칙의 원본입니다(범용 연출 구조 설계 G8, 계획 P17). 연출 SO 셋(공격 표시·피격 반응·클립 묶음)과 그 값 객체,
    /// 전투 연출 설정 표가 이 판정들을 부릅니다. 값마다 다른 범위(각도 상한, 0~1 비율 등)는 그 값을 가진 곳이 이 판정 위에 더 봅니다.
    /// 효과음 프리셋 번호 규칙("-1 = 프리셋 없음, 0 이상 = 그 프리셋", -1의 소리는 <see cref="NoSoundPreset"/>)도 여기 하나입니다. 외형·설정 표·투사체·유닛 소리 줄과 런타임 소리 프리셋이 부릅니다
    /// (투사체·피격 소리 묶음 끝 검토 중간 2).
    /// </summary>
    internal static class CombatPresentationValueRules
    {
        /// <summary><see cref="IsFinite"/> 규칙 문구입니다.</summary>
        internal const string FiniteRule = "유한한 값이어야 합니다.";

        /// <summary><see cref="IsNonNegative"/> 규칙 문구입니다.</summary>
        internal const string NonNegativeRule = "유한한 0 이상의 값이어야 합니다.";

        /// <summary><see cref="IsPositive"/> 규칙 문구입니다.</summary>
        internal const string PositiveRule = "유한한 양수여야 합니다.";

        /// <summary>NaN·무한이 아닌지입니다.</summary>
        internal static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        /// <summary>유한하고 0 이상인지입니다.</summary>
        internal static bool IsNonNegative(float value) => IsFinite(value) && value >= 0f;

        /// <summary>유한하고 0보다 큰지입니다.</summary>
        internal static bool IsPositive(float value) => IsFinite(value) && value > 0f;

        /// <summary>값 객체 생성자용입니다. <see cref="IsFinite"/>가 아니면 그 인자 이름으로 던집니다.</summary>
        /// <exception cref="ArgumentOutOfRangeException">규칙에 맞지 않을 때입니다.</exception>
        internal static void RequireFinite(float value, string name)
        {
            if (!IsFinite(value)) throw new ArgumentOutOfRangeException(name, value, FiniteRule);
        }

        /// <summary>값 객체 생성자용입니다. <see cref="IsNonNegative"/>가 아니면 그 인자 이름으로 던집니다.</summary>
        /// <exception cref="ArgumentOutOfRangeException">규칙에 맞지 않을 때입니다.</exception>
        internal static void RequireNonNegative(float value, string name)
        {
            if (!IsNonNegative(value)) throw new ArgumentOutOfRangeException(name, value, NonNegativeRule);
        }

        /// <summary>값 객체 생성자용입니다. <see cref="IsPositive"/>가 아니면 그 인자 이름으로 던집니다.</summary>
        /// <exception cref="ArgumentOutOfRangeException">규칙에 맞지 않을 때입니다.</exception>
        internal static void RequirePositive(float value, string name)
        {
            if (!IsPositive(value)) throw new ArgumentOutOfRangeException(name, value, PositiveRule);
        }

        /// <summary>
        /// 효과음 프리셋의 "프리셋 없음" 번호입니다. 외형·투사체 소리 칸의 기본값입니다. 받은 쪽이 프리셋 효과음으로 내면 소리 없음이고,
        /// 프리셋을 유지한 채 내는 칸(장애물·추락)은 칸의 지금 프리셋으로 냅니다(소리 프리셋 원본 설계 SP3). 어느 쪽인지는 그 칸의 툴팁이 적습니다.
        /// </summary>
        internal const int NoSoundPreset = -1;

        /// <summary>소리를 내는 프리셋 번호인지입니다(0 이상).</summary>
        internal static bool IsSoundPreset(int preset) => preset >= 0;

        /// <summary>
        /// 효과음 프리셋 번호 검사입니다. <paramref name="allowNone"/>이면 <see cref="NoSoundPreset"/>도 받고(외형·설정 표·투사체 칸),
        /// 아니면 소리를 내는 번호만 받습니다(유닛 소리 줄 — 끄려면 줄을 지움, 투사체·피격 소리 계획 P2).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">규칙에 맞지 않을 때입니다.</exception>
        internal static void RequireSoundPreset(int preset, string name, bool allowNone)
        {
            if (IsSoundPreset(preset) || (allowNone && preset == NoSoundPreset))
                return;
            throw new ArgumentOutOfRangeException(name, preset, allowNone
                ? "효과음 프리셋은 -1(프리셋 없음) 이상이어야 합니다."
                : "소리 큐의 프리셋은 0 이상이어야 합니다(끄려면 항목을 지웁니다).");
        }
    }
}
