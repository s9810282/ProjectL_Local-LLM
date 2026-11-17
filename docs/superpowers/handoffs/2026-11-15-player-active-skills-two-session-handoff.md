# Player Active Skills — Phase G Completed Core Handoff and Session Protocol

Date: 2026-11-15  
Purpose: Continue the work across the user's GPT-6.1 and Astra sessions, with one model owning each Phase from implementation through verification and reporting. This guide does not start implementation.

## 1. User-corrected execution model

Each Phase has **one model owner**. The owner implements, self-reviews, runs the necessary tests, fixes its findings, and reports. There is no mandatory implementation -> other-model review -> implementation exchange within a Phase.

The previous permanent GPT-6.1 implementer / Astra reviewer arrangement is superseded by this clarification. Astra implements its assigned Phases; GPT-6.1 implements its assigned Phases.

Use the model assignment and reasoning recommendation in [implementation plan section 7](../plans/2026-11-15-player-active-skills-core-implementation-plan.md). The plan's execution ledger is the single progress record; this guide does not maintain another status/model assignment table.

## 2. Read before starting or resuming a Phase

1. Repository AGENTS.md and Assets/AGENTS.md, with the user's explicit Core-task scope.
2. [Core design](../specs/2026-11-15-player-active-skills-core-design.md).
3. [Phase implementation plan](../plans/2026-11-15-player-active-skills-core-implementation-plan.md), including the requested Phase, execution ledger and model assignment.
4. The preceding Phase's report/evidence, especially contracts this Phase consumes.

Do not require the other session's chat transcript. Source, decisions, the plan ledger and concrete test evidence must be enough to continue. Check current files before trusting historical assertions.

### Phase 0 continuation

Use the [Phase 0 ledger and source audit](../plans/2026-11-15-player-active-skills-core-implementation-plan.md#61-phase-0-source-audit) for the tested checkout, baseline, append-only enum candidates and exact preservation sites. Compare the current checkout with the recorded evidence before editing shared combat files.

### Phase A continuation

Use the [Phase A ledger and contract notes](../plans/2026-11-15-player-active-skills-core-implementation-plan.md#63-phase-a-implementation-and-verification) for its contracts, source fingerprint and test evidence. Phase B consumed that verified source; use the latest continuation below before editing shared files.

Phase B consumes `PlayerSkillBattleState`, `PlayerSkillUseSettlement` and the immutable PlayerSkills request/result types. Carry the same event instances through the skill result, command transition and health claims. Use the Phase 0 DH source matrix when reviewing ordinary, extra, reaction, skill and environment predicates. Phase A uses test-owned authored definitions; the production Sniper role and selection path remain F2 work.

### Phase B continuation — updated 2026-11-15

The authorized owner completed B1/B2, self-reviewed and corrected them in the same session. No other-model review was used. The [Phase B ledger](../plans/2026-11-15-player-active-skills-core-implementation-plan.md#64-phase-b-implementation-and-verification) is the authoritative record of changed files, DH classification, source hashes, every run, corrections and verification limits. The final scoped recheck passed 240/240, including all 82 new Phase B cases. The earlier affected-contract run and its corrected event-inventory failure are recorded separately; neither is a full assembly run.

Work remains uncommitted in the shared checkout at HEAD `a95ca5a22cde603e136666f6b68b2ea3ce4c9cbc`. Before continuing, compare current code/tests with `Library/CodexTests/PlayerActiveSkills/phase-b/final-corrected-20261115-063709/tested-source.csv`, and use the separate final-document hashes for the post-test plan/handoff updates. Phase B preimages and `phase-b-file-changes.csv` distinguish its changes from the preserved Phase A and piercing/Dead Hand work. No production asset, scene, View, Framework or assembly definition was changed.

Phase C consumed these Phase B boundaries:

- `CommandAttackPlan.CompleteConsequences` explicitly enables local kill-chain, reflection, death and corpse-displacement consequences. Its legacy default is `!PublicPreview`; the observer/visible environment remain separate. No projection is installed by this option. Capture movement-use evidence for skill calculations that will be published.
- A primary scope calls `TraitAttackNotifications.Apply` once before current damage preparation. Do not invoke ordinary success/action settlement, Rope completion, or ordinary pull/sweep transformations. Preserve the actual conditions of common effects; some currently deferred effects remain deferred.
- Use fresh current projected input and a new ledger for each burst, but one shared world/arrival and attack-group allocator for the cast. `DirectKilledEnemyIds` is captured before obstacle/arrival consequences from actual direct health execution. The skill executor already feeds those seeds to KillChainAttack and resolves that chain before returning. Do not replay the seed or infer new burst kills from all deaths against the original command input.
- `AttackResultFactory.CreatePlayerSkill` and `CommandAttackLedger.BindHealthClaims(damage, occurrence)` produce/bind a real `CombatPlayerSkillAttackEvent` (event 51). Insert the occurrence before its effects and preserve the exact event/contact-origin instances. Primary ordinals start at zero; primary groups are distinct. Existing child attacks remain their own events.
- Finalize the cast once, then attach one `PlayerSkillResult`/`PlayerSkillUseSettlement` and the corresponding final usage state. `CombatCommandTransition` copies, composition and publisher completion retain the payload. Buffer publication rechecks both captured and submitted source stamps, immutable Main/skill binding, one-use consumption, unchanged ordinary action state and evidence. Duplicate command numbers retain the existing idempotent transaction path.
- Actual forced movement keeps `CombatMovementAgency.Forced` and matching step evidence. Projectile path metadata never creates a character move. The skill actor's used movement checkpoint is invalidated; unrelated checkpoints survive when their actual guards/dependencies are unchanged.

Phase B left the skill masks, scaling, direct-burst reactivation and Stun delivery for Phase C. `CombatAttackDelivery.CurrentDamage` can rebase from raw common damage when the projected actor changes. The Phase C continuation records how the scale is applied to the rebased delivery.

Self-review corrected repeated old kill seeds, cross-occurrence HP provenance/order, and reflection's hard-coded triggering group. New integration fixtures validate synthetic skill transitions through the real publisher/Buffer transaction; pure Core fixtures execute the shared damage and forced-movement stages. Actual skill rules, View transport/playback, PlayMode, production activation and whole-skill forecast/execution parity are not claimed by Phase B. The previous owner stops editing here; the user owns commit/push and Phase advancement.

### Phase C continuation — updated 2026-11-15

GPT-6.1 completed C1 and self-review in the same session. The [Phase C ledger](../plans/2026-11-15-player-active-skills-core-implementation-plan.md#65-phase-c-implementation-and-verification) records the calculation contracts, fixes and verification. The final scoped Unity run passed 224/224 cases, including all 16 new C1 cases. The retained XML, logs, invocations, source hashes and case-level Phase 0 comparisons are under `Library/CodexTests/PlayerActiveSkills/phase-c/`.

The shared checkout remains uncommitted at HEAD `a95ca5a22cde603e136666f6b68b2ea3ce4c9cbc`. Check `phase-c/final-20261115-070031/tested-source.csv` under that evidence root and substitute the two post-test document hashes from `phase-c/final-document-hashes.csv`. `phase-c/phase-c-file-changes.csv` identifies C1 product and test deltas. Existing A/B and piercing/Dead Hand edits are present.

The next planned unit is Phase D (D1/D2) for Astra after a separate user request.

- Extend `PlayerSkillCalculator.CalculateCast` with Magic Bullet dispatch and its point validation. Preserve the expected/captured source guard and the single `Complete` call for a successful cast.
- Reuse one cast world/arrival, the group allocator, per-primary ledgers and the typed result/health-claim binding. `AttackDeliveryExecutor` already runs eligible KillChainAttack once from the supplied ledger. The optional pre-chain action is used by Condemnation to apply Stun after contact and before the chain.
- `CommandAttackPlan.DamagePercent` defaults to 100. It multiplies the delivered base after any common damage rebase. Keep Magic Bullet segment scaling and hit/traversal rules in D's calculator and preserve the approved DK forced-movement precedence. Distinguish projectile geometry from character movement evidence.
- C1 verified the full local calculation and the real publisher with test-owned definitions. Public forecast/disclosure and input transport remain with E. Production activation remains with F. Full assemblies, PlayMode, View and scene checks were not run.

### Phase D continuation — updated 2026-11-15

Astra completed D1/D2 and self-review; the final scoped Unity run passed 297/297, including 33 new cases. See plan §6.6 and `Library/CodexTests/PlayerActiveSkills/phase-d/d-corrected-20261115-073539/`. The same user request authorizes E1 immediately after this checkpoint. No other-model review or commit/push is involved.

Consume the shared point validator, actual projectile path/incoming direction/impact-cell facts, optional projectile budget and explicit displacement request. Preserve ordinary defaults and the corrected environmental damage-kind evidence. Base/Distortion consume the existing PiercingFalloff configuration; Judgment uses the first live target and no falloff. E must project permitted information before full calculation, retain known child consequences, and guard stale results at both service and Buffer boundaries. F still owns lifetime/upgrade wiring and production activation.

### Phase E continuation / Phase F intake — updated 2026-11-15

Astra completed the authorized D1 → D2 → E1 sequence, including implementation, self-review, fixes and scoped Unity verification. **E's corrected final run passed 148/148 (95 Core, 53 CoreIntegration), including 23 new E cases and all 33 original D cases.** The subsequent Magic Bullet fixture passed 7/7 after adding one cross-segment multi-cell deduplication acceptance case; product code was unchanged. D's earlier final scoped run passed 297/297. No full assembly run or other-model review was used. Plan §6.7 is the detailed execution, failure and correction record.

Before F, compare the actual working tree with `Library/CodexTests/PlayerActiveSkills/phase-e/e-dedup-final-20261115-075947/tested-source.csv` and substitute `phase-e/final-document-hashes.csv` for the final plan/handoff. Use `phase-d/phase-d-file-changes.csv`, `phase-e/phase-e-file-changes.csv` and their preimages to distinguish each phase from preserved A/B/C and piercing/Dead Hand edits. HEAD is still `a95ca5a22cde603e136666f6b68b2ea3ce4c9cbc`; the user owns commit/push.

Consume these E boundaries:

- Existing `CombatViewPreviewData.ForPlayerSkillAvailability`, `.ForPlayerSkillSelection`, `.ForPlayerSkillForecast` and `CombatViewCommandData.ForPlayerSkill` route through the existing services, assembler, calculator, publisher and Buffer. No new root registration or Job is needed. The tests bind actual service classes to test-owned prepared definitions; operational initialization still belongs to F.
- Availability/selection/forecast echo battle, turn, query revision and actor. Selection/forecast also preserve the request's points. Replaced requests and stale source stamps cannot install responses. A cast must carry the supplied `ExpectedSource` and is independently recomputed against current authoritative facts. Keep existing request numbering, `ClearPreview(number)` and result acknowledgment.
- Public forecasts are always `KnownInformationConditional` when accepted. They fully simulate permitted facts, including known child attacks, terrain and tornado entry, but use canonical private RNG seeds and visible-source allocation counters. They do not reserve the live RNG outcome. Hidden effects must not alter published HP/path/outcome/coverage; the paired-world tests cover hidden mines, range terrain, tornadoes and random progress. Internal full-information calculations retain real RNG parity. Preserve the internal observer through input copies and nested forced movement; `RequireAuthoritativeCalculation` blocks forecast installation.
- Actual projectile `ProjectilePath`, `IncomingDirections` and `ImpactCells` are distinct from selected points and character movement. Contact ordinals continue across bends; damage origin remains the common source contract. Judgment owns one incoming-direction, distance-two request and only bypasses physical defense. Do not restore ordinary aim/movement transformations or ordinary action settlement.
- F must bind each selected Main's authored skill, supply the existing PiercingFalloff configuration for base/Distortion, wire genuine new-battle/turn reset and persistent mutually exclusive upgrades, and perform the production eligibility audit. E deliberately uses test-owned definitions, including a test Main binding for Magic Bullet transport; it does not activate production Sniper content.

No unresolved D/E implementation failure remains in the selected checks. View UI/playback, scenes, PlayMode, production activation, F/G work and broad final regression remain unverified/out of scope. The current owner stops after reporting E1.

### Phase F continuation / Phase G intake — updated 2026-11-15

GPT-6.1 completed F1 then F2 and self-reviewed both in the same session. See the [Phase F ledger](../plans/2026-11-15-player-active-skills-core-implementation-plan.md#68-phase-f-implementation-and-verification). The final scoped run passed 262/270, including all 13 new F cases and both production Main casts. All eight failures are the same Phase 0 `DataBaseAssetTests` cases with identical messages. Full assemblies and Unity playback remain pending.

The shared checkout remains at HEAD `a95ca5a22cde603e136666f6b68b2ea3ce4c9cbc` with the uncommitted A–F and piercing/Dead Hand changes. Use `Library/CodexTests/PlayerActiveSkills/phase-f/f-final-corrected-20261115-082834/tested-source.csv` and substitute `phase-f/final-document-hashes.csv` from the shared evidence root for the post-test documentation. `phase-f/phase-f-file-changes.csv` separates F from earlier work. The run contains XML, command, log and exact case-level Phase 0 comparisons. Keep the known asset failures visible.

- The roster upgrade API uses the current deck's Main, Run ID and existing roster revision. An active prepared battle blocks upgrades until cleanup, including after completion. The operation prepares a one-field owned-record change and installs it through the CharacterBuffer transaction. Report typed failure reasons and the successful installed revision in the View handoff. Acquisition economy remains deferred.
- The operational catalog binds Akimbo/PowderBlossom/1 and Sniper/MagicBullet/1 through the InGame query context. The active starting loadout still selects Akimbo. Sniper's existing definition now has Main eligibility and keeps its authored range, attack, targeting and single PiercingFalloff setting. The production test acquired Sniper through the roster API and selected it through the existing deck transaction before entering the authored first stage. The independent Akimbo upgrade stayed on Akimbo when Sniper was selected.
- The battle definition source captures the selected Main's definition. Preparation combines it with the Main's stored upgrade. The existing lifetime installation creates the stage allowance once and pins the battle definitions; the Combat state owns all usage and turn state afterward. Test fixtures can continue using explicit test-owned definitions and stage limit 2. The scenes and custom development battle fixture were not changed.
- Preserve E's expected-source, conditional known-information forecast, publication/acknowledgment and authoritative-installation guards and D's projectile/displacement contract. Both production cases routed through existing selection/forecast/command services and produced one result sequence and one use settlement. Full internal/scope verification and the final Core-to-View handoff belong to G after separate authorization. Actual scene rebinding, UI, playback and PlayMode remain unverified.

### Phase G completion — updated 2026-11-15

Astra completed G1 in the same session: test-owned six-form end-to-end coverage, self-review, mandatory full Core/CoreIntegration regression, a related test-fixture correction, targeted revalidation and the [Core→View handoff](2026-11-15-player-active-skills-core-view-handoff.md). The owner stops here. No other-model review, product-code change, production asset change, View implementation, acquisition economy, commit or push was performed in G.

- Intake: all 9,973 F manifest entries matched after final document hashes were substituted.
- Focused: `phase-g/g-focused-corrected-20261115-084632`, 21/21 (ten new end-to-end, eleven disclosure cases).
- Mandatory full run: `phase-g/g-full-20261115-084734`, 7,061 selected, 7,003 passed, 44 failed, 14 skipped; exit 2. Thirty-six failures exactly match Phase 0's names, failure sites/messages and first source locations.
- New regressions: eight `EnemyStageBehaviorCorpus` cases used the fixed Sub Sniper test roster with the now-Main production definition. The test adapter pins only the historical fixture role; production eligibility, gameplay values, AI behavior and all assertions remain unchanged.
- Corrective run: `phase-g/g-corpus-corrected-20261115-085850`, 30/30, exit 0, no skips. All eight regressions, ten G cases and twelve authoring/production cases passed. No new failure remains unresolved. The full suite was not rerun after this test-only correction; keep broad and corrective evidence separate.

Plan §6.9 and the final View handoff list the retained 36 failures, public APIs, event/cancellation/source/acknowledgment contracts and conditional forecast limits. Exact case comparisons are in the per-run `case-comparison.csv`, root `known-baseline-failures.csv` and `corrected-regressions.csv` under `Library/CodexTests/PlayerActiveSkills/phase-g/`.

For any later task, compare current files with `phase-g/g-corpus-corrected-20261115-085850/tested-source.csv`, substitute `phase-g/final-document-hashes.csv`, and inspect `phase-g/phase-g-file-changes.csv`/preimages. The final tested manifest SHA-256 is `FE9CA0B9C160CAAE3B15CBA54FD6CC28EAD8BFFB251E0B16549F0600A4C1F556`. Existing A–F and piercing/Dead Hand work remains uncommitted; HEAD is still `a95ca5a22cde603e136666f6b68b2ea3ce4c9cbc`. Actual Unity playback, UI, scenes and scene rebind remain unverified. The user decides subsequent work and performs commit/push.

### Confirmed decisions

DV/DH/DK were resolved by the user on 2026-11-15. Read the authoritative spec/plan details and do not ask the same questions again:

- DV: preserve hidden information and show forecast limitations.
- DH: apply common damage/contact/kill/defense/reflection effects; exclude ordinary-attack movement/aim transformations and ordinary-action-completion-only effects.
- DK: Judgment's incoming direction and requested distance of two cells override other displacement traits; existing immunity, blockers, terrain reactions and interruptions remain.

The production Main-definition audit and scoped implementation checks are recorded in Phase F. Acquisition economy remains deferred.

## 3. One writer at a time

Only the currently authorized Phase owner writes product files, its tests and the plan ledger. The other session waits until its assigned Phase is requested. Do not overlap edits or run competing Unity processes against the same project.

At the Phase boundary, the owner finishes its report and stops editing. The next owner checks the current checkout against the handoff before continuing. This is a source/contract intake check, not a separate mandatory review of the preceding model's entire work.

A HEAD hash alone is insufficient because the starting checkout has uncommitted changes. Record the affected files' pre-Phase contents/hashes and tested final hashes in the local evidence folder; mark new paths as previously absent. Preserve existing work and distinguish this Phase's changes from the whole git diff.

Use the shared checkout when that is where the user runs both sessions. Separate worktrees are not required by this protocol. If the user chooses separate checkouts, identify the exact source and use the user's transfer/commit process. Neither session commits or pushes.

## 4. Phase report and continuation data

Store execution artifacts under:

    Library/CodexTests/PlayerActiveSkills/phase-<id>/

Use separate baseline/red/focused/broad/corrected run folders and do not overwrite earlier evidence.

Update the plan ledger with the following, directly or through a linked Phase report:

- Phase owner and authorized/completed Task IDs.
- Checkout, branch, HEAD, pre-existing dirty files, and tested source fingerprint.
- Files/contracts changed, identifying what the next Phase consumes.
- Test commands, fresh XML/log paths and executed case counts.
- New failures versus the current baseline; unverified conditions.
- Self-review findings, fixes made, and any remaining issue.
- The next planned Phase and a concise continuation instruction.

A separate report file is optional if the ledger entry already contains enough information. If needed, use:

    docs/superpowers/reports/2026-11-15-player-active-skills/phase-<id>-implementation.md

There is no required Astra verdict or cross-model review file. Do not mark a Phase complete without its required verification evidence. Conversely, do not leave it artificially blocked waiting for a second model when its requirements have passed.

## 5. Defects found during a later Phase

The current owner investigates relevant defects and fixes routine integration issues within the authorized Phase, rerunning the affected checks. Do not automatically return the task to the model that wrote the earlier code.

If a correction changes approved behavior, crosses the Phase's scope, or requires a new game decision, report the concrete issue and follow the user's existing scope/approval rules. The model assignment does not relax those boundaries.

Retain required shared regression and final Core/CoreIntegration checks. One model per Phase removes the mandatory second-model review loop; it does not remove tests, self-review, or the user's Phase review.

The dust/AI incident remains an example of semantic consistency risk, not a repair task.

## 6. Starter prompts

Use the common prompt in either session, then the appropriate model-specific instruction. Replace the Phase with the one actually requested by the user.

### Common Phase-owner prompt

    Work in D:/My_ProjectWork/Github_desktop/Project_L as the sole owner of the
    authorized player active-skill Core Phase.
    Read AGENTS.md, Assets/AGENTS.md, the design, implementation plan, and
    docs/superpowers/handoffs/2026-11-15-player-active-skills-two-session-handoff.md.
    Read the plan ledger and preceding Phase's contracts/test evidence.
    Authorized Phase: <Phase explicitly requested by the user>.
    Use executing-plans to implement, self-review, test, fix, and report that Phase.
    Do not require another model to review it or create an automatic subagent review loop.
    Preserve other work and apply the confirmed DV/DH/DK decisions without re-asking.
    Do not implement View, repair the illustrative dust/AI issue, commit, or push.
    At the Phase boundary update the ledger and record the changed contracts,
    tested source, evidence, remaining issues and next action, then stop for the
    user's Phase review. Do not start the next Phase automatically.

### GPT-6.1 session

    Own the GPT-6.1-assigned Phase from plan section 7, including its tests and
    corrections. Hand off only the completed Phase's continuation facts.
    Do not assume you must implement every Phase or wait for an Astra review.

### Astra session

    Own the Astra-assigned Phase from plan section 7 as its implementer, including
    code, tests, self-review, corrections and report. You are not the permanent
    reviewer of the GPT-6.1 session. Do not reopen completed Phases without a
    concrete dependency, failure or user request.

### Resuming either session

    Verify the actual current source and ledger before continuing.
    Preserve completed Tasks and confirmed decisions. If source changed since
    the recorded evidence, identify the affected area and revalidate as needed.
    Resume the authorized Phase without replaying earlier discussion or adding
    a cross-model review requirement.

## 7. User review and model assignment changes

The user retains game-rule decisions, Phase advancement and commit/push ownership. Every Phase still ends with a report to the user. Consecutive Phases may use the same session/model; that does not authorize skipping the Phase boundary.

If the user changes a model assignment, update plan section 7 and record the current owner. Transfer editing ownership once; do not create two concurrent implementers or an implicit reviewer loop.

