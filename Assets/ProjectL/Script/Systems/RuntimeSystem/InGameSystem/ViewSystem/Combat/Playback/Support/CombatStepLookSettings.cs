using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>
    /// 자발 이동의 걸음 모양과 뛰기 값입니다(걸음 모양 설계 ST4). 원본은 View DB 설정 표(<see cref="CombatPresentationSettingsData"/>)이며,
    /// 값 조건은 그 설정의 조건 함수 한 곳을 씁니다(연출 조립 설계 §9 I-5). 걷기 값(걸음 시간·높이 다른 칸 뜀)은 사건 Presenter 값 묶음에 그대로 있습니다.
    /// </summary>
    internal sealed class CombatStepLookSettings
    {
        // Walk는 뛰기 값을 읽지 않습니다. 아래 둘은 검사만 지나는 자리 값이며 설정 표 기본값과 관계없습니다.
        private const float UnusedHopHeight = 0f;
        private const float UnusedHopSeconds = 1f;

        /// <summary>걷기입니다. 설정을 넘기지 않은 조립(시험 등)에서 씁니다(걸음 모양 계획 P4).</summary>
        internal static CombatStepLookSettings Walk { get; } = new(CombatStepLook.Walk, UnusedHopHeight, UnusedHopSeconds);

        internal CombatStepLookSettings(CombatStepLook look, float hopHeight, float hopSeconds)
        {
            CombatPresentationSettingsData.ValidateStepLook(look, nameof(look));
            CombatPresentationSettingsData.ValidateHopHeight(hopHeight, nameof(hopHeight));
            CombatPresentationSettingsData.ValidateStepSeconds(hopSeconds, nameof(hopSeconds));
            Look = look;
            HopHeight = hopHeight;
            HopSeconds = hopSeconds;
        }

        /// <summary>View DB 설정 표의 값으로 만듭니다.</summary>
        internal static CombatStepLookSettings From(CombatPresentationSettingsData settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            return new CombatStepLookSettings(settings.VoluntaryStepLook, settings.HopHeight, settings.HopSeconds);
        }

        /// <summary>자발 이동의 걸음 모양입니다.</summary>
        internal CombatStepLook Look { get; }

        /// <summary>뛰기 꼭대기가 두 칸 중 높은 칸 표면보다 더 올라가는 높이입니다(필드 로컬 단위).</summary>
        internal float HopHeight { get; }

        /// <summary>뛰기 한 번(한 칸)의 시간입니다(초).</summary>
        internal float HopSeconds { get; }
    }
}
