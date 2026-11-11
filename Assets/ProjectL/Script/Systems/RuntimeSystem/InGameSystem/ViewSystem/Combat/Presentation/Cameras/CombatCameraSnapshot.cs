using System;
using Cinemachine;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras
{
    /// <summary>
    /// 명시적으로 연결한 가상 카메라의 기준 사실입니다. 표시 연결 전 한 번 읽고 복원에 사용합니다(씬 공용 카메라 설계 SC3).
    /// </summary>
    internal readonly struct CombatCameraSnapshot
    {
        /// <param name="name">오브젝트 이름입니다. 버추얼 카메라 카탈로그의 열쇠와 같습니다.</param>
        /// <param name="baseLens">기준 렌즈값입니다. 직교면 크기, 원근이면 시야각입니다. 유한한 양수입니다.</param>
        /// <param name="shakeChannel">흔들림 수신기(Listener)의 채널 마스크입니다. 수신기가 없으면 0입니다.</param>
        /// <param name="transform">
        /// 카메라 오브젝트입니다. 넘기면 지금 월드 위치·회전을 기준 자세로 읽습니다(카메라 조작 설계 CC6). 카메라 조작 시점만 씁니다.
        /// 공용 카메라 바인딩이 넘깁니다.
        /// </param>
        internal CombatCameraSnapshot(string name, float baseLens, int shakeChannel, Transform transform = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("카메라 이름이 비어 있습니다.", nameof(name));
            if (!(baseLens > 0f) || float.IsInfinity(baseLens))
                throw new ArgumentOutOfRangeException(nameof(baseLens), "카메라 기준 렌즈값은 유한한 양수여야 합니다.");
            Name = name;
            BaseLens = baseLens;
            ShakeChannel = shakeChannel;
            Transform = transform;
            BasePosition = transform != null ? transform.position : Vector3.zero;
            BaseRotation = transform != null ? transform.rotation : Quaternion.identity;
        }

        /// <summary>오브젝트 이름입니다. 카메라 효과에 보낼 때 카메라를 고르는 열쇠입니다.</summary>
        internal string Name { get; }

        /// <summary>기준 렌즈값입니다(직교 = 크기, 원근 = 시야각). 줌 배율을 곱하는 바탕입니다.</summary>
        internal float BaseLens { get; }

        /// <summary>흔들림 수신기 채널 마스크입니다. 0이면 이 카메라는 흔들림을 받지 않습니다.</summary>
        internal int ShakeChannel { get; }

        /// <summary>카메라 오브젝트입니다. 위치를 쓰는 곳은 카메라 조작 시점 하나입니다(카메라 조작 설계 CC1). 넘기지 않았으면 null입니다.</summary>
        internal Transform Transform { get; }

        /// <summary>표시 연결 전의 월드 위치입니다. 이동 오프셋의 바탕이고, 표시 해제 때 여기로 되돌립니다(설계 CC3·CC6).</summary>
        internal Vector3 BasePosition { get; }

        /// <summary>표시 연결 전의 월드 회전입니다. 이 회전의 오른쪽·위쪽이 이동 축입니다(설계 CC3, 계획 P2).</summary>
        internal Quaternion BaseRotation { get; }

        /// <summary>명시적으로 받은 가상 카메라의 연결 전 기준값을 읽습니다. 카메라나 렌즈를 변경하지 않습니다.</summary>
        internal static CombatCameraSnapshot Capture(CinemachineVirtualCamera camera)
        {
            if (camera == null)
                throw new ArgumentNullException(nameof(camera));

            LensSettings lens = camera.m_Lens;
            float baseLens = lens.Orthographic ? lens.OrthographicSize : lens.FieldOfView;
            var listener = camera.GetComponent<CinemachineImpulseListener>();
            int channel = listener != null && listener.enabled ? listener.m_ChannelMask : 0;
            return new CombatCameraSnapshot(camera.gameObject.name, baseLens, channel, camera.transform);
        }
    }

}
