using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Handlers
{
    /// <summary>
    /// 사건 틀 형식과 그 처리기의 표입니다(Presenter 분리 설계 S3). 어떤 사건이 사건 틀로 보이는지와 그 형식을 맡는 처리기 칸은
    /// 사건 형식 표(<see cref="CombatEventTypeTable"/>)가 원본이고, 이 표는 칸마다 처리기를 맞춥니다(View 2라운드 W7-1).
    /// 모든 사건 형식은 <c>sealed</c>라 형식 동일 비교가 <c>is</c> 검사와 같습니다(등록 때 확인).
    /// </summary>
    internal sealed class CombatEventHandlerTable
    {
        private readonly Dictionary<Type, CombatEventHandler> _handlers = new();

        private CombatEventHandlerTable()
        {
        }

        /// <summary>
        /// 전투 결과 재생의 표를 만듭니다. 사건 틀 형식마다 형식 표의 처리기 칸(<see cref="CombatEventTypeTable.HandlerSlotOf"/>)으로 처리기를 고릅니다.
        /// 연출이 있는 칸 열(끌어오기·특성 발동·토네이도·낙석·하붑·구조물 포함)은 그 처리기, 사실 칸은 사실 처리기입니다. 그래서 사건 틀 형식마다 처리기가 있습니다.
        /// 사실만 반영하는 새 형식은 형식 표에 한 줄을 더하고, 연출이 있는 새 형식은 처리기 칸과 처리기 하나를 만들어
        /// 이 메서드의 인자와 사건 Presenter의 조립에 더합니다.
        /// </summary>
        internal static CombatEventHandlerTable Create(CombatEventHandler attack, CombatEventHandler roar, CombatEventHandler summon,
            CombatEventHandler character, CombatEventHandler turn, CombatEventHandler facts, CombatEventHandler pull,
            CombatEventHandler trait, CombatEventHandler tornado, CombatEventHandler rockfall, CombatEventHandler haboob,
            CombatEventHandler structure, CombatEventHandler reinforcement, CombatEventHandler playerSkill, CombatEventHandler thornBomb)
        {
            var table = new CombatEventHandlerTable();
            IReadOnlyList<Type> eventTypes = CombatEventTypeTable.EventTypes;
            for (int i = 0; i < eventTypes.Count; i++)
            {
                CombatEventHandler handler = CombatEventTypeTable.HandlerSlotOf(eventTypes[i]) switch
                {
                    CombatEventHandlerSlot.Attack => attack,
                    CombatEventHandlerSlot.Roar => roar,
                    CombatEventHandlerSlot.Summon => summon,
                    CombatEventHandlerSlot.Character => character,
                    CombatEventHandlerSlot.Turn => turn,
                    CombatEventHandlerSlot.Pull => pull,
                    CombatEventHandlerSlot.Trait => trait,
                    CombatEventHandlerSlot.Tornado => tornado,
                    CombatEventHandlerSlot.Rockfall => rockfall,
                    CombatEventHandlerSlot.Haboob => haboob,
                    CombatEventHandlerSlot.Reinforcement => reinforcement,
                    CombatEventHandlerSlot.Facts => facts,
                    CombatEventHandlerSlot.Structure => structure,
                    CombatEventHandlerSlot.PlayerSkill => playerSkill,
                    CombatEventHandlerSlot.ThornBomb => thornBomb,
                    // 처리기 칸을 더하고 여기 갈래를 빠뜨리면 사실 처리기로 조용히 가지 않게 만들 때 알립니다.
                    _ => throw new InvalidOperationException($"[CombatEventHandlerTable] 처리기를 맞추지 않은 처리기 칸입니다: {eventTypes[i]}"),
                };
                table.Add(eventTypes[i], handler);
            }
            return table;
        }

        /// <summary>사건의 처리기입니다. 표에 없으면 <see cref="CombatEventHandler.None"/>입니다.</summary>
        internal CombatEventHandler For(CombatEvent e) => For(e.GetType());

        internal CombatEventHandler For(Type eventType) =>
            _handlers.TryGetValue(eventType, out CombatEventHandler handler) ? handler : CombatEventHandler.None;

        private void Add(Type eventType, CombatEventHandler handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (!typeof(CombatEvent).IsAssignableFrom(eventType) || !eventType.IsSealed)
                throw new InvalidOperationException($"[CombatEventHandlerTable] 처리기 표에는 sealed 사건 형식만 넣습니다: {eventType}");
            // 사건 틀로 오지 않는 형식의 처리기는 쓰이지 않습니다. 형식 표와 처리기 표가 어긋나면 만들 때 알립니다.
            if (!CombatEventTypeTable.IsEventType(eventType))
                throw new InvalidOperationException($"[CombatEventHandlerTable] 사건 형식 표의 사건 틀 형식이 아닙니다: {eventType}");
            _handlers.Add(eventType, handler);
        }
    }
}
