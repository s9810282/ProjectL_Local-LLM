using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 근접 클립 공격의 이벤트 줄 하나(맞는 순간)를 내는 시간 없는 조각입니다(클립 칸 다시 짜기 설계 CS2, 결정 Q-A3 A, 계획 TP-9).
    /// 시작하면 그 줄의 접촉 이펙트(Contact·Every, 대상마다)를 내고, 접촉 집계(<see cref="CombatVolleyReleasePiece"/>)에 그 줄의 접촉 하나를 넣습니다.
    /// 앞 줄은 집계가 앞 접촉 피격·숫자 나눔을 내고, 마지막 줄의 접촉이 출처를 모두 해제합니다(연발과 같은 규칙 한 벌).
    /// </summary>
    internal sealed class CombatStrikePiece : ICombatPresentationPiece
    {
        private readonly Action<EntityId, bool> _contact;
        private readonly Action _impact;

        /// <param name="contact">집계의 그 줄 접촉 입구입니다(<see cref="CombatVolleyReleasePiece.ContactCallback"/>). 대상 없이 마지막 접촉으로 넣습니다(남은 대상을 한 번에 해제).</param>
        /// <param name="impact">그 줄의 접촉 이펙트를 내는 입구입니다. 없으면 null입니다.</param>
        internal CombatStrikePiece(Action<EntityId, bool> contact, Action impact)
        {
            _contact = contact ?? throw new ArgumentNullException(nameof(contact));
            _impact = impact;
        }

        public bool IsEnded { get; private set; }

        public void Begin(CombatPieceContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            IsEnded = true;
            _impact?.Invoke();
            _contact(EntityId.Empty, true);
        }

        public void Tick(float deltaTime)
        {
        }

        public void Cancel()
        {
        }
    }
}
