using System;
using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Effect.Enum;
using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Effect.View.Table.Item;
using ProjectL.Framework.Utility;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.EffectSystem
{
    internal struct CreateElementNodeSnapShot 
    {
        public readonly EffectType effectType;
        public readonly ParticleStopNotifier particleStopNotifier;

        public CreateElementNodeSnapShot(EffectType effectType, ParticleStopNotifier particleStopNotifier) 
        {
            this.effectType = effectType;
            this.particleStopNotifier = particleStopNotifier;
        }
    }

    internal class ElementNode
    {
        public readonly EffectType effectType;
        public readonly ParticleStopNotifier particleStopNotifier;

        public ElementNode(CreateElementNodeSnapShot data)
        {
            this.effectType = data.effectType;
            this.particleStopNotifier = data.particleStopNotifier;

            if (particleStopNotifier == null)
            {
                throw new ArgumentException("This Object is not have particleStopNotifier");
            }
        }
    }

    internal interface IEffectPoolService<TElement>
    {
        public TElement GetElement();
        public void ReturnElement(TElement element);
    }

    internal class EffectPool : IEffectPoolService<ElementNode>
    {
        private sealed class EffectPoolSetting : IPoolLifecycle<ElementNode>
        {
            private readonly Transform root;

            public EffectPoolSetting(Transform root) => this.root = root;

            void IPoolLifecycle<ElementNode>.OnGet(ElementNode item)
            {
                
            }

            void IPoolLifecycle<ElementNode>.OnBeforeReturn(ElementNode item)
            {
                item.particleStopNotifier.gameObject.SetActive(false);
                item.particleStopNotifier.transform.SetParent(root, false);
                item.particleStopNotifier.StopEmitting();
            }

            void IPoolLifecycle<ElementNode>.OnReturn(ElementNode item)
            {
                
            }
        }

        private readonly EffectElementData Data;
        private readonly Transform root;
        private readonly CustomPool<ElementNode> Pool;

        public EffectPool(EffectElementData data, Transform root)
        {
            this.Data = data;
            this.root = root ?? throw new ArgumentNullException(nameof(root));

            Pool = new CustomPool<ElementNode>(new PoolContext<ElementNode>
                (
                CreateElement,
                data.EffectPoolInitializeCount,
                null,
                new EffectPoolSetting(root)
                ));
        }

        public ElementNode GetElement()
        {
             return Pool.Get();
        }

        public void ReturnElement(ElementNode element)
        {
            Pool.Return(element);
        }

        private ElementNode CreateElement()
        {
            GameObject EffectObject = GameObject.Instantiate(Data.Prefab);
            UnityEngine.Object.DontDestroyOnLoad(EffectObject);
            EffectObject.gameObject.SetActive(false);
            EffectObject.gameObject.transform.SetParent(root, false);

            ParticleSystem particleSystem = EffectObject.GetComponent<ParticleSystem>();
            particleSystem.Stop();

            ParticleStopNotifier particleStopNotifier = EffectObject.GetComponent<ParticleStopNotifier>();

            return new ElementNode(new CreateElementNodeSnapShot(Data.EffectType, particleStopNotifier));
        }
    }
}
