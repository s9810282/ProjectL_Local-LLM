using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Effect.Enum;
using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Effect.View.Table.Item;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase.RenderTable
{
    #region 서비스

    internal interface IEffectTableQuery : IServiceModule
    {
        // Read-only collection of all effect data
        IReadOnlyCollection<EffectElementData> AllEffects { get; }

        // Get effect data by type
        EffectElementData GetEffect(EffectType type);

        // Safely get effect data by type
        bool TryGetEffect(EffectType type, out EffectElementData effectData);
    }

    #endregion

    #region 시스템 모듈

    internal class EffectTable : IEffectTableQuery, IExportSystemServices
    {
        private readonly HashSet<EffectType> _effectTypes = new();
        private readonly Dictionary<EffectType, EffectElementData> _effectDict = new();

        public IReadOnlyCollection<EffectElementData> AllEffects => _effectDict.Values;

        internal EffectTable(IDataProvider<IDatabaseItem> datas) 
        {
            if (datas == null) throw new ArgumentNullException(nameof(datas));

            foreach (var effectData in datas.GetAllData().OfType<EffectDataSO>())
            {
                if(effectData.GetData() is EffectElementData elementData) 
                {
                    AddEffectData(elementData);
                }
            }
        }

        public EffectElementData GetEffect(EffectType type)
        {
            if (_effectDict.TryGetValue(type, out var data))
            {
                return data;
            }

            throw new KeyNotFoundException($"[EffectTable] Effect type {type} was not found.");
        }

        public bool TryGetEffect(EffectType type, out EffectElementData effectData)
        {
            return _effectDict.TryGetValue(type, out effectData);
        }

        public void AddEffectData(EffectElementData data)
        {
            ValidateData(data);

            _effectTypes.Add(data.EffectType);
            _effectDict.Add(data.EffectType, data);
        }

        private void ValidateData(EffectElementData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data), "[EffectTable] EffectElementData instance is null.");
            }

            if (data.Prefab == null)
            {
                throw new ArgumentNullException(nameof(data.Prefab), $"[EffectTable] Effect prefab is null. (Type: {data.EffectType})");
            }

            if (data.EffectPoolMaxCount <= 0)
            {
                throw new ArgumentException($"[EffectTable] Max pool count must be greater than 0. (Type: {data.EffectType}, MaxCount: {data.EffectPoolMaxCount})");
            }

            if (data.EffectPoolInitializeCount < 0)
            {
                throw new ArgumentException($"[EffectTable] Initial pool count cannot be negative. (Type: {data.EffectType})");
            }

            if (data.EffectPoolInitializeCount > data.EffectPoolMaxCount)
            {
                throw new ArgumentException($"[EffectTable] Initial count ({data.EffectPoolInitializeCount}) is greater than max count ({data.EffectPoolMaxCount}). (Type: {data.EffectType})");
            }

            if (_effectTypes.Contains(data.EffectType))
            {
                throw new InvalidOperationException($"[EffectTable] Effect type already exists. (Type: {data.EffectType})");
            }
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IEffectTableQuery>(this);
        }
    }

    #endregion
}
