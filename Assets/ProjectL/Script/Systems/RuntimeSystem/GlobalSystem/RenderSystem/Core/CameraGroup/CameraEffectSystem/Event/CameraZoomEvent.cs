using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraCatalogSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.DelaySystem.Core;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraEffectSystem.Event
{
    /// <summary>생성자로 받은 서비스로 실행하고 Dispose 시 모든 진행 중인 줌을 취소한다.</summary>
    internal sealed class CameraZoomEvent : ICameraEvent
    {
        private readonly CameraEventServiceContext context;
        private readonly Dictionary<string, Action> active = new Dictionary<string, Action>(StringComparer.Ordinal);
        private bool disposed;

        internal CameraZoomEvent(CameraEventServiceContext context)
            => this.context = context ?? throw new ArgumentNullException(nameof(context));

        public void Execute(ICameraEventData eventData)
        {
            if (disposed) throw new ObjectDisposedException(nameof(CameraZoomEvent));
            if (!(eventData is CameraZoomData data))
                throw new ArgumentException("CameraZoomData is required.", nameof(eventData));
            var catalog = context.Catalog;
            var delay = context.Delay;
            if (!Finite(data.From) || !Finite(data.To) || !Finite(data.Duration) || data.Duration < 0f)
                throw new ArgumentOutOfRangeException(nameof(data));
            var handle = catalog.GetHandle(data.CameraName);
            try
            {
                var validation = (ICameraCatalogZoomValidation)handle;
                validation.ValidateZoom(data.To);
                if (data.Duration > 0f) validation.ValidateZoom(data.From);
            }
            catch
            {
                handle.Dispose();
                throw;
            }

            if (active.TryGetValue(data.CameraName, out var previous)) previous();
            if (data.Duration == 0f)
            {
                using (handle) handle.SetZoom(data.To);
                return;
            }

            bool finished = false;
            float startedAt = delay.CurrentTime;
            Action cancel = null;
            cancel = () =>
            {
                if (finished) return;
                finished = true;
                handle.Dispose();
                handle = null;
                if (active.TryGetValue(data.CameraName, out var current) && ReferenceEquals(current, cancel))
                    active.Remove(data.CameraName);
            };

            void Apply(float value)
            {
                if (finished) return;
                try { handle.SetZoom(value); }
                catch (ObjectDisposedException) { cancel(); }
                catch (ArgumentOutOfRangeException) { cancel(); }
            }

            active.Add(data.CameraName, cancel);
            try
            {
                Apply(data.From);
                if (finished) return;
                delay.AddDelayEvent(new DelayDataHandler(
                    () =>
                    {
                        try { Apply(data.To); }
                        finally { cancel(); }
                    },
                    () =>
                    {
                        if (finished) return;
                        float t = Mathf.Clamp01((delay.CurrentTime - startedAt) / data.Duration);
                        Apply(Mathf.Lerp(data.From, data.To, Mathf.SmoothStep(0f, 1f, t)));
                    },
                    data.Duration));
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
            foreach (var cancel in new List<Action>(active.Values)) cancel();
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
