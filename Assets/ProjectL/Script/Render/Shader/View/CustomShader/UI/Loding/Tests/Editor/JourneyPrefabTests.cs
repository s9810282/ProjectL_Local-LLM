using System;
using NUnit.Framework;
using ProjectL.Render.Shader.View.CustomShader.UI.Editor;
using ProjectL.Render.Shader.View.CustomShader.UI.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Render.Shader.View.CustomShader.UI.Tests.Editor
{
    public sealed class JourneyPrefabTests
    {
        private GameObject _parent;

        [OneTimeSetUp]
        public void BuildPackage() => JourneyPackageBuilder.BuildAssets();

        [SetUp]
        public void SetUp()
        {
            _parent = new GameObject("JourneyTestOwner", typeof(RectTransform), typeof(Canvas));
            _parent.GetComponent<RectTransform>().sizeDelta = new Vector2(1000, 563);
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_parent);

        [Test]
        public void ReadyPrefabHasAllReferencesAndNoIndependentCanvas()
        {
            SandstormTransitionVisual visual = Instantiate();
            Assert.DoesNotThrow(visual.ValidateConfiguration);
            Assert.That(visual.GetComponentsInChildren<Canvas>(true), Is.Empty);
            Assert.That(visual.IsHidden, Is.True);
            foreach (JourneyGraphic graphic in visual.GetComponentsInChildren<JourneyGraphic>(true))
            {
                Assert.That(graphic.GetComponent<CanvasRenderer>(), Is.Not.Null, graphic.name);
            }
        }

        [Test]
        public void InstancesNeverMutateSharedWindMaterial()
        {
            SandstormTransitionVisual a = Instantiate();
            SandstormTransitionVisual b = Instantiate();
            RawImage wa = a.GetComponentInChildren<RawImage>();
            RawImage wb = b.GetComponentInChildren<RawImage>();
            Material shared = wa.material;
            a.ShowCovered();
            Assert.That(wa.material, Is.Not.SameAs(shared));
            Assert.That(wb.material, Is.SameAs(shared));
            Assert.That(shared.GetFloat("_Phase"), Is.Zero);
            Assert.That(wa.material.GetFloat("_Phase"), Is.EqualTo(2f));
        }

        [Test]
        public void ExternalClockCannotAccidentallyDoubleTickAutomaticMode()
        {
            SandstormTransitionVisual visual = Instantiate();
            visual.ShowCovered();
            Assert.Throws<InvalidOperationException>(() => visual.Advance(1));
            visual.UseExternalClock = true;
            visual.Advance(1);
            Assert.That(visual.PresentationTime, Is.EqualTo(1));
        }

        [Test]
        public void DisableResetsInputBlockingAndAllowsReuse()
        {
            SandstormTransitionVisual visual = Instantiate();
            visual.ShowCovered();
            CanvasGroup group = visual.GetComponent<CanvasGroup>();
            Assert.That(group.blocksRaycasts, Is.True);
            visual.enabled = false;
            Assert.That(group.blocksRaycasts, Is.False);
            Assert.That(group.alpha, Is.Zero);
            Assert.That(visual.IsHidden, Is.True);
            visual.enabled = true;
            visual.Cover();
            Assert.That(visual.Phase, Is.EqualTo(SandstormPhase.Covering));
        }

        [Test]
        public void DestroyReleasesOwnedMaterial()
        {
            SandstormTransitionVisual visual = Instantiate();
            visual.ShowCovered();
            Material owned = visual.GetComponentInChildren<RawImage>().material;
            UnityEngine.Object.DestroyImmediate(visual.gameObject);
            Assert.That(owned == null, Is.True);
        }

        [Test]
        public void CanvasGroupAlphaAndRectMaskMaterialDoNotStopAnimationTime()
        {
            _parent.AddComponent<RectMask2D>();
            SandstormTransitionVisual visual = Instantiate();
            visual.UseExternalClock = true;
            visual.ShowCovered();
            visual.Advance(2);
            Canvas.ForceUpdateCanvases();
            RawImage wind = visual.GetComponentInChildren<RawImage>();
            Assert.That(wind.materialForRendering.GetFloat("_PresentationTime"), Is.EqualTo(2f));
        }

        private SandstormTransitionVisual Instantiate()
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(JourneyPackageBuilder.PrefabPath);
            GameObject instance = UnityEngine.Object.Instantiate(source, _parent.transform, false);
            return instance.GetComponent<SandstormTransitionVisual>();
        }
    }
}
