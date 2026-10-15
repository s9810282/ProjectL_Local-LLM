using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.MeshSystem
{
    public interface IMeshSystemService : IServiceModule
    {
        IMeshRenderHandle Register(MeshFilter meshFilter);
    }

    internal sealed class MeshSystem :
        ISystemInitialize,
        ISystemShutdown,
        IExportSystemServices,
        IMeshSystemService,
        IMeshRenderHandleService
    {
        private readonly MeshNodePool meshNodePool = new MeshNodePool();
        private readonly Dictionary<MeshFilter, MeshNode> activeNodes = new Dictionary<MeshFilter, MeshNode>();
        private readonly HashSet<Type> registeredTypes = new HashSet<Type>();
        private readonly Dictionary<Type, Delegate> createFunctions = new Dictionary<Type, Delegate>();

        public void Initialize()
        {
            RegisterCreateFunction<CubeCreateData>(MeshHightLevelBuilder.CreateCube);
            RegisterCreateFunction<CircleCreateData>(MeshHightLevelBuilder.CreateCircle);
            RegisterCreateFunction<CylinderCreateData>(MeshHightLevelBuilder.CreateCylinder);
            RegisterCreateFunction<SingleLineCreateData>(MeshHightLevelBuilder.CreateSingleLine);
            RegisterCreateFunction<MultiLineCreateData>(MeshHightLevelBuilder.CreateMultiLine);
            RegisterCreateFunction<GridCubeCreateData>(MeshHightLevelBuilder.CreateGridCubes);
            RegisterCreateFunction<GridOutlineCreateData>(MeshHightLevelBuilder.CreateGridOutline);
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IMeshSystemService>(this);
            sink.AddModule(this);
        }

        public void Shutdown()
        {
            var nodes = new List<MeshNode>(activeNodes.Values);
            foreach (var node in nodes)
                Release(node);

            meshNodePool.Dispose();
            activeNodes.Clear();
            registeredTypes.Clear();
            createFunctions.Clear();
        }

        public IMeshRenderHandle Register(MeshFilter meshFilter)
        {
            if (meshFilter == null)
                throw new ArgumentNullException(nameof(meshFilter));

            if (activeNodes.ContainsKey(meshFilter))
                throw new InvalidOperationException("The MeshFilter is already registered.");

            var node = meshNodePool.GetMeshNode();

            try
            {
                node.Bind(meshFilter);
                activeNodes.Add(meshFilter, node);
                return new MeshRenderHandle(this, node);
            }
            catch
            {
                meshNodePool.ReturnMeshNode(node);
                throw;
            }
        }

        void IMeshRenderHandleService.Update<TData>(MeshNode node, TData data)
        {
            if (node == null || node.MeshFilter == null ||
                !activeNodes.TryGetValue(node.MeshFilter, out var activeNode) ||
                !ReferenceEquals(activeNode, node))
            {
                throw new InvalidOperationException("The mesh handle is no longer registered.");
            }

            var type = typeof(TData);
            if (!createFunctions.TryGetValue(type, out var createFunction))
                throw new InvalidOperationException($"No mesh creator is registered for type {type}.");

            var create = (Func<TData, MeshGeometryData>)createFunction;
            node.Apply(create(data));
        }

        void IMeshRenderHandleService.Release(MeshNode node)
        {
            Release(node);
        }

        private void Release(MeshNode node)
        {
            if (node == null || node.MeshFilter == null ||
                !activeNodes.TryGetValue(node.MeshFilter, out var activeNode) ||
                !ReferenceEquals(activeNode, node))
            {
                return;
            }

            activeNodes.Remove(node.MeshFilter);
            meshNodePool.ReturnMeshNode(node);
        }

        private void RegisterCreateFunction<TData>(Func<TData, MeshGeometryData> create)
            where TData : IMeshUpdateData
        {
            var type = typeof(TData);
            if (!registeredTypes.Add(type))
                throw new InvalidOperationException($"A mesh creator is already registered for type {type}.");

            createFunctions.Add(type, create ?? throw new ArgumentNullException(nameof(create)));
        }
    }
}
