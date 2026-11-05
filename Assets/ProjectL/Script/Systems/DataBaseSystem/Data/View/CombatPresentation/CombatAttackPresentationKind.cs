namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>전투 규칙과 독립적인 공격 표시 종류입니다.</summary>
    /// <remarks>옛 <c>Ballista = 4</c>(관통 화살)는 Ballista 외형이 곡사로 바뀌어(2026-10-26) 쓰는 에셋이 없어 걷었습니다(2026-10-27). 값 4는 다시 쓰지 않습니다.</remarks>
    internal enum CombatAttackPresentationKind
    {
        Linear = 1,
        Melee = 2,
        Mortar = 3
    }
}
