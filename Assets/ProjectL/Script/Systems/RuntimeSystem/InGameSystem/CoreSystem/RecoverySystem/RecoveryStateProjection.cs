using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Run;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RecoverySystem
{
    /// <summary>권위 Buffer의 사실을 캡처하고 현재·예정 결과를 같은 규칙으로 투영한다.</summary>
    internal sealed class RecoveryStateProjection
    {
        internal static bool TryGetEntry(IBufferQuery buffers, out CurrentRecoveryData recovery)
        {
            if (buffers == null) throw new InvalidOperationException("회복 조회에는 서비스 바인딩이 필요합니다.");
            var current = buffers.CurrentProgressReadSink;
            recovery = null;
            if (buffers.RunProgressReadSink.Outcome != RunOutcome.InProgress || buffers.CombatStateReadSink.HasBattle ||
                !current.HasSelection || current.Recovery == null) return false;
            recovery = current.Recovery;
            var context = recovery.Context;
            if (current.Shop != null || context.EntryId.IsEmpty || context.ChapterKey != current.CurrentMapKey ||
                context.NodeId.Value != current.CurrentStage.NodeId || context.StageKey.Value != current.CurrentStage.StageKey)
                throw new InvalidOperationException("현재 회복의 유효한 진입 문맥이 필요합니다.");
            return true;
        }

        internal Facts Capture(IBufferQuery buffers)
        {
            if (!TryGetEntry(buffers, out var recovery)) return null;
            var context = recovery.Context;
            var nodes = buffers.StageProgressReadSink.GetStages(context.ChapterKey).ToArray();
            var matching = nodes.Where(x => x.NodeId == context.NodeId.Value).ToArray();
            if (matching.Length != 1 || matching[0].StageKey != context.StageKey.Value ||
                matching[0].StageType != StageType.Event || matching[0].MapRole == StageMapRole.Boss)
                throw new InvalidOperationException("회복 문맥과 등록된 노드가 일치해야 합니다.");
            bool mapClear = buffers.MapProgressReadSink.GetMap(context.ChapterKey).IsClear;
            if (!buffers.CharacterReadSink.TryGetMainState(recovery.MainId, out var main))
                throw new InvalidOperationException("메인 지속 상태가 필요합니다.");
            if (!buffers.CharacterReadSink.TryGetOwned(recovery.MainId, out var owned))
                throw new InvalidOperationException("회복 진입의 보유 메인이 없습니다.");
            int souls = buffers.CharacterReadSink.GetSoulCount(owned.Id);
            return new Facts(recovery, buffers.CurrentProgressReadSink.CurrentStage, main, owned.Id, souls,
                buffers.CurrencyReadSink.Amount, nodes, mapClear, matching[0].IsClear, buffers.CharacterReadSink.RosterRevision);
        }

        internal RecoveryStateSnapshot Project(Facts facts, MainCharacterPersistentState main = null,
            int? gold = null, CurrentRecoveryData recovery = null, bool? completed = null)
        {
            if (facts == null) throw new ArgumentNullException(nameof(facts));
            main = main ?? facts.Main;
            recovery = recovery ?? facts.Recovery;
            int balance = gold ?? facts.Gold;
            bool isCompleted = completed ?? facts.IsCompleted;
            HealthStateSnapshot health = MainCharacterHealthCalculator.Calculate(main, facts.Souls);
            var options = recovery.Options.Select(option => RecoveryOptionEvaluator.Evaluate(option, health,
                balance, recovery.AppliedOptionId.HasValue, isCompleted)).ToArray();
            return new RecoveryStateSnapshot(recovery.Context, health, balance, options,
                new RecoveryReceiptSnapshot(recovery.AppliedOptionId, recovery.PaidAmount, recovery.HealedAmount, recovery.MaxHealthIncrease), isCompleted);
        }

        internal void RequireUnchanged(IBufferQuery buffers, Facts expected)
        {
            Facts current = Capture(buffers);
            if (current == null || !ReferenceEquals(current.Recovery, expected.Recovery) ||
                !ReferenceEquals(current.Main, expected.Main) || current.MainId != expected.MainId ||
                current.Souls != expected.Souls || current.Gold != expected.Gold ||
                current.RosterRevision != expected.RosterRevision ||
                !current.Selection.Equals(expected.Selection) || current.MapClear != expected.MapClear ||
                !current.Nodes.SequenceEqual(expected.Nodes))
                throw new InvalidOperationException("회복 준비 중 원본 사실이 변경되었습니다.");
        }

        internal sealed class Facts
        {
            internal CurrentRecoveryData Recovery { get; }
            internal StageSelectionData Selection { get; }
            internal MainCharacterPersistentState Main { get; }
            internal CharacterInstanceId MainId { get; }
            internal int Souls { get; }
            internal int Gold { get; }
            internal IReadOnlyList<StageProgressData> Nodes { get; }
            internal bool MapClear { get; }
            internal bool IsCompleted { get; }
            internal long RosterRevision { get; }

            internal Facts(CurrentRecoveryData recovery, StageSelectionData selection, MainCharacterPersistentState main,
                CharacterInstanceId mainId, int souls, int gold, StageProgressData[] nodes, bool mapClear, bool nodeClear, long rosterRevision)
            {
                Recovery = recovery;
                Selection = selection;
                Main = main;
                MainId = mainId;
                Souls = souls;
                Gold = gold;
                Nodes = Array.AsReadOnly(nodes);
                MapClear = mapClear;
                IsCompleted = mapClear || nodeClear;
                RosterRevision = rosterRevision;
            }
        }
    }
}
