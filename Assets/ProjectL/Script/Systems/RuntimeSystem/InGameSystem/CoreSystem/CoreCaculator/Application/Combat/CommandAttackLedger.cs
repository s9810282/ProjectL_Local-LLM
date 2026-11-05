using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>
    /// 이 공격이 대상마다 무엇을 했는지 기록한다. 공격 결과·미리보기는 이 기록만 읽는다.
    /// 사전은 삽입 순서를 유지하며, 그 순서가 직접 피해 사건 순서와 같다(I-2). 명령 계산이 끝나면 버린다.
    /// </summary>
    internal sealed class CommandAttackLedger
    {
        private readonly Dictionary<EntityId, List<AttackHitContactSnapshot>> _contacts = new();
        private readonly HashSet<EntityId> _auxiliary = new();
        private Dictionary<EntityId, int> _before;
        private IReadOnlyDictionary<EntityId, int> _recoveryBefore;
        internal List<TraitActivationSnapshot> Reactions { get; } = new();
        internal IEnumerable<EntityId> HitIds => _contacts.Keys.Concat(_auxiliary).Distinct();
        internal IReadOnlyList<AttackHitContactSnapshot> Contacts(EntityId id) => _contacts.TryGetValue(id, out var values) ? values : Array.Empty<AttackHitContactSnapshot>();

        /// <summary>
        /// 원래 대상을 실제로 맞은 유닛으로 바꿉니다. 희생이 대신 맞은 타격은 수호자가 되고,
        /// 접촉 기록이 없는 대상은 그대로 둡니다(2026-11-02 결정: 피해와 함께 붙는 효과는 실제 수신자가 받음).
        /// </summary>
        internal IEnumerable<EntityId> ActualRecipients(IEnumerable<EntityId> originals) => originals.SelectMany(original =>
        {
            var recipients = _contacts.Where(pair => pair.Value.Any(contact => contact.OriginalTargetId == original))
                .Select(pair => pair.Key).ToArray();
            return recipients.Length == 0 ? new[] { original } : recipients;
        });
        internal int Before(EntityId id) => _before[id];
        internal void BeginScope(CommandAttackPlan plan, CommandAttackProjection world)
        {
            if (_before != null) return;
            _before = plan.Input.Characters.ToDictionary(c => c.EntityId, c => c.Health.CurrentHealth);
            foreach (var structure in plan.Input.State.Structures) _before.Add(structure.EntityId, structure.Health.CurrentHealth);
            _recoveryBefore = world.Damage.CaptureRecovery();
        }
        internal void RecordContact(EntityId recipient, EntityId original, int index, AttackTargetRole role, int percent, CombatDamageOrigin origin)
        {
            _roles[original] = role;
            if (!_contacts.TryGetValue(recipient, out var contacts)) _contacts.Add(recipient, contacts = new List<AttackHitContactSnapshot>());
            contacts.Add(new AttackHitContactSnapshot(index, original, role, percent, origin));
        }

        private readonly Dictionary<EntityId, AttackTargetRole> _roles = new();
        private readonly Dictionary<EntityId, int> _expected = new();
        private readonly Dictionary<EntityId, TargetDamageBreakdown> _breakdowns = new();
        private readonly Dictionary<EntityId, int> _normalizationLoss = new();
        private readonly Dictionary<EntityId, int> _pendingDirect = new();
        private Dictionary<EntityId, int> _direct;
        private Dictionary<EntityId, HealthStateSnapshot> _hitHealth;
        private IReadOnlyDictionary<EntityId, int> _recovery;
        private readonly List<AttackKnockbackSnapshot> _knockbacks = new();
        internal IReadOnlyCollection<EntityId> PreHitTargets { get; private set; } = Array.Empty<EntityId>();
        /// <summary>이 공격의 넉백 요약이다. 처리 순서로 쌓인다. 추가 공격은 자기 기록을 따로 가진다(설계 2026-10-27 §5.2).</summary>
        internal IReadOnlyList<AttackKnockbackSnapshot> Knockbacks => _knockbacks;

        /// <summary>피격 역할이다. 직접 피격이 먼저 들어오고 가시밭 피해자가 뒤에 붙는다.</summary>
        internal IReadOnlyDictionary<EntityId, AttackTargetRole> Roles => _roles;
        /// <summary>대상별 예상 피해 합계다. 직접 피해에 폭발·밀치기 가시·가시밭 피해가 더해진다.</summary>
        internal IReadOnlyDictionary<EntityId, int> ExpectedDamage => _expected;

        internal void RecordDirectHit(EntityId id, AttackTargetRole role, TargetDamageBreakdown breakdown, int finalDamage)
        {
            if (_direct != null) throw new InvalidOperationException("직접 피해 단계가 이미 끝났습니다.");
            _expected[id] = checked(_expected.GetValueOrDefault(id) + finalDamage);
            _pendingDirect[id] = checked(_pendingDirect.GetValueOrDefault(id) + finalDamage);
            _breakdowns[id] = breakdown;

        }

        /// <summary>직접 피해만의 합계를 고정한다. 이후 더해지는 피해는 미리보기의 후속 피해로 보인다.</summary>
        internal void CompleteDirectStage() => _direct = _pendingDirect.ToDictionary(p => p.Key, p => p.Value);

        /// <summary>폭발 피해는 이미 직접 맞은 대상의 기대값에만 더한다.</summary>
        internal void AddBlastIfHit(EntityId id, int amount)
        {
            if (_expected.ContainsKey(id)) _expected[id] = checked(_expected[id] + amount);
        }

        internal void AdjustDirectDamage(EntityId id, int delta, int damageBeforeItems)
        {
            _expected[id] = checked(_expected[id] + delta);
            _pendingDirect[id] = checked(_pendingDirect[id] + delta);
            var before = _breakdowns[id];
            _breakdowns[id] = new TargetDamageBreakdown(before.DamageBeforeMatchup, before.MatchupNumerator, before.MatchupDenominator,
                before.DamageAfterMatchup, before.DamageAfterPhysicalDefense, before.IncomingReduction, damageBeforeItems,
                before.DamageAfterReceivedPercentages, before.TraitContributions);
        }

        internal int Recovery(EntityId id) => _recovery.GetValueOrDefault(id) - (_recoveryBefore?.GetValueOrDefault(id) ?? 0);

        internal void AddDamage(EntityId id, int amount) => _expected[id] = checked(_expected.GetValueOrDefault(id) + amount);

        /// <summary>직접 타격 전 환경 피해를 기존 피해 적용 경계의 집계로 고정합니다. 피격 역할은 만들지 않습니다.</summary>
        internal void CapturePreHitDamage(IReadOnlyDictionary<EntityId, long> damage)
        {
            if (_direct != null || _roles.Count != 0) throw new InvalidOperationException("직접 피해 전에만 사전 피해를 고정할 수 있습니다.");
            _expected.Clear();
            foreach (var pair in damage) _expected.Add(pair.Key, checked((int)pair.Value));
            PreHitTargets = damage.Keys.ToArray();
        }



        /// <summary>가시밭에만 맞은 대상은 Additional 역할로 결과에 남는다.</summary>
        internal void AddThornVictim(EntityId id)
        {
            if (!_roles.ContainsKey(id)) _roles.Add(id, AttackTargetRole.Additional);
            _auxiliary.Add(id);
        }

        internal void RecordKnockback(AttackKnockbackSnapshot record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            if (_knockbacks.Any(k => k.TargetId == record.TargetId))
                throw new InvalidOperationException("한 공격에서 같은 대상의 넉백은 한 번만 기록합니다.");
            _knockbacks.Add(record);
        }

        internal void AddNormalizationLoss(EntityId id, int loss)
        {
            if (loss < 0) throw new ArgumentOutOfRangeException(nameof(loss));
            _normalizationLoss[id] = checked(_normalizationLoss.GetValueOrDefault(id) + loss);
        }

        /// <summary>피격 체력은 비용 정산 뒤, 강제 도착과 연쇄 공격 전에 고정한다(I-4).</summary>
        internal void CaptureHitHealth(CommandAttackProjection world)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            // 회복량은 사건 목록에서 다시 계산하지 않고, 피해 적용 경계가 실제 적용량을 기록한 값을 이 시점에 고정한다.
            _recovery = world.Damage.CaptureRecovery();
            _hitHealth = HitIds.Concat(_knockbacks.Select(k => k.TargetId)).Distinct().ToDictionary(id => id, id => world.Characters.TryGetValue(id, out var c)
                ? c.Health : world.Structures[id].Health);
        }

        internal int Expected(EntityId id) => _expected.GetValueOrDefault(id);
        internal int Direct(EntityId id) => (_direct ?? throw new InvalidOperationException("직접 피해 단계가 끝나지 않았습니다.")).GetValueOrDefault(id);
        internal TargetDamageBreakdown Breakdown(EntityId id) => _breakdowns.GetValueOrDefault(id, TargetDamageBreakdown.CreatePassthrough(0));
        internal int NormalizationLoss(EntityId id) => _normalizationLoss.GetValueOrDefault(id);
        internal HealthStateSnapshot HitHealth(EntityId id) =>
            (_hitHealth ?? throw new InvalidOperationException("피격 체력을 아직 고정하지 않았습니다."))[id];
    }
}
