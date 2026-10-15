using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>공개 투영이 읽은 이동 기대값만 비교한다. 실제 게시 검증과 요청·연출 번호는 별도 경계다.</summary>
    public static class MovementContextCalculator
    {
        public static CombatCommandTransition RejectStale(CombatCommandInput input) =>
            CombatCommandCalculator.Reject(input, Contracts.Results.MovementFailureReason.StaleContext.ToString());
        public static MovementPreviewContext Capture(CombatCommandInput input, Owner observer, IReadOnlyList<IMovementTileRule> rules)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            MovementEnvironmentCalculator.RequireRules(rules);
            var actor = input.Characters.SingleOrDefault(c => c.EntityId == input.ActorId);
            if (actor == null) return null;
            if (input.Kind != CombatCommandKind.Move && input.Kind != CombatCommandKind.ReturnToCheckpoint)
                throw new ArgumentException("경로 이동 또는 체크포인트 복귀 입력이 필요합니다.", nameof(input));
            var entries = new HashSet<CellCoord>();
            var objectCells = new HashSet<CellCoord>();
            var thornCells = new HashSet<CellCoord>();
            var arrivals = new Dictionary<EntityId, CellCoord>();
            var targets = new HashSet<EntityId>();
            var terrainCells = new HashSet<CellCoord>();
            if (input.Kind == CombatCommandKind.Move)
            {
                if (input.Path == null) throw new ArgumentException("이동 문맥에는 명시적 경로가 필요합니다.", nameof(input));
                var projection = CombatMovementCalculator.ProjectRoute(input, rules, true, out _, out _);
                if (projection != null)
                {
                    entries.UnionWith(projection.PathResult().EnteredCells);
                    objectCells.UnionWith(entries);
                    objectCells.UnionWith(projection.Events.OfType<CombatCharacterEvent>()
                        .Where(e => e.Kind == CombatEventKind.Moved && e.Final.Anchor.HasValue).Select(e => e.Final.Anchor.Value));
                    thornCells.UnionWith(entries);
                    thornCells.UnionWith(projection.Events.OfType<CombatCharacterEvent>()
                        .Where(e => e.Kind == CombatEventKind.Moved && e.Final.Anchor.HasValue).Select(e => e.Final.Anchor.Value));
                    foreach (var pair in projection.ArrivalCells) arrivals.Add(pair.Key, pair.Value);
                    targets.UnionWith(projection.EffectTargetIds);
                    terrainCells.UnionWith(projection.TerrainQueryCells);
                    if (projection.Terminal)
                        targets.Add(input.Characters.Single(c => c.OwnershipRole == Contracts.Roster.CharacterOwnershipRole.Main).EntityId);
                }
            }
            else arrivals.Add(actor.EntityId, actor.MovementCheckpointAnchor);
            var visible = input.State.Environment.VisibleTo(observer);
            var pricedCells = input.Kind == CombatCommandKind.Move
                ? new HashSet<CellCoord>(input.Path.Cells) : new HashSet<CellCoord>();
            var environment = new MovementEnvironmentFacts(visible.Tiles.Where(t => t is DestinationTileModifierFact
                    ? arrivals.Values.Contains(t.Cell) : t is StaticTerrainFact terrain
                        ? terrainCells.Contains(t.Cell) || terrain.Kind == StaticTerrainKind.HighGround && pricedCells.Contains(t.Cell)
                        : input.EnvironmentBindings.ObjectSourceIds.Contains(t.Id)
                            ? objectCells.Contains(t.Cell) : entries.Contains(t.Cell)).OrderBy(t => t.Id).ToArray());
            var occupancy = input.Characters.Where(c => c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead)
                .Select(c => (Id: c.EntityId, Owner: c.Owner, Cells: c.OccupiedCells))
                .Concat(input.State.Structures.Where(s => !s.Health.IsDead).Select(s => (Id: s.EntityId, Owner: s.Owner, Cells: s.OccupiedCells)))
                .OrderBy(c => c.Id).Select(c => (c.Id, c.Owner, (IReadOnlyList<CellCoord>)c.Cells.OrderBy(p => p.Y).ThenBy(p => p.X).ToArray())).ToArray();
            var collectors = input.Kind == CombatCommandKind.Move ? input.Characters.Where(c => arrivals.ContainsKey(c.EntityId) && c.OwnedId.HasValue).ToArray()
                : Array.Empty<CharacterCombatFacts>();
            var owned = input.Owned.Where(o => collectors.Any(c => c.OwnedId == o.Id)).OrderBy(o => o.Id.Value).ToArray();
            var definitions = input.Kind == CombatCommandKind.Move ? input.Profiles
                .Where(p => p.Definition.Key == actor.CharacterKey || collectors.Any(c => c.CharacterKey == p.Definition.Key))
                .OrderBy(p => p.Definition.Key).Select(p => p.Definition).ToArray() : Array.Empty<Contracts.Commands.UnitSpec>();
            var artifactInputs = new List<(EffectOriginKey, int, int, int, int)>();
            if (input.Kind == CombatCommandKind.Move)
                foreach (var binding in ArtifactEffectCompiler.Compile(input.Artifacts).Rules.Span)
                {
                    if (binding.Rule is SoulAltarRule soul)
                        artifactInputs.Add((binding.Origin, soul.Amounts.AttackBonus, soul.Amounts.MaxHealthBonus, soul.Amounts.HealAmount, soul.DurationUnitTurns));
                    else if (binding.Rule is HealingChaliceRule heal)
                        artifactInputs.Add((binding.Origin, 0, 0, heal.HealAmount, 0));
                }
            return new MovementPreviewContext(input.BattleInstanceId, observer, actor, input.Kind, input.Path,
                input.Kind == CombatCommandKind.ReturnToCheckpoint ? actor.MovementCheckpointAnchor : null, environment,
                input.State, occupancy, input.Characters.Where(c => targets.Contains(c.EntityId)).OrderBy(c => c.EntityId).ToArray(),
                input.State.Structures.Where(s => targets.Contains(s.EntityId)).OrderBy(s => s.EntityId).ToArray(),
                input.State.ThornFields.Where(f => thornCells.Contains(f.Cell)).OrderBy(f => f.Cell.Y).ThenBy(f => f.Cell.X).ToArray(),
                input.Kind == CombatCommandKind.Move ? input.Souls.Entries.Where(s => arrivals.Values.Contains(s.Cell)).OrderBy(s => s.Id).ToArray() : Array.Empty<SoulFieldFacts>(),
                owned, input.SoulCounts.Where(p => owned.Any(o => o.Id == p.Key)).ToDictionary(p => p.Key, p => p.Value),
                collectors.Any(c => c.OwnershipRole == Contracts.Roster.CharacterOwnershipRole.Main) ? input.Main : null,
                definitions, artifactInputs, input.EnvironmentBindings,
                input.ObstacleContents.Where(p => targets.Contains(p.EntityId)).OrderBy(p => p.EntityId).ToArray());
        }

        public static bool Matches(MovementPreviewContext expected, CombatCommandInput input, Owner observer, IReadOnlyList<IMovementTileRule> rules)
        {
            if (expected == null || !expected.HasExpectedFacts || input == null || expected.Observer != observer ||
                expected.BattleInstanceId != input.BattleInstanceId || expected.ActorId != input.ActorId || expected.IntentKind != input.Kind ||
                !SamePath(expected.Path, input.Path)) return false;
            return SameValues(expected, Capture(input, observer, rules));
        }

        public static bool SameValues(MovementPreviewContext a, MovementPreviewContext b)
        {
            if (a == null || b == null) return a == b;
            return a.HasExpectedFacts && b.HasExpectedFacts && a.BattleInstanceId == b.BattleInstanceId && a.Observer == b.Observer &&
                a.ActorId == b.ActorId && a.IntentKind == b.IntentKind && SamePath(a.Path, b.Path) && a.ReturnCheckpoint == b.ReturnCheckpoint &&
                a.BoardWidth == b.BoardWidth && a.BoardHeight == b.BoardHeight && a.Phase == b.Phase && a.ActiveActorId == b.ActiveActorId &&
                a.IsNight == b.IsNight && a.StagePhase == b.StagePhase && a.CurrentTime == b.CurrentTime && a.CompletedUnitTurnCount == b.CompletedUnitTurnCount &&
                SameMover(a.Mover, b.Mover) && SameList(a.Occupancy, b.Occupancy, (x, y) => x.Id == y.Id && x.Owner == y.Owner && x.Cells.SequenceEqual(y.Cells)) &&
                SameList(a.EffectTargets, b.EffectTargets, SameTarget) &&
                SameList(a.EffectStructures, b.EffectStructures, (x, y) => x.EntityId == y.EntityId && x.Owner == y.Owner && x.Health.Equals(y.Health) &&
                    x.Anchor == y.Anchor && x.Rotation == y.Rotation && x.OccupiedCells.SequenceEqual(y.OccupiedCells)) &&
                a.VisibleEnvironment.SameValues(b.VisibleEnvironment) &&
                a.VisibleTerrainBindings.SameValues(b.VisibleTerrainBindings) &&
                ObstacleContentProfile.SameValues(a.ObstacleContents, b.ObstacleContents) &&
                SameList(a.Thorns, b.Thorns, (x, y) => x.Cell == y.Cell && x.ThornDamage == y.ThornDamage && x.RemainingUnitTurns == y.RemainingUnitTurns) &&
                SameList(a.Souls, b.Souls, (x, y) => x.Id == y.Id && x.Cell == y.Cell && x.Origin.Equals(y.Origin) && x.HealAmount == y.HealAmount && x.RemainingUnitTurns == y.RemainingUnitTurns) &&
                SameList(a.Collectors, b.Collectors, (x, y) => x.Id == y.Id && x.CharacterKey == y.CharacterKey && x.Role == y.Role) &&
                a.SoulCounts.Count == b.SoulCounts.Count && a.SoulCounts.All(p => b.SoulCounts.TryGetValue(p.Key, out int count) && count == p.Value) &&
                (a.Main == null || b.Main == null ? a.Main == b.Main : a.Main.CurrentHealth == b.Main.CurrentHealth &&
                    a.Main.PersistentMaxHealth == b.Main.PersistentMaxHealth && a.Main.PersistentAttack == b.Main.PersistentAttack) &&
                a.Definitions.SequenceEqual(b.Definitions) && a.ArtifactInputs.SequenceEqual(b.ArtifactInputs);
        }

        private static bool SameMover(CharacterCombatFacts a, CharacterCombatFacts b)
        {
            var x = a.Stats; var y = b.Stats;
            return SameTarget(a, b) && a.MovementCheckpointAnchor == b.MovementCheckpointAnchor && a.CommittedMoveDistance == b.CommittedMoveDistance &&
                a.PendingMoveDistance == b.PendingMoveDistance && a.PendingMoveActionCost == b.PendingMoveActionCost && a.AttackRefundEligibleMoveCost == b.AttackRefundEligibleMoveCost &&
                x.MoveMethod == y.MoveMethod && x.MoveRange == y.MoveRange && x.MoveCost == y.MoveCost && x.RemainingMoveRange == y.RemainingMoveRange &&
                x.CurrentActionCost == y.CurrentActionCost && x.MaxActionCost == y.MaxActionCost && x.Category.Key == y.Category.Key &&
                (x.Category.Key != CategoryKey.Heavy || x.Category.DefenseFacing == y.Category.DefenseFacing && x.Category.MovedThisTurn == y.Category.MovedThisTurn) &&
                SameList(x.TraitStatuses.ToArray().Where(t => t is FocusTraitStatusSnapshot || t is MoveAttackDiscountTraitStatusSnapshot).OrderBy(t => t.Key).ToArray(),
                    y.TraitStatuses.ToArray().Where(t => t is FocusTraitStatusSnapshot || t is MoveAttackDiscountTraitStatusSnapshot).OrderBy(t => t.Key).ToArray(), SameTrait);
        }

        private static bool SameTarget(CharacterCombatFacts a, CharacterCombatFacts b)
        {
            var x = a.Stats; var y = b.Stats;
            return a.EntityId == b.EntityId && a.OwnedId == b.OwnedId && a.CharacterKey == b.CharacterKey && a.Owner == b.Owner && a.OwnershipRole == b.OwnershipRole &&
                a.Area == b.Area && a.Anchor == b.Anchor && a.Rotation == b.Rotation && a.OccupiedCells.SequenceEqual(b.OccupiedCells) && a.Health.Equals(b.Health) &&
                a.TemporaryAttackBonus == b.TemporaryAttackBonus && a.TemporaryMaxHealthBonus == b.TemporaryMaxHealthBonus && a.PositionModifier.SameValues(b.PositionModifier) &&
                a.HeldEnvironment.SameValues(b.HeldEnvironment) &&
                x.PhysicalDefense == y.PhysicalDefense && x.AttackPower == y.AttackPower && x.ArtifactAttackBonus == y.ArtifactAttackBonus &&
                x.EffectiveAttackPower == y.EffectiveAttackPower && x.SoulAttackBonus == y.SoulAttackBonus && x.SoulMaxHealthBonus == y.SoulMaxHealthBonus && x.PositionAttackBonus == y.PositionAttackBonus;
        }

        private static bool SameTrait(TraitStatusSnapshot a, TraitStatusSnapshot b) => a switch
        {
            FocusTraitStatusSnapshot x when b is FocusTraitStatusSnapshot y => x.ActiveStacks == y.ActiveStacks && x.GainScheduled == y.GainScheduled && x.MovedThisTurn == y.MovedThisTurn,
            MoveAttackDiscountTraitStatusSnapshot x when b is MoveAttackDiscountTraitStatusSnapshot y => x.FirstMoveConsumed == y.FirstMoveConsumed && x.DiscountArmed == y.DiscountArmed,
            _ => false
        };
        private static bool SamePath(MovementPath a, MovementPath b) => a == null || b == null ? a == b : a.Cells.SequenceEqual(b.Cells);
        private static bool SameList<T>(IReadOnlyList<T> a, IReadOnlyList<T> b, Func<T, T, bool> compare) =>
            a.Count == b.Count && a.Select((x, i) => compare(x, b[i])).All(equal => equal);
    }
}
