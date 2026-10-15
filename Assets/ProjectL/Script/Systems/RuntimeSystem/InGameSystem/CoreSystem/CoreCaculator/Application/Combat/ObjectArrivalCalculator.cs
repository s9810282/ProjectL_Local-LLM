using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>현재 활성 Object의 순서만 조회한다. 진입 실행·사망·게시를 소유하지 않는다.</summary>
    public static class ObjectArrivalCalculator
    {
        public static IReadOnlyList<ObjectSourceFact> OrderedAt(MovementEnvironmentFacts environment,
            EnvironmentSemanticBindings bindings, CellCoord cell)
        {
            var objects = environment.Tiles.OfType<ObjectSourceFact>().Where(o => o.Active && o.Cell == cell &&
                bindings.ObjectSourceIds.Contains(o.Id)).ToArray();
            if (objects.Select(o => o.EntryOrdinal).Distinct().Count() != objects.Length)
                throw new ArgumentException("같은 타일의 Object 진입 순서는 중복될 수 없습니다.");
            return Array.AsReadOnly(objects.OrderBy(o => o.EntryOrdinal).ToArray());
        }
    }
}
