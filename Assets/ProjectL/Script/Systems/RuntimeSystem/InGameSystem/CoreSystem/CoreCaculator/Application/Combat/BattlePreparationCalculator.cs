using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Items;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Placement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Tiles;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    public static class BattlePreparationCalculator
    {
        public static BattlePreparation Prepare(BattleInitialization initialization,
            IReadOnlyList<OwnedCharacterRecord> owned, MainCharacterPersistentState main,
            IReadOnlyDictionary<CharacterInstanceId, int> souls, InitialDrawPlan draw,
            IReadOnlyList<CharacterCombatFacts> activeCharacters, RunDeckSnapshot membership = null, long rosterRevision = 0)
        {
            if (initialization == null || owned == null || souls == null || draw == null || activeCharacters == null)
                throw new ArgumentNullException("전투 정의와 Buffer 사실이 필요합니다.");
            if (main == null || main.CurrentHealth <= 0)
                throw new InvalidOperationException("살아 있는 메인 지속 상태가 필요합니다.");
            if (activeCharacters.Count != 0)
                throw new InvalidOperationException("이미 연결된 전투 캐릭터가 있습니다.");

            ValidateNexusReinforcements(initialization);
            if (initialization.AttackProfiles.Count > 0)
            {
                _ = OwnedUnitSpecResolver.BuildProfiles(initialization.AttackProfiles, owned, initialization.TraitGrowth);
                // 미래 성장 경로의 전수 검사는 진입 때 한 번 수행하고, 매 명령 조립에서는 현재 이력만 합성한다.
                foreach (var record in owned.Where(o => o.SpecializationCategory.HasValue))
                    TraitGrowthCatalogValidator.ValidateSpecialization(initialization.TraitGrowth,
                        initialization.AttackProfiles.Single(p => p.Definition.Key == record.CharacterKey), record.SpecializationCategory.Value,
                        record.Role == CharacterOwnershipRole.Main ? initialization.PlayerSkill : null);
            }
            var items = new Dictionary<int, ItemDefinition>();
            var itemRules = ItemRuleRegistry.CreateDefault();
            foreach (var definition in initialization.ItemDefinitions)
            {
                if (!items.TryAdd(definition.Key, definition))
                    throw new InvalidOperationException("아이템 정의 키가 중복됩니다.");
                itemRules.GetRequired(definition.Effect);
            }
            foreach (var placement in initialization.Stage.InitialEnemyPlacements.Span)
            {
                if (!placement.StartingItemKey.HasValue)
                    continue;
                if (!items.TryGetValue(placement.StartingItemKey.Value, out var item))
                    throw new InvalidOperationException("초기 적 아이템 정의가 없습니다.");
                if (!itemRules.IsAvailable(item.Effect))
                    throw new InvalidOperationException(itemRules.GetUnavailableReason(item.Effect));
            }

            StageConfig stage = initialization.Stage;
            if (stage.ReserveCapacity <= draw.InitialCount)
                throw new InvalidOperationException("초기 손패를 담을 공간이 부족합니다.");

            var gimmickKeys = new HashSet<StageGimmickKey>();
            StageGimmickComposition.Default.ValidateDefinitions(stage.StageGimmicks.ToArray());
            foreach (var gimmick in stage.StageGimmicks.Span)
                if (gimmickKeys.Add(gimmick.GimmickKey) == false ||
                    gimmick.GimmickKey == StageGimmickKey.Railway && stage.RailCells.Length == 0)
                    throw new InvalidOperationException("기믹이 중복되었거나 철도 셀이 없습니다.");

            var traits = TraitRegistry.CreateDefault();
            var categories = new CategoryRegistry(initialization.Categories);
            var baseUnits = initialization.Units.Select(u => OwnedUnitSpecResolver.ResolveBase(u, initialization.TraitGrowth)).ToArray();
            var catalog = new UnitCatalog(baseUnits, traits, categories);
            catalog.ValidateCombatReady();
            var ownedDefinitions = new Dictionary<CharacterInstanceId, UnitDefinition>();
            var roster = new Dictionary<CharacterInstanceId, OwnedCharacterRecord>();
            OwnedCharacterRecord mainRecord = null;
            int subCount = 0;
            foreach (OwnedCharacterRecord record in owned)
            {
                if (record == null || roster.ContainsKey(record.Id))
                    throw new InvalidOperationException("보유 목록이 올바르지 않습니다.");
                roster.Add(record.Id, record);
                UnitDefinition definition = catalog.GetRequired(record.CharacterKey);
                if (!record.Growth.IsEmpty || record.SpecializationCategory.HasValue)
                {
                    var effective = OwnedUnitSpecResolver.Resolve(initialization.Units.Single(u => u.Key == record.CharacterKey), record, initialization.TraitGrowth);
                    var effectiveCatalog = new UnitCatalog(new[] { effective }, traits, categories);
                    effectiveCatalog.ValidateCombatReady();
                    definition = effectiveCatalog.GetRequired(record.CharacterKey);
                }
                ownedDefinitions.Add(record.Id, definition);
                UnitEligibilityRules.Require(definition, Owner.Player, record.Role == CharacterOwnershipRole.Main);
                if (souls.TryGetValue(record.Id, out int count) == false || count < 0)
                    throw new InvalidOperationException("보유 캐릭터의 영혼 기록이 필요합니다.");

                if (record.Role == CharacterOwnershipRole.Main)
                {
                    if (mainRecord != null)
                        throw new InvalidOperationException("메인은 한 명이어야 합니다.");
                    mainRecord = record;
                }
                else
                    subCount++;
            }

            if (mainRecord == null || draw.Order.Count != subCount)
                throw new InvalidOperationException("메인과 전체 서브 드로우 목록이 필요합니다.");
            var skillDefinition = initialization.PlayerSkill;
            if (skillDefinition != null)
            {
                if (skillDefinition.MainCharacterKey != mainRecord.CharacterKey)
                    throw new InvalidOperationException($"스킬 {skillDefinition.Key}의 메인 {skillDefinition.MainCharacterKey}과 로스터 메인 {mainRecord.CharacterKey}이 다릅니다.");
                // 기본·획득·전직 특성을 모두 합성한 정의를 캐릭터 생성 전에 검사한다.
                skillDefinition.ValidateTraits(ownedDefinitions[mainRecord.Id].TraitDefinitions.ToArray().Select(t => t.TraitKey));
            }
            foreach (CharacterInstanceId id in draw.Order)
                if (roster.TryGetValue(id, out var record) == false || record.Role != CharacterOwnershipRole.Sub)
                    throw new InvalidOperationException("드로우 대상은 보유 서브여야 합니다.");

            var occupied = new HashSet<CellCoord>();
            var characters = new List<CharacterCombatFacts>();
            var structures = new List<StructureCombatFacts>();
            int nextId = 1;
            UnitDefinition mainDefinition = ownedDefinitions[mainRecord.Id];
            var mainActorId = new EntityId(nextId++);
            // 메인은 셔플과 별개로 첫 손패 0번 슬롯에 지급하고, 실제 위치는 최초 배치 명령이 정한다.
            characters.Add(BattleCharacterCalculator.CreateOwned(mainActorId, mainDefinition,
                mainRecord, main, souls[mainRecord.Id], 0,
                null, null, Array.Empty<CellCoord>(), traits));

            var victoryTargets = new List<EntityId>();
            foreach (InitialEnemyPlacementSpec placement in stage.InitialEnemyPlacements.Span)
            {
                UnitDefinition definition = catalog.GetRequired(placement.UnitKey);
                UnitEligibilityRules.Require(definition, Owner.Enemy, false);
                var effective = baseUnits.Single(u => u.Key == placement.UnitKey);
                if (!HasAvailableBehavior(effective))
                    throw new InvalidOperationException(
                        $"DeferredUnitTurnTiming: 적 유닛 {effective.Key}에게 지금 쓸 수 있는 행동(기본 공격·자발 이동·포효)이 없습니다.");
                CellCoord[] cells = Place(definition.Footprint, placement.Anchor, placement.Rotation,
                    Owner.Enemy, stage, occupied);
                var id = new EntityId(nextId++);
                if (placement.IsVictoryTarget)
                    victoryTargets.Add(id);
                characters.Add(BattleCharacterCalculator.CreateEnemy(id, definition,
                    placement.Level, placement.Anchor, placement.Rotation, cells, traits,
                    new CharacterItemState(placement.StartingItemKey, Array.Empty<ItemTemporaryEffect>())));
            }
            BattleObjective objective = BindObjective(initialization.ObjectiveKind, victoryTargets);

            var structureCatalog = new Dictionary<EntityKey, StructureSpec>();
            foreach (StructureSpec definition in initialization.Structures)
                structureCatalog.Add(definition.Key, definition);

            foreach (InitialEnemyStructurePlacementSpec placement in stage.InitialEnemyStructurePlacements.Span)
            {
                if (structureCatalog.TryGetValue(placement.StructureKey, out var definition) == false)
                    throw new InvalidOperationException("구조물 정의를 찾을 수 없습니다.");
                var footprint = new TileFootprint(definition.FootprintWidth, definition.FootprintHeight,
                    definition.FootprintLocalCells.ToArray());
                CellCoord[] cells = Place(footprint, placement.Anchor, placement.Rotation,
                    Owner.Enemy, stage, occupied);
                structures.Add(new StructureCombatFacts(new EntityId(nextId++), definition,
                    new HealthStateSnapshot(definition.MaxHealth, definition.MaxHealth),
                    placement.Anchor, placement.Rotation, cells));
            }

            var obstacleContents = new List<ObstacleContentProfile>();
            foreach (var placement in initialization.ObstaclePlacements)
            {
                var definition = placement.Definition;
                var footprint = new TileFootprint(definition.FootprintWidth, definition.FootprintHeight,
                    definition.FootprintLocalCells.ToArray());
                var cells = Place(footprint, placement.Anchor, placement.Rotation, Owner.Neutral, stage, occupied, false);
                var id = new EntityId(nextId++);
                structures.Add(new StructureCombatFacts(id, definition,
                    new ObstacleDurabilitySnapshot(placement.RequiredHits, placement.RequiredHits),
                    placement.Anchor, placement.Rotation, cells, Owner.Neutral));
                obstacleContents.Add(new ObstacleContentProfile(id, placement.Kind, placement.ExplosionDamage,
                    placement.HitAttackBonus, placement.DestructionAttackBonus, placement.FallLength));
            }
            var bindings = obstacleContents.Count == 0 ? initialization.EnvironmentBindings : new EnvironmentSemanticBindings(initialization.EnvironmentBindings.TerrainSourceIds,
                initialization.EnvironmentBindings.ObjectSourceIds,
                initialization.EnvironmentBindings.ObstacleEntityIds.Concat(obstacleContents.Select(p => p.EntityId)).ToArray());
            // 구조물 점유가 확정된 뒤 최초 토네이도를 배치한다. 기존 깃발 보류 정책은 유지한다.
            var placed = InitialTornadoPlacementCalculator.Place(initialization, structures, bindings, stage.TimelineSeed);
            bindings = placed.Bindings;
            var environment = DeactivateDeferredFlags(placed.Environment);

            // 모든 배치 검증 후 초기 지급을 계산하며 서브 상한은 드로우 정책에서 정한다.
            for (int slot = 0; slot < draw.InitialCount; slot++)
            {
                OwnedCharacterRecord record = roster[draw.Order[slot]];
                characters.Add(BattleCharacterCalculator.CreateOwned(new EntityId(nextId++),
                    ownedDefinitions[record.Id], record, null,
                    souls[record.Id], slot + 1, null, null, Array.Empty<CellCoord>(), traits));
            }

            var playerSkill = skillDefinition == null ? null : new PlayerSkillBattleState(mainRecord.Id, mainActorId,
                skillDefinition, mainRecord.PlayerSkillUpgrade);
            var preparation = new BattlePreparation(characters, new CombatPreparation(stage.MapWidth,
                stage.MapHeight, stage.ReserveCapacity, stage.TimelineSeed,
                CombatFlowPhase.PlayerInput, draw.Order, draw.InitialCount, stage.PlayerZone,
                OwnedUnitSpecResolver.BuildTraitSettings(initialization.Units, owned, initialization.TraitGrowth),
                stage.RailCells.ToArray(), placed.Definitions.ToArray(),
                environment, bindings, obstacleContents, wallBlocking: initialization.WallBlocking,
                nexusReinforcements: initialization.NexusReinforcements), structures, nextId,
                objective: objective, membership: membership, rosterRevision: rosterRevision, environmentProgress: placed.Progress,
                playerSkill: playerSkill);
            var state = StageGimmickComposition.Initialize(preparation.CommandState, preparation.Combat.StageGimmicks);
            var preparedCharacters = BattleEnvironmentPreparation.Prepare(initialization, characters, structures, main, bindings, obstacleContents,
                environment, state.Gimmicks);
            return new BattlePreparation(preparedCharacters,
                preparation.Combat, structures, nextId, commandState: state, objective: objective,
                membership: membership, rosterRevision: rosterRevision);
        }

        private static void ValidateNexusReinforcements(BattleInitialization initialization)
        {
            var spec = initialization.NexusReinforcements;
            if (spec == null)
                return;

            if (initialization.ObjectiveKind != BattleObjectiveKind.EnemyHeadquartersDestroyed)
                throw new InvalidOperationException("넥서스 증원은 적 본부 파괴 목표에서만 사용할 수 있습니다.");

            // 이 배치 목록의 구조물은 준비 때 적 소유로 최대 체력에서 시작한다.
            int nexusCount = 0;
            foreach (var placement in initialization.Stage.InitialEnemyStructurePlacements.Span)
                if (placement.StructureKey == EntityKey.Structure_Headquarters)
                    nexusCount++;
            if (nexusCount != 1)
                throw new InvalidOperationException("넥서스 증원에는 적 본부가 정확히 하나 필요합니다.");

            foreach (var entry in spec.Pool)
            {
                var unit = initialization.Units.SingleOrDefault(candidate => candidate.Key == entry.UnitKey)
                    ?? throw new InvalidOperationException($"증원 풀에 필요한 유닛 정의가 없습니다: {entry.UnitKey}.");
                ValidateReinforcementUnit(unit);
                var profile = initialization.AttackProfiles.SingleOrDefault(candidate =>
                    !candidate.OwnedId.HasValue && candidate.Definition.Key == entry.UnitKey)
                    ?? throw new InvalidOperationException($"증원 풀에 필요한 기본 공격 프로필이 없습니다: {entry.UnitKey}.");
                if (!ReferenceEquals(unit, profile.Definition))
                    ValidateReinforcementUnit(profile.Definition);
            }
        }

        private static void ValidateReinforcementUnit(UnitSpec unit)
        {
            if (unit.Role != UnitRole.Regular || unit.OwnerEligibility == UnitOwnerEligibility.PlayerOnly)
                throw new InvalidOperationException($"증원 유닛 {unit.Key}의 역할 또는 적 소유 권한이 맞지 않습니다.");
            if (unit.CategoryKey == CategoryKey.Boss)
                throw new InvalidOperationException($"보스 {unit.Key}는 증원 풀에 등록할 수 없습니다.");
            var cells = unit.FootprintLocalCells.Span;
            if (unit.FootprintWidth != 1 || unit.FootprintHeight != 1 || cells.Length != 1 || cells[0] != CellCoord.Zero)
                throw new InvalidOperationException($"증원 유닛 {unit.Key}는 한 칸만 점유해야 합니다.");
        }

        /// <summary>
        /// 깃발 획득이 보류된 동안 배치된 깃발 원본을 비활성으로 바꾼다. 다른 타일, ID, 진입 순번, 순서는 그대로 둔다.
        /// </summary>
        private static MovementEnvironmentFacts DeactivateDeferredFlags(MovementEnvironmentFacts environment)
        {
            if (DeferredCombatFeatures.IsFlagPickupAvailable ||
                !environment.Tiles.Any(t => t is FlagSourceFact { Active: true }))
                return environment;
            return new MovementEnvironmentFacts(environment.Tiles
                .Select(t => t is FlagSourceFact flag ? (MovementTileFact)flag.WithActive(false) : t).ToArray());
        }

        /// <summary>
        /// 초기 적이 이번 전투에서 한 가지라도 행동할 수 있는지 판단한다. 기본 공격, 자발 이동, 지금 지원하는 반응 특성(포효)을 센다.
        /// 사령술처럼 보류된 특성만 가진 적은 전투에서 아무것도 하지 않으므로 구성 오류로 본다.
        /// </summary>
        private static bool HasAvailableBehavior(UnitSpec authored)
        {
            if (authored.Behavior.CanBasicAttack || authored.Behavior.CanMoveVoluntarily)
                return true;
            foreach (var trait in authored.Traits.Span)
                if (trait.TraitKey == TraitKey.Roar && DeferredCombatFeatures.IsTraitAvailable(TraitKey.Roar))
                    return true;
            return false;
        }

        /// <summary>목표 종류와 초기 적 배치 표식을 맞춰 대상 개체 하나에 결합한다. 종류와 표식이 어긋나면 준비를 거절한다.</summary>
        private static BattleObjective BindObjective(BattleObjectiveKind kind, IReadOnlyList<EntityId> victoryTargets)
        {
            if (kind == BattleObjectiveKind.EnemyHeadquartersDestroyed)
            {
                if (victoryTargets.Count != 0)
                    throw new InvalidOperationException("본부 목표 전투에는 지정 보스 표식을 둘 수 없습니다.");
                return BattleObjective.EnemyHeadquarters;
            }
            if (victoryTargets.Count != 1)
                throw new InvalidOperationException("지정 보스 목표에는 표식이 붙은 초기 적 배치가 정확히 하나 필요합니다.");
            return BattleObjective.DesignatedBoss(victoryTargets[0]);
        }

        private static CellCoord[] Place(TileFootprint footprint, CellCoord anchor, Rotation4 rotation,
            Owner owner, StageConfig stage, HashSet<CellCoord> occupied, bool requireDeploymentZone = true)
        {
            var cells = new CellCoord[footprint.CellCount];
            for (int i = 0; i < cells.Length; i++)
            {
                CellCoord cell = footprint.GetOccupiedCell(i, anchor, rotation);
                if (cell.X < 0 || cell.Y < 0 || cell.X >= stage.MapWidth || cell.Y >= stage.MapHeight ||
                    occupied.Add(cell) == false)
                    throw new InvalidOperationException("초기 배치가 보드 범위 또는 점유와 충돌합니다.");
                cells[i] = cell;
            }

            // 적은 아군 배치 구역 밖이어야 한다. 판정 원본은 DeploymentZonePolicy다.
            if (requireDeploymentZone && new DeploymentZonePolicy(stage.PlayerZone).CanDeploy(owner, cells) == false)
                throw new InvalidOperationException("초기 배치가 아군 배치 구역 규칙에 맞지 않습니다.");

            return cells;
        }
    }
}
