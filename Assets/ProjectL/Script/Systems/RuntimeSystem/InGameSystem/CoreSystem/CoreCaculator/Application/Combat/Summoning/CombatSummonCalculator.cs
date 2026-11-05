using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Summoning
{
    /// <summary>
    /// 전투 중 소환 한 번을 계산한다. 상태를 보관하지 않으며 결과는 한 명령의 변경안이다.
    /// 손패 배치와 경로를 공유하지 않고, 칸 진입 가능 여부와 도착 효과만 기존 계산을 쓴다.
    /// </summary>
    internal static class CombatSummonCalculator
    {
        /// <summary>소환체는 스테이지 적 레벨과 무관하게 레벨 1로 만든다(레벨 성장 설계 G3).</summary>
        private const int SummonLevel = 1;

        /// <summary>
        /// 행위자(input.ActorId)가 사령술로 한 개체를 만든다. 생존 상한·빈 칸 없음이면 null(정상 시도 실패)이다.
        /// 턴 종료는 호출자(보스 턴 계산)가 합성한다.
        /// </summary>
        internal static CombatCommandTransition TryCalculate(CombatCommandInput input, NecromancyTraitSpec ability,
            IReadOnlyList<IMovementTileRule> rules)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (ability == null) throw new ArgumentNullException(nameof(ability));
            return CombatCommandCalculator.Reject(input, "DeferredUnitTurnTiming");
        }

        /// <summary>같은 소환자·능력으로 만든 개체 중 살아 있는 Field·EnemyWaiting 수다. 저장하지 않고 매번 계산한다.</summary>
        internal static int LivingSummons(CombatCommandInput input, EntityId summoner, TraitKey ability) =>
            input.State.Turn.Summons.Count(r => r.SummonerId == summoner && r.Ability == ability &&
                input.Characters.Any(c => c.EntityId == r.SummonedId && !c.Health.IsDead &&
                    (c.Area == CharacterRuntimeArea.Field || c.Area == CharacterRuntimeArea.EnemyWaiting)));

        /// <summary>소환자 주변 8칸 중 보드 안·미점유·자발 진입 가능 칸이다. Y, X 순으로 고정 정렬한다(G2).</summary>
        internal static IReadOnlyList<CellCoord> Candidates(CombatCommandInput input, CellCoord origin) =>
            Candidates(input.State, input.Characters, input.State.Environment, input.EnvironmentBindings, origin);

        /// <summary>계산 중 투영(다음 밤 복귀)에서도 같은 후보 규칙을 쓰도록 현재 점유·환경을 직접 받는다.</summary>
        internal static IReadOnlyList<CellCoord> Candidates(CombatCommandState state, IEnumerable<CharacterCombatFacts> characters,
            MovementEnvironmentFacts environment, EnvironmentSemanticBindings bindings, CellCoord origin)
        {
            var occupied = characters.Where(c => !c.Health.IsDead && c.Area == CharacterRuntimeArea.Field)
                .SelectMany(c => c.OccupiedCells)
                .Concat(state.Structures.Where(s => !s.Health.IsDead).SelectMany(s => s.OccupiedCells)).ToHashSet();
            var cells = new List<CellCoord>();
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    var cell = new CellCoord(checked(origin.X + dx), checked(origin.Y + dy));
                    if (state.IsInside(cell) && !occupied.Contains(cell) &&
                        !EnvironmentTerrainRules.BlocksVoluntaryEntry(environment, bindings, cell))
                        cells.Add(cell);
                }
            return cells.OrderBy(c => c.Y).ThenBy(c => c.X).ToArray();
        }

        /// <summary>전투 준비와 같은 생성 경로로 초기 상태의 적 개체를 만든다.</summary>
        private static CharacterCombatFacts Create(CombatCommandInput input, UnitSpec spec, EntityId id, CellCoord cell, Rotation4 rotation)
        {
            var categories = CombatTurnCalculator.RequiredBattle(input).Categories ??
                throw new InvalidOperationException("소환 개체를 만들려면 전투의 카테고리 정의가 필요합니다.");
            var traits = TraitRegistry.CreateDefault();
            UnitDefinition definition = new UnitCatalog(new[] { spec }, traits, new CategoryRegistry(categories)).GetRequired(spec.Key);
            if (definition.Role != UnitRole.Regular || definition.OwnerEligibility == UnitOwnerEligibility.PlayerOnly)
                throw new InvalidOperationException("소환 대상은 적이 사용할 수 있는 일반 유닛이어야 합니다.");
            if (definition.Footprint.CellCount != 1)
                throw new NotSupportedException("다칸 유닛 소환은 연결되지 않았습니다.");
            var cells = new[] { definition.Footprint.GetOccupiedCell(0, cell, rotation) };
            return BattleCharacterCalculator.CreateEnemy(id, definition, SummonLevel, cell, rotation, cells, traits);
        }
    }
}
