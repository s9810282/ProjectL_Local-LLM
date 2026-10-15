# Core Run Initialization

**Withdrawn on 2026-09-28.** The implementation described below was removed because Core directly composed Buffer initialization, violating the supplied ownership contract. Its test results are historical evidence only. Do not call InitializeRun; that API no longer exists. See [Buffer contract correction](2026-09-28-buffer-contract-correction.md) for the current implementation and the Buffer-owned reset proposal awaiting user approval. The rest of this document preserves the withdrawn implementation record.

Date: 2026-09-27

## Confirmed behavior and scope

The user clarified that defeat ends the current run. A new run regenerates the stage map, resets temporary progression, preserves permanent Artifact ownership, and waits for the player to select the first playable node. This supersedes the earlier recommendation to add a terminal flag or EndAttempt operation to CurrentProgressBuffer.

The current authorization covers the Core initialization function. Job, LifeCycle, Framework, and View edits still require the user's approval. Approval from a separate Framework developer is not an additional requirement imposed by the user.

## Entry point

`coreGroup.ChapterProgression.InitializeRun(firstChapterKey)`

Call after normal Framework InitData/PostBindService and after the owner has stopped the previous stage, detached old callbacks, and successfully cleaned up temporary stage resources. Pass the configured first chapter's DB key; the method does not assume that chapter number one has database key 1.

This synchronous method:

1. Verifies the requested chapter is registered and builds a new graph in temporary memory using a fresh random seed.
2. Clears Current selection and all registered Stage graphs/clear flags.
3. Resets every registered Map's clear flag without deleting Map registrations or repeating Framework InitData.
4. Clears the existing Character and Currency Buffers.
5. Registers the new first-chapter graph and selects that chapter, leaving CurrentStage empty.

Artifact Buffer contents are preserved. No stage is entered and no View operation is performed. A new random sample can coincidentally produce the same layout; the method does not promise unique layouts. Explicit new-run initialization uses a fresh seed even when the DB is configured for a fixed seed. Ordinary Generate retains its existing DB seed policy and selected connection algorithm.

StageGeneratorSystem now prepares its graph through an internal BuildGraph method. Ordinary Generate builds and registers as before. Initialization builds before clearing live state, so missing chapter, invalid map settings, or missing boss/regular content fail without discarding existing progression. This is a synchronous prevalidation guarantee for the existing in-memory sinks, not a general transaction or rollback mechanism.

## Integration limits

Each successful call intentionally starts another run. A future Job must invoke it once for a new-game/restart request, not once per Tick and not on ordinary victory return. Ordinary map entry continues to use EnterChapter, preserving the current graph and progress.

The current CoreGroup does not own a live StageServices instance or the legacy RunProgressionService. CharacterData and ArtifactData are placeholder records; clearing CharacterBuffer does not construct a starting deck or restore a playable character's health. CurrencyBuffer is also separate from the legacy Run wallet. This method initializes the connected Buffer state; live stage cleanup and the unmigrated deck/health/shop/recovery owners remain integration work. It must not be advertised as a complete playable restart.

The two existing Task 8 regressions remain open: validation-only entry is recorded as successful entry, and ResolveStage by itself does not enforce attempt termination. This function is not automatically called by ResolveStage. When restart is connected, stale outcomes need originating execution identity or cancellation: node/stage keys alone are insufficient because regenerated graphs can reuse those keys.

## Verification

Four added real-Buffer cases cover initial preparation, multi-chapter reset and repeated new-run initialization, permanent Artifact preservation, missing chapter, and missing boss content. All four passed in Unity 6000.0.73f1 EditMode. The combined ChapterGraphPreparation and FrameworkRuntimeComposition run completed with **21 cases: 19 passed, 2 failed**. The failures are the unchanged Audit_DefeatMustPreventLaterVictoryForSameAttempt and Audit_UnwiredStageHandlerMustNotRecordPlayableEntry cases described above. No full playable restart or fully passing suite is claimed.

Result file: `C:/CodexHome/visualizations/2026/09/27/01a0e23d-3048-7a21-9127-a1fea325480a/run-initialization.xml`.
