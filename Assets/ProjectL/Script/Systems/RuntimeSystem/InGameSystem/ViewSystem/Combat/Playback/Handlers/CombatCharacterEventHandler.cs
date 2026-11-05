using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
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
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Reaction;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Handlers
{
    /// <summary>
    /// 캐릭터 사건의 표시입니다(Presenter 분리 설계 S3). 피해 숫자를 보이고, 사망·피격 반응을 사실 반영 전에 시작합니다.
    /// 반응을 시작하지 못한 사건(반응 설정 없음, 이동·복귀 등)은 사실만 반영합니다. 기차 동시 사망도 같은 경로입니다.
    /// 반응 항목은 부품(<see cref="CombatUnitReactionSet"/>), 반응 불가 상태는 반응 재생기(<see cref="CombatReactionPlayer"/>)가 원본이고,
    /// 여기서는 부품의 시작 API와 재생기의 불가 콜백만 씁니다(연출 조립 설계 B12).
    /// 사망은 사망 조립(<see cref="ComposeDeath"/>: 사망 반응 + 사망 칸 큐), 피격은 피격 조립(<see cref="ComposeHit"/>: 피격 반응 + 피격 클립 + 피격 칸 큐)을
    /// 조각 실행 재생기로 진행합니다(조각·조립 설계 BP8·BP9). 사망 유지·반응 자세 대여와 사실 반영은 지금처럼 이 처리기가 합니다(설계 BP1·BP4).
    /// 플래시·흔들림·이펙트·소리 시점은 반응 값의 칸 큐가 정하고 큐 붙이기(<see cref="CombatCueComposer"/>) 한 곳이 항목으로 붙입니다(범용 연출 구조 설계 G6).
    /// 처리기는 주체 몸 자리(<see cref="SubjectBodyOf"/>, 몸 중심·맞은 방향)만 구해 넘깁니다.
    /// 낭떠러지에 떨어진 유닛(보인 사실의 추락 표지)의 사망은 사망 반응 대신 추락 조립(<see cref="ComposeFall"/>)을 씁니다(임시).
    /// </summary>
    internal sealed class CombatCharacterEventHandler : CombatEventHandler
    {
        private readonly CombatUnitHudFeedback _hud;
        private readonly CombatUnitPresentationWindow _presentation;
        private readonly CombatUnitReactionSet _reaction;
        private readonly Action _cancelUnavailableReaction;
        private readonly Func<FieldGridMapper> _mapper;
        private readonly CombatShownFacts _facts;
        private readonly CombatEventAudio _sound;
        private readonly CombatPieceRunPlayer _pieces;
        private readonly Func<int> _generation;
        private readonly CombatCameraWindow _camera;
        private readonly CombatEffectWindow _effects;
        private readonly CombatFallMotionSettings _fallMotion;
        private readonly ICombatTornadoField _tornado;
        private readonly System.Random _tornadoSeeds = new();

        /// <param name="hud">유닛 HUD 일시 피드백입니다. 피해 숫자만 씁니다(연출 조립 설계 §9 I-2).</param>
        /// <param name="presentation">유닛 표시 창구입니다. 반응 설정·사망 유지·반응 자세를 여기서 읽고 빌립니다(연출 조립 설계 B13).</param>
        /// <param name="pieces">조각 실행 재생기입니다. 사망 조립(조각·조립 설계 BP8)과 피격 조립(BP9)을 넘깁니다.</param>
        /// <param name="generation">사건 Presenter의 재생 세대입니다(D10). 콜백 뒤 세대가 바뀌었으면 완료·반영을 하지 않습니다.</param>
        /// <param name="camera">맵 카메라 창구입니다(필수). 흔들림을 낼 수 없으면(수신기 없음) 피격·사망 칸의 흔들림 큐를 넣지 않습니다.</param>
        /// <param name="effects">이펙트 창구입니다(피격 이펙트 설계 HE5, 필수, View 3라운드 W9-3).</param>
        /// <param name="fallMotion">낭떠러지 추락 연출 값입니다(임시, 필수).</param>
        /// <param name="tornado">토네이도 표시입니다. 던짐 연출 설정을 읽습니다(토네이도 View 설계 TN5, 필수).</param>
        internal CombatCharacterEventHandler(CombatUnitHudFeedback hud, CombatUnitPresentationWindow presentation,
            CombatUnitReactionSet reaction, Action cancelUnavailableReaction, Func<FieldGridMapper> mapper, CombatShownFacts facts,
            CombatEventAudio sound, CombatPieceRunPlayer pieces, Func<int> generation,
            CombatCameraWindow camera, CombatEffectWindow effects,
            CombatFallMotionSettings fallMotion, ICombatTornadoField tornado)
        {
            _hud = hud ?? throw new ArgumentNullException(nameof(hud));
            _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            _reaction = reaction ?? throw new ArgumentNullException(nameof(reaction));
            _cancelUnavailableReaction = cancelUnavailableReaction ?? throw new ArgumentNullException(nameof(cancelUnavailableReaction));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
            _sound = sound ?? throw new ArgumentNullException(nameof(sound));
            _pieces = pieces ?? throw new ArgumentNullException(nameof(pieces));
            _generation = generation ?? throw new ArgumentNullException(nameof(generation));
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _effects = effects ?? throw new ArgumentNullException(nameof(effects));
            _fallMotion = fallMotion ?? throw new ArgumentNullException(nameof(fallMotion));
            _tornado = tornado ?? throw new ArgumentNullException(nameof(tornado));
        }

        internal override bool TryBeginBeforeFacts(CombatEvent e, Action completed, int version)
        {
            var character = (CombatCharacterEvent)e;
            // 피해 숫자는 모션 설정과 무관하게 사건의 실제 HP 감소만 표시한다. 별도 완료 대기는 추가하지 않는다.
            if (character.Kind == CombatEventKind.HealthChanged || character.Kind == CombatEventKind.Died)
                _hud.ShowDamage(character.Final.EntityId,
                    character.Expected.Health.CurrentHealth - character.Final.Health.CurrentHealth);
            return TryBeginTornado(character, completed, version) ||
                TryBeginDeath(character, completed, version) || TryBeginHit(character, completed, version);
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

        private bool TryBeginDeath(CombatCharacterEvent death, Action completed, int version)
        {
            if (death.Kind != CombatEventKind.Died || death.Expected.Health.IsDead || !death.Final.Health.IsDead ||
                _facts.FindCharacter(death.Final.EntityId)?.Health.IsDead == true ||
                !_presentation.TryGetReactionSettings(death.Final.EntityId, out var settings))
                return false;
            EntityId id = death.Final.EntityId;
            if (_facts.HasFallen(id))
                return BeginFallDeath(death, completed, version);
            var hold = _presentation.HoldDeath(id);
            ICombatUnitPose pose = null;
            CombatUnitReactionPresenter reaction = null;
            try
            {
                pose = _presentation.BeginPose(id, _cancelUnavailableReaction);
                ICombatUnitPose rented = pose;
                // 사망 반응을 시작하고 사망 소리를 낸다. 소리는 보인 사실에 사망을 반영하기 전이다(이미 사망으로 보인 유닛은 소리를 내지 않음).
                // 완료는 반응 끝이며 세대가 같을 때만 알린다. 치명타도 피격 이펙트를 낸다(피격 이펙트 설계 Q-HE3 A).
                CombatComposition composition = ComposeDeath(id,
                    end => reaction = _reaction.BeginDeath(id, settings, rented, _mapper(), hold.Release, end, _cancelUnavailableReaction),
                    _sound, death, settings.DeathCues, _camera, _effects, SubjectBodyOf(id, death, rented));
                if (!_pieces.TryBegin(composition, completed, version))
                    throw new InvalidOperationException("[CombatCharacterEventHandler] 빌릴 채널이 없는 사망 조립은 항상 시작해야 합니다.");
                if (version != _generation()) return true;
                _facts.ApplyCharacter(death.Final);
                _facts.Render();
            }
            catch (Exception error)
            {
                try
                {
                    // 이전 시작 실패가 콜백에서 시작한 새 반응을 취소하지 않습니다.
                    RunCleanup(() => _reaction.Cancel(death.Final.EntityId, reaction),
                        () => pose?.RestorePose(), hold.Release);
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
            return _pieces.TryBegin(composition, fell, version);
        }

        private bool TryBeginHit(CombatCharacterEvent damage, Action completed, int version)
        {
            if (damage.Kind != CombatEventKind.HealthChanged || damage.Expected.Health.IsDead ||
                damage.Final.Health.IsDead || damage.Final.Health.CurrentHealth >= damage.Expected.Health.CurrentHealth ||
                !_presentation.TryGetReactionSettings(damage.Final.EntityId, out var settings))
                return false;
            EntityId id = damage.Final.EntityId;
            var pose = _presentation.BeginPose(id, _cancelUnavailableReaction);
            CombatUnitReactionPresenter reaction = null;
            try
            {
                _facts.ApplyCharacter(damage.Final);
                _facts.Render();
                if (version != _generation())
                {
                    pose.RestorePose();
                    return true;
                }
                // 반응 시작 자리는 지금처럼 사실 반영·다시 그리기 뒤입니다(설계 BP4). 완료는 반응·플래시·피격 클립이 모두 끝날 때입니다.
                _presentation.TryGetHitClip(id, out CombatClipData hitClip);
                CombatComposition composition = ComposeHit(id,
                    end => reaction = _reaction.BeginHit(id, settings, pose, damage, _mapper(), end, _cancelUnavailableReaction),
                    settings.HitCues, _presentation.CanFlash(id), hitClip, _camera, _effects, SubjectBodyOf(id, damage, pose), _sound);
                if (!_pieces.TryBegin(composition, completed, version))
                    throw new InvalidOperationException("[CombatCharacterEventHandler] 피격 조립을 시작하지 못했습니다.");
            }
            catch (Exception error)
            {
                try
                {
                    // 이전 시작 실패가 콜백에서 시작한 새 반응을 취소하지 않습니다.
                    RunCleanup(() => _reaction.Cancel(id, reaction), pose.RestorePose);
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
        /// 주체(맞은·죽는 유닛)의 몸 자리입니다(큐 자리 <c>SubjectBody</c>, 피격 이펙트 설계 HE2·HE3). 보인 사실에서 몸 중심을 찾지 못하면 null이고,
        /// 그 자리의 이펙트 큐는 항목을 넣지 않습니다. 회전 = 맞은 방향(반응 밀림과 같은 함수, 이펙트 +Z가 그 방향).
        /// 피격은 사실 반영 뒤, 사망은 반영 전에 부르며 두 경우 모두 보인 사실의 맞은 유닛은 살아 있습니다.
        /// </summary>
        private Pose? SubjectBodyOf(EntityId id, CombatCharacterEvent e, ICombatUnitPose pose)
        {
            if (!_facts.TryGetBodyCenter(id, out Vector3 center))
                return null;
            FieldGridMapper mapper = _mapper();
            Quaternion rotation = Quaternion.LookRotation(CombatHitDirection.Resolve(e, mapper, pose.Rotation), mapper.WorldUp);
            return new Pose(center, rotation);
        }

        /// <summary>
        /// 사망 조립입니다(조각·조립 설계 BP8). 유닛 실행(주체 = 죽는 유닛, 사망 실행), 일반 완료입니다. 뼈대는 0 사망 반응(외부 진행, 완료를 기다림)이고,
        /// 그 뒤 사망 칸 큐(소리 시점·흔들림·이펙트)를 붙입니다(범용 연출 구조 설계 G6). 기준 시점은 시작(실행 시작)·끝(반응 끝)입니다.
        /// 옛 고정 순서(반응 → 소리 → 흔들림 → 치명타 이펙트, 모두 시작 순간)는 공유 반응 에셋의 사망 칸 큐(소리 Start, 흔들림 Start, 이펙트 Start·SubjectBody)와 같습니다.
        /// 기차 동시 사망은 사망 실행끼리만 겹칩니다(설계 B14 O14). 소리가 기다리지 않아, 반응이 시작 안에서 끝나는 경우에도 옛 순서(완료 → 소리)와 같습니다.
        /// </summary>
        /// <param name="beginReaction">사망 반응을 시작하는 입구입니다. 넘긴 콜백을 반응 끝에 부릅니다.</param>
        /// <param name="cues">사망 칸 큐입니다(반응 값의 <c>DeathCues</c>).</param>
        /// <param name="camera">맵 카메라 창구입니다. 없거나 흔들림을 낼 수 없으면(수신기 없음) 흔들림 큐를 넣지 않습니다.</param>
        /// <param name="effects">이펙트 창구입니다. 없으면(시험 조립) 이펙트 큐를 넣지 않습니다.</param>
        /// <param name="subjectBody">주체 몸 자리입니다(<see cref="SubjectBodyOf"/>). 없으면 그 자리의 이펙트를 넣지 않습니다.</param>
        internal static CombatComposition ComposeDeath(EntityId subject, Action<Action> beginReaction, CombatEventAudio sound,
            CombatEvent death, CombatEventCues cues, CombatCameraWindow camera = null, CombatEffectWindow effects = null, Pose? subjectBody = null)
        {
            if (sound == null) throw new ArgumentNullException(nameof(sound));
            if (death == null) throw new ArgumentNullException(nameof(death));
            if (cues == null) throw new ArgumentNullException(nameof(cues));
            var composition = CombatComposition.ForUnit(subject, isDeath: true, CombatPieceCompletion.General);
            int reaction = composition.Add(new CombatReactionPiece(beginReaction), CombatPieceStart.AtStart(), waits: true);
            CombatCueComposer.Add(composition, new CombatCueAnchors().Set(CombatCueAnchor.End, reaction, CombatPieceSignal.End), cues,
                CombatCuePoints.ForSubject(subject, subjectBody), new CombatCueSinks(effects, camera, sound, death));
            return composition;
        }

        /// <summary>
        /// 낭떠러지 추락 조립입니다(임시). 유닛 실행(주체 = 떨어진 유닛, 사망 실행), 일반 완료, 자원은 그 유닛의 자세·크기입니다.
        /// 0 떨어지기(구멍 아래로 깊이만큼, 점점 빨라짐, 기다림) + 1 작아지기(작아지기 시작 지점부터 떨어지기와 같은 순간에 끝남, 기다림)
        /// + 2 사건 소리(시작 순간, 기다리지 않음). 크게 떨어지는 모습이 보이고 끝 무렵에만 작아집니다(2026-10-29 사용자 결정).
        /// 사망 반응·카메라 흔들림·피격 이펙트는 넣지 않습니다(맞은 것이 아니라 떨어진 것).
        /// </summary>
        internal static CombatComposition ComposeFall(EntityId subject, CombatFallMotionSettings settings, Vector3 worldUp,
            CombatEventAudio sound, CombatEvent death)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            var composition = CombatComposition.ForUnit(subject, isDeath: true, CombatPieceCompletion.General)
                .Lease(subject, CombatPieceChannel.Pose)
                .Lease(subject, CombatPieceChannel.Scale);
            composition.Add(new CombatSinkPiece(subject, settings.Seconds, settings.Depth, worldUp), CombatPieceStart.AtStart(), waits: true);
            composition.Add(new CombatScalePiece(subject, 1f, settings.EndScale, settings.ShrinkSeconds),
                CombatPieceStart.AtStart(settings.ShrinkDelay), waits: true);
            composition.Add(new CombatEventSoundPiece(sound, () => sound.CueOf(death)), CombatPieceStart.AtStart());
            return composition;
        }

        /// <summary>
        /// 피격 조립입니다(조각·조립 설계 BP9, 결정 Q-B5 A·Q-B7 A). 유닛 실행(주체 = 맞은 유닛), 일반 완료입니다.
        /// 뼈대는 0 피격 반응(외부 진행, 기다림)과, 피격 클립이 있으면 클립 채널을 빌린 클립 조각(같은 순간, 기다림, 클립 설계 AC3)입니다.
        /// 그 뒤 피격 칸 큐(플래시·흔들림·이펙트)를 붙입니다(범용 연출 구조 설계 G6). 기준 시점은 시작·기준 순간(피격 클립의 이벤트 타이밍)·끝(반응 끝)입니다.
        /// 플래시는 기다리는 큐라 완료가 플래시 끝까지도 기다립니다. 플래시가 반응보다 길면 그 유닛의 다음 사건이 늦어집니다(의도, 요구 1·3).
        /// 옛 고정 항목(플래시·흔들림·피격 이펙트, 모두 시작 순간)은 공유 반응 에셋의 피격 칸 큐와 같습니다. 같은 순간 항목 순서만 클립 → 플래시로 바뀌었습니다
        /// (기준 순간 큐가 클립 항목을 가리켜야 해서 뼈대가 먼저입니다. 같은 프레임이라 화면은 같습니다).
        /// </summary>
        /// <param name="beginReaction">피격 반응을 시작하는 입구입니다. 넘긴 콜백을 반응 끝에 부릅니다.</param>
        /// <param name="cues">피격 칸 큐입니다(반응 값의 <c>HitCues</c>).</param>
        /// <param name="canFlash">표시가 플래시 외형 층을 쓸 수 있는지입니다(<see cref="CombatUnitPresentationWindow.CanFlash"/>).</param>
        /// <param name="hitClip">맞은 유닛의 피격 클립입니다(<see cref="CombatUnitPresentationWindow.TryGetHitClip"/>). 없으면 null이고 클립 항목을 넣지 않습니다.</param>
        /// <param name="camera">맵 카메라 창구입니다. 없거나 흔들림을 낼 수 없으면(수신기 없음) 흔들림 큐를 넣지 않습니다.</param>
        /// <param name="effects">이펙트 창구입니다. 없으면(시험 조립) 이펙트 큐를 넣지 않습니다.</param>
        /// <param name="subjectBody">주체 몸 자리입니다(<see cref="SubjectBodyOf"/>). 없으면 그 자리의 이펙트를 넣지 않습니다.</param>
        /// <param name="sound">소리 창구입니다. 피격 칸 소리 목록(투사체·피격 소리 설계 PS4)이 씁니다. 없으면(시험 조립) 소리 항목을 넣지 않습니다.</param>
        internal static CombatComposition ComposeHit(EntityId subject, Action<Action> beginReaction, CombatEventCues cues, bool canFlash,
            CombatClipData hitClip = null, CombatCameraWindow camera = null, CombatEffectWindow effects = null, Pose? subjectBody = null,
            CombatEventAudio sound = null)
        {
            if (cues == null) throw new ArgumentNullException(nameof(cues));
            var composition = CombatComposition.ForUnit(subject, isDeath: false, CombatPieceCompletion.General);
            int reaction = composition.Add(new CombatReactionPiece(beginReaction), CombatPieceStart.AtStart(), waits: true);
            var anchors = new CombatCueAnchors().Set(CombatCueAnchor.End, reaction, CombatPieceSignal.End);
            if (hitClip != null)
            {
                composition.Lease(subject, CombatPieceChannel.Clip);
                int clip = composition.Add(new CombatClipPiece(subject, hitClip), CombatPieceStart.AtStart(), waits: true);
                anchors.Set(CombatCueAnchor.Cue, clip, CombatPieceSignal.ClipEvent);
            }
            // 피격 칸에는 소리 시점이 없고(사건 소리 없음) 소리 목록만 있어 소리 사건은 넘기지 않습니다.
            CombatCueComposer.Add(composition, anchors, cues, CombatCuePoints.ForSubject(subject, subjectBody),
                new CombatCueSinks(effects, camera, sound, null, subject, canFlash));
            return composition;
        }

        private static void RunCleanup(params Action[] actions) =>
            CleanupErrors.RunAll(CombatSceneEventPresenter.CleanupFailureMessage, actions);
    }
}
