using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>
    /// 한 결과에서 같은 유닛의 밀림 걸음으로 바로 이어지는 피격 사건들입니다(넉백 피격 연출, 임시 — 2026-11-11 사용자 결정).
    /// 이 사건은 자세를 빌리는 피격 반응 대신 밀림과 함께 가는 피격(큐·클립만)을 내고 바로 완료해, 유닛이 맞는 순간 밀리기 시작합니다
    /// (2026-11-11 사용자 "피격과 동시에 밀림"). 젖힘은 밀림 걸음이 내고 도착 뒤 폅니다.
    /// 사건 Presenter가 결과 시작에 채우고 취소 때 비웁니다. 결과의 사건 참조(위치 보정 전 원본)로 묻습니다.
    /// </summary>
    /// <remarks>
    /// "바로 이어진다"는 그 유닛의 캐릭터 사건만 보아, 밀림 걸음 바로 앞의 같은 유닛 캐릭터 사건이 피격(<see cref="CombatCharacterHits.IsPushLeadInHit"/>)인 경우입니다.
    /// Core는 직접 피해를 모두 고정한 뒤 밀기 시작하므로(넉백 계산), 피해 있는 넉백의 대상은 피격 → 밀림 걸음 순서입니다.
    /// 밀리는 도중의 피해(지형 등) 뒤에 다음 걸음이 와도 목록에 들지만, 그때는 이미 젖힌 채(표시 회전이 있음)라 캐릭터 처리기가 보통 피격으로 냅니다.
    /// </remarks>
    internal sealed class CombatKnockbackLeadIns
    {
        private readonly HashSet<CombatEvent> _leadIns = new();
        private readonly Dictionary<EntityId, CombatCharacterEvent> _last = new();

        /// <summary>결과 하나의 목록을 새로 채웁니다.</summary>
        internal void Begin(CombatResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            _leadIns.Clear();
            _last.Clear();
            try
            {
                foreach (CombatEvent item in result.Events)
                {
                    if (item is not CombatCharacterEvent character) continue;
                    EntityId id = character.Final.EntityId;
                    // 밀림 = 스스로 하는 이동이 아닌 걸음입니다(넉백·충격 볼트·날려 감 등, 이동 종류 성질 원본 CombatMovementKinds).
                    if (character.Kind == CombatEventKind.Moved && !CombatMovementKinds.IsSelfDriven(CombatMovementKinds.Of(character)) &&
                        _last.TryGetValue(id, out CombatCharacterEvent before) && CombatCharacterHits.IsPushLeadInHit(before))
                        _leadIns.Add(before);
                    _last[id] = character;
                }
            }
            finally
            {
                _last.Clear();
            }
        }

        /// <summary>그 사건(결과의 원본)이 밀림 걸음으로 바로 이어지는 피격인지입니다.</summary>
        internal bool Contains(CombatEvent e) => e != null && _leadIns.Contains(e);

        /// <summary>목록을 비웁니다(재생 취소).</summary>
        internal void Clear() => _leadIns.Clear();
    }
}
