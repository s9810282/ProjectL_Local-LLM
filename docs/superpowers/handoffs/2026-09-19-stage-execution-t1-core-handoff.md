# T1 Stage Execution Classification — Core Handoff

> 과거 기록 (2026-10-06 정리): 아래 내용은 Framework/Buffer 전환 전 구현이며 현재 구현 지침이 아닙니다. 삭제된 계획·설계 원문은 Git 커밋 `606fdf4c`에서 확인할 수 있습니다. 현재 구조와 API는 [Framework 개발 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md)와 실제 코드를 기준으로 확인합니다.

Date: 2026-09-19

Status: the independent Core portion is implemented and its focused NUnit checks pass. **T1 as a whole is incomplete:** D1/D2 block the Unity input migration and Unity acceptance. T2 has not started.

Source: Main Character Recovery Stage Implementation Plan (Git 이력: `2026-09-19-main-character-recovery-stage-implementation-plan.md`), T1 and V1/V2. Baseline: `86e25e6`.

## Implemented behavior

- Added `StageExecutionKind { Combat = 0, NonCombat = 1 }` and immutable `StageContentRef`. Empty keys and undefined kinds are rejected; generation rejects null candidate references.
- Replaced the generator and session factory input with `IReadOnlyList<StageContentRef>`. There is no old key-list overload or classification fallback.
- Preserved the width/candidate/edge random calls, candidate order and duplicate weights, graph repair, node IDs, and connection sorting. Each draw reads Key and ExecutionKind from the same selected reference.
- Carried the kind through the immutable Domain node, selection plan, validation, and snapshot. Planning/validation remain read-only; commit remains a current-node assignment. Both existing `TrySelectNode` APIs remain.
- Generated start and boss nodes explicitly store Combat. The start remains non-playable through its empty key and existing selection rules. Directly constructed NonCombat final nodes and mismatched-kind selection plans are rejected.
- Migrated independent test callers, including both reflected snapshot constructors. Existing Combat fixtures retain explicit Combat inputs.

## Changed files

Core paths below are relative to `Assets/Scripts/ProjectL/Core/`.

| File | Change |
|---|---|
| `Contracts/Common/StageExecutionKind.cs` | New enum; Unity-generated `.meta` included. |
| `Contracts/Commands/StageContentRef.cs` | New validated immutable input; Unity-generated `.meta` included. |
| `Contracts/Snapshots/StageMapNodeSnapshot.cs` | Required execution metadata. |
| `Domain/StageMap/StageMapGenerator.cs` | Ref-list input and metadata forwarding without generation-policy changes. |
| `Domain/StageMap/StageMapNode.cs` | Required immutable execution metadata. |
| `Domain/StageMap/StageMapSelectionPlan.cs` | Required target execution metadata. |
| `Domain/StageMap/StageMapRunState.cs` | Selection propagation/validation and Combat-only final-node invariant. |
| `Application/StageMap/StageMapSessionFactory.cs` | New input contract. |
| `Application/StageMap/StageMapSnapshotBuilder.cs` | Snapshot projection. |

Test paths below are relative to `Assets/Tests/EditMode/`.

| File | Change |
|---|---|
| `StageMapGeneratorTests.cs` | Migrated inputs and invalid-input group; five fixed pre-migration signatures. Existing graph/seed regressions retained. |
| `StageMapRunStateTests.cs` | Explicit Combat constructor arguments; mismatched-kind plan and NonCombat final-node rejection. |
| `StageMapSessionTests.cs` | Migrated weighted inputs; reused selection-boundary test for Combat and NonCombat propagation in a mixed map. |
| `MainCharacterRunProgressionTests.cs` | Existing Run fixture uses explicit Combat refs. |
| `GameFlowContractTests.cs` | Existing snapshot fixture supplies Combat metadata. |
| `CombatViewBoundary/StageMapViewBoundaryTests.cs` | Both factory paths migrated, preserving duplicates and stale-snapshot coverage. |
| `StageComposition/StageMapDataLayoutTests.cs` | Reflected snapshot constructor migrated; catalog assertions remain pending D2. |
| `StageComposition/StageMapHighlightTests.cs` | Additional reflected snapshot constructor found during the actual caller audit. |

This handoff is the only added implementation document. The supplied plan/design remain unchanged. No authored assets, scenes, Recovery implementation, or T2–T6 code were changed.

## Caller audit and remaining prerequisites

All generator calls, factory calls, direct node/plan/snapshot constructors, and reflected snapshot constructors under `Assets` were inspected. The independent callers above use the new signatures. The remaining old map input path is explicitly blocked:

| Boundary | Pending work |
|---|---|
| D2: `Unity/Data/ScriptableObjects/Stage/StageDefinitionSO.cs` | Still has no `ExecutionKind` producer. Its declaration was not silently pulled into T1. |
| `Unity/Data/Runtime/StageMapCatalog.cs` | Still exposes `RegularStageKeys`. After D2, freeze each definition's Key/kind into `RegularStages`, preserve duplicate candidates/copies/lookup validation, and reject a NonCombat boss definition. |
| `Unity/Runtime/Run/RunSessionHost.cs:71` | Still forwards the old catalog list. This now fails compilation against the changed factory; change to `catalog.RegularStages` with the catalog migration. |
| `Unity/Setup/StageSelect/StageSelectSceneInstaller.cs:118` | Still iterates old catalog keys. Migrate to each ref's Key with the catalog. |
| `StageComposition/StageMapDataLayoutTests.cs`, `StageComposition/StageDatabaseAssetTests.cs` | Catalog input/copy/asset assertions still await D2. No invented Combat producer was used to make them pass. |
| D1: `Unity/Runtime/Run/RunSessionHost.cs:83` | Existing Run construction omits required Deck/catalog inputs. Unity reports missing `deckSpec`. The other Deck/Gold handoff consumers remain separate prerequisite work. |

`StageMapDefinitionSO.BuildCatalog(int)` already passes definitions and needs no parallel input. The separate `StageStartCatalog.ValidateRequiredKeys` list is a key-lookup validation API, not a StageMap generation caller, and remains unchanged.

## Executed verification

Before editing the generator, captured five `CreateSignature`-equivalent outputs from the baseline source: seed 31; depth two with no regular candidates; weighted duplicates at seed 7301; and connection chances 0 and 1. Those exact outputs are now fixed test expectations, including ordered IDs, keys, depths, widths, and edges.

An ignored local runner compiled Contracts, Domain, and Application as **separate netstandard2.1 assemblies**, preserving their existing assembly names and internal-access boundaries. It executed the existing NUnit test methods under .NET 9 using the installed Unity NUnit library. View-boundary tests were compiled separately with Contracts/Application references.

| Suite | Passed | Failed |
|---|---:|---:|
| `StageMapGeneratorTests` | 12 | 0 |
| `StageMapRunStateTests` | 12 | 0 |
| `StageMapSessionTests` | 5 | 0 |
| `MainCharacterRunProgressionTests` | 30 | 0 |
| `GameFlowContractTests` | 9 | 0 |
| `CombatViewBoundary.StageMapViewBoundaryTests` | 2 | 0 |
| **Total** | **70** | **0** |

No tests were skipped by this runner. Local artifacts are in `.utmp/T1Verification/`: `ProjectL.Tests.Core.xml`, `ProjectL.Tests.CombatViewBoundary.xml`, `core-test-output.txt`, and the temporary runner projects. These files are ignored and are not a new repository test framework. This execution is **not** a Unity EditMode pass.

A Unity 6000.0.73f1 batch EditMode run was attempted for those six groups plus `StageMapDataLayoutTests` and `StageDatabaseAssetTests`. Compilation stopped before any NUnit report was produced:

- `RunSessionHost.cs:71`: CS1503, old `IReadOnlyList<StageKey>` cannot be passed as `IReadOnlyList<StageContentRef>` (the D2-blocked conversion).
- `RunSessionHost.cs:83`: CS7036, missing `deckSpec` (existing D1 prerequisite).

The Unity log reports compiler errors and termination with return code 1; the launcher wait was cancelled after that failure. A copy is retained at `.utmp/T1Verification/unity-20260919-153404.log`. Unity imported both new scripts and included them in the Contracts compiler input. Unity/catalog/layout/highlight behavior remains unverified. The additional highlight constructor fix was reviewed after this blocked run; no redundant Unity retry was made while the blockers remained.

`git diff --check` passed. The final caller audit found no additional independent constructor or factory consumers.

## Deliberately omitted verification and plan adjustments

- No per-property, trivial forwarding, enum-storage, or duplicate DTO tests were added: existing invalid-input and selection/snapshot coverage exercises those paths.
- No full Core, separate Wallet/Deck/Gold/Draw, PlayMode, or unrelated UI regression runs were made. The affected Run fixture's 30 existing tests and focused map/view/contract suites cover this independent contract change; Unity groups cannot execute until D1/D2 are resolved.
- The extra `StageMapHighlightTests` constructor migration extends the plan's incomplete caller inventory and adds no new behavior or test cases.
- The isolated NUnit run supplements the blocked Unity attempt; it does not waive V1/V2 Unity acceptance. T1 Step 5 and Unity-dependent acceptance remain pending. Resolve D1/D2, complete the catalog/Host/installer/test conversion together, and run the affected Unity groups before declaring T1 complete. Do not start T2 as part of this handoff.
