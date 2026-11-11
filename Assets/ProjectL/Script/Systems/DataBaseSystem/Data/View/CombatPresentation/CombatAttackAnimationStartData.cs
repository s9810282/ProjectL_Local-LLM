using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>선행 특성 연출 기준 공격 클립 시작 설정입니다. 기본값은 기존 순차 재생입니다.</summary>
    internal readonly struct CombatAttackAnimationStartData
    {
        internal CombatAttackAnimationStartData(CombatAttackAnimationStartMode mode, TraitActivationKind kind, float delaySeconds)
        {
            string error = FindError(mode, kind, delaySeconds);
            if (error != null)
                throw new ArgumentException(error);
            Mode = mode;
            bool selected = mode == CombatAttackAnimationStartMode.FromSelectedTraitPresentationStart;
            Kind = selected ? kind : TraitActivationKind.None;
            DelaySeconds = selected ? delaySeconds : 0f;
        }

        internal CombatAttackAnimationStartMode Mode { get; }
        internal TraitActivationKind Kind { get; }
        internal float DelaySeconds { get; }
        internal bool UsesSelectedTrait => Mode == CombatAttackAnimationStartMode.FromSelectedTraitPresentationStart;

        /// <summary>SO 입력과 불변 값이 공유하는 검사입니다. 기본 모드의 미사용 Kind·지연은 검사하지 않습니다.</summary>
        internal static string FindError(CombatAttackAnimationStartMode mode, TraitActivationKind kind, float delaySeconds)
        {
            if (mode == CombatAttackAnimationStartMode.AfterPreAttackPresentation)
                return null;
            if (mode != CombatAttackAnimationStartMode.FromSelectedTraitPresentationStart)
                return "attackAnimationStartMode — 정의되지 않은 시작 모드입니다.";
            if (!CombatPreAttackTraitKinds.SupportsKind(kind))
                return "attackAnimationStartKind — 지원하는 선행 발동 Kind를 선택해야 합니다.";
            if (!CombatPresentationValueRules.IsNonNegative(delaySeconds))
                return "attackAnimationStartDelaySeconds — 지연은 유한한 0 이상의 초여야 합니다.";
            return null;
        }
    }
}
