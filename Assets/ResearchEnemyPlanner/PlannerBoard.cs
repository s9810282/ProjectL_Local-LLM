using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyPlanner
{
    /// <summary>계획 안의 판 하나: 입력과, 그 판에서 전투가 끝났는지.</summary>
    internal sealed class PlannerState
    {
        internal CombatCommandInput Input { get; }
        internal StageOutcome Outcome { get; }
        internal bool IsEnded => Outcome != StageOutcome.InProgress;

        internal PlannerState(CombatCommandInput input, StageOutcome outcome)
        {
            Input = input ?? throw new ArgumentNullException(nameof(input));
            Outcome = outcome;
        }
    }

    /// <summary>
    /// 계획용 판을 만드는 순수 함수들(설계 CP5). 행동은 게임 정산과 같은 입구(<see cref="CombatCommandCalculator.AdvancePrepared"/>)로
    /// 돌리고 <see cref="CombatCommandComposition.Project"/>로 다음 판을 만든다. 실제 게임 상태는 건드리지 않는다.
    /// </summary>
    internal static class PlannerBoard
    {
        /// <summary>
        /// 계획용 고정 씨앗(설계 TP-3). 연구 시뮬레이터 <c>prediction_seed</c>와 같은 꼴: (진영 턴 순번 × 2 + 1) × 1,000,003 + 다음 개체 ID.
        /// 0이면 1(난수 상태 0을 피한다).
        /// </summary>
        internal static uint Seed(CombatCommandInput input)
        {
            unchecked
            {
                long sequence = input.State.Turn.ControllerTurnSequence;
                long next = input.State.Allocation.NextEntityId;
                uint seed = (uint)(((sequence * 2 + 1) * 1_000_003L + next) & 0xFFFFFFFFL);
                return seed == 0 ? 1u : seed;
            }
        }

        /// <summary>
        /// 적이 보는 환경 + 난수 상태 넷을 씨앗으로 바꾼 입력(설계 Q-CP1 A, TP-3). 실제 앞으로 나올 난수와 숨은 칸을 보지 않게 한다.
        /// 환경은 적이 모르는 칸만 빼고, 양쪽에 공개된 칸(벽 등)은 원본 그대로 둔다(벽 형상 검사가 양쪽 공개를 요구한다).
        /// </summary>
        internal static CombatCommandInput Fair(CombatCommandInput input)
        {
            MovementEnvironmentFacts visible = null;
            foreach (var _ in input.State.Environment.VisibleSteps(Owner.Enemy, value => visible = value, preservePublic: true)) { }
            return Reseed(input, Seed(input), visible);
        }

        /// <summary>난수 상태 넷(아이템·환경 진행·소환·증원)을 같은 씨앗으로 맞춘다. environment가 null이면 그대로 둔다.</summary>
        internal static CombatCommandInput Reseed(CombatCommandInput input, uint seed, MovementEnvironmentFacts environment = null)
        {
            var state = input.State;
            var progress = state.EnvironmentProgress;
            var allocation = state.Allocation;
            var reinforcements = state.Reinforcements;
            var reseeded = state.With(
                environment: environment,
                environmentProgress: new EnvironmentProgressFacts(progress.NextObjectEntryOrdinal, progress.NextSourceId, seed),
                allocation: allocation.IsAvailable ? new CombatAllocationFacts(allocation.NextEntityId, seed) : null,
                itemRandomState: seed,
                reinforcements: reinforcements.IsEnabled
                    ? new NexusReinforcementState(reinforcements.NexusId, seed, reinforcements.Records, reinforcements.LastSpawnEnemyTurn)
                    : null);
            return input.WithWorld(reseeded, input.Characters, input.Main, input.SoulCounts, input.Souls, input.EnvironmentBindings);
        }

        /// <summary>
        /// 같은 세계를 새 처리 종류의 입력으로 다시 만든다(번호·종류·기대 식별자). 꼴은 <c>CombatFirstUseRehearsal.Base</c>와 시험의 <c>FreshCommand</c>.
        /// 자동 진행은 번호 0, 명령은 번호 1이다. 기대 식별자는 지금 전투 번호·진영 턴 순번이다.
        /// </summary>
        internal static CombatCommandInput Rebuild(CombatCommandInput i, CombatProcessingKind processing, CombatCommandKind kind)
        {
            bool command = processing == CombatProcessingKind.Command;
            return new CombatCommandInput(command ? 1 : 0, kind, EntityId.Empty, default, i.State, i.Characters, i.Profiles, i.Main,
                i.Owned, i.SoulCounts, i.Souls, i.Artifacts, default, Rotation4.R0, i.Battle, processing, null, null,
                i.BattleInstanceId, i.ArtifactItems, i.EnvironmentBindings, i.ObstacleContents, null, i.ItemInventory,
                i.ItemDefinitions, i.GoldBalance, i.FameBalance, i.Membership, i.BattleInstanceId,
                i.State.Turn.ControllerTurnSequence);
        }

        /// <summary>
        /// 판 서명(설계 TP-2): 필드의 산 캐릭터 (ID, 칸, HP, 행동 단계), 구조물 (ID, HP), 진영 단계·진영 턴 순번.
        /// 공정 입력과 실제 입력은 환경·난수만 다르므로 같은 서명이 나온다.
        /// </summary>
        internal static string Signature(CombatCommandInput input)
        {
            var builder = new StringBuilder(256);
            builder.Append(input.State.Turn.ControllerPhase).Append('/').Append(input.State.Turn.ControllerTurnSequence).Append('|');
            foreach (var c in input.Characters.Where(c => c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead && c.Anchor.HasValue)
                         .OrderBy(c => c.EntityId.Value))
                builder.Append(c.EntityId.Value).Append('@').Append(c.Anchor.Value.X).Append(',').Append(c.Anchor.Value.Y)
                    .Append(':').Append(c.Health.CurrentHealth).Append(':').Append((int)c.ActionUse).Append(';');
            builder.Append('|');
            foreach (var s in input.State.Structures.OrderBy(s => s.EntityId.Value))
                builder.Append(s.EntityId.Value).Append(':').Append(s.Health.CurrentHealth).Append(';');
            return builder.ToString();
        }
    }

    /// <summary>
    /// 계획 하나 동안 행동을 엔진으로 돌리는 곳. 적 행동은 게임 정산과 같은 입구(<see cref="CombatCommandCalculator.AdvancePrepared"/>),
    /// 플레이어 행동은 명령 계산(<see cref="CombatCommandCalculator.Calculate"/>)으로 돌리고 <see cref="CombatCommandComposition.Project"/>로
    /// 다음 판을 만든다. 거절이면 null이다. 후보 하나를 돌리다 Core 검사 예외(<see cref="InvalidOperationException"/>·<see cref="ArgumentException"/>)가
    /// 나면 그 후보만 거절로 보고 <see cref="Failures"/>를 센다(계획 전체를 버리지 않게).
    /// </summary>
    internal sealed class PlannerSimulator
    {
        internal IReadOnlyList<IMovementTileRule> Rules { get; }
        /// <summary>돌린 행동 수(진단).</summary>
        internal int Simulations { get; private set; }
        /// <summary>예외로 거절로 본 행동 수(진단).</summary>
        internal int Failures { get; private set; }
        /// <summary>마지막 예외 문구(진단).</summary>
        internal string LastFailure { get; private set; }

        internal PlannerSimulator(IReadOnlyList<IMovementTileRule> rules)
        {
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
        }

        /// <summary>
        /// 측정 전용(정산 되풀이 세기, 병렬 설계 §21): 정산 하나마다 (입력, 적 결정 또는 null)을 알린다. 평소에는 null이라 아무 일도 하지 않는다.
        /// </summary>
        internal static Action<CombatCommandInput, EnemyDecision> Observe;

        /// <summary>적 진영 턴 판(또는 진영 시작 판)에 적 결정 하나를 정산한다.</summary>
        internal PlannerState ApplyEnemy(PlannerState board, EnemyDecision decision)
        {
            Observe?.Invoke(board.Input, decision);
            return Run(() => CombatCommandCalculator.AdvancePrepared(board.Input, decision, Rules));
        }

        /// <summary>명령 입력(플레이어 차례)에 행동 하나를 정산한다.</summary>
        internal PlannerState ApplyCommand(CombatCommandInput command)
        {
            Observe?.Invoke(command, null);
            return Run(() => CombatCommandCalculator.Calculate(command, Rules));
        }

        private PlannerState Run(Func<CombatCommandTransition> calculate)
        {
            Simulations++;
            try
            {
                var transition = calculate();
                if (!transition.Accepted) return null;
                return new PlannerState(CombatCommandComposition.Project(transition), transition.Outcome.Outcome);
            }
            catch (Exception exception) when (exception is InvalidOperationException || exception is ArgumentException)
            {
                Failures++;
                LastFailure = exception.Message;
                return null;
            }
        }
    }
}
