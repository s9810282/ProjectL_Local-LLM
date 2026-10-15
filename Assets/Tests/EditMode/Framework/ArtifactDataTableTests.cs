using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Artifact;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Artifact.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Artifact;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Artifact.Item;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.Factory.DBFactory;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using UnityEditor;

namespace ProjectL.Tests.Framework
{
    public sealed class ArtifactDataTableTests
    {
        private readonly DataBaseTestAssets _assets = new();

        [TearDown]
        public void TearDown() => _assets.DestroyAll();

        [Test]
        public void DefinitionTable_RejectsDuplicateIdsAndReportsMissingIds()
        {
            var first = _assets.Create<ArtifactDefinitionDataSO>();
            var second = _assets.Create<ArtifactDefinitionDataSO>();
            DataBaseTestAssets.Set(first, "artifactId", 1001);
            DataBaseTestAssets.Set(second, "artifactId", 1001);
            Assert.Throws<InvalidOperationException>(() => new ArtifactDefinitionTable(
                _assets.Table<ArtifactDefinitionDataTableSO>(first, second)));
            var table = new ArtifactDefinitionTable(
                _assets.Table<ArtifactDefinitionDataTableSO>(first));
            Assert.That(table.GetDefinition(1001).Cells.Count, Is.EqualTo(1));
            Assert.That(table.TryGetDefinition(9999, out _), Is.False);
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => table.GetDefinition(9999));
        }

        [Test]
        public void ViewTable_RejectsIncompletePresentation()
        {
            var item = _assets.Create<ArtifactViewDataSO>();
            var data = (ArtifactViewData)item.GetData();
            DataBaseTestAssets.Set(data, "artifactId", 1001);
            DataBaseTestAssets.Set(data, "displayName", "회복 성배");
            Assert.Throws<InvalidOperationException>(() => new ArtifactViewTable(
                _assets.Table<ArtifactViewDataTableSO>(item)), "아이콘 누락을 숨기면 안 됩니다.");
        }

        // G9 remains open. Replace this rejection evidence with acceptance when the DB owner migrates the SO.
        [Test]
        public void CurrentSoulDropAuthoring_IsRejectedByCanonicalCompiler()
        {
            // Load the real Artifact table through its normal adapter, independently of the pending stage Economy.
            var table = AssetDatabase.LoadAssetAtPath<ArtifactDefinitionDataTableSO>(
                "Assets/ProjectL/DataBaseSO/InGameGroup/Artifact/ArtifactDefinitionDataTable.asset");
            Assert.That(table, Is.Not.Null);
            var data = new ArtifactDefinitionTable(table).GetDefinition(1003);
            var effects = data.Effects.Select(effect =>
                new ArtifactPlacedEffectSnapshot(11, data.ArtifactId, effect)).ToArray();
            var error = Assert.Throws<ArgumentException>(() =>
                new SoulEffectCalculator(new ArtifactLoadoutSnapshot(1, effects)));
            Assert.That(error.Message, Does.Contain("source.faction"));
        }

        [Test]
        public void Catalog_RegistersMigratedRulesAndMatchingViews()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<DatabaseCatalogSO>(
                "Assets/ProjectL/DataBaseSO/DatabaseCatalogSO.asset");
            Assert.That(catalog, Is.Not.Null);
            var context = (IDataBaseQueryContext)DataBaseFactory.Create(catalog);
            var rules = context.InGameQuery.ArtifactDefinitionTableQuery;
            var views = context.ViewQuery.ArtifactViewTableQuery;
            CollectionAssert.AreEquivalent(new[] { 1001, 1002, 1003 },
                rules.GetAllDefinitions().Select(x => x.ArtifactId));
            // 등록된 View 에셋은 이전한 규칙과 같은 아티팩트 ID를 사용한다.
            CollectionAssert.AreEquivalent(new[] { 1001, 1002, 1003 },
                views.GetAllArtifactViews().Select(x => x.ArtifactId));
            Assert.That(rules.TryGetDefinition(1004, out _), Is.False);
            Assert.That(rules.TryGetDefinition(1011, out _), Is.False);
            Assert.That(rules.GetDefinition(1001).Effects[0].value.amount, Is.EqualTo(10));
            Assert.That(rules.GetDefinition(1002).Effects[0].value.amount, Is.EqualTo(3));
            Assert.That(rules.GetDefinition(1003).Effects.Count, Is.EqualTo(2));
            Assert.That(rules.GetDefinition(1001).Cells.Count, Is.EqualTo(1));
            Assert.That(rules.GetDefinition(1002).Cells.Count, Is.EqualTo(2));
            Assert.That(rules.GetDefinition(1003).Cells.Count, Is.EqualTo(4));
            foreach (var rule in rules.GetAllDefinitions())
                Assert.That(rule.InitialCount, Is.EqualTo(1));
        }
    }
}
