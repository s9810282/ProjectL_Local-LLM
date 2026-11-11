using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
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
            if (input.Kind != CombatCommandKind.Move)
                throw new ArgumentException("경로 이동 입력이 필요합니다.", nameof(input));
            var profileReads = new CombatProfileReadSet();
            var projection = CombatMovementCalculator.ProjectRoute(input, rules, true, out _, out _, profileReads);
            if (projection == null) return null;
            input = projection.Input;
            var entries = new HashSet<CellCoord>();
            var objectCells = new HashSet<CellCoord>();
            var thornCells = new HashSet<CellCoord>();
            var arrivals = new Dictionary<EntityId, CellCoord>();
            var targets = new HashSet<EntityId>();
            var terrainCells = new HashSet<CellCoord>();
            entries.UnionWith(projection.PathResult().EnteredCells);
            bool tornadoActivated = projection.Events.Any(e => e is CombatTornadoEntryEvent || e is CombatTornadoEvent ||
                e is CombatCharacterEvent character && character.TornadoSourceId.HasValue);
            objectCells.UnionWith(entries);
            objectCells.UnionWith(projection.Events.OfType<CombatCharacterEvent>()
                .Where(e => (e.Kind == CombatEventKind.Moved || e.Kind == CombatEventKind.Relocated) &&
                    e.Final.Anchor.HasValue).Select(e => e.Final.Anchor.Value));
            thornCells.UnionWith(objectCells);
            foreach (var pair in projection.ArrivalCells) arrivals.Add(pair.Key, pair.Value);
            targets.UnionWith(projection.EffectTargetIds);
            foreach (var entityId in profileReads.Entries.Keys)
            {
                if (!input.Characters.Any(character => character.EntityId == entityId))
                {
                    throw new InvalidOperationException($"프로필을 읽은 개체 {entityId.Value}가 이동 입력에 없습니다.");
                }

                if (entityId != input.ActorId)
                {
                    targets.Add(entityId);
                }
            }
            terrainCells.UnionWith(projection.TerrainQueryCells);
            if (projection.Terminal)
                targets.Add(input.Characters.Single(c => c.OwnershipRole == Contracts.Roster.CharacterOwnershipRole.Main).EntityId);
            var visible = input.State.Environment.VisibleTo(observer);
            var pricedCells = input.Kind == CombatCommandKind.Move
                ? new HashSet<CellCoord>(input.Path.Cells) : new HashSet<CellCoord>();
            // 토네이도 후보 선정은 보드 전체의 지형을 읽는다. 일반 이동은 기존의 국소 의존 범위를 유지한다.
            var environment = new MovementEnvironmentFacts(visible.Tiles.Where(t =>
                tornadoActivated && input.EnvironmentBindings.TerrainSourceIds.Contains(t.Id) || (t is TraitTrailFact trail
                    ? trail.Key == TraitKey.GroundCompaction && t.Cell == actor.Anchor : t is DestinationTileModifierFact
                    ? arrivals.Values.Contains(t.Cell) : t is StaticTerrainFact terrain
                        ? terrainCells.Contains(t.Cell) || pricedCells.Contains(t.Cell) &&
                            (terrain.Kind == StaticTerrainKind.HighGround || terrain.Kind == StaticTerrainKind.WeakGround ||
                             terrain.Kind == StaticTerrainKind.Cliff)
                        : objectCells.Contains(t.Cell))).OrderBy(t => t.Id).ToArray());
            var occupancy = input.Characters.Where(c => c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead)
                .Select(c => (Id: c.EntityId, Owner: c.Owner, Cells: c.OccupiedCells))
                .Concat(input.State.Structures.Where(s => !s.Health.IsDead).Select(s => (Id: s.EntityId, Owner: s.Owner, Cells: s.OccupiedCells)))
                .OrderBy(c => c.Id).Select(c => (c.Id, c.Owner, (IReadOnlyList<CellCoord>)c.Cells.OrderBy(p => p.Y).ThenBy(p => p.X).ToArray())).ToArray();
            var collectors = input.Kind == CombatCommandKind.Move ? input.Characters.Where(c => arrivals.ContainsKey(c.EntityId) && c.OwnedId.HasValue).ToArray()
                : Array.Empty<CharacterCombatFacts>();
            var owned = input.Owned.Where(o => o.Id == actor.OwnedId || collectors.Any(c => c.OwnedId == o.Id)).OrderBy(o => o.Id.Value).ToArray();
            var definitions = profileReads.Entries.Values.Where(profile => !profile.OwnedId.HasValue)
                .GroupBy(profile => profile.Definition.Key).OrderBy(group => group.Key)
                .Select(group => SingleReadProfile(group).Definition).ToArray();
            var ownedProfiles = profileReads.Entries.Values.Where(profile => profile.OwnedId.HasValue)
                .GroupBy(profile => profile.OwnedId.Value).OrderBy(group => group.Key.Value)
                .Select(group => SingleReadProfile(group)).ToArray();
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
                environment,
                input.State, occupancy, input.Characters.Where(c => targets.Contains(c.EntityId)).OrderBy(c => c.EntityId).ToArray(),
                input.State.Structures.Where(s => targets.Contains(s.EntityId)).OrderBy(s => s.EntityId).ToArray(),
                input.State.ThornFields.Where(f => thornCells.Contains(f.Cell)).OrderBy(f => f.Cell.Y).ThenBy(f => f.Cell.X).ToArray(),
                input.Kind == CombatCommandKind.Move ? input.Souls.Entries.Where(s => arrivals.Values.Contains(s.Cell)).OrderBy(s => s.Id).ToArray() : Array.Empty<SoulFieldFacts>(),
                owned, input.SoulCounts.Where(p => owned.Any(o => o.Id == p.Key)).ToDictionary(p => p.Key, p => p.Value),
                collectors.Any(c => c.OwnershipRole == Contracts.Roster.CharacterOwnershipRole.Main) ? input.Main : null,
                definitions, artifactInputs, input.EnvironmentBindings,
                input.ObstacleContents.Where(p => targets.Contains(p.EntityId)).OrderBy(p => p.EntityId).ToArray(), ownedProfiles,
                tornadoActivated ? input.State.EnvironmentProgress.RandomState : (uint?)null,
                Domain.Movement.MovementRouteValidator.SwapPassenger(input, input.Path.Endpoint), input.Battle?.GimmickDefinitions);
        }

        /// <summary>같은 정의 키/보유 식별자의 읽기는 같은 선택 원본이어야 한다.</summary>
        private static CombatAttackProfile SingleReadProfile(IEnumerable<CombatAttackProfile> profiles)
        {
            var selected = profiles.First();
            if (profiles.Any(profile => !ReferenceEquals(profile, selected)))
            {
                throw new InvalidOperationException($"이동 투영에서 정의 {selected.Definition.Key}/보유 {selected.OwnedId}의 프로필 선택이 섞였습니다.");
            }

            return selected;
        }

        public static bool Matches(MovementPreviewContext expected, CombatCommandInput input, Owner observer, IReadOnlyList<IMovementTileRule> rules)
        {
            if (expected == null || !expected.HasExpectedFacts || input == null || expected.Observer != observer ||
                expected.BattleInstanceId != input.BattleInstanceId || expected.ActorId != input.ActorId || expected.IntentKind != input.Kind ||
                !MovementPathNormalizer.MatchesDraft(input.Path, expected.Path)) return false;
            if (expected.PositionSwapPassenger != null &&
                Domain.Movement.MovementRouteValidator.SwapPassenger(input, expected.Path.Endpoint)?.EntityId != expected.PositionSwapPassenger.EntityId)
                return false;
            var selected = input.WithIntent(input.Kind, input.Aim, expected.Path.Endpoint, expected.Path, null);
            return SameValues(expected, Capture(selected, observer, rules));
        }

        public static bool SameValues(MovementPreviewContext a, MovementPreviewContext b)
        {
            if (a == null || b == null) return a == b;
            return a.HasExpectedFacts && b.HasExpectedFacts && a.BattleInstanceId == b.BattleInstanceId && a.Observer == b.Observer &&
                a.ActorId == b.ActorId && a.IntentKind == b.IntentKind && SamePath(a.Path, b.Path) &&
                a.BoardWidth == b.BoardWidth && a.BoardHeight == b.BoardHeight && a.Phase == b.Phase && a.ControllerPhase == b.ControllerPhase && a.ControllerTurnSequence == b.ControllerTurnSequence &&
                a.StagePhase == b.StagePhase && a.CompletedPlayerTurnCount == b.CompletedPlayerTurnCount && a.ActionUse == b.ActionUse &&
                SameMover(a.Mover, b.Mover) &&
                (a.PositionSwapPassenger == null ? b.PositionSwapPassenger == null : a.PositionSwapPassenger.SameValues(b.PositionSwapPassenger)) &&
                SameList(a.Occupancy, b.Occupancy, (x, y) => x.Id == y.Id && x.Owner == y.Owner && x.Cells.SequenceEqual(y.Cells)) &&
                SameList(a.EffectTargets, b.EffectTargets, SameTarget) &&
                SameList(a.EffectStructures, b.EffectStructures, (x, y) => x.EntityId == y.EntityId && x.Owner == y.Owner && x.Health.Equals(y.Health) &&
                    x.Anchor == y.Anchor && x.Rotation == y.Rotation && x.OccupiedCells.SequenceEqual(y.OccupiedCells)) &&
                a.VisibleEnvironment.SameValues(b.VisibleEnvironment) &&
                a.EnvironmentRandomState == b.EnvironmentRandomState &&
                SameList(a.VisibleTornadoAreas, b.VisibleTornadoAreas, (x, y) => x.SameValues(y)) &&
                a.VisibleTerrainBindings.SameValues(b.VisibleTerrainBindings) &&
                ObstacleContentProfile.SameValues(a.ObstacleContents, b.ObstacleContents) &&
                SameList(a.Thorns, b.Thorns, (x, y) => x.Cell == y.Cell && x.ThornDamage == y.ThornDamage && x.RemainingUnitTurns == y.RemainingUnitTurns && x.DefenseReductionPercent == y.DefenseReductionPercent && x.TickPhase == y.TickPhase) &&
                SameList(a.Souls, b.Souls, (x, y) => x.Id == y.Id && x.Cell == y.Cell && x.Origin.Equals(y.Origin) && x.HealAmount == y.HealAmount && x.RemainingUnitTurns == y.RemainingUnitTurns) &&
                SameList(a.Collectors, b.Collectors, (x, y) => x.SameValues(y)) &&
                a.SoulCounts.Count == b.SoulCounts.Count && a.SoulCounts.All(p => b.SoulCounts.TryGetValue(p.Key, out int count) && count == p.Value) &&
                // 부분 비교(의도 미확인): 메인 지속 값 중 셋만 본다. SameValues로 바꾸면 동작이 바뀐다. 설계 Q-B2.
                (a.Main == null || b.Main == null ? a.Main == b.Main : a.Main.CurrentHealth == b.Main.CurrentHealth &&
                    a.Main.PersistentMaxHealth == b.Main.PersistentMaxHealth && a.Main.PersistentAttack == b.Main.PersistentAttack) &&
                SameList(a.Definitions, b.Definitions, MovementDefinitionComparison.SameDefinition) &&
                SameList(a.OwnedProfiles, b.OwnedProfiles, MovementDefinitionComparison.SameProfile) &&
                a.ArtifactInputs.SequenceEqual(b.ArtifactInputs);
        }

        private static bool SameMover(CharacterCombatFacts a, CharacterCombatFacts b)
        {
            var x = a.Stats; var y = b.Stats;
            return SameTarget(a, b) && a.ActionUse == b.ActionUse &&
                x.MoveMethod == y.MoveMethod && x.MoveRange == y.MoveRange && x.Category.Key == y.Category.Key &&
                SameList(a.StatusEffects.Entries.OfType<MovementRangeDecreaseStatus>().ToArray(),
                    b.StatusEffects.Entries.OfType<MovementRangeDecreaseStatus>().ToArray(), (left, right) => left.SameValues(right)) &&
                SameList(x.TraitStatuses.ToArray().Where(t => t is FocusTraitStatusSnapshot).OrderBy(t => t.Key).ToArray(),
                    y.TraitStatuses.ToArray().Where(t => t is FocusTraitStatusSnapshot).OrderBy(t => t.Key).ToArray(), SameTrait);
        }

        private static bool SameTarget(CharacterCombatFacts a, CharacterCombatFacts b)
        {
            var x = a.Stats; var y = b.Stats;
            return a.EntityId == b.EntityId && a.OwnedId == b.OwnedId && a.CharacterKey == b.CharacterKey && a.Owner == b.Owner && a.OwnershipRole == b.OwnershipRole &&
                a.Area == b.Area && a.Anchor == b.Anchor && a.Rotation == b.Rotation && a.OccupiedCells.SequenceEqual(b.OccupiedCells) && a.Health.Equals(b.Health) &&
                a.TemporaryAttackBonus == b.TemporaryAttackBonus && a.TemporaryMaxHealthBonus == b.TemporaryMaxHealthBonus && a.PositionModifier.SameValues(b.PositionModifier) &&
                a.HeldEnvironment.SameValues(b.HeldEnvironment) && a.TraitEffects.SameValues(b.TraitEffects) &&
                SameList(x.TraitStatuses.ToArray().OfType<RampageTraitStatusSnapshot>().ToArray(),
                    y.TraitStatuses.ToArray().OfType<RampageTraitStatusSnapshot>().ToArray(), (left, right) => left.SameValues(right)) &&
                // 이동 피해가 읽는 특성별 최대 증가율의 합계만 비교하며, 출처·잔여 시간 변경은 별도 게시 경계가 검증합니다.
                WeaknessExposureRules.EffectivePercent(a.StatusEffects) == WeaknessExposureRules.EffectivePercent(b.StatusEffects) &&
                (a.Debuffs.DefenseReduction == null ? b.Debuffs.DefenseReduction == null : a.Debuffs.DefenseReduction.SameValues(b.Debuffs.DefenseReduction)) &&
                x.SpecializationCategory == y.SpecializationCategory &&
                x.PhysicalDefense == y.PhysicalDefense && x.AttackPower == y.AttackPower && x.ArtifactAttackBonus == y.ArtifactAttackBonus &&
                x.TraitAttackBonus == y.TraitAttackBonus && x.EffectiveAttackPower == y.EffectiveAttackPower && x.SoulAttackBonus == y.SoulAttackBonus && x.SoulMaxHealthBonus == y.SoulMaxHealthBonus && x.PositionAttackBonus == y.PositionAttackBonus;
        }

        private static bool SameTrait(TraitStatusSnapshot a, TraitStatusSnapshot b) => a switch
        {
            FocusTraitStatusSnapshot x when b is FocusTraitStatusSnapshot y => x.ActiveStacks == y.ActiveStacks && x.GainScheduled == y.GainScheduled && x.MovedThisTurn == y.MovedThisTurn,
            _ => false
        };
        private static bool SamePath(MovementPath a, MovementPath b) => a == null || b == null ? a == b : a.Cells.SequenceEqual(b.Cells);
        private static bool SameList<T>(IReadOnlyList<T> a, IReadOnlyList<T> b, Func<T, T, bool> compare) =>
            a.Count == b.Count && a.Select((x, i) => compare(x, b[i])).All(equal => equal);
    }
}
