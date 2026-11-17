using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack;
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
    /// <item>근접 = 이벤트 줄마다 맞는 순간(이벤트 + 줄 지연). 한 줄이면 해제 → Hit(<see cref="CombatReleasePiece"/>), 여럿이면 접촉 집계 + 줄마다 접촉(<see cref="CombatStrikePiece"/>).
    /// 원거리 = 집계 준비 뒤 클립, 이벤트 줄마다 발사 지점에서 발사, 모든 접촉 뒤 해제 → Hit → 집계 끝(<see cref="CombatClipRangedAttack"/>).</item>
    /// </list>
    /// <para>
    /// 그 뒤 공격 칸의 큐(흔들림·이펙트)와 공격 소리 줄을 기준 시점 표로 붙입니다: Event1~3 = 본 단계 몸 클립에 표식이 있는 클립 이벤트(11-17 사용자, 설계 CS3의 등록 조건을 대신함),
    /// 접촉의 FirstHit = 해제/집계의 <see cref="CombatPieceSignal.Hit"/>(옛 맞음, 설계 CS10),
    /// 끝 = 클립 끝. 이펙트 자리 이벤트 지점은 큐의 번호·회전(ActorEventPoint)입니다.
    /// 사건 완료는 첫 해제(Gate), 전체 종료는 실행 끝(클립 끝 이벤트, 원거리는 투사체 도착까지, 큐 지연이 그보다 길면 그 큐까지)입니다.
    /// 포효가 나중에 이 항목 묶음을 쓸 수 있게 공개 범위는 <c>internal static</c>입니다.
    /// </para>
    /// <para>앞 항목(<see cref="CombatAttackLead"/>, 대신 맞는 수호자 나가기)이 있으면 그 항목이 맨 앞에 오고, 방향·클립·시작 큐는 그 항목이 돌려준 시점에 시작하며,
    /// 맞는 순간 뒤 항목(대기·돌아오기)이 끝에 붙습니다(유닛 행동 설계 AA6). 그때 위 번호는 앞 항목 수만큼 밀립니다.</para>
    /// <para>창구는 묶음 하나(<see cref="CombatCueSinks.ForAttack"/>)로 받고, 자리·앞 항목은 기본값 없이 받습니다(없는 값은 null). 공격용 창구를 더할 때 고칠 곳은 묶음과 공격 시작뿐이고,
    /// 빠뜨리면 컴파일에서 걸립니다(평가 C4-08 2단계, 작은 묶음 설계 SB1). 원거리 클립의 탄별 시점은 기준 시점 표에 넣습니다(<see cref="CombatCueAnchors.SetProjectileTimings"/>).</para>
    /// </remarks>
    internal static class CombatClipAttack
    {
        /// <summary>
        /// 근접 클립 공격 조립입니다. 이벤트 줄마다 맞는 순간(그 이벤트 + 줄 지연)입니다(클립 칸 다시 짜기 설계 CS2, 결정 Q-A3 A).
        /// 줄이 하나면 지금처럼 해제 조각 하나(<see cref="CombatReleasePiece"/>)이고, 여럿이면 연발과 같은 접촉 집계(<see cref="CombatVolleyReleasePiece"/>)에
        /// 줄마다 접촉 하나(<see cref="CombatStrikePiece"/>)를 넣어 앞 줄은 앞 접촉 피격·숫자 나눔, 마지막 줄이 해제입니다(계획 TP-9).
        /// </summary>
        /// <param name="contactImpact">맞는 순간마다 낼 접촉 이펙트(Contact·Every, 대상 몸 자리)입니다. 없으면 null입니다.</param>
        /// <param name="contactFeedback">줄이 여럿일 때 접촉 순번마다 낼 표시(숫자·앞 접촉 피격)입니다. 없으면 null입니다.</param>
        /// <param name="cues">공격 칸 큐입니다(공격 표시 값의 <c>Cues</c>).</param>
        /// <param name="facing">빌린 자세(지금 위치·방향)에서 공격자가 볼 방향입니다(<see cref="CombatAttackFacing.For"/>).</param>
        /// <param name="release">출처의 대상을 모두 해제하는 입구입니다(<see cref="ICombatSourceLifetime.ReleaseAll"/>).</param>
        /// <param name="sinks">공격 창구 묶음입니다(<see cref="CombatCueSinks.ForAttack"/>). 근접 클립은 투사체 소리 없이 만듭니다.</param>
        /// <param name="actorBody">행위자 몸 자리입니다(큐 자리 <c>ActorBody</c>). 없으면 그 자리의 이펙트를 넣지 않습니다.</param>
        /// <param name="lead">공격 조립의 앞 항목입니다(대신 맞는 수호자 나가기·돌아오기, 유닛 행동 계획 TP-14). 없으면 조립이 지금과 같습니다.</param>
        internal static CombatComposition ComposeMelee(CombatAttackShape shape, CombatClipAttackData clip, CombatEventCues cues,
            Func<ICombatUnitPose, Quaternion> facing, Action release, CombatCueSinks sinks, Pose? actorBody, CombatAttackLead lead,
            Func<bool> canRelease = null, Action contactImpact = null, Action<int> contactFeedback = null)
        {
            if (shape == null) throw new ArgumentNullException(nameof(shape));
            if (clip == null) throw new ArgumentNullException(nameof(clip));
            if (cues == null) throw new ArgumentNullException(nameof(cues));
            if (facing == null) throw new ArgumentNullException(nameof(facing));
            if (release == null) throw new ArgumentNullException(nameof(release));
            if (sinks == null) throw new ArgumentNullException(nameof(sinks));
            CombatComposition composition = BeginClip(shape, facing, lead, out CombatPieceStart start);
            IReadOnlyList<CombatClipEventEntry> strikes = clip.Events;
            int hit;
            int played;
            if (strikes.Count == 1)
            {
                played = composition.Add(new CombatClipPiece(shape.AttackerId, clip.Motion), start);
                Action strike = contactImpact == null ? release : () =>
                {
                    contactImpact();
                    release();
                };
                hit = composition.Add(new CombatReleasePiece(strike, canRelease), StrikeStart(played, strikes[0]));
            }
            else
            {
                // 같은 시작 조건의 항목은 목록 순서대로 시작합니다. 집계를 클립보다 먼저 두어 0초 이벤트의 접촉도 집계 준비 뒤에 옵니다(원거리와 같음).
                var contacts = new CombatVolleyReleasePiece(strikes.Count, null, release, canRelease, contactFeedback);
                hit = composition.Add(contacts, start);
                played = composition.Add(new CombatClipPiece(shape.AttackerId, clip.Motion), start);
                for (int i = 0; i < strikes.Count; i++)
                    composition.Add(new CombatStrikePiece(contacts.ContactCallback(i), contactImpact), StrikeStart(played, strikes[i]));
            }
            CombatCueAnchors anchors = AnchorsOf(clip, start, played, hit).SetClipEvents(clip.Motion, played);
            AddCues(composition, shape, cues, anchors, sinks, actorBody);
            lead?.AfterHit(composition, hit);
            return composition;
        }

        /// <summary>
        /// 원거리(직선·곡사) 클립 공격 조립입니다. 탄별 조립이 집계를 먼저 준비하고 클립과 번호별 발사를 연결합니다.
        /// </summary>
        /// <param name="cues">공격 칸 큐입니다(공격 표시 값의 <c>Cues</c>). 접촉(Contact) 이펙트는 조립 항목이 아니라 발사의 맞음에서 공격 재생이 냅니다.</param>
        /// <param name="facing">빌린 자세(지금 위치·방향)에서 공격자가 볼 방향입니다(<see cref="CombatAttackFacing.For"/>).</param>
        /// <param name="fire">발사 입구입니다. 인자는 (발사 지점, 접촉 콜백(대상, 이 탄의 마지막 접촉인지), 도착 콜백)입니다(<see cref="CombatAttackPlayback.FireShot"/>).</param>
        /// <param name="releaseTarget">대상 하나를 해제하는 입구입니다(<see cref="ICombatSourceLifetime.Release"/>). 마지막 탄의 접촉마다 부릅니다(관통·죽음의 손 View 설계 D6).</param>
        /// <param name="release">출처의 남은 대상을 모두 해제하는 입구입니다(<see cref="ICombatSourceLifetime.ReleaseAll"/>). 마지막 탄의 마지막 접촉에서 부릅니다.</param>
        /// <param name="sinks">공격 창구 묶음입니다(<see cref="CombatCueSinks.ForAttack"/>). 투사체 소리는 각 탄의 발사·접촉에 지연을 더해 붙습니다.</param>
        /// <param name="actorBody">행위자 몸 자리입니다(큐 자리 <c>ActorBody</c>). 없으면 그 자리의 이펙트를 넣지 않습니다.</param>
        /// <param name="lead">공격 조립의 앞 항목입니다(대신 맞는 수호자 나가기·돌아오기, 유닛 행동 계획 TP-14). 없으면 조립이 지금과 같습니다.</param>
        internal static CombatComposition ComposeRanged(CombatAttackShape shape, CombatClipAttackData clip, CombatEventCues cues,
            Func<ICombatUnitPose, Quaternion> facing, Func<Vector3, Action<EntityId, bool>, Action, ICombatShot> fire, Action<EntityId> releaseTarget,
            Action release, CombatCueSinks sinks, Pose? actorBody, CombatAttackLead lead, Vector3? launchAimPoint = null,
            Func<bool> canRelease = null, Action<int> contactFeedback = null)
        {
            if (shape == null) throw new ArgumentNullException(nameof(shape));
            if (clip == null) throw new ArgumentNullException(nameof(clip));
            if (cues == null) throw new ArgumentNullException(nameof(cues));
            if (facing == null) throw new ArgumentNullException(nameof(facing));
            if (fire == null) throw new ArgumentNullException(nameof(fire));
            if (release == null) throw new ArgumentNullException(nameof(release));
            if (sinks == null) throw new ArgumentNullException(nameof(sinks));
            CombatComposition composition = BeginClip(shape, facing, lead, out CombatPieceStart start);
            CombatClipRangedAttack.Add(composition, shape.AttackerId, clip, start, fire, releaseTarget, release, launchAimPoint, out int played,
                out int hit, out IReadOnlyList<CombatProjectileCueTiming> projectileTimings, canRelease, contactFeedback);
            CombatCueAnchors anchors = AnchorsOf(clip, start, played, hit).SetProjectileTimings(projectileTimings).SetClipEvents(clip.Motion, played);
            AddCues(composition, shape, cues, anchors, sinks, actorBody);
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

        // 첫 실제 피격(Contact·FirstHit) = 해제/집계의 Hit 신호, 끝 = 클립 끝입니다. 클립 이벤트(Event1~3)는 표식이 있는 번호만 부르는 쪽이 잇습니다(SetClipEvents).
        // 시작(Start)은 본 단계의 ClipStart입니다(클립 칸 다시 짜기 설계 P7). 앞 단계가 없으면 본 단계가 항목 시작에 바로 꽂혀 지금처럼 항목 시작을 씁니다.
        private static CombatCueAnchors AnchorsOf(CombatClipAttackData clip, CombatPieceStart start, int played, int hit) =>
            new CombatCueAnchors()
                .StartAt(clip.Motion.Pre != null ? CombatPieceStart.After(played, CombatPieceSignal.ClipStart) : start)
                .SetFirstHit(hit, CombatPieceSignal.Hit)
                .Set(CombatCueAnchor.End, played, CombatPieceSignal.End);

        // 근접 이벤트 줄 하나의 맞는 순간 = 그 이벤트 신호 + 줄 지연입니다(설계 CS2).
        private static CombatPieceStart StrikeStart(int played, CombatClipEventEntry entry) =>
            CombatPieceStart.After(played, CombatClipEventSignals.SignalOf(entry.EventId), entry.Delay);

        // 이벤트 지점은 빌린 클립 채널에서 읽습니다. 원거리 조립만 탄별 소리 시점을 기준 시점 표에 더합니다.
        private static void AddCues(CombatComposition composition, CombatAttackShape shape, CombatEventCues cues, CombatCueAnchors anchors,
            CombatCueSinks sinks, Pose? actorBody)
        {
            CombatCueComposer.Add(composition, anchors, cues, CombatCuePoints.ForActor(shape.AttackerId, actorBody), sinks);
        }
    }
}
