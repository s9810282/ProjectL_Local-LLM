using ProjectL.Systems.ServiceKeys.Runtime;
using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.StageView;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRegister;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat
{
    /// <summary>
    /// 전투 진입 요청 하나의 수명을 맡습니다(재구성 설계 J5·J6, 골격 설계 §6.3 K20).
    /// 요청을 받아 진입 연쇄([진입] → [진입 전환])를 <see cref="JobType.Entry"/>로 한 번 등록하고 그 등록 결과(등록됨·등록 실패)를 요청에 표시하며,
    /// Combat 씬 진입 때 수락된 요청을 활성화합니다(골격 점검 설계 K-2).
    /// </summary>
    /// <remarks>
    /// 진입 결과를 Tick에서 감시하지 않습니다. 수락된 맵 요청은 전환 시도가 착지 대기를 남기고 끝나며,
    /// 맵 표시가 착지를 게시하면 이 서비스가 진입 전환을 한 번 다시 등록합니다(개선 설계 §6.1, Runner를 붙들지 않음).
    /// 끝난 요청(<see cref="CombatEntryRequest.IsReplaceable"/>)은 다음 요청이 대신합니다. Buffer를 직접 받지 않고 읽기 객체로 확인합니다.
    /// </remarks>
    internal sealed class CombatEntryService : ICombatEntryService, ISystemPostBindService, ISystemInitialize,
        ISystemShutdown
    {
        private readonly RuntimeFaultBoundary _faults;
        private readonly ICombatProgressReader _progress;
        private IJobRunner _runner;
        private IJobRegisterSystem _register;
        private ILodingSystemService _loading;
        private IStageViewBufferReadSink _mapInput;
        private IStageCommandResultReadSink _mapResults;
        private CombatEntryRequest _current;
        private MapCombatPresentationLease _mapPresentationOwner;
        private bool _ready;

        internal CombatEntryService(RuntimeFaultBoundary faults, ICombatProgressReader progress)
        {
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
        }

        public bool IsActive => _ready && !_faults.IsFaulted && _current != null && _current.IsActive;

        public CombatEntryRequest Current => _current;

        public MapCombatPresentationLease BindMapPresentationOwner()
        {
            if (!_ready || _faults.IsFaulted)
                throw new InvalidOperationException("[CombatEntryService] 실행 중인 서비스에만 맵 표시를 연결할 수 있습니다.");
            var owner = new MapCombatPresentationLease(this);
            MapCombatPresentationLease previous = _mapPresentationOwner;
            _mapPresentationOwner = owner;
            previous?.Dispose();
            return owner;
        }

        /// <summary>
        /// 이번 소유권과 현재 준비 요청이 맞을 때 착지를 한 번 게시합니다. 전환 시도가 이미 착지 대기를 남겼으면
        /// 진입 전환을 다시 등록합니다. 아직 시도 전이면 그 시도가 착지를 보고 바로 전환합니다.
        /// </summary>
        internal bool TryCompleteMapPresentation(MapCombatPresentationLease owner, CombatEntryRequest request)
        {
            if (!_ready || _faults.IsFaulted || owner == null || !owner.IsAttached ||
                !ReferenceEquals(_mapPresentationOwner, owner) || request == null ||
                !ReferenceEquals(_current, request) || !request.TryCompleteMapPresentation(owner))
                return false;
            if (request.IsAwaitingLanding)
                _register.Register(JobRootType.Combat, JobType.EntryTransition);
            return true;
        }

        internal void ReleaseMapPresentationOwner(MapCombatPresentationLease owner)
        {
            if (ReferenceEquals(_mapPresentationOwner, owner))
                _mapPresentationOwner = null;
            // 착지를 기다리던 요청의 표시 소유권이 사라졌다. 전환이 다시 오지 않으므로 여기서 실패로 보고한다(조용한 영구 대기 방지).
            // 입력 정리(오류 정지·정상 종료 전) 중의 해제와 서비스 종료 중의 해제는 그 정리가 맡는다.
            if (_ready && !_faults.IsFaulted && !_faults.IsStoppingInput && _current != null &&
                _current.IsAwaitingLanding && _current.UsesPresentationOwner(owner))
            {
                _current.Fail();
                _faults.Report(new InvalidOperationException(
                    "[CombatEntryService] 착지를 기다리던 전투 진입의 맵 표시 소유권을 잃었습니다."));
            }
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            var global = resolver.GetService(SystemGroupsService.Global);
            BindServices(global.JobServiceCommand.JobRunner, global.JobServiceCommand.JobRegister,
                global.LodingServiceCommand.LodingService);
            var buffers = resolver.GetService(BufferSystemKey.Buffer);
            _mapInput = buffers.StageViewReadSink;
            _mapResults = buffers.StageCommandResults;
        }

        /// <summary>Job·로딩 서비스를 보관합니다(테스트 입구이기도 합니다).</summary>
        internal void BindServices(IJobRunner runner, IJobRegisterSystem register, ILodingSystemService loading)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _register = register ?? throw new ArgumentNullException(nameof(register));
            _loading = loading ?? throw new ArgumentNullException(nameof(loading));
        }

        public void Initialize()
        {
            if (_register == null)
                throw new InvalidOperationException("[CombatEntryService] 서비스를 받기 전에 초기화할 수 없습니다.");
            _ready = true;
        }

        public void Shutdown()
        {
            _ready = false;
            _mapPresentationOwner?.Dispose();
            // Stage가 이미 취소했거나 Core가 정상 거절한 요청의 끝난 의미는 종료 중에도 보존합니다.
            if (_current != null && !_current.TryCancel() && !_current.HasExited && !_current.IsCancelled &&
                !_current.IsRejected)
                _current.Fail();
            _current = null;
            _mapInput = null;
            _mapResults = null;
            _runner = null;
            _register = null;
            _loading = null;
        }

        public CombatEntryRequest RequestEntry(int chapterKey, StageNodeId nodeId) =>
            RequestEntryCore(chapterKey, nodeId, 0);

        public CombatEntryRequest RequestEntry(int chapterKey, StageNodeId nodeId, int mapCommandNumber)
        {
            if (mapCommandNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(mapCommandNumber));
            }
            return RequestEntryCore(chapterKey, nodeId, mapCommandNumber);
        }

        private CombatEntryRequest RequestEntryCore(int chapterKey, StageNodeId nodeId, int mapCommandNumber)
        {
            var request = new CombatEntryRequest(chapterKey, nodeId, mapCommandNumber);
            if (!_ready)
                throw new InvalidOperationException("[CombatEntryService] 초기화 이후에 진입을 요청해야 합니다.");
            if (request.Kind == CombatEntryKind.Map &&
                (!_mapInput.TryGetNextCommand(_mapResults.HandledMapCommandNumber, out var input) ||
                 input.Number != mapCommandNumber || !request.IsTargetOf(input.Data.ChapterKey, input.Data.NodeId.Value)))
            {
                throw new InvalidOperationException("[CombatEntryService] 첫 미처리 맵 입력과 전투 진입 대상이 일치해야 합니다.");
            }
            if (_faults.IsFaulted)
            {
                request.Fail();
                return request;
            }
            if (_current != null && !_current.IsReplaceable)
            {
                if (_current.IsQueued && _current.IsTargetOf(chapterKey, nodeId.Value) &&
                    _current.MapCommandNumber == mapCommandNumber)
                    return _current;
                throw new InvalidOperationException("[CombatEntryService] 이전 요청을 취소·배출하거나 전투 이탈을 완료해야 합니다.");
            }
            // 진입 등록 전 조건(골격 설계 K11, 계약은 ICombatEntryService 설명): 다른 작업·로딩 중이 아니고, 이전 전투가 남아 있지 않다.
            if (!_runner.IsIdle || _loading.IsLoading)
                throw new InvalidOperationException("[CombatEntryService] 진행 중인 작업이나 로딩이 있습니다.");
            if (_progress.Read().HasBattle)
                throw new InvalidOperationException("[CombatEntryService] 이전 전투가 남아 있습니다.");

            if (request.Kind == CombatEntryKind.Map)
            {
                if (_mapPresentationOwner == null || !_mapPresentationOwner.IsAttached)
                    throw new InvalidOperationException("[CombatEntryService] 맵 표시 소유권을 연결한 뒤 진입을 요청해야 합니다.");
                request.CaptureMapPresentation(_mapPresentationOwner);
            }

            // 전투 Plan이 진입 등록 때 이 요청을 Current로 읽으므로 등록보다 먼저 둔다(골격 점검 설계 K-2).
            // 등록기의 LifeCycle 그룹이 이 서비스를 CombatEntry로 가진다는 조립 전제에 기댄다(EntryTransition·ExitCheck도 같음).
            _current = request;
            try
            {
                _register.Register(JobRootType.Combat, JobType.Entry);
                // 요청 단계의 주인은 이 서비스다. Runner에 넣은 뒤에 등록됨으로 표시한다(Runner는 Tick에서만 실행).
                request.MarkQueued();
            }
            catch
            {
                request.Fail();
                throw;
            }
            return request;
        }

        public bool CancelEntry(CombatEntryRequest request)
        {
            return request != null && ReferenceEquals(_current, request) && request.TryCancel();
        }

        /// <summary>
        /// Combat 씬 진입 때 LifeCycle이 동기로 부릅니다(설계 J6). View가 같은 Enter 노드에서 활성 진입을 요구하므로 Job으로 미루지 않습니다.
        /// 이번 요청이 수락·전환됐고 현재 선택이 요청 대상과 같을 때만 활성화합니다.
        /// </summary>
        internal void ActivateAccepted()
        {
            CombatEntryRequest entry = _current;
            try
            {
                if (entry == null || !entry.IsActivating ||
                    !_progress.TryReadSelection(out int chapterKey, out var stage) ||
                    !entry.IsTargetOf(chapterKey, stage.NodeId, stage.StageKey))
                    throw new InvalidOperationException("[CombatEntryService] 이번 요청의 수락 없이 Combat을 활성화할 수 없습니다.");
                entry.Activate();
            }
            catch
            {
                entry?.Fail();
                throw;
            }
        }

        /// <summary>
        /// Combat 씬 이탈 때 LifeCycle이 동기로 부릅니다(개선 설계 §4.6). 전투가 정리됐으면 현재 요청을 이 자리에서 닫고 참을 돌려주므로,
        /// 도착 구간의 OnEnter 전에 "이탈 완료"가 참입니다(활성화 <see cref="ActivateAccepted"/>와 같은 방식).
        /// 전투가 남았으면 닫지 않고 거짓을 돌려주며, 사후 확인 Job이 맡습니다.
        /// </summary>
        internal bool TryCloseExited()
        {
            if (_progress.Read().HasBattle)
                return false;
            _current?.Close();
            return true;
        }
    }
}
