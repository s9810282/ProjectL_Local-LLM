using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items
{
    public sealed class ItemAttackContext
    {
        public CharacterCombatFacts Holder { get; }
        public CellCoord AttackerAnchorAtDelivery { get; }
        public IReadOnlyList<ItemAttackCandidate> DirectHits { get; }
        public IReadOnlyList<CharacterCombatFacts> FinalCharacters { get; }
        public uint RandomState { get; }

        public ItemAttackContext(
            CharacterCombatFacts holder,
            CellCoord attackerAnchorAtDelivery,
            IReadOnlyList<ItemAttackCandidate> directHits,
            IReadOnlyList<CharacterCombatFacts> finalCharacters,
            uint randomState)
        {
            if (holder == null) throw new ArgumentNullException(nameof(holder));
            if (directHits == null) throw new ArgumentNullException(nameof(directHits));
            if (finalCharacters == null) throw new ArgumentNullException(nameof(finalCharacters));
            var hits = new ItemAttackCandidate[directHits.Count];
            for (int i = 0; i < hits.Length; i++)
                hits[i] = directHits[i] ?? throw new ArgumentException("피격 목록에 null을 넣을 수 없습니다.", nameof(directHits));
            var characters = new CharacterCombatFacts[finalCharacters.Count];
            var identities = new HashSet<EntityId>();
            for (int i = 0; i < characters.Length; i++)
            {
                characters[i] = finalCharacters[i] ?? throw new ArgumentException("최종 사실에 null을 넣을 수 없습니다.", nameof(finalCharacters));
                if (!identities.Add(characters[i].EntityId))
                    throw new ArgumentException("최종 사실의 개체 ID가 중복됩니다.", nameof(finalCharacters));
            }
            Holder = holder;
            AttackerAnchorAtDelivery = attackerAnchorAtDelivery;
            DirectHits = Array.AsReadOnly(hits);
            FinalCharacters = Array.AsReadOnly(characters);
            RandomState = randomState;
        }
    }
}
