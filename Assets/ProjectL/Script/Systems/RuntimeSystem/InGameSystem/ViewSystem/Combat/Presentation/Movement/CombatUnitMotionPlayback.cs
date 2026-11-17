#if UNITY_EDITOR || DEVELOPMENT_BUILD
using CombatPlaybackProfile = global::ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support.CombatPlaybackProfile;
#endif
using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Reaction;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement
{
    /// <summary>
    /// 걸음 레인 하나(유닛 하나)의 회전·보간과, 경로 끝에서 원래 방향으로 돌아가는 회전을 재생합니다(구조 리팩토링 설계 D10, 동시 처리 설계 C5).
    /// 레인들은 <see cref="CombatUnitMotionSet"/>이 유닛별로 소유하고, 결과의 걸음 해석은 레인들이 공유합니다.
    /// 걸음 도중 접촉 원본(오브젝트·타일)에 처음 닿는 순간 그 묶음을 해제합니다(이동 접촉 설계 M3·M4, 해제 원본은 <see cref="CombatStepContactGate"/>).
    /// 걸음 하나의 시간·높이·접촉 시점은 걸음을 시작할 때 이동 종류로 고른 모양(<see cref="CombatStepStyle"/>)에 묻습니다(걸음 모양 설계 ST3).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="CombatUnitMotionSet"/>이 유닛마다 만들고, 걸음 전 대기(적 행동 간격)·표시 사실·효과음은 사건 Presenter(<see cref="CombatSceneEventPresenter"/>)에 남습니다.
    /// 표시 사실 반영과 이동 효과음은 생성 때 받은 콜백으로만 합니다. 재생 세대는 사건 Presenter가 하나만 소유하며,
    /// 이 객체는 읽기 함수로 현재 세대를 확인합니다(표시 중 재진입한 취소 뒤 늦은 진행 무시).
    /// </para>
    /// <para>
    /// 걸음 사건 완료(시작 때 받은 완료 통지)는 첫 접촉 묶음 해제, 묶음이 없으면 보간 끝입니다. 사건 완료 뒤에도 보간은 계속하며(남은 걸음 연출),
    /// 그동안 유닛 표시의 배치는 보간이 소유합니다(창구의 걸음 배치 대여 <see cref="CombatUnitPresentationWindow.BeginPlacement"/>, 설계 M5 (a)).
    /// 경로 끝 방향 복귀가 보간 중에 요청되면 보간 끝 뒤로 미루고 남은 걸음 연출에 포함합니다(M3).
    /// </para>
    /// </remarks>
    internal sealed class CombatUnitMotionPlayback
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static readonly CombatPlaybackProfile.ByOwner _voluntaryProfiles = new("Move.Voluntary");
        private static readonly CombatPlaybackProfile.ByOwner _forcedProfiles = new("Move.Forced");
        private static readonly CombatPlaybackProfile.ByOwner _pullProfiles = new("Move.Pull");
        private static readonly CombatPlaybackProfile.ByOwner _facingProfiles = new("Move.FacingRestore");
        private CombatPlaybackProfile _movementProfile = _voluntaryProfiles.Unknown;
        private CombatPlaybackProfile _facingProfile = _facingProfiles.Unknown;

#endif
        private readonly CombatUnitPresentationWindow _presentation;
        private readonly Func<CharacterCombatFacts, Quaternion, FieldGridMapper, CombatBody> _moverBody;
        // 이동 종류 → 걸음 모양 표입니다(걸음 모양 설계 ST2). 레인 집합이 받은 표 하나를 레인들이 공유합니다.
        private readonly CombatStepStyles _styles;
        private readonly CombatLeapMotionSettings _leapMotion;
        private readonly float _turnSeconds;
        private readonly Func<int> _generation;
        private readonly Action<CharacterCombatFacts> _show;
        private readonly Action<CharacterCombatFacts> _playMovementSound;
        private readonly Func<ObjectSourceFact, FieldGridMapper, CombatBody> _objectBody;
        private readonly CombatStepContactGate _contacts;
        // 현재 걸음의 사건 완료 통지입니다(동시 처리 설계 C6). 걸음당 한 번, 같은 재생 세대에서만 부릅니다.
        private Action _stepCompleted;
        private int _stepVersion;
        private readonly List<float> _contactProgress = new();
        private CombatCharacterEvent _moving;
        private AttackPullSnapshot _pull;
        private CharacterCombatFacts _pullFrom;
        private int _pullIndex;
        private float _elapsed;
        private bool _movementBegan;
        private bool _stepPrepared;
        // 현재 걸음(끌어오기 포함)의 모양과, 걸음이 스스로 하는 이동인지(걸음 전 회전)입니다. 걸음·끌어오기를 시작할 때마다 정하고
        // 그동안만 읽으므로 취소 때 비우지 않습니다. 회전 여부는 걸음에만 씁니다(끌어오기는 늘 끌려가는 방향으로 돎).
        private CombatStepStyle _style;
        private bool _selfDriven;
        private bool _turning;
        private Quaternion _turnFrom;
        private Quaternion _turnTo;
        private float _turnElapsed;
        // 이번 회전의 시간입니다. 걸음 전 회전·방향 복귀는 회전 시간, 밀린 경로 끝 펴기는 젖히는 시간입니다(넉백 피격 연출, 임시).
        private float _turnDuration;
        // 밀리는 동안 젖힌 자세입니다(넉백 피격 연출, 임시). 밀림 걸음을 시작할 때마다 부르고 걸음 Tick이 오르기를 진행합니다.
        private readonly CombatPushLean _pushLean;
        private CharacterCombatFacts _facingRestore;
        private Action _facingRestoreCompleted;
        private bool _facingRestoreDeferred;
        // 보간 배치 대여입니다(설계 M5 (a)). 걸음 시작 표시 뒤 ~ 보간 끝·취소까지입니다.
        private ICombatUnitPlacement _placement;
        // 끝난 중간 걸음의 공중 위치와 구간 호입니다. 다음 원본 걸음까지 대여를 유지합니다.
        // 그동안 대여는 양보할 수 있게 표시해, 자세·서는 자리를 빌리면 창구가 배치를 빼고 아래 콜백을 부릅니다(평가 C4-05).
        private CombatLeapStepStyle _continuedLeap;
        // 창구가 경유 배치를 양보했을 때 부르는 콜백입니다. 레인마다 한 번 만들어 표시할 때마다 새로 할당하지 않습니다.
        private readonly Action _placementYielded;

        /// <param name="presentation">유닛 표시 창구입니다. 걸음 배치·표시 회전을 여기서 빌립니다(연출 조립 설계 B13).</param>
        /// <param name="moverBody">사실 칸 배치에 표시 회전을 둔 이동자 몸입니다(이동 접촉 설계 M2).</param>
        /// <param name="styles">이동 종류 → 걸음 모양 표입니다. 걸음 시간·높이·접촉 시점을 여기서 고릅니다(걸음 모양 설계 ST2).</param>
        /// <param name="turnSeconds">걸음 전 회전·경로 끝 방향 복귀 시간입니다. 0이면 바로 돌립니다.</param>
        /// <param name="generation">사건 Presenter가 소유한 현재 재생 세대입니다. 취소마다 바뀝니다.</param>
        /// <param name="show">한 개체의 사실을 표시 사실에 넣고 다시 그립니다.</param>
        /// <param name="playMovementSound">스스로 이동하는 걸음의 시작 효과음입니다.</param>
        /// <param name="contacts">결과마다 한 번 해석한 걸음 구간·접촉 묶음입니다. 레인 집합이 결과마다 준비하고 레인들이 공유합니다.</param>
        /// <param name="objectBody">오브젝트 원본의 접촉 몸입니다(<see cref="FieldObjectPresenter.BodyOf"/>). 없으면 오브젝트 묶음은 보간 끝(칸 도착)에 해제합니다.</param>
        /// <param name="leapMotion">뛰어넘기 구간 전체 시간·높이입니다. 생략하면 View 설정 기본값을 씁니다.</param>
        internal CombatUnitMotionPlayback(CombatUnitPresentationWindow presentation,
            Func<CharacterCombatFacts, Quaternion, FieldGridMapper, CombatBody> moverBody, CombatStepStyles styles, float turnSeconds,
            Func<int> generation, Action<CharacterCombatFacts> show, Action<CharacterCombatFacts> playMovementSound,
            CombatStepContacts contacts,
            Func<ObjectSourceFact, FieldGridMapper, CombatBody> objectBody = null, CombatLeapMotionSettings leapMotion = null)
        {
            _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            _moverBody = moverBody ?? throw new ArgumentNullException(nameof(moverBody));
            _styles = styles ?? throw new ArgumentNullException(nameof(styles));
            _leapMotion = leapMotion ?? CombatLeapMotionSettings.Default;
            _turnSeconds = turnSeconds;
            _generation = generation ?? throw new ArgumentNullException(nameof(generation));
            _show = show ?? throw new ArgumentNullException(nameof(show));
            _playMovementSound = playMovementSound ?? throw new ArgumentNullException(nameof(playMovementSound));
            _objectBody = objectBody;
            _contacts = new CombatStepContactGate(contacts ?? throw new ArgumentNullException(nameof(contacts)));
            _placementYielded = OnPlacementYielded;
            _pushLean = new CombatPushLean(_presentation);
        }

        /// <summary>걸음·방향 복귀·중간 자세에서 도약 배치를 되찾는 대기가 진행 중인지입니다.</summary>
        // 대여를 유지하는 경유 순간에는 다음 원본 사건을 허용합니다. 창구가 배치를 자세·서는 자리에 양보한 동안만(평가 C4-05)
        // 레인 Tick으로 그 대여가 끝나기를 기다립니다.
        internal bool IsActive => _pull != null || _moving != null || _facingRestore != null || _contacts.IsActive ||
            _continuedLeap != null && _placement == null;

        internal bool HasLeapContinuation => _continuedLeap != null;

        /// <summary>걸음 사건 완료 뒤 남은 걸음 연출(보간·미룬 방향 복귀)과 다음 사건이 겹칠 수 있는지입니다.</summary>
        internal bool AllowsOverlap => _pull == null && _contacts.AllowsOverlap;

        /// <summary>그 사건을 남은 걸음 연출과 겹쳐 지금 시작해도 되는지입니다(이동 접촉 설계 M4 규칙 1~2, 규칙 3은 동시 처리 설계 C5). 걸음이 없으면 true입니다.</summary>
        internal bool CanBegin(CombatEvent item) => _pull == null && _contacts.CanBegin(item);

        /// <summary>그 걸음의 보간이 아직 진행 중인지입니다(사건 완료 뒤 경로 끝 방향 복귀를 미룰지).</summary>
        internal bool IsInterpolating(CombatCharacterEvent step) => step != null && ReferenceEquals(_moving, step);

        /// <summary>한 걸음을 맡습니다. <paramref name="startNow"/>가 false면 걸음 전 대기가 끝난 뒤 첫 Tick에서 시작합니다.</summary>
        /// <param name="kind">이동 종류입니다. 걸음 모양은 표에서, 걸음 전 회전·접촉 Gate 겹침은 종류의 성질(<see cref="CombatMovementKinds"/>)에서 정합니다.</param>
        /// <param name="completed">걸음 사건 완료 통지입니다(첫 접촉 묶음 해제, 없으면 보간 끝). 취소는 완료가 아닙니다.</param>
        /// <param name="leapStep">원본 발동에 대응하는 도약 구간의 걸음입니다. 없으면 기존 이동 모양을 씁니다.</param>
        internal void BeginStep(CombatCharacterEvent step, CombatMovementKind kind, bool startNow, FieldGridMapper mapper, Action completed,
            CombatLeapPlan.Step leapStep = null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _movementProfile = (CombatMovementKinds.IsSelfDriven(kind) ? _voluntaryProfiles : _forcedProfiles).Of((step.Expected ?? step.Final)?.Owner);
            using var actionSample = _movementProfile.Begin.Auto();
#endif

            CombatStepStyle style = _styles.For(kind);
            if (leapStep != null)
            {
                bool continues = leapStep.Follows(_continuedLeap?.Step);
                if (!leapStep.IsFirst && !continues)
                    throw new InvalidOperationException("[CombatUnitMotionPlayback] 도약 중간 걸음에 이어지는 이전 배치가 없습니다.");
                // 이어 가는 걸음은 경유에서 쥔 배치로 다시 걷습니다. 걷는 동안은 양보하지 않습니다(결정 Q-R11 A, 평가 C4-05).
                if (continues) _presentation.ClearYieldable(step.Expected.EntityId);
                else EndLeap();
                style = new CombatLeapStepStyle(leapStep, continues ? _continuedLeap.Shape :
                    new CombatLeapStepStyle.Arc(leapStep.Span, _leapMotion, mapper));
            }
            else
                EndLeap();
            bool selfDriven = CombatMovementKinds.IsSelfDriven(kind);
            _contacts.BeginStep(step, selfDriven);
            _stepCompleted = completed ?? throw new ArgumentNullException(nameof(completed));
            _stepVersion = _generation();
            _moving = step;
            _elapsed = 0f;
            _movementBegan = false;
            _stepPrepared = false;
            _style = style;
            _selfDriven = selfDriven;
            _facingRestoreDeferred = false;
            if (startNow)
                StartStep(mapper);
        }

        /// <summary>
        /// Core가 제공한 실제 끌어오기 경로를 같은 유닛의 이동 레인에서 재생합니다. 끌어오기는 밀림이라 밀림 모양으로 칸마다 옮깁니다(걸음 모양 계획 P10).
        /// 먼저 끌려가는 방향(도착 칸 쪽)으로 돌고, 도착하면 원래 방향으로 돌아온 뒤 완료를 알립니다(걸음 모양 계획 §7, 걸음 경로와 같은 회전·복귀).
        /// </summary>
        internal void BeginPull(AttackPullSnapshot movement, CharacterCombatFacts target, FieldGridMapper mapper, Action completed)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _movementProfile = _pullProfiles.Of(target?.Owner);
            using var actionSample = _movementProfile.Begin.Auto();
#endif

            if (completed == null) throw new ArgumentNullException(nameof(completed));
            EndLeap();
            _style = _styles.For(CombatMovementKind.Forced);
            _pull = movement;
            _pullIndex = 0;
            _elapsed = 0f;
            _stepVersion = _generation();
            _stepCompleted = completed;
            _pullFrom = At(target, movement.StartAnchor);
            _show(_pullFrom);
            if (_stepVersion != _generation() || _pull == null) return;
            _placement = _presentation.BeginPlacement(target.EntityId);
            TurnToward(_pullFrom, At(target, movement.FinalAnchor), mapper);
        }

        // 칸과 도착지는 Core의 실제 경로를 그대로 씁니다. 체력·디버프는 뒤따르는 사건 전까지 바꾸지 않습니다.
        private static CharacterCombatFacts At(CharacterCombatFacts target, CellCoord cell) =>
            target.WithPlacement(target.Area, null, cell, target.Rotation, new[] { cell });

        private void TickPull(float remaining, FieldGridMapper mapper)
        {
            // 끌려가는 방향으로 다 돈 뒤 남은 시간부터 옮깁니다(걸음과 같은 순서).
            if (!TickTurn(_pullFrom.EntityId, ref remaining)) return;
            while (_pull != null && _pullIndex < _pull.Path.Count)
            {
                float seconds = _style.Seconds;
                float consumed = Math.Min(Math.Max(0f, remaining), seconds - _elapsed);
                remaining -= consumed;
                _elapsed += consumed;
                CharacterCombatFacts next = At(_pullFrom, _pull.Path[_pullIndex]);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                using var interpolateSample = _movementProfile.Interpolate.Auto();
#endif
                _placement?.Interpolate(_pullFrom, next, mapper, Mathf.Clamp01(_elapsed / seconds), _style);
                if (_elapsed < seconds) return;
                _pullFrom = next;
                _pullIndex++;
                _elapsed = 0f;
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var endSample = _movementProfile.End.Auto();
#endif
            if (_pull == null) return;
            CharacterCombatFacts final = _pullFrom;
            int version = _stepVersion;
            Action completed = _stepCompleted;
            _pull = null;
            _pullFrom = null;
            _stepCompleted = null;
            EndPlacement();
            if (version != _generation()) return;
            _show(final);
            // 원래 방향(사실 회전)으로 돌아온 뒤 완료합니다. 돌 필요가 없으면 바로 완료합니다.
            if (version == _generation()) BeginFacingRestore(final, completed, mapper);
        }

        /// <summary>경로 끝에서 원래 방향으로 돌립니다. 돌 필요가 없으면 바로 완료를 알립니다. 취소는 완료가 아닙니다.</summary>
        internal void BeginFacingRestore(CharacterCombatFacts facts, Action completed, FieldGridMapper mapper) =>
            BeginRestore(facts, completed, mapper, _turnSeconds);

        /// <summary>
        /// 밀린 경로 끝에서 젖힌 자세를 폅니다(넉백 피격 연출, 임시). 방향 복귀와 같이 사실 회전으로 돌아가고 표시 회전을 지우며,
        /// 시간만 젖히는 시간(<see cref="CombatHitLean.RiseSeconds"/>, 반응 값이 없으면 회전 시간)입니다.
        /// </summary>
        internal void BeginStraighten(CharacterCombatFacts facts, Action completed, FieldGridMapper mapper) =>
            BeginRestore(facts, completed, mapper, StraightenSeconds(facts.EntityId));

        private void BeginRestore(CharacterCombatFacts facts, Action completed, FieldGridMapper mapper, float seconds)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _facingProfile = _facingProfiles.Of(facts?.Owner);
            using var actionSample = _facingProfile.Begin.Auto();
#endif

            EndLeap();
            _pushLean.Stop();
            _facingRestore = facts;
            _facingRestoreCompleted = completed;
            BeginTurn(facts.EntityId, FieldUnitPresenter.ModelRotation(facts.Owner, facts.Rotation, mapper), seconds);
            if (!_turning) FinishFacingRestore(mapper);
        }

        private float StraightenSeconds(EntityId id) =>
            _presentation.TryGetReactionSettings(id, out CombatUnitReactionPresentationData settings) ? CombatHitLean.RiseSeconds(settings) : _turnSeconds;

        /// <summary>
        /// 사건 완료 뒤 아직 보간 중인 경로 끝 걸음의 방향 복귀를 보간 끝 뒤로 미룹니다(설계 M3). 완료 통지는 호출자가 바로 합니다.
        /// 미룬 복귀는 남은 걸음 연출이라 끝날 때까지 이동자 자신의 사건이 기다립니다.
        /// </summary>
        internal void DeferFacingRestore(CombatCharacterEvent step)
        {
            if (!IsInterpolating(step))
                throw new InvalidOperationException("[CombatUnitMotionPlayback] 보간 중인 걸음만 방향 복귀를 미룰 수 있습니다.");
            _facingRestoreDeferred = true;
        }

        /// <summary>걸음 전 대기가 끝난 뒤 남은 시간만큼 회전·보간을 진행합니다.</summary>
        internal void Tick(float remaining, FieldGridMapper mapper)
        {
            int version = _generation();
            if (_pull != null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                using var pullSample = _movementProfile.Tick.Auto();
#endif
                TickPull(remaining, mapper);
                return;
            }
            if (_facingRestore != null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                using var facingSample = _facingProfile.Tick.Auto();
#endif
                if (TickTurn(_facingRestore.EntityId, ref remaining) && version == _generation())
                    FinishFacingRestore(mapper);
                return;
            }
            if (_moving == null)
            {
                ResumeLeap(mapper);
                return;
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var actionSample = _movementProfile.Tick.Auto();
#endif
            if (!_stepPrepared)
                StartStep(mapper);
            if (version != _generation() || _moving == null) return;
            if (!TickTurn(_moving.Expected.EntityId, ref remaining)) return;
            if (!_movementBegan) StartMotion(mapper);
            if (version != _generation() || _moving == null) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var interpolateSample = _movementProfile.Interpolate.Auto();
#endif
            _elapsed += remaining;
            _pushLean.Tick(remaining);
            float progress = Mathf.Clamp01(_elapsed / _style.Seconds);
            _placement?.Interpolate(_moving.Expected, _moving.Final, mapper, progress, _style);
            _contacts.Advance(progress);
            NotifyStepCompleted();
            if (_elapsed < _style.Seconds) return;
            FinishStep(mapper);
        }

        /// <summary>걸음·방향 복귀를 버립니다. 완료를 알리지 않습니다. 재생 세대는 호출자가 먼저 올립니다.</summary>
        internal void Cancel()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var movementSample = _pull != null || _moving != null || _continuedLeap != null ? _movementProfile.Cancel.Auto() : default;
            using var facingSample = _facingRestore != null ? _facingProfile.Cancel.Auto() : default;
#endif

            _pull = null;
            _pullFrom = null;
            _pullIndex = 0;
            _moving = null;
            _elapsed = 0f;
            _movementBegan = false;
            _stepPrepared = false;
            _turning = false;
            _pushLean.Stop();
            _facingRestore = null;
            _facingRestoreCompleted = null;
            _facingRestoreDeferred = false;
            _stepCompleted = null;
            _contacts.EndStep();
            _contactProgress.Clear();
            _continuedLeap = null;
            EndPlacement();
        }

        /// <summary>
        /// 끊는 사건이면 이어 갈 도약을 닫습니다. 끊는 판정은 계획과 같은 함수 하나입니다(<see cref="CombatLeapPlan.EndsLeap"/>, 평가 C1-11).
        /// </summary>
        /// <remarks>
        /// 이동(<c>Moved</c>) 사건은 걸음 길로 와서 여기로 오지 않습니다.
        /// 다른 유닛의 재배치·복귀로 닫는 일은 지금 일어나지 않습니다. 계획이 그 사건에서 구간을 닫아 그 앞 걸음이 마지막(<see cref="CombatLeapPlan.Step.IsLast"/>)이 됩니다.
        /// 또 도약 걸음은 묶음 밖 장벽이라, 레인이 도약을 쥔 동안에는 결과 순서로 두 걸음 사이의 사건만 시작합니다. 도약 걸음을 동시 묶음에 넣으면 이 전제를 다시 봅니다.
        /// 도약 중간의 피격 자세·유닛 행동은 창구가 경유 배치를 양보받아 엽니다(<see cref="CombatUnitPresentationWindow.MarkYieldable"/>, 평가 C4-05).
        /// 그래서 여기서는 사건 종류를 보고 배치를 내려놓지 않습니다.
        /// </remarks>
        internal void BeforeEvent(CombatEvent item)
        {
            if (_continuedLeap == null || _moving != null) return;
            if (CombatLeapPlan.EndsLeap(item, _continuedLeap.Step.Event.Final.EntityId))
                EndLeap();
        }

        /// <summary>
        /// 창구가 자세·서는 자리에 양보한 도약 배치를 그 대여가 끝났으면 되찾아 공중 경유 자리에 둡니다(평가 C4-05).
        /// 되찾은 배치도 다시 양보할 수 있게 표시합니다. 양보하지 않았거나 다른 대여가 남았으면 아무 일도 하지 않습니다.
        /// </summary>
        internal void ResumeLeap(FieldGridMapper mapper)
        {
            if (_continuedLeap == null || _moving != null || _placement != null) return;
            CombatCharacterEvent last = _continuedLeap.Step.Event;
            if (!_presentation.CanResumePlacement(last.Final.EntityId)) return;
            _placement = _presentation.BeginPlacement(last.Final.EntityId);
            _presentation.MarkYieldable(last.Final.EntityId, _placementYielded);
            _placement.Interpolate(last.Expected, last.Final, mapper, 1f, _continuedLeap);
        }

        // 창구가 경유 배치를 자세·서는 자리에 넘겼습니다(평가 C4-05). 배치는 창구가 이미 뺐으므로 비우기만 합니다.
        // 이제 IsActive가 참이라 그 대여가 끝날 때까지 이 유닛의 다음 사건이 기다리고, 레인 Tick과 사건 뒤 ResumeLeap이 되찾습니다.
        private void OnPlacementYielded() => _placement = null;

        private void EndLeap()
        {
            if (_continuedLeap == null) return;
            _continuedLeap = null;
            EndPlacement();
        }

        private void StartStep(FieldGridMapper mapper)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var actionSample = _movementProfile.Prepare.Auto();
#endif

            var step = _moving;
            int version = _generation();
            _show(step.Expected);
            if (version != _generation() || _moving != step) return;
            _stepPrepared = true;
            // 걸음 시작 배치를 그린 뒤부터 보간이 위치·회전을 소유합니다. 걸음 도중 사건의 표시 갱신이 이동자를 되돌리지 않습니다(설계 M5 (a)).
            _placement ??= _presentation.BeginPlacement(step.Expected.EntityId);
            // 다음 원본 사실을 그려도 중간 칸에 착지하지 않고 같은 호의 시작 높이를 유지합니다.
            if (_style is CombatLeapStepStyle leap)
                _placement.Interpolate(step.Expected, step.Final, mapper, 0f, leap);
            if (_selfDriven)
                TurnToward(step.Expected, step.Final, mapper, _style is CombatLeapStepStyle continuation && !continuation.Step.IsFirst);
            else if (_style is not CombatLeapStepStyle)
                // 밀린 걸음은 돌지 않고 젖힌 채 밀립니다(넉백 피격 연출, 임시). 넘겨받은 젖힘은 그대로, 없으면 여기서 젖히기 시작합니다.
                _pushLean.BeginStep(step.Expected, step.Final, mapper);
            if (!_turning) StartMotion(mapper);
        }

        // 두 사실 사이 이동 방향으로 돕니다. 걸음 경로와 끌어오기가 함께 씁니다. 같은 칸이거나 표시가 없으면 돌지 않습니다.
        private void TurnToward(CharacterCombatFacts from, CharacterCombatFacts to, FieldGridMapper mapper, bool immediate = false)
        {
            if (!from.Anchor.HasValue || !to.Anchor.HasValue) return;
            // 높이가 다른 칸 사이(고지대)에서도 앞뒤로 기울지 않게 가로 방향만 봅니다(고지대 View 계획 P10).
            Vector3 direction = Vector3.ProjectOnPlane(
                FieldUnitPresenter.CenterOf(to.OccupiedCells, to.Anchor.Value, mapper)
                - FieldUnitPresenter.CenterOf(from.OccupiedCells, from.Anchor.Value, mapper), mapper.WorldUp);
            if (direction.sqrMagnitude > .000001f)
            {
                Quaternion target = Quaternion.LookRotation(direction, mapper.RotationToWorld(Rotation4.R0) * Vector3.up);
                if (immediate)
                {
                    _turning = false;
                    _presentation.SetFacing(from.EntityId, target);
                }
                else
                    BeginTurn(from.EntityId, target, _turnSeconds);
            }
        }

        private void StartMotion(FieldGridMapper mapper)
        {
            _movementBegan = true;
            ArmContacts(mapper);
            // 도약 중 경유 칸들은 같은 점프에 속하므로 첫 구간에서만 소리를 냅니다.
            if (_selfDriven && (_style is not CombatLeapStepStyle leap || leap.Step.IsFirst))
                _playMovementSound(_moving.Expected);
        }

        // 보간이 시작될 때(걸음 전 회전 뒤) 묶음별 접촉 진행률을 정합니다(설계 M2). 이동자 몸은 지금 표시 방향으로 시작 칸 → 끝 칸을 지나고,
        // 언제 닿는지는 걸음 모양이 답합니다(걷기 = 몸이 처음 닿는 순간, 뛰기 = 착지, 걸음 모양 설계 ST5).
        // 원본 몸을 얻지 못하면(조회 없음·타일·지형 확장점) 그 묶음은 보간 끝(진행률 1, 칸 도착)입니다.
        private void ArmContacts(FieldGridMapper mapper)
        {
            IReadOnlyList<CombatStepContacts.Group> groups = _contacts.Groups;
            _contactProgress.Clear();
            if (groups.Count > 0 && _style.TryGetFixedContact(out float fixedProgress))
            {
                // 몸과 관계없이 닿는 시점이 정해진 모양(뛰기 = 착지)은 몸을 구하지 않습니다(걸음 모양 계획 §6 L1).
                for (int i = 0; i < groups.Count; i++)
                    _contactProgress.Add(fixedProgress);
            }
            else if (groups.Count > 0)
            {
                CharacterCombatFacts from = _moving.Expected;
                CharacterCombatFacts to = _moving.Final;
                bool hasMover = false;
                CombatBody start = default;
                Vector2 end = default;
                if (from.Anchor.HasValue && to.Anchor.HasValue && _presentation.TryGetRotation(from.EntityId, out Quaternion facing))
                {
                    start = _moverBody(from, facing, mapper);
                    end = _moverBody(to, facing, mapper).Center;
                    hasMover = true;
                }
                for (int i = 0; i < groups.Count; i++)
                    _contactProgress.Add(hasMover && TryGetSourceBody(groups[i], mapper, out CombatBody source)
                        ? _style.ContactProgress(start, end, source)
                        : 1f);
            }
            _contacts.Arm(_contactProgress);
            // 시작부터 닿아 있는 원본은 첫 진행 전에 해제합니다.
            _contacts.Advance(0f);
            NotifyStepCompleted();
        }

        // 접촉 원본 종류별 몸 조회 한 곳입니다(설계 M2). 타일·지형 원본은 Core 진입 규칙이 생길 때 지형 표시의 몸 조회를 더합니다.
        private bool TryGetSourceBody(CombatStepContacts.Group group, FieldGridMapper mapper, out CombatBody body)
        {
            switch (group.Kind)
            {
                case CombatContactSourceKind.Object when _objectBody != null && group.Source is CombatObjectActivationEvent activation:
                    body = _objectBody(activation.Source, mapper);
                    return true;
                default:
                    body = default;
                    return false;
            }
        }

        private void FinishStep(FieldGridMapper mapper)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var actionSample = _movementProfile.End.Auto();
#endif

            CharacterCombatFacts final = _moving.Final;
            bool restore = _facingRestoreDeferred;
            _moving = null;
            _movementBegan = false;
            _stepPrepared = false;
            _facingRestoreDeferred = false;
            _contacts.CompleteInterpolation();
            if (_style is CombatLeapStepStyle leap && !leap.Step.IsLast && !restore)
            {
                // 공중 경유 자리에서 배치를 쥔 채 다음 원본 걸음을 기다립니다. 그동안 자세·서는 자리가 오면 창구가 양보받습니다(평가 C4-05).
                _continuedLeap = leap;
                _presentation.MarkYieldable(final.EntityId, _placementYielded);
            }
            else
            {
                _continuedLeap = null;
                EndPlacement();
            }
            int version = _generation();
            _show(final);
            if (version != _generation()) return;
            if (restore && _selfDriven)
                BeginFacingRestore(final, _contacts.EndStep, mapper);
            else if (restore)
                BeginStraighten(final, _contacts.EndStep, mapper);
            else
                _contacts.EndStep();
            NotifyStepCompleted();
        }

        // 걸음 사건 완료를 한 번만 알립니다(같은 재생 세대에서만). 알림은 순차 재생의 상태만 바꾸고, 다음 시작은 순차 재생 Tick이 합니다.
        private void NotifyStepCompleted()
        {
            if (_stepCompleted == null || !_contacts.IsEventComplete || _stepVersion != _generation()) return;
            Action completed = _stepCompleted;
            _stepCompleted = null;
            completed();
        }

        private void EndPlacement()
        {
            ICombatUnitPlacement placement = _placement;
            _placement = null;
            placement?.Restore();
        }

        private void BeginTurn(EntityId id, Quaternion target, float seconds)
        {
            _turning = false;
            if (!_presentation.TryGetRotation(id, out _turnFrom)) return;
            _turnTo = target;
            _turnElapsed = 0f;
            _turnDuration = seconds;
            _turning = _turnDuration > 0f && Quaternion.Angle(_turnFrom, _turnTo) > .1f;
            _presentation.SetFacing(id, _turning ? _turnFrom : _turnTo);
        }

        private bool TickTurn(EntityId id, ref float remaining)
        {
            if (!_turning) return true;
            if (!_presentation.TryGetRotation(id, out _))
            {
                _turning = false;
                return true;
            }
            float consumed = Math.Min(remaining, _turnDuration - _turnElapsed);
            remaining -= consumed;
            _turnElapsed += consumed;
            _presentation.SetFacing(id, Quaternion.Slerp(_turnFrom, _turnTo, Mathf.Clamp01(_turnElapsed / _turnDuration)));
            if (_turnElapsed < _turnDuration) return false;
            _turning = false;
            return true;
        }

        private void FinishFacingRestore(FieldGridMapper mapper)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var actionSample = _facingProfile.End.Auto();
#endif

            var facts = _facingRestore;
            var completed = _facingRestoreCompleted;
            int version = _generation();
            _facingRestore = null;
            _facingRestoreCompleted = null;
            _presentation.ClearFacing(facts, mapper);
            if (version == _generation()) completed();
        }
    }
}
