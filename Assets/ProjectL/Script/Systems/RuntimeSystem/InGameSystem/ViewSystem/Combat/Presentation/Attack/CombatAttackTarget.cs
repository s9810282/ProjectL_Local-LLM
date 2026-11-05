using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack
{
    /// <summary>받은 주 셀·방향·영향 셀을 표시 종점으로 바꿉니다. 사거리·피격·차단을 판정하지 않습니다.</summary>
    /// <remarks>실제 공격(결과)과 궤적 미리보기(응답)가 같은 해석을 씁니다(공격 궤적 설계 TJ3).</remarks>
    internal static class CombatAttackTarget
    {
        /// <summary>원래 선택 칸과 실제 타격 칸을 구분합니다. 끌어오기는 Core가 제공한 도착 칸을 씁니다.</summary>
        internal static CellCoord? DeliveryCell(in AttackResult attack) => attack.Pull?.FinalAnchor ?? attack.PrimaryCell;
        internal static CellCoord? DeliveryCell(in AttackPreview preview) => preview.Pull?.FinalAnchor ?? preview.PrimaryCell;
        internal static bool IsMovementOnly(in AttackResult attack) => attack.Pull != null && attack.Hits.Length == 0;
        internal static bool IsMovementOnly(in AttackPreview preview)
        {
            if (preview.Pull == null) return false;
            foreach (var target in preview.Targets.Span)
                if (target.IsDirectHit) return false;
            return true;
        }

        internal static Vector3 Resolve(in AttackResult attack, FieldGridMapper mapper, Vector3 origin) =>
            Resolve(DeliveryCell(attack), attack.AttackDirection, attack.AffectedCells.Span, mapper, origin);

        /// <summary>
        /// 주 셀이 있으면 그 셀 중심, 없으면 공격 방향 앞으로 가장 먼 영향 셀 중심(같으면 축에 가까운 쪽)입니다.
        /// 영향 셀이 없으면 출발점입니다.
        /// </summary>
        internal static Vector3 Resolve(CellCoord? primaryCell, Rotation4 direction, ReadOnlySpan<CellCoord> affectedCells,
            FieldGridMapper mapper, Vector3 origin)
        {
            if (primaryCell.HasValue)
                return mapper.CellCenterToWorld(primaryCell.Value);
            Vector3 forward = mapper.RotationToWorld(direction) * Vector3.forward;
            Vector3 best = origin;
            float farthest = 0f;
            float closestToAxis = float.MaxValue;
            foreach (var cell in affectedCells)
            {
                Vector3 candidate = mapper.CellCenterToWorld(cell);
                Vector3 offset = Vector3.ProjectOnPlane(candidate - origin, mapper.WorldUp);
                float along = Vector3.Dot(offset, forward);
                float lateral = (offset - forward * along).sqrMagnitude;
                if (along > farthest + .00001f ||
                    (Mathf.Abs(along - farthest) <= .00001f && along > 0f && lateral < closestToAxis))
                {
                    best = candidate;
                    farthest = along;
                    closestToAxis = lateral;
                }
            }
            // 빈 방향 결과는 사거리나 가짜 셀을 만들지 않고 출발점에서 유한하게 완료합니다.
            return best;
        }
    }
}
