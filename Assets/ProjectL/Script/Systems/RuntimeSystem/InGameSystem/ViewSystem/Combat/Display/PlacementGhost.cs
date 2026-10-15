using System;
using System.Collections.Generic;
using ProjectL.Framework.Utility;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageField.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display
{
    /// <summary>
    /// Drag 중인 유닛의 고스트 모형을 띄웁니다(옛 PlacementGhostPresenter·Pool·Instance 이식, 계획 E.2).
    /// Core 상태를 만지지 않는 표시 전용입니다.
    /// </summary>
    /// <remarks>
    /// 고스트 재질·색·크기·높이는 View DB(StageFieldView)에서 읽습니다. 색은 ColorScop으로 바꿉니다(설계 §13.3).
    /// 배치 가능 여부 색(미리보기 응답 표시)은 후속 범위라 지금은 유효 색 하나만 씁니다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class PlacementGhost : MonoBehaviour
    {
        [Tooltip("고스트 인스턴스를 둘 루트입니다.")]
        [SerializeField] private Transform ghostRoot;

        private readonly Dictionary<GameObject, CustomPool<GhostInstance>> _pools = new();
        private IUnitVisualTableQuery _visuals;
        private StageFieldViewData _config;
        private IColorSystemService _colors;
        private GhostInstance _active;

        internal void Configure(IUnitVisualTableQuery visuals, StageFieldViewData config, IColorSystemService colors)
        {
            _visuals = visuals ?? throw new ArgumentNullException(nameof(visuals));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _colors = colors ?? throw new ArgumentNullException(nameof(colors));
        }

        /// <summary>해당 종류의 고스트를 띄웁니다. 같은 종류가 이미 떠 있으면 그대로 둡니다.</summary>
        internal void Show(EntityKey entityKey)
        {
            if (_visuals == null)
                throw new InvalidOperationException("[PlacementGhost] Configure가 먼저 필요합니다.");

            GameObject prefab = _visuals.GetUnitVisual(entityKey).Prefab;
            if (_active != null && _active.Prefab == prefab)
                return;

            Hide();
            _active = Rent(prefab);
            _active.SetVisible(true);
        }

        internal void MoveTo(Vector3 worldPosition)
        {
            _active?.SetPosition(worldPosition + _config.GhostWorldOffset);
        }

        /// <summary>배치 가능·불가 색입니다(DB의 유효·무효 색). Core 배치 응답으로 정합니다(설계 §21.1).</summary>
        internal void SetPlaceable(bool placeable)
        {
            _active?.SetTint(placeable ? _config.ValidGhostTint : _config.InvalidGhostTint);
        }

        /// <summary>떠 있는 고스트를 돌려줍니다. 여러 번 불려도 안전합니다.</summary>
        internal void Hide()
        {
            if (_active == null)
                return;

            GhostInstance active = _active;
            _active = null;
            if (active.IsAlive)
                _pools[active.Prefab].Return(active);
        }

        internal string FindMissingReference()
        {
            return ghostRoot == null ? $"{nameof(PlacementGhost)}.{nameof(ghostRoot)}" : null;
        }

        private GhostInstance Rent(GameObject prefab)
        {
            if (!_pools.TryGetValue(prefab, out CustomPool<GhostInstance> pool))
            {
                pool = new CustomPool<GhostInstance>(
                    new PoolContext<GhostInstance>(() => Create(prefab), 0, null, new GhostLifecycle()));
                _pools.Add(prefab, pool);
            }

            return pool.Get();
        }

        private GhostInstance Create(GameObject prefab)
        {
            GameObject instance = Instantiate(prefab, ghostRoot, false);
            instance.name = "PlacementGhost_" + prefab.name;
            instance.transform.localScale = prefab.transform.localScale * _config.GhostScale;
            var ghost = new GhostInstance(prefab, instance, _config.GhostMaterial, _colors, _config.ValidGhostTint);
            ghost.SetVisible(false);
            return ghost;
        }

        /// <summary>고스트 인스턴스 하나입니다. 생성 때 재질·색·그림자·충돌을 고스트용으로 바꿔 둡니다.</summary>
        private sealed class GhostInstance
        {
            private readonly GameObject _gameObject;
            private readonly List<IColorHandle> _scopes = new();
            private readonly List<int> _slotCounts = new();
            private Color _tint;

            internal GhostInstance(GameObject prefab, GameObject gameObject, Material ghostMaterial, IColorSystemService colors, Color tint)
            {
                Prefab = prefab;
                _gameObject = gameObject;

                Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;

                    int slotCount = renderer.sharedMaterials.Length;
                    if (ghostMaterial != null)
                    {
                        var materials = new Material[slotCount];
                        for (int m = 0; m < slotCount; m++)
                            materials[m] = ghostMaterial;
                        renderer.sharedMaterials = materials;
                    }

                    // 색은 Renderer마다 받은 Scope로만 바꾼다. Scope 반환은 ColorSystem.Shutdown이 맡는다.
                    IColorHandle scope = colors.CreateColorHandle(renderer)
                        ?? throw new InvalidOperationException("ColorSystem이 고스트 Renderer의 Scope를 만들지 못했습니다.");
                    _scopes.Add(scope);
                    _slotCounts.Add(slotCount);
                }

                // 생성 직후에는 유효 색으로 칠한다(같은 색 판정을 건너뛰도록 처음은 반드시 칠함).
                ApplyTint(tint);

                // 고스트가 집기·충돌에 걸리지 않게 한다.
                Collider[] colliders = gameObject.GetComponentsInChildren<Collider>(true);
                for (int i = 0; i < colliders.Length; i++)
                    colliders[i].enabled = false;
            }

            /// <summary>색이 바뀔 때만 칠합니다.</summary>
            internal void SetTint(Color tint)
            {
                if (_gameObject == null || _tint == tint)
                    return;

                ApplyTint(tint);
            }

            private void ApplyTint(Color tint)
            {
                _tint = tint;
                for (int i = 0; i < _scopes.Count; i++)
                {
                    for (int m = 0; m < _slotCounts[i]; m++)
                        _scopes[i].SetOverrideData(m, MaterialBaseTypeKey.MainColor, tint);
                }
            }

            internal GameObject Prefab { get; }

            /// <summary>씬과 함께 파괴되지 않았는지입니다.</summary>
            internal bool IsAlive => _gameObject != null;

            internal void SetVisible(bool visible)
            {
                if (_gameObject != null && _gameObject.activeSelf != visible)
                    _gameObject.SetActive(visible);
            }

            internal void SetPosition(Vector3 worldPosition)
            {
                if (_gameObject != null && _gameObject.transform.position != worldPosition)
                    _gameObject.transform.position = worldPosition;
            }
        }

        private sealed class GhostLifecycle : IPoolLifecycle<GhostInstance>
        {
            public void OnGet(GhostInstance item)
            {
            }

            public void OnBeforeReturn(GhostInstance item)
            {
                item.SetVisible(false);
            }

            public void OnReturn(GhostInstance item)
            {
            }
        }
    }
}
