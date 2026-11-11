using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>정의의 역할·소유 권한이 개체의 소유자와 메인 여부에 맞는지 검사한다. 전투 준비와 보유 능력치 조회가 같이 쓴다.</summary>
    internal static class UnitEligibilityRules
    {
        internal static void Require(UnitDefinition definition, Owner owner, bool isMain)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (definition.Role != (isMain ? UnitRole.MainCharacter : UnitRole.Regular) ||
                owner == Owner.Player && definition.OwnerEligibility == UnitOwnerEligibility.EnemyOnly ||
                owner == Owner.Enemy && definition.OwnerEligibility == UnitOwnerEligibility.PlayerOnly)
                throw new InvalidOperationException("캐릭터 정의의 역할 또는 소유 권한이 맞지 않습니다.");
            if (owner == Owner.Player)
                foreach (var trait in definition.TraitDefinitions.Span)
                    if (TraitOwnerPolicy.IsEnemyOnly(trait.TraitKey))
                        throw new InvalidOperationException($"플레이어 캐릭터는 적 전용 특성을 가질 수 없습니다: {definition.Key}, {trait.TraitKey}.");
        }
    }
}
