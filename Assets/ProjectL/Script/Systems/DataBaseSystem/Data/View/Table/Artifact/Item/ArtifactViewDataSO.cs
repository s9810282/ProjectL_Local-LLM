using System;
using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.Artifact.Item
{
    /// <summary>
    /// 아티팩트를 화면에 표시하는 데 사용하는 데이터입니다.
    /// </summary>
    [Serializable]
    public sealed class ArtifactViewData : IData
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
        [Tooltip("배치 화면 등에서 사용할 프리팹, 필요 없으면 비워 둠")]
        [SerializeField]
        private GameObject prefab;

        public int ArtifactId => artifactId;
        public string DisplayName => displayName;
        public string Description => description ?? string.Empty;
        public Sprite Icon => icon;
        public GameObject Prefab => prefab;

        public void Validate()
        {
            if (artifactId < 0)
            {
                throw new InvalidOperationException(
                    "[ArtifactViewData] ArtifactId는 0 이상이어야 합니다.");
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new InvalidOperationException(
                    $"[ArtifactViewData] 표시 이름이 필요합니다. ID: {artifactId}");
            }

            if (icon == null)
            {
                throw new InvalidOperationException(
                    $"[ArtifactViewData] 아이콘이 필요합니다. ID: {artifactId}");
            }

            if (prefab != null && prefab.scene.IsValid())
            {
                throw new InvalidOperationException(
                    $"[ArtifactViewData] 씬 오브젝트 대신 프리팹 에셋을 지정해야 합니다. ID: {artifactId}");
            }
        }
    }

    /// <summary>
    /// 아티팩트 표현 데이터를 입력하는 SO입니다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ArtifactViewData",
        menuName = "ProjectL/Database/View/Artifact View Data")]
    public sealed class ArtifactViewDataSO : DatabaseItem
    {
        [SerializeField]
        private ArtifactViewData artifactViewData = new();

        public override IData GetData() => artifactViewData;
    }
}
