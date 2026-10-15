using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Effect.Enum;
using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Effect.View.Table.Item;
using ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase.RenderTable;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.EffectSystem
{
    #region 서비스

    public interface IEffectSystemServices : IServiceModule
    {
        public EffectScope GetEffectHandle();
    }

    internal interface IEffectSystemScopeServices
    {
        public ElementNode GetElementNode(EffectType effectType);
        public void ReturnElementNode(ElementNode element);
    }

    #endregion

    #region 시스템 모듈

    internal class EffectSystem :
        ISystemBindScene,
        ISystemInitData<IHasEffectQuery>,
        ISystemPostBindService,
        ISystemInitialize,
        ISystemShutdown,
        IExportSystemServices,

        IEffectSystemServices, 
        IEffectSystemScopeServices
    {
        private readonly RenderRuntimeRoot runtimeRoot;
        private Transform effectRoot;
        private IEffectTableQuery queryService { get; set; }

        private List<IDisposaHandle> disposaHandles = new List<IDisposaHandle>();
        private Dictionary<EffectType, IEffectPoolService<ElementNode>> EffectPoolData = new Dictionary<EffectType, IEffectPoolService<ElementNode>>();

        internal EffectSystem(RenderRuntimeRoot runtimeRoot)
        {
            this.runtimeRoot = runtimeRoot ?? throw new ArgumentNullException(nameof(runtimeRoot));
        }

        public void BindScene()
        {
            effectRoot = runtimeRoot.GetOrCreateSystemRoot("EffectRoot");
        }

        public void InitData(IHasEffectQuery systemDataQuery)
        {
           if (systemDataQuery != null)
           {
                queryService = systemDataQuery.EffectTableQuery;
           }
           else
           {
               Debug.LogWarning("Not EffectData Init");
           }
        }

        public void PostBindService(ISystemServiceStoreReader systemServiceResolver)
        {
            
        }

        public void Initialize()
        {
            
        }

        public void Shutdown()
        {
            for (int i = 0; i < disposaHandles.Count; i++)
            {
                disposaHandles[i].DisposHandle();
            }

            disposaHandles.Clear();
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IEffectSystemServices>(this);
            sink.AddModule(this);
        }

        #region SystemFunction

        private EffectScope CreateHandle()
        {
            EffectScope effectHandle = new EffectScope(this);

            disposaHandles.Add(effectHandle);

            return effectHandle;
        }

        private IEffectPoolService<ElementNode> GetEffectPool(EffectElementData data) 
        {
            ValidateEffectData(data);

            if (EffectPoolData.TryGetValue(data.EffectType, out IEffectPoolService<ElementNode> pool) == false) 
            {
                if (effectRoot == null)
                    throw new InvalidOperationException("EffectSystem requires BindScene before creating an effect pool.");

                EffectPool effectpool = new EffectPool(data, effectRoot);
                pool = effectpool;
                EffectPoolData.Add(data.EffectType, pool);
            }
            return pool;
        }

        #endregion

        #region ServiceFuntion

        public EffectScope GetEffectHandle()
        {
            return CreateHandle();
        }

        ElementNode IEffectSystemScopeServices.GetElementNode(EffectType effectType)
        {
            if (EffectPoolData.TryGetValue(effectType, out var pool) == false)
            {
                if(queryService.TryGetEffect(effectType, out var effect) == true) 
                {
                    pool = GetEffectPool(effect);
                }
                else 
                {
                    throw new Exception($"Effect DataBase Not Have this Key : {effectType}");
                }
            }

            return pool.GetElement();
        }

        void IEffectSystemScopeServices.ReturnElementNode(ElementNode element)
        {
            if (element == null)
                return;

            if (!EffectPoolData.TryGetValue(element.effectType, out var pool))
            {
                throw new InvalidOperationException(
                    $"Cannot find owner pool. Type: {element.effectType}");
            }

            pool.ReturnElement(element);
        }

        #endregion


        #region ValidateFunction

        private void ValidateEffectData(EffectElementData data) 
        {
            if(data.EffectType == EffectType.Empty) 
            {
                throw new Exception("Your not setting EffectType\n");
            }

            if(data.Prefab == null) 
            {
                throw new Exception($"this Data : {data.GetType()} \n" +
                    $"Not Have EffectObject\n");
            }

            if (data.Prefab.GetComponentInChildren<ParticleSystem>() == null)
            {
                throw new Exception($"[EffectSystem] {data.EffectType} ������ ���ο� ��ƼŬ �ý����� �������� �ʽ��ϴ�.");
            }
        }

        #endregion
    }

    #endregion
}
