using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace ProjectL.Tool.Editor
{
    /// <summary>원본을 Profiler 메모리에 올리기 전에 이 PC가 감당할 수 있는지 어림합니다. 불러오기·기록 수명은 맡지 않습니다.</summary>
    /// <remarks>
    /// Unity는 원본의 모든 프레임을 한 번에 올립니다. 11,065프레임(원본 1.72GB)을 불러왔을 때 에디터 메모리가 약 9GB 늘었습니다(원본 크기의 약 5.3배).
    /// 47,466프레임(원본 7.4GB)은 가상 메모리가 바닥나 에디터가 죽었습니다(2026-11-10). 값은 어림이라 실제 사용량과 다를 수 있습니다.
    /// </remarks>
    internal static class ProfilerCaptureMemory
    {
        // 원본 1바이트를 올릴 때 드는 메모리입니다. 측정값 약 5.3배에 여유를 둡니다.
        private const double LoadedBytesPerFileByte = 5.5d;
        // 불러온 뒤에도 남겨 둘 메모리입니다. 에디터와 다른 프로그램이 계속 씁니다.
        private const long SafetyBytes = 4L * 1024 * 1024 * 1024;
        // 남은 메모리를 읽지 못할 때의 고정 한도입니다. 8,605·11,065프레임 변환은 성공했습니다.
        private const int FallbackMaxFrames = 10000;
        // 프레임 수를 모를 때 파일 크기로 어림하는 프레임당 원본 크기입니다(측정 155~180KB).
        private const long FileBytesPerFrame = 160L * 1024;
        private const double BytesPerGb = 1024d * 1024d * 1024d;

        /// <summary>한 원본의 판정입니다. 메모리 값을 모르면 -1입니다.</summary>
        internal readonly struct Estimate
        {
            internal Estimate(long needBytes, long availableBytes, long availablePhysicalBytes, int frames, bool fits, bool mayPage)
            {
                NeedBytes = needBytes;
                AvailableBytes = availableBytes;
                AvailablePhysicalBytes = availablePhysicalBytes;
                Frames = frames;
                Fits = fits;
                MayPage = mayPage;
            }

            internal long NeedBytes { get; }
            /// <summary>이 프로세스가 더 쓸 수 있는 가상 메모리(commit)입니다.</summary>
            internal long AvailableBytes { get; }
            internal long AvailablePhysicalBytes { get; }
            /// <summary>판정에 쓴 프레임 수입니다. 관측값이 없으면 파일 크기로 어림합니다.</summary>
            internal int Frames { get; }
            internal bool Fits { get; }
            /// <summary>가상 메모리 안이지만 물리 메모리를 넘어 매우 느려질 수 있습니다.</summary>
            internal bool MayPage { get; }

            internal string Describe()
            {
                if (AvailableBytes < 0)
                    return "필요 메모리 약 " + Gb(NeedBytes) + ", 남은 메모리를 읽지 못해 프레임 한도 "
                        + FallbackMaxFrames.ToString("N0", CultureInfo.InvariantCulture) + "로 판정(원본 약 "
                        + Frames.ToString("N0", CultureInfo.InvariantCulture) + "프레임)";
                return "필요 메모리 약 " + Gb(NeedBytes) + ", 남은 가상 메모리 " + Gb(AvailableBytes)
                    + "(이 중 " + Gb(SafetyBytes) + "는 남겨 둠), 남은 물리 메모리 " + Gb(AvailablePhysicalBytes);
            }
        }

        /// <summary>프레임 수를 모를 때 파일 크기로 어림합니다.</summary>
        internal static int EstimateFrames(long fileBytes) => (int)Math.Min(int.MaxValue, Math.Max(0L, fileBytes) / FileBytesPerFrame);

        internal static Estimate Check(long fileBytes, int knownFrames)
        {
            long need = (long)(fileBytes * LoadedBytesPerFileByte);
            int frames = knownFrames > 0 ? knownFrames : EstimateFrames(fileBytes);
            if (!TryReadAvailable(out long available, out long physical))
                return new Estimate(need, -1, -1, frames, frames <= FallbackMaxFrames, false);
            bool fits = need <= available - SafetyBytes;
            return new Estimate(need, available, physical, frames, fits, fits && need > physical);
        }

        internal static string Gb(long bytes) => (bytes / BytesPerGb).ToString("0.0", CultureInfo.InvariantCulture) + "GB";

#if UNITY_EDITOR_WIN
        [StructLayout(LayoutKind.Sequential)]
        private struct MemoryStatusEx
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

        private static bool TryReadAvailable(out long available, out long physical)
        {
            var status = new MemoryStatusEx { dwLength = (uint)Marshal.SizeOf<MemoryStatusEx>() };
            try
            {
                if (GlobalMemoryStatusEx(ref status))
                {
                    // ullAvailPageFile은 이 프로세스가 더 commit할 수 있는 양입니다(물리 + 페이지 파일).
                    available = ToLong(status.ullAvailPageFile);
                    physical = ToLong(status.ullAvailPhys);
                    return true;
                }
            }
            catch (Exception)
            {
                // 읽지 못하면 고정 한도로 판정합니다.
            }
            available = -1;
            physical = -1;
            return false;
        }

        private static long ToLong(ulong value) => value > long.MaxValue ? long.MaxValue : (long)value;
#else
        private static bool TryReadAvailable(out long available, out long physical)
        {
            available = -1;
            physical = -1;
            return false;
        }
#endif
    }
}
