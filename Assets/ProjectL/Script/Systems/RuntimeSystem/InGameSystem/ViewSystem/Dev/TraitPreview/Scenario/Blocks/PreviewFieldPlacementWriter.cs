#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev.TraitPreview.Scenario.Blocks
{
    /// <summary>명시한 한/두 유닛의 필드 사실만 구성합니다. 교환 자격·도착 효과·역적용은 계산하지 않습니다.</summary>
    internal static class PreviewFieldPlacementWriter
    {
        internal static void Write(PreviewPacketContext packet, PreviewScenarioBlock block)
        {
            var input = block.placement ?? throw new ArgumentException("필드 배치 입력이 없습니다.");
            if (input.entries == null || input.entries.Count < 1 || input.entries.Count > 2 ||
                input.entries.Any(e => e == null) || input.entries.Select(e => e.unitId).Distinct().Count() != input.entries.Count)
                throw new ArgumentException("필드 배치는 서로 다른 한/두 유닛을 지정해야 합니다.");
            bool restore = input.eventKind == CombatEventKind.Returned;
            if (!restore && input.eventKind != CombatEventKind.Moved)
                throw new ArgumentException("필드 배치는 Moved 또는 Returned여야 합니다.");
            if (restore && (packet.Input.blocks.Count != 1 || packet.Events.Count != 0))
                throw new ArgumentException("필드 복원은 단독 묶음으로 작성하세요. 효과 제거는 별도 State 묶음의 명시 사실입니다.");
            if (!restore && input.entries.Count == 2 && (input.movementTrait != TraitKey.PositionSwap ||
                input.entries[0].agency != CombatMovementAgency.Voluntary || input.entries[1].agency != CombatMovementAgency.Forced))
                throw new ArgumentException("두 유닛 교환은 PositionSwap의 자발 주체/강제 승객 순서로 지정하세요.");
            var before = input.entries.Select(e => packet.Ledger.Alive(e.unitId)).ToArray();
            var finals = new CharacterCombatFacts[before.Length];
            for (int i = 0; i < before.Length; i++)
            {
                var row = input.entries[i];
                if (before[i].Area != CharacterRuntimeArea.Field) throw new ArgumentException("필드에 있는 유닛만 배치할 수 있습니다.");
                PreviewScenarioValidation.Defined(row.actionUse);
                PreviewScenarioValidation.Defined(row.rotation);
                PreviewScenarioValidation.Defined(row.agency);
                var cell = PreviewScenarioLedger.Cell(row.cell.x, row.cell.y);
                var record = before[i].StageRecord.UndoMovement(before[i].StageRecord.DistanceMoved).Add(distanceMoved: row.distanceMoved);
                finals[i] = before[i].WithPlacement(CharacterRuntimeArea.Field, null, cell, row.rotation, new[] { cell })
                    .With(actionUse: row.actionUse, stageRecord: record);
            }
            if (finals.Select(f => f.Anchor).Distinct().Count() != finals.Length || packet.Ledger.Snapshot().Any(f =>
                f.Area == CharacterRuntimeArea.Field && !f.Health.IsDead && !before.Any(b => b.EntityId == f.EntityId) && finals.Any(a => a.Anchor == f.Anchor)))
                throw new ArgumentException("필드 배치 목적지가 다른 유닛과 겹칩니다.");
            if (restore)
            {
                var participants = new List<MovementParticipantRestoration>();
                for (int i = 0; i < before.Length; i++)
                    participants.Add(new MovementParticipantRestoration(before[i].EntityId, before[i].Anchor.Value, finals[i].Anchor.Value,
                        finals[i].Rotation.Value, i == 0, before[i].StageRecord.DistanceMoved - finals[i].StageRecord.DistanceMoved));
                packet.Undo = new MovementUndoResult(before[0].EntityId, input.moveCommandNumber, participants, Array.Empty<MovementEffectReceipt>());
            }
            for (int i = 0; i < before.Length; i++)
            {
                packet.Events.Add(new CombatCharacterEvent(input.eventKind, before[i], finals[i], null, null,
                    CombatDamageCause.NumericDamage, restore ? null : new CombatMovementCause(input.entries[i].agency,
                        input.movementTrait == TraitKey.None ? (TraitKey?)null : input.movementTrait)));
                packet.Ledger.Set(input.entries[i].unitId, finals[i]);
            }
        }
    }
}
#endif
