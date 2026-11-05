# Trait System A–E+ Review and Remediation Design

Date: 2026-10-29

Status: The original six-repair design and the amended Phase X contracts are implemented under subsequent explicit user authorization. Phase X is complete; final verification, the single review correction and external acceptance limits are recorded in implementation plan §11.5. The original design review was documentation-only; its historical findings below are not current unresolved defects. View implementation, Phase D, external acceptance, commits and push are outside this execution.

Amendment precedence: Section 8 records the user's instruction to prioritize the updated activation request. Its reaction ownership and actual-hit requirements supersede earlier blanket compatibility/Events-only statements in this document. The six original defects and their repairs remain valid; AX-01/02/03 are additional required contract work.

Design check before the section 8 amendment: A fresh read-only builder review found no material unanswered question preventing the original six-repair Task plan. The 1,787 reviewed product/test sources and the governing documents matched their captured contents. That check does not resolve the new carrier decisions identified by section 8 and Phase X0, and is not runtime acceptance.

## 1. Intent and scope

Review the completed configurable trait implementation through Phase E+, determine which code serves a real contract, identify duplicated or bypassed calculation/publication paths, and combine the earlier review findings with newly confirmed defects.

The recommended change is bounded correction at existing owners. Preserve approved gameplay, public constructor compatibility, stable trait identities, Framework/Buffer/Job responsibilities, and existing Core/View ownership. A lower file count is not an acceptance criterion.

### 1.1 Sources and stable review baseline

Repository: `D:/My_ProjectWork/Github_desktop/Project_L`

Branch: `feature/Cheon/Core/NewCombatSytem`

Base HEAD: `e1572985064ab3cf98f9a420916e4d092e8e6f06`

The reviewed implementation includes uncommitted work and is **not** represented by HEAD alone.

The stable source is the following composite:

1. `.superpowers/trait-system/phase-eplus-base/`: the completed A–E snapshot, 1,780 files, checked against its `manifest.json`.
2. `.superpowers/trait-system/phase-eplus-tested/`: the final E+ overlay, 52 files, checked against `phase-eplus-tested-source-manifest.json`; 45 modified files and 7 new files.
3. For a path listed in the E+ manifest, use the tested overlay. For every other path, use the A–E base. The resulting source contains 1,787 C#/asmdef files across CoreSystem, BufferSystems, DataBaseSystem, and EditMode tests.

Every snapshot hash matched its manifest. Every corresponding working-tree source hash matched this composite at review capture. Working-source links below therefore identify the reviewed code and line numbers; the composite remains the recoverable evidence if subsequent work changes those files. The older `phase-eplus-reviewed/` and `phase-eplus.diff` precede final corrections and are not the final implementation baseline.

Requirements and ownership references:

- [Approved trait behavior and technical boundaries](2026-10-28-trait-system-design.md), especially §§2, 6, and 7.
- [Implementation plan and completed E+ record](../plans/2026-10-28-trait-system-implementation-plan.md), especially §7A.2 and §§11.6.1–11.6.3.
- [Repository instructions](../../../AGENTS.md) and [Assets instructions](../../../Assets/AGENTS.md).
- [Framework architecture](../../../Assets/ProjectL/Script/Systems/Documentation/framework-architecture.md).
- [Framework programmer guide](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md).

The finalized E+ execution record takes precedence over the earlier external request draft and provisional wording. In particular, damage-modifier magnitude is a delta at its numeric stage, the root attack summary has its documented group/credit scope, Intimidation application magnitude is attack-reduction percent, and unequal shield recipient outcomes may reside in paired state results.

### 1.2 Completed and external scope

| Scope | Review treatment |
|---|---|
| A–E | Completed gameplay, authoring, state, and publication implementation; reviewed for correctness and necessity. |
| E+1–E+4 | Completed activation payload, producers, ordered state/source results, public projections, and publication checks; now included in defect classification. |
| F1 | Concrete Core-to-View handoff remains pending. |
| F2 | Approved production content and external/manual acceptance remain pending. |
| View/UI/VFX/scenes/prefabs/Flow | External ownership; missing consumer connections are not defects in this review. |
| Main/enhanced content, unset production magnitudes | Content decisions; no values are invented here. |
| Position Swap / Recon Squad and other deferred traits | Availability and previous deferral remain unchanged. |

### 1.3 Existing test evidence, not new execution

The retained XML was read directly and compared with completed E:

| Saved artifact | Passed / total | Failed | Skipped |
|---|---:|---:|---:|
| `E-core-final.xml` | 3995 / 3999 | 4 | 0 |
| `E-integration-final.xml` | 310 / 315 | 5 | 0 |
| `Eplus-core-final.xml` | 4102 / 4106 | 4 | 0 |
| `Eplus-integration-final.xml` | 317 / 322 | 5 | 0 |

Failure-name sets and failure messages match. The recorded E+ delta is 107 Core and 7 CoreIntegration cases, with no removed cases. These are historical implementation results. This review did not run tests, reproduce a failure in Unity, or establish View/manual acceptance.

The six findings below are established through source control flow, valid inputs, arithmetic, and existing test assertions. Proposed regression cases are work for a subsequent authorized implementation.

## 2. Review result

Five P2 correctness/public-contract issues and one P3 duplication issue are confirmed. The four earlier findings remain present after E+. Two additional defects concern completed E+ activation metadata.

| ID | Priority | Origin | Confirmed issue |
|---|---|---|---|
| TR-01 | P2 | Earlier review, retained | Leap temporarily overlaps an occupied tornado cell, violating tornado resolution's implicit single-occupant assumption. |
| TR-02 | P2 | Earlier review, expanded by E+ | Unreduced integer factor products overflow; E+ independently repeats the unsafe product for contribution evidence. |
| TR-03 | P2 | Earlier review, retained | A caller's early state snapshot overrides late movement-completion RNG/environment changes. |
| TR-05 | P2 | New E+ finding | A configured zero Hunter award emits an ATK-grant occurrence despite granting no ATK. |
| TR-06 | P2 | New E+ finding | Roar origin lookup skips direct structure damage and can select a secondary barrel-explosion origin. |
| TR-04 | P3 | Earlier review, retained | A test-only 22-trait conversion switch duplicates the registered runtime factories. |

The E+ final review already corrected Sacrifice insertion, KillChain ordering, zero Vanguard/Morale lifecycle metadata, and the Intimidation unit description. Those corrections are present; they are not reopened as missing work.

## 3. Code necessity and route audit

### 3.1 What the added file count represents

The read-only Git inventory found 266 new C# files under the reviewed project source/test roots relative to the base checkout: 228 product/editor files and 38 test files. This is a source-file inventory, not a claim about total changed lines or a measurement of runtime cost. Unity `.meta` files are not counted.

| New-file family | Count | Actual responsibility / disposition |
|---|---:|---|
| Authoring, pure Data, and authoring selector | 45 | 22 authoring/Data pairs plus the existing-pattern SerializeReference selector. Retain Unity serialization and pure-data separation. |
| Core contracts | 55 | Typed specs/statuses and shared immutable state/result contracts. Retain required lifetime, identity, and copy boundaries. |
| Trait definitions | 22 | Runtime rule configuration created from specs. Retain the registered definition contract. |
| Trait runtime states | 22 | Required typed registration and initial status construction. These are not a second authoritative combat store. |
| Trait factories | 22 | Existing registry's definition/runtime-state factory pairs. Retain; remove the separate duplicate switch in TR-04. |
| Trait rules | 24 | 22 local mechanic rules plus area/shield rules. Retain local policy ownership and shared geometry/budget rules. |
| Trait effect adapters/state snapshots | 26 | Existing capability bindings and immutable command inputs. Retain live adapters; no blanket replacement with a generic effect language. |
| Core application owners | 12 | Cross-unit/step application, lifetime, and damage planning. Retain the owners required by accepted mechanics. |
| Tests | 38 | Contract, behavior, and native publication coverage. Preserve obligations when changing helpers. |
| **Total** | **266** | **File count alone does not establish excess.** |

E+ itself adds only seven C# files: `CombatEffectiveStatsSnapshot` and six test files. Its other 45 changed files extend existing execution/publication owners.

The native construction path was traced:

`UnitTraitAuthoring.GetData → pure Data → TraitSpecAdapter → registered definition factory → UnitCatalog / BattleCharacterCalculator → TraitEffectProvider initial status`.

Command execution then reads current immutable status through `CombatCommandEffects.Capture`, uses the existing rule/effect bindings, calculates disposable command state, and publishes through existing Buffer transactions. Authoring, initial runtime construction, current committed status, and result snapshots serve different contracts and lifetimes. Their similar names do not establish redundant state ownership.

### 3.2 Necessary alternate paths versus actual duplication

| Path | Assessment | Design action |
|---|---|---|
| Registered factory conversion versus `TraitRegistry.BuildDefinition` switch | The switch is an alternate implementation used by the round-trip test, while native catalog and command binding use factories. | Remove duplicate conversion via TR-04. |
| Aggregate target factors versus E+ prefix evidence factors | Both manually compose the same unchecked-size rational representation; a fix in one leaves the other failing. | One safe arithmetic owner via TR-02. |
| `CombatAttackCalculator.Reaction` bypasses ordinary admission/cost | Deliberate existing boss-reaction contract; it still uses shared selection/damage/displacement. | Preserve. Do not charge another action or route through ordinary admission. |
| Sacrifice disposable simulation | Uses cloned facts and non-published results to select a redirect before committing defensive changes. | Preserve common calculation ownership; no independent HP store or event channel. |
| Preview projection | Calculates disposable state/results, including RNG, without installing them. | Preserve. Regression must distinguish calculated preview events from committed occurrences. |
| `TraitPresenceReconciler.Apply` without event output | Has a real Sacrifice-simulation caller. | Keep; it is not an unauthorized publication route. |
| `AttackResult.TraitActivations` summary | Derived from ordered events with the finalized root-interval/group/credit filter. | Keep as compatibility summary; never replay it as a second event stream. |
| Contextual DEF and public DEF projection | Gameplay needs the current world; public/event-time consumers need a retained derived value. Both use the same local Cohesion rule. | Preserve this distinction and test agreement; do not add a new aura store or View calculation. |
| Legacy constructor overloads | Existing callers and reflection tests depend on their precise shapes. | Preserve additive compatibility, including the two-argument `CharacterTraitEffects` constructor. |

An unused-code search must include reflection. For example, `TraitRestrictionTests:253–256` and `TraitShieldTests:96–97` invoke `TraitTurnLifecycle.EndControllerTurn` by name. Its lack of a direct C# call does not justify deleting it.

Small potentially unused forwarding methods and codec arms are not sufficient reason for a hierarchy rewrite. They are outside the required correction set. If later removed, first prove all native and reflection callers and preserve their existing behavioral tests. No entire trait runtime/status/factory family is classified as unnecessary.

### 3.3 Structural conclusion

- **SSOT:** Owned Hunter ATK, character/debuff effects, and environment sources remain the authoritative facts. Derived numbers and ordered events are projections, not another state owner. TR-02 and TR-04 remove demonstrated duplicate transformations.
- **SRP:** Tornado entry identifies the arriving entity; projection completion owns final projected state; local trait producers own their occurrence semantics. No feature needs another feature's formula to perform these corrections.
- **OCP:** Reuse registered factories and existing effect/result contracts. Do not add trait-specific arithmetic repairs to every producer.
- **LSP:** Preserve old constructors, dedicated result behavior, existing damage rounding, reaction costs, and valid zero configurations.
- **ISP/DIP:** No new service interface, Framework module, manager, polling loop, or Buffer is proposed. Existing injected facts and result contracts are sufficient.
- **Framework/Buffer/Job:** Core calculates, Buffer prepares/validates then installs, and existing Job/Flow owners schedule. Ordinary registration and shared integration checks remain justified work.

## 4. Chosen remediation approach

### 4.1 Recommendation

Correct the six demonstrated issues at their current owners, share only the duplicated conversion/arithmetic operations, and retain all supported gameplay/public contracts.

This is the author's recommendation for review, not a claim that the user has approved implementation.

### 4.2 Alternatives considered

1. **Bounded owner corrections and demonstrated consolidation — recommended.** Small change boundaries, explicit before/after contracts, and focused tests plus the required full gates.
2. **Replace the typed trait architecture with a universal handler/configuration framework.** Rejected: it would change serialization/registration/state contracts, widen understanding and regression scope, and has no demonstrated benefit for these defects.
3. **Patch symptoms in each caller, suppress all zero magnitudes, or fix only published RNG.** Rejected: this leaves duplicate arithmetic, suppresses meaningful zero-damage decisions, or retains the stale projection-state mechanism.

No new balance decision, trait eligibility rule, target policy, damage-stage ordering, action price, or stage/Run persistence policy is proposed.

## 5. Confirmed findings and proposed corrections

### TR-01 — Explicit tornado entrant during Leap transit

**Requirement:** Trait design §§2.3 and 7.3 permit occupied intermediate cells for Leap. Existing non-mine entry behavior remains active. Tornado placement permits character-occupied cells.

**Evidence:** [TornadoCalculator.cs:35](D:/My_ProjectWork/Github_desktop/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/TornadoCalculator.cs:35), especially its occupant query at line 53; [CombatMovementCalculator.cs:117](D:/My_ProjectWork/Github_desktop/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatMovementCalculator.cs:117); [MovementProjection.cs:141](D:/My_ProjectWork/Github_desktop/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/MovementProjection.cs:141).

**Input:** Leap owner at (0,0), another living unit plus an active tornado at (1,0), free landing at (2,0), route (0,0) → (1,0) → (2,0). Provide valid movement funding/range and a surviving battle objective.

**Expected:** Apply the existing tornado entry behavior to the arriving mover and return a valid interrupted/completed movement result.

**Actual:** Enter temporarily places both units at (1,0). Tornado resolution performs `SingleOrDefault` over both living occupants and throws. A visible tornado reaches the same failure through preview.

**Correction:** Carry the already known `mover` from `ResolveEntry` into private resolution. Periodic tornado activation retains its normal unique-occupant lookup. Validate the explicit entrant against the current cell. Do not pick an arbitrary first occupant, prohibit previously permitted Leap transit, or skip all intermediate hazards.

**Change / understanding scope:** Tornado entry versus periodic resolution and the existing movement interruption contract. No change to Leap geometry, damage formulas, or Buffer ownership.

**Regression:** Occupied tornado transit for visible/hidden sources; crossed unit unchanged; ordinary entry and periodic activation; preview non-mutation; forced relocation interrupts the route; one cost settlement; trails and Acceleration reflect only completed voluntary steps.

### TR-02 — One exact factor composition for damage and evidence

**Requirement:** Trait design §§6.2 and 7.1 require valid configured factors to multiply with existing rounding. E+ §11.6.2 requires metadata from the same applied numeric stages without changing aggregate damage.

**Evidence:** [EffectTargetDamageEvaluation.cs:52](D:/My_ProjectWork/Github_desktop/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/CombatEffects/Evaluations/EffectTargetDamageEvaluation.cs:52); [CombatDamageCalculator.cs:103](D:/My_ProjectWork/Github_desktop/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatDamageCalculator.cs:103). The second file independently repeats the product at lines 104–109.

**Input:** Unmoved ATK 10 actor with StationaryFire(100), TrackingShot(50,100), FocusedBombardment(1,50), and ContemptForWeak(400,100). Exactly one living enemy has ATK 5, HP 1000/1000, DEF 0; matchup is 1. All definitions and their combination are currently accepted.

**Expected:** 10 × 2 × 2 × 1.5 × 4 = 240 damage and HP 760.

**Actual:** The unreduced numerator 200 × 200 × 150 × 400 equals 2,400,000,000 and throws in checked int multiplication. The equivalent Contempt ratio 4/1 avoids that failure. After fixing only the aggregate evaluator, the E+ evidence loop would still throw.

**Correction:** Make the existing target-factor evaluation own exact composition and scaling for both the aggregate and its ordered prefixes. Use internal exact rational arithmetic, preferably the already available BigInteger facility, with reduction as appropriate. Reject only an unrepresentable final damage integer, not a large intermediate representation of a small valid ratio. Preserve existing capability and public activation contracts; add no trait-specific arithmetic branches or new dependency.

The aggregate is rounded once at its existing stage. Every prefix is evaluated against the same pre-factor damage using the same half-up rule, not by repeatedly rounding the prior prefix's damage. Evidence retains the current absolute difference between adjacent rounded prefix values and current origin ordering. Do not change minimum damage, shield order, bypass decisions, or modifier units.

**Change / understanding scope:** The internal target-factor result, its damage consumer, and their exact-ratio contract. A factor change should no longer require maintaining two arithmetic implementations.

**Regression:** The four-factor input; 400/100 versus 4/1; existing three-factor/half-up cases; aggregate and prefix consistency; reducing factors as well as amplifiers; direct/additional/Sacrifice/preview paths. For mixed increasing/decreasing factors, compare each absolute prefix delta rather than incorrectly requiring their unsigned sum to equal the net change.

### TR-03 — Finalize projection state before applying turn overrides

**Requirement:** Trait design §§7.1 and 7.5 and implementation E3 require Hunter target selection and captured RNG progress to publish atomically. Projection-owned effects must appear in the final state/result together.

**Evidence:** [MovementProjection.cs:455](D:/My_ProjectWork/Github_desktop/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/MovementProjection.cs:455); early state captures at [CombatTurnCalculator.cs:54](D:/My_ProjectWork/Github_desktop/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatTurnCalculator.cs:54), 75 and 127; the supplied state's environment/progress is consumed by [CombatCommandCalculator.cs:94](D:/My_ProjectWork/Github_desktop/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatCommandCalculator.cs:94).

**Input:** After night recall, deploy a previously unused Hunter while two surviving enemies, IDs 2 and 3, are EnemyWaiting. Hunter target is absent, stage rewards are zero, both enemies have current HP 100 and valid remembered origins, and the objective is alive. With no arrival effect and environment RNG 1, enter combat.

**Expected:** Redeployment makes both candidates eligible; target 2 and RNG 1015568748 are published together.

**Actual:** Hunter selection consumes that draw inside Complete, but the earlier caller-supplied state wins over the final projection. Target 2 and selection/state events are retained while final RNG remains 1. The next draw can reuse it.

**Correction:** Complete pending projection-owned work, including existing ground/death/Hunter processing, before constructing the final state. Apply only the caller-owned turn/phase/night/allocation changes to that fresh state. The three identified callers need those narrow overrides, not a replacement snapshot of the whole world.

Prefer replacing the internal full-state override with narrow optional values or an equally explicit local merge. Preserve current budgets, structures, traits, sources, environment progress, reward state, and item RNG from their existing owners. Do not add another state cache or move RNG into Hunter.

A RandomState-only assignment is insufficient: the same stale-state preference can discard late terrain changes. This correction must preserve the existing **timing** of ground collapse while making final sources agree with the already-recorded changes.

**Change / understanding scope:** Movement completion and the three controller-transition call sites. The caller controls lifecycle intent; the projection controls computed world facts.

**Regression:** Next-night Hunter participation with exact target/RNG; unchanged living lock consumes no draw; both controller-end hazard paths with tied survivors; supported tornado arrival reaching weak-ground collapse; ordered source/final-state agreement; stale/duplicate publication and preview purity.

The four baseline WeakGround/ExperienceAttribution failures intersect this completion owner. Re-inspect their cause and assertions during this repair. Do not waive a changed failure because its name is historical, and do not expand this correction into a new terrain-timing policy.

### TR-04 — Remove the parallel definition-conversion switch

**Requirement:** Repository SSOT/feature-independence rules and plan §2.2 retain normal registry/factory wiring without duplicate implementations of the same conversion.

**Evidence:** [TraitRegistry.cs:130](D:/My_ProjectWork/Github_desktop/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/Traits/TraitRegistry.cs:130); [TraitDefinitionContractTests.cs:70](D:/My_ProjectWork/Github_desktop/Project_L/Assets/Tests/EditMode/TraitDefinitionContractTests.cs:70); native construction at [UnitCatalog.cs:103](D:/My_ProjectWork/Github_desktop/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/BootstrapModels/UnitCatalog.cs:103) and `CombatCommandEffects:55`.

**Concrete input/path:** The round-trip case Cohesion(3,7,2,2) calls the Registry switch. Native construction calls CohesionTraitDefinitionFactory. The switch has 22 such mappings and its only source-tree caller is that test.

**Expected:** The round-trip assertion exercises the actual registered conversion.

**Actual:** The test and gameplay use separate copies. Updating a tunable mapping requires maintaining both; the test does not validate the runtime copy. This is a confirmed maintenance/verification defect, not a claim of an already incorrect Cohesion value.

**Correction:** Keep the wrapper if useful, but delegate to the existing required registration and its TraitDefinitionFactory after the existing null check. Remove the 22-case switch. All 22 mechanics are now registered, so the Phase A implementation-pending rationale no longer needs this alternate path.

**Change / understanding scope:** One conversion owner and its tests, instead of two mappings. Keep typed factories, type checks, availability, serialized identities, and constructor validation.

**Regression:** All 22 non-default tunable round trips through registered factories; native catalog construction; existing null, type mismatch, missing registration, deferred availability, and authoring rejection cases. Do not invent another factory abstraction.

### TR-05 — Zero Hunter bonus is not an ATK-grant occurrence

**Requirement:** Finalized §7A.2 excludes zero configured contributions from committed activations. The producer matrix defines Hunter AttackGranted magnitude as Run ATK.

**Evidence:** [HunterApplication.cs:65](D:/My_ProjectWork/Github_desktop/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/HunterApplication.cs:65), with unconditional emission at lines 70–72; [HunterTraitSpec.cs:13](D:/My_ProjectWork/Github_desktop/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Commands/Traits/HunterTraitSpec.cs:13) explicitly permits zero.

**Input:** Hunter(0,3), zero successful rewards, a living owner with a locked target, and an owner-attributed lethal hit.

**Expected:** Existing cap/status/reselection behavior is retained, owned ATK is unchanged, and no numeric AttackGranted occurrence is emitted.

**Actual:** The producer emits AttackGranted with magnitude 0 while the owned ATK does not increase.

**Correction:** Compute the actual awarded delta once and emit the numeric grant only when it is positive. Keep stage-counter progression, target handling, owned-change composition, and the exact state result unchanged. This is metadata correction, not a change to zero-configured Hunter gameplay.

Do not generalize this into suppressing every zero magnitude. Empty-area Roar, selected Taunt, shield transitions, successful extra actions, and prevented displacement can be meaningful under their finalized contracts.

**Change / understanding scope:** The Hunter award producer and its occurrence unit. No change to kill credit or the common reward consumer.

**Regression:** Actual zero-bonus owner kill and native publication; unchanged Run ATK; preserved cap/reselection; absence of AttackGranted in Events and its derived summary; positive reward behavior and stale/duplicate publication.

### TR-06 — Preserve the direct Roar result origin before secondary reactions

**Requirement:** Finalized §7A.2 requires ResultOrigin to retain the physical cell, creditor, and attack group of the related result, with the trait owner kept separately.

**Evidence:** [BossRoarCalculator.cs:95](D:/My_ProjectWork/Github_desktop/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/BossRoarCalculator.cs:95); [CombatCommandEvents.cs:47](D:/My_ProjectWork/Github_desktop/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatCommandEvents.cs:47); [ObstacleReactionCalculator.cs:56](D:/My_ProjectWork/Github_desktop/Project_L/Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/ObstacleReactionCalculator.cs:56).

**Input:** The supported `BossRoarTests.Input(5, barrel: true, withTargets: false)` shape: boss 2 at (3,3), powder barrel 40 at (3,2) with HP 1, and a pending Roar. The nonempty reaction destroys the barrel.

**Expected:** RoarTriggered.ResultOrigin matches direct structure damage: physical cell (3,3), creditor 2, group 1.

**Actual:** FindOrigin ignores CombatStructureEvent. It subsequently finds the barrel's CombatObstacleReactionEvent and returns physical cell (3,2), creditor 2, group 2. Roar is thus correlated with the secondary explosion. An unprofiled structure-only hit can instead yield null correlation.

**Correction:** Extend the existing bounded origin lookup to recognize CombatStructureEvent.DamageOrigins in event order before proceeding to later events. Preserve existing creditor matching and fallback semantics. Do not reconstruct an origin from SourceUnitId, allocate an animation group, or scan preceding unrelated command segments.

**Change / understanding scope:** Origin-bearing result types and the existing Roar caller. The correction needs no obstacle-damage formula change or new event channel.

**Regression:** Barrel-only Roar, ordinary hostile structure, ordinary character hit, and empty-area Roar. Assert all three origin fields, activation before the dedicated result, and unchanged damage, counter reset, and action cost. Check other existing FindOrigin callers for preserved behavior.

## 6. Verification and implementation boundaries

### 6.1 Proposed work order

This is a dependency outline, not an implementation Task plan.

1. **Gameplay boundaries:** TR-01, TR-02, TR-03. Add focused failing behavioral cases before changing owners; keep shared movement/damage edits coordinated.
2. **Completed E+ metadata:** TR-05 and TR-06, then rerun the affected ordering, origin, summary, and lifecycle cases.
3. **Demonstrated consolidation:** TR-04, preserving all existing conversion and constructor acceptance obligations.
4. **Integrated review gate:** exact final-source capture, affected regressions, both full assemblies, and a review of the final combined diff.

Follow repository phase review stops. Do not overlap implementers in shared movement, damage, or command-finalization owners.

### 6.2 Required evidence

- Focused RED/GREEN must exercise valid native inputs where available; compile errors and fabricated successful events are not behavioral reproduction.
- Run affected movement/Tornado/Leap, Hunter, damage-factor/Cannon, Sacrifice/reflection, Roar, activation/source, and authoring/catalog suites according to the changed owner.
- Validate actual Buffer submission for target/RNG/source/result atomicity, stale proposals, duplicates, and result rereads.
- Run full Core and CoreIntegration gates after the integrated correction, using fresh output names and comparing exact failed fullnames and messages to the saved E+ baseline.
- Re-evaluate any baseline failure on a changed path. The four Core WeakGround/ExperienceAttribution cases need particular scrutiny for TR-03. The missing StageKey 2101 and the four Pull presentation fixture initialization failures do not authorize unrelated repairs.
- Hash the final tested sources and retain the evidence. An implementation report must distinguish focused fixes, full gates, compilation, and external/manual acceptance.
- When later authorized to run Unity, use the approved AppData-access route from AGENTS.md and coordinate with any running Editor. Do not upgrade Unity or delete caches for the known sandbox issue.

### 6.3 Preservation requirements

No commits/pushes by Codex. No deletion of legacy Missing Script assets. Preserve new/existing `.meta` GUIDs and serialized managed-reference names. Do not edit View/Flow/FieldCamera/Architecture as a side effect. Keep gameplay comments in Korean and implementation documentation in English.

A change to a confirmed gameplay decision, public ownership contract, or implementation scope requires design review before implementation.

## 7. Risks, rejected candidates, and limits

| Item | Disposition / next evidence |
|---|---|
| Nested follow-up/Sacrifice input uses an older owned collection in some paths | No concrete wrong native outcome was established. Current actor/raw values and projected child inputs matter. Retain a targeted integration investigation; do not implement a speculative owned-state rewrite. |
| Repeated direct character packet after first-hit death | Injected-delivery tests exercise this abstraction, but no current native producer of duplicate direct character packets was identified. Establish supported reachability before classifying or repairing; do not conflate this with the explicitly excluded duplicate-obstacle scenario. |
| Ground Compaction strongest-source selection repeated for metadata | Current behavior agrees, including the test-pinned selected-allowance magnitude. A future small shared selector may be useful, but no current divergence justifies a required refactor here. |
| Mixed shield recipient amounts represented by magnitude 0 | Allowed by the finalized paired-state contract; not a defect by itself. Preserve exact recipient state results. |
| Broad FindOrigin / empty-root DoubleShot hypothesis | The proposed path is not established: root completion precedes DoubleShot creation, and nested scopes reserve ancestor Acceleration. TR-06 uses the concrete structure/barrel path instead. |
| Event-free lifecycle wrappers | Some are called by reflection tests. Do not delete them based only on direct-call search. Preserve the actual simulation path and migrate tests deliberately if a future cleanup is approved. |
| Public derived Cohesion DEF | Retain the current Core-derived projection and same-rule calculation; do not treat a derived read value as a second authority merely because it is stored. Regressions must continue checking final/event-time agreement. |
| Runtime cost / allocations | No profiling or device evidence was gathered. The source inventory is not proof of a performance problem or proof that every allocation is cheap. |

This review establishes the listed defects and concrete maintenance duplication, not exhaustive correctness of every possible trait combination. Existing saved test success does not prove the new reproducer cases pass.

F1 must use the corrected, verified public contracts when it is authorized. F2, View playback/concurrency/debuff preservation/trail rendering, Flow, production balance, scenes/prefabs, Framework/PlayMode/manual/device acceptance remain external work. Their absence is not part of this remediation defect count.

## 8. Authoritative updated activation request — 2026-10-29 amendment

### 8.1 Precedence and evidence

The user explicitly instructed that the updated request document takes precedence over the earlier E+ interpretation. Apply that instruction to the comparison and future implementation plan; it does not authorize product implementation in this documentation turn.

Source: [Updated activation request](C:/Users/권천명/Downloads/2026-10-29-combat-trait-activation-core-request.md), SHA256 `1F7CA43DFBCA07B5998A1A13141AE7C013B6139B7EF0E4D0E83B7C7629811575`. This document was read as requirements/source material, not as an instruction to send a message, start implementation, or contact its named recipient. No earlier attachment revision was supplied, so this is a comparison against implemented E+, not a claim about the author's edit history.

Its historical inventory at HEAD efa5e7d7 is stale: there are now 37 executable producer identities, expanded activation fields/kinds, Sacrifice, and typed trail results. That inventory does not override current source facts. Its requested behavior, especially R8–R10 and Q9–Q13, takes precedence where it conflicts with earlier E+ design. The 1,787-source completed composite still matched the working tree during this comparison.

The original six defects remain six defects. The three additions below are required contract-extension work, not retroactive claims that completed E+ violated its former scope.

### 8.2 Request-to-implementation reconciliation

| Request | Current evidence | Disposition |
|---|---|---|
| R1 meaningful activations | Completed producer matrix and applied-result metadata; zero Hunter remains wrong. | Retain coverage and TR-05. Do not recreate all producers. |
| R2 cause-before-result | Implemented for ordinary outcomes/trails. Sacrifice currently emits DamageRedirected in Events after attack start. | Ordinary ordering stays; introduce R9's explicit pre-impact reaction exception. |
| R3 multiple targets/cells | TraitActivationSnapshot has defensive-copy TargetIds/TargetCells. | Implemented; preserve. |
| R4 attack correlation | ResultOrigin includes physical cell, creditor and group; group alone is not globally unique. | Retain this stronger existing correlation; fix TR-06. |
| R5 persistent versus transition facts | Character/debuff/owned/environment facts plus CombatTraitStateEvent. | Implemented; preserve state owners. |
| R6 dedicated results | Pull, mark and Roar dedicated results remain alongside activation metadata. | Preserve dedicated results. Deferred Necromancy is not reactivated. |
| R7 additional attack aim | AttackDirection/TargetCell exist; current extra attacks do not create ordinary CombatAttackEvent. | Preserve. Do not add hypothetical path attacks or generic hooks. |
| R8 movement cause | CombatCharacterEvent:104 has before/after, damage origins and tornado source, but no self/forced agency or causal trait. AttackForcedMovement:54 creates unannotated Moved. | Required extension AX-01. |
| R9 reaction available at attack start, no second occurrence | Existing E+ treats Events as the playback source and AttackResult.TraitActivations as a derived summary. SacrificeDamagePlanner.Record:68–78 publishes DamageRedirected into Events. | Supersede that policy for intercepting reactions; required AX-02. |
| R10 guardian/protected/actual recipient/group | SourceUnitId and TargetId currently encode guardian/protected unit, but there is no explicit actual-recipient field. AttackDirectDamageCalculator:105 records the original contact ID, and AttackResultFactory:22–28 builds Hits from those IDs. | Required AX-03; current Hits are not guaranteed to mean actual redirected recipient. |

Q1–Q7's intent is retained by existing meaningful outcomes, typed keys/kinds, grouped recipients, origin/aim and dedicated results. Do not renumber enums merely because the older inventory listed eight kinds. Q8 does not require undoing already completed legacy producer work. Q10/Q13 agree with keeping Sacrifice's Core position unchanged and leaving presentation offsets to View. Q11 is a constraint on real displacement/death facts, not permission to change knockback gameplay. Q12 retains the current Core selection and one guardian per protected hit, with existing configured per-turn limits.

### 8.3 AX-01 — Explicit movement cause

Add an immutable movement-cause value at the existing movement result boundary: agency (self movement or forced displacement) and causal trait key when the motion is caused by a trait. Preserve constructor compatibility with optional trailing payloads, enum stability, copies and existing tornado source identity. Null on an ordinary move retains ordinary voluntary semantics; an unrecognized cause must not be silently relabeled as voluntary.

Populate causes from actual decisions: Knockback and ImpactBolt have distinct trait keys and forced agency. Pull is forced/Pull. Leap traversal remains voluntary; Acceleration or a trail does not itself cause movement and must not be mislabeled as its mover. Do not create fictitious Sacrifice departure/return Moved events or trigger hazards for visual motion.

**Pull integration constraint:** Pull deliberately calls ExecuteUnit with publishMovedEvents:false and publishes CombatPullEvent instead. Simply adding another playable Moved stream would duplicate travel. To meet the updated request's per-step cause intent while retaining R6, the contract step must explicitly choose and document one playable movement carrier: either typed steps within the retained Pull result with the same cause contract, or Moved steps with the dedicated Pull path changed to non-replayed context. The former is the recommended minimal adaptation of R8; exact consumer acceptance of that carrier must be recorded before claiming R8 complete. This is not permission to silently drop Pull from cause coverage.

No new dash, retreat, per-step attack, displacement scheduler, or trait manager is included. Actual position, arrival hazards, costs, Acceleration and trail rules stay with current owners.

### 8.4 AX-02 — A reaction occurrence has one owner and is readable before impact

The prior blanket statement that Events is the only activation playback source is superseded as follows:

- Ordinary activation occurrences remain in ordered CombatResult.Events; their existing AttackResult summary remains non-playable.
- An intercepting reaction is canonical in the owning attack's result-side activation data, available when that attack presentation begins. It is not additionally emitted as a playable CombatEffectsEvent occurrence.
- Damage, shield/counter consumption, health/death and subsequent real displacement remain ordered result events. State records are effects of the reaction, not a second occurrence.
- Root attacks use AttackResult.TraitActivations as requested. The same occurrence must survive the CombatAttackEvent, public result copies and immutable read path without becoming a second playback source.
- Roar reactions must be readable in that Roar's embedded AttackResult. Child/follow-up attacks need the corresponding child's result/activation carrier, not the outer root's unrelated summary or a new ordinary CombatAttackEvent. This carrier must be pinned for DoubleShot, fixed/random follow-ups and KillChain before producer edits.
- Only successfully consumed/applied redirects are included. Discarded planning candidates, canceled hits and rejected commands produce no canonical reaction.

A typed reaction payload on the existing activation/result contracts is acceptable; a second persistent event bus or per-frame polling store is not. Prefer sharing the same immutable occurrence between result copies over constructing separately writable duplicates.

This changes existing public playback semantics under the user's new precedence instruction. Update the earlier E+ summary/ordering documentation and its tests deliberately. Preserve gameplay assertions rather than retaining tests that demand the now-superseded duplicate occurrence placement.

### 8.5 AX-03 — Explicit interception and actual hit recipients

For every accepted Sacrifice interception, expose the guardian, protected original target, actual numeric damage recipient, and the complete existing CombatDamageOrigin. Preserve the distinction between trait owner and creditor. SourceUnitId can remain the guardian and canonical target data can identify the protected unit; a typed interception member supplies any otherwise missing identity instead of requiring View to infer it from later HP events.

Keep the selected aim/PrimaryCell/PrimaryTarget semantics distinct from actual Hits recipients. Update result construction/validation together: a redirected Hit identifies the actual recipient and its own HP/recovery/normalization values, while a separate original-target relation retains targeting/contact semantics. Do not merely replace TargetId and leave the protected unit's HP/role/geometry attached to the guardian.

Group plus creditor identifies an attack scope, not every hit within it. AoE, repeated strikes, a guardian also directly hit, or a configured guardian protecting more than once must retain a deterministic per-delivery association. Use a local hit/recipient relationship in the existing result; do not introduce a global activation-number service. Pin aggregation and identity rules before changing the ledger.

Core Sacrifice remains a numeric redirection: no relocation, extra AP, mine/thorn/ground contact, or invented return path. Preserve original-versus-guardian defenses, shields, no relay, priorities/caps, instant-defeat bypass, kill credit and Run ATK. Real death/displacement uses actual Core coordinates. Existing original-contact knockback policy must not be changed merely by changing presentation Hits.

### 8.6 Acceptance and ownership

The corresponding Phase X in the implementation plan is required before claiming full compliance with the updated request. It extends the scope beyond the six repairs while keeping View code external.

Acceptance includes: typed movement cause on each supported trait movement carrier; root/child/Roar interception readable at the start of its owning presentation; exactly one reaction occurrence; explicit original/actual target correspondence; blocked/no-op/Finishing cases; AoE/multi-source/repeated-hit relationships; immutable copies; stale/rejected/duplicate publication; preview non-mutation; and unchanged gameplay.

View remains responsible for approach/return offsets, animations, hit-release waits, projectile contact bodies, pacing/grouping adapters and scene assets. Core tests and successful compilation do not prove those presentation behaviors. The attachment's historical claim that View cannot break is not accepted as validation evidence. F1 must communicate the reaction ownership exception, Pull carrier decision and actual-hit migration explicitly.

### 8.7 X0 contract selected for the authorized continuous Phase X execution

The user authorized Phase X after accepting A–C and subsequently explicitly delegated decisions and requested all X0–X3 without intermediate stops. That later instruction overrides X-specific agreement/stop wording below; the executor selects this contract as the implementation basis, while external View acceptance remains unconfirmed. The current 1,787 source/test hashes match the final C manifest. X0 produces the concrete contract below and the exact API declarations in implementation plan §10.1; the executor selected these declarations under that delegated authorization. Actual implementation and tests are recorded separately in plan §11.5; no View-owner or playback acceptance is inferred from authorization.

The updated activation request still has the §8.1 hash. A second input, `C:/Users/권천명/Dropbox/Project L/기획 문서/상태이상 기획서.pptx`, has SHA256 `2AD4315A94EEAAB91EB4983D3570A92E4AD9EC40C0DC0DD1CB7F8833B7858EA1`. Its 26 slides were inspected for the requested follow-up compatibility review. It is future design input, not authority to change current gameplay or state ownership.

#### 8.7.1 Alternatives and proposed decisions

| Boundary | Proposed choice | Alternative and reason not selected |
|---|---|---|
| Pull travel | Keep `CombatPullEvent` as the only playable travel carrier. Add immutable typed steps to its existing `AttackPullSnapshot`; retain `Path` as a derived compatibility projection. | Publishing `Moved` as well would duplicate motion. Replacing Pull playback with `Moved` requires a larger consumer migration and changes the existing pre-hit movement barrier. |
| Child attack reactions | Add a small `AttackOutcomeSnapshot` to the existing FollowUpAttack/KillChain activation at its existing start position. It contains only actual hits and canonical reactions. | Reusing a full `AttackResult` would require synthetic action-cost and selection metadata for fixed/random hits that currently use Designated delivery without an ordinary attack selection. A new `CombatAttackEvent` is prohibited by R7. |
| Actual hit recipients | Keep one `AttackHitResult` per actual recipient in an attack scope, with immutable per-delivery original-contact records. | One hit row per delivery would break the existing recipient aggregation and can repeat HP/recovery totals. One original-target field cannot describe a guardian protecting multiple targets or also being directly targeted. |
| Later non-trait effects | Movement agency and hit/interception relationships are independent of trait identity; only genuine trait activations require a TraitKey. | A generic effect source taxonomy, effect-state container, or service is not needed to carry the current facts. Later non-trait activation playback must be designed with its actual producer rather than assigned a fake trait. |

These are the implementation author's decisions under delegated execution authority, not a claim that the user or View owner selected each API detail. The typed Pull carrier and attack-result/contact schema are the implemented basis. The existing numeric gameplay remains fixed by §8.

#### 8.7.2 Playback ownership and identity

| Action / result | Canonical start carrier | Movement / reaction owner | Correlation |
|---|---|---|---|
| Ordinary attack, including direct ImpactBolt damage | Existing `CombatAttackEvent.Attack` | Interceptions in that `AttackResult.TraitActivations`; ordinary activation entries remain non-playable summaries of Events. | Attacker + AttackGroup within the published command result; full origin on each interception/contact. |
| Roar | Existing `CombatRoarEvent.Attack` | Same reaction rule in the embedded result. Empty Roar retains null Attack and no fabricated reaction. | Roar's embedded attack scope, never the outer command actor. |
| DoubleShot / same-direction follow-up | Existing FollowUpAttack `TraitActivationSnapshot` in `CombatEffectsEvent` | `ChildAttack.Reactions` and `ChildAttack.Hits`, finalized before the start activation is installed. | That activation's ResultOrigin and its own child group. No ordinary attack event or extra action cost. |
| Fixed/random follow-up | Existing FollowUpAttack activation | Same child outcome. Its original target/aim stays on the activation; its actual recipient is in the child hits. | A separately allocated child group; local delivery index 0. |
| KillChain | Each existing KillChainAttack activation | One child outcome per chain strike, excluding subsequent chain/follow-up scopes. | Existing strike group; local delivery index 0. |
| Knockback / ImpactBolt displacement | Existing `Moved` events | `MovementCause = Forced` with the deciding Knockback or ImpactBolt key. Existing knockback summary remains non-playable. | Character ID and ordered step facts; retain existing damage origins where present. |
| Pull | Existing `CombatPullEvent.Movement.Steps` | Every reached step carries Forced/Pull. `AttackResult.Pull` and preview copies describe the same path but do not start another replay. | Existing target ID and step order; no extra `Moved` stream. |
| Ordinary move / Leap traversal | Existing `Moved` | Null cause retains voluntary compatibility. Leap changes traversal eligibility, not movement agency. Trails/Acceleration are consequences, not movement causes. | Existing mover/path ordering and action-cost settlement. |
| Tornado / non-trait forced arrival | Existing `Relocated` / `Moved` | Forced agency with no trait key; tornado retains `TornadoSourceId`. | Existing environment identity. Null on a legacy Relocated event does not mean ordinary voluntary motion. |

Interceptions are identified by a non-null typed `Interception` payload, not by magnitude or an assumption that every activation in an attack summary should play. Native DamageRedirected entries must have that payload. Their canonical owner is exactly one root/Roar result or one child's Reactions list. Copies share immutable occurrences. They never also appear as a playable entry in `CombatEffectsEvent.Traits`. Ordinary effects, defense consumption, state changes, HP/death and real movement remain in ordered Events.

The child start activation is finalized/replaced at its already-recorded insertion position after its actual delivery completes. No mutable placeholder is published. A child outcome contains only interception reactions, not child-start activations or nested child outcomes. Descendant attacks keep their own existing start positions. This avoids recursive replay and makes the complete reaction available before the consumer releases that child's hit.

#### 8.7.3 Contact versus actual-recipient accounting

`PrimaryTargetId`, selected cells, piercing encounters, `deliveredVictims`, mark/restriction contact, Roar hit counting, item direct-contact candidates, and knockback selection keep their current **original-contact** meaning. In particular, do not replace `CommandAttackLedger.Roles` globally with guardian IDs: `AttackKnockbackCalculator` consumes it. Add actual-recipient result accounting in the same disposable ledger, not a persistent store or a second damage calculator.

Native hits have a non-null Contacts collection. Each contact records the zero-based original delivery index, original target, original role/splash percentage, and complete damage origin. The enclosing hit's TargetId is the actual recipient. Delivery indices come from the existing delivery loop before filtering; gaps from canceled deliveries are allowed, renumbering is not. Fixed/random/chain strikes use index 0 in their own groups. A contact is recorded only for an actually delivered contact; unused Sacrifice reservations have none.

One hit aggregates one actual recipient at the existing hit-health freeze point. HP before/after, recovery and normalization all belong to that recipient and that attack scope. Child scopes capture their own starting HP and recovery counters, then subtract earlier command-level recovery when finalizing; they must not reuse the parent's initial HP or cumulative recovery. Existing pre-hit Pull/environment treatment and post-freeze arrival/chain boundaries remain intact. The factory must use `plan.AttackGroup`, not its current hard-coded Direct group, when a scope-specific result is needed.

An actual recipient may have contacts `(0, protected A)`, `(1, guardian itself)`, `(2, protected B)`. It still has one HP row. Each real interception points to its own contact index and protected target; the guardian's direct contact has no interception. When all contact roles/percentages agree, retain that pair on the aggregate hit. Otherwise aggregate Role is the existing None and percentage is 100; Contacts is the authoritative targeting description. Existing auxiliary hit rows without a direct contact use an empty native Contacts collection. Null Contacts denotes an old constructor's legacy self-contact interpretation, and retains old validation. Native results must not mix legacy and native hit rows.

Selection/piercing validators validate the flattened original contacts, not actual recipient IDs or aggregate row counts. Native piercing correspondence compares its encounters to original contacts even when several contacts aggregate into one guardian. Knockback records are validated against original contacts (or legacy target IDs), not against guardian IDs alone. Preview rows use actual recipients and their occupied cells, copy contact information, and retain non-direct rows needed for Pull/pre-hit damage or an original target's displacement. No additional rules are calculated in View.

Every interception requires an actually consumed/applied redirect. Its guardian equals the current Sacrifice activation's SourceUnitId and actual recipient; the protected ID equals the activation's single target and its matching contact's original target. Its activation ResultOrigin must equal the matching contact's full origin, with a positive group and the attacking creditor. Magnitude remains actual HP loss, including zero when an applied interception is absorbed by a shield. Finishing/InstantDefeat, unused candidates and rejected commands have no interception. No positional offset, return path, AP or hazard entry is created for Sacrifice.

### 8.8 X0 follow-up buff/debuff compatibility map

Prefixes here resolve to `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/`: `A/ = Application/Combat/`, `C/ = Contracts/Combat/`, `S/ = Contracts/Snapshots/Traits/`, `R/ = Domain/Traits/Rules/`, `I/ = Contracts/Items/`. These are inspected current owners; the deck column describes proposed future behavior only.

| Deck slides / concept | Current rule and applied-state owner | Activation fact versus continuing effect | X contract impact / follow-up decision |
|---|---|---|---|
| 4, 14: ATK increase/decrease | `A/TraitAttackNotifications.cs`, `A/TraitKillSettlement.cs`, `A/TraitTurnLifecycle.cs`; `C/TraitAttackGrant.cs` and `C/CharacterTraitEffects.cs`; `S/AccelerationTraitStatusSnapshot.cs`; `A/HunterApplication.cs` and owned Hunter bonus; `C/IntimidationDebuffState.cs` / `A/CombatDebuffApplication.cs`. | Grants/consumption/expiry are occurrences; amounts, held bonus, debuff and Run bonus are state with different lifetimes. | Do not turn activations into a buff list. General flat/percent stacking and duration unification are future design; Hunter Run ownership and Acceleration attack consumption remain. |
| 5, 15: DEF changes | `A/CombatEffectiveStatsCalculator.cs`, `A/TraitPresenceReconciler.cs`, `A/ArmorBreakerApplication.cs`; `C/ArmorBreakState.cs`, `C/DefenseReductionDebuffState.cs`; `I/ItemTemporaryEffect.cs`. | Event-time effective DEF is derived; ArmorBreak stacks and item reductions are applied state, with distinct lifetimes. | Contact/actual-recipient fields must identify whose defense was used. General buff stacking is deferred; do not duplicate the effective-stat formula in payloads. |
| 6–7, 16–17: move/attack ranges | `A/TraitTrailCalculator.cs`, `C/TraitTrailFact.cs`, `Domain/Movement/EnvironmentTerrainRules.cs`, `A/CombatActionAdmission.cs`; immutable unit stats and environment sources. | GroundCompaction/DustScreen source creation/expiry is an occurrence; a unit's current range is derived at the existing query boundary. | MovementCause describes the cause of a step, never a range modifier or cached effective range. Generic timed range buffs are future work. |
| 8, 18: dealt/received damage percentages | `A/CombatDamageCalculator.cs`, `Domain/CombatEffects/Evaluations/EffectTargetDamageEvaluation.cs`, incoming evaluation; `R/EntrenchmentTraitRule.cs`, `R/DustScreenTraitRule.cs`; `C/HuntersMarkState.cs`. | Numeric stage evidence is an occurrence; aura presence, trail and mark retain existing state/consumption. | Hit accounting uses the actual recipient. Generic vulnerability is not implemented by renaming HuntersMark or enabling deferred ReconSquad. |
| 9: shield | `C/TraitShieldGrant.cs`, `C/CharacterTraitEffects.cs`, `A/TraitPresenceReconciler.cs`, `A/CombatDamageApplication.cs`. | Apply/absorb/reset/remove occurrences differ from eligible shield budget and reset count. | Interception may be meaningful with zero HP loss. Current grants require trait origins; future item/environment shields need a separate owner/schema decision, not fake TraitKeys or a duplicate shield store. |
| 10: knockback immunity | `R/SteadfastWillTraitRule.cs`, `A/AttackKnockbackCalculator.cs`, `Domain/Combat/KnockbackImmunityPolicy.cs`. | Prevention is an occurrence; current immunity is derived from equipped trait/category/behavior. | No movement cause or step on an immune/no-move result. Timed immunity is deferred; Pull immunity is not widened. |
| 11: Sacrifice | `A/SacrificeDamagePlanner.cs`, `R/SacrificeTraitRule.cs`, `S/SacrificeTraitStatusSnapshot.cs`. | Applied interception is an occurrence; per-enemy-turn activation counter is state. | X2 carries the actual decision. Deck timed/all-damage redirection differs from current lethal-hit, enemy-turn, priority/cap rules; do not migrate gameplay now. |
| 12, 21: Taunter/Taunted | `A/EnemyAi/TauntTargeting.cs`, `R/TauntTraitRule.cs`; current position/alive facts. | Target-selection occurrence is not a pair of stored timed statuses. | No new taunt state in the reaction payload. Pairing, expiry and compelled-action policy require the later design. |
| 13: perfect step | `R/LeapTraitRule.cs`, `A/CombatMovementCalculator.cs`. | Leap traversal occurrence differs from persistent full mine immunity. | Keep voluntary agency and current intermediate-mine exception; destination mines remain active. Deck immunity is a separate feature. |
| 22: mark | `A/HunterApplication.cs`, `S/HunterTraitStatusSnapshot.cs`, owned Hunter bonus; separately `A/HuntersMarkApplication.cs` / `C/HuntersMarkState.cs`. | Hunter lock/reward and consumed HuntersMark are distinct contracts; the deck's Focus/mark terminology does not merge them. | Preserve source/recipient identity and lifetime. Resolve naming, selection and reward policy in the follow-up design. |
| 23: execution | `A/FinishingShotApplication.cs`, `R/FinishingShotTraitRule.cs`, `C/CombatEvent.cs` damage cause. | Current strict pre-strike threshold is a decision/InstantDefeat result, not a timed recipient debuff. | No interception on InstantDefeat. Deck inclusive threshold/HP basis and presentation condition do not alter the current rule. |
| 24: healing | `A/TraitKillSettlement.cs`, `A/CharacterSettlementCalculator.cs`, `A/CombatDamageApplication.cs`; existing item decisions. | Healing occurrence plus authoritative HP, not a duration status. | Scoped hit recovery belongs to the actual recipient. No generic healing event system is needed for X. |
| 25: swapping two units | Deferred PositionSwap availability and existing placement/movement contracts. | Deck labels a swap as forced movement; it is not the implemented Pull/knockback behavior. | Neutral agency can describe future real movement, but no swap, undo or new movement algorithm is authorized. |

The slide 3 HUD/icon order and presentation lifetimes are View work. They need not be implemented or approved before X1–X3. `TraitAttackGrant`/`TraitShieldGrant` enforce trait-only origins today; this is a verified future migration concern for applied-effect state, not a reason to make every current activation generic. Existing `CharacterItemState` already demonstrates non-trait effect ownership. The proposed movement and attack-contact/interception DTOs contain no mutable duration, stack or source registry and can be reused only when a later concrete producer is approved.

### 8.9 X0 migration and acceptance boundary

Plan §10.1 is the single source for exact new member signatures, file locations and validation obligations. It names affected producers, constructors, previews, validators, copies and View readers. The changes intentionally migrate R9/R10 playback/hit semantics; source-compatible constructors do not imply behavioral compatibility.

The recommended Pull carrier is compatible with the inspected `CombatPullEventHandler → CombatUnitMotionSet → CombatUnitMotionPlayback` ownership because the existing Path projection is preserved. This is a source-based compatibility assessment only. The View owner has not accepted typed steps or implemented agency/reaction/contact consumption in this session. No message was sent to another worker. The user superseded the intermediate X0 agreement pause. Core implementation and verification of R8–R10 are recorded in plan §11.5; external presentation acceptance remains separate.


