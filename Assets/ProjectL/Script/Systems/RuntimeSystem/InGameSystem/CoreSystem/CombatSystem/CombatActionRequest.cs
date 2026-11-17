using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem
{
    /// <summary>
    /// 기능 소유자가 이미 해석한 전투 요청 값이다. View 요청 형식이나 서비스 참조를 담지 않는다.
    /// 행위자 보충, 요청 종류 변환, 수락 여부는 이 값을 만드는 쪽이 결정한다.
    /// </summary>
    internal sealed class CombatActionRequest
    {
        public int? ItemKey { get; }
        public int Number { get; }
        public CombatCommandKind Kind { get; }
        public EntityId ActorId { get; }
        public AttackAim Aim { get; }
        public AttackSweepSide? SweepSide { get; }
        public CellCoord Cell { get; }
        public Rotation4 Rotation { get; }
        public CombatProcessingKind ProcessingKind { get; }
        public MovementPath Path { get; }
        public MovementPreviewContext MovementContext { get; }
        public long ExpectedBattleInstanceId { get; }
        public long ExpectedControllerTurnSequence { get; }
        public int ExpectedMoveCommandNumber { get; }
        public PlayerSkillRequest PlayerSkill { get; }

        public CombatActionRequest(int number, CombatCommandKind kind, EntityId actorId, AttackAim aim,
            CellCoord cell = default, Rotation4 rotation = Rotation4.R0,
            CombatProcessingKind processingKind = CombatProcessingKind.Command,
            MovementPath path = null, MovementPreviewContext movementContext = null, int? itemKey = null,
            long expectedBattleInstanceId = 0, long expectedControllerTurnSequence = 0, int expectedMoveCommandNumber = 0,
            AttackSweepSide? sweepSide = null, PlayerSkillRequest playerSkill = null)
        {
            ItemKey = itemKey;
            Number = number;
            Kind = kind;
            ActorId = actorId;
            Aim = aim;
            AttackSweepSideValidation.Validate(sweepSide);
            SweepSide = sweepSide;
            Cell = cell;
            Rotation = rotation;
            ProcessingKind = processingKind;
            Path = path;
            MovementContext = movementContext;
            ExpectedBattleInstanceId = expectedBattleInstanceId;
            ExpectedControllerTurnSequence = expectedControllerTurnSequence;
            ExpectedMoveCommandNumber = expectedMoveCommandNumber;
            PlayerSkill = playerSkill;
        }
    }
}
