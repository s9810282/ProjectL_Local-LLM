using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    internal static class EnemyAiBenchmarkFixtures
    {
        internal static readonly string[] Ids = { "N5", "N6", "N8", "W8", "U8", "A8", "T8", "R8", "X8", "H8", "G16" };
        internal static readonly int[] Seeds = { 1, 7, 19, 41, 97 };
        internal static readonly IMovementTileRule[] Rules = { new ThornMovementTileRule(), new MineMovementTileRule() };

        internal static int EnemyCount(string fixtureId) => fixtureId switch
        {
            "N5" => 5,
            "N6" => 6,
            "G16" => 16,
            _ => 8
        };

        internal static CombatCommandInput Build(string fixtureId, int seed)
        {
            if (!Ids.Contains(fixtureId)) throw new ArgumentException(nameof(fixtureId));
            if (!Seeds.Contains(seed)) throw new ArgumentOutOfRangeException(nameof(seed));

            bool wall = fixtureId is "W8" or "U8";
            bool rail = fixtureId is "R8" or "X8";
            int size = wall || fixtureId == "G16" ? 32 : 16;
            var template = CombatCommandTestData.Input(empty: true);
            var playerCells = wall
                ? new[] { new CellCoord(28, 2), new CellCoord(28, 3), new CellCoord(29, 2), new CellCoord(29, 3) }
                : new[] { new CellCoord(0, 0), new CellCoord(0, 1), new CellCoord(2, 0), new CellCoord(4, 0) };
            var keys = new[] { EntityKey.Unit_MainCharacter, EntityKey.Unit_Cowboy, EntityKey.Unit_DynamiteHammer, EntityKey.Unit_Sniper };
            var players = keys.Select((key, index) => CombatCommandTestData.Character(
                index + 1, key, playerCells[index], Owner.Player, 10, main: index == 0));
            var enemies = Enumerable.Range(0, EnemyCount(fixtureId)).Select(index =>
            {
                var key = fixtureId == "A8" ? (index % 4) switch
                {
                    0 => EntityKey.Unit_Demon,
                    1 => EntityKey.Unit_ShotGun,
                    2 => EntityKey.Unit_Akimbo,
                    _ => EntityKey.Unit_Gravekeeper
                } : fixtureId == "X8" && index == 0 ? EntityKey.Unit_Gravekeeper : EntityKey.Unit_Demon;
                var cell = wall ? new CellCoord(2 + index % 3, 2 + index / 3)
                    : new CellCoord(5 + index % 4, 1 + index / 4);
                return CombatCommandTestData.With(CombatCommandTestData.Character(11 + index, key, cell, Owner.Enemy, 10),
                    appointRange: wall ? 1 : 4);
            });
            var characters = players.Concat(enemies).ToArray();
            var profiles = characters.Select(c => c.CharacterKey).Distinct().Select(key =>
            {
                var method = fixtureId == "A8" ? key switch
                {
                    EntityKey.Unit_Demon => new TargetingMethodSpec(TargetingShape.EightDirectionLine, TargetingChoiceMode.Single, TargetingSplash.None, 0),
                    EntityKey.Unit_ShotGun => new TargetingMethodSpec(TargetingShape.Fan, TargetingChoiceMode.Direction, TargetingSplash.None, 0),
                    EntityKey.Unit_Akimbo => new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.All, TargetingSplash.None, 0),
                    EntityKey.Unit_Gravekeeper => new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.Radial, 50),
                    _ => Single()
                } : Single();
                var traits = fixtureId == "A8" && key == EntityKey.Unit_Demon
                    ? new TraitSpec[] { new PiercingFalloffTraitSpec(30, 3) } : Array.Empty<TraitSpec>();
                var definition = CombatCommandTestData.Spec(key, 10, CategoryKey.Main, traits);
                if (fixtureId == "X8" && key == EntityKey.Unit_Gravekeeper)
                    definition = new UnitSpec(key, definition.Role, 1, 1, new[] { CellCoord.Zero }, 100,
                        definition.MoveMethod, definition.AttackRange, definition.CategoryKey, definition.AttackPower,
                        definition.PhysicalDefense, definition.Priority, traits, definition.OwnerEligibility,
                        behavior: new UnitBehaviorSpec(true, true, true));
                return new CombatAttackProfile(definition, method);
            }).ToArray();

            if (fixtureId == "A8")
            {
                var piercing = PiercingShotTestData.Input(cap: 3).Characters[0].Stats.TraitStatuses.ToArray();
                characters = characters.Select(c => c.CharacterKey == EntityKey.Unit_Demon
                    ? c.With(stats: c.Stats.With(traitStatuses: piercing)) : c).ToArray();
            }
            if (fixtureId == "T8")
            {
                var origin = new EffectOriginKey(EffectOwnerKind.Trait, 2, (int)TraitKey.AnkleBreaker, "fixture-bind");
                var restriction = new StatusEffectSet(new AppliedStatus[] { new ActionRestrictionStatus(StatusEffectKey.Bind, origin, 2) });
                characters = characters.Select(c => c.EntityId.Value == 12 ? c.WithStatusEffects(restriction) : c).ToArray();
            }

            var tiles = new List<MovementTileFact>();
            if (wall)
                tiles.AddRange(Enumerable.Range(0, fixtureId == "U8" ? size : size - 3)
                    .Select(y => WallStateLifecycleTests.Wall(100 + y, new CellCoord(8, y))));
            if (fixtureId == "H8")
            {
                tiles.Add(MineEnvironmentTests.Mine(100, 1, hidden: true, cell: new CellCoord(5, 1)));
                tiles.Add(MineEnvironmentTests.Mine(101, 2, hidden: false, cell: new CellCoord(4, 2)));
            }

            var definitions = new List<StageGimmickSpec>();
            var rails = rail ? new[] { new CellCoord(4, 0), new CellCoord(5, 1), new CellCoord(6, 2) } : Array.Empty<CellCoord>();
            if (rail)
            {
                definitions.Add(new RailwayGimmickSpec(2, 5));
                definitions.Add(new RockfallGimmickSpec());
            }
            var tornadoCenter = new CellCoord(5, 1);
            var tornadoDirection = new CellCoord(1, 1);
            if (fixtureId == "X8")
                definitions.Add(new TornadoGimmickSpec(new[] { new TornadoMemberSpec(200,
                    new[] { Owner.Player, Owner.Enemy }, tornadoCenter, 1, firstActivationAfterControllerTurns: 2) }));
            if (fixtureId == "H8")
                definitions.Add(new HaboobGimmickSpec(initialAnchor: new CellCoord(4, 1)));

            var environment = new MovementEnvironmentFacts(tiles);
            var state = new CombatCommandState(size, size, CombatFlowPhase.Automatic,
                Array.Empty<StructureCombatFacts>(), Array.Empty<ThornFieldCellSnapshot>(),
                new CombatTurnFacts(StagePhase.Combat, controllerPhase: CombatControllerPhase.Enemy,
                    controllerTurnSequence: 2, completedPlayerTurnCount: 1, completedControllerTurnCount: 1),
                environment, EnvironmentProgressFacts.Initial(environment, (uint)seed),
                itemRandomState: (uint)seed);
            state = StageGimmickComposition.Initialize(state, definitions);
            if (rail)
                state = state.With(gimmicks: state.Gimmicks.Replace(new RockfallGimmickState(new StageGimmickId(2),
                    RockfallGimmickPhase.Warning, 2, new[] { new CellCoord(5, 1), new CellCoord(6, 2) })));
            if (fixtureId == "X8")
                state = state.With(gimmicks: state.Gimmicks.Replace(new TornadoGimmickState(new StageGimmickId(3),
                    new[] { new TornadoMemberState(200, tornadoCenter, 1, TornadoGimmickPhase.Warning, 2, tornadoDirection) })));
            state.Gimmicks.ValidateDefinitions(definitions, size, size);

            var owned = characters.Where(c => c.OwnedId.HasValue)
                .Select(c => new OwnedCharacterRecord(c.OwnedId.Value, c.CharacterKey, c.OwnershipRole.Value)).ToArray();
            var input = new CombatCommandInput(0, CombatCommandKind.CompleteEnemyTurn, EntityId.Empty, default,
                state, characters, profiles, template.Main, owned, owned.ToDictionary(o => o.Id, _ => 0),
                template.Souls, template.Artifacts,
                battle: new CombatBattleFacts(8, seed, 2, 2, Array.Empty<CharacterInstanceId>(), 0, rails, definitions),
                processingKind: CombatProcessingKind.Advance, battleInstanceId: 1,
                environmentBindings: new EnvironmentSemanticBindings(tiles.OfType<StaticTerrainFact>().Select(t => t.Id).ToArray(),
                    Array.Empty<long>(), Array.Empty<EntityId>()),
                expectedBattleInstanceId: 1, expectedControllerTurnSequence: 2);
            if (fixtureId == "T8")
            {
                input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Cowboy, new TauntTraitSpec(8, 10));
                input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Sniper, new TauntTraitSpec(5, 15));
            }
            return input;
        }

        private static TargetingMethodSpec Single() =>
            new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0);

        internal static string Cells(IEnumerable<CellCoord> cells) => string.Join(";", cells.Select(Cell));
        internal static string Cell(CellCoord cell) => $"{cell.X}:{cell.Y}";

        internal static string Describe(CombatCommandInput input, bool includeRandomState)
        {
            var state = input.State;
            var turn = state.Turn;
            var lines = new List<string>
            {
                // 'night=' 표기는 동결 추적(baseline-traces.tsv)의 해시를 지키려고 남긴다. 값은 옛 밤 판정과 같은 InCombat이다.
                $"board={state.BoardWidth}:{state.BoardHeight}|{state.Phase}|night={state.InCombat}|wallCut={state.WallBlocking.CornerCutTicks}",
                $"turn={turn.StagePhase}|{turn.ControllerPhase}|{turn.ControllerTurnSequence}|{turn.CompletedPlayerTurnCount}|{turn.CompletedControllerTurnCount}",
                $"rail={Cells(input.Battle.RailCells)}"
            };
            if (includeRandomState)
                lines.Add($"seed={input.Battle.TimelineSeed}|environmentRng={state.EnvironmentProgress.RandomState}|itemRng={state.ItemRandomState}");
            lines.AddRange(input.Characters.Select(c => $"unit={c.EntityId.Value}|{c.CharacterKey}|{c.Owner}|{c.OwnedId?.Value}|{c.OwnershipRole}|{c.Area}|{c.ActionUse}|" +
                $"{Cell(c.Anchor.Value)}|{c.Rotation}|{Cells(c.OccupiedCells)}|hp={c.Health.CurrentHealth}:{c.Health.MaxHealth}|" +
                $"stats={c.Stats.MoveMethod}:{c.Stats.MoveRange}:{c.Stats.AppointRange}:{c.Stats.AttackPower}:{c.Stats.PhysicalDefense}:{c.Stats.Priority}:{c.Stats.Category.Key}|" +
                $"traits={string.Join(",", c.Stats.TraitStatuses.ToArray().Select(t => t.Key))}|" +
                $"restrictions={string.Join(",", c.StatusEffects.Entries.OfType<ActionRestrictionStatus>().Select(s => $"{s.Key}:{s.Origin}"))}"));
            lines.AddRange(input.Profiles.Select(p => $"profile={p.Definition.Key}|{p.Method.Shape}|{p.Method.ChoiceMode}|{p.Method.Splash}|{p.Method.SplashPercent}|{p.Method.ChoiceAngle}|" +
                $"definition={p.Definition.Role}:{p.Definition.OwnerEligibility}:{p.Definition.CategoryKey}:{p.Definition.MoveMethod}:{p.Definition.AttackRange}:" +
                $"{p.Definition.AttackPower}:{p.Definition.PhysicalDefense}:{p.Definition.MaxHealth}:{p.Definition.Priority}:{Cells(p.Definition.FootprintLocalCells.ToArray())}|" +
                $"behavior={p.Definition.Behavior.CanMoveVoluntarily}:{p.Definition.Behavior.CanBasicAttack}:{p.Definition.Behavior.IsForcedMovementImmune}|" +
                $"traits={string.Join(",", p.Definition.Traits.ToArray().Select(DescribeTrait))}"));
            lines.AddRange(state.Environment.Tiles.Select(t => $"tile={t.Id}|{Cell(t.Cell)}|{t.RuleKey}|{t.Priority}|{t.Active}|{t.IsMine}|{string.Join(",", t.KnownTo)}" +
                (t is MineSourceFact mine ? $"|damage={mine.Damage}|ordinal={mine.EntryOrdinal}" : "")));
            lines.AddRange(input.Battle.GimmickDefinitions.Select(DescribeGimmick));
            lines.AddRange(state.Gimmicks.Entries.Select(g => g switch
            {
                RockfallGimmickState r => $"rockfallState={r.Id.Value}|{r.Phase}|{r.NextBoundaryCount}|{Cells(r.WarnedCells)}",
                HaboobGimmickState h => $"haboobState={h.Id.Value}|{h.Phase}|{h.NextBoundaryCount}|{Cells(h.Cells)}",
                TornadoGimmickState t => $"tornadoState={t.Id.Value}|" + string.Join(";", t.Members.Select(m =>
                    $"{m.SourceId}|{Cell(m.Center)}|{m.HorizontalSign}|{m.Phase}|{m.NextBoundaryCount}|{OptionalCell(m.LockedDirection)}")),
                _ => throw new InvalidOperationException(g.Key.ToString())
            }));
            return string.Join("\n", lines);
        }

        private static string OptionalCell(CellCoord? cell) => cell.HasValue ? Cell(cell.Value) : "";

        private static string DescribeTrait(TraitSpec trait) => trait switch
        {
            TauntTraitSpec t => $"Taunt:{t.RadiusCells}:{t.DamageReductionPercent}",
            PiercingFalloffTraitSpec p => $"PiercingFalloff:{p.ReductionPercentPerTarget}:{p.MaxTargetsPerRay}",
            _ => throw new InvalidOperationException(trait.TraitKey.ToString())
        };

        private static string DescribeGimmick(StageGimmickSpec gimmick) => gimmick switch
        {
            RailwayGimmickSpec r => $"railway={r.FirstActivationAfterControllerTurns}|{r.RepeatIntervalControllerTurns}",
            RockfallGimmickSpec r => $"rockfall={r.CooldownControllerTurns}|{r.WarningControllerTurns}|{r.MinimumRockCount}|{r.MaximumRockCount}|" +
                $"{r.MaximumHealthLossPercent}|{r.BindAffectedSideTurns}|{Cells(r.FootprintOffsets)}",
            HaboobGimmickSpec h => $"haboob={h.RelocationIntervalControllerTurns}|{h.WarningLeadControllerTurns}|{OptionalCell(h.InitialAnchor)}|{Cells(h.FootprintOffsets)}",
            TornadoGimmickSpec t => "tornado=" + string.Join(";", t.Members.Select(m => $"{m.SourceId}|{string.Join(",", m.KnownTo)}|{OptionalCell(m.InitialCenter)}|" +
                $"{m.InitialHorizontalSign}|{m.SideLength}|{m.DiagonalDistance}|{m.FirstActivationAfterControllerTurns}|{m.RepeatIntervalControllerTurns}|{m.WarningLeadControllerTurns}")),
            _ => throw new InvalidOperationException(gimmick.GimmickKey.ToString())
        };

        internal static string Hash(string value)
        {
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "").ToLowerInvariant();
        }
    }
}
