namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat
{
    /// <summary>
    /// 런타임 조립 때 고정하는 전투 실행 설정입니다(설계 W8). Core의 전투 국면이 아닙니다.
    /// 등록 정책의 원본은 골격 설계 §4.1이며, 같은 런타임 안에서 바꾸지 않습니다.
    /// </summary>
    internal enum CombatExecutionMode
    {
        /// <summary>기본 조립입니다. 준비 뒤 명령·미리보기·자동 진행을 실제로 처리합니다.</summary>
        Live = 0,

        /// <summary>개발 빌드 전용 진단입니다. 실제 준비 뒤 처리 Job을 등록하지 않고 입력 기록만 확인합니다.</summary>
        RecordInputOnly = 1
    }
}
