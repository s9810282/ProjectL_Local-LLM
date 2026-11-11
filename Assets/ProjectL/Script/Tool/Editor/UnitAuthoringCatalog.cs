using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Tool.Editor
{
    /// <summary>유닛 하나(Unit SO 기준)의 목록 행입니다. 카탈로그를 만들 때 한 번 채우고 화면은 읽기만 합니다.</summary>
    internal sealed class UnitAuthoringRow
    {
        internal Object Unit;
        internal Object Visual;
        internal int Key;
        internal string KeyName;
        internal string DisplayName;
        internal string Eligibility;
        internal Sprite Icon;
        internal bool DuplicateUnitKey;
        internal bool SourceUsable;
        internal readonly List<Object> UnitTables = new();
        internal readonly List<Object> VisualTables = new();
        internal readonly List<Object> Rosters = new();
        internal readonly List<string> Problems = new();

        internal string Label => (string.IsNullOrEmpty(DisplayName) ? Unit.name : DisplayName) + $"  ({KeyName} = {Key})";
        internal bool IsAlly => Eligibility != "EnemyOnly";
        internal bool IsEnemy => Eligibility != "PlayerOnly";
    }

    /// <summary>유닛·외형·테이블·로스터·EntityKey 소스를 한 번 읽어 보관합니다(설계 UA1). 화면은 이 값만 읽습니다.</summary>
    internal sealed class UnitAuthoringCatalog
    {
        internal readonly List<UnitAuthoringRow> Rows = new();
        internal List<Object> Visuals = new();
        internal List<Object> UnitTables = new();
        internal List<Object> VisualTables = new();
        internal List<Object> Rosters = new();
        internal readonly Dictionary<Object, int> RosterStageCounts = new();
        internal readonly HashSet<int> UsedKeys = new();
        internal UnitEntityKeySource KeySource;
        internal string KeySourceError;

        internal UnitAuthoringRow Find(Object unit) => unit == null ? null : Rows.FirstOrDefault(row => row.Unit == unit);

        internal KeyValuePair<string, int>[] UnusedKeys() => KeySource == null
            ? Array.Empty<KeyValuePair<string, int>>()
            : KeySource.Entries.Where(pair => pair.Key.StartsWith("Unit_", StringComparison.Ordinal) && !UsedKeys.Contains(pair.Value))
                .OrderBy(pair => pair.Value).ToArray();

        internal int SuggestKey()
        {
            IEnumerable<int> taken = UsedKeys.Concat(KeySource?.Entries.Values ?? Enumerable.Empty<int>());
            int value = taken.DefaultIfEmpty(0).Max() + 1;
            while (UsedKeys.Contains(value) || (KeySource != null && KeySource.Entries.ContainsValue(value))) value++;
            return value;
        }

        internal static UnitAuthoringCatalog Build()
        {
            var catalog = new UnitAuthoringCatalog();
            try { catalog.KeySource = new UnitEntityKeySource(); }
            catch (Exception exception) { catalog.KeySourceError = "EntityKey.cs: " + exception.Message; }
            var names = catalog.KeySource?.Entries.ToDictionary(pair => pair.Value, pair => pair.Key) ?? new Dictionary<int, string>();

            List<Object> units = UnitAuthoringAssets.Find(UnitAuthoringAssets.UnitType);
            catalog.Visuals = UnitAuthoringAssets.Find(UnitAuthoringAssets.VisualType);
            catalog.UnitTables = UnitAuthoringAssets.Find(UnitAuthoringAssets.UnitTableType);
            catalog.VisualTables = UnitAuthoringAssets.Find(UnitAuthoringAssets.VisualTableType);
            catalog.Rosters = UnitAuthoringAssets.Find(UnitAuthoringRosters.RosterType);

            var visualsByKey = new Dictionary<int, List<Object>>();
            foreach (Object visual in catalog.Visuals)
            {
                if (!TryKey(visual, out int key)) continue;
                catalog.UsedKeys.Add(key);
                if (!visualsByKey.TryGetValue(key, out var list)) visualsByKey[key] = list = new List<Object>();
                list.Add(visual);
            }
            var unitKeys = new Dictionary<Object, int>();
            foreach (Object unit in units)
                if (TryKey(unit, out int key)) { unitKeys[unit] = key; catalog.UsedKeys.Add(key); }

            var tableItems = new Dictionary<Object, HashSet<Object>>();
            foreach (Object table in catalog.UnitTables.Concat(catalog.VisualTables))
                tableItems[table] = Items(table, "databaseItems");
            var rosterItems = catalog.Rosters.ToDictionary(roster => roster, roster => Items(roster, UnitAuthoringRosters.Members));
            foreach (Object roster in catalog.Rosters) catalog.RosterStageCounts[roster] = 0;
            foreach (Object roster in UnitAuthoringRosters.StageRosters())
                if (roster != null && catalog.RosterStageCounts.ContainsKey(roster)) catalog.RosterStageCounts[roster]++;

            foreach (Object unit in units)
            {
                var row = new UnitAuthoringRow { Unit = unit };
                try
                {
                    if (!unitKeys.TryGetValue(unit, out row.Key)) throw new InvalidOperationException("Key를 읽을 수 없습니다.");
                    row.KeyName = names.TryGetValue(row.Key, out string name) ? name : "(EntityKey에 없음)";
                    row.DuplicateUnitKey = unitKeys.Count(pair => pair.Value == row.Key) > 1;
                    if (row.DuplicateUnitKey) row.Problems.Add("같은 Key의 Unit SO가 여러 개입니다.");
                    using (var serialized = new SerializedObject(unit))
                    {
                        SerializedProperty eligibility = serialized.FindProperty("ownerEligibility");
                        row.Eligibility = eligibility != null && eligibility.enumValueIndex >= 0 && eligibility.enumValueIndex < eligibility.enumNames.Length
                            ? eligibility.enumNames[eligibility.enumValueIndex] : "";
                    }
                    visualsByKey.TryGetValue(row.Key, out var pairs);
                    if (pairs == null || pairs.Count == 0) row.Problems.Add("짝 Visual이 없습니다.");
                    else if (pairs.Count > 1) row.Problems.Add("짝 Visual이 여러 개입니다: " + string.Join(", ", pairs.Select(asset => asset.name)));
                    else row.Visual = pairs[0];
                    if (row.Visual != null)
                    {
                        using var serialized = new SerializedObject(row.Visual);
                        row.DisplayName = serialized.FindProperty(UnitAuthoringAssets.VisualRoot + ".displayName")?.stringValue;
                        row.Icon = serialized.FindProperty(UnitAuthoringAssets.VisualRoot + ".infoIcon")?.objectReferenceValue as Sprite;
                    }
                    bool unitValid = Check(row, unit, "규칙");
                    bool visualValid = row.Visual != null && Check(row, row.Visual, "외형");
                    foreach (var pair in tableItems)
                    {
                        if (pair.Value.Contains(unit)) row.UnitTables.Add(pair.Key);
                        if (row.Visual != null && pair.Value.Contains(row.Visual)) row.VisualTables.Add(pair.Key);
                    }
                    if (row.UnitTables.Count == 0) row.Problems.Add("Unit 테이블에 등록되지 않았습니다.");
                    if (row.Visual != null && row.VisualTables.Count == 0) row.Problems.Add("Visual 테이블에 등록되지 않았습니다.");
                    foreach (var pair in rosterItems)
                        if (pair.Value.Contains(unit)) row.Rosters.Add(pair.Key);
                    row.SourceUsable = !row.DuplicateUnitKey && row.Visual != null && unitValid && visualValid;
                }
                catch (Exception exception) { row.Problems.Add(exception.Message); }
                catalog.Rows.Add(row);
            }
            catalog.Rows.Sort((a, b) => a.Key.CompareTo(b.Key));
            return catalog;
        }

        private static bool TryKey(Object asset, out int key)
        {
            try { key = UnitAuthoringAssets.Key(asset); return true; }
            catch (Exception) { key = 0; return false; }
        }

        private static bool Check(UnitAuthoringRow row, Object asset, string label)
        {
            try { UnitAuthoringAssets.ValidateData(asset); return true; }
            catch (Exception exception)
            {
                row.Problems.Add($"{label} 검증 실패: {exception.Message}");
                return false;
            }
        }

        private static HashSet<Object> Items(Object owner, string path)
        {
            var result = new HashSet<Object>();
            using var serialized = new SerializedObject(owner);
            SerializedProperty items = serialized.FindProperty(path);
            if (items == null || !items.isArray) return result;
            for (int i = 0; i < items.arraySize; i++)
            {
                Object item = items.GetArrayElementAtIndex(i).objectReferenceValue;
                if (item != null) result.Add(item);
            }
            return result;
        }
    }
}
