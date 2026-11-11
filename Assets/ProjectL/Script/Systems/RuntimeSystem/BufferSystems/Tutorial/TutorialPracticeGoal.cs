namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial
{
    /// <summary>
    /// 실습 단계를 통과시키는 게임 결과의 종류입니다(튜토리얼 설계 §15.5·§15.11). 단계 규칙(<see cref="TutorialStepRule"/>)이 가지며,
    /// 정의 데이터가 이 값을 그대로 씁니다. 판정은 TutorialPlan이 공개 전투 Buffer를 읽어 합니다. 새 종류는 판정 코드와 함께 추가합니다.
    /// </summary>
    internal enum TutorialPracticeGoal
    {
        /// <summary>설명 단계의 값입니다. 실습 단계에는 쓰지 않습니다.</summary>
        None = 0,
        /// <summary>실제 전투 진입에 성공했습니다(배치 실습).</summary>
        EnterCombat = 1,
        /// <summary>현재 행동 유닛이 한 칸 이상 실제로 이동했고 그 연출이 끝났습니다(이동 실습).</summary>
        MoveAtLeastOneCell = 2,
        /// <summary>일반 공격이 적 유닛 또는 적 넥서스에 명중했고 그 연출이 끝났습니다. 0피해도 인정합니다.</summary>
        AttackEnemyOnce = 3,
        /// <summary>플레이어 턴 종료 명령이 수락됐고 해당 종료 사건의 표시가 끝났습니다.</summary>
        EndPlayerTurnOnce = 4
    }
}
