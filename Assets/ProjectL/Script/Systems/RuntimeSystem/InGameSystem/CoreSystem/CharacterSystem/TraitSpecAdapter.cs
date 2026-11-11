using System;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem
{
    internal static class TraitSpecAdapter
    {
        internal static TraitSpec Read(UnitTraitData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.TraitKey != TraitKey.None && Enum.IsDefined(typeof(TraitKey), data.TraitKey) &&
                DeferredCombatFeatures.IsTraitRetired(data.TraitKey))
                throw new InvalidOperationException($"TraitKey {data.TraitKey}: Retired.");
            data.Validate();
            return data switch
            {
                FocusTraitData t => new FocusTraitSpec(t.DamagePerStack, t.MaxStacks),
                PiercingFalloffTraitData t => new PiercingFalloffTraitSpec(t.ReductionPercentPerTarget, t.MaxTargetsPerRay),
                ThornFieldTraitData t => new ThornFieldTraitSpec(t.ThornDamage, t.LifetimeUnitTurns, t.DefenseReductionPercent, t.TickPhase),
                KnockbackTraitData t => new KnockbackTraitSpec(t.DistanceCells),
                DistanceDamageScaleTraitData t => new DistanceDamageScaleTraitSpec(t.PercentByDistance),
                KillChainAttackTraitData => new KillChainAttackTraitSpec(),
                NecromancyTraitData t => new NecromancyTraitSpec(t.SummonedUnitKey, t.MaxLivingSummons, t.FirstActionDelay),
                RoarTraitData t => new RoarTraitSpec(t.HitThreshold, t.CountPerHit, t.CountPerOwnSideTurnEnd, t.CountPerOpposingSideTurnEnd),
                RegenerationTraitData t => new RegenerationTraitSpec(t.HealAmount, t.HealEveryOwnSideTurnEnds),
                IntimidationTraitData t => new IntimidationTraitSpec(t.AttackReductionPercent),
                HuntersMarkTraitData t => new HuntersMarkTraitSpec(t.BonusDamagePercent, t.DurationAffectedSideTurnEnds),
                PullTraitData t => new PullTraitSpec(t.ExtraRangeCells),
                ImpactBoltTraitData t => new ImpactBoltTraitSpec(t.DistanceCells),
                CrashImpactTraitData t => new CrashImpactTraitSpec(t.DistanceCells, t.WallDamagePercent, t.UnitDamagePercent),
                VanguardTraitData t => new VanguardTraitSpec(t.AttackBonus, t.DurationControllerTurnEnds),
                CleanSweepTraitData t => new CleanSweepTraitSpec(t.AttackBonusPerAllyAttack, t.DurationControllerTurnEnds),
                SoloActionTraitData t => new SoloActionTraitSpec(t.ShieldAmount, t.RadiusCells, t.RequiredOccupiedNeighborCount, t.ResetEveryControllerTurnEnds),
                CallOfBloodTraitData t => new CallOfBloodTraitSpec(t.EnemyMaxHealthHealPercent),
                MoraleBoostTraitData t => new MoraleBoostTraitSpec(t.DefeatedEnemyBaseAttackPercent, t.DurationControllerTurnEnds),
                DisarmTraitData t => new DisarmTraitSpec(t.DurationAffectedSideTurnEnds),
                AnkleBreakerTraitData t => new AnkleBreakerTraitSpec(t.DurationAffectedSideTurnEnds),
                CohesionTraitData t => new CohesionTraitSpec(t.DefensePerAdjacentAlly, t.ShieldAmountPerAlly, t.RadiusCells, t.ResetEveryControllerTurnEnds),
                SacrificeTraitData t => new SacrificeTraitSpec(t.RadiusCells, t.MaxTriggersPerEnemyTurn,
                    t.PreventsLethalSacrificeDamage, t.ShieldOnActivation),
                TauntTraitData t => new TauntTraitSpec(t.RadiusCells, t.DamageReductionPercent),
                EntrenchmentTraitData t => new EntrenchmentTraitSpec(t.RadiusCells, t.DamageReductionPercent),
                SteadfastWillTraitData t => new SteadfastWillTraitSpec(),
                GroundCompactionTraitData t => new GroundCompactionTraitSpec(t.MovementRangeBonus, t.OpposingTurnEndsToExpire),
                DustScreenTraitData t => new DustScreenTraitSpec(t.AttackRangeReduction, t.AttackDamageReductionPercent,
                    t.OpposingTurnEndsToExpire, t.ExemptsAlliedAttackRangePenalty),
                LeapTraitData t => new LeapTraitSpec(),
                PositionSwapTraitData t => new PositionSwapTraitSpec(t.ShieldAmount),
                ReconSquadTraitData t => new ReconSquadTraitSpec(t.IncreasePercent, t.Radius),
                AccelerationTraitData t => new AccelerationTraitSpec(t.AttackPerMovedCell),
                StationaryFireTraitData t => new StationaryFireTraitSpec(t.ExtraDamagePercent),
                ArmorBreakerTraitData t => new ArmorBreakerTraitSpec(t.DefenseReductionPercentPerHit),
                FinishingShotTraitData t => new FinishingShotTraitSpec(t.AttackThresholdPercent),
                TrackingShotTraitData t => new TrackingShotTraitSpec(t.TargetHealthThresholdPercent, t.ExtraDamagePercent),
                HunterTraitData t => new HunterTraitSpec(t.AttackPerTargetKill, t.MaxRewardsPerStage),
                FocusedBombardmentTraitData t => new FocusedBombardmentTraitSpec(t.RequiredEnemyCount, t.ExtraDamagePercent),
                RampageTraitData t => new RampageTraitSpec(t.BaseAttackBonus, 0, t.MovementBonus),
                LavaCannonTraitData => new LavaCannonTraitSpec(),
                BleedingTraitData t => new BleedingTraitSpec(t.DamagePerStack),
                ReconstructionTraitData t => new ReconstructionTraitSpec(t.Level),
                SelfDestructTraitData t => new SelfDestructTraitSpec(t.Level),
                FearTraitData => new FearTraitSpec(),
                _ => throw new InvalidOperationException($"DB 특성 정의를 변환할 수 없습니다: {data.TraitKey}.")
            };
        }
    }
}
