using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>선정 실패에도 차단 증거를 보존한다. 성공한 선정과 실패 사유를 동시에 가질 수 없다.</summary>
    internal sealed class AttackSelectionEvaluation
    {
        internal AttackFailureReason FailureReason { get; }
        internal ResolvedAttackSelection Selection { get; }
        internal IReadOnlyList<AttackBlockedContact> BlockedContacts { get; }

        private AttackSelectionEvaluation(AttackFailureReason failureReason, ResolvedAttackSelection selection,
            IEnumerable<AttackBlockedContact> blockedContacts)
        {
            FailureReason = failureReason;
            Selection = selection;
            var copy = (blockedContacts ?? Array.Empty<AttackBlockedContact>()).ToArray();
            if (copy.Any(c => c == null)) throw new ArgumentException("차단 증거에 null을 넣을 수 없습니다.", nameof(blockedContacts));
            BlockedContacts = Array.AsReadOnly(copy);
        }

        internal static AttackSelectionEvaluation Success(ResolvedAttackSelection selection, IEnumerable<AttackBlockedContact> blockedContacts = null) =>
            new AttackSelectionEvaluation(AttackFailureReason.None, selection ?? throw new ArgumentNullException(nameof(selection)), blockedContacts);

        internal static AttackSelectionEvaluation Failure(AttackFailureReason reason, IEnumerable<AttackBlockedContact> blockedContacts = null)
        {
            if (reason == AttackFailureReason.None || !Enum.IsDefined(typeof(AttackFailureReason), reason))
                throw new ArgumentOutOfRangeException(nameof(reason));
            return new AttackSelectionEvaluation(reason, null, blockedContacts);
        }
    }
}
