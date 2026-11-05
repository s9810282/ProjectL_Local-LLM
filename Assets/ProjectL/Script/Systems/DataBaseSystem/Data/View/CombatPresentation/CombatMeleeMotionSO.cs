using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>근접 몸통 박치기 코드 모션 부품입니다(범용 연출 구조 설계 G4). 투사체 부품이 없는(근접) 공격 묶음이 씁니다.</summary>
    [CreateAssetMenu(fileName = "CombatCodeMotion", menuName = "ProjectL/Database/View/Combat Code Motion (Melee)")]
    internal sealed class CombatMeleeMotionSO : CombatCodeMotionSO
    {
        [SerializeField, Min(0.01f)] private float meleeDuration = 0.48f;
        [SerializeField, Min(0f)] private float meleeWindupAngle = 22f;
        [SerializeField, Min(0f)] private float meleeImpactAngle = 55f;
        [SerializeField, Min(0f)] private float meleePullback = 0.08f;
        [SerializeField, Min(0f)] private float meleeLunge = 0.25f;

        internal override CombatAttackPresentationKind Kind => CombatAttackPresentationKind.Melee;
        internal float Duration => meleeDuration;
        internal float WindupAngle => meleeWindupAngle;
        internal float ImpactAngle => meleeImpactAngle;
        internal float Pullback => meleePullback;
        internal float Lunge => meleeLunge;
    }
}
