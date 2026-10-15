using System;
using System.Collections.Generic;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.EventBuse.Core
{
    public sealed class StageEventBus :
        IStageEventSubscriber,
        IDomainEventPublisher,
        IPresentationEventPublisher
    {
        private sealed class Subscription : IDisposable
        {
            private Action _disposeAction;
            private bool _disposed;

            public Subscription(Action disposeAction)
            {
                _disposeAction = disposeAction;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                _disposeAction?.Invoke();
                _disposeAction = null;
            }
        }

        private readonly Dictionary<Type, List<Delegate>> _handlers = new();

        public IDisposable Subscribe<TEvent>(Action<TEvent> callback)
            where TEvent : IStageEvent
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            Type eventType = typeof(TEvent);

            if (!_handlers.TryGetValue(eventType, out List<Delegate> handlers))
            {
                handlers = new List<Delegate>();
                _handlers.Add(eventType, handlers);
            }

            handlers.Add(callback);

            return new Subscription(() => Unsubscribe(callback));
        }

        void IDomainEventPublisher.Publish<TEvent>(TEvent eventData)
        {
            PublishInternal(eventData);
        }

        void IPresentationEventPublisher.Publish<TEvent>(TEvent eventData)
        {
            PublishInternal(eventData);
        }

        private void PublishInternal<TEvent>(TEvent eventData)
            where TEvent : IStageEvent
        {
            Type eventType = typeof(TEvent);

            if (!_handlers.TryGetValue(eventType, out List<Delegate> handlers))
                return;

            Delegate[] snapshot = handlers.ToArray();

            foreach (Delegate handler in snapshot)
            {
                if (handler is Action<TEvent> action)
                    action.Invoke(eventData);
            }
        }

        private void Unsubscribe<TEvent>(Action<TEvent> callback)
            where TEvent : IStageEvent
        {
            Type eventType = typeof(TEvent);

            if (!_handlers.TryGetValue(eventType, out List<Delegate> handlers))
                return;

            handlers.Remove(callback);

            if (handlers.Count == 0)
                _handlers.Remove(eventType);
        }

        public void Clear()
        {
            _handlers.Clear();
        }
    }
}