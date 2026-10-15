using System.Collections.Generic;

namespace ProjectL.Framework.SystemBase
{
    /// <summary>
    /// 서비스 저장소의 조회(Read) 전용 인터페이스입니다.
    /// 외부나 엔진에서 안전하게 서비스를 검색할 때 사용됩니다.
    /// </summary>
    public interface ISystemServiceStoreReader
    {
        #region 서비스

        TService GetService<TService>(SystemGroupKey<TService> systemGroupKey)
            where TService : class, IServiceModule;

        #endregion
    }

    internal sealed class SystemServiceStore : ISystemServiceStoreReader
    {
        private readonly IReadOnlyList<ServiceSnapshot> _services;

        #region 서비스

        public IReadOnlyList<ServiceSnapshot> Services => _services;

        public SystemServiceStore(
            IReadOnlyList<ServiceSnapshot> services)
        {
            if (services == null)
                throw new System.ArgumentNullException(nameof(services));

            ServiceSnapshot[] copy =
                new ServiceSnapshot[services.Count];

            HashSet<System.Type> registeredTypes =
                new HashSet<System.Type>();

            for (int i = 0; i < services.Count; i++)
            {
                ServiceSnapshot snapshot = services[i];

                if (snapshot.Service == null)
                {
                    throw new System.InvalidOperationException(
                        $"[SystemServiceStore] Service is null. Index: {i}");
                }

                if (!registeredTypes.Add(snapshot.ServiceType))
                {
                    throw new System.InvalidOperationException(
                        $"[SystemServiceStore] Duplicate service type. " +
                        $"ServiceType: {snapshot.ServiceType}");
                }

                copy[i] = snapshot;
            }

            _services = System.Array.AsReadOnly(copy);
        }

        public bool TryGetService<TService>(
            SystemGroupKey<TService> systemGroupKey,
            out TService service)
            where TService : class, IServiceModule
        {
            for (int i = 0; i < _services.Count; i++)
            {
                IServiceModule serviceModule =
                    _services[i].Service;

                if (serviceModule is not TService typedService)
                    continue;

                service = typedService;
                return true;
            }

            service = null;
            return false;
        }

        public TService GetService<TService>(SystemGroupKey<TService> systemGroupKey)
            where TService : class, IServiceModule
        {
            if (TryGetService(systemGroupKey, out TService service))
                return service;

            throw new KeyNotFoundException(
                $"[SystemServiceStore] Service was not found. " +
                $"Requested: {typeof(TService).Name}");
        }

        #endregion
    }
}
