using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    internal sealed class StationaryFireTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.StationaryFire;
        internal int Numerator(bool moved, int extraPercent) => moved ? 100 : checked(100 + extraPercent);
        internal static UnitStatsSnapshot RestoreMoved(UnitStatsSnapshot stats, bool moved) => stats.With(traitStatuses: stats.TraitStatuses.ToArray()
            .Select(t => t is StationaryFireTraitStatusSnapshot ? new StationaryFireTraitStatusSnapshot(moved) : t).ToArray());
        internal static UnitStatsSnapshot BeginOwnTurn(UnitStatsSnapshot stats) => stats.With(traitStatuses: stats.TraitStatuses.ToArray()
            .Select(t => t is StationaryFireTraitStatusSnapshot state && state.MovedThisOwnTurn ? new StationaryFireTraitStatusSnapshot() : t).ToArray());
    }
}
