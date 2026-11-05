using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.MeshSystem;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field
{
    /// <summary>
    /// 이동 경로·넉백·토네이도가 함께 쓰는 평면 삼각형 화살촉입니다. 메시 규격·자세·색·핸들 수명을 소유하고 게임 상태는 모릅니다.
    /// 좌표와 크기는 부모 로컬 기준이며, 메시를 한 번 만든 뒤 자세만 바꿉니다.
    /// </summary>
    internal sealed class FieldArrowhead : IDisposable
    {
        // 반지름 1인 Circle의 꼭짓점 셋: 꼭지 +Z 1, 밑변 Z -0.5, 너비 √3, 윗면 법선 +Y입니다.
        private const float MeshRadius = 1f;
        private const int MeshSegments = 3;
        private const float MeshLength = 1.5f * MeshRadius;
        private const float MeshWidth = 1.7320508f * MeshRadius;

        private readonly float _length;
        private readonly float _width;
        private GameObject _gameObject;
        private MeshRenderer _renderer;
        private IMeshRenderHandle _mesh;
        private IColorHandle _color;
        private Color? _shownColor;

        internal FieldArrowhead(Transform parent, string name, Material material, float length, float width,
            IMeshSystemService meshes, IColorSystemService colors, bool shadows = false)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("[FieldArrowhead] 오브젝트 이름이 필요합니다.", nameof(name));
            if (material == null)
                throw new ArgumentNullException(nameof(material));
            if (!material.HasProperty(MaterialBaseTypeKey.MainColor.Name))
                throw new InvalidOperationException($"[FieldArrowhead] 화살촉 재질에 _BaseColor가 없습니다: {material.name}");
            if (!float.IsFinite(length) || length <= 0f)
                throw new ArgumentOutOfRangeException(nameof(length));
            if (!float.IsFinite(width) || width <= 0f)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (meshes == null)
                throw new ArgumentNullException(nameof(meshes));
            if (colors == null)
                throw new ArgumentNullException(nameof(colors));
            _length = length;
            _width = width;

            try
            {
                _gameObject = new GameObject(name);
                _gameObject.transform.SetParent(parent, false);
                MeshFilter filter = _gameObject.AddComponent<MeshFilter>();
                _renderer = _gameObject.AddComponent<MeshRenderer>();
                _renderer.sharedMaterial = material;
                _renderer.enabled = false;
                _renderer.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                _renderer.receiveShadows = shadows;
                _mesh = meshes.Register(filter)
                    ?? throw new InvalidOperationException("[FieldArrowhead] 메시를 등록하지 못했습니다.");
                _color = colors.CreateColorHandle(_renderer)
                    ?? throw new InvalidOperationException("[FieldArrowhead] 렌더러 색을 등록하지 못했습니다.");
                _mesh.Update(new CircleCreateData(name, Vector3.zero, MeshRadius, MeshSegments, Color.white));
            }
            catch (Exception error)
            {
                try
                {
                    Dispose();
                }
                catch (Exception releaseError)
                {
                    throw new AggregateException("[FieldArrowhead] 구성과 정리에 실패했습니다.", error, releaseError);
                }
                throw;
            }
        }

        // 표시 중인지입니다. 점등으로 잠시 꺼진(<see cref="SetLit"/>) 동안에도 참입니다.
        private bool _shown;

        /// <summary>표시 중인지입니다. 점등으로 잠시 그리기만 끈 동안에도 참입니다.</summary>
        internal bool IsVisible => _renderer != null && _shown;

        /// <summary>
        /// 꼭지를 <paramref name="anchor"/>에서 진행 방향으로 <paramref name="tipOffset"/>만큼 옮겨 그립니다.
        /// 색을 생략하면 별도 색 덮어쓰기를 하지 않으며, 색을 받으면 값이 바뀔 때만 ColorSystem에 넣습니다.
        /// </summary>
        internal void Show(Vector3 anchor, Vector3 direction, Color? color = null, float tipOffset = 0f)
        {
            if (_gameObject == null)
                throw new InvalidOperationException("[FieldArrowhead] 해제한 화살촉은 표시할 수 없습니다.");
            Vector3 forward = new Vector3(direction.x, 0f, direction.z).normalized;
            if (forward == Vector3.zero)
                throw new ArgumentException("[FieldArrowhead] 화살촉 방향이 비어 있습니다.", nameof(direction));

            float scaleZ = _length / MeshLength;
            Vector3 tip = anchor + (forward * tipOffset);
            _gameObject.transform.SetLocalPositionAndRotation(tip - (forward * (MeshRadius * scaleZ)),
                Quaternion.LookRotation(forward, Vector3.up));
            _gameObject.transform.localScale = new Vector3(_width / MeshWidth, 1f, scaleZ);
            if (color.HasValue)
                SetColor(color.Value);
            _shown = true;
            _renderer.enabled = true;
        }

        /// <summary>표시 중인 화살촉의 그리기만 켜고 끕니다(토네이도 화살촉 점등). 표시 여부(<see cref="IsVisible"/>)는 바꾸지 않고, 숨긴 화살촉은 켜지지 않습니다.</summary>
        internal void SetLit(bool lit)
        {
            if (_renderer != null)
                _renderer.enabled = _shown && lit;
        }

        /// <summary>자세는 그대로 두고 색만 바꿉니다(토네이도 화살촉 점등). 값이 바뀔 때만 ColorSystem에 넣습니다.</summary>
        internal void SetColor(Color color)
        {
            if (_gameObject == null)
                throw new InvalidOperationException("[FieldArrowhead] 해제한 화살촉의 색은 바꿀 수 없습니다.");
            if (_shownColor == color)
                return;
            _color.SetOverrideData(0, MaterialBaseTypeKey.MainColor, color);
            _shownColor = color;
        }

        /// <summary>표시를 숨깁니다. 반복 호출·해제 후에도 안전합니다.</summary>
        internal void Clear()
        {
            _shown = false;
            if (_renderer != null)
                _renderer.enabled = false;
        }

        /// <summary>색 핸들 → 메시 핸들 → 오브젝트 순서로 모두 해제합니다. 반복 호출에도 안전합니다.</summary>
        public void Dispose()
        {
            IColorHandle color = _color;
            IMeshRenderHandle mesh = _mesh;
            GameObject gameObject = _gameObject;
            _color = null;
            _mesh = null;
            _gameObject = null;
            _renderer = null;
            _shown = false;
            _shownColor = null;
            List<Exception> errors = null;
            try
            {
                color?.Dispose();
            }
            catch (Exception error)
            {
                (errors ??= new List<Exception>()).Add(error);
            }
            try
            {
                mesh?.Dispose();
            }
            catch (Exception error)
            {
                (errors ??= new List<Exception>()).Add(error);
            }
            if (gameObject != null)
            {
                gameObject.SetActive(false);
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(gameObject);
                else
                    UnityEngine.Object.DestroyImmediate(gameObject);
            }
            if (errors != null)
                throw new AggregateException("[FieldArrowhead] 해제 중 실패했습니다.", errors);
        }
    }
}
