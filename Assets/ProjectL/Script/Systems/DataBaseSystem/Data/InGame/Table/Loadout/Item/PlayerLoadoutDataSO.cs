using System;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Loadout.Item
{
    /// <summary>PlayerLoadoutData의 입력 SO입니다. 유닛은 UnitDataSO를 끌어다 놓고, GetData에서 EntityKey로 바꿉니다.</summary>
    [CreateAssetMenu(fileName = "NewPlayerLoadoutData", menuName = "ProjectL/Database/InGame/PlayerLoadoutData")]
    internal sealed class PlayerLoadoutDataSO : DatabaseItem
    {
        [Serializable]
        private struct Entry
        {
            [SerializeField] private UnitDataSO unitDefinition;
            [SerializeField, Min(1)] private int count;

            public PlayerLoadoutEntry ToData()
            {
                EntityKey unitKey = unitDefinition != null ? unitDefinition.Key : EntityKey.None;
                return new PlayerLoadoutEntry(unitKey, count);
            }
        }

        [SerializeField, Min(1)] private int loadoutKey = 1;
        [Tooltip("Role이 MainCharacter인 유닛이어야 합니다.")]
        [SerializeField] private UnitDataSO mainCharacter;
        [SerializeField] private Entry[] entries = new Entry[0];
        [Tooltip("각 스테이지 첫 손패 수(메인 포함)입니다. 첫 손패에만 적용되며 덱에서 뽑는 서브 수는 이 값 - 1입니다. 기본 4 = 메인 1 + 서브 3.")]
        [SerializeField, Min(1)] private int startingHandSize = 4;
        // 셔플 시드는 스테이지마다 다르게 섞이도록 스테이지 TimelineSeed를 쓰므로 입력 항목이 없다.
        [Tooltip("새 Run 시작 때 지급하는 Run 재화(상점·회복용)입니다. 임시 값이며 Excel DT가 들어오면 그쪽으로 옮깁니다.")]
        [SerializeField, Min(0)] private int startingRunCurrency;

        /// <summary>다른 SO가 이 구성을 참조할 때 쓰는 키입니다.</summary>
        internal int LoadoutKey => loadoutKey;

        public override IData GetData()
        {
            var data = new PlayerLoadoutEntry[entries?.Length ?? 0];
            for (int i = 0; i < data.Length; i++)
                data[i] = entries[i].ToData();
            EntityKey mainCharacterKey = mainCharacter != null ? mainCharacter.Key : EntityKey.None;
            return new PlayerLoadoutData(loadoutKey, mainCharacterKey, data, startingHandSize, startingRunCurrency);
        }
    }
}
