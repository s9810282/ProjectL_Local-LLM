using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
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
    /// <summary>진입 보드·선로·보드 위 구조물·Object·Nexus·적 조립입니다. 시간 진행과 표시 상태를 보관하지 않습니다.</summary>
    internal static class CombatBoardEntryFactory
    {
        internal static CombatBoardEntryPresentation Create(FieldView field, FieldUnitPresenter units,
            FieldObjectPresenter objects, FieldTerrainPresenter terrain, StageViewPropPresenter backdrop, CombatPresentationSettingsData settings,
            CombatCameraWindow camera, CombatEffectWindow effects, CombatEntryDeferredDisplay deferredDisplay,
            FieldRailwayPresenter railway, IAudioSystemService audio)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (railway == null) throw new ArgumentNullException(nameof(railway));
            // 보드 위 구조물과 첫 적은 같은 소환 상승 설정으로 올라옵니다(평가 C10-06).
            CombatAbilityMotionSettings rise = CombatAbilityMotionSettings.From(settings);
            return new CombatBoardEntryPresentation(backdrop?.PlacedTiles ?? Array.Empty<Transform>(),
                backdrop?.BackdropRevealTargets ?? Array.Empty<CombatBoardRevealTarget>(), field, terrain, units, objects,
                settings.BoardReveal, new CombatBoardReveal(field.CellsRoot, settings.BoardReveal),
                new CombatEntryRailwayAppearance(railway.SegmentTracks, field.CellsRoot, settings.RailwayDrop, audio),
                new CombatEntryRiseAppearance(units.Presentation, camera, rise, "구조물"),
                new CombatEntryNexusAppearance(units.Presentation, effects, settings.NexusDrop,
                    new CombatEntryGroundCompression(settings.NexusDrop), audio),
                new CombatEntryRiseAppearance(units.Presentation, camera, rise, "적"),
                new CombatEntryEnemyRows(settings.EnemyEntryRowGapSeconds), deferredDisplay, audio);
        }
    }
}
