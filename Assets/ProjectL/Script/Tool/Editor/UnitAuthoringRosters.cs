using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Tool.Editor
{
    /// <summary>UnitRoster 넣기·빼기와 유닛을 쓰는 곳 찾기입니다(설계 UA4·UA9). 다른 SO는 직렬화 필드 이름으로 읽습니다(계획 P4).</summary>
    internal static class UnitAuthoringRosters
    {
        internal const string RosterType = "UnitRosterDataSO";
        internal const string RosterTableType = "UnitRosterDataTableSO";
        internal const string Members = "unitDefinitions";
        private const string StageType = "CombatStageData";
        private static readonly string[] UsageTypes = { "EnemyLoadoutDataSO", "PlayerLoadoutDataSO", StageType, RosterType };
        private static readonly HashSet<string> Warned = new();

        internal static bool Contains(Object roster, Object unit)
        {
            using var serialized = new SerializedObject(roster);
            SerializedProperty items = RequireMembers(serialized);
            for (int i = 0; i < items.arraySize; i++)
                if (items.GetArrayElementAtIndex(i).objectReferenceValue == unit) return true;
            return false;
        }

        /// <summary>목록 끝에 붙입니다(계획 P3). undo가 거짓이면 Undo 기록 없이 씁니다(생성 요청).</summary>
        internal static void Add(Object roster, Object unit, bool undo)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (Contains(roster, unit)) return;
            using var serialized = new SerializedObject(roster);
            SerializedProperty items = RequireMembers(serialized);
            int index = items.arraySize++;
            items.GetArrayElementAtIndex(index).objectReferenceValue = unit;
            if (undo) serialized.ApplyModifiedProperties();
            else serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>그 유닛 칸만 뺍니다. 다른 칸 순서는 그대로입니다(계획 P3).</summary>
        internal static void Remove(Object roster, Object unit, bool undo)
        {
            using var serialized = new SerializedObject(roster);
            SerializedProperty items = RequireMembers(serialized);
            for (int i = items.arraySize - 1; i >= 0; i--)
            {
                if (items.GetArrayElementAtIndex(i).objectReferenceValue != unit) continue;
                items.GetArrayElementAtIndex(i).objectReferenceValue = null;
                items.DeleteArrayElementAtIndex(i);
            }
            if (undo) serialized.ApplyModifiedProperties();
            else serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static int RosterKey(Object roster)
        {
            using var serialized = new SerializedObject(roster);
            return serialized.FindProperty("rosterKey")?.intValue ?? 0;
        }

        /// <summary>전투 스테이지마다 참조하는 로스터입니다.</summary>
        internal static IEnumerable<Object> StageRosters()
        {
            foreach (Object stage in UnitAuthoringAssets.Find(StageType))
            {
                using var serialized = new SerializedObject(stage);
                SerializedProperty roster = serialized.FindProperty("unitRoster");
                if (roster == null) { WarnOnce(StageType + ".unitRoster"); continue; }
                yield return roster.objectReferenceValue;
            }
        }

        /// <summary>이 로스터를 쓰는 스테이지에서 EnemyLoadout·증원 풀이 이 유닛을 쓰는 곳입니다(설계 UA4, Q-UA2 A).</summary>
        internal static List<string> LoadoutUses(Object roster, Object unit)
        {
            var uses = new List<string>();
            foreach (Object stage in UnitAuthoringAssets.Find(StageType))
            {
                using var serialized = new SerializedObject(stage);
                SerializedProperty stageRoster = serialized.FindProperty("unitRoster");
                if (stageRoster == null) { WarnOnce(StageType + ".unitRoster"); continue; }
                if (stageRoster.objectReferenceValue != roster) continue;
                SerializedProperty loadout = serialized.FindProperty("enemyLoadout");
                if (loadout == null) WarnOnce(StageType + ".enemyLoadout");
                else if (loadout.objectReferenceValue != null && References(loadout.objectReferenceValue, unit))
                    uses.Add($"{stage.name}: EnemyLoadout {loadout.objectReferenceValue.name}");
                SerializedProperty nexus = serialized.FindProperty("nexusReinforcements");
                if (nexus == null) WarnOnce(StageType + ".nexusReinforcements");
                else if (References(nexus, unit)) uses.Add($"{stage.name}: 증원 풀");
            }
            return uses;
        }

        /// <summary>이 유닛 SO를 참조하는 로드아웃·스테이지·로스터와 필드 경로입니다(설계 UA9, 읽기만).</summary>
        internal static List<KeyValuePair<Object, string>> Usages(Object unit)
        {
            var result = new List<KeyValuePair<Object, string>>();
            foreach (string type in UsageTypes)
            foreach (Object asset in UnitAuthoringAssets.Find(type))
            {
                using var serialized = new SerializedObject(asset);
                SerializedProperty iterator = serialized.GetIterator();
                while (iterator.Next(true))
                    if (iterator.propertyType == SerializedPropertyType.ObjectReference && iterator.objectReferenceValue == unit)
                        result.Add(new KeyValuePair<Object, string>(asset, iterator.propertyPath));
            }
            return result;
        }

        private static bool References(Object owner, Object target)
        {
            using var serialized = new SerializedObject(owner);
            SerializedProperty iterator = serialized.GetIterator();
            while (iterator.Next(true))
                if (iterator.propertyType == SerializedPropertyType.ObjectReference && iterator.objectReferenceValue == target) return true;
            return false;
        }

        private static bool References(SerializedProperty property, Object target)
        {
            SerializedProperty iterator = property.Copy();
            SerializedProperty end = property.GetEndProperty();
            while (iterator.Next(true) && !SerializedProperty.EqualContents(iterator, end))
                if (iterator.propertyType == SerializedPropertyType.ObjectReference && iterator.objectReferenceValue == target) return true;
            return false;
        }

        private static SerializedProperty RequireMembers(SerializedObject serialized) =>
            UnitAuthoringAssets.Require(serialized, Members);

        private static void WarnOnce(string field)
        {
            if (Warned.Add(field)) Debug.LogWarning($"[유닛 통합 에디터] {field} 필드를 찾지 못해 이 검사를 건너뜁니다.");
        }
    }
}
