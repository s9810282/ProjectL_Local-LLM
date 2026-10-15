using System;
using System.Collections.Generic;
using Cinemachine;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.DelaySystem.Core;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraEffectSystem.Event
{
    /// <summary>Source Mono 없이 신호 발행. 수신 카메라에는 같은 채널의 ImpulseListener가 필요하다.</summary>
    internal sealed class CameraShakeEvent : ICameraEvent
    {
        private readonly CameraEventServiceContext context;
        private readonly List<Action> active = new List<Action>();
        private bool disposed;

        internal CameraShakeEvent(CameraEventServiceContext context)
            => this.context = context ?? throw new ArgumentNullException(nameof(context));

        public void Execute(ICameraEventData eventData)
        {
            if (disposed) throw new ObjectDisposedException(nameof(CameraShakeEvent));
            if (!(eventData is CameraShakeData data))
                throw new ArgumentException("CameraShakeData is required.", nameof(eventData));
            var delay = context.Delay;
            if (!Finite(data.Duration) || data.Duration <= 0f || data.Channel == 0 ||
                !Finite(data.Position) || !Finite(data.Velocity) ||
                data.Shape < CinemachineImpulseDefinition.ImpulseShapes.Recoil ||
                data.Shape > CinemachineImpulseDefinition.ImpulseShapes.Rumble)
                throw new ArgumentOutOfRangeException(nameof(data));

            // SignalSource가 Definition을 참조하므로 실행 중인 정의는 재사용하지 않는다.
            var definition = new CinemachineImpulseDefinition
            {
                m_ImpulseChannel = data.Channel,
                m_ImpulseDuration = data.Duration,
                m_ImpulseShape = data.Shape,
                m_ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform
            };
            var impulse = definition.CreateAndReturnEvent(data.Position, data.Velocity);
            if (impulse == null) throw new InvalidOperationException("Cinemachine could not create the shake impulse.");
            ISignalSource6D source = impulse.m_SignalSource;
            bool finished = false;
            Action cancel = null;

            void Finish(bool stop)
            {
                if (finished) return;
                finished = true;
                if (stop && ReferenceEquals(source, impulse.m_SignalSource))
                    impulse.Cancel(CinemachineImpulseManager.Instance.CurrentTime, true);
                active.Remove(cancel);
                // 취소된 Delay 콜백이 남아 있어도 카메라/Impulse 참조는 유지하지 않는다.
                impulse = null;
                source = null;
            }

            cancel = () => Finish(true);
            active.Add(cancel);
            try
            {
                delay.AddDelayEvent(new DelayDataHandler(() => Finish(false), null, data.Duration));
            }
            catch
            {
                cancel();
                throw;
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            foreach (var cancel in active.ToArray()) cancel();
        }

        private static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
