using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    public sealed class CompletedStageSnapshot
    {
        public StageEntryId EntryId { get; }
        public StageNodeId NodeId { get; }
        public StageKey StageKey { get; }
        public StageExecutionKind ExecutionKind { get; }
        public StageSnapshot FinalStage { get; }
        public RecoveryReceiptSnapshot Recovery { get; }
        public NonCombatStageKind? NonCombatKind { get; }
        public ShopReceiptSnapshot Shop { get; }
        public StageOutcomeSnapshot Outcome => ExecutionKind == StageExecutionKind.Combat
            ? FinalStage.Outcome
            : throw new InvalidOperationException("Non-combat completion has no combat outcome.");

        public CompletedStageSnapshot(
            StageEntryId entryId,
            StageNodeId nodeId,
            StageKey stageKey,
            StageSnapshot finalStage)
        {
            if (entryId.IsEmpty)
                throw new ArgumentException("StageEntryId는 비어 있을 수 없습니다.", nameof(entryId));
            if (nodeId.IsEmpty)
                throw new ArgumentException("StageNodeId는 비어 있을 수 없습니다.", nameof(nodeId));
            if (stageKey.IsEmpty)
                throw new ArgumentException("StageKey는 비어 있을 수 없습니다.", nameof(stageKey));
            if (finalStage == null)
                throw new ArgumentNullException(nameof(finalStage));
            if (finalStage.Outcome.IsStageEnded == false)
                throw new ArgumentException("완료 스테이지에는 종료된 StageSnapshot이 필요합니다.", nameof(finalStage));

            EntryId = entryId;
            NodeId = nodeId;
            StageKey = stageKey;
            ExecutionKind = StageExecutionKind.Combat;
            FinalStage = finalStage;
        }

        public CompletedStageSnapshot(
            StageEntryId entryId,
            StageNodeId nodeId,
            StageKey stageKey,
            RecoveryReceiptSnapshot recovery)
        {
            if (entryId.IsEmpty)
                throw new ArgumentException("StageEntryId cannot be empty.", nameof(entryId));
            if (nodeId.IsEmpty)
                throw new ArgumentException("StageNodeId cannot be empty.", nameof(nodeId));
            if (stageKey.IsEmpty)
                throw new ArgumentException("StageKey cannot be empty.", nameof(stageKey));
            if (recovery == null)
                throw new ArgumentNullException(nameof(recovery));

            EntryId = entryId;
            NodeId = nodeId;
            StageKey = stageKey;
            ExecutionKind = StageExecutionKind.NonCombat;
            Recovery = recovery;
            NonCombatKind = NonCombatStageKind.Recovery;
        }

        public CompletedStageSnapshot(StageEntryId entryId, StageNodeId nodeId,
            StageKey stageKey, ShopReceiptSnapshot shop)
        {
            if (entryId.IsEmpty) throw new ArgumentException(nameof(entryId));
            if (nodeId.IsEmpty) throw new ArgumentException(nameof(nodeId));
            if (stageKey.IsEmpty) throw new ArgumentException(nameof(stageKey));
            if (shop == null) throw new ArgumentNullException(nameof(shop));
            EntryId = entryId;
            NodeId = nodeId;
            StageKey = stageKey;
            ExecutionKind = StageExecutionKind.NonCombat;
            NonCombatKind = NonCombatStageKind.Shop;
            Shop = shop;
        }
    }
}
