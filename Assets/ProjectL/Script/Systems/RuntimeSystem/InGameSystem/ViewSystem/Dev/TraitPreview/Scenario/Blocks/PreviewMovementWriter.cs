#if UNITY_EDITOR
using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev.TraitPreview.Scenario.Blocks
{
    internal sealed class PreviewMovementWriter
    {
        internal void Write(PreviewPacketContext packet, PreviewScenarioBlock block)
        {
            var input = block.movement ?? throw new ArgumentException("이동 입력이 없습니다.");
            PreviewScenarioValidation.Defined(input.direction);
            var before = packet.Ledger.Alive(input.unitId);
            if (input.path == null || input.path.Count == 0)
                throw new ArgumentException("이동 경로가 비었습니다.");
            for (int i = 0; i < input.path.Count; i++)
            {
                var point = input.path[i];
                var cell = PreviewScenarioLedger.Cell(point.x, point.y);
                var old = before.Anchor.Value;
                if (Math.Abs(cell.X - old.X) + Math.Abs(cell.Y - old.Y) != 1)
                    throw new ArgumentException($"경로 {i}: 인접한 칸이 아닙니다.");
                if (packet.Ledger.Snapshot().Any(f => !f.Health.IsDead && f.EntityId != before.EntityId && f.OccupiedCells.Contains(cell)))
                    throw new ArgumentException($"경로 {i}: 다른 생존 유닛과 칸이 겹칩니다.");
                var after = before.WithPlacement(before.Area, null, cell, input.direction, new[] { cell });
                packet.Events.Add(new CombatCharacterEvent(CombatEventKind.Moved, before, after, null, null, CombatDamageCause.NumericDamage, new CombatMovementCause(CombatMovementAgency.Voluntary)));
                packet.Ledger.Set(input.unitId, after);
                before = after;
            }
        }
    }
}
#endif
