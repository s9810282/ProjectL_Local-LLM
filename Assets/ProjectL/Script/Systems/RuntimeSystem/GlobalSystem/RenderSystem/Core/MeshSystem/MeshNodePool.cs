using System;
using System.Collections.Generic;
using ProjectL.Framework.Utility;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.MeshSystem
{
    internal sealed class MeshNode
    {
        private MeshFilter meshFilter;
        private Mesh originalMesh;
        private Mesh runtimeMesh;

        public MeshFilter MeshFilter => meshFilter;

        public void Bind(MeshFilter target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            meshFilter = target;
            originalMesh = target.sharedMesh;
            runtimeMesh ??= CreateRuntimeMesh();
            target.sharedMesh = runtimeMesh;
        }

        public void Apply(MeshGeometryData data)
        {
            if (meshFilter == null)
                throw new InvalidOperationException("MeshNode is not bound to a MeshFilter.");

            runtimeMesh.name = data.Name;
            runtimeMesh.Clear();
            runtimeMesh.SetVertices(data.Vertices);
            runtimeMesh.SetTriangles(data.Indices, 0);
            runtimeMesh.SetUVs(0, data.UVs);
            runtimeMesh.SetNormals(data.Normals);
            runtimeMesh.SetColors(data.Colors);
            runtimeMesh.RecalculateBounds();
        }

        public void Unbind()
        {
            if (meshFilter != null && meshFilter.sharedMesh == runtimeMesh)
                meshFilter.sharedMesh = originalMesh;

            meshFilter = null;
            originalMesh = null;
        }

        public void DisposeRuntimeMesh()
        {
            Unbind();

            if (runtimeMesh == null)
                return;

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(runtimeMesh);
            else
                UnityEngine.Object.DestroyImmediate(runtimeMesh);
            runtimeMesh = null;
        }

        private static Mesh CreateRuntimeMesh()
        {
            var mesh = new Mesh
            {
                name = "RuntimeMesh"
            };
            mesh.MarkDynamic();
            return mesh;
        }
    }

    internal sealed class MeshNodePool : IDisposable
    {
        private sealed class MeshNodePoolLifecycle : IPoolLifecycle<MeshNode>
        {
            public void OnGet(MeshNode item) { }

            public void OnBeforeReturn(MeshNode item)
            {
                item.Unbind();
            }

            public void OnReturn(MeshNode item) { }
        }

        private readonly CustomPool<MeshNode> pool;
        private readonly HashSet<MeshNode> nodes = new HashSet<MeshNode>();

        public MeshNodePool()
        {
            pool = new CustomPool<MeshNode>(new PoolContext<MeshNode>(
                Create,
                10,
                null,
                new MeshNodePoolLifecycle()));
        }

        public MeshNode GetMeshNode()
        {
            return pool.Get();
        }

        public void ReturnMeshNode(MeshNode node)
        {
            pool.Return(node);
        }

        public void Dispose()
        {
            foreach (var node in nodes)
                node.DisposeRuntimeMesh();

            nodes.Clear();
        }

        private MeshNode Create()
        {
            var node = new MeshNode();
            nodes.Add(node);
            return node;
        }
    }
}
