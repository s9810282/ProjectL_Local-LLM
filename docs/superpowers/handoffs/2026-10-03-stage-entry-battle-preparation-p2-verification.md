# Stage Entry and Battle Preparation: P2 Verification

Date: 2026-10-03.

Scope: P2A and P2B of the [implementation plan](../plans/2026-10-03-stage-entry-battle-preparation-implementation-plan.md), following the [approved design](../specs/2026-10-03-stage-entry-battle-preparation-contract-design.md) and [P1 publication contract](2026-10-03-stage-entry-battle-preparation-p1-verification.md). Implemented in the existing `feature/Cheon/refactor/core-system` checkout, based on `a9db1e5a24f7b6431b36fc2da39807943abaa83c`. P1 and P2 remain uncommitted. No push or production caller migration was performed.

## Delivered contract

```csharp
ChapterProgressionResult EnterBattle(int chapterKey, StageNodeId targetNodeId);
void PrepareBattle();

BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey);
```

`EnterBattle` captures the current selection, performs live progression preflight, verifies the resolved DB root's stage/chapter identity and Combat type, reads the explicit target and calculates preparation from existing owned/main/soul facts. It then repeats progression validation, compares origin and target values and publishes through P1's two-argument battle writer. Only successful publication updates the service's definition cache. It does not separately confirm or select the stage.

Normal navigation rejection returns a rejected result before definition lookup or entry writes. A changed origin or changed accepted target returns `StageMismatch`; a second rejected preflight returns its rejection. Invalid arguments, missing dependencies, retained battle, malformed/noncombat data, source/calculation faults and publication invariant failures throw. `Accepted` means matching selection and prepared battle were published before return; it does not mean a Job completed or the lifecycle activated. Terminal battles remain retained until existing cleanup finishes.

Parameterless `PrepareBattle()` remains a compatibility operation for an already-committed Current selection. It requires map/selection before source lookup and does not perform fresh navigation or selection writes. Uncached definition reload also requires and passes committed identity; failed reads are not cached. Existing low-level command fixtures with deliberately absent sources retain their declared scope.

`DatabaseBattleInitializationSource` now uses the supplied identity and existing `BattleDefinitionAdapter`; only its existing Character read binding remains. It retains the T1/G7 Method_Key/query/unit-linkage rejection. Valid fixture data reaching that rejection proves explicit lookup and failure preservation, not actual DB readiness. No production fallback was introduced.

## Ownership and changed files

Production changes are limited to `CombatProcessingService.cs` and `DatabaseBattleInitializationSource.cs` under `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CombatSystem/`.

Core owns gameplay validation, explicit conversion orchestration and calculation. The existing Buffer group owns validated Current/Character/Combat publication; P2 does not modify P1's Buffer implementation. Current remains the selection owner. The service retains its existing static-definition cache, with no pending request, selection mirror or new runtime state owner. Pure calculators, service keys, factory construction/export, module binding and assembly structure are unchanged.

Tests under `Assets/Tests/EditMode/Framework/` changed as follows:

| File | Change |
| --- | --- |
| `BattleDefinitionAdapterTests.cs` | Explicit source identity/default-source gate, compatibility and successful-only cache reload; C2 new-entry helper migrated to `EnterBattle`. |
| `CoreStageCombatCycleTests.cs` | Real registered first/next entry, validation/failure/retry/retention tests and distinguishable definitions; C4 entry helper migrated. |
| `CharacterBattlePreparationTests.cs` | Source signature; committed selection seeded only for intentional compatibility fixtures. Existing Prepare Job assertions retained. |
| `ArtifactEditingServiceTests.cs`, `CombatCommandPublicationTests.cs`, `CombatCommonPublicationTests.cs`, `CombatPreviewPublicationTests.cs` | Mechanical source signature changes only. |
| `CombatPlanTests.cs` | `PreparationProbe.EnterBattle` throws `NotSupportedException`; old Job expectations unchanged. |

Production Combat Jobs, Plan, Flow, LifeCycle, Runner and registration are untouched. Existing Job compilation and compatibility execution do not close caller migration. Core does not submit/clear/acknowledge View input, initialize the roster/wallet/Artifact inventory, schedule work or activate Combat.

## Verification evidence

Evidence directory: `.superpowers/sdd/2026-10-03-stage-entry-battle-preparation-implementation-plan/`. Unity version: 6000.0.73f1.

| Run | Result | Exit | Meaning |
| --- | --- | --- | --- |
| `p2-baseline` | 1336/1341; five failures | 2 | Fresh incoming baseline after P1. |
| `p2-red-symbol` | Source signature compile failures | 1 | Initial missing-contract witness. |
| `p2-red-behavior` | NUnit collection constraint compile failure | 1 | Fixture syntax corrected; not behavioral RED evidence. |
| `p2-red-behavior-valid-tests` | 182/221; 39 failures | 2 | Valid compiling tests expose missing entry, committed selection guards and explicit lookup. |
| `p2-green` | 215/221; six failures | 2 | One missing-source setup and five distinct-stage deployment fixtures were invalid; not GREEN. |
| `p2-green-corrected-fixtures` | 221/221; zero skipped/inconclusive | 0 | Combined P2 filter passes all new and existing focused cases. |
| `p2-regression` | 1370/1375; zero skipped/inconclusive | 2 | 34 added cases, no existing case missing; unchanged five baseline failures. |

Full regression: Core 918/918, Framework 446/451, CombatViewBoundary 6/6. `P2/regression-summary.json` records case-set/assembly comparisons. `P2/regression-failures.json` confirms failure names, messages and stacks exactly match both fresh P2 baseline and P1 regression.

The unchanged failures are:

- `ProjectL.Tests.Framework.ArtifactDataTableTests.Catalog_RegistersOnlyMigratedRulesAndExplicitlyPendingViews`
- `ProjectL.Tests.Framework.DataBaseAssetTests.Catalog_CreatesAllDataBaseGroups`
- `ProjectL.Tests.Framework.DataBaseAssetTests.CombatStages_ReferenceExistingInGameData`
- `ProjectL.Tests.Framework.DataBaseAssetTests.EveryStage_HasNodeVisual`
- `ProjectL.Tests.Framework.DataBaseAssetTests.EveryUnitAndStructure_HasUnitVisual`

`run-tests.ps1` and each run's `.args.txt`, `.xml`, `.log` and `.exit-code.txt` retain exact commands/results. Focused execution selects the eleven P2 fixtures listed in the plan; regression selects all three assemblies. Compilation and zero-discovery runs are never treated as passing evidence.

Registered tests use real Core/Buffer factories, `FrameworkRuntime.InitData/PostBindService`, progression, calculators and stores. Local DB/source doubles supply graph definitions and supported profiles. They cover first entry without selection, next entry with old selection, rejected navigation, injected noncombat/wrong identity, source/null/placement faults and correction/retry, origin/graph changes, retained active/terminal battle and missing dependencies. Distinct stages use width 8/cost 2 versus width 10/cost 5, height 6/deployment depths 2/2, and RadialSquare versus CardinalLine. Actual command/preview results distinguish the profiles; real victory and cleanup precede next entry. Persistent HP/souls/roster/Artifact/wallet 91 and result counters are checked, and old View commands survive entry.

Fixture correction preserved the original unselected missing-source failure witness. Distinct-stage HQ placement moved from y=3 to legal enemy-zone y=4, with the command/preview path moved accordingly; the historical height-5 C4 definition and assertions remain intact. Source-induced mutations are compared against the deliberately mutated state rather than rolled back. No production interleaving hook was added.

## Review, preservation and remaining gates

A fresh read-only P2 reviewer inspected the scoped diff, approved documents, collaborating production code, factory composition and recorded test evidence. P2 was approved with no Critical, Important or newly introduced Minor finding. P1's deferred nonzero-preview evidence remains explicitly assigned to P3; this review does not close whole-package acceptance or any external gate.

SHA256 checks confirm all 17 captured P1/production execution files remain unchanged. Unity altered only the FMOD catalog's `lastScanTime`; the saved incoming bytes were restored after scan-only and concurrent-change guards, with matching incoming/final SHA256. `P2/preservation-check.json` and `P2/fmod-preservation.json` retain this evidence. The incoming FMOD modification and unrelated attachment directory remain preserved. Scoped whitespace, documentation-link and interface-inventory checks passed; HEAD and branch are unchanged.

| Condition | Status |
| --- | --- |
| P1 Buffer publication capability | Complete |
| P2 explicit Core entry/source and fixture migration | Complete |
| P3 final Core package verification/handoff | Pending; includes nonzero preview lifetime evidence and whole-package review |
| C: Core and Buffer implementation complete | Pending until P3 closes |
| V: View/Flow caller migration complete | Pending; target transport and one-time lifecycle activation require the external execution owner |
| D: Authored DB readiness | Pending; default Method_Key gate and known Economy asset failures remain |
| A: Production activation | Pending |

P2's supporting full regression does not complete P3's remaining evidence and final handoff. The serialized synchronous publication model remains unchanged; concurrent/reentrant writers, process failure and out-of-memory recovery are outside the guarantee.
