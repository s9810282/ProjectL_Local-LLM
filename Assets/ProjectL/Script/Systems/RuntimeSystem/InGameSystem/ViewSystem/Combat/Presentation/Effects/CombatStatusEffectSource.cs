using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.UnitStatus.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Effects
{
    /// <summary>
    /// 상태 유지 이펙트의 출처입니다(이펙트 끝 규칙 설계 EL2). 필드 위 살아 있는 유닛마다 HUD 아이콘과 같은 규칙(<see cref="CombatUnitStatuses.IsShown"/>)으로
    /// 보이는 상태를 고르고, 그 상태 외형에 유지 이펙트가 있으면 냅니다. 같은 상태가 출처 여럿으로 걸려도 공통 상태 읽기가 하나로 묶어 이펙트도 하나입니다.
    /// </summary>
    /// <remarks>
    /// 상태 읽기(<see cref="StatusEffectReadModel.Capture"/>)는 부를 때마다 목록을 새로 만듭니다. 사실 객체는 바뀌지 않고 보인 사실은 바뀐 유닛만
    /// 새 객체로 바꾸므로, 같은 사실 객체면 지난 읽기를 다시 씁니다(평가 C11-02, HUD의 GC 끊김 설계 GH3와 같은 근거).
    /// 이번 맞추기에 없는 유닛의 읽기는 버립니다.
    /// </remarks>
    internal sealed class CombatStatusEffectSource : ICombatPersistentEffectSource
    {
        private readonly IUnitStatusVisualTableQuery _visuals;
        // 지난 맞추기와 이번 맞추기의 유닛별 읽기입니다. 맞추기가 끝나면 둘을 바꿔, 이번에 없던 유닛은 저절로 빠집니다.
        private Dictionary<EntityId, CapturedStatuses> _read = new();
        private Dictionary<EntityId, CapturedStatuses> _next = new();

        internal CombatStatusEffectSource(IUnitStatusVisualTableQuery visuals)
        {
            _visuals = visuals ?? throw new ArgumentNullException(nameof(visuals));
        }

        public void Collect(IReadOnlyList<CharacterCombatFacts> shown, List<CombatPersistentEffectWant> wants)
        {
            if (shown == null) throw new ArgumentNullException(nameof(shown));
            if (wants == null) throw new ArgumentNullException(nameof(wants));
            _next.Clear();
            for (int i = 0; i < shown.Count; i++)
            {
                CharacterCombatFacts unit = shown[i];
                if (unit.Area != CharacterRuntimeArea.Field || unit.Health.IsDead)
                    continue;
                IReadOnlyList<StatusEffectSnapshot> statuses = StatusesOf(unit);
                for (int s = 0; s < statuses.Count; s++)
                {
                    StatusEffectSnapshot status = statuses[s];
                    if (!CombatUnitStatuses.IsShown(status) || !_visuals.TryGet(status.Key, out UnitStatusVisualData visual) || visual.WhileEffect == null)
                        continue;
                    wants.Add(new CombatPersistentEffectWant(CombatPersistentEffectKey.OfStatus(unit.EntityId, status.Key), visual.WhileEffect, visual.RestartOn));
                }
            }
            Dictionary<EntityId, CapturedStatuses> previous = _read;
            _read = _next;
            _next = previous;
        }

        // 같은 사실 객체면 지난 읽기를, 아니면 새로 읽은 것을 이번 맞추기에 넣고 돌려줍니다.
        private IReadOnlyList<StatusEffectSnapshot> StatusesOf(CharacterCombatFacts unit)
        {
            if (!_read.TryGetValue(unit.EntityId, out CapturedStatuses captured) || !ReferenceEquals(captured.Facts, unit))
                captured = new CapturedStatuses(unit, StatusEffectReadModel.Capture(unit));
            _next[unit.EntityId] = captured;
            return captured.Statuses;
        }

        private readonly struct CapturedStatuses
        {
            internal CapturedStatuses(CharacterCombatFacts facts, IReadOnlyList<StatusEffectSnapshot> statuses)
            {
                Facts = facts;
                Statuses = statuses;
            }

            internal CharacterCombatFacts Facts { get; }

            internal IReadOnlyList<StatusEffectSnapshot> Statuses { get; }
        }
    }
}
