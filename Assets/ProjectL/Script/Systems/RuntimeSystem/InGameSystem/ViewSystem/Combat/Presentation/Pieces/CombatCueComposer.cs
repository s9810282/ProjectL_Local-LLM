using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Effects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 큐가 내는 창구 묶음입니다(범용 연출 구조 설계 G6). 넘기지 않은 창구의 큐는 항목을 넣지 않습니다(사실 사건·낙석·특성 발동 큐는 소리 창구 없이 만듦 — 그 소리는 사건 소리 표가 냄).
    /// 유닛 소리(공격·피격·사망)는 큐가 아니라 그 유닛 외형의 소리 줄이라 이 묶음이 줄을 들고 옵니다(유닛 소리 설계 US2, 계획 TP-8).
    /// 공격 조립(클립 근접·클립 원거리·코드 모션)은 공격 시작이 <see cref="ForAttack"/> 하나로 만든 묶음을 받습니다(평가 C4-08 2단계, 작은 묶음 설계 SB1).
    /// 원거리 클립의 탄별 시점은 창구가 아니라 조립 하나의 시점 사실이라 기준 시점 표(<see cref="CombatCueAnchors.ProjectileTimings"/>)에 둡니다.
    /// </summary>
    internal sealed class CombatCueSinks
    {
        /// <param name="effects">이펙트 창구입니다. 없으면 이펙트 큐를 넣지 않습니다.</param>
        /// <param name="camera">맵 카메라 창구입니다. 없거나 흔들림을 낼 수 없으면(수신기 없음) 흔들림 큐를 넣지 않습니다.</param>
        /// <param name="sound">소리 창구입니다. 없으면 소리 항목(유닛 소리 줄·투사체)을 넣지 않습니다.</param>
        /// <param name="unitSounds">낼 유닛 소리 줄입니다(<see cref="CombatEventAudio.UnitSoundsOf"/>·<see cref="CombatEventAudio.HitSoundsOf"/>). 없으면(null) 유닛 소리 항목을 넣지 않습니다.</param>
        /// <param name="flashUnit">플래시할 유닛(피격 주체)입니다. 플래시가 없는 칸이면 비어 있습니다.</param>
        /// <param name="canFlash">그 유닛 표시가 플래시 외형 층을 쓸 수 있는지입니다(<c>CombatUnitPresentationWindow.CanFlash</c>).</param>
        /// <param name="projectileSounds">투사체 소리입니다(원거리 공격 칸만). 없으면 투사체 소리를 넣지 않습니다.</param>
        /// <param name="actionGroup">행동 끝 큐의 발동 묶음입니다(특성 발동 큐만). 없으면 행동 끝 이펙트는 결과 끝에 거둡니다(이펙트 끝 규칙 계획 TP-E4).</param>
        internal CombatCueSinks(CombatEffectWindow effects, CombatCameraWindow camera, CombatEventAudio sound, CombatUnitSoundSet unitSounds,
            EntityId flashUnit = default, bool canFlash = false, CombatProjectileSounds projectileSounds = null, CombatEffectActionGroup actionGroup = null)
        {
            ActionGroup = actionGroup;
            Effects = effects;
            Camera = camera;
            Sound = sound;
            UnitSounds = unitSounds;
            FlashUnit = flashUnit;
            CanFlash = canFlash;
            ProjectileSounds = projectileSounds;
        }

        /// <summary>
        /// 공격 조립이 받는 창구 묶음입니다(평가 C4-08 2단계, 작은 묶음 설계 SB1). 인자는 모두 필수라, 공격용 창구를 더하면 이 입구와 부르는 곳(공격 시작)이 컴파일에서 걸립니다.
        /// </summary>
        /// <param name="effects">이펙트 창구입니다.</param>
        /// <param name="camera">맵 카메라 창구입니다. 흔들림을 낼 수 없으면 흔들림 큐를 넣지 않습니다.</param>
        /// <param name="sound">소리 창구입니다. 공격은 유닛 소리 줄·투사체 소리를 내므로 없으면 예외입니다(옛 공격 조립 입구 셋의 검사를 옮김, 설계 P1).</param>
        /// <param name="unitSounds">공격자의 공격 소리 줄입니다(공격 모양의 출처 사건으로 고르고 그 공격 표시 종류에 맞춘 것, 유닛 소리 계획 TP-4·TP-7). 없으면 null입니다.</param>
        /// <param name="projectileSounds">투사체 소리입니다. 근접 클립은 넣지 않으므로 null입니다.</param>
        internal static CombatCueSinks ForAttack(CombatEffectWindow effects, CombatCameraWindow camera, CombatEventAudio sound,
            CombatUnitSoundSet unitSounds, CombatProjectileSounds projectileSounds)
        {
            if (sound == null) throw new ArgumentNullException(nameof(sound));
            return new CombatCueSinks(effects: effects, camera: camera, sound: sound, unitSounds: unitSounds, projectileSounds: projectileSounds);
        }

        internal CombatEffectWindow Effects { get; }
        internal CombatCameraWindow Camera { get; }
        internal CombatEventAudio Sound { get; }

        /// <summary>낼 유닛 소리 줄입니다. 없으면 null입니다.</summary>
        internal CombatUnitSoundSet UnitSounds { get; }
        internal EntityId FlashUnit { get; }
        internal bool CanFlash { get; }
        internal CombatProjectileSounds ProjectileSounds { get; }
        internal CombatEffectActionGroup ActionGroup { get; }
    }

    /// <summary>
    /// 사건 칸의 큐를 조립 항목으로 붙이는 한 곳입니다(범용 연출 구조 설계 G6, 계획 Gd.5·P18). 큐 하나 = 항목 하나이고, 조각은 지금 것을 그대로 씁니다.
    /// 처리기는 뼈대 조립 → 기준 시점 표 채우기 → 이 함수 한 줄만 합니다. "값이 있으면 항목" 갈래는 모두 여기 있습니다.
    /// </summary>
    /// <remarks>
    /// 붙이는 순서는 소리 → 플래시 → 흔들림 → 이펙트(각 목록 순서)입니다. 옛 조립의 같은 순간 항목 순서(사망: 반응 → 소리 → 흔들림 → 이펙트)와 같습니다.
    /// 소리 안에서는 유닛 소리 줄(공격·피격·사망, 유닛 소리 설계 US1) → 투사체 소리(비행·착탄)입니다(투사체·피격 소리 계획 Pb.4).
    /// 기다리는 큐는 플래시뿐입니다(설계 G2). 접촉(Every) 이펙트는 조립 항목이 아니라 공격 재생의 접촉 입구가 냅니다(계획 P5·Gd.7).
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
            if (sinks.Sound != null && sinks.UnitSounds != null)
            {
                // 유닛 소리 줄은 무엇(칸·번호)과 언제(시점·지연)를 함께 듭니다(유닛 소리 설계 US1). 단서는 조각이 시작하는 순간의 보인 사실로 정합니다(사망은 아직 반영 전일 때만).
                CombatEventAudio unitSound = sinks.Sound;
                CombatUnitSoundSet set = sinks.UnitSounds;
                for (int i = 0; i < set.Rows.Count; i++)
                {
                    CombatUnitSoundCue row = set.Rows[i];
                    // 효과음 칸이 연결 대기(피격)면 항목을 넣지 않습니다(옛 피격 소리 목록과 같음).
                    if (CombatEventSounds.UnitSound(set.Slot, row.Sound).Kind == CombatSoundCueKind.None)
                        continue;
                    if (anchors.TryStart(row.Anchor, row.ContactFilter, row.Delay, out start))
                        composition.Add(new CombatEventSoundPiece(unitSound, () => unitSound.CueOf(set, row)), start);
                }
            }
            if (sinks.Sound != null)
                CombatProjectileCueComposer.Add(composition, anchors, sinks.Sound, sinks.ProjectileSounds);

            // 플래시는 시간이 0이면 없고, 표시가 외형 층을 쓸 수 있을 때만 그 층을 빌려 완료를 기다립니다(결정 Q-B5 A·Q-B7 A).
            if (cues.Flash != null && cues.Flash.IsOn && sinks.CanFlash && anchors.TryStart(cues.Flash.Anchor, CombatContactFilter.Every, cues.Flash.Delay, out start))
            {
                composition.Lease(sinks.FlashUnit, CombatPieceChannel.Flash);
                composition.Add(new CombatFlashPiece(sinks.FlashUnit, cues.Flash.Look, CombatFlashPulses.Single(cues.Flash.Seconds)), start, waits: true);
            }

            // 흔들림은 세기가 0보다 크고 흔들림을 낼 수 있을 때만입니다. 같은 순간 여러 흔들림은 창구의 겹침 규칙이 하나로 모읍니다(카메라 연출 조각 설계 K8).
            bool canShake = sinks.Camera != null && sinks.Camera.CanShake;
            for (int i = 0; i < cues.Shakes.Count; i++)
            {
                CombatShakeCue shake = cues.Shakes[i];
                if (!shake.IsOn || !canShake || !anchors.TryStart(shake.Anchor, shake.ContactFilter, shake.Delay, out start))
                    continue;
                composition.Add(new CombatCameraShakePiece(sinks.Camera,
                    CombatCameraMotionSettings.Shake(shake.Strength, shake.Seconds, shake.Shape)), start);
            }

            if (sinks.Effects == null)
                return;
            for (int i = 0; i < cues.Effects.Count; i++)
            {
                CombatEffectCue effect = cues.Effects[i];
                if (!anchors.TryStart(effect.Anchor, effect.ContactFilter, effect.Delay, out start))
                    continue;
                // 자리를 찾지 못했으면 0개, 여럿(특성 발동의 대상·칸)이면 자리마다 하나입니다(특성 발동 연출 설계 TA4).
                int count = points.CountOf(effect);
                for (int p = 0; p < count; p++)
                    composition.Add(points.CreatePiece(sinks.Effects, effect, p, sinks.ActionGroup), start);
            }
        }
    }
}
