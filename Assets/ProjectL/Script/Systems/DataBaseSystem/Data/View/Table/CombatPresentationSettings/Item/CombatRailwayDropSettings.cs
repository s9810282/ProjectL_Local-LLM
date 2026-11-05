using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item
{
    /// <summary>보드 완성 후 선로 조각의 낙하 수치입니다. 진행 상태는 보관하지 않습니다.</summary>
    [Serializable]
    internal sealed class CombatRailwayDropSettings
    {
        [SerializeField] private bool enabled = true;
        [Tooltip("완성 위치 위에서 시작하는 높이입니다(월드 단위). 필드 위쪽 축으로 떨어집니다.")]
        [SerializeField, Min(0f)] private float height = 10f;
        [Tooltip("각 선로 조각이 가속하며 떨어지는 시간입니다. 0이면 해당 조각 시작 때 즉시 착지합니다.")]
        [SerializeField, Min(0f)] private float seconds = .45f;
        [Tooltip("앞 조각과 다음 조각의 낙하 시작 간격입니다. 0이면 전체 조각이 동시에 시작합니다.")]
        [SerializeField, Min(0f)] private float segmentGapSeconds = .08f;

        internal bool Enabled => enabled;
        internal float Height => height;
        internal float Seconds => seconds;
        internal float SegmentGapSeconds => segmentGapSeconds;

        internal void Validate()
        {
            CombatPresentationValueRules.RequireNonNegative(height, nameof(height));
            CombatPresentationValueRules.RequireNonNegative(seconds, nameof(seconds));
            CombatPresentationValueRules.RequireNonNegative(segmentGapSeconds, nameof(segmentGapSeconds));
        }
    }
}
