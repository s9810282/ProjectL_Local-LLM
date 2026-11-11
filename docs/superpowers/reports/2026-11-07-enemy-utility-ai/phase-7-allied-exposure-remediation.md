# Phase 7 candidate-scoped allied exposure remediation

The user authorized item 1 from the Phase 7 handoff on 2026-11-08. The source remains in the existing uncommitted `feature/Cheon/Core/NewCombatSytem` checkout at `9d8c9ce456db95e8e0aff766240af47353f18f46`. The Phase 7 acceptance report and raw before evidence are retained.

## Candidate impact and coverage

- `CombatActionAssessment` admits the candidate before assessing its coverage. Ordinary attacks with no Pull or Knockback do not collect unrelated movement-entry gaps. Possible displacement with unresolved landing effects retains the existing conservative fallback. Supported precise results record the actual observed allied loss as their bound.
- `MovementEnvironmentCalculator.RouteAssessmentCoverageSteps` checks planned entry cells and the swap passenger's arrival. An encountered native mine uses the existing `MineMovementTileRule.Cells` blast footprint. The allied bound sums remaining HP for exposed occupants and relevant entrants once per entity. A visible mine off the route does not charge unrelated allies or block a deterministic observed projection.
- `TornadoCalculator.AssessmentEntryContacts` uses current public member centers and the same footprint contact predicate as actual execution. Immunity suppresses throw risk. Contact by a susceptible entrant retains RandomOutcome and UnknownPosition without reading the RNG. Possible landings carry exposed entrants and relevant visible blast recipients. Unknown callbacks reachable through the candidate or its possible landing keep the broad fallback.
- `PiercingFalloffEffectRule` declares its deterministic coverage through the existing effect capability. The test compares observed and actual consequences. Unclassified world effects keep the conservative fallback where their recipients cannot be established. `HiddenEnvironment`, future hazard evaluation, scorer weights, budget, actual execution, and public service composition retain their existing contracts.

The modified responsibility is assessment provenance and scope. Route and hazard owners still define where an entry or blast can reach. The scorer consumes the resulting bound and keeps its established weighting. No new source of combat state or duplicate damage formula was introduced. The read-only review checked unbounded chains, entry and swap scope, RNG, and budget-only facts. Its budget-only concern was resolved by tracing the existing coarse score path. The review found no new high-impact issue.

## Reproduction and verification

The first test setup failed to compile due to an omitted namespace and contributes no behavior evidence. `phase-7-scope-behavior-red` then reproduced six failures. A8/X8/H8 ended immediately, a distant mine blocked a deterministic move, the entered-mine case charged 4,000 instead of 2,000 permille, and piercing failed to obtain precise observed assessment.

The final focused run passed 209/209. It includes nine new cases, which cover all fifteen affected fixture-seed inputs, partitions 1/2/31, RNG cursor variants, route-local mines, stationary attacks, genuine tornado contact and observed piercing parity. Every selected first action was accepted by the calculator without immediate allied HP loss in those frozen states.

| Fixture | Phase 7 before | Remediation first action | Seeds | Sides with an accepted action before End |
|---|---|---|---:|---:|
| A8 | Immediate End after Hold | Actor 11 moves from (5,1) to (5,0) | 5/5 | 20/20 |
| X8 | Immediate End after Hold | Actor 11 moves from (5,1) to (5,0) | 5/5 | 20/20 |
| H8 | Immediate End after Hold | Actor 13 attacks (4,0) | 5/5 | 20/20 |

`phase-7-scope-corpus` passed with 55 identical frozen input hashes, 1,100 warmed decisions and 220 sides. It records accepted Core transitions. X8 records the Tornado entry event on the chosen move. The eight other fixtures, including the standard N5, retained their complete recorded decision traces across five seeds. The before/after selections are in `phase-7-scope-corpus/scope-summary.json`.

| Assembly | Selected | Passed | Failed | Skipped |
|---|---:|---:|---:|---:|
| ProjectL.Tests.Core | 5,238 | 5,205 | 25 | 8 |
| ProjectL.Tests.CoreIntegration | 1,016 | 967 | 47 | 2 |

All 72 failed full names, labels and messages match Phase 0. The nine added cases passed and the frozen Preview, route and immediate baseline decision tests passed. Exact XML comparison is in `phase-7-scope-integration/regression-comparison.json`. The corpus, focused and both full manifests match all 118 changed C# files in the checkout. The source delta from Phase 7 is four Core files and one new test with Unity metadata. The Unity-generated FMOD and font files were verified against known generated hashes, backed up and restored.

## Remaining acceptance

This delivery resolves the excessive Hold behavior on the three named frozen fixtures. It does not establish universal coverage for unclassified effects. The G-atom and service spike investigations, calibrated allocation, real-scene profiling and Unity Flow F1–F8 remain open. The diagnostics in this follow-up verify behavior and Core regression. The activation configuration continues to select immediate Advance.
