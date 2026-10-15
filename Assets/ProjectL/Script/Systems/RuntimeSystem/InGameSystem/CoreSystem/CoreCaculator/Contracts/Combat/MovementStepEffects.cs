using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>선택된 타일의 요청값이다. 실제 변화·장벽·중단 여부는 기존 Core 계산 결과로 판정한다.</summary>
    public sealed class MovementStepEffects
    {
        public IReadOnlyList<MovementHealthRequest> Damage { get; }
        public IReadOnlyList<MovementHealthRequest> Healing { get; }
        public IReadOnlyList<MovementForcedMoveRequest> ForcedMovement { get; }
        public IReadOnlyList<MovementMethodChange> MovementMethods { get; }
        public MovementTileFact ExpectedSource { get; }
        public MovementTileFact FinalSource { get; }
        /// <summary>규칙이 판단한 보유 효과 변경이다. 조정자는 콘텐츠 종류를 모른 채 기대값을 확인하고 적용한다.</summary>
        public IReadOnlyList<MovementHeldChange> HeldChanges { get; }

        public MovementStepEffects(IReadOnlyList<MovementHealthRequest> damage = null,
            IReadOnlyList<MovementHealthRequest> healing = null,
            IReadOnlyList<MovementForcedMoveRequest> forcedMovement = null,
            MovementTileFact expectedSource = null, MovementTileFact finalSource = null,
            IReadOnlyList<MovementMethodChange> movementMethods = null,
            IReadOnlyList<MovementHeldChange> heldChanges = null)
        {
            if ((expectedSource == null) != (finalSource == null))
                throw new ArgumentException("원본 변경에는 기대값과 최종값이 함께 필요합니다.");
            if (expectedSource != null && (expectedSource.Id != finalSource.Id ||
                expectedSource.Cell != finalSource.Cell || expectedSource.RuleKey != finalSource.RuleKey ||
                expectedSource.GetType() != finalSource.GetType()))
                throw new ArgumentException("진입 규칙은 선택된 원본의 식별자를 바꿀 수 없습니다.");
            Damage = Copy(damage);
            Healing = Copy(healing);
            ForcedMovement = Copy(forcedMovement);
            MovementMethods = Copy(movementMethods);
            HeldChanges = Copy(heldChanges);
            ExpectedSource = expectedSource;
            FinalSource = finalSource;
        }

        private static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> values) where T : class
        {
            var copy = (values ?? Array.Empty<T>()).ToArray();
            if (copy.Any(value => value == null)) throw new ArgumentException("효과 요청은 null일 수 없습니다.");
            return Array.AsReadOnly(copy);
        }
    }

    /// <summary>기존 이동 방식 필드만 바꾸는 기대값·최종값이다. 전체 능력치를 덮어쓸 수 없다.</summary>
    public sealed class MovementMethodChange
    {
        public EntityId TargetId { get; }
        public MoveMethod Expected { get; }
        public MoveMethod Final { get; }
        public MovementMethodChange(EntityId targetId, MoveMethod expected, MoveMethod final)
        {
            if (targetId.IsEmpty || !Enum.IsDefined(typeof(MoveMethod), expected) ||
                !Enum.IsDefined(typeof(MoveMethod), final)) throw new ArgumentException("유효한 이동 방식 변경이 필요합니다.");
            TargetId = targetId; Expected = expected; Final = final;
        }
    }

    /// <summary>보유 효과의 기대값·최종값이다. 위치·체력·능력치는 바꾸지 않는다.</summary>
    public sealed class MovementHeldChange
    {
        public EntityId TargetId { get; }
        public CharacterHeldEnvironmentState Expected { get; }
        public CharacterHeldEnvironmentState Final { get; }
        public MovementHeldChange(EntityId targetId, CharacterHeldEnvironmentState expected, CharacterHeldEnvironmentState final)
        {
            if (targetId.IsEmpty) throw new ArgumentException("보유 효과 변경 대상 ID가 필요합니다.", nameof(targetId));
            TargetId = targetId;
            Expected = expected ?? throw new ArgumentNullException(nameof(expected));
            Final = final ?? throw new ArgumentNullException(nameof(final));
        }
    }

    public sealed class MovementHealthRequest
    {
        public EntityId TargetId { get; }
        public int Amount { get; }
        public CombatDamageOrigin DamageOrigin { get; }

        public MovementHealthRequest(EntityId targetId, int amount, CombatDamageOrigin damageOrigin = null)
        {
            if (targetId.IsEmpty) throw new ArgumentException("효과 대상 ID가 필요합니다.", nameof(targetId));
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            TargetId = targetId;
            Amount = amount;
            DamageOrigin = damageOrigin;
        }
    }

    public enum MovementDisplacementCause { Unspecified, Knockback }

    public sealed class MovementForcedMoveRequest
    {
        public EntityId TargetId { get; }
        public Rotation4 Direction { get; }
        public int Distance { get; }
        public MovementDisplacementCause Cause { get; }

        public MovementForcedMoveRequest(EntityId targetId, Rotation4 direction, int distance,
            MovementDisplacementCause cause = MovementDisplacementCause.Unspecified)
        {
            if (targetId.IsEmpty) throw new ArgumentException("효과 대상 ID가 필요합니다.", nameof(targetId));
            if (!Enum.IsDefined(typeof(Rotation4), direction)) throw new ArgumentOutOfRangeException(nameof(direction));
            if (distance < 0) throw new ArgumentOutOfRangeException(nameof(distance));
            if (!Enum.IsDefined(typeof(MovementDisplacementCause), cause)) throw new ArgumentOutOfRangeException(nameof(cause));
            TargetId = targetId;
            Direction = direction;
            Distance = distance;
            Cause = cause;
        }
    }

    /// <summary>조정자가 실제 계산 결과에서 얻는 사건 근거이며 타일 규칙의 반환값이 아니다.</summary>
    public enum MovementEffectCause
    {
        Damage, Healing, Death, ObjectConsumed, ObjectAcquired, GimmickChanged,
        PositionModifier, HealthNormalized, MovementTrait
    }
}
