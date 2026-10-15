using System;
using System.Collections.Generic;

namespace ProjectL.Framework.Utility
{
    public sealed class EventSubscriptionScope
    {
        private readonly List<IDisposable> _subscriptions = new();

        public void Add(IDisposable subscription)
        {
            if (subscription == null)
                return;

            _subscriptions.Add(subscription);
        }

        public void Clear()
        {
            for (int i = _subscriptions.Count - 1; i >= 0; i--)
            {
                _subscriptions[i]?.Dispose();
            }

            _subscriptions.Clear();
        }
    }
}