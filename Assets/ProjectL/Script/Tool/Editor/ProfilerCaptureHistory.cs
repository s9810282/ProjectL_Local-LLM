using System;
using System.Linq;
using System.Reflection;
using UnityEditorInternal;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    /// <summary>파일 변환 동안만 히스토리를 확장합니다. 플레이 녹화 버퍼와 사용자 설정은 변경하지 않습니다.</summary>
    internal sealed class ProfilerCaptureHistory : IDisposable
    {
        // 상한에 닿아 앞쪽이 잘렸으면 늘려서 다시 불러옵니다. 그래도 닿으면 이 횟수 뒤 잘린 채 진행합니다.
        private const int MaxLoadAttempts = 3;
        private readonly MethodInfo _setLength;
        private readonly int _previousLength;

        internal ProfilerCaptureHistory()
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            _setLength = typeof(ProfilerDriver).GetMethod("SetMaxFrameHistoryLength", flags, null, new[] { typeof(int) }, null);
            Type settings = typeof(ProfilerDriver).Assembly.GetTypes().FirstOrDefault(type => type.Name == "ProfilerUserSettings");
            PropertyInfo frameCount = settings?.GetProperty("frameCount", flags);
            if (_setLength == null || frameCount == null || frameCount.PropertyType != typeof(int))
                throw new NotSupportedException("[Profiler] 현재 Unity의 전체 파일 로드 API를 확인할 수 없습니다. 원본을 보존하고 변환을 중단합니다.");
            _previousLength = (int)frameCount.GetValue(null);
        }

        /// <summary>이번 변환에서 쓴 가장 큰 히스토리 길이입니다.</summary>
        internal int LargestCapacity { get; private set; }

        /// <param name="expectedFrames">이 파일에 있을 것으로 보는 프레임 수입니다. 어림이며 모자라도 다시 불러와 맞춥니다.</param>
        internal void Load(string path, int expectedFrames)
        {
            // Unity는 raw를 읽을 때도 히스토리 길이만큼 최근 프레임만 남깁니다.
            // 구간 원본은 번호가 0부터가 아닐 수 있어, 마지막 번호가 아니라 불러온 개수가 상한에 닿았는지로 잘림을 판정합니다.
            int capacity = Math.Max(_previousLength, checked(Math.Max(expectedFrames, 1) * 2 + 64));
            for (int attempt = 1; ; attempt++)
            {
                _setLength.Invoke(null, new object[] { capacity });
                LoadFile(path);
                int first = ProfilerDriver.firstFrameIndex;
                int last = ProfilerDriver.lastFrameIndex;
                int loaded = first < 0 || last < first ? 0 : checked(last - first + 1);
                if (loaded < capacity)
                    break;
                if (attempt >= MaxLoadAttempts)
                {
                    Debug.LogWarning("[Profiler] 히스토리 길이 " + capacity + "에 닿아 앞쪽 프레임이 잘렸을 수 있습니다: " + path);
                    break;
                }
                capacity = Math.Max(checked(loaded * 2), checked(last + 2));
                // 잘린 프레임을 내려놓고 다시 불러와 같은 파일이 메모리에 두 번 있지 않게 합니다.
                ProfilerDriver.ClearAllFrames();
            }
            LargestCapacity = Math.Max(LargestCapacity, capacity);
        }

        private static void LoadFile(string path)
        {
            if (!ProfilerDriver.LoadProfile(path, false))
                throw new InvalidOperationException("[Profiler] 원본을 불러오지 못했습니다: " + path);
        }

        public void Dispose()
        {
            _setLength.Invoke(null, new object[] { _previousLength });
        }
    }
}
