# Buffer Ownership Contract Correction

Date: 2026-09-28

## Governing contract

The user supplied the Framework developer's ownership contract and requested correction of deviations. Buffer owns runtime progression, initialization scope, and record retention. Systems-side Core processing binds Buffer services through PostBindService, supplies command-local values to Application/Domain, and applies decisions through public Buffer APIs. Core does not keep another mutable progression store. View reads Buffer and does not own rule execution or Job ordering.

The user also confirmed defeat/restart gameplay: retain permanent Artifact ownership, regenerate the map, and wait for first-node selection. That requirement does not authorize placing the reset policy in Core. The older source note excludes restart; the later user gameplay requirement extends that scope while retaining Buffer ownership. The user has approved the Buffer reset API below. Job/LifeCycle changes still require separate approval.

## Corrected implementation

- Removed ChapterProgressionCoordinator.InitializeRun and its Character/Currency bindings. Core no longer clears Current/Stage records, resets all Map flags, or chooses which unrelated Buffers to erase.
- Removed the generator's reset-only BuildGraph/seed override extension. Existing Generate, DB seed configuration, and connection algorithm selection are retained. Randomized restart must use the approved map configuration; no hidden policy overrides the DB setting.
- Replaced coordinator EnterStage with PrepareStageEntry and ConfirmStageEntry. Prepare checks current Buffer rules and DB content without recording entry. Confirm is the explicit entry-success boundary for the future execution Job; it rechecks current Buffer rules/content and calls SelectStage. Neither method stores pending entry state.
- Added an internal StageServiceManager.ValidateEntry helper to avoid invoking the execution handler merely to validate. The existing StageServiceManager.Enter dispatch API is unchanged.
- Kept chapter-entry record retention and ordinary/boss clear effects. ResolveStage remains a synchronous outcome-effect method; it does not prove a live battle completed or implement restart.

ConfirmStageEntry trusts the caller's completion signal. It does not independently observe playable success or create a battle. The current runtime has no Job caller connected to that signal, so no playable cutover is claimed. Scene, Job, and generic Framework production code is unchanged. The approved Buffer change is described below.

## Test correction

Removed four tests for the withdrawn Core-owned reset operation. They proved its mechanics, not compliance with Buffer ownership.

Removed Audit_DefeatMustPreventLaterVictoryForSameAttempt as a mandatory gate: it required persistent attempt termination in a command boundary that has no such contract. The previously observed behavior is not claimed fixed. A real restart/late-callback requirement must be tested where the actual execution path is introduced, without assuming Run versions or receipts.

Replaced the premature-entry audit with a behavioral test proving preparation does not write Current or clear the stage. Added confirmation revalidation and chapter-entry preservation across another completed chapter, Artifact, Character, and Currency. Explicit confirmation tests simulate a successful execution caller; they do not simulate a real playable stage.

## Approved Buffer reset API

Implemented `IRunProgressBufferWriteSink.ResetRunProgress()` in the existing BufferSystemGroup, exposed as `IBufferQuery.RunProgressWriteSink`. It follows the existing service-interface and sink-accessor style. The group implements the sink directly; no new state Buffer, Factory, service key, or root service registration is needed. Callers use the existing Buffer context rather than looking up the write interface as a separate root service.

| Data | Reset policy |
| --- | --- |
| Current | Clear selected map and stage. Do not choose the initial chapter. |
| Stage | Remove all generated chapter graphs and stage clear records. |
| Map | Preserve registrations; set all completion flags to false. |
| Character / Currency | Clear temporary character records and set currency to zero. |
| Artifact | Preserve existing ownership records and objects. |

Character/Artifact records are placeholders; this operation does not claim to reset legacy deck/health/resource owners that are not stored in these Buffers. It uses existing sink instances so already-bound readers observe the reset. Repeated calls before new gameplay are safe, but callers must not repeatedly reset after new progress has begun. No database lookup, generation, combat cleanup, scene change, event, or Job scheduling occurs inside this method.

Implementation location: `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Core/BufferSystemGroup.cs`. Bind the existing context in PostBindService and retain the write sink when needed:

```csharp
var buffers = resolver.GetService(BufferSystemKey.Buffer);
var reset = buffers.RunProgressWriteSink;
reset.ResetRunProgress();
```

The last line represents the later explicit reset command, not an instruction to reset during PostBindService. No Core-owned reset wrapper is added.

A later approved Plan/Job can request this operation and then call EnterChapter for the initial chapter; that existing command generates the missing graph and selects the chapter. The map configuration must enable randomized run seeds for randomized layouts. Buffer owns deletion/retention policy; Plan/Job owns the order of combat cleanup, reset, chapter preparation, and display. That connection must distinguish restart from victory return. It must not repeat Framework InitData or move initialization policy back into Core or View.

## Verification

After the approved Buffer reset addition, Unity 6000.0.73f1 EditMode: **27/27 passed** across MapProgressBufferTests (8), ChapterGraphPreparationTests (12), and FrameworkRuntimeCompositionTests (7). The three added cases verify reset/retention across multiple chapters, repeated/empty reset and group isolation, and explicit graph preparation after reset. Evidence: `C:/CodexHome/visualizations/2026/09/27/01a0e23d-3048-7a21-9127-a1fea325480a/buffer-reset.xml`. Job execution and playable restart were not exercised.

Unity 6000.0.73f1 EditMode: **18/18 passed** across ChapterGraphPreparationTests (11) and FrameworkRuntimeCompositionTests (7). This verifies explicit confirmation, non-mutating preparation, chapter/record retention, ordinary/boss clear effects, and real Buffer composition. The suite changed for the contract reasons above; the removed termination test is not reported as a fixed defect. Full playable runtime, View integration, and restart remain unverified and unimplemented at this boundary.

Evidence: `C:/CodexHome/visualizations/2026/09/27/01a0e23d-3048-7a21-9127-a1fea325480a/contract-correction.xml`. `git diff --check` passed. Changes remain local; the base checkout and remote were not modified.
