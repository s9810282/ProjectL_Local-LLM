# Task 7: CoreSystem Stage Entry and Outcome Flow

Date: 2026-09-27

Branch: `codex/chapter-map-task3`

## Task 8 correction

**Current API, 2026-09-28:** The coordinator now exposes PrepareStageEntry and ConfirmStageEntry in place of EnterStage. The validation path does not write Current. An execution Job must call Confirm only after actual entry preparation succeeds; that caller is not connected yet. See [Buffer contract correction](2026-09-28-buffer-contract-correction.md). Historical Task 7 implementation details and results below do not describe the new completion boundary.

The [Task 8 audit](2026-09-27-chapter-map-task8-audit.md) supersedes the completion interpretation below. Validation-only stage dispatch is not the approved playable-entry boundary. Task 8 reproduced premature `CurrentStage` mutation and acceptance of victory after defeat using real Buffers. The existing tests below establish the implemented behavior, not compliance with those two requirements. Task 7's entry/outcome execution remains incomplete; the earlier decision to treat validation as successful entry was incorrect.

## Scope boundary

This increment implements the CoreSystem portion of Task 7. The user-owned boundary excludes View and generic Framework changes. No file under `Script/Framework`, `InGameSystem/ViewSystem`, or `GlobalSystem/UISystem` is changed.

## Implemented flow

- `IChapterProgressionCoordinator.EnterStage` replaces the evaluation-only node-click API. It snapshots the current Buffer graph, evaluates the existing Core.Application and Domain navigation rules, calls the existing synchronous `StageServiceManager.Enter`, and writes `CurrentProgressBuffer.SelectStage` only after that call returns successfully.
- A stable temporary label, `Stage {stageKey}`, satisfies the current Buffer contract without adding display metadata.
- `IChapterProgressionCoordinator.ResolveStage` reevaluates the entered node and applies accepted effects. Ordinary victory clears only the Stage Buffer record. Boss victory clears the Stage record and then the already-prevalidated Map record. Defeat returns `EndAttempt` without writing either clear flag.
- Boss classification comes from the existing immutable `StageRootData.MapRole` through `StageServiceManager`. This avoids a Buffer shape or Framework change in this ownership window.
- The existing synchronous commands do not use a Job. The current Stage Job has no command payload and its LifeCycle hooks do not define a playable completion signal, so wrapping these Buffer writes in that placeholder would add replay risk without ordering real work.

## Current commit boundary

`StageServiceManager.Enter` currently resolves the DB record, chooses the StageType handler, and validates its data. The Combat, Shop, and Event handlers do not yet launch their playable services. The implementation currently writes `SelectStage` after this validation-only call; this violates the approved playable-entry contract. The write must follow confirmed playable entry. Until a real handler is connected, validation must not report a successful entry. If entry spans frames, its existing LifeCycle/Job integration must also prevent duplicate completion effects.

`StageLifeCycle` remains the stage-map scene lifecycle. Its `OnEnter` hook and `StageJob` are placeholders and receive no selected-stage payload. They were not changed.

## Verification

Unity 6000.0.73f1 EditMode `ChapterGraphPreparationTests` passed 8/8. It exercises the real DB tables, DB groups, Core factory, Buffer factory, and runtime phases. The focused Task 7 cases prove:

- accepted entry writes node, stage key, and temporary name;
- repeating the same uncleared entry is rejected;
- a missing DB stage throws before Current Buffer mutation;
- ordinary victory clears only the stage;
- traversing to and defeating the DB-classified boss clears the stage and map;
- defeat reports attempt end and leaves both clear flags false.

The existing `FrameworkRuntimeCompositionTests` passed 7/7 after the factory change. The focused managed Core suite passed 20/20. A separate Systems compile succeeded with pre-existing warnings in unrelated Audio, UI, and serialized data fields.

View intent routing and display remain outside this increment. The full plan's Task 7 exit condition is reached only when the View owner connects intent and display and the playable Combat/Shop/Event handlers define their real completion boundary.
