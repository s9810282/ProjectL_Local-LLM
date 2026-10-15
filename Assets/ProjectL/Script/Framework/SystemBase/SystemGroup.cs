using System;
using ProjectL.Framework.Composition;

namespace ProjectL.Framework.SystemBase
{
    public abstract class SystemGroup :
        IExportSystemServices
    {
        protected ISystemReader SystemReader;

        public SystemGroup(ISystemReader systemReader)
        {
            SystemReader = systemReader
                ?? throw new ArgumentNullException(
                    nameof(systemReader));
        }

        #region 시스템 모듈

        public abstract void ExportServices(ISystemWriteSink sink);

        #endregion
    }
}
