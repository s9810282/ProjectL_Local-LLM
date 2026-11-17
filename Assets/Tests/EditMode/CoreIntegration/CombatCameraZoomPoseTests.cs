using System;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras;
using UnityEngine;

namespace ProjectL.Tests.CoreIntegration
{
    /// <summary>줌 자세 수식의 끝점·팬·최단 회전·잘못된 값을 검증합니다. Unity 실행은 사용자 담당입니다.</summary>
    public sealed class CombatCameraZoomPoseTests
    {
        private static CombatCameraZoomPosePath Path()
        {
            return new CombatCameraZoomPosePath(
                new Pose(new Vector3(0f, 10f, -10f), Quaternion.identity),
                new Pose(new Vector3(4f, 6f, -2f), Quaternion.Euler(30f, 40f, 0f)), 4f, 8f);
        }

        [TestCase(8f, 0f)]
        [TestCase(6f, 0.5f)]
        [TestCase(4f, 1f)]
        [TestCase(12f, 0f)]
        [TestCase(2f, 1f)]
        public void ActualSizeDeterminesClampedProgress(float size, float expected)
        {
            Assert.That(Path().Progress(size), Is.EqualTo(expected).Within(0.00001f));
        }

        [TestCase(8f, 0f, 10f, -10f)]
        [TestCase(6f, 2f, 8f, -6f)]
        [TestCase(4f, 4f, 6f, -2f)]
        public void PositionUsesStraightPath(float size, float x, float y, float z)
        {
            Assert.That(Vector3.Distance(Path().Evaluate(size, Vector2.zero).position,
                new Vector3(x, y, z)), Is.LessThan(0.00001f));
        }

        [Test]
        public void RotationUsesShortestPathAcrossEulerWrap()
        {
            var path = new CombatCameraZoomPosePath(
                new Pose(Vector3.zero, Quaternion.Euler(0f, 350f, 0f)),
                new Pose(Vector3.zero, Quaternion.Euler(0f, 10f, 0f)), 4f, 8f);
            Assert.That(Quaternion.Angle(path.Evaluate(6f, Vector2.zero).rotation, Quaternion.identity),
                Is.LessThan(0.001f));
        }

        [Test]
        public void PanUsesZoomOutAxesAtEveryZoom()
        {
            Quaternion rotation = Quaternion.Euler(0f, 0f, 90f);
            var path = new CombatCameraZoomPosePath(new Pose(Vector3.zero, rotation),
                new Pose(new Vector3(5f, 0f, 0f), Quaternion.identity), 4f, 8f);
            Vector3 pan = rotation * new Vector3(2f, 3f, 0f);
            foreach (float size in new[] { 8f, 6f, 4f })
            {
                Vector3 difference = path.Evaluate(size, new Vector2(2f, 3f)).position
                    - path.Evaluate(size, Vector2.zero).position;
                Assert.That(Vector3.Distance(difference, pan), Is.LessThan(0.00001f));
            }
        }

        [Test]
        public void RepeatedZoomOutReturnsExactlyWithoutAccumulation()
        {
            CombatCameraZoomPosePath path = Path();
            Pose initial = path.Evaluate(8f, Vector2.zero);
            for (int i = 0; i < 100; i++)
            {
                path.Evaluate(4f, Vector2.one);
                path.Evaluate(6f, Vector2.zero);
            }
            Pose restored = path.Evaluate(8f, Vector2.zero);
            Assert.That(restored.position, Is.EqualTo(initial.position));
            Assert.That(Quaternion.Angle(restored.rotation, initial.rotation), Is.LessThan(0.001f));
        }

        [TestCase(4f, 4f)]
        [TestCase(8f, 4f)]
        [TestCase(0f, 8f)]
        [TestCase(float.NaN, 8f)]
        [TestCase(4f, float.PositiveInfinity)]
        public void InvalidSizeRangeIsRejected(float min, float max)
        {
            Assert.Throws<ArgumentException>(() => new CombatCameraZoomPosePath(
                new Pose(Vector3.zero, Quaternion.identity), new Pose(Vector3.zero, Quaternion.identity), min, max));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidActualSizeIsRejected(float size)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Path().Progress(size));
        }

        [Test]
        public void InvalidEndpointOrPanIsRejected()
        {
            Pose valid = new Pose(Vector3.zero, Quaternion.identity);
            Assert.Throws<ArgumentException>(() => new CombatCameraZoomPosePath(valid,
                new Pose(new Vector3(float.NaN, 0f, 0f), Quaternion.identity), 4f, 8f));
            Assert.Throws<ArgumentException>(() => new CombatCameraZoomPosePath(valid,
                new Pose(Vector3.zero, new Quaternion(0f, 0f, 0f, 0f)), 4f, 8f));
            Assert.Throws<ArgumentException>(() => Path().Evaluate(6f, new Vector2(float.NaN, 0f)));
        }
        [Test]
        public void BothOffsetsUseOriginalCameraAxesAndRotation()
        {
            var reference = new Pose(new Vector3(10f, 20f, 30f), Quaternion.Euler(0f, 0f, 90f));
            var path = CombatCameraZoomPosePath.FromOffsets(reference,
                new Vector3(1f, 0f, 0f), new Vector3(0f, 0f, 10f),
                new Vector3(0f, 2f, 0f), new Vector3(0f, 0f, 30f), 4f, 8f);
            Pose zoomOut = path.Evaluate(8f, Vector2.zero);
            Pose zoomIn = path.Evaluate(4f, Vector2.zero);
            Assert.That(Vector3.Distance(zoomOut.position, new Vector3(10f, 21f, 30f)), Is.LessThan(0.00001f));
            Assert.That(Vector3.Distance(zoomIn.position, new Vector3(8f, 20f, 30f)), Is.LessThan(0.00001f));
            Assert.That(Quaternion.Angle(zoomOut.rotation, Quaternion.Euler(0f, 0f, 100f)), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(zoomIn.rotation, Quaternion.Euler(0f, 0f, 120f)), Is.LessThan(0.001f));
        }

        [Test]
        public void ZeroOffsetsPreserveOriginalPoseAtBothEnds()
        {
            var reference = new Pose(new Vector3(12f, -5f, 3f), Quaternion.Euler(25f, 40f, 5f));
            var path = CombatCameraZoomPosePath.FromOffsets(reference,
                Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, 4f, 8f);
            foreach (float size in new[] { 8f, 6f, 4f })
            {
                Pose pose = path.Evaluate(size, Vector2.zero);
                Assert.That(Vector3.Distance(pose.position, reference.position), Is.LessThan(0.00001f));
                Assert.That(Quaternion.Angle(pose.rotation, reference.rotation), Is.LessThan(0.01f));
            }
        }

        [Test]
        public void SharedSettingsKeepEachCamerasOriginalComposition()
        {
            var day = CombatCameraZoomPosePath.FromOffsets(new Pose(Vector3.zero, Quaternion.identity),
                Vector3.zero, Vector3.zero, Vector3.right, Vector3.zero, 4f, 8f);
            var night = CombatCameraZoomPosePath.FromOffsets(new Pose(new Vector3(10f, 0f, 0f), Quaternion.Euler(0f, 0f, 90f)),
                Vector3.zero, Vector3.zero, Vector3.right, Vector3.zero, 4f, 8f);
            Assert.That(Vector3.Distance(day.Evaluate(4f, Vector2.zero).position, Vector3.right), Is.LessThan(0.00001f));
            Assert.That(Vector3.Distance(night.Evaluate(4f, Vector2.zero).position, new Vector3(10f, 1f, 0f)), Is.LessThan(0.00001f));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EqualSizeRangeRemainsValidOnlyForLegacyMode(bool usePose)
        {
            if (usePose)
                Assert.Throws<ArgumentException>(() => Settings(true, 4f, 4f));
            else
                Assert.That(Settings(false, 4f, 4f).UseZoomPose, Is.False);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void EveryPoseOffsetRejectsNonFiniteValues(int index)
        {
            var values = new Vector3[4];
            values[index] = new Vector3(0f, float.NaN, 0f);
            Assert.Throws<ArgumentException>(() => new CombatCameraSettingsData(
                10f, 10f, 0.02f, -Vector2.one, Vector2.one, 4f, 8f, 1f, 0.15f,
                true, values[0], values[1], values[2], values[3]));
        }

        [Test]
        public void SettingsAssetExportsNewImmutablePoseSnapshot()
        {
            CombatCameraSettingsSO asset = ScriptableObject.CreateInstance<CombatCameraSettingsSO>();
            try
            {
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                typeof(CombatCameraSettingsSO).GetField("useZoomPose", flags).SetValue(asset, true);
                typeof(CombatCameraSettingsSO).GetField("zoomInPositionOffset", flags).SetValue(asset, new Vector3(1f, 2f, 3f));
                CombatCameraSettingsData first = asset.CreateData();
                typeof(CombatCameraSettingsSO).GetField("zoomInPositionOffset", flags).SetValue(asset, Vector3.zero);
                CombatCameraSettingsData second = asset.CreateData();
                Assert.That(first.UseZoomPose, Is.True);
                Assert.That(first.ZoomInPositionOffset, Is.EqualTo(new Vector3(1f, 2f, 3f)));
                Assert.That(second.ZoomInPositionOffset, Is.EqualTo(Vector3.zero));
                Assert.That(first, Is.Not.SameAs(second));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TransposerWithoutFollowIsAllowedButActiveFollowIsRejected(bool hasFollow)
        {
            // 테스트 어셈블리의 기존 참조를 유지하면서 설치된 Cinemachine 공개 API로 실제 구성을 만듭니다.
            Type adapterType = typeof(CombatCameraZoomPosePath).Assembly.GetType(
                "ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras.CombatCameraZoomPose", true);
            var cinemachine = adapterType.BaseType.Assembly;
            Type cameraType = cinemachine.GetType("Cinemachine.CinemachineVirtualCamera", true);
            Type transposerType = cinemachine.GetType("Cinemachine.CinemachineTransposer", true);
            var root = new GameObject("Camera configuration regression");
            var target = new GameObject("Follow target");
            try
            {
                Component camera = root.AddComponent(cameraType);
                var lensField = cameraType.GetField("m_Lens");
                object lens = lensField.GetValue(camera);
                var mode = lens.GetType().GetField("ModeOverride");
                mode.SetValue(lens, System.Enum.Parse(mode.FieldType, "Orthographic"));
                lensField.SetValue(camera, lens);
                object transposer = cameraType.GetMethod("AddCinemachineComponent")
                    .MakeGenericMethod(transposerType).Invoke(camera, null);
                if (hasFollow)
                    cameraType.GetProperty("Follow").SetValue(camera, target.transform);
                Assert.That(transposerType.GetProperty("IsValid").GetValue(transposer), Is.EqualTo(hasFollow));
                var validate = adapterType.GetMethod("ValidateCamera",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (!hasFollow)
                {
                    Assert.DoesNotThrow(() => validate.Invoke(null, new object[] { camera }));
                }
                else
                {
                    var error = Assert.Throws<System.Reflection.TargetInvocationException>(
                        () => validate.Invoke(null, new object[] { camera }));
                    Assert.That(error.InnerException, Is.TypeOf<InvalidOperationException>());
                    Assert.That(error.InnerException.Message, Does.Contain("Follow"));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
        [TestCase("Cinemachine.PostFX.CinemachineVolumeSettings", true)]
        public void PostProcessingExtensionIsAllowedButPoseWriterIsRejected(string extensionName, bool allowed)
        {
            Type adapterType = typeof(CombatCameraZoomPosePath).Assembly.GetType(
                "ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras.CombatCameraZoomPose", true);
            var cinemachine = adapterType.BaseType.Assembly;
            Type cameraType = cinemachine.GetType("Cinemachine.CinemachineVirtualCamera", true);
            Type extensionType = cinemachine.GetType(extensionName, true);
            var root = new GameObject("Camera extension regression");
            try
            {
                Component camera = root.AddComponent(cameraType);
                var lensField = cameraType.GetField("m_Lens");
                object lens = lensField.GetValue(camera);
                var mode = lens.GetType().GetField("ModeOverride");
                mode.SetValue(lens, System.Enum.Parse(mode.FieldType, "Orthographic"));
                lensField.SetValue(camera, lens);
                root.AddComponent(extensionType);
                var validate = adapterType.GetMethod("ValidateCamera",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (allowed)
                {
                    Assert.DoesNotThrow(() => validate.Invoke(null, new object[] { camera }));
                }
                else
                {
                    var error = Assert.Throws<System.Reflection.TargetInvocationException>(
                        () => validate.Invoke(null, new object[] { camera }));
                    Assert.That(error.InnerException, Is.TypeOf<InvalidOperationException>());
                    Assert.That(error.InnerException.Message, Does.Contain(extensionType.Name));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
        private static CombatCameraSettingsData Settings(bool usePose, float min, float max) =>
            new CombatCameraSettingsData(10f, 10f, 0.02f, -Vector2.one, Vector2.one, min, max, 1f, 0.15f, usePose);
    }
}
