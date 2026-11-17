using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 전투 카메라 조작(이동·회전·줌) 값입니다(카메라 조작 설계 CC5, 카메라 회전 설계 CR4·CR8). 스테이지 맵 프랍(<c>StageViewPropData</c>)이 SO로 참조하고,
    /// <see cref="CombatCameraSettingsSO.CreateData"/>가 검사한 뒤 새로 만듭니다(DB 규칙 P2). 값 조건의 원본은 <see cref="FindError"/> 하나입니다.
    /// 이동은 처음 카메라 위치에서 카메라 화면 축(오른쪽·위쪽)으로 떨어진 거리(월드 단위)이고, 줌은 정사영 크기입니다.
    /// </summary>
    internal sealed class CombatCameraSettingsData
    {
        private readonly AnimationCurve _rotateCurve;

        internal CombatCameraSettingsData(
            float keyPanSpeed,
            float edgePanSpeed,
            float edgeThickness,
            Vector2 panMin,
            Vector2 panMax,
            float zoomMin,
            float zoomMax,
            float zoomStep,
            float zoomSeconds,
            bool useZoomPose = false,
            Vector3 zoomOutPositionOffset = default,
            Vector3 zoomOutRotationOffset = default,
            Vector3 zoomInPositionOffset = default,
            Vector3 zoomInRotationOffset = default,
            float rotateSpeed = 0f,
            float rotateDragDegrees = 0f,
            AnimationCurve rotateCurve = null)
        {
            string error = FindError(keyPanSpeed, edgePanSpeed, edgeThickness, panMin, panMax, zoomMin, zoomMax, zoomStep, zoomSeconds,
                useZoomPose, zoomOutPositionOffset, zoomOutRotationOffset, zoomInPositionOffset, zoomInRotationOffset, rotateSpeed, rotateDragDegrees);
            if (error != null)
                throw new System.ArgumentException($"[CombatCameraSettingsData] {error}");

            KeyPanSpeed = keyPanSpeed;
            EdgePanSpeed = edgePanSpeed;
            EdgeThickness = edgeThickness;
            PanMin = panMin;
            PanMax = panMax;
            ZoomMin = zoomMin;
            ZoomMax = zoomMax;
            ZoomStep = zoomStep;
            ZoomSeconds = zoomSeconds;
            UseZoomPose = useZoomPose;
            ZoomOutPositionOffset = zoomOutPositionOffset;
            ZoomOutRotationOffset = zoomOutRotationOffset;
            ZoomInPositionOffset = zoomInPositionOffset;
            ZoomInRotationOffset = zoomInRotationOffset;
            _rotateCurve = rotateCurve != null && rotateCurve.length >= 2 ? new AnimationCurve(rotateCurve.keys) : null;
            RotateSpeed = rotateSpeed;
            RotateDragDegrees = rotateDragDegrees;
        }

        /// <summary>WASD·화살표 이동 속도(초당 월드 단위, 처음 보기 크기 기준)입니다.</summary>
        internal float KeyPanSpeed { get; }

        /// <summary>화면 가장자리 이동 속도(초당 월드 단위, 처음 보기 크기 기준)입니다.</summary>
        internal float EdgePanSpeed { get; }

        /// <summary>가장자리로 보는 띠의 두께(화면 짧은 변에 대한 비율, 0~0.5)입니다. 0이면 가장자리 이동이 없습니다.</summary>
        internal float EdgeThickness { get; }

        /// <summary>처음 위치에서 갈 수 있는 오프셋 최소(x = 좌우, y = 상하, 월드 단위)입니다. 두 축 모두 0 이하입니다.</summary>
        internal Vector2 PanMin { get; }

        /// <summary>처음 위치에서 갈 수 있는 오프셋 최대(x = 좌우, y = 상하, 월드 단위)입니다. 두 축 모두 0 이상입니다.</summary>
        internal Vector2 PanMax { get; }

        /// <summary>가장 가까이 당긴 정사영 크기입니다.</summary>
        internal float ZoomMin { get; }

        /// <summary>가장 멀리 뺀 정사영 크기입니다.</summary>
        internal float ZoomMax { get; }

        /// <summary>휠 한 칸에 바뀌는 정사영 크기입니다.</summary>
        internal float ZoomStep { get; }

        /// <summary>줌 한 칸이 목표 크기에 닿기까지의 초입니다. 0이면 바로 바뀝니다.</summary>
        internal float ZoomSeconds { get; }

        /// <summary>위치·회전을 함께 보간합니다. 켜면 시작 크기도 ZoomMax가 원본입니다.</summary>
        internal bool UseZoomPose { get; }

        /// <summary>원래 카메라의 로컬 축 기준 줌아웃 위치 오프셋입니다(월드 단위).</summary>
        internal Vector3 ZoomOutPositionOffset { get; }

        /// <summary>원래 카메라 회전에 곱할 줌아웃 회전 오프셋입니다(Euler 도).</summary>
        internal Vector3 ZoomOutRotationOffset { get; }

        /// <summary>원래 카메라의 로컬 축 기준 줌인 위치 오프셋입니다(월드 단위).</summary>
        internal Vector3 ZoomInPositionOffset { get; }

        /// <summary>원래 카메라 회전에 곱할 줌인 회전 오프셋입니다(Euler 도).</summary>
        internal Vector3 ZoomInRotationOffset { get; }

        /// <summary>
        /// 카메라가 화면 가운데 바닥 점 둘레로 도는 빠르기(초당 도)입니다. 0이면 회전을 끕니다(카메라 회전 설계 CR4).
        /// 생성자 기본값은 0이고, 운영 값은 SO의 기본값·에셋(90)이 원본입니다(카메라 회전 계획 TP-7).
        /// </summary>
        internal float RotateSpeed { get; }

        internal float EvaluateRotation(float progress) =>
            _rotateCurve == null ? progress : Mathf.Clamp01(_rotateCurve.Evaluate(progress));

        /// <summary>
        /// 가운데 버튼 끌기로 화면 너비만큼 끌면 도는 각(도)입니다(카메라 회전 설계 CR8). 양수 = 오른쪽으로 끌면 Z와 같은 방향, 음수 = 반대, 0 = 끔.
        /// 0이어도 가운데 버튼을 누르는 동안 가장자리 이동·커서 스냅은 멈춥니다(검토 L3, 그대로 두기로 함).
        /// 생성자 기본값은 0이고, 운영 값은 SO의 기본값·에셋(180)이 원본입니다.
        /// </summary>
        internal float RotateDragDegrees { get; }

        /// <summary>
        /// 값 조건을 검사하고 첫 문제를 돌려줍니다. 없으면 null입니다.
        /// 속도·시간은 0 이상, 가장자리 두께는 0 이상 0.5 이하, 이동 범위는 처음 위치(0)를 품고, 줌은 0 &lt; 최소 ≤ 최대, 한 칸은 0보다 큽니다.
        /// 회전 빠르기는 0 이상, 끌기 회전 각은 유한한 수(음수 = 반대 방향)입니다.
        /// </summary>
        internal static string FindError(
            float keyPanSpeed,
            float edgePanSpeed,
            float edgeThickness,
            Vector2 panMin,
            Vector2 panMax,
            float zoomMin,
            float zoomMax,
            float zoomStep,
            float zoomSeconds,
            bool useZoomPose = false,
            Vector3 zoomOutPositionOffset = default,
            Vector3 zoomOutRotationOffset = default,
            Vector3 zoomInPositionOffset = default,
            Vector3 zoomInRotationOffset = default,
            float rotateSpeed = 0f,
            float rotateDragDegrees = 0f)
        {
            if (!IsNonNegative(keyPanSpeed))
                return $"keyPanSpeed({keyPanSpeed})는 0 이상이어야 합니다";
            if (!IsNonNegative(edgePanSpeed))
                return $"edgePanSpeed({edgePanSpeed})는 0 이상이어야 합니다";
            if (!IsNonNegative(edgeThickness) || edgeThickness > 0.5f)
                return $"edgeThickness({edgeThickness})는 0 이상 0.5 이하여야 합니다(화면 비율)";
            if (!IsFinite(panMin.x) || !IsFinite(panMin.y) || panMin.x > 0f || panMin.y > 0f)
                return $"panMin({panMin})은 두 축 모두 0 이하여야 합니다(처음 위치를 품어야 함)";
            if (!IsFinite(panMax.x) || !IsFinite(panMax.y) || panMax.x < 0f || panMax.y < 0f)
                return $"panMax({panMax})는 두 축 모두 0 이상이어야 합니다(처음 위치를 품어야 함)";
            if (!IsFinite(zoomMin) || zoomMin <= 0f)
                return $"zoomMin({zoomMin})은 0보다 커야 합니다";
            if (!IsFinite(zoomMax) || zoomMax < zoomMin)
                return $"zoomMax({zoomMax})는 zoomMin({zoomMin}) 이상이어야 합니다";
            if (!IsFinite(zoomStep) || zoomStep <= 0f)
                return $"zoomStep({zoomStep})은 0보다 커야 합니다";
            if (!IsNonNegative(zoomSeconds))
                return $"zoomSeconds({zoomSeconds})는 0 이상이어야 합니다";
            if (!IsFinite(zoomOutPositionOffset) || !IsFinite(zoomOutRotationOffset)
                || !IsFinite(zoomInPositionOffset) || !IsFinite(zoomInRotationOffset))
                return "줌 자세의 위치·회전 오프셋은 모두 유한해야 합니다";
            if (useZoomPose && zoomMin == zoomMax)
                return "자세 줌을 사용하면 zoomMin은 zoomMax보다 작아야 합니다";
            if (!IsNonNegative(rotateSpeed))
                return $"rotateSpeed({rotateSpeed})는 0 이상이어야 합니다";
            if (!IsFinite(rotateDragDegrees))
                return $"rotateDragDegrees({rotateDragDegrees})는 유한해야 합니다";
            return null;
        }

        private static bool IsNonNegative(float value) => IsFinite(value) && value >= 0f;

        private static bool IsFinite(Vector3 value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
