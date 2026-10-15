using System;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraEffectSystem.Event
{
    public interface ICameraEvent : IDisposable
    {
        void Execute(ICameraEventData data);
    }
}
