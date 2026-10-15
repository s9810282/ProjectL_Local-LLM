using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Core.Table.Item;
using ProjectL.Systems.Factory.DBFactory;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tests.Framework
{
    public sealed class CoreTableQueryTests
    {
        private static readonly string[] Domains =
        {
            "Chapter", "Character", "Description", "Item", "Method",
            "Object", "Obstacle", "Stage", "Terrain", "UpgradeStatistic"
        };

        private static readonly Assembly Systems = typeof(StageMasterData).Assembly;
        private readonly List<ScriptableObject> assets = new();

        private sealed class Rows : IDataProvider<IDatabaseItem>
        {
            internal List<IDatabaseItem> Values = new();
            internal int ReadCount;

            public IEnumerable<IDatabaseItem> GetAllData()
            {
                ReadCount++;
                return Values;
            }
        }

        [TearDown]
        public void TearDown()
        {
            foreach (ScriptableObject asset in assets)
                UnityEngine.Object.DestroyImmediate(asset);
            assets.Clear();
        }

        [TestCaseSource(nameof(Domains))]
        public void Query_LooksUpKeys_AndPreservesSheetOrder(string domain)
        {
            IDatabaseItem first = CreateRow(domain, 42);
            IDatabaseItem second = CreateRow(domain, 7);
            var source = new Rows { Values = new List<IDatabaseItem> { first, second } };
            object query = CreateQuery(domain, source);

            Assert.That(Call(query, "Get" + domain, 7), Is.SameAs(second));
            object[] found = { 42, null };
            Assert.That(Call(query, "TryGet" + domain, found), Is.True);
            Assert.That(found[1], Is.SameAs(first));

            var all = (IList)Call(query, "GetAll" + domain + "s");
            CollectionAssert.AreEqual(new[] { first, second }, all);
            Assert.That(all.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => all.Clear());

            source.Values.Clear();
            Assert.That(Call(query, "Get" + domain, 7), Is.SameAs(second));
            Assert.That(all.Count, Is.EqualTo(2));
            Assert.That(source.ReadCount, Is.EqualTo(1));
        }

        [TestCaseSource(nameof(Domains))]
        public void Query_MissingKey_ThrowsOrReturnsFalse(string domain)
        {
            object query = CreateQuery(domain, new Rows { Values = new List<IDatabaseItem> { CreateRow(domain, 7) } });
            var exception = Assert.Throws<TargetInvocationException>(() => Call(query, "Get" + domain, 999));
            Assert.That(exception.InnerException, Is.TypeOf<KeyNotFoundException>());
            object[] missing = { 999, null };
            Assert.That(Call(query, "TryGet" + domain, missing), Is.False);
            Assert.That(missing[1], Is.Null);
        }

        [TestCaseSource(nameof(Domains))]
        public void Query_EmptySheet_IsValid(string domain)
        {
            object query = CreateQuery(domain, new Rows());
            Assert.That((ICollection)Call(query, "GetAll" + domain + "s"), Is.Empty);
            object[] missing = { 7, null };
            Assert.That(Call(query, "TryGet" + domain, missing), Is.False);
            Assert.That(missing[1], Is.Null);
        }

        [TestCaseSource(nameof(Domains))]
        public void Query_RejectsDuplicateKeys(string domain)
        {
            AssertConstructionFails<InvalidOperationException>(domain, new Rows
            {
                Values = new List<IDatabaseItem> { CreateRow(domain, 7), CreateRow(domain, 7) }
            });
        }

        [TestCaseSource(nameof(Domains))]
        public void Query_RejectsNonPositiveKeys(string domain)
        {
            foreach (int key in new[] { 0, -1 })
                AssertConstructionFails<ArgumentException>(domain, new Rows
                {
                    Values = new List<IDatabaseItem> { CreateRow(domain, key) }
                });
        }

        [TestCaseSource(nameof(Domains))]
        public void Query_RejectsNullAndUnexpectedRows(string domain)
        {
            AssertConstructionFails<InvalidOperationException>(domain, new Rows
            {
                Values = new List<IDatabaseItem> { null }
            });
            string other = domain == "Stage" ? "Item" : "Stage";
            AssertConstructionFails<InvalidOperationException>(domain, new Rows
            {
                Values = new List<IDatabaseItem> { CreateRow(other, 7) }
            });
        }

        [TestCaseSource(nameof(Domains))]
        public void Query_RejectsNullSourceAndCollection(string domain)
        {
            AssertConstructionFails<ArgumentNullException>(domain, null);
            AssertConstructionFails<InvalidOperationException>(domain, new Rows { Values = null });
        }

        [Test]
        public void DataBaseFactory_ExportsAllTenCoreQueries_AlongsideRender()
        {
            InGameGroup inGame = CreateAsset<InGameGroup>();
            var tables = new List<DatabaseTable>();
            var expected = new Dictionary<string, IDatabaseItem>();
            foreach (string domain in Domains)
            {
                var table = (DatabaseTable)CreateAsset(DataType(domain, "MasterDataTableSO"));
                IDatabaseItem row = CreateRow(domain, 7);
                ((IList)table.GetType().GetField(domain + "Master").GetValue(table)).Add(row);
                tables.Add(table);
                expected.Add(domain, row);
            }
            Set(typeof(DatabaseGroup), inGame, "databaseTables", tables);

            var render = (DatabaseGroup)CreateAsset(Systems.GetType(
                "ProjectL.Systems.DataBaseSystem.Data.Render.RenderGroup", true));
            var renderTables = new List<DatabaseTable>();
            foreach (string domain in new[] { "Effect", "Light", "Material" })
                renderTables.Add((DatabaseTable)CreateAsset(Systems.GetType(
                    $"ProjectL.Systems.DataBaseSystem.Data.Render.Table.{domain}.View.Table.{domain}DataTableSO", true)));
            Set(typeof(DatabaseGroup), render, "databaseTables", renderTables);

            DatabaseCatalogSO catalog = CreateAsset<DatabaseCatalogSO>();
            Set(typeof(DatabaseCatalogSO), catalog, "databaseGroups", new List<DatabaseGroup> { render, inGame });
            IServiceModule database = DataBaseFactory.Create(catalog);
            object inGameQuery = database.GetType().GetProperty("InGameQuery").GetValue(database);
            Assert.That(database.GetType().GetProperty("RenderQuery").GetValue(database), Is.Not.Null);
            foreach (string domain in Domains)
            {
                object query = inGameQuery.GetType().GetProperty(domain + "TableQuery").GetValue(inGameQuery);
                Assert.That(Call(query, "Get" + domain, 7), Is.SameAs(expected[domain]), domain);
            }

            var sink = new SystemBuilderHelper();
            ((IExportSystemServices)database).ExportServices(sink);
            Assert.That(ReadService(sink.BuildSystemReader(),
                "ProjectL.Systems.DataBaseSystem.IDataBaseQueryContext"), Is.SameAs(database));
        }

        [Test]
        public void InGameFactory_RejectsMissingTable()
        {
            var exception = Assert.Throws<TargetInvocationException>(() => CreateInGame(CreateAsset<InGameGroup>()));
            Assert.That(exception.InnerException, Is.TypeOf<InvalidOperationException>());
            StringAssert.Contains("ChapterMasterDataTableSO", exception.InnerException.Message);
        }

        [Test]
        public void ImportedGroup_ExportsEveryRegisteredRow()
        {
            InGameGroup group = AssetDatabase.LoadAssetAtPath<InGameGroup>(
                "Assets/ProjectL/DataBaseSO/InGameGroup/InGameGroup.asset");
            Assert.That(group, Is.Not.Null);
            object database = CreateInGame(group);
            var tables = new DataBaseRegistry<IDatabaseTable>(group);
            foreach (string domain in Domains)
            {
                var source = (DatabaseTable)typeof(DataBaseRegistry<IDatabaseTable>).GetMethod("GetDataBase")
                    .MakeGenericMethod(DataType(domain, "MasterDataTableSO")).Invoke(tables, null);
                IDatabaseItem[] expected = source.GetAllData().ToArray();
                object query = database.GetType().GetProperty(domain + "TableQuery").GetValue(database);
                CollectionAssert.AreEqual(expected, (IEnumerable)Call(query, "GetAll" + domain + "s"), domain);
                foreach (IDatabaseItem row in expected)
                {
                    int key = (int)row.GetType().GetProperty("Key").GetValue(row);
                    Assert.That(Call(query, "Get" + domain, key), Is.SameAs(row), domain);
                }
            }
        }

        private static IDatabaseItem CreateRow(string domain, int key)
        {
            Type type = DataType(domain, "MasterData");
            var row = (IDatabaseItem)Activator.CreateInstance(type);
            type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(field => field.Name.EndsWith("_Key", StringComparison.Ordinal)).SetValue(row, key);
            return row;
        }

        private static Type DataType(string domain, string suffix)
        {
            string item = suffix == "MasterData" ? ".Item" : string.Empty;
            return Systems.GetType(
                $"ProjectL.Systems.DataBaseSystem.Data.InGame.Table.{domain}.Core.Table{item}.{domain}{suffix}", true);
        }

        private static object CreateQuery(string domain, IDataProvider<IDatabaseItem> source) =>
            Activator.CreateInstance(Systems.GetType(
                $"ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.Table.{domain}.Core.{domain}Table", true),
                BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { source }, null);

        private static object Call(object query, string method, params object[] args) =>
            query.GetType().GetMethod(method).Invoke(query, args);

        private static void AssertConstructionFails<T>(string domain, IDataProvider<IDatabaseItem> source)
            where T : Exception
        {
            var exception = Assert.Throws<TargetInvocationException>(() => CreateQuery(domain, source));
            Assert.That(exception.InnerException, Is.TypeOf<T>());
        }

        private static object CreateInGame(InGameGroup group) =>
            typeof(ProjectL.Systems.Factory.DBFactory.Group.InGameDataBaseFactory)
                .GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { group });

        private static object ReadService(ISystemReader reader, string serviceName)
        {
            Type service = Systems.GetType(serviceName, true);
            Type key = typeof(SystemGroupKey<>).MakeGenericType(service);
            object groupKey = Activator.CreateInstance(key, new object[] { service.Name });
            return typeof(ISystemServiceStoreReader).GetMethod("GetService").MakeGenericMethod(service)
                .Invoke(reader.ServiceStoreReader, new[] { groupKey });
        }

        private T CreateAsset<T>() where T : ScriptableObject => (T)CreateAsset(typeof(T));

        private ScriptableObject CreateAsset(Type type)
        {
            ScriptableObject asset = ScriptableObject.CreateInstance(type);
            assets.Add(asset);
            return asset;
        }

        private static void Set(Type owner, object target, string field, object value) =>
            owner.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
