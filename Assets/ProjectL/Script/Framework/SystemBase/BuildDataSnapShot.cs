using System;
using System.Collections.Generic;

namespace ProjectL.Framework.SystemBase
{
    internal readonly struct SystemBuildSnapshot
    {
        public IReadOnlyList<ServiceSnapshot> Services { get; }

        public IReadOnlyList<ISystemModule> Modules { get; }

        public SystemBuildSnapshot(
            IReadOnlyList<ServiceSnapshot> services,
            IReadOnlyList<ISystemModule> modules)
        {
            Services = CopyServices(services);
            Modules = CopyModules(modules);
        }

        private static IReadOnlyList<ServiceSnapshot> CopyServices(
            IReadOnlyList<ServiceSnapshot> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            ServiceSnapshot[] copy =
                new ServiceSnapshot[source.Count];

            for (int i = 0; i < source.Count; i++)
            {
                copy[i] = source[i];
            }

            return Array.AsReadOnly(copy);
        }

        private static IReadOnlyList<ISystemModule> CopyModules(
            IReadOnlyList<ISystemModule> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            ISystemModule[] copy =
                new ISystemModule[source.Count];

            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] == null)
                {
                    throw new InvalidOperationException(
                        $"[SystemBuildSnapshot] Module is null. Index: {i}");
                }

                copy[i] = source[i];
            }

            return Array.AsReadOnly(copy);
        }
    }

    internal readonly struct ServiceSnapshot
    {
        public Type ServiceType { get; }

        public IServiceModule Service { get; }

        public ServiceSnapshot(
            Type serviceType,
            IServiceModule service)
        {
            ServiceType = serviceType
                ?? throw new ArgumentNullException(nameof(serviceType));

            Service = service
                ?? throw new ArgumentNullException(nameof(service));

            if (serviceType.IsInterface == false)
            {
                throw new InvalidOperationException(
                    $"[ServiceSnapshot] ServiceType must be an interface type. " +
                    $"Type: {serviceType.Name}");
            }

            if (serviceType.IsInstanceOfType(service) == false)
            {
                throw new InvalidOperationException(
                    $"[ServiceSnapshot] Invalid service instance.\n" +
                    $"ServiceType: {serviceType.Name}\n" +
                    $"Instance   : {service.GetType().Name}");
            }
        }

        public TService GetService<TService>()
            where TService : class, IServiceModule
        {
            Type requestedType = typeof(TService);

            if (ServiceType != requestedType)
            {
                throw new InvalidOperationException(
                    $"[ServiceSnapshot] Invalid service request.\n" +
                    $"Registered: {ServiceType.Name}\n" +
                    $"Requested : {requestedType.Name}");
            }

            if (Service is TService typedService)
                return typedService;

            throw new InvalidOperationException(
                $"[ServiceSnapshot] Invalid service cast.\n" +
                $"Registered: {ServiceType.Name}\n" +
                $"Requested : {requestedType.Name}\n" +
                $"Actual    : {Service.GetType().Name}");
        }
    }

    internal readonly struct BuildSystemGroupSnapshot<TContext>
        where TContext : class, IServiceModule
    {
        /// <summary>
        /// 그룹 Context 조회에 사용하는 타입 안전 키.
        /// </summary>
        public SystemGroupKey<TContext> Key { get; }

        /// <summary>
        /// 상위 그룹의 ModuleStore에 등록할 시스템 그룹.
        /// </summary>
        public ISystemModule Group { get; }

        /// <summary>
        /// 해당 그룹이 외부에 제공하는 Context.
        /// </summary>
        public TContext Context { get; }

        public BuildSystemGroupSnapshot(
            SystemGroupKey<TContext> key,
            ISystemModule group,
            TContext context)
        {
            Group = group
                ?? throw new ArgumentNullException(nameof(group));

            Context = context
                ?? throw new ArgumentNullException(nameof(context));

            Key = key;
        }
    }
}