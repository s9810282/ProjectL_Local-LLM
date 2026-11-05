using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 공격 조립의 앞 항목입니다(유닛 행동 설계 AA6, 계획 TP-14). 공격 뼈대보다 먼저 와야 하는 것(대신 맞는 수호자의 나가기)을 넣고,
    /// 뼈대의 맞는 순간 항목이 정해진 뒤 그 뒤에 붙일 것(돌아오기)을 넣습니다. 공격 조립 둘(코드 모션·클립)이 끝 선택 인자로 받습니다.
    /// </summary>
    /// <remarks>
    /// 공격 조립은 조립을 만든 직후 <see cref="Begin"/>을 부르고, 돌려받은 시점에 뼈대 항목(모션·방향·클립)과 공격 칸 큐의 시작(Start)을 둡니다.
    /// 맞는 순간 신호(<see cref="CombatPieceSignal.Hit"/>)를 내는 항목을 만든 뒤 <see cref="AfterHit"/>을 부릅니다. 앞 항목이 없으면 조립은 지금과 같습니다.
    /// </remarks>
    internal sealed class CombatAttackLead
    {
        private readonly Func<CombatComposition, CombatPieceStart> _begin;
        private readonly Action<CombatComposition, int> _afterHit;

        /// <param name="begin">앞 항목을 넣고 뼈대가 시작할 시점을 돌려줍니다.</param>
        /// <param name="afterHit">맞는 순간 신호를 내는 항목 번호를 받아 그 뒤 항목을 넣습니다.</param>
        internal CombatAttackLead(Func<CombatComposition, CombatPieceStart> begin, Action<CombatComposition, int> afterHit)
        {
            _begin = begin ?? throw new ArgumentNullException(nameof(begin));
            _afterHit = afterHit ?? throw new ArgumentNullException(nameof(afterHit));
        }

        /// <summary>조립을 만든 직후 부릅니다. 뼈대가 시작할 시점입니다.</summary>
        internal CombatPieceStart Begin(CombatComposition composition) =>
            _begin(composition ?? throw new ArgumentNullException(nameof(composition)));

        /// <summary>맞는 순간 신호를 내는 항목(<paramref name="hitItem"/>)을 만든 뒤 부릅니다.</summary>
        internal void AfterHit(CombatComposition composition, int hitItem)
        {
            if (composition == null) throw new ArgumentNullException(nameof(composition));
            if (hitItem < 0) throw new ArgumentOutOfRangeException(nameof(hitItem));
            _afterHit(composition, hitItem);
        }
    }
}
