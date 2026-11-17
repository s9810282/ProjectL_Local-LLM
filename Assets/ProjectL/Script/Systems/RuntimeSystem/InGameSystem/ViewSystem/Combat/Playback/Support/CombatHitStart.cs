using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Reaction;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>
    /// 피격 연출 시작 한 곳입니다(피격 구조 정리 설계 HC2, 공격의 <see cref="CombatAttackStart"/>와 같은 자리).
    /// 캐릭터 처리기의 피격 사건(보통 피격·밀림과 함께 가는 피격)과 연발 앞 탄의 접촉 피격이 여기서 시작하고, 앞 피격 끊기도 여기를 지납니다.
    /// 사망은 캐릭터 처리기에 남고, 주체 몸 자리(<see cref="SubjectBodyOf"/>)만 같이 씁니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 피격 조립(<see cref="Compose"/>)은 유닛 실행 하나입니다: 뼈대(반응 조각 또는 피격 시간 기다림, <see cref="SkeletonOf"/>) + 피격 클립 + 피격 칸 큐.
    /// 뼈대 고르기 규칙은 함수 하나이고 보통 피격·연발 앞 탄이 함께 부릅니다. 밀림과 함께 가는 피격은 자세를 빌리면 안 되어 늘 기다림입니다.
    /// </para>
    /// <para>
    /// 피격 클립이 있는 유닛은 반응 값이 켜 두지 않으면 코드 피격 움직임(밀림·젖힘) 없이 클립만 냅니다(<see cref="CombatUnitReactionPresentationData.PlaysHitMotion"/>).
    /// 적 전용 재구성의 치명타(<see cref="CombatEventKind.Reconstructed"/>)는 피격으로 재생하고 사실은 반응 뒤에 반영합니다(<see cref="CombatCharacterHits"/>).
    /// </para>
    /// </remarks>
    internal sealed class CombatHitStart
    {
        private readonly CombatUnitPresentationWindow _presentation;
        private readonly Func<FieldGridMapper> _mapper;
        private readonly CombatShownFacts _facts;
        private readonly CombatEventAudio _sound;
        private readonly CombatPieceRunPlayer _pieces;
        private readonly Func<int> _generation;
        private readonly CombatCameraWindow _camera;
        private readonly CombatEffectWindow _effects;
        private readonly CombatKnockbackLeadIns _knockbackLeadIns;

        /// <param name="presentation">유닛 표시 창구입니다. 반응 설정·피격 클립·표시 회전을 읽습니다. 자세는 조립이 빌립니다.</param>
        /// <param name="pieces">조각 실행 재생기입니다. 피격 조립을 넘기고 앞 피격을 끊습니다.</param>
        /// <param name="generation">사건 Presenter의 재생 세대입니다(D10). 콜백 뒤 세대가 바뀌었으면 완료·반영을 하지 않습니다.</param>
        /// <param name="camera">맵 카메라 창구입니다. 흔들림을 낼 수 없으면(수신기 없음) 피격 칸의 흔들림 큐를 넣지 않습니다.</param>
        /// <param name="effects">이펙트 창구입니다(피격 이펙트 설계 HE5).</param>
        /// <param name="knockbackLeadIns">밀림 걸음으로 바로 이어지는 피격 목록입니다(넉백 피격 연출, 임시). 사건 Presenter가 결과마다 채웁니다.</param>
        internal CombatHitStart(CombatUnitPresentationWindow presentation, Func<FieldGridMapper> mapper, CombatShownFacts facts,
            CombatEventAudio sound, CombatPieceRunPlayer pieces, Func<int> generation, CombatCameraWindow camera, CombatEffectWindow effects,
            CombatKnockbackLeadIns knockbackLeadIns)
        {
            _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
            _sound = sound ?? throw new ArgumentNullException(nameof(sound));
            _pieces = pieces ?? throw new ArgumentNullException(nameof(pieces));
            _generation = generation ?? throw new ArgumentNullException(nameof(generation));
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _effects = effects ?? throw new ArgumentNullException(nameof(effects));
            _knockbackLeadIns = knockbackLeadIns ?? throw new ArgumentNullException(nameof(knockbackLeadIns));
        }

        /// <summary>
        /// 그 사건이 앞 피격(연발 앞 탄의 접촉 피격)을 끊는 사건이면 지금 끊습니다(2026-11-17 사용자 "A 끊고 다시", 피격 구조 정리 계획 R1).
        /// 끊을지는 그 실행의 통과 규칙(<see cref="CombatNextHitPass"/>)이 막기 판정과 같은 규칙으로 정합니다. 자세·클립·사망 유지를 빌리기 전에 부릅니다.
        /// 캐릭터 처리기(Core 사건 시작)와 다음 탄의 접촉 피격이 이것 하나를 부릅니다.
        /// </summary>
        internal void CutPrevious(CombatEvent e) => _pieces.CutRunFor(e);

        /// <summary>
        /// 피격 반응을 시작합니다. 피격으로 보이는 사건(<see cref="CombatCharacterHits.IsHitLike"/>)만이고, 반응 설정이 없으면 false라 틀이 사실만 반영합니다.
        /// 보통은 사실 반영 → 다시 그리기 → 조립 시작입니다(설계 BP4). 재구성 사건은 사건 뒤 사실이 체력 최대·재구성 중이라
        /// 반응이 끝난 뒤(조립 완료 콜백) 반영합니다(재구성 연출 설계 RE1, 낭떠러지 추락과 같은 꼴). 재생이 취소되면 콜백이 오지 않아 반영하지 않고
        /// 결과 끝·정리의 최종 사실이 맞춥니다.
        /// </summary>
        /// <param name="original">결과에 들어 있던 사건입니다(위치 보정 전). 밀림 걸음으로 이어지는지는 이것으로 묻습니다.</param>
        internal bool TryBegin(CombatCharacterEvent damage, CombatEvent original, Action completed, int version)
        {
            if (damage == null) throw new ArgumentNullException(nameof(damage));
            if (!CombatCharacterHits.IsHitLike(damage) || !_presentation.TryGetReactionSettings(damage.Final.EntityId, out var settings))
                return false;
            EntityId id = damage.Final.EntityId;
            // 밀림 걸음으로 바로 이어지는 피격은 맞는 순간 함께 밀립니다(넉백 피격 연출, 임시 — 2026-11-11 사용자 "피격과 동시에 밀림").
            // 이미 젖힌 채 밀리는 중(표시 회전이 있음, 밀리다 지형 피해 등에 맞음)이면 보통 피격입니다.
            // 재구성 사건은 밀림 앞 피격으로 세지 않습니다(CombatCharacterHits.IsPushLeadInHit) — 사실을 반응 뒤에 반영해야 해서 아래 보통 피격 길로 갑니다.
            if (_knockbackLeadIns.Contains(original) && !_presentation.TryGetFacing(id, out _))
                return BeginPushHit(damage, settings, completed, version);
            bool factsAfterReaction = CombatCharacterHits.AppliesFactsAfterReaction(damage);
            if (!factsAfterReaction)
            {
                _facts.ApplyCharacter(damage.Final);
                _facts.Render();
                if (version != _generation()) return true;
            }
            // 반응 시작 자리는 지금처럼 사실 반영·다시 그리기 뒤입니다(설계 BP4). 자세는 조립이 시작할 때 빌립니다 —
            // 체력 사건은 자리가 그대로이고, 그리기는 빌린 자세·서는 자리를 덮지 않아 같은 자세를 잡습니다(피격 구조 정리 계획 R13).
            _presentation.TryGetHitClip(id, out CombatClipMotion hitClip);
            CombatComposition composition = Compose(id, SkeletonOf(id, settings, damage), settings.HitCues,
                _presentation.CanFlash(id), hitClip, _camera, _effects, BodyOf(id, damage), _sound);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            composition.ProfilingEvent = damage;
#endif
            // 완료는 뼈대(반응 또는 피격 시간 기다림)·플래시·피격 클립이 모두 끝날 때입니다.
            Action done = factsAfterReaction ? () =>
            {
                _facts.ApplyCharacter(damage.Final);
                _facts.Render();
                completed?.Invoke();
            } : completed;
            if (!_pieces.TryBegin(composition, done, version))
                throw new InvalidOperationException("[CombatHitStart] 피격 조립을 시작하지 못했습니다.");
            return true;
        }

        /// <summary>
        /// 연발 앞 탄의 접촉 피격(곁들임)입니다(2026-11-11 사용자 "아킴보 투사체 2·숫자 2인데 피격 1"). Core는 공격 하나에 대상마다 피해 사건 하나라서,
        /// 마지막 탄은 그 사건(<see cref="TryBegin"/>·사망·체력)이 맡고 앞 탄은 사실·체력·사망 없이 피격 조립(<see cref="Compose"/>)만 냅니다.
        /// 뼈대는 보통 피격과 같은 규칙(<see cref="SkeletonOf"/>)입니다.
        /// 그 유닛이 다른 연출(유닛 실행·자세·클립·걸음 배치)을 쓰는 중이면 빼앗지 않고 내지 않습니다.
        /// 이 실행은 다음 피격이 끊어도 되는 실행입니다(통과 규칙 <see cref="CombatNextHitPass"/>, 2026-11-17 사용자 "A 끊고 다시"):
        /// 다음 탄의 접촉 피격과 마지막 탄의 피격·사망 사건은 이 실행을 기다리지 않고, 시작하며 끊은 뒤 처음부터 다시 냅니다.
        /// (옛 규칙 "마지막 탄은 이 실행이 끝날 때까지 기다림"(11-11)은 피격 클립이 들어와 실행이 1초를 넘으면서 피격이 늦어져 바꿨습니다.)
        /// </summary>
        /// <param name="damage">그 출처로 맞은 피해 사건(<c>HealthChanged</c> 또는 <c>Died</c>)입니다. 맞은 방향만 읽습니다.</param>
        internal void TryBeginContact(CombatCharacterEvent damage, int version)
        {
            if (damage == null) throw new ArgumentNullException(nameof(damage));
            if (version != _generation()) return;
            EntityId id = damage.Final.EntityId;
            // 앞 탄의 접촉 피격은 이 탄이 끊고 다시 엽니다(같은 피해 사건이라 끊는 규칙도 같음). 다른 실행(사건 피격·사망·유닛 행동 등)은 그대로 두고 이 탄은 내지 않습니다.
            CutPrevious(damage);
            // 피격·사망 반응은 늘 유닛 실행 안에서 돌아, 유닛 실행이 없으면 반응도 없습니다(피격 구조 정리 계획 R5).
            if (_pieces.HasUnitRun(id) || !_presentation.TryGetReactionSettings(id, out var settings))
                return;
            _presentation.TryGetHitClip(id, out CombatClipMotion hitClip);
            if (!_presentation.CanBeginContactHit(id, hitClip != null) || !_presentation.TryGetRotation(id, out Quaternion facing))
                return;
            CombatComposition composition = Compose(id, SkeletonOf(id, settings, damage), settings.HitCues,
                    _presentation.CanFlash(id), hitClip, _camera, _effects, SubjectBodyOf(_facts, _mapper(), id, damage, facing), _sound)
                .PassSubject(CombatNextHitPass.Instance);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            composition.ProfilingEvent = damage;
#endif
            // 곁들임이라 완료를 알릴 사건이 없습니다. 빌리지 못하면(표시가 막 꺼짐) 내지 않습니다.
            _pieces.TryBegin(composition, null, version);
        }

        /// <summary>
        /// 주체(맞은·죽는 유닛)의 몸 자리입니다(큐 자리 <c>SubjectBody</c>, 피격 이펙트 설계 HE2·HE3). 보인 사실에서 몸 중심을 찾지 못하면 null이고,
        /// 그 자리의 이펙트 큐는 항목을 넣지 않습니다. 회전 = 맞은 방향(반응 밀림과 같은 함수, 이펙트 +Z가 그 방향).
        /// 피격은 사실 반영 뒤, 사망은 반영 전, 연발 앞 탄의 접촉 피격은 사실 반영 없이 부르며 모두 보인 사실의 맞은 유닛은 살아 있습니다.
        /// 사망(캐릭터 처리기)도 부릅니다. 자리 계산이 보인 사실(Playback)을 써서 맞은 방향 함수(<see cref="CombatHitDirection"/>, Presentation) 쪽에 두지 않습니다(계획 §5 R7).
        /// </summary>
        /// <param name="facing">맞은 유닛의 지금 방향입니다(표시 회전, 깜빡임 사망은 서 있는 방향).</param>
        /// <remarks>보통 피격·밀림과 함께 가는 피격·보통 사망은 지금 표시 회전으로 구하는 <see cref="BodyOf"/>를 씁니다.</remarks>
        internal static Pose? SubjectBodyOf(CombatShownFacts facts, FieldGridMapper mapper, EntityId id, CombatCharacterEvent e, Quaternion facing)
        {
            if (facts == null) throw new ArgumentNullException(nameof(facts));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            if (!facts.TryGetBodyCenter(id, out Vector3 center))
                return null;
            Quaternion rotation = Quaternion.LookRotation(CombatHitDirection.Resolve(e, mapper, facing), mapper.WorldUp);
            return new Pose(center, rotation);
        }

        /// <summary>
        /// 지금 표시 회전으로 구한 주체 몸 자리입니다(<see cref="SubjectBodyOf"/>). 표시가 없으면 null입니다.
        /// 보통 피격·밀림과 함께 가는 피격과 보통 사망(캐릭터 처리기)이 부릅니다 — "회전이 있으면 몸 자리" 식을 한 곳에 둡니다(계획 §6.7 ⑤).
        /// </summary>
        internal Pose? BodyOf(EntityId id, CombatCharacterEvent e) =>
            _presentation.TryGetRotation(id, out Quaternion facing) ? SubjectBodyOf(_facts, _mapper(), id, e, facing) : null;

        /// <summary>
        /// 피격 조립입니다(조각·조립 설계 BP9, 결정 Q-B5 A·Q-B7 A). 유닛 실행(주체 = 맞은 유닛), 일반 완료입니다.
        /// 뼈대는 0 피격 반응 조각(기다림 — <see cref="SkeletonOf"/>)과, 피격 클립이 있으면 클립 채널을 빌린 클립 조각(같은 순간, 기다림, 클립 설계 AC3)입니다.
        /// 뼈대가 자세를 빌려야 하면(반응 조각) 이 함수가 자세 채널을 적습니다 — 반응 조각의 자세 요구를 적는 곳은 뼈대 값과 이 함수 하나입니다(계획 §6.7 ③).
        /// 그 뒤 피격 칸 큐(플래시·흔들림·이펙트)와 맞은 유닛 외형의 피격 소리 줄을 붙입니다
        /// (범용 연출 구조 설계 G6, 유닛 소리 설계 US1). 기준 시점은 시작·기준 순간(피격 클립의 이벤트 타이밍)·끝(반응 끝)입니다.
        /// 플래시는 기다리는 큐라 완료가 플래시 끝까지도 기다립니다. 플래시가 반응보다 길면 그 유닛의 다음 사건이 늦어집니다(의도, 요구 1·3).
        /// 같은 순간 항목 순서는 뼈대 → 클립 → 큐입니다(기준 순간 큐가 클립 항목을 가리켜야 해서 뼈대가 먼저, 같은 프레임이라 화면은 같음).
        /// </summary>
        /// <param name="skeleton">뼈대입니다(끝을 알리는 반응 조각 또는 피격 시간 기다림, 자세 대여 여부). 조립이 끝을 기다립니다.</param>
        /// <param name="cues">피격 칸 큐입니다(반응 값의 <c>HitCues</c>).</param>
        /// <param name="canFlash">표시가 플래시 외형 층을 쓸 수 있는지입니다(<see cref="CombatUnitPresentationWindow.CanFlash"/>).</param>
        /// <param name="hitClip">맞은 유닛의 피격 동작입니다(<see cref="CombatUnitPresentationWindow.TryGetHitClip"/>). 없으면 null이고 클립 항목을 넣지 않습니다. 큐의 Event1~3은 그 동작의 본 단계 몸 클립에 표식이 있는 이벤트만 잇습니다.</param>
        /// <param name="camera">맵 카메라 창구입니다. 없거나 흔들림을 낼 수 없으면(수신기 없음) 흔들림 큐를 넣지 않습니다.</param>
        /// <param name="effects">이펙트 창구입니다. 없으면 이펙트 큐를 넣지 않습니다(운영 경로는 늘 넘김).</param>
        /// <param name="subjectBody">주체 몸 자리입니다(<see cref="SubjectBodyOf"/>). 없으면 그 자리의 이펙트를 넣지 않습니다.</param>
        /// <param name="sound">소리 창구입니다. 맞은 유닛 외형의 피격 소리 줄(유닛 소리 설계 US1)을 냅니다. 없으면 소리 항목을 넣지 않습니다(운영 경로는 늘 넘김).</param>
        private static CombatComposition Compose(EntityId subject, Skeleton skeleton, CombatEventCues cues, bool canFlash,
            CombatClipMotion hitClip, CombatCameraWindow camera, CombatEffectWindow effects, Pose? subjectBody, CombatEventAudio sound)
        {
            if (skeleton.Piece == null) throw new ArgumentException("피격 뼈대 조각이 없습니다.", nameof(skeleton));
            if (cues == null) throw new ArgumentNullException(nameof(cues));
            var composition = CombatComposition.ForUnit(subject, isDeath: false, CombatPieceCompletion.General);
            if (skeleton.LeasesPose)
                composition.Lease(subject, CombatPieceChannel.Pose);
            int reactionItem = composition.Add(skeleton.Piece, CombatPieceStart.AtStart(), waits: true);
            var anchors = new CombatCueAnchors().Set(CombatCueAnchor.End, reactionItem, CombatPieceSignal.End);
            if (hitClip != null)
            {
                composition.Lease(subject, CombatPieceChannel.Clip);
                int clip = composition.Add(new CombatClipPiece(subject, hitClip), CombatPieceStart.AtStart(), waits: true);
                // 큐의 Event1~3은 피격 동작의 본 단계 몸 클립에 표식이 있는 번호만 잇습니다(11-17 사용자, 설계 CS3의 등록 조건을 대신함).
                anchors.SetClipEvents(hitClip, clip);
            }
            // 피격 소리는 맞은 유닛 외형의 피격 소리 줄입니다(유닛 소리 설계 US1). 시점은 이 조립의 기준 시점(시작·피격 클립 이벤트·반응 끝)입니다.
            CombatCueComposer.Add(composition, anchors, cues, CombatCuePoints.ForSubject(subject, subjectBody),
                new CombatCueSinks(effects, camera, sound, sound?.HitSoundsOf(subject), subject, canFlash));
            return composition;
        }

        /// <summary>
        /// 밀림과 함께 가는 피격입니다(넉백 피격 연출, 임시 — 2026-11-11 사용자 "넉백 당하는 캐릭터는 피격과 동시에 밀린다").
        /// 자세를 빌리는 피격 반응(젖혔다 폄·짧은 밀림)은 내지 않고, 피격 칸 큐(플래시·이펙트·흔들림·소리)와 피격 클립만 냅니다.
        /// 이 실행은 주체의 밀림 걸음을 막지 않고(통과 규칙 <see cref="CombatPushAlongsidePass"/>) 사건은 바로 완료하므로, 같은 순간 밀리기 시작합니다.
        /// 젖힘은 밀림 걸음이 피격 반응과 같은 곡선으로 냅니다(<c>CombatPushLean</c>). 뼈대는 피격 시간만큼 기다리는 조각이라 끝 시점 큐도 보통 피격과 같은 때 납니다.
        /// 피격과 밀림 사이에 끼는 그 유닛의 특성 발동·상태 사건도 막지 않습니다(같은 유닛 사건은 차례로 재생돼, 끼인 사건이 막히면 밀림도 늦어짐).
        /// 그 유닛의 다른 캐릭터 사건(밀린 뒤 부딪힘 피해 등)은 지금처럼 이 실행이 끝날 때까지 기다립니다.
        /// </summary>
        private bool BeginPushHit(CombatCharacterEvent damage, CombatUnitReactionPresentationData settings, Action completed, int version)
        {
            EntityId id = damage.Final.EntityId;
            _facts.ApplyCharacter(damage.Final);
            _facts.Render();
            if (version != _generation()) return true;
            _presentation.TryGetHitClip(id, out CombatClipMotion hitClip);
            // 자세를 빌리면 안 되어(통과 규칙이 걸음 배치와 겹침) 뼈대 고르기 규칙을 쓰지 않고 늘 피격 시간 기다림입니다.
            CombatComposition composition = Compose(id, Skeleton.Wait(settings), settings.HitCues,
                    _presentation.CanFlash(id), hitClip, _camera, _effects, BodyOf(id, damage), _sound)
                .PassSubject(CombatPushAlongsidePass.Instance);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            composition.ProfilingEvent = damage;
#endif
            // 사건 완료는 이 실행이 아니라 지금 알립니다. 빌리지 못하면(표시가 막 꺼짐) 내지 않습니다.
            _pieces.TryBegin(composition, null, version);
            if (version == _generation()) completed?.Invoke();
            return true;
        }

        /// <summary>
        /// 피격 뼈대 고르기 규칙 하나입니다(피격 구조 정리 설계 HC2). 코드 피격 움직임을 내면(<see cref="CombatUnitPresentationWindow.PlaysHitMotion"/>)
        /// 반응 조각(<see cref="CombatReactionMotionPiece.Hit"/>)이고 자세를 빌립니다. 아니면 피격 시간만 기다리고 자세를 빌리지 않습니다.
        /// 보통 피격과 연발 앞 탄이 부릅니다. 밀림과 함께 가는 피격은 자세를 빌리면 안 되어 이 규칙을 쓰지 않고 늘 기다림입니다.
        /// </summary>
        private Skeleton SkeletonOf(EntityId id, CombatUnitReactionPresentationData settings, CombatCharacterEvent damage) =>
            _presentation.PlaysHitMotion(id)
                ? new Skeleton(CombatReactionMotionPiece.Hit(id, settings, damage, _mapper()), leasesPose: true)
                : Skeleton.Wait(settings);

        /// <summary>피격 뼈대 값입니다: 끝을 알리는 조각과, 그 조각이 자세 채널을 빌려야 하는지(<see cref="Compose"/>가 적음).</summary>
        private readonly struct Skeleton
        {
            internal Skeleton(ICombatPresentationPiece piece, bool leasesPose)
            {
                Piece = piece ?? throw new ArgumentNullException(nameof(piece));
                LeasesPose = leasesPose;
            }

            internal ICombatPresentationPiece Piece { get; }

            internal bool LeasesPose { get; }

            /// <summary>피격 시간만 기다리는 뼈대입니다. 자세를 빌리지 않습니다.</summary>
            internal static Skeleton Wait(CombatUnitReactionPresentationData settings) =>
                new(new CombatWaitPiece((settings ?? throw new ArgumentNullException(nameof(settings))).HitDuration), leasesPose: false);
        }
    }
}
