using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster
{
    public static class InitialDrawCalculator
    {
        /// <summary>서브 덱을 셔플하고 첫 손패로 뽑을 서브 수(startingHandSize - 1)를 담는다. 메인은 별도로 손패 한 칸을 차지한다.</summary>
        public static InitialDrawPlan Plan(IReadOnlyList<StageDrawCandidate> candidates, int seed,
            int startingHandSize = InitialDrawPlan.DefaultStartingHandSize)
        {
            if (startingHandSize < 1)
                throw new ArgumentOutOfRangeException(nameof(startingHandSize), "첫 손패는 메인 1장 이상이어야 합니다.");
            if (candidates == null)
                throw new ArgumentNullException(nameof(candidates));

            var order = new CharacterInstanceId[candidates.Count];
            var ids = new HashSet<CharacterInstanceId>();
            for (int i = 0; i < order.Length; i++)
            {
                StageDrawCandidate candidate = candidates[i];
                if (candidate == null || candidate.Id.IsEmpty || ids.Add(candidate.Id) == false)
                    throw new ArgumentException("드로우 후보는 유효하고 보유 ID가 중복되지 않아야 합니다.");
                order[i] = candidate.Id;
            }

            Array.Sort(order);
            // 기존 드로우와 같은 Fisher–Yates 셔플을 정렬된 보유 ID에 적용한다.
            var random = new Random(seed);
            for (int i = order.Length - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                (order[i], order[swapIndex]) = (order[swapIndex], order[i]);
            }

            return new InitialDrawPlan(order, startingHandSize - 1);
        }
    }
}
