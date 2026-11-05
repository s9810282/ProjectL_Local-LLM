using System;
using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.Artifact.Item
{
    /// <summary>아티팩트 3D 보드의 표현 값을 입력하고 조회마다 새 읽기 전용 데이터를 만듭니다(DB 규칙 P2).</summary>
    [CreateAssetMenu(fileName = "ArtifactPopupConfig", menuName = "ProjectL/Database/View/Artifact Popup Config")]
    public sealed class ArtifactPopupConfigDataSO : DatabaseItem
    {
        // 칸 이름은 옛 직렬화 경로 그대로라 에셋 값이 유지됩니다.
        [SerializeField] private Input config = new();

        public override IData GetData() => config.ToData();

        [Serializable]
        private sealed class Input
        {
            [SerializeField, Min(0.01f)] private float cellSpacing = 1.2f;
            [SerializeField, Min(0.01f)] private float cellScale = 1f;
            [SerializeField, Min(0.01f)] private float modelScale = 1f;
            [SerializeField] private float modelHeight = 0.15f;
            [Tooltip("배치판 격자(칸 위치·입력 평면)의 중심입니다. 격자만 옮기고 바닥 모델은 옮기지 않습니다. 바닥은 4×4 판에 맞춘 고정 아트로 판 루트 원점에 놓입니다.")]
            [SerializeField] private Vector3 boardCenter;
            [SerializeField] private Color previewValidColor = new Color(.15f, .8f, .65f, .65f);
            [SerializeField] private Color previewInvalidColor = new Color(.95f, .2f, .2f, .65f);
            [Tooltip("팝업이 열린 동안 출력 카메라 마스크에 더할 층 이름입니다. 보유 목록 UI가 이 층에 있어야 보입니다.")]
            [SerializeField] private string outputLayerName = "UI";

            internal ArtifactPopupConfigData ToData() => new(cellSpacing, cellScale, modelScale, modelHeight,
                boardCenter, previewValidColor, previewInvalidColor, outputLayerName);
        }
    }
}
