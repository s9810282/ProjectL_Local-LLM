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
        /// 아래 catch는 사건 Presenter·공용 카메라 Session·HUD 층을 되돌립니다. 다른 필드 표시 인스턴스와 씬 컴포넌트의 Configure 결과는
        /// 조립 실패가 F1로 멈춘 뒤 씬 언로드가 거둡니다. 모두 Combat 씬 안 객체라 다음 씬에 남지 않습니다(평가 C9-05, 받아들인 한계).
        /// </remarks>
        /// <param name="root">참조와 실행 중 커서 설정을 제공하는 씬 루트입니다.</param>
        /// <param name="tables">개별 표의 null 검사를 마친 View DB 조회 묶음입니다.</param>
        /// <param name="services">Render·Audio·UI 서비스 묶음입니다(카메라 효과는 Render 쪽). 서비스는 모두 필수입니다(View 2라운드 W6-7).</param>
        /// <param name="backdrop">
        /// 이미 세운 배경 맵입니다. 바닥 타일·낙석·등장 연출의 배경 참조만 넘깁니다. null이어도 공용 시점은 씬 바인딩에서 받습니다.
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
            root.ValidateReferences();
            // 카메라 공급원은 배경과 무관한 씬 바인딩입니다. 부분 조립 실패도 같은 Session을 반환합니다.
            CombatCameraSession cameraSession = CombatCameraFactory.Create(root.SceneCamera, services.CameraCatalog, services.CameraEffects,
                CombatCameraFactory.GroundHeightOf(root.Field));
            CombatCameraWindow camera = cameraSession.Window;
            CombatSceneEventPresenter events = null;
            UnitHUDLayer unitHudLayer = null;
            try
            {
                root.FieldUnits.ValidateActionCursorReference();
                if (root.TurnCost != null)
                    services.UI.CloseUI(ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.UIId.TurnCost);
                var turnDisplay = new CombatTurnDisplayPresenter(root.TurnDisplay, audio);
                root.Completion.Configure(tables.Units);
                var selection = new CombatSelectionState();
                root.UnitInfo.Configure(tables.Units, tables.Traits, services.UI, tables.Categories, selection, tables.UnitStatuses);
                FieldView field = root.Field;
                FieldUnitPresenter units = root.FieldUnits;
                CombatHud hud = root.Hud;
                PlacementGhost ghost = root.Ghost;
                // 공유 수명(해제 뒤 규칙 D9의 원본)과 완료 표시 목록은 이 연결의 표시와 하위 객체가 같은 것을 씁니다(씬 표시 나누기 설계 DS1·DS3, 계획 TP-12).
                var lifetime = new CombatDisplayLifetime();
                var snapshot = new CombatDisplaySnapshot();
                // 포인터 매퍼는 이동 초안·고스트가 화면 좌표를 설 자리로 바꾸는 데 써서 먼저 만듭니다(설계 P9).
                // 스냅 켜짐·감도는 실행 중 Inspector 변경을 읽도록 값 대신 루트 참조를 넘깁니다(계획 TP-2).
                var pointer = new CombatPointerMapper(field, root.WorldCamera, root.InnerCellInset, root);
                // 입력 피드백은 유닛 HUD 일시 피드백과 표시 창구만 받습니다(연출 조립 설계 §9 I-2, B13). 둘은 units가 한 번 만들어 소유합니다.
                var movementPlanning = new CombatMovementPlanningPresenter(field, units.Presentation, ghost, fieldConfig, pointer);
                var placement = new CombatPlacementFeedback(field, ghost, units, pointer, lifetime);
                // 궤적 경로 구성은 필드(캔버스)·표시 창구(외형·무기)·유닛 위치 읽기만 받습니다(공격 궤적 설계 TJ2, 계획 P4·P7).
                var attack = new CombatAttackFeedback(field, units.HudFeedback, new CombatAttackTrajectory(field, units.Presentation, units),
                    snapshot, lifetime);
                var actions = new CombatActionButtonsDisplay(root.ActionPanel, lifetime);
                // 가방은 재고 스냅숏을 받아 그리기만 한다. 외형 표는 씬 연결 때 한 번 넘긴다(가방 설계 P1·P3).
                // 위 씬 루트 참조 검사가 가방·정보창 연결을 보장한다(View 단계 설계 W4).
                ICombatPackDisplay pack = root.Pack;
                pack.ConfigureItems(tables.Items);
                var hudSurfaces = new CombatHudSurfaces(hud, turnDisplay, pack, root.UnitInfo, lifetime);
                // 배경음 정책 하나를 표시(국면 배경음)와 결과 화면(승리·완료 뒤)이 함께 씁니다(평가 C2-03, 씬 표시 나누기 설계 DS4).
                var bgm = new CombatBgmPolicy(audio, tables.SoundPresets);
                var completion = new CombatResultScreenDisplay(root.Completion, bgm);

                units.ConfigureMovementAppearance(colors);
                // 사건 Presenter의 연출 값 묶음입니다(View 3라운드 W9-3). 포효 줌·흔들림, 피격·사망·기차 흔들림 값(카메라 연출 조각 설계 K5·K6·K8·K9)도 여기 있고,
                // 기차 재생이 같은 카메라 연출 값을 씁니다. 원본은 View DB 설정 표입니다.
                CombatEventPresentationValues eventValues = CombatEventPresentationValues.From(presentation);
                var objects = new FieldObjectPresenter(field.CellsRoot, tables.Objects, tables.Environments);
                var terrain = new FieldTerrainPresenter(field.CellsRoot, tables.Terrains, tables.Environments,
                    CombatTileCollapseSettings.From(presentation));
                var railway = new FieldRailwayPresenter(field.CellsRoot, tables.Railway, services.Effects);
                // 클립 공격의 이벤트 타이밍 이펙트 창구입니다(이펙트 설계 EF1·EF6). 받침은 필드 표면에 두고, 큐의 실제 효과 시작 자리는 유지합니다(EF1).
                var effects = new CombatEffectWindow(services.Effects, field.CellsRoot,
                    id => units.TrackSurfacePresence(id, field.Mapper), position => field.Mapper.OnSurface(position));
                ghost.Configure(tables.Units, fieldConfig, colors);

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
                CombatFieldEventPorts fieldPorts = CreateFieldPorts(field, objects, terrain, trails, tornado, rockfall, haboob);
                CombatEventPlaybackComposition eventComposition = CombatEventPlaybackFactory.Create(field, units, eventValues,
                    audio, tables.SoundPresets, tables.CueSources, camera, effects, fieldPorts, railway,
                    root.WorldCamera != null ? root.WorldCamera : Camera.main, presentation, turnDisplay.ShowEnemyTurnStart);
                events = eventComposition.Events;
                selection.Bind(events.InspectionFacts);
                CombatSceneEventPlayback eventPlayback = eventComposition.Playback;
                // 출력 Camera의 기존 MainCamera 대체 정책은 조립 경계에서 한 번만 적용합니다.
                unitHudLayer = CombatUnitHUDFactory.Create(root.transform,
                    root.WorldCamera != null ? root.WorldCamera : Camera.main);
                unitHudLayer.AttachUISystem(services.UI);
                events.BindStructureDamage(CombatStructureDamageFactory.Create(units.DamageTemplate, unitHudLayer, units.DamageScreenScale));
                units.Configure(tables.Units, fieldConfig.FieldUnitScale, tables.UnitStatuses, tables.Traits, unitHudLayer, tables.Items);
                // 자세 연결은 나머지 조립 뒤 시작하고, 아래 실패 정리에서 반드시 복원합니다.
                cameraSession.Begin();
                // 선택이 바뀔 때 알림은 표시를 만든 뒤 BindSelectionChanged로 겁니다(생성 인자는 null).
                var inspection = new CombatInspectionPresenter(selection, events.InspectionFacts,
                    new CombatInspectionInfoMapper(tables.Units, tables.Traits, tables.Categories, tables.UnitStatuses), root.UnitInfo, null);
                events.BindInspectionRendered(inspection.Refresh);
                var deferredDisplay = new CombatEntryDeferredDisplay(railway, tornado, rockfall, haboob, events);
                var entry = new CombatEntryPresentation(
                    CombatBoardEntryFactory.Create(field, units, objects, terrain, backdrop, presentation, camera, effects,
                        deferredDisplay, railway, audio),
                    new CombatDeploymentPresentation(services.UI, root.TurnDisplay, root.DeckCards, audio), deferredDisplay);
                // 바닥 타일 높이 표(backdrop?.TileSurface)는 씬 로드 때 배경이 맞춤 뒤 만든 것입니다(타일 높이 설계 TH2). 타일 방식이 아니면 null이고 필드 표시가 바탕을 0으로 둡니다(계획 P10).
                var fieldDisplay = new CombatFieldDisplay(field: field, config: fieldConfig, colors: colors, areaOutlines: areaOutlines,
                    meshes: meshes, tileSurface: backdrop?.TileSurface, objects: objects, terrain: terrain, railway: railway,
                    trails: trails, tornado: tornado, rockfall: rockfall, haboob: haboob, deferredDisplay: deferredDisplay);
                var selectionDisplay = new CombatSelectionDisplay(selection, inspection, unitHudLayer, field, units, events.InspectionFacts,
                    snapshot, pointer, lifetime);
                // 입력 피드백 계약의 주인 일곱을 묶습니다. 어느 주인이 어느 계약을 맡는지는 묶음 생성자 한 곳이 정합니다(씬 표시 나누기 계획 TP-1).
                var feedback = new CombatInputFeedback(pointer, placement, field, attack, events, movementPlanning, hudSurfaces);
                var display = new CombatSceneDisplay(
                    lifetime: lifetime,
                    snapshot: snapshot,
                    feedback: feedback,
                    field: fieldDisplay,
                    selection: selectionDisplay,
                    hud: hudSurfaces,
                    actions: actions,
                    completion: completion,
                    bgm: bgm,
                    units: units,
                    ghost: ghost,
                    unitHudLayer: unitHudLayer,
                    movementPlanning: movementPlanning,
                    events: events,
                    eventPlayback: eventPlayback,
                    pointer: pointer,
                    cameraSession: cameraSession,
                    entryPresentation: entry);
                inspection.BindSelectionChanged(selectionDisplay.RefreshInspectionSelection);
                return display;
            }
            catch (Exception error)
            {
                var errors = new List<Exception> { error };
                CleanupErrors.Try(() => events?.Release(), ref errors);
                CleanupErrors.Try(cameraSession.Release, ref errors);
                CleanupErrors.Try(() => unitHudLayer?.Release(), ref errors);
                CleanupErrors.Try(() =>
                {
                    if (unitHudLayer == null) return;
                    if (Application.isPlaying) UnityEngine.Object.Destroy(unitHudLayer.gameObject);
                    else UnityEngine.Object.DestroyImmediate(unitHudLayer.gameObject);
                }, ref errors);
                CleanupErrors.ThrowIfAny(errors, "[CombatSceneDisplayFactory] 표시 조립 실패 및 정리 오류입니다.");
                throw;
            }
        }

        /// <summary>
        /// 사건 Presenter가 필드 표시에 닿는 창구를 만듭니다(View 3라운드 W9-3). 운영 표시와 특성 미리보기가 이 함수 하나로 만들어
        /// 두 조립이 어긋나지 않습니다. 걸음 중 접촉할 오브젝트의 몸 크기는 오브젝트 표시에서 받습니다.
        /// </summary>
        internal static CombatFieldEventPorts CreateFieldPorts(FieldView field, FieldObjectPresenter objects,
            FieldTerrainPresenter terrain, FieldTrailPresenter trails, FieldTornadoPresenter tornado, FieldRockfallPresenter rockfall,
            FieldHaboobPresenter haboob)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));
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

            // 각 필드 표시의 콜백은 매개변수 이름으로 연결합니다.
            return new CombatFieldEventPorts(
                presentHighlight: cells => field.SetLayerCells(FieldHighlightLayer.PresentationHighlight, cells),
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

    }
}
