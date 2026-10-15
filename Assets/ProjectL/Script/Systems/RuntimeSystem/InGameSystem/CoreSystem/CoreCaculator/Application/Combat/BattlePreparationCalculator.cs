using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
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
            IReadOnlyList<CharacterCombatFacts> activeCharacters)
        {
            if (initialization == null || owned == null || souls == null || draw == null || activeCharacters == null)
                throw new ArgumentNullException("전투 정의와 Buffer 사실이 필요합니다.");
            if (main == null || main.CurrentHealth <= 0)
                throw new InvalidOperationException("살아 있는 메인 지속 상태가 필요합니다.");
            if (activeCharacters.Count != 0)
                throw new InvalidOperationException("이미 연결된 전투 캐릭터가 있습니다.");

            StageConfig stage = initialization.Stage;
            if (stage.ReserveCapacity < draw.InitialCount)
                throw new InvalidOperationException("초기 손패를 담을 공간이 부족합니다.");

            foreach (UnitSpec unit in initialization.Units)
                if (unit.HasTimelineConfiguration == false)
                    throw new InvalidOperationException("전투 정의에는 Timeline 수치가 필요합니다.");

            var gimmickKeys = new HashSet<StageGimmickKey>();
            foreach (var gimmick in stage.StageGimmicks.Span)
                if (gimmickKeys.Add(gimmick.GimmickKey) == false ||
                    gimmick.GimmickKey == StageGimmickKey.Railway && stage.RailCells.Length == 0)
                    throw new InvalidOperationException("기믹이 중복되었거나 철도 셀이 없습니다.");

            var traits = TraitRegistry.CreateDefault();
            var catalog = new UnitCatalog(initialization.Units, traits, new CategoryRegistry(initialization.Categories));
            catalog.ValidateCombatReady();
            var roster = new Dictionary<CharacterInstanceId, OwnedCharacterRecord>();
            OwnedCharacterRecord mainRecord = null;
            int subCount = 0;
            foreach (OwnedCharacterRecord record in owned)
            {
                if (record == null || roster.ContainsKey(record.Id))
                    throw new InvalidOperationException("보유 목록이 올바르지 않습니다.");
                roster.Add(record.Id, record);
                UnitDefinition definition = catalog.GetRequired(record.CharacterKey);
                ValidateEligibility(definition, Owner.Player, record.Role == CharacterOwnershipRole.Main);
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
            foreach (CharacterInstanceId id in draw.Order)
                if (roster.TryGetValue(id, out var record) == false || record.Role != CharacterOwnershipRole.Sub)
                    throw new InvalidOperationException("드로우 대상은 보유 서브여야 합니다.");

            var occupied = new HashSet<CellCoord>();
            var characters = new List<CharacterCombatFacts>();
            var structures = new List<StructureCombatFacts>();
            int nextId = 1;
            UnitDefinition mainDefinition = catalog.GetRequired(mainRecord.CharacterKey);
            CellCoord[] mainCells = Place(mainDefinition.Footprint, initialization.MainAnchor,
                initialization.MainRotation, Owner.Player, stage, occupied);
            characters.Add(BattleCharacterCalculator.Create(new EntityId(nextId++), mainDefinition,
                Owner.Player, mainRecord, main, souls[mainRecord.Id], null,
                initialization.MainAnchor, initialization.MainRotation, mainCells, traits));

            foreach (InitialEnemyPlacementSpec placement in stage.InitialEnemyPlacements.Span)
            {
                UnitDefinition definition = catalog.GetRequired(placement.UnitKey);
                ValidateEligibility(definition, Owner.Enemy, false);
                CellCoord[] cells = Place(definition.Footprint, placement.Anchor, placement.Rotation,
                    Owner.Enemy, stage, occupied);
                characters.Add(BattleCharacterCalculator.Create(new EntityId(nextId++), definition,
                    Owner.Enemy, null, null, 0, null, placement.Anchor, placement.Rotation, cells, traits));
            }

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
                structures.Add(new StructureCombatFacts(id, definition, new HealthStateSnapshot(definition.MaxHealth, definition.MaxHealth),
                    placement.Anchor, placement.Rotation, cells, Owner.Neutral));
                obstacleContents.Add(new ObstacleContentProfile(id, placement.Kind, placement.ExplosionDamage,
                    placement.HitAttackBonus, placement.DestructionAttackBonus));
            }
            var bindings = obstacleContents.Count == 0 ? initialization.EnvironmentBindings : new EnvironmentSemanticBindings(initialization.EnvironmentBindings.TerrainSourceIds,
                initialization.EnvironmentBindings.ObjectSourceIds,
                initialization.EnvironmentBindings.ObstacleEntityIds.Concat(obstacleContents.Select(p => p.EntityId)).ToArray());

            // 모든 배치 검증 후 초기 지급을 계산하며 서브 상한은 드로우 정책에서 정한다.
            for (int slot = 0; slot < draw.InitialCount; slot++)
            {
                OwnedCharacterRecord record = roster[draw.Order[slot]];
                characters.Add(BattleCharacterCalculator.Create(new EntityId(nextId++),
                    catalog.GetRequired(record.CharacterKey), Owner.Player, record, null,
                    souls[record.Id], slot, null, null, Array.Empty<CellCoord>(), traits));
            }

            var preparedCharacters = BattleEnvironmentPreparation.Prepare(initialization, characters, structures, main, bindings, obstacleContents);
            return new BattlePreparation(preparedCharacters, new CombatPreparation(stage.MapWidth,
                stage.MapHeight, stage.ReserveCapacity, stage.NightLength, stage.TimelineSeed,
                CombatFlowPhase.PlayerInput, draw.Order, draw.InitialCount,
                stage.PlayerDeploymentDepth, stage.EnemyDeploymentDepth,
                stage.RailCells.ToArray(), stage.StageGimmicks.ToArray(), initialization.Economy?.ManualDrawGoldCost,
                initialization.Environment, bindings, obstacleContents,
                initialStageGold: initialization.Economy?.InitialGold ?? 0, wallBlocking: initialization.WallBlocking), structures, nextId);
        }

        private static void ValidateEligibility(UnitDefinition definition, Owner owner, bool isMain)
        {
            if (definition.Role != (isMain ? UnitRole.MainCharacter : UnitRole.Regular) ||
                owner == Owner.Player && definition.OwnerEligibility == UnitOwnerEligibility.EnemyOnly ||
                owner == Owner.Enemy && definition.OwnerEligibility == UnitOwnerEligibility.PlayerOnly)
                throw new InvalidOperationException("캐릭터 정의의 역할 또는 소유 권한이 맞지 않습니다.");
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

            if (requireDeploymentZone && new DeploymentZonePolicy(stage.PlayerDeploymentDepth, stage.EnemyDeploymentDepth,
                stage.MapHeight).CanDeploy(owner, cells) == false)
                throw new InvalidOperationException("초기 배치가 소유자의 배치 구역을 벗어났습니다.");

            return cells;
        }
    }
}
