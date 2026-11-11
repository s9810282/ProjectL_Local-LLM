using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>
    /// 공격 모양 하나의 연출을 시작하는 한 곳입니다(특성 발동 연출 계획 Td.2). 공격 사건 처리기와 특성 처리기(추가 공격)가 함께 씁니다.
    /// 히트·완료는 조립의 출처 완료(모양의 완료 정책)와 공격 재생의 Gate가 알립니다(히트 타이밍 설계 H3).
    /// </summary>
    /// <remarks>
    /// <para>
    /// 공격자 외형에 공격 클립이 있으면 클립 길(근접 <see cref="CombatClipAttack.ComposeMelee"/>·원거리 <see cref="CombatClipAttack.ComposeRanged"/> 조립을
    /// 조각 실행 재생기로), 없으면 코드 모션 길입니다(클립 설계 AC7, 5차 AC15).
    /// 두 길의 공통 규칙(설계 AC7 표): 출처는 모양의 출처 사건, 사건 완료는 모양의 완료 정책(공격 사건은 첫 해제), 해제가 대상의 피격 사건을 엶,
    /// 방향은 같은 함수(<see cref="CombatAttackFacing"/>), 공격 소리는 설정된 시점마다 냅니다(기본 한 번). 다른 점은 해제·소리 시점(코드 모션 = 모션의 타격,
    /// 근접 클립 = 첫 이벤트 + 선딜레이, 원거리 클립 = 지정 이벤트마다 발사·모든 탄 접촉 뒤 해제)과 전체 종료(모션 끝 / 클립 끝과 모든 탄 도착)입니다.
    /// 해제는 어느 종류든 출처의 대상을 모두 해제합니다.
    /// </para>
    /// <para>
    /// 공격 표시 덮어쓰기(특성 추가 공격 줄, 결정 Q-TA3 B)를 받을 수 있습니다. 클립 길은 클립·발사 지점이 공격자 외형 기준이라 덮어쓰기도 같은 계열(근접/원거리)이어야 하고,
    /// 다르면 경고를 한 번 남기고 공격자 자신의 공격 표시로 칩니다(묶음 끝 검토 M2 — 데이터 조합 잘못으로 전투를 멈추지 않음). 덮어쓰기 큐의 행위자 지점 자리는
    /// DB 생성 때 막혀 있습니다(<see cref="CombatPresentationCueRules.UsesActorPoint"/>). 클립 길은 공격자 표시가 꺼졌거나 클립을 쓸 수 없으면 예외입니다
    /// (계획 P4, 코드 길의 자세 대여 예외와 같은 종류).
    /// </para>
    /// <para>
    /// 공격 모양에 가로채기(대신 맞기)가 있으면 대신 맞기 앞 항목(<see cref="CombatGuardLead"/>)이 만든 공격 조립의 앞 항목(<see cref="CombatAttackLead"/>)을
    /// 두 조립에 넘깁니다(유닛 행동 설계 AA6). 수호자는 공격 동작 전에 지켜진 유닛 앞으로 나가고, 맞는 순간 뒤 그 수호자의 후속(피격·사망)이 끝나면 돌아옵니다.
    /// </para>
    /// </remarks>
    internal sealed class CombatAttackStart
    {
        private readonly CombatAttackPlayback _attacks;
        private readonly Func<FieldGridMapper> _mapper;
        private readonly CombatEventAudio _sound;
        private readonly CombatPieceRunPlayer _pieces;
        private readonly CombatUnitPresentationWindow _presentation;
        private readonly CombatEffectWindow _effects;
        private readonly CombatCameraWindow _camera;
        private readonly CombatShownFacts _facts;
        private readonly CombatGuardLead _guards;
        private readonly CombatVolleyDamageNumbers _damageNumbers;
        private readonly CombatVolleyContactHits _contactHits;
        // 계열이 다른 덮어쓰기를 경고한 (공격자, 덮어쓰기)입니다. 연결 동안 한 번씩만 남깁니다.
        private readonly HashSet<(EntityId Attacker, CombatAttackPresentationData Settings)> _mismatchWarned = new();

        /// <param name="pieces">조각 실행 재생기입니다. 공격 조립을 넘깁니다(클립 설계 AC7, 범용 연출 구조 설계 G5).</param>
        /// <param name="presentation">유닛 표시 창구입니다. 공격 클립과 공격 연출 종류를 여기서 읽습니다.</param>
        /// <param name="effects">이펙트 창구입니다. 공격 칸의 이펙트 큐가 씁니다(이펙트 계획 P7·P9).</param>
        /// <param name="camera">맵 카메라 창구입니다. 공격 칸의 흔들림 큐가 씁니다(범용 연출 구조 설계 G2). 흔들림을 낼 수 없으면 그 큐를 넣지 않습니다.</param>
        /// <param name="facts">보인 사실입니다. 공격 칸 큐의 행위자 몸 자리(ActorBody)를 여기서 구합니다.</param>
        /// <param name="guards">대신 맞기 앞 항목을 만드는 곳입니다(묶음 끝 검토 M2).</param>
        /// <param name="damageNumbers">연발 탄별 피해 숫자입니다.</param>
        /// <param name="contactHits">연발 앞 탄의 접촉 피격입니다(2026-11-11). 숫자와 같은 접촉 입구로 묶습니다(<see cref="ContactFeedback"/>).</param>
        internal CombatAttackStart(CombatAttackPlayback attacks, Func<FieldGridMapper> mapper, CombatEventAudio sound,
            CombatPieceRunPlayer pieces, CombatUnitPresentationWindow presentation, CombatEffectWindow effects, CombatCameraWindow camera,
            CombatShownFacts facts, CombatGuardLead guards, CombatVolleyDamageNumbers damageNumbers, CombatVolleyContactHits contactHits)
        {
            _attacks = attacks ?? throw new ArgumentNullException(nameof(attacks));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _sound = sound ?? throw new ArgumentNullException(nameof(sound));
            _pieces = pieces ?? throw new ArgumentNullException(nameof(pieces));
            _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            _effects = effects ?? throw new ArgumentNullException(nameof(effects));
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
            _guards = guards ?? throw new ArgumentNullException(nameof(guards));
            _damageNumbers = damageNumbers ?? throw new ArgumentNullException(nameof(damageNumbers));
            _contactHits = contactHits ?? throw new ArgumentNullException(nameof(contactHits));
            // 클립 이벤트 신호 표를 한 번 건드려, 줄이 빠졌으면 첫 클립 공격이 아니라 전투 연결 때 드러나게 합니다(작은 묶음 설계 P10).
            CombatClipEventSignals.Validate();
        }

        /// <summary>
        /// 공격 모양 하나의 연출을 시작합니다. 시작했으면 true이고, 사건 완료는 그 연출이 알립니다.
        /// 코드 모션 길에서 시작할 모션이 없으면(연출 설정·코드 모션 부품 없음, 범용 연출 구조 계획 P1) false라 호출자가 틀의 일반 경로로 이어 갑니다.
        /// </summary>
        /// <param name="settings">공격 표시 덮어쓰기입니다. 없으면(null) 공격자 외형의 공격 표시를 씁니다.</param>
        internal bool TryBegin(CombatAttackShape shape, CombatAttackPresentationData settings, Action completed, int version)
        {
            if (shape == null) throw new ArgumentNullException(nameof(shape));
            if (_presentation.TryGetAttackClip(shape.AttackerId, out CombatClipAttackData clip))
                return BeginClip(shape, clip, settings, completed, version);
            return BeginCodeMotion(shape, settings, completed, version);
        }

        /// <summary>
        /// 클립이 없는 공격의 코드 모션 조립입니다(범용 연출 구조 설계 G5, 계획 Gc.5). 시작할 모션이 없으면 조립을 만들지 않고 false입니다.
        /// </summary>
        private bool BeginCodeMotion(CombatAttackShape shape, CombatAttackPresentationData settings, Action completed, int version)
        {
            EntityId attacker = shape.AttackerId;
            if (settings == null && !_presentation.TryGetAttackPresentation(attacker, out settings))
                return false;
            if (!CombatAttackPlayback.HasCodeMotion(settings, facing: false))
                return false;
            FieldGridMapper mapper = _mapper();
            CombatComposition composition = CombatCodeMotionAttack.Compose(shape, settings.Cues,
                (end, cue, hit) => _attacks.TryBeginMotion(attacker, shape, mapper, cue, hit, end, version, settings),
                () => _attacks.ReleaseAll(shape.Source, version), SinksFor(shape, CombatProjectileSounds.From(settings)),
                ActorBodyOf(attacker, settings.Cues), _guards.LeadOf(shape));
            return _pieces.TryBegin(composition, completed, version);
        }

        private bool BeginClip(CombatAttackShape shape, CombatClipAttackData clip, CombatAttackPresentationData settings, Action completed,
            int version)
        {
            CombatComposition composition = ComposeClip(shape, clip, settings, version, _attacks, null);
            if (!_pieces.TryBegin(composition, completed, version))
                throw new InvalidOperationException(
                    $"[CombatAttackStart] 클립 공격 조립을 시작하지 못했습니다(공격자 표시가 꺼졌거나 클립을 쓸 수 없음): {shape.AttackerId}");
            return true;
        }

        /// <summary>조기 공격도 같은 조립을 한 번 사용합니다. 선행 사건 중에는 출처와 Hit 신호를 보류합니다.</summary>
        internal bool TryBeginDeferred(CombatAttackShape shape, CombatDeferredAttackSource source, int version,
            Action<CombatPieceRun> unavailable, out CombatPieceRun run)
        {
            run = null;
            if (!_presentation.CanBeginAttackClip(shape.AttackerId) || _pieces.HasActionRun || _pieces.HasUnitRun(shape.AttackerId))
                return false;
            if (!_presentation.TryGetAttackClip(shape.AttackerId, out var clip))
                throw new InvalidOperationException($"조기 공격의 클립 설정이 없습니다: {shape.AttackerId}");
            CombatComposition composition = ComposeClip(shape, clip, null, version, source, () => source.CanRelease);
            return _pieces.TryBeginDeferredAttack(composition, source, version, unavailable, out run);
        }

        private CombatComposition ComposeClip(CombatAttackShape shape, CombatClipAttackData clip,
            CombatAttackPresentationData settings, int version, ICombatSourceLifetime lifetime, Func<bool> canRelease)
        {
            EntityId attacker = shape.AttackerId;
            // 목표점 해석은 외형 종류마다 다릅니다(원본 CombatAttackStyle.TryTarget). 공격 클립 묶음은 공격 연출 설정을 요구합니다(CombatUnitAnimationRules).
            if (!_presentation.TryGetAttackPresentation(attacker, out CombatAttackPresentationData own))
                throw new InvalidOperationException($"[CombatAttackStart] 공격 클립이 있는데 공격 연출 설정이 없습니다: {attacker}");
            if (settings == null)
                settings = own;
            else if (CombatUnitAnimationRules.FiresProjectile(settings.Kind) != CombatUnitAnimationRules.FiresProjectile(own.Kind))
            {
                // 덮어쓰기는 그 특성을 가진 어느 유닛이 쓸지 몰라 부트에서 거를 수 없습니다. 데이터 조합 잘못으로 전투를 멈추지 않고 유닛 자신의 표시로 칩니다(검토 M2).
                if (_mismatchWarned.Add((attacker, settings)))
                    Debug.LogWarning($"[CombatAttackStart] 공격 표시 덮어쓰기({settings.Kind})가 공격 클립 유닛의 계열({own.Kind})과 다릅니다(근접/원거리). "
                        + $"그 유닛의 공격 표시로 칩니다: {attacker}");
                settings = own;
            }
            CombatAttackPresentationKind kind = settings.Kind;
            FieldGridMapper mapper = _mapper();
            Func<ICombatUnitPose, Quaternion> facing = pose => CombatAttackFacing.For(kind, shape, mapper, pose.Position);
            Vector3? launchAimPoint = CombatAttackStyles.Of(kind) is CombatMortarStyle mortar
                ? mortar.LaunchAimPoint(shape, settings, mapper)
                : null;
            // 해제는 종류와 상관없이 이 출처의 대상을 모두 해제합니다(출처 수명 계약, 세대가 같을 때만).
            Action release = () => lifetime.ReleaseAll(shape.Source, version);
            CombatAttackLead lead = _guards.LeadOf(shape);
            // 원거리 판정은 DB 검사와 같은 함수 하나입니다(설계 5차 AC11, 계획 P10).
            // 근접 클립에는 투사체 소리를 넣지 않습니다(옛 입구의 projectileSounds: null).
            CombatComposition composition = CombatUnitAnimationRules.FiresProjectile(kind)
                ? CombatClipAttack.ComposeRanged(shape, clip, settings.Cues, facing,
                    (from, hit, arrived) => _attacks.FireShot(shape, settings, from, mapper, hit, arrived, version), release,
                    SinksFor(shape, CombatProjectileSounds.From(settings)), ActorBodyOf(attacker, settings.Cues), lead, launchAimPoint, canRelease,
                    ContactFeedback(shape.Source, clip.Shots.Count, version))
                : CombatClipAttack.ComposeMelee(shape, clip, settings.Cues, facing, release, SinksFor(shape, null),
                    ActorBodyOf(attacker, settings.Cues), lead, canRelease);
            return composition;
        }

        /// <summary>
        /// 연발 접촉 순번마다 낼 표시입니다: 탄별 숫자, 그다음 앞 탄의 접촉 피격(마지막 탄은 Core 피해 사건이 피격을 냄). 둘 다 없으면 null입니다.
        /// </summary>
        private Action<int> ContactFeedback(CombatEvent source, int count, int version)
        {
            Action<int> numbers = _damageNumbers.ContactNumbers(source, count, version);
            Action<int> hits = _contactHits.ContactHits(source, count, version);
            if (numbers == null) return hits;
            if (hits == null) return numbers;
            return index =>
            {
                numbers(index);
                hits(index);
            };
        }

        /// <summary>
        /// 공격 하나의 창구 묶음입니다(평가 C4-08 2단계, 작은 묶음 설계 SB1). 세 조립 길이 이 한 곳에서 같은 창구·같은 소리 사건(모양의 출처 사건)을 받습니다.
        /// 공격용 창구를 더하면 <see cref="CombatCueSinks.ForAttack"/>의 인자와 이 줄만 고칩니다.
        /// </summary>
        private CombatCueSinks SinksFor(CombatAttackShape shape, CombatProjectileSounds projectileSounds) =>
            CombatCueSinks.ForAttack(_effects, _camera, _sound, shape.Source, projectileSounds);

        /// <summary>
        /// 공격 칸 큐의 행위자 몸 자리입니다(큐 자리 <c>ActorBody</c>, 범용 연출 구조 설계 G3). 그 자리를 쓰는 큐가 없거나 보인 사실에서 몸을 찾지 못하면 null이고,
        /// 그 자리의 이펙트 큐는 항목을 넣지 않습니다. 위치 = 몸 상자 중심, 회전 = 행위자 모델 방향(그 사실대로 서 있을 때).
        /// </summary>
        private Pose? ActorBodyOf(EntityId attacker, CombatEventCues cues)
        {
            if (!UsesActorBody(cues) || !_facts.TryGetBodyCenter(attacker, out Vector3 center))
                return null;
            return new Pose(center, _facts.StandingPoseOf(_facts.FindCharacter(attacker)).rotation);
        }

        private static bool UsesActorBody(CombatEventCues cues)
        {
            for (int i = 0; i < cues.Effects.Count; i++)
                if (cues.Effects[i].Point == CombatCuePoint.ActorBody) return true;
            return false;
        }
    }
}
