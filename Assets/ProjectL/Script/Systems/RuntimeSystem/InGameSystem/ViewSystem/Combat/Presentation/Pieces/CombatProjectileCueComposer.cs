using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>한 탄의 소리를 연결할 발사·접촉 시점입니다. 별도 시각이나 접촉 상태를 소유하지 않습니다.</summary>
    internal readonly struct CombatProjectileCueTiming
    {
        internal CombatProjectileCueTiming(CombatPieceStart flight, CombatPieceStart contact)
        {
            Flight = flight;
            Contact = contact;
        }

        internal CombatPieceStart Flight { get; }
        internal CombatPieceStart Contact { get; }
    }

    /// <summary>클립의 탄별 시점과 코드 모션의 기존 시점에 같은 투사체 소리 프리셋·지연을 붙입니다.</summary>
    internal static class CombatProjectileCueComposer
    {
        // 호출 조립이 인자와 시점을 검사합니다. 소리가 없는 프리셋은 시점 조회도 하지 않습니다.
        internal static void Add(CombatComposition composition, CombatCueAnchors anchors, CombatEventAudio sound,
            CombatProjectileSounds settings, IReadOnlyList<CombatProjectileCueTiming> timings)
        {
            if (settings == null) return;
            AddSound(composition, anchors, sound, CombatEventSounds.ProjectileFlight(settings.FlightPreset), settings.FlightDelay,
                timings, flight: true);
            AddSound(composition, anchors, sound, CombatEventSounds.ProjectileImpact(settings.ImpactPreset), settings.ImpactDelay,
                timings, flight: false);
        }

        private static void AddSound(CombatComposition composition, CombatCueAnchors anchors, CombatEventAudio sound, CombatSoundCue cue,
            float delay, IReadOnlyList<CombatProjectileCueTiming> timings, bool flight)
        {
            if (cue.Kind == CombatSoundCueKind.None) return;
            if (timings == null)
            {
                if (anchors.TryStart(flight ? CombatCueAnchor.Cue : CombatCueAnchor.Hit, delay, out CombatPieceStart start))
                    composition.Add(new CombatEventSoundPiece(sound, () => cue), start);
                return;
            }
            for (int i = 0; i < timings.Count; i++)
            {
                CombatPieceStart start = flight ? timings[i].Flight : timings[i].Contact;
                CombatPieceStart delayed = start.Kind == CombatPieceStartKind.AtStart
                    ? CombatPieceStart.AtStart(start.Delay + delay)
                    : CombatPieceStart.After(start.Item, start.Signal, start.Delay + delay);
                composition.Add(new CombatEventSoundPiece(sound, () => cue), delayed);
            }
        }
    }
}
