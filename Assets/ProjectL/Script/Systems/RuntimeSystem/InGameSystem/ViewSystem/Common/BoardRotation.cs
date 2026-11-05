using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common
{
    /// <summary>
    /// 판 위 수평 회전 식(회전 단계 × 90°)의 원본입니다. 로비 판의 도착 방향과 아티팩트 판의 모델 회전이 함께 씁니다
    /// (로비 리팩토링 2차 설계 M-09·N-40).
    /// </summary>
    internal static class BoardRotation
    {
        internal static Quaternion Yaw(Rotation4 rotation) => Quaternion.Euler(0f, (int)rotation * 90f, 0f);
    }
}
