using System.Threading.Tasks;

namespace ProjectL.Framework.SystemBase
{
    #region 시스템 모듈

    internal interface ISystemPreload : ISystemModule
    {
        Task PreloadAsync();
    }

    #endregion
}
