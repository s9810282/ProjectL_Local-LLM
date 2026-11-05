using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageField.Item;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.MeshSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.AreaOutlineSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Feedback;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Hud;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Turn;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Entry;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Environment;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement;
using UnityEngine;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Selection;

namespace ProjectL.Systems.Factory.Runtime.InGame.View
{
    /// <summary>Combat 씬의 표시 객체를 생성·설정하고 같은 협력 객체를 연결합니다(조립 분리 SD-D1).</summary>
    internal static class CombatSceneDisplayFactory
    {
        /// <summary>
        /// 씬 로드 때(Combat Enter <c>SceneNodeOrder.CombatBackdrop</c>, 카메라 다시 연결보다 먼저) 세울 배경 맵의 주인을 만듭니다(배경 설계 BD1·BD3).
        /// 부모는 필드 칸 루트입니다. 필드 구성은 이 Transform을 옮기지 않으므로 필드보다 먼저 세워도 위치가 같습니다.
        /// 씬 전체 참조 검사는 표시를 만들 때(<see cref="Create"/>) 합니다.
        /// </summary>
        internal static StageViewPropPresenter CreateBackdrop(CombatSceneRoot root, CombatDisplayTables tables)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            if (tables == null)
                throw new ArgumentNullException(nameof(tables));
            if (root.Field == null)
                throw new InvalidOperationException("[CombatSceneDisplayFactory] 배경을 세울 필드가 씬 루트에 연결되지 않았습니다.");
            // 필드 설정은 바닥 타일을 칸 경계에 맞출 칸 간격을 읽는 데 씁니다(타일 맞춤 계획 b.7).
            return new StageViewPropPresenter(tables.StageProps, root.Field.CellsRoot, tables.Field);
        }

        /// <summary>씬 연결 한 번에 사용할 표시를 조립합니다. 설치·재설치·해제 순서는 Display가 정합니다.</summary>
        /// <remarks>
        /// 공유 씬 객체를 설정하므로 호출자가 활성 전투·중복 연결을 먼저 검사해야 합니다.
        /// 이 Factory 안의 부분 Configure 실패는 복구하지 않습니다. 생성 뒤 ViewSystem이 인수하기 전의 실패는
        /// <c>CombatViewSystem.AttachCreated</c>가 표시를 반환합니다. 연출 시간 값은 View DB 설정 검사로 생성 전에 거절합니다(구조 개선 SV1, 연출 조립 설계 §9 I-5).
        /// </remarks>
        /// <param name="root">참조와 실행 중 커서 설정을 제공하는 씬 루트입니다.</param>
        /// <param name="tables">개별 표의 null 검사를 마친 View DB 조회 묶음입니다.</param>
        /// <param name="services">Render·Audio·UI 서비스 묶음입니다(카메라 효과는 Render 쪽). 서비스는 모두 필수입니다(View 2라운드 W6-7).</param>
        /// <param name="backdrop">
        /// 이미 세운 배경 맵입니다. 그 맵 카메라 사실을 카메라 창구에 넘기고, 아침 화면 전환 카메라 이름이 그 안에 있는지 봅니다.
        /// 바닥 타일 높이 표도 표시에 넘깁니다(타일 높이 계획 P10). 없으면(표시 시험) 맵 카메라가 없는 창구이고 바닥 타일 높이도 없습니다.
        /// </param>
        internal static CombatSceneDisplay Create(CombatSceneRoot root, CombatDisplayTables tables, CombatDisplayServices services,
            StageViewPropPresenter backdrop = null)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            if (tables == null)
                throw new ArgumentNullException(nameof(tables));
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            StageFieldViewData fieldConfig = tables.Field;
            CombatPresentationSettingsData presentation = tables.PresentationSettings;
            IColorSystemService colors = services.Colors;
            IAreaOutlineSystemService areaOutlines = services.AreaOutlines;
            IMeshSystemService meshes = services.Meshes;
            IAudioSystemService audio = services.Audio;

            // 연출 값은 표가 만들 때 검사했지만, 씬 객체를 Configure하거나 소유 GameObject를 만들기 전에 한 번 더 거절한다(구조 개선 SV1).
            presentation.Validate();
            // 아침 화면 전환 설정과 맵 카메라 존재를 씬 객체를 건드리기 전에 검사합니다. 자동 전환은 현재 비활성이지만 Day 카메라 요구는 유지합니다.
            CombatPhaseTransitionSettings transitionSettings = CombatPhaseTransitionSettings.From(presentation);
            if (backdrop != null)
            {
                RequireMapCamera(backdrop.MapCameras, presentation.MorningTransitionCamera);
            }
            root.ValidateReferences();
            if (root.TurnCost != null)
                services.UI.CloseUI(ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.UIId.TurnCost);
            var turnDisplay = new CombatTurnDisplayPresenter(root.TurnDisplay);
            root.Completion.Configure(tables.Units);
            var selection = new CombatSelectionState();
            root.UnitInfo.Configure(tables.Units, tables.Traits, services.UI, tables.Categories, selection, tables.UnitStatuses);
            FieldView field = root.Field;
            FieldUnitPresenter units = root.FieldUnits;
            CombatHud hud = root.Hud;
            PlacementGhost ghost = root.Ghost;
            // 입력 피드백은 유닛 HUD 일시 피드백과 표시 창구만 받습니다(연출 조립 설계 §9 I-2, B13). 둘은 units가 한 번 만들어 소유합니다.
            var movementPlanning = new CombatMovementPlanningPresenter(field, units.Presentation, ghost, fieldConfig);
            var placement = new CombatPlacementFeedback(field, ghost, units);
            // 궤적 경로 구성은 필드(캔버스)·표시 창구(외형·무기)·유닛 위치 읽기만 받습니다(공격 궤적 설계 TJ2, 계획 P4·P7).
            var attack = new CombatAttackFeedback(field, units.HudFeedback, new CombatAttackTrajectory(field, units.Presentation, units));
            CombatActionPanel actions = root.ActionPanel;
            // 가방은 재고 스냅숏을 받아 그리기만 한다. 외형 표는 씬 연결 때 한 번 넘긴다(가방 설계 P1·P3).
            // 위 씬 루트 참조 검사가 가방·정보창 연결을 보장한다(View 단계 설계 W4).
            ICombatPackDisplay pack = root.Pack;
            pack.ConfigureItems(tables.Items);
            var pointer = new CombatPointerMapper(field, root.WorldCamera, root.InnerCellInset);
            // 스냅 켜짐·감도는 실행 중 Inspector 변경을 읽도록 값 대신 루트 참조를 전달합니다.
            CombatSceneRoot cursorSnapSettings = root;

            units.ConfigureMovementAppearance(colors);
            // 카메라 창구 하나를 사건 Presenter(조각 실행이 줌을 빌림)와 기차 재생이 함께 씁니다(카메라 연출 조각 설계 K1).
            // 맵 카메라 사실은 씬 로드 때 먼저 세운 배경이 읽어 둔 것을 여기서 넘기고, 표시 해제가 비웁니다(배경 설계 BD3·BD4).
            // 흔들림 겹침 시계는 Cinemachine Impulse와 같은 Unity 시간입니다(설계 K8).
            var camera = new CombatCameraWindow(services.CameraEffects, () => Time.time);
            if (backdrop != null)
                camera.AttachMap(backdrop.MapCameras);
            // 플레이어 카메라 조작 시점입니다(카메라 조작 설계 CC1, 계획 P8). 같은 맵 카메라와 배경이 읽은 조작 값을 받고, 배경이 없으면 조작이 없습니다.
            // 연결 때 처음 위치·크기로 맞추고(CC6·P4), 표시 해제가 되돌립니다.
            var cameraView = backdrop != null
                ? new CombatCameraView(camera, backdrop.MapCameras, backdrop.CameraSettings)
                : new CombatCameraView(camera, Array.Empty<CombatMapCamera>(), null);
            // 사건 Presenter의 연출 값 묶음입니다(View 3라운드 W9-3). 포효 줌·흔들림, 피격·사망·기차 흔들림 값(카메라 연출 조각 설계 K5·K6·K8·K9)도 여기 있고,
            // 기차 재생이 같은 카메라 연출 값을 씁니다. 원본은 View DB 설정 표입니다.
            CombatEventPresentationValues eventValues = CombatEventPresentationValues.From(presentation);
            var objects = new FieldObjectPresenter(field.CellsRoot, tables.Objects, tables.Environments);
            var terrain = new FieldTerrainPresenter(field.CellsRoot, tables.Terrains, tables.Environments,
                CombatTileCollapseSettings.From(presentation));
            var railway = new FieldRailwayPresenter(field.CellsRoot, tables.Railway, services.Effects);
            // 클립 공격의 이벤트 타이밍 이펙트 창구입니다(이펙트 설계 EF1·EF6). 필드 루트에 놓고, 몸 자리 큐는 유닛 몸 위치를 따라갑니다(이펙트 따라가기 설계 FE2). 사건 Presenter가 회수·해제합니다.
            var effects = new CombatEffectWindow(services.Effects, field.CellsRoot, units.TrackBodyPresence);
            ghost.Configure(tables.Units, fieldConfig, colors);

            // 연출 시간 값(이동 step 포함)은 위 설정 검사가 이미 거절했으므로 가시밭 소유 GameObject를 먼저 만들어
            // 같은 표시를 사건 Presenter·재생·Display가 공유합니다(구조 개선 SV1·E17). 프리팹은 DB 표가 필수로 검사합니다(View 3라운드 W12-5).
            var thorns = new FieldThornPresenter(tables.ThornField.Prefab, field.CellsRoot);
            // 특성 흔적 지형(땅 다지기·먼지 연막) 표시입니다. 외형은 특성 외형 표의 흔적 칸입니다(특성 시스템 View 설계 TS7).
            var trails = new FieldTrailPresenter(field.CellsRoot, tables.Traits);
            // 움직이는 토네이도 표시입니다(토네이도 View 설계 TN3). 외형은 지형 외형 표의 Tornado 줄입니다.
            var tornado = new FieldTornadoPresenter(field.CellsRoot, tables.Terrains, meshes, colors);
            // 낙석 표시입니다(낙석 View 설계 RF2). 외형은 낙석 표현 표, 터널 폭발 자리와 돌 출발 능선은 맵 프랍 안의 오브젝트입니다.
            var rockfall = new FieldRockfallPresenter(field, tables.Rockfall, backdrop?.RockfallTunnel, backdrop?.RockfallRidgeStart, backdrop?.RockfallRidgeEnd,
                backdrop?.RockfallTunnelExplosionEnabled ?? true);
            // 하붑 구름 표시입니다(하붑 View 계획 HP-17). 칸마다 연기 이펙트를 같은 창구로 냅니다.
            var haboob = new FieldHaboobPresenter(tables.Haboob, effects);
            // 필드 표시 창구입니다(View 3라운드 W9-3). 특성 미리보기와 같은 함수로 만듭니다(평가 C4-02).
            CombatFieldEventPorts fieldPorts = CreateFieldPorts(field, thorns, objects, terrain, trails, tornado, rockfall, haboob);
            CombatEventPlaybackComposition eventComposition = CombatEventPlaybackFactory.Create(field, units, eventValues,
                audio, tables.SoundPresets, tables.CueSources, camera, effects, fieldPorts, railway, thorns,
                services.ScreenTransitions, transitionSettings,
                root.WorldCamera != null ? root.WorldCamera : Camera.main, presentation);
            CombatSceneEventPresenter events = eventComposition.Events;
            selection.Bind(events.InspectionFacts);
            CombatSceneEventPlayback eventPlayback = eventComposition.Playback;
            // 출력 Camera의 기존 MainCamera 대체 정책은 조립 경계에서 한 번만 적용합니다.
            UnitHUDLayer unitHudLayer = CombatUnitHUDFactory.Create(root.transform,
                root.WorldCamera != null ? root.WorldCamera : Camera.main);
            try
            {
                unitHudLayer.AttachUISystem(services.UI);
                events.BindStructureDamage(CombatStructureDamageFactory.Create(units.DamageTemplate, unitHudLayer, units.DamageScreenScale));
                units.ConfigureHUDCategories(tables.Categories);
                units.Configure(tables.Units, fieldConfig.FieldUnitScale, tables.UnitStatuses, tables.Traits, unitHudLayer, tables.Items);
                // 자세 연결은 나머지 조립 뒤 시작하고, 아래 실패 정리에서 반드시 복원합니다.
                cameraView.Begin();
                // 바닥 타일 높이 표는 씬 로드 때 배경이 맞춤 뒤 만든 것입니다(타일 높이 설계 TH2). 타일 방식이 아니면 null이고 표시가 바탕을 0으로 둡니다(계획 P10).
                var inspection = new CombatInspectionPresenter(selection, events.InspectionFacts,
                    new CombatInspectionInfoMapper(tables.Units, tables.Traits, tables.Categories, tables.UnitStatuses), root.UnitInfo, null);
                events.BindInspectionRendered(inspection.Refresh);
                var deferredDisplay = new CombatEntryDeferredDisplay(railway, tornado, rockfall, haboob, events);
                var display = new CombatSceneDisplay(
                    field, movementPlanning, placement, attack, units, hud, ghost, actions, cursorSnapSettings,
                    pointer, fieldConfig, colors, areaOutlines, meshes, events, audio, tables.SoundPresets,
                    camera, cameraView, objects, terrain, railway, thorns, trails, tornado, rockfall, haboob, eventPlayback, root.UnitInfo, unitHudLayer, pack, root.Completion, selection,
                    turnDisplay, backdrop?.TileSurface, inspection, deferredDisplay,
                    CombatBoardEntryFactory.Create(field, units, terrain, backdrop, presentation, camera, effects, deferredDisplay, railway));
                inspection.BindSelectionChanged(display.RefreshInspectionSelection);
                return display;
            }
            catch (Exception error)
            {
                var errors = new List<Exception> { error };
                CleanupErrors.Try(events.Release, ref errors);
                CleanupErrors.Try(cameraView.Restore, ref errors);
                CleanupErrors.Try(unitHudLayer.Release, ref errors);
                CleanupErrors.Try(() =>
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(unitHudLayer.gameObject);
                    else UnityEngine.Object.DestroyImmediate(unitHudLayer.gameObject);
                }, ref errors);
                CleanupErrors.ThrowIfAny(errors, "[CombatSceneDisplayFactory] 표시 조립 실패 및 정리 오류입니다.");
                throw;
            }
        }

        /// <summary>
        /// 사건 Presenter가 필드 표시에 닿는 창구를 만듭니다(View 3라운드 W9-3). 운영 표시와 특성 미리보기가 이 함수 하나로 만들어
        /// 두 조립이 어긋나지 않습니다(평가 C4-02, 결정 Q-N2 A). 걸음 접촉 원본의 몸(이동 접촉 설계 M2)은 가시·오브젝트 표시가 변환 함수로 줍니다.
        /// </summary>
        internal static CombatFieldEventPorts CreateFieldPorts(FieldView field, FieldThornPresenter thorns, FieldObjectPresenter objects,
            FieldTerrainPresenter terrain, FieldTrailPresenter trails, FieldTornadoPresenter tornado, FieldRockfallPresenter rockfall,
            FieldHaboobPresenter haboob)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));
            if (thorns == null)
                throw new ArgumentNullException(nameof(thorns));
            if (objects == null)
                throw new ArgumentNullException(nameof(objects));
            if (terrain == null)
                throw new ArgumentNullException(nameof(terrain));
            if (trails == null)
                throw new ArgumentNullException(nameof(trails));
            if (tornado == null)
                throw new ArgumentNullException(nameof(tornado));
            if (rockfall == null)
                throw new ArgumentNullException(nameof(rockfall));
            if (haboob == null)
                throw new ArgumentNullException(nameof(haboob));

            // 같은 형식의 인자(몸 조회 둘, 숨김 둘)가 이어져 있어 이름으로 넘깁니다(구조 원칙 평가 R5).
            return new CombatFieldEventPorts(
                consumeThorn: thorns.Consume,
                presentHighlight: cells => field.SetLayerCells(FieldHighlightLayer.PresentationHighlight, cells),
                thornBody: thorns.BodyOf,
                objectBody: objects.BodyOf,
                // 발동한 오브젝트는 사건 시점에 숨깁니다(이동 접촉 설계 M6). 결과 끝 Refresh가 최종 사실로 다시 맞춥니다.
                hideObject: objects.HideActivated,
                // 무너진 약한 지반은 사건 시점에 칸을 낭떠러지로 바꿉니다(약한 지반 설계 WG6). 결과 끝 Refresh가 최종 사실로 다시 맞춥니다.
                showTerrainChange: terrain.ShowChanged,
                // 특성 흔적은 사건 시점에 놓거나 지웁니다(특성 시스템 View 설계 TS7). 결과 끝 Refresh가 최종 사실로 다시 맞춥니다.
                showTrail: trails.Show,
                // 필드 외형 연출(토네이도 생성·축소, 약한 지반 무너짐)입니다. 무너짐은 떨리는 동안만 다음 사건을 막습니다(타일 교체 설계 TR8).
                tickAppearance: deltaTime =>
                {
                    terrain.TickAppearance(deltaTime);
                    tornado.TickAppearance(deltaTime);
                },
                appearancePlaying: () => terrain.IsAppearancePlaying() || tornado.IsAppearancePlaying,
                appearanceBlocksStart: () => terrain.IsAppearanceBlockingStart() || tornado.IsAppearancePlaying,
                cancelAppearance: () =>
                {
                    terrain.CancelAppearance();
                    tornado.CancelAppearance();
                },
                tornado: tornado,
                rockfall: rockfall,
                haboob: haboob);
        }

        /// <summary>화면 전환이 옮겨 갈 카메라가 세운 맵 프랍 안에 있는지 봅니다(View 3라운드 W9-1). 이름은 카메라 효과 카탈로그의 열쇠와 같습니다.</summary>
        private static void RequireMapCamera(IReadOnlyList<CombatMapCamera> cameras, string name)
        {
            for (int i = 0; i < cameras.Count; i++)
            {
                if (string.Equals(cameras[i].Name, name, StringComparison.Ordinal))
                    return;
            }
            throw new InvalidOperationException($"[CombatSceneDisplayFactory] 화면 전환 카메라가 맵 프랍에 없습니다: {name}");
        }
    }
}
