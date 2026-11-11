using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Rewards;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Experience;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Tiles;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>한 명령의 최종 변경만 계산한다. 계산 중의 투영값은 호출 이후 보관하지 않는다.</summary>
    public static class CombatCommandCalculator
    {
        /// <summary>명령 하나를 계산한다. 원래 명령 정산 뒤 대기 포효를 반응 단계로 붙인다(T5.2). 미리보기는 반응을 건너뛴다(G13).</summary>
        public static CombatCommandTransition Calculate(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)
            => MovementUndoPolicy.Reconcile(BossRoarCalculator.ApplyPendingReactions(Calculate(input, rules, out _), rules));

        public static AttackRangeResult GetAttackRange(CombatCommandInput input) => CombatAttackQueries.Range(input);

        /// <summary>현재 공개 사실로 선택 유닛의 일반 행동 가능 여부를 반환한다.</summary>
        public static CombatActionAvailability GetActionAvailability(CombatCommandInput input) => CombatActionAdmission.GetAvailability(input);

        /// <summary>필드 유닛 하나의 공격 범위 칸 조회다. 차례·국면과 무관하고 지형은 플레이어 시점이다(호버 공격 범위 설계 UR2).</summary>
        public static UnitAttackRangeResult GetUnitAttackRange(CombatCommandInput input) => CombatAttackQueries.UnitRange(input);

        /// <summary>보스(입력의 행동자)가 지금 포효하면 어떻게 되는지의 예고다. 실제 포효와 같은 계산, 플레이어 시점 공개 미리보기다(포효 예고 설계 RF2).</summary>
        public static RoarForecastResult GetRoarForecast(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)
            => BossRoarCalculator.Forecast(input, rules);

        public static CombatCommandTransition Advance(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)
            => Advance(input, rules, null);

        /// <summary>준비 경로와 같은 Utility 세션을 한 호출에서 진행하는 진단 진입점이다.</summary>
        public static CombatCommandTransition AdvanceUtility(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)
            => AdvanceUtility(input, rules, null);

        internal static CombatCommandTransition AdvanceUtility(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules,
            EnemyDecisionDiagnostics diagnostics)
            => AdvanceUtility(input, rules, diagnostics, null);

        /// <param name="sideCache">같은 적 진영의 결정끼리 공유 구조를 다시 쓰는 캐시다(적 AI 근간 개편 설계 6.8). null이면 쓰지 않는다.</param>
        internal static CombatCommandTransition AdvanceUtility(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules,
            EnemyDecisionDiagnostics diagnostics, EnemyDecisionSideCache sideCache)
            => AdvanceCore(input, rules, diagnostics, immediateCompatibility: false, sideCache);

        internal static CombatCommandTransition Advance(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules,
            EnemyDecisionDiagnostics diagnostics)
            => AdvanceCore(input, rules, diagnostics, immediateCompatibility: true, null);

        private static CombatCommandTransition AdvanceCore(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules,
            EnemyDecisionDiagnostics diagnostics, bool immediateCompatibility, EnemyDecisionSideCache sideCache)
        {
            MovementEnvironmentCalculator.RequireRules(rules);
            RequireAdvance(input);
            var transition = input.State.Turn.ControllerPhase == CombatControllerPhase.Enemy
                ? EnemyAi.EnemyAiTurnService.Calculate(input, rules, diagnostics, immediateCompatibility, sideCache)
                : CombatTurnCalculator.BeginControllerTurn(input, rules);
            return FinishAdvance(transition, rules);
        }

        /// <summary>진영 시작이나 대기 포효를 처리할 때는 적 행동 선택이 필요하지 않다.</summary>
        public static bool RequiresEnemyDecision(CombatCommandInput input)
        {
            RequireAdvance(input);
            return input.State.Turn.ControllerPhase == CombatControllerPhase.Enemy &&
                BossRoarCalculator.PendingBosses(input).Count == 0;
        }

        /// <summary>준비한 행동 하나를 기존 명령으로 정산한다. 거절 이후 대안은 선택 세션이 찾는다.</summary>
        public static CombatCommandTransition AdvancePrepared(CombatCommandInput input, EnemyDecision decision,
            IReadOnlyList<IMovementTileRule> rules)
        {
            MovementEnvironmentCalculator.RequireRules(rules);
            RequireAdvance(input);
            if (input.State.Turn.ControllerPhase != CombatControllerPhase.Enemy)
                return FinishAdvance(CombatTurnCalculator.BeginControllerTurn(input, rules), rules);
            var pending = BossRoarCalculator.PendingBosses(input);
            if (pending.Count > 0)
                return FinishAdvance(BossRoarCalculator.TryRoar(input, pending[0], rules), rules);
            if (decision == null) throw new ArgumentNullException(nameof(decision));
            return FinishAdvance(EnemyAi.EnemyAiTurnService.Execute(input, decision, rules), rules);
        }

        private static void RequireAdvance(CombatCommandInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.ProcessingKind != CombatProcessingKind.Advance || input.State.Phase != CombatFlowPhase.Automatic ||
                input.State.Turn.StagePhase != StagePhase.Combat)
                throw new InvalidOperationException("자동 진행 입력이 아닙니다.");
            if (input.State.Turn.ControllerPhase != CombatControllerPhase.Enemy &&
                input.State.Turn.ControllerPhase != CombatControllerPhase.StartingPlayer &&
                input.State.Turn.ControllerPhase != CombatControllerPhase.StartingEnemy)
                throw new InvalidOperationException("자동 진행 가능한 진영 단계가 아닙니다.");
        }

        // 자동 진행 끝도 반응 경계다. 이미 정산한 포효는 카운터와 사건 순서로 구분한다.
        private static CombatCommandTransition FinishAdvance(CombatCommandTransition transition, IReadOnlyList<IMovementTileRule> rules) =>
            MovementUndoPolicy.Reconcile(BossRoarCalculator.ApplyPendingReactions(transition, rules));

        internal static CombatAttackCalculation BeginPreview(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)
            => new CombatAttackCalculation(input, rules);

        public static AttackPreview Preview(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)
            => Preview(input, rules, out _);

        internal static AttackPreview Preview(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules,
            out CombatCommandTransition projection)
        {
            projection = Calculate(input, rules, out var preview, publicPreview: true);
            return projection.Accepted ? preview : AttackPreview.Failure(
                Enum.Parse<AttackFailureReason>(projection.Reason), input.ActorId, EntityId.Empty, projection.BlockedContacts);
        }

        private static CombatCommandTransition Calculate(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules,
            out AttackPreview preview, bool publicPreview = false)
        {
            preview = default;
            if (input == null) throw new ArgumentNullException(nameof(input));
            MovementEnvironmentCalculator.RequireRules(rules);
            if (input.Kind == CombatCommandKind.UndoMove) return MovementUndoCalculator.Calculate(input);
            if (input.Kind == CombatCommandKind.EquipItem || input.Kind == CombatCommandKind.UnequipItem || input.Kind == CombatCommandKind.ReplaceItem)
                return Items.ItemEquipmentCalculator.Calculate(input);
            if (input.Kind == CombatCommandKind.Move) return CombatMovementCalculator.Calculate(input, rules);
            if (input.Kind == CombatCommandKind.EndPlayerTurn) return CombatTurnCalculator.EndPlayerTurn(input, rules);
            if (input.Kind == CombatCommandKind.EnterCombat) return CombatTurnCalculator.EnterCombat(input, rules);
            if (input.Kind == CombatCommandKind.PlaceMainCharacter || input.Kind == CombatCommandKind.PlaceUnit)
                return CombatPlacementCalculator.Place(input, rules);
            if (input.Kind == CombatCommandKind.ReturnUnit) return CombatPlacementCalculator.Return(input);
            if (input.Kind == CombatCommandKind.Attack) return CombatAttackCalculator.Calculate(input, rules, publicPreview, out preview);
            return Reject(input, "UnsupportedCommand");
        }

        internal static CombatCommandTransition Finish(CombatCommandInput input,
            Dictionary<EntityId, CharacterCombatFacts> projected, Dictionary<CharacterInstanceId, int> counts,
            SoulFieldSnapshot soulField, Dictionary<CellCoord, ThornFieldCellSnapshot> fields,
            StructureCombatFacts[] structures, List<long> consumed, Dictionary<EntityId, CellCoord> deathAnchors,
            SoulEffectCalculator souls, AttackResult? attack, CombatCommandState proposedState = null,
            List<CombatEvent> events = null, ISet<EntityId> resolvedDeaths = null,
            EnvironmentSemanticBindings environmentBindings = null, UnitActionUseSettlementSnapshot actionUseSettlement = null,
            CombatStageTally stageTally = null, IReadOnlyList<OwnedCharacterChange> proposedOwned = null)
        {
            using var work = BeginFinish(input, projected, counts, soulField, fields, structures, consumed, deathAnchors, souls, attack, proposedState, events, resolvedDeaths, environmentBindings, actionUseSettlement, stageTally, proposedOwned);
            while (!work.IsComplete) work.Step();
            return work.Result;
        }

        internal static Finalization BeginFinish(CombatCommandInput input,
            Dictionary<EntityId, CharacterCombatFacts> projected, Dictionary<CharacterInstanceId, int> counts,
            SoulFieldSnapshot soulField, Dictionary<CellCoord, ThornFieldCellSnapshot> fields,
            StructureCombatFacts[] structures, List<long> consumed, Dictionary<EntityId, CellCoord> deathAnchors,
            SoulEffectCalculator souls, AttackResult? attack, CombatCommandState proposedState = null,
            List<CombatEvent> events = null, ISet<EntityId> resolvedDeaths = null,
            EnvironmentSemanticBindings environmentBindings = null, UnitActionUseSettlementSnapshot actionUseSettlement = null,
            CombatStageTally stageTally = null, IReadOnlyList<OwnedCharacterChange> proposedOwned = null) =>
            new Finalization(completed => FinishSteps(input, projected, counts, soulField, fields, structures, consumed, deathAnchors, souls, attack, proposedState, events, resolvedDeaths, environmentBindings, actionUseSettlement, stageTally, proposedOwned, completed).GetEnumerator());

        private static IEnumerable<int> FinishSteps(CombatCommandInput input,
            Dictionary<EntityId, CharacterCombatFacts> projected, Dictionary<CharacterInstanceId, int> counts,
            SoulFieldSnapshot soulField, Dictionary<CellCoord, ThornFieldCellSnapshot> fields,
            StructureCombatFacts[] structures, List<long> consumed, Dictionary<EntityId, CellCoord> deathAnchors,
            SoulEffectCalculator souls, AttackResult? attack, CombatCommandState proposedState,
            List<CombatEvent> events, ISet<EntityId> resolvedDeaths,
            EnvironmentSemanticBindings environmentBindings, UnitActionUseSettlementSnapshot actionUseSettlement,
            CombatStageTally stageTally, IReadOnlyList<OwnedCharacterChange> proposedOwned, Action<CombatCommandTransition> completed)
        {
            events ??= new List<CombatEvent>();
            var sourceState = proposedState ?? input.State;
            var environment = sourceState.Environment;
            var progress = sourceState.EnvironmentProgress;
            environmentBindings ??= input.EnvironmentBindings;
            var fallen = new HashSet<EntityId>();
            foreach (var entry in events)
            {
                if (entry is CombatFallEvent fall) fallen.Add(fall.EntityId);
                yield return 0;
            }
            FlagDropCalculator.ResolveDeaths(input, projected, structures, deathAnchors,
                ref environment, ref environmentBindings, ref progress, events, fallen);
            yield return 0;
            var outcome = BattleOutcomeCalculator.Resolve(projected.Values.ToArray(), structures, input.State.Objective);
            yield return 0;
            foreach (var character in projected.Values.ToArray())
            {
                yield return 0;
                if (character.Health.IsDead || outcome.IsStageEnded)
                    projected[character.EntityId] = CombatTemporaryStateCleanup.Clear(character, events,
                        character.Health.IsDead ? ItemEventReason.Death : ItemEventReason.BattleEnded);
            }
            var pendingOwned = new Dictionary<CharacterInstanceId, OwnedCharacterChange>();
            foreach (var change in proposedOwned ?? Array.Empty<OwnedCharacterChange>())
            {
                pendingOwned.Add(change.Expected.Id, change);
                yield return 0;
            }
            var ownedInput = pendingOwned.Count == 0 ? input : input.WithPersistentFacts(input.Owned.Select(o =>
                pendingOwned.TryGetValue(o.Id, out var change) ? change.Final : o).ToArray(), input.ItemInventory, input.GoldBalance, input.FameBalance);
            yield return 0;
            if (!outcome.IsStageEnded)
            {
                using var presence = TraitPresenceReconciler.BeginReconcile(ownedInput, projected, events);
                while (!presence.IsComplete)
                {
                    presence.Step();
                    yield return 0;
                }
            }
            // 이 단계의 처치 경험치를 쌓고, 승리 단계면 보유 기록에 반영한다(경험치·레벨업 설계 §4.4).
            var kills = ConfirmedKillCalculator.Collect(input, projected, events, sourceState);
            yield return 0;
            // 이 단계의 피해·이동·처치를 보유 아군의 스테이지 기록에 더한다. 승리 정산의 영수증이 이 값을 복사하므로 그 앞이다.
            CombatStageRecordSettlement.Record(input, projected, events, kills, stageTally);
            yield return 0;
            int gold = 0;
            foreach (var kill in kills)
            {
                gold = checked(gold + kill.Gold);
                yield return 0;
            }
            var rewards = sourceState.Rewards;
            CombatRewardChange rewardChange = null;
            var clear = outcome.Outcome == StageOutcome.Victory ? input.Battle?.ClearRewards : null;
            if (gold != 0)
                rewards = new CombatRewardFacts(rewards.ParticipantsCaptured, rewards.ClearParticipantIds,
                    checked(rewards.KillGoldEarned + gold));
            var experience = CombatExperienceSettlement.Settle(ownedInput, projected, events, outcome, kills, rewards, clear);
            yield return 0;
            var finalOwned = new Dictionary<CharacterInstanceId, OwnedCharacterRecord>();
            foreach (var record in ownedInput.Owned)
            {
                finalOwned.Add(record.Id, record);
                yield return 0;
            }
            foreach (var change in experience.Owned)
            {
                finalOwned[change.Final.Id] = change.Final;
                yield return 0;
            }
            var ownedChanges = new List<OwnedCharacterChange>();
            foreach (var record in input.Owned)
            {
                if (!record.SameValues(finalOwned[record.Id])) ownedChanges.Add(new OwnedCharacterChange(record, finalOwned[record.Id]));
                yield return 0;
            }
            // 경험치 영수증도 같은 원래 기대값과 합성된 최종 보유 기록을 가리킵니다.
            if (pendingOwned.Count > 0)
                for (int i = 0; i < events.Count; i++)
                {
                    yield return 0;
                    if (events[i] is CombatExperienceEvent applied && applied.Kind == CombatEventKind.ExperienceApplied)
                    {
                        var combined = ownedChanges.Single(c => c.Final.Id == applied.Owned.Final.Id);
                        events[i] = CombatExperienceEvent.Applied(applied.RecipientId, combined, applied.Amount,
                            applied.LevelsGained, applied.MainGrowth);
                    }
                }
            var rewardSettlement = new CombatRewardSettlement(kills.Count, gold, clear?.Gold ?? 0, clear?.Fame ?? 0,
                clear != null, experience.Characters);
            int totalGold = rewardSettlement.GoldAwarded;
            int fame = rewardSettlement.ClearFame;
            if (totalGold != 0 || fame != 0)
            {
                int expected = input.GoldBalance ?? throw new InvalidOperationException("처치 골드 계산에는 현재 잔액이 필요합니다.");
                int expectedFame = input.FameBalance ?? (fame == 0 ? 0 :
                    throw new InvalidOperationException("클리어 명성 계산에는 현재 잔액이 필요합니다."));
                rewardChange = new CombatRewardChange(expected, checked(expected + totalGold), expectedFame, checked(expectedFame + fame));
            }
            // 메인이 승리 반영으로 성장했으면 종료 체력 정산과 지속 상태 변경은 성장한 지속 상태에서 시작한다(X5).
            var mainState = experience.GrownMain ?? input.Main;
            var main = input.Characters.Single(c => c.OwnershipRole == CharacterOwnershipRole.Main);
            var finalMain = projected[main.EntityId];
            if (outcome.IsStageEnded)
            {
                foreach (var c in projected.Values.OrderBy(c => c.EntityId).ToArray())
                {
                    yield return 0;
                    var cleared = HeldEnvironmentEffects.ClearForBattleEnd(c);
                    if (ReferenceEquals(c, cleared)) continue;
                    projected[c.EntityId] = cleared;
                    events.Add(new CombatHeldEnvironmentEvent(c, cleared));
                }
                finalMain = projected[main.EntityId];
                var terminal = new BattleCompletionCalculator(input.Artifacts).PlanTerminalHealth(finalMain,
                    mainState.WithCurrentHealth(finalMain.Health.CurrentHealth), outcome, events);
                projected[main.EntityId] = finalMain = terminal.Characters[0].Final;
                yield return 0;
            }
            var orderedChanges = new SortedDictionary<EntityId, CharacterStateChange>();
            var inputIds = new HashSet<EntityId>();
            foreach (var character in input.Characters)
            {
                inputIds.Add(character.EntityId);
                if (!ReferenceEquals(character, projected[character.EntityId]))
                    orderedChanges.Add(character.EntityId, new CharacterStateChange(character, projected[character.EntityId]));
                yield return 0;
            }
            var changes = orderedChanges.Values.ToArray();
            yield return 0;
            // 이번 명령에서 만든 개체는 투영에만 있다. 원래 있던 개체의 변경과 섞지 않고 최종 사실 하나로 게시한다.

            var orderedCreated = new SortedDictionary<EntityId, CharacterCombatFacts>();
            foreach (var character in projected.Values)
            {
                if (!inputIds.Contains(character.EntityId)) orderedCreated.Add(character.EntityId, character);
                yield return 0;
            }
            var created = orderedCreated.Values.ToArray();
            var persistent = new MainCharacterPersistentChange(input.MainId, input.Main,
                mainState.WithCurrentHealth(finalMain.Health.CurrentHealth));
            var characterChanges = new CharacterChanges(changes, counts.Where(p => p.Value != input.SoulCounts[p.Key])
                .Select(p => new SoulCountChange(p.Key, input.SoulCounts[p.Key], p.Value)).ToArray(), persistent, created, ownedChanges);
            yield return 0;
            var deaths = changes.Where(c => !c.Expected.Health.IsDead && c.Final.Health.IsDead &&
                !fallen.Contains(c.Final.EntityId) &&
                (resolvedDeaths == null || !resolvedDeaths.Contains(c.Final.EntityId)))
                .Select(c => new SoulDeathFacts(c, deathAnchors.TryGetValue(c.Final.EntityId, out var anchor)
                    ? anchor : c.Expected.Anchor ?? throw new InvalidOperationException("사망 영혼 정산의 실제 위치가 없습니다."))).ToArray();
            yield return 0;
            var deathField = souls.PlanDeaths(soulField, deaths, input.State.InCombat);
            if (!deathField.Expected.SameValues(deathField.Final)) events.Add(new CombatSoulEvent(deathField));
            soulField = deathField.Final;
            // 개별 유닛 사망은 진영 턴을 끝내거나 예산을 채우지 않는다. 종료 결과만 모든 추가 진행을 막는다.
            var state = sourceState.With(
                phase: outcome.IsStageEnded ? CombatFlowPhase.Finished : sourceState.Phase,
                structures: structures, thornFields: fields.Values.OrderBy(f => f.Cell.Y).ThenBy(f => f.Cell.X).ToArray(),
                environment: environment, environmentProgress: progress, rewards: rewards);
            if (outcome.IsStageEnded)
            {
                state = state.With(environment: TraitTrailCalculator.Clear(state.Environment, events));
                events.Add(new CombatOutcomeEvent(outcome));
            }
            yield return 0;
            completed(new CombatCommandTransition(input, true, "", characterChanges,
                new SoulFieldChange(input.Souls, soulField), state, outcome, attack, consumed, events: events,
                environmentBindings: input.EnvironmentBindings.SameValues(environmentBindings) ? null :
                    new EnvironmentBindingsChange(input.EnvironmentBindings, environmentBindings), blockedContacts: attack?.BlockedContacts,
                rewardChange: rewardChange, rewardSettlement: rewardSettlement, actionUseSettlement: actionUseSettlement));
            yield return 0;
                }

        /// <summary>최종 변경 제안을 원래 정산 순서로 조립하며 완성된 전이만 반환한다.</summary>
        internal sealed class Finalization : IDisposable
        {
            private IEnumerator<int> _steps;
            private CombatCommandTransition _result;
            private bool _complete;
            private bool _disposed;
            private bool _faulted;
            private long _version;
            internal bool IsComplete
            {
                get
                {
                    RequireActive();
                    return _complete;
                }
            }
            internal long WorkVersion
            {
                get
                {
                    RequireActive();
                    return _version;
                }
            }
            internal CombatCommandTransition Result
            {
                get
                {
                    RequireActive();
                    if (!_complete) throw new InvalidOperationException("전이 정산이 끝나지 않았습니다.");
                    return _result;
                }
            }

            internal Finalization(Func<Action<CombatCommandTransition>, IEnumerator<int>> create)
            {
                _steps = create(value => _result = value);
            }

            internal void Step()
            {
                RequireActive();
                if (_complete) return;
                using var sample = Diagnostics.CoreProfiling.Sample(Diagnostics.CoreProfileSection.CommandFinalization);
                try
                {
                    _complete = !_steps.MoveNext();
                    _version++;
                    if (_complete)
                    {
                        _steps.Dispose();
                        _steps = null;
                    }
                }
                catch
                {
                    _faulted = true;
                    _steps?.Dispose();
                    _steps = null;
                    _result = null;
                    throw;
                }
            }

            private void RequireActive()
            {
                if (_disposed) throw new ObjectDisposedException(nameof(Finalization));
                if (_faulted) throw new InvalidOperationException("실패한 전이 정산은 다시 진행할 수 없습니다.");
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _steps?.Dispose();
                _steps = null;
                _result = null;
            }
        }

        internal static void Collect(CombatCommandInput input, SoulEffectCalculator souls,
            Dictionary<EntityId, CharacterCombatFacts> projected, Dictionary<CharacterInstanceId, int> counts,
            CharacterCombatFacts actor, SoulArrivalKind arrivalKind, ref SoulFieldSnapshot field, List<long> consumed, List<CombatEvent> events = null)
        {
            var owned = input.Owned.Single(o => o.Id == actor.OwnedId.Value);
            var main = actor.OwnershipRole == CharacterOwnershipRole.Main
                ? input.Main.WithCurrentHealth(actor.Health.CurrentHealth) : null;
            int baseMax = main?.PersistentMaxHealth ?? SubBaseMaxHealth(actor);
            var collected = souls.Collect(new SoulCollectorInput(actor, owned, counts[owned.Id], baseMax, main, arrivalKind), field, true);
            if (collected.ConsumedSoulIds.Count != 0) events?.Add(new CombatSoulEvent(collected.Field, actor.EntityId, collected));
            foreach (var change in collected.Characters.Characters) projected[change.Final.EntityId] = change.Final;
            foreach (var change in collected.Characters.Souls) counts[change.Id] = change.FinalCount;
            field = collected.Field.Final;
            consumed.AddRange(collected.ConsumedSoulIds);
        }

        /// <summary>
        /// 서브의 영혼·전투 보정 전 최대 체력이다. 생성 때 레벨로 성장시킨 값이 전투 기록에 들어 있으므로 정의에서 다시 고르지 않는다.
        /// 전투 중 레벨이 바뀌면 전투 기록이 함께 바뀌므로 이 값도 따라간다.
        /// </summary>
        private static int SubBaseMaxHealth(CharacterCombatFacts actor) => SoulGrowthMath.BaseMaxHealth(actor.Health,
            actor.Stats.SoulMaxHealthBonus, actor.TemporaryMaxHealthBonus, actor.PositionModifier.MaxHealthBonus);

        internal static int ConsumeFields(Dictionary<CellCoord, ThornFieldCellSnapshot> fields, CellCoord[] visited,
            List<ThornFieldCellSnapshot> consumed = null)
        {
            int damage = 0;
            foreach (var cell in visited)
                if (fields.Remove(cell, out var field))
                {
                    consumed?.Add(field);
                    damage = checked(damage + field.ThornDamage);
                }
            return damage;
        }

        internal static CombatAttackProfile RequiredProfile(CombatCommandInput input, CharacterCombatFacts character)
        {
            if (character.OwnedId.HasValue)
            {
                var exact = input.Profiles.SingleOrDefault(p => p.OwnedId == character.OwnedId && p.Definition.Key == character.CharacterKey);
                if (exact != null) return exact;
                if (input.Owned.SingleOrDefault(o => o.Id == character.OwnedId.Value)?.Growth.IsEmpty == false)
                    throw new InvalidOperationException("특성이 성장한 개체의 전용 프로필이 없습니다.");
            }
            return input.Profiles.SingleOrDefault(p => !p.OwnedId.HasValue && p.Definition.Key == character.CharacterKey) ??
                throw new InvalidOperationException("명시적으로 연결한 지정 방식과 유닛 정의가 필요합니다.");
        }

        internal static CombatAttackProfile RequiredProfile(CombatCommandInput input, CharacterCombatFacts character,
            CombatProfileReadSet profileReads)
        {
            var profile = RequiredProfile(input, character);
            profileReads?.Record(character.EntityId, profile);
            return profile;
        }

        internal static CharacterCombatFacts Hurt(CharacterCombatFacts c, int damage) => Copy(c,
            new HealthStateSnapshot(Math.Max(0, c.Health.CurrentHealth - Math.Min(c.Health.CurrentHealth, damage)), c.Health.MaxHealth), c.Stats);
        internal static CombatCommandTransition Reject(CombatCommandInput input, string reason,
            IReadOnlyList<AttackBlockedContact> blockedContacts = null) => new(input, false, reason,
            new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>()), new SoulFieldChange(input.Souls, input.Souls),
            input.State, new StageOutcomeSnapshot(StageOutcome.InProgress, StageOutcomeReason.None), blockedContacts: blockedContacts);

        internal static CharacterCombatFacts Copy(CharacterCombatFacts c, HealthStateSnapshot health,
            UnitStatsSnapshot stats, CellCoord? anchor = null)
        {
            bool dead = health.IsDead;
            var position = anchor ?? c.Anchor;
            // 사망하면 위치를 비우고 제외 영역으로 옮기는 규칙은 여기(Core 계산)가 소유한다. 필드 나열은 계약 사본이 맡는다.
            return RampageApplication.ReconcileChange(c, c.WithPlacement(
                area: dead ? CharacterRuntimeArea.Excluded : c.Area,
                reserveSlotIndex: dead ? null : c.ReserveSlotIndex,
                anchor: dead ? null : c.Anchor.HasValue ? position : null,
                rotation: dead ? null : c.Rotation,
                occupiedCells: dead ? Array.Empty<CellCoord>() : c.Anchor.HasValue ? new[] { position.Value } : c.OccupiedCells,
                health: health, stats: stats));
        }

        internal static Rotation4 AttackRotation(AttackAim aim, CellCoord origin)
        {
            if (aim.Kind == AttackAimKind.Direction)
                return aim.Direction == AttackDirection.Right ? Rotation4.R90 : aim.Direction == AttackDirection.Down ? Rotation4.R180 :
                    aim.Direction == AttackDirection.Left ? Rotation4.R270 : Rotation4.R0;
            if (!aim.Cell.HasValue) return Rotation4.R0;
            long x = (long)aim.Cell.Value.X - origin.X, y = (long)aim.Cell.Value.Y - origin.Y;
            return Math.Abs(x) >= Math.Abs(y) ? x > 0 ? Rotation4.R90 : x < 0 ? Rotation4.R270 : Rotation4.R0 : y > 0 ? Rotation4.R0 : Rotation4.R180;
        }
    }
}
