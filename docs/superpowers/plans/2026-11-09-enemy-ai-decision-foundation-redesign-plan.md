# Enemy AI Decision Foundation Redesign — Implementation Plan

> Steps use checkbox syntax. Stop at every phase boundary and report; the next phase needs the user's go-ahead. This document does not itself authorize implementation.

**Goal:** Make enemies engage purposefully on every stage shape and with real loadouts: no whole-side idling from unmodelled sources, multi-target route-aware engagement, move-then-attack valued without full projection, brave-but-not-suicidal exchange scoring with designer-tunable values, beam candidate selection, and a production-stage behaviour corpus as acceptance.

**Spec:** [Enemy AI decision foundation redesign](../specs/2026-11-09-enemy-ai-decision-foundation-redesign-design.md). All decisions Q-R1–Q-R13 are settled (Q-R1/Q-R2 by user direction, the rest as recommended).

**Tech stack:** Unity 6000.0.73f1; pure C# Core (`ProjectL.Core.Application`); EditMode assemblies `ProjectL.Tests.Core` and `ProjectL.Tests.CoreIntegration` only.

**Status:** Plan written 2026-11-09. R-0 done and pass thresholds confirmed (2026-11-10, Q-R8). R-1, R-2 and R-3 done (2026-11-10). R-4 done (2026-11-10; actor order changed to attack-ready first by user decision). R-5 done (2026-11-10). R-6 done (2026-11-10): threshold 3 missed and the threshold 6 refinement borderline (see the R-6 record). R-6 follow-up (2026-11-10, user decisions): threshold 3 judged by the AI's own route cost and passes at 1.91%; goal-set cache removed; threshold 6 refinement judged by the median of five runs on the final code and passes (+7.2%). Long-update investigation (2026-11-10): every update over 4 ms is a GC pause; the threshold 6 refinement is now the structural Step limit (≤ 0.1 ms, measured 0.048 ms; user decision). Every threshold passes; waiting for the user's manual Unity check and commit. R-3 to R-6 revised after an external review (2026-11-10, review revision record at the end of R-6); the three threshold refinements there were confirmed by the user the same day.

## Prefixes

- `C/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/`
- `A/` = `C/Application/Combat/`
- `E/` = `A/EnemyAi/`
- `S/` = `Assets/ProjectL/Script/Systems/`
- `T/` = `Assets/Tests/EditMode/`

## Global constraints

- Preserve actual combat rules, admission, taunt obligations, hazard forecast ownership, one action per publication, prepared/synchronous publication parity, cancellation/staleness, Buffer and event schemas, and the immediate-compatibility path (`SelectImmediate`, `CombatCommandCalculator.Advance`).
- Determinism: identical input → identical decision for Step partitions 1, 2 and 31.
- No View, Flow, Plan, Job or Framework surface changes; no new services or Factory registrations except the optional DB table wiring of R-3 (connection lines only; if a ServiceKey changes, record it for the Framework owner).
- Public API changes allowed only as listed in the spec §7: `moveCandidateLimit` default 4 → 6; three optional tuning parameters on `EnemyDecisionSettings`; the side-cache handle and one session constructor overload; `InternalsVisibleTo("ProjectL.Tests.CoreIntegration")` on Core.Application.
- One new InGame DB config table (R-3), following CLAUDE.md DB rules P1–P7; the table asset and `InGameGroup` registration land in the same change as the code.
- Tests: Core/CoreIntegration only (no View tests). Every intentional expectation change in an existing test is listed in the phase record with its reason. The enemy AI tests are authored by the user (GwonCheonmyeong); tests by other workers are not changed without asking.
- New code comments in Korean; documents in English; every new `.cs` gets a `.meta`.
- Unity batch runs only while the Editor is closed. After every run restore Unity side-effect files (`Assets/Audio/FMODplus/New Fmod Event Catalog.asset`, `Assets/Plugins/FMOD/Cache/Editor/FMODStudioCache.asset`, the two `Assets/ProjectL/Resources/Font/*SDF.asset`, `ProjectSettings/Packages/com.unity.sequences/Settings.json`) after showing their diff.
- The user commits. Claude does not commit or push unless told to.

## Entry condition (before R-0)

Current uncommitted work becomes the baseline (Q-R13). The user commits, or tells Claude to proceed on top of it:

- `E/EnemyActionCandidates.cs` (truncation fallback), `E/EnemyEngagementMap.cs` (cap 289), `T/EnemyEngagementProgressTests.cs`
- `T/CoreIntegration/EnemyEngagementFollowupPublicationTests.cs` (+ `.meta`)
- `T/CoreIntegration/Stage1127EnemyHoldDiagnostics.cs` (+ `.meta`), `T/EnemyArtifactHoldDiagnostics.cs` (+ `.meta`) — temporary diagnostics, folded into the corpus in R-0
- `docs/superpowers/plans/2026-11-09-enemy-ai-engagement-followup-plan.md`, `docs/superpowers/specs/2026-11-09-enemy-ai-engagement-followup-design.md`, the new spec and this plan

## Phase R-0 — Baseline and production-stage corpus

**Files:** create `T/CoreIntegration/EnemyStageBehaviorCorpus.cs` (+ `.meta`); modify `C/Application/Properties/AssemblyInfo.cs` (add `InternalsVisibleTo("ProjectL.Tests.CoreIntegration")`, Q-R11); remove the two temporary diagnostics once the corpus covers them.

- [ ] Record HEAD, status and the entry-condition state.
- [ ] Add the `InternalsVisibleTo` line so the corpus calls internals directly (no reflection).
- [ ] Build the corpus (explicit measurement test plus a small pinned set added in later phases):
  - Stage list: every production combat stage whose key is in the LD series, plus current normal and boss combat stages, read from the production DB (`DataBaseFactory.Create(DatabaseCatalogSO)`), as the stage 1127 diagnostic does.
  - Runtime: test DB roster/deck (`CoreIntegrationTestData`) plus the production stage definition through a test-owned `IBattleInitializationSource`; deployments chosen deterministically from the stage's legal deployment cells (front-most first, then spread variant).
  - Player scripts: hold back; advance on round 1 (each unit takes its longest legal step toward the nearest enemy); spread. 4–6 rounds.
  - Loadouts: none; one placed artifact (install through the artifact Buffer store with a test-owned helper; locate the store API as the first step of this task); one equipped item via the existing item commands; rosters with undeclared traits where the stage has them (boss, CactusCannon).
  - Metrics per stage × script × loadout: idle ratio (enemies with a reachable attack position that hold), retreats (moves that increase route distance to every player unit with no hazard reason; test-owned BFS over static terrain, allies passable), first contact round, enemy deaths by hazard and by player, published action counts, `EnemyDecisionDiagnostics` work/preview counters, decision latency.
  - Output: TSV to `PROJECTL_ENEMY_AI_RAW`, plus a summary in the test output.
- [ ] Run full `ProjectL.Tests.Core` and `ProjectL.Tests.CoreIntegration`; compare with the known 64 failures by full name and message.
- [ ] Run the corpus and the existing measurement captures (`phase-6-corpus`, `phase-6-atoms`, `phase-7-service`) at this baseline.
- [x] **Stop. Report baseline metrics (with and without artifact) and propose pass thresholds; the user sets them (Q-R8).** Confirmed as proposed (completion record R-0).

## Phase R-1 — AI-ignored sources and obstacle scoping (spec §6.2)

**Files:** modify `A/CombatCommandEffects.cs` (enemy-assessment mode), `A/CombatActionAssessment.cs` (use the mode; obstacle scoping), `E/EnemyThreatField.cs` (same mode for its coverage pass); create `T/EnemyIgnoredSourceTests.cs` (+ `.meta`); update `T/EnemyFollowupEstimateTests.cs`, `T/EnemyEffectHarmScopeTests.cs` where expectations change by intent.

- [x] RED first: add the tests below and run them to see the expected failures.

| Test | Asserts |
|---|---|
| `OneArtifactDoesNotChangeEnemyDecisions` | For every benchmark fixture and seed, the first decision with one placed artifact equals the decision without it; one artifact never idles a side that acts without it |
| `ItemsHeldEnvironmentAndUndeclaredRulesAreIgnored` | In enemy-assessment mode these sources yield no gap, no harm callback, no projection block; real-combat mode yields exactly the old gaps |
| `ObstacleCountsOnlyWhenCandidateCanTriggerIt` | An attack whose delivery hits a powder barrel counts allies inside `CombatHazardAreas.PowderBarrelCells`; an attack or move that cannot touch the obstacle gets no ceiling from it |
| `DeclaredRulesAndRouteCoverageUnchanged` | Effects with declarations and route-local environment coverage keep their current gaps and harm |
| `KnockbackConditionUnchanged` | Undeclared knockback-type rules still count only for the acting attack |

- [x] Add the enemy-assessment mode to `CombatCommandEffects.AssessmentCoverageSteps` (an explicit parameter; default = current behaviour). Skip: placed artifacts, equipped/temporary items, held environment, effect rules without an assessment declaration (knockback keeps its rule).
- [x] Obstacles: in `CombatActionAssessment`, remove the blanket obstacle ceiling for enemy assessment; when the candidate's delivery cells include an obstacle, add only allies inside that obstacle's reaction area from `CombatHazardAreas`.
- [x] Use the same mode in `EnemyThreatField` so its gaps follow the same policy.
- [x] Update `EnemyFollowupEstimateTests.MixedSourcesKeepProjectionBlocked` (Artifact/Item/Held/Unknown/UndeclaredHarmless now unblocked; Obstacle scoped) and any harm-scope expectations; list each change.
- [x] GREEN: new tests, then the enemy decision suites (`EnemyActionAssessmentTests`, `EnemyAlliedExposureTests`, `EnemyEffectHarmScopeTests`, `EnemyFollowupEstimateTests`, `EnemyUtilityDecisionTests`, `EnemyUtilityScoreTests`, `EnemyPreviewPreparationTests`), then the full two assemblies.
- [x] Corpus with and without artifact; record deltas.
- [x] **Stop and report.**

## Phase R-2 — Engagement field (spec §6.3)

**Files:** create `E/EnemyEngagementField.cs`, `E/EnemyAiTuning.cs` (constants start here: ally soft cost 2) (+ `.meta`); modify `E/EnemyAttackReach.cs` (take `ObservedGeometry`, reuse one relocated input per cell), `E/EnemyActionCandidates.cs` (field instead of single-target map; remove the Manhattan pursuit target except the truncation fallback), `E/EnemyDecisionContext.cs` (per-decision goal-set cache by `(profile, range, occlusion, Leap)`); remove `E/EnemyEngagementMap.cs` (+ `.meta`) once no reference remains; migrate `T/EnemyEngagementProgressTests.cs` to the field; create `T/EnemyEngagementFieldTests.cs` (+ `.meta`).

- [x] RED tests:

| Test | Asserts |
|---|---|
| `NearestByRouteNotStraightLine` | Manhattan-nearest target behind walls loses to a farther target with a shorter route |
| `HaboobUnattackableTargetIsSkipped` | Target inside the cloud with an outside attacker: field prefers a reachable outside target; no detour around the map |
| `AlliedUnitsAreSoftHostileAreHard` | A corridor blocked by an ally gives a finite cost (+2 per allied cell); blocked by a player unit gives no route through it |
| `GoalsMatchAttackReach` | For melee, ranged, directional, splash, pull bonus, destination range modifier and occlusion, goal cells equal the shared reach predicate (port of `AttackGeometryDefinesGoals`); pierce falls back to a full scan |
| `TargetValueAndHeadquartersOrder` | One-attack loss + elimination weight when lethal; HQ only when no unit goal is reachable |
| `TauntRestrictsFieldToSource` | With an obligation, only the required source is a target |
| `CapsTruncationAndPartitions` | 289-anchor component completes; 290 truncates and falls back to representative pursuit progress; partitions 1/2/31 identical; mine/Leap/collapse eligibility unchanged |

- [x] CoreIntegration pinned (RED now, GREEN after this phase): stage 1127 advanced-player script, round 3 — the Demon does not move away from every player while another target is reachable; round 1 — the Heavy behind allies has at least one move with positive progress.
- [x] Implement the field: targets, goal cells via `EnemyAttackReach` bounded like `EnumerateAimsThatCanHit`, integer-cost multi-source reverse search (bucket queue), per-cell `RemainingCost`/`BestTarget`/`IsAttackPosition`/`TargetValue`, yields at every expansion and reach substep.
- [x] Refactor `EnemyAttackReach` to shared geometry; keep `EnemyPursuitSearch.CanAttackFrom` behaviour (run `EnemyPursuitEquivalenceTests`, `EnemyPursuitResumptionTests`).
- [x] Replace the map in candidates: destination distance and `Progress` from the field; truncation fallback unchanged.
- [x] GREEN: new tests, migrated engagement tests, pursuit/candidate/decision suites, full assemblies.
- [x] Measure `phase-6-atoms` and `phase-6-corpus`; apply the stop rule (first-decision work > 1.5× baseline → stop and report).
- [x] **Stop and report.**

## Phase R-3 — Geometric follow-up, exchange model, tuning asset (spec §6.4, §6.5, §6.5.1)

**Files:**
- Create `E/EnemyFollowupGeometry.cs` (+ `.meta`).
- Movement-side moved-actor helper: first locate where completion sets per-turn movement facts (`ActionUse.Moved`, the state `StationaryFireEffectRule` reads as `MovedThisOwnTurn`) and expose one internal helper there that completion and AI both use. Location decided in this task and recorded.
- Modify `E/EnemyUtilityScorer.cs` (new terms, `PlayerRisk`), `E/EnemyDecisionSession.cs` (calls the lethal-hazard predicate for Hold and assessed candidates, follow-up wiring, settings, Hold reason), `E/EnemyActionCandidates.cs` (owns the one lethal-hazard predicate; the BFS applies it where a cell is evaluated for landing, never to traversal), `E/EnemyDecisionDiagnostics.cs` (minimal Hold reason code, moved forward from R-5), `E/EnemyAiTuning.cs` (defaults), `C/Contracts/Combat/EnemyAi/EnemyDecisionSettings.cs` (three optional parameters with validation: `deathTradeThresholdPercent` 50, `threatAversionPercent` 50, `nextTurnApproachPercent` 50).
- DB (same change): `EnemyAiTuningData` (pure C#, `Validate`), `EnemyAiTuningDataSO` (input layer, Korean inspector labels), `EnemyAiTuningDataTableSO`, `EnemyAiTuningTable` + optional query interface on `S/DataBaseSystem/Queries/InGameDataBase/InGameDataBase.cs`, optional registration in `S/Factory/DBFactory/Group/InGameDataBaseFactory.cs` (trait-growth optional pattern), one data asset + table asset under `Assets/ProjectL/DataBaseSO/InGameGroup/`, and the `InGameGroup.asset` entry.
- `S/RuntimeSystem/InGameSystem/CoreSystem/CombatSystem/CombatAdvanceService.cs`: implement `ISystemInitData<IDataBaseQueryContext>`, read the optional table (absent → defaults), pass settings to the session.

**Lethal-hazard filter rules** (review revision 2026-11-10; derived from spec §6.5 and the current code, change only with the user's decision):

1. Arrival and transit are judged separately.
   - Arrival loss: what the actor suffers by standing on the endpoint until its next opportunity. Sources: `StageHazardForecast.Evaluate(...).KnownHpLoss` at the endpoint (railway, rockfall and instant-defeat cells fire at boundaries) and a collapse landing (`CollapsesOnLanding`, from `WeakGroundCalculator.CollapsesOnNextEntry`). Cells only crossed are not arrival: crossing an active railway, a rockfall warning cell or weak ground to a safe endpoint stays a candidate (weak ground collapses at command end with nobody on it; railway and rockfall fire at the boundary).
   - Transit loss: certain damage taken while moving (contact entry damage such as thorns, the numbers `EntryEstimateSteps` uses). Lethal transit excludes that route, not the endpoint: the endpoint stays when the existing refinement (`RefineSteps`) finds a route whose transit loss is below HP.
2. Only certain lethality is filtered: arrival `KnownHpLoss ≥ HP`, collapse landing, certain transit loss ≥ HP. Uncertain loss (`PossibleHpLoss ≥ HP` with `KnownHpLoss < HP`, `RandomOutcome` / `UnknownPosition` gaps, tornado throw) stays a scored hazard penalty.
3. One predicate: the rule lives in one internal function in `EnemyActionCandidates` that reads only owner facts (forecast exposure, weak-ground landing, contact damage). The BFS and the session call it. No hazard-kind switch in AI; a new gimmick is covered once its owner reports it in `StageHazardForecast`.
4. Priority, top first:
   1. Admission and the taunt obligation define the candidate set (existing authority; under an obligation only taunt candidates remain and they beat Hold).
   2. A candidate whose precise projection guarantees an enemy win this action (`GuaranteedTerminal`, outcome Defeat) is not filtered: later boundaries never come (same rule as `PositionFacts` today).
   3. The certain-lethal filter applies to every other candidate, taunt candidates included (today's collapse-landing filter already beats taunt, `EnemyDecisionSession` `coarseCollapseLanding`).
   4. All-lethal exception: only when Hold is certain-lethal too and every remaining candidate is certain-lethal, the least lethal by the existing `HazardRank` is kept; Hold reason `LethalOnly`.
5. Hold reason codes are needed by the corpus from this phase (moved forward from R-5 §6.9, minimal set): `NoReachableTarget`, `FieldTruncated` (the engagement field was truncated, so "search incomplete" is not reported as "no reachable target"), `LethalOnly`, `ExchangeLoss`, `WorldUncertainty`; R-4 adds `Pruned`. One code per held actor-side, recorded in `EnemyDecisionDiagnostics` only when capture is requested. R-5 extends the same record to per-candidate terms.

- [x] Entry check (review revision 2026-11-10): confirm in the R-2 record that the per-decision goal-set cache key covers every attacker fact `EnemyAttackReach` reads: `SelectionRange` inputs (pull bonus, destination range modifiers), attack restriction/admission, common damage against the target's defence (`AppliesDamage`), footprint, and for piercing shapes the other units on the line. If R-2 shares goal sets between actors that differ in one of these, fix that first (or share only the geometric candidate bound and keep the delivery check per actor).
- [x] RED tests (Core unless noted):

| Test | Asserts |
|---|---|
| `AdjacentAttackPositionCreditedWithoutProjection` | With projection blocked by a declared-gap effect, an endpoint adjacent to a target still gets the follow-up value |
| `FollowupRespectsRules` | Disarmed actor → 0; route with arrival gaps → 0; collapse landing → 0; taunt obligation at the endpoint restricts the aim; aim chosen by target value |
| `MovedActorFactsComeFromMovementOwner` | Moved actor from the shared helper has `Moved` and the move flag; a StationaryFire attacker gets no stationary bonus in the follow-up |
| `ExchangeWorkedExamples` | Spec §6.5: move 270 vs Hold 150; lethal poke −380; lethal heavy hit +120; lethal kill accepted |
| `LethalArrivalNeverChosen` | Collapse landing, an active railway cell at the next boundary and a rockfall cell with known loss ≥ HP are not chosen while a non-lethal option (Hold included) exists |
| `SafeCrossingAllowed` | A move that crosses an active railway, rockfall warning or weak-ground cell and ends on a safe cell stays a candidate and can be chosen |
| `LethalTransitRouteExcluded` | A route whose certain contact damage reaches HP is excluded; the same endpoint through a safe route stays |
| `UncertainHazardScoredNotFiltered` | `PossibleHpLoss ≥ HP` with `KnownHpLoss < HP` keeps the candidate, with the hazard penalty |
| `AllOptionsLethalKeepsLeastLethal` | Hold and every candidate certain-lethal: the least lethal by `HazardRank` is chosen and the Hold reason is `LethalOnly` |
| `LethalFilterPriority` | A taunt candidate with a lethal arrival is dropped (another taunt candidate or Hold wins); a guaranteed-win attack by an actor standing on a lethal cell is kept |
| `HoldReasonCodes` | Every held actor-side records one code; a truncated field gives `FieldTruncated`, not `NoReachableTarget` |
| `TuningValuesChangeDecisions` | Changing the death trade or threat aversion flips the controlled decision; invalid settings throw |
| `TuningAssetLoadsAndDefaults` (CoreIntegration) | Production asset values reach the session settings; a DB without the table uses defaults |

- [x] CoreIntegration pinned (RED → GREEN): stage 1127 advanced-player round 2 — the Demon steps next to the Hammer and attacks in the same side; rounds 3–4 — Gravekeeper/Heavy engage when the exchange is favourable.
- [x] Implement follow-up geometry, scorer terms, lethal-hazard filter, settings parameters, tuning table and service wiring. `NextTurnStrike` uses the field's best target only when `StageHazardForecast.CanDesignateAt(cell, target, boundary)` allows it at the next boundary (haboob relocation); add a test case for a target that leaves or enters the cloud at the boundary.
- [x] Update expectations by intent (list each): `EnemyUtilityScoreTests`, `EnemyUtilityDecisionTests`, `EnemyMoveThenAttackTests`, `EnemyFollowupEstimateTests`, `EnemyForecastBindDecisionTests`.
- [x] GREEN: new tests, decision suites, publication suites (`EnemyUtilityPublicationTests`, `EnemyAdvancePreparationTests`, `EnemyDecisionSourceStampTests`, `EnemyAssessmentBoundaryPublicationTests`, `EnemyEngagementFollowupPublicationTests`), `DataBaseAssetTests`, full assemblies.
- [x] Corpus deltas, with the review additions (2026-11-10):
  - Idle split by Hold reason code; `LethalOnly` and `NoReachableTarget` are reported as justified, the rest per stage.
  - Enemy hazard deaths split into avoidable (at that decision a non-lethal option, Hold included, existed by the same predicate) and unavoidable (listed with stage, round and actor). Avoidable deaths must be 0. Deaths are reported next to attacks and first-contact round, so fewer deaths caused only by fewer engagements is not read as an improvement.
  - Engagement-field truncations per stage. Expected 0: the largest production field is 15×15 = 225 cells, below the 289 cap. Any truncation is reported with the stage and the affected actors' Hold reasons.
- [x] **Stop and report.**

## Phase R-4 — Beam selection and actor order (spec §6.6, §6.7)

**Files:** modify `E/EnemyActionCandidates.cs` (numeric cheap score replaces `Destination.Rank`; reservations; remove `Math.Min(4, …)`), `E/EnemyDecisionSession.cs` (remove `Math.Min(2, …)`; readiness-first actor order as a two-pass with the field kept for scoring), `C/Contracts/Combat/EnemyAi/EnemyDecisionSettings.cs` (`moveCandidateLimit` default 6); create `T/EnemyCandidateBeamTests.cs`, `T/EnemyActorOrderTests.cs` (+ `.meta`).

**Reservation rules** (review revision 2026-11-10). `moveCandidateLimit` may be any value ≥ 1, so reservations can outnumber K:

- Fill order: reserved candidates first, then the remaining slots by cheap score (stable ties as today: steps, y, x).
- A candidate reserved for several reasons takes one slot (dedupe by endpoint, as `used` does today).
- The taunt obligation is a rule, not diversity: under an obligation the beam holds only taunt candidates (the session drops the others anyway) and the taunt route's slot is never given up.
- Without an obligation the reservation priority is: hazard escape (today's `escape` condition: lower hazard rank than standing still) → best attack position → best progress endpoint. When reservations exceed K, drop from the lowest priority; with K = 1 only the top reservation survives.
- The priority list and the K handling live in one function in `EnemyActionCandidates`.
- An actor that holds because its only better options were pruned records `Pruned`.

- [x] RED tests:

| Test | Asserts |
|---|---|
| `AttackPositionSurvivesPruning` | An attack position with higher threat than safe cells is in the beam |
| `ReservationsCountInsideBeam` | Taunt route, hazard escape, best attack position, best progress endpoint are present and counted in K |
| `ReservationOverflowFollowsPriority` | K = 1 and K = 2 with three distinct reservations keep the top 1 / 2 by the priority above; a cell reserved twice uses one slot; under an obligation the taunt route is kept and non-taunt reservations are not added |
| `LimitsHonoured` | Default K = 6, R = 2; lower configured limits honoured; no hard-coded caps remain |
| `PruningUsesScorer` | Non-reserved slots follow the scorer's cheap score order with stable ties |
| `ReadinessFirstOrder` | Units that can attack now act first, then by field cost, ties by EntityId; deterministic across partitions |
| `CorridorUnblockedByOrder` | Front unit in a one-cell corridor moves before the unit behind it |

- [x] Implement beam and order; keep the Preview budget rules.
- [x] Update expectations by intent: `EnemyActionCandidateTests`, `EnemyDecisionBudgetTests`, `EnemyDecisionSessionTests`, publication traces that depend on order.
- [x] GREEN: new tests, decision/publication suites, full assemblies; measurements with the stop rule.
- [x] **Stop and report.**

## Phase R-5 — Side-scoped shared-structure reuse and diagnostics record (spec §6.8, §6.9)

**Files:** create `E/EnemyDecisionSideCache.cs` (+ `.meta`; public opaque handle); modify `E/EnemyDecisionSession.cs` (public overload taking the handle), `E/EnemyDecisionContext.cs` (threat field, hazard forecast and goal sets from the cache when the read-set digest matches), `E/EnemyDecisionDiagnostics.cs` (per-candidate record on top of the R-3 Hold reasons, captured on request), `S/.../CombatAdvanceService.cs` (create the handle at enemy side start, drop it at side end or battle change); create `T/EnemyDecisionSideCacheTests.cs` (+ `.meta`).

**Read sets** (review revision 2026-11-10):

- First task: write the read set of each cached structure from its code into the phase record. Each builder owns the digest of exactly what it reads (next to the code that reads it); the cache only stores and compares. Someone who changes a builder's inputs then sees the digest in the same place. No global version scheme.
- Starting points to verify:
  - Threat field: player units' positions and facts (HP, statuses such as disarm, range modifiers, traits and their bindings); **enemy units' positions and facts** (splash centres come from eligible units, piercing encounters stop at units, the coverage pass reads every character's bindings); visible environment, walls, elevation, gimmick state. One enemy acts per publication, so reuse across decisions may be rare: measure the hit rate on the corpus before keeping this cache, and if it does not pay for the digest, report and drop it instead of tuning.
  - Hazard forecast: gimmick states and schedules, boundary counts, positions and HP of the units it evaluates (main, objective target), environment bindings.
  - Goal candidate sets: target footprint and facts; attacker facts read by `EnemyAttackReach` (profile, `SelectionRange` inputs such as pull bonus and destination modifiers, restrictions, common damage against the target's defence, footprint); walls, elevation, haboob state; for piercing shapes the other units on the line.
- Trace comparison scope for `CacheOnOffIdenticalTraces`: published actions in order (kind, actor, aim, path, taunt source), Hold and end decisions, resulting events and state, and the diagnostics record's score terms and Hold reasons. Excluded: work and expansion counters, preview call counts, cache hit counts, timings.

- [x] RED tests:

| Test | Asserts |
|---|---|
| `CacheOnOffIdenticalTraces` | Every benchmark fixture × seed and the corpus stages: full sides with and without the cache produce identical traces in the scope above, partitions 1/2/31 |
| `DigestInvalidation` | For each cache, changing one fact of its read set invalidates it: player HP/death/forced move, player disarm or range change, enemy move or death, gimmick change and visible environment change for the threat field/forecast; target move, terrain change and attacker trait differences (pull bonus, destination modifier, restriction) for goal sets; two actors with the same profile tuple but different traits never share goal sets |
| `CacheScopedToSide` | A new side or battle never reuses entries |
| `DiagnosticsRecordTerms` | Records list every candidate's terms and a reason code for Hold |

- [x] Implement; the cache can be disabled by passing no handle.
- [x] CoreIntegration: prepared service with cache keeps publication parity (existing publication suites + `EnemyEngagementFollowupPublicationTests`).
- [x] Switch the corpus to the diagnostics record.
- [x] Measurements (`phase-6-corpus`, `phase-6-atoms`, `phase-7-service`, corpus); report work and latency against baseline. Also report the per-update (per-frame slice) preparation p99 and max and the count of updates over 4 ms, and the corpus decision latency, grouped by field enemy count (≤ 5, 6–8, 9–10, > 10), measured the same way at the R-0 commit for comparison. The per-frame maximum is reported next to the per-decision total.
- [x] **Stop and report.**

## Phase R-6 — Final verification and documents

- [x] Full `ProjectL.Tests.Core` and `ProjectL.Tests.CoreIntegration`; failures compared with the baseline by name and message.
- [x] Corpus against the thresholds set in R-0; measurements against baseline and the stop rule.
- [x] Update the spec (implementation memo) and this plan's completion record; retire or rewrite `EnemyUtilityAcceptanceMeasurements.CaptureHold` (it encodes the old all-Hold state).
- [x] State in the spec memo what the R-1 artifact checks mean (review revision 2026-11-10): `OneArtifactDoesNotChangeEnemyDecisions` compares inputs whose battle facts are identical and differ only by the ignored source (what the test already does); loadout threshold 1 is the separate "no stall" check. An artifact that has already changed HP, attack or range before the decision may change decisions; that is a legitimate reaction, not a failure of either check.
- [x] Manual Unity checklist for the user (below).
- [x] **Stop. The user commits.**

**Threshold refinements from the review revision (2026-11-10), confirmed by the user on 2026-11-10 (Q-R8 addendum). They replace the matching R-0 thresholds for R-6:**

- Threshold 2 counts as idle only Holds whose reason is not `LethalOnly` or `NoReachableTarget`; the raw idle ratio is reported next to it.
- Threshold 4 becomes "avoidable hazard deaths in the enemy window: 0"; unavoidable deaths (every option, Hold included, certain-lethal) are listed.
- Threshold 6 adds: per-update preparation p99 for the 6–8 and 9–10 enemy groups within 10% of the same groups at the R-0 commit; per-update max reported per group. Judged by the median of at least five runs (user decision in the R-6 follow-up). **Replaced on 2026-11-10 (user decision after the long-update investigation):** the largest structural preparation Step (`phase-7-atoms`, minimum over four identical repeats) is at most 0.1 ms; `CaptureStructuralAtoms` asserts it. The per-update p99, max, GC-affected count and execute frames from `phase-7-service` (`prepared-summary.tsv`) are reported as context and are not a pass condition, because they move with GC pauses and the machine.

**Review revision record (2026-11-10).** An external review of this plan was checked against the code. Adopted into R-3 to R-6: lethal arrival vs transit and certain vs uncertain, priority with taunt / guaranteed win / all-lethal, one lethality predicate, manual checklist aligned with the all-lethal exception (R-3); Hold reason codes and avoidable-death classification moved forward from R-5 (R-3); goal-set key entry check for R-2's cache (R-3); reservation overflow rules (R-4); per-cache read sets owned by each builder, enemy positions in the threat field's read set, trace comparison scope, per-frame slice timing by enemy count (R-5); artifact check wording (R-6). Checked and not needed: the 289 cap cannot trigger on production fields (largest 15×15 = 225 cells), so R-3 only counts truncations; the R-1 artifact test already compares identical battle facts.

## Validation commands

Unity must be closed. Example (absolute output paths in the scratchpad):

```text
"C:\Program Files\Unity\Hub\Editor\6000.0.73f1\Editor\Unity.exe" -batchmode -projectPath D:\Github_LocalFile\Project_L
  -runTests -testPlatform EditMode -assemblyNames "ProjectL.Tests.Core;ProjectL.Tests.CoreIntegration"
  -testResults <results.xml> -logFile <editor.log>
```

Focused runs add `-assemblyNames <one>` and `-testFilter <full test class name>`. Measurement captures set `PROJECTL_ENEMY_AI_CAPTURE` and `PROJECTL_ENEMY_AI_RAW` (`phase-7-atoms` = `EnemyAdvancePreparationMeasurements.CaptureStructuralAtoms`, structural atom cost; `phase-7-service` also writes `prepared-summary.tsv` with GC-affected updates separated; added 2026-11-10, see the long-update investigation at the end). Acceptance per phase: compile clean (no `error CS` in the log), RED evidence for new tests before the change, GREEN after, no new failures against the baseline list, phase-specific corpus and measurement records.

## Manual Unity checklist (user)

- [ ] With artifacts placed in the lobby, enemies move and attack on stage 1127 and two other LD stages.
- [ ] Enemies approach to striking distance and attack after moving in the same enemy side.
- [ ] An enemy accepts a lethal counter-attack position only to kill or heavily damage a player unit.
- [ ] While a non-lethal option (waiting included) exists, enemies never end a move on collapsing ground, an active railway cell or a lethal rockfall warning cell; they may cross such cells to reach a safe cell. An enemy whose every option is lethal may die (same condition as `LethalArrivalNeverChosen` / `AllOptionsLethalKeepsLeastLethal`).
- [ ] In a narrow corridor the front enemy moves first and the others follow.
- [ ] With the Hammer inside the haboob cloud, enemies do not circle the map; they go for a reachable target or wait.
- [ ] Changing the tuning asset's death trade value changes how readily enemies trade their lives (next Play).
- [ ] Taunt, final-night recall, cancellation and terminal transitions still behave.
- [ ] Enemy turns show no new visible hitches on the LD stages (R-6: the fixture measurement saw single preparation updates up to 12 ms, R-0 up to 9.6 ms; final code after the R-6 follow-up up to 9.8 ms, but 8–9.5 ms in the 6–8 enemy group where R-0 stayed under 4 ms). The long-update investigation (end of the completion record) found that every update over 4 ms, at R-0 and now, is a Unity GC pause and no preparation Step is structurally over 0.05 ms; hitches seen here are expected to be GC pauses, somewhat more per side because enemies act more.

## Completion record

### R-0 (2026-11-09/10, done; thresholds confirmed)

- Baseline: HEAD `d08b1aee4` (merge of develop on top of `7503b8a79`, which holds all entry-condition files). Working tree was clean, so the entry condition is met by that commit.
- Code (Core runtime unchanged):
  - `C/Application/Properties/AssemblyInfo.cs`: `InternalsVisibleTo("ProjectL.Tests.CoreIntegration")` (Q-R11).
  - `T/CoreIntegration/CoreIntegrationTestData.cs`: settable `ArtifactDefinitions` (default empty = old behaviour) so the runtime can resolve a placed artifact.
  - New `T/CoreIntegration/EnemyStageBehaviorCorpus.cs` (+ `.meta`): explicit test `MeasureProductionStages`.
- Corpus as built:
  - Stages: every combat stage registered in any production stage map, keys below 10000 (test stages excluded).
  - Runtime: test roster/deck, production stage definition through a test-owned `IBattleInitializationSource` (chapter key = the stage's map key); enemy decisions through `CombatCommandCalculator.AdvanceUtility` with `EnemyDecisionDiagnostics`, published by the production publisher.
  - Scripts: `HoldBack` (front deployment, players never move), `Advance` (front deployment, round 1 each unit walks to the reachable cell with the shortest static route to the nearest enemy, up to six tries), `Spread` (farthest-point deployment, no moves). Players never attack. 5 rounds.
  - Loadouts: `None`; `Artifact` (lowest-id production artifact with effects, placed through `ArtifactInitializationWriteSink` before battle entry); `Item` (lowest-key item of the battle definition, granted and equipped on the first placed sub unit through the `EquipItem` command). Undeclared-trait rosters come from the stages themselves; the enemy keys per run are in the TSV.
  - Metrics: idle = enemy actor that, at its side's first decision input, can hit a living player unit from its anchor or a `UnitMoveCells` cell (`EnemyAttackReach`) and publishes no move or attack in that side; retreat = move that increases the static route distance to every living player unit (test BFS: visible active wall/cliff, living obstacles and structures block; units pass; high-ground one-way ignored), flagged separately when the origin is a `StageHazardForecast` cell; enemy deaths split by enemy-side window and player-side window (players never attack, so both are hazard/self deaths); player deaths; first contact round; work, expanded nodes, previews, latency per decision.
  - Output: `corpus-runs.tsv`, `corpus-decisions.tsv`, `corpus-actors.tsv`, `corpus-summary.txt` in `PROJECTL_ENEMY_AI_RAW`, summary in the test output. Runs that cannot be measured are listed and fail the test at the end.
- Deviation: the two temporary diagnostics are kept. The corpus does not replace the 1127 per-candidate score dump (that becomes the R-5 diagnostics record), and the artifact benchmark comparison is replaced by the R-1 test `OneArtifactDoesNotChangeEnemyDecisions`. Remove `EnemyArtifactHoldDiagnostics` in R-1 and `Stage1127EnemyHoldDiagnostics` in R-5.
- Measurement fix during R-0: the first corpus run reported no reachable actor at all because `EnemyAttackReach` admits the attacker through the input's current actor. The corpus now passes `input.WithActor(actor)`, the same condition the pursuit search uses.
- Full suites (Unity batch, editor closed): 6,492 tests, 60 failed (Core 25, CoreIntegration 35), against the 64-failure reference run (14:20Z, before the develop merge `d08b1aee4`). No compile errors. Differences all in `DeckSlotInfoPanelTests` (UI layout, from the develop merge, not this change): `Abilities_ShowAllContentsInExactlyOneArea(1..5)` now pass, `Abilities_ShowAllContentsInOrderAndScrollToLast(1)` newly fails (`-342.46` outside `(-318.14, 0.1)`). The new baseline failure list is this run.
- Stage corpus baseline (18 stages: LD 1120–1134 and 2135 boss, chapter 2 regular 2201 and boss 2299; 162 runs, 5 rounds, 87 s; artifact 1001):

| Loadout | Reach actor-sides | Idle | Idle ratio | Moves | Retreats (no hazard) | Attacks | Sides with attack | Enemy deaths, enemy window | Player deaths | Mean first-decision work | Decision latency mean / max ms |
|---|---|---|---|---|---|---|---|---|---|---|---|
| None | 841 | 453 | 0.539 | 697 | 73 (58) | 193 | 128 | 0 | 24 | 19,658 | 24.6 / 144.7 |
| Artifact | 530 | 530 | 1.000 | 0 | 0 (0) | 0 | 0 | 4 | 2 | 80,108 | 71.3 / 258.9 |
| Item | 538 | 518 | 0.963 | 54 | 8 (5) | 11 | 11 | 4 | 3 | 78,932 | 61.0 / 309.3 |

  - With one placed artifact no enemy moves or attacks on any stage; one equipped item does nearly the same (F2 confirmed on production data). Idle enemies standing still also die to hazards (4 deaths) where the acting side lost none.
  - Both boss stages (2135, 2299) are fully idle even without a loadout (25/25), consistent with undeclared boss traits.
  - Per stage without loadout (idle/reach): 1120 37/53, 1121 35/53, 1122 9/39, 1123 25/66, 1124 10/37, 1125 48/62, 1126 32/47, 1127 16/42, 1128 15/44, 1129 25/35, 1130 19/36, 1131 24/67, 1132 22/54, 1133 28/50, 1134 21/53, 2135 25/25, 2201 37/53, 2299 25/25. Stage 2201 gives exactly the same numbers as 1120.
  - First contact without loadout: `Advance` script reaches contact in round 1–3 on every regular stage; `HoldBack` never reaches contact on 1120, 1121, 1126, 1129 and 2201 within 5 rounds.
  - Raw files: `corpus-runs.tsv`, `corpus-decisions.tsv`, `corpus-actors.tsv`, `corpus-summary.txt` (scratchpad, not committed).
- Existing captures (Core unchanged, all within run-to-run noise of the follow-up record):
  - `phase-6-corpus`: first intents Attack 30 / Move 20 / Boundary 5 (identical); first-decision work 861,315 (follow-up after fix 859,820).
  - `phase-6-atoms`: 1,695,540 warmed Steps; p95 0.0035 ms, p99 0.0074 ms, max 7.70 ms, 10 Steps over 1 ms; first observations over 1 ms 74, max 18.96 ms (`Attack/SacrificePlan`).
  - `phase-7-service`: preparation per update p95 1.030 ms, p99 1.083 ms, max 9.57 ms, 3,166 updates, 6 over 4 ms; decision latency p95 9.66 ms.
- Unity side-effect files (FMOD catalog path, three font atlases, sequences settings line endings) were shown and restored after the runs.
- Pass thresholds for R-6 (Q-R8), confirmed by the user on 2026-11-10 as proposed:
  1. Loadout independence: Artifact and Item idle ratio within 0.05 of None, and every stage has at least one enemy move or attack with each loadout.
  2. Idle ratio (None) ≤ 0.20 overall, and no stage at 1.0 while it has reachable actors (bosses included).
  3. Retreats without a hazard reason ≤ 2% of enemy moves (baseline 8.3%). Since the R-6 follow-up (user decision, 2026-11-10) a retreat is a move that raises the AI's own route cost (published progress term negative); the static-terrain count is reported for reference.
  4. Enemy deaths in the enemy window: 0 for every loadout.
  5. First contact: `Advance` by round 2 on every regular stage; `HoldBack` within 5 rounds on every regular stage.
  6. Performance: the spec §8.3 stop rule applies to the stage corpus too (mean first-decision work ≤ 1.5× this baseline per loadout); service preparation p95 ≤ 1.1 ms; warmed Step p95 unchanged.
  - Thresholds 2, 4 and 6 were refined on 2026-11-10 (user confirmed): see "Threshold refinements" at the end of R-6.

### R-1 (2026-11-10, done)

- Decision during the phase (user, 2026-11-10): undeclared parts of an effect rule are ignored **separately**. A rule without a coverage declaration (`IEffectAssessmentCoverageRule`) adds no gap, harm or projection block unless its harm applies to this candidate (declared `ActingAttackOnly`/`Unbounded`, or an undeclared knockback rule on the acting attack), in which case it keeps the old `UnsupportedEffect` gap, harm and block. A rule without a harm declaration (`IEffectAssessmentHarmRule`) adds no allied-harm ceiling, except undeclared knockback on the acting attack. Coverage-declared rules keep their gaps. This resolves the plan's conflict between `UndeclaredHarmless` becoming unblocked and "declared rules unchanged" in favour of the former.
- Code:
  - `A/CombatCommandEffects.cs`: `AssessmentCoverageSteps(..., bool enemyAssessment = false)`. Enemy mode applies the rule above and skips held environment, equipped/temporary items, placed artifacts and obstacle bindings. Default mode is unchanged (`ItemsHeldEnvironmentAndUndeclaredRulesAreIgnored` checks both modes).
  - `A/CombatActionAssessment.cs`: passes `enemyAssessment: true`; new `ObstacleExposureSteps` for attacks only (moves cannot enter structure cells). Seeds are the powder barrels the attack's selection targets (every barrel when the attack can force movement); the chain follows barrels inside a blast; allied units and structures inside the blast cells (`CombatHazardAreas.PowderBarrelCells`) join `exposed`; a touched barrel adds `UnsupportedEffect`. Supplies never harm and add nothing.
  - `E/EnemyThreatField.cs`: same mode for its coverage pass.
  - Both callers are Utility-session only; `SelectImmediate` / `CombatCommandCalculator.Advance` do not reach this code.
- Tests: new `T/EnemyIgnoredSourceTests.cs` (+ `.meta`, 5 tests / 10 cases). RED on the unchanged code: 9 cases failed as expected, `DeclaredRulesAndRouteCoverageUnchanged` passed (guard). GREEN after the change. `EnemyArtifactHoldDiagnostics` (+ `.meta`) removed (covered by `OneArtifactDoesNotChangeEnemyDecisions`).
- Intended expectation changes in existing tests (all GwonCheonmyeong):
  - `EnemyEffectHarmScopeTests`: `RemoteClassifiedEffect…` Bleeding/Disarm gap `UnsupportedEffect` → none; `ExistingSixExemptionsKeepTheirCoverageAndBound` → `ExistingSixExemptionsAddNoGapOrHarm` (gap → none); `OwnerAttackForcedMovementRetainsConservativeHarm` owned attack unchanged (4000), move and remote holder gap → none; `IndependentUnboundedSourcesRemainConservative` → `IndependentSourcesAddNoHarmCeiling` (harm 4000 → 0 for all five; Roar/KillChain keep `ActualOnlyReaction`, Unknown/Item/Obstacle gap → none).
  - `EnemyFollowupEstimateTests`: `ConditionalProjectionRetainsGaps…` → `RegenerationMoveIsPreciseWithExactNonlethalScore` (precise, gap `HiddenEnvironment`; follow-up 400, score 110 unchanged); `MixedSourcesKeepProjectionBlocked` → `MixedSourcesFollowEnemyAssessmentPolicy` (real mode unchanged; enemy mode: projection present, one preview, no `UnsupportedEffect`); `NoDoubleChargeForCoarseMove…` → `NoDoubleChargeForPreciseMove…` (precise facts hold the thorn loss once, no entry estimate added); `PartitionDecisionsAndBudgetsAgree` (previews and follow-up previews equal across partitions 1/2/31 and within `ActorPreviewLimit`, instead of ≤ 2 and 0); `UnsupportedPassengerArrivalStaysBlocked` fixture tile made visible to the mover (it passed before only through the PositionSwap blanket gap; route coverage now catches it); obstacle fixture given a matching barrel profile (the precise path reaches the input check that requires it); four helpers made `internal`.
  - `EnemyLeapAssessmentTests`: `IntermediateLeapMine…` precise, actor unharmed in the projection; `LeapDoesNotExemptPassengerArrival` 4000 → 3000 (exactly the route-exposed actor, passenger 20 and structure 90 the test already lists); `NonMineIntermediateEffectsRemainVisible` now checks the precise thorn loss 70.
  - `EnemyAlliedExposureTests`: two remote-trait tests now expect no `UnsupportedEffect` (harm still 0).
  - `EnemyActionCandidateTests.WeakGroundRiderCoarseAssessment…`: precise, and the projected rider is dead exactly when the actual move falls.
- Consequence to carry into R-3: the conditional move projection (imprecise but projected) is no longer reached by any current rule, because Regeneration was the only rule that produced a gap while supporting projection. The code path stays; R-3's geometric follow-up replaces its purpose.
- Full suites: 61 failures in the run before the last fix, 60 known plus `PartitionDecisionsAndBudgetsAgree`; after updating that test the focused run passed (`EnemyFollowupEstimateTests` 36/36). The failure set equals the R-0 baseline list (not rerun in full after that one-test fix).
- Stage corpus delta (R-0 → R-1):

| Loadout | Idle ratio | Moves | Retreats (no hazard) | Attacks | Sides with attack | Enemy deaths, enemy window | Player deaths | Mean first-decision work | Latency mean / max ms |
|---|---|---|---|---|---|---|---|---|---|
| None | 0.539 → 0.345 | 697 → 676 | 58 → 31 (4.6%) | 193 → 238 | 128 → 153 | 0 → 0 | 24 → 58 | 19,658 → 16,285 | 24.6 / 145 → 27.4 / 147 |
| Artifact | 1.000 → 0.345 | 0 → 676 | 0 → 31 | 0 → 238 | 0 → 153 | 4 → 0 | 2 → 58 | 80,108 → 16,285 | 71.3 / 259 → 27.0 / 157 |
| Item | 0.963 → 0.344 | 54 → 678 | 5 → 32 | 11 → 240 | 11 → 155 | 4 → 0 | 3 → 57 | 78,932 → 16,285 | 61.0 / 309 → 26.8 / 150 |

  - Loadouts no longer change behaviour (artifact runs are identical to no-loadout runs; item differs by a few decisions).
  - Bosses 2135 and 2299 act: idle 25/25 → 14/56, attacks 0 → 20 each.
  - First contact: `HoldBack` now reaches contact on every regular stage (rounds 1–4); `Advance` by round 2 on every regular stage.
  - Against the R-6 thresholds: 1 (loadout independence) and 4 (no enemy-window deaths) and 5 (first contact) already hold; 2 (idle ≤ 0.20) and 3 (retreats ≤ 2%) do not yet — R-2/R-3 target those.

### R-2 (2026-11-10, done)

- Code:
  - New `E/EnemyEngagementField.cs` (+ `.meta`): all living hostile units as targets (HQ only when no unit goal is reachable from the actor; a taunt route restricts the field to its source). Goals are the shared reach predicate per target, limited to landable cells within a Chebyshev radius of selection range + splash (pull bonus included; piercing and all-board aims scan the whole component). Multi-source reverse search with a bucket queue; step cost 1, allied cell `1 + AllySoftCost` (never a goal); hostile units, walls, cliffs and structures hard; mines and Leap transit as before; 289-cell component cap and truncation unchanged (`IsTruncated` stays readable, surfaced as `EnemyActionCandidates.EngagementTruncated`). Per cell: cost, best target (ties: higher value, unit before HQ, lower id), attack position; per target: value = one basic attack's loss permille (threat-field coarse damage) + elimination weight when lethal.
  - New `E/EnemyAiTuning.cs` (+ `.meta`): `AllySoftCost = 2`.
  - `E/EnemyAttackReach.cs`: optional `ObservedGeometry` (reuses walls and elevation) and a per-actor `Relocations` store (moved actor and projected input per cell, shared across that actor's targets only). `EnemyPursuitSearch` passes neither, so pursuit behaviour is unchanged.
  - `E/EnemyActionCandidates.cs`: the field replaces the map; pursuit = the field's best target at the actor's cell; the Manhattan target remains only as the truncation fallback.
  - `E/EnemyUtilityScorer.cs`: `OpposingEliminationWeight` made `internal` (read by the target value).
  - Removed `E/EnemyEngagementMap.cs` (+ `.meta`).
- Deviation: the per-decision goal-set cache keyed by `(profile, range, occlusion, Leap)` was **not** built. Goal cells depend on more per-actor facts than that key (unit range, attack restriction, destination modifiers, size, other units on a piercing line including the actor's own vacated cell), which the user's review of 2026-11-10 also listed. Each actor computes its own goals; only the target-independent relocation is shared, and only within one actor. Performance stayed inside the stop rule without the cache, so it is left to R-5, where only geometric candidate ranges (not delivery) would be shared.
- Finding: two enemies with the same unit key cannot differ in Pull, because `PullAttackCalculator` requires the Pull status to match the profile definition and enemy profiles are per key.
- Tests:
  - New `T/EnemyEngagementFieldTests.cs` (+ `.meta`): `NearestByRouteNotStraightLine`, `HaboobUnattackableTargetIsSkipped`, `AlliedUnitsAreSoftHostileAreHard`, `GoalsMatchAttackReach` (square, cardinal, fan/direction, radial splash, pull extra, pierce), `SameProfileActorsKeepTheirOwnGoals` (range, disarm, destination modifier; added on the user's review), `TargetValueAndHeadquartersOrder`, `TauntRestrictsFieldToSource`, `PartitionsProduceIdenticalFields`.
  - RED with candidates still on the old map: the candidate-progress assertions failed as predicted (ally corridor 0 vs 2, haboob 0 vs 1, walled detour −1 vs 1) while the field-level assertions already passed (which also confirms the reach predicate enforces haboob designation). GREEN after the switch.
  - CoreIntegration pinned (in `EnemyStageBehaviorCorpus`): `Stage1127HeavyBehindAlliesKeepsApproachProgress` (each heavy that has not acted yet has an attack or a move with positive progress; failed on the old map) and `Stage1127DemonDoesNotRetreatWhileTargetReachable` (widened from round 3 to rounds 1–4 because the Demon no longer moves in round 3; now sees Demon moves and finds no retreat). The corpus setup was extracted into `ProductionBattle` for reuse.
  - Migrated `EnemyEngagementProgressTests` (GwonCheonmyeong) to the field: helper builds a field restricted to the same single target (identical distances); `AttackGeometryDefinesGoals` moved to `GoalsMatchAttackReach`; `CollapseGoalAndLivingOccupancyAreNotInventedLandings` now expects the allied cell to be passable at cost 2 (origin cost 4 + `AllySoftCost`) but never an attack position (intended F6 change). The goal-reference helper skips unit-occupied cells.
- Full suites: the run after the switch had 62 failures = the 60 baseline + the heavy test sampling an already-moved heavy + a disarm-fixture range mismatch (both test fixes, not code). After the fixes the focused run passed; the full set was not rerun.
- Stage corpus (R-1 → R-2, no loadout): idle 0.345 → 0.350, moves 676 → 725, retreats without hazard 31 → 30 (4.1%), attacks 238 → 247, player deaths 58 → 63, mean first-decision work 16,285 → 15,876, latency mean 27.4 → 18.2 ms. Artifact and item runs still match the no-loadout runs.
- Measurements vs R-0: `phase-6-corpus` first intents unchanged (Attack 30 / Move 20 / Boundary 5), first-decision work 861,315 → 980,080 (1.14×, under the 1.5× stop rule; largest per fixture A8 2.15×, H8 1.40×; includes R-1's precise previews, no capture was taken between R-1 and R-2). `phase-6-atoms`: warmed Step p95 0.0025 ms (R-0 0.0035), p99 0.0072, max 8.56 ms, 9 Steps over 1 ms.
- Idle ratio barely moves in R-2: the field gives correct progress and targets, but whether a move beats Hold is decided by the scorer, which R-3 changes.

### R-3 (2026-11-10, done)

- Entry check: satisfied by the R-2 record (no cross-actor goal-set sharing; every actor computes its own goals).
- Code:
  - Movement-side helper (location decided here): `A/CombatMovementCalculator.cs` gained `ApplyMoveSucceeded` (the move-success effect plan + stat copy, now also called by route completion `RouteWork`, so completion and AI share one owner for `StationaryFire.MovedThisOwnTurn`, Acceleration, etc.) and `MovedActor` (vacate tile modifier → anchor at the endpoint → `ActionUse.Moved`, same as `MovementProjection.BeginOrdinaryMove` → endpoint tile modifier → `ApplyMoveSucceeded`). Path arrival chains are not applied; callers filter such routes.
  - New `E/EnemyFollowupGeometry.cs` (+ `.meta`): `IsEligible` (field present and not truncated, endpoint is an attack position, no route arrival gaps, not a collapse landing) and `Relocate` (moved actor from the helper; null when `UnitActionUseRules.CanStartAttack` fails or `CombatActionAdmission.IsAttackRestricted`). Used only when no precise projection exists; the follow-up attack itself is assessed with its own scoped facts (existing `FollowupSteps`, coarse). Taunt is re-evaluated at the endpoint by the follow-up candidate builder, and the follow-up aim is ordered by the field's target value (`EnemyActionCandidates.AttackOrder`, follow-up only).
  - `E/EnemyUtilityScorer.cs`: `EnemyScoreFacts.NextTurnStrike` / `PlayerThreatLethal`; `EnemyScoreWeights` (from settings); `Score` = attack benefit + follow-up × `FollowupWeight` + next-turn × % + progress (±100) − `PlayerRisk` − hazard × 4 − bind penalty; `PlayerRisk` = lethal threat ? death-trade price (percent × 10 permille) : threat × %. Old `ThreatWeight` (1) and follow-up divisor (2) removed.
  - `C/Contracts/Combat/EnemyAi/EnemyDecisionSettings.cs`: three optional parameters (`deathTradeThresholdPercent`, `threatAversionPercent`, `nextTurnApproachPercent`, 0..1000) plus public default and maximum constants.
  - `E/EnemyThreatField.cs`: `Exposure.IsLethal` (damage ≥ current HP).
  - `E/EnemyActionCandidates.cs`: the one lethal-hazard predicate `IsCertainLethal(health, arrival, collapseLanding, transitLoss)` and `LethalRank` (existing `HazardRank` plus transit; collapse = all HP). The BFS applies it where a cell is evaluated for landing (arrival-lethal → dropped; transit-only lethal → `RefineSteps`, dropped if no safe route), never to traversal. Representative routes (taunt route, pursuit fallback) get the same treatment (`SafeRoute`): previously a lethal pursuit path could occupy the endpoint slot and hide the safe refined route to the same endpoint. `LethalLandingsDropped` counts dropped options; `Field` accessor.
  - `E/EnemyDecisionSession.cs`: per actor, Hold arrival lethality and Hold score with weights and its own next-turn strike; per candidate, route gaps, geometric follow-up when no projection, next-turn strike at the endpoint, threat lethality, `CandidateLethality` (guaranteed enemy win exempt; dead after the action = lethal; arrival at the endpoint; transit only for coarse facts). Filter: any safe option → drop lethal ones, and safe options bypass the Hold comparison when Hold itself is certain-lethal (otherwise the death would be avoidable by the same predicate); all lethal and Hold lethal → keep the least lethal by `LethalRank`, compared with Hold by score; all lethal and Hold safe → Hold. `NextTurnStrike`: field cost ≤ voluntary movement allowance and the best target designatable at `LastBoundaryCount` from the endpoint (`CanDesignateAt == true`).
  - `E/EnemyDecisionDiagnostics.cs`: internal `EnemyHoldReason` (`NoReachableTarget`, `FieldTruncated`, `LethalOnly`, `ExchangeLoss`, `WorldUncertainty`) and one hold record per held actor per decision, kept only with `captureAlternatives`.
  - `E/EnemyAiTuning.cs`: `FollowupWeight = 1`.
  - DB: `EnemyAiTuningData` (pure, `Validate` via the settings contract, `ToSettings`), `EnemyAiTuningDataSO` (Korean header/tooltips, ranges 0..1000), `EnemyAiTuningDataTableSO`, `EnemyAiTuningTable` + `IEnemyAiTuningTableQuery`, optional `IHasEnemyAiTuningTable` on `InGameDataBase`, `InGameTableKey.EnemyAiTuning`, optional registration in `InGameDataBaseFactory`; assets `DataBaseSO/InGameGroup/EnemyAi/EnemyAiTuningDataTable.asset` and `Item/EnemyAiTuning_Default.asset` (50/50/50) registered in `InGameGroup.asset`. Connection lines only (ServiceKey added: `InGameTableKey.EnemyAiTuning`, for the Framework owner's record).
  - `CombatAdvanceService`: `ISystemInitData<IDataBaseQueryContext>`; builds settings from the optional table (absent → `EnemyDecisionSettings.Default`) and passes them to the prepared session (`DecisionSettings`).
- Deviations / decisions to confirm:
  - Defaults live on the public settings contract (`EnemyDecisionSettings.Default…Percent`), not in `E/EnemyAiTuning.cs` as spec 6.5.1 says: the DB input SO needs them and cannot see Core.Application internals. One owner per value.
  - `NextTurnStrike` checks designation from the endpoint, not from the (unknown) attack position the actor would step to next turn.
  - `WorldUncertainty` = settlement rejected the chosen action, or the best safe candidate was lowered by the allied-harm ceiling.
  - Finding: `GuaranteedTerminal` can never be true for assessment facts, because the assessment always keeps `HiddenEnvironment` in the gaps. The rule 4-2 exemption (and the existing `PositionFacts` early return) is therefore dormant in play; it is covered at function level only.
  - The non-prepared path (`CombatCommandCalculator.Advance` → `EnemyAiTurnService`) still uses default settings; production uses the prepared path.
- Tests:
  - New `T/EnemyExchangeModelTests.cs` (+ `.meta`): `ExchangeWorkedExamples`, `AdjacentAttackPositionCreditedWithoutProjection`, `FollowupRespectsRules`, `MovedActorFactsComeFromMovementOwner`, `LethalArrivalNeverChosen` (railway, rockfall, collapse), `SafeCrossingAllowed` (railway, rockfall), `LethalTransitRouteExcluded`, `UncertainHazardScoredNotFiltered`, `AllOptionsLethalKeepsLeastLethal`, `LethalFilterPriority`, `HoldReasonCodes`, `TuningValuesChangeDecisions`, `NextTurnStrikeFollowsHaboobDesignation` (the plan's haboob boundary case). Fixture `Arena` (rails, rockfall warning, thorns, haboob, move range, actor HP).
  - New `T/CoreIntegration/EnemyAiTuningAssetTests.cs` (+ `.meta`): `TuningAssetLoadsAndDefaults` (production asset 50/50/50, SO → Data mapping and range error, service settings from a tuned table, defaults without the table).
  - CoreIntegration pinned (in `EnemyStageBehaviorCorpus`): `Stage1127DemonStepsNextToHammerAndAttacks`. The design's round-2 situation (Demon holding at (8,6)) no longer occurs: in the first enemy side the Demon already goes (9,6) → (8,5) and attacks the Hammer, so the test looks at rounds 1–2; rounds 3–4 require a Gravekeeper/Heavy attack (a Heavy attacks in round 3).
  - RED was not observed separately: the R-3 code was written before the tests, so the first run of the new tests was already against the implementation. The first run found two real gaps, fixed in code (representative routes hiding the safe refined route; `NoReachableTarget` instead of `LethalOnly` when the BFS had already dropped every lethal landing).
- Expectation changes in existing tests (all GwonCheonmyeong, intended by the spec §6.5 weights unless noted):
  - `EnemyUtilityScoreTests.LockedWeightsProduceExactFixedPointValues`: threat 250 → −125 (was −250; threat aversion 50%), follow-up 200 → 200 (was 100; follow-up weight 1); added lethal threat → −500 and next-turn 300 → 150.
  - `EnemyFollowupEstimateTests.RegenerationMoveIsPreciseWithExactNonlethalScore`: 110 → 385 (follow-up 400 × 1 + progress 60 − threat 150 / 2).
  - `EnemyForecastBindDecisionTests.ScorerPreservesExistingTermsAndFollowupBenefit`: (2110, 1610) → (2260, 1760) (follow-up 200 × 1, threat 100 / 2).
  - `PositionFacts` gained a next-turn parameter: the reflection delegates in `EnemyFollowupEstimateTests` and `EnemyForecastBindDecisionTests` and the invocations in `CoreIntegration/Stage1127EnemyHoldDiagnostics` pass 0; the diagnostic's `Score` lookup selects the one-argument overload (was ambiguous).
  - `EnemyUtilityDecisionTests` and `EnemyMoveThenAttackTests` needed no change (they pass unchanged).
  - Helpers made `internal` for reuse: `EnemyFollowupEstimateTests.Followup`, `EnemyActionCandidateTests.RiderOnWeakGround`, `EnemyEngagementFieldTests.WithUnit`.
  - `EnemyStageBehaviorCorpus.Stage1127DemonDoesNotRetreatWhileTargetReachable` (R-2 pinned test): its retreat helper now treats the other living player units as blocking (the engagement field's rule). With R-3 the Demon reaches (8,5) in round 1; in round 3 the haboob covers the Hammer and the Cowboy, the Demon cannot attack, and the field routes it around the walls toward the 6 HP Sniper (field cost 8 → 5 via (9,7)). The static-terrain distance ignored the player units blocking the short way and reported this detour as a retreat. The corpus retreat metric itself is unchanged (static terrain), so corpus retreat counts stay comparable with earlier phases.
- Full suites (`ProjectL.Tests.Core` + `ProjectL.Tests.CoreIntegration`): 6,532 tests, 60 failures (Core 25, CoreIntegration 35), identical by name and message to the R-0 baseline (all pre-existing: `DataBaseAssetTests` asset expectations, night/turn-cycle tests and others listed in the R-0 record). No new failure. Publication suites (`EnemyUtilityPublicationTests`, `EnemyAdvancePreparationTests`, `EnemyDecisionSourceStampTests`, `EnemyAssessmentBoundaryPublicationTests`, `EnemyEngagementFollowupPublicationTests`) pass. The last focused run (21 tests incl. the corpus and the pinned tests) passed after the hold API was made internal.
- Stage corpus (R-2 → R-3, no loadout; artifact and item runs give the same counts):
  - Raw idle ratio 0.350 → 0.316 (183 / 580), moves 725 → 645, retreats without hazard 30 → 16 (2.5% of moves; R-6 threshold 2%), attacks 247 → 286 with fewer enemy sides (189 → 165), player deaths 63 → 75, runs ending inside 5 rounds 43 → 51 of 54, mean first contact round 1.52 → 1.30, mean first-decision work 15,876 → 16,307, latency mean 18.2 → 15.3 ms.
  - Idle split by Hold reason (reachable actor-sides that did not act): `NotEvaluated` 129 (the battle ended during that side before the actor's turn, mostly the main dying; recorded as "-" in this run, renamed afterwards), `ExchangeLoss` 54, all others 0. Held but unreachable: `NoReachableTarget` 18. Idle per the refined threshold 2 (Holds other than `LethalOnly`/`NoReachableTarget`, actors that got a decision) = 54 / 451 = 0.120.
  - Boss stages 2135 and 2299 now engage in round 1–2 (fully idle at R-0). Every regular stage's `Advance` first contact is by round 2 and every `HoldBack` by round 3.
  - Hazard deaths: no enemy died in any window (players never attack), so avoidable / unavoidable / other are all 0; the avoidable-death rule is covered by the unit tests rather than by corpus events.
  - Engagement-field truncations: 0 on every stage.
  - Corpus additions: `captureAlternatives` diagnostics, per actor-side `holdReason`, `finalLethal`, `finalUnavoidable` (last decision, same predicate) and `truncated`; enemy deaths classified in the enemy window, at the enemy-end boundary (previously uncounted) and in the player window.
- Measurements vs R-0: `phase-6-corpus` first-decision work 861,315 → 887,105 (1.03×; R-2 980,080). First intents Attack 30 → 25, Move 20 → 25, Boundary 5: the five changes are fixture H8, where actor 11 now moves first and then attacks; the side's attacks go from 3 to 7 (actors 11, 12, 15, 16, 17 move then attack). `phase-6-atoms`: warmed Step p95 0.0026 ms (R-0 0.0035, R-2 0.0025), p99 0.0080, max 8.08 ms, 10 Steps over 1 ms.
- Side-effect files restored after the runs (FMOD catalog, three font SDF assets, sequences settings; diffs shown before restoring).

### R-4 (2026-11-10, done; actor order changed by user decision)

- Decision during the phase (user, 2026-11-10, after the stop rule fired): actor order is **attack-ready first, then EntityId**; the field-cost key of spec 6.7 is dropped. Building every actor's engagement field in every decision to sort by cost made the first decision 6.6× the R-0 baseline (table below); the readiness-only order builds a field only for actors that are evaluated.
- Code:
  - `E/EnemyActionCandidates.cs`: every landable endpoint gets a numeric cheap score from the scorer formula (`EnemyUtilityScorer.Score`) without precise terms: hazard (possible loss, collapse = all HP), certain transit loss as immediate loss, player risk (lethal → death-trade price), field progress, next-turn strike, the forecast bind penalty, and for attack positions the field's best-target value as the follow-up proxy. `Destination.Rank` = (−score, steps, y, x). One function `SelectBeam` owns the reservation priority and K: under a taunt obligation the beam holds only the taunt route; otherwise hazard escape (hazard rank below standing still) → best attack position → best progress endpoint (the pursuit route when the field is truncated), deduped by endpoint, then the remaining slots by cheap score; the beam is returned in cheap-score order. `Math.Min(4, …)` caps removed for attacks and moves. The old `potential` check (`EngagementSteps`) and `Insert` are gone; the pursuit fallback is built only when the field is truncated. New: `CheapScores`, `Reserved`, `HoldCheapScore`, `BestPrunedScore`, `Actor`, `OrderReady` (set right after the attack candidates, before the field), `OrderKey` (can attack now, EntityId); `NextTurnStrike` moved here from the session (shared by the cheap score and the session score).
  - `E/EnemyDecisionSession.cs`: pass 1 steps every actor's candidates until `OrderReady` (attack candidates only) and sorts by `OrderKey`; pass 2 finishes the candidates (field, moves) only for actors it evaluates, in that order. Attacks are assessed as before; moves are first assessed coarse with the geometric follow-up, sorted (safe first, then score, then intent), and the top `RefinedMoveLimit` are re-assessed with precise projection (spec 6.6 steps 3–4; `Math.Min(2, …)` removed). Hold reason `Pruned` when a pruned endpoint's cheap score beat Hold. The per-action evaluation moved into `OptionSteps` with a shared `DecisionWorld`.
  - `C/Contracts/Combat/EnemyAi/EnemyDecisionSettings.cs`: `moveCandidateLimit` default 4 → 6 (Q-R6).
  - `E/EnemyDecisionDiagnostics.cs`: `EnemyHoldReason.Pruned`.
- Stop rule (first-decision work ≤ 1.5× the R-0 baseline 861,315), `phase-6-corpus` and the stage corpus (no loadout):

| Variant | First-decision work | × R-0 | Corpus mean first-decision work | Refined idle (threshold 2) | Attacks per enemy side | Retreats without hazard |
|---|---|---|---|---|---|---|
| R-3 | 887,105 | 1.03 | 16,307 | 0.120 | 1.73 | 16 |
| beam + spec order (field cost), rejected | 5,715,105 | 6.64 | 108,561 | 0.070 | 1.71 | 15 |
| beam only (id order), experiment | 973,465 | 1.13 | 20,217 | 0.075 | 1.65 | 17 |
| **beam + attack-ready first (final)** | **1,032,905** | **1.20** | **22,106** | **0.067** | **1.66** | **16** |

  With the spec order, `phase-6-atoms` on A8 spent 622,046 of about 628,000 steps in `UtilityOrder`, and the corpus test exceeded its 180 s timeout. First intents with the final order: Attack 30 / Move 20 / Boundary 5 (R-3: 25 / 25 / 5; attack-ready actors now act first). `phase-6-atoms`: warmed Step p95 0.0025 ms (R-0 0.0035), p99 0.0077, max 8.29 ms, 10 Steps over 1 ms.
- Stage corpus (final, no loadout; artifact and item runs within 0.002 of it): raw idle 0.320 (182 / 569), idle split: `NotEvaluated` 154 (battle ended during that side), `ExchangeLoss` 26, `Pruned` 2; held but unreachable `NoReachableTarget` 18. Refined idle 28 / 415 = 0.067 (threshold 2: ≤ 0.20). Moves 627, retreats without hazard 16 (2.6%; threshold 3: 2%, unchanged from R-3), attacks 269 over 162 enemy sides, player deaths 76, runs ending inside 5 rounds 53 / 54, first contact `Advance` ≤ round 2 and `HoldBack` ≤ round 3 on every regular stage, hazard deaths 0 (none in any window), field truncations 0. Attacks per side read 1.73 → 1.66 because more battles end during an enemy side (`NotEvaluated` 129 → 154); attacks and player deaths per run are about the same.
- Tests:
  - New `T/EnemyCandidateBeamTests.cs` (+ `.meta`): `AttackPositionSurvivesPruning`, `ReservationsCountInsideBeam`, `ReservationOverflowFollowsPriority`, `LimitsHonoured`, `PruningUsesScorer`. `ReservationOverflowFollowsPriority` cannot show three distinct reservations: whenever an attack position is reachable it also has the maximum progress (cost 0), so best attack position and best progress coincide; the overflow is tested with escape + attack position and the dedupe with attack position = progress.
  - New `T/EnemyActorOrderTests.cs` (+ `.meta`): `ReadinessFirstOrder` (attack-ready 13 before 11, 12, 14; partitions 1, 2, 31; under the old id order 11 would move first) and `BlockedBackUnitWaitsForFrontUnit` (the plan's `CorridorUnblockedByOrder`, renamed with the order change: both corridor units are ordered by id, the blocked back unit has no option and holds, the front unit moves first and the back unit follows).
  - `EnemyExchangeModelTests`: `Arena` gained `allies`; calls follow the moved `NextTurnStrike` and the new `HoldReason` parameter.
  - Expectation changes by intent (GwonCheonmyeong): `EnemyActionCandidateTests.ShortlistsKeepLegalDistinctIntentsAndDoNotChargePreviews` move count up to K (was 4); `TauntAndEscapeReservationsFitInsideCap` → `TauntObligationBeamHoldsOnlyTauntRoute` (the obligation keeps only the taunt route; the session dropped the escape anyway); `EnemyDecisionSessionTests.Settings_DefaultsAndRelationshipsAreStable` default K 6; `EnemyEngagementProgressTests.CandidateAndDecisionPartitionsAreIdentical` generated moves ≤ K, refined ≤ R. `EnemyDecisionBudgetTests` and the publication suites needed no change.
  - Full suites (`ProjectL.Tests.Core` + `ProjectL.Tests.CoreIntegration`, final code): 6,542 tests, 60 failures, identical by name and message to the R-0 baseline; no new failure. The two drain-limit failures seen with the spec order are gone. Corpus fixture (corpus + three pinned 1127 tests) passes.
- Side-effect files restored after the runs (FMOD catalog, three font SDF assets, sequences settings).

### R-5 (2026-11-10, done)

- Read sets (first task; each is a `ReadSet` type next to its builder with `Difference` = first differing item and `Matches`; the cache only stores and compares):
  - Hazard forecast (`StageHazardForecast.ReadSet`): phase, turn facts, objective, board size, gimmick states, the battle's gimmick definitions (by reference) and rail cells, the main (and that there is exactly one) and the objective target, and for `Evaluate` the stored input's profiles and owned records (forced-movement immunity). Not read: other units, environment, structures, so enemy moves and environment changes do not invalidate it.
  - Threat field (`EnemyThreatField.ReadSet`): the whole battle state (gameplay values: environment, wall blocking, gimmicks for haboob designation, structures, board, phase), environment bindings, input kind (actor only for attack inputs, as the coverage pass reads it), profiles, owned records, placed artifacts, a presence summary of every character (id, owner, area, dead, cell count: multi-cell deferral, coverage pass, artifact attack power), every player character's facts and soul count. Every character's facts only when a ready player has splash or piercing. Refined from the plan's starting point: enemy positions are read only through splash centres and piercing encounters; the coverage pass reads which living characters exist and their profiles' rules, not positions. Its status/category consistency checks are battle invariants and are not digested.
  - Goal sets (`EnemyAttackReach.ReadSet`, per actor): the actor's facts, the whole battle state, bindings, processing kind and input actor, profiles, owned records, artifacts, the actor's soul count, the facts of every character not on the actor's side (target cells, eligibility, splash, piercing), presence only for same-side characters (not eligible without a friendly policy; the primary-cell occupancy check fails either way). Every character's facts when the targeting policy allows friendly targets or the profile has Pull (pull destinations read occupancy). Memo = (target, cell) → can hit; landing and radius filters are re-applied on every use.
  - Comparison of battle-constant inputs (decision during the phase, confirmed by the user on 2026-11-10): the input assembler recomposes the profiles (and owned units' `UnitSpec`) every decision, so reference comparison gave 0 reuse on production stages. Profiles are now compared by owned id, unit key and method reference, owned records by `SameValues`. The definition content is composed only from the battle definition (fixed per battle instance, covered by the scope key, as spec 6.8's key "battle id" intends) and the owned records. An input that changes only a unit definition under the same battle id is not distinguished (documented on `EnemyReadSetValues.SameProfiles`).
- Code:
  - New `E/EnemyDecisionSideCache.cs` (+ `.meta`): public sealed handle with no public members. Scope = (battle instance id, controller turn sequence); any other input clears all entries. Stores forecast, threat field and per-actor goal memos; internal hit/miss counters and miss reasons for tests and measurements. `EnemyReadSetValues` holds the value comparisons (profiles, owned, artifacts, characters, presence, souls).
  - `E/EnemyDecisionSession.cs`: public overload `(input, rules, settings, EnemyDecisionSideCache)`; internal `(…, diagnostics, sideCache = null)`; the forecast comes from the context; diagnostics record.
  - `E/EnemyDecisionContext.cs`: `SideCache` (enters the scope on construction); `ForecastSteps` and `ThreatSteps` take the cached structure when the read set matches. Follow-up contexts (projected inputs) get no cache.
  - `E/EnemyEngagementField.cs`: goal checks go through the actor's memo when a cache is present (target preparation is then lazy); without a cache the steps are unchanged.
  - `E/EnemyUtilityScorer.cs`: `EnemyScoreTerms` and `Terms()`; `Score` is `Terms().Total` (one owner of the formula).
  - `E/EnemyDecisionDiagnostics.cs`: per actor `ActorRecord` (hold terms, hold tier, hold lethality, hold reason) with `CandidateRecord`s (action, terms, tier, lethality, precise), captured only with `captureAlternatives`.
  - `S/.../CombatAdvanceService.cs`: creates the handle at the first preparation of an enemy side, keeps it across `CancelPreparation` (after each publish), drops it when the side ends or the battle or turn sequence changes (`Tick`, `PrepareAdvanceSlice`, `Shutdown`).
  - Files outside the plan's list: `A/StageGimmicks/StageHazardForecast.cs` (its read set, next to the builder), `A/CombatCommandCalculator.cs` and `E/EnemyAiTurnService.cs` (internal overloads passing a cache, used by the corpus and tests; the three-parameter `AdvanceUtility` stays because `EnemyAiPerformanceMeasurements` binds it as a delegate).
- Tests:
  - New `T/EnemyDecisionSideCacheTests.cs` (+ `.meta`): `CacheOnOffIdenticalTraces` (11 fixtures × 5 seeds × partitions 1/2/31; sessions stepped per partition and settled by `AdvancePrepared` like the service; trace = published action, events, state, record terms and hold reasons; full state serialized at partition 1, a unit/structure/environment summary at 2 and 31 to keep the test near 100 s), `DigestInvalidation`, `CacheScopedToSide`, `DiagnosticsRecordTerms`.
  - CoreIntegration (`EnemyStageBehaviorCorpus`): `CacheOnOffIdenticalTracesOnProductionStages` (18 stages, `Advance`, no loadout, 5 rounds, partitions 1/2/31); explicit `DumpStage1127DecisionRecords(bool)` replaces the removed `Stage1127EnemyHoldDiagnostics` (+ `.meta`), which read the same terms by reflection.
  - Corpus switched to the record: each run uses a side cache (as production); hold reasons and certain-lethal flags come from the record instead of being recomputed; new `corpus-candidates.tsv` (one row per hold and candidate with terms and a published flag); decision rows add the field enemy count and per-decision cache counts; the summary adds cache counts and latency by enemy band; trace runs skip `MeasureReach`; a garbled R-3 comment fixed; `Timeout(900000)` on the measurement and the identity test.
  - RED: with the cache wiring and the record switched off (temporary), both identity tests failed on "no reuse" while their traces matched, and `CacheScopedToSide` and `DiagnosticsRecordTerms` failed; `DigestInvalidation` failed on a test-input bug (environment progress id), fixed. GREEN after restoring.
  - Expectation changes in existing tests: none.
- Full suites: 6,547 tests, 60 failures (Core 25, CoreIntegration 35), identical by name and message to the R-0 baseline; no new failure. The publication suites (prepared service now with the cache) pass.
- Identity: fixtures 1,665 decisions per mode identical (reuse: forecast 1,245, threat 375, goal sets 0, because every evaluated fixture actor acts, so no actor's field is built twice in a side); production stages 990 decisions per mode identical (reuse: forecast 762, threat 537, goal read sets 39 of 834 field builds, goal cells 717 of 32,091; misses besides first lookups: forecast main 72, threat players 264 and state 33, goals characters 33 and state 6).
- Threat field hit rate (the plan's condition for keeping it): production corpus 583 of 1,005 decisions (69% of the decisions after the first in a side). It pays (below), so it is kept.
- Stage corpus (no loadout, R-4 → R-5): behaviour identical (decisions in the same order; idle 182 / 569, moves 627, retreats without hazard 16, attacks 269, player deaths 76, hazard deaths 0, truncations 0, hold reasons unchanged). Work 24,269,777 → 22,110,435 (−8.9%); first decisions 3,965,410 → 3,965,102 (mean 22,106 → 22,103, lazy target preparation); later decisions mean 24,086 → 21,525 (−10.6%). Per-decision deltas by cache use: threat reuse ≈ 3,470 per reused decision (≈ 92% of the saving), goal-set reuse ≈ 2,190 more per decision in 36 decisions (≈ 4%), forecast reuse ≈ 76 per decision (≈ 3%). The goal-set and forecast caches pay little; they are kept as planned (dropping them is a user decision).
- Corpus decision latency by field enemy count (enemy actors at side start; the same derivation applied to the R-0 and R-4 raw files; corpus stages have no 9–10 or > 10 band). Latency compares across sessions and is noisy; work is deterministic:

| Band | R-0 n / mean / p95 / max ms / mean work | R-4 | R-5 |
|---|---|---|---|
| ≤ 5 | 195 / 16.17 / 37.82 / 78.61 / 19,450 | 134 / 21.26 / 58.07 / 148.50 / 19,773 | 134 / 12.26 / 35.07 / 49.51 / 18,336 |
| 6–8 | 935 / 26.31 / 81.75 / 144.73 / 26,622 | 871 / 26.43 / 70.80 / 116.32 / 24,822 | 871 / 15.34 / 39.65 / 84.51 / 22,564 |

- Measurements vs R-0:
  - `phase-6-corpus`: first-decision work 1,032,905 (= R-4, 1.20× R-0 861,315; stop rule respected), first intents Attack 30 / Move 20 / Boundary 5, side traces identical to R-4 (the harness passes no cache).
  - `phase-6-atoms`: warmed Step p95 0.0027 ms (R-4 0.0025, R-0 0.0035), p99 0.0078, max 7.52 ms, 5 Steps over 1 ms.
  - `phase-7-service` (the service uses the cache), two runs: updates 3,706 / 3,685 (R-0 3,166), preparation per update p95 1.044 / 1.035 ms (R-0 1.030; threshold 1.1), p99 1.178 / 1.116 (R-0 1.083), max 9.96 / 9.07 ms (R-0 9.57), updates over 4 ms 7 / 7 (R-0 6); decisions 560 (R-0 480), decision latency p95 12.62 / 13.35 ms (R-0 9.66). By fixture enemy count, with the R-0 raw output regrouped the same way (the computation reproduces R-0's totals exactly):

| Band (fixtures) | R-0 p99 / max ms / over 4 ms | R-5 run 1 | R-5 run 2 | R-5 code, cache off (control, two runs) |
|---|---|---|---|---|
| ≤ 5 (N5) | 1.141 / 9.57 / 4 | 1.184 / 7.13 / 2 | 1.095 / 8.56 / 2 | 1.098 / 9.04 / 2; 1.081 / 8.09 / 1 |
| 6–8 (N6, N8) | 1.069 / 3.97 / 0 | 1.205 / 9.96 / 5 | 1.131 / 9.07 / 5 | 1.119 / 8.29 / 3; 1.121 / 8.18 / 4 |
| > 10 (G16) | 1.065 / 8.19 / 2 | 1.106 / 3.89 / 0 | 1.081 / 1.33 / 0 | 1.035 / 3.29 / 0; 1.285 / 10.43 / 6 |

  The cache-off control (same session; the service passed no handle, reverted afterwards) gives the same 6–8 p99 as the cached runs, so the rise over R-0 does not come from the cache; it came with R-1..R-4 (no `phase-7-service` capture was taken in those phases), and the run-to-run spread is several percent. Against the R-6 refinement (6–8 p99 within 10% of R-0, i.e. ≤ 1.176 ms) run 2 passes and run 1 does not: to watch in R-6. No 9–10 fixture exists.
- Side-effect files (FMOD catalog, font SDF assets, sequences settings) shown and restored after the runs.

### R-6 (2026-11-10, done; threshold 3 missed, threshold 6 refinement borderline)

- Code: Core runtime unchanged since R-5. `T/EnemyUtilityAcceptanceMeasurements.cs` (GwonCheonmyeong, explicit `phase-7-behavior` capture) rewritten: `CaptureHoldScoresAndScheduleParity` → `CaptureScheduleParityAndDecisionRecords`. The schedule parity part (side traces at Step widths 1/2/31, first intent unchanged under changed RNG cursors) is kept; the old `CaptureHold` part is retired: it asserted the pre-redesign all-Hold state on A8/X8/H8 and bound `PositionFacts` by reflection with the pre-R-3 signature (it could not run any more). It now writes the first decision's diagnostics record for A8/X8/H8 (`first-decision-records.tsv`); `hold-candidates.tsv` and `hold-sources.tsv` are gone.
- Full suites (final code): 6,547 tests, 60 failures (Core 25, CoreIntegration 35), identical by name and message to the R-0 baseline; no new failure, no compile error.
- `phase-7-behavior` (rewritten): passes; 55 inputs, traces identical at widths 1/2/31 and first intents identical under changed RNG cursors; 120 record rows for A8/X8/H8.
- Corpus against the R-0 thresholds (the R-5 corpus run; the Core code is unchanged since):

| # | Threshold | Result | Verdict |
|---|---|---|---|
| 1 | Artifact and Item idle within 0.05 of None; every stage has an enemy move or attack with each loadout | raw idle 0.320 / 0.320 / 0.318; every stage × loadout has moves or attacks | pass |
| 2 | Refined idle ≤ 0.20 (Holds other than `LethalOnly` / `NoReachableTarget`, actors that got a decision); no stage at 1.0 with reachable actors | None 28 / 415 = 0.067 (`ExchangeLoss` 26, `Pruned` 2), Item 29 / 417 = 0.070; highest stage raw idle 0.57 (1129); bosses act | pass |
| 3 | Retreats without a hazard reason ≤ 2% of enemy moves | 16 / 627 = 2.55% (None and Artifact; Item 16 / 625) | **missed** |
| 4 | Avoidable hazard deaths in the enemy window 0; unavoidable listed | 0 avoidable, 0 unavoidable (no enemy hazard death in any window) | pass |
| 5 | First contact: `Advance` by round 2, `HoldBack` within 5 rounds, every regular stage | `Advance` ≤ 2, `HoldBack` ≤ 3 (bosses: 1–2) | pass |
| 6 | Mean first-decision work ≤ 1.5× R-0 per loadout; service preparation p95 ≤ 1.1 ms; warmed Step p95 unchanged | corpus 22,103 vs R-0 19,658 / 80,108 / 78,932 (1.12× / 0.28× / 0.28×); `phase-6-corpus` 1,032,905 = 1.20×; service p95 1.027–1.044 ms over five runs; Step p95 0.0027 ms (R-0 0.0035) | pass |
| 6 (refinement) | Per-update p99 for 6–8 and 9–10 enemies within 10% of R-0 (6–8: ≤ 1.176 ms); per-update max reported | 6–8 over five runs 1.205 / 1.131 / 1.117 / 1.289 / 1.166 (median 1.166 = +9.1%, mean 1.182 = +10.5%); no 9–10 fixture | **borderline** |

- Threshold 3, the 16 moves classified with the diagnostics record (published candidate's terms):
  - 3 route detours the engagement field counts as approach (progress term +20 to +60): 1122 Gravekeeper, 1128 and 1132 UndeadCannonCart. The corpus metric uses static terrain and ignores the player units that block the short way.
  - 7 moves from one attack position to another followed by an attack (field cost 0 → 0, follow-up term 250–600): Demons on 1120, 1121, 1122, 1123, 1129, 2201 and a Heavy on 1122.
  - 1 move to a next-turn kill position at unchanged field cost (next-turn term 1,200): 1123 `HoldBack` Gravekeeper.
  - 5 steps out of a threatened cell (Hold player risk −108 → 0) at a cost of one or two field steps (progress −20 / −40): 1123 `HoldBack` and `Spread` Gravekeepers (two of them keep a 1,200 next-turn kill), 1133 Gravekeeper, 2135 and 2299 Gravekeeper.
  - None is unexplained; the 5 threat-aversion steps are the only moves that increase the AI's own route cost (0.8% of moves). Whether to accept, to measure retreats with the field instead of static terrain, or to change the threat aversion value is the user's decision.
- `phase-7-service`, five runs on the final code (runs 1–2 are the R-5 runs; per band by fixture enemy count, R-0 regrouped the same way):

| Run | Updates | p95 ms | p99 ms | max ms | > 4 ms | ≤ 5 p99 / max | 6–8 p99 / max | > 10 p99 / max | Decision latency p95 ms |
|---|---|---|---|---|---|---|---|---|---|
| R-0 | 3,166 | 1.030 | 1.083 | 9.57 | 6 | 1.141 / 9.57 | 1.069 / 3.97 | 1.065 / 8.19 | 9.66 |
| 1 | 3,706 | 1.044 | 1.178 | 9.96 | 7 | 1.184 / 7.13 | 1.205 / 9.96 | 1.106 / 3.89 | 12.62 |
| 2 | 3,685 | 1.035 | 1.116 | 9.07 | 7 | 1.095 / 8.56 | 1.131 / 9.07 | 1.081 / 1.33 | 13.35 |
| 3 | 3,566 | 1.027 | 1.072 | 7.89 | 3 | 1.058 / 4.06 | 1.117 / 7.89 | 1.041 / 1.25 | 12.14 |
| 4 | 3,541 | 1.033 | 1.167 | 11.37 | 9 | 1.106 / 8.90 | 1.289 / 11.37 | 1.063 / 4.02 | 12.04 |
| 5 | 3,863 | 1.036 | 1.166 | 12.14 | 10 | 1.175 / 8.76 | 1.166 / 12.14 | 1.156 / 8.82 | 13.72 |

  The cache-off controls of R-5 (6–8 p99 1.119 / 1.121) sit inside the same spread, so the rise is not from the cache. More decisions per side (560 vs 480, enemies now act) and larger precise previews since R-1 make the slices longer; single updates now reach 12 ms (R-0 9.6 ms) and updates over 4 ms range 3–10 per run (R-0 6). The refinement passes by the median and fails by the mean; the user decides how to read it (or whether to investigate the long atoms first).
- Stop rule: respected throughout (first-decision work 1.20× R-0 at most).
- Spec: implementation memo added (section 14), including what the artifact checks mean. Manual Unity checklist: one item added for visible hitches.
- Side-effect files (FMOD catalog, font SDF assets, sequences settings) shown and restored after the runs.
- Open decisions for the user (all decided in the R-6 follow-up below): threshold 3; how to read the threshold 6 refinement; whether to keep the low-value goal-set and forecast caches; the non-prepared path still uses default tuning settings. The user commits (R-4, R-5 and R-6 are uncommitted).

### R-6 follow-up (2026-11-10, user decisions after R-6; thresholds 3 and 6 now pass)

- Decisions (user, 2026-11-10, after the R-6 report): (1) threshold 3 is judged by the AI's own route cost instead of static terrain; the static metric stays in the corpus for reference. (2) The goal-set cache is removed; the forecast and threat-field caches stay.
- Code:
  - `E/EnemyAttackReach.cs` and `E/EnemyEngagementField.cs` back to their R-4 state (the goal `ReadSet`, the per-actor memo and the lazy target preparation are gone; the field builds every actor's goals as without a cache).
  - `E/EnemyDecisionSideCache.cs`: goal entries, `GoalsFor` and the goal counters removed; the class comment records why.
- Tests (all GwonCheonmyeong / R-5, adjusted to the removal; no expectation of a kept behaviour changed):
  - `T/EnemyDecisionSideCacheTests.cs`: `CacheOnOffIdenticalTraces` no longer sums goal counters; `DigestInvalidation` loses its goal-set part (target move, target facts, terrain, destination modifier, attacker restriction, ally death and move, pull bonus, per-actor memo); `CacheScopedToSide` checks forecast and threat hits only.
  - `T/CoreIntegration/EnemyStageBehaviorCorpus.cs`: the production identity test asserts threat-field reuse instead of goal reuse; cache counts are forecast and threat only. New field-based retreat metric: a move is a field retreat when the published candidate's progress term is negative (its own engagement-field cost rose); the hazard-origin exclusion is the same as for the static metric. New columns `fieldRetreat` (decisions), `fieldRetreats` / `fieldRetreatsNoHazard` (runs and the summary) and `fieldRetreats/moves` in the per-run table; threshold 3 reads `fieldRetreatsNoHazard`.
- Focused run: `EnemyDecisionSideCacheTests` and the whole `EnemyStageBehaviorCorpus` class, 11 / 11 pass. Identity: fixtures 1,665 decisions, production stages 990 decisions identical with and without the cache (reuse unchanged from R-5: fixtures forecast 1,245 / threat 375, production forecast 762 / threat 537).
- Full suites: 6,547 tests, 60 failures (Core 25, CoreIntegration 35), identical by name and message to R-6 and the R-0 baseline; no compile error.
- Stage corpus (no loadout; artifact and item runs alike): behaviour identical to R-5 (idle 182 / 569, moves 627, attacks 269, player deaths 76, hold reasons unchanged). Work 22,110,435 → 22,190,957 (+0.36% from the removal; R-4 24,269,777, so −8.6% overall); mean first-decision work 22,103 → 22,106 (the R-4 value: the lazy target preparation went with the memo).
- Threshold 3 by the field metric: 12 / 627 = 1.91% (Item 12 / 625 = 1.92%): **pass**, with little margin. Static terrain for reference: 16 / 627 = 2.55% (unchanged). Field retreats in all 18 (6 start on a hazard cell and are excluded). The 12, from the record:
  - 5 steps out of a threatened cell with nothing else gained (Hold player risk −108 → 0, progress −20 / −40): 1133 `Spread` Gravekeeper, 2135 and 2299 Gravekeeper in `HoldBack` and in `Advance`.
  - 4 steps out of a threatened cell that keep a next-turn kill (player risk −108 or −83 → 0, next-turn term 350 or 1,200 kept): 1123 `HoldBack` Gravekeepers #4 and #8 and Heavy #6, 1123 `Spread` Gravekeeper #4.
  - 3 moves to a next-turn kill position one to three field steps back (next-turn term 80 or 140 → 1,100 or 1,200): 1123 `Advance` Heavy #6, 1125 Gravekeeper #7, 1129 `Spread` Heavy #3.
  - Correction to the R-6 record: it estimated 5 field retreats (0.8%) by looking only at the 16 static retreats. The field metric also counts 7 moves the static metric does not (moves that do not get farther from every player unit on static terrain while their own route cost rises, for example 2135 / 2299 `Advance` and the three next-turn kill positions).
- Not rerun: `phase-6-corpus` and `phase-6-atoms` (their harness passes no cache, so the removal does not change them).
- Threshold 6 refinement (decision: user, 2026-11-10, after this follow-up): judged by the median of at least five `phase-7-service` runs on the final code, because one run varies by close to ±10% and the R-0 value is a single run. Five new runs on the final code (goal-set cache removed); per band by fixture enemy count, R-0 regrouped the same way:

| Run | Updates | p95 ms | p99 ms | max ms | > 4 ms | ≤ 5 p99 / max | 6–8 p99 / max | > 10 p99 / max | Decision latency p95 ms |
|---|---|---|---|---|---|---|---|---|---|
| R-0 | 3,166 | 1.030 | 1.083 | 9.57 | 6 | 1.141 / 9.57 | 1.069 / 3.97 | 1.065 / 8.19 | 9.66 |
| F1 | 3,581 | 1.027 | 1.088 | 8.23 | 7 | 1.088 / 8.23 | 1.092 / 8.11 | 1.070 / 3.80 | 12.37 |
| F2 | 3,684 | 1.032 | 1.123 | 9.35 | 8 | 1.110 / 7.56 | 1.146 / 9.35 | 1.110 / 3.54 | 12.67 |
| F3 | 3,660 | 1.031 | 1.128 | 9.47 | 8 | 1.087 / 7.77 | 1.144 / 9.47 | 1.045 / 4.07 | 12.42 |
| F4 | 3,868 | 1.045 | 1.266 | 9.39 | 11 | 1.081 / 7.12 | 1.293 / 8.13 | 1.453 / 9.39 | 13.63 |
| F5 | 3,910 | 1.039 | 1.128 | 9.81 | 10 | 1.089 / 8.90 | 1.153 / 8.64 | 1.139 / 9.81 | 15.30 |

  - 6–8 p99: median 1.146 ms = +7.2% over R-0 1.069 (limit +10% = 1.176): **pass**. Mean 1.166 (+9.0%); one run of five (F4, 1.293) exceeds the limit. No 9–10 fixture exists. Service preparation p95 1.027–1.045 ms (≤ 1.1): pass.
  - Per-update max reported per group: 6–8 8.1–9.5 ms (R-0 3.97 ms); overall 8.2–9.8 ms (R-0 9.57 ms); updates over 4 ms 7–11 per run (R-0 6). The 12 ms update seen in R-6 run 5 did not recur. The longer single updates in the 6–8 group are left to the separate performance investigation (session "Find the long enemy-turn preparation steps", started by the user 2026-11-10); they do not block this plan.
- Side-effect files (FMOD catalog, font SDF assets, sequences settings) shown and restored after the runs.
- With this, every R-0 threshold is judged and passes. Left for the user: the manual Unity checklist and the commit (R-4, R-5, R-6 and this follow-up are uncommitted). The non-prepared path still uses default tuning settings (kept as recommended; spec §14).

### Long preparation updates investigation (2026-11-10, side session; investigation only, no runtime change)

Question (manual checklist, last item): which preparation steps make the long Steps (`phase-6-atoms`) and the long service updates (`phase-7-service`, up to 12 ms against R-0 9.6 ms), and why.

- Method:
  - Existing raw files regrouped: R-0 (`phase-6-atoms`, `phase-7-service`), R-4 and R-5 atoms, the R-5/R-6 service runs and the five F runs above.
  - New runs on the current code (`6185a55b1` plus the uncommitted R-4 to R-6 follow-up): `phase-6-atoms` once, `phase-7-service` twice (P1, P2).
  - A temporary CoreIntegration probe (`ZzTempEnemyAtomProbe`, deleted afterwards; a copy is in the side-session notes) ran the service order on the same four fixtures and seeds: first atom = `AssembleDecision` + session construction, then `Step` until 1 ms, `yield`, next frame `AdvancePrepared` + publish. It records every Step's duration, phase before/after and `GC.CollectionCount(0)` change; a second pass records the `GC.GetTotalMemory` change per atom. Repeats 0–3 use identical inputs, so the minimum over the four repeats is a Step's structural cost and the spread above it is environment.
  - Runtime facts read by the probe: incremental GC on (`gcIncremental: 1`), `GarbageCollector.incrementalTimeSliceNanoseconds` = 3,000,000 (3 ms).
- Finding 1 — every update over 4 ms is a GC pause, at R-0 and now:

| Runs | Updates over 4 ms | With a completed collection (6–12 ms) | One 3 ms incremental slice (about 4.0–4.1 ms, largest atom 3.0–3.2 ms) | Other |
|---|---|---|---|---|
| R-0 (1 run) | 6 | 5 | 1 | 0 |
| Current code (15 runs: R-5 ×2, R-5 cache-off ×2, R-6 ×3, F ×5, P ×2) | 3–11 per run, 124 in total | 98 | 20 | 6, all GC-shaped: largest atom 5.9–7.0 ms without a completed collection (two slices back to back), or a 4.11 ms update whose largest atom is 0.01 ms (a slice outside the atoms) |

  - The per-update maximum is the largest GC pause of the run: R-0 9.57 ms, current runs 7.89–12.14 ms (median 9.5). Execute frames (`AdvancePrepared` + publish; the harness subtracts this execution section from the preparation metric) show the same pauses: R-0 max 9.42 ms, current runs 3.97–12.94 ms.
  - The long warmed Steps of `phase-6-atoms` are the same thing: R-0 10 Steps over 1 ms (4 with a collection at 6.5–7.7 ms, 5 slices at 3.06–3.12 ms, one 1.40 ms), R-5 5 (2 / 3), current code 7 (4 collections at 7.2–8.0 ms, 3 slices at 3.08–3.13 ms). Their phase labels (`UtilityCandidates`, `MoveRoute`, `UtilityThreat`, `Admission`, …) name whichever Step was allocating when the collector ran, not a builder that does that much work.
  - GC pauses spread over the fixtures by allocation volume and are not tied to one input (current runs, measured repeats: N5 52, N6 38, N8 54, G16 52).
- Finding 2 — no preparation Step is structurally long:
  - Probe, 4,409,712 Steps over 560 decisions: the largest structural cost (minimum over the four repeats) is 0.060 ms; 29 Steps exceed 0.05 ms (all `Attack/SacrificeSource` → `Attack/SacrificePlan`, in precise attack assessment and in the follow-up assessment); **none exceeds 0.1 ms**. The 263 Steps that exceeded 0.1 ms in some repeat (max 1.26 ms) were short in the other repeats of the same input: preemption or core scheduling (the machine is a hybrid i7-14700), not code.
  - Other atoms: first atom (input assembly) p50 0.053 ms, p99 0.167 ms, max 0.30 ms; session construction about 0.001 ms; side-cache read sets (forecast and threat capture + compare, measured separately on the same inputs) p99 ≤ 0.04 ms, max 0.087 ms. The threat read set matched in 320 of 480 later decisions; the others differ in player facts.
  - So the builders (threat field, engagement field, precise previews, forecast, follow-up) all yield at fine granularity; no single yield covers too much work.
- Finding 3 — why the per-update p99 moves between runs: an update is the atoms until the 1 ms budget is spent, so its length is 1 ms plus the overshoot of the atom that crosses the budget. In the 6–8 band (1,600–1,900 updates) the top 1% is 16–19 updates, and 6–10 of them are GC-affected in every run; the rest are noise-lengthened crossing atoms. On the same code the 6–8 p99 ranges 1.092–1.293 ms over ten runs and its GC-free part 1.075–1.237 ms; the probe, a few minutes before P1 and P2, had a GC-free 6–8 p99 of 1.060 ms (R-0, single run: 1.062). The 6–8 maximum of R-0 (3.97 ms) is a slice; R-0's collections happened to land in the N5 and G16 fixtures. The code influences the p99 through the number of GC pauses per run, not through atom size.
- Finding 4 — allocation (probe, current code, service order, Boehm estimate from `GC.GetTotalMemory` changes in atoms without a collection): about 940 KB per enemy decision.

| Where | Share | Note |
|---|---|---|
| `UtilityCandidates` (engagement field per actor, move BFS, cheap score per landable cell) | 43% | About 390 KB per decision. The field's reach check per (target, cell) creates an iterator, a closure and a delegate (`reach.FromSteps(cell, value => goal = value)`); the BFS creates a `Node` per visited cell and an `EnemyScoreFacts` per landable cell in `CheapScore` |
| Follow-up assessment (`UtilityFollowup`, `UtilityFollowup/*`) | 14% | |
| Action execution (`AdvancePrepared` + publish) | 13% | Outside the AI redesign |
| Attack assessment (`Attack/*`, `Admission`, `Visibility`, `EffectCoverage`) | 11% | |
| `MoveRoute`, `MoveCompletion` (precise move preview) | 8% | |
| `UtilityOrder` (pass-1 attack candidates of every actor, R-4) | 7% | |
| `UtilityThreat` (threat field, geometry, targets) | 2.6% | |

  - Completed collections per 100 decisions: current code 113 / 7,840 = 1.44 over 14 runs; R-0 6 / 480 = 1.25 (too few events to tell them apart). A run now holds 560 decisions instead of 480 because enemies act, so it holds more pauses (updates over 4 ms: median 8 against 6).
- Finding 5 — an execute frame is one unsliced unit per action: structural cost (minimum over repeats) 0.66–0.95 ms (G16 0.86–0.95 ms), the same as at R-0 (median 0.74 ms then, 0.68–0.75 ms now). It is not in the preparation metric but is part of what a player sees in that frame.
- Proposals (the user chooses; none is implemented):

| # | Change | Where | Expected effect | Risk |
|---|---|---|---|---|
| 1 (recommended first) | Report GC separately in `phase-7-service`: per band the GC-affected update count, the GC-free p99, collections per 100 decisions, and execute-frame time next to preparation. Judge slicing by the structural atom (probe-style minimum over repeats, limit for example 0.1 ms) instead of the raw p99 of one run | Test code only (`EnemyAdvancePreparationMeasurements`, GwonCheonmyeong) | Stable verdicts; the current code passes structurally (max 0.060 ms). Makes the manual hitch check readable | None at runtime; changes how threshold 6 is read (user decision) |
| 2 | Cut allocation in candidate generation: a synchronous reach check per cell with a yield between cells (no iterator/closure per check), reuse the field's sets and dictionaries across the actors of one decision, make the cheap-score inputs a value type or reuse them. First measure inside `UtilityCandidates` with finer phase labels or Profiler `GC.Alloc` samples | Core (`EnemyEngagementField`, `EnemyAttackReach`, `EnemyActionCandidates`, possibly `EnemyScoreFacts`) | If candidate allocation halves, total −20%, so about 20% fewer GC pauses per enemy side; each pause stays 3–12 ms in the Editor | Low to medium: determinism and identity are guarded by the identity tests (partitions 1/2/31, cache on/off); a value-type `EnemyScoreFacts` touches the scorer's single formula owner, so the formula must stay in one place |
| 3 | GC policy: a shorter incremental slice (for example 1 ms via `GarbageCollector.incrementalTimeSliceNanoseconds`) and/or `GarbageCollector.CollectIncremental` in frames with slack (View playback, player turn) so an enemy side starts with heap headroom; check pause length in a Player build, where the heap is much smaller than in the Editor | Outside Core (boot or Framework, JMH) | The 4 ms slice updates become about 2 ms; fewer completions during enemy sides | Global, affects every system; owner's decision |
| 4 | Treat the execute frame (0.7–0.95 ms structural, 13% of allocation) as its own item if frame time matters | Core combat rules (not the AI) | Small; it is already under 1 ms structurally | Separate work |

  - Not recommended: a smaller per-frame budget or more yields. Atoms are already ≤ 0.06 ms structurally; the remaining overshoot is noise and GC.
- Manual checklist note: hitches seen in Editor Play mode during enemy sides are expected to be GC pauses of the same kind as at R-0, somewhat more per side because enemies now act more. A Profiler capture with `GC.Collect` / `GC.Alloc` markers confirms it.
- Unity side-effect files (FMOD catalog, two font SDF assets, sequences settings) were shown and restored after each run. The probe file and its `.meta` were deleted; no runtime code changed. Scratch outputs (probe CSVs, P1/P2, `phase-6-atoms`) are in the side session's scratchpad; the analysis scripts are in the side-session notes folder.
- **Proposal 1 implemented (2026-11-10, user choice "1번 측정 개선"; test code only, runtime unchanged).** `T/CoreIntegration/EnemyAdvancePreparationMeasurements.cs` (GwonCheonmyeong):
  - `CaptureYieldedOpenEncounters` (`phase-7-service`), behaviour and publication checks unchanged:
    - new last column `gcAffected` in `prepared-frames.csv`;
    - `incrementalGc`, `gcSliceNs` and the GC rule in `prepared-runtime.txt`;
    - new `prepared-summary.tsv`, also printed to the test output. Per band (≤ 5, 6–8, 9–10, > 10, all): updates, p95, p99, max, over 4 ms, GC-affected, over 4 ms that are GC-affected, GC-free p99 / max, execute frames p50 / p95 / p99 / max and GC-free max, decisions, collections, collections per 100 decisions.
    - Rule: an update is GC-affected when the collection count changed, a preparation atom is ≥ 90% of the GC slice, a preparation update is ≥ budget + 90% of the slice (a slice outside the atoms), or an execution is ≥ 90% of the slice. With incremental GC off, only collections count. Percentiles are nearest-rank, the same as the earlier `p7.sh` regrouping. Preparation values are computed as before, so the earlier records stay comparable.
  - New explicit test `CaptureStructuralAtoms` (capture `phase-7-atoms`; same fixtures and seeds; one warm-up side and four measured sides per seed). It runs the service order with the side cache, without frame yields: first atom = `AssembleDecision` + session construction, then every `Step`, then `AdvancePrepared` + publish.
    - It requires the four measured repeats of an input to give the same atom sequence; the test fails otherwise.
    - It keeps each atom's minimum over the repeats (structural cost) and maximum.
    - Outputs: `structural-summary.txt` (also in the test output), `structural-steps.csv` (top 200 Steps by structural cost), `structural-runtime.txt`.
    - It reports values and does not assert a limit; the limit for threshold 6 is still the user's to set (suggested: structural Step ≤ 0.1 ms).
- Results on the current code (Unity batch; compile clean after fixing two compile errors of the first draft; both tests pass):

| Capture | Result |
|---|---|
| `phase-7-atoms`, run 1 / run 2 | 1,102,428 Steps over 140 decisions. Structural Step p99 0.0044 / 0.0046 ms, max 0.0438 / 0.0481 ms (`G16` seed 7 decision 1, `Attack/SacrificeSource` → `Attack/SacrificePlan`); Steps with structural cost > 0.05 ms 0 / 0. Steps over 0.1 ms in some repeat 343 / 185 (environment). First atom structural p99 0.056 / 0.060 ms, max 0.057 / 0.064 ms. Execute frame structural p50 0.61 / 0.62 ms, max 0.85 / 0.98 ms (G16 highest) |
| `phase-7-service` (new summary), one run | All bands: 3,745 updates, p95 1.053, p99 1.279, max 11.25 ms; over 4 ms 14, all 14 GC-affected; 28 GC-affected updates; GC-free p99 1.183 ms. 6–8: p99 1.288, GC-free p99 1.209, GC-free max 3.05 ms, 12 GC-affected. 12 collections, 2.14 per 100 decisions. Execute frames p50 0.70, p99 2.97, max 3.63 ms |

  - The structural capture gives the same verdict on two runs (max 0.044 / 0.048 ms). In the same minutes, the one service run had a 6–8 p99 of 1.288 ms (GC-free 1.209 ms), against 1.092–1.293 ms over the ten earlier runs of this code, so the raw p99 of a single run stays a poor acceptance measure.
- Decision (user, 2026-11-10, "구조 기준으로 변경"): the threshold 6 refinement is read from `phase-7-atoms`: the largest structural Step is at most 0.1 ms. `CaptureStructuralAtoms` now asserts it (constant `StructuralStepLimitMs`); the first atom (input assembly + session construction) and execute frames are reported, not limited. `prepared-summary.tsv` stays as context. The threshold text at the end of R-6 is updated.
  - Run after adding the assertion: pass; structural Step max 0.0481 ms (`G16` seed 7 decision 1, `Attack/SacrificeSource` → `Attack/SacrificePlan`), 0 Steps over 0.05 ms, 218 Steps over 0.1 ms in some repeat (environment). Threshold 6 refinement: **pass**.
  - Side-effect files shown and restored after the run.

### Forced-movement attacks fix (2026-11-10, after the user's manual check; user decision "A안 정밀 평가", then "원천적으로 차단")

- User report from the manual check: the enemy Rider (Gravekeeper, `Unit_Gravekeeper`: Fan / Direction, range 1, attack 11, knockback trait 1 cell) approaches but keeps standing diagonally and almost never attacks; Heavy, Demon and Cannon are fine. The user also asked that no other unit with knockback can hit the same problem.
- Evidence (R-6 follow-up corpus, no loadout): Gravekeeper **0 attacks** over 18 stages (175 moves, 22 actor-sides that could attack from their cell, 174 with a reachable attack); Demon 146, Heavy 90, Cannon 33 attacks. All 118 Gravekeeper attack candidates had an attack term of −11,650 to −20,720, coarse (`precise False`); hold reasons included `WorldUncertainty` 15 times.
- Cause: two conservative paths that R-1 kept on purpose (spec 6.2 "knockback-type rules keep the existing acting-attack condition"):
  1. `CombatCommandEffects.AssessmentCoverageSteps` (enemy mode): `KnockbackEffectRule` declares harm `ActingAttackOnly` but no coverage, so for the acting attack it added `UnsupportedEffect` and unbounded allied harm. That blocked the precise preview and charged the full current-HP loss of **every** living ally (×3 in the scorer). The undeclared `ImpactBoltEffectRule` and the `ActingAttackOnly` `PullEffectRule` hit the same path.
  2. `CombatActionAssessment`, for attacks that can force movement: `MovementEnvironmentCalculator.AssessmentCoverageSteps` flagged every active arrival tile on the **whole board** as unsupported and unbounded, and obstacle scoping seeded **every** powder barrel.
  - So any enemy whose own attack pushes or pulls was effectively unable to attack; between attacks it stood where it could strike next turn with the least threat (diagonal cells).
- Facts checked: the attack calculation, and so the precise preview, executes the forced movement completely (`AttackForcedMovement`: admission, terrain fall/weak ground, thorns, tornado entry, object arrivals). A blocked knockback stops without collision damage.
- Code (enemy assessment only; real combat, the immediate-compatibility path and the threat field are unchanged):
  - `A/CombatCommandEffects.cs`: in enemy mode, rules whose harm happens only inside the acting attack's resolution are left to the precise preview: knockback-capability rules (`IEffectKnockbackRule`) and rules declaring `EffectAssessmentHarmScope.ActingAttackOnly` (today Knockback, ImpactBolt, Pull). They add no gap, no allied-harm ceiling and no projection block. Rules that declare coverage gaps keep their declaration. Capability-based, so a future knockback-type or acting-attack-only rule gets the same treatment without a list to update.
  - `A/CombatActionAssessment.cs`: the forced reach of an attack = the pull path (with the pulled unit's footprint) and the cells within the knockback distance (Chebyshev) of every selected target's cells. The distance comes from the same evaluator the attack uses (`CombatEffectEngine.EvaluateKnockback`, longest contribution, as in `AttackKnockbackCalculator`); the push direction is not recomputed. Arrival coverage and powder-barrel seeding look only at that reach.
  - `A/MovementEnvironmentCalculator.cs`: new `ForcedEntryAssessmentCoverageSteps` with the same rules as the route check: a known mine in reach exposes only the units in its blast; any other arrival source in reach keeps the conservative ceiling; a pushed unit touching a tornado makes the landing unknown, so every arrival source counts. The blast exposure is shared with the route check (`BlastExposureSteps`, `AddMineBlast`). The whole-board `AssessmentCoverageSteps` and `TornadoCalculator.AssessmentCoverageSteps` are removed (no caller left).
  - In coarse (non-precise) estimates the forced movement itself is not charged (same rule as other effects the AI does not model, spec 6.2); the chosen attack is assessed precisely when the preview budget allows.
- Tests:
  - New `T/EnemyForcedMovementAssessmentTests.cs` (+ `.meta`): `OwnForcedMovementAttackIsAssessedByPreview` (Knockback, ImpactBolt: precise, one preview, positive attack benefit), `ArrivalSourceOutsideForcedReachDoesNotBlockPreview`, `MineInsideForcedReachChargesOnlyBlastAllies` (possible harm 1000 = the one ally in the blast, not every ally).
  - New pinned CoreIntegration test `EnemyStageBehaviorCorpus.EveryEnemyKindAttacksWhenItCan`: on every production stage (`Advance` and `HoldBack`, no loadout), every enemy unit kind that could attack from its cell at a side start attacks at least once. It catches the whole class (a unit kind that never attacks), whatever the cause.
  - Intended expectation changes in existing tests (all GwonCheonmyeong):
    - `EnemyEffectHarmScopeTests.OwnerAttackForcedMovementRetainsConservativeHarm` → `OwnerAttackForcedMovementIsLeftToPrecisePreview` (Knockback, Pull: gap `UnsupportedEffect` + ceiling 4000 → no gap, 0).
    - `EnemyEffectHarmScopeTests.UndeclaredKnockbackCapabilityKeepsLegacyFallback` → `UndeclaredKnockbackCapabilityIsLeftToPrecisePreview` (ImpactBolt: 2000 → 0; real-mode checks unchanged).
    - `EnemyIgnoredSourceTests.KnockbackConditionUnchanged` → `KnockbackLeftToPrecisePreviewInEnemyMode` (enemy mode: no gap, no harm; real mode still gap + harm, now asserted).
    - `EnemyActionAssessmentTests.UnknownRuleIsPartial` → `UnmodelledForcedArrivalIsPartial` (knockback alone is no longer partial; a mine in reach with no mine rule passed keeps the partial, no-preview result the test checks).
    - `EnemyFollowupEstimateTests.CoarseAttackRetainsDirectAndUnboundedAlliedHarm` → `CoarseAttackRetainsDirectAlliedHarmAndKnockbackFollowupIsPrecise` (knockback follow-up: ≤ 0 with no preview → positive with one preview; the splash direct-harm part unchanged).
  - RED: not run separately; the corpus record above (0 Gravekeeper attacks) is the failing evidence for the pinned test.
- Spec: §14 (6.2) updated.
- Verification (Unity batch, code base HEAD `94adff2b8` = the user's `d527a001d` "적 수치 조절 …" plus a develop merge, with this change on top):
  - Full `ProjectL.Tests.Core` + `ProjectL.Tests.CoreIntegration`: 6,553 tests, 63 failures. The 60 known failures are identical by name and message to the R-6 follow-up run. Three new:
    1. `EnemyFollowupEstimateTests.CoarseAttackRetainsDirectAlliedHarmAndKnockbackFollowupIsPrecise`: `ArgumentException` "메인 수집 입력이 지속 원본과 다릅니다" from soul collection when the precise preview pushes the main. Fixture inconsistency exposed by the preview now running: `World()`'s main has attack 15 while its persistent state has attack 1. The test now sets the main's persistent state from the character (comment in the test); it passes. Production inputs are assembled from one source and are not affected.
    2. `EnemyAiTuningAssetTests.TuningAssetLoadsAndDefaults`: expects the production tuning asset 50/50/50; the user's commit `d527a001d` set `nextTurnApproachPercent` to 150. Not caused by this change.
    3. `EnemyStageBehaviorCorpus.Stage1127DemonStepsNextToHammerAndAttacks`: with the `d527a001d` stats (Cannon attack 6 → 18, range 2 → 3; Demon 6 → 10; Gravekeeper 7 → 11; Heavy 4 → 8; HP changes) the 1127 `Advance` battle ends in round 2 (Cannon #4 kills the 10 HP main), so rounds 3–4 never happen. The Gravekeeper's round-1 move is the same as before ((3,8) → (3,4)) and the battle ends before its round-2 turn. Not caused by this change.
    - Items 2 and 3: fixed to the new stats on the user's instruction ("새 수치에 맞게 수정", 2026-11-10):
      - `EnemyAiTuningAssetTests.TuningAssetLoadsAndDefaults`: the production asset is now pinned at (50, 50, 150); the contract defaults (50/50/50) stay checked by the "no table" case in the same test. The pin has to follow any later asset change.
      - `Stage1127DemonStepsNextToHammerAndAttacks`: the Demon part (rounds 1–2, move then attack) stays on the advancing battle and still passes. The back-line part (a Gravekeeper or Heavy attacks) moved to the holding battle (`advancePlayers: false`), any of rounds 1–4, because the advancing battle now ends in round 2. In the holding battle the Gravekeeper moves (3,4) → (5,2) and attacks with knockback in round 2, so the test also pins the forced-movement fix on a production stage. Loop extracted into `PlayStage1127Rounds`.
      - Run: both pass, with the other two 1127 pinned tests (4 / 4); side-effect files restored.
  - Focused run (after the fixture fix): `EnemyFollowupEstimateTests` 35 / 35, `EnemyForcedMovementAssessmentTests` 4 / 4, `EveryEnemyKindAttacksWhenItCan` pass (Gravekeeper: 3 actor-sides able to attack from its cell, 18 attacks; Demon 24 / 36; Heavy 17 / 28; Cannon 7 / 10), `DumpStage1127DecisionRecords` pass.
  - Stage corpus (no loadout; current stats, so not comparable one-to-one with earlier phases): Gravekeeper 26 attacks / 84 moves (R-6 follow-up, older stats: 0 / 175). Its 31 attack candidates score +200 to +3,000 (18 precise; earlier −11,650 to −20,720, none precise). Thresholds: 1 loadout independence holds (None / Artifact / Item identical); 2 refined idle 2 / 154 = 0.013; 3 field retreats 2 / 305 = 0.66%; 4 enemy hazard deaths 0; 5 first contact ≤ round 2 on every regular stage for every script, bosses round 1; 6 mean first-decision work 22,683 = 1.15× R-0 (stop rule 1.5×). With the stronger enemies and players that never attack, most battles end in rounds 1–2 (246 of 248 held reachable actor-sides are `NotEvaluated`), so the corpus now measures less than before.
  - `phase-7-atoms` (threshold 6 refinement): pass, largest structural Step 0.0434 ms (`Attack/SacrificeSource` → `Attack/SacrificePlan`), none over 0.05 ms.
  - Unity side-effect files (FMOD catalog, two font SDF assets, sequences settings) restored after the runs.
