using System;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.UnityCameraCatalogSystem
{
    /// <summary>출력 카메라의 임시 설정을 복원할 때 사용하는 값입니다.</summary>
    public struct UnityCameraRenderState
    {
        public int CullingMask;
        public RenderTexture TargetTexture;
        public bool Enabled;
        public float RenderOrder;
    }

    /// <summary>출력 카메라의 렌더링 설정을 제어합니다. Dispose로 풀에 반환한 참조는 더 이상 사용하지 않습니다.</summary>
    public interface IUnityCameraCatalogHandle : IDisposable
    {
        UnityCameraRenderState Capture();
        void Restore(UnityCameraRenderState state);
        void SetRenderOrder(float order);
        void SetCullingMask(int cullingMask);
        void SetTargetTexture(RenderTexture targetTexture);
        void SetEnabled(bool enabled);
    }

    internal interface IUnityCameraCatalogHandlePoolService
    {
        void Return(UnityCameraCatalogHandle handle);
    }

    /// <summary>풀에서 직접 대여·반환하는 출력 카메라 제어 객체입니다.</summary>
    internal sealed class UnityCameraCatalogHandle : IUnityCameraCatalogHandle
    {
        private IUnityCameraCatalogHandlePoolService _pool;
        private Camera _camera;

        internal void Bind(IUnityCameraCatalogHandlePoolService pool, Camera camera)
        {
            _pool = pool;
            _camera = camera;
        }

        internal void Clear()
        {
            _pool = null;
            _camera = null;
        }

        public UnityCameraRenderState Capture()
        {
            Camera camera = RequireCamera();
            return new UnityCameraRenderState
            {
                CullingMask = camera.cullingMask,
                TargetTexture = camera.targetTexture,
                Enabled = camera.enabled,
                RenderOrder = camera.depth
            };
        }

        public void Restore(UnityCameraRenderState state)
        {
            Camera camera = RequireCamera();
            camera.cullingMask = state.CullingMask;
            camera.targetTexture = state.TargetTexture;
            camera.enabled = state.Enabled;
            camera.depth = state.RenderOrder;
        }

        public void SetRenderOrder(float order)
        {
            if (float.IsNaN(order) || float.IsInfinity(order))
                throw new ArgumentOutOfRangeException(nameof(order));
            RequireCamera().depth = order;
        }

        public void SetCullingMask(int cullingMask) => RequireCamera().cullingMask = cullingMask;

        public void SetTargetTexture(RenderTexture targetTexture) => RequireCamera().targetTexture = targetTexture;

        public void SetEnabled(bool enabled) => RequireCamera().enabled = enabled;

        public void Dispose()
        {
            _pool?.Return(this);
        }

        private Camera RequireCamera()
        {
            if (_pool == null || _camera == null)
                throw new ObjectDisposedException(nameof(UnityCameraCatalogHandle),
                    "The handle was disposed, its camera was destroyed, or the catalog was shut down.");

            return _camera;
        }
    }
}
