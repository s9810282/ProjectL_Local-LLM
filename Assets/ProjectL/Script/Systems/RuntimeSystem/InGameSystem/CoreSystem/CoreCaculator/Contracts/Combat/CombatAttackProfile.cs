using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>
    /// DB 경계에서 명시적으로 연결한 기존 유닛 정의와 새 지정 방식이다.
    /// 유닛 정의와 지정 방식의 결합 조건은 여기서 한 번 검사한다. 그래서 전투 준비가 성공한 프로필은 공격 계산 중에 결합 오류를 내지 않는다.
    /// </summary>
    public sealed class CombatAttackProfile
    {
        public UnitSpec Definition { get; }
        public AttackOcclusionSpec Occlusion => Definition.Occlusion;
        public TargetingMethodSpec Method { get; }

        public CombatAttackProfile(UnitSpec definition, TargetingMethodSpec method)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Method = method ?? throw new ArgumentNullException(nameof(method));
            if (definition.FootprintLocalCells.Length != 1)
                throw new ArgumentException("다칸 유닛의 새 공격 경로는 기획 검토 전까지 지원하지 않습니다.");
            if (HasTrait(definition, TraitKey.PiercingFalloff) && !SupportsPierce(method))
                throw new ArgumentException("관통 특성은 8방향 직선·방향 지정·스플래시 없음 지정 방식에서만 쓸 수 있습니다.");
            // 가시밭은 선택한 칸(PrimaryCell)을 중심으로 생긴다. 방향·전체 선택에는 그 칸이 없어 효과가 조용히 사라지므로 미리 거부한다.
            if (HasTrait(definition, TraitKey.ThornField) && method.ChoiceMode != TargetingChoiceMode.Single)
                throw new ArgumentException("가시밭 특성은 칸 선택(Single) 지정 방식에서만 쓸 수 있습니다.");
        }

        /// <summary>관통(PiercingFalloff)이 쓰는 지정 방식이다. 방향으로 직선을 따라 맞은 대상을 모두 모은다.</summary>
        public static bool SupportsPierce(TargetingMethodSpec method)
            => method.Shape == TargetingShape.EightDirectionLine && method.ChoiceMode == TargetingChoiceMode.Direction &&
               method.Splash == TargetingSplash.None;

        private static bool HasTrait(UnitSpec definition, TraitKey key)
        {
            foreach (var trait in definition.Traits.Span)
                if (trait.TraitKey == key) return true;
            return false;
        }
    }
}
