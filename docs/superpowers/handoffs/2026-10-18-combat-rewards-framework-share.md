# Combat Rewards — Framework/Buffer Share

Date: 2026-10-18

Status: Local technical handoff; not externally delivered. See execution evidence (Git history: `2026-10-18-combat-rewards-plan.md#15-execution-record--r5-2026-10-18`) and [View consumption contracts](2026-10-18-combat-rewards-view-handoff.md). No Framework assembly or Plan/Job implementation change was required. The B1-B3 boundary follow-up is complete; see boundary verification and ownership review (Git history: `2026-10-18-combat-reward-boundaries-plan.md#9-execution-record--b3-2026-10-18`).

## 1. Registration and ownership

`BufferSystemFactory.Create` constructs exactly one FameBuffer alongside existing stores. It exports public `IFameBufferReadSink : IServiceModule` (`int Amount { get; }`) and internal `IFameBufferStore : IServiceModule` (`int PrepareAmount(int expected, int final)`, `void InstallAmount(int prepared)`). Internal FameBufferKey.Read/Store follow the existing typed registration pattern. The Buffer group exposes the read sink as `IBufferQuery.FameReadSink` and injects the store into BattleTransactions. No public fame write/reset service or lifecycle module was introduced.

The concrete consumer is CombatInputAssembler: after PostBindService it includes actual CurrencyReadSink.Amount and FameReadSink.Amount in every command/advance/preview input. Existing composition preserves expected/final balances across substeps. A new runtime starts fame at 0; Run/chapter/battle cleanup does not replace the instance. Saving to disk is a separate integration.

Any IBufferQuery fake/decorator must forward the new FameReadSink member. The affected ArtifactEditingServiceTests query and ChapterResetSpy were updated with the same registered sink. Do not fabricate a second fame store in a decorator or reset it from Run initialization.

## 2. Transaction boundaries

Core `Finish` resolves clear applicability and step kill gold once, then emits `CombatRewardSettlement`. `CombatExperienceSettlement` selects the recipient union, calculates kill/clear XP splits, applies positive XP once and emits finalized `CharacterRewardReceipt` rows and thresholds. Pending XP remains kill-only; owned progression changes once on victory. `CombatRewardFacts` remains the battle ledger/cohort source. `CombatRewardChange` retains expected/final gold/fame. The settlement is immutable command-scoped calculation evidence, not another store or a View grant API.

Composition checked-sums step kill gold and retains the terminal XP rows/clear award. It preserves original balance expectations and final balances without recounting pending XP. `WithCompletion` and movement wrapping preserve the same settlement.

Before any install, `BattleTransactions` prepares character, soul, item, command/result, completion, gold and fame values. It compares current balances to input, award deltas to canonical settlement and cumulative kill ledger, receipt fields/order to canonical rows, and rows to actual owned before/after changes and matching Applied events. `BattleCompletionTransaction` checks the selected node's exact Depth/WidthIndex. Any failure leaves participating stores unchanged. Installation contains no calculation or persistence callback.

`BattleRewardReceiptCalculator.Create` is used only by the existing publisher to package the final ledger/cohort, canonical clear values/rows and completed-node coordinates. Neither it nor Buffer selects XP recipients, reads numerical clear policy, or calls `Gain`/`Required`. Core is the trusted gameplay calculator: Buffer proves proposal/store consistency, rather than independently proving gameplay formulas for a coherently forged proposal. Core policy tests and transaction tampering tests cover those separate obligations. Direct `CompleteBattle` cannot install a non-null reward receipt outside the atomic command transaction. Legacy nonreward completion and abandonment constructors remain compatible.

### Input preservation and composition ownership

`CombatCommandInput` owns one source-copy constructor and three required-argument operations: `WithIntent`, `WithWorld`, and `WithPersistentFacts`. All seven derived-input paths use them. Explicit null clears optional path/context/balance; existing intent-wrapper preservation and AI intentional resets remain at those callers. Modified collections are defensively copied, and unchanged immutable snapshots can be shared. The only fresh production construction remains `CombatInputAssembler`.

The upper `FrameworkRuntimeFactory` still connects Buffer/Global/InGame/LifeCycle groups. Combat-specific services/collaborators stay in the existing `CombatSystemFactory`; storage registration stays in `BufferSystemFactory`. B1-B3 add no Factory/content composition, runtime package field, service/key, module or assembly reference. Existing Artifact editing composition inside the InGame Core factory predates this change and was not migrated or audited as a separate feature. The boundary follow-up does not certify every unrelated factory as refactored.

## 3. Consumers and lifetime

- CombatTransitionPublisher binds the stage-progress reader, resolves the planner's accepted chapter/node/stage occurrence and attaches the immutable reward receipt with WithCompletion. Reused StageKeys remain distinct by node and BattleInstanceId.
- CombatStateBuffer retains Completion through EndBattle and clears it at existing new-battle/Run boundaries. Duplicate commands remain no-ops; stale automatic transitions reject. Reads and confirmation do not repay.
- Existing CombatResult sequence/presentation acknowledgement owns refresh. Existing CombatPlan completion presentation and exit sequence owns confirmation/cleanup. There is no additional scheduler, event bus, claim command or scene-transition API.
- InitialDrawPlan.DefaultStartingHandSize and operational loadout are 4 (main + up to three subs), with manual main placement. Explicit custom values retain their existing semantics.

## 4. Verification and outstanding work

The original plan and boundary plan execution records list exact commands/counts, known baseline failures and native-run limits. B3 completed the full Core assembly (3,343/3,343) and focused Framework selection (192/192), each with complete XML and Unity exit 0. B1 baseline identities retained their passing results; the full Framework suite and manual View playthrough were not rerun. R5 uses actual registered Core/Buffer services with asset-backed reward/progression data, plus a controlled test-only stage shape/stats to isolate rewards. This is not a claim of manual scene/UI acceptance. Full Framework failures are reported rather than silently repaired outside scope.

The developer Fixture source remains excluded by explicit user decision; use Database mode. Minecart key 25 has a saved configured asset but is not registered in the operational Unit table; the variation test imports it explicitly into the test definition and does not activate production content. Heavy is registered in Core/InGame (entity 26, targeting 9), but live spawn and View assets remain content work. Critical-hit support and disk-save persistence remain separate. Commit/push and external delivery are user-owned.
