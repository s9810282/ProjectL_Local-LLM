using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public enum AttackOcclusionPhase { Direct = 1, Propagation = 2 }

    /// <summary>벽에 가려진 접촉 셀과 차단 벽의 원본 증거다. 연출 객체나 전투 상태를 보관하지 않는다.</summary>
    public sealed class AttackBlockedContact
    {
        public CellCoord Origin { get; }
        public CellCoord Contact { get; }
        public AttackOcclusionPhase Phase { get; }
        public IReadOnlyList<long> WallSourceIds { get; }
        public IReadOnlyList<CellCoord> WallCells { get; }

        internal static IReadOnlyList<AttackBlockedContact> CopyCollection(IReadOnlyList<AttackBlockedContact> contacts)
        {
            if (contacts == null || contacts.Count == 0) return Array.Empty<AttackBlockedContact>();
            var copy = contacts.ToArray();
            if (copy.Any(contact => contact == null))
                throw new ArgumentException("차단 증거 목록에는 null을 넣을 수 없습니다.", nameof(contacts));
            return Array.AsReadOnly(copy);
        }

        public AttackBlockedContact(CellCoord origin, CellCoord contact, AttackOcclusionPhase phase,
            IReadOnlyList<long> wallSourceIds, IReadOnlyList<CellCoord> wallCells)
        {
            if (phase != AttackOcclusionPhase.Direct && phase != AttackOcclusionPhase.Propagation)
                throw new ArgumentOutOfRangeException(nameof(phase));
            if (wallSourceIds == null) throw new ArgumentNullException(nameof(wallSourceIds));
            if (wallCells == null) throw new ArgumentNullException(nameof(wallCells));
            if (wallSourceIds.Count == 0 || wallSourceIds.Any(id => id <= 0) ||
                wallSourceIds.Distinct().Count() != wallSourceIds.Count)
                throw new ArgumentException("차단 벽은 중복 없는 양수 원본 ID가 필요합니다.", nameof(wallSourceIds));
            if (wallCells.Count != wallSourceIds.Count || wallCells.Distinct().Count() != wallCells.Count)
                throw new ArgumentException("차단 벽의 셀은 원본 ID 수와 같고 중복이 없어야 합니다.", nameof(wallCells));
            Origin = origin;
            Contact = contact;
            Phase = phase;
            WallSourceIds = Array.AsReadOnly(wallSourceIds.OrderBy(id => id).ToArray());
            WallCells = Array.AsReadOnly(wallCells.OrderBy(cell => cell.Y).ThenBy(cell => cell.X).ToArray());
        }
    }
}
