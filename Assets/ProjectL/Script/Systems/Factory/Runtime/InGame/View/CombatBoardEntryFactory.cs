using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Entry;
using UnityEngine;

namespace ProjectL.Systems.Factory.Runtime.InGame.View
{
    /// <summary>진입 보드·선로·Nexus·적 조립입니다. 시간 진행과 표시 상태를 보관하지 않습니다.</summary>
    internal static class CombatBoardEntryFactory
    {
        internal static CombatBoardEntryPresentation Create(FieldView field, FieldUnitPresenter units,
            FieldTerrainPresenter terrain, StageViewPropPresenter backdrop, CombatPresentationSettingsData settings,
            CombatCameraWindow camera, CombatEffectWindow effects, CombatEntryDeferredDisplay deferredDisplay,
            FieldRailwayPresenter railway)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (railway == null) throw new ArgumentNullException(nameof(railway));
            return new CombatBoardEntryPresentation(backdrop?.PlacedTiles ?? Array.Empty<Transform>(),
                backdrop?.BackdropRevealTargets ?? Array.Empty<CombatBoardRevealTarget>(), field, terrain, units,
                settings.BoardReveal, new CombatBoardReveal(field.CellsRoot, settings.BoardReveal),
                new CombatEntryRailwayAppearance(railway.SegmentTracks, field.CellsRoot, settings.RailwayDrop),
                new CombatEntryNexusAppearance(units.Presentation, effects, settings.NexusDrop),
                new CombatEntryEnemyAppearance(units.Presentation, camera, CombatAbilityMotionSettings.From(settings)), deferredDisplay);
        }
    }
}
