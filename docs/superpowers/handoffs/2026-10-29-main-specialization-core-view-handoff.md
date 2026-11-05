# Main Specialization — Core and View Handoff

**Date:** 2026-10-31. **Status:** M1-M4 Core foundation complete; handoff prepared locally, not externally delivered. Core verification is qualified by the retained integration failure below. No production acquisition route or View acceptance is delivered.

**Authority:** [implementation plan](../plans/2026-10-29-main-specialization-plan.md), [accepted design §5](../specs/2026-10-29-legacy-enhancement-retirement-main-specialization-design.md#5-main-specialization-contract). Earlier phase records describe their historical stop boundaries; this handoff describes the completed foundation.

## 1. Delivered behavior and identity

An owned Main may retain exactly one additional Rider, Heavy or Cannon category per Run. Null means unspecialized. Same-class and different-class repeat requests are refused; there is no replacement/refund. Fresh Run acquisition starts unspecialized. The same owned record retains its choice across stage/battle teardown and night/recall; existing Main night policy still keeps Main on the field while Subs return to hand.

Identity, MainId, ownership/PlayerOnly eligibility, native traits, weapon shape/aiming/range/footprint, persistent Main ATK/HP/DEF, XP, growth history and Hunter contribution remain intact. A specialized Main is not a Sub. Selected-class movement, matchup row/column and current category rules apply once, including existing Cannon SafeBombardment. No Main-plus-class stacking, removed costs/shared budgets, Rider refund, Heavy protection or disabled immunity is restored. Existing one ordinary move followed by one ordinary attack, attack-first lockout and voluntary early turn end remain authoritative. Movement zero disables voluntary movement.

Main's configured ordered growth steps and IDs remain unchanged. `EffectiveTraitProgression.Resolve` combines Main and selected-class pools distinctly, using existing compatibility, availability, candidate sufficiency and Enhance rules. It grants no trait, adds no class steps and does not replay history or refill a frozen offer. An empty Main sequence stays empty.

## 2. Read contracts for consumers

| Actual contract/member | Meaning |
|---|---|
| `OwnedCharacterRecord.SpecializationCategory` | Sole persistent choice; optional Main-only Rider/Heavy/Cannon |
| `UnitSpec.CategoryKey` | Authored identity category; Main remains Main |
| `UnitSpec.SpecializationCategory`, `CombatCategoryKey` | Effective-spec projection; combat category is choice or identity |
| `UnitDefinition.Category`, `CategoryKey` | Compiled identity category |
| `UnitDefinition.CombatCategory`, `CombatCategoryKey`, `SpecializationCategory` | Existing registry category object, derived key/choice; no copied balance configuration |
| `OwnedCharacterStatsSnapshot.Role` | Ownership role; still Main |
| `OwnedCharacterStatsSnapshot.Stats.CategoryKey` | Baseline identity category; still Main |
| `OwnedCharacterStatsSnapshot.Stats.SpecializationCategory`, `.CombatCategoryKey` | Baseline choice and derived effective class |
| `CharacterCombatFacts.OwnershipRole` | Battle ownership role; still Main |
| `CharacterCombatFacts.Stats.Category.Key`, `.CombatCategoryKey` | Effective combat class |
| `CharacterCombatFacts.Stats.IdentityCategoryKey`, `.SpecializationCategory` | Explicit identity and selected class |

There are no top-level category properties added to `OwnedCharacterStatsSnapshot`: read its `Stats`. Keep `CharacterKey` for definition/appearance lookup and owned/entity IDs for instance identity. Do not infer ownership from the effective class or rewrite the authored Main SO.

View reads Core-published Buffer facts. Existing `IBufferQuery.CharacterStatsRequestSink.RequestRefresh()` and `CharacterStatsReadSink.ReadOwned()` provide the owned presentation path; handle the existing query status, including Pending, rather than calculating InGame data in View. `CharacterPresentationPublisher` computes/publishes requested stats on Core Tick. Battle display uses `CharacterReadSink` character facts and their `Stats`. Use supplied movement/stat/trait facts instead of recalculating specialization or growth in View. No specialization-only event, player input, screen or public grant method was added; this document does not authorize presentation wiring.

## 3. Core preparation and publication

Paths below use these prefixes:

- **C:** `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/`
- **S:** `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CharacterSystem/`
- **B:** `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/`

The actual chain is:

1. `C/Application/Roster/MainSpecializationRules.Plan(expected, selected)` returns `MainSpecializationDecision`, with immutable `MainSpecializationChange(Expected, Final)` on success. This pure proposal knows no offer, currency, UI or current Run. Null arguments retain explicit exception behavior. Dedicated changes permit only null-to-one-class and preserve all other owned values; generic owned/growth changes cannot grant, erase or replace the choice.
2. Internal `S/CharacterRosterService.MainSpecialization.cs` provides `PrepareMainSpecialization(RunId expectedRun, long expectedRosterRevision, CharacterInstanceId mainId, CategoryKey selected)` returning `MainSpecializationOperationPreparation`. It captures source facts read-only and validates native/completed-history compatibility and all reachable progression paths.
3. Internal `ApplyMainSpecialization(MainSpecializationOperationPreparation prepared)` rechecks service ownership, single use, original sources, DB values and readiness. Failed preparation returns its refusal without installation. It rechecks the original Core source after Buffer preparation so a newly captured Buffer source cannot legitimize an old decision.
4. Internal `IBufferQuery.MainSpecializationOperations` exposes `IMainSpecializationTransaction.Prepare(expectedRun, expectedRosterRevision, change)` / `Install(prepared)`, implemented by `B/Core/MainSpecializationTransactions.cs`. Existing `BufferSystemGroup` composes it with the existing character store; there is no additional root service/Job/lifecycle.
5. Internal `ICharacterBufferStore.PrepareMainSpecialization(change)` / `InstallMainSpecialization(prepared)` in `B/Character/CharacterBuffer.cs` validates shape, exact expected record and roster revision, then replaces one existing owned record. Owner, stale/reused tokens and overflow are checked before writes. The existing roster revision advances exactly once.

Standalone preparation/application requires an in-progress nonempty Run, the selected living owned Main, no active or unsettled battle and existing growth `ReadyToContinue`. An unlocked unresolved step without an offer, active frozen offer or unacknowledged result blocks application. There is no allow-pending flag, auto-acknowledgment or step skipping.

Source validation includes Run/outcome, roster revision, selected deck membership/revision, Main persistent health/state, battle query revision **and BattleInstanceId** (including entered-then-ended battle), growth offer/result/handled/acknowledged state, and immutable DB inputs. Equivalent fresh DB conversions compare by value. Success changes only specialization and roster revision; deck, health, XP/history/Hunter, souls, other owned records, currency, shop and growth receipts/counters are preserved. Tokens are ephemeral evidence, not saved state or long-lived UI commands.

### Failure contract

`MainSpecializationDecision` exposes `Succeeded`, `Change` and `FailureReason`; None is success only and failed decisions carry no change.

| FailureReason | Actual boundary/meaning |
|---|---|
| `None` | Valid success change |
| `InvalidTarget` | Pure Plan rejects Sub; service also rejects nonselected/missing/dead Main |
| `InvalidCategory` | Pure Plan rejects choices outside Rider/Heavy/Cannon |
| `AlreadySpecialized` | Fresh request for any class after a choice |
| `SourceChanged` | Expected Run/revision mismatch, stale/foreign/reused service preparation, changed source/DB; service maps Buffer prepare overflow/refusal here |
| `RunUnavailable` | Empty or ended Run at initial preparation |
| `InBattle` | Active/unsettled battle at initial preparation |
| `GrowthPending` | Existing growth query is not ReadyToContinue |
| `InvalidDefinition` | Native profile/definition validation refusal |
| `UnsupportedProgression` | Requested combination/history/progression validation refusal |

Failure precedence follows the actual service checks; changed state after a successful preparation normally reports SourceChanged rather than recomputing initial eligibility. Invalid direct Buffer requests/installations throw using existing exception conventions. Null/unbound dependencies and required null configuration remain exceptions, not successful no-ops. Buffer does not query DB or calculate growth readiness/progression; Core owns those rules. Calling the lower typed store alone does not establish the full Core eligibility contract.

## 4. Future acquisition adapters — not implemented

The reusable building blocks are pure Plan plus typed owned preparation **inside the acquiring owner's atomic publication**, with equivalent source/content validation. The internal standalone apply boundary is exercised by integration fixtures and is not a public UI command or a purchase transaction.

- A future level-up class choice must validate/consume its exact matching offer and publish its result atomically with the owned change. It must define its own accepted class-choice contract. Existing `TraitGrowthSelection` remains `StepId/TraitKey`; no class choice kind was added. Do not call standalone apply while an offer is pending or apply then consume separately.
- A future enhancement-shop adapter must validate its frozen offer and combine payment, class change and purchase receipt in the existing atomic shop transaction. Do not apply the class first and spend afterward. `EnhancementKey.ChangeRider = 2` remains reserved; this foundation adds no product/price/visit/stage subtype or purchase route.

How a class is earned, its price/unlock level, production pools, placement and UI surface need a separate bounded integration design/authorization. No dedicated screen or new stage subtype is assumed. Production Main content need not be activated to complete this fixture-verified Core foundation.

## 5. Verification and source identity

**Source checkpoint:** HEAD `3a05140fdf60f9cac8b2867988063eb280306b3c` on `feature/Cheon/Core/NewCombatSytem`, plus uncommitted M1-M3 and separately owned test-input-isolation work. No delivered commit is claimed. Exact phase evidence is under `.superpowers/sdd/2026-10-29-main-specialization-plan/`:

| Phase | Source package / SHA-256 |
|---|---|
| M1 | `m1-source-manifest.json`: `32E2601D58288253698BB86C74466EFE0F4C84035400F1073CCDEDA8A1B5C294` (historical M1 checkpoint) |
| M2 | `m2-source-manifest.json`: `81CAD1E3845ECE7430B0D42796D4F6ABFB700983E312DC9409B25CAF652E2FB5` |
| M3 | `m3-baseline/m3-manifest.sha256`: `93A6A67400681A1BEC1CD156FA7A18A104A9EBFA4F92DC7945265E71BE7FCE71` |

At M4, all 14 M3 source/meta hashes matched. All 42 M2 entries matched current files or, for the one intentionally changed service file, its exact M3 starting copy. No C# file under project scripts/tests had a write time later than the final Core XML. M4 changes documents only; unchanged final runtime evidence is reused as the plan requires. No Unity tests were newly launched in M4. Source hashes are phase-specific, not a claim that earlier snapshots contain later implementation.

Native evidence uses Unity `6000.0.73f1`, sequential instances and the approved AppData-enabled runner. XML/log paths are `.superpowers/trait-system/<Run>.xml` / `.log`. Counts below were independently read from XML during M4; all listed runs have zero skips.

| Phase / final Run | Passed / Total | Failed |
|---|---:|---:|
| M1 focused `mainclass-m1-green-verified` | 138/138 | 0 |
| M1 Core `mainclass-m1-final-core-verified` | 4398/4398 | 0 |
| M1 integration `mainclass-m1-final-integration` | 477/484 | 7 |
| M2 focused `mainclass-m2-validation-boundary-green` | 37/37 | 0 |
| M2 integration focused `mainclass-m2-integration-green-verified` | 38/38 | 0 |
| M2 Core `mainclass-m2-final-core-verified` | 4425/4425 | 0 |
| M2 integration `mainclass-m2-final-integration` | 506/507 | 1 |
| M3 focused `mainclass-m3-focused-final-08` | 68/68 | 0 |
| M3 Core `mainclass-m3-full-core-09` | 4425/4425 | 0 |
| M3 integration `mainclass-m3-full-integration-10` | 574/575 | 1 |

The [plan execution records](../plans/2026-10-29-main-specialization-plan.md#6-execution-record) retain exact RED/GREEN runs, intermediate fixture corrections, test preservation and baseline differences for each phase. M1 added 39 Core and 7 integration cases. M2 added 27 Core and 15 integration cases; concurrent input-isolation changes are separately accounted for. M3 added 68 integration cases, removed none, and left the Core case set unchanged. M4 rechecked M2/M3 case-set differences and the exact remaining failure. Final M3 Core and integration logs contain zero C# compiler errors.

**Remaining baseline failure (unchanged from M2):**

`ProjectL.Tests.CoreIntegration.CombatCameraZoomPoseTests.PostProcessingExtensionIsAllowedButPoseWriterIsRejected("Cinemachine.CinemachineCameraOffset",False)`

`System.TypeLoadException : Could not load type 'Cinemachine.CinemachineCameraOffset' from assembly 'Cinemachine, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.`

The full integration suite is not wholly green. Earlier boss/authoring failures were repaired in the separate test-input-isolation work, not by specialization. M4 does not change the camera, tests or assets to hide the failure. Core/CoreIntegration evidence does not establish full Framework/View, PlayMode, scenes, visuals or production/manual acceptance.

## 6. Architecture, locality and maintenance

The owned record is the only persistent specialization authority. `OwnedUnitSpecResolver` derives effective specs; `CategoryRegistry` owns configured category objects; `EffectiveTraitProgression` owns pool composition. Main stat growth remains with existing persistent-state/XP owners. CharacterBuffer owns mutation, while Core owns eligibility/content rules. Framework service creation/export/lookup and lifetimes are retained; the feature partial belongs to the existing roster service. FW-001/002/003/016 gain no extra registrations or lifecycles, and FW-004 retains the internal Buffer write boundary.

SRP separates pure selection, effective projections, context validation and publication. Existing constructors preserve unspecialized behavior (LSP); narrow internal preparation contracts limit dependencies (ISP/DIP); eligible Main definitions do not require an entity-key allowlist (OCP). There is no second roster, XP engine, offer history, mutable category configuration, service cache or Job infrastructure.

| Real future change | Modification and understanding boundary | Required verification |
|---|---|---|
| Tune Rider movement | Existing category configuration and effective projection contract; no shop or unrelated trait internals | Owned/battle movement, preview/execution and allowance parity |
| Change class trait access | EffectiveTraitProgression and existing growth/validator contracts | History, candidate sufficiency, frozen offer/receipt/XP regressions |
| Change application guards | Existing roster feature partial, DB snapshot comparison and Buffer source/publication contracts | Stale source, direct store, no partial mutation, next battle |
| Expose an acquisition route | Its transaction adapter plus shared transition/store/content-validation contracts | Exact offer/payment/receipt atomicity and shared Core regressions |

The expected earlier scope included shop kinds/prices/content and presentation; the observed M1-M3 implementation changes the owned/combat/growth and existing Core/Buffer owners only. Normal composition changes remain valid. Shared state contracts still require full Core/CoreIntegration regression coverage; locality does not eliminate these dependencies. The global growth-readiness query genuinely reads all-roster definitions.

**Known maintenance limitation:** `MainSpecializationDefinitionSnapshot` explicitly compares authored DB values because TraitSpec lacks complete semantic equality and runtime snapshots omit fields. Adding a trait type/payload requires updating this comparison; concrete-type/constructor-payload tests detect omissions. Descriptions and unselected-class payloads are conservatively included, so harmless DB edits may invalidate a preparation. This is retained source-validation conservatism, not an alternate state owner. No runtime reflection comparison engine was added.

The existing direct pure Prepare overload may omit AttackProfiles for partial fixtures; it cannot establish the full weapon/progression compatibility claim without profiles. The real DB source supplies them. The integrated preparation/application and following battle tests are the capability evidence.

## 7. Delivery state and next owner

| Item | State |
|---|---|
| M1-M3 Core foundation | Verified against the stated baseline; integration caveat retained |
| M4 handoff and plan record | Prepared locally |
| External delivery | Not sent |
| Commit/push | Not performed |
| Production acquisition/price/unlock level/pools | Deferred integration/content decisions |
| View/Flow display/input wiring | Not implemented by this plan |
| PlayMode/manual/visual/production acceptance | Not claimed; remains with later integration owners |

A later owner should first define the acquisition contract and atomic publication with Core, then connect presentation through existing Buffer/UI/input boundaries. This handoff supplies facts and constraints; it does not authorize those later changes. No activation phase remains in the Core-only plan.
