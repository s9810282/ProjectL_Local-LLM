using System;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.EventBuse.Core
{

    // Marker interface for stage events
    public interface IStageEvent { }

    public interface IDomainEvent : IStageEvent { }

    public interface IPresentationEvent : IStageEvent { }

    // Marker interface for event subscribers
    public interface IStageEventSubscriber
    {
        IDisposable Subscribe<TEvent>(Action<TEvent> callback)
            where TEvent : IStageEvent;
    }

    public interface IDomainEventPublisher
    {
        void Publish<TEvent>(TEvent eventData)
            where TEvent : IDomainEvent;
    }

    public interface IPresentationEventPublisher
    {
        void Publish<TEvent>(TEvent eventData)
            where TEvent : IPresentationEvent;
    }
}