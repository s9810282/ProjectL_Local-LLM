using System;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.View;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Artifact;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Artifact.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.Factory.DBFactory.Group;
using UnityEditor;

namespace ProjectL.Tests.Framework
{
    public sealed class ArtifactPopupConfigTests
    {
        [Test]
        public void Table_RejectsEmptyOrMultipleConfigurations()
        {
            var assets = new DataBaseTestAssets();
            try
            {
                var item = assets.Create<ArtifactPopupConfigDataSO>();
                Assert.Throws<InvalidOperationException>(() => new ArtifactPopupConfigTable(assets.Table<ArtifactPopupConfigDataTableSO>()));
                Assert.Throws<InvalidOperationException>(() => new ArtifactPopupConfigTable(assets.Table<ArtifactPopupConfigDataTableSO>(item, item)));
                Assert.DoesNotThrow(() => new ArtifactPopupConfigTable(assets.Table<ArtifactPopupConfigDataTableSO>(item)));
            }
            finally { assets.DestroyAll(); }
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void Configuration_RejectsInvalidSpacing(float spacing)
        {
            var config = new ArtifactPopupConfigData();
            DataBaseTestAssets.Set(config, "cellSpacing", spacing);
            Assert.Throws<InvalidOperationException>(config.Validate);
        }

        [Test]
        public void ActualViewDatabase_ProvidesLayoutAndModelsAndDescriptions()
        {
            var group = AssetDatabase.LoadAssetAtPath<ViewGroup>("Assets/ProjectL/DataBaseSO/ViewGroup/ViewGroup.asset");
            var query = (IViewQuery)ViewDataBaseFactory.Create(group);
            var layout = ((IHasArtifactPopupConfig)query).ArtifactPopupConfig.Config;
            Assert.That(layout.CellSpacing, Is.EqualTo(2.2f));
            foreach (int id in new[] { 1001, 1002, 1003 })
            {
                Assert.That(query.ArtifactViewTableQuery.TryGetArtifactView(id, out ArtifactViewData data), Is.True);
                Assert.That(data.Prefab, Is.Not.Null);
                Assert.That(data.Description, Is.Not.Empty);
            }
        }
    }
}
