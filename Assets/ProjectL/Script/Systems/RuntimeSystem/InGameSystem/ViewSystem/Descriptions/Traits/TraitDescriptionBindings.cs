using System.Collections.Generic;
using System.Collections.ObjectModel;
using ProjectL.Systems.RuntimeSystem.Common.Descriptions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Descriptions.Traits
{
    /// <summary>특성별 허용 토큰과 최종 설정값 읽기의 원본입니다. 보유·전투는 같은 등록을 사용합니다.</summary>
    internal static class TraitDescriptionBindings
    {
        private static readonly IReadOnlyDictionary<TraitKey, TraitDescriptionBinding> Entries =
            new ReadOnlyDictionary<TraitKey, TraitDescriptionBinding>(new Dictionary<TraitKey, TraitDescriptionBinding>
            {
                [TraitKey.Acceleration] = new(
                    DescriptionToken<TraitSpec>.From<AccelerationTraitSpec>("이동칸당공격력증가량", value => value.AttackPerMovedCell)),
                [TraitKey.AnkleBreaker] = new(
                    DescriptionToken<TraitSpec>.From<AnkleBreakerTraitSpec>("지속횟수", value => value.DurationAffectedSideTurnEnds)),
                [TraitKey.ArmorBreaker] = new(
                    DescriptionToken<TraitSpec>.From<ArmorBreakerTraitSpec>("방어력감소율", value => value.DefenseReductionPercentPerHit)),
                [TraitKey.CallOfBlood] = new(
                    DescriptionToken<TraitSpec>.From<CallOfBloodTraitSpec>("회복률", value => value.EnemyMaxHealthHealPercent)),
                [TraitKey.CleanSweep] = new(
                    DescriptionToken<TraitSpec>.From<CleanSweepTraitSpec>("아군공격당공격력증가량", value => value.AttackBonusPerAllyAttack),
                    DescriptionToken<TraitSpec>.From<CleanSweepTraitSpec>("지속횟수", value => value.DurationControllerTurnEnds)),
                [TraitKey.Cohesion] = new(
                    DescriptionToken<TraitSpec>.From<CohesionTraitSpec>("아군당방어력증가량", value => value.DefensePerAdjacentAlly),
                    DescriptionToken<TraitSpec>.From<CohesionTraitSpec>("아군당보호막량", value => value.ShieldAmountPerAlly),
                    DescriptionToken<TraitSpec>.From<CohesionTraitSpec>("반경", value => value.RadiusCells),
                    DescriptionToken<TraitSpec>.From<CohesionTraitSpec>("초기화주기", value => value.ResetEveryControllerTurnEnds)),
                [TraitKey.CrashImpact] = new(
                    DescriptionToken<TraitSpec>.From<CrashImpactTraitSpec>("날아가는거리", value => value.DistanceCells),
                    DescriptionToken<TraitSpec>.From<CrashImpactTraitSpec>("벽피해율", value => value.WallDamagePercent),
                    DescriptionToken<TraitSpec>.From<CrashImpactTraitSpec>("유닛피해율", value => value.UnitDamagePercent)),
                // 피해 계수는 현재 등급의 값입니다(관통·죽음의 손 View 설계 §3.9).
                [TraitKey.DeadHand] = new(
                    DescriptionToken<TraitSpec>.From<DeadHandTraitSpec>("피해계수", value => value.DamagePercent)),
                [TraitKey.Disarm] = new(
                    DescriptionToken<TraitSpec>.From<DisarmTraitSpec>("지속횟수", value => value.DurationAffectedSideTurnEnds)),
                [TraitKey.DustScreen] = new(
                    DescriptionToken<TraitSpec>.From<DustScreenTraitSpec>("사거리감소량", value => value.AttackRangeReduction),
                    DescriptionToken<TraitSpec>.From<DustScreenTraitSpec>("피해감소율", value => value.AttackDamageReductionPercent),
                    DescriptionToken<TraitSpec>.From<DustScreenTraitSpec>("지속횟수", value => value.OpposingTurnEndsToExpire)),
                [TraitKey.Entrenchment] = new(
                    DescriptionToken<TraitSpec>.From<EntrenchmentTraitSpec>("반경", value => value.RadiusCells),
                    DescriptionToken<TraitSpec>.From<EntrenchmentTraitSpec>("피해감소율", value => value.DamageReductionPercent)),
                [TraitKey.FinishingShot] = new(
                    DescriptionToken<TraitSpec>.From<FinishingShotTraitSpec>("처치기준율", value => value.AttackThresholdPercent)),
                [TraitKey.Focus] = new(
                    DescriptionToken<TraitSpec>.From<FocusTraitSpec>("중첩당추가피해량", value => value.DamagePerStack),
                    DescriptionToken<TraitSpec>.From<FocusTraitSpec>("최대중첩수", value => value.MaxStacks)),
                [TraitKey.FocusedBombardment] = new(
                    DescriptionToken<TraitSpec>.From<FocusedBombardmentTraitSpec>("필요적수", value => value.RequiredEnemyCount),
                    DescriptionToken<TraitSpec>.From<FocusedBombardmentTraitSpec>("피해증가율", value => value.ExtraDamagePercent)),
                [TraitKey.GroundCompaction] = new(
                    DescriptionToken<TraitSpec>.From<GroundCompactionTraitSpec>("이동범위증가량", value => value.MovementRangeBonus),
                    DescriptionToken<TraitSpec>.From<GroundCompactionTraitSpec>("지속횟수", value => value.OpposingTurnEndsToExpire)),
                [TraitKey.HuntersMark] = new(
                    DescriptionToken<TraitSpec>.From<HuntersMarkTraitSpec>("피해증가율", value => value.BonusDamagePercent),
                    DescriptionToken<TraitSpec>.From<HuntersMarkTraitSpec>("지속횟수", value => value.DurationAffectedSideTurnEnds)),
                [TraitKey.Hunter] = new(
                    DescriptionToken<TraitSpec>.From<HunterTraitSpec>("처치당공격력증가량", value => value.AttackPerTargetKill),
                    DescriptionToken<TraitSpec>.From<HunterTraitSpec>("최대보상횟수", value => value.MaxRewardsPerStage)),
                [TraitKey.ImpactBolt] = new(
                    DescriptionToken<TraitSpec>.From<ImpactBoltTraitSpec>("밀치기거리", value => value.DistanceCells)),
                [TraitKey.Intimidation] = new(
                    DescriptionToken<TraitSpec>.From<IntimidationTraitSpec>("공격력감소율", value => value.AttackReductionPercent)),
                [TraitKey.Knockback] = new(
                    DescriptionToken<TraitSpec>.From<KnockbackTraitSpec>("밀치기거리", value => value.DistanceCells)),
                [TraitKey.MoraleBoost] = new(
                    DescriptionToken<TraitSpec>.From<MoraleBoostTraitSpec>("공격력증가율", value => value.DefeatedEnemyBaseAttackPercent),
                    DescriptionToken<TraitSpec>.From<MoraleBoostTraitSpec>("지속횟수", value => value.DurationControllerTurnEnds)),
                [TraitKey.Necromancy] = new(
                    DescriptionToken<TraitSpec>.From<NecromancyTraitSpec>("소환수한도", value => value.MaxLivingSummons),
                    DescriptionToken<TraitSpec>.From<NecromancyTraitSpec>("첫행동지연", value => value.FirstActionDelay)),
                [TraitKey.PiercingFalloff] = new(
                    DescriptionToken<TraitSpec>.From<PiercingFalloffTraitSpec>("대상당피해감소율", value => value.ReductionPercentPerTarget),
                    DescriptionToken<TraitSpec>.From<PiercingFalloffTraitSpec>("최대관통대상수", value => value.MaxTargetsPerRay)),
                [TraitKey.PositionSwap] = new(
                    DescriptionToken<TraitSpec>.From<PositionSwapTraitSpec>("보호막량", value => value.ShieldAmount)),
                [TraitKey.Pull] = new(
                    DescriptionToken<TraitSpec>.From<PullTraitSpec>("추가사거리", value => value.ExtraRangeCells)),
                [TraitKey.ReconSquad] = new(
                    DescriptionToken<TraitSpec>.From<ReconSquadTraitSpec>("피해증가율", value => value.IncreasePercent),
                    DescriptionToken<TraitSpec>.From<ReconSquadTraitSpec>("반경", value => value.Radius)),
                [TraitKey.Regeneration] = new(
                    DescriptionToken<TraitSpec>.From<RegenerationTraitSpec>("회복량", value => value.HealAmount),
                    DescriptionToken<TraitSpec>.From<RegenerationTraitSpec>("회복주기", value => value.HealEveryOwnSideTurnEnds)),
                [TraitKey.Roar] = new(
                    DescriptionToken<TraitSpec>.From<RoarTraitSpec>("필요피격횟수", value => value.HitThreshold),
                    DescriptionToken<TraitSpec>.From<RoarTraitSpec>("피격당카운트", value => value.CountPerHit),
                    DescriptionToken<TraitSpec>.From<RoarTraitSpec>("자기턴종료카운트", value => value.CountPerOwnSideTurnEnd),
                    DescriptionToken<TraitSpec>.From<RoarTraitSpec>("상대턴종료카운트", value => value.CountPerOpposingSideTurnEnd)),
                [TraitKey.Sacrifice] = new(
                    DescriptionToken<TraitSpec>.From<SacrificeTraitSpec>("반경", value => value.RadiusCells),
                    DescriptionToken<TraitSpec>.From<SacrificeTraitSpec>("최대발동횟수", value => value.MaxTriggersPerEnemyTurn)),
                [TraitKey.SoloAction] = new(
                    DescriptionToken<TraitSpec>.From<SoloActionTraitSpec>("보호막량", value => value.ShieldAmount),
                    DescriptionToken<TraitSpec>.From<SoloActionTraitSpec>("반경", value => value.RadiusCells),
                    DescriptionToken<TraitSpec>.From<SoloActionTraitSpec>("필요주변점유칸수", value => value.RequiredOccupiedNeighborCount),
                    DescriptionToken<TraitSpec>.From<SoloActionTraitSpec>("초기화주기", value => value.ResetEveryControllerTurnEnds)),
                [TraitKey.StationaryFire] = new(
                    DescriptionToken<TraitSpec>.From<StationaryFireTraitSpec>("피해증가율", value => value.ExtraDamagePercent)),
                [TraitKey.Taunt] = new(
                    DescriptionToken<TraitSpec>.From<TauntTraitSpec>("반경", value => value.RadiusCells),
                    DescriptionToken<TraitSpec>.From<TauntTraitSpec>("피해감소율", value => value.DamageReductionPercent)),
                [TraitKey.ThornBomb] = new(
                    DescriptionToken<TraitSpec>.From<ThornBombTraitSpec>("피해율", value => value.DamagePercent)),
                [TraitKey.TrackingShot] = new(
                    DescriptionToken<TraitSpec>.From<TrackingShotTraitSpec>("대상체력기준율", value => value.TargetHealthThresholdPercent),
                    DescriptionToken<TraitSpec>.From<TrackingShotTraitSpec>("피해증가율", value => value.ExtraDamagePercent)),
                [TraitKey.Vanguard] = new(
                    DescriptionToken<TraitSpec>.From<VanguardTraitSpec>("공격력증가량", value => value.AttackBonus),
                    DescriptionToken<TraitSpec>.From<VanguardTraitSpec>("지속횟수", value => value.DurationControllerTurnEnds))
            });

        internal static bool TryGet(TraitKey key, out TraitDescriptionBinding binding) => Entries.TryGetValue(key, out binding);
    }
}
