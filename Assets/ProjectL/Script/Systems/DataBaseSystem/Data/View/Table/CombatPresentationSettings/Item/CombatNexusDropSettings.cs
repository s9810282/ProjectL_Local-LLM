using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item
{
    /// <summary>첫 적 Nexus의 낙하 시간과 높이입니다. 진행 상태는 보관하지 않습니다.</summary>
    [Serializable]
    internal sealed class CombatNexusDropSettings
    {
        [SerializeField] private bool enabled = true;
        [Tooltip("완성 위치보다 위에서 시작하는 높이입니다(월드 단위). 필드 위쪽 축으로 떨어집니다.")]
        [SerializeField, Min(0f)] private float height = 10f;
        [Tooltip("Nexus가 가속하며 떨어지는 시간입니다. 0이면 즉시 착지하고 이펙트를 냅니다.")]
        [SerializeField, Min(0f)] private float seconds = .6f;

        internal bool Enabled => enabled;
        internal float Height => height;
        internal float Seconds => seconds;

        internal void Validate()
        {
            CombatPresentationValueRules.RequireNonNegative(height, nameof(height));
            CombatPresentationValueRules.RequireNonNegative(seconds, nameof(seconds));
        }
    }
}
