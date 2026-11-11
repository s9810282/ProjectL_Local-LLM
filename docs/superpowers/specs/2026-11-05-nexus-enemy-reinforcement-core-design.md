# Nexus Enemy Reinforcement — Core Design

Date: 2026-11-05

Status: Phases 1–4 are implemented. The final Core and CoreIntegration regression found no new failures against the pre-Phase 3 baseline. See the companion plan for observed results and the View handoff for playback and authoring contracts.
Companion plan: [Phase implementation plan](../plans/2026-11-05-nexus-enemy-reinforcement-core-implementation-plan.md)

Consumer contract and remaining View acceptance are recorded in the [Core-to-View handoff](../handoffs/2026-11-05-nexus-enemy-reinforcement-core-view-handoff.md).

## 1. Intent and scope

Give ordinary headquarters-objective stages a finite source of enemy reinforcements. The enemy nexus supplies new enemies at the start of an enemy turn, increasing pressure without supplying extra kill gold or experience. Destroying the nexus remains the existing victory condition. Boss stages do not use this feature.

This is Core work: authoring input, immutable battle definitions, pure calculation, existing Buffer publication, read-only range data, and Core/CoreIntegration verification. View implementation, effects, scenes, production stage population, balance tuning, and unrelated refactoring are outside this delivery. A new public event and range query require a View handoff before production activation.

Implementation documents are English. New code comments and XML documentation are Korean. Preserve existing Core paths, Framework/Buffer/Job ownership, public behavior outside the explicitly extended contracts, `.meta` GUIDs, and the existing misspelled `CoreCaculator` namespace. Codex must not commit or push. Future implementation stops and reports after each phase.

The supplied `feature-change-locality-guidelines.md` is a reference for the design review, not authorization to change code. Its modification/understanding/verification criteria are applied in section 10. This document makes no claim of an implemented or measured improvement.

## 2. Confirmed gameplay contract

| ID | Requirement |
|---|---|
| R1 | Initial stage enemies remain unchanged. The nexus pool is a separate reinforcement supply; initial placement does not consume it. |
| R2 | The pool is finite, authored as unit type plus quantity. Successful creation consumes one entry permanently. Death, night recall, and redeployment never replenish it. |
| R3 | At enemy-turn start, count living enemy characters currently on the field. Initial enemies and prior reinforcements both count; structures, dead characters, and off-field characters do not. The field limit is a per-stage setting, default 5 (2026-11-09). |
| R4 | Create up to 2 enemies per enemy turn by default. Designers can change this batch size. Respect capacity, remaining supply, and available cells. |
| R5 | Spawn only at enemy-turn start. A newly created enemy cannot move or attack during that enemy turn. It becomes ordinarily eligible at its next enemy-turn start, subject to other restrictions. |
| R6 | Default range is a 6 by 6 rectangle centered on the actual 2 by 2 nexus footprint, excluding the nexus's 4 cells: 32 cells before board clipping. Designers can change the outward padding, default 2 cells. |
| R7 | Pick randomly within the remaining pool and randomly among valid cells. Each remaining individual has equal probability: A x 3 and B x 2 give the next draw probabilities 3/5 and 2/5. Draw without replacement. |
| R8 | Valid cells are inside the range and board, with no living unit/building/obstacle, special terrain, object such as a trap/supply, or actual persistent ground effect. |
| R9 | Exclude the four orthogonally adjacent cells of every living player character on the field, including the main character. Diagonal adjacency alone does not exclude a cell. |
| R10 | Nearby unit influence, such as Taunt or Solo Action, does not itself block spawning. Immediately reconcile applicable existing effects with all new enemies present. |
| R11 | No valid cell or no supply means no spawn. Insufficient cells allow a partial batch. A failed opportunity consumes no pool entry and carries no extra batch allowance into the next turn. |
| R12 | Reinforcement kills award no kill gold and no kill experience. Initial enemies of the same unit type keep their existing rewards. |
| R13 | Surviving reinforcements use the existing enemy night recall/redeployment policy. Their first spawn cell becomes their redeployment origin; blocked origins use the existing nearest-valid-cell fallback. |
| R14 | Night changes retain pool consumption, entity identity, and reinforcement provenance. Returning an existing enemy is not a fresh spawn. |
| R15 | Core publishes the geometric range cells through the existing read-only Combat Buffer surface so View does not calculate the rule. |
| R16 | Reinforcement level follows the existing stage enemy level. Out-of-board range cells are clipped. |

### 2.1 Implementation decisions within the approved direction

These details were selected while writing the specification, rather than separately requested gameplay changes:

- 2026-11-09 change: the field limit is a per-stage designer setting (`fieldEnemyLimit`, default 5) next to batch size and padding. All three are independently editable positive integers. Originally this was a shared constant.
- Existing stages are opt-in: missing/disabled reinforcement authoring produces no definition. An enabled empty pool is valid and simply exhausted. No existing production asset is automatically enabled.
- 2026-11-09 change: the initial enemy count is not limited. An enabled stage may start above the field limit; reinforcements then wait until living field enemies fall below it. The original rule rejected more than 5 initial enemies; it was a spec-writing decision, not a requested gameplay rule.
- Pool entries must be unique by unit key, have positive counts, reference enemy-eligible regular units in the stage roster, and use the currently supported single-cell footprint. Invalid references, boss definitions, player-only definitions, or multi-cell candidates fail preparation. Do not silently skip bad entries.
- Spawn facing is `Rotation4.R0`, matching current random initial placement. It is not another authoring field.
- Roll the complete batch from one enemy-turn-start snapshot and reconcile presence after all its creation events. No AI action or intermediate publication occurs between members. Do not refill vacancies caused by effects in the same start boundary.
- Only gold/experience suppression is new. Preserve existing death handling, killer attribution, kill counters, kill-triggered effects, and stage-clear rewards. This does not introduce a new immunity to damage or debuffs.
- Initial deployment-zone restrictions are not an additional nexus-spawn condition. Only R6/R8/R9 constrain new spawn cells. Subsequent night redeployment deliberately uses the existing placement-zone policy, as agreed in R13.
- Actual rail cells are non-normal terrain and excluded. Warning-only cells and prospective explosion areas are not actual ground effects and do not independently exclude otherwise normal cells.

## 3. Current-code evidence

All paths below are repository-relative. They were inspected on `feature/Cheon/Core/NewCombatSytem` on 2026-11-05. These are source observations, not execution results.

Use these path prefixes throughout both documents:

| Prefix | Repository directory |
|---|---|
| `C/` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/` |
| `S/` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CombatSystem/` |
| `B/` | `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/` |
| `D/` | `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Stage/Combat/` |
| `T/` | `Assets/Tests/EditMode/` |

| Observed owner / function | Consequence for this change |
|---|---|
| `C/Application/Combat/EnemySpawnCalculator.cs`, `Calculate`; `S/BattleDefinitionAdapter.cs`, `Read` | Initial weighted placement happens during definition assembly and uses whole-board deployment rules. It is not a runtime finite pool and remains separate. |
| `Assets/ProjectL/DataBaseSO/InGameGroup/Structure/Item/Structure_Headquarters.asset` | The current nexus footprint is 2 by 2. The 32-cell interpretation matches this authored size. |
| `C/Application/Combat/CombatTurnCalculator.cs`, `BeginControllerTurn` | Resets existing actors to `Available`, runs turn-start traits, advances the controller sequence, and reconciles presence. This is the insertion boundary. |
| `C/Contracts/Combat/UnitActionUseRules.cs`; `C/Application/Combat/EnemyAi/EnemyAiTurnService.cs` | Existing `Deferred` actors cannot start movement or attack. A separate spawn-stun status is unnecessary. |
| `C/Application/Combat/BattleCharacterCalculator.cs`, `CreateEnemy` | Initial and generated enemies can share baseline stats, category, trait, and level initialization. |
| `C/Contracts/Combat/CharacterChanges.cs`; `C/Application/Combat/CombatCommandComposition.cs` | `Created`, `Project`, and `Merge` already support creation followed by effects/death and publish one final fact per new entity. |
| `B/Combat/CombatStateBuffer.cs`, `ValidateCreation` | New IDs must be contiguous, and every creation currently requires a character-summon record. Extend provenance validation; do not bypass the store. |
| `C/Application/Combat/CombatPlacementCalculator.cs`, `RecallForNight`, `RedeployEnemies` | Ordinary enemies return via `CombatTurnFacts.EnemyOrigins`; character summons use their summoner. New nexus enemies need an ordinary redeployment origin. |
| `C/Application/Combat/TraitPresenceReconciler.cs` | Solo Action, Cohesion, and Taunted reconciliation already exist. Use the complete projected character set, including new actors. |
| `C/Application/Combat/CellEnvironmentQuery.cs`; `C/Domain/Movement/EnvironmentTerrainRules.cs` | Spatial facts already classify terrain, objects, and occupancy. Ordinary movement blocks only walls/cliffs and is insufficient for the stricter spawn predicate. |
| `C/Application/Combat/Rewards/ConfirmedKillCalculator.cs`; `C/Application/Combat/CombatCommandCalculator.cs`, `Finish` | One credited-kill list feeds gold, experience, and character kill statistics. Dropping reinforcement kills from that list would change more than reward amounts. |
| `C/Contracts/Combat/CombatChangeEvidenceReader.cs` | Unknown event types fail evidence validation. A new creation event needs explicit evidence support. |
| `B/Combat/CombatStateData.cs`, `ReadTornadoAreas` | Buffer read models already derive geometry from authoritative battle facts without creating another mutable world. Follow that ownership pattern. |
| View `CombatEventTypeTable.cs` / `OrderedCombatPresentation.cs` | Unknown event types are skipped with a warning and final refresh is separate. New reinforcement playback is an explicit View integration dependency, not assumed complete. |

The current working tree also contains unrelated FMOD/font assets, `CombatEffectCapabilities.cs`, and `Portfolio_Output/` changes. They are outside this documentation and future feature scope. Recheck before implementation; do not overwrite them.

## 4. Approaches and chosen boundary

**Chosen: dedicated pure reinforcement calculation within the existing turn/publication pipeline.** Keep the pool policy, range filtering, deterministic draws, and reinforcement creation together. Existing combat orchestration determines when to call it; existing Buffers own installation. This is the approach approved in the conversation.

Alternatives considered:

1. Extend the initial `EnemySpawnCalculator` into one initial/runtime spawning engine. Some weighted-selection code is similar, but initial deployment and turn-start reinforcement differ in supply, placement rules, provenance, lifetime, and rewards. Sharing the whole policy increases the knowledge required to change either feature.
2. Model this as a new system/job or generalize all creation into a new framework. This would duplicate scheduling/state ownership or require speculative interfaces. The existing controller-start, `Created`, allocation, and atomic publisher contracts already supply the required mechanics.

Do not activate deferred Necromancy or change the StageGimmick end-boundary architecture as part of this feature. Its currently deferred behavior is an out-of-scope observation.

## 5. Definition, runtime state, and ownership

### 5.1 Authoring and immutable definition

Add `D/NexusReinforcementAuthoring.cs`, a serializable nested input used by `CombatStageData`. Fields: `enabled` (false), `spawnCountPerTurn` (2), `rangePadding` (2), and pool rows with `UnitDataSO unitDefinition` plus `count`. Convert references to keys. There is no second `EnemyLoadout` use, shared pool asset, mutable SO inventory, or extra runtime DB query.

`NexusReinforcementAuthoring.ToSpec(int enemyLevel)` returns null when disabled; otherwise it creates a new immutable `NexusReinforcementSpec`. `CombatStageData` passes its existing `enemyLevel`; the reinforcement level is a captured projection of that sole authored value, not another editable source.

New `C/Contracts/Combat/NexusReinforcementSpec.cs` contains:

```csharp
public sealed class NexusReinforcementEntry
{
    public EntityKey UnitKey { get; }
    public int Count { get; }
    public NexusReinforcementEntry(EntityKey unitKey, int count);
}

public sealed class NexusReinforcementSpec
{
    public const int DefaultFieldEnemyLimit = 5;
    public IReadOnlyList<NexusReinforcementEntry> Pool { get; }
    public int SpawnCountPerTurn { get; }
    public int RangePadding { get; }
    public int EnemyLevel { get; }
    public int FieldEnemyLimit { get; }
    public NexusReinforcementSpec(IReadOnlyList<NexusReinforcementEntry> pool,
        int enemyLevel, int spawnCountPerTurn = 2, int rangePadding = 2, int fieldEnemyLimit = 5);
}
```

Signatures are planning contracts, not implemented code. Defensively copy, reject null/duplicate/invalid rows, sort pool rows by numeric `EntityKey`, and check the total count for integer overflow. Pool indices are stable after preparation. Unit eligibility/roster/profile checks occur at definition/preparation boundaries, before state installation.

Carry the same immutable definition via optional `NexusReinforcements` members on `CombatStageDetail`, `BattleInitialization`, `CombatPreparation`, `CombatStateData`, and `CombatBattleFacts`. Assembly pins it for the active battle. Preserve it in `BattleInitialization.WithAttackProfiles` / `WithTraitGrowth` and all relevant copy constructors. No `StageConfig` enlargement is needed: use `BattleInitialization` for this optional battle definition. Existing callers default to null.

`BattleDefinitionAdapter.Read` verifies roster membership and loads profiles for pool-only candidates even if they are absent from initial placements. `BattlePreparationCalculator` also validates definitions supplied directly by fixtures or non-DB sources. Enabled reinforcement requires `EnemyHeadquartersDestroyed` and exactly one live enemy headquarters; reject a definition attached to a boss objective. Do not disable malformed enabled data silently.

### 5.2 Runtime authority

Add `CombatCommandState.Reinforcements : NexusReinforcementState`, default `Disabled`, and preserve it in `With`, equality, direct/target-typed reconstruction, projections, cleanup, night changes, and completion. The existing `CombatStateBuffer` remains the only authoritative store. Calculators keep temporary dictionaries/lists only for one calculation.

`C/Contracts/Combat/NexusReinforcementState.cs` contains an immutable state and record:

- `NexusReinforcementRecord`: `EntityId EntityId`, `int PoolEntryIndex`.
- `NexusReinforcementState`: `EntityId NexusId`, `uint RandomState`, append-only `IReadOnlyList<NexusReinforcementRecord> Records`, `IsEnabled`, `Disabled`, `SameValues`.
- `RemainingCount(NexusReinforcementSpec spec, int poolEntryIndex)` and `RemainingTotal(NexusReinforcementSpec spec)` derive remaining inventory as authored counts minus creation records.
- `Contains(EntityId entityId)` answers reinforcement provenance, including after death or recall.

Do not store a second remaining-count array, aggregate alive count, reward-exclusion boolean on characters, or copied spawn origin. Origin locations live only in the existing `CombatTurnFacts.EnemyOrigins`. The record identifies provenance and consumed inventory; character facts own type/health/location. Events are historical evidence, not mutable state owners.

Preparation binds `NexusId` to the actual structure entity and starts with no records. Initial random state is `unchecked((uint)TimelineSeed ^ 0x4E585350u)`. Use `EnvironmentRandom.Choose(ref state, count)` for the existing deterministic algorithm, while storing this feature's stream separately from environment, items, and character summons. Seed 0 remains valid. No new RNG service is required.

Successful creation appends one record and one ordinary enemy origin, and advances the shared `Allocation.NextEntityId` once. Keep `Allocation.SummonRandomState` unchanged. Death never removes provenance records. A fresh battle gets fresh state; a new night does not.

## 6. Geometry and valid-cell contract

New `C/Contracts/Combat/NexusReinforcementGeometry.cs` exposes:

```csharp
public static IReadOnlyList<CellCoord> Area(StructureCombatFacts nexus,
    int rangePadding, int boardWidth, int boardHeight);
```

Take the bounding rectangle of the nexus's actual occupied cells, expand each edge by padding, clip to the board, and remove its occupied cells. Return unique cells ordered by Y then X in a read-only collection. Use widened arithmetic before clipping and iterate only board-clipped bounds, including for very large positive padding. For a rectangular footprint of width W and height H away from edges, the count is `(W + 2p) * (H + 2p) - W * H`; W=H=p=2 gives 32. Rotation/anchor interpretation comes from existing occupied cells, not a View approximation.

New `C/Application/Combat/NexusReinforcementCalculator.cs` owns the stricter selection predicate. Use existing spatial facts/classification; do not change general movement or placement rules to make them satisfy spawning.

Exclude:

1. Alive field-character occupancy and alive structure/obstacle occupancy of any owner.
2. Active special terrain and object cells from authoritative environment facts/bindings, including hidden traps. Consumed/inactive sources no longer block by their old location.
3. Actual persistent effect cells: `ThornFields`, active `TraitTrailFact`/destination modifiers, legacy active tornado source cells, the complete footprint of active moving Tornado members, and the actual Haboob cells represented by the existing state/environment. Reuse owner geometry/projections; do not reproduce Tornado/Haboob movement rules. Rail cells are excluded explicitly. Validate every currently supported environment fact kind against this classification during implementation.
4. Orthogonal neighbors of all occupied cells of living player field characters. Never use hand/waiting/dead actors as blockers. Geometry already removes the nexus itself.

The range is independent of blockage. Taunt/Solo Action influence is not an environment occupancy source. Warning markers alone and the possible blast radius of a mine/barrel do not turn surrounding ground into a persistent occupied effect. The actual mine/barrel cell remains excluded.

No `VisibleTo(Player)` filtering belongs in authoritative selection. View receives geometry only, so the public range does not reveal which cells contain hidden blockers.

## 7. Turn integration and atomic generation

### 7.1 Boundary and budget

The only live caller is `CombatTurnCalculator.BeginControllerTurn` for a valid automatic `StartingEnemy` transition. Capture the live field-enemy count at that boundary before spawning. Preserve existing turn-start processing for existing actors, then calculate reinforcement against the resulting world, before the first enemy AI action.

Compute once:

```text
capacity = max(0, fieldEnemyLimit - livingFieldEnemiesAtStart)
batch = min(spawnCountPerTurn, capacity, remainingTotal, validCellCount)
```

Zero means no inventory, ID, RNG, or creation-event change. The ordinary turn start still completes. Existing actors being killed or newly created actors dying during the same boundary do not increase that already fixed allowance. Calls after the controller has become `Enemy` cannot rerun reinforcement. Existing expected-revision publication protects against a duplicate/stale calculation.

### 7.2 Calculator boundary

```csharp
internal static CombatCommandTransition TryCalculate(
    CombatCommandInput startedEnemyTurn, int livingFieldEnemiesAtStart);
```

Input is the projected result of the existing turn-start transition: full characters/profiles/categories, pinned reinforcement definition, state, and environment bindings. Output is null for an ordinary no-spawn opportunity, or one accepted pure transition containing creations, appended origins/provenance, allocation/RNG progress, and ordered events. Invalid configuration/state throws before publication. It owns no Buffer, DB, Unity object, static mutable cache, or callback.

Algorithm:

1. Validate the boundary/source and compute the complete candidate set and fixed batch budget.
2. Work on local copies. For each slot, use `Choose` over the remaining individual count; resolve that roll by cumulative remaining quantities in stable pool order.
3. Use `Choose` over the remaining Y/X-ordered cells, then remove the chosen cell without changing the relative order of others. A singleton draw consumes no RNG, as defined by the existing helper.
4. Allocate the next shared entity ID. Reuse `BattleCharacterCalculator.CreateEnemy` with the pinned stage level and resolved existing unit/category/trait definitions. Create with `UnitActionUse.Deferred`; do not fake an attack, move, or stun.
5. Append its consumption/provenance record and `CombatEnemyOrigin` using the actual first cell/facing. Emit one typed reinforcement-creation event. A batch has no duplicate cells or IDs.
6. Reconcile existing presence effects once on the world containing the complete batch. Existing and newly created actors must both receive their resulting changes. New actors are carried as final `CharacterChanges.Created` facts, not lost by the existing-character-only loop in `CombatTurnCalculator.Changed`.
7. Combine start and spawn transitions using existing `Project`/`Merge`. Publish once through the existing transition publisher. Calculation, queries, and rejected publication cannot consume authoritative inventory or RNG.

Do not publish the intermediate start transition separately, and do not reset new actors to `Available` after creating them. Reconciliation must use the new turn's state, not the old `StartingEnemy` snapshot. Existing enemy-start effects can still run without any spawn.

### 7.3 Store invariants and creation evidence

Extend `CombatStateBuffer.ValidateCreation` to accept two disjoint provenance sources: existing character-summon records and new nexus-reinforcement records. Preserve all existing character-summon validation; a structure is not a character summoner.

- The union of newly appended records matches `CharacterChanges.Created` one-to-one; no overlap, duplicate, unrecorded creation, or record without a creation is accepted.
- Existing records and nexus binding cannot change/disappear. Per-entry consumed counts cannot exceed the pinned definition. Disabled state cannot acquire records.
- New nexus records occur only in the accepted enemy-start transition; their source is the bound live enemy headquarters, their pool indices/types match, and their final action state is `Deferred` even if immediately dead.
- Each reinforcement has exactly one newly appended ordinary origin matching its creation event. Old origins remain unchanged, and existing character summons remain disjoint from origins.
- Shared entity allocation remains contiguous. A no-creation result cannot change reinforcement RNG/provenance. Non-reinforcement creation does not consume this stream.
- Structural validation is not a second spawn algorithm. Probability and tile selection remain in the calculator; store checks identity, conservation, boundary, and consistency.

Add `CombatEventKind.Reinforced` as the next unused stable enum number (48 at inspection time; recheck before implementation), with `CombatReinforcementEvent` in `C/Contracts/Combat/NexusReinforcementEvent.cs`. Fields: `EntityId NexusId`, `NexusReinforcementRecord Record`, `CharacterCombatFacts Created`. `Created` is the initial deferred fact before presence changes. Events occur after controller-start events and before their dependent effect events. Extend `CombatChangeEvidenceReader` explicitly. Keep existing enum values intact.

If the calculation is composed with subsequent effects/death, preserve exactly one final created fact and the initial creation event, following `CombatCreationCompositionTests`. Use the existing settlement path for effects that require death/outcome processing; do not add a second damage engine.

## 8. Rewards and night lifecycle

### 8.1 Rewards

Extend `ConfirmedKillCalculator.Collect` to receive the effective `CombatCommandState sourceState` used by `CombatCommandCalculator.Finish`. This is important when creation and damage are composed in the same command: provenance may already exist in projected state but not in the original command input.

After the existing death-evidence, attribution, profile, and eligibility rules identify a credited kill, use `sourceState.Reinforcements.Contains(victim.EntityId)` to set both reward amounts to zero. Keep the `CreditedKill` entry. Existing `CombatStageRecordSettlement`, reward killed-count summaries, and kill-triggered mechanics retain their current semantics; no XP-earned event is produced for amount zero. Do not mutate a shared `UnitSpec.KillReward`, invent a second unit definition, or infer provenance from a unit key.

Direct attack, reflected damage, environmental damage, and other existing death routes all pass the same suppression boundary. If a route has no eligible killer, its existing attribution behavior remains unchanged. Stage-clear rewards and participant experience are unaffected.

### 8.2 Night recall and redeployment

Register the first spawn origin at creation. Then `RecallForNight` and the ordinary branch of `RedeployEnemies` already provide the agreed behavior: same entity, same current health/other surviving state under existing cleanup rules, original position if available, existing nearest-cell fallback otherwise. Reuse the ordinary path; do not put nexus reinforcements in `Turn.Summons`.

Redeployment is not subject to fresh-spawn pool deductions, the fresh-spawn normal-tile filter, the nexus radius, or the friendly-cross exclusion. It follows existing deployment rules and emits `Placed`, not `Reinforced`. Its ordinary blocked-redeployment failure semantics remain unchanged. No new replenishment or despawn mechanic is introduced.

## 9. Read-only View handoff

Add to existing `CombatStateData`:

```csharp
public IReadOnlyList<CellCoord> ReadNexusReinforcementArea();
```

It uses the pinned definition, bound nexus, current occupied cells, and `NexusReinforcementGeometry.Area`. Return empty for disabled/unprepared source, destroyed nexus, or finished battle. A configured live nexus exposes the range during deployment and combat, including when its pool is exhausted or the field cap is reached: it is a geometric range, not a promise to spawn now.

The same snapshot can derive remaining supply from its pinned definition and `CommandState.Reinforcements`; do not add a separate mutable View inventory. This phase does not introduce an exact available-cell preview, a new request command, service registration, or View-to-Core write. Existing Buffer snapshot/revision lifetime applies; View refreshes after published results/battle changes, never retains geometry as a second authority.

The handoff must explain `CombatReinforcementEvent` ordering and initial versus final created facts, `Deferred` action semantics, and next-night `Placed`. View workers must register the new event in their event-type/handler system and implement spawn presentation/range display separately. A final-state refresh alone is not proof that ordered playback and subsequent effect targets work. No View tests, View code, MSBuild, or PlayMode claim belongs to this Core documentation task.

## 10. Locality, SOLID, SSOT, and Framework review

| Real change scenario | Current burden if inserted into existing unrelated policy | Minimum boundary and expected change | Understanding still required | Required verification |
|---|---|---|---|---|
| Change nexus radius or batch amount | Initial spawn uses whole-board deployment and a different RNG lifecycle. Folding runtime spawning into it would mix those policies. | Edit `NexusReinforcementAuthoring` values; geometric formula remains in one pure contract. Normal tuning needs no turn/AI/initial-placement edit. | Pool definition and geometry semantics. | Authored validation, range examples; initial placement regression when shared definition plumbing changes. |
| Change what ground qualifies for nexus spawning | Reusing/modifying `BlocksVoluntaryEntry` would change movement, initial placement, and redeployment. | Change the reinforcement predicate only, reading existing spatial/environment contracts. | Current environment categories and actual effect footprints, not movement/attack implementations. | Spawn filtering plus affected source-projection integration; general movement tests remain required when shared code actually changes. |
| Add finite nexus creation and suppress its rewards | Store validation assumes character summoners, and credited kills feed both rewards and statistics. | Add typed nexus provenance to existing creation validation; suppress amounts at the shared reward boundary. | Atomic `Created`/allocation/event contract, provenance lifetime, reward attribution. | Publication rejection/rollback, composition, same-type initial rewards, statistics, night recall and next-turn eligibility. |

Necessary coupling remains: turn timing, ID allocation, effect reconciliation, publication, reward attribution, and redeployment are real gameplay/integrity contracts. Their integration tests cannot be removed in the name of locality. Initial/runtime draw-policy unification, additional framework services, and state copies are avoidable coupling and are not introduced.

- **SRP:** authoring supplies immutable input; the calculator owns reinforcement rules; lifecycle owns timing; Buffer owns state installation; View owns presentation.
- **OCP:** add the feature calculation and a typed provenance/event extension at existing boundaries. Do not rewrite unrelated rules or promise zero assembly edits.
- **LSP:** preserve existing summon, rejected-transition, state-copy, and read-only snapshot guarantees. Optional absence must preserve old behavior.
- **ISP:** the range reader returns cells, not a DB/query bundle. The calculator takes existing combat facts and one captured count; it does not expose a generic spawn service.
- **DIP / FW:** pure Contracts/Application stay Unity- and Buffer-independent; adapters provide definitions. No new service/module, export, job, event bus, or singleton is needed. Existing registration/lifecycle contracts remain unchanged.
- **SSOT:** authored counts are immutable definitions; consumption is the append-only record; remaining inventory is derived; origins stay in `EnemyOrigins`; entity allocation remains shared; geometry and the valid-cell predicate each have one owner.

Framework review uses `Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md` and `framework-architecture.md`. FW-001/002/003 require no new registration or Builder lifecycle here; FW-012 introduces no new table/registry entry because authoring extends an existing stage item; FW-016 keeps the existing initialized service/runtime path. The finite gameplay pool is not the Framework resource-pool API, and recorded combat events are not EventBuse publications; do not infer new FW-013/014 integration from those names. Buffer mutation safety follows the actual existing prepare/install contract, without overstating FW-004 as ownership of all game data.

Expected locality improvements are design predictions. The companion plan records source and integration evidence as each phase is implemented.

## 11. Acceptance and verification

Use test-owned definitions and fixtures for Core and CoreIntegration regression coverage. New `.cs` files need `.meta` files. Follow the repository's test ownership and approval rule when an existing test fails at runtime.

| Area | Required evidence |
|---|---|
| Definitions | Disabled compatibility, empty pool, stage enemy-level capture, pool-only profiles, wrong objective/source, invalid count/key/roster/role/footprint, configured field limit, initial count above the limit accepted, overflow and defensive copies. |
| Geometry | Default 32 cells, padding 1 gives 12 around 2x2, clipping, rotation/actual footprint, no duplicates, deterministic order, bounded large-padding calculation. |
| Candidates | All occupancy categories, inactive versus active objects, hidden traps, every actual effect footprint, rail/warning distinction, all friendly orthogonal neighbors, allowed diagonals, off-field actors. |
| Draw | Counts 0/3/4/5/>5, changed batch size, empty/partial pool, 0/1/few cells, without-replacement quantity behavior, reproducible results, independent streams, no RNG/ID consumption on no-spawn. |
| Turn/effects | One spawn pass per enemy start, none on player start/terminal state, existing actors available and new ones deferred, immediate Taunt/Solo Action reconciliation including changes to existing actors, next enemy-turn action, fixed budget despite immediate death. |
| Store/composition | Atomic success, stale/rejected publication leaves all stores unchanged, record/creation/origin/event conservation, invalid provenance rejected, create-then-effect/death produces one final created fact, state survives all copies. |
| Rewards/lifetime | Same type initial versus reinforcement rewards, direct and indirect kills, preserved credited kill count, zero earned XP event, unchanged clear rewards, night recall/blocked-origin fallback, no pool reset, fresh battle reset. |
| Consumer contract | Buffer range equals calculator geometry without hidden blocker leakage; event evidence accepts the new kind; View handoff documents unimplemented playback and display. |

Unity version is 6000.0.73f1. Execute future Unity runs through the approved path with AppData access; the known sandbox `CurlRequestCache.db` failure is not a reason to upgrade Unity or delete caches. Record actual runner/XML evidence and any unrelated failures. No Unity, compiler, EditMode, or PlayMode run was performed to author this specification.

## 12. Delivery boundary

The companion plan implements this single feature in reviewed phases. Production pool quantities and stage activation remain designer content work; no invented production values are part of the plan. Core completion means implemented and verified Core contracts plus a written handoff, not a claim of View rendering/playback completion.

Before each implementation phase, recheck current source drift and working-tree ownership. If a change would alter a confirmed gameplay rule, public contract outside the specified extensions, or ownership boundary, stop that change and ask the user. Routine local implementation choices that preserve this specification do not require another permission round.

## 13. Documentation review record

On 2026-11-05 an independent builder check reviewed this specification against the source and reported no important unanswered planning questions. The author checked requirement coverage, contract names, phase boundaries, source references, and local links before implementation.
