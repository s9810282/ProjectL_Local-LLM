using System;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Effect.Enum;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.Railway.Item
{
    /// <summary>
    /// 레일웨이 기믹의 연출 리소스와 크기입니다. 발동 시점과 피해 규칙은 InGame(CombatStageDetail.Railway)이 가집니다.
    /// </summary>
    [Serializable]
    internal sealed class RailwayVisualData : IData
    {
        [Header("Prefabs")]
        [Tooltip("열차 맨 앞 칸입니다. 앞면(Front Contact)이 이동·접촉·카메라 시각의 기준입니다.")]
        [FormerlySerializedAs("trainPrefab")]
        [SerializeField] private GameObject trainHeadPrefab;
        [Tooltip("Head 뒤에 이어 붙이는 몸통 칸입니다. Body Count가 0이면 비워도 됩니다.")]
        [SerializeField] private GameObject trainBodyPrefab;
        [Tooltip("Head 뒤에 붙일 몸통 칸 수입니다. 0이면 Head만 지나갑니다.")]
        [SerializeField, Min(0)] private int trainBodyCount;
        [SerializeField] private GameObject railPrefab;
        [Tooltip("선로 바닥입니다. 비워 두면 바닥 타일 없이 선로만 표시합니다.")]
        [SerializeField] private GameObject railTilePrefab;

        [Header("Static Height")]
        [Tooltip("선로 바닥 피벗의 필드 기준 높이입니다.")]
        [SerializeField] private float railTileYOffset = 0.002f;
        [Tooltip("선로 피벗의 필드 기준 높이입니다.")]
        [SerializeField] private float railYOffset = 0.045f;
        [Tooltip("각 직선의 첫 칸과 마지막 칸에서 표시용 선로를 각각 더 만드는 개수입니다. 열차 이동 여유와 별개입니다.")]
        [SerializeField, Min(0)] private int railExtensionCells = 5;

        [Header("Animation")]
        [Tooltip("Head 칸 외형 애니메이터입니다. 이동은 Duration 기준 등속(코드)이며 비워도 됩니다.")]
        [SerializeField] private RuntimeAnimatorController controller;
        [Tooltip("Head 칸 외형 클립입니다. 루트 이동은 적용하지 않으며 비워도 됩니다.")]
        [SerializeField] private AnimationClip clip;
        [Tooltip("통과할 때 재생할 Render 이펙트입니다. Empty면 재생하지 않습니다.")]
        [SerializeField] private EffectType effect = EffectType.Empty;

        [Header("Motion")]
        [Tooltip("이전 에셋 호환용 보존 값입니다. 등속 이동과 접촉 예약에는 사용하지 않습니다.")]
        [SerializeField] private float clipEndZ = -246.8396f;
        [Tooltip("화면 밖 진입·퇴장 구간을 포함한 전체 재생 시간(초)입니다.")]
        [SerializeField, Min(0.1f)] private float duration = 8f;
        [SerializeField, Min(0.1f)] private float trainWidthInCells = 0.8f;
        [Tooltip("보드 경계 밖 열차 이동 여유(칸)입니다. 선로 외형의 연장 개수는 Rail Extension Cells로 정합니다.")]
        [SerializeField, Min(0.1f)] private float outsideCells = 2f;
        [Tooltip("Outside Cells에 더하는 열차 진입·퇴장 여유 거리(칸)입니다. 선로 외형 개수에는 영향을 주지 않습니다.")]
        [SerializeField, Min(0f)] private float travelPaddingCells = 20f;
        [Tooltip("이동 방향은 유지하고 열차 외형만 Y축으로 회전합니다. 앞뒤가 반대인 모델은 180도입니다.")]
        [SerializeField] private float trainModelYaw = 180f;
        [SerializeField] private bool reverse;

        public GameObject TrainHeadPrefab => trainHeadPrefab;
        /// <summary>Body Count가 0이면 null일 수 있습니다.</summary>
        public GameObject TrainBodyPrefab => trainBodyPrefab;
        public int TrainBodyCount => trainBodyCount;
        public GameObject RailPrefab => railPrefab;
        /// <summary>선로 바닥을 표시하지 않으면 null입니다.</summary>
        public GameObject RailTilePrefab => railTilePrefab;
        public float RailTileYOffset => railTileYOffset;
        public float RailYOffset => railYOffset;
        public int RailExtensionCells => railExtensionCells;
        /// <summary>비어 있으면 null입니다.</summary>
        public RuntimeAnimatorController Controller => controller;
        /// <summary>비어 있으면 null입니다.</summary>
        public AnimationClip Clip => clip;
        public EffectType Effect => effect;
        /// <summary>이전 직렬화 값의 호환용입니다. 열차 이동·접촉 계산에는 사용하지 않습니다.</summary>
        public float ClipEndZ => clipEndZ;
        public float Duration => duration;
        public float TrainWidthInCells => trainWidthInCells;
        public float OutsideCells => outsideCells;
        public float TravelPaddingCells => travelPaddingCells;
        public float TrainModelYaw => trainModelYaw;
        public bool Reverse => reverse;

        internal void Validate()
        {
            if (trainHeadPrefab == null || railPrefab == null)
                throw new InvalidOperationException("[RailwayVisualData] Train Head와 Rail Prefab이 필요합니다.");
            if (trainBodyCount < 0)
                throw new InvalidOperationException("[RailwayVisualData] Train Body Count는 0 이상이어야 합니다.");
            if (trainBodyCount > 0 && trainBodyPrefab == null)
                throw new InvalidOperationException("[RailwayVisualData] Train Body Count가 1 이상이면 Train Body Prefab이 필요합니다.");
            if (!System.Enum.IsDefined(typeof(EffectType), effect))
                throw new InvalidOperationException("[RailwayVisualData] Effect is invalid.");
            if (!float.IsFinite(railTileYOffset) || !float.IsFinite(railYOffset))
                throw new InvalidOperationException("[RailwayVisualData] 선로 높이는 유한한 값이어야 합니다.");
            if (railExtensionCells < 0)
                throw new InvalidOperationException("[RailwayVisualData] Rail Extension Cells는 0 이상이어야 합니다.");
            if (!float.IsFinite(duration) || duration <= 0f ||
                !float.IsFinite(trainWidthInCells) || trainWidthInCells <= 0f ||
                !float.IsFinite(outsideCells) || outsideCells <= 0f ||
                !float.IsFinite(travelPaddingCells) || travelPaddingCells < 0f ||
                !float.IsFinite(trainModelYaw))
                throw new InvalidOperationException("[RailwayVisualData] Motion values are invalid.");
        }
    }

    [CreateAssetMenu(fileName = "NewRailwayVisualData", menuName = "ProjectL/Database/View/RailwayVisualData")]
    internal sealed class RailwayVisualDataSO : DatabaseItem
    {
        [SerializeField] private RailwayVisualData railwayVisualData = new();

        public override IData GetData() => railwayVisualData;
    }
}
