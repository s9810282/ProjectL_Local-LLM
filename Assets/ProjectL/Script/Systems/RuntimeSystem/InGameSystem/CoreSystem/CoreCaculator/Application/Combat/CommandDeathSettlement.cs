using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>한 명령 안의 사망 위치·정산한 사망과 영혼 개수·필드·소모 기록이다.
    /// 사망 영혼 생성과 도착 수집만 정산하며 체력 변경이나 이동 중단은 호출하는 투영이 판단한다.</summary>
    internal sealed class CommandDeathSettlement
    {
        private readonly HashSet<EntityId> _resolved = new();
        private readonly HashSet<EntityId> _preexisting;
        internal Dictionary<EntityId, CellCoord> DeathAnchors { get; }
        internal SoulEffectCalculator Souls { get; }
        internal Dictionary<CharacterInstanceId, int> Counts { get; }
        internal List<long> ConsumedSouls { get; }
        internal SoulFieldSnapshot SoulField { get; private set; }
        internal ISet<EntityId> ResolvedDeaths => _resolved;

        internal CommandDeathSettlement(Dictionary<CharacterInstanceId, int> counts, SoulFieldSnapshot soulField,
            List<long> consumedSouls, Dictionary<EntityId, CellCoord> deathAnchors, SoulEffectCalculator souls,
            IEnumerable<EntityId> preexistingDeaths)
        {
            Counts = counts ?? throw new ArgumentNullException(nameof(counts));
            SoulField = soulField ?? throw new ArgumentNullException(nameof(soulField));
            ConsumedSouls = consumedSouls ?? throw new ArgumentNullException(nameof(consumedSouls));
            DeathAnchors = deathAnchors ?? throw new ArgumentNullException(nameof(deathAnchors));
            Souls = souls ?? throw new ArgumentNullException(nameof(souls));
            _preexisting = (preexistingDeaths ?? Array.Empty<EntityId>()).ToHashSet();
        }

        /// <summary>명령 시작 때 살아 있었고 아직 정산하지 않은 사망을 사망 위치 기준으로 영혼 필드에 반영한다.</summary>
        internal bool SettleNewDeaths(CombatCommandInput original, IReadOnlyDictionary<EntityId, CharacterCombatFacts> characters,
            bool isNight, List<CombatEvent> events)
        {
            var deaths = characters.Values.Where(c => c.Health.IsDead && !_resolved.Contains(c.EntityId) &&
                    !_preexisting.Contains(c.EntityId) &&
                    !original.Characters.Single(o => o.EntityId == c.EntityId).Health.IsDead)
                .OrderBy(c => c.EntityId).Select(c => new SoulDeathFacts(
                    new CharacterStateChange(original.Characters.Single(o => o.EntityId == c.EntityId), c),
                    DeathAnchors.GetValueOrDefault(c.EntityId, original.Characters.Single(o => o.EntityId == c.EntityId).Anchor ??
                        c.MovementCheckpointAnchor))).ToArray();
            if (deaths.Length == 0) return false;
            var field = Souls.PlanDeaths(SoulField, deaths, isNight, false);
            if (!field.Expected.SameValues(field.Final)) events.Add(new CombatSoulEvent(field));
            SoulField = field.Final;
            foreach (var d in deaths) _resolved.Add(d.Transition.Final.EntityId);
            return true;
        }

        /// <summary>도착한 보유 캐릭터의 영혼 수집을 정산한다. 영혼을 소모했으면 true다.</summary>
        internal bool CollectOnArrival(CombatCommandInput current, Dictionary<EntityId, CharacterCombatFacts> characters,
            CharacterCombatFacts actor, SoulArrivalKind kind, List<CombatEvent> events)
        {
            var field = SoulField;
            int consumedBefore = ConsumedSouls.Count;
            CombatCommandCalculator.Collect(current, Souls, characters, Counts, actor, kind, ref field, ConsumedSouls, events);
            SoulField = field;
            return ConsumedSouls.Count != consumedBefore;
        }
    }
}
