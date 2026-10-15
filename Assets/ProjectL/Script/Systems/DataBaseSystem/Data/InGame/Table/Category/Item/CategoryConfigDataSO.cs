using ProjectL.Framework.DataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Category.Item
{
    /// <summary>CategoryConfigData의 입력 SO입니다. 테이블에 하나만 둡니다(설정형).</summary>
    [CreateAssetMenu(fileName = "NewCategoryConfigData", menuName = "ProjectL/Database/InGame/CategoryConfigData")]
    internal sealed class CategoryConfigDataSO : DatabaseItem
    {
        [Header("Heavy")]
        [SerializeField, Range(1, 100)] private int heavyDamageReductionPercent = 50;
        [SerializeField] private Rotation4 heavyInitialFacing = Rotation4.R0;

        public override IData GetData() => new CategoryConfigData(heavyDamageReductionPercent, heavyInitialFacing);
    }
}
