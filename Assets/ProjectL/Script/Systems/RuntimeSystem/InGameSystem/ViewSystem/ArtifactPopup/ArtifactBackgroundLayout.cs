using ProjectL.Systems.DataBaseSystem.Data.View.Table.Artifact.Item;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactPopup
{
    /// <summary>보관한 출력 카메라의 시야가 바뀔 때만 배경 위치·크기를 맞춥니다.</summary>
    internal sealed class ArtifactBackgroundLayout
    {
        private readonly Camera _camera;
        private readonly RectTransform _background;
        private readonly ArtifactPopupConfigData _config;
        private Matrix4x4 _view, _projection;
        private bool _valid;

        internal ArtifactBackgroundLayout(Camera camera, RectTransform background, ArtifactPopupConfigData config)
        {
            _camera = camera;
            _background = background;
            _config = config;
        }

        internal void Update(bool force = false)
        {
            if (_camera == null || _background == null) return;
            Matrix4x4 view = _camera.worldToCameraMatrix, projection = _camera.projectionMatrix;
            if (!force && _valid && view == _view && projection == _projection) return;
            _view = view; _projection = projection; _valid = true;
            float distance = Mathf.Clamp(_config.BackgroundDistance, _camera.nearClipPlane + .01f, _camera.farClipPlane - .01f);
            // ViewportPointToRay를 사용해 직교·원근과 비대칭 투영의 화면 모서리를 함께 처리합니다.
            var plane = new Plane(_camera.transform.forward, _camera.transform.position + _camera.transform.forward * distance);
            Vector3 a = Corner(0, 0, plane), b = Corner(1, 0, plane), c = Corner(0, 1, plane), d = Corner(1, 1, plane);
            _background.pivot = new Vector2(.5f, .5f);
            _background.SetPositionAndRotation((a + b + c + d) * .25f, _camera.transform.rotation);
            _background.localScale = Vector3.one;
            _background.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Vector3.Distance(a, b) * _config.BackgroundMargin);
            _background.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Vector3.Distance(a, c) * _config.BackgroundMargin);
        }

        private Vector3 Corner(float x, float y, Plane plane)
        {
            Ray ray = _camera.ViewportPointToRay(new Vector3(x, y, 0));
            return plane.Raycast(ray, out float distance) ? ray.GetPoint(distance) : ray.origin;
        }
    }
}
