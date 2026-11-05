using System;
using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Effect.Enum;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.EffectSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.MeshSystem;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.DrawPointLineSystem
{
    /// <summary>풀에서 대여한 선의 메시·색·끝점 이펙트를 갱신합니다.</summary>
    internal sealed class LineHandle : ILineHandle
    {
        private readonly ILineHandlePoolService _poolService;
        private readonly GameObject _gameObject;
        private IMeshRenderHandle _mesh;
        private IColorHandle _color;
        private readonly EffectScope _effect;
        private Vector3[] _points = Array.Empty<Vector3>();
        private EffectType _playingEffect = EffectType.TestFire;
        internal MeshFilter MeshFilter { get; }
        internal MeshRenderer MeshRenderer { get; }
        internal Transform EndPointAnchor { get; }
        internal bool IsValid { get; private set; }

        internal LineHandle(ILineHandlePoolService poolService, GameObject gameObject,
            MeshFilter meshFilter, MeshRenderer meshRenderer, Transform endPointAnchor, EffectScope effect)
        {
            _poolService = poolService ?? throw new ArgumentNullException(nameof(poolService));
            _gameObject = gameObject != null ? gameObject : throw new ArgumentNullException(nameof(gameObject));
            MeshFilter = meshFilter != null ? meshFilter : throw new ArgumentNullException(nameof(meshFilter));
            MeshRenderer = meshRenderer != null ? meshRenderer : throw new ArgumentNullException(nameof(meshRenderer));
            EndPointAnchor = endPointAnchor != null ? endPointAnchor : throw new ArgumentNullException(nameof(endPointAnchor));
            _effect = effect ?? throw new ArgumentNullException(nameof(effect));
        }

        internal void Validate() => IsValid = true;

        internal void Bind(IMeshRenderHandle mesh, IColorHandle color)
        {
            if (!IsValid || _mesh != null || _color != null)
                throw new InvalidOperationException("LineHandle must be rented and unbound before binding.");
            if (mesh == null)
                throw new ArgumentNullException(nameof(mesh));
            if (color == null)
                throw new ArgumentNullException(nameof(color));
            _mesh = mesh;
            _color = color;
            _gameObject.SetActive(true);
        }

        public void Update(DrawPointLineData data)
        {
            ThrowIfDisposed();
            Validate(data);
            int count = data.Cells.Length;
            if (_points.Length != count)
                _points = new Vector3[count];

            for (int index = 0; index < count; index++)
            {
                Vector2Int cell = data.Cells[index];
                _points[index] = data.GridOrigin + new Vector3(
                    cell.x * data.CellSize.x, data.YOffset, cell.y * data.CellSize.y);
            }

            _mesh.Update(new MultiLineCreateData(
                data.Name, _points, data.Thickness, data.UvScale, Color.white));
            _color.SetOverrideData(0, MaterialBaseTypeKey.MainColor, data.Color);
            MeshRenderer.enabled = count >= 2;

            EffectType nextEffect = count == 0 ? EffectType.Empty : data.EndPointEffect;
            if (count > 0)
                EndPointAnchor.localPosition = _points[count - 1];

            if (_playingEffect == nextEffect)
                return;

            _effect.StopAll();
            _playingEffect = EffectType.Empty;
            if (nextEffect != EffectType.Empty)
            {
                try
                {
                    _effect.Play(new EffectScopeRequest(
                        nextEffect, EndPointAnchor, Vector3.zero, Quaternion.identity));
                    _playingEffect = nextEffect;
                }
                catch
                {
                    _effect.StopAll();
                    throw;
                }
            }
        }

        public void SetOverrideData<T>(TypeSettingKey<T> key, T value) where T : notnull
        {
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(key.Name))
                throw new ArgumentException("A shader property key is required.", nameof(key));
            _color.SetOverrideData(0, key, value);
        }

        public void Dispose()
        {
            if (!IsValid)
                return;
            IsValid = false;
            _poolService.Return(this);
        }

        internal void Reset()
        {
            IsValid = false;
            _playingEffect = EffectType.Empty;
            IMeshRenderHandle mesh = _mesh;
            IColorHandle color = _color;
            _mesh = null;
            _color = null;
            // 어느 하위 반환이 실패해도 나머지 자원과 표시 상태를 정리합니다.
            try { _effect.StopAll(); }
            finally
            {
                try { color?.Dispose(); }
                finally
                {
                    try { mesh?.Dispose(); }
                    finally
                    {
                        MeshRenderer.SetPropertyBlock(null, 0);
                        MeshRenderer.enabled = false;
                        EndPointAnchor.localPosition = Vector3.zero;
                        _gameObject.SetActive(false);
                    }
                }
            }
        }

        internal void DestroyGameObject()
        {
            if (_gameObject == null)
                return;
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(_gameObject);
            else
                UnityEngine.Object.DestroyImmediate(_gameObject);
        }

        private void ThrowIfDisposed()
        {
            if (!IsValid || _mesh == null || _color == null)
                throw new ObjectDisposedException(nameof(LineHandle));
        }

        private static void Validate(DrawPointLineData data)
        {
            if (data.Cells == null)
                throw new ArgumentNullException(nameof(data.Cells));
            if (!IsFinite(data.GridOrigin.x) || !IsFinite(data.GridOrigin.y) || !IsFinite(data.GridOrigin.z) ||
                !IsFinite(data.YOffset) || !IsFinite(data.CellSize.x) || !IsFinite(data.CellSize.y) ||
                data.CellSize.x <= 0f || data.CellSize.y <= 0f ||
                !IsFinite(data.Thickness) || data.Thickness <= 0f ||
                !IsFinite(data.UvScale) || data.UvScale < 0f)
                throw new ArgumentException("Line geometry requires finite coordinates and positive cell size/thickness.", nameof(data));
            if (!System.Enum.IsDefined(typeof(EffectType), data.EndPointEffect))
                throw new ArgumentException("Unknown endpoint effect type.", nameof(data));
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
