using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.RunStart
{
    /// <summary>
    /// 새 Run 호출 결과를 받는 쪽입니다. 호출·검사는 <see cref="RunStartJob"/> 한 곳이 하고, 받는 쪽의 차이만 여기에 둡니다
    /// (로비는 출발 요청에 게시, Stage는 거절이면 멈춤, 검사 후속 정리 계획 TP-R8).
    /// </summary>
    internal interface IRunStartOutcome
    {
        /// <summary>호출 직전에 부릅니다. 이미 취소된 요청이면 false이고, Job은 Core를 부르지 않고 끝냅니다.</summary>
        bool TryStart();

        /// <summary>새 Run 호출 결과를 받습니다. 성공한 결과의 로스터 확인은 이 뒤에 Job이 합니다.</summary>
        void Publish(RosterAcquireResult result);

        /// <summary>Job이 예외로 끝날 때 부릅니다. 원래 예외를 가리지 않게 던지지 않습니다.</summary>
        void Fail();
    }
}
