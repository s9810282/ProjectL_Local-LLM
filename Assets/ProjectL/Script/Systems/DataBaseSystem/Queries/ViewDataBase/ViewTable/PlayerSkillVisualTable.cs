using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.PlayerSkill.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;

namespace ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable
{
    internal interface IPlayerSkillVisualTableQuery : IServiceModule
    {
        /// <summary>그 스킬·강화의 외형입니다. 스킬 줄이 없으면 <see cref="KeyNotFoundException"/>입니다.</summary>
        PlayerSkillVisualLook Get(PlayerSkillKey skill, PlayerSkillUpgradeKey upgrade);

        bool TryGet(PlayerSkillKey skill, PlayerSkillUpgradeKey upgrade, out PlayerSkillVisualLook look);
    }

    /// <summary>플레이어 액티브 스킬 외형을 스킬 키 기준으로 제공합니다(전투 스킬 View 설계 SK5). 강화 덮어쓰기는 줄 값이 고릅니다.</summary>
    internal sealed class PlayerSkillVisualTable : IPlayerSkillVisualTableQuery, IExportSystemServices
    {
        private readonly Dictionary<PlayerSkillKey, PlayerSkillVisualData> _dataByKey = new();

        internal PlayerSkillVisualTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item is not PlayerSkillVisualDataSO dataSO || dataSO.GetData() is not PlayerSkillVisualData data)
                    throw new InvalidOperationException("[PlayerSkillVisualTable] Requires non-null PlayerSkillVisualDataSO items.");

                if (!_dataByKey.TryAdd(data.Key, data))
                    throw new InvalidOperationException($"[PlayerSkillVisualTable] Duplicate skill key: {data.Key}.");
            }
        }

        public PlayerSkillVisualLook Get(PlayerSkillKey skill, PlayerSkillUpgradeKey upgrade)
        {
            if (_dataByKey.TryGetValue(skill, out PlayerSkillVisualData data))
                return data.LookOf(upgrade);

            throw new KeyNotFoundException($"[PlayerSkillVisualTable] Skill visual not found. Skill: {skill}.");
        }

        public bool TryGet(PlayerSkillKey skill, PlayerSkillUpgradeKey upgrade, out PlayerSkillVisualLook look)
        {
            if (_dataByKey.TryGetValue(skill, out PlayerSkillVisualData data))
            {
                look = data.LookOf(upgrade);
                return true;
            }

            look = default;
            return false;
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IPlayerSkillVisualTableQuery>(this);
        }
    }
}
