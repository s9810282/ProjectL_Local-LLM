using System;
using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Effect.Enum;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 조건이 맞는 동안 몸에 붙어 보이는 이펙트 하나의 입력입니다(이펙트 끝 규칙 설계 EL2·EL3, 계획 TP-E11). 상태 외형의 상태 유지 이펙트와 특성 외형의 상시 목록이 함께 씁니다.
    /// 시작 자리는 유닛 아래 필드 표면이고, 이후 표면 받침을 따라갑니다. 끝은 조건이 정하므로 끝 칸이 없습니다.
    /// </summary>
    [Serializable]
    internal sealed class CombatHeldEffectEntry
    {
        [Tooltip("낼 이펙트입니다. Render DB 이펙트 표에 등록돼 있어야 합니다. Empty면 이펙트가 없습니다. 조건이 꺼질 때까지 남아야 하므로 반복(Loop)·링 버퍼 파티클이 맞습니다.")]
        [SerializeField] private EffectType effect = EffectType.Empty;
        [Tooltip("유닛 아래 필드 표면 기준 위치 보정입니다. x = 오른쪽, y = 위, z = 그 유닛의 앞이고 월드 단위입니다.")]
        [SerializeField] private Vector3 positionOffset;
        [Tooltip("이펙트가 붙는 대상이 Rider일 때 Position Offset에 더할 월드 Y 보정입니다. 양수면 위로 올립니다. 발동 유닛의 병과가 아니라 붙는 대상의 병과를 따릅니다. 칸에 놓는 이펙트에는 적용하지 않습니다.")]
        [SerializeField] private float riderYOffset;
        [Tooltip("몸 회전 뒤에 더하는 회전입니다(오일러 각, 도). 0이면 몸 회전 그대로입니다.")]
        [SerializeField] private Vector3 rotationOffset;

        // Unity 직렬화가 쓰는 기본 생성자입니다.
        public CombatHeldEffectEntry()
        {
        }

        /// <summary>값입니다. 이펙트가 비어 있으면 null입니다(그 유지 이펙트 없음).</summary>
        /// <exception cref="ArgumentOutOfRangeException">정의되지 않은 이펙트 종류이거나 Rider 보정이 유한하지 않을 때입니다.</exception>
        internal CombatHeldEffect ToData() => effect == EffectType.Empty ? null : new CombatHeldEffect(effect, positionOffset, rotationOffset, riderYOffset);
    }

    /// <summary>
    /// 조건이 맞는 동안 유닛에 붙어 보이는 이펙트 하나의 값입니다(계획 TP-E11). 보정을 표면 자리에 적용하는 규칙은 큐와 같은 한 곳(View <c>CombatCuePoints.Apply</c>)입니다.
    /// 하붑 구름은 몸 대신 칸 자리에 같은 규칙으로 씁니다(하붑 View 계획 HP-18).
    /// </summary>
    internal sealed class CombatHeldEffect
    {
        /// <exception cref="ArgumentOutOfRangeException">낼 수 없는 이펙트 종류(Empty 포함)이거나 Rider 보정이 유한하지 않을 때입니다.</exception>
        internal CombatHeldEffect(EffectType effect, Vector3 positionOffset, Vector3 rotationOffset, float riderYOffset = 0f)
        {
            if (!CombatPresentationEffectRules.IsPlayable(effect))
                throw new ArgumentOutOfRangeException(nameof(effect), effect, CombatPresentationEffectRules.PlayableRule);
            CombatPresentationValueRules.RequireFinite(riderYOffset, nameof(riderYOffset));
            Effect = effect;
            PositionOffset = positionOffset;
            RiderYOffset = riderYOffset;
            RotationOffset = Quaternion.Euler(rotationOffset);
        }

        internal EffectType Effect { get; }

        /// <summary>유닛 아래 필드 표면 기준 위치 보정입니다(x 오른쪽·y 위·z 앞, 월드 단위).</summary>
        internal Vector3 PositionOffset { get; }

        /// <summary>이펙트가 붙는 대상이 Rider일 때 더할 월드 Y 보정입니다.</summary>
        internal float RiderYOffset { get; }

        /// <summary>몸 회전 뒤에 더하는 회전입니다.</summary>
        internal Quaternion RotationOffset { get; }

        /// <summary>같은 이펙트를 같은 보정으로 내는지입니다. 유지 이펙트 맞추기가 "이미 켜진 것"을 판단할 때 씁니다(값 객체가 바뀌어도 다시 내지 않게).</summary>
        internal bool SameAs(CombatHeldEffect other)
            => other != null && Effect == other.Effect && PositionOffset == other.PositionOffset && RiderYOffset == other.RiderYOffset &&
                RotationOffset == other.RotationOffset;
    }
}
