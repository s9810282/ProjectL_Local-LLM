# Rampage, Lava Cannon, and Bleeding — Core Design and Boundary Review

Date: 2026-11-06
Status: Phases 0–5 implemented and verified against the recorded baseline on 2026-11-07; see the implementation plan for execution evidence. Production activation and PlayMode acceptance remain pending.
Baseline inspected: `bca934e08`, `feature/Cheon/Core/NewCombatSytem`.
Phase plan: [Implementation plan](../plans/2026-11-06-rampage-lava-cannon-bleeding-implementation-plan.md).

## 1. Intent and scope

Add Rider Rampage, Cannon Lava Cannon, and Common Bleeding. Reduce the risk of a new trait working in its calculator but failing in result validation, movement undo, preview, AI, or publication. The requested change includes these three entries even though the initial request called them two new traits.

The user selected a focused refactor of the common boundaries needed by these traits. Preserve existing Framework contracts, Core paths, public behavior of other traits, and Buffer/Job ownership. Do not turn the review into a wholesale trait rewrite. Implementation documents are English; new code comments and XML documentation are Korean. Do not commit or push.

This document defines the Core work, InGame authoring/rank integration, and required View handoff. It does not authorize scene, VFX, camera, or unrelated UI work. Production activation must include the paired View registrations described below; Core completion alone is not gameplay acceptance.

Proposed deliverables include editable, unregistered growth-definition SO assets for the three traits, in addition to authoring types and test-owned definitions. Keep existing live category acquisition pools, unit loadouts, and category defaults unchanged in these Core phases. Adding the new assets to live catalogs/pools is a paired activation task after the View handoff is implemented; do not create an active entry that can immediately fail its required presentation registration.

## 2. Approaches

1. **Selected: focused shared boundaries.** Reuse attack selection/delivery, `CombatDamageApplication`, death settlement, immutable statuses, existing rank resolution, and Buffer publication. Consolidate movement allowance and attack completion duplication, then introduce only the post-contact and periodic-status collaboration needed here. Benefits: concrete reduction in repeated rules without migrating unrelated traits. Cost: the existing trait authoring/registration chain still requires normal composition changes.
2. **Not selected: migrate all traits to a new lifecycle model first.** Replace the existing mix of capability interfaces and application-level trait branches across the entire catalog. This could unify more entry points, but changes unrelated behaviors and broadens regression risk before providing the requested features.

Do not add a generic event bus, a second effect engine, a new global service, or another mutable status store. A registration change is a legitimate composition change, not evidence of failed independence.

## 3. Confirmed gameplay decisions

| Feature | Confirmed rule |
|---|---|
| Rampage condition | Current HP is strictly below 40% of current maximum HP. Exactly 40% is inactive. |
| Rampage ranks | Movement bonus: 1 / 1 / 2. Attack bonus: n / n+5 / n+7. `n` is a fixed value editable in SO authoring, not the unit's own attack. |
| Rampage kills | While active, gain 100% of the defeated enemy's **base attack**, excluding buffs and debuffs. Gains accumulate. |
| Rampage reset | Reaching 40% HP or higher removes the conditional bonus and clears the accumulated kill bonus. |
| Lava Cannon targets | The existing Cannon basic attack's primary target and actual splash footprint. This is not an aura or a fresh query over the Cannon's selectable range. |
| Lava Cannon interruption | Once the attack is accepted and its primary/splash targets are resolved, apply fire and slow to all surviving enemies in that captured set even if reflection kills the Cannon and interrupts later direct hits. Do not restrict the effect to contacts delivered before the death. |
| Lava Cannon fire | Apply non-stacking fire, dealing 2 damage at the affected side's turn start, for two ticks. Reapplication refreshes the duration. |
| Lava Cannon slow | Reduce movement by 1 during the affected side's next turn, then remove it. It does **not** last as long as fire. |
| Slow timeline | Enemy attack → Player turn has movement -1 → following Enemy turn has no slow. |
| Bleeding | Attacks apply stacks; target-wide maximum 5. Reapplication refreshes the shared duration to three affected-side turn-start ticks. |
| Bleeding rank damage | Trait level 1 = damage 1 per stack; level 2 = damage 2; level 3 = damage 3. Every stack preserves the applying unit's level damage. Different levels can coexist on the same target. |
| Periodic damage | Use the existing damage-over-time path: no physical attack-defense calculation; apply applicable incoming-damage modifiers, exposure, shields, and item rules at the common boundary. |

The slow correction above supersedes the earlier answer that both Lava Cannon effects lasted two turns.
The final interruption decision above supersedes the intermediate answer to apply Lava Cannon only through the last delivered contact. The user requested the simpler captured-area rule even when the Cannon dies.

## 4. Adopted planning details

These were proposed to complete unspecified edge cases and are carried into the Phase plan after the user's planning approval. They are distinguished from the directly specified gameplay decisions in Section 3. The user's latest corrections always take precedence.

- **Attack counting:** one Bleeding stack per actual damage-bearing contact with a surviving hostile character, including primary/splash and genuine extra/reaction attacks. A zero-HP-loss contact absorbed by a shield still counts. Non-damaging contacts, reflection, environmental damage, and periodic ticks do not apply on-hit traits. Multiple actual hits can add multiple stacks; merely occupying multiple cells does not duplicate one contact.
- **Full Bleeding stack set:** at five stacks, retain the five existing stack magnitudes and origins and refresh the timer; do not silently replace a weaker stack. A new stack added at fewer than five refreshes the lifetime of the whole set.
- **Lava Cannon recipients:** preserve the engine's resolved primary/splash target set. Apply once to each surviving enemy in that footprint even when damage is absorbed or the Cannon dies before reaching that target. Do not move the area debuff to a Sacrifice protector outside the footprint. Bleeding follows the actual hit recipient, including a protector, because its trigger is a hit rather than area membership.
- **Lava attack types:** proposed behavior is to follow accepted ordinary, extra, and reaction attacks using the holder's attack profile, while excluding reflection and periodic/environmental damage as independent triggers. Its area-status application does not filter by delivered-hit count or applied damage percentage. Apply it once per accepted attack, using the trait configuration captured for that attack even if its holder dies.
- **Fire refresh source:** one fire status per target; the latest successful application owns the refreshed status's damage credit. Multiple Cannons never multiply fire damage or slow magnitude.
- **Periodic ordering and credit:** process living field recipients in EntityId order, fire before Bleeding, and Bleeding stacks in application order. Each stack retains its source and passes a separate damage application through the common boundary. The contribution that causes death supplies kill credit. This deliberately preserves source-specific damage and item processing; do not combine multiple origins and arbitrarily credit their total to the first source.
- **Turn-start ordering:** perform the existing start-side shield expiry and start resets before periodic damage. Resolve the complete captured periodic batch, skipping recipients that are already dead, then use existing completion/outcome priority once. If Main and the objective both die in the batch, use that existing priority rather than introducing a special DoT tie rule. Preserve the pre-tick `livingAtStart` count for Nexus reinforcement eligibility; tick deaths do not open new same-turn reinforcement capacity, and a terminal batch performs no reinforcement.
- **Duration:** fire/bleeding count future starts of the affected side, never global half-turns. Apply the tick before consuming that remaining tick; remove after the final tick. Slow expires at the end of the next affected-side turn that starts after application. A reaction during an already-running affected-side turn must not consume that future turn at the immediately following end. Reapplication refreshes that expiry without increasing the magnitude.
- **Rampage transitions:** capture eligibility at the credited enemy death before kill-triggered healing. Apply the reward once, then reconcile HP-based eligibility after all associated healing; healing to 40% or more clears the accumulated reward immediately. Re-entering Rampage starts the kill accumulator at zero. An inactive or dead holder receives no new Rampage gain. Current maximum HP is used after max-HP changes as well.
- **Reserve and battle cleanup:** clear these transient statuses and the Rampage kill accumulator on death, reserve return, or battle end, consistent with existing transient-status cleanup. No Rampage attack gain persists in the Run roster.
- **Growth:** Rampage is `CategorySpecific` in the Rider pool; Bleeding is `Common`. Lava Cannon is `CategorySpecific` in the Cannon pool and has fixed values; no unrequested rank scaling or enhanced variant is invented. The existing `TraitGrowthGroup` remains Common/CategorySpecific; category ownership stays in category progression definitions.
- **Authoring value:** no balance value for `n` was supplied. The authoring contract allows a nonnegative designer value, including zero, and fixtures use explicit values. A new authoring instance may use zero as its neutral editable default, clearly documented as a placeholder. Do not invent production balancing or automatically replace category-default traits.

## 5. Current code evidence and review findings

Paths below use `C/` for `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/` and `S/` for `Assets/ProjectL/Script/Systems/`.

### 5.1 The resolved Taunt failure

Commit `968436c83` repaired an exception during action-availability querying: the query built an undo candidate whose result validator rejected additional Taunt recomputation events. The subsequent `941fe4f83` moved the exception from a Taunt-specific comparison to `AppliedStatus.Lifetime == CurrentEligibility`.

Current sources: `C/Contracts/Results/MovementUndoResult.cs`, `C/Application/Combat/MovementEffectUndo.cs`, `C/Contracts/CombatEffects/AppliedStatus.cs`, and `Assets/Tests/EditMode/MovementPassiveUndoTests.cs`.

Preserve that contract. Fire, Bleeding, slow, and the Rampage kill accumulator are stateful history and must not be mislabeled `CurrentEligibility` to bypass undo validation. Only the genuinely recomputable condition is derived from current HP. An HP transition caused by a move must still restore or invalidate the relevant historical effects through the existing undo contract. A status lifetime label alone cannot undo modified stats or consumed damage history.

### 5.2 Confirmed duplication and narrow cleanup candidates

| Finding | Evidence | Minimal change | Classification |
|---|---|---|---|
| Attack completion is invoked twice on the successful non-preview delivery path. | `C/Application/Combat/AttackDeliveryExecutor.cs` calls `HunterApplication.FinishAction` inside the conditional block and again immediately afterward. `HunterApplication.FinishAction` returns when `scope.IsFinished`. | Keep one completion call after optional kill-chain work, preserving completion for preview/reflection/no-direct-damage branches and nested scopes. | Confirmed redundant call; no duplicate reward bug is claimed because the guard exists. |
| Movement allowance is calculated independently in lookup and execution validation. | `C/Application/Combat/CombatActionAdmission.cs:MovementAllowance` and `C/Domain/Movement/MovementRouteValidator.cs:StepBudget` both combine base range and GroundCompaction. AI uses the route budget and separately gates zero base range. | Make one pure Domain rule own effective voluntary movement and let lookup, validation, and AI consume it. Retain explicit immobility and forced-movement rules. | Confirmed repeated rule; a concrete drift risk when adding Rampage/slow. |

Zero base movement currently disables voluntary movement in both paths. The nullable allowance is not an unlimited-movement contract; `CombatMovementCalculator.Actor` rejects it. Existing `CombatActionCostRetirementTests.ZeroVoluntaryRange_DisablesMovementWithoutChangingFacts` documents this. Do not report a current zero-range inconsistency or change this behavior accidentally.

### 5.3 Required integration work, not claims of existing failures

| Boundary | Observed source | Required treatment |
|---|---|---|
| Hit ordering and mid-attack changes | `AttackDirectDamageCalculator.Apply` has an explicit list deciding when pending health must flush before the next hit. | Rampage kill gains and HP-dependent effects must be visible at the intended subsequent hit. Make the requirement explicit in the attack policy/capability used here, rather than depending on an undocumented new trait-key whitelist. Do not reorder unrelated attacks. |
| On-hit state application | `AttackDirectDamageCalculator.FlushHealth` directly calls WeaknessExposure, restriction, and ArmorBreaker collaborators. | Add a focused contact collaboration that receives the original target, actual recipient, attack origin/group, damage-bearing-contact flag, and immutable attack context. Keep existing trait predicates and order. Lava area's target set remains distinct from hit recipients. |
| Turn-start death | `CombatTurnCalculator.BeginControllerTurn` currently performs status reset/action availability; `Changed` constructs an InProgress outcome. | Periodic damage must use the existing shared projection/completion path, publish damage/death/reward/soul/owned changes, and resolve victory/defeat before input or enemy actions. Merely subtracting HP inside `TraitTurnLifecycle` would bypass required settlement. |
| Status clocks | `TraitTurnLifecycle` records expiry while `AppliedStatusRules` owns state reduction; `StatusEffectSourceSnapshot` exposes existing clock types. | Introduce the necessary affected-side-start tick read contract and preserve the independent end-of-next-side-turn slow expiry. Derive expiry evidence from the actual transition; do not create a second countdown in View or the controller. |
| Status presentation | `S/Factory/Runtime/InGame/View/CombatUnitStatusFactory.cs` registers exact snapshot formatters; `CombatEffectFormatters.Get` throws for missing entries; `CombatUnitStatuses.AddTo` also requires the visual row. | Prepare an exact handoff for new snapshot types, keys, magnitudes, stacks, sources, and timing. Paired formatter/visual registration is required before activating production content. Do not hide missing required registration by swallowing exceptions. |

Planning-session evidence only: no Unity execution, runtime reproduction, or passing-test claim was made during that review. This is a focused review of the paths required for the requested changes, not a completed audit of every trait.

## 6. Ownership and collaboration design

### 6.1 State and configuration

- Keep `CharacterCombatFacts.StatusEffects` as the canonical target-owned store for fire, Bleeding, and slow. Each stack contains its immutable origin and fixed damage at application. The Bleeding set owns one shared remaining-ticks counter.
- Keep Rampage's kill accumulator in its existing-pattern trait runtime state/snapshot. Derive active/inactive status from current HP; do not store a second boolean that can diverge. Conditional stat projection reads this canonical accumulator and the resolved trait definition.
- Extend the existing SO → immutable InGame Data → TraitSpec → registry definition/runtime/effect binding chain. Preserve pure InGame types and existing namespace/meta conventions.
- Reuse `TraitGrowthDefinitionDataSO` and rank resolution. Store `n` once in the normal Rampage authoring and use rank offsets 0/5/7 with movement bonuses 1/1/2. Changing `n` must not require manually editing two copied absolute values in rank 2 and rank 3. Bleeding resolves stack damage 1/2/3 through the same growth path.
- Allocate new stable trait/status identifiers without reusing retired values. Extend classification and public snapshot support together. No additional Framework service registration is needed for these pure internal collaborators.

### 6.2 Effective stats

Use a pure Domain movement rule that accepts the unit facts and the existing environment view. Apply the resolved Rampage movement bonus and target slow once, clamp the resulting finite allowance at zero, and keep the existing base-zero/voluntary-movement capability policy. GroundCompaction remains an existing environmental contribution. The Application entry points and AI consume this rule; Domain must not reference Application.

Project Rampage attack through the current effective-trait-attack contribution flow. Base attack remains unchanged. Existing displayed effective attack and actual damage must agree; no presentation-only attack field or parallel mutable total is added. Damage, recovery, max-HP change, return, death, preview, and restoration paths must reconcile the condition. Only historical values such as the kill accumulator are retained.

### 6.3 Attacks and status application

Use the engine's resolved target/footprint and delivered contacts; do not duplicate splash geometry or targeting policy. The application collaboration owns when a trait is invoked; each trait rule owns its trigger and contribution. Target status rules own merge/refresh/cap behavior.

Apply Bleeding after an eligible actual hit to a surviving recipient. Capture Lava Cannon's enemy target IDs from the accepted attack's existing resolved primary/splash selection before direct delivery. After delivery, apply its statuses once per surviving captured target without checking whether the attacker remains alive, how many hits were delivered, or where a target moved after knockback. This is a separate area application, not an actual-recipient on-hit callback. No status is created on a corpse or structure. Rejected attacks never apply it. Preview uses a private projection and must not mutate live facts, random state, event sequence, or the roster.

Ordinary, extra, and reaction attacks must enter these boundaries according to the explicit attack-counting policy. Preserve existing behavior for restricted targets, Sacrifice, shields, instant defeat, reflection death, and empty attacks. Do not trigger another attack from a periodic tick.

### 6.4 Periodic damage and turn completion

At an affected-side start, preserve the existing shield-expiry/reset ordering, then capture the statuses eligible for that boundary and process them deterministically through the shared `CombatDamageApplication` with `CombatDamageKind.DamageOverTime` and source attribution. A refreshed or newly created status during processing cannot recursively tick at the same boundary. Skip already-dead or non-field recipients. Complete the full captured batch before evaluating the final outcome; deaths still settle once through the shared damage boundary during the batch.

Reuse the existing command projection and completion ownership so deaths, drops, souls, kill traits, roster changes, statistics, objective outcomes, and result validation stay on their common routes. Stop normal turn continuation after a terminal batch. Retain the existing pre-tick `livingAtStart` reinforcement admission snapshot, and perform reinforcement only after nonterminal completion; newly spawned reinforcements do not inherit an earlier status tick.

After each relevant state change, reconcile conditional Rampage stats before a subsequent action or hit reads them. Expiry and state events must describe the exact before/after facts used by the result validator and publisher. No special unvalidated HP writes are allowed.

### 6.5 Kill rewards and reversible movement

Use `TraitKillSettlement` and its once-per-death set. Read the enemy's `death.Expected.Stats.AttackPower` for base attack, matching the existing MoraleBoost basis. Keep Rampage's eligibility capture and the later health reconciliation explicit so combining CallOfBlood with Rampage does not depend on registry order.

Move-induced damage/healing can activate or clear Rampage. Extend the existing movement-effect evidence/restoration boundary where necessary; do not weaken `SameExceptCurrentEligibility` or discard kill/timer history during comparisons. Both accepted undo and rejected undo must be valid query results. Stale movement contexts must fail normally when effective movement or relevant status inputs change.

## 7. Change, understanding, and verification scope

| Scenario | Current change/understanding burden | Intended boundary after this work | Required verification |
|---|---|---|---|
| Change a movement bonus/penalty | Inspect admission/range lookup, route budget, AI gating, and trait/environment contributions separately. | Edit the trait/status contribution; all voluntary movement consumers use one Domain allowance rule. | Range vs execution vs AI, zero allowance, GroundCompaction, forced movement, stale context, undo. |
| Add another contact-applied status | Understand the direct-hit flush body and each neighboring trait's trigger/recipient convention. | Implement its rule and wire it into the focused contact collaboration; original target and actual recipient are explicit. | Ordinary/extra/reaction, splash, shields, Sacrifice, repeat contacts, death, preview. |
| Change Bleeding damage or cap | No current implementation. Naive placement would scatter rules across hit/turn/read-model code. | Merge/tick policy stays with canonical Bleeding status rules; orchestration consumes transitions. | Mixed-rank stack values, cap/refresh, exact three ticks, source credit, cleanup. |
| Add turn-start damage | Existing turn start assumes no new lethal damage, so all settlement obligations need tracing. | One periodic-status boundary delegates damage and completion to the existing owners. | Death and terminal outcome, items/shields, rewards/souls/statistics, once-only ticking and publication. |
| Register a new trait/status | Existing SO, Core registry, rank mapping, publication and View formatter registrations must be identified. | Keep legitimate composition edits explicit in a single handoff checklist; do not hide them behind reflection or fallbacks. | Definition validation, rank resolution, Buffer snapshots, paired View readiness. |

SOLID/SSOT assessment: trait rules own mechanics; orchestration owns order; state has one owner; narrow contexts expose required facts without passing all of another feature's implementation; existing contracts remain substitutable; composition owns registration. Apply FW-001/002 only to actual Framework service/module registrations, not every helper. Preserve FW-004 access boundaries, Core/Application independence from Unity and Buffer, and the existing Systems coordinator's commit/publication responsibility.

## 8. Verification and phase boundaries

The linked implementation plan follows the user's design approval and incorporates the subsequently supplied feature-change locality guidelines. Its phases separate: baseline evidence, behavior-preserving boundary cleanup, status/tick infrastructure plus Lava Cannon/Bleeding, Rampage, rank/SO authoring, and integration review with View handoff. Stop and report after each implemented phase, as required by the repository instructions. Task grouping does not change gameplay or widen scope.

Mandatory acceptance cases include:

- HP 39/100, 40/100, and 41/100; non-divisible max HP; healing and max-HP changes; repeated Rampage entry/exit; multiple kills and kill-triggered healing; base-attack reward excludes all bonuses.
- Primary plus splash targets, out-of-footprint enemies, full shield absorption, Sacrifice inside/outside the footprint, hostile filtering, death, multi-contact attacks, reflection, extra/reaction attacks, and preview isolation. In particular, reflection death must still apply Lava Cannon to surviving captured splash targets whose direct hit was never delivered, while Bleeding must not be applied to those unhit targets.
- Fire ticks twice and never stacks; slow affects exactly the next target-side turn and expires before the opposite turn; refresh does not stack; application during the target's running turn preserves the promised next turn.
- Bleeding mixed ranks (for example 1+3+2), five-stack cap, refresh at cap, exact three future target-side ticks, source-specific lethal credit, dead/missing source, and source rank changes after application.
- Turn-start lethal damage to the last objective enemy or Main; no action or reinforcement continuation after terminal completion; all common settlement outputs and statistics published once.
- Move allowance consistency, zero movement, GroundCompaction, forced movement, action availability, undo and stale-context paths, including the resolved Taunt case.
- Existing Core and CoreIntegration regressions appropriate to shared attack/status/movement changes. Use test-owned definitions rather than mutable production catalogs. Record baseline failures separately from introduced failures.

This request is Core-focused; it does not authorize View test code or combat-View MSBuild. Existing test ownership restrictions remain applicable: do not rewrite another author's behavioral expectations without the required user decision. The planner must identify any such expectations affected by new enum members. If Unity is executed later, use the approved AppData-access path; do not delete caches or upgrade Unity to work around sandbox access.

Planning-session verification status (historical): static source review only; compilation unverified (컴파일 미확인), no tests or PlayMode run, no scene/visual acceptance. No source files or production assets have been modified.

## 9. Design review

A separate planning review checked the draft against the current attack, turn-start, and authoring paths. The design specifies shield-expiry order, whole-batch periodic settlement, the unchanged pre-tick reinforcement count, and dormant SO deliverables. The user resolved the interruption question with the final simpler rule: Lava Cannon applies to the surviving captured primary/splash target set even after the Cannon dies. The review's remaining planning questions are addressed. The user then reaffirmed the Bleeding level values and requested the Phase plan. The subsequently authorized Phase 0–5 execution and verification are recorded in the implementation plan.

## 10. Implementation status — 2026-11-07

Core behavior, rank authoring, and the three dormant assets have been delivered through Phase 5. Unity EditMode passed the 106-case authoring/regression selection and the combined Buffer publication scenario. The final Core/CoreIntegration run passed 5,888 of 5,960 cases, with all 72 failure names and messages matching the pre-implementation baseline. The [Core-to-View handoff](../handoffs/2026-11-06-rampage-lava-cannon-bleeding-core-view-handoff.md) lists paired formatter and visual registrations, activation order, and the remaining Unity PlayMode checks.
