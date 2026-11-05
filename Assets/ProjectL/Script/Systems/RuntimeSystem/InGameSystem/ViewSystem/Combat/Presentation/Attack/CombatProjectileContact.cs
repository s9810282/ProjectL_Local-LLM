using System;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack
{
    /// <summary>
    /// 투사체가 대상에 닿는 순간의 자리(월드 위치·회전)입니다(투사체 접촉 이펙트 설계 IE3). 발사 때 경로가 정해지면 한 번 만들고,
    /// 맞음 콜백이 그대로 넘깁니다. 무엇을 낼지는 받는 쪽(공격 재생)이 정합니다 — 이 값은 사실만 담습니다.
    /// </summary>
    internal readonly struct CombatProjectileContact
    {
        private CombatProjectileContact(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }

        /// <summary>닿는 자리의 월드 위치입니다. 곡사는 착탄점, 직선은 발사 경로의 접촉 지점입니다.</summary>
        internal Vector3 Position { get; }

        /// <summary>투사체의 수평 진행 방향(필드 위쪽 기준)입니다. 경로에 수평 길이가 없으면 대체 회전입니다.</summary>
        internal Quaternion Rotation { get; }

        /// <summary>
        /// 발사 경로 <paramref name="from"/>→<paramref name="to"/>를 <paramref name="progress"/>(0~1로 맞춤)만큼 간 자리입니다. 곡사 착탄은 1입니다.
        /// 회전은 방향 계산 원본 <see cref="CombatAttackFacing.Toward"/>를 그대로 씁니다(공격자가 보는 방향과 같은 규칙).
        /// </summary>
        /// <param name="up">필드 위쪽(정규화)입니다.</param>
        /// <param name="fallback">경로에 수평 길이가 없을 때의 회전입니다. 그 공격이 이미 쓰는 공격 방향을 넘깁니다(계획 P5).</param>
        /// <exception cref="ArgumentOutOfRangeException">진행률이 유한하지 않을 때입니다(부르는 쪽이 이미 걸러야 하는 값).</exception>
        internal static CombatProjectileContact Along(Vector3 from, Vector3 to, float progress, Vector3 up, Quaternion fallback)
        {
            if (!float.IsFinite(progress))
                throw new ArgumentOutOfRangeException(nameof(progress), progress, "접촉 진행률은 유한해야 합니다.");
            return new CombatProjectileContact(Vector3.Lerp(from, to, progress), CombatAttackFacing.Toward(from, to, up, fallback));
        }
    }
}
