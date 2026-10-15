using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Loadout.Item
{
    /// <summary>UnitRosterData의 입력 SO입니다. 유닛은 UnitDataSO를 끌어다 놓고, GetData에서 EntityKey로 바꿉니다.</summary>
    [CreateAssetMenu(fileName = "NewUnitRosterData", menuName = "ProjectL/Database/InGame/UnitRosterData")]
    internal sealed class UnitRosterDataSO : DatabaseItem
    {
        [SerializeField, Min(1)] private int rosterKey = 1;
        [SerializeField] private UnitDataSO[] unitDefinitions = new UnitDataSO[0];

        /// <summary>다른 SO가 이 목록을 참조할 때 쓰는 키입니다.</summary>
        internal int RosterKey => rosterKey;

        public override IData GetData()
        {
            var keys = new EntityKey[unitDefinitions?.Length ?? 0];
            for (int i = 0; i < keys.Length; i++)
                keys[i] = unitDefinitions[i] != null ? unitDefinitions[i].Key : EntityKey.None;
            return new UnitRosterData(rosterKey, keys);
        }
    }
}
