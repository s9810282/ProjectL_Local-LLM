using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Material.View.Table.Item;
using ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase.RenderTable;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.MeshSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.AreaOutlineSystem;
using UnityEngine;

namespace ProjectL.Tests.Framework
{
    public sealed class MeshSystemHandleTests
    {
        private GameObject root;
        private MeshFilter meshFilter;
        private Mesh originalMesh;
        private IMeshSystemService meshSystem;
        private ISystemInitialize initialize;
        private ISystemShutdown shutdown;

        [SetUp]
        public void SetUp()
        {
            Type type = typeof(IMeshSystemService).Assembly.GetType(
                "ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.MeshSystem.MeshSystem", true);
            object system = Activator.CreateInstance(type, true);

            meshSystem = (IMeshSystemService)system;
            initialize = (ISystemInitialize)system;
            shutdown = (ISystemShutdown)system;
            initialize.Initialize();

            root = new GameObject("Mesh Handle Contract Test");
            meshFilter = root.AddComponent<MeshFilter>();
            originalMesh = new Mesh { name = "Original Mesh" };
            meshFilter.sharedMesh = originalMesh;
        }

        [TearDown]
        public void TearDown()
        {
            shutdown?.Shutdown();

            if (root != null)
                UnityEngine.Object.DestroyImmediate(root);
            if (originalMesh != null)
                UnityEngine.Object.DestroyImmediate(originalMesh);
        }

        [Test]
        public void Register_BindsRuntimeMeshAndDisposeRestoresOriginalMesh()
        {
            IMeshRenderHandle handle = meshSystem.Register(meshFilter);
            Mesh runtimeMesh = meshFilter.sharedMesh;

            Assert.That(runtimeMesh, Is.Not.SameAs(originalMesh));

            handle.Update(new CubeCreateData(
                "Range",
                Vector3.zero,
                Vector3.one,
                Color.white));

            Assert.That(runtimeMesh.vertexCount, Is.EqualTo(24));
            Assert.That(runtimeMesh.triangles, Has.Length.EqualTo(36));

            handle.Dispose();

            Assert.That(meshFilter.sharedMesh, Is.SameAs(originalMesh));
        }

        [Test]
        public void Update_RewritesTheBoundRuntimeMesh()
        {
            using IMeshRenderHandle handle = meshSystem.Register(meshFilter);

            handle.Update(new CubeCreateData(
                "First",
                Vector3.zero,
                Vector3.one,
                Color.white));
            Mesh runtimeMesh = meshFilter.sharedMesh;

            handle.Update(new CubeCreateData(
                "Updated",
                Vector3.zero,
                Vector3.one * 2f,
                Color.red));

            Assert.That(runtimeMesh.name, Is.EqualTo("Updated"));
        }

        [Test]
        public void Register_SameMeshFilterTwice_Throws()
        {
            using IMeshRenderHandle handle = meshSystem.Register(meshFilter);

            Assert.Throws<InvalidOperationException>(() => meshSystem.Register(meshFilter));
        }

        [Test]
        public void Update_GridOutlineOmitsSharedCellEdges()
        {
            using IMeshRenderHandle handle = meshSystem.Register(meshFilter);

            handle.Update(new GridOutlineCreateData(
                "Grid Outline",
                new[] { Vector2Int.zero, Vector2Int.right },
                Vector3.zero,
                Vector2.one,
                0.01f,
                0.05f,
                Color.white));

            Mesh runtimeMesh = meshFilter.sharedMesh;

            // Six boundary sections share vertex pairs; only the UV seam is duplicated.
            Assert.That(runtimeMesh.vertexCount, Is.EqualTo(14));
            Assert.That(runtimeMesh.triangles, Has.Length.EqualTo(36));
        }

        [Test]
        public void Update_GridOutlineKeepsOuterCornersFilledWhenThicknessChanges()
        {
            using IMeshRenderHandle handle = meshSystem.Register(meshFilter);

            foreach (float thickness in new[] { 0.1f, 0.4f })
            {
                handle.Update(new GridOutlineCreateData(
                    "Grid Outline",
                    new[] { Vector2Int.zero },
                    Vector3.zero,
                    Vector2.one,
                    0.01f,
                    thickness,
                    Color.white));

                float corner = 0.5f + thickness * 0.375f;
                Assert.That(CoversXZ(meshFilter.sharedMesh, new Vector2(corner, corner)), Is.True,
                    $"Outer corner has a gap at thickness {thickness}.");
            }
        }

        [Test]
        public void Update_GridOutlineUnwrapsOneContinuousStripWithSharedMiterCorners()
        {
            using IMeshRenderHandle handle = meshSystem.Register(meshFilter);
            handle.Update(new GridOutlineCreateData(
                "Strip", new[] { Vector2Int.zero, Vector2Int.right },
                Vector3.zero, new Vector2(2f, 1f), 0.01f, 0.1f, Color.white));

            Mesh mesh = meshFilter.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            Vector2[] uv = mesh.uv;
            Assert.That(vertices[0], Is.EqualTo(vertices[vertices.Length - 2]));
            Assert.That(vertices[1], Is.EqualTo(vertices[vertices.Length - 1]));
            Assert.That(uv[0], Is.EqualTo(Vector2.zero));
            Assert.That(uv[uv.Length - 1], Is.EqualTo(Vector2.one));
            float perimeter = 10f; // A 4-by-1 rectangle.
            for (int i = 0; i < vertices.Length - 2; i += 2)
            {
                Assert.That(uv[i].x, Is.EqualTo(uv[i + 1].x));
                Assert.That(uv[i].y, Is.EqualTo(0f));
                Assert.That(uv[i + 1].y, Is.EqualTo(1f));
                Vector3 start = (vertices[i] + vertices[i + 1]) * 0.5f;
                Vector3 end = (vertices[i + 2] + vertices[i + 3]) * 0.5f;
                Assert.That(uv[i + 2].x - uv[i].x,
                    Is.EqualTo(Vector3.Distance(start, end) / perimeter).Within(0.00001f));
            }
            Assert.That(TriangleAreaXZ(mesh), Is.EqualTo(perimeter * 0.1f).Within(0.00001f),
                "Miter joins must not overlap and darken transparent corners.");
        }

        [Test]
        public void Update_GridOutlineHandlesConcaveCornersHolesAndDiagonalIslands()
        {
            using IMeshRenderHandle handle = meshSystem.Register(meshFilter);
            var shapes = new[]
            {
                new[] { Vector2Int.zero, Vector2Int.right, Vector2Int.up },
                new[] { Vector2Int.zero, Vector2Int.one },
                new[]
                {
                    new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0),
                    new Vector2Int(0, 1), new Vector2Int(2, 1),
                    new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2)
                }
            };
            int[] expectedLoops = { 1, 2, 2 };
            float[] perimeters = { 8f, 8f, 16f };
            for (int shape = 0; shape < shapes.Length; shape++)
            {
                handle.Update(new GridOutlineCreateData(
                    "Loops", shapes[shape], Vector3.zero, Vector2.one, 0f, 0.1f, Color.white));
                Mesh mesh = meshFilter.sharedMesh;
                int seams = 0;
                foreach (Vector2 uv in mesh.uv)
                    if (uv == Vector2.zero) seams++;
                Assert.That(seams, Is.EqualTo(expectedLoops[shape]));
                Assert.That(TriangleAreaXZ(mesh), Is.EqualTo(perimeters[shape] * 0.1f).Within(0.00001f));
            }
        }

        private static float TriangleAreaXZ(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            float area = 0f;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = vertices[triangles[i]];
                Vector3 b = vertices[triangles[i + 1]];
                Vector3 c = vertices[triangles[i + 2]];
                float signedArea = Vector3.Cross(b - a, c - a).y * 0.5f;
                Assert.That(signedArea, Is.GreaterThan(0f), "All strip triangles must face up.");
                area += signedArea;
            }
            return area;
        }

        private static bool CoversXZ(Mesh mesh, Vector2 point)
        {
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = vertices[triangles[i]];
                Vector3 b = vertices[triangles[i + 1]];
                Vector3 c = vertices[triangles[i + 2]];
                float ab = CrossXZ(a, b, point);
                float bc = CrossXZ(b, c, point);
                float ca = CrossXZ(c, a, point);
                if ((ab >= 0f && bc >= 0f && ca >= 0f) ||
                    (ab <= 0f && bc <= 0f && ca <= 0f))
                    return true;
            }

            return false;
        }

        private static float CrossXZ(Vector3 a, Vector3 b, Vector2 point)
        {
            return (b.x - a.x) * (point.y - a.z) - (b.z - a.z) * (point.x - a.x);
        }
    }

    public sealed class AreaOutlineHandleTests
    {
        private Material outlineMaterial;
        private ISystemShutdown meshShutdown;
        private ISystemShutdown colorShutdown;
        private ISystemShutdown areaShutdown;

        [SetUp]
        public void SetUp()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            Assert.That(shader, Is.Not.Null, "The project's URP Lit shader is required.");
            outlineMaterial = new Material(shader);
        }

        [TearDown]
        public void TearDown()
        {
            areaShutdown?.Shutdown();
            meshShutdown?.Shutdown();
            colorShutdown?.Shutdown();

            if (outlineMaterial != null)
                UnityEngine.Object.DestroyImmediate(outlineMaterial);
        }

        [Test]
        public void Update_WritesGridOutlineAndDisposeReturnsBothCoreHandles()
        {
            object mesh = CreateInternalSystem("MeshSystem");
            object color = CreateInternalSystem("ColorSystem");
            object area = CreateInternalSystem("AreaOutlineSystem");
            ((ISystemInitialize)mesh).Initialize();

            SetPrivateField(area, "meshSystem", (IMeshSystemService)mesh);
            SetPrivateField(area, "colorSystem", (IColorSystemService)color);
            var materialData = new MetrialData();
            SetPrivateField(materialData, "key", "_AreaOutline");
            SetPrivateField(materialData, "material", outlineMaterial);
            ((ISystemInitData<IHasMaterialQuery>)area).InitData(
                new TestMaterialQuery(new TestMaterialTable(materialData)));
            ((ISystemInitialize)area).Initialize();
            meshShutdown = (ISystemShutdown)mesh;
            colorShutdown = (ISystemShutdown)color;
            areaShutdown = (ISystemShutdown)area;

            var areaService = (IAreaOutlineSystemService)area;
            IAreaOutlineHandle handle = areaService.Create();
            MeshFilter meshFilter = GetHandleComponent<MeshFilter>(handle, "MeshFilter");
            MeshRenderer renderer = GetHandleComponent<MeshRenderer>(handle, "MeshRenderer");
            Assert.That(meshFilter.gameObject, Is.SameAs(renderer.gameObject));
            Assert.That(meshFilter.gameObject.activeSelf, Is.True);
            Assert.That(renderer.sharedMaterial, Is.SameAs(outlineMaterial));

            handle.Update(new AreaOutlineData(
                "Move Range",
                new[] { Vector2Int.zero, Vector2Int.right },
                Vector3.zero,
                Vector2.one,
                0.01f,
                0.05f,
                Color.cyan));

            Assert.That(meshFilter.sharedMesh.vertexCount, Is.EqualTo(14));
            Assert.That(meshFilter.sharedMesh.triangles, Has.Length.EqualTo(36));
            var propertyBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propertyBlock, 0);
            Assert.That(propertyBlock.GetColor(Shader.PropertyToID("_BaseColor")), Is.EqualTo(Color.cyan));

            handle.Dispose();

            Assert.That(meshFilter.sharedMesh, Is.Null);
            Assert.That(renderer.sharedMaterial, Is.Null);
            Assert.That(meshFilter.gameObject.activeSelf, Is.False);
            renderer.GetPropertyBlock(propertyBlock, 0);
            Assert.That(propertyBlock.isEmpty, Is.True);
            using (IAreaOutlineHandle reused = areaService.Create())
            {
                Assert.That(reused, Is.SameAs(handle));
            }

            using IColorHandle colorHandle = ((IColorSystemService)color).CreateColorHandle(renderer);
            Assert.That(colorHandle, Is.Not.Null);
        }

        private static T GetHandleComponent<T>(IAreaOutlineHandle handle, string propertyName)
            where T : Component
        {
            var property = handle.GetType().GetProperty(propertyName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null);
            return (T)property.GetValue(handle);
        }

        private sealed class TestMaterialQuery : IHasMaterialQuery
        {
            public IMetrialTableQuery MetrialTableQuery { get; }

            public TestMaterialQuery(IMetrialTableQuery table)
            {
                MetrialTableQuery = table;
            }
        }

        private sealed class TestMaterialTable : IMetrialTableQuery
        {
            private readonly MetrialData data;

            public TestMaterialTable(MetrialData data)
            {
                this.data = data;
            }

            public IReadOnlyList<MetrialData> GetData() => new[] { data };

            public MetrialData GetMaterial(string key)
            {
                if (key == data.Key)
                    return data;
                throw new KeyNotFoundException(key);
            }

            public bool TryGetMaterial(string key, out MetrialData materialData)
            {
                materialData = key == data.Key ? data : null;
                return materialData != null;
            }
        }

        private static object CreateInternalSystem(string name)
        {
            Type type = typeof(IMeshSystemService).Assembly.GetType(
                $"ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.{GetNamespace(name)}.{name}", true);
            return Activator.CreateInstance(type, true);
        }

        private static string GetNamespace(string name)
        {
            return name switch
            {
                "MeshSystem" => "Core.MeshSystem",
                "ColorSystem" => "Core.ColorSystem",
                "AreaOutlineSystem" => "View.AreaOutlineSystem",
                _ => throw new ArgumentOutOfRangeException(nameof(name))
            };
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }
    }
}
