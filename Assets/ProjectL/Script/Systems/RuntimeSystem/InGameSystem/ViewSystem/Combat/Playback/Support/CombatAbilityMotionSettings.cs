using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>
    /// 소환 등장·포효 동작의 시간·크기 값입니다(보스 활성화 설계 D6b). 원본은 View DB 설정 표(<see cref="CombatPresentationSettingsData"/>)이며,
    /// 값 조건은 그 설정의 조건 함수 한 곳을 씁니다(연출 조립 설계 §9 I-5).
    /// </summary>
    internal sealed class CombatAbilityMotionSettings
    {
        /// <summary>모든 동작이 바로 끝나는 값입니다. 설정을 넘기지 않은 조립(시험 등)에서 씁니다.</summary>
        internal static CombatAbilityMotionSettings Instant { get; } = new(0f, 0f, 0f, 0f, 0f, 0f, 1f);

        internal CombatAbilityMotionSettings(float summonRiseDelaySeconds, float summonRiseSeconds, float summonRiseDepth,
            float summonRiseTurns, float roarDelaySeconds, float roarPulseSeconds, float roarPeakScale)
        {
            CombatPresentationSettingsData.ValidateSeconds(summonRiseDelaySeconds, nameof(summonRiseDelaySeconds));
            CombatPresentationSettingsData.ValidateSeconds(summonRiseSeconds, nameof(summonRiseSeconds));
            CombatPresentationSettingsData.ValidateSeconds(roarDelaySeconds, nameof(roarDelaySeconds));
            CombatPresentationSettingsData.ValidateSeconds(roarPulseSeconds, nameof(roarPulseSeconds));
            CombatPresentationSettingsData.ValidateRiseDepth(summonRiseDepth, nameof(summonRiseDepth));
            CombatPresentationSettingsData.ValidateRiseTurns(summonRiseTurns, nameof(summonRiseTurns));
            CombatPresentationSettingsData.ValidatePeakScale(roarPeakScale, nameof(roarPeakScale));

            SummonRiseDelaySeconds = summonRiseDelaySeconds;
            SummonRiseSeconds = summonRiseSeconds;
            SummonRiseDepth = summonRiseDepth;
            SummonRiseTurns = summonRiseTurns;
            RoarDelaySeconds = roarDelaySeconds;
            RoarPulseSeconds = roarPulseSeconds;
            RoarPeakScale = roarPeakScale;
        }

        /// <summary>View DB 설정 표의 값으로 만듭니다(연출 조립 설계 §9 I-5).</summary>
        internal static CombatAbilityMotionSettings From(CombatPresentationSettingsData settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            return new CombatAbilityMotionSettings(settings.SummonRiseDelaySeconds, settings.SummonRiseSeconds,
                settings.SummonRiseDepth, settings.SummonRiseTurns, settings.RoarDelaySeconds, settings.RoarPulseSeconds,
                settings.RoarPeakScale);
        }

        /// <summary>소환 칸을 강조한 뒤 올라오기 시작할 때까지 바닥 아래에서 기다리는 시간입니다.</summary>
        internal float SummonRiseDelaySeconds { get; }

        /// <summary>바닥 아래에서 제자리까지 올라오는 시간입니다.</summary>
        internal float SummonRiseSeconds { get; }

        /// <summary>올라오기 시작하는 깊이입니다(월드 단위, 필드 위쪽 방향의 반대).</summary>
        internal float SummonRiseDepth { get; }

        /// <summary>올라오는 동안 위쪽 축(Y)으로 도는 바퀴 수입니다.</summary>
        internal float SummonRiseTurns { get; }

        /// <summary>포효 칸을 강조한 뒤 커지기 시작할 때까지 기다리는 시간입니다.</summary>
        internal float RoarDelaySeconds { get; }

        /// <summary>포효의 커지기와 작아지기에 걸리는 전체 시간입니다(각각 절반). 공격 모션은 커진 채로 그 사이에 재생합니다.</summary>
        internal float RoarPulseSeconds { get; }

        /// <summary>가장 커졌을 때의 크기 배율입니다. 동작을 시작할 때의 크기에 곱합니다.</summary>
        internal float RoarPeakScale { get; }
    }
}
