using ProjectL.Systems.ServiceKeys.Runtime.Global.Render;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.SelectOutlineSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.AreaOutlineSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.DrawPointLineSystem;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View
{
    /// <summary>
    /// 공용 렌더 인프라를 조합한 상위 View 렌더링 기능입니다.
    /// </summary>
    public interface IRenderViewServiceContext : IServiceModule
    {
        ISelectOutlineSystemService SelectOutlineSystemServices { get; }
        IAreaOutlineSystemService AreaOutlineSystemServices { get; }
        IDrawPointLineSystemService DrawPointLineSystemServices { get; }
    }

    internal sealed class RenderViewGroup : IRenderViewServiceContext
    {
        public ISelectOutlineSystemService SelectOutlineSystemServices { get; }
        public IAreaOutlineSystemService AreaOutlineSystemServices { get; }
        public IDrawPointLineSystemService DrawPointLineSystemServices { get; }

        public RenderViewGroup(ISystemReader systemReader)
        {
            SelectOutlineSystemServices = systemReader.ServiceStoreReader.GetService(RenderSystemServiceKey.SelectOutlineSystem);
            AreaOutlineSystemServices = systemReader.ServiceStoreReader.GetService(RenderSystemServiceKey.AreaOutlineSystem);
            DrawPointLineSystemServices = systemReader.ServiceStoreReader.GetService(RenderSystemServiceKey.DrawPointLineSystem);
        }
    }
}
