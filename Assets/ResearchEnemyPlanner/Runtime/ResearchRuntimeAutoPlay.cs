using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Profiling;
using UnityEngine.Scripting;
using UnityEngine.UI;
using Unity.Profiling;
using Unity.Profiling.LowLevel;
using Unity.Profiling.LowLevel.Unsafe;

/// <summary>
/// 연구 저장소 전용: 빌드(플레이어)에서 사람 손 없이 "메인 배치 → 전투 진입 → 턴 종료 → 첫 적 턴"을 돌리고 끝낸다(병렬 설계 §26).
/// 실행 인자 <c>-researchAutoPlay</c>가 있을 때만 켜진다. 없으면 아무 일도 하지 않는다.
/// <c>-researchOut 폴더</c>에 <c>lines.txt</c>(진행·계획 AI 줄)와 <c>done.txt</c>(끝 상태)를 쓴다.
/// <c>-researchGc auto|manual:밀리초|disabled</c>로 계획 동안의 GC 방식을 고른다(에디터는 GC 방식 변경을 받지 않아 빌드에서만 된다).
/// 에디터용 <c>ResearchAutoPlay</c>와 같은 순서·같은 입구를 쓴다.
/// </summary>
public sealed class ResearchRuntimeAutoPlay : MonoBehaviour
{
    private const double TimeoutSeconds = 300;
    private const double AfterPlanSeconds = 12;
    private const double RescanAfterDecisionSeconds = 6;
    private const double StalledSeconds = 70;
    private const float SpikeMs = 8.3f;

    private string _folder;
    private string _gc = "auto";
    private double _start, _nextPoll, _planSeen = -1, _submit, _releaseMouseAt;
    private int _tick, _point, _step, _unitsBefore, _problems, _notes, _collectionsAtStart;
    private bool _placed, _gcOn, _releaseKeys, _releaseMouse, _finished;
    private string _lastNote = "";
    private Vector2 _mousePoint;
    // 프레임 길이(우리 쪽에서 따로 잰다): 턴 종료 뒤 첫 결정까지, 그리고 결정 뒤 평소 구간.
    private readonly FrameWindow _waitFrames = new FrameWindow(), _afterFrames = new FrameWindow();
    // 긴 프레임이 나왔을 때 그 프레임에서 시간이 든 엔진 구간을 적기 위한 기록기(§27). 이름 → 기록기.
    private readonly Dictionary<string, ProfilerRecorder> _recorders = new Dictionary<string, ProfilerRecorder>();
    private readonly List<ProfilerMarker> _named = new List<ProfilerMarker>();
    private int _spikes, _logs, _lastCollections;
    // 적 턴을 몇 번 볼지(-researchRounds). 같은 순간이 두 번째·세 번째에도 튀는지 보려는 것이다(§27).
    private int _rounds = 1, _round;
    private double _rescanAt = -1;
    private bool _selfWork;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        string[] args = Environment.GetCommandLineArgs();
        if (!args.Contains("-researchAutoPlay")) return;
        var host = new GameObject("ResearchRuntimeAutoPlay");
        DontDestroyOnLoad(host);
        var play = host.AddComponent<ResearchRuntimeAutoPlay>();
        play._folder = Arg(args, "-researchOut") ?? Application.persistentDataPath;
        string gc = Arg(args, "-researchGc") ?? "auto";
        play._gc = gc.StartsWith("manual") || gc == "disabled" ? gc : "auto";
        if (int.TryParse(Arg(args, "-researchRounds"), out int rounds) && rounds > 1) play._rounds = Math.Min(rounds, 6);
    }

    private static string Arg(string[] args, string name)
    {
        int at = Array.IndexOf(args, name);
        return at >= 0 && at + 1 < args.Length ? args[at + 1] : null;
    }

    private string LinesPath => Path.Combine(_folder, "lines.txt");
    private string DonePath => Path.Combine(_folder, "done.txt");

    private void Awake()
    {
        Application.logMessageReceived += OnLog;
    }

    private void Start()
    {
        Directory.CreateDirectory(_folder);
        if (File.Exists(DonePath)) File.Delete(DonePath);
        // 창이 뒤에 있어도 입력·진행이 멈추지 않게, 프레임이 화면 주사율에 묶이지 않게(에디터 측정과 같은 조건).
        Application.runInBackground = true;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
        File.WriteAllText(LinesPath, $"player development={Debug.isDebugBuild} gc={_gc} screen={Screen.width}x{Screen.height}" + Environment.NewLine);
        _start = Time.realtimeSinceStartupAsDouble;
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= OnLog;
    }

    private void Update()
    {
        if (_finished) return;
        double now = Time.realtimeSinceStartupAsDouble;
        float frameMs = Time.unscaledDeltaTime * 1000f;
        // 앞 프레임에 이 도구가 구간 이름을 다시 훑었으면(자기 일) 그 프레임은 세지 않는다.
        bool self = _selfWork;
        _selfWork = false;
        if (!self)
        {
            if (Waiting) _waitFrames.Add(frameMs);
            else if (_planSeen >= 0) _afterFrames.Add(frameMs);
        }
        if (_placed) NoteSpike(now, frameMs, self);
        if (_placed && !self) TickWatches();

        if (now - _start > TimeoutSeconds) { Finish("timeout"); return; }
        if (_round >= 1 && _round < _rounds && now - _planSeen > StalledSeconds) { Finish("ok-stalled"); return; }
        if (_round >= _rounds && now - _planSeen > AfterPlanSeconds) { Finish("ok"); return; }
        if (_rescanAt > 0 && now >= _rescanAt) { _rescanAt = -1; StartRecorders(); }

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
        _nextPoll = now + 0.5;
        if (_round >= _rounds) return;         // 볼 만큼 본 뒤에는 더 누르지 않는다.
        if (ClickConfirmYes()) { _submit = now; return; }
        _tick++;
        if (_tick < 8) return;                 // 씬이 뜨고 진입 연출이 돌 시간을 준다.
        if (!_placed)
        {
            _nextPoll = now + 0.2;             // 칸을 차례로 눌러 보는 동안은 빠르게.
            PlaceMain();
            return;
        }
        if (Keyboard.current != null)
        {
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(_tick % 2 == 0 ? Key.G : Key.Space));
            _releaseKeys = true;
        }
        if (_tick % 2 == 1 && ClickField("CombatActionPanel", "endTurnButton")) _submit = now;
    }

    /// <summary>메인 캐릭터를 필드에 놓는다. 놓였는지는 필드 위에 보이는 유닛 수가 늘었는지로 본다.</summary>
    private void PlaceMain()
    {
        const BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
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
        var camera = Camera.main != null ? Camera.main : Camera.allCameras.OrderByDescending(c => c.depth).FirstOrDefault();
        if (Mouse.current == null || camera == null) { Note($"wait mouse={(Mouse.current != null)} camera={(camera != null)}"); return; }
        int columns = (int)mapper.GetType().GetProperty("ColumnCount", Any).GetValue(mapper);
        int rows = (int)mapper.GetType().GetProperty("RowCount", Any).GetValue(mapper);

        if (_step == 0)
        {
            if (!(bool)Member(placement, "CanSelect", main)) { Note("wait canSelect=False"); return; }
            if (_point >= columns * rows) { Finish("main-not-placed"); return; }
            _unitsBefore = units;
            Member(placement, "Select", main);
            _step = 1;
            return;
        }
        if (_step == 1 || _step == 2)
        {
            // 아래 줄(행 0)과 위 줄을 번갈아, 줄 안에서는 가운데 열부터.
            int rowStep = _point / columns, column = _point % columns;
            int row = rowStep % 2 == 0 ? rowStep / 2 : rows - 1 - rowStep / 2;
            int x = columns / 2 + (column % 2 == 0 ? column / 2 : -(column / 2 + 1));
            if (x < 0 || x >= columns) x = column;
            var toWorld = mapper.GetType().GetMethod("CellSurfaceToWorld", Any);
            object cell = Activator.CreateInstance(toWorld.GetParameters()[0].ParameterType, x, row);
            Vector3 screen = camera.WorldToScreenPoint((Vector3)toWorld.Invoke(mapper, new[] { cell }));
            _mousePoint = new Vector2(screen.x, screen.y);
            if (_step == 1)
            {
                // 먼저 포인터만 그 칸으로 옮긴다. 게임이 그 칸을 포인터 칸으로 잡은 뒤에 눌러야 놓인다.
                InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = _mousePoint });
                _step = 2;
                return;
            }
            if (_point < 8) Append($"placeTry cell={x},{row} screen={_mousePoint.x:F0},{_mousePoint.y:F0} units={units}");
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = _mousePoint }.WithButton(MouseButton.Left));
            _releaseMouse = true;
            _releaseMouseAt = Time.realtimeSinceStartupAsDouble + 0.08;
            _point++;
            _step = 3;
            return;
        }
        if (units > _unitsBefore)
        {
            _placed = true;
            StartRecorders();
            Append($"mainPlaced attempt={_point} units={units}");
            Append("atMainPlaced " + ResearchJitPrewarm.Status + $" now={Time.realtimeSinceStartupAsDouble:F2}s");
            return;
        }
        _step = _step >= 6 ? 0 : _step + 1;
    }

    /// <summary>GC 실험. 메인이 놓인 뒤부터 끝날 때까지 건다.</summary>
    private void TickGc()
    {
        if (!_placed) return;
        if (!_gcOn)
        {
            _gcOn = true;
            _collectionsAtStart = GC.CollectionCount(0);
            string actual = "unchanged";
            if (_gc != "auto")
            {
                try
                {
                    GarbageCollector.GCMode = _gc == "disabled" ? GarbageCollector.Mode.Disabled : GarbageCollector.Mode.Manual;
                    actual = GarbageCollector.GCMode.ToString();
                }
                catch (Exception exception) { actual = "error:" + exception.GetType().Name; }
            }
            Append($"gc requested={_gc} actual={actual} incremental={GarbageCollector.isIncremental} {Heap()}");
        }
        if (_gc.StartsWith("manual") && GarbageCollector.GCMode == GarbageCollector.Mode.Manual)
        {
            double ms = double.TryParse(_gc.Substring(_gc.IndexOf(':') + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : 1.0;
            GarbageCollector.CollectIncremental((ulong)(ms * 1000000));
        }
    }

    /// <summary>턴 종료를 낸 뒤 아직 그 턴의 결정이 안 나온 동안.</summary>
    private bool Waiting => _submit > 0 && _submit > _planSeen;

    /// <summary>한 구간의 "처음 지나간 프레임 값"과 "그 뒤 값들"을 따로 모은다. 처음만 긴지(첫 사용 비용) 매번 긴지 가르려는 것이다.</summary>
    private sealed class Watch
    {
        internal string Name;
        internal ProfilerRecorder Recorder;
        internal bool Late;
        internal double First, LaterMax, LaterSum;
        internal int Count;
    }

    private readonly List<Watch> _watches = new List<Watch>();
    private bool _watchLate;

    private static bool Watched(string name) =>
        name.StartsWith("Core.Calc.Enemy", StringComparison.Ordinal) || name.StartsWith("Core.Combat.", StringComparison.Ordinal)
        || name.StartsWith("Combat.Result.", StringComparison.Ordinal) || name.StartsWith("Combat.Units.", StringComparison.Ordinal)
        || (name.StartsWith("Combat.Action.Playback.", StringComparison.Ordinal)
            && (name.EndsWith(".Handle", StringComparison.Ordinal) || name.EndsWith(".Dispatch", StringComparison.Ordinal) || name.EndsWith(".Begin", StringComparison.Ordinal)));

    private void TickWatches()
    {
        foreach (var watch in _watches)
        {
            if (!watch.Recorder.Valid) continue;
            double ms = watch.Recorder.LastValue / 1000000.0;
            if (ms <= 0) continue;
            if (watch.Count++ == 0) { watch.First = ms; continue; }
            watch.LaterSum += ms;
            if (ms > watch.LaterMax) watch.LaterMax = ms;
        }
    }

    /// <summary>
    /// 지금 등록된 엔진·게임 구간 가운데 볼 만한 것에 기록기를 붙인다(시간 단위만, 메인 스레드에서 쓴 시간만).
    /// 게임 구간(Combat.·Core.)과 스크립트 구간을 먼저 붙이고 남는 자리에 엔진 구간을 붙인다.
    /// 구간 이름은 처음 지나갈 때 생기므로 여러 번 부른다. 훑는 일 자체가 그 프레임을 길게 하므로 그 프레임은 "자기 일"로 표시한다.
    /// </summary>
    private void StartRecorders()
    {
        _selfWork = true;
        if (_named.Count == 0)
            foreach (string name in FirstUseNames()) _named.Add(new ProfilerMarker(name));   // 처음 지나가는 프레임에도 잡히게 이름을 미리 만든다.
        var handles = new List<ProfilerRecorderHandle>();
        ProfilerRecorderHandle.GetAvailable(handles);
        int timeMarkers = 0;
        for (int pass = 0; pass < 2; pass++)
            foreach (var handle in handles)
            {
                if (_recorders.Count >= 4000) break;
                var description = ProfilerRecorderHandle.GetDescription(handle);
                if (description.UnitType != ProfilerMarkerDataUnit.TimeNanoseconds) continue;
                if (pass == 0) timeMarkers++;
                if (_recorders.ContainsKey(description.Name)) continue;
                bool game = description.Name.StartsWith("Combat.", StringComparison.Ordinal) || description.Name.StartsWith("Core.", StringComparison.Ordinal)
                    || description.Category.Name == "Scripts";
                if (pass == 0 ? !game : !Wanted(description.Name)) continue;
                var recorder = new ProfilerRecorder(handle, 1,
                    ProfilerRecorderOptions.Default | ProfilerRecorderOptions.CollectOnlyOnCurrentThread | ProfilerRecorderOptions.StartImmediately);
                _recorders.Add(description.Name, recorder);
                if (Watched(description.Name)) _watches.Add(new Watch { Name = description.Name, Recorder = recorder, Late = _watchLate });
            }
        _watchLate = true;      // 이 뒤에 붙는 구간은 처음 지나간 프레임을 놓쳤을 수 있다.
        Append($"recorders={_recorders.Count} timeMarkers={timeMarkers} jitNames={string.Join(",", _recorders.Keys.Where(k => k.IndexOf("JIT", StringComparison.OrdinalIgnoreCase) >= 0).Take(6))}");
    }

    /// <summary>게임 쪽 구간 이름 가운데 처음 쓰일 때 생기는 것들(같은 이름이면 같은 구간이 된다).</summary>
    private static IEnumerable<string> FirstUseNames()
    {
        string[] top =
        {
            "Combat.Result.PresentEvent", "Combat.Result.Finalize", "Core.Combat.Command", "Core.Combat.Advance", "Core.Combat.Calculate",
            "Core.Combat.Publish", "Core.Combat.AssembleInput", "Combat.Units.CreateModel", "Combat.Units.CreateHUD", "Combat.Units.ApplyHUD",
            "Combat.Units.CreateClipChannel", "Combat.Units.CreateHealthLabel", "Combat.Units.CreateOutsidePreparedCapacity",
            "Combat.Units.PreparedCapacityMiss", "Combat.Units.PrepareSelected", "Combat.Units.PreparationTick",
            "Combat.View.Field.Outline.Refresh", "Combat.View.Field.Fill.Apply", "Combat.View.Field.Path.Apply"
        };
        foreach (string name in top) yield return name;
        string[] owners = { "Enemy", "Player", "System", "Unknown" };
        string[] actions = { "Attack", "Step", "Turn", "Effects", "Character.Moved", "Character.Damaged", "Character.Died", "UnitRun", "Accent" };
        string[] phases = { "Dispatch", "Handle", "Begin", "Tick", "Hit", "End", "Prepare", "Interpolate", "MotionTick", "ProjectileBegin", "ProjectileTick", "ProjectileEnd" };
        foreach (string owner in owners)
            foreach (string action in actions)
                foreach (string phase in phases)
                    yield return "Combat.Action.Playback." + owner + "." + action + "." + phase;
    }

    private static readonly string[] WantedParts =
    {
        "PlayerLoop", "ScriptRunBehaviour", "ScriptRunDelayed", "BehaviourUpdate", "GC.", "JIT", "Shader", "GpuProgram", "GPUProgram", "Instantiate", "Loading.",
        "AwakeFromLoad", "Canvas", "UGUI", "Layout", "TMP", "Font", "Animator", "Animation.", "Director", "Particle", "Gfx.", "Render", "Camera.",
        "Culling", "Shadows", "Audio", "FMOD", "Physics", "Destroy", "Awake", "OnEnable", "Start", "SetActive", "Mesh.", "Texture",
        "Material", "Decal", "Cinemachine", "VFX", "Batch", "UnloadUnused", "GarbageCollectAssets", "EarlyUpdate", "PreUpdate", "FixedUpdate",
        "PreLateUpdate", "PostLateUpdate", "Update.", "Input", "EventSystem", "Playable", "Transform", "SkinnedMesh", "Skinning"
    };

    private static bool Wanted(string name)
    {
        foreach (var part in WantedParts)
            if (name.IndexOf(part, StringComparison.Ordinal) >= 0) return true;
        return false;
    }

    /// <summary>
    /// 긴 프레임 하나: 몇 번째 적 턴인지, 구간(배치·기다림·결정 뒤), 그 구간 기준 시각, 길이, 그 프레임의 GC 수집 수, 메인 스레드에서 시간이 많이 든 구간 위쪽.
    /// 기록기 값은 앞 프레임 것이라 프레임 길이와 같은 프레임을 가리킨다.
    /// </summary>
    private void NoteSpike(double now, float frameMs, bool self)
    {
        int collections = GC.CollectionCount(0);
        int collected = collections - _lastCollections;
        _lastCollections = collections;
        if (self || frameMs <= SpikeMs || _spikes >= 120) return;
        _spikes++;
        var top = new List<KeyValuePair<string, double>>();
        foreach (var pair in _recorders)
        {
            if (!pair.Value.Valid) continue;
            double ms = pair.Value.LastValue / 1000000.0;
            if (ms >= 0.3) top.Add(new KeyValuePair<string, double>(pair.Key, ms));
        }
        top.Sort((a, b) => b.Value.CompareTo(a.Value));
        string where = Waiting ? "wait" : _planSeen >= 0 ? "after" : "deploy";
        double t = Waiting ? now - _submit : _planSeen >= 0 ? now - _planSeen : now - _start;
        Append(string.Format(CultureInfo.InvariantCulture, "spike round={0} {1} t={2:F2}s ms={3:F1} gc={4} | {5}", Waiting ? _round + 1 : _round, where, t, frameMs, collected,
            string.Join("; ", top.Take(22).Select(p => p.Key + "=" + p.Value.ToString("F1", CultureInfo.InvariantCulture)))));
    }

    private static string Heap() => $"heapMB={Profiler.GetMonoHeapSizeLong() >> 20} usedMB={Profiler.GetMonoUsedSizeLong() >> 20}";

    private void Finish(string status)
    {
        if (_finished) return;
        _finished = true;
        Append($"gcEnd {Heap()} collections={GC.CollectionCount(0) - _collectionsAtStart}");
        if (_rounds > 1) Append("waitFramesAll " + _waitFrames);
        Append("afterFrames " + _afterFrames);
        Append(ResearchJitPrewarm.Status);
        foreach (var watch in _watches.Where(w => w.Count > 0 && (w.First >= 1.0 || w.LaterMax >= 1.0)).OrderByDescending(w => Math.Max(w.First, w.LaterMax)))
            Append(string.Format(CultureInfo.InvariantCulture, "watch {0} first={1:F1}ms{2} laterFrames={3} laterMax={4:F1}ms laterMean={5:F2}ms", watch.Name, watch.First,
                watch.Late ? "(late)" : "", watch.Count - 1, watch.LaterMax, watch.Count > 1 ? watch.LaterSum / (watch.Count - 1) : 0));
        foreach (var recorder in _recorders.Values) recorder.Dispose();
        try { GarbageCollector.GCMode = GarbageCollector.Mode.Enabled; } catch (Exception) { }
        string lines = File.Exists(LinesPath) ? File.ReadAllText(LinesPath) : "";
        File.WriteAllText(DonePath, $"status={status} problems={_problems}" + Environment.NewLine + lines);
        Application.Quit();
    }

    private void OnLog(string condition, string stackTrace, LogType type)
    {
        if (_finished || condition == null || _folder == null) return;
        bool plan = condition.StartsWith("[턴 계획 AI]", StringComparison.Ordinal);
        bool problem = type == LogType.Exception || type == LogType.Error || type == LogType.Assert;
        // 결정 뒤 구간의 게임 로그는 시각과 함께 적어, 긴 프레임이 어떤 일과 겹치는지 본다.
        if (!plan && !problem && _planSeen >= 0 && _logs++ < 30)
            Append(string.Format(CultureInfo.InvariantCulture, "log t={0:F2}s {1}: {2}", Time.realtimeSinceStartupAsDouble - _planSeen, type, condition.Split('\n')[0]));
        if (!plan && !problem) return;
        if (problem && _problems++ >= 5) return;
        Append((problem ? "[" + type + "] " : "") + condition.Split('\n')[0]);
        if (plan && condition.Contains("기다린 프레임") && _round < _rounds)
        {
            _planSeen = Time.realtimeSinceStartupAsDouble;
            _round++;
            // 그 사이 새로 생긴 구간 이름은 연출이 돈 뒤에 붙인다(결정 프레임에 훑으면 그 프레임이 길어진다).
            _rescanAt = _planSeen + RescanAfterDecisionSeconds;
            Append($"round={_round} atDecision {Heap()} collections={GC.CollectionCount(0) - _collectionsAtStart}");
            Append("lastSubmitToDecision=" + (_planSeen - _submit).ToString("F1", CultureInfo.InvariantCulture) + "s");
            if (_round == 1) Append("waitFrames " + _waitFrames);
        }
    }

    private void Note(string line)
    {
        if (_lastNote == line) return;
        _lastNote = line;
        if (_notes++ < 12) Append("note " + line);
    }

    private void Append(string line)
    {
        try { File.AppendAllText(LinesPath, line + Environment.NewLine); }
        catch (IOException) { }
    }

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

    private static bool ClickConfirmYes() => ClickField("TextNoticeUI", "yesButton") | ClickField("ConfirmUI", "yesButton");

    private static bool ClickField(string typeName, string fieldName)
    {
        bool clicked = false;
        foreach (var behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
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

    /// <summary>프레임 길이 모음: 수, 중간값, 가장 긴 값, 8.3·16.7·33ms를 넘은 수.</summary>
    private sealed class FrameWindow
    {
        private readonly System.Collections.Generic.List<float> _ms = new System.Collections.Generic.List<float>(4096);
        internal void Add(float ms) => _ms.Add(ms);
        public override string ToString()
        {
            if (_ms.Count == 0) return "count=0";
            var sorted = _ms.OrderBy(v => v).ToArray();
            return string.Format(CultureInfo.InvariantCulture, "count={0} median={1:F1}ms p99={2:F1}ms max={3:F1}ms over8.3={4} over16.7={5} over33={6}",
                sorted.Length, sorted[sorted.Length / 2], sorted[(int)(sorted.Length * 0.99)], sorted[sorted.Length - 1],
                sorted.Count(v => v > 8.3f), sorted.Count(v => v > 16.7f), sorted.Count(v => v > 33.3f));
        }
    }
}
