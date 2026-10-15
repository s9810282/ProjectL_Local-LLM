using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>초기 장애물 배치 정의다. 개체 ID는 기존 전투 준비의 할당 흐름에서 결정한다.</summary>
    public sealed class InitialObstaclePlacementSpec
    {
        public StructureSpec Definition { get; }
        public ObstacleContentKind Kind { get; }
        public CellCoord Anchor { get; }
        public Rotation4 Rotation { get; }
        public int ExplosionDamage { get; }
        public int HitAttackBonus { get; }
        public int DestructionAttackBonus { get; }
        public InitialObstaclePlacementSpec(StructureSpec definition, ObstacleContentKind kind, CellCoord anchor,
            Rotation4 rotation, int explosionDamage, int? hitAttackBonus = null, int? destructionAttackBonus = null)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (definition.Key != ObstacleContentProfile.KeyFor(kind)) throw new ArgumentException("장애물 종류와 정의 키가 다릅니다.");
            if (!Enum.IsDefined(typeof(Rotation4), rotation)) throw new ArgumentOutOfRangeException(nameof(rotation));
            if (explosionDamage < 0 || kind == ObstacleContentKind.Supplies && explosionDamage != 0)
                throw new ArgumentOutOfRangeException(nameof(explosionDamage));
            Kind = kind; Anchor = anchor; Rotation = rotation; ExplosionDamage = explosionDamage;
            ObstacleContentProfile.ValidateRewards(kind, hitAttackBonus, destructionAttackBonus);
            HitAttackBonus = hitAttackBonus ?? 0; DestructionAttackBonus = destructionAttackBonus ?? 0;
        }
    }
}
