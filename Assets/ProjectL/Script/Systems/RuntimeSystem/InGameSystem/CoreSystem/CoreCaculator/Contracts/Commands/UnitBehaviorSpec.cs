namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    /// <summary>
    /// 유닛 정의에 고정된 행동 가능 여부다. 상태·효과 훅이 없으므로 Trait로 표현하지 않는다.
    /// 같은 의미를 Trait나 비정상 비용 수치로 다시 만들지 않는다.
    /// </summary>
    public sealed class UnitBehaviorSpec
    {
        /// <summary>자발 이동·기본 공격이 가능하고 강제 이동을 받는 일반 유닛이다.</summary>
        public static UnitBehaviorSpec Default { get; } = new UnitBehaviorSpec(true, true, false);

        public bool CanMoveVoluntarily { get; }
        public bool CanBasicAttack { get; }
        /// <summary>공격 넉백·타일 강제 이동·토네이도 재배치 등 모든 강제 이동을 받지 않는다(G9). 피해는 정상으로 받는다.</summary>
        public bool IsForcedMovementImmune { get; }

        public UnitBehaviorSpec(bool canMoveVoluntarily, bool canBasicAttack, bool isForcedMovementImmune)
        {
            CanMoveVoluntarily = canMoveVoluntarily;
            CanBasicAttack = canBasicAttack;
            IsForcedMovementImmune = isForcedMovementImmune;
        }
    }
}
