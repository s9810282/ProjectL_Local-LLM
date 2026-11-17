using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    public static class ObstacleCommandCalculator
    {
        /// <summary>현재 전투의 화약통을 즉시 폭발시키고, 기존 게시 경계가 받을 전이를 계산한다.</summary>
        public static CombatCommandTransition DetonatePowderBarrel(CombatCommandInput input, EntityId barrelId,
            CombatDamageOrigin origin, IReadOnlyList<IMovementTileRule> rules)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (origin == null) throw new ArgumentNullException(nameof(origin));
            if (!origin.PhysicalCell.HasValue || !input.State.IsInside(origin.PhysicalCell.Value))
                throw new ArgumentException("기폭 원인은 보드 안의 위치가 필요합니다.", nameof(origin));
            MovementEnvironmentCalculator.RequireRules(rules);
            ObstacleContentProfile.Validate(input.ObstacleContents, input.EnvironmentBindings, input.State.Structures);
            if (!input.ObstacleContents.Any(p => p.EntityId == barrelId && p.Kind == ObstacleContentKind.PowderBarrel))
                throw new ArgumentException("현재 전투의 화약통이 필요합니다.", nameof(barrelId));

            // 기폭은 유닛의 일반 행동 사용을 소비하지 않는다. 입력의 전투/명령 원본 식별자는 유지한다.
            var intent = CombatCommandComposition.Intent(input, CombatCommandKind.DetonatePowderBarrel, default);
            if (input.State.Structures.Single(s => s.EntityId == barrelId).IsDestroyed)
                return CombatCommandCalculator.Reject(intent, "PowderBarrelAlreadyDestroyed");
            var projection = MovementProjection.ForArrival(intent, intent.Characters.ToDictionary(c => c.EntityId),
                new List<CombatEvent>(), rules: rules);
            projection.DetonatePowderBarrel(barrelId, origin);
            return MovementUndoPolicy.Reconcile(BossRoarCalculator.ApplyPendingReactions(projection.Complete(), rules));
        }
    }
}
