using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>곡사 코드 모션 부품입니다(범용 연출 구조 설계 G4). 쏘기 반동 값에 무기 버킷의 준비·복귀를 더합니다. 곡사 투사체 공격 묶음이 씁니다.</summary>
    [CreateAssetMenu(fileName = "CombatCodeMotion", menuName = "ProjectL/Database/View/Combat Code Motion (Mortar)")]
    internal sealed class CombatMortarMotionSO : CombatShootMotionSO
    {
        [SerializeField, Min(0f)] private float prepareDuration = 0.15f;
        [SerializeField, Min(0f)] private float returnDuration = 0.25f;
        [SerializeField] private float bucketPitch = -20f;

        internal override CombatAttackPresentationKind Kind => CombatAttackPresentationKind.Mortar;
        internal float PrepareDuration => prepareDuration;
        internal float ReturnDuration => returnDuration;
        internal float BucketPitch => bucketPitch;
    }
}
