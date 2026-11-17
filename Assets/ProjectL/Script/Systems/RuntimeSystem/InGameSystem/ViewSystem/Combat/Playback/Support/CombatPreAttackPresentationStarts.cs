using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>
    /// 선행 구간 사건의 연출 시작을 그 공격의 조기 동작 예약에 잇는 어댑터입니다(클립 칸 다시 짜기 설계 CS11, 계획 TP-12).
    /// 시작 기준이 <see cref="CombatAttackMotionStart.WithLead"/>인 공격만 다루고, 특성 종류는 보지 않습니다. 입구는 둘이고 사건 Presenter가 부릅니다:
    /// 사건 연출을 시작한 곳(사건 처리기 시작·걸음 시작)의 <see cref="EventStarted"/>, 그보다 앞서 사건이 닿는 곳(사건 시작·걸음 시작 입구)의 <see cref="TakeLeadHold"/>(앞당김).
    /// 연출 없이 바로 끝난 사건은 부르는 쪽이 거릅니다(설계 P19). 예약·시계·첫 시작 선택은 수신자의 책임이며 이 객체는 결과·세대가 지난 통지를 버립니다.
    /// </summary>
    internal sealed class CombatPreAttackPresentationStarts
    {
        private readonly Func<EntityId, CombatClipAttackData> _clipOf;
        private readonly Func<int> _generation;
        // WithLead 공격 사건 위치 → 그 공격 칸 값입니다. 시작 지연·붙잡을 초는 값이 답합니다(묶음 끝 검토 L2 — 사본을 두지 않음).
        private readonly Dictionary<int, CombatClipAttackData> _clips = new();
        // 앞당김을 이미 처리한 공격 사건 위치입니다. 공격마다 선행 구간의 처음 닿은 사건 하나만 붙잡습니다.
        private readonly HashSet<int> _held = new();
        private CombatResult _result;
        private CombatPreAttackLeadMap _map;
        private Action<CombatPreAttackLead, float, bool> _reserve;
        private Func<CombatPreAttackLead, bool> _beginNow;

        internal CombatPreAttackPresentationStarts(Func<EntityId, CombatClipAttackData> clipOf, Func<int> generation)
        {
            _clipOf = clipOf ?? throw new ArgumentNullException(nameof(clipOf));
            _generation = generation ?? throw new ArgumentNullException(nameof(generation));
        }

        /// <summary>
        /// 같은 재생 수명의 예약 수신자를 연결합니다.
        /// </summary>
        /// <param name="reserve">예약 입구입니다. 인자는 (선행 구간 사건, 시작 지연 초, 그 사건이 아직 처리되지 않았는지)이고,
        /// 셋째 인자는 앞당김에서만 참입니다(사건을 붙잡기 전에 예약하므로 그 사건 자신도 아직 처리 전).</param>
        /// <param name="beginNow">앞당김 예약을 지금 시작시켜 보고, 그 공격 동작이 실제로 시작했는지 돌려줍니다. 시작했을 때만 사건을 붙잡습니다(묶음 끝 검토 M2).</param>
        internal void BindReceiver(Action<CombatPreAttackLead, float, bool> reserve, Func<CombatPreAttackLead, bool> beginNow)
        {
            _reserve = reserve ?? throw new ArgumentNullException(nameof(reserve));
            _beginNow = beginNow ?? throw new ArgumentNullException(nameof(beginNow));
        }

        internal void Begin(CombatResult result)
        {
            Clear();
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            for (int i = 0; i < result.Events.Count; i++)
            {
                // 포효는 전용 코드 모션이며 일반 공격 클립 설정으로 바꾸지 않습니다.
                if (result.Events[i] is not CombatAttackEvent attackEvent)
                    continue;
                CombatClipAttackData clip = _clipOf(attackEvent.Attack.AttackerId);
                if (clip != null && clip.StartsWithLead)
                    _clips.Add(i, clip);
            }
            // 기본 기준만 있는 결과는 소속 검사와 색인 할당을 요구하지 않습니다.
            if (_clips.Count == 0)
                return;
            _map = CombatPreAttackLeadMap.Build(result, _generation(), _clips.ContainsKey);
            _result = result;
        }

        /// <summary>그 사건이 WithLead 공격의 선행 구간 안인지입니다. 아니면(대부분의 사건) 부르는 쪽이 시작 확인을 하지 않습니다.</summary>
        internal bool IsLead(CombatEvent item) => _map != null && _map.TryGet(item, out _);

        /// <summary>
        /// 선행 구간 사건 하나의 연출이 시작됐습니다. 그 공격의 조기 동작 예약에 알립니다. 첫 시작만 기준으로 쓰는 것은 수신자의 책임입니다.
        /// 선행 구간 밖 사건·지난 세대의 통지는 버립니다.
        /// </summary>
        internal void EventStarted(CombatEvent item, int generation)
        {
            if (TryFind(item, generation, out CombatPreAttackLead lead, out CombatClipAttackData clip))
                _reserve?.Invoke(lead, clip.MotionStartDelay, false);
        }

        /// <summary>
        /// 앞당김(음수 지연) 공격의 선행 구간에서 처음 닿은 사건이면 그 공격 동작을 지금 예약·시작하고, 시작했으면 그 사건 연출을 붙잡을 초(<see cref="CombatClipAttackData.LeadHoldSeconds"/>)를 돌려줍니다(11-17 사용자 결정 A).
        /// 공격마다 한 번이고, 앞당기지 않는 공격·선행 구간 밖 사건·지난 세대는 0입니다.
        /// 붙잡을 수 없거나(<paramref name="canHold"/> 거짓 — 다른 걸음이 진행 중) 동작을 지금 시작하지 못하면(앞 연출이 남음 등, 묶음 끝 검토 M2) 경고 한 번 뒤 0이고,
        /// 동작은 앞당김 없이 막힌 것이 풀릴 때(늦어도 그 공격 사건) 시작합니다. 사건만 늦고 동작은 안 보이는 일을 막습니다.
        /// </summary>
        internal float TakeLeadHold(CombatEvent item, int generation, bool canHold)
        {
            if (!TryFind(item, generation, out CombatPreAttackLead lead, out CombatClipAttackData clip) || clip.LeadHoldSeconds <= 0f ||
                !_held.Add(lead.Attack.EventIndex))
                return 0f;
            if (!canHold)
            {
                WarnNoHold(lead, "선행 구간 첫 사건이 닿을 때 다른 걸음이 진행 중이라");
                return 0f;
            }
            _reserve?.Invoke(lead, 0f, true);
            bool started = _beginNow != null && _beginNow(lead);
            if (generation != _generation())
                return 0f;
            if (!started)
            {
                WarnNoHold(lead, "선행 구간 첫 사건이 닿을 때 앞 연출이 남아 공격 동작을 시작하지 못해");
                return 0f;
            }
            return clip.LeadHoldSeconds;
        }

        private void WarnNoHold(CombatPreAttackLead lead, string reason) =>
            Debug.LogWarning($"[CombatPreAttackPresentationStarts] 전투 {_result.BattleInstanceId}, 결과 {_result.Sequence}, 공격 사건 {lead.Attack.EventIndex}: "
                + $"{reason} 앞당김을 쓰지 않습니다(사건을 붙잡지 않음).");

        internal void Clear()
        {
            _result = null;
            _map = null;
            _clips.Clear();
            _held.Clear();
        }

        private bool TryFind(CombatEvent item, int generation, out CombatPreAttackLead lead, out CombatClipAttackData clip)
        {
            clip = null;
            lead = default;
            return _map != null && generation == _generation() && _map.TryGet(item, out lead) &&
                ReferenceEquals(_result, lead.Attack.Result) && lead.Attack.Generation == generation &&
                _clips.TryGetValue(lead.Attack.EventIndex, out clip);
        }
    }
}
