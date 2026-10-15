using System;
using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.Artifact.Item
{
    /// <summary>3D 보드의 표현 단위입니다. 논리 보드 크기는 Buffer가 소유합니다.</summary>
    [Serializable]
    internal sealed class ArtifactPopupConfigData : IData
    {
        [SerializeField, Min(0.01f)] private float cellSpacing = 1.2f;
        [SerializeField, Min(0.01f)] private float cellScale = 1f;
        [SerializeField, Min(0.01f)] private float modelScale = 1f;
        [SerializeField] private float modelHeight = 0.15f;
        [SerializeField] private Vector3 boardCenter;
        [SerializeField] private Color previewValidColor = new Color(.15f, .8f, .65f, .65f);
        [SerializeField] private Color previewInvalidColor = new Color(.95f, .2f, .2f, .65f);
        [SerializeField, Min(0.01f)] private float backgroundDistance = 30f;
        [SerializeField, Min(1f)] private float backgroundMargin = 1.05f;

        internal float CellSpacing => cellSpacing;
        internal float CellScale => cellScale;
        internal float ModelScale => modelScale;
        internal float ModelHeight => modelHeight;
        internal Vector3 BoardCenter => boardCenter;
        internal Color PreviewValidColor => previewValidColor;
        internal Color PreviewInvalidColor => previewInvalidColor;
        internal float BackgroundDistance => backgroundDistance;
        internal float BackgroundMargin => backgroundMargin;

        internal void Validate()
        {
            if (!float.IsFinite(cellSpacing) || cellSpacing <= 0 ||
                !float.IsFinite(cellScale) || cellScale <= 0 ||
                !float.IsFinite(modelScale) || modelScale <= 0 || !float.IsFinite(modelHeight) ||
                !float.IsFinite(boardCenter.x) || !float.IsFinite(boardCenter.y) || !float.IsFinite(boardCenter.z) ||
                !float.IsFinite(backgroundDistance) || backgroundDistance <= 0 ||
                !float.IsFinite(backgroundMargin) || backgroundMargin < 1)
                throw new InvalidOperationException("[ArtifactPopupConfig] Layout values must be finite; spacing and scales must be positive.");
        }
    }

    [CreateAssetMenu(fileName = "ArtifactPopupConfig", menuName = "ProjectL/Database/View/Artifact Popup Config")]
    public sealed class ArtifactPopupConfigDataSO : DatabaseItem
    {
        [SerializeField] private ArtifactPopupConfigData config = new();
        public override IData GetData() => config;
    }
}
