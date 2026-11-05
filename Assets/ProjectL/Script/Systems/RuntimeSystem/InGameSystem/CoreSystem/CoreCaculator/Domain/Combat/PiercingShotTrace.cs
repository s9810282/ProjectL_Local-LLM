using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat
{
    /// <summary>피해 예산 적용 전 한 줄의 실제 이동 순서와 별도 차단 증거다.</summary>
    internal sealed class PiercingShotTrace
    {
        internal CellCoord OriginCell { get; }
        internal CellCoord AimCell { get; }
        internal CellCoord Step { get; }
        internal IReadOnlyList<CellCoord> TraveledCells { get; }
        internal IReadOnlyList<(EntityId Id, int Encounter, CellCoord Cell)> Encounters { get; }
        internal CellCoord Endpoint { get; }
        internal PiercingShotStopReason StopReason { get; }
        internal IReadOnlyList<AttackBlockedContact> BlockedContacts { get; }

        internal PiercingShotTrace(CellCoord originCell, CellCoord aimCell, CellCoord step,
            IEnumerable<CellCoord> traveledCells, IEnumerable<(EntityId Id, int Encounter, CellCoord Cell)> encounters,
            CellCoord endpoint, PiercingShotStopReason stopReason, IEnumerable<AttackBlockedContact> blockedContacts)
        {
            if (traveledCells == null) throw new ArgumentNullException(nameof(traveledCells));
            if (encounters == null) throw new ArgumentNullException(nameof(encounters));
            if (blockedContacts == null) throw new ArgumentNullException(nameof(blockedContacts));
            OriginCell = originCell;
            AimCell = aimCell;
            Step = step;
            TraveledCells = Array.AsReadOnly(traveledCells.ToArray());
            Encounters = Array.AsReadOnly(encounters.ToArray());
            Endpoint = endpoint;
            StopReason = stopReason;
            BlockedContacts = Array.AsReadOnly(blockedContacts.ToArray());
        }
    }
}
