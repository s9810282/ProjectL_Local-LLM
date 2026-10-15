# Environment Content Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans for sequential execution, or superpowers:subagent-driven-development if the user selects that method. Steps use checkbox (`- [ ]`) syntax. Execute only authorized tasks and their policy-cleared portions. Commit or push only when explicitly requested; otherwise leave a reviewable diff and verification evidence.

**Goal:** Extend the completed Cell foundation so approved Terrain, Obstacle and Object behavior can run through the existing movement, combat, turn and Buffer publication boundaries, with explicit per-content activation gates.

**Architecture:** Cell remains a coordinate and a derived composition of authoritative facts. Existing Combat and Character Buffers own state; Core proposes immutable changes; existing View/Flow presents accepted results and acknowledges once. Add focused typed facts and calculations at actual consumers, without a new environment manager, Buffer, scheduler or effect engine.

**Tech Stack:** Unity 6000.0.73f1, C#, NUnit EditMode, existing ProjectL Core and Systems assemblies, existing Framework factories and Buffer prepare/install operations.

**Spec:** [Environment Content Follow Up Design](../specs/2026-10-05-environment-content-follow-up-design.md), especially sections 2.1, 4, 6 and 8.

**Date and status:** updated 2026-10-06. Post-E10 review refactoring is tracked in section 18: R1, P3, P1 and P2 are complete (behavior-preserving; full EditMode failure set unchanged). E1–E10 are complete for the authorized Core/Buffer fixture rules and existing View logical-result compatibility. E10 was requested separately after E9; section 17 records focused **85/85** and full **2676/2692**, with exactly the same 16 inherited failures as E9 and unchanged HEAD. The full suite remains non-green. The [activation handoff](../handoffs/2026-10-05-environment-content-activation-handoff.md) records all eight contents, actual DB owners and remaining production/visual gates. Actual balance values remain explicit fixtures; production authoring, T1/G7 default activation, dedicated/stacked visuals and PlayMode are not delivered. Source inspection baseline is `0b7e4a07`; E7–E10 continued from `3d7b8103` on `feature/Cheon/refactor/core-system`. E1–E10 do not restart completed Foundation P1–P5. See sections 6–17 for historical and final evidence.

## Global Constraints

- Initial plan authoring created only this document. Subsequent user requests authorize E1–E10's scoped implementation/test changes and progress recording, including confirmed gameplay decisions. E9 was completed alone after the earlier E10 cancellation; the later explicit E10 request authorizes existing View logical-result compatibility and the activation handoff. Historical plans, production assets/scenes and Framework infrastructure remain outside the executed scope. Necessary Buffer additions/modifications are authorized while preserving Framework–Buffer ownership. E8 extends existing Buffer contracts/publication and introduces no new Buffer.
- Preserve completed movement behavior and existing assertions. Future authorized extensions may connect new content to the listed movement boundaries; do not replace drag-path movement, generic sequential effects, route order, death settlement, paid-cost accounting or attack refunds.
- Multiple Objects per tile and tile-entry-order resolution are resumed logical requirements. Stacked visual composition remains deferred. Never use source ID, rule Priority, overwrite or first-only activation as the gameplay policy.
- Resolve each Object against the latest projection. Settle its damage/death before the next Object. Death stops remaining activation and consumption; an earlier Flag activates before a later Mine.
- Objects remain tile-bound, immune to direct/AoE damage, activated only by approved arrival and removed from active play after activation. Terrain + Object is allowed; Terrain + Obstacle and Obstacle + Object are prohibited. Ordinary floor is not special Terrain.
- Low-to-high entry costs base AP +2 and exactly one step. Descent and high-to-high have no surcharge. High Ground reduces damage from non-High-Ground origins by 30%; Sandstorm reduces incoming damage by 20%; percentages compose successively.
- A Badge blocks one whole attack, including every hit; another held Badge does not add a charge. Supplies expires before night-to-day recall, including for Main. A Tornado turn is one completed allied or enemy unit turn.
- Mine/Barrel blasts can damage all HP-bearing allied/enemy Characters and structures; Barrel chains are enabled. Lethal Obstacle processing selects destruction, never hit plus destruction for the same processing unit.
- Attack execution seals Return; entering/cancelling targeting does not. Do not introduce a blanket Terrain Return barrier.
- Keep classification separate from position/HP, physical damage origin separate from credited Character, and attack protection grouping separate from causality or command identity.
- Keep Wall, the struck-through HP-99 sentinel, new physical-defense formulas, production asset changes and stacked visuals outside this plan's executable scope. Preserve default DB T1/G7 rejection until its independent authoring/query requirements are resolved.
- Do not create a Framework Tick, global trigger bus, generic policy scripting system, new assembly or package. Add fields/types only with a consumer in the same task or immediately dependent tested slice.
- Implementation documents are English; new code comments are Korean. New C# files need unique Unity `.meta` GUIDs; preserve existing GUIDs.
- Preserve unrelated incoming changes, including `Assets/Audio/FMODplus/New Fmod Event Catalog.asset`. The follow-up design is an existing untracked input, not permission to overwrite it.

## Review Focus

1. A cost-relevant source at the route origin must invalidate a visible stale preview, while hidden-only state changes must not disclose hidden content: E2/E7.
2. A stale semantic binding, held state, allocator or RNG proposal must reject the entire publication without partial HP, event or turn installation: E1/E6/E8.
3. A spent Badge must still protect later hits of the same attack, and copying/recalling the Character must never recharge it: E6.
4. Hazard/Recovery grouping must not move a later Mine ahead of an earlier pickup; death must leave later Objects unconsumed: E7.
5. Converging explosion chains must not duplicate destruction, and Supplies expiry must run at the pre-recall boundary even when Main stays on the field: E5/E6.

## 1 Execution scope and local decision gates

This is one integration plan because these contents share command projection and atomic publication. Each task has its own measurable result. Execute a ready task without waiting for unrelated content decisions; do not mark a partially supported content as complete.

| Task | Deliverable | Readiness and dependency |
| --- | --- | --- |
| E1 | Typed static Terrain facts, captured semantic input and exact local formulas | Complete; fixture contracts verified, no production activation. |
| E2 | High Ground route pricing through validation, Preview, execution, range and Return | E1; AI policy confirmed during E2: lowest AP, then fewest steps. |
| E3 | Shared damage provenance and behavior-preserving extraction | Ready independently; new damage semantics remain gated. |
| E4 | Complete static Terrain range, defense and knockback integration | E1–E3; G1 must close for affected cases. |
| E5 | Destructible Obstacles and terminating Barrel chains | Complete: confirmed per-hit/zero-damage/direct-target/contact/group/credit rules; explicit fixture values only. |
| E6 | Held-state publication, whole-attack Badge and Supplies lifetime | Complete; G3 behavior confirmed, explicit fixture values, real tile-entry Object sources remain E7. |
| E7 | Ordered arrival Objects, Mine and selected-source consumption | Complete for system/fixture scope: uniform-N full 3x3 Mine, ordering, Badge, chains, approved arrivals and publication verified. See section 14 for baseline failure comparison. |
| E8 | Dynamic source publication and Flag acquisition/death drop | Implemented for authorized Core/Buffer fixture scope; final evidence in section 15. |
| E9 | Tornado timed relocation and respawn | Complete for Core/Buffer fixtures; G6 confirmed; fresh review fixes verified, section 16. |
| E10 | Cross-boundary regression and per-content production handoff | Each completed slice; G7 remains a separate activation gate. |

The design suggests Sandstorm first once its policies are settled. E1–E2 instead expose the already-settled High Ground movement rule first; they do not claim complete High Ground support. E4 remains the first complete static Terrain slice. No unfinished content may bypass preparation guards to appear enabled in a real stage.

| Gate | Required decision before the dependent implementation step | Does not reopen |
| --- | --- | --- |
| G1 — closed for E4 | Terrain percentages follow existing matchup/PhysicalDefense/Heavy stages, retain exact intermediates and floor final damage. Sandstorm reads current range after bonuses and preserves 0/1. Transit defense uses the current cell; originless damage gets Sandstorm defense but no High Ground defense. One-cell knockback blocks only low-to-high; Return exempt. Multi-cell displacement remains unsupported. | User confirmed all four E4 questions. Multiplicative percentages, +2 AP and one-step distance remain settled. E2 AI objective: lowest AP, then fewest steps. |
| G2 — closed for E5/E7 behavior | Both factions directly attack Obstacles; reactions per hit, no reaction at zero damage, lethal destruction only; one damage application per entity per blast; each Barrel explosion is a separate attack with original Character credit. Distinct typed contract keys use existing neutral ownership without changing Headquarters. HP/explosion values remain unapproved, with explicit fixtures only. E7 replacement confirms Mine's full 3x3 uniform N raw damage. | Faction-independent blast victims, Barrel's eight neighbors excluding center, terminating chains. Mine geometry comes from the explicit replacement answer. |
| G3 — closed for E6/E7 behavior | Badge consumes on zero final damage, protects all hits of the same attack, and bypasses explicit instant death including Railway. Each Barrel explosion, Mine explosion and stepped Thorn cell is a separate attack. Duplicate pickup consumes the field Object without adding protection; day recall retains Badge; terminal battle end clears it. Supplies rewards accumulate per exclusive Hit/Destroyed reaction and go only to a living original credited Character; expire before day recall, including Main. Reward values remain explicit fixtures pending G7. Flag terminal lifetime remains E8. | Whole-attack Badge, no stacked protection, recall retention, Supplies pre-recall expiry. Concrete Mine/Badge acquisition and groups are verified in E7. |
| G4 — closed for E7 | Initial Object order follows the authored placement list. Voluntary entry, forced arrival, player placement and enemy night redeployment activate Objects. Mine includes center and all eight neighbors with identical explicit N raw damage, clipped to the board; existing tile-arrival activation remains. Existing hazards precede ordered Objects; existing recovery follows them. Death, battle end or relocation stops the previous-cell sequence. Objects created during resolution remain an E8 decision; E7 does not create or relocate sources. | Entry order, Flag before later Mine, death stopping and preserving remaining Objects. Preserve existing non-Object behavior at each arrival boundary rather than introducing new generic triggers. |
| G5 — closed for E8 execution | User confirmed additive N Priority, one held Flag, one death drop, terminal removal and immediate pickup if created under a unit; normally avoid occupied drop cells. Current action continues; its next interval uses the increased Priority, allowing A → A. Execution rulings: squared Euclidean distance, random selection among equally nearest empty valid cells, no candidate means remove without allocating; duplicate held pickup leaves the extra Flag on the field. | Existing Object coexistence/no overwrite and day-recall retention remain. Core and existing Buffer modifications are authorized without changing Framework–Buffer ownership. |
| G6 — closed for E9 execution | User approved first due immediately after the third aggregate completed unit turn, then every three; persist the count/due across day/night; settle before recall. Empty cells still respawn. Unit destinations exclude the origin, live Characters and every living structure; apply positional Terrain before approved arrival effects. Tornado destinations exclude the origin, every other active Terrain and living structures; allow Characters/Objects without extra arrival or Terrain replacement. No candidate skips that choice without RNG but advances due by three. Process sources by ID against current facts and stop at terminal outcome. | Both factions count; no query/frame/path clock. Production authoring and visual playback remain G7/E10. |
| G7 | Approved content keys/values/visibility/placements, real SO-to-Query mapping, default targeting authoring, presentation assets and stage activation. | Fixture-based contract testing and independently completed logical slices. |

Record answers in the follow-up design and this plan's affected gate before implementing dependent behavior. If still unanswered, leave that step unchecked and report its precise boundary. Do not insert a production default, skipped test or inert content as a substitute. Missing balance numbers alone can use explicitly labeled fixture values after behavior is settled.

## 2 File map and ownership

All aliases are repository-relative directories, not new directories. `CoreCaculator` is the existing spelling.

| Alias | Exact root |
| --- | --- |
| S | `Assets/ProjectL/Script/Systems` |
| C | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator` |
| K | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat` |
| A | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat` |
| D | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain` |
| B | `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems` |
| CS | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CombatSystem` |
| DB | `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Stage/Combat` |
| V | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat` |
| T | `Assets/Tests/EditMode` |
| F | `Assets/Tests/EditMode/Framework` |

The task file lists below use `A/File.cs` to mean the exact alias root plus `/File.cs`. Existing files remain responsible for their existing work; new files contain focused facts/calculations, not services.

| Boundary | Planned responsibility and first owner |
| --- | --- |
| `K/StaticTerrainFact.cs`, `D/Movement/EnvironmentTerrainRules.cs` | New immutable High Ground/Sandstorm descriptors and local calculations, E1. The helper moved from Application to Domain in E2 to serve route validation without a reverse dependency. |
| `K/CombatCommandInput.cs`, `CS/CombatProcessingService.cs` | Capture read-only semantic bindings beside command state, E1. |
| `A/CombatCommandComposition.cs`, `A/MovementProjection.cs`, `A/CombatAttackQueries.cs`, `A/EnemyAi/EnemyMovementPathfinder.cs` | Preserve added input through intent, projection, hypothetical attack and query copies, E1 onward. |
| `A/CombatDamageCalculator.cs`, `K/CombatDamageOrigin.cs` | Extract current attack arithmetic and carry provenance, E3. |
| `K/ObstacleContentProfile.cs`, `A/ObstacleReactionCalculator.cs` | Immutable definition association and command-local hit/destruction reactions, E5. |
| `K/CharacterHeldEnvironmentState.cs`, `A/HeldEnvironmentEffects.cs` | Explicit Badge and Supplies state/lifetime, extended with approved Flag state later, E6/E8. |
| `K/ObjectSourceFact.cs`, `K/EnvironmentProgressFacts.cs`, `A/ObjectArrivalCalculator.cs` | Object entry ordinal, ordinal allocator and one arrival sequence inside existing coordinator, E7. |
| `K/EnvironmentBindingsChange.cs`, `A/FlagDropCalculator.cs` | Dynamic binding proposal and death drop; source allocator/RNG extend E7 progress only when used, E8. |
| `A/TornadoCalculator.cs` | Due action calculation called by existing gimmick/turn boundaries, E9. |
| Existing Buffer and View paths | Validate/install all new facts atomically; present immutable results through the existing owner. No new stores or content-owned acknowledgements. |

New names above and method signatures below are proposed APIs, not claims that code already exists. Names marked as gated are contract destinations; finish the stated policy-dependent shape before adding their runtime type. Keep helpers internal unless existing cross-assembly use requires a public contract.

### Copy and preparation obligations

These are part of the owning task's file scope, not optional cleanup after integration.

| New fact | Required propagation and validation |
| --- | --- |
| Semantic command input, E1 | `A/CombatCommandComposition.cs` intent/project/merge, `A/MovementProjection.cs` current input, `A/CombatAttackQueries.cs` aim copies, `A/EnemyAi/EnemyMovementPathfinder.cs` hypothetical inputs and test builders. Buffer prepare compares the captured binding value even when `CombatCommandState` itself is unchanged. |
| Stage content, E4/E5/E7/E9 | `CS/BattleDefinitionAdapter.cs` must actually pass the new placement input to the adapter. E5 profiles additionally travel through `K/BattleInitialization.cs`, `A/BattlePreparationCalculator.cs`, `K/BattlePreparation.cs` (which also declares `CombatPreparation`) and `B/Combat/CombatStateData.cs`. Retain profiles as immutable battle definition data, then capture them in command input; reject mismatched profile input during prepare rather than trusting command-supplied definitions. |
| Environment progress, E7/E8 | Include in `K/CombatCommandState.cs` constructor/default/equality, preparation and initial validation, then preserve in state reconstructions in `A/CombatCommandCalculator.cs`, `A/CombatTurnCalculator.cs`, `A/MovementProjection.cs` and `A/PositionTileModifierCalculator.cs`. Initial next ordinal exceeds every authored ordinal; initial next source ID exceeds every initial source ID. Empty legacy state remains compatible. |
| Held state and transition changes, E6/E8 | Character copies preserve value; comparer includes value. Projection uses proposed final state; composition/merge retain the original expected state and one final result. `K/CombatCommandTransition.cs` completion copies must not discard binding changes. Add a round-trip test for each newly extended copy path. |

At execution time search constructor call sites before closing the task; the table identifies inspected paths, not permission to ignore newly added callers. Fix only propagation needed for these contracts, and keep profile/order/progress data out of `EnvironmentSemanticBindings`.

## 3 Verification protocol for future execution

Use existing fixture builders (`T/MovementTestData.cs`, `T/CombatCommandTestData.cs`, `T/CellEnvironmentTestData.cs`) and real Buffer publication suites. Extend builders to carry new fields; do not rewrite old assertions to accept changed legacy behavior. No test or Unity invocation runs as part of writing this document.

For each task: establish the focused failing test, implement, run that fixture plus the named regression suites, then record the result. A compile failure caused by a missing planned API can establish initial RED; after the API exists, a behavioral failure must establish the rule. No tests selected, compiler errors during GREEN, ignored tests or absent XML are not a pass.

Use this PowerShell helper during implementation, with evidence outside tracked source. If Unity already owns this project, use its supported EditMode runner or arrange the run with the owner; do not kill their Editor to force batch mode.

```powershell
function Invoke-EnvironmentTests([string]$Filter, [string]$Label) {
    $evidenceDir = Join-Path $env:TEMP ('ProjectL-Environment-' + [guid]::NewGuid())
    New-Item -ItemType Directory -Path $evidenceDir | Out-Null
    $resultPath = Join-Path $evidenceDir ($Label + '.xml')
    $logPath = Join-Path $evidenceDir ($Label + '.log')
    $unityArgs = @('-batchmode', '-nographics', '-projectPath',
        '"D:\My_ProjectWork\Github_desktop\Project_L"',
        '-runTests', '-testPlatform', 'EditMode', '-assemblyNames',
        'ProjectL.Tests.Core;ProjectL.Tests.Framework;ProjectL.Tests.CombatViewBoundary',
        '-testResults', ('"' + $resultPath + '"'), '-logFile', ('"' + $logPath + '"'))
    if ($Filter) { $unityArgs += @('-testFilter', ('"' + $Filter + '"')) }
    $run = Start-Process -FilePath 'D:\Program Files\Unity\Hub\Editor\6000.0.73f1\Editor\Unity.exe' `
        -ArgumentList $unityArgs -WindowStyle Hidden -PassThru -Wait
    if (!(Test-Path -LiteralPath $resultPath)) { throw "Missing test XML: $logPath" }
    [xml]$report = Get-Content -LiteralPath $resultPath -Raw
    $suite = $report.'test-run'
    if ($run.ExitCode -ne 0 -or [int]$suite.total -le 0 -or
        [int]$suite.failed -ne 0 -or [int]$suite.skipped -ne 0 -or
        $suite.result -ne 'Passed') { throw "Tests did not pass: $evidenceDir" }
    Write-Output $evidenceDir
}
```

Filter strings below match fixture names; join them with `;` for one invocation. At execution time verify discovered fixture names against the XML. Capture incoming working-tree changes before Editor use; preserve pre-existing FMOD catalog changes if Editor import produces more generated output. Do not restore the whole file from Git.

Every task ends with a reviewable diff and evidence, not an automatic commit. Recommended review checkpoints: after E2/E4 calculation integration, after E6/E7 held state and arrival ordering, and after E8 dynamic publication. The user chooses review timing/model; this plan does not schedule an automatic review.

## 4 Tasks

### E1 — Typed static Terrain facts and semantic command input

**Files:** Create `K/StaticTerrainFact.cs`, `A/EnvironmentTerrainRules.cs`, `T/EnvironmentTerrainRuleTests.cs`. Modify `K/CombatCommandInput.cs`, `K/EnvironmentSemanticBindings.cs`, `CS/CombatProcessingService.cs`, `A/CombatCommandComposition.cs`, `A/MovementProjection.cs`, `A/CombatAttackQueries.cs`, `A/EnemyAi/EnemyMovementPathfinder.cs`, `A/MovementEnvironmentCalculator.cs`, `B/Combat/CombatStateBuffer.cs`. Extend `T/CellEnvironmentQueryTests.cs`, `F/CombatCommandPublicationTests.cs` and relevant fixture builders.

**Interfaces:**

- `StaticTerrainFact : MovementTileFact` takes `(long id, CellCoord cell, StaticTerrainKind kind, bool active, IReadOnlyList<Owner> knownTo)`; `StaticTerrainKind` contains only `HighGround` and `Sandstorm`. Implement existing `SameValues`/`WithKnownTo` without losing `Kind`. It carries identity/location/type, not mutable Character stats or dynamic turn state.
- Add `EnvironmentSemanticBindings EnvironmentBindings` as a final optional command-input argument normalized to `EnvironmentSemanticBindings.Empty` for old fixtures. Production capture supplies the actual current bindings. Add `bool SameValues(EnvironmentSemanticBindings other)` with set semantics per classification namespace; array order is not gameplay order.
- `EnvironmentTerrainRules.HasTerrain(MovementEnvironmentFacts environment, EnvironmentSemanticBindings bindings, CellCoord cell, StaticTerrainKind kind) : bool` reads active, correctly classified sources.
- `EnvironmentTerrainRules.EdgeActionCost(MovementEnvironmentFacts environment, EnvironmentSemanticBindings bindings, CellCoord from, CellCoord to, int baseCost) : int` returns base +2 only for low-to-high. Reject negative base cost and arithmetic overflow; it does not validate adjacency or charge AP.
- `EnvironmentTerrainRules.SandstormRange(int inputRange) : int` returns `inputRange > 1 ? inputRange - 1 : inputRange`; rejects negative range. This local formula does not yet decide where to read effective range.

- [x] **E1.1 — Add failing cases:** `EdgeCost_IsDirectional` asserts base1 gives low→high3, high→low1, high→high1, low→low1; base0 ascent gives2. `SandstormRange_PreservesZero` asserts 0/1/2/4→0/1/1/3. `TerrainFacts_IgnoreInactiveOrUnclassifiedSources` and `VisibilityCopy_PreservesTerrainKind` assert exact query/copy behavior.
- [x] **E1.2 — Establish RED:** run `Invoke-EnvironmentTests 'EnvironmentTerrainRuleTests;CellEnvironmentQueryTests;CombatCommandPublicationTests' 'E1-red'`; record the expected new failures.
- [x] **E1.3 — Implement facts and pure rules:** use the existing source collection and Cell query classifications. Exclude static presence-only facts from generic arrival-rule dispatch; do not register fake entry rules for them.
- [x] **E1.4 — Wire input and expected-state comparison:** capture bindings, preserve them in every listed copy path and reject a command prepared against different current bindings before installation. `BindingsOnlyStaleInput_RejectsWithoutPublication` must assert unchanged Characters, Combat state and result sequence. Ordinary commands keep the current binding object; no binding mutation API yet.
- [x] **E1.5 — Verify GREEN:** rerun E1 filters plus `MovementContractTests;EnvironmentEntryPublicationTests`. Do not relax `BattleEnvironmentPreparation` to advertise complete static Terrain support yet; E4 owns that capability. E1 is a tested source/query contract, not a production feature toggle.

### E2 — Directional movement price without replacing movement

**Files:** Modify `D/Movement/MovementRouteValidator.cs`, `A/CombatMovementCalculator.cs`, `A/EnemyAi/EnemyMovementPathfinder.cs`, `A/MovementContextCalculator.cs`, `K/MovementPreviewContext.cs`; adjust `A/MovementProjection.cs` only where actual edge cost is supplied. Relocate the E1 pure helper and its GUID from `A/EnvironmentTerrainRules.cs` to `D/Movement/EnvironmentTerrainRules.cs`. Create `D/Movement/MovementSearchLabel.cs` for the AP/step labels shared by range and AI. Extend `T/EnemyMovementPathTests.cs`, `F/MovementPublicationTests.cs` and binding-preserving fixture builders; verify existing route/contract tests. Create `T/HighGroundMovementTests.cs` and update the temporary E1 uniform-cost assertion in `T/EnvironmentTerrainRuleTests.cs` while retaining its static-dispatch checks.

**Interfaces:** Consume E1 `EdgeActionCost`. Keep `RecordEnteredStep(actor, stepCost)` and existing pending-distance/pending-cost ledgers. Route cost is the sum of edges, distance is the number of edges. Range exploration tracks nondominated `(cell, steps, AP cost)` labels; a label dominates another only if neither budget is worse. The current route validator remains responsible for whole-route bounds/adjacency/occupancy/repetition checks.

- [x] **E2.1 — Add failing cases:** `AscendingEdge_ChargesThreeApAndOneStep` with base1/AP3/range1 succeeds, AP2 fails; `DescendingAndPlateau_NoSurcharge`; `MixedRoute_ReportsAndRefundsActualCost` uses low→high→high→low for 5 AP and 3 steps, then refunds the existing refundable ledger; `ZeroBaseCost_StillChargesAscent` never divides by zero.
- [x] **E2.2 — Add budget/context cases:** `Range_KeepsCheaperLongerAndCostlierShorterLabels` asserts both feasible continuations under competing budgets; `Preview_OriginTerrainChangeIsRelevant` changes only the visible starting cell's High Ground source and rejects stale approval; `Preview_HiddenOnlyChangeDoesNotLeak` preserves public known-fact output/staleness behavior. Capture relevant from-cells as well as destination cells, without replacing visible knowledge with all sources.
- [x] **E2.3 — Establish RED:** run `Invoke-EnvironmentTests 'HighGroundMovementTests;MovementContractTests' 'E2-red'`.
- [x] **E2.4 — Integrate player paths:** replace uniform multiplication in whole-route admission, requested Preview cost, execution and range with shared edge pricing. Preserve checks before publication and current per-step projected state. Charge each entered edge once; do not perform a second post-move surcharge. Return uses recorded paid cost, not recalculation against changed Terrain.
- [x] **E2.5 — Integrate AI after route-choice decision:** preserve no-Terrain goal/tie behavior and known-mine exclusion. Record whether step count or AP is the primary objective on competing Terrain routes before changing search selection. Use nondominated labels and sum the chosen prefix's actual edge costs; test the approved conflicting-route example and affordable prefix. Do not call E2 fully complete while this branch is open.
- [x] **E2.6 — Verify GREEN:** run `Invoke-EnvironmentTests 'HighGroundMovementTests;MovementRouteTests;MovementEnvironmentTests;MovementContractTests;EnemyMovementPathTests;MovementPublicationTests;MovementDraftTests' 'E2-green'`. Record player-path readiness separately from any open AI policy.

### E3 — Damage provenance and compatible shared calculation

**Files:** Create `K/CombatDamageOrigin.cs`, `A/CombatDamageCalculator.cs`, `T/EnvironmentDamageTests.cs`. Modify `A/CombatCommandCalculator.cs`, `A/MovementProjection.cs`, `K/CombatEvent.cs`; extend `T/CombatCommandEffectTests.cs`, `T/MovementEnvironmentTests.cs` and verify existing `T/CategoryDamagePipelineTests.cs`. The executed provenance path additionally requires narrow changes to `A/CombatCommandEvents.cs`, `A/MovementEnvironmentCalculator.cs` and `K/MovementStepEffects.cs`; these retain existing eligibility and event ordering.

**Interfaces:**

- `CombatDamageOrigin` is immutable `(CellCoord? physicalCell, EntityId? creditedCharacterId)`; a destroyed source's cell is captured before removal. Null credit is allowed and is not a substitute physical origin.
- Extract the existing private damage wrapper as `CombatDamageCalculator.Attack(CombatCommandInput input, CombatEffectEngine engine, TargetDamagePipeline pipeline, CharacterCombatFacts actor, CharacterCombatFacts victim, int damage, AttackTargetRole role, int percent, CellCoord? impactOrigin, Rotation4? ray) : TargetDamageBreakdown`. Preserve exact existing stages/results first; null victim remains the existing structure path.
- Existing environmental HP-loss callers must carry `CombatDamageOrigin` to their application boundary. Ordinary damage, direct instant kill and max-HP normalization remain distinct operations. E4/E6 extend damage at this boundary, not every caller's HP setter.
- Executed E3 boundary: `CombatDamageCalculator.Environment(int damage, CombatDamageOrigin origin)` preserves legacy environmental amounts without a fabricated attacker Category or Heavy reduction. `MovementHealthRequest` accepts an optional origin; the coordinator supplies the selected source cell and null credit when absent. Character/structure HP events copy immutable `DamageOrigins` lists. Aggregated multi-cell thorn settlement retains all physical source cells within its existing single HP event; healing, instant kill and normalization do not gain ordinary damage metadata.

- [x] **E3.1 — Add characterization cases:** `ExtractedAttackDamage_MatchesExistingBreakdown` covers primary/additional, category, incoming effect and structure paths; `LegacyThorn_DoesNotGainCategoryOrHeavyRules` pins existing amounts; `PhysicalOrigin_IsIndependentOfCredit_AfterSourceConsumption` retains a fixture source cell while credit points at a Character elsewhere, including lethal damage. Assert event origin survives source deactivation and existing structure-destruction records retain captured facts. Actual Barrel destruction/explosion behavior remains E5.
- [x] **E3.2 — Establish RED:** run `Invoke-EnvironmentTests 'EnvironmentDamageTests;CategoryDamagePipelineTests;CombatCommandEffectTests;MovementEnvironmentTests' 'E3-red'`.
- [x] **E3.3 — Extract and thread provenance:** retain `TargetDamagePipeline` and `CombatEffectEngine`, preserving splash/pierce/category arithmetic and the physical-defense pass-through. Keep caller-specific eligibility. Do not apply hostile-only or attacker-self exclusion to future environmental blasts, and do not fabricate a category for them.
- [x] **E3.4 — Verify GREEN:** rerun E3 filters plus `TargetingSplashDamageTests;CombatCommandCalculationTests;KnockbackTraitTests`. E3 claims compatibility and provenance only; percentage rounding and Badge consumption are not secretly defaulted here.

### E4 — Complete static Terrain integration

**Files:** Modify `D/Movement/EnvironmentTerrainRules.cs`, `A/CombatDamageCalculator.cs`, `D/Combat/TargetDamagePipeline.cs`, `D/Combat/TargetDamageBreakdown.cs`, `A/CombatAttackQueries.cs`, `A/CombatCommandCalculator.cs`, `A/MovementEnvironmentCalculator.cs`, `A/MovementProjection.cs`, `A/MovementContextCalculator.cs`, `A/EnemyAi/EnemyMovementPathfinder.cs`, `K/MovementStepEffects.cs`, `K/MovementPreviewContext.cs`, `A/BattleEnvironmentPreparation.cs`, `CS/StageEnvironmentAdapter.cs`, `CS/BattleDefinitionAdapter.cs`, `DB/CombatStageDetail.cs`. Create `DB/StaticTerrainPlacementData.cs`, `T/StaticTerrainIntegrationTests.cs`. Extend `F/StageEnvironmentAdapterTests.cs`, `F/EnvironmentEntryPublicationTests.cs`, `T/BattleEnvironmentPreparationTests.cs` and the existing Character fixture's effective-range input. Cross-consumer targeting/knockback assertions are centralized in the new integration suite; existing targeting/knockback suites remain regression filters.

**Gate:** G1 closed by the user's four answers before implementing dependent callers; see section 9.

**Interfaces:** `StaticTerrainPlacementData(long sourceId, CellCoord cell, StaticTerrainKind kind, IReadOnlyList<Owner> knownTo)` is an immutable authored DTO. `CombatStageDetail` has an optional static-Terrain placement collection; the adapter overload retains destination-modifier placements. `EnvironmentTerrainRules.AppointRange(input, actor, environment = null)` derives range for Selection, Range, execution and hypothetical AI positions. Public callers pass visible facts; execution uses authoritative facts. `TargetDamageBreakdown.DamageAfterReceivedPercentages` retains exact decimal intermediates. `MovementForcedMoveRequest.Cause` defaults to Unspecified; only explicit Knockback receives the directional restriction. Command-local `MovementProjection.TerrainQueryCells` records defense and knockback dependencies for visible stale-context validation, including blocked candidate edges. Keep damage in the shared calculator; no second mutable defense/range field or new Buffer.

- [x] **E4.1 — Pin policies and failing assertions:** add exact G1 answers with numerical final-HP expectations. `SuccessiveReductions_AreNotAdded` must retain the pre-rounding value `10 × 0.8 × 0.7 = 5.6`; the integer assertion comes from the approved rule. `HighGround_UsesPhysicalOrigin` distinguishes low source/high credited Character from high source/low credit. Use a synthetic independent 20% contribution if needed; do not assume High Ground and Sandstorm may occupy one tile.
- [x] **E4.2 — Add cross-consumer cases:** `Sandstorm_QueryExecutionAndAiAgree`, `TerrainDefense_DirectAndEnvironmentalDamageAgree`, `DepartureAndReturn_RecomputePresence`, `Knockback_LowToHighRejected_HighToLowAllowed`, plus approved transit/originless/high-to-high cases. Preserve other range contributions and authored attack geometry.
- [x] **E4.3 — Establish RED:** run `Invoke-EnvironmentTests 'StaticTerrainIntegrationTests;StageEnvironmentAdapterTests;BattleEnvironmentPreparationTests;TargetingPreviewParityTests;KnockbackTraitTests' 'E4-red'`.
- [x] **E4.4 — Implement calculations and entry support:** apply percentages at the approved shared damage stage; route all ordinary environmental damage that requires Terrain defense through it. Add typed adapter conversion and only the now-implemented preparation capabilities. Keep initial presence separate from arrival; reject unsupported Terrain combinations rather than inventing composition. Do not enable dynamic Tornado or Object entry here.
- [x] **E4.5 — Verify GREEN:** rerun E4 filters plus E2/E3 suites and `EnvironmentEntryPublicationTests;StageEnemyAiTurnTests`. Static Terrain fixture readiness requires initial preparation, command publication and current/hypothetical query agreement. Default production T1/G7 remains intact.

### E5 — Obstacle reactions and Barrel chains

**Files:** Create `K/ObstacleContentProfile.cs`, `K/InitialObstaclePlacementSpec.cs`, `A/ObstacleReactionCalculator.cs`, `T/ObstacleReactionTests.cs`. Modify `K/CombatCommandInput.cs`, `K/CombatDamageOrigin.cs`, `K/CombatEvent.cs`, `K/StructureCombatFacts.cs`, `K/BattleInitialization.cs`, `K/BattlePreparation.cs`, `K/MovementPreviewContext.cs`, `C/Contracts/Ids/EntityKey.cs`, `A/CombatCommandComposition.cs`, `A/CombatCommandCalculator.cs`, `A/CombatAttackQueries.cs`, `A/MovementProjection.cs`, `A/MovementContextCalculator.cs`, `A/EnemyAi/EnemyMovementPathfinder.cs`, `A/BattlePreparationCalculator.cs`, `A/BattleEnvironmentPreparation.cs`, `B/Combat/CombatStateData.cs`, `B/Combat/CombatStateBuffer.cs`, `CS/CombatProcessingService.cs`, `CS/BattleDefinitionAdapter.cs`, `CS/StageEnvironmentAdapter.cs`, `DB/CombatStageDetail.cs`. Extend `T/CombatCommandTestData.cs`, `T/BattleEnvironmentPreparationTests.cs`, `F/CombatCommandPublicationTests.cs`, `F/StageEnvironmentAdapterTests.cs`, `F/EnvironmentEntryPublicationTests.cs`; retain existing placement-rule regression coverage.

**Gate:** E5 behavior gates are closed by user answers. Distinct Barrel/Supplies contract keys and the existing Owner.Neutral value are supported narrowly; real SO/query mappings, HP/n and stage activation remain G7. Do not classify every structure as an Obstacle or reuse Headquarters. Supplies placement remains rejected until E6.

**Interfaces:** `ObstacleContentProfile` associates an existing `EntityId` with `ObstacleContentKind` (`PowderBarrel`, `Supplies`) and approved immutable values; it never duplicates HP or occupied cells. Add `IReadOnlyList<ObstacleContentProfile> ObstacleContents` to command input and stage preparation, with unique existing bound-Obstacle references. `ObstacleReactionCalculator.Select(int beforeHp, int afterHp) : ObstacleReactionKind` returns `None`, `Hit` or `Destroyed` for a policy-qualified damage unit; `Destroyed` requires an alive→dead crossing. `BarrelCells(CellCoord origin, int width, int height) : IReadOnlyList<CellCoord>` returns the in-board eight neighbors, excluding center.

- [x] **E5.1 — Define the reaction unit and failing tests:** `LethalUnit_EmitsDestructionOnly`, `NonlethalUnit_EmitsHitOnly`, `AlreadyDestroyed_DoesNotReact`, `BarrelMask_ClipsEdgesAndExcludesCenter`, `Barrel_HitsAllFactionsAndStructures`. Supply explicit fixture HP/n after behavior is settled; assert Terrain/Object sources are unchanged.
- [x] **E5.2 — Add chain tests:** `DiamondChain_ExplodesEachBarrelOnce` sends two branches into one Barrel and asserts one alive→dead reaction; `Blast_MultipleFootprintContactsDamageOnceAndReleaseFullFootprint`; approved multi-cell-contact and direct-target cases. Death result carries the original anchor.
- [x] **E5.3 — Establish RED:** run `Invoke-EnvironmentTests 'ObstacleReactionTests;CombatCommandPublicationTests;EnvironmentPlacementRuleTests' 'E5-red'`.
- [x] **E5.4 — Implement within the command projection:** choose reaction after the approved damage unit, queue newly destroyed Barrels, and deduplicate by destroyed EntityId in command-local state. Read updated HP before each reaction. Each blast uses its own physical origin and approved protection group. Retain zero-HP structures in the existing collection; do not relax structure identity/pose invariants just to destroy them.
- [x] **E5.5 — Verify GREEN:** rerun E5 filters plus `EnvironmentDamageTests;CombatCommandCalculationTests;TargetingPreviewParityTests`. Final HP, reactions and ordered events must publish together. Supplies uses the same selector, but reward storage/expiry is E6; retain its preparation rejection until then and do not report Supplies ready yet.

### E6 — Held effects, whole-attack protection and interval expiry

**Files:** Create `K/CharacterHeldEnvironmentState.cs`, `A/HeldEnvironmentEffects.cs`, `T/HeldEnvironmentEffectTests.cs`. Modify `K/CharacterCombatFacts.cs`, `B/Character/CharacterData.cs`, `B/Character/CharacterCombatFactsComparer.cs`, `A/CombatCommandEffects.cs`, `A/CombatDamageCalculator.cs`, `A/CombatCommandCalculator.cs`, `A/MovementProjection.cs`, `A/CombatPlacementCalculator.cs`, `A/CombatTurnCalculator.cs`, `A/CharacterSettlementCalculator.cs`, `A/PositionTileModifierCalculator.cs`, `A/BattleCompletionCalculator.cs`, `K/CombatEvent.cs`. Extend `F/CharacterBattleSettlementTests.cs`, `F/CharacterBufferStateTests.cs`, `F/CoreStageCombatCycleTests.cs`, `F/CombatCommandPublicationTests.cs`.

**Gate:** E6 behavior is confirmed by the user's five answers on 2026-10-06; see G3. Actual Supplies balance remains unspecified and requires explicit fixture values. Badge field consumption and the Mine source are E7 consumers of the completed held acquisition/protection contract; E6 does not activate those production sources.

**Interfaces:** Add `CharacterHeldEnvironmentState HeldEnvironment` to Character facts, immutable and empty by default. Initially store only the approved Badge reference/remaining use and distinct Supplies attack contribution. Add `SameValues` and preserve this snapshot in all copy paths. `HeldEnvironmentEffects.ExpireSupplies(CharacterCombatFacts character) : CharacterCombatFacts` removes exactly that contribution. A command-local `HashSet<(EntityId Target, long AttackGroup)>` records protected target/group pairs; it is calculation state, never a Buffer or persistent turn-wide shield. Group IDs are assigned per actual attack occurrence, not per hit, command or actor turn.

**Executed file-map extensions:** existing ArtifactAttackPowerEvaluator reads the independent Supplies contribution; SoulEffectCalculator and CharacterBuffer.BuildFacts preserve held state; MovementContextCalculator compares relevant held values. Existing movement/Obstacle coordinators share one command-local group allocator. InitialObstaclePlacementSpec and ObstacleContentProfile carry explicit hit/destruction reward amounts through preparation and expected-state comparison. CombatCommandEffects remains the Trait/Category bridge; held effects use the existing damage-application boundary without a fake Trait or a new effect engine.

- [x] **E6.1 — Add failing Badge cases:** `Badge_BlocksEveryHitOfOneAttack` asserts three positive hits cause zero HP loss and consume one use; `Badge_NextIndependentAttackDealsDamage`; `Badge_DuplicateDoesNotAddProtection`; `CopyAndRecall_DoNotRechargeBadge`. Add the chosen zero-damage, instant-death and environmental group assertions only after G3 answers.
- [x] **E6.2 — Add lifetime/publication cases:** `Supplies_ExpiresBeforeRecallIncludingMain` asserts the contribution survives ordinary actions, disappears before day recall and stays absent on redeployment; unrelated contributions and retained held state survive. `StaleHeldState_RejectsWholeCommand` asserts no source consumption, HP change or result publication.
- [x] **E6.3 — Establish RED:** run `Invoke-EnvironmentTests 'HeldEnvironmentEffectTests;CharacterBattleSettlementTests;CoreStageCombatCycleTests;CombatCommandPublicationTests' 'E6-red'`.
- [x] **E6.4 — Implement acquisition and damage consumption:** bridge typed held-state transitions through existing effect/application code, without encoding Badge as an unrelated Trait. First protected hit plans use consumption; later hits check the protected pair. Character publication compares expected held values. Ordinary HP clamps and explicitly classified instant kill bypass ordinary damage shielding as approved.
- [x] **E6.5 — Implement Supplies boundary:** E5's chosen reaction grants its corresponding approved contribution to the credited eligible Character. Expire it at night→day before recall, not when a Character happens to leave a tile or only when the stage clears. Terminal cleanup also prevents carry into a later battle. Keep Flag/Badge terminal behavior behind its own approved lifetime rule.
- [x] **E6.6 — Verify GREEN:** rerun E6 filters plus `CharacterBufferStateTests;CombatCommandEffectTests;MovementEnvironmentTests;CombatCommandCalculationTests`. Assert the new state survives movement, intent/project/merge, turn resets and field/hand copies; do not reconstruct charge from a definition.

### E7 — Tile-entry Object order, Mine and consumption

**Files:** Create `K/ObjectSourceFact.cs`, `K/EnvironmentProgressFacts.cs`, `A/ObjectArrivalCalculator.cs`, `T/ObjectArrivalOrderTests.cs`. Modify `K/CombatCommandState.cs`, `K/IMovementTileRule.cs` only if a narrow contextual target overload is needed, `A/MovementEnvironmentCalculator.cs`, `A/MovementProjection.cs`, `A/MovementContextCalculator.cs`, `A/BattleEnvironmentPreparation.cs`, `CS/StageEnvironmentAdapter.cs`, `DB/CombatStageDetail.cs`, `K/CombatEvent.cs`. Extend `T/MovementEnvironmentTests.cs`, `T/MovementContractTests.cs`, `F/MovementPublicationTests.cs`. Add concrete rule registration only in `S/Factory/Runtime/InGame/Core/InGameCoreSystemFactory.cs` when its supported arrival behavior is complete.

**Gate:** G4 and Mine portions of G2/G3. Order/liveness orchestration can first use explicit test rules and authored ordinals; do not call that production Flag or Mine implementation.

**Confirmed-arrival file extension:** `K/InitialObjectPlacementSpec.cs`, `CS/BattleDefinitionAdapter.cs`, `A/CombatPlacementCalculator.cs`, `A/CombatTurnCalculator.cs`, `A/CombatCommandCalculator.cs` and `K/BattlePreparation.cs` carry authored order, approved deployment/redeployment/attack-knockback arrival and progress copying. Existing Framework tests in `F/StageEnvironmentAdapterTests.cs` and `F/EnvironmentEntryPublicationTests.cs` verify the actual adapter and Factory/Buffer path. No Framework infrastructure or additional Buffer owner is introduced.

**User answers during E7:** initial list order; voluntary and forced arrival; player deployment and enemy night redeployment; existing Hazard → ordered Objects → existing Recovery with death/terminal/relocation interruption. Origin and checkpoint return do not synthesize Object arrival. The user rejected the proposed center-n/neighbor-half-n interpretation, then confirmed its replacement on 2026-10-06: the full 3x3 (center plus eight diagonal-inclusive neighbors) applies identical explicit N raw damage, and existing tile-arrival activation remains. Clip the mask to the board, damage each overlapping live field Character/structure once regardless of faction/contact count, and retain ordinary Terrain/Badge calculation. Actual N remains an explicit fixture input until G7.

**Interfaces:** `ObjectSourceFact : MovementTileFact` adds immutable `long EntryOrdinal` and typed content payload (`Flag`, `SheriffBadge`, `Mine` only as implemented). Identity and entry ordinal are separate; `SameValues`/visibility copies retain both. `EnvironmentProgressFacts` initially owns only `NextObjectEntryOrdinal` in `CombatCommandState`; state equality and every state reconstruction preserve it. `ObjectArrivalCalculator.OrderedAt(MovementEnvironmentFacts environment, EnvironmentSemanticBindings bindings, CellCoord cell) : IReadOnlyList<ObjectSourceFact>` selects active Objects and orders by entry ordinal; duplicate ordinals are invalid, not broken by ID/Priority. Initial order is explicit authored input under G4.

- [x] **E7.1 — Add failing sequence cases:** `EarlierPickup_PrecedesLaterHazardDespitePriorityAndId` reverses IDs/Priorities relative to entry ordinals; assert pickup event/state precedes damage. `LethalMine_LeavesLaterObjectActive` asserts death then no later activation/consumption. `Consumption_BlocksReturnEvenWhenDamageIsBlocked` asserts existing barrier and unchanged attack-refund rules. Use test pickup rules before real Flag exists; E8 supplies the production Flag→Mine case.
- [x] **E7.2 — Add area/knowledge cases:** `Mine_TargetsCurrentAreaOccupants` uses approved geometry and all eligible HP-bearing targets; `Mine_CannotBeDamagedOrTriggeredByBlast`; `HiddenMineVictims_DoNotLeakIntoPreview`; `OrdinalCounter_PreservedOnRejectedPublication`. Pin Badge→Mine and Mine→Badge under the approved environmental group policy.
- [x] **E7.3 — Establish RED:** run `Invoke-EnvironmentTests 'ObjectArrivalOrderTests;MovementEnvironmentTests;MovementContractTests;MovementPublicationTests' 'E7-red'`.
- [x] **E7.4 — Extend the existing coordinator:** at the approved Object/non-Object boundary, process one Object at a time against current projected facts, settle damage/death, then inspect the next. Keep non-Object Hazard/Recovery processing and route-cell order. `ObjectArrivalCalculator` is a helper of that coordinator, not a second command runner. Do not preconsume the whole list.
- [x] **E7.5 — Implement target discovery and selected-source replacement:** the existing `TargetIds(tile, mover)` lacks spatial context. Resolve Mine candidates in the coordinator using the current board/occupants before evaluating its effects, or add a narrow contextual overload retaining existing rule behavior. Validate all targets; do not bake EntityIds into authored Mine data. Deactivate only the selected source through existing strict `MovementStepEffects`/projection identity checks. Keep spawn/relocation out of this API.
- [x] **E7.6 — Verify GREEN:** rerun E7 filters plus `EnemyMovementPathTests;EnvironmentEntryPublicationTests`. Include initial-order/counter validation and ordinary command copies. Existing Object bindings may refer to consumed inactive sources without resurrecting them. Report logical order readiness separately from deferred visuals and any gated arrival causes.

### E8 — Dynamic publication and real Flag lifecycle

**Files:** Create `K/EnvironmentBindingsChange.cs`, `A/FlagDropCalculator.cs`, `T/FlagLifecycleTests.cs`, `F/DynamicEnvironmentPublicationTests.cs`. Modify `K/EnvironmentProgressFacts.cs`, `K/CombatCommandTransition.cs`, `K/CharacterHeldEnvironmentState.cs`, `A/HeldEnvironmentEffects.cs`, `A/CombatCommandComposition.cs`, `A/CombatCommandCalculator.cs`, `A/MovementProjection.cs`, `A/CombatTurnCalculator.cs`, `A/CharacterSettlementCalculator.cs`, `B/Combat/CombatStateData.cs`, `B/Combat/CombatStateBuffer.cs`, `B/Core/BufferSystemGroup.cs`, `K/CombatEvent.cs`.

**Gate:** G5 and applicable G3/G4 lifetime/arrival rules. No allocation/RNG framework before this real consumer.

**Interfaces:** `EnvironmentBindingsChange(EnvironmentSemanticBindings expected, EnvironmentSemanticBindings final)` is optional on `CombatCommandTransition`; absent means preserve current bindings. `WithCompletion`, projection and merge retain the proposal's original expected and final values. Extend `EnvironmentProgressFacts` with battle-local `NextSourceId` and deterministic `RandomState`; initialize above all initial source IDs, never reuse retired IDs, and never borrow Character `NextEntityId`. Use a project-local deterministic algorithm with pinned fixture sequences; its state belongs to expected/final command facts. `FlagDropCalculator` consumes pre-death anchor, approved held Flag payload, current projected occupancy and progress; returns proposed sources, bindings, progress and events, without publishing. Finalize its result shape after G5 determines multiplicity/fallback outcomes.

- [x] **E8.1 — Record G5 and add failing Flag tests:** approved Priority operation changes both effective Character value and participant/forecast at approved timing; `Flag_RecallRetainsHeldState`; `Flag_DeathDropsExactlyOnce`; `Flag_DropAppendsToOccupiedObjectTile` preserves old Objects and assigns a later ordinal. `FlagThenMine_ActivatesFlagBeforeLethalDamage` verifies real acquisition, subsequent death/drop and unprocessed sources.
- [x] **E8.2 — Add atomicity cases:** `StaleBindings_RejectsSourceCreation`, `InvalidFinalPlacement_InstallsNothing`, `PreviewOrRejectedCommand_DoesNotAdvanceRandomOrIds`, `ConsumedHistoricalBinding_DoesNotRejectUnrelatedCommand`. Assert Characters/HP/held state, sources, bindings, all counters, turn facts and result sequence remain unchanged on failure.
- [x] **E8.3 — Establish RED:** run `Invoke-EnvironmentTests 'FlagLifecycleTests;DynamicEnvironmentPublicationTests;ObjectArrivalOrderTests' 'E8-red'`.
- [x] **E8.4 — Extend existing prepare/install:** validate expected bindings as well as command state, new references, source identity uniqueness, supported capability, bounds, final coexistence and affected Character contributions. Prepare every changed store/result before any installation. Do not blindly apply the initial live-reference check to all retained consumed bindings.
- [x] **E8.5 — Implement acquisition/drop using that proposal:** use recorded death anchors before Character removal and a command-local resolved-death set; remove held Flag once and emit one approved drop per held item. Select fallback only from approved nearest candidates with proposed RNG progression. No View callback creates the drop. Reconcile affected cells through existing presence calculation; Flag acquisition does not itself make Priority a Framework scheduling value.
- [x] **E8.6 — Verify GREEN:** rerun E8 filters plus `CombatCommandPublicationTests;EnvironmentEntryPublicationTests;CharacterBattleSettlementTests;CoreStageCombatCycleTests`. Stale failure must preserve allocator/RNG and never leave a new source without its binding. This is a useful atomic-publication review checkpoint if the user requests review.

### E9 — Tornado on the completed-unit-turn clock

**Files:** Create `A/TornadoCalculator.cs`, `T/TornadoEnvironmentTests.cs`. Extend typed Terrain/progress facts only with the approved timed-source payload; modify `A/CombatGimmickCalculator.cs`, `A/CombatTurnCalculator.cs`, `K/CombatTurnFacts.cs`, `A/EnvironmentPresenceReconciler.cs`, `A/BattleEnvironmentPreparation.cs`, `CS/StageEnvironmentAdapter.cs`, `K/CombatEvent.cs`. Extend `F/DynamicEnvironmentPublicationTests.cs`, `F/CoreStageCombatCycleTests.cs`, `T/CombatViewBoundary/StageGimmickViewBoundaryTests.cs`.

**Gate:** G6 and approved arrival/placement policies from G4. Keep Railway timing behavior unchanged. Existing forced movement only supports one-cell displacement; it is not a Tornado teleport implementation.

**Interfaces:** `TornadoCalculator.IsDue(long completedUnitTurns, long nextDueCompletedTurn) : bool` compares the approved due fact to the existing completed-unit count; recurring interval is exactly 3. The existing turn/gimmick coordinator invokes the calculation once at the chosen phase. Relocation uses E8's command-local source/progress proposal and existing Character changes, reconciles old/new-cell presence, and invokes E7 arrival only for the approved cause. Finalize whether due state extends the existing gimmick fact or a typed source payload after G6; store it once, never in both.

- [x] **E9.1 — Pin due/reset behavior and failing tests:** `MixedFactionTurns_AdvanceOneClock` counts allied/enemy/allied completions as three; `QueriesAndMovement_DoNotAdvanceClock`; approved first-due and night/day reset cases; `DueAction_NotRepeatedAfterPublication`.
- [x] **E9.2 — Add relocation cases:** unit and Tornado respawn use their independently approved candidate sets; empty/no-candidate paths assert exact approved outcomes; `Relocation_ReconcilesBothCellsBeforeArrival`, `RejectedRelocation_PreservesRandomAndTurn`, simultaneous-source order and battle-end cases. Do not assume a no-candidate result consumes RNG or advances due time without a rule.
- [x] **E9.3 — Establish RED:** run `Invoke-EnvironmentTests 'TornadoEnvironmentTests;DynamicEnvironmentPublicationTests;CoreStageCombatCycleTests;StageGimmickViewBoundaryTests' 'E9-red'`.
- [x] **E9.4 — Implement the due action:** use one expected/final command projection for movement, Terrain replacement/respawn, presence, approved arrival, deaths and next-due/RNG changes. Same-source relocation may retain source ID, but uses explicit dynamic validation rather than weakening selected-source consumption checks. Forecast must use the same due facts without consuming randomness or performing relocation.
- [x] **E9.5 — Verify GREEN:** rerun E9 filters plus `StageEnemyAiTurnTests;MovementEnvironmentTests;CombatFlowIntegrationTests`. Assert existing Railway forecast and progression remain unchanged.

### E10 — Integration evidence and activation handoff

**Files:** Extend `F/CombatMovementViewTests.cs`, `F/CombatFlowTests.cs`, `F/CombatFlowIntegrationTests.cs`, `T/CombatViewBoundary/StageGimmickViewBoundaryTests.cs`. Modify `V/CombatViewSystem.cs` and `V/CombatResultPlayback.cs` only for new logical result compatibility through their existing owner. Update this plan's evidence/status and create `docs/superpowers/handoffs/2026-10-05-environment-content-activation-handoff.md` when implementation evidence exists.

**Interfaces:** accepted immutable `CombatEvent` sequence is the presentation input. Record source identity, captured position, targets and before/after facts needed after consumption/destruction. No content calls `AcknowledgePresented` independently. Visual assets, stacked rendering and authored stage activation are separate owner work.

- [x] **E10.1 — Add failing boundary tests:** `ContentResult_IsPresentedAndAcknowledgedOnce`, `ConsumedSource_PresentationUsesCapturedFacts`, `RejectedContentCommand_PublishesNoResult`; mixed move→pickup→damage→death→drop trace preserves logical order. Test targeting enter/cancel versus successful attack Return behavior unchanged.
- [x] **E10.2 — Establish RED:** run `Invoke-EnvironmentTests 'CombatMovementViewTests;CombatFlowTests;CombatFlowIntegrationTests;StageGimmickViewBoundaryTests' 'E10-red'`.
- [x] **E10.3 — Integrate result compatibility:** teach the existing presentation owner to consume the new logical records without state mutation callbacks or another scheduling loop. Do not substitute a second per-content acknowledgement or claim deferred animations/layouts are delivered.
- [x] **E10.4 — Verify GREEN and regression:** rerun E10 filters, then `Invoke-EnvironmentTests '' 'environment-full-regression'` for the three listed assemblies. Record actual totals, failures/skips, XML/log paths and tested revision; never reuse Foundation's old test totals as new evidence. Run broader checks only for new changes or unresolved failures.
- [x] **E10.5 — Write the activation handoff:** for each of the eight contents, list completed tasks, unresolved gates, exact supported arrival/damage/lifetime cases, tests, content identity/DB needs and presentation needs. Link actual authoring owners: `DB/CombatStageData.cs`, `S/DataBaseSystem/Data/InGame/Table/Structure/StructureDataTableSO.cs`, `S/DataBaseSystem/Data/InGame/Table/Structure/Item/StructureDataSO.cs` and `StructureData.cs`, plus `CS/DatabaseBattleInitializationSource.cs`. Do not remove T1/G7 or create production assets as part of this handoff.
- [x] **E10.6 — Verify scope:** review status/diff for unrelated audio, generated files, historical plans, Framework infrastructure, assets/scenes and unintended assembly changes. Report logical readiness separately from production-stage readiness and deferred stacked visuals; commit/push only on a new explicit request.

## 5 Completion and continuation rules

A task is complete only when its required policy decisions, consumer integration and focused verification are complete. A successfully compiled helper or fixture does not make its content production-ready. Leave gated steps unchecked; an implementation report must name both completed portions and blocked portions without reopening already-confirmed gameplay.

| Design requirement | Owning task/evidence |
| --- | --- |
| Current Cell composition, semantic ownership, stage preparation | E1/E4, query/entry tests |
| High Ground cost and existing movement ledgers | E2, dual-budget and Return tests |
| Sandstorm range and both Terrain defenses | E3/E4, query/AI/damage parity |
| Obstacle HP, exclusive reactions, Barrel chains | E5, faction/footprint/exact-once tests |
| Supplies acquisition and pre-recall lifetime | E5/E6, Main and redeployment tests |
| Badge attack grouping and non-stacking held state | E6/E7, multi-hit, copies and pickup tests |
| Mine and same-tile Object entry order | E7, geometry, immunity, death interruption and hidden knowledge |
| Flag Priority, death drop and dynamic publication | E8, scheduler agreement and all-or-nothing tests |
| Tornado clock, relocation and random progression | E9, both factions, due/reset and rejected publication |
| Framework/Buffer/View boundaries and default DB gate | E10 plus each publication task |

E1–E5 are complete, including static Terrain and terminating Barrel chains. Review after E5, before E6, is the recommended next checkpoint: damage provenance, multi-hit reactions and atomic publication now meet in a concrete consumer. No review starts automatically. E6 is the next implementation task only when requested, with its remaining acquisition/protection policies resolved locally. Do not add the later allocator, Flag payload or Tornado machinery ahead of its task.

Planning verification: source paths and current integration boundaries were inspected; this document's coverage, proposed type names, dependencies and review-focus tests were self-reviewed. Initial plan authoring performed no implementation or Unity test run. Subsequent E1 execution evidence follows.

## 6 E1 execution evidence — 2026-10-05

E1 added `StaticTerrainFact` and the exact local High Ground/Sandstorm formulas, captured semantic bindings on command input, preserved them through production input copies and test builders, excluded static Terrain from arrival-rule dispatch, and validated expected bindings before Combat publication. Classification comparison uses sets within separate namespaces; an equivalent reordered input is accepted without replacing installed metadata.

- Baseline: Unity EditMode 67/67 passed for existing query, publication, movement-contract and environment-entry fixtures.
- Contract RED: Unity compilation reported missing planned Terrain types and the `environmentBindings` input parameter. Test-only setup errors were corrected before behavioral evidence was recorded.
- Behavioral RED: 71 tests, 68 passed and 3 failed. Failures demonstrated lost binding copies, static Terrain dispatched as an arrival rule, and stale bindings incorrectly accepted for publication.
- Focused GREEN: 84/84 passed. Additional publication cases then covered stale subsets of nonempty current bindings and equivalent reordered bindings.
- Final regression: **2,375/2,375 passed, zero failures/skips**, Unity process exit 0, across `ProjectL.Tests.Core` (1,151), `ProjectL.Tests.Framework` (1,218), and `ProjectL.Tests.CombatViewBoundary` (6).
- Final evidence: `C:/Temp/ProjectL-Environment-25aa50dc-8750-4673-80d1-876635ebb628/E1-full-regression.xml` and its adjacent log. RED evidence: `C:/Temp/ProjectL-Environment-ef52bf1f-d9d5-4546-9c9b-9ee2d3f944b6/E1-red-behavior-fixed-import.xml`.

The original feature checkout was retained for continuity with the user's existing branch and untracked plan/spec. The sandboxed Editor launch produced no XML; only our launched processes were stopped, and the automatically approved unsandboxed Unity runner supplied the recorded evidence. Editor-generated audio-catalog changes were restored from the captured incoming file, preserving the user's earlier change.

E1 does not enable static Terrain preparation, apply its surcharge to real route pricing, integrate damage/range consumers or activate authored content. Those remain E2/E4 work. No automatic code review, commit or push was performed.

## 7 E2 execution evidence — 2026-10-05

E2 integrates the shared low-to-high surcharge into whole-route admission, requested Preview quotes, actual entered-edge payment, movement range and AI. Each ascent costs base AP +2 and one step; descent and plateau travel retain base cost. Return refunds the recorded paid ledger even after Terrain changes. Existing arrival-effect sequencing, death interruption and attack-refund ownership remain intact.

The user confirmed AI route selection: lowest AP first, then fewest steps. Range and AI share nondominated AP/step labels. AI selects a feasible attack-position route within both budgets, or walks an affordable prefix toward the best approach goal when no goal fits this turn. Known-mine avoidance and existing deterministic goal/traversal ties are preserved. Visible Preview approval includes cost-relevant origin/route Terrain and its classification; hidden-only changes neither disclose source IDs nor cause a stale-context signal. Execution still uses authoritative facts.

The pure E1 helper was relocated, retaining its Unity GUID, from Application to Domain/Movement because the Domain route validator cannot depend on Application. A small immutable search label is shared by the two actual consumers; no new service, state owner or assembly was introduced. The test builder now preserves captured bindings. E1's temporary uniform-cost assertion was removed while its static-dispatch/consumption assertions remain.

- Behavioral RED: **43 tests, 31 passed, 12 failed, zero skipped**. Failures covered the absent surcharge, origin/classification staleness, discarded dual-budget paths and uniform-cost AI behavior. Evidence: `C:/Temp/ProjectL-Environment-823a097f-1284-40be-b888-bf7a33c7bbb2/E2-red.xml`.
- First focused GREEN: **135/135 passed**, zero failures/skips. Additional cases then checked actual Buffer publication and Return after Terrain changes, stale origin approval, hidden authoritative costs, lethal early-stop payment, AI equal-AP ties and competing movement budgets.
- Final focused GREEN: **142/142 passed**, zero failures/skips, process exit 0. Evidence: `C:/Temp/ProjectL-Environment-ee4c4380-989b-4543-b621-52bb8c18b074/E2-green-publication.xml`. A missing test-only snapshot import was corrected before this successful run.
- Full regression: **2,396/2,396 passed**, zero failures/skips, Unity process exit 0: Core 1,169; Framework 1,221; CombatViewBoundary 6. Evidence: `C:/Temp/ProjectL-Environment-9a9fec97-5760-4e54-949d-10ccbbc29bfb/E2-full-regression.xml` and its adjacent log.
- Tested revision: uncommitted E1/E2 changes on `feature/Cheon/refactor/core-system`, base `0b7e4a07`. The incoming audio file was restored byte-for-byte after Editor execution; the completed Foundation plan is unchanged.

E2 is complete with no remaining AI-policy gate. E3–E10 remain unexecuted. Static Terrain stage authoring/preparation activation remains E4 work; no production assets, scenes, Framework infrastructure, new Buffers or damage/range content were added. No automatic code review, commit or push was performed.

## 8 E3 execution evidence — 2026-10-05

E3 extracts the legacy target-specific attack wrapper into `CombatDamageCalculator.Attack` without changing the existing `TargetDamagePipeline` or `CombatEffectEngine`. Literal characterization cases preserve primary/additional splash scaling, Category half-up arithmetic, incoming Heavy reduction and the categoryless structure path. Existing pierce, knockback and attack-preview consumers continue through the same calculation.

`CombatDamageOrigin` stores the physical cell independently of the optional credited Character. Attack HP events capture the attacker cell for direct/pierce damage and the selected center for splash damage. Legacy Thorn route entry, forced entry, attack knockback and immediate Thorn application now pass origin through the environmental calculation boundary. Stored fields have unknown credit; immediate caster-created Thorn damage carries the caster ID. Rules can supply an explicit origin, and existing rules receive their selected source cell as the fallback. Environmental eligibility remains caller-owned, including damage to structures and friendly/self targets where the existing caller allows it.

Character/structure HP events copy a read-only list of immutable origins. This keeps every source in aggregated multi-cell Thorn settlement without splitting its existing HP event. Tests preserve distinct damage/healing records, forced Thorn death location, structure destruction facts, and the physical location/credit after a source becomes inactive. Max-HP normalization and authored instant kill remain on their existing paths. The environmental boundary currently passes legacy amounts through; Terrain percentages, rounding stages, Badge grouping/consumption and new content activation are not implemented by E3.

- Contract RED: the compiler reported the missing planned damage calculator/origin types and event/request provenance. Evidence: `C:/Temp/ProjectL-Environment-a3e0a47c-71bd-4382-a9f6-3e10463183a2/E3-red-contracts.log`.
- Behavioral RED: **85 tests, 81 passed, 4 failed, zero skipped**, after correcting test-only Heavy/profile consistency. The failures demonstrated missing origins in attack, stored Thorn and consumed-source damage events. Evidence: `C:/Temp/ProjectL-Environment-c748e93d-d9cb-4950-b202-1a5f33ecd42d/E3-red-behavior-fixed-fixture.xml`.
- Focused GREEN: **131/131 passed**, zero failures/skips, Unity process exit 0, including the specified attack, splash, Category, movement-environment and knockback fixtures. Evidence: `C:/Temp/ProjectL-Environment-9ec64a6e-d64b-4180-8796-5fbc8771ca65/E3-green.xml`.
- Final full regression after additional event assertions: **2,407/2,407 passed**, zero failures/skips, Unity process exit 0: Core 1,180; Framework 1,221; CombatViewBoundary 6. Evidence: `C:/Temp/ProjectL-Environment-4554ab57-9d0d-48ef-b1d0-ccc32ed3018e/E3-full-regression.xml` and its adjacent log.
- Tested revision: uncommitted E1–E3 changes on `feature/Cheon/refactor/core-system`, base `0b7e4a07`. The incoming audio file was restored byte-for-byte after the Editor runs. Completed Foundation plans, Framework infrastructure, Buffers, assets and scenes received no E3 changes.

The initial contract-error Editor had already exited while its runner shell remained waiting; only that verified task-owned shell was stopped after retaining its error log. Later behavioral/GREEN runs exited normally. E3 is complete. E4–E10 remain unexecuted; no automatic code review, commit or push was performed.

## 9. E4 execution evidence — 2026-10-06

G1 was closed by the user's answers before dependent integration: Terrain percentages follow existing matchup/PhysicalDefense/Heavy settlement, preserve decimal intermediate values and floor final damage. Sandstorm reads current AppointRange after other bonuses and preserves 0/1. Transit defense reads the current cell. Unknown physical origin receives Sandstorm defense but no High Ground defense. One-cell knockback blocks only low-to-high; plateau/descent remain allowed and Return is exempt. Multi-cell displacement remains unsupported.

Implemented immutable static placement data through StageEnvironmentAdapter and BattleDefinitionAdapter into real preparation/publication. Static Terrain queries derive range/defense from current facts, including hypothetical AI positions, without mutating base stats or adding Buffers. Supported initial static placement does not synthesize arrival. Unsupported same-cell static/other Terrain composition is rejected as a current capability limitation, not a permanent gameplay rule. Default production T1/G7 guards remain intact.

Behavioral RED: 91 tests, 84 passed, 7 failed, 0 skipped; evidence `C:/Temp/ProjectL-Environment-8b70c65b-78b7-41b7-9447-bc1f502d0c3b/E4-red-behavior-fixed-import.xml`. Additional corrected-direction RED: 22 tests, 20 passed, 2 failed; explicit Knockback ascent and missing Sandstorm expected-context classification, evidence `C:/Temp/ProjectL-Environment-051b0789-a113-4937-88c9-c12daebc0e63/E4-red-context-displacement-corrected.xml`. Compiler-only fixture/setup attempts are not behavioral evidence.

Focused GREEN: 193/193 passed, zero failures/skips, process exit 0; evidence `C:/Temp/ProjectL-Environment-b4469f2b-6796-4dfb-a78f-320fba804997/E4-green-final.xml`. Covers direct/environmental/originless/transit defense, sequential final-floor arithmetic after Heavy, Sandstorm 0/1/2/4/5 range, actual pierce reach, hypothetical AI position, visible stale context and hidden facts, classified/unclassified forced movement, knockback directions, and real Factory/Buffer initial preparation, Move publication and Return.

Final full regression: 2439/2439 passed (Core 1208, Framework 1225, CombatViewBoundary 6), zero failures/skips, process exit 0; evidence `C:/Temp/ProjectL-Environment-29a6ab69-078b-484e-9bcf-1bc6cec526fd/E4-full-regression.xml`.

Scope decisions: cross-consumer assertions live in StaticTerrainIntegrationTests instead of duplicating existing targeting/knockback fixtures. MovementProjection records only command-local Terrain read cells so public context can validate damage and blocked knockback dependencies; hidden facts remain excluded. Generic sequential effects, route-cell ordering, damage/death settlement and existing unclassified forced movement are preserved. Production assets/scenes, Framework/Buffer infrastructure and historical Foundation documents are unchanged by E4. The incoming audio asset was restored byte-for-byte after Editor runs. E4 is complete; E5–E10 remain unexecuted. No automatic review, commit or push was performed.

## 10. E5 execution evidence — 2026-10-06

Status: complete for the authorized E5 system scope, with explicit fixture values only. Actual initial HP and Barrel explosion n remain undecided. No production defaults, SO/query mappings, assets, scene placements or content activation were introduced. Supplies shares the reaction contract but initial placement stays rejected until E6 implements acquisition/storage/expiry. E6–E10 remain unexecuted; no review, commit or push was performed.

Confirmed rules: both factions can directly attack Obstacles; each positive-damage hit produces either Hit or Destroyed, never both; zero damage and already-dead targets produce no reaction. Barrel explodes on destruction only. Each blast hits every living overlapping Character/structure once regardless of footprint contact count or faction. Each Barrel blast has a separate command-local attack group, retains its own physical anchor and preserves the original credited Character throughout chains. Terrain/Object sources remain immune and unchanged.

Implementation: ObstacleContentProfile stores immutable definition association and explosion amount without duplicating HP/pose. InitialObstaclePlacementSpec uses explicit definitions/values; existing preparation assigns runtime IDs and validates footprints/coexistence before deriving bindings/profiles. Distinct Barrel/Supplies contract keys use the existing Owner.Neutral narrowly; Headquarters keeps its original meaning. Profiles survive stage adaptation, preparation, CombatStateData, service capture, intent/project/query/hypothetical/movement copies. PrepareCommand rejects stale profiles before installing any state or result.

One command-local ObstacleReactionCalculator serves attacks and environmental structural damage during movement. Direct Obstacle hits settle separately instead of aggregating multi-hit damage. A queue processes newly destroyed Barrels against latest HP; alive-to-dead checks and destroyed IDs prevent repeated explosions in converging chains. Zero-HP structures keep identity, definition, max HP, anchor, rotation and full footprint, while existing live-cell queries release their occupancy. Structure destruction grants no Character soul reward. Existing damage/death/outcome settlement and generic sequential movement processing remain authoritative. Shared environmental damage applies receiver Terrain defense using each blast's physical source; movement preview captures relevant visible profile/Terrain dependencies.

RED evidence:

- Primitive contract APIs missing: `C:/Temp/ProjectL-Environment-2715a1c1-3a54-48d7-bcf2-9aca6bd7131e/E5-red-contracts.log`.
- Integration contract APIs missing: `C:/Temp/ProjectL-Environment-2f80dd67-218b-489d-9292-da080c5d0d22/E5-red-chains.log`; corrected test-only signature/ID setup separately.
- Behavioral integration RED: 10 cases, 4 failures for absent blast/reaction/copy behavior; `C:/Temp/ProjectL-Environment-adeefe88-91cc-4ff6-aebc-55973d8f5936/E5-red-chains-behavior.xml`.
- Movement boundary RED: 80 cases, 2 failures; `C:/Temp/ProjectL-Environment-d43c7804-5fc1-4e73-a2a7-e0cc65ad65a8/E5-red-movement-boundary.xml`. One was the missing route-triggered blast; the other used an incorrect pierce geometry fixture, corrected to the existing diverging rays without changing that behavior. Compilation/setup failures are not behavioral RED.

GREEN evidence: focused 211/211 in `C:/Temp/ProjectL-Environment-b4242b2d-2f85-48f7-b7dc-c0e4f096f08d/E5-green-final.xml`; additional actual Core entry/attack, both-faction query, full-footprint release and stale movement-profile cases 123/123 in `C:/Temp/ProjectL-Environment-c7eee6f0-1fc6-4beb-9214-d557aabbf61d/E5-green-core-flow.xml`. Both had zero failures/skips and exit 0.

Final fresh full regression: **2465/2465 passed** (Core 1230, Framework 1229, CombatViewBoundary 6), zero failures/skips, process exit 0. Evidence: `C:/Temp/ProjectL-Environment-095e0de2-5af4-44a5-b44b-ae5018930649/E5-full-regression.xml`.

Implementation rulings: use typed contract identities without production activation; permit the existing neutral owner only for those identities; require explicit explosion amounts rather than silently supplying zero; integrate the same command-local reaction calculator into existing attack and movement projections; reuse immutable placement specs in stage data instead of duplicating an authoring DTO; allocate separate blast groups locally, leaving unrelated environmental grouping to E6/E7. These choices add no Framework infrastructure or Buffer owner. If later authoring/group requirements differ, adapt their contract mappings/boundaries rather than introducing unapproved gameplay defaults.

Recommended mid-review: after E5 and before E6, when common damage, multi-hit reactions, chains and publication can be checked together. A further checkpoint after E7 and final E10 review remain useful. The timing recommendation does not authorize automatic review.

## 11. Post-E5 mid-review and targeted refactoring — 2026-10-06

The user authorized a mid-review after E5 and fixes/refactoring for concrete structural problems, without starting E6. A fresh read-only reviewer inspected the uncommitted E1–E5 changes against this plan/spec and the current consumers. No Critical issue was identified. Two actionable correctness issues were independently reproduced before correction:

| Finding | Root cause and correction | Regression evidence |
| --- | --- | --- |
| P1: direct multi-ray damage plus a Barrel blast aborts a valid attack | AttackTargetPreview required the direct ray sum to equal the whole command damage, although the command included a separate blast. Add explicit SecondaryDamage, retain direct ray details and require ray subtotal + secondary damage to equal the total. Direct roles and knockback eligibility stay intact. | PierceAndBlast_PreserveRaySubtotalAndCombinedDamage: two rays total 40, secondary blast 7, command total 47 and final HP 53 in execution/preview. |
| P2: AI omits explosion-only hostile damage and friendly victims | AI evaluated only direct AttackPreview targets. An internal Preview overload now returns the same public-filtered pure projection; AI aggregates positive damage-origin HP losses by entity from immutable result events. It also retains direct zero-damage friendly contacts. No second simulation, fake splash role or separate state owner. | Ai_SelectsBarrelWhenOnlyBlastCanReachHostileUnit and Ai_AvoidsBlastOnlyFriendlyVictimWhenSafeAttackExists. |

Targeted structural refactoring centralizes native Barrel/Supplies key recognition in ObstacleContentProfile.IsNativeObstacleKey. Attack, movement, preparation validation and structure ownership share that predicate while retaining their existing validation boundaries and legacy compatibility. A duplicate System.Linq import was removed. No speculative manager, Buffer, trigger bus, scripting layer or new gameplay policy was added.

Behavioral RED: 27 tests, 24 passed, 3 failed, zero skipped; C:/Temp/ProjectL-Environment-24d84e1b-84fd-410c-b8e2-09db9763a760/E5-review-red.xml. Failures were the composed-damage exception, rejected beneficial Barrel aim and ignored blast-only friendly victim. P1 GREEN: 51/51, C:/Temp/ProjectL-Environment-498d3974-5251-4475-9f42-7c26a1f837d7/E5-review-green-composed-damage.xml. P2 plus existing Obstacle/AI regression GREEN: 50/50, C:/Temp/ProjectL-Environment-0466ba47-4097-48cd-a8c9-f93dd3683451/E5-review-green-ai.xml. Both GREEN runs had zero failures/skips and exit 0. Final full verification follows below.

Review boundaries: exhaustive blast UI target/affected-cell presentation remains deferred. Badge spending/cross-command protection and Supplies acquisition/expiry are E6; Object/Mine ordering is E7. Production authoring/assets/balance activation remains gated. Static Terrain does not mutate inside supported blast chains. Inherited pre-E1 thorn occupancy handling was considered but no separate new E1–E5 regression was established; no unrelated rewrite was made. These boundaries do not exclude AI outcome scoring, which has a current consumer and was corrected.

Final fresh full regression after both fixes and structural refactoring: **2468/2468 passed** (Core 1233, Framework 1229, CombatViewBoundary 6), zero failures/skips, process exit 0. Evidence: C:/Temp/ProjectL-Environment-77394785-d609-4fbb-9f1b-6f27a0020696/E5-review-full-regression.xml. Existing tests remain unchanged in their assertions; three new behavioral regressions cover the findings. The review's two Important findings and both minor cleanup items are addressed; no remaining finding requires blocking E6 for code correctness. E6 still requires its own authorization and unresolved gameplay decisions.

Incoming audio bytes were restored after Unity verification; historical Foundation documentation, production assets/scenes and Framework infrastructure remain preserved. No E6 implementation, commit or push was performed. Review status is limited to the implemented E1–E5 slice and does not claim production activation or visual completion.

## 12 E6 execution and verification — 2026-10-06

E6 is complete. CharacterHeldEnvironmentState is immutable, empty by default and authoritative through the existing Character Buffer. It records Badge source/use and a separate Supplies contribution. Character copies, position changes, soul collection, persistent Main rebuild, movement, intent/project/merge and turn resets preserve the state. Character expected-state comparison and relevant movement contexts include its values; stale held/profile proposals reject before any install.

Badge defense runs after ordinary attack/environment arithmetic and Terrain reduction. Zero final damage consumes one use. A command-local target/attack-group pair blocks every later hit of that attack after the held charge is spent; a new independent attack is unprotected. Direct attack, each Barrel blast and each stepped Thorn cell receive distinct appropriate groups. Explicit HP assignment, normalization and Railway instant death bypass shielding. Duplicate held acquisition adds no protection. Day recall retains the current state; terminal completion/abandonment clears it. E7 must consume duplicate field pickups and use one group for one Mine explosion; those production sources are not activated here.

Supplies grants the exclusive per-hit or destruction reward to the living original credited Character. Repeated rewards accumulate; missing/dead credit receives none, including when the causing Character dies in the explosion before reward settlement. Immutable placement/profile facts require explicit reward values and preserve them through preparation and stale checks. Actual attack power reads the separate contribution alongside existing sources; base, soul and positional attack are unchanged. Expiry removes only Supplies for all projected Characters before night-to-day recall, including Main and off-field records. Terminal cleanup removes held effects without changing persistent Main attack. Existing UnitStatsSnapshot.EffectiveAttackPower retains its prior projection meaning; the held contribution is separately readable through CharacterData and included in actual attack evaluation. Visual display remains deferred.

Behavioral RED evidence: copy/lifetime/publication 31 cases, 5 failures (C:/Temp/ProjectL-Environment-b35bf080-684a-438f-8c5b-94ab3f5f5d77/E6-red-lifetime-publication.xml); approved damage 10 cases, 4 failures (C:/Temp/ProjectL-Environment-141c838f-1724-4849-aeef-d0c6d40dfd2a/E6-red-approved-damage.xml); reward/terminal 14 cases, 2 failures (C:/Temp/ProjectL-Environment-325ca3cf-82c1-4553-be59-239f269baec7/E6-red-rewards-terminal.xml); initial Supplies 58 cases, 1 failure (C:/Temp/ProjectL-Environment-fa65d4fa-da51-4ae4-8ddd-5f3b0552b53e/E6-red-initial-supplies.xml). Contract/fixture setup errors are recorded separately in the ledger and are not claimed as behavioral RED. The existing multi-cell Character attack rejection is preserved: three-hit protection is tested at the actual damage-application boundary, with separate real single-cell attack/preview and command composition tests.

Final fresh full regression: **2497/2497 passed** (Core 1254, Framework 1237, CombatViewBoundary 6), zero failures/skips, exit 0. Evidence: C:/Temp/ProjectL-Environment-d1d9923e-eac9-496e-b9f3-62c51d0fe554/E6-full-verified.xml. Dedicated held tests passed 20/20; Core flow also covers Main night-end expiry, Railway bypass and publication/acknowledgment cleanup. Existing unrelated gameplay assertions remain intact; the old Supplies preparation rejection test was replaced by explicit-value readiness and missing-value rejection assertions because E6 intentionally lifts that gate.

Actual Supplies balance/HP, production SO/query mappings, assets, stage activation and visual presentation remain G7. Mine geometry and ordered real Object acquisition remain E7; Flag remains E8. No E7 implementation, automatic review, commit or push was performed. Incoming audio bytes and historical Foundation plan remain unchanged after verification; no new Buffer, Framework infrastructure, scene or production asset was introduced.

## 13. E7 execution — historical checkpoint; superseded by section 14

At this earlier checkpoint E7 was not complete. The user confirmed initial authored list order, voluntary and forced arrival, player deployment, enemy night redeployment, existing Hazard → ordered Objects → existing Recovery, and death/terminal/relocation interruption. The proposed Mine geometry/arithmetic was rejected and a replacement had not yet been recorded. E7.2, the Mine target-discovery portion of E7.5 and complete E7.6 were open; section 14 records their later completion.

Implemented immutable typed Object sources and activation events, per-source entry ordinals, strict selected-source consumption and an expected-state ordinal progress fact. IDs and rule Priority do not define Object order. Initial stage placement lists allocate ascending ordinals; visibility/source copies preserve them. The next ordinal exceeds all existing ordinals, including consumed sources. Every production command-state reconstruction preserves progress; stale publication leaves HP, sources and progress untouched.

The existing MovementProjection and movement-environment coordinator resolve one Object at a time from current projected facts. Consumption sets the existing return barrier even when held protection blocks damage. Death or terminal outcome leaves later Objects active. Sheriff Badge sources are supported and registered only after all approved arrival connections were implemented. Duplicate Badge sources are each consumed without granting additional protection. Initial preparation checks supported typed sources, semantic classification, same-cell order and coexistence; preparing existing presence does not synthesize a placement command or arrival.

Approved connections: each entered voluntary route cell; the destination of existing one-cell forced movement; attack knockback after existing transit hazards and before existing soul/position recovery; hand/player placement; enemy night redeployment immediately after each placement. Route origin and checkpoint return do not trigger Objects. Generic non-Object order and trigger eligibility, movement/AP/refund ledgers, existing one-cell displacement limits and attack hit-health reporting are preserved. Arrival continuations reuse current dictionaries, events, held attack groups and the existing Finish/publication boundary. Preexisting attack deaths retain their prior soul-settlement timing; new Object deaths settle before the next Object. Visible context includes Object sources at forced destinations without including hidden sources or the global ordinal counter.

Evidence:

- Ordinal/state-copy RED: E7-red-progress-copy.xml, 3 cases, 1 failure; C:/Temp/ProjectL-Environment-e0216267-fabb-4c86-af10-73dfb9e496df.
- Sequence RED: E7-red-sequence-behavior.xml, 5 cases, 2 failures; C:/Temp/ProjectL-Environment-a0a5a53c-1f66-4d5d-9dd1-86541f08789d.
- Source-immutability RED: E7-red-immutable-source.xml, 7 cases, 1 failure; C:/Temp/ProjectL-Environment-15a634ff-9da7-4090-ac0a-162f3e502f8c.
- Route RED: E7-red-route.xml, 9 cases, 3 failures; C:/Temp/ProjectL-Environment-61aed8d5-0d84-4b39-8561-429938cb2920.
- Approved placement/knockback RED: E7-red-approved-arrivals-corrected.xml, 11 cases, 2 failures; C:/Temp/ProjectL-Environment-9d5a01e8-915a-446e-a34b-d047ee72087b.
- Initial authoring/preparation/counter RED: E7-red-initial-support.xml, 75 cases, 3 failures; C:/Temp/ProjectL-Environment-637b154c-78e4-4af0-bac4-c9d4c1e594ee.
- Forced-destination context RED: E7-red-forced-context.xml, 78 cases, 1 failure; C:/Temp/ProjectL-Environment-04b51594-e10f-47df-b1c6-efe5263f0b85.
- Focused GREEN: E7-green-confirmed-scope.xml, 178/178; C:/Temp/ProjectL-Environment-61297b68-21a2-4ff0-ac16-879462f44cf5.
- Final full GREEN: E7-full-confirmed-scope-corrected.xml, **2519/2519** (Core 1273, Framework 1240, CombatViewBoundary 6), zero failures/skips, exit 0; C:/Temp/ProjectL-Environment-6ee609d4-74b4-439b-b5d4-426cf1b21db7.

Earlier compiler-only attempts (missing placement contracts, test namespace/member references) and the blocked-knockback test fixture were corrected separately; these are not behavioral evidence. A regression caused by newly triggering generic effects on forced arrival was traced to that added call and corrected by connecting only approved Object activation. The historical movement tests retain their assertions.

Remaining work: approved Mine geometry/raw-damage arithmetic, current-board area target discovery, concrete Mine source/rule, Mine immunity/visibility/chain/group tests and real Badge→Mine / Mine→Badge integration. Fixture damage Objects verify sequence/liveness without representing production Mine or Flag. Flag creation/drop and source allocation remain E8; no new Object appears during E7 resolution. Production balance, assets/SO authoring, stage activation and visual composition remain gated. No E8, automatic review, commit or push was performed; incoming audio bytes and the historical Foundation plan are preserved.

## 14. E7 completion and review verification — 2026-10-06

E7 is complete for the authorized system/fixture scope. The user's replacement rule is **full 3x3 including center and all eight neighbors, uniform explicit N raw damage, existing tile-arrival activation**. The earlier reduced-additional-damage interpretation is superseded. Ordinary receiver Terrain defense, final-floor arithmetic and Badge protection remain applicable; no production balance value was invented.

MineSourceFact and MinePlacementSpec preserve damage, visibility, activity and authored entry order through immutable copies and real initial preparation. MineMovementTileRule discovers recipients from the current command projection: live field Characters of either faction and living structures whose full footprint overlaps the clipped 3x3, each entity once. The existing coordinator consumes only the selected Mine, uses one attack group for its explosion, settles damage/death and subsequent Barrel chains, then considers the next Object. Each Barrel retains its separate group and physical origin; stored Mine credit remains absent. Factory registration enables the approved voluntary/forced arrival, attack knockback, player placement and enemy night redeployment connections. Preparation, route origin and checkpoint Return do not synthesize activation. Direct attacks and blasts cannot damage or detonate Object sources.

Regressions cover clipping and immutable facts; center/diagonal/faction/footprint recipients; latest projected position and dead/off-field exclusion; Badge→Mine versus lethal Mine→Badge; multiple Mines and zero/blocked consumption; Terrain defense; Barrel chains without neighboring Mine detonation; visible stale source/HP/held dependencies and hidden Mine/victim filtering; all approved arrival connections; and actual stage adaptation, Factory/Preview/Buffer publication. No new Buffer, runtime owner, Framework infrastructure or generalized trigger system was added.

Fresh read-only review identified one Important correctness issue and no Critical/Minor findings: a zero-damage or Badge-blocked Barrel-chain recipient outside the Mine mask was absent from movement expected targets although its held Badge changed. Two parameterized regressions reproduced stale prediction acceptance before correction. ObstacleReactionCalculator now records every consulted blast recipient in a command-local set, and MovementProjection includes that set in EffectTargetIds. Existing BlastDamage accounting remains unchanged.

Persisted evidence is under `D:/Github_LocalFile/Project_L/.utmp/E7-completion` (already ignored). Unity cleanup removed some earlier Temp XML files; their observed counts remain in the execution ledger and are not substituted for the following retained final evidence.

| Validation | Result | Persisted evidence |
| --- | --- | --- |
| Mine contract RED | Missing planned Mine API compiler diagnostics; not behavioral evidence | `E7-red-contracts.log` |
| Mine behavior RED | 101 total, 86 passed, 15 failed; 14 missing behavior failures and one subsequently corrected empty-attack fixture assumption | `E7-red-behavior.xml` |
| Review regression RED | Both stale held-state cases failed before the fix | `E7-review-red-corrected.xml` |
| Final focused GREEN | **245/245 passed**, no failures/skips, Unity exit 0 | `E7-review-green-focused.xml` |
| Unchanged HEAD full baseline | **2599 total, 2583 passed, 16 failed**, no skips | `E7-head-baseline-full.xml` |
| Final full regression after review fix | **2623 total, 2607 passed, 16 failed**, no skips; Unity exit 2 | `E7-final-full-verified.xml` and adjacent log |

The final failure-name set equals the unchanged HEAD baseline exactly (zero differences); all **24 added tests passed**. Final assembly counts: Core **1294/1294**, CombatViewBoundary **6/6**, Framework **1307/1323**. The 16 inherited failures comprise CombatDisplayReuse (1), CombatPointerMapping/EventSystem (9), CombatSceneEventPresenter (3), CoreStageCombatCycle (1) and GameBootstrapFaultBoundary (2). They were not skipped or changed. The broader suite remains non-green; these results establish E7's focused EditMode and current full-suite comparison, not PlayMode/gameplay, visual or production readiness.

Execution rulings: continue the existing clean feature checkout at base `3d7b8103`; use the current Unity 6000.0.73f1 executable/project paths through the permitted elevated launch; preserve existing empty-target attack and deployment-depth policies by correcting only new fixtures. A final attempt was blocked while the interactive Editor held the project; after the user closed it, the retained final full run completed normally. Temporary baseline restoration was followed by byte/hash verification of every E7 source/test restoration. Unity-generated FMOD scan/cache metadata is restored to its initial tracked values after verification. Historical Foundation documents and unrelated authored content remain preserved.

Deferred boundaries: Flag/drop/dynamic source allocation and RNG remain E8; Tornado remains E9; E10 is unexecuted. Production SO/query conversion, balance values, stage/scene activation and visuals remain gated. No new simultaneous Mine/Barrel settlement policy, unrelated Framework test repair, commit or push was included.

## 15. E8 Flag and dynamic publication completion — 2026-10-06

E8 is complete for the authorized Core and existing Buffer scope. The user explicitly permits Buffer additions/modifications while retaining the Framework–Buffer relationship. This slice changes Core rules/contracts, existing CombatStateBuffer validation and CombatStateData replacement, Factory registration, tests and these plan/spec records. BufferSystemGroup already prepares every store before installation and needs no new publication phase. View/Flow, Framework infrastructure, scenes, production SO/data values and presentation assets remain outside this slice.

Confirmed Flag behavior: add explicit N to Priority; hold at most one; retain through day recall; drop once on death; clear held Flag at terminal battle settlement. The current action continues. Its next action interval immediately uses the increased Priority, so A → A is possible. A pinned fixture changes Priority 10 → 40: from current time 10, A's next time becomes 13 instead of 20 and precedes a competitor at 15; forecast and actual EndUnitTurn agree. Existing scheduled times are preserved until their action finishes.

Normal drops use the pre-death location and avoid living Character/structure occupancy. Other Objects and Terrain do not block a Flag drop. The user's required exceptional creation under a Character acquires that new Flag immediately when the unit has no Flag. Remaining implementation rulings are explicit: squared Euclidean distance (no diagonal-only rule), deterministic random selection among equally nearest valid empty cells, remove held Flag without allocating/advancing RNG if no candidate exists, and leave a duplicate Flag on the field. A newly created source does not restart the captured arrival sequence; death still preserves later unprocessed Objects.

FlagSourceFact/FlagPlacementSpec and the immutable held payload preserve explicit bonus/visibility. Held-state updates adjust only the Flag Priority contribution and preserve Badge/Supplies; terminal clearing subtracts it. Existing final settlement updates Character, participant and timeline facts together. FlagDropCalculator serves movement/Object death boundaries and common command Finish, so cleared held state prevents repeat drops. Each new Flag gets a fresh environment source ID and later Object ordinal, with a captured creation event.

EnvironmentProgressFacts now owns NextSourceId and RandomState alongside NextObjectEntryOrdinal. Initial allocation is above all retained source IDs, including inactive sources. Fixed LCG/rejection-sampling tie selection has a pinned sequence; calculations and Preview mutate only proposed local progress. EnvironmentBindingsChange is optional expected/final metadata on CombatCommandTransition and survives WithCompletion, movement wrapping, Project and Merge. Existing Buffer preparation checks stale input/bindings/progress, source allocation, typed Object identity, classification, supported content, coexistence and Priority contribution before replacing CombatStateData. Historical consumed bindings may remain absent without resurrection. Legacy non-Object mutation support stays separate from typed Flag creation.

Evidence under `D:/Github_LocalFile/Project_L/.utmp/E8-completion`:

| Validation | Result | Evidence |
| --- | --- | --- |
| Initial contracts | Missing FlagSourceFact compiler diagnostic; not behavioral evidence | `E8-red-contracts.log` |
| Initial behavior | 4 failures: three missing Flag behaviors and one corrected terminal-death fixture assumption | `E8-red-behavior.xml` |
| Dynamic publication RED | 8 total, 4 passed, 4 failed: missing/dangling binding, allocation and binding installation | `E8-red-publication-fixed-fixture.xml` |
| Expanded integration GREEN | 234/234 passed, zero failures/skips | `E8-green-integration-fixed-fixture.xml` |
| Timeline regression correction | 303/303 passed, including all four affected existing CommonCommand cases | `E8-green-timeline-regression.xml` |
| Fresh review RED | Existing Flag ID could be replaced with a positional modifier; 1/1 failed as expected before fix | `E8-review-red.xml` |
| Final focused GREEN | **304/304 passed**, zero failures/skips, Unity exit 0 | `E8-review-green-focused.xml` |
| Final full comparison | **2646 total, 2630 passed, 16 failed**, zero skipped; Unity exit 2 | `E8-final-full.xml` and adjacent log |

Final assembly results: Core **1307/1307**, CombatViewBoundary **6/6**, Framework **1317/1333**. All **23 added E8 tests passed**. Machine comparison found zero failure-name differences against both retained E7 final XML and the unchanged HEAD baseline from section 14. The broader suite remains non-green due to the same 16 Framework View/EventSystem/flow/bootstrap failures; no inherited failure was skipped or fixed as part of E8.

Fresh read-only review found one Important/P2 issue and no other concrete findings: filtering validation by the final Object type allowed an existing Object ID to change to a non-Object. The regression failed before correction; validation now checks surviving old Object identity before type filtering. Earlier full regression caught four pure-calculation fixtures with unrelated timeline IDs absent from their Character facts. Priority synchronization now updates known Characters and preserves existing unrelated entries; those assertions were not changed. New fixture namespace/command-name corrections are not counted as behavioral RED. The existing terrain-removal helper was updated to preserve allocator/RNG rather than reset it; its gameplay assertions remain unchanged.

Final rulings and limits: continue the current feature checkout with completed E7 changes preserved; use the authorized current-machine Unity path; no commit/push. Generated FMOD scan metadata was restored after the final run. New meta GUIDs are unique and `git diff --check` passes. This is EditMode system/fixture evidence, not PlayMode, production authoring, stage activation or visual readiness. E9 Tornado and E10 remain unexecuted.

## 16 E9 implementation and verification — 2026-10-06

The user redirected the task to E9 only and approved the complete G6 policy recorded above. E10 was stopped before changes; no activation handoff or View implementation was created. E9 extends Core and the existing Combat Buffer prepare/install path, preserving Framework ownership and the incoming E7/E8 changes.

Tornado identity, position, activity, visibility and next due point are immutable facts. `IsDue` uses `long`, matching the authoritative completed-unit count. Source IDs/bindings and allocation counters remain unchanged on respawn. Unit relocation is a destination operation with a captured `Relocated` event, no manufactured path or one-cell knockback. Position normalization precedes Hazard → ordered Objects → Recovery; existing forced Soul arrival/death/Flag drop use the latest projection. All due sources resolve inside the ending unit's command, before Supplies expiry/night recall, with no extra command scheduler or acknowledgement.

Buffer preparation explicitly validates same-source Tornado payload, one due increment, completed-turn advancement, captured events, bounds, Terrain/structure coexistence and authorized RNG change without allocation. Stale publication rejects every changed store/result. Source creation, type replacement and Terrain overwrite are rejected. Terminal arrival can retain an already-chosen unit destination and its RNG while stopping respawn and remaining sources. Object identity/consumption checks remain strict.

The battle-wide completed count survives combat re-entry. Railway keeps its separate automatic execution boundary and new-night relative start by offsetting its existing due schedule by that count. Player forecast uses visible Tornado due facts and a typed Environment timeline entry keyed by `EnvironmentSourceId`; it does not invent a StageGimmickId, relocate anything, draw randomness or disclose hidden sources/destinations. The typed Core adapter accepts explicit immutable Tornado fixtures; default production DB activation remains guarded.

Evidence under `D:/Github_LocalFile/Project_L/.utmp/E9-completion`:

| Validation | Result | Evidence |
| --- | --- | --- |
| Initial behavioral RED | 9 total, 3 passed, 6 missing Tornado behavior failures | `E9-red.xml` |
| Publication RED | 25 total, 24 passed; valid timed RNG/relocation rejected by allocation-only guard | `E9-publication-red.xml` |
| Forecast/next-night RED | 19 total, 16 passed; two missing forecasts and Railway relative-night due | `E9-forecast-red.xml` |
| Typed adapter RED | 133 total, 131 passed; two missing adapter capabilities | `E9-adapter-red.xml` |
| Focused GREEN | **276/276 passed**, zero failed/skipped, exit 0 | `E9-targeted-final.xml` |
| Broader focused regression | **313/314 passed**, one inherited registered drag-cycle failure, zero skipped | `E9-focused-green.xml` |
| Full EditMode | **2676 total, 2660 passed, 16 failed**, zero skipped, exit 2 | `E9-full.xml` |

Full assemblies: Core **1326/1326**, CombatViewBoundary **7/7**, Framework **1327/1343**. All **30 added E9 cases passed**. Machine comparison found no failure-name differences against E8 or unchanged HEAD `3d7b8103`; the full suite remains non-green. The existing broad drag-cycle test runs unchanged; the actual Factory environment-entry fixture additionally verifies initialization through the third unit-turn publication. Compiling test API mistakes and the corrected Main-recall fixture assumption are not behavioral RED evidence.

Implementation rulings: use the existing `long` clock; store due on the typed source only; reuse source identity/allocator and existing deterministic candidate selection; preserve Railway through an offset rather than a second clock; use typed Core fixture initialization rather than adding production DB/SO authoring. One-cell-only unit handling remains explicit. No Framework rewrite, View source changes, assets, scene work, balance decisions, commit or push. This is EditMode Core/Buffer fixture evidence; E10, visual playback, PlayMode and production stage activation remain separate.

Fresh review found two Important/P2 publication issues: a valid timed action allowed arbitrary final RNG, and a captured source could stay at its original cell despite available destinations. Both failed in `E9-review-red.xml` (6/8 passed) and were fixed in one pass. Captured `Relocated` facts now identify their Tornado source. Before Buffer install, Core revalidates ordered current occupancy/candidates, exact unit/respawn choices and interleaved Flag-drop RNG; remaining at the origin is allowed only when the respawn candidate set is empty. Zero/single-candidate atomic publication and multi-source/death/terminal replay are verified. `E9-final-focused.xml` passed **280/280**, zero failures/skips. No deferred minor findings. The review excluded PlayMode and production View/DB/SO activation, which remain outside the authorized E9 scope.
Final full evidence after both review corrections: `E9-final-full.xml` and adjacent log, **2680 total, 2664 passed, 16 failed, zero skipped**, Unity exit 2. Core **1326/1326**, CombatViewBoundary **7/7**, Framework **1331/1347**. All **34 added E9 cases passed**. Machine comparison found zero failure-name differences from retained E8 and unchanged HEAD; the full suite remains non-green. Earlier table entries are the before-review checkpoints, superseded by this final evidence. `git diff --check` passes, new meta GUIDs are unique, index is empty, and only test-generated FMOD scan fields were restored. E9.1–E9.5 are complete for the authorized Core/Buffer fixture slice; E10 remains unchecked and unexecuted.

## 17 E10 execution evidence — 2026-10-06

The user requested E10 separately after completing E9. E10 fixes a real scene compatibility gap: `CombatSceneEventPresenter.CanPresent` rejected `CombatObstacleReactionEvent`, `CombatHeldEnvironmentEvent`, `CombatObjectActivationEvent`, `CombatObjectCreatedEvent` and `CombatTornadoEvent`. The existing presenter now accepts these captured logical facts in its existing ordered loop. Its metadata completion does not mutate Core/Buffer state or add dedicated effects. Existing Character/structure rendering remains fact-driven; Tornado Relocated snaps directly to its captured destination without walking interpolation. `CombatViewSystem` and `CombatResultPlayback` require no new acknowledgement or scheduling logic.

New tests exercise a real Core/Buffer move -> Flag acquisition -> Mine damage -> enemy death -> one nonterminal Flag drop result. The probe uses the real Scene presenter's capability check. Delayed drop presentation blocks Flow and final reconciliation; duplicate event callbacks still produce exactly one result acknowledgement. Captured source identity/cell/payload and death/drop positions remain available after consumption. Five additional real Scene cases verify typed metadata acceptance and once-only completion; a Scene relocation case checks the captured destination. Boundary tests retain immutable visibility/payload and separate physical origin, original credit and protection group.

Rejected commands deliberately retain the existing rejected-result/acknowledgement contract while publishing no gameplay events or mutations. The corresponding test is `RejectedContentCommand_PublishesNoGameplayEvents_AndAcknowledgesRejection`, a compatibility ruling over the plan's illustrative `PublishesNoResult` name. An additional actual View/Core test confirms entering/cancelling targeting keeps the checkpoint and Return, whereas a successful attack seals Return and cannot queue a Return command afterward. Existing Flow and input tests run unchanged.

| Checkpoint | Result | Persisted evidence under `.utmp/E10-completion/` |
| --- | --- | --- |
| Behavioral RED with valid content fixture | 84 total, 77 passed, 7 missing event-support failures, zero skipped | `E10-red-valid-fixture.xml` and adjacent log |
| Initial focused GREEN | 84/84 passed, zero failures/skips, Unity exit 0 | `E10-focused.xml` and adjacent log |
| Final focused GREEN | **85/85 passed**, zero failures/skips, Unity exit 0 | `E10-final-focused.xml` and adjacent log |
| Full EditMode regression | **2692 total, 2676 passed, 16 failed, zero skipped**, Unity exit 2 | `E10-final-full.xml` and adjacent log |

The final filter is `CombatMovementViewTests;CombatFlowTests;CombatFlowIntegrationTests;StageGimmickViewBoundaryTests;EnvironmentRecord_RealScenePresentation;TornadoRelocation_UsesCaptured`. Full assembly results: Core **1326/1326**, CombatViewBoundary **9/9**, Framework **1341/1357**. All **12 added E10 cases passed**. Exact failed-fullname comparison found zero differences against retained `.utmp/E9-completion/E9-final-full.xml` and unchanged-HEAD `.utmp/E7-completion/E7-head-baseline-full.xml`. These inherited failures remain: one display HUD cache, nine pointer/EventSystem setup, three existing scene lifecycle/allocation, one registered drag cycle and two bootstrap expected-log failures. The full suite is still non-green; this task does not claim those unrelated cases are fixed.

First RED's two real-result cases used an older Thorn-only test fixture. Connecting existing Flag/Mine rules exactly as the Factory does produced the recorded behavioral RED before restoring the presenter fix. Intermediate targeting-test request-timing and Entity-versus-Cell aim mistakes were corrected in tests only and are not production-regression or behavior-RED claims. Final tests use the existing Q planning/cancel and cell-aim contracts.

A fresh read-only review found no must-fix correctness/ownership issue. Its nonblocking missing targeting/checkpoint comparison was covered by the additional final test. Tested revision: uncommitted E7/E8/E9/E10 work on `feature/Cheon/refactor/core-system`, base **3d7b8103**, Unity **6000.0.73f1**. Initial dirty files and status were captured under `.utmp/E10-completion/incoming/`; the ledger records the scope rulings. Scope comparison shows pre-existing Core/Buffer changes were preserved. E10 changes only the existing Scene event presenter, the three listed test files and current plan/spec plus the new handoff. No new C# file/assembly/Buffer, Framework infrastructure, production data, scene or prefab was added. Unity-generated audio changes were limited to two scan-metadata fields and those fields were restored; audio status is clean. `git diff --check` passes for the final scope. No commit or push.

The [activation handoff](../handoffs/2026-10-05-environment-content-activation-handoff.md) records each of the eight contents' exact supported rules, tests, typed identities, actual authoring owners and visual needs. **Logical integration is complete. Production-stage activation, final balance/visibility/placement, default T1/G7 targeting authoring, dedicated/stacked visuals and PlayMode validation remain separate owner work.** Earlier E9-only exclusion paragraphs are historical scope records, superseded for current status by this separately authorized E10 execution.

## 18 Post-E10 review refactoring plan — 2026-10-06

**Source:** structural review after E10 and the user's acceptance of every recommended direction, recorded in the follow-up design "Post-E10 review decisions". This is behavior-preserving refactoring: no gameplay outcome, published event order or Framework infrastructure changes. Each phase ends with a reviewable diff, focused evidence and a full EditMode comparison against the 16 inherited failures in section 17; stop and report after each phase. No commit or push.

Line numbers refer to the working tree at review time and must be re-checked before editing. Existing assertions stay unless a row below names them as moved responsibility.

### R1 — Buffer keeps storage invariants only

- [x] **R1.1** `B/Combat/CombatStateBuffer.cs`: remove `using ...Application.Combat` (L9); call `ValidateEnvironmentChange(transition)` without characters (L183); replace L204-263 with the storage-only validation (binding append = added Object IDs; added Objects take contiguous source IDs and ordinals while unclassified legacy source additions keep their prior allocation rule; progress never decreases; an existing ID keeps its runtime type; Objects keep content and never reactivate; active ordinals unique per cell); delete `ValidateTornadoChanges` (L265-306), the RandomState clause, the Flag/Mine/Badge type lists, the `CellEnvironmentQuery` coexistence call and the Priority formula.
- [x] **R1.2** `A/TornadoCalculator.cs`: delete `ValidateRandomAndDestinations` (L76-164); optionally make the class internal if test visibility allows.
- [x] **R1.3** Tests: in `F/DynamicEnvironmentPublicationTests.cs` keep `Type`/`Outside` cases of `Tornado_InvalidRelocation_…`, remove `Due`/`TerrainOverlap`/`Clock`/`NoEvent`/`Random`/`Stay`; remove the forged-random Buffer rejection (L81-87) from `Tornado_NoDrawForZeroOrSingleCandidate_…` while keeping its Core and publication assertions; remove the three `ValidateRandomAndDestinations` oracle lines in `T/TornadoEnvironmentTests.cs` (L210, L266, L284). Add Core `RandomState_AdvancesOncePerMultiCandidateChoice` (interleaved unit relocation, respawn and Flag drop; one draw per multi-candidate choice, one `CombatTornadoEvent` per due source). Removed cases map to existing Core tests: Due/Clock → `ThirdCompletedTurn_IsFirstDue…`, `MixedFactionTurns_AdvanceOneClock`, `QueriesAndMovement_DoNotAdvanceClockOrRandom`; TerrainOverlap → `Candidates_SeparateUnitTerrainAndSourceRules…`; Stay → `NoCandidate_SkipsRandomButAdvancesDue`, `EmptySourceCell_StillRespawns…`.
- [x] **R1.4** Verify `DynamicEnvironmentPublicationTests;TornadoEnvironmentTests;FlagLifecycleTests;CombatCommandPublicationTests;EnvironmentEntryPublicationTests;ObjectArrivalOrderTests;MineEnvironmentTests`, then full EditMode. Confirm the runtime-type check rejects no existing legitimate source change.

**R1 execution — 2026-10-06.** Base: user commit `cb3f2ec6` (E7–E10 and this section). Working-tree changes are limited to `CombatStateBuffer.cs` (393 → 337 lines, no Core Application reference), `TornadoCalculator.cs` (166 → 75), `DynamicEnvironmentPublicationTests.cs` and `TornadoEnvironmentTests.cs`. Execution ruling: the first full run exposed three new `MovementPublicationTests` failures because the drafted "every added source must be an Object" rule also rejected the existing unclassified legacy source-addition path (section 15). The prior allocation rule for unbound non-Object additions was restored unchanged except for the removed RandomState clause; the drafted `Created` (unbound Tornado addition) case was withdrawn because that path is legacy-permitted storage and Core never produces it. The fixture `Tornado_InvalidRelocation_…` is renamed `Tornado_StorageViolation_…` (Type/Outside) and `Tornado_NoDrawForZeroOrSingleCandidate_IsValidatedBeforeAtomicInstall` is renamed `…_PublishesWithoutRandomAdvance`.

| Validation | Result | Evidence under `.utmp/R1-review/` |
| --- | --- | --- |
| Focused (7 fixtures, before legacy-path correction) | 140/140 passed, zero skipped, exit 0 | `R1-focused.xml` |
| First full run | 2688 total, 19 failed: inherited 16 + 3 `MovementPublicationTests` legacy-path regressions | `R1-full.xml` |
| Final full run after correction | **2687 total, 2671 passed, 16 failed, zero skipped**; Core 1327/1327, CombatViewBoundary 9/9, Framework 1335/1351 | `R1-full-2.xml` |

Failed-fullname comparison with `.utmp/E10-completion/E10-final-full.xml` shows zero new and zero fixed failures. The total changes from 2692 by −6 removed rule-replay cases and +1 new Core test (`RandomState_AdvancesOncePerMultiCandidateChoice`, passed). The Unity-rewritten FMOD catalog was restored byte-for-byte (SHA-256 verified). `git diff --check` passes. No commit or push. P3 is next and starts only on request.

### P3 — Derived effective Priority (after R1)

- [x] **P3.1** `A/HeldEnvironmentEffects.cs`: `WithHeldState` stops adjusting `Stats.Priority`; add `EffectivePriority(CharacterCombatFacts)` = base + held Flag bonus. Revert the `priority` argument added to `CombatCommandEffects.With`.
- [x] **P3.2** Scheduling reads: `A/CombatTurnCalculator.cs` participant creation (L38) and `A/CombatCommandCalculator.cs` Finish participant/timeline synchronization (L366, L375, L377) use `EffectivePriority`.
- [x] **P3.3** `K/CharacterHeldEnvironmentState.cs`: replace the `FlagSourceFact` payload with an immutable held-only Flag value (bonus, visibility) and update `SameValues`, `AcquireFlag`, `ClearFlag` and `FlagDropCalculator.CreateAt`. A new drop source uses default rule Priority because Object order never uses it.
- [x] **P3.4** Tests: move `T/FlagLifecycleTests.cs` `Stats.Priority` assertions (L183, 185, 196, 200, 202, 205, 216, 222) to effective/participant Priority with the same numbers; adapt `F/EnvironmentEntryPublicationTests.cs` held-Flag type use. Verify `FlagLifecycleTests;TornadoEnvironmentTests;HeldEnvironmentEffectTests;CharacterBufferStateTests;CoreStageCombatCycleTests;CombatMovementViewTests`, then full EditMode.

**P3 execution — 2026-10-06.** `Stats.Priority` is now the base value only; `HeldEnvironmentEffects.EffectivePriority` (base + held Flag bonus) feeds turn-participant creation and the Finish participant/timeline synchronization, so forecast and scheduling numbers are unchanged. The held payload is the new immutable `HeldFlag` (bonus and visibility, declared beside `CharacterHeldEnvironmentState`; no new file or `.meta`). The `priority` argument of `CombatCommandEffects.With` was removed; the one test that used it builds its fixture stats locally. Test changes beyond the listed `FlagLifecycleTests` and `EnvironmentEntryPublicationTests`: `CombatMovementViewTests` and `CombatSceneEventPresenterTests` construct held state through `HeldFlag.From`; Priority assertions keep the same numbers and additionally pin that base `Stats.Priority` no longer changes on pickup.

Full EditMode (`.utmp/P3-review/P3-full.xml`): **2687 total, 2671 passed, 16 failed, zero skipped**; Core 1327/1327, CombatViewBoundary 9/9, Framework 1335/1351. Focused fixtures from the same run: FlagLifecycle 13/13, HeldEnvironmentEffect 20/20, Tornado 20/20, CharacterBufferState 13/13, CombatMovementView 22/22, EnvironmentEntryPublication 25/25, DynamicEnvironmentPublication 14/14; CoreStageCombatCycle (1) and CombatSceneEventPresenter (3) failures are the inherited ones. Failed-fullname comparison with E10 final shows zero new and zero fixed failures. FMOD catalog restored byte-for-byte; `git diff --check` passes. No commit or push. P1 is next and starts only on request.

### P1 — One place per content-kind decision

- [x] **P1.1** `K/MovementEnvironmentFacts.cs` `MovementTileFact`: add virtual `TriggersArrivalRule` (true); override false in `DestinationTileModifierFact`, `StaticTerrainFact`, `TornadoSourceFact`. Replace the duplicated type exclusions in `A/MovementEnvironmentCalculator.cs` (L100) and `A/MovementProjection.cs` (L159).
- [x] **P1.2** Create public `A/EnvironmentContentSupport.cs` (+ unique `.meta`) with supported initial Terrain, exclusive Terrain and supported Object predicates; use it in `A/BattleEnvironmentPreparation.cs` (L37-47) and `CS/StageEnvironmentAdapter.cs` (L72-73).
- [x] **P1.3** Verify `MovementEnvironmentTests;StaticTerrainIntegrationTests;BattleEnvironmentPreparationTests;StageEnvironmentAdapterTests;ObjectArrivalOrderTests;MineEnvironmentTests;TornadoEnvironmentTests`, then full EditMode.

**P1 execution — 2026-10-06.** `MovementTileFact.TriggersArrivalRule` (default true; false for destination modifiers, static Terrain and Tornado) replaces the two duplicated exclusion lists in the movement coordinator and `MovementProjection.StepInput`. `EnvironmentContentSupport` (new file and unique `.meta`, GUID `fc582191d4734c3588d96582a434603f`) owns the supported initial Terrain, exclusive Terrain and supported Object predicates used by preparation and the stage adapter. Execution ruling: the class is `public`, not internal as drafted, because `StageEnvironmentAdapter` lives in `ProjectL.Systems` while the predicate lives in the separate `ProjectL.Core.Application` assembly; this matches the existing public `CellEnvironmentQuery`. The first run stopped at that CS0122 compile error and produced no test results. Remaining type checks (`PositionTileModifierCalculator`, `MovementContextCalculator`, `SameValues` overrides) read type-specific payload and are not support lists.

Full EditMode (`.utmp/P1-review/P1-full.xml`): **2687 total, 2671 passed, 16 failed, zero skipped**; Core 1327/1327, CombatViewBoundary 9/9, Framework 1335/1351. Focused fixtures from the same run all passed: MovementEnvironment 22/22, StaticTerrainIntegration 25/25, BattleEnvironmentPreparation 45/45, StageEnvironmentAdapter 21/21, ObjectArrivalOrder 18/18, MineEnvironment 20/20, TornadoEnvironment 20/20. Failed-fullname comparison with E10 final shows zero new and zero fixed failures. FMOD catalog restored byte-for-byte; `git diff --check` passes. No commit or push. P2 is next and starts only on request.

### P2 — Rule target context without downcast

- [x] **P2.1** `K/IMovementTileRule.cs`: `TargetIds(MovementTileFact tile, EntityId mover, CombatCommandInput current)`. Thorn/Flag/Badge ignore `current`; Mine removes its throwing overload and implements the area query through the interface.
- [x] **P2.2** `A/MovementEnvironmentCalculator.cs` (L41-42, L110) call the single signature with `projection.CurrentInput()`; delete the `is MineMovementTileRule` branch.
- [x] **P2.3** Update the nine test rule fixtures' signatures only (`EnemyMovementPathTests`, `EnvironmentDamageTests`, `MovementPublicationTests`, `MovementEnvironmentTests`, three in `ObjectArrivalOrderTests`, `ObstacleReactionTests`, `StaticTerrainIntegrationTests`). Verify those suites plus `MineEnvironmentTests;EnvironmentEntryPublicationTests`, then full EditMode.
- Deferred, separate decision: expressing Badge/Flag acquisition through `MovementStepEffects` to remove the remaining coordinator type branches.

**P2 execution — 2026-10-06.** `IMovementTileRule.TargetIds` now receives the current command input. Mine implements its clipped 3x3 occupant query through that single signature; the throwing overload, the internal overload and the coordinator's `is MineMovementTileRule` branch are gone. Both coordinator call sites pass `projection.CurrentInput()`; Thorn/Flag/Badge and the nine test fixtures changed signature only. The deferred `MovementStepEffects` held-acquisition decision remains open.

Full EditMode (`.utmp/P2-review/P2-full.xml`): **2687 total, 2671 passed, 16 failed, zero skipped**; Core 1327/1327, CombatViewBoundary 9/9, Framework 1335/1351. Focused fixtures from the same run all passed: EnemyMovementPath 23/23, EnvironmentDamage 11/11, MovementPublication 20/20, MovementEnvironment 22/22, ObjectArrivalOrder 18/18, ObstacleReaction 22/22, StaticTerrainIntegration 25/25, MineEnvironment 20/20, EnvironmentEntryPublication 25/25. Failed-fullname comparison with E10 final shows zero new and zero fixed failures. FMOD catalog restored byte-for-byte; `git diff --check` passes. No commit or push.

**Section 18 status:** R1, P3, P1 and P2 are complete. Across all four phases, production code under `Assets/ProjectL` changed in 17 tracked files (+98/−214 lines) plus the new `EnvironmentContentSupport.cs`; no Buffer, service, Framework infrastructure, scene or asset was added, and gameplay outcomes and event order are unchanged as evidenced by the identical failure set.

Out of scope for this section: classification migration (decision A keeps bindings), `MovementProjection` decomposition, RNG/attack-group relocation and other low-severity review notes.
