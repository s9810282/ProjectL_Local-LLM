# Enemy AI Decision Foundation Redesign

**Date:** 2026-11-09
**Status:** Design draft, revised after one independent read-only review (section 12) and the user's direction of 2026-11-09 (section 6.2 ignored sources, section 6.5 death trade with a designer-editable tuning asset, base defense deferred to section 13). All decisions settled on 2026-11-09 (Q-R1, Q-R2 by user direction; Q-R3 to Q-R13 recommendations accepted). Task plan: [foundation redesign plan](../plans/2026-11-09-enemy-ai-decision-foundation-redesign-plan.md). No implementation authorized yet.
**Supersedes in part:** the single-target engagement map of [engagement follow-up design](2026-11-09-enemy-ai-engagement-followup-design.md) A1–A3, the "Regeneration-only" follow-up gate of the same design B1–B3, the world-level allied-harm ceiling kept by [assessment boundaries design](2026-11-09-enemy-ai-assessment-boundaries-design.md), and the deferral of beam search in [utility AI design](2026-11-07-enemy-utility-ai-hazard-forecast-design.md) §1. Everything else in those designs (actual combat rules, preparation lifecycle, one-action publication, hazard forecast, taunt) stays.

Path prefixes:

- `Core/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/`
- `AI/` = `Core/Application/Combat/EnemyAi/`
- `Tests/` = `Assets/Tests/EditMode/`

## 1. Why a foundation change

The user reported that on stage 1127 (`Stage_LD_P27`) enemies end their side without moving even after the player moves. The same symptom has been reported on other maps after each of the last three improvement rounds (Utility AI 11-07, assessment boundaries 11-09 A–D, engagement/follow-up 11-09 A–C). Some stages behave and others do not. The user asked for a definitive root-cause analysis and a redesign of the decision foundation rather than another term-level patch.

This document keeps the Utility decision shell (one immutable input, bounded resumable work, one action per publication, Hold baseline, deterministic order) and replaces the parts that decide *whether acting is worth it* and *where to go*.

## 2. Evidence

### 2.1 Stage 1127 reproduction (no artifacts)

`Tests/CoreIntegration/Stage1127EnemyHoldDiagnostics.cs` (explicit, uncommitted diagnostic) reads the real stage 1127 definition from the production database (13×13 field, walls, weak ground with entry threshold 1, cliffs, haboob relocating every 2 controller turns; enemies Demon, 2× UndeadCannonCart, Gravekeeper with Knockback/Leap, 2× UndeadHeavy with Regeneration/SteadfastWill). The player roster is the CoreIntegration test roster (Akimbo main, Cowboy with Pull/Leap/Acceleration, DynamiteHammer with Leap, Sniper), deployed like the user's screenshot at (2,1), (4,2), (8,2), (10,2), **with no artifacts or items**. It plays four rounds with the production Utility decision (`CombatCommandCalculator.AdvanceUtility`, the same session the prepared runtime uses) and prints each candidate's score terms next to Hold.

| Case | Observation |
|---|---|
| A: player ends turns without moving | Enemies advance and attack. No stall. |
| B, round 1 | Heavy (8,11) holds: every move has engagement progress 0 (map component 47 cells; allies that moved earlier block the corridors). |
| B, round 2 | Demon (8,6) holds. Its only approaching move (8,5) is adjacent to the Hammer: progress +20, player threat −400, follow-up attack not computed. Score −380 versus Hold 0. |
| B, round 3 | Demon moves (8,6) → (8,9), away from every player. Hammer is inside the haboob cloud; the Demon's target stays the Hammer (Manhattan-nearest); attacks across the cloud edge are forbidden (`HaboobTargetingRules.CanDesignate`), so the only goals are cloud cells reachable by circling the map. |
| B, rounds 3–4 | Gravekeeper (5,2) and Heavy (3,5) hold. Approach moves score −177 / −126 (threat 217 / 166 versus progress +40); follow-up not computed although each endpoint is adjacent to Akimbo. |

### 2.2 Artifact comparison (decisive)

`Tests/EnemyArtifactHoldDiagnostics.cs` (explicit, uncommitted) plays one full enemy side on each benchmark fixture, seed 1, with and without **one** placed artifact effect (`ArtifactSoulSchemaTests.SoulEffect`). Published actions:

| Fixture | No artifact | One artifact |
|---|---|---|
| N5 | 5 attacks | 0 attacks, 0 moves |
| N6 | 6 attacks | 0, 0 |
| N8 | 8 attacks | 0, 0 |
| W8 | 8 moves | 0, 0 |
| A8 | 7 attacks, 2 moves | 0, 0 |
| R8 | 7 attacks, 2 moves | 0, 0 |
| X8 | 4 attacks, 6 moves | 0, 0 |
| H8 | 3 attacks, 3 moves | 0, 0 |
| G16 | 16 attacks | 0, 0 |
| T8 | 8 attacks | 8 attacks (taunt is mandatory) |
| U8 | 0 | 0 |

A single artifact makes the whole enemy side idle on every non-taunt fixture. Real runs normally carry artifacts placed in the lobby, so this alone explains "the enemy does not move" independent of map shape. Which stages "work" then depends on taunt, items, rosters and traits rather than on the map.

## 3. Root causes

Ordered by impact. Each is a structural property of the current pipeline; different runs and maps trigger different subsets.

### F1. A world-level "possible allied harm" ceiling hits every candidate but never Hold

- `CombatCommandEffects.AssessmentCoverageSteps` reports unbounded allied harm for: any effect rule without an `IEffectAssessmentHarmRule` declaration (except knockback-type rules outside the acting attack), any held environment, equipped item or temporary item effect on **any** unit, **any** placed artifact effect, and **any** obstacle binding (`CombatCommandEffects.cs` 45–47, 53–73, 76–80).
- `CombatActionAssessment.Work.Run` then sums the current HP permille of **every** allied (enemy-side) field unit into `PossibleAlliedHpLossPermille` (178–186). The same sources add `UnsupportedEffect`, so the candidate can never be precise and `HasUnresolvedAlliedLoss` stays true.
- `EnemyUtilityScorer.AttackBenefit` subtracts `3 × max(allied, possible)` from every attack and move. Hold is scored without the ceiling (`EnemyDecisionSession` Hold facts).
- With N enemies at full HP every candidate loses about 3000 × N. The whole side holds (section 2.2). The sources are persistent world facts present whether or not the candidate acts, so the penalty does not distinguish between candidates at all; it only distinguishes "act" from "Hold".

### F2. The "move then attack" value requires a full movement projection that is almost never allowed

`MovedFacts` exists only when the candidate is one of the first two moves in list order (taunt route, escape, pursuit fallback, retained) and a movement projection is accepted (`CombatActionAssessment.cs` 198–200; `EnemyDecisionSession.cs` 163). The projection is blocked whenever any effect with assessment gaps lacks `SupportsMoveProjection` (only Regeneration opts in; rules that declare no gaps, such as SteadfastWill, do not block), and by every non-trait source of F1. In production rosters it is effectively always blocked, so a move into an attack position is valued only by its approach step.

### F3. Score terms are not commensurate

Engagement progress is `20 × steps`, clamped to ±100. Player threat is `−1 ×` estimated loss permille (up to −1000). There is no exchange concept (damage dealt versus damage taken). With F2 removing the attack value, entering any cell the player can currently hit is almost always worse than Hold: enemies stop at the edge of the player's reach and hold or back away when the player advances.

Also: threat is capped at the actor's current HP (`EnemyThreatField.cs` 40), so a wounded unit sees lethal cells as *less* negative; there is no self-elimination term. Only coarse collapse landings are a hard filter (`EnemyDecisionSession.cs` 215); a known lethal hazard is only a large penalty.

### F4. Candidate pruning ranks by safety before purpose

`EnemyActionCandidates.Destination.Rank` is lexicographic: `(hazard, entry risk, player threat, attack potential, progress, steps, y, x)`. At most 4 destinations survive (`Math.Min(4, MoveCandidateLimit)`, line 69) and at most 2 are refined (`Math.Min(2, RefinedMoveLimit)`, session line 163). Attack positions are inside the target's threat by nature, so they sort behind every safe cell and are pruned before scoring. Pruning and scoring use different value models.

### F5. One fixed target chosen by straight-line distance

`EnemyActionCandidates.Build` picks the pursuit target by `(unit before HQ, Manhattan distance, id)` (line 132). It ignores walls, the haboob designation rule, and whether any attack position is reachable. The engagement map is built for that one target. When it is unattackable or reachable only by a long detour, all moves score 0 or the enemy detours (looks like retreat), while other reachable targets are ignored.

### F6. Allies are permanent walls in the long-range graph

`MovementRouteValidator.PursuitTraversal` blocks every living unit except the actor. This is the correct rule for this turn (actual movement cannot pass units unless Leap; `MovementRouteValidator.CanTraverse`), but the engagement map reuses it for multi-turn distance. An ally that moved earlier into a one-cell corridor makes the corridor "impassable" for those behind it; every move scores 0 and they hold.

### F7. Hold wins all non-positive ties

`SelectUtility` admits a candidate only if `rank > holdRank`. Strict comparison is correct; the defect is that F1–F6 remove or zero the positive terms that should break the tie.

### F8. Validation corpus does not resemble production

`EnemyAiBenchmarkFixtures` covers open boards plus some walls (W8/U8), haboob (H8) and taunt (T8), but no artifacts, items, held environment, obstacles, undeclared traits, production rosters, or ally-blocked corridors. Section 2.2 shows the corpus passes only because it has no artifacts. Every previous round passed its corpus while production stalled.

### F9. Cost: held actors are re-evaluated every decision

A side publishes one action per decision. The session visits actors in order and stops at the first actor with a positive candidate. Actors that held before it are fully re-evaluated in every later decision of the same side. This cost grows with the number of idle actors — exactly the failure case.

### Outside AI scope (reported, not redesigned here)

`CombatAttackQueries.UnitReady` refuses every attack, for both sides, while any multi-cell unit is on the field (`DefersMultiCellAttack`, lines 59–64), and `EnemyThreatField` returns an empty field in that case. This is a deferred combat-rule limitation, not an AI decision. Boss stages with multi-cell units will look "idle" for this reason. It needs a separate rule decision.

## 4. Why the previous rounds did not fix it

| Round | What it changed | Untouched cause |
|---|---|---|
| 11-07 Utility AI | Replaced attack-first with Utility + Hold | F1, F2, F3, F4, F5, F6 |
| 11-09 boundaries A–D | Narrowed the ceiling for traits that declare scope and for route-local environment | F1 for artifacts, items, held environment, obstacles, undeclared traits; F2–F6 |
| 11-09 engagement A | Route-distance progress for one target | F3, F4, F5, F6 (made F5/F6 visible as 0-progress holds) |
| 11-09 follow-up B | Let Regeneration stop blocking projection | F1, F2 for every other source |

## 5. Goals, non-goals, preserved contracts

Goals:

- Enemies with a reachable, non-lethal way to strike engage, on any stage shape and with real loadouts (artifacts, items, traits).
- Hold occurs only for a stated reason: every option is lethal or clearly losing in the exchange, or no attackable target is reachable.
- Move candidates are selected with the same value model that scores them.
- Decision cost is measured against current HEAD with an explicit stop rule; per-frame slicing guarantees stay.
- A production-stage behaviour corpus with real loadouts becomes part of acceptance.

Non-goals: side-level joint planning / multi-actor beam search, multi-turn lookahead, simulating player responses, per-unit personality authoring, base (headquarters) defense (section 13), worker threads, AI LOD, repeated-Hold penalties, the multi-cell attack rule (section 3).

Preserved:

- Actual combat rules, admission, taunt obligations, hazard forecast ownership, one action per publication, prepared/synchronous publication parity, cancellation and staleness, Buffer and event schemas.
- No View, Flow, Plan, Job or Framework changes. No new services or registrations. One exception requested by the user: a single designer-editable InGame DB config table for AI tuning (section 6.5).
- The immediate-compatibility path (`SelectImmediate`, `CombatCommandCalculator.Advance`) is unchanged.
- Determinism: identical input gives identical decisions for every Step partition.
- Public API changes are limited to those listed in section 7 and require decisions Q-R6, Q-R10.

## 6. New decision foundation

### 6.1 Pipeline

```text
Per decision (one immutable input), shared by all actors:
  context index, observed geometry, hazard forecast, player threat field      (unchanged)
  AI-ignored sources dropped from assessment; obstacles scoped per candidate  (6.2)
Per actor:
  1. Engagement field  (6.3)  multi-target, attackability-aware, ally soft cost
  2. Reachable endpoints this turn (existing BFS, hard occupancy)
  3. Cheap score for every endpoint with the full value model (6.5)
  4. Beam: top K + reservations (6.6); geometric follow-up (6.4); re-rank
  5. Precise assessment for the top R where supported (existing)
  6. Compare with Hold scored by the same model; pick
Across decisions of one side:
  reuse of side-invariant shared structures (6.8)
```

### 6.2 AI-ignored sources and candidate-scoped obstacles (fixes F1)

User decision (2026-11-09): the enemy AI does not reason about sources whose consequences it cannot model.

- **Ignored by the AI entirely:** placed artifacts, equipped and temporary items, held environment state, and effect rules without an assessment declaration (`IEffectAssessmentCoverageRule` / `IEffectAssessmentHarmRule`). For enemy assessment they produce no coverage gap, no move-projection block and no allied-harm ceiling. Actual combat still applies them in full; the AI may mispredict an outcome they change, which the user accepted.
- **Obstacles: scoped per candidate (author's recommendation, accepted).** An obstacle matters only if this candidate can trigger it: the candidate's delivery hits the obstacle, or its route enters the obstacle's trigger cell. Then the ceiling counts only allied units inside that obstacle's reaction area, taken from the existing public owner `CombatHazardAreas` (the same source the hover range uses). Obstacles the candidate cannot touch contribute nothing.
- **Unchanged:** effect rules that declare gaps or harm scope keep their declarations; route-local environment coverage from boundaries A–D stays; knockback-type rules keep the existing acting-attack condition.

Mechanism: `CombatCommandEffects.AssessmentCoverageSteps` gains an assessment mode for enemy AI that skips the ignored source kinds. Real-combat callers and the immediate-compatibility path keep the current behaviour. `CombatActionAssessment` applies the obstacle scoping using the candidate's delivery cells and route. Because ignored sources no longer add `UnsupportedEffect`, candidates can again reach precise assessment and the conditional move projection when nothing else blocks them.

Consequence for section 2.2: one artifact no longer changes any decision. The pinned regression in 8.1 checks exactly that.

### 6.3 Engagement field (replaces the single-target map; fixes F5, F6)

New `AI/EnemyEngagementField.cs` replaces `EnemyEngagementMap`.

- **Targets.** All living hostile units and attackable hostile structures in the observed world. With a taunt obligation the field contains only the required source (the taunt rule stays the authority).
- **Goal cells.** For each target, attack positions are found with the shared `EnemyAttackReach` predicate (shape, destination range modifiers via `MovedTo`, pull bonus, walls/occlusion, haboob designation). The candidate cells per target are bounded by the same reach rules `CombatAttackQueries.EnumerateAimsThatCanHit` uses (range plus destination and pull bonuses, splash radius); shapes that pierce to the board edge fall back to scanning the whole component. Landing exclusions stay (known mine, next-entry collapse, occupied).
- **Goal value.** `V(t)` = estimated opponent loss permille of one basic attack on target `t` (shared coarse damage), plus the existing elimination weight when lethal; units before HQ as today.
- **Costs.** Multi-source reverse search from all goal cells with integer costs (bucket queue). Step cost 1. A cell occupied by an **allied** unit costs `1 + AllySoftCost` and cannot be a goal. Hostile units, walls, cliffs, structures stay hard. Mines and Leap transit keep existing rules. The field estimates multi-turn reach only; this turn's moves still use hard occupancy.
- **Bounds.** `CellLimit` 289 retained anchors. If truncated, progress falls back to the representative pursuit route toward the Manhattan-nearest target (today's fallback, kept only for that case).
- **Next-turn attackability.** Haboob relocation is a boundary event: for `NextTurnStrike` the field uses `StageHazardForecast.CanDesignateAt(endpoint, target, boundary)` rather than the current cloud.
- **Shared geometry.** `EnemyAttackReach` currently builds its own wall geometry and copies the world per candidate cell. It is refactored to take the decision's `ObservedGeometry` and to reuse one relocated input per cell across targets. Goal sets are cached per decision by `(profile, range, occlusion, Leap)` so actors with the same attack profile share them.
- **Outputs per cell:** `RemainingCost`, `BestTarget`, `IsAttackPosition` (cost 0), `TargetValue`.

The Manhattan pursuit target in `EnemyActionCandidates.Build` is removed (except the truncation fallback). Pursuit and taunt routes use the field's best target.

### 6.4 Geometric follow-up (fixes F2)

New `AI/EnemyFollowupGeometry.cs`. For an endpoint `e` where the field says `IsAttackPosition`, estimate the same actor's best attack from `e` this phase without a full movement projection:

- The moved actor is produced by one shared helper owned by the movement side (not AI-local): anchor/footprint at `e` (as `EnemyAttackReach.MovedTo` does), `ActionUse = Moved`, and the per-turn movement state that effect owners read (e.g. `StationaryFireEffectRule` uses `MovedThisOwnTurn`). AI must not reproduce those flags itself.
- Taunt is re-evaluated at `e` (`TauntTargeting` depends on position); with an obligation the follow-up may only target the required source, otherwise it is 0.
- The aim is chosen by the field's best target value first, then the existing stable order; `FollowupAimLimit` stays 1.
- Delivery is estimated with `EnemyActionCandidates.EstimateAttackSteps` on the relocated child input. The `partial` facts passed in are the follow-up attack's own scoped facts (6.2 rules applied to that attack), not the move's facts, so no ceiling is charged twice or clamps the follow-up to 0.
- Admission uses the actor's current restrictions (`CombatActionAdmission.IsAttackRestricted`) and `UnitActionUseRules`.
- Not credited when the route has unresolved arrival consequences (`RouteAssessmentCoverageSteps` gaps) or the endpoint is a collapse landing.
- When a precise projection is available, its follow-up result replaces the geometric estimate. Both feed the same score field.

### 6.5 Exchange-based value model: brave, not suicidal (fixes F3)

User direction (2026-11-09): enemies should fear death less and accept dying when they **kill a player unit or deal heavy damage**, but must never walk into a gimmick to die. The trade ratio must be editable by designers.

`EnemyUtilityScorer` owns the formula. Numbers come from one tuning source (below).

```text
Score = AttackBenefit(now)                                   (attacks; 6.2 scoping)
      + FollowupWeight   × max(0, FollowupBenefit(e))        (6.4 or precise)
      + NextTurnWeight   × NextTurnStrike(e)                 (new)
      + Progress(e)                                          (field cost delta, ±cap)
      − PlayerRisk(e)                                        (new, below)
      − HazardWeight     × Hazard(e)                         (unchanged, non-lethal hazards)
      − forecast bind penalty                                (unchanged)
```

Two death sources are treated differently:

- **Gimmick / terrain death is never chosen.** An endpoint whose known hazard loss reaches the actor's HP (`StageHazardForecast` known loss, collapse landing, railway, rockfall, haboob boundary, entry damage on the path) is removed from the candidates, unless Hold itself is equally lethal and every alternative is too (then the least lethal is kept, as today's terminal hazard rank does). This replaces "only coarse collapse landings are filtered".
- **Death by player attack is a trade.**
  - `PlayerRisk(e)` = `ThreatWeight × PlayerThreat(e)` while the threat is below the actor's current HP.
  - When the threat reaches the actor's HP (the player can kill it there next turn), `PlayerRisk(e)` = `DeathTradeThreshold` instead. This is a flat price for the enemy's life, in opponent-loss permille.
  - Default `DeathTradeThreshold` = 500, i.e. "worth dying for if it deals at least half of a unit's max HP, or kills one" (a kill adds the existing elimination weight 2000).
  - The flat price also removes the wounded-unit inversion of F3: a lethal cell costs the same at any remaining HP.
- `NextTurnStrike(e)` = `TargetValue` of the best target if `RemainingCost(e) ≤` the actor's movement allowance (attackable at the next boundary per 6.3), else 0.
- Hold is scored with the same function at the current cell, including its own `NextTurnStrike` and `PlayerRisk`.

Initial values, validated by the corpus (section 8): `FollowupWeight = 1` (same enemy phase), `NextTurnWeight = 1/2`, `ThreatWeight = 1/2`, `DeathTradeThreshold = 500`, progress 20/step ±100, hazard and allied-loss weights unchanged.

Worked checks:

- Stage 1127 B round 2 Demon (15 HP; Hammer 20 HP, 6 damage, Hammer threat 6 < 15):
  - Move to (8,5): follow-up 300 + next-turn 0.5 × 300 = 150 + progress 20 − 0.5 × 400 = **+270**.
  - Hold at (8,6): one step from an attack position, so next-turn 150; threat 0 → **+150**. The move wins by 120.
- Lethal exposure, trade price 500:
  - Poke for 100 permille: 100 + 20 − 500 = **−380**, holds.
  - Heavy hit for 600 permille: 600 + 20 − 500 = **+120**, accepts death.
  - Kill (1000 + 2000): accepts.
- Walking onto a collapsing tile or a railway cell about to fire: never a candidate.

### 6.5.1 Designer-editable tuning asset

A single InGame DB config table (same pattern as `CategoryConfigTable`: exactly one item):

- `EnemyAiTuningDataSO` (input layer, Unity serialized fields) → `EnemyAiTuningData` (pure C#, immutable, `Validate`) → `EnemyAiTuningTable` query.
- Designer fields (Korean labels in the inspector), first release:
  - `DeathTradeThresholdPercent` — minimum damage, as % of the target's max HP, worth the enemy's life (default 50).
  - `ThreatAversionPercent` — how much non-lethal incoming damage counts against a move (default 50, i.e. weight 1/2).
  - `NextTurnApproachPercent` — value of standing where it can strike next turn (default 50).
- Everything else (follow-up weight, progress, hazard and allied-loss weights, ally soft cost, beam width) stays as code constants in `AI/EnemyAiTuning.cs` until a designer need appears.
- Flow: `CombatAdvanceService` gains the existing module lifecycle `ISystemInitData<IDataBaseQueryContext>` (it is already registered as a module, FW-002), reads the table there through an optional query interface (absent table → code defaults, same pattern as the trait growth tables) and builds the decision settings passed to `EnemyDecisionSession`. The values travel in the public `EnemyDecisionSettings` contract (new optional parameters; contract addition, Q-R12). Core.Application stays free of DB types.
- Timing: DB tables are read once when the game starts. A value changed in the editor applies from the next Play. Live editing during Play is not in this release.
- DB rules apply (CLAUDE.md P1–P7): pure Data has no Unity types; SO and Data in separate files; the table asset and the `InGameGroup` registration ship in the same commit as the code, or database creation fails at boot.

`AI/EnemyAiTuning.cs` holds the code constants and the defaults; with the asset present, the asset's three values override the defaults. Numbers have one owner per value.

### 6.6 Beam candidate selection (fixes F4)

`EnemyActionCandidates.MoveSteps` keeps its reachable-area BFS but replaces the lexicographic `Destination.Rank` with the numeric cheap score of 6.5 (without precise terms):

1. Score every landable endpoint cheaply: lethal hazards filtered (6.5), non-lethal hazard, `PlayerRisk`, field cost/value, `IsAttackPosition`, `NextTurnStrike`.
2. Keep the top K endpoints. Reservations count inside the beam: taunt route, best hazard escape, best attack position, best progress endpoint.
3. Add geometric follow-up (6.4) to beam members that are attack positions, then re-rank.
4. The top R get the existing precise assessment when supported; the others keep coarse facts.
5. The hard-coded `Math.Min(4, …)` and `Math.Min(2, …)` caps are removed. K and R come from `EnemyDecisionSettings.MoveCandidateLimit` / `RefinedMoveLimit`. The public default of `moveCandidateLimit` changes from 4 to 6 (contract change, Q-R6). R stays 2.

This is the per-actor beam (move → attack) agreed with the user. Side-level beam is out of scope.

### 6.7 Actor order (helps F6)

Current order is ascending EntityId (preserved contract from 11-07). Proposed: actors that can attack from their current cell first, then ascending `RemainingCost` to their best target, ties by EntityId. Front units act first and clear corridors. It needs a cheap first pass over all actors (current-cell attack check plus field cost) before scoring; the field built in that pass is kept for the actor's scoring, so it is not built twice. Deterministic; no rule change. Decision Q-R4.

Revised in R-4 (user decision, 2026-11-10): the order is **attack-ready first, then EntityId**. Sorting by field cost needs every actor's field in every decision and made the first decision 6.6× the R-0 work; fields are now built only for actors that are evaluated. See the plan's R-4 record.

### 6.8 Side-scoped reuse of shared structures (fixes F9)

Reusing whole actor results across decisions cannot be made identical to recomputation: the per-decision Preview budget is shared across actors, the best target can be anywhere on the board, and long-range shapes reach far. This design therefore reuses only structures whose read set is explicit:

- **Player threat field and hazard forecast.** Within one enemy side, players do not act; they change only through enemy actions (HP, death, forced movement) and gimmick state. Key: battle id, controller turn sequence, and a digest of player-side character facts, gimmick states and visible environment.
- **Per-target goal candidate sets** (before the landing/occupancy filter, which is re-applied on use). Key: target footprint and facts read by `EnemyAttackReach`, attacker profile tuple, walls/elevation/haboob state.
- Actor-result reuse is deferred (Q-R7). If wanted later, it needs per-actor budgets or budget replay plus a whole-board change digest.

Visibility: `CombatAdvanceService` lives in `ProjectL.Systems` and only sees the public session API. The cache must be an Application type exposed as a public opaque handle (one public sealed class with no data members, plus one public `EnemyDecisionSession` constructor overload that accepts it). `CombatAdvanceService` creates the handle at side start and drops it at side end or battle change. This is a public API addition (Q-R10). No Framework registration; not a service.

Identity rule: a side played with and without the cache must produce byte-identical traces. A digest that misses an input is a defect caught by that test.

### 6.9 Diagnostics

Promote the per-candidate breakdown used in section 2 to an internal diagnostic record on `EnemyDecisionDiagnostics`, captured only when requested: actor, candidate, each score term, Hold terms, and reason codes (`NoReachableTarget`, `LethalOnly`, `ExchangeLoss`, `Pruned`, `WorldUncertainty`). The CoreIntegration corpus needs to read it: add `InternalsVisibleTo("ProjectL.Tests.CoreIntegration")` to `Core/Application/Properties/AssemblyInfo.cs` (test visibility only, Q-R11) instead of reflection.

## 7. Ownership, SSOT, SOLID, Framework, change independence

| Fact | Single owner | Others must not |
|---|---|---|
| Which sources enemy assessment ignores | `CombatCommandEffects` enemy-assessment mode (6.2) | Sum all allies' HP for every candidate; change real-combat coverage |
| Obstacle reaction area | `CombatHazardAreas` | Re-implement barrel/mine areas in AI |
| Attackability, range, occlusion, haboob rule | Attack owners via `EnemyAttackReach` | Re-implement shape or haboob checks in AI |
| Moved-actor facts for follow-up | Movement side shared helper | Set `Moved` / move flags inside AI |
| Multi-turn route cost and best target | `EnemyEngagementField` | Pick targets by Manhattan distance (except truncation fallback) |
| This-turn movement legality | `MovementRouteValidator` / `CombatMovementCalculator` | Let soft ally cost leak into actual paths |
| Designer-tunable values (death trade, threat aversion, next-turn approach) | `EnemyAiTuningDataSO` asset → `EnemyDecisionSettings` | Hard-code them in AI |
| Other numeric weights and defaults | `EnemyAiTuning` | Hold numbers in the scorer or candidate code |
| Score formula | `EnemyUtilityScorer` | Rank candidates with a different value model |
| Candidate pruning | Beam in `EnemyActionCandidates` using the scorer | Lexicographic safety-first rank |
| Side cache validity | Application cache keyed by explicit read sets | Keep results beyond one side or without a digest |

SRP: field (distance/targets), follow-up geometry (attack estimate), beam (pruning), scorer (formula), tuning (numbers), session (orchestration), side cache (reuse). OCP: new terrain, traits and artifacts are handled by their owners' declarations, not AI switches. DIP: AI depends on shared queries and capabilities.

Framework: no change to FW-001–016 surfaces, services, modules, Factory lines or ServiceKeys. Buffer, View, Flow untouched.

Public API changes (all need approval): default `moveCandidateLimit` 4 → 6 (Q-R6); side cache handle type and session constructor overload (Q-R10); `InternalsVisibleTo` for CoreIntegration tests (Q-R11); three optional tuning parameters on `EnemyDecisionSettings` (Q-R12). Data addition: one InGame DB config table (6.5.1).

Change scenarios:

| Change | Edit | Re-verify |
|---|---|---|
| Tune bravery or approach (designer) | `EnemyAiTuning` asset values | Corpus metrics |
| Tune other weights | `EnemyAiTuning` code constants | Corpus metrics, decision unit tests |
| New artifact or item effect | Nothing in AI (ignored by design) | Corpus with loadouts |
| New obstacle type | Its reaction area in `CombatHazardAreas` | Obstacle scoping tests |
| New attack shape or terrain modifier | Feature owner only | Field goal tests, corpus |
| New trait that changes attack after moving | The movement-side moved-actor helper, if it adds per-turn movement state | Follow-up tests |
| Actor order policy | Session ordering function | Publication parity, corpus |
| Beam width | `EnemyDecisionSettings` | Budget tests, measurements |

Files expected to change: `Core/Application/Combat/CombatCommandEffects.cs`, `Core/Application/Combat/CombatActionAssessment.cs`, `AI/EnemyActionCandidates.cs`, `AI/EnemyAttackReach.cs`, `AI/EnemyDecisionSession.cs`, `AI/EnemyUtilityScorer.cs`, `AI/EnemyDecisionContext.cs`, `AI/EnemyDecisionDiagnostics.cs`, `Core/Contracts/Combat/EnemyAi/EnemyDecisionSettings.cs`, the movement-side moved-actor helper (location chosen in the plan), `Core/Application/Properties/AssemblyInfo.cs`, and `Core/../CombatSystem/CombatAdvanceService.cs` (holding the cache handle). New: `AI/EnemyEngagementField.cs`, `AI/EnemyFollowupGeometry.cs`, `AI/EnemyAiTuning.cs`, `AI/EnemyDecisionSideCache.cs`, and the tuning table (`EnemyAiTuningDataSO`, `EnemyAiTuningData`, `EnemyAiTuningTable`, its InGameGroup registration and one asset) (each with `.meta`). Retired: `AI/EnemyEngagementMap.cs`. Tests whose expectations change by intent are listed per phase in the plan, with permission requests for tests written by other workers.

## 8. Validation (fixes F8)

### 8.1 Production-stage behaviour corpus

Generalize the two diagnostics into `Tests/CoreIntegration/EnemyStageBehaviorCorpus.cs`:

- Stages: all LD-series combat stages plus normal and boss stages (exact list Q-R8).
- Loadouts: no artifact; a representative artifact set; equipped items; rosters with undeclared traits (boss, CactusCannon).
- Player scripts: hold back, advance on round 1, spread; fixed deployments per stage; 4–6 rounds.
- Metrics per stage, loadout and script: idle ratio (actors with a reachable attack position that hold), retreats (moves that increase field cost to every target without a hazard reason), first contact round, enemy deaths to known hazards and to lethal threat, decision work, decision latency.
- Pinned regressions as normal tests: one artifact does not idle the side; stage 1127 B round 2 Demon does not hold; B round 3 Demon does not move away while another target is reachable; a unit behind an ally in a corridor does not report "no reachable target".

### 8.2 Existing suites

Full `ProjectL.Tests.Core` and `ProjectL.Tests.CoreIntegration` against the current baseline (64 known failures). Decision-level unit tests (`EnemyUtilityDecisionTests`, `EnemyActionCandidateTests`, `EnemyEngagementProgressTests`, `EnemyFollowupEstimateTests`, `EnemyEffectHarmScopeTests`, `EnemyAlliedExposureTests`) will change expectations where behaviour changes by intent; each change is listed and justified. `EnemyUtilityAcceptanceMeasurements.CaptureHold` encodes the old all-Hold state and is already failing at HEAD; it is retired or rewritten.

### 8.3 Performance

Same harnesses as the 11-09 Phase C measurements (`phase-6-corpus`, `phase-6-atoms`, `phase-7-service`) plus the stage corpus. New work: the multi-target field (targets × candidate cells × delivery check per actor) and K geometric follow-ups, which production rarely ran before because F2 blocked them. Offsets: shared geometry, per-decision goal-set cache, side-scoped reuse. Targets: warmed Step p95 unchanged; service preparation p95 ≤ 1.1 ms; no new synchronous drain; first-decision work and decision latency reported against current HEAD. Stop rule: if first-decision work exceeds 1.5× current HEAD on the corpus, stop and report before tuning K or limits.

## 9. Risks

- Bravery may feel reckless on some maps. Mitigation: lethal hazards are never candidates; the death trade price is a designer value; the corpus counts enemy deaths to players and to hazards separately.
- Ignoring artifacts, items, held environment and undeclared traits means the AI can mispredict outcomes they change (accepted by the user). Mitigation: real combat is unaffected; diagnostics mark these candidates as approximate.
- Geometric follow-up may miss effects that change attack after moving. Mitigation: shared moved-actor helper owns movement flags; route-local gate; diagnostics flag approximations.
- Actor reordering changes presentation order. Mitigation: Q-R4; parity tests compare prepared and synchronous runs.
- Side cache correctness. Mitigation: explicit read-set digests; mandatory cache on/off identity test; cache can be disabled.

## 10. Phases (proposal; Task plan follows approval)

| Phase | Content | Stop and report |
|---|---|---|
| R-0 | Settle current uncommitted work; build the stage corpus with loadouts; record baseline metrics on current HEAD | Baseline idle/retreat metrics with and without artifacts |
| R-1 | AI-ignored sources and obstacle scoping (6.2) | Artifact/item/trait corpus delta, harm-scope tests |
| R-2 | Engagement field (6.3); remove Manhattan target | Field tests, corpus delta |
| R-3 | Geometric follow-up, exchange model with death trade and lethal-hazard filter, `EnemyAiTuning` constants and the tuning asset (6.4, 6.5, 6.5.1) | Decision tests, asset/DB tests, corpus delta |
| R-4 | Beam selection (6.6) and actor order if approved (6.7) | Candidate tests, budget, corpus delta |
| R-5 | Side-scoped shared-structure reuse (6.8) and diagnostics record (6.9) | Cache identity test, measurements |
| R-6 | Full suites, measurements, docs, manual playback checklist | Final evidence |

R-1 comes first because section 2.2 shows it alone can idle a whole side in any stage.

## 11. Decisions requested

All recommendations below were accepted by the user on 2026-11-09 ("권장안대로"); the "Recommendation" column is now the decision.

| ID | Question | Recommendation |
|---|---|---|
| Q-R1 | **Decided 2026-11-09:** AI ignores artifacts, equipped/temporary items, held environment and undeclared traits; obstacles count only when the candidate can trigger them | — |
| Q-R2 | **Decided 2026-11-09:** brave-not-suicidal model; lethal hazards never chosen; death by player is a trade priced at 50% of a unit's max HP (kills always qualify); price, threat aversion and next-turn approach editable in a DB asset | — |
| Q-R3 | Ally soft cost in the field | 2 extra steps per allied cell |
| Q-R4 | Actor order | Readiness first (can attack now, then field cost), ties by id |
| Q-R5 | Target value | One-attack loss permille + elimination weight; units before HQ |
| Q-R6 | Beam width / precise refinements; public default change | K = 6 (default `moveCandidateLimit` 4 → 6), R = 2; remove hard-coded caps |
| Q-R7 | Actor-result reuse across decisions | Defer; reuse shared structures only (6.8) |
| Q-R8 | Corpus stage list and pass thresholds | All LD combat stages + normal/boss; thresholds set after R-0 baseline with the user |
| Q-R9 | Repeated-Hold penalty | Still excluded |
| Q-R10 | Side cache as public opaque handle + session overload | Yes |
| Q-R11 | `InternalsVisibleTo` for CoreIntegration on Core.Application | Yes (test-only visibility) |
| Q-R12 | Tuning values travel as three optional parameters on public `EnemyDecisionSettings`, read from the DB by `CombatAdvanceService` | Yes |
| Q-R13 | Current uncommitted changes (truncation fallback, 289 cap, Phase C fixture, two diagnostics) | Commit before R-0 as the baseline; diagnostics become the corpus seed |

## 12. Review record

One independent read-only review (2026-11-09) checked this draft against the code. Adopted:

- High: the world-level allied-harm ceiling was missing as a cause; it is now F1 and was confirmed behaviourally (section 2.2).
- High: whole actor-result reuse cannot be identical (shared Preview budget, board-wide field, source stamp changes every publish) and an internal cache is invisible to `CombatAdvanceService`. Replaced by explicit-read-set structure reuse behind a public opaque handle (6.8, Q-R10).
- Medium: F2 wording (SteadfastWill declares no gaps; non-trait blockers; only the first two moves get `MovedFacts`); hard-coded `Math.Min(4)` / `Math.Min(2)` caps; worked example now includes Hold's next-turn term; threat cap inversion and missing self-elimination term; follow-up must reuse `MovedTo`, set `Moved` and move-sensitive state through the movement owner, re-check taunt at the endpoint, and choose the aim by value; goal bounds must follow `EnumerateAimsThatCanHit` (pull, splash, pierce); truncation fallback defined; next-turn haboob through `CanDesignateAt`; performance stated as measured targets with a stop rule.
- Low: F5 no longer claims HP-based target flips; F8 acknowledges W8/H8/T8; weights have one owner; diagnostics visibility made explicit (Q-R11); multi-cell attack deferral reported as outside AI scope; actor ordering cost noted.

## 13. Future extension: base defense (deferred)

User decision (2026-11-09): enemies do not defend their headquarters in this release; build the fundamentals first. The foundation leaves room for it without rework:

- **Threat-to-base priority:** in the engagement field (6.3), raise `TargetValue` for player units that can attack the enemy headquarters at the next boundary. Enemies then converge on attackers of the base through the same field and beam.
- **Guard posture:** when no target is reachable, score positions that cover the base's approaches instead of the current cell (a Hold variant scored by the same model).
- **Guard roles:** optionally limit guards to unit categories or flags set in data.

Relevant fact: stage 1127 uses `BattleObjectiveKind.EnemyHeadquartersDestroyed`, so the player wins by destroying the enemy headquarters; today's AI has no notion of it.

## 14. Implementation memo (2026-11-10, phases R-0 to R-6)

Implemented in phases R-0 to R-5 and verified in R-6; the phase records in the plan hold the evidence. Where the implementation differs from the sections above, this memo is the current description.

- **6.2 AI-ignored sources.** As designed, with one decision during R-1: the undeclared parts of an effect rule are ignored separately. A rule without a coverage declaration adds no gap, harm or projection block unless its harm applies to the candidate (declared unbounded harm). Obstacle scoping applies to attacks only (moves cannot enter structure cells).
- **6.2 Forced movement of the acting attack (user decision 2026-11-10, after the manual check; replaces "knockback-type rules keep the existing acting-attack condition").** Rules whose harm happens only inside the acting attack's resolution (knockback-capability rules and rules declaring `ActingAttackOnly`: Knockback, ImpactBolt, Pull) add no gap, ceiling or block in enemy assessment; the precise preview executes the forced movement with the real rules. Arrival coverage and powder-barrel seeding look only at the forced reach (pull path; cells within the knockback distance of the selected targets), with the route check's rules (known mine → blast exposure; other arrival source → ceiling; tornado contact → every source). Before this, every enemy whose own attack pushes or pulls was charged every ally's HP and never attacked (Gravekeeper: 0 attacks in the corpus). The pinned corpus test `EveryEnemyKindAttacksWhenItCan` guards the class. Details in the plan's record "Forced-movement attacks fix".
- **What the artifact checks mean (R-6).** `OneArtifactDoesNotChangeEnemyDecisions` compares inputs whose battle facts are identical and differ only by the ignored source (the placed artifact). Loadout threshold 1 is the separate "no stall" check on the corpus. An artifact that has already changed HP, attack or range before the decision may change decisions; that is a legitimate reaction to changed facts, not a failure of either check.
- **6.3 Engagement field.** As designed, except the per-decision goal-set cache keyed by profile tuple: goal cells depend on per-actor facts (range, restrictions, destination modifiers, size, other units on a piercing line), so every actor computes its own goals. A per-actor reuse across decisions in the side cache was built in R-5 and removed after R-6 (user decision, 2026-11-10): it gave about 4% of the cache saving for the most complex read set.
- **6.4 Geometric follow-up.** The moved-actor helper lives on the movement side (`CombatMovementCalculator.MovedActor` / `ApplyMoveSucceeded`); route completion uses the same success effects.
- **6.5 / 6.5.1 Exchange model and tuning asset.** The three designer defaults live on the public settings contract (`EnemyDecisionSettings.Default…Percent`), not in `EnemyAiTuning`, because the DB input SO needs them and cannot see Core.Application internals (one owner per value). `NextTurnStrike` checks designation from the endpoint. `GuaranteedTerminal` cannot be true for assessment facts (the assessment always keeps `HiddenEnvironment`), so the guaranteed-win exemption is dormant in play. The non-prepared path (`CombatCommandCalculator.Advance` → `EnemyAiTurnService`) still uses default settings; production uses the prepared path, which reads the asset. ServiceKey added: `InGameTableKey.EnemyAiTuning`.
- **6.6 Beam.** As designed (K = 6, R = 2, reservations inside the beam, one function owns priority and K).
- **6.7 Actor order.** Attack-ready first, then EntityId (user decision in R-4; see 6.7).
- **6.8 Side cache.** Public opaque handle `EnemyDecisionSideCache` and one public session overload; `CombatAdvanceService` creates it at the first preparation of an enemy side, keeps it across preparations of that side and drops it at side end or battle change. Scope = (battle instance id, controller turn sequence). Each builder owns its read set next to its code (`StageHazardForecast.ReadSet`, `EnemyThreatField.ReadSet`); the cache stores and compares. Goal sets are not cached (6.3 above). Read sets as implemented (details in the plan's R-5 record):
  - Forecast: phase, turn, objective, board, gimmick states, the battle's gimmick definitions and rail cells, the main and the objective target, and the profiles/owned records used by `Evaluate`. It does not read other units or the environment.
  - Threat field: the whole battle state, environment bindings, profiles, owned records, artifacts, a presence summary of every character, every player character's facts and soul count; every character's facts only when a ready player has splash or piercing (enemy positions are read only through splash centres and piercing encounters).
  - Profiles are compared by owned id, unit key and method reference, owned records by value; unit definitions are covered by the battle scope, because the input assembler recomposes them every decision from the battle definition (fixed per battle) and the owned records (compared). Specialization and trait growth or training are rejected during battle and live in the owned records. Confirmed by the user on 2026-11-10.
  - Effect: identical traces with and without the cache (fixtures and production stages, partitions 1/2/31); stage corpus work −8.9% in R-5 (later decisions −10.6%), about 92% of it from threat-field reuse; goal-set and forecast reuse contributed about 4% and 3%. After removing the goal-set reuse the corpus work is −8.6% against R-4.
- **6.9 Diagnostics.** As designed: per evaluated actor a record of the hold terms, hold tier, hold lethality and hold reason, with every compared candidate's terms, tier, lethality and precision, captured only with `captureAlternatives`. The score formula has one owner (`EnemyUtilityScorer.Terms`, `Score` = its total). The corpus reads hold reasons and lethality from the record and writes `corpus-candidates.tsv`; the stage 1127 reflection diagnostic was replaced by an explicit record dump.
- **8. Validation (R-6, details in the plan's R-6 record).** Full suites: no failure beyond the 60 known. Thresholds 1, 2, 4, 5 and the main parts of 6 pass. Threshold 3 (retreats without a hazard reason ≤ 2% of moves) was missed at 2.55% (16 / 627) with the static-terrain metric; every one of the 16 is explained by the value model (3 route detours that the field counts as approach, 7 moves to another attack position followed by an attack, 1 move to a next-turn kill position, 5 steps out of a threatened cell at a cost of one or two field steps). By user decision (2026-11-10) threshold 3 is now judged by the AI's own route cost (a move whose published progress term is negative); it passes at 1.91% (12 / 627: 9 steps out of a threatened cell, 4 of them keeping a next-turn kill, and 3 moves to a next-turn kill position). The static metric stays in the corpus for reference. The refinement on threshold 6 (per-update p99 for 6–8 enemies within 10% of R-0) was borderline in R-6 (median +9.1%, mean +10.5% over five runs, the same value with the cache switched off). By user decision (2026-11-10) it is judged by the median of at least five runs; on the final code (goal-set cache removed) the median is +7.2% (mean +9.0%, one run of five over the limit), so it passes. Single preparation updates in the 6–8 group still reach 8–9.5 ms (R-0 under 4 ms). The follow-up investigation (plan, end of the completion record) found that every update over 4 ms, at R-0 and now, is a Unity GC pause (incremental GC with a 3 ms slice, or a collection completing), and that no preparation Step costs more than 0.05 ms structurally (minimum over identical repeats); the per-update p99 moves with GC count and the machine. By user decision (2026-11-10) the threshold 6 refinement is therefore the structural Step limit: the largest structural preparation Step (`phase-7-atoms`) is at most 0.1 ms. The final code measures 0.048 ms and passes; `phase-7-service` reports p99, max, GC-affected updates and execute frames as context.
