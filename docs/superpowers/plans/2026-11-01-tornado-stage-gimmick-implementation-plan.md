# Tornado Stage Gimmick Implementation Plan

> For agentic workers, use `superpowers:executing-plans` for direct execution, or the delegated execution workflow if the user selects it. Execute only the requested Phase, report evidence and stop for review. The user owns commits and pushes.

**Goal.** Replace the legacy single-cell Tornado with a scheduled moving square and immediate combat contact, using the existing stage gimmick system and forced-arrival settlement.

**Architecture.** Prepare immutable storm definitions from existing stage authoring and install one grouped Tornado entry in the Combat Buffer. A feature-local calculator resolves travel and contact against a single movement projection. The common coordinator executes scheduled rules and existing battle transactions publish the result.

**Tech stack.** Unity 6000.0.73f1, C#, existing Core.Contracts, Core.Domain, Core.Application, Systems, NUnit EditMode, Core and CoreIntegration assemblies.

**Spec.** [Tornado Core design](../specs/2026-11-01-tornado-stage-gimmick-design.md). Planning baseline verified on `feature/Cheon/Core/NewCombatSytem`, HEAD `c3daac1fc`. Phases T1, T2 and T3 are complete. See the execution reports linked below.

## Global constraints

- Scope covers Core, its SO/Data input, Buffer contracts, query results, evidence and tests. Presentation consumes the published contract under separate ownership.
- Setup is per battle. Prepare and retain the starting center once. During deployment, placement and pre-entry enemy redeployment leave the Tornado inert. A permitted reader may see the stationary area. This is the planning choice for the user's suggested start flow.
- Use the existing ongoing-combat phase for eligibility. Successful combat entry enables future contacts. Activation alone does not throw an overlapping unit. Default warning and travel are due at the first and second completed-side ends. Preserve center, deadline and RNG through intervening deployments in the same battle.
- Use one eligibility check at the Tornado contact boundary. The shared completed-side boundary already gates scheduling. Keep `IsNight`, night counters, recall exceptions and separate activation flags out of the feature. Ordinary movement/placement and other terrain continue through their own contracts.
- Preserve authored visibility, forced-movement immunity, movement interruption, landing effects, event ordering, deterministic RNG, stale-input protection and reserve cleanup. A fatal scheduled enemy-end landing retains the previously recorded enemy end and its +1.
- Use English implementation documents, Korean code comments, existing namespaces and Unity metadata conventions. Run Unity on the AppData-authorized path. Preserve unrelated working-tree changes.

## Review focus

| Failure to catch | Proof |
|---|---|
| Deployment accidentally moves a unit or spends RNG on contact | T2.1 and T3.2 exercise accepted placement, pre-entry redeployment, rejected entry and successful entry with existing overlap. |
| Several triggers or multiple storm instances cause repeated throws | T2.2 tests nested A → B → A and overlap across sweep steps, plus a later independent re-entry. |
| A forced arrival bypasses contact or settles twice | T2.2 and T3.1 exercise shove, pull, tile displacement, swap and changed anchors after a throw. |
| Intermediate/fatal movement leaves stale position, RNG or turn facts | T2.3 and T3.2 verify an actual fatal landing, ordered evidence, state equality and transaction retries. |
| A stale preview or a hidden storm reveals or applies a wrong result | T3.1 and T3.2 compare observer-filtered predictions, actual outcomes and stale-context rejection. |

## File and contract conventions

Paths below use `Core` for `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator`, `Services` for its sibling `CombatSystem`, `DB` for `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table`, and `Tests` for `Assets/Tests/EditMode`. New names are proposals for implementation.

| Area | Main files and ownership |
|---|---|
| Immutable definitions and state | Add `Core/Contracts/Commands/StageGimmicks/TornadoGimmickSpec.cs` and `Core/Contracts/Combat/TornadoGimmickState.cs`. Update `Contracts/Common/StageGimmickKey.cs`, `Contracts/Combat/StageGimmickState.cs` and typed event/evidence files. |
| Input and preparation | `DB/Terrain/Item/TerrainDefinitionDataSO.cs`, `TerrainDefinitionData.cs`, `DB/Stage/Combat/CombatStageEnvironmentInput.cs`, `Services/BattleDefinitionAdapter.cs`, `Core/Application/Combat/BattlePreparationCalculator.cs`, `InitialTornadoPlacementCalculator.cs`. |
| Rule implementation | Add `Core/Application/Combat/StageGimmicks/TornadoGimmickRule.cs`. Rework `Core/Application/Combat/TornadoCalculator.cs` into the shared contact/landing calculation. Keep geometry in one pure helper used by both preparation and progression. |
| Execution and publication | `Core/Application/Combat/StageGimmicks/StageGimmickComposition.cs`, `CombatGimmickCalculator.cs`, `CombatTurnCalculator.cs`, `MovementProjection.cs`, `CommandEnvironmentState.cs`, `Core/Contracts/Combat/CombatChangeEvidenceReader.cs`, existing CombatStateBuffer and BattleTransactions validation. |
| Movement and query integration | `Core/Application/Combat/MovementEnvironmentCalculator.cs`, `AttackForcedMovement.cs`, `AttackKnockbackCalculator.cs`, `MovementContextCalculator.cs`, `Core/Contracts/Combat/MovementPreviewContext.cs`. Audit pull, swap, preview, undo and placement callers by actual entry behavior. |
| Existing tests to migrate | `Tests/TornadoEnvironmentTests.cs`, `Tests/InitialTornadoPlacementTests.cs`, `Tests/CoreIntegration/StageGimmickCompositionTests.cs`; retain current shared gimmick, status, terrain, undo, DB and publication suites. |

The key rule signature remains `IStageGimmickRule.TryAdvance(CombatCommandInput, StageGimmickSpec, StageGimmickId, StageGimmickBoundary?)`. Production binds the caller's validated movement-rule list to the Tornado rule. Contact receives the current movement projection, storm member/step and entrant identity. The throw guard lives for one sweep or independent entry chain. A unique relocation must identify its source and actual center-step index, including a distinguishable stationary-entry origin. Use the existing environmental RNG cursor in the projection throughout.

## T1. Prepared data and geometry

**Deliverable.** A validated, deterministic prepared Tornado contract and tested travel geometry. Keep the production switch to the new runtime path together in T3. The unit tests in this Phase may directly construct the new contracts.

### T1.1. Inventory and convert the authored inputs

- [x] Record current branch/HEAD, modified files and targeted pre-change test results. Inventory fixed and random placement records, source IDs, visibility, custom intervals and default asset references. Record authored centers that cannot fit the new square. Preserve hand-authored values pending an explicit content correction.
- [x] Add tests for fixed edge centers, an interior center with an explicit first horizontal direction, seeded edge starts, duplicate source IDs, multiple storms, invalid masks/board size, and the no-Tornado case. Verify the last consumes zero storm draws.
- [x] Build one immutable grouped definition per stage from the existing authoring route. Preserve per-source identity. Defaults are odd side length 3, diagonal distance 3, first/repeat 2 and warning lead 1. Validate positive durations/distance, full-square fit, and lead at most the shorter of first/repeat. Keep one authored source for each tuning value.
- [x] Implement preparation and state validation for the new types. Store center, horizontal sign, next deadline and locked direction when warned, with one grouped next deadline derived from its members. Deterministically initialize in source-ID order. If both horizontal edges coincide on a narrow valid board, use an explicit starting sign or a seeded side choice. A stationary area is valid even when diagonal travel is impossible.

### T1.2. Lock the travel geometry

- [x] Add geometry tests with center (2,2) and three positive diagonal steps. Assert actual centers (2,2), (3,3), (4,4), (5,5). Also cover all four diagonal signs, one-step shortening, zero-step travel and reflection on the following activation.
- [x] Implement full-square containment and stable direction selection. Walls, cliffs, structures and units may be covered by the storm. Select a feasible vertical sign when issuing the warning, then use that direction at travel. Preserve a one-center path on zero-step travel. Verify immediate next warning when lead equals repeat.
- [x] Run the targeted preparation/geometry tests including a non-default SO profile; inspect the diff and report the immutable contracts. **Stop after T1.**

## T2. Resolution, landing and ordered evidence

**Deliverable.** Exercise the actual Tornado rule against prepared combat facts. Validate every event used by that rule before connecting production callers.

### T2.1. Apply the combat-only contact boundary

- [x] Add a shared contact eligibility test for the existing ongoing combat phase. Exercise an overlapping unit during deployment and during combat on otherwise identical inputs. Assert inert deployment causes zero throws, zero contact draws and unchanged center/deadline.
- [x] In combat, enumerate unique living field characters touching the current center's footprint, ordered by entity ID. On an actual step into a stationary area, evaluate the entrant immediately. Keep preparation, phase entry, idle reads and restoration free of synthetic entry contacts.
- [x] Exercise a unit present in the footprint when combat starts. On default settings, activation alone leaves it in place. If still inside, it is eligible at the first scheduled sweep. If it moves into another covered cell first, the actual entry triggers immediately.

### T2.2. Share throws between sweeps and stationary entry

- [x] Test origin-only, intermediate-only, endpoint-only and stationary-entry victims. Include a landing inside the current footprint and another ahead of the storm. Assert one actual throw per storm/unit for that sweep and a new throw on a later independent entry.
- [x] Use current projected occupancy and the existing voluntary-standing terrain predicate for a supported one-cell unit. Exclude the unit's departure cell, walls, cliffs and occupied cells. Keep immunity and no-landing cases stationary. Sample in stable candidate order. Mark a successful throw before invoking nested landing effects.
- [x] Reuse forced movement recording, terrain entry, position effects, object/soul arrival and death/outcome settlement in their existing order. Recheck anchor, life and terminal state at the original caller before continuing a route or deferred landing effect. Maintain a per-resolution guard keyed by (storm source ID, entity ID).
- [x] Test multiple sources in ascending source-ID order, A → B → A termination, a freed/occupied landing candidate, immunity and no legal destination. Preserve the existing deferred support boundary for movable multi-cell victims.

### T2.3. Produce and validate evidence

- [x] Implement warning/travel events with source IDs, expected/final grouped facts, actual path prefixes and correlated relocations. One group change per boundary can describe several member actions. A stationary contact changes character/arrival facts and leaves storm scheduling facts equal.
- [x] Extend evidence reading and validation for legal source/state identity, boundary counts, ordered diagonal steps, at-most-once throws and terminal truncation. Keep the gameplay candidate calculation in Core. Verify stale proposals fail before installing HP, position, storm state or RNG.
- [x] Test fatal contact before the final center and verify no unperformed steps or later source effects are published. Run targeted rule, arrival and evidence tests. **Stop after T2.**

## T3. Atomic cutover and Core acceptance

**Deliverable.** The normal stage preparation, side-end, movement and query paths all use the new system. Turn progress, event evidence and final state install through the existing transaction.

### T3.1. Connect scheduling and actual entry paths together

- [x] Enable the grouped preparation path in `BattleDefinitionAdapter`, `BattlePreparationCalculator` and `StageGimmickComposition.Initialize`. Append the definition to preserve prior indices. Reserve authored source IDs and stop materializing the corresponding single-cell `TornadoSourceFact` terrain instances. Check initialization RNG consumption once in the combined Haboob/Tornado preparation order.
- [x] Bind the validated movement rules through the normal command/advance composition. Register scheduled Tornado first, followed by Railway, Haboob and Rockfall. Replace both legacy `TornadoCalculator.ResolveDue` calls together. An accepted side-end supplies the incremented boundary exactly once.
- [x] Wire the shared contact operation into ordinary movement, both forward swap arrivals, attack shove/pull/knockback and tile-forced steps at the actual reached cell. Preserve caller-specific thorn/fall/object order. In the existing `EnterCombat` path, enemy redeployment runs while the input still says Deployment. Let the common eligibility check keep those steps inert. Once the accepted transition says Combat, subsequent real entries can throw.
- [x] Update observer-filtered preview facts, stale-context matching, change evidence and undo barriers. Replace reliance on one-cell terrain events. Check hidden sources, multi-source attribution, a changed relevant blocker/area before commit, and read-only RNG. Undo is restoration and cannot trigger the storm.
- [x] Switch the named default `Assets/ProjectL/DataBaseSO/InGameGroup/Terrain/Item/Terrain_Tornado.asset` from 3/3 to 2/2 with the new defaults, preserving unrelated assets and custom tuning. Audit remaining legacy APIs by caller before retiring them. Document any downstream consumer still reading the legacy event shape in the Phase report.

### T3.2. Verify through the real Buffer and services

- [x] Add or extend real CoreIntegration tests using prepared stage input, normal command/advance services and Buffer readers. Verify one source per authored ID, one group update, observer-visible area, warning at 1, matching sweep at 2, and the next cycle at 3/4. Repeat with a custom SO profile.
- [x] Exercise deployment placement/repositioning, failed and successful combat entry, a later movement step into the footprint during combat, and an ordinary return to deployment in the same battle. Assert the prepared center/deadline persist and placement leaves storm contact inactive. This one phase-boundary regression covers the current lifecycle regardless of how the game later labels it.
- [x] Start at count 1 on enemy end. Verify one `EnemyPhaseEnded` with count 2 precedes Tornado. Force a lethal landing and assert one terminal outcome with count 2 retained and no later gimmick. Verify a non-terminal simultaneous boundary preserves Tornado → Railway → Haboob → Rockfall. Replace legacy tests expecting 3/6/9, one-cell contact, random storm respawn or an omitted fatal enemy end with the new contract assertions. Retain shared arrival and transaction regressions.
- [x] Exercise an actual attack knockback and a tile push into the storm mid-path. Verify the throw interrupts remaining displacement, the original endpoint does not settle twice, and actual random landing matches a fresh legal preview where the observer has the required facts. Verify retry/stale publication does not duplicate effects or alter RNG.
- [x] Run full Core and CoreIntegration EditMode suites, including `DataBaseAssetTests`. Report targeted and full results, pre-existing failures, and actual contract member names for Core consumers. **Stop after T3.**

## Verification procedure and completion record

For each behavioral task, first run the named test fixture in Unity Test Runner (EditMode) and verify the intended failure. Implement the smallest change, rerun the fixture, then run the affected shared regressions at the Phase gate. For T3 use assemblies `ProjectL.Tests.Core` and `ProjectL.Tests.CoreIntegration` and retain the result XML and Editor log. Use the AppData-authorized Unity 6000.0.73f1 route. Each report records changed files, before/after behavior, actual test counts, artifacts and remaining consumer dependencies. Run `git diff --check` and inspect the working tree at each stop. New C# files require their Unity metadata and Korean code comments.

| Phase | Execution state | Evidence |
|---|---|---|
| T1 · Data and geometry | Complete — stopped for review | New T1 30/30; Core 4491/4491; CoreIntegration 616/617 (unrelated camera type lookup); DB assets 9/9. [Execution report](../handoffs/2026-11-01-tornado-stage-gimmick-t1-report.md) |
| T2 · Core resolution | Complete — stopped for review | New T2 37/37; Core 4527/4527; CoreIntegration 621/622 (same camera type lookup); DB assets 9/9; review findings fixed. [Execution report](../handoffs/2026-11-01-tornado-stage-gimmick-t2-report.md) |
| T3 · Integration and verification | Complete — stopped for review | Runtime18/18; Core4521/4521; CoreIntegration628/629 (same camera type lookup); DB assets9/9; three Important review findings fixed. [Execution report](../handoffs/2026-11-01-tornado-stage-gimmick-t3-report.md) |
