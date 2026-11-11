using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster
{
    /// <summary>
    /// Contracts 입력만으로 보유 전체의 배치 전 능력치를 계산하는 공개 진입점이다.
    /// 정의 생성은 전투 준비와 같은 카탈로그 경로를 쓰고, 수치는 <see cref="CharacterBaselineStatsCalculator"/>가 계산한다.
    /// 유닛은 전투와 같은 공격 프로필(정의 + 지정 방식)로 받아, 공격 무늬도 전투와 같은 지정 방식에서 만든다.
    /// 전투 유무·화면 상태는 판단하지 않으며 Buffer를 읽지 않는다.
    /// </summary>
    public static class OwnedCharacterStatsCalculator
    {
        public static IReadOnlyList<OwnedCharacterStatsSnapshot> Calculate(
            IReadOnlyList<CombatAttackProfile> profiles,
            IReadOnlyList<CategorySpec> categories,
            IReadOnlyList<OwnedCharacterRecord> owned,
            MainCharacterPersistentState main,
            IReadOnlyDictionary<CharacterInstanceId, int> souls, LevelProgressionSpec progression,
            TraitGrowthCatalog traitGrowth = null)
        {
            if (owned == null)
                throw new ArgumentNullException(nameof(owned));
            var rows = owned.Where(x => x != null && x.Role == CharacterOwnershipRole.Main).ToArray();
            if (rows.Length > 1)
                throw new InvalidOperationException("단일 메인 입력에는 메인이 한 명이어야 합니다.");
            if ((rows.Length == 1) != (main != null))
                throw new InvalidOperationException("메인 보유 기록과 메인 지속 상태는 함께 있어야 합니다.");
            var mains = new Dictionary<CharacterInstanceId, MainCharacterPersistentState>();
            if (rows.Length == 1)
                mains.Add(rows[0].Id, main);
            return CalculateForMains(profiles, categories, owned, mains, souls, progression, traitGrowth);
        }

        /// <summary>
        /// 보유 ID별 메인 상태를 한 카탈로그로 계산한다. 선택 여부는 지속 능력치를 바꾸지 않는다.
        /// <paramref name="progression"/>은 다음 레벨 진행값(<see cref="OwnedCharacterStatsSnapshot.Experience"/>)에만 쓴다.
        /// 빠뜨리면 조용히 "경험치 꺼짐"이 되므로 필수다. 꺼짐을 뜻할 때만 <c>default</c>를 넘긴다.
        /// </summary>
        public static IReadOnlyList<OwnedCharacterStatsSnapshot> CalculateForMains(
            IReadOnlyList<CombatAttackProfile> profiles, IReadOnlyList<CategorySpec> categories,
            IReadOnlyList<OwnedCharacterRecord> owned,
            IReadOnlyDictionary<CharacterInstanceId, MainCharacterPersistentState> mains,
            IReadOnlyDictionary<CharacterInstanceId, int> souls, LevelProgressionSpec progression,
            TraitGrowthCatalog traitGrowth = null)
        {
            if (profiles == null)
                throw new ArgumentNullException(nameof(profiles));
            if (categories == null)
                throw new ArgumentNullException(nameof(categories));
            if (owned == null)
                throw new ArgumentNullException(nameof(owned));
            if (mains == null)
                throw new ArgumentNullException(nameof(mains));
            if (souls == null)
                throw new ArgumentNullException(nameof(souls));

            if (owned.Count == 0)
            {
                if (mains.Count != 0)
                    throw new InvalidOperationException("메인 기록 없이 메인 지속 상태만 있을 수 없습니다.");
                return Array.AsReadOnly(Array.Empty<OwnedCharacterStatsSnapshot>());
            }

            ValidateRoster(owned, mains, souls);
            // 전투 준비와 같은 정의 생성 경로다. 특성·병과 런타임 상태는 만들지 않는다.
            var units = profiles.Select(p => p?.Definition
                ?? throw new ArgumentException("공격 프로필은 null일 수 없습니다.", nameof(profiles))).ToArray();
            var catalog = new UnitCatalog(units, TraitRegistry.CreateDefault(), new CategoryRegistry(categories));
            var items = new OwnedCharacterStatsSnapshot[owned.Count];
            for (int i = 0; i < items.Length; i++)
            {
                OwnedCharacterRecord record = owned[i];
                if (catalog.TryGet(record.CharacterKey, out UnitDefinition definition) == false)
                    throw new InvalidOperationException($"보유 캐릭터의 정의를 찾을 수 없습니다: {record.CharacterKey}.");

                var ranks = new Dictionary<TraitKey, int>();
                var profile = profiles.Single(p => p.Definition.Key == record.CharacterKey);
                var basis = profile.Definition;
                var display = new List<TraitDisplayEntry>();
                var resolved = OwnedUnitSpecResolver.Resolve(basis, record, traitGrowth ?? TraitGrowthCatalog.Disabled, ranks, display);
                if (!ReferenceEquals(basis, resolved))
                {
                    definition = new UnitCatalog(new[] { resolved }, TraitRegistry.CreateDefault(), new CategoryRegistry(categories)).GetRequired(record.CharacterKey);
                }
                bool isMain = record.Role == CharacterOwnershipRole.Main;
                UnitEligibilityRules.Require(definition, Owner.Player, isMain);
                int soulCount = souls[record.Id];
                CharacterBaselineStats baseline = CharacterBaselineStatsCalculator.ForOwned(definition, record,
                    isMain ? mains[record.Id] : null, soulCount);
                ThornFieldTraitStatusSnapshot thornFieldDescription = null;
                HuntersMarkTraitStatusSnapshot huntersMarkDescription = null;
                PullTraitStatusSnapshot pullDescription = null;
                ImpactBoltTraitStatusSnapshot impactBoltDescription = null;
                ReconSquadTraitStatusSnapshot reconSquadDescription = null;
                foreach (var trait in definition.TraitDefinitions.Span)
                {
                    if (trait is ThornFieldTraitDefinition thorn)
                        thornFieldDescription = new ThornFieldTraitStatusSnapshot(thorn.ThornDamage,
                            thorn.LifetimeUnitTurns, thorn.DefenseReductionPercent, thorn.TickPhase);
                    else if (trait is HuntersMarkTraitDefinition mark)
                        huntersMarkDescription = new HuntersMarkTraitStatusSnapshot(mark.BonusDamagePercent, mark.DurationAffectedSideTurnEnds);
                    else if (trait is PullTraitDefinition pull)
                        pullDescription = new PullTraitStatusSnapshot(pull.ExtraRangeCells);
                    else if (trait is ImpactBoltTraitDefinition bolt)
                        impactBoltDescription = new ImpactBoltTraitStatusSnapshot(bolt.DistanceCells);
                    else if (trait is ReconSquadTraitDefinition recon)
                        reconSquadDescription = new ReconSquadTraitStatusSnapshot(recon.IncreasePercent, recon.Radius);
                }
                var stats = ToSnapshot(definition, baseline);
                var pattern = ToPattern(profile.Method, stats.AttackRange);
                // 전투 중 특성 상태와 같은 원본(DeferredCombatFeatures)으로 카드에 사용 가능 여부를 싣는다.
                var availability = stats.TraitKeys.ToArray().Select(DeferredCombatFeatures.GetTraitAvailability).ToArray();
                items[i] = new OwnedCharacterStatsSnapshot(record.Id, record.CharacterKey, record.Role,
                    record.Level, ExperienceRules.Progress(record, progression), soulCount, stats, pattern, TraitSettingsSet.From(resolved, display), thornFieldDescription, huntersMarkDescription, pullDescription,
                    impactBoltDescription, availability, reconSquadDescription, ranks);
            }

            return Array.AsReadOnly(items);
        }

        private static void ValidateRoster(IReadOnlyList<OwnedCharacterRecord> owned,
            IReadOnlyDictionary<CharacterInstanceId, MainCharacterPersistentState> mains, IReadOnlyDictionary<CharacterInstanceId, int> souls)
        {
            var ids = new HashSet<CharacterInstanceId>();
            int mainCount = 0;
            foreach (OwnedCharacterRecord record in owned)
            {
                if (record == null || ids.Add(record.Id) == false)
                    throw new InvalidOperationException("보유 목록에 null 항목이나 중복 보유 ID가 있습니다.");
                if (record.Role == CharacterOwnershipRole.Main)
                {
                    mainCount++;
                    if (!mains.TryGetValue(record.Id, out var state) || state == null)
                        throw new InvalidOperationException("보유 ID에 대응하는 메인 상태가 필요합니다.");
                }
                if (souls.TryGetValue(record.Id, out int count) == false || count < 0)
                    throw new InvalidOperationException($"보유 캐릭터 {record.Id}의 영혼 기록이 필요합니다.");
            }

            if (mainCount != mains.Count || mains.Keys.Any(id => !ids.Contains(id)))
                throw new InvalidOperationException("메인 보유 기록과 메인 지속 상태는 함께 있어야 합니다.");
        }

        /// <summary>
        /// 전투 공격 범위의 첫 단계와 같은 <see cref="AppointRangeCalculator"/>로 공격자 칸 (0,0)·아군 정면 무늬를 만든다.
        /// 전장·특성 보정은 넣지 않는다. 칸 순서는 전투 범위와 같은 Y·X 순서다.
        /// </summary>
        private static AttackPatternSnapshot ToPattern(TargetingMethodSpec method, int attackRange)
        {
            var range = new AppointRangeCalculator().CalculateRangeCells(method, attackRange, CellCoord.Zero, Owner.Player)
                .OrderBy(c => c.Y).ThenBy(c => c.X).ToArray();
            return new AttackPatternSnapshot(method, range, AppointShapeCalculator.CreateSplashOffsets(method.Splash));
        }

        private static CharacterBaselineStatsSnapshot ToSnapshot(UnitDefinition definition,
            CharacterBaselineStats baseline)
        {
            var traitKeys = new TraitKey[definition.TraitDefinitions.Length];
            var traits = definition.TraitDefinitions.Span;
            for (int i = 0; i < traitKeys.Length; i++)
                traitKeys[i] = traits[i].TraitKey;

            return new CharacterBaselineStatsSnapshot(baseline.InitialCurrentHealth,baseline.MaxHealth,                baseline.EffectiveAttackPower,baseline.PhysicalDefense,baseline.MoveMethod,baseline.MoveRange,baseline.AttackRange,                baseline.Priority,definition.CategoryKey,traitKeys,definition.SpecializationCategory);
        }
    }
}
