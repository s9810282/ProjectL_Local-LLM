using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits
{
    internal sealed class TraitRegistry
    {
        private readonly Dictionary<TraitKey, TraitRegistration> _registrations;

        internal IReadOnlyList<TraitRegistration> OrderedRegistrations { get; }

        internal TraitRegistry(TraitRegistration[] registrations)
        {
            if (registrations == null)
                throw new ArgumentNullException(nameof(registrations));

            _registrations = new Dictionary<TraitKey, TraitRegistration>(registrations.Length);

            for (int i = 0; i < registrations.Length; i++)
            {
                TraitRegistration registration = registrations[i];

                if (registration == null)
                {
                    throw new ArgumentException(
                        $"Registration 목록의 {i}번 항목은 null일 수 없습니다.",
                        nameof(registrations));
                }

                if (registration.TraitKey == TraitKey.None ||
                    Enum.IsDefined(typeof(TraitKey), registration.TraitKey) == false)
                {
                    throw new ArgumentException(
                        $"Registration 목록의 {i}번 항목에 유효하지 않은 TraitKey가 있습니다.",
                        nameof(registrations));
                }

                if (DeferredCombatFeatures.IsTraitRetired(registration.TraitKey))
                    throw new ArgumentException($"TraitKey {registration.TraitKey}: Retired.", nameof(registrations));

                if (registration.TraitKey != registration.Rule.Key)
                {
                    throw new ArgumentException(
                        $"{registration.TraitKey} Registration의 Rule Key가 {registration.Rule.Key}로 일치하지 않습니다.",
                        nameof(registrations));
                }

                if (typeof(TraitSpec).IsAssignableFrom(registration.SpecType) == false ||
                    registration.SpecType.IsAbstract)
                {
                    throw new ArgumentException(
                        $"{registration.TraitKey} Registration에는 구체적인 TraitSpec 타입이 필요합니다.",
                        nameof(registrations));
                }

                if (typeof(TraitDefinition).IsAssignableFrom(registration.DefinitionType) == false ||
                    registration.DefinitionType.IsAbstract)
                {
                    throw new ArgumentException(
                        $"{registration.TraitKey} Registration에는 구체적인 TraitDefinition 타입이 필요합니다.",
                        nameof(registrations));
                }

                if (typeof(TraitRuntimeState).IsAssignableFrom(registration.RuntimeStateType) == false ||
                    registration.RuntimeStateType.IsAbstract)
                {
                    throw new ArgumentException(
                        $"{registration.TraitKey} Registration에는 구체적인 TraitRuntimeState 타입이 필요합니다.",
                        nameof(registrations));
                }

                ITraitDefinitionFactory definitionFactory =
                    registration.TraitDefinitionFactory;
                if (definitionFactory.TraitKey != registration.TraitKey ||
                    definitionFactory.SpecType != registration.SpecType ||
                    definitionFactory.DefinitionType != registration.DefinitionType)
                {
                    throw new ArgumentException(
                        $"{registration.TraitKey} Registration의 Definition Factory 조합이 선언 타입과 일치하지 않습니다.",
                        nameof(registrations));
                }

                ITraitRuntimeStateFactory runtimeStateFactory =
                    registration.TraitRuntimeStateFactory;
                if (runtimeStateFactory.TraitKey != registration.TraitKey ||
                    runtimeStateFactory.DefinitionType != registration.DefinitionType ||
                    runtimeStateFactory.RuntimeStateType != registration.RuntimeStateType)
                {
                    throw new ArgumentException(
                        $"{registration.TraitKey} Registration의 RuntimeState Factory 조합이 선언 타입과 일치하지 않습니다.",
                        nameof(registrations));
                }

                if (_registrations.TryAdd(registration.TraitKey, registration) == false)
                {
                    throw new ArgumentException(
                        $"TraitKey {registration.TraitKey}가 중복 등록되었습니다.",
                        nameof(registrations));
                }
            }

            TraitRegistration[] orderedRegistrations = registrations
                .OrderBy(registration => registration.TraitKey)
                .ToArray();

            OrderedRegistrations = Array.AsReadOnly(orderedRegistrations);
        }

        internal TraitRegistration GetRequiredRegistration(TraitKey key)
        {
            if (key == TraitKey.None || Enum.IsDefined(typeof(TraitKey), key) == false)
                throw new ArgumentOutOfRangeException(nameof(key));

            if (DeferredCombatFeatures.IsTraitRetired(key))
                throw new InvalidOperationException($"TraitKey {key}: Retired.");

            if (DeferredCombatFeatures.IsImplementationPending(key))
                throw new InvalidOperationException($"TraitKey {key}: ImplementationPending.");

            if (_registrations.TryGetValue(key, out TraitRegistration registration) == false)
            {
                throw new InvalidOperationException(
                    $"TraitKey {key}에 해당하는 Registration이 없습니다.");
            }

            return registration;
        }

        internal TraitDefinition BuildDefinition(TraitSpec spec)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            return GetRequiredRegistration(spec.TraitKey).TraitDefinitionFactory.BuildDefinition(spec);
        }

        internal static TraitRegistry CreateDefault()
        {
            using var sample = CoreProfiling.Sample(CoreProfileSection.TraitRegistryBuild);
            var focusRule = new FocusTraitRule();
            TraitRegistration focusRegistration = new TraitRegistration(
                TraitKey.Focus,
                typeof(FocusTraitSpec),
                typeof(FocusTraitDefinition),
                typeof(FocusRuntimeState),
                new FocusTraitDefinitionFactory(),
                new FocusRuntimeStateFactory(),
                focusRule,
                definition => new FocusEffectRule((FocusTraitDefinition)definition, focusRule));

            var falloffRule = new PiercingFalloffTraitRule();
            TraitRegistration piercingFalloffRegistration = new TraitRegistration(
                TraitKey.PiercingFalloff,
                typeof(PiercingFalloffTraitSpec),
                typeof(PiercingFalloffTraitDefinition),
                typeof(PiercingFalloffRuntimeState),
                new PiercingFalloffTraitDefinitionFactory(),
                new PiercingFalloffRuntimeStateFactory(),
                falloffRule,
                definition => new PiercingFalloffEffectRule(
                    (PiercingFalloffTraitDefinition)definition,
                    falloffRule));

            var knockbackRule = new KnockbackTraitRule();
            TraitRegistration knockbackRegistration = new TraitRegistration(
                TraitKey.Knockback,
                typeof(KnockbackTraitSpec),
                typeof(KnockbackTraitDefinition),
                typeof(KnockbackRuntimeState),
                new KnockbackTraitDefinitionFactory(),
                new KnockbackRuntimeStateFactory(),
                knockbackRule,
                definition => new KnockbackEffectRule(
                    (KnockbackTraitDefinition)definition,
                    knockbackRule));

            var distanceDamageScaleRule = new DistanceDamageScaleTraitRule();
            TraitRegistration distanceDamageScaleRegistration = new TraitRegistration(
                TraitKey.DistanceDamageScale,
                typeof(DistanceDamageScaleTraitSpec),
                typeof(DistanceDamageScaleTraitDefinition),
                typeof(DistanceDamageScaleRuntimeState),
                new DistanceDamageScaleTraitDefinitionFactory(),
                new DistanceDamageScaleRuntimeStateFactory(),
                distanceDamageScaleRule,
                definition => new DistanceDamageScaleEffectRule(
                    (DistanceDamageScaleTraitDefinition)definition,
                    distanceDamageScaleRule));

            var killChainAttackRule = new KillChainAttackTraitRule();
            TraitRegistration killChainAttackRegistration = new TraitRegistration(
                TraitKey.KillChainAttack,
                typeof(KillChainAttackTraitSpec),
                typeof(KillChainAttackTraitDefinition),
                typeof(KillChainAttackRuntimeState),
                new KillChainAttackTraitDefinitionFactory(),
                new KillChainAttackRuntimeStateFactory(),
                killChainAttackRule,
                definition => new KillChainAttackEffectRule(
                    (KillChainAttackTraitDefinition)definition,
                    killChainAttackRule));

            var necromancyRule = new NecromancyTraitRule();
            TraitRegistration necromancyRegistration = new TraitRegistration(
                TraitKey.Necromancy,
                typeof(NecromancyTraitSpec),
                typeof(NecromancyTraitDefinition),
                typeof(NecromancyRuntimeState),
                new NecromancyTraitDefinitionFactory(),
                new NecromancyRuntimeStateFactory(),
                necromancyRule,
                definition => new NecromancyEffectRule(
                    (NecromancyTraitDefinition)definition,
                    necromancyRule));

            var roarRule = new RoarTraitRule();
            TraitRegistration roarRegistration = new TraitRegistration(
                TraitKey.Roar,
                typeof(RoarTraitSpec),
                typeof(RoarTraitDefinition),
                typeof(RoarRuntimeState),
                new RoarTraitDefinitionFactory(),
                new RoarRuntimeStateFactory(),
                roarRule,
                definition => new RoarEffectRule(
                    (RoarTraitDefinition)definition,
                    roarRule));

            var regenerationRule = new RegenerationTraitRule();
            TraitRegistration regenerationRegistration = new TraitRegistration(
                TraitKey.Regeneration,
                typeof(RegenerationTraitSpec),
                typeof(RegenerationTraitDefinition),
                typeof(RegenerationRuntimeState),
                new RegenerationTraitDefinitionFactory(),
                new RegenerationRuntimeStateFactory(),
                regenerationRule,
                definition => new RegenerationEffectRule(
                    (RegenerationTraitDefinition)definition,
                    regenerationRule));

            var intimidationRule = new IntimidationTraitRule();
            TraitRegistration intimidationRegistration = new TraitRegistration(
                TraitKey.Intimidation,
                typeof(IntimidationTraitSpec),
                typeof(IntimidationTraitDefinition),
                typeof(IntimidationRuntimeState),
                new IntimidationTraitDefinitionFactory(),
                new IntimidationRuntimeStateFactory(),
                intimidationRule,
                definition => new IntimidationEffectRule(
                    (IntimidationTraitDefinition)definition,
                    intimidationRule));

            TraitRegistration[] traitRegistrations =
            {
                new TraitRegistration(TraitKey.Vanguard, typeof(VanguardTraitSpec), typeof(VanguardTraitDefinition),
                    typeof(VanguardRuntimeState), new VanguardTraitDefinitionFactory(), new VanguardRuntimeStateFactory(),
                    new VanguardTraitRule(), definition => new VanguardEffectRule((VanguardTraitDefinition)definition, new VanguardTraitRule())),
                new TraitRegistration(TraitKey.CleanSweep, typeof(CleanSweepTraitSpec), typeof(CleanSweepTraitDefinition),
                    typeof(CleanSweepRuntimeState), new CleanSweepTraitDefinitionFactory(), new CleanSweepRuntimeStateFactory(),
                    new CleanSweepTraitRule(), definition => new CleanSweepEffectRule((CleanSweepTraitDefinition)definition, new CleanSweepTraitRule())),
                new TraitRegistration(TraitKey.CallOfBlood, typeof(CallOfBloodTraitSpec), typeof(CallOfBloodTraitDefinition),
                    typeof(CallOfBloodRuntimeState), new CallOfBloodTraitDefinitionFactory(), new CallOfBloodRuntimeStateFactory(),
                    new CallOfBloodTraitRule(), definition => new CallOfBloodEffectRule((CallOfBloodTraitDefinition)definition, new CallOfBloodTraitRule())),
                new TraitRegistration(TraitKey.MoraleBoost, typeof(MoraleBoostTraitSpec), typeof(MoraleBoostTraitDefinition),
                    typeof(MoraleBoostRuntimeState), new MoraleBoostTraitDefinitionFactory(), new MoraleBoostRuntimeStateFactory(),
                    new MoraleBoostTraitRule(), definition => new MoraleBoostEffectRule((MoraleBoostTraitDefinition)definition, new MoraleBoostTraitRule())),
                new TraitRegistration(TraitKey.Disarm, typeof(DisarmTraitSpec), typeof(DisarmTraitDefinition),
                    typeof(DisarmRuntimeState), new DisarmTraitDefinitionFactory(), new DisarmRuntimeStateFactory(),
                    new DisarmTraitRule(), definition => new DisarmEffectRule((DisarmTraitDefinition)definition, new DisarmTraitRule())),
                new TraitRegistration(TraitKey.AnkleBreaker, typeof(AnkleBreakerTraitSpec), typeof(AnkleBreakerTraitDefinition),
                    typeof(AnkleBreakerRuntimeState), new AnkleBreakerTraitDefinitionFactory(), new AnkleBreakerRuntimeStateFactory(),
                    new AnkleBreakerTraitRule(), definition => new AnkleBreakerEffectRule((AnkleBreakerTraitDefinition)definition, new AnkleBreakerTraitRule())),
                new TraitRegistration(TraitKey.Rampage, typeof(RampageTraitSpec), typeof(RampageTraitDefinition),
                    typeof(RampageRuntimeState), new RampageTraitDefinitionFactory(), new RampageRuntimeStateFactory(),
                    new RampageTraitRule(), definition => new RampageEffectRule((RampageTraitDefinition)definition, new RampageTraitRule())),
                new TraitRegistration(TraitKey.Bleeding, typeof(BleedingTraitSpec), typeof(BleedingTraitDefinition),
                    typeof(BleedingRuntimeState), new BleedingTraitDefinitionFactory(), new BleedingRuntimeStateFactory(),
                    new BleedingTraitRule(), definition => new BleedingEffectRule((BleedingTraitDefinition)definition, new BleedingTraitRule())),
                new TraitRegistration(TraitKey.Fear, typeof(FearTraitSpec), typeof(FearTraitDefinition),
                    typeof(FearRuntimeState), new FearTraitDefinitionFactory(), new FearRuntimeStateFactory(),
                    new FearTraitRule(), definition => new FearEffectRule((FearTraitDefinition)definition, new FearTraitRule())),
                new TraitRegistration(TraitKey.SelfDestruct, typeof(SelfDestructTraitSpec), typeof(SelfDestructTraitDefinition),
                    typeof(SelfDestructRuntimeState), new SelfDestructTraitDefinitionFactory(), new SelfDestructRuntimeStateFactory(),
                    new SelfDestructTraitRule(),
                    definition => new SelfDestructEffectRule((SelfDestructTraitDefinition)definition, new SelfDestructTraitRule())),
                new TraitRegistration(TraitKey.Reconstruction, typeof(ReconstructionTraitSpec), typeof(ReconstructionTraitDefinition),
                    typeof(ReconstructionRuntimeState), new ReconstructionTraitDefinitionFactory(), new ReconstructionRuntimeStateFactory(),
                    new ReconstructionTraitRule(),
                    definition => new ReconstructionEffectRule((ReconstructionTraitDefinition)definition, new ReconstructionTraitRule())),
                new TraitRegistration(TraitKey.LavaCannon, typeof(LavaCannonTraitSpec), typeof(LavaCannonTraitDefinition),
                    typeof(LavaCannonRuntimeState), new LavaCannonTraitDefinitionFactory(), new LavaCannonRuntimeStateFactory(),
                    new LavaCannonTraitRule(), definition => new LavaCannonEffectRule((LavaCannonTraitDefinition)definition, new LavaCannonTraitRule())),
                new TraitRegistration(TraitKey.SoloAction, typeof(SoloActionTraitSpec), typeof(SoloActionTraitDefinition),
                    typeof(SoloActionRuntimeState), new SoloActionTraitDefinitionFactory(), new SoloActionRuntimeStateFactory(),
                    new SoloActionTraitRule(), definition => new SoloActionEffectRule((SoloActionTraitDefinition)definition, new SoloActionTraitRule())),
                new TraitRegistration(TraitKey.Cohesion, typeof(CohesionTraitSpec), typeof(CohesionTraitDefinition),
                    typeof(CohesionRuntimeState), new CohesionTraitDefinitionFactory(), new CohesionRuntimeStateFactory(),
                    new CohesionTraitRule(), definition => new CohesionEffectRule((CohesionTraitDefinition)definition, new CohesionTraitRule())),
                new TraitRegistration(TraitKey.Entrenchment, typeof(EntrenchmentTraitSpec), typeof(EntrenchmentTraitDefinition),
                    typeof(EntrenchmentRuntimeState), new EntrenchmentTraitDefinitionFactory(), new EntrenchmentRuntimeStateFactory(),
                    new EntrenchmentTraitRule(), definition => new EntrenchmentEffectRule((EntrenchmentTraitDefinition)definition, new EntrenchmentTraitRule())),
                new TraitRegistration(TraitKey.Sacrifice, typeof(SacrificeTraitSpec), typeof(SacrificeTraitDefinition),
                    typeof(SacrificeRuntimeState), new SacrificeTraitDefinitionFactory(), new SacrificeRuntimeStateFactory(),
                    new SacrificeTraitRule(), definition => new SacrificeEffectRule((SacrificeTraitDefinition)definition, new SacrificeTraitRule())),
                new TraitRegistration(TraitKey.Taunt, typeof(TauntTraitSpec), typeof(TauntTraitDefinition),
                    typeof(TauntRuntimeState), new TauntTraitDefinitionFactory(), new TauntRuntimeStateFactory(),
                    new TauntTraitRule(), definition => new TauntEffectRule((TauntTraitDefinition)definition, new TauntTraitRule())),
                new TraitRegistration(TraitKey.SteadfastWill, typeof(SteadfastWillTraitSpec), typeof(SteadfastWillTraitDefinition),
                    typeof(SteadfastWillRuntimeState), new SteadfastWillTraitDefinitionFactory(), new SteadfastWillRuntimeStateFactory(),
                    new SteadfastWillTraitRule(), definition => new SteadfastWillEffectRule((SteadfastWillTraitDefinition)definition, new SteadfastWillTraitRule())),
                new TraitRegistration(TraitKey.GroundCompaction, typeof(GroundCompactionTraitSpec), typeof(GroundCompactionTraitDefinition),
                    typeof(GroundCompactionRuntimeState), new GroundCompactionTraitDefinitionFactory(), new GroundCompactionRuntimeStateFactory(),
                    new GroundCompactionTraitRule(), definition => new GroundCompactionEffectRule((GroundCompactionTraitDefinition)definition, new GroundCompactionTraitRule())),
                new TraitRegistration(TraitKey.ReconSquad, typeof(ReconSquadTraitSpec), typeof(ReconSquadTraitDefinition),
                    typeof(ReconSquadRuntimeState), new ReconSquadTraitDefinitionFactory(), new ReconSquadRuntimeStateFactory(),
                    new ReconSquadTraitRule(), definition => new ReconSquadEffectRule((ReconSquadTraitDefinition)definition, new ReconSquadTraitRule())),
                new TraitRegistration(TraitKey.DustScreen, typeof(DustScreenTraitSpec), typeof(DustScreenTraitDefinition),
                    typeof(DustScreenRuntimeState), new DustScreenTraitDefinitionFactory(), new DustScreenRuntimeStateFactory(),
                    new DustScreenTraitRule(), definition => new DustScreenEffectRule((DustScreenTraitDefinition)definition, new DustScreenTraitRule())),
                new TraitRegistration(TraitKey.PositionSwap, typeof(PositionSwapTraitSpec), typeof(PositionSwapTraitDefinition),
                    typeof(PositionSwapRuntimeState), new PositionSwapTraitDefinitionFactory(), new PositionSwapRuntimeStateFactory(),
                    new PositionSwapTraitRule(), definition => new PositionSwapEffectRule((PositionSwapTraitDefinition)definition, new PositionSwapTraitRule())),
                new TraitRegistration(TraitKey.Leap, typeof(LeapTraitSpec), typeof(LeapTraitDefinition),
                    typeof(LeapRuntimeState), new LeapTraitDefinitionFactory(), new LeapRuntimeStateFactory(),
                    new LeapTraitRule(), definition => new LeapEffectRule((LeapTraitDefinition)definition, new LeapTraitRule())),
                new TraitRegistration(TraitKey.Acceleration, typeof(AccelerationTraitSpec), typeof(AccelerationTraitDefinition),
                    typeof(AccelerationRuntimeState), new AccelerationTraitDefinitionFactory(), new AccelerationRuntimeStateFactory(),
                    new AccelerationTraitRule(), definition => new AccelerationEffectRule((AccelerationTraitDefinition)definition, new AccelerationTraitRule())),
                new TraitRegistration(TraitKey.StationaryFire, typeof(StationaryFireTraitSpec), typeof(StationaryFireTraitDefinition),
                    typeof(StationaryFireRuntimeState), new StationaryFireTraitDefinitionFactory(), new StationaryFireRuntimeStateFactory(),
                    new StationaryFireTraitRule(), definition => new StationaryFireEffectRule((StationaryFireTraitDefinition)definition, new StationaryFireTraitRule())),
                new TraitRegistration(TraitKey.ArmorBreaker, typeof(ArmorBreakerTraitSpec), typeof(ArmorBreakerTraitDefinition),
                    typeof(ArmorBreakerRuntimeState), new ArmorBreakerTraitDefinitionFactory(), new ArmorBreakerRuntimeStateFactory(),
                    new ArmorBreakerTraitRule(), definition => new ArmorBreakerEffectRule((ArmorBreakerTraitDefinition)definition, new ArmorBreakerTraitRule())),
                new TraitRegistration(TraitKey.FinishingShot, typeof(FinishingShotTraitSpec), typeof(FinishingShotTraitDefinition),
                    typeof(FinishingShotRuntimeState), new FinishingShotTraitDefinitionFactory(), new FinishingShotRuntimeStateFactory(),
                    new FinishingShotTraitRule(), definition => new FinishingShotEffectRule((FinishingShotTraitDefinition)definition, new FinishingShotTraitRule())),
                new TraitRegistration(TraitKey.TrackingShot, typeof(TrackingShotTraitSpec), typeof(TrackingShotTraitDefinition),
                    typeof(TrackingShotRuntimeState), new TrackingShotTraitDefinitionFactory(), new TrackingShotRuntimeStateFactory(),
                    new TrackingShotTraitRule(), definition => new TrackingShotEffectRule((TrackingShotTraitDefinition)definition, new TrackingShotTraitRule())),
                new TraitRegistration(TraitKey.FocusedBombardment, typeof(FocusedBombardmentTraitSpec), typeof(FocusedBombardmentTraitDefinition),
                    typeof(FocusedBombardmentRuntimeState), new FocusedBombardmentTraitDefinitionFactory(), new FocusedBombardmentRuntimeStateFactory(),
                    new FocusedBombardmentTraitRule(), definition => new FocusedBombardmentEffectRule((FocusedBombardmentTraitDefinition)definition, new FocusedBombardmentTraitRule())),
                new TraitRegistration(TraitKey.Hunter, typeof(HunterTraitSpec), typeof(HunterTraitDefinition),
                    typeof(HunterRuntimeState), new HunterTraitDefinitionFactory(), new HunterRuntimeStateFactory(),
                    new HunterTraitRule(), definition => new HunterEffectRule((HunterTraitDefinition)definition, new HunterTraitRule())),
                focusRegistration,
                piercingFalloffRegistration,
                knockbackRegistration,
                distanceDamageScaleRegistration,
                killChainAttackRegistration,
                necromancyRegistration,
                roarRegistration,
                regenerationRegistration,
                intimidationRegistration,
                new TraitRegistration(TraitKey.ImpactBolt, typeof(ImpactBoltTraitSpec), typeof(ImpactBoltTraitDefinition),
                    typeof(ImpactBoltRuntimeState), new ImpactBoltTraitDefinitionFactory(), new ImpactBoltRuntimeStateFactory(),
                    new ImpactBoltTraitRule(), definition => new ImpactBoltEffectRule((ImpactBoltTraitDefinition)definition, new ImpactBoltTraitRule())),
                new TraitRegistration(TraitKey.CrashImpact, typeof(CrashImpactTraitSpec), typeof(CrashImpactTraitDefinition),
                    typeof(CrashImpactRuntimeState), new CrashImpactTraitDefinitionFactory(), new CrashImpactRuntimeStateFactory(),
                    new CrashImpactTraitRule(), definition => new CrashImpactEffectRule((CrashImpactTraitDefinition)definition, new CrashImpactTraitRule())),
                new TraitRegistration(TraitKey.DeadHand, typeof(DeadHandTraitSpec), typeof(DeadHandTraitDefinition),
                    typeof(DeadHandRuntimeState), new DeadHandTraitDefinitionFactory(), new DeadHandRuntimeStateFactory(),
                    new DeadHandTraitRule(), definition => new DeadHandEffectRule((DeadHandTraitDefinition)definition)),
                new TraitRegistration(TraitKey.ThornBomb, typeof(ThornBombTraitSpec), typeof(ThornBombTraitDefinition),
                    typeof(ThornBombRuntimeState), new ThornBombTraitDefinitionFactory(), new ThornBombRuntimeStateFactory(),
                    new ThornBombRules(), definition => new ThornBombEffectRule((ThornBombTraitDefinition)definition)),
                new TraitRegistration(TraitKey.Pull, typeof(PullTraitSpec), typeof(PullTraitDefinition),
                    typeof(PullRuntimeState), new PullTraitDefinitionFactory(), new PullRuntimeStateFactory(),
                    new PullTraitRule(), definition => new PullEffectRule((PullTraitDefinition)definition, new PullTraitRule())),
                new TraitRegistration(TraitKey.HuntersMark, typeof(HuntersMarkTraitSpec), typeof(HuntersMarkTraitDefinition),
                    typeof(HuntersMarkRuntimeState), new HuntersMarkTraitDefinitionFactory(), new HuntersMarkRuntimeStateFactory(),
                    new HuntersMarkTraitRule(), definition => new HuntersMarkEffectRule((HuntersMarkTraitDefinition)definition,
                        new HuntersMarkTraitRule())),
            };

            return new TraitRegistry(traitRegistrations);
        }
    }
}
