using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts
{
    /// <summary>
    /// 카메라 입력(<c>CombatCameraInput</c>)이 시점에 거는 좁은 계약입니다(카메라 조작 설계 CC1). 입력은 무엇을 눌렀나만 넘기고,
    /// 얼마나·어디까지 움직일지(속도·제한·줌 크기)는 구현이 정합니다.
    /// </summary>
    /// <remarks>구현: <c>CombatCameraView</c>(표시가 연결마다 하나 가짐). 맵 카메라가 없으면 모든 조작이 아무 일도 하지 않습니다.</remarks>
    internal interface ICombatCameraControl
    {
        /// <summary>화면 가장자리로 보는 띠의 두께입니다(화면 짧은 변에 대한 비율). 0이면 가장자리 이동이 없습니다.</summary>
        float EdgeThickness { get; }

        /// <summary>
        /// 줌 렌즈가 새 보기 크기로 옮겨 가는 시간(초)입니다. 그동안 화면 아래 칸이 계속 바뀌므로 입력이 포인터를 다시 집습니다(평가 C10-01).
        /// 맵 카메라가 없으면 0입니다.
        /// </summary>
        float ZoomSeconds { get; }

        /// <summary>이번 프레임의 이동을 더합니다. 방향은 x = 오른쪽, y = 위이고 길이 1을 넘으면 1로 줄입니다. 실제로 움직였으면 true입니다.</summary>
        bool Pan(Vector2 keyDirection, Vector2 edgeDirection, float deltaTime);

        /// <summary>보기 크기를 휠 칸 수만큼 바꿉니다. 양수 = 줌인입니다(카메라 조작 계획 P1). 바꿨으면 true입니다.</summary>
        bool Zoom(int steps);
    }
}
