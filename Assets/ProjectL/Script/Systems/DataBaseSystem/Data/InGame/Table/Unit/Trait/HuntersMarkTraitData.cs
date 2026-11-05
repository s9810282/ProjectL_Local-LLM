using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    /// <summary>유닛 정의에서 내보낸 불변 표식 설정입니다.</summary>
    public sealed class HuntersMarkTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.HuntersMark;
        public int BonusDamagePercent { get; }

        public int DurationAffectedSideTurnEnds { get; }

        public HuntersMarkTraitData(int bonusDamagePercent, int durationAffectedSideTurnEnds)
        {
            BonusDamagePercent = bonusDamagePercent;
            DurationAffectedSideTurnEnds = durationAffectedSideTurnEnds;
        }

        public override void Validate()
        {
            if (BonusDamagePercent < 0)
                throw new InvalidOperationException("약점 노출의 피해 증가율은 음수일 수 없습니다.");
            if (DurationAffectedSideTurnEnds < 1)
                throw new InvalidOperationException("약점 노출의 대상 진영 종료 횟수는 양수여야 합니다.");
        }
    }
}
