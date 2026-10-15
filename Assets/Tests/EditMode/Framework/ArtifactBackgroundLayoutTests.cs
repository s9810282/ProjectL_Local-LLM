using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Artifact.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactPopup;
using UnityEngine;

namespace ProjectL.Tests.Framework
{
    public sealed class ArtifactBackgroundLayoutTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void Background_CoversViewport_AndFollowsCameraAndLens(bool orthographic)
        {
            var cameraObject = new GameObject("Camera", typeof(Camera));
            var backgroundObject = new GameObject("Background", typeof(RectTransform));
            try
            {
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.orthographic = orthographic;
                camera.aspect = 1.7f;
                camera.transform.SetPositionAndRotation(new Vector3(4, 6, -7), Quaternion.Euler(35, 25, 0));
                RectTransform background = backgroundObject.GetComponent<RectTransform>();
                background.pivot = Vector2.zero;
                var layout = new ArtifactBackgroundLayout(camera, background, new ArtifactPopupConfigData());
                layout.Update();
                Assert.That(Vector3.Distance(camera.WorldToViewportPoint(background.position), new Vector3(.5f, .5f, 30)), Is.LessThan(.001f));
                var corners = new Vector3[4]; background.GetWorldCorners(corners);
                Assert.That(camera.WorldToViewportPoint(corners[0]).x, Is.LessThanOrEqualTo(0));
                Assert.That(camera.WorldToViewportPoint(corners[2]).x, Is.GreaterThanOrEqualTo(1));
                Assert.That(camera.WorldToViewportPoint(corners[0]).y, Is.LessThanOrEqualTo(0));
                Assert.That(camera.WorldToViewportPoint(corners[2]).y, Is.GreaterThanOrEqualTo(1));
                float oldHeight = background.rect.height;
                camera.transform.position += Vector3.right * 3;
                camera.orthographicSize *= 2; camera.fieldOfView += 10; camera.aspect = 2.4f;
                layout.Update();
                Assert.That(background.rect.height, Is.GreaterThan(oldHeight));
                Assert.That(camera.WorldToViewportPoint(background.position).x, Is.EqualTo(.5f).Within(.001f));
            }
            finally { Object.DestroyImmediate(backgroundObject); Object.DestroyImmediate(cameraObject); }
        }
    }
}
