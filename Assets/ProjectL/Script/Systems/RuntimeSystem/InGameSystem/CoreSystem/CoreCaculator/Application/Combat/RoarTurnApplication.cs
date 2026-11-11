using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>
    /// 진영 턴이 끝날 때 포효 카운터를 정의의 턴 종료 증가량만큼 올린다. 소유자 진영이 끝났으면 자기 진영 값, 상대 진영이 끝났으면 상대 진영 값이다.
    /// 임계값 도달 뒤의 포효 발동은 이 정산이 아니라 명령 끝의 반응 경계(<see cref="BossRoarCalculator.ApplyPendingReactions"/>)가 맡는다.
    /// </summary>
    internal static class RoarTurnApplication
    {
        /// <param name="projected">진영 종료 정리 투영이다. 바뀐 캐릭터를 같은 열쇠로 덮어쓴다.</param>
        /// <param name="events">카운터가 바뀐 캐릭터마다 특성 상태 사건을 남긴다. 체력 변화가 없어 이 사건이 변경 근거다.</param>
        internal static void EndControllerTurn(CombatCommandInput input, IDictionary<EntityId, CharacterCombatFacts> projected,
            Owner endedSide, List<CombatEvent> events)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (projected == null) throw new ArgumentNullException(nameof(projected));
            if (endedSide != Owner.Player && endedSide != Owner.Enemy) throw new ArgumentOutOfRangeException(nameof(endedSide));
            // 피격 카운트와 같이 밤 전투에서만 쌓는다.
            if (!input.State.InCombat || input.State.Phase == CombatFlowPhase.Finished) return;
            foreach (var character in projected.Values.Where(c => (c.Owner == Owner.Player || c.Owner == Owner.Enemy) &&
                c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead).OrderBy(c => c.EntityId).ToArray())
            {
                var status = RoarHitCounter.Status(character);
                if (status == null) continue;
                int amount = status.CountPerTurnEnd(ownSideEnded: character.Owner == endedSide);
                if (amount == 0) continue;
                var final = RoarHitCounter.WithStatus(character, status.WithHitCount(checked(status.HitCount + amount)));
                projected[character.EntityId] = final;
                CombatCommandEvents.TraitState(events, character, final);
            }
        }
    }
}
