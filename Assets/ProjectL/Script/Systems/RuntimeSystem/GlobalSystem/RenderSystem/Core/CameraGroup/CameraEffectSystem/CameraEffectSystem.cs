using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraCatalogSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraEffectSystem.Event;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraEffectSystem
{
    public interface ICameraEffectService : IServiceModule
    {
        void Dispatch(ICameraEventData data);
    }

    internal sealed class CameraEffectSystem : ICameraEffectService, IExportSystemServices,
        ISystemBindScene, ISystemPostBindService, ISystemShutdown
    {
        private readonly ICameraCatalogService catalog;
        private readonly Dictionary<Type, ICameraEvent> events = new Dictionary<Type, ICameraEvent>();
        private CameraEventServiceContext serviceContext;
        private bool ready;

        internal CameraEffectSystem(ICameraCatalogService catalog)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<ICameraEffectService>(this);
            sink.AddModule(this);
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            var delay = resolver.GetService(SystemGroupsService.Global).UtilityServiceCommand.delayService;
            var nextContext = new CameraEventServiceContext(catalog, delay);
            DisposeEvents();
            serviceContext = nextContext;
            RegisterEvents();
        }

        public void Dispatch(ICameraEventData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            RequireReady();
            if (!events.TryGetValue(data.GetType(), out var cameraEvent))
                throw new NotSupportedException($"No camera event registered for {data.GetType().Name}.");
            cameraEvent.Execute(data);
        }

        public void BindScene()
        {
            DisposeEvents();
            if (serviceContext != null) RegisterEvents();
            ready = true;
        }

        private void RequireReady()
        {
            if (!ready || serviceContext == null)
                throw new InvalidOperationException("Bind the camera scene and DelayService before dispatching effects.");
        }

        public void Shutdown()
        {
            DisposeEvents();
            ready = false;
            serviceContext = null;
        }

        private void RegisterEvents()
        {
            events[typeof(CameraZoomData)] = new CameraZoomEvent(serviceContext);
            events[typeof(CameraShakeData)] = new CameraShakeEvent(serviceContext);
        }

        private void DisposeEvents()
        {
            foreach (var cameraEvent in events.Values) cameraEvent.Dispose();
            events.Clear();
        }
    }
}
