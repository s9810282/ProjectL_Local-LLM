using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Profiling;
using UnityEngine.Scripting;
using UnityEngine.UI;

/// <summary>
/// 연구 저장소 전용 에디터 자동 재생(병렬 설계 §24). 사람 손 없이 "재생 → 메인 배치 → 전투 진입 → 턴 종료 → 첫 적 턴"을 돌려
/// 계획 AI 콘솔 줄을 파일로 남긴다. <c>LLMTest/core_port/autoplay/trigger.txt</c>가 생기면 시작하고,
/// 끝나면 <c>done.txt</c>에 결과를 쓴다. 트리거 파일이 없으면 아무 일도 하지 않는다.
/// 하는 것: 덱의 메인 카드를 집어(덱 UI와 같은 입구) 화면의 여러 점을 차례로 눌러 놓일 때까지 시도, 전투 진입 키(G), 턴 종료 키(Space),
/// 확인창의 예 버튼, 행동 패널의 턴 종료 버튼. 도는 동안 실제 마우스·키보드를 쓰면 섞인다.
/// </summary>
[InitializeOnLoad]
internal static class ResearchAutoPlay
{
    private const string ActiveKey = "ResearchAutoPlay.Active";
    private const string StartKey = "ResearchAutoPlay.Start";
    private const string PlanSeenKey = "ResearchAutoPlay.PlanSeen";
    private const string TickKey = "ResearchAutoPlay.Tick";
    private const string SubmitKey = "ResearchAutoPlay.Submit";
    private const string ProblemKey = "ResearchAutoPlay.Problems";
    private const string PlacedKey = "ResearchAutoPlay.MainPlaced";
    private const string PointKey = "ResearchAutoPlay.Point";
    private const string NoteKey = "ResearchAutoPlay.Note";
    private const string StepKey = "ResearchAutoPlay.PlaceStep";
    private const string UnitsKey = "ResearchAutoPlay.Units";
    private const string GcKey = "ResearchAutoPlay.Gc";
    private const string GcOnKey = "ResearchAutoPlay.GcOn";
    private const string GcCountKey = "ResearchAutoPlay.GcCount";
    private const double TimeoutSeconds = 300;
    private const double AfterPlanSeconds = 12;
    private const double TickSeconds = 0.5;

    // 메인을 놓아 볼 화면 점(가로·세로 비율). 가운데에서 바깥으로.
    private static readonly float[] Ratios = { 0.5f, 0.4f, 0.6f, 0.3f, 0.7f, 0.2f, 0.8f };

    private static readonly string Folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../LLMTest/core_port/autoplay"));
    private static string TriggerPath => Path.Combine(Folder, "trigger.txt");
    private static string LinesPath => Path.Combine(Folder, "lines.txt");
    private static string DonePath => Path.Combine(Folder, "done.txt");

    private static double _nextPoll;
    private static bool _releaseKeys;
    private static bool _releaseMouse;
    private static double _releaseMouseAt;
    private static Vector2 _mousePoint;

    static ResearchAutoPlay()
    {
        EditorApplication.update += Update;
        Application.logMessageReceived += OnLog;
    }

    private static bool Active
    {
        get => SessionState.GetBool(ActiveKey, false);
        set => SessionState.SetBool(ActiveKey, value);
    }

    private static void Update()
    {
        double now = EditorApplication.timeSinceStartup;
        if (!Active)
        {
            if (now < _nextPoll) return;
            _nextPoll = now + 1.0;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || !File.Exists(TriggerPath)) return;
            // 트리거 내용 = GC 실험 방식(§25): "auto"(기본), "manual:<ms>"(자동 GC 끄고 프레임마다 그 시간만큼 나눠 수집), "disabled"(계획 동안 끔).
            string gc = File.ReadAllText(TriggerPath).Trim();
            if (gc == "build")
            {
                // 개발 빌드 만들기(§26). 끝날 때까지 에디터가 멈춘다. 결과는 done.txt.
                File.Delete(TriggerPath);
                if (File.Exists(DonePath)) File.Delete(DonePath);
                string result;
                try { result = ResearchBuild.Run(); }
                catch (Exception exception) { result = "status=build-exception " + exception.GetType().Name + ": " + exception.Message; }
                File.WriteAllText(DonePath, result);
                return;
            }
            SessionState.SetString(GcKey, gc.StartsWith("manual") || gc == "disabled" ? gc : "auto");
            SessionState.SetBool(GcOnKey, false);
            File.Delete(TriggerPath);
            if (File.Exists(DonePath)) File.Delete(DonePath);
            File.WriteAllText(LinesPath, "codeOptimization=" + CompilationPipeline.codeOptimization + Environment.NewLine);
            Active = true;
            SessionState.SetFloat(StartKey, (float)now);
            SessionState.SetFloat(PlanSeenKey, -1f);
            SessionState.SetInt(TickKey, 0);
            SessionState.SetInt(ProblemKey, 0);
            SessionState.SetBool(PlacedKey, false);
            SessionState.SetInt(PointKey, 0);
            SessionState.SetString(NoteKey, "");
            SessionState.SetInt(StepKey, 0);
            SessionState.SetInt(NoteKey + ".Count", 0);
            EditorApplication.isPlaying = true;
            return;
        }

        double start = SessionState.GetFloat(StartKey, (float)now);
        float planSeen = SessionState.GetFloat(PlanSeenKey, -1f);
        if (now - start > TimeoutSeconds) { Finish("timeout"); return; }
        if (planSeen >= 0 && now - planSeen > AfterPlanSeconds) { Finish("ok"); return; }
        if (!EditorApplication.isPlaying) return;

        TickGc();
        if (_releaseKeys && Keyboard.current != null)
        {
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            _releaseKeys = false;
        }
        if (_releaseMouse && Mouse.current != null && now >= _releaseMouseAt)
        {
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = _mousePoint });
            _releaseMouse = false;
        }
        if (now < _nextPoll) return;
        _nextPoll = now + TickSeconds;
        if (planSeen >= 0) return;            // 첫 계획이 나온 뒤에는 더 누르지 않는다(다음 플레이어 턴을 넘기지 않게).
        FocusGameView();
        if (ClickConfirmYes()) { SessionState.SetFloat(SubmitKey, (float)now); return; }
        int tick = SessionState.GetInt(TickKey, 0);
        SessionState.SetInt(TickKey, tick + 1);
        if (tick < 6) return;                 // 씬이 뜨고 진입 연출이 돌 시간을 준다.
        if (!SessionState.GetBool(PlacedKey, false))
        {
            _nextPoll = now + 0.2;            // 칸을 차례로 눌러 보는 동안은 빠르게.
            PlaceMain();
            return;
        }
        if (Keyboard.current != null)
        {
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(tick % 2 == 0 ? Key.G : Key.Space));
            _releaseKeys = true;
        }
        if (tick % 2 == 1 && ClickField("CombatActionPanel", "endTurnButton")) SessionState.SetFloat(SubmitKey, (float)now);
    }

    /// <summary>
    /// 메인 캐릭터를 필드에 놓는다(메인 배치는 전투 진입의 필수 조건). 덱 UI가 쓰는 배치 입구로 메인 카드를 집고,
    /// 필드 칸을 하나씩 눌러 본다(포인터 옮김 → 누름·뗌 → 기다림). 놓였는지는 필드 위에 보이는 유닛 수가 늘었는지로 본다
    /// (메인은 놓인 뒤에도 덱 카드로 다시 집을 수 있어 "집을 수 있나"로는 알 수 없다).
    /// </summary>
    private static void PlaceMain()
    {
        const BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        var behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        var deck = behaviours.FirstOrDefault(b => b != null && b.GetType().Name == "DeckCardList" && b.isActiveAndEnabled);
        var field = behaviours.FirstOrDefault(b => b != null && b.GetType().Name == "FieldView" && b.isActiveAndEnabled);
        if (deck == null || field == null) { Note($"wait deck={(deck != null)} field={(field != null)}"); return; }
        var shown = deck.GetType().GetField("_shown", Any)?.GetValue(deck) as Array;
        object placement = deck.GetType().GetProperty("Placement", Any)?.GetValue(deck);
        object mapper = field.GetType().GetProperty("Mapper", Any)?.GetValue(field);
        if (shown == null || shown.Length == 0 || placement == null || mapper == null)
        {
            Note($"wait shown={(shown != null)} placement={(placement != null)} mapper={(mapper != null)}");
            return;
        }
        object main = shown.GetValue(0);
        int units = behaviours.Count(b => b != null && b.GetType().Name == "FieldUnitView" && b.isActiveAndEnabled);
        int step = SessionState.GetInt(StepKey, 0);
        int point = SessionState.GetInt(PointKey, 0);
        var camera = Camera.main != null ? Camera.main : Camera.allCameras.OrderByDescending(c => c.depth).FirstOrDefault();
        if (Mouse.current == null || camera == null) return;
        int columns = (int)mapper.GetType().GetProperty("ColumnCount", Any).GetValue(mapper);
        int rows = (int)mapper.GetType().GetProperty("RowCount", Any).GetValue(mapper);

        if (step == 0)
        {
            // 집기. 진입 연출 등으로 아직 집을 수 없으면 기다린다.
            if (!(bool)Member(placement, "CanSelect", main)) { Note("wait canSelect=False"); return; }
            if (point >= columns * rows) { Finish("main-not-placed"); return; }
            SessionState.SetInt(UnitsKey, units);
            Member(placement, "Select", main);
            SessionState.SetInt(StepKey, 1);
            return;
        }
        if (step == 1 || step == 2)
        {
            // 아래 줄(행 0)과 위 줄을 번갈아, 줄 안에서는 가운데 열부터.
            int rowStep = point / columns, column = point % columns;
            int row = rowStep % 2 == 0 ? rowStep / 2 : rows - 1 - rowStep / 2;
            int x = columns / 2 + (column % 2 == 0 ? column / 2 : -(column / 2 + 1));
            if (x < 0 || x >= columns) x = column;
            var toWorld = mapper.GetType().GetMethod("CellSurfaceToWorld", Any);
            object cell = Activator.CreateInstance(toWorld.GetParameters()[0].ParameterType, x, row);
            // 에디터 갱신 시점의 카메라 픽셀 크기는 게임 화면과 다를 수 있어, 화면 비율(뷰포트)에 게임 화면 크기를 곱한다.
            Vector3 viewport = camera.WorldToViewportPoint((Vector3)toWorld.Invoke(mapper, new[] { cell }));
            Vector2 view = Handles.GetMainGameViewSize();
            _mousePoint = new Vector2(viewport.x * view.x, viewport.y * view.y);
            if (step == 1)
            {
                // 먼저 포인터만 그 칸으로 옮긴다. 게임이 그 칸을 포인터 칸으로 잡은 뒤에 눌러야 놓인다.
                InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = _mousePoint });
                SessionState.SetInt(StepKey, 2);
                return;
            }
            if (point < 8) Append($"placeTry cell={x},{row} screen={_mousePoint.x:F0},{_mousePoint.y:F0} units={units}");
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = _mousePoint }.WithButton(MouseButton.Left));
            _releaseMouse = true;
            _releaseMouseAt = EditorApplication.timeSinceStartup + 0.08;   // 누른 채 몇 프레임 둔 뒤 뗀다.
            SessionState.SetInt(PointKey, point + 1);
            SessionState.SetInt(StepKey, 3);
            return;
        }
        // 3~6: 명령 처리와 표시를 기다린 뒤 필드 유닛 수를 본다.
        if (units > SessionState.GetInt(UnitsKey, units))
        {
            SessionState.SetBool(PlacedKey, true);
            Append($"mainPlaced attempt={point} units={units}");
            return;
        }
        SessionState.SetInt(StepKey, step >= 6 ? 0 : step + 1);
    }

    /// <summary>인터페이스 멤버를 이름으로 부른다(명시 구현도 닿게 인터페이스에서 찾는다).</summary>
    private static object Member(object target, string name, params object[] args)
    {
        foreach (var contract in target.GetType().GetInterfaces())
        {
            var method = contract.GetMethod(name);
            if (method != null) return method.Invoke(target, args);
            var property = contract.GetProperty(name);
            if (property != null) return property.GetValue(target);
        }
        throw new MissingMemberException(target.GetType().Name, name);
    }

    /// <summary>
    /// GC 실험(§25). 메인이 놓인 뒤부터 끝날 때까지 건다. manual이면 자동 수집을 끄고 에디터 갱신마다 정한 시간만큼만 나눠 수집한다.
    /// 설정이 실제로 먹었는지(에디터가 받아 주는지)와 힙 크기를 줄로 남긴다.
    /// </summary>
    private static void TickGc()
    {
        string gc = SessionState.GetString(GcKey, "auto");
        if (!SessionState.GetBool(PlacedKey, false)) return;
        if (!SessionState.GetBool(GcOnKey, false))
        {
            SessionState.SetBool(GcOnKey, true);
            SessionState.SetInt(GcCountKey, GC.CollectionCount(0));
            string actual = "unchanged";
            if (gc != "auto")
            {
                try
                {
                    GarbageCollector.GCMode = gc == "disabled" ? GarbageCollector.Mode.Disabled : GarbageCollector.Mode.Manual;
                    actual = GarbageCollector.GCMode.ToString();
                }
                catch (Exception exception) { actual = "error:" + exception.GetType().Name; }
            }
            Append($"gc requested={gc} actual={actual} incremental={GarbageCollector.isIncremental} heapMB={Profiler.GetMonoHeapSizeLong() >> 20} usedMB={Profiler.GetMonoUsedSizeLong() >> 20}");
        }
        if (gc.StartsWith("manual") && GarbageCollector.GCMode == GarbageCollector.Mode.Manual)
        {
            double ms = double.TryParse(gc.Substring(gc.IndexOf(':') + 1), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var parsed) ? parsed : 1.0;
            GarbageCollector.CollectIncremental((ulong)(ms * 1000000));
        }
    }

    private static void RestoreGc()
    {
        if (!SessionState.GetBool(GcOnKey, false)) return;
        SessionState.SetBool(GcOnKey, false);
        Append($"gcEnd heapMB={Profiler.GetMonoHeapSizeLong() >> 20} usedMB={Profiler.GetMonoUsedSizeLong() >> 20} collections={GC.CollectionCount(0) - SessionState.GetInt(GcCountKey, 0)}");
        try { GarbageCollector.GCMode = GarbageCollector.Mode.Enabled; } catch (Exception) { }
    }

    private static void Finish(string status)
    {
        RestoreGc();
        Active = false;
        string lines = File.Exists(LinesPath) ? File.ReadAllText(LinesPath) : "";
        File.WriteAllText(DonePath, "status=" + status + " problems=" + SessionState.GetInt(ProblemKey, 0) + Environment.NewLine + lines);
        if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
    }

    /// <summary>진단 줄. 같은 내용이 이어지면 한 번만 적는다.</summary>
    private static void Note(string line)
    {
        if (SessionState.GetString(NoteKey, "") == line) return;
        SessionState.SetString(NoteKey, line);
        int count = SessionState.GetInt(NoteKey + ".Count", 0);
        SessionState.SetInt(NoteKey + ".Count", count + 1);
        if (count < 12) Append("note " + line);
    }

    private static void Append(string line)
    {
        try { File.AppendAllText(LinesPath, line + Environment.NewLine); }
        catch (IOException) { }
    }

    private static void OnLog(string condition, string stackTrace, LogType type)
    {
        if (!Active || condition == null) return;
        bool plan = condition.StartsWith("[턴 계획 AI]", StringComparison.Ordinal);
        bool problem = type == LogType.Exception || type == LogType.Error || type == LogType.Assert;
        if (!plan && !problem) return;
        if (problem)
        {
            // 오류는 처음 다섯 줄만 적는다(같은 예외가 프레임마다 쏟아질 수 있다).
            int problems = SessionState.GetInt(ProblemKey, 0);
            SessionState.SetInt(ProblemKey, problems + 1);
            if (problems >= 5) return;
        }
        Append((problem ? "[" + type + "] " : "") + condition.Split('\n')[0]);
        if (plan && condition.Contains("기다린 프레임") && SessionState.GetFloat(PlanSeenKey, -1f) < 0)
        {
            double seen = EditorApplication.timeSinceStartup;
            SessionState.SetFloat(PlanSeenKey, (float)seen);
            // 마지막으로 턴 종료를 누른(또는 그 확인창에서 예를 누른) 때부터 첫 결정이 준비될 때까지. 누름 간격만큼의 오차가 있다.
            Append($"atDecision heapMB={Profiler.GetMonoHeapSizeLong() >> 20} usedMB={Profiler.GetMonoUsedSizeLong() >> 20} collections={GC.CollectionCount(0) - SessionState.GetInt(GcCountKey, 0)}");
            Append("lastSubmitToDecision=" + (seen - SessionState.GetFloat(SubmitKey, (float)seen)).ToString("F1") + "s");
        }
    }

    private static void FocusGameView()
    {
        var type = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        if (type != null) EditorWindow.FocusWindowIfItsOpen(type);
    }

    /// <summary>열린 확인창(공용 안내창·확인창)의 예 버튼을 누른다. 눌렀으면 true.</summary>
    private static bool ClickConfirmYes() => ClickField("TextNoticeUI", "yesButton") | ClickField("ConfirmUI", "yesButton");

    /// <summary>씬에서 이름이 typeName인 컴포넌트의 private Button 필드를 찾아 왼쪽 클릭을 보낸다.</summary>
    private static bool ClickField(string typeName, string fieldName)
    {
        bool clicked = false;
        foreach (var behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (behaviour == null || behaviour.GetType().Name != typeName) continue;
            var field = behaviour.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (!(field?.GetValue(behaviour) is Button button) || button == null) continue;
            if (!button.isActiveAndEnabled || !button.IsInteractable() || EventSystem.current == null) continue;
            var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerClickHandler);
            clicked = true;
        }
        return clicked;
    }
}
