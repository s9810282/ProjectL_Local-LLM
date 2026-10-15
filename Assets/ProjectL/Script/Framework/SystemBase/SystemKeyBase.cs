using System;

namespace ProjectL.Framework.SystemBase
{
    #region 서비스

    public readonly struct SystemGroupKey<TContext>
        where TContext : class, IServiceModule
    {
        public string Name { get; }
    
        public SystemGroupKey(string name)
        {
            Name = name
                ?? throw new ArgumentNullException(nameof(name));
        }
    
        public override string ToString()
        {
            return Name;
        }
    }

    #endregion
}
