using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.OutlineSystem
{
    public struct OutlineData
    {
        public readonly Transform Target;
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;

        public OutlineData(Transform target, Vector3 position, Quaternion rotation)
        {
            this.Target = target;
            this.Position = position;
            this.Rotation = rotation;
        }
    }

    public interface IOutlineScopService : IDisposable
    {
        IColorHandle ColorHandle { get; }
    }

    public class OutlineScop : IOutlineScopService
    {
        private OutlineNode node;
        private IOutlineSystemOnlyScopService systemService;

        public IColorHandle ColorHandle => node?.colorHandle;

        internal OutlineScop(
            OutlineNode node,
            IOutlineSystemOnlyScopService systemService,
            OutlineData data)
        {
            this.node = node;
            this.systemService = systemService;

            ApplyTo(data);
        }

        private void ApplyTo(OutlineData data)
        {
            if (data.Target == null) return;

            var targetFilter = data.Target.GetComponent<MeshFilter>();
            if (targetFilter != null && node.meshFilter != null)
            {
                node.meshFilter.sharedMesh = targetFilter.sharedMesh;
            }

            node.gameObject.transform.SetParent(data.Target);
            node.gameObject.transform.localPosition = data.Position;
            node.gameObject.transform.localRotation = data.Rotation;
            node.gameObject.transform.localScale = Vector3.one;

            node.gameObject.SetActive(true);
        }

        public void Dispose()
        {
            if (node != null)
            {
                // �ý��ۿ� �ڱ� �ڽ�(Scop)�� ����ߴ� Node�� ���� �����Ͽ� ����Ʈ���� ���ŵǰ� ��
                systemService.ReturnOutlineScop(this, node);
                node = null;
            }
        }
    }
}
