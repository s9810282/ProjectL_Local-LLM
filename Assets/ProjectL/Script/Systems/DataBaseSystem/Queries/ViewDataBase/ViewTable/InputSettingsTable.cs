using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.InputSettings.Item;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable
{
    internal interface IInputSettingsTableQuery : IServiceModule
    {
        InputSettingsData Config { get; }
    }

    /// <summary>설정형 테이블입니다. 아이템이 정확히 1개여야 합니다.</summary>
    internal sealed class InputSettingsTable : IInputSettingsTableQuery, IExportSystemServices
    {
        public InputSettingsData Config { get; }

        internal InputSettingsTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item is not InputSettingsDataSO dataSO || dataSO.GetData() is not InputSettingsData data)
                    throw new InvalidOperationException("[InputSettingsTable] Requires non-null InputSettingsDataSO items.");
                if (Config != null)
                    throw new InvalidOperationException("[InputSettingsTable] Exactly one item is required, but found more.");

                data.Validate();
                Config = data;
            }

            if (Config == null)
                throw new InvalidOperationException("[InputSettingsTable] Exactly one item is required, but found none.");
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IInputSettingsTableQuery>(this);
        }
    }
}
