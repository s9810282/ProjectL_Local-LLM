using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageField.Item;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.MeshSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.AreaOutlineSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Feedback;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Hud;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Turn;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.ActionButtons;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Entry;
using UnityEngine;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Selection;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display
{
    /// <summary>
    /// Combat 씬의 Presenter(필드·유닛·Object·Terrain·HUD·고스트·행동 버튼·가방·정보창·결과 화면)에 값과 표시 의도를 나눠 주는 구현입니다(계획 D.5·E).
    /// 정적 필드 구성은 설치 때 한 번, 동적 값은 결과마다 반영합니다.
    /// 사건 재생은 <see cref="CombatSceneEventPlayback"/>(재생 계약으로 그대로 넘김, <see cref="EventPlayback"/>),
    /// 화면 좌표의 칸 집기와 격자 매핑은 <see cref="CombatPointerMapper"/>에 위임합니다(구조 개선 VR4, 구조 리팩토링 설계 D5·D7).
    /// </summary>
    /// <remarks>
    /// 씬 연결(Attach) 하나에 하나 만들어지고 Release 뒤 다시 쓰지 않습니다. Buffer를 읽지 않고 받은 값만 씁니다.
    /// 외형은 View DB, 색은 Render ColorSystem을 씁니다. 배경 맵은 씬 연결 수명이 먼저 세우고(배경 설계 BD3), 이 표시는 해제 때 카메라 조작 시점을 처음 위치·크기로 되돌린 뒤 그 맵 카메라를 받은 카메라 창구를 비웁니다(카메라 조작 설계 CC6).
    /// 해제 뒤 규칙(D9): 사건 시작·Install·Refresh·완료 표시는 예외, 표시 의도·집기는 아무것도 하지 않습니다(집기는 false).
    /// 사건 시작·Tick·취소는 재생 계약(<see cref="EventPlayback"/>) 쪽이며 같은 규칙을 재생 조정이 지킵니다(연출 바탕 계획 §8, A-4b).
    /// Tick과 취소는 진행 중인 것이 없으면 아무것도 하지 않습니다. 연결 해제(콜백 모두 null)는 해제 뒤에도 전달합니다.
    /// </remarks>
    internal sealed class CombatSceneDisplay : ICombatBattleDisplay, ICombatUnitInspection, ICombatGimmickRangeFeedback
    {
        private readonly FieldView _field;
        private readonly CombatMovementPlanningPresenter _movementPlanning;
        private readonly CombatPlacementFeedback _placement;
        private readonly CombatAttackFeedback _attack;
        private readonly FieldUnitPresenter _units;
        private CombatUnitPreparation _unitPreparation;
        private readonly UnitHUDLayer _unitHudLayer;
        // 가방 표시입니다(가방 설계 P1). 씬 루트 참조 검사가 연결을 보장합니다.
        private readonly ICombatPackDisplay _pack;
        private readonly CombatHud _hud;
        private readonly CombatTurnDisplayPresenter _turnDisplay;
        private readonly CombatEndUI _completion;
        private readonly CombatUnitInfo _unitInfo;
        private readonly CombatSelectionState _selection;
        private readonly CombatInspectionPresenter _inspection;
        internal void RefreshInspectionSelection()
        {
            _unitHudLayer.SetSelectedEntity(_selection.SelectedEntityId);
            RenderSelectedActor();
            // 전투 목표 강조도 선택 강조와 같은 보인 사실로 사건마다 맞춥니다(평가 C1-14). 선택이 바뀔 때도 불리지만 칸이 같으면 다시 그리지 않습니다.
            RenderObjectiveTarget();
        }
        private IReadOnlyList<CharacterData> _displayedCharacters = Array.Empty<CharacterData>();
        // 전투 목표(지정 보스)입니다. 결과 끝 그리기(RefreshCore)가 Buffer 원본으로 정하고, 강조는 보인 사실로 그립니다(평가 C1-14).
        private EntityId _objectiveTarget;
        private readonly PlacementGhost _ghost;
        private readonly CombatActionPanel _actions;
        private readonly CombatSceneRoot _cursorSnapSettings;
        // 화면 좌표 → 칸·격자 변환과 UI 가림 Raycast(구조 개선 VR4). Install 때 연결, Clear 때 해제, Release 때 최종 해제.
        private readonly CombatPointerMapper _pointer;
        private readonly StageFieldViewData _fieldConfig;
        private readonly IColorSystemService _colors;
        private readonly IAreaOutlineSystemService _areaOutlines;
        private readonly IMeshSystemService _meshes;
        private readonly CombatSceneEventPresenter _events;
        // 국면 배경음·승리 효과음·완료 뒤 배경음을 내는 배경음 정책입니다(평가 C2-03 1단계). 생성자가 받은 오디오·소리 프리셋으로 만듭니다.
        private readonly CombatBgmPolicy _bgm;
        // 맵 카메라 사실을 받은 카메라 창구입니다. 해제 때 비웁니다(빌린 줌 취소 포함, 배경 설계 BD4).
        private readonly CombatCameraWindow _camera;
        // 플레이어가 움직이는 시점입니다(카메라 조작 설계 CC1). 해제 때 창구를 비우기 전에 처음 위치·크기로 되돌립니다(CC6, 계획 P6).
        private readonly CombatCameraView _cameraView;
        private readonly FieldObjectPresenter _objects;
        private readonly FieldTerrainPresenter _terrain;
        private readonly FieldRailwayPresenter _railway;
        private readonly FieldThornPresenter _thorns;
        // 특성 흔적 지형(땅 다지기·먼지 연막) 표시입니다(특성 시스템 View 설계 TS7). Object·Terrain처럼 Mapper를 붙잡지 않습니다.
        private readonly FieldTrailPresenter _trails;
        // 움직이는 토네이도 표시입니다(토네이도 View 설계 TN3). 플레이어에게 허용된 영역만 그립니다.
        private readonly FieldTornadoPresenter _tornado;
        // 낙석 표시입니다(낙석 View 설계 RF2). 경고 칸을 결과 끝에 Core 상태로 다시 맞춥니다.
        private readonly FieldRockfallPresenter _rockfall;
        // 하붑 구름 표시입니다(하붑 View 계획 HP-17). 구름 칸의 연기를 결과 끝에 Core 상태로 다시 맞춥니다.
        private readonly FieldHaboobPresenter _haboob;
        // 바닥 타일의 필드 칸별 윗면 높이 표입니다(타일 높이 설계 TH2). 배경이 타일 방식이 아니면 null이고 바탕은 모두 0입니다.
        private readonly CombatMapTileSurface _tileSurface;
        // 사건 재생 조정(구조 리팩토링 설계 D5). 사건 Presenter·레일 표시의 필드 연결 수명은 이 표시가 정한다.
        private readonly CombatSceneEventPlayback _eventPlayback;
        private readonly CombatBoardEntryPresentation _entryPresentation;
        private readonly CombatEntryDeferredDisplay _deferredDisplay;
        internal ICombatEntryPresentation EntryPresentation => _entryPresentation;
        // 정리 묶음(D8): Mapper를 붙잡고 스스로 진행할 수 있는 표시다. Install 직전·실패와 Clear가 이 목록 하나를 함께 쓴다.
        private readonly Action[] _fieldBound;
        // 필드 기믹 표시(흔적·토네이도·낙석·하붑)의 정리·해제 묶음입니다(평가 C2-01 1단계). Install 실패·Clear·Release 목록이 이 묶음을 붙여 쓰므로,
        // 정리 목록의 기믹 이름은 여기 한 곳입니다. 생성 때 한 번 만듭니다. 진입 보류(CombatEntryDeferredDisplay)는 흔적을 뺀 셋을 따로 다룹니다.
        private readonly Action[] _gimmickClears;
        private readonly Action[] _gimmickReleases;

        private bool _isReleased;
        public float InnerCellInset => _pointer.InnerCellInset;

        /// <summary>카메라 조작 시점입니다(카메라 조작 설계 CC1). 카메라 입력이 이것만 움직입니다.</summary>
        public ICombatCameraControl CameraControl => _cameraView;

        /// <summary>Factory가 조립한 같은 협력 객체를 보관하고 공통 정리 목록을 정의합니다.</summary>
        /// <remarks>
        /// SceneRoot 하나에 살아 있는 Display는 하나입니다. 생성·검증·Configure는 CombatSceneDisplayFactory가 담당합니다.
        /// 정보창·유닛 HUD 층·가방·결과 화면은 필수입니다(View 단계 설계 W4). 씬 루트 참조 검사와 Factory가 늘 넘깁니다.
        /// </remarks>
        /// <param name="audio">오디오 서비스입니다(필수, View 2라운드 W6-7). 배경음 정책(<see cref="CombatBgmPolicy"/>)이 씁니다.</param>
        /// <param name="sounds">국면·완료 뒤 배경음 프리셋을 답합니다(필수, View 3라운드 W9-2). 배경음 정책이 씁니다.</param>
        /// <param name="thorns">가시밭 표시입니다(필수, 프리팹은 DB 표가 검사, View 3라운드 W12-5).</param>
        /// <param name="trails">특성 흔적 지형 표시입니다(필수, 외형은 특성 외형 표의 흔적 칸, 특성 시스템 View 설계 TS7).</param>
        /// <param name="cameraView">카메라 조작 시점입니다(필수, 맵 카메라가 없으면 조작 없는 시점, 카메라 조작 계획 P8).</param>
        /// <param name="tileSurface">
        /// 배경이 맞춘 바닥 타일의 필드 칸별 윗면 높이 표입니다. 자리는 늘 넘기고(타일 높이 계획 P10), 배경이 타일 방식이 아니거나 배경이 없으면(표시 시험) null입니다.
        /// null이면 높이 표의 바탕이 모두 0이라 화면이 타일 높이 이전과 같습니다.
        /// </param>
        internal CombatSceneDisplay(
            FieldView field,
            CombatMovementPlanningPresenter movementPlanning,
            CombatPlacementFeedback placement,
            CombatAttackFeedback attack,
            FieldUnitPresenter units,
            CombatHud hud,
            PlacementGhost ghost,
            CombatActionPanel actions,
            CombatSceneRoot cursorSnapSettings,
            CombatPointerMapper pointer,
            StageFieldViewData fieldConfig,
            IColorSystemService colors,
            IAreaOutlineSystemService areaOutlines,
            IMeshSystemService meshes,
            CombatSceneEventPresenter events,
            IAudioSystemService audio,
            CombatSoundPresets sounds,
            CombatCameraWindow camera,
            CombatCameraView cameraView,
            FieldObjectPresenter objects,
            FieldTerrainPresenter terrain,
            FieldRailwayPresenter railway,
            FieldThornPresenter thorns,
            FieldTrailPresenter trails,
            FieldTornadoPresenter tornado,
            FieldRockfallPresenter rockfall,
            FieldHaboobPresenter haboob,
            CombatSceneEventPlayback eventPlayback,
            CombatUnitInfo unitInfo,
            UnitHUDLayer unitHudLayer,
            ICombatPackDisplay pack,
            CombatEndUI completion,
            CombatSelectionState selection,
            CombatTurnDisplayPresenter turnDisplay,
            CombatMapTileSurface tileSurface, CombatInspectionPresenter inspection, CombatEntryDeferredDisplay deferredDisplay,
            CombatBoardEntryPresentation entryPresentation = null)
        {
            _inspection = inspection ?? throw new ArgumentNullException(nameof(inspection));
            _selection = selection ?? throw new ArgumentNullException(nameof(selection));
            _pack = pack ?? throw new ArgumentNullException(nameof(pack));
            _field = field;
            _movementPlanning = movementPlanning;
            _placement = placement;
            _attack = attack;
            _units = units;
            _unitHudLayer = unitHudLayer != null ? unitHudLayer : throw new ArgumentNullException(nameof(unitHudLayer));
            _hud = hud;
            _turnDisplay = turnDisplay ?? throw new ArgumentNullException(nameof(turnDisplay));
            _completion = completion != null ? completion : throw new ArgumentNullException(nameof(completion));
            _unitInfo = unitInfo != null ? unitInfo : throw new ArgumentNullException(nameof(unitInfo));
            _ghost = ghost;
            _actions = actions;
            _cursorSnapSettings = cursorSnapSettings;
            _pointer = pointer;
            _fieldConfig = fieldConfig;
            _colors = colors;
            _areaOutlines = areaOutlines;
            _meshes = meshes;
            _events = events;
            // 오디오·소리 프리셋은 필수입니다. 없으면 배경음 정책이 ArgumentNullException(audio·sounds)을 던집니다(검사 차례는 예전과 같음).
            _bgm = new CombatBgmPolicy(audio, sounds);
            _camera = camera;
            _cameraView = cameraView ?? throw new ArgumentNullException(nameof(cameraView));
            _objects = objects;
            _terrain = terrain;
            _railway = railway;
            _thorns = thorns ?? throw new ArgumentNullException(nameof(thorns));
            _trails = trails ?? throw new ArgumentNullException(nameof(trails));
            _tornado = tornado ?? throw new ArgumentNullException(nameof(tornado));
            _rockfall = rockfall ?? throw new ArgumentNullException(nameof(rockfall));
            _haboob = haboob ?? throw new ArgumentNullException(nameof(haboob));
            _tileSurface = tileSurface;
            _eventPlayback = eventPlayback;
            _entryPresentation = entryPresentation;
            _deferredDisplay = deferredDisplay ?? throw new ArgumentNullException(nameof(deferredDisplay));
            _fieldBound = new Action[]
            {
                CancelEntryPresentation, _eventPlayback.CancelPendingEvents, _movementPlanning.Clear, _events.Clear,
                _pointer.Unbind, _railway.Clear, _thorns.Clear
            };
            // 순서는 정리 목록에 적던 그대로입니다: 흔적 → 토네이도 → 낙석 → 하붑.
            _gimmickClears = new Action[] { _trails.Clear, _tornado.Clear, _rockfall.Clear, _haboob.Clear };
            _gimmickReleases = new Action[] { _trails.Release, _tornado.Release, _rockfall.Release, _haboob.Release };
        }

        #region 표시

        /// <summary>이 표시 연결에 귀속된 준비를 조립합니다. 진행은 AfterLoad 뒤 LoadingSystem이 소유합니다.</summary>
        internal CombatUnitPreparation CreateUnitPreparation(CombatUnitPreparationInput input, Func<bool> isCurrent)
        {
            ThrowIfReleased();
            if (_unitPreparation != null)
                throw new InvalidOperationException("[CombatSceneDisplay] 같은 연결의 선택 유닛 준비가 이미 있습니다.");
            _unitPreparation = new CombatUnitPreparation(_units, input, isCurrent);
            return _unitPreparation;
        }

        private void CancelUnitPreparation() => _unitPreparation?.Cancel();

        public void Install(in CombatDisplayValues values)
        {
            ThrowIfReleased();
            ClearSelection();
            _turnDisplay.Clear();
            // 설치 중의 UI/필드 콜백보다 먼저 목록 시점을 고정합니다. RefreshCore에서 한 번 적용합니다.
            IReadOnlyList<CharacterData> characters = SnapshotCharacters(values.Characters);
            // Build 전 실패라도 이전 Mapper로 사건 표시·좌표 변환이 재개되지 않도록 먼저 연결을 끊는다.
            // Object·Terrain·흔적은 Mapper를 붙잡고 스스로 움직이지 않으므로 비우지 않는다. 아래 Refresh가 새 Mapper로 다시 그리고 View를 재사용한다(D8).
            RunAll(_fieldBound);
            try
            {
                HideCompletion();
                _hud.Present();
                CombatStateData state = values.State;
                _field.Build(state.BoardWidth, state.BoardHeight, _fieldConfig, _colors, _areaOutlines, _meshes);
                // 바닥 타일 윗면을 높이 표의 바탕으로 넣습니다(타일 높이 설계 TH6). 칸 높이는 아래 RefreshCore에서 바탕 + 지형으로 바뀌고,
                // 바뀌면 강조·데칼을 다시 그립니다. 같은 표로 지형 표시가 바닥 교체 칸의 타일을 찾습니다(타일 교체 설계 TR4).
                _field.SetGroundBase(_tileSurface);
                _terrain.BindTiles(_tileSurface);
                _railway.Install(_field.Mapper, state.RailCells);
                _events.Bind(_field.Mapper);
                _pointer.Bind(_field.Mapper);
                _field.ApplyStaticHighlights(state.PlayerZone, state.RailCells);
                _entryPresentation?.PrepareDisplay();
                RefreshCore(values, characters);
                // 첫 그리기 뒤, 아직 안 만든 것만 미리 만듭니다(GC 끊김 설계 GH6). 결과 끝 Refresh에서는 하지 않습니다.
                // 등장 연출 준비보다 먼저 해, 미리 만들기가 실패해도 준비된 등장 연출이 남지 않게 합니다.
                Prewarm(characters);
                _entryPresentation?.Prepare(characters, state.Structures);
            }
            catch (Exception error)
            {
                ClearSelection();
                // 연결됐던 대상까지 끊고, 일부만 그려진 Object·Terrain·기믹(흔적·토네이도·낙석·하붑)도 비운다. 다음 Install에서 다시 연결한다.
                try
                {
                    RunAll(Combine(Combine(_fieldBound, _turnDisplay.Clear, ClearUnitInfo, _objects.Clear, _terrain.Clear), _gimmickClears));
                }
                catch (Exception cleanup)
                {
                    throw new AggregateException(error, cleanup);
                }
                throw;
            }
        }

        private void CancelEntryPresentation() => _entryPresentation?.Cancel();

        public void Refresh(in CombatDisplayValues values)
        {
            ThrowIfReleased();
            RefreshCore(values, SnapshotCharacters(values.Characters));
        }

        private void RefreshCore(in CombatDisplayValues values, IReadOnlyList<CharacterData> characters)
        {
            if (!_eventPlayback.IsPresentationComplete) throw new InvalidOperationException("남은 기차·유닛 연출을 마친 뒤 최종 사실을 표시해야 합니다.");
            CombatStateData state = values.State;
            _displayedCharacters = characters;
            _inspection.Synchronize(state.CommandState.Turn.StagePhase, values.Owned, state.TraitSettings);
            // 칸 표면 높이(고지대)는 그 위에 서는 유닛보다 먼저 정합니다(고지대 View 설계 F10·HG-2). 블록은 아래 지형 그리기가 놓습니다.
            // 높이가 바뀌면 높이별로 그린 강조·데칼을 다시 그립니다(HG-4a). 강조 갱신이 없는 Refresh에서도 맞도록 여기서 알립니다.
            if (_terrain.SyncSurface(state.Environment, state.EnvironmentBindings, _field.Mapper))
                _field.RepaintSurface();
            // 설치 첫 그리기와 결과 끝 그리기의 국면 배경음은 배경음 정책이 냅니다(평가 C2-03). 결과 재생 중 국면이 바뀌는 사건의 배경음은 사건 소리(CombatEventAudio)가 냅니다.
            // 국면 → 프리셋 번호는 소리 프리셋 한 곳입니다(구조 원칙 평가 R2 (a), View 3라운드 W9-2).
            _bgm.ApplyPhase(state.CommandState.IsNight);
            // 목표 대상은 전투 동안 바뀌지 않지만 Buffer 원본을 매번 따른다(보스 활성화 설계 D3). 사실 그리기(_events.Refresh) 전에 정해,
            // 그 그리기가 부르는 강조 맞춤(RefreshInspectionSelection)이 이번 목표를 쓰게 한다(평가 C1-14).
            _objectiveTarget = CombatObjectiveDisplay.TargetOf(state.CommandState.Objective);
            _events.Refresh(_displayedCharacters, state.Structures);
            _unitHudLayer.SetSelectedEntity(_selection.SelectedEntityId);
            _objects.Render(state.Environment, state.EnvironmentBindings, values.StageKey, _field.Mapper);
            _terrain.Render(state.Environment, state.EnvironmentBindings, values.StageKey, _field.Mapper);
            // 특성 흔적은 바닥 지형 위에 겹칩니다. 사건 시점에 놓은 것을 최종 사실로 다시 맞춥니다(특성 시스템 View 설계 TS7).
            _trails.Render(state.Environment, _field.Mapper);
            // 토네이도·낙석 경고·하붑은 진입 중 보드 완성까지 보류하고, 이후에는 기존 사실대로 바로 그립니다.
            _deferredDisplay.Render(state.Gimmicks, state.ReadTornadoAreas(Owner.Player), _field.Mapper);
            // 새 생성 칸은 모든 사건을 재생한 뒤 전달받은 Core 목록만으로 표시합니다.
            _thorns.Sync(state.CommandState.ThornFields, _field.Mapper);
            _hud.Render(values.RunGold);
            _turnDisplay.Render(state.CommandState.Turn, state.CommandState.Phase);
            // 재고는 골드와 같은 그리기에서 넘긴다. 가방은 받은 스냅숏만 그린다(가방 설계 P1).
            _pack.ShowInventory(values.ItemInventory);
            _unitInfo.Render(state.CommandState.Turn.StagePhase);
            if (_isReleased)
                return;
            // 선택 아군과 전투 목표(보스 활성화 설계 D3)가 점유한 칸을 강조한다. 둘 다 최종 사실로 바뀐 보인 사실에서 읽는다.
            RenderSelectedActor();
            RenderObjectiveTarget();
        }

        /// <summary>
        /// 전투 중 처음 나올 때 만들던 유닛 표시·HUD·고스트·투사체·몸 위 글자를 설치 때 만듭니다(GC 끊김 설계 GH6, 계획 TP-6).
        /// 씬 전환 로딩 안이라 끊김이 보이지 않습니다. 셀 캐릭터(살아 있고 제외되지 않은 것 — 필드·손패·적 대기 모두)는 여기 한 곳에서 정하고,
        /// 고스트는 그중 플레이어 캐릭터 키만 셉니다. 전투 중 소환되는 유닛은 키를 미리 몰라 넣지 않습니다. 실패하면 설치 실패 정리 길로 갑니다(계획 TP-7).
        /// </summary>
        private void Prewarm(IReadOnlyList<CharacterData> characters)
        {
            var counts = new Dictionary<EntityKey, int>();
            var playerKeys = new List<EntityKey>();
            for (int i = 0; i < characters.Count; i++)
            {
                CharacterData character = characters[i];
                if (character.Health.IsDead || character.Area == CharacterRuntimeArea.Excluded)
                    continue;
                EntityKey key = character.CharacterKey;
                counts[key] = counts.TryGetValue(key, out int count) ? count + 1 : 1;
                if (character.Owner == Owner.Player && !playerKeys.Contains(key))
                    playerKeys.Add(key);
            }
            _units.Prewarm(counts);
            for (int i = 0; i < playerKeys.Count; i++)
                _ghost.Prewarm(playerKeys[i]);
            _events.Prewarm(counts.Keys);
        }

        /// <summary>가변 Buffer 목록의 불변 개체 참조를 완료 표시 한 번에 고정합니다.</summary>
        private static IReadOnlyList<CharacterData> SnapshotCharacters(IReadOnlyList<CharacterData> characters)
        {
            if (characters.Count == 0)
                return Array.Empty<CharacterData>();
            var snapshot = new CharacterData[characters.Count];
            for (int i = 0; i < snapshot.Length; i++)
                snapshot[i] = characters[i];
            return Array.AsReadOnly(snapshot);
        }

        private void ClearSelection()
        {
            _inspection.Clear();
            _unitHudLayer.SetSelectedEntity(EntityId.Empty);
            _displayedCharacters = Array.Empty<CharacterData>();
            _objectiveTarget = EntityId.Empty;
        }

        /// <summary>선택 아군이 점유한 칸을 보인 사실(선택 reader)에서 읽어 강조합니다. 선택 아군이 없으면 강조를 지웁니다.</summary>
        private void RenderSelectedActor()
        {
            IReadOnlyList<CellCoord> cells = _selection.SelectedActorCells;
            if (cells.Count == 0)
                _field.ClearLayer(FieldHighlightLayer.ActiveActor);
            else
                _field.SetLayerCells(FieldHighlightLayer.ActiveActor, cells);
        }

        /// <summary>
        /// 전투 목표가 점유한 칸을 목표 층에 둡니다. 선택 강조와 같은 보인 사실에서 읽어, 결과 재생 중 넉백·이동·사망을 사건마다 따라갑니다(평가 C1-14).
        /// 목표가 없거나, 필드에 없거나(손패·낮 대기 영역·제외), 죽었으면(보인 사실이 거름) 층을 지웁니다. 같은 ID가 구조물이면 그리지 않습니다(예전과 같음).
        /// </summary>
        private void RenderObjectiveTarget()
        {
            if (_events.InspectionFacts.TryGet(_objectiveTarget, out CombatInspectionFacts facts) && facts.Character != null)
                _field.SetLayerCells(FieldHighlightLayer.ObjectiveTarget, facts.Cells);
            else
                _field.ClearLayer(FieldHighlightLayer.ObjectiveTarget);
        }

        public void Clear()
        {
            ThrowIfReleased();
            RunAll(Combine(Combine(Combine(new Action[] { CancelUnitPreparation, ClearSelection }, _fieldBound), ClearUnitInfo, _units.HudFeedback.ClearAttackHealthPreview, _ghost.Hide, _units.Release,
                HideCompletion, _hud.Clear, _turnDisplay.Clear, HideActionsCore, _objects.Clear, _terrain.Clear), _gimmickClears, _field.Release));
        }

        public void Release()
        {
            if (_isReleased)
                return;

            _isReleased = true;
            // 해제 뒤 사건 시작 거절(D9)은 재생 조정이 맡습니다. 정리 목록보다 먼저 알립니다.
            _eventPlayback.MarkReleased();
            // 소유 자원 해제(D8)는 이 목록 한 곳에서만 정의한다. 기존 표시는 풀·참조를 정리하고 View 소유 레일 인스턴스는 제거한다.
            // 셀 색은 되돌리지 않는다.
            RunAll(Combine(new Action[] { CancelUnitPreparation, ClearSelection, CancelEntryPresentation, _inspection.Release, ClearUnitInfo, _eventPlayback.CancelPendingEvents, _movementPlanning.Release, _units.HudFeedback.ClearAttackHealthPreview, _pointer.Release, _events.Release, _railway.Release, _thorns.Release,
                HideCompletion, _turnDisplay.Clear,
                () => _actions.Bind(null, null, null), HideActionsCore,
                _ghost.Release, _units.Release, _units.ReleaseHudLayer, _units.ReleasePools, ReleaseUnitHudLayer,
                _objects.Release, _terrain.Release }, _gimmickReleases, _cameraView.Restore, _camera.DetachMap, _field.Release));
        }

        public void ShowCompletion(BattleCompletionChanges completion, Func<bool> confirm)
        {
            ThrowIfReleased();
            if (confirm == null) throw new ArgumentNullException(nameof(confirm));
            if (completion.Outcome.Outcome == StageOutcome.Victory) _bgm.PlayVictory();
            // 확인이 받아들여졌을 때만 완료 뒤 배경음으로 바꾼다. 국면 배경음(Refresh)·승리 효과음과 같은 배경음 정책이 낸다(View 2라운드 W6-5, 평가 C2-03).
            _completion.Present(completion, () =>
            {
                if (!confirm())
                    return;
                _bgm.ApplyAfterCompletion();
            });
        }

        // 정리 목록에서도 불리므로 다른 정리처럼 파괴된 객체를 거릅니다.
        private void HideCompletion()
        {
            if (_completion != null) _completion.Dismiss();
        }

        #endregion

        #region 행동 버튼 (행동 버튼 설계)

        public void BindActions(Action attack, Action endTurn, Action returnToCheckpoint)
        {
            // 해제 뒤 새 연결은 무시하지만, 연결 해제(모두 null)는 정리 콜백이므로 항상 전달한다.
            bool unbind = attack == null && endTurn == null && returnToCheckpoint == null;
            if (_isReleased && !unbind)
                return;
            _actions.Bind(attack, endTurn, returnToCheckpoint);
        }

        public void SetActionsAvailable(in CombatActionButtons buttons)
        {
            if (!_isReleased)
                _actions.SetAvailable(buttons);
        }

        // 전투 정리·씬 연결 해제 때 버튼을 숨긴다. Release 안에서도 불리므로 해제 여부를 보지 않는다.
        private void HideActionsCore() => _actions.SetAvailable(default);

        #endregion

        #region 집기·좌표 변환 (ICombatPointerPicker·ICombatPointerMapping, PointerMapper에 위임)

        public ICombatSelectionReader Selection => _selection;

        public void InspectUnit(float screenX, float screenY)
        {
            if (_isReleased || _pointer.IsPointerOverUi(screenX, screenY)) return;
            _inspection.SelectCell(_pointer.TryPickCell(screenX, screenY, out CellCoord cell) ? cell : (CellCoord?)null);
            _unitHudLayer.SetSelectedEntity(_selection.SelectedEntityId);
            // 정보창의 UI 콜백에서 씬 연결이 해제될 수 있습니다.
            if (_isReleased) return;
            RenderSelectedActor();
        }

        // 해제 뒤에는 Raycast·좌표 변환을 하지 않고 false를 돌려준다(D9).
        public bool TryPickCell(float screenX, float screenY, out CellCoord cell)
        {
            cell = default;
            return !_isReleased && _pointer.TryPickCell(screenX, screenY, out cell);
        }

        public bool IsPointerOverUi(float screenX, float screenY) => !_isReleased && _pointer.IsPointerOverUi(screenX, screenY);

        public bool TryMapPointer(Vector2 screen, out Vector2 grid, out bool overCancelArea)
        {
            grid = default;
            overCancelArea = false;
            return !_isReleased && _pointer.TryMapPointer(screen, out grid, out overCancelArea);
        }

        public bool IsCursorSnapEnabled => !_isReleased && _cursorSnapSettings != null && _cursorSnapSettings.SnapCursorToGrid;

        public float CursorSnapThreshold => _cursorSnapSettings != null ? _cursorSnapSettings.CursorSnapThreshold : 0f;

        public bool TryCellCenterToScreen(CellCoord cell, out Vector2 screen)
        {
            screen = default;
            return !_isReleased && _pointer.TryCellCenterToScreen(cell, out screen);
        }

        public bool TryWarpCursor(Vector2 screen) => !_isReleased && _pointer.TryWarpCursor(screen);

        #endregion

        #region 입력 중 표시 의도 (ICombatInteractionFeedback)

        public void ShowGhost(EntityKey entityKey)
        {
            if (_isReleased)
                return;

            _placement.ShowGhost(entityKey);
        }

        public void MoveGhostToCell(CellCoord cell)
        {
            if (!_isReleased)
                _placement.MoveGhostToCell(cell);
        }

        public void MoveGhostToPointer(float screenX, float screenY)
        {
            if (!_isReleased && _pointer.TryRayToStandingPoint(screenX, screenY, out Vector3 point))
                _placement.MoveGhostToWorld(point);
        }

        public void HideGhost()
        {
            if (_isReleased)
                return;

            _placement.HideGhost();
        }

        // 끄는 아이템 그림은 가방이 그린다(가방 설계 P4).
        public void ShowItemDrag(int itemKey)
        {
            if (!_isReleased) _pack.ShowItemDrag(itemKey);
        }

        public void MoveItemDrag(float screenX, float screenY)
        {
            if (!_isReleased) _pack.MoveItemDrag(screenX, screenY);
        }

        public void HideItemDrag()
        {
            if (!_isReleased) _pack.HideItemDrag();
        }

        // HUD 아이콘 우클릭 해제는 유닛 표시가 HUD에 연결한다(가방 설계 P6). 풀기(null)는 해제 뒤에도 받는다.
        public void BindItemUnequip(Action<EntityId> unequip)
        {
            if (!_isReleased || unequip == null) _units.BindItemUnequip(unequip);
        }

        public void HideOriginal(EntityId entityId)
        {
            if (_isReleased)
                return;

            _placement.HideOriginal(entityId);
        }

        public void ShowOriginal()
        {
            if (_isReleased)
                return;

            _placement.ShowOriginal();
        }

        public void SetGhostPlaceable(bool placeable)
        {
            if (!_isReleased)
                _placement.SetGhostPlaceable(placeable);
        }

        public void SetHoverCell(CellCoord cell, CombatHoverState state, CombatHoverKind kind)
        {
            if (!_isReleased)
                _field.SetHoverCell(cell, state, kind);
        }

        public void ClearHoverCell()
        {
            if (!_isReleased)
                _field.ClearHoverCell();
        }

        public void ShowRangeCells(IReadOnlyList<CellCoord> cells, IReadOnlyList<CellCoord> basicCells = null)
        {
            if (!_isReleased)
                _attack.ShowRangeCells(cells, basicCells);
        }

        public void ClearRangeCells()
        {
            if (!_isReleased)
                _attack.ClearRangeCells();
        }

        public void ShowHoverRange(IReadOnlyList<CellCoord> attackCells, IReadOnlyList<CellCoord> moveCells, CombatRangeTone tone, EntityId source = default)
        {
            if (!_isReleased)
            {
                CharacterData character = source.IsEmpty ? null : CombatCharacterQueries.Find(_displayedCharacters, source);
                if (tone != CombatRangeTone.Hazard && character == null)
                    throw new InvalidOperationException($"[CombatSceneDisplay] 호버 범위 원점 유닛 {source}이 완료 표시 목록에 없습니다.");
                _attack.ShowHoverRange(attackCells, moveCells, tone, character?.OccupiedCells ?? Array.Empty<CellCoord>());
            }
        }

        public void ClearHoverRange()
        {
            if (!_isReleased)
                _attack.ClearHoverRange();
        }

        public void ShowGimmickRange(IReadOnlyList<CellCoord> cells)
        {
            if (!_isReleased)
                _field.SetLayerCells(FieldHighlightLayer.GimmickHoverRange, cells);
        }

        public void ClearGimmickRange()
        {
            if (!_isReleased)
                _field.ClearLayer(FieldHighlightLayer.GimmickHoverRange);
        }

        public void ShowAffectedCells(IReadOnlyList<CellCoord> cells, AttackPullSnapshot pull = null, bool closeOutline = false)
        {
            if (!_isReleased)
                _attack.ShowAffectedCells(cells, pull, closeOutline);
        }

        public void ClearAffectedCells()
        {
            if (!_isReleased)
                _attack.ClearAffectedCells();
        }

        public void ShowAttackHealthPreview(ReadOnlyMemory<AttackTargetPreview> targets)
        {
            if (!_isReleased)
                _attack.ShowAttackHealthPreview(targets);
        }

        public void ClearAttackHealthPreview()
        {
            if (!_isReleased)
                _attack.ClearAttackHealthPreview();
        }

        public void ShowAttackBlockers(IReadOnlyList<AttackBlockedContact> contacts)
        {
            if (_isReleased) return;
            _attack.ShowAttackBlockers(contacts);
        }

        public void ClearAttackBlockers()
        {
            if (_isReleased) return;
            _attack.ClearAttackBlockers();
        }

        #endregion

        #region 공격 궤적 (ICombatAttackTrajectoryFeedback, 공격 궤적 설계 TJ2)

        public void ShowAttackTrajectory(EntityId attackerId, in AttackPreview preview)
        {
            if (_isReleased) return;
            _attack.ShowAttackTrajectory(attackerId, preview);
        }

        public void ShowBlockedAttackTrajectory(EntityId attackerId, CellCoord target, IReadOnlyList<AttackBlockedContact> contacts)
        {
            if (_isReleased) return;
            _attack.ShowBlockedAttackTrajectory(attackerId, target, contacts);
        }

        public void ClearAttackTrajectory()
        {
            if (_isReleased) return;
            _attack.ClearAttackTrajectory();
        }

        #endregion

        #region 넉백 미리보기 (ICombatKnockbackPreviewFeedback, 넉백 미리보기 설계 KB3)

        public void ShowKnockbackPreview(in AttackPreview preview)
        {
            if (_isReleased) return;
            _attack.ShowKnockbackPreview(preview);
        }

        public void ClearKnockbackPreview()
        {
            if (_isReleased) return;
            _attack.ClearKnockbackPreview();
        }

        #endregion

        #region 공격 모드 대상 (ICombatAttackModeTargets, 이펙트 끝 규칙 설계 EL3 · 마무리 사격 FS-b)

        public void ShowAttackModeTargets(EntityId actor, ReadOnlyMemory<EntityId> targets)
        {
            if (_isReleased) return;
            _events.ShowAttackModeTargets(actor, targets);
        }

        public void ClearAttackModeTargets()
        {
            if (_isReleased) return;
            _events.ClearAttackModeTargets();
        }

        #endregion

        #region 이동 초안·범위·편집 (ICombatMovementFeedback)

        public void ShowDraft(MovementPath path, MovePreview? preview, bool pointerValid, CellCoord? candidate = null)
        {
            // 해제 뒤 늦게 도착한 표시 호출은 무시한다(다른 표시 의도 메서드와 같은 계약).
            if (_isReleased)
                return;

            _movementPlanning.ShowDraft(path, preview, pointerValid, candidate);
        }

        public void ClearDraft()
        {
            if (_isReleased)
                return;

            _movementPlanning.ClearDraft();
        }

        public void SetPathPlanning(bool planning)
        {
            if (!_isReleased)
                _movementPlanning.SetPathPlanning(planning);
        }

        public void ShowMovementRange(MovementRangeResult range)
        {
            if (!_isReleased)
                _movementPlanning.ShowRange(range);
        }

        public void ClearMovementRange()
        {
            if (!_isReleased)
                _movementPlanning.ClearRange();
        }

        public void BeginMovementPlanning(EntityId actor, EntityKey key, Rotation4 rotation)
        {
            if (!_isReleased) _movementPlanning.BeginEditing(actor, key, rotation);
        }

        public void UpdateMovementPointer(Vector2? screenPosition, bool? valid, bool frozen = false)
        {
            if (_isReleased) return;
            // 이동 고스트는 포인터가 가리키는 칸에 설 자리를 따라갑니다(솟은 칸은 윗면, 들어간 칸은 타일 윗면, 고지대 View 계획 P14·타일 높이 계획 §8.7 L6).
            Vector3? point = !frozen && screenPosition.HasValue && _pointer.TryRayToStandingPoint(screenPosition.Value.x,
                screenPosition.Value.y, out Vector3 world) ? world : (Vector3?)null;
            _movementPlanning.UpdatePointer(point, !valid.HasValue ? CombatHoverState.Neutral
                : valid.Value ? CombatHoverState.Valid : CombatHoverState.Invalid, frozen);
        }

        public void EndMovementPlanning()
        {
            if (!_isReleased)
            {
                _movementPlanning.EndEditing();
            }
        }

        #endregion

        /// <summary>
        /// 순차 연출이 쓰는 재생 계약입니다. 위임하지 않고 재생 조정을 그대로 넘깁니다(연출 조립 설계 §9 I-4, 결정 Q-R16 A).
        /// 해제 뒤 사건 시작 거절(D9)은 재생 조정이 합니다.
        /// </summary>
        public ICombatEventPlayback EventPlayback => _eventPlayback;

        // 정리 경로는 씬이 내려가며 이미 파괴된 Unity 객체를 만날 수 있어 Unity의 null 비교를 남깁니다(생성자에서는 필수).
        private void ClearUnitInfo()
        {
            if (_unitInfo != null) _unitInfo.Clear();
        }

        private void ReleaseUnitHudLayer()
        {
            if (_unitHudLayer == null) return;
            _unitHudLayer.Release();
            if (Application.isPlaying) UnityEngine.Object.Destroy(_unitHudLayer.gameObject);
            else UnityEngine.Object.DestroyImmediate(_unitHudLayer.gameObject);
        }

        private void ThrowIfReleased()
        {
            if (_isReleased)
                throw new InvalidOperationException("[CombatSceneDisplay] 씬 연결이 해제된 표시입니다.");
        }

        /// <summary>정리 묶음 뒤에 정리를 더한 목록을 만듭니다. 묶음을 중첩 실행하지 않아 실패가 한 겹으로 모입니다.</summary>
        private static Action[] Combine(Action[] group, params Action[] more)
        {
            var all = new Action[group.Length + more.Length];
            group.CopyTo(all, 0);
            more.CopyTo(all, group.Length);
            return all;
        }

        /// <summary>정리 목록 뒤에 다른 묶음과 정리를 차례로 더한 목록을 만듭니다. 묶음을 목록 가운데 끼울 때 씁니다(기믹 묶음, 평가 C2-01).</summary>
        private static Action[] Combine(Action[] group, Action[] next, params Action[] more)
        {
            var all = new Action[group.Length + next.Length + more.Length];
            group.CopyTo(all, 0);
            next.CopyTo(all, group.Length);
            more.CopyTo(all, group.Length + next.Length);
            return all;
        }

        /// <summary>하나가 실패해도 나머지를 실행하고 실패를 모아 던집니다(공용 규칙 <see cref="CleanupErrors"/>).</summary>
        private static void RunAll(params Action[] actions) =>
            CleanupErrors.RunAll("[CombatSceneDisplay] 표시 정리 중 실패했습니다.", actions);
    }
}
