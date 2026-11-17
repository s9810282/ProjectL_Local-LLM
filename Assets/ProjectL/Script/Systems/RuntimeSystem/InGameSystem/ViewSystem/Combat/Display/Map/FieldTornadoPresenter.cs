using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Terrain.Enum;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Terrain.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.MeshSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Environment;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map
{
    /// <summary>
    /// 움직이는 토네이도의 몸과 방향 화살표입니다(토네이도 View 설계 TN3). 원본마다 인스턴스 하나이고, 플레이어에게 허용된 영역(<see cref="TornadoAreaFact"/>)으로만 만들어
    /// 숨김 판단이 한 곳입니다. 인스턴스의 자리는 사건 재생이 보이는 값이고 게임 상태가 아닙니다. 결과 끝 <see cref="Render"/>가 Buffer 값으로 다시 맞춥니다.
    /// </summary>
    /// <remarks>
    /// 전투 하나의 원본은 몇 개뿐이고 전투 동안 사라지지 않아 풀을 쓰지 않습니다. 인스턴스는 <see cref="Clear"/>에서 없앱니다.
    /// 3×3 범위는 칸마다 따로 그리지 않습니다(결정 Q-TN1). 프리팹 이펙트가 보여 주고 최종 외형은 프리팹으로 옵니다.
    /// </remarks>
    internal sealed class FieldTornadoPresenter : ICombatTornadoField
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 이 기능의 계측 정의입니다. 일반 배포 빌드에서는 선언과 호출을 함께 제외합니다.
        private static readonly Unity.Profiling.ProfilerMarker _profilePlaybackSystemTornadoAppearancePoll = new("Combat.Action.Playback.System.TornadoAppearance.Poll");
#endif

        private sealed class Instance
        {
            internal readonly GameObject Body;
            internal readonly CombatTornadoAppearance Appearance;
            internal readonly FieldArrowhead Arrow;
            internal readonly Quaternion BodyRotation;
            internal readonly float ArrowTipOffset;
            internal readonly Vector3 Offset;
            internal readonly int SideLength;
            // 화살촉 재질 색입니다. 점등 밝기를 여기에 곱합니다.
            internal readonly Color ArrowColor;
            // 점등의 원위치(필드 로컬)와 진행 방향입니다. 방향을 다시 맞출 때마다 바뀝니다.
            internal Vector3 ArrowAnchor;
            internal Vector3 ArrowForward;
            // 화살촉이 켜진 뒤 지난 초입니다. 숨었다 다시 켜지면 0부터 셉니다.
            internal float PulseElapsed;

            internal Instance(GameObject body, CombatTornadoAppearance appearance, FieldArrowhead arrow, Quaternion bodyRotation,
                float arrowTipOffset, Vector3 offset, int sideLength, Color arrowColor)
            {
                Body = body;
                Appearance = appearance;
                Arrow = arrow;
                BodyRotation = bodyRotation;
                ArrowTipOffset = arrowTipOffset;
                Offset = offset;
                SideLength = sideLength;
                ArrowColor = arrowColor;
            }
        }

        private readonly Transform _parent;
        private readonly ITerrainVisualTableQuery _visuals;
        private readonly IMeshSystemService _meshes;
        private readonly IColorSystemService _colors;
        private readonly Dictionary<long, Instance> _active = new();
        private readonly HashSet<long> _wanted = new();
        private readonly List<long> _removal = new();
        private Transform _root;
        private bool _isReleased;

        internal FieldTornadoPresenter(Transform parent, ITerrainVisualTableQuery visuals, IMeshSystemService meshes,
            IColorSystemService colors)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            _parent = parent;
            _visuals = visuals ?? throw new ArgumentNullException(nameof(visuals));
            _meshes = meshes ?? throw new ArgumentNullException(nameof(meshes));
            _colors = colors ?? throw new ArgumentNullException(nameof(colors));
        }

        public TornadoPresentationSettingsData Settings =>
            _visuals.GetTerrainVisual(TerrainContentKind.Tornado).TornadoPresentation ??
            throw new InvalidOperationException("[FieldTornadoPresenter] 토네이도 외형에는 전용 연출 설정이 필요합니다.");

        public bool IsAimed(long sourceId) => _active.TryGetValue(sourceId, out Instance instance) && instance.Arrow.IsVisible;

        /// <summary>외형 전환(커지며 등장·옮길 때 축소 후 확대)이 진행 중인지입니다. 다음 사건과 결과 끝 사실 반영이 기다립니다.</summary>
        internal bool IsAppearancePlaying
        {
            get
            {
                foreach (Instance instance in _active.Values)
                    if (instance.Appearance.IsPlaying) return true;
                return false;
            }
        }

        internal void TickAppearance(float deltaTime)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profilePlaybackSystemTornadoAppearancePollSample = _profilePlaybackSystemTornadoAppearancePoll.Auto();
#endif

            foreach (Instance instance in _active.Values)
                if (instance.Appearance.IsPlaying) instance.Appearance.Tick(deltaTime);
        }

        internal void CancelAppearance()
        {
            foreach (Instance instance in _active.Values) instance.Appearance.Reset();
        }

        /// <summary>진입·결과 끝에 허용된 영역으로 표시를 다시 맞춥니다. 목록에 없는 원본은 없앱니다.</summary>
        internal void Render(IReadOnlyList<TornadoAreaFact> areas, FieldGridMapper mapper)
        {
            ThrowIfReleased();
            if (areas == null) throw new ArgumentNullException(nameof(areas));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            _wanted.Clear();
            for (int i = 0; i < areas.Count; i++)
            {
                TornadoAreaFact area = areas[i];
                if (!_active.TryGetValue(area.SourceId, out Instance instance))
                {
                    instance = Create(area, mapper.TerrainCellScale);
                    _active.Add(area.SourceId, instance);
                }
                _wanted.Add(area.SourceId);
                instance.Appearance.Refresh(BodyPosition(area.Center, instance, mapper));
                instance.Body.transform.rotation = mapper.RotationToWorld(Rotation4.R0) * instance.BodyRotation;
                ApplyDirection(instance, area.Center, area.WarningDirection, mapper, Settings);
            }
            _removal.Clear();
            foreach (long id in _active.Keys)
                if (!_wanted.Contains(id)) _removal.Add(id);
            for (int i = 0; i < _removal.Count; i++)
                Remove(_removal[i]);
            _removal.Clear();
            _wanted.Clear();
        }

        public void Place(long sourceId, CellCoord center, FieldGridMapper mapper)
        {
            ThrowIfReleased();
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            if (_active.TryGetValue(sourceId, out Instance instance))
                instance.Body.transform.position = BodyPosition(center, instance, mapper);
        }

        public void SetDirection(long sourceId, CellCoord center, CellCoord? direction, FieldGridMapper mapper)
        {
            ThrowIfReleased();
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            if (_active.TryGetValue(sourceId, out Instance instance))
                ApplyDirection(instance, center, direction, mapper, Settings);
        }

        public bool TryGetBodyMove(long sourceId, CellCoord center, FieldGridMapper mapper, out Vector3 from, out Vector3 to)
        {
            ThrowIfReleased();
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            from = default;
            to = default;
            if (!_active.TryGetValue(sourceId, out Instance instance))
                return false;
            from = instance.Body.transform.position;
            to = BodyPosition(center, instance, mapper);
            return true;
        }

        public bool TryGetBodyPosition(long sourceId, out Vector3 position)
        {
            position = default;
            if (!_active.TryGetValue(sourceId, out Instance instance))
                return false;
            position = instance.Body.transform.position;
            return true;
        }

        public void MoveBody(long sourceId, Vector3 position)
        {
            ThrowIfReleased();
            if (_active.TryGetValue(sourceId, out Instance instance))
                instance.Body.transform.position = position;
        }

        /// <summary>모든 표시를 없앱니다. 전투가 바뀌면 이 경로로 지난 전투의 원본이 남지 않습니다.</summary>
        internal void Clear()
        {
            _removal.Clear();
            _removal.AddRange(_active.Keys);
            List<Exception> errors = null;
            for (int i = 0; i < _removal.Count; i++)
            {
                try
                {
                    Remove(_removal[i]);
                }
                catch (Exception error)
                {
                    (errors ??= new List<Exception>()).Add(error);
                }
            }
            _removal.Clear();
            if (errors != null)
                throw new AggregateException("[FieldTornadoPresenter] 표시 해제 중 실패했습니다.", errors);
        }

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
                Transform root = _root;
                _root = null;
                if (root != null) DestroyObject(root.gameObject);
            }
        }

        // 칸 바탕 기준 자리입니다. 높이는 덮는 칸의 타일 바탕 중 가장 높은 값이라 몸이 타일에 묻히지 않습니다.
        // 바닥 교체로 바뀌는 바탕은 보지 않습니다. 바탕이 결과마다 바뀌면 자리 옮김 전환이 헛되이 돕니다.
        private static Vector3 BodyPosition(CellCoord center, Instance instance, FieldGridMapper mapper)
        {
            float height = float.NegativeInfinity;
            IReadOnlyList<CellCoord> cells = TornadoGeometry.Footprint(center, instance.SideLength);
            for (int i = 0; i < cells.Count; i++)
                height = Mathf.Max(height, mapper.Surface.TileBaseHeightAt(cells[i]));
            return mapper.LocalToWorld(mapper.CellCenterToLocal(center) + (Vector3.up * height) + instance.Offset);
        }

        // 화살표는 중심에서 방향으로 (반지름 + 1)칸입니다(계획 TP-6). 경고가 있으면 한 칸은 갈 수 있어 그 칸은 늘 판 안입니다.
        // 그 자리가 점등의 원위치이고, 숨어 있다 켜질 때만 점등을 처음부터 시작합니다(켜진 채 다시 맞추면 주기를 이어 갑니다).
        private static void ApplyDirection(Instance instance, CellCoord center, CellCoord? direction, FieldGridMapper mapper,
            TornadoPresentationSettingsData settings)
        {
            if (!direction.HasValue || direction.Value == CellCoord.Zero)
            {
                instance.Arrow.Clear();
                return;
            }
            CellCoord step = direction.Value;
            int reach = (instance.SideLength / 2) + 1;
            var cell = new CellCoord(checked(center.X + (step.X * reach)), checked(center.Y + (step.Y * reach)));
            // 띄움은 기존 월드 단위를 유지하고, 공용 화살촉에는 부모(필드) 로컬 자리·방향을 넘깁니다.
            instance.ArrowAnchor = mapper.WorldToLocal(mapper.LocalToWorld(mapper.CellSurfaceToLocal(cell)) +
                (mapper.WorldUp * settings.DirectionArrowLift));
            instance.ArrowForward = mapper.StepToLocal(step);
            if (!instance.Arrow.IsVisible)
                instance.PulseElapsed = 0f;
            ShowPulse(instance, settings.DirectionArrowPulse);
        }

        // 지금 점등 순간의 모습으로 화살촉을 그립니다. 꺼진 구간이면 그리기만 끕니다(표시 중인 상태는 그대로라 경고 판단 IsAimed는 바뀌지 않음).
        private static void ShowPulse(Instance instance, TornadoArrowPulse pulse)
        {
            if (!pulse.TryEvaluate(instance.PulseElapsed, out float slide, out float brightness))
            {
                instance.Arrow.SetLit(false);
                return;
            }
            Color color = instance.ArrowColor;
            color.r *= brightness;
            color.g *= brightness;
            color.b *= brightness;
            instance.Arrow.Show(instance.ArrowAnchor, instance.ArrowForward, color, instance.ArrowTipOffset + slide);
        }

        // 프레임마다 켜진 화살촉의 점등을 진행합니다(FieldTornadoArrowPulse가 부름). 결과 재생과 무관하게 플레이어 차례에도 돕니다.
        private void TickPulse(float deltaTime)
        {
            if (_isReleased || _active.Count == 0)
                return;
            TornadoArrowPulse pulse = Settings.DirectionArrowPulse;
            if (pulse.IsStill)
                return;
            foreach (Instance instance in _active.Values)
            {
                if (!instance.Arrow.IsVisible)
                    continue;
                instance.PulseElapsed += deltaTime;
                ShowPulse(instance, pulse);
            }
        }

        private Instance Create(TornadoAreaFact area, float cellScale)
        {
            TerrainVisualData visual = _visuals.GetTerrainVisual(TerrainContentKind.Tornado);
            if (visual.Prefab == null)
                throw new InvalidOperationException("[FieldTornadoPresenter] 토네이도 외형에 프리팹이 없습니다.");
            TornadoPresentationSettingsData settings = Settings;
            if (_root == null)
            {
                _root = new GameObject("FieldTornadoes").transform;
                _root.SetParent(_parent, false);
                _root.gameObject.AddComponent<FieldTornadoArrowPulse>().Bind(TickPulse);
            }
            // 프리팹은 기준 칸 수로 만들어졌습니다. 판정 칸 수에 맞춰 배율과 프리팹 축 보정을 함께 키웁니다(계획 TP-5).
            float ratio = area.SideLength / (float)settings.BodyReferenceCells;
            GameObject body = null;
            FieldArrowhead arrow = null;
            try
            {
                body = Object.Instantiate(visual.Prefab, _root, false);
                body.name = $"FieldTornado_{area.SourceId}";
                body.SetActive(false);
                arrow = new FieldArrowhead(_root, $"FieldTornadoArrow_{area.SourceId}", settings.DirectionArrowMaterial,
                    settings.DirectionArrowLength, settings.DirectionArrowWidth, _meshes, _colors);
                Vector3 scale = Vector3.Scale(visual.Prefab.transform.localScale, visual.LocalScale * cellScale) * ratio;
                return new Instance(body, new CombatTornadoAppearance(body, scale, settings), arrow, visual.Prefab.transform.localRotation,
                    settings.DirectionArrowLength * .5f, visual.LocalOffset * cellScale * ratio, area.SideLength,
                    settings.DirectionArrowMaterial.GetColor(MaterialBaseTypeKey.MainColor.Name));
            }
            catch (Exception error)
            {
                try
                {
                    arrow?.Dispose();
                }
                catch (Exception releaseError)
                {
                    throw new AggregateException("[FieldTornadoPresenter] 구성과 정리에 실패했습니다.", error, releaseError);
                }
                finally
                {
                    DestroyObject(body);
                }
                throw;
            }
        }

        private void Remove(long sourceId)
        {
            if (!_active.TryGetValue(sourceId, out Instance instance))
                return;
            _active.Remove(sourceId);
            try
            {
                instance.Appearance.Reset();
            }
            finally
            {
                try
                {
                    instance.Arrow.Dispose();
                }
                finally
                {
                    DestroyObject(instance.Body);
                }
            }
        }

        private static void DestroyObject(GameObject gameObject)
        {
            if (gameObject == null) return;
            gameObject.SetActive(false);
            if (Application.isPlaying) Object.Destroy(gameObject);
            else Object.DestroyImmediate(gameObject);
        }

        private void ThrowIfReleased()
        {
            if (_isReleased) throw new InvalidOperationException("[FieldTornadoPresenter] 해제된 표시입니다.");
        }
    }
}
