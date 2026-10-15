using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// 결과를 기다리는 시작/턴 종료 명령 하나입니다(설계 §7.2, W5·W14). 종류와 Buffer가 돌려준 번호만 담습니다.
    /// 두 개의 대기 플래그나 별도 번호 필드를 두지 않습니다.
    /// </summary>
    internal readonly struct PendingInputCommand
    {
        internal PendingInputCommand(CombatViewCommandKind kind, int number)
        {
            if (kind != CombatViewCommandKind.EnterCombat && kind != CombatViewCommandKind.EndUnitTurn)
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "대기는 시작·턴 종료 명령만 기록합니다.");
            if (number <= 0)
                throw new ArgumentOutOfRangeException(nameof(number));

            Kind = kind;
            Number = number;
        }

        internal CombatViewCommandKind Kind { get; }

        internal int Number { get; }
    }
}
