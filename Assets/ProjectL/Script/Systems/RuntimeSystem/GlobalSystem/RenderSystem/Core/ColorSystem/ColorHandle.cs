using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem
{
    // ����ڰ� ���� �����ϵ��� ����� �������̽�
    public interface IColorHandle : IDisposable
    {
        void SetOverrideData<T>(int index, TypeSettingKey<T> key, T value);
    }

    public class ColorHandle : IColorHandle, IDisposable
    {
        private Renderer Renderer;

        internal Renderer GetTargetRender => Renderer;

        // Render�� �������� �ʰ� ������Ƽ ���ϸ� �����Ͽ� ����Ʈ�� ����
        private List<MaterialPropertyBlock> propertyBlocks = new();

        // TypeSettingKey<T>�� T�� Ű�� ����Ͽ� Delegate�� ����ϴ� ����ó
        private Dictionary<Type, Delegate> dispatchers = new();

        private IColorSystemOnlyHandleService Service;

        internal ColorHandle()
        {
            // TypeSettingKey<T>�� T�� �ٷ� �߷��Ͽ� enum ���� ����.
            RegisterDispatcher<float>(MaterialOvrridFunction.OverrideFloat);
            RegisterDispatcher<Color>(MaterialOvrridFunction.OverrideColor);
            RegisterDispatcher<int>(MaterialOvrridFunction.OverrideInt);
            RegisterDispatcher<Vector4>(MaterialOvrridFunction.OverrideVector);
        }

        // ������: Render�� �޾ƿͼ� MaterialPropertyBlock�� ���� �� ����
        internal void Set(Renderer renderer, IColorSystemOnlyHandleService service)
        {
            Renderer = renderer;

            if (renderer == null) throw new ArgumentNullException(nameof(renderer));
            if (service == null) throw new ArgumentNullException(nameof(service));

            this.Service = service;

            // 1. ��Ƽ���� �ε�����ŭ ������Ƽ ������ ������ ����Ʈ�� ����
            int materialCount = renderer.sharedMaterials.Length;
            for (int i = 0; i < materialCount; i++)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block, i);
                propertyBlocks.Add(block);
            }
        }

        // ����ó ��� �Լ�
        internal void RegisterDispatcher<T>(Action<MaterialPropertyBlock, int, T> action)
        {
            dispatchers[typeof(T)] = action;
        }

        // �������̽� ����: Index, TypeSettingKey, T value�� �޾� ����
        public void SetOverrideData<T>(int index, TypeSettingKey<T> key, T value)
        {
            if (index < 0 || index >= propertyBlocks.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            MaterialPropertyBlock block = propertyBlocks[index];
            int nameID = Shader.PropertyToID(key.Name);

            // TypeSettingKey<T>�� T�� �����ϹǷ�, enum �ִ� Ű ���� ������ �����.
            if (dispatchers.TryGetValue(typeof(T), out Delegate del))
            {
                if (del is Action<MaterialPropertyBlock, int, T> action)
                {
                    action.Invoke(block, nameID, value);

                    Renderer.SetPropertyBlock(block, index);
                }
                else
                {
                    Debug.LogError($"Ÿ�� ĳ���� ���� - ��ϵ� ��������Ʈ�� ���޵� T�� ��ġ���� �ʽ��ϴ�: {typeof(T).Name}");
                }
            }
            else
            {
                Debug.LogError($"����ó�� ��ϵ��� ���� ������Ʈ Ÿ���Դϴ�: {typeof(T).Name}");
            }
        }

        public void Dispose()
        {
            if (Renderer == null)
                return;

            for (int i = 0; i < propertyBlocks.Count; i++)
            {
                propertyBlocks[i].Clear();
            }

            propertyBlocks.Clear();

            Service.ReturnColorHandle(this); // ���� ��ȯ
            Renderer = null;
            Service = null;
        }
    }
}
