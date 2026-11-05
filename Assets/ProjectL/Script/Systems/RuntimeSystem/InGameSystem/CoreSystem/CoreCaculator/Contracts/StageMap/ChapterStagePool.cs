using System;
using System.Collections.Generic;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap
{
    /// <summary>
    /// 한 스테이지 종류의 출현 규칙과 그 종류의 후보 스테이지 키다. 후보 키는 복사해 오름차순으로 둔다.
    /// 활성 규칙(가중치나 최소 개수가 있음)은 후보가 하나 이상 필요하다. 비활성 규칙은 후보가 없어도 되고,
    /// 후보가 있으면 무작위로는 뽑히지 않지만 명시적인 고정 노드에는 쓸 수 있다.
    /// </summary>
    public sealed class ChapterStagePool
    {
        public ChapterStageDistributionRule Rule { get; }
        /// <summary>후보 스테이지 키. 1 이상, 중복 없음, 오름차순.</summary>
        public IReadOnlyList<int> StageKeys { get; }

        public ChapterStagePool(ChapterStageDistributionRule rule, IReadOnlyList<int> stageKeys)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            if (stageKeys == null) throw new ArgumentNullException(nameof(stageKeys));

            var keys = new int[stageKeys.Count];
            for (int i = 0; i < keys.Length; i++)
            {
                if (stageKeys[i] < 1)
                    throw new ArgumentException($"{rule.Kind} 후보 스테이지 키는 1 이상이어야 합니다: {stageKeys[i]}.", nameof(stageKeys));
                keys[i] = stageKeys[i];
            }
            Array.Sort(keys);
            for (int i = 1; i < keys.Length; i++)
            {
                if (keys[i] == keys[i - 1])
                    throw new ArgumentException($"{rule.Kind} 후보 스테이지 키가 중복됩니다: {keys[i]}.", nameof(stageKeys));
            }
            if (rule.IsActive && keys.Length == 0)
                throw new ArgumentException($"{rule.Kind}는 가중치나 최소 개수가 있지만 후보 스테이지가 없습니다.", nameof(stageKeys));

            Rule = rule;
            StageKeys = Array.AsReadOnly(keys);
        }
    }
}
