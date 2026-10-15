# Recovery Stage Core Connection — P2 Verification

Archive note (2026-10-03): raw execution artifacts and task scratch files referenced below were backed up outside the repository during user-requested cleanup. Commands and paths below record the original execution; the verification summary remains in this document.

Date: 2026-10-03  
Base: `e55348ff`, branch `feature/Cheon/refactor/core-system`  
Status: P1/P2 implemented and reviewed as an uncommitted checkpoint; P3 and complete Core gate C remain pending. No commit, push or production activation is claimed.

## Delivered consumer contract

Access the callable service through `inGame.CoreGroup.Recovery`:

```csharp
ChapterProgressionResult EnterRecovery(int chapterKey, StageNodeId targetNodeId);
bool Recover(int chapterKey, StageNodeId nodeId, RecoveryOptionId optionId);
ChapterProgressionResult CompleteRecovery(int chapterKey, StageNodeId nodeId);
```

After legitimate caller-owned battle settlement and `Combat.EndBattle()`, enter a legal recovery node. Acceptance installs Current selection/options together. Read installed options; do not separately confirm selection or reset options.

`Recover` validates current chapter/node, registered stage key/type/role, unresolved map/node, prepared Recovery, DB non-boss Event identity and absence of retained battle. It repeats storage validation after the root lookup and delegates to the existing `Combat.Recover`. Installed option values remain authoritative; options are not reloaded from DB. Existing soul-adjusted health, integer rounding, effect caps and payment/publication remain unchanged.

`true` means newly applied; `false` means that same option was already applied in this unresolved visit. Insufficient funds, unknown/different option, no effect, dead/missing main, stale identity or completed state reject by exception, without translating failures into `false`. Invalid scalar arguments throw `ArgumentException` subclasses. Legacy direct `Combat.Recover(optionId)` remains compatible and does not gain the new caller-identity guards.

`CompleteRecovery` accepts both applied and unapplied prepared recovery. It returns a progression result with only `ClearStage=true`; `ClearMap`, `EndAttempt`, `SelectMap` and `SelectStage` are false. It clears only the current node, leaving HP, currency, installed options/receipt, map flags, combat records/counters and View input/presentation untouched. It publishes no combat outcome and awards no rewards.

Duplicate completion returns `StageAlreadyCleared`. Stale chapter/node or changed graph binding returns the corresponding `ChapterMismatch`, `StageMismatch` or `StageNotFound`; completed maps return `ChapterCompleted`. Unprepared/non-recovery/Boss stages, missing dependencies, retained battles and invalid definitions throw. Applied or duplicate healing after completion is rejected before the legacy operation.

Completion without recovery was explicitly approved by the user on 2026-10-03 and is now implemented. Full HP or an unaffordable menu does not block this exit: it costs zero, heals zero, records no applied option and clears the node through the same completion API.

## Completion-only retry

Recovery/payment and completion remain separate operations. If recovery succeeds and completion fails before its clear write, retain the committed HP, currency and applied receipt; the node stays uncleared. Correct the failure and retry only `CompleteRecovery` for the same current identity. Do not roll back, call `Recover` again, re-enter or reinstall options.

The focused case uses HP 7/10, balance 8 and a Full option costing 3. Recovery commits HP 10, balance 5 and a receipt with paid/healed 3. An existing root-query callback faults completion with all facts preserved. Removing the fault permits one uncleared-to-cleared transition, no additional healing/payment and the same receipt. Duplicate completion rejects without another mutation.

## Implementation scope

- `RecoveryStageService.cs`: guarded application/completion and storage checks before/after DB root lookup.
- `ChapterProgressionCoordinator.cs`: internal expected-identity check, pure completion decision and one accepted `SetClear` call. Public progression interface is unchanged.
- `ChapterProgressionCommands.cs` / `ChapterProgressionRules.cs`: outcome-free recovery completion. Existing combat validation order and outcome evaluation remain unchanged.
- `RecoveryStageOperationsTests.cs`: application/completion coverage in the existing partial `RecoveryStageServiceTests` fixture, with Unity metadata; `RecoveryStageServiceTests.cs` is now partial.
- `ChapterProgressionCommandsTests.cs`: pure completion acceptance and invalid progress/input cases.
- Test-only `RecoveryBufferSnapshot`: optional progress comparison suppression, paired with exact node-field assertions for accepted completion. All existing assertions retain their defaults.

No new runtime Buffer, session, revision, transaction or production fault hook was added. Production Job/Flow/View, legacy Combat code and authored assets were not changed by P2.

## Verification and review

Unity 6000.0.73f1 used the approved user-profile execution path with AppData access. Evidence and `run-tests.ps1` are in `.superpowers/verification/recovery-stage-core-connection/`. Runs use EditMode assemblies `ProjectL.Tests.Core;ProjectL.Tests.Framework;ProjectL.Tests.CombatViewBoundary`.

| Run | Passed / total | Unity exit | Evidence |
| --- | --- | --- | --- |
| `p2-baseline` | 1449/1454 | 2 | Fresh P1 checkpoint baseline; five existing Economy failures. |
| `p2-red-compiled` | 28/82 | 2 | All 54 new cases fail for absent P2 APIs; 28 existing cases pass. Earlier compile failure is excluded from RED. |
| `p2-green-first` | 134/134 | 0 | Initial application/completion and existing settlement/recovery contracts. |
| `p2-green-typed` | 262/262 | 0 | Typed APIs and expanded lifecycle, soul-rounding, stale identity and existing entry/graph/cycle regressions. |
| `p2-review-red` | 0/2 | 2 | Active/terminal battle installed during root lookup exposes missing repeated battle guard. |
| `p2-green-final` | 264/264 | 0 | Review fix plus all nine intended fixtures discovered; no skips/inconclusive cases. |
| `p2-broad-verified` | 1513/1518 | 2 | 64 added cases, none missing; identical baseline failure names/messages/stacks. |

Final focused counts: recovery service 73, pure chapter 19, existing recovery rules 10, recovery contracts 24, battle settlement 18, DB adapter 27, recovery publication 26, registered combat cycle 27 and chapter graph 40. The original RED tests used reflection solely to compile before API creation; final tests call typed APIs.

The broad run has Core 934/934, Framework 573/578 and CombatViewBoundary 6/6. The suite is not all-green. All five existing catalog failures report `[CombatStageDetail] Combat stage requires Economy`, exactly as the fresh baseline:

- `ArtifactDataTableTests.Catalog_RegistersOnlyMigratedRulesAndExplicitlyPendingViews`
- `DataBaseAssetTests.Catalog_CreatesAllDataBaseGroups`
- `DataBaseAssetTests.CombatStages_ReferenceExistingInGameData`
- `DataBaseAssetTests.EveryStage_HasNodeVisual`
- `DataBaseAssetTests.EveryUnitAndStructure_HasUnitVisual`

`p2-verified-comparison.json` / `p2-verified-failures.json` retain the comparison. No failure is suppressed or treated as passing.

A fresh read-only reviewer found one Important issue and no Critical/Minor issues: completion did not repeat the retained-battle guard after DB root lookup. Active/terminal callback cases reproduced it, then passed after moving the existing guard into the shared storage validator. Final focused and broad runs verify the single review-fix pass. Reviewer inspection is separate from the parent's Unity execution evidence.

## Remaining work and decisions

P3 must still prove measurable settlement-once behavior across battle → caller cleanup → recovery/application or skip → completion → next battle, and write the final package handoff. Constituent cycle/settlement regressions are reused; their passing counts do not replace this missing bridge. DB authoring readiness (D), View/Flow adoption (V) and production activation (A) remain external/pending. The user-approved no-purchase policy (P) is implemented in Core; complete Core gate C still requires P3 evidence.

Continue in the existing feature checkout with PowerShell orchestration and retained evidence; this preserves the approved uncommitted P1 work but shares checkout state and leaves verification artifacts to manage. The new recovery domain operation stays separate from combat outcome evaluation to preserve compatibility; the cost is small repeated identity validation. General concurrent/reentrant writers, direct legacy calls and identity reuse across Run reset remain outside the delivered guarantees. Existing injected-query mutation seams are covered by explicit post-query validation. Incoming FMOD bytes and attachments are preserved, with only Unity's additional scan timestamp restored after verifying it was the sole extra asset mutation.
