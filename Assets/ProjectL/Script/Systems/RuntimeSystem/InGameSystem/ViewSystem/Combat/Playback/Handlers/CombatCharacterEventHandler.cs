using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Environment;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Handlers
{
    /// <summary>
    /// 캐릭터 사건의 표시입니다(Presenter 분리 설계 S3). 피해 숫자를 보이고, 갈래(앞 피격 끊기 → 토네이도 → 사망 → 피격)를 고릅니다.
    /// 반응을 시작하지 못한 사건(반응 설정 없음, 이동·복귀 등)은 사실만 반영합니다. 기차 동시 사망도 같은 경로입니다.
    /// 피격은 피격 시작(<see cref="CombatHitStart"/>)이 맡고(피격 구조 정리 설계 HC2), 이 처리기에는 토네이도 던짐과 사망 셋(보통·낭떠러지 추락·자폭)이 남습니다.
    /// 사망 반응은 조립 안의 반응 조각(<see cref="CombatReactionMotionPiece"/>)이 진행하고 자세는 조립이 빌립니다(HC1). 표시가 사라지면 그 실행이 사라짐이 되어
    /// 조각 실행 재생기가 다음 Tick에 알립니다. 사망 조립(<see cref="ComposeDeath"/>: 사망 반응 + 사망 칸 큐)은 조각 실행 재생기로 진행합니다(조각·조립 설계 BP8).
    /// 사망 유지와 사실 반영은 이 처리기가 합니다(설계 BP1·BP4). 흔들림·이펙트는 반응 값의 칸 큐, 사망 소리는 그 유닛 외형의 소리 줄이 정하고
    /// 큐 붙이기(<see cref="CombatCueComposer"/>) 한 곳이 항목으로 붙입니다(범용 연출 구조 설계 G6, 유닛 소리 설계 US1).
    /// 주체 몸 자리(몸 중심·맞은 방향)는 피격과 같은 함수(<see cref="CombatHitStart.SubjectBodyOf"/>)로 구해 넘깁니다.
    /// 낭떠러지에 떨어진 유닛(보인 사실의 추락 표지)의 사망은 사망 반응 대신 추락 조립(<see cref="ComposeFall"/>)을 씁니다(임시).
    /// </summary>
    internal sealed class CombatCharacterEventHandler : CombatEventHandler
    {
        private readonly CombatVolleyDamageNumbers _damageNumbers;
        private readonly CombatUnitPresentationWindow _presentation;
        private readonly Func<FieldGridMapper> _mapper;
        private readonly CombatShownFacts _facts;
        private readonly CombatEventAudio _sound;
        private readonly CombatPieceRunPlayer _pieces;
        private readonly Func<int> _generation;
        private readonly CombatCameraWindow _camera;
        private readonly CombatEffectWindow _effects;
        private readonly CombatFallMotionSettings _fallMotion;
        private readonly ICombatTornadoField _tornado;
        private readonly Func<CombatCharacterEvent, bool> _isAttackHit;
        private readonly CombatHitStart _hits;
        private readonly CombatSelfDestructDeaths _selfDestruct;
        private readonly System.Random _tornadoSeeds = new();

        /// <param name="damageNumbers">탄별로 나눈 숫자는 중복하지 않고 나머지는 기존 피해 숫자를 표시합니다.</param>
        /// <param name="presentation">유닛 표시 창구입니다. 반응 설정·사망 유지를 여기서 읽고 빌립니다(연출 조립 설계 B13). 반응 자세는 조립이 빌립니다.</param>
        /// <param name="pieces">조각 실행 재생기입니다. 사망 조립(조각·조립 설계 BP8)·토네이도 던짐을 넘깁니다(피격 조립은 피격 시작이 넘김).</param>
        /// <param name="generation">사건 Presenter의 재생 세대입니다(D10). 콜백 뒤 세대가 바뀌었으면 완료·반영을 하지 않습니다.</param>
        /// <param name="camera">맵 카메라 창구입니다(필수). 흔들림을 낼 수 없으면(수신기 없음) 피격·사망 칸의 흔들림 큐를 넣지 않습니다.</param>
        /// <param name="effects">이펙트 창구입니다(피격 이펙트 설계 HE5, 필수, View 3라운드 W9-3).</param>
        /// <param name="fallMotion">낭떠러지 추락 연출 값입니다(임시, 필수).</param>
        /// <param name="tornado">토네이도 표시입니다. 던짐 연출 설정을 읽습니다(토네이도 View 설계 TN5, 필수).</param>
        /// <param name="hits">피격 시작입니다(피격 구조 정리 설계 HC2, 필수). 피격 사건과 앞 피격 끊기를 넘깁니다. 연발 앞 탄의 접촉 피격도 사건 Presenter가 이것에 바로 잇습니다.</param>
        /// <param name="selfDestruct">이 결과에서 터질 유닛과 그 깜빡임 값·몸 유지입니다(자폭 깜빡임 설계 SD1·SD4, 필수).</param>
        internal CombatCharacterEventHandler(CombatVolleyDamageNumbers damageNumbers, CombatUnitPresentationWindow presentation,
            Func<FieldGridMapper> mapper, CombatShownFacts facts,
            CombatEventAudio sound, CombatPieceRunPlayer pieces, Func<int> generation,
            CombatCameraWindow camera, CombatEffectWindow effects,
            CombatFallMotionSettings fallMotion, ICombatTornadoField tornado, Func<CombatCharacterEvent, bool> isAttackHit,
            CombatHitStart hits, CombatSelfDestructDeaths selfDestruct)
        {
            _damageNumbers = damageNumbers ?? throw new ArgumentNullException(nameof(damageNumbers));
            _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
            _sound = sound ?? throw new ArgumentNullException(nameof(sound));
            _pieces = pieces ?? throw new ArgumentNullException(nameof(pieces));
            _generation = generation ?? throw new ArgumentNullException(nameof(generation));
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _effects = effects ?? throw new ArgumentNullException(nameof(effects));
            _fallMotion = fallMotion ?? throw new ArgumentNullException(nameof(fallMotion));
            _tornado = tornado ?? throw new ArgumentNullException(nameof(tornado));
            _isAttackHit = isAttackHit ?? throw new ArgumentNullException(nameof(isAttackHit));
            _hits = hits ?? throw new ArgumentNullException(nameof(hits));
            _selfDestruct = selfDestruct ?? throw new ArgumentNullException(nameof(selfDestruct));
        }

        internal override bool TryBeginBeforeFacts(CombatEvent e, CombatEvent original, Action completed, int version)
        {
            var character = (CombatCharacterEvent)e;
            // 위치 보정 전 원본으로 탄별 숫자의 중복을 막습니다. 사실·피격·사망 시점은 기존대로입니다.
            _damageNumbers.ShowUnsplit(character, original);
            // 연발 앞 탄의 접촉 피격은 그 유닛의 다음 피격·사망이 끊고 새로 엽니다(2026-11-17 사용자 "A 끊고 다시"). 자세·클립·사망 유지를 빌리기 전에 끊습니다.
            _hits.CutPrevious(character);
            return TryBeginTornado(character, completed, version) ||
                TryBeginDeath(character, (CombatCharacterEvent)original, completed, version) ||
                _hits.TryBegin(character, original, completed, version);
        }

        // 행동 대기가 끝난 뒤 사건을 반영하며 중간 체력·사망 시점은 최신 Buffer가 아닌 사건 스냅샷을 따른다.
        internal override void ApplyFacts(CombatEvent e) => _facts.ApplyCharacter(((CombatCharacterEvent)e).Final);

        private bool TryBeginTornado(CombatCharacterEvent relocated, Action completed, int version)
        {
            if (relocated.Kind != CombatEventKind.Relocated || !relocated.TornadoSourceId.HasValue) return false;
            long sourceId = relocated.TornadoSourceId.Value;
            // 새 Core는 던짐마다 접촉 정보를 함께 냅니다(토네이도 View 계획 TP-7). 중심은 표시나 최종 사실이 아니라 이 사건의 접촉 중심입니다(인계 §4.5).
            TornadoContactOrigin contact = relocated.TornadoContact ??
                throw new InvalidOperationException($"토네이도 던짐에 접촉 정보가 없습니다: 원본 {sourceId}.");
            FieldGridMapper mapper = _mapper();
            var target = new CombatTornadoLiftTarget(FollowStorm(sourceId, mapper.CellSurfaceToWorld(contact.Center)), _tornado.Settings);
            EntityId unit = relocated.Final.EntityId;
            var composition = CombatComposition.ForUnit(unit, isDeath: false, CombatPieceCompletion.General)
                .Lease(unit, CombatPieceChannel.Pose);
            int lift = composition.Add(new CombatTornadoLiftPiece(unit, target, _facts.StandingPoseOf(relocated.Final), mapper.WorldUp,
                _tornadoSeeds.Next()), CombatPieceStart.AtStart(), waits: true);
            // 휩쓸림(Start = 조립 시작)·착지(End = 들어 올림 끝) 큐입니다(이펙트 창구 확장 설계 EC8). 자리는 그 시점에 서 있는 유닛 몸이고,
            // 끝 자리는 아직 보이지 않은 도착 사실로 구합니다(계획 P19). 지연은 큐 규칙이 0만 받습니다(묶음 끝 검토 M1 — 완료 콜백이 자세 대여가 돌아가기 전에 착지 칸을 그리므로).
            CombatCueComposer.Add(composition, new CombatCueAnchors().Set(CombatCueAnchor.End, lift, CombatPieceSignal.End), target.Settings.Cues,
                CombatCuePoints.ForSubject(unit, _facts.StandingBodyOf(_facts.FindCharacter(unit)), _facts.StandingBodyOf(relocated.Final)),
                new CombatCueSinks(_effects, _camera, null, null));
            // 기존 휩쓸림 시작과 종료 호출을 사용합니다. 별도 하강 신호는 만들지 않습니다.
            composition.Add(new CombatEventSoundPiece(_sound, CombatEventSounds.TornadoLift), CombatPieceStart.AtStart());
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            composition.ProfilingEvent = relocated;
#endif
            if (!_pieces.TryBegin(composition, () =>
            {
                if (version != _generation()) return;
                _sound.Play(CombatEventSounds.TornadoLand());
                if (version != _generation()) return;
                // 실행기가 자세를 반환한 같은 호출 안에서 최종 칸·방향을 그립니다. 중간 중심/상승/낙하 pose는 사실에 저장하지 않습니다.
                // 떨어지기의 끝 자세가 이 그리기와 같은 서 있는 자세(StandingPoseOf)라 착지 순간 튀지 않습니다.
                ApplyFacts(relocated);
                _facts.Render();
            }, version))
                throw new InvalidOperationException($"토네이도 대상의 자세 채널을 빌리지 못했습니다: {unit}, 원본 {sourceId}.");
            // 사건은 유닛이 빨려 들어가기 시작하면 끝납니다. 폭풍은 착지를 기다리지 않고 다음 칸으로 갑니다(토네이도 View 설계 TN10).
            // 그 유닛의 다음 사건은 주체 막기가, 결과 끝은 재생 끝 판정이 착지까지 기다립니다.
            if (version == _generation()) completed?.Invoke();
            return true;
        }

        // 접촉 자리에서 폭풍 몸이 움직인 만큼 옮긴 자리를 답합니다. 몸의 자리는 프리팹 보정이 들어 있어 그대로 쓰지 않습니다. 숨겨진 폭풍은 접촉 자리입니다.
        private Func<Vector3> FollowStorm(long sourceId, Vector3 contactCenter)
        {
            if (!_tornado.TryGetBodyPosition(sourceId, out Vector3 bodyAtContact))
                return () => contactCenter;
            return () => _tornado.TryGetBodyPosition(sourceId, out Vector3 body) ? contactCenter + (body - bodyAtContact) : contactCenter;
        }

        private bool TryBeginDeath(CombatCharacterEvent death, CombatCharacterEvent original, Action completed, int version)
        {
            if (death.Kind != CombatEventKind.Died || death.Expected.Health.IsDead || !death.Final.Health.IsDead ||
                _facts.FindCharacter(death.Final.EntityId)?.Health.IsDead == true)
                return false;
            EntityId id = death.Final.EntityId;
            // 추락 사망은 반응 설정을 쓰지 않으므로 반응 설정보다 먼저 가릅니다(평가 C10-11). 반응 없는 외형도 추락 연출을 냅니다.
            if (_facts.HasFallen(id))
                return BeginFallDeath(death, completed, version);
            // 이 결과에서 터질 유닛은 사망 반응(쓰러짐·디졸브) 없이 몸을 남겼다가 자기 자폭 발동 순간에 내립니다. 폭발 이펙트가 몸이 부서지는 모습이라
            // 죽은 모습을 따로 보이지 않습니다(자폭 깜빡임 설계 SD4·SD5, 계획 TP-5 — 2026-11-11 사용자 "늘 몸 남김"). Core는 낙사한 유닛에 자폭을 내지 않아 낙사와 겹치지 않습니다.
            if (_selfDestruct.IsExploding(id))
                return BeginSelfDestructDeath(death, completed, version);
            if (!_presentation.TryGetReactionSettings(id, out var settings))
                return false;
            bool launchRagdoll = _isAttackHit(original);
            var hold = _presentation.HoldDeath(id);
            try
            {
                // 순서는 사망 유지 → 조립 시작(자세 대여·사망 반응 시작) → 사실 반영입니다(피격 구조 정리 계획 Tb.5). 사망 소리는 보인 사실에 사망을
                // 반영하기 전에 납니다(이미 사망으로 보인 유닛은 소리를 내지 않음). 완료는 반응 끝이며 세대가 같을 때만 알립니다.
                // 치명타도 피격 이펙트를 냅니다(피격 이펙트 설계 Q-HE3 A). 몸 자리는 지금 표시 회전으로 구합니다(피격과 같은 도우미 BodyOf, 자세는 조립이 빌림).
                CombatComposition composition = ComposeDeath(id,
                    CombatReactionMotionPiece.Death(id, settings, death, _mapper(), launchRagdoll, hold.Release),
                    _sound, death, settings.DeathCues, _camera, _effects, _hits.BodyOf(id, death));
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                composition.ProfilingEvent = death;
#endif
                if (!_pieces.TryBegin(composition, completed, version))
                    throw new InvalidOperationException($"[CombatCharacterEventHandler] 사망 조립을 시작하지 못했습니다(자세 대여 실패): {id}");
                if (version != _generation()) return true;
                _facts.ApplyCharacter(death.Final);
                _facts.Render();
            }
            catch (Exception error)
            {
                try
                {
                    // 시작한 실행은 사건 Presenter의 오류 정리가 취소하고, 그때 반응 조각 취소가 자세·사망 유지를 함께 정리합니다(사망 유지 놓기는 여러 번 불려도 됨).
                    hold.Release();
                }
                catch (Exception cleanup)
                {
                    throw new AggregateException(error, cleanup);
                }
                throw;
            }
            return true;
        }

        /// <summary>
        /// 낭떠러지에 떨어진 유닛의 사망입니다(임시). 사망 유지 없이 추락 조립을 돌리고, 사망 사실은 추락이 끝날 때 반영합니다.
        /// 추락하는 동안 유닛은 보인 사실에서 살아 있고, 자세 대여가 다시 그리기로부터 위치를 지킵니다.
        /// 실행이 크기·자세 대여를 되돌린 뒤 완료를 알리므로(조각 실행의 "정리 뒤 통지") 같은 호출 안에서 사망을 반영해 모델을 내려놓고,
        /// 원래 크기로 보이는 프레임이 없습니다. 재생이 취소되면 반영하지 않고 결과 끝·정리의 최종 사실이 맞춥니다(붙잡은 모델이 남지 않음).
        /// 자세·크기를 빌리지 못하면 아무것도 시작하지 않고 false라, 틀이 소리·사실 반영으로 이어 갑니다.
        /// </summary>
        private bool BeginFallDeath(CombatCharacterEvent death, Action completed, int version)
        {
            CombatComposition composition = ComposeFall(death.Final.EntityId, _fallMotion, _mapper().WorldUp, _sound, death);
            Action fell = () =>
            {
                _facts.ApplyCharacter(death.Final);
                _facts.Render();
                completed?.Invoke();
            };
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            composition.ProfilingEvent = death;
#endif
            return _pieces.TryBegin(composition, fell, version);
        }

        /// <summary>
        /// 터질 유닛의 사망입니다(자폭 깜빡임 설계 SD4·SD5, 계획 TP-5). 사망 반응(쓰러짐·디졸브)을 쓰지 않고 몸을 사망 유지로 남겨
        /// 그 유닛의 자폭 발동이 시작할 때 놓습니다(<see cref="CombatSelfDestructDeaths.Detonate"/>).
        /// 발동 줄에 깜빡임 값이 있고 색 층을 쓸 수 있으면 깜빡임 조립을 돌리고 사망 사실을 시작 직후 반영합니다(일반 사망과 같은 자리).
        /// 깜빡임을 쓸 수 없으면 몸만 남기고 false를 돌려줘, 틀이 사건 소리·순간 큐·사실 반영·완료로 이어 갑니다(유지가 몸을 지킴).
        /// 켜진 표시가 없으면 남길 몸이 없어 false입니다. 맞은 방향의 기본 회전은 보인 사실의 서 있는 방향입니다(계획 TP-4, 자세를 빌리지 않음).
        /// </summary>
        private bool BeginSelfDestructDeath(CombatCharacterEvent death, Action completed, int version)
        {
            EntityId id = death.Final.EntityId;
            if (!_presentation.CanHoldDeath(id))
                return false;
            bool blinks = _selfDestruct.TryGetBlink(id, out CombatDeathBlinkVisualData blink) && _presentation.CanFlash(id);
            CombatEventCues cues = blinks && _presentation.TryGetReactionSettings(id, out var settings) ? settings.DeathCues : null;
            Pose? standing = blinks ? _facts.StandingBodyOf(id) : null;
            Pose? body = standing.HasValue ? CombatHitStart.SubjectBodyOf(_facts, _mapper(), id, death, standing.Value.rotation) : null;
            var hold = _presentation.HoldDeath(id);
            try
            {
                if (!blinks)
                {
                    _selfDestruct.Keep(id, hold);
                    return false;
                }
                CombatComposition composition = ComposeSelfDestructDeath(id, blink, _sound, death, cues, _camera, _effects, body);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                composition.ProfilingEvent = death;
#endif
                if (!_pieces.TryBegin(composition, completed, version))
                    throw new InvalidOperationException($"[CombatCharacterEventHandler] 깜빡임 사망 조립을 시작하지 못했습니다(색 층 대여 실패): {id}");
                if (version != _generation())
                {
                    // 시작 안에서 재생이 바뀌었으면 맡기지 않고 바로 놓습니다(남은 몸이 없게).
                    hold.Release();
                    return true;
                }
                _selfDestruct.Keep(id, hold);
                _facts.ApplyCharacter(death.Final);
                _facts.Render();
            }
            catch (Exception error)
            {
                try
                {
                    hold.Release();
                }
                catch (Exception cleanup)
                {
                    throw new AggregateException(error, cleanup);
                }
                throw;
            }
            return true;
        }

        /// <summary>
        /// 사망 조립입니다(조각·조립 설계 BP8). 유닛 실행(주체 = 죽는 유닛, 사망 실행), 일반 완료입니다. 뼈대는 0 사망 반응 조각(완료를 기다림,
        /// <see cref="CombatReactionMotionPiece.Death"/> — 자세 채널은 이 조립이 적음)이고,
        /// 그 뒤 사망 칸 큐(흔들림·이펙트)와 죽는 유닛 외형의 사망 소리 줄을 붙입니다(범용 연출 구조 설계 G6, 유닛 소리 설계 US1). 기준 시점은 시작(실행 시작)·끝(반응 끝)입니다.
        /// 옛 고정 순서(반응 → 소리 → 흔들림 → 치명타 이펙트, 모두 시작 순간)는 공유 반응 에셋의 사망 칸 큐(소리 Start, 흔들림 Start, 이펙트 Start·SubjectBody)와 같습니다.
        /// 기차 동시 사망은 사망 실행끼리만 겹칩니다(설계 B14 O14). 소리가 기다리지 않아, 반응이 시작 안에서 끝나는 경우에도 옛 순서(완료 → 소리)와 같습니다.
        /// </summary>
        /// <param name="reaction">끝을 알리는 사망 반응 조각입니다. 조립이 끝을 기다립니다.</param>
        /// <param name="cues">사망 칸 큐입니다(반응 값의 <c>DeathCues</c>).</param>
        /// <param name="camera">맵 카메라 창구입니다. 없거나 흔들림을 낼 수 없으면(수신기 없음) 흔들림 큐를 넣지 않습니다.</param>
        /// <param name="effects">이펙트 창구입니다. 없으면 이펙트 큐를 넣지 않습니다(운영 처리기는 늘 넘김).</param>
        /// <param name="subjectBody">주체 몸 자리입니다(<see cref="CombatHitStart.SubjectBodyOf"/>). 없으면 그 자리의 이펙트를 넣지 않습니다.</param>
        internal static CombatComposition ComposeDeath(EntityId subject, CombatReactionMotionPiece reaction, CombatEventAudio sound,
            CombatEvent death, CombatEventCues cues, CombatCameraWindow camera = null, CombatEffectWindow effects = null, Pose? subjectBody = null)
        {
            if (reaction == null) throw new ArgumentNullException(nameof(reaction));
            if (sound == null) throw new ArgumentNullException(nameof(sound));
            if (death == null) throw new ArgumentNullException(nameof(death));
            if (cues == null) throw new ArgumentNullException(nameof(cues));
            // 사망 반응 조각은 자세를 빌려야 합니다. 그 요구는 여기서 적습니다(피격 쪽은 CombatHitStart의 뼈대 값, 계획 §6.7 ③).
            var composition = CombatComposition.ForUnit(subject, isDeath: true, CombatPieceCompletion.General)
                .Lease(subject, CombatPieceChannel.Pose);
            int reactionItem = composition.Add(reaction, CombatPieceStart.AtStart(), waits: true);
            CombatCueComposer.Add(composition, new CombatCueAnchors().Set(CombatCueAnchor.End, reactionItem, CombatPieceSignal.End), cues,
                CombatCuePoints.ForSubject(subject, subjectBody), new CombatCueSinks(effects, camera, sound, sound.DeathSoundsForComposition(death)));
            return composition;
        }

        /// <summary>
        /// 낭떠러지 추락 조립입니다(임시). 유닛 실행(주체 = 떨어진 유닛, 사망 실행), 일반 완료, 자원은 그 유닛의 자세·크기입니다.
        /// 0 떨어지기(구멍 아래로 깊이만큼, 점점 빨라짐, 기다림) + 1 작아지기(작아지기 시작 지점부터 떨어지기와 같은 순간에 끝남, 기다림)
        /// + 2 사망 소리 줄(시작 순간, 기다리지 않음 — 줄의 시점·지연은 쓰지 않음, 유닛 소리 계획 TP-5). 크게 떨어지는 모습이 보이고 끝 무렵에만 작아집니다(2026-10-29 사용자 결정).
        /// 사망 반응·카메라 흔들림·피격 이펙트는 넣지 않습니다(맞은 것이 아니라 떨어진 것).
        /// </summary>
        internal static CombatComposition ComposeFall(EntityId subject, CombatFallMotionSettings settings, Vector3 worldUp,
            CombatEventAudio sound, CombatEvent death)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (sound == null) throw new ArgumentNullException(nameof(sound));
            var composition = CombatComposition.ForUnit(subject, isDeath: true, CombatPieceCompletion.General)
                .Lease(subject, CombatPieceChannel.Pose)
                .Lease(subject, CombatPieceChannel.Scale);
            composition.Add(new CombatSinkPiece(subject, settings.Seconds, settings.Depth, worldUp), CombatPieceStart.AtStart(), waits: true);
            composition.Add(new CombatScalePiece(subject, 1f, settings.EndScale, settings.ShrinkSeconds),
                CombatPieceStart.AtStart(settings.ShrinkDelay), waits: true);
            AddSoundsNow(composition, sound, sound.DeathSoundsForComposition(death));
            return composition;
        }

        /// <summary>
        /// 터질 유닛의 사망 조립입니다(자폭 깜빡임 설계 SD4). 유닛 실행(주체 = 죽는 유닛, 사망 실행), 일반 완료, 자원은 그 유닛의 색 층(Flash)입니다.
        /// 뼈대는 0 깜빡임(시작, 기다림)이고, 그 뒤 사망 칸 큐를 붙입니다 — 시작 = 깜빡임 시작, 끝 = 깜빡임 끝이라
        /// 치명타 피격 이펙트·사망 소리는 지금 사망처럼 맞는 순간에 납니다. 사망 칸 큐에는 플래시가 없습니다(큐 규칙상 피격 칸만).
        /// 반응 설정이 없어 큐가 없으면(<paramref name="cues"/> null) 사망 소리 줄만 시작 순간에 냅니다(추락 조립과 같음).
        /// </summary>
        internal static CombatComposition ComposeSelfDestructDeath(EntityId subject, CombatDeathBlinkVisualData blink, CombatEventAudio sound,
            CombatEvent death, CombatEventCues cues, CombatCameraWindow camera, CombatEffectWindow effects, Pose? subjectBody)
        {
            if (blink == null) throw new ArgumentNullException(nameof(blink));
            if (sound == null) throw new ArgumentNullException(nameof(sound));
            if (death == null) throw new ArgumentNullException(nameof(death));
            // 모든 항목 완료입니다. 지연 큐가 깜빡임보다 늦게 끝나도 사건은 그때 끝나므로, 몸 대여를 거두는 자폭 발동이 실행보다 먼저 시작하지 않습니다(묶음 끝 검토 F1).
            var composition = CombatComposition.ForUnit(subject, isDeath: true, CombatPieceCompletion.AllItems)
                .Lease(subject, CombatPieceChannel.Flash);
            // 깜빡임은 몸 색만 바꿉니다(외곽선 폭 0 — 피격 플래시의 외곽선 모습은 쓰지 않음).
            var look = new CombatFlashLook(blink.Color, blink.Color, 0f);
            int pulses = composition.Add(new CombatFlashPiece(subject, look, CombatFlashPulses.From(blink)), CombatPieceStart.AtStart(), waits: true);
            if (cues != null)
                CombatCueComposer.Add(composition, new CombatCueAnchors().Set(CombatCueAnchor.End, pulses, CombatPieceSignal.End), cues,
                    CombatCuePoints.ForSubject(subject, subjectBody), new CombatCueSinks(effects, camera, sound, sound.DeathSoundsForComposition(death)));
            else
                AddSoundsNow(composition, sound, sound.DeathSoundsForComposition(death));
            return composition;
        }

        // 시계를 쓰지 않는 사망(추락·큐 없는 자폭)의 유닛 소리입니다. 줄마다 시작 순간에 냅니다 — 줄의 시점·지연은 쓰지 않습니다(유닛 소리 계획 TP-5).
        // 사망 줄의 "아직 반영 전" 판단은 부르는 쪽이 조립 때 끝냅니다(CombatEventAudio.DeathSoundsForComposition, 묶음 끝 검토 M1).
        private static void AddSoundsNow(CombatComposition composition, CombatEventAudio sound, CombatUnitSoundSet sounds)
        {
            if (sounds == null) return;
            for (int i = 0; i < sounds.Rows.Count; i++)
            {
                CombatUnitSoundCue row = sounds.Rows[i];
                composition.Add(new CombatEventSoundPiece(sound, () => sound.CueOf(sounds, row)), CombatPieceStart.AtStart());
            }
        }
    }
}
