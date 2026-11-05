using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;
using System.Collections.Generic;
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

        internal static bool IsCliffOccupied(MovementEnvironmentFacts environment,
            EnvironmentSemanticBindings bindings, CellCoord cell) =>
            HasTerrain(environment, bindings, cell, StaticTerrainKind.Cliff);

        /// <summary>일반 이동·배치가 공유하는 지형 진입 제한이다. 강제 이동은 별도 규칙을 쓴다.</summary>
        internal static bool BlocksVoluntaryEntry(MovementEnvironmentFacts environment,
            EnvironmentSemanticBindings bindings, CellCoord cell) =>
            IsWallOccupied(environment, bindings, cell) || IsCliffOccupied(environment, bindings, cell);

        internal static bool HasTerrain(MovementEnvironmentFacts environment,
            EnvironmentSemanticBindings bindings, CellCoord cell, StaticTerrainKind kind)
        {
            ValidateTerrainQuery(environment, bindings, kind);
            return environment.Tiles.OfType<StaticTerrainFact>().Any(t => t.Cell == cell && IsTerrain(t, bindings, kind));
        }

        /// <summary><paramref name="kind"/> 지형이 있는 칸 전체다. <see cref="HasTerrain"/>와 같은 판정을 쓴다.</summary>
        internal static IReadOnlyCollection<CellCoord> TerrainCells(MovementEnvironmentFacts environment,
            EnvironmentSemanticBindings bindings, StaticTerrainKind kind)
        {
            ValidateTerrainQuery(environment, bindings, kind);
            return environment.Tiles.OfType<StaticTerrainFact>().Where(t => IsTerrain(t, bindings, kind))
                .Select(t => t.Cell).Distinct().ToArray();
        }

        private static void ValidateTerrainQuery(MovementEnvironmentFacts environment,
            EnvironmentSemanticBindings bindings, StaticTerrainKind kind)
        {
            if (environment == null) throw new ArgumentNullException(nameof(environment));
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            if (!Enum.IsDefined(typeof(StaticTerrainKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
        }

        /// <summary>"이 원본이 그 종류의 지형인가"의 유일한 판정이다: 활성 + 종류 + 지형 의미 분류 포함.</summary>
        private static bool IsTerrain(StaticTerrainFact terrain, EnvironmentSemanticBindings bindings, StaticTerrainKind kind) =>
            terrain.Active && terrain.Kind == kind && bindings.TerrainSourceIds.Contains(terrain.Id);

        internal static int SandstormRange(int inputRange)
        {
            if (inputRange < 0) throw new ArgumentOutOfRangeException(nameof(inputRange));
            return inputRange > 1 ? inputRange - 1 : inputRange;
        }

        internal static int AppointRange(CombatCommandInput input, CharacterCombatFacts actor,
            MovementEnvironmentFacts environment = null)
        {
            environment ??= input.State.Environment;
            int range = actor.Stats.AppointRange;
            if (!actor.Anchor.HasValue) return range;
            if (HasTerrain(environment, input.EnvironmentBindings, actor.Anchor.Value, StaticTerrainKind.Sandstorm)) range = SandstormRange(range);
            return Math.Max(0, range - DustScreenTraitRule.RangeReductionAt(actor.Anchor.Value, environment));
        }

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
