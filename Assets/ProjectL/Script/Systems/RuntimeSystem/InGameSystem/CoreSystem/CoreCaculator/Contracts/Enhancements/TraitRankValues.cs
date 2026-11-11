using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements
{
    public sealed class TraitRankValues
    {
        public TraitKey Key { get; }
        public int Amount { get; }
        public int? SecondAmount { get; }
        public bool PreventsLethalSacrificeDamage { get; }
        public bool ExemptsAlliedAttackRangePenalty { get; }

        public TraitRankValues(TraitKey key, int amount, int? secondAmount = null,
            bool preventsLethalSacrificeDamage = false, bool exemptsAlliedAttackRangePenalty = false)
        {
            bool two = key == TraitKey.Cohesion || key == TraitKey.TrackingShot || key == TraitKey.Rampage;
            if (two != secondAmount.HasValue || amount < 0 || secondAmount < 0)
                throw new ArgumentException("등급 수치의 개수 또는 값이 올바르지 않습니다.");
            if (key == TraitKey.DustScreen && amount != 0)
                throw new ArgumentException("모래 장막 등급은 수치 변경을 지정할 수 없습니다.", nameof(amount));
            if (key == TraitKey.Bleeding && amount == 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            if ((key == TraitKey.Taunt || key == TraitKey.Entrenchment || key == TraitKey.ArmorBreaker ||
                key == TraitKey.CallOfBlood || key == TraitKey.TrackingShot) && amount > 100)
                throw new ArgumentOutOfRangeException(nameof(amount));
            if (preventsLethalSacrificeDamage && key != TraitKey.Sacrifice)
                throw new ArgumentException("희생 등급에만 치명 피해 방지를 지정할 수 있습니다.", nameof(preventsLethalSacrificeDamage));
            if (exemptsAlliedAttackRangePenalty && key != TraitKey.DustScreen)
                throw new ArgumentException("모래 장막 등급에만 아군 사거리 감소 면제를 지정할 수 있습니다.", nameof(exemptsAlliedAttackRangePenalty));
            Key = key;
            Amount = amount;
            SecondAmount = secondAmount;
            PreventsLethalSacrificeDamage = preventsLethalSacrificeDamage;
            ExemptsAlliedAttackRangePenalty = exemptsAlliedAttackRangePenalty;
        }

        internal static TraitRankValues FromNormal(TraitSpec normal) => normal switch
        {
            VanguardTraitSpec t => new(t.TraitKey, t.AttackBonus),
            CleanSweepTraitSpec t => new(t.TraitKey, t.AttackBonusPerAllyAttack),
            MoraleBoostTraitSpec t => new(t.TraitKey, t.DefeatedEnemyBaseAttackPercent),
            SoloActionTraitSpec t => new(t.TraitKey, t.ShieldAmount),
            CallOfBloodTraitSpec t => new(t.TraitKey, t.EnemyMaxHealthHealPercent),
            PositionSwapTraitSpec t => new(t.TraitKey, t.ShieldAmount),
            CohesionTraitSpec t => new(t.TraitKey, t.DefensePerAdjacentAlly, t.ShieldAmountPerAlly),
            TauntTraitSpec t => new(t.TraitKey, t.DamageReductionPercent),
            EntrenchmentTraitSpec t => new(t.TraitKey, t.DamageReductionPercent),
            GroundCompactionTraitSpec t => new(t.TraitKey, t.MovementRangeBonus),
            ReconSquadTraitSpec t => new(t.TraitKey, t.IncreasePercent),
            StationaryFireTraitSpec t => new(t.TraitKey, t.ExtraDamagePercent),
            ArmorBreakerTraitSpec t => new(t.TraitKey, t.DefenseReductionPercentPerHit),
            FinishingShotTraitSpec t => new(t.TraitKey, t.AttackThresholdPercent),
            TrackingShotTraitSpec t => new(t.TraitKey, t.TargetHealthThresholdPercent, t.ExtraDamagePercent),
            HunterTraitSpec t => new(t.TraitKey, t.AttackPerTargetKill),
            FocusedBombardmentTraitSpec t => new(t.TraitKey, t.ExtraDamagePercent),
            RampageTraitSpec t => new(t.TraitKey, t.MovementBonus, t.RankAttackBonus),
            DustScreenTraitSpec t => new(t.TraitKey, 0, exemptsAlliedAttackRangePenalty: t.ExemptsAlliedAttackRangePenalty),
            SacrificeTraitSpec t => new(t.TraitKey, t.ShieldOnActivation, preventsLethalSacrificeDamage: t.PreventsLethalSacrificeDamage),
            BleedingTraitSpec t => new(t.TraitKey, t.DamagePerStack),
            _ => throw new ArgumentException("등급 수치가 정의되지 않은 특성입니다.", nameof(normal))
        };

        internal TraitSpec Apply(TraitSpec source, TraitSpec normal)
        {
            if (source.TraitKey != Key || normal.TraitKey != Key)
                throw new ArgumentException("등급 정의와 적용 대상의 키가 다릅니다.");
            return source switch
            {
                VanguardTraitSpec t => new VanguardTraitSpec(Amount, t.DurationControllerTurnEnds),
                CleanSweepTraitSpec => new CleanSweepTraitSpec(Amount, ((CleanSweepTraitSpec)normal).DurationControllerTurnEnds),
                MoraleBoostTraitSpec t => new MoraleBoostTraitSpec(Amount, t.DurationControllerTurnEnds),
                SoloActionTraitSpec t => new SoloActionTraitSpec(Amount, t.RadiusCells, t.RequiredOccupiedNeighborCount, t.ResetEveryControllerTurnEnds),
                CallOfBloodTraitSpec => new CallOfBloodTraitSpec(Amount),
                PositionSwapTraitSpec => new PositionSwapTraitSpec(Amount),
                CohesionTraitSpec t => new CohesionTraitSpec(Amount, SecondAmount.Value, t.RadiusCells, t.ResetEveryControllerTurnEnds),
                TauntTraitSpec t => new TauntTraitSpec(t.RadiusCells, Amount),
                EntrenchmentTraitSpec t => new EntrenchmentTraitSpec(t.RadiusCells, Amount),
                GroundCompactionTraitSpec t => new GroundCompactionTraitSpec(Amount, t.OpposingTurnEndsToExpire),
                ReconSquadTraitSpec t => new ReconSquadTraitSpec(Amount, t.Radius),
                StationaryFireTraitSpec => new StationaryFireTraitSpec(Amount),
                ArmorBreakerTraitSpec => new ArmorBreakerTraitSpec(Amount),
                FinishingShotTraitSpec => new FinishingShotTraitSpec(Amount),
                TrackingShotTraitSpec => new TrackingShotTraitSpec(Amount, SecondAmount.Value),
                HunterTraitSpec t => new HunterTraitSpec(Amount, t.MaxRewardsPerStage),
                FocusedBombardmentTraitSpec t => new FocusedBombardmentTraitSpec(t.RequiredEnemyCount, Amount),
                RampageTraitSpec => new RampageTraitSpec(((RampageTraitSpec)normal).BaseAttackBonus, SecondAmount.Value, Amount),
                DustScreenTraitSpec t => new DustScreenTraitSpec(t.AttackRangeReduction, t.AttackDamageReductionPercent,
                    t.OpposingTurnEndsToExpire, ExemptsAlliedAttackRangePenalty),
                SacrificeTraitSpec t => new SacrificeTraitSpec(t.RadiusCells, t.MaxTriggersPerEnemyTurn, PreventsLethalSacrificeDamage, Amount),
                BleedingTraitSpec => new BleedingTraitSpec(Amount),
                _ => throw new ArgumentException("등급 수치가 정의되지 않은 특성입니다.", nameof(source))
            };
        }
    }
}
