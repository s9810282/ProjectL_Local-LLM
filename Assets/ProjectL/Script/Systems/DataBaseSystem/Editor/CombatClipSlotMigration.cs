#if UNITY_EDITOR
using System.Text;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Editor
{
    /// <summary>
    /// 클립 칸 다시 짜기의 에셋 옮기기 도구입니다(설계 CS9, 결정 Q-A5 A, 계획 TP-1). 단계마다 메뉴 하나이고, 다시 돌려도 결과가 같습니다(이미 옮긴 값은 건너뜀).
    /// 바꾼 에셋은 콘솔에 남깁니다.
    /// </summary>
    /// <remarks>
    /// 단계 1(이벤트 지점·큐)·단계 2(클립 묶음)는 CS-a·CS-b 옛 칸을 옮겼고, 옛 칸을 지운 CS-c에서 메뉴를 걷었습니다(계획 §5.1·§5.2).
    /// 단계 3은 옛 시작 모드 칸을 읽으려고 그 칸을 한 Phase 남겨 둡니다(계획 TP-3).
    /// </remarks>
    internal static class CombatClipSlotMigration
    {
        private const string Menu = "ProjectL/Combat/클립 칸 옮기기 ";
        private const string DataRoot = "Assets/ProjectL/DataBaseSO";

        // 옛 시작 모드 값입니다: 0 기본 순차 재생, 1 지정 Kind 발동 시작, 2 휩쓸기 첫 걸음.
        private const int OldDefaultMode = 0;
        private const int OldSelectedTraitMode = 1;
        private const int OldSweepMode = 2;

        /// <summary>
        /// 단계 3: 클립 묶음의 옛 시작 모드(기본 / 지정 Kind / 휩쓸기)를 공통 시작 기준으로 옮깁니다(설계 CS11, 계획 Tc.6).
        /// 0 → <see cref="CombatAttackMotionStart.AfterLead"/>(값 그대로), 1·2 → <see cref="CombatAttackMotionStart.WithLead"/> + 옛 지연. 기준 Kind는 버립니다.
        /// 옮긴 뒤 옛 칸을 0으로 비워, 다시 돌리면 건너뜁니다.
        /// </summary>
        [MenuItem(Menu + "(3) 시작 기준")]
        private static void MigrateMotionStart()
        {
            var log = new StringBuilder("[CombatClipSlotMigration] 단계 3 — 시작 기준\n");
            int changed = 0;
            int skipped = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(CombatUnitAnimationSO), new[] { DataRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var bundle = AssetDatabase.LoadAssetAtPath<CombatUnitAnimationSO>(path);
                if (bundle == null) continue;
                var so = new SerializedObject(bundle);
                SerializedProperty oldMode = so.FindProperty("attackAnimationStartMode");
                SerializedProperty oldDelay = so.FindProperty("attackAnimationStartDelaySeconds");
                if (oldMode == null || oldDelay == null || oldMode.intValue == OldDefaultMode) continue;
                if (oldMode.intValue != OldSelectedTraitMode && oldMode.intValue != OldSweepMode)
                {
                    // 모르는 값은 옮기지 않습니다. 부트 검사가 옛 칸에 남은 값을 보고 멈춥니다.
                    log.AppendLine($"건너뜀(모르는 옛 모드 {oldMode.intValue}): {path}");
                    skipped++;
                    continue;
                }
                so.FindProperty("motionStart").intValue = (int)CombatAttackMotionStart.WithLead;
                so.FindProperty("motionStartDelay").floatValue = oldDelay.floatValue;
                log.AppendLine($"{path}: 옛 모드 {oldMode.intValue} → WithLead, 지연 {oldDelay.floatValue}초");
                oldMode.intValue = OldDefaultMode;
                oldDelay.floatValue = 0f;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(bundle);
                changed++;
            }

            AssetDatabase.SaveAssets();
            log.AppendLine($"바꾼 묶음 {changed}개, 건너뛴 묶음 {skipped}개");
            Debug.Log(log.ToString());
            EditorUtility.DisplayDialog("클립 칸 옮기기 (3)", $"바꾼 묶음 {changed}개, 건너뛴 묶음 {skipped}개입니다. 목록은 콘솔에 있습니다.", "확인");
        }
    }
}
#endif
