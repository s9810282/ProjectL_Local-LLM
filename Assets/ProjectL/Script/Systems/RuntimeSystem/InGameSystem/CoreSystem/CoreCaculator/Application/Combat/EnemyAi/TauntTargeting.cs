using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi
{
    /// <summary>현재 출처만 조회합니다. 공격·이동 가능 여부와 자원 정산은 기존 소유자에게 맡깁니다.</summary>
    internal static class TauntTargeting
    {
        internal static CharacterCombatFacts[] Sources(CombatCommandInput input, CharacterCombatFacts actor) =>
            Sources(input, input.Characters, actor);

        internal static IEnumerable<int> SourceSteps(CombatCommandInput input, CharacterCombatFacts actor,
            Action<IReadOnlyCollection<CharacterCombatFacts>> completed)
        {
            var sources = new SortedDictionary<(long distance, int id), CharacterCombatFacts>();
            foreach (var source in input.Characters)
            {
                if (Affects(input, source, actor, null))
                {
                    long distance = long.MaxValue;
                    foreach (var cell in source.OccupiedCells)
                        foreach (var origin in actor.OccupiedCells)
                        {
                            distance = Math.Min(distance, Distance(cell, origin));
                            yield return 0;
                        }
                    sources.Add((distance, source.EntityId.Value), source);
                }
                yield return 0;
            }
            completed(sources.Values);
        }

        /// <summary>입력과 투영 양쪽에서 같은 도발 자격을 사용한다.</summary>
        internal static CharacterCombatFacts[] Sources(CombatCommandInput input, IEnumerable<CharacterCombatFacts> characters,
            CharacterCombatFacts actor, CombatProfileReadSet profileReads = null) => characters
            .Where(source => Affects(input, source, actor, profileReads))
            .OrderBy(source => source.OccupiedCells.Min(cell => actor.OccupiedCells.Min(origin => Distance(cell, origin))))
            .ThenBy(source => source.EntityId).ToArray();

        private static bool Affects(CombatCommandInput input, CharacterCombatFacts source, CharacterCombatFacts actor,
            CombatProfileReadSet profileReads) =>
            !source.Health.IsDead && source.Area == CharacterRuntimeArea.Field &&
            source.Stats.TraitStatuses.ToArray().Any(t => t.Key == Contracts.Common.TraitKey.Taunt) &&
            CombatCommandEffects.Capture(source, CombatCommandCalculator.RequiredProfile(input, source, profileReads))
                .Any(binding => binding.Rule is TauntEffectRule rule &&
                    rule.Affects(new EffectRuleInput(binding.Origin, binding.ExpectedState), source, actor));

        private static long Distance(Contracts.Common.CellCoord cell, Contracts.Common.CellCoord origin) =>
            Math.Abs((long)cell.X - origin.X) + Math.Abs((long)cell.Y - origin.Y);
    }
}
