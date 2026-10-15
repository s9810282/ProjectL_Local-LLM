using System;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.MeshSystem
{
    public interface IMeshRenderHandle : IDisposable
    {
        void Update<TData>(TData data) where TData : IMeshUpdateData;
    }

    internal interface IMeshRenderHandleService
    {
        void Update<TData>(MeshNode node, TData data) where TData : IMeshUpdateData;
        void Release(MeshNode node);
    }

    internal sealed class MeshRenderHandle : IMeshRenderHandle
    {
        private IMeshRenderHandleService service;
        private MeshNode node;

        internal MeshRenderHandle(IMeshRenderHandleService service, MeshNode node)
        {
            this.service = service;
            this.node = node;
        }

        public void Update<TData>(TData data) where TData : IMeshUpdateData
        {
            if (service == null || node == null)
                throw new ObjectDisposedException(nameof(MeshRenderHandle));

            service.Update(node, data);
        }

        public void Dispose()
        {
            if (service == null || node == null)
                return;

            service.Release(node);
            service = null;
            node = null;
        }
    }
}
