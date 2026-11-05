using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 쏘기(발사 반동) 코드 모션 부품입니다(범용 연출 구조 설계 G4). 직선 투사체 공격 묶음이 씁니다.
    /// 곡사 모션(<see cref="CombatMortarMotionSO"/>)이 같은 반동 값을 이어받으므로 봉인하지 않습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "CombatCodeMotion", menuName = "ProjectL/Database/View/Combat Code Motion (Shoot)")]
    internal class CombatShootMotionSO : CombatCodeMotionSO
    {
        [SerializeField, Min(0f)] private float recoilDistance = 0.1f;
        [SerializeField, Min(0f)] private float recoilAngle = 7f;
        [SerializeField, Min(0.01f)] private float shootDuration = 0.42f;
        [Tooltip("준비를 제외한 발사 동작 안의 발사 시점 비율입니다. 초 단위가 아닙니다.")]
        [SerializeField, Range(0f, 1f)] private float fireTime = 0.3f;

        internal override CombatAttackPresentationKind Kind => CombatAttackPresentationKind.Linear;
        internal float RecoilDistance => recoilDistance;
        internal float RecoilAngle => recoilAngle;
        internal float ShootDuration => shootDuration;
        internal float FireTime => fireTime;
    }
}
