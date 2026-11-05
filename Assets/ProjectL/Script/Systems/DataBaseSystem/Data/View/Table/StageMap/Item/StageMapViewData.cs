using System;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap.Item
{
    /// <summary>스테이지 맵의 레이아웃, 프리팹, 마커와 피킹 설정입니다. 조회마다 새로 만드는 읽기 전용 값입니다.</summary>
    internal sealed class StageMapViewData : IData
    {
        private const string Owner = "[StageMapViewData]";

        internal StageMapViewData(float depthSpacing, float widthSpacing, float nodeScale, float buildingHeight,
            float lineHeight, float lineWidth,
            GameObject connectionPrefab, GameObject startVisualPrefab, GameObject currentMarkerPrefab,
            Vector3 markerOffset, float markerYaw, float markerScale, float markerMoveDuration,
            float markerJumpHeight, float pickDistance, Color lineColor)
        {
            DepthSpacing = depthSpacing;
            WidthSpacing = widthSpacing;
            NodeScale = nodeScale;
            BuildingHeight = buildingHeight;
            LineHeight = lineHeight;
            LineWidth = lineWidth;
            LineColor = lineColor;
            ConnectionPrefab = connectionPrefab;
            StartVisualPrefab = startVisualPrefab;
            CurrentMarkerPrefab = currentMarkerPrefab;
            MarkerOffset = markerOffset;
            MarkerYaw = markerYaw;
            MarkerScale = markerScale;
            MarkerMoveDuration = markerMoveDuration;
            MarkerJumpHeight = markerJumpHeight;
            PickDistance = pickDistance;
        }

        public float DepthSpacing { get; }
        public float WidthSpacing { get; }
        public float NodeScale { get; }
        public float BuildingHeight { get; }
        public float LineHeight { get; }
        public float LineWidth { get; }
        public Color LineColor { get; }
        public GameObject ConnectionPrefab { get; }
        public GameObject StartVisualPrefab { get; }
        public GameObject CurrentMarkerPrefab { get; }
        public Vector3 MarkerOffset { get; }
        public float MarkerYaw { get; }
        public float MarkerScale { get; }
        public float MarkerMoveDuration { get; }
        public float MarkerJumpHeight { get; }
        public float PickDistance { get; }

        internal void Validate()
        {
            RequirePositiveFinite(DepthSpacing, nameof(DepthSpacing));
            RequirePositiveFinite(WidthSpacing, nameof(WidthSpacing));
            RequirePositiveFinite(NodeScale, nameof(NodeScale));
            if (!float.IsFinite(BuildingHeight))
                throw new InvalidOperationException($"{Owner} {nameof(BuildingHeight)} must be finite.");
            RequirePositiveFinite(LineWidth, nameof(LineWidth));
            if (!float.IsFinite(LineColor.r) || !float.IsFinite(LineColor.g) ||
                !float.IsFinite(LineColor.b) || !float.IsFinite(LineColor.a))
                throw new InvalidOperationException($"{Owner} 연결선 색상은 유한한 값이어야 합니다.");
            RequirePositiveFinite(PickDistance, nameof(PickDistance));
            RequirePositiveFinite(MarkerMoveDuration, nameof(MarkerMoveDuration));
            ViewPrefabRules.RequirePresentation(MarkerOffset, MarkerYaw, MarkerScale, $"{Owner} 마커");
            if (!float.IsFinite(LineHeight))
                throw new InvalidOperationException($"{Owner} {nameof(LineHeight)} must be finite.");
            if (!float.IsFinite(MarkerJumpHeight) || MarkerJumpHeight < 0f)
                throw new InvalidOperationException($"{Owner} {nameof(MarkerJumpHeight)} must be a finite value of 0 or more.");

            ViewPrefabRules.RequirePrefabAsset(ConnectionPrefab, $"{Owner} {nameof(ConnectionPrefab)}");
            ViewPrefabRules.RequirePrefabAsset(CurrentMarkerPrefab, $"{Owner} {nameof(CurrentMarkerPrefab)}");
            if (StartVisualPrefab != null)
                ViewPrefabRules.RequirePrefabAsset(StartVisualPrefab, $"{Owner} {nameof(StartVisualPrefab)}");

            ValidateConnectionPrefab();
            if (CurrentMarkerPrefab.GetComponentInChildren<Renderer>(true) == null)
                throw new InvalidOperationException($"{Owner} Current Marker prefab requires a Renderer.");
            ViewPrefabRules.RequireActiveRoot(CurrentMarkerPrefab, $"{Owner} {nameof(CurrentMarkerPrefab)}");
        }

        private void ValidateConnectionPrefab()
        {
            ViewPrefabRules.RequireActiveRoot(ConnectionPrefab, $"{Owner} {nameof(ConnectionPrefab)}");
            MeshFilter filter = ConnectionPrefab.GetComponent<MeshFilter>();
            MeshRenderer renderer = ConnectionPrefab.GetComponent<MeshRenderer>();
            if (filter == null || filter.sharedMesh == null || renderer == null || !renderer.enabled ||
                ConnectionPrefab.GetComponentsInChildren<Renderer>(true).Length != 1 ||
                renderer.sharedMaterials.Length != 1 || renderer.sharedMaterial == null ||
                !renderer.sharedMaterial.HasProperty(MaterialBaseTypeKey.MainColor.Name))
                throw new InvalidOperationException($"{Owner} 연결선 루트에 Quad MeshFilter와 _BaseColor 머티리얼 하나를 쓰는 활성 MeshRenderer가 필요합니다.");

            Bounds bounds = filter.sharedMesh.bounds;
            if (bounds.center.sqrMagnitude > 0.000001f ||
                (bounds.size - new Vector3(1f, 1f, 0f)).sqrMagnitude > 0.000001f)
                throw new InvalidOperationException($"{Owner} 연결선 메시는 중심이 원점인 1×1 XY Quad여야 합니다.");
        }

        private static void RequirePositiveFinite(float value, string name)
        {
            if (!float.IsFinite(value) || value <= 0f)
                throw new InvalidOperationException($"{Owner} {name} must be a positive finite value.");
        }
    }
}
