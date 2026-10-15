using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public enum MovementEffectPhase { Hazard, Recovery }

    /// <summary>진입 규칙이 읽는 현재 이동자 사실만 복사한다. 명령·체크포인트·발행 권한은 전달하지 않는다.</summary>
    public sealed class MovementStepInput
    {
        public CellCoord EnteredCell { get; }
        public EntityId MoverId { get; }
        public Owner Owner { get; }
        public HealthStateSnapshot Health { get; }
        public UnitStatsSnapshot Stats { get; }
        public CellCoord? Anchor { get; }
        public Rotation4? Rotation { get; }
        public IReadOnlyList<CellCoord> OccupiedCells { get; }
        public MovementEffectPhase Phase { get; }
        public IReadOnlyList<MovementHealthTarget> Targets { get; }
        public IReadOnlyList<MovementTileFact> SourcesAtCell { get; }
        /// <summary>도착자가 현재 보유한 환경 효과다. 획득 규칙이 중복 여부를 스스로 판단할 때 읽는다.</summary>
        public CharacterHeldEnvironmentState HeldEnvironment { get; }

        public MovementStepInput(CellCoord enteredCell, CharacterCombatFacts mover)
            : this(enteredCell, mover, MovementEffectPhase.Hazard,
                Array.Empty<MovementHealthTarget>(), Array.Empty<MovementTileFact>()) { }

        public MovementStepInput(CellCoord enteredCell, CharacterCombatFacts mover, MovementEffectPhase phase,
            IReadOnlyList<MovementHealthTarget> targets, IReadOnlyList<MovementTileFact> sourcesAtCell)
        {
            if (mover == null) throw new ArgumentNullException(nameof(mover));
            EnteredCell = enteredCell;
            MoverId = mover.EntityId;
            Owner = mover.Owner;
            Health = mover.Health;
            Stats = mover.Stats;
            Anchor = mover.Anchor;
            Rotation = mover.Rotation;
            OccupiedCells = Array.AsReadOnly(mover.OccupiedCells.ToArray());
            HeldEnvironment = mover.HeldEnvironment;
            if (!Enum.IsDefined(typeof(MovementEffectPhase), phase)) throw new ArgumentOutOfRangeException(nameof(phase));
            Phase = phase;
            Targets = Array.AsReadOnly((targets ?? throw new ArgumentNullException(nameof(targets))).ToArray());
            SourcesAtCell = Array.AsReadOnly((sourcesAtCell ?? throw new ArgumentNullException(nameof(sourcesAtCell))).ToArray());
        }
    }

    /// <summary>규칙의 대상 선택에 필요한 현재 체력·위치만 전달한다. 명령 및 이동 장부는 노출하지 않는다.</summary>
    public sealed class MovementHealthTarget
    {
        public EntityId EntityId { get; }
        public Owner Owner { get; }
        public HealthStateSnapshot Health { get; }
        public IReadOnlyList<CellCoord> OccupiedCells { get; }
        public MovementHealthTarget(EntityId entityId, Owner owner, HealthStateSnapshot health,
            IReadOnlyList<CellCoord> occupiedCells)
        {
            EntityId = entityId; Owner = owner; Health = health;
            OccupiedCells = Array.AsReadOnly(occupiedCells.ToArray());
        }
    }
}
