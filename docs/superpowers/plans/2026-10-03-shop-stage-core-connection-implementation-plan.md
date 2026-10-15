# Shop Stage Core Connection Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:executing-plans` for sequential implementation, or `superpowers:subagent-driven-development` if the user selects delegation. Steps use checkboxes. This document does not start implementation or select delegation.

**Goal:** Connect explicit shop entry, optional character purchase and shop-node completion to the next battle through existing Core services.

**Architecture:** Extend the existing Shop service and inject the existing progression coordinator. Prepare selection/offers together in Current Buffer, reuse purchase publication, and complete only the selected shop node through pure progression rules.

**Tech Stack:** C#, Unity 6000.0.73f1, existing Framework and NUnit EditMode tests; no new dependency or assembly.

**Spec:** [Shop Stage Core Connection Design](../specs/2026-10-03-shop-stage-core-connection-design.md).

**Status:** P1–P3 complete, verified and independently reviewed on 2026-10-03. P3 focused 326/326; broad 1666/1671 with five unchanged Economy failures. See the [consumer handoff](../handoffs/2026-10-03-shop-stage-core-connection-handoff.md). Uncommitted. P2 focused 267/267; broad 1661/1666 with the same five baseline Economy failures and 67 new passing cases. See the [P1 verification report](../handoffs/2026-10-03-shop-stage-core-connection-p1-verification.md) and [P2 verification report](../handoffs/2026-10-03-shop-stage-core-connection-p2-verification.md).

## Global constraints

- Implementation documentation is English; new code comments are Korean.
- Follow the six Framework references in the design. Keep Framework/Core assembly dependencies unchanged.
- Runtime facts remain in existing Buffers. No new store, session, revision counter or generic transaction framework.
- Preserve legacy `PrepareShop(int)`/`Purchase(ShopOfferId)`, offer RNG behavior, `SoldOut`, character identity, recovery and combat contracts.
- New consumers use explicit chapter/node APIs. Keep the existing Shop service key and Core Context property.
- Allow explicit completion after zero, one, two or three purchases, including unaffordable offers. Never auto-complete on the third purchase.
- No production Job/Flow/View, importer/assets, reward or Run-end work; no commit or push without user instruction.
- Operations are synchronous and serialized. Query-injected external mutations are not rolled back; rejected operations add no writes of their own.
- Keep raw test XML/logs/launcher files outside the repository. Preserve the existing uncommitted temporary runtime package.

## Review focus

1. A late candidate lookup fails or changes graph type/role: entry must preserve selection and prior receipts. P1 tests.
2. The final purchase unit lookup changes selection or installs a battle: no acquisition/payment may follow. P2 tests.
3. Different nodes share a StageKey: new offers belong to the new node and stale intent must not consume them. P1/P2 tests.
4. Purchase succeeds and completion fails: purchased identity, currency and mask survive; retry only completion. P2 tests.
5. Terminal battle remains after settlement: entry rejects until legitimate caller cleanup; subsequent shop/next-battle operations preserve persistent facts without replaying settlement. P3 tests.

## File map

Aliases are exact repository-relative prefixes:

| Alias | Prefix |
| --- | --- |
| `Core/` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/` |
| `Calc/` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/` |
| `Buffers/` | `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/` |
| `Factory/` | `Assets/ProjectL/Script/Systems/Factory/Runtime/` |
| `Tests/` | `Assets/Tests/EditMode/Framework/` |

| Phase | Action / file | Responsibility |
| --- | --- | --- |
| P1–P2 | Modify `Core/ShopSystem/CharacterShopService.cs` | Extend its interface and implementation; share existing offer conversion, keep legacy behavior. |
| P1 | Create `Buffers/Current/ShopEntryPreparation.cs` | Immutable expected-origin/target/offers DTO. |
| P1 | Modify `Buffers/Current/CurrentProgressBuffer.cs` | Prepare shop selection and reuse one install boundary. |
| P1 | Modify `Buffers/Core/BufferSystemGroup.cs` | Narrow entry write port, live validation and query forwarding. |
| P1 | Modify `Factory/InGame/Core/InGameCoreSystemFactory.cs` | Pass existing progression instance to Shop constructor. |
| P1 | Modify `Tests/ChapterResetSpy.cs`, `Tests/ArtifactEditingServiceTests.cs` | Forward new `IBufferQuery.ShopStageWriteSink` in existing test doubles. Search for additional implementers before editing. |
| P1 | Create `Tests/ShopEntryPublicationTests.cs` | Real Current/Buffer preparation and preservation. |
| P1–P2 | Create `Tests/ShopStageServiceTests.cs`, `Tests/ShopStageTestDatabase.cs` | Registered-service fixture and test-only query seams, following recovery fixture style. |
| P2 | Create `Tests/ShopStageOperationsTests.cs` | Partial service fixture for guarded purchase/completion/retry. |
| P1–P2 | Modify `Core/ChapterProgressionCoordinator.cs` | Query-free entry revalidation in P1; internal accepted completion write in P2. |
| P2 | Modify `Calc/Application/Chapter/ChapterProgressionCommands.cs`, `Calc/Domain/StageMap/ChapterProgressionRules.cs` | Outcome-free pure shop completion. |
| P2 | Modify `Assets/Tests/EditMode/ChapterProgressionCommandsTests.cs` | Shop completion decisions and existing progression regressions. |
| P3 | Modify `Factory/Development/TemporaryCoreRuntimeFactory.cs`, `Factory/Development/TemporaryCoreDatabase.cs` | Named shop scenario and stage 30 definition; retain default recovery scenario. |
| P3 | Create `Tests/TemporaryCoreShopRuntimeTests.cs` | Real battle/shop/next-battle sequence. |
| P3 | Create `docs/superpowers/handoffs/2026-10-03-shop-stage-core-connection-handoff.md` | Consumer sequence, failure/retry, evidence and external acceptance. |
| P3 | Modify `docs/superpowers/handoffs/2026-10-03-temporary-core-runtime.md` | Document the additional development entry point separately from original evidence. |

Create unique Unity `.meta` files for new Assets files. Do not regenerate existing GUIDs. No changes to `CharacterShopRules`, character/currency purchase stores, service keys or Core Context are intended. A small private completion helper may be shared with recovery; do not broaden this into a progression refactor.

## Verification procedure

Before implementation, preserve the existing worktree changes and capture a fresh broad baseline. Check that no other Unity Editor owns this checkout. Use the approved AppData-capable execution path: the known sandbox `CurlRequestCache.db` failure occurs before project code and does not justify cache deletion or a Unity upgrade.

Keep evidence in a fresh directory under the system temporary directory, outside the repository. Run this command shape from an approved PowerShell process after confirming the executable path; the path contains spaces, so quote the project argument:

```powershell
$shopEvidence = Join-Path ([System.IO.Path]::GetTempPath()) ('ProjectL-shop-core-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $shopEvidence | Out-Null
$shopLabel = 'baseline'
$shopFilter = ''
$shopArgs = @('-batchmode', '-nographics', '-projectPath', ('"' + (Get-Location).Path + '"'),
    '-runTests', '-testPlatform', 'EditMode',
    '-assemblyNames', 'ProjectL.Tests.Core;ProjectL.Tests.Framework;ProjectL.Tests.CombatViewBoundary',
    '-testResults', ('"' + (Join-Path $shopEvidence ($shopLabel + '.xml')) + '"'),
    '-logFile', ('"' + (Join-Path $shopEvidence ($shopLabel + '.log')) + '"'))
if ($shopFilter) { $shopArgs += @('-testFilter', $shopFilter) }
$shopProcess = Start-Process -FilePath 'D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe' -ArgumentList $shopArgs -WindowStyle Hidden -PassThru
```

Poll the created process in bounded intervals and inspect its final exit code and NUnit XML. Preserve arguments/exit status alongside the XML/log. For subsequent runs reuse the evidence directory, change the label and filter before building arguments, and do not overwrite baseline results. Do not start overlapping Editors. Missing XML, compile failure or zero discovered tests is not a passing run.

Focused filters are fully qualified fixture names separated by semicolons. P1/P2 use `ProjectL.Tests.Framework.ShopEntryPublicationTests;ProjectL.Tests.Framework.ShopStageServiceTests`; the partial operations fixture keeps the latter class name. Also run existing `CharacterRosterServiceTests`, Core `CharacterRosterRuleTests`/`ChapterProgressionCommandsTests` and affected composition/recovery fixtures, resolving their existing namespaces from source. P3 adds `ProjectL.Tests.Framework.TemporaryCoreShopRuntimeTests;ProjectL.Tests.Framework.TemporaryCoreRuntimeTests` plus chapter-graph and battle-preparation regressions.

Expected RED: the new named tests fail specifically because the contract is missing or incorrect; compilation/setup errors alone are not behavioral RED evidence. Expected GREEN: all selected tests pass, with no skipped cases. Run the broad suite once before changes and once at the end; repeat only for changed code or a specific failure investigation. The historical broad result is 1532/1537 with five catalog/Economy failures; compare against the fresh baseline, and do not report the whole suite green while those failures remain.

## P1 — Prepared shop entry

**Interfaces produced:**

```csharp
ChapterProgressionResult ICharacterShopService.EnterShop(int chapterKey, StageNodeId targetNodeId, int seed);
internal CharacterShopService(ChapterProgressionCoordinator progression);
void IShopStageBufferWriteSink.PrepareShop(ShopEntryPreparation preparation);
IShopStageBufferWriteSink IBufferQuery.ShopStageWriteSink { get; }
PreparedCurrentSelection ICurrentProgressBufferStore.PrepareShopSelection(ShopEntryPreparation preparation);
```

The internal store interface and its explicit implementation both live in `CurrentProgressBuffer.cs`; follow the existing `PrepareRecoverySelection` placement. The DTO constructor is `ShopEntryPreparation(int expectedChapterKey, bool expectedHasSelection, StageSelectionData expectedStage, StageNodeId targetNodeId, StageKey targetStageKey, string targetStageName, IReadOnlyList<ShopPurchaseSnapshot> offers)`. Expose those inputs as read-only properties, using `Offers` for the final argument, and validate/copy through `CurrentShopData` like recovery does through `CurrentRecoveryData`.

- [x] **P1.1: Add entry/publication tests.** `EnterShop_PreparesSelectionAndThreeOffersTogether` asserts matching chapter/node/key, three frozen offers, zero purchased mask and cleared old recovery selection. `EntryFailure_PreservesPreviousState` covers invalid/unreachable/cleared targets, boss/nonshop roots, retained active/terminal battle, invalid unselected candidate, a later lookup exception and changed origin/graph key/type/role during queries. Snapshot HP, souls, currency, roster IDs, progress, combat state/result/counters and old shop/recovery receipts; exclude only mutations deliberately performed by the query fixture itself.
- [x] **P1.2: Run RED.** Add only the minimum API declarations if needed for compilation, then run the P1 filter. Verify assertion failures for absent publication/protection; keep the evidence outside the repo.
- [x] **P1.3: Implement DTO and Buffer publication.** Add the narrow port/property on the existing group, expected-origin and live metadata checks, `PrepareShopSelection` and one `InstallSelection`. Forward the property in all existing `IBufferQuery` test doubles. Reject identical selected targets; never clear old state before successful installation.
- [x] **P1.4: Implement service entry/composition.** Inject the existing coordinator; preflight, read/validate/convert all candidates with existing rules, repeat preflight after the last lookup, then publish. Preserve legacy `PrepareShop` and its no-reroll behavior. After the repeated preflight finishes its own DB query, call the new internal query-free `ChapterProgressionCoordinator.RevalidateStageEntry(int chapterKey, StageNodeId targetNodeId)` before comparing origin/target and publishing. Register no additional service/module or key.
- [x] **P1.5: Pin visit identity and composition.** `EnterShop_DifferentNodesWithSameStageKeyHaveIndependentOffers` proves receipts are not shared; `EnterShop_SameSelectedNodeCannotRefreshOffers` proves consumed slots survive rejection. Fixture setup exercises both supported InitData/PostBind orders using real Factory exports and `CoreGroup.Shop`; child Shop is not separately exported from the root.
- [x] **P1.6: Verify GREEN and inspect the diff.** Run P1 plus existing roster/shop and affected composition/recovery tests. Require no new behavior change in legacy APIs and no files beyond the P1 inventory/mechanical forwarding. P1 is independently deliverable as safe entry, without claiming node completion exists yet.

## P2 — Guarded purchase and explicit completion

**Consumes:** P1 entry and prepared `CurrentShopData`; existing `PlanPurchase`/`PublishPurchase` and progression stores.

**Interfaces produced:**

```csharp
CharacterPurchaseResult ICharacterShopService.Purchase(int chapterKey, StageNodeId nodeId, ShopOfferId offerId);
ChapterProgressionResult ICharacterShopService.CompleteShop(int chapterKey, StageNodeId nodeId);
internal ChapterProgressionResult ChapterProgressionCoordinator.CompleteShopStage(int chapterKey, StageNodeId expectedNodeId, StageKey expectedStageKey);
```

Add `ChapterProgressionCommands.CompleteShopStage` with the same parameters/return type as `CompleteRecoveryStage`: requested/current/graph chapter keys, selection presence, entered node/key, chapter-clear flag and `IReadOnlyList<ChapterNodeProgress>`. Its domain counterpart `EvaluateShopCompletion` matches `EvaluateRecoveryCompletion` inputs. Keep the public progression service interface unchanged.

- [x] **P2.1: Add guarded-purchase tests and run RED.** `Purchase_ExplicitIntentUsesFrozenOfferAndNewOwnedIdentity` pins cost, one new Sub identity and consumed bit. `Purchase_RejectsStaleNodeEvenWhenStageKeyMatches` pins `EntryMismatch`. `Purchase_StateChangesDuringFinalUnitLookupAddsNoWrites` covers selection/shop replacement, graph changes and active/terminal battle installation after stage lookup. Expect the design's `EntryMismatch`/`StaleEntry`/`WrongPhase` mapping, with no additional acquisition/payment/slot write. Keep existing `UnknownOffer`, `SoldOut`, insufficient-funds and overflow tests as rule/publication regression coverage.
- [x] **P2.2: Implement guarded purchase.** Capture node/key/shop before queries; revalidate after the final unit query. Read current main/balance after this guard, reuse `PlanPurchase` and publish once with captured IDs. Do not call the legacy purchase blindly after a precheck because its later query is another validation boundary.
- [x] **P2.3: Add completion tests and run RED.** `CompleteShop_AllowsZeroThroughThreePurchases` uses four cases and asserts exactly the selected node becomes clear, with no chapter clear, map-end attempt, combat settlement/reset or financial/roster change. Assert the third purchase alone leaves the node uncleared. `CompleteShop_AfterUnaffordablePurchaseAllowsExit` proves failed purchase does not block exit. Cover duplicate completion (`StageAlreadyCleared`), guarded purchase afterward (`StaleEntry`), mismatched intent, unprepared/nonshop/boss state and battle installed during the root lookup. Extend pure command tests for matching/mismatching chapter/node/key, cleared map/node and absent selection.
- [x] **P2.4: Implement completion.** Validate prepared shop and root/graph before and after the root lookup, then call the internal coordinator. Use the pure outcome-free command and one accepted `SetClear`. Share only genuinely identical private recovery validation; retain all recovery results and public signatures.
- [x] **P2.5: Pin purchase-success/completion-failure.** With gold 50, buy a cost-5 offer: assert gold 45, one additional owned identity and its purchased bit. Make the test DB root query throw during completion before the clear write: assert the same gold/identity/mask and an uncleared node. Restore the query and call only `CompleteShop`: assert one clear transition and unchanged HP/gold/roster/mask. Duplicate completion makes no further write. Use a query fixture or existing spy; add no production fault hook or entry reset.
- [x] **P2.6: Verify GREEN.** Run the P2 filter plus pure progression, existing roster/shop, character battle-cycle and recovery tests. Confirm legacy duplicate purchase still means `SoldOut`, prices remain frozen and no new global state or rollback mechanism exists. P2 delivers the complete Core consumer API.

## P3 — Temporary shop cycle and handoff

**Consumes:** P1/P2 APIs and the existing uncommitted temporary runtime package.

**Interface produced:** `public static FrameworkRuntime TemporaryCoreRuntimeFactory.CreateWithShop()` and `public const int ShopNodeId = 5`, under the existing `UNITY_EDITOR || DEVELOPMENT_BUILD` guard. Keep `Create()` unique and unchanged in behavior; use a small private composition helper if needed. No new battle-source implementation is required.

- [x] **P3.1: Add cycle tests and run RED.** Create `TemporaryCoreShopRuntimeTests` using `CreateWithShop` and real registered services. `BattleShopNextBattle_PreservesOwnedFacts` has purchase-count cases 0, 1, 2 and 3. Complete the first battle through normal commands, demonstrate retained terminal battle blocks shop entry, call the existing `EndBattle`, enter shop, purchase/skip, complete and enter the boss. Assert wallet `50 - 5*n`, owned count `4 + n`, new distinct identities with zero souls, persistent HP/existing souls, and purchased IDs exactly once in the next battle's full draw order. Assert result/counter retention and cleanup at the same boundaries as existing battle/recovery tests; do not impose indefinite retention after the next battle begins.
- [x] **P3.2: Implement the explicit temporary scenario.** Add stage 30, regular `ShopStageDetail`, one Akimbo candidate costing 5. `CreateWithShop` uses graph 1 → 2 → 5 → 4 with depths 0–3 and stage keys 0/101/30/102. Reuse initialized main-plus-three roster, 50 gold, fields and targeting profiles. Keep `Create()` at 1 → 2 → 3 → 4 with recovery stage 20. Do not change production composition or add a fallback for bad authored data.
- [x] **P3.3: Verify focused GREEN and broad regression once.** Run new cycle tests, original temporary/recovery cycles, chapter-graph reflection/composition and battle preparation fixtures. Then run the broad procedure, compare test names and outcomes with baseline and investigate any new failure. A default-path regression or changed baseline failure blocks completion; unrelated catalog failures remain explicitly reported. Existing deck-rule tests are reused, not duplicated into a new deck project.
- [x] **P3.4: Write the consumer handoff.** Document caller-owned `EndBattle`, explicit entry, optional identity-aware purchase, explicit completion, result/failure meanings and retry-only-completion. State legacy limitations, the separate two temporary scenarios, their disposable data and how production data replaces them. Record actual commands/evidence locations and counts only after execution. Separate Core service acceptance from authored-data and Job/Flow/scene acceptance; mark no work on final Run policy.
- [x] **P3.5: Check delivery.** Inspect changed-file scope, `.meta` GUIDs, English docs/Korean new comments, unchanged assembly dependencies and development guards. Run `git diff --check`; ensure no XML/log/launcher scratch files enter the worktree. Preserve unrelated user changes and leave committing/pushing to explicit instruction.

## Completion gate

The package is complete when P1–P3 have source-backed verification, new focused tests pass, the broad comparison adds no regressions, and the handoff contains a usable consumer sequence and retry behavior. This means Core shop integration is ready for its caller; it does not mean scenes, production data, final Run completion or the entire framework migration are finished.
