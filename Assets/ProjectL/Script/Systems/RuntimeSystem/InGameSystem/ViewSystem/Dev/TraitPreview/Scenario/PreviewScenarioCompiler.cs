#if UNITY_EDITOR
using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;
using System.Collections.Generic;
using System.Text;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev.TraitPreview.Scenario
{
    internal interface IPreviewScenarioWriter
    {
        void Initialize(PreviewScenarioLedger ledger, PreviewScenarioUnit unit);
        void Write(PreviewPacketContext packet, PreviewScenarioBlock block);
    }

    internal sealed class PreviewPacketContext
    {
        internal PreviewPacketContext(PreviewScenarioLedger ledger, PreviewScenarioEntry input)
        {
            Ledger = ledger;
            Input = input;
        }

        internal PreviewScenarioLedger Ledger { get; }
        internal PreviewScenarioEntry Input { get; }
        internal List<CombatEvent> Events { get; } = new();
        internal CombatDamageOrigin Origin { get; set; }
        internal ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results.MovementUndoResult Undo { get; set; }
    }

    /// <summary>입력 사본 전체를 검사하고 블록 작성기를 통해 불변 재생 프로그램으로 바꿉니다.</summary>
    internal sealed class PreviewScenarioCompiler
    {
        private readonly IPreviewScenarioWriter _writer;
        private readonly CombatTraitStateEvidenceRegistry _evidence;
        internal PreviewScenarioCompiler(IPreviewScenarioWriter writer, CombatTraitStateEvidenceRegistry evidence)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _evidence = evidence ?? throw new ArgumentNullException(nameof(evidence));
        }

        internal CompiledTraitPreviewScenario Compile(TraitPreviewScenarioInput input)
        {
            input = TraitPreviewScenarioImport.Upgrade(input);
            PreviewScenarioValidation.Require(input);
            var ledger = new PreviewScenarioLedger(input.units);
            foreach (var unit in input.units)
            {
                try
                {
                    _writer.Initialize(ledger, unit);
                }
                catch (Exception error)
                {
                    throw new ArgumentException($"초기 유닛 '{unit.name}': {error.Message}", error);
                }
            }

            var initial = ledger.Snapshot();
            var entries = new List<CompiledPreviewEntry>();
            int sequence = 0;
            foreach (var entry in input.entries)
            {
                var before = ledger.Snapshot();
                var packet = new PreviewPacketContext(ledger, entry);
                var diagnostic = new StringBuilder();
                if (entry.kind == PreviewEntryKind.Packet)
                {
                    foreach (var block in entry.blocks)
                    {
                        int first = packet.Events.Count;
                        try
                        {
                            _writer.Write(packet, block);
                        }
                        catch (Exception error)
                        {
                            throw new ArgumentException($"묶음 '{entry.name}' / 블록 '{block.name}' ({block.id}): {error.Message}", error);
                        }

                        diagnostic.AppendLine($"{block.name} [{block.kind}] → 사건 {first}~{packet.Events.Count - 1}");
                    }
                }

                CombatResult result = entry.kind == PreviewEntryKind.Packet ? new CombatResult(++sequence, CombatProcessingKind.Command, sequence, true, string.Empty, events: packet.Events, battleInstanceId: 1,
                    commandKind: packet.Undo == null ? CombatCommandKind.None : CombatCommandKind.UndoMove,
                    actorId: packet.Undo?.ActorId ?? default,
                    controllerTurnSequence: 1,
                    actionUseSettlement: packet.Undo == null ? null : new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.UnitActionUseSettlementSnapshot(
                        packet.Undo.ActorId, UnitActionUse.Moved, UnitActionUse.Available), undoMovement: packet.Undo) : null;
                if (result != null) _ = CombatPlacementBatchPlan.Build(result);
                PreviewTraitFeedbackValidation.Require(result, entry, _evidence);
                entries.Add(new CompiledPreviewEntry(entry, result, before, ledger.Snapshot(), diagnostic.ToString()));
            }

            return new CompiledTraitPreviewScenario(initial, entries);
        }
    }
}
#endif
