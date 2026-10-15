using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    /// <summary>
    /// 유닛 특성 하나의 규칙 데이터입니다. Core가 받는 순수 C# 데이터이며 UnitTraitAuthoring이 생성합니다.
    /// 필드와 검증 규칙은 옛 Contracts의 *TraitSpec과 같습니다.
    /// </summary>
    public abstract class UnitTraitData
    {
        public abstract TraitKey TraitKey { get; }
        public abstract void Validate();
    }

    /// <summary>공격 성공 시 다음 턴에 중첩되는 추가 피해입니다. 이동하면 리셋됩니다.</summary>
    public sealed class FocusTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.Focus;
        public int DamagePerStack { get; }
        public int MaxStacks { get; }

        public FocusTraitData(int damagePerStack, int maxStacks)
        {
            DamagePerStack = damagePerStack;
            MaxStacks = maxStacks;
        }

        public override void Validate()
        {
            if (DamagePerStack <= 0 || MaxStacks <= 0)
                throw new InvalidOperationException("[FocusTraitData] DamagePerStack and MaxStacks must be positive.");
        }
    }

    /// <summary>그 턴 첫 이동 성공 시 다음 공격 비용을 깎습니다.</summary>
    public sealed class MoveAttackDiscountTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.MoveAttackDiscount;
        public int DiscountAmount { get; }
        public int MinimumAttackCost { get; }

        public MoveAttackDiscountTraitData(int discountAmount, int minimumAttackCost)
        {
            DiscountAmount = discountAmount;
            MinimumAttackCost = minimumAttackCost;
        }

        public override void Validate()
        {
            if (DiscountAmount <= 0 || MinimumAttackCost <= 0)
                throw new InvalidOperationException(
                    "[MoveAttackDiscountTraitData] DiscountAmount and MinimumAttackCost must be positive.");
        }
    }

    /// <summary>관통 공격에서 대상마다 피해가 줄어드는 비율입니다.</summary>
    public sealed class PiercingFalloffTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.PiercingFalloff;
        public int ReductionPercentPerTarget { get; }
        public int MaxTargetsPerRay { get; }

        public PiercingFalloffTraitData(int reductionPercentPerTarget, int maxTargetsPerRay)
        {
            ReductionPercentPerTarget = reductionPercentPerTarget;
            MaxTargetsPerRay = maxTargetsPerRay;
        }

        public override void Validate()
        {
            if (ReductionPercentPerTarget < 1 || ReductionPercentPerTarget > 100)
                throw new InvalidOperationException(
                    "[PiercingFalloffTraitData] ReductionPercentPerTarget must be between 1 and 100.");
            if (MaxTargetsPerRay < 0)
                throw new InvalidOperationException("[PiercingFalloffTraitData] MaxTargetsPerRay must not be negative.");
        }
    }

    /// <summary>가시밭을 남기는 특성입니다.</summary>
    public sealed class ThornFieldTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.ThornField;
        public int ThornDamage { get; }
        public int LifetimeUnitTurns { get; }

        public ThornFieldTraitData(int thornDamage, int lifetimeUnitTurns)
        {
            ThornDamage = thornDamage;
            LifetimeUnitTurns = lifetimeUnitTurns;
        }

        public override void Validate()
        {
            if (ThornDamage < 1 || LifetimeUnitTurns < 1)
                throw new InvalidOperationException(
                    "[ThornFieldTraitData] ThornDamage and LifetimeUnitTurns must be at least 1.");
        }
    }

    /// <summary>피격 대상을 밀어내는 특성입니다.</summary>
    public sealed class KnockbackTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.Knockback;
        public int DistanceCells { get; }

        public KnockbackTraitData(int distanceCells)
        {
            DistanceCells = distanceCells;
        }

        public override void Validate()
        {
            if (DistanceCells < 1)
                throw new InvalidOperationException("[KnockbackTraitData] DistanceCells must be at least 1.");
        }
    }

    /// <summary>
    /// 공격자-대상 거리별 피해 백분율입니다(샷건 산탄). 0번이 거리 1이며, 공격이 닿는 모든 거리를 덮어야 합니다.
    /// </summary>
    public sealed class DistanceDamageScaleTraitData : UnitTraitData
    {
        private readonly int[] _percentByDistance;

        public override TraitKey TraitKey => TraitKey.DistanceDamageScale;

        /// <summary>입력 배열의 사본입니다. 입력이 null이면 null이며 Validate가 거부합니다.</summary>
        public IReadOnlyList<int> PercentByDistance => _percentByDistance;

        public DistanceDamageScaleTraitData(IReadOnlyList<int> percentByDistance)
        {
            _percentByDistance = percentByDistance?.ToArray();
        }

        public override void Validate()
        {
            if (_percentByDistance == null || _percentByDistance.Length == 0)
                throw new InvalidOperationException(
                    "[DistanceDamageScaleTraitData] PercentByDistance must have at least one entry.");
            if (_percentByDistance.Any(percent => percent < 0))
                throw new InvalidOperationException(
                    "[DistanceDamageScaleTraitData] PercentByDistance entries must be zero or positive.");
        }
    }

    /// <summary>적을 처치하면 맵 전체의 랜덤한 적 캐릭터를 추가 공격하는 특성입니다(아킴보 난사). 설정값이 없습니다.</summary>
    public sealed class KillChainAttackTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.KillChainAttack;

        public override void Validate()
        {
        }
    }
}
