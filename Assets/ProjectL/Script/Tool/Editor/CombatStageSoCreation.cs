using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using Object = UnityEngine.Object;

namespace ProjectL.Tool.Editor
{
    // 새 스테이지 셋 만들기와 표 등록·해제입니다(설계 SM4·SM8, 계획 P2·P3·P5·P6·P12).
    // SO 필드는 CombatStageSoAssets로만 읽고 씁니다.
    internal static class CombatStageSoCreation
    {
        private const string RootPrefix = "Stage_", CombatPrefix = "Combat_", ViewPrefix = "StageViewProp_";

        // 틀 키의 백 단위 안에서 비어 있는 다음 키입니다(일반 11xx, 보스 21xx, 계획 P3). 백 단위가 차면 쓰인 키의 최댓값 + 1입니다.
        internal static int ProposeStageKey(int templateKey, ICollection<int> usedKeys)
        {
            int band = templateKey / 100 * 100;
            for (int key = band + 1; key < band + 100; key++)
                if (!usedKeys.Contains(key)) return key;
            return usedKeys.DefaultIfEmpty(0).Max() + 1;
        }

        internal static string NameError(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "새 스테이지 이름을 입력해 주세요.";
            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return "이름에 파일 이름으로 쓸 수 없는 글자가 있습니다.";
            return null;
        }

        // 틀 스테이지 셋을 같은 폴더에 복사해 새 셋을 만듭니다(계획 P2·P6). 새로 만들기는 배치를 비우고, 복제는 배치까지 둡니다.
        // 파일을 만들기 전에 새 작업본을 틀 복제본에 써서 검사하므로, 검사에 걸리면 아무 파일도 만들지 않습니다.
        // 만든 셋은 표에 등록하지 않습니다(설계 SM8). 실패하면 null을 내고 이유를 problems에 담습니다.
        internal static Object Create(Object templateRoot, Object templateCombat, Object templateView, string name, int stageKey,
            bool copyPlacements, ICollection<int> usedKeys, List<string> problems)
        {
            string nameError = NameError(name);
            if (nameError != null) problems.Add(nameError);
            if (templateView == null) problems.Add("틀 스테이지에 짝 View가 없어 새 스테이지를 만들 수 없습니다.");
            if (problems.Count > 0) return null;
            name = name.Trim();
            string rootPath = SiblingPath(templateRoot, RootPrefix + name);
            string combatPath = SiblingPath(templateCombat, CombatPrefix + name);
            string viewPath = SiblingPath(templateView, ViewPrefix + name);
            foreach (string path in new[] { rootPath, combatPath, viewPath })
                if (Exists(path)) problems.Add($"같은 이름의 파일이 있습니다: {path}");
            if (problems.Count > 0) return null;

            var document = CombatStageSoAssets.Read(templateRoot, templateView, new CombatStageSoNames());
            document.stageKey = stageKey;
            if (!copyPlacements) document.ClearPlacements();
            problems.AddRange(CombatStageSoValidation.CheckDocument(document, templateRoot, templateCombat, templateView, usedKeys, clearLegacyPlacements: !copyPlacements));
            if (problems.Count > 0) return null;

            foreach (var (source, target) in new[] { (templateCombat, combatPath), (templateRoot, rootPath), (templateView, viewPath) })
            {
                if (AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source), target)) continue;
                problems.Add($"파일을 복사하지 못했습니다: {target}");
                return null;
            }
            Object root = AssetDatabase.LoadMainAssetAtPath(rootPath);
            Object combat = AssetDatabase.LoadMainAssetAtPath(combatPath);
            Object view = AssetDatabase.LoadMainAssetAtPath(viewPath);
            CombatStageSoAssets.Write(document, root, combat, view);
            if (!copyPlacements) CombatStageSoAssets.ClearPlacementsOutsideLayout(combat);
            Save(root, combat, view);
            return root;
        }

        // 짝 View가 없는 스테이지에 틀 스테이지의 View를 복사해 붙입니다(계획 P12). 키는 그 스테이지 키로 바로 씁니다.
        internal static Object CreateView(Object stageRoot, Object templateView, int stageKey, List<string> problems)
        {
            if (templateView == null)
            {
                problems.Add("틀 스테이지에 짝 View가 없습니다.");
                return null;
            }
            string viewPath = SiblingPath(templateView, ViewPrefix + CombatStageSoAssets.ShortAssetName(stageRoot.name));
            if (Exists(viewPath))
            {
                problems.Add($"같은 이름의 파일이 있습니다: {viewPath}");
                return null;
            }
            if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(templateView), viewPath))
            {
                problems.Add($"파일을 복사하지 못했습니다: {viewPath}");
                return null;
            }
            Object view = AssetDatabase.LoadMainAssetAtPath(viewPath);
            CombatStageSoAssets.WriteViewStageKey(view, stageKey);
            Save(view);
            return view;
        }

        // 여러 스테이지가 함께 쓰는 공유 SO(필드·로스터·경제·적 로드아웃)를 같은 폴더에 복사해 이 스테이지 전용을 만듭니다(설계 SM7, T16).
        // 이름은 "<원본 이름>_<스테이지 이름>", 키는 새로 정하고(필드는 "<원본 fieldId>_<스테이지 키>"), 원본이 든 표에 등록합니다(계획 P5·P6).
        internal static Object CloneShared(Object source, string stageName, int stageKey, List<string> problems)
        {
            string tableType = CombatStageSoAssets.SharedTableType(source);
            if (tableType == null)
            {
                problems.Add("전용 복제를 할 수 없는 SO입니다. 필드·로스터·경제·적 로드아웃만 복제합니다.");
                return null;
            }
            string error = TableFor(tableType, source, out Object table);
            if (error != null)
            {
                problems.Add(error);
                return null;
            }
            Object copy = CopyNextTo(source, source.name + "_" + stageName, problems);
            if (copy == null) return null;
            CombatStageSoAssets.AssignNewSharedKey(copy, CombatStageSoAssets.SharedKeyText(source) + "_" + stageKey);
            CombatStageSoAssets.AddTableItem(table, copy);
            Save(copy, table);
            return copy;
        }

        // 크기를 넣어 새 필드를 만듭니다(Q-SM4 A, T18). 지금 필드를 복사해 값만 바꾸고, 이름은 "Field_<가로>x<세로>"(있으면 뒤에 스테이지 이름)입니다.
        // 적 배치 깊이는 Core가 읽지 않아 복사한 지금 필드 값을 그대로 둡니다(설계 P-AZ8).
        internal static Object CreateField(Object template, int columns, int rows, int reserve, int playerDepth,
            string stageName, List<string> problems)
        {
            if (columns < 1 || rows < 1 || reserve < 1 || playerDepth < 1)
                problems.Add("가로·세로·대기 칸·아군 줄 수는 모두 1 이상이어야 합니다.");
            // 줄 수 구역은 판 높이 이하여야 합니다(Core PlayerDeploymentZone.FitsBoard).
            if (rows >= 1 && playerDepth > rows)
                problems.Add("아군 줄 수는 세로보다 클 수 없습니다.");
            string tableType = CombatStageSoAssets.SharedTableType(template);
            if (tableType == null) problems.Add("지금 필드 SO가 없어 새 필드를 만들 수 없습니다. 먼저 필드를 고르세요.");
            if (problems.Count > 0) return null;
            string error = TableFor(tableType, template, out Object table);
            if (error != null)
            {
                problems.Add(error);
                return null;
            }
            string fileName = $"Field_{columns}x{rows}";
            if (Exists(SiblingPath(template, fileName))) fileName += "_" + stageName;
            Object field = CopyNextTo(template, fileName, problems);
            if (field == null) return null;
            CombatStageSoAssets.WriteFieldValues(field, columns, rows, reserve, playerDepth);
            CombatStageSoAssets.AssignNewSharedKey(field, $"field_{columns}x{rows}");
            CombatStageSoAssets.AddTableItem(table, field);
            Save(field, table);
            return field;
        }

        private static Object CopyNextTo(Object source, string fileName, List<string> problems)
        {
            string path = SiblingPath(source, fileName);
            if (Exists(path))
            {
                problems.Add($"같은 이름의 파일이 있습니다: {path}");
                return null;
            }
            if (AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source), path)) return AssetDatabase.LoadMainAssetAtPath(path);
            problems.Add($"파일을 복사하지 못했습니다: {path}");
            return null;
        }

        // 원본이 든 표에 넣습니다. 원본이 어느 표에도 없으면 그 형식 표가 하나일 때만 그 표를 씁니다(계획 P5).
        private static string TableFor(string tableType, Object source, out Object table)
        {
            table = CombatStageSoAssets.FindAll(tableType).FirstOrDefault(found => CombatStageSoAssets.TableContains(found, source));
            return table != null ? null : TableOf(tableType, out table);
        }

        // Root 표와 View 표에 함께 넣고 함께 뺍니다(설계 SM8). Group은 이미 두 표를 갖고 있어 바꾸지 않습니다.
        // 표는 형식으로 찾고, 같은 형식 표가 여럿이면 어느 표인지 정할 수 없어 거절합니다(계획 P5).
        // 빼기는 View 없이도 Root만 뺍니다. 보스 교체 때 짝 View가 없는 옛 보스를 빼려는 것입니다(병합 대응 계획 P-MG4).
        internal static string SetRegistered(Object root, Object view, bool registered)
        {
            if (root == null) return "Stage Root가 있어야 표에 넣거나 뺄 수 있습니다.";
            if (registered && view == null) return "Stage Root와 짝 View가 모두 있어야 표에 넣을 수 있습니다.";
            string error = TableOf(CombatStageSoAssets.RootTableType, out Object rootTable);
            if (error != null) return error;
            error = TableOf(CombatStageSoAssets.ViewTableType, out Object viewTable);
            if (error != null) return error;
            if (registered)
            {
                CombatStageSoAssets.AddTableItem(rootTable, root);
                CombatStageSoAssets.AddTableItem(viewTable, view);
            }
            else
            {
                CombatStageSoAssets.RemoveTableItem(rootTable, root);
                if (view != null) CombatStageSoAssets.RemoveTableItem(viewTable, view);
            }
            Save(rootTable, viewTable);
            return null;
        }

        private static string TableOf(string tableType, out Object table)
        {
            var tables = CombatStageSoAssets.FindAll(tableType);
            table = tables.Count == 1 ? tables[0] : null;
            if (tables.Count == 1) return null;
            return tables.Count == 0
                ? $"{tableType} 표를 찾을 수 없습니다."
                : $"{tableType} 표가 {tables.Count}개라 어느 표에 넣을지 정할 수 없습니다: {string.Join(", ", tables.Select(found => found.name))}";
        }

        private static string SiblingPath(Object asset, string fileName) =>
            Path.GetDirectoryName(AssetDatabase.GetAssetPath(asset)).Replace('\\', '/') + "/" + fileName + ".asset";

        private static bool Exists(string path) => AssetDatabase.LoadMainAssetAtPath(path) != null || File.Exists(path);

        private static void Save(params Object[] assets)
        {
            foreach (Object asset in assets)
            {
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssetIfDirty(asset);
            }
        }
    }
}
