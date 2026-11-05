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
    /// 결과는 프로젝트 루트 <c>Logs/ProfilerExport/&lt;시각&gt;/</c>(git이 무시하는 폴더)에 셋입니다.
    /// <list type="bullet">
    /// <item><c>summary.md</c>: 프레임 시간·카운터 통계, 자기 시간 상위 마커, GC 할당 상위 경로, 느린 프레임의 계층.</item>
    /// <item><c>frames.csv</c>: 프레임마다 한 줄(시간·카운터·자기 시간이 가장 큰 마커).</item>
    /// <item><c>markers.csv</c>: 계층 경로마다 한 줄. <c>avg_*</c>는 내보낸 전체 프레임 수로 나눈 값입니다.</item>
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

        private static readonly Encoding Utf8 = new UTF8Encoding(false);
        private static readonly char[] CsvSpecial = { ',', '"', '\n', '\r' };

        [MenuItem("ProjectL/Profiler/기록 내보내기 (Claude 분석용)")]
        private static void Export()
        {
            int first = ProfilerDriver.firstFrameIndex;
            int last = ProfilerDriver.lastFrameIndex;
            if (first < 0 || last < first)
            {
                EditorUtility.DisplayDialog(Title, "Profiler에 기록된 프레임이 없습니다.\nProfiler 창에서 녹화하거나 .data 파일을 불러온 뒤 다시 누르세요.", "확인");
                return;
            }

            bool recording = ProfilerDriver.enabled;
            if (recording)
                Debug.LogWarning(LogPrefix + "Profiler가 녹화 중입니다. 내보내는 동안 버퍼에서 밀려난 프레임은 건너뜁니다.");

            var scan = new ProfilerFrameScan();
            try
            {
                for (int frame = first; frame <= last; frame++)
                {
                    if ((frame - first) % ProgressStep == 0
                        && EditorUtility.DisplayCancelableProgressBar(Title, $"프레임 {frame} / {last}", (float)(frame - first) / (last - first + 1)))
                    {
                        Debug.Log(LogPrefix + "취소했습니다. 파일을 쓰지 않았습니다.");
                        return;
                    }
                    scan.Read(frame);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (scan.Frames.Count == 0)
            {
                EditorUtility.DisplayDialog(Title, "메인 스레드 자료가 있는 프레임이 없습니다.", "확인");
                return;
            }

            string folder = Path.Combine(ExportRoot, DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "frames.csv"), FramesCsv(scan), Utf8);
            File.WriteAllText(Path.Combine(folder, "markers.csv"), MarkersCsv(scan), Utf8);
            File.WriteAllText(Path.Combine(folder, "summary.md"), Summary(scan, first, last, recording), Utf8);
            Debug.Log(LogPrefix + $"프레임 {scan.Frames.Count}개를 내보냈습니다: {folder}");
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
                sb.Append(row.Frame).Append(',').Append(Number(row.CpuMs)).Append(',')
                    .Append(row.GpuMs > 0f ? Number(row.GpuMs) : string.Empty).Append(',')
                    .Append(OptionalNumber(row.PlayerLoopMs)).Append(',').Append(OptionalNumber(row.EditorLoopMs));
                foreach (long? value in row.Counters)
                    sb.Append(',').Append(value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
                sb.Append(',').Append(Csv(row.TopSelfName ?? string.Empty)).Append(',').Append(Number(row.TopSelfMs)).AppendLine();
            }
            return sb.ToString();
        }

        private static string MarkersCsv(ProfilerFrameScan scan)
        {
            double frames = scan.Frames.Count;
            var sb = new StringBuilder();
            sb.AppendLine("path,depth,name,frames_present,avg_total_ms,max_total_ms,avg_self_ms,max_self_ms,avg_calls,avg_gc_bytes");
            foreach (ProfilerFrameScan.Totals totals in scan.Paths.OrderByDescending(t => t.TotalMs))
            {
                sb.Append(Csv(totals.Key)).Append(',').Append(totals.Depth).Append(',').Append(Csv(totals.Name)).Append(',')
                    .Append(totals.Frames).Append(',')
                    .Append(Number(totals.TotalMs / frames)).Append(',').Append(Number(totals.MaxTotalMs)).Append(',')
                    .Append(Number(totals.SelfMs / frames)).Append(',').Append(Number(totals.MaxSelfMs)).Append(',')
                    .Append(Number(totals.Calls / frames)).Append(',').Append(Number(totals.GcBytes / frames)).AppendLine();
            }
            return sb.ToString();
        }

        #endregion

        #region 요약

        private static string Summary(ProfilerFrameScan scan, int first, int last, bool recording)
        {
            IReadOnlyList<ProfilerFrameScan.Row> rows = scan.Frames;
            double frames = rows.Count;
            var sb = new StringBuilder();

            sb.AppendLine("# Profiler 기록 요약").AppendLine();
            sb.Append("- 내보낸 시각: ").AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            sb.Append("- Unity: ").Append(Application.unityVersion).Append(", 재생 중: ").AppendLine(EditorApplication.isPlaying ? "예" : "아니오");
            sb.Append("- 프레임: ").Append(first).Append(" ~ ").Append(last).Append(" (내보냄 ").Append(rows.Count)
                .Append("개, 건너뜀 ").Append(scan.Skipped).Append("개").Append(recording ? ", 녹화 중에 내보냄" : string.Empty).AppendLine(")");
            sb.Append("- 스레드: ").Append(scan.ThreadName).AppendLine(" 하나. 보기: 같은 이름 합침, 에디터 전용 샘플 뺌");
            sb.AppendLine("- 에디터에서 재생하면 EditorLoop(에디터 자체)가 프레임 시간에 들어갑니다. 게임 코드는 PlayerLoop 아래를 봅니다.");
            sb.AppendLine("- 표의 \"평균/프레임\"은 내보낸 전체 프레임 수로 나눈 값입니다.").AppendLine();

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

            bool byPlayerLoop = rows.All(r => r.PlayerLoopMs.HasValue);
            Func<ProfilerFrameScan.Row, float> slowness = byPlayerLoop ? r => r.PlayerLoopMs.Value : (Func<ProfilerFrameScan.Row, float>)(r => r.CpuMs);
            sb.Append("## 느린 프레임 ").Append(WorstFrameCount).Append("개 (기준: ").Append(byPlayerLoop ? "PlayerLoop 시간" : "프레임 CPU 시간").AppendLine(")").AppendLine();
            sb.Append("계층의 각 줄은 `이름  전체 ms (self ms, 호출 수, GC)`입니다. ").Append(Number(TreeMinMs))
                .Append(" ms보다 짧거나 형제 ").Append(TreeMaxChildren).AppendLine("개를 넘는 줄은 \"나머지\"로 묶었습니다.").AppendLine();
            var buffers = new List<List<int>>();
            foreach (ProfilerFrameScan.Row row in rows.OrderByDescending(slowness).Take(WorstFrameCount))
            {
                sb.Append("### 프레임 ").Append(row.Frame).Append(" — ").Append(Number(slowness(row))).Append(" ms")
                    .Append(" (CPU ").Append(Number(row.CpuMs)).Append(" ms, GC ").Append(Bytes(row.Counters[0] ?? 0)).AppendLine(")").AppendLine();
                sb.AppendLine("```");
                using (HierarchyFrameDataView view = ProfilerFrameScan.Open(row.Frame))
                {
                    if (view == null)
                        sb.AppendLine("(내보내는 동안 버퍼에서 사라졌습니다)");
                    else
                        WriteTree(view, view.GetRootItemID(), 0, sb, buffers);
                }
                sb.AppendLine("```").AppendLine();
            }
            return sb.ToString();
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

        private static string Csv(string value) =>
            value.IndexOfAny(CsvSpecial) < 0 ? value : "\"" + value.Replace("\"", "\"\"") + "\"";

        private static string Cell(string value) => value.Replace("|", "\\|");

        #endregion
    }
}
