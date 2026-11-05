namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>이미 빌린 유닛 한 번의 표시 수명에만 적용하는 공격 자세 계약입니다.</summary>
    /// <remarks>구현은 ID·대여 수명을 확인하고, 다음 대여를 이전 RestorePose가 수정하지 못하게 합니다.</remarks>
    internal interface ICombatAttackUnitPose : ICombatUnitPose
    {
    }
}
