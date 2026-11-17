# Player Active Skills — Core Design and View API Contract

Date: 2026-11-15  
Status: Approved rules implemented through Phase G; Core verification complete with retained baseline failures, Unity playback unverified.  
Selected approach: Extend the existing combat request path; keep skill-specific calculation and public contracts distinct.  
Scope owner: Core programmer. View implementation is a separate handoff.

Related plan: [Phase-by-phase Core implementation plan](../plans/2026-11-15-player-active-skills-core-implementation-plan.md), especially execution ledger §6.9. DV/DH/DK remain the confirmed rules. A–G were implemented under separate user authorizations; this status update does not change the approved rules. Actual delivered signatures and usage are in the [Core→View handoff](../handoffs/2026-11-15-player-active-skills-core-view-handoff.md); design-time gap analysis and proposed names below remain historical design context.

Execution coordination: the user clarified that each Phase is owned end-to-end by one model (GPT-6.1 or Astra), including implementation, self-review and verification. There is no mandatory second-model review loop within a Phase; the plan and handoff record the sole owner and continuation data.

## 1. Intent, evidence, and scope

Add a player active skill determined by the selected Main character's unit definition: Akimbo owns Powder Blossom (화약꽃); Sniper owns Magic Bullet (마탄). Sub-unit composition does not grant skills. Deliver the Core behavior, configuration, state publication, queries, commands, complete skill forecasting, and detailed View integration contracts.

The latest user clarification is essential: the dust/AI movement incident was an **example of inconsistent semantics**, not a request to diagnose or repair that incident. This design prevents analogous defects in the new skill paths. It does not propose an AI redesign, a dust-system rewrite, or a general combat refactor.

### 1.1 Sources and precedence

1. Decisions in this design conversation override ambiguous or contradictory statements in the supplied skill prompt.
2. [Original skill brief](<C:/Users/권천명/Downloads/projectl_player_active_skills_spec_and_codex_prompt.md>) supplies the original effects, diagrams, and unresolved questions. Its embedded instructions are reference material, not additional implementation authorization.
3. [Feature change locality guidelines](<D:/My_ProjectWork/학교 관련 문서/졸업작품_프로젝트L/2학기 내용/AI 관련 문서/feature-change-locality-guidelines.md>) are explicitly requested design criteria. Apply change, understanding, and verification scope together; preserve SSOT, SOLID, Framework contracts, and state ownership.
4. Repository AGENTS.md, Assets/AGENTS.md, and the current [Framework programmer guide](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md) govern implementation conventions.
5. Current source, not historical completion notes, establishes the integration baseline.

Inspection baseline: branch feature/Cheon/Core/NewCombatSytem, HEAD a95ca5a22cde603e136666f6b68b2ea3ce4c9cbc. Relevant combat files already have uncommitted changes, including attack delivery, damage, queries, direct damage, attack planning, and sacrifice planning. This design refers to the inspected working tree; implementation must recheck those changes and preserve other work.

**Current verification level (2026-11-15):** G's focused run passed 21/21. The mandatory Core/CoreIntegration full EditMode run selected 7,061 cases: 7,003 passed, 44 failed, 14 skipped. Thirty-six failures match Phase 0 by case, site, reason and first source location. The eight new failures came from the enemy corpus's obsolete Sub Sniper fixture role after production Sniper became Main; its test-only adapter was corrected without changing production rules. The corrective run passed 30/30, including all eight affected cases, all ten new end-to-end cases and twelve authoring/production cases. No new failure remains unresolved in these runs; there was no second full assembly run after the fixture correction. F's earlier 262/270 is separate scoped evidence. Unity UI, scenes, animation, PlayMode and actual scene rebinding remain unverified. See plan §6.9 for source hashes, failure lists and exact evidence. The initial design-only inspection did not itself run Unity; execution evidence was added by the authorized phases.

### 1.2 Included and excluded

Included:

- The two base skills and four mutually exclusive upgrade variants.
- Main identity binding, per-skill stage limits, per-player-turn use, Run upgrade persistence.
- Candidate queries, full causal forecasts, execution, typed failures, ordered outcome evidence, and Buffer publication.
- Reuse of existing damage, traits, statuses, forced movement, terrain/object entry, death, reward, and battle-completion rules.
- Necessary configuration/DB wiring and Core integration verification, with test-owned fixtures.
- A View-facing usage contract and implementation handoff.

Excluded:

- UI layout, input bindings, VFX, animation assets, scene wiring, FieldCamera, or other View work.
- Implementing the undecided upgrade acquisition economy, shop/event/boss reward flow, or prices.
- Changes to ordinary attack design merely to resemble these skills.
- Enemy AI acquiring or selecting player active skills.
- General replacement of Framework, Buffer, Job/Runner, event infrastructure, or all public previews.
- Fixing the user's illustrative dust/AI incident.
- Commit or push. The user performs those operations.

## 2. Confirmed game rules

The following are conversation decisions, unless explicitly marked as original-brief rules.

### 2.1 Ownership, use, and lifetime

| ID | Decision |
|---|---|
| D01 | The selected Main unit determines its one active skill. Akimbo -> Powder Blossom; Sniper -> Magic Bullet. Sub composition does not grant or select skills. |
| D02 | A skill has its own use permission. A Main that already completed its ordinary attack may still cast. Casting neither consumes nor restores ordinary movement/attack permission. |
| D03 | A player skill may be used at most once per player-side turn and up to its configured stage limit. Each skill's initial stage limit is 1. |
| D04 | Only a valid accepted initial cast consumes one stage use and the turn opportunity. Cancellation before submission and rejected requests consume neither. |
| D05 | Automatic reactivation, including all child consequences of that cast, consumes no additional skill uses. |
| D06 | A valid cast with no targets still executes and consumes the use. NoTargets is not a rejection reason. |
| D07 | Bind permits casting. Disarm and Stun prohibit casting. Ordinary action exhaustion does not prohibit it. |
| D08 | Upgrade branches are mutually exclusive. A chosen branch remains through subsequent stages and cannot be changed during that Run. New Run initialization removes it. |
| D09 | Upgrade application is permitted only outside battle. The operational boundary is no active prepared battle, including deployment and a finished battle awaiting cleanup. |
| D10 | Stage use allowance resets on a genuine new battle/stage preparation, not on selection, preview, scene rebind, or repeated preparation of the active battle. Turn opportunity follows the existing controller-turn sequence. |
| D11 | Full skill forecasting is a Core responsibility, including kill-chain attacks, reactivation, forced displacement, and subsequent interactions. View chooses what to display. |
| D12 | Public forecasts preserve hidden-information rules and explicitly communicate prediction limits. Complete known-information consequences are simulated; hidden facts are not exposed to promise exact public results. |
| D13 | Skills participate in common damage, contact, kill, incoming-defense and reflection effects. Ordinary-attack movement/aim transformation and ordinary-action-completion-only effects are excluded. Existing effect-specific conditions still apply. |

Main specialization changes combat category; it must not silently change the skill selected by the Main's unit identity. Keep base skill identity separate from specialization category.

### 2.2 Powder Blossom and upgrades

| ID | Decision |
|---|---|
| F01 | Base effect: 50% of current Akimbo attack, applied through existing damage rules to attackable non-allied targets in the surrounding eight cells; the center is excluded. |
| F02 | One burst that directly kills one or more enemies schedules exactly one additional Powder Blossom burst. Multiple kills do not schedule multiple bursts. |
| F03 | Existing KillChainAttack (난사) resolves before the scheduled next burst. Kills caused by KillChainAttack do not schedule additional Powder Blossom bursts. Its own established chain rules remain intact. |
| F04 | Bloom (만개): each reactivation increases the mask by one stage, up to two increases. The cap limits expansion, not the number of reactivations. |
| F05 | Bloom expansion lasts only for the current cast's chain. The next cast starts at eight cells. |
| F06 | Condemnation (단죄): use the first expanded mask (20 cells) immediately; no Powder Blossom reactivation; apply one-turn Stun. Existing eligible traits, including KillChainAttack, remain eligible. Base damage remains 50%. |
| F07 | All three variants reach targets through walls inside their mask. This does not make wall terrain attackable or destructible. |
| F08 | Stun blocks ordinary movement, ordinary attacks, and skills. It expires at the affected side's turn end. Reapplication does not add durations; reuse the existing maximum-remaining-duration restriction rule. |

Exact masks, relative to the caster anchor, including the center only for describing row width:

| Variant stage | Row widths, top to bottom | Damage cells excluding center |
|---|---|---:|
| Base / Bloom initial | 3, 3, 3 | 8 |
| Bloom first increase / Condemnation | 3, 5, 5, 5, 3 | 20 |
| Bloom second increase and later | 3, 5, 7, 7, 7, 5, 3 | 36 |

Preserve the diagram masks; do not substitute full 5x5 or 7x7 squares. Clip to board bounds. Deduplicate targets by entity identity, including multi-cell structures. Target eligibility and enemy-kill qualification are different predicates: damaging a neutral obstacle does not itself qualify as an enemy-character kill.

### 2.3 Magic Bullet and upgrades

| ID | Decision |
|---|---|
| M01 | A is selected in the Sniper's cardinal/cross attack range, relative to its current position. There is no absolute forward direction. |
| M02 | B is selected from the two cardinal directions through A perpendicular to Sniper->A, within the Sniper's attack range measured from A. |
| M03 | Distortion (왜곡) adds C: two directions through B perpendicular to A->B, within the Sniper's attack range measured from B. |
| M04 | Base path: Sniper->A, then through B and beyond. Distortion path: Sniper->A->B, then through C and beyond. A and B are the two bends in Distortion; C is not another bend or a stopping point. |
| M05 | Every point must be in bounds, non-wall, within the required range/direction, and reachable from its previous point without crossing a wall. Consecutive identical points are invalid because they define no direction. |
| M06 | Empty cells and cells occupied by allies or enemies are valid point selections. Selection does not require an enemy. |
| M07 | Base and Distortion use 100% attack before existing modifiers. They pierce eligible allies, enemies, and attackable structures. |
| M08 | Each distinct hit entity, including an ally or structure, consumes one encounter in the existing falloff and target-budget calculation. Multi-cell occupancy does not multiply encounters. |
| M09 | Encounter index and remaining budget persist across all bends of the same projectile. They never reset at A or B. |
| M10 | Reuse current PiercingFalloff calculation and rounding. With 25% reduction, the encounter factors are 100%, 75%, 50%, 25%, then 0%; existing budget calculation can stop earlier due to rounded damage or configured maximum targets. |
| M11 | The projectile stops at the allowed target budget, a blocking wall, or the board boundary. A valid selected point may never be reached because the budget is exhausted first. |
| M12 | Unlike ordinary piercing attacks, Base and Distortion pass through an attackable structure even when it survives. A structure hit still consumes one encounter. Wall terrain remains blocking. |
| M13 | Judgment (심판): retain base A/B aiming; hit the first attackable target, allies included, then terminate. Use 150% attack, bypass physical defense, and knock back two cells along the incoming projectile segment. |
| M14 | Judgment does not resume after the victim dies or moves. Non-piercing also applies to structures. Other defensive mitigation and forced-movement immunity are not implicitly bypassed. |
| M15 | Judgment's incoming-segment direction and requested distance of two cells take precedence over other displacement traits. Immunity, blockers, terrain reactions and interruption still apply; two cells is the requested distance, not a guaranteed arrival. |

The candidate path and actual delivered path are distinct public facts. View must not extend the actual path to the last selected point when the projectile ended earlier.

## 3. Approach selection

### 3.1 Selected: extend existing combat requests

Extend CombatView command/preview payloads, map them in existing services, calculate through skill-owned Core code, and publish through the existing transition and result Buffer path.

- Skill geometry and sequencing are owned by the skill.
- Common damage/trait/terrain behavior stays in its existing owner.
- Normal registration and dispatch edits are expected.
- Do not insert the complete skill implementation into the ordinary attack switch or service.
- Do not create a second combat state store or a skill-specific command queue.

### 3.2 Considered: separate skill service forwarding to combat

A separate skill service could offer discoverable entry points while forwarding to the same queue and publication path. It adds a service lifecycle and another forwarding boundary without an established need. The user selected the first approach.

This does not prohibit adding an out-of-battle upgrade operation to the existing character/roster service, whose ownership responsibility already fits the operation.

## 4. Current implementation and concrete gaps

Paths below are relative to Assets/ProjectL/Script/Systems unless otherwise stated.

| Area | Observed source | Reuse and required change |
|---|---|---|
| Input | RuntimeSystem/BufferSystems/CombatView/CombatViewBuffer.cs | Submit, SetPreview, request-number cancellation, handled-command removal, and presentation acknowledgment already exist. Add skill payloads; retain these lifetimes. |
| Command service | RuntimeSystem/InGameSystem/CoreSystem/CombatSystem/CombatCommandService.cs | Maps View kinds, assembles facts, calculates, publishes. Add explicit skill mapping; do not masquerade as Attack. |
| Preview service | .../CombatSystem/CombatPreviewService.cs | Captures latest request and discards replaced requests. Add explicit skill kinds; unknown kinds must not fall into placement handling. |
| Input assembly | .../CombatSystem/CombatInputAssembler.cs | Builds facts from existing Buffer owners. Add pinned skill facts, not a resolver or DB object passed into pure Core. |
| Calculation | .../CoreCaculator/Application/Combat/CombatCommandCalculator.cs | Dispatches commands and runs final reconciliation. Add a skill dispatch that follows the same finalization contracts. |
| Publication | .../CombatSystem/CombatTransitionPublisher.cs and BufferSystems/Core/BattleTransactions.cs | Core transition is prepared/validated before store installation. Include skill usage in the same accepted transition. |
| Ordinary action validation | BufferSystems/Combat/CombatStateBuffer.cs, ValidateActionState | Current validators recognize ordinary move/attack settlements. Add separate skill-use validation; do not forge an Attacked settlement or weaken all command checks. |
| Attack execution | .../Application/Combat/AttackDeliveryExecutor.cs | Shared direct-hit, displacement, arrival, trait, and health stages are available. Existing public preview skips KillChainAttack. Reuse execution stages with explicit simulation behavior. |
| Projection | .../Application/Combat/CommandAttackProjection.cs | Owns temporary world state and one arrival projection. PublicPreview currently disables death traits and movement-use capture in relevant paths. A complete skill forecast must not inherit those omissions. |
| Kill chain | .../Application/Combat/KillChainAttackCalculator.cs | Uses existing random stream and current damage rules, but initial kills are inferred against the command input. Repeated bursts must supply their own fresh cause/baseline to avoid recounting earlier deaths. |
| Piercing | .../Domain/Combat/PierceTargetCollector.cs; .../Application/Combat/CombatAttackDelivery.cs | Current path is one straight ray and stops at live structures. Reuse falloff/eligibility, but provide bent skill traversal and explicitly different structure stopping. |
| Damage | .../Application/Combat/CombatDamageCalculator.cs; .../Domain/Combat/TargetDamagePipeline.cs | Current physical-defense bypass and later incoming defenses are distinct. Judgment must bypass only the defense stage. |
| Forced movement | .../Application/Combat/AttackForcedMovement.cs; AttackKnockbackCalculator.cs | Existing blocker, terrain, object, immunity, and interruption rules apply. Supply the incoming segment direction; existing actor-to-target direction is insufficient after a bend. |
| Statuses | .../Contracts/CombatEffects/StatusEffectKey.cs; AppliedStatus.cs; .../Domain/CombatEffects/Rules/AppliedStatusRules.cs | Stun is not currently a status key. ActionRestrictionStatus accepts Bind/Disarm only. Add a real Stun restriction and update its consumers, not an unrelated buff or a hidden pair of independent statuses. |
| Main / Run | .../Contracts/Roster/OwnedCharacterRecord.cs; BufferSystems/Character/CharacterBuffer.cs; BufferSystems/Core/ProgressTransactions.cs | Reuse owned identity, roster revision, prepare/install changes, and new-Run roster clearing. Extend preservation/equality paths for the upgrade value. |
| Definitions | DataBaseSystem/Data/InGame/Table/Unit/Item/UnitDataSO.cs and UnitData.cs | Use existing unit keys and pure authored facts. A skill table owns skill-to-Main binding and stage limits. |
| Composition | Factory/Runtime/InGame/Core/CombatSystemFactory.cs -> InGameCoreSystemGroup | Existing shared assembler/publisher and command/preview services remain. No new root service registration or Framework dependency is needed. |
| Tests | Assets/Tests/EditMode/AttackPreviewParityTests.cs; KillChainAttackTraitTests.cs | Existing tests deliberately preserve legacy public-preview omissions. Do not rewrite them to pretend all previews were always complete. Add skill-specific parity evidence while retaining legacy expectations. |

### 4.1 Important baseline findings

- The inspected Akimbo asset uses MainCharacter role; the inspected Sniper asset currently has Regular role with Main category. Production Sniper activation requires an explicit data audit and correction to the intended Main definition. Do not weaken roster validation or silently grant a skill to a Sub to compensate.
- The current general public forecast is intentionally not a complete simulation of every live consequence. Sharing its entry method alone does not establish skill forecast parity.
- Several existing trait predicates explicitly enumerate OrdinaryAttack, ExtraAttack, and ReactionAttack. A new skill damage kind cannot be added without auditing those predicates; otherwise effects such as dust mitigation or bleeding would silently disappear.
- Current health claim/scope constructors recognize specific attack/roar/child-attack events. Adding a skill declaration without extending the evidence readers would produce a semantically incomplete result even if final HP looked correct.
- Existing public forecasts filter hidden environment facts. The confirmed policy preserves that secrecy and exposes forecast limitations; see section 7.3. Implementing full known-information consequences is still required.

These are source findings, not reproduced bugs or test results.

## 5. State ownership and configuration

### 5.1 Authoritative ownership

| Fact | Authoritative owner | Readers and lifetime |
|---|---|---|
| Main definition -> skill mapping; stage limit | New PlayerActiveSkill table in InGame DB | Core definition preparation only; immutable authored data. View receives projected keys and limits through Buffers. |
| Chosen upgrade | OwnedCharacterRecord in CharacterBuffer, keyed by owned Main instance | Run lifetime. Preserved through level/growth/specialization copies and stage transitions. Cleared by genuine new-Run roster creation. |
| Bound battle definition | Existing battle preparation/lifetime facts | Immutable snapshot of skill definition, selected upgrade, and owning Main for this battle. Not an independently mutable second definition. |
| Remaining stage uses; last used player-turn sequence | A compact PlayerSkillBattleState in CombatCommandState, stored by CombatStateBuffer | Initialized on accepted battle preparation. Updated in accepted skill transitions only. |
| Selected points and current hover | View interaction state / immutable request payload | No gameplay authority. Cancellation discards this selection. |
| Burst ordinal, pending reactivation, encountered entity IDs | One skill calculation's local state | Discard after calculation; never persisted into Run/turn Buffers. |
| Preview result | Existing preview response storage | Derived, request- and source-stamped, replaceable; never a commit source. |
| Published effects/result history | Existing combat result Buffer/history | Same acknowledgment and cleanup as other commands. |

Do not copy HP, occupancy, traits, random state, or terrain into a new persistent skill store. Existing projected facts are valid transient calculation inputs, not additional authorities.

For multiple owned Main instances, the proposed engineering choice is to keep upgrade selection on the owning instance; only the currently selected battle Main can cast. This follows current roster identity and prevents changing the selected deck Main from transferring an upgrade to a different character. It does not introduce multi-Main simultaneous casting.

### 5.2 Definitions

Add a small PlayerActiveSkillData table using the existing Item SO -> fresh pure Data -> Query -> Core facts pattern.

Each row owns:

- Skill key and supported base kind.
- Owning Main unit definition key; the SO may reference a Unit SO, but GetData returns only the key.
- StageUseLimit, default 1, positive integer.
- The supported two upgrade keys, validated against the skill kind.

Keep the fixed, approved masks and effect behavior in their skill owner. Do not build a generic effect-graph engine or expose arbitrary unsupported combinations through authoring. Existing attack range, attack strength, defense, trait ranks, and pierce settings remain their existing owners' facts. No duplicated hard-coded Sniper range of 3 or duplicated 25% formula.

Register the new table in InGameDataBaseFactory and expose it through the existing InGame query context. Validate duplicate skill keys, duplicate Main bindings, unsupported branches, invalid limits, and the bound unit's intended player/Main eligibility. Catalog wiring and production role correction are explicit activation work, not assumed complete.

### 5.3 State transition rules

- A cast validates the active Run, battle identity, player combat turn, selected Main, alive/field presence, skill binding, status restrictions, budget, and point validity.
- Validation failure produces a handled rejection with no gameplay mutation, skill charge, RNG advance, or ordinary-action settlement.
- Acceptance reserves the one use inside the transient calculation and commits it with all resulting state in one existing publication operation. No early live decrement.
- Automatic child effects do not re-enter the command queue or consume a second use.
- Maintain LastUsedControllerTurnSequence and compare it with the existing sequence. No separate turn clock.
- Preserve PlayerSkillBattleState in every CombatCommandState.With/copy/composition operation and compare it in SameValues/SameGameplayValues. A default constructor parameter must not erase it during unrelated moves or automatic turns.
- Upgrade changes use existing RunId/roster revision and prepare/install discipline. Revalidate no active battle and an unchanged selected Main before installation.
- Upgrade selection is one-way from None to a valid branch. A repeated or conflicting selection returns AlreadyUpgraded; it does not reset usage or silently replace the branch.
- Main death/battle termination follows existing finalization. Do not invent a new Run exit policy.

## 6. Calculation boundaries

Suggested names describe the intended ownership; final file splits can be settled during implementation if these boundaries remain intact.

### 6.1 PlayerSkillCalculator

Owns admission, skill dispatch, one isolated calculation lifetime, one use settlement, and construction of a single final skill transition. It receives immutable Core contracts and existing calculation collaborators. It does not resolve services, read Systems Buffer implementations, or call Unity.

The ordinary command dispatcher adds one case. It does not learn Bloom mask arithmetic, C-point geometry, or reactivation conditions.

### 6.2 PowderBlossom calculation

Owns exact masks, non-allied target policy, direct-burst enemy-kill qualification, expansion, and one pending reactivation flag.

For each burst:

1. Read the current projected caster and environment. Stop new bursts if the caster is dead/not on the field or the battle is terminal.
2. Capture that burst's attack scope and eligible targets using the current anchor and approved mask.
3. Deliver the scaled attack through common damage, health, defense, trait, and obstacle reaction paths.
4. For Condemnation, apply Stun to surviving eligible direct recipients using the common status application path, with no reactivation.
5. Qualify direct enemy-character deaths from this burst's cause/health execution. Set a single pending reactivation flag if appropriate.
6. Complete existing eligible KillChainAttack consequences before starting a pending next burst.
7. If pending and still active, advance the Bloom mask at most to stage 2 and repeat.

A fresh burst is a child attack scope, not a fresh live command. Use the existing projection/current-input mechanisms for causal continuation; never calculate the second burst from the original unmodified world.

**Critical integration change:** KillChainAttack currently discovers its initial work by comparing command input with projected deaths. The skill integration must pass a per-burst baseline or explicit qualified seed deaths to the existing kill-chain routine. Reusing the original command baseline for every burst would count already resolved deaths again. Keep the old ordinary-attack default behavior unchanged and test both callers.

Direct Powder Blossom deaths and downstream trait/environment deaths must retain distinct origins. Enemy deaths from KillChainAttack, a structure explosion, reflection, or terrain do not become direct-burst kills merely because the caster is credited. This is the proposed narrow interpretation of “the skill itself kills”; approval of the design should confirm this causal boundary.

No arbitrary cap may silently truncate a legitimate chain. Preserve terminal/actor checks, one-use semantics, existing death/respawn limits, and bounded/yielding execution discipline. If a configured interaction can create a nonterminating chain, report the unsupported rule explicitly; do not publish a silently partial success.

### 6.3 MagicBullet calculation

Owns point validation/candidates, ordered cardinal segments, hit traversal, bend direction, projectile-level deduplication/budget, and skill-specific structure pass-through.

- Candidate calculation and execution use the same pure point validator.
- Wall reachability is enforced for each segment regardless of general attack height-based wall bypass. That is the confirmed skill rule, not a change to ordinary attacks.
- The current effective Sniper attack range is resolved once for the query/cast snapshot and used for A, B, and C distances; do not sample a hypothetical attack-range modifier at a bend as though the caster moved there.
- Traverse segments in physical order. Include a bend cell once, not as both the preceding endpoint and the next segment's first hit.
- Keep one encountered-entity set for the whole projectile. Multi-cell entities and a later segment touching the same entity do not produce extra encounters.
- Keep one encounter index and initial target budget for the projectile. Do not recalculate a fresh budget at a bend.
- Distinguish geometry, encounter eligibility, and stopping. A blocked/non-attackable object is not automatically a damage target.
- Attackable non-wall structures do not stop Base/Distortion. Existing wall terrain and boundary geometry remain authoritative.
- Reuse current falloff and target-budget calculation, including its rounding, rather than reconstructing it from UI damage labels.
- Judgment terminates at its first eligible encounter and supplies the incoming segment vector to forced movement. It does not reuse caster-to-victim direction after a bend.

Proposed engineering defaults: retain current target eligibility for neutral/attackable objects; non-wall non-attackable objects are not invented as new blocking terrain. Skill requests cannot change the caster's rotation/position merely by selecting points.

### 6.4 Shared attack stages, without ordinary-action impersonation

A skill must have a distinct command identity and use settlement. Do not submit it as Attack and repair the action counter afterward.

A small explicit calculation policy should distinguish:

- Damage/attack family and source identity.
- Whether an ordinary action permission is settled.
- Whether the calculation simulates complete causal consequences.
- What information may be published to the observer.

These are different facts. The existing PublicPreview boolean currently influences several of them; do not propagate that conflation into new skill code.

**Proposed damage classification:** append PlayerSkillAttack to CombatDamageKind and add a narrow shared predicate for attack-family effects where the current rule means direct/extra/reaction attack damage. Keep genuinely ordinary-command-only behavior explicit. This requires a targeted audit of current exact-kind checks, not an indiscriminate replacement of every OrdinaryAttack check.

| Concern | Required skill behavior |
|---|---|
| Physical defense, category matchup, incoming/outgoing damage, held shields, damage redirection | Existing attack-damage stages; Judgment bypasses physical defense only. |
| Eligible damage/contact/kill traits | Include skill hits and retain source/recipient distinctions. Child KillChainAttack remains ExtraAttack. |
| Ordinary movement/attack permission | No settlement or reset. |
| Ordinary-command-only equipment/action-completion triggers | Excluded by D13. Inventory/Artifact-specific predicates must be enumerated during the hook audit so genuine common incoming-hit effects are not accidentally excluded. |
| Attack-triggered trait notifications/counters | Use actual attack scopes, not spent ordinary action count. Proposed: each actual burst and existing extra attack notifies once; candidates/preview publication do not notify live state. |
| Death, rewards, souls, environment reactions | Common projected processing and one outer finalization/publication. |
| Health and movement provenance | Existing causal facts, extended explicitly where a new skill source is required. No synthetic TraitKey masquerading as the skill. |

The user confirmed the common-effect versus ordinary-action-only boundary on 2026-11-15 (D13). The source hook audit remains an implementation deliverable: map each existing predicate to that boundary, preserve effect-specific conditions, and report a genuinely unclassifiable rule instead of silently broadening it. Do not ask the user to approve the same general boundary again.

The inspected checks below apply the confirmed boundary. The matrix specifies the required treatment; implementation still needs source inspection and regression evidence:

| Current check | Primary-skill treatment under D13 | Preserved distinction |
|---|---|---|
| BleedingTraitRule, DustScreenTraitRule, LavaCannonApplication enumerate ordinary/extra/reaction attacks | Include PlayerSkillAttack in the relevant attack-family predicate | Environment and periodic damage do not become attacks. |
| SacrificeDamagePlanner checks the same attack family for attack-specific target calculations | Include PlayerSkillAttack and retain original target versus actual recipient evidence | No separate skill-only sacrifice formula. |
| CombatDamageApplication has a complementary non-attack branch | Classify PlayerSkillAttack consistently with the shared attack stages | Prevent a skill from running both attack-specific and non-attack completion paths. |
| HuntersMarkTraitRule admits ordinary/extra attacks, excludes reaction attacks | Include an actively cast skill under the confirmed contact-effect policy | Reaction exclusions remain unchanged; do not replace its predicate with an all-attacks predicate. |
| DeadHandEffectRule explicitly scales ordinary attacks; pull/sweep geometry is selected separately | Do not inherit ordinary-attack geometry or its shape-specific multiplier | A skill does not unexpectedly become a movement attack. |
| AttackDeliveryExecutor gates ThornHeart reflection on OrdinaryAttack | Include skill hits under the confirmed defensive-reflection policy, in both complete forecast and execution | Reflection remains reflected damage; prevent recursive relabeling as a skill hit. |
| AttackDirectDamageCalculator records ItemDirectHits for OrdinaryAttack | Audit each consuming item policy; extend generic incoming-hit effects, retain explicit ordinary-action-only exclusions | Do not convert every item hook with a global search/replace. |

This inventory is not a promise that an enum addition alone is sufficient. Search all readers, switch expressions, evidence constructors, and serializers during implementation. The existing behavior of every other damage kind must stay unchanged.

## 7. Forecast/execution agreement

### 7.1 One calculation, two consumers

For equal authoritative facts, equal selected points, equal rule/configuration snapshots, and equal random-state input:

    ExecuteSkillCore(input)
        -> accepted/rejected decision
        -> complete projected final facts
        -> ordered causal events and health/movement evidence
        -> skill geometry, hits, children, use settlement

    Forecast -> convert that computation to a read-only forecast; do not install
    Execute  -> validate and install the transition through existing Buffer publication

This is a semantic contract, not a proposal for a second persistent simulation engine. Reuse CommandAttackProjection/MovementProjection and existing helpers. Introduce only the policy parameters needed to run full effects in the transient skill calculation without changing legacy ordinary previews.

Full skill forecasting must simulate KillChainAttack, death traits, terrain/object entry, forced movement interruptions, shields/consumables, and random choices in the local projected world when those effects are applicable. It must not call a helper with PublicPreview=true and assume that all effects ran.

### 7.2 Randomness and input identity

- Read current existing RNG progress into the calculation; advance only the local projection during forecasting.
- Candidate enumeration must be stable where existing rules depend on order.
- Repeated forecasts of the same request/source produce the same result and do not reroll live state.
- Execution recalculates from current facts. A forecast is not a reservation of an outcome and is never accepted as a transition supplied by View.
- Stamp responses with existing BattleInstanceId, ControllerTurnSequence, QueryRevision, request number, and owning actor/selection. No new global version/lock system.
- Discard responses if request/source is replaced during work. A stale response must not authorize a later cast.
- A submitted command carries the expected battle/turn/source stamp. A changed source returns SourceChanged without consuming the use; View may refresh and resubmit.

### 7.3 Full simulation versus information visibility

Full causal simulation and public visibility are separate requirements.

Current public attack previews omit hidden environment facts and some consequences. This design does not silently reveal hidden information or silently call a partial forecast complete.

**Confirmed on 2026-11-15 (Q1 / D12):** preserve hidden-information rules and explicitly communicate forecast limitations. Do not expose hidden consequences through the skill forecast.

Required policy:

- Internal full-information parity tests compare complete simulation with execution.
- Public forecasting uses the approved observer information policy and simulates all consequences available under that policy.
- Publish a conservative known-information/conditional coverage label without leaking hidden object positions, identities, RNG branches, or even a secret-presence flag. Its selection must follow public information policy, not a secret-dependent test such as whether an undisclosed mine is actually present.
- Do not show an exact authoritative final HP/path while merely hiding the event that caused it.
- Whole-forecast completeness and visibility limitations are explicit contract fields. No silent omission of known KillChainAttack or known forced arrivals.
- Keep existing non-skill preview behavior unchanged.

For two worlds with the same permitted public facts but different hidden effects, the public forecast must remain indistinguishable except for already-permitted opaque source bookkeeping. Withhold or project hidden-dependent HP/path/outcome details instead of exposing exact authoritative values and hiding only their cause. Complete internal forecasts still match execution for equal full facts/RNG; public forecasts promise complete simulation of known information, not exact outcomes in the presence of hidden information. This distinction is part of the API contract, not an implementation failure or permission to omit known effects.

## 8. Consistent result semantics and integration safeguards

This section addresses the user's illustrative concern. It is not a diagnosis of the existing AI incident.

### 8.1 Command, effect, movement, and damage are not interchangeable

| Fact | Meaning |
|---|---|
| CommandKind = UsePlayerSkill | The player's accepted top-level intent. |
| Skill key/variant/cast ordinal | The skill identity and the local causal sequence. |
| DamageKind = PlayerSkillAttack / ExtraAttack / EnvironmentDamage | The actual damage family. A terrain reaction is still environment damage. |
| CombatMovementCause.Agency = Forced | Actual forced displacement, even when caused by a skill. |
| Trait or skill provenance | Why a common event happened; it does not replace the event's movement/damage meaning. |
| SkillUseSettlement | One skill-use mutation; never UnitActionUse.Attacked. |

A Judgment knockback entering dust, thorns, weak ground, a mine, or a tornado must still emit the appropriate existing movement and environment events. Terrain changes or trail consumption do not change that movement into a different top-level command.

Use existing CombatMovementCause with Forced agency. If View needs the skill source, add optional skill provenance to the relevant result/cause contract while preserving agency and existing trait provenance. Do not create a fake trait or a second “skill movement” event family for the same relocation.

A projectile path is not character movement: it does not relocate the caster, consume movement, or trigger tile-entry effects along the projectile cells.

### 8.2 Single causal event stream

Propose a skill declaration/event payload that wraps the skill identity, path/burst metadata, and attack results. Reuse existing target damage, knockback, status, environment, and child-attack records. The exact event wrapper must be integrated with:

- CombatCommandTransition and CombatResult payload validation/copying.
- CombatHealthScope / CombatHealthClaim occurrence matching.
- CombatChangeEvidenceReader and health/change consistency checks.
- MovementEffectUse capture and MovementUndoPolicy reconciliation.
- Battle/Character store validation, completion, reward, and result-history publication.
- Public forecast projection and eventual View event routing.

Do not copy events during result wrapping while leaving health claims pointing at obsolete event instances. Do not double-publish the same HP loss in both a skill summary and a standalone damage operation.

The skill result exposes child occurrences in causal order, actual recipients after redirection, source identity, physical impact position, and incoming projectile direction. View must not reconstruct those from final positions.

**Result-shape choice:** current AttackResult success construction requires a UnitActionUseSettlementSnapshot and uses a reaction exception for unchanged action use. A player skill must not pass reaction=true merely to bypass that requirement. Introduce a PlayerSkillAttackResult containing the existing AttackHitResult, damage breakdown, trait/category snapshots, affected cells, and knockback records, without an ordinary-action settlement. Add a CombatPlayerSkillAttackEvent for each actual burst/projectile; preserve existing child-attack events for KillChainAttack. The outer PlayerSkillResult groups these same immutable occurrences and the one skill-use settlement. Extend health scope/claim recognition for the new primary occurrence explicitly. Extract only the shared hit-summary construction needed from AttackResultFactory; do not duplicate the damage pipeline or rework all ordinary result types.

### 8.3 Forced movement and terrain

Reuse existing one-cell-at-a-time forced displacement:

- Wall, board edge, terrain admission, living occupancy, immunity, interrupted movement, and terminal rules remain common.
- Judgment requests distance 2 along the incoming segment. Do not teleport directly to an endpoint.
- Terrain/object reactions run at their existing arrival phases and can stop or redirect movement.
- No Judgment-only collision damage is added. Existing independently eligible collision effects must retain their own source.
- A death event is not a move event; preserve existing dead-recipient/death-anchor behavior.
- Record actual path, requested distance, completed steps, and stop reason for forecasting and playback.

**Confirmed on 2026-11-15 (Q3 / M15):** Judgment overrides competing displacement direction, mode and distance with one incoming-segment, two-cell request. Do not sum distances, take a longer trait distance, reject the skill because of a conflicting trait displacement mode, or run a second independent move. Normalize the effective request before the shared displacement calculation; preserve ordinary callers' existing composition behavior. Common immunity, occupancy, wall/boundary admission and terrain interruptions may reduce or redirect the actual travel through their existing rules. Any independently eligible collision effect keeps its own source and conditions; it cannot replace Judgment's movement request.

### 8.4 Publication and completion

- Extend validators to recognize a real skill outcome; do not catch and suppress validation failures to publish it.
- Rejected requests may advance handled request bookkeeping, but no gameplay state or RNG.
- Accepted initial cast consumes exactly one use even if no targets, a target survives, a child is interrupted, or the battle ends during the action.
- One top-level command produces one transition and one result sequence. Children remain ordered occurrences inside it.
- Existing battle completion handles Run outcome, rewards, souls, and exit policy. Never finalize after each burst and then continue a supposedly completed battle.
- Existing movement-undo reconciliation consumes the actual evidence. Preserve unrelated valid checkpoints; invalidate dependencies actually used or changed by the skill. Do not invent a global “all moves become different” flag.

## 9. Proposed public contracts and View usage

Names in this section are proposed API names, not claims that the types already exist. All payloads exposed across assemblies belong in Core.Contracts or existing Systems input contracts; no Core.Application/Domain types leak to View.

### 9.1 Query and command surface

Use existing ICombatViewBufferWriteSink.SetPreview / Submit and existing result readers.

| Operation | Proposed payload | Returned information |
|---|---|---|
| Skill availability | PlayerSkillAvailability request for battle Main | Skill/variant key, owner IDs, stage limit/remaining uses, turn-used flag, CanUse, typed failure, source stamp. |
| Point candidates | PlayerSkillSelection with ordered confirmed points | Valid prefix, required/selected point count, next candidate cells, input completion, blocked diagnostics allowed by visibility, source stamp. |
| Full forecast | PlayerSkillForecast with complete points; empty point list for Powder Blossom | Accepted/rejected forecast, complete causal result projection, hit/path/status/movement facts, projected use settlement, coverage, source stamp. |
| Execute | UsePlayerSkill with complete points and expected source | Existing numbered command result plus PlayerSkillResult / PlayerSkillUseSettlement. |
| Upgrade availability/application | Existing character/roster service operation | Current skill/branch options and typed upgrade application result using RunId/roster revision; no active battle. |

For candidates, points=[] returns A for Magic Bullet; [A] returns B; Distortion [A,B] returns C. Base [A,B] and Distortion [A,B,C] are complete. Extra points are invalid. Powder Blossom requires zero points.

View can include a hover point in a forecast request without committing it locally. Incomplete requests return selection information rather than executing damage. Decide exact overloads in implementation; do not introduce server-side selection sessions.

### 9.1a Concrete proposed signatures

These are the proposed integration surface, not compilable code delivered by this task. Add factories to the existing input DTOs, preserving their request-number issuance through the Buffer:

    CombatViewPreviewData.ForPlayerSkillAvailability(EntityId actorId)
    CombatViewPreviewData.ForPlayerSkillSelection(
        EntityId actorId, IReadOnlyList<CellCoord> selectedPoints)
    CombatViewPreviewData.ForPlayerSkillForecast(
        EntityId actorId, IReadOnlyList<CellCoord> completePoints)
    CombatViewCommandData.ForPlayerSkill(
        EntityId actorId, IReadOnlyList<CellCoord> completePoints,
        PlayerSkillSourceStamp expectedSource)

PlayerSkillSourceStamp bundles existing identity values; it is not a new revision authority:

    long BattleInstanceId
    long ControllerTurnSequence
    long QueryRevision
    EntityId ActorId

Add three mutually exclusive payloads to CombatPreviewResult:

    PlayerSkillAvailability SkillAvailability
    PlayerSkillSelectionResult SkillSelection
    PlayerSkillForecast SkillForecast

Each payload includes Source. Availability includes SkillKey, UpgradeKey, MainInstanceId, RemainingUses, StageUseLimit, UsedThisTurn, CanUse, and FailureReason. Selection includes an immutable SelectedPoints copy, RequiredPointCount, PrefixIsValid, IsComplete, NextCandidateCells, and FailureReason. It must never treat a partially selected Magic Bullet as a valid cast.

Forecast includes CanUse, FailureReason, Coverage, and the read-only PlayerSkillResult prediction. Final execution attaches PlayerSkillResult and PlayerSkillUseSettlement to the existing CombatResult, retaining CommandKind, command number, result sequence, and ordered Events. The forecast prediction carries no installable Buffer handles or callbacks.

Add out-of-battle operations to the existing ICharacterRosterService, accessed through CoreGroup.Characters:

    PlayerSkillUpgradeAvailability ReadPlayerSkillUpgradeAvailability()
    PlayerSkillUpgradeApplyResult TryApplyPlayerSkillUpgrade(
        RunId expectedRunId, long expectedRosterRevision,
        CharacterInstanceId mainId, PlayerSkillUpgradeKey selectedBranch)

The result supplies Succeeded, FailureReason, owning Main, skill/branch, and final roster revision. The operation validates and applies only the upgrade. It does not invent a price, spend currency, grant a boss reward, or permit an arbitrary View-provided skill definition. A future acquisition owner composes its own validated grant/payment with this operation through the existing preparation boundary; it must not charge separately and then ignore a failed application.

Example View integration, with buffer/service references already obtained through the approved group context:

    var request = CombatViewPreviewData.ForPlayerSkillSelection(mainEntityId, points);
    int requestNumber = combatInput.SetPreview(request);
    // 같은 요청 번호와 Source의 응답만 화면에 반영한다.
    // 완성된 선택의 미리보기를 받은 뒤 사용자 확정 시 제출한다.
    int commandNumber = combatInput.Submit(
        CombatViewCommandData.ForPlayerSkill(mainEntityId, completePoints, forecast.Source));
    // 해당 명령의 결과를 읽고, 연출 완료 뒤 기존 결과 순번을 승인한다.
    combatInput.AcknowledgePresented(result.Sequence);

The sample's Korean comments follow repository code-comment conventions; all design prose is English. Receiving a result is not sufficient to acknowledge presentation before playback actually completes. On cancellation before submission use ClearPreview(requestNumber), not the parameterless clear that may erase another request owner's preview.

### 9.2 Result content

PlayerSkillResult should expose, directly or through existing nested contracts:

- Skill key, upgrade key, casting Main identity, submitted points.
- Ordered burst/projectile/child occurrences and their causal relationship.
- Exact affected cells and actual projectile path; separate selected path from delivered path.
- Per-entity hits, encounter ordinal, falloff factor, current damage breakdown, expected and final HP where publishable.
- Actual recipient and original target when defenses redirect damage.
- Applied/expired statuses and their remaining affected-side duration.
- Existing knockback/forced-movement snapshots and terrain/object outcomes.
- Actual termination reason: wall, board edge, target budget, Judgment hit, or causal interruption.
- One use settlement with before/after remaining uses and turn sequence.
- Existing stage outcome and final state publication through the outer result, not an independently applied skill state.

Use existing immutable result records where semantics match. Do not give View a dictionary of untyped effect strings requiring its own rule interpreter.

### 9.3 Failure taxonomy

Expected failures return typed reasons and normal result bookkeeping. Invalid internal composition throws before installation.

Suggested skill reasons:

- NoActiveBattle, NotPlayerCombatTurn, NotSelectedMain, ActorUnavailable.
- NoSkillConfigured, Disarmed, Stunned, StageUsesExhausted, UsedThisTurn.
- WrongPointCount, PointOutsideBoard, PointOnWall, PointOutOfRange, InvalidSegmentDirection, SegmentBlockedByWall.
- SourceChanged / BattleChanged.

NoTargets is deliberately absent. Do not collapse every rejection into “cannot attack,” because ordinary action exhaustion is not a skill restriction.

Upgrade reasons include NoActiveRun, InBattle, InvalidMain, NoSkillConfigured, InvalidBranch, AlreadyUpgraded, and SourceChanged. Configuration corruption is not a normal empty-success result.

### 9.4 View sequence

1. Read selected Main and request skill availability through the combat preview path.
2. Enter selection only when available; keep selected points in View.
3. Query candidate points using the latest prefix. Display returned cells rather than calculating perpendicular directions or wall visibility in View.
4. Request full forecast for the complete tentative selection. Check request/source stamp and coverage before displaying it.
5. On confirmation, Submit the complete skill command. Prevent duplicate local submission using the existing pending-command mechanism.
6. Await its handled result. On rejection, refresh state and display the typed reason; do not decrement a local gameplay counter.
7. On success, consume ordered result evidence for playback, then acknowledge the existing result sequence.
8. On cancel before Submit, clear only the owned preview request number and discard local points. Clearing a preview after submission is not command cancellation.

Provide an implementation handoff containing actual signatures, sample calls, reply/result examples, failure handling, cancellation, source invalidation, presentation acknowledgment, and required new event handlers. UI construction is not part of this Core delivery.

## 10. Feature locality and architectural checks

### 10.1 Representative changes

| Real change case | Current pressure / risk | Minimum design response | Expected change / understanding / verification scope |
|---|---|---|---|
| Modify Bloom expansion or reactivation | Reusing ordinary command orchestration can recount deaths, spend ordinary action use, or require knowledge of unrelated attack shapes. | Skill-owned burst control; existing attack execution with explicit per-burst cause and unchanged shared stages. | Change Powder Blossom rules/masks and focused fixtures. Understand common attack-scope and death-seed contract, not Magic Bullet selection or AI internals. Verify kill-chain order, direct-kill qualification, forecast parity, and shared damage hooks actually touched. |
| Make Magic Bullet pierce structures and bend | Ordinary ray collector stops at structures and assumes one straight direction. A global change would alter ordinary attacks/AI. | Skill-owned segment traversal; reuse eligibility/falloff and common hit delivery; pass actual incoming direction. | Change Magic Bullet geometry/policy. Understand encounter and delivery contracts, not Firework sequencing. Verify existing straight piercing unchanged, bend/structure/limit cases, and displacement integration. |
| Add full skill forecasts | Existing public-preview flag filters information and skips causal effects. Returning a different event shape may violate health/movement validators. | Full local skill simulation shared with execution; separate observation policy and installation; explicitly extend common evidence readers. | Changes cross shared execution/evidence boundaries for real reasons. Understand simulation policy, projection, and publication contracts. Retain necessary Core/CoreIntegration and unchanged legacy-preview regression coverage. |

These are design expectations, not measured improvements. File count alone is not an acceptance measure.

### 10.2 SOLID / SSOT / Framework assessment

- **SRP:** skill owners calculate their geometry/sequence; shared stages calculate their existing effects; services route requests; Buffer owners validate/install; View displays.
- **OCP:** add one explicit skill dispatch and contracts. Normal registration edits are allowed; no reflective registry or universal plug-in engine is justified.
- **LSP:** a skill result must satisfy the same meaningful transition/evidence contracts. It must not pretend to be a Move/Attack result while violating that type's conditions.
- **ISP:** typed skill query/result payloads expose the information View needs without a whole mutable battle context.
- **DIP:** pure Core uses contract facts and existing calculation interfaces; no Framework resolver, Unity objects, or Systems Buffer implementation.
- **SSOT:** authored mapping, persistent upgrade, battle use, and transient sequence each have one owner. Forecasts and read models are derived.
- **FW-001/002/003:** register through existing local builders/group exports; lifecycle modules only where needed; build once.
- **Composition path:** DB factory -> InGame query context -> existing initialization; CombatSystemFactory -> core group -> existing command/preview services -> Buffer query. No new root export shortcut.
- Preserve current assembly boundaries. Do not introduce a new Systems->Core.Application/Domain dependency as the reason this feature works; do not opportunistically clean up existing references in this task.
- Required shared validation remains shared. Feature independence is not permission to skip required integration checks.

## 11. Work packages for the later implementation plan

These are design dependencies, not an executable implementation plan or permission to start coding. After design approval, prepare a separate Task plan and stop/report at each agreed Phase.

1. **Contracts and ownership:** skill/upgrade definitions, state lifetimes, typed requests/results/failures, Stun representation, definition and copy/equality preservation.
2. **Shared semantic seams:** complete transient simulation policy, damage-family hook audit, skill health/event evidence, separate use settlement and publication validation. Preserve ordinary behavior with regression evidence.
3. **Powder Blossom:** masks, direct-kill qualification, KillChainAttack ordering, Bloom and Condemnation, full forecast parity.
4. **Magic Bullet:** shared candidate/execution validation, bent traversal, structure piercing, cumulative budget, Distortion and Judgment, common forced displacement.
5. **Systems/Buffer integration:** input/preview/result forwarding, source stamps, upgrade application, battle/Run resets, DB catalog activation.
6. **Handoff and end-to-end verification:** concrete View API documentation, fixture-backed integration, remaining Unity/manual checks.

Do not implement an earlier feature with throwaway duplicated damage or movement code intending to merge it later. Establish only the shared seams actually required by these two skills.

## 12. Verification design

### 12.1 Parity oracle

For a test-owned, fully known immutable input and fixed existing RNG state:

- Run the complete skill forecast and skill execution independently.
- Assert input facts and live Buffer state remain unchanged by forecasting.
- Compare acceptance/rejection, use settlement, final gameplay facts, RNG progress, ordered semantic events, actual recipients, status durations, occupancy/terrain, health claims, forced movement stop reasons, and terminal outcome.
- Exclude only documented outer request/publication identifiers or visibility projection. Do not ignore event kinds, origins, RNG, or child consequences merely to make equality pass.
- Validate the executed transition through real Buffer preparation/publication in CoreIntegration. Equal calculator output alone does not prove publication accepts it.
- Repeat forecasts and issue unrelated/stale requests to prove no reroll, hidden live mutation, or stale-response installation.

Where public visibility is conditional, compare complete internal simulation separately and assert the public projection neither claims certainty nor leaks prohibited facts. Do not change golden references to bless an unexplained difference.

### 12.2 Required scenarios

| Area | Cases and invariant |
|---|---|
| Ownership | Akimbo/Sniper selected Main; Subs have no effect; wrong Main/actor rejected; specialization does not reassign skill; unsupported Main reports no skill. |
| Use | After ordinary attack; before ordinary attack; Bind allowed; Disarm/Stun blocked; repeated same turn; exhausted stage; empty valid cast consumes one; invalid/cancel consumes none; duplicate/stale submission cannot double-spend. |
| Reset | New turn, new battle, repeated preparation, stage transition, scene rebind, new Run, owned-record growth/specialization copies, immutable battle definition. |
| Powder Blossom | 8/20/36 exact masks, board clipping, walls ignored, allies excluded, non-allied structures, same multi-cell target hit once per burst. |
| Chain | Multiple direct kills schedule one next burst; several KillChainAttack kills do not add bursts; fresh per-burst seeds; no old-death recount; attack modifiers change during chain; actor/terminal interruption; Condemnation still permits eligible traits. |
| Stun | Affected-side expiry, no additive duration, longer remaining duration preserved, normal move/attack and skill admissions agree, death/reserve cleanup follows shared lifecycle. |
| Selection | Four initial cardinal directions, both perpendicular choices, B/C range measured from previous point, occupied points, zero-length/out-of-board/wall/blocked segments, cancelled/replaced prefix. |
| Projectile | Hit at bend counted once; multi-cell target deduplication across segments; ally and structure encounters consume budget; live structure passthrough; wall terrain stop; early target-budget stop; existing ordinary structure stop unchanged. |
| Judgment | First eligible target only, 150% before existing mitigation, physical defense bypass only, final-segment direction, immunity/blockers, two-cell request, no continuation after target death. |
| Interaction | Knockback through dust/thorns/weak ground/mines/tornado or applicable fixture terrain; structure explosion; reflection/redirection/shields; death traits; actual event identities and origins remain valid. |
| Full forecast | KillChainAttack and death/arrival consequences included; local RNG and consumable effects; candidate queries never run damaging effects; full-information terminal outcome agrees; equal public worlds with differing hidden effects cannot be distinguished through public forecast data or coverage. |
| Publication | Correct skill command identity; no ordinary action settlement; one result sequence; complete health/movement evidence; existing completion/reward/undo reconciliation; rejection purity. |
| Handoff | Buffer-only View usage; typed reason/source checks; no DB rule lookup; playback consumer can route skill occurrences and existing movement/status events without reinterpreting them. |

### 12.3 Existing suites to reuse

Inspect and retain relevant coverage from:

- AttackPreviewParityTests, AttackPreviewResumptionTests, CombatCommandInputCopyTests.
- KillChainAttackTraitTests and KillChainSacrificeRecipientRegressionTests.
- PiercingFalloffTraitTests, PiercingShotCommandTests, PiercingShotTraceTests, PiercingShotContractTests.
- ForcedDisplacementTests, ForcedDisplacementPathTests, EnvironmentTerrainRuleTests, PullTerrainTests, DeadHandTerrainTests.
- CombatCommandCalculationTests, CombatCommandEffectTests, MainSpecialization tests, and relevant CoreIntegration publication fixtures.

The current KillChainAttackTraitTests explicitly expect legacy preview to omit extra attacks. Preserve that expectation for legacy calls; add complete skill-forecast coverage separately. If shared evidence or DamageKind changes intentionally affect a legacy contract, investigate and obtain scope agreement rather than modifying expected output blindly.

New tests belong to Core/CoreIntegration, not View. Use test-owned definitions and catalogs, not mutable production SO values. Preserve repository restrictions on editing other authors' tests. Existing mandatory Core/integration suites still apply. Unity must use the approved AppData-access execution path; do not delete caches or upgrade Unity as a workaround.

No verification in this section has been executed by the design task.

## 13. Open decisions, proposals, and activation gates

| ID | Status | Impact |
|---|---|---|
| Q1 | Confirmed 2026-11-15: preserve secrecy and show prediction limits (D12). | Implement complete known-information forecasting and full-information internal parity as distinct guarantees; no hidden-information leakage. |
| Q2 | Upgrade acquisition method remains intentionally deferred. | Core supplies applicability/application contracts; no price, currency debit, reward schedule, or acquisition UI is invented. |
| Q3 | Confirmed 2026-11-15: common combat effects apply, ordinary-action-only transformations/completion effects do not (D13); Judgment direction/two-cell request has precedence (M15). | Source hook classification and regression work remain required, but the gameplay policy is no longer a pending user decision. |
| Q4 | Production Sniper Main-role/configuration audit required. | Core fixtures may validate behavior before activation, but production Sniper support cannot be declared complete while the supplied unit definition remains incompatible. |

Engineering choices proposed for approval with this document:

- Upgrade stored per owned Main instance; battle binds only the selected Main.
- Consecutive points must be distinct; target entities are deduplicated for one projectile.
- Current effective attack range is sampled from the caster for all point distances.
- Preserve existing neutral/object eligibility; do not turn non-attackable objects into new targets.
- Physical-defense-only bypass, existing forced-movement immunity, and common terrain reactions.
- Direct burst deaths alone qualify for Powder Blossom's next burst; indirect deaths retain their existing causal classification.
- Current attack/damage and rounding rules are reevaluated through common stages at their established boundaries, rather than freezing the whole chain at initial click time.
- Add explicit skill identity while preserving shared damage/movement meanings; do not weaken Buffer validators.
- Preserve ordinary public-preview semantics outside this feature.

A design review may accept these choices or request corrections. Unresolved gameplay choices must not be silently settled during implementation.

## 14. Deliverable and approval boundary

This task delivers the design only. Code, tests, scenes, assets, and existing in-progress work remain untouched.

An independent builder check identified three material planning questions: hidden-information policy, attack-hook eligibility, and Judgment displacement precedence. The user answered all three on 2026-11-15; the decisions are recorded in D12/D13/M15 and Q1/Q3. Their confirmation is not evidence of implementation or compilation. Acquisition economy remains deferred and the production Main audit remains outstanding.

The phase-based plan exists and its three gameplay decision gates are now resolved. Implementation still starts only when the user requests the relevant Phase; retain the plan's validation and two-session handoff boundaries. A finished design or confirmed policy is not evidence that the skills compile, run, or satisfy playback acceptance.

