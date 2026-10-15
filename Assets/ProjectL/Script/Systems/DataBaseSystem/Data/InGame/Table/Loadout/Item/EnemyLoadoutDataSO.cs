using System;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Loadout.Item
{
    /// <summary>EnemyLoadoutData의 입력 SO입니다. 유닛은 UnitDataSO를 끌어다 놓고, GetData에서 EntityKey로 바꿉니다.</summary>
    [CreateAssetMenu(fileName = "NewEnemyLoadoutData", menuName = "ProjectL/Database/InGame/EnemyLoadoutData")]
    internal sealed class EnemyLoadoutDataSO : DatabaseItem
    {
        [Serializable]
        private struct Entry
        {
            [SerializeField] private UnitDataSO unitDefinition;
            [SerializeField, Min(1)] private int weight;

            public EnemyLoadoutEntry ToData()
            {
                EntityKey unitKey = unitDefinition != null ? unitDefinition.Key : EntityKey.None;
                return new EnemyLoadoutEntry(unitKey, weight);
            }
        }

        [SerializeField, Min(1)] private int loadoutKey = 1;
        [SerializeField] private Entry[] entries = new Entry[0];
        [SerializeField, Min(0)] private int spawnCount = 3;
        [SerializeField] private int randomSeed;

        /// <summary>다른 SO가 이 구성을 참조할 때 쓰는 키입니다.</summary>
        internal int LoadoutKey => loadoutKey;

        public override IData GetData()
        {
            var data = new EnemyLoadoutEntry[entries?.Length ?? 0];
            for (int i = 0; i < data.Length; i++)
                data[i] = entries[i].ToData();
            return new EnemyLoadoutData(loadoutKey, data, spawnCount, randomSeed);
        }
    }
}
