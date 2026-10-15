using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects
{
    public readonly struct EffectOriginKey :
        IEquatable<EffectOriginKey>,
        IComparable<EffectOriginKey>
    {
        public EffectOwnerKind Kind { get; }
        public int OwnerInstanceId { get; }
        public int DefinitionKey { get; }
        public string RuleKey { get; }

        public EffectOriginKey(
            EffectOwnerKind kind,
            int ownerInstanceId,
            int definitionKey,
            string ruleKey)
        {
            if (kind == EffectOwnerKind.None || Enum.IsDefined(typeof(EffectOwnerKind), kind) == false)
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (ownerInstanceId < 0) throw new ArgumentOutOfRangeException(nameof(ownerInstanceId));
            if (definitionKey < 0) throw new ArgumentOutOfRangeException(nameof(definitionKey));
            if (string.IsNullOrWhiteSpace(ruleKey)) throw new ArgumentException(nameof(ruleKey));

            Kind = kind;
            OwnerInstanceId = ownerInstanceId;
            DefinitionKey = definitionKey;
            RuleKey = ruleKey;
        }

        public bool IsValid =>
            Kind != EffectOwnerKind.None &&
            Enum.IsDefined(typeof(EffectOwnerKind), Kind) &&
            OwnerInstanceId >= 0 &&
            DefinitionKey >= 0 &&
            string.IsNullOrWhiteSpace(RuleKey) == false;

        public bool Equals(EffectOriginKey other)
        {
            return Kind == other.Kind &&
                   OwnerInstanceId == other.OwnerInstanceId &&
                   DefinitionKey == other.DefinitionKey &&
                   string.Equals(RuleKey, other.RuleKey, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is EffectOriginKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Kind;
                hashCode = (hashCode * 397) ^ OwnerInstanceId;
                hashCode = (hashCode * 397) ^ DefinitionKey;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(RuleKey ?? string.Empty);
                return hashCode;
            }
        }

        public int CompareTo(EffectOriginKey other)
        {
            int comparison = Kind.CompareTo(other.Kind);
            if (comparison != 0) return comparison;

            comparison = OwnerInstanceId.CompareTo(other.OwnerInstanceId);
            if (comparison != 0) return comparison;

            comparison = DefinitionKey.CompareTo(other.DefinitionKey);
            if (comparison != 0) return comparison;

            return string.Compare(RuleKey, other.RuleKey, StringComparison.Ordinal);
        }

        public static bool operator ==(EffectOriginKey left, EffectOriginKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EffectOriginKey left, EffectOriginKey right)
        {
            return left.Equals(right) == false;
        }
    }
}
