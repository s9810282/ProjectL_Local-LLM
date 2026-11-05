using System;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.Rockfall.Item
{
    /// <summary>
    /// 낙석의 표현 값입니다(낙석 View 설계 RF5, 계획 RP-3·RP-6·RP-18). 칸·단계·맞은 유닛·피해는 Core 값이라 포함하지 않습니다.
    /// 터널 폭발 자리와 돌 출발 능선은 맵마다 달라 맵 프리팹 안의 오브젝트입니다(<c>RockfallTunnelBoom</c>, <c>RockfallRidgeStart</c>·<c>RockfallRidgeEnd</c>).
    /// </summary>
    internal sealed class RockfallVisualData : IData
    {
        /// <summary>떨어지는 돌입니다. 낙하 동안만 빌려 씁니다.</summary>
        public GameObject StonePrefab { get; }

        /// <summary>경고 칸에 눕혀 놓는 느낌표입니다. 색은 이미지 그대로이고 코드가 입히지 않습니다.</summary>
        public GameObject WarningMarkPrefab { get; }

        /// <summary>느낌표 판의 크기입니다(칸 대비 비율).</summary>
        public float WarningMarkSize { get; }

        /// <summary>느낌표를 칸 표시 높이에서 띄우는 거리입니다.</summary>
        public float WarningMarkLift { get; }

        /// <summary>능선이 없는 맵에서 돌이 떨어지기 시작하는 높이입니다(칸 표면 위 월드 거리). 능선이 있으면 능선 오브젝트의 높이를 씁니다.</summary>
        public float DropHeight { get; }

        /// <summary>돌 하나가 칸에 닿기까지의 시간입니다(초).</summary>
        public float DropSeconds { get; }

        /// <summary>돌과 돌 사이 시작 간격입니다(초). 0이면 함께 떨어집니다.</summary>
        public float DropIntervalSeconds { get; }

        /// <summary>돌의 출발 자리를 흩뜨리는 최대 거리입니다(월드 거리). 능선에서 출발할 때만 씁니다. 0이면 흩뜨리지 않습니다.</summary>
        public float DropStartScatter { get; }

        /// <summary>돌마다 프리팹 크기에 곱하는 배율의 최솟값입니다.</summary>
        public float StoneScaleMin { get; }

        /// <summary>돌마다 프리팹 크기에 곱하는 배율의 최댓값입니다.</summary>
        public float StoneScaleMax { get; }

        /// <summary>돌의 초당 회전 각도 최솟값입니다.</summary>
        public float SpinDegreesMin { get; }

        /// <summary>돌의 초당 회전 각도 최댓값입니다.</summary>
        public float SpinDegreesMax { get; }

        /// <summary>경고 시작의 이펙트·흔들림입니다. 이펙트 자리는 터널 자리입니다.</summary>
        internal CombatEventCues WarningCues { get; }

        /// <summary>돌 하나가 칸에 닿는 순간의 이펙트·흔들림입니다. 이펙트 자리는 그 칸입니다.</summary>
        internal CombatEventCues ImpactCues { get; }

        /// <summary>경고 시작의 효과음 프리셋입니다. -1이면 소리 없음입니다.</summary>
        public int WarningSoundPreset { get; }

        /// <summary>돌이 모두 착지한 때의 효과음 프리셋입니다. -1이면 소리 없음입니다.</summary>
        public int ImpactSoundPreset { get; }

        /// <summary>경고 칸 호버 설명의 이름입니다.</summary>
        public string DisplayName { get; }

        /// <summary>경고 칸 호버 설명입니다. <c>{n}</c> 자리에 피해 비율이 들어갑니다.</summary>
        public string Description { get; }

        /// <exception cref="InvalidOperationException">프리팹이 없거나 값이 범위를 벗어날 때입니다.</exception>
        /// <exception cref="ArgumentException">큐가 칸 규칙에 맞지 않을 때입니다.</exception>
        internal RockfallVisualData(GameObject stonePrefab, GameObject warningMarkPrefab, float warningMarkSize, float warningMarkLift,
            float dropHeight, float dropSeconds, float dropIntervalSeconds,
            float dropStartScatter, float stoneScaleMin, float stoneScaleMax, float spinDegreesMin, float spinDegreesMax,
            CombatEventCues warningCues, CombatEventCues impactCues,
            int warningSoundPreset, int impactSoundPreset, string displayName, string description)
        {
            if (stonePrefab == null)
                throw new InvalidOperationException("[RockfallVisualData] 돌 프리팹(stonePrefab)이 필요합니다.");
            if (warningMarkPrefab == null)
                throw new InvalidOperationException("[RockfallVisualData] 느낌표 프리팹(warningMarkPrefab)이 필요합니다.");
            RequirePositive(warningMarkSize, nameof(warningMarkSize));
            RequireNotNegative(warningMarkLift, nameof(warningMarkLift));
            RequirePositive(dropHeight, nameof(dropHeight));
            RequirePositive(dropSeconds, nameof(dropSeconds));
            RequireNotNegative(dropIntervalSeconds, nameof(dropIntervalSeconds));
            RequireNotNegative(dropStartScatter, nameof(dropStartScatter));
            RequirePositive(stoneScaleMin, nameof(stoneScaleMin));
            RequireMaxNotBelowMin(stoneScaleMin, stoneScaleMax, nameof(stoneScaleMax));
            RequireNotNegative(spinDegreesMin, nameof(spinDegreesMin));
            RequireMaxNotBelowMin(spinDegreesMin, spinDegreesMax, nameof(spinDegreesMax));
            if (warningCues == null) throw new ArgumentNullException(nameof(warningCues));
            if (impactCues == null) throw new ArgumentNullException(nameof(impactCues));
            CombatPresentationCueRules.Require(warningCues, CombatCueEvent.RockfallWarning, "[RockfallVisualData]");
            CombatPresentationCueRules.Require(impactCues, CombatCueEvent.RockfallImpact, "[RockfallVisualData]");
            CombatPresentationSettingsData.ValidateSoundPreset(warningSoundPreset, nameof(warningSoundPreset));
            CombatPresentationSettingsData.ValidateSoundPreset(impactSoundPreset, nameof(impactSoundPreset));

            StonePrefab = stonePrefab;
            WarningMarkPrefab = warningMarkPrefab;
            WarningMarkSize = warningMarkSize;
            WarningMarkLift = warningMarkLift;
            DropHeight = dropHeight;
            DropSeconds = dropSeconds;
            DropIntervalSeconds = dropIntervalSeconds;
            DropStartScatter = dropStartScatter;
            StoneScaleMin = stoneScaleMin;
            StoneScaleMax = stoneScaleMax;
            SpinDegreesMin = spinDegreesMin;
            SpinDegreesMax = spinDegreesMax;
            WarningCues = warningCues;
            ImpactCues = impactCues;
            WarningSoundPreset = warningSoundPreset;
            ImpactSoundPreset = impactSoundPreset;
            DisplayName = displayName ?? string.Empty;
            Description = description ?? string.Empty;
        }

        private static void RequirePositive(float value, string name)
        {
            if (!float.IsFinite(value) || value <= 0f)
                throw new InvalidOperationException($"[RockfallVisualData] {name}은(는) 유한한 양수여야 합니다: {value}");
        }

        private static void RequireNotNegative(float value, string name)
        {
            if (!float.IsFinite(value) || value < 0f)
                throw new InvalidOperationException($"[RockfallVisualData] {name}은(는) 0 이상의 유한한 값이어야 합니다: {value}");
        }

        private static void RequireMaxNotBelowMin(float min, float max, string name)
        {
            if (!float.IsFinite(max) || max < min)
                throw new InvalidOperationException($"[RockfallVisualData] {name}은(는) 최솟값 {min} 이상의 유한한 값이어야 합니다: {max}");
        }
    }

    [CreateAssetMenu(fileName = "NewRockfallVisualData", menuName = "ProjectL/Database/View/RockfallVisualData")]
    internal sealed class RockfallVisualDataSO : DatabaseItem
    {
        [Header("돌")]
        [Tooltip("떨어지는 돌 프리팹입니다. 충돌체를 두지 않습니다. 필수이며, 비우면 DB를 만들 때 실패합니다.")]
        [SerializeField] private GameObject stonePrefab;
        [Tooltip("능선이 없는 맵에서 돌이 떨어지기 시작하는 높이입니다(칸 표면 위 월드 거리). 능선이 있으면 맵 프리팹의 RockfallRidgeStart·RockfallRidgeEnd 높이를 씁니다.")]
        [SerializeField, Min(.001f)] private float dropHeight = 8f;
        [Tooltip("돌 하나가 칸에 닿기까지의 시간(초)입니다.")]
        [SerializeField, Min(.001f)] private float dropSeconds = .5f;
        [Tooltip("돌과 돌 사이 시작 간격(초)입니다. 순서는 섞이고, 유닛이 맞는 칸의 돌은 마지막에 함께 떨어집니다. 0이면 모두 함께 떨어집니다.")]
        [SerializeField, Min(0f)] private float dropIntervalSeconds = .1f;
        [Tooltip("돌의 출발 자리를 흩뜨리는 최대 거리입니다(월드 거리). 능선에서 출발할 때만 씁니다. 0이면 흩뜨리지 않습니다.")]
        [SerializeField, Min(0f)] private float dropStartScatter = 1.5f;
        [Tooltip("돌마다 프리팹 크기에 곱하는 배율의 최솟값입니다.")]
        [SerializeField, Min(.001f)] private float stoneScaleMin = .7f;
        [Tooltip("돌마다 프리팹 크기에 곱하는 배율의 최댓값입니다. 최솟값 이상이어야 합니다.")]
        [SerializeField, Min(.001f)] private float stoneScaleMax = 1.3f;
        [Tooltip("돌의 초당 회전 각도 최솟값입니다.")]
        [SerializeField, Min(0f)] private float spinDegreesMin = 180f;
        [Tooltip("돌의 초당 회전 각도 최댓값입니다. 최솟값 이상이어야 합니다.")]
        [SerializeField, Min(0f)] private float spinDegreesMax = 540f;

        [Header("경고 칸 느낌표")]
        [Tooltip("경고 칸에 눕혀 놓는 느낌표 프리팹입니다. 색은 이미지 그대로이니 경고 색(FieldView)을 바꾸면 이미지도 함께 바꿉니다. 필수입니다.")]
        [SerializeField] private GameObject warningMarkPrefab;
        [Tooltip("느낌표 판의 크기입니다(칸 대비 비율). 지금 이미지는 느낌표가 판 높이의 약 66%라 0.9면 칸의 약 60%입니다.")]
        [SerializeField, Min(.001f)] private float warningMarkSize = .9f;
        [Tooltip("느낌표를 칸 표시 높이에서 띄우는 거리입니다.")]
        [SerializeField, Min(0f)] private float warningMarkLift = .01f;

        [Header("연출 큐")]
        [Tooltip("경고 시작의 이펙트·흔들림입니다. 시점은 Start만, 이펙트 자리는 EventCell(= 터널 자리, 회전은 터널 방향)만 됩니다. 터널 자리가 없는 스테이지에서는 내지 않습니다.")]
        [SerializeField] private CombatVisualCueInput warningCues = new();
        [Tooltip("돌 하나가 칸에 닿는 순간의 이펙트·흔들림입니다. 시점은 End만, 이펙트 자리는 EventCell(= 그 칸)만 됩니다.")]
        [SerializeField] private CombatVisualCueInput impactCues = new();

        [Header("소리")]
        [Tooltip("경고 시작의 효과음 프리셋 번호입니다. -1이면 소리를 내지 않습니다.")]
        [SerializeField, Min(-1)] private int warningSoundPreset = -1;
        [Tooltip("돌이 모두 착지한 때의 효과음 프리셋 번호입니다. 칸마다 내지 않고 한 번만 냅니다. -1이면 소리를 내지 않습니다.")]
        [SerializeField, Min(-1)] private int impactSoundPreset = -1;

        [Header("호버 설명")]
        [SerializeField] private string displayName;
        [Tooltip("경고 칸 호버 설명입니다. {n} 자리에 피해 비율(%)이 들어갑니다.")]
        [TextArea(2, 6)]
        [SerializeField] private string description;

        public override IData GetData() => new RockfallVisualData(stonePrefab, warningMarkPrefab, warningMarkSize, warningMarkLift,
            dropHeight, dropSeconds, dropIntervalSeconds,
            dropStartScatter, stoneScaleMin, stoneScaleMax, spinDegreesMin, spinDegreesMax,
            (warningCues ?? throw new InvalidOperationException("[RockfallVisualDataSO] 경고 큐 칸이 없습니다.")).ToCues(),
            (impactCues ?? throw new InvalidOperationException("[RockfallVisualDataSO] 충돌 큐 칸이 없습니다.")).ToCues(),
            warningSoundPreset, impactSoundPreset, displayName, description);
    }
}
