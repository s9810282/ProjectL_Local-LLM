using System;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Effect.Enum;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap.Item
{
    /// <summary>스테이지 맵의 레이아웃, 강조 색, 선택 이펙트, 프리팹, 피킹 설정입니다.</summary>
    [Serializable]
    internal sealed class StageMapViewData : IData
    {
        /// <summary>노드 프리팹의 직접 자식 이름입니다. 종류별 외형을 이 아래에 생성합니다. 없으면 노드 Root를 사용합니다.</summary>
        internal const string VisualRootName = "VisualRoot";
        /// <summary>노드 프리팹의 직접 자식 이름입니다. 완료 표시용 Renderer를 가지며 원본에서는 비활성입니다.</summary>
        internal const string ClearedIndicatorName = "ClearedIndicator";
        /// <summary>노드 프리팹의 직접 자식 이름입니다. 피킹용 Collider를 가지며 그 Layer는 NodeMask에 포함되어야 합니다.</summary>
        internal const string HitTargetName = "HitTarget";

        [Header("Local Layout")]
        [Tooltip("맵 Root의 로컬 Z 방향 단계 간격입니다. 전체 진행 방향은 Root 또는 카메라로 조정합니다.")]
        [SerializeField, Min(0.01f)] private float depthSpacing = 4f;
        [Tooltip("같은 단계에서 로컬 X 방향으로 벌어지는 노드 간격입니다.")]
        [SerializeField, Min(0.01f)] private float widthSpacing = 4f;
        [SerializeField, Min(0.01f)] private float nodeScale = 1f;

        [Header("Connections")]
        [Tooltip("연결선의 맵 Root 기준 로컬 높이입니다.")]
        [SerializeField] private float lineHeight = 0.05f;
        [SerializeField, Min(0.001f)] private float lineWidth = 0.12f;

        [Header("Node Highlight")]
        [SerializeField] private Color currentColor = new(0.15f, 0.8f, 1f, 1f);
        [SerializeField] private Color selectableColor = new(1f, 1f, 1f, 1f);
        [SerializeField] private Color hoveredColor = new(1f, 0.9f, 0.4f, 1f);
        [SerializeField] private Color candidateColor = new(0.3f, 1f, 0.5f, 1f);
        [SerializeField] private Color unavailableColor = new(0.35f, 0.35f, 0.35f, 1f);

        [Header("Node Effects")]
        [Tooltip("노드에 포인터가 진입할 때 사용할 공통 이펙트입니다. Empty면 재생하지 않습니다.")]
        [SerializeField] private EffectType hoverEffect = EffectType.Empty;
        [Tooltip("노드를 선택할 때 사용할 공통 이펙트입니다. Core의 진입 승인과는 별개이며 Empty면 재생하지 않습니다.")]
        [SerializeField] private EffectType selectEffect = EffectType.Empty;

        [Header("Prefabs")]
        [Tooltip("노드 프리팹입니다. Collider를 가진 HitTarget 자식과 Renderer를 가진 ClearedIndicator 자식이 필요합니다.")]
        [SerializeField] private GameObject nodePrefab;
        [Tooltip("연결선 프리팹입니다. LineRenderer와 머티리얼이 있어야 합니다.")]
        [SerializeField] private GameObject connectionPrefab;
        [Tooltip("시작 지점 장식입니다. 비워 두면 기본 노드만 표시합니다.")]
        [SerializeField] private GameObject startVisualPrefab;
        [Tooltip("현재 플레이어 노드 위에 표시하고 이동시킬 마커 프리팹입니다.")]
        [SerializeField] private GameObject currentMarkerPrefab;

        [Header("Current Marker Motion")]
        [Tooltip("현재 위치 마커가 다음 노드로 이동하는 시간(초)입니다.")]
        [SerializeField, Min(0.01f)] private float markerMoveDuration = 0.6f;
        [Tooltip("마커가 이동할 때 뛰어오르는 높이입니다. 0이면 뛰지 않습니다.")]
        [SerializeField, Min(0f)] private float markerJumpHeight = 0.8f;

        [Header("Picking")]
        [Tooltip("노드 프리팹 Collider가 속한 Layer를 포함해야 합니다.")]
        [SerializeField] private LayerMask nodeMask = ~0;
        [SerializeField, Min(1f)] private float pickDistance = 1000f;

        public float DepthSpacing => depthSpacing;
        public float WidthSpacing => widthSpacing;
        public float NodeScale => nodeScale;
        public float LineHeight => lineHeight;
        public float LineWidth => lineWidth;
        public Color CurrentColor => currentColor;
        public Color SelectableColor => selectableColor;
        public Color HoveredColor => hoveredColor;
        public Color CandidateColor => candidateColor;
        public Color UnavailableColor => unavailableColor;
        /// <summary>Empty이면 View가 EffectSystem 재생 호출을 생략합니다.</summary>
        public EffectType HoverEffect => hoverEffect;
        /// <summary>Empty이면 View가 EffectSystem 재생 호출을 생략합니다.</summary>
        public EffectType SelectEffect => selectEffect;
        public GameObject NodePrefab => nodePrefab;
        public GameObject ConnectionPrefab => connectionPrefab;
        public GameObject StartVisualPrefab => startVisualPrefab;
        public GameObject CurrentMarkerPrefab => currentMarkerPrefab;
        public float MarkerMoveDuration => markerMoveDuration;
        public float MarkerJumpHeight => markerJumpHeight;
        public LayerMask NodeMask => nodeMask;
        public float PickDistance => pickDistance;

        internal void Validate()
        {
            if (!System.Enum.IsDefined(typeof(EffectType), hoverEffect) ||
                !System.Enum.IsDefined(typeof(EffectType), selectEffect))
                throw new InvalidOperationException("[StageMapViewData] Node effect is invalid.");

            RequirePositiveFinite(depthSpacing, nameof(DepthSpacing));
            RequirePositiveFinite(widthSpacing, nameof(WidthSpacing));
            RequirePositiveFinite(nodeScale, nameof(NodeScale));
            RequirePositiveFinite(lineWidth, nameof(LineWidth));
            RequirePositiveFinite(pickDistance, nameof(PickDistance));
            RequirePositiveFinite(markerMoveDuration, nameof(MarkerMoveDuration));
            if (!float.IsFinite(lineHeight))
                throw new InvalidOperationException($"[StageMapViewData] {nameof(LineHeight)} must be finite.");
            if (!float.IsFinite(markerJumpHeight) || markerJumpHeight < 0f)
                throw new InvalidOperationException($"[StageMapViewData] {nameof(MarkerJumpHeight)} must be a finite value of 0 or more.");
            RequireFiniteColor(currentColor, nameof(CurrentColor));
            RequireFiniteColor(selectableColor, nameof(SelectableColor));
            RequireFiniteColor(hoveredColor, nameof(HoveredColor));
            RequireFiniteColor(candidateColor, nameof(CandidateColor));
            RequireFiniteColor(unavailableColor, nameof(UnavailableColor));

            if (nodePrefab == null || connectionPrefab == null || currentMarkerPrefab == null)
                throw new InvalidOperationException("[StageMapViewData] Node, Connection and Current Marker prefabs are required.");
            if (nodePrefab.scene.IsValid() || connectionPrefab.scene.IsValid() || currentMarkerPrefab.scene.IsValid() ||
                (startVisualPrefab != null && startVisualPrefab.scene.IsValid()))
                throw new InvalidOperationException("[StageMapViewData] Prefabs must be prefab assets, not scene instances.");

            Transform hitTarget = nodePrefab.transform.Find(HitTargetName);
            Collider nodeCollider = hitTarget != null ? hitTarget.GetComponent<Collider>() : null;
            if (nodeCollider == null)
                throw new InvalidOperationException("[StageMapViewData] Node prefab requires HitTarget with a Collider for picking.");
            if ((nodeMask.value & (1 << nodeCollider.gameObject.layer)) == 0)
                throw new InvalidOperationException("[StageMapViewData] Node Mask must include the node Collider layer.");

            Transform clearedIndicator = nodePrefab.transform.Find(ClearedIndicatorName);
            if (clearedIndicator == null || clearedIndicator.GetComponentInChildren<Renderer>(true) == null)
                throw new InvalidOperationException("[StageMapViewData] Node prefab requires ClearedIndicator with a Renderer.");

            LineRenderer line = connectionPrefab.GetComponentInChildren<LineRenderer>(true);
            if (line == null || line.sharedMaterial == null)
                throw new InvalidOperationException("[StageMapViewData] Connection prefab requires a LineRenderer with a material.");
            if (currentMarkerPrefab.GetComponentInChildren<Renderer>(true) == null)
                throw new InvalidOperationException("[StageMapViewData] Current Marker prefab requires a Renderer.");
        }

        private static void RequirePositiveFinite(float value, string name)
        {
            if (!float.IsFinite(value) || value <= 0f)
                throw new InvalidOperationException($"[StageMapViewData] {name} must be a positive finite value.");
        }

        private static void RequireFiniteColor(Color color, string name)
        {
            if (!float.IsFinite(color.r) || !float.IsFinite(color.g) || !float.IsFinite(color.b) || !float.IsFinite(color.a))
                throw new InvalidOperationException($"[StageMapViewData] {name} must be finite.");
        }
    }

    [CreateAssetMenu(fileName = "NewStageMapViewData", menuName = "ProjectL/Database/View/StageMapViewData")]
    internal sealed class StageMapViewDataSO : DatabaseItem
    {
        [SerializeField] private StageMapViewData stageMapViewData = new();

        public override IData GetData() => stageMapViewData;
    }
}
