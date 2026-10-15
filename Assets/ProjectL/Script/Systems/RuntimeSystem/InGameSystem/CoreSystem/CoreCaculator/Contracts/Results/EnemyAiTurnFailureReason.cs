using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public enum EnemyAiTurnFailureReason
    {
        None = 0,
        NoActiveUnit,
        ActiveUnitNotEnemy, // 유닛이 enemy 유닛이 아님
        EndTurnFailed, // ai 턴 종료 실패
        StageEnded,
    }
}
