using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 특성 발동 유닛 행동의 끝 자리(위치·방향) 계산 한 곳입니다(유닛 행동 설계 AA4). 같은 입력이면 같은 자리를 냅니다.
    /// </summary>
    /// <remarks>
    /// 위치는 모두 서 있는 자리(발밑, 칸 표면 높이 포함)입니다. 몸 가운데가 아닙니다.
    /// 거리는 칸 비율이고 필드의 칸 폭(<see cref="FieldGridMapper.WorldCellWidth"/>)으로 바꿉니다. 방향은 필드 평면(<see cref="FieldGridMapper.WorldUp"/>에 수직)에서 잽니다.
    /// 자리가 쓸 기준점이 없으면(대상·공격자 없음) 제자리입니다. 그 경고는 부르는 쪽(행동 조립)이 냅니다(설계 P10).
    /// </remarks>
    internal static class CombatUnitActionPlaces
    {
        /// <param name="place">갈 자리입니다.</param>
        /// <param name="distance">기준점에서 떨어지는 거리(칸 비율)입니다.</param>
        /// <param name="face">머무는 동안 볼 쪽입니다.</param>
        /// <param name="home">행동 유닛이 지금 서 있는 자리(서는 자리 대여를 빌릴 때 자리)입니다.</param>
        /// <param name="target">대상(가로채기는 지켜진 유닛)이 서 있는 자리입니다. 없으면 null입니다.</param>
        /// <param name="attacker">공격자가 서 있는 자리입니다. 공격 반응(가로채기)에만 있고, 없으면 null입니다.</param>
        internal static Pose EndPose(CombatUnitActionPlace place, float distance, CombatUnitActionFace face, Pose home,
            Vector3? target, Vector3? attacker, FieldGridMapper mapper)
        {
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            Vector3 up = mapper.WorldUp.normalized;
            float reach = distance * mapper.WorldCellWidth;
            Vector3 position = home.position;
            switch (place)
            {
                case CombatUnitActionPlace.Stay:
                    break;
                // 지켜진 유닛 앞, 공격자 쪽입니다(설계 P2). 높이는 지켜진 유닛이 선 칸 표면입니다.
                case CombatUnitActionPlace.GuardFront:
                    if (target.HasValue && attacker.HasValue)
                        position = target.Value + Horizontal(attacker.Value - target.Value, up) * reach;
                    break;
                case CombatUnitActionPlace.TowardTarget:
                    if (target.HasValue)
                        position = home.position + Horizontal(target.Value - home.position, up) * reach;
                    break;
                case CombatUnitActionPlace.AwayFromAttacker:
                    if (attacker.HasValue)
                        position = home.position + Horizontal(home.position - attacker.Value, up) * reach;
                    break;
                // 위치 이동의 끝 자리는 기준점이 아니라 미뤄 둔 사실 자리입니다(계획 TP-E5). 그 자세는 행동 조립이 받아 그대로 씁니다.
                case CombatUnitActionPlace.Relocate:
                    throw new ArgumentException("위치 이동(Relocate)의 끝 자리는 미뤄 둔 사실 자리입니다. CombatUnitActionComposer.ComposeRelocate를 씁니다.", nameof(place));
                default:
                    throw new ArgumentOutOfRangeException(nameof(place), place, "정의되지 않은 행동 자리입니다.");
            }
            return new Pose(position, FacingOf(face, home, position, target, attacker, up));
        }

        // 머무는 동안의 방향입니다. 볼 기준점이 없으면 지금 방향(옮겨 갔으면 가는 쪽)입니다.
        private static Quaternion FacingOf(CombatUnitActionFace face, Pose home, Vector3 position, Vector3? target, Vector3? attacker,
            Vector3 up)
        {
            Quaternion keep = CombatAttackFacing.Toward(home.position, position, up, home.rotation);
            switch (face)
            {
                case CombatUnitActionFace.Keep:
                    return keep;
                case CombatUnitActionFace.Target:
                    return target.HasValue ? CombatAttackFacing.Toward(position, target.Value, up, keep) : keep;
                case CombatUnitActionFace.Attacker:
                    return attacker.HasValue ? CombatAttackFacing.Toward(position, attacker.Value, up, keep) : keep;
                default:
                    throw new ArgumentOutOfRangeException(nameof(face), face, "정의되지 않은 보는 쪽입니다.");
            }
        }

        // 필드 평면 위의 단위 방향입니다. 거의 0이면 0(그 자리에 섬)입니다.
        private static Vector3 Horizontal(Vector3 direction, Vector3 up)
        {
            Vector3 flat = Vector3.ProjectOnPlane(direction, up);
            return flat.sqrMagnitude > .000001f ? flat.normalized : Vector3.zero;
        }
    }
}
