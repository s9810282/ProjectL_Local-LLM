using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Tool.Editor;
using UnityEngine;

namespace ProjectL.OutlineNormals.Editor.Tests
{
    public sealed class OutlineNormalPostprocessorTests
    {
        [TestCase(0f)]
        [TestCase(0.0000001f)]
        public void OpposingNormals_ProduceUnitFallbackWithoutChangingSurfaceNormals(float residual)
        {
            var normals = new[] { Vector3.right, new Vector3(-1, residual, 0) };
            var mesh = new Mesh
            {
                vertices = new[] { Vector3.zero, Vector3.zero },
                normals = normals
            };
            try
            {
                Generate(mesh);
                var outline = new List<Vector3>();
                mesh.GetUVs(3, outline);
                Assert.That(outline, Is.EqualTo(new[] { Vector3.up, Vector3.up }));
                Assert.That(mesh.normals, Is.EqualTo(normals), "표면 조명용 노멀을 덮어쓰면 안 됩니다.");
            }
            finally { Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void CoincidentHardSeams_ShareUnitOutlineWhileOtherPositionsRemainIndependent()
        {
            var normals = new[] { Vector3.right, Vector3.up, Vector3.forward, Vector3.left };
            var mesh = new Mesh
            {
                vertices = new[] { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.one },
                normals = normals
            };
            try
            {
                Generate(mesh);
                var outline = new List<Vector3>();
                mesh.GetUVs(3, outline);
                for (int i = 0; i < 3; i++)
                    Assert.That(Vector3.Distance(outline[i], Vector3.one.normalized), Is.LessThan(0.00001f));
                Assert.That(outline[3], Is.EqualTo(Vector3.left));
                Assert.That(mesh.normals, Is.EqualTo(normals));
            }
            finally { Object.DestroyImmediate(mesh); }
        }

        private static void Generate(Mesh mesh)
        {
            MethodInfo method = typeof(OutlineNormalPostprocessor).GetMethod("GenerateOutlineNormals", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);
            method.Invoke(null, new object[] { mesh });
        }
    }
}
