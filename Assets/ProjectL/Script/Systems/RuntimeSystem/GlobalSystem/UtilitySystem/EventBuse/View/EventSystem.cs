using ProjectL.Framework.Utility;
using System;
using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.EventBuse.Core;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.EventBuse.View
{
    #region 서비스

    public interface IEventSystemService : IServiceModule
    {
        public void SubscribeStage<TEvent>(Action<TEvent> callback)
        where TEvent : IStageEvent;

        public void PublishDomain<TEvent>(TEvent eventData)
        where TEvent : IDomainEvent;

        public void PublishPresentation<TEvent>(TEvent eventData)
            where TEvent : IPresentationEvent;
    }

    #endregion

    #region 시스템 모듈

    internal sealed class EventSystem : 
        IEventSystemService,
        ISystemShutdown,
        IExportSystemServices
    {
        private readonly StageEventBus _eventBus = new();
        private readonly EventSubscriptionScope _stageSubscriptions = new();

        public IStageEventSubscriber Subscriber => _eventBus;
        public IDomainEventPublisher DomainPublisher => _eventBus;
        public IPresentationEventPublisher PresentationPublisher => _eventBus;

        public void SubscribeStage<TEvent>(Action<TEvent> callback)
            where TEvent : IStageEvent
        {
            IDisposable subscription = _eventBus.Subscribe(callback);
            _stageSubscriptions.Add(subscription);
        }

        public void PublishDomain<TEvent>(TEvent eventData)
            where TEvent : IDomainEvent
        {
            DomainPublisher.Publish(eventData);
        }

        public void PublishPresentation<TEvent>(TEvent eventData)
            where TEvent : IPresentationEvent
        {
            PresentationPublisher.Publish(eventData);
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IEventSystemService>(this);
            sink.AddModule<ISystemModule>(this);
        }

        public void Shutdown()
        {
            _stageSubscriptions.Clear();
            _eventBus.Clear();
        }
    }

    #endregion
}
