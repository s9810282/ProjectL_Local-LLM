using System;
using System.Collections.Generic;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Enhancement.Item
{
    [CreateAssetMenu(fileName = "Enhancement", menuName = "ProjectL/Database/InGame/Enhancement")]
    internal sealed class EnhancementDataSO : DatabaseItem
    {
        [SerializeField] private EnhancementKey key;
        [SerializeField] private UnitDataSO character;
        [SerializeField, Min(1)] private int level = 1;
        [SerializeField, Min(0)] private int cost = 700;
        [SerializeField, Min(1)] private int weight = 1;
        [SerializeField] private string displayName = "";
        [SerializeField, TextArea] private string description = "";
        [SerializeReference] private List<UnitTraitAuthoring> addedTraits = new();
        [SerializeReference] private List<UnitTraitAuthoring> replacedTraits = new();
        internal EnhancementKey Key => key;

        public override IData GetData()
        {
            if (character == null) throw new InvalidOperationException("강화 대상 유닛이 필요합니다.");
            return new EnhancementData(key, character.Key, level, cost, weight, displayName, description,
                Read(addedTraits), Read(replacedTraits));
        }

        private static UnitTraitData[] Read(List<UnitTraitAuthoring> traits)
        {
            if (traits == null) throw new InvalidOperationException("강화 특성 목록이 필요합니다.");
            var result = new UnitTraitData[traits.Count];
            for (int i = 0; i < result.Length; i++) result[i] = traits[i]?.ToData();
            return result;
        }
    }
}
