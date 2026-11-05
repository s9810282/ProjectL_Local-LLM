using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Effects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 큐가 내는 창구 묶음입니다(범용 연출 구조 설계 G6). 없는 창구(시험 조립)의 큐는 항목을 넣지 않습니다.
    /// </summary>
    internal sealed class CombatCueSinks
    {
        /// <param name="effects">이펙트 창구입니다. 없으면 이펙트 큐를 넣지 않습니다.</param>
        /// <param name="camera">맵 카메라 창구입니다. 없거나 흔들림을 낼 수 없으면(수신기 없음) 흔들림 큐를 넣지 않습니다.</param>
        /// <param name="sound">소리 창구입니다. 없으면 소리 큐(시점·목록·투사체)를 넣지 않습니다.</param>
        /// <param name="soundEvent">소리 시점 큐에서 소리 표가 무엇을 낼지 정할 사건입니다. 없으면 소리 시점 큐를 넣지 않습니다.</param>
        /// <param name="flashUnit">플래시할 유닛(피격 주체)입니다. 플래시가 없는 칸이면 비어 있습니다.</param>
        /// <param name="canFlash">그 유닛 표시가 플래시 외형 층을 쓸 수 있는지입니다(<c>CombatUnitPresentationWindow.CanFlash</c>).</param>
        /// <param name="projectileSounds">투사체 소리입니다(원거리 공격 칸만). 없으면 투사체 소리를 넣지 않습니다.</param>
        /// <param name="actionGroup">행동 끝 큐의 발동 묶음입니다(특성 발동 큐만). 없으면 행동 끝 이펙트는 결과 끝에 거둡니다(이펙트 끝 규칙 계획 TP-E4).</param>
        /// <param name="projectileTimings">원거리 클립의 탄별 발사·접촉 시점입니다. 없으면 코드 모션의 기존 Cue·Hit을 씁니다.</param>
        internal CombatCueSinks(CombatEffectWindow effects, CombatCameraWindow camera, CombatEventAudio sound, CombatEvent soundEvent,
            EntityId flashUnit = default, bool canFlash = false, CombatProjectileSounds projectileSounds = null, CombatEffectActionGroup actionGroup = null,
            IReadOnlyList<CombatProjectileCueTiming> projectileTimings = null)
        {
            ActionGroup = actionGroup;
            Effects = effects;
            Camera = camera;
            Sound = sound;
            SoundEvent = soundEvent;
            FlashUnit = flashUnit;
            CanFlash = canFlash;
            ProjectileSounds = projectileSounds;
            ProjectileTimings = projectileTimings;
        }

        internal CombatEffectWindow Effects { get; }
        internal CombatCameraWindow Camera { get; }
        internal CombatEventAudio Sound { get; }
        internal CombatEvent SoundEvent { get; }
        internal EntityId FlashUnit { get; }
        internal bool CanFlash { get; }
        internal CombatProjectileSounds ProjectileSounds { get; }
        internal IReadOnlyList<CombatProjectileCueTiming> ProjectileTimings { get; }
        internal CombatEffectActionGroup ActionGroup { get; }
    }

    /// <summary>
    /// 사건 칸의 큐를 조립 항목으로 붙이는 한 곳입니다(범용 연출 구조 설계 G6, 계획 Gd.5·P18). 큐 하나 = 항목 하나이고, 조각은 지금 것을 그대로 씁니다.
    /// 처리기는 뼈대 조립 → 기준 시점 표 채우기 → 이 함수 한 줄만 합니다. "값이 있으면 항목" 갈래는 모두 여기 있습니다.
    /// </summary>
    /// <remarks>
    /// 붙이는 순서는 소리 → 플래시 → 흔들림 → 이펙트(각 목록 순서)입니다. 옛 조립의 같은 순간 항목 순서(사망: 반응 → 소리 → 흔들림 → 이펙트)와 같습니다.
    /// 소리 안에서는 소리 시점(사건 소리) → 프리셋 소리 목록(피격) → 투사체 소리(비행·착탄)입니다(투사체·피격 소리 계획 Pb.4).
    /// 기다리는 큐는 플래시뿐입니다(설계 G2). 접촉 시점 이펙트는 조립 항목이 아니라 공격 재생의 접촉 입구가 냅니다(계획 P5·Gd.7).
    /// </remarks>
    internal static class CombatCueComposer
    {
        internal static void Add(CombatComposition composition, CombatCueAnchors anchors, CombatEventCues cues, CombatCuePoints points,
            CombatCueSinks sinks)
        {
            if (composition == null) throw new ArgumentNullException(nameof(composition));
            if (anchors == null) throw new ArgumentNullException(nameof(anchors));
            if (cues == null) throw new ArgumentNullException(nameof(cues));
            if (sinks == null) throw new ArgumentNullException(nameof(sinks));

            CombatPieceStart start;
            // 소리 시점은 무엇을 낼지 정하지 않고 언제 낼지만 정합니다. 낼지는 소리 표·프리셋이 정합니다(계획 P15).
            if (sinks.Sound != null && sinks.SoundEvent != null)
            {
                // 사건 소리는 조각이 시작하는 순간의 보인 사실로 고릅니다(소리 표).
                CombatEventAudio eventSound = sinks.Sound;
                CombatEvent soundEvent = sinks.SoundEvent;
                for (int i = 0; i < cues.SoundTimings.Count; i++)
                {
                    CombatSoundTimingCue timing = cues.SoundTimings[i];
                    if (anchors.TryStart(timing.Anchor, timing.Delay, out start))
                        composition.Add(new CombatEventSoundPiece(eventSound, () => eventSound.CueOf(soundEvent)), start);
                }
            }
            if (sinks.Sound != null)
            {
                // 프리셋 소리 목록(피격 칸)은 항목이 무엇을 낼지 직접 듭니다(결정 Q-PS2·Q-PS3). 자리는 지금 소리 위치에 쓰지 않습니다(계획 P3).
                for (int i = 0; i < cues.PresetSounds.Count; i++)
                {
                    CombatPresetSoundCue sound = cues.PresetSounds[i];
                    AddSound(composition, anchors, sinks.Sound, CombatEventSounds.UnitHit(sound.Preset), sound.Anchor, sound.Delay);
                }
                CombatProjectileCueComposer.Add(composition, anchors, sinks.Sound, sinks.ProjectileSounds, sinks.ProjectileTimings);
            }

            // 플래시는 시간이 0이면 없고, 표시가 외형 층을 쓸 수 있을 때만 그 층을 빌려 완료를 기다립니다(결정 Q-B5 A·Q-B7 A).
            if (cues.Flash != null && cues.Flash.IsOn && sinks.CanFlash && anchors.TryStart(cues.Flash.Anchor, cues.Flash.Delay, out start))
            {
                composition.Lease(sinks.FlashUnit, CombatPieceChannel.Flash);
                composition.Add(new CombatFlashPiece(sinks.FlashUnit, cues.Flash.Color, cues.Flash.Seconds), start, waits: true);
            }

            // 흔들림은 세기가 0보다 크고 흔들림을 낼 수 있을 때만입니다. 같은 순간 여러 흔들림은 창구의 겹침 규칙이 하나로 모읍니다(카메라 연출 조각 설계 K8).
            bool canShake = sinks.Camera != null && sinks.Camera.CanShake;
            for (int i = 0; i < cues.Shakes.Count; i++)
            {
                CombatShakeCue shake = cues.Shakes[i];
                if (!shake.IsOn || !canShake || !anchors.TryStart(shake.Anchor, shake.Delay, out start))
                    continue;
                composition.Add(new CombatCameraShakePiece(sinks.Camera,
                    CombatCameraMotionSettings.Shake(shake.Strength, shake.Seconds, shake.Shape)), start);
            }

            if (sinks.Effects == null)
                return;
            for (int i = 0; i < cues.Effects.Count; i++)
            {
                CombatEffectCue effect = cues.Effects[i];
                if (!anchors.TryStart(effect.Anchor, effect.Delay, out start))
                    continue;
                // 자리를 찾지 못했으면 0개, 여럿(특성 발동의 대상·칸)이면 자리마다 하나입니다(특성 발동 연출 설계 TA4).
                int count = points.CountOf(effect);
                for (int p = 0; p < count; p++)
                    composition.Add(points.CreatePiece(sinks.Effects, effect, p, sinks.ActionGroup), start);
            }
        }

        // 소리 표가 고른 단서를 그 시점에 냅니다. 소리 없음(프리셋 없음·슬롯 연결 대기, 계획 P9)이면 항목을 넣지 않습니다.
        private static void AddSound(CombatComposition composition, CombatCueAnchors anchors, CombatEventAudio sound, CombatSoundCue cue,
            CombatCueAnchor anchor, float delay)
        {
            if (cue.Kind == CombatSoundCueKind.None || !anchors.TryStart(anchor, delay, out CombatPieceStart start))
                return;
            composition.Add(new CombatEventSoundPiece(sound, () => cue), start);
        }
    }
}
