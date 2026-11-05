using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 대신 맞은 수호자가 돌아오기 전에 기다리는 조각입니다(유닛 행동 설계 AA5, 계획 TP-13). 그 출처의 맞는 순간 후속 중 그 유닛 것(피격·사망·밀림)이 모두 끝나면 끝납니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 판정은 <see cref="Tick"/>에서만 합니다(시작 순간에는 끝나지 않음). 맞는 순간 해제가 일어난 프레임에는 후속 사건이 아직 시작 전이라
    /// 시작 순간에 물으면 "끝남"으로 잘못 답할 수 있기 때문입니다.
    /// </para>
    /// <para>
    /// "그 유닛 연출이 비었음"은 따로 묻지 않습니다. 피격·사망 반응이 끝난 뒤에도 남는 위치 소유는 자세 대여뿐이고, 다음 돌아오기 조각이 자세가 빌 때까지
    /// 기다립니다(<see cref="ICombatUnitStand.CanMove"/>).
    /// </para>
    /// </remarks>
    internal sealed class CombatUnitSettleWaitPiece : ICombatPresentationPiece
    {
        private readonly EntityId _unit;
        private readonly CombatEvent _source;
        private readonly ICombatSourceSettlement _settlement;
        private bool _ended;

        /// <param name="unit">기다릴 유닛(수호자)입니다.</param>
        /// <param name="source">그 공격 출처(공격 사건 또는 추가 공격 시작 발동 사건)입니다. 결과 안의 같은 사건 인스턴스여야 합니다.</param>
        /// <param name="settlement">순차 재생기의 후속 정리 질문입니다.</param>
        internal CombatUnitSettleWaitPiece(EntityId unit, CombatEvent source, ICombatSourceSettlement settlement)
        {
            if (unit.IsEmpty)
                throw new ArgumentException("기다릴 유닛이 비어 있습니다.", nameof(unit));
            _unit = unit;
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _settlement = settlement ?? throw new ArgumentNullException(nameof(settlement));
        }

        public bool IsEnded => _ended;

        public void Begin(CombatPieceContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            _ended = false;
        }

        public void Tick(float deltaTime)
        {
            if (!_ended)
                _ended = _settlement.IsSettled(_source, _unit);
        }

        public void Cancel()
        {
        }
    }
}
