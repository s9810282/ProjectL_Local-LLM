namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>공격 클립을 시작할 기준입니다. 0은 기존 순차 재생을 유지합니다.</summary>
    internal enum CombatAttackAnimationStartMode
    {
        AfterPreAttackPresentation = 0,
        FromSelectedTraitPresentationStart = 1,
    }
}
