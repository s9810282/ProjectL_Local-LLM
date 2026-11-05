using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>
    /// 낭떠러지 추락 연출(임시)의 시간·깊이·끝 크기·작아지기 시작 지점입니다. 원본은 View DB 설정 표(<see cref="CombatPresentationSettingsData"/>)이며,
    /// 값 조건은 그 설정의 조건 함수 한 곳을 씁니다(연출 조립 설계 §9 I-5).
    /// </summary>
    internal sealed class CombatFallMotionSettings
    {
        /// <summary>바로 끝나는 값입니다. 설정을 넘기지 않은 조립(시험 등)에서 씁니다.</summary>
        internal static CombatFallMotionSettings Instant { get; } = new(0f, 0f, 1f, 0f);

        internal CombatFallMotionSettings(float seconds, float depth, float endScale, float shrinkStart)
        {
            CombatPresentationSettingsData.ValidateSeconds(seconds, nameof(seconds));
            CombatPresentationSettingsData.ValidateFallDepth(depth, nameof(depth));
            CombatPresentationSettingsData.ValidateFallEndScale(endScale, nameof(endScale));
            CombatPresentationSettingsData.ValidateFallShrinkStart(shrinkStart, nameof(shrinkStart));
            Seconds = seconds;
            Depth = depth;
            EndScale = endScale;
            ShrinkStart = shrinkStart;
        }

        /// <summary>View DB 설정 표의 값으로 만듭니다.</summary>
        internal static CombatFallMotionSettings From(CombatPresentationSettingsData settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            return new CombatFallMotionSettings(settings.FallSeconds, settings.FallDepth, settings.FallEndScale, settings.FallShrinkStart);
        }

        /// <summary>떨어지는 전체 시간입니다(초).</summary>
        internal float Seconds { get; }

        /// <summary>떨어지는 깊이입니다(월드 단위, 필드 위쪽 방향의 반대).</summary>
        internal float Depth { get; }

        /// <summary>끝날 때의 크기 배율입니다. 시작 크기에 곱합니다.</summary>
        internal float EndScale { get; }

        /// <summary>전체 시간 가운데 작아지기 시작하는 지점입니다(0 이상 1 미만).</summary>
        internal float ShrinkStart { get; }

        /// <summary>작아지기 시작까지 기다리는 시간입니다(초).</summary>
        internal float ShrinkDelay => Seconds * ShrinkStart;

        /// <summary>작아지는 시간입니다(초). 떨어지기와 같은 순간에 끝납니다.</summary>
        internal float ShrinkSeconds => Seconds - ShrinkDelay;
    }
}
