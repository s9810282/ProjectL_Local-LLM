using System;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat
{
    /// <summary>게임 결과와 분리한 이번 요청의 표시 완료 상태입니다.</summary>
    internal enum MapCombatPresentationState
    {
        Pending,
        Ready,
        Abandoned
    }

    /// <summary>하나의 전투 요청과 맵 표시 소유권을 대응시킵니다.</summary>
    internal sealed class MapCombatPresentationTicket
    {
        private MapCombatPresentationLease _owner;
        private MapCombatPresentationState _state = MapCombatPresentationState.Pending;

        internal MapCombatPresentationTicket(MapCombatPresentationLease owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        internal MapCombatPresentationState ReadState()
        {
            // 교체된 과거 요청도 별도 목록 없이 자기 lease의 폐기를 관찰합니다.
            return _owner != null && !_owner.IsAttached
                ? MapCombatPresentationState.Abandoned
                : _state;
        }

        /// <summary>이 표시 대기를 주어진 소유권이 맡는지입니다. 활성화를 통과한 뒤에는 거짓입니다.</summary>
        internal bool IsOwnedBy(MapCombatPresentationLease owner) => owner != null && ReferenceEquals(_owner, owner);

        internal bool TryComplete(MapCombatPresentationLease owner)
        {
            if (!ReferenceEquals(_owner, owner) || ReadState() != MapCombatPresentationState.Pending)
                return false;
            _state = MapCombatPresentationState.Ready;
            return true;
        }

        internal void Pass()
        {
            if (ReadState() != MapCombatPresentationState.Ready)
                throw new InvalidOperationException("맵 표시가 완료되기 전에 전투 활성화를 시작할 수 없습니다.");
            // 정상 씬 이탈의 lease 폐기는 이미 통과한 gate를 다시 무효화하지 않습니다.
            _owner = null;
        }
    }
}
