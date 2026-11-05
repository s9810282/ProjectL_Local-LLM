using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>
    /// 위치 교환 승객의 미뤄 둔 끝 사실 보관입니다(이펙트 끝 규칙 설계 EL11, 계획 TP-E7, 묶음 끝 검토 M3에서 보인 사실에서 뺌).
    /// 쥔 것은 아직 아무도 가져가지 않았고, 가져간 것은 위치 이동 행동이 끝날 때 보인 사실이 반영합니다. 반영·그리기는 보인 사실(<see cref="CombatShownFacts"/>)이 하고,
    /// 이 클래스는 상태(쥠·가져감·버림)만 갖습니다.
    /// </summary>
    internal sealed class CombatDeferredPlacements
    {
        private readonly struct Entry
        {
            internal Entry(CharacterCombatFacts final, bool preservePlacement = false)
            {
                Final = final;
                PreservePlacement = preservePlacement;
            }

            internal CharacterCombatFacts Final { get; }
            internal bool PreservePlacement { get; }
        }

        private readonly Dictionary<EntityId, Entry> _held = new();
        private readonly Dictionary<EntityId, Entry> _claimed = new();

        /// <summary>쥐었지만 아직 아무도 가져가지 않은 배치가 있는지입니다.</summary>
        internal bool HasHeld => _held.Count > 0;

        /// <summary>승객의 끝 사실을 쥡니다(교환 쌍이 시작될 때, 계획 TP-E7 ①). 같은 유닛을 두 번 쥐면 예외입니다.</summary>
        internal void Hold(CharacterCombatFacts facts)
        {
            if (facts == null) throw new ArgumentNullException(nameof(facts));
            if (_held.ContainsKey(facts.EntityId) || _claimed.ContainsKey(facts.EntityId))
                throw new InvalidOperationException($"미뤄 둔 배치가 중복 등록됐습니다: {facts.EntityId}");
            _held.Add(facts.EntityId, new Entry(facts));
        }

        /// <summary>쥔 배치를 가져갑니다(위치 이동 행동, 계획 TP-E7 ②). 없으면 false입니다.</summary>
        internal bool TryClaim(EntityId id, out CharacterCombatFacts facts)
        {
            facts = null;
            if (!_held.Remove(id, out Entry entry))
                return false;
            _claimed.Add(id, entry);
            facts = entry.Final;
            return true;
        }

        /// <summary>가져간 배치를 다시 쥔 것으로 돌립니다. 행동을 시작하지 못했을 때 씁니다.</summary>
        internal void Unclaim(EntityId id)
        {
            if (PreservesPlacement(id))
                throw new InvalidOperationException("동시 위치 교환의 배치는 개별 행동에 돌려놓을 수 없습니다.");
            if (_claimed.Remove(id, out Entry entry))
                _held[id] = entry;
        }

        /// <summary>가져간 배치를 꺼냅니다. 그 사이 더 새 사실이 와서 버려졌으면 false입니다.</summary>
        internal bool TryTakeClaimed(EntityId id, out CharacterCombatFacts facts)
        {
            facts = null;
            if (!_claimed.TryGetValue(id, out Entry entry))
                return false;
            if (entry.PreservePlacement)
                throw new InvalidOperationException("동시 위치 교환의 배치는 쌍으로만 꺼내야 합니다.");
            _claimed.Remove(id);
            facts = entry.Final;
            return true;
        }

        /// <summary>시간 후 함께 반영할 두 배치를 바로 가져갑니다. 중간 상태 갱신은 현재 표시 위치를 보존합니다.</summary>
        internal void ClaimPair(CharacterCombatFacts first, CharacterCombatFacts partner)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (partner == null) throw new ArgumentNullException(nameof(partner));
            if (first.EntityId == partner.EntityId || _held.ContainsKey(first.EntityId) || _claimed.ContainsKey(first.EntityId) ||
                _held.ContainsKey(partner.EntityId) || _claimed.ContainsKey(partner.EntityId))
                throw new InvalidOperationException("동시 위치 교환 참여자의 보류 배치가 중복됐습니다.");
            _claimed.Add(first.EntityId, new Entry(first, preservePlacement: true));
            _claimed.Add(partner.EntityId, new Entry(partner, preservePlacement: true));
        }

        internal bool PreservesPlacement(EntityId id) => _claimed.TryGetValue(id, out Entry entry) && entry.PreservePlacement;

        /// <summary>두 배치를 먼저 검사한 뒤 함께 꺼냅니다. 한쪽만 반영하는 경로는 없습니다.</summary>
        internal void TakePair(EntityId firstId, EntityId partnerId, out CharacterCombatFacts first, out CharacterCombatFacts partner)
        {
            if (firstId == partnerId || !_claimed.TryGetValue(firstId, out Entry firstEntry) || !firstEntry.PreservePlacement ||
                !_claimed.TryGetValue(partnerId, out Entry partnerEntry) || !partnerEntry.PreservePlacement)
                throw new InvalidOperationException("동시 위치 교환의 두 배치를 모두 가져온 상태여야 합니다.");
            first = firstEntry.Final;
            partner = partnerEntry.Final;
            _claimed.Remove(firstId);
            _claimed.Remove(partnerId);
        }

        /// <summary>쥔 배치를 모두 꺼내 <paramref name="into"/>에 담고 비웁니다(가져간 것은 그대로).</summary>
        internal void TakeAllHeld(List<CharacterCombatFacts> into)
        {
            if (into == null) throw new ArgumentNullException(nameof(into));
            foreach (Entry entry in _held.Values)
                into.Add(entry.Final);
            _held.Clear();
        }

        /// <summary>그 유닛의 쥔·가져간 배치를 버립니다. 더 새 사실이 이깁니다(계획 TP-E7 ④).</summary>
        internal void Drop(EntityId id)
        {
            _held.Remove(id);
            _claimed.Remove(id);
        }

        /// <summary>모두 버립니다(결과 시작·취소·끝 — 최종 사실이 맞춤, 계획 TP-E7 ⑤).</summary>
        internal void Clear()
        {
            _held.Clear();
            _claimed.Clear();
        }
    }
}
