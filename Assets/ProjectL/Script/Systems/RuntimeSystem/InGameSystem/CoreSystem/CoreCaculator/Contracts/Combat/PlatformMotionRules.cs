using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>
    /// 이동 플랫폼 사전 이동의 운반·점유·시작 조건을 한 곳에서 정한다(설계 2026-11-17 PF-2).
    /// 실제 계산, Buffer 근거 검사, 공개 예측이 모두 이 규칙을 부르므로 한쪽만 바꿔 Core 정산 중 근거 검사가 깨지는 일을 막는다.
    /// </summary>
    public static class PlatformMotionRules
    {
        /// <summary>한 칸만 차지하는 보스가 아닌 유닛만 플랫폼이 운반한다.</summary>
        public static bool IsTransportable(CharacterCombatFacts unit)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            return unit.OccupiedCells.Count == 1 && unit.Stats.Category.Key != CategoryKey.Boss;
        }

        /// <summary>활성이고 현재 지형 바인딩에 있는 플랫폼만 그 칸을 지지한다. 낭떠러지 지지와 잔해 차단이 같은 판정을 쓴다.</summary>
        public static bool SuppliesSupport(MovingPlatformFact platform, EnvironmentSemanticBindings bindings)
        {
            if (platform == null) throw new ArgumentNullException(nameof(platform));
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            return platform.Active && bindings.TerrainSourceIds.Contains(platform.Id);
        }

        /// <summary>플랫폼 모양의 칸을 하나라도 차지한 살아 있는 필드 캐릭터다.</summary>
        public static IEnumerable<CharacterCombatFacts> Occupants(IEnumerable<CharacterCombatFacts> characters,
            MovingPlatformFact platform)
        {
            if (characters == null) throw new ArgumentNullException(nameof(characters));
            if (platform == null) throw new ArgumentNullException(nameof(platform));
            return characters.Where(c => c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead &&
                c.OccupiedCells.Any(platform.Occupies));
        }

        /// <summary>운반할 수 없는 캐릭터나 파괴되지 않은 구조물이 플랫폼 칸에 있으면 그 플랫폼은 옮기지 않는다.</summary>
        public static bool HasNontransportableOccupant(IEnumerable<CharacterCombatFacts> characters,
            IEnumerable<StructureCombatFacts> structures, MovingPlatformFact platform)
        {
            if (structures == null) throw new ArgumentNullException(nameof(structures));
            return Occupants(characters, platform).Any(c => !IsTransportable(c)) ||
                structures.Any(s => !s.IsDestroyed && s.OccupiedCells.Any(platform.Occupies));
        }

        /// <summary>
        /// 이동 시작 때 탑승자였던 유닛의 운반이 끝나는지 정한다. 죽거나, 필드를 떠나거나, 이동한 뒤의 플랫폼 모양을 벗어난 경우만 끝난다.
        /// 진입 효과로 같은 플랫폼의 다른 칸에 옮겨진 탑승자는 계속 운반하며, 다음 칸은 그 실제 위치에서 출발한다(확정 결정, 검토 PF-1).
        /// 이동 도중 플랫폼에 올라온 유닛은 탑승자가 아니며 이 규칙의 대상이 아니다(결정 Q-PF1 B).
        /// </summary>
        public static bool EndsTransport(CharacterCombatFacts rider, MovingPlatformFact platform)
        {
            if (rider == null) throw new ArgumentNullException(nameof(rider));
            if (platform == null) throw new ArgumentNullException(nameof(platform));
            return rider.Health.IsDead || rider.Area != CharacterRuntimeArea.Field || !rider.Anchor.HasValue ||
                !platform.Occupies(rider.Anchor.Value);
        }

        /// <summary>이동 축과 방향 부호로 정한 한 칸 이동량이다.</summary>
        public static CellCoord TravelOffset(MovingPlatformSpec spec, int sign)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            if (sign is not (-1 or 1)) throw new ArgumentOutOfRangeException(nameof(sign));
            return spec.TravelAxis == PlatformAxis.Horizontal ? new CellCoord(sign, 0) : new CellCoord(0, sign);
        }

        /// <summary>현재 방향으로 한 칸 더 가면 경로를 벗어나는지다. 이동한 칸이 끝점이면 그 자리에서 바로 안쪽으로 돌아선다.</summary>
        public static bool FacesRouteEnd(MovingPlatformFact platform)
        {
            if (platform == null) throw new ArgumentNullException(nameof(platform));
            return !platform.Spec.ContainsAnchor(platform.Cell + TravelOffset(platform.Spec, platform.TravelSign));
        }

        /// <summary>
        /// 전투 국면의 진영 시작 직전(자동 처리)이고, 마지막으로 처리한 경계 뒤에 끝난 진영 턴이 있으며, 플랫폼이 있으면 사전 이동 차례다.
        /// </summary>
        public static bool IsPreludeDue(CombatCommandState state, int platformCount)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (platformCount < 0) throw new ArgumentOutOfRangeException(nameof(platformCount));
            var turn = state.Turn;
            return platformCount > 0 && state.Phase == CombatFlowPhase.Automatic && turn.StagePhase == StagePhase.Combat &&
                turn.ControllerPhase is CombatControllerPhase.StartingPlayer or CombatControllerPhase.StartingEnemy &&
                turn.CompletedControllerTurnCount > state.LastPlatformBoundaryCount;
        }
    }
}
