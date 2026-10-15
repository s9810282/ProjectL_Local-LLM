using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>한 명령의 장애물 피해, 배타적 반응과 연쇄 폭발을 투영한다. 게시와 보상은 호출자가 처리한다.</summary>
    internal sealed class ObstacleReactionCalculator
    {
        private readonly CombatCommandInput input;
        private readonly MovementEnvironmentFacts environment;
        private readonly Dictionary<EntityId, CharacterCombatFacts> characters;
        private readonly Dictionary<EntityId, StructureCombatFacts> structures;
        private readonly List<CombatEvent> events;
        private readonly Dictionary<EntityId, CellCoord> deathAnchors;
        private readonly Dictionary<EntityId, ObstacleContentProfile> profiles;
        private readonly Queue<(StructureCombatFacts Source, ObstacleContentProfile Profile, CombatDamageOrigin Origin)> explosions = new();
        private readonly HashSet<EntityId> destroyed = new();
        private readonly CommandAttackGroups attackGroups;
        internal Dictionary<EntityId, int> BlastDamage { get; } = new();
        internal HashSet<EntityId> BlastTargetIds { get; } = new();
        internal HashSet<CellCoord> TerrainQueryCells { get; } = new();

        internal ObstacleReactionCalculator(CombatCommandInput input, MovementEnvironmentFacts environment,
            Dictionary<EntityId, CharacterCombatFacts> characters, Dictionary<EntityId, StructureCombatFacts> structures,
            List<CombatEvent> events, Dictionary<EntityId, CellCoord> deathAnchors, CommandAttackGroups attackGroups = null)
        {
            this.input = input; this.environment = environment; this.characters = characters;
            this.structures = structures; this.events = events; this.deathAnchors = deathAnchors;
            this.attackGroups = attackGroups ?? new CommandAttackGroups();
            profiles = input.ObstacleContents.ToDictionary(p => p.EntityId);
        }

        internal bool HasProfile(EntityId id) => profiles.ContainsKey(id);

        internal void DamageStructure(EntityId id, int amount, CombatDamageOrigin origin)
        {
            if (!structures.TryGetValue(id, out var before)) throw new InvalidOperationException("피해 대상 구조물이 없습니다.");
            if (before.Health.IsDead || amount == 0) return;
            var after = new StructureCombatFacts(before.EntityId, before.Definition,
                new HealthStateSnapshot(Math.Max(0, before.Health.CurrentHealth - Math.Min(before.Health.CurrentHealth, amount)), before.Health.MaxHealth),
                before.Anchor, before.Rotation, before.OccupiedCells, before.Owner);
            structures[id] = after;
            events.Add(new CombatStructureEvent(before, after, new[] { origin }));
            if (!profiles.TryGetValue(id, out var profile)) return;
            var reaction = Select(before.Health.CurrentHealth, after.Health.CurrentHealth);
            if (reaction == ObstacleReactionKind.Destroyed && !destroyed.Add(id)) return;
            long group = profile.Kind == ObstacleContentKind.PowderBarrel && reaction == ObstacleReactionKind.Destroyed
                ? attackGroups.Next() : origin.AttackGroup;
            var physicalOrigin = new CombatDamageOrigin(before.Anchor, origin.CreditedCharacterId, group);
            events.Add(new CombatObstacleReactionEvent(id, profile.Kind, reaction, physicalOrigin, group));
            if (profile.Kind == ObstacleContentKind.Supplies && origin.CreditedCharacterId is EntityId credit &&
                characters.TryGetValue(credit, out var recipient) && !recipient.Health.IsDead)
            {
                var rewarded = HeldEnvironmentEffects.GrantSupplies(recipient,
                    reaction == ObstacleReactionKind.Destroyed ? profile.DestructionAttackBonus : profile.HitAttackBonus);
                if (!ReferenceEquals(recipient, rewarded))
                {
                    characters[credit] = rewarded;
                    events.Add(new CombatHeldEnvironmentEvent(recipient, rewarded, physicalOrigin));
                }
            }
            if (profile.Kind == ObstacleContentKind.PowderBarrel && reaction == ObstacleReactionKind.Destroyed)
                explosions.Enqueue((before, profile, new CombatDamageOrigin(before.Anchor, origin.CreditedCharacterId, group)));
        }

        internal void ResolveExplosions()
        {
            // 큐는 한 명령에만 존재한다. 다음 폭발은 앞선 폭발의 체력 정산이 끝난 최신 사실을 읽는다.
            while (explosions.TryDequeue(out var blast))
            {
                var cells = new HashSet<CellCoord>(BarrelCells(blast.Source.Anchor, input.State.BoardWidth, input.State.BoardHeight));
                foreach (var id in characters.Values.Where(c => c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead &&
                    c.OccupiedCells.Any(cells.Contains)).Select(c => c.EntityId).OrderBy(id => id).ToArray())
                {
                    var before = characters[id];
                    // 피해가 전부 막혀도 보유 방어를 읽고 소비하므로 공개 기대값에 수신자를 남긴다.
                    BlastTargetIds.Add(id);
                    TerrainQueryCells.Add(before.Anchor.Value);
                    TerrainQueryCells.Add(blast.Source.Anchor);
                    int damage = CombatDamageCalculator.Environment(input, blast.Profile.ExplosionDamage, blast.Origin,
                        before.Anchor, environment).FinalDamage;
                    damage = CombatDamageCalculator.ApplyHeldDefense(attackGroups, characters, id, damage, blast.Origin, events);
                    before = characters[id];
                    if (damage == 0) continue;
                    int hp = Math.Max(0, before.Health.CurrentHealth - Math.Min(before.Health.CurrentHealth, damage));
                    var after = hp == 0 ? CombatCommandCalculator.Hurt(before, damage) :
                        CharacterSettlementCalculator.Copy(before, new HealthStateSnapshot(hp, before.Health.MaxHealth), before.Stats);
                    characters[id] = after;
                    if (after.Health.IsDead) deathAnchors[id] = before.Anchor.Value;
                    CombatCommandEvents.Health(events, before, after, new[] { blast.Origin });
                    BlastDamage[id] = checked(BlastDamage.GetValueOrDefault(id) + damage);
                }
                // 점유 칸이 여러 개 겹쳐도 각 개체는 이 폭발에서 한 번만 정산한다.
                foreach (var id in structures.Values.Where(s => !s.Health.IsDead && s.OccupiedCells.Any(cells.Contains))
                    .Select(s => s.EntityId).OrderBy(id => id).ToArray())
                {
                    BlastTargetIds.Add(id);
                    DamageStructure(id, blast.Profile.ExplosionDamage, blast.Origin);
                    BlastDamage[id] = checked(BlastDamage.GetValueOrDefault(id) + blast.Profile.ExplosionDamage);
                }
            }
        }

        internal static ObstacleReactionKind Select(int beforeHp, int afterHp)
        {
            if (beforeHp < 0) throw new ArgumentOutOfRangeException(nameof(beforeHp));
            if (afterHp < 0) throw new ArgumentOutOfRangeException(nameof(afterHp));
            if (beforeHp == 0 || afterHp >= beforeHp) return ObstacleReactionKind.None;
            return afterHp == 0 ? ObstacleReactionKind.Destroyed : ObstacleReactionKind.Hit;
        }

        internal static IReadOnlyList<CellCoord> BarrelCells(CellCoord origin, int width, int height)
        {
            if (width < 1) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 1) throw new ArgumentOutOfRangeException(nameof(height));
            if (origin.X < 0 || origin.Y < 0 || origin.X >= width || origin.Y >= height)
                throw new ArgumentOutOfRangeException(nameof(origin));
            var cells = new List<CellCoord>(8);
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    long x = (long)origin.X + dx;
                    long y = (long)origin.Y + dy;
                    if (x >= 0 && y >= 0 && x < width && y < height)
                        cells.Add(new CellCoord((int)x, (int)y));
                }
            return cells.AsReadOnly();
        }
    }
}
