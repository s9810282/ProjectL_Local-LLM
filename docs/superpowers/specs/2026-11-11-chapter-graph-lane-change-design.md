# Chapter Graph Lane Change — Design and Plan (2026-11-11)

Status: rule chosen by the user on 2026-11-11 ("10% + 맵당 1 보장"). P1–P9 were approved as written ("그대로 진행"). Phases 1 and 2 are implemented and pass Core/CoreIntegration in batch mode; see "Implementation record". Not committed. Unity visual check of the stage map is pending.

## Problem

Players often cannot switch to another lane after their first choice: the path to the boss is effectively a straight line.

Cause (code-confirmed):

- Chapter 1 and 2 use `connectionAlgorithm: Nearest` with `additionalConnectionChance: 0.25`.
- The extra connection picks its target through `Select`, which in Nearest mode always returns the nearest node. That node is already connected, so `Add` drops it as a duplicate (`ChapterGraphGenerator.Connect`, third loop). The effective chance is 0%.
- With Nearest only, 2→2 and 3→3 layer gaps are parallel lines, 3→2 only merges, and the only fork is the orphan fill in a 2→3 gap.

Measured with the Chapter 1 shape (depth 6, width 2–3, single first layer, 200,000 runs; script in the session scratchpad):

| Rule | Straight maps | Forks per run | Fork-node ratio | Paths |
|---|---|---|---|---|
| Current | 50.0% | 0.23 | 10% | 3.0 |
| Per-node 10% | 30.6% | 0.38 | 18% | 3.45 |
| **Per-node 10% + at least 1 fork per map** (chosen) | **0%** | 0.50 | 24% | 3.78 |
| One fork per layer gap | 0% | 0.80 | 40% | 4.9 |

"Straight map": after picking at depth 2 there is no further choice on any path. "Forks per run": average forks met after depth 2 on a random walk.

## Chosen rule

1. **Extra connection (per node, chance p):** each node may gain one more edge to a neighbouring node in the next layer. Chapter 1/2 use p = 0.1 (existing `additionalConnectionChance` field).
2. **Minimum forks per map (N):** after all layers are connected, if fewer than N fork nodes exist, add extra edges by the same rule until N is reached. Chapter 1/2 use N = 1 (new field).

## Proposals (confirm before implementation)

- **P1 — Target:** the extra edge goes to a neighbour (width ±1) of the source's single current target. This applies to all three connection modes; `Select` is no longer used for extra edges. Base edges keep their current algorithm.
- **P2 — Cap:** only a source with exactly one outgoing edge may gain an extra edge, so an extra edge never makes more than two. Base and orphan-fill edges are unchanged (a single-node layer still fans out to every node).
- **P3 — No crossing:** an edge (a→b) crosses (c→d) when `a<c && b>d` or `a>c && b<d`. A candidate that crosses any edge already in that gap, or duplicates one, is rejected. If both neighbours remain, `random.Next(2)` picks one; if none remains, the source is skipped. Because Nearest base edges are monotone, Nearest maps never cross. Random/WeightedRandom base edges can already cross; that stays out of scope.
- **P4 — Fork node:** a node in a layer with two or more nodes that has two or more next nodes. Natural 2→3 splits count. A single-node layer's fan-out does not count: everyone passes through it, so it offers no lane change.
- **P5 — Guarantee placement:** runs once after every gap is connected, using the topology RNG. Candidates are all (source, target) pairs allowed by P1–P3 across the whole map, where the source sits in a layer of two or more nodes. One candidate is drawn uniformly, added, and the step repeats until N is reached or no candidate remains. If the shape cannot host N forks (for example width 1 everywhere), generation still succeeds with fewer forks; there is no exception.
- **P6 — Determinism:** the extra roll is still consumed for every source when the next layer has more than one node, exactly as today. The side choice consumes RNG only on success with two valid neighbours. Consequences:
  - p = 0 and N = 0 reproduce today's graphs in every mode at p = 0, and today's Nearest graphs at any p (they had no extra edges).
  - Changing N never changes base or extra edges: the guarantee runs after them.
  - Topology still never reads pools or kinds, so pins, weights, minimums and start depths keep not affecting topology.
  - Different p values in Random/WeightedRandom change later base edges, as `Select` consumption already did.
- **P7 — Authoring:**
  - `StageMapDataSO`: new `minimumBranchCount` (`Min(0)`, Korean tooltip). Its initializer stays 0 so an omitted field reads as off (same convention as `reduceConsecutive`). `Reset` sets new assets to 1 and `additionalConnectionChance` to 0.1. `additionalConnectionChance` gets a tooltip explaining neighbour, two-edge cap and no crossing; its initializer becomes 0.1.
  - `StageMapData`: required parameter right after `additionalConnectionChance`; `Validate` rejects negatives. The obsolete constructor passes 0.
  - `ChapterGraphSpec`: trailing optional `minimumBranchCount = 0` (same style as `pin` and `firstStageKey`); negative throws `ArgumentOutOfRangeException`. The editor tool `CombatStageSoChapter` only builds the spec for validation and needs no change.
  - `StageGeneratorSystem.PrepareGraph` passes `map.MinimumBranchCount`.
- **P8 — Production values:** `StageMap_Chapter1` and `StageMap_Chapter2` set `additionalConnectionChance` from 0.25 to 0.1 and `minimumBranchCount` to 1. Connection mode stays Nearest.
- **P9 — Golden tests:**
  - The 8 Nearest cases switch their spec to p = 0. The expected strings stay unchanged, which proves old maps are preserved when extras are off.
  - The 16 Random/WeightedRandom cases and the pinned case keep p = 0.25 and are re-recorded from the first test run (NUnit prints the actual string).
  - A few Chapter 1-shape cases with p = 0.1 and N = 1 are added and recorded the same way, as a regression snapshot of the new rule.

## Change impact

| Area | Change |
|---|---|
| `ChapterGraphGenerator` | `Connect` extra loop uses P1–P3; new private guarantee step after all gaps; private helpers for fork count and crossing |
| `ChapterGraphSpec` | `MinimumBranchCount` property and optional parameter |
| `StageMapData` / `StageMapDataSO` | New field, validation, `Reset`, tooltips |
| `StageGeneratorSystem` | One argument |
| Chapter 1/2 map assets | Two values each |
| Tests (Core, CoreIntegration) | Golden update, new connection tests, `ChapterDistributionTestData` constructor call, authoring and production-asset checks |
| Editor tool, Buffer, Job, View | No code change. The View draws more edges from the same `NextNodeIds`; a Unity visual check of the map lines is needed |

Unused contract `StageMapGenerationConfig` also carries `AdditionalConnectionChance`. It has no callers and is left alone.

SSOT: p and N each live in one SO field and flow SO → Data → Spec unchanged. The rule itself lives only in the generator. SRP: generation stays a pure calculation; installation and presentation are unchanged.

## Verification

Core (`ProjectL.Tests.Core`):

1. Nearest extras, seeds × widths 2–3 and 6: the graph with p > 0 is a superset of the p = 0 graph. Each added edge comes from a source that had one outgoing edge, goes to a neighbour of that target, is at most one per source, and no gap has crossing edges.
2. Guarantee, Chapter 1 shape at p = 0: with N = 1 every seed has at least one fork node, and some seeds have none at N = 0. The N = k graph is a superset of the N = 0 graph and adds `max(0, k − forks at N = 0)` edges when feasible.
3. Topology independence with N = 1: different pools give the same topology.
4. Infeasible shape (width 1, N = 3): no exception and no fork node.
5. Spec rejects a negative N.
6. Golden cases per P9; existing invariant, pin, distribution and repeat-reduction tests stay green.

CoreIntegration (`ProjectL.Tests.CoreIntegration`):

7. SO → Data → `PrepareGraph` carries N; `Validate` rejects negatives; a new SO (`Reset`) defaults to N = 1 and p = 0.1.
8. Production assets: Chapter 1/2 have p = 0.1 and N = 1 and pass `Validate`.

Unity (user): the stage map shows the extra lines without overlaps on a few runs.

## Plan

- **Phase 1 — Code and rule tests:** generator, spec, data, SO (fields, `Reset`, tooltips), generator system, test-data constructor call, tests 1–5 and 7. Golden expectations are not edited yet. Run Core + CoreIntegration. The expected failures are only the golden cases, and their actual strings are collected from the results. Stop and report.
- **Phase 2 — Golden and production values:** apply P9 using the collected strings and the p = 0 switch, edit the two map assets (only while no test run is in progress), and add test 8. Run Core + CoreIntegration again. Stop and report; the user checks the map in Unity and commits.

Test runs: if the Unity editor is closed, Claude runs batch mode in the background. Otherwise the user runs Test Runner and Claude reads `TestResults.xml`.

## Implementation record (2026-11-11)

Implemented as designed, with no deviation from P1–P9.

- Generator: the third loop in `Connect` now calls `LaneChangeTargets` (single-edge source, neighbour of its target, no crossing, `random.Next(2)` only when both neighbours are valid). `EnsureForks` runs after every gap inside `BuildTopology`. Helpers: `CountForks`, `Crosses`, `WidthOf`.
- Plumbing: `ChapterGraphSpec.MinimumBranchCount` (trailing optional), `StageMapData.MinimumBranchCount` (required after the chance, `Validate` rejects negatives), `StageMapDataSO.minimumBranchCount` (initializer 0, `Reset` sets 1 and chance 0.1, tooltips on both fields), `StageGeneratorSystem.PrepareGraph`.
- Missed in the design's impact table and found by the first compile: three target-typed `new(...)` calls of the `StageMapData` constructor — `TemporaryCoreDatabase.cs:100` (dev database), `ChapterStageDistributionAuthoringTests.cs:191` and `CoreIntegrationTestData.cs:64`. Each passes 0, so behaviour is unchanged.
- Production assets: `StageMap_Chapter1` and `StageMap_Chapter2` now have `additionalConnectionChance: 0.1` and `minimumBranchCount: 1`.
- Golden tests: failures now print the full actual string. The 8 Nearest cases at p = 0 kept their original strings and pass, which confirms old Nearest maps are preserved when extras are off. 15 Random/WeightedRandom/pinned cases changed and were re-recorded; 10 unchanged. 4 new Chapter-settings cases (Nearest, p = 0.1, N = 1, seeds 0–3) were recorded.
- New tests: `ChapterGraphLaneChangeTests` (Core, 10 cases) and `ChapterGraphLaneChangeAuthoringTests` plus `ChapterGraphLaneChangeProductionAssetTests` (CoreIntegration, 6 cases).

Verification (batch mode, `ProjectL.Tests.Core` + `ProjectL.Tests.CoreIntegration`):

- Phase 1 run: Core 5563 total, 16 failed (1 pre-existing + 15 golden as expected); CoreIntegration 1070 total, 20 failed (all pre-existing).
- Phase 2 run: Core 5567 total, 1 failed; CoreIntegration 1072 total, 20 failed. The 21 failures match the 2026-11-11 00:15 Test Runner baseline exactly, and all 180 chapter graph/stage tests pass.
- Pre-existing failures are unrelated: Taunt activation, deck main-change policy, camera zoom pose, structure inspection, production DB assets, run-gold facilities.

Not verified: the Unity stage map presentation of the extra lines (user check).
