using System;
using System.Collections.Generic;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data
{
    public readonly struct InputLayer :
        IEquatable<InputLayer>
    {
        private static readonly IReadOnlyList<string>
            EmptyActionMaps =
                Array.AsReadOnly(Array.Empty<string>());

        private readonly IReadOnlyList<string> _actionMaps;

        public string Name { get; }

        public IReadOnlyList<string> ActionMaps =>
            _actionMaps ?? EmptyActionMaps;

        public bool IsValid =>
            string.IsNullOrWhiteSpace(Name) == false;

        public InputLayer(
            string name,
            params string[] actionMaps)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(
                    "Input Layer name is required.",
                    nameof(name));
            }

            if (actionMaps == null)
            {
                throw new ArgumentNullException(
                    nameof(actionMaps));
            }

            if (actionMaps.Length == 0)
            {
                throw new ArgumentException(
                    "At least one Action Map is required.",
                    nameof(actionMaps));
            }

            string[] copiedMaps =
                new string[actionMaps.Length];

            for (int i = 0; i < actionMaps.Length; i++)
            {
                string actionMap = actionMaps[i];

                if (string.IsNullOrWhiteSpace(actionMap))
                {
                    throw new ArgumentException(
                        "Action Map name is required.",
                        nameof(actionMaps));
                }

                for (int previous = 0;
                     previous < i;
                     previous++)
                {
                    if (string.Equals(
                            copiedMaps[previous],
                            actionMap,
                            StringComparison.Ordinal))
                    {
                        throw new ArgumentException(
                            $"Action Map is duplicated: {actionMap}",
                            nameof(actionMaps));
                    }
                }

                copiedMaps[i] = actionMap;
            }

            Name = name;
            _actionMaps =
                Array.AsReadOnly(copiedMaps);
        }

        public bool Allows(string actionMap)
        {
            if (IsValid == false ||
                string.IsNullOrWhiteSpace(actionMap) ||
                _actionMaps == null)
            {
                return false;
            }

            for (int i = 0; i < _actionMaps.Count; i++)
            {
                if (string.Equals(
                        _actionMaps[i],
                        actionMap,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public bool Equals(InputLayer other)
        {
            return string.Equals(
                Name,
                other.Name,
                StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is InputLayer other &&
                   Equals(other);
        }

        public override int GetHashCode()
        {
            return Name == null
                ? 0
                : StringComparer.Ordinal.GetHashCode(Name);
        }

        public override string ToString()
        {
            return Name ?? string.Empty;
        }

        public static bool operator ==(
            InputLayer left,
            InputLayer right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            InputLayer left,
            InputLayer right)
        {
            return left.Equals(right) == false;
        }
    }
}