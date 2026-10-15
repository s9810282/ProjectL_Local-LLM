using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase;
using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem;
using ProjectL.Systems.ServiceKeys;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.OutlineSystem
{
    #region 서비스

    public interface IOutlineSystemService : IServiceModule
    {
        IOutlineScopService CreateOutlineScop(OutlineData data);
    }

    internal interface IOutlineSystemOnlyScopService : IServiceModule
    {
        // ����Ʈ���� �����ϱ� ���� Scop �ڽŵ� �Ű������� ����
        void ReturnOutlineScop(OutlineScop scop, OutlineNode node);
    }

    #endregion

    #region 시스템 모듈

    internal class OutlineSystem :
        ISystemBindScene,
        ISystemInitialize,
        ISystemShutdown,
        IExportSystemServices,
        ISystemInitData<IHasMaterialQuery>,
        ISystemPostBindService,
        IOutlineSystemService,
        IOutlineSystemOnlyScopService
    {
        private readonly RenderRuntimeRoot runtimeRoot;
        private Transform outlineRoot;
        private IHasMaterialQuery MaterialQuery;
        private IColorSystemService ColorSystemService;
        private OutlinePool outlinePool;

        // �߱޵� ���������� �����ϱ� ���� ����Ʈ
        private List<OutlineScop> activeScopes = new List<OutlineScop>();

        internal OutlineSystem(RenderRuntimeRoot runtimeRoot)
        {
            this.runtimeRoot = runtimeRoot ?? throw new System.ArgumentNullException(nameof(runtimeRoot));
        }

        public void BindScene()
        {
            outlineRoot = runtimeRoot.GetOrCreateSystemRoot("OutlineRoot");
        }

        public void InitData(IHasMaterialQuery data)
        {
            MaterialQuery = data;
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            var globalContext = resolver.GetService(SystemGroupsService.Global);
            ColorSystemService = globalContext.RenderServiceCommand.CoreGroup.ColorSystemServices;
        }

        public void Initialize()
        {
            Material outlineMat = MaterialQuery.MetrialTableQuery.GetMaterial("_SelectOutline").Material;

            if (outlineRoot == null)
                throw new System.InvalidOperationException("OutlineSystem requires BindScene before initialization.");

            outlinePool = new OutlinePool(outlineMat, ColorSystemService, outlineRoot);
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IOutlineSystemService>(this);
            sink.AddModule<ISystemModule>(this);
        }

        public IOutlineScopService CreateOutlineScop(OutlineData data)
        {
            OutlineNode node = outlinePool.GetElement();
            OutlineScop scop = new OutlineScop(node, this, data);

            // �뿩�� �� List�� ���
            activeScopes.Add(scop);

            return scop;
        }

        public void ReturnOutlineScop(OutlineScop scop, OutlineNode node)
        {
            // ����ڰ� Dispose�� ȣ���ϸ� List���� �����ϰ� Ǯ�� ��ȯ
            activeScopes.Remove(scop);
            outlinePool.ReturnElement(node);
        }

        public void Shutdown()
        {
            // Shutdown �� Ȱ��ȭ�Ǿ� �ִ� ���������� ��� ����
            // ��ȸ �� Dispose�� ȣ��Ǹ� activeScopes.Remove�� ����ǹǷ� ���ܸ� �����ϱ� ���� �迭�� ����
            var scopesToDispose = activeScopes.ToArray();

            foreach (var scop in scopesToDispose)
            {
                scop.Dispose();
            }

            activeScopes.Clear();
        }
    }

    #endregion
}
