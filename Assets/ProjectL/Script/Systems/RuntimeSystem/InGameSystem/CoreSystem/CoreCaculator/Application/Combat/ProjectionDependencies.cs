using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>한 명령 계산이 읽은 공개 의존이다. 미리보기 문맥의 오래됨 판정만 이 기록을 사용한다.</summary>
    internal sealed class ProjectionDependencies
    {
        /// <summary>규칙 후보나 폭발 수신자로 체력·보유 상태를 읽은 개체다.</summary>
        internal HashSet<EntityId> EffectTargetIds { get; } = new();
        /// <summary>지형 방어·넉백 판정에서 읽은 셀이다.</summary>
        internal HashSet<CellCoord> TerrainQueryCells { get; } = new();
        /// <summary>도착 처리(영혼 수집·위치 보정)를 마친 개체의 셀이다.</summary>
        internal Dictionary<EntityId, CellCoord> ArrivalCells { get; } = new();
    }
}
