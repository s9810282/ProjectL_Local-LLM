using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;
using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>Buffer 조회값을 기존 효과 규칙의 입력과 최종 조회값으로 변환한다.</summary>
    internal static class CombatCommandEffects
    {
        // 등록표는 무상태 규칙·팩토리만 공유한다. 유닛별 정의와 현재 효과 상태는 Capture에서 매번 만든다.
        private static readonly TraitRegistry _traitRegistry = TraitRegistry.CreateDefault();

        /// <param name="enemyAssessment">
        /// 참이면 적 AI 평가 정책이다(적 AI 근간 개편 설계 6.2). AI가 결과를 모형화하지 않는 출처는 공백·피해 상한·이동 투영 막기를 내지 않는다:
        /// 배치 Artifact, 장착·임시 아이템, 보유 환경, 장애물(후보가 건드릴 때만 <see cref="CombatActionAssessment"/>가 따로 센다),
        /// 그리고 효과 규칙 선언의 빠진 부분(공백 선언이 없으면 공백 없음, 피해 선언이 없으면 피해 없음).
        /// 공백 선언이 없는 규칙도 이번 후보에 피해가 미치면 기존처럼 지원 안 됨 공백을 낸다. 단, 행위자 공격 처리 안에서만 피해가 나는 규칙
        /// (넉백 계열, 피해 범위 <see cref="EffectAssessmentHarmScope.ActingAttackOnly"/>: 넉백·당기기 같은 강제 이동)은 공백·상한·막기를 내지 않는다.
        /// 그 피해는 정밀 미리보기(실제 공격 계산)가 그대로 세고, 강제 이동이 닿는 칸의 도착 원본은 <see cref="CombatActionAssessment"/>가 따로 검사한다
        /// (사용자 결정 2026-11-10: 넉백을 가진 적이 공격하지 못하던 문제의 원천 수정). 거짓이면 기존 보수 정책이다.
        /// </param>
        internal static IEnumerable<CombatAssessmentGap> AssessmentCoverageSteps(CombatCommandInput input,
            Action onUnboundedAlliedHarm = null, Action onMoveProjectionBlocked = null, bool enemyAssessment = false)
        {
            foreach (var character in input.Characters)
            {
                yield return CombatAssessmentGap.None;
                if (character.Area != CharacterRuntimeArea.Field || character.Health.IsDead) continue;
                var profile = CombatCommandCalculator.RequiredProfile(input, character);
                foreach (var binding in CaptureSteps(character, profile))
                {
                    if (binding == null || enemyAssessment && ResolvedInsideActingAttack(binding.Rule))
                    {
                        yield return CombatAssessmentGap.None;
                        continue;
                    }
                    bool harm = CanCauseUnboundedAlliedHarm(binding.Rule,
                        input.Kind == CombatCommandKind.Attack && character.EntityId == input.ActorId, enemyAssessment);
                    var gap = binding.Rule is IEffectAssessmentCoverageRule supported ? supported.AssessmentGaps
                        : !enemyAssessment || harm ? CombatAssessmentGap.UnsupportedEffect : CombatAssessmentGap.None;
                    if (gap != CombatAssessmentGap.None && harm)
                        onUnboundedAlliedHarm?.Invoke();
                    if (gap != CombatAssessmentGap.None &&
                        (binding.Rule is not IEffectMoveProjectionRule projection || !projection.SupportsMoveProjection))
                        onMoveProjectionBlocked?.Invoke();
                    yield return gap;
                }
                if (!enemyAssessment && (!character.HeldEnvironment.SameValues(CharacterHeldEnvironmentState.Empty) ||
                    character.Items.EquippedItemKey.HasValue || character.Items.Effects.Count != 0))
                {
                    onUnboundedAlliedHarm?.Invoke();
                    onMoveProjectionBlocked?.Invoke();
                    yield return CombatAssessmentGap.UnsupportedEffect;
                }
            }
            if (enemyAssessment) yield break;
            // 실제 공격 전용 반사와 아이템 연쇄는 공개 Preview가 전부 실행하지 않는다.
            if (input.Artifacts.Effects.Length != 0 || input.ArtifactItems != null && input.ArtifactItems.Count != 0)
            {
                onUnboundedAlliedHarm?.Invoke();
                onMoveProjectionBlocked?.Invoke();
                yield return CombatAssessmentGap.ActualOnlyReaction | CombatAssessmentGap.UnsupportedEffect;
            }
            if (input.EnvironmentBindings.ObstacleEntityIds.Count != 0)
            {
                onUnboundedAlliedHarm?.Invoke();
                onMoveProjectionBlocked?.Invoke();
                yield return CombatAssessmentGap.UnsupportedEffect;
            }
        }

        // 적 평가에서 정밀 미리보기에 맡기는 규칙이다: 피해가 그 행위자의 공격 처리 안에서만 나는 규칙(넉백 계열 능력, 행위자 공격 한정 피해 선언).
        // 공백을 직접 선언한 규칙은 선언을 따른다(미리보기가 모르는 실제 전용 반응 등은 선언으로 남긴다).
        private static bool ResolvedInsideActingAttack(ICombatEffectRule rule) =>
            rule is not IEffectAssessmentCoverageRule &&
            (rule is IEffectKnockbackRule ||
                rule is IEffectAssessmentHarmRule { AssessmentHarmScope: EffectAssessmentHarmScope.ActingAttackOnly });

        private static bool CanCauseUnboundedAlliedHarm(ICombatEffectRule rule, bool isActingAttack, bool enemyAssessment)
        {
            // 선언이 없는 넉백 계열은 기존 소유자 공격 조건을 따른다. 나머지 선언 없는 규칙은 기존 정책이면 보수적으로, 적 평가면 무시한다.
            if (rule is not IEffectAssessmentHarmRule declared)
                return rule is IEffectKnockbackRule ? isActingAttack : !enemyAssessment;

            var scope = declared.AssessmentHarmScope;
            return scope switch
            {
                EffectAssessmentHarmScope.NoAdditionalImmediateHarm => false,
                EffectAssessmentHarmScope.ActingAttackOnly => isActingAttack,
                EffectAssessmentHarmScope.Unbounded => true,
                _ => throw new InvalidOperationException($"{rule.GetType().Name}의 피해 평가 범위가 유효하지 않습니다: {scope}."),
            };
        }

        internal static EffectRuleBinding[] CaptureIncoming(CombatCommandInput input, IReadOnlyList<CharacterCombatFacts> characters, CharacterCombatFacts recipient, MovementEnvironmentFacts environment = null,
            CombatProfileReadSet profileReads = null)
            => CaptureIncoming(input, characters, recipient, out _, environment, profileReads);

        internal static EffectRuleBinding[] CaptureIncoming(CombatCommandInput input, IReadOnlyList<CharacterCombatFacts> characters,
            CharacterCombatFacts recipient, out IReadOnlyList<TraitTrailFact> selectedDust, MovementEnvironmentFacts environment = null,
            CombatProfileReadSet profileReads = null)
        {
            var own = Capture(recipient, CombatCommandCalculator.RequiredProfile(input, recipient, profileReads));
            var auras = characters.Where(c => c.EntityId != recipient.EntityId && !c.Health.IsDead && c.Area == CharacterRuntimeArea.Field &&
                    c.Stats.TraitStatuses.ToArray().Any(t => t.Key == TraitKey.Entrenchment))
                .SelectMany(c => Capture(c, CombatCommandCalculator.RequiredProfile(input, c, profileReads)).Where(b => b.Rule is EntrenchmentEffectRule));
            var bindings = own.Concat(auras).ToList();
            selectedDust = DustScreenTraitRule.DefenseSourcesAt(recipient, environment ?? input.State.Environment);
            var dust = selectedDust.FirstOrDefault();
            if (dust != null)
            {
                bindings.RemoveAll(b => b.Origin == dust.Origin);
                bindings.Add(new EffectRuleBinding(dust.Origin, new DustScreenEffectRule(dust), StatelessEffectStateSnapshot.Instance));
            }
            return bindings.ToArray();
        }

        internal static EffectRuleBinding[] Capture(CharacterCombatFacts character, CombatAttackProfile profile)
        {
            using var sample = CoreProfiling.Sample(CoreProfileSection.AttackEffectBinding);
            var bindings = new List<EffectRuleBinding>();
            foreach (var binding in CaptureSteps(character, profile))
                if (binding != null) bindings.Add(binding);
            return bindings.ToArray();
        }

        internal static IEnumerable<EffectRuleBinding> CaptureSteps(CharacterCombatFacts character, CombatAttackProfile profile)
        {
            var statuses = character.Stats.TraitStatuses.ToArray();
            if (statuses.Length != profile.Definition.Traits.Length)
                throw new InvalidOperationException("특성 정의와 공개 상태의 개수가 다릅니다.");
            foreach (var spec in profile.Definition.Traits.ToArray())
            {
                var status = statuses.Single(s => s.Key == spec.TraitKey);
                var registration = _traitRegistry.GetRequiredRegistration(spec.TraitKey);
                var definition = registration.TraitDefinitionFactory.BuildDefinition(spec);
                if (!DeferredCombatFeatures.IsTraitAvailable(spec.TraitKey)) { yield return null; continue; }
                yield return new EffectRuleBinding(TraitEffectOrigin.Create(character.EntityId, spec.TraitKey),
                    registration.EffectRuleFactory(definition), Capture(status));
            }
            var category = character.Stats.Category;
            if (category.Key != profile.Definition.CombatCategoryKey ||
                character.Stats.SpecializationCategory != profile.Definition.SpecializationCategory)
                throw new InvalidOperationException("캐릭터와 정의의 Category가 다릅니다.");
            if (category.Key == CategoryKey.Cannon)
                yield return new EffectRuleBinding(CategoryEffectOrigin.Create(character.EntityId, category.Key,
                    CategoryRuleKey.SafeBombardment),
                    CategoryRuleFactories.CreateRule(new SafeBombardmentCategoryRuleDefinition()),
                    StatelessEffectStateSnapshot.Instance);
            yield return null;
        }

        private static IEffectStateSnapshot Capture(TraitStatusSnapshot status) => status switch
        {
            FocusTraitStatusSnapshot f => new FocusEffectStateSnapshot(f.ActiveStacks, f.GainScheduled, f.MovedThisTurn),
            PiercingFalloffTraitStatusSnapshot => new PiercingFalloffEffectStateSnapshot(),
            ThornFieldTraitStatusSnapshot => new ThornFieldEffectStateSnapshot(),
            KnockbackTraitStatusSnapshot => new KnockbackEffectStateSnapshot(),
            DistanceDamageScaleTraitStatusSnapshot => new DistanceDamageScaleEffectStateSnapshot(),
            KillChainAttackTraitStatusSnapshot => new KillChainAttackEffectStateSnapshot(),
            NecromancyTraitStatusSnapshot => new NecromancyEffectStateSnapshot(),
            RoarTraitStatusSnapshot r => new RoarEffectStateSnapshot(r.HitCount),
            RegenerationTraitStatusSnapshot r => new RegenerationEffectStateSnapshot(r.CompletedEligibleTurnEnds),
            IntimidationTraitStatusSnapshot => new IntimidationEffectStateSnapshot(),
            HuntersMarkTraitStatusSnapshot => new HuntersMarkEffectStateSnapshot(),
            PullTraitStatusSnapshot => new PullEffectStateSnapshot(),
            ImpactBoltTraitStatusSnapshot => new ImpactBoltEffectStateSnapshot(),
            CrashImpactTraitStatusSnapshot => new CrashImpactEffectStateSnapshot(),
            StationaryFireTraitStatusSnapshot s => new StationaryFireEffectStateSnapshot(s.MovedThisOwnTurn),
            AccelerationTraitStatusSnapshot s => new AccelerationEffectStateSnapshot(s.HeldAttackBonus),
            LeapTraitStatusSnapshot => StatelessEffectStateSnapshot.Instance,
            PositionSwapTraitStatusSnapshot => StatelessEffectStateSnapshot.Instance,
            HunterTraitStatusSnapshot h => new HunterEffectStateSnapshot(h.TargetId, h.SuccessfulRewards),
            FocusedBombardmentTraitStatusSnapshot => StatelessEffectStateSnapshot.Instance,
            TrackingShotTraitStatusSnapshot => StatelessEffectStateSnapshot.Instance,
            FinishingShotTraitStatusSnapshot => StatelessEffectStateSnapshot.Instance,
            ArmorBreakerTraitStatusSnapshot => StatelessEffectStateSnapshot.Instance,
            DustScreenTraitStatusSnapshot => StatelessEffectStateSnapshot.Instance,
            GroundCompactionTraitStatusSnapshot => StatelessEffectStateSnapshot.Instance,
            ReconSquadTraitStatusSnapshot => StatelessEffectStateSnapshot.Instance,
            SteadfastWillTraitStatusSnapshot => StatelessEffectStateSnapshot.Instance,
            TauntTraitStatusSnapshot => StatelessEffectStateSnapshot.Instance,
            SacrificeTraitStatusSnapshot s => new SacrificeEffectStateSnapshot(s.LastEnemyTurnSequence, s.ActivationsThisEnemyTurn),
            EntrenchmentTraitStatusSnapshot => StatelessEffectStateSnapshot.Instance,
            CohesionTraitStatusSnapshot => StatelessEffectStateSnapshot.Instance,
            SoloActionTraitStatusSnapshot => StatelessEffectStateSnapshot.Instance,
            AnkleBreakerTraitStatusSnapshot => StatelessEffectStateSnapshot.Instance,
            BleedingTraitStatusSnapshot => StatelessEffectStateSnapshot.Instance,
            FearTraitStatusSnapshot => StatelessEffectStateSnapshot.Instance,
            SelfDestructTraitStatusSnapshot => StatelessEffectStateSnapshot.Instance,
            // 재구성 효과 규칙은 상태를 읽지 않는다. 사용 여부는 공개 상태가 원본이고 공통 피해 경계가 직접 읽는다.
            ReconstructionTraitStatusSnapshot => StatelessEffectStateSnapshot.Instance,
            RampageTraitStatusSnapshot r => new RampageEffectStateSnapshot(r.KillAttackBonus),
            LavaCannonTraitStatusSnapshot => StatelessEffectStateSnapshot.Instance,
            DisarmTraitStatusSnapshot => StatelessEffectStateSnapshot.Instance,
            MoraleBoostTraitStatusSnapshot => StatelessEffectStateSnapshot.Instance,
            CallOfBloodTraitStatusSnapshot => StatelessEffectStateSnapshot.Instance,
            CleanSweepTraitStatusSnapshot => StatelessEffectStateSnapshot.Instance,
            VanguardTraitStatusSnapshot => StatelessEffectStateSnapshot.Instance,
            _ => throw new InvalidOperationException("공개 특성 상태를 효과 입력으로 변환할 수 없습니다.")
        };

        internal static UnitStatsSnapshot Apply(UnitStatsSnapshot source, CombatEffectPlan plan)
        {
            var traits = source.TraitStatuses.ToArray();
            var category = source.Category;
            foreach (var change in plan.Changes)
            {
                if (change.Origin.Kind == EffectOwnerKind.Trait)
                {
                    int index = Array.FindIndex(traits, t => (int)t.Key == change.Origin.DefinitionKey);
                    if (index < 0 || !Capture(traits[index]).ValueEquals(change.ExpectedState))
                        throw new InvalidOperationException("효과 변경의 특성 원본이 다릅니다.");
                    traits[index] = change.FinalState switch
                    {
                        AccelerationEffectStateSnapshot state => new AccelerationTraitStatusSnapshot(state.HeldAttackBonus),
                        StationaryFireEffectStateSnapshot state => new StationaryFireTraitStatusSnapshot(state.MovedThisOwnTurn),
                        SacrificeEffectStateSnapshot s => new SacrificeTraitStatusSnapshot(s.LastEnemyTurnSequence, s.ActivationsThisEnemyTurn),
                        FocusEffectStateSnapshot f => new FocusTraitStatusSnapshot(f.ActiveStacks, f.GainScheduled, f.MovedThisTurn),
                        _ => throw new InvalidOperationException("변경 가능한 특성 상태가 아닙니다.")
                    };
                }
                else throw new InvalidOperationException("변경 가능한 Category 상태가 아닙니다.");
            }
            return AccelerationTraitRule.ProjectChange(source, With(source, traits: traits, category: category));
        }

        internal static UnitStatsSnapshot With(UnitStatsSnapshot s,
            TraitStatusSnapshot[] traits = null, CategorySnapshot? category = null,
            MoveMethod? moveMethod = null) => s.With(moveMethod: moveMethod,
            traitStatuses: traits, category: category, effectiveAttackPower: s.EffectiveAttackPower);
        internal static void AppendActivations(CombatEffectPlan plan, List<TraitActivationSnapshot> traits,
            List<CategoryActivationSnapshot> categories)
        {
            foreach (var a in plan.Activations)
            {
                var id = new EntityId(a.Origin.OwnerInstanceId);
                EntityId? target = a.TargetId.IsEmpty ? null : a.TargetId;
                if (a.Origin.Kind == EffectOwnerKind.Trait)
                    traits.Add(new TraitActivationSnapshot(id, target, TraitEffectOrigin.GetRequiredTraitKey(a.Origin, id),
                        (TraitActivationKind)a.Code, a.Magnitude));
                else if (a.Origin.Kind == EffectOwnerKind.Category)
                    categories.Add(new CategoryActivationSnapshot(id, target, CategoryEffectOrigin.GetRequiredCategoryKey(a.Origin, id),
                        CategoryEffectOrigin.GetRequiredRuleKey(a.Origin, id), (CategoryActivationKind)a.Code, a.Magnitude));
            }
        }
    }
}
