using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using UnityEngine;

namespace ProjectL.Systems
{
    [CreateAssetMenu(fileName = "UnitCardVisuals", menuName = "ProjectL/UI/Unit Card Visuals")]
    public sealed class UnitCardVisualsSO : ScriptableObject
    {
        [Serializable]
        public sealed class CharacterVisual
        {
            public EntityKey Character;
            public string DisplayName;
            public Sprite Portrait;
            public Sprite SynergyIcon;
            [TextArea] public string SynergyDescription;
            public Sprite FunctionIcon;
            [TextArea] public string FunctionDescription;
        }

        [SerializeField] private CharacterVisual[] characters = Array.Empty<CharacterVisual>();

        public CharacterVisual GetVisual(EntityKey key)
        {
            if (key == EntityKey.None || characters == null) return null;
            foreach (var entry in characters)
                if (entry != null && entry.Character == key) return entry;
            return null;
        }
    }
}
