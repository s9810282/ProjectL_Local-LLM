using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Shop
{
    public static class ShopOfferPlanner
    {
        public static IReadOnlyList<ShopPurchaseSnapshot> Plan(IReadOnlyList<ShopCandidateSpec> candidates,
            IReadOnlyList<OwnedCharacterRecord> owned, Random random, int itemWeight, int unitWeight,
            IReadOnlyDictionary<EntityKey, CharacterDefinitionFacts> definitions)
        {
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));
            if (owned == null) throw new ArgumentNullException(nameof(owned));
            if (random == null) throw new ArgumentNullException(nameof(random));
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (itemWeight < 1) throw new ArgumentOutOfRangeException(nameof(itemWeight));
            if (unitWeight < 1 || (long)itemWeight + unitWeight > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(unitWeight));


            long totalLevel = 0;
            int subCount = 0;
            for (int i = 0; i < owned.Count; i++)
            {
                var character = owned[i] ?? throw new ArgumentException("보유 기록에 null을 넣을 수 없습니다.", nameof(owned));

                if (character.Role == CharacterOwnershipRole.Sub)
                {
                    totalLevel = checked(totalLevel + character.Level);
                    subCount++;
                }
            }
            int level = subCount == 0 ? 1 : (int)(totalLevel / subCount);
            var existing = new List<CharacterDefinitionFacts>();
            if (candidates.Any(x => x != null && x.Kind == ShopProductKind.Unit))
                foreach (var record in owned)
                {
                    if (!definitions.TryGetValue(record.CharacterKey, out var definition))
                        throw new InvalidOperationException("상점 소유 제한 계산에 보유 정의가 필요합니다.");
                    existing.Add(definition);
                }
            var units = new List<ShopCandidateSpec>();
            var items = new List<ShopCandidateSpec>();
            var unitKeys = new HashSet<EntityKey>();
            var unitLevels = new Dictionary<EntityKey, int>();
            var itemKeys = new HashSet<int>();
            for (int i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i] ?? throw new ArgumentException("상점 후보에 null을 넣을 수 없습니다.", nameof(candidates));
                if (candidate.Kind == ShopProductKind.Unit)
                {
                    if (!unitKeys.Add(candidate.CharacterKey))
                        throw new ArgumentException("유닛 후보가 중복되었습니다.", nameof(candidates));
                    if (!definitions.TryGetValue(candidate.CharacterKey, out var definition))
                        throw new InvalidOperationException("상점 후보 정의가 필요합니다.");
                    // 서브는 보유 서브 평균 레벨로, 메인은 소유 규칙이 정한 메인 획득 레벨로 제시한다(가격은 후보 값 그대로).
                    int offerLevel = definition.Role == UnitRole.MainCharacter ? CharacterRosterRules.MainAcquisitionLevel : level;
                    if (CharacterRosterRules.PlanAcquire(definition, existing, offerLevel).Succeeded)
                    {
                        units.Add(candidate);
                        unitLevels.Add(candidate.CharacterKey, offerLevel);
                    }
                }
                else if (candidate.Kind == ShopProductKind.ConsumableItem)
                {
                    if (!itemKeys.Add(candidate.ItemKey))
                        throw new ArgumentException("아이템 후보가 중복되었습니다.", nameof(candidates));
                    items.Add(candidate);
                }
                else
                {
                    throw new ArgumentException("상점 상품 종류가 올바르지 않습니다.", nameof(candidates));
                }
            }
            units.Sort((left, right) => left.CharacterKey.CompareTo(right.CharacterKey));
            items.Sort((left, right) => left.ItemKey.CompareTo(right.ItemKey));
            var offers = new List<ShopPurchaseSnapshot>(ShopOfferLimits.Maximum);
            while (offers.Count < ShopOfferLimits.Maximum && (items.Count != 0 || units.Count != 0))
            {
                // 종류를 먼저 뽑아 후보 수가 종류별 가중치를 바꾸지 않게 한다.
                bool selectItem = units.Count == 0 || items.Count != 0 && random.Next(itemWeight + unitWeight) < itemWeight;
                var pool = selectItem ? items : units;
                int index = random.Next(pool.Count);
                var candidate = pool[index];
                pool.RemoveAt(index);
                var id = new ShopOfferId(offers.Count + 1);
                offers.Add(selectItem
                    ? ShopPurchaseSnapshot.ForItem(id, candidate.ItemKey, candidate.Cost, candidate.ItemName, candidate.ItemDescription)
                    : ShopPurchaseSnapshot.ForUnit(id, candidate.CharacterKey, candidate.Cost, unitLevels[candidate.CharacterKey]));
            }
            return offers.AsReadOnly();
        }
    }
}
