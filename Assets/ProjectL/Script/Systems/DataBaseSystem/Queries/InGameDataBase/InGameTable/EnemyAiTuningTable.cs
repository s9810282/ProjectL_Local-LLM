using System;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.EnemyAi.Item;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable
{
    internal interface IEnemyAiTuningTableQuery : IServiceModule
    {
        EnemyAiTuningData Tuning { get; }
    }

    /// <summary>설정형 선택 테이블입니다. 등록하면 아이템이 정확히 1개여야 합니다.</summary>
    internal sealed class EnemyAiTuningTable : IEnemyAiTuningTableQuery, IExportSystemServices
    {
        public EnemyAiTuningData Tuning { get; }

        internal EnemyAiTuningTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item is not EnemyAiTuningDataSO dataSO || dataSO.GetData() is not EnemyAiTuningData data)
                    throw new InvalidOperationException("[EnemyAiTuningTable] Requires non-null EnemyAiTuningDataSO items.");
                if (Tuning != null)
                    throw new InvalidOperationException("[EnemyAiTuningTable] Exactly one item is required, but found more.");

                data.Validate();
                Tuning = data;
            }

            if (Tuning == null)
                throw new InvalidOperationException("[EnemyAiTuningTable] Exactly one item is required, but found none.");
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IEnemyAiTuningTableQuery>(this);
        }
    }
}
