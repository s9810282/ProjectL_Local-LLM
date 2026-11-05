using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable
{
    internal interface ICombatPresentationSettingsTableQuery : IServiceModule
    {
        CombatPresentationSettingsData Config { get; }
    }

    /// <summary>
    /// 설정형 테이블입니다. 아이템이 정확히 1개여야 하고, 만들 때 값 조건을 검사합니다(연출 조립 설계 §9 I-5).
    /// </summary>
    internal sealed class CombatPresentationSettingsTable : ICombatPresentationSettingsTableQuery, IExportSystemServices
    {
        public CombatPresentationSettingsData Config { get; }

        internal CombatPresentationSettingsTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item is not CombatPresentationSettingsDataSO dataSO || dataSO.GetData() is not CombatPresentationSettingsData data)
                    throw new InvalidOperationException("[CombatPresentationSettingsTable] Requires non-null CombatPresentationSettingsDataSO items.");
                if (Config != null)
                    throw new InvalidOperationException("[CombatPresentationSettingsTable] Exactly one item is required, but found more.");

                data.Validate();
                Config = data;
            }

            if (Config == null)
                throw new InvalidOperationException("[CombatPresentationSettingsTable] Exactly one item is required, but found none.");
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<ICombatPresentationSettingsTableQuery>(this);
        }
    }
}
