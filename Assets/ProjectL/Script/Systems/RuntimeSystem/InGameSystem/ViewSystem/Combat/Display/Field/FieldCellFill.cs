using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.MeshSystem;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field
{
    /// <summary>
    /// GridOutline 모드에서 칸 묶음 하나를 한 색으로 반투명하게 채웁니다. 기존 Render 부품을 조합합니다:
    /// MeshSystem(칸마다 얇은 판, <see cref="GridCubeCreateData"/>)과 ColorSystem(색 덮어쓰기).
    /// </summary>
    /// <remarks>
    /// 머티리얼은 칸 채우기 셰이더(ProjectL/Combat/CellFill)이고 색은 <c>_BaseColor</c> 하나만 렌더러마다 넣습니다.
    /// 판 모양 값(격자·칸 지도·안쪽 거리·모서리·구멍)은 <see cref="FieldCellFillStack"/>이 판마다 재질 사본에 넣어 이 객체에 줍니다.
    /// 셰이더가 칸을 오브젝트 좌표로 찾으므로 표시 오브젝트는 부모(칸 루트) 바로 아래 단위 변환으로 둡니다.
    /// 표시 오브젝트는 <see cref="Build"/> 때 부모(칸 루트) 아래에 만들고 <see cref="Release"/> 때 지웁니다.
    /// 정렬 순서(<see cref="FieldSortingOrders.Fill"/>)로 같은 Transparent 큐의 테두리보다 먼저, 아래 깊이 채우기보다 나중에 그립니다.
    /// 어떤 칸을 어떤 색·크기로 채울지와 좌표 계산은 <see cref="FieldCellFillStack"/>이 정하고, 이 객체는 받은 판을 그리기만 합니다.
    /// </remarks>
    internal sealed class FieldCellFill
    {
        // 판으로 보일 만큼만 얇게 둡니다.
        private const float Thickness = 0.001f;

        private readonly Transform _parent;
        private readonly string _name;
        private readonly Material _material;
        private readonly IMeshSystemService _meshes;
        private readonly IColorSystemService _colors;
        private readonly int _sortingOrder;

        private GameObject _gameObject;
        private IMeshRenderHandle _mesh;
        private IColorHandle _color;
        // 판 위치 배열입니다. 칸 수가 같으면 다시 씁니다(GC 끊김 설계 GH2). GlobalSystem은 받은 배열로 그 자리에서 메시를 만들고 보관하지 않습니다.
        private Vector3[] _positions = Array.Empty<Vector3>();

        /// <param name="name">표시 오브젝트와 메시 이름입니다(예: FieldFill_MoveRange).</param>
        /// <param name="sortingOrder">렌더러 정렬 순서입니다(<see cref="FieldSortingOrders.Fill"/>).</param>
        internal FieldCellFill(Transform parent, string name, Material material, IMeshSystemService meshes,
            IColorSystemService colors, int sortingOrder)
        {
            _parent = parent != null ? parent : throw new ArgumentNullException(nameof(parent));
            _name = !string.IsNullOrEmpty(name) ? name : throw new ArgumentNullException(nameof(name));
            _material = material != null ? material : throw new ArgumentNullException(nameof(material));
            _meshes = meshes ?? throw new ArgumentNullException(nameof(meshes));
            _colors = colors ?? throw new ArgumentNullException(nameof(colors));
            _sortingOrder = sortingOrder;
        }

        /// <summary>표시 오브젝트를 만들고 Mesh·Color 핸들을 등록합니다. 실패하면 만든 것을 거두고 원인을 전파합니다.</summary>
        internal void Build()
        {
            if (_gameObject != null)
                throw new InvalidOperationException($"[FieldCellFill] {_name}은(는) 이미 구성돼 있습니다.");

            var gameObject = new GameObject(_name);
            gameObject.transform.SetParent(_parent, false);
            MeshFilter filter = gameObject.AddComponent<MeshFilter>();
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            // 모든 칸 테두리보다 먼저, 아래 깊이 채우기보다 나중에 그립니다(정렬 순서 표).
            renderer.sortingOrder = _sortingOrder;
            _gameObject = gameObject;

            try
            {
                _mesh = _meshes.Register(filter);
                _color = _colors.CreateColorHandle(renderer)
                    ?? throw new InvalidOperationException($"[FieldCellFill] {_name}의 렌더러를 ColorSystem에 등록하지 못했습니다.");
            }
            catch (Exception error)
            {
                // 만든 것까지 거둔다. 거두다 실패하면 원래 원인과 함께 보고한다.
                try
                {
                    Release();
                }
                catch (Exception releaseError)
                {
                    throw new AggregateException($"[FieldCellFill] {_name} 구성에 실패했고 정리도 실패했습니다.", error, releaseError);
                }

                throw;
            }
        }

        /// <summary>판 중심(필드 루트 기준 로컬 좌표)마다 <paramref name="cellSize"/> 크기의 판을 그립니다. 목록이 비면 아무것도 보이지 않습니다.</summary>
        internal void Update(IReadOnlyList<Vector3> localCenters, Vector2 cellSize, Color color)
        {
            if (localCenters == null)
                throw new ArgumentNullException(nameof(localCenters));
            if (_mesh == null || _color == null)
                throw new InvalidOperationException($"[FieldCellFill] {_name}을(를) 구성하기 전에 그릴 수 없습니다.");

            if (_positions.Length != localCenters.Count)
                _positions = new Vector3[localCenters.Count];
            for (int i = 0; i < _positions.Length; i++)
                _positions[i] = localCenters[i];

            _mesh.Update(new GridCubeCreateData(_name, _positions, new Vector3(cellSize.x, Thickness, cellSize.y),
                Vector3.zero, color));
            _color.SetOverrideData(0, MaterialBaseTypeKey.MainColor, color);
        }

        /// <summary>
        /// Color 핸들 → Mesh 핸들 → 표시 오브젝트 순서로 각각 거두고, 실패는 모아 한 번 던집니다. 여러 번 불려도 안전합니다.
        /// 참조를 먼저 비우므로 같은 핸들을 두 번 돌려주지 않습니다.
        /// </summary>
        internal void Release()
        {
            IColorHandle color = _color;
            IMeshRenderHandle mesh = _mesh;
            GameObject gameObject = _gameObject;
            _color = null;
            _mesh = null;
            _gameObject = null;

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
                throw new AggregateException($"[FieldCellFill] {_name} 해제 중 실패했습니다.", errors);
        }
    }
}
