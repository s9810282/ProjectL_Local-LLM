using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Run;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    public static class CharacterRecoveryCalculator
    {
        public static CharacterRecoveryDecision Plan(MainCharacterPersistentState main, CharacterInstanceId soulSourceId, int soulCount,
            RecoveryOptionSpec option, int balance)
        {
            if (main == null || option == null) throw new ArgumentNullException();
            if (soulCount < 0 || balance < 0) throw new ArgumentOutOfRangeException();
            if (main.CurrentHealth == 0) throw new InvalidOperationException("사망한 메인은 회복할 수 없습니다.");
            HealthStateSnapshot health = CharacterHealthRules.PersistentMainHealth(main, soulCount);
            var evaluation = RecoveryOptionEvaluator.Evaluate(option, health, balance, false, false);
            if (!evaluation.CanSelect)
                throw new InvalidOperationException("회복 효과가 없거나 비용이 부족합니다.");
            int healed = evaluation.ProjectedHealAmount;
            int maxHealthIncrease = evaluation.ProjectedMaxHealthIncrease;
            var changes = new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>(),
                new MainCharacterPersistentChange(soulSourceId, main,
                    main.With(currentHealth: checked(main.CurrentHealth + healed),
                        persistentMaxHealth: checked(main.PersistentMaxHealth + maxHealthIncrease))));
            return new CharacterRecoveryDecision(option, soulSourceId, soulCount, balance, balance - option.Cost, healed, changes, maxHealthIncrease);
        }
    }
}
