using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Boot;
using UnityEditor;

namespace ProjectL.Tests.Framework
{
    /// <summary>
    /// Boot smoke 테스트가 Boot 씬의 공용 시작 구간과 관계없이 StageSelect부터 시작하도록 이 PC의 Play From Boot 덮어쓰기를 잠시 켭니다.
    /// </summary>
    /// <remarks>
    /// 재생 진입 때 도메인이 다시 로드되므로 원래 값은 정적 필드가 아니라 EditorPrefs 백업 키에 둡니다.
    /// <see cref="Restore"/>는 백업이 있을 때만 원래 값(없던 키는 삭제)으로 되돌리고 백업을 지웁니다.
    /// </remarks>
    internal static class BootStageStartOverride
    {
        private const string Prefix = "ProjectL.BootOverride.";
        private const string Backup = "ProjectL.BootOverrideTestBackup.";
        private const string Saved = Backup + "Saved";
        private const int StageStart = 1;
        private const int LiveMode = 0;

        internal static void UseStageStart()
        {
            if (!EditorPrefs.GetBool(Saved, false))
            {
                BackupBool(nameof(GameBootstrapEditorOverrides.Enabled));
                BackupInt(nameof(GameBootstrapEditorOverrides.StartLifeCycle));
                BackupInt(nameof(GameBootstrapEditorOverrides.ExecutionMode));
                EditorPrefs.SetBool(Saved, true);
            }

            GameBootstrapEditorOverrides.Enabled = true;
            GameBootstrapEditorOverrides.StartLifeCycle = StageStart;
            GameBootstrapEditorOverrides.ExecutionMode = LiveMode;
        }

        internal static void Restore()
        {
            if (!EditorPrefs.GetBool(Saved, false))
                return;

            RestoreBool(nameof(GameBootstrapEditorOverrides.Enabled));
            RestoreInt(nameof(GameBootstrapEditorOverrides.StartLifeCycle));
            RestoreInt(nameof(GameBootstrapEditorOverrides.ExecutionMode));
            EditorPrefs.DeleteKey(Saved);
        }

        private static void BackupBool(string name)
        {
            bool has = EditorPrefs.HasKey(Prefix + name);
            EditorPrefs.SetBool(Backup + name + ".Has", has);
            if (has) EditorPrefs.SetBool(Backup + name, EditorPrefs.GetBool(Prefix + name));
        }

        private static void BackupInt(string name)
        {
            bool has = EditorPrefs.HasKey(Prefix + name);
            EditorPrefs.SetBool(Backup + name + ".Has", has);
            if (has) EditorPrefs.SetInt(Backup + name, EditorPrefs.GetInt(Prefix + name));
        }

        private static void RestoreBool(string name)
        {
            if (EditorPrefs.GetBool(Backup + name + ".Has", false)) EditorPrefs.SetBool(Prefix + name, EditorPrefs.GetBool(Backup + name));
            else EditorPrefs.DeleteKey(Prefix + name);
            EditorPrefs.DeleteKey(Backup + name);
            EditorPrefs.DeleteKey(Backup + name + ".Has");
        }

        private static void RestoreInt(string name)
        {
            if (EditorPrefs.GetBool(Backup + name + ".Has", false)) EditorPrefs.SetInt(Prefix + name, EditorPrefs.GetInt(Backup + name));
            else EditorPrefs.DeleteKey(Prefix + name);
            EditorPrefs.DeleteKey(Backup + name);
            EditorPrefs.DeleteKey(Backup + name + ".Has");
        }
    }
}
