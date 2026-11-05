using System;
using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.Artifact.Item
{
    /// <summary>아티팩트를 화면에 표시하는 데 사용하는 데이터입니다. 조회마다 새로 만드는 읽기 전용 값입니다.</summary>
    /// <remarks>같은 폴더의 다른 Data처럼 internal입니다(로비 리팩토링 2차 설계 N-74).</remarks>
    internal sealed class ArtifactViewData : IData
    {
        internal ArtifactViewData(int artifactId, string displayName, string description, Sprite icon, GameObject prefab)
        {
            ArtifactId = artifactId;
            DisplayName = displayName;
            Description = description ?? string.Empty;
            Icon = icon;
            Prefab = prefab;
        }

        internal int ArtifactId { get; }
        internal string DisplayName { get; }
        internal string Description { get; }
        /// <summary>목록 아이콘입니다. 읽는 화면이 아직 없어 선택 칸입니다(로비 리팩토링 2차 설계 N-29).</summary>
        internal Sprite Icon { get; }
        /// <summary>아티팩트 판·끌기 표시에 쓰는 모델입니다. 필수입니다(로비 재평가 R-M-03).</summary>
        internal GameObject Prefab { get; }

        internal void Validate()
        {
            if (ArtifactId < 0)
            {
                throw new InvalidOperationException(
                    "[ArtifactViewData] ArtifactId는 0 이상이어야 합니다.");
            }

            if (string.IsNullOrWhiteSpace(DisplayName))
            {
                throw new InvalidOperationException(
                    $"[ArtifactViewData] 표시 이름이 필요합니다. ID: {ArtifactId}");
            }

            ViewPrefabRules.RequirePrefabAsset(Prefab, $"[ArtifactViewData] ID {ArtifactId} 프리팹");
        }
    }
}
