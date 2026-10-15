using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Shop
{
    /// <summary>상점에서 살 수 있는 유닛 하나와 가격입니다. Core ShopCandidateSpec에 대응합니다.</summary>
    public readonly struct ShopCandidate
    {
        public EntityKey UnitKey { get; }
        public int Cost { get; }

        public ShopCandidate(EntityKey unitKey, int cost)
        {
            UnitKey = unitKey;
            Cost = cost;
        }
    }

    /// <summary>
    /// 상점 스테이지 상세 데이터입니다. Core가 받는 순수 C# 데이터이며 ShopStageData SO가 생성합니다.
    /// Core ShopStageSpec(stageKey, candidates)에 대응합니다.
    /// </summary>
    public sealed class ShopStageDetail : IStageData
    {
        public StageType StageType => StageType.Shop;

        public IReadOnlyList<ShopCandidate> Candidates { get; }

        public ShopStageDetail(ShopCandidate[] candidates)
        {
            Candidates = new ReadOnlyCollection<ShopCandidate>(candidates ?? Array.Empty<ShopCandidate>());
        }

        public void Validate()
        {
            // Core ShopStageSpec·ShopCandidateSpec과 같은 규칙입니다.
            if (Candidates.Count == 0)
                throw new InvalidOperationException("[ShopStageDetail] Shop stage requires at least one candidate.");

            var keys = new HashSet<EntityKey>();
            foreach (ShopCandidate candidate in Candidates)
            {
                if (candidate.UnitKey == EntityKey.None)
                    throw new InvalidOperationException("[ShopStageDetail] Shop candidate unit is missing.");
                if (candidate.Cost < 0)
                    throw new InvalidOperationException($"[ShopStageDetail] Cost must not be negative. Unit: {candidate.UnitKey}");
                if (!keys.Add(candidate.UnitKey))
                    throw new InvalidOperationException($"[ShopStageDetail] Duplicate shop candidate: {candidate.UnitKey}.");
            }
        }
    }
}
