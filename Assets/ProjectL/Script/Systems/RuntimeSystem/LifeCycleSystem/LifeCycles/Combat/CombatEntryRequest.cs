using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat
{
    /// <summary>View가 읽는 실행 계층의 활성화 사실입니다. Core 전투 상태를 복제하지 않습니다.</summary>
    public interface ICombatEntryRead
    {
        bool IsActive { get; }
    }

    /// <summary>전투 대상의 진입 요청과 실행 전 취소만 제공합니다. 결과는 반환된 요청에서 읽습니다.</summary>
    /// <remarks>
    /// <b>진입 요청의 사전 조건</b>(어기면 <see cref="System.InvalidOperationException"/>, 런타임 오류 경계로 멈춤):
    /// 초기화가 끝났고, 공용 Job Runner가 비었고 로딩 중이 아니며, 이전 전투가 남지 않았고, 이전 요청이 끝났다(<see cref="CombatEntryRequest.IsReplaceable"/>).
    /// Runner·로딩은 일시적 조건이므로 부르는 쪽이 먼저 확인합니다(개선 설계 §6.3). 오류 상태면 예외 없이 실패한 요청을 돌려줍니다.
    /// </remarks>
    public interface ICombatEntryService : IServiceModule, ICombatEntryRead, IMapCombatPresentationOwnerSource
    {
        /// <summary>번호 없는 직행 진입입니다(<see cref="CombatEntryKind.Direct"/>). 사전 조건은 인터페이스 설명에 있습니다.</summary>
        CombatEntryRequest RequestEntry(int chapterKey, StageNodeId nodeId);
        /// <summary>맵 입력에서 발급한 양수 번호를 기존 전투 준비 수명에 전달합니다(<see cref="CombatEntryKind.Map"/>).</summary>
        CombatEntryRequest RequestEntry(int chapterKey, StageNodeId nodeId, int mapCommandNumber);
        bool CancelEntry(CombatEntryRequest request);

        /// <summary>
        /// 진입 서비스가 지금 맡은 요청입니다(등록 직전부터 다음 요청이 대신할 때까지, 끝난 요청도 포함). 없으면 null입니다.
        /// 전투 Plan이 진입 등록과 이탈 확인에 씁니다(재구성 설계 J14, 골격 점검 설계 K-2).
        /// </summary>
        CombatEntryRequest Current { get; }
    }

    /// <summary>진입 요청이 어디서 왔는지입니다(개선 설계 §6.2). 맵 진입만 맵 입력 번호와 착지 표시를 가집니다.</summary>
    public enum CombatEntryKind
    {
        /// <summary>번호 없는 직행 진입(개발 Bootstrap)입니다. 착지 표시를 기다리지 않습니다.</summary>
        Direct,
        /// <summary>맵 입력 번호로 들어온 진입입니다. 맵 표시가 착지한 뒤 전환합니다.</summary>
        Map
    }

    /// <summary>진입 요청의 단계입니다. 단계 사이 이동은 <see cref="CombatEntryRequest"/>의 전이 표 한 곳이 정합니다.</summary>
    public enum CombatEntryStatus
    {
        Created,
        Queued,
        Running,
        Prepared,
        Rejected,
        Cancelled,
        Activating,
        Active,
        Closed,
        Faulted
    }

    /// <summary>
    /// 요청 당시의 대상을 실행까지 유지하고 한 호출의 결과를 보관합니다.
    /// 외부는 읽기만 하며, 진입 서비스와 전투 Job만 자신의 실행 단계를 변경합니다.
    /// </summary>
    /// <remarks>
    /// 단계 이동은 <see cref="CanMove"/> 표 하나를 따릅니다. <see cref="Fail"/>만 예외로, 끝나지 않은 모든 단계에서 받으며 예외를 내지 않습니다
    /// (실패를 처리하는 catch가 원래 예외를 가리지 않게). 소비자는 단계 값을 해석하는 대신 이름 있는 질문을 읽습니다.
    /// </remarks>
    public sealed class CombatEntryRequest
    {
        private MapCombatPresentationTicket _mapPresentation;
        private bool _awaitingLanding;

        public int ChapterKey { get; }
        public StageNodeId NodeId { get; }
        public CombatEntryKind Kind { get; }
        /// <summary>맵 입력 번호입니다. <see cref="CombatEntryKind.Direct"/>는 0입니다.</summary>
        public int MapCommandNumber { get; }
        public CombatEntryStatus Status { get; private set; }
        public ChapterProgressionResult Result { get; private set; }
        public bool IsJobCompleted { get; private set; }

        /// <summary>만들어졌고 아직 등록되지 않았는지입니다. 전투 Plan은 이 요청만 진입으로 받습니다(골격 점검 설계 K-2).</summary>
        public bool IsAwaitingRegistration => Status == CombatEntryStatus.Created;

        /// <summary>준비가 수락된 맵 진입이 맵 표시 착지를 기다리는지입니다. 착지 게시가 전환을 다시 등록합니다(개선 설계 §6.1).</summary>
        public bool IsAwaitingLanding => _awaitingLanding && Status == CombatEntryStatus.Prepared;

        /// <summary>
        /// Core가 진입을 수락해 준비를 마쳤고 진입 Job도 끝났는지입니다. 이 단계는 수락된 결과로만 들어가므로 <see cref="Result"/>는 늘 수락입니다.
        /// 맵 진입은 이 상태에서 맵 표시 착지를 기다립니다.
        /// </summary>
        public bool IsPrepared => Status == CombatEntryStatus.Prepared && IsJobCompleted;

        /// <summary>Combat을 나가 요청이 닫혔는지입니다.</summary>
        public bool HasExited => Status == CombatEntryStatus.Closed;

        /// <summary>등록됐고 아직 실행 전인지입니다(실행 전 취소 가능).</summary>
        public bool IsQueued => Status == CombatEntryStatus.Queued;

        /// <summary>
        /// 수락된 요청의 활성화가 시작돼(Combat 전환 요청 뒤, 씬 로딩 중 포함) Combat 씬 진입의 활성화를 기다리는지입니다.
        /// 활성화 시작은 진입 Job 배출 뒤에만 오므로 배출 조건은 확인을 겹칠 뿐입니다.
        /// </summary>
        public bool IsActivating => Status == CombatEntryStatus.Activating && IsJobCompleted;

        /// <summary>
        /// 요청이 활성 단계인지입니다. 요청 단계만 봅니다. 진입 서비스의 <see cref="ICombatEntryRead.IsActive"/>는
        /// 여기에 서비스 준비·오류 없음까지 더한 것입니다.
        /// </summary>
        public bool IsActive => Status == CombatEntryStatus.Active;

        /// <summary>실행 전 취소나 실행 중 입력 소멸로 취소됐는지입니다.</summary>
        public bool IsCancelled => Status == CombatEntryStatus.Cancelled;

        /// <summary>Core가 진입을 정상 거절했는지입니다. 사유는 <see cref="Result"/>에 있습니다.</summary>
        public bool IsRejected => Status == CombatEntryStatus.Rejected;

        /// <summary>다음 요청이 대신해도 되는 요청입니다. 거절·취소는 진입 Job이 배출된 뒤, 이탈한 요청은 닫힌 뒤입니다.</summary>
        public bool IsReplaceable => HasExited || ((IsRejected || IsCancelled) && IsJobCompleted);

        /// <summary>이번 요청과 같은 대상(챕터·노드)인지입니다. 진입 전 입력 대조에 씁니다.</summary>
        public bool IsTargetOf(int chapterKey, int nodeId) => ChapterKey == chapterKey && NodeId.Value == nodeId;

        /// <summary>수락된 이번 요청과 같은 대상(챕터·노드·수락된 스테이지)인지입니다. 수락 전이면 거짓입니다.</summary>
        public bool IsTargetOf(int chapterKey, int nodeId, int stageKey) =>
            Result != null && Result.Accepted && IsTargetOf(chapterKey, nodeId) && Result.StageKey.Value == stageKey;

        internal MapCombatPresentationState ReadMapPresentationState() => Kind == CombatEntryKind.Direct
            ? MapCombatPresentationState.Ready
            : _mapPresentation?.ReadState() ?? MapCombatPresentationState.Abandoned;

        /// <summary>이 요청의 착지 표시를 주어진 소유권이 맡는지입니다. 소유권을 잃을 때 서비스가 확인합니다.</summary>
        internal bool UsesPresentationOwner(MapCombatPresentationLease owner) =>
            _mapPresentation != null && _mapPresentation.IsOwnedBy(owner);

        internal void CaptureMapPresentation(MapCombatPresentationLease owner)
        {
            Require(CombatEntryStatus.Created);
            if (Kind != CombatEntryKind.Map || _mapPresentation != null || owner == null || !owner.IsAttached)
                throw new InvalidOperationException("맵 요청에는 유효한 표시 소유권을 한 번만 연결해야 합니다.");
            _mapPresentation = new MapCombatPresentationTicket(owner);
        }

        internal bool TryCompleteMapPresentation(MapCombatPresentationLease owner)
        {
            return Kind == CombatEntryKind.Map && IsPrepared &&
                Result != null && Result.Accepted && _mapPresentation != null && _mapPresentation.TryComplete(owner);
        }

        /// <summary>전환 시도가 착지 전이라 끝날 때 남깁니다. 착지 게시가 전환을 다시 등록합니다(개선 설계 §6.1).</summary>
        internal void MarkAwaitingLanding()
        {
            Require(CombatEntryStatus.Prepared);
            if (Kind != CombatEntryKind.Map || !IsJobCompleted || ReadMapPresentationState() != MapCombatPresentationState.Pending)
                throw new InvalidOperationException("착지 대기는 준비가 끝나고 착지 전인 맵 진입에만 남길 수 있습니다.");
            _awaitingLanding = true;
        }

        internal CombatEntryRequest(int chapterKey, StageNodeId nodeId, int mapCommandNumber = 0)
        {
            if (chapterKey < 1)
                throw new ArgumentOutOfRangeException(nameof(chapterKey));
            if (nodeId.IsEmpty)
                throw new ArgumentException("진입할 노드가 필요합니다.", nameof(nodeId));
            if (mapCommandNumber < 0)
                throw new ArgumentOutOfRangeException(nameof(mapCommandNumber));
            ChapterKey = chapterKey;
            NodeId = nodeId;
            MapCommandNumber = mapCommandNumber;
            Kind = mapCommandNumber == 0 ? CombatEntryKind.Direct : CombatEntryKind.Map;
        }

        internal void MarkQueued() => Move(CombatEntryStatus.Queued);

        /// <summary>실행 전(생성·등록)에만 취소합니다. 실행이 시작된 요청은 거짓입니다.</summary>
        internal bool TryCancel()
        {
            if (Status != CombatEntryStatus.Created && Status != CombatEntryStatus.Queued)
                return false;
            Move(CombatEntryStatus.Cancelled);
            return true;
        }

        internal bool TryStart()
        {
            if (Status == CombatEntryStatus.Cancelled)
                return false;
            Move(CombatEntryStatus.Running);
            return true;
        }

        /// <summary>Core가 입력 소멸을 확인하여 실행하지 않은 준비 Job만 정상 취소로 끝냅니다.</summary>
        internal void CancelUnprocessed()
        {
            Require(CombatEntryStatus.Running);
            Move(CombatEntryStatus.Cancelled);
        }

        internal void PublishResult(ChapterProgressionResult result)
        {
            Require(CombatEntryStatus.Running);
            if (result == null || (result.Accepted &&
                (!result.SelectStage || result.ChapterKey != ChapterKey || result.NodeId != NodeId || result.StageKey.IsEmpty)))
                throw new InvalidOperationException("전투 진입 결과가 이번 요청과 대응하지 않습니다.");
            Result = result;
            Move(result.Accepted ? CombatEntryStatus.Prepared : CombatEntryStatus.Rejected);
        }

        internal void CompleteJob()
        {
            if (Status != CombatEntryStatus.Prepared && Status != CombatEntryStatus.Rejected && Status != CombatEntryStatus.Cancelled)
                throw new InvalidOperationException("전투 진입 결과 없이 Job을 완료할 수 없습니다.");
            IsJobCompleted = true;
        }

        internal void BeginActivation()
        {
            Require(CombatEntryStatus.Prepared);
            if (!IsJobCompleted)
                throw new InvalidOperationException("진입 Job 완료 전에 활성화할 수 없습니다.");
            if (ReadMapPresentationState() != MapCombatPresentationState.Ready)
                throw new InvalidOperationException("맵 표시 완료를 확인한 뒤 전투를 활성화해야 합니다.");
            _mapPresentation?.Pass();
            _awaitingLanding = false;
            Move(CombatEntryStatus.Activating);
        }

        internal void Activate() => Move(CombatEntryStatus.Active);

        /// <summary>Combat을 나갔습니다. 활성이었던 요청만 닫습니다.</summary>
        internal void Close() => Move(CombatEntryStatus.Closed);

        /// <summary>실패로 끝냅니다. 끝나지 않은 모든 단계에서 받고, 이미 끝났으면(닫힘·실패) 그대로 둡니다. 예외를 내지 않습니다.</summary>
        internal void Fail()
        {
            if (Status != CombatEntryStatus.Closed && Status != CombatEntryStatus.Faulted)
                Status = CombatEntryStatus.Faulted;
        }

        /// <summary>
        /// 단계 전이 표입니다. 요청의 단계 이동은 모두 여기를 지납니다(<see cref="Fail"/> 제외).
        /// 생성 → 등록 → 실행 → 수락(준비)·거절 → 활성화 시작 → 활성 → 닫힘이며, 실행 전(생성·등록)과 실행 중 입력 소멸은 취소로 끝납니다.
        /// </summary>
        internal static bool CanMove(CombatEntryStatus from, CombatEntryStatus to) => (from, to) switch
        {
            (CombatEntryStatus.Created, CombatEntryStatus.Queued) => true,
            (CombatEntryStatus.Created, CombatEntryStatus.Cancelled) => true,
            (CombatEntryStatus.Queued, CombatEntryStatus.Running) => true,
            (CombatEntryStatus.Queued, CombatEntryStatus.Cancelled) => true,
            (CombatEntryStatus.Running, CombatEntryStatus.Prepared) => true,
            (CombatEntryStatus.Running, CombatEntryStatus.Rejected) => true,
            (CombatEntryStatus.Running, CombatEntryStatus.Cancelled) => true,
            (CombatEntryStatus.Prepared, CombatEntryStatus.Activating) => true,
            (CombatEntryStatus.Activating, CombatEntryStatus.Active) => true,
            (CombatEntryStatus.Active, CombatEntryStatus.Closed) => true,
            _ => false
        };

        private void Move(CombatEntryStatus to)
        {
            if (!CanMove(Status, to))
                throw new InvalidOperationException($"전투 진입 단계를 바꿀 수 없습니다: {Status} → {to}");
            Status = to;
        }

        private void Require(CombatEntryStatus expected)
        {
            if (Status != expected)
                throw new InvalidOperationException($"전투 진입 단계가 다릅니다. 기대: {expected}, 실제: {Status}");
        }
    }
}
