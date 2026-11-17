using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>기존 효과 엔진으로 승리 회복을 계산한다. 중복 게시 판단은 Buffer가 담당한다.</summary>
    public sealed class BattleCompletionCalculator
    {
        private readonly CompiledArtifactLoadout _loadout;
        private readonly CombatEffectEngine _effects = new();

        public BattleCompletionCalculator(ArtifactLoadoutSnapshot loadout) =>
            _loadout = ArtifactEffectCompiler.Compile(loadout);

        public BattleCompletionChanges Complete(CharacterCombatFacts main, MainCharacterPersistentState persistent,
            StageOutcomeSnapshot outcome, ChapterProgressionResult progression)
            => new(PlanTerminalHealth(main, persistent, outcome), outcome, progression);

        /// <summary>공격 정산에서도 사망 영혼 생성 전에 같은 종료 회복을 한 번 계산한다.</summary>
        internal CharacterChanges PlanTerminalHealth(CharacterCombatFacts main, MainCharacterPersistentState persistent,
            StageOutcomeSnapshot outcome, List<CombatEvent> events = null, List<CombatHealthExecution> executions = null,
            List<CombatHealthClaim> claims = null)
        {
            CharacterSettlementCalculator.ValidateMain(main, persistent);
            int health = persistent.CurrentHealth;
            CombatArtifactEvent activation = null;
            if (outcome.Outcome == StageOutcome.Victory)
            {
                if (main.Health.IsDead) throw new InvalidOperationException("메인 사망은 승리보다 우선합니다.");
                var evaluated = _effects.EvaluateStageCleared(_loadout.CaptureBindings(),
                    new StageClearedEffectContext(main.EntityId, health, main.Health.MaxHealth));
                health = evaluated.FinalHealth;
                if (evaluated.Contributions.Count != 0 && events != null)
                {
                    activation = new CombatArtifactEvent(ArtifactActivationCalculator.FromHealing(evaluated), CombatArtifactPurpose.Healing);
                    events.Add(activation);
                }
            }
            var healed = main.WithHealth(new HealthStateSnapshot(health, main.Health.MaxHealth));
            if (events != null)
            {
                var evidence = CombatCommandEvents.Health(events, main, healed);
                if (activation != null && executions != null && claims != null)
                    RecordArtifactHealing(main, activation, evidence, executions, claims);
            }
            // 임시·위치 최대 체력 기여분은 전투와 함께 끝나고 영혼은 Run 동안 유지된다.
            // 전투 중 위치 기여분이 사라질 때와 같은 규칙(min(현재, 새 최대))을 종료 경계에 적용한다.
            health = Math.Min(health, BattleEndMaxHealth(main, persistent));
            var settled = CharacterSettlementCalculator.SetHealth(main, persistent, health);
            var before = settled.Characters[0].Final;
            var cleared = HeldEnvironmentEffects.ClearForBattleEnd(before);
            if (!ReferenceEquals(before, cleared)) events?.Add(new CombatHeldEnvironmentEvent(before, cleared));
            cleared = CombatTemporaryStateCleanup.Clear(cleared);
            if (events != null) PositionTileModifierCalculator.RecordNormalization(events, healed, cleared, executions);
            return new CharacterChanges(new[] { new CharacterStateChange(main, cleared) }, settled.Souls, settled.Main);
        }

        private static void RecordArtifactHealing(CharacterCombatFacts main, CombatArtifactEvent activation,
            CombatCharacterEvent evidence, List<CombatHealthExecution> executions, List<CombatHealthClaim> claims)
        {
            var before = main.Health;
            var steps = new List<CombatHealthStep>();
            for (int i = 0; i < activation.Activations.Count; i++)
            {
                // 기존 승리 정산이 배분한 실제 회복량을 적용 순서 그대로 사용합니다.
                var contribution = activation.Activations[i];
                int recovery = checked((int)contribution.AppliedAmount);
                var claimSteps = Array.Empty<CombatHealthStep>();
                if (recovery > 0)
                {
                    var after = new HealthStateSnapshot(checked(before.CurrentHealth + recovery), before.MaxHealth);
                    var source = new CombatHealthSource(CombatHealthOperationKind.EffectRecovery, contribution.Origin, contextEvent: activation);
                    var step = new CombatHealthStep(main.EntityId, before, after, 0, recovery, 0, source, evidence, i);
                    steps.Add(step);
                    claimSteps = new[] { step };
                    before = after;
                }
                claims.Add(new CombatHealthClaim(CombatHealthClaimKind.ArtifactRecovery, activation, i, -1, main.EntityId, claimSteps));
            }
            if (steps.Count != 0) executions.Add(new CombatHealthExecution(steps, new[] { evidence }));
        }

        /// <summary>전투가 끝난 뒤 남는 메인 최대 체력이다(지속 최대 체력 + Run 동안 유지되는 영혼 기여분).</summary>
        internal static int BattleEndMaxHealth(CharacterCombatFacts main, MainCharacterPersistentState persistent) =>
            checked(persistent.PersistentMaxHealth + main.Stats.SoulMaxHealthBonus);

        public BattleCompletionChanges Abandon(CharacterCombatFacts main, MainCharacterPersistentState persistent,
            ChapterProgressionResult progression) => Complete(main, persistent,
                new StageOutcomeSnapshot(StageOutcome.Defeat, StageOutcomeReason.VoluntaryAbandonment), progression);

        /// <summary>미게시 정산의 최종 메인 값으로 성배를 계산하되 예상 값은 명령 이전 원본을 유지한다.</summary>
        public BattleCompletionChanges Complete(CharacterCombatFacts main, MainCharacterPersistentState persistent,
            StageOutcomeSnapshot outcome, ChapterProgressionResult progression, SoulActionChanges settled)
        {
            if (settled == null) throw new ArgumentNullException(nameof(settled));
            var characters = new List<CharacterStateChange>(settled.Characters.Characters);
            int index = characters.FindIndex(c => c.Final.EntityId == main.EntityId);
            CharacterCombatFacts projectedMain = index >= 0 ? characters[index].Final : main;
            var projectedPersistent = settled.Characters.Main?.Final ?? persistent;
            var completion = Complete(projectedMain, projectedPersistent, outcome, progression);
            var healedMain = completion.Characters.Characters[0].Final;
            var mainChange = new CharacterStateChange(index >= 0 ? characters[index].Expected : main, healedMain);
            if (index >= 0) characters[index] = mainChange;
            else characters.Add(mainChange);
            for (int i = 0; i < characters.Count; i++)
            {
                var cleared = CombatTemporaryStateCleanup.Clear(HeldEnvironmentEffects.ClearForBattleEnd(characters[i].Final));
                characters[i] = new CharacterStateChange(characters[i].Expected, cleared);
            }
            var changes = new CharacterChanges(characters, settled.Characters.Souls,
                new MainCharacterPersistentChange(main.OwnedId.Value, settled.Characters.Main?.Expected ?? persistent,
                    completion.Characters.Main.Final), owned: settled.Characters.Owned);
            return new BattleCompletionChanges(new SoulActionChanges(changes, settled.Field,
                settled.ConsumedSoulIds), outcome, progression);
        }


    }
}
