using System;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item
{
    /// <summary>스테이지 종류별 상세 데이터가 따르는 계약입니다. 순수 C# 데이터입니다.</summary>
    public interface IStageData : IData
    {
        StageType StageType { get; }
        void Validate();
    }

    /// <summary>
    /// DB가 StageKey로 보관·조회하는 공통 스테이지 데이터입니다.
    /// Core가 받는 순수 C# 데이터이며 StageRootDataSO가 생성합니다.
    /// </summary>
    public sealed class StageRootData : IData
    {
        public int StageKey { get; }
        public int MapKey { get; }
        public StageMapRole MapRole { get; }
        public IStageData StageData { get; }
        public StageType StageType => StageData.StageType;

        public StageRootData(int stageKey, int mapKey, StageMapRole mapRole, IStageData stageData)
        {
            StageKey = stageKey;
            MapKey = mapKey;
            MapRole = mapRole;
            StageData = stageData;
        }

        public void Validate()
        {
            if (StageKey < 1)
                throw new InvalidOperationException("StageKey must be positive.");
            if (MapKey < 1)
                throw new InvalidOperationException("MapKey must be positive.");
            if (!System.Enum.IsDefined(typeof(StageMapRole), MapRole))
                throw new InvalidOperationException("Stage map role is invalid.");
            if (StageData == null)
                throw new InvalidOperationException("StageData is required.");
            StageData.Validate();
        }
    }
}
