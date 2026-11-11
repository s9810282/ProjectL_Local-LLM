using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>
    /// 연발 앞 탄의 접촉마다 그 출처로 맞은 대상의 피격을 냅니다(2026-11-11 사용자 "아킴보 투사체 2·숫자 2인데 피격 1" → "권장안대로").
    /// </summary>
    /// <remarks>
    /// Core는 공격 하나에 대상마다 피해 사건을 하나 내므로 마지막 탄은 출처 해제가 연 그 사건(피격·사망·체력)이 맡고, 앞 탄은 사실 없이 피격만 냅니다.
    /// 맞은 대상은 숫자 나누기와 같은 색인(<see cref="CombatVolleyDamageNumbers.ContactEventsOf"/>)이고, 유닛·건물 피격을 어떻게 내는지는
    /// 그 주인(캐릭터 사건 처리기·건물 피격 표시)이 정합니다. 이 부품은 "어느 접촉에 누구를"만 압니다.
    /// </remarks>
    internal sealed class CombatVolleyContactHits
    {
        private readonly CombatVolleyDamageNumbers _index;
        private readonly Action<CombatCharacterEvent, int> _character;
        private readonly Action<CombatStructureEvent> _structure;
        private readonly Func<int> _generation;

        /// <param name="index">출처별로 각 탄이 맞히는 사건 색인입니다.</param>
        /// <param name="character">유닛의 접촉 피격 입구입니다(피해 사건, 재생 세대).</param>
        /// <param name="structure">건물의 접촉 피격 입구입니다.</param>
        internal CombatVolleyContactHits(CombatVolleyDamageNumbers index, Action<CombatCharacterEvent, int> character,
            Action<CombatStructureEvent> structure, Func<int> generation)
        {
            _index = index ?? throw new ArgumentNullException(nameof(index));
            _character = character ?? throw new ArgumentNullException(nameof(character));
            _structure = structure ?? throw new ArgumentNullException(nameof(structure));
            _generation = generation ?? throw new ArgumentNullException(nameof(generation));
        }

        /// <summary>
        /// 복수 탄 조립에만 접촉 피격 입구를 줍니다. 인자는 중복 제거된 실제 접촉 순번이고, 마지막 순번(<paramref name="count"/> − 1)은 아무것도 내지 않습니다.
        /// 단발이거나 맞은 대상이 없으면 null입니다.
        /// </summary>
        internal Action<int> ContactHits(CombatEvent source, int count, int version)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 1) return null;
            IReadOnlyList<CombatEvent> events = _index.ContactEventsOf(source);
            if (events.Count == 0) return null;
            return index =>
            {
                if (index < 0 || index >= count) throw new ArgumentOutOfRangeException(nameof(index));
                // 마지막 접촉 뒤에는 출처가 풀려 Core 피해 사건이 피격을 냅니다.
                if (index == count - 1) return;
                for (int i = 0; i < events.Count; i++)
                {
                    if (version != _generation()) return;
                    if (events[i] is CombatCharacterEvent character)
                        _character(character, version);
                    else if (events[i] is CombatStructureEvent structure)
                        _structure(structure);
                }
            };
        }
    }
}
