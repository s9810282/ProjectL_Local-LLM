using System;
using System.Collections.Generic;

namespace ProjectL.Systems.RuntimeSystem.Common.Descriptions
{
    /// <summary>설명 한 종류에서 허용하는 토큰 이름과 공개 수치 읽기를 함께 정의합니다.</summary>
    internal sealed class DescriptionToken<T> where T : class
    {
        private readonly Func<T, decimal> _read;
        private readonly string[] _names;

        internal DescriptionToken(string name, Func<T, decimal> read, params string[] aliases)
        {
            if (!IsValidName(name))
                throw new ArgumentException("설명 토큰은 문자 또는 밑줄로 시작하는 이름이어야 합니다.", nameof(name));
            _read = read ?? throw new ArgumentNullException(nameof(read));
            if (aliases == null) throw new ArgumentNullException(nameof(aliases));
            _names = new string[aliases.Length + 1];
            _names[0] = name;
            var seen = new HashSet<string>(StringComparer.Ordinal) { name };
            for (int i = 0; i < aliases.Length; i++)
            {
                if (!IsValidName(aliases[i]) || !seen.Add(aliases[i]))
                    throw new ArgumentException($"설명 토큰 '{name}'의 별칭이 잘못되거나 중복됐습니다: {aliases[i]}", nameof(aliases));
                _names[i + 1] = aliases[i];
            }
        }

        internal IReadOnlyList<string> Names => Array.AsReadOnly(_names);
        internal decimal Read(T source) => _read(source);

        /// <summary>여러 사실 형식을 받는 경계에서도 이 토큰이 요구하는 실제 형식을 검사합니다.</summary>
        internal static DescriptionToken<T> From<TSource>(string name, Func<TSource, decimal> read, params string[] aliases)
            where TSource : T
        {
            if (read == null) throw new ArgumentNullException(nameof(read));
            return new DescriptionToken<T>(name, source => source is TSource typed
                ? read(typed)
                : throw new InvalidOperationException($"필요한 설명 원본은 {typeof(TSource).Name}이고 실제 원본은 {source?.GetType().Name ?? "null"}입니다."), aliases);
        }

        internal static bool IsValidName(string name)
        {
            if (string.IsNullOrEmpty(name) || !(char.IsLetter(name[0]) || name[0] == '_')) return false;
            for (int i = 1; i < name.Length; i++)
                if (!(char.IsLetterOrDigit(name[i]) || name[i] == '_')) return false;
            return true;
        }
    }
}
