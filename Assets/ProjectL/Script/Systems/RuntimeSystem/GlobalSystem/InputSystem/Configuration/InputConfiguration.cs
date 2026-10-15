using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Core;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Configuration
{
    public sealed class InputConfiguration
    {
        private readonly IReadOnlyList<
            InputBindingDescriptor> _bindings;

        #region Public API

        public IReadOnlyList<InputBindingDescriptor>
            Bindings => _bindings;

        public static InputConfiguration Create(
            params IInputController[] controllers)
        {
            if (controllers == null)
            {
                throw new ArgumentNullException(
                    nameof(controllers));
            }

            if (controllers.Length == 0)
            {
                throw new ArgumentException(
                    "At least one Input Controller " +
                    "is required.",
                    nameof(controllers));
            }

            InputRouteTable routeTable =
                new InputRouteTable();

            List<InputBindingDescriptor> descriptors =
                new List<InputBindingDescriptor>();

            for (int i = 0;
                 i < controllers.Length;
                 i++)
            {
                IInputController controller =
                    controllers[i];

                if (controller == null)
                {
                    throw new ArgumentException(
                        "Input Controller cannot be null. " +
                        $"Index={i}",
                        nameof(controllers));
                }

                string actionMap =
                    controller.ActionMap;

                if (string.IsNullOrWhiteSpace(
                        actionMap))
                {
                    throw new ArgumentException(
                        "Controller Action Map is required. " +
                        $"Controller=" +
                        $"{controller.GetType().FullName}, " +
                        $"Index={i}",
                        nameof(controllers));
                }

                string controllerName =
                    controller.GetType().FullName
                    ?? controller.GetType().Name;

                InputBindings input =
                    new InputBindings(
                        controllerName,
                        actionMap,
                        routeTable,
                        descriptors);

                try
                {
                    controller.Configure(input);

                    if (input.BindingCount == 0)
                    {
                        throw new InvalidOperationException(
                            "Input Controller registered " +
                            "no bindings. " +
                            $"Controller={controllerName}, " +
                            $"ActionMap={actionMap}");
                    }
                }
                finally
                {
                    input.Seal();
                }
            }

            routeTable.Seal();

            return new InputConfiguration(
                routeTable,
                descriptors.ToArray());
        }

        #endregion

        #region Internal

        internal InputRouteTable RouteTable { get; }

        private InputConfiguration(
            InputRouteTable routeTable,
            InputBindingDescriptor[] bindings)
        {
            RouteTable =
                routeTable
                ?? throw new ArgumentNullException(
                    nameof(routeTable));

            if (bindings == null)
            {
                throw new ArgumentNullException(
                    nameof(bindings));
            }

            _bindings =
                Array.AsReadOnly(bindings);
        }

        #endregion
    }
}