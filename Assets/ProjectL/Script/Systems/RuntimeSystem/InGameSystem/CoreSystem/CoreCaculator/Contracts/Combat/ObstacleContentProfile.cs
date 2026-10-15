using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public enum ObstacleContentKind { PowderBarrel = 1, Supplies }
    public enum ObstacleReactionKind { None, Hit, Destroyed }

    /// <summary>장애물 정의를 기존 구조물 ID에 연결한다. 체력과 점유 위치는 구조물 사실만 소유한다.</summary>
    public sealed class ObstacleContentProfile
    {
        public EntityId EntityId { get; }
        public ObstacleContentKind Kind { get; }
        public int ExplosionDamage { get; }
        public int HitAttackBonus { get; }
        public int DestructionAttackBonus { get; }
        public ObstacleContentProfile(EntityId entityId, ObstacleContentKind kind, int explosionDamage,
            int? hitAttackBonus = null, int? destructionAttackBonus = null)
        {
            if (entityId.IsEmpty) throw new ArgumentException("장애물 ID가 필요합니다.", nameof(entityId));
            if (!Enum.IsDefined(typeof(ObstacleContentKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (explosionDamage < 0 || kind == ObstacleContentKind.Supplies && explosionDamage != 0)
                throw new ArgumentOutOfRangeException(nameof(explosionDamage));
            EntityId = entityId; Kind = kind; ExplosionDamage = explosionDamage;
            ValidateRewards(kind, hitAttackBonus, destructionAttackBonus);
            HitAttackBonus = hitAttackBonus ?? 0; DestructionAttackBonus = destructionAttackBonus ?? 0;
        }
        internal static void ValidateRewards(ObstacleContentKind kind, int? hit, int? destruction)
        {
            if (hit < 0 || destruction < 0 || kind == ObstacleContentKind.Supplies && (!hit.HasValue || !destruction.HasValue) ||
                kind == ObstacleContentKind.PowderBarrel && (hit.GetValueOrDefault() != 0 || destruction.GetValueOrDefault() != 0))
                throw new ArgumentException("보급품은 명시적인 비음수 보상 두 값이 필요하고 화약통은 보상이 없습니다.");
        }
        public bool SameValues(ObstacleContentProfile other) => other != null && EntityId == other.EntityId &&
            Kind == other.Kind && ExplosionDamage == other.ExplosionDamage && HitAttackBonus == other.HitAttackBonus &&
            DestructionAttackBonus == other.DestructionAttackBonus;
        public static bool SameValues(IReadOnlyList<ObstacleContentProfile> a, IReadOnlyList<ObstacleContentProfile> b) =>
            a.Count == b.Count && a.OrderBy(p => p.EntityId).Zip(b.OrderBy(p => p.EntityId), (x, y) => x.SameValues(y)).All(v => v);
        public static void Validate(IReadOnlyList<ObstacleContentProfile> profiles, EnvironmentSemanticBindings bindings,
            IReadOnlyList<StructureCombatFacts> structures)
        {
            if (profiles.Any(p => p == null) || profiles.Select(p => p.EntityId).Distinct().Count() != profiles.Count ||
                !new HashSet<EntityId>(profiles.Select(p => p.EntityId)).SetEquals(bindings.ObstacleEntityIds))
                throw new ArgumentException("장애물 프로필과 분류는 중복 없이 일치해야 합니다.");
            foreach (var profile in profiles)
            {
                var structure = structures.SingleOrDefault(s => s.EntityId == profile.EntityId);
                if (structure == null || structure.Definition.Key != KeyFor(profile.Kind))
                    throw new ArgumentException("장애물 프로필은 일치하는 기존 구조물에 연결되어야 합니다.");
            }
            if (structures.Any(s => IsNativeObstacleKey(s.Definition.Key) &&
                !profiles.Any(p => p.EntityId == s.EntityId)))
                throw new ArgumentException("지원되는 장애물 정의는 실행 프로필 없이 게시할 수 없습니다.");
        }
        public static bool IsNativeObstacleKey(EntityKey key) =>
            key == EntityKey.Structure_PowderBarrel || key == EntityKey.Structure_Supplies;

        public static EntityKey KeyFor(ObstacleContentKind kind) => kind switch {
            ObstacleContentKind.PowderBarrel => EntityKey.Structure_PowderBarrel,
            ObstacleContentKind.Supplies => EntityKey.Structure_Supplies,
            _ => throw new ArgumentOutOfRangeException(nameof(kind)) };
    }
}
