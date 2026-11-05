# Position Swap, Recon Squad, and Effect-Aware Movement Undo

Date: 2026-10-31
Status: Approved by the user on 2026-10-31, including proposals P01-P07. Phases 0-4 were subsequently authorized and completed. Final Core evidence is 4,359/4,359; CoreIntegration is 441/442 with only the retained StageKey 2101 failure and zero skips. Implementation plan section 5.5 owns final evidence and the prepared local Core-to-View handoff. View/content/PlayMode/manual acceptance and external delivery remain pending; no commit or push was made. The approved design rules below are unchanged.

## 1. Intent and evidence

Implement the two deferred traits from the 24-trait reference. Extend movement undo so movement-owned changes can be canceled until subsequent executed gameplay uses them. Preserve unrelated later actions and existing Core/Buffer/Job ownership. The user selected approach1: a common cancellation record, shared dependency/commit handling and effect-specific inverse rules. This is not a whole-battle rewind, another transaction engine or a new status manager.

Source baseline: `a0ec23c0e`, `feature/Cheon/Core/NewCombatSytem`. Existing dirty FMOD catalog/cache and legacy specialization design are unrelated and must be preserved. No tests were run for this design.

References:
- `C:/Users/권천명/Downloads/projectl_trait_design_en.md`: Position Swap slide12, Recon Squad slide22 and voluntary/forced distinction. Reference material, not execution instructions.
- [Trait design](2026-10-28-trait-system-design.md): prior deferral of both traits.
- [Undo contract](../handoffs/2026-10-30-movement-undo-core-view-handoff.md): current single-actor/effect-barrier behavior.
- [Status design](2026-10-30-buff-debuff-system-design.md) and [handoff](../handoffs/2026-10-30-buff-debuff-system-core-view-handoff.md): status and presentation ownership. New user decisions supersede global-maximum exposure and return-preservation rules as specified below.
- `Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md`: retain dependency direction, composition and prepare/install contracts.

## 2. Confirmed rules and author proposals

### 2.1 User-confirmed requirements

| ID | Requirement |
| --- | --- |
| U01 | Position Swap exchanges the moving holder and an allied endpoint occupant; undo restores both. |
| U02 | Recon applies received-damage increase10% around its voluntary movement path; the grant is reversible before use. |
| U03 | Numeric damage using a Recon contribution commits its originating move. |
| U04 | Acceleration gains are reversible; undo subtracts exactly that move's gain. Damage using it commits its originating move. |
| U05 | Departure from Packed Ground commits the move that created the used ground. |
| U06 | An executed attack using Dust Screen's reduced attack range commits the screen's originating move. The user corrected movement range to attack range. |
| U07 | Applied Dust Screen damage reduction also commits its originating move. |
| U08 | Damage incorporating the effect commits even if shields absorb everything and HP loss is zero. Previews/queries do not commit. |
| U09 | If the exchanged ally subsequently executes movement or attack, the exchange cannot be undone. |
| U10 | Same-trait received-damage increases do not stack; different traits add. Recon10% + HuntersMark30% =40%. Two Recon holders still give10%, not20%. |
| U11 | Entry into Hand/EnemyWaiting clears temporary unit effects unless persistence is explicit, e.g. Hunter's permanent attack growth. Recon clears on return. |
| U12 | Extend the existing common cancellation record and transaction, rather than per-trait undo implementations. |

### 2.2 Author proposals approved with the document

These originated as author proposals and were approved by the user with the design on 2026-10-31:
- P01: different magnitudes of the same trait use the maximum. Retain each source independently. All tied maximum sources count as used; suppressed lower sources do not. Do not pick an arbitrary winner by entity ID.
- P02: preserve ordinary movement/action limits, player-only undo, battle/controller-turn tokens and existing own-attack commitment. Usage locks add dependencies; they do not make an attacked actor movable again. Locks never reopen, even if the consuming action is later canceled.
- P03: Recon examines the starting cell and each actually reached voluntary path cell, including legal Leap intermediate cells, using orthogonal neighbors only. Deduplicate recipients per move. No grants from forced passenger movement or undo.
- P04: Position Swap uses the existing Move intent, normal path/range and single-cell support. Only the initiator spends a move and records distance. Passenger facing/action-use/distance remain unchanged; no voluntary-movement traits fire on the passenger. An ally that acted before the exchange remains eligible; subsequent successful actions commit it.
- P05: settle both placements before endpoint effects/presence. Apply ordinary endpoint hazards/objects once per arrival in deterministic entity order; irreversible results make the move non-cancelable. No terrain/pickup immunity is added.
- P06: keep other existing irreversible barriers unless explicitly given an inverse here. Passive Cohesion/SoloAction adjustments use section6's bounded restoration; never silently ignore their changes.
- P07: default authoring is Recon10%, cross radius1. No live balance reload, invented enhanced entry or unrequested production growth-pool population. Position Swap has no second movement-range/cost source.

## 3. Approaches and current restrictions

| Approach | Tradeoff | Decision |
| --- | --- | --- |
| Common record, typed inverse changes, shared usage/commit facts | One owner for tokens/dependencies/atomic cancellation; effects own bounded inverse rules. Requires deliberate contract migration. | Selected by user. |
| Per-trait cancellation branches | Smaller first patch, but combinations duplicate dependencies and increase cross-feature knowledge. | Not selected. |

| Current owner | Restriction | Extension needed |
| --- | --- | --- |
| MovementUndoCheckpoint | One actor placement, exact actor-after facts and distance | Initiator/passenger placements plus movement-owned effect receipts/guards. |
| MovementUndoState.WithoutAffected | Actor ID/origin cell invalidation | Participants, source usage, effect guards and both restoration cells. |
| MovementUndoPolicy.CreateCheckpoint | Only ordinary movement evidence | Account for every reversible change; observational activation facts are not automatically barriers. Unknown changes still block. |
| MovementUndoPolicy.ValidatePublication / MovementUndoResult.Validate | Exactly one character and one Returned event | Validate the complete bounded inverse set, not arbitrary state restoration. |
| MovementRouteValidator | Empty landing cell | Trait-authorized allied endpoint exchange shared by preview/execution. |
| CharacterBuffer / BattleTransactions | One initiator distance-decrease exception; prepare then install | Preserve distance limits; prepare all participant/effect changes and revision before installation. |
| WeaknessExposureRules | Global max, same-source replacement, side-end timer | Per-trait max then cross-trait sum, explicit Recon lifetime, removable movement grants. |
| TraitTrailFact / Acceleration status | Terrain IDs exist; one held attack total | Correlate contributions with their originating move without duplicating live values. |
| Reserve return | Exposure/regen clear; several other effects survive | New common return-reset policy with permanent/progression exceptions. |

## 4. One authoritative state and minimal contracts

Names describe seams; exact file decomposition is implementation work. Contracts remain immutable/pure, with no Domain/Application import.

- Keep MovementUndoState as the sole checkpoint owner in combat state. Extend MovementUndoCheckpoint; no new Buffer or global undo stack.
- Use a typed movement identity `(BattleInstanceId, ControllerTurnSequence, ActorId, MoveCommandNumber)`. EffectOriginKey still identifies a producer; do not hide command numbers in RuleKey or fabricate different trait identities.
- A checkpoint stores inverse material: original/expected-final participant placements, initiator action/distance delta, Recon contribution identities, added Acceleration amount/guard, created trail IDs and supported passive adjustment guards. It is not an installable old world.
- Live HP/status/held bonuses/terrain remain in their existing owners. Receipts and old values are immutable history/validation, not another live damage total. Build lookup indexes from existing state if useful; do not persist another owner.
- Recon contributions from different cancelable moves require distinct live identities. Same-source grants can coexist as individually removable entries without stacking effective magnitude. Repeated contact within one move is deduplicated. Ordinary HuntersMark same-source reapplication retains its existing magnitude/time replacement behavior.
- Enemy/automatic movement executes trait rules but has no player undo. Grants without an undo-capable player command use ordinary producer identity/reapplication; do not invent a positive player command token.
- Usage evidence is immutable data in the existing command transition, conceptually `MovementEffectUse(source IDs/originating move, consumer, kind, existing action/damage origin)`. Derive it from actual execution evaluation, never View claims, previews or guesses from final HP. It is not another VFX event stream.
- Removing a checkpoint commits the move; it does not remove its surviving effects. Historical effect provenance may outlive the checkpoint. Undo never resurrects an expired/cleared effect.

## 5. Forward trait behavior

### 5.1 Position Swap

A normalized Move path can end at an ally. Admit a living field holder with Position Swap and a distinct living same-side field passenger; both must meet current single-cell support. Keep existing faction/action/AI admission and real path allowance. Intermediate cells retain normal traversal/Leap rules.

Capture passenger identity/placement in the movement context. Occupant replacement is stale, not permission to swap with whoever is there now. Validate both final footprints/terrain/structures while treating the pair atomically. Never publish a transient overlap.

At the final step, the initiator occupies the passenger's cell; the passenger moves forcibly to the initiator's original cell. If intermediate effects invalidate the exchange, use the existing accepted-prefix/stop contract and leave the passenger untouched. Known invalidity before execution rejects without spending action.

Apply endpoint arrivals after both placements are in the isolated projection; terminal outcomes obey existing short-circuit ordering. Passenger displacement has endpoints, not traversal of the initiator's entire route. Reconcile presence against the simultaneous positions.

The passenger's older checkpoint, if any, is invalidated by forced relocation. Undoing the exchange does not resurrect that old token. The initiator alone owns the exchange query/token.

### 5.2 Recon Squad

Gather living hostile field recipients in the orthogonal cross around the origin and actually entered cells (P03). Multiple qualifying contacts grant once per move; unexecuted path suffixes grant nothing.

Starting-cell scan occurs after admission immediately before the first actual step. Each reached-cell scan follows existing step arrival hazards/reactions, provided the actor is still living/on-field and battle continues. Damage before a grant does not retroactively use it; later damage does. A move with irreversible effects still executes forward trait behavior but receives no cancelable checkpoint.

Recon contributes10% to the shared received-damage family. Reuse the receiving pipeline, not another Scouted repository or multiplier. It has no side-end countdown: remove on recipient death, reserve return or battle end. Producer death/return does not remove effects already installed on other recipients.

## 6. Reversible changes and barriers

| Change | Inverse and guard |
| --- | --- |
| Initiator placement/action/distance | Restore origin and pre-move opportunity; subtract only this move's recorded voluntary distance. Preserve other statistics. |
| Passenger placement | Restore original placement/facing only; its action/distance were not spent. |
| Recon | Remove only this move's live contribution identities; preserve earlier grants, other holders/traits and independent recipient HP/state. |
| Acceleration | Subtract the actual added held amount; reproject via existing owner. Later consumption/replacement of that guarded amount commits. No second held total. |
| Ground/Dust trails | Deactivate only created source IDs using existing successor/event rules. Preserve overlaps. Never rewind source allocation, RNG, command number or query revision. |
| StationaryFire moved flag | Restore the prior flag only if its guarded version was not consumed/changed; do not reset unrelated counters. |
| Cohesion/SoloAction consequences | Record bounded movement-caused source-budget/eligibility changes and recipient IDs. Validate these specific states have not been consumed/altered, restore the contribution, then use existing reconciliation against the candidate world. Never refund spent unrelated shields or restore an entire recipient snapshot. |

Compute inverses against current authoritative facts. Whole facts may guard unchanged participants, but must never overwrite later HP/items/XP/status/Run growth. Keep conservative invalidation for material changes to position participants; Recon-only recipients use contribution-specific guards.

Other changes remain barriers: damage/recovery, death/fall, item/soul/resource acquisition, random outcome or hidden-information revelation, unrelated environment mutations and effects without a validated inverse. Account for every changed fact, not just recognized event names. Explicitly supported passive adjustments are not a blanket exception for all trait events.

Undo is field-to-field restoration, not reserve return. Do not trigger return reset because an event is named Returned. Do not rerun entry hazards, create trails, grant Recon/Acceleration, collect objects or execute another action. Account for candidate presence normalization; if it introduces unsupported HP/budget effects, reject without mutation.

## 7. Later execution commits earlier movement

Install locks atomically with the consuming action, including shield-all-absorbed outcomes. Rejected action/failed preparation installs neither gameplay nor lock. Preview and Sacrifice planning may collect speculative receipts only inside their discarded projections.

| Use | Authoritative boundary |
| --- | --- |
| Recon damage increase | Actual recipient's numeric evaluation after interception, before items/shields/HP. |
| Acceleration | Real attack scope's captured positive held contribution used in a delivered numeric hit. Reuse AttackTraitScope; do not lend the parent's reserved gain to a child again. |
| Packed Ground | Successful departure uses selected active pre-action start-cell ground. Even a short route counts. Queries, rejected/no-step moves and ground newly created by that same departure do not. |
| Dust range | An attack actually starts using a positive selected screen range reduction. Target distance need not be in the removed outer band. Queries/canceled-before-start attacks do not count. |
| Dust defense | Actual incoming calculation incorporates positive eligible reduction; preserve current supported damage kinds. |
| Exchange participant | Either participant's subsequent actual movement/attack, including a real reaction attack, invalidates the exchange. Earlier actions do not. |
| Passive guarded effect | Subsequent consumption/modification of recorded shield/presence contributions invalidates through the same checkpoint owner. |

Positive contribution included in a real numeric hit counts even if later rounding/armor/items/shields erase its marginal HP difference. Zero input with no delivered numeric damage is not fabricated use. Instant defeat/falling do not use the receiving multiplier; existing action/participant barriers still apply.

The same calculation returns the value and selected-source detail. Current Packed Ground emits RangeChanged only when the route exceeds the base allowance; that event alone cannot implement U05 for short routes. Capture pre-action departure use separately in the same execution boundary. Current Dust incoming selection keeps one maximum trail with an ID tie-break; preserve numerical reduction once, but retain all equal maximum source IDs for P01 cancellation evidence instead of applying the reduction multiple times. Never infer use from trait possession, final HP difference, status presence or animation. Preserve detail through exposure maxima, terrain maxima and captured Acceleration. All tied maxima lock their receipts (P01); suppressed lower entries are not used. This prevents sequential cancellation of every interchangeable source after an action depended on the aggregate.

If B uses A's ground, A remains committed even after B cancels its own move. Locks never reopen. Unrelated actions without participant/source dependencies preserve A's checkpoint. Turn/lifecycle changes clear old checkpoints as today.

## 8. Shared received-damage increase and lifetime

For active trait-origin entries on one recipient:

`effectivePercent = sum(max(active percentages within each TraitKey))`

Recon10 + Recon10 =10; Recon10 + Recon20 =20; Recon10 + HuntersMark30 =40. Another holder or callback RuleKey is not another trait group. Qualify producer identity by EffectOwnerKind; this scope adds no Item/Artifact gameplay producers.

Check both group summation and the wide integer-floor damage formula for overflow before proposing state installation; apply the formula once at the actual numeric recipient. Timed HuntersMark source replacement and clocks remain. Recon uses explicit `UntilReserveReturnOrDeath` lifetime; battle cleanup removes both kinds. No zero/int.MaxValue permanent-duration sentinel or extra turn runner.

Expose effective total, per-source magnitude and explicit lifetime kind through immutable status/read facts. `MaximumIncreasePercent` no longer truthfully describes the total; migrate to an effective-total name and update Core consumers/tests and the View handoff. Keep a clearly documented zero-storage compatibility alias only if a real downstream compile dependency requires it.

Damage evaluators return the effective value and participating sources together. MovementContextCalculator must compare the new effective total instead of global maximum, while continuing to ignore duration-only changes that do not affect movement damage. Full transaction validation still checks complete expected facts.

## 9. Return reset

U11 changes existing tested preservation of Acceleration/Intimidation across reserve return. Apply on actual entry into Hand/EnemyWaiting, never field-to-field move/swap/UndoMove. A Main remaining in Field at night end has not returned.

| State | Return behavior |
| --- | --- |
| Applied temporary attack/defense changes, restrictions, exposure/Recon, recipient shields | Remove temporary recipient effects and reproject stats from surviving authoritative sources. |
| Acceleration held attack, Regeneration cadence, temporary per-field producer flags | Reset temporary progress; preserve authored configuration. |
| Derived position/presence/terrain contributions | Remove field eligibility with existing reconciliation; no duplicate stat bonus. |
| Item temporary modifiers, temporary stat boosts, held transient modifiers | The temporary-effect cleanup itself clears transient modifiers and normalizes HP/max HP without inventing healing/damage. It must not independently discard/refund equipment or mutate inventory. The surrounding existing return flow still owns equipment handling: manual Hand return continues RefundReturn (one item refunded, slot cleared); night/lifecycle handling retains its existing equipment policy. Do not reuse battle-end item discard wholesale or refund an already consumed item. |
| Hunter permanent attack, owned growth/level/XP, Run state | Preserve and reproject from the persistent source. |
| Stage reward caps, turn action/trigger allowances, shield reset/consumption history, RNG/allocation | Preserve anti-abuse/progression history. These are not buffs to reset for another benefit, even if a visible shield is removed. |
| Hunter target / reaction bookkeeping | Reconcile through existing owners without resetting successful rewards or refreshing consumed same-turn allowances. |
| Terrain or debuffs on other entities | Their own lifetimes govern. Returning the producer does not return every effect it previously created. |

Do not call CombatTemporaryStateCleanup.Clear unchanged: it also discards items and clears Hunter stage state for death/battle completion. Reuse compatible subrules while keeping existing item/health/trait owners. Failed return must not partially clear effects. EnemyWaiting and Hand use the same recipient-reset policy.

## 10. Atomic execution and freshness

Prepare final characters, environment, cancellation state, resources/field, result and revision in the existing command transaction before installation. Extend allowed payloads/validation, not the prepare/install order or Job execution authority.

1. Admit/normalize using battle/turn/actor/context guards.
2. Calculate isolated projections; attribute movement-created changes and actual usage in deterministic execution order.
3. Account for all forward changes as reversible, observational or a barrier. Invalidate old dependencies before adding an eligible new checkpoint.
4. For UndoMove, validate token, participants, both restoration cells and exact owned effect guards. Compute bounded inverses against current facts; validate simultaneous occupancy while excluding the pair's own expected positions.
5. Prepare all stores and result/revision; install once. Stale source, occupied destination or late failure installs nothing.

Temporary third-party occupancy may make undo unavailable without destroying an otherwise valid checkpoint; requery after the blocker leaves. Genuine participant actions/use locks never reopen. QueryRevision must change whenever eligibility-affecting metadata changes, even with no HP change. Keep source revision, battle/turn identity and move token distinct.

Replace the one-character/one-event result restriction with validation of the complete bounded participant/inverse set. Keep initiator metadata; add immutable participant restoration details and typed effect-removal evidence. Never refund passenger distance/actions. Ordinary commands retain their existing monotonic distance validation.

Direct writes still invalidate affected receipts using existing typed change evidence. Usage validation must bind each declared source to captured authoritative inputs and an actually executed action/damage boundary. A caller cannot mark arbitrary receipts used or restore arbitrary state. Buffer does not reimplement gameplay formulas.

## 11. View, registration and ownership

This is Core/InGame/Buffer work plus a handoff, not View implementation. Keep ActionAvailability queries, existing UndoMove actor/token submission, source-revision checks and Flow/Job processing. Optional immutable participant details support UI without giving View rollback authority.

Preserve common TraitActivationSnapshot selected by `(TraitKey, TraitActivationKind)`: source, targets/cells, applied outcome and result origin. Use existing forced-movement cause/path contracts and extend only missing exchange/restoration data. Complete character/status/environment facts are authoritative. Undo is not a new forward activation/attack; summaries must not replay facts twice. No new per-trait Buffer or external event bus.

View handoff must cover paired exchange/return playback, exposure total and source/lifetime detail, Recon removal on undo/return, Acceleration stat refresh, trail-source deactivation and refreshed availability after another actor uses an effect. Cancel/rebind/detach never executes gameplay or ticks effects.

Add unused stable TraitKeys and existing authoring/data/spec/definition/factory/runtime-description plumbing. Never reuse retired IDs. Position Swap is Heavy; Recon is Rider. Preserve production acquisition pools unless explicitly included by the later approved plan; do not invent missing enhanced content. No root service/per-effect Framework registration.

## 12. Change locality and source owners

Prefixes: C = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator`; B = `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems`.

| Change | Main owners | Necessary understanding / verification |
| --- | --- | --- |
| New trait trigger/settings | Existing DB authoring/data, spec/definition/factories, movement application | Eligibility, target/configuration and receipt contract; normal registration edits are legitimate. |
| Exchange and undo | MovementRouteValidator; CombatMovementCalculator/MovementProjection; MovementUndoCalculator/Policy/State/Checkpoint/Result | Both footprints, action use, arrival ordering, snapshots; movement/preview/undo/occupancy tests. |
| Recon and grouping | AppliedStatus/StatusEffectSet, WeaknessExposureRules, AppliedStatusRules, StatusEffectReadModel/snapshots | Grouping, lifetime and removable contribution; damage, copy/equality, expiry/read tests. |
| Usage locks | Actual damage/range/start-move boundaries, AttackTraitScope, CombatChangeEvidenceReader | Existing selected contributions/origins; no duplicated formula; shield/interception/reaction/order tests. |
| Trails/Acceleration inverse | TraitTrailCalculator, CommandEnvironmentState, existing held-state owner | Exact source IDs/gain and successor validation; overlap/consumption/allocation tests. |
| Return reset | CombatPlacementCalculator, compatible cleanup subrules, trait/item/health owners | Temporary versus persistent/progression state; return/night/redeployment/Main/growth tests. |
| Publication | CharacterBuffer, CombatState store, BattleTransactions and existing publisher | Complete expected/final facts/revision; native stale/late-failure/replay tests. |

SRP: producers own triggers/targets, effect rules own bounded calculations/inverses, checkpoint owner owns cancellation, Buffer installs, View renders. OCP/ISP: add only needed typed reversible families, not universal hooks on every trait. LSP: inverses preserve unrelated facts and reject stale dependencies. DIP/FW: no Core-to-Unity or Framework-to-Systems dependency. SSOT: live values remain with existing owners; receipts are immutable history/guards.

Judge locality through changes: another Recon-like producer reuses grouping/lifetime/usage; changing GroundCompaction magnitude does not require Recon internals; a new reversible family legitimately adds its adapter, validation and tests. File/interface count is not evidence of improvement.

## 13. Required verification for the later plan

No implementation tests run during this design. Add meaningful RED cases before fixes, then full Core/CoreIntegration gates. View acceptance is separate.

1. Ordinary undo still restores exact action/distance once, rejects stale/foreign tokens and preserves independent checkpoints/query freshness.
2. Swap preview/execute agree; ally endpoint works, enemies/unsupported footprints/blocked reverse positions reject. Late failure never half-swaps. Passenger facing/action/distance remain unchanged.
3. Participant later move/attack locks; preview/rejection does not. Canceling the consuming action never reopens old checkpoints. Forced passenger relocation does not resurrect older tokens.
4. Recon starting-cell/path cross, interrupted-prefix and deduplication are deterministic. No forced/undo grant. No side-end tick; recipient return/death/battle clears, producer departure does not.
5. Same-trait max/cross-trait sum, weaker/shorter timed-source replacement, expiry fallback, duplicate same-move suppression and source-specific undo preserve unrelated entries.
6. Actual-recipient damage attributes use once through Sacrifice, multi-hit, reactions, mines/reflection. Shield-all-absorbed locks; simulation does not. Ties lock tied sources; suppressed lower sources remain unused.
7. Acceleration inverse removes only the move gain, preserves prior held bonus and respects attack-scope reservations/own-attack commitment. Old snapshots stay immutable.
8. Packed Ground departure uses pre-action sources, including short routes; no lock from query/rejection/self-emitted new ground. Allocation IDs never rewind.
9. Dust actual restricted-range attack and eligible incoming reduction lock; queries/inactive or suppressed sources/canceled-before-start attacks do not. Interception uses the real recipient's screen.
10. Overlaps and unrelated later state survive undo. Passive shield/presence inverse never refunds consumed/unrelated budgets. Unsupported effects explicitly block cancellation.
11. Hand/EnemyWaiting clears temporary effects/Acceleration, preserving Hunter growth and reward/trigger/reset history. Field Main does not reset solely at night end; swap/undo never runs reserve cleanup. Max-HP normalization creates no fake heal/damage credit. Manual Hand return still refunds equipped items exactly once, including no partial cleanup/refund if inventory preparation fails; night equipment handling retains its own policy.
12. Native publication rejects stale participant/source/receipt and late failure with every store/result/revision unchanged. Handled command replay is a no-op; consumed token rejects. Direct writes refresh eligibility.
13. Common activation provenance and complete playback facts describe exchange/grant/inverse exactly once; no phantom activation during undo.
14. Core is Unity-free; new files have unique .meta GUIDs; authoring copies retain configured values and normal/enhanced independence. Run DataBaseAssetTests if assets change. Retired/deferred features remain unchanged.

## 14. Review and next step

Return reset, cross-trait received-damage addition and reversible movement effects materially extend prior behavior; approval and regression coverage must explicitly include them. The historical statement that only safe single-actor movement is reversible no longer describes the proposed scope.

No claim is made that existing View, production content or whole-game presentation supports this design. StageKey2101 and native growth serialization remain separately reported; no suppression/repair under this task. No commit/push is authorized for this design turn.

Independent builder check completed on 2026-10-31. It raised one material ambiguity: whether preserving equipment inside temporary-effect cleanup would replace the existing manual Hand-return refund. Source inspection of CombatPlacementCalculator.Return and ItemEquipmentCalculator.RefundReturn resolves it without a new gameplay decision: preserve the existing refund/slot clearing, and prevent the new cleanup helper from performing duplicate equipment work. Section9 and acceptance11 now state this explicitly. No other planning-blocking question was reported by that review; this is not implementation verification.

On 2026-10-31 the user approved the design including P01-P07 and requested a phased implementation plan, explicitly excluding implementation. The equipment wording means that an equipped item is returned to inventory and its equipment slot cleared during the existing manual Hand-return flow; this is not a gold refund or an additional reward. Temporary-effect cleanup must not duplicate that operation. The linked plan records tasks and stop boundaries; implementation still requires a later instruction.
