using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageField.Item;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.MeshSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.AreaOutlineSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Entry;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map
{
    /// <summary>
    /// 필드와 그 위 물체(격자·바닥 타일 바탕·선로·오브젝트·지형·특성 흔적·기믹·가시밭)의 구성과 그리기입니다(씬 표시 나누기 설계 DS5).
    /// </summary>
    /// <remarks>
    /// 언제 부를지와 정리·해제 목록의 차례는 표시(<see cref="CombatSceneDisplay"/>)가 정합니다(설계 §3.8). 여기서는 단계 본문과
    /// 정리·해제 묶음(<see cref="Action"/> 배열, 생성 때 한 번)만 내놓고, 묶음을 스스로 실행하지 않습니다(실패가 표시 목록 한 겹으로 모이게, 설계 M2).
    /// 사건 Presenter·포인터를 격자에 연결하는 일은 표시가 <see cref="Build"/>와 <see cref="ApplyStaticHighlights"/> 사이에 합니다(설계 P4, 계획 TP-9).
    /// 기믹 표시 넷(흔적·토네이도·낙석·하붑)은 지금처럼 하나씩 받습니다. 기믹 공통 계약(평가 C2-01 2단계)은 다음 묶음이 이 자리를 바꿉니다.
    /// </remarks>
    internal sealed class CombatFieldDisplay
    {
        private readonly FieldView _field;
        private readonly StageFieldViewData _config;
        private readonly IColorSystemService _colors;
        private readonly IAreaOutlineSystemService _areaOutlines;
        private readonly IMeshSystemService _meshes;
        // 바닥 타일의 필드 칸별 윗면 높이 표입니다(타일 높이 설계 TH2). 배경이 타일 방식이 아니면 null이고 바탕은 모두 0입니다.
        private readonly CombatMapTileSurface _tileSurface;
        private readonly FieldObjectPresenter _objects;
        private readonly FieldTerrainPresenter _terrain;
        private readonly FieldRailwayPresenter _railway;
        private readonly FieldThornPresenter _thorns;
        // 특성 흔적 지형(땅 다지기·먼지 연막) 표시입니다(특성 시스템 View 설계 TS7). Object·Terrain처럼 Mapper를 붙잡지 않습니다.
        private readonly FieldTrailPresenter _trails;
        // 토네이도·낙석 경고·하붑은 진입 중 보드 완성까지 보류하고, 이후에는 기존 사실대로 바로 그립니다.
        private readonly CombatEntryDeferredDisplay _deferredDisplay;

        /// <param name="tileSurface">배경이 맞춘 바닥 타일 높이 표입니다. 자리는 늘 넘기고(타일 높이 계획 P10), 배경이 타일 방식이 아니면 null입니다.</param>
        /// <param name="thorns">가시밭 표시입니다(프리팹은 DB 표가 검사, View 3라운드 W12-5).</param>
        /// <param name="trails">특성 흔적 지형 표시입니다(외형은 특성 외형 표의 흔적 칸, 특성 시스템 View 설계 TS7).</param>
        /// <param name="tornado">움직이는 토네이도 표시입니다(토네이도 View 설계 TN3). 진입 보류가 그리고, 여기서는 정리·해제만 합니다.</param>
        /// <param name="rockfall">낙석 표시입니다(낙석 View 설계 RF2). 진입 보류가 그리고, 여기서는 정리·해제만 합니다.</param>
        /// <param name="haboob">하붑 구름 표시입니다(하붑 View 계획 HP-17). 진입 보류가 그리고, 여기서는 정리·해제만 합니다.</param>
        internal CombatFieldDisplay(FieldView field, StageFieldViewData config, IColorSystemService colors, IAreaOutlineSystemService areaOutlines,
            IMeshSystemService meshes, CombatMapTileSurface tileSurface, FieldObjectPresenter objects, FieldTerrainPresenter terrain,
            FieldRailwayPresenter railway, FieldThornPresenter thorns, FieldTrailPresenter trails, FieldTornadoPresenter tornado,
            FieldRockfallPresenter rockfall, FieldHaboobPresenter haboob, CombatEntryDeferredDisplay deferredDisplay)
        {
            _field = field != null ? field : throw new ArgumentNullException(nameof(field));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _colors = colors ?? throw new ArgumentNullException(nameof(colors));
            _areaOutlines = areaOutlines ?? throw new ArgumentNullException(nameof(areaOutlines));
            _meshes = meshes ?? throw new ArgumentNullException(nameof(meshes));
            _tileSurface = tileSurface;
            _objects = objects ?? throw new ArgumentNullException(nameof(objects));
            _terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
            _railway = railway ?? throw new ArgumentNullException(nameof(railway));
            _thorns = thorns ?? throw new ArgumentNullException(nameof(thorns));
            _trails = trails ?? throw new ArgumentNullException(nameof(trails));
            if (tornado == null) throw new ArgumentNullException(nameof(tornado));
            if (rockfall == null) throw new ArgumentNullException(nameof(rockfall));
            if (haboob == null) throw new ArgumentNullException(nameof(haboob));
            _deferredDisplay = deferredDisplay ?? throw new ArgumentNullException(nameof(deferredDisplay));

            // Mapper를 붙잡고 스스로 진행할 수 있는 필드 표시입니다(정리 묶음 D8의 필드 몫).
            BoundClears = new Action[] { _railway.Clear, _thorns.Clear };
            BoundReleases = new Action[] { _railway.Release, _thorns.Release };
            // Mapper를 붙잡지 않는 필드 표시입니다. 설치 직전에는 비우지 않고 새 Mapper로 다시 그립니다(D8).
            ObjectClears = new Action[] { _objects.Clear, _terrain.Clear };
            ObjectReleases = new Action[] { _objects.Release, _terrain.Release };
            // 필드 기믹 표시입니다(평가 C2-01 1단계). 순서: 흔적 → 토네이도 → 낙석 → 하붑. 진입 보류는 흔적을 뺀 셋을 따로 다룹니다.
            GimmickClears = new Action[] { _trails.Clear, tornado.Clear, rockfall.Clear, haboob.Clear };
            GimmickReleases = new Action[] { _trails.Release, tornado.Release, rockfall.Release, haboob.Release };
        }

        /// <summary>이번 설치의 격자입니다. 구성 전·정리·해제 뒤에는 null입니다.</summary>
        internal FieldGridMapper Mapper => _field.Mapper;

        /// <summary>선로·가시밭의 정리 묶음입니다(정리 묶음 <c>_fieldBound</c>의 필드 몫).</summary>
        internal Action[] BoundClears { get; }

        /// <summary>선로·가시밭의 해제 묶음입니다.</summary>
        internal Action[] BoundReleases { get; }

        /// <summary>오브젝트·지형의 정리 묶음입니다.</summary>
        internal Action[] ObjectClears { get; }

        /// <summary>오브젝트·지형의 해제 묶음입니다.</summary>
        internal Action[] ObjectReleases { get; }

        /// <summary>기믹 표시(흔적·토네이도·낙석·하붑)의 정리 묶음입니다. 정리 목록의 기믹 이름은 여기 한 곳입니다.</summary>
        internal Action[] GimmickClears { get; }

        /// <summary>기믹 표시의 해제 묶음입니다.</summary>
        internal Action[] GimmickReleases { get; }

        /// <summary>정적 필드를 구성합니다: 격자 → 바닥 타일 바탕 → 지형의 타일 연결 → 선로 설치.</summary>
        internal void Build(CombatStateData state)
        {
            _field.Build(state.BoardWidth, state.BoardHeight, _config, _colors, _areaOutlines, _meshes);
            // 바닥 타일 윗면을 높이 표의 바탕으로 넣습니다(타일 높이 설계 TH6). 칸 높이는 결과 끝 그리기에서 바탕 + 지형으로 바뀌고,
            // 바뀌면 강조·데칼을 다시 그립니다. 같은 표로 지형 표시가 바닥 교체 칸의 타일을 찾습니다(타일 교체 설계 TR4).
            _field.SetGroundBase(_tileSurface);
            _terrain.BindTiles(_tileSurface);
            _railway.Install(_field.Mapper, state.RailCells);
        }

        /// <summary>아군 배치 구역·선로의 정적 강조를 둡니다.</summary>
        internal void ApplyStaticHighlights(CombatStateData state) => _field.ApplyStaticHighlights(state.PlayerZone, state.RailCells);

        /// <summary>
        /// 칸 표면 높이(고지대)를 그 위에 서는 유닛보다 먼저 정합니다(고지대 View 설계 F10·HG-2). 블록은 <see cref="RenderObjects"/>의 지형 그리기가 놓습니다.
        /// 높이가 바뀌면 높이별로 그린 강조·데칼을 다시 그립니다(HG-4a). 강조 갱신이 없는 결과 끝 그리기에서도 맞도록 여기서 알립니다.
        /// </summary>
        internal void SyncSurface(CombatStateData state)
        {
            if (_terrain.SyncSurface(state.Environment, state.EnvironmentBindings, _field.Mapper))
                _field.RepaintSurface();
        }

        /// <summary>오브젝트 → 지형 → 특성 흔적 → 기믹(진입 보류 경유) → 가시밭을 최종 사실로 그립니다.</summary>
        internal void RenderObjects(CombatStateData state, int stageKey)
        {
            // 격자는 단계마다 다시 읽습니다(옛 표시와 같음).
            _objects.Render(state.Environment, state.EnvironmentBindings, stageKey, _field.Mapper);
            _terrain.Render(state.Environment, state.EnvironmentBindings, stageKey, _field.Mapper);
            // 특성 흔적은 바닥 지형 위에 겹칩니다. 사건 시점에 놓은 것을 최종 사실로 다시 맞춥니다(특성 시스템 View 설계 TS7).
            _trails.Render(state.Environment, _field.Mapper);
            _deferredDisplay.Render(state.Gimmicks, state.ReadTornadoAreas(Owner.Player), _field.Mapper);
            // 새 생성 칸은 모든 사건을 재생한 뒤 전달받은 Core 목록만으로 표시합니다.
            _thorns.Sync(state.CommandState.ThornFields, _field.Mapper);
        }

        /// <summary>구성한 필드를 놓습니다(정리·해제 목록의 마지막). 셀 색은 되돌리지 않습니다.</summary>
        internal void ReleaseField() => _field.Release();
    }
}
