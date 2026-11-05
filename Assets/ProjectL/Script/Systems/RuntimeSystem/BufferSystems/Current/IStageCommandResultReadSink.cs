using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Current
{
    /// <summary>처리 번호와 결과는 Buffer 인스턴스 수명 동안 유지된다. 선택 정리와 새 Run에서도 초기화하지 않는다.</summary>
    public interface IStageCommandResultReadSink : IServiceModule
    {
        int HandledMapCommandNumber { get; }
        int HandledShopCommandNumber { get; }
        bool TryGetMapResult(out StageEntryCommandResult result);
        bool TryGetShopResult(out ShopCommandResult result);
    }

    /// <summary>Core가 번호와 실행 중복을 먼저 검증한 뒤 호출한다. 설치는 검증·할당·외부 호출 없이 필드만 바꾼다.</summary>
    internal interface IStageCommandResultStore : IServiceModule
    {
        void InstallMapResult(StageEntryCommandResult result);
        void InstallShopResult(ShopCommandResult result);
    }
}
