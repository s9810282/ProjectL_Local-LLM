using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem
{
    /// <summary>View의 최신 미리보기 요청에 응답한다. 게임 상태와 명령 처리 번호는 바꾸지 않는다.</summary>
    public interface ICombatPreviewService : IServiceModule
    {
        /// <summary>응답을 게시했으면 true다. 새 요청이 없거나 계산 중 요청·전투가 바뀌었으면 false다.</summary>
        bool ProcessLatestPreview();
    }
}
