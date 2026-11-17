# Nexus Reinforcement Spawn Interval Design

Date: 2026-11-16
Status: Phases 1 and 2 implemented on 2026-11-16; Phase 3 acceptance remains pending.

## 1. Intent and decision status

Allow designers to set how many enemy turns must pass after a successful nexus reinforcement batch before another batch is eligible. Keep the existing field enemy limit and reinforcement pipeline.

**Confirmed by the user:** preserve the field enemy limit; make the spawn interval configurable through the SO; write the design before implementation. The motivating configuration is one enemy every two enemy turns, with one every turn also supported. When capacity or placement blocks a due batch, keep it due and start the next interval from the turn on which spawning actually succeeds. The user subsequently accepted partial-batch success: with batch size 2 and only one available cell, create one enemy, retain unused supply, and restart the interval from that turn.

**Documented defaults carried into planning:** the first attempt occurs on enemy turn N for interval N; the default interval is 1. Interpret "as soon as space is available" within the existing enemy-turn-start boundary: retry at each subsequent enemy start, without adding a mid-turn spawn trigger. These were stated in the reviewed draft; the user explicitly confirmed the partial-batch rule and requested the implementation plan.

This extension uses one authoritative last-successful-spawn turn value in the existing reinforcement state. It does not introduce a wave scheduler, a first-spawn-offset setting, or a countdown UI. Stage-specific rollout values are separate from adding the setting; existing assets are not automatically changed to interval 2 or batch size 1.

## 2. Current behavior and owning sources

The following observations come from the current code, rather than assuming older design documents still describe current execution.

| Owning source | Observed contract |
|---|---|
| [NexusReinforcementAuthoring](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Stage/Combat/NexusReinforcementAuthoring.cs) | Serializes enabled, batch size, range padding, field limit, and finite pool rows. `ToSpec` returns null when disabled and a new immutable definition otherwise. There is no interval field. |
| [NexusReinforcementSpec](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/NexusReinforcementSpec.cs) | Owns validated immutable settings. Defaults are batch size 2, padding 2, and field limit 5; stages may override them. |
| [CombatTurnCalculator](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatTurnCalculator.cs) | `EndPlayerTurn` increments `CompletedPlayerTurnCount`. `BeginControllerTurn` captures the living field-enemy count before start effects, completes ordinary start processing, then invokes reinforcement once before enemy AI. |
| [NexusReinforcementCalculator](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/NexusReinforcementCalculator.cs) | Limits creation by batch size, field capacity, remaining pool, and valid cells. Creates actors with `UnitActionUse.Deferred`. |
| [CombatTurnFacts](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/CombatTurnFacts.cs) | Carries the authoritative turn values already stored by the combat Buffer. |
| [NexusReinforcementState](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/NexusReinforcementState.cs) | Owns the bound nexus, reinforcement RNG, and append-only creation records. Records identify the actor and pool entry, but do not contain a creation turn. Last successful spawn timing cannot currently be derived from them. |
| [CombatStateData](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Combat/CombatStateData.cs) and [CombatStateBuffer](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Combat/CombatStateBuffer.cs) | Retain the prepared reinforcement definition and install validated creation facts, provenance, allocation progress, and events through the existing publication boundary. |

The limit counts living enemy characters across the entire field, including initial enemies and previous reinforcements. It excludes structures, dead characters, and off-field characters. The nexus neighborhood determines candidate spawn cells, not which enemies count toward the limit.

## 3. Approaches

The user selected extending the existing SO setting while retaining the field limit, then clarified that blocked batches must retry and the cycle must restart from actual success.

| Approach | Tradeoff | Decision |
|---|---|---|
| Interval measured from the last successful spawn | A due batch retries until it can create enemies, then starts a fresh interval. Requires one authoritative last-success turn value. | Selected direction following the user's clarification. |
| Fixed opportunities derived from the battle turn count | Predictable N, 2N, 3N schedule, but a blocked opportunity can delay spawning again after space opens. | Rejected because it does not match the clarified behavior. |
| Arbitrary wave/turn list | Supports irregular schedules but adds authoring and validation unrelated to a single interval. | Outside scope. |

## 4. Gameplay contract

An interval counts **enemy turns within the current battle**, not individual enemy actions, frames, real time, or both sides' turns separately. The first enemy phase following the first player turn is enemy turn 1.

| Interval | Batch size | Successful enemy turns when never blocked | Result when capacity, supply, and cells permit |
|---|---|---|---|
| 1 | 1 | 1, 2, 3, 4, ... | One enemy each opportunity |
| 2 | 1 | 2, 4, 6, 8, ... | One enemy each opportunity |
| 3 | 2 | 3, 6, 9, 12, ... | Up to two enemies each opportunity |

Apply these rules:

- Before the interval has elapsed, ordinary turn-start effects and enemy actions still run. Only reinforcement calculation is skipped.
- Once due, retry once at every enemy-turn start until at least one enemy is created. Opening space during a turn does not cause a mid-turn spawn.
- On a due turn, use the existing living-enemy count captured before turn-start effects. Deaths during those effects do not increase that turn's reinforcement allowance; a full field at that snapshot waits until the next enemy start. Candidate cells still use the world after ordinary start processing, as today.
- The created count remains `min(SpawnCountPerTurn, FieldEnemyLimit - livingAtStart, remainingPool, validCells)` when capacity is positive. The interval does not change the existing batch setting or its default.
- A full field, exhausted pool, or no valid cells produces no reinforcement and does not restart the interval. Waiting creates no extra batch allowance: only one ordinary batch can be created when space becomes available.
- A partial batch is allowed. Creating at least one enemy restarts the interval; an unfilled portion does not become an immediately pending second batch. Only created enemies consume pool entries.
- For interval 2, if turn 2 is blocked and space is available at turn 3's start, spawn on turn 3. Turn 4 is the waiting turn; the next attempt is turn 5. If turns 2 and 3 are both blocked and turn 4 succeeds, the next attempt is turn 6.
- A spawned actor remains deferred during its creation turn. It becomes ordinarily eligible at the next enemy-turn start, even if that next turn is not a reinforcement opportunity, subject to existing status restrictions.
- No creation occurs on player turns, during deployment, or after battle termination. A new battle clears last-success progress and first becomes due on enemy turn N.
- Pool consumption, selection RNG, placement exclusions, provenance, rewards, and ordered creation events retain their existing behavior. No creation means no consumption of pool entries, IDs, or reinforcement RNG and no change to the last-success turn; unrelated turn-start effects may still change the world.
- Success means an accepted, atomically published creation batch. An actor created and then killed by effects in that same result still counts as a creation. A rejected or stale result changes no last-success progress.

## 5. Minimal implementation boundary

### 5.1 SO and immutable definition

Add `spawnIntervalTurns` to `NexusReinforcementAuthoring`, initialized from `NexusReinforcementSpec.DefaultSpawnIntervalTurns = 1`, with Inspector minimum 1 and a Korean tooltip explaining the enemy-turn unit, first attempt, retry while blocked, and restart after successful creation.

Add a get-only `SpawnIntervalTurns` property to `NexusReinforcementSpec`. Append the optional constructor parameter `spawnIntervalTurns = DefaultSpawnIntervalTurns` after the existing parameters, preserving their order and defaults. Reject values below 1 with `ArgumentOutOfRangeException`; do not silently clamp invalid input. `ToSpec` passes the authored value. Disabled authoring continues to return null without activating reinforcement.

The existing definition path already carries the whole spec: `CombatStageData` -> `BattleDefinitionAdapter` -> battle preparation -> `CombatStateData` -> `CombatInputAssembler`. No extra DB query or separately copied interval is needed. The captured definition remains fixed for the active battle; later SO edits apply when a new battle definition is prepared.

Compatibility acceptance requires existing SOs without the new serialized field to load as interval 1, including after reimport/reload. Verify this with an existing serialized asset, not only a newly constructed SO. If field initialization does not preserve that value, include a targeted missing-field migration in the implementation plan before rollout; preserve asset GUIDs and all existing values. Explicitly authored zero or negative values must remain invalid. No general asset cleanup is part of this change.

### 5.2 Scheduling and state ownership

Add a get-only `long LastSpawnEnemyTurn` to the existing `NexusReinforcementState`. Zero means no batch has succeeded in this battle; a positive value records the enemy turn of the last successful batch. Initialize new/disabled reinforcement state to zero. Include the value in construction, `SameValues`, and every state reconstruction that preserves reinforcement progress. A nonempty creation history must carry a positive stamp; an empty history must carry zero. Reject negative or inconsistent values instead of inventing a historical spawn turn.

Use one pure eligibility member on the existing reinforcement contract, such as `NexusReinforcementState.IsSpawnDue(spec, enemyTurn)`, shared by calculation and publication validation. Invoke it inside `NexusReinforcementCalculator.TryCalculate` after the existing phase/configuration checks and before capacity calculation, candidate enumeration, or random draws. Keep `CombatTurnCalculator` responsible for orchestration and reinforcement responsible for its own eligibility policy.

At the existing enemy-start boundary, `CompletedPlayerTurnCount` equals the one-based enemy-turn ordinal: combat starts with the player, each accepted player end increments the value once, and enemy start retains it. Use that existing fact:

```text
enemyTurn = state.Turn.CompletedPlayerTurnCount
lastSpawn = state.Reinforcements.LastSpawnEnemyTurn
due = enemyTurn >= 1 AND enemyTurn - lastSpawn >= spec.SpawnIntervalTurns
```

Validate that the last-success stamp does not exceed the current enemy turn. Subtraction avoids adding an interval to a potentially large turn count. With a zero stamp, the same expression gives the first attempt on turn N. A blocked attempt leaves the stamp unchanged, so the batch stays due on later enemy turns.

Do not use `ControllerTurnSequence` directly; it advances for both sides. The existing Buffer owns the current turn and the reinforcement state containing the last-success stamp. The stamp records an additional historical fact, not a duplicate turn counter. Do not also store remaining cooldown, next-spawn turn, or a pending flag; all are derived from those facts. The calculator remains pure.

If not due, or due but unable to create, return the existing no-reinforcement result without changing the stamp. On a batch that creates one or more actors, set `LastSpawnEnemyTurn` to `enemyTurn` in the projected reinforcement state alongside RNG and provenance. Publish it in the same atomic result as the creations. Existing phase transitions still prevent a second accepted start for the same enemy turn.

Extend `CombatStateBuffer.ValidateReinforcementCreation` to require an unchanged stamp when no records are appended. When creation records are appended, require a due previous state and a final stamp matching the validated enemy-start turn, alongside existing creation/event checks. Reuse the shared eligibility member rather than copying the timing formula into the Buffer. A failed publication must leave all stores, including the stamp, unchanged. No new event kind, service, Job, Factory registration, or View polling loop is proposed.

## 6. Change, understanding, and verification scope

These are expected boundaries from source inspection, not verified implementation results.

| Scenario | Before | After this change | Necessary understanding and verification |
|---|---|---|---|
| Configure one stage for every second enemy turn | No authored interval; requires a code change. | Change `spawnIntervalTurns` in that stage SO; keep or separately configure its existing batch size. | Understand interval, initial delay, retry, and field cap. Check authored values and actual success timing. |
| Add the interval feature | Timing is implicit in the every-enemy-start call. | Edit `NexusReinforcementAuthoring`, `NexusReinforcementSpec`, `NexusReinforcementState`, `NexusReinforcementCalculator`, and `CombatStateBuffer`; update state-construction call sites such as `BattlePreparation` as required. | Understand turn-count semantics, start-effect ordering, immutable state equality, spec propagation, and atomic publication. Verify default compatibility, retries, stamp preservation, and rollback. |
| Spawn after a blocked attempt | Every enemy turn already retries naturally. | Retain retry at enemy starts after the interval expires, then delay the next batch from actual success. | Preserve field/cell checks and one ordinary batch. Verify turn 2 blocked -> turn 3 success -> turn 5 next attempt, including partial success. |

SSOT is preserved through one authored interval, one prepared immutable definition, the existing authoritative current-turn facts, and one last-success stamp in the existing Buffer-owned reinforcement state. SRP keeps input conversion, validation, orchestration, and reinforcement policy in their current owners. OCP is served by extending existing configuration/state contracts without new AI or View branches; LSP adds no new substitution behavior, and default interval 1 preserves valid gameplay; ISP adds no broad interface; DIP keeps Core calculation dependent on pure facts rather than Unity, DB, or Buffer implementations.

Under the [Framework programmer guide](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md) and [feature independence criteria, section 4.6](../handoffs/2026-09-30-combat-view-design-session-handoff.md), service registration and lifetimes remain unchanged, and no internal state boundary is bypassed. Turn ordering, state equality, and publication are necessary integration dependencies. Their verification is required even though the configurable rule is local. Initial placement, enemy AI scoring, presentation timing, and unrelated feature internals do not need redesign.

## 7. Acceptance and review boundary

The later implementation plan must cover:

- Definition/authoring: defaults and old constructor calls retain interval 1; positive values survive SO conversion and battle preparation; invalid enabled values fail; disabled reinforcement remains disabled; existing serialized assets load correctly.
- Timing: intervals 1, 2, and 3 follow the table when unblocked; the first attempt is on turn N; a zero turn count is not due; a new battle clears last-success progress; counting uses enemy turns rather than both sides or individual actions.
- Retry: full capacity or blocked cells keep an elapsed batch due. Check turn 2 blocked -> turn 3 success -> turn 5 next attempt, and turns 2/3 blocked -> turn 4 success -> turn 6 next attempt. No mid-turn spawn occurs when space opens.
- Batch progress: empty pools and failed attempts leave the stamp, reinforcement RNG, inventory, and allocation untouched. A partial batch restarts the interval and consumes only actual creations. Long blockage never accumulates extra batches. Creation followed by immediate death still restarts the interval.
- State integrity: equality distinguishes different last-success stamps; reconstruction preserves them; new/disabled states use zero. Reject negative, future, or history-inconsistent stamps. Publication rejects a changed stamp without creations, an incorrect success stamp, or creation before the interval elapses, without changing any store.
- Lifecycle: ordinary start effects run while waiting or blocked; pre-effect field counting is preserved; new actors wait until the next enemy start, including while the nexus interval is still waiting; player/deployment/terminal states do not spawn; duplicate starts remain rejected.
- Integration/regression: prepared settings reach the calculator; successful creations and their stamp publish atomically; no-creation results publish ordinary turn changes without resetting progress. Retain existing reinforcement geometry, provenance, rewards, and default-interval regressions, plus required Core/CoreIntegration checks.

Existing suites include `NexusReinforcementDefinitionTests`, `NexusReinforcementCalculationTests`, `NexusReinforcementLifecycleTests`, `NexusReinforcementStateTests`, `NexusReinforcementGeometryTests`, `NexusReinforcementRewardTests`, and the CoreIntegration authoring/publication suites. Before Phase 2, `NexusReinforcementTestData.Input` constructed synthetic enemy-turn facts with a zero completed-player count. Phase 2 supplies positive current-turn facts and explicit success stamps while preserving the existing assertions. Direct calculator fixtures that simulate repeated batches must advance the authoritative turn facts. Do not weaken timing or history invariants to accommodate impossible live fixture states.

Unity acceptance includes the SO Inspector setting and an interval-2 playthrough that becomes blocked, retries at the next eligible enemy start, and waits two turns from actual success. No View test code or combat View MSBuild work is proposed. Any eventual asset-value changes require the repository's `DataBaseAssetTests` acceptance step.

The [implementation plan](../plans/2026-11-16-nexus-reinforcement-spawn-interval-plan.md) records implementation and verification by phase. Phases 1 and 2 are complete; Phase 3 remains pending.

## Phase 2 delivery status

The implementation plan records the Phase 2 execution evidence: 146 focused EditMode passes after guard restoration. Full-assembly regression and Unity Inspector/playback acceptance remain Phase 3. No production-stage retuning or View implementation was performed.
