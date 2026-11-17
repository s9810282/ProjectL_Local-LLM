using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>한 명령의 장애물 피해, 배타적 반응과 연쇄 폭발을 투영한다. 게시와 보상은 호출자가 처리한다.</summary>
    internal sealed class ObstacleReactionCalculator
    {
        private readonly CombatCommandInput input;
        private readonly CommandEnvironmentState environment;
        private readonly Dictionary<EntityId, CharacterCombatFacts> characters;
        private readonly Dictionary<EntityId, StructureCombatFacts> structures;
        private readonly List<CombatEvent> events;
        private readonly Dictionary<EntityId, CellCoord> deathAnchors;
        private readonly Dictionary<EntityId, ObstacleContentProfile> profiles;
        private readonly Queue<(StructureCombatFacts Source, ObstacleContentProfile Profile, CombatDamageOrigin Origin,
            CombatObstacleReactionEvent Occurrence, CombatObstacleDurabilityEvent Destruction, IReadOnlyList<CellCoord> PillarCells)> consequences = new();
        private readonly HashSet<EntityId> destroyed = new();
        private readonly CommandAttackGroups attackGroups;
        private readonly CombatHitCause sharedHitCause;
        private readonly CombatDamageApplication damageApplication;
        internal Dictionary<EntityId, int> BlastDamage { get; } = new();
        internal HashSet<EntityId> BlastTargetIds { get; } = new();
        internal HashSet<CellCoord> TerrainQueryCells { get; } = new();

        internal ObstacleReactionCalculator(CombatCommandInput input, CommandEnvironmentState environment,
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

        /// <summary>이전 타격의 장부가 소비한 집계만 지운다. 명령의 환경과 파괴 기록은 유지한다.</summary>
        internal void ResetHitSummaries()
        {
            if (consequences.Count != 0) throw new InvalidOperationException("장애물 연쇄가 끝난 뒤 타격 집계를 초기화해야 합니다.");
            BlastDamage.Clear();
            BlastTargetIds.Clear();
            TerrainQueryCells.Clear();
        }

        internal void DamageStructure(EntityId id, int amount, CombatDamageOrigin origin, CombatDamageCause cause = CombatDamageCause.NumericDamage,
            CombatEvent contextEvent = null, MovementTileFact environmentSource = null, EffectOriginKey? effectOrigin = null)
        {
            if (!structures.TryGetValue(id, out var before)) throw new InvalidOperationException("피해 대상 구조물이 없습니다.");
            if (before.ObstacleDurability != null || HasProfile(id))
                throw new InvalidOperationException("장애물은 타수 내구도 경계를 사용해야 합니다.");
            if (!Enum.IsDefined(typeof(CombatDamageCause), cause)) throw new ArgumentOutOfRangeException(nameof(cause));
            if (before.Health.IsDead || cause == CombatDamageCause.NumericDamage && amount == 0) return;
            var after = new StructureCombatFacts(before.EntityId, before.Definition,
                new HealthStateSnapshot(cause == CombatDamageCause.InstantDefeat ? 0 : Math.Max(0, before.Health.CurrentHealth - Math.Min(before.Health.CurrentHealth, amount)), before.Health.MaxHealth),
                before.Anchor, before.Rotation, before.OccupiedCells, before.Owner);
            structures[id] = after;
            var origins = new[] { origin };
            var evidence = new CombatStructureEvent(before, after, origins, cause);
            events.Add(evidence);
            var source = new CombatHealthSource(CombatHealthOperationKind.Damage, effectOrigin, origins,
                contextEvent: contextEvent, damageCause: cause, environmentSource: environmentSource);
            CombatHealthRecording.Add(damageApplication.HealthExecutions, id, before.Health, after.Health, source, evidence,
                loss: before.Health.CurrentHealth - after.Health.CurrentHealth);
        }

        internal void ApplyHit(EntityId id, CombatDamageOrigin origin,
            ObstacleDestructionCause cause = ObstacleDestructionCause.DeliveredHit, CombatEvent contextEvent = null,
            MovementTileFact environmentSource = null)
        {
            if (cause is not (ObstacleDestructionCause.DeliveredHit or ObstacleDestructionCause.EnvironmentalContact or
                ObstacleDestructionCause.BarrelBlast))
                throw new ArgumentOutOfRangeException(nameof(cause));
            var before = RequireObstacle(id);
            if (before.IsDestroyed) return;
            ChangeDurability(before, before.ObstacleDurability.RemainingHits - 1, cause, origin, contextEvent, environmentSource);
        }

        internal void DestroyObstacle(EntityId id, CombatDamageOrigin origin, ObstacleDestructionCause cause,
            CombatEvent contextEvent = null)
        {
            if (cause is not (ObstacleDestructionCause.BarrelChain or ObstacleDestructionCause.ExplicitDetonation or ObstacleDestructionCause.PillarCrush))
                throw new ArgumentOutOfRangeException(nameof(cause));
            var before = RequireObstacle(id);
            if (cause != ObstacleDestructionCause.PillarCrush && profiles[id].Kind != ObstacleContentKind.PowderBarrel)
                throw new ArgumentException("즉시 폭발은 화약통에만 적용합니다.", nameof(id));
            if (before.IsDestroyed) return;
            ChangeDurability(before, 0, cause, origin, contextEvent);
        }

        internal void RemoveFallenObstacle(EntityId id, CombatFallEvent fall)
        {
            var before = RequireObstacle(id);
            if (fall == null || fall.EntityId != id) throw new ArgumentException("장애물 낙사 사건이 필요합니다.", nameof(fall));
            if (!before.IsDestroyed) ChangeDurability(before, 0, ObstacleDestructionCause.Fall, null, fall);
        }

        private StructureCombatFacts RequireObstacle(EntityId id)
        {
            if (!structures.TryGetValue(id, out var fact) || !HasProfile(id) || fact.ObstacleDurability == null)
                throw new ArgumentException("타수 내구도를 가진 장애물이 필요합니다.", nameof(id));
            return fact;
        }

        private void ChangeDurability(StructureCombatFacts before, int remaining, ObstacleDestructionCause cause,
            CombatDamageOrigin origin, CombatEvent contextEvent, MovementTileFact environmentSource = null)
        {
            if (origin?.PhysicalCell is CellCoord source && !input.State.IsInside(source))
                throw new ArgumentOutOfRangeException(nameof(origin));
            var id = before.EntityId;
            var profile = profiles[id];
            var after = before.WithObstacleDurability(new ObstacleDurabilitySnapshot(remaining, before.ObstacleDurability.RequiredHits));
            var evidence = new CombatObstacleDurabilityEvent(before, after, cause, origin, contextEvent, environmentSource);
            IReadOnlyList<CellCoord> pillarCells = null;
            if (after.IsDestroyed && profile.Kind == ObstacleContentKind.Pillar && cause != ObstacleDestructionCause.Fall)
                pillarCells = PillarFallCalculator.GetCells(before.Anchor, origin.PhysicalCell.Value, profile.FallLength,
                    input.State.BoardWidth, input.State.BoardHeight);
            if (after.IsDestroyed && !destroyed.Add(id))
                throw new InvalidOperationException("같은 명령에서 장애물을 두 번 파괴할 수 없습니다.");
            structures[id] = after;
            events.Add(evidence);
            if (cause == ObstacleDestructionCause.Fall) return;
            var reaction = Select(before.ObstacleDurability.RemainingHits, remaining);
            long group = profile.Kind != ObstacleContentKind.Supplies && reaction == ObstacleReactionKind.Destroyed
                ? attackGroups.Next() : origin.AttackGroup;
            var physicalOrigin = new CombatDamageOrigin(before.Anchor, origin.CreditedCharacterId, group);
            bool suppressRewards = cause == ObstacleDestructionCause.PillarCrush ||
                contextEvent is CombatObstacleReactionEvent parent && parent.SuppressSupplyRewards;
            var occurrence = new CombatObstacleReactionEvent(id, profile.Kind, reaction, physicalOrigin, group, suppressRewards);
            events.Add(occurrence);
            if (!suppressRewards && profile.Kind == ObstacleContentKind.Supplies && origin.CreditedCharacterId is EntityId credit &&
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
            if (profile.Kind != ObstacleContentKind.Supplies && reaction == ObstacleReactionKind.Destroyed)
                consequences.Enqueue((before, profile, physicalOrigin, occurrence, evidence, pillarCells));
        }

        internal void ResolveExplosions()
        {
            foreach (var _ in ResolveExplosionSteps()) { }
        }

        internal IEnumerable<string> ResolveExplosionSteps()
        {
            // 한 명령의 FIFO 큐다. 기둥·폭발 자식은 먼저 파괴 표시하고 현재 영향의 정산 뒤 실행한다.
            while (consequences.TryDequeue(out var blast))
            {
                if (blast.Profile.Kind == ObstacleContentKind.Pillar)
                {
                    foreach (var phase in PillarFallCalculator.ResolveSteps(input, environment, damageApplication, structures,
                        deathAnchors, this, blast.Destruction, blast.Occurrence, blast.Profile.FallLength, blast.PillarCells))
                        yield return phase;
                    continue;
                }
                var hitCause = sharedHitCause ?? new CombatHitCause();
                var currentEnvironment = environment.Facts;
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
                        before.Anchor, currentEnvironment).FinalDamage;
                    yield return "ObstacleBlastDamageInput";
                    foreach (var phase in damageApplication.ApplySteps(id, CombatDamageKind.EnvironmentDamage, blast.Origin,
                        damage, value => damage = value.RemainingDamage, heldDefenseGroups: attackGroups,
                        contextEvent: blast.Occurrence)) yield return phase;
                    if (characters[id].Health.IsDead) deathAnchors[id] = before.Anchor.Value;
                    BlastDamage[id] = checked(BlastDamage.GetValueOrDefault(id) + damage);
                    yield return "ObstacleBlastCharacter";
                }
                // 점유 칸이 여러 개 겹쳐도 각 개체는 이 폭발에서 한 번만 정산한다.
                targets.Clear();
                foreach (var structure in structures.Values)
                {
                    if (!structure.IsDestroyed && structure.OccupiedCells.Any(cells.Contains))
                        targets.Add(structure.EntityId);
                    yield return "ObstacleBlastStructureScan";
                }
                foreach (var id in targets)
                {
                    BlastTargetIds.Add(id);
                    if (!profiles.TryGetValue(id, out var profile))
                    {
                        DamageStructure(id, blast.Profile.ExplosionDamage, blast.Origin, contextEvent: blast.Occurrence);
                        BlastDamage[id] = checked(BlastDamage.GetValueOrDefault(id) + blast.Profile.ExplosionDamage);
                    }
                    else if (profile.Kind == ObstacleContentKind.PowderBarrel)
                        DestroyObstacle(id, blast.Origin, ObstacleDestructionCause.BarrelChain, blast.Occurrence);
                    else
                        ApplyHit(id, blast.Origin, ObstacleDestructionCause.BarrelBlast, blast.Occurrence);
                    yield return "ObstacleBlastStructure";
                }
                yield return "ObstacleBlastComplete";
            }
        }

        internal static ObstacleReactionKind Select(int beforeHits, int afterHits)
        {
            if (beforeHits < 0) throw new ArgumentOutOfRangeException(nameof(beforeHits));
            if (afterHits < 0) throw new ArgumentOutOfRangeException(nameof(afterHits));
            if (beforeHits == 0 || afterHits >= beforeHits) return ObstacleReactionKind.None;
            return afterHits == 0 ? ObstacleReactionKind.Destroyed : ObstacleReactionKind.Hit;
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
