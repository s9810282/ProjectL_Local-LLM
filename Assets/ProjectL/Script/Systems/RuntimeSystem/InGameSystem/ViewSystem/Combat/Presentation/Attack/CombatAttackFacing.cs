using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack
{
    /// <summary>
    /// 공격자가 공격할 때 볼 방향입니다(클립 설계 E2, 계획 T20). 코드 모션(<see cref="CombatAttackMotion"/>)과 클립 공격(<c>CombatClipAttack</c>)이
    /// 같은 계산을 씁니다. 목표점은 공격 종류 전략(<see cref="CombatAttackStyle.TryTarget"/>)이 답하고, 이 클래스는 그 점을 향하는 회전만 만듭니다.
    /// </summary>
    internal static class CombatAttackFacing
    {
        /// <summary>
        /// <paramref name="origin"/>에서 <paramref name="target"/>을 향하는 수평(<paramref name="up"/>에 수직) 회전입니다.
        /// 수평 거리가 거의 0이면 <paramref name="fallback"/>입니다.
        /// </summary>
        internal static Quaternion Toward(Vector3 origin, Vector3 target, Vector3 up, Quaternion fallback)
        {
            Vector3 direction = Vector3.ProjectOnPlane(target - origin, up);
            return direction.sqrMagnitude > .000001f ? Quaternion.LookRotation(direction, up) : fallback;
        }

        /// <summary>
        /// 공격 모양 하나에서 공격자가 볼 방향입니다. 목표점이 없거나(곡사인데 주 칸 없음) 목표가 출발점과 같으면 공격 방향입니다.
        /// 코드 모션은 같은 목표점·공격 방향·위쪽을 생성자로 받아 <see cref="Toward"/>를 부르므로 같은 회전이 나옵니다.
        /// </summary>
        /// <param name="kind">공격자 외형의 공격 연출 종류입니다. 목표점 해석이 외형마다 다릅니다.</param>
        /// <param name="shape">공격 모양입니다(특성 발동 연출 설계 TA5).</param>
        /// <param name="origin">공격자의 지금 위치입니다.</param>
        internal static Quaternion For(CombatAttackPresentationKind kind, CombatAttackShape shape, FieldGridMapper mapper, Vector3 origin)
        {
            if (shape == null) throw new ArgumentNullException(nameof(shape));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            Quaternion fallback = mapper.RotationToWorld(shape.Direction);
            if (!CombatAttackStyles.Of(kind).TryTarget(shape.DeliveryCell, shape.Direction, shape.AffectedCells.Span, mapper, origin,
                    out Vector3 target))
                return fallback;
            // 코드 모션은 받은 위쪽을 정규화해 씁니다. 같은 값을 넘겨 회전이 비트 단위로 같게 합니다.
            return Toward(origin, target, mapper.WorldUp.normalized, fallback);
        }
    }
}
