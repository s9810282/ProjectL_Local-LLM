using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction
{
    /// <summary>공격에서 시작한 Drag의 누른 칸과 release 소유권만 관리합니다.</summary>
    internal sealed class CombatAttackDragGesture
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 이 기능의 계측 정의입니다. 일반 배포 빌드에서는 선언과 호출을 함께 제외합니다.
        private static readonly Unity.Profiling.ProfilerMarker _profileInteractionAttackDragPress = new("Combat.Action.Interaction.AttackDrag.Press");
        private static readonly Unity.Profiling.ProfilerMarker _profileInteractionAttackDragPromoteToMove = new("Combat.Action.Interaction.AttackDrag.PromoteToMove");
        private static readonly Unity.Profiling.ProfilerMarker _profileInteractionAttackDragRelease = new("Combat.Action.Interaction.AttackDrag.Release");
        private static readonly Unity.Profiling.ProfilerMarker _profileInteractionAttackDragReset = new("Combat.Action.Interaction.AttackDrag.Reset");
#endif

        internal readonly struct Origin
        {
            internal Origin(long battleId, EntityId actorId, CellCoord anchor, int resultSequence, Vector2 pressedScreen)
            {
                BattleId = battleId;
                ActorId = actorId;
                Anchor = anchor;
                ResultSequence = resultSequence;
                PressedScreen = pressedScreen;
            }
            internal long BattleId { get; }
            internal EntityId ActorId { get; }
            internal CellCoord Anchor { get; }
            internal int ResultSequence { get; }
            internal Vector2 PressedScreen { get; }
        }

        internal enum ReleaseRoute { PassThrough, ForwardToPath, Consume }
        private enum State { Idle, Candidate, MoveOwned, SuppressUntilRelease }
        private readonly ICombatPointerMapping _mapping;
        private State _state;
        private Origin _origin;
        private int _generation;

        internal CombatAttackDragGesture(ICombatPointerMapping mapping)
        {
            _mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
        }

        internal void BeginPress(Origin? origin)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionAttackDragPressSample = _profileInteractionAttackDragPress.Auto();
#endif

            _generation++;
            _origin = origin.GetValueOrDefault();
            _state = origin.HasValue ? State.Candidate : State.Idle;
        }

        internal bool TryGetCandidate(out Origin origin, out int generation)
        {
            origin = _origin;
            generation = _generation;
            return _state == State.Candidate;
        }

        internal bool TryPromote(float screenX, float screenY, bool overUi)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionAttackDragPromoteToMoveSample = _profileInteractionAttackDragPromoteToMove.Auto();
#endif

            if (_state != State.Candidate || overUi)
                return false;
            int generation = _generation;
            bool mapped = _mapping.TryMapPointer(new Vector2(screenX, screenY), out var grid, out var cancel);
            if (generation != _generation || _state != State.Candidate || !mapped || cancel
                || MovementPointerSampler.CellAt(grid) == _origin.Anchor)
                return false;
            _state = State.MoveOwned;
            return true;
        }

        internal bool Owns(int generation) => _generation == generation && _state == State.MoveOwned;

        internal ReleaseRoute Release()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionAttackDragReleaseSample = _profileInteractionAttackDragRelease.Auto();
#endif

            ReleaseRoute route = _state == State.MoveOwned ? ReleaseRoute.ForwardToPath
                : _state == State.SuppressUntilRelease ? ReleaseRoute.Consume : ReleaseRoute.PassThrough;
            Reset();
            return route;
        }

        internal void Invalidate()
        {
            _generation++;
            if (_state != State.Idle)
                _state = State.SuppressUntilRelease;
        }

        internal void Reset()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionAttackDragResetSample = _profileInteractionAttackDragReset.Auto();
#endif

            _generation++;
            _state = State.Idle;
            _origin = default;
        }
    }
}
