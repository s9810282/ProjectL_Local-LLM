using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageField.Item;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.MeshSystem;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field
{
    /// <summary>
    /// 넉백 미리보기의 공용 삼각형 화살촉을 필요한 수만큼 만들어 다시 씁니다. 상태별 색·띄움 높이를 정하며,
    /// 메시·색 핸들 수명과 삼각형 자세는 FieldArrowhead가 소유합니다. 받은 표지만 그리며 매 프레임 하는 일은 없습니다.
    /// </summary>
    internal sealed class FieldKnockbackMarkers
    {
        private const string Name = "FieldKnockback";
        private readonly Transform _parent;
        private readonly Material _material;
        private readonly float _length;
        private readonly float _width;
        private readonly float _lift;
        private readonly Color _pushColor;
        private readonly Color _blockedColor;
        private readonly Color _immuneColor;
        private readonly Color _collisionColor;
        private readonly IMeshSystemService _meshes;
        private readonly IColorSystemService _colors;
        private readonly List<FieldArrowhead> _markers = new();
        private GameObject _root;

        internal FieldKnockbackMarkers(Transform parent, StageFieldViewData config, Color pushColor, Color blockedColor,
            Color immuneColor, Color collisionColor, IMeshSystemService meshes, IColorSystemService colors)
        {
            _parent = parent != null ? parent : throw new ArgumentNullException(nameof(parent));
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            _material = config.KnockbackArrowMaterial != null
                ? config.KnockbackArrowMaterial
                : throw new ArgumentException("[FieldKnockbackMarkers] 넉백 화살촉 재질이 필요합니다.", nameof(config));
            _length = config.KnockbackArrowLength;
            _width = config.KnockbackArrowWidth;
            _lift = config.KnockbackArrowLift;
            _pushColor = pushColor;
            _blockedColor = blockedColor;
            _immuneColor = immuneColor;
            _collisionColor = collisionColor;
            _meshes = meshes ?? throw new ArgumentNullException(nameof(meshes));
            _colors = colors ?? throw new ArgumentNullException(nameof(colors));
        }

        /// <summary>표지를 담을 부모와 첫 화살촉을 숨긴 채 구성하여 재질·크기 오류도 구성 때 확인합니다.</summary>
        internal void Build()
        {
            if (_root != null)
                throw new InvalidOperationException("[FieldKnockbackMarkers] 이미 구성돼 있습니다.");
            _root = new GameObject(Name);
            _root.transform.SetParent(_parent, false);
            try
            {
                _markers.Add(CreateMarker());
            }
            catch (Exception error)
            {
                try
                {
                    Release();
                }
                catch (Exception releaseError)
                {
                    throw new AggregateException("[FieldKnockbackMarkers] 구성과 정리에 실패했습니다.", error, releaseError);
                }
                throw;
            }
        }

        /// <summary>표지의 앞뒤 가운데를 받은 자리에 맞추고, 진행 방향·상태별 색을 넣습니다. 남는 표지는 숨깁니다.</summary>
        internal void Show(IReadOnlyList<FieldKnockbackMark> marks)
        {
            if (marks == null)
                throw new ArgumentNullException(nameof(marks));
            if (_root == null)
                throw new InvalidOperationException("[FieldKnockbackMarkers] 구성하기 전에 그릴 수 없습니다.");
            while (_markers.Count < marks.Count)
                _markers.Add(CreateMarker());
            for (int i = 0; i < marks.Count; i++)
            {
                FieldKnockbackMark mark = marks[i];
                _markers[i].Show(mark.LocalPosition + (Vector3.up * _lift), mark.LocalRotation * Vector3.forward,
                    ColorOf(mark.Kind), _length * 0.5f);
            }
            for (int i = marks.Count; i < _markers.Count; i++)
                _markers[i].Clear();
        }

        /// <summary>모든 표지를 숨깁니다. 구성 전에도 안전합니다.</summary>
        internal void Clear()
        {
            for (int i = 0; i < _markers.Count; i++)
                _markers[i].Clear();
        }

        /// <summary>표지의 모든 핸들과 부모를 거둡니다. 일부 해제가 실패해도 나머지 해제를 계속합니다.</summary>
        internal void Release()
        {
            GameObject root = _root;
            _root = null;
            List<Exception> errors = null;
            for (int i = 0; i < _markers.Count; i++)
            {
                try
                {
                    _markers[i].Dispose();
                }
                catch (Exception error)
                {
                    (errors ??= new List<Exception>()).Add(error);
                }
            }
            _markers.Clear();
            if (root != null)
            {
                root.SetActive(false);
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(root);
                else
                    UnityEngine.Object.DestroyImmediate(root);
            }
            if (errors != null)
                throw new AggregateException("[FieldKnockbackMarkers] 해제 중 실패했습니다.", errors);
        }

        private FieldArrowhead CreateMarker() => new(_root.transform, $"{Name}_{_markers.Count}", _material, _length, _width,
            _meshes, _colors);

        private Color ColorOf(FieldKnockbackMarkKind kind)
        {
            switch (kind)
            {
                case FieldKnockbackMarkKind.Push: return _pushColor;
                case FieldKnockbackMarkKind.Blocked: return _blockedColor;
                case FieldKnockbackMarkKind.Immune: return _immuneColor;
                case FieldKnockbackMarkKind.Collision: return _collisionColor;
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }
    }
}
