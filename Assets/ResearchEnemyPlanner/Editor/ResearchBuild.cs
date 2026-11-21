using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 연구 저장소 전용: 열려 있는 에디터에서 개발 빌드(플레이어)를 만든다(병렬 설계 §26).
/// 자동 재생 트리거 내용이 <c>build</c>이면 <see cref="ResearchAutoPlay"/>가 부른다.
/// 출력은 저장소 밖 임시 폴더(<c>%TEMP%/ProjectL_ResearchBuild</c>)다.
/// </summary>
internal static class ResearchBuild
{
    /// <summary>이 빌드가 도는 동안만 true. 아래 씬 처리기가 다른 빌드·재생에는 손대지 않게 한다.</summary>
    internal static bool Active;

    internal static string OutputFolder => Path.Combine(Path.GetTempPath(), "ProjectL_ResearchBuild");
    internal static string OutputPath => Path.Combine(OutputFolder, "ProjectL_Research.exe");

    internal static string Run()
    {
        var text = new StringBuilder();
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
            return "status=wrong-target target=" + EditorUserBuildSettings.activeBuildTarget;
        var options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = OutputPath,
            target = BuildTarget.StandaloneWindows64,
            // 개발 빌드: 개발 진입(전투로 바로)과 계획 AI 진단 줄이 들어간다.
            options = BuildOptions.Development
        };
        Active = true;
        BuildReport report;
        try { report = BuildPipeline.BuildPlayer(options); }
        finally { Active = false; }
        var summary = report.summary;
        text.AppendLine($"status={(summary.result == BuildResult.Succeeded ? "ok" : "build-" + summary.result)} errors={summary.totalErrors} seconds={summary.totalTime.TotalSeconds:F0}");
        text.AppendLine("output=" + OutputPath);
        text.AppendLine("backend=" + PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone));
        int shown = 0;
        foreach (var step in report.steps)
            foreach (var message in step.messages)
                if (message.type == LogType.Error && shown++ < 10)
                    text.AppendLine("error: " + message.content.Split('\n')[0]);
        return text.ToString();
    }
}

/// <summary>
/// 연구용 빌드에서만: 빌드되는 Boot 씬의 GameBootstrap에 이 PC의 "Play From Boot 설정"을 적용한다
/// (에디터 재생이 쓰는 것과 같은 함수 <c>ApplyEditorOverrides</c>). 씬 에셋 파일은 바뀌지 않는다.
/// 빌드에서는 에디터 덮어쓰기가 없어 Boot 씬에 저장된 값으로 시작하므로, 전투로 바로 들어가려면 이것이 필요하다.
/// </summary>
internal sealed class ResearchBuildSceneProcessor : IProcessSceneWithReport
{
    public int callbackOrder => 0;

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        if (!ResearchBuild.Active || report == null) return;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null || behaviour.GetType().Name != "GameBootstrap") continue;
                var apply = behaviour.GetType().GetMethod("ApplyEditorOverrides", BindingFlags.Instance | BindingFlags.NonPublic);
                if (apply == null) throw new BuildFailedException("[ResearchBuild] GameBootstrap.ApplyEditorOverrides를 찾지 못했습니다.");
                apply.Invoke(behaviour, null);
                Debug.Log("[ResearchBuild] Boot 씬의 GameBootstrap에 Play From Boot 설정을 적용했습니다(빌드 안에서만).");
            }
    }
}
