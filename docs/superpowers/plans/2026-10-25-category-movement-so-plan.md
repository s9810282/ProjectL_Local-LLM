# Category Movement SO Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:executing-plans` for inline execution, or `superpowers:subagent-driven-development` if delegated execution is selected. Track steps with checkboxes. This document authorizes neither execution nor delegation.

**Goal:** Author category movement allowance and player movement cost in the existing category SO, with identical values in stats, previews and execution.

**Architecture:** Reuse the category data/spec/definition pipeline and existing movement snapshot fields. Core computes proposals; Buffer validates and installs state. Keep one authored source and no new service/table.

**Tech Stack:** Unity 6000.0.73f1, C#, existing ScriptableObject DB, NUnit EditMode.

**Spec:** [Approved design](../specs/2026-10-25-category-movement-so-design.md). The user authorized Tasks 1, 2 and 3 sequentially on 2026-10-25. Execution records below distinguish implementation evidence from remaining baseline failures.

## Global constraints

- One implementation Phase, three sequential tasks, then report and stop. No commit/push; preserve unrelated work and intentional deletions.
- Defaults `(allowance, cost)`: Main `(3,2)`, Rider `(4,2)`, Heavy `(3,2)`, Cannon `(2,2)`, Boss `(0,2)`. Both integers are nonnegative. Zero allowance disables voluntary movement; zero cost still consumes one action. Do not clamp costs above AP maximum.
- Preserve fixed per-action charging, diagonal L-path effects, behavior restrictions, forced movement, enemy unbudgeted execution, attack prices, AP 10, night length 8, and one move OR attack.
- No live SO reload, per-unit overrides, deferred-content reactivation, View behavior, scenes/prefabs, Framework/Factory/ServiceKey/Job/asmdef changes. Constructor-only adaptation of the existing development fixture is allowed below.
- English documents; Korean code comments; existing namespace/style conventions. Any new C# file requires a matching `.meta`; no new production file is expected.

## Review focus

| Risk | Owning check |
|---|---|
| Missing serialized fields silently produce zero range | Task 1: explicitly populate assets and validate exported defaults |
| Stats show SO values but runtime still uses constants | Task 2: non-default allowance 5 / cost 4 through preview and execution |
| Free/disabled movement grants an extra action or blocks forced movement | Task 2: zero-value and behavior/forced-movement checks |
| An editor change or default catalog changes an active battle | Task 3: immutable battle capture and configured-category routing |
| Existing failed fixtures hide publication regressions | Task 3: compare failure identities and execute a valid publication scenario |

## Paths and baseline

Paths use these repository-relative prefixes: `S = Assets/ProjectL/Script/Systems/`; `C = S/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/`; `CS = S/RuntimeSystem/InGameSystem/CoreSystem/`; `T = Assets/Tests/EditMode/`; `F = T/Framework/`.

- [x] Before execution, inspect HEAD/status and preserve concurrent changes. Capture current test failure identities with the verification command below. The reviewed `d4a6d6f` had Core 3,497 pass and selected Core–Buffer 28 pass / 99 fail; these are historical, not a fresh baseline.
- [x] Keep required regression coverage. Fix only directly affected or blocking fixtures within this Phase; unrelated fixture repair remains a separate finding. Never relax production guards or skip tests to obtain green results.

## Task 1: Carry authored movement values through category definitions

**Modify:** `S/DataBaseSystem/Data/InGame/Table/Category/Item/CategoryConfigDataSO.cs`, `CategoryConfigData.cs` in the same folder; `CS/CharacterSystem/UnitSpecAdapter.cs`; `C/Contracts/Commands/Categories/CategorySpec.cs`; `C/Application/BootstrapModels/CategoryRegistry.cs`; `C/Domain/Entities/CategoryDefinition.cs`, `UnitDefinition.cs`; `Assets/ProjectL/DataBaseSO/InGameGroup/Category/Item/CategoryConfig_Default.asset`.

**Interfaces:** Add get-only `int MovementAllowance` and `int MoveCost` to `CategorySpec` and `CategoryDefinition`. Use `CategorySpec(CategoryKey key, IReadOnlyList<CategoryRuleSpec> ruleSpecs, int movementAllowance, int moveCost, CategoryLevelGrowth levelGrowth = default)`; append the two required integers to the internal `CategoryDefinition` constructor. In `CategoryConfigData`, expose `<Category>MovementAllowance` / `<Category>MoveCost` for all five categories and require those ten constructor integers after `heavyDamageReductionPercent`, before existing optional growth/progression parameters. Avoid a new policy type or hidden constructor defaults.

- [x] Add failing contract/adapter checks in `T/CharacterCategoryContractTests.cs`, `CharacterCategoryBootstrapTests.cs`, `F/BattleDefinitionAdapterTests.cs`, and `DataBaseAssetTests.cs`: negative inputs reject; zero inputs survive; Rider `(5,4)` reaches its definition while Main stays `(3,2)`; asset exports match the default table. Confirm intended failures before completing production wiring.
- [x] Add five serialized movement pairs to the existing SO using one nested input struct with `movementAllowance` / `moveCost`. Export a fresh pure Data object and validate both Data and category contracts. Inspector constraints supplement runtime validation.
- [x] Pass the pairs through `ReadCategories` and `CategoryRegistry`. Make `UnitDefinition.MoveRange` / `MoveCost` derive from its `Category`; preserve attack-price behavior.
- [x] Explicitly serialize default values in every existing `CategoryConfigDataSO` asset; preserve GUIDs and unrelated settings. Keep `CategoryRegistry.CreateDefault` values explicit for non-DB validation/tests; verify live battle and owned-stat paths use supplied categories.
- [x] Migrate all affected constructor callers found by search, including test fixtures and `S/RuntimeSystem/InGameSystem/ViewSystem/Dev/DevBattleInitializationSource.cs` (constructor arguments only). Do not introduce optional fallback values to hide missing caller updates.
- [x] Run Task 1 fixtures; require the new checks to pass and classify any existing failures.

### Task 1 execution record — 2026-10-25

**Scope:** Task 1 only, in the current checkout as selected by the user. HEAD remained `d4a6d6fd83cb79e862e582d183a77a93241efe50`; the initial tracked tree was clean, with this plan and its design untracked. No commit or push was performed.

**Explicitly approved additions:**

- `UnitCatalog.ValidateCombatReady` now rejects negative movement cost rather than zero. Its prior positive-only guard conflicted with the approved zero-cost contract. The supplied-category test also calls this preparation validation.
- `Factory/Runtime/Development/TemporaryCoreDatabase.cs` received the ten required constructor arguments only. Factory registration and composition were not changed.

**Implemented:** The existing SO exports fresh validated data with five movement pairs. Required immutable category values flow through the adapter and registry to `UnitDefinition`; there is no optional movement fallback. The sole existing category asset explicitly contains Main `(3,2)`, Rider `(4,2)`, Heavy `(3,2)`, Cannon `(2,2)`, Boss `(0,2)`. An asset comparison confirmed that only these five blocks were added; previous values and all GUIDs were retained. No new C# file, service, table, or assembly boundary was added.

**Test-first evidence:** Ten SO negative-input tests failed on absent serialized movement fields before schema changes. After the schema and category contracts were connected, the three supplied-value tests failed because `UnitDefinition` still returned fixed Rider `(4,2)` for `(5,4)`, `(0,0)`, and `(5,12)`. Switching its movement properties to `Category` resolved those failures.

| Verification | Before | After Task 1 |
|---|---:|---:|
| Full Core assembly | 3,497 passed / 0 failed | 3,512 passed / 0 failed |
| Selected Framework fixtures | 155 passed / 90 failed | 181 passed / 90 failed |
| New feature cases | Not present | 41 passed / 0 failed |
| Skipped tests | 0 | 0 |

The final Unity run compiled successfully. All 90 existing failures retained the same full test names and failure messages; there were no new failures. The four Task 1 fixtures executed: contract **26/26**, bootstrap **20/20**, adapter **100 passed / 32 existing failures**, asset **47 passed / 21 existing failures**. Unity ran through the approved AppData-capable path; no cache deletion or upgrade was needed.

| Existing failed fixture | Count | Observed baseline failure class |
|---|---:|---|
| `BattleDefinitionAdapterTests` | 32 | Missing asset assertions (15), existing Boss behavior validation (17) |
| `DataBaseAssetTests` | 21 | Missing or mismatched existing content, references and retired-key expectations |
| `CharacterStatsQueryServiceTests` | 1 | Missing `Unit_DynamiteHammer` catalog entry |
| `CombatAdvanceServiceTests` | 3 | Old battle preparation budget/action state |
| `CombatCommandServiceTests` | 4 | Old battle preparation budget/action state |
| `MovementPublicationTests` | 19 | Old battle preparation budget/action state |
| `CoreStageCombatCycleTests` | 10 | Existing action/controller expectations and one preparation-state failure |

Local evidence is under `.superpowers/category-movement-so/`: `baseline.xml`, `task1-red.xml`, `task1-wiring-red.xml`, `task1-final.xml`, their logs, `baseline-failures.txt`, `task1-final-failures.txt`, and `task1-added-tests.txt`. These are ignored execution artifacts, not committed project content.

**Boundary audit:** `BattleDefinitionAdapter` supplies the configured categories to `BattlePreparationCalculator`, including enhanced-unit catalog construction. `OwnedStatsProjection` supplies the same configuration to `OwnedCharacterStatsCalculator`, including its enhanced-unit path. The two-argument `UnitCatalog` in `StageStartCatalog` validates stage definitions; its temporary default definitions do not replace these supplied catalogs. Existing baseline calculators already propagate definition movement values, so no additional state source or Buffer responsibility was introduced. Pure changed Core/Data types have no Unity reference, and `git diff --check` passed.

**Change / understanding / verification scope:** Previously, category movement tuning required editing fixed code values. Task 1 establishes SO-to-definition tuning through the existing input and immutable translation contracts; constructor callers required explicit migration once. Damage, rewards, Framework registration and Buffer settlement internals were not changed. Verification covered category input/contracts and the full Core plus selected Framework regression fixtures. Runtime availability, range and action charging still use the fixed readers until Task 2; Task 3 remains responsible for configured-value publication and lifetime integration evidence. This record does not claim the complete movement feature or all Framework integration is green.

**Review and stopping point:** An independent read-only reviewer found no critical, important, or minor issues in Task 1. The reviewer explicitly excluded Task 2 action semantics, Task 3 publication/lifetime integration, and the unchanged baseline failures; these remain pending as described above. Task 1 is complete; stop here for the user's next instruction.

## Task 2: Make action readers consume installed movement facts

**Modify:** `C/Application/Combat/CombatActionAdmission.cs`; `C/Domain/Movement/MovementRouteValidator.cs`; `C/Domain/Categories/CategoryActionRules.cs` and its attack-price callers. **Audit, edit only if propagation is missing:** `C/Application/Roster/CharacterBaselineStatsCalculator.cs`, `OwnedCharacterStatsCalculator.cs`; `C/Application/Combat/BattleCharacterCalculator.cs`, `MovementContextCalculator.cs`, `EnemyAi/EnemyMovementPathfinder.cs`; snapshot copy paths.

**Interfaces:** Retain `CombatActionAdmission.MovementAllowance(CharacterCombatFacts actor): int?`, returning null for `Stats.MoveRange == 0`, and `MoveCost(CharacterCombatFacts actor): int`, returning `Stats.MoveCost`. Keep `MovementRouteValidator.StepBudget(CharacterCombatFacts actor, long weightedCost)` and its typed failures. Replace the mixed fixed-rule tuple with `CategoryActionRules.AttackCost(CategoryKey category): int`, preserving Cannon 5 / others 3 and invalid-key rejection.

- [x] Extend `T/PlayerTurnMovementTests.cs`, `MovementRouteTests.cs`, `EnemyMovementPathTests.cs`, and `CategoryActionRulesTests.cs` before changing readers. With allowance 5 / cost 4: five edges succeed, six reject, AP 4 becomes 0, AP 3 rejects, and one/five-edge moves both spend 4. Preview and availability must report the same values.
- [x] Switch admission and route budgeting to installed stats; remove all fixed movement lookup members. Keep Buffer settlement/publication ownership and the existing action-use transition.
- [x] Audit manually constructed movement stats in `T/CombatCommandTestData.cs`, `MovementTestData.cs` and affected fixtures: previously ignored placeholder values now matter. Supply intentional category values; preserve non-default-value tests instead of restoring runtime constants to satisfy old fixtures.
- [x] Pin edge cases: zero cost leaves AP unchanged but sets `Moved`; zero allowance rejects voluntary movement without spend; positive allowance cannot bypass disabled behavior; forced displacement still works. Enemies use allowance 5 at player AP 0 and leave AP unchanged. Preserve diagonal middle effects, interrupted/dead-mover settlement and stale-context rejection when relevant facts differ.
- [x] Run Task 2 fixtures and existing movement/environment/attack regression tests. Reject any preview/commit disagreement or accidental attack-price change.

### Task 2 execution record — 2026-10-25

**Scope:** Task 2 only, continuing in the selected checkout with Task 1 changes preserved. HEAD remains `d4a6d6fd83cb79e862e582d183a77a93241efe50`; no commit or push.

**Implemented:** Admission reads installed `Stats.MoveRange` / `Stats.MoveCost`; route budgeting reads the same installed allowance. Zero allowance returns the existing disabled result. `CategoryActionRules` now owns only attack prices (Cannon 5, other valid categories 3), with invalid-key rejection retained. Its callers were migrated. The shared test helper preserves captured movement facts during ordinary copies and uses the existing default category registry only when explicitly changing category. No new production file, configuration source, service or state owner was added.

**Test-first evidence:** `task2-red.xml` ran 99 cases: 84 passed and 15 failed. Thirteen failures exposed the fixed movement readers. Two exposed an incorrect test assumption: stale-fact rejection is owned by `CombatCommandService`, which calls `MovementContextCalculator.Matches` and `RejectStale` before publication. The Core tests were corrected to exercise those existing comparison/rejection contracts; no duplicate guard was added to the calculator. Actual service/publication integration remains Task 3.

| Verification | Result |
|---|---:|
| Fresh full Core baseline | 3,512 passed / 0 failed / 0 skipped |
| Full Core after reader migration | 3,530 passed / 0 failed / 0 skipped |
| Final full Core, including additional free/interrupted movement cases | 3,535 passed / 0 failed / 0 skipped |

Final Task 2 fixtures passed: player movement **22**, routes **13**, enemy paths **17**, category action rules **8**, behavior restrictions **18**, movement environment **26**, player-turn rules **20**. Coverage includes `(5,4)` range/preview/availability/execution, fixed per-action charging, insufficient AP, cost above maximum, zero allowance, zero cost at AP 0 and 10, five-edge enemy movement at player AP 0, forced displacement with zero allowance, disabled behavior with positive allowance, diagonal middle effects, lethal/nonlethal interruption, and changed movement facts invalidating captured contexts. The full Core run also exercised existing attack and environment regressions.

**Boundary audit:** Baseline, owned-stat and battle-character calculations already propagate configured movement facts. Snapshot copying/comparison and movement-context comparison retain them; enemy pathfinding uses the updated route budget. These production paths required no additional edits. Buffer settlement/publication, Framework registration/lifecycle contracts, View and assets were unchanged by Task 2. No old fixed movement lookup remains, changed pure production files have no Unity references, and `git diff --check` passed.

**Change / understanding / verification scope:** The runtime change is limited to two movement readers and the attack-only table/callers. Tuning now reaches action queries and calculations through installed facts; changing it does not require understanding unrelated damage/reward implementations. Required validation still covers the shared action, movement, environment and attack contracts through full Core regression. DB-to-Buffer publication and configuration lifetime verification, plus the historical Framework failures, remain Task 3; this record does not claim complete Phase integration.

Evidence: `.superpowers/category-movement-so/task2-baseline.xml`, `task2-red.xml`, `task2-green.xml`, `task2-final.xml` and matching logs. Unity compiled successfully and logged exit code 0 for the final run through the approved AppData-capable path.

**Review and stopping point:** An independent read-only reviewer found no critical, important or minor issues and checked the final XML. Existing Task 1 diffs outside the Task 2 file list were compared with the initial snapshot and remained identical. Task 2 is complete. Task 3 remains pending; stop here for the user's next instruction.

## Task 3: Verify DB, stats and Buffer integration; close the Phase

**Tests:** `F/DataBaseAssetTests.cs`, `BattleDefinitionAdapterTests.cs`, `CharacterStatsQueryServiceTests.cs`, `MovementPublicationTests.cs`, `CoreStageCombatCycleTests.cs`, `CombatCommandServiceTests.cs`, `CombatAdvanceServiceTests.cs`; `T/CharacterBaselineStatsTests.cs`, `OwnedCharacterStatsCalculatorTests.cs`, `EnemyRedeploymentResolverTests.cs` and the full Core assembly. Directly blocking helpers may be adapted in `F/CharacterBufferTestData.cs`, `CharacterBattleSettlementTests.cs`, `CombatRequestTestParts.cs`; preserve the original assertions' purpose.

- [x] Add a DB-configured Rider `(5,4)` integration case: owned-stat query and prepared character agree; real preview/command publication changes AP 10 to 6 and only that unit to `Moved`. Build the fixture through valid preparation and controller start. Verify insufficient/stale requests preserve world, budget and publication contracts.
- [x] Confirm copied, leveled and redeployed facts retain their captured movement values. Changing the authored object after capture leaves the active battle unchanged; a fresh DB/definition lifecycle reads the new values. Confirm default catalogs cannot replace configured categories in these paths.
- [x] Run the full Core assembly and affected Framework-hosted fixtures. Compare each failure with baseline; report new failures separately. Existing blockers do not establish successful integration; unresolved required checks must be reported as incomplete.
- [x] Audit `CategoryActionRules` callers, Unity references in pure types, asset GUID/value changes, `git diff --check`, and the final file list against scope. Report actual SO-to-publication evidence and remaining failures, then stop. Further architecture changes or unrelated cleanup require a new decision.

### Task 3 execution record — 2026-10-25

**Scope:** Task 3 adds verification in six existing test files; it changes no production code or assets. Prior Task 1/2 diffs outside those files remained identical. No commit or push; HEAD remains `d4a6d6fd83cb79e862e582d183a77a93241efe50`.

**Verified integration:** A transient `CategoryConfigDataSO` exports Rider `(5,4)` through the existing DB query fixture. The default Core factory uses the real owned-stat query, DB battle adapter, battle entry, placement commands and controller start. Prepared movement facts match the owned query; preview quotes five edges at cost 4 without changing world/AP/result sequence. Command publication changes AP **10 to 6**, moves the Rider to its destination, marks only that actor `Moved`, and matches the preview's entered cells. No preparation guard or state-owner boundary was bypassed to create this scenario.

Changing the authored SO to `(2,1)` after capture leaves the existing battle at `(5,4)`; a fresh DB/runtime and battle capture `(2,1)`. Core tests additionally verify configured movement in owned/baseline/battle calculations at levels 1 and 3, snapshot/placement copies, and enemy redeployment. Inspection confirms both normal and enhanced-unit catalog construction use supplied categories; no default category catalog replaces them.

**Rejection contracts:** After other actors spend AP through real moves, the Rider's preview reports `ActionCostInsufficient` and provides no movement context, with AP and command/result publication unchanged. Replaying a previously valid request after that spend produces one acknowledged `StaleContext` rejection. Changing installed allowance or cost after preview also rejects once. Character, environment, soul and budget facts remain unchanged by each rejected command. The first integration run exposed an incorrect test assumption that an unsuccessful preview would provide a context; the test was corrected to the existing contract, without changing production code.

**Directly blocking fixture repair:** `CharacterStatsQueryServiceTests.Initialization` omitted the already-owned `Unit_DynamiteHammer` definition. Adding that existing definition allowed its before/during/after battle lifecycle comparison to execute and pass. Shared preparation helpers and unrelated obsolete fixtures were not redesigned.

| Verification | Fresh Task 3 baseline | Final Task 3 |
|---|---:|---:|
| Full Core assembly | 3,535 passed / 0 failed | 3,540 passed / 0 failed |
| Selected Framework fixtures | 181 passed / 90 failed | 186 passed / 89 failed |
| New Task 3 feature cases | Not present | 9 passed / 0 failed |
| Skipped cases | 0 | 0 |

All remaining failures have the same full test names and messages as the fresh baseline, which also matched the Task 1 baseline failure set. There are **no new or changed failures**. The combined runner exits **2**, not 0, because those existing failures remain. Compilation succeeded. Final relevant fixtures: baseline stats **15/15**, owned stats **25/25**, enemy redeployment **2/2**, stats query **11/11**, and movement publication **4 new passes / 19 existing failures**.

| Remaining failed fixture | Count | Limitation |
|---|---:|---|
| `BattleDefinitionAdapterTests` | 32 | Existing missing-content assertions and Boss behavior validation |
| `DataBaseAssetTests` | 21 | Existing content/reference and retired-key expectations |
| `MovementPublicationTests` | 19 | Legacy fixtures stop at initial preparation budget/action guards |
| `CombatCommandServiceTests` | 4 | Legacy preparation budget/action state |
| `CombatAdvanceServiceTests` | 3 | Legacy preparation budget/action state |
| `CoreStageCombatCycleTests` | 10 | Existing controller/action expectations and preparation state |

These 89 cases remain unresolved and do not prove their legacy scenarios succeeded. The new normal-entry tests provide the required configured-movement publication evidence independently. Full Framework acceptance is not green; unrelated fixture/content repair and manual gameplay acceptance are not claimed.

**Final audit and independence:** `CategoryActionRules` has only two production callers, both for attack price. Pure changed Core/Data files have no Unity reference. The category asset differs from HEAD only by the five intended movement pairs; no GUID/meta file changed and no C# file was added. `git diff --check` and LF checks passed. The implementation uses the existing category translation and installed snapshot contracts; Core proposes transitions and Buffer retains state/publication ownership. Task 3 required understanding those integration contracts, without modifying unrelated feature internals or introducing registration/lifecycle changes. Regression scope remained full Core plus the specified Framework fixtures.

Evidence under `.superpowers/category-movement-so/`: `task3-baseline.xml`, `task3-integration.xml`, `task3-check.xml` and matching logs; `task3-baseline-failures.txt`, `task3-final-failures.txt`, `task3-start.diff`, `phase-review.diff`. Unity used the approved AppData-capable execution path.

**Final review and Phase status:** An independent read-only reviewer checked the Phase diff, configured-value routing, normal publication flow and final XML, and found no critical, important or minor issues. Tasks 1–3 and the configured-movement verification are complete. The 89 documented legacy failures remain unresolved; full Framework and manual View/Play acceptance are not claimed. No further scope expansion, commit or push was performed. Stop here.

## Verification command (execution only)

Run Unity through the approved AppData-capable execution path; do not delete its cache or upgrade Unity. From the repository root, create the evidence directory and use this template. For baseline and final runs, use distinct result/log names.

```powershell
$evidence = Join-Path (Get-Location) '.superpowers/category-movement-so'
New-Item -ItemType Directory -Force -Path $evidence | Out-Null
Start-Process 'D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe' -WindowStyle Hidden -Wait -PassThru -ArgumentList @(
    '-batchmode', '-nographics', '-projectPath', 'D:/My_ProjectWork/Github_desktop/Project_L',
    '-runTests', '-testPlatform', 'EditMode', '-assemblyNames', 'ProjectL.Tests.Core',
    '-testResults', "$evidence/core-final.xml", '-logFile', "$evidence/core-final.log")
```

For a focused run, replace `-assemblyNames ProjectL.Tests.Core` with `-testFilter` and semicolon-separated fully qualified fixture names listed in the task (`ProjectL.Tests.Core.<name>` for `T`, `ProjectL.Tests.Framework.<name>` for `F`). Require compiler success, executed test XML, no new failures/skips, and passing new feature assertions; an exit code alone is insufficient. No Unity execution belongs to this plan-writing delivery.
