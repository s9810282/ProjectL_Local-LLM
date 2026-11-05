using System;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat
{
    /// <summary>맵 표시 연결의 소유권만 제공합니다. 별도로 등록하는 서비스가 아닙니다.</summary>
    public interface IMapCombatPresentationOwnerSource
    {
        MapCombatPresentationLease BindMapPresentationOwner();
    }

    /// <summary>한 맵 연결이 같은 전투 요청의 착지 완료를 게시하는 권한입니다.</summary>
    public sealed class MapCombatPresentationLease : IDisposable
    {
        private CombatEntryService _service;

        internal MapCombatPresentationLease(CombatEntryService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        internal bool IsAttached => _service != null;

        /// <summary>이번 소유권과 현재 준비 요청이 일치할 때만 착지를 한 번 게시합니다.</summary>
        public bool TryComplete(CombatEntryRequest request) =>
            _service != null && _service.TryCompleteMapPresentation(this, request);

        public void Dispose()
        {
            CombatEntryService service = _service;
            // 요청이 옛 lease를 보관해도 실행 계층을 붙들지 않도록 먼저 참조를 끊습니다.
            _service = null;
            service?.ReleaseMapPresentationOwner(this);
        }
    }
}
