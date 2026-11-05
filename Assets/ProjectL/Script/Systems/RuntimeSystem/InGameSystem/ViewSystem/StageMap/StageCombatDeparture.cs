using System;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap.Item;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.BufferSystems.StageView;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.DelaySystem.View;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Deck;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>출전 전의 미제출 목적지와 표시 이동만 소유합니다. 진행 Buffer를 쓰거나 Core를 호출하지 않습니다.</summary>
    internal sealed class StageCombatDeparture : IDisposable
    {
        private readonly ICurrentProgressBufferReadSink _current;
        private readonly IStageProgressBufferReadSink _stages;
        private readonly IStageViewBufferReadSink _requests;
        private readonly IStageViewBufferWriteSink _writer;
        private readonly StageMapViewReader _reader;
        private readonly StageMapViewDisplay _display;
        private readonly Transform _mapRoot;
        private readonly StageMapViewData _data;
        private readonly IDelayService _delay;
        private readonly IAudioSystemService _audio;
        private readonly DeckEditPopupController _deck;
        private readonly Func<bool> _canWork;
        private ChessPieceMotion _motion;
        private int _chapter;
        private int _node;
        private int _origin;
        private int _submitted;
        private bool _arrived;
        private bool _landingPublished;
        private bool _disposed;

        internal bool IsBusy => _node != 0 && !_disposed;

        internal StageCombatDeparture(ICurrentProgressBufferReadSink current, IStageProgressBufferReadSink stages,
            IStageViewBufferReadSink requests, IStageViewBufferWriteSink writer, StageMapViewReader reader,
            StageMapViewDisplay display, Transform mapRoot, StageMapViewData data, IDelayService delay,
            DeckEditPopupController deck, Func<bool> canWork, IAudioSystemService audio)
        {
            _current = current ?? throw new ArgumentNullException(nameof(current));
            _stages = stages ?? throw new ArgumentNullException(nameof(stages));
            _requests = requests ?? throw new ArgumentNullException(nameof(requests));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _display = display ?? throw new ArgumentNullException(nameof(display));
            _mapRoot = mapRoot != null ? mapRoot : throw new ArgumentNullException(nameof(mapRoot));
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _delay = delay ?? throw new ArgumentNullException(nameof(delay));
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _deck = deck ?? throw new ArgumentNullException(nameof(deck));
            _canWork = canWork ?? throw new ArgumentNullException(nameof(canWork));
        }

        /// <summary>전투 노드만 출전 대기로 가로챕니다. 나머지는 기존 명령 경로를 유지합니다.</summary>
        internal bool TryBegin(int chapter, int nodeId)
        {
            if (_disposed || !_canWork() || IsBusy) return true;
            if (_current.CurrentMapKey != chapter || !_reader.TryReadCurrentNode(out int origin)) return true;
            bool combat = false;
            foreach (var node in _stages.GetStages(chapter))
                if (node.NodeId == nodeId) combat = !node.IsStart && node.StageType == StageType.Combat;
            if (!combat) return false;
            // 선택 가능한지 최종 판단하는 곳은 출전 후 Core입니다. 여기서는 표시된 연결 밖의 미리 이동만 막습니다.
            bool connected = false;
            foreach (var node in _stages.GetStages(chapter))
                if (node.NodeId == origin)
                    foreach (int next in node.NextNodeIds)
                        if (next == nodeId) connected = true;
            if (!connected) return true;
            _chapter = chapter;
            _node = nodeId;
            _origin = origin;
            Transform moving = _display.BeginMarkerMove(nodeId, _mapRoot);
            if (!_canWork() || _disposed) return true;
            if (!_display.TryGetNode(nodeId, out Transform target))
                throw new InvalidOperationException("[StageCombatDeparture] 표시 목적지가 없습니다.");
            Vector3 destination = _mapRoot.InverseTransformPoint(target.position);
            float duration = _data.MarkerMoveDuration;
            _motion = new ChessPieceMotion(_delay, moving, new ChessPieceMotionSettings(_data.MarkerJumpHeight,
                duration * 12f / 59f, duration * 35f / 59f, duration * 12f / 59f), true);
            _motion.Begin(destination, destination);
            if (_canWork() && !_disposed && _motion?.IsMoving == true)
                _audio.Play(UnitEvent.UnitMovement, (int)UnitMovementPreset.Base);
            if (!_canWork() || _disposed) Dispose();
            return true;
        }

        internal void Tick()
        {
            if (!IsBusy || !_canWork()) return;
            if (_current.CurrentMapKey != _chapter ||
                (_submitted == 0 && (!_reader.TryReadCurrentNode(out int current) || current != _origin)))
            {
                Cancel();
                return;
            }
            if (_submitted != 0)
            {
                // 거절·취소의 결과 처리는 기존 Observer가 입력을 제거한 뒤 완료됩니다.
                if (!_requests.TryGetNextCommand(0, out var command) || command.Number != _submitted) Cancel();
                return;
            }
            if (_motion == null || !_motion.TryConsumeCompletion()) return;
            _motion.Dispose();
            _motion = null;
            _display.CompleteMarkerMove(_node);
            _arrived = true;
            if (!_canWork() || _disposed) return;
            if (!_deck.OpenForCombat(Submit, Cancel)) Cancel();
        }

        internal bool CompleteAccepted(StageViewCommand command, CombatEntryRequest request,
            MapCombatPresentationLease lease)
        {
            if (!IsBusy || !_arrived || _submitted != command.Number) return false;
            if (request == null || !request.IsPrepared || command.Data.ChapterKey != _chapter ||
                command.Data.NodeId.Value != _node || StageEntryIdentity.Of(command) != StageEntryIdentity.Of(request))
                throw new InvalidOperationException("[StageCombatDeparture] 출전 요청과 전투 준비 결과가 일치하지 않습니다.");
            if (!_landingPublished)
            {
                // 전환 Job이 실행되기 전 Observer가 다시 보더라도 착지는 한 번만 게시합니다.
                _landingPublished = true;
                if (lease == null || !lease.TryComplete(request))
                    throw new InvalidOperationException("[StageCombatDeparture] 출전한 전투의 착지 완료를 게시할 수 없습니다.");
            }
            return true;
        }

        private void Submit()
        {
            if (!IsBusy || !_arrived || _submitted != 0 || !_canWork()) return;
            if (_current.CurrentMapKey != _chapter || !_reader.TryReadCurrentNode(out int current) ||
                current != _origin || _requests.Commands.Count != 0)
            {
                Cancel();
                return;
            }
            _submitted = _writer.Submit(StageViewCommandData.SelectStageNode(_chapter, new StageNodeId(_node)));
        }

        private void Cancel()
        {
            if (!IsBusy) return;
            _motion?.Dispose();
            _motion = null;
            if (!_arrived) _display.CompleteMarkerMove(_node);
            _node = 0;
            _submitted = 0;
            _arrived = false;
            _landingPublished = false;
            _deck.CloseCombat();
            if (_reader.TryReadCurrentNode(out int current)) _display.SetCurrentNode(current);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _motion?.Dispose();
            _motion = null;
            _node = 0;
        }
    }
}
