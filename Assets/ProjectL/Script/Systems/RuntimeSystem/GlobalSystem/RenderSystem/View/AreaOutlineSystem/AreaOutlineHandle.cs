using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.MeshSystem;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.AreaOutlineSystem
{
    internal interface IAreaOutlineHandlePoolService
    {
        void Return(AreaOutlineHandle handle);
    }

    internal sealed class AreaOutlineHandle : IAreaOutlineHandle
    {
        private readonly IAreaOutlineHandlePoolService poolService;
        private readonly GameObject gameObject;
        private IMeshRenderHandle meshHandle;
        private IColorHandle colorHandle;

        internal MeshFilter MeshFilter { get; }
        internal MeshRenderer MeshRenderer { get; }
        internal bool IsValid { get; private set; }

        internal AreaOutlineHandle(
            IAreaOutlineHandlePoolService poolService,
            GameObject gameObject,
            MeshFilter meshFilter,
            MeshRenderer meshRenderer)
        {
            this.poolService = poolService ?? throw new ArgumentNullException(nameof(poolService));
            this.gameObject = gameObject ?? throw new ArgumentNullException(nameof(gameObject));
            MeshFilter = meshFilter ?? throw new ArgumentNullException(nameof(meshFilter));
            MeshRenderer = meshRenderer ?? throw new ArgumentNullException(nameof(meshRenderer));
        }

        internal void Validate() => IsValid = true;

        internal void Bind(IMeshRenderHandle mesh, IColorHandle color)
        {
            if (!IsValid)
                throw new InvalidOperationException("AreaOutlineHandle must be rented before binding.");

            meshHandle = mesh ?? throw new ArgumentNullException(nameof(mesh));
            colorHandle = color ?? throw new ArgumentNullException(nameof(color));
            gameObject.SetActive(true);
        }

        public void Update(AreaOutlineData data)
        {
            if (!IsValid || meshHandle == null || colorHandle == null)
                throw new ObjectDisposedException(nameof(AreaOutlineHandle));

            meshHandle.Update(data.ToMeshData());
            colorHandle.SetOverrideData(0, MaterialBaseTypeKey.MainColor, data.Color);
            // 점선·흐름 값은 핸들마다 넣는다. DashLength를 쓰면 메시가 루프마다 U를 이미 반복하므로 셰이더 반복 수는 1이다.
            colorHandle.SetOverrideData(0, ColorShaderPropertyKey.AreaOutlineDashEnabled, data.IsDashed ? 1f : 0f);
            colorHandle.SetOverrideData(0, ColorShaderPropertyKey.AreaOutlineDashCount, data.DashLength > 0f ? 1f : data.DashCount);
            colorHandle.SetOverrideData(0, ColorShaderPropertyKey.AreaOutlineDashFill, data.DashFill);
            colorHandle.SetOverrideData(0, ColorShaderPropertyKey.AreaOutlineFlowDirection,
                data.FlowDirection == AreaOutlineFlowDirection.Clockwise ? 1f : 0f);
            colorHandle.SetOverrideData(0, ColorShaderPropertyKey.AreaOutlineFlowSpeed, data.FlowSpeed);
        }

        public void Dispose()
        {
            if (IsValid)
                poolService.Return(this);
        }

        internal void Reset()
        {
            meshHandle?.Dispose();
            colorHandle?.Dispose();
            meshHandle = null;
            colorHandle = null;
            MeshRenderer.SetPropertyBlock(null, 0);
            gameObject.SetActive(false);
            IsValid = false;
        }

        internal void DestroyGameObject()
        {
            if (gameObject == null)
                return;

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(gameObject);
            else
                UnityEngine.Object.DestroyImmediate(gameObject);
        }
    }
}
