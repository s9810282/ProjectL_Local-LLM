using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    /// <summary>
    /// Profiler 창에 기록된 프레임을 글과 CSV로 내보내는 도구입니다(2026-11-03).
    /// Claude Code는 Profiler 창도 <c>.data</c> 캡처도 직접 읽지 못해서, 읽을 수 있는 파일로 바꿉니다.
    /// </summary>
    /// <remarks>
    /// 쓰는 법: Profiler 창에서 녹화하거나 <c>.data</c>를 불러온 뒤 메뉴 <c>ProjectL/Profiler/기록 내보내기</c>를 누릅니다.
    /// 녹화를 멈춘 뒤 누르면 내보내는 동안 버퍼에서 밀려나는 프레임이 없습니다.
    /// 결과는 프로젝트 루트 <c>Logs/ProfilerExport/&lt;시각-식별자&gt;/</c>(git이 무시하는 폴더)에 다섯 파일입니다.
    /// <list type="bullet">
    /// <item><c>summary.md</c>: 프레임 시간·카운터 통계, 자기 시간 상위 마커, GC 할당 상위 경로, 느린 프레임의 계층.</item>
    /// <item><c>frames.csv</c>: 프레임마다 한 줄(시간·카운터·자기 시간이 가장 큰 마커).</item>
    /// <item><c>markers.csv</c>: 계층 경로마다 한 줄. <c>avg_*</c>는 내보낸 전체 프레임 수로 나눈 값입니다.</item>
    /// <item><c>frame_markers.csv</c>: 모든 프레임×경로의 시간·호출·할당입니다.</item>
    /// <item><c>actions.csv</c>: 프레임별 행동 마커와 분류입니다.</item>
    /// </list>
    /// 버퍼에 남은 프레임 전부를 메인 스레드만 읽습니다(<see cref="ProfilerFrameScan"/>).
    /// </remarks>
    internal static class ProfilerFrameExporter
    {
        private const string Title = "Profiler 기록 내보내기";
        private const string LogPrefix = "[ProfilerFrameExporter] ";
        private const string OutputFolderName = "ProfilerExport";
        private const int ProgressStep = 10;

        // 요약에 넣는 양입니다. 나머지는 CSV에 모두 있습니다.
        private const int WorstFrameCount = 5;
        private const int TopSelfMarkerCount = 25;
        private const int TopGcPathCount = 15;
        // 느린 프레임 계층: 이 시간보다 짧은 줄과 깊이·형제 수를 넘는 줄은 "나머지" 한 줄로 묶습니다.
        private const float TreeMinMs = 0.1f;
        private const int TreeMaxDepth = 14;
        private const int TreeMaxChildren = 8;

        [MenuItem("ProjectL/Profiler/기록 내보내기 (Claude 분석용)")]
        private static void Export() => ExportCurrent();

        /// <summary>현재 Profiler 버퍼 전체를 변환합니다. 연속 기록 원본은 <see cref="Session"/>으로 구간마다 읽습니다.</summary>
        internal static string ExportCurrent(ProfilerCaptureRecord capture = null)
        {
            if (ProfilerContinuousCapture.IsRecording)
                throw new InvalidOperationException("[Profiler] 연속 기록 중에는 '연속 기록 중지 및 내보내기'를 사용하세요.");
            using var session = new Session(capture);
            return session.ReadLoaded(0, 1, null) ? session.Commit() : null;
        }

        /// <summary>
        /// 불러온 범위를 하나 이상 차례로 읽어 한 출력 폴더로 확정합니다. 범위를 불러오고 비우는 일은 부른 쪽이 합니다.
        /// 연속 기록 구간 원본은 한 번에 하나만 메모리에 올려 이어 붙입니다(2026-11-10 긴 기록 변환 크래시 대응).
        /// </summary>
        internal sealed class Session : IDisposable
        {
            private readonly ProfilerCaptureRecord _capture;
            private readonly ProfilerFrameCsvOutput _output;
            private readonly ProfilerFrameScan _scan;
            // 느린 프레임 계층입니다(출력 번호 → 계층 글). 앞 범위는 다음 범위를 불러오면 사라지므로 범위를 다 읽을 때마다 후보를 미리 적습니다.
            private readonly Dictionary<int, string> _trees = new Dictionary<int, string>();
            private readonly List<List<int>> _treeBuffers = new List<List<int>>();
            private readonly List<string> _missing = new List<string>();
            private int _ranges;
            private int _firstOut = -1;
            private int _lastOut = -1;
            // 바로 앞에 읽은 범위의 불러온 번호와 번호 이동입니다. 다음 범위의 번호가 이어지는지·겹치는지·다시 시작하는지 가릅니다.
            private int _prevFirstLocal;
            private int _prevLastLocal;
            private int _prevOffset;
            private int _gapFrames;
            private int _overlapFrames;
            private int _renumbered;
            private bool _recording;

            internal Session(ProfilerCaptureRecord capture)
            {
                _capture = capture;
                if (capture != null)
                {
                    ResetResult(capture);
                    capture.exportStatus = "Converting";
                }
                _output = new ProfilerFrameCsvOutput(ExportRoot);
                _scan = new ProfilerFrameScan(_output.Write);
            }

            /// <summary>앞 변환의 결과 칸을 비웁니다. 변환하지 않고 건너뛸 때도 부릅니다.</summary>
            internal static void ResetResult(ProfilerCaptureRecord capture)
            {
                capture.loadedFirstFrame = -1;
                capture.loadedLastFrame = -1;
                capture.exportedFrames = 0;
                capture.skippedFrames = 0;
                capture.actionPaths = 0;
                capture.segmentGapFrames = 0;
                capture.segmentOverlapFrames = 0;
                capture.renumberedSegments = 0;
                capture.missingSegments = 0;
                capture.exportFolder = string.Empty;
                if (capture.segments == null)
                    return;
                foreach (ProfilerCaptureSegment segment in capture.segments)
                {
                    segment.loadedFirstFrame = -1;
                    segment.loadedLastFrame = -1;
                    segment.outputOffset = 0;
                    segment.loadStatus = null;
                }
            }

            /// <summary>불러오지 못한 범위를 남깁니다. 하나라도 있으면 전체 기록으로 표시하지 않습니다.</summary>
            internal void NoteMissing(string reason) => _missing.Add(reason);

            /// <summary>지금 Profiler 버퍼의 전체 범위를 읽습니다. 취소하면 false입니다.</summary>
            /// <param name="segment">연속 기록 구간이면 불러온 번호와 번호 이동을 적습니다. 아니면 null입니다.</param>
            internal bool ReadLoaded(int rangeIndex, int rangeCount, ProfilerCaptureSegment segment)
            {
                int first = ProfilerDriver.firstFrameIndex;
                int last = ProfilerDriver.lastFrameIndex;
                if (first < 0 || last < first)
                {
                    if (segment != null) segment.loadStatus = "Empty";
                    NoteMissing("범위 " + rangeIndex + ": 불러온 프레임 없음");
                    return true;
                }
                if (ProfilerDriver.enabled && !_recording)
                {
                    _recording = true;
                    Debug.LogWarning(LogPrefix + "Profiler가 녹화 중입니다. 내보내는 동안 버퍼에서 밀려난 프레임은 건너뜁니다.");
                }

                // 출력 번호 = 불러온 번호 + offset. 앞 범위와 비교해 세 경우로 나눕니다.
                // 이어짐(사이 빈 번호는 셈) / 이어지는데 경계 프레임이 두 파일에 다 있음(겹친 프레임은 한 번만 읽음) / 0부터 다시 셈(앞 범위 끝 다음으로 옮김).
                int offset = 0;
                int start = first;
                if (_ranges > 0)
                {
                    if (first > _prevLastLocal)
                    {
                        offset = _prevOffset;
                        _gapFrames += first - _prevLastLocal - 1;
                    }
                    else if (first > _prevFirstLocal)
                    {
                        offset = _prevOffset;
                        start = _prevLastLocal + 1;
                        _overlapFrames += Math.Min(last, _prevLastLocal) - first + 1;
                    }
                    else
                    {
                        offset = _lastOut + 1 - first;
                        _renumbered++;
                    }
                }
                if (segment != null)
                {
                    segment.loadedFirstFrame = first;
                    segment.loadedLastFrame = last;
                    segment.outputOffset = offset;
                }

                int count = last - start + 1;
                try
                {
                    for (int frame = start; frame <= last; frame++)
                    {
                        if ((frame - start) % ProgressStep == 0)
                        {
                            string label = rangeCount > 1
                                ? $"구간 {rangeIndex + 1}/{rangeCount} · 프레임 {frame} / {last}"
                                : $"프레임 {frame} / {last}";
                            if (EditorUtility.DisplayCancelableProgressBar(Title, label, (rangeIndex + (float)(frame - start) / count) / rangeCount))
                            {
                                Debug.Log(LogPrefix + "내보내기를 취소했습니다. 이번 실행의 임시 파일을 정리합니다.");
                                if (_capture != null) _capture.exportStatus = "Cancelled";
                                if (segment != null) segment.loadStatus = "Cancelled";
                                return false;
                            }
                        }
                        _scan.Read(frame, frame + offset);
                    }
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }

                if (segment != null) segment.loadStatus = "Loaded";
                if (start <= last)
                {
                    CaptureSlowTrees(start + offset, last + offset, offset);
                    if (_firstOut < 0) _firstOut = start + offset;
                    _lastOut = Math.Max(_lastOut, last + offset);
                }
                _prevFirstLocal = first;
                _prevLastLocal = last;
                _prevOffset = offset;
                _ranges++;
                return true;
            }

            // 요약의 느린 프레임 선택(PlayerLoop 기준·CPU 기준 상위 개수)과 같은 순서로 후보를 고릅니다.
            // 최종 상위 프레임은 그 프레임이 들어 있던 범위를 다 읽은 순간에도 상위였으므로 여기서 빠지지 않습니다.
            private void CaptureSlowTrees(int outFirst, int outLast, int offset)
            {
                IEnumerable<ProfilerFrameScan.Row> candidates = _scan.Frames.Where(r => r.PlayerLoopMs.HasValue)
                    .OrderByDescending(r => r.PlayerLoopMs.Value).Take(WorstFrameCount)
                    .Concat(_scan.Frames.OrderByDescending(r => r.CpuMs).Take(WorstFrameCount));
                foreach (ProfilerFrameScan.Row row in candidates)
                {
                    if (row.Frame < outFirst || row.Frame > outLast || _trees.ContainsKey(row.Frame))
                        continue;
                    using (HierarchyFrameDataView view = ProfilerFrameScan.Open(row.Frame - offset))
                    {
                        if (view == null)
                            continue;
                        var sb = new StringBuilder();
                        WriteTree(view, view.GetRootItemID(), 0, sb, _treeBuffers);
                        _trees.Add(row.Frame, sb.ToString());
                    }
                }
            }

            /// <summary>읽은 범위를 다섯 파일로 확정합니다. 쓸 프레임이 없으면 null입니다.</summary>
            internal string Commit()
            {
                if (_ranges == 0)
                {
                    if (_capture != null) _capture.exportStatus = "NoFrames";
                    EditorUtility.DisplayDialog(Title, "Profiler에 기록된 프레임이 없습니다.\nProfiler 창에서 녹화하거나 .data 파일을 불러온 뒤 다시 누르세요.", "확인");
                    return null;
                }
                if (_scan.Frames.Count == 0)
                {
                    EditorUtility.DisplayDialog(Title, "메인 스레드 자료가 있는 프레임이 없습니다.", "확인");
                    if (_capture != null) _capture.exportStatus = "NoReadableFrames";
                    return null;
                }

                int actions = _scan.Paths.Count(t => ProfilerFrameCsvOutput.IsAction(t.Name));
                if (_capture != null)
                {
                    _capture.loadedFirstFrame = _firstOut;
                    _capture.loadedLastFrame = _lastOut;
                    _capture.exportedFrames = _scan.Frames.Count;
                    _capture.skippedFrames = _scan.Skipped;
                    _capture.actionPaths = actions;
                    _capture.segmentGapFrames = _gapFrames;
                    _capture.segmentOverlapFrames = _overlapFrames;
                    _capture.renumberedSegments = _renumbered;
                    _capture.missingSegments = _missing.Count;
                    _capture.exportFolder = _output.Destination;
                    _capture.exportStatus = _capture.status == "Interrupted" || _scan.Skipped > 0 || _capture.droppedFrameWarnings > 0
                        || _missing.Count > 0 || _gapFrames > 0
                        || _scan.Frames.Count < _capture.ObservedGameFrameSpan ? "Incomplete" : "Converted";
                }
                string folder = _output.Commit(FramesCsv(_scan), MarkersCsv(_scan),
                    Summary(_scan, _firstOut, _lastOut, _recording, _capture, _trees, RangeNote()), _capture?.Json());
                Debug.Log(LogPrefix + $"프레임 {_scan.Frames.Count}개를 내보냈습니다: {folder}");
                if (actions == 0)
                    Debug.LogWarning(LogPrefix + "행동 마커가 0건입니다. 배치·포효가 기록됐다고 판단하지 마세요. 범위와 CPU 기록 설정을 확인하세요.");
                if (_capture?.exportStatus == "Incomplete")
                    Debug.LogWarning(LogPrefix + "연속 기록이 중단됐거나 관측 구간보다 출력이 적습니다. capture.json을 확인하세요. 원본: " + _capture.rawPath);
                return folder;
            }

            // 범위가 하나뿐이고 빠진 것이 없으면 요약에 따로 적지 않습니다.
            private string RangeNote()
            {
                if (_ranges + _missing.Count <= 1)
                    return null;
                var sb = new StringBuilder();
                sb.Append("- 원본 구간 ").Append(_ranges + _missing.Count).Append("개 중 ").Append(_ranges)
                    .Append("개를 한 번에 하나씩 불러 이었습니다. 구간 사이 빈 번호 ").Append(_gapFrames)
                    .Append("개, 두 파일에 다 있어 한 번만 읽은 프레임 ").Append(_overlapFrames)
                    .Append("개, 번호를 앞 구간 뒤로 옮긴 구간 ").Append(_renumbered).AppendLine("개.");
                foreach (string missing in _missing)
                    sb.Append("- **읽지 못한 구간:** ").AppendLine(missing);
                return sb.ToString();
            }

            public void Dispose() => _output.Dispose();
        }

        [MenuItem("ProjectL/Profiler/내보낸 폴더 열기")]
        private static void OpenExportFolder()
        {
            Directory.CreateDirectory(ExportRoot);
            EditorUtility.RevealInFinder(ExportRoot);
        }

        private static string ExportRoot =>
            Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs", OutputFolderName);

        #region CSV

        private static string FramesCsv(ProfilerFrameScan scan)
        {
            var sb = new StringBuilder();
            sb.Append("frame,cpu_ms,gpu_ms,player_loop_ms,editor_loop_ms");
            foreach ((string Column, string Marker) counter in ProfilerFrameScan.Counters)
                sb.Append(',').Append(counter.Column);
            sb.AppendLine(",top_self_marker,top_self_ms");

            foreach (ProfilerFrameScan.Row row in scan.Frames)
            {
                sb.Append(row.Frame).Append(',').Append(CsvNumber(row.CpuMs)).Append(',')
                    .Append(row.GpuMs > 0f ? CsvNumber(row.GpuMs) : string.Empty).Append(',')
                    .Append(OptionalCsvNumber(row.PlayerLoopMs)).Append(',').Append(OptionalCsvNumber(row.EditorLoopMs));
                foreach (long? value in row.Counters)
                    sb.Append(',').Append(value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
                sb.Append(',').Append(Csv(row.TopSelfName ?? string.Empty)).Append(',').Append(CsvNumber(row.TopSelfMs)).AppendLine();
            }
            return sb.ToString();
        }

        private static string MarkersCsv(ProfilerFrameScan scan)
        {
            double frames = scan.Frames.Count;
            var sb = new StringBuilder();
            sb.AppendLine("path,depth,name,frames_present,avg_total_ms,max_total_ms,avg_self_ms,max_self_ms,avg_calls,avg_gc_bytes,max_total_frame,max_self_frame,max_gc_frame,max_gc_bytes,avg_present_total_ms,avg_present_self_ms,avg_present_calls,avg_present_gc_bytes,first_frame,last_frame");
            foreach (ProfilerFrameScan.Totals totals in scan.Paths.OrderByDescending(t => t.TotalMs))
            {
                sb.Append(Csv(totals.Key)).Append(',').Append(totals.Depth).Append(',').Append(Csv(totals.Name)).Append(',')
                    .Append(totals.Frames).Append(',')
                    .Append(CsvNumber(totals.TotalMs / frames)).Append(',').Append(CsvNumber(totals.MaxTotalMs)).Append(',')
                    .Append(CsvNumber(totals.SelfMs / frames)).Append(',').Append(CsvNumber(totals.MaxSelfMs)).Append(',')
                    .Append(CsvNumber(totals.Calls / frames)).Append(',').Append(CsvNumber(totals.GcBytes / frames)).Append(',')
                    .Append(totals.MaxTotalFrame).Append(',').Append(totals.MaxSelfFrame).Append(',').Append(totals.MaxGcFrame).Append(',')
                    .Append(CsvNumber(totals.MaxGcBytes)).Append(',').Append(CsvNumber(totals.TotalMs / totals.Frames)).Append(',')
                    .Append(CsvNumber(totals.SelfMs / totals.Frames)).Append(',').Append(CsvNumber(totals.Calls / totals.Frames)).Append(',')
                    .Append(CsvNumber(totals.GcBytes / totals.Frames)).Append(',').Append(totals.FirstFrame).Append(',').Append(totals.LastFrame).AppendLine();
            }
            return sb.ToString();
        }

        #endregion

        #region 요약

        /// <param name="trees">느린 프레임 계층(출력 번호 → 글)입니다. 없는 프레임은 버퍼에서 사라졌다고 적습니다.</param>
        /// <param name="rangeNote">구간을 이어 읽었을 때의 안내 줄입니다. 범위가 하나면 null입니다.</param>
        private static string Summary(ProfilerFrameScan scan, int first, int last, bool recording, ProfilerCaptureRecord capture,
            IReadOnlyDictionary<int, string> trees, string rangeNote)
        {
            IReadOnlyList<ProfilerFrameScan.Row> rows = scan.Frames;
            double frames = rows.Count;
            var sb = new StringBuilder();

            sb.AppendLine("# Profiler 기록 요약").AppendLine();
            if (capture == null)
                sb.AppendLine("- 출처: 현재 Profiler 버퍼. 전체 플레이 기록이 아니며 오래된 프레임은 이미 밀려났을 수 있습니다.");
            else
            {
                sb.Append("- 출처: 저장된 연속 기록/파일 `").Append(capture.rawPath).AppendLine("`");
                sb.Append("- 기록 시작/종료 UTC: ").Append(capture.startedUtc).Append(" / ").AppendLine(capture.stoppedUtc);
                sb.Append("- 기록 상태: ").Append(capture.status).Append(" / CSV 변환: ").AppendLine(capture.exportStatus);
                sb.Append("- 기록 종료 사유: ").AppendLine(capture.stopReason);
                sb.Append("- Play 진입 전 시작: ").AppendLine(capture.startedBeforePlay ? "예" : "아니오 또는 외부 파일");
                sb.Append("- 관측 game-frame span: ").Append(capture.ObservedGameFrameSpan).Append(" / 출력 프레임: ").Append(rows.Count)
                    .Append(" / Unity 프레임 누락 알림: ").AppendLine(capture.droppedFrameWarnings.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("- 관측 span은 Editor에서 확인한 두 game-frame 지점 사이입니다. 0이면 비교 자료가 없으며, 출력 수와 같아도 모든 네이티브 누락이 없다는 증명은 아닙니다.");
                if (capture.exportStatus == "Incomplete")
                    sb.AppendLine("- **불완전한 기록: 중단·스킵·Unity 누락 알림·관측 구간 대비 부족·구간 누락이나 빈 번호 중 하나가 있습니다. 전체 플레이 수치로 해석하지 마세요.**");
            }
            if (rangeNote != null)
                sb.Append(rangeNote);
            sb.Append("- 내보낸 시각: ").AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            sb.Append("- Unity: ").Append(Application.unityVersion).Append(", 재생 중: ").AppendLine(EditorApplication.isPlaying ? "예" : "아니오");
            sb.Append("- 프레임: ").Append(first).Append(" ~ ").Append(last).Append(" (내보냄 ").Append(rows.Count)
                .Append("개, 건너뜀 ").Append(scan.Skipped).Append("개").Append(recording ? ", 녹화 중에 내보냄" : string.Empty).AppendLine(")");
            sb.Append("- 스레드: ").Append(scan.ThreadName).AppendLine(" 하나. 보기: 같은 이름 합침, 에디터 전용 샘플 뺌");
            sb.AppendLine("- 에디터에서 재생하면 EditorLoop(에디터 자체)가 프레임 시간에 들어갑니다. 게임 코드는 PlayerLoop 아래를 봅니다.");
            sb.AppendLine("- 표의 \"평균/프레임\"은 내보낸 전체 프레임 수로 나눈 값입니다.").AppendLine();
            sb.AppendLine("- frame_markers.csv는 모든 경로를 보존합니다. actions.csv는 Combat.Action. 마커의 파생 표입니다.");
            sb.AppendLine("- avg_present_*는 관측 프레임 기준 평균입니다. 최대값 동률은 최초 프레임이며, total/self/GC 최대 프레임은 서로 다를 수 있습니다.");
            sb.AppendLine("- first/last는 관측 범위입니다. 행동의 시작/끝이 캡처 밖일 수 있으며 연속 관측을 하나의 행동 인스턴스로 해석하지 않습니다.").AppendLine();

            sb.AppendLine("## 프레임 시간 (ms)").AppendLine();
            sb.AppendLine("| 항목 | 평균 | 중앙값 | 95% | 최대 |");
            sb.AppendLine("|---|---|---|---|---|");
            StatsRow(sb, "프레임 CPU", rows.Select(r => (double)r.CpuMs), Number);
            StatsRow(sb, "PlayerLoop", rows.Where(r => r.PlayerLoopMs.HasValue).Select(r => (double)r.PlayerLoopMs.Value), Number);
            StatsRow(sb, "EditorLoop", rows.Where(r => r.EditorLoopMs.HasValue).Select(r => (double)r.EditorLoopMs.Value), Number);
            StatsRow(sb, "GPU", rows.Where(r => r.GpuMs > 0f).Select(r => (double)r.GpuMs), Number);
            sb.AppendLine();

            sb.AppendLine("## 카운터 (프레임마다)").AppendLine();
            sb.AppendLine("| 카운터 | 평균 | 중앙값 | 95% | 최대 |");
            sb.AppendLine("|---|---|---|---|---|");
            for (int i = 0; i < ProfilerFrameScan.Counters.Length; i++)
            {
                int index = i;
                (string column, string marker) = ProfilerFrameScan.Counters[i];
                Func<double, string> format = column.EndsWith("_bytes", StringComparison.Ordinal) ? Bytes : Number;
                StatsRow(sb, marker, rows.Where(r => r.Counters[index].HasValue).Select(r => (double)r.Counters[index].Value), format);
            }
            int gcFrames = rows.Count(r => r.Counters[0] > 0);
            sb.AppendLine().Append("GC 할당이 있었던 프레임: ").Append(gcFrames).Append(" / ").Append(rows.Count).AppendLine().AppendLine();

            sb.Append("## 자기 시간(Self) 상위 마커 (이름 기준, 상위 ").Append(TopSelfMarkerCount).AppendLine(")").AppendLine();
            sb.AppendLine("| 마커 | 평균 self ms/프레임 | 한 프레임 최대 self ms | 평균 호출/프레임 | 평균 GC/프레임 |");
            sb.AppendLine("|---|---|---|---|---|");
            foreach (ProfilerFrameScan.Totals totals in scan.Names.OrderByDescending(t => t.SelfMs).Take(TopSelfMarkerCount))
            {
                sb.Append("| ").Append(Cell(totals.Name)).Append(" | ").Append(Number(totals.SelfMs / frames)).Append(" | ")
                    .Append(Number(totals.MaxSelfMs)).Append(" | ").Append(Number(totals.Calls / frames)).Append(" | ")
                    .Append(Bytes(totals.GcBytes / frames)).AppendLine(" |");
            }
            sb.AppendLine();

            sb.Append("## GC 할당 상위 경로 (상위 ").Append(TopGcPathCount).AppendLine(")").AppendLine();
            Dictionary<string, double> directGc = DirectGc(scan);
            List<ProfilerFrameScan.Totals> gcPaths = scan.Paths.Where(t => directGc[t.Key] >= 1d)
                .OrderByDescending(t => directGc[t.Key]).Take(TopGcPathCount).ToList();
            if (gcPaths.Count == 0)
            {
                sb.AppendLine("GC 할당이 없습니다.");
            }
            else
            {
                sb.AppendLine("직접 할당(그 경로의 GC − 자식 경로 GC 합)이 큰 순서입니다.").AppendLine();
                sb.AppendLine("| 경로 | 직접 GC/프레임 | 전체 GC/프레임 | 나온 프레임 | 평균 호출/프레임 |");
                sb.AppendLine("|---|---|---|---|---|");
                foreach (ProfilerFrameScan.Totals totals in gcPaths)
                {
                    sb.Append("| ").Append(Cell(totals.Key)).Append(" | ").Append(Bytes(directGc[totals.Key] / frames))
                        .Append(" | ").Append(Bytes(totals.GcBytes / frames)).Append(" | ").Append(totals.Frames)
                        .Append(" | ").Append(Number(totals.Calls / frames)).AppendLine(" |");
                }
            }
            sb.AppendLine();

            AppendActions(sb, scan);
            bool byPlayerLoop = rows.All(r => r.PlayerLoopMs.HasValue);
            Func<ProfilerFrameScan.Row, float> slowness = byPlayerLoop ? r => r.PlayerLoopMs.Value : (Func<ProfilerFrameScan.Row, float>)(r => r.CpuMs);
            sb.Append("## 느린 프레임 ").Append(WorstFrameCount).Append("개 (기준: ").Append(byPlayerLoop ? "PlayerLoop 시간" : "프레임 CPU 시간").AppendLine(")").AppendLine();
            sb.Append("계층의 각 줄은 `이름  전체 ms (self ms, 호출 수, GC)`입니다. ").Append(Number(TreeMinMs))
                .Append(" ms보다 짧거나 형제 ").Append(TreeMaxChildren).AppendLine("개를 넘는 줄은 \"나머지\"로 묶었습니다.").AppendLine();
            foreach (ProfilerFrameScan.Row row in rows.OrderByDescending(slowness).Take(WorstFrameCount))
            {
                sb.Append("### 프레임 ").Append(row.Frame).Append(" — ").Append(Number(slowness(row))).Append(" ms")
                    .Append(" (CPU ").Append(Number(row.CpuMs)).Append(" ms, GC ").Append(Bytes(row.Counters[0] ?? 0)).AppendLine(")").AppendLine();
                sb.AppendLine("```");
                if (trees.TryGetValue(row.Frame, out string tree))
                    sb.Append(tree);
                else
                    sb.AppendLine("(내보내는 동안 버퍼에서 사라졌습니다)");
                sb.AppendLine("```").AppendLine();
            }
            return sb.ToString();
        }

        private static void AppendActions(StringBuilder sb, ProfilerFrameScan scan)
        {
            sb.AppendLine("## 행동 마커별 최대 처리 프레임 (경로별)").AppendLine();
            var actions = scan.Paths.Where(t => ProfilerFrameCsvOutput.IsAction(t.Name)).OrderByDescending(t => t.MaxTotalMs).ToList();
            if (actions.Count == 0)
            {
                sb.AppendLine("행동 식별 마커 없음. 이전 캡처이거나 이 구간에서 새 마커가 관측되지 않았습니다. 행동이 없었다는 뜻은 아닙니다.").AppendLine();
                return;
            }
            var frames = scan.Frames.ToDictionary(r => r.Frame);
            sb.AppendLine("부모·자식 total은 중복되므로 합산하지 않습니다. Tick은 CPU 작업 시간이며 애니메이션 경과 시간이나 GPU 시간이 아닙니다.");
            sb.AppendLine("Queued는 View 제출, Dispatch는 결과 표시입니다. Core 성공 여부와 다릅니다. 아래 시간은 각 경로 total 최대 프레임의 값입니다.").AppendLine();
            sb.AppendLine("| 마커 경로 | 관측 프레임 수 | 최초 | 마지막 | 최대 total ms | 프레임 | PlayerLoop ms | EditorLoop ms | 프레임 GC |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|");
            foreach (ProfilerFrameScan.Totals action in actions)
            {
                ProfilerFrameScan.Row frame = frames[action.MaxTotalFrame];
                sb.Append("| ").Append(Cell(action.Key)).Append(" | ").Append(action.Frames).Append(" | ").Append(action.FirstFrame)
                    .Append(" | ").Append(action.LastFrame).Append(" | ").Append(ProfilerFrameCsvOutput.Number(action.MaxTotalMs))
                    .Append(" | ").Append(action.MaxTotalFrame).Append(" | ").Append(OptionalNumber(frame.PlayerLoopMs))
                    .Append(" | ").Append(OptionalNumber(frame.EditorLoopMs)).Append(" | ")
                    .Append(frame.Counters[0].HasValue ? Bytes(frame.Counters[0].Value) : "미관측").AppendLine(" |");
            }
            sb.AppendLine();
        }

        // 경로마다 직접 할당(그 경로의 GC − 바로 아래 자식 경로 GC 합)입니다. 경로 = 부모 경로 + 구분자 + 이름이라 끝을 잘라 부모를 찾습니다.
        private static Dictionary<string, double> DirectGc(ProfilerFrameScan scan)
        {
            var direct = scan.Paths.ToDictionary(t => t.Key, t => t.GcBytes, StringComparer.Ordinal);
            foreach (ProfilerFrameScan.Totals totals in scan.Paths)
            {
                if (totals.Depth == 0 || totals.GcBytes <= 0d)
                    continue;
                string parent = totals.Key.Substring(0, totals.Key.Length - totals.Name.Length - ProfilerFrameScan.PathSeparator.Length);
                direct[parent] -= totals.GcBytes;
            }
            return direct;
        }

        private static void WriteTree(HierarchyFrameDataView view, int parent, int depth, StringBuilder sb, List<List<int>> buffers)
        {
            if (depth >= TreeMaxDepth)
                return;
            while (buffers.Count <= depth)
                buffers.Add(new List<int>());
            List<int> children = buffers[depth];
            view.GetItemChildren(parent, children);
            children.Sort((a, b) => TotalOf(view, b).CompareTo(TotalOf(view, a)));

            int shown = 0;
            int hidden = 0;
            double hiddenMs = 0d;
            foreach (int id in children)
            {
                float total = TotalOf(view, id);
                if (total < TreeMinMs || shown >= TreeMaxChildren)
                {
                    hidden++;
                    hiddenMs += total;
                    continue;
                }
                shown++;
                sb.Append(' ', depth * 2).Append(view.GetItemName(id)).Append("  ").Append(Number(total)).Append(" ms (self ")
                    .Append(Number(view.GetItemColumnDataAsFloat(id, HierarchyFrameDataView.columnSelfTime))).Append(", 호출 ")
                    .Append(Number(view.GetItemColumnDataAsFloat(id, HierarchyFrameDataView.columnCalls))).Append(", GC ")
                    .Append(Bytes(view.GetItemColumnDataAsFloat(id, HierarchyFrameDataView.columnGcMemory))).AppendLine(")");
                WriteTree(view, id, depth + 1, sb, buffers);
            }
            if (hidden > 0 && hiddenMs >= TreeMinMs)
                sb.Append(' ', depth * 2).Append("… 나머지 ").Append(hidden).Append("개, 합 ").Append(Number(hiddenMs)).AppendLine(" ms");
        }

        private static float TotalOf(HierarchyFrameDataView view, int id) =>
            view.GetItemColumnDataAsFloat(id, HierarchyFrameDataView.columnTotalTime);

        private static void StatsRow(StringBuilder sb, string label, IEnumerable<double> values, Func<double, string> format)
        {
            List<double> sorted = values.OrderBy(v => v).ToList();
            if (sorted.Count == 0)
                return;
            sb.Append("| ").Append(Cell(label)).Append(" | ").Append(format(sorted.Average())).Append(" | ")
                .Append(format(Percentile(sorted, 0.5))).Append(" | ").Append(format(Percentile(sorted, 0.95))).Append(" | ")
                .Append(format(sorted[sorted.Count - 1])).AppendLine(" |");
        }

        // 정렬된 목록에서 가장 가까운 순위 방식으로 고릅니다.
        private static double Percentile(List<double> sorted, double ratio)
        {
            int index = (int)Math.Ceiling(ratio * sorted.Count) - 1;
            return sorted[Math.Max(0, Math.Min(sorted.Count - 1, index))];
        }

        #endregion

        #region 형식

        private static string CsvNumber(double value) => ProfilerFrameCsvOutput.Number(value);

        private static string OptionalCsvNumber(float? value) => value.HasValue ? CsvNumber(value.Value) : string.Empty;

        private static string Number(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        private static string OptionalNumber(float? value) => value.HasValue ? Number(value.Value) : string.Empty;

        private static string Bytes(double bytes)
        {
            if (bytes >= 1024d * 1024d)
                return (bytes / (1024d * 1024d)).ToString("0.##", CultureInfo.InvariantCulture) + " MB";
            if (bytes >= 1024d)
                return (bytes / 1024d).ToString("0.##", CultureInfo.InvariantCulture) + " KB";
            return bytes.ToString("0", CultureInfo.InvariantCulture) + " B";
        }

        private static string Csv(string value) => ProfilerFrameCsvOutput.Csv(value);

        private static string Cell(string value) => value.Replace("|", "\\|");

        #endregion
    }
}
