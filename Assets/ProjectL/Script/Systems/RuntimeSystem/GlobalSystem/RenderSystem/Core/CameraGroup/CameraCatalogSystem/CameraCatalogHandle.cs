using System;
using Cinemachine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraCatalogSystem
{
    public interface ICameraCatalogHandle : IDisposable
    {
        void SetPriority(int priority);
        void SetZoom(float value);
        void Customize(CameraCatalogData data);
    }

    internal interface ICameraCatalogZoomValidation
    {
        void ValidateZoom(float value);
    }

    internal interface ICameraCatalogHandlePoolService
    {
        void Return(CameraCatalogNode node, ulong version);
    }

    internal sealed class CameraCatalogHandle : ICameraCatalogHandle, ICameraCatalogZoomValidation
    {
        private ICameraCatalogHandlePoolService pool;
        private CameraCatalogNode node;
        private readonly ulong version;

        internal CameraCatalogHandle(ICameraCatalogHandlePoolService pool, CameraCatalogNode node)
        {
            this.pool = pool;
            this.node = node;
            version = node.Version;
        }

        public void SetPriority(int priority) => RequireCamera().Priority = priority;

        /// <summary>직교: OrthographicSize, 원근: FieldOfView. 절대값이며 작을수록 줌인.</summary>
        public void SetZoom(float value)
        {
            var camera = RequireCamera();
            ValidateZoom(value);
            var lens = camera.m_Lens;
            if (lens.Orthographic) lens.OrthographicSize = value;
            else lens.FieldOfView = value;
            camera.m_Lens = lens;
        }

        public void ValidateZoom(float value)
        {
            var camera = RequireCamera();
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentOutOfRangeException(nameof(value), "Zoom must be finite.");
            var lens = camera.m_Lens;
            if (lens.Orthographic)
            {
                if (value <= 0f)
                    throw new ArgumentOutOfRangeException(nameof(value), "Orthographic size must be positive.");
            }
            else
            {
                if (value < 1f || value > 179f)
                    throw new ArgumentOutOfRangeException(nameof(value), "Field of view must be between 1 and 179 degrees.");
            }
        }

        public void Customize(CameraCatalogData data)
        {
            var camera = RequireCamera();
            camera.transform.SetPositionAndRotation(data.Position, data.Rotation);
            camera.m_Lens = data.Lens;
            camera.Follow = data.Follow;
            camera.LookAt = data.LookAt;
            camera.Priority = data.Priority;
            camera.m_StandbyUpdate = data.StandbyUpdate;
            camera.m_Transitions = data.Transitions;
            camera.enabled = data.Enabled;
            camera.gameObject.SetActive(data.Active);
        }

        public void Dispose()
        {
            if (pool == null || node == null) return;
            pool.Return(node, version);
            pool = null;
            node = null;
        }

        private CinemachineVirtualCamera RequireCamera()
        {
            if (node == null || node.Version != version || node.Camera == null)
                throw new ObjectDisposedException(nameof(CameraCatalogHandle),
                    "The handle was returned, its camera was destroyed, or the scene was rebound.");
            return node.Camera;
        }
    }
}
