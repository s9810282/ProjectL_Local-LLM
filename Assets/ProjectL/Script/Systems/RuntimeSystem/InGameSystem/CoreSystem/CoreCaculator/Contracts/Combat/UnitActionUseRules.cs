namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>
    /// 한 진영 턴에서 유닛이 일반 행동을 시작할 수 있는 행동 사용 상태의 유일한 원본이다.
    /// 이동 → 공격 순서로 둘 다 할 수 있고, 공격한 뒤에는 이동도 공격도 할 수 없다.
    /// 입장 검사·공격 결과 계약·Buffer 게시 검사가 함께 읽는다(설계 2026-10-27 이동 후 공격).
    /// </summary>
    public static class UnitActionUseRules
    {
        public static bool CanStartMove(UnitActionUse use) => use == UnitActionUse.Available;

        public static bool CanStartAttack(UnitActionUse use) =>
            use == UnitActionUse.Available || use == UnitActionUse.Moved;
    }
}
