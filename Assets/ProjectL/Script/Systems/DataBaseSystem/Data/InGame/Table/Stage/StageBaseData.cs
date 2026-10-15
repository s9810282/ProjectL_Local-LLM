using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Framework.DataBase;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage
{
    /// <summary>
    /// 모든 스테이지 상세 데이터 SO의 공통 기반입니다.
    /// StageRootDataSO는 이 Item을 참조하고, GetData는 종류별 순수 C# 상세 데이터를 반환합니다.
    /// </summary>
    public abstract class StageBaseData : DatabaseItem
    {
        public abstract StageType StageType { get; }

        public override IData GetData() => CreateStageData();

        protected abstract IStageData CreateStageData();
    }
}
