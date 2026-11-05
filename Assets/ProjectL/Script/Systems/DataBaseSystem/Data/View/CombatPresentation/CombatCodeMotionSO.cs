using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 공격 묶음이 참조하는 코드 모션 부품 입력입니다(범용 연출 구조 설계 G4). 공격 클립이 없는 유닛의 공격과 보스 포효의 정면 공격이 이 값으로 몸을 움직입니다.
    /// 공격 묶음에서 비워도 됩니다. 그러면 코드 모션 길과 정면 공격은 시작하지 않습니다(계획 P1).
    /// </summary>
    /// <remarks>값 조건의 원본은 공격 값 객체 생성자입니다. 이 SO는 칸을 내줍니다.</remarks>
    internal abstract class CombatCodeMotionSO : ScriptableObject
    {
        /// <summary>
        /// 이 모션이 맞는 공격 표시 종류입니다. 공격 묶음 검사가 투사체 부품에서 나온 종류와 맞는지 봅니다(근접 ↔ 근접, 쏘기 ↔ 직선, 곡사 ↔ 곡사).
        /// </summary>
        internal abstract CombatAttackPresentationKind Kind { get; }
    }
}
