using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using UnityEngine;
namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Item.Authoring
{
    [System.Serializable]
    internal sealed class RopeItemAuthoring : ItemEffectAuthoring
    {
        [SerializeField, Min(1)] private int priorityPenalty = 10;
        public override ItemEffectSpec ToSpec() => new RopeItemSpec(priorityPenalty);
    }
}
