using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.ThornBombs
{
    /// <summary>
    /// 게시할 전이의 가시 폭탄 상태와 부착 사건을 전투 사실과 대조한다. Buffer는 이 검증을 호출만 한다
    /// (<see cref="MovementUndoPolicy"/>·기믹 사건 검증과 같은 경계). 만료 순번은 <see cref="ThornBombRules"/>,
    /// 대상 모양은 공격 프로필의 원본을 그대로 쓴다.
    /// </summary>
    public static class ThornBombPublicationPolicy
    {
        public static void ValidatePlacement(CombatCommandTransition transition, IReadOnlyList<CharacterCombatFacts> characters)
        {
            if (transition == null) throw new ArgumentNullException(nameof(transition));
            if (characters == null) throw new ArgumentNullException(nameof(characters));
            var input = transition.Input;
            var final = transition.FinalState;
            foreach (var bomb in final.ThornBombs.Bombs)
                ValidateBomb(bomb, requireSource: false, requireLivingTarget: !transition.Outcome.IsStageEnded);

            long sequence = input.State.Turn.ControllerTurnSequence;
            foreach (var fact in transition.Events)
            {
                if (fact is CombatTurnEvent { Kind: CombatEventKind.ControllerTurnStarted } started &&
                    started.Expected.ControllerTurnSequence == sequence &&
                    started.Final.ControllerTurnSequence == final.Turn.ControllerTurnSequence)
                    sequence = started.Final.ControllerTurnSequence;

                if (fact is not CombatThornBombAttachedEvent attached) continue;
                if (sequence < 1 || attached.Bomb.DueControllerTurnSequence != ThornBombRules.CalculateDueSequence(sequence))
                    throw new InvalidOperationException("가시 폭탄 만료 순번이 부착 시점과 다릅니다.");

                // 같은 명령에서 부착·소비된 폭탄도 출처와 대상의 로컬 칸을 확인한다.
                ValidateBomb(attached.Bomb, requireSource: true, requireLivingTarget: false);
            }

            void ValidateBomb(ThornBombSnapshot bomb, bool requireSource, bool requireLivingTarget)
            {
                var target = characters.FirstOrDefault(c => c.EntityId == bomb.TargetId);
                if (target == null || (requireLivingTarget && (target.Health.IsDead || target.Area != CharacterRuntimeArea.Field)) ||
                    CombatCommandCalculator.RequiredProfile(input, target).Definition.FootprintLocalCells.Span
                        .IndexOf(bomb.AttachmentLocalCell) < 0)
                    throw new InvalidOperationException("가시 폭탄 대상 또는 부착 로컬 칸이 잘못되었습니다.");

                // 부착이 끝난 폭탄은 사망·퇴장한 출처가 없어도 유효하다.
                var source = characters.FirstOrDefault(c => c.EntityId == bomb.SourceId) ??
                    input.Characters.FirstOrDefault(c => c.EntityId == bomb.SourceId);
                if (source == null ? requireSource : source.Owner != bomb.SourceOwner)
                    throw new InvalidOperationException("가시 폭탄의 출처가 전투 사실과 다릅니다.");
            }
        }
    }
}
