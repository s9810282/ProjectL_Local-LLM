using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>한 공격의 전용 투영과 자식 계산을 보관하며 완료된 결과만 공개한다.</summary>
    internal sealed class CombatAttackCalculation : IDisposable
    {
        private IEnumerator<string> _steps;
        private bool _complete;
        private bool _disposed;
        private bool _faulted;
        private long _version;
        private AttackPreview _preview;
        private CombatCommandTransition _transition;
        private IReadOnlyList<CellCoord> _emptyAreaCells = Array.Empty<CellCoord>();
        private string _phase = "Begin";

        internal CombatAttackCalculation(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules,
            bool publicPreview = true, CharacterCombatFacts reactionActor = null, AttackAim? reactionAim = null,
            Owner? previewObserver = null)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (rules == null && reactionActor == null) throw new ArgumentNullException(nameof(rules));
            if (reactionActor == null && input.Kind != CombatCommandKind.Attack)
                throw new ArgumentException("공격 입력만 재개할 수 있습니다.", nameof(input));
            _steps = CombatAttackCalculator.Run(input, rules, publicPreview, reactionActor, reactionAim,
                (transition, preview, emptyAreaCells) =>
                {
                    _transition = transition;
                    _preview = publicPreview && transition != null && !transition.Accepted
                        ? AttackPreview.Failure(Enum.Parse<AttackFailureReason>(transition.Reason),
                            input.ActorId, EntityId.Empty, transition.BlockedContacts)
                        : preview;
                    _emptyAreaCells = emptyAreaCells;
                }, previewObserver).GetEnumerator();
        }

        internal bool IsComplete
        {
            get
            {
                RequireActive();
                return _complete;
            }
        }
        internal long WorkVersion
        {
            get
            {
                RequireActive();
                return _version;
            }
        }
        internal string Phase
        {
            get
            {
                RequireActive();
                return _phase;
            }
        }
        internal AttackPreview Preview
        {
            get
            {
                RequireComplete();
                return _preview;
            }
        }
        internal CombatCommandTransition Transition
        {
            get
            {
                RequireComplete();
                return _transition;
            }
        }
        internal IReadOnlyList<CellCoord> EmptyAreaCells
        {
            get
            {
                RequireComplete();
                return _emptyAreaCells;
            }
        }

        internal void Step()
        {
            RequireActive();
            if (_complete) return;
            try
            {
                _complete = !_steps.MoveNext();
                _version++;
                if (_complete)
                {
                    _steps.Dispose();
                    _steps = null;
                    _phase = "Complete";
                }
                else _phase = _steps.Current;
            }
            catch
            {
                _faulted = true;
                Release();
                throw;
            }
        }

        private void RequireActive()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(CombatAttackCalculation));
            if (_faulted) throw new InvalidOperationException("실패한 공격 계산은 다시 진행할 수 없습니다.");
        }

        private void RequireComplete()
        {
            RequireActive();
            if (!_complete) throw new InvalidOperationException("공격 계산이 끝나지 않았습니다.");
        }

        private void Release()
        {
            _steps?.Dispose();
            _steps = null;
            _transition = null;
            _preview = default;
            _emptyAreaCells = null;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Release();
        }
    }
}
