namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem
{
    /// <summary>로딩이 표현 완료를 기다릴 때 사용하는 계약. 씬 로드와 시간 진행은 소유하지 않는다.</summary>
    public interface ILodingPresentation
    {
        bool IsCovered { get; }
        bool IsHidden { get; }
        void BeginCover();
        void BeginReveal();
        void End();
    }
}
