# Code Review — Click Shortest-Path Move, Meteor, and Lava Chain (Core)

- Date: 2026-11-17
- Status: Review complete. Fixes R1–R6 are applied in the working tree. On 2026-11-17 the user chose "both as recommended": Q1, D1, D3, and Q3 are now implemented (§7). D2 remains a separate design before Phase 4. **Unity compile and tests have not been run for any of it.**
- Code baseline: `f6b6ae1f8` (click shortest-path move) and `1abe42230` (Meteor / Lava Chain). The working tree also holds another session's uncommitted cactus-cannon thorn-bomb changes (for example `MovementProjection.cs` near line 343, `CombatStateBuffer.cs`, `CombatEvent.cs`). The fixes below do not touch those hunks.
- Criteria: the user's feature-change locality guideline (modification / understanding / verification scope), SSOT, SOLID, JMH Framework contracts, and hardcoding.
- Method: every Core/Systems file changed by the two commits was read together with its design and plan ([shortest-path design](2026-11-17-combat-click-shortest-move-core-design.md), [gimmick design](2026-11-17-new-stage-gimmicks-core-design.md)). Findings were traced in source. "Verified" means source inspection only.

## 1. Verdict

**Click shortest-path move** — Structurally sound and matches design C1–C4: the BFS moved into `MovementRangeSearch`, the range response derives destinations from routes, and `CombatPreviewService` stamps the pre-calculation `QueryRevision` and checks it before publishing. No defect found. One duplicated rule was fixed (R6). The new contract has no Core tests (F7).

**Meteor / Lava Chain** — The rule logic follows the approved design (boundary phases, Eruption-before-Chain order, warning/impact/expiry, perpendicular push priority, retraction), and each gimmick state owns its own facts. Numeric tuning is not hardcoded: defaults exist only as stage SO authoring field initializers (design §4.4), and specs take explicit values. The problems are at the **shared seams the next four gimmicks will use**:

- occupancy was hard-wired to `LavaChainGimmickState` in three functions;
- generic movement code called the Meteor rule directly from two places;
- the "blocked by gimmick" check was copied inline;
- the chain's wall check bypassed the terrain-classification SSOT.

R1–R5 fix these. Movement undo receipts, the forecast evaluator, and validator RNG tracking still name each gimmick (D1–D3).

**On hardcoding:** the hardcoding found is mostly *type/kind* checks at shared seams, not numbers. Remaining literals are acceptable: effect-origin strings `"lava-chain"` and `"meteor:{impact}:{ordinal}"` each have a single source, and the 8-direction table is fixed topology. The `new CellCoord[8]` buffer was tied to the table (R-minor).

## 2. Fixes applied (R1–R6)

| # | Change case (guideline §2) | Problem and evidence | Change | After |
|---|---|---|---|---|
| R1 | Eruption/Altar pillars must block movement (plan Phase 5: "Task 3 derives fixed pillar cells from the active Eruption state"). | `GimmickOccupancyRules.BlockingCells / Overlaps / Blocks` each tested `entry is LavaChainGimmickState`. Adding an occupant meant editing three functions with chain knowledge. `BlockingCells` returned after the first chain found. | New `StageGimmickState.BlockingCells` (public virtual, empty by default). `LavaChainGimmickState` overrides it with `[HeadCell]`. The rules iterate every entry. | A new occupant overrides one property; the shared occupancy rules do not change. Behavior is the same. |
| R2 | Any change to "what a gimmick blocks". | "Persistent occupancy OR transient reservation" was copied inline in `AttackForcedMovement.FindBlocker` (twice) and the forced-admission check in `MovementEnvironmentCalculator`, although `Overlaps` already did the same thing. | `GimmickOccupancyRules.Blocks(gimmicks, cell, transientReservations)` is the only source; `Overlaps` and the three call sites use it. | One predicate. Same results. |
| R3 | Wall classification changes (bindings, new wall-like terrain). | `LavaChainGimmickRule.HardBlockers` tested `Active && Kind == Wall` itself and skipped the semantic-binding condition that `EnvironmentTerrainRules.IsTerrain` declares as the single wall judgment. | It uses `EnvironmentTerrainRules.TerrainCellSteps(environment, bindings, Wall)`. The signature gains `bindings`; the rule (4 calls), the validator (3), and `StageHazardForecast.ReadSet` (1) pass it. | Same wall definition as movement. A difference appears only for an active wall fact that is not in `TerrainSourceIds` (none known; the chain tests bind their walls). |
| R4 | Initial gimmick blocking rules change. | `LavaChainGimmickRule.Initialize` rejected a head on a wall or structure, without bindings, cliffs, or characters. `CellEnvironmentQuery.ValidateInitialPlacement(…, gimmicks)` already checks wall, unsupported cliff, structure, and character for every gimmick blocking cell, and `BattlePreparationCalculator` always runs it right after `Initialize` (the Buffer validator runs it again). | `Initialize` keeps only the board-bounds check. Preparation is the single overlap check. | A head on a wall is still rejected at battle entry, now with the preparation message ("기믹 차단 칸과 초기 지형·개체 배치가 겹칩니다."). No test asserts the old message. |
| R5 | Eruption center benefits and any later on-entry effect. | `MovementProjection.ResolveArrivalSteps` and the object-arrival path in `MovementEnvironmentCalculator` called `MeteorGimmickRule.ApplyEntry` directly. The rule found its spec by `GimmickDefinitions[state.Id.Value - 1]`. | New `IStageGimmickEntryRule`. `StageGimmickComposition.ApplyEntry(projection, arriver)` dispatches with the definition and id that composition already owns. `MeteorGimmickRule` implements the interface. | Movement code names no gimmick. A new entry gimmick implements the interface; movement code is not edited or re-read. |
| R6 | Range rule changes (landing, ordering). | `CombatMovementCalculator.Range` and `UnitMoveCells` each repeated "distance > 0 && `CanLand`, order Y→X" over the search distances, and `CanLand` ran twice for swap endpoints. | `MovementRangeSearch.Destinations` is built during the BFS; both consumers read it. `Distances` is now private. | One destination rule. Output order and contents are the same (route tie order is still the BFS's Right/Up/Left/Down). |
| R-minor | — | `ChooseHeading` sized its buffer as `new CellCoord[8]`. | `LavaChainGeometry.Directions.Count`. | — |

Changed files: `StageGimmickState.cs`, `LavaChainGimmickState.cs`, `GimmickOccupancyRules.cs`, `AttackForcedMovement.cs`, `MovementEnvironmentCalculator.cs` (two lines), `MovementProjection.cs` (one line), `IStageGimmickRule.cs`, `StageGimmickComposition.cs`, `MeteorGimmickRule.cs`, `LavaChainGimmickRule.cs`, `LavaChainGimmickEvidenceValidator.cs`, `StageHazardForecast.cs` (one line), `MovementRangeSearch.cs`, `CombatMovementCalculator.cs`. No new files and no test changes.

**Public contract note for View:** `StageGimmickState.BlockingCells` is a new read-only public property (additive). The Meteor/Chain View handoff is not published yet (gimmick plan Task 10); include it there.

## 3. Findings recorded, not fixed

| # | Finding | Why not fixed now |
|---|---|---|
| F1 | The gimmick id ↔ definition index convention (`GimmickDefinitions[id.Value - 1]`, `new StageGimmickId(i + 1)`) appears in about 13 places across Contracts and Application, including pre-existing Tornado code. The new code follows it. A named accessor (on `StageGimmickId` or `CombatBattleFacts`) would make it one source. | Most sites are pre-existing code outside this review (guideline §5). Do it the next time Tornado is touched. |
| F2 | Meteor site candidates do not exclude the chain head. `MeteorGimmickRule.BlockedCells` = living structures + voluntary-blocked terrain, so a footprint may include the head cell, which units cannot enter. Design §5.5 requires footprint cells on "terrain characters can enter"; §5.2 lists head exclusions for movement, placement, forced landing, and relocation, but not meteor sites. | Rule interpretation → Q1. **Implemented in §7.1.** |
| F3 | Two strategies check the environment RNG start. The Meteor validator assumes "only gimmick ⇒ expected random = input random" (`GimmickDefinitions.Count == 1`). The chain validator tracks Meteor's `FinalRandomState` and marks the value unknown after Tornado/Haboob/Rockfall events. Every later RNG gimmick must edit the other validators, and the Meteor check silently stops checking when another gimmick exists. | → D3. **Implemented in §7.3** (also fixes a latent false rejection found there). |
| F4 | Movement undo of meteor flame Fire is hard-wired: `MovementEffectUndo.FireEffect` checks `DefinitionKey == Meteor`, reads `MeteorGimmickState` and the spec by index, and `MovementFireReceipt` rejects non-Meteor origins. | → D1. **Implemented in §7.2.** |
| F5 | The hazard forecast evaluator branches per gimmick (`fact.Key == LavaChain`, `fact.Meteor != null`, `MeteorGimmickRule.HealthDamage`; `ReadSet._chainBlockers` behind `is LavaChainGimmickState`), and `HazardCellFact` grows per-gimmick optional fields. This extends a pre-existing pattern (Rockfall, Haboob, Tornado). | → D2. Touches enemy AI forecasting. |
| F6 | `LavaChainGimmickRule.ResolveContact` rebuilds the push step with `Math.Sign(target − origin)` although `FindLanding` already chose the side, and repeats the forced-step sequence (`RecordForcedMove` → `ResolveForcedTerrainEntry` → `ReconcilePresence` → `ResolveEntrySteps` → `ResolveForcedArrival`). No shared multi-step push helper used by knockback was found to reuse. | Low. Revisit if another lateral-push effect appears. |
| F7 | The shortest-path contract (K1–K7) has no Core tests. Its plan excluded tests by applying the View-work test rule to a Core plan; Core tests in `ProjectL.Tests.Core` are allowed. | Follow-up offer → Q3. **Implemented in §7.4.** |
| F8 | `MeteorGimmickRule.Plan`'s minimum fallback `FitMinimum` is a backtracking search. With a large authored minimum count and a crowded board it can explore many combinations before failing (the area pre-check only bounds by cell count). | Low with current defaults (minimum 2). An authoring limit or iteration cap can be added if larger minimums are used. |
| F9 | `MovementRangeResult` derives occupied cells as `[route.Endpoint]` (single-cell units) and `TryGetRoute` scans linearly. | Accepted by the design for current player units and range sizes. |

## 4. Proposed designs (not implemented)

### D1 — Entry undo receipts come from the entry rule

- **Change case:** Phase 5 Eruption. A center shield granted on entry must be undone with movement undo, restoring the grant entitlement (gimmick design §6, last row).
- **Current burden:** the implementer edits `MovementEffectUndo` (a new Eruption-aware effect next to `FireEffect`), adds a receipt type, and adds its `Matches` and restore cases. They must read the meteor Fire undo internals to copy them.
- **Proposal:** extend `IStageGimmickEntryRule` with `CollectEntryReceipts(input, movement, before, after, requireContact, receipts)`. `StageGimmickComposition` aggregates it, and `MovementEffectUndo` calls the composition at its two sites. Move `FireEffect` into `MeteorGimmickRule`. `MovementFireReceipt` validates `EffectOwnerKind.StageGimmick` instead of the Meteor key; it becomes the generic "Fire status changed by a gimmick entry" receipt. Restore and match cases stay generic per receipt type.
- **After:** the rule that applies an entry effect also recognizes it for undo, so apply and undo live in one place. Movement undo has no gimmick knowledge.
- **Keep:** receipt identity and restore order. Verify with `MovementUndoRulesTests`, `MovementUndoPublicationTests`, `MeteorGimmickTests`, and `MeteorPublicationTests`.

### D2 — Forecast impact supplied per rule

- **Change case:** Phase 4 Smog (multiplier, low-HP execution) and Phase 5 Eruption (30% damage, relocation uncertainty).
- **Current burden:** each adds `HazardCellFact` fields and branches inside the shared evaluator loop and `ReadSet`. The author must understand the Rockfall/Haboob/Tornado/Meteor/Chain handling in the same loop and re-verify all forecast behavior.
- **Options:**
  - A — migrate all gimmicks, including legacy ones. Large; touches stable code.
  - B — new gimmicks only. `HazardCellFact` gets one impact object (health loss for an actor, position uncertainty, death-group key) produced by the rule's `EnumerateForecast`. Meteor and Chain move to it; legacy fields stay until touched. `ReadSet` asks rules for extra read tokens instead of special-casing the chain.
  - C — keep the current pattern.
- **Recommendation:** B, as its own short design and plan before Phase 4, because enemy AI forecasting must stay identical (`EnemyHazardForecastTests`).

### D3 — One environment-RNG evidence trace

- **Change case:** Smog band selection and Eruption site/relocation draws at Scheduled and Warning boundaries.
- **Current burden:** the chain validator must learn their event types, and the Meteor heuristic silently weakens.
- **Proposal:** a Contracts interface (for example `IEnvironmentRandomEvidence { uint? ExpectedRandomState; uint? FinalRandomState; }`) implemented by the Meteor and Chain events, plus a shared `EnvironmentRandomTrace` in `Application/Combat/StageGimmicks`. It walks the transition's events and updates the known value from declaring events. One list of legacy RNG events that declare no state marks the value unknown. Validators call `RequireStart(expected)`. This removes the `Count == 1` heuristic and the chain validator's Meteor and legacy event lists.
- **Verify:** `MeteorPublicationTests`, `LavaChainPublicationTests`, `GimmickEvidenceOrderingTests`, `MeteorGimmickTests`, and `LavaChainGimmickTests`.

## 5. Verification

Not run: Unity compile and Test Runner. Running now would mix in the other session's in-progress changes. After that session's work compiles, run all of `ProjectL.Tests.Core` and `ProjectL.Tests.CoreIntegration`. Expected: no new failures against the commit message baseline (Core 6,125 passed; selected CoreIntegration 142 passed; 11 diagnostics skipped). Targets most related to each fix:

| Fix | Tests |
|---|---|
| R1, R2 | `GimmickOccupancyTests`, `CoreIntegration.GimmickOccupancyPublicationTests`, `PlatformMotionTests`, `CoreIntegration.PillarPlatformPublicationTests` |
| R3, R4 | `LavaChainGimmickTests`, `LavaChainGeometryTests`, `CoreIntegration.LavaChainPublicationTests`, `EnemyHazardForecastTests`, `NewGimmickBoundaryTests` |
| R5 | `MeteorGimmickTests`, `CoreIntegration.MeteorPublicationTests`, `MovementUndoRulesTests`, `CoreIntegration.MovementUndoPublicationTests` |
| R6 | `MovementRouteTests`, `LeapTraitTests`, `PositionSwapMovementTests`, `UnitAttackRangeQueryTests`, `CoreIntegration.MovementPublicationTests` |

## 6. Decisions

Decided by the user on 2026-11-17 ("둘다 권장대로 진행해"), implemented in §7:

- **Q1 (F2):** Meteor excludes gimmick blocking cells (the chain head now; fixed pillars later) from its site candidates.
- **Q2 (D1–D3):** D1 and D3 implemented. D2 gets its own design before Phase 4 (not started).
- **Q3 (F7):** Core and CoreIntegration tests added for the shortest-path contract.

## 7. Follow-up implementation (2026-11-17)

Unity compile and tests have not been run. Another session was still editing the same working tree for the cactus-cannon thorn bomb (its last edits came minutes before this record). A batch run now would include its unfinished code and could collide with its Unity runs.

During this follow-up, that session also edited `MeteorGimmickRule.cs`, `LavaChainGimmickRule.cs`, `RockfallGimmickRule.cs`, and `StageGimmickComposition.cs`. It added a movement-rule constructor argument and `ForArrival(..., rules: _rules)`. Both sets of changes are present in the files; none of their lines overlap.

### 7.1 Q1 — Meteor sites exclude gimmick occupancy

- `MeteorGimmickRule.BlockedCells` takes the gimmick state set and adds `GimmickOccupancyRules.BlockingCells`. Warning selection and impact revalidation pass `input.State.Gimmicks`.
- The validator reads the gimmick set at the event's position from the shared spatial projection (§7.3).
- Effect: candidate windows that contain the head disappear, so RNG picks among a smaller list on boards where the head lies inside a window. The existing chain + meteor integration fixture keeps the head outside the only window, so its draws are unchanged.
- Test: `MeteorGimmickTests.SitesNeverIncludeACellOccupiedByAnotherGimmick`.

### 7.2 D1 — Entry undo receipts from the entry rule

- `IStageGimmickEntryRule` gains `CollectEntryReceipts(...)`. `StageGimmickComposition.CollectEntryReceipts` aggregates it and shares `EntryRules` with `ApplyEntry`.
- `MovementEffectUndo` calls the composition at its two sites. `FireEffect` and its Meteor knowledge moved into `MeteorGimmickRule.CollectEntryReceipts`, which finds its state by id instead of `GimmickDefinitions[id - 1]`.
- `MovementFireReceipt` now requires only a stage-gimmick origin (the base class check). The Meteor-key check is gone; the rule decides when to create one.
- Behavior unchanged. Covered by `MeteorGimmickTests` (movement into flames yields one `MovementFireReceipt`), `MovementUndoRulesTests`, and `MovementUndoPublicationTests`.

### 7.3 D3 — One environment-RNG trace, and a latent false rejection

**Found while implementing.** `HunterApplication.Reconcile` draws environment RNG when a marked target dies and two or more candidates remain. `MovementProjection.ChooseRandom` records the draw as `CombatEnvironmentRandomEvent` only in the `BeforeControllerTurn` window, so a draw during a gimmick projection leaves no random evidence; only a `CombatEffectsEvent` with `HunterTargetSelected` shows it.

The old chain validator compared its warning's expected random with the transition's input value unless a Tornado/Haboob/Rockfall event came first. So the following valid transition would have been **rejected**: a Meteor impact kills a hunter-marked target, a new mark is drawn, and the chain warns at the same boundary.

The old Meteor validator avoided this only because it skipped the check whenever any other gimmick existed (`GimmickDefinitions.Count == 1`).

**Change:**
- New Contracts interfaces in `Contracts/Combat/StageGimmickEvidence.cs`:
  - `IEnvironmentRandomEvidence` — implemented by `CombatMeteorEvent` and `CombatLavaChainEvent`, whose existing properties already match.
  - `IStageGimmickStateChange` — exposes the event's final gimmick state.
- New `Application/Combat/StageGimmicks/EnvironmentRandomTrace.cs` follows the known value in event order:
  - Declaring events update it: `IEnvironmentRandomEvidence` with a final value, and `CombatEnvironmentRandomEvent`.
  - One list makes it unknown: Tornado, Haboob, and Rockfall events, and Hunter target selection.
  - `Matches(expected)` replaces the per-validator start checks.
  - `EndsWith(declaring, final)` replaces the "last event ⇒ final state" checks. It also stays correct when a later gimmick (Smog, Eruption) warns after the chain.
- `GimmickEvidenceWorld` follows gimmick state through `IStageGimmickStateChange` and passes it into `CreateInput`. The chain validator no longer replays Meteor/Chain gimmick events itself; its `Current` state takes gimmicks from the projection.
- Effect:
  - The Meteor start check now also applies when other gimmicks exist. It defers after an unknown draw, the same as the chain's check.
  - The chain validator no longer lists other gimmicks' event types.
  - A new RNG gimmick declares its values on its event and needs no edit in the other validators.
- Remaining limit: any future environment-RNG consumer that leaves no evidence and can run inside a turn-end boundary before a gimmick warning must be added to `EnvironmentRandomTrace.ConsumesUndeclaredRandom`. Currently only kill-chain uses environment RNG outside that list, and it runs only in attack transitions, where no gimmick warning exists.
- Test: `EnvironmentRandomTraceTests` (start value, declarations, Hunter unknown, `EndsWith`). No end-to-end Hunter + Meteor + Chain fixture was added; building one needs a Hunter loadout and aligned schedules.

### 7.4 Q3 — Shortest-path contract tests

- `MovementRangeSearchTests` (Core):
  - tie order (Right → Up → Left → Down) and repeatability;
  - diagonal step count and wall detour length;
  - allowance boundary with no partial route;
  - every destination has one orthogonal route from the origin, with output in Y → X order;
  - `UnitMoveCells` equals the action range (R6);
  - `Success` copies its input and rejects malformed route sets;
  - `SourceRevision` copies, `Failure`, and `default`.
- `CoreIntegration.MoveRangePublicationTests` checks the full service path:
  - a published MoveRange carries the pre-calculation `QueryRevision`;
  - the query does not change the revision;
  - every route matches its destination;
  - one route runs through the existing Move preview and command as a single Move to its endpoint.

### 7.5 Files

- New, each with `.meta`:
  - `Contracts/Combat/StageGimmickEvidence.cs`
  - `Application/Combat/StageGimmicks/EnvironmentRandomTrace.cs`
  - `Assets/Tests/EditMode/MovementRangeSearchTests.cs`
  - `Assets/Tests/EditMode/EnvironmentRandomTraceTests.cs`
  - `Assets/Tests/EditMode/CoreIntegration/MoveRangePublicationTests.cs`
- Changed in this follow-up (in addition to §2):
  - `MeteorGimmickEvents.cs`, `LavaChainGimmickEvents.cs`, `MovementEffectReceipt.cs`, `MovementEffectUndo.cs`
  - `IStageGimmickRule.cs`, `StageGimmickComposition.cs`, `MeteorGimmickRule.cs`
  - `GimmickEvidenceWorld.cs`, `MeteorGimmickEvidenceValidator.cs`, `LavaChainGimmickEvidenceValidator.cs`
  - `MeteorGimmickTests.cs` (one test added)

### 7.6 Verification to run

After the other session's work compiles, run all of `ProjectL.Tests.Core` and `ProjectL.Tests.CoreIntegration`. In addition to the §5 targets:

- `MovementRangeSearchTests`
- `EnvironmentRandomTraceTests`
- `CoreIntegration.MoveRangePublicationTests`
- `MeteorGimmickTests`
- `LavaChainGimmickTests`
- `GimmickEvidenceOrderingTests`
- `CoreIntegration.MeteorPublicationTests`
- `CoreIntegration.LavaChainPublicationTests`
- `MovementUndoRulesTests`
- `CoreIntegration.MovementUndoPublicationTests`

Expected: all pass. If a validator test fails on "시작 난수" or "최종 난수", the trace is stricter than the old checks in a case that was previously unchecked; inspect that transition's events before loosening anything.

**Batch attempt (2026-11-17 16:48, Unity 6000.0.73f1, Core + CoreIntegration).** Batch mode stopped at compilation, so no test ran. It reported exactly two errors:

- `MeteorGimmickTests.cs:76` — this follow-up used `Does.Not.Contain(CellCoord)`, which this NUnit version accepts only for strings. Fixed to `Has.No.Member(head)`.
- `CombatStateBuffer.cs:13` — the concurrent thorn-bomb work added `using …CoreCaculator.Domain.Traits.Rules` for `ThornBombRules` in the Buffer. `ProjectL.Systems` does not reference `ProjectL.Core.Domain`. That line belongs to the other work and was not changed here.

`ProjectL.Core.Contracts`, `.Domain`, and `.Application` compiled without errors, and `ProjectL.Tests.Core` had no other error. So the Core changes and the two new Core test files compile; the CoreIntegration test has not been compiled. The run left no side-effect file changes (working-tree status, FMOD catalog, and LocalLow `TestResults.xml` are unchanged).

**Buffer compile fix (2026-11-17, user decision "옮겨줘").** The thorn-bomb placement check had been added to `CombatStateBuffer` (Systems). It called `ThornBombRules` (internal in Core.Domain, which Systems does not reference) and `CombatCommandCalculator.RequiredProfile` (internal in Core.Application). It was moved unchanged into the new public `Application/Combat/ThornBombs/ThornBombPublicationPolicy.ValidatePlacement` (plus null guards); the Buffer now calls it in the same position. This matches how the Buffer already calls `MovementUndoPolicy` and the gimmick evidence validators. No assembly reference or `InternalsVisibleTo` changed.

**Batch run (2026-11-17 17:09, Unity 6000.0.73f1).** Compilation succeeded.

| Assembly | Total | Passed | Failed | Skipped |
|---|---|---|---|---|
| `ProjectL.Tests.Core` | 6,151 | 6,132 | 8 | 11 |
| `ProjectL.Tests.CoreIntegration` | 1,192 | 1,184 | 0 | 8 |

- All tests related to this review pass:
  - `MovementRangeSearchTests` 8/8, `EnvironmentRandomTraceTests` 4/4, `CoreIntegration.MoveRangePublicationTests` 1/1
  - `MeteorGimmickTests` 11/11, `LavaChainGimmickTests` 13/13, `GimmickOccupancyTests` 9/9, `GimmickEvidenceOrderingTests` 4/4
  - `MovementUndoRulesTests` 26/26, `CoreIntegration.MovementUndoPublicationTests` 33/33
  - `CoreIntegration.MeteorPublicationTests` 7/7, `CoreIntegration.LavaChainPublicationTests` 6/6
- The 8 failures are frozen-reference comparisons:
  - `AttackPreviewParityTests` ×2, `AttackPreviewResumptionTests` ×3, `EnemyActionAssessmentTests.FrozenRoutePreviewAndExecutionRemainEquivalent`, `EnemyDecisionReuseParityTests` ×2.
  - Each fails because the serialized command state now has the thorn-bomb work's `ThornBombs` (and `ThornBombExplosionPreview`) where the frozen files have `ThornFields`. The decision-parity record already differs at the input-state hash, before any decision is made.
  - These references belong to the thorn-bomb contract change and need regenerating with it; they are not caused by this review.
- Side effects:
  - The run modified `Assets/ProjectL/Resources/Font/SCDream7 SDF.asset`; it was reverted to HEAD (it was clean before the run).
  - The LocalLow `TestResults.xml` was restored to the user's earlier file. The batch copy is kept with the run log in the session scratchpad.
