using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Tool.Editor
{
    /// <summary>
    /// 유닛 삭제의 계획·실행입니다(2026-11-11 사용자 결정: 쓰는 곳을 경고한 뒤 참조도 함께 빼고, enum은 체크했을 때만 지움).
    /// 계획 단계는 아무것도 바꾸지 않고, 실행은 참조 빼기 → 테이블 빼기 → SO를 휴지통으로 → (선택) enum 순서입니다.
    /// </summary>
    internal static class UnitAuthoringDeletion
    {
        private static readonly Regex ArrayElement = new(@"^(?<array>.*)\.Array\.data\[(?<index>\d+)\]");
        private static readonly string[] ScannedExtensions = { ".asset", ".prefab", ".unity" };

        /// <summary>삭제 전에 보여 줄 내용입니다.</summary>
        internal sealed class Plan
        {
            internal Object Unit;
            internal Object Visual;
            internal string KeyName;
            internal int Key;
            internal readonly List<Object> Tables = new();
            /// <summary>도구가 자동으로 뺄 참조(로드아웃·스테이지·로스터의 칸).</summary>
            internal readonly List<KeyValuePair<Object, string>> References = new();
            /// <summary>자동으로 빼지 못하는 다른 파일의 참조(GUID가 들어 있는 파일).</summary>
            internal readonly List<string> OtherFiles = new();
            /// <summary>enum 이름을 코드에서 쓰는 곳. 있으면 enum 지우기를 막습니다.</summary>
            internal readonly List<string> CodeUses = new();

            internal string Describe(bool removeKey)
            {
                var lines = new List<string> { $"{Unit.name}" + (Visual != null ? $" + {Visual.name}" : "") + "을(를) 휴지통으로 옮깁니다." };
                if (Tables.Count > 0) lines.Add("테이블에서 뺌: " + string.Join(", ", Tables.Select(table => table.name)));
                if (References.Count > 0)
                {
                    lines.Add("");
                    lines.Add($"이 유닛을 쓰는 곳 {References.Count}칸 — 함께 뺍니다:");
                    lines.AddRange(References.Take(20).Select(pair => $"  · {pair.Key.name}: {pair.Value}"));
                    if (References.Count > 20) lines.Add($"  … 외 {References.Count - 20}칸");
                }
                if (OtherFiles.Count > 0)
                {
                    lines.Add("");
                    lines.Add("자동으로 빼지 못하는 참조(삭제 뒤 Missing이 됩니다):");
                    lines.AddRange(OtherFiles.Take(10).Select(path => "  · " + path));
                    if (OtherFiles.Count > 10) lines.Add($"  … 외 {OtherFiles.Count - 10}개");
                }
                lines.Add("");
                lines.Add(removeKey ? $"EntityKey.cs에서 {KeyName} = {Key}도 지우고 재컴파일합니다." : $"EntityKey {KeyName} = {Key}는 남깁니다.");
                return string.Join("\n", lines);
            }
        }

        internal static Plan Prepare(UnitAuthoringRow row, UnitAuthoringCatalog catalog)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));
            if (row.DuplicateUnitKey) throw new InvalidOperationException("같은 Key의 Unit SO가 여러 개라 삭제할 대상을 하나로 정할 수 없습니다.");
            var plan = new Plan { Unit = row.Unit, Visual = row.Visual, KeyName = row.KeyName, Key = row.Key };
            plan.Tables.AddRange(row.UnitTables);
            plan.Tables.AddRange(row.VisualTables);
            plan.References.AddRange(UnitAuthoringRosters.Usages(row.Unit));

            // 도구가 아는 곳 밖의 참조는 GUID로 파일을 훑어 알려만 줍니다.
            var own = new HashSet<string>(plan.Tables.Concat(plan.References.Select(pair => pair.Key))
                .Concat(new[] { row.Unit, row.Visual }).Where(asset => asset != null).Select(AssetDatabase.GetAssetPath));
            var guids = new[] { row.Unit, row.Visual }.Where(asset => asset != null).Select(UnitAuthoringAssets.GuidOf).ToArray();
            foreach (string path in Directory.EnumerateFiles("Assets", "*.*", SearchOption.AllDirectories))
            {
                if (!ScannedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase)) continue;
                string assetPath = path.Replace('\\', '/');
                if (own.Contains(assetPath)) continue;
                string text;
                try { text = File.ReadAllText(path); }
                catch (IOException) { continue; }
                if (guids.Any(guid => text.Contains(guid))) plan.OtherFiles.Add(assetPath);
            }

            // 키를 쓰는 다른 SO(구조물 등)가 있으면 enum을 지우지 못하게 코드 사용처와 함께 셉니다.
            if (catalog.Visuals.Concat(catalog.Rows.Select(other => other.Unit))
                .Any(asset => asset != row.Unit && asset != row.Visual && SafeKey(asset) == row.Key))
                plan.CodeUses.Add($"다른 SO가 Key {row.Key}를 씁니다.");
            var name = new Regex(@"\bEntityKey\." + Regex.Escape(row.KeyName ?? "") + @"\b");
            foreach (string root in new[] { "Assets/ProjectL/Script", "Assets/Tests" })
            {
                if (!Directory.Exists(root)) continue;
                foreach (string path in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
                {
                    string assetPath = path.Replace('\\', '/');
                    if (assetPath == UnitEntityKeySource.AssetPath) continue;
                    if (name.IsMatch(File.ReadAllText(path))) plan.CodeUses.Add(assetPath);
                }
            }
            return plan;
        }

        internal static void Execute(Plan plan, bool removeKey)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (UnitAuthoringOperation.HasPending) throw new InvalidOperationException("진행 중인 유닛 생성 요청을 먼저 끝내 주세요.");
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("편집 모드에서 컴파일·에셋 갱신이 끝난 뒤 실행해 주세요.");
            byte[] keyBytes = null;
            UnitEntityKeySource source = null;
            if (removeKey)
            {
                if (plan.CodeUses.Count > 0)
                    throw new InvalidOperationException("EntityKey를 지울 수 없습니다. 쓰는 곳:\n" + string.Join("\n", plan.CodeUses));
                source = new UnitEntityKeySource();
                keyBytes = source.RemovedBytes(plan.KeyName, plan.Key);
            }

            var warnings = new List<string>();
            foreach (var group in plan.References.GroupBy(pair => pair.Key))
            {
                RemoveReferences(group.Key, group.Select(pair => pair.Value).ToList(), plan.Unit);
                try { UnitAuthoringAssets.ValidateData(group.Key); }
                catch (Exception exception) { warnings.Add(exception.Message); }
            }
            foreach (Object table in plan.Tables)
            {
                using var serialized = new SerializedObject(table);
                SerializedProperty items = UnitAuthoringAssets.Require(serialized, "databaseItems");
                for (int i = items.arraySize - 1; i >= 0; i--)
                {
                    Object item = items.GetArrayElementAtIndex(i).objectReferenceValue;
                    if (item != plan.Unit && item != plan.Visual) continue;
                    items.GetArrayElementAtIndex(i).objectReferenceValue = null;
                    items.DeleteArrayElementAtIndex(i);
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                UnitAuthoringAssets.SaveChecked(table);
            }
            foreach (Object asset in new[] { plan.Unit, plan.Visual })
            {
                if (asset == null) continue;
                string path = AssetDatabase.GetAssetPath(asset);
                if (!AssetDatabase.MoveAssetToTrash(path)) throw new IOException($"휴지통으로 옮기지 못했습니다: {path}");
            }
            if (keyBytes != null)
            {
                source.Write(keyBytes, Guid.NewGuid().ToString("N"));
                AssetDatabase.ImportAsset(UnitEntityKeySource.AssetPath, ImportAssetOptions.ForceUpdate);
                CompilationPipeline.RequestScriptCompilation();
            }
            if (warnings.Count > 0)
                Debug.LogWarning("[유닛 통합 에디터] 참조를 뺀 뒤 검증에 실패한 SO가 있습니다. 직접 확인해 주세요:\n" + string.Join("\n", warnings));
        }

        /// <summary>참조 칸을 뺍니다. 배열 안 칸이면 그 원소를 지우고, 그냥 칸이면 비웁니다. 같은 배열은 뒤 번호부터 지웁니다.</summary>
        private static void RemoveReferences(Object owner, List<string> paths, Object unit)
        {
            using var serialized = new SerializedObject(owner);
            var elements = new List<(string array, int index)>();
            foreach (string path in paths)
            {
                MatchCollection matches = Regex.Matches(path, @"\.Array\.data\[\d+\]");
                if (matches.Count == 0)
                {
                    SerializedProperty field = serialized.FindProperty(path);
                    if (field != null && field.objectReferenceValue == unit) field.objectReferenceValue = null;
                    continue;
                }
                Match match = ArrayElement.Match(path.Substring(0, matches[matches.Count - 1].Index + matches[matches.Count - 1].Length));
                elements.Add((match.Groups["array"].Value, int.Parse(match.Groups["index"].Value)));
            }
            foreach (var element in elements.Distinct().OrderBy(item => item.array).ThenByDescending(item => item.index))
            {
                SerializedProperty array = serialized.FindProperty(element.array);
                if (array == null || !array.isArray || element.index >= array.arraySize) continue;
                SerializedProperty item = array.GetArrayElementAtIndex(element.index);
                if (item.propertyType == SerializedPropertyType.ObjectReference) item.objectReferenceValue = null;
                array.DeleteArrayElementAtIndex(element.index);
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(owner);
        }

        private static int SafeKey(Object asset)
        {
            try { return UnitAuthoringAssets.Key(asset); }
            catch (Exception) { return 0; }
        }
    }
}
