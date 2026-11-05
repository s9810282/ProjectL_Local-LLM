using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ProjectL.Systems.RuntimeSystem.Common.Descriptions
{
    /// <summary>
    /// 설명 틀을 한 번 검사하여 리터럴과 수치 읽기로 나눕니다. DB·게임 규칙·화면 상태는 보관하지 않습니다.
    /// 숫자 형식은 문화권에 영향을 받지 않으며, 단위·부호·서식 태그는 원문을 보존합니다.
    /// </summary>
    internal sealed class DescriptionTemplate<T> where T : class
    {
        private readonly string _context;
        private readonly Part[] _parts;
        private readonly string _constant;
        private readonly string _firstTokenName;

        internal DescriptionTemplate(string context, string text, params DescriptionToken<T>[] tokens)
        {
            if (string.IsNullOrWhiteSpace(context)) throw new ArgumentException("설명 종류·키·필드가 필요합니다.", nameof(context));
            if (tokens == null) throw new ArgumentNullException(nameof(tokens));
            _context = context;
            var bindings = new Dictionary<string, DescriptionToken<T>>(StringComparer.Ordinal);
            foreach (DescriptionToken<T> token in tokens)
            {
                if (token == null) throw Invalid("토큰 등록이 비었습니다.");
                foreach (string name in token.Names)
                    if (!bindings.TryAdd(name, token)) throw Invalid($"토큰 '{{{name}}}'이 중복 등록됐습니다.");
            }

            text ??= string.Empty;
            var literal = new StringBuilder(text.Length);
            var parts = new List<Part>();
            for (int i = 0; i < text.Length; i++)
            {
                char current = text[i];
                if (current != '{' && current != '}')
                {
                    literal.Append(current);
                    continue;
                }
                if (i + 1 < text.Length && text[i + 1] == current)
                {
                    literal.Append(current);
                    i++;
                    continue;
                }
                if (current == '}') throw Invalid($"닫는 중괄호의 짝이 없습니다. 위치: {i}.");

                int end = text.IndexOf('}', i + 1);
                if (end < 0) throw Invalid($"여는 중괄호의 짝이 없습니다. 위치: {i}.");
                string name = text.Substring(i + 1, end - i - 1);
                if (!DescriptionToken<T>.IsValidName(name))
                    throw Invalid($"토큰 '{{{name}}}'의 이름이 잘못됐습니다. 계산식·서식 지정자·숫자 인덱스는 지원하지 않습니다.");
                if (!bindings.TryGetValue(name, out DescriptionToken<T> binding))
                    throw Invalid($"토큰 '{{{name}}}'이 등록되지 않았습니다.");
                if (literal.Length > 0)
                {
                    parts.Add(new Part(literal.ToString(), null));
                    literal.Clear();
                }
                parts.Add(new Part(name, binding));
                if (!RequiresValues) _firstTokenName = name;
                RequiresValues = true;
                i = end;
            }
            if (literal.Length > 0) parts.Add(new Part(literal.ToString(), null));
            _parts = parts.ToArray();
            _constant = RequiresValues ? null : literal.ToString();
        }

        internal bool RequiresValues { get; }

        /// <summary>수치가 없는 설명은 원본 없이도 사용할 수 있습니다. 빈 설명도 정상 입력입니다.</summary>
        internal string Format(T source)
        {
            if (!RequiresValues) return _constant;
            if (source == null) throw Invalid($"토큰 '{{{_firstTokenName}}}'을 채울 공개 원본이 없습니다.");
            var result = new StringBuilder();
            foreach (Part part in _parts)
            {
                if (part.Token == null)
                {
                    result.Append(part.Text);
                    continue;
                }
                try
                {
                    result.Append(part.Token.Read(source).ToString(CultureInfo.InvariantCulture));
                }
                catch (InvalidOperationException error)
                {
                    throw new InvalidOperationException($"[DescriptionTemplate] {_context}: 토큰 '{{{part.Text}}}'의 공개값을 읽을 수 없습니다.", error);
                }
            }
            return result.ToString();
        }

        private InvalidOperationException Invalid(string message) =>
            new InvalidOperationException($"[DescriptionTemplate] {_context}: {message}");

        private readonly struct Part
        {
            internal Part(string text, DescriptionToken<T> token)
            {
                Text = text;
                Token = token;
            }

            internal string Text { get; }
            internal DescriptionToken<T> Token { get; }
        }
    }
}
