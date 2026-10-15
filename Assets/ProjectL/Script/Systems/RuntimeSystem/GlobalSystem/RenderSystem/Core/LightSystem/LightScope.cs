using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Light.Enum;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.LightSystem
{
    // ����ڰ� ������ �����ϴ� Proxy���� Pool�� ���� ���
    public class LightHandle : IDisposable
    {
        public readonly LightPresetType PresetType;
        internal readonly Light TargetLight; // Scope���� �ʱ� ������ ���� internal�� ����
        private readonly ILightSystemScopeServices _system;

        public bool IsValid { get; private set; }

        internal LightHandle(LightPresetType type, Light targetLight, ILightSystemScopeServices system)
        {
            PresetType = type;
            TargetLight = targetLight ?? throw new ArgumentNullException(nameof(targetLight));
            _system = system;
            IsValid = false;
        }

        internal void Validate() => IsValid = true;
        internal void Invalidate() => IsValid = false;

        // ���� �ڵ��� ���ؼ� �߰������� ������ �� �ִ� ���ѵ�
        public void SetLocalPosition(Vector3 position) { if (IsValid) TargetLight.transform.localPosition = position; }
        public void SetLocalRotation(Quaternion rotation) { if (IsValid) TargetLight.transform.localRotation = rotation; }
        public void SetColor(Color color) { if (IsValid) TargetLight.color = color; }
        public void SetIntensity(float intensity) { if (IsValid) TargetLight.intensity = intensity; }

        public void Dispose()
        {
            if (!IsValid) return;

            // �ý������� �ݳ� (���� PoolLifecycle�� OnBeforeReturn���� Invalidate ��)
            _system.ReturnLightHandle(this);
        }
    }

    // Handle���� ��Ƶΰ� �ϰ� �����ϴ� Context ������
    public class LightScope : IDisposable
    {
        private readonly List<LightHandle> _handles = new List<LightHandle>();
        private readonly ILightSystemScopeServices _system;
        private bool _isDisposed = false;

        internal LightScope(ILightSystemScopeServices system)
        {
            _system = system;
        }

        // ������ �߰��ϰ�, Handle ��ü ��� ���ٿ� Index(int)�� ��ȯ�մϴ�.
        public int RequestLight(LightSnapshotData snapshot)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(LightScope));

            LightHandle handle = _system.GetLightHandle(snapshot.PresetType);

            Transform targetTransform = handle.TargetLight.transform;
            targetTransform.SetParent(snapshot.Parent, false);
            targetTransform.localPosition = snapshot.LocalPosition;
            targetTransform.localRotation = snapshot.LocalRotation;

            handle.TargetLight.color = snapshot.Color;
            handle.TargetLight.intensity = snapshot.Intensity;
            handle.TargetLight.gameObject.SetActive(true); // �⺻ Ȱ��ȭ

            _handles.Add(handle);

            // ��ϵ� �迭�� �ε����� ��ȯ
            return _handles.Count - 1;
        }

        // ��� 1: �ε����� ���� ��� ����
        public LightHandle GetHandle(int index)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(LightScope));
            if (index < 0 || index >= _handles.Count) throw new IndexOutOfRangeException($"[LightScope] Invalid index: {index}");

            return _handles[index];
        }

        // ��� 2: ��ü�� �����ֱ�
        public void ShowAll()
        {
            if (_isDisposed) return;
            foreach (var handle in _handles)
            {
                if (handle.IsValid)
                    handle.TargetLight.gameObject.SetActive(true);
            }
        }

        // ��� 3: ��ü�� �����
        public void HideAll()
        {
            if (_isDisposed) return;
            foreach (var handle in _handles)
            {
                if (handle.IsValid)
                    handle.TargetLight.gameObject.SetActive(false);
            }
        }

        public void ReturnAll() 
        {
            foreach (var handle in _handles)
            {
                handle.Dispose();
            }
            _handles.Clear();
        }

        // ��� 4: ��ȯ�ϱ�
        public void Dispose()
        {
            if (_isDisposed) return;

            // �����ϰ� �ִ� ��� �ڵ��� �ý������� �ϰ� ��ȯ
            foreach (var handle in _handles)
            {
                handle.Dispose();
            }
            _handles.Clear();

            _system.RemoveScope(this);
            _isDisposed = true;
        }
    }
}