using System;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field
{
    /// <summary>
    /// 궤적 선 하나의 경로입니다(궤적 대상별 선 설계 PT5). 점은 필드 셀 루트 로컬 좌표이고, 배열 앞쪽 <see cref="Count"/>개를 씁니다.
    /// 배열은 부르는 쪽이 다시 쓰는 버퍼이며 복사하지 않습니다.
    /// </summary>
    internal readonly struct FieldTrajectoryPath
    {
        internal FieldTrajectoryPath(Vector3[] points, int count)
        {
            Points = points ?? throw new ArgumentNullException(nameof(points));
            if (count < 0 || count > points.Length)
                throw new ArgumentOutOfRangeException(nameof(count));
            Count = count;
        }

        internal Vector3[] Points { get; }
        internal int Count { get; }
    }
}
