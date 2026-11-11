using System;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;

namespace ProjectL.Tool.Editor
{
    /// <summary>Editor 플레이의 바이너리 기록 수명입니다. Hierarchy 읽기와 CSV 변환은 종료 뒤에만 수행합니다.</summary>
    [InitializeOnLoad]
    internal static class ProfilerContinuousCapture
    {
        // C2: 사용자 승인하에 Unity에서 기록 OFF/ON을 직접 대조합니다.
        internal static readonly bool Available = true;
        private const string Prefix = "ProjectL.ProfilerCapture.";
        private const string AutoMenu = "ProjectL/Profiler/전체 플레이 자동 기록";
        private const string ActiveKey = Prefix + "Active";
        private const string SettingsKey = Prefix + "Settings";
        private const string StartingKey = Prefix + "Starting";
        private const string LastKey = Prefix + "Last";
        private const string PendingKey = Prefix + "PendingExport";
        // 이 게임 프레임 수마다 다음 구간 파일로 바꿉니다. 구간 하나를 불러오는 메모리는 약 4GB입니다(ProfilerCaptureMemory).
        private const int SegmentFrames = 5000;
        private static ProfilerCaptureRecord _record;
        private static SavedSettings _settings;
        private static bool _starting;
        private static bool _stopping;
        private static double _nextPersist;

        [Serializable]
        private sealed class SavedSettings
        {
            public string logFile;
            public bool driverEnabled;
            public bool engineEnabled;
            public bool profileEditor;
            public bool cpuEnabled;
        }

        static ProfilerContinuousCapture()
        {
            if (!Available)
            {
                SessionState.SetBool(Prefix + "Auto", false);
                SessionState.EraseString(PendingKey);
                SessionState.EraseString(ActiveKey);
                SessionState.EraseString(SettingsKey);
                SessionState.SetBool(StartingKey, false);
                return;
            }
            string active = SessionState.GetString(ActiveKey, string.Empty);
            if (!string.IsNullOrEmpty(active))
            {
                _record = JsonUtility.FromJson<ProfilerCaptureRecord>(active);
                _settings = JsonUtility.FromJson<SavedSettings>(SessionState.GetString(SettingsKey, string.Empty));
                _starting = SessionState.GetBool(StartingKey, false);
            }
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.update += Update;
            EditorApplication.quitting += OnQuitting;
            AssemblyReloadEvents.beforeAssemblyReload += BeforeReload;
            Application.logMessageReceived += OnLog;
            EditorApplication.delayCall += Recover;
        }

        internal static bool IsRecording => _record != null;
        internal static string CaptureRoot => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs", "ProfilerCapture");

        [MenuItem(AutoMenu)]
        private static void ToggleAuto()
        {
            if (!Available) return;
            bool enabled = !SessionState.GetBool(Prefix + "Auto", false);
            SessionState.SetBool(Prefix + "Auto", enabled);
            Debug.Log("[Profiler] 전체 플레이 자동 기록 " + (enabled ? "켜짐. 다음 Play부터 종료까지 raw로 저장하고 CSV로 변환합니다." : "꺼짐."));
        }

        [MenuItem(AutoMenu, true)]
        private static bool ValidateAuto()
        {
            Menu.SetChecked(AutoMenu, SessionState.GetBool(Prefix + "Auto", false));
            return Available && !EditorApplication.isPlayingOrWillChangePlaymode && !IsRecording;
        }

        [MenuItem("ProjectL/Profiler/지금부터 연속 기록 시작")]
        private static void StartNow() => Start(false);

        [MenuItem("ProjectL/Profiler/지금부터 연속 기록 시작", true)]
        private static bool ValidateStart() => Available && EditorApplication.isPlaying && !IsRecording;

        [MenuItem("ProjectL/Profiler/연속 기록 중지 및 내보내기")]
        private static void StopNow()
        {
            Stop("사용자 중지", false, true);
            if (EditorApplication.isPlaying)
                Debug.Log("[Profiler] 원본을 저장했습니다. Play를 종료하면 CSV 변환을 시작합니다.");
        }

        [MenuItem("ProjectL/Profiler/연속 기록 중지 및 내보내기", true)]
        private static bool ValidateStop() => IsRecording;

        [MenuItem("ProjectL/Profiler/마지막 연속 기록 다시 내보내기")]
        private static void ExportLast()
        {
            string json = SessionState.GetString(LastKey, string.Empty);
            if (string.IsNullOrEmpty(json))
                throw new InvalidOperationException("[Profiler] 이 Editor 세션의 연속 기록이 없습니다. 저장된 raw 선택 메뉴를 사용하세요.");
            var record = JsonUtility.FromJson<ProfilerCaptureRecord>(json);
            try
            {
                ProfilerCaptureImport.Export(record.rawPath, record, true);
            }
            catch (Exception error)
            {
                record.exportStatus = "Failed: " + error.Message;
                throw;
            }
            finally
            {
                SessionState.SetString(LastKey, record.Json());
                record.Save();
            }
        }

        [MenuItem("ProjectL/Profiler/연속 기록 원본 폴더 열기")]
        private static void OpenFolder()
        {
            Directory.CreateDirectory(CaptureRoot);
            EditorUtility.RevealInFinder(CaptureRoot);
        }

        private static void Start(bool beforePlay)
        {
            if (!Available) throw new InvalidOperationException("[Profiler] 크래시 조사 중으로 연속 기록 기능을 비활성화했습니다.");
            if (IsRecording)
                throw new InvalidOperationException("[Profiler] 연속 기록이 이미 진행 중입니다.");
            if (!string.IsNullOrEmpty(SessionState.GetString(PendingKey, string.Empty)))
                throw new InvalidOperationException("[Profiler] 이전 기록의 CSV 변환이 대기 중입니다. Play를 종료해 변환을 마친 뒤 다시 시작하세요.");
            CloseInterruptedLog();
            if (Profiler.enableBinaryLog)
                throw new InvalidOperationException("[Profiler] 다른 바이너리 기록이 켜져 있습니다. 먼저 그 기록을 종료하세요.");
            if (!beforePlay && !EditorApplication.isPlaying)
                throw new InvalidOperationException("[Profiler] 중간 시작은 Play 중에만 가능합니다. 전체 기록은 자동 기록 메뉴를 켜세요.");

            string folder = Path.Combine(CaptureRoot, DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            _settings = new SavedSettings
            {
                logFile = Profiler.logFile,
                driverEnabled = ProfilerDriver.enabled,
                engineEnabled = Profiler.enabled,
                profileEditor = ProfilerDriver.profileEditor,
                cpuEnabled = ProfilerDriver.IsAreaEnabled(ProfilerArea.CPU)
            };
            _record = new ProfilerCaptureRecord
            {
                rawPath = Path.Combine(folder, SegmentName(0)),
                startedUtc = DateTime.UtcNow.ToString("O"),
                status = "Recording",
                startedBeforePlay = beforePlay,
                firstGameFrame = beforePlay ? -1 : Time.frameCount,
                lastGameFrame = beforePlay ? -1 : Time.frameCount,
                segmentFrames = SegmentFrames,
                unityVersion = Application.unityVersion
            };
            _record.segments.Add(new ProfilerCaptureSegment { path = _record.rawPath, firstGameFrame = _record.firstGameFrame });
            _starting = beforePlay;
            try
            {
                // 설정을 바꾸기 전에 리로드를 넘길 복구 정보를 저장합니다.
                Persist();
                _record.Save();
                ProfilerDriver.enabled = false;
                Profiler.enabled = false;
                ProfilerDriver.profileEditor = false;
                ProfilerDriver.SetAreaEnabled(ProfilerArea.CPU, true);
                Profiler.logFile = _record.rawPath;
                Profiler.enableBinaryLog = true;
                ProfilerDriver.enabled = true;
                Profiler.enabled = true;
                Debug.Log("[Profiler] 연속 기록 시작: " + _record.rawPath + " (게임 프레임 "
                    + SegmentFrames.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + "개마다 다음 구간 파일로 나눕니다)");
            }
            catch
            {
                Stop("기록 시작 실패", true, false, restoreRecording: true);
                throw;
            }
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            try
            {
                switch (state)
                {
                    case PlayModeStateChange.ExitingEditMode:
                        if (SessionState.GetBool(Prefix + "Auto", false)) Start(true);
                        break;
                    case PlayModeStateChange.EnteredPlayMode:
                        if (_record == null) break;
                        _starting = false;
                        ObserveFrame();
                        Persist();
                        break;
                    case PlayModeStateChange.ExitingPlayMode:
                        Stop("Play 종료", false, true);
                        break;
                    case PlayModeStateChange.EnteredEditMode:
                        if (IsRecording) Stop("Play 진입 취소 또는 중단", true, false);
                        EditorApplication.delayCall += ExportPending;
                        break;
                }
            }
            catch (Exception error)
            {
                Debug.LogException(error);
            }
        }

        private static void Update()
        {
            if (_record == null || _stopping || _starting || !EditorApplication.isPlaying) return;
            ObserveFrame();
            try
            {
                if (NeedsNextSegment()) BeginNextSegment();
            }
            catch (Exception error)
            {
                Debug.LogException(error);
            }
            if (_record == null || EditorApplication.timeSinceStartup < _nextPersist) return;
            _nextPersist = EditorApplication.timeSinceStartup + 1d;
            try
            {
                if (!OwnsLog() || !Profiler.enableBinaryLog || !Profiler.enabled)
                    Stop("기록 설정이 외부에서 변경되거나 중지됨: actual=" + Profiler.logFile
                        + ", expected=" + _record.ActivePath + ", binary=" + Profiler.enableBinaryLog
                        + ", enabled=" + Profiler.enabled, true, false);
                else
                    Persist();
            }
            catch (Exception error)
            {
                Debug.LogException(error);
            }
        }

        private static void ObserveFrame()
        {
            int frame = Time.frameCount;
            if (_record.firstGameFrame < 0) _record.firstGameFrame = frame;
            _record.lastGameFrame = Math.Max(_record.lastGameFrame, frame);
        }

        private static string SegmentName(int index) =>
            "capture-" + index.ToString("000", System.Globalization.CultureInfo.InvariantCulture) + ".raw";

        // 이 코드 전에 시작해 리로드를 넘어온 기록은 구간 목록이 비어 있어 나누지 않습니다.
        // 매 Editor 프레임 불리므로 할당 없는 프레임 수 비교를 먼저 합니다. 경로 비교(문자열 할당)는 바꿀 때만 해서 기록 중 GC 측정에 섞이지 않게 합니다.
        // 경로 소유가 바뀐 경우는 1초 검사가 기록을 멈추므로 여기서는 바꾸지 않습니다.
        private static bool NeedsNextSegment()
        {
            if (_record.segments == null || _record.segments.Count == 0) return false;
            ProfilerCaptureSegment current = _record.segments[_record.segments.Count - 1];
            int start = current.firstGameFrame >= 0 ? current.firstGameFrame : _record.firstGameFrame;
            if (start < 0 || Time.frameCount - start < SegmentFrames) return false;
            return Profiler.enableBinaryLog && OwnsLog();
        }

        private static void BeginNextSegment()
        {
            string path = Path.Combine(Path.GetDirectoryName(_record.rawPath), SegmentName(_record.segments.Count));
            try
            {
                // Unity 문서: binary log가 켜진 채 새 경로를 주면 그 파일에 이어 저장합니다. 기록은 끄지 않습니다.
                // 새 파일이 혼자 열리는지·경계 프레임이 빠지는지는 Unity 확인(계획 Ub) 대상입니다.
                Profiler.logFile = path;
            }
            catch
            {
                // 경로가 새 파일로 바뀐 뒤 실패했으면 그 파일을 우리 구간으로 더해야 Stop이 기록을 닫습니다.
                bool movedToNew = false;
                try { movedToNew = IsSameLogPath(Profiler.logFile, path); }
                catch (Exception) { }
                if (movedToNew)
                    _record.segments.Add(new ProfilerCaptureSegment { path = path, firstGameFrame = Time.frameCount });
                Stop("구간 파일 전환 실패: " + path, true, false);
                throw;
            }
            _record.segments.Add(new ProfilerCaptureSegment { path = path, firstGameFrame = Time.frameCount });
            Persist();
            _record.Save();
            Debug.Log("[Profiler] 연속 기록 구간 " + (_record.segments.Count - 1) + " 시작(게임 프레임 " + Time.frameCount + "): " + path);
        }

        private static bool OwnsLog() => _record != null &&
            IsSameLogPath(Profiler.logFile, _record.ActivePath);

        // Unity 네이티브 경로와 Windows Path.Combine 결과는 구분자 표기가 다를 수 있습니다.
        internal static bool IsSameLogPath(string left, string right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right)) return false;
            return string.Equals(Path.GetFullPath(left).Replace('\\', '/'),
                Path.GetFullPath(right).Replace('\\', '/'), StringComparison.OrdinalIgnoreCase);
        }

        internal static void CloseInterruptedLog()
        {
            if (!Available) return;
            if (IsRecording || EditorApplication.isPlaying || !Profiler.enableBinaryLog) return;
            string json = SessionState.GetString(LastKey, string.Empty);
            if (string.IsNullOrEmpty(json)) return;
            var record = JsonUtility.FromJson<ProfilerCaptureRecord>(json);
            if (record == null || record.status != "Interrupted" || !IsSameLogPath(Profiler.logFile, record.ActivePath)) return;

            // 이전 버전이 경로 표기 차이로 놓친 우리 원본만 닫습니다. 파일을 다시 열어 덮어쓰지 않습니다.
            ProfilerDriver.enabled = false;
            Profiler.enabled = false;
            Profiler.logFile = string.Empty;
            Profiler.enableBinaryLog = false;
            Debug.Log("[Profiler] 중단 처리 뒤 남아 있던 원본 기록을 닫았습니다: " + record.ActivePath);
        }

        private static void Stop(string reason, bool interrupted, bool export, bool restoreRecording = false)
        {
            if (_record == null || _stopping) return;
            _stopping = true;
            var record = _record;
            var settings = _settings;
            if (EditorApplication.isPlaying && !_starting) ObserveFrame();
            Exception failure = null;
            try
            {
                if (OwnsLog())
                {
                    ProfilerDriver.enabled = false;
                    Profiler.enabled = false;
                    // logFile을 비워 네이티브 파일 핸들을 닫은 뒤에만 로드합니다.
                    Profiler.logFile = string.Empty;
                    Profiler.enableBinaryLog = false;
                }
                else if (!restoreRecording)
                {
                    interrupted = true;
                    reason += " (기록 경로 소유권 변경)";
                    settings = null;
                }
            }
            catch (Exception error)
            {
                failure = error;
                interrupted = true;
            }
            finally
            {
                try
                {
                    if (settings != null)
                    {
                        Profiler.logFile = settings.logFile ?? string.Empty;
                        ProfilerDriver.profileEditor = settings.profileEditor;
                        ProfilerDriver.SetAreaEnabled(ProfilerArea.CPU, settings.cpuEnabled);
                        ProfilerDriver.enabled = restoreRecording && settings.driverEnabled;
                        Profiler.enabled = restoreRecording && settings.engineEnabled;
                    }
                }
                catch (Exception error)
                {
                    failure = failure == null ? error : new AggregateException(failure, error);
                    interrupted = true;
                }
                _record = null;
                _settings = null;
                _starting = false;
                _stopping = false;
                SessionState.EraseString(ActiveKey);
                SessionState.EraseString(SettingsKey);
                SessionState.SetBool(StartingKey, false);
            }
            record.stoppedUtc = DateTime.UtcNow.ToString("O");
            record.status = interrupted ? "Interrupted" : "Stopped";
            record.stopReason = reason;
            SessionState.SetString(LastKey, record.Json());
            record.Save();
            if (export && failure == null)
            {
                SessionState.SetString(PendingKey, record.Json());
                if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
                    EditorApplication.delayCall += ExportPending;
            }
            Debug.Log("[Profiler] 연속 기록 " + record.status + ": " + record.rawPath
                + (record.IsSegmented ? " 외 구간 파일 " + (record.segments.Count - 1) + "개" : string.Empty));
            if (failure != null) throw failure;
        }

        private static void Persist()
        {
            SessionState.SetString(ActiveKey, _record.Json());
            SessionState.SetString(SettingsKey, JsonUtility.ToJson(_settings));
            SessionState.SetBool(StartingKey, _starting);
        }

        private static void Recover()
        {
            try
            {
                if (IsRecording)
                {
                    if (!EditorApplication.isPlayingOrWillChangePlaymode)
                        Stop("재생 밖에서 남은 기록 정리", true, false);
                    else if (EditorApplication.isPlaying)
                    {
                        _starting = false;
                        ObserveFrame();
                        Persist();
                    }
                }
                ExportPending();
            }
            catch (Exception error) { Debug.LogException(error); }
        }

        private static void BeforeReload()
        {
            if (_starting && EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Persist();
                return;
            }
            Stop("스크립트 리로드", true, false);
        }

        private static void OnQuitting() => Stop("Editor 종료", true, false);

        private static void OnLog(string message, string stack, LogType type)
        {
            if (_record != null && message.IndexOf("Skipping profile frame", StringComparison.OrdinalIgnoreCase) >= 0)
                ++_record.droppedFrameWarnings;
        }

        private static void ExportPending()
        {
            if (IsRecording || EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;
            string json = SessionState.GetString(PendingKey, string.Empty);
            if (string.IsNullOrEmpty(json)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += ExportPending;
                return;
            }
            SessionState.EraseString(PendingKey);
            var record = JsonUtility.FromJson<ProfilerCaptureRecord>(json);
            try
            {
                // 자동 변환은 묻지 않습니다. 메모리가 모자라면 원본만 남기고 안내합니다.
                ProfilerCaptureImport.Export(record.rawPath, record, false);
            }
            catch (Exception error)
            {
                record.exportStatus = "Failed: " + error.Message;
                Debug.LogException(error);
            }
            finally
            {
                SessionState.SetString(LastKey, record.Json());
                record.Save();
            }
        }
    }
}
