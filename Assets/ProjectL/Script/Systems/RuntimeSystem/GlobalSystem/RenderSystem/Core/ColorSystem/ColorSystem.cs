using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase.RenderTable;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Framework.Utility;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem
{
    // ����ڿ��� ����� �ý��� ����
    #region 서비스

    public interface IColorSystemService : IServiceModule
    {
        // ����ڿ��� ������ �����ؼ� ��ȯ���ִ� �Լ�
        IColorHandle CreateColorHandle(Renderer renderer);
    }

    // ���� ���� ���� ����
    internal interface IColorSystemOnlyHandleService : IServiceModule
    {
        // ��ȯ�ϴ� �Լ� (�����δ� ���� ���̹Ƿ� �ϴ� ���븸 �ۼ�)
        void ReturnColorHandle(ColorHandle handle);
    }

    #endregion

    #region 시스템 모듈

    internal class ColorSystem :
        ISystemInitialize,
        ISystemShutdown,
        IExportSystemServices,

        IColorSystemService,
        IColorSystemOnlyHandleService
    {
        // �������� Ŭ������(ColorHandle)�� �����ϱ� ���� colorHandles�� ����
        private CustomPool<ColorHandle> colorHandlePool;
        private readonly HashSet<Renderer> renderers = new HashSet<Renderer>();
        private readonly List<IColorHandle> colorHandles = new List<IColorHandle>();

        private IMetrialTableQuery metrialTableQuery;

        #region SystemFunction

        internal ColorSystem() 
        {
            colorHandlePool = new CustomPool<ColorHandle>(
            new PoolContext<ColorHandle>(
                PoolCreateColorHandle,
                10,
                null,
                null
                )
            );
        }

        public void Initialize()
        {

        }

        public void Shutdown()
        {
            var handlesToDispose = colorHandles.ToArray();
            foreach (var handle in handlesToDispose)
                handle.Dispose();

            colorHandles.Clear();
            renderers.Clear();
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IColorSystemService>(this);
            sink.AddModule(this);
        }

        #endregion

        #region IColorSystemService (����� ���� �������̽� ����)

        // ������ �����ϰ� ����Ʈ�� ����� �� ��ȯ
        public IColorHandle CreateColorHandle(Renderer renderer)
        {
            if (renderer == null)
            {
                Debug.LogError("Renderer�� null�Դϴ�. ColorHandle�� ������ �� �����ϴ�.");
                return null;
            }

            if (renderers.Add(renderer) == false) 
            {
                Debug.LogError("Renderer�� �ߺ���� �Ǿ����ϴ�. ColorHandle�� ������ �� �����ϴ�.");
                return null;
            }

            // 1. ���ο� ���� ���� (������ ���� ColorHandle Ŭ����)
            ColorHandle newHandle = colorHandlePool.Get();

            newHandle.Set(renderer, this);

            // 2. Shutdown �� �޸� ������ ���� IDisposable ����Ʈ�� ���
            colorHandles.Add(newHandle);

            // 3. ����ڿ��� ��ȯ
            return newHandle;
        }

        #endregion

        #region IColorSystemOnlyScopService (���� �ý��� �������̽� ����)

        // ���� ���̹Ƿ� �ϴ� null�� ��ȯ�ϰų� �����
        public void ReturnColorHandle(ColorHandle handle)
        {
            colorHandles.Remove(handle);
            renderers.Remove(handle.GetTargetRender);
            colorHandlePool.Return(handle);
        }

        private ColorHandle PoolCreateColorHandle()
        {
            return new ColorHandle();
        }

        #endregion
    }

    #endregion
}
