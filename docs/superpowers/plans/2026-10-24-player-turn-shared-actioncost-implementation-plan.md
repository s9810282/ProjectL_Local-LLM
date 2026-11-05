# Player Turns and Shared ActionCost Implementation Plan

> **For agentic workers:** When implementation is explicitly authorized, use `superpowers:executing-plans` or the user-selected `superpowers:subagent-driven-development` workflow. Track the checkboxes below. Complete only the authorized Phase and stop for review. This document does not authorize implementation, commits, pushes, or work in another owner's area.

**Goal:** Replace unit initiative and per-unit AP with player AP 10, one ordinary action per unit per own-side turn, simple unbudgeted enemies, effectful L-shaped movement, and day after the enemy response to player turn 8.

**Architecture:** Preserve LifeCycle `CombatPlan` and existing Jobs, role-specific Core services, pure Core calculations, and atomic Buffer installation. Combat Buffer owns the player budget/controller lifecycle; Character Buffer owns each unit's action use. Feature rules remain with their existing owners; View consumes facts and submits intent.

**Tech Stack:** Unity 6000.0.73f1, C#, existing Core Contracts/Domain/Application and Systems assemblies, NUnit EditMode; no new package, Framework scheduler, event bus, or transaction framework.

**Spec:** [Player Turns and Shared ActionCost — Migration Design](../specs/2026-10-23-player-turn-shared-actioncost-design.md), including decisions D1–D7 and §4.3.1. Source baseline inspected: `d73fb5c6192cb2ea7e0bb9bd15b0b9509f84658c`. Recheck the checkout before execution.

## Global constraints

- Player shared maximum is **10**; reset only at player-turn start. Initial scope has no progression or stage-specific maximum override.
- Each unit can move **once OR attack once** per own-side turn. Eligibility is per unit, AP is shared. New units wait for the next own-side start.
- Player first, then enemy; enemies have **no AP budget** and choose a legal current attack, otherwise movement. No move-then-attack and no hidden unit initiative.
- Player end is manual, including early end. Zero AP/no legal action never automatically ends the player turn in this scope.
- Count completed **player turns**. Day follows enemy-phase completion after player turn **8** of the current night. Do not increment the player counter again at enemy end.
- Player movement costs **2 per action**. Allowances: Main/Heavy **3**, Rider **4**, Cannon **2**. Attack price: Cannon **5**, other playable categories **3**. Boss movement remains undefined and disabled where existing behavior disables it.
- A diagonal is two orthogonal steps along one legal L route. At least one route must work. Its chosen middle cell is actually entered and applies ordinary effects. Commit follows the exact previewed route.
- Delete checkpoint return and unit AP/timeline authority. Preserve deployment return, actual effect/animation ordering, battle RNG inputs, and Framework Job ordering.
- Preserve original Buffer ownership: calculators propose; Buffer validates expected sources, prepares, installs, publishes and owns revisions. Do not create dual budgets or private authoritative controller state.
- Keep items/traits/gimmick definitions when deferring their runtime capability. Missing configuration remains an error; intentionally unavailable content has an explicit reason and cannot silently consume an item or arm an expired timer.
- **Systems-only scope, confirmed 2026-10-24:** traits, items and gimmicks will undergo a separate planner-approved overhaul, including deletion or replacement. B migrates turn/action/state contracts and disconnects incompatible capabilities; it does not redesign legacy effects, balance, durations or triggers. No compatibility layer, generic content scheduler or promise to revive every feature is required. Pending content decisions do not block B/C completion; required system and integration verification remains in scope. Actual content-asset deletion needs a concrete authorized list.
- Documents are English. New/edited code comments and XML documentation are Korean. Follow namespace, Allman, UTF-8/LF and `.meta` conventions; preserve GUIDs when moving files.
- No commit/push. Preserve concurrent work and protected legacy Missing Script assets. Framework implementation, FieldCamera and Architecture are out of scope.
- Core implementation requires explicit authorization for the Core Phase. **View implementation is outside this plan executor's scope.** Do not edit ViewSystem, UI/HUD, input bindings, presentation code, scenes or prefabs, including compile-only adaptations. Describe required changes for the View owner; do not assign those fixes to the user. A future change of this boundary requires a new explicit instruction.
- `B/CombatView` command/preview payloads are shared Buffer contracts, not View implementation. They may change with authorized Core/Buffer contracts, but every affected View producer/consumer must be identified in the handoff. Existing LifeCycle Plan/Job production behavior is preserved; changes required in that owner's implementation are also handed off.
- Apply the user-supplied `feature-change-locality-guidelines.md` as review criteria, not as code-change authorization. Evaluate modification, understanding and verification scope using real paths/contracts; necessary integration checks remain necessary.

## Review focus

1. **Stale end-turn input:** an old queued EndPlayerTurn must not end the next player turn after an enemy response. Covered by B1/B4/B8.
2. **Budgetless enemy admission:** an enemy with a legal zero-damage target attacks even if player AP is zero; neither an old AP check nor positive-damage scoring may force a move. Covered by B3/B4.
3. **Chosen diagonal middle cell changes:** commit rejects a stale route instead of silently using the other corner; hidden effects still resolve authoritatively. Covered by B2/B8.
4. **Enemy response 8 is empty or terminal:** empty phases still permit day; terminal actions stop without day/refill/duplicate rewards. Covered by B4/B8/C1.
5. **Deferred producer with a retained consumer:** unavailable Rope/traits/world producers must not consume stock, write dormant counters, or appear active in UI. Night-scoped items still expire after response 8. Covered by B5/B6/B7/B8.

---

## 1. Phase boundaries and ownership

| Phase | Deliverable | Entry requirement | Stop condition |
|---|---|---|---|
| A — Pure rules and contracts | Independently tested immutable rule/value additions; current runtime behavior remains unchanged | Explicit Phase A authorization; source/working-tree baseline recorded | New pure tests pass and no new live state/service is installed. Stop for review. This is not migration completion. |
| B — Core/Buffer cutover and consumer handoff | Authorized Core, Buffer and rule-data changes plus a concrete View-owner handoff; obsolete Core authority removed | Explicit Phase B authorization; Phase A accepted; isolate breaking changes and preserve concurrent work | Report Core source delivery and available verification separately from View integration. If View prevents compilation, stop with exact errors and tests pending; do not mark the migration verified/operational. |
| C — External integration acceptance | After the View owner's independent delivery, verify combined compilation, affected integration checks and actual Boot/Combat flow | Core delivery accepted; View owner has supplied the necessary adaptation; current content and acceptance authorization available | Record integrated test/manual results and remaining failures. This Phase authorizes acceptance, not implementing View fixes. |

**Source delivery versus integrated readiness:** removing `ActiveActorId`, unit AP and return/timeline contracts necessarily affects both Core and View, but that dependency does not transfer View implementation to this worker or to the user. Core source and the B7 handoff can be delivered before the View owner adapts consumers. Only the combined version is considered an operational migration. Before breaking contracts in B, use an isolated checkout containing the accepted A work; do not leave the shared working project intentionally uncompilable. Follow the app's worktree workflow and do not assume it copies uncommitted changes.

Do not create fake unit AP properties, no-op return APIs, fabricated timeline output or a second authority merely to keep old View compiling. If unmodified View consumers cause compiler errors, report the exact file/symbol/replacement and keep integration pending. Unity Test Runner filters do not bypass project compilation errors; blocked tests are **not run**, not passed. Do not modify View tests, remove assemblies, suppress errors or substitute a new standalone harness to hide that limitation.

**Responsibility limits:** Plan/Jobs coordinate accepted transitions and presentation readiness. They do not select category costs, route corners, AI targets or status expiry. Buffer validates consistency/source identity, not those game rules. Feature-specific deactivation belongs at that feature's calculation/admission seam; no central controller switch that implements every trait. Normal Factory/Group/ServiceKey changes for removed/added services are legitimate composition work and are listed explicitly.

## 2. File map and contract vocabulary

Use the exact directory aliases in [design §2.1](../specs/2026-10-23-player-turn-shared-actioncost-design.md#21-source-notation-and-authority): `C/`, `CS/`, `B/`, `L/`, `V/`, `U/`, `D/`, `A/`, `S/`, `T/`. All new C# files require matching `.meta` files. Brace lists below enumerate named sibling files, not permission to refactor the entire directory.

### 2.1 Small contracts/rules to add or reuse (Phase A)

| Create or reuse | Responsibility / exact planned surface |
|---|---|
| `C/Contracts/Common/PlayerTurnRules.cs` | Pure constants `MaximumActionCost = 10`, `PlayerTurnsPerNight = 8`; sole initial policy source. No configurable service or SO. |
| `C/Contracts/Combat/PlayerActionBudget.cs` | Immutable `Current`, derived `Maximum`; constructor `PlayerActionBudget(int current)` validates 0..10. `PlayerActionBudget Spend(int amount)` rejects negative/insufficient spending and returns a value; `Full` and `Empty` construct rule-based values. No store or reset callback. |
| `C/Contracts/Combat/UnitActionUse.cs` | Enum `Deferred`, `Available`, `Moved`, `Attacked`. An unavailable unit still has independent life/location/behavior checks. No per-unit resource count. |
| `C/Contracts/Combat/CombatControllerPhase.cs` | Enum `None`, `StartingPlayer`, `Player`, `StartingEnemy`, `Enemy`. Encodes controller plus start boundary without a unit appointment list. |
| `C/Contracts/Combat/ActionFundingKind.cs` | Enum `PlayerBudget`, `Unbudgeted`. Selected by trusted processing/ownership, never by client preference. |
| Reuse `C/Domain/Categories/CategoryActionRules.cs` | Observed concurrent addition: `internal static (int? MovementAllowance, int MoveCost, int AttackCost) Get(CategoryKey category)`. It already expresses the confirmed table, Boss allowance null and invalid-category rejection. Preserve that small surface; do not add three forwarding methods or a duplicate rule. Application exposes derived values to Systems; no new Systems-to-Domain reference. |

Do not add a System/interface/Factory for these value types or static rules. Keep the existing `ActionCostSettlementSnapshot` as the result settlement contract, adapting its funding scope in B rather than creating a second settlement subsystem.

### 2.2 Fields and APIs changed atomically in Phase B

| Existing contract / file | Required final surface |
|---|---|
| `C/Contracts/Combat/CombatTurnFacts.cs` | Retain preparation/placement lock/enemy origin/summon facts. Replace participant/time/unit-count authority with `CombatControllerPhase ControllerPhase`, `long ControllerTurnSequence`, `long CompletedPlayerTurnCount`, `long NightStartPlayerTurnCount`. Derive `Controller` and `CompletedPlayerTurnsThisNight`; check count range 0..8 in active combat. No independently mutable night countdown. |
| `C/Contracts/Combat/CombatCommandState.cs` | Add `PlayerActionBudget PlayerBudget`. Remove `ActiveActorId` and `Timeline`. ControllerPhase is authoritative for side admission; retain existing FlowPhase/StagePhase consumers with validated mappings, including terminal override. Do not leave separately writable controller mirrors. |
| `C/Contracts/Combat/CharacterCombatFacts.cs`, `B/Character/CharacterData.cs` | Add `UnitActionUse ActionUse`; preserve it in `With`, `WithPlacement`, `SameValues`, source validation and publication. Remove checkpoint/pending/refund fields and unit current/max AP from nested stats/contracts. |
| `C/Contracts/Combat/CombatCommandInput.cs` (also declares `CombatCommandKind`) | `EndPlayerTurn` replaces `EndUnitTurn`; delete `ReturnToCheckpoint`. Ordinary commands carry expected battle ID and controller-turn sequence separately from freshly captured state. Automatic start/end decisions are explicit intents, not an EndUnitTurn with an invented actor. |
| `B/CombatView/CombatViewCommand{,Kind}.cs`, `CS/CombatActionRequest.cs` | `EndPlayerTurn(long expectedBattleInstanceId, long expectedControllerTurnSequence)` is actor-free. Move/attack carry the same expected identity pair. Existing placement/equipment commands keep their own lifecycle semantics. |
| `C/Contracts/Combat/CombatCommandTransition.cs`, `CombatResult.cs`; `C/Contracts/Snapshots/ActionCostSettlementSnapshot.cs` | Result carries controller-turn identity, funding kind, applied spend, applicable player before/after funds and actor action-use change. Enemy ordinary action consumes eligibility and spends 0; a reaction does not consume another ordinary action. Rejected actions preserve gameplay facts. |
| `C/Contracts/Combat/MovementPreviewContext.cs`; `C/Contracts/Results/{MovePreview,MovementPathResult,MovementDestination}.cs` | Exact normalized orthogonal path, shared funds, actor eligibility and controller-turn identity; no return checkpoint/refund quote. Preserve actual entered cells/stop reason/causal events. Path length and player action price stay separate. |
| `C/Contracts/Results/{MovementFailureReason,AttackFailureReason}.cs` | Add/map relevant `WrongController`, `ActionAlreadyUsed`, `ActionDeferred`, shared insufficiency and stale-turn failures consistently. Existing string-to-enum preview conversion must accept every emitted attack failure. |

**Small query addition:** create `C/Contracts/Combat/CombatActionAvailability.cs` with actor/turn identity, action use, applicable player budget, move allowance/prices and typed move/attack failure reasons. `CanMove`/`CanAttack` derive from those reasons. Add `CombatCommandCalculator.GetActionAvailability(CombatCommandInput input)` and an `ActionAvailability` preview kind/result through existing `CombatPreviewService`. Do not add a separate public service unless a demonstrated synchronous consumer requires it. Inspection-only `UnitAttackRange` remains geometry, not permission to act.

**Lifetime encoding:** current Phase/StagePhase mappings must be explicit: deployment accepts placement; StartingPlayer/StartingEnemy require automatic start processing; Player accepts player commands; Enemy requires automatic enemy decisions; Finished overrides all admission. Increment controller sequence only at a real side start. Day clears active controller; next EnterCombat starts player first. This uses existing state validation and does not create a parallel Core Plan/Job layer.

## 3. Phase A — Pure rule preparation

### A0. Record the execution baseline

**Files:** read-only repository state, current design, AGENTS, Framework references; future evidence under `.superpowers/sdd/2026-10-24-player-turn-shared-actioncost/`.

- [x] Record HEAD/branch/worktree and `git status --short`; preserve concurrent changes and identify any attached active worktree before creating another. See §8.1.
- [x] Compare the live caller/type paths with §2. Record changed integration assumptions before editing; do not treat the old design's inspection SHA as current evidence. No new live consumer was introduced in A.
- [x] Identify the View-owner handoff route and affected consumer contract list. B7 keeps the migration table with this plan or the existing project handoff; the recipient is the View worker. Existing timeline, end-unit-turn and checkpoint-return consumers were confirmed read-only. No View edit or external message was performed.
- [x] Capture only the existing affected baseline fixtures needed for later comparison using §7. Reused the same-checkout Core 3680/3680 result; Framework/View baseline execution is excluded by the user's current instruction, not claimed as passing.

### A1. Add and verify immutable rules

**Files:** create the five new Contracts files in §2.1 and `T/PlayerTurnRuleTests.cs` with metas; reuse the concurrent `CategoryActionRules.cs` and `T/CategoryActionRulesTests.cs` after owner/status verification. Existing production behavior is not modified.

**Consumes:** existing `CategoryKey`, `Owner` and Core assembly boundaries. **Produces:** §2.1 rules/value contracts for B; no runtime registration or state installation.

- [x] Add pure cases `MaximumIsTen`, `NightLengthIsEight`, `BudgetBoundsAndSpend`. Assertions include `10 - 2 == 8`, exact `10 - 5 - 5 == 0`, and insufficient/negative spend rejection. Reused `CategoryActionRulesTests` without duplicating its category table cases.
- [x] Implement the small immutable Contracts rules and enum values. Independently reviewed and reused the earlier category rule; invalid categories reject without substituting Main and Boss movement remains unspecified.
- [x] Run `ProjectL.Tests.Core.PlayerTurnRuleTests;ProjectL.Tests.Core.CategoryActionRulesTests` through §7. Both fixtures ran inside the Core-only assembly run: 20/20 and 8/8 passed; input preservation is covered. See §8.1.
- [x] Audit that these additions have no live Buffer field, service registration, second AP writer or altered current gameplay. Evidence is recorded in §8.1. **Phase A stopped for review; B/C not started.**

## 4. Phase B — Core/Buffer cutover and View-owner handoff

**Execution rule:** B1–B6 implement authorized Core/Buffer/data changes, B7 prepares the external consumer handoff, and B8 audits delivery and verification limits. No B task authorizes View implementation. Temporary dependencies inside Core must be resolved by Core; errors caused by unchanged external consumers are inventoried for their owner. Add meaningful acceptance cases as the contract is introduced, but do not claim execution while project compilation is blocked. A source delivery is not a shipped/operational migration.

### B1. Install new facts through the existing transaction

**Modify:** §2.2 contracts; `B/Character/{CharacterBuffer,CharacterData}.cs`; `B/Combat/{CombatStateBuffer,CombatStateData,BattlePreparationValidator}.cs`; `B/Core/BattleTransactions.cs`; `C/Contracts/Combat/{BattlePreparation,CombatBattleFacts}.cs`; `C/Application/Combat/{BattlePreparationCalculator,BattleCharacterCalculator,CombatCommandComposition,CharacterSettlementCalculator}.cs`; `CS/{CombatInputAssembler,CombatActionRequest,CombatCommandService,CombatTransitionPublisher,BattleEntryService}.cs`.

**Test:** create `T/SharedActionBudgetTests.cs`, `T/Framework/PlayerTurnPublicationTests.cs`; adapt existing `CharacterActionCostPublicationTests`, `CombatCommandServiceTests`, contract-copy and membership fixtures.

**Consumes:** A1; existing prepared character/command publication. **Produces:** one player budget and per-unit action state in the existing transaction; battle/turn-scoped request identity.

- [ ] Add acceptance assertions: A moves from player budget 10 to 8 and marks only A Moved; B remains Available; stale state/turn/battle and invalid budget/eligibility evidence fail before any store installation.
- [ ] Apply §2.2 fields to immutable copies/constructors and equality. Extend existing Prepare methods to validate budget settlement agreement and eligibility/source identity, not calculate costs or decide whose turn it is.
- [ ] Preserve prepare-all-before-install order; result/handled number publish last. A repeated handled number must not spend twice. A second request number for an already-used actor must yield a normal rejected result.
- [ ] Preserve action use and player funds through health changes, level-up, death cleanup, persistent-main updates and result composition. Remove all unit-AP refill/clamp code, including `CharacterSettlementCalculator.EndTurn` where obsolete; do not refill through stat copying.
- [ ] Ensure rejected gameplay commands advance their handled number without changing world/funds/eligibility. Stale prepared transitions remain invariant failures; no partial publication or reward duplication.

### B2. Make movement standalone and normalize L routes

**Modify:** `C/Application/Combat/{CombatMovementCalculator,MovementProjection,MovementContextCalculator,MovementEnvironmentCalculator,CombatCommandCalculator,CombatPlacementCalculator,AttackKnockbackCalculator,BossRoarCalculator,CommandDeathSettlement,FlagDropCalculator}.cs`; `C/Application/Artifact/SoulEffectCalculator.cs`; `C/Domain/Movement/{MovementRouteValidator,EnvironmentTerrainRules}.cs`; affected movement contracts in §2.2.

**Create:** `C/Application/Combat/MovementPathNormalizer.cs` with `MovementFailureReason TryNormalize(CombatCommandInput input, MovementPath draft, Owner observer, out MovementPath normalized)`. This is a pure calculation helper, not a System. It returns failure/null for no valid complete route and does not execute hidden effects.

**Delete when callers migrate:** `C/Application/Combat/CheckpointReturnCalculator.cs`, `C/Contracts/Results/CheckpointReturnPreview.cs`, `CheckpointReturnResult.cs` and metas.

**Test:** create `T/PlayerTurnMovementTests.cs`; adapt `MovementRouteTests`, `MovementEnvironmentTests`, `HighGroundMovementTests`, `WallMovementTests`, `Framework/MovementPublicationTests`.

**Consumes:** B1 budget/action-use facts and current world geometry. **Produces:** a committed normalized route, fixed player spend 2, used eligibility and ordinary entry/arrival events in one transition.

- [ ] Add cases: `OneLegalLCornerSucceeds`, `BothLCornersBlockedReject`, `PreviewedMiddleCellIsNotSwapped`, `MiddleMineStopsBeforeDiagonalEndpoint`, `UnchosenMiddleHasNoEffects`, `DetourUsesActualSteps`, `MoveCompletesWithoutAttack`, `ZeroPathRejectsWithoutSpend`.
- [ ] Normalize the full draft using both legal L candidates where needed; prefer the explicit draft choice, otherwise deterministic X-then-Y. Search an alternate candidate if a later full-route constraint invalidates the first; no exponential generic path framework is needed for the current bounded allowances. Publish the exact expanded path for commit.
- [ ] Validate the normalized route with existing orthogonal geometry, occupancy, repeated-cell and allowance checks. Range/AI can reuse orthogonal search because an L diagonal has identical traversed cells and length 2. Do not substitute displacement for path length.
- [ ] Separate per-step reach/entry events from AP. Charge player 2 once for an accepted movement and consume eligibility once. A hazard interruption keeps the actual stop and fixed charge; enemy movement has no charge. Apply middle-cell entry effects and existing arrival-only rules exactly once.
- [ ] Remove high-ground AP surcharge and all pending/checkpoint/refund ledgers and seal calls. Replace death/displacement fallback positions with explicit original/last-entered facts already carried by projections/events; never fabricate a checkpoint.
- [ ] Update movement context to the normalized route, player funding and per-unit eligibility; preserve observer-visible comparisons and hidden-mine guarantees. A changed previewed corner rejects rather than selecting a new route silently.

### B3. Migrate attack admission and public availability

**Modify:** `C/Application/Combat/{CombatAttackQueries,CombatAttackCalculator,CombatCommandCalculator,CombatCommandEffects}.cs`; `CS/CombatPreviewService.cs`; `B/CombatView/CombatViewPreview{,Kind}.cs`; `C/Contracts/Combat/CombatPreviewResult.cs`; attack/settlement/result/failure contracts in §2.2. Create the availability contract named in §2.2.

**Test:** create `T/PlayerTurnAttackTests.cs`; adapt `CharacterActionCostLifecycleTests`, `Framework/CombatPreviewServiceTests`, affected attack targeting/category/delivery tests.

**Consumes:** B1 facts, category rules and existing target/damage execution. **Produces:** ownership/unused-action/funding-aware ordinary attacks and a read-only availability result.

- [ ] Add cases: two different Cannon units spend 5+5 from 10; budget 4 rejects Cannon; A attack followed by A attack/move rejects; B remains Available; legal zero damage spends/consumes once; invalid/missing mandatory target does neither.
- [ ] Replace `ActiveActorId` and unit AP admission with current controller, actor identity and `ActionUse`. Compute category prices in Core and use them consistently in range, preview and commit. Trusted enemy execution bypasses funding only, not action/target/capability validation.
- [ ] Remove attack-confirmed movement and Rider AP refunds. Reuse `AttackDeliveryExecutor`, damage/knockback, contact, item and death settlement. One initiating ordinary attack consumes one action regardless of its internal hits/chains; reactions do not create an extra ordinary command.
- [ ] Add `GetActionAvailability` and the existing preview-service route. Keep hover-only attack geometry usable for inspection without implying that a unit can act. Update all string/enum rejection mappings and typed tests.

### B4. Replace controller lifecycle and enemy behavior; restore day after response 8

**Modify:** `C/Application/Combat/{CombatTurnCalculator,CombatCommandCalculator,CombatPlacementCalculator}.cs`; `C/Application/Combat/EnemyAi/{EnemyAiTurnService,EnemyAiPolicy,EnemyMovementPathfinder,BossTurnCalculator}.cs`; `CS/CombatAdvanceService.cs`; `C/Contracts/Combat/CombatEvent.cs`; `C/Application/Combat/Summoning/CombatSummonCalculator.cs` only for eligibility/contract adaptation while activation stays disabled.

**Interfaces:** keep `CombatCommandCalculator.Calculate`/`Advance` entrypoints. Replace unit methods with `CombatTurnCalculator.EndPlayerTurn(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)`, `BeginControllerTurn(CombatCommandInput input)`, and `EndEnemyPhase(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)`. Existing `EnterCombat` remains. Each returns one immutable `CombatCommandTransition`; no stored Core loop state.

**Test:** create `T/EnemyControllerPhaseTests.cs`, `T/EightTurnNightTests.cs`; adapt `StageEnemyAiTurnTests`, `EnemyMovementPathTests`, `BossNightLifecycleTests` and `Framework/CombatAdvanceServiceTests`.

- [ ] Add cases for initial player AP10, owner-only reset, created-unit deferral, no auto player end, empty enemy completion, eighth response before day, two nights preserving cumulative count, terminal action suppressing day/start, and repeated end requests.
- [ ] Implement the explicit state transitions: deployment → StartingPlayer → Player; manual end increments player count → StartingEnemy → Enemy; no remaining enemy action → StartingPlayer when count <8, otherwise day/deployment. Controller sequence advances only on actual side starts.
- [ ] Start-player sets AP10; start-enemy creates no budget. Reset only eligible living field units of that side. Selection and individual action completion cannot enter a start transition. Newly created units are Deferred until the next actual owner start.
- [ ] Choose eligible enemies in stable EntityId order. If there is a legal current target, attack; otherwise attempt a legal pursuit move. A zero-damage legal target is still a target. Reuse hostility/geometry and useful tie logic; bypass old affordability/positive-damage-as-admission gates. No global tactical optimizer, precomputed initiative list, move-then-attack or AP refill.
- [ ] If no unit can act, return an accepted enemy-phase completion transition, satisfying the existing Advance Job postcondition. Do not loop rejected Advances. Actor death keeps the controller turn alive if combat continues.
- [ ] At enemy completion with night-local player count 8, process enabled end work, night expiry, existing item/supplies cleanup and survivor recall, increment PreparationIndex once and publish NightEnded/day. Next EnterCombat captures a new baseline. Terminal outcome suppresses all later phase/day/refill work.

### B5. Disconnect unsupported content; retain required system cleanup

**Modify only relevant seams:** `C/Application/Combat/CombatCommandEffects.cs`; `C/Application/Combat/Traits/TraitEffectProvider.cs`; `C/Application/Combat/Categories/{CategorySnapshotBuilder,CategoryRuleFactories}.cs`; `C/Application/Artifact/{ArtifactEffectCompiler,SoulEffectCalculator}.cs`; `C/Application/Combat/Items/{ItemEquipmentCalculator,ItemRuleRegistry,ItemLifetimeCalculator}.cs`; `C/Application/Combat/{CombatDebuffApplication,CombatEffectiveStatsCalculator,HeldEnvironmentEffects,ThornFieldApplicationCalculator,CombatGimmickCalculator,TornadoCalculator,WeakGroundCalculator}.cs`; existing feature rule files only where the producer cannot be gated at these boundaries.

**Test:** create `T/DeferredCombatFeatureTests.cs`; adapt `CombatItemTimelineTests`, `RegenerationTurnTests`, `WeakGroundTurnTests`, `TornadoEnvironmentTests` and relevant trait/item fixtures.

**Produces:** explicit availability and no hidden active unit-turn timers. This is system migration work, not content redesign or a configurable feature-flag platform. The retained effects below are a temporary regression baseline, not a guarantee that the planner will keep them. If preserving a mixed feature requires a new gameplay rule or substantial compatibility work, disconnect that capability and record the unresolved content decision instead.

- [ ] Preserve ordinary targeting/damage, Heavy protection, Cannon targeting, hit/kill chains, reflection, forced movement, badge and instant item behavior. Preserve Intimidation attack reduction/attack-completion expiry and Frenzy attack waves; disable only their priority/scheduling components, with accurate published capability information.
- [ ] Disable runtime Rider refund, MoveAttackDiscount, Focus, periodic Regeneration, timed thorn-field production/debuff, timed soul-drop production, railway/tornado activation, weak-ground entry/collapse behavior, priority-only flag pickup and necromancy activation. Leave definitions/identities and unaffected pure calculations intact; this does not require preserving obsolete runtime compatibility. Do not leave producers writing state whose consumer is disconnected.
- [ ] Rope remains in definitions/inventory but cannot be equipped/used or offered as functioning content. Enforce the same availability decision for authored starting equipment and shop/read models in B6. Unknown item/configuration remains a validation error, distinct from explicit unavailability.
- [ ] Keep Lantern, Whiskey, Roket and supplies active with their existing causal effects. Expire night-scoped item effects/supplies and discard unused equipped items **after enemy phase 8**, before recall/day, preserving unequipped Run inventory and permanent growth. An effect obtained during response 8 expires at that same day boundary.
- [ ] Leave replacement effects, durations, intervals and special-action cadence to the planner; do not implement or promise the earlier speculative lifetime mappings. No replacement Boss allowance is invented. An encounter whose only required boss behavior is disabled must be marked unavailable rather than silently becoming an operational inert boss.
- [ ] Assert no disabled binding/producers alter stats, arm counters, consume equipment/inventory or emit active presentation claims. Preserve unaffected rule tests instead of deleting whole trait/item test families.

### B6. Migrate authoring/stat contracts and remove live timeline registration

**Modify:** `D/Unit/Item/UnitData{,SO}.cs`; `D/Enhancement/Item/EnhancementData{,SO}.cs`; `CS/../CharacterSystem/{UnitSpecAdapter,CharacterDefinitionAdapter,CharacterStatsQueryService}.cs`; `C/{Contracts/Commands/UnitSpec,Domain/Entities/UnitDefinition}.cs`; `C/Contracts/{Snapshots/UnitStatsSnapshot,Snapshots/CharacterBaselineStatsSnapshot,Roster/MainCharacterPersistentState}.cs`; `C/Application/{BootstrapModels/UnitCatalog,Roster/CharacterBaselineStats,Roster/CharacterBaselineStatsCalculator,Roster/OwnedCharacterStatsCalculator,Enhancements/OwnedUnitSpecResolver}.cs`; current Core-owned Stage/night adapters and fixtures; `S/Factory/Runtime/InGame/Core/CombatSystemFactory.cs`, `S/RuntimeSystem/InGameSystem/CoreSystem/InGameCoreSystemGroup.cs`, `S/ServiceKeys/Runtime/InGame/Core/InGameCoreSystemServiceKey.cs`. View-owned development sources such as `DevBattleInitializationSource` are handoff consumers, not permitted edits.

**Availability consumers:** `C/Application/Shop/ShopOfferPlanner.cs`, existing shop/item definition adapters and item read models only for unavailable product admission; preserve shop purchase order/currency behavior. If publication needs an unavailable reason, add it to the existing product/query contract rather than branching on item names in View.

**Assets:** only obsolete unit range/cost authority and active enhancement price-override authoring, old night-clock authoring and required unavailable-content admission for affected `A/Unit/Item`, `A/Enhancement/Item`, Stage/Terrain/Item assets. Capture exact asset paths/fields before editing; preserve all unrelated YAML values, GUIDs and references. Do not delete content assets to disable capabilities.

**Test:** `Framework/CharacterActionCostAuthoringTests`, `DataBaseAssetTests`, `MainCategoryMigrationTests`, affected enhancement/stat/definition fixtures; compile all constructor consumers.

- [ ] Remove unit maximum/current AP fields and `ActionPoint` alias from definitions, persistent/stat snapshots and adapters; replace fixed range/cost authority with Application-derived category values. Main category 10 includes regular units as well as the main role. Disable enhancement price overrides without removing other enhancement/trait choices.
- [ ] Use `PlayerTurnRules` as the sole initial AP10/night8 source. Remove old `NightLength`/Delay-as-clock admission and serialized editing controls; preserve priority stat and unrelated values. Do not convert old serialized night units into player turns.
- [ ] Preserve `TimelineSeed` values for draw/environment/item/growth RNG. Either keep that existing name temporarily with a clear comment or perform a value-preserving rename; do not reroll or delete it because initiative is gone.
- [ ] Audit all timeline consumers, then remove Core query registration/key/group accessors and obsolete scheduling authority in the isolated Core delivery: `CombatTurnOrder`, `CombatTimelineCalculator`, `CombatTimelineScheduler`, `CombatDebuffScheduleCalculator`, query interface/implementation and obsolete timeline snapshots/results. Resolve Core-owned references; record remaining View references in B7 rather than editing them or returning fake forecasts. Integrated compilation waits for the View-owner delivery.
- [ ] Record mechanically required Core/Buffer constructor/test-fixture adaptations. References outside the owned scope become handoff entries, not permission to edit them. Run asset cross-validation and source-copy tests when the project compiles; otherwise record those exact checks as pending. Do not absorb unrelated refactors or gameplay fixes.

### B7. Prepare the View/input handoff — no View edits

**This worker's deliverable:** a concrete contract migration table and unresolved compiler/consumer inventory, kept with this plan or appended to the existing project handoff. Do not create a new document merely to copy this section. Sending it externally requires separate authorization. **Recipient implementation owner:** the View worker; this is not a request for the user to perform the fixes.

**Read-only affected consumer inventory, not an edit allowlist:** `V/{CombatViewSystem,CombatDisplayValues}.cs`; `V/Input/{CombatInputPolicy,CombatInputRouter,CombatInputFacts,PendingInputCommand}.cs`; `V/Input/Interaction/AttackTargetingInteraction.cs`; `V/Input/MovePath/{CombatPathInput,CombatReturnRequest,CombatReturnEstimateTracker}.cs`; `V/Input/ActionButtons/CombatActionButtons.cs`; `V/Display/{CombatSceneDisplay,Unit/FieldUnitPresenter,Hud/CombatUnitInfo}.cs`; `V/Playback/{Handlers/CombatTurnEventHandler,Support/CombatEnemyPresentationPacing,Support/CombatEventSounds,Ordering/CombatActionBoundary,Ordering/CombatEventRoutes,CombatPhaseTransitionPresenter}.cs`; `U/Screens/Stage/{UnitHUD,NightCheckerUI,DeckEditUI}.cs`; `U/Screens/Global/{UnitCardView,UnitCardUI}.cs`; existing `ViewSystem/StageInput` receiver/controller/slot/key-binding files and corresponding input action/scene/prefab references.

The StageInput handoff explicitly includes `S/RuntimeSystem/InGameSystem/ViewSystem/StageInput/{MovementDraft,CombatInputController,IStageInputReceiver,StageInputReceiverSlot,StageInputKeyBindings,StageInputNames}.cs` and `S/RuntimeSystem/GlobalSystem/InputSystem/UnityBridge/KeyBindingService.cs`. MovementDraft must carry the intended route/gesture without becoming a second legality owner.

**Handoff inputs:** B1–B4 snapshots, exact normalized-path preview and ActionAvailability; existing result acknowledgement and UI/Input Framework conventions. **External acceptance target:** coherent selection/shared-budget/player-end UI and no consumer awaiting a removed unit event. The checklist below is limited to preparing the handoff; the following bullets describe the recipient's future work.

- [ ] Record each removed/changed public symbol, its replacement signature/read model, affected caller, expected lifetime and rejection semantics. Include exact compiler diagnostics when available; distinguish compilation fixes from behavior/UI changes.
- [ ] Include example request/result sequences for selecting two units, stale preview rejection, player end 8 → enemy → day, and action-based reload completion. Use the actual delivered contracts, not hypothetical placeholders.
- [ ] Mark View implementation and its tests as external/pending until that owner delivers them. Do not mark those bullets complete merely because this handoff was written.

- External View work: selected allied unit interaction uses Core availability and actor-named commands. Switching selection cannot reset funds/eligibility. Preserve pinned-card behavior; hover geometry is informational.
- External View work: replace local AP/reservation/order presentation with shared current/max and per-unit action availability. Show progress out of 8 plus current side and the eighth pending enemy response. Noncombat cards show rule prices/allowance, not a fabricated current shared balance.
- External View work: delete checkpoint return button/request/tracker/preview and Return binding; preserve draft cancellation and deployment ReturnUnit. Ignore/remove retired saved bindings without renumbering other action IDs or clearing all preferences.
- External View work: show and submit the exact Core-normalized L path/context; do not duplicate legality or silently choose another corner.
- External View work: remove remaining query/order badge/icon consumers, keep TimelineUI retired, and update tutorials/manual EndPlayerTurn controls without auto-end.
- External View work: rebind reload/pacing to action presentation completion and preserve Cancel/completion/release/acknowledgement. Scene transitions consume actual EnterCombat/NightEnded events.
- External View acceptance: verify EventSystem/receiver behavior, scene references and playback. Any independent HUD follows UIBase/UISystem registration; no external activation-service bypass.

### B8. Core delivery audit and verification-limit report

**Files:** all B deliverables; adapt `T/Framework/{CombatPlanTests,CombatPlanDecideTests,CombatProgressJobTests,CoreStageCombatCycleTests,CombatServiceCompositionTests,MovementPublicationTests}.cs`; relevant reward, identity and night/item fixtures. Create focused tests only where these boundaries lack coverage.

- [ ] Check compilation and run the executable Core/Buffer/service/data checks from §7. If unchanged View consumers block compilation, capture that result and leave the affected Unity checks pending for C; a filter cannot make those checks runnable. Validate source/contract consistency without claiming runtime proof.
- [ ] When runnable, verify stale battle/turn/preview source and handled-number idempotency; otherwise carry those checks explicitly to C. For every failure, compare exact name/message with A0 baseline; separate existing failures, intentional superseded tests, fixture adaptation mistakes and new regressions.
- [ ] When runnable, verify unchanged kill gold, victory XP/clear receipts and stage/Run exit; otherwise list the missing evidence for C. Terminal movement/attack/enemy response must not also grant day cleanup or another player start. Retain existing completion presentation waits and Job postconditions.
- [ ] Search production, tests, serialized data and docs for obsolete references using §7. Core-owned unit AP/return/timeline authority must be absent; remaining View/input/UI references must appear in B7 as pending owner work. Complete repository-wide removal is a C integration gate. No fake compatibility signature may imply old unit-turn behavior.
- [ ] Audit diff for Framework, Factory, ServiceKeys, SystemGroup and asmdef changes. Expected structural changes are the named combat query registration removal and actual consumer wiring; no Framework implementation or assembly-reference change is expected.
- [ ] Record status separately: **Core source delivery**, **Core verification**, **View delivery**, **integrated build/tests**, **manual acceptance**. Use done/pending/blocked/not-run according to evidence. Stop after B's Core delivery/handoff; do not call the migration fully complete or verified when an integration prerequisite is missing, and do not take on View fixes to close that gap. Planner-owned content redesign is a separate backlog, not unfinished B implementation; verify safe unavailability and unaffected behavior instead.

## 5. Phase C — Acceptance after the View owner's delivery

### C1. Run the product scenarios and hand off

**Files:** no default production changes. Evidence and this plan's execution record only. A discovered defect returns to its authorized owner; View defects remain View-owner handoff entries, not work for this executor or the user. Acceptance uses the agreed available system/content subset and unavailable-content behavior. It does not wait for the planner's future content overhaul or require every legacy item, trait or gimmick to work.

- [ ] Confirm the external View contract delivery is present. Compile the combined project and run B's deferred tests plus required consumer/integration checks. Resolve ownership of each failure; do not suppress it or edit View to force acceptance.

- [ ] Use actual Boot/Combat composition and valid registered assets. Confirm selection among several units, AP10 shared spending, one action per unit, manual early end and disabled/unsupported feedback.
- [ ] Observe unbudgeted enemies: legal target → attack, otherwise movement; no move-then-attack; player AP unchanged. Check an empty enemy response.
- [ ] Observe a full eight-player-turn night. Enemy response 8 completes before NightEnded, item/supplies expiry and recall/day; the next night restarts local progress and player AP without resetting cumulative completed-player count.
- [ ] Exercise both diagonal corner choices, one blocked L route, both blocked, intermediate hazard stop and preview/commit consistency. Confirm no return input/UI remains and draft cancellation works.
- [ ] Confirm attack/reload/pacing and result acknowledgement finish without a removed unit-turn callback. Verify terminal victory/defeat/abandonment retains the existing destination and new-Run behavior.
- [ ] Record passed/failed/not-run scenarios explicitly. Preserve screenshots/logs only as acceptance evidence; logic green is not a substitute for this phase. Deliver handoff and stop.

## 6. Change-locality assessment

This section implements the supplied independence guidelines. The before state is source-inspected; the after state is a design expectation until implemented and verified.

| Real change | Why scope is broad today | Minimum boundary / future understanding | Necessary verification |
|---|---|---|---|
| Change Cannon cost/allowance | Unit assets, enhancement overrides, per-edge pricing, unit balances and attack refund interact | One category policy; preparation/query/commit consume derived values. A later maintainer understands category policy and action admission, not enemy appointment internals. Normal adapter changes remain allowed when the public contract changes | Category value publication, shared affordability across units, movement reach vs fixed price. These are genuine gameplay dependencies |
| Change diagonal path/entry behavior | Movement reservations, attack seal, return, forced displacement and position fallbacks share bookkeeping | Normalized explicit path is the contract; movement owns traversal/effects, attack only consumes final world facts. Keep existing displacement/contact semantics; do not build a universal movement layer | Preview-to-commit route identity, middle-cell effects, hazard interruption, death/reward attribution and View route display. Removing these checks would hide necessary coupling |
| Reactivate one deferred item/gimmick | Unit scheduler/forecast and own/global turn callbacks mix several clocks | Existing item/trait/gimmick owner gets a specified trigger/lifetime; Core lifecycle invokes that boundary and UI reads availability. No new feature Bootstrap or copied timer state | Its trigger frequency, expiry, disabled→enabled admission, terminal interruption and affected UI/data integration; unrelated trait internals need not be read |

**SOLID/FW acceptance:** SRP separates rules, lifecycle coordination, installation and display; OCP reuses existing feature binding/query seams; LSP preserves rejection and Job-completion semantics while intentionally replacing obsolete APIs; ISP keeps narrow role services/read/write sinks; DIP keeps pure Core independent of Unity/Buffers. SSOT includes one player budget, one per-unit eligibility value, one category rule source and one night baseline. Framework service registration/build/lifecycle contracts remain unchanged. A small helper is justified only if it removes a real shared rule duplication; class/file count is not an improvement metric.

## 7. Verification commands and evidence policy

**No tests were run while writing this plan.** The following commands are for later authorized execution. Use the connected Editor Test Runner if the project is already open; do not start a competing Editor. The installed executable was checked at planning time, but recheck it at execution.

**Current execution restriction (2026-10-24):** the user explicitly limited execution to Core tests. Do not run View/Framework tests under this authorization, including the broader A0 baseline examples below. Their verification remains not run. Phase A uses the recorded Core baseline and Core-only execution in §8.1; later integration evidence must not be inferred from it.

Run native Unity through the approved path with **AppData access**. Sandbox CurlRequestCache startup failure is not a test failure and does not justify deleting caches or upgrading Unity. Start background helpers hidden and inspect progress in bounded intervals.

```powershell
$taskProject = 'D:/My_ProjectWork/Github_desktop/Project_L'
$taskEditor = 'D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe'
$taskEvidence = Join-Path $taskProject '.superpowers/sdd/2026-10-24-player-turn-shared-actioncost'
$taskRun = 'A1-rules'
$taskFilter = 'ProjectL.Tests.Core.PlayerTurnRuleTests;ProjectL.Tests.Core.CategoryActionRulesTests'
New-Item -ItemType Directory -Force -Path $taskEvidence | Out-Null
$taskArgs = @('-batchmode', '-nographics', '-projectPath', ('"' + $taskProject + '"'),
    '-runTests', '-testPlatform', 'EditMode', '-testFilter', ('"' + $taskFilter + '"'),
    '-testResults', ('"' + (Join-Path $taskEvidence ($taskRun + '.xml')) + '"'),
    '-logFile', ('"' + (Join-Path $taskEvidence ($taskRun + '.log')) + '"'))
$taskProcess = Start-Process -FilePath $taskEditor -ArgumentList $taskArgs -WindowStyle Hidden -PassThru
$taskProcess.Id
```

Replace `$taskRun` with a unique run name and `$taskFilter` with the relevant fully qualified fixtures joined by semicolons. Do not copy absent new fixture names into the baseline filter. XML must exist, discovery count must be positive, and all intended cases must be accounted for; process exit alone is insufficient. Record compile diagnostics separately from behavioral verdicts. Do not suppress or remove tests merely to make a filter green.

| Group | Minimum fixture coverage |
|---|---|
| A0 baseline | Existing `ProjectL.Tests.Framework.CombatCommandServiceTests`, `CombatAdvanceServiceTests`, `CombatPlanDecideTests`, `MovementPublicationTests`, `CoreStageCombatCycleTests`; relevant existing Core night/item/route fixtures before their semantics change |
| A1 | `ProjectL.Tests.Core.PlayerTurnRuleTests;ProjectL.Tests.Core.CategoryActionRulesTests` |
| B Core | New `SharedActionBudgetTests`, `PlayerTurnMovementTests`, `PlayerTurnAttackTests`, `EnemyControllerPhaseTests`, `EightTurnNightTests`, `DeferredCombatFeatureTests`; relevant existing path/target/damage/item/death/reward regressions named in each task |
| B publication/flow | New `ProjectL.Tests.Framework.PlayerTurnPublicationTests`; existing command/advance/preview/service-composition/Plan/Job/cycle fixtures, movement source-staleness and reward-publication fixtures |
| B content | `DataBaseAssetTests` and relevant Core-owned authoring/stat fixtures when compilation permits; otherwise record as pending |
| C external consumer/integration | View-owner input/selection/movement/playback/key-binding/night UI tests and adapted timeline-retirement smoke, plus B checks previously blocked by unchanged View. Actual scene/product acceptance uses an appropriate Editor session; headless output is not visual verification |

These groups are the minimum targeted evidence, not a waiver of existing mandatory integration/regression checks. After shared constructor/contract removal, inspect every affected consumer/test-fixture reference and attempt combined compilation. Do not adapt unowned View fixtures; hand them off. Unrunnable checks move to the explicit C pending list, not to a passed/skipped-success count. Broaden execution where the audit identifies a genuine dependency or required suite; do not rerun unrelated suites merely because this document lists many subsystems.

Source audit commands (inspect matches; allowed retained content must be explained):

```powershell
git diff --check
git diff --name-only
rg -n 'CurrentActionCost|MaxActionCost|PendingMoveActionCost|AttackRefundEligibleMoveCost|MovementCheckpointAnchor' Assets/ProjectL/Script -g '*.cs'
rg -n 'EndUnitTurn|ReturnToCheckpoint|CombatTurnParticipant|NextActionTime|CombatTimelineQuery' Assets/ProjectL/Script Assets/Tests/EditMode -g '*.cs'
rg -n 'TimelineSeed|timelineSeed' Assets/ProjectL/Script Assets/ProjectL/DataBaseSO
```

The last search protects required deterministic seeds; it is not a deletion list. Validate `.meta` pairing/GUID preservation and serialized reference integrity for removed/changed consumer types. Keep input bindings and ordinary animation timelines out of blanket text replacement.

## 8. Execution record and stop gate

Current status: **Phase A complete; Phase B delivery is in the user worktree as uncommitted changes. Full Unity project compilation and all 3,497 Core tests pass after the authorized test migration. Framework/View integration execution and manual acceptance remain pending; Phase B integrated acceptance is not claimed. C is unstarted.** Sections 8.2-8.4 record isolated implementation history, 8.5 the transfer, and 8.6 the current verification. The primary design and D7 systems-only clarification are retained. No commit/push or external message delivery was performed.

**Document-only scope clarification, 2026-10-24:** the user confirmed that this migration is systems-only and that content deletion/redesign requires the planner's decisions, then explicitly instructed editing the design and plan without touching code. This clarification updates D7, the lifetime/content boundary, B5 and the B/C completion criteria only. That document-only edit did not start Phase B or change the recorded Phase A implementation/evidence; subsequent implementation and transfer are recorded below.

Planning-stage history: the plan author observed concurrent untracked `CategoryActionRules.cs`, `CategoryActionRulesTests.cs` and their metas and recorded their surface in §2.1. The Phase A executor is the same chat that implemented those files under the user's earlier narrow authorization. Their ownership, code review and test evidence are reconciled below; they were preserved rather than rewritten.

At every authorized Phase stop, append: date and exact HEAD/worktree; actual changed files and owners; test commands/discovered/passed/failed counts and exact baseline comparison; evidence paths; manual checks passed/failed/not run; deviations and unresolved prerequisites; next allowed Phase. B may report Core source/handoff delivery while external integration remains pending, but must never call that integrated build/test completion. Do not begin C or deferred feature reactivation automatically, and do not assign the remaining View implementation to the user.

### 8.1 Phase A execution — 2026-10-24

**Baseline and ownership:** `D:/My_ProjectWork/Github_desktop/Project_L`, branch `feature/Cheon/Core/NewCombatSytem`, HEAD `d73fb5c6192cb2ea7e0bb9bd15b0b9509f84658c`. There was no attached app worktree. The existing additive work continued in this checkout; other worktrees were not used. Initial status contained only the four earlier category source/test/meta files and the design/plan as untracked files. The design and concurrent B/C ownership refinements were preserved. Unrelated concurrent changes later appeared in `AGENTS.md`, `CLAUDE.md`, `README.md` and other documents; they were left untouched and are not part of this Phase A delivery.

**Delivered files:** added `C/Contracts/Common/PlayerTurnRules.cs`, `C/Contracts/Combat/{PlayerActionBudget,UnitActionUse,CombatControllerPhase,ActionFundingKind}.cs`, `T/PlayerTurnRuleTests.cs`, and all six metas. Updated this plan's checks and evidence. Reused the earlier `C/Domain/Categories/CategoryActionRules.cs`, `T/CategoryActionRulesTests.cs` and their metas unchanged. `PlayerActionBudget` is a readonly value with a rule-derived maximum; default/Empty is zero. Negative spending throws `ArgumentOutOfRangeException`; insufficient funds throw `InvalidOperationException`; neither changes the source value. Normal gameplay rejection/admission remains a B concern.

**Review and boundaries:** an independent review of the pre-plan category code and a separate final Phase A review found no actionable issues. The current tuple API matches §2.1, Boss movement stays unspecified, and no additional forwarding API is needed. Reference searches found only the new definitions and test consumers. No existing runtime, Buffer, DB, View, Framework, Factory, ServiceKey, SystemGroup or asmdef source changed; no live budget or eligibility field was installed. The eight source/test metas have unique GUIDs. New files use UTF-8/LF. Unity's FMOD `lastScannedFolder` import byproduct was restored after checking the exact one-line diff.

**Verification:** Unity 6000.0.73f1 ran through the approved AppData-capable path, hidden, with `-batchmode -nographics -runTests -testPlatform EditMode -assemblyNames ProjectL.Tests.Core` and the current project path. The initial run additionally used `-testFilter ProjectL.Tests.Core.PlayerTurnRuleTests`; the final run selected the entire Core assembly. Evidence:

| Run | Result | Repository-relative evidence |
|---|---|---|
| Reused pre-A baseline | Core 3680/3680 passed, 0 failed/skipped, including category 8/8 | `.superpowers/sdd/2026-10-24-category-action-rules/core-green.xml` |
| A1 RED | 20 discovered; 17 expected assertion failures for missing rule/spending/validation behavior, 3 zero-spend cases passed; no compile failure | `.superpowers/sdd/2026-10-24-player-turn-shared-actioncost/A1-red.xml` and `.log` |
| A1 final Core run | 3700/3700 passed, 0 failed/skipped; category 8/8 and new player-rule 20/20; Unity exit 0 | `.superpowers/sdd/2026-10-24-player-turn-shared-actioncost/A1-core-green.xml` and `.log` |

The final XML contains only `ProjectL.Tests.Core.dll`. Compared with the reused baseline, all 3680 existing cases remain passing and 20 cases were added. Compile warnings match those already observed before this phase: CS8632 in `CombatCommandEffects.cs` and `CombatCommandTestData.cs`, and CS0105 in `Framework/LobbyDepartureIntegrationTests.cs`; no such file was changed. Unity compiling an existing Framework test assembly is not execution of its tests. View/Framework tests and manual scene acceptance were **not run**.

**Change locality and stop:** the new budget rule can be understood and tested through its value contract, without modifying combat orchestration or reading unrelated trait internals. Actual shared-state publication and consumer adoption are still required in B; pure rule success does not establish those behaviors. Phase A is complete under the current Core-only verification scope. Next eligible work is an explicitly authorized Phase B, using the latest Core/Buffer delivery and external View-owner handoff boundary, with an isolated checkout containing accepted A work. No B/C implementation or external handoff was started here.

### 8.2 Phase B source delivery and verification boundary — 2026-10-24

**Subsequent authorization (2026-10-24):** the user now permits the minimum View consumer/fixture changes needed to compile against the delivered Core contracts and run Core tests, in this isolated worktree only. Independent checkpoint-return and timeline code may be deleted; shared HUD/input/playback code should have only those retired connections disabled. This overrides the earlier no-View-edit boundary for this narrow continuation. Do not modify the primary branch, commit/push, rebuild UI/scenes, run Framework/View tests, or add agents. Execution order: (1) disconnect retired requests/queries and migrate live consumers, (2) resolve project/fixture compilation without excluding assemblies, (3) run Core tests, repair owned regressions and record actual evidence. Full product/UI acceptance remains separate.

**Checkout:** `C:/CodexHome/worktrees/player-turn-phase-b/Project_L`, HEAD `d73fb5c6192cb2ea7e0bb9bd15b0b9509f84658c`. This is the attached isolated worktree. Accepted A sources/tests/metas and this design/plan were copied using the recorded SHA256 manifest. The primary checkout's copied A source/meta hashes still match that manifest. Concurrent primary-checkout documentation work was not edited. This worktree has uncommitted changes; nothing was merged, committed, pushed, or sent to another worker.

**Status by boundary:**

| Boundary | Actual status |
|---|---|
| B1–B6 Core/Buffer/data source implementation | Implemented and inspected; native whole-project compilation and runtime acceptance remain pending |
| Core tests | Updated/added/retired according to the new contract; Unity Core-only execution attempted, but no test run or result XML |
| B7 contract handoff | Prepared below; not sent externally |
| B8 source/ownership/asset audit | Completed; details and limits below |
| View/input/UI delivery | Pending external View-owner implementation; no edits by this worker |
| Systems/Buffer compilation, integrated tests, asset cross-validation | Blocked by unchanged View declarations; additional diagnostics may surface after those declarations migrate |
| Framework/View tests and manual scene acceptance | Not run; outside the current test authorization |
| Phase C | Not started |

The task checkboxes above combine implementation with acceptance. Unchecked verification requirements remain pending; the table here distinguishes delivered code from unproven behavior. A0/A1 green is not B evidence.

**Implemented scope and ownership:**

- B1: `CombatCommandState.PlayerBudget` is the shared immutable balance; `CharacterCombatFacts.ActionUse` is per-unit eligibility. `CombatStateBuffer` remains the owner of live installation, result sequence, handled-command state and publication. It checks source identities, spend/eligibility settlement and owner-start boundaries before installation. No second budget or controller service was added.
- B2: `MovementPathNormalizer`, movement calculation/projection and context comparison use a complete explicit orthogonal route. One accepted player move costs 2; entered distance and intermediate effects are separate. Checkpoint return, pending movement and attack sealing were removed. Deployment `ReturnUnit` remains.
- B3: `CombatActionAdmission` centralizes actor/controller/eligibility/funding admission used by movement, attack and availability. Category rules provide prices and allowance. Ordinary attacks consume one action; hit chains, reflection, knockback and other reactions retain their existing settlement routes.
- B4: controller starts, manual player end, stable-ID enemy processing and enemy-phase completion replace the unit scheduler. Player-start alone replenishes AP10. Response 8 precedes night cleanup/recall/day. Terminal results preserve the terminal boundary. Starting another night preserves cumulative completed-player turns.
- B5: runtime binding/production of the deferred features is disconnected at its feature owner. Rope is unavailable for equipment, authored starting equipment and shop offers. Initial random tornado production is disconnected. Existing thorn contact damage remains calculable, but contact no longer arms a deferred defense-reduction timer. Item/supplies cleanup occurs at the eighth completed enemy response. A nonattacking necromancy-only encounter is rejected as unavailable.
- B6: unit AP, authored movement prices/range and delay/night-clock authority were removed from data/stat adapters and assets. `TimelineSeed` values remain unchanged for other random streams. Removed `CombatTurnSnapshot`/`StageNightSnapshot` legacy clock records; `StageSnapshot.CombatTurn` now carries `CombatTurnFacts` and has no separate night clock. Removed `CharacterActionCostRules`, timeline query/scheduler contracts and checkpoint-return contracts.
- Mechanical integration: `TemporaryCoreDatabase` constructors were adapted to the new UnitData/CombatStageDetail signatures. The other Factory/ServiceKey/SystemGroup changes only remove timeline-query creation, registration and access. No Framework implementation, View, LifeCycle, scene, prefab, input binding or asmdef change was made.

**Focused review corrections:** the Buffer now permits the real deployment-to-combat eligibility deferral, including the persistent main on a later night. Existing thorn contacts cannot create an unconsumed timed debuff. The live Rider category snapshot exposes no refund-use state (`RefundUsedThisTurn == null`), rather than implying an unused active refund. Trait snapshots publish `IsAvailable`, `UnavailableReason` and partial-unavailability information for the retained attack portions of Intimidation/Frenzy.

**Test migration policy:** the user's later instruction explicitly permits deleting tests whose only purpose was the retired behavior. The obsolete timeline forecast helper and scheduling/return-only cases were removed. Old random-tornado generation and timed soul-spawn/countdown integration cases were removed; compact disabled-producer assertions replace them. Old poison-thorn timer behavior was removed while preserving contact damage, source-context comparison and death cleanup coverage. Pure deferred-rule/definition tests remain where they still validate retained calculations. No Ignore/skip, compatibility AP fields, fake timeline forecast, assembly exclusion or standalone test harness was introduced.

New rule coverage is in `PlayerTurnMovementTests`, `PlayerTurnAttackTests`, `EnemyControllerPhaseTests`, `EightTurnNightTests` and `DeferredCombatFeatureTests`, alongside migrated lifecycle/common-command/reward/experience/item/damage tests. Shared-budget cases are consolidated in the attack and lifecycle fixtures instead of adding a duplicate `SharedActionBudgetTests` class. This is a source inventory, not a statement that these tests passed. Integration fixture migration/execution in the Framework test assembly remains pending under the Core-only restriction.

**Native verification:** Unity 6000.0.73f1 was launched hidden through the approved AppData-capable path with:

```text
-batchmode -nographics
-projectPath C:/CodexHome/worktrees/player-turn-phase-b/Project_L
-runTests -testPlatform EditMode -assemblyNames ProjectL.Tests.Core
-testResults <evidence>/B-core-12.xml -logFile <evidence>/B-core-12.log
```

Evidence directory: `.superpowers/sdd/2026-10-24-player-turn-shared-actioncost-implementation-plan/`. The initial attempts exposed incomplete Core/test contract migration and were used to repair owned references. Final `B-core-12.log` reports the same seven CS0246 diagnostics as runs 10/11 below and no Core/test diagnostics. Exit code is 1; the requested XML does not exist. Tests discovered/passed/failed are **not available**, not zero failures or a passing subset. Compilation of the Systems assembly is not established: View declarations can prevent later body diagnostics in that same assembly from being reported.

| Exact unchanged View location, relative to V | Missing type |
|---|---|
| `CombatDisplayValues.cs:18,43` | `CombatTimelineEntrySnapshot` |
| `CombatViewSystem.cs:110` | `ICombatTimelineQuery` |
| `Display/CombatSceneDisplay.cs:604` | `CheckpointReturnPreview` |
| `Display/Hud/CombatActionPanel.cs:148` | `CheckpointReturnPreview` |
| `Display/Unit/FieldUnitPresenter.cs:394` | `CombatTimelineEntrySnapshot` |
| `Input/Contracts/ICombatReturnFeedback.cs:13` | `CheckpointReturnPreview` |

**Source audit:** `B-source-audit.json` lists all changed paths and the ownership/meta/asset checks: 322 tracked changed paths and 36 untracked paths, including the copied A work and design/plan. It reports no forbidden-area changes, no C#/.meta pairing errors, no duplicate GUIDs among 17 new metas, and no primary A source/meta hash mismatch. The 31 asset edits are limited to 14 Unit assets (`moveRange/moveCost/attackCost/delay`), five Enhancement assets (`overrideMoveCost/moveCost/overrideAttackCost/attackCost`) and 12 Combat Stage assets (`nightLength`). All other YAML values, existing references, GUIDs and seeds compare equal. Existing YAML whitespace is preserved. Final `git diff --check` passed. This audit is not `DataBaseAssetTests` and does not establish playable content readiness.

#### 8.2.1 Delivered contract handoff for the View owner

The following is the concrete B7 delivery. The owner must update consumers, their fixtures and presentation behavior; replacing the seven missing type declarations alone is insufficient.

| Removed/changed contract | Actual replacement and lifetime | Affected consumers |
|---|---|---|
| Per-unit AP, pending move/refund/reservation and active actor | `Current.CommandState.PlayerBudget.Current/Maximum`; `CharacterCombatFacts.ActionUse` (`Deferred/Available/Moved/Attacked`); `Turn.ControllerPhase/ControllerTurnSequence`. Selection has no Core mutation | CombatInputFacts/Policy/Router, PendingInputCommand, CombatUnitInfo, CombatSceneDisplay, FieldUnitPresenter, UnitHUD and card UIs |
| `EndUnitTurn` | `CombatViewCommandData.EndPlayerTurn(long expectedBattleInstanceId, long expectedControllerTurnSequence)`; no actor. Uses identities captured at submission and rejects stale turn identity; never substitutes a current actor | CombatActionButtons, pending command and StageInput end-turn route |
| Attack request without identity | `CombatViewCommandData.Attack(EntityId attacker, AttackAim aim, long expectedBattleInstanceId, long expectedControllerTurnSequence)` | AttackTargetingInteraction, router and pending command |
| Action enablement inferred from unit AP | `CombatViewPreviewData.ActionAvailability(EntityId)` → `CombatPreviewResult.ActionAvailability`, with actor, battle/turn identity, nullable player funds, allowance, fixed prices and typed movement/attack failures | Selection/action buttons; keep hover-only `UnitAttackRange` informational |
| Draft/checkpoint movement | `Move(EntityId, MovementPath, MovementPreviewContext)`; `response.Move.Value.PathMovement.RequestedPath` and `response.MovementContext.Path` are the chosen expanded route. Display and submit that route/context. Context validates shared budget, eligibility, identity and relevant visible world | CombatPathInput, MovementDraft, movement receiver/controller and field feedback |
| `ReturnToCheckpoint`, `CheckpointReturnPreview/Result`, `CanReturn` | No replacement execution command. Keep draft cancellation and deployment `ReturnUnit` | CombatReturnRequest, CombatReturnEstimateTracker, ICombatReturnFeedback, CombatActionPanel, StageInput Return binding/preferences |
| Unit TurnStarted/TurnEnded and initiative query | `CombatTurnEvent` for `ControllerTurnStarted(36)`, `PlayerTurnEnded(37)`, `EnemyPhaseEnded(38)`; existing `CombatEntered`/`NightEnded` retained. No forecast/query replacement | CombatTurnEventHandler, CombatEventHandler, CombatEnemyPresentationPacing, CombatEventSounds, action ordering and reload completion |
| Old night length/time | `Turn.CompletedPlayerTurnsThisNight` and `PlayerTurnRules.PlayerTurnsPerNight == 8`, plus controller phase. Count 8 while Enemy is active means the final response is still pending | NightCheckerUI, HUD, phase transition presentation |
| Unit/Stage authoring constructor signatures | No max/current unit AP, authored move prices/range, Delay or NightLength arguments. Priority and TimelineSeed retained. Noncombat cards show derived rule prices/range; no invented current balance | DevBattleInitializationSource, UnitCardView/UI, DeckEditUI and relevant fixture constructors |
| Disabled content presented as usable | Trait status availability/partial reason; Rope unavailable in item/equipment/shop admission; Rider refund state absent. Preserve authored identities without describing disabled effects as active | Trait/card/tooltips, equipment/shop UI and tutorials |

Failure enums include `WrongController`, `ActionAlreadyUsed`, `ActionDeferred`, `StaleTurn`; shared insufficiency is still `ActionCostInsufficient`. Movement source/context changes reject with `StaleContext`. `CombatResult` publishes `BattleInstanceId`, `ControllerTurnSequence`, the initiating actor and optional `ActionCost`. `ActionCostSettlementSnapshot` carries `ActorId, FundingKind, Spent, Before, Final, BeforeActionUse, FinalActionUse`; unbudgeted enemy results have null balances and zero spend. The Core-owned advance service chooses the enemy; View must not supply it.

**Request/result sequences:**

1. After a real player start, read battle b/turn s, budget 10 and A/B Available. Select A, request availability and move preview. Display the normalized path and submit `Move(A, preview.PathMovement.RequestedPath, response.MovementContext)`. Accepted result spends 2 and publishes A=Moved, budget=8. Selecting B does not reset anything; a valid ordinary non-Cannon attack by B costs 3, leaving 5 and B=Attacked.
2. If another action changes the shared budget or a relevant source after A's preview, submitting the saved context is rejected as `StaleContext`. If a saved attack/end request belongs to an older controller sequence, it is rejected as `StaleTurn`. Consume the rejection through the existing acknowledgement path and request a fresh preview; do not rewrite the captured identity.
3. With seven player ends recorded this night, submit `EndPlayerTurn(b, s)`. The result publishes count 8 and StartingEnemy, with no NightEnded. Automatic start advances the controller sequence, enemies each act at most once, and no-action completion then expires round items/supplies, recalls survivors and publishes NightEnded/day. A terminal attack/reaction stops this chain before day or a player refill.
4. An accepted attack publishes the actor and attack/reaction events. Complete reload/pacing from that actor's action presentation completion, then preserve the existing result acknowledgement. Do not wait for a removed unit-turn callback. Cancel/failure/release behavior remains the View owner's responsibility.

The StageInput file list in B7 is still required, including `MovementDraft, CombatInputController, IStageInputReceiver, StageInputReceiverSlot, StageInputKeyBindings, StageInputNames` and `KeyBindingService`. Preserve other serialized action IDs/preferences. The UI list also includes `U/Screens/Stage/{UnitHUD,NightCheckerUI,DeckEditUI}` and `U/Screens/Global/{UnitCardView,UnitCardUI}`. Existing Framework test fixtures still reference removed constructors/symbols; their owner must migrate them before combined compilation/tests.

#### 8.2.2 Stop gate and remaining evidence

Source delivery is ready for review in this worktree, with **runtime correctness unverified**. After the external View/fixture delivery, the remaining checks are: combined compilation; the entire Core assembly (including migrated tests); Buffer stale-source/spend/start/handled-number atomicity; command/advance/preview/result publication; two real consecutive nights; deferred item/trait/content admission; kill gold, victory XP/clear receipts, and terminal stage/Run exit. Asset cross-validation and the required Framework/View integration tests need their own authorization. Further compile diagnostics or behavioral failures must be assigned to their actual owner, not hidden by restoring retired APIs.

The change-locality result is source-inspected: prices/allowance now have one category rule source; movement owns traversal and attack consumes final positions; Buffer owns installation. Necessary shared-world damage/reward and presentation integrations remain necessary verification. No performance or manual-play acceptance was inferred. Do not start C, re-enable deferred effects, add SO policy authoring, or merge this worktree automatically.

### 8.3 Minimal View production adaptation and test-scope correction — 2026-10-24

**Latest authorization:** adapt only the necessary View production consumers in the same isolated worktree. Delete independent checkpoint-return/timeline code; disconnect retired functionality from shared input/HUD/playback. The user then explicitly instructed that View test code be left alone and prioritized production compilation. That later instruction supersedes the fixture-migration portion of the continuation note in §8.2.

**Final retained changes:**

- Removed the independent `CombatReturnRequest`, `CombatReturnEstimateTracker`, `ICombatReturnFeedback` and their metas. Removed their calls from movement input and display. The existing Return input slot remains inert, and the serialized return button is hidden/noninteractive. Deployment return, ordinary movement preview, draft cancellation and their shared UI remain.
- Removed timeline-query retrieval and the timeline field in `CombatDisplayValues`. Disconnected per-unit order badges without removing shared unit HUDs or presentation. No empty/fake Core timeline API was restored.
- Reused the existing unit-info selection as the View-selected allied actor. Core budget/eligibility remain authoritative; selecting a card does not mutate either. Movement input reads that selection through its existing assembly point. Attack input captures battle/controller identity; player end is actor-free and captures the same identity.
- Migrated current-player-turn progress and fixed price text; hid obsolete per-unit AP visuals. Shared-budget HUD redesign was not added. Development fixture constructors now use the current Core contracts.
- Migrated side-boundary pacing and disconnected old unit-turn pickup audio. Ballista reload starts after actual attack animation completion through the existing attack playback lifetime, rather than waiting for a removed unit-turn event. Cancellation, completion and manual scene acceptance have not been exercised in this continuation.
- No Framework implementation, LifeCycle, scene/prefab, asmdef or primary-checkout source changes were made. The only additional assembly-site edit is `CombatSceneInputFactory` passing the selected-actor reader.

**Test scope and restoration:** early continuation attempts exposed old declarations in the Framework test assembly. A fixture migration was started but the user then directed this worker to leave those tests alone. All 163 Framework/View test paths changed or deleted during that attempt were restored to their pre-continuation contents. The pre-continuation source audit had no changes in that directory; the session edit ledgers and the remaining explicit diffs were checked before restoration. Final `git diff --name-only -- Assets/Tests/EditMode/Framework` is empty. Existing Phase B Core test changes remain. The intermediate fixture-migration scripts/logs under ignored `.superpowers` are historical evidence only and must not be rerun as a pending task.

**Dependency clarification:** `ProjectL.Tests.Core.asmdef` references only `ProjectL.Core.Contracts`, `ProjectL.Core.Domain`, `ProjectL.Core.Application` and NUnit; `noEngineReferences` is true. Those Core assemblies have no View/System/Framework test reference. `ProjectL.Tests.Framework` is a separate assembly containing Framework, service and View tests. The blocker is Unity's project compilation before test execution, not a Core test requiring a View test result.

**Final evidence:** `V-compile-final.log`, using Unity 6000.0.73f1 through the approved AppData-capable path with `-batchmode -nographics -quit -projectPath <isolated-worktree> -logFile <evidence>/V-compile-final.log` and **no test-run argument**. It reports 0 production-source compiler diagnostics and 29 diagnostics in the unchanged `Assets/Tests/EditMode/Framework` sources. `Library/ScriptAssemblies/ProjectL.Systems.dll` was regenerated (2,056,192 bytes; local time 2026-10-24 05:59:55). Unity exits 1 because the test assembly still has errors. The 29 are the currently reported declaration errors, not proof that no additional test errors remain.

Earlier `V-core-1` through `V-core-6` attempts targeted only `ProjectL.Tests.Core`; no result XML or executed tests were produced. No Framework/View tests were executed. Final `git -c core.autocrlf=input diff --check` passed. **Production source compilation is established; full Unity project compilation, Core test execution and manual/UI acceptance are not established.** The user branch remains untouched. Do not resume test migration, exclude test assemblies, add compatibility stubs or change assembly references without a new instruction.

### 8.4 View owner handoff document — 2026-10-24

At the user's request, the [Core to View handoff](../handoffs/2026-10-24-player-turn-core-to-view-handoff.md) now provides the current recipient-facing contract mapping, complete retained View change inventory, 29 reported compiler diagnostics, and verification ownership. Core/Buffer verification remains with the Core owner; the View production adaptations are provisional and require View review. This is documentation delivery only in the isolated worktree. It does not establish test or gameplay acceptance, send the document externally, or apply changes to the user branch. Sections 8.2 and 8.3 remain the historical execution record.

### 8.5 Transfer to the user worktree — 2026-10-24

The user subsequently authorized transferring the prepared work into `D:/My_ProjectWork/Github_desktop/Project_L`. Target baseline: branch `feature/Cheon/Core/NewCombatSytem`, HEAD `42d3263acc7164c8da06c930df154f3ea7741ebb`, initially clean. The source remains preserved at `C:/CodexHome/worktrees/player-turn-phase-b/Project_L`, based on `d73fb5c6192cb2ea7e0bb9bd15b0b9509f84658c`.

The delivery manifest contains 391 paths: 306 existing files updated, 48 retired files deleted, 19 new files added, this plan merged, 16 identical Phase A files left unchanged, and the newer target design preserved unchanged. Runtime/source changes have no overlapping newer target edits. The 305 target paths changed since the source base but outside the delivery set are preserved byte-for-byte or remain deleted. The current D7 design, planner-owned content decisions, and plan constraints remain authoritative; this plan combines them with the source execution record rather than replacing them with an older plan.

Transfer preparation and verification use exact source/target hashes and a backup manifest outside the repository at `C:/CodexHome/visualizations/2026/10/23/01a16992-5a3f-7892-a093-dfca1a3cc331/phase-b-transfer/`. No branch switch, index staging, commit, push, or source-worktree removal is part of this transfer. Framework/View test files remain untouched by this delivery, including newer target fixtures.

The section 8.3 compile result is historical evidence from the isolated source: 29 test-source compiler diagnostics and no Core test execution. It is not a fresh compile/test result for the newer target checkout. Transfer integrity is separate from gameplay acceptance; the verification responsibilities and open View tasks in the linked handoff still apply. Earlier statements that the primary checkout was untouched describe the stages before this authorized transfer.


### 8.6 Retired-test cleanup, contract migration, and Core verification — 2026-10-24

**Authorization and location:** the user explicitly requested deletion of tests dedicated to retired functionality, minimal migration of retained tests, no new View production behavior, updated handoff, compilation, and Core tests if possible. This supersedes the earlier temporary instruction to leave View test sources untouched; it does not authorize running Framework/View suites. Work occurred in `D:/My_ProjectWork/Github_desktop/Project_L`, branch `feature/Cheon/Core/NewCombatSytem`, HEAD `42d3263acc7164c8da06c930df154f3ea7741ebb`. The isolated source was preserved. No subagents, commit, push, or Phase C work were used.

**Changes:** 217 test paths changed against the pre-migration snapshot (44 Core, 173 Framework, including paired meta deletions; excluding line-ending-only differences). Nine retired-only classes and their metas were deleted. The [handoff section 4](../handoffs/2026-10-24-player-turn-core-to-view-handoff.md#4-test-migration-and-resolved-compile-barrier) records their names, reasons, mixed-class treatment, and owner boundaries. Fixtures now capture battle/controller identity, preserve obstacle profiles, use shared budgets and action use, and begin actual owner turns for repeated ordinary actions. Damage, rewards, geometry, immutability, and cleanup coverage were retained where applicable. Enemy wall pursuit uses category-limited movement across turns. No ignore directives, assembly exclusion, replacement harness, or fake retired API was added.

**Additional Core repairs found by retained tests:** `CombatAttackQueries.Range` keeps geometric range information on insufficient funds; `CombatAttackCalculator.Calculate` returns spatial blockage evidence before the funding rejection; `AttackFailureReason` preserves existing numeric IDs across removed slots. These change three production files relative to the transferred delivery. Funding enforcement remains in action availability/execution. No additional View, Framework implementation, LifeCycle, scene/prefab, or asmdef changes were made during this continuation. Unity's local FMOD scan-folder rewrite was removed after verification.

**Native evidence:** Unity `6000.0.73f1`, approved AppData access, `-batchmode -nographics -projectPath D:/My_ProjectWork/Github_desktop/Project_L -runTests -testPlatform EditMode -assemblyNames ProjectL.Tests.Core -testResults <evidence>/core-9.xml -logFile <evidence>/core-9.log`. Final run: **3,497 discovered, 3,497 passed, 0 failed, 0 skipped**, exit code **0**, 2026-10-24 10:54:50-10:55:00 UTC. Full project source compilation, including Framework/View test sources, completed before execution. No C# compiler diagnostics were present in the final run. Earlier executable runs were 3,541/3,424/117 and 3,497/3,476/21, then 3,497/3,493/4 (total/pass/fail); intermediate compile errors were repaired. The total dropped by 44 explicitly retired cases, not by skipping failures. These are migration runs, not a green pre-change baseline comparison.

**Local evidence location:** `C:/CodexHome/visualizations/2026/10/23/01a16992-5a3f-7892-a093-dfca1a3cc331/test-contract-migration/`. `core-9.xml`, `core-9.log`, `final-audit.json`, `retired-core-methods.json`, and the pre-edit snapshot/ledger record results and disposition. Meta pairing passed; no new skipped/explicit tests were introduced. The additional production hash comparison identifies only the three Core paths above. Framework implementation, LifeCycle, scenes, and assembly boundaries remain unchanged by this continuation.

**Change locality and limits:** the necessary fixture migration crosses constructors, controller identity, shared spending, and action lifetime; it does not require understanding or rewriting unrelated View internals. Real next-turn fixtures replaced manual action resets, and obstacle context copies retain their full validation contract. Core regression execution now covers retained rules. Framework/View source compilation is not behavioral validation: Buffer atomicity/publication, input selection lifetime, ActionAvailability/UI wiring, shared HUD, Ballista reload/cancellation, asset cross-validation, Flow exits, and two actual consecutive nights remain owner work. In particular, the old immediate night-clock recall test is retired; new eighth-enemy-response publication integration still needs execution.

**Stop gate:** requested test cleanup, compilation, Core execution, and handoff update are delivered. Keep the existing D7 planner boundary and do not reactivate deferred content or begin C. Integrated Phase B/View/manual acceptance remains open. Commits and push remain user-owned.
