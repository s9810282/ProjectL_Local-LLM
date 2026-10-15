namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>현재 단계의 한 타일 효과만 선택한다. 위험 단계와 생존 후 회복 단계마다 최신 사실로 평가한다.</summary>
    public interface IMovementTileRule
    {
        string Key { get; }
        /// <summary>작성 원본과 이동자로 정해지는 후보 대상이다. 공개 체력 기대값과 실제 규칙 입력에 같은 집합을 쓴다.
        /// <paramref name="current"/>는 현재 명령 투영의 보드 사실이며, 범위형 규칙만 읽고 다른 규칙은 무시한다.</summary>
        System.Collections.Generic.IReadOnlyList<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids.EntityId>
            TargetIds(MovementTileFact tile, ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids.EntityId mover,
                CombatCommandInput current);
        MovementStepEffects Evaluate(MovementStepInput input, MovementTileFact tile);
    }
}
