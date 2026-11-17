using System;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Obstacle.Item
{
    /// <summary>장애물의 필요 타수·점유·반응 설정입니다. 구조물 키는 종류에서 도출합니다.</summary>
    public sealed class ObstacleDefinitionData : IData
    {
        public ObstacleContentKind Kind { get; }
        public EntityKey Key => ObstacleContentProfile.KeyFor(Kind);
        public int FootprintWidth { get; }
        public int FootprintHeight { get; }
        public int RequiredHits { get; }
        public int ExplosionDamage { get; }
        public int HitAttackBonus { get; }
        public int DestructionAttackBonus { get; }
        public int FallLength { get; }

        public ObstacleDefinitionData(ObstacleContentKind kind, int footprintWidth, int footprintHeight,
            int requiredHits, int explosionDamage = 0, int hitAttackBonus = 0, int destructionAttackBonus = 0,
            int fallLength = 3)
        {
            Kind = kind;
            FootprintWidth = footprintWidth;
            FootprintHeight = footprintHeight;
            RequiredHits = requiredHits;
            ExplosionDamage = explosionDamage;
            HitAttackBonus = hitAttackBonus;
            DestructionAttackBonus = destructionAttackBonus;
            FallLength = fallLength;
            Validate();
        }

        public void Validate()
        {
            if (!System.Enum.IsDefined(typeof(ObstacleContentKind), Kind))
            {
                throw new InvalidOperationException("지원되지 않는 장애물 종류입니다.");
            }

            if (FootprintWidth < 1 || FootprintHeight < 1 || RequiredHits < 1)
            {
                throw new InvalidOperationException("장애물 점유 크기와 필요 타수는 1 이상이어야 합니다.");
            }

            if (ExplosionDamage < 0 || HitAttackBonus < 0 || DestructionAttackBonus < 0)
            {
                throw new InvalidOperationException("장애물 피해와 보상은 음수일 수 없습니다.");
            }

            if (Kind == ObstacleContentKind.PowderBarrel && (HitAttackBonus != 0 || DestructionAttackBonus != 0)
                || Kind == ObstacleContentKind.Supplies && ExplosionDamage != 0
                || Kind == ObstacleContentKind.Pillar && (ExplosionDamage != 0 || HitAttackBonus != 0 || DestructionAttackBonus != 0))
            {
                throw new InvalidOperationException("화약통은 폭발 피해만, 보급품은 피격·파괴 보상만 설정할 수 있습니다.");
            }
            if (FallLength < 1)
                throw new InvalidOperationException("기둥의 낙하 길이는 1 이상이어야 합니다.");
        }

        /// <summary>순수 규칙과 스테이지 좌표로 기존 Core 초기 장애물 계약을 만듭니다.</summary>
        public InitialObstaclePlacementSpec CreatePlacement(CellCoord anchor, Rotation4 rotation)
        {
            var cells = new CellCoord[checked(FootprintWidth * FootprintHeight)];
            for (int y = 0; y < FootprintHeight; y++)
            {
                for (int x = 0; x < FootprintWidth; x++)
                {
                    cells[y * FootprintWidth + x] = new CellCoord(x, y);
                }
            }

            var definition = new StructureSpec(Key, FootprintWidth, FootprintHeight, cells, 1, false, 0);
            return new InitialObstaclePlacementSpec(definition, Kind, anchor, rotation, RequiredHits, ExplosionDamage,
                hitAttackBonus: HitAttackBonus, destructionAttackBonus: DestructionAttackBonus, fallLength: FallLength);
        }
    }
}
