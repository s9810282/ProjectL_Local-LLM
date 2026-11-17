using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public enum CombatHealthClaimKind
    {
        AttackHit = 1,
        ChildAttackHit,
        ArtifactDamage,
        ArtifactRecovery,
        ItemRecovery,
        TraitRecovery
    }

    /// <summary>공격의 결과 확정 시점에 포함된 단계 참조입니다. 체력 값이나 실행 상태를 복제하지 않습니다.</summary>
    public sealed class CombatHealthScope
    {
        public CombatEvent Event { get; }
        public int EntryIndex { get; }
        public IReadOnlyList<CombatHealthStep> Steps { get; }

        internal CombatHealthScope(CombatEvent occurrence, int entryIndex, IEnumerable<CombatHealthStep> steps)
        {
            Event = occurrence ?? throw new ArgumentNullException(nameof(occurrence));
            bool valid = entryIndex == -1 && (occurrence is CombatAttackEvent or CombatPlayerSkillAttackEvent || occurrence is CombatRoarEvent roar && roar.Attack.HasValue) ||
                occurrence is CombatEffectsEvent effects && entryIndex >= 0 && entryIndex < effects.Traits.Count && effects.Traits[entryIndex].ChildAttack != null;
            if (!valid) throw new ArgumentException("공격 범위의 선언 사건과 항목이 일치해야 합니다.", nameof(entryIndex));
            if (steps == null) throw new ArgumentNullException(nameof(steps));
            var copy = steps.ToArray();
            if (copy.Any(step => step == null) || copy.Distinct().Count() != copy.Length)
                throw new ArgumentException("공격 범위의 단계에는 null 또는 중복 참조를 넣을 수 없습니다.", nameof(steps));
            Steps = Array.AsReadOnly(copy);
            EntryIndex = entryIndex;
        }
    }

    /// <summary>사건 속 선언 한 건을 실제 체력 단계의 참조에 연결합니다.</summary>
    public sealed class CombatHealthClaim
    {
        public CombatHealthClaimKind Kind { get; }
        public CombatEvent Event { get; }
        public int EntryIndex { get; }
        public int HitIndex { get; }
        public EntityId RecipientId { get; }
        public IReadOnlyList<CombatHealthStep> Steps { get; }
        public CombatHealthScope Scope { get; }
        public CombatDamageOrigin DamageOrigin { get; }

        internal CombatHealthClaim(CombatHealthClaimKind kind, CombatEvent declaration, int entryIndex, int hitIndex,
            EntityId recipientId, IReadOnlyList<CombatHealthStep> steps, CombatHealthScope scope = null, CombatDamageOrigin damageOrigin = null)
        {
            Event = declaration ?? throw new ArgumentNullException(nameof(declaration));
            if (recipientId.IsEmpty) throw new ArgumentException("체력 선언의 수신자가 필요합니다.", nameof(recipientId));
            if (!MatchesOccurrence(kind, declaration, entryIndex, hitIndex, recipientId))
                throw new ArgumentException($"체력 선언 {kind}의 사건·항목·피격 순번 또는 수신자 {recipientId}가 일치하지 않습니다.");
            if (steps == null) throw new ArgumentNullException(nameof(steps));
            var copy = steps.ToArray();
            if (copy.Any(step => step == null || step.RecipientId != recipientId) || copy.Distinct().Count() != copy.Length)
                throw new ArgumentException($"체력 선언 {kind}에는 수신자 {recipientId}의 단계를 한 번씩만 연결해야 합니다.", nameof(steps));
            bool attackClaim = kind is CombatHealthClaimKind.AttackHit or CombatHealthClaimKind.ChildAttackHit;
            if (attackClaim != (scope != null) || scope != null &&
                (!ReferenceEquals(scope.Event, declaration) || scope.EntryIndex != entryIndex ||
                !scope.Steps.Where(step => step.RecipientId == recipientId).SequenceEqual(copy)))
                throw new ArgumentException("공격 선언은 확정 범위에 속한 수신자의 모든 단계를 그대로 사용해야 합니다.", nameof(scope));
            if ((kind == CombatHealthClaimKind.ArtifactDamage) != (damageOrigin != null))
                throw new ArgumentException("반사 피해 선언에는 그 실행에 전달한 실제 피해 출처가 필요합니다.", nameof(damageOrigin));

            Kind = kind;
            EntryIndex = entryIndex;
            HitIndex = hitIndex;
            RecipientId = recipientId;
            Steps = Array.AsReadOnly(copy);
            Scope = scope;
            DamageOrigin = damageOrigin;
        }

        private static bool MatchesOccurrence(CombatHealthClaimKind kind, CombatEvent declaration, int entryIndex,
            int hitIndex, EntityId recipientId)
        {
            switch (kind)
            {
                case CombatHealthClaimKind.AttackHit when entryIndex == -1:
                    if (declaration is CombatPlayerSkillAttackEvent skill)
                        return InRange(hitIndex, skill.Attack.Hits.Count) && skill.Attack.Hits[hitIndex].TargetId == recipientId;
                    AttackResult? attack = declaration switch
                    {
                        CombatAttackEvent primary => primary.Attack,
                        CombatRoarEvent roar => roar.Attack,
                        _ => null
                    };
                    return attack.HasValue && InRange(hitIndex, attack.Value.Hits.Length) &&
                        attack.Value.Hits.Span[hitIndex].TargetId == recipientId;
                case CombatHealthClaimKind.ChildAttackHit when declaration is CombatEffectsEvent childEffects &&
                    InRange(entryIndex, childEffects.Traits.Count):
                    var child = childEffects.Traits[entryIndex].ChildAttack;
                    return child != null && InRange(hitIndex, child.Hits.Count) && child.Hits[hitIndex].TargetId == recipientId;
                case CombatHealthClaimKind.ArtifactDamage:
                case CombatHealthClaimKind.ArtifactRecovery:
                    var purpose = kind == CombatHealthClaimKind.ArtifactDamage ? CombatArtifactPurpose.Reflection : CombatArtifactPurpose.Healing;
                    return declaration is CombatArtifactEvent artifact && artifact.Purpose == purpose && hitIndex == -1 &&
                        InRange(entryIndex, artifact.Activations.Count) && artifact.Activations[entryIndex]?.TargetId == recipientId;
                case CombatHealthClaimKind.ItemRecovery when declaration is CombatItemEvent item:
                    return entryIndex == -1 && hitIndex == -1 && item.Activated && item.ActivationKind == ItemActivationKind.Healing &&
                        item.Stage == ItemEventStage.AfterDamage && item.TargetId == recipientId && item.HolderId == recipientId;
                case CombatHealthClaimKind.TraitRecovery when declaration is CombatEffectsEvent effects &&
                    hitIndex == -1 && InRange(entryIndex, effects.Traits.Count):
                    var trait = effects.Traits[entryIndex];
                    return trait.TraitActivationKind == TraitActivationKind.HealthRecovered && trait.TargetIds.Contains(recipientId);
                default:
                    return false;
            }
        }

        private static bool InRange(int index, int count) => index >= 0 && index < count;
    }
}
