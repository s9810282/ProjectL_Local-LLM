using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Chapter.Core.Table.Item;

namespace ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.Table.Chapter.Core
{
    #region 서비스

    internal interface IChapterTableQuery : IServiceModule
    {
        ChapterMasterData GetChapter(int key);
        bool TryGetChapter(int key, out ChapterMasterData data);
        IReadOnlyList<ChapterMasterData> GetAllChapters();
    }

    #endregion

    #region 시스템 모듈

    internal sealed class ChapterTable : IChapterTableQuery, IExportSystemServices
    {
        private readonly ExcelTableIndex<ChapterMasterData> index;

        internal ChapterTable(IDataProvider<IDatabaseItem> source)
        {
            index = new ExcelTableIndex<ChapterMasterData>(source, data => data.Key);
        }

        public ChapterMasterData GetChapter(int key) => index.Get(key);

        public bool TryGetChapter(int key, out ChapterMasterData data) => index.TryGet(key, out data);

        public IReadOnlyList<ChapterMasterData> GetAllChapters() => index.GetAll();

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IChapterTableQuery>(this);
        }
    }

    #endregion
}
