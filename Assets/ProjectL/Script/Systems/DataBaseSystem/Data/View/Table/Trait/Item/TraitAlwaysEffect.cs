using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.Trait.Item
{
    /// <summary>
    /// 특성 상시 이펙트 항목 하나의 입력입니다(이펙트 끝 규칙 설계 EL3, 계획 Ec.2). 조건 하나와 몸에 붙는 이펙트 하나입니다.
    /// 이펙트 값은 상태 외형의 유지 이펙트와 같은 입력(<see cref="CombatHeldEffectEntry"/>)입니다(계획 TP-E11).
    /// </summary>
    [Serializable]
    internal sealed class TraitAlwaysEffectEntry
    {
        [Tooltip("언제 보일지입니다. WhileOwned = 보유 유닛이 필드에 살아 있는 동안(보유 유닛 몸), "
            + "AttackModeTarget = 보유 유닛의 공격 모드 동안 조건을 채운 적마다(그 적 몸, 지금은 마무리 사격만 대상을 받음).")]
        [SerializeField] private CombatTraitAlwaysCondition condition = CombatTraitAlwaysCondition.WhileOwned;
        [SerializeField] private CombatHeldEffectEntry effect = new();

        // Unity 직렬화가 쓰는 기본 생성자입니다.
        public TraitAlwaysEffectEntry()
        {
        }

        /// <summary>값입니다.</summary>
        /// <exception cref="InvalidOperationException">조건이 정의되지 않았거나 이펙트가 비었을 때입니다.</exception>
        /// <exception cref="ArgumentOutOfRangeException">낼 수 없는 이펙트 종류일 때입니다.</exception>
        internal TraitAlwaysEffect ToData(string context)
        {
            if (!System.Enum.IsDefined(typeof(CombatTraitAlwaysCondition), condition))
                throw new InvalidOperationException($"{context}: 상시 이펙트 조건이 정의되지 않았습니다: {condition}");
            CombatHeldEffect held = (effect ?? throw new InvalidOperationException($"{context}: 상시 이펙트 칸이 없습니다.")).ToData()
                ?? throw new InvalidOperationException($"{context}: 상시 이펙트가 비었습니다(Empty). 항목을 지우거나 이펙트를 고르세요.");
            return new TraitAlwaysEffect(condition, held);
        }
    }

    /// <summary>특성 상시 이펙트 항목 하나의 값입니다(설계 EL3). 같은 연결 동안 같은 객체라, 유지 표시 부품이 다시 내지 않습니다.</summary>
    internal sealed class TraitAlwaysEffect
    {
        internal TraitAlwaysEffect(CombatTraitAlwaysCondition condition, CombatHeldEffect effect)
        {
            Condition = condition;
            Effect = effect ?? throw new ArgumentNullException(nameof(effect));
        }

        internal CombatTraitAlwaysCondition Condition { get; }
        internal CombatHeldEffect Effect { get; }
    }
}
