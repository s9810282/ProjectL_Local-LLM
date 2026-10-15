using System;
using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Light.View.Table.Item;
using ProjectL.Framework.Utility;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.LightSystem
{
    internal interface ILightPoolService<TElement>
    {
        TElement GetElement();
        void ReturnElement(TElement element);
    }

    internal class LightPool : ILightPoolService<LightHandle>
    {
        private sealed class LightPoolSetting : IPoolLifecycle<LightHandle>
        {
            private readonly Transform root;

            public LightPoolSetting(Transform root) => this.root = root;

            void IPoolLifecycle<LightHandle>.OnGet(LightHandle item)
            {
                item.Validate(); // Ǯ���� ���� �� ��ȿ ���·� ��ȯ
            }

            void IPoolLifecycle<LightHandle>.OnBeforeReturn(LightHandle item)
            {
                // Ǯ�� ���ư��� ���� ���� ���� ����, ��ӹ޾Ҵ� �θ� ���
                item.TargetLight.gameObject.SetActive(false);
                item.TargetLight.transform.SetParent(root, false);
                item.Invalidate(); // ����� ���� ����
            }

            void IPoolLifecycle<LightHandle>.OnReturn(LightHandle item) { }
        }

        private readonly LightData _data;
        private readonly CustomPool<LightHandle> _pool;
        private readonly ILightSystemScopeServices _system;
        private readonly Transform root;

        public LightPool(LightData data, ILightSystemScopeServices system, Transform root)
        {
            _data = data;
            _system = system;
            this.root = root ?? throw new ArgumentNullException(nameof(root));
            _pool = new CustomPool<LightHandle>(new PoolContext<LightHandle>
            (
                CreateElement,
                data.InitialPoolSize,
                null,
                new LightPoolSetting(root)
            ));
        }

        public LightHandle GetElement() => _pool.Get();
        public void ReturnElement(LightHandle element) => _pool.Return(element);

        private LightHandle CreateElement()
        {
            GameObject lightObj = GameObject.Instantiate(_data.LightPrefab.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(lightObj);
            lightObj.SetActive(false);
            lightObj.transform.SetParent(root, false);

            Light lightComponent = lightObj.GetComponent<Light>();
            if (lightComponent == null)
            {
                throw new Exception($"[LightPool] Prefab of type {_data.PresetType} does not have a Light component.");
            }

            // LightNode ��� LightHandle�� ���� �����Ͽ� ��ȯ (�ý��� ���� ����)
            return new LightHandle(_data.PresetType, lightComponent, _system);
        }
    }
}
