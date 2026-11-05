using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster
{
    public static class CharacterRosterRules
    {
        /// <summary>메인 획득의 유일한 레벨이다. 획득 판정(PlanAcquire)과 상점 제시 레벨이 이 값을 함께 쓴다.</summary>
        public const int MainAcquisitionLevel = 1;

        /// <summary>같은 정의(키)를 몇 개까지 보유하는가. 메인·서브 모두 하나다(중복 서브 금지 설계 U1).</summary>
        public const int PerDefinitionLimit = 1;

        /// <summary>서브 카테고리(Cannon·Rider·Heavy)마다 몇 개까지 보유하는가. 서로 다른 정의를 섞어 센다(런 덱 설계 D3).</summary>
        public const int SubCategoryLimit = 2;

        public static CharacterAcquisitionDecision PlanInitialRoster(CharacterDefinitionFacts main,
            IReadOnlyList<CharacterDefinitionFacts> startingSubs, bool rosterIsEmpty)
        {
            if (startingSubs == null)
                throw new ArgumentNullException(nameof(startingSubs));
            if (rosterIsEmpty == false)
                return CharacterAcquisitionDecision.Failure(RosterAcquireFailureReason.InvalidRequest);
            if (!IsSupportedDefinition(main) || main.Role != UnitRole.MainCharacter)
                return CharacterAcquisitionDecision.Failure(RosterAcquireFailureReason.InvalidDefinition);
            if (main.InitialMain == null ||
                main.InitialMain.CurrentHealth != main.InitialMain.PersistentMaxHealth)
                return CharacterAcquisitionDecision.Failure(RosterAcquireFailureReason.InvalidMainState);

            var requests = new RosterAcquireRequest[startingSubs.Count + 1];
            requests[0] = new RosterAcquireRequest(main.Key, CharacterOwnershipRole.Main, initialMain: main.InitialMain);
            for (int i = 0; i < startingSubs.Count; i++)
            {
                if (!IsSupportedDefinition(startingSubs[i]) || startingSubs[i].Role != UnitRole.Regular)
                    return CharacterAcquisitionDecision.Failure(RosterAcquireFailureReason.InvalidDefinition);
                requests[i + 1] = new RosterAcquireRequest(startingSubs[i].Key, CharacterOwnershipRole.Sub);
            }

            var resulting = new List<CharacterDefinitionFacts> { main };
            resulting.AddRange(startingSubs);
            var reason = ValidateOwnership(resulting);
            return reason == RosterAcquireFailureReason.None
                ? CharacterAcquisitionDecision.Success(requests) : CharacterAcquisitionDecision.Failure(reason);
        }

        /// <summary>획득할 인스턴스를 기존 보유 정의와 함께 같은 정책으로 판정한다.</summary>
        public static CharacterAcquisitionDecision PlanAcquire(CharacterDefinitionFacts definition,
            IReadOnlyList<CharacterDefinitionFacts> existingInstances, int initialLevel = 1)
        {
            if (existingInstances == null) throw new ArgumentNullException(nameof(existingInstances));
            if (initialLevel < 1) throw new ArgumentOutOfRangeException(nameof(initialLevel));
            if (!IsSupportedDefinition(definition))
                return CharacterAcquisitionDecision.Failure(RosterAcquireFailureReason.InvalidDefinition);
            bool main = definition.Role == UnitRole.MainCharacter;
            if (main && (initialLevel != MainAcquisitionLevel || definition.InitialMain == null ||
                definition.InitialMain.CurrentHealth != definition.InitialMain.PersistentMaxHealth))
                return CharacterAcquisitionDecision.Failure(RosterAcquireFailureReason.InvalidMainState);
            var resulting = new List<CharacterDefinitionFacts>(existingInstances) { definition };
            var reason = ValidateOwnership(resulting);
            if (reason != RosterAcquireFailureReason.None) return CharacterAcquisitionDecision.Failure(reason);
            return CharacterAcquisitionDecision.Success(new[] { new RosterAcquireRequest(definition.Key,
                main ? CharacterOwnershipRole.Main : CharacterOwnershipRole.Sub, initialLevel,
                main ? definition.InitialMain : null) });
        }

        /// <summary>
        /// 모든 획득 적용 후의 인스턴스를 함께 검증한다. 실패 시 입력은 수정하지 않는다.
        /// 보유 한도의 유일한 판정이다: 정의당 <see cref="PerDefinitionLimit"/>, 서브는 카테고리당 <see cref="SubCategoryLimit"/>.
        /// </summary>
        public static RosterAcquireFailureReason ValidateOwnership(IReadOnlyList<CharacterDefinitionFacts> resultingInstances)
        {
            if (resultingInstances == null) throw new ArgumentNullException(nameof(resultingInstances));
            var definitionCounts = new Dictionary<EntityKey, int>();
            var subCategoryCounts = new Dictionary<CategoryKey, int>();
            foreach (var definition in resultingInstances)
            {
                if (!IsSupportedDefinition(definition)) return RosterAcquireFailureReason.InvalidDefinition;
                if (Increment(definitionCounts, definition.Key) > PerDefinitionLimit)
                    return RosterAcquireFailureReason.OwnershipLimitReached;
                if (definition.Role != UnitRole.MainCharacter &&
                    Increment(subCategoryCounts, definition.Category) > SubCategoryLimit)
                    return RosterAcquireFailureReason.OwnershipLimitReached;
            }
            return RosterAcquireFailureReason.None;
        }

        private static int Increment<TKey>(Dictionary<TKey, int> counts, TKey key)
        {
            counts.TryGetValue(key, out int count);
            return counts[key] = count + 1;
        }

        internal static bool IsSupportedDefinition(CharacterDefinitionFacts definition)
        {
            if (definition == null) return false;
            try
            {
                PlayerMainDefinitionPolicy.Validate(definition.Key, definition.Role, definition.Category, definition.OwnerEligibility);
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            if (definition.Role == UnitRole.MainCharacter) return true;
            return definition.OwnerEligibility != UnitOwnerEligibility.EnemyOnly &&
                (definition.Category == CategoryKey.Cannon || definition.Category == CategoryKey.Rider || definition.Category == CategoryKey.Heavy);
        }
    }
}
