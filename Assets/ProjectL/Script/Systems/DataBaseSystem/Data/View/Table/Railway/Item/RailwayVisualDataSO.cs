using System;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Effect.Enum;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.Railway.Item
{
    /// <summary>
    /// 레일웨이 기믹의 연출 리소스와 크기입니다. 발동 시점과 피해 규칙은 InGame(CombatStageDetail.Railway)이 가집니다.
    /// </summary>
    [Serializable]
    internal sealed class RailwayVisualData : IData
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject trainPrefab;
        [SerializeField] private GameObject railPrefab;
        [SerializeField] private GameObject railTilePrefab;

        [Header("Animation")]
        [Tooltip("열차 이동 애니메이터입니다. 비워도 됩니다.")]
        [SerializeField] private RuntimeAnimatorController controller;
        [Tooltip("열차 이동 클립입니다. 비워도 됩니다.")]
        [SerializeField] private AnimationClip clip;
        [Tooltip("통과할 때 재생할 Render 이펙트입니다. Empty면 재생하지 않습니다.")]
        [SerializeField] private EffectType effect = EffectType.Empty;

        [Header("Motion")]
        [Tooltip("애니메이션 원본의 마지막 Z 위치입니다. 맵 길이에 맞춰 정규화합니다.")]
        [SerializeField] private float clipEndZ = -246.8396f;
        [Tooltip("화면 밖 진입·퇴장 구간을 포함한 전체 재생 시간(초)입니다.")]
        [SerializeField, Min(0.1f)] private float duration = 8f;
        [SerializeField, Min(0.1f)] private float trainWidthInCells = 0.8f;
        [SerializeField, Min(0.1f)] private float outsideCells = 2f;
        [Tooltip("바깥 선로 끝에서 열차 생성·종료 지점을 더 멀리 두는 거리(칸)입니다.")]
        [SerializeField, Min(0f)] private float travelPaddingCells = 20f;
        [Tooltip("이동 방향은 유지하고 열차 외형만 Y축으로 회전합니다. 앞뒤가 반대인 모델은 180도입니다.")]
        [SerializeField] private float trainModelYaw = 180f;
        [SerializeField] private bool reverse;

        public GameObject TrainPrefab => trainPrefab;
        public GameObject RailPrefab => railPrefab;
        public GameObject RailTilePrefab => railTilePrefab;
        /// <summary>비어 있으면 null입니다.</summary>
        public RuntimeAnimatorController Controller => controller;
        /// <summary>비어 있으면 null입니다.</summary>
        public AnimationClip Clip => clip;
        public EffectType Effect => effect;
        public float ClipEndZ => clipEndZ;
        public float Duration => duration;
        public float TrainWidthInCells => trainWidthInCells;
        public float OutsideCells => outsideCells;
        public float TravelPaddingCells => travelPaddingCells;
        public float TrainModelYaw => trainModelYaw;
        public bool Reverse => reverse;

        internal void Validate()
        {
            if (trainPrefab == null || railPrefab == null || railTilePrefab == null)
                throw new InvalidOperationException("[RailwayVisualData] Train, Rail and Rail Tile prefabs are required.");
            if (!System.Enum.IsDefined(typeof(EffectType), effect))
                throw new InvalidOperationException("[RailwayVisualData] Effect is invalid.");
            if (!float.IsFinite(clipEndZ) || Mathf.Abs(clipEndZ) < 0.001f ||
                !float.IsFinite(duration) || duration <= 0f ||
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
