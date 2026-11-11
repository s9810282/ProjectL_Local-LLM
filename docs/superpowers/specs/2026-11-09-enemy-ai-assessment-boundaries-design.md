# Enemy AI Assessment Boundaries Design

Date: 2026-11-09
Status: Design originally proposed for review; Phases A-C were subsequently authorized and verified on 2026-11-09. Phase D remains pending. See the companion plan for execution evidence; this document does not itself authorize additional implementation.
Scope: Review findings 1-3: unrelated effect harm, Leap mine traversal assessment, and omitted forecast Bind scoring.

## 1. Intent and evidence

Correct the information supplied to the existing Utility decision process without replacing its policy, combat execution, or publication architecture. “Preserve behavior” means preserving rules and contracts outside these three corrections; the affected AI choices are intentionally allowed to change.

The current working tree on `feature/Cheon/Core/NewCombatSytem` was inspected on 2026-11-09 (HEAD `eac06712f`). It contains unrelated uncommitted changes, including a range-owner argument in `EnemyActionCandidates`. Preserve them. The following are static source findings, not newly executed reproductions:

| Finding | Current source and causal path |
|---|---|
| F1: unrelated effects inflate harm | `CombatCommandEffects.AssessmentCoverageSteps` visits every live field unit. Its concrete-type exceptions feed `CanCauseUnboundedAlliedHarm`; the default is true. `CombatActionAssessment.Work.Run` then sums all allied remaining HP. `Hunter`, `Bleeding`, and `Disarm` are concrete cases still outside the exceptions. |
| F2: Leap mine mismatch | `EnemyActionCandidates` permits crossing a known mine with Leap. `CombatMovementCalculator.RouteWork` skips mines on intermediate steps. `MovementEnvironmentCalculator.RouteAssessmentCoverageSteps` nevertheless includes those mines in its blast exposure. |
| F3: forecast Bind omitted | `StageHazardForecast` calculates Bind duration, and candidate ranking uses it. `EnemyDecisionSession.PositionFacts` transfers only forecast HP loss to `EnemyScoreFacts`; final utility ignores forecast movement restriction. Zero-damage Rockfall is valid and still applies Bind. |

Paths below use these explicit prefixes:

- `C` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/`
- `A` = `C/Application/Combat/`
- `E` = `C/Domain/Traits/Effects/`
- `T` = `Assets/Tests/EditMode/`

Source of truth is the current code and Framework contracts. Earlier Enemy AI reports are historical evidence, not proof of current compilation, performance, or approval.

## 2. Scope and preserved contracts

### Required invariants

1. Keep public APIs, Buffer schemas, result/event formats, state ownership, save data, and authored assets unchanged.
2. Keep ascending actor order, mandatory Taunt, strict candidate-over-Hold comparison, deterministic ties, and rejection progression unchanged.
3. Keep shortlist and work limits: four attacks, four moves, two refined moves, one follow-up aim per refined move, eight precise children per actor, sixteen per decision, and 256 alternate-route expansions.
4. Keep actual attack/movement admission, damage, reaction, status application, mine activation, and settlement behavior unchanged. Actual commands still pass through `CombatCommandCalculator` and the existing Publisher.
5. Keep `HiddenEnvironment` and all existing effect coverage gaps. A harmless classification must not make an unsupported effect precise or certify a terminal result.
6. Keep unknown reactions, genuinely unbounded chains, random landing, and unknown effect implementations conservative.
7. Keep preparation slicing, source stamps, cancellation, one-result publication, and the distinction between synchronous Utility and legacy immediate Advance unchanged.
8. Do not change View, Flow, Plan, Job, Framework registration, or production composition. Do not introduce another scheduler, simulator, service, or persistent cache.

### Non-goals

No attack shortlist reordering (review finding 4), general weight tuning, removal of Hold, global classification of every trait/item/artifact, expansion of precise Preview coverage, threat-map redesign, performance optimization campaign, or production asset edits. In particular, unknown effects and existing item/artifact/obstacle fallbacks may still produce conservative choices. This design resolves the named effect cases and establishes an owner-local extension contract; it does not claim every no-action situation is eliminated.

The original documentation task included no implementation, test files, Unity execution, commits, or pushes. Subsequent phase execution is recorded in the companion plan. Code comments and XML documentation must be Korean; implementation documents remain English.

## 3. Approaches and recommendation

| Approach | Benefit | Cost / disposition |
|---|---|---|
| Extend the central type whitelist and add special-case score branches | Smallest initial patch | Retains hidden knowledge of unrelated traits in AI-adjacent code and separate movement rules. Not recommended. |
| Add a narrow optional effect harm capability, share the existing Leap contact predicate, and carry forecast Bind into scoring | Localizes the changing rules while preserving the existing pipeline | A small internal capability and changes to its concrete owners. Recommended proposal for review. |
| Build a complete candidate effect dependency graph and a future-turn simulator | Could classify more cases precisely | Duplicates or expands combat responsibilities and exceeds the three findings. Rejected for this scope. |

The recommendation was delivered as an assistant proposal in the original documentation task. The user subsequently authorized Phases A/B in another session and explicitly requested Phase C after the 500-point scoring proposal was explained. Further execution remains bounded by phase authorization.

## 4. D1 — Separate coverage uncertainty from additional immediate harm

### Internal contract

Add the following internal enum and optional interface to the existing `C/Domain/CombatEffects/Rules/CombatEffectCapabilities.cs`:

- `EffectAssessmentHarmScope`: `NoAdditionalImmediateHarm`, `ActingAttackOnly`, `Unbounded`.
- `IEffectAssessmentHarmRule : ICombatEffectRule` with get-only `EffectAssessmentHarmScope AssessmentHarmScope`.

This declaration concerns whether an unresolved effect can expand immediate allied HP harm beyond the direct-delivery estimate. It is not a statement that the effect has no gameplay effect, no future damage, no randomness, or complete Preview support. Keep `IEffectAssessmentCoverageRule.AssessmentGaps` independent and unchanged.

`CombatCommandEffects.AssessmentCoverageSteps` continues visiting the same bindings and yielding the same gaps. For a binding with a nonzero gap:

| Scope | Invoke the existing unbounded-harm callback? |
|---|---|
| `NoAdditionalImmediateHarm` | No. Other bindings, route contacts, and existing fallback categories can still invoke it. |
| `ActingAttackOnly` | Only when `input.Kind == Attack` and the binding owner is `input.ActorId`. |
| `Unbounded` or no classification | Yes, preserving the conservative fallback. |

Preserve the existing semantic fallback for another implementation of `IEffectKnockbackRule`: absent an explicit capability, it remains `ActingAttackOnly`. Do not silently change this existing interface-level behavior while removing the concrete-type whitelist. Unknown enum values must fail with a descriptive `InvalidOperationException`, not become harmless.

### Exact initial classifications

All names below are files under `E` ending in `.cs`:

| Effect rule | Declaration | Basis |
|---|---|---|
| `RegenerationEffectRule`, `LeapEffectRule`, `HuntersMarkEffectRule`, `VanguardEffectRule`, `AccelerationEffectRule`, `StationaryFireEffectRule` | `NoAdditionalImmediateHarm` | Preserve the current six explicit exemptions exactly. |
| `KnockbackEffectRule`, `PullEffectRule` | `ActingAttackOnly` | Preserve the existing owner-attack condition; forced arrival remains conservatively assessed separately. |
| `HunterEffectRule` | `NoAdditionalImmediateHarm` | Target reconciliation changes selection; `HunterApplication.TargetDied` awards attack to the credited owner. Retain `RandomOutcome`, so this change does not enable precise evaluation of reward/attack chains. Other reaction bindings retain their own fallback. |
| `BleedingEffectRule` | `NoAdditionalImmediateHarm` | `TraitContactApplication.ApplyBleeding` applies a periodic status to hostile recipients; this is not immediate HP loss to all allies. Periodic damage and unsupported coverage are not removed. |
| `DisarmEffectRule` | `NoAdditionalImmediateHarm` | `TraitRestrictionApplication.AfterContact` applies a restriction to the actual surviving hostile recipient. It does not create immediate all-allied HP loss. |

Leave all other rules unclassified in this change. Roar, kill chains, unknown effects, equipped/held items, artifacts, and obstacle fallbacks must retain their current protection. Do not infer safety from distance alone: passive reactions and auras can legitimately be relevant remotely.

### Ownership and effect

The effect owner supplies metadata; `CombatCommandEffects` translates it into the existing callback; `CombatActionAssessment` still owns the exposed-entity/allied-HP aggregation. No duplicate harm list, copied combat state, or second trait registry is introduced. Neither the scorer nor the decision session learns concrete trait types.

A remote classified trait alone must not increase a safe candidate's `PossibleAlliedHpLossPermille`. Its coverage gap must remain. Because coverage is deliberately unchanged, a candidate may still be coarse and its complete decision need not be identical to a world without that trait. Tests must assert the narrowed harm contract, rather than falsely requiring universal decision identity.

## 5. D2 — Reuse Leap mine contact eligibility

Keep execution as the behavioral reference. Introduce two internal helpers in existing owners:

- `CombatMovementCalculator.IsLeapActive(CombatCommandInput input, CharacterCombatFacts actor, CombatProfileReadSet profileReads = null) : bool`: move the current `LeapTraitRule` plus captured `LeapEffectRule.AllowsIntermediate` check here without changing validation or evaluation order.
- `LeapTraitRule.SkipsMineEntry(bool leapActive, bool isFinalStep) : bool`: returns true only for active Leap on an intermediate route step.

Actual route execution uses the second helper for its existing `skipMines` argument. Assessment uses the same eligibility and predicate when deciding whether a known mine on the original route can contribute blast exposure.

Do not remove intermediate cells from the general entry set. Thorn, tornado, weak-ground entry counts, other arrival rules, and displacement still observe their existing contacts.

The coverage filter must distinguish:

1. Original mover, fixed original route, intermediate mine, active Leap: exclude that mine's gap and blast contribution for this contact.
2. Final route cell: do not skip the mine. Candidate generation must continue rejecting voluntary known-mine landings.
3. Exchanged passenger at the origin: evaluate that passenger's actual arrival; the mover's Leap does not exempt it.
4. Random landing or unresolved forced arrival: retain the conservative mine contact. An original intermediate index is not proof of the eventual landing.
5. Mixed contacts: if any eligible contact can trigger a mine, retain its contribution, even if the mover also crosses it under Leap.

Do not change damage, activation, consumption, events, occupied-cell rules, or route search. A skipped mine does not imply that all coverage gaps disappear: the Leap effect itself currently remains unsupported for precise assessment.

## 6. D3 — Score additional forecast Bind at the next opportunity

### Narrow horizon

Use the forecast's existing horizon: the next enemy opportunity, or the final enemy boundary before night recall. Do not model arbitrary future turns. Score a newly lost movement opportunity once, not a penalty per Bind duration or per Rockfall.

Extend internal `HazardExposure` with `bool AdditionalBindAtNextOpportunity`. Existing HP/duration/evidence/gap fields retain their meanings. `StageHazardForecast` computes the flag; the decision session must not reinterpret Rockfall timing itself.

Derivation inside the existing forecast evaluation:

1. Use the positioned actor passed to `Evaluate` (the post-action actor for precise assessments).
2. Determine whether a next enemy action opportunity exists in this horizon. Return false for final-night recall, known actor death/non-field state, or a known terminal cutoff before that opportunity. Preserve uncertainty when only possible terminal outcomes exist.
3. Project only the actor's existing status lifetime across the forecast's boundary sequence using `AppliedStatusRules.EndControllerTurn`; the ended-side order is the same one already used by the forecast. This is a temporary derived value, never published or retained across decisions.
4. If that baseline still has Bind at the next opportunity, return false: the forecast adds no new binary movement restriction.
5. Otherwise return true if the existing `BindEndCount` or `PossibleBindEndCount` establishes a possible surviving Rockfall Bind in the horizon. Use their existing survivor/cutoff logic; do not treat death as an extra Bind penalty.

For this fixed horizon, Rockfall's `BindEndCount` already compensates for same-side cleanup (`BindAffectedSideTurns + 1` on that side). A positive surviving impact duration remains relevant at the next enemy opportunity. Do not add a new duration formula or infer the duration from raw spec fields in the session. If the horizon is extended in a later project, this derivation must be revisited.

### Score connection

- Add `bool AdditionalForecastBind` to internal `EnemyScoreFacts`, defaulting to false, and preserve it through `WithPosition`.
- `EnemyDecisionSession.PositionFacts` passes `exposure.AdditionalBindAtNextOpportunity` for Hold and every assessed candidate.
- Define `EnemyUtilityScorer.RestrictionUnitPermille = 500` as an internal constant; replace the existing immediate restriction literal in `CombatActionAssessment` with this same constant without changing its result.
- Final `Score` subtracts `RestrictionWeight * RestrictionUnitPermille` once when `AdditionalForecastBind` is true. It does not change `AttackBenefit`, follow-up attack benefit, existing weights, or terminal tiers.

The new charge is therefore 500 points, reusing the existing value of one action restriction. This was proposed here and subsequently authorized for Phase C; it was not a pre-existing gameplay contract. HP hazard retains its existing weight of -4; Bind must not accidentally inherit that multiplier.

Examples, with all unrelated terms equal:

| Situation | Additional charge |
|---|---:|
| Safe cell, no new Bind | 0 |
| Zero-HP-loss Rockfall causes Bind at the next opportunity | -500 |
| Multiple possible Bind contacts in the same horizon | -500, not a sum |
| Existing Bind remains through the next opportunity | 0 |
| Existing Bind expires, but a new Rockfall Bind survives | -500 |
| Final night recalls the survivor before another action | 0; HP/death hazards still apply |
| Guaranteed terminal before the opportunity | 0; preserve current terminal handling |

Immediate action-caused restriction remains in `RestrictionDeltaPermille`. Its post-action state is the future baseline, preventing the same surviving restriction from being charged again. A possible Bind remains possible: do not clear gaps or claim guaranteed status application.

## 7. Architecture and change independence

| Change scenario | Current modification / understanding scope | Proposed modification / understanding scope | Required verification |
|---|---|---|---|
| Classify an effect's additional harm | Effect implementation plus central concrete-type exception list, assessment aggregation, and global score consequence | Effect-local capability plus its existing execution semantics; shared capability consumer only changes when the contract changes | Owner/remote action cases, unknown fallback, related reactions; shared consumer changes also require integrated AI regression |
| Change Leap's mine contact eligibility | Execution predicate and a separate coverage interpretation | Existing Leap helper and shared activation lookup; callers consume one rule | Actual move/Preview, coverage, passenger/displacement, and final AI selection |
| Change forecast Bind scoring | Forecast evidence, shortlist ranking, and a missing final score path | Forecast supplies one derived fact; scorer supplies one weight; session only wires them | Boundary cleanup, death/terminal/recall, Hold comparison, synchronous/prepared parity |

This is an expected reduction in understanding scope, not a measured implementation result.

- SRP: effects own harm classification, movement owns contact eligibility, forecast owns future exposure, scorer owns weights.
- OCP: declared effects no longer require a central concrete-type whitelist entry. Unknown effects retain a safe default; classification is an explicit opt-in.
- LSP: new metadata does not alter effect execution interfaces, coverage guarantees, or fallback behavior.
- ISP: the optional harm capability does not force every effect to claim precise Preview support.
- DIP: the shared consumer reads the capability; it does not gain new concrete trait dependencies.
- SSOT: actual combat state stays in existing owners; metadata and temporary assessment facts do not become another state source.
- Framework: `CombatSystemFactory` still creates/registers `CombatAdvanceService` in the Core Builder; `InGameCoreSystemGroup` exposes it via its existing Core context. No service re-export, registration, Job, Buffer, or lifecycle change is needed.

## 8. Verification and delivery boundary

Use test-owned inputs. Add dedicated Core/CoreIntegration regressions only during a separately authorized Core implementation. No View test code or combat View MSBuild is planned. Existing third-party tests must not be rewritten to accept behavior changes without the user's approval.

Required evidence includes the newly failing cases before each fix, focused passing cases after it, unchanged fixed-intent actual transitions, and synchronous-versus-prepared Utility equality. Cross-issue cases must include unknown reactions alongside classified effects, Leap alongside random displacement, and Bind alongside zero HP loss and final-night recall.

Run required existing Core and CoreIntegration regressions after the combined change, sequentially, using Unity 6000.0.73f1 through an AppData-capable approved execution path. Record current baseline failures by full name/message; historical pass counts cannot substitute. Do not upgrade Unity, delete caches, alter production assets, or silently update golden outputs. Check allocation/slice counters for regression, without expanding this work into the deferred performance project. User Unity playback remains separate from automated evidence.

Deliver in phases A (effect harm), B (Leap contacts), C (forecast Bind), D (combined verification). Stop and report after each phase; continuing requires the next phase to be authorized. No commits or pushes by Codex.

Companion plan: [Task plan](../plans/2026-11-09-enemy-ai-assessment-boundaries-plan.md).

## 9. Document review record

An independent read-only builder check on 2026-11-09 compared D1-D3 with the current effect, movement, forecast, and status-cleanup sources. It found no material blocking questions. The author checked scope coverage and interface consistency against the companion plan. These are design reviews only: they neither approve implementation nor establish compilation or runtime correctness.
