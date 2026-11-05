using ProjectL.Framework.Utility;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.UnityCameraCatalogSystem
{
    internal sealed class UnityCameraCatalogHandlePool
    {
        private sealed class Lifecycle : IPoolLifecycle<UnityCameraCatalogHandle>
        {
            public void OnGet(UnityCameraCatalogHandle handle) { }
            public void OnBeforeReturn(UnityCameraCatalogHandle handle) => handle.Clear();
            public void OnReturn(UnityCameraCatalogHandle handle) { }
        }

        private readonly CustomPool<UnityCameraCatalogHandle> _pool =
            new(new PoolContext<UnityCameraCatalogHandle>(
                () => new UnityCameraCatalogHandle(), 0, null, new Lifecycle()));

        internal UnityCameraCatalogHandle Get() => _pool.Get();
        internal void Return(UnityCameraCatalogHandle handle) => _pool.Return(handle);
    }
}
