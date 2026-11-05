using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMapBackground.Item;

namespace ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable
{
    /// <summary>챕터 키로 배경 설정만 조회합니다. 다른 챕터의 배경으로 대체하지 않습니다.</summary>
    internal interface IStageMapBackgroundTableQuery : IServiceModule
    {
        /// <summary>등록된 배경을 반환합니다. 없는 키는 KeyNotFoundException으로 알립니다.</summary>
        StageMapBackgroundData Get(int chapterKey);

        /// <summary>등록되지 않은 키는 false와 null을 반환합니다.</summary>
        bool TryGet(int chapterKey, out StageMapBackgroundData data);
    }
}
