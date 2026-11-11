# Enemy Utility AI and Turn-Boundary Hazard Forecasting

Date: 2026-11-07

Status on 2026-11-08. Phase 6 Utility development is verified under diagnostic composition. The [Phase 7 review](../reports/2026-11-07-enemy-utility-ai/phase-7-acceptance.md) confirms the overly broad allied-loss bound and records timing overruns. Gameplay acceptance remains unmet. Normal gameplay uses immediate Advance. The [handoff](../handoffs/2026-11-07-enemy-utility-ai-hazard-forecast-handoff.md) carries the next review conditions.

Inspected source: `feature/Cheon/Core/NewCombatSytem`, HEAD `9d8c9ce45` plus the uncommitted changes identified in the [Preview prerequisite report](../reports/2026-11-07-enemy-utility-ai/phase-2-preview-optimization.md). Current owning code establishes the contracts; the report distinguishes baseline and optimization evidence.

## 1. Intent and decisions

Replace attack-first enemy decision making with Utility evaluation of attacks, movement, and holding position. Consider known hazards through the next opportunity to act. Preserve actual combat rules, authoritative state ownership, and one-action publication.

The user selected **bounded candidate evaluation with computation spread across frames**, accepting additional decision latency to protect rendering. Beam search and coordinated action-order search are deferred. Keep ascending EntityId actor order initially. A move may consider one possible subsequent attack by the same actor; this is a positional feature, not a searched or committed multi-actor plan.

The design must not recreate the former pathfinding performance problem. More intelligence is useful only if worst-frame behavior remains acceptable.

### Alternatives considered

| Approach | Trade-off | Decision |
|---|---|---|
| Fully evaluate every move/aim combination | Rich comparisons, but multiplies settlement, copies, and allocations by board cells and aims | Rejected |
| Cheap screening, bounded precise evaluation, synchronous decision | Small scheduling change; one expensive decision can still stall a frame | Not selected |
| Cheap screening, bounded precise evaluation, resumable preparation across frames | Adds a preparation lifecycle and scheduling contract; limits optional work and yields between work units | Selected by user |

No worker threads, Unity Jobs/Burst migration, third-party AI framework, generic planning engine, or global cross-turn cache is included. Difficulty/personality authoring UI and new enemy skills are also excluded. Internal tuning values are centralized, but a new SO/table chain is not required for the first implementation.

## 2. Evidence and present limits

Paths in this document use these abbreviations:

- `Core/`: `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/`.
- `CombatService/`: `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CombatSystem/`.
- `Flow/`: `Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/`.

| Current source | Observed behavior |
|---|---|
| `Core/Application/Combat/EnemyAi/EnemyAiTurnService.cs` | Ascending EntityId; attack first, otherwise pursue; returns one accepted action; pending boss roar precedes ordinary AI |
| `.../EnemyAiPolicy.cs` | Attack previews rank friendly contacts, target kind, actual HP loss, and hit count; taunt restrictions are resolved before ordinary selection |
| `.../EnemyMovementPathfinder.cs` | BFS with target-directed aim prefiltering, per-search cached geometry, and first-goal-distance stopping; not a general escape candidate generator |
| `Core/Contracts/Combat/UnitActionUseRules.cs` | Available allows movement or attack; Moved allows attack; attacking prevents subsequent movement |
| `Core/Application/Combat/CombatTurnCalculator.cs` | Controller-end count advances at side endings; hazards precede end cleanup and the last-enemy-turn night recall |
| `Core/Application/Combat/StageGimmicks/StageGimmickComposition.cs` | Tornado, railway, haboob, rockfall; project each result in order; stop on terminal outcome |
| `Core/Application/Combat/CombatCommandCalculator.cs` | Preview differs from execution: visible environment and omitted pending boss reactions |
| `Core/Application/Combat/AttackDeliveryExecutor.cs` | Preview also omits actual-only kill-chain behavior; it is not a complete simulator |
| `Core/Application/Combat/CombatCommandComposition.cs` | Project produces temporary next facts; Merge permits one ordinary action settlement per publication |
| `Flow/Jobs/CombatNextJob.cs`, `CombatAdvanceJob.cs` | Each child Job must complete in one Execute; Advance additionally requires an accepted published result |
| `CombatService/CombatAdvanceService.cs` | Currently assembles, calculates, and publishes synchronously |

Historical diagnostic evidence exists in [the November 3 pathfinding record](2026-11-03-enemy-pursuit-pathfinding-performance-design.md), section 1: one enemy Advance was recorded at 231.98 ms / 63.1 MB allocated; pathfinding accounted for 226.53 ms / 61.5 MB, including 121 attack-position checks and 121 full range queries. This is a historical report, not a fresh measurement or a normative design dependency. The current pathfinder contains the candidate filtering and reuse described above. Its optimized structure must be preserved.

The original design-only session performed no new measurements. Subsequent Phase 0/1 and Preview prerequisite reports now supply measurements; this document amendment itself runs no Unity tests. The optimized full Preview still measured mean 1.0931 ms, p95 2.5499 ms and maximum 24.8348 ms, with 471/1,400 warmed calls above 1 ms. Section 12 changes the proposed work boundary in response; it does not relabel those measurements as passing.

## 3. Ownership and processing shape

```text
Existing CombatPlan / CombatNextJob
  -> one preparation Job: bounded work, then Job completes
  -> next frame: Plan rechecks completion, abandonment, presentation, and readiness
  -> more preparation OR existing Advance Job
  -> existing command calculation and publisher: one actual action
  -> presentation, then a new decision from fresh facts
```

Use three cohesive responsibilities, not one framework service per scoring term:

1. **Decision session in Core.Application**: owns candidate enumeration cursors, shortlist, evaluation cache, work counters, and selected intent. Reads one immutable input. Has no Buffer/Unity access and never installs speculative results.
2. **Feature-owned assessment queries**: existing attack/movement/effect/gimmick owners provide shared admission, geometry, and observable consequence facts. AI turns those facts into preferences. Damage rules, hazard timing, and trait restrictions remain with their current owners.
3. **Preparation lifecycle in CombatAdvanceService**: owns the one active decision session, input validity checks, frame budget, release, and handoff to actual execution. Its existing Factory is the composition point. No duplicated root service export is added.

A session is derived work, not a second authoritative combat state. It may retain immutable source references and one exclusively owned, private mutable candidate projection while that calculation is suspended. Existing projection owners retain all mutation rules; the projection is neither shared with another candidate nor installed in Buffer. Release it after incremental score extraction. Never retain a transition for every explored cell. Section 12 defines the ownership and cancellation boundary.

## 4. Candidate generation and choice

### 4.1 Hard rules before preferences

Use existing action admission, movement, targeting, hostility, and taunt rules. A high score never overrides a hard restriction. Preserve current taunt resolution: attack the required reachable source when applicable, otherwise pursue the required source, and use the existing fallback only when the restriction cannot be obeyed. Performance truncation is not proof that a taunter is unreachable.

Pending boss reactions and controller-start transitions remain ahead of normal enemy planning. Do not score forced reactions against ordinary actions.

### 4.2 Actors and holding

Visit eligible actors in ascending EntityId, preserving the current ordering policy. Evaluate the actor's useful attacks/moves against an internal Hold baseline. Select the first actor with a preferable legal action. Reconsider from the new actual state after that action.

Hold means skip this actor for this decision pass, not publish a no-op, consume an action, or persist a deferred flag. If every eligible actor selects Hold or has no legal action, select EndEnemyPhase once. This intentionally allows ending the side when available actions are worse than holding; update the old 'no possible action' comment and its policy expectations explicitly. Holding must never produce an infinite automatic loop. A legal taunt obligation cannot be evaded through Hold.

There is no new public Wait command. Two actors exchanging move order or later returning to the first actor within a planned sequence are outside this release.

### 4.3 Cheap candidate pass

- Build actor/target lookup and occupancy information once per session. Reuse shared geometric and targeting rules.
- For attack aims, use target-directed candidate generation where its existing coverage applies; retain target-shape, wall, splash, pierce, and friendly-contact distinctions. Do not call full Preview while enumerating every cell.
- For movement, perform one resumable reachable-area traversal for the current actor, retaining predecessor information. Rank destinations by hazard reduction, potential attack geometry, distance to useful engagement, and player-threat exposure. Reconstruct routes only for shortlisted destinations.
- Include an escape candidate even when it moves away from the pursuit target. Reserve a shortlist place for the best known hazard-reducing legal route, if one exists. Reserve a taunt-compliant candidate when required; reservations count inside caps.
- Preserve the existing optimized long-distance pursuit search for targets outside this turn's reach, but make its frontier resumable and reuse results within this session. Its first-goal-layer stopping remains correct for its original objective; do not apply that stopping rule to the new best-position/escape objective.
- Known mine exclusion, leap traversal versus landing, occupied cells, and movement allowance come from the existing rules. Screen traversed cells as well as endpoints. A shortest route to a safe endpoint is not assumed safe.
- Keep at most one ordinary route per destination initially. If it is hazardous, a bounded hazard-aware alternative search may supply an escape route; score its hazard exposure separately from the game's unit-cost movement allowance. Do not silently change movement costs to implement AI preferences. This is incomplete tactical search, not a guarantee to discover every safe route.

Existing `CombatMovementCalculator.Range` returns destinations rather than complete route trees. Avoid running it and a second equivalent BFS for each destination. Share traversal/admission primitives while allowing the different pursuit and candidate-selection stopping criteria.

### 4.4 Precise shortlist pass

Starting tuning proposal, not a measured optimum:

| Item | Initial cap |
|---|---:|
| Current-position attack aims retained per actor | 4 |
| Move destinations retained per actor | 4 |
| Move routes receiving full preview per actor | 2 |
| Subsequent attack evaluated for each precisely previewed move | 1 |
| Full preview calls per actor | 8 total: attack + move + subsequent attack |
| Full preview calls across one published-action decision | 16 total, including fallback refinement |

Do not recreate a full movement-by-aim product. For each of the two refined moves, choose at most one promising subsequent aim by cheap geometry before previewing it. Execute only the move if selected; its later attack is neither reserved nor queued. Other low-cost moves remain eligible with explicitly approximate positional scores.

For movement, reuse the `CombatMovementCalculator.ProjectRoute(..., preview: true, ...)` semantics used by movement Preview, through its later owner-local resumable RouteWork and shared completion work (plan Task 4.1). Keep the synchronous ProjectRoute wrapper for direct callers; scheduled assessment does not drain it inside one Step. Do not route a Move through the attack-oriented `CombatCommandCalculator.Preview`: its Move dispatch reaches the execution calculation and does not supply the desired preview visibility boundary.

The total precise-preview cap bounds optional quality work, not legal admission. On exhaustion, use the best known legal candidate and conservative score/coverage facts. If no candidate has been admitted, continue the finite, resumable mandatory admission scan; do not equate 'not inspected' with 'no action'. Do not reset optional counters when switching actor, target, or fallback. Mandatory taunt reachability/admission may span more frames; expose that separately in diagnostics.

If actual execution rejects a chosen action due to hidden facts, retain only the rejected candidate identity in the current session, try a bounded admitted alternative with the same remaining budget, and preserve the existing hidden-information policy. Do not restart an unlimited search or expose the hidden cause to the scoring model. A changed source snapshot discards this rejection set.

## 5. Score facts, traits, and uncertainty

The scoring model compares these quantities from the enemy's perspective:

```text
useful opposing HP loss + elimination value + useful status change
+ engagement-position value + immediate same-actor attack opportunity
- allied harm - actor loss risk - known hazard loss - estimated player threat
```

Normalize HP loss by maximum HP, cap overkill at actual remaining HP, and use fixed-point/integer weights with stable ties. Keep a separate kill bonus so several harmless hits cannot mechanically outweigh a decisive kill. Count each consequence once: damage and trait activation do not both receive damage credit for the same loss. A trait name or activation event alone earns no generic bonus.

Hold uses the same future exposure model as every action; it is not an always-safe zero score. Compare each action's value against that baseline. Engagement value rewards progress toward a reachable useful attack position, so harmless approach can beat indefinite inaction. Discount the estimated subsequent attack after movement (initial proposal: one half) because it is not committed. Exact weight tuning is left to the builder using the behavioral acceptance cases; it must not alter legality or candidate budgets. Resolve equal scores by a documented stable action/aim/cell order, with Hold winning a truly zero-gain tie rather than pointless movement.

This formula is a preference model, not an alternative damage engine. Use installed effect-adjusted facts and shared calculation results. Status utility is based on observable restrictions and duration; unknown secondary value is recorded as unassessed rather than reconstructed with a TraitKey switch inside AI.

Attack's position is locked for the rest of that actor's turn. Movement retains a possible attack, but not a second movement. Score the actor's actual projected endpoint after deterministic displacement, not merely the requested destination.

Outcome interpretation is explicit: `StageOutcome.Defeat` means a player defeat and an enemy success; `Victory` means the opposite. Only a complete deterministic immediate outcome can outrank ordinary scores as a confirmed terminal result. Preview-only outcomes with omitted reactions or random consequences are estimates and cannot be advertised as a guaranteed win.

### Evaluation coverage contract

Each assessment carries covered consequences and uncertainty reasons. Current public Preview is never labeled 'full future simulation'. It excludes some actual-only effects; those omissions must be reported at the shared calculation boundary.

- Reuse a full preview only when its evaluated chain is observable and deterministic for scoring. Its existing omitted reaction coverage remains explicit.
- If the chain can draw randomness, do not rank a candidate by the result obtained from the live RNG cursor. Use a conservative geometric/status risk band instead. This applies to tornado throws and effect-driven random movement as well as future hazard spawns.
- Add a small assessment-capability query beside the existing effect/entry rules, compiled once for the session's bindings. Unclassified behavior defaults to partial/uncertain. Owners describe support/coverage, not a duplicated implementation of their effects; AI does not maintain a second trait registry.
- Coverage is transitive: uncertain reaction/displacement/entry effects make dependent endpoints, later hazard contacts and kills uncertain too. Deterministic geometry before that boundary remains usable; a downstream deterministic rule cannot restore certainty to an unknown input position.
- Deterministic direct damage and legality may still use existing narrow queries when full consequence evaluation is unavailable. No unsupported bonus or guaranteed kill is invented.
- Seed changes affecting only unobserved future randomness must not change the selected intent. Actual execution keeps the current RNG semantics.

Lower assessment precision may miss a clever interaction. This is an accepted initial limitation; silently predicting all traits perfectly is not a performance goal. Mandatory targeting/restriction behavior remains exact regardless of assessment coverage.

**User-confirmed Phase 4 interpretation:** no public contract proves hidden hazards absent. All publicly identical assessments therefore retain HiddenEnvironment. IsPrecise identifies a deterministic projection of the observed world; it does not certify the actual world's terminal outcome. Observable random, actual-only or unsupported chains are screened before precise child creation. Dead/terminal moved actors do not expose a follow-up world. Unclassified optional effects contribute no invented benefit and retain conservative possible allied harm.

## 6. Turn-based hazard forecast

### 6.1 Time horizon

Use `CompletedControllerTurnCount` and explicit ended-side boundaries. Individual enemy actions do not advance this clock. Normally assess this enemy-side end and the following player-side end, through the next enemy action opportunity. On the last enemy response of a night, include the immediate end hazards before recall, then stop when the unit leaves the field; do not invent a next active combat turn.

The forecast is conditional on public facts and on the evaluated unit remaining at the assessed position. Player decisions, later enemy displacement, and future random warnings are not known facts. An actor already moved cannot evade a hazard by moving again. Restriction duration and turn-start effects can delay the next real voluntary action; report that extra exposure where shared lifecycle queries can determine it, otherwise use conservative uncertainty.

### 6.2 Feature-owned facts, built once

Prepare compact cell/time exposure facts once per immutable session, shared across actor evaluations where appropriate. Bound memory by board cells and the short horizon. Store phase/timing/source/severity and coverage, not one opaque danger number that loses why and when a cell is dangerous.

| Hazard | Shared source and assessment |
|---|---|
| Railway | Extract the due-boundary predicate from the current rule into a shared query. Rail cells are dangerous at activation boundaries, not forbidden on every movement step. Contact at activation means environmental instant death under current rules. |
| Rockfall | Read Warning/WarnedCells/NextBoundaryCount. Share the current max-HP-loss and Bind-duration calculation with the live rule. Do not generate future warning positions from RNG. |
| Tornado | Reuse `TornadoGeometry.Travel`, published center/side/locked direction and schedule. Include current entry contact and the complete warned swept area, not just final center. Query forced-movement immunity through existing rules. Random landing is uncertain; never label it a known safe escape. |
| Haboob | Reuse current `HaboobTargetingRules` and boundaries. It changes legal targeting/position value; it is not inherently HP damage. Unknown relocation cells are not forecast as known. |
| Other entry hazards | Use visible environment, existing movement/entry rules and shortlist previews. Preserve known-mine policy. Unknown mines remain unknown. |

Expose assessment facts through the existing gimmick registration/composition boundary. Each feature supplies its own narrow assessment behavior. Keep execution order in one registration source; do not duplicate a second order list inside AI. Pure shared timing/geometry/severity helpers may be extracted where needed, but do not replace the live settlement pipeline or call full `EndEnemyPhase` for every candidate.

### 6.3 Combined hazards and terminal boundaries

Independent masks are sufficient only where earlier hazards cannot change later contact. If a tornado can displace the actor before railway/rockfall, mark the subsequent occupancy uncertain. Use a conservative risk band and refuse to certify safety from the original cell. A deterministic displacement already captured by an action preview may be followed by hazard queries on its final position.

Do not run all combinations of random landing and later hazards. Avoid counting two certain deaths as twice the loss of the same unit; clamp terminal loss and distinguish additional status costs only for survivors. Stop an exact forecast after a confirmed terminal result; conditional terminal outcomes do not erase possible hazards.

Use hazard risk as a strong preference penalty, not a universal hard movement ban. A verified immediate enemy win may justify sacrifice. Soft risk weights cannot override taunt or movement legality. Shortlist reservations prevent cheap attack ranking from removing all escape choices before comparison.

Player threat is a separate approximate field assembled from shared range/occlusion/targeting geometry. Do not simulate every player reply at every destination or treat all possible attacks as certain simultaneous damage. Cache its geometry only within the source state and mark assumptions about movement and next-turn availability.

## 7. Scheduling and validity contracts

### 7.1 One slice is one completed preparation Job

Add distinct `PrepareAdvance` and `PreparedAdvance` steps/Jobs. A preparation slice completes in one Execute without publishing a CombatResult; its postcondition is recorded preparation progress/readiness, not a result sequence increment. `CombatNextJob` and the shared Runner retain their current one-shot behavior. The existing `CombatAdvanceJob` remains the immediate/NotRequired path and still requires a published accepted Advance result. The new prepared-execution Job has the explicit result table below; do not weaken the existing Job's postcondition.

| Prepared execution disposition | Required postcondition |
|---|---|
| Published | Exactly one accepted Advance result; validate its sequence/kind just as the existing Advance Job does |
| Stale | No result; discard old session; next Plan evaluation may prepare fresh input |
| RetryPreparation | No result; keep rejected candidate identity and remaining counters; next frame resumes bounded fallback preparation |
| Cancelled | No result; release session; normal completion/abandon/lifecycle priority determines subsequent work |

Every disposition completes this one-shot Job. Stale/RetryPreparation never drains preparation, executes an alternative, or recursively retries in the same call. Unexpected calculator/configuration failures are not converted into these normal dispositions.

Extend the existing advance service contract with narrow preparation/readiness/cancellation operations. The service owns the session; `CombatProgressReader` reads preparation status through the existing CoreGroup service route and places that derived status in `CombatProgressFacts`. It continues to read authoritative battle/result facts from their existing Buffer sinks. No thinking status is copied into a new authoritative Buffer.

When the prepared route is enabled and normal automatic progress is allowed, the Plan chooses preparation until ready, then PreparedAdvance. Controller start and required reaction work can report NotRequired and use the existing immediate Advance path. Preserve completion > abandon > presentation > ordinary work precedence. A preparation Job must not recursively schedule another slice or execute the chosen action in the same call.

Protect against multiple Runner calls in one rendered frame: the Systems service receives/checks the current frame stamp and does not spend a second slice or commit in the same frame. Time/frame access remains outside pure Core. Do not use scaled game time for CPU budgeting. No sleeping, Tasks, or per-frame reconstruction of the entire decision.

Compatibility: preserve `AdvanceCombat()`'s existing bool meaning (published / normal no-op). For existing direct synchronous consumers, it may drain the same deterministic decision session before executing; it must not use a second legacy policy. The production Plan/Job path must always prepare first for ordinary enemy decisions and must never silently fall back to draining a missing/stale session in one frame. Add a prepared-only execution entry/guard for that path. Existing pure calculator entry points can drain the same session for offline calculation, but are not used to hide synchronous work in production.

### 7.2 Freshness and cleanup

A decision is bound to battle ID, controller sequence, result sequence, Combat QueryRevision, actor facts, roster revision, and the remaining immutable input dependencies (inventory, artifacts, currency/fame, definitions and configuration). `QueryRevision` covers accepted combat changes but must not be assumed to cover every assembler dependency.

Capture a narrow source stamp alongside assembly, using existing revisions and immutable source identities/content where revisions are absent. `CharacterData` is immutable; ordered entity identities plus immutable fact identities can detect replacement without copying character arrays every slice. Capture owned/main/soul values, artifact and inventory source identity/content, balances, and battle definition identity once. Do not hash the entire board each frame or add revisions to unrelated stores merely for convenience.

Validate the stamp before every slice and readiness read. Reassemble fresh input once before execution and verify the decision dependencies still match; mismatch invalidates the decision and returns Stale without publishing. A known hidden-fact rejection returns RetryPreparation rather than repeatedly selecting the rejected candidate. Actual calculator/publisher failures still follow the existing fault boundary, not silent success.

Release session buffers on success, invalidation, abandonment, terminal completion, battle end/replacement, and shutdown. Make cleanup explicit in the current combat lifecycle/composition; do not leave a large cancelled snapshot retained until another enemy turn. Cancellation is idempotent. The session never resumes after its battle ID changes.

### 7.3 Fair budgeting and progress

Pure Core advances by deterministic work units: frontier nodes, geometric aim candidates, assessment items, or one feature-owned **substep within** a precise preview. A full Preview is a logical evaluation spanning multiple units, not an atomic Step. Systems checks CPU elapsed time between units. Time only determines when to yield; it does not select a different candidate on a faster computer. Candidate limits and tie order determine quality consistently.

Candidate generation, hazard/occupancy preparation, pursuit, and mandatory admission must all be resumable, not only the final preview loop. A request that yields must retain a cursor or finish a bounded work item. Progress diagnostics distinguish 'optional cap exhausted' from 'mandatory validation in progress'. Do not return Pending forever because a fresh slice restarts its traversal.

## 8. Performance targets and failure gates

These are proposed acceptance targets on the user's reference machine, to be calibrated against a fresh baseline before implementation. They are not achieved performance or a hard real-time guarantee.

| Metric | Proposed target |
|---|---|
| Per-frame preparation budget | 1.0 ms soft CPU target; yield at work-unit boundary |
| Preparation frame distribution | p95 <= 1.5 ms; p99 <= 2 ms; any sample > 4 ms requires investigation before acceptance |
| Atomic preparation substep, including precise-preview work | Target <= 0.5 ms; no unexamined > 1 ms atom in the accepted stress set; measure complete Preview CPU/latency separately |
| Decision latency | Normal-scene p95 <= 150 ms, stress p95 <= 300 ms; record max and mandatory-work exceptions |
| Allocation | Report bytes per decision, largest preview, and whole enemy side; provisional <= 256 KiB per decision and <= 2 MiB per 8-enemy side, subject to baseline feasibility |
| Real execution/publication | Measure separately; no claim that preparation slicing bounds these existing synchronous operations |

A stopwatch cannot interrupt a running Preview, input assembly, settlement, or GC. An expensive atom is a failed performance gate, even if the surrounding loop checks a 1 ms deadline. Reduce its inputs/copies through shared helpers, use a cheaper supported assessment, or design a feature-owned resumable operation before accepting it. Do not mask the issue by simply raising the budget or lowering FPS expectations.

Allocation targets are explicitly provisional because no baseline per-preview byte count was captured in this session. If baseline live execution alone exceeds a target, report that fact separately and revise the numeric target with evidence; never label a failing measurement as passing. FPS alone is not an acceptance metric.

Practical restrictions:

- No full range query or full transition for every BFS node.
- No global full-board LINQ/ToArray/ToDictionary pass inside candidate loops when session indexes suffice.
- No new collection or world snapshot per cheap score item. Prefer bounded arrays/lists and indexed lookup; use existing pooling only with a demonstrated need and explicit release.
- No persistent state-wide cache requiring guesses about invalidation. Session-local source-safe reuse first.
- No equal-target-set aim merging unless delivery, order, occlusion, displacement and effects are proven equivalent.
- No thread/Burst optimization before profiling identifies a compatible computational kernel.
- Do not trade mandatory rule correctness for a time cap. Bound optional quality, yield mandatory work, and report remaining latency honestly.

## 9. Diagnostics and validation

Extend `CoreProfileSection` and the existing Systems marker adapter for preparation, candidate enumeration, hazard facts, cheap scoring, precise preview, and freshness validation. Record generated/retained candidates, full preview calls, expanded nodes, cache hits, per-slice CPU, largest atom, bytes allocated, latency, coverage limitations, invalidations and cap hits. Counters are development instrumentation, not combat state. Keep a bounded decision explanation (chosen score components and a few rejected alternatives); do not allocate a formatted log for every candidate in production.

Required validation scope for the later implementation:

1. Capture baseline and changed runs with identical fixtures/seeds, build mode, settings and markers. Warm up; use repeatable 6- and 8-enemy scenes, large/reachable boards, dense walls, long-range and splash attacks, taunt, multiple traits, overlapping hazards and reinforcement growth. Include no-action/unreachable-target cases. Record at least 1,000 decisions across fixtures, separate cold starts, and report percentile and maximum cost, allocations and complete enemy-side latency.
2. Preserve shared legality and original pursuit equivalence checks. Intentionally changed utility preferences require new expectations clearly separated from unchanged attack/movement mechanics. Existing fixed actor order remains verified.
3. Verify move-then-attack versus attack-lock, forced taunt, known/unknown mines, leap, hidden rejection fallback, all-Hold side completion, and no repeated no-op publication.
   Include 'both positions risky, escape is less risky' and 'safe approach beats endless Hold' cases to verify the common baseline.
4. Verify railway traversal versus end occupancy, count+1 and count+2 deadlines, rockfall warning versus unknown future warning, tornado entry/sweep/immune units and random landing, haboob targeting, last-night hazard-before-recall, terminal short circuit, and combined hazard uncertainty.
5. Verify identical choices under different frame budgets and unobserved random seeds; source world/RNG unchanged by assessment; abandoned/stale sessions never publish; one final publication; no intermediate result/event/reward leaks.
6. Verify `PrepareAdvance` makes no result sequence change but completes its Job, multiple calls in one frame spend at most one slice, old synchronous API and prepared runtime choose the same action, and existing completion/abandon/presentation precedence remains intact.
7. Retain affected Core/CoreIntegration regressions for targeting, trait effects, movement, turn/gimmick processing, publishing, and advance services. Scheduling changes also require integration coverage and user Unity playback; Core-only evidence does not verify View behavior.

This design does not authorize new View tests or MSBuild. Under current repository instructions, View compilation/playback is user-owned; if existing tests written by someone else need runtime expectation changes, obtain the required permission before editing them. Implementing approved Core phases and scheduling changes must retain the applicable verification boundaries. Unity batch execution, if later authorized, must use the approved AppData-capable path rather than cache deletion or an Editor upgrade.

## 10. Change independence and impact

| Change scenario | Before | Proposed modification / understanding / verification boundary |
|---|---|---|
| Tune attack versus survival preference | Modify attack rank and orchestration; movement not comparable | Utility scorer/settings only; understand score facts and action locks; tactical preference cases and performance sample |
| Add a hazard | Register live rule; AI has no hazard timing model | Feature owns shared schedule/assessment and live rule; common registration changes are allowed; verify forecast/live parity and combination order, not unrelated trait internals |
| Add a trait | Live effect may incidentally appear in Preview | Feature declares assessment coverage with its existing rule; scorer consumes consequences; verify actual mechanics and coverage/seed policy, no copied trait switch |
| Change frame budget | No resumable decision | Systems budget only; Core candidate result must remain identical; scheduling, cancellation and performance checks |
| Change candidate search | Pursuit algorithm determines a single destination | AI candidate generator/session plus shared movement admission; maintain original pursuit behavior where reused; verify escape/quality and node/preview caps |

Expected implementation touch points: AI policy/service/pathfinder, narrow shared attack/movement assessment queries, feature-owned hazard timing/assessment, diagnostics, CombatAdvanceService/interface, its Factory dependencies, CombatProgressReader/Facts, CombatPlan/Step/Context and preparation/advance Jobs. Pure assessment metadata and a prepared-decision contract may be added to Core.Contracts as required by the assembly boundary. No Framework rewrite, second Buffer, ViewGroup database rule read, new UI, scene, camera, or art change is needed.

SOLID/SSOT checks: scoring owns preferences; feature rules own consequences; Systems owns resumable lifetime; Flow owns when work is permitted; publisher owns actual mutation. A new effect does not require AI to know its private implementation. Coverage defaults conservatively and must not weaken a shared contract. Interfaces are narrow only where an assembly/consumer boundary requires them; internal helpers need no framework registration. Registration, lifecycle and publication regressions remain necessary shared verification.

## 11. Proposed review milestones, not an implementation Task plan

1. Baseline cost capture and shared assessment/coverage contracts, including the single-atom feasibility gate.
2. Resumable preparation integration with original decision behavior, cancellation and publication boundaries verified.
3. Utility candidates/scoring and known railway/rockfall timing, with candidate and allocation instrumentation.
4. Tornado/haboob and entry-hazard uncertainty, combined-hazard coverage, full regression and stress capture.

Every implementation Phase stops for review. The linked Task plan now owns the execution ledger; the current amendment remains documentation-only. No code, tests, production assets, commits or pushes are part of this revision.

### Decisions proposed by the author for approval

- Preserve actor order; Hold is internal and may end the enemy side when all actions are undesirable.
- Use conservative partial assessments for unknown/stochastic/actual-only consequences, not a complete trait simulator.
- Start with the candidate caps and provisional performance targets above; measure before accepting them.
- Add a separate one-shot preparation step and a prepared-only execution route; preserve the existing synchronous API's published/no-op meaning for compatibility.
- Authoring UI, cross-turn caches, worker threads, beam search and coordinated actor ordering remain deferred.

### Builder check

An independent read-only builder check identified two material ambiguities: hidden-fact execution rejection needed an explicit resumable disposition, and Hold needed the same exposure baseline as actions. Both are incorporated in sections 5 and 7, with matching verification cases. Movement assessment now names the existing preview-mode route boundary explicitly. Coverage uncertainty propagates through dependent effects. Performance feasibility remains a measured acceptance gate, not a result of this document review.

## 12. Core-only resumable Preview amendment (2026-11-07)

### 12.1 Direction and scope

The user requested this amendment after confirming their Core-programmer role. The proposed implementation stays under `Core/` plus Core/CoreIntegration tests and documentation. No View/UI, Flow/Job, Factory, Buffer, scene, production asset or camera implementation is included in the revised Phase 2. The existing Phase 3 cross-layer scheduling work remains a separate future authorization/handoff.

| Approach | Consequence | Disposition |
|---|---|---|
| Keep a whole Preview as one unit and only reduce call count | Preserves the measured multi-ms atom; optional caps do not interrupt it | Rejected for the frame-protection objective |
| Replace legacy Preview with cheap/partial assessment now | Smaller cost, but changes current ranking and coverage before the parity phase | Remains Phase 4/6 policy work, not this amendment |
| Resume the existing feature-owned calculation internally; synchronous callers drain it | More retained execution state; preserves rules and supports yielding inside the measured operation | Direction requested by the user; concrete contracts below are the proposed revision |

This is not a claim that every local optimization has been exhausted. Retain the verified fixed-registration reuse. Further reductions are permitted within these owners when they preserve semantics and are measured; they do not replace the resumability acceptance checks.

### 12.2 One calculation, two ways to drive it

Add one internal `CombatAttackCalculation` in `Core/Application/Combat/CombatAttackCalculation.cs`. It owns the lifetime of a single attack evaluation, its existing `CommandAttackProjection`, `CommandAttackPlan`, `CommandAttackLedger`, optional shared `MovementProjection`, and active child cursors. It owns no damage formula, trait policy, service registration or clock.

The internal `CombatCommandCalculator.BeginPreview(input, rules)` entry returns this work object for an Attack intent. `Step()` advances one measured leaf item or one finite phase transition. `IsComplete`, `WorkVersion`, `Preview` and `Transition` expose only progress and the completed result. Exact signatures and disposal/error behavior are locked in plan section 2.5.

Existing public `Preview`, ordinary `Calculate` and `Reaction` signatures stay intact. Their attack paths synchronously drain the same owner-level machinery; they must not retain a separate old attack algorithm. Existing non-Attack command dispatch stays unchanged. Reaction admission, its null result/empty-area evidence, and actual-only branches retain their current wrappers and semantics. Actual execution remains synchronous and separately measured. No claim is made that this amendment bounds publication, reactions or actual-only kill-chain execution.

AI preparation uses `BeginPreview` and retains the child work across decision-session calls. It never calls the synchronous Preview wrapper or drains a child from `EnemyDecisionSession.Step`. The later assessment adapter must also expose resumable work rather than calling a draining `TryAssess` from a scheduled unit.

### 12.3 Feature-owned continuations and ordering

Keep helper continuations nested in their existing owner files where practical; the one new top-level calculation file is a lifetime coordinator. Do not introduce a generic task graph, event bus, executor service, copied effect engine, or one interface/file per stage. A parent may advance **one** child unit and return, even when that child emits no result. Child completion/result reads cannot do hidden work.

| Existing owner / dependency | Resumption boundary and invariant |
|---|---|
| `CombatAttackQueries`, wall/selection helpers | Incremental actor/target/geometry items; preserve admission failure order, public visibility, target and aim ordering. Do not hide full range materialization in the first Step. |
| `CommandAttackProjection`, `CommandAttackPlan`, `CommandAttackLedger` | Build indexes/collections one source item at a time; share one private world and ledger for this candidate. Retain attack-group identity, captured inputs and event positions across yields. |
| `CombatAttackCalculator.Execute`, `PullAttackCalculator`, `AttackForcedMovement` | Keep notification/binding setup, optional Hunter reconciliation and pre-hit Pull before direct delivery; rebuild delivery at the same post-pull boundary. One forced path cell may need its own entry-effect child. |
| `AttackDirectDamageCalculator`, `CombatDamageApplication`, `ObstacleReactionCalculator` | Retain hit index, pending-health batch, reflection state, obstacle queue and ledger. Resume within a flush if necessary, but preserve **when** the flush starts/finishes relative to ordered hits and reflection. Never turn simultaneous damage into eager per-hit application. |
| `AttackKnockbackCalculator`, `MovementEnvironmentCalculator`, `ObjectArrivalCalculator`, `TornadoCalculator` | Preserve sorted displacement order, intermediate contacts and deferred final-object arrival. Resume target/cell/source/effect enumeration, including no-op items; child cascades cannot be drained recursively inside one parent unit. |
| `TraitPresenceReconciler`, `MovementProjection.CurrentInput` | Capture one observation boundary, compute all pending changes against its immutable facts, then record/apply in the original order. Do not eagerly expose an earlier recipient's new facts to later recipients. |
| `AttackDeliveryExecutor`, `HunterApplication.FinishAction` | Keep direct damage, displacement, action-use settlement, hit-health capture, final arrivals and scope completion in the existing sequence. FinishAction also consumes Acceleration; absence of Hunters does not authorize skipping it. |
| `AttackResultFactory`, `MovementProjection.Complete`, `CombatCommandCalculator.Finish` | Resume event/target/result assembly and existing end settlement. Preserve WeakGround/death/Hunter reconciliation, outcome, presence, kills/rewards/experience, main health and soul ordering. Insert attack/pull/reaction events at the reserved positions, not at whichever Step finishes first. |

Each collection loop must retain its cursor, including rejected/filtered items. Each remaining leaf callback is timed. One target/cell is not automatically small: nested reflection, explosion, arrival or settlement fan-out needs a child cursor if it exceeds the gate. Unsupported configuration still faults; slow work is not silently skipped or marked Unsupported to preserve legacy parity.

Immutable public constructors may copy/validate large collections. Result sealing, the last `MoveNext`, projection construction and cleanup are part of the measured work. Retain defensive public contracts. Only if evidence identifies a costly constructor may its owner add an internal incremental builder with exclusive buffer ownership and complete validation before exposure; never add a public unchecked/trusted-data escape hatch.

### 12.4 Suspended state, observation barriers and cleanup

- Exactly one candidate calculation owns its mutable projection. The parent session and other candidates can observe neither partial dictionaries nor hypothetical events. Final facts remain speculative until the existing actual command/publisher path runs later.
- Installed definitions and movement rules remain immutable during one calculation. External source changes invalidate/dispose the parent session before more work; the child never rereads a live Buffer/DB or repairs a stale source itself.
- A `CurrentInput` or presence query is an **observation barrier**: finish its required reconciliation and immutable snapshot assembly before the consumer proceeds. Its temporary read context expires before any dependent health, position, state, environment, reward or owned-record mutation. Do not add a global cache, a shadow Buffer revision or guesses about invalidation.
- Presence evaluation retains the current semantics: calculate proposed changes from one captured population, record shield/taunt events in the existing ordering, then apply changes. Cancellation before the barrier completes discards its proposals. It never partially updates authoritative state.
- Preserve candidate-local random cursor, attack groups, deferred deaths, scope ancestry and event indices through every yield. Re-evaluating after a yield must not draw again or reapply an effect. Phase 2 preserves current Preview omissions/random semantics; Phase 4's conservative coverage rules still determine which assessments may later be used for Utility scoring.
- `Dispose` and faults release all retained references/cursors without finishing pending calculations, emitting results, flushing damage, consuming RNG or publishing. No yielded cursor has a `finally` that drains gameplay work. Prefer dropping private collections to bulk clearing/pooling them; measure release costs and subsequent GC separately.
- A malformed input/configuration exception is propagated and terminal for that calculation; it is not an admitted no-action result, a rejected execution retry, or a quality-budget exhaustion. Only a completed normal rejected preview can be scored/skipped according to existing rules.

### 12.5 Logical evaluation budgets and measurement

One full attack evaluation consumes one Preview allowance, charged immediately before creating its child work. Child Steps, state transitions and resumed frames consume work units, not additional Preview allowances. Do not refund a started evaluation because it rejects or is cancelled within a retained decision. Phase 2 still scans all legacy aims; the 8/16 caps remain inactive until Phase 6. Rejection fallback keeps counters and identities as already specified.

The existing numeric soft target (0.5 ms) and >1 ms investigation gate now apply to each non-preemptible preparation substep, including construction, sealing and release. This explicitly replaces the former whole-Preview work boundary; it is not a larger budget. Continue reporting the complete Preview's summed CPU, allocation, number of substeps and eventual frame latency. Do not report a 0.2 ms substep as a 0.2 ms complete evaluation.

Initial/zero-target/terminal paths, no-match scans, large hits, forced entry cascades and final result assembly require measurements. Capture both uninstrumented matched end-to-end runs and matching-instrumentation Step runs; include parent/preview ID, phase/item ordinal, work version, time and supported bytes. Cold/JIT and warmed samples stay separate. Allocation unavailable is not zero, and reducing every Step by adding excessive copies is not acceptance.

The legacy full-Preview gate remains a historical failure. On 2026-11-07 the user approved separate Phase 3 development entry and gameplay activation criteria. Verified Phase 2 parity, finite progress and isolation support development entry. G-atom remains unmet and blocks gameplay activation. Explicit test or diagnostic composition can exercise the prepared route. Normal game composition continues to select immediate Advance. The user explicitly resumed Phase 3 after the earlier documentation-only stop. Its implementation and Core verification are recorded in [the Phase 3 report](../reports/2026-11-07-enemy-utility-ai/phase-3-preparation.md); user compilation/F1–F8 and performance activation gates remain outstanding. No Phase 4 implementation or gameplay activation follows automatically from that checkpoint.

### 12.6 Locality, regression and remaining acceptance

| Representative change | Modification / understanding boundary after this amendment | Required verification |
|---|---|---|
| Change a damage or contact rule | Existing rule and owner; retain its leaf/result contract. The AI coordinator does not enumerate trait internals. Registration changes are legitimate. | Live rule cases, synchronous/resumed preview parity and affected cascades |
| Change a yield location | Owning continuation plus progress evidence; know its mutation/observation barrier and parent result contract | All partitions, cancellation at every boundary, exact ordered events/RNG and atom measurements |
| Change AI weights/caps | Existing AI scorer/session; one logical Preview budget regardless of its internal Step count | Numeric scoring/cap tests and identical intent across work partitions |

Expected costs are extra private execution state and more calls per decision. No speedup or bound is promised until measured. This wider shared-Core change requires full Core and CoreIntegration regression, including existing target, reflection, forced movement, trait, death, reward and publication tests. Test inputs remain test-owned. Frozen legacy traces are an independent oracle; draining the new worker and comparing it only to itself is insufficient.

The user approved the Phase 2 implementation, including necessary Core owners along the Preview call graph. The [latest Phase 2 decision report](../reports/2026-11-07-enemy-utility-ai/phase-2-decision-resumption.md) records the implemented continuation and the remaining atom gate.

### 12.7 Amendment builder check

An independent read-only builder check compared this amendment and its tasks with the current attack/delivery/projection/presence owners. It found no important unanswered implementation questions. It checked nested work and cleanup, the captured-population barrier, frozen-result independence, logical Preview charging, actual-only branches and Phase boundaries. Private cursor placement and measured leaf refinements remain implementation decisions within the named owners. This review is not compilation, execution or a performance-gate result.
