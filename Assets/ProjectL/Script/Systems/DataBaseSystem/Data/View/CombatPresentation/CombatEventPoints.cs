using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 연결 때 복사한 이벤트 지점 목록입니다(클립 칸 다시 짜기 설계 CS4). 번호 = <see cref="CombatAnimationEvents"/>의 Event Points 목록 칸입니다.
    /// 쓰는 쪽은 둘입니다: 원거리 이벤트 줄의 발사 지점, 이펙트 큐의 <see cref="CombatCuePoint.ActorEventPoint"/>. 번호 해석은 이 값 한 곳입니다.
    /// </summary>
    internal sealed class CombatEventPoints
    {
        private readonly Transform[] _points;

        internal CombatEventPoints(IReadOnlyList<Transform> points)
        {
            int count = points?.Count ?? 0;
            _points = new Transform[count];
            for (int i = 0; i < count; i++)
                _points[i] = points[i];
        }

        /// <summary>목록 칸 수입니다(빈 칸 포함).</summary>
        internal int Count => _points.Length;

        /// <summary>그 번호의 지점입니다. 목록 밖이거나 칸이 비었으면 false입니다.</summary>
        /// <exception cref="ArgumentOutOfRangeException">음수 번호일 때입니다.</exception>
        internal bool TryGet(int point, out Transform transform)
        {
            if (point < 0) throw new ArgumentOutOfRangeException(nameof(point), point, "이벤트 지점 번호는 0 이상이어야 합니다.");
            transform = point < _points.Length ? _points[point] : null;
            return transform != null;
        }
    }
}
