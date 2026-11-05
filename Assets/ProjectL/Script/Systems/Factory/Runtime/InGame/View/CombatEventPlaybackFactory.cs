using System;
using UnityEngine;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Text;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.ScreenTransitionSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Environment;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement;

namespace ProjectL.Systems.Factory.Runtime.InGame.View
{
    /// <summary>운영 전투와 연출 확인 환경의 사건 재생을 같은 경로로 조립합니다.</summary>
    internal static class CombatEventPlaybackFactory
    {
        internal static CombatEventPlaybackComposition Create(FieldView field, FieldUnitPresenter units, CombatEventPresentationValues values, IAudioSystemService audio, CombatSoundPresets sounds, CombatCueSources cues, CombatCameraWindow camera, CombatEffectWindow effects, CombatFieldEventPorts ports, FieldRailwayPresenter railway, FieldThornPresenter thorns, IScreenTransitionSystemService transitions, CombatPhaseTransitionSettings transitionSettings, Camera output, CombatPresentationSettingsData settings)
        {
            CombatBodyTextFactory.ValidatePrefabs(cues, settings.BodyTextPrefab);
            CombatBodyTextResources textResources = CombatBodyTextFactory.CreateResources(field.gameObject.scene, output);
            CombatBodyTextWindow bodyText = null;
            CombatSceneEventPresenter events = null;
            try
            {
                bodyText = new CombatBodyTextWindow(textResources, output, settings.BodyTextPrefab, settings.BodyTextStackGap, units.TrackBody);
                events = new CombatSceneEventPresenter(units, values, audio, sounds, cues, camera, effects, field.CellsRoot, ports, bodyText, CombatTraitFeedbackFactory.CreateEvidence());
                var transition = new CombatPhaseTransitionPresenter(transitions, transitionSettings, events.BeginEvent);
                var playback = new CombatSceneEventPlayback(events, railway, field, thorns, (Func<CharacterCombatFacts, FieldGridMapper, CombatBody>)units.BodyOf, transition, camera, values.CameraMotion);
                return new CombatEventPlaybackComposition(events, playback);
            }
            catch
            {
                try
                {
                    events?.Release();
                }
                finally
                {
                    try
                    {
                        bodyText?.Release();
                    }
                    finally
                    {
                        textResources.Dispose();
                    }
                }

                throw;
            }
        }
    }

    /// <summary>생성한 객체의 참조입니다. 연결·해제 수명은 호출자가 소유합니다.</summary>
    internal sealed class CombatEventPlaybackComposition
    {
        internal CombatEventPlaybackComposition(CombatSceneEventPresenter events, CombatSceneEventPlayback playback)
        {
            Events = events;
            Playback = playback;
        }

        internal CombatSceneEventPresenter Events { get; }
        internal CombatSceneEventPlayback Playback { get; }
    }
}
