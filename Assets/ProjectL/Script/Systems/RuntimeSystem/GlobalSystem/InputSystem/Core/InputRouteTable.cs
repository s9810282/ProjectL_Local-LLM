using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Configuration;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Core
{
    internal sealed class InputRouteTable
    {
        private readonly Dictionary<
            RouteKey,
            RouteEntry> _routes =
                new Dictionary<RouteKey, RouteEntry>();

        private bool _isSealed;

        #region Configuration

        internal InputRouteTable()
        {
        }

        internal void Add(
            InputBindingDescriptor descriptor,
            Action<InputSnapshot> handler)
        {
            if (_isSealed)
            {
                throw new InvalidOperationException(
                    "Input Route Table is already sealed.");
            }

            if (descriptor.IsValid == false)
            {
                throw new ArgumentException(
                    "Input binding descriptor is invalid.",
                    nameof(descriptor));
            }

            if (handler == null)
            {
                throw new ArgumentNullException(
                    nameof(handler));
            }

            RouteKey key =
                new RouteKey(
                    descriptor.ActionMap,
                    descriptor.ActionName);

            if (_routes.TryGetValue(
                    key,
                    out RouteEntry existingEntry))
            {
                throw new InvalidOperationException(
                    "Input binding is already registered: " +
                    $"{descriptor.ActionMap}." +
                    $"{descriptor.ActionName}. " +
                    "ExistingController=" +
                    $"{existingEntry.Descriptor.ControllerName}, " +
                    "NewController=" +
                    $"{descriptor.ControllerName}");
            }

            _routes.Add(
                key,
                new RouteEntry(
                    descriptor,
                    handler));
        }

        internal void Seal()
        {
            if (_isSealed)
            {
                throw new InvalidOperationException(
                    "Input Route Table is already sealed.");
            }

            _isSealed = true;
        }

        #endregion

        #region Dispatch

        internal bool TryDispatch(
            string actionMap,
            string actionName,
            InputSnapshot snapshot)
        {
            RouteKey key =
                new RouteKey(
                    actionMap,
                    actionName);

            if (_routes.TryGetValue(
                    key,
                    out RouteEntry entry) == false)
            {
                return false;
            }

            InputBindingDescriptor descriptor =
                entry.Descriptor;

            if (descriptor.ValueKind !=
                snapshot.Value.Kind)
            {
                throw new InvalidOperationException(
                    "Input value kind mismatch: " +
                    $"{actionMap}.{actionName}. " +
                    $"Expected={descriptor.ValueKind}, " +
                    $"Actual={snapshot.Value.Kind}, " +
                    $"Controller=" +
                    $"{descriptor.ControllerName}");
            }

            entry.Invoke(snapshot);
            return true;
        }

        #endregion

        #region Internal Types

        private readonly struct RouteKey :
            IEquatable<RouteKey>
        {
            private readonly string _actionMap;
            private readonly string _actionName;

            internal RouteKey(
                string actionMap,
                string actionName)
            {
                _actionMap = actionMap;
                _actionName = actionName;
            }

            public bool Equals(RouteKey other)
            {
                return
                    string.Equals(
                        _actionMap,
                        other._actionMap,
                        StringComparison.Ordinal) &&
                    string.Equals(
                        _actionName,
                        other._actionName,
                        StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is RouteKey other &&
                       Equals(other);
            }

            public override int GetHashCode()
            {
                int mapHash =
                    _actionMap == null
                        ? 0
                        : StringComparer.Ordinal.GetHashCode(
                            _actionMap);

                int actionHash =
                    _actionName == null
                        ? 0
                        : StringComparer.Ordinal.GetHashCode(
                            _actionName);

                unchecked
                {
                    return
                        (mapHash * 397) ^
                        actionHash;
                }
            }
        }

        private readonly struct RouteEntry
        {
            private readonly Action<
                InputSnapshot> _handler;

            internal InputBindingDescriptor
                Descriptor
            { get; }

            internal RouteEntry(
                InputBindingDescriptor descriptor,
                Action<InputSnapshot> handler)
            {
                Descriptor = descriptor;
                _handler = handler;
            }

            internal void Invoke(
                InputSnapshot snapshot)
            {
                _handler(snapshot);
            }
        }

        #endregion
    }
}