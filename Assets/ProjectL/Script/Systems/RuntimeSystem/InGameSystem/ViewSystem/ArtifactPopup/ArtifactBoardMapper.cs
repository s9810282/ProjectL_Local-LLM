using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Artifact.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactPopup
{
    /// <summary>표시와 입력이 공유하는 중심 기준 XZ 평면 좌표입니다.</summary>
    internal sealed class ArtifactBoardMapper
    {
        private readonly Transform _root;
        private readonly ArtifactPopupConfigData _config;

        internal ArtifactBoardMapper(Transform root, ArtifactPopupConfigData config)
        {
            _root = root != null ? root : throw new ArgumentNullException(nameof(root));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        internal bool TryMap(Ray ray, int width, int height, out CellCoord cell)
        {
            cell = default;
            var localRay = new Ray(_root.InverseTransformPoint(ray.origin), _root.InverseTransformVector(ray.direction));
            var plane = new Plane(Vector3.up, _config.BoardCenter);
            if (!plane.Raycast(localRay, out float distance)) return false;
            Vector3 point = localRay.GetPoint(distance) - _config.BoardCenter;
            cell = new CellCoord(Mathf.FloorToInt(point.x / _config.CellSpacing + width * .5f),
                Mathf.FloorToInt(point.z / _config.CellSpacing + height * .5f));
            return true;
        }

        internal Vector3 CellPosition(float x, float y, int width, int height, float heightOffset = 0)
        {
            return _config.BoardCenter + new Vector3((x - (width - 1) * .5f) * _config.CellSpacing,
                heightOffset, (y - (height - 1) * .5f) * _config.CellSpacing);
        }

        internal static bool Contains(CellCoord cell, int width, int height)
        {
            return cell.X >= 0 && cell.Y >= 0 && cell.X < width && cell.Y < height;
        }
    }
}
