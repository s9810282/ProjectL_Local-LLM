using System;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageNode.Item;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap.Item
{
    /// <summary>스테이지 맵 표시 설정을 입력하고 조회마다 새 읽기 전용 데이터를 만듭니다(DB 규칙 P2).</summary>
    [CreateAssetMenu(fileName = "NewStageMapViewData", menuName = "ProjectL/Database/View/StageMapViewData")]
    internal sealed class StageMapViewDataSO : DatabaseItem
    {
        // 칸 이름은 옛 직렬화 경로 그대로라 에셋 값이 유지됩니다.
        [SerializeField] private Input stageMapViewData = new();

        public override IData GetData() => stageMapViewData.ToData();

        [Serializable]
        private sealed class Input
        {
            [Header("Local Layout")]
            [Tooltip("맵 Root의 로컬 Z 방향 단계 간격입니다. 전체 진행 방향은 Root 또는 카메라로 조정합니다.")]
            [SerializeField, Min(0.01f)] private float depthSpacing = 4f;
            [Tooltip("같은 단계에서 로컬 X 방향으로 벌어지는 노드 간격입니다.")]
            [SerializeField, Min(0.01f)] private float widthSpacing = 4f;
            [SerializeField, Min(0.01f)] private float nodeScale = 1f;

            [Tooltip("건물 밑면을 놓을 노드 로컬 높이입니다. 연결선 높이와 비슷하게 둡니다. 건물이 뜨거나 묻혀 보이면 조절합니다.")]
            [SerializeField] private float buildingHeight;

            [Header("Connections")]
            [Tooltip("연결선의 맵 Root 기준 로컬 높이입니다.")]
            [SerializeField] private float lineHeight = 0.05f;
            [SerializeField, Min(0.001f)] private float lineWidth = 0.12f;
            [Tooltip("연결선 색상입니다. 불투명 머티리얼을 사용하며 변경 후 Play를 재시작해야 적용됩니다.")]
            [SerializeField, ColorUsage(false)] private Color lineColor = new(0.1f, 0.85f, 1f, 1f);

            [Header("Prefabs")]
            [Tooltip("루트에 중심 원점의 1×1 XY Quad MeshFilter와 _BaseColor 머티리얼의 MeshRenderer가 필요합니다.")]
            [SerializeField] private GameObject connectionPrefab;
            [Tooltip("시작 지점 장식입니다. 비워 두면 시작 지점에는 아무것도 표시하지 않습니다.")]
            [SerializeField] private GameObject startVisualPrefab;

            [Header("Current Marker Presentation")]
            [Tooltip("노드 루트 기준 메인 캐릭터 모델의 표시 위치입니다. 모델의 루트 Transform 대신 적용합니다.")]
            [SerializeField] private Vector3 markerOffset;
            [Tooltip("이동 방향에 더할 모델의 Y 회전 보정입니다.")]
            [SerializeField] private float markerYaw;
            [Tooltip("모델 표시 배율입니다. 노드 크기와 별개로 적용합니다.")]
            [SerializeField, Min(0.001f)] private float markerScale = 1f;

            [Header("Current Marker Motion")]
            [Tooltip("현재 위치 마커가 다음 노드로 이동하는 시간(초)입니다.")]
            [SerializeField, Min(0.01f)] private float markerMoveDuration = 0.6f;
            [Tooltip("마커가 이동할 때 뛰어오르는 높이입니다. 0이면 뛰지 않습니다.")]
            [SerializeField, Min(0f)] private float markerJumpHeight = 0.8f;

            [Header("Node Visit")]
            [SerializeField] private MarkerVisitInput markerVisit = new();

            [Header("Picking")]
            [SerializeField, Min(1f)] private float pickDistance = 1000f;

            [Header("Node Drop")]
            [Tooltip("챕터 최초 표시 때 플레이어부터 뻗는 선의 도착에 맞추는 낙하 연출입니다.")]
            [SerializeField] private NodeDropInput nodeDrop = new();

            [Header("Map Intro")]
            [Tooltip("챕터 최초 표시의 선 전개와 카메라 왕복 시간입니다. 낙하는 같은 시간표에 맞춥니다.")]
            [SerializeField] private MapIntroInput mapIntro = new();

            [Header("Camera Focus")]
            [Tooltip("노드 이동 전 구간에 카메라가 동행하고 로딩 중 복귀 구도를 맞춥니다.")]
            [SerializeField] private CameraFocusInput cameraFocus = new();

            [Header("Node Hover")]
            [Tooltip("건물 윤곽선과 선택 가능한 연결선의 색상·두께·전환 시간입니다.")]
            [SerializeField] private StageNodeHoverStyleSO hoverStyle;

            internal StageMapViewData ToData() => new(depthSpacing, widthSpacing, nodeScale, buildingHeight,
                lineHeight, lineWidth, connectionPrefab, startVisualPrefab,
                markerOffset, markerYaw, markerScale, markerMoveDuration, markerJumpHeight,
                pickDistance, lineColor, nodeDrop != null ? nodeDrop.ToData() :
                    throw new InvalidOperationException("[StageMapViewDataSO] Node Drop 설정이 필요합니다."),
                mapIntro != null ? mapIntro.ToData() :
                    throw new InvalidOperationException("[StageMapViewDataSO] Map Intro 설정이 필요합니다."),
                cameraFocus != null ? cameraFocus.ToData() :
                    throw new InvalidOperationException("[StageMapViewDataSO] Camera Focus 설정이 필요합니다."),
                markerVisit != null ? markerVisit.ToData() :
                    throw new InvalidOperationException("[StageMapViewDataSO] Marker Visit 설정이 필요합니다."),
                hoverStyle != null ? hoverStyle.GetData() :
                    throw new InvalidOperationException("[StageMapViewDataSO] Node Hover의 Hover Style 에셋을 연결해야 합니다."));
        }

        [Serializable]
        private sealed class MarkerVisitInput
        {
            [Tooltip("출발 노드와 이어진 선 쪽으로 건물 끝에서 더 띄우는 맵 로컬 여백입니다.")]
            [SerializeField, Min(0.001f)] private float frontClearance = 0.4f;
            [SerializeField, Min(0f)] private float settleJumpHeight = 0.2f;
            [SerializeField, Min(0.001f)] private float settleJumpSeconds = 0.25f;

            internal StageMarkerVisitData ToData() => new(frontClearance, settleJumpHeight, settleJumpSeconds);
        }

        [Serializable]
        private sealed class CameraFocusInput
        {
            [SerializeField] private bool enabled = true;
            [Tooltip("플레이어 바닥 위치의 화면 높이입니다. 아래 0, 위 1이며 맵 끝에서는 이동 범위가 우선합니다.")]
            [SerializeField, Range(0.01f, 0.99f)] private float playerViewportY = 0.35f;

            internal StageMapCameraFocusData ToData() => new(enabled, playerViewportY);
        }

        [Serializable]
        private sealed class NodeDropInput
        {
            [SerializeField] private bool enabled = true;
            [SerializeField, Min(0f)] private float height = 20f;
            [SerializeField, Min(0f)] private float seconds = 0.8f;
            internal StageNodeDropData ToData() => new(enabled, height, seconds);
        }

        [Serializable]
        private sealed class MapIntroInput
        {
            [SerializeField] private bool enabled = true;
            [SerializeField, Min(0f)] private float startHoldSeconds = 0.6f;
            [SerializeField, Min(0.01f)] private float revealSeconds = 7f;
            [SerializeField, Min(0f)] private float bossHoldSeconds = 0.9f;
            [SerializeField, Min(0f)] private float returnSeconds = 2.2f;

            internal StageMapIntroData ToData() => new(enabled, startHoldSeconds, revealSeconds,
                bossHoldSeconds, returnSeconds);
        }
    }
}
