using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>
    /// 유닛 표시 집합이 창구(<see cref="CombatUnitPresentationWindow"/>)와 HUD 일시 피드백(<see cref="CombatUnitHudFeedback"/>)에 주는 것입니다
    /// (연출 조립 설계 B13 보완 4, §9 I-2). <see cref="FieldUnitPresenter"/>가 구현합니다.
    /// 둘이 표시 집합의 구현을 모르게 해, 배타 규칙·소유 사실·미리보기 대상을 가짜 표시로 시험할 수 있게 합니다.
    /// </summary>
    internal interface ICombatUnitTargets
    {
        /// <summary>그 ID가 지금 대여 중인 표시입니다. 켜짐 여부와 관계없습니다. 없으면 false입니다.</summary>
        bool TryGet(EntityId id, out ICombatUnitPresentationTarget target);

        /// <summary>그 ID가 지금 대여 중인 표시의 HUD입니다(<see cref="CombatUnitHudFeedback"/>가 씀). 켜짐 여부와 관계없습니다. 없으면 false입니다.</summary>
        bool TryGetHud(EntityId id, out ICombatUnitHudTarget target);

        /// <summary>그 표시가 아직 그 ID의 대여인지입니다.</summary>
        bool IsRented(EntityId id, ICombatUnitPresentationTarget target);

        /// <summary>그 ID·표시의 대여를 닫고 풀로 돌려줍니다(사망 모델 유지 해제).</summary>
        void ReleaseRental(EntityId id, ICombatUnitPresentationTarget target);

        /// <summary>그 외형의 공격 연출 값입니다. 없으면 null입니다. 구현은 외형마다 한 번 읽어 보관합니다(범용 연출 구조 계획 P2).</summary>
        CombatAttackPresentationData AttackSettingsOf(EntityKey key);

        /// <summary>그 외형의 피격·사망 연출 값입니다. 없으면 null입니다. 구현은 외형마다 한 번 읽어 보관합니다(범용 연출 구조 계획 P2).</summary>
        CombatUnitReactionPresentationData ReactionSettingsOf(EntityKey key);

        /// <summary>그 외형의 공격·피격 클립 묶음입니다(클립 설계 AC1). 없으면 null입니다. 구현은 외형마다 한 번 읽어 보관합니다(클립 계획 T12).</summary>
        CombatUnitAnimationData AnimationSettingsOf(EntityKey key);

        /// <summary>그 사실을 그릴 때의 월드 위치입니다.</summary>
        Vector3 FactPosition(CharacterCombatFacts facts, FieldGridMapper mapper);

        /// <summary>그 사실을 그릴 때의 월드 회전입니다.</summary>
        Quaternion FactRotation(CharacterCombatFacts facts, FieldGridMapper mapper);
    }
}
