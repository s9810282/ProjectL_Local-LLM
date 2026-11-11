# Enemy AI Engagement Progress and Follow-up Assessment Design

**Date:** 2026-11-09  
**Status:** Phases A and B separately authorized and implemented on 2026-11-09; Phase C verification and measurements completed the same day; a large-map Hold regression found by measurement was fixed with a truncation fallback to the prior pursuit progress (manual playback not run). See the plan's completion record for scoped evidence.  
**Plan:** [Phase and Task plan](../plans/2026-11-09-enemy-ai-engagement-followup-plan.md)

## 1. Purpose and observed behavior

Make useful approach moves receive consistent value, and stop an unrelated Regeneration effect from removing the entire move-then-attack benefit. Keep the current Utility selector, actual command execution, and publication pipeline.

The user reported frequent waiting on several maps, particularly around railways, even after the previous assessment-boundaries Phases A–D. Inspection used HEAD `c995dd4555f3b657f7605eeddf8d422650e9d7db` plus the current uncommitted A–D changes. Their completion does not establish that this new behavior is fixed.

The separate pure-Core diagnostic at `C:\CodexHome\visualizations\2026\11\09\01a1bf48-920a-75d3-94ee-86e1b65ad95d\hold-diagnosis\trace.jsonl` reproduced:

| Controlled case | Current result | Causal source |
|---|---|---|
| Three-step representative pursuit ends on a lethal rail; two-step and one-step safe approaches exist | Representative move scores -3,940; safe approaches and Hold score 0; enemy phase ends | `EnemyActionCandidates.Progress` rewards only `_pursuitPath.Endpoint` |
| Nonlethal move-then-attack opportunity, no Regeneration | Progress +60, follow-up +200, player threat -150; move scores +110 | Existing precise movement and follow-up path |
| Same opportunity with Regeneration | Follow-up disappears; move scores -90; Hold wins | `CombatActionAssessment` stops at unsupported coverage; `EnemyDecisionSession` requires `MovedFacts`, then requires precise attack facts |

These are controlled reproductions, not an exact replay of the user's live battle. The diagnostic did not establish Unity scene or View acceptance.

## 2. Scope and alternatives

The user requested documents for the recommended two corrections, followed by phase-based implementation, and subsequently authorized Phase A including its 256-anchor bound, then separately authorized Phase B's projection and follow-up changes. Phase C requires separate authorization.

| Approach | Tradeoff |
|---|---|
| Increase aggression, reduce railway penalties, or punish repeated Hold | Small patch, but masks missing benefit and can encourage lethal moves. Excluded. |
| Shared wall-aware approach distance plus bounded reuse of current movement projection and attack estimation | Corrects the two demonstrated omissions inside the existing decision pipeline. Recommended. |
| Search enemy move, player response, and later enemy turns | A different planner, substantially larger simulation and performance scope. Excluded. |

The horizon is preparation for engagement and one immediate attack after movement, potentially in the **same enemy phase**. Approach distance is a heuristic, not a simulated next turn. Do not forecast player responses, future healing, or a multi-turn attack sequence.

Non-goals: target-policy changes, attack shortlist reordering, role-specific aggression, retaliation memory, repeated-Hold penalties, threat/defense formula changes, globally supporting all traits, Dust Screen harm reclassification, production map edits, and solving the existing performance backlog. Safe Hold remains valid.

## 3. Preserved contracts

- Preserve public APIs, Buffer schemas, result/event order, source stamps, cancellation, exactly-once publication, and actual combat rules.
- Preserve ascending actor order, mandatory Taunt, deterministic intent ties, strict candidate-over-Hold selection, and rejection continuation.
- Preserve existing limits: four attacks, four moves, two refined moves, one follow-up aim per refined move, eight Preview children per actor, sixteen per decision, and 256 alternate-route expansions. Respect lower configured limits.
- The additional approach map has its own fixed bound described in section 4. This adds work; unchanged existing limits do not imply unchanged execution cost.
- Retain `HiddenEnvironment`, all existing coverage flags, unknown-reaction fallbacks, possible allied-loss bounds, final-night handling, and forecast Bind handling.
- No new scheduler, persistent world state, service, registry, public setting, or runtime registration. No View, Flow, Plan, Job, Framework, DB, or authored asset changes.
- Use test-owned inputs. Do not rewrite another worker's existing test assertions without permission.
- Implementation documents are English; code comments and XML documentation are Korean. New C# files require Unity `.meta` files.
- The user owns commits/pushes. Stop after each authorized phase.

## 4. A — Consistent engagement progress

### A1. One metric per actor and selected target

Retain the existing pursuit target choice, including Taunt overrides and current unit/headquarters preference. Do not pick a different target for each candidate. Build one actor-local distance map for that target, even when an immediate attack candidate exists. Skip it when no movement or target exists, or in `followupOnly` candidate generation.

Let `d(c)` be the shortest number of cardinal steps from anchor `c` to **any attack-capable landing anchor for that target**, using the observed static traversal graph. Compute progress as:

```text
progress(move) = d(original anchor) - d(move endpoint)
approach score = clamp(20 * progress, -100, 100)
```

Use the same immutable map for origin and all candidates. Hold has zero progress. A shortest-path prefix of two steps normally earns +40, regardless of the representative pursuit endpoint. A wall detour can earn progress while increasing Manhattan distance. Equal-distance sidesteps earn zero; retreat increases remaining distance and earns negative progress. No per-turn state or repeated-movement reward is stored.

Already being in attack geometry means `d(origin) = 0`; moving between equally attack-capable cells earns no approach bonus. Other score terms may still justify movement.

### A2. Reuse geometry and traversal rules

Extract the existing nested `EnemyPursuitSearch.AttackReach` implementation to internal `EnemyAttackReach` in the same directory, preserving its logic and resumable calls. Both pursuit and the new map use this one owner. Keep `EnemyPursuitSearch.CanAttackFrom` as the existing wrapper. No new approximation of attack range, occlusion, directional aim, destination modifiers, or target occupancy.

Use `MovementRouteValidator.PursuitTraversal` for graph occupancy/traversal. The origin is a seed; remove the actor's old footprint through the existing traversal behavior, not by mutating characters. Keep existing Leap traversal rules, known-mine transit exclusions for ordinary movement, and mine/next-entry-collapse landing exclusions. Leap transit cells can be graph nodes without being valid goal anchors. Resolve goals through the shared attack-reach predicate, including its existing removal of Disarm for **future engagement geometry only**. Immediate attack admission in Phase B must still honor Disarm.

The graph models observed static reachability. It does not predict future unit movement, mine consumption, hazard timing, displacement, passenger exchange, or the landing schedule across multiple turns. Exchange-only endpoints outside this graph have unknown distance and receive zero progress. Actual candidate admission and execution remain authoritative.

### A3. Bounded construction and unknown results

Add a concrete internal `EnemyEngagementMap`; no interface or service is needed. Construct the origin's connected traversal component in deterministic Right/Up/Left/Down order. At most **289 distinct traversable anchors** may be retained (raised from 256 on 2026-11-09 so that the largest planned field, 17×17, completes on an empty board). Inspect at most four outgoing edges per retained anchor. If discovering another valid anchor would exceed the limit, mark the map truncated and return no distances for this actor. Do not score a partially explored component or fall back to Manhattan distance. On truncation, approach progress falls back to the prior rule: only the representative pursuit route endpoint receives its step count (added 2026-11-09 after measurement showed large-map Hold).

For a complete component, evaluate each eligible goal once with the shared attack-reach iterator, then run one reverse multi-source BFS over the component. Each anchor is dequeued at most once in that distance pass. This bounds stored anchors, goal evaluations, and distance expansions separately; it is not a total-CPU-operation claim. Every traversal edge, goal-query step, and distance expansion yields through the existing decision iterator. Do not call synchronous `CanAttackFrom` repeatedly inside one Step.

Both distances must be finite before awarding signed progress. No reachable goal, a truncated component, or an endpoint outside the component yields zero progress. Report truncation through existing cap diagnostics. An inaccessible nearest target remains a limitation of the unchanged target policy.

Use finite `d(endpoint)` in place of Manhattan target distance at the existing **distance position** in destination ranking; unknown sorts after finite distance. Keep the preceding hazard, entry-risk, threat, and engagement ordering, escape reservation, representative pursuit reservation, and move count. If the entire map is unavailable, use a neutral equal distance for all destinations, then existing step/coordinate ties. This prevents cutoff-dependent partial preference and makes the fallback deterministic.

Retain the existing pursuit route generator. The new map supplies scoring/ranking facts and does not replace route reconstruction or validation. Dispose map references with the actor's candidate builder; never cache across changed input/source stamps.

## 5. B — Preserve a bounded move-then-attack estimate

### B1. Separate projection eligibility from complete coverage

`NoAdditionalImmediateHarm` is **not** permission to invent a post-move world. A harmless effect can still change attack availability, damage, or status. Keep harm metadata independent.

Add optional internal `IEffectMoveProjectionRule : ICombatEffectRule` with `bool SupportsMoveProjection { get; }` in `CombatEffectCapabilities.cs`. A true declaration says the existing public movement projector represents this effect's immediate movement consequences sufficiently to build a follow-up input. It does not certify full action assessment, future boundaries, attack reactions, hidden information, or terminal outcomes.

Initially declare true **only on `RegenerationEffectRule`**. Its healing belongs to `RegenerationTurnApplication` at side-end boundaries; an immediate move/attack pair does not cross one. Already supported effects with zero assessment gaps retain their existing behavior without a new declaration. Leave every other unsupported effect undeclared until its own behavior and tests justify support.

Extend `CombatCommandEffects.AssessmentCoverageSteps` with an optional trailing `Action onMoveProjectionBlocked = null`. For each nonzero-gap effect, invoke it unless that rule explicitly supports move projection. Held environment, equipped/items effects, artifacts, and obstacle fallbacks always invoke it. A declaration for Regeneration must never clear a block contributed by another source. Continue yielding the exact existing gaps and invoking the independent unbounded-harm callback as before.

### B2. Project through the existing calculator

Inside `CombatActionAssessment.Work`, retain effect and route coverage provenance separately before combining gaps. Existing precise assessment remains unchanged. An additional **nonprecise movement projection** is allowed only when:

1. The move is one of the existing refined moves (`allowPrecise` is true), and admission already succeeded.
2. No effect source blocked movement projection and no unbounded-harm source was reported.
3. Route/environment coverage contains no additional gaps; only the enclosing `HiddenEnvironment` remains allowed. Random/unknown landing, unsupported arrival, exposed mine uncertainty, and unresolved passengers must not qualify.
4. The existing context successfully charges one Preview slot.

Reuse `CombatMovementCalculator.BeginProjectRoute`, projection completion, and `CombatCommandComposition.ProjectSteps`, exactly as the current precise movement branch does. Do not copy placement, action-use, terrain modifiers, damage, or status rules into an AI-specific simulator. Reuse this machinery within the existing Work iterator rather than executing the route twice.

For this added path, keep the original coarse `EnemyScoreFacts`, gaps, and harm bound. Set `MovedFacts` only for an accepted projection with a living field actor and no projected terminal result. `MovedFacts` becomes a conditional public follow-up input and no longer implies `Facts.IsPrecise`. Do not copy projected move losses into coarse facts and then charge `EntryEstimateSteps` again. Existing coarse entry/hazard charges remain once. The temporary input is never published or retained across decisions.

This initial support resolves the demonstrated Regeneration blocker, including remote Regeneration. It intentionally does not guarantee follow-up evaluation for every unsupported trait or arrival rule.

### B3. Reuse coarse attack estimation when precision is unavailable

Extend `EnemyDecisionSession.FollowupSteps` with `bool preciseMove` supplied from the movement assessment. Keep one aim and the existing follow-up candidate ordering. Build the child context from `MovedFacts`, so its actor, action-use state, stats, geometry, and recipients are all from that input; use the parent context only as the shared Preview budget owner.

- For a precise move and available budget, attempt the existing precise attack assessment.
- For a nonprecise move, request admission/coverage only (`allowPrecise: false`). Do not promote a conditional world to precise attack/terminal certainty.
- If the admitted attack is nonprecise, call existing `EnemyActionCandidates.EstimateAttackSteps` with the **moved child context and actor**, plus the assessment's partial facts.
- Compute `EnemyUtilityScorer.AttackBenefit` from the resulting facts. Preserve direct allied loss and the existing maximum possible allied-loss rule before the scorer clamps a negative follow-up benefit to zero.
- Remove the early `CanChargePreview` return that currently prevents all follow-up work when Preview budget is exhausted. Budget exhaustion allows the bounded one-aim coarse estimate, but never an extra precise child. No moved input means no follow-up attempt.

Keep `max(0, followup) / 2` unchanged. Do not add projected kill, restriction, or terminal rewards to coarse estimates. Coarse delivery damage is the existing heuristic, not a guaranteed lower bound or a new defense-accurate damage model. Unsupported reactions and forced movement retain their assessment gaps and harm bounds. A proposed attack with negative net benefit can simply be declined, while the move's own risk remains charged separately.

The nonlethal, otherwise identical Regeneration control should recover the existing +200 follow-up contribution. Universal decision identity with/without Regeneration is not promised: precise kill/status rewards and coarse estimates differ by design.

## 6. Ownership, independence, and Framework

| Change scenario | Current understanding/edit scope | Proposed scope | Necessary verification |
|---|---|---|---|
| Change approach distance | Candidate representative route and scorer consequence | Engagement map plus its traversal/reach contracts; scorer only when weight changes | Walls, ranges, alternate endpoints, cutoff, candidates and final decisions |
| Change attack-position geometry | Nested pursuit implementation and separate consumers | Shared `EnemyAttackReach`; callers consume the same predicate | Existing pursuit equivalence plus map goals; no unrelated effect internals |
| Support another effect in conditional movement projection | Broad coverage gate and movement consequences | Effect-owned opt-in and its movement behavior; shared gate only when contract changes | Positive owner/remote cases, mixed blockers, fixed-intent execution |
| Change follow-up benefit | Session precision gate and score | Session wires assessment/estimate; scorer owns weights | Admission, ally harm, budgets, synchronous/prepared parity |

These are expected improvements, not measured implementation results. SRP keeps geometry, metric, effect eligibility, orchestration, and scoring in their current functional domains. OCP/ISP use a narrow optional effect capability, not a trait-name switch. LSP preserves existing precise guarantees; conditional projection never upgrades them. DIP uses rule capabilities and existing calculators. SSOT keeps Buffer and combat transition ownership unchanged; derived maps and projected inputs live only within one decision.

Framework FW-001–003 registration/lifecycle remain unchanged; FW-004 state boundaries remain enforced by the existing prepare/publish path. No creation, registration, upper-group export, or consumer lookup is added. Do not re-export a Core service or add a parallel Job to support this work. Required Core integration regression is not reduced by these internal boundaries.

## 7. Verification and approval boundary

Deliver Phase A (approach), Phase B (follow-up), and Phase C (combined evidence), stopping after each. The plan defines test cases and exact source ownership. Obtain current red evidence before each fix and preserve baseline manifests; the earlier diagnostic is a starting fixture, not current regression proof.

Measure additional map work, Preview counts, yielded Step durations, and allocations on the existing corpus plus the new rail/wall/Regeneration cases. Include a component at 256 anchors, one exceeding it, and expensive ranged/occlusion geometry. Existing performance failures remain separate; an unchanged shortlist cap is not performance acceptance. A material new regression requires reporting and a revised design before widening limits.

Full Core/CoreIntegration suites, preparation immutability, cancellation/staleness, one-result publication, and actual fixed-intent parity remain required. Run Unity 6000.0.73f1 through an AppData-capable approved path only during authorized implementation; do not delete caches, upgrade Unity, or compete with the user's open Editor.

Manual playback must confirm useful safe approaches, non-suicidal railway choices, move-then-attack for Regeneration units, Taunt, and final-night/terminal exits. It must also check whether remaining waits are reasonable and fun. This proposal does not guarantee that every map's waiting behavior disappears.

Documentation review does not establish compilation or playback. Phase A passed its focused Unity EditMode validation; Phase B passed all new cases with only two unchanged entry-baseline lifecycle failures. See the plan for exact counts and evidence. Manual playback and Phase C remain unverified.

## 8. Document review record

An independent read-only builder check on 2026-11-09 inspected this proposal, its plan, and the relevant current source. It found no material unanswered implementation questions. The author's plan review checked source ownership, interface names, coverage of the five review risks, and the pre-feature full-suite baseline requirement. These checks do not approve implementation or establish runtime correctness. The 256-anchor cutoff, initial Regeneration-only opt-in, and heuristic coarse damage remain explicit limitations requiring implementation evidence.
