using System;
using System.Collections.Generic;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items
{
    /// <summary>장착 슬롯과 대상에게 적용된 아이템 효과를 보관합니다. 재적용은 기존 값을 유지합니다.</summary>
    public sealed class CharacterItemState
    {
        public static CharacterItemState Empty { get; } = new CharacterItemState(null, Array.Empty<ItemTemporaryEffect>());
        public int? EquippedItemKey { get; }
        public IReadOnlyList<ItemTemporaryEffect> Effects { get; }

        public CharacterItemState(int? equippedItemKey, IReadOnlyList<ItemTemporaryEffect> effects)
        {
            if (equippedItemKey <= 0) throw new ArgumentOutOfRangeException(nameof(equippedItemKey));
            if (effects == null) throw new ArgumentNullException(nameof(effects));
            var copy = new List<ItemTemporaryEffect>();
            var seen = new HashSet<(int, ItemStatKind)>();
            foreach (var effect in effects)
            {
                if (effect == null) throw new ArgumentException("아이템 효과에 null을 넣을 수 없습니다.", nameof(effects));
                if (seen.Add((effect.ItemKey, effect.Stat))) copy.Add(effect);
            }
            EquippedItemKey = equippedItemKey;
            Effects = copy.AsReadOnly();
        }

        public bool SameValues(CharacterItemState other)
        {
            if (other == null || EquippedItemKey != other.EquippedItemKey || Effects.Count != other.Effects.Count)
                return false;
            foreach (var effect in Effects)
            {
                bool found = false;
                foreach (var candidate in other.Effects)
                {
                    if (effect.ItemKey == candidate.ItemKey && effect.Stat == candidate.Stat &&
                        effect.Magnitude == candidate.Magnitude)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) return false;
            }
            return true;
        }
    }
}
