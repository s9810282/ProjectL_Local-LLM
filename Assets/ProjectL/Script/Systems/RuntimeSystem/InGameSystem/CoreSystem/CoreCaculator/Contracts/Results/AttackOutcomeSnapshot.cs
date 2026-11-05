using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public sealed class AttackOutcomeSnapshot
    {
        public IReadOnlyList<AttackHitResult> Hits { get; }
        public IReadOnlyList<TraitActivationSnapshot> Reactions { get; }
        public AttackOutcomeSnapshot(IReadOnlyList<AttackHitResult> hits, IReadOnlyList<TraitActivationSnapshot> reactions)
        {
            if (hits == null) throw new ArgumentNullException(nameof(hits));
            if (reactions == null) throw new ArgumentNullException(nameof(reactions));
            Hits = Array.AsReadOnly(hits.ToArray()); Reactions = Array.AsReadOnly(reactions.ToArray());
            if (Hits.Any(h => h.Contacts == null) || Hits.Select(h => h.TargetId).Distinct().Count() != Hits.Count)
                throw new ArgumentException("실제 피격자와 접촉 기록이 필요합니다.", nameof(hits));
            var contacts = Hits.SelectMany(h => h.Contacts).ToArray();
            if (contacts.Select(c => c.DeliveryIndex).Distinct().Count() != contacts.Length)
                throw new ArgumentException("공격 내 접촉 순번은 유일해야 합니다.", nameof(hits));
            var indices = new HashSet<int>();
            foreach (var reaction in Reactions)
            {
                if (reaction == null || reaction.Interception == null || reaction.ChildAttack != null) throw new ArgumentException(nameof(reactions));
                var r = reaction.Interception;
                var matching = Hits.Where(h => h.TargetId == r.ActualRecipientId).SelectMany(h => h.Contacts)
                    .Where(c => c.DeliveryIndex == r.DeliveryIndex && c.OriginalTargetId == r.ProtectedId && SameOrigin(c.Origin, reaction.ResultOrigin)).ToArray();
                if (!indices.Add(r.DeliveryIndex) || matching.Length != 1) throw new ArgumentException("반응과 실제 접촉이 일치하지 않습니다.", nameof(reactions));
            }
            foreach (var hit in Hits)
                foreach (var contact in hit.Contacts.Where(c => c.OriginalTargetId != hit.TargetId))
                    if (!indices.Contains(contact.DeliveryIndex)) throw new ArgumentException("수신자가 바뀐 접촉에는 대응 반응이 필요합니다.", nameof(reactions));
        }
        internal void ValidateScope(EntityId actor, long group)
        {
            if (Hits.SelectMany(h => h.Contacts).Any(c => c.Origin.CreditedCharacterId != actor || c.Origin.AttackGroup != group))
                throw new ArgumentException("공격과 접촉 출처가 다릅니다.");
        }
        internal static bool SameOrigin(CombatDamageOrigin a, CombatDamageOrigin b) => a != null && b != null &&
            a.CreditedCharacterId == b.CreditedCharacterId && a.AttackGroup == b.AttackGroup && a.PhysicalCell == b.PhysicalCell;
    }
}
