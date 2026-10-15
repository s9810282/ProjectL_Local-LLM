using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView
{
    /// <summary>
    /// 사용자가 요청한 전투 미리보기 하나입니다. 상태를 바꾸지 않는 계산 요청이며, 새 요청이 오면 덮어씁니다.
    /// Kind에 쓰이지 않는 값은 기본값이며 의미가 없습니다.
    /// </summary>
    public readonly struct CombatViewPreviewData
    {
        public CombatViewPreviewKind Kind { get; }
        /// <summary>배치·이동·공격 미리보기의 주체입니다. 메인 배치 미리보기는 Empty입니다.</summary>
        public EntityId EntityId { get; }
        /// <summary>배치·이동 미리보기의 목표 칸입니다.</summary>
        public CellCoord Cell { get; }
        /// <summary>배치 미리보기의 방향입니다.</summary>
        public Rotation4 Rotation { get; }
        /// <summary>공격 미리보기의 조준입니다.</summary>
        public AttackAim Aim { get; }
        public MovementPath Path { get; }

        private CombatViewPreviewData(CombatViewPreviewKind kind, EntityId entityId,
            CellCoord cell, Rotation4 rotation, AttackAim aim, MovementPath path = null)
        {
            Kind = kind;
            EntityId = entityId;
            Cell = cell;
            Rotation = rotation;
            Aim = aim;
            Path = path;
        }

        public static CombatViewPreviewData MainCharacterPlacement(CellCoord cell, Rotation4 rotation)
        {
            CombatViewInputGuard.RequireRotation(rotation);
            return new CombatViewPreviewData(CombatViewPreviewKind.MainCharacterPlacement,
                EntityId.Empty, cell, rotation, default);
        }

        public static CombatViewPreviewData UnitPlacement(EntityId unit, CellCoord cell, Rotation4 rotation)
        {
            CombatViewInputGuard.RequireEntity(unit, nameof(unit));
            CombatViewInputGuard.RequireRotation(rotation);
            return new CombatViewPreviewData(CombatViewPreviewKind.UnitPlacement,
                unit, cell, rotation, default);
        }

        public static CombatViewPreviewData MoveRange(EntityId unit)
        {
            CombatViewInputGuard.RequireEntity(unit, nameof(unit));
            return new CombatViewPreviewData(CombatViewPreviewKind.MoveRange,
                unit, default, default, default);
        }

        public static CombatViewPreviewData Move(EntityId unit, MovementPath path)
        {
            CombatViewInputGuard.RequireEntity(unit, nameof(unit));
            if (path == null) throw new ArgumentNullException(nameof(path));
            return new CombatViewPreviewData(CombatViewPreviewKind.Move,
                unit, default, default, default, path);
        }

        public static CombatViewPreviewData ReturnToCheckpoint(EntityId unit)
        {
            CombatViewInputGuard.RequireEntity(unit, nameof(unit));
            return new CombatViewPreviewData(CombatViewPreviewKind.ReturnToCheckpoint, unit, default, default, default);
        }

        public static CombatViewPreviewData AttackRange(EntityId attacker)
        {
            CombatViewInputGuard.RequireEntity(attacker, nameof(attacker));
            return new CombatViewPreviewData(CombatViewPreviewKind.AttackRange,
                attacker, default, default, default);
        }

        public static CombatViewPreviewData Attack(EntityId attacker, AttackAim aim)
        {
            CombatViewInputGuard.RequireEntity(attacker, nameof(attacker));
            CombatViewInputGuard.RequireAim(aim);
            return new CombatViewPreviewData(CombatViewPreviewKind.Attack,
                attacker, default, default, aim);
        }
    }

    /// <summary>CombatViewBuffer가 번호를 붙여 보관하는 미리보기 요청입니다.</summary>
    public readonly struct CombatViewPreviewRequest
    {
        /// <summary>Buffer가 발급한 번호입니다. 명령과 같은 카운터를 씁니다.</summary>
        public int Number { get; }
        public CombatViewPreviewData Data { get; }

        internal CombatViewPreviewRequest(int number, CombatViewPreviewData data)
        {
            if (number < 1)
                throw new ArgumentOutOfRangeException(nameof(number));
            if (data.Kind == CombatViewPreviewKind.None)
                throw new ArgumentException("미리보기 종류가 비어 있습니다. 정적 생성 메서드로 만든 값이 필요합니다.", nameof(data));

            Number = number;
            Data = data;
        }
    }
}
