using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Tests.Framework
{
    public sealed class StageDataTableTests
    {
        private readonly List<ScriptableObject> assets = new();
        private static readonly Assembly SystemsAssembly =
            typeof(ProjectL.Systems.DataBaseSystem.Data.InGame.Enum.StageEnum).Assembly;

        private sealed class Items : IDataProvider<IDatabaseItem>
        {
            internal readonly List<IDatabaseItem> Values = new();
            public IEnumerable<IDatabaseItem> GetAllData() => Values;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var asset in assets) UnityEngine.Object.DestroyImmediate(asset);
            assets.Clear();
        }

        [TestCase("Map", "mapKey", "MapKey", "GetStageMap", "GetAllStageMaps")]
        public void Table_LooksUpIndependentKeys_AndReturnsAll(
            string kind, string keyField, string keyProperty, string lookup, string all)
        {
            var first = CreateItem(kind, keyField, 7);
            var second = CreateItem(kind, keyField, 42);
            var table = CreateTable(kind, first, second);
            var found = table.GetType().GetMethod(lookup).Invoke(table, new object[] { 42 });
            Assert.That(GetKey(found, keyProperty), Is.EqualTo(42));
            var values = (IEnumerable)table.GetType().GetMethod(all).Invoke(table, null);
            var keys = new List<object>();
            foreach (var value in values) keys.Add(GetKey(value, keyProperty));
            CollectionAssert.AreEquivalent(new object[] { 7, 42 }, keys);
            var exception = Assert.Throws<TargetInvocationException>(
                () => table.GetType().GetMethod(lookup).Invoke(table, new object[] { 999 }));
            Assert.That(exception.InnerException, Is.TypeOf<KeyNotFoundException>());
        }

        [TestCase("Map", "mapKey")]
        public void Table_RejectsDuplicateKeys(string kind, string keyField)
        {
            var first = CreateItem(kind, keyField, 7);
            var second = CreateItem(kind, keyField, 7);
            var exception = Assert.Throws<TargetInvocationException>(() => CreateTable(kind, first, second));
            Assert.That(exception.InnerException, Is.TypeOf<InvalidOperationException>());
        }

        private DatabaseItem CreateItem(string kind, string keyField, int key)
        {
            var type = SystemsAssembly.GetType(
                $"ProjectL.Systems.DataBaseSystem.Data.InGame.Table.{kind}.Item.Stage{kind}DataSO", true);
            var item = (DatabaseItem)ScriptableObject.CreateInstance(type);
            assets.Add(item);
            // GetData는 매번 새 순수 데이터를 만들므로 입력 계층인 SO 필드에 값을 넣는다.
            Set(item, keyField, key);
            return item;
        }

        private static object CreateTable(string kind, params DatabaseItem[] items)
        {
            var provider = new Items();
            provider.Values.AddRange(items);
            return Activator.CreateInstance(SystemsAssembly.GetType(
                $"ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable.Stage{kind}Table", true),
                BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { provider }, null);
        }

        private static object GetKey(object target, string property)
        {
            return target.GetType().GetProperty(property).GetValue(target);
        }

        private static void Set(object target, string field, object value)
        {
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
