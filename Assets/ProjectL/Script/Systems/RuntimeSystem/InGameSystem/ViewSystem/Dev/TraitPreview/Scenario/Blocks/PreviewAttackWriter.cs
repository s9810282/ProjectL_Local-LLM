#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev.TraitPreview.Scenario.Blocks
{
    /// <summary>원래 대상과 실제 수신자를 먼저 확정하고 공격·피해를 한 번씩 작성합니다.</summary>
    internal sealed class PreviewAttackWriter
    {
        private readonly PreviewTraitWriter _traits;
        internal PreviewAttackWriter(PreviewTraitWriter traits) => _traits = traits;
        internal void Write(PreviewPacketContext packet, PreviewScenarioBlock block)
        {
            var input = block.attack ?? throw new ArgumentException("공격 입력이 없습니다.");
            var ledger = packet.Ledger;
            var attacker = ledger.Alive(input.attackerId);
            PreviewScenarioValidation.Defined(input.direction);
            var selected = PreviewScenarioLedger.Cell(input.targetCell.x, input.targetCell.y);
            if (input.hits == null || input.hits.Count == 0 || input.hits.Any(h => h == null) || input.hits.Select(h => h.targetId).Distinct().Count() != input.hits.Count)
                throw new ArgumentException("공격 대상이 비었거나 중복됩니다.");
            bool child = block.kind == PreviewBlockKind.ChildAttack;
            var origin = new CombatDamageOrigin(attacker.Anchor, attacker.EntityId, ledger.NextAttack());
            packet.Origin = origin;
            TraitActivationSnapshot childActivation = child ? _traits.Create(ledger, block.trait, origin, input) : null;
            var redirects = new Dictionary<string, string>();
            var reactions = new List<TraitActivationSnapshot>();
            foreach (var reaction in packet.Input.blocks.Where(b => b.kind == PreviewBlockKind.Interception))
            {
                var value = _traits.Create(ledger, reaction.trait, origin, input);
                if (reaction.trait.targetIds.Count != 1)
                    throw new ArgumentException($"반응 '{reaction.name}': 보호 대상은 한 명이어야 합니다.");
                string protectedId = reaction.trait.targetIds[0];
                string guardianId = reaction.trait.sourceId;
                int contact = input.hits.FindIndex(h => h.targetId == protectedId);
                if (contact < 0 || protectedId == input.attackerId || guardianId == input.attackerId || !redirects.TryAdd(protectedId, guardianId))
                    throw new ArgumentException($"반응 '{reaction.name}': 공격 접촉·세 역할·보호 대상 중복을 확인하세요.");
                ledger.Alive(protectedId);
                ledger.Alive(guardianId);
                reactions.Add(value.WithInterception(new AttackInterceptionSnapshot(value.SourceUnitId, ledger.Get(protectedId).EntityId, value.SourceUnitId, contact)));
            }

            var contacts = new Dictionary<string, List<AttackHitContactSnapshot>>();
            var health = new Dictionary<string, int>();
            for (int i = 0; i < input.hits.Count; i++)
            {
                var hit = input.hits[i];
                var original = ledger.Alive(hit.targetId);
                string recipientId = redirects.TryGetValue(hit.targetId, out var guardian) ? guardian : hit.targetId;
                var recipient = ledger.Alive(recipientId);
                if (recipient.EntityId == attacker.EntityId)
                    throw new ArgumentException("이 시연 공격의 실제 수신자와 공격자는 달라야 합니다.");
                if (hit.finalHealth < 0 || hit.finalHealth > recipient.Health.CurrentHealth)
                    throw new ArgumentException($"대상 {i}: 최종 HP는 실제 수신자의 현재 HP 이하 0 이상 값이어야 합니다.");
                if (!contacts.TryGetValue(recipientId, out var list))
                    contacts.Add(recipientId, list = new());
                if (health.TryGetValue(recipientId, out int final) && final != hit.finalHealth)
                    throw new ArgumentException("같은 실제 수신자에게 모이는 접촉의 최종 HP는 같아야 합니다.");
                health[recipientId] = hit.finalHealth;
                var role = child ? AttackTargetRole.Designated : i == 0 ? AttackTargetRole.Primary : AttackTargetRole.Additional;
                list.Add(new AttackHitContactSnapshot(i, original.EntityId, role, 100, origin));
            }

            var hits = new List<AttackHitResult>();
            var followups = new List<CombatEvent>();
            foreach (var pair in contacts)
            {
                var before = ledger.Get(pair.Key);
                int final = health[pair.Key];
                var roles = pair.Value.Select(c => c.Role).Distinct().ToArray();
                hits.Add(new AttackHitResult(before.EntityId, before.Health.CurrentHealth, final, roles.Length == 1 ? roles[0] : AttackTargetRole.None, 100, 0, 0, pair.Value));
                var after = before.WithHealth(new HealthStateSnapshot(final, before.Health.MaxHealth));
                followups.Add(new CombatCharacterEvent(final == 0 ? CombatEventKind.Died : CombatEventKind.HealthChanged, before, after, new[] { origin }));
                ledger.Set(pair.Key, after);
            }

            if (child)
            {
                var activation = childActivation.WithChildAttack(new AttackOutcomeSnapshot(hits, reactions));
                packet.Events.Add(new CombatEffectsEvent(new[] { activation }, Array.Empty<CategoryActivationSnapshot>()));
            }
            else
            {
                var primary = ledger.Get(input.hits[0].targetId);
                if (primary.Anchor.Value != selected)
                    throw new ArgumentException("지정 칸은 첫 번째 원래 대상의 칸이어야 합니다.");
                var cells = input.hits.Select(h => ledger.Get(h.targetId).Anchor.Value).Distinct().ToArray();
                var selection = new AttackCellSelection(selected, new[] { selected }, cells.Where(c => c != selected).ToArray());
                // 행동 사용·비용은 시연에서 계산하지 않습니다. 기존 반응용 계약으로 동일 전후 값을 전달합니다.
                var cost = new UnitActionUseSettlementSnapshot(attacker.EntityId, attacker.ActionUse, attacker.ActionUse);
                var attack = AttackResult.FromFacts(attacker.EntityId, primary.EntityId, input.direction, cost, cells, hits.ToArray(), selection, traitActivations: reactions.ToArray(), attackGroup: origin.AttackGroup, reaction: true);
                packet.Events.Add(new CombatAttackEvent(attack));
            }

            packet.Events.AddRange(followups);
        }
    }
}
#endif
