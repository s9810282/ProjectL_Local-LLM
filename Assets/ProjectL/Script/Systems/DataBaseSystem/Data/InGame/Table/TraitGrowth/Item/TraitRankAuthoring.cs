using System;
using UnityEngine;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item
{
    [Serializable]
    internal abstract class TraitRankAuthoring
    {
        internal abstract TraitRankValues ToValues();
    }

    [Serializable]
    internal sealed class VanguardRankAuthoring : TraitRankAuthoring
    {
        [SerializeField, Min(0)] private int attackBonus = -1;

        internal override TraitRankValues ToValues() => new(TraitKey.Vanguard, attackBonus);
    }

    [Serializable]
    internal sealed class CleanSweepRankAuthoring : TraitRankAuthoring
    {
        [SerializeField, Min(0)] private int attackBonusPerAllyAttack = -1;

        internal override TraitRankValues ToValues() => new(TraitKey.CleanSweep, attackBonusPerAllyAttack);
    }

    [Serializable]
    internal sealed class MoraleBoostRankAuthoring : TraitRankAuthoring
    {
        [SerializeField, Min(0)] private int defeatedEnemyBaseAttackPercent = -1;

        internal override TraitRankValues ToValues() => new(TraitKey.MoraleBoost, defeatedEnemyBaseAttackPercent);
    }

    [Serializable]
    internal sealed class SoloActionRankAuthoring : TraitRankAuthoring
    {
        [SerializeField, Min(0)] private int shieldAmount = -1;

        internal override TraitRankValues ToValues() => new(TraitKey.SoloAction, shieldAmount);
    }

    [Serializable]
    internal sealed class CallOfBloodRankAuthoring : TraitRankAuthoring
    {
        [SerializeField, Min(0)] private int enemyMaxHealthHealPercent = -1;

        internal override TraitRankValues ToValues() => new(TraitKey.CallOfBlood, enemyMaxHealthHealPercent);
    }

    [Serializable]
    internal sealed class PositionSwapRankAuthoring : TraitRankAuthoring
    {
        [SerializeField, Min(0)] private int shieldAmount = -1;

        internal override TraitRankValues ToValues() => new(TraitKey.PositionSwap, shieldAmount);
    }

    [Serializable]
    internal sealed class CohesionRankAuthoring : TraitRankAuthoring
    {
        [SerializeField, Min(0)] private int defensePerAdjacentAlly = -1;
        [SerializeField, Min(0)] private int shieldAmountPerAlly = -1;

        internal override TraitRankValues ToValues() => new(TraitKey.Cohesion, defensePerAdjacentAlly, shieldAmountPerAlly);
    }

    [Serializable]
    internal sealed class TauntRankAuthoring : TraitRankAuthoring
    {
        [SerializeField, Min(0)] private int damageReductionPercent = -1;

        internal override TraitRankValues ToValues() => new(TraitKey.Taunt, damageReductionPercent);
    }

    [Serializable]
    internal sealed class EntrenchmentRankAuthoring : TraitRankAuthoring
    {
        [SerializeField, Min(0)] private int damageReductionPercent = -1;

        internal override TraitRankValues ToValues() => new(TraitKey.Entrenchment, damageReductionPercent);
    }

    [Serializable]
    internal sealed class GroundCompactionRankAuthoring : TraitRankAuthoring
    {
        [SerializeField, Min(0)] private int movementRangeBonus = -1;

        internal override TraitRankValues ToValues() => new(TraitKey.GroundCompaction, movementRangeBonus);
    }

    [Serializable]
    internal sealed class ReconSquadRankAuthoring : TraitRankAuthoring
    {
        [SerializeField, Min(0)] private int increasePercent = -1;

        internal override TraitRankValues ToValues() => new(TraitKey.ReconSquad, increasePercent);
    }

    [Serializable]
    internal sealed class StationaryFireRankAuthoring : TraitRankAuthoring
    {
        [SerializeField, Min(0)] private int extraDamagePercent = -1;

        internal override TraitRankValues ToValues() => new(TraitKey.StationaryFire, extraDamagePercent);
    }

    [Serializable]
    internal sealed class ArmorBreakerRankAuthoring : TraitRankAuthoring
    {
        [SerializeField, Min(0)] private int defenseReductionPercentPerHit = -1;

        internal override TraitRankValues ToValues() => new(TraitKey.ArmorBreaker, defenseReductionPercentPerHit);
    }

    [Serializable]
    internal sealed class FinishingShotRankAuthoring : TraitRankAuthoring
    {
        [SerializeField, Min(0)] private int attackThresholdPercent = -1;

        internal override TraitRankValues ToValues() => new(TraitKey.FinishingShot, attackThresholdPercent);
    }

    [Serializable]
    internal sealed class TrackingShotRankAuthoring : TraitRankAuthoring
    {
        [SerializeField, Min(0)] private int targetHealthThresholdPercent = -1;
        [SerializeField, Min(0)] private int extraDamagePercent = -1;

        internal override TraitRankValues ToValues() => new(TraitKey.TrackingShot, targetHealthThresholdPercent, extraDamagePercent);
    }

    [Serializable]
    internal sealed class HunterRankAuthoring : TraitRankAuthoring
    {
        [SerializeField, Min(0)] private int attackPerTargetKill = -1;

        internal override TraitRankValues ToValues() => new(TraitKey.Hunter, attackPerTargetKill);
    }

    [Serializable]
    internal sealed class FocusedBombardmentRankAuthoring : TraitRankAuthoring
    {
        [SerializeField, Min(0)] private int extraDamagePercent = -1;

        internal override TraitRankValues ToValues() => new(TraitKey.FocusedBombardment, extraDamagePercent);
    }
}
