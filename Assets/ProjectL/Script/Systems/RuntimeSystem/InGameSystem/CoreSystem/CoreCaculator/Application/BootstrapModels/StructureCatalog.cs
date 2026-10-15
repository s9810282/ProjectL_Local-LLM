using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Tiles;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels
{
    internal sealed class StructureCatalog
    {
        private readonly Dictionary<EntityKey, StructureDefinition> _definitions;

        internal StructureCatalog(IReadOnlyList<StructureSpec> specs)
        {
            if (specs == null)
                throw new ArgumentNullException(nameof(specs));

            _definitions = new Dictionary<EntityKey, StructureDefinition>(specs.Count);

            for (int i = 0; i < specs.Count; i++)
            {
                StructureSpec spec = specs[i];
                if (spec == null)
                    throw new ArgumentException("spec는 null일 수 없습니다.", nameof(specs));
                if (_definitions.ContainsKey(spec.Key))
                    throw new ArgumentException($"중복된 키입니다 : {spec.Key}", nameof(specs));

                TileFootprint footprint = new TileFootprint(
                    spec.FootprintWidth,
                    spec.FootprintHeight,
                    spec.FootprintLocalCells.ToArray());

                StructureDefinition definition = new StructureDefinition(
                    spec.Key,
                    footprint,
                    spec.MaxHealth,
                    spec.IsAttackable,
                    spec.AttackPower);

                _definitions.Add(spec.Key, definition);
            }
        }

        internal StructureDefinition GetRequired(EntityKey key)
        {
            if (key == EntityKey.None || Enum.IsDefined(typeof(EntityKey), key) == false)
                throw new ArgumentOutOfRangeException(nameof(key));

            if (_definitions.TryGetValue(key, out StructureDefinition definition) == false)
                throw new ArgumentException(nameof(key));

            return definition;
        }
    }
}
