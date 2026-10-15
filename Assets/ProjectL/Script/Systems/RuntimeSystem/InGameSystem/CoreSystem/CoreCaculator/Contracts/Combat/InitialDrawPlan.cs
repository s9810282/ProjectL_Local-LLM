using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>
    /// 스테이지 덱의 셔플 순서와 첫 손패로 뽑을 서브 수다. 첫 손패는 메인 1장 + 서브 InitialCount장이다(첫 손패에만 적용).
    /// </summary>
    public sealed class InitialDrawPlan
    {
        /// <summary>메인을 포함한 기본 첫 손패 수(메인 1 + 서브 3). 시작 로드아웃 설정이 없는 Run(개발·테스트 로스터)에서 쓴다.</summary>
        public const int DefaultStartingHandSize = 4;

        public IReadOnlyList<CharacterInstanceId> Order { get; }
        /// <summary>첫 손패로 뽑을 서브 수. 요청 수와 덱 크기 중 작은 값이다.</summary>
        public int InitialCount { get; }

        /// <param name="initialSubDraws">첫 손패로 뽑을 서브 수(메인 제외). 기본은 DefaultStartingHandSize - 1이다.</param>
        public InitialDrawPlan(IReadOnlyList<CharacterInstanceId> order, int initialSubDraws = DefaultStartingHandSize - 1)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));
            if (initialSubDraws < 0)
                throw new ArgumentOutOfRangeException(nameof(initialSubDraws));

            var copy = new CharacterInstanceId[order.Count];
            var ids = new HashSet<CharacterInstanceId>();
            for (int i = 0; i < copy.Length; i++)
            {
                if (order[i].IsEmpty || ids.Add(order[i]) == false)
                    throw new ArgumentException("드로우 순서의 보유 ID는 비어 있거나 중복될 수 없습니다.");
                copy[i] = order[i];
            }

            Order = Array.AsReadOnly(copy);
            InitialCount = Math.Min(initialSubDraws, copy.Length);
        }
    }
}
