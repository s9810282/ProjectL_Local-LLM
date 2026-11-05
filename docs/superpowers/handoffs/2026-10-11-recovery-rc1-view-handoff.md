# Recovery RC1 View Handoff

Prepared on 2026-10-12 (Asia/Seoul). This handoff documents immutable contracts and three Systems interface declarations for delivery through Git merge. Runtime implementations, service registration and View integration remain pending RC2/RC3/V4 and later integration gates.

## 1 Delivery and acceptance status

| Checkpoint | Status |
|---|---|
| Core RC1 source, metadata and verification evidence | Prepared locally; locations in section 5 |
| Git merge into View's branch | Not recorded |
| View signature comparison and acceptance | Pending |
| View V4 implementation started | Not recorded |

The user's accepted [proposal](2026-10-11-recovery-core-code-audit-and-rc0-rc1-proposal.md) and implementation plan (Git history: `2026-10-11-recovery-rc1-contract-implementation-plan.md`) are the implementation basis. The proposal's original status statements are historical; this document records the actual RC1 implementation. Prepared source is not proof of external acceptance or runtime completion.

## 2 Exact source and API manifest

Paths below are repository-relative. Every listed `.cs` includes its matching `.cs.meta`. The new `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Recovery.meta` is also included. Existing parent metadata and asmdefs are unchanged prerequisites, not replacement files.

| Prefix | Exact path / namespace |
|---|---|
| C | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts` |
| CN | `ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts` |
| B | `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Recovery` |
| BN | `ProjectL.Systems.RuntimeSystem.BufferSystems.Recovery` |
| R | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/RecoverySystem` |
| RN | `ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RecoverySystem` |
| T | `Assets/Tests/EditMode` |

All properties below are get-only; computed properties are identified explicitly. Core types compile in `ProjectL.Core.Contracts` without Unity, Framework or Buffer/View dependencies.

| File | Namespace | Public API |
|---|---|---|
| C/Commands/RecoveryEntryContext.cs | CN.Commands | `readonly struct`, `IEquatable<RecoveryEntryContext>`; constructor `(StageEntryId entryId, int chapterKey, StageNodeId nodeId, StageKey stageKey)`; properties `StageEntryId EntryId`, `int ChapterKey`, `StageNodeId NodeId`, `StageKey StageKey`; typed/object Equals, GetHashCode, ==, != |
| C/Commands/RecoveryCommandKind.cs | CN.Commands | Enum `Select = 1`, `Complete = 2` |
| C/Commands/RecoveryCommandData.cs | CN.Commands | Sealed class; `RecoveryCommandKind Kind`, `RecoveryEntryContext Context`, `RecoveryOptionId? OptionId`; private constructor; static `RecoveryCommandData Select(RecoveryEntryContext context, RecoveryOptionId optionId)` and `RecoveryCommandData Complete(RecoveryEntryContext context)` |
| C/Snapshots/RecoveryOptionState.cs | CN.Snapshots | Sealed class; constructor `(RecoveryOptionSpec option, int projectedHealAmount, RecoveryFailureReason unavailableReason)`; properties `RecoveryOptionSpec Option`, `int ProjectedHealAmount`, `RecoveryFailureReason UnavailableReason`; derived `bool CanSelect` |
| C/Snapshots/RecoveryStateSnapshot.cs | CN.Snapshots | Sealed class; constructor `(RecoveryEntryContext context, HealthStateSnapshot mainCharacterHealth, int gold, IReadOnlyList<RecoveryOptionState> options, RecoveryReceiptSnapshot receipt, bool isCompleted)`; matching PascalCase properties; derived `bool CanComplete` |
| C/Results/RecoveryResultKind.cs | CN.Results | Enum `Accepted = 1`, `TradeRejected = 2`, `StateMismatch = 3` |
| C/Results/RecoveryOperationResult.cs | CN.Results | Sealed class; `RecoveryResultKind Kind`, `RecoveryFailureReason FailureReason`, `bool AlreadyApplied`, `RecoveryStateSnapshot StateAfter`, `ChapterProgressionResult Progression`; derived `bool Succeeded`; private constructor; factories below |
| C/Results/RecoveryProcessedResult.cs | CN.Results | Sealed class; constructor `(int number, RecoveryCommandData command, RecoveryOperationResult result)`; properties `int Number`, `RecoveryCommandData Command`, `RecoveryOperationResult Result` |
| B/IRecoveryStateReadSink.cs | BN | `IServiceModule`; `bool TryGetCurrent(out RecoveryStateSnapshot state)` |
| B/IRecoveryResultReadSink.cs | BN | `IServiceModule`; `int HandledCommandNumber { get; }`, `bool TryGetLatest(out RecoveryProcessedResult result)` |
| R/IRecoveryCommandProcessingService.cs | RN | `IServiceModule`; `bool ProcessNextRecoveryCommand(int expectedNumber)` |

The three interfaces compile in `ProjectL.Systems`. They have **no implementation, registration or new Group accessor in RC1**. Future current-state reads return false/null if no recovery entry remains active; a completed but still-selected entry may return its completed snapshot. Future result reads return false/null before any result; otherwise they retain historical output, independently of the current popup. Query faults must not be converted into an ordinary false result.

```csharp
public static RecoveryOperationResult Selected(RecoveryStateSnapshot state, bool alreadyApplied = false);
public static RecoveryOperationResult Completed(RecoveryStateSnapshot state, ChapterProgressionResult progression);
public static RecoveryOperationResult TradeRejected(RecoveryFailureReason reason, RecoveryStateSnapshot state);
public static RecoveryOperationResult StateMismatch(RecoveryFailureReason reason);
```

StateMismatch has null StateAfter and Progression. Accepted Select and TradeRejected retain StateAfter and have null Progression. Accepted Complete has a completed StateAfter and matching node-only Progression. References follow the existing project's nullable convention; no nullable compiler settings changed.

Reused prerequisites: StageEntryId, StageNodeId, StageKey, RecoveryOptionId, RecoveryOptionSpec, RecoveryHealSpec, RecoveryHealMode, HealthStateSnapshot, RecoveryReceiptSnapshot, RecoveryFailureReason and ChapterProgressionResult. Legacy RecoveryStageSnapshot and RecoveryCommandResult constructors are unchanged. The already-integrated `BufferSystems/Current/IStageCommandResultReadSink.cs` remains the map/shop boundary; RC1 neither duplicates nor replaces it. Phase 0's cross-branch absence observation was specific to its earlier checkout.

| Test source (namespace / assembly `ProjectL.Tests.Core`) | Cases |
|---|---:|
| T/RecoveryInputContractTests.cs | 15 |
| T/RecoveryStateContractTests.cs | 51 |
| T/RecoveryOperationResultContractTests.cs | 49 |
| T/RecoveryProcessedResultContractTests.cs | 46 |

The RC1 changes contain 15 C# files, 15 source metadata files and one new folder metadata file. They are delivered with this document through Git merge.

## 3 Public-contract usage

This is a consumer-side example using supplied snapshots and results. These helpers only construct values and classify an already-read result; they do not submit input, invoke unimplemented services or acknowledge a result. Choose a displayed option index in the UI, then supply that same snapshot. A fixture supplies context only in tests; production receives it from Core's future state reader.

```csharp
using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

public static class RecoveryContractExample
{
    public static RecoveryCommandData Select(RecoveryStateSnapshot state, int optionIndex)
    {
        return RecoveryCommandData.Select(state.Context, state.Options[optionIndex].Option.Id);
    }

    public static RecoveryCommandData Complete(RecoveryStateSnapshot state)
    {
        return RecoveryCommandData.Complete(state.Context);
    }

    public static string Describe(RecoveryProcessedResult processed)
    {
        var command = processed.Command;
        var result = processed.Result;
        string request = $"#{processed.Number} {command.Kind} {command.Context.EntryId} " +
            $"chapter={command.Context.ChapterKey} node={command.Context.NodeId} " +
            $"stage={command.Context.StageKey} option={command.OptionId}";

        switch (result.Kind)
        {
            case RecoveryResultKind.Accepted:
                return $"{request}: accepted; alreadyApplied={result.AlreadyApplied}";
            case RecoveryResultKind.TradeRejected:
                return $"{request}: trade rejected ({result.FailureReason})";
            case RecoveryResultKind.StateMismatch:
                return $"{request}: stale/invalid context or option ({result.FailureReason})";
            default:
                throw new InvalidOperationException();
        }
    }
}
```

Before presenting any outcome, the future request-lifetime owner correlates Number and the complete echoed command (Kind, all Context fields and OptionId) with its retained request. It compares the echoed context to the currently displayed entry separately. A result from an old entry is still consumed and acknowledged, but is not painted onto a new popup. The example's string is illustrative diagnostic output, not a localization/UI policy or a correlation implementation.

## 4 Behavioral policy and integration obligations

Use the [original failure classification and priority table, section 4.3](2026-10-11-recovery-core-code-audit-and-rc0-rc1-proposal.md#43-operations-and-result-classification) as the policy source. Do not maintain a second failure table here. Same-option Select retries are Accepted with AlreadyApplied=true and no additional transaction. A duplicate Complete under a new request number is StateMismatch/StaleEntry; reprocessing an already-handled request number publishes nothing.

The [numbering and retention contract, section 4.4](2026-10-11-recovery-core-code-audit-and-rc0-rc1-proposal.md#44-numbered-requests-and-result-retention), requires one pending request and one result slot. View owns RecoveryViewCommand, input Buffer/read-write sinks, positive monotonically increasing numbers, removal and acknowledgement. Clear/Hide is not acknowledgement. A lifetime owner must keep consuming and acknowledging matched historical results while hidden and across entry/Run changes. Numbers, handled number, latest result and acknowledgement survive those changes within one runtime Buffer instance. Full runtime replacement ends that lifetime; old jobs must not enter a replacement runtime.

Future `ProcessNextRecoveryCommand(expectedNumber)` accepts only positive numbers. True means a previously unhandled exact-number request received a published result, including rejection. False means no new transaction/result because input is absent/cancelled/already handled or a prior result remains unacknowledged. View rereads input and output after false; it must consume prior output before retrying a retained input. Core never removes View input. Cancellation before commit produces no result; after commit, publication must finish despite Clear/Hide. Actual overwrite blocking, transaction atomicity and cancellation are later runtime tests, not proven by RC1 DTO constructors.

RC1 validates representable facts, not authoritative state freshness. The next reviewed Core plan must cover context issuance/preservation, fresh HP/gold reads, typed operations and the internal result Store (RC2); exact-number processing and shared registration after V4 (RC3); and integrated recovery transaction/lifetime regressions (RC4). Stage node entry routing must reuse/extend the integrated Stage path under its owner's reviewed contract. The third recovery option and production DB checks remain separate asset work.

## 5 Verification and Git handoff

Execution checkout: `D:/My_ProjectWork/Github_desktop/Project_L`, branch `feature/Cheon/refactor/core-system`, HEAD `141e917915d35d02120c0aea9039c301f36e7505`. RC1 additions are uncommitted; this SHA identifies the prerequisite checkout, not a delivery commit. The earlier Phase 0 baseline used `2d0509dffc6c249965dc10855074f16ac49091af`. No agent commit, push or external message was performed.

Unity `6000.0.73f1 (a166abc3bf0e)` ran sequentially via the approved AppData-access path. These are EditMode tests, not manual gameplay verification.

| Run under `Logs/RecoveryRC1/` | Coverage | Passed / failed / skipped | Exit |
|---|---|---|---:|
| p0-core-approved-20261011-235858700 | Original focused Core baseline | 34 / 0 / 0 | 0 |
| p0-framework-approved-20261012-000048215 | Original Framework baseline | 137 / 0 / 0 | 0 |
| p4-core-full-20261012-011535665 | Entire ProjectL.Tests.Core, including all four new fixtures | 2122 / 0 / 0 | 0 |
| p4-framework-20261012-011621813 | Planned recovery service, publication, adapter, cycle and factory cases | 141 / 0 / 0 | 0 |

Final counts, preservation checks and review disposition are recorded in plan section 10.8 (Git history: `2026-10-11-recovery-rc1-contract-implementation-plan.md#108-phase-4-interface-declarations-and-final-handoff-2026-10-12`). The original focused baseline remains covered by the final gates; the larger counts are not a same-sized all-project baseline comparison. Earlier phase records contain the behavioral RED/GREEN evidence. Both final logs have no C# error/warning diagnostics and contain the previously observed nonfatal licensing access-token update error. All 6,014 pre-Phase-4 source/asset/configuration files match their starting hashes after restoring the Unity-generated FMOD catalog scan timestamp; preexisting FMOD changes are preserved.

The user will deliver source, metadata and documents through Git merge. Share this handoff's repository path and the actual merged commit when available. Original XML/log/exit-code files remain in the run directories listed above; scope and review records remain under `Logs/RecoveryRC1/p4-baseline/`. These local Git-ignored test records can be consulted if needed; a separate archive is not required. The redundant ZIP, staging copy and package-only checksum file were removed at the user's request on 2026-10-12.

## 6 View acceptance checklist

- [ ] Compare all actual namespaces, signatures and existing prerequisite leaf types against View's checkout.
- [ ] Compile DTO consumers and retain snapshots/results without modifying their collections or duplicating mutable authority.
- [ ] Confirm interface declarations are available; do not assume their implementations, Group accessors or registration exist yet.
- [ ] Compare the merged source with this manifest and review the recorded test results; consult original XML/logs if needed, distinguishing constructor coverage from deferred runtime guarantees.
- [ ] Copy agreed signatures into View V4 through V6 planning and assign input numbering/acknowledgement/lifetime ownership.
- [ ] Record Git merge, signature acceptance and V4 start separately in section 1 when they actually occur.

SSOT review: context and immutable snapshots carry caller-supplied facts; no GUID/number issuer or duplicate wallet/health/receipt owner was added. CanSelect, CanComplete and Succeeded are derived. SOLID review: input values, display state, operation outcomes and correlation have separate responsibilities; the three consumer interfaces remain narrow. JMH/FW review: DTOs preserve the existing pure assembly boundary, Systems declarations follow FW-001, and no Store is exposed (FW-004). Registration, initialization and completed runtime preparation under FW-002/003/016 remain RC2/RC3 obligations. Phase 4 ends at this delivery gate.
