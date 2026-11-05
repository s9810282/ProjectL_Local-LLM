using System;
using System.Collections.Generic;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Object.Enum;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Object.Item
{
    /// <summary>지뢰·깃발·배지의 불변 설정입니다. 위치와 획득 상태는 저장하지 않습니다.</summary>
    public sealed class ObjectDefinitionData : IData
    {
        public ObjectContentKind Kind { get; }
        public int Damage { get; }
        public int PriorityBonus { get; }

        public ObjectDefinitionData(ObjectContentKind kind, int damage = 0, int priorityBonus = 0)
        {
            Kind = kind;
            Damage = damage;
            PriorityBonus = priorityBonus;
            Validate();
        }

        public void Validate()
        {
            if (Kind == ObjectContentKind.None || !System.Enum.IsDefined(typeof(ObjectContentKind), Kind))
            {
                throw new InvalidOperationException("Object 종류가 지정되지 않았거나 지원되지 않습니다.");
            }

            if (Damage < 0 || PriorityBonus < 0)
            {
                throw new InvalidOperationException("지뢰 피해와 깃발 우선순위 보너스는 음수일 수 없습니다.");
            }

            if (Kind != ObjectContentKind.Mine && Damage != 0
                || Kind != ObjectContentKind.Flag && PriorityBonus != 0)
            {
                throw new InvalidOperationException("지뢰만 피해를, 깃발만 우선순위 보너스를 설정할 수 있습니다.");
            }
        }

        /// <summary>규칙과 스테이지 좌표를 기존 Core 초기 Object 계약으로 변환합니다.</summary>
        public InitialObjectPlacementSpec CreatePlacement(long sourceId, CellCoord cell, IReadOnlyList<Owner> knownTo)
        {
            return Kind switch
            {
                ObjectContentKind.Mine => new MinePlacementSpec(sourceId, cell, Damage, knownTo),
                ObjectContentKind.Flag => new FlagPlacementSpec(sourceId, cell, PriorityBonus, knownTo),
                ObjectContentKind.SheriffBadge => new SheriffBadgePlacementSpec(sourceId, cell, knownTo),
                _ => throw new InvalidOperationException("지원되지 않는 Object 종류입니다.")
            };
        }
    }
}
