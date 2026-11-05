using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using Unity.Profiling;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>선택 개체를 한 Tick에 하나씩 준비하고 마지막 UI 갱신 다음 프레임에 반환합니다.</summary>
    internal sealed class CombatUnitPreparation : ILodingPreparation
    {
        private static readonly ProfilerMarker TickMarker = new("Combat.Units.PreparationTick");
        private readonly FieldUnitPresenter _units;
        private CombatUnitPreparationInput _input;
        private Func<bool> _isCurrent;
        private readonly List<CombatUnitPreparationLease> _leases = new();
        private int _next;
        private int _uiUpdateFrame = -1;
        private bool _started;
        private bool _covered;
        private bool _subscribed;

        internal CombatUnitPreparation(FieldUnitPresenter units, CombatUnitPreparationInput input, Func<bool> isCurrent)
        {
            _units = units != null ? units : throw new ArgumentNullException(nameof(units));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _isCurrent = isCurrent ?? throw new ArgumentNullException(nameof(isCurrent));
        }

        public LodingPreparationState State { get; private set; } = LodingPreparationState.Pending;

        public void Begin(bool isCovered)
        {
            if (_started || State != LodingPreparationState.Pending)
                throw new InvalidOperationException("[CombatUnitPreparation] 준비 작업은 한 번만 시작할 수 있습니다.");
            _started = true;
            _covered = isCovered;
            Canvas.willRenderCanvases += OnCanvasUpdate;
            _subscribed = true;
        }

        public void Tick(float deltaTime)
        {
            using var sample = TickMarker.Auto();
            if (State != LodingPreparationState.Pending) return;
            try
            {
                if (!_started || _units == null || !_isCurrent())
                    throw new InvalidOperationException("[CombatUnitPreparation] 준비 중 씬 연결·전투·확정 덱이 바뀌었습니다.");
                if (_next < _input.Targets.Count)
                {
                    CombatUnitPreparationLease lease = _units.PrepareSelectedUnit(_input.Targets[_next], _covered);
                    _leases.Add(lease);
                    if (State != LodingPreparationState.Pending || !_isCurrent())
                        throw new InvalidOperationException("[CombatUnitPreparation] 유닛 준비 콜백 중 연결이 취소됐습니다.");
                    _next++;
                    _uiUpdateFrame = -1;
                    // 표현 없는 개발 경로는 alpha 0 상태에서 명시적으로 UI를 갱신합니다.
                    // 실제 화면 렌더/GPU 준비를 증명하는 경로는 아닙니다.
                    if (!_covered && _next == _input.Targets.Count) Canvas.ForceUpdateCanvases();
                    return;
                }
                foreach (CombatUnitPreparationLease lease in _leases) lease.Validate();
                // willRender는 시작 알림일 수 있습니다. 그 프레임을 온전히 지난 뒤에만 성공합니다.
                if (_uiUpdateFrame < 0 || Time.frameCount <= _uiUpdateFrame) return;
                ReleaseLeases();
                if (State != LodingPreparationState.Pending || !_isCurrent())
                    throw new InvalidOperationException("[CombatUnitPreparation] 자원 반환 중 연결이 취소됐습니다.");
                _units.MarkPreparationComplete(_input.Counts);
                State = LodingPreparationState.Complete;
                ReleaseInput();
            }
            catch (Exception error)
            {
                State = LodingPreparationState.Failed;
                try { ReleaseLeases(); }
                catch (Exception cleanup) { throw new AggregateException(error, cleanup); }
                finally { ReleaseInput(); }
                throw;
            }
        }

        private void OnCanvasUpdate()
        {
            if (State == LodingPreparationState.Pending && _input != null &&
                _next == _input.Targets.Count && _uiUpdateFrame < 0)
                _uiUpdateFrame = Time.frameCount;
        }

        public void Cancel()
        {
            if (State != LodingPreparationState.Pending) return;
            State = LodingPreparationState.Cancelled;
            try { ReleaseLeases(); }
            finally { ReleaseInput(); }
        }

        private void ReleaseLeases()
        {
            Unsubscribe();
            List<Exception> errors = null;
            // 실패한 대여도 재시도·중복 반환하지 않습니다. 나머지 대여는 모두 정리합니다.
            while (_leases.Count > 0)
            {
                int i = _leases.Count - 1;
                CombatUnitPreparationLease lease = _leases[i];
                _leases.RemoveAt(i);
                CleanupErrors.Try(lease.Dispose, ref errors);
            }
            CleanupErrors.ThrowIfAny(errors, "[CombatUnitPreparation] 준비 완료/취소 정리에 실패했습니다.");
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            Canvas.willRenderCanvases -= OnCanvasUpdate;
            _subscribed = false;
        }

        private void ReleaseInput()
        {
            Unsubscribe();
            _input = null;
            _isCurrent = null;
        }
    }
}
