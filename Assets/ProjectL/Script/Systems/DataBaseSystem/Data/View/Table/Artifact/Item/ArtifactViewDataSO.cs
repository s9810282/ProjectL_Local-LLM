using System;
using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.Artifact.Item
{
    /// <summary>아티팩트 표현 데이터를 입력하고 조회마다 새 읽기 전용 데이터를 만듭니다(DB 규칙 P2).</summary>
    [CreateAssetMenu(
        fileName = "ArtifactViewData",
        menuName = "ProjectL/Database/View/Artifact View Data")]
    public sealed class ArtifactViewDataSO : DatabaseItem
    {
        // 칸 이름은 옛 직렬화 경로 그대로라 에셋 값이 유지됩니다.
        [SerializeField]
        private Input artifactViewData = new();

        public override IData GetData() => artifactViewData.ToData();

        [Serializable]
        private sealed class Input
        {
            [Header("Identity")]
            [SerializeField, Min(0)]
            private int artifactId;

            [Header("UI")]
            [SerializeField]
            private string displayName;

            [SerializeField, TextArea(3, 8)]
            private string description;

            [SerializeField]
            private Sprite icon;

            [Header("World")]
            [Tooltip("아티팩트 판에 놓은 모델과 끄는 동안 보이는 모델의 프리팹입니다. 필수이며, 비우면 DB를 만들 때 예외입니다.")]
            [SerializeField]
            private GameObject prefab;

            internal ArtifactViewData ToData() => new(artifactId, displayName, description, icon, prefab);
        }
    }
}
