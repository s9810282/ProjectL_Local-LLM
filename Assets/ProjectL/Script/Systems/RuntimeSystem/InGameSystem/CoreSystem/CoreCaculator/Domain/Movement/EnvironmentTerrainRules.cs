using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement
{
    /// <summary>현재 분류된 지형에서 확정된 지역 공식을 계산한다. AP 차감과 이동 검증은 호출자가 담당한다.</summary>
    internal static class EnvironmentTerrainRules
    {
        /// <summary>벽 셀 전체의 점유만 검사한다. 공격용 모서리 절삭과 시야 계산은 이동에 적용하지 않는다.</summary>
        internal static bool IsWallOccupied(MovementEnvironmentFacts environment,
            EnvironmentSemanticBindings bindings, CellCoord cell) =>
            HasTerrain(environment, bindings, cell, StaticTerrainKind.Wall);

        internal static bool HasTerrain(MovementEnvironmentFacts environment,
            EnvironmentSemanticBindings bindings, CellCoord cell, StaticTerrainKind kind)
        {
            if (environment == null) throw new ArgumentNullException(nameof(environment));
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            if (!Enum.IsDefined(typeof(StaticTerrainKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            return environment.Tiles.OfType<StaticTerrainFact>().Any(t =>
                t.Active && t.Cell == cell && t.Kind == kind && bindings.TerrainSourceIds.Contains(t.Id));
        }

        internal static int EdgeActionCost(MovementEnvironmentFacts environment,
            EnvironmentSemanticBindings bindings, CellCoord from, CellCoord to, int baseCost)
        {
            if (baseCost < 0) throw new ArgumentOutOfRangeException(nameof(baseCost));
            bool fromHigh = HasTerrain(environment, bindings, from, StaticTerrainKind.HighGround);
            bool toHigh = HasTerrain(environment, bindings, to, StaticTerrainKind.HighGround);
            return checked(baseCost + (!fromHigh && toHigh ? 2 : 0));
        }

        internal static int SandstormRange(int inputRange)
        {
            if (inputRange < 0) throw new ArgumentOutOfRangeException(nameof(inputRange));
            return inputRange > 1 ? inputRange - 1 : inputRange;
        }

        internal static int AppointRange(CombatCommandInput input, CharacterCombatFacts actor,
            MovementEnvironmentFacts environment = null) => actor.Anchor.HasValue && HasTerrain(environment ?? input.State.Environment,
                input.EnvironmentBindings, actor.Anchor.Value, StaticTerrainKind.Sandstorm)
                ? SandstormRange(actor.Stats.AppointRange) : actor.Stats.AppointRange;

        internal static int RemainingDamagePercent(MovementEnvironmentFacts environment, EnvironmentSemanticBindings bindings,
            CellCoord? physicalOrigin, CellCoord receiver)
        {
            if (HasTerrain(environment, bindings, receiver, StaticTerrainKind.Sandstorm)) return 80;
            if (physicalOrigin.HasValue && HasTerrain(environment, bindings, receiver, StaticTerrainKind.HighGround) &&
                !HasTerrain(environment, bindings, physicalOrigin.Value, StaticTerrainKind.HighGround)) return 70;
            return 100;
        }

        internal static bool CanKnockback(MovementEnvironmentFacts environment, EnvironmentSemanticBindings bindings,
            CellCoord from, CellCoord to) => !IsWallOccupied(environment, bindings, to) &&
                (!HasTerrain(environment, bindings, to, StaticTerrainKind.HighGround) ||
                 HasTerrain(environment, bindings, from, StaticTerrainKind.HighGround));
    }
}
