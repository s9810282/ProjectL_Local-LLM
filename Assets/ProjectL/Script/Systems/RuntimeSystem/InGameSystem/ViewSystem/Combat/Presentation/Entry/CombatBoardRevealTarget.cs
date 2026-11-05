using System;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Entry
{
    /// <summary>실제 외형과 같은 박자를 따라갈 타일입니다. 타일이 없으면 자신의 필드 깊이를 씁니다.</summary>
    internal readonly struct CombatBoardRevealTarget
    {
        internal CombatBoardRevealTarget(Transform body, Transform tile)
        {
            Body = body != null ? body : throw new ArgumentNullException(nameof(body));
            Tile = tile;
        }

        internal Transform Body { get; }
        internal Transform Tile { get; }
    }
}
