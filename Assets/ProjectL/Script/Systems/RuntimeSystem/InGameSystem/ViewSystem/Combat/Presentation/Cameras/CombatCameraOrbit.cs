using System;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras
{
    /// <summary>
    /// 전투 카메라 회전 계산입니다(카메라 회전 설계 CR2). 상태가 없고 입력·Cinemachine을 모릅니다.
    /// 회전은 월드 위 축(Y)만이고 내려다보는 각·롤은 바꾸지 않습니다. 시점(<see cref="CombatCameraView"/>)과
    /// 줌 자세 어댑터(<see cref="CombatCameraZoomPose"/>)가 같은 함수를 부릅니다.
    /// </summary>
    /// <remarks>
    /// 방위각이 0이면 <see cref="Apply"/>·<see cref="ToBasePan"/>가 입력을 그대로 돌려줍니다. 그래서 회전하지 않은 동안은 회전 전과 같은 수가 나옵니다(계획 TP-4).
    /// 각의 부호는 Unity 위 축 회전과 같습니다. 양의 각은 앞(+Z)을 오른쪽(+X)으로 돌리므로, 카메라는 보던 점 둘레로 자기 왼쪽으로 갑니다(계획 TP-1).
    /// </remarks>
    internal static class CombatCameraOrbit
    {
        // 바닥을 보는지 가르는 경계입니다. 앞 방향의 높이 성분이 −(이 값)보다 크면(수평에 가깝거나 위를 보면) 바닥 점이 없습니다.
        private const float MinDownward = 0.0001f;
        // 이동 기준 축의 허용 값입니다(계획 TP-3). 오른쪽 축의 높이 성분이 이보다 크면 롤이 있는 것으로 봅니다.
        private const float MaxRightTilt = 0.001f;
        // 이동 기준 축이 바닥을 충분히 내려다보는지의 경계입니다(sin, 계획 TP-3).
        private const float MinPitchSin = 0.01f;

        /// <summary>
        /// 자세의 앞 방향 광선이 높이 <paramref name="groundY"/>의 바닥 평면과 만나는 점입니다(설계 CR1, 화면 가운데가 보는 바닥 점).
        /// 바닥을 보지 않는 자세면 예외입니다.
        /// </summary>
        internal static Vector3 GroundPoint(Pose pose, float groundY)
        {
            if (!TryGroundPoint(pose, groundY, out Vector3 point))
                throw new InvalidOperationException("[CombatCameraOrbit] 카메라가 바닥을 보지 않아 회전 중심을 정할 수 없습니다.");
            return point;
        }

        /// <summary>
        /// <see cref="GroundPoint"/>와 같되 바닥을 보지 않으면 false를 돌려줍니다. Cinemachine 콜백처럼 예외를 던지면 매 프레임 되풀이되는 곳이 씁니다
        /// (Play 중 Transform을 고쳐 바닥을 보지 않게 된 경우, 검토 L3).
        /// </summary>
        internal static bool TryGroundPoint(Pose pose, float groundY, out Vector3 point)
        {
            Vector3 forward = pose.rotation * Vector3.forward;
            if (forward.y > -MinDownward)
            {
                point = default;
                return false;
            }
            float distance = (groundY - pose.position.y) / forward.y;
            point = pose.position + forward * distance;
            return true;
        }

        /// <summary>
        /// 자세를 <paramref name="pivot"/>을 지나는 위 축 둘레로 <paramref name="yaw"/>도 돌립니다(설계 CR2).
        /// 위치 = 중심 + R·(위치 − 중심), 회전 = R·회전입니다. 방위각이 0이면 자세를 그대로 돌려줍니다.
        /// </summary>
        internal static Pose Apply(Pose pose, Vector3 pivot, float yaw)
        {
            if (yaw == 0f)
                return pose;
            Quaternion turn = Quaternion.AngleAxis(yaw, Vector3.up);
            return new Pose(pivot + turn * (pose.position - pivot), turn * pose.rotation);
        }

        /// <summary>
        /// 지금 화면 축의 이동량(x = 오른쪽, y = 위)을 처음 카메라 화면 축의 오프셋 변화로 바꿉니다(설계 CR3).
        /// 화면 위로 d만큼 = 바닥 앞으로 d ÷ sin(내려다보는 각)이라, 바닥 방향으로 늘려 방위각만큼 돌린 뒤 다시 화면 축으로 줄입니다.
        /// 방위각이 0이면 이동량을 그대로 돌려줍니다.
        /// </summary>
        /// <param name="pitchSin">이동 기준 축의 내려다보는 정도입니다(<see cref="PitchSinOf"/>). 0보다 커야 합니다.</param>
        internal static Vector2 ToBasePan(Vector2 screen, float yaw, float pitchSin)
        {
            if (yaw == 0f)
                return screen;
            float radians = yaw * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            float right = screen.x;
            float ahead = screen.y / pitchSin;
            return new Vector2(right * cos + ahead * sin, (-right * sin + ahead * cos) * pitchSin);
        }

        /// <summary>
        /// 바닥 위 이동(월드, 높이 성분은 무시)을 처음 카메라 화면 축의 오프셋 변화로 바꿉니다(회전 중심 보정, 설계 §8 검토 L1).
        /// x = 오른쪽 축 성분, y = 바닥 앞 축 성분 × sin(내려다보는 각)입니다. 축은 <see cref="FlatAxesOf"/>가 만듭니다.
        /// </summary>
        internal static Vector2 GroundToBasePan(Vector3 ground, Vector3 flatRight, Vector3 flatAhead, float pitchSin) =>
            new Vector2(Vector3.Dot(ground, flatRight), Vector3.Dot(ground, flatAhead) * pitchSin);

        /// <summary>
        /// 이동 기준 축의 바닥 위 오른쪽·앞 방향(수평 단위 벡터)입니다. <see cref="FindFrameError"/>를 통과한 회전이어야 합니다(롤 없음·바닥을 봄).
        /// </summary>
        internal static void FlatAxesOf(Quaternion rotation, out Vector3 flatRight, out Vector3 flatAhead)
        {
            Vector3 right = rotation * Vector3.right;
            Vector3 forward = rotation * Vector3.forward;
            flatRight = new Vector3(right.x, 0f, right.z).normalized;
            flatAhead = new Vector3(forward.x, 0f, forward.z).normalized;
        }

        /// <summary>회전의 내려다보는 정도입니다. 앞 방향 높이 성분의 반대 부호라, 아래를 볼수록 큽니다.</summary>
        internal static float PitchSinOf(Quaternion rotation) => -(rotation * Vector3.forward).y;

        /// <summary>
        /// 이동 기준 축으로 쓸 수 있는지 검사합니다(계획 TP-3). 롤이 없고(오른쪽 축이 수평), 위아래가 뒤집히지 않았고(위 축이 위를 향함),
        /// 바닥을 충분히 내려다봐야 합니다. 뒤집힌 카메라(롤 180° 또는 내려다보는 각 90° 초과)는 화면 위가 바닥 앞과 반대라 돌린 뒤 이동이 거꾸로 갑니다(검토 L2).
        /// 문제가 없으면 null, 있으면 문구를 돌려줍니다.
        /// </summary>
        internal static string FindFrameError(Quaternion rotation)
        {
            if (Mathf.Abs((rotation * Vector3.right).y) > MaxRightTilt)
                return "카메라에 롤이 있습니다. 회전 이동은 오른쪽 축이 수평인 카메라만 지원합니다";
            if (!((rotation * Vector3.up).y > 0f))
                return "카메라 위 축이 아래를 향합니다(위아래가 뒤집힘)";
            if (!(PitchSinOf(rotation) > MinPitchSin))
                return "카메라가 바닥을 내려다보지 않습니다";
            return null;
        }
    }
}
