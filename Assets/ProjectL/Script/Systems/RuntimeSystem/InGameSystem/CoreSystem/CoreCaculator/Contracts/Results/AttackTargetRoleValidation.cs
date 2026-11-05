using System;
using System.Linq;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    /// <summary>관통 기록이 없는 일반 공격의 피격 역할 규칙이다. 결과와 미리보기 팩토리가 이 검사 하나를 함께 쓴다.</summary>
    internal static class AttackTargetRoleValidation
    {
        /// <summary>
        /// 칸 지정이면 주 대상만 Primary이고 나머지는 Additional이다. 칸이 없는 지정(방향·전체)이면 모두 Designated다.
        /// </summary>
        internal static void ValidateOrdinary<T>(CellCoord? primaryCell, EntityId primaryTargetId, T[] hits,
            Func<T, EntityId> targetId, Func<T, AttackTargetRole> role, string paramName, Func<T, IReadOnlyList<AttackHitContactSnapshot>> contacts = null)
        {
            if (contacts != null && hits.Any(h => contacts(h) != null))
            {
                if (hits.Any(h => contacts(h) == null)) throw new ArgumentException("신규 접촉과 옛 피격을 혼합할 수 없습니다.", paramName);
                ValidateOrdinary(primaryCell, primaryTargetId, hits.SelectMany(contacts).ToArray(), c => c.OriginalTargetId, c => c.Role, paramName);
                return;
            }
            foreach (T hit in hits)
            {
                AttackTargetRole hitRole = role(hit);
                bool valid = primaryCell.HasValue
                    ? (hitRole == AttackTargetRole.Primary || hitRole == AttackTargetRole.Additional) &&
                      (hitRole == AttackTargetRole.Primary) == (targetId(hit) == primaryTargetId)
                    : hitRole == AttackTargetRole.Designated;
                if (!valid)
                    throw new ArgumentException("피격 역할이 지정 결과와 일치하지 않습니다.", paramName);
            }
        }
    }
}
