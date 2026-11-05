using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using UnityEngine;
namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Item.Authoring
{
    [System.Serializable]
    internal sealed class WhiskeyItemAuthoring : ItemEffectAuthoring
    {
        [SerializeField, Min(1)] private int triggerHealthPercent = 50;
        [SerializeField, Min(1)] private int healPercent = 10;
        public override ItemEffectSpec ToSpec() => new WhiskeyItemSpec(triggerHealthPercent, healPercent);
    }
}
