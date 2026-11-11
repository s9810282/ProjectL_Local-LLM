#if UNITY_EDITOR || DEVELOPMENT_BUILD
using CombatPlaybackProfile = global::ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support.CombatPlaybackProfile;
using CombatEventProfiler = global::ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support.CombatEventProfiler;
#endif
using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Effect.Enum;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Handlers;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack
{
    /// <summary>
    /// 공격 사건(과 조립의 정면 공격 모션)의 연출 재생입니다(구조 리팩토링 설계 D10, 보스 활성화 설계 D6). 코드 모션 셋(Linear·Melee·Mortar)과
    /// 출처 발사(클립 공격의 직선·곡사)의 시작·Tick·취소, 공격 후속 사건 대응(<see cref="CombatHitFollowUps"/>)과 해제(<see cref="CombatHitGate"/>)를 소유합니다.
    /// 투사체 풀·진행·정리는 투사체 진행(<see cref="CombatProjectileFlights"/>)에 맡깁니다(7기준 검사 K2, 클립 계획 T29a).
    /// 종류마다 무엇을 어디로 쏘고 언제 맞는지는 공격 종류 전략(<see cref="CombatAttackStyle"/>, 표 <see cref="CombatAttackStyles"/>)이 정하고,
    /// 이 객체는 수명(Gate·진행·Tick·취소)과 접촉 진행률 계산을 갖고 좁은 맥락(<see cref="ICombatAttackStyleContext"/>)으로 전략에 내줍니다(View 3라운드 W8).
    /// </summary>
    /// <remarks>
    /// <see cref="CombatSceneEventPresenter"/>가 만들고 사건 분배·표시 사실·오디오는 그쪽에 남습니다. 효과음은 시작 때 받은 콜백으로만 냅니다.
    /// 재생 세대는 사건 Presenter가 하나만 소유하고, 이 객체는 읽기 함수로 현재 세대를 확인합니다(늦은 완료 통지 무시).
    /// 다음 사건과의 겹침(<see cref="CanBegin"/>·<see cref="BlocksNextEvent"/>·<see cref="AllowsOverlap"/>·<see cref="Tick"/>의 반환)은
    /// Gate 상태 하나에서 파생합니다(히트 타이밍 설계 H3). 코드 모션 공격·포효는 조립 실행이 출처 수명 창구(<see cref="ICombatSourceLifetime"/>,
    /// <see cref="BeginSource"/>~<see cref="EndSource"/>)로 한 수명을 열고, 모션(<see cref="TryBeginMotion"/>) 타격이 히트입니다
    /// (설계 §6 Q1 재결정, 조각·조립 설계 BP6). 이 객체는 포효 형식이나 순서를 모릅니다. 그것은 포효 조립이 가집니다.
    /// </remarks>
    internal sealed class CombatAttackPlayback : ICombatSourceLifetime, ICombatAttackStyleContext
    {
        private readonly CombatUnitPresentationWindow _presentation;
        private readonly Func<int> _generation;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal CombatEventProfiler Profiler { get; set; }
        private CombatPlaybackProfile _motionProfile = CombatEventProfiler.Unknown;

#endif
        private readonly CombatAttackPresenter _attack = new();
        private readonly CombatProjectileFlights _flights;
        private readonly CombatHitFollowUps _followUps = new();
        private readonly CombatHitGate _gate;
        // 현재 표시 사실의 몸 조회입니다(설계 H4). 사실 목록은 사건 Presenter가 소유하고 여기에는 조회만 있습니다.
        private readonly CombatBodyLookup _bodies;
        // 접촉 칸 투영으로 대신한 진단 경고 기록입니다(설계 §6 Q3). 결과 하나 안에서 같은 이유·대상(키)당 한 번입니다.
        private readonly HashSet<(string Reason, string Subject)> _contactWarnings = new();
        private bool _unavailable;
        private bool _ticking;
        // 투사체 접촉 이펙트를 내는 창구입니다(투사체 접촉 이펙트 설계 IE5, 필수, View 3라운드 W9-3).
        // 회수(재생 취소·해제)는 창구 주인인 사건 Presenter가 합니다.
        private readonly CombatEffectWindow _effects;

        /// <param name="presentation">유닛 표시 창구입니다. 공격 자세·연출 값·곡사 무기를 여기서 빌리고 읽습니다(연출 조립 설계 B13).</param>
        /// <param name="generation">사건 Presenter가 소유한 현재 재생 세대입니다. 취소마다 바뀝니다.</param>
        /// <param name="bodies">대상의 현재 표시 사실로 몸을 찾는 조회입니다. 발사 경로가 정해질 때 한 번 부릅니다.</param>
        /// <param name="effects">투사체 접촉 이펙트 창구입니다(필수).</param>
        internal CombatAttackPlayback(CombatUnitPresentationWindow presentation, Transform projectileRoot, Func<int> generation,
            CombatBodyLookup bodies, CombatEffectWindow effects)
        {
            _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            if (projectileRoot == null) throw new ArgumentNullException(nameof(projectileRoot));
            _generation = generation ?? throw new ArgumentNullException(nameof(generation));
            _bodies = bodies ?? throw new ArgumentNullException(nameof(bodies));
            _flights = new CombatProjectileFlights(projectileRoot, generation);
            _gate = new CombatHitGate(_followUps);
            _effects = effects ?? throw new ArgumentNullException(nameof(effects));
        }

        /// <summary>공격 애니메이션(화살 비행 포함)이 진행 중인지입니다.</summary>
        internal bool IsPlaying => _attack.IsPlaying;

        /// <summary>공격 중 유닛·무기 표시가 사라져 연출을 끝낼 수 없게 됐는지입니다. 다음 Tick에서 오류로 알립니다.</summary>
        internal bool IsUnavailable => _unavailable;

        /// <summary>다른 공격 출처가 열려 있는지입니다. 조기 공격이 출처 수명을 덮어쓰지 않도록 사용합니다.</summary>
        internal bool HasSource => _gate.IsActive;

        /// <summary>공격이 끝났는지입니다.</summary>
        internal bool IsComplete => !_attack.IsPlaying && !_unavailable;

        /// <summary>
        /// 다음 사건 시작을 막는 공격 상태가 있는지입니다. <paramref name="allowAttackOverlap"/>면 사건 완료를 알린 뒤 남은 공격 연출과의 겹침은
        /// 막지 않습니다(어떤 사건이 겹쳐도 되는지는 <see cref="CanBegin"/>이 답합니다).
        /// </summary>
        internal bool BlocksNextEvent(bool allowAttackOverlap) =>
            (_attack.IsPlaying && (!allowAttackOverlap || !_gate.AllowsOverlap)) || _unavailable;

        /// <summary>결과 하나의 재생을 시작합니다. 공격 후속 사건 대응을 이 결과로 다시 만듭니다.</summary>
        internal void BeginResult(CombatResult result)
        {
            if (_attack.IsPlaying) throw new InvalidOperationException("이전 결과의 공격 표시가 남아 있습니다.");
            _gate.Clear();
            _contactWarnings.Clear();
            _followUps.Begin(result);
        }

        /// <summary>남은 공격 연출과 겹쳐 이 사건을 시작해도 되는지입니다. 반응·걸음 같은 다른 재생 자원은 실행 목록(<see cref="CombatPresentationPlayers"/>)이 함께 봅니다.</summary>
        internal bool CanBegin(CombatEvent item) => _gate.CanBegin(item);

        /// <summary>이 결과에서 실제 공격 히트에 대응하는 피해 대상 사건인지 확인합니다.</summary>
        internal bool IsAttackHit(CombatCharacterEvent item) =>
            HitSourceOf(item) != null;

        /// <summary>기존 후속 대응표에서 히트로 해제되는 대상의 출처만 읽습니다.</summary>
        internal CombatEvent HitSourceOf(CombatEvent item) =>
            _followUps.TryGet(item, out var followUp) && followUp.ReleasedByHit &&
            _followUps.IsHitTarget(followUp.Source, followUp.Target) ? followUp.Source : null;

        /// <summary>
        /// 활성 출처(공격·포효)가 사건 완료를 알렸고 아직 전체 종료 전인지입니다. 이때만 남은 연출(포효 작아지기 포함)과 다음 사건이 겹칠 수 있습니다.
        /// </summary>
        internal bool AllowsOverlap => _gate.AllowsOverlap;

        /// <summary>
        /// 행위자에게 시작할 수 있는 코드 모션이 있는지입니다(범용 연출 구조 계획 Gc.5). 연출 설정이 있고 코드 모션 부품이 있어야 하며(계획 P1),
        /// 공격 사건이 없는 정면 공격(<paramref name="facing"/>)은 그 종류가 정면 공격을 지원해야 합니다. 처리기가 조립을 만들기 전에 묻습니다.
        /// 아니면 처리기는 지금처럼 일반 사건(틀의 일반 소리)으로 이어 갑니다.
        /// </summary>
        internal bool HasCodeMotion(EntityId actorId, bool facing) =>
            _presentation.TryGetAttackPresentation(actorId, out var settings) && HasCodeMotion(settings, facing);

        /// <summary>그 공격 표시 값에 시작할 수 있는 코드 모션이 있는지입니다. 판정 규칙은 행위자 판(<see cref="HasCodeMotion(EntityId, bool)"/>)과 같습니다.</summary>
        internal static bool HasCodeMotion(CombatAttackPresentationData settings, bool facing) =>
            settings != null && settings.HasCodeMotion && (!facing || CombatAttackStyles.Of(settings.Kind).SupportsFacingMotion);

        /// <summary>
        /// 출처 하나의 Gate 수명을 시작합니다(조각·조립 설계 BP6). 연출 시작보다 먼저 부르고, 사건 완료는 정책(<paramref name="completion"/>)을 따라
        /// 세대가 같을 때만 전합니다. 끝은 <see cref="EndSource"/>, 시작 실패·취소는 <see cref="AbortSource"/>입니다.
        /// 어떤 출처가 언제 열리고 닫히는지는 그 사건의 조립이 정합니다(포효는 <see cref="CombatRoarEventHandler.Compose"/>).
        /// </summary>
        public void BeginSource(CombatEvent source, EntityId actorId, CombatHitCompletion completion, Action completed, int version)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var sourceProfile = Profiler?.Of(source) ?? CombatEventProfiler.Unknown;
            _gate.Profile = sourceProfile;
#endif
            _gate.Begin(source, actorId, completion, () =>
            {
                if (version == _generation())
                    completed?.Invoke();
            });
        }

        /// <summary>출처의 연출과 자원 반환이 정상으로 끝났음을 알립니다. 아직 사건 완료 전이면 지금 알립니다. 세대가 다르면 무시합니다.</summary>
        public void EndSource(CombatEvent source, int version)
        {
            if (version != _generation()) return;
            _gate.EndAnimation(source);
        }

        /// <summary>그 출처가 아직 활성일 때만 Gate를 비웁니다. 완료를 알리지 않습니다.</summary>
        public void AbortSource(CombatEvent source) => _gate.ClearIf(source);

        /// <summary>
        /// 코드 모션을 시작합니다(범용 연출 구조 계획 P8 — 옛 공격 입구와 정면 공격 입구를 하나로). Gate를 열지 않습니다.
        /// 출처의 Gate 수명은 이 모션을 담은 조립 실행이 <see cref="BeginSource"/>~<see cref="EndSource"/>로 맡습니다(코드 모션 공격 조립·포효 조립).
        /// </summary>
        /// <param name="shape">공격 모양입니다(특성 발동 연출 설계 TA5). 그 타격 칸·공격 방향을 목표로 씁니다. null이면 정면 공격 모션으로, 행위자가 지금 보는 방향 앞을 목표로 씁니다.
        /// 전체(All) 지정은 방향이 없어 Core 결과의 공격 방향이 기본값(R0)이라, 그 방향을 쓰면 적 보스의 동작이 플레이어 반대쪽으로 재생되기 때문입니다.
        /// 모양의 공격자는 <paramref name="actorId"/>와 같아야 합니다.</param>
        /// <param name="cue">기준 순간 콜백입니다(근접 = 타격, 직선·곡사 = 발사, 옛 효과음 순간). 근접은 이 콜백 뒤 같은 순간에 <paramref name="hit"/>을 부릅니다.</param>
        /// <param name="hit">타격 순간입니다. 대상 해제와 그 뒤 신호를 호출자가 묶어 넘깁니다(<see cref="CombatHitMoment.ReleaseThenSignal"/>). 투사체 종류는 접촉 이펙트 뒤에 부릅니다.</param>
        /// <param name="end">모션의 전체 완료입니다. 세대가 같을 때만 부릅니다.</param>
        /// <param name="settings">공격 표시 덮어쓰기입니다(특성 추가 공격 줄의 공격 표시, 특성 발동 연출 결정 Q-TA3 B). 없으면 행위자 외형의 공격 표시입니다.</param>
        /// <returns>시작했으면 true, 시작할 코드 모션이 없으면(<see cref="HasCodeMotion(CombatAttackPresentationData, bool)"/>이 아님, 곡사 무기 없음) false입니다.</returns>
        internal bool TryBeginMotion(EntityId actorId, CombatAttackShape shape, FieldGridMapper mapper, Action cue, Action hit, Action end, int version,
            CombatAttackPresentationData settings = null)
        {
            if (shape != null && shape.AttackerId != actorId)
                throw new ArgumentException($"[CombatAttackPlayback] 공격 모양의 공격자({shape.AttackerId})가 모션 행위자({actorId})와 다릅니다.", nameof(shape));
            // 표시가 없는 공격자는 종류와 관계없이 false입니다(계약). 덮어쓰기 값이 있어도 표시는 봅니다(평가 C10-12).
            if (settings == null ? !_presentation.TryGetAttackPresentation(actorId, out settings) : !_presentation.HasTarget(actorId))
                return false;
            if (!HasCodeMotion(settings, shape == null))
                return false;
            // 종류마다의 코드 모션은 그 종류의 전략이 정합니다(View 3라운드 W8).
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _motionProfile = Profiler?.AttackOf(actorId) ?? CombatEventProfiler.Unknown;
            _flights.Profile = _motionProfile;
#endif
            return CombatAttackStyles.Of(settings.Kind).TryBeginCodeMotion(this, actorId, shape, settings, mapper, hit, cue, end, version);
        }

        /// <summary>
        /// 코드 모션 애니메이션 수명을 시작합니다. 끝(<paramref name="ended"/>)은 <see cref="CombatAttackPresenter"/>가 완료를 확인하고 자원을 반환한
        /// 정상 완료 콜백에서만, 세대가 같을 때 알립니다. Gate는 조립 실행이 맡으므로 여기서 열지 않습니다(계획 P8).
        /// </summary>
        private void BeginAnimation(ICombatAttackAnimation animation, Action ended, int version) =>
            _attack.Begin(animation, () =>
            {
                if (version == _generation())
                    ended?.Invoke();
            });

        /// <summary>공격(과 날고 있는 투사체)을 한 프레임 진행합니다.</summary>
        /// <param name="fieldBound">사건 Presenter에 필드(Mapper)가 연결돼 있는지입니다. 공격 진행 중인데 끊겨 있으면 오류입니다.</param>
        /// <returns>
        /// 이 프레임에 반응·이동 진행을 계속해도 되면 true입니다. 공격 진행 중 세대가 바뀌었거나,
        /// 이 프레임 시작 때 사건 완료 전(겹침 불가)의 공격이 진행 중이었으면 false입니다.
        /// </returns>
        internal bool Tick(float deltaTime, bool fieldBound)
        {
            if (_unavailable)
                throw new InvalidOperationException("[CombatAttackPlayback] 공격 중 유닛·무기 표시가 비활성화됐습니다.");
            if (!_attack.IsPlaying)
                return !_flights.HasShots || TickShotsOnly(deltaTime);

            // 프레임 시작 때의 겹침 허용으로 판단합니다. 이 Tick 안의 완료 콜백에서 시작한 반응에 같은 프레임 시간을 다시 쓰지 않습니다.
            bool overlapping = _gate.AllowsOverlap;
            if (!fieldBound)
                throw new InvalidOperationException("[CombatAttackPlayback] 필드 구성 성공 후 Mapper 연결이 필요합니다.");
            if (_ticking)
                throw new InvalidOperationException("[CombatAttackPlayback] 공격 Tick을 중첩 호출할 수 없습니다.");
            int attackVersion = _generation();
            _ticking = true;
            try
            {
                if (!_flights.Tick(deltaTime, attackVersion))
                    return false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                using (_motionProfile.MotionTick.Auto())
                {
                    _flights.Profile = _motionProfile;
#endif
                _attack.Tick(deltaTime);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                }
#endif
            }
            finally
            {
                _ticking = false;
            }
            return attackVersion == _generation() && overlapping;
        }

        /// <summary>
        /// 코드 모션 없이 출처 발사(클립 공격)만 날고 있을 때의 진행입니다. 다른 진행을 막지 않습니다(막기·완료는 조각 실행과 Gate, 계획 P12).
        /// 진행 중 콜백이 재생 세대를 바꾸면 false입니다.
        /// </summary>
        private bool TickShotsOnly(float deltaTime)
        {
            if (_ticking)
                throw new InvalidOperationException("[CombatAttackPlayback] 공격 Tick을 중첩 호출할 수 없습니다.");
            int version = _generation();
            _ticking = true;
            try
            {
                return _flights.Tick(deltaTime, version);
            }
            finally
            {
                _ticking = false;
            }
        }

        /// <summary>
        /// 출처 발사(클립 공격의 발사 조각)입니다(클립 설계 5차 AC12, 계획 T29b). 목표점과 맞는 순간은 종류의 전략이 정하고
        /// (곡사 = 주 칸 중심 착탄, 직선 = 발사 높이로 올린 목표점·주 대상 몸 접촉 진행률, View 3라운드 W8),
        /// 날리기·진행·회수는 투사체 진행이 합니다. 근접은 쏘지 않아 예외입니다.
        /// <paramref name="hit"/>은 맞는 순간, <paramref name="arrived"/>는 끝에 닿은 순간이며(곡사는 같은 순간, 맞음 먼저) 재생 세대가 같을 때만 부릅니다.
        /// </summary>
        /// <param name="shape">공격 모양입니다(특성 발동 연출 설계 TA5).</param>
        /// <param name="from">발사 지점(포구)입니다. 직선의 목표점 해석 출발점도 이 점입니다(계획 P11).</param>
        internal ICombatShot FireShot(CombatAttackShape shape, CombatAttackPresentationData settings, Vector3 from, FieldGridMapper mapper,
            Action hit, Action arrived, int version)
        {
            if (shape == null) throw new ArgumentNullException(nameof(shape));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _flights.Profile = Profiler?.AttackOf(shape.AttackerId) ?? CombatEventProfiler.Unknown;
#endif
            Vector3 up = mapper.WorldUp.normalized;
            // 접촉 자리가 수평 길이 없이 정해질 때의 회전입니다(코드 모션과 같은 공격 방향, 계획 P5).
            Quaternion attackFacing = mapper.RotationToWorld(shape.Direction);
            return CombatAttackStyles.Of(settings.Kind).FireShot(this, shape, settings, from, mapper, up, attackFacing, hit, arrived, version);
        }

        /// <summary>
        /// 이 공격 연출 값의 투사체를 한 발 미리 만들어 둡니다(GC 끊김 설계 GH6, 계획 TP-6). 한 공격에 한 발이 날고 맞으면 바로 풀로 돌아와 하나면 됩니다.
        /// 투사체를 쏘지 않는 종류(근접)는 아무것도 하지 않습니다. 전투 설치 때 사건 Presenter가 부릅니다.
        /// </summary>
        internal void Prewarm(CombatAttackPresentationData settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (CombatUnitAnimationRules.FiresProjectile(settings.Kind))
                _flights.Prewarm(settings.ProjectilePrefab, 1);
        }

        /// <summary>진행 중 공격·투사체를 버리고 되돌립니다. 재생 세대는 호출자가 먼저 올립니다. 완료를 알리지 않습니다.</summary>
        internal void Cancel()
        {
            _unavailable = false;
            _gate.Clear();
            _followUps.Clear();
            _contactWarnings.Clear();
            RunCleanup(_attack.Cancel, _flights.Clear);
        }

        /// <summary>공격 애니메이션 자원과 투사체 풀을 최종 해제합니다.</summary>
        internal void Release() => RunCleanup(_attack.Release, _flights.Release);

        /// <summary>
        /// 투사체 접촉의 맞음입니다(투사체 접촉 이펙트 설계 IE5). 접촉 자리에 그 공격 표시의 접촉 이펙트를 낸 뒤 기존 맞음(해제)을 부릅니다.
        /// 늦은 접촉은 애니메이션이 자기 세대로 걸러 이 콜백을 부르지 않습니다.
        /// </summary>
        private Action<CombatProjectileContact> ImpactThen(CombatAttackPresentationData settings, Action hit) =>
            contact =>
            {
                PlayImpact(settings, contact);
                hit?.Invoke();
            };

        /// <summary>발사 때 접촉 자리가 정해지는 출처 발사(클립 원거리)의 맞음입니다. 늦은 접촉은 투사체 진행이 세대로 거릅니다.</summary>
        private Action ImpactThen(CombatAttackPresentationData settings, Action hit, CombatProjectileContact contact) =>
            () =>
            {
                PlayImpact(settings, contact);
                hit?.Invoke();
            };

        /// <summary>
        /// "투사체가 닿으면 무엇을 내나"의 한 곳입니다(설계 IE5, 범용 연출 구조 계획 Gd.7). 공격 칸 큐 가운데 접촉(Contact) 시점 이펙트를 접촉 자리에 냅니다.
        /// 접촉 시점은 이펙트만 두고 지연은 0이라(계획 P5) 조립 항목이 아니라 여기서 바로 냅니다. 자리 해석은 큐 자리 한 곳(<see cref="Pieces.CombatCuePoints.Contact"/>)입니다.
        /// 낸 이펙트의 회수는 창구 주인(사건 Presenter)이 재생 취소·해제 때 합니다(클립 이펙트 결정 Q-E4 A).
        /// </summary>
        private void PlayImpact(CombatAttackPresentationData settings, CombatProjectileContact contact)
        {
            IReadOnlyList<CombatEffectCue> effects = settings.Cues.Effects;
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i].Anchor != CombatCueAnchor.Contact) continue;
                // 접촉 자리의 보정은 여기서 적용합니다(이펙트 창구 확장 계획 P3).
                Pose at = Pieces.CombatCuePoints.Apply(Pieces.CombatCuePoints.Contact(contact), effects[i]);
                _effects.Play(effects[i].Effect, at, CombatEffectPlacement.ForCue(effects[i], EntityId.Empty));
            }
        }

        /// <summary>세대가 같을 때만 이 출처의 대상을 모두 해제합니다. 코드 모션의 타격(조립이 <see cref="CombatHitMoment.ReleaseThenSignal"/>로 묶음)과 클립 공격의 해제 조각이 같은 입구를 씁니다.</summary>
        public void ReleaseAll(CombatEvent source, int version)
        {
            if (version == _generation()) _gate.ReleaseAll(source);
        }

        /// <summary>
        /// 직선 투사체의 히트 진행률을 발사 경로가 정해질 때 한 번 정합니다(설계 H6). 주 대상이 없거나(방향·전체 지정) 몸 조회에 실패하면 종점 도착(1)입니다.
        /// 몸과 궤적이 닿지 않으면 주 셀 투영(설계 §6 Q3 B)이며 주 셀 사실이 없으면 오류입니다.
        /// 주 대상이 가로채졌으면 실제로 맞은 수호자 몸(옮긴 자리)으로 잽니다(<see cref="CombatAttackShape.ContactTargetId"/>, 유닛 행동 설계 P5).
        /// </summary>
        private float LinearContactProgress(CombatAttackShape shape, Vector3 from, Vector3 to, FieldGridMapper mapper)
        {
            EntityId target = shape.ContactTargetId;
            if (target.IsEmpty || !_bodies(target, out CombatBody body, out EntityKey key))
                return 1f;
            return ContactProgress(shape, target, key.ToString(), body, shape.DeliveryCell, from, to, mapper);
        }

        /// <summary>
        /// 접촉 시각 결정 경계입니다(설계 H5·§6 Q3). 점 투사체와 몸의 처음 닿는 진행률이고, 닿지 않으면
        /// Core가 준 접촉 칸(<paramref name="cell"/>)을 경로에 투영한 진행률입니다. 대체할 때는 진단 경고를 남기되 점 몸의 미교차는
        /// 콜라이더 없음 경고(<see cref="FieldUnitPresenter.TryGetBodyShape"/>)가 원인을 이미 알려 내지 않습니다. 경고는 진행을 바꾸지 않습니다.
        /// 몸 조회 실패는 부르는 쪽(직선)이 종점 도착으로 이미 처리했습니다(옛 Ballista 관통 길만 실패한 몸으로 불렀고, 그 길과 함께 갈래를 걷음).
        /// </summary>
        /// <param name="subject">경고에 쓸 대상 이름입니다(외형 키).</param>
        private float ContactProgress(CombatAttackShape shape, EntityId target, string subject, CombatBody body, CellCoord? cell,
            Vector3 from, Vector3 to, FieldGridMapper mapper)
        {
            Vector2 start = CombatBody.Flatten(mapper.WorldToLocal(from));
            Vector2 end = CombatBody.Flatten(mapper.WorldToLocal(to));
            if (CombatContactGeometry.TrySegmentContact(start, end, Vector2.zero, body, out float time))
                return time;
            if (!cell.HasValue)
                throw new InvalidOperationException(
                    $"[CombatAttackPlayback] 접촉 시각을 대신할 Core 접촉 칸 사실이 없습니다: 공격자 {shape.AttackerId}, 대상 {target}.");
            if (!body.IsPoint)
                WarnContact("몸·궤적 미교차", shape, target, subject, "프리팹 BoxCollider가 점유 칸을 덮는지 확인하세요");
            return CombatContactGeometry.ProjectOnSegment(start, end, CombatBody.Flatten(mapper.CellCenterToLocal(cell.Value)));
        }

        private void WarnContact(string reason, CombatAttackShape shape, EntityId target, string subject, string hint)
        {
            if (!_contactWarnings.Add((reason, subject))) return;
            Debug.LogWarning($"[CombatAttackPlayback] {reason}: 공격자 {shape.AttackerId}, 대상 {target}({subject}). " +
                $"Core가 준 접촉 칸을 경로에 투영한 시각으로 대신합니다. {hint}.");
        }

        private void CancelUnavailable()
        {
            // 성공으로 넘기면 후속 피해가 선반영된다. 자원을 먼저 정리하고 다음 Tick에서 기존 오류 경계에 알린다.
            _unavailable = true;
            _attack.Cancel();
        }

        private static void RunCleanup(params Action[] actions) =>
            CleanupErrors.RunAll("[CombatAttackPlayback] 공격 표시 정리에 실패했습니다.", actions);

        // 공격 종류 전략에 내주는 좁은 맥락입니다(View 3라운드 W8). 모두 이 객체의 수명·계산을 그대로 넘깁니다.
        ICombatAttackUnitPose ICombatAttackStyleContext.BeginAttackPose(EntityId attackerId, Action cancelled) =>
            _presentation.BeginAttackPose(attackerId, cancelled);

        bool ICombatAttackStyleContext.TryGetMortarWeapon(EntityId attackerId, out CatusCannonWeaponView weapon) =>
            _presentation.TryGetMortarWeapon(attackerId, out weapon);

        CombatProjectilePool ICombatAttackStyleContext.PoolOf(GameObject prefab) => _flights.PoolOf(prefab);

        ICombatShot ICombatAttackStyleContext.FireArc(GameObject prefab, Vector3 from, Vector3 to, float duration, float height, Vector3 up,
            Vector3 spin, CombatProjectileEasing easing, Action hit, Action arrived, int version, CombatArcRotationMode rotationMode) =>
            _flights.FireArc(prefab, from, to, duration, height, up, spin, easing, hit, arrived, version, rotationMode);

        ICombatShot ICombatAttackStyleContext.FireLinear(GameObject prefab, Vector3 from, Vector3 to, float speed, Vector3 up, float hitAt,
            CombatProjectileEasing easing, Action hit, Action arrived, int version) =>
            _flights.FireLinear(prefab, from, to, speed, up, hitAt, easing, hit, arrived, version);

        void ICombatAttackStyleContext.BeginAnimation(ICombatAttackAnimation animation, Action ended, int version) =>
            BeginAnimation(animation, ended, version);

        Action<CombatProjectileContact> ICombatAttackStyleContext.ImpactThen(CombatAttackPresentationData settings, Action hit) =>
            ImpactThen(settings, hit);

        Action ICombatAttackStyleContext.ImpactThen(CombatAttackPresentationData settings, Action hit, CombatProjectileContact contact) =>
            ImpactThen(settings, hit, contact);

        float ICombatAttackStyleContext.LinearContactProgress(CombatAttackShape shape, Vector3 from, Vector3 to, FieldGridMapper mapper) =>
            LinearContactProgress(shape, from, to, mapper);

        void ICombatAttackStyleContext.CancelUnavailable() => CancelUnavailable();
    }
}
