namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>위치 교환 한 번의 대기시간입니다. 이펙트와 배치가 같은 완료 시점을 사용합니다.</summary>
    internal sealed class CombatPositionSwapVisualData
    {
        internal CombatPositionSwapVisualData(float waitSeconds)
        {
            CombatPresentationValueRules.RequireNonNegative(waitSeconds, nameof(waitSeconds));
            WaitSeconds = waitSeconds;
        }

        internal float WaitSeconds { get; }
    }
}
