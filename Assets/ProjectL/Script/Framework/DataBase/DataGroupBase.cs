using ProjectL.Framework.SystemBase;

namespace ProjectL.Framework.DataBase
{
    public abstract class DataGroupBase : SystemGroup
    {
        protected DataGroupBase(ISystemReader systemReader) : base(systemReader)
        {

        }
    }
}