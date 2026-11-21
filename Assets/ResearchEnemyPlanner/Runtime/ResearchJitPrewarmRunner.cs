using System;
using UnityEngine;

/// <summary>
/// 연구 저장소 전용 실험(병렬 설계 §29): <see cref="ResearchJitPrewarm"/>의 메인 스레드 몫(정적 생성자가 있는 형식)을
/// 프레임마다 정해진 시간만큼 컴파일한다. 실행 인자 <c>-researchPrewarm</c>이 있을 때만 생긴다.
/// 프레임 예산은 <c>-researchPrewarmBudget 밀리초</c>(기본 3)다.
/// </summary>
public sealed class ResearchJitPrewarmRunner : MonoBehaviour
{
    private double _budget = 3;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        if (!ResearchJitPrewarm.Enabled) return;
        string[] args = Environment.GetCommandLineArgs();
        if (Array.IndexOf(args, "-researchPrewarmUnsafe") >= 0) return;
        var host = new GameObject("ResearchJitPrewarmRunner");
        DontDestroyOnLoad(host);
        var runner = host.AddComponent<ResearchJitPrewarmRunner>();
        int at = Array.IndexOf(args, "-researchPrewarmBudget");
        if (at >= 0 && at + 1 < args.Length && double.TryParse(args[at + 1], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double budget) && budget > 0) runner._budget = budget;
    }

    private void Update() => ResearchJitPrewarm.TickMain(_budget, Time.realtimeSinceStartupAsDouble);
}
