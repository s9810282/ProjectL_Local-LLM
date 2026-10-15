using System;
using System.Collections.Generic;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Framework.Composition
{
    public interface ISystemWriteSink
    {
        #region 서비스

        void AddService<TService>(TService service)
            where TService : class, IServiceModule;

        void AddService(
            Type serviceType,
            object service);

        #endregion

        #region 시스템 모듈

        void AddModule<TSystem>(TSystem system)
            where TSystem : class;

        #endregion
    }

    public sealed class SystemBuilderHelper :
        ISystemWriteSink
    {
        private readonly HashSet<Type> serviceHistory = new();
        private readonly List<ServiceSnapshot> serviceSnapshots = new();

        private readonly HashSet<Type> moduleHistory = new();
        private readonly List<ISystemModule> modules = new();

        private bool isClosed;

        #region 서비스

        public void AddService<TService>(TService service)
            where TService : class, IServiceModule
        {
            AddService(typeof(TService), service);
        }

        public void AddService(
            Type serviceType,
            object service)
        {
            EnsureOpen();
            ValidateService(serviceType, service);

            if (serviceHistory.Add(serviceType) == false)
            {
                throw new InvalidOperationException(
                    $"[SystemBuilderHelper] Service already registered. " +
                    $"ServiceType: {serviceType.Name}");
            }

            serviceSnapshots.Add(
                new ServiceSnapshot(
                    serviceType,
                    (IServiceModule)service));
        }

        #endregion

        #region 시스템 모듈

        public void AddModule<TSystem>(TSystem system)
            where TSystem : class
        {
            EnsureOpen();

            if (system == null)
                throw new ArgumentNullException(nameof(system));

            if (system is not ISystemModule systemModule)
            {
                throw new InvalidOperationException(
                    $"[SystemBuilderHelper] System does not implement " +
                    $"{nameof(ISystemModule)}. " +
                    $"SystemType: {system.GetType().Name}");
            }

            Type systemType = system.GetType();

            ValidateSystemModule(systemType);

            if (moduleHistory.Add(systemType) == false)
            {
                throw new InvalidOperationException(
                    $"[SystemBuilderHelper] Module already registered. " +
                    $"SystemType: {systemType.Name}");
            }

            modules.Add(systemModule);
        }

        #endregion

        public SystemReader BuildSystemReader()
        {
            EnsureOpen();

            isClosed = true;

            var Data = new SystemBuildSnapshot(
                serviceSnapshots.ToArray(),
                modules.ToArray());

            return new SystemReader(
                new SystemServiceStore(Data.Services),
                new SystemModuleStore(Data.Modules));
        }

        private static void ValidateSystemModule(
            Type systemType)
        {
            if (systemType == null)
                throw new ArgumentNullException(nameof(systemType));

            Type[] interfaceTypes =
                systemType.GetInterfaces();

            for (int i = 0; i < interfaceTypes.Length; i++)
            {
                if (IsValidSystemModuleInterface(
                        interfaceTypes[i]))
                {
                    return;
                }
            }

            throw new InvalidOperationException(
                $"[SystemBuilderHelper] " +
                $"No concrete system module interface found. " +
                $"SystemType: {systemType.Name}");
        }

        private static bool IsValidSystemModuleInterface(
            Type interfaceType)
        {
            if (interfaceType == null)
                return false;

            if (interfaceType == typeof(ISystemModule))
                return false;

            if (!typeof(ISystemModule)
                    .IsAssignableFrom(interfaceType))
            {
                return false;
            }

            // IServiceModule은 외부 제공 서비스 마커다.
            // 시스템 생명주기 모듈로 분류하지 않는다.
            if (typeof(IServiceModule)
                .IsAssignableFrom(interfaceType))
            {
                return false;
            }

            return true;
        }

        private static void ValidateService(
            Type serviceType,
            object service)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));

            if (service == null)
                throw new ArgumentNullException(nameof(service));

            if (!serviceType.IsInterface)
            {
                throw new InvalidOperationException(
                    $"[SystemBuilderHelper] " +
                    $"Service must be registered by interface type. " +
                    $"ServiceType: {serviceType.Name}");
            }

            if (serviceType == typeof(IServiceModule))
            {
                throw new InvalidOperationException(
                    $"[SystemBuilderHelper] " +
                    $"Do not register base " +
                    $"{nameof(IServiceModule)} directly.");
            }

            if (!typeof(IServiceModule)
                    .IsAssignableFrom(serviceType))
            {
                throw new InvalidOperationException(
                    $"[SystemBuilderHelper] " +
                    $"Service interface must inherit " +
                    $"{nameof(IServiceModule)}. " +
                    $"ServiceType: {serviceType.Name}");
            }

            if (!serviceType.IsInstanceOfType(service))
            {
                throw new InvalidOperationException(
                    $"[SystemBuilderHelper] Invalid service instance.\n" +
                    $"ServiceType : {serviceType.Name}\n" +
                    $"Instance    : {service.GetType().Name}");
            }
        }

        private void EnsureOpen()
        {
            if (isClosed)
            {
                throw new InvalidOperationException(
                    "[SystemBuilderHelper] " +
                    "Builder is already closed.");
            }
        }
    }
}
