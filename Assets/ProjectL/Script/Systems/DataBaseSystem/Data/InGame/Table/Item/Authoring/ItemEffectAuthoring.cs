using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Item.Authoring
{
    [System.Serializable]
    internal abstract class ItemEffectAuthoring
    {
        public abstract ItemEffectSpec ToSpec();
    }
}
