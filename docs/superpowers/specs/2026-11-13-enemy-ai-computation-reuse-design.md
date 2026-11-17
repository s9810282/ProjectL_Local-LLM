# Enemy AI Computation Reuse Design

Date: 2026-11-13  
Status: Proposed for user review; implementation is not authorized by this document.  
Scope: Core computation only. The user requested this design and its implementation plan together; neither document is an implementation approval.

## 1. Intent and evidence

Reduce the pause before an enemy acts while preserving the current Utility/beam decisions. The user chose to defer Behavior Trees and to remove duplicate computations first.

The [latency investigation](../reports/2026-11-12-enemy-ai-latency-analysis.md) measured the code at `360ab06e9e626cd3aaffcf10fe5d279fbde66135`. With the current SO-equivalent values `20/20/200`, A8 decision preparation took a warmed median 37.24 ms: policy/range preparation 5.42 ms, aim enumeration 5.30 ms, common damage 1.82 ms, and distance propagation only 0.38 ms. U8 spent 5.83 ms in visibility preparation, including work beyond the visibility projection itself. These are diagnostic timings, not projected savings.

The restored prepared service needed a median 15–16 update slots for the first decision in its separate fixtures. Its 1 ms slice explains why modest computation can become a visible delay, but PlayMode latency remains unmeasured. Reuse must reduce work rather than conceal it by raising that budget.

## 2. Approaches and chosen direction

| Approach | Tradeoff | Decision |
|---|---|---|
| BT, early selection, smaller beams, or less precise evaluation | Can reduce work by changing which alternatives are considered | Deferred by the user; outside this plan |
| Persistent, cross-actor or cross-turn caches | Potentially broad reuse; expensive dependency/invalidation contract as effects and terrain change | Not selected |
| Exact reuse inside existing immutable calculation scopes | Smaller opportunities, explicit dependencies, no world-state invalidation machinery | Selected direction; the concrete design below is proposed |

Do not introduce a generic cache service, strategy interface, Blackboard, alternative combat evaluator, or new Framework registration.

## 3. Non-negotiable invariants

1. Preserve action availability, generated candidate order, beam reservations, candidate limits, refined-move selection, preview budget charging, score terms, safety filters, actor ordering, tie breaks and rejection fallback.
2. Preserve the full selected intent: actor, kind, aim, path cells, and taunt source. Preserve published transitions, event order, final battle facts and RNG state for identical inputs.
3. Preserve results across incremental execution partitions, cancellation and retry. WorkVersion, yield count, cache counters and elapsed time may change; they are not gameplay results.
4. Preserve the public Core/Buffer/Job contracts and state ownership. No View, FieldCamera, Architecture, SO values, scene, Factory registration or combat scheduler changes.
5. Keep the existing 1 ms preparation slice and all search/precision limits. Do not combine iterator work into larger blocking atoms as a shortcut.
6. Reuse only pure derived values from immutable input. Do not cache executions, trait state transitions, emitted events, RNG consumption, preview-budget decisions, or mutable command worlds.
7. A cache miss, capacity limit or absent cache follows the existing shared calculation. It never means that an action is invalid or that a target can be omitted.
8. Retain existing Core paths. Implementation documents are English; code/XML comments are Korean. New or moved `.cs` files require `.meta`; retain GUIDs for moved files.
9. Codex must not commit or push. Stop and report after every phase. A changed design or scope requires user review before implementation.

## 4. Scope and phase boundaries

| Phase | Deliverable | Boundary |
|---|---|---|
| P0 | Freeze current semantic baselines and measure duplicate opportunities | No optimization; no timing-only acceptance |
| P1 | Reuse per-position common damage and attack-selection prerequisites | Existing actor-local `EnemyAttackReach.Relocations` scope |
| P2 | Conditionally reuse target-independent attack delivery | Only if P0/P1 evidence demonstrates repeated keys and useful savings |
| P3 | Reuse assessment visibility projection | One completed entry per decision context, exact source reference and observer |
| P4 | Integrated equivalence, cost and latency evidence | No additional optimization or scheduler work |

P2 may be explicitly skipped at its review point when opportunities are absent or overhead outweighs benefit; this is an allowed outcome, not permission to broaden the cache. P3 remains independent of P2.

Deferred findings: geometric early rejection in W8 fallback pursuit; aim-enumeration redesign; actor-order reuse; effect-coverage caching; cross-decision observation reuse; background preparation during View playback; tuning expansion and category profiles. Exact early rejection may be useful later but is not duplicate-computation removal and needs its own proof.

## 5. Attack-query ownership and reuse

### 5.1 Scope owner

`EnemyEngagementField.BuildSteps` already creates a `Relocations` object for one input/actor and shares it across that actor's target queries. Extend that object, rather than creating a second cache owner.

Bind it once with:

```csharp
internal Relocations(CombatCommandInput input, CharacterCombatFacts actor,
    EnemyDecisionContext.ObservedGeometry geometry)
```

`geometry.Environment` is the observed environment. Store the exact input, actor and geometry references and require them to match when an `EnemyAttackReach` joins this scope. The constructor's separately supplied `environment` must also be reference-equal to the bound `geometry.Environment`; matching geometry alone is insufficient. Validate all four arguments before any moved/projection/damage/delivery cache access, including delivery hits that would bypass selection preparation. Mismatch is an `InvalidOperationException`, not an implicit rebind. Matching by actor ID or cell alone is insufficient.

The scope admission method is `RequireScope(CombatCommandInput input, CharacterCombatFacts actor, MovementEnvironmentFacts environment, EnemyDecisionContext.ObservedGeometry geometry)`. A shared scope requires a non-null bound geometry. The existing no-scope path still permits null geometry and performs its own preparation.

Keep the existing moved-actor and projected-input reuse. Add lazy derived entries keyed by `CellCoord`: common damage and selection preparation. Per-cell values depend on the relocated actor; never reuse one cell's damage or range for another cell. The existing field component limit bounds the positions visited in this scope.

The optional-cache path stays optional. When `Relocations` is absent, keep transient values within a `FromSteps` call; do not add a growing position dictionary to fallback pursuit merely to share the same implementation. Do not share entries with the hypothetical post-move world used by followup assessment.

Release: the owning candidate builder/session releases the scope naturally. It is not held by `EnemyDecisionSideCache`, a static field, a service, or a pool.

### 5.2 Common damage

For a position that actually produces an aim, compute damage through the existing `CombatAttackCalculator.CommonAttackDamage(projected, moved, profile)` with no event sink or execution trait scope. Reuse it for later target/aim queries at that same position and bound world.

The input binding includes artifacts, souls, other characters and all other world facts read by damage calculation. Do not replace this dependency with only attack power/category/trait IDs. Keep evaluation lazy so a position with no applicable aim does not gain expensive work or new failure paths.

### 5.3 Selection preparation

`CombatAttackQueries` remains the single owner of targeting-policy and selection-range calculation. Introduce a nested immutable `SelectionPreparation` containing the computed `AttackTargetingPolicy`, `int Range`, and exact source references for validation.

Proposed internal API:

```csharp
internal static SelectionPreparation PrepareSelection(CombatCommandInput input,
    CharacterCombatFacts actor, CombatAttackProfile profile,
    MovementEnvironmentFacts environment);

internal static (AttackTargetingPolicy policy, int range) ReadSelectionParameters(
    CombatCommandInput input, CharacterCombatFacts actor,
    CombatAttackProfile profile, MovementEnvironmentFacts environment);
```

`PrepareSelection` uses `ReadSelectionParameters`, which owns the existing policy/range expressions exactly once. Existing unprepared calls read the same parameters directly, avoiding an extra preparation-object allocation on every unrelated combat/preview call.

Append optional `SelectionPreparation preparation = null` to the existing `SelectionSteps`, `DeliverySteps` and `DeliveryCheckSteps` signatures. Preserve all existing parameters and defaults. On the first iterator step, a supplied preparation must match the input, actor, profile and normalized environment (`environment ?? input.State.Environment`) by reference before it is used. Construction is only through `CombatAttackQueries`.

Only policy and range are reused in P1. Aim cells, target eligibility, walls/occlusion, pull resolution and delivery continue through the existing implementation. Do not copy any of those rules into AI. Do not move checks outside their lazy iterator execution accidentally.

## 6. Conditional delivery reuse

`DeliveryCheckSteps` currently resolves delivery for an input/actor/position/aim, then checks whether one requested target is in `Hits` with `AppliesDamage`. The latter check is target-dependent; the preceding delivery computation has no requested-target argument.

P0 must measure `(position, aim)` attempts and unique keys within each bound `Relocations` scope, not globally. P2 is eligible only when repeated keys occur in relevant fixtures, P1 has not removed the dominant cost, and matched measurements can show a repeatable benefit.

If enabled:

- Store completed `ResolvedAttackDelivery` by `(CellCoord, AttackAim)` in the same bound scope. Cache a completed null result too; dictionary absence and cached failure are distinct.
- Use an internal insertion limit of **256 entries per scope**, initially an engineering bound rather than gameplay tuning. At capacity, keep existing entries and compute missing keys uncached. No eviction/reordering, no target pruning. A capacity of zero is supported internally for differential verification; it is not exposed in SO/UI.
- Store only after the delivery iterator fully completes. Cancellation or an exception cannot publish a partially computed value.
- Extract the existing target-membership loop into `CombatAttackQueries.DeliveryHitsTargetSteps(ResolvedAttackDelivery delivery, EntityId targetId, Action<bool> completed)`. Both cached and uncached paths use it; preserve hit order and `AppliesDamage` semantics.
- Retain aim enumeration and first-success behavior in their original order. Cached delivery cannot change which aim is considered first or bypass actor/taunt/safety requirements.
- A capacity change requires a new performance/memory review, not a gameplay retune. If the memory cost is unjustified, leave P2 disabled rather than adding an LRU layer.

No full-command preview is cached, so preview charging must remain unchanged. Counter reductions do not justify a changed precise/coarse classification.

## 7. Assessment visibility reuse

`CombatActionAssessment` requires `VisibleSteps(observer, ..., preservePublic: true)`. `EnemyDecisionContext.GeometrySteps` uses the ordinary observer projection. They are not interchangeable.

Add this internal method to `EnemyDecisionContext`:

```csharp
internal IEnumerable<int> AssessmentVisibilitySteps(
    MovementEnvironmentFacts source, Owner observer,
    Action<MovementEnvironmentFacts> completed);
```

The context owns one completed entry: exact `source` reference, observer and projected result. `preservePublic` is always true in this method and is not another configurable mode. On a hit, return that immutable projection with a bounded resumable step. On a miss, run the existing `source.VisibleSteps` with `preservePublic: true`; replace the entry only after successful enumeration. Do not cache an unfinished callback result.

`CombatActionAssessment.Run` passes **its actual `input.State.Environment` and actual actor owner**. It must not assume these equal `context.Input`: followup assessment deliberately uses a projected input with the parent budget context. Changed environment reference or observer means a miss even when tile IDs or board dimensions are equal.

Reuse only the visibility projection. Rebuild the candidate's public state/input, actor/intent and all coverage/route/exposure results as today. Coverage depends on actor, command kind, effects and projected state; caching it is explicitly outside this design.

Cancellation, normal completion and exceptions retain existing session disposal. The single entry cannot retain every hypothetical world in a decision.

## 8. Behavioral and performance acceptance

### 8.0 False global-harm regression gate

User clarification on 2026-11-13: a recurring failure is one effect causing an excessive all-allies loss estimate, large negative utility and unjustified holding. Exact old/new parity alone is insufficient if the old behavior is already wrong.

Current `AssessmentCoverageSteps(..., enemyAssessment: true)` distinguishes harm scope: unrelated/undeclared effects are not automatically global harm, and acting-attack forced movement can be left to precise preview. Existing tests cover remote Hunter/Bleeding/Disarm, other remote traits, Knockback/Pull, irrelevant obstacles and scoped mine exposure. These protections must be preserved explicitly.

An all-allies upper-bound path still exists: unbounded harm in `CombatActionAssessment` sums allied remaining-health ratios, and an unresolved bound can be weighted by `AlliedLossWeight = -3`. Four full-health allies can therefore contribute `-12000` before other terms. An arbitrary arrival callback on an actual/reachable path can still mark harm unbounded. A large negative value alone is not proof of a bug: real friendly fire or global exposure may justify it.

Before freezing P0 behavior:

- Run `EnemyEffectHarmScopeTests`, `EnemyAlliedExposureTests`, and `EnemyForcedMovementAssessmentTests`.
- Add bounded, test-owned counterexamples: adding a remote non-contributing trait or unrelated ally must not increase the same candidate's harm bound/allied-loss term; adding a source outside the actual/forced-movement reach must not create global harm. Keep genuine local blast and real exposure as positive controls.
- Inspect suspicious holds with their candidate, originating effect/source, gap flags, exposure set, unbounded marker, possible allied loss and score components. Do not classify by an arbitrary score threshold, and do not treat every uncertainty flag as proof of global damage.
- If a false bound reproduces before optimization, record it as a pre-existing assessment defect and stop affected baseline approval. Do not freeze it as correct, weaken its assertion, clamp the score, zero all uncertain risks, or silently change the scoring policy. Bring a separate minimal assessment correction for review; then regenerate only the explicitly approved baseline.

During every phase, rerun this gate alongside equivalence. No new global effect analysis, speculative future-effect simulation or broader coverage cache is authorized. Preserve the existing bounded evaluation model and the distinction between an unknown outcome and a reachable source of harm. This adds a correctness gate, not a guarantee for every future trait combination.

Current verification: on 2026-11-13 the three existing suites above ran in Unity 6000.0.73f1 with **40 passed, 0 failed, 0 skipped** (`Logs/EnemyAi/20261113-ai-harm-scope-check/tests.xml`). No product/test code was changed. The additional P0 counterexamples are planned, not implemented; this focused result does not establish universal trait coverage or approve P0.

### 8.1 Baseline capture before optimization

Use test-owned inputs; do not derive expectations from mutable production assets. Capture both `50/50/50` and explicit `20/20/200`, all 11 existing fixture IDs, and seeds `1,7,19,41,97`. Record canonical full selected intents and complete-side transitions/final facts. Also capture candidate score terms, tiers, lethal flags, precision/gaps and preview charges where diagnostic facts expose them.

Store baselines under `docs/superpowers/reports/2026-11-13-enemy-ai-reuse-evidence/`. Capture is an explicit mode and refuses to overwrite existing files. Tests never update expectations automatically. Record source hashes and fixture/input hashes.

The old pursuit reference predates this optimization but is not a complete Utility oracle. Use it for reach/path parity together with the new current-behavior baseline, not instead of that baseline.

### 8.2 Required verification matrix

| Input/condition | Expected invariant |
|---|---|
| Multiple targets share a cell/aim query; successful and failed deliveries | Same target results, selected aim and order; fewer builds when reuse applies |
| Same entity/cell but different input, actor facts, profile, terrain, souls or artifact state; same geometry with a separately supplied different environment | A separate scope or an explicit mismatch before any hit; no stale reuse |
| Direction/All, splash, piercing caps, pull, friendly targeting, Cannon exclusions | Identical shared attack selection and delivery |
| Destination modifiers, disarm-cleared reach facts, visibility/hidden terrain | No leakage between cells, owners, public projections or followup worlds |
| Yield partitions 1/2/31, cancellation during a miss, rejected action retry, capacity 0/full | Same complete intents/transitions; no partial cache publication |
| New effects or reactive/forced-movement interactions | Existing execution/assessment parity and relevant effect regressions remain required |

Run all existing mandatory Core and CoreIntegration regressions at integration. A newly failing pre-existing test owned by another author must not be rewritten without user approval. This is Core work: do not add View tests or run combat-View MSBuild as part of it.

### 8.3 Cost acceptance

Measure query builds and hits, unique positions/aims, entry counts, preview counts and fallback use, alongside warmed median/tail timing and preparation slots. WorkVersion is not an equal-cost unit and may decrease.

For each phase, use three matched baseline/changed captures on the same machine and unchanged inputs/settings/runtime. Require consistent reduction in the intended duplicate work and repeatable improvement beyond baseline run-to-run noise. Do not accept a repeatable regression in another representative fixture silently; report the tradeoff at the phase review. If no useful benefit is shown, revert/skip that optimization.

Use the existing 1 ms prepared-service capture without altering scheduling. Separate first-use costs, warm CPU-work intervals, prepared update slots and actual PlayMode latency. Allocation API calibration must pass before claiming byte reductions; otherwise report it unavailable and use entry counts plus a later supported profiler capture.

A lower-spec machine and representative in-game trace remain final validation targets. This design promises semantic preservation and evidence-driven improvement, not a universal millisecond ceiling for future content.

## 9. Change independence, SSOT, SOLID and Framework review

| Scenario | Before | After / remaining responsibility |
|---|---|---|
| Change a targeting effect | Shared effect/query code evaluates it repeatedly | Same rule owner; reuse remains valid only for immutable bound facts. Recheck local invalidation and query parity, not View implementation |
| Change AI score weights | Scorer/settings | Still scorer/settings; reuse stores raw query values, not weighted scores |
| Change visibility semantics | `VisibleSteps` and its consumers | Same projection owner; assessment cache calls it and tests `preservePublic`/observer identity |
| Change presentation or Job scheduling | View/LifeCycle/advance service | Outside implementation scope; no cache lifetime or ownership moves there |

SRP: combat queries own rules; existing reach/context scopes own reuse. OCP: future traits continue through existing effect contracts, with explicit dependency verification rather than a second evaluator. LSP: optional prepared calls must produce the same result as unprepared calls. ISP: internal preparation data exposes only derived values needed by existing queries. DIP: Core still consumes immutable contracts and does not depend on Unity, Buffer implementations or View.

No Framework services are added, so FW-001/002/003 registration/build responsibilities do not change. FW-004 state boundaries remain intact. Existing Job ordering, result publication, cancellation and buffer lifetime remain owned by their current systems. Shared attack-query changes still require attack/preview/integration regression checks; reducing unrelated understanding is not an excuse to omit those dependencies.

## 10. Review and rollout

The implementation plan must preserve the phase stops above. P0 validates opportunity and captures the current contract; P1 is the first implementation candidate. P2 has an explicit evidence gate; P3 can proceed independently after its review. P4 reports exact behavior parity, timing distributions, workload limits and unverified PlayMode/low-spec items.

Implementation choices left to the builder: private collection layout inside existing scope classes, local variable names, and test helper factoring. Changes to scope identity, public API/state ownership, cache lifetime/capacity policy, candidate budgets or behavioral invariants are not left to the builder.

Builder check: a separate reviewer identified the separately supplied environment identity as a missing scope guard. It is now explicit in §5.1 and the verification matrix. No other implementation-blocking questions were reported. This review does not replace user approval or implementation verification.
