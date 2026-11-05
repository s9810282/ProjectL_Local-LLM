using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>뛰어넘기 구간 전체의 시간·높이입니다. 기본값과 검증의 원본은 View 연출 설정입니다.</summary>
    internal sealed class CombatLeapMotionSettings
    {
        internal static CombatLeapMotionSettings Default { get; } = new(
            CombatPresentationSettingsData.DefaultLeapSeconds, CombatPresentationSettingsData.DefaultLeapHeight);

        internal CombatLeapMotionSettings(float seconds, float height)
        {
            CombatPresentationSettingsData.ValidateStepSeconds(seconds, nameof(seconds));
            CombatPresentationSettingsData.ValidateHopHeight(height, nameof(height));
            Seconds = seconds;
            Height = height;
        }

        internal float Seconds { get; }
        internal float Height { get; }

        internal static CombatLeapMotionSettings From(CombatPresentationSettingsData settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            return new CombatLeapMotionSettings(settings.LeapSeconds, settings.LeapHeight);
        }
    }
}
