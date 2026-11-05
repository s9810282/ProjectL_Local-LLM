using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>명령 계산 동안만 쓰는 검증된 대상·경로·직접 피해 허용 결정입니다.</summary>
    internal sealed class PullAttackEvaluation
    {
        internal AttackFailureReason FailureReason { get; }
        internal EntityId TargetId { get; }
        internal CellCoord StartAnchor { get; }
        internal CellCoord DestinationAnchor { get; }
        internal IReadOnlyList<CellCoord> Path { get; }
        internal bool RequiresPull => Path.Count > 0;
        internal bool DeliverDirectDamage { get; }

        internal PullAttackEvaluation(AttackFailureReason failureReason, EntityId targetId, CellCoord startAnchor,
            CellCoord destinationAnchor, IReadOnlyList<CellCoord> path, bool deliverDirectDamage)
        {
            FailureReason = failureReason;
            TargetId = targetId;
            StartAnchor = startAnchor;
            DestinationAnchor = destinationAnchor;
            Path = Array.AsReadOnly((path ?? throw new ArgumentNullException(nameof(path))).ToArray());
            DeliverDirectDamage = deliverDirectDamage;
        }

        internal static PullAttackEvaluation Failure(AttackFailureReason reason) =>
            new(reason, default, default, default, Array.Empty<CellCoord>(), false);
    }
}
