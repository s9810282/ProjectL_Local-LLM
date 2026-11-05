# Trait System Remediation Implementation Plan

> **For agentic workers:** After explicit implementation authorization, use superpowers:executing-plans for sequential execution, or superpowers:subagent-driven-development if that execution mode is selected. Steps use checkboxes. Never overlap edits to shared movement/damage/publication owners. Repository instructions override skill defaults: Codex does not commit or push.

**Goal:** Correct the six confirmed A–E+ review issues and fulfill the updated activation request through required Phase X, preserving gameplay/state ownership while explicitly migrating the public reaction and hit contracts described in design §8.

**Amendment precedence:** Section 10 implements the user's instruction to prioritize the updated request. Current sequence: 0 → A → B → C → X0 → X1 → X2 → X3 → D. Earlier six-repair-only descriptions do not waive X or its contract gates. Implementation requires explicit user authorization; the current continuous Phase X authorization is recorded below.

**Architecture:** Keep the existing trait registry, local rules, disposable command projections, and atomic Buffer publication. Make tornado entry and final-state ownership explicit; unify factor arithmetic and definition conversion. Correct only the two demonstrated activation metadata defects.

**Tech Stack:** Unity 6000.0.73f1, C#, existing Core/Domain/Contracts assemblies, System.Numerics.BigInteger, NUnit EditMode, existing CoreIntegration fixtures and approved Unity runner.

**Spec:** [Trait System A–E+ Review and Remediation Design](../specs/2026-10-29-trait-system-remediation-design.md).

**Date / status:** 2026-10-29. All authorized remediation Phases 0–C, X0–X3 and D are complete; see §§11.5–11.6. The combined review found no new actionable defect. Current sources exactly match the reused X3 gates: Core 4154/4154 and CoreIntegration 337/338, with the sole existing StageKey 2101 failure and zero skips. D made no product/test edit and ran no new Unity process. F1/F2 delivery and external View/Framework/PlayMode/manual acceptance remain separately authorized owner work. No commit or push.

**Current Phase X execution authorization (2026-10-29):** The user explicitly instructed completion of all X0–X3 without intermediate stops and delegated contract decisions for final review. This overrides the X-specific stop/agreement pauses below for this execution only. Resolve technical findings, record decisions and preserve all tests/gates; do not stop for X0/X1/X2 approval. Phase D, View implementation and commits/push remain excluded. The concrete §10.1 proposal is the implementation basis selected by the executor; external View acceptance is still unconfirmed.

## Global Constraints

- Read the linked remediation design, the original trait design/implementation plan, AGENTS.md, Assets/AGENTS.md, and Framework architecture/programmer guide before execution.
- Preserve SSOT, all five SOLID principles, Framework/Buffer/Job responsibilities, and the existing Core/View ownership boundary.
- Preserve stable trait keys, serialized identities, old public constructor shapes, existing rounding/stages, costs, attribution, and stage/Run persistence. Phase X explicitly changes reaction ownership and actual-hit semantics under the updated request; record that migration and consumer impact rather than claiming unchanged playback compatibility. No new public service is authorized.
- No new manager, Buffer, event channel, generic trait language, dependency, or View-side calculation.
- No commits, pushes, checkout changes, source resets, or unrelated cleanup. Inspect the actual workspace before execution; do not create a worktree or switch checkout automatically.
- Stop and report after each Phase. Authorization to execute one Phase does not authorize the next.
- A different gameplay/ownership decision must return to design review before code changes.
- Preserve legacy Missing Script assets, existing .meta GUIDs and managed-reference names. Pair any genuinely required new .cs with a unique .meta.
- Product comments/XML docs remain Korean; implementation documentation remains English.
- View/UI/VFX/Flow, scenes/prefabs, FieldCamera, Architecture, production values, Main/enhanced content, F1/F2 delivery, and deferred traits are outside this implementation.
- Optional simplifications and unconfirmed risks in design §7 are investigation notes, not extra implementation tasks. Do not delete reflection-used lifecycle wrappers.
- No test or Unity process is started while writing this plan. Future execution requires the approved AppData-access route and an available Editor/project; never upgrade Unity or delete caches for the known sandbox problem.

## Review Focus

1. Leap crossing an occupied tornado cell must identify the arriving unit without changing periodic activation, hidden-source visibility, or movement cost. Covered by A1.
2. Equivalent ratios must produce identical damage and ordered prefix evidence; fixing aggregate damage alone must not leave the E+ overflow route. Covered by A2.
3. Late Hunter/terrain work must survive turn-state overrides while budgets, allocation and source IDs remain correct. Covered by A3.
4. Zero numeric Hunter rewards must not remove real cap/reselection facts, and meaningful zero-magnitude effects must remain. Covered by B1.
5. A structure-hit origin must be found before a secondary explosion without changing unrelated callers or assigning a synthetic group. Covered by B2.

## 1. Source baseline and Phase sequence

The design's source is uncommitted work on feature/Cheon/Core/NewCombatSytem at base HEAD e1572985064ab3cf98f9a420916e4d092e8e6f06. HEAD alone is not the implemented baseline.

The completed baseline is phase-eplus-base/manifest.json (1,780 files) overlaid by phase-eplus-tested-source-manifest.json and phase-eplus-tested/ (52 files, seven new): 1,787 C#/asmdef files. All paths are below .superpowers/trait-system/. Earlier reviewed snapshots/diffs are not substitutes for the final tested overlay.

Historical full gates: Eplus-core-final.xml = 4102/4106 and Eplus-integration-final.xml = 317/322, with four/five failures and zero skips. These are saved results, not results of this plan. Do not hard-code a future total or call a run passing merely because it has nine failures.

| Phase | Tasks / order | Dependencies | Exit / stop |
|---|---|---|---|
| 0 — Execution baseline | P0.1 source, ownership and evidence capture | Explicit execution authorization | Baseline confirmed; report and stop before product edits. |
| A — Gameplay boundaries | A1 TR-01 → A2 TR-02 → A3 TR-03 | Phase 0 accepted | Three reproductions corrected; focused/native checks and full Core/CoreIntegration gates; report and stop. |
| B — E+ occurrence contracts | B1 TR-05 → B2 TR-06 | Phase A accepted | Zero-grant/origin fixes with unchanged gameplay; focused/native checks and full gates; report and stop. |
| C — Single conversion route | C1 TR-04 | Phase B accepted | Duplicate switch removed; native conversion/authoring obligations and full gates; report and stop. |
| X — Updated request contracts | X0 contract decisions → X1 movement cause → X2 interception/result migration → X3 publication | Phase C accepted; stop after each X subphase | R8–R10 and full R1–R10 correspondence verified; report and stop. |
| D — Integrated acceptance record | D1 final review, hashes, evidence and scope | Phase X3 accepted | Six-repair and updated-request ledgers with exact tested-source record ready for later F1; report and stop. |

The six-repair order follows design §6.1; design §8 adds X before final closure. Tasks inside a Phase are sequential to keep RED/GREEN attribution and shared-file changes reviewable. Phase D reuses X3's full gates when its sources are unchanged; do not rerun unchanged suites only to create another report.

## 2. File and responsibility map

Prefixes below expand to exact repository-relative directories:

| Prefix | Directory |
|---|---|
| C/ | Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/ |
| B/ | Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/ |
| T/ | Assets/Tests/EditMode/ |
| TI/ | Assets/Tests/EditMode/CoreIntegration/ |

| Task | Product changes | Existing tests to extend |
|---|---|---|
| A1 | C/Application/Combat/TornadoCalculator.cs | T/LeapTraitTests.cs; TI/LeapPublicationTests.cs |
| A2 | C/Domain/CombatEffects/Evaluations/EffectTargetDamageEvaluation.cs; C/Application/Combat/CombatDamageCalculator.cs | T/EnhancementTargetDamageCapabilityTests.cs; T/CannonThresholdTraitTests.cs; T/TraitActivationOutcomeTests.cs; TI/CannonThresholdPublicationTests.cs |
| A3 | C/Application/Combat/MovementProjection.cs; C/Application/Combat/CombatTurnCalculator.cs | T/HunterTraitTests.cs; T/TornadoEnvironmentTests.cs; TI/HunterPersistenceTests.cs; TI/TraitActivationPublicationTests.cs |
| B1 | C/Application/Combat/HunterApplication.cs | T/HunterTraitTests.cs; T/TraitActivationOutcomeTests.cs; TI/HunterPersistenceTests.cs |
| B2 | C/Application/Combat/CombatCommandEvents.cs | T/BossRoarTests.cs; T/TraitActivationOutcomeTests.cs; TI/TraitActivationPublicationTests.cs |
| C1 | C/Application/Combat/Traits/TraitRegistry.cs | T/TraitDefinitionContractTests.cs; TI/TraitDefinitionAuthoringTests.cs |

No new product file or assembly is planned. Extend existing fixtures with narrowly scoped local helpers when required. CoreIntegration must use its own native fixtures and production contracts; do not add a dependency on the Core test assembly to reuse a helper.

Inspect relevant callers before each edit. Add another product file to the edit set only when a demonstrated compilation/call-contract dependency requires it, and record why; a new owner, service, behavior or public contract requires design review.

## 3. Verification procedure for future execution

### 3.1 Existing runner and commands

Recheck .superpowers/trait-system/run-tests.ps1 and the configured editor path before use. It accepts Run, Assembly, and optional Filter, launches the Editor hidden, rejects existing evidence names and another running Unity process, and produces XML/log/exit files. If the ignored runner is absent, restore an equivalent evidence-only runner from the existing implementation plan §1.1; do not modify product code or weaken runner checks.

Run from the repository root through the approved AppData-access route. Commands below are **future instructions, not commands executed during planning**.

~~~powershell
pwsh -File .superpowers/trait-system/run-tests.ps1 -Run TR-A1-red -Assembly ProjectL.Tests.Core -Filter ProjectL.Tests.Core.LeapTraitTests.Leap_OccupiedTornado_UsesExplicitEntrant
pwsh -File .superpowers/trait-system/run-tests.ps1 -Run TR-A1-green -Assembly ProjectL.Tests.Core -Filter ProjectL.Tests.Core.LeapTraitTests
~~~

For every task use fresh names TR-A1, TR-A2, TR-A3, TR-B1, TR-B2 or TR-C1 with -red, -green and -native suffixes; add a numeric suffix for reruns, never overwrite evidence. Use the exact test methods/classes named below. Each fixture in a regression list can run independently via the same command and its fully qualified class name; names under T use ProjectL.Tests.Core, and names under TI use ProjectL.Tests.CoreIntegration.

Task-focused GREEN requires all selected cases present, total > 0, failed = 0 and skipped = 0. Confirm the requested new case names exist in XML. RED must show the intended behavioral assertion/exception, not compilation, fixture setup, zero selected tests, or infrastructure failure. For TR-04, a registration-routing spy is appropriate because the defect is specifically an alternate conversion route.

At the end of A, B and C, run both full assemblies sequentially, substituting that Phase letter:

~~~powershell
pwsh -File .superpowers/trait-system/run-tests.ps1 -Run TR-A-core -Assembly ProjectL.Tests.Core
pwsh -File .superpowers/trait-system/run-tests.ps1 -Run TR-A-integration -Assembly ProjectL.Tests.CoreIntegration
~~~

An affected regression set containing baseline failures is assessed case-by-case under §3.2, not mislabeled an all-green focused run. Phase D uses TR-D-core/TR-D-integration only if product/test fixes after the last full gates require new verification.

### 3.2 Baseline failures and acceptance

Capture exact failed fullnames, messages, stacks, added/removed cases and skips against both completed E+ and the preceding Phase. A repaired baseline failure may disappear; document the related correction rather than forcing failure-count equality.

The known four Core failures are ExperienceAttributionPathTests.DeferredGroundAtPlayerEnd_DoesNotCreateKillsOrExperience and WeakGroundTurnTests.ControllerCompletion_DoesNotCollapseOrCountGround for Player/0, Enemy/1 and Enemy/8. They intersect A3's completion owner: inspect their causes and changed assertions rather than automatically accepting them.

The five CoreIntegration failures are BossStagePreparationTests.ProductionBossStage_PreparesAndPlacesBoss (missing StageKey 2101) and four PullAndMarkPresentationTests cases for direct motion/arrival (False/True). Treat their saved evidence as history. Investigate a changed cause or affected path; their existence does not authorize fixture/content/View repairs.

No additional unexplained failure, missing acceptance case, or skip is permitted at a Phase exit. Retain existing tests; change an assertion only with source-backed contract justification. Full Core/CoreIntegration success does not establish Framework/View/PlayMode/manual acceptance.

## 4. Phase 0 — Establish the execution baseline

### P0.1 Capture before changing anything

**Files:** Read the design, this plan, repository instructions, final manifests/XML, and actual source status. Write future execution evidence only to the existing ignored evidence workspace and this plan's execution log.

**Interface:** Produces the exact source hashes, failure inventory and protected-scope status against which A1 begins.

- [x] Confirm authorization for Phase 0, actual checkout/HEAD, dirty files and active Editor ownership. Preserve all unrelated changes.
- [x] Validate the completed E+ composite and compare current sources. If drift exists, identify the changed owners and its effect on the six reproductions; do not revert or mix snapshots.
- [x] Record exact full-suite baseline failures and source correspondence. Reuse the saved E+ gates only when source/test hashes match; otherwise, once execution is authorized and the Editor is available, capture fresh TR-0-core/TR-0-integration gates.
- [x] Record the six issue IDs, named regression fixtures and phase stop boundaries. Verify all necessary fixture files and runner inputs.
- [x] Report the baseline and any source/contract conflict. **Stop before Phase A.**

## 5. Phase A — Correct gameplay boundaries

### A1 / TR-01 — Explicit tornado entrant

**Interfaces:** Keep ResolveEntry(MovementProjection projection, EntityId mover, CellCoord enteredCell, IReadOnlyList<IMovementTileRule> rules). Extend its private Resolve helper with optional EntityId? entrant = null. ResolveDue uses the ordinary occupant lookup; ResolveEntry supplies mover. No public movement/result change.

- [x] Add Leap_OccupiedTornado_UsesExplicitEntrant in T/LeapTraitTests.cs: mover (0,0), another living unit and tornado (1,0), empty endpoint (2,0), sufficient range/funds. Assert no exception, the crossed unit unchanged, and normal tornado relocation/interruption of the entrant.
- [x] Add visible/hidden variants. Visible preview agrees with execution; hidden preview preserves visibility policy and does not expose/consume the hidden source. Both preview calls leave input state/RNG unchanged.
- [x] Run the named RED case before editing TornadoCalculator.
- [x] Pass the explicit entrant into Resolve, validate its current living field/cell facts, and retain the unique-occupant branch for periodic activation. Do not use FirstOrDefault to choose between overlapping units.
- [x] Extend TI/LeapPublicationTests.cs with accepted native movement: one action cost; actual voluntary prefix only for trails/Acceleration; unchanged crossed unit; repeat submission does not apply again.
- [x] Run focused GREEN/native tests plus TornadoEnvironmentTests, InitialTornadoPlacementTests, TraitActivationMovementTests and existing LeapPublicationTests. Record results before A2.

### A2 / TR-02 — Shared exact damage factor scaling

**Interfaces:** Keep EffectTargetDamageEvaluation's contribution/expectation ordering and BypassPhysicalDefense contract. Replace its overflow-prone internal aggregate representation with BigInteger. Add internal int Scale(int baseDamage, int contributionCount), where 0 means identity and Count means the full product. Validate baseDamage >= 0 and 0 <= contributionCount <= Contributions.Count. The internal evaluator owns exact prefix products; no new public service/type is needed.

CombatDamageCalculator uses Scale(minimum, Count) for final damage and Scale(minimum, index + 1) for each evidence prefix. Remove its independent numerator/denominator multiplication. Audit all uses of the old int Numerator/Denominator members and adapt internal consumers/tests together; preserve public gameplay/result contracts.

- [x] Add EquivalentRatios_FourFactors_Produce240Damage in T/CannonThresholdTraitTests.cs: ATK 10; StationaryFire(100), TrackingShot(50,100), FocusedBombardment(1,50), ContemptForWeak(400,100); one enemy ATK 5, HP 1000/1000, DEF 0, matchup 1. Assert damage 240, HP 760, and identical outcome for 4/1.
- [x] Add ExactScale_PreservesHalfUpAndPrefixValues to T/EnhancementTargetDamageCapabilityTests.cs. Pin identity, zero base, invalid prefix, half-up ties, equivalent ratios, and OverflowException only when the requested scaled integer cannot be represented. Use the same original base for every prefix.
- [x] Add FactorEvidence_MatchesOrderedExactPrefixes in T/TraitActivationOutcomeTests.cs. Verify existing origin order and absolute differences between adjacent rounded prefix values, including a reducing Contempt ratio followed by amplifiers. Do not assert unsigned deltas sum to net change for mixed factors.
- [x] Run focused RED for the native four-factor case and evidence before product edits.
- [x] Implement exact composition/scaling in the existing evaluation owner with System.Numerics.BigInteger and existing half-up semantics; update both application paths to use it. Preserve minimum damage, defense bypass, received reductions and shields.
- [x] Extend TI/CannonThresholdPublicationTests.cs to publish the four-factor attack and compare owned/character/result facts, then duplicate/stale behavior. Exercise preview and additional-attack/Sacrifice paths using existing supported fixtures.
- [x] Run focused GREEN/native plus CombatEffectEngineTests, EnhancementTargetDamageTests, CannonThresholdTraitTests, CombatDamageBoundaryTests, SacrificeTraitTests and SacrificePublicationTests. Verify no second factor-product implementation remains before A3.

### A3 / TR-03 — Complete projection before lifecycle overrides

**Interfaces:** Replace MovementProjection.Complete's optional whole CombatCommandState with explicit optional lifecycle values:
Complete(AttackResult? attack = null, CombatTurnFacts turn = null, CombatFlowPhase? phase = null, bool? isNight = null, CombatAllocationFacts allocation = null) -> CombatCommandTransition.
These are proposed internal parameters. Existing no-override callers keep their behavior. EnterCombat, EndPlayerTurn and EndEnemyPhase pass only their existing lifecycle intent.

Inside Complete: existing pending ground/death/Hunter work → CurrentInput().State → apply supplied lifecycle values → existing Finish. Preserve computed budgets, structures, sources/progress, TraitTurn, rewards and item RNG. Do not take a pre-finalization full-world snapshot from callers or patch RandomState alone.

- [x] Add EnterCombat_HunterTie_PublishesTargetAndAdvancedRng in T/HunterTraitTests.cs: after recall/new Hunter placement, absent target, rewards 0, EnemyWaiting IDs 2/3 with HP 100 each, valid origins/objective, no arrival effects, RNG 1. Assert target 2 and final RNG 1015568748 together.
- [x] Add ControllerEnd_FinalizationPreservesLateSourceAndRng for both side endings; cover a killed Hunter lock with tied survivors and supported tornado arrival reaching weak-ground collapse. Assert recorded source chain equals final environment while preserving existing collapse timing.
- [x] Run the new RED cases; separately inspect the four affected historical Core failures and record their actual causes.
- [x] Implement the narrow Complete contract and update all three lifecycle callers. Keep late computation in the existing projection and installation in Buffer.
- [x] Extend TI/HunterPersistenceTests.cs and TI/TraitActivationPublicationTests.cs: real EnterCombat publication, preserved turn/phase/allocation/budget values, locked target consumes no second draw, stale publication installs neither target nor RNG, duplicate command and result reread are inert.
- [x] Run focused GREEN/native plus HunterTraitTests, HunterPersistenceTests, TornadoEnvironmentTests, WeakGroundMovementTests, WeakGroundTurnTests, ExperienceAttributionPathTests and TraitTrailPublicationTests. Explain any changed baseline case under §3.2.

### Phase A exit

- [x] Run TR-A-core then TR-A-integration from the final A sources, compare exact failures and inspect existing consumers' compilation.
- [x] Record reproduction outcomes, changed files/signatures, source hashes and actual edit/understanding/validation scope for TR-01/02/03.
- [x] Report remaining B/C work and external limits. **Stop; do not start B without authorization.**

## 6. Phase B — Correct E+ occurrence semantics

### B1 / TR-05 — Suppress only the zero numeric Hunter grant

**Interfaces:** Preserve HunterApplication.TargetDied's existing signature and owned/status composition. Calculate awardedAttack from final.HunterAttackBonus minus expected.HunterAttackBonus; guard only AttackGranted emission with awardedAttack > 0. Keep TraitState, counter and reselection behavior.

- [x] Add Hunter_ZeroReward_ChangesCapWithoutAttackGranted in T/TraitActivationOutcomeTests.cs using Hunter(0,3), a locked target and an owner-attributed lethal hit. Assert owned ATK unchanged, successful count 0 → 1, truthful state/reselection facts, no Hunter AttackGranted in Events or derived root summary.
- [x] Pin positive Hunter(2,3) reward and unchanged non-owner kill behavior in T/HunterTraitTests.cs.
- [x] Run the zero-bonus RED case before editing HunterApplication.
- [x] Apply the local delta guard without changing zero-configured gameplay, kill credit, cap or authoritative state.
- [x] Extend TI/HunterPersistenceTests.cs with zero/positive native command publication and stale/duplicate assertions.
- [x] Run focused GREEN/native plus existing Hunter persistence, TraitActivationOutcomeTests, TraitPresentationContractTests and TraitGrowthStatSettlementTests. Preserve meaningful zero Roar/Taunt/shield/extra-action events; no common magnitude-zero filter.

### B2 / TR-06 — Recognize direct structure damage origins

**Interfaces:** Keep CombatCommandEvents.FindOrigin(IEnumerable<CombatEvent> events, EntityId creditor) -> CombatDamageOrigin. In the existing event-order scan, examine CombatStructureEvent.DamageOrigins with the same creditor matching as character damage before proceeding to subsequent events. Preserve null when no correlation exists and all other fallback behavior.

- [x] Add Roar_BarrelOnly_UsesDirectStructureOrigin in T/TraitActivationOutcomeTests.cs with the supported BossRoarTests.Input(5, barrel: true, withTargets: false) shape: boss 2 at (3,3), barrel 40 at (3,2), HP 1. Assert Roar origin (3,3)/2/group 1, while the secondary explosion remains (3,2)/2/group 2.
- [x] Add structure-only, ordinary-character and empty-area variants in T/BossRoarTests.cs or the same outcome fixture. Assert origin fields, one occurrence preceding its dedicated event, unchanged counter reset, damage and cost; empty/no-related-result retains null.
- [x] Run the barrel-only RED case before editing FindOrigin.
- [x] Add structure handling in event order. Do not reconstruct origin from trait owner, mint groups, or search unrelated earlier command segments.
- [x] Extend TI/TraitActivationPublicationTests.cs with an integration-owned boss/barrel encounter fixture and accepted public result assertions. Reuse existing integration source contracts, not the Core test assembly.
- [x] Run focused GREEN/native plus BossRoarTests, TraitActivationOutcomeTests and TraitActivationPublicationTests. Inspect all FindOrigin callers and cover preserved Acceleration consumption and Taunt selection correlation.

### Phase B exit

- [x] Run TR-B-core then TR-B-integration and compare exact failures against A and completed E+.
- [x] Verify ordering/summary compatibility and the four already-corrected E+ final-review cases remain covered.
- [x] Record hashes and the metadata-only nature of B1/B2. **Report and stop before C.**

## 7. Phase C — Remove demonstrated duplicate conversion

### C1 / TR-04 — Route the round-trip through registered factories

**Interfaces:** Keep TraitRegistry.BuildDefinition(TraitSpec spec) -> TraitDefinition and its null rejection. Delegate to GetRequiredRegistration(spec.TraitKey).TraitDefinitionFactory.BuildDefinition(spec). Keep the registered factories, validation, deferred availability and native catalog routes unchanged.

- [x] Extend T/TraitDefinitionContractTests.cs with DefinitionConversion_UsesRegisteredFactory. Construct a valid registry registration with a test-local counting/sentinel factory for a new trait such as Hunter; call BuildDefinition(Hunter(2,4)); assert the registered factory receives that spec once and its definition is returned. A direct switch must fail this assertion.
- [x] Keep Definitions_RoundTripEveryTunable for all 22 traits with existing non-default values, including Cohesion(3,7,2,2) and Hunter(2,4). Add/preserve null, type mismatch, missing registration and unavailable-trait obligations at their actual owning boundary.
- [x] Run the registration-route RED test. Do not invent a gameplay difference to justify this structural regression.
- [x] Replace the duplicate switch with factory delegation. Do not introduce another mapping, generic factory framework or remove serialized/runtime/status types.
- [x] Verify native UnitCatalog and CombatCommandEffects still use the same registered conversion. Extend TI/TraitDefinitionAuthoringTests.cs only where a native conversion assertion is missing.
- [x] Run focused GREEN/native plus TraitGrowthCatalogTests, TraitGrowthRulesTests, TraitDefinitionAuthoringTests and TraitGrowthAuthoringTests. Reflection-used lifecycle/constructor tests remain intact.

### Phase C exit

- [x] Run TR-C-core then TR-C-integration from final sources; compare exact failures, additions, removals and skips.
- [x] Record one conversion owner, removed switch scope, preserved contracts and final source hashes.
- [x] **Report and stop before X0.**

## 8. Phase D — Integrated review and evidence closure

### D1 Verify the combined change without expanding scope

**Files:** This plan's execution log, final source manifest/diff and result ledger in the existing ignored evidence workspace. No product edits are planned in this Phase.

**Interfaces:** Consumes A/B/C and X task evidence and exact final sources. Produces the six-issue disposition, R1–R10 correspondence and handoff-ready evidence record, not F1/F2 delivery.

- [x] Check each TR issue against its original input, expected result, owner correction and regression evidence. Mark implementation, focused verification and native publication separately.
- [x] Review the combined diff for SSOT, SRP/OCP/LSP/ISP/DIP and Framework/Buffer/Job boundaries. Confirm no second factor arithmetic or conversion route remains, no event channel/state store was added, and normal registration is preserved.
- [x] Verify final source hashes match the last full gates. Reuse X3's full gates if unchanged. If review reveals a needed product correction, reopen its owning task/Phase and obtain authorization; do not silently implement it as documentation cleanup.
- [x] Examine removed/renamed tests and each baseline failure's cause on changed paths. Keep unconfirmed risks and external acceptance separate from confirmed corrections.
- [x] Record source identity, new/changed tests, exact pass/fail/skip counts, baseline differences, preserved unrelated changes and remaining owner work.
- [x] **Stop and report.** F1 handoff and F2 production/manual acceptance require separate authorization. No commit or push.

## 9. Execution record template and plan self-check

Append one record per authorized task/Phase without replacing earlier evidence:

| Field | Required content |
|---|---|
| Authorization / source | Authorized Phase, date, branch/HEAD, starting hashes and known drift |
| Reproduction | Test fullname, input, expected result, actual RED failure and XML/log |
| Change | Owner files/signatures, minimal correction, preserved contracts |
| Focused / native checks | Exact commands, test names/counts, failures/skips and evidence paths |
| Full gate | Core/CoreIntegration results, exact baseline differences and investigation |
| Locality | Actual edit, understanding and required validation scope versus plan |
| Exit | Completed/pending issue IDs, remaining risks/owner boundaries and explicit stop |

Planning self-check: all six design IDs map to tasks (TR-01/A1, TR-02/A2, TR-03/A3, TR-05/B1, TR-06/B2, TR-04/C1); all five review-focus cases have explicit assertions; all tasks have RED, minimal change and GREEN/native steps; every implementation Phase has full-suite and stop gates. No implementation result is claimed.

Future execution mode is not selected by this document. Sequential native execution is suitable because shared owners and review stops dominate the work; a user-selected subagent workflow must preserve the same sequence, ownership, evidence and stop boundaries. No choice or authorization is needed merely to deliver this plan.

## 10. Required Phase X — Updated activation request amendment

This amendment follows the user's explicit instruction to prioritize the updated [activation request](C:/Users/권천명/Downloads/2026-10-29-combat-trait-activation-core-request.md). See remediation design §8 for source hash, comparison and revised ownership. It adds three required contract work packages (AX-01/02/03); it does not reclassify them as three additional old defects. No implementation is authorized by editing this plan.

**Sequence:** Phase 0 → A → B → C → **X0 → X1 → X2 → X3** → D. A–C's six repairs and their checks remain unchanged. The current user instruction overrides intermediate X report/stop pauses; retain every technical and verification gate, then stop before D. Completion of A–C alone is not compliance with updated R8–R10.

### X0 — Pin the revised public contract before producer edits

**Files:** Update design §8 and this plan with exact agreed signatures/copy rules; read C/Contracts/Combat/CombatEvent.cs, C/Contracts/Results/{AttackResult,AttackHitResult}.cs, C/Contracts/Snapshots/Traits/TraitActivationSnapshot.cs, existing Pull snapshot, and their constructors/readers.

**Deliverable:** A concrete carrier and identity table for normal attack, Roar, DoubleShot, fixed/random follow-up, KillChain, Knockback, ImpactBolt and Pull. Record exact file paths before introducing new DTOs; keep the smallest additive immutable payloads in the existing Contracts assembly.

**Follow-up compatibility review:** Before freezing X0, add a short mapping table for the planned buff/debuff consolidation: current rule/state owner and source path → activation fact versus applied effect state → impact on the proposed X contract → decision needed now or deferred to the follow-up design. Use the status-effect planning deck (`C:/Users/권천명/Dropbox/Project L/기획 문서/상태이상 기획서.pptx`) as follow-up input, not as authorization to change the currently approved gameplay. This is a contract review within X0; A–C and the implementation sequence remain unchanged.

- [x] Confirm source drift and re-read the updated R8–R10/Q9–Q13, overriding former E+ reaction-summary rules where they conflict.
- [x] Pin movement agency/key values, ordinary null compatibility and validation. Select one playable Pull movement carrier as required by design §8.3; record the minimal typed-Pull-step recommendation and consumer acceptance, or explicitly specify the Moved-carrier migration. Do not leave two playable paths.
- [x] Pin the canonical reaction carrier for each attack type. Root/Roar AttackResult holds reaction activations; child attacks must expose their own reactions at the existing child start carrier without adding CombatAttackEvent. Pin how ordinary summaries and canonical reactions are distinguished, copied and consumed once.
- [x] Pin guardian/protected/actual-recipient fields, per-delivery correspondence and Hits aggregation. Include cases where one guardian is itself a direct target or protects multiple configured hits. Preserve original selection and knockback/contact semantics separately.
- [x] Identify public validators/readers/tests that require migration: ordinary/piercing role correspondence, result factories/copies, attack previews, event subject/grouping and existing dedicated Pull/Roar handling. Read View consumers for compatibility only; do not edit them.
- [x] Separate trait activation conditions/facts from the resulting buff/debuff state and its application, stacking, consumption and expiry responsibilities in the mapping table. Preserve the existing state owner and Buffer publication contract; activation metadata must not become a second mutable effect-state store.
- [x] Check the proposed movement/reaction/hit carriers against later effect application from non-trait sources. Keep trait identity on genuine trait activations; identify any unnecessary trait-only assumption in an effect-facing contract without adding speculative source enums, generic effect DTOs, services or state stores.
- [x] Record concrete reuse or migration implications for X1–X3: source/recipient identity, attack linkage, canonical playback ownership, immutable copies, preview non-mutation and atomic publication. For each actual contract risk, name the affected file/member, minimum correction and required regression/integration checks. Distinguish verified current dependencies from anticipated follow-up changes; do not broaden or omit existing gates.
- [x] Defer buff/debuff runtime implementation, state migration and differences between the planning deck and approved gameplay to a separate design/plan. Resolve only decisions needed for the current X contracts; do not make completion of the future system a prerequisite for X1–X3. If a concrete conflict requires changing approved behavior or ownership, report it for a decision before dependent implementation.
- [x] Record the finalized schemas and exact constructor/member signatures in this plan before X1/X2 product edits. Any unresolved carrier decision is a contract gate, not permission to invent a speculative implementation.
- [x] Report the concrete contract and remaining View-owned integration. **Continue to X1 under the current continuous authorization.** This gate concerns technical carrier agreement; the user's priority instruction has already settled which request takes precedence.

### X1 / AX-01 — Movement-cause contract and producers

**Files:** C/Contracts/Combat/CombatEvent.cs and the X0-selected movement-cause DTO/enum; existing Pull result contract; C/Application/Combat/{AttackForcedMovement,AttackKnockbackCalculator,PullAttackCalculator,MovementProjection}.cs. Extend T/{TraitActivationContractTests,TraitActivationMovementTests,ContractShapeGuardTests}.cs and TI/{LeapPublicationTests,PullPublicationTests}.cs.

**Interfaces:** Consume X0's immutable movement cause. Preserve old event construction and source-specific tornado identity; annotate actual trait movement at the producing owner, not through View inference.

- [x] Add MovementCause_DistinguishesKnockbackImpactBoltAndPull, OrdinaryMove_RetainsVoluntaryCompatibility, and CauseCopy_IsImmutable tests with exact agency/key assertions.
- [x] Add Pull_HasExactlyOnePlayableMovementCarrier and HiddenArrival_DoesNotExposeUnseenCause; exercise blocked/zero-length paths, voluntary Leap and forced movement.
- [x] Run focused RED before implementation with fresh TR-X1-red evidence.
- [x] Implement the X0 schema and propagate cause through actual movement decisions. Keep costs, occupied cells, hazard execution, immunity and trail/Acceleration accounting unchanged.
- [x] Run focused GREEN and native publication checks, existing Pull/Leap/knockback/Tornado regressions and TR-X1-core then TR-X1-integration full gates under §3.
- [x] Record exactly what existing View consumers must change and which old calls still compile. **Recorded; continue to X2 under the current authorization.**

### X2 / AX-02 + AX-03 — Attack-attached interception and actual-recipient results

**Files:** X0-selected existing activation/attack/hit/Pull contracts and immutable payload files; C/Application/Combat/{SacrificeDamagePlanner,AttackDirectDamageCalculator,CommandAttackLedger,AttackResultFactory,CombatAttackCalculator,FollowUpAttackExecutor,KillChainAttackCalculator,BossRoarCalculator}.cs. Audit result copying in UnitAttackService and other readers before editing. Extend T/{SacrificeTraitTests,TraitActivationOutcomeTests,AttackCommandCharacterizationTests,ContractShapeGuardTests}.cs and TI/{SacrificePublicationTests,TraitActivationPublicationTests}.cs.

**Interfaces:** The applied redirect decision feeds one canonical reaction in its owning attack result; ordered Events retains resulting state/HP/death. Hits exposes actual recipients with explicit original-target correspondence as fixed by X0.

- [x] Add Interception_IsAvailableInOwningAttackAtStart and Interception_HasNoSecondEffectsOccurrence for root, extra attacks and Roar. Assert guardian/protected/actual recipient plus physical cell, creditor and group.
- [x] Add RedirectedHit_UsesGuardianHealthAndPreservesOriginalAim; use distinct target/guardian HP and defense so a TargetId-only patch fails. Verify no duplicated/misassigned damage, recovery or normalization.
- [x] Add AoeAndRepeatedHits_PreservePerDeliveryInterceptions, GuardianAlsoDirectTarget_KeepsBothRelationships, and MultiGuardianSelection_PreservesExistingPriorityAndCaps.
- [x] Add RejectedOrUnusedRedirect_HasNoReaction and FinishingDefeat_HasNoSacrificeReaction. Verify no fictitious movement/hazards/AP; preserve current original-contact displacement policy and real coordinate/death facts.
- [x] Run meaningful RED cases before producer edits. Do not use fabricated success events or mutate the planner's disposable candidates into published reactions.
- [x] Implement the finalized carrier/ledger/factory/validation changes together. Remove only the duplicate playable interception occurrence from Events; preserve exact ordered result facts and all ordinary activations. Child reactions remain with their child scope.
- [x] Update superseded summary/ordering assertions with the explicit request amendment as justification. Retain damage, shielding, XP/kill/Run ATK and no-relay regressions.
- [x] Run focused GREEN/native plus direct/additional/reflection, Sacrifice, Cannon/Finishing, Pull/knockback, Roar, targeting/piercing and copy regressions. Run TR-X2-core then TR-X2-integration and inspect consumer compilation.
- [x] Report contract migration, gameplay preservation and outstanding View replay work. **Recorded; continue to X3 under the current authorization.**

### X3 — Updated-request publication and readiness gate

**Files:** Existing native contract/publication fixtures, especially TI/{TraitActivationPublicationTests,TraitPresentationContractTests,SacrificePublicationTests}.cs; this plan's evidence log. Modify production publication owners only for a demonstrated invariant omission, not to duplicate Core rules.

- [x] Verify public reads expose movement causes and the owning attack's reaction before its ordered consequences. Assert exactly one playable occurrence across nested result copies and Events.
- [x] Verify defensive copies, identity validation, expected/final consistency, snapshot/result read purity, preview non-mutation, stale rejection and duplicate command handling for both new payload families.
- [x] Compare unchanged gameplay against the original native fixtures: HP, shield/cap, target choice/RNG, cost, occupied cells, hazards, credit/XP and owned Run ATK. Investigate every difference rather than attributing it to metadata.
- [x] Run focused/native checks and full TR-X3-core/TR-X3-integration gates with exact baseline comparison and final-source hashes.
- [x] Produce an R1–R10 acceptance table: implemented, tests/source evidence, and View/external acceptance still pending. Do not claim future unimplemented path-attack mechanics or reactivation of deferred traits.
- [x] **Report and stop before D.** F1 must consume this revised contract, not the earlier blanket Events-only rule.

### Amendment to Phase D and completion criteria

D now consumes both the six-repair ledger and AX-01/02/03/R1–R10 evidence. Use X3's full gates if sources remain unchanged; otherwise rerun only as required by actual subsequent edits. Earlier references to C's final gates apply only to the six-repair-only checkpoint.

Do not mark the full updated plan complete while X0 carrier decisions, X1/X2 implementation or X3 publication acceptance remain pending. A–C may still be reported as completed separately. F1 preparation/delivery and all View motion, hit gating, rendering and scenes remain externally owned and require their own authorization.

### 10.1 X0 selected API and migration package

**Status:** Selected by the implementation author under the user's explicit delegation to finish all X0–X3 without intermediate pauses. X1/X2 implement these declarations; §11.5 records validation and the bounded review correction. External View acceptance is unconfirmed. Design §§8.7–8.9 own rationale, playback identity and follow-up compatibility; this section owns exact declarations. Existing public constructor shapes remain available, including reflection-visible arities.

#### 10.1.1 Movement and Pull

New file `C/Contracts/Combat/CombatMovementCause.cs` (enum and sealed immutable class):

```csharp
public enum CombatMovementAgency { Voluntary = 1, Forced = 2 }
public sealed class CombatMovementCause
{
    public CombatMovementAgency Agency { get; }
    public TraitKey? TraitKey { get; }
    public CombatMovementCause(CombatMovementAgency agency, TraitKey? traitKey = null);
}
```

Reject undefined agency, None or undefined non-null TraitKey. Null key is valid for non-trait forced movement; no speculative item/environment/source enum is added. Ordinary Moved events retain null cause, with voluntary semantics. Native `MovementProjection.RecordForcedMove` supplies Forced/null for environment displacement (including tornado); its existing TornadoSourceId remains unchanged. A legacy Relocated event is still identified by its kind/source, never classified as ordinary voluntary movement merely because the new field is null.

In `C/Contracts/Combat/CombatEvent.cs`, retain the current six-parameter `CombatCharacterEvent` constructor exactly and add:

```csharp
public CombatMovementCause MovementCause { get; }
public CombatCharacterEvent(CombatEventKind kind, CharacterCombatFacts expected,
    CharacterCombatFacts final, IReadOnlyList<CombatDamageOrigin> damageOrigins,
    long? tornadoSourceId, CombatDamageCause damageCause,
    CombatMovementCause movementCause);
```

The existing overload delegates with null. A non-null cause is allowed only on Moved/Relocated. Relocated requires Forced with no trait key and retains its existing positive tornado ID validation. Knockback and ImpactBolt producers pass Forced with their actual deciding key through `AttackForcedMovement.ExecuteUnit`; do not guess from the displaced unit's traits. Voluntary Leap remains untagged. Immunity, blocked zero-length paths and visual Sacrifice offsets create no steps.

`CombatStructureEvent` also retains its four-parameter constructor and adds the same read-only `MovementCause` with a five-parameter overload:

```csharp
public CombatStructureEvent(StructureCombatFacts expected, StructureCombatFacts final,
    IReadOnlyList<CombatDamageOrigin> damageOrigins, CombatDamageCause damageCause,
    CombatMovementCause movementCause);
```

A non-null cause requires actual anchor displacement. `MovementProjection.MergeStructure` forwards the deciding Knockback cause only when an existing structure displacement occurs. This closes the X0 builder review's structure-carrier gap without adding an event or changing its kind/health facts.

New file `C/Contracts/Results/AttackPullStepSnapshot.cs`:

```csharp
public sealed class AttackPullStepSnapshot
{
    public CellCoord From { get; }
    public CellCoord To { get; }
    public CombatMovementCause Cause { get; }
    public AttackPullStepSnapshot(CellCoord from, CellCoord to, CombatMovementCause cause);
}
```

Reject null cause and non-adjacent/non-orthogonal step geometry. A Pull step specifically requires Forced/Pull. In existing `AttackPullSnapshot.cs`, retain the four-parameter constructor and add:

```csharp
public IReadOnlyList<AttackPullStepSnapshot> Steps { get; }
public static AttackPullSnapshot FromSteps(EntityId targetId, CellCoord startAnchor,
    IReadOnlyList<AttackPullStepSnapshot> steps);
```

`FromSteps` copies a nonempty list with no nulls, validates continuity from StartAnchor, and derives FinalAnchor and Path from its To cells. The old constructor creates those same typed Forced/Pull steps and keeps its current final-anchor validation. The two views cannot be independently supplied or edited. `CombatPullEvent` is the sole playable path. `AttackResult.Pull` and `AttackPreview.Pull` share immutable context; neither emits another travel occurrence. Interrupted motion contains only reached steps. A no-motion result keeps Pull null.

**Consumer compatibility:** The retained Path compiles with the inspected Pull handler/motion lane; Core X1/X3 verify typed steps, copies and the single playable path. The executor selected this carrier under delegated authorization. Typed-step/reaction consumption by the View owner remains external acceptance. F1 must communicate that there is no corresponding Moved stream.

#### 10.1.2 Interceptions and child attacks

New file `C/Contracts/Results/AttackInterceptionSnapshot.cs`:

```csharp
public sealed class AttackInterceptionSnapshot
{
    public EntityId GuardianId { get; }
    public EntityId ProtectedId { get; }
    public EntityId ActualRecipientId { get; }
    public int DeliveryIndex { get; }
    public AttackInterceptionSnapshot(EntityId guardianId, EntityId protectedId,
        EntityId actualRecipientId, int deliveryIndex);
}
```

Require nonempty IDs, guardian != protected, and index >= 0. The payload itself carries relationships, not a trait requirement or mutable effect state. Current Sacrifice production additionally requires guardian == actual recipient. The enclosing activation supplies the full ResultOrigin (including AttackGroup); do not add an independently writable group field here.

New file `C/Contracts/Results/AttackOutcomeSnapshot.cs`:

```csharp
public sealed class AttackOutcomeSnapshot
{
    public IReadOnlyList<AttackHitResult> Hits { get; }
    public IReadOnlyList<TraitActivationSnapshot> Reactions { get; }
    public AttackOutcomeSnapshot(IReadOnlyList<AttackHitResult> hits,
        IReadOnlyList<TraitActivationSnapshot> reactions);
}
```

Copy both non-null collections. Hits use the native contact form below and have unique actual TargetIds. Reactions have non-null Interception and null ChildAttack, with no duplicate delivery index. Validate reaction/contact correspondence; the enclosing start activation validates creditor/group against its own ResultOrigin. Empty hits/reactions are allowed for an actual empty follow-up shot, not to fabricate a canceled shot. This DTO does not create an action, cost settlement, persistent store or second event stream.

In `C/Contracts/Snapshots/Traits/TraitActivationSnapshot.cs`, retain the existing ten-parameter public constructor with its defaults. Add immutable copy methods instead of changing its arity:

```csharp
public AttackInterceptionSnapshot Interception { get; }
public AttackOutcomeSnapshot ChildAttack { get; }
public TraitActivationSnapshot WithInterception(AttackInterceptionSnapshot interception);
public TraitActivationSnapshot WithChildAttack(AttackOutcomeSnapshot childAttack);
```

Both methods reject null and return a new snapshot, retaining/copying the existing IDs/cells/origin/aim. Interception and ChildAttack are mutually exclusive. WithInterception is valid only for DamageRedirected and requires SourceUnitId == GuardianId, exactly one target == ProtectedId, and a non-null origin with a nonempty attacking creditor and positive group. WithChildAttack is valid only for FollowUpAttack or KillChainAttack, requires SourceUnitId == ResultOrigin.CreditedCharacterId and positive group, and validates every child contact/reaction against that scope. It retains original aim on the start activation. No payload is added to an ordinary activation by default.

Root/Roar `AttackResult.TraitActivations` contains its canonical interception entries plus the existing non-playable ordinary summary. Every native DamageRedirected entry there requires Interception. Child reactions live only in ChildAttack.Reactions at their own existing start activation; the parent summary does not collect them. Reject an interception placed in `CombatEffectsEvent.Traits`, and validate that no child reaction/start is duplicated into the parent summary. State/shield/HP/death events remain ordered facts, not replay occurrences. Ordinary reaction-free legacy activation construction stays available.

`SacrificeDamagePlanner.Plan` remains a disposable simulation. Record the occurrence only after TryConsume succeeds and the real damage boundary applies the redirected delivery, using the returned actual HP loss. Finalize root/Roar results or replace the existing child start activation only after that scope's actual result is known. Zero HP loss from shields is a valid applied reaction; a rejected/unused reservation, canceled hit or InstantDefeat is not.

#### 10.1.3 Actual hits, original contacts and previews

New file `C/Contracts/Results/AttackHitContactSnapshot.cs`:

```csharp
public sealed class AttackHitContactSnapshot
{
    public int DeliveryIndex { get; }
    public EntityId OriginalTargetId { get; }
    public AttackTargetRole Role { get; }
    public int AppliedSplashPercent { get; }
    public CombatDamageOrigin Origin { get; }
    public AttackHitContactSnapshot(int deliveryIndex, EntityId originalTargetId,
        AttackTargetRole role, int appliedSplashPercent, CombatDamageOrigin origin);
}
```

Require index >= 0, nonempty original ID, defined non-None contact role, nonnegative percent, non-Additional percent == 100, and a non-null origin with positive group/valid creditor. PhysicalCell remains nullable under the existing origin contract; current attack producers supply their actual physical origin. A contact has no trait identity, effect duration, guardian slot or duplicate actual-recipient ID: the enclosing hit owns that recipient.

In `C/Contracts/Results/AttackHitResult.cs`, retain the existing seven-parameter constructor exactly and add:

```csharp
public IReadOnlyList<AttackHitContactSnapshot> Contacts { get; }
public AttackHitResult(EntityId targetId, int targetHealthBefore, int targetHealthAfter,
    AttackTargetRole role, int appliedSplashPercent, int healthNormalizationLoss,
    int healthRecovery, IReadOnlyList<AttackHitContactSnapshot> contacts);
```

The old constructor leaves Contacts null (legacy self-contact validation). The new overload requires/copies a non-null collection with no null or duplicate delivery indices. An empty native list is reserved for existing auxiliary hit rows without direct contact. Nonempty rows use the uniform role/percentage pair or None/100 for mixed contacts, as design §8.7.3 defines. Keep the current health/recovery/normalization arithmetic; never combine another recipient's HP with a substituted ID. Across a native result, actual TargetIds and delivery indices are unique; all rows are native rather than a mixture of native/legacy.

A root/Roar result's native contacts match its attacking creditor/group; no child group's contacts or recoveries are included. For every reaction, exactly one hit has TargetId == ActualRecipientId and exactly one contact has its DeliveryIndex, ProtectedId and full origin. Direct contacts to the guardian remain separate contacts without reactions. Repeated protection within a configured cap yields separate reactions/contact indices in one aggregate guardian HP row. Existing originating target/geometry and gameplay contact lists are unchanged.

In `C/Contracts/Results/AttackTargetPreview.cs`, retain all existing constructor signatures and add:

```csharp
public IReadOnlyList<AttackHitContactSnapshot> Contacts { get; }
public AttackTargetPreview WithContacts(IReadOnlyList<AttackHitContactSnapshot> contacts);
```

The hit-based constructor copies/shares the hit's immutable contact collection, including null for a legacy hit. WithContacts copies a non-null collection while preserving every existing preview field exactly. Native auxiliary preview rows use an empty collection and IsDirectHit=false. Root preview generation uses the actual recipient's cells, defenses, HP, recovery and normalization. It also retains non-direct preview rows for an original target's Pull/pre-hit damage or displacement when that original target has no actual hit row. Do not copy a protected target's geometry onto its guardian.

Update `AttackResult.Success`, `SuccessForCell`, `FromFacts`, `AttackPreview.Success`, `SuccessForCell`, `FromFacts`, `AttackTargetRoleValidation.ValidateOrdinary`, `PiercingShotDelivery.ValidateCorrespondence`, and `AttackResult.ValidateKnockbacks` together. Legacy all-null contacts keep existing validation. Native ordinary/piercing checks flatten original contacts; native auxiliary rows do not invent a geometric contact. Piercing encounter count is compared to contacts, not aggregate actual-hit count. Knockbacks are associated with original contact IDs, and preview displacement-only rows remain valid. Factories reject malformed reaction/contact/group relationships before publication.

#### 10.1.4 Implementation owner and consumer obligations

| Scope | Exact owner / member (C prefix from §2) | Minimum migration and required checks |
|---|---|---|
| X1 | `Application/Combat/AttackForcedMovement.ExecuteUnit`, `AttackKnockbackCalculator.Resolve`, `PullAttackCalculator.Execute`, `MovementProjection.RecordForcedMove`; existing movement contracts | Propagate deciding cause, produce typed Pull steps once, preserve non-trait forced movement and ordinary null. Existing X1 gates plus environment-forced-step cause, old constructor arities, empty/blocked/hidden arrival cases. |
| X2 direct/aggregate | `Application/Combat/AttackDirectDamageCalculator.Apply/FlushHealth`, `CommandAttackLedger`, `AttackResultFactory.Create`, `AttackDeliveryExecutor.Execute` | Keep original contact roles for gameplay; capture actual-recipient accounting and scoped recovery at existing freeze points. The executor is an explicit additional inspected dependency for scope capture, not a new owner. Test guardian also targeted, multiple protected targets, repeated contacts, shield absorption, healing/normalization and interruption. |
| X2 child scopes | `Application/Combat/FollowUpAttackExecutor.ExecuteTarget/Execute`, `KillChainAttackCalculator.Resolve` | Build child outcome with its own HP/recovery baseline and existing group, attach to the existing start activation after real application; no new action event or cost. Verify fixed/random/DoubleShot/KillChain independently and nested child groups without parent duplication. Reuse the factory's hit assembly logic. |
| X2 root/Roar | `Application/Combat/SacrificeDamagePlanner.Record`, `CombatAttackCalculator.Execute`, `BossRoarCalculator` | Replace only canonical interception emission; ordinary activation summaries remain scoped. Roar reads embedded result; empty Roar has no fabricated carrier. Verify accepted/unused redirects and Finishing bypass. |
| X2 validation/preview | `Contracts/Results/AttackResult.cs`, `AttackHitResult.cs`, `AttackPreview.cs`, `AttackTargetPreview.cs`, `AttackTargetRoleValidation.cs`, `PiercingShotDelivery.cs` | Use original contacts for selection/piercing/knockback validation and actual recipient facts for numbers. Preserve all existing constructor shapes and legacy branches. Existing X2 gates include targeting, piercing, Pull/knockback and contract-shape tests. |
| X2 copies | `Application/Combat/UnitAttackService.cs` (`AttackExecutionResult`), `Contracts/Combat/CombatResult.cs`, `CombatCommandTransition.cs`, result/preview factories | Existing UnitAttackService clones hit structs and has no trait result carrier; copied immutable contact lists must survive. Do not invent new reaction playback in that legacy wrapper. Verify nested arrays/copies and old callers. |
| X3 | Existing `BufferSystems/Core/` transaction and `BufferSystems/Combat/CombatStateBuffer.cs` publication paths, native result-read fixtures | Validate atomic installation and no duplicate playable interception across Events/results; reject inconsistent proposals before any store changes. Reuse existing state expectations and command identity. Do not calculate gameplay in Buffer. Inspect exact validator ownership before any demonstrated validation edit. |
| View read-only review | `ViewSystem/Combat/Playback/Handlers/CombatPullEventHandler.cs`; `Presentation/Movement/CombatUnitMotionPlayback.cs`; `Playback/Ordering/CombatEventTypeTable.cs`; `Presentation/Attack/CombatAttackTarget.cs`; `Playback/Support/CombatShownFacts.cs` | Pull stays on its single lane/Path; later consumer reads Steps causes. Effects are currently Facts-only in the type table: View must add child/reaction consumption, attacker/group linkage and pre-impact interception handling. Existing attack motion/targeting must distinguish original aim from actual hit recipient. No View code is changed in X. |

All listed new C# files require unique `.meta` files when their owning implementation subphase is authorized. No new assembly, service, manager, generic effect DTO or persistent source registry is planned. Test fixture names and full gates in X1–X3 remain mandatory; this review does not replace them with static inspection.

#### 10.1.5 X0 acceptance state

The source/schema review and follow-up effect mapping are complete. Under the user's later continuous-execution instruction, the executor selected typed Pull steps and the canonical interception/child-outcome/actual-recipient contact schema. One read-only builder check identified the structure movement carrier; §10.1.1 includes the additive correction. There is no unresolved Core carrier decision. View-owner acceptance is unconfirmed, and no buff/debuff runtime/state migration was implemented. The user requested final review after all X work; no intermediate approval is pending.

## 11. Execution log

### 11.1 Phase 0 / P0.1 — execution baseline, 2026-10-29

**Authorization and checkout:** The user explicitly requested Phase 0 only. The repository remains at `D:/My_ProjectWork/Github_desktop/Project_L`, branch `feature/Cheon/Core/NewCombatSytem`, HEAD `e1572985064ab3cf98f9a420916e4d092e8e6f06`. Starting status contains 628 existing dirty paths: 92 modified tracked paths and 536 untracked paths. They are the starting workspace, not changes made by this Phase. No checkout/worktree, commit, push or reset was performed.

**Source correspondence:** All 1,780 base snapshot hashes and all 52 final E+ overlay hashes validate. The overlay contains 45 modified and seven new files; its precedence produces exactly 1,787 C#/asmdef files. Every current source matches the resulting composite. A fresh inventory of CoreSystem, BufferSystems, DataBaseSystem and EditMode tests also contains exactly 1,787 files, with no missing, changed or additional source. The six findings therefore retain the source basis recorded in the remediation design; no drift requires a different implementation decision.

| Manifest | SHA256 |
|---|---|
| `phase-eplus-base/manifest.json` | `7c5ef13cac4dfbf3813f33ece13488fdfa8ec75ce9596048a7d850924871b9c8` |
| `phase-eplus-tested-source-manifest.json` | `437406cc07bf27822148a0e52a2d77b2a5e21c09b83484a0bf96226afe31b417` |

**Verification decision:** Reuse the saved final E+ full gates under P0.1 because source/test hashes match. No Unity process or `Temp/UnityLockfile` was present at capture. The configured Unity 6000.0.73f1 executable exists, the project version agrees, and `run-tests.ps1` parses without errors with `Run`, `Assembly` and `Filter` parameters. Its hidden launch, existing-Editor guard, unique-evidence checks and exact assembly inputs were inspected. This is static runner readiness, not a new Editor/AppData-access execution check. Any later launch still requires the approved AppData-access route and a fresh Editor-ownership check.

| Saved gate, reused without rerunning | Passed / total | Failed | Skipped | Process exit |
|---|---:|---:|---:|---:|
| `Eplus-core-final.xml` | 4102 / 4106 | 4 | 0 | 2 |
| `Eplus-integration-final.xml` | 317 / 322 | 5 | 0 | 2 |

These are historical failed full-suite runs with exact source correspondence, not all-green runs. Direct XML comparison with completed E confirms identical failure fullnames/messages, 107 added Core and seven added CoreIntegration cases, all added cases passing, and no removed cases. Exact messages, stacks, complete case inventories, artifact hashes and current source hashes are retained in the authoritative evidence JSON below.

Core failure prefix: `ProjectL.Tests.Core.`

| Exact remaining fullname after prefix | Saved failure / required treatment |
|---|---|
| `ExperienceAttributionPathTests.DeferredGroundAtPlayerEnd_DoesNotCreateKillsOrExperience` | Expected False, actual True; A3 must re-inspect the ground/death/experience completion path. |
| `WeakGroundTurnTests.ControllerCompletion_DoesNotCollapseOrCountGround(Player,0)` | Expected no fall events, actual `CombatFallEvent`; A3 must re-inspect timing and final-state agreement. |
| `WeakGroundTurnTests.ControllerCompletion_DoesNotCollapseOrCountGround(Enemy,1)` | Same saved assertion; assess separately in A3. |
| `WeakGroundTurnTests.ControllerCompletion_DoesNotCollapseOrCountGround(Enemy,8)` | Same saved assertion; assess separately in A3. |

CoreIntegration failure prefix: `ProjectL.Tests.CoreIntegration.`

| Exact remaining fullname after prefix | Saved failure / scope |
|---|---|
| `BossStagePreparationTests.ProductionBossStage_PreparesAndPlacesBoss` | `KeyNotFoundException`, missing StageKey 2101. No content repair is authorized. |
| `PullAndMarkPresentationTests.Pull_AttackMotionRunsOnlyForDirectHit(False)` | `NullReferenceException` in fixture `Set` (line 473), through `EnableAttack` (line 378). |
| `PullAndMarkPresentationTests.Pull_AttackMotionRunsOnlyForDirectHit(True)` | Same saved fixture failure. |
| `PullAndMarkPresentationTests.Pull_PreviewAndAttackTargetUseArrival(False)` | Same saved fixture failure. |
| `PullAndMarkPresentationTests.Pull_PreviewAndAttackTargetUseArrival(True)` | Same saved fixture failure. |

The four Core cases are not waived for A3 merely because their names are historical. Likewise, an affected or changed integration failure requires investigation; its presence does not authorize View/fixture/content repair in this Phase.

**Issue and fixture readiness:** All 35 issue-to-fixture references resolve to 29 distinct existing fixture files, their `.meta` files, and nonzero selected cases in the saved final E+ XML. Full paths, hashes, namespaces and per-fixture case counts are recorded in `fixtures` in the evidence JSON. In particular, `TraitGrowthStatSettlementTests` and `TraitPresentationContractTests` belong to CoreIntegration, not Core. The future RED cases in §§5–7 have not been added or executed.

| Issue / task | Primary fixture files (T/TI prefixes from §2) | Boundary retained |
|---|---|---|
| TR-01 / A1 | `T/LeapTraitTests.cs`, `TI/LeapPublicationTests.cs` | Explicit tornado entrant; periodic occupant behavior preserved. |
| TR-02 / A2 | `T/EnhancementTargetDamageCapabilityTests.cs`, `T/CannonThresholdTraitTests.cs`, `T/TraitActivationOutcomeTests.cs`, `TI/CannonThresholdPublicationTests.cs` | One exact aggregate/prefix arithmetic owner. |
| TR-03 / A3 | `T/HunterTraitTests.cs`, `T/TornadoEnvironmentTests.cs`, `TI/HunterPersistenceTests.cs`, `TI/TraitActivationPublicationTests.cs` | Projection finalization before narrow lifecycle overrides. |
| TR-05 / B1 | `T/HunterTraitTests.cs`, `T/TraitActivationOutcomeTests.cs`, `TI/HunterPersistenceTests.cs` | Suppress only an actual zero numeric Hunter award occurrence. |
| TR-06 / B2 | `T/BossRoarTests.cs`, `T/TraitActivationOutcomeTests.cs`, `TI/TraitActivationPublicationTests.cs` | Ordered direct structure origin before secondary reaction origin. |
| TR-04 / C1 | `T/TraitDefinitionContractTests.cs`, `TI/TraitDefinitionAuthoringTests.cs` | Registered factory conversion remains authoritative. |

**Evidence and reproducibility:** All artifacts below are in the existing ignored `.superpowers/trait-system/` workspace. `TR-0-capture.ps1` produced `TR-0-baseline-verified.json` and `TR-0-git-status-verified.txt`; `TR-0-verify.ps1` independently validates that record against current source, original XML, artifact hashes and starting dirty-file hashes, then writes `TR-0-verification-final.json`. The initial `TR-0-baseline.json` is superseded because its serializer stringified XML failure elements rather than extracting their text; its source-hash audit was unaffected. Use only the verified record for failure details. No test XML/log was overwritten.

**Actual locality and ownership:** The only repository-visible edit is this plan's status, P0.1 checkboxes and execution log. Additional files are ignored evidence only. Product code, tests, public contracts, state owners, Framework/Buffer/Job wiring, View/Flow, FieldCamera/Architecture, scenes/prefabs, production assets and legacy Missing Script assets are unchanged. SSOT and all five SOLID principles have no implementation delta to assess in Phase 0. Required understanding was the composite precedence, failure identities, fixture assemblies and existing execution contract; verification was hashes/XML/static runner checks. This establishes the A1 baseline, not correctness of the future reproductions or external/manual acceptance.

**Exit:** P0.1 is complete. No source drift or blocking contract conflict was found. TR-01–TR-06 remain unrepaired; A, B, C, X0, X1, X2, X3 and D remain pending with their individual report/stop boundaries. X0's reaction/Pull/hit-carrier agreement remains a later mandatory gate. **Stop before Phase A.** F1/F2 and all external acceptance remain separately authorized owner work.

### 11.2 Phase A / A1–A3 — gameplay boundary repairs, 2026-10-29

**Authorization and baseline:** The user explicitly authorized Phase A and confirmed Phase 0 was already complete. Execution stayed on `feature/Cheon/Core/NewCombatSytem`, HEAD `e1572985064ab3cf98f9a420916e4d092e8e6f06`, in the existing checkout. Before editing, all 1,787 Phase 0 source/test hashes matched; the 628 pre-existing dirty paths were preserved. No worktree, checkout, commit, push, or reset was performed. Tasks ran sequentially A1 → A2 → A3, followed by one fresh review and its bounded correction pass.

**Implemented boundaries:**

| Issue | Actual product edits and retained contracts | Change / understanding / validation scope |
|---|---|---|
| TR-01 / A1 | `TornadoCalculator.Resolve` accepts private optional `EntityId? entrant`; entry supplies the mover, periodic activation keeps unique-occupant lookup. Resolve reads and validates the entrant **after** `CurrentInput` reconciles presence. | One product owner. Understand temporary Leap overlap, presence reconciliation, and forced-arrival sequencing; no change to movement geometry, visibility, costs, trail/Acceleration accounting, or periodic selection. Verify ordinary/Leap entry, hidden/visible preview, shields, native publication and duplicates. |
| TR-02 / A2 | `EffectTargetDamageEvaluation` owns exact `BigInteger` prefix products and `Scale(baseDamage, contributionCount)`. `CombatDamageCalculator.Attack` uses it for both final damage and every ordered evidence prefix. The old internal int aggregate members and their test assumptions were replaced. | Two product files. Before: factor multiplication in evaluator and evidence consumer could diverge. After: one scaling contract; adding a factor does not require another arithmetic implementation. Validate output overflow, cancellation, half-up rounding from the original base, mixed reducing/amplifying evidence, preview, direct/fixed/random extra attacks and Sacrifice. Legacy distance-specific scaling is a separate evaluator, outside this repair. |
| TR-03 / A3 | `MovementProjection.Complete(AttackResult? attack = null, CombatTurnFacts turn = null, CombatFlowPhase? phase = null, bool? isNight = null, CombatAllocationFacts allocation = null)` finalizes existing ground/death/Hunter work before reading projected state and applying only lifecycle intent. `CombatTurnCalculator` updates EnterCombat and both side-ending calls. | Two product files. Callers no longer supply a whole pre-finalization world snapshot. Computed state stays with the projection; Buffer still validates/installs it. Verify exact target/RNG, late terrain source chains, existing collapse timing, turn/allocation/budgets/item RNG, actual next-night entry, stale rejection and inert repeated reads/publication. |

No public result/constructor, serialized identity, service registration, state owner, Framework/Buffer/Job wiring, or production value changed. SSOT is strengthened by one factor arithmetic owner and explicit lifecycle-only overrides. SRP/OCP retain local trait rules and existing extension points; LSP retains gameplay stages and public construction; ISP/DIP require no new interfaces, services or dependencies. The required integration suites remain necessary because these internal calculations feed shared atomic publication.

**Tests and source extent:** Five product files and eleven existing test/helper files changed, listed with hashes in `TR-A-changed-paths.json` and the final manifest. No new product/test `.cs` or `.meta` was created. Test files follow the task map; the only additional helper edit is `TI/ConfiguredMovementBattle.cs`: an optional `deploySniper = true` preserves every existing setup and permits a previously undeployed Hunter to be placed after a real eight-turn night. This reaches the native defect without fabricated combat state or duplicated Framework setup. The integration-owned fixture is shared only inside CoreIntegration; no Core test assembly dependency was added.

**RED → GREEN evidence:** All artifacts below are in `.superpowers/trait-system/`. Every run used the existing hidden `run-tests.ps1` through the approved AppData-access route with Unity 6000.0.73f1. No Unity upgrade or cache deletion occurred. Run names are unique; failed attempts were retained.

| Task / run stem | Behavioral RED | Focused GREEN / native |
|---|---|---|
| A1 | `TR-A1-red-2`: 0/2; visible and hidden `Leap_OccupiedTornado_UsesExplicitEntrant` throw duplicate-occupant `InvalidOperationException`. | `TR-A1-green`: 63/63 across Leap, TornadoEnvironment, InitialTornadoPlacement and TraitActivationMovement; `TR-A1-native`: 4/4 LeapPublication. |
| A2 | `TR-A2-red-2`: 2/4; the 400/100 native attack and ordered-evidence cases overflow, while equivalent 4/1 and reducing-prefix controls pass. | `TR-A2-green-4`: 163/163 across the seven required Core fixtures; `TR-A2-native`: 6/6 CannonThresholdPublication and SacrificePublication. |
| A3 | `TR-A3-red`: 3/10; three new failures plus four known failures. EnterCombat selects target 2 but retains RNG 1 instead of 1015568748; both side endings record collapse but publish the preceding ground state. `TR-A3-native-red`: 1/3; real next-night entry and public result tests retain RNG 1, while stale rejection passes. | `TR-A3-green`: 55/55 Hunter, TornadoEnvironment and WeakGroundMovement; `TR-A3-native`: 20/20 HunterPersistence, TraitActivationPublication and TraitTrailPublication. WeakGroundTurn and ExperienceAttribution are included in the full gate and assessed separately below. |
| Review correction | `TR-A-review-red`: 0/2; ordinary and Leap tornado entry with SoloAction boundary changes throw the current-projection expected-value exception. | `TR-A-review-green`: 78/78 across Leap, TornadoEnvironment, InitialTornadoPlacement, TraitActivationMovement and TraitShield. Final integration also exercises SoloAction in both visible/hidden occupied-tornado cases. |

All listed GREEN/native runs have zero failures and zero skips. Initial `TR-A1-red` and `TR-A2-red` stopped at newly written test member-name compilation errors and are not behavioral RED evidence. A2's first three GREEN attempts caught new fixture mistakes (category-definition mismatch and the exact selected-target-count condition for FocusedBombardment); only fixture setup changed to correct those attempts. The product fix was not broadened.

**Final review and correction:** One independent read-only reviewer found a P2 issue in the first A1 implementation: capturing the entrant before `CurrentInput` could retain an obsolete character object when SoloAction/Cohesion presence changed. Both new regression variants failed before correction. The final code resolves the explicit ID from the reconciled current characters, and the 78-case focused run passed. No additional A2/A3 issue was found. The reviewer deliberately excluded the pre-existing ground timing policy, later Phases, and View/manual acceptance; these remain outside the authorized repair. No second reviewer was dispatched. The corrected sources receive new full gates rather than reusing the earlier gates.

| Final gate | Passed / total | Failed | Skipped | Exit |
|---|---:|---:|---:|---:|
| `TR-A-core-final.xml` | 4117 / 4121 | 4 | 0 | 2 |
| `TR-A-integration-final.xml` | 324 / 329 | 5 | 0 | 2 |

The earlier `TR-A-core` (4115/4119) and `TR-A-integration` (324/329) predate the review correction and are retained as intermediate evidence only. Final XML is checked against E+ for exact failed identities, new/missing cases, messages and stacks. These full suites are not all-green runs.

**Affected baseline failure investigation:** Existing assertions were retained unchanged.

- `ExperienceAttributionPathTests.DeferredGroundAtPlayerEnd_DoesNotCreateKillsOrExperience` still fails at line 65, expected alive/actual dead. Its fixture starts with already-pending ground. Existing `Complete → CollapsePending → CliffFallCalculator → death settlement` already drops that occupant before this repair. Its message is unchanged; A3 does not change the existing death/credit/timing policy.
- `WeakGroundTurnTests.ControllerCompletion_DoesNotCollapseOrCountGround(Player,0)`, `(Enemy,1)`, and `(Enemy,8)` previously passed final-environment equality and then failed on nonempty fall events. They now fail one assertion earlier, at line 33: final environment equality is false, because the final source correctly retains the collapse already present in events. The same pending-ground fixtures and same collapse/death path remain; A3 removes only their stale final-environment override. This is an explained message change, not an additional failure or a new terrain-timing decision.
- The older weak-ground handoff's statement that side endings bypass `Complete` is superseded by the actual tornado integration. The new supported-arrival tests start with unarmed weak ground, perform periodic tornado displacement, and verify entry → tornado completion → collapse → fall with matching final sources on both side endings. No timing policy was inferred from the stale handoff.
- CoreIntegration's missing StageKey 2101 and four `PullAndMarkPresentationTests` fixture null references remain externally owned baseline failures; no content or View fixture repair was included.

**Evidence and reproducibility:** `TR-A-progress.md` records task decisions and intermediate attempts. `TR-A-review.diff` is the initial reviewed Phase A delta; `TR-A-final.diff` includes its correction. `TR-A-tested-source-manifest-final.json` records all 1,787 final source/test hashes, SHA256 `bd571cbc90be8e148b9de8bb2082f2c427396500aa1229c8f4c35f7813af3e3b`. `TR-A-tested/` preserves the sixteen changed files as an overlay on the Phase 0/E+ composite. The final audit script compares current files, added/removed test fullnames, exact failure text/stacks, original dirty-file hashes and the final XML. The authoritative `TR-A-verification-final.json` confirms every current source hash matches the final tested manifest, all 22 added cases pass (15 Core + 7 CoreIntegration), zero cases were removed or skipped, and the original unrelated dirty-file hashes are unchanged. The earlier verification JSON is superseded. Unity compilation succeeded for the existing consumers; this is not Framework/View/PlayMode/manual test acceptance.

**Workspace preservation:** The final status has 633 dirty paths: all 628 starting paths plus five previously clean source/test paths from the sixteen-file edit set. Unity automatically refreshed one FMOD catalog and generated two audio metadata files during import. Their generated contents were retained under `TR-A-unity-generated*`; only those run-created side effects were removed/restored. The catalog matches its original Git blob and working-tree line endings, and every pre-existing unrelated dirty file retains its recorded hash. No legacy Missing Script asset, existing GUID, View/Flow code, scene, prefab, FieldCamera or Architecture file was changed by Phase A.

**Exit:** A1/TR-01, A2/TR-02 and A3/TR-03 are complete under the plan's baseline-aware acceptance rule. The review finding is reproduced and corrected, with focused and full final verification; no unresolved Phase A review finding remains. The full suites still contain the nine explicitly investigated historical failures. B1/TR-05 (zero Hunter award metadata), B2/TR-06 (direct structure origin), C1/TR-04 (single conversion route), X0–X3 and D remain pending. F1/F2 delivery, View integration, production configuration and manual/PlayMode acceptance remain separately owned work. **Stop at Phase A; Phase B was not started.** No commit or push.

### 11.3 Phase B / B1–B2 — occurrence metadata, 2026-10-29

**Authorization / baseline:** The user explicitly authorized Phase B and reported Phase A complete in another session. All 1,787 current source/test hashes matched `TR-A-tested-source-manifest-final.json` before edits. Branch `feature/Cheon/Core/NewCombatSytem` and HEAD `e1572985064ab3cf98f9a420916e4d092e8e6f06` are unchanged. The 633 starting dirty paths were captured in `TR-B-start-dirty.json`. B1 and B2 ran sequentially in the existing checkout; no commit, push, reset or checkout change occurred.

**Implemented change / locality:**

- B1/TR-05: `HunterApplication.TargetDied` calculates the actual owned ATK delta once and emits `AttackGranted` only for a positive delta. The owned proposal, successful-reward count, state event, target handling, credit and reselection remain in their existing owners. No common magnitude-zero filter was introduced; meaningful Roar/Taunt/shield/extra-action events retain their behavior.
- B2/TR-06: `CombatCommandEvents.FindOrigin` recognizes `CombatStructureEvent.DamageOrigins` during the existing ordered scan with the existing creditor match. Direct structure damage can supply the origin before a later explosion. Null/fallback behavior and caller boundaries remain intact. All callers were inspected: Roar, Acceleration consumption and Taunt selection.
- Actual edit scope is two product files and three existing test files: `T/TraitActivationOutcomeTests.cs`, `TI/HunterPersistenceTests.cs`, `TI/TraitActivationPublicationTests.cs`. Existing Hunter positive/non-owner/cap tests remain unchanged and ran. No new C# or meta file, public constructor, state owner, registration, Framework/Buffer/Job responsibility or View calculation was added. A native Roar encounter source lives only in its integration fixture.
- Before/after understanding scope remains the local producer's occurrence unit or the common origin-bearing event contract. Required validation includes Hunter publication/atomicity, Roar/obstacle ordering, Acceleration/Taunt correlation and both full assemblies because those shared contracts feed existing result publication. SSOT and SRP retain the authoritative owned/character facts; OCP uses the existing result kinds; LSP preserves gameplay and constructors; ISP/DIP require no new service/interface/layer. No measured performance or broader independence improvement is claimed.

**Focused verification:** The existing hidden Unity 6000.0.73f1 runner used the approved AppData-access route; no editor upgrade or cache deletion. Evidence names are unique.

| Task / evidence | Result and meaning |
|---|---|
| `TR-B1-red-2` | 1/2: configured zero fails because one numeric grant exists; positive award control passes. |
| `TR-B1-green` | 90/90 HunterTraitTests and TraitActivationOutcomeTests. |
| `TR-B1-native` | 18/18 HunterPersistenceTests, TraitPresentationContractTests and TraitGrowthStatSettlementTests; zero/positive native proposals, stale owned/status/RNG and duplicate behavior included. |
| `TR-B2-red-2` | 0/2: barrel-only Roar gets secondary position (3,2) instead of (3,3); ordinary hostile structure gets null instead of its direct origin. |
| `TR-B2-green-2` | 116/116 BossRoarTests, TraitActivationOutcomeTests and TraitActivationMovementTests. Covers exact origin/credit/group, preceding activation, normal/empty Roar, counter/cost/damage preservation, Acceleration and Taunt. |
| `TR-B2-native` | 7/7 TraitActivationPublicationTests, including real initialized boss/barrel encounter, accepted command, public ordered result origin and inert result reread/duplicate processing. |

Every GREEN/native run has zero failures/skips. Initial `TR-B1-red` was a new test's nullable AttackResult member-access compile error, not behavioral RED. Initial `TR-B2-red` had the valid barrel reproduction plus an invalid unprofiled PowderBarrel fixture; the ordinary-structure variant was corrected to a valid Headquarters definition before `red-2`. Initial `TR-B2-green` was 115/116 because the new test incorrectly expected final counter zero after the secondary explosion hits the boss. The corrected assertion checks the real ordered reset to zero, then preserved final counter one/HP93 for explosion versus zero/HP100 without explosion. No gameplay change was made to satisfy that assertion.

**Single independent review:** A fresh read-only reviewer checked the five-file delta against the Phase A composite and independently inspected the saved focused XML. No actionable product finding or gameplay/ownership deviation was found. One nonblocking coverage note is deferred: Hunter's native zero/positive assertion inspects the applied transition rather than explicitly rereading the public CombatResult and its root summary. Core occurrence/summary assertions and actual Buffer stale/duplicate tests cover the fix; a direct public Hunter result assertion would strengthen evidence. No second review or unrelated implementation was performed.

| Final full gate | Passed / total | Failed | Skipped | Exit |
|---|---:|---:|---:|---:|
| `TR-B-core.xml` | 4121 / 4125 | 4 | 0 | 2 |
| `TR-B-integration.xml` | 329 / 334 | 5 | 0 | 2 |

These full gates are baseline-aware acceptance, not all-green suites. Against Phase A, exact failed fullnames/messages remain unchanged, no case is removed and all nine added cases pass (four Core, five CoreIntegration). The four Core cases are the ExperienceAttributionPath case and three WeakGroundTurn variants enumerated/investigated in §11.2. The five integration cases remain missing StageKey 2101 and four PullAndMarkPresentation fixture null references. Against E+, failure identities remain identical; the three changed WeakGround messages are exactly the already-investigated A3 final-source correction, not a Phase B change. No baseline repair or assertion weakening is included.

**Evidence / preservation:** `.superpowers/trait-system/TR-B-final.diff`, `TR-B-tested/`, `TR-B-tested-source-manifest.json`, and `TR-B-verification.json` retain the five-file delta, exact tested sources, full failed messages/stacks and gate comparisons. `TR-B-audit.ps1` compares all 1,787 source hashes, both earlier baselines, case additions/removals, skips and unrelated starting dirty hashes. Unity refreshed the previously clean FMOD catalog and generated two audio meta files; their generated contents were retained under `TR-B-unity-generated/`, then only those run-created side effects were restored/removed. Product assets, existing meta GUIDs, Missing Script assets, View/Flow, FieldCamera, Architecture, scenes and prefabs are unchanged.

**Exit:** B1/TR-05 and B2/TR-06 are complete under the plan's baseline-aware acceptance rule. The public-Hunter-result assertion remains a deferred minor coverage note, not an unresolved product defect. **Stop before Phase C.** C1/TR-04, X0–X3, D, F1/F2 and View/Framework/PlayMode/manual acceptance remain pending or externally owned. No commit or push.

### 11.4 Phase C / C1–TR-04 — registered conversion, 2026-10-29

**Authorization / baseline:** The user authorized Phase C after completion of B. All 1,787 current source/test hashes matched the final B manifest before editing. Branch `feature/Cheon/Core/NewCombatSytem`, HEAD `e1572985064ab3cf98f9a420916e4d092e8e6f06`, existing checkout and unrelated changes were retained. `TR-C-start-dirty.json` captures the actual starting dirty paths/hashes; the already-dirty FMOD catalog was also copied to `TR-C-audio-before/catalog.asset` before Unity execution. That pre-existing change is preserved, not restored to Git. No commit, push, reset or checkout change.

**Change:** `TraitRegistry.BuildDefinition` retains its signature/null check and delegates to `GetRequiredRegistration(spec.TraitKey).TraitDefinitionFactory.BuildDefinition(spec)`. The 22-case duplicate switch is removed. Registered typed factories, native UnitCatalog construction and CombatCommandEffects binding remain the conversion owners; their source call paths were inspected and unchanged. Deferred availability, serialized identity, runtime states, public constructors and validation ownership are retained.

**Tests / locality:** Only one product file and `T/TraitDefinitionContractTests.cs` changed. `DefinitionConversion_UsesRegisteredFactory` constructs a valid Hunter registration with a local counting/sentinel factory, verifies the exact spec is passed once, and verifies the exact returned instance. `DefinitionConversion_PreservesValidationBoundaries` pins wrapper null rejection, missing registration, and factory null/type mismatch. All 22 existing non-default roundtrips are preserved and additionally construct actual UnitCatalog definitions and check each configured value. Existing availability/deferred, catalog/growth, constructor/reflection and authoring obligations remain intact. No new C#/meta, service, dependency or assembly access was introduced. Native authoring tests were retained byte-for-byte because they already cover all 22 SO/Data/Spec conversions; catalog assertions live in the existing Core friend assembly.

Before, changing a conversion required understanding the registered runtime factory and the parallel test-only switch. After, that mapping is maintained only in its registered factory; normal registry/assembly wiring remains permitted. Validation still covers the shared construction and growth contracts plus both full assemblies; no necessary regression was removed. SSOT removes the demonstrated duplicate transformation; SRP/OCP keep each conversion in its local factory; LSP retains public construction and gameplay; ISP/DIP require no new layer/interface. Framework/Buffer/Job and authoritative state ownership are unchanged. No runtime performance improvement is claimed.

| Evidence stem | Result |
|---|---|
| `TR-C1-red` | 0/2: old switch calls factory zero times and converts a Hunter even with no registration. Both are meaningful contract assertions. |
| `TR-C1-green-2` | 86/86 TraitDefinitionContractTests, TraitGrowthCatalogTests, TraitGrowthRulesTests and CombatTraitContractTests. |
| `TR-C1-native` | 42/42 TraitDefinitionAuthoringTests and TraitGrowthAuthoringTests. |

GREEN/native have zero failures/skips. Initial `TR-C1-green` stopped at compilation of an attempted test calling internal catalog/registry from CoreIntegration and an incorrect UnitRole name. Those unshipped test edits were removed; native catalog assertions were placed in the existing Core contract fixture with `UnitRole.Regular`. No product visibility, friend assembly, dependency or gameplay change was made to satisfy the test. The final integration authoring source matches its B hash.

**Review:** One independent read-only review checked the two-file delta against all B manifest hashes, the registered conversion contract and focused GREEN evidence. No actionable finding or deferred minor was reported. No second review was needed.

| Final gate | Passed / total | Failed | Skipped | Exit |
|---|---:|---:|---:|---:|
| `TR-C-core.xml` | 4123 / 4127 | 4 | 0 | 2 |
| `TR-C-integration.xml` | 329 / 334 | 5 | 0 | 2 |

These are baseline-aware gates, not all-green runs. Exact failure identities/messages match B: the four Core WeakGround/ExperienceAttribution cases and five integration StageKey/Pull fixture cases detailed in §§11.2–11.3. No case is removed or skipped; the two newly added Core cases pass. E+ failure identities remain unchanged; its three WeakGround message differences are inherited from A3, as previously investigated. Existing assertions were not weakened and no unrelated baseline repair was included.

**Evidence / preservation:** `TR-C-final.diff`, `TR-C-tested/`, `TR-C-tested-source-manifest.json` and `TR-C-verification.json` in the ignored `.superpowers/trait-system/` workspace retain the exact two-file delta, 1,787 tested source hashes, failed messages/stacks and baseline/case comparisons. `TR-C-audit.ps1` verifies correspondence and unrelated starting dirty hashes. Run-generated audio files are retained under `TR-C-unity-generated/`; only new meta side effects are removed, and the catalog is restored from its captured pre-run copy. Production assets, existing GUIDs, Missing Script assets, View/Flow, FieldCamera, Architecture, scenes and prefabs are preserved. All Unity runs used the existing hidden 6000.0.73f1 runner through the approved AppData route, without upgrade or cache deletion.

**Exit:** C1/TR-04 is complete. The six original remediation tasks A1–C1 are implemented, but the full amended plan is not complete: **stop before X0**. X0's revised carrier/ownership agreement, X1–X3 implementation/acceptance and D remain pending. F1/F2, View/Framework/PlayMode/manual acceptance remain externally owned and separately authorized. B's previously recorded minor public-Hunter-result coverage note is unchanged and was not expanded into C.

### 11.5 Phase X0–X3 — updated activation contract, 2026-10-29

**Authorization and sequence:** The user first authorized Phase X after accepting A–C, then explicitly instructed completion of all X0–X3 without intermediate stops and delegated implementation decisions for final review. That instruction superseded the X-specific agreement/stop pauses. Execution remained sequential at shared movement/damage/publication boundaries. Phase D, F1/F2 delivery, View implementation, commits and push were not authorized or performed. Branch `feature/Cheon/Core/NewCombatSytem` and HEAD `e1572985064ab3cf98f9a420916e4d092e8e6f06` were retained; no worktree/reset/checkout was made.

**Starting state and concurrent work:** The first X0 source check matched all 1,787 C manifest entries. Before the X1 source edit baseline was copied, a separate baseline-repair task updated `T/ExperienceAttributionPathTests.cs`, `T/WeakGroundTurnTests.cs` and `TI/PullAndMarkPresentationTests.cs`. Their current contents match `TR-X-baseline` and the separately captured `TR-X-external-drift.json`; this session did not edit them. Their saved `BASE9-core-final` and `BASE9-integration-final` contain 4130/4130 and 333/334, respectively. Consequently eight earlier failures are absent independently of X; the remaining failure is missing StageKey 2101. The actual X0 inventory has 640 pre-existing dirty paths, including the already-dirty FMOD catalog. Its pre-run content was saved rather than treating Git HEAD as its original state.

**X0 decisions and follow-up review:** Design §§8.7–8.9 and plan §10.1 specify the selected declarations. The 26-slide status-effect deck was read as future input and mapped to current applied-state owners: attack/defense modifiers, ranges, damage reductions, shields, immunity, Sacrifice, Taunt, Leap, marks, execution, healing and swapping. No duration/stack state, buff runtime, generic effect service or new mutable store was introduced. Deck differences from current gameplay remain future design decisions. One read-only builder check identified structure knockback's existing `CombatStructureEvent` carrier; the same optional movement cause was added there with its old constructor retained. There is no unresolved Core carrier decision or pending intermediate approval. View acceptance remains separate.

| Task | Actual change and ownership | Actual change / understanding / verification scope |
|---|---|---|
| X1 / AX-01 | Immutable `CombatMovementCause` and `AttackPullStepSnapshot`; optional cause on existing character and structure events. Knockback/ImpactBolt record the deciding trait key, environment displacement records Forced/null, and ordinary voluntary moves retain null compatibility. Pull's existing snapshot derives immutable Forced/Pull steps and its retained Path. | Movement owners and public carriers only. No second Pull Moved stream, geometry/cost/hazard/immunity change, or new mutable source. Consumers can read cause without inferring it from unrelated action ordering. Existing Pull/Leap/tornado/structure and native publication gates remain required. |
| X2 / AX-02 | `SacrificeDamagePlanner.Record` returns an immutable applied interception after real consumption/application, instead of inserting a playable Effects occurrence. Root/Roar results own their reactions. Existing FollowUp/KillChain start activations gain `ChildAttack` outcomes with their own contacts, HP and reactions. | Existing planner, direct damage and child executors, ledger and result factory. Original planner simulation, selection priority/cap, InstantDefeat bypass, contact status rules and costs remain. New effects still use the same existing command/result publication. |
| X2 / AX-03 | `CommandAttackLedger` keeps original contact roles for gameplay displacement while recording actual recipients separately. `AttackHitResult.Contacts` retains original target, role, percentage, delivery index and full origin; one actual-recipient HP row aggregates repeated/direct-plus-protected contacts. Root and child scopes capture their own HP/recovery boundaries. | Result/preview/piercing/selection/knockback validators follow original contacts while HP, defense and cells belong to the actual recipient. Old constructors and legacy all-null contacts remain source-compatible. Both numeric and identity migration need shared regression/integration checks; reducing that verification would be unsafe. |
| X3 | Factory/copy, malformed scope/duplicate contact, public read, stale/duplicate and preview purity checks. One reviewed displacement-only preview boundary was repaired at the existing HP freeze. | No new Buffer/Framework/Job logic. The existing freeze dictionary additionally captures original knockback targets; only those new auxiliary preview rows read that frozen HP. Existing Pull/pre-hit auxiliary behavior is preserved. |

**Actual extent and principles:** 25 product files (11 existing calculation/assembly owners and 14 contract files, including five new DTO/enum files) and eight existing test files changed relative to `TR-X-baseline`. Each new C# file has a unique meta GUID. Two existing design/plan Markdown files document the work. The three external baseline test edits are excluded from this count. Source paths are in `TR-X-own-changed.json`, with the final diff and source manifest described below. SSOT keeps original contacts, actual hit accounting and canonical playback ownership explicit; snapshots are immutable projections, not competing authoritative state. SRP retains calculation, occurrence and publication responsibilities; OCP uses existing trait hooks and result carriers; LSP retains old construction while explicitly documenting the requested native semantic migration; ISP/DIP add no service, interface, registry, package or assembly. Normal wiring remains permitted. This is an observed contract/locality change, not a claimed performance improvement.

**RED / focused / native evidence:** All artifacts are in the existing ignored `.superpowers/trait-system/` directory. Every Unity run used the existing hidden 6000.0.73f1 runner through the approved AppData-access route. No Unity upgrade or cache deletion occurred.

| Evidence | Result / meaning |
|---|---|
| `TR-X1-red-2` | 0/5: missing typed movement cause/Pull steps; the earlier attempt used an invalid legacy Knockback fixture and is not the accepted reproduction. |
| `TR-X1-green`, `TR-X1-native` | 194/194 focused and 20/20 native. The final hidden-arrival assertion is included in the X1 full gates. |
| `TR-X1-core`, `TR-X1-integration` | 4137/4137 and 333/334; StageKey 2101 is the sole integration failure. |
| `TR-X2-red-2` | 0/4: root actual recipient remained the protected unit; root and fixed/random child outcomes lacked the new payload. Initial `TR-X2-red` selected zero tests because the test-writing command used an unavailable Python alias; it is not behavioral evidence. |
| `TR-X2-validation-red` | 1/4: defensive-copy control passed; unexplained redirection and duplicate/foreign preview contacts were wrongly accepted. Added checks corrected these three failures. |
| X2 focused attempts | Preserved as `TR-X2-green*` and `TR-X2-boundaries`. Superseded Effects-only Sacrifice assertions were migrated to canonical start ownership. New DoubleShot and Finishing fixtures were corrected to their existing kill/direction/threshold requirements; line targeting hits every selected contact. No gameplay was changed to satisfy those fixtures. |
| `TR-X2-native` | 15/15 through actual initialized battle/Buffer paths, including root/DoubleShot outcome reads and stale/duplicate handling. |
| `TR-X2-core`, `TR-X2-integration` | 4151/4151 and 337/338; no new failure. |
| `TR-X3-focused` | 161/161: immutable payloads, factory and legacy wrapper copies, scope/duplicate validation, movement and reaction regressions. |
| `TR-X-review-red-3` | 0/1: the public auxiliary preview showed HP 0 instead of the attack-freeze HP 5 for Sacrifice + Knockback + a visible destination mine. Earlier attempts exposed a legacy fixture-constructor error and a mine hidden from the enemy attacker; neither was accepted as the defect reproduction. |
| `TR-X-review-green` | 86/86, including the reproduction, Sacrifice, KnockbackPath, PullPreview and contract regressions. The projected final target still dies to the mine, while the root attack preview retains HP 5; gameplay/timing is unchanged. |
| `TR-X3-native` | 27/27: actual publication, root/child reactions, no fictitious Sacrifice movement, preview non-mutation, stale rejection, repeat reads, duplicate handling and single Pull/Leap movement carriers. |

**Single final code review:** The independent read-only reviewer found one P2: new displacement-only preview rows read post-arrival HP instead of the existing attack freeze. The final correction above reproduces and fixes it. No other confirmed gameplay or ownership issue was reported. The initial child-outcome suspicion in the unused Calculate preview was explicitly excluded after tracing the public preview branch; the visible-mine case is the actual public defect. The reviewer also excluded known documentation status cleanup, the three external baseline edits, StageKey 2101, View/PlayMode/manual acceptance and performance claims. Those exclusions are retained as the execution boundary, not counted as passes. No second reviewer was dispatched.

| Final X3 gate | Passed / total | Failed | Skipped | Exit |
|---|---:|---:|---:|---:|
| `TR-X3-core.xml` | 4154 / 4154 | 0 | 0 | 0 |
| `TR-X3-integration.xml` | 337 / 338 | 1 | 0 | 2 |

The final gates and independent source audit have been read. This is a green Core suite and a baseline-aware integration acceptance, not an all-green integration suite. The only remaining baseline failure is `ProjectL.Tests.CoreIntegration.BossStagePreparationTests.ProductionBossStage_PreparesAndPlacesBoss`: `KeyNotFoundException`, missing StageKey 2101. No content repair is included.

**Updated-request acceptance and consumer boundary:**

| Request | Core status and evidence | View / external work still pending |
|---|---|---|
| R1 — meaningful occurrences | Existing E+ producer matrix retained; A–C repairs plus current TraitActivationOutcome/Shield/Reduction/Trail/KillReward tests run in the final suites. No numeric zero Hunter grant is fabricated. | Map keys/kinds to visuals and author production content. |
| R2 — ordered occurrence | Ordinary activations remain before ordered consequences; interception is the explicit R9 exception, available at the owning start. TraitActivationOutcome and Sacrifice order tests verify both. | Pacing, concurrency and hit-release barriers. |
| R3 — multi-target/cell facts | Existing defensive target lists retained; new reaction/contact/outcome collections are copied and validated. Contract tests cover immutable nested copies. | Multi-target visual dispatch. |
| R4 — attack correlation | Full physical origin, attacking creditor and positive group are validated against native contacts; child groups remain separate. Root/child/Roar and malformed-scope tests cover this. | Attack grouping adapters and hit correlation. |
| R5 — continuing state | Existing character trait/debuff/effect state and atomic publication remain authoritative. Activation metadata has no duration/stack state. Existing lifecycle/publication suites remain. | HUD and continuing-effect presentation; later buff/debuff consolidation remains a separate design. |
| R6 — dedicated carriers | Pull/mark/Roar retain their dedicated carriers plus existing activation indications. X adds no duplicate motion/event lane and does not enable deferred summon. | Preserve dedicated handlers while adding activation visuals. |
| R7 — child action identity | Fixed/random/DoubleShot/KillChain retain existing start activations with aim and child outcomes; no new CombatAttackEvent or cost. Current executable producers are covered. Future movement-path attacks are not implemented or claimed. | Consume child outcomes at the start activation and preserve movement playback boundaries. |
| R8 — movement cause | Supported Knockback/ImpactBolt/Pull, structure displacement and non-trait forced movement are typed; ordinary voluntary/Leap compatibility and hidden/no-op cases remain tested. | Read agency/cause; Pull remains exclusively on its dedicated typed-step path. Future voluntary trait displacement needs a concrete producer. |
| R9 — reaction in owning action | Root/Roar TraitActivations and child ChildAttack.Reactions are canonical. Effects contains no native interception occurrence. Native reads expose reactions before consumption/HP events. | Begin interception before impact, await guardian arrival, then release hits. |
| R10 — guardian/protected/actual | Explicit immutable relationship and delivery identity; actual recipient HP/defense/recovery with original-contact geometry and knockback preserved. AoE, repeated hits, direct-plus-protected guardian, shields, priority/cap, no-relay and Finishing regressions remain. | Use original aim and actual contact body correctly; approach/return offsets remain visual only. |

F1 must consume this revised contract rather than the earlier blanket Events-only rule. Constructor source compatibility and Unity compilation do not establish unchanged View semantics or presentation acceptance. No View/Flow, FieldCamera, Architecture, scene, prefab, production value, old Missing Script asset or existing GUID is intentionally changed by this execution.


**Final evidence and preservation:** `TR-X3-source-manifest.json` records 1,792 final source/test files, SHA256 `afe74fa47a1c3020a1b145d59215aeae1f99f64cab7aaf9e755fbb2c063d27d0`. `TR-X3-tested/` stores the changed-source overlay; `TR-X-final.diff` compares only this execution's 33 source/test changes against `TR-X-baseline`. `TR-X-audit.py` writes `TR-X-verification.json`, checking exact current hashes and inventory, external baseline edits, unrelated starting dirty-file hashes, existing meta preservation, five unique new meta GUIDs, whitespace, full XML identities/messages/stacks and test additions/removals. Final cases versus the independently repaired starting suite: 24 added Core and four added integration cases, all passing; zero removed/skipped cases and identical remaining failure identity. Compilation of the existing Unity consumers succeeded. This is not a Framework/View test-suite, PlayMode or manual acceptance claim.

Unity refreshed the catalog and generated two audio metadata files. Their generated contents are retained under `TR-X-unity-generated/`; only those run-created side effects were restored/removed. The catalog exactly matches the already-dirty pre-X copy, every unrelated pre-existing dirty hash is unchanged, and the three external test changes are preserved. Final Git status contains 660 paths versus 640 at entry, accounting for new files/meta and previously clean edited sources; no commit or push occurred.

**Exit:** X0, AX-01/X1, AX-02+AX-03/X2 and X3 are complete under the updated request and delegated continuous-execution authority. The single final review finding is corrected, focused/native/full gates are read, and no Phase X product finding remains open. Stop before Phase D. F1/F2 delivery, actual View reaction/movement consumption, production configuration and external/manual/PlayMode acceptance remain separately owned work. This completes Phase X, not the entire remediation plan.


### 11.6 Phase D / D1 — integrated review and evidence closure, 2026-10-29

**Authorization / source:** The user explicitly requested Phase D after Phase X completion. D1 is a review/evidence-only task; product corrections require reopening their owning Phase with authorization. At entry, branch `feature/Cheon/Core/NewCombatSytem`, HEAD `e1572985064ab3cf98f9a420916e4d092e8e6f06`, all 660 dirty paths, and all 1,792 X3 source/test hashes were captured in `TR-D-start.json`. The current inventory has no missing or added source. Both X3 XML hashes match the saved verification record. The X3 full gates are therefore reused under §8 and the §10 amendment; Unity was not launched again.

**Six-issue disposition:** Implementation, focused verification and native evidence are separate columns. Inputs/expected behavior below are the original design §5 cases, not newly expanded gameplay requirements. All evidence stems resolve inside `.superpowers/trait-system/`.

| Issue | Original input → required result / implemented owner | Focused verification | Native path / disposition |
|---|---|---|---|
| TR-01 / A1 | Leap crosses an occupied tornado cell → apply the tornado to the explicit mover. `TornadoCalculator.Resolve` resolves that ID after current-presence reconciliation; periodic occupancy selection is retained. | `TR-A1-red-2` 0/2 → `TR-A1-green` 63/63. The later SoloAction reference-freshness reproduction and correction passed `TR-A-review-green` 78/78. | `TR-A1-native` 4/4 Leap publication; final suites retain the cases. **Implemented and verified.** |
| TR-02 / A2 | ATK 10 with factors 2×2×1.5×4, including 400/100 → damage 240 without intermediate int overflow. `EffectTargetDamageEvaluation.Scale` owns aggregate and ordered-prefix arithmetic; `CombatDamageCalculator` calls that owner for both. | `TR-A2-red-2` 2/4 with two reproductions failing → `TR-A2-green-4` 163/163; rounding, cancellation, reducing/amplifying prefixes and final overflow boundaries covered. | `TR-A2-native` 6/6 Cannon/Sacrifice publication. **Implemented and verified.** Distance scaling is a different existing stage, not a duplicate target-factor implementation. |
| TR-03 / A3 | Next-night Hunter selection at RNG 1 → target 2 and RNG 1015568748 together; late terrain events agree with final sources. `MovementProjection.Complete` finalizes pending work before lifecycle-only overrides from three turn callers. | `TR-A3-red` 3/10 includes three new failures and four historical failures; `TR-A3-native-red` 1/3. `TR-A3-green` 55/55 and final A failure investigation retained. | `TR-A3-native` 20/20 actual next-night/atomic publication. **Implemented and verified.** Historical weak-ground fixture repairs are separately attributed below. |
| TR-04 / C1 | Configured trait roundtrip must execute the registered factory used by runtime. `TraitRegistry.BuildDefinition` delegates after null validation; native UnitCatalog and CombatCommandEffects keep the same registration route. | `TR-C1-red` 0/2 → `TR-C1-green-2` 86/86, including all 22 non-default mappings and native catalog construction. | `TR-C1-native` 42/42 SO/Data/Spec authoring cases; this is native conversion evidence, not a new Buffer transaction feature. **Implemented and verified.** |
| TR-05 / B1 | Hunter(0,3) owner kill → retain cap/target/state progression but emit no zero numeric ATK grant. `HunterApplication.TargetDied` calculates the actual owned delta once and publishes only positive awards. | `TR-B1-red-2` 1/2 → `TR-B1-green` 90/90; positive/non-owner/cap behavior preserved. | `TR-B1-native` 18/18 persistence/authoring-settlement checks. **Implemented and verified**, retaining the nonblocking public-Hunter-summary coverage note in §11.3. |
| TR-06 / B2 | Barrel-only Roar at (3,3) → origin (3,3), boss creditor, direct group 1 rather than barrel explosion group 2. `CombatCommandEvents.FindOrigin` recognizes structure origins in its existing ordered scan. | `TR-B2-red-2` 0/2 → `TR-B2-green-2` 116/116; ordinary structure, character, empty Roar and other callers covered. | `TR-B2-native` 7/7 including real boss/barrel result reads. **Implemented and verified.** |

**R1–R10 closure:** The acceptance matrix in §11.5 remains the handoff-ready correspondence record and was rechecked against current contracts, production owners and the unchanged final gates. R1–R7 retain applied occurrences, ordering, targets/cells, origin correlation, character-owned ongoing state and dedicated carriers. R8 uses the actual deciding movement cause and the single Pull step carrier. R9 uses root/Roar attack reactions or the owning child start outcome. R10 records guardian/protected/actual recipient plus original contacts, actual-recipient accounting and preserved displacement policy. The X review correction freezes new displacement-only preview HP at the existing attack boundary. Future movement-path attacks, deferred traits, buff/debuff state migration and actual View replay remain outside the implemented acceptance claim.

**Combined ownership/locality review:** The verified E+ composite (1,780 base entries plus 52 overlay entries, 1,787 unique sources) was compared to current X3 sources. `TR-D-combined-files.json` and `TR-D-combined.diff` isolate 49 remediation source/test files; three independent baseline-test edits are flagged and excluded. No unrelated dirty tree is treated as this implementation. The actual combined delta is 32 product and 17 test/helper files, including the five additive X contract files.

- SSOT: one target-factor scaling owner and one registered trait conversion route remain; original contacts and actual recipients have distinct contracts, while character/owned/environment facts retain authoritative state.
- SRP/OCP: tornado-entry identity, projection finalization, local occurrence production and result assembly remain with their existing owners. New trait rules use normal registration; no universal effect framework or speculative interface was introduced.
- LSP: old constructor arities, valid zero configurations, existing gameplay stages, costs and lifecycle responsibilities are preserved. Native reaction/hit semantics deliberately migrate under R9/R10 and are not described as unchanged playback behavior.
- ISP/DIP and Framework/Buffer/Job: no new service, module registration, dependency, polling loop, event channel or persistent store was added. Existing factories/exports and scheduling remain unchanged; Core calculates, Buffer validates/installs, and the existing Job/Flow owns scheduling. Necessary shared regression/integration checks were retained.
- Actual locality: factor and conversion changes no longer require maintaining parallel arithmetic/mappings. Reaction/movement work necessarily spans producers, immutable result contracts, validators and existing consumers; this dependency is explicit rather than hidden. No performance or whole-codebase independence improvement is claimed.

**Exact verification lineage:** `TR-D-evidence.json` independently parses each full XML and all 31 accepted focused/RED/native evidence artifacts, checks saved changed-source overlays against their phase manifests, and records exact failed names/messages/stacks plus added/removed test identities. A/B/C saved overlays have no hash discrepancy; X's overlay includes the explicitly separated three external edits. No source was removed between phase manifests.

| Checkpoint | Core pass / total | CoreIntegration pass / total | Skipped |
|---|---:|---:|---:|
| E+ before remediation | 4102 / 4106 | 317 / 322 | 0 |
| A final | 4117 / 4121 | 324 / 329 | 0 |
| B final | 4121 / 4125 | 329 / 334 | 0 |
| C final | 4123 / 4127 | 329 / 334 | 0 |
| Independently repaired baseline before X | 4130 / 4130 | 333 / 334 | 0 |
| X3 final, reused by D | **4154 / 4154** | **337 / 338** | **0** |

The remediation phases added **61 passing cases** (45 Core, 16 CoreIntegration: A 22, B 9, C 2, X 28), with no test removal in their own phase comparisons. The separate baseline work adds four Core case identities and removes one old identity, a net three cases:

- `ExperienceAttributionPathTests.DeferredGroundAtPlayerEnd_DoesNotCreateKillsOrExperience` was replaced by `UnexhaustedGroundAtPlayerEnd_DoesNotCreateKillsOrExperience(1)` and `(5)`. The old fixture started already pending collapse; the replacement checks valid unexhausted ground and adds explicit environment/fall/XP assertions. This is an external fixture correction, not silent deletion or evidence that the old pending-ground policy was implemented.
- `WeakGroundTurnTests.ControllerCompletion_DoesNotCollapseOrCountGround` retains its three old case identities, changes the fixture to one entry before collapse, and adds threshold 1/5 cases. A3's earlier failure-message shift is retained in §11.2 and the evidence JSON: stale final environment was corrected while the already-recorded collapse path remained. The subsequent passing fixture changes are not credited to A3 or X.
- Four `PullAndMarkPresentationTests` cases keep their names and assertions but construct the current projectile/melee SO parts instead of reflecting obsolete fields; their previous null references disappear. That work is preserved as external test maintenance, not View implementation by this remediation.
- The sole remaining failure is `ProjectL.Tests.CoreIntegration.BossStagePreparationTests.ProductionBossStage_PreparesAndPlacesBoss`, with the same `KeyNotFoundException: Stage data not found. StageKey: 2101.` The fixture explicitly requests operating DB key 2101. This is the previously recorded content/test-input failure; no content repair is included. Core is green; CoreIntegration is baseline-aware acceptance, not all green.

**Independent combined review:** One fresh read-only reviewer inspected the 49 owned combined source/test files, checked current hashes and saved final XML, and reported **ready for D closure; no new actionable defect**. The reviewer confirmed TR-01–06, root/child/Roar reaction scope, actual-recipient accounting, original-contact displacement, child recovery boundaries and the ownership/registration conclusions above. No correction or owning-phase reopening was required. Declined judgments were View replay/hit waits/Pull consumption/animations/scenes; F1/F2/production/PlayMode/manual acceptance; future buff/debuff/deferred traits; StageKey 2101 and external test repairs; and the wider dirty checkout or broad refactoring. These remain outside D, not implicit passes. No second reviewer, product edit or Unity run occurred.

**Remaining owners / handoff boundary:** F1 may use §10.1 declarations, §11.5 R1–R10 and this issue/evidence ledger when separately authorized. F1/F2 delivery was not performed and no message was sent to another worker. View owns typed movement consumption, original-aim versus actual-hit routing, child/interception start handling, guardian approach/return and hit-release timing. Production configuration, StageKey 2101 repair, PlayMode/manual/device acceptance, and future status-effect design remain separate. B's minor missing direct public Hunter summary assertion remains a coverage note rather than a confirmed product defect. No new Framework/View-suite or performance acceptance is inferred from Core/CoreIntegration gates.


**D-only verification and preservation:** `TR-D-verification.json` records the final correspondence to X3 and D entry: 1,792 matching source/test hashes, unchanged inventory and X3 XML hashes, all 660 starting dirty paths preserved, and only this plan changed among repository-visible files. `TR-D-evidence.json` retains the phase source-chain and XML/case comparisons; `TR-D-combined.diff` and `TR-D-combined-files.json` are the bounded combined review package. No tests were skipped, deleted, renamed or modified in D. No source, asset, meta, Framework/View/Flow file or branch state was changed.

**Exit:** D1 is complete. Six original repairs and AX-01/02/03 have implementation and recorded verification; the combined review is closed with no unresolved new product finding. The sole historical content failure and the existing minor Hunter coverage note remain explicitly recorded. This closes the authorized remediation plan through D, with no claim of all-green integration or external presentation acceptance. **Stop here; F1 handoff and F2 production/manual acceptance require separate authorization.** No commit or push.
