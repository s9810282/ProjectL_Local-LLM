# Main Specialization Core Foundation Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans only after the user approves the design and authorizes a phase. Preserve phase review stops, Core ownership and no-commit/no-push policy. This document does not authorize implementation.

**Goal:** Implement the Core capability for a Main to retain its identity and growth while holding one Rider/Heavy/Cannon specialization per Run, stopping before any shop or level-up acquisition integration.

**Architecture:** One optional specialization on the existing immutable owned record; derive effective category and progression through existing composition. Reuse Core roster preparation and checked Buffer installation, with no production acquisition caller. Future acquisition adapters can compose the pure transition and typed store preparation inside their existing transaction. No general multi-category/reward framework, second Buffer or alternate stat/XP engine.

**Tech Stack:** Unity 6000.0.73f1; C#, existing SO/DB/Core/Buffer contracts; Core and CoreIntegration tests.

**Spec:** [Design](../specs/2026-10-29-legacy-enhancement-retirement-main-specialization-design.md), especially §5. [Retirement plan](2026-10-29-legacy-enhancement-retirement-plan.md) is independently deliverable.

**Status (2026-10-31):** Phases M1-M4 are complete for the Core-only foundation. The local [Core/View handoff](../handoffs/2026-10-29-main-specialization-core-view-handoff.md) records actual contracts, source/evidence and future acquisition boundaries. Verified unchanged M3 evidence: Core 4,425/4,425; CoreIntegration 574/575 with the retained camera type-load failure. Acquisition, production content, View wiring and manual acceptance remain deferred; external delivery, commit and push were not performed.

**Policy alignment:** Design §4.5 records combat-cost removal. Use selected-class maximum movement distance and existing one-move/one-ordinary-attack rules, with voluntary end-turn; do not reintroduce combat costs. The 2026-10-31 scope supersedes the earlier monetary purchase/activation tasks. Re-read accepted basic-undo and buff/debuff contracts at implementation; do not repeat or change those systems here.

## Global Constraints

- Eligibility is Main ownership + valid Main definition/category, not an Akimbo/Sniper entity-key switch. Preserve Id, CharacterKey, MainId, Role, PlayerOnly eligibility, native traits, persistent ATK/HP/DEF, XP and all existing selection history/Hunter bonus.
- Select exactly one Rider/Heavy/Cannon; null means unspecialized. No simultaneous three-class membership, stat/movement/matchup stacking or within-Run replacement/refund. Fresh Run starts unspecialized; same-Run stage/night/recall preserve the choice.
- Apply only currently supported category behavior, including Cannon SafeBombardment. Do not revive removed Rider refunds/Heavy protection or currently disabled category immunity. Do not restore the old ChangeRider MoveCost=2/AttackCost=4 override or any combat pricing/budget. Maximum movement distance comes from the selected class; ordinary action counts and voluntary end-turn follow design §4.5.
- ChangeRider retains EnhancementKey=2; specialization is not a TraitKey or new attack effect. Old seven products and legacy Sub shop handling remain retired/rejected.
- Main step IDs/order remain authoritative. Class access does not grant all class traits, import Sub stat growth or silently append class progression steps. Pools remain SO-configured.
- Reuse existing Main growth, owned composition, expected/final Buffer validation and Framework registration/lifetimes. Preserve SSOT, SOLID, FW-001/002/003/004/016; no new service registration, Job, input Buffer, scene or UI dependency for this foundation.
- Keep preparation/application internal to Core/Buffer. No public free-grant API or production input route. Pure specialization rules do not know shop payment, growth offers or UI. Standalone application requires growth ReadyToContinue; a future acquisition owner must consume its own offer/payment in its own atomic publication, not call apply and consume afterward.
- Exclude shop products/visit kinds/stage details/prices/purchase changes, level-up class-choice integration, dedicated specialization UI and production content activation. Preserve existing ordinary shops and retired enhancement rejection. Explicit fixture data is sufficient for Core completion; production prices/levels/pools are not prerequisites.
- Documents English; implementation comments/XML Korean; paired .meta, UTF-8/LF and existing naming conventions. No commits/push, destructive cleanup or edits to another owner's live phase.

## Review Focus

1. A specialized Main still passes identity/deck/health/XP rules and does not become a Sub: M1/M2 role, identity and Main persistent-growth tests.
2. Owned empty-growth fast paths must not discard specialization; unrelated XP/Hunter/growth changes must preserve it and cannot grant it: M1/M2 reconstruction/typed-change tests.
3. Pool union must preserve old history and three-choice sufficiency without replaying steps or altering frozen offers: M2 graph and readiness tests.
4. Stale Run/roster/deck/health, active battle, pending growth and repeated application must never install a partial record or mutate unrelated stores: M3 service plus direct prepare/install tests.
5. Owned read, prepared battle and later battle must agree on identity/effective class; future callers must not need a shop to use the Core contract or an unsafe second write to consume an acquisition: M2/M3 integration tests and M4 handoff. Existing public constructors retain behavior.

## 1. Ordering and execution evidence

Use C/S/B/D/A/T/TI path prefixes from the design. The original dependency order was retirement R0-R3, cost-removal R4, basic movement undo and buff/debuff migration before specialization. At implementation, establish their current accepted source checkpoints and do not rerun completed phases from historical documentation. The focused document review used HEAD `2bd95ad342f5075ecbd31714bde8f2ad756af0ab`; it is not a runtime verification checkpoint. F1 of the prior trait plan is not blocked by this future feature.

| Phase | Deliverable |
|---|---|
| M1 | Pure owned capability and effective identity/combat-category contracts |
| M2 | Native owned/battle/stat and trait-growth integration |
| M3 | Reusable Core preparation/application and checked owned-state publication |
| M4 | Final Core evidence and handoff; no acquisition or content activation |

Every phase requires explicit user authorization, focused behavioral RED/GREEN, appropriate existing regressions and a stop/report. Use the approved AppData-enabled Unity route and existing runner sequentially, with fresh names such as `mainclass-m1-red`, `mainclass-m2-green`, `mainclass-final-core` and `mainclass-final-integration`. Exact assemblies are ProjectL.Tests.Core and ProjectL.Tests.CoreIntegration. Final evidence must include XML cases/skips/failure fullnames, source identity and baseline differences. No Unity runs or manual acceptance are claimed here.

Existing local runner commands (execute only during an authorized implementation phase, through the AppData-enabled approval route):

```powershell
& ./.superpowers/trait-system/run-tests.ps1 -Run mainclass-m1-red -Assembly ProjectL.Tests.Core -Filter ProjectL.Tests.Core.MainSpecializationContractTests
& ./.superpowers/trait-system/run-tests.ps1 -Run mainclass-m1-green -Assembly ProjectL.Tests.Core -Filter ProjectL.Tests.Core.MainSpecializationContractTests
& ./.superpowers/trait-system/run-tests.ps1 -Run mainclass-final-core -Assembly ProjectL.Tests.Core
& ./.superpowers/trait-system/run-tests.ps1 -Run mainclass-final-integration -Assembly ProjectL.Tests.CoreIntegration
```

Use each task's named test class as the focused filter, with unique M2/M3 red/green run names. Before first use, verify the local runner/editor path and actual fixture fullnames. RED must fail for the target behavior; GREEN must pass with nonzero selected cases and no unexplained skips. Full gates require no new failures against a fresh baseline, with any pre-existing failures reported by fullname and cause; never erase or relabel tests to preserve a count. Do not launch concurrent Unity instances or delete caches to address the known sandbox/AppData issue.

## 2. Phase M1 — owned capability and category identity

### M1.1 Add a narrow immutable specialization transition

**Create:** `C/Application/Roster/MainSpecializationRules.cs`; `C/Contracts/Roster/MainSpecializationChange.cs`, `MainSpecializationDecision.cs`, `MainSpecializationFailureReason.cs`; `T/MainSpecializationContractTests.cs`. Add paired metas.

**Modify:** `C/Contracts/Roster/{OwnedCharacterRecord,OwnedCharacterChange}.cs`; `C/Contracts/Enhancements/TraitGrowthChange.cs`; reconstruction in `C/Application/Roster/{ExperienceRules,TraitGrowthRules}.cs`; `B/Character/CharacterBuffer.cs` and new `B/Character/PreparedMainSpecializationChange.cs` only for typed prepare/install. Extend copy/equality/With/shape guards where these facts travel.

**Interfaces:** OwnedCharacterRecord gains optional trailing `CategoryKey? specializationCategory = null` and get-only SpecializationCategory. MainSpecializationRules.ResolveCombatCategory(identity, specialization) returns identity when null, selected class only for identity Main otherwise rejects. Plan(expected, selected) returns MainSpecializationDecision with an immutable change or a typed failure. MainSpecializationChange(Expected, Final) allows only null -> Rider/Heavy/Cannon while preserving every other owned field; OwnedCharacterChange and TraitGrowthChange must preserve specialization. CharacterBuffer validates expected record/revision before typed installation; no public setter or new state store.

`MainSpecializationDecision` exposes Succeeded, Change and FailureReason. Use failure reasons None, InvalidTarget, InvalidCategory, AlreadySpecialized, SourceChanged, RunUnavailable, InBattle, GrowthPending, InvalidDefinition and UnsupportedProgression; None is success only. Pure Plan owns target/category/repeat failures; M3 owns current-context/content failures. Add internal `PrepareMainSpecialization(MainSpecializationChange change) -> PreparedMainSpecializationChange` and `InstallMainSpecialization(PreparedMainSpecializationChange prepared)` to ICharacterBufferStore. Store preparation validates exact expected record, allowed delta and revision overflow before any write. Installation replaces that record and increments the existing roster revision once; stale/foreign/reused preparations reject without mutation. Invalid direct store requests follow existing Buffer exception conventions. Context coordination is M3, not a new state store in M1.

- [x] Add `MainAcceptsEachSpecialization`, `SubAndInvalidClassesReject`, `RepeatedChoiceDoesNotChangeRecord`, `ChangePreservesIdentityGrowthXpAndHunter`, `GenericChangesCannotGrantOrEraseSpecialization`, `SnapshotCopyAndEqualityIncludeSpecialization`, and `FreshRunIsUnspecialized`. Use all three categories and two valid synthetic Main definitions; retain the real Main policy.
- [x] Run focused RED for behavior before adding fields/rules. Implement the minimal typed transition, trailing compatibility constructors and independent validation. No production offer or shop access yet.
- [x] Update all actual record reconstruction paths; prove XP/growth/Hunter updates preserve a chosen class. Test direct stale expected/revision preparation and invalid cross-role records reject before installation.
- [x] Run focused GREEN with owned same-values, XP, growth, new-Run and contract guards. Continue only to M1.2 within the same authorized phase; this is an internal capability, not an available specialization feature.

### M1.2 Define identity versus effective combat category without weakening Main policy

**Modify:** `C/Contracts/Commands/UnitSpec.cs`; `C/Domain/Entities/UnitDefinition.cs`; `C/Application/BootstrapModels/UnitCatalog.cs`; leave `PlayerMainDefinitionPolicy.Validate` identity requirements intact. Test `T/MainSpecializationContractTests.cs`.

**Interfaces:** effective UnitSpec adds optional trailing specializationCategory and derived CombatCategoryKey; CategoryKey remains identity. UnitDefinition retains Category/CategoryKey identity and additionally references CombatCategory / derived CombatCategoryKey resolved through existing CategoryRegistry. Legacy null behavior resolves the same category. MoveRange uses the effective class once; Main level-growth source and ordinary action counts do not change. MoveCost/AttackCost and shared budgets removed by R4 are not recreated. Do not accept specialization on a catalog-authored Sub or unowned enemy profile.

- [x] Add legacy constructor parity, Main identity policy preservation, invalid effective-category and selected-Cannon-distance cases. Vary fixture movement limits from defaults to prove real config propagation, including zero distance disabling voluntary movement rather than granting unlimited movement. Verify specialization grants no extra ordinary movement/attack action and cannot restore costs.
- [x] Run focused RED/GREEN and existing category/profile/ownership tests. Ensure compilation and copy projections do not lose the optional field. Stop at the M1 phase checkpoint after both tasks.

## 3. Phase M2 — effective stats, behavior and trait growth

### M2.1 Integrate owned/battle projection and current category rules

**Modify:** `C/Application/Enhancements/OwnedUnitSpecResolver.cs`; `C/Application/Roster/{CharacterBaselineStatsCalculator,OwnedCharacterStatsCalculator}.cs`; `C/Application/Combat/{BattleCharacterCalculator,CombatCommandEffects,MovementContextCalculator}.cs`; category snapshot/runtime creation; `C/Contracts/Snapshots/{OwnedCharacterStatsSnapshot,CharacterBaselineStatsSnapshot,UnitStatsSnapshot}.cs` only as required to expose explicit identity/specialization; corresponding Buffer equality/preparation/copy guards. Preserve existing trait-definition ownership.

**Test:** new `T/MainSpecializationCombatTests.cs`, `TI/MainSpecializationBattleTests.cs`; existing Main/deck, configured movement, action use, voluntary end-turn, damage, SafeBombardment, Hunter persistence and preview suites.

**Interfaces:** remove the resolver's empty-Growth bypass when specialization exists. Identity stays Main in definition/baseline identity fields; expose SpecializationCategory/CombatCategoryKey as Core-derived read facts. Combat Stats.Category and category runtime bindings use the effective class, so existing movement/matchup/AI/preview code consumes one consistent class. In CombatCommandEffects.Capture compare character.Stats.Category.Key with profile.Definition.CombatCategoryKey, not its identity CategoryKey; keep PlayerMainDefinitionPolicy identity validation unchanged. CharacterCombatFacts uses OwnershipRole for Main ownership. Preserve role-based Main behavior and persistent stat ownership. No SO mutation or post-install repair.

- [x] Add `EmptyGrowthStillAppliesClass`, `MainStatsAndNativeWeaponStayIntact`, `OwnedReadAndBattleUseSameClass`, `SpecializedMainCaptureAndAttackUseEffectiveCategory`, `MovementPreviewExecutionAndAllowanceAgree`, `SpecializationPreservesOneMoveOneAttackAndEarlyEndTurn`, `AttackerAndDefenderMatchupsUseSelectedClassOnce`, `CannonUsesExistingSafeBombardment`, and `StageNightRecallKeepClassNewRunClearsIt`.
- [x] Run RED through real configured battle/owned read paths, not detached scalar helpers. Use distinctive fixture category values and nonneutral matchup entries. Preserve ATK/HP/DEF/XP/MainId, weapon shape/range and Hunter bonus while movement-distance/class facts change as specified. Retain move-then-attack, attack-first lockout, rejected-action and unused-action end-turn coverage without affordability assumptions.
- [x] Apply effective category at existing compilation/install owners. Audit class-based knockback and rules against the current policy; do not restore disabled immunity. Update stale-context comparison, snapshot With/SameValues/copies and Buffer preparation together.
- [x] Run GREEN with Main/Sub growth, source replacement, configured movement, attack admission, category damage and immutable contract regressions. No change for an unspecialized Main, ordinary Sub/enemy/summon, or shared definition used by another record.

### M2.2 Extend Main access through the existing progression owner

**Create:** `C/Application/Roster/EffectiveTraitProgression.cs`; `T/MainSpecializationGrowthTests.cs`; `TI/MainSpecializationGrowthPublicationTests.cs`.

**Modify:** `C/Application/Roster/{TraitGrowthRules,TraitGrowthCatalogValidator}.cs`; `C/Application/Enhancements/OwnedUnitSpecResolver.cs`; actual Core trait-growth preparation/service call sites to consume the same effective progression. No separate selection history/step stream/offer queue.

**Interfaces:** `EffectiveTraitProgression.Resolve(UnitSpec basis, OwnedCharacterRecord owned, TraitGrowthCatalog catalog) -> CategoryTraitProgressionSpec` returns existing category progression for ordinary records and a Main progression with unchanged Main Steps plus distinct Main.Pool union chosen-class.Pool for specialized Main. Existing compatibility/availability filtering and stable ordering remain authoritative. It does not append class steps or grant traits.

- [x] Add `MainAndChosenPoolUnionIsDistinct`, `OtherClassPoolIsExcluded`, `NoAutoGrantOrReplayedStep`, `OldMainHistoryReconstructsAfterChoice`, `MainStepsAndPersistentGrowthAreUnchanged`, `EachReachableAcquireHasThreeChoices`, and `EnhanceStillTargetsOwnedEligibleTraits`. Use already-specialized fixture records for this phase; applying a class while growth is pending is tested at M3's application boundary.
- [x] Run RED with overlapping pools, native overlaps, invalid attack combinations, retired/deferred candidates, empty Main steps and same-level ordered steps. A selected class must not make an invalid reachable path disappear through reroll/fallback.
- [x] Reuse effective progression for candidate selection, history reconstruction and DB/native validation of each supported specialization/Main combination. In `S/RuntimeSystem/InGameSystem/CoreSystem/CharacterSystem/CharacterRosterService.cs`, update ReadTraitGrowthProgressCore, PrepareNextTraitGrowthOffer, PrepareTraitGrowthChoice and FindNext to use that same resolver. Keep frozen offers/receipts exact and publish no new growth action merely because pool access changed. Validate the requested class before M3 can apply it; do not require unrelated production combinations or new assets to be activated.
- [x] Run GREEN through real Buffer offer/choice/acknowledgment and XP/Hunter settlement paths. Run full Core/CoreIntegration gates sequentially and stop/report M2. The existing trait-choice route stays trait-only; class acquisition integration is outside the plan.

## 4. Phase M3 — reusable Core preparation and application

### M3.1 Prepare the class change without an acquisition route

**Create:** `S/RuntimeSystem/InGameSystem/CoreSystem/CharacterSystem/MainSpecializationOperationPreparation.cs`; `TI/MainSpecializationPreparationTests.cs`.

**Modify:** `S/RuntimeSystem/InGameSystem/CoreSystem/CharacterSystem/CharacterRosterService.cs`. Reuse its bound DB/Buffer inputs, Main/profile adapters, M1 pure rules and M2 effective progression; no new exported service or parallel catalog cache.

**Interfaces:** internal `PrepareMainSpecialization(RunId expectedRun, long expectedRosterRevision, CharacterInstanceId mainId, CategoryKey selected) -> MainSpecializationOperationPreparation`. The preparation exposes its MainSpecializationDecision and captured Run/roster/deck/selected-Main health, growth readiness and relevant immutable DB inputs. It is bound to the creating service and does not mutate any store. Use existing snapshot/revision contracts; do not add new mutable authority for deck, health or progression. Compare DB values rather than fresh conversion object references. Failed preparation has a typed failure and no installable change.

- [x] Add `PrepareEachClassDoesNotMutate`, `RejectSubNonselectedAndDeadMain`, `RejectEmptyOrEndedRunAndActiveBattle`, `RejectStaleRunAndRoster`, `RejectInvalidProfileOrProgression`, `RepeatedClassReturnsAlreadySpecialized`, and `PureTransitionDoesNotDependOnShopOrGrowthOffer`. Assert the actual existing stores, revisions and result counters stay unchanged during success and failure preparation. Run focused RED.
- [x] Add `PendingStepWithoutOfferBlocksStandaloneApplication`, `PendingOfferBlocksStandaloneApplication`, and `UnacknowledgedGrowthResultBlocksStandaloneApplication`. Use the real growth progress query and require ReadyToContinue; no offer cancellation, step skipping or auto-acknowledgment. Keep this readiness gate outside MainSpecializationRules and the typed character store.
- [x] Implement internal preparation: validate current Run and selected living Main, capture state, invoke M1 Plan, resolve/validate the chosen class's native profile and effective progression, then recheck captured source facts. Invalid caller/domain state returns the M1 typed failure reasons; null or missing required configuration follows existing exception contracts.
- [x] Run focused GREEN with all three classes, two synthetic eligible Main definitions and distinctive configured category values. An empty Main step sequence remains valid/empty; no production pool edits or shop details are required.

### M3.2 Apply through the existing Buffer ownership and prove next-battle behavior

**Create:** `B/Core/MainSpecializationTransactions.cs` containing the internal transaction interface/implementation and ephemeral `PreparedMainSpecializationApplication`; `TI/MainSpecializationPublicationTests.cs`.

**Modify:** `B/Core/BufferSystemGroup.cs` for internal composition; `S/RuntimeSystem/InGameSystem/CoreSystem/CharacterSystem/CharacterRosterService.cs` for application. Reuse M1's `B/Character/PreparedMainSpecializationChange.cs` and store operations. Do not modify ShopService, ShopTransactions, CurrentShop, growth-choice input/result contracts or View/Flow.

**Interfaces:** `IBufferQuery.MainSpecializationOperations` provides internal `IMainSpecializationTransaction.Prepare(RunId expectedRun, long expectedRosterRevision, MainSpecializationChange change) -> PreparedMainSpecializationApplication` and `Install(PreparedMainSpecializationApplication prepared) -> void`. BufferSystemGroup constructs it with existing Run/combat/deck/character/growth read/store dependencies; no separate root service registration. Prepare captures the current selected Main/deck/health/growth source and prepares the typed owned delta. Install validates owner, single use, captured sources, no active/unsettled battle, no frozen offer/unacknowledged growth result and unchanged roster revision before any write. Buffer validates state/shape; it does not calculate progression or query the DB.

Internal `CharacterRosterService.ApplyMainSpecialization(MainSpecializationOperationPreparation prepared) -> MainSpecializationDecision` rechecks the M3.1 service ownership, state snapshots, ReadyToContinue and relevant DB values, then uses the Buffer Prepare/Install pair. Recheck the original M3.1 source after Buffer Prepare and before Install; capturing a new Buffer source must not legitimize a stale Core decision. A failed Core preparation returns its failure without installation. Stale source returns SourceChanged with no mutation; a fresh repeated request returns AlreadySpecialized. Invalid direct Buffer calls throw before mutation. No arbitrary public category setter, production command, event bus or new Job is exposed. The standalone operation is not an acquisition opportunity; it is a Core integration boundary exercised by fixtures.

- [x] Add `ApplyEachClassUpdatesOnlyOwnedClassAndRevision` and `AppliedClassIsUsedByNextBattle`. Use the composed service and real Buffer store, then enter a battle through the normal preparation path. Assert Main identity/health/XP/history/Hunter/native weapon remain equal, only the owned class changes, roster revision increases by exactly one, and owned/battle effective category and movement agree.
- [x] Add `RunRosterDeckHealthOrGrowthChangeBetweenPrepareAndApplyRejects`, `RelevantDefinitionChangeRejectsButEquivalentCopiesDoNot`, `BattleEntryAfterPreparationRejects`, `DirectBufferPreparationCannotBypassTargetAndSourceGuards`, `ForeignOrReusedPreparationRejects`, and `RevisionOverflowDoesNotMutate`. Check both service preparation/application and Buffer preparation/install boundaries. On rejection, every store remains at its state immediately before the rejected operation, including legitimate intervening changes.
- [x] Add same-class and different-class repeat cases, plus success/rejection assertions that currency, shop visit, frozen growth offer/history and all result/acknowledgment counters remain unchanged. Do not create purchase or growth command receipts to make the tests pass. Run focused RED.
- [x] Implement the narrow coordinator at the existing composition boundary. Complete every possible failure check before installing the M1 delta; derive read facts through existing projections. Reuse pure Plan and store preparation rather than replicating field validation in a future shop/UI owner. Do not add an allow-pending-growth flag.
- [x] Run focused GREEN, existing Main/deck/growth/publication, ordinary shop and retired-enhancement regressions, then full Core/CoreIntegration gates sequentially. Stop/report M3 with explicit evidence that no screen, stage/product asset or acquisition command was needed. Future adapters must compose typed owned preparation with their own payment/offer consumption; they must not call standalone apply and consume afterward.

## 5. Phase M4 — Core verification and handoff

### M4.1 Record the completed foundation and future adapter boundary

**Create:** `docs/superpowers/handoffs/2026-10-29-main-specialization-core-view-handoff.md` during authorized M4 execution. Update this plan's execution record. No new runtime, View/Flow/UI/scene or production asset work.

- [x] Record M1-M3 exact source, focused/full evidence and remaining baseline failures. Check SSOT, SOLID and FW boundaries using the actual owned-state, category/progression and Core/Buffer call chain. Reuse still-current final evidence; rerun only if code changed or an unresolved concern requires it.
- [x] Document identity/specialization/effective-category read fields, once-per-Run behavior, preparation/application preconditions and typed failures. Identify pure Plan and typed store preparation as the reusable composition contract; internal standalone apply is not a public UI command or future purchase transaction.
- [x] Explain the two deferred acquisition examples without implementing either: a level-up selection must consume its matching choice and publish its result atomically; an enhancement-shop purchase must validate its frozen offer and combine payment, class change and receipt. No dedicated UI, new stage subtype or actual acquisition rule is presumed. Existing TraitGrowthSelection remains StepId/TraitKey until a separate integration design is accepted.
- [x] Record Core verified, handoff prepared and external delivery separately. State that production acquisition, prices/levels/pools, display wiring and manual acceptance are not delivered and are not blockers to this foundation's completion. Stop/report M4; no external message, commit or push.

There is **no M4.2 activation task** in the revised plan. Any later acquisition/content/UI work needs its own scope and authorization. Core tests with explicit fixtures do not establish player-facing production availability.

## 6. Execution record

Documentation created 2026-10-29. All tasks unchecked. Self-review checked design coverage, phase interfaces, stable identity/effective class semantics, review-focus test ownership, retirement preservation and content/owner gates. Fresh design builder-check corrections are incorporated. Append authorized phase, concrete source/checkpoints, RED/GREEN/full evidence, actual locality/SSOT/SOLID/FW review and external ownership states during later execution.

2026-10-29 documentation revision: replaced planned combat pricing integration/tests with category movement-distance and action-use guarantees after R4; preserved monetary shop prices and purchase atomicity. This revision is documentation only, with no new builder-agent review, runtime implementation or Unity execution.

2026-10-31 documentation revision: user reduced the deliverable to Core functionality before shop integration and explicitly requested no implementation. Retained M1/M2 owned/category/combat/progression coverage; replaced M3 products/purchases with read-only Core preparation and checked application through the existing Buffer owner; reduced M4 to verification/handoff and removed content activation. Kept future acquisition consumption separate from pure specialization rules and the standalone growth-readiness gate. All implementation tasks remain unchecked. No runtime/test/asset changes, Unity execution, commit, push or external delivery occurred as part of this revision.

The revised design received a fresh read-only builder check with no material unanswered questions. Plan self-review checked spec coverage, phase signatures, failure/readiness ownership, source capture across both preparation boundaries, named verification cases and the Core-only completion boundary. These are document checks, not executable evidence.
### 2026-10-31 — Phase M1 execution checkpoint

The user explicitly authorized **M1 only**, after the documentation-only scope revision. M1.1 and M1.2 are complete. M2-M4 remain unchecked and require separate authorization. No acquisition route, public grant/setter, shop/level-up integration, production asset activation, scene, UI, commit or push was added.

**Source:** HEAD `3a05140fdf60f9cac8b2867988063eb280306b3c` on `feature/Cheon/Core/NewCombatSytem`, plus the M1 working changes. Exact source/test/meta SHA-256 values are captured in `.superpowers/sdd/2026-10-29-main-specialization-plan/m1-source-manifest.json` (manifest SHA-256 `32E2601D58288253698BB86C74466EFE0F4C84035400F1073CCDEDA8A1B5C294`). Existing pending design/plan revisions were preserved.

**Implemented contract:** `OwnedCharacterRecord.SpecializationCategory` is optional, immutable and Main-only. `MainSpecializationRules.Plan` proposes the single null-to-Rider/Heavy/Cannon transition; the dedicated change preserves all other owned values. Generic owned/trait-growth changes cannot grant, replace or erase it. XP, trait-growth and Hunter record reconstruction preserve the field; immutable collection copies and `SameValues` retain/compare it. Fresh roster acquisition remains unspecialized. No Main persistent state, native definition, trait history or currency owner was moved.

`UnitSpec.CategoryKey` and `UnitDefinition.Category/CategoryKey` remain identity. Effective specs expose `SpecializationCategory/CombatCategoryKey`; compiled definitions reference `CombatCategory/CombatCategoryKey` from the existing registry and derive `MoveRange` from it once. Legacy null constructors retain category/movement behavior and `PlayerMainDefinitionPolicy` is unchanged. Tests use two synthetic Main identities and configured movement 6/1/7/0, including zero disabling voluntary movement. Existing action-use, no-cost, profile, ownership and Main-policy regressions pass.

The internal character store prepares and installs `MainSpecializationChange` using its existing roster/revision. Preparation is read-only. Installation checks token owner/single use, current expected record, index/revision and overflow before any write, replaces one existing owned record and advances revision exactly once. Main persistent state, souls and unrelated owned entries remain unchanged. Current Run/deck/living-selected-Main/growth-readiness coordination is intentionally M3.

**Actual reconstruction/compatibility adjustments:** `HunterTraitRule.Award` was an additional real owned-record reconstruction found by the M1.1 audit. `OwnedUnitSpecResolver` and candidate hypothetical `UnitSpec` construction only preserve an already supplied effective-spec field in M1; applying a specialization from owned state, changing the empty-growth bypass, effective combat snapshots/rules, Capture validation and progression union remain M2. Both existing TraitState test helpers used fixed seven-argument constructor reflection; they now pass the preserved trailing field without removing cases/assertions. No test was deleted or relabeled to maintain a count.

**Verification:** Unity `6000.0.73f1`, the existing runner and approved AppData-enabled execution, sequential instances. Evidence files are `.superpowers/trait-system/<Run>.xml` and `.log`.

| Run | Cases | Passed | Failed | Skipped | Meaning |
|---|---:|---:|---:|---:|---|
| `mainclass-m1-baseline-core` | 4359 | 4359 | 0 | 0 | Fresh pre-change Core baseline |
| `mainclass-m1-baseline-integration` | 477 | 470 | 7 | 0 | Fresh pre-change integration baseline |
| `mainclass-m1-red` | 6 | 0 | 6 | 0 | Missing pure specialization transition, compiled behavioral RED |
| `mainclass-m1-store-red` | 1 | 0 | 1 | 0 | Missing typed store boundary, compiled behavioral RED |
| `mainclass-m1-owned-green` | 50 | 50 | 0 | 0 | Owned, XP, growth and contract guards |
| `mainclass-m1-store-green` | 7 | 7 | 0 | 0 | Typed store source/owner/reuse/overflow/new-roster checks |
| `mainclass-m1-category-red` | 4 | 0 | 4 | 0 | Missing effective-spec input, compiled behavioral RED |
| `mainclass-m1-green-verified` | 138 | 138 | 0 | 0 | M1 and existing category/profile/ownership/action/movement checks |
| `mainclass-m1-final-core-verified` | 4398 | 4398 | 0 | 0 | Full Core, 39 added cases, none removed |
| `mainclass-m1-final-integration` | 484 | 477 | 7 | 0 | Full CoreIntegration, 7 added cases, none removed |

The first M1.2 green attempt rejected three copy fixtures with mismatched definition/owned keys; fixture keys were corrected. The first full Core run exposed the two fixed-arity reflection helper failures described above. Final reruns supersede these attempts; their XML/logs remain available. Final baseline comparison found **identical failed fullnames and messages**, zero new failures, zero removed cases and zero skips. Both final logs contain zero C# compiler errors. This is not a wholly green integration suite.

The following seven existing integration failures remain unchanged:
- `ProjectL.Tests.CoreIntegration.BossStagePreparationTests.ProductionBossStage_PreparesAndPlacesBoss` — System.Collections.Generic.KeyNotFoundException : Stage data not found. StageKey: 2101.
- `ProjectL.Tests.CoreIntegration.CombatCameraZoomPoseTests.PostProcessingExtensionIsAllowedButPoseWriterIsRejected("Cinemachine.CinemachineCameraOffset",False)` — System.TypeLoadException : Could not load type 'Cinemachine.CinemachineCameraOffset' from assembly 'Cinemachine, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
- `ProjectL.Tests.CoreIntegration.DataBaseAssetTests.ProductionShotGun_HasExplicitDurationOneAndUnchangedMagnitudeAndGuid` — System.InvalidOperationException : Sequence contains no elements
- `ProjectL.Tests.CoreIntegration.HuntersMarkAuthoringTests.OwnedProjection_PreservesActualAuthoredPercentageWithoutChangingAsset(10)` — System.InvalidOperationException : Sequence contains more than one element
- `ProjectL.Tests.CoreIntegration.HuntersMarkAuthoringTests.OwnedProjection_PreservesActualAuthoredPercentageWithoutChangingAsset(25)` — System.InvalidOperationException : Sequence contains more than one element
- `ProjectL.Tests.CoreIntegration.RetiredTraitAuthoringTests.ProductionShotgun_UsesMarkTrait` — System.InvalidOperationException : Sequence contains no elements
- `ProjectL.Tests.CoreIntegration.TraitTurnConfigurationTests.UnitCopy_CarriesDurationAndFreezesAlreadyBuiltDefinitions` — System.NullReferenceException : Object reference not set to an instance of an object

The Stage lookup lacks key 2101; the camera test requests an unavailable CinemachineCameraOffset type. The other five failures depend on production ShotGun trait shape: absent HuntersMark entries, a multiple-element list where Single is used, or missing serialized trait fields. These were present before M1 and were not changed to make this phase pass.

**Locality and architecture review (observed):** a specialization change now needs the pure transition and typed existing-store contract, without reading payment, shop receipts, growth offers, UI, Job or unrelated trait internals. Persistent selection still has one owned-record authority; effective category/configuration is derived and uses the registry object. SRP separates the rule from publication, legacy null construction preserves behavior (LSP), the narrow internal store contract limits dependencies (ISP/DIP), and new valid Main definitions need no key allowlist (OCP). Framework registration/export/lifetime paths are unchanged (FW-001/002/003/016); mutation stays at the existing Buffer owner (FW-004). The shared record still requires Core/CoreIntegration regression coverage; no verification reduction was inferred from locality.

A fresh read-only independent M1 review found no actionable issue after inspecting the two test helper updates. Its deferred items are exactly M2 owned/battle/progression integration, M3 contextual coordination and acquisition/View/content work. No PlayMode, visual/manual acceptance, full View/Framework suite, production availability or external delivery is claimed. Paired metadata, UTF-8/LF and `git diff --check` were verified. Unity's generated FMOD scan-folder-only change was reverted to its pre-run value; no cache was deleted and no Unity upgrade was made.

### 2026-10-31 — Phase M2 execution checkpoint

The user explicitly authorized **M2 only**, continuing the other session's uncommitted M1 result, and identified existing integration failures being repaired in a separate session. M2.1 and M2.2 are complete. Stop before M3. No specialization grant command, contextual application service, shop/level-up acquisition, production SO edit, scene/UI work, commit or push was added.

**Source:** HEAD `3a05140fdf60f9cac8b2867988063eb280306b3c` plus M1, M2 and the separate test-input-isolation changes. The pre-M2 files are preserved under `.superpowers/sdd/2026-10-29-main-specialization-plan/m2-baseline/`. `m2-only.diff` isolates this phase from M1 and concurrent work; `m2-files.txt` lists 21 C# files (14 runtime, 7 test). The 42 source/meta hashes are recorded in `m2-source-manifest.json`, manifest SHA-256 `81CAD1E3845ECE7430B0D42796D4F6ABFB700983E312DC9409B25CAF652E2FB5`. Final tests and independent review used the same code hashes.

**M2.1 implemented:** `OwnedUnitSpecResolver`, `OwnedCharacterStatsCalculator` and `BattlePreparationCalculator` now compose the owned specialization even with empty trait history. UnitDefinition keeps Main identity and derives its specialization from the existing effective category; category runtime/snapshots and movement use CombatCategory. Main persistent ATK/HP/DEF, ownership, native weapon/traits and Hunter contribution remain unchanged. `CharacterBaselineStatsSnapshot.CategoryKey` remains identity, with SpecializationCategory and derived CombatCategoryKey. `UnitStatsSnapshot.Category` remains the effective combat category and additionally exposes SpecializationCategory, derived IdentityCategoryKey and CombatCategoryKey. Its With and SameValues retain/compare the choice. Capture compares the effective profile category and specialization; existing Cannon SafeBombardment is bound once. Movement contexts compare the added fact, and the existing CharacterBuffer rejects battle facts whose specialization disagrees with the owned source, including fresh battle installation.

**M2.2 implemented:** `EffectiveTraitProgression.Resolve` is the shared pure owner of unchanged Main steps plus a distinct Main/selected-class pool union. Candidate selection, history reconstruction, roster progress/offer/choice paths and catalog validation use it. Existing compatibility, availability, deterministic sampling, three-candidate Acquire sufficiency, Enhance eligibility, frozen offers and acknowledgment rules remain authoritative. Class selection does not grant traits, import class steps, replay prior steps or create an opportunity for an empty Main sequence. `TraitGrowthCatalogValidator.ValidateSpecialization` checks all reachable paths for a requested Main/class using the native profile. Battle preparation calls it before installation. Per-command `BuildProfiles` reconstructs current history without repeating the full future-path traversal; no new cache or state owner was added.

**Concrete integration evidence:** configured Rider/Heavy/Cannon movement 6/1/7 and Cannon 0, native Main stats with a Hunter bonus, outgoing and incoming category damage, effective Capture, snapshot copies, stale movement context, live/fresh Buffer rejection, ordinary move/attack limits and early end-turn were exercised. A real trait command/receipt/acknowledgment sequence acquires and enhances a class-pool trait while retaining prior Main history, then enters a real database-sourced battle. Night completion preserves the existing policy: Main stays on the field, Subs return to hand; specialization survives both night and teardown. Abandon/EndBattle/NewRun retains the old record through teardown and creates a fresh unspecialized Main. None of these fixtures is a production acquisition route.

**Verification:** Unity `6000.0.73f1`, existing runner, approved AppData-enabled route, sequential editor instances. Evidence is `.superpowers/trait-system/<Run>.xml` and `.log`.

| Run | Cases | Passed | Failed | Skipped | Meaning |
|---|---:|---:|---:|---:|---|
| `mainclass-m2-baseline-core` | 4398 | 4398 | 0 | 0 | Fresh M1-based Core baseline |
| `mainclass-m2-baseline-integration` | 491 | 489 | 2 | 0 | Concurrent authoring fixes had already removed five of the previous seven failures |
| `mainclass-m2-combat-red` | 12 | 0 | 12 | 0 | Missing owned/battle specialization behavior |
| `mainclass-m2-combat-green` | 98 | 98 | 0 | 0 | Combat, M1, owned stats and shape checks |
| `mainclass-m2-growth-red-behavior` | 10 | 1 | 9 | 0 | Missing class pool access/history/path validation; empty Main steps already behaved correctly |
| `mainclass-m2-context-red-verified` | 1 | 0 | 1 | 0 | Specialization-only profile changes were omitted from movement context comparison |
| `mainclass-m2-growth-green` | 83 | 83 | 0 | 0 | New and existing growth/contract regressions |
| `mainclass-m2-integration-green-verified` | 38 | 38 | 0 | 0 | Real battle, growth publication, M1 store and existing transaction regressions |
| `mainclass-m2-validation-boundary-red` | 1 | 0 | 1 | 0 | Future graph traversal incorrectly repeated during runtime profile composition |
| `mainclass-m2-validation-boundary-green` | 37 | 37 | 0 | 0 | Entry-only graph validation, new combat/growth and copy-preservation checks |
| `mainclass-m2-final-core-verified` | 4425 | 4425 | 0 | 0 | Full Core; 27 added cases, zero removed |
| `mainclass-m2-final-integration` | 507 | 506 | 1 | 0 | Full integration; 15 M2 cases plus concurrent test-isolation changes |

The initial full Core run (`mainclass-m2-final-core`) passed 4,423/4,424 and exposed `ContractWithPreservationTests.Stats_WithEachValue_ChangesOnlyThatValueAndItsDerivedValues`: changing an ordinary category now also changes the two newly derived category keys. Its expected changed-property list was updated; the original assertions/cases remain. Earlier focused attempts exposed test-only API/fixture mistakes (NUnit collection syntax, CoreIntegration's absent Domain reference, minimum unlock level, battle-active owned query, rejected preview context and a fixed Run ID not yet copied to the deck). These were corrected without changing the existing product policies. `mainclass-m2-integration-red` separately reproduced the two missing Buffer class guards and the unavailable class-pool choice. The final runs supersede those attempts. Both final logs have zero C# compiler errors; no unexplained skips or new failures remain.

**Remaining integration failure:** `ProjectL.Tests.CoreIntegration.CombatCameraZoomPoseTests.PostProcessingExtensionIsAllowedButPoseWriterIsRejected("Cinemachine.CinemachineCameraOffset",False)` — TypeLoadException for `Cinemachine.CinemachineCameraOffset` in the installed Cinemachine assembly. This was one of the original seven and is outside M2. The separate test-input-isolation session repaired the production-dependent authoring/boss cases; M2 did not edit those files or claim those fixes. Their changed case counts/names are distinguished from M2 additions. This is not a wholly green integration suite.

**Locality/architecture review (observed):** the optional owned field remains the only persistent selection authority. Configured category objects and effective progression are derived; no alternate roster, stat/XP engine, transaction owner, service registration or Job was introduced. Changing class movement needs category configuration and existing projection contracts; changing candidate access needs the shared progression resolver and growth rules, not shop prices or UI internals. All actual combat/owned projection, source-comparison and reconstruction consumers were checked. Shared record/snapshot changes still required the full Core/CoreIntegration regressions. Framework assembly direction, composition, lifetime and existing Buffer mutation ownership remain intact (FW-001/002/003/004/016).

A fresh independent M2 review found no Critical, Important or Minor issue and verified the final XML and all 42 source/meta hashes. Two scope boundaries were explicitly retained: (1) M3 contextual preparation/application and acquisition/UI/content remain deferred by the user's phase boundary; (2) the existing direct Prepare overload may omit AttackProfiles for partial/pure fixtures, so it does not establish full weapon/progression compatibility when profiles are absent. The actual DB source supplies profiles and passes the integrated validation. Neither boundary authorizes new gameplay behavior or weakens existing production entry checks.

UTF-8/LF, paired metadata, `git diff --check` and source/evidence consistency passed. No PlayMode/manual, full View/Framework, production acquisition or external handoff delivery is claimed. M2 is complete; M3-M4 remain unchecked.

### 2026-10-31 — Phase M3 execution checkpoint

The user explicitly authorized **M3 only**. M3.1 and M3.2 are complete. Stop before M4; its handoff was not created. No acquisition command, shop/level-up integration, production SO, UI/scene, commit, push or external delivery was added.

**Source:** HEAD `3a05140fdf60f9cac8b2867988063eb280306b3c` plus the preserved M1/M2 and separate test-input-isolation changes. The exact M3 starting copies and isolated diff are in `.superpowers/sdd/2026-10-29-main-specialization-plan/m3-baseline/`. `m3-files.txt` lists 14 source/meta files; `m3-manifest.sha256` records their hashes (manifest SHA-256 `93A6A67400681A1BEC1CD156FA7A18A104A9EBFA4F92DC7945265E71BE7FCE71`). Root verification found all 14 hashes unchanged after the final gates and review. The implementation report links each requirement to its actual test.

**Implemented:** Internal `CharacterRosterService.PrepareMainSpecialization` captures the original Run, roster revision, selected deck/Main health, battle generation/revision, growth offer/result/acknowledgment and immutable DB values without mutation. It requires an in-progress nonempty Run, the living selected Main, no active/unsettled battle and the existing `ReadyToContinue` result. Pending steps without an offer, frozen offers and unacknowledged results all block standalone application. Invalid caller/domain conditions return typed failures; missing required configuration retains its exception contract.

Native and completed-history compatibility use `CombatAttackProfile` and `OwnedUnitSpecResolver`; future paths use `TraitGrowthCatalogValidator.ValidateSpecialization`. Equivalent fresh DB conversions are accepted by value. The original Core source is checked again after Buffer preparation and before installation. `MainSpecializationTransactions`, composed in the existing `BufferSystemGroup`, validates state/shape and reuses M1's typed character-store preparation/install. Foreign, reused, stale and overflowing preparations fail before mutation. Success changes only the owned specialization and roster revision once; no result receipt or acquisition opportunity is invented.

**File organization:** The service gains a feature partial, `CharacterRosterService.MainSpecialization.cs`, while retaining its existing registration/lifecycle. `MainSpecializationOperationPreparation` and `MainSpecializationDefinitionSnapshot` hold ephemeral evidence. `MainSpecializationTransactions.cs` contains the internal Buffer contract, source evidence and prepared token. These supporting files implement the planned service/Buffer boundary without additional services, state owners or layers. The two new integration fixtures exercise the real composition and subsequent `BattleEntry.EnterBattle` path.

**Verification:** Unity `6000.0.73f1`, existing runner through the approved AppData-enabled route, sequential editor instances. XML/log files are `.superpowers/trait-system/<Run>.xml` and `.log`.

| Run | Cases | Passed | Failed | Skipped | Meaning |
|---|---:|---:|---:|---:|---|
| `mainclass-m3-preparation-red-01` | 3 | 0 | 3 | 0 | Missing internal preparation boundary |
| `mainclass-m3-preparation-green-01` | 3 | 3 | 0 | 0 | Initial three-class read-only preparation |
| `mainclass-m3-null-red-03` | 1 | 0 | 1 | 0 | Required null configuration was swallowed as a typed refusal |
| `mainclass-m3-focused-04` | 57 | 57 | 0 | 0 | Expanded preparation/application boundaries |
| `mainclass-m3-battle-source-red-06` | 1 | 0 | 1 | 0 | Entered/ended battle reset QueryRevision and escaped freshness checks |
| `mainclass-m3-history-red-07` | 2 | 1 | 1 | 0 | Actual completed history passed; a new native duplicate was incorrectly accepted |
| `mainclass-m3-focused-final-08` | 68 | 68 | 0 | 0 | All M3 behavior and semantic-comparison guards |
| `mainclass-m3-full-core-09` | 4425 | 4425 | 0 | 0 | Full Core; unchanged case set |
| `mainclass-m3-full-integration-10` | 575 | 574 | 1 | 0 | Full CoreIntegration; 68 added, zero removed |

The null failure was corrected by preserving `ArgumentNullException`; battle freshness now includes the existing `BattleInstanceId`; completed-history validation reuses the existing resolver/profile constructor. Earlier expanded attempts also exposed two test-fixture errors: a targeting key rejected before reaching the intended profile test, and a payload-coverage generator excluding valid enum zero. These fixtures were corrected without changing game policies. Final focused/full XML supersedes earlier attempts. Final logs contain zero C# compiler errors and all runs above selected nonzero cases.

**Remaining failure:** `ProjectL.Tests.CoreIntegration.CombatCameraZoomPoseTests.PostProcessingExtensionIsAllowedButPoseWriterIsRejected("Cinemachine.CinemachineCameraOffset",False)` — `System.TypeLoadException : Could not load type 'Cinemachine.CinemachineCameraOffset' from assembly 'Cinemachine, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.` Root and independent reviewer compared its exact fullname/message to M2's final XML and found them unchanged. The integration suite is not wholly green; camera/View work remains outside M3.

**Actual coverage/locality:** All three classes and two synthetic Main definitions use the actual service. Real acquire/enhance/acknowledgment history survives application and the next battle. Success preserves identity, health, XP, Hunter bonus/history, native weapon/traits, deck, souls, other owned entries, currency, shop and growth counters. Core and direct Buffer rejection tests compare stores immediately before rejection, including legitimate intervening state changes. Tests also mutate state/definitions during reads and after Buffer preparation to demonstrate that a newly captured Buffer source cannot legitimize an old Core decision.

The owned record remains the sole specialization authority; the existing roster/deck/health/growth owners are reused. Rule changes require the pure transition and feature service boundary, publication changes require the typed existing-store contract, and neither requires shop/UI/Job internals. All-roster definition reads are a real dependency of the existing global growth-readiness query. Shared contract regressions remain necessary; no verification reduction was claimed. Existing FW registration/export/lifetime and Buffer mutation ownership are retained (FW-001/002/003/004/016).

**Maintenance limitation:** Existing `TraitSpec` contracts have no complete semantic equality; runtime status snapshots omit authored fields. The bounded explicit DB-value comparison therefore requires maintenance when a trait type/payload changes. A concrete-type/constructor-payload coverage test detects omissions. The comparison also conservatively includes catalog descriptions and unselected-class payloads, so some otherwise harmless DB edits invalidate an outstanding preparation. No generic reflection engine, catalog cache or broad trait-contract refactor was introduced; production reflection is not used.

A fresh independent read-only review found no actionable spec-compliance, correctness, atomicity or ownership defect and explicitly retained the comparison-maintenance limitation above. Root independently checked the final XML, exact baseline failure, compiler logs and source hashes. UTF-8/LF, paired metadata/GUID uniqueness and `git diff --check` passed. Existing M1/M2, separate test-isolation and dirty FMOD work were preserved. M3 is complete; M4 and player-facing acquisition/content/View/manual acceptance remain pending.
### 2026-10-31 — Phase M4 execution checkpoint

The user authorized M4 after completing M2/M3. Created [Main specialization Core/View handoff](../handoffs/2026-10-29-main-specialization-core-view-handoff.md). All four M4.1 tasks are complete. This phase changes only this plan and the handoff; runtime/tests, production assets, View/Flow/UI/scenes and the existing dirty FMOD/test-isolation work were preserved. No external message, commit or push was performed.

**Verification reused deliberately:** M4 independently parsed M1/M2/M3 focused/final XML, including M3 focused 68/68, full Core 4425/4425 and full CoreIntegration 574/575 (zero skips, zero C# compiler errors). M3 source/meta hashes matched 14/14. All 42 M2 manifest entries matched current sources or the exact M3 starting copy for the one intentionally changed service file. No C# under project scripts/tests was newer than the final Core XML. The M2/M3 integration comparison confirmed 68 added cases, none removed, and identical remaining failure fullname/message. Documents are the only M4 deliverable, so no Unity suite was rerun; the plan explicitly requires reusing still-current evidence.

**Retained failure:** `ProjectL.Tests.CoreIntegration.CombatCameraZoomPoseTests.PostProcessingExtensionIsAllowedButPoseWriterIsRejected("Cinemachine.CinemachineCameraOffset",False)` — TypeLoadException for the unavailable `Cinemachine.CinemachineCameraOffset` type in the installed assembly. Full integration is not wholly green. Earlier seven-failure M1 records remain historical; separately owned test-input-isolation fixes account for repaired boss/authoring cases.

**Handoff contents checked against actual code:** owned/baseline/battle identity and effective-category fields (including nested OwnedCharacterStatsSnapshot.Stats), sole owned-state authority, once-per-Run behavior, native stat/weapon/history preservation, configured category rules, effective Main-step/pool composition, internal service/Buffer/store signatures, preconditions, typed failures, source/generation/DB-value freshness and no partial mutation. It identifies pure Plan and typed store preparation as the reusable transaction pieces and keeps standalone apply internal. Future level-up and shop examples require atomic choice consumption/result or payment/class/receipt publication; neither may apply then consume/spend afterward. Existing TraitGrowthSelection remains StepId/TraitKey.

**Observed boundaries:** Core owns content/progression validation; existing CharacterBuffer owns state installation; the existing BufferSystemGroup composes the narrow transaction. No additional state/configuration authority, root service, lifecycle, Job or cache was added (SSOT, SOLID, FW-001/002/003/004/016). The handoff documents actual modification/understanding/verification scope for movement, candidate access, application guards and future adapters, including necessary all-roster growth-readiness dependencies and shared Core/CoreIntegration regressions. The explicit authored-trait comparison maintenance cost and conservative invalidation of descriptions/unselected-class payloads are retained. Optional-profile pure fixtures do not establish full compatibility; actual DB battle entry supplies profiles.

**Delivery states:** Core foundation verified with the stated baseline caveat; local handoff prepared; external delivery not performed. Production acquisition, prices/unlock levels/pools, display wiring and PlayMode/manual/visual/production acceptance are deferred integration/content work, not incomplete Core-only deliverables. There is no activation task to execute. Stop after M4.

Independent read-only handoff review found no actionable documentation mismatch against code/contracts and retained the evidence/maintenance/delivery caveats. Its earlier plan snapshot preceded the M4 status update; root separately verified the final four checked tasks, current status and this execution record. Relative document links, UTF-8/LF and git diff --check passed. No runtime changes were made during review.
