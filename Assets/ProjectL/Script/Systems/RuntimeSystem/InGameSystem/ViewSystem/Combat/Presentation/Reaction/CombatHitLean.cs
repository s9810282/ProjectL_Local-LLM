using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Reaction
{
    /// <summary>
    /// 맞아서 몸을 젖히는 자세의 원본입니다. 피격 반응(젖혔다 폄)과 넉백(맞는 순간 젖히며 밀림, 도착 뒤 폄, 임시 — 2026-11-11 사용자 결정)이 같은 식을 씁니다.
    /// 젖히는 축은 맞은(밀리는) 방향에 수직인 필드 수평 축이고, 위가 그 방향으로 기웁니다. 각도는 반응 값의 Hit Angle입니다.
    /// 피격 반응 곡선은 sin(π × 경과 / 피격 시간)이라 가장 크게 젖히는 순간은 피격 시간의 절반입니다. 젖히기(오르기)·펴기 시간은 이 절반을 씁니다.
    /// </summary>
    internal static class CombatHitLean
    {
        /// <summary>방향(필드 수평)에 수직인 젖힘 축입니다.</summary>
        internal static Vector3 AxisOf(Vector3 direction, Vector3 worldUp) => Vector3.Cross(worldUp, direction).normalized;

        /// <summary>똑바른 회전을 축으로 각도 × 양만큼 젖힌 회전입니다. 양은 0(똑바름)~1(Hit Angle)입니다.</summary>
        internal static Quaternion Apply(Quaternion upright, Vector3 axis, float angle, float amount) =>
            Quaternion.AngleAxis(angle * amount, axis) * upright;

        /// <summary>가장 크게 젖힐 때까지의 시간이자 펴는 시간입니다(피격 시간의 절반).</summary>
        internal static float RiseSeconds(CombatUnitReactionPresentationData settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            return settings.HitDuration * .5f;
        }

        /// <summary>젖히기 시작에서 경과한 시간의 젖힘 양입니다. 피격 반응 곡선의 앞 절반과 같고, 오르기 시간 뒤에는 1입니다.</summary>
        internal static float RiseAmount(float elapsed, CombatUnitReactionPresentationData settings) =>
            Mathf.Sin(Mathf.PI * .5f * Mathf.Clamp01(elapsed / RiseSeconds(settings)));
    }
}
