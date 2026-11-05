# Run Gold Facilities and Trait Ranks Implementation Plan

> **For agentic workers:** Use `superpowers:executing-plans` for inline execution or `superpowers:subagent-driven-development` if the owner chooses delegated execution. Checkboxes track implementation evidence.

**Goal** — Deliver three optional Run gold purchases and a common Core path from acquired trait to trained combat effect.

**Architecture** — Category SOs own acquisition pools and schedules. The owned character stores earned growth, the trait definition owns effect tuning, and the existing Core services and Buffer transactions settle Run purchases. Section 4 of the design is the tuning source for all 17 traits.

**Tech stack** — Unity 6000.0.73f1, C#, ScriptableObjects, Framework services and EditMode NUnit.

**Spec** — [Run gold facilities and trait ranks](../specs/2026-11-01-run-gold-facilities-and-trait-ranks-core-design.md). Read it alongside each phase. Plan status: **Phases 0 through F implementation/verification complete. Production training activation remains pending approved prices; View/manual acceptance is downstream**.

## Global constraints

- Implement only Core, required Core-facing Buffer contracts, InGame SOs and their tests. Preserve existing Framework registration, Job dispatch, snapshots and publication ownership. Prepare the View contract handoff through the final phase.
- Use English for implementation documents and Korean for new code comments. Preserve Unity GUIDs, include matching metadata for new scripts/assets, and keep existing serialized enum values and public callers compatible.
- Execute one approved phase at a time. End each phase with changed paths, test evidence, deviations and outstanding failures. Stop for review before the next phase. The owner handles Git commits and pushes.
- Never guess production training prices. Fully exercise the feature with fixture prices. Keep production training activation pending until both prices are supplied. No day/night exceptions.

## Review focus

| Input or failure | Owning check |
|---|---|
| Old Cannon acquired Taunt before its pool changed, or completed an old Enhance step | A1/B1 preserve the trait and the new level-3 choice without opening illegal offers |
| A native Akimbo trait differs from the catalog or a specialized Main inherits a duplicate default | B2 resolves the same effective values for roster and battle without replacing unrelated parameters |
| Two swap shields overlap, one is consumed and a move undo is requested | C1 checks origin, remaining budget, expiry and receipt dependencies |
| Full-health Main buys max HP, or the Main's soul modifier changes before publication | D1 verifies exact deltas, price and stale rejection |
| Two identical off-deck units, stale/racing purchases or zero affordable offers | E1/E2 distinguish instances and keep gold, roster and visit atomic |

## Execution map and verification

The file map below uses paths relative to the repository. Within the tasks, a filename refers to its named owner here or to the uniquely named file under that owner. Additional changes to existing callers must follow the actual call graph.

| Prefix | Directory and responsibility |
|---|---|
| Calc | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator` — immutable contracts, domain rules and projections |
| Services | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem` — character, recovery, shop and chapter entry |
| Buffer | `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems` — Current, Character, ShopView, Recovery and Core transactions |
| Data | `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table` — typed SO inputs and immutable data |
| Assets | `Assets/ProjectL/DataBaseSO/InGameGroup` — active production content |
| Tests | `Assets/Tests/EditMode` — Core tests, with integration fixtures under `CoreIntegration/` |

| Phase | Deliverable | Dependency |
|---|---|---|
| 0 | Current baseline and compatibility inventory | Before edits |
| A | Level-2/3 Acquire, variable choices and production category pools | 0 |
| B | Rank definitions, ownership, compatibility and effective resolution | A |
| C | Ranked combat effects and ranked production content | B |
| D | Rest Site and Mercenary Office | 0, independent of rank work |
| E | Training entry, selection, atomic purchase and exit | B/C, existing shop path |
| F | Production checks, full regressions and downstream handoff | A–E |

Use this checkout's existing local runner from the repository root. Run each Unity invocation via the approved AppData path, one Editor at a time. Use a fresh run label for every attempt. The following examples are execution instructions.

```powershell
$runner = '.superpowers/position-swap-recon-undo/run-tests.ps1'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmssfff'
& $runner -Run "gold-growth-$stamp" -Assembly ProjectL.Tests.Core -Filter ProjectL.Tests.Core.TraitGrowthOfferTests
& $runner -Run "gold-assets-$stamp" -Assembly ProjectL.Tests.CoreIntegration -Filter ProjectL.Tests.CoreIntegration.DataBaseAssetTests
& $runner -Run "gold-core-$stamp" -Assembly ProjectL.Tests.Core
& $runner -Run "gold-integration-$stamp" -Assembly ProjectL.Tests.CoreIntegration
```

Use the same commands with each task's fixture name and a fresh label. For every behavioral task, first add the assertion to the closest fixture, run it against the old behavior and record the expected failure. After editing, rerun the fixture including existing cases. A pass requires a fresh XML with the intended cases, positive count, zero failures and zero skips. Separate pre-existing failures by name, message and baseline evidence.

### Phase 0 — Establish the baseline

- [x] Record branch, HEAD, local modifications and Unity version. Run both full assemblies using the runner above. Record XML/log locations, totals, skipped cases and failures. Verify the current status of any StageKey 2101 failure from the actual run.
- [x] Trace the active catalog to category pools, ranked trait definitions, the recovery event and ordinary/enhancement nodes. Record existing category assignments, pool GUIDs, step IDs, serialized legacy Enhance meanings and native/default overrides. Use current assets and prior shipped schemas where old IDs require disambiguation. Inventory the roster/history construction and restore entry points. Any ambiguous old ID is a review item before rewriting history.
- [x] Compare design sections 2 and 4 to production. Record missing definitions and table registrations separately from missing effect code. Preserve the current Knockback memberships pending reassignment. Record training prices as open and identify the production activation step affected. Report and stop.

### Phase A — Acquisition schedules and category pools

**A1. Keep old ownership valid and support one to three choices.** Owners: Calc `Application/Roster/TraitGrowthRules.cs`, `TraitGrowthCatalogValidator.cs`, `TraitGrowthOfferSampler.cs`, `Contracts/Roster/TraitGrowthOffer.cs`, `Application/Enhancements/OwnedUnitSpecResolver.cs` and growth history contracts.

- [x] Extend `TraitGrowthOfferTests`, `TraitGrowthCatalogTests` and `CoreIntegration/TraitGrowthChoiceServiceTests`. For eligible counts 0, 1, 2, 3 and 4 expect respectively an error, one, two, three and three distinct keys. Assert one visible option still requires selection, with stable IDs/order across repeated reads. A reachable zero in step 2 must report unit, category, step and prior path at DB initialization.
- [x] Set Acquire's minimum to one and maximum to three in the validator, sampler and published offer. Keep the deterministic seed, sorted output, pending step order and existing Enhance behavior. Test the real Buffer offer and selection command.
- [x] Preserve old history before changing any pool. Today `TraitGrowthSelection` stores only step ID and key, and replay matches current positional steps and pool membership. Normalize old histories by proven schedule/step meaning at the owning boundary. Distinguish a consumed Acquire from a historical Enhance; validate new selections against the current pool. Fixture: Cannon retains a formerly legal Taunt, later cannot draw Taunt, and a historic Enhance does not consume the new level-3 Acquire. Reject unknown or impossible history.

**A2. Apply the production content change.** Owners: Data `TraitGrowth/Item/CategoryTraitProgressionDataSO.cs`, Assets `TraitGrowth/Item/CategoryTraitProgression_*.asset`, `TraitGrowth/TraitGrowthDefinitionTable.asset` and the referenced definition assets.

- [x] In `CoreIntegration/TraitGrowthAuthoringTests` and `DataBaseAssetTests`, assert the design's common and exclusive membership matrix, including Main's deduplicated specialization union and previously acquired/native/default exclusions. Test a level jump with both choices pending, and that an unchosen eligible trait can appear again.
- [x] Keep step ID 10 at unit level 2. Append a distinct, verified unused step ID at unit level 3, kind Acquire, for all four categories. Populate the existing pools using design section 2. Register missing normal definitions, including Position Swap and Recon Squad, using existing trait keys and typed authoring. Keep uncategorized entries in their current eligible pools. Retain native/enemy-only behavior.
- [x] Run A1/A2 fixtures and both specialization growth paths; check the production DB loads and each category has at least one reachable candidate at each step. Report IDs, membership differences and grandfathered cases. Stop.

### Phase B — Rank state and authored effect resolution

**B1. Separate paid increments from unit-level selections.** Owners: Calc `Contracts/Enhancements/OwnedTraitGrowth.cs`, `TraitGrowthSelection.cs`, `Contracts/Roster/OwnedCharacterRecord.cs`, and `Application/Enhancements/OwnedUnitSpecResolver.cs`.

- [x] Extend `OwnedTraitGrowthTests` and `CoreIntegration/TraitGrowthStatSettlementTests`. Assert a paid increment preserves selected step IDs, applies to exactly one owned instance and survives record rebuilds, battles and reserve return. A new Run starts with no paid increments. Missing ownership cannot be created by a rank record.
- [x] Extend the immutable growth value with purchased increments by trait key. Keep old constructors valid with empty purchases. Include purchases in equality, empty checks and every append/copy path. Preserve Hunter attack, specialization and instance identity when replacing a record. Normalize a valid historic Enhance on a trainable trait once to effective rank 2, allowing one more purchase; retain the legacy effect for other valid Enhance history. Never debit gold during normalization.
- [x] Publish effective rank on the existing owned-character projection. Use one rank resolution path for future training quotes, roster stats and combat setup. Test same-key duplicate ownership and rank cap, and trace all construction paths that might otherwise discard purchases.

**B2. Extend the trait definition and apply rank values for every source.** Owners: Data `TraitGrowth/Item/TraitGrowthDefinitionDataSO.cs` and `TraitGrowthDefinitionData.cs`, Services `CharacterSystem/TraitGrowthDefinitionAdapter.cs`, Calc `Contracts/Enhancements/TraitGrowthDefinition.cs`, and B1's resolver.

- [x] Add authoring tests for no training ranks, complete ranks 2/3, missing halves, invalid magnitudes and key mismatch. An old `enhanced` field alone must not expose training. Keep SO references at the authoring boundary and emit immutable typed data.
- [x] Provide a complete ranked definition with one editable final value per rank-controlled field. Derive trainability from the complete definition. Use the existing normal field for rank 1; keep other geometry, durations and targeting fields at their existing owner unless the design explicitly changes them. Do not store runtime formulas or duplicate rank-1 values.
- [x] In `CoreIntegration/MainSpecializationBattleTests` and `TraitGrowthAuthoringTests`, check native Akimbo Morale Boost is 10% and Clean Sweep expires at turn end even without purchased growth. Use fixture definitions first. Verify native/default/acquired projections, including the no-selections fast path, preserve unrelated native parameters. Roster query, preview and battle must agree. Stop after B1/B2 regressions.

### Phase C — Combat behavior and production ranks

**C1. Position Swap shield transaction and expiry.** Owners: Calc `Application/Combat/MovementProjection.cs`, `MovementEffectUndo.cs`, `MovementUndoPolicy.cs`, `CombatTurnCalculator.cs`, `Contracts/Combat/MovementEffectReceipt.cs`, `TraitShieldGrant.cs`, and the Position Swap spec/authoring. Use `ShieldStatusRules` for absorption and origin-specific restoration.

- [x] Add cases to `PositionSwapMovementTests`, `PositionSwapUndoTests` and `CoreIntegration/PositionSwapPublicationTests`. Assert 0/10/18 per participant, two rank-2 grants to one unit total 20, and 7 damage leaves 13. Expire only that remainder at the granting side's next turn start. Mirror for enemy use; preserve other shields.
- [x] Grant only on a successful paired swap. Capture prior/final shield state for each touched recipient and origin in the existing receipt. Restore both positions and the precise prior balances on legal undo, including a second swap over a first grant. Exercise consumed/stale receipts, redo restrictions, reserve cleanup, rejected swap and failed publication with no partial shields or movement.

**C2. Taunt, Compaction and Recon.** Owners: Calc `Application/Combat/EnemyAi/TauntTargeting.cs`, `TraitPresenceReconciler.cs`, incoming damage evaluation, `TraitTrailCalculator.cs`, `Contracts/Combat/TraitTrailFact.cs` and existing typed trait rules/specs.

- [x] Extend `TauntTraitTests` and `CoreIntegration/TauntPublicationTests`. For an attacker taunted by A at rank 2 and B at rank 3, use a 100-damage area hit with no other modifiers; expect 90 on A, 85 on B and 100 on C. Contribute once per qualifying recipient before shield absorption using the same source identity and incoming reduction path as preview. Verify forced target/pursuit and release on lost eligibility.
- [x] Extend `TraitTrailTests` and `CoreIntegration/TraitTrailPublicationTests`. For Ground Compaction test start-on-trail bonuses 1/2/3, actual path only, either side, no expiry at creation-side end or intervening opposing-side end, expiry at the end of the originator's next turn. Use the existing trail state and turn boundary; preserve other trail expiry and undo behavior.
- [x] Extend `ReconSquadMovementTests`, `ReconSquadUndoTests` and `CoreIntegration/ReconSquadPublicationTests`. Assert 10/15/20% on the start/reached-cell orthogonal footprint, once per enemy per move. Clean undo restores exposure, applied damage blocks undo, waiting-area return clears it, and re-entry does not reapply it.

**C3. Publish the full tuning set.** Owners: Assets `TraitGrowth/Item/TraitGrowth_*.asset`, the active definition table and existing typed attack, damage, healing, aura and Hunter rules. Use design section 4 as the expected-value table.

- [x] Add a parameterized `DataBaseAssetTests` case that reads all **17 keys × 3 ranks** and checks every numeric column against the design, independently of the code generating them. Assert only those 17 have a complete training definition. For a Vanguard fixture, changing rank 1 to 2 leaves ranks 2/3 at 4/9.
- [x] Wire the rank data for all 17 and verify the existing owners rather than forking combat. Use `TraitAttackNotificationTests` / `TraitKillRewardTests` for Vanguard, Clean Sweep, Call of Blood, Morale Boost and Hunter (area kills, rounding, seeded ties, three awards per stage). Use `TraitShieldTests` / `TraitDamageReductionTests` for Solo Action, Cohesion and Entrenchment (no shield refill from repeated adjacency changes, eight-cell geometry, both turn ends).
- [x] Cover Stationary Fire (unmoved attack and additional damage), Armor Breaker (successive multiplicative post-hit stacks), Finishing Shot (strict pre-hit threshold and existing execution targets), Tracking Shot (inclusive HP threshold) and Focused Bombardment (exactly one enemy) in `TraitMovementAttackTests`, `ArmorBreakerTraitTests`, `CannonThresholdTraitTests` and `CannonBombardmentTests`. Re-run C1/C2 with production rank data, Core and CoreIntegration. Stop with the combat results and any baseline differences.

### Phase D — Rest Site and Mercenary Office

**D1. Implement both rest purchases through recovery.** Owners: Data `Recovery/Item/RecoveryOptionDataSO.cs` and its immutable data, Calc `RecoveryOptionSpec.cs`, `RecoveryOptionEvaluator.cs`, `CharacterRecoveryCalculator.cs`, `CharacterRecoveryDecision.cs`; Services `RecoverySystem/RecoveryDefinitionAdapter.cs`, `RecoveryStageService.cs`; Buffer `Core/StageServiceTransactions.cs` and recovery snapshots/receipts.

- [x] Extend `RecoveryOptionEvaluatorTests`, `CharacterRecoveryRuleTests` and recovery operation/receipt tests. At 40/60 expect 58/60 for 5 gold or 40/70 for 10. At 60/60 reject heal and allow 60/70. Verify flooring, clamping, insufficient funds, and unchanged soul-derived bonuses.
- [x] Keep the existing heal option and calculation. Add a distinct max-HP growth effect and receipt delta; the old decision requires positive healing and unchanged maximum, so validate deltas by effect kind. Reuse the persistent Main record and recovery prepare/install path. Preserve visit allowance and Run spending. Test stale state/revision and duplicate commands leave all state unchanged.
- [x] Edit the active `Assets/Stage/Event/Event_Recovery.asset` references and `Assets/Recovery/` table/options (paths use the Assets prefix above). Keep the existing 5-gold/30% offer and add an independently configurable 10-gold/+10 offer. Stop offering flat/full heals here. Validate the active catalog GUIDs via `DataBaseAssetTests` and exercise a real recovery visit through entry, selection and completion.

**D2. Narrow the production ordinary shop to units.** Owners: Assets `Stage/Shop/Shop_Default.asset`, Services `ShopSystem/ShopDefinitionAdapter.cs` and normal shop transaction contracts. Prefer a data-only edit when the existing path already accepts unit-only candidates.

- [x] Exercise `ShopOfferPlannerTests`, `ShopPurchaseRuleTests`, `CoreIntegration/SubTraitShopRetirementTests` and production catalog coverage. Enter using the production definition, assert every offer is a recruit, buy once and reject a second hire with no additional debit/roster change.
- [x] Remove ordinary office item references from production shopping data. Keep unit eligibility, recruit price, receipt and ordinary offer cap. Preserve valid general item contracts, other item sources and historical receipt readability. Record the new facility display name for the View owner. Stop.

### Phase E — Training visit and purchase

**E1. Present direct choices and support exit.** Owners: Data `Stage/Enhancement/EnhancementStageData.cs` and `EnhancementStageDetail.cs`; Services `ChapterProgressionCoordinator.cs`, `StageSystem/StageGeneratorSystem.cs`, `ShopSystem/ShopService.cs`; Calc `ShopVisitKind.cs`, shop snapshot contracts; Buffer `Current/CurrentShopData.cs`.

- [x] Extend `CoreIntegration/SubTraitShopRetirementTests` or add adjacent `TrainingGroundVisitTests.cs`. With fixture prices 10/20, publish a visit including Main and off-deck Subs, distinguish duplicate unit instances, and allow >3 eligible pairs. Exclude absent/unlisted/rank-3 traits. With no eligible or affordable purchase, enter and complete the node; also allow completion with an affordable choice.
- [x] Provide a distinguishable training stage definition and visit kind with independent validated prices. Route new definitions through normal chapter prepare/confirm/enter and `CompleteShop`; legacy enhancement snapshots/nodes retain their rejection. Keep original enum values and ordinary offer-ID guards. Publish an immutable collection of training choices separate from capped ordinary offers: instance ID, trait key, current/next rank, price and availability. Build from the existing owned roster, catalog and B's effective resolution.

**E2. Make one atomic purchase.** Owners: Services `ShopSystem/ShopService.cs`, `ShopCommandExecutor.cs`; Buffer `ShopView/ShopViewCommand.cs`, `Current/ShopPurchasePreparation.cs`, `Core/ShopTransactions.cs`; Calc `Application/Shop/ShopPurchaseRules.cs` and purchase/receipt contracts.

- [x] Add a training request carrying `ShopVisitContext`, owned instance ID, trait key and expected rank. Proposed service signature: `ShopPurchaseResult PurchaseTraining(ShopVisitContext context, CharacterInstanceId ownedId, TraitKey traitKey, int expectedRank)`. Match it in `ShopViewCommandData` and dispatch through the existing shop Job. Keep `Purchase(ShopVisitContext, ShopOfferId)` for existing callers. Resolve actual cost from active definition, never from UI input. Informational selection performs no settlement.
- [x] Add `CoreIntegration/TrainingGroundPurchaseTests.cs` for a successful off-deck 1→2 purchase, Main 2→3 on a separate visit, two identical units, repeated/stale commands, changed balance/roster/visit, forged target or expected rank, unowned trait, rank cap and unaffordable purchase. Assert currency, Run spending, receipt, roster revision, chosen rank, untouched sibling, level-up history and visit consumption together.
- [x] Verify expected balance, current roster revision, effective ownership/rank and unspent visit again at publication. Prepare all changes before installing any of them through existing guards. Issue one receipt only on success. Reject retired legacy enhancement offers at service and direct transaction boundaries. Run E1/E2 and ordinary purchase/retirement regressions.
- [ ] Open the production Training Ground only after balance values are supplied and asset validation passes with `price1to2 > 0`, `price2to3 > price1to2`. Until then report production activation as pending and keep fixture-backed implementation and verification independently reviewable. Stop.

### Phase F — Cross-feature acceptance and handoff

- [x] Run a CoreIntegration journey through Run ownership, levels 2 and 3, a rest purchase, unit recruitment, off-deck training, leaving a second training visit without buying, and the next battle. Assert independent step history/rank, Run gold, character identity, effective stats/projection and fresh Run reset.
- [x] Run production `DataBaseAssetTests`, then both full assemblies with fresh XML and logs. Compare to Phase 0. Report baseline issues by exact case. Report any unconfirmed production prices or gated node separately from fixture results.
- [x] Prepare an English Core-to-View handoff under `docs/superpowers/handoffs/` with actual public member names, command inputs, completion/failure results, refresh boundaries, rank previews, rest option kinds, variable growth choice count and skip semantics. Identify display names, translated copy, screens/scenes and manual acceptance for the View owner. Record phase evidence and stop for review.

## Execution record — 2026-11-01

Phase 0 inventory and baseline: [baseline report](../handoffs/2026-11-01-run-gold-phase-0-baseline.md). Both full assemblies ran before edits. Four baseline failures are recorded there. The owner explicitly authorized Phase 0 and A together; continue to A and stop before B.

### Earlier checkpoint — Phase A1 complete; Phase A2 decision pending (superseded below)

Implemented variable Acquire offers (1–3 distinct sorted keys), deterministic sampling and mandatory explicit selection. Added the prior-schema contract on category SO/Data/Core and one read-only history interpreter. Existing constructors remain valid. Existing selections resolve by proven original step meaning; new commands still validate against the live pool. Legacy Enhance does not consume a new Acquire. Specialized Main keeps both pool sources, including one-sided edits. Catalog validation checks current continuations after legitimate historical prefixes; the roster boundary rejects impossible reconstructed attack profiles before publication. Buffer ownership, transactions, Factory registration, Job dispatch, native tuning and enum values are preserved.

Verification: focused Core **66/66** and the relevant integration cases **179/179** passed (the latter selected from the final full XML). Full Core **4544/4545**, full CoreIntegration **661/664**, zero skips in both. The four failures are identical to the Phase 0 baseline by name and message. Fresh evidence is under `.superpowers/position-swap-recon-undo/gold-a1-review-core-green-*`, `gold-a1-core-regression-final-*` and `gold-a1-integration-final-*` (XML/log/exit files). A fresh reviewer found four compatibility gaps; each was reproduced RED, fixed and verified GREEN. No deferred review findings.

A2 cannot apply the specified Main pool unchanged: production Akimbo is Main and has all seven common traits natively. With common7 plus preserved Knockback, only Knockback is available at step 10 and step 30 has zero candidates. The current validator must reject that production configuration. The owner was asked to choose a native-Akimbo adjustment, retain/extend the previous Main pool, or introduce a specialization precondition. No answer has been received, so no policy was assumed. **No production pool, schedule, definition registration or native asset was edited.** Pending A2 tests are saved at `.superpowers/sdd/2026-11-01-run-gold-facilities-and-trait-ranks-implementation-plan/DataBaseAssetTests.phase-a2.pending.cs`, outside test compilation. Restore and finish these only after the decision; keep A2 unchecked.

The Unity test runs automatically changed the FMOD catalog's local bank folder and cleared a dynamic font atlas. Both initially clean files were backed up in this plan's workspace and restored to HEAD. No commit or push was performed. No B–F work was started.

### Phase A completed — owner authorized direct SO edits

The owner approved changing shared SO assets and will inform View workers. A2 is complete in this checkout; no separate worktree or runtime development mode was added. Four category schedules use step10/level2 and step30/level3 Acquire. Pools: Main8, Heavy14, Rider12, Cannon13, with the approved common/exclusive matrix and unchanged Knockback eligibility. The 25 registered definitions include Position Swap440 and Recon Squad450. Prior still-supported pools are frozen from 4e0fc4a51^ (23/23/22/22) for history replay, not current offers. All existing category and unit asset GUIDs remain unchanged.

Akimbo's presentation-test traits were replaced with its proven pre-test KillChainAttack. Current HP5 and all other unit settings were retained. Its production Morale Boost/Clean Sweep test payload no longer exists; future B2 native-tuning coverage must use explicit fixtures for those keys. That owner-approved data cleanup supersedes the literal native-Akimbo example in the earlier B2 description; the general native/default/acquired rank resolution requirement is unchanged. View/PlayMode acceptance and training activation are not claimed.

Verification: A2 RED6/6 failed against old data; A2 acceptance GREEN188/188 with zero skips. Final full Core4544/4545 and CoreIntegration669/672, zero skips. Remaining failures are identical to baseline: Taunt_OnlyAnAppliedForcedDecisionActivates(True); CinemachineCameraOffset TypeLoadException; production specialized battle placement for Rider/Leap and Heavy/SteadfastWill. No StageKey2101 failure. No SerializeReference missing-entry error in the final integration log.

Evidence (under `.superpowers/position-swap-recon-undo/`):

- `gold-a2-acceptance-20261101-224649997.xml` (matching `.log` and `.exit.txt`).
- `gold-phase-a-core-final-20261101-224814142.xml` (matching `.log` and `.exit.txt`).
- `gold-phase-a-integration-final-20261101-224903235.xml` (matching `.log` and `.exit.txt`).

Independent A2 review verified exact historical GUID sequences, new GUID uniqueness and correct memberships. No Important/Critical findings. Deferred minor: production legacy-pool regression asserts counts and excluded new keys; an exact full historical key-set assertion could strengthen it. Current historical sets were independently compared with git and are correct.

Unity-generated FMOD local-path and dynamic-font-cache changes were backed up and restored. No commits or pushes. Stop before Phase B.

### Phase B execution record · 2026-11-01

B1. `OwnedTraitGrowth.Purchases` stores immutable per-key counts separately from ordered level choices. Growth copies, equality and the existing selection transaction preserve those counts. `OwnedUnitSpecResolver` joins purchases with native, category-default and acquired ownership. A valid previous Enhance contributes one rank for a trainable trait. Missing ownership, an impossible Enhance, and ranks above three are rejected. Future Enhance candidates also exclude capped traits. `OwnedCharacterStatsSnapshot.TraitRanks` reports the effective rank per owned key. The record copy keeps identity, level, experience, Hunter growth and specialization.

B2. The growth definition SO and its Inspector drawer expose optional typed rank 2 and 3 payloads. Both must match the normal key. Authors edit only the fields controlled by each rank. The normal spec supplies rank 1. The shared resolver overlays controlled fields and preserves source-specific parameters, including native Morale Boost duration. Clean Sweep takes its expiry from the shared normal definition. The existing specialization snapshot now compares the ranked fields as part of definition identity. Position Swap and Taunt expose their numeric fields at the input/spec boundary for Phase C.

Verification used the approved Unity 6000.0.73f1 runner. Three new Core and seven new CoreIntegration cases passed. Tests exercised Unity asset serialization, a valid and an impossible historic Enhance, distinct copies of one unit, native Akimbo fixture values, and a ranked default across night recall, battle teardown and Run reset. The stationary attack preview and actual hit both dealt 20 at rank 1 and 22 at rank 2. Fixtures seeded the purchased record before battle. The payment command belongs to Phase E.

| Final EditMode assembly | Passed / total | Skipped | Remaining failures |
|---|---:|---:|---|
| Core | 4547 / 4548 | 0 | Existing Taunt activation count (expected 1, actual 2) |
| CoreIntegration | 676 / 679 | 0 | Existing CinemachineCameraOffset type load and two specialized production stage placement cases (Rider/Leap and Heavy/SteadfastWill) |

Failure names and messages match the Phase A XMLs. Final evidence in `.superpowers/position-swap-recon-undo/`: `gold-b-core-final-20261101-230103933.xml` and `gold-b-int-verified-20261101-230158027.xml`, with matching logs. Independent review identified the impossible historical Enhance, an Enhance offer at the paid cap, and missing ranked preview coverage. The fixes and regression cases are included above. Production tuning and the combat triggers remain at the Phase C review boundary.

Focused reruns passed with zero skips: Core 89/89 in `gold-b-focused-core-20261101-230315492.xml` and CoreIntegration 77/77 in `gold-b-focused-int-20261101-230233196.xml`.

### Phase C execution record · 2026-11-01

C1. A successful Position Swap at ranks 1, 2 and 3 grants 0, 10 and 18 shield to each participant before arrival effects settle. The status stores the source and expiry side. The shared shield pipeline absorbs damage, and the next granting-side start removes the remainder. The existing per-origin movement receipt restores the exact prior balance on a legal paired undo. Publication verifies the two actual movements, recipients, authored amount and final recorded shield. A regression reproduced a fabricated third-party grant on both a swap and an ordinary move before the guard rejected both atomically.

Two rank-2 swaps on one recipient gave 20 shield. After 7 damage, 13 remained until expiry. Undoing the second swap preserved the first grant. Tests also covered a previously depleted grant from the same source, an unrelated shield, reserve cleanup and invalidation after consumption. The enemy case uses an explicit legal swap path and verifies enemy-side expiry. The existing AI pathfinder selects unoccupied destinations in ordinary pursuit.

C2. Taunt uses the damaging character and the recipient's own taunt area in the existing incoming damage calculation. An area hit for 100 dealt 90 and 85 to the two taunters and 100 to the bystander. A 95-point shield on the first holder retained 5. The ordered damage path records a holder's death and release before the next target's reduction. Ground Compaction now carries the creation turn on the actual path and expires at the end of the creator's next turn for production ranks. Dust Screen keeps its opposing-turn expiry. Recon uses the existing start/reached-cell scan, exposure, undo and reserve rules with the authored 10/15/20 percent values.

C3. The 17 growth definition assets now carry their two higher ranks. The production catalog tests check all 51 effective specs, every numeric column and the exact trainable key set. Combat tests cover area kill rounding, adjacency including diagonals, shield consumption and resets, stationary attacks, execution thresholds, armor reduction, mark rewards and single-enemy bombardment. The existing category pools and service registrations supply these definitions.

| Final EditMode assembly | Passed / total | Skipped | Failure comparison with Phase B |
|---|---:|---:|---|
| Core | 4578 / 4579 | 0 | Same Taunt activation count, expected 1 and observed 2 |
| CoreIntegration | 738 / 741 | 0 | Same CinemachineCameraOffset type load and Rider/Leap and Heavy/SteadfastWill production placement failures |

Full XML evidence in `.superpowers/position-swap-recon-undo/` is `gold-c-core-verified-20261101-232310880.xml` and `gold-c-int-final-20261101-232021098.xml`. Failure names and messages were compared with the Phase B XMLs. Independent review found the third-party grant and the order of damage events on taunter death. Both were reproduced and fixed. Phase C stops here for review.

Focused EditMode reruns passed with zero skips. Core passed 183/183 in `gold-c-focused-core-20261101-232120114.xml` and CoreIntegration passed 103/103 in `gold-c-focused-int-20261101-232221325.xml`.

### Phase D execution record · 2026-11-02

D1/D2 completed. [Phase D result and View/content follow-up](../handoffs/2026-11-02-run-gold-phase-d-result.md) records the effect contracts, exact health/payment outcomes, active SO references, atomic publication guards, changed paths and remaining baseline failures. Rest now offers option2 at5 gold/30% and new option4 at10 gold/+10 persistent max HP, sharing the existing visit allowance. Ordinary production shopping data is unit-only; its ShotGun candidate,600-gold price and eligibility remain unchanged.

Final full Core4598/4599 and CoreIntegration763/766, zero skips. All four failures match Phase C by name/message. Relevant Core264/264 and focused Phase D integration31/31 passed. An independent reviewer found the new-visit-at-cap projection issue; three failing service cases reproduced it before the fix, and the final regressions verified it. No deferred D review findings. Prior-phase modified paths were preserved, and unrelated Unity-generated FMOD/font changes were restored. No commits or pushes. E/F remain unstarted; training prices are still open.

### Phase E execution record · 2026-11-02

E1/E2 implementation is complete with fixture prices 10/20. Production activation awaits the two balance prices. Existing enhancement assets load in legacy mode. Explicitly enabling training requires positive, increasing prices. The existing enhancement distribution, chapter preparation and shop entry paths recognize the new training definition.

The active visit publishes `CurrentShopData.TrainingChoices` independently of the three ordinary offer slots. Each immutable choice carries `OwnedId`, `TraitKey`, `CurrentRank`, `NextRank`, `Price` and `CanPurchase`. Main and every owned Sub are considered, including off-deck duplicate instances. Missing traits, unavailable or unranked definitions and rank 3 are excluded. Empty or unaffordable lists and affordable unselected lists all permit `CompleteShop`.

The public service entry is `PurchaseTraining(context, ownedId, traitKey, expectedRank)`. `ShopViewCommandData.PurchaseTraining` reaches it through the existing shop executor and Job. Successful commands retain the existing purchase-result-then-completion sequence and request-number replay guard. The financial receipt is `CurrentShopData.TrainingReceipt` and `ShopPurchaseResult.Training`, containing instance, trait, previous/final rank and price.

The visit captures its immutable definition and roster revision. `TrainingRules` uses the same effective resolver as roster and battle setup. `ShopTransactions.PublishTraining` checks visit identity, current selection, unspent allowance, revision, balance, ownership and effective rank before preparing the roster, currency and visit result. The existing CharacterBuffer and CurrencyBuffer install those prepared values. Tests cover off-deck 1→2, Main 2→3 at a later visit, native/default/acquired and legitimate legacy Enhance ownership, untouched duplicate siblings, preserved selection history, stale wallet/roster/visit and spending overflow. Both new APIs reject a restored retired enhancement visit.

Independent review found that re-reading changed SO prices could block exit from a valid active visit. Two failing cases reproduced it. Prices and definitions now remain fixed for that visit, matching ordinary fixed offers. Reauthoring applies on the next visit. Completion and purchase at the published price pass the regression. No deferred review findings remain. The supplied Unity runner used the approved AppData path, with one Editor at a time.

Relevant integration tests passed 57/57 with zero skips in `.superpowers/position-swap-recon-undo/gold-e-review-green-20261102-003519162.xml`. Full CoreIntegration passed 800/803 with zero skips in `gold-e-integration-final-20261102-003617523.xml`. Its three failures match Phase D by name and message, covering CinemachineCameraOffset and the Rider/Leap and Heavy/SteadfastWill production placement cases.

Change independence. Price edits now live in the training stage's two SO fields. Ownership and rank resolution remain in the shared Core resolver, and payment remains in the existing shop transaction. A later trait effect edit uses that trait's definition and combat owner. The necessary verification boundary for a purchase includes the target record, wallet, receipt and visit allowance together. Phase E stops for review with production activation pending.

Full Core passed 4598/4599 with zero skips in `gold-e-core-final-20261102-003723804.xml`. Its Taunt activation failure matches Phase D by name and message. Matching logs and exit files accompany all XML evidence.

### Phase F execution record · 2026-11-02

Cross-feature acceptance and the [English Core-to-View handoff](../handoffs/2026-11-02-run-gold-facilities-trait-ranks-core-view-handoff.md) are complete. The new journey verifies a real Run, actual growth/receipt commands at fixture-reached levels2/3, rest growth, recruitment, off-deck training, affordable skip, battle projection and fresh-Run reset. It checks the published CharacterStats request/Tick/read boundary used by View. Only test support changed: TraitGrowthTestRuntime accepts an optional null-default deck-timing dependency; all previous helper behavior is preserved. No product source, SO or View implementation changed in F.

Final production assets74/76, Core4598/4599 and CoreIntegration801/804, zero skips. XML/log/exit evidence: `gold-f-production-assets-20261102-005530177.xml`, `gold-f-core-final-20261102-005627363.xml`, `gold-f-integration-verified-20261102-005946553.xml` under `.superpowers/position-swap-recon-undo/`. The new journey passed in the final full assembly. Exact failed-case names and messages match Phase0 with zero differences: Taunt activation count, CinemachineCameraOffset type load and the Rider/Leap and Heavy/SteadfastWill production placement cases. The handoff lists each exact case. No new failing tests.

The final fresh review identified no Critical/Important/Minor findings in inspected contracts. Published-buffer refresh assertions were strengthened afterward and passed in the final full integration run. Training fixture prices10/20 do not authorize production balance; the existing unchecked production-activation item in E remains open. The handoff records current/next-rank and price preview, the absent generic next-rank numeric payload, rest effect kinds/receipts, variable mandatory growth offers, optional facility completion, concrete View adaptation points and manual acceptance. Generated FMOD/font changes are restored from the Phase F starting copies. No commit/push. Stop at the Phase F review checkpoint.

### Owner-approved follow-up — training-only SO · 2026-11-02

The owner approved removing the training mode and legacy candidate input after confirming that the active enhancement product table and stage candidate list are empty. This supersedes E’s legacy/current SO distinction and the previous handoff’s Training checkbox instructions. The asset/script GUIDs, stage1202 and existing Framework/shop/Buffer ownership remain. A null TrainingPrices now denotes unconfigured prices, not a selectable legacy SO mode. Both authored prices at zero preserve catalog loading and reject new entry through the existing guard; otherwise TrainingPrices requires first>0 and second>first. Restored legacy product snapshots still cannot spend. Existing active visits retain their definition and allow payment/completion after the author clears prices.

- [ ] Verify price-only SO authoring and removal of old fields; reject partial/invalid prices and keep unconfigured catalog loadable.
- [ ] Remove the legacy candidate-bearing stage detail contract, update its fixture callers, and preserve old purchase snapshot rejection.
- [ ] Verify active visit price pinning through price removal, production asset loading, distribution and cross-feature journey; compare fresh full regression failures to Phase F.
- [ ] Replace the View handoff instructions and record final evidence. No commit/push.
