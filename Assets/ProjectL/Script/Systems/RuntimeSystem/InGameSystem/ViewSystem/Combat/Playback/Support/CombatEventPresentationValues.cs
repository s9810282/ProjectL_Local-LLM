using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>
    /// 사건 Presenter의 연출 값 묶음입니다(View 3라운드 W9-3, 결정 Q-V27 A). 운영 값의 원본은 View DB 설정 표입니다(<see cref="From"/>).
    /// 값 조건 검사는 이 생성자 한 곳에서 하며 설정 표의 조건 함수를 부릅니다. 소리 프리셋은 값이 아니라 표 조회라 여기 두지 않습니다.
    /// 시험 조립은 생성자로 필요한 값만 정합니다(시험 도우미 <c>CombatViewSystemTests.EventValues</c>).
    /// </summary>
    internal sealed class CombatEventPresentationValues
    {
        /// <param name="stepSeconds">이동 한 걸음의 보간 시간입니다(유한한 양수).</param>
        /// <param name="enemyActionGapSeconds">적 행동 사이 간격입니다(유한한 0 이상).</param>
        /// <param name="enemyTurnGapSeconds">적 턴 사이 간격입니다(유한한 0 이상).</param>
        /// <param name="movementTurnSeconds">이동 전 회전·도착 뒤 방향 복귀 시간입니다(유한한 0 이상).</param>
        /// <param name="abilityMotion">보스 능력(소환·포효) 동작 값입니다.</param>
        /// <param name="cameraMotion">포효 줌·흔들림과 기차 흔들림·줌 값입니다(피격·사망 흔들림은 반응 묶음 큐, 범용 연출 구조 설계 G7). "연출 없음"은 <see cref="CombatCameraMotionSettings.None"/>입니다.</param>
        /// <param name="stepClimbHop">높이가 다른 칸 사이 걸음의 뜀 높이입니다(유한한 0 이상).</param>
        /// <param name="fallMotion">낭떠러지 추락 연출 값입니다(임시). 넘기지 않으면 바로 끝나는 값(<see cref="CombatFallMotionSettings.Instant"/>)이고,
        /// 운영 조립(<see cref="From"/>)은 늘 설정 값을 넘깁니다.</param>
        /// <param name="stepLook">자발 이동의 걸음 모양과 뛰기 값입니다(걸음 모양 설계 ST4). 넘기지 않으면 걷기(<see cref="CombatStepLookSettings.Walk"/>, 지금과 같음)이고,
        /// 운영 조립(<see cref="From"/>)은 늘 설정 값을 넘깁니다.</param>
        /// <param name="leapMotion">실제 뛰어넘기 발동 구간 전체의 시간·높이입니다. 생략하면 View 설정의 기본값을 씁니다.</param>
        internal CombatEventPresentationValues(float stepSeconds, float enemyActionGapSeconds, float enemyTurnGapSeconds,
            float movementTurnSeconds, CombatAbilityMotionSettings abilityMotion, CombatCameraMotionSettings cameraMotion,
            float stepClimbHop, CombatFallMotionSettings fallMotion = null, CombatStepLookSettings stepLook = null,
            CombatLeapMotionSettings leapMotion = null)
        {
            // 순서는 옛 사건 Presenter 생성자와 같습니다. 여러 값이 함께 잘못됐을 때 먼저 알리는 값이 같습니다.
            ValidateStepSeconds(stepSeconds, nameof(stepSeconds));
            CombatPresentationSettingsData.ValidateStepClimbHop(stepClimbHop, nameof(stepClimbHop));
            CombatEnemyPresentationPacing.ValidateSeconds(movementTurnSeconds, nameof(movementTurnSeconds));
            CombatEnemyPresentationPacing.ValidateSeconds(enemyActionGapSeconds, nameof(enemyActionGapSeconds));
            CombatEnemyPresentationPacing.ValidateSeconds(enemyTurnGapSeconds, nameof(enemyTurnGapSeconds));
            StepSeconds = stepSeconds;
            EnemyActionGapSeconds = enemyActionGapSeconds;
            EnemyTurnGapSeconds = enemyTurnGapSeconds;
            MovementTurnSeconds = movementTurnSeconds;
            AbilityMotion = abilityMotion ?? throw new ArgumentNullException(nameof(abilityMotion));
            CameraMotion = cameraMotion ?? throw new ArgumentNullException(nameof(cameraMotion));
            StepClimbHop = stepClimbHop;
            FallMotion = fallMotion ?? CombatFallMotionSettings.Instant;
            StepLook = stepLook ?? CombatStepLookSettings.Walk;
            LeapMotion = leapMotion ?? CombatLeapMotionSettings.Default;
        }

        internal float StepSeconds { get; }
        internal float EnemyActionGapSeconds { get; }
        internal float EnemyTurnGapSeconds { get; }
        internal float MovementTurnSeconds { get; }
        internal CombatAbilityMotionSettings AbilityMotion { get; }
        internal CombatCameraMotionSettings CameraMotion { get; }
        internal float StepClimbHop { get; }
        internal CombatFallMotionSettings FallMotion { get; }
        internal CombatStepLookSettings StepLook { get; }
        internal CombatLeapMotionSettings LeapMotion { get; }

        /// <summary>설정 표 값으로 만듭니다. 표시 Factory가 부르고, 기차 재생도 같은 카메라 연출 값(<see cref="CameraMotion"/>)을 씁니다.</summary>
        internal static CombatEventPresentationValues From(CombatPresentationSettingsData settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            return new CombatEventPresentationValues(settings.MovementStepSeconds, settings.EnemyActionGapSeconds,
                settings.EnemyTurnGapSeconds, settings.MovementTurnSeconds, CombatAbilityMotionSettings.From(settings),
                CombatCameraMotionSettings.From(settings), settings.StepClimbHop, CombatFallMotionSettings.From(settings),
                CombatStepLookSettings.From(settings), CombatLeapMotionSettings.From(settings));
        }

        /// <summary>
        /// 이동 한 걸음의 연출 시간 조건입니다(유한한 양수). 조건 원본은 View DB 설정 표 한 곳이며(연출 조립 설계 §9 I-5),
        /// 설정 검사와 이 생성자가 같은 조건을 씁니다(구조 개선 SV1).
        /// </summary>
        internal static void ValidateStepSeconds(float seconds, string name) =>
            CombatPresentationSettingsData.ValidateStepSeconds(seconds, name);
    }
}
