using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts
{
    /// <summary>
    /// 공격 조준이 공격 모드 동안 "즉시 처치 대상" 표시 의도만 전하는 좁은 계약입니다(이펙트 끝 규칙 설계 EL3 공격 모드 대상, 마무리 사격 FS-b, 계획 TP-E16).
    /// 공격 조준만 받습니다. 구현: 씬 표시(<see cref="CombatSceneDisplay"/>)가 유지 표시 부품의 공격 모드 대상 출처에 넘깁니다.
    /// </summary>
    /// <remarks>
    /// 부르는 쪽은 Core 공격 범위 응답의 대상 목록(<c>AttackRangeResult.InstantDefeatTargetIds</c>)을 그대로 넘기고 판정하지 않습니다(판정 원본은 Core 하나).
    /// 무엇을 어느 몸에 낼지는 특성 외형 상시 목록(조건 <c>AttackModeTarget</c>)이 정합니다. 구현은 멱등이고, 해제 뒤 늦게 온 호출은 무시합니다.
    /// </remarks>
    internal interface ICombatAttackModeTargets
    {
        /// <summary>공격 모드 행위자와 지금 범위 응답의 즉시 처치 대상을 보입니다. 앞의 것을 대신합니다. 대상이 비면 지운 것과 같습니다.</summary>
        void ShowAttackModeTargets(EntityId actor, ReadOnlyMemory<EntityId> targets);

        /// <summary>공격 모드 대상 표시를 지웁니다. 여러 번 불려도 안전합니다.</summary>
        void ClearAttackModeTargets();
    }
}
