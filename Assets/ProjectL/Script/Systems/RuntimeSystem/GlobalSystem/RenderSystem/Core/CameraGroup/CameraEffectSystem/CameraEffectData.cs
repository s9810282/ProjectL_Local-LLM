using System;
using Cinemachine;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraEffectSystem
{
    public interface ICameraEventData { }

    [Serializable]
    public struct CameraZoomData : ICameraEventData
    {
        public string CameraName;
        // 직교는 Size, 원근은 FOV. From/To는 호출자가 결정한다.
        public float From;
        public float To;
        public float Duration;
    }

    [Serializable]
    public struct CameraShakeData : ICameraEventData
    {
        public Vector3 Position;
        // 방향 및 크기가 흔들림의 방향 및 강도다.
        public Vector3 Velocity;
        public float Duration;
        public int Channel;
        public CinemachineImpulseDefinition.ImpulseShapes Shape;

        public static CameraShakeData Default => new CameraShakeData
        {
            Velocity = Vector3.down,
            Duration = 0.2f,
            Channel = 1,
            Shape = CinemachineImpulseDefinition.ImpulseShapes.Bump
        };
    }
}
