using System;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item
{
    [CreateAssetMenu(fileName = "TraitGrowthDefinition", menuName = "ProjectL/Database/InGame/TraitGrowthDefinition")]
    internal sealed class TraitGrowthDefinitionDataSO : DatabaseItem
    {
        [SerializeReference] private UnitTraitAuthoring normal;
        [SerializeReference] private UnitTraitAuthoring enhanced;
        [SerializeReference] private TraitRankAuthoring rank2;
        [SerializeReference] private TraitRankAuthoring rank3;
        [SerializeField] private string displayName = "";
        [SerializeField, TextArea] private string description = "";
        [SerializeField] private string enhancedName = "";
        [SerializeField, TextArea] private string enhancedDescription = "";
        internal TraitKey Key => ((TraitGrowthDefinitionData)GetData()).Key;

        public override IData GetData()
        {
            if (normal == null) throw new InvalidOperationException($"기본 특성 정의가 필요합니다: {name}.");
            try
            {
                return new TraitGrowthDefinitionData(normal.ToData(), enhanced?.ToData(), displayName, description,
                    enhancedName, enhancedDescription, rank2?.ToValues(), rank3?.ToValues());
            }
            catch (Exception exception) when (exception is InvalidOperationException || exception is ArgumentException)
            {
                throw new InvalidOperationException($"Trait definition {name}: {exception.Message}", exception);
            }
        }
    }
}
