using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

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

    public sealed class DeadHandTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.DeadHand;
        public int DamagePercent { get; }

        public DeadHandTraitData(int damagePercent)
        {
            DamagePercent = damagePercent;
        }

        public override void Validate()
        {
            if (DamagePercent < 0)
                throw new InvalidOperationException("죽음의 손 피해 비율은 음수일 수 없습니다.");
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
        private readonly IReadOnlyList<int> _percentByDistance;

        public override TraitKey TraitKey => TraitKey.DistanceDamageScale;

        /// <summary>입력 목록을 복사한 읽기 전용 값입니다. 입력이 null이면 null이며 Validate가 거부합니다.</summary>
        public IReadOnlyList<int> PercentByDistance => _percentByDistance;

        public DistanceDamageScaleTraitData(IReadOnlyList<int> percentByDistance)
        {
            _percentByDistance = percentByDistance == null ? null : Array.AsReadOnly(percentByDistance.ToArray());
        }

        public override void Validate()
        {
            if (_percentByDistance == null || _percentByDistance.Count == 0)
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

    /// <summary>사령술의 소환 유닛 키, 생존 상한과 첫 행동 지연입니다. 런타임 소환 기록은 보관하지 않습니다.</summary>
    public sealed class NecromancyTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.Necromancy;
        public EntityKey SummonedUnitKey { get; }
        public int MaxLivingSummons { get; }
        public long FirstActionDelay { get; }

        public NecromancyTraitData(EntityKey summonedUnitKey, int maxLivingSummons, long firstActionDelay)
        {
            SummonedUnitKey = summonedUnitKey;
            MaxLivingSummons = maxLivingSummons;
            FirstActionDelay = firstActionDelay;
        }

        public override void Validate()
        {
            if (SummonedUnitKey == EntityKey.None || !System.Enum.IsDefined(typeof(EntityKey), SummonedUnitKey))
            {
                throw new InvalidOperationException("[NecromancyTraitData] SummonedUnitKey must be a defined, non-None key.");
            }
            if (MaxLivingSummons < 1 || FirstActionDelay < 0)
            {
                throw new InvalidOperationException("[NecromancyTraitData] MaxLivingSummons must be positive and FirstActionDelay must not be negative.");
            }
        }
    }

    /// <summary>
    /// 포효 발동 카운터 값과 카운터를 쌓는 양입니다(밤 피격 1회마다, 자기 진영 턴 종료마다, 상대 진영 턴 종료마다).
    /// 현재 카운터는 런타임 상태가 소유합니다.
    /// </summary>
    public sealed class RoarTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.Roar;
        public int HitThreshold { get; }
        public int CountPerHit { get; }
        public int CountPerOwnSideTurnEnd { get; }
        public int CountPerOpposingSideTurnEnd { get; }

        public RoarTraitData(int hitThreshold, int countPerHit = 1, int countPerOwnSideTurnEnd = 0, int countPerOpposingSideTurnEnd = 0)
        {
            HitThreshold = hitThreshold;
            CountPerHit = countPerHit;
            CountPerOwnSideTurnEnd = countPerOwnSideTurnEnd;
            CountPerOpposingSideTurnEnd = countPerOpposingSideTurnEnd;
        }

        public override void Validate()
        {
            string error = RoarCountRules.FindDefinitionError(HitThreshold, CountPerHit, CountPerOwnSideTurnEnd, CountPerOpposingSideTurnEnd);
            if (error != null)
            {
                throw new InvalidOperationException(
                    $"[RoarTraitData] {error} is invalid. HitThreshold must be positive, counts must not be negative, and at least one count must be positive.");
            }
        }
    }
}
