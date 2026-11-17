using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack
{
    /// <summary>
    /// 근접(몸통 박치기) 공격입니다(View 3라운드 W8). 투사체·궤적이 없고, 출처 발사를 하지 않습니다(클립 공격은 해제 조각으로 맞음).
    /// </summary>
    internal sealed class CombatMeleeStyle : CombatDirectAttackStyle
    {
        internal override CombatAttackPresentationKind Kind => CombatAttackPresentationKind.Melee;

        protected override CombatProjectilePool PoolOf(ICombatAttackStyleContext context, CombatAttackPresentationData settings) => null;

        protected override ICombatAttackAnimation CreateAnimation(ICombatAttackStyleContext context, CombatAttackPresentationData settings,
            FieldGridMapper mapper, CombatProjectilePool pool, ICombatAttackUnitPose pose, Vector3 target, Quaternion direction,
            CombatAttackShape shape, CombatContactRelease hit, Action cue) =>
            // 근접은 타격 순간 한 번에 모두 해제합니다(관통·죽음의 손 View 설계 D6의 접촉 하나 = 끝).
            new CombatMeleeAttackAnimation(settings, mapper, pose, target, direction, cue, context.CancelUnavailable, hit.Finish);
    }
}
