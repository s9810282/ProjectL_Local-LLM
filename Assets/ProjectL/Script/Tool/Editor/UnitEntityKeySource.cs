using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tool.Editor
{
    /// <summary>EntityKey의 기존 선언을 보존하며 명시적 정수 항목만 추가합니다.</summary>
    internal sealed class UnitEntityKeySource
    {
        internal const string AssetPath = "Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Ids/EntityKey.cs";
        private readonly byte[] _bytes;
        private readonly string _text;
        private readonly int _closing;
        private readonly bool _bom;
        internal readonly Dictionary<string, int> Entries = new();
        internal string Hash => HashBytes(_bytes);

        internal UnitEntityKeySource()
        {
            _bytes = File.ReadAllBytes(AssetPath);
            _bom = _bytes.Length >= 3 && _bytes[0] == 239 && _bytes[1] == 187 && _bytes[2] == 191;
            _text = new UTF8Encoding(false, true).GetString(_bytes, _bom ? 3 : 0, _bytes.Length - (_bom ? 3 : 0));
            // 주석 길이를 보존하여 원본의 삽입 위치와 일치시킵니다.
            string code = Regex.Replace(_text, @"//[^\r\n]*|/\*[\s\S]*?\*/", match =>
                new string(match.Value.Select(c => c == '\r' || c == '\n' ? c : ' ').ToArray()));
            var body = Regex.Match(code,
                @"\A\s*namespace\s+ProjectL\.Systems\.RuntimeSystem\.InGameSystem\.CoreSystem\.CoreCaculator\.Contracts\.Ids\s*\{\s*public\s+enum\s+EntityKey\s*\{(?<body>[^{}]*)\}\s*\}\s*\z");
            if (!body.Success)
                throw new InvalidOperationException("EntityKey.cs가 지원하는 단일 enum 형식이 아닙니다. 조건부 선언·추가 타입·기반 타입은 수동 확인이 필요합니다.");
            Group contents = body.Groups["body"];
            _closing = contents.Index + contents.Length;
            string remaining = contents.Value.Trim();
            if (remaining.Length == 0 || !remaining.EndsWith(",", StringComparison.Ordinal))
                throw new InvalidOperationException("EntityKey 항목은 명시적 정수와 끝 쉼표가 필요합니다.");
            foreach (string entry in remaining.Substring(0, remaining.Length - 1).Split(','))
            {
                Match declaration = Regex.Match(entry, @"\A\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*([0-9]+)\s*\z");
                if (!declaration.Success || !int.TryParse(declaration.Groups[2].Value, NumberStyles.None,
                        CultureInfo.InvariantCulture, out int value))
                    throw new InvalidOperationException($"지원하지 않는 EntityKey 선언입니다: {entry.Trim()}");
                string name = declaration.Groups[1].Value;
                if (Entries.ContainsKey(name) || Entries.ContainsValue(value))
                    throw new InvalidOperationException($"EntityKey 이름 또는 숫자가 중복입니다: {name} = {value}");
                Entries.Add(name, value);
            }
        }

        internal void Validate(string name, int value, bool add)
        {
            if (name == null || !Regex.IsMatch(name, @"\AUnit_[A-Za-z][A-Za-z0-9_]*\z") || value < 1)
                throw new InvalidOperationException("키 이름은 Unit_ 뒤 영문자로 시작하고 영문·숫자·밑줄만 사용해야 하며, 번호는 양수여야 합니다.");
            if (add)
            {
                if (Entries.ContainsKey(name) || Entries.ContainsValue(value) ||
                    Enum.GetNames(typeof(EntityKey)).Contains(name) || Enum.IsDefined(typeof(EntityKey), value))
                    throw new InvalidOperationException($"이미 사용한 EntityKey 이름 또는 번호입니다: {name} = {value}");
            }
            else if (!Entries.TryGetValue(name, out int existing) || existing != value)
            {
                throw new InvalidOperationException($"소스에 일치하는 EntityKey가 없습니다: {name} = {value}");
            }
        }

        internal byte[] AddedBytes(string name, int value)
        {
            Validate(name, value, true);
            string newline = _text.Contains("\r\n") ? "\r\n" : "\n";
            int lineStart = _text.LastIndexOf('\n', _closing - 1) + 1;
            bool ownLine = string.IsNullOrWhiteSpace(_text.Substring(lineStart, _closing - lineStart));
            int insertion = ownLine ? lineStart : _closing;
            string addition = (ownLine ? "" : newline) + "        " + name + " = " +
                value.ToString(CultureInfo.InvariantCulture) + "," + newline;
            string updated = _text.Insert(insertion, addition);
            var encoding = new UTF8Encoding(_bom);
            return encoding.GetPreamble().Concat(encoding.GetBytes(updated)).ToArray();
        }

        /// <summary>항목 한 줄(이름 = 번호,)을 지운 내용입니다. 다른 줄·주석·줄바꿈은 그대로 둡니다.</summary>
        internal byte[] RemovedBytes(string name, int value)
        {
            Validate(name, value, false);
            var line = new Regex(@"^[ \t]*" + Regex.Escape(name) + @"[ \t]*=[ \t]*" + value.ToString(CultureInfo.InvariantCulture) + @"[ \t]*,[ \t]*\r?\n", RegexOptions.Multiline);
            MatchCollection matches = line.Matches(_text);
            if (matches.Count != 1)
                throw new InvalidOperationException($"EntityKey.cs에서 지울 줄을 하나로 찾지 못했습니다: {name} = {value}");
            string updated = _text.Remove(matches[0].Index, matches[0].Length);
            var encoding = new UTF8Encoding(_bom);
            return encoding.GetPreamble().Concat(encoding.GetBytes(updated)).ToArray();
        }

        internal static string BackupPath(string requestId)
        {
            if (!Guid.TryParseExact(requestId, "N", out _)) throw new ArgumentException("작업 ID가 필요합니다.", nameof(requestId));
            return "Library/ProjectLUnitAuthoring/backups/" + requestId + "/EntityKey.cs.original";
        }

        internal void Write(byte[] updated, string requestId)
        {
            if (updated == null) throw new ArgumentNullException(nameof(updated));
            string backup = BackupPath(requestId);
            Directory.CreateDirectory(Path.GetDirectoryName(backup));
            if (File.Exists(backup))
            {
                if (HashBytes(File.ReadAllBytes(backup)) != Hash)
                    throw new InvalidOperationException($"기존 백업과 현재 원본이 다릅니다. 새 요청으로 다시 확인해 주세요: {backup}");
            }
            else WriteDurable(backup, _bytes);

            // 원본과 같은 볼륨의 숨김 임시 파일을 완성한 후 교체합니다. 원본에는 직접 쓰지 않습니다.
            string temporary = Path.Combine(Path.GetDirectoryName(AssetPath), ".EntityKey." + requestId + "." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                WriteDurable(temporary, updated);
                // 교체 직전에 내용을 다시 비교합니다. 원본을 연 채로 교체하면 Windows가
                // "바꿀 파일을 제거할 수 없습니다"로 거절하므로, 비교한 뒤 닫고 교체합니다.
                if (HashBytes(File.ReadAllBytes(AssetPath)) != Hash)
                    throw new InvalidOperationException("EntityKey.cs가 다른 작업에서 변경됐습니다. 다시 확인해 주세요.");
                UnitAuthoringPendingStore.ReplaceFile(temporary, AssetPath);
                if (HashBytes(File.ReadAllBytes(AssetPath)) != HashBytes(updated))
                    throw new IOException($"EntityKey.cs 교체 결과가 다릅니다. 원본 백업: {backup}");
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }

        private static void WriteDurable(string path, byte[] bytes)
        {
            using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush(true);
        }

        internal static bool IsLoaded(string name, int value) =>
            Enum.IsDefined(typeof(EntityKey), value) && Enum.GetName(typeof(EntityKey), value) == name;

        internal static string HashBytes(byte[] bytes)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "");
        }
    }
}
