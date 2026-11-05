#if UNITY_EDITOR
using System;
using System.Linq;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.View;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Trait;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Trait.Item;
using UnityEditor;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev.TraitPreview.Scenario
{
    /// <summary>연결된 환경 또는 기본 Catalog의 View 등록을 읽습니다. Core 사용 가능 판정은 재현하지 않습니다.</summary>
    internal static class PreviewTraitRegistration
    {
        private const string CatalogPath = "Assets/ProjectL/DataBaseSO/DatabaseCatalogSO.asset";
        internal static TraitVisualDataSO[] Assets()
        {
            var host = TraitActivationPreviewBridge.Host;
            var catalog = host != null ? host.Catalog : AssetDatabase.LoadAssetAtPath<DatabaseCatalogSO>(CatalogPath);
            if (catalog == null) throw new InvalidOperationException($"특성 시연 Catalog가 없습니다: {CatalogPath}");
            var table = catalog.GetAllData().OfType<ViewGroup>().SelectMany(g => g.GetAllData()).OfType<TraitVisualDataTableSO>().SingleOrDefault();
            if (table == null) throw new InvalidOperationException("특성 시연 Catalog의 TraitVisual 테이블이 필요합니다.");
            return table.GetAllData().Select(item => item as TraitVisualDataSO ?? throw new InvalidOperationException("TraitVisual 테이블의 에셋 형식이 잘못됐습니다.")).ToArray();
        }
        internal static void Require(TraitVisualDataSO asset)
        {
            if (!Assets().Contains(asset))
                throw new ArgumentException($"활성 TraitVisual 표에 등록되지 않은 외형입니다: {asset?.name}");
        }
    }
}
#endif
