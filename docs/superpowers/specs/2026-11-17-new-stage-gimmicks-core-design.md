# New Stage Gimmicks: Core, Data, and Buffer Design

- Date: 2026-11-17
- Status: Approved for implementation planning on 2026-11-17; implementation has not started
- Code baseline inspected: `0515527b1`
- Scope: Volcanic Smog, Lava Chain, Devil's Altar, Contract of Equivalent Exchange, Meteor, and Lava Eruption.
- Revisions after approval (2026-11-17): the [implementation plan](../plans/2026-11-17-new-stage-gimmicks-core-implementation-plan.md) supersedes parts of this document. Its Task 9 replaces the Contract player choice with automatic random settlement (sections 4.1, 4.4, 5.4, 7.1 steps 7 and 9, and 8.4). Its Task 6 replaces the Lava Eruption mechanic with the slide 21 revision (sections 4.2, 4.4, 5.6, 6, and 7.1 step 4). Volcanic Smog is on hold until the planners confirm it (plan Phase 8).

## 1. Purpose, authority, and boundaries

Define the six mechanics and their integration contracts before adding them. Reduce later changes to terrain, traits, and items by settling shared timing, occupancy, damage, persistent-stat, and choice boundaries first.

Sources:

- The supplied English conversion, `C:/Users/권천명/Downloads/projectl_new_gimmicks_design_en.md`.
- The updated PowerPoint, `C:/Users/권천명/Dropbox/Project L/기획 문서/기믹 기획서.pptx`. Slides 7-13 were inspected as rendered images. They replace the older Lava Chain interpretation. In this version: Smog is slide 6, Altar 16-17, Contract 18-19, Meteor 20, and Eruption 21.
- The user's subsequent decisions in the design discussion, including the final corrections to Lava Chain and SO authoring. These override conflicting source examples.
- Current repository code and Framework contracts. Older feature design documents are not evidence of current implementation.

The user approved the recommended edge cases and execution order after their Korean walkthrough on 2026-11-17 and requested an implementation plan. Earlier proposals are now adopted design rules. Section 12 preserves the review/approval history. This approval does not authorize implementation or commit/push by itself.

Included: pure Core rules; stage SO authoring and immutable input conversion; combat and Run state ownership; input, snapshot, event, and forecast contracts; regression requirements; View handoff requirements.

Excluded: View/UI/animation implementation, scene wiring, production asset activation, the other four planned terrain types, Cactus Cannon's trait, item redesign, a generic scripting engine, and reimplementation of existing gimmicks. No commit or push is part of this work. The implementation Task plan follows design approval.

## 2. Approaches and chosen direction

### 2.1 Authoring: user-selected approach

| Approach | Tradeoff | Decision |
|---|---|---|
| Reusable gimmick definition SOs referenced by stage placements | Shared tuning across stages, but adds definition assets and another authoring relationship | Considered, then explicitly rejected in favor of the next option |
| Settings and placements inside each stage SO | Follows existing authoring; each stage owns its tuning | **Selected** |

Use the existing flow: **stage SO fields -> authoring conversion -> fresh immutable pure Data -> Core specs**. There are no new standalone gimmick definition SO assets. Preserve the existing configuration of Tornado, Railway, Haboob, and Rockfall.

### 2.2 Runtime integration: adopted approach

Extend existing registered gimmick rules and the current transition publication pipeline. Keep calculations stateless between calls. Add narrow integration points where the new mechanics actually require them: damage processing, occupancy queries, end-of-turn settlement, and combat choice input.

A separate scheduler, damage engine, permanent-stat store, or View-owned timer would duplicate existing ownership. No such replacement is proposed. Adding a rule to composition is a normal dependency change, not a failure of feature independence.

## 3. Current owning contracts

Core paths below are relative to `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/`.

| Owning code | Verified current behavior | Design implication |
|---|---|---|
| `Application/Combat/StageGimmicks/StageGimmickComposition.cs` and `IStageGimmickRule.cs` | Registered rules, initialization, forecasts, sequential projected transitions; stops on stage completion | Extend the existing composition and make new boundary ordering explicit |
| `Application/Combat/CombatTurnCalculator.cs` | Each accepted player/enemy end increments `CompletedControllerTurnCount`; gimmicks run before existing end cleanup | Reuse this counter; add the post-cleanup choice/interaction boundary explicitly |
| `Application/Combat/CombatDamageApplication.cs` | Shared numeric damage, incoming effects, exposure, item protection, shields, HP changes, Reconstruction, and post-hit items; separate instant defeat and pillar-crush paths | Add Smog at the correct stages; reuse health evidence and death settlement |
| `Domain/CombatEffects/Rules/PeriodicStatusRules.cs`, `Contracts/CombatEffects/FireStatus.cs` | Existing Fire deals 2 per affected-side start; ordinary application lasts 2 ticks and reapplication replaces the status | Extend duration input; do not add a second Burn implementation |
| `Domain/Movement/EnvironmentTerrainRules.cs`, `Application/Combat/AttackForcedMovement.cs` | Shared terrain entry/knockback checks; forced movement uses current projected occupancy and entry effects | Integrate new blockers at shared queries, not in each gimmick's private movement copy |
| `Contracts/Roster/MainCharacterPersistentState.cs` | Main's persistent HP/maximum HP/attack/defense | Preserve this owner for Main's lasting changes |
| `Contracts/Roster/OwnedCharacterRecord.cs`, `Application/Roster/CharacterBaselineStatsCalculator.cs` | Sub baselines derive from level; owned records already carry distinct persistent contributions such as Hunter attack | Add only the missing Sub stat adjustment, preserving growth and existing contribution ownership |
| `CombatSystem/CombatCommandService.cs`, `CombatAdvanceService.cs`, `CombatTransitionPublisher.cs` under CoreSystem | View commands are converted to Core requests; automatic advancement requires `Automatic`; transitions publish through the existing battle apply boundary | Add a choice command and waiting phase without a direct UI-to-state write |
| `BufferSystems/Combat/CombatStateBuffer.cs` | Validates preparation and gimmick transition evidence | Extend validation together with state and events; do not bypass it |

These are source-inspection findings, not runtime verification. No Unity execution or compilation was performed for this design.

## 4. Confirmed common rules

### 4.1 Timing and recipients

- One gimmick timing unit is **one faction turn end**. Player end adds 1; enemy end adds another 1. It is not a full player/enemy round. Existing gimmick timing is unchanged.
- Smog stack gain and tile interactions are checked on the affected character's own faction end, using its current position at the applicable settlement boundary.
- Environmental effects of Smog, Chain, Meteor, and Eruption affect player and enemy characters.
- Altar and Contract activate only for **owned player Main/Sub characters**. Enemies neither activate nor consume them. No allied summoning feature is assumed or added.
- Altar/Contract require the character to remain on the activation center at settlement. An earlier arrival followed by departure does not activate them.
- Turns do not advance while gimmick effects are being resolved. A pending player reward choice also prevents the next faction from starting.
- Automatic return to hand after night is no longer a gameplay feature. Do not restore it or make these gimmicks depend on a night-return/redeployment cycle. Existing deployment-only manual return and movement undo retain their own contracts.
- Meteor's warning is included in its 4-unit cycle: warning at 2, impact at 4, warning at 6, impact at 8. Eruption: warning at 1, activation at 3, warning at 4, activation at 6.
- Chain selects and exposes its heading one faction end before activation. It retains that heading at activation; newly appearing obstacles may shorten the movement but do not cause a hidden reroll.

### 4.2 Numeric damage and health

- Meteor impact uses 60% of the target's final maximum HP at impact. Eruption pillar appearance uses 30% of final maximum HP.
- Both are numeric **environment damage**, processed through the existing applicable incoming modifiers, items, and shields. They do not copy Rockfall's direct-HP-loss bypass.
- Smog doubles all numeric damage, including attacks, reflection, environment damage, and damage over time. Direct HP costs/loss, normalization, and instant defeat are excluded.
- Smog amplification occurs before item protection and shield absorption.
- A Smog execution requires actual HP loss while the target is inside the current Smog. If the surviving HP is strictly below 10% of maximum HP, execute before post-hit healing items. Entering Smog at low HP or losing only shield budget does not qualify.
- Eruption relocation failure after its damage is a spatial crush: the affected survivor dies and **Reconstruction does not save it**. Do not silently apply this exception to every new instant-defeat effect.

### 4.3 Shared Fire

- Meteor applies the existing Fire behavior for **3 affected-side starts**, dealing 2 each time. The environmental cycle counter and Fire lifetime counter are different clocks.
- Ordinary existing Fire applications retain their default 2 ticks.
- Burn is a gameplay name for the existing Fire status, not a second status store.

### 4.4 SO tuning

Expose gameplay tuning in each stage SO: cycle counts, warning lead, stack threshold, damage/execute/shield percentages, status durations, remaining-area lifetimes, meteor counts, and exchange amounts. The following numbers are defaults, not hidden rule constants.

| Mechanic | Confirmed defaults |
|---|---|
| Smog | 3 adjacent horizontal rows; relocation 5; stack threshold/cap 3; received damage 200%; execution below 10% maximum HP |
| Chain | Cycle 3; warning lead 1; push distance 1 |
| Altar | Maximum HP minimum 1; attack/defense minimum 0 |
| Health Contract | Maximum HP -5; choose attack +3 or defense +3 |
| Attack Contract | Attack -3; choose maximum HP +5 or defense +3 |
| Defense Contract | Defense -3; choose maximum HP +5 or attack +3 |
| Meteor | Cycle 4; warning lead 2; 2-4 impacts; default footprint 3x3; maximum-HP damage 60%; Fire 3 own starts; flames last 4 faction ends |
| Eruption | Superseded by plan Task 6 SO tuning: cycle 3; warning lead 2; 1-3 crosses; 7x7 landing square; HP loss 10% maximum HP; Attack 200% for 1 own turn |

Keep fixed mechanic topology distinct from tunable amounts: Altar has a center, four cardinal stairs, and four diagonal pillars; Eruption has a center and four cardinal pillars. Do not invent arbitrary topology editing in this feature. Existing shared Fire damage remains owned by the existing Fire rule; do not duplicate that constant in six authoring classes.

## 5. Confirmed per-gimmick behavior

### 5.1 Volcanic Smog

> On hold (2026-11-17): the planners have not confirmed Smog. Plan Phase 8 implements it only after confirmation, and the rule below may change.

- Present from battle initialization as a full-width horizontal band of three adjacent rows.
- Relocates every 5 faction ends.
- If relocation and stacking occur at the same boundary, **relocate first**, then grant stacks using the new position.
- Living field characters of the ending faction inside the band gain one stack. Partial stacks remain after leaving; cap at 3.
- Reaching the threshold activates the stage-long received-damage multiplier. Leaving the band does not remove it.
- The low-HP execution depends on current location, independently of the stack count.

### 5.2 Lava Chain

The final user-confirmed interpretation supersedes all earlier wall-wide wave/body-rotation descriptions.

1. Between activations there is **one head cell**. That cell occupies the map and cannot be entered by units.
2. One faction end before activation, consider the head's eight grid directions and select uniformly among legal directions. Publish and retain that heading. This is not restricted to the reverse hemisphere of the previous direction.
3. The head advances in the selected direction toward the board edge or the first blocking obstacle. Its visited cells form an extending chain behind the head.
4. Contact pushes a character **perpendicular to travel**, never forward/backward. For northeast travel, candidates are northwest and southeast.
5. Compare the number of grid steps to the map boundary along those two perpendicular directions, measured from the contacted character. Try the nearer boundary side first. On a tie, try the **left side relative to travel** first. Internal obstacles do not redefine which side is geometrically outer; they affect whether that destination is legal.
6. Try one-cell displacement toward the outer side, then the inner side. If both fail, or the character cannot be knocked back, the result is instant defeat.
7. Resolve contact only while extending/advancing. Retraction has no additional hit or damage pass.
8. After movement and all resulting interactions finish, the extended chain retracts into the head at its final position. Start the next cycle from that position.

Walls, ordinary blocking pillars, Altar pillars, and Eruption pillars stop the head before their occupied cell. A map edge similarly limits the last head cell to an in-bounds position.

The resting head must be excluded by ordinary movement, placement, forced-movement destinations, and random relocation candidates. Do not store one head position in gimmick state and a second independently mutable position in an obstacle store.

The head and fixed Altar/Eruption pillars are not attack targets and do not block attack targeting or attack paths. Their blocking role applies to movement/landing and Chain travel only.

The slide 9 endpoint-death example is conditional: a character at the head destination survives if an allowed lateral destination exists. Being at the map-edge endpoint is not an unconditional death rule.

When Eruption and Chain activate on the same boundary, **create Eruption pillars and settle its displacement first**, then resolve Chain. The newly created pillars block Chain.

### 5.3 Devil's Altar

- A stationary 3x3 footprint: diagonal corners are impassable pillars; cardinal neighbors are traversable stairs; the center is the activation cell.
- Owned player Main/Sub only; activate on the player's end settlement when still at the center.
- Pool the character's persistent baseline **maximum HP, attack, and defense**, including level growth and prior Altar/Contract adjustments. Exclude soul, equipment, trait, and positional contributions.
- Randomly redistribute the entire pool, preserving its sum. Maximum HP is at least 1; attack and defense may be 0.
- Keep current HP, clamped to the newly computed final maximum HP. An increase in maximum HP does not heal.
- The result persists for the Run. Subsequent normal level growth still adds its normal increments.
- Each Altar is consumed once. Enemies do not consume it.
- Pillars are fixed gimmick obstacles, not attackable/toppling ordinary pillars.

### 5.4 Contract of Equivalent Exchange

> Superseded (2026-11-17): plan Task 9 settles each circle automatically with a random reward and has no player choice or turn gate. The text below is the earlier approved rule.

- A stationary one-cell circle, using the exchange defaults in section 4.4.
- Owned player Main/Sub only. There is no enemy or summoned-unit reward-selection logic in this feature.
- After other end effects are settled, present a choice only for a living, eligible character still on the circle and able to pay the cost.
- The player must choose one of the two rewards. There is no decline action. Standing there at the end is the commitment to use the circle.
- Until a valid selection is accepted, do not charge the cost, grant the reward, or consume the circle. The next faction cannot start.
- On acceptance, cost, reward, HP normalization, persistent state, and circle consumption are one transition.
- A circle is usable once per stage. An unaffordable attempt does not consume it.
- Stat changes last for the Run; later normal growth remains effective. Maximum-HP changes retain current HP subject to the new maximum.

### 5.5 Meteor

- Every 4 faction ends, 2-4 non-overlapping meteor footprints activate. Each default footprint is 3x3.
- All footprint cells must be on terrain characters can enter. Unit occupancy is allowed and determines victims; it is not a placement rejection.
- Warn the full footprint 2 timing units before impact.
- Resolve 60%-maximum-HP environment damage and apply Fire for 3 affected-side starts to surviving hit characters.
- Leave flames with an independent SO lifetime, default 4 faction ends.
- Flames apply/refresh Fire both on entry and when the affected character ends its own faction turn while inside. Entry includes ordinary movement, forced movement, and legal placement.
- Do not change the locations or hide extra impact cells behind a center-only warning.

### 5.6 Lava Eruption

> Superseded (2026-11-17): plan Task 6 governs Eruption. It uses one to three crosses, launches targets inside a 7x7 square, applies a direct 10% HP loss, and doubles Attack for the target's next own turn. These numbers are SO defaults listed in that task. It has no pillars, center benefits, shield, or spatial crush. The text below is the earlier approved rule.

- One center cell with four cardinal pillar cells. The center is not itself a pillar or an appearance-damage cell.
- Warn the full five-cell cross 2 timing units before the 3-unit activation.
- Select only complete valid footprints. Pillar cells cannot be on cliffs, walls, platforms, or existing pillars; the center must support the character's placement. Unit occupancy is handled by the effect rather than rejected at selection.
- A character on an appearing pillar cell takes 30%-maximum-HP environment damage. If still alive, relocate it to a randomly selected legal standing cell.
- If no legal relocation exists after damage, spatial crush kills the survivor without Reconstruction. Do not cancel the eruption or omit just that pillar.
- A character at the center when it appears, or entering the center later, is eligible for the center benefits.
- While in that center, its attacks apply Fire. Leaving or Eruption expiry ends this attack benefit; already applied Fire has its own lifetime.
- Grant a shield of 20% of maximum HP once per character per Eruption instance. Re-entry neither recharges nor duplicates that shield.
- The shield remains after leaving the center, but is removed when its source Eruption expires.
- The center and four fixed, non-toppling pillars expire together; SO lifetime defaults to 3 faction ends.

## 6. Approved completion of mechanical edge cases

These defaults were approved after the design walkthrough. Return/redeployment compatibility applies only to supported existing paths; it is not a requirement to recreate the removed night-return feature.

| Topic | Approved rule |
|---|---|
| Percent arithmetic | Use checked integer arithmetic and floor the percentage once at its declared stage. Compare Smog's strict threshold by cross multiplication; do not round the threshold first. Zero numeric damage cannot trigger Smog execution. |
| Existing vulnerability plus Smog | Evaluate existing vulnerability normally, then apply Smog's multiplier once before item protection. Do not represent Smog as another additive WeaknessExposure contribution. |
| Fire reapplication | One Fire status. Keep `max(remaining ticks, incoming duration)`; a shorter application cannot shorten a longer one. Use the incoming origin if it sets or ties the resulting lifetime, otherwise retain the existing origin. Existing two-tick-only cases keep their behavior. |
| Eruption attack Fire | Default duration 3 own starts, SO-configurable. Use the existing attack-family classification: ordinary, extra, reaction, and player-skill attacks; exclude reflected damage and damage over time. Apply on a resolved hit against a surviving target, even if a shield absorbed the HP damage. |
| Same-character multiple occupied cells | One contact application per head step, meteor instance, or Eruption instance, not one per occupied cell/pillar. A later genuine Chain contact may act again; no once-per-activation immunity is introduced. Current authored Unit assets inspected are 1x1; this deduplication clarifies the existing general footprint contract rather than adding multi-cell content. |
| Static Altar after use | Disable its interaction; retain its footprint, stairs, and pillars. |
| Altar random allocation | Uniform over valid integer triples satisfying configured minima and exact pool conservation. Do not use random display-stat multipliers or independently round three percentages. |
| Contract affordability | Use the same persistent baseline as Altar; the cost must leave maximum HP >= 1 and attack/defense >= 0. Temporary bonuses cannot fund a permanent cost. |
| Smog random placement | Use full in-bounds bands allowed by stage settings. Prefer a different eligible band when one exists; retain the only eligible band otherwise. Invalid initial authoring fails validation. |
| Remaining-area lifetime | Store an absolute expiry derived from the accepted activation boundary. Creation does not immediately consume one lifetime unit. Expire before resolving new effects at that boundary. |
| Meteor flame footprint | Same cells as the impact. Overlapping flames from different cycles retain distinct source/expiry facts, but never multiply one entry/end-boundary Fire application. |
| Insufficient meteor sites | Attempt the randomly selected count within 2-4 using deterministic candidate ordering. Use fewer only if at least the configured minimum fit; otherwise skip that cycle with explicit evidence and no partial impact. |
| Eruption count | One cross per activation by default. Do not add arbitrary multi-eruption scheduling as an implied feature. |
| Invalid warned footprint at activation | Revalidate against the latest projected terrain/obstacles. Cancel the affected whole footprint with an event; never silently retarget to unannounced cells. For Meteors, other valid warned footprints may still resolve. |
| Random relocation | Deterministically ordered, unoccupied legal standing anchors for the target's footprint; exclude current/future pillar cells, the head cell, and previously reserved destinations. Draw using the existing environment RNG. Dead targets are not relocated. |
| Eruption victim capture | Capture distinct living characters overlapping any planned pillar cell before applying the first appearance hit. Process that stable set by entity ID, at most once per character, skipping any who have already died. Do not discover new victims by rescanning after relocation. Reserve all upcoming pillar cells against every relocation. |
| Chain direction with no space | At the warning boundary, a legal heading must allow at least one head step before a hard blocker. If none exist, commit a no-movement activation for that cycle. If a committed heading becomes immediately blocked before activation, likewise retain the head and advance the schedule. Neither case rerolls during activation. |
| Chain path | Traverse ordered grid steps, stopping before the first hard blocker. Perpendicular left is `(-dy, dx)` with logical +Y north; right is its negative. Map-boundary distance ignores internal obstacles; actual landing validation does not. |
| Chain occupancy during resolution | The current/next head cell and the already extended chain cells are reserved against unit landing until retraction. Check the full candidate unit footprint against these new gimmick reservations and fixed gimmick blockers. This supplements existing admission; it does not globally rewrite legacy anchor-only forced-movement rules for unrelated terrain. Only query-local projections carry transient reservations. |
| Chain retraction | Intermediate chain cells are transient resolution/playback facts. Only the final head cell remains occupied after publication. Retraction cannot refresh effects or generate a second collision pass. |
| Reconstruction after failed Chain push | Reuse ordinary instant-defeat behavior. If any contacted target remains alive and undisplaced after this resolution, stop the head before its occupied cells. This includes a knockback-immune target saved by Reconstruction even when lateral cells are open. Do not override immunity, overlap the survivor, or apply a second execution. |
| Existing return compatibility and stage lifetime | Smog stacks and its unlocked multiplier last until battle end. If a supported return path is applicable, it must not shorten this lifetime or reset an Eruption's once-granted identity. Fire follows existing cleanup. No automatic night return or new combat redeployment path is introduced. Current manual Return is deployment-only and rejects combat-phase requests. |
| Shield combinations | Use existing source-distinct shield budgets and absorption order. Different Eruption instances can contribute separate shields; same-source re-entry cannot. Do not duplicate shield amounts in gimmick state. |
| Persistent edits and undo | Altar/Contract are end-boundary transactions, not reversible movement side effects. Movement undo involving Fire application or a center shield must use the existing movement-effect receipt rules and restore the source grant entitlement together with the reversible shield grant. |

## 7. Approved execution order and state ownership

### 7.1 One accepted end boundary

Retain the current turn counter and the existing gimmicks' relative execution order. Make additional integration stages explicit rather than relying on constructor order or View callbacks.

1. Accept faction end and advance its existing count once.
2. Expire due new environmental areas and their source-bound benefits.
3. Run the existing Tornado -> Railway -> Haboob -> Rockfall order unchanged.
4. Settle due Smog relocation; then new Meteor activations; then Eruption appearance/damage/relocation; then Chain movement. In particular, Eruption is before Chain.
5. For the ending faction's survivors at their resulting positions, apply Smog stacks and flame end-boundary Fire refresh.
6. Run the existing end cleanup, including current trait/regeneration behavior.
7. Resolve eligible Altar uses, then form Contract offers from the surviving owned characters still on circles.
8. Prepare and publish any new warnings due at this boundary from the settled state, without activating their effects. Retain already committed warnings; previews never silently retarget them.
9. Publish `AwaitingGimmickChoice` if offers remain; otherwise continue the existing automatic transition to the next faction.

Stop later stages when the existing stage-completion contract says the battle has ended. Each individual multi-target activation must use the existing completion/health-evidence settlement boundary; do not award kills or end the stage halfway through an activation by adding a second custom settlement loop.

The user approved this total order after the walkthrough. It extends the earlier individual decisions for relocation-before-Smog-stacks, Eruption-before-Chain, and Contract-after-end-effects; it is an approved project decision rather than a claim that the source PPT specified every relationship.

### 7.2 State owners

| Fact | Owner | Consumers |
|---|---|---|
| Authored definitions/placements | Stage SO, converted to immutable battle inputs | Core preparation/rules |
| Turn count and environment RNG | Existing combat state facts | Existing and new gimmick rules |
| Smog band, head cell, warnings, active flames/eruptions, consumption, source grant identities | Typed entries under existing gimmick state | Occupancy/rules/forecast/read snapshots |
| Stack/multiplier and applied Fire | Existing character status facts with explicit lifetime | Damage/turn/cleanup/read model |
| Shield remaining budget | Existing source-tagged shield status | Shield absorption and source expiry |
| Main persistent stats | Existing Main persistent state | Baseline, combat projection, roster queries |
| Sub persistent stat adjustment | Owned Sub record extension | Same shared baseline calculation used for entry and roster queries |
| Pending Contract offers | Combat command state | Command validation, automatic gate, Buffer read model |
| Playback progress/input waiting | Existing View/Flow ownership | View only; no new Core animation clock |

Derive blocking cells from the owning gimmick state. A cached derived set is permitted only as immutable query work for a specific source stamp; it is not another mutable map. Do not turn the head into a fake destructible character or reuse ordinary toppling-pillar state for fixed gimmick pillars.

## 8. Core and Buffer contracts

### 8.1 Gimmick definitions, preparation, and occupancy

- Add the six kinds without renumbering existing public enum values. One definition section per kind may contain its authored placements; preserve the existing one-definition-per-kind composition contract.
- Add stage authoring sections, pure immutable Data, and Core specs through `BattleDefinitionAdapter`. Disabled/missing new sections preserve existing stage behavior. Validate enabled configuration before entering battle.
- The selected stage SO is the tuning source for that stage. New authoring fields define creation defaults; enabled pure Data/spec constructors receive explicit values instead of independently re-defaulting them. Battle inputs capture those values once; changing authoring does not mutate an active battle. Test fixtures supply their own values and are not another production tuning source.
- Initialize fixed Altar/Contract placements and Chain head occupancy before validating character placement. Initialize Smog at boundary zero; Meteor/Eruption begin inactive and follow their warning schedules.
- Each registered rule owns construction of its initial state, including its phase, shape, initial random choice, and schedule. Composition owns definition iteration, stable ID allocation, dispatch, aggregation, shared RNG publication, and cross-definition validation. Do not add per-kind initialization formulas to `StageGimmickComposition.Initialize`. Move its existing initialization branches into their existing rule owners when extending the rule contract, preserving legacy state values, ID allocation, and RNG consumption order. Railway remains a stateless initialization case; no new feature-specific factory/Bootstrap layer is required.
- Extend shared occupancy/query construction to expose resting head cells and fixed gimmick pillar cells. Every ordinary/forced placement consumer must use the same classification. Existing special movement rules still decide whether intermediate cells may be bypassed; no destination may overlap a blocker.
- The resting head and fixed gimmick pillars are untargetable and do not block attacks, as subsequently confirmed by the user. Do not reuse an ordinary wall/pillar classification that would accidentally add attack occlusion or toppling.

### 8.2 Damage and status integration

- Smog multiplication belongs in the common numeric damage application, after existing non-item incoming amplification and before item/shield protection. All numeric kinds must reach it exactly once.
- Add the execution check after actual HP loss and before post-hit healing. If ordinary lethal handling has already killed or reconstructed the target, do not execute the same hit a second time. Use the same health evidence, origin attribution, and death/reward settlement mechanisms as the initiating damage.
- Extend Fire application with an optional duration retaining default 2. New callers pass their configured duration. Do not change the shared tick damage or its own-side-start scheduling.
- Use explicit stage-long status lifetime/cleanup for Smog. Preserve applicable existing return cleanup with a narrow typed lifetime rule; do not disable cleanup for other statuses or add night-return gameplay. The observed manual return path is deployment-only, so post-night redeployment is not an acceptance scenario.
- Record Eruption crush as its own valid environmental spatial-crush evidence. Do not fabricate an ordinary pillar-fall event merely to satisfy the current `CrushSteps` validation.
- Connect new entry effects to the existing arrival and movement-undo pipeline. Previews use the same pure rules without consuming live RNG, grant entitlement, status duration, or circle usage.

### 8.3 Persistent-stat changes

- Share only baseline reading and conversion of an already-decided final baseline into validated persistent/combat changes with HP normalization. `PersistentGimmickStatsCalculator` must not select Altar allocations, define Contract prices, create offers, or own consumption.
- Altar alone owns pool conservation/minima/random allocation in `DevilsAltarRedistributionRules`. Contract alone owns affordability, reward alternatives, and selected exchange in `EquivalentExchangeChoiceCalculator`. Both consume the same immutable baseline contract and existing atomic publication path; neither knows the other's internal algorithm.
- Keep Main's existing persistent-state owner. Apply its resulting baseline through the existing persistent Main settlement contract.
- For Subs, add a signed maximum-HP/attack/defense adjustment to the owned record. Final baseline = existing level-derived baseline + this adjustment. Future level growth remains unchanged; do not freeze a copied stat snapshot as a new baseline source.
- Preserve the adjustment in all owned-record copy, equality, preparation, acquisition, and serialization paths. New acquisitions/new Runs start at zero. Existing Hunter/trait/soul contributions remain separate.
- Publish character projection and owned/Main changes atomically using existing prepared changes and battle application. Roster queries and future battle entry must use the same baseline calculator, not a second Altar-specific formula.
- HP clamping caused by a maximum change is normalization, not damage: it cannot trigger Smog execution, lifesteal, damage rewards, or post-hit items.

### 8.4 Contract choice transaction

> Superseded (2026-11-17): plan Task 9 has no choice command, waiting phase, or offers. The text below is the earlier approved contract.

Public additions are a `ChooseGimmickReward` command, `AwaitingGimmickChoice` flow phase, and immutable pending-offer/read snapshots. The implementation plan pins their concrete shapes; the behavior below is the contract.

- Offer identity includes battle instance, an offer sequence, gimmick placement, and owned character identity. Include both selectable rewards, their cost, and the resulting stat/HP preview. View must not read InGame DB or recompute affordability.
- Route the selection through the existing `CombatViewBuffer` command numbering, command service, Core calculator, and transition publisher. Include expected battle/turn/offer identity and the selected option.
- During the wait, reject ordinary combat actions and do not start AI preparation or advance another turn. Normal presentation of previously published results may finish. Existing abandon/Run-exit authority remains usable and clears the pending state when the battle is closed.
- Multiple offers are ordered by stable placement ID then owned character ID. Resolve one at a time. No timer or automatic random player selection is introduced.
- Validate identity, index, ownership, current offer, and cost before applying. Reject stale/duplicate/invalid selections without a second charge or consumption. Publication uses the existing transaction validation boundary.
- After the final accepted choice, resume the next faction without repeating the end counter, gimmicks, cleanup, Altar use, or reward offers.

### 8.5 Events, snapshots, forecasts, and View handoff

Expose typed results through the existing Buffer/read-sink path:

- Current head cell, selected direction when known, ordered extension path, final head cell, contacted targets, actual lateral displacement, execution/failure reason, and retraction cue.
- Smog occupied cells, relocation, stack changes, multiplier status, and execution evidence.
- Warning/activation/cancellation/expiry identities and cells for Meteor/Eruption; distinguish Eruption center from pillar cells.
- Fixed Altar/Contract state, consumed flags, persistent-stat before/after values, and Contract offers/results.
- Source identities for Fire/shields and public remaining-duration information using the correct clock.

Core owns cells, ordering, effect outcomes, and choice availability. View owns presentation and completion acknowledgement. No UI, VFX, prefab, scene, or View test implementation is included here. Production activation of Contract requires a consumer for its offer/selection contract; otherwise the deliberate wait cannot complete through the player UI.

Warnings and AI forecasts must not advance the live RNG. Include new obstacle occupancy and source versions in existing freshness checks/caches where those inputs affect a prediction. Chain commits its heading in a warning transition; forecast traversal reads it and current blockers without drawing a replacement heading. The heading is guaranteed, not an exact displacement result under future occupancy changes. View may show the known heading and current reachable path, and must refresh the path if new blockers change it.

## 9. Feature independence, SSOT, SOLID, and Framework review

| Change scenario | Planned edit/understanding scope | Necessary verification |
|---|---|---|
| Change a Meteor damage/count default | Its stage authoring/config and Meteor rule contract; no Altar or Chain internals | Config conversion, Meteor results, shared damage interaction fixtures |
| Change Chain lateral priority | Chain displacement policy plus shared landing contract; no Meteor/Contract implementation | Chain geometry/ties/occupancy and forced arrival regression |
| Change Contract reward values | Its stage configuration; shared baseline transaction and offer contract | Affordability, choice validation, atomic persistence, next battle/level growth |
| Change a shared damage hook | `CombatDamageApplication` and affected shared status/evidence contracts | All relevant damage kinds, protection/reconstruction/death/reward paths; broader integration is required |
| Add another blocking gimmick | Its typed state, occupancy projection, and normal composition registration | Placement, movement, knockback, random relocation, and forecast invalidation |

Before these changes, the existing extension points do not expose resting-head occupancy, a combat reward-choice wait, or general persistent Sub stat adjustments. The proposal adds those specific capabilities instead of requiring each gimmick to recreate them. These are expected scope reductions, not measured implementation results.

### 9.1 Locality review against the supplied guideline

Reviewed on 2026-11-17 against the [user-supplied locality guideline](<D:/My_ProjectWork/학교 관련 문서/졸업작품_프로젝트L/2학기 내용/AI 관련 문서/feature-change-locality-guidelines.md>). Its criteria apply within this documentation task; it does not authorize runtime code edits. The following is a source-and-plan review, not an implemented refactor or runtime result.

| Representative change | Observed burden and cause | Minimum planning correction | Expected edit / understanding / verification scope afterward |
|---|---|---|---|
| Adjust a gimmick's initial placement or warning schedule | Current `StageGimmickComposition.Initialize` directly constructs Rockfall state and selects Haboob/Tornado preparation branches. The previous plan did not prevent another six feature-specific branches. Shared ID/RNG ordering is necessary; per-feature initialization formulas in the coordinator are not. | Give the existing registered rule contract an initialization operation and move each initial-state calculation to its rule. Keep the coordinator's ordering/validation responsibilities. | Edit that rule and its tests for an internal initial-placement change; understand immutable input, RNG, and initial-state contracts rather than unrelated initializer internals. The initial ownership move still requires all legacy initialization/regression checks. New registration or a changed shared contract can legitimately require coordinator edits. |
| Change Altar's redistribution algorithm without changing Contract | The previous plan's Task 8 added `Redistribute` to the shared `PersistentGimmickStatsCalculator`. This would mix one mechanic's randomness/minima with the baseline/persistence boundary used by both mechanics. Shared baseline ownership is necessary; shared feature-specific algorithms are not. | Keep `Read`/`CreateChanges` in the shared calculator; put redistribution in `DevilsAltarRedistributionRules`. Contract keeps its own exchange policy. | Edit Altar's policy and Altar-focused tests; understand the baseline/result contract, not Contract's offer/selection implementation. Shared persistence tests remain necessary when that boundary changes, and the mandatory Phase regression gate remains intact. |
| Tune a stage's Meteor percentage or Contract amounts | Stage-local authoring was approved, but duplicate optional defaults in authoring/Data/spec layers or formulas in adapters could make a value edit require code investigation. This was an underspecified plan risk, not a verified runtime defect in the unimplemented features. | One authored source, explicit immutable conversion, no adapter rule formulas, and separate responsibilities for tuning edits versus new schema fields. | A stage value edit stays in its SO; understand supported ranges and the affected mechanic contract, not unrelated mechanics. Validate conversion and affected behavior. New fields/types still require normal Data/spec/adapter/read-model edits and boundary verification. |

The code inspection needed for this review was broader than the intended knowledge needed for a later feature edit. That investigation is not skipped in the name of locality. No reduction in runtime coupling, file count, or regression cost is claimed until implementation and measurement establish it.

### 9.2 Operational boundary contracts

| Boundary | Inputs and caller conditions | Output, ownership, ordering, and failure |
|---|---|---|
| Rule initialization | Validated matching definition, initial immutable combat state, assigned ID, caller-local environment RNG | Returns that rule's immutable initial state; Railway returns no entry. May advance only the passed local RNG. Cannot increment turns, mutate characters, or publish. Composition installs the aggregate only after validation; an invalid definition produces no partial installed state. Definition order and assigned IDs remain stable. |
| Boundary execution | Current projected input, an accepted faction-end count/side, and the explicit boundary phase | A nonparticipating/no-due rule returns no transition. A produced transition is accepted, preserves turn facts, and references the projected input. The coordinator owns the approved inter-rule order and terminal cutoff; a rule cannot invoke another rule to arrange that order. Invalid data throws before publication instead of being treated as a normal skipped activation. |
| Shared damage | Current recipient facts and the approved numeric damage stage | Smog's helper owns its multiplier/eligibility predicate, while the damage owner keeps protection, HP changes, Reconstruction, health evidence, death, and rewards. The helper must not recreate any of those operations or privately reorder another feature's effects. |
| Gimmick occupancy | The same current gimmick-state projection used by the calculation plus explicit query-local transient reservations | Returns derived blocking cells/overlap only. It does not advance a gimmick, move units, choose relocation destinations, add attack occlusion, or cache another mutable map. Eruption and Chain share blocker facts, not calls into each other's algorithms. |
| Persistent-stat boundary | A validated owned recipient and existing baseline facts; caller supplies the final baseline selected by its own rule | Returns proposed `CharacterChanges` and normalization evidence using the established Main/Sub owners. It does not consume the Altar/circle, make random choices, or publish. The feature includes its own usage/offer changes in the same outer transaction. Failure cannot install only the persistent or only the combat half. |
| Contract choice | A still-pending immutable offer, correct battle/turn/offer/owned identity, valid option, and current affordability | Invalid/stale selection produces the existing rejected-command result with no gameplay mutation. Success charges/grants/consumes atomically and resumes only after the last offer. There is no hidden callback or event subscription to restart end-of-turn effects. |

Schema registration, serialization/equality preservation, Buffer evidence validation, and the mandated regression gates are necessary shared contracts. Do not remove their checks to make a feature appear independent. Conversely, an internal Altar allocation or Chain lateral-priority edit should not require learning another gimmick's calculation simply because both are registered in the same composition.

- **SRP:** each mechanic owns its rules; turn composition owns ordering; Buffer owns accepted facts; View owns animation.
- **OCP:** add registered rules and typed data; change shared integration points only for a demonstrated new capability. Normal factory/registration edits are allowed.
- **LSP:** rules return valid projected transitions preserving turn facts; forecasts are read-only; rejected commands do not partially mutate state.
- **ISP:** consumers receive the relevant state/read contract, not a new whole-world mutable service.
- **DIP:** pure Core does not depend on Unity, SO, Buffer implementation, or View. Existing services remain responsible for conversion/publication.
- **SSOT:** no duplicated head positions, independent turn timers, shield budgets, or persistent baseline stores. Distinguish the fact of a shield grant from its remaining budget.
- **Framework:** preserve FW-001/002 service/module registration, FW-003 build-once composition, FW-004 owning-state boundaries, and existing input/event/lifetime contracts. Internal rule helpers do not automatically become Framework services or Jobs.

Relevant maintained references: [Framework architecture](../../../Assets/ProjectL/Script/Systems/Documentation/framework-architecture.md), [Framework programmer guide](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md), and [feature-independence criteria, section 4.6](../handoffs/2026-09-30-combat-view-design-session-handoff.md#46-기능-변경의-독립성--조립과-기능-구현-분리).

## 10. Acceptance and regression design

Use test-owned inputs, not mutable production SO catalogs. These are planned Core/integration checks, not tests written or executed in this document task. Do not add View tests or run combat View MSBuild. User Unity verification covers scene/visual behavior later.

| Area | Required cases |
|---|---|
| Timing | Player end +1, enemy end +1, no duplicate end on choice resume; exact warning/activation sequences; creation boundary not counted toward lifetime |
| Smog | Relocation before stacks; own-side-only gain; stage-long partial stacks and capped multiplier; applicable existing cleanup compatibility without restoring night return; exact 10% is not execution; below threshold with real loss is; shield-only hit and normalization are not; all numeric damage kinds, post-hit healing order, Reconstruction |
| Chain geometry | All 8 directions; in-bounds final head; perpendicular candidates; nearest-boundary priority and left tie; blocked outer fallback; both blocked/immune defeat; endpoint survives if a lateral destination exists |
| Chain occupancy | Resting head rejects ordinary movement/placement/forced and random landing; full candidate footprint cannot overlap transient reservations or new gimmick blockers; old head released and final head occupied coherently; terrain/arrival effects run through existing rules; no retraction hit; no legal heading terminates without a loop |
| Chain warning/survival | Heading revealed one faction end early; immediate new obstruction yields no movement rather than a reroll; immune target saved by Reconstruction stops the head even with open lateral cells; head/pillars do not block attacks |
| Chain/Eruption integration | Same-boundary new pillars stop Chain; expired pillars no longer block; relocation reserves distinct destinations; spatial crush bypasses Reconstruction only in the defined no-space case |
| Altar | Baseline-only pool and sum preservation; minima; current HP clamp; one use; enemy ignored; reused pillars remain; Main/Sub persistence and later growth |
| Contract | Every circle default including defense; configurable costs/rewards; owned-only eligibility; insufficient cost retains circle; mandatory two-option offer; wait blocks next faction; invalid/stale/duplicate choice; multiple offers; atomic charge/reward/consume; abandon clears wait |
| Meteor | Count bounds; complete valid footprints; no same-activation overlap; locked full-area warning; dynamic invalidation; numeric 60% damage; hit/entry/end Fire; expiry and overlapping flame sources |
| Eruption | Cross roles; all-four pillar validity; initial victim capture and one 30% hit per character even across multiple pillar cells; relocate only survivors with no victim rescan; no-space crush; later center entry; once-per-instance shield with no re-entry refill; source expiry removes remote shield; center exit ends attack benefit |
| Shared contracts | Fire 2/3-duration interaction; existing shield ordering; all copy/equality/snapshot and Buffer evidence paths preserve new fields; deterministic RNG and preview purity; new occupancy invalidates stale forecasts |
| Compatibility | Old stages with new fields absent/disabled; existing Tornado/Railway/Haboob/Rockfall order and behavior; movement undo/return; level growth; damage attribution, death, rewards, stage completion, and Run restart |

The View handoff checklist must include full warning footprints, correct lateral displacement playback, retraction without replaying hits, distinct Eruption center/pillars, expiry, consumed interaction display, and reward-choice submit/disable/resume behavior. No visual acceptance is implied by Core test results.

## 11. Implementation dependency outline

This is a dependency outline. The executable task breakdown is in the [implementation plan](../plans/2026-11-17-new-stage-gimmicks-core-implementation-plan.md).

1. Read the approved decision record in section 12 and the implementation plan; recheck the current source baseline before execution.
2. Add pure data/authoring and common contracts: timing integration, derived blocker queries, status lifetime/Fire duration, persistent Sub adjustments, and pending choice. Keep every new stage section disabled by default.
3. Implement focused environmental mechanics against those contracts: Smog and Meteor, then Eruption, then Chain integration with Eruption blockers. Validate each phase before proceeding.
4. Implement Altar/Contract through the shared baseline and transaction boundary; Contract additionally requires the choice command/gating contract.
5. Complete Core integration/evidence/forecast regression and deliver the View contract. View/production scene activation remains a separate task.

Do not implement all shared abstractions speculatively before a consumer needs them. Split the actual Task plan into reviewable phases after design approval. Code comments remain Korean; implementation/design documentation remains English.

## 12. Review decisions and approval record

The following questions were raised against the first draft and subsequently answered by the user:

| ID | Question | Design consequence | Confirmed decision |
|---|---|---|---|
| O1 | Do the resting head and fixed Altar/Eruption pillars block attacks? | Their movement classification must not accidentally change attack rules | Movement/landing and Chain blocking only; untargetable and no attack-path blocking |
| O2 | When is Chain's random heading chosen and exposed? | Forecasts must distinguish committed heading from a path affected by later obstacles | Select and publish one faction end before activation; retain the direction, clamp movement at actual blockers, and never secretly reroll |

The builder review also requested explicit handling of multi-cell Eruption contact, transient Chain occupancy/full-footprint overlap, and Reconstruction of an immune Chain target. Section 6 supplies the adopted defaults. These are project design decisions, not additional claims about the source slides.

On 2026-11-17 the user approved the recommendations in sections 6 and 7 and requested a plan. They also clarified that automatic night-return no longer exists; this document and the plan must not reintroduce it. If execution requires a different gameplay rule, state owner, or ordering, ask before implementing that deviation. Stop and report at every planned Phase boundary.

The subsequent user-requested locality review refined initialization ownership, Altar-versus-shared-stat policy ownership, and boundary contracts in sections 8-9. These are planning refinements within the requested scope; approved gameplay, execution order, public behavior, and runtime state owners are unchanged. Runtime implementation and its regression evidence remain pending.

## 13. Document verification record

- Source text and rendered PPT slides were inspected; later user corrections were reconciled explicitly.
- Existing code ownership and integration points were inspected at the baseline above.
- No Core/View code, test code, production assets, or scenes were changed for this draft.
- Runtime, compilation, and visual acceptance remain unverified.
- An independent builder review identified three consequential gaps beyond the initial O1/O2 questions. After amendment, the follow-up review reported no remaining important questions preventing a reliable implementation plan. This is design review evidence, not compilation or gameplay acceptance.
