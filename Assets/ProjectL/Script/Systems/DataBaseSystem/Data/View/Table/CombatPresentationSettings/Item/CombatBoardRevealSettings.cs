using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item
{
    /// <summary>전투 진입 보드의 표시 시간과 높이입니다. 진행 상태는 보관하지 않습니다.</summary>
    [Serializable]
    internal sealed class CombatBoardRevealSettings
    {
        [SerializeField] private bool enabled = true;
        [Tooltip("Default는 기존 아군→적 순서, Coner는 필드 좌하단→우상단으로 묶음 크기를 늘리며 등장합니다.")]
        [SerializeField] private CombatBoardRevealMode mode = CombatBoardRevealMode.Default;
        [Tooltip("Coner 전용. 첫 묶음은 1개이며 차례마다 이 값만큼 늘린 후 소수점 아래를 버립니다. 1: 1·2·3, 2: 1·3·5, 0.5: 1·1·2·2, 0: 계속 1개.")]
        [SerializeField, Min(0f)] private float conerBatchGrowth = 1f;
        [Tooltip("첫 줄/묶음부터 마지막 줄/묶음까지 시작 시간을 분배하는 시간입니다. 0이면 모두 동시에 시작합니다.")]
        [SerializeField, Min(0f)] private float sweepSeconds = 1f;
        [Tooltip("타일 하나가 상승하고 안착하는 전체 시간입니다. 같은 줄/묶음의 타일은 함께 움직입니다.")]
        [SerializeField, Min(0f)] private float tileSeconds = .4f;
        [Tooltip("시작 깊이입니다. 필드 로컬 높이 단위입니다.")]
        [SerializeField, Min(0f)] private float depth = 1.5f;
        [Tooltip("완성 높이 위로 솟는 높이입니다. 필드 로컬 높이 단위입니다.")]
        [SerializeField, Min(0f)] private float overshoot = .15f;

        internal bool Enabled => enabled;
        internal CombatBoardRevealMode Mode => mode;
        internal float ConerBatchGrowth => conerBatchGrowth;
        internal float SweepSeconds => sweepSeconds;
        internal float TileSeconds => tileSeconds;
        internal float Depth => depth;
        internal float Overshoot => overshoot;

        internal void Validate()
        {
            if (!System.Enum.IsDefined(typeof(CombatBoardRevealMode), mode))
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "알 수 없는 보드 등장 모드입니다.");
            CombatPresentationValueRules.RequireNonNegative(conerBatchGrowth, nameof(conerBatchGrowth));
            CombatPresentationSettingsData.ValidateSeconds(sweepSeconds, nameof(sweepSeconds));
            CombatPresentationSettingsData.ValidateSeconds(tileSeconds, nameof(tileSeconds));
            CombatPresentationValueRules.RequireNonNegative(depth, nameof(depth));
            CombatPresentationValueRules.RequireNonNegative(overshoot, nameof(overshoot));
        }
    }
}
