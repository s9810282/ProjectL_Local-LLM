using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.TraitGrowth
{
    /// <summary>표시 계층은 고른 것만 제출합니다. 번호 보관·실행은 요청 소유자가, 결과 확인은 결과를 보여 준 화면이 합니다.</summary>
    public interface ITraitGrowthRequestSubmitter : IServiceModule
    {
        /// <summary>제출했지만 결과를 확인하기 전인 요청이 있는지입니다. 화면 잠금의 원본입니다.</summary>
        bool HasPending { get; }

        int Submit(TraitGrowthChoiceRequest request);
    }
}
