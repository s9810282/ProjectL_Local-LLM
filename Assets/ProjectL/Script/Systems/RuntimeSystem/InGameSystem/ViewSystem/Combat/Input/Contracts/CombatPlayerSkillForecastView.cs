using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts
{
    /// <summary>유닛 하나의 예상 끝 체력입니다(전투 스킬 View 설계 SK2·P4).</summary>
    internal readonly struct CombatHealthPreview
    {
        internal CombatHealthPreview(EntityId targetId, int health)
        {
            TargetId = targetId;
            Health = health;
        }

        internal EntityId TargetId { get; }
        internal int Health { get; }
    }

    /// <summary>
    /// 스킬 예보 답 하나를 그리기 값으로 읽은 것입니다(전투 스킬 View 설계 SK2, 계획 c.1). 예보 사건 목록을 차례로 읽어 영향 칸(사건 51들의 합)·
    /// 실제 경로(투사체 경로가 있는 첫 사건 51)·유닛별 끝 체력(연쇄 포함, P4)·넉백 기록(사건 51들의 것)·넉백 대상의 점유 칸을 모읍니다.
    /// </summary>
    /// <remarks>
    /// 규칙은 Core 답을 옮기기만 합니다. 같은 객체를 다시 쓰므로 <see cref="Read"/>할 때마다 목록을 비우고 채웁니다(호버마다 할당 없음).
    /// 예보 사건은 표시용이라 실제 결과로 확인하지 않습니다(Core 인계 §5).
    /// </remarks>
    internal sealed class CombatPlayerSkillForecastView
    {
        private readonly List<CellCoord> _affected = new();
        private readonly HashSet<CellCoord> _affectedSet = new();
        private readonly List<CellCoord> _path = new();
        private readonly List<CombatHealthPreview> _healths = new();
        private readonly List<AttackKnockbackSnapshot> _knockbacks = new();
        private readonly Dictionary<EntityId, IReadOnlyList<CellCoord>> _occupied = new();
        private readonly Dictionary<EntityId, int> _firstHealth = new();
        private readonly Dictionary<EntityId, int> _lastHealth = new();
        private readonly List<EntityId> _order = new();

        /// <summary>영향 칸입니다(중복 없음, 사건 순서).</summary>
        internal IReadOnlyList<CellCoord> AffectedCells => _affected;

        /// <summary>실제로 지나는 경로 칸입니다(시전자 칸 제외). 투사체 경로가 없으면 비어 있습니다.</summary>
        internal IReadOnlyList<CellCoord> Path => _path;

        /// <summary>체력이 바뀌는 유닛의 끝 체력입니다(죽으면 0, 재구성되면 0 — 공격 미리보기와 같은 규칙).</summary>
        internal IReadOnlyList<CombatHealthPreview> Healths => _healths;

        /// <summary>넉백 기록입니다(사건 51들의 것).</summary>
        internal IReadOnlyList<AttackKnockbackSnapshot> Knockbacks => _knockbacks;

        /// <summary>그 칸이 그려진 영향 칸인지입니다. 범위 격발(남은 점 0)의 "영역 안" 판단입니다(설계 P13).</summary>
        internal bool Contains(CellCoord cell) => _affectedSet.Contains(cell);

        /// <summary>넉백 대상의 지금 점유 칸입니다. Buffer 캐릭터 목록에 없으면 null입니다(그리는 쪽이 시작 칸 하나로 씀).</summary>
        internal IReadOnlyList<CellCoord> OccupiedOf(EntityId targetId) =>
            _occupied.TryGetValue(targetId, out IReadOnlyList<CellCoord> cells) ? cells : null;

        /// <summary>예보 하나를 읽습니다. 받아들여진 예보(<see cref="PlayerSkillForecast.CanUse"/>)만 받습니다.</summary>
        internal void Read(PlayerSkillForecast forecast, IReadOnlyList<CharacterData> characters)
        {
            if (forecast == null) throw new ArgumentNullException(nameof(forecast));
            if (characters == null) throw new ArgumentNullException(nameof(characters));
            if (!forecast.CanUse || forecast.Prediction == null)
                throw new ArgumentException("[CombatPlayerSkillForecastView] 받아들여진 예보만 읽습니다.", nameof(forecast));

            Clear();
            IReadOnlyList<CombatEvent> events = forecast.Prediction.Events;
            for (int i = 0; i < events.Count; i++)
            {
                switch (events[i])
                {
                    case CombatPlayerSkillAttackEvent skill:
                        ReadSkillAttack(skill.Attack);
                        break;
                    case CombatCharacterEvent character:
                        ReadHealth(character);
                        break;
                }
            }

            for (int i = 0; i < _order.Count; i++)
            {
                EntityId id = _order[i];
                int first = _firstHealth[id];
                int last = _lastHealth[id];
                if (last != first)
                    _healths.Add(new CombatHealthPreview(id, last));
            }

            for (int i = 0; i < _knockbacks.Count; i++)
            {
                EntityId target = _knockbacks[i].TargetId;
                if (_occupied.ContainsKey(target))
                    continue;
                CharacterData character = CombatCharacterQueries.Find(characters, target);
                if (character != null)
                    _occupied.Add(target, character.OccupiedCells);
            }
        }

        /// <summary>읽은 값을 비웁니다.</summary>
        internal void Clear()
        {
            _affected.Clear();
            _affectedSet.Clear();
            _path.Clear();
            _healths.Clear();
            _knockbacks.Clear();
            _occupied.Clear();
            _firstHealth.Clear();
            _lastHealth.Clear();
            _order.Clear();
        }

        private void ReadSkillAttack(PlayerSkillAttackResult attack)
        {
            for (int i = 0; i < attack.AffectedCells.Count; i++)
            {
                CellCoord cell = attack.AffectedCells[i];
                if (_affectedSet.Add(cell))
                    _affected.Add(cell);
            }
            // 경로는 투사체 경로가 있는 첫 사건 하나입니다(마탄은 사건 하나, 화약꽃은 경로 없음).
            if (_path.Count == 0)
            {
                for (int i = 0; i < attack.ProjectilePath.Count; i++)
                    _path.Add(attack.ProjectilePath[i]);
            }
            for (int i = 0; i < attack.Knockbacks.Count; i++)
                _knockbacks.Add(attack.Knockbacks[i]);
        }

        // 유닛마다 처음 본 사건의 원래 체력과 마지막 사건의 끝 체력입니다. 재구성은 미리보기 규칙대로 0입니다(CombatUnitHudFeedback.ShownHealthOf와 같은 뜻).
        private void ReadHealth(CombatCharacterEvent character)
        {
            EntityId id = character.Final.EntityId;
            if (!_firstHealth.ContainsKey(id))
            {
                _firstHealth.Add(id, character.Expected.Health.CurrentHealth);
                _order.Add(id);
            }
            int health = character.Kind == CombatEventKind.Reconstructed || character.Final.Health.IsDead
                ? 0
                : character.Final.Health.CurrentHealth;
            _lastHealth[id] = health;
        }
    }
}
