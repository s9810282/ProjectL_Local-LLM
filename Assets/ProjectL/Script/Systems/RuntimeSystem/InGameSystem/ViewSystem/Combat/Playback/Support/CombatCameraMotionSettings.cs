using System;
using Cinemachine;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraEffectSystem;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>
    /// 포효 조립·기차 재생이 쓰는 카메라 연출 값입니다(카메라 연출 조각 설계 K5·K9). 피격·사망 흔들림(K8)은 반응 묶음의 큐로 옮겼습니다(범용 연출 구조 설계 G7). 원본은 View DB 설정 표(<see cref="CombatPresentationSettingsData"/>)입니다.
    /// 능력 동작 값(<see cref="CombatAbilityMotionSettings"/>)과 나눠, 카메라 값을 바꿀 때 이 클래스만 보면 되게 합니다.
    /// </summary>
    /// <remarks>
    /// 배율·세기는 우리 값이라 설정 Data의 조건 함수로 검사합니다. 흔들림 시간·모양의 조건 원본은 카메라 효과이며, 잘못된 값은 보낼 때 예외입니다(결정 A).
    /// 포효 줌 시간은 따로 두지 않고 조립이 포효 맥박 절반에서 계산합니다(P7). 기차 줌 시간은 맞출 다른 값이 없어 설정 값으로 둡니다(K10). 흔들림 값은 카메라 효과의 <see cref="CameraShakeData"/>로 만들어 넘기고,
    /// 채널은 카메라 창구가 맵 카메라 수신기로 채웁니다.
    /// </remarks>
    internal sealed class CombatCameraMotionSettings
    {
        private readonly ShakeValues _roarShake;
        private readonly ShakeValues _trainShake;

        /// <summary>카메라 연출이 없는 값입니다(줌 배율 1, 모든 흔들림 세기 0). 지금 쓰는 곳은 없습니다(운영은 <see cref="From"/>).</summary>
        internal static CombatCameraMotionSettings None { get; } =
            new(1f, 0f, 0f, CinemachineImpulseDefinition.ImpulseShapes.Bump);

        /// <param name="roarZoomRatio">포효 줌 배율입니다(기준에 곱함, 1이면 줌 없음).</param>
        /// <param name="roarShakeStrength">포효 타격 흔들림 세기입니다(0이면 흔들림 없음).</param>
        /// <param name="roarShakeSeconds">포효 흔들림 시간입니다. 검사는 카메라 효과가 합니다.</param>
        /// <param name="roarShakeShape">포효 흔들림 모양입니다. 검사는 카메라 효과가 합니다.</param>
        /// <param name="trainShakeStrength">기차가 필드에 들어올 때의 흔들림 세기입니다(0이면 없음, 기본 0, 설계 K9).</param>
        /// <param name="trainShakeSeconds">기차 흔들림 시간입니다. 검사는 카메라 효과가 합니다.</param>
        /// <param name="trainShakeShape">기차 흔들림 모양입니다. 검사는 카메라 효과가 합니다.</param>
        /// <param name="trainZoomRatio">기차가 필드를 지나는 동안의 줌 배율입니다(1이면 줌 없음, 기본 1, 설계 K10).</param>
        /// <param name="trainZoomInSeconds">기차 줌으로 들어가는 시간입니다.</param>
        /// <param name="trainZoomOutSeconds">기차 줌에서 기준으로 돌아오는 시간입니다.</param>
        internal CombatCameraMotionSettings(float roarZoomRatio, float roarShakeStrength, float roarShakeSeconds,
            CinemachineImpulseDefinition.ImpulseShapes roarShakeShape,
            float trainShakeStrength = 0f, float trainShakeSeconds = 0f,
            CinemachineImpulseDefinition.ImpulseShapes trainShakeShape = CinemachineImpulseDefinition.ImpulseShapes.Bump,
            float trainZoomRatio = 1f, float trainZoomInSeconds = 0f, float trainZoomOutSeconds = 0f)
        {
            CombatPresentationSettingsData.ValidateZoomRatio(roarZoomRatio, nameof(roarZoomRatio));
            CombatPresentationSettingsData.ValidateShakeStrength(roarShakeStrength, nameof(roarShakeStrength));
            CombatPresentationSettingsData.ValidateShakeStrength(trainShakeStrength, nameof(trainShakeStrength));
            CombatPresentationSettingsData.ValidateZoomRatio(trainZoomRatio, nameof(trainZoomRatio));
            CombatPresentationSettingsData.ValidateSeconds(trainZoomInSeconds, nameof(trainZoomInSeconds));
            CombatPresentationSettingsData.ValidateSeconds(trainZoomOutSeconds, nameof(trainZoomOutSeconds));
            RoarZoomRatio = roarZoomRatio;
            TrainZoomRatio = trainZoomRatio;
            TrainZoomInSeconds = trainZoomInSeconds;
            TrainZoomOutSeconds = trainZoomOutSeconds;
            _roarShake = new ShakeValues(roarShakeStrength, roarShakeSeconds, roarShakeShape);
            _trainShake = new ShakeValues(trainShakeStrength, trainShakeSeconds, trainShakeShape);
        }

        /// <summary>View DB 설정 표의 값으로 만듭니다.</summary>
        internal static CombatCameraMotionSettings From(CombatPresentationSettingsData settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            return new CombatCameraMotionSettings(settings.RoarZoomRatio, settings.RoarShakeStrength, settings.RoarShakeSeconds,
                settings.RoarShakeShape,
                settings.TrainShakeStrength, settings.TrainShakeSeconds, settings.TrainShakeShape,
                settings.TrainZoomRatio, settings.TrainZoomInSeconds, settings.TrainZoomOutSeconds);
        }

        /// <summary>포효 때 맵 카메라 줌 배율입니다. 1이면 줌하지 않습니다.</summary>
        internal float RoarZoomRatio { get; }

        /// <summary>포효 줌을 하는지입니다.</summary>
        internal bool HasRoarZoom => RoarZoomRatio != 1f;

        /// <summary>포효 타격 흔들림을 하는지입니다(세기가 0보다 큼).</summary>
        internal bool HasRoarShake => _roarShake.IsOn;

        /// <summary>포효 타격 흔들림 값입니다. 아래 방향 × 세기, 시간, 모양을 채운 카메라 효과 값입니다.</summary>
        internal CameraShakeData RoarShake => _roarShake.Build();

        /// <summary>기차 흔들림을 하는지입니다(세기가 0보다 큼, 카메라 연출 조각 설계 K9).</summary>
        internal bool HasTrainShake => _trainShake.IsOn;

        /// <summary>기차 앞면이 필드에 들어오는 순간의 흔들림 값입니다.</summary>
        internal CameraShakeData TrainShake => _trainShake.Build();

        /// <summary>기차가 필드를 지나는 동안의 줌 배율입니다(설계 K10). 1이면 줌하지 않습니다.</summary>
        internal float TrainZoomRatio { get; }

        /// <summary>기차 줌을 하는지입니다.</summary>
        internal bool HasTrainZoom => TrainZoomRatio != 1f;

        /// <summary>기차 앞면이 필드에 들어올 때 줌으로 옮기는 시간입니다.</summary>
        internal float TrainZoomInSeconds { get; }

        /// <summary>기차 앞면이 필드를 빠져나갈 때 기준으로 돌아오는 시간입니다.</summary>
        internal float TrainZoomOutSeconds { get; }

        /// <summary>
        /// 흔들림 큐 값(세기·시간·모양)을 카메라 효과 값으로 바꿉니다(범용 연출 구조 계획 Gd.8). 피격·사망 흔들림이 반응 묶음 큐로 옮겨 가,
        /// 설정 값(포효·기차)과 큐가 이 바꾸기 하나를 같이 씁니다.
        /// </summary>
        internal static CameraShakeData Shake(float strength, float seconds, CinemachineImpulseDefinition.ImpulseShapes shape) =>
            new ShakeValues(strength, seconds, shape).Build();

        /// <summary>
        /// 설정의 흔들림 값 셋(세기·시간·모양)입니다. 세기는 우리 값(아래 방향 속도의 크기)이라 카메라 효과 값으로 바꾸는 곳이 필요하고,
        /// 그 바꾸기를 여기 한 곳에 둡니다. 밖에는 <see cref="CameraShakeData"/>만 내보냅니다.
        /// </summary>
        private readonly struct ShakeValues
        {
            private readonly float _strength;
            private readonly float _seconds;
            private readonly CinemachineImpulseDefinition.ImpulseShapes _shape;

            internal ShakeValues(float strength, float seconds, CinemachineImpulseDefinition.ImpulseShapes shape)
            {
                _strength = strength;
                _seconds = seconds;
                _shape = shape;
            }

            internal bool IsOn => _strength > 0f;

            internal CameraShakeData Build()
            {
                CameraShakeData shake = CameraShakeData.Default;
                shake.Velocity = Vector3.down * _strength;
                shake.Duration = _seconds;
                shake.Shape = _shape;
                return shake;
            }
        }
    }
}
