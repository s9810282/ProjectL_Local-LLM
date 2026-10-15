using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>회복 계산의 불변 입력과 원자적으로 게시할 결과다.</summary>
    public sealed class CharacterRecoveryDecision
    {
        public RecoveryOptionSpec Option { get; }
        /// <summary>최대 체력 계산에 영혼 수를 사용한 보유 캐릭터다. Buffer는 이 캐릭터의 현재 영혼 수와 <see cref="SoulCount"/>만 비교한다.</summary>
        public CharacterInstanceId SoulSourceId { get; }
        public int SoulCount { get; }
        public int ExpectedBalance { get; }
        public int FinalBalance { get; }
        public int HealedAmount { get; }
        public CharacterChanges Characters { get; }

        public CharacterRecoveryDecision(RecoveryOptionSpec option, CharacterInstanceId soulSourceId, int soulCount, int expectedBalance,
            int finalBalance, int healedAmount, CharacterChanges characters)
        {
            Option = option ?? throw new ArgumentNullException(nameof(option));
            Characters = characters ?? throw new ArgumentNullException(nameof(characters));
            if (soulSourceId.IsEmpty) throw new ArgumentException("영혼 출처 보유 ID가 필요합니다.", nameof(soulSourceId));
            SoulSourceId = soulSourceId;
            if (soulCount < 0 || finalBalance < 0 || expectedBalance - (long)option.Cost != finalBalance ||
                healedAmount <= 0 || characters.Main == null || characters.Characters.Count != 0 ||
                characters.Souls.Count != 0 ||
                characters.Main.Final.CurrentHealth - (long)characters.Main.Expected.CurrentHealth != healedAmount)
                throw new ArgumentException("회복 제안의 비용·체력·변경 범위가 일치해야 합니다.");
            var before = characters.Main.Expected;
            var after = characters.Main.Final;
            if (before.CurrentHealth <= 0 || after.CurrentHealth > (long)before.PersistentMaxHealth + soulCount ||
                before.PersistentMaxHealth != after.PersistentMaxHealth || before.PersistentAttack != after.PersistentAttack ||
                before.MaxActionCost != after.MaxActionCost || before.MoveRange != after.MoveRange || before.MoveCost != after.MoveCost)
                throw new ArgumentException("회복은 살아 있는 메인의 체력만 바꿀 수 있습니다.");
            SoulCount = soulCount;
            ExpectedBalance = expectedBalance;
            FinalBalance = finalBalance;
            HealedAmount = healedAmount;
        }
    }
}
