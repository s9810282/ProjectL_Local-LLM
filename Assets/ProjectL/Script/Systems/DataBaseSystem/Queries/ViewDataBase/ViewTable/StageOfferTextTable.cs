using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageOfferText.Item;

namespace ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable
{
    internal interface IStageOfferTextTableQuery : IServiceModule
    {
        /// <summary>증강 화면의 글입니다.</summary>
        StageOfferTextData TraitGrowth { get; }

        /// <summary>
        /// 팝업 화면이 있는 스테이지 키의 화면 글입니다. 없으면 <see cref="KeyNotFoundException"/>,
        /// 그 키가 이름·설명만 쓰는 줄이면 <see cref="InvalidOperationException"/>입니다.
        /// </summary>
        StageOfferTextData GetForStage(int stageKey);

        /// <summary>
        /// 맵 노드(건물 호버)의 이름·설명 줄입니다. 그 키의 줄이 있으면 그 줄, 없으면 종류·역할 공통 줄입니다(스테이지 맵 설계 §5.7 3판).
        /// 둘 다 없으면 <see cref="KeyNotFoundException"/>입니다.
        /// </summary>
        StageOfferTextData GetNodeText(int stageKey, StageType stageType, StageMapRole mapRole);
    }

    /// <summary>
    /// 이벤트 스테이지·증강 공통 레이아웃의 글을 스테이지 키 기준으로 제공합니다(공통 레이아웃 설계 SO5).
    /// 건물 호버의 이름·설명도 여기서 읽습니다(키 전용 → 종류·역할 공통).
    /// </summary>
    internal sealed class StageOfferTextTable : IStageOfferTextTableQuery, IExportSystemServices
    {
        private readonly Dictionary<int, StageOfferTextData> _dataByStageKey = new();
        private readonly Dictionary<(StageType, StageMapRole), StageOfferTextData> _commonByKind = new();

        internal StageOfferTextTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            foreach (IDatabaseItem item in source.GetAllData())
            {
                if (item is not StageOfferTextDataSO dataSO || dataSO.GetData() is not StageOfferTextData data)
                    throw new InvalidOperationException("[StageOfferTextTable] Requires non-null StageOfferTextDataSO items.");

                data.Validate();
                Add(data);
            }

            if (TraitGrowth == null)
                throw new InvalidOperationException("[StageOfferTextTable] 증강 글(target = TraitGrowth)이 없습니다.");
        }

        public StageOfferTextData TraitGrowth { get; private set; }

        public StageOfferTextData GetForStage(int stageKey)
        {
            if (!_dataByStageKey.TryGetValue(stageKey, out StageOfferTextData data))
                throw new KeyNotFoundException($"[StageOfferTextTable] Stage offer text not found. Stage key: {stageKey}.");
            if (data.Target != StageOfferTextTarget.Stage)
                throw new InvalidOperationException(
                    $"[StageOfferTextTable] StageKey {stageKey}의 글 '{data.EventName}'은 화면 글이 아니라 이름·설명 줄({data.Target})입니다.");
            return data;
        }

        public StageOfferTextData GetNodeText(int stageKey, StageType stageType, StageMapRole mapRole)
        {
            if (_dataByStageKey.TryGetValue(stageKey, out StageOfferTextData data) ||
                _commonByKind.TryGetValue((stageType, mapRole), out data))
                return data;

            throw new KeyNotFoundException(
                $"[StageOfferTextTable] 노드 글이 없습니다. StageKey {stageKey}의 줄도, ({stageType}, {mapRole}) 공통 줄도 없습니다.");
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IStageOfferTextTableQuery>(this);
        }

        private void Add(StageOfferTextData data)
        {
            if (data.Target == StageOfferTextTarget.TraitGrowth)
            {
                if (TraitGrowth != null)
                    throw new InvalidOperationException($"[StageOfferTextTable] 증강 글이 둘입니다: '{TraitGrowth.EventName}', '{data.EventName}'.");

                TraitGrowth = data;
                return;
            }

            if (data.IsCommonInfo)
            {
                if (!_commonByKind.TryAdd((data.StageType, data.MapRole), data))
                    throw new InvalidOperationException(
                        $"[StageOfferTextTable] 공통 줄이 겹칩니다: ({data.StageType}, {data.MapRole}) '{_commonByKind[(data.StageType, data.MapRole)].EventName}', '{data.EventName}'.");
                return;
            }

            foreach (int stageKey in data.StageKeys)
            {
                if (!_dataByStageKey.TryAdd(stageKey, data))
                    throw new InvalidOperationException($"[StageOfferTextTable] Duplicate stage key: {stageKey}.");
            }
        }
    }
}
