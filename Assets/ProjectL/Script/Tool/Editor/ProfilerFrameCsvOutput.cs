using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace ProjectL.Tool.Editor
{
    /// <summary>프레임 경로를 순차 기록하고, 다 쓴 내보내기 폴더만 확정합니다.</summary>
    internal sealed class ProfilerFrameCsvOutput : IDisposable
    {
        internal const string ActionPrefix = "Combat.Action.";
        private static readonly Encoding _utf8 = new UTF8Encoding(false);
        private static readonly char[] _csvSpecial = { ',', '"', '\n', '\r' };
        private readonly string _root;
        private readonly string _temporary;
        private readonly string _destination;
        private StreamWriter _frames;
        private StreamWriter _actions;
        private bool _committed;
        private bool _readyToCommit;

        internal ProfilerFrameCsvOutput(string root)
        {
            _root = Path.GetFullPath(root);
            string token = Guid.NewGuid().ToString("N");
            _temporary = Path.Combine(_root, ".partial-" + token);
            _destination = Path.Combine(_root, DateTime.Now.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture) + "-" + token.Substring(0, 8));
            Directory.CreateDirectory(_temporary);
            try
            {
                _frames = new StreamWriter(Path.Combine(_temporary, "frame_markers.csv"), false, _utf8);
                _actions = new StreamWriter(Path.Combine(_temporary, "actions.csv"), false, _utf8);
                _frames.WriteLine("frame,path,depth,name,total_ms,self_ms,calls,gc_bytes");
                _actions.WriteLine("frame,path,name,category,action,phase,total_ms,self_ms,calls,gc_bytes");
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal void Write(ProfilerFrameScan.Sample sample)
        {
            _frames.WriteLine(string.Join(",", sample.Frame.ToString(CultureInfo.InvariantCulture), Csv(sample.Path),
                sample.Depth.ToString(CultureInfo.InvariantCulture), Csv(sample.Name), Number(sample.Total), Number(sample.Self),
                Number(sample.Calls), Number(sample.Gc)));
            if (!IsAction(sample.Name))
                return;

            string suffix = sample.Name.Substring(ActionPrefix.Length);
            int first = suffix.IndexOf('.');
            int last = suffix.LastIndexOf('.');
            bool parsed = first > 0 && last > first && last < suffix.Length - 1;
            string category = parsed ? suffix.Substring(0, first) : string.Empty;
            string action = parsed ? suffix.Substring(first + 1, last - first - 1) : suffix;
            string phase = parsed ? suffix.Substring(last + 1) : string.Empty;
            _actions.WriteLine(string.Join(",", sample.Frame.ToString(CultureInfo.InvariantCulture), Csv(sample.Path), Csv(sample.Name),
                Csv(category), Csv(action), Csv(phase), Number(sample.Total), Number(sample.Self), Number(sample.Calls), Number(sample.Gc)));
        }

        internal string Destination => _destination;

        internal string Commit(string frames, string markers, string summary, string captureJson = null)
        {
            CloseWriters();
            File.WriteAllText(Path.Combine(_temporary, "frames.csv"), frames, _utf8);
            File.WriteAllText(Path.Combine(_temporary, "markers.csv"), markers, _utf8);
            File.WriteAllText(Path.Combine(_temporary, "summary.md"), summary, _utf8);
            if (captureJson != null)
                File.WriteAllText(Path.Combine(_temporary, "capture.json"), captureJson, _utf8);
            // 완성 파일은 폴더 확정에 실패해도 보존합니다. 외부 파일 감시기의 일시 잠금을 재시도합니다.
            _readyToCommit = true;
            for (int attempt = 0; ; attempt++)
            {
                try
                {
                    Directory.Move(_temporary, _destination);
                    break;
                }
                catch (IOException) when (attempt < 5 && Directory.Exists(_temporary) && !Directory.Exists(_destination))
                {
                    Thread.Sleep(100);
                }
                catch (UnauthorizedAccessException) when (attempt < 5 && Directory.Exists(_temporary) && !Directory.Exists(_destination))
                {
                    Thread.Sleep(100);
                }
            }
            _committed = true;
            return _destination;
        }

        public void Dispose()
        {
            try
            {
                CloseWriters();
            }
            finally
            {
                // 이 출력기가 만든 루트 바로 아래의 고유 임시 폴더만 지웁니다.
                if (!_committed && !_readyToCommit && Directory.Exists(_temporary)
                    && string.Equals(Path.GetDirectoryName(_temporary), _root, StringComparison.OrdinalIgnoreCase)
                    && Path.GetFileName(_temporary).StartsWith(".partial-", StringComparison.Ordinal))
                    Directory.Delete(_temporary, true);
            }
        }

        private void CloseWriters()
        {
            try
            {
                _frames?.Dispose();
            }
            finally
            {
                _frames = null;
                try
                {
                    _actions?.Dispose();
                }
                finally
                {
                    _actions = null;
                }
            }
        }

        internal static bool IsAction(string name) => name.StartsWith(ActionPrefix, StringComparison.Ordinal);
        internal static string Number(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);
        internal static string Csv(string value) =>
            value.IndexOfAny(_csvSpecial) < 0 ? value : "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
