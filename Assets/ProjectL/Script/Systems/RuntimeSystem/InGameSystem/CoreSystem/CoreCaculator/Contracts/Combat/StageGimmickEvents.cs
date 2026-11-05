using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public enum RockfallGimmickStep
    {
        Warning,
        Impact
    }

    public enum HaboobGimmickStep
    {
        Warning,
        Relocation
    }

    public sealed class CombatHaboobEvent : CombatEvent
    {
        public HaboobGimmickStep Step { get; }
        public HaboobGimmickState Expected { get; }
        public HaboobGimmickState Final { get; }

        public CombatHaboobEvent(HaboobGimmickStep step, HaboobGimmickState expected, HaboobGimmickState final)
            : base(CombatEventKind.HaboobAdvanced)
        {
            Expected = expected ?? throw new ArgumentNullException(nameof(expected));
            Final = final ?? throw new ArgumentNullException(nameof(final));
            if (!Enum.IsDefined(typeof(HaboobGimmickStep), step) || expected.Id != final.Id || final.NextBoundaryCount <= expected.NextBoundaryCount)
                throw new ArgumentException("하부브 사건의 식별자·기한이 잘못되었습니다.");
            if (step == HaboobGimmickStep.Warning ? expected.Phase != HaboobGimmickPhase.Active ||
                final.Phase != HaboobGimmickPhase.RelocationWarning || !expected.Cells.SequenceEqual(final.Cells) :
                expected.Phase != HaboobGimmickPhase.RelocationWarning || !SameFootprint(expected.Cells, final.Cells))
                throw new ArgumentException("하부브 사건의 전후 단계·영역이 다릅니다.");
            Step = step;
        }

        private static bool SameFootprint(IReadOnlyList<Common.CellCoord> a, IReadOnlyList<Common.CellCoord> b)
        {
            int ax = a.Min(c => c.X), ay = a.Min(c => c.Y), bx = b.Min(c => c.X), by = b.Min(c => c.Y);
            return a.Select(c => new Common.CellCoord(c.X - ax, c.Y - ay)).SequenceEqual(
                b.Select(c => new Common.CellCoord(c.X - bx, c.Y - by)));
        }
    }

    public sealed class CombatRockfallEvent : CombatEvent
    {
        public RockfallGimmickStep Step { get; }
        public RockfallGimmickState Expected { get; }
        public RockfallGimmickState Final { get; }
        public IReadOnlyList<EntityId> RecipientIds { get; }

        public CombatRockfallEvent(RockfallGimmickStep step, RockfallGimmickState expected,
            RockfallGimmickState final, IReadOnlyList<EntityId> recipientIds) : base(CombatEventKind.RockfallAdvanced)
        {
            Expected = expected ?? throw new ArgumentNullException(nameof(expected));
            var ids = (recipientIds ?? throw new ArgumentNullException(nameof(recipientIds))).ToArray();
            if (!Enum.IsDefined(typeof(RockfallGimmickStep), step) || ids.Any(id => id.IsEmpty) || ids.Distinct().Count() != ids.Length ||
                final != null && (final.Id != expected.Id || final.NextBoundaryCount <= expected.NextBoundaryCount))
                throw new ArgumentException("낙석 사건의 식별자·기한·수신자가 잘못되었습니다.");
            if (step == RockfallGimmickStep.Warning ? expected.Phase != RockfallGimmickPhase.Cooldown ||
                final == null || final.Phase != RockfallGimmickPhase.Warning || ids.Length != 0 :
                expected.Phase != RockfallGimmickPhase.Warning || final != null && final.Phase != RockfallGimmickPhase.Cooldown)
                throw new ArgumentException("낙석 사건의 전후 단계가 다릅니다.");
            Step = step;
            Final = final;
            RecipientIds = Array.AsReadOnly(ids.OrderBy(id => id).ToArray());
        }
    }
}
