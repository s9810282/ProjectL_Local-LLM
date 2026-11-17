#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.EffectSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Environment;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace ProjectL.Systems.Factory.Runtime.InGame.View
{
    /// <summary>입력·게임 진행 UI 없이 운영 필드 표시와 사건 재생을 조립합니다.</summary>
    internal static class CombatTraitPreviewFactory
    {
        /// <summary>기존 HUD의 포인터 설명 입력만 연결합니다. 전투 명령은 생성하지 않습니다.</summary>
        internal static GameObject CreatePointerInput(Transform parent)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));
            if (EventSystem.current != null)
                return null;
            // InputSystemUIInputModule은 OnEnable에서 기본 UI 액션을 연결하고 OnDisable에서 해제합니다.
            var root = new GameObject("TraitPreviewUIInput", typeof(EventSystem), typeof(InputSystemUIInputModule));
            root.transform.SetParent(parent, false);
            return root;
        }

        internal static CombatTraitPreviewDisplay Create(FieldView field, FieldUnitPresenter units, Camera output, Transform parent, CombatDisplayTables tables, CombatDisplayServices services, StageViewPropPresenter backdrop, EffectScope scope, CombatSceneCameraBinding sceneCamera)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));
            if (units == null)
                throw new ArgumentNullException(nameof(units));
            string missing = field.FindMissingReference() ?? units.FindMissingReference();
            if (missing != null)
                throw new InvalidOperationException($"특성 확인 표시 참조가 없습니다: {missing}");
            var result = new CombatTraitPreviewDisplay();
            try
            {
                // 템플릿은 그리지 않습니다. 실행마다 새 표시를 만들어 프리팹 풀·외형 캐시까지 최신 값으로 연결합니다.
                field = UnityEngine.Object.Instantiate(field, parent);
                FieldView ownedField = field;
                result.Own(() => UnityEngine.Object.Destroy(ownedField.gameObject));
                result.Own(field.Release);
                units = UnityEngine.Object.Instantiate(units, parent);
                FieldUnitPresenter ownedUnits = units;
                result.Own(() => UnityEngine.Object.Destroy(ownedUnits.gameObject));
                tables.PresentationSettings.Validate();
                CombatCameraSession cameraSession = CombatCameraFactory.Create(sceneCamera, services.CameraCatalog, services.CameraEffects,
                    CombatCameraFactory.GroundHeightOf(field));
                result.Own(cameraSession.Release);
                CombatCameraWindow camera = cameraSession.Window;
                var objects = new FieldObjectPresenter(field.CellsRoot, tables.Objects, tables.Environments);
                result.Own(objects.Release);
                var terrain = new FieldTerrainPresenter(field.CellsRoot, tables.Terrains, tables.Environments, CombatTileCollapseSettings.From(tables.PresentationSettings));
                result.Own(terrain.Release);
                var trails = new FieldTrailPresenter(field.CellsRoot, tables.Traits);
                result.Own(trails.Release);
                // 사건 재생의 필수 창구입니다. 미리보기는 토네이도를 그리지 않아 표시는 비어 있습니다(토네이도 View 계획 TP-9).
                var tornado = new FieldTornadoPresenter(field.CellsRoot, tables.Terrains, services.Meshes, services.Colors);
                result.Own(tornado.Release);
                // 낙석도 같습니다. 미리보기는 그리지 않아 표시는 비어 있습니다.
                var rockfall = new FieldRockfallPresenter(field, tables.Rockfall, backdrop.RockfallTunnel, backdrop.RockfallRidgeStart, backdrop.RockfallRidgeEnd,
                    backdrop.RockfallTunnelExplosionEnabled);
                result.Own(rockfall.Release);
                var railway = new FieldRailwayPresenter(field.CellsRoot, tables.Railway, services.Effects);
                result.Own(railway.Release);
                var layer = CombatUnitHUDFactory.Create(parent, output);
                result.Own(() =>
                {
                    layer.Release();
                    UnityEngine.Object.Destroy(layer.gameObject);
                });
                layer.AttachUISystem(services.UI);
                units.ConfigureMovementAppearance(services.Colors);
                units.Configure(tables.Units, tables.Field.FieldUnitScale, tables.UnitStatuses, tables.Traits, layer, tables.Items);
                result.Own(units.ReleaseHudLayer);
                result.Own(units.Release);
                field.Build(8, 8, tables.Field, services.Colors, services.AreaOutlines, services.Meshes);
                field.SetGroundBase(backdrop.TileSurface);
                terrain.BindTiles(backdrop.TileSurface);
                railway.Install(field.Mapper, Array.Empty<CellCoord>());
                var effects = new CombatEffectWindow(services.Effects, field.CellsRoot,
                    id => units.TrackSurfacePresence(id, field.Mapper), scope, position => field.Mapper.OnSurface(position));
                result.Own(effects.Release);
                // 하붑도 같습니다. 구름은 이펙트라 창구를 먼저 만듭니다.
                var haboob = new FieldHaboobPresenter(tables.Haboob, effects);
                result.Own(haboob.Release);
                // 필드 창구는 운영 표시와 같은 함수로 만듭니다(평가 C4-02, 결정 Q-N2 A). 토네이도 외형 연출(진행·재생 중·막음·취소)도 운영과 같습니다.
                CombatFieldEventPorts ports = CombatSceneDisplayFactory.CreateFieldPorts(field, objects, terrain, trails, tornado, rockfall, haboob);
                CombatEventPlaybackComposition composition = CombatEventPlaybackFactory.Create(field, units, CombatEventPresentationValues.From(tables.PresentationSettings), services.Audio, tables.SoundPresets, tables.CueSources, camera, effects, ports, railway, output, tables.PresentationSettings);
                result.Events = composition.Events;
                result.Playback = composition.Playback;
                result.Own(composition.Events.Release);
                result.Own(composition.Playback.CancelPendingEvents);
                composition.Events.Bind(field.Mapper);
                cameraSession.Begin();
                return result;
            }
            catch
            {
                result.Dispose();
                throw;
            }
        }
    }

    /// <summary>테스트 표시 한 실행의 자원을 역순으로 반환합니다. 게임 상태는 소유하지 않습니다.</summary>
    internal sealed class CombatTraitPreviewDisplay : IDisposable
    {
        private readonly List<Action> _cleanup = new();
        internal CombatSceneEventPresenter Events { get; set; }
        internal CombatSceneEventPlayback Playback { get; set; }

        internal void Own(Action release) => _cleanup.Add(release);
        public void Dispose()
        {
            Playback?.MarkReleased();
            List<Exception> errors = null;
            for (int i = _cleanup.Count - 1; i >= 0; --i)
                CleanupErrors.Try(_cleanup[i], ref errors);
            _cleanup.Clear();
            if (errors != null)
                throw new AggregateException("특성 확인 표시 해제 실패", errors);
        }
    }
}
#endif
