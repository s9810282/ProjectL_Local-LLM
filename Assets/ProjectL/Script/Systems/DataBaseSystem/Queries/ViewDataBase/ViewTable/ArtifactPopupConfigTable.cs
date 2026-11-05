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
                if (Config != null)
                    throw new InvalidOperationException("[ArtifactPopupConfigTable] 아티팩트 팝업 설정은 하나만 등록해야 합니다.");
                if (item is not ArtifactPopupConfigDataSO so)
                    throw new InvalidOperationException($"[ArtifactPopupConfigTable] 설정 표에 다른 형식의 항목이 있습니다({item?.GetType().Name ?? "null"}).");
                if (so.GetData() is not ArtifactPopupConfigData data)
                    throw new InvalidOperationException($"[ArtifactPopupConfigTable] {so.name}의 GetData가 설정 Data를 돌려주지 않습니다.");
                data.Validate();
                Config = data;
            }
            if (Config == null) throw new InvalidOperationException("[ArtifactPopupConfigTable] 아티팩트 팝업 설정이 등록되어 있지 않습니다.");
        }

        public void ExportServices(ISystemWriteSink sink) => sink.AddService<IArtifactPopupConfigTableQuery>(this);
    }
}
