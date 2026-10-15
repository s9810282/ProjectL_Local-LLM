using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Light.Enum;
using ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase.RenderTable;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.LightSystem
{
    #region 서비스

    public interface ILightSystemService : IServiceModule
    {
        LightScope CreateScope();
    }

    internal interface ILightSystemScopeServices
    {
        LightHandle GetLightHandle(LightPresetType type);
        void ReturnLightHandle(LightHandle handle);
        void RemoveScope(LightScope scope);
    }

    #endregion

    #region 시스템 모듈

    internal class LightSystem :
        ISystemBindScene,
        ISystemInitialize,
        ISystemShutdown,
        IExportSystemServices,
        ILightSystemService,
        ILightSystemScopeServices,
        ISystemInitData<IHasLightQuery>
    {
        private readonly RenderRuntimeRoot runtimeRoot;
        private Transform lightRoot;
        private ILightTableQuery _query;
        private readonly HashSet<LightScope> _activeScopes = new HashSet<LightScope>();

        // Node ��� Handle�� Ǯ��
        private readonly Dictionary<LightPresetType, ILightPoolService<LightHandle>> _poolDict
            = new Dictionary<LightPresetType, ILightPoolService<LightHandle>>();

        #region System Lifecycle

        internal LightSystem(RenderRuntimeRoot runtimeRoot)
        {
            this.runtimeRoot = runtimeRoot ?? throw new ArgumentNullException(nameof(runtimeRoot));
        }

        public void BindScene()
        {
            lightRoot = runtimeRoot.GetOrCreateSystemRoot("LightRoot");
        }

        public void InitData(IHasLightQuery data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("LightQuery Data is Null. Check DataBase RenderGroup Factory");
            }
            _query = data.LightTableQuery;
        }

        public void Initialize() { }

        public void Shutdown()
        {
            var scopesToDispose = new LightScope[_activeScopes.Count];
            _activeScopes.CopyTo(scopesToDispose);

            foreach (var scope in scopesToDispose)
            {
                scope.Dispose();
            }

            _activeScopes.Clear();
            _poolDict.Clear();
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<ILightSystemService>(this);
            sink.AddModule(this);
        }

        #endregion

        #region Public Service (ILightSystemService)

        public LightScope CreateScope()
        {
            LightScope newScope = new LightScope(this);
            _activeScopes.Add(newScope);
            return newScope;
        }

        #endregion

        #region Internal Scope Services (ILightSystemScopeServices)

        LightHandle ILightSystemScopeServices.GetLightHandle(LightPresetType type)
        {
            if (!_poolDict.TryGetValue(type, out var pool))
            {
                if (_query.TryGetLight(type, out var lightData))
                {
                    // Pool ���� �� this�� �Ѱ��־� Handle�� �ý��ۿ� �ݳ��� �� �ְ� ��
                    if (lightRoot == null)
                        throw new InvalidOperationException("LightSystem requires BindScene before creating a light pool.");

                    pool = new LightPool(lightData, this, lightRoot);
                    _poolDict.Add(type, pool);
                }
                else
                {
                    throw new Exception($"[LightSystem] Database does not have this Light Preset: {type}");
                }
            }

            return pool.GetElement();
        }

        void ILightSystemScopeServices.ReturnLightHandle(LightHandle handle)
        {
            if (handle == null) return;

            if (!_poolDict.TryGetValue(handle.PresetType, out var pool))
            {
                throw new InvalidOperationException($"[LightSystem] Cannot find owner pool for type: {handle.PresetType}");
            }

            pool.ReturnElement(handle);
        }

        void ILightSystemScopeServices.RemoveScope(LightScope scope)
        {
            _activeScopes.Remove(scope);
        }

        #endregion
    }

    #endregion
}
