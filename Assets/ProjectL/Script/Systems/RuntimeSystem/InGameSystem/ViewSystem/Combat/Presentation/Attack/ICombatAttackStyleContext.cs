using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack
{
    /// <summary>
    /// 공격 재생(<see cref="CombatAttackPlayback"/>)이 공격 종류 전략(<see cref="CombatAttackStyle"/>)에 내주는 맥락입니다(View 3라운드 W8).
    /// 전략은 이것으로만 수명(Gate·투사체 진행·자세 대여)과 접촉 계산을 씁니다. 공격 재생이 명시적으로 구현합니다.
    /// </summary>
    /// <remarks>Gate·투사체·Tick·취소의 수명과 접촉 진행률 계산은 공격 재생에 남습니다. 전략은 "그 종류가 무엇을 어디로 쏘고 언제 맞나"만 정합니다.</remarks>
    internal interface ICombatAttackStyleContext
    {
        /// <summary>공격자의 공격 자세를 빌립니다. <paramref name="cancelled"/>는 표시가 꺼져 대여가 끊길 때 부르며 없으면 null입니다.</summary>
        ICombatAttackUnitPose BeginAttackPose(EntityId attackerId, Action cancelled);

        /// <summary>지금 표시의 곡사 무기입니다. 표시가 없으면 false, 무기가 없으면 예외입니다.</summary>
        bool TryGetMortarWeapon(EntityId attackerId, out CatusCannonWeaponView weapon);

        /// <summary>그 프리팹의 투사체 풀입니다(코드 모션).</summary>
        CombatProjectilePool PoolOf(GameObject prefab);

        /// <summary>출처 발사 곡사 한 발입니다(<see cref="CombatProjectileFlights.FireArc"/>). 완급 곡선은 공격 표시 값이 줍니다.</summary>
        ICombatShot FireArc(GameObject prefab, Vector3 from, Vector3 to, float duration, float height, Vector3 up, Vector3 spin,
            CombatProjectileEasing easing, Action hit, Action arrived, int version, CombatArcRotationMode rotationMode);

        /// <summary>출처 발사 직선 한 발입니다(<see cref="CombatProjectileFlights.FireLinear"/>). 완급 곡선은 공격 표시 값이 줍니다.</summary>
        ICombatShot FireLinear(GameObject prefab, Vector3 from, Vector3 to, float speed, Vector3 up, float hitAt,
            CombatProjectileEasing easing, Action hit, Action arrived, int version);

        /// <summary>
        /// 코드 모션 애니메이션 수명을 시작합니다. 정상으로 끝나면 세대가 같을 때 <paramref name="ended"/>를 부릅니다.
        /// Gate는 열지 않습니다. 출처 수명은 이 모션을 담은 조립 실행이 맡습니다(범용 연출 구조 계획 P8).
        /// </summary>
        void BeginAnimation(ICombatAttackAnimation animation, Action ended, int version);

        /// <summary>코드 모션 투사체의 맞음입니다: 접촉 자리에 접촉 이펙트를 낸 뒤 <paramref name="hit"/>을 부릅니다.</summary>
        Action<CombatProjectileContact> ImpactThen(CombatAttackPresentationData settings, Action hit);

        /// <summary>발사 때 접촉 자리가 정해지는 출처 발사의 맞음입니다.</summary>
        Action ImpactThen(CombatAttackPresentationData settings, Action hit, CombatProjectileContact contact);

        /// <summary>직선 투사체가 주 대상 몸에 처음 닿는 진행률입니다(설계 H6).</summary>
        float LinearContactProgress(CombatAttackShape shape, Vector3 from, Vector3 to, FieldGridMapper mapper);

        /// <summary>공격 중 유닛·무기 표시가 사라졌습니다. 자원을 먼저 정리하고 다음 Tick에서 오류로 알립니다.</summary>
        void CancelUnavailable();
    }
}
