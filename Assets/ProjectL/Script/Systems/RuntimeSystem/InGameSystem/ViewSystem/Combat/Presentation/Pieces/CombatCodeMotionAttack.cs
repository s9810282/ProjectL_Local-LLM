using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Effects;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 코드 모션 공격 조립입니다(범용 연출 구조 설계 G5, 계획 Gc.4). 공격 클립이 없는 유닛의 공격도 클립 공격·포효와 같은 실행 장치(조각 실행)로 돕니다.
    /// 행동 실행 + 출처 완료(공격 모양의 출처 사건, 공격자, 모양의 완료 정책 — 공격 사건은 첫 해제)라 Gate 정책·겹침 규칙이 클립 공격과 같습니다.
    /// 입력은 공격 사건이 아니라 공격 모양입니다(특성 발동 연출 설계 TA5).
    /// </summary>
    /// <remarks>
    /// <para>항목:</para>
    /// <list type="number">
    /// <item>0 코드 모션 — 공격 재생기가 모션을 진행합니다(<see cref="CombatCodeMotionPiece"/>, 실행 시작). 기준 순간에 <see cref="CombatPieceSignal.MotionCue"/>,
    /// 타격에 대상 모두 해제 → <see cref="CombatPieceSignal.Hit"/>을 냅니다. 모션 끝이 이 항목의 끝입니다.</item>
    /// </list>
    /// <para>그 뒤 공격 칸의 큐를 붙입니다(범용 연출 구조 설계 G6): 기준 순간 = 항목 0의 <see cref="CombatPieceSignal.MotionCue"/>, 맞음 = 항목 0의 <see cref="CombatPieceSignal.Hit"/>,
    /// 끝 = 모션 끝. 소리 시점 기본(Cue + 0)이 옛 코드 모션이 효과음을 내던 순간과 같습니다(계획 §0.4 1번). 행위자 지점 자리는 클립 채널이 없어 쓰지 않습니다(계획 P6).</para>
    /// <para>
    /// 사건 완료는 첫 해제(Gate), 전체 종료는 모션 끝입니다. 자세는 모션이 스스로 빌리므로(공격 재생의 공격 자세) 실행 대여에 넣지 않습니다(포효의 정면 공격과 같음).
    /// 모션을 시작하지 못하면 항목 0이 바로 끝나고, 기준 순간·맞음을 기다리던 큐는 건너뜁니다. 그래서 처리기는 시작할 모션이 있을 때만 이 조립을 만듭니다
    /// (<see cref="CombatAttackPlayback.HasCodeMotion(EntityId, bool)"/>).
    /// </para>
    /// <para>앞 항목(<see cref="CombatAttackLead"/>, 대신 맞는 수호자 나가기)이 있으면 그 항목이 맨 앞에 오고, 모션과 시작 큐는 그 항목이 돌려준 시점에 시작하며,
    /// 맞는 순간 뒤 항목(대기·돌아오기)이 끝에 붙습니다(유닛 행동 설계 AA6). 그때 모션 번호는 앞 항목 수만큼 밀립니다.</para>
    /// <para>창구·자리·앞 항목 인자는 클립 공격 입구(<see cref="CombatClipAttack"/>)처럼 기본값 없이 모두 받습니다. 창구를 더할 때 빠뜨린 입구가 컴파일에서 걸리게 하려는 것이고,
    /// 없는 값은 null로 넘깁니다(평가 C4-08 1단계).</para>
    /// </remarks>
    internal static class CombatCodeMotionAttack
    {
        /// <param name="tryBeginMotion">코드 모션 입구입니다. 인자는 (모션 완료 콜백, 기준 순간 콜백, 타격 순간 콜백)입니다(<see cref="CombatAttackPlayback.TryBeginMotion"/>).</param>
        /// <param name="release">출처의 대상을 모두 해제하는 입구입니다(<see cref="ICombatSourceLifetime.ReleaseAll"/>). 타격 신호보다 먼저 부릅니다.</param>
        /// <param name="cues">공격 칸 큐입니다(공격 표시 값의 <c>Cues</c>).</param>
        /// <param name="effects">이펙트 창구입니다. 없으면 이펙트 큐를 넣지 않습니다.</param>
        /// <param name="camera">맵 카메라 창구입니다. 없으면 흔들림 큐를 넣지 않습니다.</param>
        /// <param name="actorBody">행위자 몸 자리입니다(큐 자리 <c>ActorBody</c>). 없으면 그 자리의 이펙트를 넣지 않습니다.</param>
        /// <param name="projectileSounds">투사체 소리입니다(직선·곡사만, 비행 = 발사(MotionCue) + 지연, 착탄 = 맞음 + 지연, 투사체·피격 소리 설계 PS3). 없으면 넣지 않습니다.</param>
        /// <param name="lead">공격 조립의 앞 항목입니다(대신 맞는 수호자 나가기·돌아오기, 유닛 행동 계획 TP-14). 없으면 조립이 지금과 같습니다.</param>
        internal static CombatComposition Compose(CombatAttackShape shape, CombatEventCues cues, Func<Action, Action, Action, bool> tryBeginMotion,
            Action release, CombatEventAudio sound, CombatEffectWindow effects, CombatCameraWindow camera, Pose? actorBody,
            CombatProjectileSounds projectileSounds, CombatAttackLead lead)
        {
            if (shape == null) throw new ArgumentNullException(nameof(shape));
            if (cues == null) throw new ArgumentNullException(nameof(cues));
            if (tryBeginMotion == null) throw new ArgumentNullException(nameof(tryBeginMotion));
            if (release == null) throw new ArgumentNullException(nameof(release));
            if (sound == null) throw new ArgumentNullException(nameof(sound));
            EntityId attacker = shape.AttackerId;
            var composition = CombatComposition.ForAction(
                CombatPieceCompletion.FromSource(shape.Source, attacker, shape.Completion));
            // 앞 항목(수호자 나가기)이 있으면 뼈대와 시작 큐는 그 뒤입니다(유닛 행동 계획 TP-14).
            CombatPieceStart start = lead?.Begin(composition) ?? CombatPieceStart.AtStart();
            // 해제가 먼저입니다(순서 규칙은 CombatPieceSignal.Hit 문서, 함수는 CombatHitMoment 하나).
            int motion = composition.Add(new CombatCodeMotionPiece((end, cue, hit) =>
                tryBeginMotion(end, cue, CombatHitMoment.ReleaseThenSignal(release, hit))), start);
            var anchors = new CombatCueAnchors()
                .StartAt(start)
                .Set(CombatCueAnchor.Cue, motion, CombatPieceSignal.MotionCue)
                .Set(CombatCueAnchor.Hit, motion, CombatPieceSignal.Hit)
                .Set(CombatCueAnchor.End, motion, CombatPieceSignal.End);
            CombatCueComposer.Add(composition, anchors, cues, CombatCuePoints.ForActor(attacker, actorBody),
                new CombatCueSinks(effects, camera, sound, shape.Source, projectileSounds: projectileSounds));
            lead?.AfterHit(composition, motion);
            return composition;
        }
    }
}
