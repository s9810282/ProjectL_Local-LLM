using ProjectL.Framework.Utility;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraCatalogSystem
{
    internal sealed class CameraCatalogNodePool
    {
        private sealed class Lifecycle : IPoolLifecycle<CameraCatalogNode>
        {
            public void OnGet(CameraCatalogNode node) { }
            public void OnBeforeReturn(CameraCatalogNode node) => node.Clear();
            public void OnReturn(CameraCatalogNode node) { }
        }

        private readonly CustomPool<CameraCatalogNode> pool =
            new CustomPool<CameraCatalogNode>(new PoolContext<CameraCatalogNode>(
                () => new CameraCatalogNode(), 0, null, new Lifecycle()));

        internal CameraCatalogNode Get() => pool.Get();
        internal void Return(CameraCatalogNode node) => pool.Return(node);
    }
}
