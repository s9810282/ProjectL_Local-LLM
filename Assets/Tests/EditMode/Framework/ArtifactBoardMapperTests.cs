using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Artifact.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactPopup;
using UnityEngine;
using UnityEditor;

namespace ProjectL.Tests.Framework
{
    public sealed class ArtifactBoardMapperTests
    {
        [Test]
        public void ActualFloor_ConfiguredCellsStayOnSamePlacementSurface()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Artifacts/ArtifactFloor.prefab");
            var config = (ArtifactPopupConfigData)AssetDatabase.LoadAssetAtPath<ArtifactPopupConfigDataSO>("Assets/ProjectL/DataBaseSO/ViewGroup/Artifact/Item/ArtifactPopupConfig_Default.asset").GetData();
            var floor = Object.Instantiate(prefab);
            try
            {
                floor.transform.localScale = prefab.transform.localScale * config.CellScale;
                var mesh = floor.GetComponentInChildren<MeshFilter>();
                var collider = mesh.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh.sharedMesh;
                Physics.SyncTransforms();
                Assert.That(collider.Raycast(new Ray(new Vector3(0, 20, 0), Vector3.down), out var center, 40), Is.True);
                Debug.Log($"ArtifactFloor center surface: {center.point}; configured center: {config.BoardCenter}");
                Assert.That(config.BoardCenter.y, Is.EqualTo(center.point.y).Within(.05f), "입력 평면과 모델 배치 면의 높이는 같아야 합니다.");
                for (int y = 0; y < 4; y++)
                    for (int x = 0; x < 4; x++)
                    {
                        Vector3 point = config.BoardCenter + new Vector3((x - 1.5f) * config.CellSpacing, 20, (y - 1.5f) * config.CellSpacing);
                        Assert.That(collider.Raycast(new Ray(point, Vector3.down), out var hit, 40), Is.True);
                        Assert.That(hit.point.y, Is.EqualTo(center.point.y).Within(.05f), $"칸 ({x},{y})이 벽·계단 위가 아닌 배치 면에 있어야 합니다.");
                    }
            }
            finally { Object.DestroyImmediate(floor); }
        }

        [Test]
        public void RayMapping_UsesSameCenteredPlane_AsDisplay_AndAllowsOutsideCandidates()
        {
            var obj = new GameObject("Board");
            try
            {
                obj.transform.SetPositionAndRotation(new Vector3(3, 2, 1), Quaternion.Euler(0, 35, 0));
                var mapper = new ArtifactBoardMapper(obj.transform, new ArtifactPopupConfigData());
                Vector3 point = mapper.CellPosition(1, 2, 4, 4);
                var ray = new Ray(obj.transform.TransformPoint(point) + obj.transform.up * 10, -obj.transform.up);
                Assert.That(mapper.TryMap(ray, 4, 4, out var cell), Is.True);
                Assert.That(cell, Is.EqualTo(new CellCoord(1, 2)));
                Assert.That(ArtifactBoardMapper.Contains(new CellCoord(-1, 2), 4, 4), Is.False);
                Assert.That(mapper.TryMap(new Ray(obj.transform.position + Vector3.up * 10, Vector3.up), 4, 4, out _), Is.False);
            }
            finally { Object.DestroyImmediate(obj); }
        }
    }
}
