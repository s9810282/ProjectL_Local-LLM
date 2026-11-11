using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;

namespace ProjectL.Tool.Editor
{
    /// <summary>
    /// 완성된 네이티브 기록을 읽어 공통 CSV 내보내기로 넘깁니다. 기록 수명은 맡지 않습니다.
    /// 구간으로 나뉜 기록은 구간을 한 번에 하나만 불러 이어 씁니다. 불러오기 전에 메모리를 어림해 넘치면 불러오지 않고,
    /// 불러온 뒤에는 다음 구간 전과 변환이 끝날 때 Profiler 버퍼를 비웁니다.
    /// </summary>
    internal static class ProfilerCaptureImport
    {
        private const string Title = "Profiler 원본 변환";
        private const string SkippedTooLarge = "SkippedTooLarge";

        /// <summary>불러올 원본 파일 하나입니다. 옛 기록·외부 파일은 하나, 구간 기록은 구간마다 하나입니다.</summary>
        private sealed class Source
        {
            /// <param name="spanFrames">관측한 게임 프레임 수입니다. 모르면 0입니다.</param>
            /// <param name="upperFrames">히스토리 상한을 잡을 넉넉한 값입니다(옛 기록은 마지막 관측 번호 + 1).</param>
            internal Source(string path, int spanFrames, int upperFrames, ProfilerCaptureSegment segment)
            {
                FilePath = path;
                Segment = segment;
                Bytes = File.Exists(path) ? new FileInfo(path).Length : 0;
                // 크래시로 관측이 짧게 남은 구간도 있어 파일 크기 어림과 큰 쪽을 씁니다.
                MemoryFrames = Math.Max(Math.Max(spanFrames, 0), ProfilerCaptureMemory.EstimateFrames(Bytes));
                ExpectedFrames = Math.Max(upperFrames, MemoryFrames);
            }

            internal string FilePath { get; }
            internal long Bytes { get; }
            /// <summary>남은 메모리를 읽지 못할 때 고정 한도와 비교할 프레임 수입니다.</summary>
            internal int MemoryFrames { get; }
            /// <summary>히스토리 상한을 잡을 프레임 수입니다. 모자라면 다시 불러와 맞춥니다.</summary>
            internal int ExpectedFrames { get; }
            internal ProfilerCaptureSegment Segment { get; }
        }

        [MenuItem("ProjectL/Profiler/저장된 raw 또는 data 전체 내보내기...")]
        private static void SelectFile()
        {
            string path = EditorUtility.OpenFilePanelWithFilters("연속 기록 원본 선택", ProfilerContinuousCapture.CaptureRoot,
                new[] { "Profiler 원본", "raw,data" });
            if (!string.IsNullOrEmpty(path))
                Export(path, null, true);
        }

        /// <param name="path">원본 파일입니다. 구간 기록이면 어느 구간을 주어도 그 기록의 구간 전체를 변환합니다.</param>
        /// <param name="interactive">사용자가 메뉴로 시작했으면 true입니다. 메모리가 모자라면 묻고, false면 묻지 않고 건너뜁니다.</param>
        internal static void Export(string path, ProfilerCaptureRecord record, bool interactive)
        {
            if (!ProfilerContinuousCapture.Available)
                throw new InvalidOperationException("[Profiler] 크래시 조사 중으로 raw/data 변환 기능을 비활성화했습니다.");
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode || ProfilerContinuousCapture.IsRecording)
                throw new InvalidOperationException("[Profiler] Play와 연속 기록을 종료한 뒤 원본을 변환하세요.");
            path = Path.GetFullPath(path);
            if (!File.Exists(path) || new FileInfo(path).Length == 0)
                throw new InvalidOperationException("[Profiler] 기록 원본이 없거나 비어 있습니다: " + path);

            record ??= ReadRecord(path);
            // 기대 프레임 수는 내보내기 세션이 불러온 범위 칸을 비우기 전에 정합니다.
            List<Source> sources = SourcesOf(record, path);

            // 한 번에 하나만 올리므로 가장 큰 파일이 판정합니다. 버퍼 백업보다 먼저 판정해 건너뛸 때는 백업도 만들지 않습니다.
            Source largest = sources[0];
            foreach (Source source in sources)
            {
                if (source.Bytes > largest.Bytes) largest = source;
            }
            ProfilerCaptureMemory.Estimate memory = ProfilerCaptureMemory.Check(largest.Bytes, largest.MemoryFrames);
            record.loadNeedBytes = memory.NeedBytes;
            record.loadAvailableBytes = memory.AvailableBytes;
            // '그래도 변환'을 골랐으면 구간마다 다시 묻거나 건너뛰지 않습니다.
            bool forced = false;
            if (!memory.Fits)
            {
                if (!ConfirmLargeLoad(largest.FilePath, memory, interactive))
                {
                    // 앞 변환의 결과가 이번 결과처럼 보이지 않게 비웁니다.
                    ProfilerFrameExporter.Session.ResetResult(record);
                    record.exportStatus = SkippedTooLarge;
                    Debug.LogWarning("[Profiler] 원본이 커서 불러오지 않았습니다(CSV 변환 안 함). " + memory.Describe()
                        + ".\n원본은 그대로 남겼습니다: " + largest.FilePath
                        + "\n할 일: 다른 프로그램을 닫고 다시 내보내기 메뉴에서 '그래도 변환'을 고르거나, 다시 기록하세요(새 기록은 구간 파일로 나뉘어 한 번에 하나씩 변환됩니다).");
                    return;
                }
                forced = true;
            }
            if (memory.MayPage)
                Debug.LogWarning("[Profiler] 불러올 양이 남은 물리 메모리보다 많아 변환이 매우 느릴 수 있습니다. " + memory.Describe() + ".");

            // 다른 도구의 기록 파일을 닫거나 바꾸지 않습니다.
            ProfilerContinuousCapture.CloseInterruptedLog();
            if (Profiler.enableBinaryLog)
                throw new InvalidOperationException("[Profiler] 다른 바이너리 기록이 켜져 있어 원본을 불러올 수 없습니다.");
            ProfilerDriver.enabled = false;
            Profiler.enabled = false;
            Directory.CreateDirectory(ProfilerContinuousCapture.CaptureRoot);
            if (ProfilerDriver.firstFrameIndex >= 0)
            {
                string backup = Path.Combine(ProfilerContinuousCapture.CaptureRoot, "buffer-before-load-" + Guid.NewGuid().ToString("N") + ".data");
                ProfilerDriver.SaveProfile(backup);
                if (!File.Exists(backup) || new FileInfo(backup).Length == 0)
                    throw new InvalidOperationException("[Profiler] 기존 버퍼 보존에 실패해 원본 로드를 중단했습니다: " + backup);
            }

            bool loaded = false;
            try
            {
                using var history = new ProfilerCaptureHistory();
                using var export = new ProfilerFrameExporter.Session(record);
                bool completed = true;
                bool outOfMemory = false;
                for (int i = 0; i < sources.Count && completed; i++)
                {
                    Source source = sources[i];
                    if (outOfMemory)
                    {
                        MarkMissing(export, source, i, "SkippedMemory", "앞 구간에서 남은 메모리가 모자라 멈춤");
                        continue;
                    }
                    if (source.Bytes == 0)
                    {
                        MarkMissing(export, source, i, "Missing", "파일이 없거나 비어 있음");
                        continue;
                    }
                    // 앞 구간을 내려놓은 뒤 다음 구간을 올려 한 번에 한 구간만 메모리에 둡니다. 첫 구간 앞의 사용자 버퍼는 이미 백업했습니다.
                    if (loaded)
                        ProfilerDriver.ClearAllFrames();
                    // 내려놓은 메모리가 바로 돌아오지 않을 수 있어 구간마다 다시 봅니다. 모자라면 읽은 데까지만 출력하고 불완전으로 남깁니다.
                    if (sources.Count > 1 && !forced)
                    {
                        ProfilerCaptureMemory.Estimate again = ProfilerCaptureMemory.Check(source.Bytes, source.MemoryFrames);
                        if (!again.Fits)
                        {
                            outOfMemory = true;
                            MarkMissing(export, source, i, "SkippedMemory", again.Describe());
                            continue;
                        }
                    }
                    // 불러오기가 중간에 실패해도 버퍼는 이미 바뀌었을 수 있습니다.
                    loaded = true;
                    EditorUtility.DisplayProgressBar(Title, LoadingLabel(i, sources.Count, source.FilePath), (float)i / sources.Count);
                    try
                    {
                        history.Load(source.FilePath, source.ExpectedFrames);
                    }
                    catch (InvalidOperationException error) when (sources.Count > 1)
                    {
                        // 구간 하나를 못 읽어도 나머지는 변환하고, 결과는 불완전으로 남깁니다.
                        MarkMissing(export, source, i, "Failed", error.Message);
                        continue;
                    }
                    finally
                    {
                        EditorUtility.ClearProgressBar();
                    }
                    ProfilerDriver.enabled = false;
                    completed = export.ReadLoaded(i, sources.Count, source.Segment);
                }
                record.importHistoryCapacity = history.LargestCapacity;
                string folder = completed ? export.Commit() : null;
                if (folder == null)
                    Debug.Log("[Profiler] CSV 출력을 완료하지 못했습니다 (" + record.exportStatus + "). 원본에서 다시 내보낼 수 있습니다: " + path);
            }
            finally
            {
                // 새 녹화가 버퍼를 바꾸지 않도록 정지 상태로 둡니다.
                ProfilerDriver.enabled = false;
                Profiler.enabled = false;
                if (loaded)
                    ClearLoadedFrames(Path.GetDirectoryName(path));
            }
        }

        // 구간 기록은 구간마다, 그 밖은 고른 파일 하나입니다. 기대 프레임 수는 구간을 바꾼 게임 프레임 사이 간격입니다.
        private static List<Source> SourcesOf(ProfilerCaptureRecord record, string path)
        {
            var sources = new List<Source>();
            if (!record.IsSegmented)
            {
                sources.Add(new Source(path, record.ObservedGameFrameSpan, Math.Max(record.lastGameFrame, record.loadedLastFrame) + 1, null));
                return sources;
            }
            for (int i = 0; i < record.segments.Count; i++)
            {
                ProfilerCaptureSegment segment = record.segments[i];
                int start = StartOf(record, segment);
                int end = i + 1 < record.segments.Count ? StartOf(record, record.segments[i + 1]) : record.lastGameFrame + 1;
                int span = start >= 0 && end > start ? end - start : 0;
                sources.Add(new Source(Path.GetFullPath(segment.path), span, span, segment));
            }
            return sources;
        }

        private static int StartOf(ProfilerCaptureRecord record, ProfilerCaptureSegment segment) =>
            segment.firstGameFrame >= 0 ? segment.firstGameFrame : record.firstGameFrame;

        private static void MarkMissing(ProfilerFrameExporter.Session export, Source source, int index, string status, string reason)
        {
            if (source.Segment != null)
                source.Segment.loadStatus = status;
            export.NoteMissing("구간 " + index + " `" + source.FilePath + "`: " + reason);
            Debug.LogWarning("[Profiler] 구간 " + index + "을(를) 읽지 못해 건너뜁니다(" + reason + "): " + source.FilePath);
        }

        private static string LoadingLabel(int index, int count, string path) =>
            count > 1 ? $"구간 {index + 1}/{count} 불러오는 중: {Path.GetFileName(path)}" : "원본 불러오는 중: " + Path.GetFileName(path);

        // 불러온 프레임은 원본이 디스크에 있고 그 전 버퍼는 백업했으므로, 비워서 메모리를 바로 돌려줍니다.
        private static void ClearLoadedFrames(string folder)
        {
            try
            {
                ProfilerDriver.ClearAllFrames();
                Debug.Log("[Profiler] 변환을 마치고 Profiler 버퍼를 비웠습니다. 이 기록을 Profiler 창에서 보려면 원본(구간 파일)을 다시 불러오세요: " + folder);
            }
            catch (Exception error)
            {
                Debug.LogWarning("[Profiler] 변환 뒤 Profiler 버퍼를 비우지 못했습니다. 메모리가 계속 잡혀 있으면 Editor를 다시 켜세요: " + error.Message);
            }
        }

        private static bool ConfirmLargeLoad(string path, ProfilerCaptureMemory.Estimate memory, bool interactive)
        {
            if (!interactive)
                return false;
            // 기본 버튼(Enter)과 Esc가 모두 변환하지 않게 버튼을 셋 둡니다.
            int choice = EditorUtility.DisplayDialogComplex(Title,
                "이 원본은 이 PC에서 한 번에 불러오기에 너무 큽니다.\n" + memory.Describe() + ".\n\n"
                + "그대로 불러오면 메모리가 바닥나 Editor가 멈추거나 꺼질 수 있습니다. 원본은 어느 경우에도 지우지 않습니다.\n\n"
                + "다른 프로그램을 닫았거나 위험을 감수할 때만 '그래도 변환'을 고르세요.\n" + path,
                "변환하지 않음", "취소", "그래도 변환");
            return choice == 2;
        }

        private static ProfilerCaptureRecord ReadRecord(string path)
        {
            // 선택한 외부 원본 옆의 메타데이터는 읽기만 합니다.
            string manifest = Path.Combine(Path.GetDirectoryName(path), "capture.json");
            if (File.Exists(manifest))
            {
                try
                {
                    var record = JsonUtility.FromJson<ProfilerCaptureRecord>(File.ReadAllText(manifest));
                    if (record != null && !string.IsNullOrEmpty(record.rawPath) && IsPartOf(record, path))
                    {
                        // 기록 폴더를 옮겼어도 같은 폴더의 메타데이터이므로, 적힌 경로 대신 이 폴더의 같은 이름 파일을 가리킵니다.
                        string folder = Path.GetDirectoryName(path);
                        record.rawPath = Path.Combine(folder, Path.GetFileName(record.rawPath));
                        if (record.segments != null)
                        {
                            foreach (ProfilerCaptureSegment segment in record.segments)
                                segment.path = Path.Combine(folder, Path.GetFileName(segment.path));
                        }
                        if (record.status == "Recording")
                        {
                            record.status = "Interrupted";
                            record.stopReason = "정상 종료되지 않은 기록 원본을 다시 불러옴";
                        }
                        return record;
                    }
                }
                catch (Exception error)
                {
                    Debug.LogWarning("[Profiler] 기록 메타데이터를 읽지 못해 관측 범위 없이 원본만 변환합니다: " + error.Message);
                }
            }
            return new ProfilerCaptureRecord { rawPath = path, status = "Imported", unityVersion = Application.unityVersion };
        }

        // 어느 구간 파일을 골라도 그 기록 전체로 읽습니다. 메타데이터는 고른 파일과 같은 폴더에 있으므로 파일 이름으로 맞춥니다.
        private static bool IsPartOf(ProfilerCaptureRecord record, string path)
        {
            string name = Path.GetFileName(path);
            if (IsSameName(record.rawPath, name)) return true;
            if (record.segments == null) return false;
            foreach (ProfilerCaptureSegment segment in record.segments)
            {
                if (IsSameName(segment.path, name)) return true;
            }
            return false;
        }

        private static bool IsSameName(string recordedPath, string name) =>
            !string.IsNullOrEmpty(recordedPath) && string.Equals(Path.GetFileName(recordedPath), name, StringComparison.OrdinalIgnoreCase);
    }
}
