using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView
{
    /// <summary>
    /// 사용자가 고른 전투 명령 하나입니다. 개체 ID·논리 칸·방향·조준만 담고, 가능한지는 Core가 판단합니다.
    /// Kind에 쓰이지 않는 값은 기본값이며 의미가 없습니다.
    /// </summary>
    public readonly struct CombatViewCommandData
    {
        public CombatViewCommandKind Kind { get; }
        /// <summary>배치·회수·이동·공격의 주체입니다. 주체가 없는 명령은 Empty입니다.</summary>
        public EntityId EntityId { get; }
        /// <summary>메인 배치·유닛 배치·이동의 목표 칸입니다.</summary>
        public CellCoord Cell { get; }
        /// <summary>메인 배치·유닛 배치의 방향입니다.</summary>
        public Rotation4 Rotation { get; }
        /// <summary>공격 조준입니다.</summary>
        public AttackAim Aim { get; }
        public MovementPath Path { get; }
        public MovementPreviewContext MovementContext { get; }

        private CombatViewCommandData(CombatViewCommandKind kind, EntityId entityId,
            CellCoord cell, Rotation4 rotation, AttackAim aim, MovementPath path = null, MovementPreviewContext movementContext = null)
        {
            Kind = kind;
            EntityId = entityId;
            Cell = cell;
            Rotation = rotation;
            Aim = aim;
            Path = path; MovementContext = movementContext;
        }

        public static CombatViewCommandData PlaceMainCharacter(CellCoord cell, Rotation4 rotation)
        {
            CombatViewInputGuard.RequireRotation(rotation);
            return new CombatViewCommandData(CombatViewCommandKind.PlaceMainCharacter,
                EntityId.Empty, cell, rotation, default);
        }

        public static CombatViewCommandData PlaceUnit(EntityId unit, CellCoord cell, Rotation4 rotation)
        {
            CombatViewInputGuard.RequireEntity(unit, nameof(unit));
            CombatViewInputGuard.RequireRotation(rotation);
            return new CombatViewCommandData(CombatViewCommandKind.PlaceUnit,
                unit, cell, rotation, default);
        }

        public static CombatViewCommandData ReturnUnit(EntityId unit)
        {
            CombatViewInputGuard.RequireEntity(unit, nameof(unit));
            return new CombatViewCommandData(CombatViewCommandKind.ReturnUnit,
                unit, default, default, default);
        }

        public static CombatViewCommandData EnterCombat()
        {
            return new CombatViewCommandData(CombatViewCommandKind.EnterCombat,
                EntityId.Empty, default, default, default);
        }

        public static CombatViewCommandData Move(EntityId unit, MovementPath path, MovementPreviewContext context)
        {
            CombatViewInputGuard.RequireEntity(unit, nameof(unit));
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (context == null) throw new ArgumentNullException(nameof(context));
            return new CombatViewCommandData(CombatViewCommandKind.Move,
                unit, default, default, default, path, context);
        }

        public static CombatViewCommandData ReturnToCheckpoint(EntityId unit, MovementPreviewContext context)
        {
            CombatViewInputGuard.RequireEntity(unit, nameof(unit));
            if (context == null) throw new ArgumentNullException(nameof(context));
            return new CombatViewCommandData(CombatViewCommandKind.ReturnToCheckpoint, unit, default, default, default,
                movementContext: context);
        }

        public static CombatViewCommandData Attack(EntityId attacker, AttackAim aim)
        {
            CombatViewInputGuard.RequireEntity(attacker, nameof(attacker));
            CombatViewInputGuard.RequireAim(aim);
            return new CombatViewCommandData(CombatViewCommandKind.Attack,
                attacker, default, default, aim);
        }

        public static CombatViewCommandData EndUnitTurn()
        {
            return new CombatViewCommandData(CombatViewCommandKind.EndUnitTurn,
                EntityId.Empty, default, default, default);
        }

        public static CombatViewCommandData DrawCharacter()
        {
            return new CombatViewCommandData(CombatViewCommandKind.DrawCharacter,
                EntityId.Empty, default, default, default);
        }
    }

    /// <summary>CombatViewBuffer가 번호를 붙여 보관하는 실행 명령입니다.</summary>
    public readonly struct CombatViewCommand
    {
        /// <summary>Buffer가 발급한 번호입니다. 1부터 커지며 다시 쓰이지 않습니다.</summary>
        public int Number { get; }
        public CombatViewCommandData Data { get; }

        internal CombatViewCommand(int number, CombatViewCommandData data)
        {
            if (number < 1)
                throw new ArgumentOutOfRangeException(nameof(number));
            if (data.Kind == CombatViewCommandKind.None)
                throw new ArgumentException("명령 종류가 비어 있습니다. 정적 생성 메서드로 만든 값이 필요합니다.", nameof(data));

            Number = number;
            Data = data;
        }
    }

    /// <summary>명령과 미리보기 값의 형식을 검사합니다. 규칙상 가능한지는 검사하지 않습니다.</summary>
    internal static class CombatViewInputGuard
    {
        internal static void RequireEntity(EntityId entityId, string paramName)
        {
            if (entityId.IsEmpty)
                throw new ArgumentException("개체 ID가 필요합니다.", paramName);
        }

        internal static void RequireRotation(Rotation4 rotation)
        {
            if (!System.Enum.IsDefined(typeof(Rotation4), rotation))
                throw new ArgumentOutOfRangeException(nameof(rotation));
        }

        internal static void RequireAim(AttackAim aim)
        {
            if (aim.Kind != AttackAimKind.Cell && aim.Kind != AttackAimKind.Direction && aim.Kind != AttackAimKind.All)
                throw new ArgumentException("조준은 AttackAim.ForCell·ForDirection·ForAll로 만들어야 합니다.", nameof(aim));
        }
    }
}
