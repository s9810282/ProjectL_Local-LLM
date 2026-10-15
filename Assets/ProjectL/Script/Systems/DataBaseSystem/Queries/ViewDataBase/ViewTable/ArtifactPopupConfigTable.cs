using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Artifact.Item;

namespace ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable
{
    internal interface IArtifactPopupConfigTableQuery : IServiceModule
    {
        ArtifactPopupConfigData Config { get; }
    }

    internal sealed class ArtifactPopupConfigTable : IArtifactPopupConfigTableQuery, IExportSystemServices
    {
        public ArtifactPopupConfigData Config { get; }

        internal ArtifactPopupConfigTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (Config != null || item is not ArtifactPopupConfigDataSO so || so.GetData() is not ArtifactPopupConfigData data)
                    throw new InvalidOperationException("[ArtifactPopupConfigTable] Exactly one configuration item is required.");
                data.Validate();
                Config = data;
            }
            if (Config == null) throw new InvalidOperationException("[ArtifactPopupConfigTable] Configuration is missing.");
        }

        public void ExportServices(ISystemWriteSink sink) => sink.AddService<IArtifactPopupConfigTableQuery>(this);
    }
}
