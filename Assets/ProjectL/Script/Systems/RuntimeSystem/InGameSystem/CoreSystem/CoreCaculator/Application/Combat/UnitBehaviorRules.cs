using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>
    /// 행동 가능 여부의 유일한 판정 경로다. 명령 입장·범위 조회·미리보기·AI가 같은 값을 쓴다.
    /// 정의는 전투 준비에 연결된 공격 프로필의 유닛 정의에서 읽는다.
    /// </summary>
    internal static class UnitBehaviorRules
    {
        internal static bool CanMoveVoluntarily(CombatCommandInput input, CharacterCombatFacts character,
            CombatProfileReadSet profileReads = null) => Of(input, character, profileReads).CanMoveVoluntarily;

        internal static bool CanBasicAttack(CombatCommandInput input, CharacterCombatFacts character) =>
            Of(input, character).CanBasicAttack;

        internal static bool IsForcedMovementImmune(CombatCommandInput input, CharacterCombatFacts character,
            CombatProfileReadSet profileReads = null) => Of(input, character, profileReads).IsForcedMovementImmune;

        /// <summary>보유 개체의 유효 프로필에서 행동 정의를 읽는다. 강화 개체는 기본형으로 대체하지 않는다.</summary>
        private static UnitBehaviorSpec Of(CombatCommandInput input, CharacterCombatFacts character,
            CombatProfileReadSet profileReads = null) =>
            CombatCommandCalculator.RequiredProfile(input, character, profileReads).Definition.Behavior;
    }
}
