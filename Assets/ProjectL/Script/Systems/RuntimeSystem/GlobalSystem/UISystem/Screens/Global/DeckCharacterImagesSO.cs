using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using UnityEngine;

namespace ProjectL.Systems
{
    [CreateAssetMenu(fileName = "DeckCharacterImages", menuName = "ProjectL/UI/Deck Character Images")]
    public sealed class DeckCharacterImagesSO : ScriptableObject
    {
        [Serializable]
        private struct CharacterImage
        {
            public EntityKey Character;
            public Sprite Image;
        }

        [SerializeField] private CharacterImage[] images = Array.Empty<CharacterImage>();

        public Sprite GetSprite(EntityKey character)
        {
            if (character == EntityKey.None || images == null) return null;
            foreach (var entry in images)
                if (entry.Character == character) return entry.Image;
            return null;
        }
    }
}
