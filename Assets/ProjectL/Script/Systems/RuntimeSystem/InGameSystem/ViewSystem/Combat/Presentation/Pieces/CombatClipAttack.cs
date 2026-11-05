using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Effects;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 클립 공격 조립입니다(클립 설계 AC7·E3, 5차 AC15, 계획 T23·T31). 포효와 같은 모양이라(행동 실행 + 공격 모양의 출처 사건이 출처, 모양의 완료 정책 — 공격 사건은 첫 해제) Gate 정책·겹침 규칙을 그대로 씁니다.
    /// 입력은 공격 사건이 아니라 공격 모양입니다(특성 발동 연출 설계 TA5).
    /// 입구는 근접(<see cref="ComposeMelee"/>)과 원거리(<see cref="ComposeRanged"/>) 둘이고(§11 K3), 어느 쪽인지는 처리기가
    /// <see cref="CombatUnitAnimationRules.FiresProjectile"/>로 고릅니다.
    /// </summary>
    /// <remarks>
    /// <para>뼈대 항목(설계 AC7·AC15 표 요약):</para>
    /// <list type="number">
    /// <item>0 방향 — 공격자를 목표 쪽으로 돌림(<see cref="CombatFaceTargetPiece"/>, 실행 시작).</item>
    /// <item>클립 — 공격 클립 재생(<see cref="CombatClipPiece"/>, 실행 시작). 시작·존재하는 번호별 이벤트 신호를 냅니다.</item>
    /// <item>근접 = 첫 이벤트 + 피격 선딜레이에 해제 → Hit(<see cref="CombatReleasePiece"/>).
    /// 원거리 = 집계 준비 뒤 클립, 이벤트별 포구 발사, 모든 접촉 뒤 해제 → Hit → 집계 끝(<see cref="CombatClipRangedAttack"/>).</item>
    /// </list>
    /// <para>
    /// 그 뒤 공격 칸의 큐(소리 시점·흔들림·이펙트)를 기준 시점 표로 붙입니다: Cue = 첫 클립 이벤트, 원거리 Cue2·3 = 해당 선택 이벤트,
    /// 맞음 = 해제/집계의 <see cref="CombatPieceSignal.Hit"/>,
    /// 끝 = 클립 끝. 옛 "소리 = 이벤트 타이밍 + 소리 선딜레이"·"이펙트 = 이벤트 타이밍 + 이펙트 선딜레이, 이펙트 지점"은 공격 큐의 소리 시점 Cue·이펙트 큐(Cue·ActorEffectPoint)입니다.
    /// 사건 완료는 첫 해제(Gate), 전체 종료는 실행 끝(클립 끝 이벤트, 원거리는 투사체 도착까지, 큐 지연이 그보다 길면 그 큐까지)입니다.
    /// 포효가 나중에 이 항목 묶음을 쓸 수 있게 공개 범위는 <c>internal static</c>입니다.
    /// </para>
    /// <para>앞 항목(<see cref="CombatAttackLead"/>, 대신 맞는 수호자 나가기)이 있으면 그 항목이 맨 앞에 오고, 방향·클립·시작 큐는 그 항목이 돌려준 시점에 시작하며,
    /// 맞는 순간 뒤 항목(대기·돌아오기)이 끝에 붙습니다(유닛 행동 설계 AA6). 그때 위 번호는 앞 항목 수만큼 밀립니다.</para>
    /// <para>창구·자리·앞 항목 인자는 기본값 없이 모두 받습니다. 창구를 더할 때 빠뜨린 입구가 컴파일에서 걸리게 하려는 것이고, 없는 값은 null로 넘깁니다(평가 C4-08 1단계).</para>
    /// </remarks>
    internal static class CombatClipAttack
    {
        /// <summary>근접 클립 공격 조립입니다. 항목 2는 해제 조각(이벤트 타이밍 + 피격 선딜레이)입니다.</summary>
        /// <param name="cues">공격 칸 큐입니다(공격 표시 값의 <c>Cues</c>).</param>
        /// <param name="facing">빌린 자세(지금 위치·방향)에서 공격자가 볼 방향입니다(<see cref="CombatAttackFacing.For"/>).</param>
        /// <param name="release">출처의 대상을 모두 해제하는 입구입니다(<see cref="ICombatSourceLifetime.ReleaseAll"/>).</param>
        /// <param name="effects">이펙트 창구입니다. 없으면 이펙트 큐를 넣지 않습니다(계획 P7).</param>
        /// <param name="camera">맵 카메라 창구입니다. 없으면 흔들림 큐를 넣지 않습니다.</param>
        /// <param name="actorBody">행위자 몸 자리입니다(큐 자리 <c>ActorBody</c>). 없으면 그 자리의 이펙트를 넣지 않습니다.</param>
        /// <param name="lead">공격 조립의 앞 항목입니다(대신 맞는 수호자 나가기·돌아오기, 유닛 행동 계획 TP-14). 없으면 조립이 지금과 같습니다.</param>
        internal static CombatComposition ComposeMelee(CombatAttackShape shape, CombatClipAttackData clip, CombatEventCues cues,
            Func<ICombatUnitPose, Quaternion> facing, Action release, CombatEventAudio sound, CombatEffectWindow effects,
            CombatCameraWindow camera, Pose? actorBody, CombatAttackLead lead)
        {
            if (shape == null) throw new ArgumentNullException(nameof(shape));
            if (clip == null) throw new ArgumentNullException(nameof(clip));
            if (cues == null) throw new ArgumentNullException(nameof(cues));
            if (facing == null) throw new ArgumentNullException(nameof(facing));
            if (release == null) throw new ArgumentNullException(nameof(release));
            if (sound == null) throw new ArgumentNullException(nameof(sound));
            CombatComposition composition = BeginClip(shape, facing, lead, out CombatPieceStart start);
            int played = composition.Add(new CombatClipPiece(shape.AttackerId, clip.Clip), start);
            int hit = composition.Add(new CombatReleasePiece(release), CombatPieceStart.After(played, CombatPieceSignal.ClipEvent, clip.HitDelay));
            AddCues(composition, shape, cues, AnchorsOf(start, played, hit), sound, effects, camera, actorBody,
                projectileSounds: null, projectileTimings: null);
            lead?.AfterHit(composition, hit);
            return composition;
        }

        /// <summary>
        /// 원거리(직선·곡사) 클립 공격 조립입니다. 탄별 조립이 집계를 먼저 준비하고 클립과 번호별 발사를 연결합니다.
        /// </summary>
        /// <param name="cues">공격 칸 큐입니다(공격 표시 값의 <c>Cues</c>). 접촉(Contact) 이펙트는 조립 항목이 아니라 발사의 맞음에서 공격 재생이 냅니다.</param>
        /// <param name="facing">빌린 자세(지금 위치·방향)에서 공격자가 볼 방향입니다(<see cref="CombatAttackFacing.For"/>).</param>
        /// <param name="fire">발사 입구입니다. 인자는 (발사 지점, 맞음 콜백, 도착 콜백)입니다(<see cref="CombatAttackPlayback.FireShot"/>).</param>
        /// <param name="release">출처의 대상을 모두 해제하는 입구입니다(<see cref="ICombatSourceLifetime.ReleaseAll"/>).</param>
        /// <param name="effects">이펙트 창구입니다. 없으면 이펙트 큐를 넣지 않습니다(계획 P7).</param>
        /// <param name="camera">맵 카메라 창구입니다. 없으면 흔들림 큐를 넣지 않습니다.</param>
        /// <param name="actorBody">행위자 몸 자리입니다(큐 자리 <c>ActorBody</c>). 없으면 그 자리의 이펙트를 넣지 않습니다.</param>
        /// <param name="projectileSounds">투사체 소리입니다. 각 탄의 발사·접촉에 지연을 더합니다. 없으면 넣지 않습니다.</param>
        /// <param name="lead">공격 조립의 앞 항목입니다(대신 맞는 수호자 나가기·돌아오기, 유닛 행동 계획 TP-14). 없으면 조립이 지금과 같습니다.</param>
        internal static CombatComposition ComposeRanged(CombatAttackShape shape, CombatClipAttackData clip, CombatEventCues cues,
            Func<ICombatUnitPose, Quaternion> facing, Func<Vector3, Action, Action, ICombatShot> fire, Action release, CombatEventAudio sound,
            CombatEffectWindow effects, CombatCameraWindow camera, Pose? actorBody,
            CombatProjectileSounds projectileSounds, CombatAttackLead lead)
        {
            if (shape == null) throw new ArgumentNullException(nameof(shape));
            if (clip == null) throw new ArgumentNullException(nameof(clip));
            if (cues == null) throw new ArgumentNullException(nameof(cues));
            if (facing == null) throw new ArgumentNullException(nameof(facing));
            if (fire == null) throw new ArgumentNullException(nameof(fire));
            if (release == null) throw new ArgumentNullException(nameof(release));
            if (sound == null) throw new ArgumentNullException(nameof(sound));
            CombatComposition composition = BeginClip(shape, facing, lead, out CombatPieceStart start);
            CombatClipRangedAttack.Add(composition, shape.AttackerId, clip, start, fire, release, out int played, out int hit,
                out IReadOnlyList<CombatProjectileCueTiming> projectileTimings);
            CombatCueAnchors anchors = AnchorsOf(start, played, hit);
            CombatClipRangedAttack.ConnectEventCues(anchors, clip.Clip, played);
            AddCues(composition, shape, cues, anchors, sound, effects, camera, actorBody, projectileSounds, projectileTimings);
            lead?.AfterHit(composition, hit);
            return composition;
        }

        // 두 입구의 공통 앞부분입니다: 출처·대여·가로채기 앞 항목·방향. 클립 앞의 집계 유무는 각 기능 조립이 정합니다.
        private static CombatComposition BeginClip(CombatAttackShape shape, Func<ICombatUnitPose, Quaternion> facing,
            CombatAttackLead lead, out CombatPieceStart start)
        {
            EntityId attacker = shape.AttackerId;
            var composition = CombatComposition.ForAction(
                    CombatPieceCompletion.FromSource(shape.Source, attacker, shape.Completion))
                .Lease(attacker, CombatPieceChannel.Pose)
                .Lease(attacker, CombatPieceChannel.Clip);
            start = lead?.Begin(composition) ?? CombatPieceStart.AtStart();
            composition.Add(new CombatFaceTargetPiece(attacker, facing), start);
            return composition;
        }

        // 공통 기준 순간 = 첫 클립 이벤트, 맞음 = 해제/집계의 Hit, 끝 = 클립 끝입니다.
        private static CombatCueAnchors AnchorsOf(CombatPieceStart start, int played, int hit) =>
            new CombatCueAnchors()
                .StartAt(start)
                .Set(CombatCueAnchor.Cue, played, CombatPieceSignal.ClipEvent)
                .Set(CombatCueAnchor.Hit, hit, CombatPieceSignal.Hit)
                .Set(CombatCueAnchor.End, played, CombatPieceSignal.End);

        // 행위자 지점은 빌린 클립 채널에서 읽습니다. 원거리 조립만 선택 이벤트·탄별 소리 시점을 더합니다.
        private static void AddCues(CombatComposition composition, CombatAttackShape shape, CombatEventCues cues, CombatCueAnchors anchors,
            CombatEventAudio sound, CombatEffectWindow effects, CombatCameraWindow camera, Pose? actorBody, CombatProjectileSounds projectileSounds,
            IReadOnlyList<CombatProjectileCueTiming> projectileTimings)
        {
            CombatCueComposer.Add(composition, anchors, cues, CombatCuePoints.ForActor(shape.AttackerId, actorBody),
                new CombatCueSinks(effects, camera, sound, shape.Source, projectileSounds: projectileSounds, projectileTimings: projectileTimings));
        }
    }
}
