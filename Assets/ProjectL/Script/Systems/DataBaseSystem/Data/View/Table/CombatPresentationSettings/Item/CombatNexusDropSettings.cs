using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item
{
    /// <summary>첫 적 Nexus의 낙하와 착지 눌림 수치입니다. 진행 상태는 보관하지 않습니다.</summary>
    [Serializable]
    internal sealed class CombatNexusDropSettings
    {
        [SerializeField] private bool enabled = true;
        [Tooltip("완성 위치보다 위에서 시작하는 높이입니다(월드 단위). 필드 위쪽 축으로 떨어집니다.")]
        [SerializeField, Min(0f)] private float height = 10f;
        [Tooltip("Nexus가 가속하며 떨어지는 시간입니다. 0이면 즉시 착지하고 이펙트를 냅니다.")]
        [SerializeField, Min(0f)] private float seconds = .6f;

        [Header("착지 때 건물·바닥 눌림")]
        [Tooltip("착지한 Nexus와 점유 칸 아래 바닥이 함께 눌리는 깊이입니다(월드 단위). 0이면 눌림을 건너뜁니다.")]
        [SerializeField, Min(0f)] private float impactDepth = .35f;
        [Tooltip("착지 직후 가장 낮은 위치까지 내려가는 시간입니다. 0이면 즉시 눌립니다.")]
        [SerializeField, Min(0f)] private float impactDownSeconds = .08f;
        [Tooltip("눌린 위치에서 원래 높이로 돌아오는 시간입니다. 0이면 즉시 복구합니다.")]
        [SerializeField, Min(0f)] private float impactReturnSeconds = .22f;

        internal bool Enabled => enabled;
        internal float Height => height;
        internal float Seconds => seconds;
        internal float ImpactDepth => impactDepth;
        internal float ImpactDownSeconds => impactDownSeconds;
        internal float ImpactReturnSeconds => impactReturnSeconds;

        internal void Validate()
        {
            CombatPresentationValueRules.RequireNonNegative(height, nameof(height));
            CombatPresentationValueRules.RequireNonNegative(seconds, nameof(seconds));
            CombatPresentationValueRules.RequireNonNegative(impactDepth, nameof(impactDepth));
            CombatPresentationValueRules.RequireNonNegative(impactDownSeconds, nameof(impactDownSeconds));
            CombatPresentationValueRules.RequireNonNegative(impactReturnSeconds, nameof(impactReturnSeconds));
            CombatPresentationValueRules.RequireNonNegative(impactDownSeconds + impactReturnSeconds, "착지 눌림 전체 시간");
        }
    }
}
