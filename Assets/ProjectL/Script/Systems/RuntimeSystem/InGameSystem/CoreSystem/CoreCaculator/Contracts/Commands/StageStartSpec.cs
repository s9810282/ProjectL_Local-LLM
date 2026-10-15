using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    public sealed class StageStartSpec
    {
        public StageKey StageKey { get; }
        public StageConfig Config { get; }
        public IReadOnlyList<UnitSpec> Units { get; }
        public IReadOnlyList<StructureSpec> Structures { get; }
        public CategoryMatchupSpec CategoryMatchups { get; }

        public StageStartSpec(
            StageKey stageKey,
            StageConfig config,
            IReadOnlyList<UnitSpec> units,
            IReadOnlyList<StructureSpec> structures,
            CategoryMatchupSpec categoryMatchups)
        {
            if (stageKey.IsEmpty)
                throw new ArgumentException("StageKey는 비어 있을 수 없습니다.", nameof(stageKey));
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (units == null)
                throw new ArgumentNullException(nameof(units));
            if (structures == null)
                throw new ArgumentNullException(nameof(structures));
            if (categoryMatchups == null)
                throw new ArgumentNullException(nameof(categoryMatchups));
            if (units.Count == 0)
                throw new ArgumentException("Units는 비어 있을 수 없습니다.", nameof(units));

            UnitSpec[] copiedUnits = new UnitSpec[units.Count];
            for (int i = 0; i < units.Count; i++)
            {
                UnitSpec unit = units[i];
                if (unit == null)
                    throw new ArgumentException("Units에는 null 항목을 넣을 수 없습니다.", nameof(units));

                copiedUnits[i] = unit;
            }

            StructureSpec[] copiedStructures = new StructureSpec[structures.Count];
            for (int i = 0; i < structures.Count; i++)
            {
                StructureSpec structure = structures[i];
                if (structure == null)
                    throw new ArgumentException("Structures에는 null 항목을 넣을 수 없습니다.", nameof(structures));

                copiedStructures[i] = structure;
            }

            StageKey = stageKey;
            Config = config;
            Units = Array.AsReadOnly(copiedUnits);
            Structures = Array.AsReadOnly(copiedStructures);
            CategoryMatchups = categoryMatchups;
        }
    }
}
