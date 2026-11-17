using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    /// <summary>유닛 하나의 이동 목적지와 목적지별 경로를 전달한다.</summary>
    public readonly struct MovementRangeResult
    {
        private readonly MovementPath[] _routes;

        public bool Succeeded { get; }
        public MovementFailureReason FailureReason { get; }
        public EntityId UnitId { get; }
        public ReadOnlyMemory<MovementDestination> Destinations { get; }

        /// <summary>계산에 사용한 판의 리비전이다. 0은 게시 출처가 아직 부착되지 않은 값이다.</summary>
        public long SourceRevision { get; }

        private MovementRangeResult(bool succeeded, MovementFailureReason failureReason, EntityId unitId,
            MovementPath[] routes, ReadOnlyMemory<MovementDestination> destinations, long sourceRevision)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
            UnitId = unitId;
            _routes = routes;
            Destinations = destinations;
            SourceRevision = sourceRevision;
        }

        public static MovementRangeResult Failure(MovementFailureReason failureReason, EntityId unitId)
        {
            return new MovementRangeResult(false, failureReason, unitId,
                Array.Empty<MovementPath>(), Array.Empty<MovementDestination>(), 0);
        }

        /// <summary>같은 원점에서 출발하는 경로를 보관하고 각 경로에서 목적지를 만든다.</summary>
        public static MovementRangeResult Success(EntityId unitId, IReadOnlyList<MovementPath> routes)
        {
            if (routes == null) throw new ArgumentNullException(nameof(routes));
            if (unitId.IsEmpty) throw new ArgumentException("이동 범위에는 유효한 유닛 식별자가 필요합니다.", nameof(unitId));

            int count = routes.Count;
            var copiedRoutes = new MovementPath[count];
            var destinations = new MovementDestination[count];
            var endpoints = new HashSet<CellCoord>();
            for (int i = 0; i < count; i++)
            {
                var route = routes[i];
                if (route == null || route.StepCount == 0)
                    throw new ArgumentException("이동 범위의 각 경로에는 한 걸음 이상이 필요합니다.", nameof(routes));
                if (i > 0 && route.Origin != copiedRoutes[0].Origin)
                    throw new ArgumentException("이동 범위의 모든 경로는 같은 원점에서 시작해야 합니다.", nameof(routes));

                var target = route.Endpoint;
                if (!endpoints.Add(target))
                    throw new ArgumentException("이동 범위에 같은 목적지가 두 번 이상 나올 수 없습니다.", nameof(routes));

                copiedRoutes[i] = route;
                destinations[i] = new MovementDestination(target, new[] { target }, route.StepCount);
            }

            return new MovementRangeResult(true, MovementFailureReason.None, unitId, copiedRoutes, destinations, 0);
        }

        /// <summary>목적지에 저장된 경로를 찾는다. 경로가 없으면 false와 null을 반환한다.</summary>
        public bool TryGetRoute(CellCoord target, out MovementPath route)
        {
            if (_routes != null)
            {
                foreach (var candidate in _routes)
                {
                    if (candidate.Endpoint != target) continue;

                    route = candidate;
                    return true;
                }
            }

            route = null;
            return false;
        }

        /// <summary>기존 결과와 경로를 유지하며 양수인 계산 출처를 붙인 응답을 만든다.</summary>
        public MovementRangeResult WithSourceRevision(long revision)
        {
            if (revision <= 0) throw new ArgumentOutOfRangeException(nameof(revision));
            if (_routes == null)
                throw new InvalidOperationException("초기화되지 않은 이동 범위에 계산 출처를 붙일 수 없습니다.");

            return new MovementRangeResult(Succeeded, FailureReason, UnitId, _routes, Destinations, revision);
        }
    }
}
