using System;
using System.Collections.Generic;
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
        public readonly Vector3 worldOriginOffset;

        public ElementNode(CreateElementNodeSnapShot data)
        {
            this.effectType = data.effectType;
            this.particleStopNotifier = data.particleStopNotifier;

            if (particleStopNotifier == null)
            {
                throw new InvalidOperationException(
                    $"[EffectPool] EffectType={effectType}: 인스턴스 루트에 ParticleStopNotifier가 없거나 파괴됐습니다.");
            }

            EffectPrefabOriginOffset placement = particleStopNotifier.GetComponent<EffectPrefabOriginOffset>();
            worldOriginOffset = placement != null ? placement.WorldOffset : Vector3.zero;
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
        // 초기 생성 도중 실패하면 이미 만든 인스턴스도 함께 회수합니다. 초기화가 끝나면 참조를 비웁니다.
        private List<GameObject> _initializingInstances;

        public EffectPool(EffectElementData data, Transform root)
        {
            this.Data = data ?? throw new ArgumentNullException(nameof(data));
            this.root = root ?? throw new ArgumentNullException(nameof(root));

            _initializingInstances = new List<GameObject>(data.EffectPoolInitializeCount);
            try
            {
                Pool = new CustomPool<ElementNode>(new PoolContext<ElementNode>
                    (
                    CreateElement,
                    data.EffectPoolInitializeCount,
                    null,
                    new EffectPoolSetting(root)
                    ));
            }
            catch
            {
                foreach (GameObject instance in _initializingInstances)
                    DestroyFailedInstance(instance);
                throw;
            }
            finally
            {
                _initializingInstances = null;
            }
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
            // 원본 구성은 EffectSystem이 검사합니다. 풀은 생성된 인스턴스와 그 수명을 책임집니다.
            string source = Describe(Data);
            if (root == null)
                throw new InvalidOperationException($"{source}: 이펙트 풀의 부모가 파괴됐습니다.");

            GameObject instance = null;
            try
            {
                instance = GameObject.Instantiate(Data.Prefab);
                UnityEngine.Object.DontDestroyOnLoad(instance);
                instance.SetActive(false);
                instance.transform.SetParent(root, false);

                // 원본 검사 이후 인스턴스 콜백 등으로 필수 컴포넌트가 사라진 경우도 명시적으로 실패합니다.
                if (!instance.TryGetComponent(out ParticleSystem particleSystem))
                    throw new InvalidOperationException("생성된 인스턴스 루트에 ParticleSystem이 없습니다.");
                if (!instance.TryGetComponent(out ParticleStopNotifier notifier))
                    throw new InvalidOperationException("생성된 인스턴스 루트에 ParticleStopNotifier가 없습니다.");
                particleSystem.Stop();

                var element = new ElementNode(new CreateElementNodeSnapShot(Data.EffectType, notifier));
                _initializingInstances?.Add(instance);
                return element;
            }
            catch (Exception exception)
            {
                DestroyFailedInstance(instance);
                throw new InvalidOperationException($"{source}: 이펙트 인스턴스 생성에 실패했습니다. {exception.Message}", exception);
            }
        }

        private static string Describe(EffectElementData data) =>
            $"[EffectPool] EffectType={data.EffectType} ({(int)data.EffectType}), Prefab='{(data.Prefab != null ? data.Prefab.name : "<미지정/파괴됨>")}'";

        private static void DestroyFailedInstance(GameObject instance)
        {
            if (instance == null) return;
            instance.SetActive(false);
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(instance);
            else
                UnityEngine.Object.DestroyImmediate(instance);
        }
    }
}
