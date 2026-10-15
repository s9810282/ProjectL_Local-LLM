using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Light.Enum;
using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Light.View.Table.Item;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase.RenderTable
{
    #region 서비스

    internal interface ILightTableQuery : IServiceModule
    {
        IReadOnlyCollection<LightData> AllLights { get; }

        LightData GetLight(LightPresetType type);

        bool TryGetLight(LightPresetType type, out LightData lightData);
    }

    #endregion

    #region 시스템 모듈

    internal class LightTable : ILightTableQuery, IExportSystemServices
    {
        private readonly HashSet<LightPresetType> _lightTypes = new();
        private readonly Dictionary<LightPresetType, LightData> _lightDict = new();

        public IReadOnlyCollection<LightData> AllLights => _lightDict.Values;

        internal LightTable(IDataProvider<IDatabaseItem> datas)
        {
            if (datas == null) throw new ArgumentNullException(nameof(datas));

            foreach (var lightDataSO in datas.GetAllData().OfType<LightDataSO>())
            {
                if (lightDataSO.GetData() is LightData lightData)
                {
                    AddLightData(lightData);
                }
            }
        }

        public LightData GetLight(LightPresetType type)
        {
            if (_lightDict.TryGetValue(type, out var data))
            {
                return data;
            }

            throw new KeyNotFoundException($"[LightTable] Light preset type {type} was not found.");
        }

        public bool TryGetLight(LightPresetType type, out LightData lightData)
        {
            return _lightDict.TryGetValue(type, out lightData);
        }

        public void AddLightData(LightData data)
        {
            ValidateData(data);

            _lightTypes.Add(data.PresetType);
            _lightDict.Add(data.PresetType, data);
        }

        private void ValidateData(LightData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data), "[LightTable] LightData instance is null.");
            }

            if (data.PresetType == LightPresetType.Empty)
            {
                throw new ArgumentException("[LightTable] Light preset type cannot be 'Empty'.");
            }

            if (data.LightPrefab == null)
            {
                throw new ArgumentNullException(nameof(data.LightPrefab), $"[LightTable] Light prefab is null. (Type: {data.PresetType})");
            }

            if (data.InitialPoolSize < 0)
            {
                throw new ArgumentException($"[LightTable] Initial pool size cannot be negative. (Type: {data.PresetType}, Size: {data.InitialPoolSize})");
            }

            if (_lightTypes.Contains(data.PresetType))
            {
                throw new InvalidOperationException($"[LightTable] Light preset type already exists. (Type: {data.PresetType})");
            }
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<ILightTableQuery>(this);
        }
    }

    #endregion
}
