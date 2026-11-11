using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    /// <summary>재컴파일을 넘겨 보관하는 생성 요청입니다. 게임 데이터로 사용하지 않습니다.</summary>
    [Serializable]
    internal sealed class UnitAuthoringRequest
    {
        /// <summary>기록 형식입니다. 0(옛 기록)은 읽을 때 올립니다(계획 P2).</summary>
        public int version;
        public string id;
        public string phase;
        public string error;
        public string unitSource;
        public string visualSource;
        public string unitFingerprint;
        public string visualFingerprint;
        public string unitTable;
        public string visualTable;
        public string keyName;
        public int keyValue;
        public bool addKey;
        public string displayName;
        public string description;
        /// <summary>원본 Unit의 진영 자격 enum 순번입니다. -1이면 원본 그대로입니다.</summary>
        public int eligibilityIndex = -1;
        public string[] rosters = Array.Empty<string>();
        public bool[] rosterRegistered = Array.Empty<bool>();
        public bool sourceChanged;
        public bool resultOpened;
        public string unitPath;
        public string visualPath;
        public string sourceBefore;
        public string sourceAfter;
        public string sourceBackup;
        public string createdUnit;
        public string createdVisual;
        public bool unitRegistered;
        public bool visualRegistered;
        public bool cancelRequested;
        public bool compilationRequested;
        public bool compilationStarted;
        public bool compilationFinished;
        public bool compilationHadErrors;
        public string compilationDomain;
        public double compilationRequestedAt;
        public double compilationFinishedAt;
    }

    /// <summary>프로젝트 Library의 단일 진행 요청을 원자적으로 교체합니다.</summary>
    internal static class UnitAuthoringPendingStore
    {
        internal const int CurrentVersion = 2;
        internal const string FilePath = "Library/ProjectLUnitAuthoring/request.json";

        internal static UnitAuthoringRequest Load()
        {
            if (!File.Exists(FilePath)) return null;
            var request = JsonUtility.FromJson<UnitAuthoringRequest>(File.ReadAllText(FilePath, Encoding.UTF8));
            if (request == null || string.IsNullOrEmpty(request.id) || string.IsNullOrEmpty(request.phase))
                throw new InvalidOperationException($"유닛 생성 작업 기록을 읽을 수 없습니다: {FilePath}");
            if (request.version < CurrentVersion)
            {
                // 옛 기록은 로스터 없이 진행하고, 이미 끝난 요청은 다시 열지 않습니다(계획 P2).
                request.rosters = Array.Empty<string>();
                request.rosterRegistered = Array.Empty<bool>();
                request.eligibilityIndex = -1;
                request.resultOpened = request.phase == UnitAuthoringOperation.Complete || request.phase == UnitAuthoringOperation.Cancelled;
                request.version = CurrentVersion;
            }
            request.rosters ??= Array.Empty<string>();
            if (request.rosterRegistered == null || request.rosterRegistered.Length != request.rosters.Length)
                Array.Resize(ref request.rosterRegistered, request.rosters.Length);
            return request;
        }

        /// <summary>기록 파일만 지웁니다. 생성물·enum 백업 폴더는 건드리지 않습니다.</summary>
        internal static void Discard()
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
        }

        internal static void Save(UnitAuthoringRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            request.version = CurrentVersion;
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            string temporary = FilePath + ".tmp";
            byte[] bytes = new UTF8Encoding(false).GetBytes(JsonUtility.ToJson(request, true));
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            ReplaceFile(temporary, FilePath);
        }

        /// <summary>
        /// 임시 파일로 대상을 바꿉니다. 에디터·IDE·백신이 대상을 잠깐 잡고 있으면 Windows가
        /// "바꿀 파일을 제거할 수 없습니다"로 거절하므로 몇 번 기다렸다 다시 시도하고, 끝까지 안 되면 덮어쓰기로 바꿉니다.
        /// </summary>
        internal static void ReplaceFile(string temporary, string destination)
        {
            const int attempts = 5;
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    if (File.Exists(destination)) File.Replace(temporary, destination, null);
                    else File.Move(temporary, destination);
                    return;
                }
                catch (Exception exception) when ((exception is IOException || exception is UnauthorizedAccessException) && attempt < attempts)
                {
                    System.Threading.Thread.Sleep(50 * attempt);
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
                {
                    File.Copy(temporary, destination, true);
                    File.Delete(temporary);
                    return;
                }
            }
        }
    }
}
