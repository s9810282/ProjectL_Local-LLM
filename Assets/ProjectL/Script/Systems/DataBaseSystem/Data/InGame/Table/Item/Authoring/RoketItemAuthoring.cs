using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using UnityEngine;
namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Item.Authoring
{
    [System.Serializable]
    internal sealed class RoketItemAuthoring : ItemEffectAuthoring
    {
        [SerializeField, Min(1)] private int triggerDamagePercent = 20;
        [SerializeField, Min(1)] private int reductionPercent = 50;
        public override ItemEffectSpec ToSpec() => new RoketItemSpec(triggerDamagePercent, reductionPercent);
    }
}
