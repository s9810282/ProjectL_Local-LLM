using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactPopup
{
    /// <summary>팝업에 그릴 아이템 하나의 값입니다. Presenter는 이 값만 보고 그립니다(아티팩트 팝업 계획 D17).</summary>
    internal readonly struct ArtifactPopupItem
    {
        internal int ArtifactId { get; }
        internal int InstanceId { get; }
        /// <summary>배치 아이템은 보드 절대 셀, Pool 아이템은 정의 기준 로컬 셀입니다. 다시 회전·이동하지 않습니다.</summary>
        internal IReadOnlyList<CellCoord> Cells { get; }
        /// <summary>View DB 아이콘입니다. 외형이 등록되지 않았으면 null입니다.</summary>
        internal Sprite Icon { get; }
        internal GameObject ModelPrefab { get; }
        internal Rotation4 Rotation { get; }
        internal string DisplayName { get; }
        internal string Description { get; }

        internal ArtifactPopupItem(int artifactId, int instanceId, IReadOnlyList<CellCoord> cells, Sprite icon,
            GameObject modelPrefab = null, Rotation4 rotation = Rotation4.R0, string displayName = "", string description = "")
        {
            ArtifactId = artifactId;
            InstanceId = instanceId;
            Cells = cells ?? throw new ArgumentNullException(nameof(cells));
            Icon = icon;
            ModelPrefab = modelPrefab;
            Rotation = rotation;
            DisplayName = displayName;
            Description = description;
        }
    }

    /// <summary>팝업에 그릴 보드 전체의 값입니다. 크기는 Buffer의 현재 크기입니다(D6).</summary>
    internal sealed class ArtifactPopupBoard
    {
        internal int Width { get; }
        internal int Height { get; }
        internal IReadOnlyList<ArtifactPopupItem> PlacedItems { get; }
        internal IReadOnlyList<ArtifactPopupItem> PoolItems { get; }

        internal ArtifactPopupBoard(int width, int height,
            IReadOnlyList<ArtifactPopupItem> placedItems, IReadOnlyList<ArtifactPopupItem> poolItems)
        {
            if (width < 1) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 1) throw new ArgumentOutOfRangeException(nameof(height));
            Width = width;
            Height = height;
            PlacedItems = placedItems ?? throw new ArgumentNullException(nameof(placedItems));
            PoolItems = poolItems ?? throw new ArgumentNullException(nameof(poolItems));
        }
    }
}
