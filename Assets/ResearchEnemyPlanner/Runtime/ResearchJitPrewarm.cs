using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using UnityEngine;

/// <summary>
/// 연구 저장소 전용 실험(병렬 설계 §29): 게임 코드의 메서드를 시작할 때 미리 컴파일해 둔다.
/// Mono 빌드에서 메서드는 처음 불릴 때 컴파일되므로(JIT), 연출 종류마다 첫 재생 프레임이 길다는 추정(§27 (가))을 확인하려는 것이다.
/// 실행 인자 <c>-researchPrewarm 범위</c>가 있을 때만 켜진다. 범위는 형식 전체 이름에 들어 있어야 하는 글자다
/// (쉼표로 여럿, <c>*</c>는 게임 어셈블리 전부, <c>**</c>는 엔진·패키지 어셈블리까지). 예: <c>-researchPrewarm Combat</c>, <c>-researchPrewarm *</c>.
/// <para>
/// Mono는 메서드를 컴파일할 때 그 형식의 정적 생성자를 함께 돌린다(측정으로 확인: 일 스레드에서 컴파일하자 렌더 패스의 정적 필드가
/// "메인 스레드에서만" 예외로 깨졌다). 그래서 정적 생성자가 있는 형식은 메인 스레드에서 프레임마다 조금씩 컴파일하고
/// (<see cref="ResearchJitPrewarmRunner"/>), 없는 형식만 일 스레드에서 컴파일한다. <c>-researchPrewarmUnsafe</c>를 주면 옛 방식대로
/// 전부 일 스레드에서 한다(비교용).
/// </para>
/// 일반화(제네릭) 형식·메서드, 본문 없는 메서드는 건너뛴다.
/// </summary>
public static class ResearchJitPrewarm
{
    private static volatile string _thread = "", _main = "";
    private static volatile bool _enabled, _listed;
    private static readonly ConcurrentQueue<MethodBase[]> MainQueue = new ConcurrentQueue<MethodBase[]>();
    private static int _mainTypes, _mainMethods, _mainFailed, _mainFrames;
    private static long _mainTicks, _mainLongestTicks;
    private static double _mainDoneAt = -1;

    /// <summary>끝 상태 한 줄(자동 재생 도구가 결과 파일에 적는다).</summary>
    public static string Status => !_enabled ? "prewarm off" : "prewarm " + _thread + " | " + _main;

    internal static bool Enabled => _enabled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void Init()
    {
        string[] args = Environment.GetCommandLineArgs();
        int at = Array.IndexOf(args, "-researchPrewarm");
        if (at < 0 || at + 1 >= args.Length) return;
        string label = args[at + 1];
        string[] scopes = label.Split(',');
        bool allOnThread = Array.IndexOf(args, "-researchPrewarmUnsafe") >= 0;
        _enabled = true;
        _thread = "thread running scope=" + label;
        _main = allOnThread ? "main none(unsafe)" : "main waiting";
        double startedAt = Time.realtimeSinceStartupAsDouble;
        var thread = new Thread(() => Run(scopes, label, startedAt, allOnThread))
            { IsBackground = true, Priority = System.Threading.ThreadPriority.BelowNormal, Name = "ResearchJitPrewarm" };
        thread.Start();
    }

    private static bool InScope(string[] scopes, string name)
    {
        foreach (string scope in scopes)
            if (scope == "*" || scope == "**" || name.IndexOf(scope, StringComparison.Ordinal) >= 0) return true;
        return false;
    }

    private static bool Compilable(MethodBase method) =>
        !method.IsAbstract && !method.ContainsGenericParameters && (method.Attributes & MethodAttributes.PinvokeImpl) == 0 &&
        (method.GetMethodImplementationFlags() & (MethodImplAttributes.InternalCall | MethodImplAttributes.Runtime)) == 0;

    /// <summary>메서드 하나를 컴파일한다. Mono에서는 함수 주소를 달라고 하면 컴파일된다(<c>RuntimeHelpers.PrepareMethod</c>는 아무 일도 하지 않았다).</summary>
    private static bool Compile(MethodBase method)
    {
        try
        {
            method.MethodHandle.GetFunctionPointer();
            return true;
        }
        catch (Exception) { return false; }
    }

    private static void Run(string[] scopes, string label, double startedAt, bool allOnThread)
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        var total = Stopwatch.StartNew();
        int types = 0, methods = 0, failed = 0, skipped = 0;
        // 범위 ** 는 엔진·패키지 어셈블리까지 전부다(기본 라이브러리만 뺀다). 남은 첫 재생 비용이 게임 코드 밖인지 보려는 것이다.
        bool everything = Array.IndexOf(scopes, "**") >= 0;
        try
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string assemblyName = assembly.GetName().Name;
                if (everything)
                {
                    if (assemblyName == "mscorlib" || assemblyName == "netstandard" || assemblyName.StartsWith("System", StringComparison.Ordinal) ||
                        assemblyName.StartsWith("Mono.", StringComparison.Ordinal)) continue;
                }
                else if (!assemblyName.StartsWith("ProjectL", StringComparison.Ordinal) && assemblyName != "Framework" && assemblyName != "Assembly-CSharp") continue;
                if (assemblyName.StartsWith("ProjectL.Research", StringComparison.Ordinal)) continue;
                Type[] found;
                try { found = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException exception) { found = exception.Types; }
                foreach (Type type in found)
                {
                    if (type == null || type.ContainsGenericParameters || type.IsInterface) continue;
                    if (!InScope(scopes, type.FullName ?? type.Name)) continue;
                    MethodBase[] members;
                    try
                    {
                        MethodInfo[] declared = type.GetMethods(All);
                        ConstructorInfo[] constructors = type.GetConstructors(All);
                        members = new MethodBase[declared.Length + constructors.Length];
                        declared.CopyTo(members, 0);
                        constructors.CopyTo(members, declared.Length);
                    }
                    catch (Exception) { failed++; continue; }
                    if (!allOnThread && type.TypeInitializer != null)
                    {
                        MainQueue.Enqueue(members);      // 정적 생성자가 있는 형식은 메인 스레드 몫
                        continue;
                    }
                    types++;
                    foreach (MethodBase method in members)
                    {
                        if (!Compilable(method)) { skipped++; continue; }
                        if (Compile(method)) methods++; else failed++;
                    }
                }
            }
            _thread = $"thread done scope={label} types={types} methods={methods} skipped={skipped} failed={failed} seconds={total.Elapsed.TotalSeconds:F2} startedAt={startedAt:F2}s";
        }
        catch (Exception exception)
        {
            _thread = $"thread failed scope={label} methods={methods} error={exception.GetType().Name}: {exception.Message}";
        }
        _listed = true;
    }

    /// <summary>메인 스레드 몫을 이번 프레임 예산만큼 컴파일한다. 형식 하나는 끊지 않는다(정적 생성자가 그 안에서 돈다).</summary>
    internal static void TickMain(double budgetMilliseconds, double now)
    {
        if (_mainDoneAt >= 0) return;
        long start = Stopwatch.GetTimestamp();
        long budget = (long)(budgetMilliseconds * Stopwatch.Frequency / 1000.0);
        bool worked = false;
        while (Stopwatch.GetTimestamp() - start < budget && MainQueue.TryDequeue(out MethodBase[] members))
        {
            worked = true;
            _mainTypes++;
            foreach (MethodBase method in members)
            {
                if (!Compilable(method)) continue;
                if (Compile(method)) _mainMethods++; else _mainFailed++;
            }
        }
        long spent = Stopwatch.GetTimestamp() - start;
        if (worked)
        {
            _mainFrames++;
            _mainTicks += spent;
            if (spent > _mainLongestTicks) _mainLongestTicks = spent;
        }
        bool done = _listed && MainQueue.IsEmpty;
        if (done) _mainDoneAt = now;
        double ms = 1000.0 / Stopwatch.Frequency;
        _main = $"main {(done ? "done" : "running")} types={_mainTypes} methods={_mainMethods} failed={_mainFailed} frames={_mainFrames} " +
            $"totalMs={_mainTicks * ms:F0} longestFrameMs={_mainLongestTicks * ms:F1}" + (done ? $" doneAt={now:F2}s" : "");
    }
}
