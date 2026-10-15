using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Map;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat
{
    public sealed class AttackGeometryCalculator
    {

        public AttackCellSelection CalculateCells(TargetingMethodSpec method, int range, CellCoord origin, AttackAim aim)
        {
            if (new AttackSelectionResolver().TryResolve(method, range, origin, aim, out var cells, out var failure))
                return cells;
            throw new ArgumentException($"지정 의도가 올바르지 않습니다: {failure}", nameof(aim));
        }

        internal static AttackCellSelection CalculateResolvedCells(
            TargetingMethodSpec method, int range, CellCoord origin, AttackAim aim)
        {
            if (method.ChoiceMode == TargetingChoiceMode.Single)
            {
                CellCoord primary = aim.Cell.Value;
                CellCoord[] additional = AppointShapeCalculator.Translate(
                    AppointShapeCalculator.CreateSplashOffsets(method.Splash), primary);
                return new AttackCellSelection(primary, new[] { primary }, additional);
            }

            CellCoord[] offsets = method.ChoiceMode == TargetingChoiceMode.All
                ? AppointShapeCalculator.CreateBaseMask(method.Shape, range)
                : AppointShapeCalculator.CreateDirectionMask(method.Shape, range, aim.Direction);
            return new AttackCellSelection(null, AppointShapeCalculator.Translate(offsets, origin), Array.Empty<CellCoord>());
        }

        public AttackGeometryCalculator(AppointRangeCalculator rangeCalculator)
        {
            if (rangeCalculator == null) throw new ArgumentNullException(nameof(rangeCalculator));
        }
    }
}
