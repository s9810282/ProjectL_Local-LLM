using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.PlayerSkills
{
    public static class PlayerSkillQueries
    {
        public static PlayerSkillForecast Forecast(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules, PlayerSkillSourceStamp source)
        {
            var known = KnownInput(input);
            var selection = SelectionActual(known, source);
            var failure = !selection.PrefixIsValid ? selection.FailureReason
                : !selection.IsComplete ? PlayerSkillFailureReason.WrongPointCount : PlayerSkillFailureReason.None;
            var points = input.PlayerSkillRequest?.Points ?? Array.Empty<CellCoord>();
            if (failure != PlayerSkillFailureReason.None)
                return new PlayerSkillForecast(source, failure, PlayerSkillForecastCoverage.None, null, points);
            known = known.ForSkillForecast(new PlayerSkillRequest(points, source));
            var transition = PlayerSkillCalculator.Calculate(known, rules);
            if (!transition.Accepted)
                throw new InvalidOperationException("검증된 스킬 예측 계산이 거절되었습니다: " + transition.Reason);
            // 외부 반응까지 포함한 같은 전체 계산을 공개한다. 조건부 표시는 숨은 효과의 존재와 무관하게 고정한다.
            var prediction = new PlayerSkillResult(known.State.PlayerSkill.Definition, known.State.PlayerSkill.UpgradeKey,
                transition.Events, transition.PlayerSkill.UseSettlement);
            return new PlayerSkillForecast(source, PlayerSkillFailureReason.None,
                PlayerSkillForecastCoverage.KnownInformationConditional, prediction, points);
        }

        public static PlayerSkillSelectionResult Selection(CombatCommandInput input, PlayerSkillSourceStamp source)
            => SelectionActual(KnownInput(input), source);

        internal static PlayerSkillSelectionResult SelectionActual(CombatCommandInput input, PlayerSkillSourceStamp source)
        {
            var availability = Availability(input, source);
            var points = input.PlayerSkillRequest?.Points ?? Array.Empty<CellCoord>();
            int required = availability.SkillKey == PlayerSkillKey.MagicBullet
                ? availability.UpgradeKey == PlayerSkillUpgradeKey.Distortion ? 3 : 2 : 0;
            if (!availability.CanUse)
                return new PlayerSkillSelectionResult(source, points, required, Array.Empty<CellCoord>(), availability.FailureReason);
            return availability.SkillKey == PlayerSkillKey.MagicBullet ? MagicBulletCalculator.Selection(input, source)
                : new PlayerSkillSelectionResult(source, points, 0, Array.Empty<CellCoord>(),
                    points.Count == 0 ? PlayerSkillFailureReason.None : PlayerSkillFailureReason.WrongPointCount);
        }

        private static CombatCommandInput KnownInput(CombatCommandInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            // 벽의 양측 공개 성질은 기존 기하 계약이 요구하므로 보존한다. 숨은 원본과 그 ID/RNG 진행은 공개 입력에서 제외한다.
            MovementEnvironmentFacts environment = null;
            foreach (var _ in input.State.Environment.VisibleSteps(Owner.Player, value => environment = value, preservePublic: true)) { }
            var ids = environment.Tiles.Select(t => t.Id).ToHashSet();
            var bindings = new EnvironmentSemanticBindings(input.EnvironmentBindings.TerrainSourceIds.Where(ids.Contains).ToArray(),
                input.EnvironmentBindings.ObjectSourceIds.Where(ids.Contains).ToArray(), input.EnvironmentBindings.ObstacleEntityIds);
            var allocation = input.State.Allocation.IsAvailable
                ? new CombatAllocationFacts(input.State.Allocation.NextEntityId, CombatAllocationFacts.InitialSummonRandomState)
                : input.State.Allocation;
            var state = input.State.With(environment: environment, environmentProgress: EnvironmentProgressFacts.Initial(environment),
                itemRandomState: 1, allocation: allocation);
            return input.WithWorld(state, input.Characters, input.Main, input.SoulCounts, input.Souls, bindings);
        }

        public static PlayerSkillAvailability Availability(CombatCommandInput input, PlayerSkillSourceStamp source)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            var skill = input.State.PlayerSkill;
            var turn = input.State.Turn;
            bool belongsToActor = skill != null && skill.ActorId == input.ActorId && skill.MainInstanceId == input.MainId;
            var visibleSkill = belongsToActor ? skill : null;
            bool usedThisTurn = visibleSkill != null && turn.ControllerTurnSequence > 0 &&
                visibleSkill.LastUsedControllerTurnSequence == turn.ControllerTurnSequence;

            var failure = GetFailure(input, source, visibleSkill, usedThisTurn);
            return new PlayerSkillAvailability(source, visibleSkill, usedThisTurn, failure);
        }

        private static PlayerSkillFailureReason GetFailure(CombatCommandInput input, PlayerSkillSourceStamp source,
            PlayerSkillBattleState skill, bool usedThisTurn)
        {
            if (input.BattleInstanceId == 0 || input.Membership == null || input.State.Phase == CombatFlowPhase.None)
                return PlayerSkillFailureReason.NoActiveBattle;
            var turn = input.State.Turn;
            if (input.State.Phase != CombatFlowPhase.PlayerInput || turn.StagePhase != StagePhase.Combat ||
                turn.ControllerPhase != CombatControllerPhase.Player || input.ProcessingKind != CombatProcessingKind.Command)
                return PlayerSkillFailureReason.NotPlayerCombatTurn;

            var actor = input.Characters.SingleOrDefault(character => character.EntityId == input.ActorId);
            if (actor == null || actor.Health.IsDead || actor.Area != CharacterRuntimeArea.Field)
                return PlayerSkillFailureReason.ActorUnavailable;
            if (actor.Owner != Owner.Player || actor.OwnershipRole != CharacterOwnershipRole.Main || actor.OwnedId != input.MainId)
                return PlayerSkillFailureReason.NotSelectedMain;
            if (source.BattleInstanceId != input.BattleInstanceId)
                return PlayerSkillFailureReason.BattleChanged;
            if (source.ActorId != input.ActorId || source.ControllerTurnSequence != turn.ControllerTurnSequence ||
                !input.PlayerSkillSource.HasValue || !input.PlayerSkillSource.Value.Equals(source))
                return PlayerSkillFailureReason.SourceChanged;
            if (skill == null || skill.Definition.MainCharacterKey != actor.CharacterKey)
                return PlayerSkillFailureReason.NoSkillConfigured;
            if (CombatActionAdmission.IsStunned(actor))
                return PlayerSkillFailureReason.Stunned;
            if (AppliedStatusRules.IsRestricted(actor.StatusEffects, StatusEffectKey.Disarm))
                return PlayerSkillFailureReason.Disarmed;
            if (skill.RemainingUses == 0)
                return PlayerSkillFailureReason.StageUsesExhausted;
            return usedThisTurn ? PlayerSkillFailureReason.UsedThisTurn : PlayerSkillFailureReason.None;
        }
    }
}
