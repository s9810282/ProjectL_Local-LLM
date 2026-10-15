using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Configuration;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Core;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View
{
    #region Public Contracts

    public interface IInputController
    {
        string ActionMap { get; }

        void Configure(InputBindings input);
    }

    #endregion

    public sealed class InputBindings
    {
        private readonly string _controllerName;
        private readonly string _actionMap;
        private readonly InputRouteTable _routeTable;

        private readonly List<
            InputBindingDescriptor> _descriptors;

        private bool _isSealed;
        private int _bindingCount;

        #region Binding API

        public void Press(
            string actionName,
            Action handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(
                    nameof(handler));
            }

            Raw(
                actionName,
                InputValueKind.Button,
                snapshot =>
                {
                    if (snapshot.Phase ==
                        InputPhase.Performed)
                    {
                        handler();
                    }
                });
        }

        public void Button(
            string actionName,
            Action<bool> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(
                    nameof(handler));
            }

            Raw(
                actionName,
                InputValueKind.Button,
                snapshot =>
                {
                    if (snapshot.Phase ==
                        InputPhase.Performed)
                    {
                        handler(
                            snapshot.Value.ButtonValue);
                    }
                    else if (snapshot.Phase ==
                             InputPhase.Canceled)
                    {
                        handler(false);
                    }
                });
        }

        public void Float(
            string actionName,
            Action<float> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(
                    nameof(handler));
            }

            Raw(
                actionName,
                InputValueKind.Float,
                snapshot =>
                {
                    if (snapshot.Phase ==
                            InputPhase.Performed ||
                        snapshot.Phase ==
                            InputPhase.Canceled)
                    {
                        handler(
                            snapshot.Value.FloatValue);
                    }
                });
        }

        public void Vector2(
            string actionName,
            Action<float, float> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(
                    nameof(handler));
            }

            Raw(
                actionName,
                InputValueKind.Vector2,
                snapshot =>
                {
                    if (snapshot.Phase !=
                            InputPhase.Performed &&
                        snapshot.Phase !=
                            InputPhase.Canceled)
                    {
                        return;
                    }

                    handler(
                        snapshot.Value.X,
                        snapshot.Value.Y);
                });
        }

        public void Raw(
            string actionName,
            InputValueKind valueKind,
            Action<InputSnapshot> handler)
        {
            EnsureOpen();

            if (handler == null)
            {
                throw new ArgumentNullException(
                    nameof(handler));
            }

            InputBindingDescriptor descriptor =
                new InputBindingDescriptor(
                    _controllerName,
                    _actionMap,
                    actionName,
                    valueKind);

            _routeTable.Add(
                descriptor,
                handler);

            _descriptors.Add(descriptor);
            _bindingCount++;
        }

        #endregion

        #region Internal

        internal int BindingCount =>
            _bindingCount;

        internal InputBindings(
            string controllerName,
            string actionMap,
            InputRouteTable routeTable,
            List<InputBindingDescriptor> descriptors)
        {
            if (string.IsNullOrWhiteSpace(
                    controllerName))
            {
                throw new ArgumentException(
                    "Input Controller name is required.",
                    nameof(controllerName));
            }

            if (string.IsNullOrWhiteSpace(
                    actionMap))
            {
                throw new ArgumentException(
                    "Action Map name is required.",
                    nameof(actionMap));
            }

            _controllerName = controllerName;
            _actionMap = actionMap;

            _routeTable =
                routeTable
                ?? throw new ArgumentNullException(
                    nameof(routeTable));

            _descriptors =
                descriptors
                ?? throw new ArgumentNullException(
                    nameof(descriptors));
        }

        internal void Seal()
        {
            _isSealed = true;
        }

        private void EnsureOpen()
        {
            if (_isSealed)
            {
                throw new InvalidOperationException(
                    "Bindings can only be added while " +
                    "configuring an Input Controller.");
            }
        }

        #endregion
    }

    public abstract class InputController :
        IInputController
    {
        #region Public API

        public abstract string ActionMap { get; }

        #endregion

        #region Extension Point

        protected abstract void Configure(
            InputBindings input);

        #endregion

        #region Internal

        void IInputController.Configure(
            InputBindings input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(
                    nameof(input));
            }

            Configure(input);
        }

        #endregion
    }
}