using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat
{
    /// <summary>
    /// "같은 전투의 완료인가"를 판정하는 한 곳입니다(개선 설계 §7.3 F4). View의 완료 표시·확인·결과 유지가 이것을 부릅니다.
    /// </summary>
    /// <remarks>
    /// <b>Core와의 약속</b>(요청하지 않고 기대는 동작): Core는 한 전투의 완료를 객체 하나로 게시하고, 정리(EndBattle)와 챕터 완료 뒤에도
    /// 같은 참조를 남기며, 다음 전투를 준비할 때 비웁니다. 그래서 같은 참조면 같은 전투의 완료입니다.
    /// 값이 같은 다른 전투의 완료와 구별하려고 값 비교를 쓰지 않습니다. 이 약속이 바뀌면 이 함수 하나를 고칩니다.
    /// </remarks>
    internal static class CombatCompletionIdentity
    {
        /// <summary>둘이 같은 전투의 완료인지입니다. 어느 쪽이든 없으면 거짓입니다.</summary>
        internal static bool IsSame(BattleCompletionChanges expected, BattleCompletionChanges current) =>
            expected != null && ReferenceEquals(expected, current);
    }
}
