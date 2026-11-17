using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 전투 카메라 조작(이동·회전·줌) 입력입니다(카메라 조작 설계 CC5, 카메라 회전 설계 CR4). 씬 공용 카메라 바인딩이 참조합니다.
    /// 운영과 특성 확인 환경이 같은 설정을 함께 씁니다. 자세 줌을 켜면 시작 크기·양 끝점의 위치·회전까지 여기서 지정합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "CombatCameraSettings", menuName = "ProjectL/Database/View/Combat Camera Settings")]
    internal sealed class CombatCameraSettingsSO : ScriptableObject
    {
#if UNITY_EDITOR
        [NonSerialized] private int _editorRevision;
        internal int EditorRevision => System.Threading.Volatile.Read(ref _editorRevision);
        private void OnValidate() => System.Threading.Interlocked.Increment(ref _editorRevision);
#endif

        [Header("이동")]
        [Tooltip("WASD·화살표 이동 속도(초당 월드 단위)입니다. 처음 보기 크기 기준이고, 줌을 바꾸면 화면에서 같은 빠르기가 되게 크기 비율을 곱합니다.")]
        [SerializeField, Min(0f)] private float keyPanSpeed = 10f;
        [Tooltip("화면 가장자리에 마우스를 댔을 때 이동 속도(초당 월드 단위, 처음 보기 크기 기준)입니다.")]
        [SerializeField, Min(0f)] private float edgePanSpeed = 10f;
        [Tooltip("가장자리로 보는 띠의 두께입니다(화면 짧은 변에 대한 비율, 0~0.5). 0이면 가장자리 이동을 끕니다.")]
        [SerializeField, Range(0f, 0.5f)] private float edgeThickness = 0.02f;
        [Tooltip("처음 카메라 위치에서 갈 수 있는 오프셋 최소입니다(x = 좌우, y = 상하, 카메라 화면 축의 월드 단위). 두 축 모두 0 이하.")]
        [SerializeField] private Vector2 panMin = new(-6f, -4f);
        [Tooltip("처음 카메라 위치에서 갈 수 있는 오프셋 최대입니다(x = 좌우, y = 상하, 카메라 화면 축의 월드 단위). 두 축 모두 0 이상.")]
        [SerializeField] private Vector2 panMax = new(6f, 4f);

        [Header("회전")]
        [Tooltip("Z·C 회전의 평균 속도(초당 도)입니다. 90도 회전 시간은 90 ÷ 속도입니다. 0이면 회전을 끕니다. 내려다보는 각은 바뀌지 않습니다.")]
        [SerializeField, Min(0f)] private float rotateSpeed = 90f;
        [Tooltip("Z·C 회전의 가로축은 경과 시간 비율, 세로축은 회전 진행률입니다. (0, 0)과 (1, 1) 사이를 직선 또는 원하는 곡선으로 조절합니다.")]
        [SerializeField] private AnimationCurve rotateCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [Tooltip("가운데 버튼(휠)을 누른 채 좌우로 끌 때, 화면 너비만큼 끌면 도는 각도(도)입니다. 오른쪽으로 끌면 Z와 같은 방향(화면 아래쪽 바닥이 커서를 따라감)이고, 음수면 반대 방향, 0이면 끌기 회전을 끕니다. 0이어도 가운데 버튼을 누르는 동안은 화면 가장자리 이동과 커서 칸 스냅이 멈춥니다.")]
        [SerializeField] private float rotateDragDegrees = 180f;

        [Header("줌")]
        [Tooltip("가장 가까이 당긴 정사영 크기입니다(Cinemachine Ortho Size).")]
        [SerializeField, Min(0.01f)] private float zoomMin = 4f;
        [Tooltip("가장 멀리 뺀 정사영 크기입니다. 자세 줌을 켜면 이 크기로 시작합니다. 꺼져 있으면 기존 프리팹 크기를 범위 안으로 맞춥니다.")]
        [SerializeField, Min(0.01f)] private float zoomMax = 10f;
        [Tooltip("휠 한 칸에 바뀌는 정사영 크기입니다. 위로 굴리면 당기고(작아짐), 아래로 굴리면 뺍니다.")]
        [SerializeField, Min(0.01f)] private float zoomStep = 1f;
        [Tooltip("줌 한 칸이 목표 크기에 닿기까지의 초입니다. 0이면 바로 바뀝니다.")]
        [SerializeField, Min(0f)] private float zoomSeconds = 0.15f;

        [Header("줌 자세 (각 카메라의 원래 구도 기준)")]
        [Tooltip("켜면 Size·위치·회전을 함께 보간하고 Zoom Max 구도로 시작합니다. 별도 마커나 카메라 컴포넌트 연결은 필요 없습니다.")]
        [SerializeField] private bool useZoomPose;
        [Tooltip("줌아웃 위치: 원래 카메라의 로컬 축 기준 오프셋입니다(월드 단위). 0이면 원래 위치입니다.")]
        [SerializeField] private Vector3 zoomOutPositionOffset;
        [Tooltip("줌아웃 회전: 원래 회전에 곱할 로컬 Euler 오프셋입니다(도). 0이면 원래 회전입니다.")]
        [SerializeField] private Vector3 zoomOutRotationOffset;
        [Tooltip("줌인 위치: 원래 카메라의 로컬 축 기준 오프셋입니다(월드 단위). X=오른쪽, Y=위, Z=전방입니다.")]
        [SerializeField] private Vector3 zoomInPositionOffset;
        [Tooltip("줌인 회전: 원래 회전에 곱할 로컬 Euler 오프셋입니다(도). 실행 중에는 Quaternion으로 보간합니다.")]
        [SerializeField] private Vector3 zoomInRotationOffset;

        /// <summary>입력을 검사하고 매번 새 불변 값을 만듭니다(DB 규칙 P2). 조건 원본은 <see cref="CombatCameraSettingsData.FindError"/>입니다.</summary>
        internal CombatCameraSettingsData CreateData()
        {
            string error = CombatCameraSettingsData.FindError(
                keyPanSpeed, edgePanSpeed, edgeThickness, panMin, panMax, zoomMin, zoomMax, zoomStep, zoomSeconds,
                useZoomPose, zoomOutPositionOffset, zoomOutRotationOffset, zoomInPositionOffset, zoomInRotationOffset, rotateSpeed, rotateDragDegrees);
            if (error != null)
                throw new InvalidOperationException($"[CombatCameraSettingsSO] 카메라 조작 값이 잘못되었습니다: {error}, Asset: {name}");

            return new CombatCameraSettingsData(
                keyPanSpeed, edgePanSpeed, edgeThickness, panMin, panMax, zoomMin, zoomMax, zoomStep, zoomSeconds,
                useZoomPose, zoomOutPositionOffset, zoomOutRotationOffset, zoomInPositionOffset, zoomInRotationOffset, rotateSpeed, rotateDragDegrees, rotateCurve);
        }
    }
}
