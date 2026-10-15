using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    /// <summary>요청 생성은 맵 선택이나 Run 상태를 변경하지 않는다.</summary>
    public sealed class StageEntryRequest
    {
        public RunId RunId { get; }
        public StageEntryId EntryId { get; }
        public long ExpectedRunRevision { get; }
        public StageNodeId NodeId { get; }
        public StageKey StageKey { get; }
        public UnitSpec MainCharacterSpec { get; }
        public HealthStateSnapshot Health { get; }
        public StageDrawSpec Draw { get; }

        public StageEntryRequest(RunId runId, StageEntryId entryId, long expectedRunRevision,
            StageNodeId nodeId, StageKey stageKey, UnitSpec mainCharacterSpec, HealthStateSnapshot health,
            StageDrawSpec draw)
        {
            if (runId.IsEmpty) throw new ArgumentException(nameof(runId));
            if (entryId.IsEmpty) throw new ArgumentException(nameof(entryId));
            if (expectedRunRevision < 0) throw new ArgumentOutOfRangeException(nameof(expectedRunRevision));
            if (nodeId.IsEmpty) throw new ArgumentException(nameof(nodeId));
            if (stageKey.IsEmpty) throw new ArgumentException(nameof(stageKey));
            if (mainCharacterSpec == null) throw new ArgumentNullException(nameof(mainCharacterSpec));
            if (mainCharacterSpec.Key != EntityKey.Unit_MainCharacter || mainCharacterSpec.Role != UnitRole.MainCharacter)
                throw new ArgumentException("주인공 UnitSpec이 필요합니다.", nameof(mainCharacterSpec));
            if (health.MaxHealth < 1 || health.IsDead || health.CurrentHealth > health.MaxHealth)
                throw new ArgumentException("살아 있는 주인공의 HP가 필요합니다.", nameof(health));
            if (draw == null) throw new ArgumentNullException(nameof(draw));
            if (draw.RunId != runId || draw.EntryId != entryId)
                throw new ArgumentException("덱 입력의 Run과 진입 ID가 요청과 일치해야 합니다.", nameof(draw));

            RunId = runId;
            EntryId = entryId;
            ExpectedRunRevision = expectedRunRevision;
            NodeId = nodeId;
            StageKey = stageKey;
            MainCharacterSpec = mainCharacterSpec;
            Health = health;
            Draw = draw;
        }
    }
}
