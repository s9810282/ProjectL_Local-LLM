using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Character.Core.Table.Item;

namespace ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.Table.Character.Core
{
    #region 서비스

    internal interface ICharacterTableQuery : IServiceModule
    {
        CharacterMasterData GetCharacter(int key);
        bool TryGetCharacter(int key, out CharacterMasterData data);
        IReadOnlyList<CharacterMasterData> GetAllCharacters();
    }

    #endregion

    #region 시스템 모듈

    internal sealed class CharacterTable : ICharacterTableQuery, IExportSystemServices
    {
        private readonly ExcelTableIndex<CharacterMasterData> index;

        internal CharacterTable(IDataProvider<IDatabaseItem> source)
        {
            index = new ExcelTableIndex<CharacterMasterData>(source, data => data.Key);
        }

        public CharacterMasterData GetCharacter(int key) => index.Get(key);

        public bool TryGetCharacter(int key, out CharacterMasterData data) => index.TryGet(key, out data);

        public IReadOnlyList<CharacterMasterData> GetAllCharacters() => index.GetAll();

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<ICharacterTableQuery>(this);
        }
    }

    #endregion
}
