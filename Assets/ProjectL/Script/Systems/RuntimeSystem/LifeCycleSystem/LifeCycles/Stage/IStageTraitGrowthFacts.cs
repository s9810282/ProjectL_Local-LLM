using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage
{
    /// <summary>증강 선택 화면이 읽는 Stage 흐름의 사실입니다(설계 §14 TG2). 읽기만 하며 상태를 바꾸지 않습니다.</summary>
    public interface IStageTraitGrowthFacts : IServiceModule
    {
        /// <summary>
        /// 증강 선택이 남아 있는지(선택 필요 또는 결과 확인 대기)입니다. 확인과 다음 제안 준비 사이에도 참이라 화면이 닫히지 않습니다.
        /// Stage가 활성이 아니면 거짓입니다.
        /// </summary>
        bool IsPending { get; }
    }
}
