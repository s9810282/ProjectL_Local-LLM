using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    internal enum CombatStageSoDraftStatus { Empty, Success, Conflict, Failed }

    internal readonly struct CombatStageSoDraftResult
    {
        public readonly CombatStageSoDraftStatus Status;
        public readonly string Raw;
        public readonly string Message;
        public readonly bool RollbackFailed;
        public bool Succeeded => Status == CombatStageSoDraftStatus.Success || Status == CombatStageSoDraftStatus.Empty;

        public CombatStageSoDraftResult(CombatStageSoDraftStatus status, string raw = null, string message = null, bool rollbackFailed = false)
        {
            Status = status; Raw = raw; Message = message; RollbackFailed = rollbackFailed;
        }
    }

    [Serializable]
    internal sealed class CombatStageSoProtectedDraft
    {
        public int envelopeFormat;
        public string raw, storedUtc, reason, source;
        public int originalFormat;
        public string rootGuid;
    }

    // 원문을 변환하지 않고 보관합니다. 에셋이나 편집 중인 작업본에는 접근하지 않습니다.
    internal static class CombatStageSoDraftStore
    {
        [Serializable]
        private sealed class Header
        {
            public int format;
            public string rootGuid;
        }

        public static string RecoveryKey(string activeKey) => activeKey + ".Recovery";

        public static CombatStageSoDraftResult ReadRaw(string key)
        {
            try
            {
                return EditorPrefs.HasKey(key)
                    ? new CombatStageSoDraftResult(CombatStageSoDraftStatus.Success, EditorPrefs.GetString(key))
                    : new CombatStageSoDraftResult(CombatStageSoDraftStatus.Empty);
            }
            catch (Exception exception) { return Failed(exception.Message); }
        }

        public static CombatStageSoDraftResult WriteVerified(string key, string raw)
        {
            if (raw == null) return Failed("보관할 원문이 없습니다.");
            try
            {
                EditorPrefs.SetString(key, raw);
                var read = ReadRaw(key);
                return read.Status == CombatStageSoDraftStatus.Success && read.Raw == raw
                    ? read : Failed("초안을 쓴 뒤 원문 일치를 확인하지 못했습니다. " + read.Message, raw);
            }
            catch (Exception exception) { return Failed(exception.Message, raw); }
        }

        public static CombatStageSoDraftResult ClearIfMatches(string key, string expectedRaw)
        {
            var read = ReadRaw(key);
            if (!read.Succeeded) return read;
            if (read.Status == CombatStageSoDraftStatus.Empty) return read;
            if (read.Raw != expectedRaw) return Conflict(read.Raw);
            try
            {
                EditorPrefs.DeleteKey(key);
                var after = ReadRaw(key);
                return after.Status == CombatStageSoDraftStatus.Empty
                    ? after : Failed("초안 정리를 확인하지 못했습니다. " + after.Message, expectedRaw);
            }
            catch (Exception exception) { return Failed(exception.Message, expectedRaw); }
        }

        // 이전 원문이 아직 같은 경우만 바꿉니다. 실패 뒤 복구에도 실패하면 호출자가 원문을 계속 보유합니다.
        public static CombatStageSoDraftResult ReplaceVerified(string key, string expectedRaw, string nextRaw)
        {
            var read = ReadRaw(key);
            if (!read.Succeeded) return read;
            if (read.Raw != expectedRaw) return Conflict(read.Raw);
            var written = nextRaw == null ? ClearIfMatches(key, expectedRaw) : WriteVerified(key, nextRaw);
            if (written.Succeeded) return written;
            var current = ReadRaw(key);
            CombatStageSoDraftResult rollback;
            if (!current.Succeeded) rollback = current;
            else if (current.Raw == expectedRaw) rollback = current;
            else if (current.Raw != nextRaw && current.Raw != null)
                rollback = Conflict(current.Raw);
            else rollback = expectedRaw == null ? ClearIfMatches(key, current.Raw) : WriteVerified(key, expectedRaw);
            return new CombatStageSoDraftResult(CombatStageSoDraftStatus.Failed, expectedRaw,
                written.Message + (rollback.Succeeded ? " 이전 원문은 유지했습니다." : " 이전 원문 복구도 실패했습니다: " + rollback.Message), !rollback.Succeeded);
        }

        public static bool TryReadProtected(string raw, out CombatStageSoProtectedDraft draft)
        {
            draft = null;
            if (string.IsNullOrEmpty(raw)) return false;
            try
            {
                draft = JsonUtility.FromJson<CombatStageSoProtectedDraft>(raw);
                return draft != null && draft.envelopeFormat == 1 && draft.raw != null
                    && !string.IsNullOrEmpty(draft.storedUtc) && !string.IsNullOrEmpty(draft.source);
            }
            catch (Exception) { return false; }
        }

        public static CombatStageSoDraftResult ProtectRaw(string activeKey, string raw, string reason, string source)
        {
            if (raw == null) return Failed("보호할 원문이 없습니다.");
            string recoveryKey = RecoveryKey(activeKey);
            var saved = ReadRaw(recoveryKey);
            if (!saved.Succeeded) return saved;
            string expectedEnvelope = saved.Raw;
            if (saved.Status != CombatStageSoDraftStatus.Empty)
            {
                if (!TryReadProtected(saved.Raw, out var existing) || existing.raw != raw)
                    return Conflict(saved.Raw, "다른 원문 또는 읽을 수 없는 보호 봉투가 이미 있습니다. 대상별 내보내기·폐기 후 재시도하세요.");
            }
            else
            {
                var envelope = new CombatStageSoProtectedDraft
                {
                    envelopeFormat = 1, raw = raw, storedUtc = DateTime.UtcNow.ToString("O"), reason = reason, source = source
                };
                try
                {
                    var header = JsonUtility.FromJson<Header>(raw);
                    if (header != null) { envelope.originalFormat = header.format; envelope.rootGuid = header.rootGuid; }
                }
                catch (Exception) { /* 손상 원문도 그대로 보관합니다. */ }
                expectedEnvelope = JsonUtility.ToJson(envelope);
                var write = ReplaceVerified(recoveryKey, null, expectedEnvelope);
                if (!write.Succeeded) return write;
            }
            var verified = ReadRaw(recoveryKey);
            if (!verified.Succeeded) return verified;
            if (verified.Raw != expectedEnvelope) return Conflict(verified.Raw, "보호 원문 확인 중 대상이 바뀌었습니다. 활성 원문은 정리하지 않았습니다.");
            // 보호본을 확인한 뒤에만 같은 활성 원문을 지웁니다. 메모리 사본은 활성 키가 없을 수 있습니다.
            var active = ReadRaw(activeKey);
            if (!active.Succeeded) return active;
            if (active.Status == CombatStageSoDraftStatus.Empty) return new CombatStageSoDraftResult(CombatStageSoDraftStatus.Success, raw);
            var cleared = ClearIfMatches(activeKey, raw);
            return cleared.Succeeded ? new CombatStageSoDraftResult(CombatStageSoDraftStatus.Success, raw) : cleared;
        }

        public static CombatStageSoDraftResult ExportRaw(string path, string raw)
        {
            if (string.IsNullOrEmpty(path)) return new CombatStageSoDraftResult(CombatStageSoDraftStatus.Empty);
            if (raw == null) return Failed("내보낼 원문이 없습니다.");
            try
            {
                File.WriteAllText(path, raw, new UTF8Encoding(false));
                return File.ReadAllText(path, Encoding.UTF8) == raw
                    ? new CombatStageSoDraftResult(CombatStageSoDraftStatus.Success, raw)
                    : Failed("내보낸 파일의 원문 일치를 확인하지 못했습니다.", raw);
            }
            catch (Exception exception) { return Failed(exception.Message, raw); }
        }

        private static CombatStageSoDraftResult Failed(string message, string raw = null) =>
            new CombatStageSoDraftResult(CombatStageSoDraftStatus.Failed, raw, message);

        private static CombatStageSoDraftResult Conflict(string raw, string message = "확인한 뒤 초안 원문이 바뀌었습니다. 다시 확인하세요.") =>
            new CombatStageSoDraftResult(CombatStageSoDraftStatus.Conflict, raw, message);
    }
}
