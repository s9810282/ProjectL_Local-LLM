using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>위치 교환의 모션 생략·시간 후 동시 반영을 고르는 선택 입력입니다.</summary>
    [Serializable]
    internal sealed class CombatPositionSwapVisualInput
    {
        [Tooltip("PositionSwap/MovementChanged 전용입니다. 켜면 선행 걷기 없이 이펙트를 시작하고, 대기 후 두 위치를 함께 바꿉니다. 기존 유닛 행동과 함께 쓰지 않습니다.")]
        [SerializeField]
        private bool enabled;
        [Tooltip("이펙트 시작부터 두 위치를 함께 바꾸고 이펙트를 지울 때까지의 초입니다. 유한한 0 이상이며, 0이면 즉시 교환합니다.")]
        [SerializeField, Min(0f)]
        private float waitSeconds = .5f;

        internal CombatPositionSwapVisualData ToData() => enabled ? new CombatPositionSwapVisualData(waitSeconds) : null;
    }
}
