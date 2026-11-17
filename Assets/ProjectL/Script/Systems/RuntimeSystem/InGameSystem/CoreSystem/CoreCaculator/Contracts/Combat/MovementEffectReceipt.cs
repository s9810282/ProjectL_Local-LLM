using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>플레이어 이동의 출처입니다. 자동 이동에 가짜 취소 토큰을 만들지 않습니다.</summary>
    public readonly struct MovementEffectId : IEquatable<MovementEffectId>
    {
        public long BattleInstanceId { get; }
        public long ControllerTurnSequence { get; }
        public EntityId ActorId { get; }
        public int MoveCommandNumber { get; }
        public bool IsValid => BattleInstanceId > 0 && ControllerTurnSequence > 0 && !ActorId.IsEmpty && MoveCommandNumber > 0;
        public MovementEffectId(long battleInstanceId, long controllerTurnSequence, EntityId actorId, int moveCommandNumber)
        {
            if (battleInstanceId < 1 || controllerTurnSequence < 1 || actorId.IsEmpty || moveCommandNumber < 1)
                throw new ArgumentException("이동 효과에는 유효한 전투·턴·행위자·명령 식별자가 필요합니다.");
            BattleInstanceId = battleInstanceId;
            ControllerTurnSequence = controllerTurnSequence;
            ActorId = actorId;
            MoveCommandNumber = moveCommandNumber;
        }
        public bool Equals(MovementEffectId other) => BattleInstanceId == other.BattleInstanceId &&
            ControllerTurnSequence == other.ControllerTurnSequence && ActorId == other.ActorId && MoveCommandNumber == other.MoveCommandNumber;
        public override bool Equals(object obj) => obj is MovementEffectId other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(BattleInstanceId, ControllerTurnSequence, ActorId, MoveCommandNumber);
        public static bool operator ==(MovementEffectId left, MovementEffectId right) => left.Equals(right);
        public static bool operator !=(MovementEffectId left, MovementEffectId right) => !left.Equals(right);
    }

    /// <summary>참여자별 배치 복원값입니다. 동승자는 행동이나 누적 거리를 환급받지 않습니다.</summary>
    public sealed class MovementParticipantRestoration
    {
        public EntityId EntityId { get; }
        public CellCoord From { get; }
        public CellCoord To { get; }
        public Rotation4 Rotation { get; }
        public bool RestoresActionUse { get; }
        public int DistanceReverted { get; }
        public MovementParticipantRestoration(EntityId entityId, CellCoord from, CellCoord to, Rotation4 rotation,
            bool restoresActionUse, int distanceReverted)
        {
            if (entityId.IsEmpty || !Enum.IsDefined(typeof(Rotation4), rotation) || distanceReverted < 0 ||
                !restoresActionUse && distanceReverted != 0)
                throw new ArgumentException("참여자 배치·회전·행동·거리 복원값이 잘못되었습니다.");
            EntityId = entityId;
            From = from;
            To = to;
            Rotation = rotation;
            RestoresActionUse = restoresActionUse;
            DistanceReverted = distanceReverted;
        }
        public bool SameValues(MovementParticipantRestoration other) => other != null && EntityId == other.EntityId &&
            From == other.From && To == other.To && Rotation == other.Rotation &&
            RestoresActionUse == other.RestoresActionUse && DistanceReverted == other.DistanceReverted;
    }

    /// <summary>전체 사실은 변경 감지에만 쓰고 복원은 배치·행동·거리의 제한된 값으로 수행합니다.</summary>
    public sealed class MovementUndoParticipant
    {
        public MovementParticipantRestoration Restoration { get; }
        public MovementUndoOriginFacts OriginFacts { get; }
        public CharacterCombatFacts ExpectedAfterMove { get; }
        public EntityId EntityId => ExpectedAfterMove.EntityId;
        public MovementUndoParticipant(CharacterCombatFacts expectedAfterMove, MovementUndoOriginFacts originFacts,
            Rotation4 rotation, bool restoresActionUse = false, int recordedDistance = 0)
        {
            if (expectedAfterMove == null) throw new ArgumentNullException(nameof(expectedAfterMove));
            if (originFacts == null) throw new ArgumentNullException(nameof(originFacts));
            if (expectedAfterMove.Owner != Owner.Player || expectedAfterMove.Area != CharacterRuntimeArea.Field ||
                expectedAfterMove.Health.IsDead || !expectedAfterMove.Anchor.HasValue || !expectedAfterMove.Rotation.HasValue ||
                expectedAfterMove.OccupiedCells.Count != 1 || expectedAfterMove.OccupiedCells[0] != expectedAfterMove.Anchor.Value ||
                recordedDistance > expectedAfterMove.StageRecord.DistanceMoved ||
                restoresActionUse && expectedAfterMove.ActionUse != UnitActionUse.Moved ||
                !restoresActionUse && rotation != expectedAfterMove.Rotation.Value)
                throw new ArgumentException("참여자는 살아 있는 플레이어 단일 칸 개체이며 복원 권한과 일치해야 합니다.");
            Restoration = new MovementParticipantRestoration(expectedAfterMove.EntityId, expectedAfterMove.Anchor.Value,
                originFacts.Cell, rotation, restoresActionUse, recordedDistance);
            OriginFacts = originFacts;
            ExpectedAfterMove = expectedAfterMove;
        }
        public bool SameValues(MovementUndoParticipant other) => other != null && Restoration.SameValues(other.Restoration) &&
            OriginFacts.SameValues(other.OriginFacts) && ExpectedAfterMove.SameValues(other.ExpectedAfterMove);
    }

    /// <summary>효과 소유자의 제한된 역산 재료입니다. 외부 상속으로 미지원 역산을 추가할 수 없습니다.</summary>
    public abstract class MovementEffectReceipt
    {
        public MovementEffectId Movement { get; }
        public EffectOriginKey Origin { get; }
        public EntityId RecipientId { get; }
        private protected MovementEffectReceipt(MovementEffectId movement, EffectOriginKey origin, EntityId recipientId,
            EffectOwnerKind sourceKind = EffectOwnerKind.Trait)
        {
            if (!movement.IsValid || !origin.IsValid || origin.Kind != sourceKind || origin.OwnerInstanceId < 1 || recipientId.IsEmpty)
                throw new ArgumentException("이동 역산에는 유효한 이동·출처·수신자가 필요합니다.");
            Movement = movement;
            Origin = origin;
            RecipientId = recipientId;
        }
        public virtual bool SameContribution(MovementEffectReceipt other) => other != null && GetType() == other.GetType() &&
            Movement == other.Movement && Origin.Kind == other.Origin.Kind && Origin.OwnerInstanceId == other.Origin.OwnerInstanceId &&
            Origin.DefinitionKey == other.Origin.DefinitionKey && RecipientId == other.RecipientId;
        public abstract bool SameValues(MovementEffectReceipt other);

        /// <summary>역산 대상 기여분의 현재 값만 검사합니다. 수신자의 다른 체력·아이템·상태는 검사하지 않습니다.</summary>
        public bool MatchesGuard(CharacterCombatFacts current)
        {
            if (current == null) return this is MovementExposureReceipt;
            if (current.EntityId != RecipientId) return false;
            return this switch
            {
                MovementAccelerationReceipt gain => current.Stats.TraitStatuses.ToArray().OfType<AccelerationTraitStatusSnapshot>()
                    .SingleOrDefault()?.HeldAttackBonus == gain.ExpectedHeldAmount,
                MovementStationaryReceipt flag => current.Stats.TraitStatuses.ToArray().OfType<StationaryFireTraitStatusSnapshot>()
                    .SingleOrDefault()?.MovedThisOwnTurn == flag.ExpectedMoved,
                MovementShieldReceipt shield => SameStatus(current.StatusEffects.Entries.OfType<ShieldStatus>()
                    .SingleOrDefault(s => s.Origin == Origin), shield.ExpectedStatus) &&
                    (!shield.ExpectedDefenseBonus.HasValue || current.TraitEffects.CohesionDefenseBonus == shield.ExpectedDefenseBonus.Value),
                MovementExposureReceipt exposure => current.StatusEffects.Entries.OfType<WeaknessExposureStatus>()
                    .Where(s => s.Movement == Movement && s.Origin.Value.Kind == Origin.Kind &&
                        s.Origin.Value.OwnerInstanceId == Origin.OwnerInstanceId && s.Origin.Value.DefinitionKey == Origin.DefinitionKey)
                    .All(s => s.SameValues(exposure.ExpectedStatus)),
                MovementTrailReceipt => true,
                MovementFireReceipt fire => fire.ExpectedStatus.SameValues(current.StatusEffects.Entries.OfType<FireStatus>().SingleOrDefault()),
                _ => false
            };
        }

        private static bool SameStatus(AppliedStatus a, AppliedStatus b) => a == null ? b == null : a.SameValues(b);
        private protected void RequireProducer(TraitKey key)
        {
            if (Origin.OwnerInstanceId != Movement.ActorId.Value || Origin.DefinitionKey != (int)key)
                throw new ArgumentException("이동 효과의 출처는 해당 이동 행위자의 특성이어야 합니다.");
        }
    }

    /// <summary>
    /// 이동의 기믹 도착 효과가 바꾼 화상 하나다(예: 운석 불길 진입). 어떤 기믹의 도착 효과인지는
    /// 그 기믹의 도착 규칙이 판단해 만들며, 영수증은 기믹 출처와 화상 부여분만 보장한다.
    /// </summary>
    public sealed class MovementFireReceipt : MovementEffectReceipt
    {
        public FireStatus PreviousStatus { get; }
        public FireStatus ExpectedStatus { get; }

        internal MovementFireReceipt(MovementEffectId movement, EntityId recipientId, FireStatus previousStatus, FireStatus expectedStatus)
            : base(movement, (expectedStatus ?? throw new ArgumentNullException(nameof(expectedStatus))).Origin.Value,
                recipientId, EffectOwnerKind.StageGimmick)
        {
            if (previousStatus != null && (previousStatus.SameValues(expectedStatus) ||
                    previousStatus.RemainingAffectedSideStarts > expectedStatus.RemainingAffectedSideStarts))
                throw new ArgumentException("화상 역산에는 이동으로 달라진 기믹 도착 효과의 부여분이 필요합니다.");
            PreviousStatus = previousStatus;
            ExpectedStatus = expectedStatus;
        }

        public override bool SameValues(MovementEffectReceipt other) => other is MovementFireReceipt fire &&
            SameContribution(fire) && ExpectedStatus.SameValues(fire.ExpectedStatus) &&
            (PreviousStatus == null ? fire.PreviousStatus == null : PreviousStatus.SameValues(fire.PreviousStatus));
    }

    public sealed class MovementExposureReceipt : MovementEffectReceipt
    {
        public WeaknessExposureStatus ExpectedStatus { get; }
        public MovementExposureReceipt(MovementEffectId movement, EntityId recipientId, WeaknessExposureStatus expectedStatus)
            : base(movement, (expectedStatus ?? throw new ArgumentNullException(nameof(expectedStatus))).Origin.Value, recipientId)
        {
            RequireProducer(TraitKey.ReconSquad);
            if (expectedStatus.Movement != movement)
                throw new ArgumentException("노출 영수증과 실제 기여분의 이동 출처가 다릅니다.", nameof(expectedStatus));
            ExpectedStatus = expectedStatus;
        }
        public override bool SameValues(MovementEffectReceipt other) => SameContribution(other) && ExpectedStatus.SameValues(((MovementExposureReceipt)other).ExpectedStatus);
    }

    public sealed class MovementAccelerationReceipt : MovementEffectReceipt
    {
        public int AddedAmount { get; }
        public int ExpectedHeldAmount { get; }
        public MovementAccelerationReceipt(MovementEffectId movement, EffectOriginKey origin, int addedAmount, int expectedHeldAmount)
            : base(movement, origin, movement.ActorId)
        {
            RequireProducer(TraitKey.Acceleration);
            if (addedAmount < 1 || expectedHeldAmount < addedAmount) throw new ArgumentOutOfRangeException(nameof(addedAmount));
            AddedAmount = addedAmount;
            ExpectedHeldAmount = expectedHeldAmount;
        }
        public override bool SameValues(MovementEffectReceipt other) => SameContribution(other) && Origin == other.Origin &&
            AddedAmount == ((MovementAccelerationReceipt)other).AddedAmount && ExpectedHeldAmount == ((MovementAccelerationReceipt)other).ExpectedHeldAmount;
    }

    public sealed class MovementTrailReceipt : MovementEffectReceipt
    {
        public TraitTrailFact CreatedSource { get; }
        public MovementTrailReceipt(MovementEffectId movement, TraitTrailFact createdSource)
            : base(movement, (createdSource ?? throw new ArgumentNullException(nameof(createdSource))).Origin, movement.ActorId)
        {
            RequireProducer(createdSource.Key);
            if (!createdSource.Active || createdSource.CreatingSide != Owner.Player)
                throw new ArgumentException("취소할 흔적은 플레이어가 생성한 활성 출처여야 합니다.", nameof(createdSource));
            CreatedSource = createdSource;
        }
        public override bool SameContribution(MovementEffectReceipt other) => base.SameContribution(other) && CreatedSource.Id == ((MovementTrailReceipt)other).CreatedSource.Id;
        public override bool SameValues(MovementEffectReceipt other) => SameContribution(other) && CreatedSource.SameValues(((MovementTrailReceipt)other).CreatedSource);
    }

    public sealed class MovementStationaryReceipt : MovementEffectReceipt
    {
        public bool PreviousMoved { get; }
        public bool ExpectedMoved { get; }
        public MovementStationaryReceipt(MovementEffectId movement, EffectOriginKey origin, bool previousMoved, bool expectedMoved)
            : base(movement, origin, movement.ActorId)
        {
            RequireProducer(TraitKey.StationaryFire);
            if (previousMoved || !expectedMoved) throw new ArgumentException("이동으로 새로 설정한 플래그만 복원할 수 있습니다.");
            PreviousMoved = previousMoved;
            ExpectedMoved = expectedMoved;
        }
        public override bool SameValues(MovementEffectReceipt other) => SameContribution(other) && Origin == other.Origin;
    }

    public enum MovementEffectUseKind
    {
        ParticipantAction = 1,
        ExposureDamage = 2,
        AccelerationDamage = 3,
        GroundDeparture = 4,
        DustAttackRange = 5,
        DustDefense = 6,
        PassiveShield = 7
    }

    /// <summary>현재 제안의 실제 실행 사건에 묶인 사용 근거입니다. 조회나 별도 투영의 사건은 게시할 수 없습니다.</summary>
    public sealed class MovementEffectUse
    {
        public MovementEffectId? Movement { get; }
        public EffectOriginKey? Source { get; }
        public EntityId ConsumerId { get; }
        public MovementEffectUseKind Kind { get; }
        public CombatEvent Execution { get; }
        public CombatDamageOrigin DamageOrigin { get; }
        public long? SourceId { get; }
        public CharacterCombatFacts ConsumerBefore { get; }
        public int? NumericInput { get; }
        public CombatDamageKind? DamageKind { get; }
        public int CapturedAmount { get; }

        public MovementEffectUse(MovementEffectId? movement, EffectOriginKey? source, EntityId consumerId,
            MovementEffectUseKind kind, CombatEvent execution, CombatDamageOrigin damageOrigin = null, long? sourceId = null,
            CharacterCombatFacts consumerBefore = null, int? numericInput = null, CombatDamageKind? damageKind = null, int capturedAmount = 0)
        {
            if (consumerId.IsEmpty || !Enum.IsDefined(typeof(MovementEffectUseKind), kind) ||
                movement.HasValue && !movement.Value.IsValid || source.HasValue && !source.Value.IsValid || sourceId.HasValue && sourceId.Value < 1 ||
                kind == MovementEffectUseKind.ParticipantAction && (!movement.HasValue || source.HasValue || sourceId.HasValue || damageOrigin != null) ||
                kind != MovementEffectUseKind.ParticipantAction && !source.HasValue)
                throw new ArgumentException("이동 효과 사용의 출처·수신자·종류가 일치해야 합니다.");
            if (consumerBefore != null && consumerBefore.EntityId != consumerId || numericInput < 0 || capturedAmount < 0 ||
                damageKind.HasValue && !Enum.IsDefined(typeof(CombatDamageKind), damageKind.Value))
                throw new ArgumentException("사용한 수신자와 수치 계산 원본이 유효해야 합니다.");
            ConsumerBefore = consumerBefore;
            NumericInput = numericInput;
            DamageKind = damageKind;
            CapturedAmount = capturedAmount;
            Movement = movement;
            Source = source;
            ConsumerId = consumerId;
            Kind = kind;
            Execution = execution ?? throw new ArgumentNullException(nameof(execution));
            DamageOrigin = damageOrigin;
            SourceId = sourceId;
        }

        public bool SameValues(MovementEffectUse other) => other != null && Movement == other.Movement && Source == other.Source &&
            ConsumerId == other.ConsumerId && Kind == other.Kind && ReferenceEquals(Execution, other.Execution) && SourceId == other.SourceId &&
            NumericInput == other.NumericInput && DamageKind == other.DamageKind && CapturedAmount == other.CapturedAmount &&
            (ConsumerBefore == null ? other.ConsumerBefore == null : ConsumerBefore.SameValues(other.ConsumerBefore)) &&
            (DamageOrigin == null ? other.DamageOrigin == null : other.DamageOrigin != null &&
                DamageOrigin.PhysicalCell == other.DamageOrigin.PhysicalCell && DamageOrigin.CreditedCharacterId == other.DamageOrigin.CreditedCharacterId &&
                DamageOrigin.AttackGroup == other.DamageOrigin.AttackGroup);
    }

    public sealed class MovementShieldReceipt : MovementEffectReceipt
    {
        public ShieldStatus PreviousStatus { get; }
        public ShieldStatus ExpectedStatus { get; }
        public int? PreviousDefenseBonus { get; }
        public int? ExpectedDefenseBonus { get; }
        public MovementShieldReceipt(MovementEffectId movement, EntityId recipientId, EffectOriginKey origin,
            ShieldStatus previousStatus, ShieldStatus expectedStatus, int? previousDefenseBonus = null, int? expectedDefenseBonus = null) : base(movement, origin, recipientId)
        {
            if (origin.DefinitionKey != (int)TraitKey.Cohesion && origin.DefinitionKey != (int)TraitKey.SoloAction &&
                origin.DefinitionKey != (int)TraitKey.PositionSwap ||
                previousStatus != null && previousStatus.Origin != origin || expectedStatus != null && expectedStatus.Origin != origin ||
                previousDefenseBonus.HasValue != expectedDefenseBonus.HasValue || previousDefenseBonus < 0 || expectedDefenseBonus < 0 ||
                previousDefenseBonus.HasValue && (origin.DefinitionKey != (int)TraitKey.Cohesion || origin.OwnerInstanceId != recipientId.Value ||
                    previousDefenseBonus == expectedDefenseBonus) ||
                !previousDefenseBonus.HasValue && (previousStatus == null ? expectedStatus == null : previousStatus.SameValues(expectedStatus)))
                throw new ArgumentException("패시브 역산에는 같은 출처의 변경된 보호막 예산·자격만 넣을 수 있습니다.");
            if (origin.DefinitionKey == (int)TraitKey.PositionSwap)
            {
                RequireProducer(TraitKey.PositionSwap);
                if (expectedStatus?.ExpiresAtSideTurnStart != Owner.Player || !expectedStatus.Eligible ||
                    expectedStatus.RemainingBudget <= (previousStatus?.RemainingBudget ?? 0))
                    throw new ArgumentException("교대 실드 영수증은 해당 이동의 지급분이어야 합니다.");
            }
            PreviousStatus = previousStatus;
            ExpectedStatus = expectedStatus;
            PreviousDefenseBonus = previousDefenseBonus;
            ExpectedDefenseBonus = expectedDefenseBonus;
        }
        public override bool SameValues(MovementEffectReceipt other) => SameContribution(other) && Origin == other.Origin &&
            PreviousDefenseBonus == ((MovementShieldReceipt)other).PreviousDefenseBonus && ExpectedDefenseBonus == ((MovementShieldReceipt)other).ExpectedDefenseBonus &&
            (PreviousStatus == null ? ((MovementShieldReceipt)other).PreviousStatus == null : PreviousStatus.SameValues(((MovementShieldReceipt)other).PreviousStatus)) &&
            (ExpectedStatus == null ? ((MovementShieldReceipt)other).ExpectedStatus == null : ExpectedStatus.SameValues(((MovementShieldReceipt)other).ExpectedStatus));
    }

}
