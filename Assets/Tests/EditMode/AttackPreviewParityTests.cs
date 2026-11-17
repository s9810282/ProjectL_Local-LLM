using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class AttackPreviewParityTests
    {
        private const string ReferenceDirectory = "docs/superpowers/reports/2026-11-12-combat-health-execution-evidence";
        internal const string ReferenceRelativePath = ReferenceDirectory + "/phase-2-preview-reference.json";

        [Test]
        public void FrozenPreviewAndActualSemanticsMatch() =>
            CheckVectors(Vectors(), ReferenceRelativePath, "preview-reference");

        internal const string MovementReferencePath = ReferenceDirectory + "/phase-2-movement-reference.json";

        [Test]
        public void FrozenMovementContinuationSemanticsMatch() =>
            CheckVectors(MovementVectors(), MovementReferencePath, "movement-reference");

        private static void CheckVectors(IEnumerable<Vector> vectors, string relativePath, string captureMode)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
            bool capture = Environment.GetEnvironmentVariable("PROJECTL_ENEMY_AI_CAPTURE") == captureMode;
            var reference = capture ? new Dictionary<string, string>() : ReadReference(path);
            if (capture) Assert.That(File.Exists(path), Is.False, "기존 동결 결과는 덮어쓰지 않습니다.");
            var rows = new List<string>();
            var seen = new HashSet<string>();
            foreach (var vector in vectors)
            {
                TestContext.Out.WriteLine(vector.Id);
                Assert.That(seen.Add(vector.Id), Is.True);
                var types = new HashSet<string>();
                string before = Serialize(vector.Input, types);
                var preview = CombatCommandCalculator.Preview(vector.Input, vector.Rules, out var projection);
                var actual = CombatCommandCalculator.Calculate(vector.Input, vector.Rules);
                if (vector.Id is "terminal-hq" or "terminal-main")
                    Assert.That(actual.Outcome.IsStageEnded, Is.True, vector.Id);
                if (vector.Id == "hidden-attack-rejection")
                {
                    Assert.That(preview.CanAttack, Is.True);
                    Assert.That(actual.Accepted, Is.False);
                }
                string semantic = Serialize(new object[] { before, preview, projection, actual }, types);
                Assert.That(Serialize(vector.Input, new HashSet<string>()), Is.EqualTo(before), vector.Id + " 원본 변경");
                string encoded = Encode(semantic);
                string encodedTypes = Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Join("\n", types.OrderBy(t => t, StringComparer.Ordinal))));
                if (capture)
                    rows.Add("{\"id\":\"" + vector.Id + "\",\"semanticGzipBase64\":\"" + encoded + "\",\"typesUtf8Base64\":\"" + encodedTypes + "\"}");
                else
                {
                    Assert.That(reference.ContainsKey(vector.Id), Is.True, vector.Id + " 기준 없음");
                    var old = reference[vector.Id].Split('|');
                    var allowed = new HashSet<string>(Encoding.UTF8.GetString(Convert.FromBase64String(old[1])).Split('\n'));
                    Assert.That(types.Except(allowed).ToArray(), Is.Empty, vector.Id + " 미등록 사실/사건 형식");
                    Assert.That(semantic, Is.EqualTo(Decode(old[0])), vector.Id);
                }
            }
            if (capture)
            {
                File.WriteAllText(path, "{\"schemaVersion\":2,\"records\":[\n" + string.Join(",\n", rows) + "\n]}\n", new UTF8Encoding(false));
                TestContext.Out.WriteLine("Captured vectors=" + rows.Count);
            }
            else Assert.That(reference.Count, Is.EqualTo(seen.Count));
        }

        internal static Dictionary<string, string> ReadReference(string path = null)
        {
            path ??= Path.Combine(Directory.GetCurrentDirectory(), ReferenceRelativePath);
            Assert.That(File.Exists(path), Is.True, "동결 기준을 먼저 만들어야 합니다.");
            var result = new Dictionary<string, string>();
            foreach (Match match in Regex.Matches(File.ReadAllText(path),
                "\\{\"id\":\"([^\"]+)\",\"semanticGzipBase64\":\"([^\"]+)\",\"typesUtf8Base64\":\"([^\"]+)\"\\}"))
                result.Add(match.Groups[1].Value, match.Groups[2].Value + "|" + match.Groups[3].Value);
            Assert.That(result.Count, Is.GreaterThan(0));
            return result;
        }

        private static string Encode(string text)
        {
            using var bytes = new MemoryStream();
            using (var gzip = new GZipStream(bytes, CompressionLevel.Optimal, true))
            {
                var data = Encoding.UTF8.GetBytes(text);
                gzip.Write(data, 0, data.Length);
            }
            return Convert.ToBase64String(bytes.ToArray());
        }

        internal static string Decode(string value)
        {
            using var bytes = new MemoryStream(Convert.FromBase64String(value));
            using var gzip = new GZipStream(bytes, CompressionMode.Decompress);
            using var reader = new StreamReader(gzip, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        internal sealed class Vector
        {
            internal string Id { get; }
            internal CombatCommandInput Input { get; }
            internal IReadOnlyList<IMovementTileRule> Rules { get; }
            internal Vector(string id, CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules = null)
            { Id = id; Input = input; Rules = rules ?? EnemyAiBenchmarkFixtures.Rules; }
        }

        internal static IEnumerable<Vector> MovementVectors()
        {
            var source = CliffFallTests.Knockback();
            StaticTerrainFact Ground(long id, CellCoord cell, int count = 0) =>
                new StaticTerrainFact(id, cell, StaticTerrainKind.WeakGround, true,
                    new[] { Owner.Player, Owner.Enemy }, 1, count);
            yield return new Vector("weak-knockback", CliffFallTests.WithEnvironment(source, Ground(71, new CellCoord(5, 3))));
            yield return new Vector("weak-two", CliffFallTests.WithEnvironment(source,
                Ground(71, new CellCoord(5, 3)), Ground(72, new CellCoord(5, 5))));
            var main = source.Characters.Single(c => c.OwnershipRole == CharacterOwnershipRole.Main);
            yield return new Vector("weak-main", CliffFallTests.WithEnvironment(source, Ground(71, main.Anchor.Value, 1)));
            var pull = PullTestData.Input(extra: 4, distance: 5);
            yield return new Vector("weak-pull", CliffFallTests.WithEnvironment(pull, Ground(71, new CellCoord(9, 7))));
        }

        internal static IEnumerable<Vector> Vectors()
        {
            foreach (string fixture in EnemyAiBenchmarkFixtures.Ids)
                foreach (int seed in EnemyAiBenchmarkFixtures.Seeds)
                {
                    var input = EnemyAiBenchmarkFixtures.Build(fixture, seed).WithActor(new EntityId(11));
                    input = input.WithIntent(CombatCommandKind.Attack, AttackAim.ForCell(new CellCoord(2, 0)), default, null, null);
                    yield return new Vector(fixture + "-" + seed, input);
                }
            yield return new Vector("single", CombatCommandTestData.Input(new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0)));
            yield return new Vector("splash", CombatCommandTestData.Input());
            yield return new Vector("friendly", CombatCommandTestData.Input(friendly: true));
            yield return new Vector("empty", CombatCommandTestData.Input(empty: true));
            yield return new Vector("out-of-board", CombatCommandTestData.Input(aim: AttackAim.ForCell(new CellCoord(99, 99))));
            yield return new Vector("direction", CombatTraitTestFixtures.FanAttackInput());
            yield return new Vector("all", CombatCommandTestData.Input(new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.All, TargetingSplash.None, 0), AttackAim.ForAll()));
            yield return new Vector("pull", PullTestData.Input());
            yield return new Vector("pull-long", PullTestData.Input(extra: 4, distance: 5));
            yield return new Vector("pull-friendly", PullTestData.Input(owner: Owner.Player));
            var blockedPull = PullTestData.Input(extra: 4, distance: 5);
            var blocker = CombatCommandTestData.Character(70, EntityKey.Unit_Demon, new CellCoord(10, 7), Owner.Enemy);
            yield return new Vector("pull-interrupted", CombatCommandTestData.With(blockedPull, blockedPull.Characters.Append(blocker).ToArray()));
            yield return new Vector("impact", ImpactBoltTests.Input(distance: 2));
            yield return new Vector("impact-empty-center", ImpactBoltTests.Input(distance: 2, centerTarget: false));
            yield return new Vector("reflection", ThornHeartTestData.Input());
            yield return new Vector("reflection-launched-pierce", ThornHeartTestData.Input(piercing: true, hp: 1));
            yield return new Vector("sacrifice-ordered", SacrificeTraitTests.Input(splash: true));
            yield return new Vector("hunter-reward", HunterTraitTests.Input(splash: true));
            yield return new Vector("knockback", CombatCommandEffectTests.ActorEffect(new KnockbackTraitSpec(2), new KnockbackTraitStatusSnapshot(2)));
            yield return new Vector("stationary-before", CombatCommandEffectTests.ActorEffect(new StationaryFireTraitSpec(30), new StationaryFireTraitStatusSnapshot(false)));
            yield return new Vector("stationary-after", CombatCommandEffectTests.ActorEffect(new StationaryFireTraitSpec(30), new StationaryFireTraitStatusSnapshot(true)));
            yield return new Vector("barrel-cascade", ObstacleReactionTests.Barrels(
                ObstacleReactionTests.Barrel(51, new CellCoord(4, 3), 1, 30),
                ObstacleReactionTests.Barrel(52, new CellCoord(5, 3), 1, 20)));
            var terminal = CombatTraitTestFixtures.WithRewards(CombatCommandTestData.Input());
            terminal = CombatCommandTestData.With(terminal, terminal.Characters.Select(c => c.Owner == Owner.Enemy
                ? c.WithHealth(new HealthStateSnapshot(1, 100)) : c).ToArray());
            yield return new Vector("terminal-rewards", terminal);
            var hidden = CombatCommandEffectTests.ActorEffect(new KnockbackTraitSpec(1), new KnockbackTraitStatusSnapshot(1));
            var environment = new MovementEnvironmentFacts(new MovementTileFact[] {
                new DestinationTileModifierFact(71, new CellCoord(5, 3), true, new[] { Owner.Enemy }, 0, -20) });
            yield return new Vector("hidden-arrival", CombatCommandTestData.With(hidden, state: hidden.State.With(environment: environment,
                environmentProgress: EnvironmentProgressFacts.Initial(environment))));
            var acceleration = CombatCommandEffectTests.ActorEffect(new AccelerationTraitSpec(2), new AccelerationTraitStatusSnapshot());
            acceleration = CombatCommandTestData.With(acceleration, acceleration.Characters.Select(c => c.EntityId == acceleration.ActorId
                ? c.With(stats: c.Stats.With(traitAttackBonus: 6, traitStatuses: new TraitStatusSnapshot[] { new AccelerationTraitStatusSnapshot(6) })) : c).ToArray());
            yield return new Vector("acceleration-scope", acceleration);
            yield return new Vector("cohesion-shield", TraitCombatFixtures.Equip(CombatCommandTestData.Input(), EntityKey.Unit_Demon,
                new CohesionTraitSpec(2, 10, 2, 2)));
            var storm = CombatCommandEffectTests.ActorEffect(new KnockbackTraitSpec(3), new KnockbackTraitStatusSnapshot(3));
            storm = TraitCombatFixtures.Characters(storm, storm.Characters.Where(c => c.EntityId.Value is 1 or 2 or 10).ToArray());
            storm = TraitTrailTestData.Environment(storm, TornadoEnvironmentTests.Source(cell: new CellCoord(5, 3)));
            yield return new Vector("knockback-tornado", storm, TornadoEnvironmentTests.Rules);
            yield return new Vector("pull-tornado", TraitTrailTestData.Environment(PullTestData.Input(extra: 3, distance: 4),
                TornadoEnvironmentTests.Source(cell: new CellCoord(10, 7))), TornadoEnvironmentTests.Rules);
            var sand = EnemyMovementPathTests.Input(targetCell: new CellCoord(4, 1));
            var enemy = sand.Characters.Single(c => c.EntityId == sand.ActorId);
            sand = CombatCommandTestData.With(sand,
                sand.Characters.Select(c => c.EntityId == sand.ActorId ? CombatCommandTestData.With(c, appointRange: 3) : c).ToArray(),
                profiles: sand.Profiles.Select(p => p.Definition.Key == enemy.CharacterKey ? new CombatAttackProfile(p.Definition,
                    new TargetingMethodSpec(TargetingShape.CardinalLine, TargetingChoiceMode.Single, TargetingSplash.None, 0)) : p).ToArray());
            sand = HighGroundMovementTests.TerrainInput(sand, new StaticTerrainFact(11, new CellCoord(1, 1), StaticTerrainKind.Sandstorm,
                true, new[] { Owner.Player })).WithIntent(CombatCommandKind.Attack, AttackAim.ForCell(new CellCoord(4, 1)), default, null, null);
            yield return new Vector("hidden-attack-rejection", sand);
            var shared = CombatCommandEffectTests.ActorEffect(new StationaryFireTraitSpec(30), new StationaryFireTraitStatusSnapshot(false));
            var other = CombatCommandTestData.With(CombatCommandTestData.Character(77, EntityKey.Unit_Akimbo, new CellCoord(2, 1), Owner.Player, 20),
                traits: new TraitStatusSnapshot[] { new StationaryFireTraitStatusSnapshot(true) });
            yield return new Vector("shared-profile-distinct-state", TraitCombatFixtures.Characters(shared, shared.Characters.Append(other).ToArray()));
            var victory = CombatTraitTestFixtures.WithRewards(CombatCommandTestData.Input(empty: true));
            var hqCell = new CellCoord(4, 3);
            var hq = new StructureCombatFacts(new EntityId(20), new StructureSpec(EntityKey.Structure_Headquarters, 1, 1,
                new[] { CellCoord.Zero }, 1, false, 0), new HealthStateSnapshot(1, 1), hqCell, Rotation4.R0, new[] { hqCell }, Owner.Enemy);
            yield return new Vector("terminal-hq", CombatCommandTestData.With(victory, state: victory.State.With(structures: new[] { hq })));
            var defeat = ThornHeartTestData.Input(heart: false, hp: 100);
            defeat = CombatCommandTestData.With(defeat, defeat.Characters.Select(c => c.EntityId.Value == 10
                ? CombatCommandTestData.With(c, position: new CellCoord(4, 3), hp: 1)
                : c.EntityId.Value == 2 ? CombatCommandTestData.With(c, position: new CellCoord(5, 4)) : c).ToArray(),
                main: defeat.Main.WithCurrentHealth(1));
            yield return new Vector("terminal-main", defeat);
        }

        // 테스트 전용 전체 공개 사실 직렬화다. 순서 있는 목록은 그대로 두고 사전만 키로 정렬한다.
        internal static string Serialize(object value, ISet<string> types)
        {
            var text = new StringBuilder();
            Write(value, text, types, 0);
            return text.ToString();
        }

        private static void Write(object value, StringBuilder text, ISet<string> types, int depth)
        {
            if (depth > 100) throw new InvalidOperationException("사실 그래프에 순환이 있습니다.");
            if (value == null) { text.Append("null"); return; }
            var type = value.GetType();
            types.Add(type.FullName);
            if (value is string word) { Quote(word, text); return; }
            if (type.IsEnum) { Quote(type.Name + ":" + Convert.ToInt64(value, CultureInfo.InvariantCulture), text); return; }
            if (value is bool flag) { text.Append(flag ? "true" : "false"); return; }
            if (type.IsPrimitive || value is decimal) { text.Append(Convert.ToString(value, CultureInfo.InvariantCulture)); return; }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ReadOnlyMemory<>))
            {
                Write(type.GetMethod("ToArray", Type.EmptyTypes).Invoke(value, null), text, types, depth + 1);
                return;
            }
            bool dictionary = value is IDictionary || type.GetInterfaces().Any(i => i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>));
            if (dictionary)
            {
                var entries = new List<(string Key, object Value)>();
                foreach (object pair in (IEnumerable)value)
                {
                    object key, item;
                    if (pair is DictionaryEntry entry) { key = entry.Key; item = entry.Value; }
                    else { key = pair.GetType().GetProperty("Key").GetValue(pair); item = pair.GetType().GetProperty("Value").GetValue(pair); }
                    entries.Add((Serialize(key, types), item));
                }
                text.Append('[');
                bool first = true;
                foreach (var entry in entries.OrderBy(e => e.Key, StringComparer.Ordinal))
                {
                    if (!first) text.Append(','); first = false;
                    text.Append('[').Append(entry.Key).Append(','); Write(entry.Value, text, types, depth + 1); text.Append(']');
                }
                text.Append(']'); return;
            }
            if (value is IEnumerable sequence)
            {
                text.Append('['); bool first = true;
                foreach (object item in sequence) { if (!first) text.Append(','); first = false; Write(item, text, types, depth + 1); }
                text.Append(']'); return;
            }
            if (type.Assembly != typeof(CombatCommandInput).Assembly)
                throw new InvalidOperationException("동결되지 않은 사실 형식: " + type.FullName);
            text.Append("{\"type\":"); Quote(type.FullName.Replace("ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.", ""), text);
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.GetIndexParameters().Length == 0 && p.CanRead).OrderBy(p => p.Name, StringComparer.Ordinal).ToArray();
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public).OrderBy(f => f.Name, StringComparer.Ordinal).ToArray();
            if (properties.Length + fields.Length == 0) throw new InvalidOperationException("공개 사실이 없는 형식: " + type.FullName);
            foreach (var property in properties)
            {
                object propertyValue = property.GetValue(value);
                // 추가된 선택적 사실의 기본값은 기존 동결 기록과 같은 형태로 씁니다. 지정한 값은 그대로 비교합니다.
                if (value is TargetingMethodSpec && property.Name == nameof(TargetingMethodSpec.Width) && (int)propertyValue == 0 ||
                    value is CombatCommandInput && property.Name == nameof(CombatCommandInput.SweepSide) && propertyValue == null ||
                    value is CombatCommandInput && property.Name is (nameof(CombatCommandInput.PlayerSkillRequest) or
                        nameof(CombatCommandInput.PlayerSkillSource)) && propertyValue == null ||
                    value is CombatCommandState && property.Name == nameof(CombatCommandState.PlayerSkill) && propertyValue == null ||
                    value is CombatCommandState && property.Name == nameof(CombatCommandState.LastPlatformBoundaryCount) && (long)propertyValue == 0 ||
                    value is NexusReinforcementState { IsEnabled: false, LastSpawnEnemyTurn: 0 } &&
                        property.Name == nameof(NexusReinforcementState.LastSpawnEnemyTurn) ||
                    value is AttackPreview && property.Name == nameof(AttackPreview.PillarFalls) && ((AttackPreview)value).PillarFalls.Count == 0 ||
                    value is CombatCommandTransition && property.Name == nameof(CombatCommandTransition.PlayerSkill) && propertyValue == null ||
                    value is OwnedCharacterRecord && property.Name == nameof(OwnedCharacterRecord.PlayerSkillUpgrade) &&
                        (PlayerSkillUpgradeKey)propertyValue == PlayerSkillUpgradeKey.None ||
                    value is AttackResult && property.Name == nameof(AttackResult.Sweep) && propertyValue == null ||
                    value is AttackPreview && property.Name == nameof(AttackPreview.Sweep) && propertyValue == null ||
                    value is AttackHitResult && property.Name == nameof(AttackHitResult.IsObstacle) && !(bool)propertyValue ||
                    value is ObstacleContentProfile obstacleProfile && obstacleProfile.Kind != ObstacleContentKind.Pillar &&
                        property.Name == nameof(ObstacleContentProfile.FallLength) && (int)propertyValue == 0 ||
                    value is CombatObstacleReactionEvent && property.Name == nameof(CombatObstacleReactionEvent.SuppressSupplyRewards) &&
                        !(bool)propertyValue ||
                    value is StructureCombatFacts structure && structure.ObstacleDurability == null &&
                        property.Name is (nameof(StructureCombatFacts.ObstacleDurability) or nameof(StructureCombatFacts.IsDestroyed)))
                    continue;
                text.Append(','); Quote(property.Name, text); text.Append(':'); Write(propertyValue, text, types, depth + 1);
            }
            foreach (var field in fields) { text.Append(','); Quote(field.Name, text); text.Append(':'); Write(field.GetValue(value), text, types, depth + 1); }
            text.Append('}');
        }

        private static void Quote(string value, StringBuilder text)
        {
            text.Append('"');
            foreach (char c in value)
                if (c == '"' || c == '\\') text.Append('\\').Append(c);
                else if (c < ' ') text.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                else text.Append(c);
            text.Append('"');
        }
    }
}
