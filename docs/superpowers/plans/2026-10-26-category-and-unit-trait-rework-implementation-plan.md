# Category and Unit Trait Rework Implementation Plan

> **For agentic workers:** Read sections 2.3–2.5 before editing code; their architecture requirements are completion criteria, not optional style advice. Use `superpowers:executing-plans` for inline implementation, or `superpowers:subagent-driven-development` if the user selects delegation. Execute only the authorized phase, task by task. Steps use checkboxes. Stop and report at each phase boundary. Do not commit or push. Passing behavior tests does not waive a failed architecture check.

**Goal:** Retire Rider refund, Heavy ally protection, and Frenzy; add Hunter's Mark and configurable Pull in the existing trait system; migrate content and tests without losing surviving behavior coverage.

**Architecture:** Keep definitions, rules, runtime markers, adapters, and registration in their existing trait directories. Feature rules own mark eligibility/consumption and Pull geometry; combat application coordinates their use in the existing projection and attack sequence. Buffer publication, Job scheduling, and View state ownership remain unchanged.

**Tech Stack:** Unity 6000.0.73f1, C#, existing Core.Contracts/Domain/Application assemblies, Systems SO authoring, NUnit EditMode tests.

**Spec:** [Category and Unit Trait Rework](../specs/2026-10-26-category-and-unit-trait-rework-design.md).

**Status:** Phases A and B implemented and verified at their authorized local checkpoints, 2026-10-26; see section 9 for source, test and review evidence. The user separately confirmed G-M as design sections 6.2–6.3. G-P confirmed with the user's obstacle exclusion; Phase C implemented and verified with the obstacle exclusion, Phase D code implemented and independently reviewed; three findings fixed with a green two-assembly gate. Final manual/art acceptance remains open. The original planning inspection used HEAD `1ba7aec4`; execution started at `c816c46d`. No commits or pushes.

**Architecture revision (2026-10-26):** The user requested stronger constraints against feature-specific hard-coded branches, bypass paths, and concentrating unrelated responsibilities in one script. Sections 2.3–2.5 make ownership, connection paths, and review evidence mandatory for every implementing model. This revision changes implementation constraints, not the unresolved gameplay decisions in G-M/G-P/G-C.

## Global Constraints

- English implementation documents; Korean code comments and XML documentation. Preserve existing namespace/folder spelling, Allman formatting, and `.meta` handling.
- No commits or pushes. Execute only the explicitly authorized phase. No restoration, staging, or deletion of another session's working-tree changes.
- Core owns combat rules/facts; View consumes published contracts. Cross-owner View/Framework work needs its owning phase/worker. Do not touch FieldCamera or Architecture as incidental cleanup.
- Keep Ballista unchanged. Keep DistanceDamageScale as a reusable trait; replace only Shotgun's base-trait assignment. Preserve Heavy knockback immunity and other category rules.
- One mark per target; designer-authored bonus defaults to **10%**. Cowboy defaults to basic range **1**, Pull extension **1**, cardinal/single/unrestricted targeting. Derive total reach as `A + P`; outer targets move to distance **A** before damage.
- Preserve ordinary cost/action ownership, immutable snapshots, query/preview parity, atomic publication, and relevant FW-001–016 contracts. Never implement another Buffer, Job, event bus, or generic skill framework for this work.
- Keep the protected legacy Missing Script assets. Never reuse retired serialized enum numbers or managed-reference class identities.
- Use the approved AppData-access path for Unity verification. Do not upgrade Unity or clear caches to work around sandbox access.
- A smaller diff or a green test result does not justify placing a rule outside its owner. Do not postpone a known boundary violation as a TODO, compatibility hack, temporary fallback, or later cleanup.

## Review Focus

1. A second Shotgun consumes an existing mark, then marks again; multi-hit batching must not give multiple bonuses for the same mark. B2 tests consumption before queued HP application and reapplication after damage.
2. Pull succeeds but the victim dies on terrain before the hit; C2 tests actual movement/hazard publication, no direct attack, and one attacker settlement.
3. The selected cell becomes empty after Pull; C2 tests identity-pinned delivery using arrival positions, with no attack against the original cell.
4. Friendly Pull must not spend shields, trigger reflection/hostile-hit effects, or use the victim's action; C2/C3 explicitly test these distinctions.
5. Obsolete suites also carry generic publication/playback/defense checks; A1–A3 require case-level preservation before fixture deletion.

## 1. Reading and review gates

Read the spec and this plan together. Also read repository AGENTS instructions and the current Framework programmer guide. The user-supplied [feature-change locality guideline](<D:/My_ProjectWork/학교 관련 문서/졸업작품_프로젝트L/2학기 내용/AI 관련 문서/feature-change-locality-guidelines.md>) was read for this plan. It is guidance for evaluating change boundaries, not independent authorization to modify code.

The guideline is applied as follows: trace actual rule/state/call-order dependencies; distinguish required gameplay coupling from avoidable implementation coupling; allow normal registration edits; avoid duplicate state and speculative interfaces; preserve necessary regression coverage. The locality table in section 8 records predicted benefits, not proven execution results.

| Gate | Decision required before dependent implementation | Proposed baseline from the spec |
|---|---|---|
| G-M — confirmed 2026-10-26 | Mark trigger/reapplication, percentage stage, zero-damage consumption, source lifetime | Spec 6.2–6.3: first qualifying ordinary hit, one slot, consume before reapply, final pre-shield damage multiplied by `100+p`, half-up rounding, mark survives source death. |
| G-P — confirmed 2026-10-26 | Blocked action cost, friendly no-op, Pull immunity, terrain effects; obstacles excluded | Spec 5.2: geometric rejection costs nothing, inner friendly no-op rejects, explicit immunity blocks, Heavy category alone does not block Pull, hazards apply. Obstacles/structures retain ordinary attacks within basic range; no Pull or extension-range attacks against them. |
| G-C | Intermediate content between deletion and replacement | Proposed development-only Phase A: Cowboy has no base trait temporarily; Shotgun keeps its old trait until B3. Neither is a final release checkpoint. If unacceptable, revise phase/cutover ordering before A3. |

Writing this plan does not wait on those decisions. B gameplay tasks and C gameplay tasks do wait on their respective gates. Record an answer in the spec before executing dependent code. Do not reinterpret a general “proceed” as permission to choose a different proposal.

## 2. File locations and ownership

All paths in task tables expand from these exact repository-relative roots:

| Alias | Root |
|---|---|
| `C/` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/` |
| `U/` | `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Unit/` |
| `CS/` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/` |
| `S/` | `Assets/ProjectL/Script/Systems/` |
| `V/` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/` |
| `DB/` | `Assets/ProjectL/DataBaseSO/` |
| `T/` | `Assets/Tests/EditMode/` |

### 2.1 New traits use existing directories

For each exact prefix `HuntersMark` and `Pull`, create the following files with matching `.meta` files. Do not create a parallel feature framework or reorganize existing traits to accommodate them.

| New file pattern | Responsibility / nearest reference |
|---|---|
| `U/Trait/{Prefix}TraitAuthoring.cs`, `{Prefix}TraitData.cs` | SO managed-reference input and pure immutable Data. Match `IntimidationTraitAuthoring.cs` / Data. |
| `C/Contracts/Commands/Traits/{Prefix}TraitSpec.cs` | Validated gameplay configuration passed into Core. |
| `C/Domain/Traits/Definitions/{Prefix}TraitDefinition.cs` | Runtime immutable definition, matching existing registration. |
| `C/Domain/Traits/Rules/{Prefix}TraitRule.cs` | Actual feature decisions, not merely rules scattered in orchestration. Implements existing `ICombatTraitRule`. |
| `C/Domain/Traits/States/{Prefix}RuntimeState.cs` | Stateless owner marker required by existing registration; never stores victim marks or a second range source. |
| `C/Domain/Traits/Effects/{Prefix}EffectStateSnapshot.cs`, `{Prefix}EffectRule.cs` | Existing effect adapter/codec integration. Pull contributes friendly-target eligibility through `IEffectAttackTargetingRule` in `CombatEffectCapabilities.cs`; Mark follows the marker adapter pattern used by Intimidation. |
| `C/Application/Combat/Traits/{Prefix}TraitFactories.cs` | Definition/runtime factories in one existing-style file; construction and validation only. |
| `C/Contracts/Snapshots/Traits/{Prefix}TraitStatusSnapshot.cs` | Configured percentage/extension for consumers and trait availability. No mutable status ownership. |

The necessary shared registration edits are `C/Contracts/Common/TraitKey.cs`, `CS/CharacterSystem/TraitSpecAdapter.cs`, `C/Application/Combat/Traits/TraitRegistry.cs`, `TraitEffectProvider.cs`, `C/Domain/Traits/Effects/TraitEffectStateCodec.cs`, and `C/Application/Combat/CombatCommandEffects.cs`. Also update `U/Trait/UnitTraitAuthoring.cs` / `UnitTraitData.cs` only where their supported-type contracts require it. No automatic discovery/reflection registration.

Use proposed new keys `HuntersMark = 190` and `Pull = 200`, after confirming they remain unallocated at execution time. Retire Frenzy 120 without reusing or renumbering other IDs. A concurrent allocation requires a coordinated new unused value, not silent collision.

### 2.2 Feature rules versus necessary orchestration

| File | New responsibility | Must not own |
|---|---|---|
| `C/Contracts/Combat/HuntersMarkState.cs` | Immutable source entity, owner, percentage; equality. | A second mutable mark store. |
| `C/Domain/Traits/Rules/HuntersMarkTraitRule.cs` | Determine bonus/consumption and non-stacking application from supplied facts. | World dictionaries, Buffer writes, animation. |
| `C/Application/Combat/HuntersMarkApplication.cs` | Apply rule outputs to command-local facts at the correct direct-hit boundaries; append status events. | A second mark store or Cowboy rules. |
| `C/Domain/Traits/Rules/PullTraitRule.cs` | Derive `A+P`, validate cardinal geometry, compute distance-A destination. | Terrain effects, damage, transaction installation. |
| `C/Application/Combat/PullAttackCalculator.cs` | Preflight the path against existing world contracts, execute shared displacement, and rebuild arrival delivery with pinned identity. | Reimplementation of damage/terrain or changes to other traits. |
| `C/Application/Combat/PullAttackEvaluation.cs` | Immutable command-local target/path/failure plan. | Persistent battle state. |
| `C/Contracts/Results/AttackPullSnapshot.cs` | Immutable actual Pull path/start/final facts for result and preview. | View-derived gameplay decisions. |
| `C/Contracts/Combat/CombatHuntersMarkEvent.cs`, `CombatPullEvent.cs` | Ordered immutable presentation facts for mark changes / actual unit movement. | State installation or a second event bus. |

`AttackDirectDamageCalculator`, `AttackDeliveryExecutor`, and `CombatAttackCalculator` retain legitimate runtime sequencing. They call the feature functions; they do not gain percentage math, source-eligibility switches, or cardinal destination math. This is a responsibility boundary inside the established architecture, not a blanket split of every orchestration file.

### 2.3 Mandatory rule ownership and permitted branching

The constraint is **where a decision belongs and which contract it uses**, not the spelling of `if`, `switch`, a ternary, a pattern match, or a lookup table. A branch inside its rule owner is normal. Replacing a misplaced `if` with a dictionary, delegate, partial class, extension method, or generic utility does not repair the ownership violation.

| Decision / change | Required owner | Permitted consumer behavior | Rejected implementation |
|---|---|---|---|
| Author mark percentage / Pull extension | The trait's existing-style Authoring -> Data -> Spec -> Definition path | Pass/read the validated instance value; View uses its published snapshot. | Literal 10% or +1 in damage, range, UI, fallback, or bootstrap code. Percentage denominator 100 and ordinary geometry constants are not tuning defaults. |
| Mark application, source/team eligibility, consumption, no-stack, bonus math | `HuntersMarkTraitRule` | `HuntersMarkApplication` supplies facts and applies returned decisions to the existing projection. | Put those conditions in `AttackDirectDamageCalculator`, `CombatDamageCalculator`, a registry, or View. |
| Mark direct-hit timing and event emission | `HuntersMarkApplication` at B2's named prepare/complete hooks | Shared damage execution calls the hooks in order and handles their result. | A second attack path for Shotgun, delayed end-of-command consumption, or an event listener that mutates marks. |
| Pull range/destination and trait-specific damage eligibility | `PullTraitRule`; the registered targeting capability supplies selection eligibility | `PullAttackCalculator` evaluates the selected target once and carries the decision in `PullAttackEvaluation`. | `if Cowboy`, `if Rider`, or `if Pull` followed by destination/bonus/range math inside shared query, damage, or View methods. |
| Occupancy, walls, terrain and movement application | Existing movement/environment contracts, with only the bounded C2 extraction if demonstrated necessary | Pull provides its path/target; shared movement owns traversal facts and existing environmental consequences. | Copy the knockback body, bypass collision, create separate character/Obstacle world stores, or fix only the displayed destination. |
| Attack admission, stage order, terminal outcome, one settlement and publication | Existing combat orchestrators and transaction owners | Branch on validated evaluation outcomes: rejected, requires movement, alive/terminal, may deliver damage. Invoke existing settlement/publication once. | Copy the ordinary attack executor into a Cowboy executor or add a second settlement/writer to make a special case pass. |
| Trait construction, lookup, codec, enum dispatch | Existing adapters, registry/factories, codec/status conversion | Explicit type/key dispatch for construction/validation remains legitimate. Resolve typed configuration at the feature adapter boundary. | Run gameplay calculations or query global mutable services from a factory/registry; scan for the same trait independently in multiple shared attack stages. |
| Result routing / presentation | Existing View consumers | Route by event/result type, animate supplied paths, display supplied values. | Identify Cowboy/Shotgun to reconstruct Core behavior, or infer a mark from a damage increase. |

In runtime gameplay, unit keys 11/19, asset names, display strings, object names, and retired enum numbers must not select the new behavior. Unit identity comparisons required by a rule, such as attacker entity != mark source entity, belong in that rule and are allowed. Unit keys and GUIDs remain valid in authored content, catalog lookup, migrations, and asset assertions.

Absence of an optional trait/mark is a normal identity/no-effect case. Presence with missing registration, invalid configuration, a mismatched runtime snapshot, or broken required binding is an error under the existing contracts. Do not catch that error and fall back to ordinary attack, assume 10%/1 cell, skip a target, or return success. Preserve explicitly valid normal rejection/empty-result behavior; do not turn it into an exception merely to enforce this rule.

### 2.4 Required connection paths and responsibility limits

Use these connections; do not introduce an alternative gameplay entry point:

```text
Trait authoring -> Data -> TraitSpecAdapter -> TraitRegistry/factories
    -> definition + owner marker/effect binding -> configured status snapshot

Ordinary direct-hit pipeline
    -> HuntersMarkApplication.PrepareHit -> HuntersMarkTraitRule.EvaluateHit
    -> existing held-defense / damage application
    -> HuntersMarkApplication.CompleteHit -> HuntersMarkTraitRule.CanApplyMark / ApplyMark
    -> same command projection -> existing atomic publication -> View facts/events

Existing attack query/admission
    -> Pull range rule + registered target policy -> PullAttackCalculator.Evaluate
    -> PullAttackEvaluation -> existing command projection / one arrival projection
    -> shared forced movement -> pinned-target delivery rebuild -> ordinary damage if permitted
    -> existing one settlement / atomic publication -> View facts/events
```

Hunter's Mark's marker-style registration does not mean its decisions can be embedded in arbitrary application files. The two explicit application hooks are the approved orchestration integration for a target-owned status; implementing a new general effect framework is not required. Pull's pre-hit stage is explicit because the existing knockback stage runs after damage. Do not disguise the different timing by flipping a distance sign or invoking the old stage twice.

For this feature, `AttackDirectDamageCalculator` may add the mark hooks, consume the returned damage, and connect already-resolved delivery decisions. `CombatAttackQueries` may call the shared range/target evaluation and return its results. `CombatAttackCalculator`/`AttackDeliveryExecutor` may connect the pre-hit stage, terminal handling, event ordering, and existing settlement. None may implement the feature's percentage formula, no-stack rule, source eligibility, landing-distance calculation, or authored-value fallback.

`HuntersMarkApplication` and `PullAttackCalculator` are not containers for every change associated with the feature. They may own the application responsibilities in section 2.2, not authoring conversion, UI formatting, global service lookup, new storage, or independent versions of damage/terrain/transaction logic. A large feature-named script is still a violation if it combines those responsibilities. Conversely, do not split a coherent rule into single-line wrapper classes to make file counts look better.

Public snapshots/evaluations must describe input identity, outputs, mutation ownership, timing, lifetime, and failure behavior. A command-local evaluation is discarded after the command; a projected copy is not a second state source. The existing caller supplies required world dependencies explicitly. No singleton, static mutable dictionary, service locator, string dispatch, or reflection-discovered hook may hide a dependency.

Before adding a file or changing a path outside the task map, identify the existing owner and the missing contract. A local support type/helper within the planned owner's directory is allowed when its responsibility and callers are recorded and it removes real duplication. Moving rules to a different owner, changing public gameplay semantics, adding a new execution/service/storage layer, or expanding another worker's area is a plan deviation: document the evidence and obtain the required scope decision before implementing that deviation. Do not request another approval for ordinary planned registrations or in-owner implementation details.

### 2.5 Task-entry and task-exit architecture checks

**Before each behavioral task**, add a short boundary record to section 9: (1) rule owner and exact functions, (2) required input/output/state, (3) callers and ordering, (4) expected files, and (5) behavior and structural checks. Reuse the task's existing record rather than creating a new design document per method. Inspect relevant implementations during this work; locality is not an excuse to skip investigation.

**Before marking the task complete**, review the actual attributed diff, including new/untracked files, against all checks below. If several tasks share a compiling phase, retain each task's record and close the checks at that phase's validation point.

- [x] **AC-1 — Rule placement:** all new feature formulas and gameplay predicates are in their mapped rule owner. Shared orchestrators only coordinate and consume decisions. Inspect every new conditional in the changed shared methods, not just conditions containing a trait name.
- [x] **AC-2 — Path integrity:** trace authoring -> registration -> evaluation -> projection -> publication -> consumer where changed. Ordinary execution, preview, and AI use the same rule implementation. No parallel fallback or special unit-ID entry path exists.
- [x] **AC-3 — State and value SSOT:** one mark slot, one configured value source, derived A+P, and one transaction owner. Application, View, and test helpers do not reconstruct authoritative gameplay independently. Boundary input validation is not forbidden duplication; duplicated gameplay decisions are.
- [x] **AC-4 — Change-locality demonstration:** explain the exact edit/understanding/verification scope for tuning the percentage, changing mark eligibility, or tuning Pull extension as relevant. These changes must not require editing an unrelated trait's implementation, changing a central feature switch, or repairing an unconnected screen. Shared contract/terrain changes still require their real integration tests.
- [x] **AC-5 — Data-driven behavior:** tests use another valid unit key with the same trait/configuration and obtain the same behavior; removing the trait from a synthetic definition removes the behavior. Altering 10 -> 25 or A/P changes outcomes through the real configuration path. This rules out unit-name/ID/default-value shortcuts without assuming every trait combination is legal.
- [x] **AC-6 — Removal and preserved behavior:** retired code has no live shadow path; each mixed test/helper deletion has a disposition and surviving guarantees pass. A compilation fix may not silently disable the feature or a required test.

Use scoped searches on the files attributed to this task to locate new references to `HuntersMark`, `Pull`, `BonusDamagePercent`, `ExtraRangeCells`, unit keys/names, fallback/catch logic, and new mutable state. Classify hits by the allowed roles above. Search results alone are not proof: inspect the call graph and shared-method diff, including disguised ternaries/lookups and value reconstruction. Do not use repository-wide “no if” counts, line-count thresholds, filename counts, or brittle source-string tests as architecture acceptance.

Record each AC result as **pass with file/function evidence**, **not applicable with reason**, or **fail with the concrete violation**. Missing evidence is incomplete, not pass. Any failure prevents completion even when behavioral tests pass. Fix an in-scope violation in its rightful owner and rerun affected checks; if the only repair requires a plan deviation, stop that dependent work and report the required decision. Do not label the workaround complete with a future-refactor note.

These are reviewable implementation obligations, not an automated enforcement tool or a guarantee that a model will comply. No persistent lint/test framework is added by this document revision. Use a separate reviewer when the chosen execution workflow includes one; otherwise perform and report this review explicitly. No additional agent dispatch is authorized merely by the existence of this checklist.

## 3. Verification commands and evidence

Before edits, capture current status and the applicable baseline. Existing test work is moving service checks into `T/CoreIntegration/`; use actual current files and assemblies, not old counts or old Framework locations. Do not run another Unity instance against an open project. If the Editor is open, run EditMode through that Editor's supported runner; otherwise use approved batch execution.

Example PowerShell invocation, to be run at implementation time with the approved execution permission:

```powershell
$traitEvidence = Join-Path (Get-Location) '.superpowers/category-trait-rework'
New-Item -ItemType Directory -Force -Path $traitEvidence | Out-Null
$traitAssembly = 'ProjectL.Tests.Core'
$traitFilter = 'ProjectL.Tests.Core.HuntersMarkRuleTests'
$traitLabel = 'B1-hunters-mark-red'
$traitXml = Join-Path $traitEvidence "$traitLabel.xml"
$traitLog = Join-Path $traitEvidence "$traitLabel.log"
$traitProcess = Start-Process 'D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe' -WindowStyle Hidden -Wait -PassThru -ArgumentList @(
    '-batchmode', '-nographics', '-projectPath', 'D:/My_ProjectWork/Github_desktop/Project_L',
    '-runTests', '-testPlatform', 'EditMode', '-assemblyNames', $traitAssembly,
    '-testFilter', $traitFilter, '-testResults', $traitXml, '-logFile', $traitLog)
if (!(Test-Path -LiteralPath $traitXml)) { throw 'No test XML; inspect the Unity log.' }
[xml]$traitResult = Get-Content -LiteralPath $traitXml -Raw
$traitResult.'test-run' | Select-Object result,total,passed,failed,skipped
```

Use a new label for each run; replace the project path with the actual execution worktree if isolated. Each task lists its exact filter. `Run(filter)` below means this invocation with the stated assembly/filter. For full assembly gates, omit `-testFilter`. A green check requires process exit 0, valid fresh XML, nonzero expected case count, and all required selected cases passed; investigate skipped/inconclusive cases. A red check must fail for the intended missing/incorrect feature, not unrelated compilation/setup failure.

Every implementation task uses the cycle: add the named failing assertions -> run red -> implement the stated boundary -> run green and affected regression. Record commands, XML/log paths, counts, baseline comparison, and unrun acceptance at the phase checkpoint. For pure deletion, demonstrate changed surviving behavior assertions and reference closure rather than writing tests that only assert source files disappeared.

A2 is preservation/relocation of existing behavior: establish a passing source or equivalent destination baseline and keep it green; do not manufacture a behavioral failure merely to move an assertion.

Task filters for the command above (semicolon separates classes in a single filter; select the matching assembly):

| Task | Assembly / filter |
|---|---|
| A1 | Core: `ProjectL.Tests.Core.CategoryEffectRetirementTests`; CoreIntegration: `ProjectL.Tests.CoreIntegration.CategoryDefinitionMigrationTests` |
| A2 | Core: `ProjectL.Tests.Core.CombatDebuffCalculatorTests;ProjectL.Tests.Core.PlayerTurnAttackTests`; Framework: `ProjectL.Tests.Framework.CombatPlaybackContinuationTests` or the recorded existing destination class |
| A3 | Core: `ProjectL.Tests.Core.CategoryEffectRetirementTests`; CoreIntegration: `ProjectL.Tests.CoreIntegration.RetiredTraitAuthoringTests` |
| B1 | Core: `ProjectL.Tests.Core.HuntersMarkRuleTests;ProjectL.Tests.Core.HuntersMarkStateTests`; CoreIntegration: `ProjectL.Tests.CoreIntegration.HuntersMarkAuthoringTests` |
| B2 | Core: `ProjectL.Tests.Core.HuntersMarkAttackTests`; CoreIntegration: `ProjectL.Tests.CoreIntegration.HuntersMarkPublicationTests` |
| B3 | CoreIntegration: `ProjectL.Tests.CoreIntegration.HuntersMarkAuthoringTests`; Framework: `ProjectL.Tests.Framework.UnitCardTraitContentTests;ProjectL.Tests.Framework.DataBaseAssetTests` |
| C1 | Core: `ProjectL.Tests.Core.PullRangeTests;ProjectL.Tests.Core.PullTargetAdmissionTests`; CoreIntegration: `ProjectL.Tests.CoreIntegration.PullAuthoringTests` |
| C2 | Core: `ProjectL.Tests.Core.PullAttackTests;ProjectL.Tests.Core.PullTerrainTests;ProjectL.Tests.Core.PullObstacleTests` |
| C3 | Core: `ProjectL.Tests.Core.PullPreviewTests`; CoreIntegration: `ProjectL.Tests.CoreIntegration.PullPublicationTests` |
| C4 | CoreIntegration: `ProjectL.Tests.CoreIntegration.PullAuthoringTests`; Framework: `ProjectL.Tests.Framework.DataBaseAssetTests` |
| D1 | Framework: `ProjectL.Tests.Framework.PullPresentationTests;ProjectL.Tests.Framework.HuntersMarkPresentationTests;ProjectL.Tests.Framework.UnitCardTraitContentTests;ProjectL.Tests.Framework.DataBaseAssetTests` |

Here Core/CoreIntegration/Framework abbreviate `ProjectL.Tests.Core`, `ProjectL.Tests.CoreIntegration`, and `ProjectL.Tests.Framework` assembly names. Run additional regression classes listed in each task. Pure Core tests can exercise Domain rules; CoreIntegration tests must use their existing Systems/Contracts/Application references and real public/service paths, not add a Domain/test-assembly dependency for convenience.

## 4. Phase A — retired rules and tests

### A0. Reconcile the workspace, test ownership, and decisions

**Files:** read the spec, this plan, `T/CoreIntegration/`, and [concurrent test disposition](2026-10-26-core-test-foundation-disposition.md). Write execution decisions/evidence in this plan's checkpoint section; do not edit the other plan.

**Interfaces:** consumes current source ownership and G-C; produces the exact case/helper disposition and baseline for A1–A3. This is a planning/preflight task, not a new runtime component.

- [x] Capture branch/HEAD/status; identify concurrent files. Confirm G-C before changing Cowboy content.
- [x] Record the phase's ownership/call-path map using sections 2.3–2.5. Planned deletion must remove the obsolete responsibility; replacing it with an always-false branch, empty provider, or alternate hidden path is not removal.
- [x] Search production/test/assets for `ManeuverRefund`, `HeavyProtection`, `Frenzy`, and `DistanceDamageScale`; classify each reference as retire, adapt, preserve, or owner handoff. Expand mixed suites to method-level disposition before deleting them.
- [x] Run Core and CoreIntegration baselines plus the affected Framework tests from A1–A3. Record existing setup failures separately; a failing legacy fixture is not deletion justification.

### A1. Remove Rider refund and Heavy protection through their full consumers

**Files:** modify `CS/CharacterSystem/UnitSpecAdapter.cs`; `C/Application/BootstrapModels/CategoryRegistry.cs`, `UnitCatalog.cs`; `C/Application/Combat/Categories/CategoryRuleFactories.cs`, `CategorySnapshotBuilder.cs`; `C/Contracts/Snapshots/CategorySnapshot.cs`; `C/Contracts/Commands/UnitSpec.cs`; `C/Domain/Entities/UnitDefinition.cs`; `C/Application/Enhancements/OwnedUnitSpecResolver.cs`; `U/Item/UnitData.cs`, `UnitDataSO.cs`; `S/DataBaseSystem/Data/InGame/Table/Category/Item/CategoryConfigData.cs`, `CategoryConfigDataSO.cs`; View `Dev/DevBattleInitializationSource.cs`; active category/unit assets that serialize protection fields. Modify protection-bearing attack/movement/follow-up callers identified by A0, including `CommandAttackPlan`, `CombatDamageCalculator`, `CombatAttackCalculator`, `FollowUpAttackExecutor`, `KillChainAttackCalculator`, and `FrenzyAttackCalculator` until A3 removes it.

**Remove after reference closure:** dedicated `ManeuverRefund*` category Spec/Definition/Rule/state files; dedicated `HeavyProtection*` Spec/Definition/Rule, application capture/query/incoming-effects and domain calculator/snapshot files; `CS/CombatSystem/HeavyProtectionQueryService.cs`, `S/RuntimeSystem/BufferSystems/Combat/IHeavyProtectionReadSink.cs`. Update `S/Factory/Runtime/InGame/Core/CombatSystemFactory.cs` and `CS/InGameCoreSystemGroup.cs` exports/accessors in the same task.

**Tests:** add `T/CategoryEffectRetirementTests.cs`, `T/CoreIntegration/CategoryDefinitionMigrationTests.cs`; adapt `CharacterCategoryBootstrapTests`, `CharacterCategoryContractTests`, `CategoryDamagePipelineTests`, `CategoryActionRulesTests`, relevant authoring/asset cases. Apply spec section 9's Rider/Heavy disposition.

**Interfaces:** production/default/dev category builders produce Rider/Heavy with no retired rules; category snapshots no longer require protection metadata; existing damage callers no longer require `HeavyProtectionAttackSnapshot`. Incoming effects from surviving traits remain evaluated. No empty replacement query service.

- [x] In Core, add `RiderMoveThenAttack_DoesNotRefundSharedBudget`, `HeavyWithoutOffsets_ConstructsDefaultCatalog`, and `BehindHeavy_DamageMatchesUnprotectedControl` with the same attack/defense/terrain in both controls. In CoreIntegration, add `HeavyWithoutOffsets_ConstructsThroughProductionAndDevSources`. Add checks that Heavy category, physical defense, held shield, and knockback immunity survive.
- [x] `Run(ProjectL.Tests.Core.CategoryEffectRetirementTests)` red, then remove the closed feature paths and authored values. Change all callers of removed signatures together; preserve unaffected optional values with named arguments.
- [x] Preserve `HeavyProtectionIncomingEffectsTests.Engine_CapsCombinedContributionsAtIncomingDamage` in the surviving engine tests using synthetic/surviving contributions. Preserve distinct defense/reflection/terrain/preview guarantees before deleting mixed Heavy suites. Delete the test-only `RefundSettlement` helper with its obsolete test.
- [x] Run the new suite and affected category/physical-defense/knockback tests green, plus real production/default/dev assembly and asset checks. Trace query removal through factory -> group -> consumer. Record API removals for the owner handoff. Development evidence covers category assembly, not full fixture playability; see section 9.

### A2. Preserve independent guarantees currently embedded in Frenzy fixtures

**Files:** `T/FrenzyStatePreservationTests.cs`, `FrenzyAttackTests.cs`, `Framework/FrenzyViewContractTests.cs`; destination `T/CombatDebuffCalculatorTests.cs`, `PlayerTurnAttackTests.cs`, and an existing generic playback suite or new `T/Framework/CombatPlaybackContinuationTests.cs` if no suitable owner exists.

**Interfaces:** test fixtures cease relying on Frenzy while maintaining actual state-copy, one-action/no-refund, and playback-continuation contracts. No production feature changes in this task.

- [x] Re-home `RiderMoveAndWaves_RespectOneActionAndNoRefund`'s surviving one-action/no-refund assertions without waves; preserve status copy/equality with Intimidation and defense reduction; preserve `UnsupportedWaveMarker_DoesNotSuppressLaterHealthEventsOrFinalization`'s generic continuation assertion using a suitable test event.
- [x] Run destination tests and verify each unique guarantee actually exercises production behavior. Avoid a duplicate replacement if an equivalent current test already proves it; record that mapping instead.
- [x] Remove only transferred assertions/helpers that no longer have consumers. Keep test assemblies independent; CoreIntegration fixtures must not import Framework fixtures.

### A3. Delete Frenzy and migrate its content/registrations

**Files:** delete dedicated Frenzy trait files in the same locations listed for new traits in section 2.1; `C/Contracts/Combat/FrenzyDebuffState.cs`, `CombatFrenzyWaveEvent.cs`; `C/Domain/Combat/FrenzyPriorityCalculator.cs`; `C/Application/Combat/FrenzyAttackLedger.cs`, `FrenzyAttackCalculator.cs`. Modify `CharacterDebuffState`, `CombatDebuffCalculator`, `CombatDebuffApplication`, trait mappings, `AttackDirectDamageCalculator`, `AttackDeliveryExecutor`, `CombatEvent.cs`, and all A0 consumers. Migrate `DB/InGameGroup/Unit/Item/Playerable/Unit_Cowboy.asset` and the Frenzy View visual-table entry/asset with references and metas handled together.

**Tests:** remove exclusively obsolete `Frenzy*Tests` cases after A2; add `T/CoreIntegration/RetiredTraitAuthoringTests.cs` for real content construction and surviving catalog validation; extend `T/CategoryEffectRetirementTests.cs` for the no-extra-attack guarantee.

**Interfaces:** no Frenzy runtime marker, victim component, wave event, priority rule, special direct-hit argument, or managed-reference payload remains. Old numeric IDs remain reserved in migration documentation; all surviving enum values remain stable.

- [x] Add `CowboyDefinition_NoFrenzyAndCatalogStillBuilds` and `DirectKill_DoesNotProduceAdditionalFrenzyAttack` (assert one ordinary attack result/cost and no extra victim damage, not the deleted event type).
- [x] Run red; remove Frenzy-only hooks without removing shared health flushing, reflection, kill accounting for other features, or action settlement. Remove `frenzy` constructor arguments by explicit callsite migration, not positional reinterpretation.
- [x] Retire dedicated assertions after A2 replacements pass. Search for serialized class names, retired keys, helper references, asset GUID references, and availability text. Active source/content may not instantiate retired types; historical docs may mention them.
- [x] Run retirement/content tests, surviving debuff/trait registry and player-turn tests, then Core/CoreIntegration. The user's later instruction excludes further Framework execution; earlier evidence and that limit are recorded in section 9. Check `.meta` pairing and real asset deserialization.

**Phase A checkpoint:** close AC-1–AC-6 for the changed boundaries, then stop and report. Check removal paths and retained test guarantees in particular; data-driven new-trait checks can be not applicable until B/C. Cowboy is temporarily traitless only under G-C; Shotgun still uses DistanceDamageScale. Categories no longer supply the removed effects. List deleted cases and preserved destinations, contract consumers, results, and limitations. No claim of completed new traits.

## 5. Phase B — Hunter's Mark

### B1. Add configuration, registration, pure rule, and target state

**Files:** create the `HuntersMark` files from section 2.1 and `C/Contracts/Combat/HuntersMarkState.cs`; modify its listed registries/adapters and `C/Contracts/Combat/CharacterDebuffState.cs`. Audit every `new CharacterDebuffState(...)` in `CombatDebuffApplication`, `CombatDebuffCalculator`, lifecycle rules, and tests so replacing independent statuses preserves the mark. Create `T/HuntersMarkRuleTests.cs`, `HuntersMarkStateTests.cs`, and `T/CoreIntegration/HuntersMarkAuthoringTests.cs`.

**Interfaces (new):** `HuntersMarkTraitData(int bonusDamagePercent)`, `HuntersMarkTraitSpec(int bonusDamagePercent)`, `HuntersMarkTraitDefinition(int bonusDamagePercent)`, and `HuntersMarkTraitStatusSnapshot(int bonusDamagePercent)` expose `BonusDamagePercent`. Authoring field `bonusDamagePercent = 10`. `HuntersMarkState(EntityId sourceId, Owner sourceOwner, int bonusDamagePercent)` exposes `SourceId`, `SourceOwner`, `BonusDamagePercent`, `SameValues(HuntersMarkState other)`. `CharacterDebuffState.HuntersMark` is the only victim mark source.

**Rule interfaces:** in `HuntersMarkTraitRule`, internal static `EvaluateHit(CharacterCombatFacts attacker, CharacterCombatFacts victim, int damageBeforeHeldDefense, CombatDamageKind kind)` returns `(int Damage, bool ConsumeMark)`; internal static `CanApplyMark(CharacterCombatFacts attacker, CharacterCombatFacts victim, CombatDamageKind kind) -> bool` owns application eligibility; internal static `ApplyMark(HuntersMarkState current, EntityId sourceId, Owner sourceOwner, int bonusDamagePercent)` returns the single resulting mark. All are pure; no dictionary writes. Exact semantics follow G-M. The application hook may branch on these returned decisions but must not repeat their source/team/kind/no-stack conditions.

Representative approved-G-M assertion for non-stacking (in HuntersMarkRuleTests; normal test imports omitted):

```csharp
var current = new HuntersMarkState(new EntityId(1), Owner.Player, 10);
var result = HuntersMarkTraitRule.ApplyMark(current, new EntityId(2), Owner.Player, 25);
Assert.That(result.SameValues(current), Is.True);
Assert.That(result.BonusDamagePercent, Is.EqualTo(10));
```

- [x] Confirm G-M. Write `DefaultTenPercent_RoundTripsAuthoringAdapterRegistryCodecStatus`, `DifferentAlly_TwentyBecomesTwentyTwo`, `SameSource_NoConsumption`, `RepeatedApplication_DoesNotStackOrReplaceExistingSource`, and `IndependentStatusChanges_PreserveMark`. Pin approved zero/large percentage and rounding cases explicitly.
- [x] Run `ProjectL.Tests.Core.HuntersMarkRuleTests` / `HuntersMarkStateTests` and `ProjectL.Tests.CoreIntegration.HuntersMarkAuthoringTests` red.
- [x] Implement the existing-style type family, factories, rule, and status preservation. Do not store victim marks in `HuntersMarkRuntimeState`; that class is only the owner's registration marker. Preserve expected-state/equality checks through existing facts.
- [x] Add `ConfiguredPercentage_ChangesThroughRealAuthoringPath` with 10 and 25, and verify the registry/adapter supplies the tested value. Classify registration branches as construction only and all gameplay predicates as HuntersMarkTraitRule responsibilities in the boundary record.
- [x] Run the suites green plus existing debuff state tests and trait registry/codec validation. Test source death/turn transitions preserve the target mark; dead target/battle disposal clear it under G-M.

Death/disposal preservation tests must exercise the actual command and existing `C/Application/Combat/CombatTemporaryStateCleanup.cs` boundaries, including direct lethal damage and non-damage falls. Do not claim lifecycle coverage from a manually constructed empty status. Add a narrow mark cleanup hook at the existing lifecycle boundary if needed; never put a second victim-state owner in the trait runtime marker or clear unrelated statuses as a shortcut.

### B2. Connect direct-hit preparation, health application, and publication

**Files:** create `C/Application/Combat/HuntersMarkApplication.cs`, `C/Contracts/Combat/CombatHuntersMarkEvent.cs`; modify `AttackDirectDamageCalculator.cs`, `CombatEvent.cs`, and only existing result/ledger paths that need the adjusted final damage. Create `T/HuntersMarkAttackTests.cs`, `T/CoreIntegration/HuntersMarkPublicationTests.cs`.

**Interfaces:** `HuntersMarkApplication.PrepareHit(CommandAttackPlan plan, CommandAttackProjection world, EntityId targetId, int damageBeforeHeldDefense) -> int` computes/consumes an eligible existing mark in the local projection. `CompleteHit(CommandAttackPlan plan, CommandAttackProjection world, EntityId targetId) -> void` applies the attacker's configured mark to a surviving qualifying enemy after actual direct health application. `CombatHuntersMarkEvent(EntityId targetId, HuntersMarkState expected, HuntersMarkState final)` publishes immutable status change facts, with a new stable event kind. No-op transitions emit no event.

- [x] Add `FirstDeliveredHit_ConsumesBeforeLaterHitCalculation`, `SecondShotgun_ConsumesThenReappliesOneMark`, `ShieldedHit_UsesReviewedConsumptionRule`, `IndirectAndReactionDamage_DoNotConsume`, `PreviewAndAi_DoNotMutateSource`, and `StaleExpectedMark_RejectsAllWrites` under G-M.
- [x] Run Core attack and CoreIntegration publication suites red. Use the current service/publication fixtures; do not copy a Frenzy fixture or mock away Buffer expected-state validation.
- [x] Insert PrepareHit after `CombatDamageCalculator.Attack` and before `ApplyHeldDefense`, only for qualifying delivered damage hits. Update breakdown/ledger consistently so preview agrees. Consumption must happen during per-hit calculation, not after the whole attack, because HP applications are queued.
- [x] Call CompleteHit after that hit's actual `world.Damage.Apply` inside the shared health-flush boundary. Preserve reflection flush points, death accounting, and ordinary follow-up behavior. Do not append a second attack/damage group for the bonus. Add status events in order without using events as the state owner.
- [x] Add `EquivalentTraitOnDifferentUnitKey_PreservesMarkBehavior` and `SameSyntheticUnitWithoutTrait_DoesNotApplyMark` using otherwise equivalent valid definitions. Inspect the changed shared damage methods: they may contain timing/hook/result handling, but no mark math, source eligibility, or no-stack conditions. A helper method elsewhere in that same shared class is not an acceptable rule owner.
- [x] Run the new suites green, plus multi-hit, reflection, physical-defense, held-defense, preview and transaction rejection regressions. Prove a source with no mark trait can consume another ally's mark.

### B3. Migrate Shotgun and expose configured description data

**Files:** `DB/InGameGroup/Unit/Item/Playerable/Unit_ShotGun.asset`; new `DB/ViewGroup/TraitVisual/Item/TraitVisual_HuntersMark.asset` and the existing trait visual table; existing card description consumer `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Global/UnitCardTraitContent.cs` in the View-owned portion; authoring/asset tests and `T/Framework/UnitCardTraitContentTests.cs`.

**Interfaces:** Shotgun key 11 remains Heavy with the same other stats/targeting; its one base trait becomes HuntersMark default 10. Core status supplies the configured percent for card display. DistanceDamageScale remains registered for other content.

- [x] Add real asset assertions for Shotgun and a configurable description test (10 -> 25 reflects the snapshot, not a static string). Run red.
- [x] Migrate the asset payload explicitly, update visual registration, and implement the narrow existing description adapter with the View owner. Do not fabricate a final icon/VFX asset if art is unavailable; record missing art acceptance rather than claiming a finished visual.
- [x] Run authoring/catalog/card tests green, existing DistanceDamageScale tests, then required phase suites. Verify mark status survives through the exposed character facts used by View.

**Executed verification owner:** The user retained Core/CoreIntegration-only execution. The card/asset assertions run in `T/CoreIntegration/HuntersMarkCardContentTests.cs` and `RetiredTraitAuthoringTests.cs`; no Framework assembly was executed in Phase B. Existing owned-snapshot and deck-display-model connections are documented in section 9.

**Phase B checkpoint:** close AC-1–AC-6, including an evidence-backed trace of both hit hooks and the alternate-key/configuration tests, then stop and report. Mark calculations/publication/content are tested; final in-world status visuals may remain D-owned and must be reported separately. Cowboy remains in the Phase A development state. G-M decisions are recorded, not inferred.

## 6. Phase C — Pull

### C1. Add Pull trait and one shared range/selection evaluation

**Files:** create `Pull` files in section 2.1, `C/Application/Combat/PullAttackEvaluation.cs`, `PullAttackCalculator.cs`; modify `CombatAttackQueries.cs`, `AttackSelectionEvaluation.cs`, and `C/Contracts/Results/AttackRangeResult.cs`. Create `T/PullRangeTests.cs`, `PullTargetAdmissionTests.cs`, `T/CoreIntegration/PullAuthoringTests.cs`.

**Interfaces:** `PullTraitData(int extraRangeCells)`, `PullTraitSpec(int extraRangeCells)`, `PullTraitDefinition(int extraRangeCells)`, and `PullTraitStatusSnapshot(int extraRangeCells)` expose `ExtraRangeCells`; authoring defaults to 1. Internal static `PullTraitRule.SelectionRange(int attackRange, int extraRangeCells) -> int`; internal static `Destination(CellCoord actor, CellCoord contact, int attackRange) -> CellCoord`; internal static `DealsAttackDamage(Owner attackerOwner, Owner targetOwner) -> bool`, using the existing HostilityPolicy rather than a new team comparison system. Destination rejects non-cardinal contacts and leaves contacts inside A unchanged.

Representative geometry assertions (in PullRangeTests):

```csharp
Assert.That(PullTraitRule.SelectionRange(3, 2), Is.EqualTo(5));
Assert.That(PullTraitRule.Destination(new CellCoord(0, 0), new CellCoord(5, 0), 3),
    Is.EqualTo(new CellCoord(3, 0)));
Assert.That(PullTraitRule.Destination(new CellCoord(0, 0), new CellCoord(2, 0), 3),
    Is.EqualTo(new CellCoord(2, 0)));
```

`PullAttackCalculator.Evaluate(CombatCommandInput input, CharacterCombatFacts actor, CombatAttackProfile profile, ResolvedAttackSelection selection, MovementEnvironmentFacts environment, WallGeometry walls) -> PullAttackEvaluation` performs no mutation. Evaluation exposes `FailureReason`, `TargetId`, `StartAnchor`, `DestinationAnchor`, `Path`, `RequiresPull`, and `DeliverDirectDamage`. The last value is supplied by PullTraitRule, not recomputed in the common damage path. Attach the evaluation to `AttackSelectionEvaluation` so execution consumes the same decision. Range computation derives A from `EnvironmentTerrainRules.AppointRange`; P comes from the trait. Add `BasicAttackCells` alongside existing `RangeCells` (total selectable reach) using backward-compatible constructor defaults for units without Pull. Do not change base stat meaning.

- [x] Confirm G-P: rules 1–3 approved; obstacles excluded from Pull and keep ordinary attack rules.
- [x] Add parameterized cases: A=3/P=2, d=1..6 in four directions; distances 1–3 unchanged, 4–5 destination distance 3, 6 rejected. Add default 1/1, P=0, range modifiers, negative/overflow/unsupported configuration, diagonal and self rejection, friendly eligibility/no-op under G-P.
- [x] Run the three suites red. Implement geometry in PullTraitRule, eligibility in the existing effect capability, and shared evaluation used by `Selection`, `RangeCells`, unit range and attack range/AI consumers. Inspect every `AppointRange`/`CalculateRangeCells` caller to prevent a second independent Pull formula.
- [x] Add path preflight against current footprint occupancy/walls; board clipping does not permit illegal paths. Keep hidden-environment observations within the existing preview contract. No displacement occurs here.
- [x] Run green with ordinary target policy/range/admission and authoring suites. No special-case `Cowboy` entity key is permitted.
- [x] Add `EquivalentPullTraitOnDifferentUnitKey_UsesSameRangeAndDestination` and `SameSyntheticUnitWithoutPull_UsesBaseRange`. Demonstrate that changing P through authoring affects the query/evaluation through the same rule, with no copied `A + P` formula in query consumers. Complete execution-side coverage in C2 once execution exists.

### C2. Execute Pull before damage using the current world projection

**Files:** `PullAttackCalculator.cs`; modify `CombatAttackCalculator.cs`, `AttackDeliveryExecutor.cs`, `CommandAttackPlan.cs`, `CommandAttackProjection.cs`, `AttackDirectDamageCalculator.cs`, `AttackResultFactory.cs`; use `ForcedDisplacementPathCalculator.cs`, `MovementProjection.cs`, `MovementEnvironmentCalculator.cs`, and `AttackKnockbackCalculator.cs` in `C/Application/Combat/`. Create `C/Contracts/Results/AttackPullSnapshot.cs`, `C/Contracts/Combat/CombatPullEvent.cs` and add the corresponding kind in `CombatEvent.cs` in this task, before execution consumes them. Create `T/PullAttackTests.cs`, `PullTerrainTests.cs`, `PullObstacleTests.cs` (ordinary obstacle attack/no-Pull regressions).

**Interfaces:** `PullAttackCalculator.Execute(PullAttackEvaluation evaluation, CommandAttackPlan plan, CommandAttackProjection world, CommandAttackLedger ledger, MovementProjection arrival)` returns `(bool CanDeliverDamage, AttackPullSnapshot Movement)`. `RebuildDelivery(CommandAttackPlan originalPlan, CommandAttackProjection world, MovementProjection arrival, EntityId targetId) -> CommandAttackPlan` preserves original input/expected-state identity while using projected actor/target/environment for selection and delivery. It never chooses a different entity now occupying the old cell.

`AttackPullSnapshot(EntityId targetId, CellCoord startAnchor, CellCoord finalAnchor, IReadOnlyList<CellCoord> path)` defensively copies actual traveled cells and exposes those values. `CombatPullEvent(AttackPullSnapshot movement)` carries them in the existing ordered event stream for units only. Obstacles remain on their ordinary attack path. Create and test these contracts here; C3 consumes them rather than supplying a forward dependency. For a no-movement outcome, Movement is absent, and basic inner-enemy attacks remain valid.

- [x] Add `OuterEnemy_MovesBeforeDirectDamage`, `BlockedRoute_NoMovementDamageOrCost`, `FriendlyPull_DoesNotTouchShieldReflectionMarkOrVictimAction`, `HazardDeath_SpendsOnceWithoutDirectHit`, `ArrivalTerrainControlsDamage`, `Obstacle_InnerAttackUnchanged_OuterAttackRejected_NoMovement`, and `OldSelectedCell_IsNeverRetargeted`, plus reviewed immunity cases.
- [x] Run red. Validate cost and geometry before mutation; create only one command world/arrival projection. `CreateArrivalProjection` currently rejects duplicate creation: reuse and pass that instance to the existing executor rather than constructing another world or suppressing the guard.
- [x] Execute the pre-hit Pull stage, applying existing per-cell terrain/contact/normalization rules. Extract only truly shared unit displacement mechanics from `AttackKnockbackCalculator` if necessary into `C/Application/Combat/AttackForcedMovement.cs`, with the same source/target/projection contracts; keep Pull destination policy in PullTraitRule and knockback direction/immunity policy in its owner. Do not use a negative knockback distance, skip terrain, or clone its entire body. Record the extraction's regression obligations before editing it.
- [x] Record the exact shared movement functions reused/extracted and their actual callers. This is a required reference audit, not an instruction to create AttackForcedMovement unconditionally. If the extraction cannot preserve existing movement ownership/order within this task, report that specific deviation; do not implement a Cowboy-only movement copy.
- [x] Rebuild delivery from projected facts for a living eligible target. Pass the same arrival projection to subsequent stages. A friendly target bypasses direct damage and hostile-hit accounting entirely; zeroing damage after shield/reflection processing is incorrect. Keep ordinary post-hit knockback unchanged for non-Pull attacks.
- [x] Place actual Pull movement events before the subsequent attack/hit events; update `attackEventIndex` insertion accordingly. Preserve existing cost settlement exactly once even on accepted terrain-terminal paths. Do not append late movement behind a prebuilt stale direct hit.
- [x] Run Pull tests green and affected movement, knockback, terrain, Obstacle, reflection and attack ordering suites. Test Pull combined with a retained after-hit effect only where the existing enhancement system can legally produce that combination; do not invent a new combination system.
- [x] Extend C1's alternate-key, trait-absent, and edited-P cases through actual execution. Verify the executor consumes the evaluated range/destination/damage permission rather than repeating their gameplay conditions.

### C3. Expose results/preview and verify transaction boundaries

**Files:** consume C2's `AttackPullSnapshot.cs` and `CombatPullEvent.cs`; modify `C/Contracts/Results/AttackResult.cs`, `AttackPreview.cs`, `AttackTargetPreview.cs`, `AttackResultFactory.cs`; existing `S/RuntimeSystem/BufferSystems/Core/BattleTransactions.cs` only if assertions demonstrate a missing expected-state/installation invariant. Create `T/CoreIntegration/PullPublicationTests.cs`, `T/PullPreviewTests.cs`.

**Interfaces:** add an optional Pull snapshot to AttackResult/AttackPreview creation; existing callers default to absence. C2 supplies the immutable actual movement/event contract. Mark events and Pull events use distinct new stable kinds appended without reusing FrenzyWave 30.

- [x] Add `RepeatedPreview_LeavesWorldAndMarkUnchanged`, `PreviewArrivalAndDamage_MatchVisibleExecution`, `StaleTargetPositionOrMark_RejectsAllStores`, `ObstacleOrdinaryDamage_LeavesOccupancyUnchanged`, and `FriendlyMove_PublishesWithoutFakeDamageHit`.
- [x] Run red; construct outcome snapshots from actual projection changes, including shortened hazard paths. Do not report the intended full destination as reached after death. Preserve original occupied-cell semantics in existing preview properties and expose the arrival through the Pull snapshot.
- [x] Integrate without a separate writer or store. A movement-only friendly result needs explicit movement/target data even when the damage ledger is empty. No duplicate presentation movement event for the same step; route new Pull facts once rather than also animating generic Moved events for the same displacement. Obstacles never produce Pull snapshots/events.
- [x] Run green through real command publication, covering stale occupancy and retry. Verify no public preview field leaks hidden terrain and that costs/action-use snapshots remain authoritative.

### C4. Migrate Cowboy and finish Core acceptance

**Files:** `DB/InGameGroup/Unit/Item/Playerable/Unit_Cowboy.asset`; reuse a compatible cardinal/single/unrestricted targeting entry or create `DB/InGameGroup/Targeting/Item/TargetingMethod_CowboyPull.asset` with its table reference and meta; new `DB/ViewGroup/TraitVisual/Item/TraitVisual_Pull.asset` and table registration; `T/CoreIntegration/PullAuthoringTests.cs` and affected real asset tests.

**Interfaces:** Cowboy key 19 remains Rider, base AttackRange=1; Pull extension=1; method shape=1, choiceMode=1, choiceAngle=0, splash=None. Other stats remain unchanged. Base range and extension tune independently; total reach is not serialized into a second field.

- [x] Add real authored assertions and a reverse-reference check proving other users of `TargetingMethod_1.asset` retain their old shape. Run red.
- [x] Migrate Cowboy and visual content explicitly; never overwrite the shared radial method globally. Verify production/default/dev definitions agree where they represent Cowboy, without altering unrelated fixtures to force a match.
- [x] Run authoring/DB checks and full Core/CoreIntegration phase gates, with Framework execution excluded by the user for this phase. Re-run Mark/Pull interaction tests where shared hit ordering changed.

**Phase C checkpoint:** close AC-1–AC-6, including the shared traversal reference audit, alternate-key/configuration tests, and inspection of all changed common attack/query branches, then stop and report. Cowboy's new Core behavior and content are complete only if query, preview, calculation, real publication, and architecture checks pass. Final View motion/mark display remains a separate acceptance boundary.

## 7. Phase D — coordinated View integration and final acceptance

### D1. Connect existing range, description, and playback consumers

**Files/owners:** View owner modifies `V/Input/Interaction/AttackTargetingInteraction.cs`, `V/Playback/Ordering/CombatEventRoutes.cs`, `CombatEventSubjects.cs`, `OrderedCombatPresentation.cs`, `V/Playback/CombatSceneEventPlayback.cs`, `CombatSceneEventPresenter.cs`, and the existing display/health/status presenter selected by its current ownership. Reuse `V/Presentation/Movement/CombatUnitMotionPlayback.cs` and the existing Obstacle visual binding. Card descriptions remain in `S/RuntimeSystem/GlobalSystem/UISystem/Screens/Global/UnitCardTraitContent.cs`. New small presenter files are justified only by a concrete missing consumer, not created as feature bootstraps.

**Tests (execution override, 2026-10-26):** the user limited execution to Core and CoreIntegration. Implemented `T/CoreIntegration/PullAndMarkPresentationTests.cs` exercises the actual View boundaries without linking Framework tests. The originally planned separate Framework test files/runs are superseded for this authorized phase; Framework and manual acceptance remain separately recorded. Create `docs/superpowers/handoffs/2026-10-26-category-unit-trait-core-view-handoff.md` with actual contract signatures, event ordering, owner assignments, verification, and remaining art acceptance.

**Interfaces:** Core supplies BasicAttackCells/RangeCells, actual Pull path, mark change events and configured trait/status snapshots. View submits the existing attack aim, animates movement before hit, renders one mark, and displays authored values. It does not calculate range/destination/bonus or mutate marks.

- [x] Add `Pull_CompletesMovementBeforeDamagePresentation`, `ObstacleAttack_DoesNotAnimatePull`, `MarkApplyConsume_OneIndicator`, `ConfiguredValues_AppearOnCards`, and `CancelOrSceneExit_ReleasesMotionAndMarkVisuals`. Pin event order rather than relying on incidental enumeration/grouping.
- [x] Run behavioral red in CoreIntegration under the user's two-assembly override; connect existing routing, UI and presentation lifecycle while preserving input/registration. No new skill mode/button or subscription graph.
- [x] Run new playback/resource/card/range checks and all Core/CoreIntegration regressions. The actual production-asset checks already in CoreIntegration ran; Framework `DataBaseAssetTests` and other Framework tests were not run under the user's explicit limit. Four Framework fake signatures were updated only for compilation. No unrelated repair.
- [x] Inspect all new View branches: event routing and presentation lifecycle are allowed; unit-name/ID checks, bonus math, range/destination reconstruction, or independent mark lifetime are not. Confirm changed percentages/ranges appear from snapshots rather than shared hard-coded constants.
- [ ] Perform manual scenes: inner/outer enemy attack; blocked Pull; ally movement and unchanged obstacle attacks; A=3/P=2; mark application/consumption/no-stack; changed percentage; final cleanup on exit. Record art/animation gaps separately from Core success.

**Final checkpoint:** close AC-1–AC-6 for the integrated diff, then stop and report implemented scope, actual file/contract changes, retained/deleted test disposition, evidence and remaining acceptance. No commit/push. This checkpoint never treats “Core passed” as completed View integration or passing tests as proof of compliant architecture.

## 8. Locality assessment and self-review

| Change case | Necessary dependencies / current cost | Minimal boundary and expected after-state | Verification that remains necessary |
|---|---|---|---|
| Change mark eligibility or bonus math | Direct damage batches HP; status copies share CharacterDebuffState. A naive patch spreads rules into damage/flush/copy code. | HuntersMarkTraitRule owns decisions; HuntersMarkApplication owns the two named hit hooks. Registration edits only when contracts change. No Pull internals needed. | Rule tests, queued-hit/reflection/shield order, state preservation, transaction checks because these are real dependencies. |
| Tune Pull extension or landing distance | Range query and selection use separate calls to base range; old selection facts are reused during damage. | One Pull rule plus one evaluation shared through admission; only arrival delivery adapter crosses into orchestration. No mark percentage logic needed. | Query/preview/AI/execute parity and arrival-sensitive damage; tuning alone should not change registries. |
| Change shared forced terrain mechanics | Knockback currently includes collision/terrain/contact code. Those are genuinely shared with Pull, but destination/immunity policies differ. | Extract only proved shared traversal/application if required. Keep policy separate and never duplicate live state. | Knockback and Pull terrain regressions are required, not evidence of failed separation. |

Confirmed by source inspection: existing directory/registration structure; target status owner; queued direct health application; arrival projection single-creation guard; range/selection shared entry points; current immutable result/event contracts. Predicted only: reduced future understanding/edit scope after the proposed boundaries. No runtime verification was performed while writing this plan.

Self-review coverage: spec removals -> A1/A3; preserved tests -> A0/A2 plus each retirement; mark behavior/configuration/state/publication -> B1–B3; Pull range/order/terrain/publication/content -> C1–C4; View/FW/manual -> D1. G-M/G-P proposals remain explicitly gated. Task signatures use the same names across callers, files, results, and tests. All five Review Focus items have named owning test cases. Phase checkpoints distinguish compiling development content from final release acceptance.

Planning checks corrected a forward dependency by creating Pull movement contracts in C2, before C3 consumes them, and moved production/dev authoring assertions into CoreIntegration rather than the pure Core assembly. Existing path references and document links were checked; planned new files are explicitly labeled. No tests were executed for this document-only task.

The architecture revision adds mandatory placement/connection/completion checks and ties them to each phase. Self-review checked that construction dispatch, normal validation, explicit orchestration, and required shared regression remain allowed; no arbitrary ban on conditionals, minimum file count, or mandatory generic framework was introduced. New rule-owned eligibility functions match the connection map and task interfaces. C1 checks query/evaluation; C2 adds execution evidence, avoiding a new forward dependency.

## 9. Execution checkpoint template

For each authorized phase, record: approval/gate answers; actual starting HEAD/status; case-level retire/adapt/preserve mapping; files and consumer contracts changed; red/green commands and XML/log paths; observed locality changes versus predictions; baseline failures; unrun manual/View checks; next permitted phase. Do not mark any checkbox from historical test counts or another session's result.

Include the required architecture evidence in this compact form; reuse rows across tasks when the same boundary is unchanged:

| Check | Actual evidence required | Result |
|---|---|---|
| Task entry | Rule owner/functions, input/output/state, call order, planned files and tests. | Recorded / missing |
| AC-1 | Changed predicates/formulas -> owning file/function; classification of new shared-method branches. | Pass / not applicable with reason / fail |
| AC-2 | Actual authoring/registration/evaluation/projection/publication/consumer path and preview/AI callers. | Pass / not applicable with reason / fail |
| AC-3 | Authoritative values/state, projected copies and installation owner. | Pass / not applicable with reason / fail |
| AC-4 | One relevant change example with concrete edit/understanding/regression scope; necessary shared dependencies remain explicit. | Pass / not applicable with reason / fail |
| AC-5 | Alternate-unit-key, trait-absent, and edited-configuration test names and results. | Pass / not applicable with reason / fail |
| AC-6 | Removed path closure and method/helper disposition with retained verification evidence. | Pass / not applicable with reason / fail |

Do not write only “SOLID/SSOT satisfied.” Cite actual functions and observed paths. Any remaining failed or unreviewed applicable check leaves that task incomplete; describe it rather than claiming architecture compliance.

### Phase A execution — 2026-10-26

The user explicitly authorized Phase A implementation and verification, followed by a stop at its checkpoint. This supersedes the historical planning-only status for Phase A. G-C uses the exact planned development checkpoint: Cowboy temporarily has no base trait; Shotgun retains DistanceDamageScale. G-M/G-P and Phases B–D remain unexecuted. No commits or pushes are authorized.

Starting checkout: `feature/Cheon/Core/NewCombatSytem`, HEAD `c816c46df00c63bbb7065328f4a73f829f11895d`, clean working tree. The earlier test-foundation migration is committed here; current CoreIntegration is the integration owner. Execution evidence is under `.superpowers/sdd/category-and-unit-trait-rework/`.

| Task entry | Owner, facts and connection | Expected edits and checks |
|---|---|---|
| A1 | Category authoring/adapters/default/dev sources create definitions; CategoryRegistry/CategoryDefinition validate composition; BattleCharacterCalculator builds published snapshots. CommandAttackPlan and the shared damage pipeline pass captured protection to direct/follow-up/reaction damage. The protection query is created in CombatSystemFactory, exported through InGameCoreSystemGroup, and consumed through IHeavyProtectionReadSink. | Remove the obsolete rules, capture/query/metadata and their complete factory/group/key path. Migrate UnitData/Spec/Definition/owned resolver and movement equality together. Preserve common incoming effects, damage order, settlement and Buffer publication. Test no-offset Heavy construction, protected/control equivalence, ordinary Rider cost/action ownership, physical/held defense and knockback. |
| A2 | CharacterDebuffState owns immutable victim components; CombatDebuffCalculator and DefenseReductionStatusRule copy/tick them. Shared player-turn settlement owns one action and budget. CombatResultPlayback/OrderedCombatPresentation own event continuation/finalization. | Preserve state copy/equality with Intimidation plus defense reduction, map ordinary action/no-refund assertions to current player-turn/retirement cases, and replace the failing Frenzy-dependent playback fixture with an independent surviving unsupported event. No production changes in this task. |
| A3 | Trait authoring -> adapter/registry/codec/status -> AttackDeliveryExecutor/AttackDirectDamageCalculator -> FrenzyAttackLedger/FrenzyAttackCalculator. CharacterDebuffState carries the obsolete priority component; Cowboy and the visual table author Frenzy. | Remove the complete trait/wave/priority family and typed arguments; retain shared health flushing/reflection, KillChain, follow-ups, costs and publication. Explicitly migrate debuff constructor call sites and active managed-reference/visual content. Test ordinary direct kill without a second attack and actual Cowboy/catalog deserialization. |

Fresh A0 baseline: Core **3540/3540**, CoreIntegration **44/44**, selected Framework **206/279**, **73 pre-existing failures**, zero skips; Unity exit 2. `A0-baseline.xml`/`.log` and `A0-baseline-failures.tsv` record every selected case and failure message. Framework failures: BattleDefinitionAdapterTests 32, DataBaseAssetTests 21, HeavyProtectionQueryServiceTests 8, CombatEventRoutesTests 3, FrenzyPublicationTests 3, HeavyProtectionAuthoringTests 2, UndeadHeavyDefinitionTests 2, CombatEventHandlerTableTests 1, FrenzyViewContractTests 1. These are baseline evidence, never a justification to delete surviving coverage.

#### A. Verification and review result

The user subsequently restricted execution to **Core and CoreIntegration only**, citing existing Framework test problems. No further Framework tests were run after that instruction. Earlier selected Framework runs are historical evidence within this execution, not a green Framework gate. Actual asset assertions now run in CoreIntegration without importing Framework fixtures.

| Evidence label under the execution directory | Observed result |
|---|---|
| `A0-additional-consumers-baseline` | Additional affected legacy Framework consumers: 258/375, 117 existing failures. Exact identities/messages retained separately. |
| `A1-red-confirmed` | 41/45 passed; four intended failures demonstrate Heavy protection and mandatory offsets before removal. |
| `A1-green-A3-content-red` | Category replacements pass; real Cowboy trait payload and retired numeric-key assertions fail before Frenzy removal. |
| `A3-core-integration-first-green` | Core 3426/3427; CoreIntegration 50/50. One migrated DoubleShot expectation still included retired extra-wave damage; corrected to ordinary initial/repeat damage. |
| `A-core-integration-final` | Core 3427/3427; CoreIntegration 51/51; zero skips, exit 0. Includes repeated service preview/execution. |
| `A-core-integration-reviewed` | Core 3428/3428; CoreIntegration 51/51; zero skips, exit 0. Restores the independent Cannon-required-rule assertion. |
| `A-core-integration-final-coverage` | 3479/3480; the added overflow preservation fixture supplied only one distance percentage for a distance-two target, so it raised configuration validation instead of the intended overflow. Test input corrected to cover both distances. |
| **`A-core-integration-verified`** | **Core 3428/3428; CoreIntegration 52/52; total 3480/3480; zero failed/skipped/inconclusive; Unity exit 0.** Includes calculation-failure/no-publication coverage. |

Final command: `& '.superpowers/sdd/category-and-unit-trait-rework/run-tests.ps1' -Label 'A-core-integration-verified' -Assemblies 'ProjectL.Tests.Core;ProjectL.Tests.CoreIntegration'`. The script launches Unity 6000.0.73f1 through the approved AppData-access path with EditMode and the two exact assemblies. Each label retains fresh XML and log files.

One fresh-context review found no runtime defect and one Important coverage omission: deleting `FrenzyPublicationTests.LateWaveOverflow_LeavesCharacterCombatAndCommandStateUnpublished` lost the service-level calculation-exception guarantee. `CombatCommandPublicationTests.CalculationOverflow_LeavesEveryStoreAndCommandMarkersUnpublished` now invokes the real `ProcessNextCommand` with a valid surviving DistanceDamageScale configuration that overflows during damage calculation. It compares every store using `CoreIntegrationSnapshot`, result/handled counters and the pending command, then retries to prove no partial publication. This is preserved behavior, so no artificial production regression was introduced to manufacture red. The final whole-suite run above covers the correction. No second review was dispatched.

Core case identity changes are retirement, adaptation and transfers, not simply deleted coverage: baseline 3540 -> 3428. CoreIntegration retains every baseline identity and adds eight cases, 44 -> 52. Detailed old/new case identities and original dedicated fixture bodies are retained in the execution evidence. Passing these two assemblies does not establish Framework or manual View acceptance.

#### B. Mixed-case and helper disposition

All method names below refer to the original fixture unless a destination is specified. Retired-only expectations are removed because their feature no longer exists; baseline failures were not used as a deletion criterion.

| Original fixture / method | Disposition and surviving destination |
|---|---|
| `HeavyProtectionAttackIntegrationTests.HeldDefense_IsConsumedAfterProtectionReduction` | Adapted as `CombatDamageBoundaryTests.HeldDefense_IsConsumedAfterIncomingCalculation`; ordinary incoming/held-defense order remains tested. |
| `AttackOnly_LeavesThornDamageUnchangedInsideProtection`, `AttackOnly_LeavesReflectionUnchangedInsideProtection`, `AttackOnly_LeavesAttackTriggeredExplosionUnchangedInsideProtection` | Adapted in the same moved fixture as `ThornDamage_IsUnchangedBesideHeavy`, `Reflection_IsUnchangedBesideHeavy`, `AttackTriggeredExplosion_IsUnchangedBesideHeavy`. |
| `AttackBoundary_RequiresCapture_AndExcludesStructuresAndEnvironment` | Capture-only assertion retired; categoryless structure/environment damage retained as `AttackAndEnvironment_PreserveCategorylessStructureDamage`. |
| `ProviderDeath_SameAttack_OrderInvariant`, `KillChain_UsesCurrentProjectionAfterProviderDeath`, `Reaction_RefreshesAfterProviderDeath` | Adapted as `OtherHeavyDeath_SameAttack_OrderInvariant`, `KillChain_UsesCurrentProjectionAfterOtherVictimDeath`, `Reaction_UsesCurrentProjectionAfterOtherVictimDeath`; no protective-provider prerequisite. |
| `NextTurnAttack_UsesCurrentProjection`, `Preview_MatchesExecutionWithoutChangingInput`, `UnprotectedHeavy_HasNoSelfArmor` | Retained in `CombatDamageBoundaryTests`. Eleven surviving methods / fourteen cases retain this fixture's independent guarantees. Its moved `.meta` GUID is unchanged. |
| `Protection_IgnoresWallBetweenProviderAndRecipient`, `Frenzy_CapturesOncePerWave_ThenRefreshes`, `HeavySnapshotEquality_UsesContentsInMovementContext`, `MoveAndLaterOwnerTurnMoveBack_AreObservedByNextCapture` | Retired: protection geometry, capture and protection metadata no longer exist. Movement equality remains in `CharacterCombatFactsSameValuesTests` and installed-movement invalidation tests. |
| `HeavyProtectionIncomingEffectsTests.Engine_CapsCombinedContributionsAtIncomingDamage` | Preserved as `CombatEffectEngineTests.IncomingDamage_CapsCombinedContributionsAtIncomingDamage`, using two synthetic incoming contributions through the actual engine. |
| `RiderManeuverRefundTests.NegativeRefundContribution_IsRejected` | Preserved as `CombatEffectEngineTests.RefundContribution_RejectsNegativeValue`; the generic refund contribution contract survives. |
| `FrenzyAttackTests.RiderMoveAndWaves_RespectOneActionAndNoRefund` | Wave assertions retired; ordinary movement/attack exclusion, next-turn action, budget and source immutability tested by `CategoryEffectRetirementTests.RiderMoveThenAttack_DoesNotRefundSharedBudget`, plus existing PlayerTurn movement/attack tests. |
| `FrenzyStatePreservationTests.ApplyAndClear_EitherOrderPreservesIndependentComponent`, `Equality_IncludesBothComponentsAndAbsence`, `ExistingFactCopies_PreserveCombinedStateAndSource` | Preserved in `CombatDebuffCalculatorTests.IndependentStatuses_ApplyAndClearPreserveOtherComponent`, `CombinedStatusEquality_IncludesEachComponentAndAbsence`, `ExistingFactCopies_PreserveCombinedStateAndSource`, using Intimidation and defense reduction. |
| `FrenzyStatePreservationTests.CommandCapture_MarkerAndDeferredDiscountDoNotTickAtTurnEnd` | Retired marker assertion; surviving discount capture/turn behavior remains in `EnhancementActionCostTests` and `CharacterActionCostLifecycleTests`. |
| `FrenzyPreviewTests.RepeatedPreviewAndAi_UseInitialDamageOnly_WithoutMutatingSource` | Wave-specific damage retired. Calculator preview immutability remains in `CombatDamageBoundaryTests.Preview_MatchesExecutionWithoutChangingInput` and existing attack-query/preview tests; real repeated preview/command immutability is added to CoreIntegration below. |
| `FrenzyHitAccountingTests.ZeroDamageDelivery_DoesNotArmPriorityDebuff_AndBlockedAttackRejects` | Priority marker retired; surviving zero-damage spend/admission covered by `PlayerTurnAttackTests.LegalZeroDamageStillSpendsAndConsumesOnce` and `MissingMandatoryTargetDoesNotSpendOrConsume`. |
| `FrenzyHitAccountingTests.BarrelOnlyKill_IsNotDirectCredit`, `ReflectionKillsActor_DirectDeliverySettlesButNoWaves` | Frenzy arming/wave predicates retired. Surviving damage origins, reflection/death order and one settlement remain in `CombatItemDamageSourceTests`, `ThornHeartReflectionDirectTests`, `ThornHeartReflectionScopeTests`, `CombatDamageBoundaryTests` and ordinary player-turn tests. |
| `Framework/FrenzyPublicationTests.RepeatedPreviewThenAttack_PublishesAllWavesExactlyOnce` | Retired wave assertions; service guarantee preserved as CoreIntegration `CombatCommandPublicationTests.RepeatedAttackPreviewThenCommand_PreservesGameplayAndPublishesOnce`. |
| `LateWaveOverflow_LeavesCharacterCombatAndCommandStateUnpublished` | Preserved as CoreIntegration `CalculationOverflow_LeavesEveryStoreAndCommandMarkersUnpublished`, described above. |
| `StaleFrenzyTransition_RejectsAtomically_AndEndBattleClearsRuntime` | Preserved with surviving status facts as CoreIntegration `Publication_StaleDebuffRejectsEveryStore_AndBattleEndClearsCharacters`. |
| `Framework/FrenzyViewContractTests.UnsupportedWaveMarker_DoesNotSuppressLaterHealthEventsOrFinalization` | Preserved as `Framework/CombatPlaybackContinuationTests.UnsupportedEvent_DoesNotSuppressLaterHealthEventsOrFinalization`, using surviving `CombatExperienceEvent`. It passed before the later Framework exclusion; no fresh Framework claim. Presentation/movement test doubles remain local to this test. |
| `Framework/HeavyProtectionAuthoringTests.ProductionShotgun_HasApprovedHeavyStatsAndPreservesCombatProfile` | Preserved in CoreIntegration `RetiredTraitAuthoringTests.ProductionShotgun_PreservesRemainingStatsAndDistanceTrait`. Actual pre-change asset stats HP35/attack7/defense5/priority30/range2 are preserved; obsolete HP40/attack6 expectations already failed at baseline. |
| `ProductionShotgun_InspectorCoordinatesRoundTripOnIndependentCopy`, `ProductionCategory_HasCommonRateWithoutFacingInput` | Retired protection-coordinate/rate authoring. Actual no-offset category/unit assembly and catalog deserialization move to `CategoryDefinitionMigrationTests` and `RetiredTraitAuthoringTests`. |
| `CombatItemDamageSourceTests.Frenzy_UsesExtraAttackCause` | Removed only the Frenzy source cases. Existing `KillChain_UsesExtraAttackCause` still verifies item behavior on the shared extra-attack damage cause. |
| `FrameworkRuntimeCompositionTests.HeavyProtection_CoreAccessorMatchesRegistration_AndBindsAtRoot` | Retired service-only registration/availability check; the service, key, factory registration and accessor are removed together. Other runtime composition tests remain. |
| `FrenzyPublicationFixture` and its `Create`, `Facts`, `Character`, `Attack` helpers | Renamed/reduced to `Framework/CombatItemBattleFixture`, preserving the original `.meta` GUID. Item publication/integration/timeline consumers still use it; no CoreIntegration dependency on Framework. Frenzy-specific parameters/payload are removed. |
| `FrenzyTestData`, retired Heavy area/capture/query helpers, `RefundSettlement` | Removed after callers were retired or migrated; no surviving callers. `UnitSpecTestFactory` now constructs units without protection offsets. Mixed authoring fixtures keep their original tests and remove only obsolete offset setup. |

The remaining dedicated methods below are exclusively retired feature contracts. Helpers and teardown routines owned solely by those deleted fixtures are removed with them; they own no shared runtime state.

| Retired fixture | Exact retired-only methods |
|---|---|
| `HeavyProtectionAreaTests` | `Area_TranslatesIndependentShapes`; `Area_ClipsWithoutClamping`; `Area_AllCellsOutsideBoard_ReturnsEmpty`; `Area_PreservesOriginAndAuthoredOrderOnRectangularBoard`; `Area_MovingAnchorTranslatesTheSameOffsets`; `Area_ReturnedArraysDoNotAliasOffsetsOrEachOther`; `Area_InvalidBoardBoundsFail`; `Area_MissingOrDuplicateOffsetsFail`; `Area_OverflowFailsBeforeClipping`. |
| `HeavyProtectionCaptureTests` | `Capture_UsesAnyOccupiedCell`; `Capture_ExcludesSelfEnemyDeadReserveAndStructures`; `Capture_OverlapChoosesStableProviderOnce`; `Capture_DoesNotRetainMutableWorld`; `Capture_AllowsOtherHeavyAndUnarmedProvider`; `Capture_DoesNotRotateProviderOffsets`. |
| `HeavyProtectionDefinitionTests` | `Catalog_PreservesExplicitShapeAndCopiesInputs`; `Constructors_RejectMissingDuplicateOrWrongCategoryOffsets`; `Heavy_NullOffsetsAreRejected`; `NonHeavy_OmittedOffsetsStayEmptyThroughCatalog`. No-offset construction has new passing controls. |
| `HeavyProtectionIncomingEffectsTests` | `Incoming_RoundsReductionDown`; `Incoming_UsesProviderOriginAndOneBinding`; `Incoming_RejectsMissingCapture`; `Spec_RejectsInvalidPercent`. Generic engine cap test is preserved above. |
| `HeavyProtectionQueryParityTests` | `Query_MatchesAttackCaptureAtEveryBoardCell`. |
| `Framework/HeavyProtectionAuthoringTests` | `Offsets_AreCopiedAtBoundaries`; `Authoring_ExportsFreshImmutableValuesThroughAdapter`; `Authoring_ValidatesCategoryAndDuplicates`; `Heavy_NullOffsetsAreRejectedWithoutAnImplicitShape`; `NonHeavy_OmittedOffsetsRemainEmpty`; `Authoring_RoundTripsSignedOffsets`. Production methods are mapped above. |
| `Framework/HeavyProtectionQueryServiceTests` | `Query_AvailabilityAndSnapshotLifetime`; `Query_RejectsReserveAndNonHeavy`; `Query_MatchesAttackArea`; `Query_ValidEmptyDiffersFromUnavailable_AndErrorsAreNotHidden`; `Query_RegisteredAndPostBound_WithoutDatabase`; `Snapshot_CopiesCallerCells`. |
| `RiderManeuverRefundTests` | `UnusedRule_RequestsCurrentPendingOnly`; `UsedOrZeroPending_RequestsZero`; `EngineAllocation_CapsRefundByRemainingCapacity`; `UsedTransition_HappensOnlyWhenActualRefundIsPositive`; `TurnStart_ResetsUsedFlag`; `SettlementPlan_AppliesSpendAndRefundAsOneFinalValue`. Negative generic contribution is preserved above. |
| `FrenzyAttackTests` | `ThreeWaveKills_GrantThreeMoreWaves_WithOneCostAndNoPriorityReduction`; `DistanceModifier_AppliesToOrdinaryHitAndWaves_AndChangesChainKills`; `Waves_UseCurrentRangeAndHostility_AndNoTargetsStopPendingWork`; `Wall_BlocksWaveVictimDespiteFreeAttack`. Surviving DistanceDamageScale and wall admission suites remain; Rider mixed case is mapped above. |
| `FrenzyPriorityTests` | `Evaluate_FloorsOneAccumulatedReductionAndClamps`; `RepeatedAndDifferentSources_AddToOneImmutableVictimTotal`; `Parameters_RejectInvalidPercent`; `Parameters_AcceptInclusiveBounds`; `ActiveState_RejectsNonPositiveTotal`; `AccumulationOverflow_FailsWithoutChangingSource`; `AbsenceAndInvalidInputs_KeepExistingContract`; `ExistingFlatPenalty_PrecedesFrenzyWithoutChangingAttackReduction`. Surviving Intimidation priority/validation tests remain. |
| `FrenzyScheduleTests` | `FrenzyPriorityMarker_DoesNotChangeControllerOrEligibility`; `PriorityComponentIsUnavailable_WhileAttackWavesRemainAvailable`. Controller/budget/eligibility coverage remains in PlayerTurn and `IntimidationScheduleTests`. |
| `FrenzyStatePreservationTests` | `RegistryAndStatus_PreserveAuthoredPercentage`; `RuntimeMarker_ClonesAndRoundTripsWithoutVictimState`; `FactoriesAndStatus_RejectWrongKindsAndCompareConfiguredValue`. Mixed state methods are mapped above. |
| `Framework/FrenzyAuthoringTests` | `DefaultEntry_ExportsValidFivePercent`; `Exports_AreFreshImmutableAndIndependentOfLaterEditing`; `SharedAdapterAndBattleProfile_PreservePercentageAndOtherTraits`; `InvalidPercent_RejectsAtDataUnitAndSharedAdapter`; `DuplicateFrenzy_UsesExistingUnitValidation`; `OwnedQuery_UsesSameAuthoredSevenWithoutChangingBasePriority`; `ManagedReference_ReloadAndInspectorEditingPreserveConfiguredValue`. Actual Cowboy/catalog migration is tested in CoreIntegration. |

Other mixed fixtures retain their methods: category contract/bootstrap tests use stateless Rider/Heavy and surviving SafeBombardment or a local synthetic state for generic clone/transition validation; Cannon still requires its own rule. `EnhancementDoubleShotTests.UnrelatedEffectsRemainEnabled_OnRepeatKills` retains Regeneration and ordinary distance-scaled repeat damage. Environment, targeting, rewards, status, movement, authoring and publication fixtures explicitly migrate removed arguments while retaining their surviving assertions.

#### C. Actual boundary and locality checks

| Check | Actual result and evidence |
|---|---|
| AC-1 | **Pass for removal.** CategoryRegistry/CategoryRuleFactories no longer construct refund/protection rules. AttackDirectDamageCalculator/AttackDeliveryExecutor remove protection/Frenzy capture and wave orchestration; ordinary damage, shared flushing, reflection, KillChain and one settlement stay with their existing owners. No new feature formula or runtime unit-key branch was added. |
| AC-2 | **Pass.** UnitDataSO -> UnitData -> UnitSpecAdapter -> UnitCatalog/CategoryRegistry -> BattleCharacterCalculator -> command/query/preview -> existing BattleWriteSink remains the path. HeavyProtectionQueryService -> factory -> service key -> group accessor -> read sink is completely removed, with no empty service/fallback. Default, production and development category consumers, including TemporaryCoreDatabase, are migrated. |
| AC-3 | **Pass.** CategorySnapshot now contains category key only; removed values are not reconstructed elsewhere. CharacterDebuffState retains Intimidation and defense reduction, including named constructor migration. Existing command projections and Buffer transaction writer remain the only calculation/install ownership; no new persistent state, service, Job or storage layer. Mark/Pull state is not introduced. |
| AC-4 | **Pass for removal, observed rather than projected.** Previously changing/removing protection shape touched UnitData/SO/Spec/Definition/resolver, category snapshots/builders, capture/query/factory/group and ordinary/follow-up/reaction damage callers. Those metadata and capture dependencies now do not exist. Tuning Heavy movement/cost still follows CategoryConfig -> adapter -> registry; it does not require protection offsets or Frenzy internals. Shared damage/cost/status contracts still require their real Core and CoreIntegration regressions, which were run. Normal composition edits were retained where the removed service was registered. |
| AC-5 | **Not applicable to new traits in Phase A.** No Mark/Pull configuration or gameplay is implemented. Data-driven retirement controls compare identical target defense/held shield with and without another Heavy; no unit-specific replacement behavior exists. Real Shotgun/Cowboy authoring and unchanged DistanceDamageScale configuration pass. |
| AC-6 | **Pass within the requested verification scope.** Complete live source/content reference closure, mixed method/helper mappings above, service exception preservation, actual catalog deserialization, `.meta` pairing and moved GUID checks. Retired trait 120/event 30/category rule 1,4/activation 10,21 remain reserved. Framework execution limit and manual acceptance remain explicit. |

All applicable Phase A checks are closed; the global checklist in section 2.5 remains reusable for later phases. Public removals and consumer impacts are recorded in the [Core/View boundary handoff](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#2026-10-26--phase-a-retired-category-and-trait-contracts), locally only. Protected legacy Missing Script assets were retained. Only the obsolete Frenzy visual asset and its meta were deleted from asset content; active Cowboy managed-reference and visual-table registration were migrated together.

#### D. Decisions and remaining acceptance

- The explicit Phase A request authorizes exactly G-C's intermediate content. Cowboy is temporarily traitless; Shotgun retains DistanceDamageScale. This is a development checkpoint and not final new-trait release acceptance.
- The existing development fixture has obsolete main identities unrelated to this task. The new integration test uses its actual category specs plus production unit adaptation and valid current main/enemy fixtures; it establishes category assembly, not full development fixture playability. Repairing those identities is outside Phase A.
- Framework runtime and manual scene/visual acceptance are not claimed under the user's latest two-assembly scope. Earlier failing Framework baselines and obsolete Shotgun expectations were recorded rather than repaired or used to discard independent guarantees.
- The reviewer set aside Mark/Pull gameplay, old development fixture playability, Framework/manual acceptance, baseline problems and pending documentation/font cleanup. The first four follow the explicit phase and verification boundaries above; documentation and generated-font cleanup are completed here. No minor findings were deferred.
- No commits, pushes, external handoff messages, Mark/Pull implementation or Phase B–D execution. Stop here for user review. Future gameplay requires its respective G-M/G-P decisions and phase authorization.

### Phase B execution — 2026-10-26

The user authorized Phase B, then separately confirmed G-M exactly as design sections 6.2–6.3. Continue from the uncommitted Phase A checkpoint in the same supplied feature checkout; preserve all Phase A changes. G-P and Phase C/D remain outside scope. Final execution scope remains Core and CoreIntegration only, with actual card/asset checks placed in CoreIntegration where possible.

B1 entry: Authoring -> Data -> TraitSpecAdapter -> TraitRegistry/factories -> TraitEffectProvider/codec -> public status. A target's immutable CharacterDebuffState is the sole mark owner; existing copy/lifecycle/cleanup functions must preserve it until target death/battle disposal. Registration and type dispatch are construction responsibilities; the new rule owns damage/eligibility/non-stacking decisions.

B2 entry: AttackDirectDamageCalculator calls feature PrepareHit after normal damage and before held defense, then CompleteHit immediately after each actual shared Damage.Apply during health flush. The feature changes only command-local facts/events; existing Buffer expected-state validation and installation own publication.

B3 entry: Real Shotgun asset changes its base trait; visual metadata and existing card description use configured snapshots. Owned cards currently expose keys plus ThornFieldDescription, so the existing OwnedCharacterStatsSnapshot/calculator and UnitCardUI/DeckEditUI callers need the corresponding mark-description value connection. This is a required in-owner snapshot/consumer connection for B3, not permission to redesign cards or read InGame data from View.

Pre-implementation support audit: the existing CategoryMatchupDamageCalculator owns half-up percentage math, but its numerator is int. Approved nonnegative int percentages include int.MaxValue, whose 100+p needs a wider numerator. B1 will add a checked long-numerator overload to that existing helper, keep the int overload/API and rounding behavior, and reuse it from HuntersMarkTraitRule. No copied formula/new utility or change to ordinary damage semantics; full Core regressions remain required for this shared math dependency.
B2 caller audit before edits: two existing extra-hit owners calculate direct character damage without going through AttackDirectDamageCalculator: FollowUpAttackExecutor.ExecuteTarget (LuckyBullet and other target requests) and KillChainAttackCalculator.Resolve. G-M explicitly permits ordinary trait-generated follow-up hits. Connect the same feature-owned PrepareHit/CompleteHit functions before held defense and after actual Damage.Apply in those owners. A small overload taking their existing actor/profile/damage-kind/character-dictionary/event-list avoids a fake CommandAttackPlan or a second world; it preserves their current action/group/health/death/random ownership and also covers direct calculator callers where the optional parent/world are null. No copied executor or new gameplay decision. Add real KillChain/LuckyBullet tests as well as repeated-delivery and same-direction repeat regression.
B3 display-path audit: DeckEditUI receives DeckEditCardModel, not OwnedCharacterStatsSnapshot. Its existing DeckEditModelBuilder drops trait-description configuration while selecting baseline stats. Carry HuntersMarkDescription through that existing immutable display DTO and builder, then the card formatter; preserve the prior ThornField behavior in this separate deck-edit path rather than expanding Phase B into its unrelated omission. The new CoreIntegration authoring/card test exercises this actual model builder as well as the formatter. No screen lifecycle/input change or View read of InGame rules.
#### B.1 Verification and review result

The complete authorized gate ran through the approved AppData-access path with Unity **6000.0.73f1**, EditMode, assemblies **ProjectL.Tests.Core;ProjectL.Tests.CoreIntegration**. No Framework tests were executed in Phase B. The earlier Phase A result (3428 Core + 52 CoreIntegration) was the unchanged entry baseline.

| Evidence label under `.superpowers/sdd/category-and-unit-trait-rework/` | Actual result |
|---|---|
| `B1-authoring-red` | 0/5: Editor authoring discovery fails because the feature is absent. |
| `B1-authoring-green-corrected` | 5/5: actual authoring -> adapter -> battle preparation -> status/preview succeeds, including 0, 25, 150 and int.MaxValue configuration. |
| `B1-state-preservation-red` | 8/10: one intended independent-status preservation failure and one fall-fixture coordinate error. The latter was corrected before recording the next intended red. |
| `B1-rules-state-red-confirmed` | 9/28: 18 missing rule methods/decisions plus the independent-status preservation gap. Temporary API-discovery reflection was removed after implementation; final rule calls are direct. |
| `B1-green` | 77/77 with no skips, exit 0. |
| `B2-full-red-confirmed` | 10/24: 14 expected failures for missing apply/consume/damage/publication hooks, including actual KillChain and LuckyBullet consumption. |
| `B2-green` | 115/115: Mark, real publication/authoring, physical/held defense, reflection, KillChain and follow-up regressions; exit 0. |
| `B3-content-red` | 3/9: six expected failures for old Shotgun trait, absent visual/configured card descriptions and missing owned configuration. |
| `B3-deck-path-red` | 7/9: two actual authoring values reach the Core snapshot but the existing deck model builder omits them; fixed by passing that same snapshot through the existing DTO. |
| **`B-core-integration-full-corrected`** | **Core 3475/3475; CoreIntegration 71/71; total 3546/3546; zero failures, skips or inconclusive cases; Unity exit 0.** |

Final command: `& '.superpowers/sdd/category-and-unit-trait-rework/run-tests.ps1' -Label 'B-core-integration-full-corrected' -Assemblies 'ProjectL.Tests.Core;ProjectL.Tests.CoreIntegration'`. Each label retains its full XML/log. Earlier attempts that stopped for duplicate insertion, missing new-test imports or fixture setup errors are retained but are not counted as behavioral red or successful verification.

Phase B adds **47 Core cases** and **19 net CoreIntegration cases**. Every existing Core case identity remains; the old CoreIntegration Shotgun test was renamed from `ProductionShotgun_PreservesRemainingStatsAndDistanceTrait` to `ProductionShotgun_PreservesRemainingStatsAndMarkTrait`, retaining all non-trait stats/profile assertions and replacing only the deliberately migrated base-trait expectation. This is the only removed old case identity; no independent guarantee was deleted. Exact case deltas are retained with the evidence.

One fresh-context read-only review found **no Critical, Important or Minor findings** and judged Phase B ready for this checkpoint. It independently inspected the final source, connections, tests and retained XML/log; it did not rerun Unity. No fix pass or second review was needed. Structural audit found no missing/orphan metas, new GUID collisions, live retired feature references or whitespace errors. New C# files use UTF-8/LF and paired fresh metas; the new visual GUID is unique. Generated font/FMOD changes are excluded from delivery after checking the working tree.

#### B.2 Actual boundary and locality evidence

| Check | Actual connection and result |
|---|---|
| AC-1 — rule placement | **Pass.** `HuntersMarkTraitRule.EvaluateHit`, `CanApplyMark`, `ApplyMark` own damage kind/hostility/source eligibility, percentage and non-stacking decisions. `HuntersMarkApplication` supplies facts/configuration, consumes the decision and installs local facts/events. New shared-method branches only inspect the existing `DeliveredAttackHit.AppliesDamage` fact or coordinate the returned result; no percentage formula, unit-key selector or mark predicate is added there. |
| AC-2 — complete path | **Pass.** `HuntersMarkTraitAuthoring -> HuntersMarkTraitData -> TraitSpecAdapter.Read -> TraitRegistry/factories -> definition/runtime marker -> TraitEffectProvider/codec/status -> CombatCommandEffects`. Ordinary execution, preview and `EnemyAiPolicy.TrySelectAttackAim` use the existing attack calculator. `AttackDirectDamageCalculator` invokes PrepareHit before held defense and health queue installation, and CompleteHit immediately after actual Damage.Apply. Existing target follow-ups and KillChain invoke those same feature functions at the same boundaries. The existing BattleWriteSink validates/installs the transition; existing read sinks expose the resulting facts. |
| AC-3 — SSOT/state ownership | **Pass.** The sole victim slot is `CharacterDebuffState.HuntersMark`, an immutable `(SourceId, SourceOwner, BonusDamagePercent)` value. The owner runtime marker holds no victims. Configuration is authored once, passed through the normal definition path, and captured on application; source removal requires no lookup. Other status writers preserve the slot. Existing cleanup clears it on victim death/end; no second lifecycle or transaction owner. Owned/card snapshots contain trait configuration only, never a victim's active combat mark. |
| AC-4 — concrete change scope | **Pass.** Tuning 10 -> 25 changes the authoring entry and flows through the same adapter/registry/spec path; the actual different-ally test changes damage 20 -> 22 versus 25 and card text accordingly, with no edit to shared attack code, another trait or View math. Changing consumption eligibility belongs in EvaluateHit/CanApplyMark and its tests; direct/extra hit ordering and existing publication/preview integration remain real regression obligations. Config display changes follow the owned/combat snapshot -> existing card/model formatter path; no InGame lookup from View. |
| AC-5 — data driven | **Pass.** `EquivalentTraitOnDifferentUnitKey_PreservesMarkBehavior` uses valid Akimbo/Shotgun synthetic definitions; `SameSyntheticUnitWithoutTrait_DoesNotApplyMark` removes it from the same definition. `ConfiguredPercentage_ControlsActualAlliedDamage` converts real authoring values 10/25, applies through a valid battle entry and first command, then consumes with another unmarked ally through the real service. Card tests edit an independent actual Shotgun SO copy with SerializedObject and verify Core-owned, combat and deck-model configuration. No Cowboy/Shotgun runtime identity shortcut. |
| AC-6 — preserved behavior/content | **Pass.** Remaining Shotgun stats/targeting/category are unchanged; DistanceDamageScale code and visual remain available. New trait190/event39 do not reuse retired IDs. Mark tests cover queued repeated hits, consume-before-reapply, own-source non-refresh, zero/shield damage, zero-percent splash exclusion, lethal/structure exclusion, nonqualifying damage/reactions, actual KillChain/LuckyBullet, source death/absence, target lethal/fall cleanup, turns/night, preview/AI immutability, stale expected-state rejection, one publication/retry, calculation failure and battle disposal. Full required two-assembly gate is green. No Phase A preservation tests were removed. |

The shared half-up helper extension and additional existing follow-up/deck-model caller connections are recorded above with their actual owner and missing contract. They preserve approved semantics and connect existing responsibilities rather than adding a framework or relocating feature rules. The reviewer agreed with those boundaries. The global architecture checklist remains reusable for later phases; these rows close it for B1–B3.

#### B.3 Consumer handoff, decisions and remaining acceptance

- G-M is explicitly approved in design sections 6.2–6.3. The user authorized B1–B3 only. G-P, Pull and Phases C/D remain unexecuted; Cowboy stays in Phase A's temporary traitless state.
- Shotgun's one base trait is now HuntersMark at 10. HP35, attack7, physical defense5, priority30, range2, Heavy identity and existing targeting remain unchanged. DistanceDamageScale retains its implementation, registration and existing visual for other consumers.
- `HuntersMarkTraitStatusSnapshot.BonusDamagePercent` is the configured consumer value. `OwnedCharacterStatsSnapshot.HuntersMarkDescription` carries that value for owned cards, including the existing DeckEditCardModel/Builder route. Missing required Mark display configuration raises an error; no static 10% fallback. The separate pre-existing ThornField omission in the deck-edit model remains outside this phase.
- `CombatHuntersMarkEvent` (39) carries target, expected and final mark; null final means consumed. Existing `CombatEventRoutes.RouteOf` currently skips unknown event types with a warning and keeps the normal final refresh. Ordered Core facts/events are delivered, but in-world mark indicators, animation routing and final icon art remain D/art acceptance. The new visual metadata intentionally has no icon; no final art was fabricated.
- No Framework execution or manual scene/visual acceptance is claimed. The reviewer set aside later Pull behavior, D visuals/art/manual acceptance, the explicitly excluded Framework run, the preceding Phase A audit, and final documentation/font cleanup. These follow the authorized phase/test boundaries; Phase A's prior disposition remains in its checkpoint and full required Core regressions still passed. Documentation and generated-file checks are completed here. No minor findings were deferred.
- The [Core/View boundary handoff](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#2026-10-26--phase-b-hunters-mark-contracts) records the new contracts locally. No external handoff message, commit, push, Phase C or Phase D work was performed. **Stop at the Phase B review checkpoint.**

### Phase C authorization and revised obstacle boundary — 2026-10-26

The user confirmed G-P rules 1–3 and explicitly excluded obstacles from Pull. Obstacles and other structures retain existing attack behavior within the effective basic attack range A; the Pull extension P does not enable attacks against them. No obstacle displacement/footprint extension is implemented for Pull. Existing knockback behavior is preserved. This approved decision supersedes every earlier obstacle-Pull proposal in this plan and the spec. Phase C (C1–C4) is authorized, followed by a stop; Phase D is not authorized. Run only ProjectL.Tests.Core and ProjectL.Tests.CoreIntegration, including real asset checks in CoreIntegration. No Framework tests, commit or push.


### Phase C execution checkpoint — 2026-10-26

C1–C4 implementation, the independent review fix pass and the required two-assembly verification are complete. Stop at this Phase C checkpoint. The latest user instruction explicitly approves G-P1–3 and excludes obstacles/structures from Pull. Only Phase C was authorized. Phase D, external messages, commits and pushes remain unexecuted.

#### C.1 Verification and preserved tests

- Approved Unity 6000.0.73f1 batch EditMode, full `ProjectL.Tests.Core;ProjectL.Tests.CoreIntegration`: **3534/3534 Core + 85/85 CoreIntegration = 3619/3619**, zero failures/skips/inconclusive, exit 0. Evidence: `.superpowers/sdd/category-and-unit-trait-rework/C-final-verified.xml` and matching log. Command: `& .superpowers/sdd/category-and-unit-trait-rework/run-tests.ps1 -Label C-final-verified -Assemblies 'ProjectL.Tests.Core;ProjectL.Tests.CoreIntegration'`.
- Registration red: 15 expected missing authoring/status failures and one no-trait control; initial registration/range/selection regression green: 234/234. Execution red exposed absent movement, wrong arrival damage and friendly direct hits. The shared movement regression initially passed 281/283; two new ally fixtures omitted owned soul-count entries and were corrected at their input, without a production fallback. Result/preview red exposed absent Pull facts, missing movement-only target previews and missing pre-hit mine damage; subsequent Pull/Mark combined run passed 115/115.
- The first full run passed 3609/3610; the real Cowboy SO test found a mistaken targeting GUID. The actual Shielder meta GUID was then read and applied; the complete final gate above passed. Initial compile-only test typos, invalid deployment fixtures and the incorrect expectation that an already-handled command should throw are retained as diagnostic evidence, not counted as intended feature failures. Existing retry behavior is an idempotent no-op.
- Relative to Phase B, this phase adds **59 Core and 14 CoreIntegration cases**. No previous case identities or files are removed. `CowboyDefinition_NoFrenzyAndCatalogStillBuilds` keeps all category/stat/catalog assertions and replaces only Phase A's explicitly temporary traitless expectation with the final Pull trait. The other original 3546 cases remain in the full run.
- Tests cover four cardinal rays and A3/P2 distances1–6, P0, default1/1, live Sandstorm range changes, alternate valid unit keys, absent traits, invalid configuration, blocked paths, explicit immunity versus Heavy, friendly movement/shields/mark/action state, actual path/event ordering, arrival defenses, thorns/mines/cliff, target/main/attacker death, real stale-position/mark rejection with all stores unchanged, idempotent publication, hidden-mine preview, and real production/visual assets.
- Source and asset snapshots at `C-start/` plus `C-start-manifest.json` establish the uncommitted A/B baseline. `C-phase.diff` attributes only Phase C. Final structural audit found all 26 new C# files paired with metas, no new GUID collisions/orphans, no U+FFFD or CRLF in new C#; whitespace checks passed. Framework tests and manual scene/art/playback acceptance were not run.

#### C.2 Shared mechanics, connection boundaries and actual locality

| Check | Actual evidence | Result |
|---|---|---|
| AC-1 — rule placement | `PullTraitRule.SelectionRange/Destination/DealsAttackDamage/ValidateMethod/TargetingPolicy` own tuning derivation, geometry, damage eligibility and supported targeting. `PullAttackCalculator` validates the configuration binding, preflights the selected unit, and coordinates movement/pinned delivery. Shared query/executor branches consume evaluated outcomes; no Cowboy key/name or A+P/destination formula is embedded there. | Pass after the reviewed boundary fixes and full regression. |
| AC-2 — one path | Authoring -> Data -> `TraitSpecAdapter` -> registry/factories -> marker/codec/status -> `CombatCommandEffects` and registered targeting capability. Query/UnitRange/AI share the same selection range. `AttackSelectionEvaluation.Pull` passes the decision to the existing attack command; the same world and single arrival projection flow through shared movement, rebuilt delivery, existing settlement and BattleWriteSink. Real CoreIntegration publication verifies this route. | Pass after the reviewed boundary fixes and full regression. |
| AC-3 — SSOT | P is authored once; A is the current effective basic range; total reach is derived. Pull's runtime state is a stateless registration marker. All changes use the existing character/world/environment dictionaries and transaction. Path/result records are immutable command facts, not another battle store. The existing damage application boundary tracks wide applied-damage/recovery totals for the command ledger; checked result conversion does not constrain unrelated movement. | Pass after the reviewed boundary fixes and full regression. |
| AC-4 — locality | Editing P changes only authoring; the configured1/2 test changes actual preview/admission/movement through that path. Shared query/executor/other traits need no tuning edits. Destination policy changes belong in PullTraitRule and its tests. A shared forced-terrain change legitimately requires Pull, knockback and environment regressions; that coupling remains explicit. Display changes consume the published status/range/path fields in D. | Pass after the reviewed boundary fixes and full regression. |
| AC-5 — data driven | Equivalent Pull on Akimbo and Shotgun synthetic keys produces the same range and actual movement; removing Pull restores base reach. Real authoring values round-trip through battle preparation, effect/status capture and commands. No runtime unit key/name selector was added. | Pass after the reviewed boundary fixes and full regression. |
| AC-6 — preserved behavior | All prior tests remain; no retired feature is restored. The only content expectation migrated is temporary traitless Cowboy. Obstacles receive ordinary inner-range attacks, reject extension-only attacks and retain occupancy, including a multi-cell obstacle through real publication. All shared Core/CoreIntegration regressions pass. | Pass after the reviewed boundary fixes and full regression. |

The bounded extraction is `AttackForcedMovement.CanEnterUnit/ExecuteUnit`. `AttackKnockbackCalculator.Resolve` calls it with its existing direction/immunity policy, generic Moved events and delayed final-object processing; its obstacle movement body is unchanged. `PullAttackCalculator.Evaluate` reuses the entry predicate, and Execute calls the same unit traversal before damage with immediate final-object processing and one dedicated Pull event. Per-cell cliff/weak-ground, thorns, held defense/statuses and existing object arrival remain in their existing owners. No copied Cowboy displacement engine was added.

Additional bounded connections were needed within existing owners: a character-only friendly-target permission in `AttackTargetingPolicy` preserves ordinary structure eligibility; `CommandAttackPlan.RequestedCells` preserves original selection separately from rebuilt arrival delivery; `CommandDeathSettlement.DeferUntilCompletion` preserves direct-hit death timing when the arrival projection is created early; and `CombatDamageApplication.CaptureAppliedDamage` supplies pre-hit environmental damage to the existing ledger without scanning events or confusing falls/healing with damage. Existing `MovementProjection` records actual object/soul recovery and returns actual arrival normalization; the ledger accumulates departure/arrival normalization and `AttackTargetPreview` accepts the corresponding recovery when validating final health. Movement-only previews use `IsDirectHit=false`, with no fake `AttackResult.Hits` entry. No Buffer, Job, service registration, Framework, assembly definition or View input/playback implementation changed.

Current production enhancement content has no Cowboy recipe. DoubleShot requires Direction aim while Pull requires Single, so this phase does not fabricate a new compatible-combination policy or a new enhancement framework. Existing enhancement/follow-up regressions remain in the full Core gate.

#### C.3 Authored content and consumer handoff

- Cowboy key19 remains Rider, HP25, attack10, defense5, priority15, range1 and one-cell footprint. Its sole trait is Pull with P1. It reuses the unchanged `TargetingMethod_Shielder` key6, GUID `b392af0b31d746a1ae14c1795a406e7a`: CardinalLine/Single/Unrestricted/None. No new targeting asset or table row was needed. The original radial method and its four other referring SOs are unchanged. Generic catalogs consume the authored definition; the inspected temporary/default dev fixtures do not represent Cowboy and were not expanded.
- Pull visual key200 is registered with descriptive text and no final icon. Numeric card formatting, range styling, ordered Pull animation and final art/manual acceptance remain Phase D. This is a Core/content checkpoint, not completed View integration.
- `AttackRangeResult.BasicAttackCells` exposes A's cells; `RangeCells` exposes total selectable reach. Existing non-Pull callers default basic cells to range cells. `AttackAims` remains authoritative for actual legal selections.
- `AttackResult.Pull` and `AttackPreview.Pull` are optional immutable `AttackPullSnapshot` values with TargetId, StartAnchor, FinalAnchor and copied Path. Null means no Pull movement. The path records actual entered cells and stops at a lethal hazard. Original selected and occupied cells remain original; actual delivery/affected cells use arrival positions.
- `CombatPullEvent` kind40 precedes movement hazard and direct attack/hit events. Generic Moved events are not duplicated for the same Pull. Obstacles never produce this event/snapshot. Friendly movement and terrain-terminal outcomes can have an accepted attack result with zero direct Hits; target previews still expose identity, final health and movement. `IsDirectHit=false` identifies these movement-only/environment-only preview entries; SecondaryDamage includes pre-hit environmental damage.
- New normal rejection reasons: `PullPathBlocked=40`, `PullImmune=41`, `PullNoMovement=42`. Required malformed configuration still throws rather than falling back to ordinary attack. Existing publication rejects stale target position/mark before any stores change, and ignores an already-handled retry without publishing twice.

No external message, commit, push or Phase D implementation was performed. The review result and fix evidence below close this checkpoint.


#### C.4 Independent review and final fix pass

One fresh-context, read-only review found **three Important findings**, no Critical or Minor findings. It inspected actual source, the phase-only baseline/diff and saved XML/log, and independently confirmed that all Phase B case identities remained. It did not execute Unity or modify files. The implementer accepted all three findings and completed one fix pass, with no second reviewer:

1. **Pre-hit non-damage HP accounting:** destination max-HP reduction was missing from normalization, and the supported Object healing extension was missing from recovery. Existing movement/damage owners now record actual recovery and return actual arrival normalization; normalization accumulates across boundaries. Hostile/friendly destination reduction, departure+arrival reduction, and Object recovery with/without destination reduction are covered. The synthetic healing Object exercises an existing extension contract; no production healing content was added.
2. **Unrelated movement overflow:** a new checked int damage total could abort an ordinary move through valid mines dealing1 then int.MaxValue. Damage/recovery totals now use long internally; only the consuming int result contract performs checked conversion. Requested damage is not silently clamped. The ordinary move completes with the existing lethal damage behavior.
3. **Pull/piercing permission leak:** the existing profile-combination validation now rejects Pull + PiercingFalloff before selection/delivery. This prevents a Pull-only friendly selection permission from entering piercing's damage candidate list. **Ruling:** no hybrid gameplay was approved or needed for production Cowboy; inventing it would expand this phase. The tradeoff is that designers requesting this hybrid must define it in a later design, rather than receive silent friendly damage. Ordinary Ballista/piercing remains unchanged.

All **7 new regression cases failed for the intended defects** in C-review-fixes-red-confirmed, then passed in the final complete run **Core3534/3534 + CoreIntegration85/85 = 3619/3619**, exit0, zero failures/skips/inconclusive. The first reproduction used an invalid piercing reduction0 and was corrected to valid30 before the confirmed red run; that setup error is not behavioral evidence. C-case-deltas.json confirms 3546 prior identities retained, 73 added, none removed. The final structural audit covers26 new C# files with paired unique metas and no encoding defects. No generated font/FMOD differences from the Phase C starting snapshot remain.

Reviewer exclusions were explicitly resolved: prior A/B retirement completeness is not re-audited in this phase (its evidence remains, and every prior test still passes); D playback/formatting/icons/UI/manual acceptance stays deferred; Framework execution stays excluded by the user; pending documentation is completed in this checkpoint. The potential IsDirectHit value on thorn-only ledger roles has no live caller because current ThornField application is disabled; no unrelated deferred feature was re-enabled or claimed as validated. Arbitrary trait-combination gameplay is not invented; the concrete unsafe Pull/piercing case is rejected as above. These limitations do not substitute for the required current Core/CoreIntegration gate, which passed. No deferred Minor findings.

A final category-policy check added two more Core cases: an A1 Cannon-profile Pull cannot land an enemy in the existing forbidden adjacent direct-hit cells, while A2 remains valid. The first case failed admission as expected and the valid control passed in C-arrival-policy-red. Pull preflight now consumes the already-composed selection policy and existing exclusion calculator before mutation; it introduces no new category rule and keeps friendly movement-only outcomes separate. The final C-final-verified gate above includes both cases. Final delta:73 new cases, no removed identities.


### Phase D execution checkpoint — 2026-10-26

Authorization: Phase D, tests only Core and CoreIntegration. Existing feature checkout with uncommitted A/B/C prerequisites retained; no commit/push. D-start snapshots in the plan workspace attribute this phase independently of HEAD.

Boundary before implementation: C already supplies actual Pull paths, BasicAttackCells/RangeCells, mark events and configured trait snapshots. Missing consumers were Event routing/handler, motion-lane playback, shown-fact mark projection/HUD status, and dynamic Pull card text. Owned-card Pull configuration was absent, so the existing owned-stat descriptor path was extended alongside the existing mark descriptor; no gameplay or storage responsibility moved. Basic/extension/destination highlighting uses existing Field state/renderer/palette.

Actual changes and signatures are recorded in [the Core/View handoff](../handoffs/2026-10-26-category-unit-trait-core-view-handoff.md). `OrderedCombatPresentation` and `CombatSceneEventPlayback` need no replacement: an ordinary ungrouped Pull event uses their existing event barrier and lifecycle. One small Pull handler was added to the existing handler directory; no feature bootstrap/service or synthetic Moved-event graph.

Verification: D-behavior-red observed8/9 intended failures; D-bindings-green9/9. D-range-red-confirmed failed only the missing range contract (10/11); D-range-green11/11. Enhanced owned-projection cases then joined the full run. `D-core-integration-pre-review.xml/.log`: exit0, Core3534/3534 + CoreIntegration98/98 =3632/3632, no skips/failures. D-case-deltas.json confirms13 added and0 removed identities versus C-final-verified. D structural audit:2 new C# files, paired unique metas, no errors. `git diff --check` passed. No prior test deletion or retirement in D.

Scope limits: Framework execution/DataBaseAssetTests are excluded by the user. Real production asset authoring checks in CoreIntegration passed; D changes no assets. Manual scenes and final art remain unverified; automated GameObject/HUD/motion checks do not certify final scene readability/composition or visual timing. Fresh review and final AC closure are recorded below when available.


#### Phase D review resolution and architecture closure

One fresh-context read-only review found3 Important issues and no Critical/Minor issues. All3 were accepted and fixed in one author pass; no second independent review is claimed. Historical departure/intermediate health snapshots no longer rewind completed Pull placement; movement-only Pull no longer triggers direct attack animation/fire/sound; actual supplied arrival cells now drive trajectory, facing, code/clip shots and contact endpoints without changing the original aim contract.

`D-review-all-green.xml/.log`:20/20 focused cases passed, including nonlethal/lethal emitted Core terrain events, normalization plus subsequent movement, actor-backed motion controls and actual linear/arc projectile endpoints. One initial mortar fixture lacked its required weapon binding; it was corrected rather than weakening production validation. `D-final-verified.xml/.log`:Core3534/3534 + CoreIntegration105/105 =3639/3639, Unity exit0, no skips/failures. Final same-result movement check and case-identity audit are recorded in the final verification line below.

| Check | Actual D evidence and limits |
|---|---|
| AC-1 | New branches perform event/lifetime routing, supplied-cell partitioning, snapshot validation and text formatting. Gameplay geometry, damage, cost, eligibility and mark rules remain in A–C owners. Existing AttackTarget owns the difference between original aim and provided delivery endpoint. |
| AC-2 | Traced real Core results through registered Pull/mark routes, existing Scene playback, motion lane, shown facts and HUD. Traced Pull definition through owned-stat calculator, descriptor, deck/card model and shared formatter. Original PrimaryCell remains intact. Actual source-shot endpoints use the same delivery-cell resolver as preview. |
| AC-3 | One Core mark slot/configuration/transaction owner. Existing shown facts own display position; the per-result Pull-arrival marker stores IDs only, prevents historical health rewind, and clears on actual move/relocation/field exit/result start/cancel/refresh. No new gameplay or position store. |
| AC-4 | Percentage/extension tuning propagates via supplied descriptors without editing View or the other trait. Changing HUD layout needs existing HUD/field ownership, not Core rule internals. Motion changes still require playback/placement/cleanup checks; shared terrain/publication regressions remain required. Future edit-effort reduction is predicted, not measured. |
| AC-5 | Actual owned projection and combat cards checked atP=0/2/7; mark text consumes27%. Prior A–C alternate-key/removal/10-to25/A-P authoring tests are retained in the full passing suites. Production View has no unit-ID/name gameplay dispatch or independent A+P/bonus computation. |
| AC-6 | D removes no tests/behavior. All prior3619 Core/CoreIntegration identities remain;20 new cases added. Four Framework fake signatures preserve old calls while accepting optional supplied cells/Pull. Framework runtime and final manual/art acceptance remain unverified, not bypassed or declared green. |

Declined review scopes: multi-cell Pull is excluded by design; prior A–C retirement review remains at its verified checkpoints; the pre-existing DeckEdit ThornField descriptor omission remains separate; final icons/VFX/readability/scenes and Framework execution are not certified. No deferred Minor findings and no unrelated fixes. Core owned-read changes are recorded in the existing View/Core boundary handoff.

Final verification: `D-final-acceptance-verified.xml/.log`, Unity exit0, Core3534/3534 and CoreIntegration105/105, total3639/3639, no failed/skipped/inconclusive cases. The same-result historical-health -> forced-move control passed, so arrival protection does not rely on a new result clearing it. This is the final code checkpoint; manual scenes/art remain unchecked. No commits or pushes.
