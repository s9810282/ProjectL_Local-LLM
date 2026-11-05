using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Reaction
{
    /// <summary>
    /// 맞은 방향(피해 근원 → 맞은 유닛, 필드 수평)의 원본입니다(피격 이펙트 설계 HE2). 피격 반응의 밀림 방향과
    /// 피격 이펙트의 회전이 이 함수 하나를 부릅니다.
    /// </summary>
    internal static class CombatHitDirection
    {
        /// <summary>
        /// 사건의 피해 근원 칸이 하나로 정해지면 그 칸 → 맞은 유닛 칸 방향입니다. 근원 칸이 없거나 둘 이상 다르거나 유닛 위치가 없으면
        /// 지금 회전의 뒤쪽, 그것도 수평 성분이 없으면 필드 기본 회전의 뒤쪽입니다. 결과는 필드 위쪽에 수직인 단위 벡터입니다.
        /// 유닛 위치는 결과 사실의 것이고, 사망 사건처럼 결과에 위치가 없으면(필드에서 빠짐) 사건 전 위치입니다.
        /// </summary>
        /// <param name="current">맞은 유닛의 지금 표시 회전입니다(반응 자세 대여의 회전).</param>
        internal static Vector3 Resolve(CombatCharacterEvent e, FieldGridMapper mapper, Quaternion current)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            CellCoord? source = null;
            bool ambiguous = false;
            foreach (var origin in e.DamageOrigins)
            {
                if (!origin.PhysicalCell.HasValue)
                {
                    ambiguous = true;
                    break;
                }
                if (source.HasValue && !source.Value.Equals(origin.PhysicalCell.Value))
                {
                    ambiguous = true;
                    break;
                }
                source = origin.PhysicalCell;
            }
            CellCoord? victim = e.Final.Anchor ?? e.Expected.Anchor;
            Vector3 direction = !ambiguous && source.HasValue && victim.HasValue
                ? Vector3.ProjectOnPlane(mapper.CellCenterToWorld(victim.Value) - mapper.CellCenterToWorld(source.Value), mapper.WorldUp)
                : Vector3.zero;
            if (direction.sqrMagnitude < .000001f)
                direction = Vector3.ProjectOnPlane(-(current * Vector3.forward), mapper.WorldUp);
            if (direction.sqrMagnitude < .000001f)
                direction = mapper.RotationToWorld(Rotation4.R0) * Vector3.back;
            return direction.normalized;
        }
    }
}
