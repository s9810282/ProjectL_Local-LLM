using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat
{
    public sealed class MeteorGimmickData
    {
        public int CycleControllerTurns { get; }
        public int WarningLeadControllerTurns { get; }
        public int MinimumMeteorCount { get; }
        public int MaximumMeteorCount { get; }
        public int FootprintSideLength { get; }
        public int MaximumHealthDamagePercent { get; }
        public int FireAffectedSideStarts { get; }
        public int FlameControllerTurns { get; }

        public MeteorGimmickData(int cycleControllerTurns, int warningLeadControllerTurns, int minimumMeteorCount,
            int maximumMeteorCount, int footprintSideLength, int maximumHealthDamagePercent,
            int fireAffectedSideStarts, int flameControllerTurns)
        {
            var validated = new MeteorGimmickSpec(cycleControllerTurns, warningLeadControllerTurns, minimumMeteorCount,
                maximumMeteorCount, footprintSideLength, maximumHealthDamagePercent, fireAffectedSideStarts, flameControllerTurns);
            CycleControllerTurns = validated.CycleControllerTurns;
            WarningLeadControllerTurns = validated.WarningLeadControllerTurns;
            MinimumMeteorCount = validated.MinimumMeteorCount;
            MaximumMeteorCount = validated.MaximumMeteorCount;
            FootprintSideLength = validated.FootprintSideLength;
            MaximumHealthDamagePercent = validated.MaximumHealthDamagePercent;
            FireAffectedSideStarts = validated.FireAffectedSideStarts;
            FlameControllerTurns = validated.FlameControllerTurns;
        }
    }
}
