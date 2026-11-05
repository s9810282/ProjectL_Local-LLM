namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>특성 발동의 유닛 행동이 머무는 동안 볼 쪽입니다(유닛 행동 설계 AA1).</summary>
    internal enum CombatUnitActionFace
    {
        /// <summary>지금 방향 그대로입니다. 옮겨 가면 가는 쪽을 봅니다.</summary>
        Keep = 0,

        /// <summary>발동 대상(가로채기는 지켜진 유닛) 쪽을 봅니다.</summary>
        Target = 1,

        /// <summary>공격자 쪽을 봅니다. 공격 반응(가로채기)에서만 뜻이 있고, 다른 발동에서는 지금 방향 그대로입니다.</summary>
        Attacker = 2
    }
}
