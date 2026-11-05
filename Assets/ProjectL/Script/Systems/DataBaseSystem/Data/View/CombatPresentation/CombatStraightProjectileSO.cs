using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>직선 투사체 부품입니다(범용 연출 구조 설계 G4). 이 부품을 참조한 공격 묶음은 직선(<see cref="CombatAttackPresentationKind.Linear"/>)입니다.</summary>
    [CreateAssetMenu(fileName = "CombatProjectile", menuName = "ProjectL/Database/View/Combat Projectile (Straight)")]
    internal sealed class CombatStraightProjectileSO : CombatProjectileSO
    {
        [Tooltip("직선 총알의 초당 평균 이동 거리입니다(완급 곡선이 빠르기를 나눔).")]
        [SerializeField, Min(0.01f)] private float projectileSpeed = 20f;
        [Tooltip("직선 총알의 필드 법선 방향 높이입니다.")]
        [SerializeField, Min(0f)] private float projectileHeight = 0.6f;

        internal override CombatAttackPresentationKind Kind => CombatAttackPresentationKind.Linear;
        internal float Speed => projectileSpeed;
        internal float Height => projectileHeight;
    }
}
