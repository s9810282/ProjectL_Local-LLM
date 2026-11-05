using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>선정 실패에도 차단 증거를 보존한다. 성공한 선정과 실패 사유를 동시에 가질 수 없다.</summary>
    internal sealed class AttackSelectionEvaluation
    {
        internal AttackFailureReason FailureReason { get; }
        internal ResolvedAttackSelection Selection { get; }
        internal PullAttackEvaluation Pull { get; }
        internal IReadOnlyList<AttackBlockedContact> BlockedContacts { get; }

        /// <summary>
        /// 대상이 없어 실패한 위치 없는 지정(전체 지정 등)이 덮었을 칸이다. 그 밖의 실패·성공에서는 비어 있다.
        /// 표시 전용(포효 범위)이며 판정에는 쓰지 않는다.
        /// </summary>
        internal IReadOnlyList<CellCoord> EmptyAreaCells { get; }

        private AttackSelectionEvaluation(AttackFailureReason failureReason, ResolvedAttackSelection selection,
            IEnumerable<AttackBlockedContact> blockedContacts, IEnumerable<CellCoord> emptyAreaCells = null, PullAttackEvaluation pull = null)
        {
            FailureReason = failureReason;
            Selection = selection;
            Pull = pull;
            var copy = (blockedContacts ?? Array.Empty<AttackBlockedContact>()).ToArray();
            if (copy.Any(c => c == null)) throw new ArgumentException("차단 증거에 null을 넣을 수 없습니다.", nameof(blockedContacts));
            BlockedContacts = Array.AsReadOnly(copy);
            EmptyAreaCells = Array.AsReadOnly((emptyAreaCells ?? Array.Empty<CellCoord>()).ToArray());
        }

        internal static AttackSelectionEvaluation Success(ResolvedAttackSelection selection, IEnumerable<AttackBlockedContact> blockedContacts = null, PullAttackEvaluation pull = null) =>
            new AttackSelectionEvaluation(AttackFailureReason.None, selection ?? throw new ArgumentNullException(nameof(selection)), blockedContacts, pull: pull);

        internal static AttackSelectionEvaluation Failure(AttackFailureReason reason, IEnumerable<AttackBlockedContact> blockedContacts = null,
            IEnumerable<CellCoord> emptyAreaCells = null)
        {
            if (reason == AttackFailureReason.None || !Enum.IsDefined(typeof(AttackFailureReason), reason))
                throw new ArgumentOutOfRangeException(nameof(reason));
            return new AttackSelectionEvaluation(reason, null, blockedContacts, emptyAreaCells);
        }
    }
}
