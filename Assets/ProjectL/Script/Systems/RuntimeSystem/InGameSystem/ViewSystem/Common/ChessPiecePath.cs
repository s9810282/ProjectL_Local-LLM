using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common
{
    /// <summary>
    /// 체스 말 이동 한 번의 경로 모양입니다. <see cref="ChessPieceMotion"/>이 이동을 시작할 때 <see cref="Plan"/>으로 이번 경로를 준비하고,
    /// 예약 시간 동안 <see cref="Evaluate"/>로 위치·회전을 묻습니다. 예약·취소·완료와 정착 자세는 <see cref="ChessPieceMotion"/>이 소유합니다.
    /// </summary>
    /// <remarks>
    /// 위치는 이동 대상과 같은 좌표계(월드 또는 부모 로컬)입니다. 한 이동 연출이 모양 하나를 소유하며 한 번에 경로 하나만 준비합니다.
    /// 같은 입력에는 같은 자세를 내야 합니다.
    /// </remarks>
    internal abstract class ChessPiecePath
    {
        /// <summary>이번 경로를 준비하고 전체 시간(초)을 돌려줍니다. 출발과 도착은 서로 다른 위치입니다.</summary>
        /// <returns>유한한 양수여야 합니다. 아니면 <see cref="ChessPieceMotion"/>이 예외로 멈춥니다.</returns>
        internal abstract float Plan(Vector3 origin, Quaternion originRotation, Vector3 destination, Vector3? facingPosition);

        /// <summary>
        /// 경과 시간(0~전체 시간)의 위치와 회전입니다. 전체 시간의 위치는 도착 위치입니다. 이동 중 매 프레임 불리므로 할당하지 않습니다.
        /// </summary>
        /// <param name="currentRotation">대상의 지금 회전입니다. 바라볼 수평 방향이 없을 때 유지할 값으로 씁니다.</param>
        internal abstract void Evaluate(float elapsed, Quaternion currentRotation, out Vector3 position, out Quaternion rotation);

        /// <summary>위치에서 바라볼 점을 향하는 수평 회전입니다. 바라볼 점이 없거나 수평 방향이 0이면 <paramref name="fallback"/>입니다.</summary>
        protected internal static Quaternion FacingRotation(Vector3 position, Vector3? facingPosition, Quaternion fallback)
        {
            if (!facingPosition.HasValue)
                return fallback;
            Vector3 direction = facingPosition.Value - position;
            direction.y = 0f;
            // 같은 XZ 위치에서는 정면을 다시 만들지 않고 직전의 유효한 방향을 유지합니다.
            if (direction.sqrMagnitude <= Mathf.Epsilon)
                return fallback;
            return Quaternion.LookRotation(direction, Vector3.up);
        }
    }
}
