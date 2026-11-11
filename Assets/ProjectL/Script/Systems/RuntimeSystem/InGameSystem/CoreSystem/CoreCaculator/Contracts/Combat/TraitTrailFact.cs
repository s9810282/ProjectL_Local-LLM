using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>기존 환경 ID를 사용하는 특성 흔적입니다. 지형 원본이나 셀의 상태를 덮어쓰지 않습니다.</summary>
    public sealed class TraitTrailFact : MovementTileFact
    {
        public EffectOriginKey Origin { get; }
        public Owner CreatingSide { get; }
        public TraitKey Key => (TraitKey)Origin.DefinitionKey;
        public int MovementRangeBonus { get; }
        public int AttackRangeReduction { get; }
        public int AttackDamageReductionPercent { get; }
        public bool ExemptsAlliedAttackRangePenalty { get; }
        public int RemainingTurnEnds { get; }
        // 이전 조회 계약의 이름을 유지한다. 감소할 진영은 흔적 종류가 결정한다.
        public int RemainingOpposingTurnEnds => RemainingTurnEnds;
        public long CreationControllerTurnSequence { get; }
        public override bool TriggersArrivalRule => false;

        public TraitTrailFact(long id, CellCoord cell, EffectOriginKey origin, Owner creatingSide, int movementRangeBonus,
            int attackRangeReduction, int attackDamageReductionPercent, int remainingOpposingTurnEnds, bool active, IReadOnlyList<Owner> knownTo)
            : this(id, cell, origin, creatingSide, movementRangeBonus, attackRangeReduction, attackDamageReductionPercent,
                remainingOpposingTurnEnds, active, knownTo, 0)
        {
        }

        public TraitTrailFact(long id, CellCoord cell, EffectOriginKey origin, Owner creatingSide, int movementRangeBonus,
            int attackRangeReduction, int attackDamageReductionPercent, int remainingOpposingTurnEnds, bool active,
            IReadOnlyList<Owner> knownTo, bool exemptsAlliedAttackRangePenalty)
            : this(id, cell, origin, creatingSide, movementRangeBonus, attackRangeReduction, attackDamageReductionPercent,
                remainingOpposingTurnEnds, active, knownTo, 0, exemptsAlliedAttackRangePenalty)
        {
        }

        public TraitTrailFact(long id, CellCoord cell, EffectOriginKey origin, Owner creatingSide, int movementRangeBonus,
            int attackRangeReduction, int attackDamageReductionPercent, int remainingTurnEnds, bool active,
            IReadOnlyList<Owner> knownTo, long creationControllerTurnSequence, bool exemptsAlliedAttackRangePenalty = false)
            : base(id, cell, "trait-trail", 0, active, knownTo)
        {
            if (!origin.IsValid || origin.Kind != EffectOwnerKind.Trait || origin.OwnerInstanceId < 1 || origin.RuleKey != "main" ||
                (origin.DefinitionKey != (int)TraitKey.GroundCompaction && origin.DefinitionKey != (int)TraitKey.DustScreen))
                throw new ArgumentException("흔적은 해당 특성의 유효한 출처가 필요합니다.", nameof(origin));
            if (creatingSide != Owner.Player && creatingSide != Owner.Enemy) throw new ArgumentOutOfRangeException(nameof(creatingSide));
            if (movementRangeBonus < 0) throw new ArgumentOutOfRangeException(nameof(movementRangeBonus));
            if (attackRangeReduction < 0) throw new ArgumentOutOfRangeException(nameof(attackRangeReduction));
            if (attackDamageReductionPercent < 0 || attackDamageReductionPercent > 100) throw new ArgumentOutOfRangeException(nameof(attackDamageReductionPercent));
            if (remainingTurnEnds < 0 || active != (remainingTurnEnds > 0)) throw new ArgumentOutOfRangeException(nameof(remainingTurnEnds));
            if (creationControllerTurnSequence < 0) throw new ArgumentOutOfRangeException(nameof(creationControllerTurnSequence));
            if (origin.DefinitionKey == (int)TraitKey.GroundCompaction && (attackRangeReduction != 0 || attackDamageReductionPercent != 0 || exemptsAlliedAttackRangePenalty) ||
                origin.DefinitionKey == (int)TraitKey.DustScreen && movementRangeBonus != 0)
                throw new ArgumentException("흔적 종류와 수치 필드가 일치해야 합니다.", nameof(origin));
            Origin = origin;
            CreatingSide = creatingSide;
            MovementRangeBonus = movementRangeBonus;
            AttackRangeReduction = attackRangeReduction;
            AttackDamageReductionPercent = attackDamageReductionPercent;
            ExemptsAlliedAttackRangePenalty = exemptsAlliedAttackRangePenalty;
            RemainingTurnEnds = remainingTurnEnds;
            CreationControllerTurnSequence = creationControllerTurnSequence;
        }

        internal TraitTrailFact EndControllerTurn(Owner endedSide, long turnSequence)
        {
            bool due = Key == TraitKey.GroundCompaction
                ? CreatingSide == endedSide && turnSequence > CreationControllerTurnSequence
                : CreatingSide != endedSide;
            return Active && due ? WithRemainingTurnEnds(RemainingTurnEnds - 1) : this;
        }

        public TraitTrailFact WithRemainingOpposingTurnEnds(int remaining) => WithRemainingTurnEnds(remaining);

        internal TraitTrailFact WithRemainingTurnEnds(int remaining) => new(Id, Cell, Origin, CreatingSide, MovementRangeBonus,
            AttackRangeReduction, AttackDamageReductionPercent, remaining, remaining > 0, KnownTo, CreationControllerTurnSequence,
            ExemptsAlliedAttackRangePenalty);

        public override MovementTileFact WithKnownTo(IReadOnlyList<Owner> knownTo) => new TraitTrailFact(Id, Cell, Origin, CreatingSide,
            MovementRangeBonus, AttackRangeReduction, AttackDamageReductionPercent, RemainingTurnEnds, Active, knownTo, CreationControllerTurnSequence,
            ExemptsAlliedAttackRangePenalty);

        public override bool SameValues(MovementTileFact other) => SameSourceValues(other) && other is TraitTrailFact trail &&
            SameConfiguration(trail) && RemainingTurnEnds == trail.RemainingTurnEnds;

        public override string SuccessorFailure(MovementTileFact final) => final is not TraitTrailFact trail ||
            Id != trail.Id || Cell != trail.Cell || !KnownTo.SequenceEqual(trail.KnownTo) || !SameConfiguration(trail) ||
            trail.RemainingTurnEnds > RemainingTurnEnds ? IdentityChangeFailure : null;

        private bool SameConfiguration(TraitTrailFact other) => Origin == other.Origin && CreatingSide == other.CreatingSide &&
            CreationControllerTurnSequence == other.CreationControllerTurnSequence &&
            MovementRangeBonus == other.MovementRangeBonus && AttackRangeReduction == other.AttackRangeReduction &&
            AttackDamageReductionPercent == other.AttackDamageReductionPercent &&
            ExemptsAlliedAttackRangePenalty == other.ExemptsAlliedAttackRangePenalty;
    }
}
