using System;
using System.Collections.Generic;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 열거 값이 정의된 값인지 박싱 없이 봅니다(GC 끊김 설계 GH4). <c>System.Enum.IsDefined(Type, object)</c>는 부를 때마다 값을 박싱하므로,
    /// 조각·조립·Gate처럼 사건마다 도는 검사는 이 도우미를 씁니다.
    /// </summary>
    /// <remarks>int 바탕 열거만 받습니다. 부르는 쪽이 값을 <c>(int)</c>로 바꿔 넘깁니다. 정의된 값 표는 형식마다 처음 한 번 만듭니다.</remarks>
    internal static class CombatEnumValues
    {
        /// <summary><paramref name="value"/>가 <typeparamref name="T"/>에 정의된 값인지입니다.</summary>
        internal static bool IsDefined<T>(int value) where T : struct, System.Enum => Table<T>.Values.Contains(value);

        private static class Table<T> where T : struct, System.Enum
        {
            internal static readonly HashSet<int> Values = Build();

            private static HashSet<int> Build()
            {
                if (System.Enum.GetUnderlyingType(typeof(T)) != typeof(int))
                    throw new InvalidOperationException($"[CombatEnumValues] {typeof(T).Name}은(는) int 바탕 열거가 아닙니다.");
                var values = new HashSet<int>();
                foreach (T value in (T[])System.Enum.GetValues(typeof(T)))
                    values.Add(Convert.ToInt32(value));
                return values;
            }
        }
    }
}
