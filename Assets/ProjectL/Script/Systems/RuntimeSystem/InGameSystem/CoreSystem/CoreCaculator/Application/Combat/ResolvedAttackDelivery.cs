using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>
    /// 기본 지정과 독립적으로 확정한 전달 결과다. 일반 공격과 관통 모두 같은 모양의 피격 목록과 영향 칸을 내보낸다.
    /// 관통 기록은 공개 연출 사실로 따로 남긴다.
    /// </summary>
    internal sealed class ResolvedAttackDelivery
    {
        /// <summary>전달 순서의 피격 목록이다. 일반 공격은 지정 대상의 ID 순서, 관통은 조우 순서다.</summary>
        internal IReadOnlyList<DeliveredAttackHit> Hits { get; }
        internal IReadOnlyList<CellCoord> AffectedCells { get; }
        internal PiercingShotDelivery PiercingShot { get; }
        internal IReadOnlyList<AttackBlockedContact> BlockedContacts { get; }

        internal ResolvedAttackDelivery(IEnumerable<DeliveredAttackHit> hits, IEnumerable<CellCoord> affectedCells,
            PiercingShotDelivery piercingShot = null, IEnumerable<AttackBlockedContact> blockedContacts = null)
        {
            Hits = Array.AsReadOnly((hits ?? throw new ArgumentNullException(nameof(hits))).ToArray());
            AffectedCells = Array.AsReadOnly((affectedCells ?? throw new ArgumentNullException(nameof(affectedCells))).ToArray());
            PiercingShot = piercingShot;
            BlockedContacts = Array.AsReadOnly((blockedContacts ?? Array.Empty<AttackBlockedContact>()).ToArray());
        }
    }
}
