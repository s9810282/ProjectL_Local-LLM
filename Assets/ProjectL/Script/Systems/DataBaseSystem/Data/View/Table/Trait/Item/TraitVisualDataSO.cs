using System;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.Trait.Item
{
    /// <summary>유닛 특성의 표시 이름, 아이콘, 설명입니다. 키는 InGame과 같은 TraitKey입니다.</summary>
    [Serializable]
    internal sealed class TraitVisualData : IData
    {
        [SerializeField] private TraitKey key;
        [SerializeField] private string displayName;
        [Tooltip("특성 아이콘입니다. 비워도 됩니다.")]
        [SerializeField] private Sprite icon;
        [TextArea(2, 6)]
        [SerializeField] private string description;

        public TraitKey Key => key;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? key.ToString() : displayName;
        /// <summary>비어 있으면 null입니다.</summary>
        public Sprite Icon => icon;
        public string Description => description ?? string.Empty;

        internal void Validate()
        {
            if (key == TraitKey.None || !System.Enum.IsDefined(typeof(TraitKey), key))
                throw new InvalidOperationException($"[TraitVisualData] Trait key is invalid. Key: {key}");
        }
    }

    [CreateAssetMenu(fileName = "NewTraitVisualData", menuName = "ProjectL/Database/View/TraitVisualData")]
    internal sealed class TraitVisualDataSO : DatabaseItem
    {
        [SerializeField] private TraitVisualData traitVisualData = new();

        public override IData GetData() => traitVisualData;
    }
}
