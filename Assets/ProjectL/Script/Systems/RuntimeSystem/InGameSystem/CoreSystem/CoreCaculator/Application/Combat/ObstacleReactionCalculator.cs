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
        private readonly CombatHitCause sharedHitCause;
        private readonly CombatDamageApplication damageApplication;
        internal Dictionary<EntityId, int> BlastDamage { get; } = new();
        internal HashSet<EntityId> BlastTargetIds { get; } = new();
        internal HashSet<CellCoord> TerrainQueryCells { get; } = new();

        internal ObstacleReactionCalculator(CombatCommandInput input, MovementEnvironmentFacts environment,
            CombatDamageApplication damage, Dictionary<EntityId, StructureCombatFacts> structures,
            Dictionary<EntityId, CellCoord> deathAnchors, CommandAttackGroups attackGroups = null,
            CombatHitCause hitCause = null)
        {
            damageApplication = damage ?? throw new ArgumentNullException(nameof(damage));
            // 캐릭터 사전과 사건 목록은 피해 적용 경계와 같은 원본을 쓴다.
            this.input = input; this.environment = environment; characters = damage.Characters;
            this.structures = structures; events = damage.Events; this.deathAnchors = deathAnchors;
            this.attackGroups = attackGroups ?? new CommandAttackGroups();
            // 공격 명령은 공격 원인을 공유한다. 독립 환경 발동에서는 폭발마다 새 원인을 만든다(D7).
            sharedHitCause = hitCause;
            profiles = input.ObstacleContents.ToDictionary(p => p.EntityId);
        }

        internal bool HasProfile(EntityId id) => profiles.ContainsKey(id);

        internal void DamageStructure(EntityId id, int amount, CombatDamageOrigin origin, CombatDamageCause cause = CombatDamageCause.NumericDamage)
        {
            if (!structures.TryGetValue(id, out var before)) throw new InvalidOperationException("피해 대상 구조물이 없습니다.");
            if (!Enum.IsDefined(typeof(CombatDamageCause), cause)) throw new ArgumentOutOfRangeException(nameof(cause));
            if (before.Health.IsDead || cause == CombatDamageCause.NumericDamage && amount == 0) return;
            var after = new StructureCombatFacts(before.EntityId, before.Definition,
                new HealthStateSnapshot(cause == CombatDamageCause.InstantDefeat ? 0 : Math.Max(0, before.Health.CurrentHealth - Math.Min(before.Health.CurrentHealth, amount)), before.Health.MaxHealth),
                before.Anchor, before.Rotation, before.OccupiedCells, before.Owner);
            structures[id] = after;
            events.Add(new CombatStructureEvent(before, after, new[] { origin }, cause));
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
            foreach (var _ in ResolveExplosionSteps()) { }
        }

        internal IEnumerable<string> ResolveExplosionSteps()
        {
            // 큐는 한 명령에만 존재한다. 다음 폭발은 앞선 폭발의 체력 정산이 끝난 최신 사실을 읽는다.
            while (explosions.TryDequeue(out var blast))
            {
                var hitCause = sharedHitCause ?? new CombatHitCause();
                var cells = new HashSet<CellCoord>(BarrelCells(blast.Source.Anchor, input.State.BoardWidth, input.State.BoardHeight));
                var targets = new SortedSet<EntityId>();
                foreach (var character in characters.Values)
                {
                    if (character.Area == CharacterRuntimeArea.Field && !character.Health.IsDead &&
                        character.OccupiedCells.Any(cells.Contains)) targets.Add(character.EntityId);
                    yield return "ObstacleBlastCharacterScan";
                }
                foreach (var id in targets)
                {
                    var before = characters[id];
                    // 피해가 전부 막혀도 보유 방어를 읽고 소비하므로 공개 기대값에 수신자를 남긴다.
                    BlastTargetIds.Add(id);
                    TerrainQueryCells.Add(before.Anchor.Value);
                    TerrainQueryCells.Add(blast.Source.Anchor);
                    RoarHitCounter.Record(characters, id, hitCause, input.State.InCombat);
                    int damage = CombatDamageCalculator.Environment(input, blast.Profile.ExplosionDamage, blast.Origin,
                        before.Anchor, environment).FinalDamage;
                    yield return "ObstacleBlastDamageInput";
                    foreach (var phase in damageApplication.ApplySteps(id, CombatDamageKind.EnvironmentDamage, blast.Origin,
                        damage, value => damage = value.RemainingDamage, heldDefenseGroups: attackGroups)) yield return phase;
                    if (characters[id].Health.IsDead) deathAnchors[id] = before.Anchor.Value;
                    BlastDamage[id] = checked(BlastDamage.GetValueOrDefault(id) + damage);
                    yield return "ObstacleBlastCharacter";
                }
                // 점유 칸이 여러 개 겹쳐도 각 개체는 이 폭발에서 한 번만 정산한다.
                targets.Clear();
                foreach (var structure in structures.Values)
                {
                    if (!structure.Health.IsDead && structure.OccupiedCells.Any(cells.Contains))
                        targets.Add(structure.EntityId);
                    yield return "ObstacleBlastStructureScan";
                }
                foreach (var id in targets)
                {
                    BlastTargetIds.Add(id);
                    DamageStructure(id, blast.Profile.ExplosionDamage, blast.Origin);
                    BlastDamage[id] = checked(BlastDamage.GetValueOrDefault(id) + blast.Profile.ExplosionDamage);
                    yield return "ObstacleBlastStructure";
                }
                yield return "ObstacleBlastComplete";
            }
        }

        internal static ObstacleReactionKind Select(int beforeHp, int afterHp)
        {
            if (beforeHp < 0) throw new ArgumentOutOfRangeException(nameof(beforeHp));
            if (afterHp < 0) throw new ArgumentOutOfRangeException(nameof(afterHp));
            if (beforeHp == 0 || afterHp >= beforeHp) return ObstacleReactionKind.None;
            return afterHp == 0 ? ObstacleReactionKind.Destroyed : ObstacleReactionKind.Hit;
        }

        // 폭발 칸 모양의 원본은 CombatHazardAreas 하나다(View 호버도 같은 입구를 부른다).
        internal static IReadOnlyList<CellCoord> BarrelCells(CellCoord origin, int width, int height)
        {
            var cells = new List<CellCoord>(8);
            CombatHazardAreas.PowderBarrelCells(origin, width, height, cells);
            return cells.AsReadOnly();
        }
    }
}
