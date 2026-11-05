using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.StageView;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>
    /// 연결 한 번의 노드 진입 표시 부품입니다. 전투 표시 소유권 대여, 노드 이동 연출, 진입 결과 관찰자를 갖습니다.
    /// 진입 관찰자가 수락을 확인하면 <see cref="IStageMapEntryPresentation.PresentAccepted"/>로 이동을 시작합니다.
    /// </summary>
    internal sealed class StageMapViewEntry : IStageMapEntryPresentation
    {
        private readonly StageMapViewServices _services;
        private readonly StageMapViewReader _reader;
        private readonly StageMapSelectionPresentation _selection;
        private readonly Func<bool> _canWork;
        private MapCombatPresentationLease _lease;
        private StageMapNodeMovement _movement;
        private StageMapEntryObserver _observer;
        internal StageCombatDeparture Departure { private get; set; }

        internal StageMapViewEntry(StageMapViewServices services, StageMapViewReader reader,
            StageMapSelectionPresentation selection, Func<bool> canWork)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _selection = selection ?? throw new ArgumentNullException(nameof(selection));
            _canWork = canWork ?? throw new ArgumentNullException(nameof(canWork));
        }

        internal bool IsPresenting => _movement?.IsPresenting == true;
        internal bool IsNoncombatPresenting => _movement?.IsNoncombatPresenting == true;

        /// <summary>전투 표시 소유권을 빌리고 노드 이동 연출을 만듭니다. 도중에 연결이 끝나면 빌린 것을 돌려주고 false입니다.</summary>
        internal bool StartMovement(StageMapViewScene scene)
        {
            if (scene == null) throw new ArgumentNullException(nameof(scene));
            MapCombatPresentationLease lease = _services.PresentationOwner.BindMapPresentationOwner()
                ?? throw new InvalidOperationException("[StageMap] 표시 소유권이 반환되지 않았습니다.");
            if (!_canWork())
            {
                lease.Dispose();
                return false;
            }
            _lease = lease;
            _movement = new StageMapNodeMovement(scene.Display, scene.Root.MapRoot, _services.MapData,
                _services.Delay, lease, _canWork, _services.Audio);
            return true;
        }

        /// <summary>진입 요청의 처리 결과를 관찰합니다. 비전투 착지는 <paramref name="restoreNoncombat"/>로 팝업을 되살립니다.</summary>
        internal void StartObserver(Action<int> restoreNoncombat, Action<string> report)
        {
            _observer = new StageMapEntryObserver(_services.Requests, _services.RequestWriter, _services.Results,
                _services.Current, _services.Attempts, restoreNoncombat, report, this, _canWork);
        }

        /// <summary>진입 결과 관찰 → 이동 중 진행 확인 → 이동 연출 순서로 한 Tick을 진행합니다.</summary>
        internal void Tick()
        {
            _observer.Tick();
            if (!_canWork()) return;
            ValidateMovingProgress();
            if (!_canWork()) return;
            _movement.Tick();
        }

        internal void SynchronizeCurrentNode(int nodeId) => _movement.SynchronizeCurrentNode(nodeId);

        bool IStageMapEntryPresentation.PresentAccepted(StageViewCommand command, CombatEntryRequest request)
        {
            if (!_canWork()) return false;
            // Observer가 같은 요청의 준비·Job 완료·수락 결과를 대조한 뒤에만 호출합니다.
            // 준비 전 거절·취소는 이 경계에 오지 않으므로 선택 화면을 숨기지 않습니다.
            if (request != null) _selection.HideSelection();
            if (_canWork() && Departure?.CompleteAccepted(command, request, _lease) == true)
            {
                return true;
            }
            return _canWork() && _movement.PresentAccepted(command, request);
        }

        /// <summary>이동 연출과 표시 소유권을 돌려줍니다. 교체(Supersede)와 폐기가 모두 부르며, 두 번 불러도 됩니다.</summary>
        internal void Release()
        {
            MapCombatPresentationLease lease = _lease;
            StageMapNodeMovement movement = _movement;
            _lease = null;
            _movement = null;
            _observer = null;
            List<Exception> errors = null;
            CleanupErrors.Try(() => lease?.Dispose(), ref errors);
            CleanupErrors.Try(() => movement?.Dispose(), ref errors);
            CleanupErrors.ThrowIfAny(errors, "[StageMap] 노드 이동 정리 중 실패했습니다.");
        }

        // Buffer 읽기는 재진입이 없으므로 그 뒤에 연결 확인을 다시 하지 않습니다(B7).
        private void ValidateMovingProgress()
        {
            if (!_movement.IsPresenting) return;
            if (_services.Current.CurrentMapKey != _movement.PresentedChapterKey)
                throw new InvalidOperationException("[StageMap] 이동 중 현재 챕터가 표시 요청과 다릅니다.");
            if (!_reader.TryReadCurrentNode(out int node))
                throw new InvalidOperationException("[StageMap] 이동 중 권위 현재 위치가 없습니다.");
            _movement.SynchronizeCurrentNode(node);
        }
    }
}
