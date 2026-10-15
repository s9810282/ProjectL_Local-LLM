using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.OutlineSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.AreaOutlineSystem;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View
{
    /// <summary>
    /// 공용 렌더 인프라를 조합한 상위 View 렌더링 기능입니다.
    /// </summary>
    public interface IRenderViewServiceContext : IServiceModule
    {
        IOutlineSystemService OutlineSystemServices { get; }
        IAreaOutlineSystemService AreaOutlineSystemServices { get; }
    }

    internal sealed class RenderViewGroup : IRenderViewServiceContext
    {
        public IOutlineSystemService OutlineSystemServices { get; }
        public IAreaOutlineSystemService AreaOutlineSystemServices { get; }

        public RenderViewGroup(ISystemReader systemReader)
        {
            OutlineSystemServices = systemReader.ServiceStoreReader.GetService(RenderSystemServiceKey.OutlineSystem);
            AreaOutlineSystemServices = systemReader.ServiceStoreReader.GetService(RenderSystemServiceKey.AreaOutlineSystem);
        }
    }
}
