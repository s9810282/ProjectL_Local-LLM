# Task 3: Chapter Navigation Rule Boundary

Date: 2026-09-24
Branch: local `codex/chapter-map-task3`, based on `ac3e3e35`.

## Implemented

- Added `Contracts/StageMap/ChapterNodeProgress.cs`: a command-local copy of one Stage Buffer node's ID, stage key, depth, clear flag, and outgoing edges. It contains no Run ID, revision, mutable progress store, stage name, or execution kind. Its outgoing edges are copied and exposed read-only.
- Added `Contracts/Results/ChapterSelectionResult.cs`: an accepted/rejected rule decision carrying the target node/key only when accepted.
- Added `Domain/StageMap/ChapterNavigationRules.cs`: a stateless evaluator of requested/current/graph chapter keys, a coordinator-supplied source node, map completion, graph edges, current-stage completion, and target-stage completion. An empty source ID means the graph's start sentinel. It never writes Buffer or Core state.
- Added `Assets/Tests/EditMode/ChapterNavigationRulesTests.cs` with focused chapter and navigation cases and Unity `.meta` files.

No existing production C# file was modified. The old `StageMapRunState` remains because `StageMapSession` and `RunProgressionService` still call it in their standalone path. This branch does not include the earlier Task 1/2 commits. The rule is preparation for an external coordinator, not a live Buffer integration or a new View snapshot source.

## Contract mapping

`CurrentProgressBuffer.CurrentMapKey` supplies the current chapter; a View command supplies the requested chapter/node. The coordinator obtains a graph with `StageProgressBuffer.GetStages(chapterKey)` and translates each `StageProgressData` into `ChapterNodeProgress`. The key used for that graph query must be passed as `graphChapterKey`. `MapProgressBuffer.GetMap(chapterKey).IsClear` supplies chapter completion. The rule accepts node IDs, stage keys, edges, and clear flags without needing names or combat kinds. It checks node identity rather than stage-key uniqueness, so repeated stage keys remain valid.

The rule takes `fromNodeId` as a command-local input. **Owner decision still required:** does `CurrentProgressBuffer.CurrentStage` mean a highlighted choice, committed entry, or both? The coordinator must choose the authoritative source node only after that meaning is confirmed. If there is no selected/entered stage, it passes `StageNodeId.Empty` and the rule uses the graph start sentinel. Task 3 adds no Buffer field or reset API to guess this answer.

The coordinator/Job composition, actual Buffer writes, stage name/kind display needs, chapter-entry retention, and multi-Buffer failure handling remain Tasks 5–8 and owner-controlled work. Systems cannot directly reference Core.Domain under the current asmdefs; the owner-approved outer caller or Contracts seam must invoke this calculation without changing that dependency rule.

## Verification

Using Unity 6000.0.73f1's bundled Roslyn compiler and Mono runtime, the three Core assemblies and Core test assembly compiled. Focused managed NUnit fixtures `ChapterNavigationRulesTests`, `StageMapRunStateTests`, `StageMapSessionTests`, and `StageMapGeneratorTests` returned **35 passed, 0 failed, 0 skipped, 0 inconclusive**: six new cases and 29 existing map cases. Evidence: `C:/CodexHome/visualizations/2026/09/22/01a0c8a7-306f-7390-bbe5-67d9adb08dd3/chapter-map-task3/task3-results.xml`. This was not a Unity Editor/EditMode execution and did not exercise real Buffers, Jobs, View, or runtime bootstrap. No other tests were run for this Task 3 change.
