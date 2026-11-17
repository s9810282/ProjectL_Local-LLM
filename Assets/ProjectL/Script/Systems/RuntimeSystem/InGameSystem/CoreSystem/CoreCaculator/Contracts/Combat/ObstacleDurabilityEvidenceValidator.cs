using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>장애물 사건을 순서대로 대조하고, 확정할 내구도·위치와 원인 연결을 검사한다.</summary>
    public static class ObstacleDurabilityEvidenceValidator
    {
        public static void Validate(CombatCommandTransition transition)
        {
            if (transition == null) throw new ArgumentNullException(nameof(transition));
            var input = transition.Input;
            if (input.Kind != CombatCommandKind.DetonatePowderBarrel && input.ObstacleContents.Count == 0 && !transition.Events.Any(e =>
                e is CombatObstacleDurabilityEvent or CombatObstacleReactionEvent))
                return;

            var profiles = input.ObstacleContents.ToDictionary(p => p.EntityId);
            var current = input.State.Structures.Where(s => profiles.ContainsKey(s.EntityId)).ToDictionary(s => s.EntityId);
            var seen = new HashSet<CombatEvent>();
            var directContacts = new HashSet<(EntityId, CombatDamageOrigin)>();
            var barrelContacts = new HashSet<(EntityId, CombatObstacleReactionEvent)>();
            bool explicitDetonation = false;
            CombatObstacleDurabilityEvent previous = null;
            foreach (var fact in transition.Events)
            {
                if (previous != null && previous.Cause != ObstacleDestructionCause.Fall)
                {
                    var reaction = fact as CombatObstacleReactionEvent;
                    Require(reaction != null && reaction.ObstacleId == previous.ObstacleId &&
                        reaction.ContentKind == profiles[previous.ObstacleId].Kind &&
                        reaction.Reaction == (previous.Final.IsDestroyed ? ObstacleReactionKind.Destroyed : ObstacleReactionKind.Hit) &&
                        reaction.Origin.PhysicalCell == previous.Expected.Anchor &&
                        reaction.Origin.CreditedCharacterId == previous.Origin.CreditedCharacterId &&
                        reaction.Origin.AttackGroup == reaction.AttackGroup &&
                        reaction.SuppressSupplyRewards == (previous.Cause == ObstacleDestructionCause.PillarCrush ||
                            previous.ContextEvent is CombatObstacleReactionEvent parent && parent.SuppressSupplyRewards),
                        "타수 변경 직후의 장애물 반응");
                }
                else Require(fact is not CombatObstacleReactionEvent, "타수 변경에 연결되지 않은 장애물 반응");

                previous = null;
                switch (fact)
                {
                    case CombatAttackEvent attack:
                        AddContacts(attack.Attack.Hits.ToArray(), directContacts);
                        break;
                    case CombatRoarEvent roar when roar.Attack.HasValue:
                        AddContacts(roar.Attack.Value.Hits.ToArray(), directContacts);
                        break;
                    case CombatPlayerSkillAttackEvent skill:
                        AddContacts(skill.Attack.Hits, directContacts);
                        break;
                    case CombatEffectsEvent effects:
                        foreach (var trait in effects.Traits)
                            if (trait.ChildAttack != null) AddContacts(trait.ChildAttack.Hits, directContacts);
                        break;
                    case CombatStructureEvent move when current.TryGetValue(move.Final.EntityId, out var at):
                        Require(!at.IsDestroyed && at.SameValues(move.Expected) && move.MovementCause != null &&
                            at.WithPlacement(move.Final.Anchor, move.Final.OccupiedCells).SameValues(move.Final),
                            "타수 변경 없는 장애물 이동");
                        current[at.EntityId] = move.Final;
                        break;
                    case CombatObstacleDurabilityEvent change:
                        Require(current.TryGetValue(change.ObstacleId, out var before) && !before.IsDestroyed &&
                            before.SameValues(change.Expected), "장애물 타수의 현재 기대값");
                        ValidateChange(transition, profiles[change.ObstacleId], change, seen, directContacts, barrelContacts);
                        if (change.Cause == ObstacleDestructionCause.ExplicitDetonation)
                        {
                            Require(!explicitDetonation, "명령당 한 번의 명시 기폭");
                            explicitDetonation = true;
                        }
                        current[change.ObstacleId] = change.Final;
                        previous = change;
                        break;
                }
                seen.Add(fact);
            }

            Require(previous == null || previous.Cause == ObstacleDestructionCause.Fall, "마지막 타수 변경의 장애물 반응");
            Require(input.Kind != CombatCommandKind.DetonatePowderBarrel || explicitDetonation, "명시 기폭의 원본 사건");
            Require(directContacts.All(contact => current.TryGetValue(contact.Item1, out var target) && target.IsDestroyed),
                "생존 장애물의 모든 명중에 대응하는 타수 변경");
            var final = transition.FinalState.Structures.Where(s => s.ObstacleDurability != null).ToDictionary(s => s.EntityId);
            Require(final.Count == current.Count && current.All(pair =>
                final.TryGetValue(pair.Key, out var value) && pair.Value.SameValues(value)), "타수 사건과 게시 상태의 대응");
        }

        private static void ValidateChange(CombatCommandTransition transition, ObstacleContentProfile profile,
            CombatObstacleDurabilityEvent change, HashSet<CombatEvent> seen,
            HashSet<(EntityId, CombatDamageOrigin)> directContacts,
            HashSet<(EntityId, CombatObstacleReactionEvent)> barrelContacts)
        {
            var before = change.Expected.ObstacleDurability;
            var after = change.Final.ObstacleDurability;
            bool singleHit = change.Cause is ObstacleDestructionCause.DeliveredHit or
                ObstacleDestructionCause.EnvironmentalContact or ObstacleDestructionCause.BarrelBlast;
            Require(after.RequiredHits == before.RequiredHits &&
                after.RemainingHits == (singleHit ? before.RemainingHits - 1 : 0) &&
                change.Expected.WithObstacleDurability(after).SameValues(change.Final), "타수 변경의 범위와 필요 타수 보존");
            Require(change.ContextEvent == null || seen.Contains(change.ContextEvent), "타수 변경 이전의 원인 사건");
            var tile = change.EnvironmentSource;
            Require(tile == null || change.Cause == ObstacleDestructionCause.EnvironmentalContact &&
                (transition.Input.State.Environment.Tiles.Any(tile.SameValues) ||
                 seen.OfType<CombatMovementSourceEvent>().Any(s => tile.SameValues(s.Expected) || tile.SameValues(s.Final))),
                "실제 환경 접촉 원본");
            Require(change.Origin == null || transition.Input.State.IsInside(change.Origin.PhysicalCell.Value),
                "타수 변경의 보드 내 물리 출처");
            switch (change.Cause)
            {
                case ObstacleDestructionCause.DeliveredHit:
                    Require(change.ContextEvent == null && directContacts.Remove((change.ObstacleId, change.Origin)),
                        "전달된 타격의 유일한 접촉");
                    break;
                case ObstacleDestructionCause.BarrelBlast:
                case ObstacleDestructionCause.BarrelChain:
                    var barrel = change.ContextEvent as CombatObstacleReactionEvent;
                    Require(barrel != null && barrel.ObstacleId != change.ObstacleId &&
                        barrel.ContentKind == ObstacleContentKind.PowderBarrel && barrel.Reaction == ObstacleReactionKind.Destroyed &&
                        barrel.Origin.PhysicalCell == change.Origin.PhysicalCell &&
                        barrel.Origin.CreditedCharacterId == change.Origin.CreditedCharacterId &&
                        barrel.Origin.AttackGroup == change.Origin.AttackGroup &&
                        (profile.Kind == ObstacleContentKind.PowderBarrel) == (change.Cause == ObstacleDestructionCause.BarrelChain) &&
                        barrelContacts.Add((change.ObstacleId, barrel)), "폭발 한 번당 장애물 접촉과 연쇄 원본");
                    break;
                case ObstacleDestructionCause.ExplicitDetonation:
                    Require(profile.Kind == ObstacleContentKind.PowderBarrel && change.ContextEvent == null &&
                        transition.Input.Kind == CombatCommandKind.DetonatePowderBarrel, "명시적으로 기폭한 화약통");
                    break;
                case ObstacleDestructionCause.PillarCrush:
                    var pillar = change.ContextEvent as CombatPillarFallEvent;
                    Require(pillar != null && pillar.DestroyedObstacleIds.Contains(change.ObstacleId) &&
                        change.Expected.OccupiedCells.Any(pillar.ImpactCells.Contains) &&
                        ReferenceEquals(change.Origin, pillar.Reaction.Origin), "기둥 낙하에 포함된 장애물과 물리 출처");
                    break;
                case ObstacleDestructionCause.Fall:
                    var fall = change.ContextEvent as CombatFallEvent;
                    Require(fall != null && fall.EntityId == change.ObstacleId &&
                        change.Expected.OccupiedCells.Contains(fall.Cell) &&
                        transition.Input.EnvironmentBindings.TerrainSourceIds.Contains(fall.TerrainSourceId),
                        "추락한 장애물과 지형 원본");
                    break;
            }
        }

        private static void AddContacts(IReadOnlyList<AttackHitResult> hits, HashSet<(EntityId, CombatDamageOrigin)> contacts)
        {
            foreach (var hit in hits)
                if (hit.IsObstacle && hit.Contacts != null)
                    foreach (var contact in hit.Contacts)
                        if (contact.AppliedSplashPercent > 0 && contact.OriginalTargetId == hit.TargetId)
                            Require(contacts.Add((hit.TargetId, contact.Origin)), "중복된 장애물 접촉");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("장애물 내구도 근거가 올바르지 않습니다: " + message);
        }
    }
}
