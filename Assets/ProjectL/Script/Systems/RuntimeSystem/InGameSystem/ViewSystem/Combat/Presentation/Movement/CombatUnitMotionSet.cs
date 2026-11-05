using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement
{
    /// <summary>
    /// 유닛별 걸음 레인(<see cref="CombatUnitMotionPlayback"/>)을 소유합니다(동시 처리 설계 C5). 같은 순간에 밀린 여러 유닛이 함께 걸을 수 있고,
    /// 한 유닛의 걸음은 그 레인에서 차례로 돕니다. 결과의 걸음 해석(<see cref="CombatStepContacts"/>)은 결과마다 한 번 만들어 레인들이 공유합니다.
    /// </summary>
    /// <remarks>
    /// 무엇을 함께 시작할지는 순차 재생(스케줄러)이 정하고, 이 집합은 레인 자원(그 유닛의 걸음·미룬 방향 복귀)과 걸음 Gate 규칙으로만 답합니다.
    /// 재생 세대는 사건 Presenter가 하나만 소유하며 레인들은 읽기 함수로 확인합니다.
    /// </remarks>
    internal sealed class CombatUnitMotionSet
    {
        private readonly CombatUnitPresentationWindow _presentation;
        private readonly Func<CharacterCombatFacts, Quaternion, FieldGridMapper, CombatBody> _moverBody;
        private readonly CombatStepStyles _styles;
        private readonly CombatLeapMotionSettings _leapMotion;
        private readonly float _turnSeconds;
        private readonly Func<int> _generation;
        private readonly Action<CharacterCombatFacts> _show;
        private readonly Action<CharacterCombatFacts> _playMovementSound;
        private readonly Func<CellCoord, FieldGridMapper, CombatBody> _thornBody;
        private readonly Func<ObjectSourceFact, FieldGridMapper, CombatBody> _objectBody;
        private readonly CombatStepContacts _contacts = new();
        private readonly Dictionary<EntityId, CombatUnitMotionPlayback> _lanes = new();
        private readonly List<CombatUnitMotionPlayback> _tickSnapshot = new();
        private bool _ticking;
        private CombatLeapPlan _leaps;

        /// <param name="presentation">유닛 표시 창구입니다. 레인이 걸음 배치·표시 회전을 여기서 빌립니다(연출 조립 설계 B13).</param>
        /// <param name="moverBody">사실 칸 배치에 표시 회전을 둔 이동자 몸입니다(<see cref="FieldUnitPresenter.BodyOf(CharacterCombatFacts, Quaternion, FieldGridMapper)"/>).</param>
        /// <param name="styles">이동 종류 → 걸음 모양 표입니다(걸음 모양 설계 ST2). 레인들이 함께 씁니다.</param>
        /// <param name="turnSeconds">걸음 전 회전·경로 끝 방향 복귀 시간입니다. 0이면 바로 돌립니다.</param>
        /// <param name="generation">사건 Presenter가 소유한 현재 재생 세대입니다. 취소마다 바뀝니다.</param>
        /// <param name="show">한 개체의 사실을 표시 사실에 넣고 다시 그립니다.</param>
        /// <param name="playMovementSound">이동 시작 효과음입니다.</param>
        /// <param name="thornBody">가시 칸의 접촉 몸입니다. 없으면 가시 묶음은 보간 끝(칸 도착)에 해제합니다.</param>
        /// <param name="objectBody">오브젝트 원본의 접촉 몸입니다. 없으면 오브젝트 묶음은 보간 끝(칸 도착)에 해제합니다.</param>
        /// <param name="leapMotion">뛰어넘기 구간 전체 시간·높이입니다. 생략하면 View 설정 기본값을 씁니다.</param>
        internal CombatUnitMotionSet(CombatUnitPresentationWindow presentation,
            Func<CharacterCombatFacts, Quaternion, FieldGridMapper, CombatBody> moverBody, CombatStepStyles styles, float turnSeconds,
            Func<int> generation, Action<CharacterCombatFacts> show, Action<CharacterCombatFacts> playMovementSound,
            Func<CellCoord, FieldGridMapper, CombatBody> thornBody = null,
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
            _thornBody = thornBody;
            _objectBody = objectBody;
        }

        /// <summary>걸음 또는 경로 끝 방향 복귀가 진행 중인 레인이 있는지입니다.</summary>
        internal bool IsActive
        {
            get
            {
                foreach (CombatUnitMotionPlayback lane in _lanes.Values)
                    if (lane.IsActive) return true;
                return false;
            }
        }

        /// <summary>진행 중인 모든 레인이 걸음 사건 완료 뒤라 남은 연출과 겹칠 수 있는지입니다. 진행 중 레인이 없으면 true입니다.</summary>
        internal bool AllowsOverlap
        {
            get
            {
                foreach (CombatUnitMotionPlayback lane in _lanes.Values)
                    if (lane.IsActive && !lane.AllowsOverlap) return false;
                return true;
            }
        }

        /// <summary>그 유닛의 레인이 진행 중인지입니다.</summary>
        internal bool IsActiveFor(EntityId id) => _lanes.TryGetValue(id, out CombatUnitMotionPlayback lane) && lane.IsActive;

        /// <summary>그 유닛의 레인이 걸음 사건 완료 뒤라 남은 연출과 겹칠 수 있는지입니다. 진행 중이 아니면 true입니다.</summary>
        internal bool AllowsOverlapFor(EntityId id) =>
            !_lanes.TryGetValue(id, out CombatUnitMotionPlayback lane) || !lane.IsActive || lane.AllowsOverlap;

        /// <summary>진행 중인 모든 레인의 걸음 Gate가 그 사건의 시작을 허락하는지입니다(이동 접촉 설계 M4 규칙 1~2, 동시 처리 설계 C5).</summary>
        internal bool CanBegin(CombatEvent item)
        {
            foreach (CombatUnitMotionPlayback lane in _lanes.Values)
                if (lane.IsActive && !lane.CanBegin(item)) return false;
            return true;
        }

        /// <summary>결과 하나의 걸음 구간·접촉 묶음을 준비합니다. 레인은 모두 쉬고 있어야 합니다(사건 Presenter가 검사).</summary>
        internal void BeginResult(CombatResult result)
        {
            foreach (CombatUnitMotionPlayback lane in _lanes.Values)
                if (lane.HasLeapContinuation)
                    throw new InvalidOperationException("[CombatUnitMotionSet] 이전 결과의 도약 배치가 남아 있습니다.");
            _leaps = CombatLeapPlan.Build(result);
            _contacts.Begin(result);
        }

        /// <summary>
        /// 중간 사실 사건이 도약을 끊으면 이어 갈 도약을 닫습니다. 일반 걸음은 영향을 받지 않습니다.
        /// 자세·서는 자리에 도약 배치를 넘기는 일은 창구의 양보가 합니다(평가 C4-05).
        /// </summary>
        internal void BeforeEvent(CombatEvent item)
        {
            foreach (CombatUnitMotionPlayback lane in _lanes.Values)
                lane.BeforeEvent(item);
        }

        /// <summary>창구가 자세·서는 자리에 양보한 도약 배치를 그 대여가 끝났으면 되찾아 공중 경유 위치에 둡니다(평가 C4-05).</summary>
        internal void ResumeLeaps(FieldGridMapper mapper)
        {
            foreach (CombatUnitMotionPlayback lane in _lanes.Values)
                lane.ResumeLeap(mapper);
        }

        /// <summary>그 걸음의 보간이 아직 진행 중인지입니다(사건 완료 뒤 경로 끝 방향 복귀를 미룰지).</summary>
        internal bool IsInterpolating(CombatCharacterEvent step) =>
            step != null && _lanes.TryGetValue(step.Expected.EntityId, out CombatUnitMotionPlayback lane) && lane.IsInterpolating(step);

        /// <summary>그 유닛의 레인에서 한 걸음을 시작합니다. 그 레인이 쉬고 있어야 합니다.</summary>
        /// <param name="kind">이동 종류입니다. 걸음 전 회전과 걸음 모양을 정합니다(걸음 모양 설계 ST1).</param>
        /// <param name="completed">걸음 사건 완료 통지입니다(첫 접촉 묶음 해제, 없으면 보간 끝). 취소는 완료가 아닙니다.</param>
        internal void BeginStep(CombatCharacterEvent step, CombatMovementKind kind, bool startNow, FieldGridMapper mapper, Action completed)
        {
            if (step == null) throw new ArgumentNullException(nameof(step));
            CombatUnitMotionPlayback lane = Lane(step.Expected.EntityId);
            if (lane.IsActive)
                throw new InvalidOperationException("[CombatUnitMotionSet] 그 유닛의 이전 걸음이 끝나지 않았습니다.");
            lane.BeginStep(step, kind, startNow, mapper, completed, _leaps?.Of(step));
        }

        /// <summary>Core가 제공한 실제 끌어오기 경로를 같은 유닛의 이동 레인에서 재생합니다.</summary>
        internal void BeginPull(AttackPullSnapshot movement, CharacterCombatFacts target, FieldGridMapper mapper, Action completed)
        {
            CombatUnitMotionPlayback lane = Lane(movement.TargetId);
            if (lane.IsActive)
                throw new InvalidOperationException("끌어오기 전에 대상의 이전 이동이 끝나야 합니다.");
            lane.BeginPull(movement, target, mapper, completed);
        }

/// <summary>경로의 첫 걸음이 시작됨을 알립니다(이동 효과음).</summary>
        internal void NotifyMovementStarted(CombatCharacterEvent firstStep)
        {
            if (firstStep != null && _lanes.TryGetValue(firstStep.Expected.EntityId, out CombatUnitMotionPlayback lane))
                lane.NotifyMovementStarted(firstStep);
        }

        /// <summary>그 유닛의 레인에서 경로 끝 방향 복귀를 합니다. 돌 필요가 없으면 바로 완료를 알립니다.</summary>
        internal void BeginFacingRestore(CharacterCombatFacts facts, Action completed, FieldGridMapper mapper)
        {
            if (facts == null) throw new ArgumentNullException(nameof(facts));
            Lane(facts.EntityId).BeginFacingRestore(facts, completed, mapper);
        }

        /// <summary>사건 완료 뒤 아직 보간 중인 경로 끝 걸음의 방향 복귀를 그 레인의 보간 끝 뒤로 미룹니다(이동 접촉 설계 M3).</summary>
        internal void DeferFacingRestore(CombatCharacterEvent step)
        {
            if (step == null || !_lanes.TryGetValue(step.Expected.EntityId, out CombatUnitMotionPlayback lane))
                throw new InvalidOperationException("[CombatUnitMotionSet] 보간 중인 걸음만 방향 복귀를 미룰 수 있습니다.");
            lane.DeferFacingRestore(step);
        }

        /// <summary>진행 중인 모든 레인을 같은 시간만큼 진행합니다. 레인 Tick 안에서 취소되면(세대 변경) 남은 레인은 진행하지 않습니다.</summary>
        internal void Tick(float remaining, FieldGridMapper mapper)
        {
            if (_ticking) throw new InvalidOperationException("[CombatUnitMotionSet] 걸음 Tick을 중첩 호출할 수 없습니다.");
            _ticking = true;
            try
            {
                int version = _generation();
                _tickSnapshot.AddRange(_lanes.Values);
                foreach (CombatUnitMotionPlayback lane in _tickSnapshot)
                {
                    if (!lane.IsActive) continue;
                    lane.Tick(remaining, mapper);
                    if (version != _generation()) return;
                }
            }
            finally
            {
                _tickSnapshot.Clear();
                _ticking = false;
            }
        }

        /// <summary>모든 레인의 걸음·방향 복귀를 버리고 결과 해석을 비웁니다. 완료를 알리지 않습니다. 재생 세대는 호출자가 먼저 올립니다.</summary>
        internal void Cancel()
        {
            foreach (CombatUnitMotionPlayback lane in _lanes.Values)
                lane.Cancel();
            _contacts.Clear();
            _leaps = null;
        }

        private CombatUnitMotionPlayback Lane(EntityId id)
        {
            if (!_lanes.TryGetValue(id, out CombatUnitMotionPlayback lane))
            {
                lane = new CombatUnitMotionPlayback(_presentation, _moverBody, _styles, _turnSeconds, _generation, _show,
                    _playMovementSound,
                    _contacts, _thornBody, _objectBody, _leapMotion);
                _lanes.Add(id, lane);
            }
            return lane;
        }
    }
}
