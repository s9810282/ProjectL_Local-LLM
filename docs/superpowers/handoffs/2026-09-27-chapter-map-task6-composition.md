# Task 6: Chapter Progression Composition

Date: 2026-09-27

Branch: `codex/chapter-map-task3`

## Governing contracts

The Framework programmer guide and architecture govern assembly boundaries, module registration, `InitData`, and `PostBindService`. The chapter design revision 5 and the Task 5 handoff govern progression and the stage-entry gate. Tasks 1 and 2 remain archived and are not production dependencies.

## Implemented boundary

- `ChapterProgressionCommands` is a stateless Core.Application entry point over Contracts and existing Domain rules. Chapter entry rejects an active unfinished different chapter and a missing graph. Click and outcome evaluation return effects without owning progression state.
- `ChapterProgressionCoordinator` is a Systems CoreGroup service and lifecycle module. Its `PostBindService` obtains the published Current, Stage, and Map sinks from the root Buffer group. The Core and InGame parent groups forward that lifecycle phase.
- `EnterChapter` reads the registered map and graph. If the graph is absent, it invokes the existing `StageGeneratorSystem.Generate(mapKey)` after the normal `InitData` and `PostBindService` phases, then reevaluates the command. An accepted entry calls `CurrentProgressWriteSink.SelectMap`. It reuses an existing graph and never clears the graph or its clear flags.
- `EvaluateNodeClick` copies graph facts from Stage Buffer, derives the previous node from Current Buffer, and evaluates the Core rules. It is evaluation only. The result's `SelectStage` flag is not applied because the current stage handlers only validate data; there is no confirmed playable entry success boundary. This method must not be exposed as a completed stage-entry command to View.
- Systems has an explicit reference to Core.Application and no direct Core.Domain reference. No Framework, Buffer, Job, or View implementation was changed.

## Verification

- Focused managed Core suite: 20 passed, 0 failed, including three new Application command tests.
- Unity 6000.0.73f1 EditMode `FrameworkRuntimeCompositionTests`: 7 passed, 0 failed. The new tests establish real service/module binding, pre-registered graph reuse, initial `SelectMap`, and evaluation without a premature CurrentStage write. The Unity result is a composition check, not a playable stage test.
- Follow-up Unity 6000.0.73f1 EditMode `ChapterGraphPreparationTests`: 4 passed, 0 failed. In-memory ScriptableObjects feed the real StageMap/Stage tables, DB query groups, Core factory, Buffer factory, and FrameworkRuntime `InitData`/`PostBindService` phases. The tests call `EnterChapter` with an initially empty Stage Buffer. Nearest, Random, and WeightedRandom each produce a connected next-depth graph with the supplied regular and boss stage keys, then select the map. Re-entry preserves graph IDs, stage clear state, and CurrentStage. Missing boss data fails before graph registration or Current selection. Test assets are destroyed after each case; no production asset or implementation was changed.
- This closes the test-data DB-to-generator-to-Buffer verification gap. It does not verify production asset loading, exact probability distributions, or playable stage dispatch. The active branch's `StageMapChapter1.asset` references script GUID `b0caa440360593b4c8de8ed7df98d038`; current `StageMapDataSO.cs.meta` is `b1f720117a064279a50cd6163093fd2b`. Commit `39b634f` on the separate DB-SO branch contains a matching `StageMap_Chapter1.asset`. That data work is not merged into this branch.

## Task 7/8 gate

The Stage flow owner must define confirmed entry success before the coordinator dispatches a click and writes `SelectStage`. View routing, Jobs, outcome writes, attempt end, boss classification from the DB-SO Buffer data, and the boss Stage/Map two-write review remain for Task 7. Task 8 must verify the production path and cut over only verified callers.

The plan ends with one Buffer-authoritative chapter/map gameplay path. It does not integrate the separate stateful `RunProgressionService` (`RunProgressState`/`StageMapSession`) or its combat, resource, Run ID, restart, or history behavior. Existing Domain navigation rules and the Systems Buffer and generator services are reused; the legacy Run service is not called by this chapter coordinator.
