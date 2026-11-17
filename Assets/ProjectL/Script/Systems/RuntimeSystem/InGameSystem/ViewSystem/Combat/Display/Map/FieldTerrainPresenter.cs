using System;
using System.Collections.Generic;
using ProjectL.Framework.Utility;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Terrain.Enum;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Terrain.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageEnvironment.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Environment;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Entry;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map
{
    /// <summary>
    /// Terrain(타일 위 효과·벽·고지대 블록, 바닥 교체)의 외형·격자 배치·풀 수명과 칸 표면 높이 표 채우기입니다. 전달받은 사실만 표시합니다.
    /// 이웃 모양을 쓰는 종류는 같은 종류의 보이는 이웃으로 모양 프리팹과 회전을 고릅니다(지형 이웃 모양 설계 TC2~TC4).
    /// 바닥 교체 칸은 맵 타일을 끄고(<see cref="FieldTileReplacement"/>) 교체 외형을 그 타일 자리에 놓으며, 사건 시점에 빠지는 교체 외형은
    /// 무너짐 집합(<see cref="CombatTileCollapseSet"/>)에 넘겨 다 무너진 뒤 풀에 돌려줍니다(타일 교체 설계 TR3·TR4·TR8).
    /// </summary>
    internal sealed class FieldTerrainPresenter
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 이 기능의 계측 정의입니다. 일반 배포 빌드에서는 선언과 호출을 함께 제외합니다.
        private static readonly Unity.Profiling.ProfilerMarker _profilePlaybackSystemTerrainAppearancePoll = new("Combat.Action.Playback.System.TerrainAppearance.Poll");
#endif

        private sealed class Instance
        {
            internal readonly GameObject GameObject;
            internal readonly TerrainVisualData Visual;
            internal readonly Quaternion BaseRotation;
            private readonly Vector3 _prefabScale;
            private readonly Vector3 _visualScale;
            internal readonly Vector3 Offset;
            // 바닥 교체 외형인지입니다. 그러면 타일이 있는 칸에서 끈 타일 자리에 놓입니다(타일 교체 설계 TR3).
            internal readonly bool ReplacesTile;
            internal long SourceId;
            internal CellCoord Cell;

            // 기본 회전·크기는 실제로 만든 모양 프리팹에서, 보정·방식은 종류의 외형 값에서 읽습니다.
            internal Instance(GameObject gameObject, GameObject prefab, TerrainVisualData visual)
            {
                GameObject = gameObject;
                Visual = visual;
                BaseRotation = prefab.transform.localRotation;
                _prefabScale = prefab.transform.localScale;
                _visualScale = visual.LocalScale;
                Offset = visual.LocalOffset;
                ReplacesTile = visual.VisualMode == TerrainVisualMode.ReplaceTile;
            }

            internal Vector3 ScaleAt(float cellScale) => Vector3.Scale(_prefabScale, _visualScale * cellScale);
        }

        private readonly Transform _parent;
        private readonly ITerrainVisualTableQuery _visuals;
        private readonly IStageEnvironmentVisualTableQuery _stages;
        private readonly Dictionary<TerrainLookKey, CustomPool<Instance>> _pools = new();
        private readonly TerrainViewSet<Instance> _views;
        // 바닥 교체 칸의 타일을 끄고 켜는 부품입니다(타일 교체 설계 TR4). 타일 켜짐의 주인은 이것 하나입니다.
        private readonly FieldTileReplacement _tiles = new();
        private readonly Action<Instance, MovementTileFact, TerrainLook> _apply;
        private readonly Func<MovementTileFact, TerrainContentKind, TerrainLook> _lookOf;
        private readonly Action<MovementTileFact, TerrainContentKind> _markShown;
        private readonly Action<MovementTileFact, TerrainContentKind> _fillSurface;
        private readonly Action<TerrainLookKey, Instance> _leave;
        // 사건 시점에 연결에서 빠져 떨리고 가라앉는 바닥 교체 외형들입니다(타일 교체 설계 TR8). 다 무너지면 넘긴 콜백이 풀에 돌려줍니다.
        private readonly CombatTileCollapseSet _collapses;
        private Transform _root;
        // 이웃 모양을 고를 재료입니다. Render 맨 앞에서 다시 채우고 배열은 다시 씁니다.
        private FieldTerrainNeighbors _neighbors;
        // SyncSurface·Render·ShowChanged 호출 중에만 사용합니다.
        private FieldGridMapper _mapper;
        private StageEnvironmentVisualData _rotations;
        // 마지막 Render의 스테이지 키입니다. 사건 시점 갱신(ShowChanged)이 회전 표를 다시 찾을 때 씁니다(전투 중 스테이지는 바뀌지 않음, 약한 지반 계획 TP-5).
        private int _stageKey;
        private EnvironmentSemanticBindings _bindings;
        private bool _isReleased;

        /// <param name="collapse">바닥 교체 외형이 사건 시점에 빠질 때의 떨림·가라앉기 값입니다(타일 교체 설계 TR8).</param>
        internal FieldTerrainPresenter(Transform parent, ITerrainVisualTableQuery visuals,
            IStageEnvironmentVisualTableQuery stages, CombatTileCollapseSettings collapse)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            _parent = parent;
            _visuals = visuals ?? throw new ArgumentNullException(nameof(visuals));
            _stages = stages ?? throw new ArgumentNullException(nameof(stages));
            _collapses = new CombatTileCollapseSet(collapse ?? throw new ArgumentNullException(nameof(collapse)));
            _lookOf = Look;
            _views = new TerrainViewSet<Instance>(Rent, Return, CanDisplay, _lookOf);
            _apply = Apply;
            _markShown = MarkShown;
            _fillSurface = FillSurface;
            _leave = Leave;
        }

        /// <summary>
        /// 칸 ↔ 바닥 타일 표를 정합니다(타일 교체 설계 TR4). 표시 설치 때 높이 표 바탕(<see cref="FieldView.SetGroundBase"/>)과 같은 표를 넘깁니다.
        /// 타일이 없는 배경이면 null이고, 그러면 바닥 교체 외형은 칸 가운데 바닥면에 놓입니다. 다른 표로 바뀌면 옛 표에서 끈 타일을 먼저 켭니다.
        /// </summary>
        internal void BindTiles(CombatMapTileSurface tiles)
        {
            if (_isReleased) throw new InvalidOperationException("[FieldTerrainPresenter] 해제된 표시입니다.");
            _tiles.Bind(tiles);
        }

        /// <summary>
        /// 칸 표면 높이 표를 이번 사실로 다시 채우고, 격자 높이(지형이 올린 높이)가 바뀌었으면 true를 돌려줍니다(높이별로 그린 강조를 다시 그릴 신호, HG-4a).
        /// 다시 채우기는 타일 바탕(바닥 타일 윗면, 표시 설치 때 넣음)에서 시작합니다. 보이는 바닥 교체 지형은 그 칸의 바탕을 외형의 <c>SurfaceHeight</c>로 바꾸고,
        /// 보이는 타일 위 지형은 칸 배율을 적용한 <c>SurfaceHeight</c>만큼 바탕 위로 올립니다.
        /// 보이는 지형 판단은 블록 그리기(<see cref="Render"/>)와 같은 하나입니다. 유닛을 그리기 전에 불러야 합니다.
        /// </summary>
        internal bool SyncSurface(MovementEnvironmentFacts environment, EnvironmentSemanticBindings bindings, FieldGridMapper mapper)
        {
            if (_isReleased) throw new InvalidOperationException("[FieldTerrainPresenter] 해제된 표시입니다.");
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            _mapper = mapper;
            try
            {
                mapper.Surface.BeginRefill();
                _views.VisitShown(environment, bindings, _fillSurface);
                return mapper.Surface.EndRefill();
            }
            catch
            {
                // 일부만 채운 표를 남기지 않습니다. 타일 바탕(바닥 타일, 없으면 0)으로 되돌립니다.
                mapper.Surface.Reset();
                throw;
            }
            finally
            {
                _mapper = null;
            }
        }

        // 표면 높이의 뜻은 방식마다 다릅니다(타일 교체 설계 TR1). 바닥 교체는 그 칸의 새 바탕이고, 타일 위는 바탕 위로 올리는 높이입니다.
        // 타일 위 높이 0(효과·벽 등)은 바탕 그대로라 표를 바꾸지 않고, 한 칸의 여러 원본은 가장 높은 값이 남습니다(타일 높이 계획 P2).
        private void FillSurface(MovementTileFact source, TerrainContentKind kind)
        {
            TerrainVisualData visual = _visuals.GetTerrainVisual(kind);
            if (visual.VisualMode == TerrainVisualMode.ReplaceTile) _mapper.Surface.Replace(source.Cell, ReplacementHeight(source, visual));
            else _mapper.Surface.Raise(source.Cell, visual.SurfaceHeight * _mapper.TerrainCellScale);
        }

        private static float ReplacementHeight(MovementTileFact source, TerrainVisualData visual) =>
            visual.SurfaceHeight - EntryDrop(source, visual);

        // 프리팹 배치와 칸 바탕을 같은 필드 로컬 거리만큼 내립니다.
        private static float EntryDrop(MovementTileFact source, TerrainVisualData visual)
        {
            if (visual.VisualMode != TerrainVisualMode.ReplaceTile ||
                source is not StaticTerrainFact terrain || terrain.Kind != StaticTerrainKind.WeakGround)
                return 0f;
            // 마지막 진입은 별도 붕괴가 재생되므로 직전 표면에서 붕괴를 시작합니다.
            return Math.Min(terrain.EntryCount, terrain.EntryThreshold - 1) * visual.WeakGroundDropPerEntry;
        }

        internal void Render(MovementEnvironmentFacts environment, EnvironmentSemanticBindings bindings,
            int stageKey, FieldGridMapper mapper)
        {
            if (_isReleased) throw new InvalidOperationException("[FieldTerrainPresenter] 해제된 표시입니다.");
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            _mapper = mapper;
            _stageKey = stageKey;
            _bindings = bindings;
            try
            {
                _rotations = RotationsOf(stageKey);
                ValidateTerrainRotations(_rotations, bindings);
                MarkShownCells(environment, bindings, mapper);
                _tiles.Apply();
                _views.Render(environment, bindings, _apply);
            }
            catch (Exception error)
            {
                try
                {
                    Clear();
                }
                catch (Exception cleanup)
                {
                    throw new AggregateException(error, cleanup);
                }
                throw;
            }
            finally
            {
                _mapper = null;
                _rotations = null;
            }
        }

        // 이웃 모양을 고를 재료와 끌 바닥 타일 칸을 채웁니다. 보이는 지형 판단은 그리기와 같은 하나(VisitShown)입니다
        // (지형 이웃 모양 설계 TC2, 타일 교체 설계 P1 — 공개되지 않은 바닥 교체 칸의 타일은 그대로입니다).
        private void MarkShownCells(MovementEnvironmentFacts environment, EnvironmentSemanticBindings bindings, FieldGridMapper mapper)
        {
            if (_neighbors == null || _neighbors.ColumnCount != mapper.ColumnCount || _neighbors.RowCount != mapper.RowCount)
                _neighbors = new FieldTerrainNeighbors(mapper.ColumnCount, mapper.RowCount);
            else
                _neighbors.Clear();
            _tiles.BeginMarks();
            _views.VisitShown(environment, bindings, _markShown);
        }

        // 이웃 모양을 쓰는 종류만 이웃으로 표시합니다. 바닥 교체 종류는 프리팹 유무와 상관없이 그 칸의 타일을 끌 칸으로 표시합니다(타일 교체 설계 TR4).
        private void MarkShown(MovementTileFact source, TerrainContentKind kind)
        {
            TerrainVisualData visual = _visuals.GetTerrainVisual(kind);
            if (visual.UsesEdgeShapes) _neighbors.Mark(source.Cell, kind);
            if (visual.VisualMode == TerrainVisualMode.ReplaceTile) _tiles.Mark(source.Cell);
        }

        // 스테이지 외형 회전 표입니다. 없으면 null입니다. Render와 ShowChanged가 함께 씁니다.
        private StageEnvironmentVisualData RotationsOf(int stageKey) =>
            stageKey > 0 && _stages.TryGetStageEnvironmentVisual(stageKey, out var data) ? data : null;

        // 외형 선택은 한 곳(CombatTerrainLook)입니다. 스테이지 회전·이웃 표는 Render 동안 채워진 값을 씁니다.
        private TerrainLook Look(MovementTileFact source, TerrainContentKind kind) =>
            CombatTerrainLook.Resolve(kind, _visuals.GetTerrainVisual(kind), source, _rotations, _neighbors);

        /// <summary>Terrain 회전만 분류와 대조합니다. 표시하지 않는 바닥 지형의 유효한 ID도 허용합니다.</summary>
        internal static void ValidateTerrainRotations(StageEnvironmentVisualData rotations, EnvironmentSemanticBindings bindings)
        {
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            if (rotations == null) return;
            foreach (EnvironmentVisualRotation rotation in rotations.TerrainRotations)
            {
                // 분류 소속은 CombatEnvironmentFacts 한 곳입니다(평가 C1-05).
                if (!CombatEnvironmentFacts.IsTerrain(bindings, rotation.SourceId))
                    throw new InvalidOperationException($"[FieldTerrainPresenter] Terrain 회전의 Source Id가 Buffer 분류에 없습니다: {rotation.SourceId}.");
            }
        }

        /// <summary>
        /// 약한 지반 진입이면 떨림 뒤 외형과 표면을 함께 내립니다.
        /// 같은 원본의 종류가 사건 시점에 바뀌면(약한 지반 붕괴 → 낭떠러지) 그 칸 외형을 새 종류로 바꿉니다(약한 지반 설계 WG6).
        /// 지금 보이는 원본만 바꿉니다. 옛·새 종류 중 하나가 이웃 모양을 쓰거나, 방식이 다르거나(타일 끄고 켜기가 바뀜),
        /// 타일 위 종류인데 표면 높이가 다르면(그 위 외형 자리가 바뀜) 이웃 표·타일·높이 표까지 바뀌어야 하므로 여기서는 하지 않고
        /// 결과 끝 <see cref="Render"/>에 맡깁니다(약한 지반 계획 TP-3, 타일 교체 설계 TR6). 바닥 교체끼리는 타일이 이미 꺼져 있어
        /// 높이가 달라도 외형만 바로 바꾸고, 높이 표는 결과 끝 Refresh가 맞춥니다. 결과 끝 Render가 최종 사실로 다시 맞춥니다.
        /// 빠지는 바닥 교체 외형은 바로 끄지 않고 떨리다 가라앉은 뒤 돌려줍니다(<see cref="Leave"/>, 타일 교체 설계 TR8).
        /// </summary>
        internal void ShowChanged(StaticTerrainFact expected, StaticTerrainFact final, FieldGridMapper mapper, Func<CellCoord, Action> bindSurfaceOccupants)
        {
            if (expected == null) throw new ArgumentNullException(nameof(expected));
            if (final == null) throw new ArgumentNullException(nameof(final));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            if (bindSurfaceOccupants == null) throw new ArgumentNullException(nameof(bindSurfaceOccupants));
            if (_isReleased) return;
            if (expected.Kind == final.Kind)
            {
                BeginEntry(expected, final, mapper, bindSurfaceOccupants);
                return;
            }
            if (!_views.TryGet(final.Id, out Instance shown)) return;
            TerrainVisualData before = shown.Visual;
            TerrainVisualData after = _visuals.GetTerrainVisual(EnvironmentVisualKinds.TerrainKind(final));
            if (before.UsesEdgeShapes || after.UsesEdgeShapes || before.VisualMode != after.VisualMode) return;
            if (after.VisualMode == TerrainVisualMode.OnTile && before.SurfaceHeight != after.SurfaceHeight) return;
            _mapper = mapper;
            try
            {
                _rotations = RotationsOf(_stageKey);
                _views.TryReplace(final, _apply, _leave);
            }
            finally
            {
                _mapper = null;
                _rotations = null;
            }
        }

        private void BeginEntry(StaticTerrainFact before, StaticTerrainFact after, FieldGridMapper mapper, Func<CellCoord, Action> bindSurfaceOccupants)
        {
            if (_bindings == null || !CombatEnvironmentFacts.IsTerrain(_bindings, after.Id) || !CombatEnvironmentFacts.IsShown(after))
                return;
            TerrainVisualData visual = _visuals.GetTerrainVisual(EnvironmentVisualKinds.TerrainKind(after));
            if (visual.VisualMode != TerrainVisualMode.ReplaceTile) return;
            float from = ReplacementHeight(before, visual);
            float to = ReplacementHeight(after, visual);
            if (to >= from) return;
            Action alignOccupants = bindSurfaceOccupants(after.Cell);

            // 프리팹이 비어 있는 교체 칸도 그 위의 유닛은 표면을 따릅니다.
            Transform tile = _views.TryGet(after.Id, out Instance shown) ? shown.GameObject.transform : null;
            Vector3 origin = mapper.LocalToWorld(Vector3.zero);
            _collapses.BeginEntry(tile, mapper.LocalToWorld(Vector3.right) - origin, mapper.LocalToWorld(Vector3.forward) - origin,
                mapper.SurfaceOffset(to - from), progress =>
                {
                    float previous = mapper.Surface.BaseHeightAt(after.Cell);
                    mapper.Surface.Replace(after.Cell, progress >= 1f ? to : Mathf.Lerp(from, to, progress));
                    if (mapper.Surface.BaseHeightAt(after.Cell) != previous) alignOccupants?.Invoke();
                });
        }

        // 필드 지형 루트 아래 놓는 원본 표시 위치(필드 로컬)입니다.
        // 타일 위 외형은 칸의 바탕(바닥 타일 윗면, 타일이 없으면 바닥면, 바닥 교체 칸은 그 지형 높이) 위에 놓습니다. 지형이 올린 높이는 외형 자신이 그립니다(타일 높이 설계 Q-TH2 B·TH5).
        // 바닥 교체 외형은 타일이 없는 칸에서만 여기에 오고, 바꾸기 전 바닥면(칸 가운데, 높이 0)에 놓습니다(타일 교체 설계 P7). 타일이 있는 칸은 PlaceOnTile이 끈 타일 자리에 놓습니다.
        private static Vector3 LocalPosition(CellCoord cell, Instance instance, FieldGridMapper mapper, float cellScale) =>
            (instance.ReplacesTile ? mapper.CellCenterToLocal(cell) : mapper.CellBaseToLocal(cell)) + instance.Offset * cellScale;

        /// <summary>바닥 교체 무너짐이 진행 중인지입니다. 결과 끝 사실 반영이 이것을 기다립니다.</summary>
        internal bool IsAppearancePlaying() => _collapses.IsPlaying;

        /// <summary>
        /// 진입은 표면 하강이 끝날 때까지, 붕괴는 추락이 시작될 떨림 끝까지 다음 사건을 기다리게 합니다.
        /// </summary>
        internal bool IsAppearanceBlockingStart() => _collapses.BlocksStart;

        /// <summary>무너짐을 진행합니다. 다 무너진 외형은 무너짐 집합이 풀에 돌려줍니다.</summary>
        internal void TickAppearance(float deltaTime)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profilePlaybackSystemTerrainAppearancePollSample = _profilePlaybackSystemTerrainAppearancePoll.Auto();
#endif
            _collapses.Tick(deltaTime);
        }

        /// <summary>무너짐을 멈춥니다. 무너지는 중인 외형은 남은 연출 없이 바로 돌려줍니다.</summary>
        internal void CancelAppearance() => _collapses.FinishAll();

        /// <summary>표시를 모두 반환하고(무너지는 중인 외형 포함), 바닥 교체로 끈 타일을 다시 켭니다(타일 교체 설계 TR4·TR8).</summary>
        internal void Clear()
        {
            try
            {
                _collapses.FinishAll();
                _views.Clear();
            }
            finally
            {
                _tiles.Restore();
                _bindings = null;
            }
        }

        // 사건 시점에 연결에서 빠진 외형을 받습니다(타일 교체 설계 TR8). 보이는 바닥 교체 외형이면 무너짐 집합에 넘겨 떨림 → 가라앉기 뒤 돌려주고,
        // 그 밖(타일 위 외형·이미 꺼진 외형·연출 시간 0)은 지금처럼 바로 돌려줍니다. 흔들고 가라앉는 축은 지금 필드 축입니다.
        // ShowChanged가 Mapper를 세운 동안에만 불립니다(묶음 끝 검토 L9).
        private void Leave(TerrainLookKey key, Instance instance)
        {
            if (_mapper == null)
                throw new InvalidOperationException("[FieldTerrainPresenter] 외형 내보내기는 사건 시점 바꾸기(ShowChanged) 안에서만 불립니다.");
            if (instance.ReplacesTile && instance.GameObject != null && instance.GameObject.activeInHierarchy)
            {
                Vector3 origin = _mapper.LocalToWorld(Vector3.zero);
                if (_collapses.TryBegin(instance.GameObject.transform, _mapper.LocalToWorld(Vector3.right) - origin,
                        _mapper.LocalToWorld(Vector3.forward) - origin, _mapper.WorldUp, () => Return(key, instance)))
                    return;
            }
            Return(key, instance);
        }

        /// <summary>풀을 포함한 전용 루트만 제거합니다. Clear 뒤에는 풀을 재사용합니다.</summary>
        internal void Release()
        {
            if (_isReleased) return;
            _isReleased = true;
            try
            {
                Clear();
            }
            finally
            {
                _tiles.Bind(null);
                _pools.Clear();
                if (_root != null)
                {
                    _root.gameObject.SetActive(false);
                    if (Application.isPlaying)
                        Object.Destroy(_root.gameObject);
                    else
                        Object.DestroyImmediate(_root.gameObject);
                }
                _root = null;
            }
        }

        // 그릴 수 있는지는 외형의 기본 프리팹 하나가 정합니다. 타일 위(OnTile)는 외형 데이터 생성자 검사로 늘 프리팹이 있고,
        // 바닥 교체(ReplaceTile)는 프리팹이 있을 때만 그립니다(고지대 View 설계 HG2, 타일 교체 설계 TR1). 이웃 모양을 쓰면 기본 프리팹이 필수입니다.
        private bool CanDisplay(TerrainContentKind kind) => _visuals.GetTerrainVisual(kind).Prefab != null;

        /// <summary>그려진 지형과 같은 박자를 따라갈 타일을 수집합니다. 부모와 활성 소유권은 유지합니다.</summary>
        internal void CollectBoardRevealTargets(List<CombatBoardRevealTarget> targets)
        {
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            _views.VisitViews(instance =>
            {
                if (instance.GameObject == null) throw new InvalidOperationException("[FieldTerrainPresenter] 진입 대상 지형이 사라졌습니다.");
                _tiles.TryGetTile(instance.Cell, out Transform tile);
                targets.Add(new CombatBoardRevealTarget(instance.GameObject.transform, tile));
            });
        }

        /// <summary>착지 칸 아래 실제 타일과 그 위 지형을 수집합니다. 공유 타일은 한 번만 움직이며 활성 상태는 바꾸지 않습니다.</summary>
        internal void CollectEntryGroundTargets(IReadOnlyList<CellCoord> cells, List<Transform> targets)
        {
            if (cells == null) throw new ArgumentNullException(nameof(cells));
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            if (_isReleased) throw new InvalidOperationException("[FieldTerrainPresenter] 해제된 표시입니다.");
            var occupied = new HashSet<CellCoord>(cells);
            var tiles = new HashSet<Transform>();
            var seen = new HashSet<Transform>(targets);
            foreach (CellCoord cell in occupied)
            {
                if (!_tiles.TryGetTile(cell, out Transform tile)) continue;
                tiles.Add(tile);
                if (seen.Add(tile)) targets.Add(tile);
            }
            _views.VisitViews(instance =>
            {
                bool onTile = _tiles.TryGetTile(instance.Cell, out Transform tile) && tiles.Contains(tile);
                if (!occupied.Contains(instance.Cell) && !onTile) return;
                if (instance.GameObject == null)
                    throw new InvalidOperationException("[FieldTerrainPresenter] 착지 대상 지형이 사라졌습니다.");
                Transform body = instance.GameObject.transform;
                if (seen.Add(body)) targets.Add(body);
            });
        }

        private void Apply(Instance instance, MovementTileFact source, TerrainLook look)
        {
            instance.Cell = source.Cell;
            if (instance.GameObject == null)
                throw new InvalidOperationException("[FieldTerrainPresenter] 사용 중인 Terrain 외형이 파괴됐습니다.");
            Transform transform = instance.GameObject.transform;
            float drop = EntryDrop(source, instance.Visual);
            if (instance.ReplacesTile && _tiles.TryGetTile(source.Cell, out Transform tile))
                PlaceOnTile(transform, instance, tile, look, drop);
            else
                PlaceInField(transform, instance, source.Cell, look, drop);
            if (instance.SourceId != source.Id)
            {
                instance.GameObject.name = $"FieldTerrain_{source.GetType().Name}_{source.Id}";
                instance.SourceId = source.Id;
            }
            if (!instance.GameObject.activeSelf) instance.GameObject.SetActive(true);
        }

        // 필드 지형 루트 아래, 칸 자리(LocalPosition)에 놓습니다. 회전은 외형 선택 결과입니다(모양을 안 쓰는 종류는 스테이지 외형 회전, 지형 이웃 모양 설계 TC3).
        private void PlaceInField(Transform transform, Instance instance, CellCoord cell, TerrainLook look, float drop)
        {
            if (transform.parent != _root) transform.SetParent(_root, false);
            float cellScale = instance.ReplacesTile ? 1f : _mapper.TerrainCellScale;
            Vector3 position = _mapper.LocalToWorld(LocalPosition(cell, instance, _mapper, cellScale) + Vector3.down * drop);
            Vector3 scale = instance.ScaleAt(cellScale);
            Quaternion rotation = _mapper.RotationToWorld(look.Rotation) * instance.BaseRotation;
            if (transform.position != position) transform.position = position;
            if (transform.rotation != rotation) transform.rotation = rotation;
            if (transform.localScale != scale) transform.localScale = scale;
        }

        // 바닥 교체 외형을 끈 타일과 같은 부모(타일 루트) 아래 그 타일 자리에 타일처럼 놓습니다(타일 교체 설계 TR3, 계획 P11·P12).
        // 교체 프리팹은 타일 프리팹처럼 만들므로 크기 변환이 필요 없습니다. 보정 위치·크기는 타일 루트 기준입니다.
        // 회전은 필드 기준입니다. 외형 회전(이웃 모양·스테이지 회전)은 필드 격자의 사실이고, 타일 루트는 맞춤 뒤에도 필드와 90° 단위로
        // 어긋날 수 있어서 필드 회전을 타일 루트 기준으로 바꿔 넣습니다(묶음 끝 검토 M1, 남은 각이 90° 배수라 타일 격자와도 맞음).
        private void PlaceOnTile(Transform transform, Instance instance, Transform tile, TerrainLook look, float drop)
        {
            Transform parent = tile.parent;
            if (transform.parent != parent) transform.SetParent(parent, false);
            Vector3 position = tile.localPosition + instance.Offset;
            if (drop != 0f) position -= parent.InverseTransformVector(_mapper.SurfaceOffset(drop));
            Vector3 scale = instance.ScaleAt(1f);
            Quaternion rotation = Quaternion.Inverse(parent.rotation) * _mapper.RotationToWorld(look.Rotation) * instance.BaseRotation;
            if (transform.localPosition != position) transform.localPosition = position;
            if (transform.localRotation != rotation) transform.localRotation = rotation;
            if (transform.localScale != scale) transform.localScale = scale;
        }

        private Instance Rent(TerrainLookKey key)
        {
            if (!_pools.TryGetValue(key, out var pool))
            {
                pool = new CustomPool<Instance>(new PoolContext<Instance>(() => Create(key), 0));
                _pools.Add(key, pool);
            }
            Instance instance;
            do
            {
                instance = pool.Get();
            }
            while (instance.GameObject == null);
            return instance;
        }

        private void Return(TerrainLookKey key, Instance instance)
        {
            if (instance.GameObject == null) return;
            instance.GameObject.SetActive(false);
            // 바닥 교체 외형은 쓰는 동안 타일 루트 아래에 있으므로 지형 루트로 되돌립니다(타일 교체 계획 P11).
            Transform transform = instance.GameObject.transform;
            if (_root != null && transform.parent != _root) transform.SetParent(_root, false);
            _pools[key].Return(instance);
        }

        private Instance Create(TerrainLookKey key)
        {
            TerrainVisualData visual = _visuals.GetTerrainVisual(key.Kind);
            // 빈 모양 칸은 외형 선택이 Center로 바꿔 오므로, 여기서 프리팹이 없으면 계약 오류입니다.
            GameObject prefab = visual.ShapePrefab(key.Shape);
            if (prefab == null)
                throw new InvalidOperationException($"[FieldTerrainPresenter] {key.Kind}의 {key.Shape} 프리팹이 없습니다.");
            if (_root == null)
            {
                _root = new GameObject("FieldTerrains").transform;
                _root.SetParent(_parent, false);
            }
            GameObject gameObject = Object.Instantiate(prefab, _root, false);
            gameObject.SetActive(false);
            return new Instance(gameObject, prefab, visual);
        }
    }
}
