namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 유닛 하나의 전투 사건 클립 묶음 값입니다(클립 설계 AC1). 칸은 이름 있는 둘(공격·피격)이고 열거형으로 두지 않습니다(계획 P2).
    /// 칸이 비면 그 사건은 지금처럼 코드 모션입니다. Idle은 여기 없고 프리팹 Animator의 컨트롤러가 원본입니다(설계 AC9).
    /// </summary>
    internal sealed class CombatUnitAnimationData
    {
        internal CombatUnitAnimationData(CombatClipAttackData attack, CombatClipData hit)
        {
            Attack = attack;
            Hit = hit;
        }

        /// <summary>공격 칸입니다. 비어 있으면 null이고 공격은 코드 모션입니다.</summary>
        internal CombatClipAttackData Attack { get; }

        /// <summary>피격 칸입니다. 비어 있으면 null이고 피격은 코드 반응만 씁니다.</summary>
        internal CombatClipData Hit { get; }
    }
}
