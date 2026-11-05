using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common
{
    /// <summary>플레이어 메인의 키·역할·병과·소유 자격 불변식을 검증한다.</summary>
    public static class PlayerMainDefinitionPolicy
    {
        public static void Validate(EntityKey key, UnitRole role, CategoryKey category, UnitOwnerEligibility eligibility)
        {
            if (key == EntityKey.Unit_MainCharacter && eligibility != UnitOwnerEligibility.EnemyOnly)
                throw new InvalidOperationException("퇴역한 메인 키는 플레이어 정의로 사용할 수 없습니다.");
            if (role == UnitRole.MainCharacter &&
                (category != CategoryKey.Main || eligibility != UnitOwnerEligibility.PlayerOnly))
                throw new InvalidOperationException("플레이어 메인은 Main 병과와 PlayerOnly 소유 자격이 필요합니다.");
        }
    }
}
