using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills
{
    /// <summary>공통 피해 계산에서 가져온 상성·방어·피격 경감 내역이다.</summary>
    public readonly struct PlayerSkillDamageBreakdown
    {
        public int DamageBeforeMatchup { get; }
        public int MatchupNumerator { get; }
        public int MatchupDenominator { get; }
        public int DamageAfterMatchup { get; }
        public int DamageAfterPhysicalDefense { get; }
        public int IncomingReduction { get; }
        public int FinalDamage { get; }

        public PlayerSkillDamageBreakdown(int damageBeforeMatchup, int matchupNumerator, int matchupDenominator,
            int damageAfterMatchup, int damageAfterPhysicalDefense, int incomingReduction, int finalDamage)
        {
            if (damageBeforeMatchup < 0 || matchupNumerator < 0 || matchupDenominator <= 0 ||
                damageAfterMatchup < 0 || damageAfterPhysicalDefense < 0 || incomingReduction < 0 || finalDamage < 0)
                throw new ArgumentException("피해 단계와 상성 비율이 유효해야 합니다.");
            DamageBeforeMatchup = damageBeforeMatchup;
            MatchupNumerator = matchupNumerator;
            MatchupDenominator = matchupDenominator;
            DamageAfterMatchup = damageAfterMatchup;
            DamageAfterPhysicalDefense = damageAfterPhysicalDefense;
            IncomingReduction = incomingReduction;
            FinalDamage = finalDamage;
        }
    }

    /// <summary>스킬의 일차 피해 발생 한 건이다. 기존 타격·넉백 근거를 담고 일반 행동 사용은 정산하지 않는다.</summary>
    public sealed class PlayerSkillAttackResult
    {
        public EntityId ActorId { get; }
        public int AttackOrdinal { get; }
        public long AttackGroup { get; }
        public IReadOnlyList<CellCoord> AffectedCells { get; }
        public IReadOnlyList<CellCoord> ProjectilePath { get; }
        /// <summary>투사체 접촉 순번마다 그 접촉이 들어온 구간 방향이다.</summary>
        public IReadOnlyDictionary<int, CellCoord> IncomingDirections { get; }
        /// <summary>공통 피해의 발원 칸과 별개인 투사체 실제 접촉 칸이다.</summary>
        public IReadOnlyDictionary<int, CellCoord> ImpactCells { get; }
        public IReadOnlyList<AttackHitResult> Hits { get; }
        public IReadOnlyDictionary<EntityId, PlayerSkillDamageBreakdown> DamageByRecipient { get; }
        public IReadOnlyList<AttackKnockbackSnapshot> Knockbacks { get; }
        public IReadOnlyList<TraitActivationSnapshot> TraitActivations { get; }
        public IReadOnlyList<CategoryActivationSnapshot> CategoryActivations { get; }

        public PlayerSkillAttackResult(EntityId actorId, int attackOrdinal, long attackGroup,
            IReadOnlyList<CellCoord> affectedCells, IReadOnlyList<AttackHitResult> hits,
            IReadOnlyList<AttackKnockbackSnapshot> knockbacks = null, IReadOnlyList<TraitActivationSnapshot> traitActivations = null,
            IReadOnlyList<CategoryActivationSnapshot> categoryActivations = null, IReadOnlyList<CellCoord> projectilePath = null,
            IReadOnlyDictionary<int, CellCoord> incomingDirections = null,
            IReadOnlyDictionary<EntityId, PlayerSkillDamageBreakdown> damageByRecipient = null,
            IReadOnlyDictionary<int, CellCoord> impactCells = null)
        {
            if (actorId.IsEmpty) throw new ArgumentException("행위자 ID가 필요합니다.", nameof(actorId));
            if (attackOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(attackOrdinal));
            if (attackGroup <= 0) throw new ArgumentOutOfRangeException(nameof(attackGroup));

            ActorId = actorId;
            AttackOrdinal = attackOrdinal;
            AttackGroup = attackGroup;
            AffectedCells = Array.AsReadOnly((affectedCells ?? throw new ArgumentNullException(nameof(affectedCells))).ToArray());
            Hits = Array.AsReadOnly((hits ?? throw new ArgumentNullException(nameof(hits))).ToArray());
            Knockbacks = Array.AsReadOnly((knockbacks ?? Array.Empty<AttackKnockbackSnapshot>()).ToArray());
            TraitActivations = Array.AsReadOnly((traitActivations ?? Array.Empty<TraitActivationSnapshot>()).ToArray());
            CategoryActivations = Array.AsReadOnly((categoryActivations ?? Array.Empty<CategoryActivationSnapshot>()).ToArray());
            ProjectilePath = Array.AsReadOnly((projectilePath ?? Array.Empty<CellCoord>()).ToArray());
            IncomingDirections = new ReadOnlyDictionary<int, CellCoord>(incomingDirections == null
                ? new Dictionary<int, CellCoord>() : new Dictionary<int, CellCoord>(incomingDirections));
            DamageByRecipient = new ReadOnlyDictionary<EntityId, PlayerSkillDamageBreakdown>(damageByRecipient == null
                ? new Dictionary<EntityId, PlayerSkillDamageBreakdown>() : new Dictionary<EntityId, PlayerSkillDamageBreakdown>(damageByRecipient));
            ImpactCells = new ReadOnlyDictionary<int, CellCoord>(impactCells == null
                ? new Dictionary<int, CellCoord>() : new Dictionary<int, CellCoord>(impactCells));
            if (ImpactCells.Any(pair => !IncomingDirections.ContainsKey(pair.Key) || !ProjectilePath.Contains(pair.Value)))
                throw new ArgumentException("투사체 접촉 칸은 같은 접촉의 방향과 실제 경로에 속해야 합니다.", nameof(impactCells));
            if (Hits.Any(hit => hit.TargetId.IsEmpty) || Hits.Select(hit => hit.TargetId).Distinct().Count() != Hits.Count ||
                Hits.SelectMany(hit => hit.Contacts ?? Array.Empty<AttackHitContactSnapshot>()).Any(contact =>
                    contact.Origin.AttackGroup != attackGroup || contact.Origin.CreditedCharacterId != actorId))
                throw new ArgumentException("스킬의 실제 수신자와 접촉 출처가 공격 발생에 일치해야 합니다.", nameof(hits));
            if (Knockbacks.Any(value => value == null) || Knockbacks.Select(value => value.TargetId).Distinct().Count() != Knockbacks.Count ||
                TraitActivations.Any(value => value == null) || CategoryActivations.Any(value => value == null))
                throw new ArgumentException("스킬의 넉백·효과 근거는 유효한 발생마다 한 번만 기록해야 합니다.");
            if (DamageByRecipient.Keys.Any(id => !Hits.Any(hit => hit.TargetId == id)))
                throw new ArgumentException("피해 계산 내역은 실제 수신자에게 연결해야 합니다.", nameof(damageByRecipient));
            if (IncomingDirections.Any(pair => pair.Key < 0 || Math.Abs((long)pair.Value.X) + Math.Abs((long)pair.Value.Y) != 1))
                throw new ArgumentException("투사체 접촉에는 유효한 순번과 한 칸 상하좌우 방향이 필요합니다.", nameof(incomingDirections));
        }
    }

    public sealed class PlayerSkillResult
    {
        public PlayerSkillKey SkillKey { get; }
        public PlayerSkillUpgradeKey UpgradeKey { get; }
        public IReadOnlyList<CombatEvent> Events { get; }
        public PlayerSkillUseSettlement UseSettlement { get; }

        public PlayerSkillResult(PlayerSkillDefinition definition, PlayerSkillUpgradeKey upgradeKey,
            IReadOnlyList<CombatEvent> events, PlayerSkillUseSettlement useSettlement)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (upgradeKey != PlayerSkillUpgradeKey.None && !definition.SupportsUpgrade(upgradeKey))
                throw new ArgumentException("스킬에 속하지 않는 강화입니다.", nameof(upgradeKey));
            var copy = (events ?? throw new ArgumentNullException(nameof(events))).ToArray();
            if (copy.Any(occurrence => occurrence == null))
                throw new ArgumentException("스킬 사건에는 null을 넣을 수 없습니다.", nameof(events));

            SkillKey = definition.Key;
            UpgradeKey = upgradeKey;
            Events = Array.AsReadOnly(copy);
            UseSettlement = useSettlement ?? throw new ArgumentNullException(nameof(useSettlement));
            var primary = copy.OfType<CombatPlayerSkillAttackEvent>().ToArray();
            if (copy.Distinct().Count() != copy.Length || primary.Length == 0 ||
                primary.Where((occurrence, index) => occurrence.Attack.AttackOrdinal != index ||
                    occurrence.Attack.ActorId != useSettlement.ActorId).Any() ||
                primary.Select(occurrence => occurrence.Attack.AttackGroup).Distinct().Count() != primary.Length)
                throw new ArgumentException("스킬 발생은 같은 행위자의 순서 있는 고유 공격과 하나의 사용 정산이어야 합니다.", nameof(events));
        }

        internal void ValidateCommand(CombatCommandKind kind, EntityId actorId, IReadOnlyList<CombatEvent> events)
        {
            if (kind != CombatCommandKind.UsePlayerSkill || actorId != UseSettlement.ActorId)
                throw new ArgumentException("스킬 결과와 명령의 종류·행위자가 일치해야 합니다.");
            int cursor = 0;
            foreach (var occurrence in Events)
            {
                while (cursor < events.Count && !ReferenceEquals(events[cursor], occurrence)) cursor++;
                if (cursor == events.Count) throw new ArgumentException("스킬 결과는 명령의 같은 사건 인스턴스를 같은 순서로 참조해야 합니다.");
                cursor++;
            }
            if (!events.OfType<CombatPlayerSkillAttackEvent>().SequenceEqual(Events.OfType<CombatPlayerSkillAttackEvent>()))
                throw new ArgumentException("명령의 모든 일차 스킬 발생은 하나의 스킬 결과에 포함되어야 합니다.");
        }
    }
}
