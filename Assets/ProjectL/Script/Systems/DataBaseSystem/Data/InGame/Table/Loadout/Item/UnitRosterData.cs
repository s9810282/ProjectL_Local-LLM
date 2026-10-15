using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Loadout.Item
{
    /// <summary>
    /// 스테이지에서 사용할 수 있는 유닛 목록입니다. Core가 받는 순수 C# 데이터이며 UnitRosterDataSO가 생성합니다.
    /// </summary>
    public sealed class UnitRosterData : IData
    {
        public int RosterKey { get; }
        public IReadOnlyList<EntityKey> UnitKeys { get; }

        public UnitRosterData(int rosterKey, EntityKey[] unitKeys)
        {
            RosterKey = rosterKey;
            UnitKeys = new ReadOnlyCollection<EntityKey>(unitKeys ?? Array.Empty<EntityKey>());
        }

        public void Validate()
        {
            if (RosterKey < 1)
                throw new InvalidOperationException("[UnitRosterData] RosterKey must be positive.");

            var keys = new HashSet<EntityKey>();
            foreach (EntityKey key in UnitKeys)
            {
                if (key == EntityKey.None)
                    throw new InvalidOperationException($"[UnitRosterData] Unit is missing. RosterKey: {RosterKey}");
                if (!keys.Add(key))
                    throw new InvalidOperationException($"[UnitRosterData] Duplicate unit {key}. RosterKey: {RosterKey}");
            }
        }
    }
}
