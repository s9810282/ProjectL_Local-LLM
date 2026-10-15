using System;

namespace ProjectL.Framework.SystemBase
{
    public interface ISystemReader
    {
        #region 서비스

        ISystemServiceStoreReader ServiceStoreReader { get; }

        #endregion

        #region 시스템 모듈

        ISystemModuleStoreReader SystemModuleStoreReader { get; }

        #endregion
    }

    public sealed class SystemReader : ISystemReader
    {
        #region 서비스

        public ISystemServiceStoreReader ServiceStoreReader { get; }

        #endregion

        #region 시스템 모듈

        public ISystemModuleStoreReader SystemModuleStoreReader { get; }

        #endregion

        internal SystemReader(
            ISystemServiceStoreReader serviceStoreReader,
            ISystemModuleStoreReader systemModuleStoreReader)
        {
            ServiceStoreReader = serviceStoreReader
                ?? throw new ArgumentNullException(nameof(serviceStoreReader));

            SystemModuleStoreReader = systemModuleStoreReader
                ?? throw new ArgumentNullException(nameof(systemModuleStoreReader));
        }
    }
}
