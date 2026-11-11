using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ProjectL.Framework.DataBase;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Tool.Editor
{
    /// <summary>유닛 저작용 직렬화 경로와 에셋 검증·등록의 단일 경계입니다.</summary>
    internal static class UnitAuthoringAssets
    {
        internal const string UnitType = "UnitDataSO";
        internal const string VisualType = "UnitVisualDataSO";
        internal const string UnitTableType = "UnitDataTableSO";
        internal const string VisualTableType = "UnitVisualDataTableSO";
        internal const string UnitKey = "key";
        internal const string VisualRoot = "unitVisualData";
        internal const string VisualKey = VisualRoot + ".key";
        private const string TableItems = "databaseItems";

        internal static List<Object> Find(string type) => AssetDatabase.FindAssets("t:" + type, new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath).Distinct().Select(AssetDatabase.LoadMainAssetAtPath)
            .Where(asset => IsType(asset, type)).OrderBy(AssetDatabase.GetAssetPath).ToList();

        internal static bool IsType(Object asset, string type) => asset != null && asset.GetType().Name == type &&
            asset.GetType().Namespace != null && asset.GetType().Namespace.StartsWith("ProjectL.Systems.DataBaseSystem.Data.", StringComparison.Ordinal);

        internal static SerializedProperty Require(SerializedObject serialized, string path) =>
            serialized.FindProperty(path) ?? throw new InvalidOperationException($"{serialized.targetObject.name}: 필수 필드 {path}가 없습니다.");

        internal static int Key(Object asset)
        {
            RequireType(asset, IsType(asset, UnitType) ? UnitType : VisualType);
            using var serialized = new SerializedObject(asset);
            return Require(serialized, IsType(asset, UnitType) ? UnitKey : VisualKey).intValue;
        }

        internal static Object Pair(Object unit)
        {
            RequireType(unit, UnitType);
            int key = Key(unit);
            var matches = Find(VisualType).Where(asset => Key(asset) == key).ToList();
            if (matches.Count != 1)
                throw new InvalidOperationException($"{unit.name}, Key {key}: 짝 Visual이 {matches.Count}개입니다. " +
                    string.Join(", ", matches.Select(AssetDatabase.GetAssetPath)));
            return matches[0];
        }

        internal static string GuidOf(Object asset)
        {
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
            if (string.IsNullOrEmpty(guid)) throw new InvalidOperationException($"저장된 에셋이 필요합니다: {asset}");
            return guid;
        }

        internal static Object Load(string guid, string type)
        {
            Object asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid ?? ""));
            RequireType(asset, type);
            return asset;
        }

        /// <summary>원본 지문 불일치입니다. 창이 [바뀐 원본으로 계속]을 보입니다(설계 UA7).</summary>
        internal sealed class SourceChangedException : InvalidOperationException
        {
            internal SourceChangedException(string message) : base(message) { }
        }

        internal static string Fingerprint(Object asset)
        {
            if (EditorUtility.IsDirty(asset))
                throw new InvalidOperationException($"원본의 변경을 먼저 저장해 주세요: {AssetDatabase.GetAssetPath(asset)}");
            string path = AssetDatabase.GetAssetPath(asset);
            return UnitEntityKeySource.HashBytes(File.ReadAllBytes(path)) + ":" +
                UnitEntityKeySource.HashBytes(File.ReadAllBytes(path + ".meta"));
        }

        internal static void ValidateData(Object asset)
        {
            if (asset is not DatabaseItem item) throw new InvalidOperationException($"DatabaseItem이 아닙니다: {asset}");
            try
            {
                IData data = item.GetData() ?? throw new InvalidOperationException($"{asset.name}: GetData 결과가 없습니다.");
                MethodInfo validate = data.GetType().GetMethod("Validate", BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                if (validate == null || validate.ReturnType != typeof(void))
                    throw new InvalidOperationException($"{data.GetType().FullName}: Validate() 계약이 없습니다.");
                validate.Invoke(data, null);
            }
            catch (TargetInvocationException exception)
            {
                throw new InvalidOperationException($"{asset.name}: {exception.InnerException?.Message ?? exception.Message}", exception.InnerException ?? exception);
            }
        }

        internal static bool Contains(Object table, Object item)
        {
            using var serialized = new SerializedObject(table);
            var items = Require(serialized, TableItems);
            for (int i = 0; i < items.arraySize; i++)
                if (items.GetArrayElementAtIndex(i).objectReferenceValue == item) return true;
            return false;
        }

        internal static Object DefaultTable(string type, Object source)
        {
            var all = Find(type);
            var registered = all.Where(table => Contains(table, source)).ToList();
            return registered.Count == 1 ? registered[0] : registered.Count == 0 && all.Count == 1 ? all[0] : null;
        }

        internal static string Catalogs(Object unitTable, Object visualTable)
        {
            if (unitTable == null || visualTable == null) return "등록할 두 테이블을 선택해 주세요.";
            var catalogs = FindCatalogs(unitTable, visualTable);
            return catalogs.Count == 0 ? "두 테이블을 함께 포함하는 Catalog가 없습니다." :
                "연결된 Catalog: " + string.Join(", ", catalogs.Select(AssetDatabase.GetAssetPath));
        }

        private static List<Object> FindCatalogs(params Object[] required)
        {
            var results = new List<Object>();
            foreach (string guid in AssetDatabase.FindAssets("t:DatabaseCatalogSO", new[] { "Assets" }))
            {
                var catalog = AssetDatabase.LoadAssetAtPath<DatabaseCatalogSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (catalog == null) continue;
                var tables = catalog.GetAllData().OfType<DatabaseGroup>()
                    .Where(group => group != null).SelectMany(group => group.GetAllData()).OfType<DatabaseTable>().ToList();
                if (required.All(table => tables.Contains(table as DatabaseTable))) results.Add(catalog);
            }
            return results;
        }

        internal static string OutputPath(Object folder, string name)
        {
            string path = AssetDatabase.GetAssetPath(folder);
            if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path))
                throw new InvalidOperationException("저장할 프로젝트 폴더를 선택해 주세요.");
            string result = path.TrimEnd('/') + "/" + name + ".asset";
            ValidatePath(result);
            return result;
        }

        internal static void ValidatePath(string path)
        {
            string assets = Path.GetFullPath("Assets") + Path.DirectorySeparatorChar;
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal) ||
                !path.EndsWith(".asset", StringComparison.Ordinal) ||
                !Path.GetFullPath(path).StartsWith(assets, StringComparison.OrdinalIgnoreCase) ||
                !AssetDatabase.IsValidFolder(Path.GetDirectoryName(path).Replace('\\', '/')))
                throw new InvalidOperationException($"Assets 아래 기존 폴더의 .asset 경로가 필요합니다: {path}");
            string streaming = Path.GetFullPath("Assets/StreamingAssets") + Path.DirectorySeparatorChar;
            if (Path.GetFullPath(path).StartsWith(streaming, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"StreamingAssets에는 SO를 만들 수 없습니다: {path}");
        }

        internal static void RequireVacant(string path)
        {
            ValidatePath(path);
            if (File.Exists(path) || File.Exists(path + ".meta") || AssetDatabase.LoadMainAssetAtPath(path) != null)
                throw new InvalidOperationException($"이미 파일이 있습니다. 덮어쓰지 않습니다: {path}");
        }

        internal static void ValidateRequest(UnitAuthoringRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            Object unitTable = Load(request.unitTable, UnitTableType);
            Object visualTable = Load(request.visualTable, VisualTableType);
            if (string.IsNullOrEmpty(request.createdUnit) || string.IsNullOrEmpty(request.createdVisual))
            {
                Object unit = Load(request.unitSource, UnitType);
                Object visual = Load(request.visualSource, VisualType);
                if (Pair(unit) != visual || Find(UnitType).Count(asset => Key(asset) == Key(unit)) != 1)
                    throw new InvalidOperationException("원본 Unit·Visual의 키가 다르거나 같은 키의 원본이 중복입니다.");
                if (Fingerprint(unit) != request.unitFingerprint || Fingerprint(visual) != request.visualFingerprint)
                    throw new SourceChangedException("원본 SO가 요청 후 변경됐습니다. 바뀐 원본으로 계속하거나 요청을 취소해 주세요.");
                ValidateData(unit);
                ValidateData(visual);
            }
            if (FindCatalogs(unitTable, visualTable).Count == 0)
                throw new InvalidOperationException(Catalogs(unitTable, visualTable));
            CheckTable(unitTable, UnitType);
            CheckTable(visualTable, VisualType);
            Object createdUnit = CheckOutput(request.createdUnit, request.unitPath, UnitType, request.keyValue);
            Object createdVisual = CheckOutput(request.createdVisual, request.visualPath, VisualType, request.keyValue);
            if (request.unitRegistered && (createdUnit == null || !Contains(unitTable, createdUnit)))
                throw new InvalidOperationException("완료 기록이 있는 Unit 등록이 외부에서 제거됐습니다. 생성물과 테이블을 확인해 주세요.");
            if (request.visualRegistered && (createdVisual == null || !Contains(visualTable, createdVisual)))
                throw new InvalidOperationException("완료 기록이 있는 Visual 등록이 외부에서 제거됐습니다. 생성물과 테이블을 확인해 주세요.");
            foreach (Object asset in Find(UnitType).Concat(Find(VisualType)))
                if (Key(asset) == request.keyValue && asset != createdUnit && asset != createdVisual)
                    throw new InvalidOperationException($"새 Key {request.keyValue}를 사용한 SO가 있습니다: {AssetDatabase.GetAssetPath(asset)}");
            ValidateRosters(request, unitTable, createdUnit);
            if (string.Equals(request.unitPath, request.visualPath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Unit과 Visual의 저장 경로가 같습니다.");
        }

        private static void ValidateRosters(UnitAuthoringRequest request, Object unitTable, Object createdUnit)
        {
            string[] rosters = request.rosters ?? Array.Empty<string>();
            if (rosters.Length == 0) return;
            if (rosters.Distinct().Count() != rosters.Length) throw new InvalidOperationException("같은 UnitRoster를 두 번 골랐습니다.");
            var rosterTables = Find(UnitAuthoringRosters.RosterTableType);
            for (int i = 0; i < rosters.Length; i++)
            {
                Object roster = Load(rosters[i], UnitAuthoringRosters.RosterType);
                ValidateData(roster);
                Object table = rosterTables.FirstOrDefault(candidate => Contains(candidate, roster))
                    ?? throw new InvalidOperationException($"{roster.name}: UnitRoster 테이블에 등록되지 않은 로스터입니다.");
                if (FindCatalogs(unitTable, table).Count == 0)
                    throw new InvalidOperationException($"{roster.name}: Unit 테이블과 같은 Catalog에 있는 로스터가 아닙니다.");
                bool registered = request.rosterRegistered != null && i < request.rosterRegistered.Length && request.rosterRegistered[i];
                if (registered && (createdUnit == null || !UnitAuthoringRosters.Contains(roster, createdUnit)))
                    throw new InvalidOperationException($"완료 기록이 있는 {roster.name} 등록이 외부에서 제거됐습니다. 로스터를 확인해 주세요.");
            }
        }

        private static Object CheckOutput(string guid, string path, string type, int key)
        {
            if (!string.IsNullOrEmpty(guid)) return LoadOwned(guid, path, type, key);
            RequireVacant(path);
            return null;
        }

        internal static Object LoadOwned(string guid, string path, string type, int key)
        {
            Object asset = Load(guid, type);
            if (RequirePersistent(asset, path) != guid || Key(asset) != key)
                throw new InvalidOperationException($"보관된 생성물의 GUID·경로·Key가 달라졌습니다: {path}");
            ValidateData(asset);
            return asset;
        }

        private static string RequirePersistent(Object asset, string path)
        {
            ValidatePath(path);
            if (asset == null || !AssetDatabase.Contains(asset) || AssetDatabase.GetAssetPath(asset) != path ||
                !File.Exists(path) || !File.Exists(path + ".meta") || AssetDatabase.LoadMainAssetAtPath(path) != asset)
                throw new InvalidOperationException($"SO가 요청한 경로에 생성되지 않았습니다. Console의 생성 오류를 확인해 주세요: {path}");
            string guid = GuidOf(asset);
            if (AssetDatabase.GUIDToAssetPath(guid) != path)
                throw new InvalidOperationException($"생성된 SO의 GUID와 경로가 일치하지 않습니다: {path}");
            return guid;
        }

        internal static void SaveChecked(Object asset)
        {
            if (asset == null) throw new ArgumentNullException(nameof(asset));
            string path = AssetDatabase.GetAssetPath(asset);
            RequirePersistent(asset, path);
            AssetDatabase.SaveAssetIfDirty(asset);
            if (EditorUtility.IsDirty(asset) || !File.Exists(path))
                throw new IOException($"에셋 저장이 끝나지 않았습니다. 파일 권한과 Console을 확인해 주세요: {path}");
        }

        private static void CheckTable(Object table, string type)
        {
            using var serialized = new SerializedObject(table);
            var items = Require(serialized, TableItems);
            var keys = new HashSet<int>();
            for (int i = 0; i < items.arraySize; i++)
            {
                Object item = items.GetArrayElementAtIndex(i).objectReferenceValue;
                if (item == null) throw new InvalidOperationException($"{table.name}: {i}번 칸이 비었습니다.");
                if (!IsType(item, type)) throw new InvalidOperationException($"{table.name}: {i}번 칸이 {type}이 아닙니다: {item.name}");
                if (!keys.Add(Key(item))) throw new InvalidOperationException($"{table.name}: 중복 Key {Key(item)}가 있습니다.");
            }
        }

        internal static Object Create(Object source, string path, int key, string displayName, string description,
            int eligibilityIndex, Action<string> recordOwnership)
        {
            if (recordOwnership == null) throw new ArgumentNullException(nameof(recordOwnership));
            RequireVacant(path);
            Object clone = Object.Instantiate(source);
            try
            {
                clone.name = Path.GetFileNameWithoutExtension(path);
                using (var serialized = new SerializedObject(clone))
                {
                    Require(serialized, IsType(source, UnitType) ? UnitKey : VisualKey).intValue = key;
                    if (IsType(source, VisualType))
                    {
                        Require(serialized, VisualRoot + ".displayName").stringValue = displayName;
                        if (!string.IsNullOrEmpty(description)) Require(serialized, VisualRoot + ".description").stringValue = description;
                    }
                    else if (eligibilityIndex >= 0) Require(serialized, "ownerEligibility").enumValueIndex = eligibilityIndex;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                ValidateData(clone);
                AssetDatabase.CreateAsset(clone, path);
                RequirePersistent(clone, path);
                return clone;
            }
            finally
            {
                if (clone != null)
                {
                    // CreateAsset 이후 예외가 나도 살아 있는 복제본의 GUID를 먼저 기록하여 복구할 수 있게 합니다.
                    if (AssetDatabase.Contains(clone) && AssetDatabase.GetAssetPath(clone) == path) recordOwnership(GuidOf(clone));
                    else if (!AssetDatabase.Contains(clone)) Object.DestroyImmediate(clone);
                }
            }
        }

        /// <summary>짝 Visual이 없는 유닛에 다른 Visual을 복제해 붙입니다(설계 UA2, 계획 P5). enum은 바꾸지 않습니다.</summary>
        internal static Object CreatePair(Object sourceVisual, Object unit, Object visualTable)
        {
            RequireType(sourceVisual, VisualType);
            RequireType(unit, UnitType);
            RequireType(visualTable, VisualTableType);
            int key = Key(unit);
            if (Find(VisualType).Any(asset => Key(asset) == key))
                throw new InvalidOperationException($"Key {key}의 Visual이 이미 있습니다.");
            string name = unit.name.StartsWith("Unit_", StringComparison.Ordinal) ? unit.name.Substring(5) : unit.name;
            string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(sourceVisual)).Replace('\\', '/');
            string path = folder + "/UnitVisual_" + name + ".asset";
            string created = null;
            try
            {
                Object visual = Create(sourceVisual, path, key, name, null, -1, guid => created = guid);
                Register(visualTable, visual);
                SaveChecked(visual);
                return visual;
            }
            catch
            {
                if (!string.IsNullOrEmpty(created))
                {
                    try { RemoveOwned(GuidOf(visualTable), created, path); }
                    catch (Exception cleanup) { Debug.LogError("[유닛 통합 에디터] 짝 Visual 정리 실패: " + cleanup.Message); }
                }
                throw;
            }
        }

        internal static void Register(Object table, Object item)
        {
            if (item == null) throw new InvalidOperationException($"{table.name}: 저장되지 않은 SO는 등록할 수 없습니다.");
            RequirePersistent(item, AssetDatabase.GetAssetPath(item));
            CheckTable(table, IsType(item, UnitType) ? UnitType : VisualType);
            if (Contains(table, item))
            {
                SaveChecked(table);
                return;
            }
            using var serialized = new SerializedObject(table);
            var items = Require(serialized, TableItems);
            for (int i = 0; i < items.arraySize; i++)
                if (Key(items.GetArrayElementAtIndex(i).objectReferenceValue) == Key(item))
                    throw new InvalidOperationException($"{table.name}: 이미 등록된 Key {Key(item)}입니다.");
            int index = items.arraySize++;
            items.GetArrayElementAtIndex(index).objectReferenceValue = item;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SaveChecked(table);
            if (!Contains(table, item)) throw new InvalidOperationException($"{table.name}: SO 등록 결과를 확인할 수 없습니다.");
        }

        internal static void RemoveOwned(string tableGuid, string assetGuid, string path)
        {
            if (string.IsNullOrEmpty(assetGuid)) return;
            ValidatePath(path);
            string actualPath = AssetDatabase.GUIDToAssetPath(assetGuid);
            if (string.IsNullOrEmpty(actualPath) && !File.Exists(path) && !File.Exists(path + ".meta")) return;
            if (!string.Equals(actualPath, path, StringComparison.Ordinal) || AssetDatabase.AssetPathToGUID(path) != assetGuid)
                throw new InvalidOperationException($"복구 대상의 GUID 또는 경로가 변경됐습니다. 직접 확인해 주세요: {path}");
            Object asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null) throw new InvalidOperationException($"복구 대상 에셋을 로드할 수 없습니다: {path}");
            Object table = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(tableGuid));
            RequireType(table, IsType(asset, UnitType) ? UnitTableType : VisualTableType);
            using (var serialized = new SerializedObject(table))
            {
                var items = Require(serialized, TableItems);
                for (int i = items.arraySize - 1; i >= 0; i--)
                {
                    if (items.GetArrayElementAtIndex(i).objectReferenceValue != asset) continue;
                    items.GetArrayElementAtIndex(i).objectReferenceValue = null;
                    items.DeleteArrayElementAtIndex(i);
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                SaveChecked(table);
            }
            if (!AssetDatabase.DeleteAsset(path)) throw new IOException($"이번 작업의 에셋 복구에 실패했습니다: {path}");
        }

        private static void RequireType(Object asset, string type)
        {
            if (!IsType(asset, type)) throw new InvalidOperationException($"{type} 에셋이 필요합니다: {asset}");
        }
    }
}
