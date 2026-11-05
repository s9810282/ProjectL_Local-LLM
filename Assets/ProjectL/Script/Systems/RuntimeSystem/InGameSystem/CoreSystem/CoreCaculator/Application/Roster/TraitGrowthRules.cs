using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster
{
    /// <summary>서브 특성 성장의 모양·열림·후보·선택을 판정한다. 현재 특성 합성은 공용 합성기에 맡긴다.</summary>
    public static class TraitGrowthRules
    {
        /// <summary>보유·강화 키와 병과 풀에서 후보를 얻고, 기존 공격 프로필 계약으로 결합 가능성을 검사한다.</summary>
        public static IReadOnlyList<TraitKey> EligibleTraits(CombatAttackProfile basis,
            IReadOnlyCollection<TraitKey> ownedTraits, IReadOnlyCollection<TraitKey> enhancedTraits,
            TraitGrowthKind kind, CategoryTraitProgressionSpec progression, TraitGrowthCatalog catalog)
        {
            if (basis == null)
                throw new ArgumentNullException(nameof(basis));
            if (ownedTraits == null)
                throw new ArgumentNullException(nameof(ownedTraits));
            if (enhancedTraits == null)
                throw new ArgumentNullException(nameof(enhancedTraits));
            if (progression == null)
                throw new ArgumentNullException(nameof(progression));
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            if (kind != TraitGrowthKind.Acquire && kind != TraitGrowthKind.Enhance)
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (basis.Definition.CategoryKey != progression.Category)
                throw new ArgumentException("유닛과 성장 설정의 병과가 다릅니다.", nameof(progression));
            var unit = OwnedUnitSpecResolver.ResolveBase(basis.Definition, catalog);
            var native = unit.Traits.ToArray().ToDictionary(t => t.TraitKey);
            var owned = new HashSet<TraitKey>(ownedTraits.Concat(native.Keys));
            var enhanced = new HashSet<TraitKey>(enhancedTraits);
            if (!enhanced.IsSubsetOf(owned))
                throw new ArgumentException("강화 키는 보유 특성에 포함되어야 합니다.", nameof(enhancedTraits));
            var current = owned.ToDictionary(k => k, k =>
            {
                var source = native.TryGetValue(k, out var trait) ? trait : catalog.GetTrait(k).Normal;
                if (!enhanced.Contains(k)) return source;
                var definition = catalog.GetTrait(k);
                return definition.IsTrainable ? definition.ResolveRank(source, 2) :
                    definition.Enhanced ?? throw new InvalidOperationException("보유 강화형 정의가 없습니다.");
            });
            var candidates = new List<TraitKey>();
            foreach (var key in progression.Pool.OrderBy(k => k))
            {
                var definition = catalog.GetTrait(key);
                if (!DeferredCombatFeatures.IsTraitAvailable(key) ||
                    (kind == TraitGrowthKind.Acquire ? owned.Contains(key) :
                        !owned.Contains(key) || enhanced.Contains(key) || definition.Enhanced == null))
                    continue;
                // 후보 검증용 일시 투영이다. 보유 기록의 합성과 저장은 기존 소유자의 책임이다.
                var projected = new Dictionary<TraitKey, TraitSpec>(current)
                {
                    [key] = kind == TraitGrowthKind.Acquire ? definition.Normal : definition.Enhanced
                };

                try
                {
                    var hypothetical = new UnitSpec(unit.Key, unit.Role, unit.FootprintWidth, unit.FootprintHeight,
                        unit.FootprintLocalCells.ToArray(), unit.MaxHealth, unit.MoveMethod, unit.AttackRange,
                        unit.CategoryKey, unit.AttackPower, unit.PhysicalDefense, unit.Priority, projected.Values.ToArray(),
                        unit.OwnerEligibility, unit.Occlusion, unit.Behavior, unit.KillReward, unit.SpecializationCategory);
                    _ = new CombatAttackProfile(hypothetical, basis.Method);
                    if (!HasDistanceDamageCoverage(hypothetical, basis.Method))
                        continue;
                    candidates.Add(key);
                }
                catch (ArgumentException)
                {
                    // 공격 프로필이 거부한 조합은 저작 검증과 실행 후보 모두에서 제외한다.
                }
            }
            return candidates.AsReadOnly();
        }

        internal static bool HasDistanceDamageCoverage(UnitSpec unit, TargetingMethodSpec method)
        {
            var traits = unit.Traits.ToArray();
            var scale = traits.OfType<DistanceDamageScaleTraitSpec>().SingleOrDefault();
            if (scale == null)
                return true;
            // 보드 끝 관통·전체 맵 추가 공격은 프로필만으로 최대 피격 거리를 정할 수 없다.
            // 해당 조합은 보드·환경 경계가 제공되기 전까지 성장 후보로 지원하지 않는다.
            if (traits.Any(t => t.TraitKey == TraitKey.PiercingFalloff || t.TraitKey == TraitKey.KillChainAttack))
                return false;
            // 기존 지정 모양은 체비셰프 사거리 안에 있고, 추가 범위의 길이는 기존 기하 제공자가 정한다.
            long maximumDistance = unit.AttackRange;
            var splash = AppointShapeCalculator.CreateSplashOffsets(method.Splash);
            if (splash.Length > 0)
                maximumDistance += splash.Max(c => Math.Max(Math.Abs(c.X), Math.Abs(c.Y)));
            return scale.PercentByDistance.Count >= maximumDistance;
        }

        public static TraitGrowthStepSpec NextStep(OwnedCharacterRecord owned, CategoryTraitProgressionSpec progression)
        {
            if (owned == null)
                throw new ArgumentNullException(nameof(owned));
            if (progression == null)
                throw new ArgumentNullException(nameof(progression));
            var completed = TraitGrowthHistory.ResolveSteps(owned, progression).Select(s => s.StepId).ToHashSet();
            var next = progression.Steps.FirstOrDefault(s => !completed.Contains(s.StepId));
            return next != null && owned.Level >= next.UnlockLevel ? next : null;
        }

        /// <summary>실제 지정 방식까지 있는 로스터 경계에서 불가능한 복원 이력을 거부한다.</summary>
        public static void ValidateHistory(OwnedCharacterRecord owned, CombatAttackProfile basis, TraitGrowthCatalog catalog)
        {
            if (basis == null) throw new ArgumentNullException(nameof(basis));
            var current = OwnedUnitSpecResolver.Resolve(basis.Definition, owned, catalog);
            try
            {
                _ = new CombatAttackProfile(current, basis.Method);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException($"성장 이력과 공격 방식이 맞지 않습니다: Unit={current.Key}, Category={current.CategoryKey}.", exception);
            }
            if (!HasDistanceDamageCoverage(current, basis.Method))
                throw new InvalidOperationException($"성장 이력의 사거리 피해 정의가 부족합니다: Unit={current.Key}, Category={current.CategoryKey}.");
        }

        public static IReadOnlyList<TraitKey> Candidates(OwnedCharacterRecord owned, CombatAttackProfile basis, TraitGrowthCatalog catalog)
        {
            if (owned == null)
                throw new ArgumentNullException(nameof(owned));
            if (basis == null)
                throw new ArgumentNullException(nameof(basis));
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            if (owned.CharacterKey != basis.Definition.Key)
                throw new InvalidOperationException("성장 대상과 공격 프로필이 다릅니다.");
            var progression = EffectiveTraitProgression.Resolve(basis.Definition, owned, catalog);
            var step = NextStep(owned, progression);
            var ranks = new Dictionary<TraitKey, int>();
            var current = OwnedUnitSpecResolver.Resolve(basis.Definition, owned, catalog, ranks);
            if (step == null)
                return Array.Empty<TraitKey>();
            var steps = TraitGrowthHistory.ResolveSteps(owned, progression);
            var enhanced = owned.Growth.Selections.Select((selection, i) => new { selection, kind = steps[i].Kind })
                .Where(s => s.kind == TraitGrowthKind.Enhance).Select(s => s.selection.TraitKey).ToArray();
            var effective = new CombatAttackProfile(current, basis.Method);
            var candidates = EligibleTraits(effective, ranks.Keys.ToArray(), enhanced, step.Kind, progression, catalog);
            return step.Kind == TraitGrowthKind.Enhance ? candidates.Where(key => ranks[key] < 3).ToArray() : candidates;
        }

        public static TraitGrowthDecision PlanChoice(OwnedCharacterRecord owned, TraitGrowthChoiceRequest request,
            TraitGrowthOffer offer, CombatAttackProfile basis, TraitGrowthCatalog catalog)
        {
            if (owned == null)
                throw new ArgumentNullException(nameof(owned));
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (basis == null)
                throw new ArgumentNullException(nameof(basis));
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            if (offer == null || offer.RunId != request.RunId || offer.OfferId != request.OfferId ||
                offer.OwnedId != request.OwnedId || offer.StepId != request.StepId || owned.Id != request.OwnedId)
                return TraitGrowthDecision.Failure(TraitGrowthFailureReason.StaleOffer);
            var step = NextStep(owned, EffectiveTraitProgression.Resolve(basis.Definition, owned, catalog));
            if (step == null || step.StepId != offer.StepId || step.Kind != offer.Kind)
                return TraitGrowthDecision.Failure(TraitGrowthFailureReason.NotUnlocked);
            if (!offer.Choices.Any(c => c.TraitKey == request.TraitKey) || !Candidates(owned, basis, catalog).Contains(request.TraitKey))
                return TraitGrowthDecision.Failure(TraitGrowthFailureReason.NotCandidate);
            var final = owned.WithGrowth(owned.Growth.Append(new TraitGrowthSelection(step.StepId, request.TraitKey, step.Kind)));
            return TraitGrowthDecision.Success(new TraitGrowthChange(owned, final));
        }
    }
}
