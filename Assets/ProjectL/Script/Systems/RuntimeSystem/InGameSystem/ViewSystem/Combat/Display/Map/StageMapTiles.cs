using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageViewProp.Item;
using ProjectL.Tool.Runtime;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map
{
    /// <summary>
    /// 배경 구멍을 채우는 바닥 타일을 세웁니다(배경 방식 PrefabWithTiles). 부르는 곳은 배경 <see cref="StageViewPropPresenter"/> 하나이고,
    /// 타일 루트는 배경 아래에 있어 배경과 함께 없어집니다.
    /// </summary>
    /// <remarks>
    /// 타일 루트(<c>MapTileRoot</c>)는 배경 인스턴스 아래 자식으로, 배경 로컬 <see cref="CombatMapTilesData.Offset"/>에 회전 없이·배율 1로 놓여
    /// 배경의 회전·배율을 그대로 받습니다(타일 맞춤 설계 TA1 2026-10-28 개정 5). 타일 격자 가운데는 루트 원점(<see cref="CombatMapTilesData.GridCenter"/>)입니다.
    /// 타일은 풀 없이 한 번 만들고, 그 뒤 배경은 켜짐을 건드리지 않습니다. 바닥 교체 칸의 타일을 끄고 켜는 것은 지형 표시
    /// (<see cref="FieldTileReplacement"/>)입니다(타일 교체 설계 TR4).
    /// 지형 표시는 바닥 교체 외형을 쓰는 동안 타일 루트 아래(끈 타일 자리)에 두고 반환 때 지형 루트로 되돌립니다(타일 교체 계획 P11).
    /// 그래서 배경을 지형 표시보다 먼저 없애면 쓰는 중인 외형도 함께 없어집니다. 지금 씬 해제는 표시를 먼저 놓고 배경을 놓습니다(묶음 끝 검토 L6).
    /// </remarks>
    internal static class StageMapTiles
    {
        private const string RootName = "MapTileRoot";

        /// <summary>
        /// 배경 아래에 타일 루트를 <see cref="CombatMapTilesData.Offset"/> 자리에 만들고, 칸마다 그 종류의 프리팹을 하나씩 만듭니다.
        /// 칸 자리는 루트 기준 <see cref="CombatMapTilesData.CenterOf"/>이고, 회전은 <see cref="CombatMapTilesData.RotationAt"/>입니다.
        /// </summary>
        /// <param name="placed">만든 타일입니다. 타일 맵 칸 [가로, 세로]이고 크기는 <see cref="CombatMapTilesData.Columns"/> × <see cref="CombatMapTilesData.Rows"/>입니다.</param>
        /// <returns>만든 타일 루트입니다.</returns>
        internal static GameObject Build(Transform backdrop, CombatMapTilesData tiles, out Transform[,] placed)
        {
            if (backdrop == null)
                throw new ArgumentNullException(nameof(backdrop));
            if (tiles == null)
                throw new ArgumentNullException(nameof(tiles));

            var root = new GameObject(RootName);
            Transform rootTransform = root.transform;
            rootTransform.SetParent(backdrop, false);
            rootTransform.localPosition = tiles.Offset;
            placed = new Transform[tiles.Columns, tiles.Rows];
            int seed = Guid.NewGuid().GetHashCode();
            bool square = Mathf.Approximately(tiles.TileSize.x, tiles.TileSize.y);
            for (int row = 0; row < tiles.Rows; row++)
            {
                for (int column = 0; column < tiles.Columns; column++)
                {
                    CombatMapTileKind kind = tiles.KindAt(column, row);
                    CombatMapTileVisual visual = tiles.VisualOf(kind);
                    int candidate = MapTileRandomSelection.PrefabIndex(seed, column, row, (int)kind, visual.PrefabCount);
                    GameObject tile = Object.Instantiate(visual.PrefabAt(candidate), rootTransform, false);
                    tile.name = $"Tile_{column}_{row}";
                    tile.transform.localPosition = tiles.CenterOf(column, row);
                    // 칸 회전은 프리팹 자기 회전 뒤에 타일 루트 위 방향으로 곱합니다(피벗 중심, SO 모드 설계 §13 TT2).
                    CombatMapTileRotation rotation = tiles.RotationAt(column, row);
                    int turns = rotation == CombatMapTileRotation.Random
                        ? MapTileRandomSelection.RotationQuarterTurns(seed, column, row, (int)kind, square)
                        : (int)rotation;
                    tile.transform.localRotation = Quaternion.Euler(0f, turns * 90f, 0f) * tile.transform.localRotation;
                    placed[column, row] = tile.transform;
                }
            }
            return root;
        }
    }
}
