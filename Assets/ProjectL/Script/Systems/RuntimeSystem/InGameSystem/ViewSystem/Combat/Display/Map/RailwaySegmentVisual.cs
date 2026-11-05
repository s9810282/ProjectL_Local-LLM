using System;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map
{
    /// <summary>설치된 선로 한 조각입니다. 선택 사항인 바닥이 있으면 레일과 같은 등장 시각을 사용합니다.</summary>
    internal readonly struct RailwaySegmentVisual
    {
        internal RailwaySegmentVisual(Transform tile, Transform rail)
        {
            Tile = tile;
            Rail = rail != null ? rail : throw new ArgumentNullException(nameof(rail));
        }

        /// <summary>바닥 프리팹을 지정하지 않았으면 null입니다.</summary>
        internal Transform Tile { get; }
        internal Transform Rail { get; }
    }
}
