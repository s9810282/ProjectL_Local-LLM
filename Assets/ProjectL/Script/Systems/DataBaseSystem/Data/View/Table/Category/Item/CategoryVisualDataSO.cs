using System;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.Category.Item
{
    /// <summary>병종 키별 흰색·검은색 공통 아이콘입니다. 병종 값은 Buffer에서 받습니다.</summary>
    [Serializable]
    internal sealed class CategoryVisualData : IData
    {
        [SerializeField] private CategoryKey key;
        [Tooltip("흰색 병종 아이콘입니다. 입력 준비 중에는 비워 둘 수 있습니다.")]
        [SerializeField] private Sprite whiteIcon;
        [FormerlySerializedAs("icon")]
        [Tooltip("검은색 병종 아이콘입니다. 입력 준비 중에는 비워 둘 수 있습니다.")]
        [SerializeField] private Sprite blackIcon;

        public CategoryKey Key => key;
        public Sprite WhiteIcon => whiteIcon;
        public Sprite BlackIcon => blackIcon;

        /// <summary>기존 표시 경로의 기본 아이콘은 검은색 버전을 사용합니다.</summary>
        public Sprite Icon => BlackIcon;

        internal void Validate()
        {
            if (key == CategoryKey.None || !System.Enum.IsDefined(typeof(CategoryKey), key))
                throw new InvalidOperationException($"[CategoryVisualData] 병종 키가 올바르지 않습니다. Key: {key}.");
        }
    }

    [CreateAssetMenu(fileName = "NewCategoryVisualData", menuName = "ProjectL/Database/View/CategoryVisualData")]
    internal sealed class CategoryVisualDataSO : DatabaseItem
    {
        [SerializeField] private CategoryVisualData categoryVisualData = new();

        public override IData GetData() => categoryVisualData;
    }
}
