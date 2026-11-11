using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>연발 접촉에 실제 HP 감소 숫자만 정수로 나눕니다. 체력 사실·피격·사망·접촉 수명은 소유하지 않습니다.</summary>
    /// <remarks>
    /// "출처의 각 탄이 맞히는 사건" 색인(<see cref="Begin"/>)은 이 한 곳입니다. 앞 탄의 접촉 피격(<see cref="CombatVolleyContactHits"/>)도
    /// <see cref="ContactEventsOf"/>로 같은 색인을 읽습니다.
    /// </remarks>
    internal sealed class CombatVolleyDamageNumbers
    {
        private readonly CombatUnitHudFeedback _hud;
        private readonly CombatStructureHitFeedback _structure;
        private readonly Func<CombatEvent, CombatEvent> _hitSource;
        private readonly Func<int> _generation;
        private readonly Dictionary<CombatEvent, List<CombatEvent>> _bySource = new();
        private readonly HashSet<CombatEvent> _split = new();

        internal CombatVolleyDamageNumbers(CombatUnitHudFeedback hud, CombatStructureHitFeedback structure,
            Func<CombatEvent, CombatEvent> hitSource, Func<int> generation)
        {
            _hud = hud ?? throw new ArgumentNullException(nameof(hud));
            _structure = structure ?? throw new ArgumentNullException(nameof(structure));
            _hitSource = hitSource ?? throw new ArgumentNullException(nameof(hitSource));
            _generation = generation ?? throw new ArgumentNullException(nameof(generation));
        }

        /// <summary>기존 공격 후속 대응표를 만든 뒤 호출합니다. 결과의 원본 사건 참조만 색인합니다.</summary>
        internal void Begin(CombatResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            Clear();
            foreach (CombatEvent item in result.Events)
            {
                if (!CanSplit(item) || Loss(item) <= 0) continue;
                CombatEvent source = _hitSource(item);
                if (source == null) continue;
                if (!_bySource.TryGetValue(source, out var events))
                    _bySource.Add(source, events = new List<CombatEvent>());
                if (!events.Contains(item)) events.Add(item);
            }
        }

        /// <summary>복수 탄 조립에만 숫자 입구를 줍니다. 호출 순번은 중복 제거된 실제 접촉 순서입니다.</summary>
        internal Action<int> ContactNumbers(CombatEvent source, int count, int version)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 1 || !_bySource.TryGetValue(source, out var events)) return null;
            foreach (CombatEvent item in events)
                _split.Add(item);
            return index =>
            {
                if (version != _generation()) return;
                if (index < 0 || index >= count) throw new ArgumentOutOfRangeException(nameof(index));
                foreach (CombatEvent item in events)
                {
                    if (version != _generation() || !_split.Contains(item)) return;
                    int total = Loss(item);
                    int amount = total / count + (index < total % count ? 1 : 0);
                    ShowNumber(item, amount);
                }
            };
        }

        /// <summary>위치 보정 전 원본으로 중복을 막고, 분할하지 않은 사건은 기존 전체 숫자를 표시합니다.</summary>
        internal void ShowUnsplit(CombatCharacterEvent character, CombatEvent original)
        {
            if (!IsSplit(original))
                _hud.ShowDamage(character.Final.EntityId, Loss(character));
        }

        /// <summary>그 출처의 각 탄이 맞히는 사건(수치 피해로 체력이 준 유닛·건물)입니다. 없으면 빈 목록입니다. 이번 결과 동안만 뜻이 있습니다.</summary>
        internal IReadOnlyList<CombatEvent> ContactEventsOf(CombatEvent source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return _bySource.TryGetValue(source, out var events) ? events : Array.Empty<CombatEvent>();
        }

        /// <summary>원본 사건의 숫자가 탄별 표시로 연결됐는지입니다. 사실·피격 큐 해제와는 별개입니다.</summary>
        internal bool IsSplit(CombatEvent original) => _split.Contains(original);

        internal void Clear()
        {
            _bySource.Clear();
            _split.Clear();
        }

        private static bool CanSplit(CombatEvent item) => item switch
        {
            CombatCharacterEvent character => character.DamageCause == CombatDamageCause.NumericDamage,
            CombatStructureEvent structure => structure.DamageCause == CombatDamageCause.NumericDamage &&
                CombatStructureHitFeedback.CanShow(structure),
            _ => false,
        };

        // 캐릭터의 잃은 체력 원본은 피격 규칙 한 곳입니다(재구성은 사건 전 체력, 재구성 연출 설계 RE1).
        private static int Loss(CombatEvent item) => item switch
        {
            CombatCharacterEvent character => CombatCharacterHits.HealthLoss(character),
            CombatStructureEvent structure when structure.Kind == CombatEventKind.HealthChanged || structure.Kind == CombatEventKind.Died =>
                Math.Max(0, structure.Expected.Health.CurrentHealth - structure.Final.Health.CurrentHealth),
            _ => 0,
        };

        private void ShowNumber(CombatEvent item, int amount)
        {
            if (item is CombatCharacterEvent character)
                _hud.ShowDamage(character.Final.EntityId, amount);
            else if (item is CombatStructureEvent structure)
                _structure.ShowDamage(structure, amount);
        }
    }
}
