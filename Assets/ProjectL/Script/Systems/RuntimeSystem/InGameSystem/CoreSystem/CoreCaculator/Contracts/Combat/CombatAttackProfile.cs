using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
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
        public CharacterInstanceId? OwnedId { get; }
        public UnitSpec Definition { get; }
        public AttackOcclusionSpec Occlusion => Definition.Occlusion;
        public TargetingMethodSpec Method { get; }

        /// <summary>이 공격이 관통 전달을 쓰는지 나타낸다. 관통 적용 여부는 여기서 한 번만 판단한다.</summary>
        public bool UsesPiercingShot { get; }

        public CombatAttackProfile(UnitSpec definition, TargetingMethodSpec method, CharacterInstanceId? ownedId = null)
        {
            if (ownedId.HasValue && ownedId.Value.IsEmpty) throw new ArgumentException(nameof(ownedId));
            OwnedId = ownedId;
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Method = method ?? throw new ArgumentNullException(nameof(method));
            if (definition.FootprintLocalCells.Length != 1)
                throw new ArgumentException("다칸 유닛의 새 공격 경로는 기획 검토 전까지 지원하지 않습니다.");
            UsesPiercingShot = HasTrait(definition, TraitKey.PiercingFalloff);
            // 충격 볼트는 하나의 중심을 요구하며, 공격자 기준 밀치기(넉백·작렬)·관통 전달과 결합하지 않습니다.
            // 넉백 평가는 방향 모드가 다른 기여를 함께 받지 않으므로, 여기서 전투 준비 때 거절합니다.
            if (HasTrait(definition, TraitKey.ImpactBolt) &&
                (method.ChoiceMode != TargetingChoiceMode.Single || UsesPiercingShot || HasTrait(definition, TraitKey.Knockback) ||
                 HasTrait(definition, TraitKey.CrashImpact)))
                throw new ArgumentException("충격 볼트는 단일 칸 지정에서만 쓰며 넉백·작렬·관통 특성과 함께 쓸 수 없습니다.");
            // 끌어오기의 아군 선택 권한을 관통 피격 목록으로 확장하지 않습니다. 두 전달 방식의 조합은 지원하지 않습니다.
            if (UsesPiercingShot && HasTrait(definition, TraitKey.Pull))
                throw new ArgumentException("끌어오기와 관통 특성은 함께 쓸 수 없습니다.");
            // 끌어오기 지정 조건을 여기서 거부해, 공유 지정 방식이 바뀌어도 전투 중 첫 조회가 아니라 전투 준비에서 실패한다.
            if (HasTrait(definition, TraitKey.Pull))
            {
                if (!IsSupportedPullMethod(method))
                    throw new ArgumentException("끌어오기는 직선·단일·방향 제한 없음·추가 범위 없음 지정만 지원합니다.");
                if (definition.AttackRange < 1)
                    throw new ArgumentException("끌어오기 유닛의 공격 사거리는 1 이상이어야 합니다.");
            }
            if (UsesPiercingShot &&
                (method.ChoiceMode != TargetingChoiceMode.Single || method.Splash != TargetingSplash.None))
                throw new ArgumentException("관통 특성은 단일 지정·스플래시 없음 방식에서만 쓸 수 있습니다.");
            // 관통 기록에는 화살이 만난 대상만 들어간다. 가시밭이 경로 밖 대상을 때리면 결과와 기록이 어긋나므로 함께 쓰지 않는다.
            if (UsesPiercingShot && HasTrait(definition, TraitKey.ThornField))
                throw new ArgumentException("관통 특성과 가시밭 특성은 함께 쓸 수 없습니다.");
            // 가시밭은 선택한 칸(PrimaryCell)을 중심으로 생긴다. 방향·전체 선택에는 그 칸이 없어 효과가 조용히 사라지므로 미리 거부한다.
            if (HasTrait(definition, TraitKey.ThornField) && method.ChoiceMode != TargetingChoiceMode.Single)
                throw new ArgumentException("가시밭 특성은 칸 선택(Single) 지정 방식에서만 쓸 수 있습니다.");
        }

        /// <summary>끌어오기가 지원하는 지정 방식(직선·단일·스플래시 없음·방향 제한 없음)인지 판단한다. 이 조건의 유일한 정의다.</summary>
        public static bool IsSupportedPullMethod(TargetingMethodSpec method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            return method.Shape == TargetingShape.CardinalLine && method.ChoiceMode == TargetingChoiceMode.Single &&
                method.Splash == TargetingSplash.None && method.ChoiceAngle == TargetingChoiceAngle.Unrestricted;
        }

        private static bool HasTrait(UnitSpec definition, TraitKey key)
        {
            foreach (var trait in definition.Traits.Span)
                if (trait.TraitKey == key) return true;
            return false;
        }
    }
}
