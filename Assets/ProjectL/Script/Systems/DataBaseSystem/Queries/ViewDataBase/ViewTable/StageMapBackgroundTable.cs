using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMapBackground.Item;

namespace ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable
{
    /// <summary>검증한 배경 설정을 챕터 키로 보관합니다. 표시 인스턴스와 진행 상태는 소유하지 않습니다.</summary>
    internal sealed class StageMapBackgroundTable : IStageMapBackgroundTableQuery, IExportSystemServices
    {
        private readonly Dictionary<int, StageMapBackgroundData> _dataByChapter = new();

        internal StageMapBackgroundTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            IEnumerable<IDatabaseItem> items = source.GetAllData()
                ?? throw new InvalidOperationException("[StageMapBackgroundTable] 항목 목록이 없습니다.");
            foreach (IDatabaseItem item in items)
            {
                if (item is not StageMapBackgroundDataSO dataSO || dataSO == null)
                {
                    throw new InvalidOperationException(
                        "[StageMapBackgroundTable] 유효한 StageMapBackgroundDataSO 항목이 필요합니다.");
                }

                if (dataSO.GetData() is not StageMapBackgroundData data)
                {
                    throw new InvalidOperationException(
                        $"[StageMapBackgroundTable] '{dataSO.name}'에서 배경 데이터를 얻지 못했습니다.");
                }

                data.Validate();
                if (!_dataByChapter.TryAdd(data.ChapterKey, data))
                {
                    throw new InvalidOperationException(
                        $"[StageMapBackgroundTable] 챕터 {data.ChapterKey}의 배경이 중복 등록되었습니다.");
                }
            }
        }

        public StageMapBackgroundData Get(int chapterKey)
        {
            if (_dataByChapter.TryGetValue(chapterKey, out StageMapBackgroundData data))
            {
                return data;
            }

            throw new KeyNotFoundException($"[StageMapBackgroundTable] 챕터 {chapterKey}의 배경이 없습니다.");
        }

        public bool TryGet(int chapterKey, out StageMapBackgroundData data) =>
            _dataByChapter.TryGetValue(chapterKey, out data);

        public void ExportServices(ISystemWriteSink sink)
        {
            if (sink == null)
            {
                throw new ArgumentNullException(nameof(sink));
            }

            sink.AddService<IStageMapBackgroundTableQuery>(this);
        }
    }
}
