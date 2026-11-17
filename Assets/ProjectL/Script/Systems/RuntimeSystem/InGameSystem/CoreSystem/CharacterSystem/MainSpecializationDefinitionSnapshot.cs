using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Category.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem
{
    // GetData가 새 객체를 반환해도 동일한 설정은 같은 원본이다. 비교하는 계약 필드를 명시한다.
    internal sealed class MainSpecializationDefinitionSnapshot
    {
        internal CombatAttackProfile Profile { get; }
        internal TraitGrowthCatalog Catalog { get; }
        internal PlayerSkillDefinition PlayerSkill { get; }
        private readonly object[] _values;

        internal MainSpecializationDefinitionSnapshot(CombatAttackProfile profile, TraitGrowthCatalog catalog,
            CategoryConfigData config, IReadOnlyList<UnitSpec> rosterDefinitions, PlayerSkillDefinition playerSkill = null)
        {
            Profile = profile;
            Catalog = catalog;
            PlayerSkill = playerSkill;
            var values = new List<object> { playerSkill != null, playerSkill?.Key, playerSkill?.MainCharacterKey, playerSkill?.StageUseLimit };
            foreach (var unit in rosterDefinitions) AddUnit(values, unit);
            AddUnit(values, profile.Definition);
            var method = profile.Method;
            values.AddRange(new object[] { method.Shape, method.ChoiceMode, method.Splash, method.SplashPercent, method.ChoiceAngle, method.Width,
                config.MainMovementAllowance, config.RiderMovementAllowance, config.HeavyMovementAllowance,
                config.CannonMovementAllowance, config.BossMovementAllowance, config.MainLevelGrowth, config.RiderLevelGrowth,
                config.HeavyLevelGrowth, config.CannonLevelGrowth, config.BossLevelGrowth, config.LevelProgression });
            values.Add(config.DefaultTraits.Count);
            foreach (var assignment in config.DefaultTraits.OrderBy(p => p.Key))
                values.AddRange(new object[] { assignment.Key, assignment.Value });
            foreach (var category in catalog.Categories.OrderBy(c => c.Category))
            {
                values.Add(category.Category);
                values.Add(category.Steps.Count);
                foreach (var step in category.Steps) values.AddRange(new object[] { step.StepId, step.UnlockLevel, step.Kind });
                values.Add(category.Pool.Count);
                values.AddRange(category.Pool.OrderBy(k => k).Cast<object>());
            }
            values.Add(catalog.Definitions.Count);
            foreach (var trait in catalog.Definitions.OrderBy(t => t.Key))
            {
                AddTrait(values, trait.Normal);
                AddTrait(values, trait.Enhanced);
                values.Add(trait.AllowUnchangedRankValues);
                AddRank(values, trait.Rank2);
                AddRank(values, trait.Rank3);
                values.AddRange(new object[] { trait.Name, trait.Description, trait.EnhancedName, trait.EnhancedDescription });
            }
            _values = values.ToArray();
        }

        internal bool SameValues(MainSpecializationDefinitionSnapshot other) => other != null && _values.SequenceEqual(other._values);

        private static void AddRank(List<object> values, TraitRankValues rank)
        {
            values.AddRange(new object[] { rank != null, rank?.Amount, rank?.SecondAmount,
                rank?.PreventsLethalSacrificeDamage, rank?.ExemptsAlliedAttackRangePenalty });
        }

        private static void AddUnit(List<object> values, UnitSpec unit)
        {
            values.AddRange(new object[] { unit.Key, unit.Role, unit.FootprintWidth, unit.FootprintHeight, unit.MaxHealth,
                unit.MoveMethod, unit.CategoryKey, unit.SpecializationCategory, unit.AttackRange, unit.AttackPower,
                unit.PhysicalDefense, unit.Priority, unit.OwnerEligibility, unit.Occlusion.Delivery, unit.Occlusion.Propagation,
                unit.Behavior.CanMoveVoluntarily, unit.Behavior.CanBasicAttack, unit.Behavior.IsForcedMovementImmune,
                unit.KillReward?.IsEligible, unit.KillReward?.Gold, unit.KillReward?.Experience, unit.FootprintLocalCells.Length });
            values.AddRange(unit.FootprintLocalCells.ToArray().Cast<object>());
            values.Add(unit.Traits.Length);
            foreach (var trait in unit.Traits.ToArray().OrderBy(t => t.TraitKey)) AddTrait(values, trait);
        }

        private static void AddTrait(List<object> values, TraitSpec trait)
        {
            if (trait == null) { values.Add(null); return; }
            values.Add(trait.TraitKey);
            switch (trait)
            {
                case AccelerationTraitSpec t: values.Add(t.AttackPerMovedCell); break;
                case AnkleBreakerTraitSpec t: values.Add(t.DurationAffectedSideTurnEnds); break;
                case ArmorBreakerTraitSpec t: values.Add(t.DefenseReductionPercentPerHit); break;
                case BleedingTraitSpec t: values.Add(t.DamagePerStack); break;
                case CallOfBloodTraitSpec t: values.Add(t.EnemyMaxHealthHealPercent); break;
                case CleanSweepTraitSpec t: values.Add(t.AttackBonusPerAllyAttack); values.Add(t.DurationControllerTurnEnds); break;
                case CohesionTraitSpec t: values.Add(t.DefensePerAdjacentAlly); values.Add(t.ShieldAmountPerAlly); values.Add(t.RadiusCells); values.Add(t.ResetEveryControllerTurnEnds); break;
                case DisarmTraitSpec t: values.Add(t.DurationAffectedSideTurnEnds); break;
                case DistanceDamageScaleTraitSpec t: values.AddRange(t.PercentByDistance.Cast<object>()); break;
                case DustScreenTraitSpec t:
                    values.AddRange(new object[] { t.AttackRangeReduction, t.AttackDamageReductionPercent,
                        t.OpposingTurnEndsToExpire, t.ExemptsAlliedAttackRangePenalty });
                    break;
                case EntrenchmentTraitSpec t: values.Add(t.RadiusCells); values.Add(t.DamageReductionPercent); break;
                case FinishingShotTraitSpec t: values.Add(t.AttackThresholdPercent); break;
                case FocusedBombardmentTraitSpec t: values.Add(t.RequiredEnemyCount); values.Add(t.ExtraDamagePercent); break;
                case FocusTraitSpec t: values.Add(t.DamagePerStack); values.Add(t.MaxStacks); break;
                case GroundCompactionTraitSpec t: values.Add(t.MovementRangeBonus); values.Add(t.OpposingTurnEndsToExpire); break;
                case HuntersMarkTraitSpec t: values.Add(t.BonusDamagePercent); values.Add(t.DurationAffectedSideTurnEnds); break;
                case HunterTraitSpec t: values.Add(t.AttackPerTargetKill); values.Add(t.MaxRewardsPerStage); break;
                case DeadHandTraitSpec t: values.Add(t.DamagePercent); break;
                case ImpactBoltTraitSpec t: values.Add(t.DistanceCells); break;
                case ThornBombTraitSpec t: values.Add(t.DamagePercent); break;
                case CrashImpactTraitSpec t: values.Add(t.DistanceCells); values.Add(t.WallDamagePercent); values.Add(t.UnitDamagePercent); break;
                case IntimidationTraitSpec t: values.Add(t.AttackReductionPercent); break;
                case KillChainAttackTraitSpec t:  break;
                case KnockbackTraitSpec t: values.Add(t.DistanceCells); break;
                case LavaCannonTraitSpec: break;
                case LeapTraitSpec t:  break;
                case MoraleBoostTraitSpec t: values.Add(t.DefeatedEnemyBaseAttackPercent); values.Add(t.DurationControllerTurnEnds); break;
                case NecromancyTraitSpec t: values.Add(t.SummonedUnitKey); values.Add(t.MaxLivingSummons); values.Add(t.FirstActionDelay); break;
                case PiercingFalloffTraitSpec t: values.Add(t.ReductionPercentPerTarget); values.Add(t.MaxTargetsPerRay); break;
                case PositionSwapTraitSpec t: values.Add(t.ShieldAmount); break;
                case PullTraitSpec t: values.Add(t.ExtraRangeCells); break;
                case RampageTraitSpec t: values.Add(t.BaseAttackBonus); values.Add(t.RankAttackBonus); values.Add(t.MovementBonus); break;
                case ReconSquadTraitSpec t: values.Add(t.IncreasePercent); values.Add(t.Radius); break;
                case RegenerationTraitSpec t: values.Add(t.HealAmount); values.Add(t.HealEveryOwnSideTurnEnds); break;
                case RoarTraitSpec t:
                    values.AddRange(new object[] { t.HitThreshold, t.CountPerHit, t.CountPerOwnSideTurnEnd, t.CountPerOpposingSideTurnEnd });
                    break;
                case SacrificeTraitSpec t:
                    values.AddRange(new object[] { t.RadiusCells, t.MaxTriggersPerEnemyTurn, t.PreventsLethalSacrificeDamage, t.ShieldOnActivation });
                    break;
                case SoloActionTraitSpec t: values.Add(t.ShieldAmount); values.Add(t.RadiusCells); values.Add(t.RequiredOccupiedNeighborCount); values.Add(t.ResetEveryControllerTurnEnds); break;
                case StationaryFireTraitSpec t: values.Add(t.ExtraDamagePercent); break;
                case SteadfastWillTraitSpec t:  break;
                case TauntTraitSpec t: values.Add(t.RadiusCells); values.Add(t.DamageReductionPercent); break;
                case TrackingShotTraitSpec t: values.Add(t.TargetHealthThresholdPercent); values.Add(t.ExtraDamagePercent); break;
                case VanguardTraitSpec t: values.Add(t.AttackBonus); values.Add(t.DurationControllerTurnEnds); break;
                case ReconstructionTraitSpec t: values.Add(t.Level); break;
                case SelfDestructTraitSpec t: values.Add(t.Level); break;
                case FearTraitSpec: break;
                default: throw new InvalidOperationException("전직 원본 비교에서 지원하지 않는 특성 정의입니다.");
            }
        }
    }
}
