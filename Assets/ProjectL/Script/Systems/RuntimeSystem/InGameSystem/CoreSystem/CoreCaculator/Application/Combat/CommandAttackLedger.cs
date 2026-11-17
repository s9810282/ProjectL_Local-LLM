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
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>
    /// 이 공격이 대상마다 무엇을 했는지 기록한다. 공격 결과·미리보기는 이 기록만 읽는다.
    /// 사전은 삽입 순서를 유지하며, 그 순서가 직접 피해 사건 순서와 같다(I-2). 명령 계산이 끝나면 버린다.
    /// </summary>
    internal sealed class CommandAttackLedger
    {
        private readonly Dictionary<EntityId, List<AttackHitContactSnapshot>> _contacts = new();
        private Dictionary<EntityId, int> _before;
        private HashSet<EntityId> _obstacles;
        private IReadOnlyDictionary<EntityId, int> _recoveryBefore;
        private int _healthStart;
        private IReadOnlyList<CombatHealthStep> _healthSteps;
        // 범위 시작의 캐릭터 사실(입력 목록 참조만)과, 피격 체력 고정 때 재구성이 일어난 캐릭터입니다(재구성 연출 설계 RE3, 공격 미리보기 칸).
        // 적 AI 후보 평가도 이 길을 지나므로 명령마다 모든 캐릭터를 훑거나 표를 만들지 않습니다. 재구성이 일어났을 때만 집합을 만듭니다(묶음 끝 검토 L5).
        private IReadOnlyList<CharacterCombatFacts> _scopeCharacters;
        private HashSet<EntityId> _reconstructed;
        internal List<TraitActivationSnapshot> Reactions { get; } = new();
        /// <summary>이번 직접 타격이 실제 수신자에게 만든 적 캐릭터 사망입니다. 반사·환경·이전 발생의 사망은 포함하지 않습니다.</summary>
        internal IReadOnlyCollection<EntityId> DirectKilledEnemyIds { get; private set; } = Array.Empty<EntityId>();
        internal IEnumerable<EntityId> HitIds => _contacts.Keys;
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
        internal bool IsObstacle(EntityId id) => _obstacles?.Contains(id) ?? false;
        internal void BeginScope(CommandAttackPlan plan, CommandAttackProjection world)
        {
            if (_before != null) return;
            _before = plan.Input.Characters.ToDictionary(c => c.EntityId, c => c.Health.CurrentHealth);
            foreach (var structure in plan.Input.State.Structures)
            {
                _before.Add(structure.EntityId, structure.Health.CurrentHealth);
                if (structure.ObstacleDurability != null)
                    (_obstacles ??= new HashSet<EntityId>()).Add(structure.EntityId);
            }
            _recoveryBefore = world.Damage.CaptureRecovery();
            _healthStart = world.Damage.HealthExecutions.Count;
            _scopeCharacters = plan.Input.Characters;
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
        private readonly HashSet<EntityId> _collisionVictims = new();
        internal IReadOnlyCollection<EntityId> PreHitTargets { get; private set; } = Array.Empty<EntityId>();
        /// <summary>
        /// 작렬로 날아온 유닛에 부딪혀 피해를 받은 유닛이다. 피격 대상(<see cref="HitIds"/>)이 아니라 미리보기 보조 행으로만 나간다.
        /// 넉백 후보는 넉백 시작 때 고정돼 여기 들어온 유닛은 밀리지 않는다.
        /// </summary>
        internal IReadOnlyCollection<EntityId> CollisionVictims => _collisionVictims;
        internal void AddCollisionVictim(EntityId id) => _collisionVictims.Add(id);
        /// <summary>이 공격의 넉백 요약이다. 처리 순서로 쌓인다. 추가 공격은 자기 기록을 따로 가진다(설계 2026-10-27 §5.2).</summary>
        internal IReadOnlyList<AttackKnockbackSnapshot> Knockbacks => _knockbacks;

        /// <summary>실제 명중한 대상의 피격 역할이다.</summary>
        internal IReadOnlyDictionary<EntityId, AttackTargetRole> Roles => _roles;
        /// <summary>대상별 예상 피해 합계다. 직접 피해에 장애물 폭발과 작렬 부딪힘 피해가 더해진다.</summary>
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

        internal void CaptureDirectKills(CommandAttackPlan plan, CommandAttackProjection world)
        {
            var hostility = new HostilityPolicy();
            DirectKilledEnemyIds = Array.AsReadOnly(CombatHealthRecording.StepsInScope(world.Damage.HealthExecutions,
                _healthStart, world.Damage.HealthExecutions.Count)
                .Where(step => step.ActualLoss > 0 && !step.Before.IsDead && step.After.IsDead &&
                    step.Source.Kind == CombatHealthOperationKind.Damage &&
                    world.Characters.TryGetValue(step.RecipientId, out var recipient) && recipient.Health.IsDead &&
                    hostility.AreHostile(plan.Actor.Owner, recipient.Owner) && Contacts(step.RecipientId).Any(contact =>
                        step.Source.DamageOrigins.Any(origin => ReferenceEquals(origin, contact.Origin) &&
                            origin.AttackGroup == plan.AttackGroup && origin.CreditedCharacterId == plan.Actor.EntityId)))
                .Select(step => step.RecipientId).Distinct().OrderBy(id => id).ToArray());
        }

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
            _healthSteps = Array.AsReadOnly(CombatHealthRecording.StepsInScope(world.Damage.HealthExecutions,
                _healthStart, world.Damage.HealthExecutions.Count).ToArray());
            _hitHealth = HitIds.Concat(_knockbacks.Select(k => k.TargetId)).Concat(_collisionVictims).Distinct().ToDictionary(id => id, id => world.Characters.TryGetValue(id, out var c)
                ? c.Health : world.Structures[id].Health);
            // 재구성 판정은 재구성 규칙 한 곳이 정합니다. 장부는 고정한 대상마다 범위 시작·지금 사실을 넘기기만 합니다.
            _reconstructed = null;
            foreach (EntityId id in _hitHealth.Keys)
            {
                if (!world.Characters.TryGetValue(id, out var after) || !TryGetScopeCharacter(id, out var before)) continue;
                if (ReconstructionTraitRule.Triggered(before, after))
                    (_reconstructed ??= new HashSet<EntityId>()).Add(id);
            }
        }

        /// <summary>이 공격으로 그 캐릭터의 재구성이 일어났는지입니다(피격 체력 고정 시점까지). 공격 결과·미리보기 칸이 읽습니다.</summary>
        internal bool Reconstructed(EntityId id)
        {
            if (_hitHealth == null) throw new InvalidOperationException("피격 체력을 아직 고정하지 않았습니다.");
            return _reconstructed != null && _reconstructed.Contains(id);
        }

        private bool TryGetScopeCharacter(EntityId id, out CharacterCombatFacts character)
        {
            if (_scopeCharacters != null)
                for (int i = 0; i < _scopeCharacters.Count; i++)
                    if (_scopeCharacters[i].EntityId == id)
                    {
                        character = _scopeCharacters[i];
                        return true;
                    }
            character = null;
            return false;
        }

        internal void BindHealthClaims(CombatDamageApplication damage, CombatEvent occurrence, AttackResult result)
            => BindHealthClaims(damage, occurrence, result.Hits.ToArray());

        internal void BindHealthClaims(CombatDamageApplication damage, CombatPlayerSkillAttackEvent occurrence)
            => BindHealthClaims(damage, occurrence, occurrence.Attack.Hits);

        private void BindHealthClaims(CombatDamageApplication damage, CombatEvent occurrence, IReadOnlyList<AttackHitResult> hits)
        {
            var healthScope = new CombatHealthScope(occurrence, -1,
                _healthSteps ?? throw new InvalidOperationException("공격 체력 범위가 확정되지 않았습니다."));
            var scope = healthScope.Steps.ToLookup(step => step.RecipientId);
            for (int i = 0; i < hits.Count; i++)
            {
                var recipient = hits[i].TargetId;
                var steps = scope[recipient].ToArray();
                damage.HealthClaims.Add(new CombatHealthClaim(CombatHealthClaimKind.AttackHit, occurrence, -1, i, recipient, steps, healthScope));
            }
        }

        internal int Expected(EntityId id) => _expected.GetValueOrDefault(id);
        internal int Direct(EntityId id) => (_direct ?? throw new InvalidOperationException("직접 피해 단계가 끝나지 않았습니다.")).GetValueOrDefault(id);
        internal TargetDamageBreakdown Breakdown(EntityId id) => _breakdowns.GetValueOrDefault(id, TargetDamageBreakdown.CreatePassthrough(0));
        internal int NormalizationLoss(EntityId id) => _normalizationLoss.GetValueOrDefault(id);
        internal HealthStateSnapshot HitHealth(EntityId id) =>
            (_hitHealth ?? throw new InvalidOperationException("피격 체력을 아직 고정하지 않았습니다."))[id];
    }
}
