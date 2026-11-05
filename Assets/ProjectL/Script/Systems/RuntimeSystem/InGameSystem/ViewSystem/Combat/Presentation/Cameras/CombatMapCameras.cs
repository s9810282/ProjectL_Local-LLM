using System;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras
{
    /// <summary>
    /// 맵 프랍 안 버추얼 카메라 하나의 사실입니다(카메라 연출 조각 설계 K1). 프랍을 세울 때 한 번 읽은 사본이고, 원본은 프리팹입니다.
    /// </summary>
    internal readonly struct CombatMapCamera
    {
        /// <param name="name">오브젝트 이름입니다. 버추얼 카메라 카탈로그의 열쇠와 같습니다.</param>
        /// <param name="baseLens">기준 렌즈값입니다. 직교면 크기, 원근이면 시야각입니다. 유한한 양수입니다.</param>
        /// <param name="shakeChannel">흔들림 수신기(Listener)의 채널 마스크입니다. 수신기가 없으면 0입니다.</param>
        /// <param name="transform">
        /// 카메라 오브젝트입니다. 넘기면 지금 월드 위치·회전을 기준 자세로 읽습니다(카메라 조작 설계 CC6). 카메라 조작 시점만 쓰고,
        /// 줌·흔들림만 보는 창구 시험은 넘기지 않습니다.
        /// </param>
        internal CombatMapCamera(string name, float baseLens, int shakeChannel, Transform transform = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("맵 카메라 이름이 비어 있습니다.", nameof(name));
            if (!(baseLens > 0f) || float.IsInfinity(baseLens))
                throw new ArgumentOutOfRangeException(nameof(baseLens), "맵 카메라 기준 렌즈값은 유한한 양수여야 합니다.");
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

        /// <summary>프랍을 세운 순간의 월드 위치입니다. 이동 오프셋의 바탕이고, 표시 해제 때 여기로 되돌립니다(설계 CC3·CC6).</summary>
        internal Vector3 BasePosition { get; }

        /// <summary>프랍을 세운 순간의 월드 회전입니다. 이 회전의 오른쪽·위쪽이 이동 축입니다(설계 CC3, 계획 P2).</summary>
        internal Quaternion BaseRotation { get; }
    }

    /// <summary>
    /// 세운 맵 프랍 안의 버추얼 카메라를 읽습니다(카메라 연출 조각 설계 K1). 렌즈·위치에 쓰지 않고 읽기만 합니다.
    /// 렌즈는 카메라 효과(<c>ICameraEffectService.Dispatch</c>)로만 쓰고, 자세는 View가 맡습니다(카탈로그 문서의 "가진 참조에서 설정 복사" 방식).
    /// </summary>
    internal static class CombatMapCameras
    {
        /// <summary>
        /// <paramref name="root"/> 아래(비활성 포함)의 버추얼 카메라를 모두 읽어 <paramref name="into"/>를 채웁니다.
        /// 직교·원근 판정은 렌즈가 Brain 출력 카메라에서 받은 값을 따르며, 카메라 효과의 줌도 같은 판정으로 크기·시야각을 고릅니다.
        /// 이름이 겹치면 카탈로그가 고를 수 없으므로 예외입니다.
        /// </summary>
        internal static void Read(Transform root, List<CombatMapCamera> into)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            if (into == null) throw new ArgumentNullException(nameof(into));
            into.Clear();
            CinemachineVirtualCamera[] cameras = root.GetComponentsInChildren<CinemachineVirtualCamera>(true);
            for (int i = 0; i < cameras.Length; i++)
            {
                CinemachineVirtualCamera camera = cameras[i];
                string name = camera.gameObject.name;
                for (int j = 0; j < into.Count; j++)
                {
                    if (string.Equals(into[j].Name, name, StringComparison.Ordinal))
                        throw new InvalidOperationException($"[CombatMapCameras] 맵 프랍 안에 같은 이름의 버추얼 카메라가 둘 있습니다: {name}");
                }
                LensSettings lens = camera.m_Lens;
                float baseLens = lens.Orthographic ? lens.OrthographicSize : lens.FieldOfView;
                var listener = camera.GetComponent<CinemachineImpulseListener>();
                int channel = listener != null && listener.enabled ? listener.m_ChannelMask : 0;
                into.Add(new CombatMapCamera(name, baseLens, channel, camera.transform));
            }
        }
    }
}
