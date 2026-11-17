using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Assist;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.MovePath
{
    /// <summary>빈 목적 칸에서 시작한 누름과, 다음 클릭까지 보이는 이동 후보를 관리합니다.</summary>
    internal sealed class CombatClickMove
    {
        private enum PreviewState
        {
            Waiting,
            Displayed,
            Rejected
        }

        private readonly CombatMovementQuote _quote;
        private readonly CombatMovementRangeTracker _range;
        private readonly ICombatMovementFeedback _display;
        private readonly ICombatActionRejectionSink _rejections;
        private readonly Func<CombatMovementRangeFacts> _current;
        private CombatMovementRangeFacts _candidate;
        private CombatMovementRangeFacts _pressFacts;
        private MovementPath _pressedPath;
        private int _confirmNumber;
        private long _generation;
        private PreviewState _state;

        internal CombatClickMove(CombatMovementQuote quote, CombatMovementRangeTracker range,
            ICombatMovementFeedback display, ICombatActionRejectionSink rejections, Func<CombatMovementRangeFacts> current)
        {
            _quote = quote ?? throw new ArgumentNullException(nameof(quote));
            _range = range ?? throw new ArgumentNullException(nameof(range));
            _display = display ?? throw new ArgumentNullException(nameof(display));
            _rejections = rejections ?? throw new ArgumentNullException(nameof(rejections));
            _current = current ?? throw new ArgumentNullException(nameof(current));
        }

        internal bool HasCandidate => _candidate.IsInitialized;
        internal bool NeedsPolling => HasCandidate || _pressedPath != null;

        internal bool Press(in CombatMovementRangeFacts facts, CellCoord? cell, bool occupied, bool awaitingAvailability)
        {
            ResetPress();
            bool expired = HasCandidate && !IsCurrent(facts);
            if (!cell.HasValue || occupied || (facts.IsInitialized && cell == facts.Origin))
            {
                Cancel();
                return false;
            }
            if (expired)
            {
                Cancel();
                return true;
            }
            if (!facts.IsInitialized)
                return false;
            if (!_range.TryGetShown(facts, out var range))
                return true;
            if (awaitingAvailability && !HasCandidate && range.Succeeded && range.Destinations.Length > 0)
                return true;
            if (!range.TryGetRoute(cell.Value, out var route))
            {
                Cancel();
                return false;
            }
            if (route.Origin != facts.Origin)
                throw new InvalidOperationException("이동 범위 경로의 출발 칸이 요청과 다릅니다.");

            if (HasCandidate && cell == _quote.Path.Endpoint)
            {
                if (_state == PreviewState.Waiting)
                    return true;
                if (_state == PreviewState.Displayed)
                    _confirmNumber = _quote.Number;
            }
            _pressFacts = facts;
            _pressedPath = route;
            return true;
        }

        internal void Release(CellCoord? cell, CharacterData actor)
        {
            var path = _pressedPath;
            var facts = _pressFacts;
            int confirm = _confirmNumber;
            ResetPress();
            if (path == null)
            {
                if (HasCandidate && cell != _quote.Path.Endpoint)
                    Cancel();
                return;
            }
            if (actor == null || cell != path.Endpoint || actor.EntityId != facts.ActorId || !facts.Matches(_current())
                || !_range.TryGetShown(facts, out var range) || !range.TryGetRoute(path.Endpoint, out var currentPath)
                || !ReferenceEquals(currentPath, path) || (HasCandidate && !IsCurrent(facts)))
            {
                Cancel();
                return;
            }
            if (confirm != 0)
            {
                // 표시 뒤 누른 같은 후보의 자격을 소비하고, 재진입해도 이 클릭으로는 한 번만 제출합니다.
                bool canSubmit = _state == PreviewState.Displayed && confirm == _quote.Number && _quote.IsApproved;
                _state = PreviewState.Waiting;
                try
                {
                    if (canSubmit)
                        _quote.TrySubmit();
                }
                finally
                {
                    Cancel();
                }
                return;
            }

            long replacementGeneration = _generation + 1;
            Cancel();
            if (_generation != replacementGeneration || !facts.Matches(_current()))
                return;
            _candidate = facts;
            long generation = ++_generation;
            try
            {
                _quote.Request(actor.EntityId, path, generation);
                _display.SetPathPlanning(true);
                if (generation != _generation) return;
                _display.BeginMovementPlanning(actor.EntityId, actor.CharacterKey, actor.Rotation.Value);
                if (generation != _generation) return;
                _display.UpdateMovementPointer(null, null);
                if (generation == _generation)
                    _display.ShowDraft(path, null, true, path.Endpoint);
            }
            catch
            {
                if (generation == _generation) Cancel();
                throw;
            }
        }

        internal void Poll(CombatPreviewResult response)
        {
            var current = _current();
            if (_pressedPath != null && !_pressFacts.Matches(current))
                ResetPress();
            if (!HasCandidate)
                return;
            if (!IsCurrent(current))
            {
                Cancel();
                return;
            }
            if (_state != PreviewState.Waiting || !_quote.Matches(response, _generation))
                return;

            var move = response.Move.Value;
            bool approved = _quote.Accept(response);
            if (move.CanMove && !approved)
            {
                Cancel();
                return;
            }
            long generation = _generation;
            var path = _quote.Path;
            try
            {
                _display.ShowDraft(path, move, true, path.Endpoint);
                if (generation != _generation) return;
                if (approved && !_display.TryShowMovementStop(move.PathMovement.FinalStop))
                {
                    if (generation == _generation) Cancel();
                    return;
                }
                if (!approved)
                    _rejections.MovementRejected(move.FailureReason);
                if (generation != _generation) return;
                if (!IsCurrent(_current()))
                {
                    Cancel();
                    return;
                }
                _state = approved ? PreviewState.Displayed : PreviewState.Rejected;
            }
            catch
            {
                if (generation == _generation) Cancel();
                throw;
            }
        }

        private bool IsCurrent(in CombatMovementRangeFacts facts) =>
            _candidate.Matches(facts) && _quote.IsCurrent(_generation);

        private void ResetPress()
        {
            _pressFacts = default;
            _pressedPath = null;
            _confirmNumber = 0;
        }

        internal void Cancel()
        {
            ++_generation;
            ResetPress();
            if (!HasCandidate)
                return;
            _candidate = default;
            _state = PreviewState.Waiting;
            CleanupErrors.RunAll("클릭 이동 표시를 정리하지 못했습니다.",
                _quote.Clear, _display.EndMovementPlanning, _display.ClearDraft, () => _display.SetPathPlanning(false));
        }
    }
}
