using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap.Item;
using ProjectL.Systems.RuntimeSystem.BufferSystems.StageView;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.DelaySystem.View;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>수락된 한 노드 요청의표시 이동과 Buffer 위치 동기화 수명을 관리합니다.</summary>
    internal sealed class StageMapNodeMovement : IStageMapEntryPresentation, IDisposable
    {
        private readonly StageMapViewDisplay _display;
        private readonly Transform _mapRoot;
        private readonly StageMapViewData _data;
        private readonly IDelayService _delay;
        private readonly IAudioSystemService _audio;
        private readonly MapCombatPresentationLease _combatLease;
        private readonly Func<bool> _canWork;
        private ChessPieceMotion _motion;
        private StageViewCommand _command;
        private CombatEntryRequest _combatRequest;
        private bool _hasContext;
        private bool _completed;
        private bool _disposed;

        internal bool IsPresenting => _hasContext && !_completed && !_disposed;
        internal bool IsNoncombatPresenting => IsPresenting && _combatRequest == null;
        internal int PresentedChapterKey => _command.Data.ChapterKey;

        internal StageMapNodeMovement(StageMapViewDisplay display, Transform mapRoot, StageMapViewData data,
            IDelayService delay, MapCombatPresentationLease combatLease, Func<bool> canWork, IAudioSystemService audio)
        {
            _display = display ?? throw new ArgumentNullException(nameof(display));
            _mapRoot = mapRoot != null ? mapRoot : throw new ArgumentNullException(nameof(mapRoot));
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _delay = delay ?? throw new ArgumentNullException(nameof(delay));
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _combatLease = combatLease;
            _canWork = canWork ?? throw new ArgumentNullException(nameof(canWork));
        }

        bool IStageMapEntryPresentation.PresentAccepted(StageViewCommand command, CombatEntryRequest combatRequest) =>
            PresentAccepted(command, combatRequest);

        internal bool PresentAccepted(StageViewCommand command, CombatEntryRequest combatRequest)
        {
            if (!CanWork()) return false;
            command.Data.Validate();
            if (command.Number <= 0)
                throw new InvalidOperationException("[StageMap] 이동 요청 번호가 필요합니다.");
            bool same = _hasContext && StageEntryIdentity.Of(_command) == StageEntryIdentity.Of(command) &&
                ReferenceEquals(_combatRequest, combatRequest);
            if (same) return _completed;
            if (_hasContext && !_completed)
                throw new InvalidOperationException("[StageMap] 이동 중 다른 요청을 표시할 수 없습니다.");
            if (combatRequest != null && (_combatLease == null ||
                StageEntryIdentity.Of(combatRequest) != StageEntryIdentity.Of(command) || !combatRequest.IsPrepared))
                throw new InvalidOperationException("[StageMap] 수락·준비 완료된 같은 전투 요청만 표시할 수 있습니다.");

            _command = command;
            _combatRequest = combatRequest;
            _hasContext = true;
            _completed = false;
            Transform movingRoot = _display.BeginMarkerMove(command.Data.NodeId.Value, _mapRoot);
            if (!CanWork()) return false;
            if (!_display.TryGetNode(command.Data.NodeId.Value, out Transform target))
                throw new InvalidOperationException("[StageMap] 이동 목적지가 표시에서 사라졌습니다.");
            Vector3 destination = _mapRoot.InverseTransformPoint(target.position);
            float duration = _data.MarkerMoveDuration;
            var settings = new ChessPieceMotionSettings(_data.MarkerJumpHeight,
                duration * 12f / 59f, duration * 35f / 59f, duration * 12f / 59f);
            _motion = new ChessPieceMotion(_delay, movingRoot, settings, true);
            _motion.Begin(destination, destination);
            // 사전 출전 이동 뒤 같은 노드의 수락을 표시하는 경우에는 이동이 없어 소리를 중복 재생하지 않습니다.
            if (CanWork() && _motion?.IsMoving == true)
                _audio.Play(UnitEvent.UnitMovement, (int)UnitMovementPreset.Base);
            // 예약 서비스의 동기 콜백에서 소유권을 잃으면 늦은 Delay가 Transform을 붙들지 않습니다.
            if (!CanWork()) Dispose();
            return false;
        }

        internal void Tick()
        {
            if (!CanWork() || _motion == null || !_motion.TryConsumeCompletion()) return;
            ChessPieceMotion finished = _motion;
            _motion = null;
            finished.Dispose();
            if (!CanWork()) return;
            _display.CompleteMarkerMove(_command.Data.NodeId.Value);
            if (!CanWork()) return;
            if (_combatRequest != null && !_combatLease.TryComplete(_combatRequest))
                throw new InvalidOperationException("[StageMap] 현재 전투 표시의 착지 완료를 게시할 수 없습니다.");
            _completed = true;
        }

        internal void SynchronizeCurrentNode(int currentNodeId)
        {
            if (!CanWork()) return;
            if (_hasContext && !_completed)
            {
                if (currentNodeId != _command.Data.NodeId.Value)
                    throw new InvalidOperationException("[StageMap] 이동 목적지와 권위 현재 위치가 다릅니다.");
                return;
            }
            _display.SetCurrentNode(currentNodeId);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            ChessPieceMotion owned = _motion;
            _motion = null;
            _combatRequest = null;
            owned?.Dispose();
        }

        private bool CanWork()
        {
            if (_disposed) return false;
            bool current = _canWork();
            return current && !_disposed;
        }
    }
}
