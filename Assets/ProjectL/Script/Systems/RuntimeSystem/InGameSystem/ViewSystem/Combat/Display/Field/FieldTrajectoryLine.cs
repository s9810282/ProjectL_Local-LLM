using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageField.Item;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.MeshSystem;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field
{
    /// <summary>
    /// 공격 궤적 점선 하나를 그립니다(공격 궤적 설계 TJ1·TJ7). 하위 Render 부품을 조합합니다:
    /// MeshSystem(꺾은선 띠, <see cref="MultiLineCreateData"/>)과 ColorSystem(색·점선 값 덮어쓰기). 선례는 <see cref="FieldCellFill"/>입니다.
    /// </summary>
    /// <remarks>
    /// 궤적 하나는 메시 하나이고, 점은 셰이더(AreaOutline)가 UV로 찍는 무늬입니다. 점마다 객체를 만들지 않습니다.
    /// 점선 규칙: 경로를 같은 길이 구간으로 다시 나누고(<see cref="FieldTrajectorySegments"/>), 셰이더는 구간 하나에 점 하나를 찍습니다.
    /// 지금 MultiLine UV는 길이 방향이 구간마다 0→1이고, <c>uvScale = 1 / 구간 길이</c>로 넘겨 UV가 고쳐져도 같은 화면이 됩니다.
    /// 다시 채우는 때는 <see cref="Show"/>뿐이며 매 프레임 하는 일은 없습니다(흐름은 셰이더 시간). 점 배열은 구간 수마다 한 번 만들어 다시 씁니다.
    /// 좌표는 부모(필드 셀 루트) 로컬입니다. 공격·유닛을 모르고 받은 경로만 그립니다.
    /// </remarks>
    internal sealed class FieldTrajectoryLine
    {
        private const string Name = "FieldTrajectory";

        // 이보다 짧은 경로는 그리지 않습니다.
        private const float MinLength = .0001f;

        private readonly Transform _parent;
        private readonly Material _material;
        private readonly float _thickness;
        private readonly float _dotSpacing;
        private readonly float _dotFill;
        private readonly float _flowSpeed;
        private readonly GameObject _markerPrefab;
        private readonly FieldHighlightPalette _palette;
        private readonly IMeshSystemService _meshes;
        private readonly IColorSystemService _colors;
        private readonly Dictionary<int, Vector3[]> _points = new();

        private GameObject _gameObject;
        private MeshRenderer _renderer;
        private GameObject _marker;
        private IMeshRenderHandle _mesh;
        private IColorHandle _colorHandle;
        // 마지막에 ColorSystem에 넣은 색과 그때의 막힘 여부입니다. 색이 다를 때만 다시 넣고(계획 TP-4),
        // 막힘 여부는 에디터 값 바뀜 때 지금 색을 다시 넣는 데 씁니다(필드 색 DB 설계 FC3).
        private Color? _shownColor;
        private bool? _lastBlocked;

        /// <param name="config">궤적 값(재질·폭·점 간격·채움·흐름 속도·막힘 표지)의 원본입니다. 재질이 있어야 합니다.</param>
        /// <param name="palette">궤적 색(가능·막힘)을 그릴 때 읽는 필드 색입니다(필드 색 DB 설계 FC2).</param>
        internal FieldTrajectoryLine(Transform parent, StageFieldViewData config, FieldHighlightPalette palette,
            IMeshSystemService meshes, IColorSystemService colors)
        {
            _parent = parent != null ? parent : throw new ArgumentNullException(nameof(parent));
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            _material = config.TrajectoryMaterial != null
                ? config.TrajectoryMaterial
                : throw new ArgumentException("[FieldTrajectoryLine] 궤적 머티리얼이 필요합니다.", nameof(config));
            _thickness = config.TrajectoryThickness;
            _dotSpacing = config.TrajectoryDotSpacing;
            _dotFill = config.TrajectoryDotFill;
            _flowSpeed = config.TrajectoryFlowSpeed;
            _markerPrefab = config.TrajectoryBlockedMarker;
            _palette = palette ?? throw new ArgumentNullException(nameof(palette));
            _meshes = meshes ?? throw new ArgumentNullException(nameof(meshes));
            _colors = colors ?? throw new ArgumentNullException(nameof(colors));
        }

        /// <summary>지금 궤적이 보이는지입니다.</summary>
        internal bool IsShown => _renderer != null && _renderer.enabled;

        /// <summary>
        /// 표시 오브젝트(꺼 둔 상태)와 막힘 표지를 만들고 Mesh·Color 핸들을 등록한 뒤 점선 고정 값을 한 번 넣습니다.
        /// 실패하면 만든 것을 거두고 원인을 전파합니다.
        /// </summary>
        internal void Build()
        {
            if (_gameObject != null)
                throw new InvalidOperationException("[FieldTrajectoryLine] 이미 구성돼 있습니다.");

            var gameObject = new GameObject(Name);
            gameObject.transform.SetParent(_parent, false);
            MeshFilter filter = gameObject.AddComponent<MeshFilter>();
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            // 모든 칸 테두리 위에 그립니다(정렬 순서 표).
            renderer.sortingOrder = FieldSortingOrders.Trajectory;
            renderer.enabled = false;
            _gameObject = gameObject;
            _renderer = renderer;
            _shownColor = null;
            _lastBlocked = null;

            try
            {
                if (_markerPrefab != null)
                {
                    _marker = UnityEngine.Object.Instantiate(_markerPrefab, gameObject.transform, false);
                    _marker.SetActive(false);
                }

                _mesh = _meshes.Register(filter);
                _colorHandle = _colors.CreateColorHandle(renderer)
                    ?? throw new InvalidOperationException("[FieldTrajectoryLine] 렌더러를 ColorSystem에 등록하지 못했습니다.");
                // 점선 규칙(구간 하나에 점 하나, 목표 쪽으로 흐름)의 셰이더 값입니다. 이후 바뀌지 않습니다.
                _colorHandle.SetOverrideData(0, ColorShaderPropertyKey.AreaOutlineDashEnabled, 1f);
                _colorHandle.SetOverrideData(0, ColorShaderPropertyKey.AreaOutlineDashCount, 1f);
                _colorHandle.SetOverrideData(0, ColorShaderPropertyKey.AreaOutlineDashFill, _dotFill);
                _colorHandle.SetOverrideData(0, ColorShaderPropertyKey.AreaOutlineFlowDirection, 1f);
                _colorHandle.SetOverrideData(0, ColorShaderPropertyKey.AreaOutlineFlowSpeed, _flowSpeed);
            }
            catch (Exception error)
            {
                try
                {
                    Release();
                }
                catch (Exception releaseError)
                {
                    throw new AggregateException("[FieldTrajectoryLine] 구성에 실패했고 정리도 실패했습니다.", error, releaseError);
                }

                throw;
            }
        }

        /// <summary>
        /// 경로(부모 로컬, 앞쪽 <paramref name="count"/>개)를 점선으로 그립니다. 막힘이면 막힘 색이고, 표지가 있으면 끝점에 둡니다.
        /// 점이 둘 미만이거나 길이가 거의 0이면 숨깁니다.
        /// </summary>
        internal void Show(Vector3[] localPath, int count, bool blocked)
        {
            if (localPath == null)
                throw new ArgumentNullException(nameof(localPath));
            if (count < 0 || count > localPath.Length)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (_mesh == null || _colorHandle == null)
                throw new InvalidOperationException("[FieldTrajectoryLine] 구성하기 전에 그릴 수 없습니다.");

            float length = count >= 2 ? FieldTrajectorySegments.Length(localPath, count) : 0f;
            if (!(length > MinLength))
            {
                Clear();
                return;
            }

            int segments = FieldTrajectorySegments.SegmentCount(length, _dotSpacing);
            Vector3[] points = PointsFor(segments);
            FieldTrajectorySegments.Resample(localPath, count, length, segments, points);
            _mesh.Update(new MultiLineCreateData(Name, points, _thickness, segments / length, Color.white));

            ApplyColor(blocked);
            _renderer.enabled = true;

            if (_marker != null)
            {
                if (blocked)
                    _marker.transform.localPosition = points[segments];
                _marker.SetActive(blocked);
            }
        }

        /// <summary>
        /// 보이는 궤적에 지금 필드 색을 다시 넣습니다(필드 색 DB 설계 FC3, 에디터 값 바뀜 때). 숨겨져 있으면 다음 <see cref="Show"/>가 넣습니다.
        /// </summary>
        internal void ReapplyColor()
        {
            if (IsShown && _lastBlocked.HasValue)
                ApplyColor(_lastBlocked.Value);
        }

        // 막힘 여부의 지금 색을 넣습니다. 지난번과 같은 색이면 ColorSystem을 부르지 않습니다.
        private void ApplyColor(bool blocked)
        {
            _lastBlocked = blocked;
            Color color = blocked ? _palette.TrajectoryBlocked : _palette.Trajectory;
            if (_shownColor == color)
                return;
            _colorHandle.SetOverrideData(0, MaterialBaseTypeKey.MainColor, color);
            _shownColor = color;
        }

        /// <summary>궤적과 표지를 숨깁니다. 구성 전이어도 안전합니다.</summary>
        internal void Clear()
        {
            if (_renderer != null)
                _renderer.enabled = false;
            if (_marker != null)
                _marker.SetActive(false);
        }

        /// <summary>
        /// Color 핸들 → Mesh 핸들 → 표시 오브젝트(표지 포함) 순서로 각각 거두고, 실패는 모아 한 번 던집니다. 여러 번 불려도 안전합니다.
        /// </summary>
        internal void Release()
        {
            IColorHandle color = _colorHandle;
            IMeshRenderHandle mesh = _mesh;
            GameObject gameObject = _gameObject;
            _colorHandle = null;
            _mesh = null;
            _gameObject = null;
            _renderer = null;
            _marker = null;
            _shownColor = null;
            _lastBlocked = null;

            List<Exception> errors = null;
            try
            {
                color?.Dispose();
            }
            catch (Exception exception)
            {
                (errors ??= new List<Exception>()).Add(exception);
            }
            try
            {
                mesh?.Dispose();
            }
            catch (Exception exception)
            {
                (errors ??= new List<Exception>()).Add(exception);
            }
            if (gameObject != null)
            {
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(gameObject);
                else
                    UnityEngine.Object.DestroyImmediate(gameObject);
            }

            if (errors != null)
                throw new AggregateException("[FieldTrajectoryLine] 해제 중 실패했습니다.", errors);
        }

        /// <summary>구간 수에 맞는 점 배열입니다. 구간 수마다 처음 쓸 때 한 번만 만듭니다(MultiLine은 배열 길이를 점 개수로 씁니다).</summary>
        private Vector3[] PointsFor(int segments)
        {
            if (!_points.TryGetValue(segments, out Vector3[] points))
            {
                points = new Vector3[segments + 1];
                _points.Add(segments, points);
            }
            return points;
        }
    }
}
