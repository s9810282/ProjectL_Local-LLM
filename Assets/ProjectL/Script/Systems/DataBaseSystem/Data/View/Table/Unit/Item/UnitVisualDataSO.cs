using System;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.Unit.Item
{
    /// <summary>유닛·구조물의 프리팹, 표시 이름, 아이콘입니다. 키는 EntityKey이고 View는 이 값을 Buffer에서 받습니다.</summary>
    [Serializable]
    internal sealed class UnitVisualData : IData
    {
        [SerializeField] private EntityKey key;
        [SerializeField] private GameObject prefab;
        [Tooltip("비워 두면 프리팹 이름을 표시합니다.")]
        [SerializeField] private string displayName;
        [Tooltip("타임라인 UI에 쓰는 아이콘입니다. 비워도 됩니다.")]
        [SerializeField] private Sprite timelineIcon;
        [Tooltip("유닛 정보 UI에 쓰는 아이콘입니다. 비워도 됩니다.")]
        [SerializeField] private Sprite infoIcon;

        public EntityKey Key => key;
        public GameObject Prefab => prefab;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) && prefab != null
            ? prefab.name
            : displayName;
        /// <summary>비어 있으면 null입니다.</summary>
        public Sprite TimelineIcon => timelineIcon;
        /// <summary>비어 있으면 null입니다.</summary>
        public Sprite InfoIcon => infoIcon;

        internal void Validate()
        {
            if (key == EntityKey.None || !System.Enum.IsDefined(typeof(EntityKey), key))
                throw new InvalidOperationException($"[UnitVisualData] Entity key is invalid. Key: {key}");
            if (prefab == null)
                throw new InvalidOperationException($"[UnitVisualData] Prefab is required. Key: {key}");
        }
    }

    [CreateAssetMenu(fileName = "NewUnitVisualData", menuName = "ProjectL/Database/View/UnitVisualData")]
    internal sealed class UnitVisualDataSO : DatabaseItem
    {
        [SerializeField] private UnitVisualData unitVisualData = new();

        public override IData GetData() => unitVisualData;
    }
}
