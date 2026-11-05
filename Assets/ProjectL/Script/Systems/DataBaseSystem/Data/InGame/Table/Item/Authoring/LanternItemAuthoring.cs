using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using UnityEngine;
namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Item.Authoring
{
    [System.Serializable]
    internal sealed class LanternItemAuthoring : ItemEffectAuthoring
    {
        [SerializeField, Min(1)] private int defensePenalty = 5;
        public override ItemEffectSpec ToSpec() => new LanternItemSpec(defensePenalty);
    }
}
