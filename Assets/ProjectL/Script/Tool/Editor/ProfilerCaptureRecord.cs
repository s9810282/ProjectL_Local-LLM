using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    /// <summary>원본 파일과 관측 범위를 남기는 진단 기록입니다. 게임 상태를 소유하지 않습니다.</summary>
    [Serializable]
    internal sealed class ProfilerCaptureRecord
    {
        /// <summary>첫 구간 파일입니다. 폴더 위치와 옛 기록(<c>capture.raw</c> 하나)의 원본을 가리킵니다.</summary>
        public string rawPath;
        public string startedUtc;
        public string stoppedUtc;
        public string status;
        public string stopReason;
        public bool startedBeforePlay;
        public int firstGameFrame = -1;
        public int lastGameFrame = -1;
        public int droppedFrameWarnings;
        // 기록을 나눈 게임 프레임 수와 구간 파일 목록입니다. 옛 기록은 비어 있고 rawPath 하나를 구간 하나로 봅니다.
        public int segmentFrames;
        public List<ProfilerCaptureSegment> segments = new List<ProfilerCaptureSegment>();
        // 출력 번호 기준 범위입니다. 구간을 이어 읽으면 구간 원본의 번호와 다를 수 있습니다(구간마다 outputOffset).
        public int loadedFirstFrame = -1;
        public int loadedLastFrame = -1;
        // 구간을 이어 읽은 결과입니다. 빈 번호·누락이 있으면 전체 기록으로 표시하지 않습니다.
        public int segmentGapFrames;
        // 번호가 이어지는데 두 구간 파일에 다 있던 프레임 수입니다. 한 번만 출력합니다.
        public int segmentOverlapFrames;
        public int renumberedSegments;
        public int missingSegments;
        public int importHistoryCapacity;
        // 불러오기 전 메모리 어림(ProfilerCaptureMemory)입니다. 남은 메모리를 읽지 못했거나 판정 전이면 -1입니다.
        public long loadNeedBytes;
        public long loadAvailableBytes = -1;
        public int exportedFrames;
        public int skippedFrames;
        public int actionPaths;
        public string exportFolder;
        public string exportStatus;
        public string unityVersion;

        // Editor update에서 실제로 관측한 두 지점 사이입니다. 전체 raw 프레임 수라고 주장하지 않습니다.
        internal int ObservedGameFrameSpan => firstGameFrame < 0 || lastGameFrame < firstGameFrame
            ? 0 : lastGameFrame - firstGameFrame;

        /// <summary>지금 기록 중이거나 마지막으로 기록한 구간 파일입니다.</summary>
        internal string ActivePath => segments == null || segments.Count == 0 ? rawPath : segments[segments.Count - 1].path;

        /// <summary>구간 파일이 둘 이상입니다. 한 파일만 불러오면 기록 일부만 보입니다.</summary>
        internal bool IsSegmented => segments != null && segments.Count > 1;

        internal string Json() => JsonUtility.ToJson(this, true);

        internal void Save()
        {
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(rawPath), "capture.json"), Json(), new UTF8Encoding(false));
        }
    }

    /// <summary>연속 기록의 구간 파일 하나입니다.</summary>
    [Serializable]
    internal sealed class ProfilerCaptureSegment
    {
        public string path;
        // 이 파일로 바꾼 순간의 게임 프레임입니다. Play 전에 시작한 첫 구간은 -1이며 기록의 firstGameFrame을 씁니다.
        public int firstGameFrame = -1;
        // 변환 때 이 파일에서 불러온 Profiler 번호와, 출력 번호로 옮긴 양(출력 = 불러온 번호 + outputOffset)입니다.
        public int loadedFirstFrame = -1;
        public int loadedLastFrame = -1;
        public int outputOffset;
        // 변환 결과: Loaded / Empty(불러왔지만 프레임 없음) / Missing(파일 없음·빈 파일) / Failed(불러오기 실패)
        // / SkippedMemory(남은 메모리 부족) / Cancelled입니다. 변환 전에는 비어 있습니다.
        public string loadStatus;
    }
}
