using System;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field
{
    /// <summary>
    /// 궤적 경로를 같은 길이 구간으로 다시 나눕니다. "구간 하나 = 점 하나" 점선 규칙의 원본입니다(공격 궤적 설계 TJ7).
    /// </summary>
    /// <remarks>
    /// 지금 MultiLine UV는 길이 방향이 구간마다 0→1이라, 셰이더가 구간 하나에 점 하나를 찍습니다(<c>_DashCount = 1</c>).
    /// 구간 길이가 모두 같아야 점 간격이 고릅니다. 곡선은 호 길이 기준으로 나누므로 구간(현) 길이 차이는 아주 작습니다.
    /// 할당하지 않습니다. 출력 배열은 부르는 쪽이 길이 <c>구간 수 + 1</c>로 준비합니다.
    /// </remarks>
    internal static class FieldTrajectorySegments
    {
        /// <summary>구간 수 상한입니다. 필드 대각선을 가장 짧은 간격으로 나눠도 넘지 않는 값입니다.</summary>
        internal const int MaxSegments = 256;

        /// <summary>점 배열 앞쪽 <paramref name="count"/>개를 이은 꺾은선 길이입니다.</summary>
        internal static float Length(Vector3[] path, int count)
        {
            ValidatePath(path, count);
            float length = 0f;
            for (int i = 1; i < count; i++)
                length += Vector3.Distance(path[i - 1], path[i]);
            return length;
        }

        /// <summary>구간 수 = max(1, round(길이 / 간격))이며 <see cref="MaxSegments"/>를 넘지 않습니다.</summary>
        internal static int SegmentCount(float length, float spacing)
        {
            if (!float.IsFinite(spacing) || spacing <= 0f)
                throw new ArgumentOutOfRangeException(nameof(spacing), "점 간격은 유한한 양수여야 합니다.");
            if (!float.IsFinite(length) || length < 0f)
                throw new ArgumentOutOfRangeException(nameof(length), "경로 길이는 유한한 0 이상이어야 합니다.");
            return Mathf.Clamp(Mathf.RoundToInt(length / spacing), 1, MaxSegments);
        }

        /// <summary>
        /// 경로를 호 길이 기준 같은 간격의 점 <paramref name="segments"/> + 1개로 다시 나눠 <paramref name="output"/>에 채웁니다.
        /// 양 끝은 경로의 양 끝 그대로입니다.
        /// </summary>
        /// <param name="length">경로 길이(<see cref="Length"/>)입니다. 양수여야 합니다.</param>
        internal static void Resample(Vector3[] path, int count, float length, int segments, Vector3[] output)
        {
            ValidatePath(path, count);
            if (count < 2)
                throw new ArgumentOutOfRangeException(nameof(count), "다시 나눌 경로는 점이 둘 이상이어야 합니다.");
            if (!float.IsFinite(length) || length <= 0f)
                throw new ArgumentOutOfRangeException(nameof(length), "다시 나눌 경로 길이는 유한한 양수여야 합니다.");
            if (segments < 1 || segments > MaxSegments)
                throw new ArgumentOutOfRangeException(nameof(segments));
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            if (output.Length != segments + 1)
                throw new ArgumentException("출력 배열 길이는 구간 수 + 1이어야 합니다.", nameof(output));

            output[0] = path[0];
            output[segments] = path[count - 1];
            float step = length / segments;
            int end = 1;
            float walked = 0f;
            float piece = Vector3.Distance(path[0], path[1]);
            for (int k = 1; k < segments; k++)
            {
                float target = step * k;
                while (end < count - 1 && walked + piece < target)
                {
                    walked += piece;
                    end++;
                    piece = Vector3.Distance(path[end - 1], path[end]);
                }
                float local = piece > 0f ? Mathf.Clamp01((target - walked) / piece) : 0f;
                output[k] = Vector3.Lerp(path[end - 1], path[end], local);
            }
        }

        private static void ValidatePath(Vector3[] path, int count)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (count < 0 || count > path.Length)
                throw new ArgumentOutOfRangeException(nameof(count));
        }
    }
}
