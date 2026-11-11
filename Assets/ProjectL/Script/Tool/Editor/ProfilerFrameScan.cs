using System;
using System.Collections.Generic;
using UnityEditor.Profiling;
using UnityEditorInternal;

namespace ProjectL.Tool.Editor
{
    /// <summary>
    /// Profiler 버퍼의 프레임을 하나씩 읽어 프레임마다 한 줄과 마커 합계를 모읍니다(<see cref="ProfilerFrameExporter"/>가 씁니다).
    /// 읽는 스레드는 메인 스레드 하나이고, 보기는 Profiler Hierarchy의 기본 보기와 같습니다(같은 이름 합침, 에디터 전용 샘플 뺌).
    /// </summary>
    internal sealed class ProfilerFrameScan
    {
        internal const string PathSeparator = " > ";

        private const int MainThreadIndex = 0;
        private const HierarchyFrameDataView.ViewModes ViewMode =
            HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName | HierarchyFrameDataView.ViewModes.HideEditorOnlySamples;
        private const string PlayerLoopName = "PlayerLoop";
        private const string EditorLoopName = "EditorLoop";

        /// <summary>프레임마다 읽는 카운터입니다. 열 이름이 <c>_bytes</c>로 끝나면 바이트 값입니다. 그 프레임에 값이 없으면 비웁니다.</summary>
        internal static readonly (string Column, string Marker)[] Counters =
        {
            ("gc_alloc_bytes", "GC Allocated In Frame"),
            ("gc_alloc_count", "GC Allocation In Frame Count"),
            ("draw_calls", "Draw Calls Count"),
            ("batches", "Batches Count"),
            ("setpass_calls", "SetPass Calls Count"),
            ("triangles", "Triangles Count"),
            ("total_used_memory_bytes", "Total Used Memory"),
            ("gc_used_memory_bytes", "GC Used Memory"),
        };

        private readonly List<Row> _frames = new List<Row>();
        private readonly Dictionary<string, Totals> _paths = new Dictionary<string, Totals>(StringComparer.Ordinal);
        private readonly Dictionary<string, Totals> _names = new Dictionary<string, Totals>(StringComparer.Ordinal);
        // 한 프레임 안에서 같은 이름이 여러 경로에 나오므로, 이름 합계는 프레임마다 먼저 더한 뒤 접습니다.
        private readonly Dictionary<string, (double Self, double Calls, double Gc)> _frameNames =
            new Dictionary<string, (double Self, double Calls, double Gc)>(StringComparer.Ordinal);
        private readonly List<List<int>> _children = new List<List<int>>();
        private readonly Action<Sample> _writeSample;

        internal ProfilerFrameScan(Action<Sample> writeSample = null)
        {
            _writeSample = writeSample;
        }

        internal IReadOnlyList<Row> Frames => _frames;
        /// <summary>계층 경로마다의 합계입니다. 같은 이름 합침 보기라 한 프레임 안에서 경로는 하나뿐입니다.</summary>
        internal IReadOnlyCollection<Totals> Paths => _paths.Values;
        /// <summary>마커 이름마다의 합계입니다(모든 경로를 더함). 전체 시간은 재귀 때문에 뜻이 없어 쓰지 않습니다.</summary>
        internal IReadOnlyCollection<Totals> Names => _names.Values;
        /// <summary>버퍼에서 사라졌거나 메인 스레드 자료가 없어 건너뛴 프레임 수입니다.</summary>
        internal int Skipped { get; private set; }
        internal string ThreadName { get; private set; }

        /// <summary>그 프레임의 메인 스레드 계층을 엽니다. 부른 쪽이 Dispose합니다. 없으면 null입니다.</summary>
        internal static HierarchyFrameDataView Open(int frame)
        {
            HierarchyFrameDataView view = ProfilerDriver.GetHierarchyFrameDataView(
                frame, MainThreadIndex, ViewMode, HierarchyFrameDataView.columnTotalTime, false);
            if (view != null && view.valid)
                return view;
            view?.Dispose();
            return null;
        }

        /// <param name="frame">지금 불러온 버퍼에서 열 프레임 번호입니다.</param>
        /// <param name="outputFrame">출력·합계에 적을 번호입니다. 구간 원본을 이어 붙일 때 <paramref name="frame"/>과 다를 수 있습니다.</param>
        internal void Read(int frame, int outputFrame)
        {
            using (HierarchyFrameDataView view = Open(frame))
            {
                if (view == null)
                {
                    Skipped++;
                    return;
                }
                ThreadName ??= view.threadName;

                var row = new Row(outputFrame, view.frameTimeMs, view.frameGpuTimeMs, ReadCounters(frame));
                _frameNames.Clear();
                Walk(view, view.GetRootItemID(), null, 0, row);
                foreach (KeyValuePair<string, (double Self, double Calls, double Gc)> pair in _frameNames)
                    TotalsOf(_names, pair.Key, pair.Key, -1).Add(outputFrame, 0d, pair.Value.Self, pair.Value.Calls, pair.Value.Gc);
                _frames.Add(row);
            }
        }

        private void Walk(HierarchyFrameDataView view, int parent, string parentPath, int depth, Row row)
        {
            // 깊이마다 목록 하나를 다시 씁니다. 아래 깊이를 읽어도 이 깊이 목록은 바뀌지 않습니다.
            List<int> children = ChildrenAt(depth);
            view.GetItemChildren(parent, children);
            foreach (int id in children)
            {
                string name = view.GetItemName(id);
                string path = parentPath == null ? name : parentPath + PathSeparator + name;
                float total = view.GetItemColumnDataAsFloat(id, HierarchyFrameDataView.columnTotalTime);
                float self = view.GetItemColumnDataAsFloat(id, HierarchyFrameDataView.columnSelfTime);
                float calls = view.GetItemColumnDataAsFloat(id, HierarchyFrameDataView.columnCalls);
                float gc = view.GetItemColumnDataAsFloat(id, HierarchyFrameDataView.columnGcMemory);

                TotalsOf(_paths, path, name, depth).Add(row.Frame, total, self, calls, gc);
                _writeSample?.Invoke(new Sample(row.Frame, path, depth, name, total, self, calls, gc));
                _frameNames.TryGetValue(name, out (double Self, double Calls, double Gc) sum);
                _frameNames[name] = (sum.Self + self, sum.Calls + calls, sum.Gc + gc);
                if (depth == 0)
                    row.NoteRoot(name, total);
                row.NoteSelf(name, self);

                Walk(view, id, path, depth + 1, row);
            }
        }

        private List<int> ChildrenAt(int depth)
        {
            while (_children.Count <= depth)
                _children.Add(new List<int>());
            return _children[depth];
        }

        private static long?[] ReadCounters(int frame)
        {
            var values = new long?[Counters.Length];
            using (RawFrameDataView raw = ProfilerDriver.GetRawFrameDataView(frame, MainThreadIndex))
            {
                if (raw == null || !raw.valid)
                    return values;
                for (int i = 0; i < Counters.Length; i++)
                {
                    int marker = raw.GetMarkerId(Counters[i].Marker);
                    if (marker != FrameDataView.invalidMarkerId && raw.HasCounterValue(marker))
                        values[i] = raw.GetCounterValueAsLong(marker);
                }
            }
            return values;
        }

        private static Totals TotalsOf(Dictionary<string, Totals> table, string key, string name, int depth)
        {
            if (!table.TryGetValue(key, out Totals totals))
            {
                totals = new Totals(key, name, depth);
                table.Add(key, totals);
            }
            return totals;
        }

        /// <summary>프레임 하나의 값입니다. 시간은 ms, 카운터 순서는 <see cref="Counters"/>와 같습니다.</summary>
        internal sealed class Row
        {
            internal Row(int frame, float cpuMs, float gpuMs, long?[] counters)
            {
                Frame = frame;
                CpuMs = cpuMs;
                GpuMs = gpuMs;
                Counters = counters ?? throw new ArgumentNullException(nameof(counters));
            }

            internal int Frame { get; }
            /// <summary>메인 스레드 프레임 시간입니다. 에디터에서 재생하면 EditorLoop도 들어갑니다.</summary>
            internal float CpuMs { get; }
            /// <summary>0 이하면 GPU 기록이 없습니다.</summary>
            internal float GpuMs { get; }
            internal float? PlayerLoopMs { get; private set; }
            internal float? EditorLoopMs { get; private set; }
            internal IReadOnlyList<long?> Counters { get; }
            internal string TopSelfName { get; private set; }
            internal float TopSelfMs { get; private set; }

            internal void NoteRoot(string name, float totalMs)
            {
                if (name == PlayerLoopName)
                    PlayerLoopMs = totalMs;
                else if (name == EditorLoopName)
                    EditorLoopMs = totalMs;
            }

            internal void NoteSelf(string name, float selfMs)
            {
                if (TopSelfName != null && selfMs <= TopSelfMs)
                    return;
                TopSelfName = name;
                TopSelfMs = selfMs;
            }
        }

        /// <summary>경로 또는 이름 하나의 합계입니다. 평균은 부른 쪽이 내보낸 전체 프레임 수로 나눕니다.</summary>
        internal sealed class Totals
        {
            internal Totals(string key, string name, int depth)
            {
                Key = key;
                Name = name;
                Depth = depth;
            }

            internal string Key { get; }
            internal string Name { get; }
            /// <summary>경로 합계의 깊이(0 = 최상위)입니다. 이름 합계는 -1입니다.</summary>
            internal int Depth { get; }
            internal int Frames { get; private set; }
            internal double Calls { get; private set; }
            internal double TotalMs { get; private set; }
            internal double MaxTotalMs { get; private set; }
            internal double SelfMs { get; private set; }
            internal double MaxSelfMs { get; private set; }
            internal double GcBytes { get; private set; }
            internal double MaxGcBytes { get; private set; }
            internal int FirstFrame { get; private set; }
            internal int LastFrame { get; private set; }
            internal int MaxTotalFrame { get; private set; }
            internal int MaxSelfFrame { get; private set; }
            internal int MaxGcFrame { get; private set; }

            internal void Add(int frame, double totalMs, double selfMs, double calls, double gcBytes)
            {
                if (Frames == 0)
                    FirstFrame = frame;
                LastFrame = frame;
                if (Frames == 0 || totalMs > MaxTotalMs)
                    MaxTotalFrame = frame;
                if (Frames == 0 || selfMs > MaxSelfMs)
                    MaxSelfFrame = frame;
                if (Frames == 0 || gcBytes > MaxGcBytes)
                    MaxGcFrame = frame;
                Frames++;
                Calls += calls;
                TotalMs += totalMs;
                MaxTotalMs = Math.Max(MaxTotalMs, totalMs);
                SelfMs += selfMs;
                MaxSelfMs = Math.Max(MaxSelfMs, selfMs);
                GcBytes += gcBytes;
                MaxGcBytes = Math.Max(MaxGcBytes, gcBytes);
            }
        }

        /// <summary>읽은 프레임의 경로 하나입니다. 순차 출력하고 장기 보관하지 않습니다.</summary>
        internal readonly struct Sample
        {
            internal Sample(int frame, string path, int depth, string name, double total, double self, double calls, double gc)
            {
                Frame = frame;
                Path = path;
                Depth = depth;
                Name = name;
                Total = total;
                Self = self;
                Calls = calls;
                Gc = gc;
            }

            internal int Frame { get; }
            internal string Path { get; }
            internal int Depth { get; }
            internal string Name { get; }
            internal double Total { get; }
            internal double Self { get; }
            internal double Calls { get; }
            internal double Gc { get; }
        }
    }
}
