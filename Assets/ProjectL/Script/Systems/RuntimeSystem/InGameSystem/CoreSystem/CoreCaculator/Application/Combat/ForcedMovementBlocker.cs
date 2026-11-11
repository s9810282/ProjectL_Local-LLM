using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>
    /// 유닛 강제 이동이 다음 칸에 들어가지 못한 이유다. <see cref="AttackForcedMovement.FindBlocker"/>만 만든다.
    /// 진입 허용(<see cref="AttackForcedMovement.CanEnterUnit"/>)과 넉백 부딪힘 종류가 같은 판정을 읽는다.
    /// </summary>
    internal readonly struct ForcedMovementBlocker
    {
        internal static ForcedMovementBlocker None => default;
        internal static ForcedMovementBlocker Wall => new(AttackKnockbackCollisionKind.Wall, null);
        internal static ForcedMovementBlocker Structure(EntityId id) => new(AttackKnockbackCollisionKind.Structure, id);
        internal static ForcedMovementBlocker Character(EntityId id) => new(AttackKnockbackCollisionKind.Character, id);

        /// <summary>막은 것이 없으면 0(정의되지 않은 값)이다. <see cref="IsNone"/>으로 묻는다.</summary>
        internal AttackKnockbackCollisionKind Kind { get; }
        /// <summary>구조물·캐릭터면 그 개체다. 벽이면 비어 있다.</summary>
        internal EntityId? EntityId { get; }
        internal bool IsNone => Kind == 0;

        private ForcedMovementBlocker(AttackKnockbackCollisionKind kind, EntityId? entityId)
        {
            Kind = kind;
            EntityId = entityId;
        }
    }
}
