using System;
using NUnit.Framework;
using ProjectL.Render.Shader.View.CustomShader.UI.Runtime;
using UnityEngine;

namespace ProjectL.Render.Shader.View.CustomShader.UI.Tests.Editor
{
    public sealed class SandstormPlaybackTests
    {
        [Test]
        public void CoverHoldsUntilRevealAndMotionKeepsAdvancing()
        {
            SandstormPlayback playback = new SandstormPlayback();
            playback.Cover(0.5f);
            playback.Advance(0.5);
            Assert.That(playback.IsCovered, Is.True);
            playback.Advance(37.0);
            Assert.That(playback.IsCovered, Is.True);
            Assert.That(playback.PresentationTime, Is.EqualTo(37.5).Within(0.00001));
            playback.Reveal(0.5f);
            playback.Advance(0.5);
            Assert.That(playback.IsHidden, Is.True);
        }

        [Test]
        public void DuplicateRequestsDoNotRestartTransition()
        {
            SandstormPlayback playback = new SandstormPlayback();
            playback.Cover(1f);
            playback.Advance(0.4);
            playback.Cover(1f);
            Assert.That(playback.Progress, Is.EqualTo(0.4f).Within(0.00001));
            playback.Advance(0.6);
            playback.Reveal(1f);
            playback.Advance(0.3);
            playback.Reveal(1f);
            Assert.That(playback.Progress, Is.EqualTo(0.3f).Within(0.00001));
        }

        [Test]
        public void ReversingIncompleteMaskFailsBeforeChangingState()
        {
            SandstormPlayback playback = new SandstormPlayback();
            playback.Cover(1f);
            Assert.Throws<InvalidOperationException>(() => playback.Reveal(1f));
            Assert.That(playback.Phase, Is.EqualTo(SandstormPhase.Covering));
            playback.ShowCovered();
            playback.Reveal(1f);
            Assert.Throws<InvalidOperationException>(() => playback.Cover(1f));
            Assert.That(playback.Phase, Is.EqualTo(SandstormPhase.Revealing));
        }

        [TestCase(-1.0)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void InvalidTimeDoesNotMutatePlayback(double value)
        {
            SandstormPlayback playback = new SandstormPlayback();
            playback.Cover(1f);
            Assert.Throws<ArgumentOutOfRangeException>(() => playback.Advance(value));
            Assert.That(playback.PresentationTime, Is.Zero);
            Assert.That(playback.Progress, Is.Zero);
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidDurationDoesNotStartCover(float value)
        {
            SandstormPlayback playback = new SandstormPlayback();
            Assert.Throws<ArgumentOutOfRangeException>(() => playback.Cover(value));
            Assert.That(playback.IsHidden, Is.True);
        }

        [Test]
        public void FastLoadingDoesNotPopJourneyToFullOpacity()
        {
            SandstormPlayback playback = new SandstormPlayback();
            playback.Cover(0.1f);
            playback.Advance(0.11);
            float before = playback.JourneyOpacity;
            playback.Reveal(0.5f);
            Assert.That(playback.JourneyOpacity, Is.EqualTo(before));
            playback.Advance(0.1);
            Assert.That(playback.JourneyOpacity, Is.LessThanOrEqualTo(before));
        }

        [Test]
        public void RevealFadesWholePresentationForEntireDuration()
        {
            SandstormPlayback playback = new SandstormPlayback();
            playback.ShowCovered();
            playback.Reveal(1f);
            Assert.That(playback.VisibilityOpacity, Is.EqualTo(1f));
            playback.Advance(0.2);
            Assert.That(playback.VisibilityOpacity, Is.EqualTo(0.896f).Within(0.001f));
            Assert.That(playback.JourneyOpacity, Is.EqualTo(1f), "마차는 부모와 함께 사라지며 먼저 꺼지지 않는다");
            playback.Advance(0.3);
            Assert.That(playback.VisibilityOpacity, Is.EqualTo(0.5f).Within(0.001f));
            playback.Advance(0.3);
            Assert.That(playback.VisibilityOpacity, Is.EqualTo(0.104f).Within(0.001f));
            Assert.That(playback.IsHidden, Is.False);
            playback.Advance(0.2);
            Assert.That(playback.VisibilityOpacity, Is.Zero);
            Assert.That(playback.IsHidden, Is.True);
        }

        [Test]
        public void ResetCanBeRepeatedAndReused()
        {
            SandstormPlayback playback = new SandstormPlayback();
            playback.ShowCovered();
            playback.Advance(10);
            playback.Reset();
            playback.Reset();
            Assert.That(playback.PresentationTime, Is.Zero);
            Assert.That(playback.JourneyOpacity, Is.Zero);
            playback.Cover(1f);
            Assert.That(playback.Phase, Is.EqualTo(SandstormPhase.Covering));
        }

        [Test]
        public void HoofPositionAndVelocityAreContinuousAtCycleAndLiftBoundaries()
        {
            JourneyMotionSettings settings = ScriptableObject.CreateInstance<JourneyMotionSettings>();
            try
            {
                const double epsilon = 0.00001;
                foreach (double boundary in new[] { settings.StrideDuration * (double)settings.StanceFraction, settings.StrideDuration })
                {
                    var a = JourneyMotionMath.SampleHoof(settings, boundary - epsilon, 0f);
                    var b = JourneyMotionMath.SampleHoof(settings, boundary, 0f);
                    var c = JourneyMotionMath.SampleHoof(settings, boundary + epsilon, 0f);
                    Assert.That(Mathf.Abs(a.Offset - c.Offset), Is.LessThan(0.01f));
                    Assert.That(Mathf.Abs(a.Lift - c.Lift), Is.LessThan(0.01f));
                    float left = (b.Offset - a.Offset) / (float)epsilon;
                    float right = (c.Offset - b.Offset) / (float)epsilon;
                    Assert.That(Mathf.Abs(left - right), Is.LessThan(3f));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }
    }
}
