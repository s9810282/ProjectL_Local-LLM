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
            CharacterCombatFacts actor, CombatProfileReadSet profileReads = null)
        {
            List<(long distance, CharacterCombatFacts source)> found = null;
            foreach (var source in characters)
            {
                if (!Affects(input, source, actor, profileReads)) continue;
                long distance = long.MaxValue;
                foreach (var cell in source.OccupiedCells)
                    foreach (var origin in actor.OccupiedCells)
                        distance = Math.Min(distance, Distance(cell, origin));
                (found ??= new List<(long, CharacterCombatFacts)>()).Add((distance, source));
            }
            if (found == null) return System.Array.Empty<CharacterCombatFacts>();
            for (int i = 1; i < found.Count; i++)
            {
                var item = found[i];
                int j = i - 1;
                while (j >= 0 && (found[j].distance > item.distance ||
                    found[j].distance == item.distance && found[j].source.EntityId.CompareTo(item.source.EntityId) > 0))
                {
                    found[j + 1] = found[j];
                    j--;
                }
                found[j + 1] = item;
            }
            var result = new CharacterCombatFacts[found.Count];
            for (int i = 0; i < result.Length; i++) result[i] = found[i].source;
            return result;
        }

        private static bool Affects(CombatCommandInput input, CharacterCombatFacts source, CharacterCombatFacts actor,
            CombatProfileReadSet profileReads)
        {
            if (source.Health.IsDead || source.Area != CharacterRuntimeArea.Field) return false;
            bool taunt = false;
            foreach (var t in source.Stats.TraitStatuses.Span)
                if (t.Key == Contracts.Common.TraitKey.Taunt) { taunt = true; break; }
            if (!taunt) return false;
            foreach (var binding in CombatCommandEffects.Capture(source, CombatCommandCalculator.RequiredProfile(input, source, profileReads)))
                if (binding.Rule is TauntEffectRule rule &&
                    rule.Affects(new EffectRuleInput(binding.Origin, binding.ExpectedState), source, actor)) return true;
            return false;
        }

        private static long Distance(Contracts.Common.CellCoord cell, Contracts.Common.CellCoord origin) =>
            Math.Abs((long)cell.X - origin.X) + Math.Abs((long)cell.Y - origin.Y);
    }
}
