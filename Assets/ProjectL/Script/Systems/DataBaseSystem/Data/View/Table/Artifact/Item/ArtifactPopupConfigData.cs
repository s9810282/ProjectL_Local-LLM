using System;
using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.Artifact.Item
{
    /// <summary>3D 보드의 표현 단위입니다. 논리 보드 크기는 Buffer가 소유합니다. 조회마다 새로 만드는 읽기 전용 값입니다.</summary>
    internal sealed class ArtifactPopupConfigData : IData
    {
        internal ArtifactPopupConfigData(float cellSpacing, float cellScale, float modelScale, float modelHeight,
            Vector3 boardCenter, Color previewValidColor, Color previewInvalidColor, string outputLayerName)
        {
            CellSpacing = cellSpacing;
            CellScale = cellScale;
            ModelScale = modelScale;
            ModelHeight = modelHeight;
            BoardCenter = boardCenter;
            PreviewValidColor = previewValidColor;
            PreviewInvalidColor = previewInvalidColor;
            OutputLayerName = outputLayerName;
        }

        internal float CellSpacing { get; }
        internal float CellScale { get; }
        internal float ModelScale { get; }
        internal float ModelHeight { get; }
        internal Vector3 BoardCenter { get; }
        internal Color PreviewValidColor { get; }
        internal Color PreviewInvalidColor { get; }
        /// <summary>팝업 동안 출력 카메라 마스크에 더할 층 이름입니다(로비 리팩토링 2차 설계 N-61).</summary>
        internal string OutputLayerName { get; }

        internal void Validate()
        {
            if (!float.IsFinite(CellSpacing) || CellSpacing <= 0 ||
                !float.IsFinite(CellScale) || CellScale <= 0 ||
                !float.IsFinite(ModelScale) || ModelScale <= 0 || !float.IsFinite(ModelHeight) ||
                !float.IsFinite(BoardCenter.x) || !float.IsFinite(BoardCenter.y) || !float.IsFinite(BoardCenter.z))
                throw new InvalidOperationException("[ArtifactPopupConfigData] 배치 값은 유한해야 하고, 칸 간격과 배율은 양수여야 합니다.");
            if (string.IsNullOrEmpty(OutputLayerName) || LayerMask.NameToLayer(OutputLayerName) < 0)
                throw new InvalidOperationException($"[ArtifactPopupConfigData] outputLayerName: 출력 마스크에 더할 층 '{OutputLayerName}'이 프로젝트에 없습니다.");
        }
    }
}
