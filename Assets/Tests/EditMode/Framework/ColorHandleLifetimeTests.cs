using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Framework.SystemBase;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProjectL.Tests.Framework
{
    /// <summary>
    /// ColorSystem의 현재 공개 계약만 검증한다.
    /// Handle의 수명은 ColorSystem.Shutdown이 관리하며, 소비자가 개별 해제하는 계약은 없다.
    /// </summary>
    public sealed class ColorHandleLifetimeTests
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private GameObject _root;
        private Material _material;
        private Renderer _renderer;
        private IColorSystemService _colors;
        private ISystemShutdown _shutdown;

        [SetUp]
        public void SetUp()
        {
            Type type = typeof(IColorSystemService).Assembly.GetType(
                "ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem.ColorSystem", true);
            object system = Activator.CreateInstance(type, true);
            _colors = (IColorSystemService)system;
            _shutdown = (ISystemShutdown)system;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            Assert.That(shader, Is.Not.Null, "The project's URP Lit shader is required.");
            _material = new Material(shader);

            _root = new GameObject("Color Handle Contract Test");
            _renderer = _root.AddComponent<MeshRenderer>();
            _renderer.sharedMaterials = new[] { _material, _material };
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                _shutdown?.Shutdown();
            }
            finally
            {
                if (_root != null)
                    UnityEngine.Object.DestroyImmediate(_root);
                if (_material != null)
                    UnityEngine.Object.DestroyImmediate(_material);
            }
        }

        [Test]
        public void SetOverrideData_AppliesOnlyToRequestedMaterialSlot()
        {
            IColorHandle handle = _colors.CreateColorHandle(_renderer);

            handle.SetOverrideData(0, MaterialBaseTypeKey.MainColor, Color.green);

            Assert.That(ReadSlot(_renderer, 0).GetColor(BaseColorId), Is.EqualTo(Color.green));
            Assert.That(ReadSlot(_renderer, 1).isEmpty, Is.True);
        }

        [Test]
        public void Handles_ForDifferentRenderers_KeepOverridesIndependent()
        {
            var secondObject = new GameObject("Second Renderer");
            secondObject.transform.SetParent(_root.transform, false);
            Renderer secondRenderer = secondObject.AddComponent<MeshRenderer>();
            secondRenderer.sharedMaterial = _material;

            IColorHandle first = _colors.CreateColorHandle(_renderer);
            IColorHandle second = _colors.CreateColorHandle(secondRenderer);

            first.SetOverrideData(0, MaterialBaseTypeKey.MainColor, Color.red);
            second.SetOverrideData(0, MaterialBaseTypeKey.MainColor, Color.blue);

            Assert.That(ReadSlot(_renderer, 0).GetColor(BaseColorId), Is.EqualTo(Color.red));
            Assert.That(ReadSlot(secondRenderer, 0).GetColor(BaseColorId), Is.EqualTo(Color.blue));
        }

        [TestCase(-1)]
        [TestCase(2)]
        public void SetOverrideData_WithInvalidMaterialIndex_Throws(int materialIndex)
        {
            IColorHandle handle = _colors.CreateColorHandle(_renderer);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                handle.SetOverrideData(materialIndex, MaterialBaseTypeKey.MainColor, Color.red));
        }

        [Test]
        public void CreateColorHandle_WithNullRenderer_ReturnsNull()
        {
            LogAssert.Expect(LogType.Error, new Regex("Renderer.*null"));

            Assert.That(_colors.CreateColorHandle(null), Is.Null);
        }

        [Test]
        public void Shutdown_WithActiveHandles_CanBeCalledRepeatedly()
        {
            IColorHandle handle = _colors.CreateColorHandle(_renderer);
            handle.SetOverrideData(0, MaterialBaseTypeKey.MainColor, Color.green);

            Assert.DoesNotThrow(() => _shutdown.Shutdown());
            Assert.DoesNotThrow(() => _shutdown.Shutdown());
        }

        private static MaterialPropertyBlock ReadSlot(Renderer renderer, int index)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block, index);
            return block;
        }
    }
}
