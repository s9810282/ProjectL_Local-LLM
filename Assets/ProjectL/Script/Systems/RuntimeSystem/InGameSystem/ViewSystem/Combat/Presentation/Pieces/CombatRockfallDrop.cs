using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 돌 하나가 떨어지는 모습의 값입니다(낙석 View 설계 RG4·RG5). <see cref="CombatRockfallDrops"/>가 조립할 때 한 번 만들고, 낙하 조각은 그리기만 합니다.
    /// </summary>
    internal readonly struct CombatRockfallDrop
    {
        /// <summary>돌이 닿을 칸 표면입니다.</summary>
        internal Vector3 Landing { get; }

        /// <summary>출발 자리가 칸에서 옆으로 떨어진 변위입니다. 0이면 곧게 떨어집니다.</summary>
        internal Vector3 Side { get; }

        /// <summary>출발 자리가 칸에서 위로 떨어진 변위입니다.</summary>
        internal Vector3 Rise { get; }

        /// <summary>프리팹 크기에 곱하는 배율입니다.</summary>
        internal float Scale { get; }

        /// <summary>회전축입니다(월드, 단위 벡터).</summary>
        internal Vector3 SpinAxis { get; }

        /// <summary>초당 회전 각도입니다.</summary>
        internal float SpinDegrees { get; }

        /// <summary>출발할 때의 회전입니다(월드).</summary>
        internal Quaternion StartRotation { get; }

        internal CombatRockfallDrop(Vector3 landing, Vector3 side, Vector3 rise, float scale, Vector3 spinAxis, float spinDegrees, Quaternion startRotation)
        {
            Landing = landing;
            Side = side;
            Rise = rise;
            Scale = scale;
            SpinAxis = spinAxis;
            SpinDegrees = spinDegrees;
            StartRotation = startRotation;
        }
    }
}
