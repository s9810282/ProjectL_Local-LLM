using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem
{
    /// <summary>
    /// 설치된 전투의 불변 정의만 읽는 계약이다. 설치·해제 권한은 주지 않는다.
    /// 현재 가변 전투 상태는 이 계약이 아니라 Buffer에서 읽는다.
    /// </summary>
    internal interface IBattleDefinitionRead
    {
        /// <summary>지정한 전투 인스턴스의 정의를 돌려준다. 설치되지 않았거나 다른 전투이면 상태 오류다.</summary>
        BattleInitialization Require(long battleInstanceId);
    }
}
