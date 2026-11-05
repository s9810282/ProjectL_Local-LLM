# Buff and Debuff System — Core-to-View Handoff

Date: 2026-10-30. Status: **Core implementation/evidence complete with the documented existing integration exception; handoff prepared; View implementation/acceptance pending; not externally delivered.**

Phase C Core contracts and the HuntersMark cutover are implemented and verified; Phase C evidence remains in section 6. Phase E retirement changes and receiving-owner requirements are in section 7. This document was prepared before C4 and is being maintained against later authorized Core changes. This handoff does not authorize this Core worker to edit View/UI/presentation DB or run their tests. Final Phase F Core acceptance and remaining release gates are recorded in section 9.

Plan: [implementation plan](../plans/2026-10-30-buff-debuff-system-implementation-plan.md). Design: [approved status design](../specs/2026-10-30-buff-debuff-system-design.md).

## 1. Ownership and exported contracts

The existing flow remains InGame authoring -> Core isolated command projection -> CombatCommandTransition -> CombatTransitionPublisher -> BattleTransactions prepare/validate/install -> immutable CharacterData and result facts -> View. Core owns state, magnitudes, source identity, clocks and damage/recovery. View presents supplied facts; it does not choose the strongest source, decrement clocks, recreate damage or update Buffer state.

Contract namespaces below start with `ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator` (`C`). Buffer types start with `ProjectL.Systems.RuntimeSystem.BufferSystems` (`B`).

| Contract | Exact public members / use |
| --- | --- |
| `C.Contracts.Combat.CharacterCombatFacts` | `StatusEffects`, `WithStatusEffects(StatusEffectSet)`, `With(...)`, `WithPlacement(...)`, `SameValues(...)`. Immutable facts; partial legacy writes replace only the families their DTO can represent. |
| `C.Contracts.CombatEffects.StatusEffectSet` | `Empty`, ordered `IReadOnlyList<AppliedStatus> Entries`, defensive-copy constructor, `SameValues`. Source duplicates are allowed where the family policy allows them. |
| `AppliedStatus` in that namespace | `Key`, nullable `Origin`, typed immutable subclasses. Nullable provenance exists only for preserved legacy synthetic facts; live grants carry valid origins. |
| `StatusEffectKey` / `StatusEffectCatalog` | Stable keys 1-13, independent of TraitKey; `WeaknessExposure=13`, `None=0`; `ClassificationOf(key)` returns separate `StatusEffectClassification.Buff/Debuff`. |
| `B.Character.CharacterData` | `StatusEffects`, cached `IReadOnlyList<StatusEffectSnapshot> StatusEffectSnapshots`, `ToFacts()`, existing effective-stat fields. The canonical set is the same immutable set as `ToFacts().StatusEffects`. |
| `C.Application.Combat.StatusEffectReadModel` | `Capture(CharacterCombatFacts)` returns effect rows for supplied facts, including intermediate playback facts. It does not mutate or advance time. |
| `C.Contracts.Snapshots.StatusEffectSnapshot` | `Key`, `Classification`, `Sources`. Rows sort Buff then Debuff, then effect key; sources retain accepted order within their family. |
| `StatusEffectSourceSnapshot` | `Status`, `Origin`, `LifetimeKind`, nullable `RemainingControllerTurnEnds`, nullable `RemainingAffectedSideTurnEnds`, nullable `LastResetControllerTurnCount`, `Eligible`. Inspect the typed `Status` for its magnitude/budget. Do not coalesce different clock kinds into one number. |
| `C.Contracts.Combat.CombatTraitStateEvent` | `Expected`, `Final`, optional `Origin`, paired `ExpectedStats`/`FinalStats`; `Kind=TraitStateChanged` (41). Apply the complete `Final` at this event's ordering boundary. |
| `C.Contracts.Snapshots.Traits.HuntersMarkTraitStatusSnapshot` | `BonusDamagePercent`, `DurationAffectedSideTurnEnds`, existing availability. The same configured description is supplied as `OwnedCharacterStatsSnapshot.HuntersMarkDescription` and in combat trait statuses. Configured duration is not a recipient's remaining duration. |

All configured HuntersMark data/spec/definition constructors require the duration explicitly. No runtime default grant lifetime exists. SO authoring has initial `1` and Inspector minimum `1`. ShotGun remains 30%, now explicitly authored with duration 1. Existing growth slots can independently configure normal/enhanced values; no missing enhanced definition or growth pool was added.

## 2. Typed rows and distinct lifetimes

| Row / source type | Values and lifetime |
| --- | --- |
| `AttackIncreaseEffectSnapshot` / `AttackIncreaseStatus` | `AppliedAmount`, `HeldForAttackAmount`, `TotalAmount`; applied sources have `RemainingControllerTurnEnds`. Held Acceleration remains with its trait, `HeldLifetimeKind=AttackCompletion`. |
| `AttackDecreaseEffectSnapshot` / `AttackDecreaseStatus` | First accepted `ReductionPercent`; `AttackCompletion`. Phase E removes the legacy priority payload; repeated application preserves the first reduction. |
| `DefenseIncreaseEffectSnapshot` | Cohesion `Amount`, `CurrentEligibility`; no fabricated source list or timer. |
| `DefenseDecreaseEffectSnapshot` / `DefenseDecreaseStatus` | `EffectiveDefense`, ordered per-source `ReductionPercent`, `BattleEnd`; no fictitious aggregate expiry. |
| `ShieldEffectSnapshot` / `ShieldStatus` | `RemainingEligibleBudget`; each source carries `RemainingBudget`, `Eligible`, historical `LastResetControllerTurnCount`. Lifetime `ReconciledBudget`, not a remaining-turn countdown. Suppressed/spent sources remain visible in details. |
| `ActionRestrictionEffectSnapshot` / `ActionRestrictionStatus` | Disarm or Bind key, `RemainingAffectedSideTurnEnds`, `AffectedSideTurnEnds`. Forced movement and voluntary movement are different Core rules. |
| `WeaknessExposureEffectSnapshot` / `WeaknessExposureStatus` | `MaximumIncreasePercent`; every source carries `IncreasePercent`, `RemainingAffectedSideTurnEnds`, `AffectedSideTurnEnds`. Lower sources remain active and tick while suppressed. |

TrackingShot, FocusedBombardment, Entrenchment and terrain contributions need current target/profile/terrain context. Existing `EffectTargetDamageContribution` / `EffectIncomingDamageContribution` carry a nullable evaluated effect identity when active. Do not infer unconditional actor badges from possessed traits. GroundCompaction/DustScreen lifetimes remain with terrain; shield reset configuration stays with the producer. No duplicated recipient timers are provided.

## 3. Weakness Exposure behavior and deterministic examples

The policy uses the maximum active percentage. Logical source identity is the applying character plus applying trait, not callback RuleKey. Different traits on one character and the same trait on different characters are independent. Same-source reapplication replaces magnitude and duration even when weaker/shorter, retaining its source position; it does not add duration. Generic provenance kinds remain distinct.

C1 policy/read tests verify these examples:

| Input / boundary | Expected output |
| --- | --- |
| source A: 20%/2; source B: 30%/1 | One WeaknessExposure row, effective 30%; two source details with remaining 2 and 1. |
| First affected-side end | B expires; A becomes effective 20% with 1 remaining. |
| Next affected-side end | No exposure row. |
| Replace B's 30%/3 with 10%/1 | B is now 10%/1; an independent A at 20% becomes effective. A different RuleKey does not create another B. |
| Other-side end | No exposure countdown. |
| Duration 1 applied during recipient's current side turn | That same side ending expires it. |
| Target enters Hand or EnemyWaiting | Remove all target exposure sources, preserve unrelated effect families. Field-to-Field relocation/persistent Main retains them. |
| Numeric 100 with 30%; numeric 1 with 30%; numeric 0 | 130; 1; 0. Integer floor, no new minimum. |

C4 live integration tests verify these boundaries:

- Source death/departure leaves the recipient's installed values intact. Target death and battle completion clear them. A return does not bank time or restore the status on redeployment.
- After actual recipient selection and existing non-item/held-defense calculation, `100 -> exposure 30% -> 130 -> shield 50 -> HP loss 80`. BeforeDamage items see 130; their existing order and healing/shield behavior remain intact.
- Exposed original target + unexposed Sacrifice protector: protector receives no exposure multiplier. Unexposed original + exposed protector: protector receives its own multiplier exactly once. The original living contact still receives the post-hit HuntersMark grant.
- Only eligible ordinary/extra hostile direct contacts grant the effect, after their hit. First grant does not amplify that same hit; later hits use the updated projection. Reactions/environment/mines/reflection can benefit from an installed exposure without automatically granting it. The source attacker also benefits.
- FinishingShot instant defeat/falling bypass numeric amplification. Reflection keeps its existing trigger, basis and termination; each reflected numeric recipient is evaluated once. Existing public preview/AI still excludes ThornHeart reflection. Its artifact activation records the greater of the original configured request and the shared evaluated pre-item request, separately from actual HP loss, preserving the existing request-bound validation.
- Direct20 plus barrel10 against an exposed30% target produces expected39/secondary13 and HP100 -> 61. With shield30, the published expected/secondary values are9 and HP91. The same evidence is verified for the retained forced-field path; the deferred ThornField producer is not enabled.

## 4. Required View changes

1. **Full-fact playback.** `Combat/Playback/Handlers/CombatFactEventHandler.cs` currently handles `CombatHuntersMarkEvent` by constructing `CharacterDebuffState` with only Intimidation, DefenseReduction and the old mark. That drops other representable debuff families. New exposure is not encoded by that DTO. Consume `CombatTraitStateEvent.Final` in full and preserve independently ordered HP/movement facts. Do not reconstruct just a subset of status fields or read a future Buffer snapshot early.
2. **Ordering registration.** `Combat/Playback/Ordering/CombatEventTypeTable.cs` currently registers `CombatHuntersMarkEvent` but has no `CombatTraitStateEvent` row. The current `RouteOf` returns `Skip` for an unregistered event, so intermediate status/cadence facts are not replayed by that path. Register the full event with its subject `Final.EntityId` and supplied origin at the appropriate existing fact boundary. Respect the complete event stream, root/child/attack ownership and existing movement/HP sequence contracts.
3. **Effect HUD.** `Combat/Display/Unit/FieldUnitPresenter.cs` currently checks `Debuffs.HuntersMark`, reads the HuntersMark trait visual and emits `HuntersMarkDebuff`. Use the supplied effect rows, one row per effect key with per-source details. `CombatObjectiveDisplay.cs` is a consumer integration audit point, not an independent timer/calculator. Weakness Exposure needs a broken-shield icon.
4. **Cards/deck.** `GlobalSystem/UISystem/Screens/Global/UnitCardTraitContent.cs` currently describes another ally's ordinary/extra hit consuming a mark. `ViewSystem/StageMap/Deck/DeckEditDisplayModel.cs` passes the configured HuntersMark description. Display configured percentage and affected-side duration for normal/enhanced definitions; remove one-hit consumption, indefinite duration, source exclusion and no-stacking wording. Do not change Hunter's separate selected-target Mark or permanent reward display.
5. **Visual lookup ownership.** Recommend an effect-keyed lookup under the existing View DB path, separate from possessed-trait visuals. The receiving worker chooses concrete files/layout. Follow `ViewDataBaseFactory` table creation/export -> `ViewTableKey` -> `ViewDataBase`/`IViewQuery` -> normal View consumer dependency. Validate required registrations/references there; do not introduce a Core service, root lookup shortcut, or duplicate gameplay values.

View paths above are relative to `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem` unless explicitly prefixed. View DB lives under `Systems/DataBaseSystem/Queries/ViewDataBase`, `Systems/Factory/DBFactory/Group`, and `Systems/ServiceKeys/DBFactory/Group`.

## 5. Compatibility and receiving-owner acceptance

`HuntersMarkState`, `CharacterDebuffState.HuntersMark`, `CombatHuntersMarkEvent` and numeric event ID 39 remain available to compile current consumers/read old facts. C4 has stopped producing the old single-hit mark and does not synthesize a false single-source mark from exposure. Coordinate eventual type/event-handler cleanup with the View owner. The existing full trait-state event is the new source; no second event stream or status store is added.

Receiving View acceptance (pending, not executed by this Core work):

- **Pending View owner:** Two sources display one effect and correct per-source details; max-to-fallback and weaker/shorter replacement are correct.
- **Pending View owner:** Ordered full-fact playback reaches the published final state without losing shields, restrictions, attack reduction, armor stacks, HP or movement facts.
- **Pending View owner:** Hand and EnemyWaiting return remove exposure; source removal leaves it; persistent Main counts its ordinary affected-side endings.
- **Pending View owner:** New percentage/duration descriptions and normal/enhanced differences reach unit/deck cards; old consumption text and old mark presentation are gone.
- **Pending View owner:** Broken-shield effect lookup and every required registration/reference validate in the owning View DB path.
- **Pending View owner:** Hunter Mark remains separate; no View-side countdown or damage calculation, duplicate expiry, or producer-name HUD rule is introduced.
- **Pending View owner:** Existing consumer integration, PlayMode, scene and manual visual acceptance run under the View owner. Core/CoreIntegration success is not evidence of those results.

The old CoreIntegration `HuntersMarkCardContentTests.cs` / `PullAndMarkPresentationTests.cs` were intentionally removed by Task 0.2. They are historical context, not Core gates to restore or execute. No handoff was sent externally.

## 6. Historical Phase C evidence and reusable result samples

Final Unity 6000.0.73f1 gates, approved AppData route, zero skips:

- `C-core-02`: **4,264/4,264**, exit0.
- `C-integration-02`: **390/391**, exit2. Only the unchanged `BossStagePreparationTests.ProductionBossStage_PreparesAndPlacesBoss` StageKey2101 failure remains. This is not an all-green integration suite.
- Both actual `DataBaseAssetTests` cases pass in the final full run: ShotGun 30%/1, unchanged GUID/type and catalog path; existing growth payload/key/clock validation.
- Focused evidence: C1 policy/read14/14; C4 receiving/lifecycle/reflection114/114; real publication18/18. An independent review's four red barrel/forced-field evidence cases pass in the final full Core gate after both callers consume the shared outcome.
- Six stale old single-mark tests were migrated, retaining damage provenance, immutable repeated previews, stale-state rejection and exactly-once publication. No View test was run or changed.

Existing production diagnostics remain explicit: the first cold growth-asset load in `C2-green-01` failed on 22 native SerializeReference `Refid -2` errors. Identical diagnostics occur in pre-C A/B logs and final C integration, with unchanged growth assets. Passing final data assertions does not repair those diagnostics. No log suppression, asset repair, cache deletion or Unity upgrade was used.

Observed Core examples for consumer fixtures:

| Fixture / ordered evidence | Verified result |
| --- | --- |
| `HuntersMarkAttackTests.OrderedHits_FirstGrantAffectsOnlyLaterHits` | Initial HP100; first numeric20 -> HP80, then `StatusApplied` and full `CombatTraitStateEvent` install25%/1. The next ordered20 hit receives25 and leavesHP55; direct total45. An unchanged same-source refresh emits no false state transition. |
| `WeaknessExposurePublicationTests.FullEventFacts_ReconstructPublishedExposureAndPreserveOldSnapshots` | Prior source20%/2 and new source30%/1 publish one row at30% with details `[2,1]`. Replaying complete character/trait-state facts reaches published status and HP. The prior snapshot remains20%. No old mark event is emitted. |
| `AffectedEnd_PublishesFallbackThenExpiryWithoutBankingOtherSide` | Other-side end retains30%; first affected-side end exposes20%/1; next removes the row. |
| `SourceAttackerBenefitsAndRefreshesWeakerShorterValues` | Existing source30%/3 amplifies its own current20 hit to26; after the hit its configured10%/1 replaces the same source. |
| `NumericReceivingBoundary_AmplifiesOnceBeforeShield` | Incoming100 ->130; shield50 -> remaining/actual HP loss80. All supported numeric kinds use the same boundary. |
| `SecondaryDamage_UsesReceivingOutcomeForPreviewAndLiveHealth` | Barrel/retained-field examples above agree across expected/secondary totals, projected/live HP and partial shield remainder. |
| `Reflection_AmplifiesItsRecipientWithoutChangingBasisOrRepeating` | Configured reflection3, recipient exposure100% -> request6, one reflection. Without shield, actual loss6; with shield4, actual loss2. |
| `StaleExpiryRefresh_InstallsNoPartialStateOrResult` and `LatePrepareFailure_InstallsNeitherGrantedExposureNorHealthNorResult` | Failed publication preserves every captured store/result; no partial status/HP installation. |

Exact commands, test migration details, architecture/locality assessment and manifests are in [plan section5.5](../plans/2026-10-30-buff-debuff-system-implementation-plan.md#55-phase-c-execution--2026-10-30). Local XML/log evidence is in `.superpowers/buff-debuff-system/`. All section5 receiving-owner cases remain **Pending View owner**. This handoff is prepared and not externally delivered; whole-game/visual release acceptance remains pending.

## 7. Phase E retirement delta and View follow-up

Intimidation now exposes attack reduction only through `IntimidationTraitAuthoring`, `IntimidationTraitData`, `IntimidationTraitSpec`, `IntimidationTraitDefinition` and `IntimidationTraitStatusSnapshot`. The pure constructors take one `attackReductionPercent`; `PriorityReduction`, `IntimidationDebuffState.PriorityPenalty` and `AttackDecreaseStatus.LegacyPriorityPenalty` are removed. `GetTraitAvailability(Intimidation)` no longer supplies `DeferredPriorityComponent`. Current View code has no references to the removed fields/types, so no Core compatibility shell is required. AttackCompletion, first-application retention, source identity and existing death/battle cleanup remain unchanged. General Priority stats, the Rope item and flag availability remain unchanged.

`TraitKey.MoveAttackDiscount = 20` is reserved and explicitly retired. Default registration, executable factories/rules/state codecs, authoring and pure data/spec/status types are removed; acquisition rejects this identity as `Retired`. There is no replacement action economy. Numeric historical activation kinds remain reserved without an active producer. The previous six retired identities remain retired. Regeneration remains available; Focus, ThornField and Necromancy remain deferred.

Serialized-reference inspection covered all Assets before deletion: no live discount authoring/data instances and no GUID/type references to the seven deleted pure C# types were found. Their seven `.meta` files were removed with them. Shielder keeps its asset GUID, managed-reference type and existing 20% attack reduction; only the obsolete serialized `priorityReduction` member was removed. Existing View assets were preserved.

Required receiving-owner changes and acceptance, **pending View owner**:

- **Pending View owner:** `Assets/ProjectL/DataBaseSO/ViewGroup/TraitVisual/Item/TraitVisual_Intimidation.asset`: remove the repeated-hit Priority reduction sentences and placeholder. Describe only attack reduction through the recipient's next completed attack. Read the configured percentage from supplied Core trait data/status and the applied reduction from the typed effect row.
- **Pending View owner:** `Assets/ProjectL/DataBaseSO/ViewGroup/TraitVisual/Item/TraitVisual_MoveAttackDiscount.asset` and its View table reference: remove or hide the retired offer/description through the owning presentation registration path. The retained key-20 visual row is inert presentation data; it cannot execute or acquire a trait.
- **Pending View owner:** `Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Global/UnitCardTraitContent.cs`: the old `DeferredPriorityComponent` text mapping has no current Core producer. Coordinate its eventual removal with this owner; it is not a live compatibility dependency.
- **Pending View owner:** Verify cards/HUD show Intimidation as fully available with attack reduction only and never offer the retired discount; retain general Priority/item display behavior. Run consumer integration, scene/PlayMode and manual checks in the View workspace. Core-only tests do not establish these visual results.

Phase E verification and exact migration/reference evidence are recorded in implementation plan section 5.7. Final integrated Core acceptance is recorded separately below; external delivery remains pending.

## 8. Regeneration configuration, cadence and recovery contract

`C.Contracts.Snapshots.Traits.RegenerationTraitStatusSnapshot` exposes `HealAmount`, `HealEveryOwnSideTurnEnds`, and `CompletedEligibleTurnEnds`. Read it from the holder's full facts (`CharacterData.ToFacts().Stats.TraitStatuses`). The public constructor is `(int healAmount, int healEveryOwnSideTurnEnds = 1, int completedEligibleTurnEnds = 0)`; amount/interval are positive and progress is in `[0, interval)`. Existing amount-only callers retain interval1 compatibility. Authoring/Data/Spec/Definition carry the same configured amount/interval; runtime state, effect snapshot, codec and status snapshot carry the single holder-owned progress value. There is no recipient Healing status, independent timer or secondary HP total.

The existing `Unit_UndeadHeavy.asset` keeps amount10 and explicitly authors interval1, with its GUID/type unchanged. Temporary unit/growth copies prove independent normal/enhanced intervals2/3 and immutable old values; no absent enhanced entry was added. ShotGun remains30%/1; Shielder remains20% attack reduction only.

After end-of-side environment/gimmick damage and terminal outcome checks, each living Field holder of that side advances once. Due recovery uses the existing capped health settlement. Full HP consumes the opportunity; dead/non-field holders do not heal or advance. Other-side ends and previews do not tick. Field exit, death, battle cleanup and a fresh post-growth battle reset progress; ordinary relocation and a persistent Field Main retain it. Last-night enemy recovery happens before recall if combat remains active. Live trait replacement is prohibited by existing battle-growth guards; the supported enhancement path builds fresh zero progress next battle.

`CombatTraitStateEvent` carries changed progress, followed by `HealthRecovered` trait activation and normal health facts when actual healing is positive. Activation magnitude and HP delta report actual recovery, not the configured request. A due full-HP opportunity can reset progress without a heal animation event; interval1 may leave progress unchanged at0. Apply supplied facts in order. Main persistent HP travels through the same transaction.

| Core fixture | Expected sequence |
| --- | --- |
| `RegenerationTurnTests.SideEnd_HealsOnlyTheLivingFieldHoldersOfItsOwnSide` | Idle eligible holder, amount10/interval1, HP40 ->50 at its side end. |
| `RegenerationCadenceTests.IntervalTwo_HealsOnlyOnEligibleEndsTwoAndFour` | Start progress0; end1 ->1/no heal; end2 ->0/heal; repeat at ends3/4. |
| `FullHealthDueEnd_ConsumesTheOpportunityWithoutBanking` | A scheduled full-HP recovery is consumed; later damage does not receive banked healing. |
| `RegenerationTurnTests.EndEnvironment_ResolvesDamageAndOutcomeBeforeRegeneration` | Nonterminal survivors recover after damage; lethal/terminal outcomes do not heal or continue the turn. |
| `RegenerationPublicationTests.EndTurn_PublishesCappedRecoveryAndMainHealthOnce_WithoutChangingOldSnapshots` | HP at max minus5 with request10 yields actual5; immutable old snapshot retains its prior HP; replay does not heal twice. |
| `NightRecall_PublishesZeroCadenceForReturnedSub_AndRetainsFieldMainProgress` | Recalled Sub has progress0; persistent Field Main keeps its independent progress. |
| `FreshBattle_RejectsCarriedCadenceBeforeAnyStoreInstalls`, `StaleCadence_RejectsEveryStoreAndResult`, `LatePreparationFailure_PublishesNeitherCadenceNorHealthNorResult` | Invalid/stale publication installs no partial cadence, HP or result. |

- **Pending View owner:** display configured regeneration amount/interval from supplied trait data; render actual capped recovery from existing activation/health evidence. Do not decrement cadence, infer healing from elapsed animations, or replay the configured amount as actual recovery.
- **Pending View owner:** cancel/rebind/refresh must preserve published state without extra ticks or heals; verify full-HP opportunity, terminal damage, last-night recovery-before-recall and persistent Main cases in consumer integration/PlayMode.
- **Pending View owner:** preserve distinct Hunter Mark, effect ordering and all section5/7 requirements. No View test results or manual acceptance have been received by this Core task.
## 9. Final Core acceptance and release gates

Phase F independently reviewed the complete A-E implementation for design omissions, state/ownership boundaries and regression risks. One P2 was found and corrected: movement context equality omitted changed Weakness Exposure, allowing a previously previewed mine10 loss to become13 without invalidating admission. `MovementContextCalculator` now compares the existing maximum-percentage policy for movers and affected targets. Full status publication remains separately validated; duration-only changes at the same effective percentage do not introduce unnecessary admission dependencies.

The two focused cases first ran1/2, then passed in fresh full `F-core-01` **4,269/4,269, exit0**. Fresh `F-integration-01` **404/405, exit2** has only the unchanged StageKey2101 production failure; both suites have zero skips. All three production asset and five retired-authoring cases pass. Earlier C/D/E counts above are historical stage evidence, not the final totals. No log suppression or unrelated Stage/growth repair occurred; native growth serialization diagnostics remain unresolved. Exact current-source hashes and phase coverage are in plan section5.8 and local F evidence.

No further actionable in-scope design gap or ownership violation was identified after the fix. This is a bounded source/contract/test judgment, not proof of every combination or release readiness. The actual state owner, producer-specific triggers, immutable read projections, existing health/turn settlement and Buffer prepare/install boundaries remain separate. No new Framework service, duplicate timer or transaction was introduced.

- **Pending View owner:** register and apply complete `CombatTraitStateEvent` facts at the correct playback boundary. Current unregistered events are skipped; final refresh alone does not establish correct intermediate status/cadence playback.
- **Pending View owner:** finish effect-keyed HUD/broken-shield lookup, per-source duration detail, cards for configured values, Intimidation/discount text/registration cleanup, and Hunter Mark separation described above.
- **Pending View owner:** verify cancel/refresh/rebind without duplicate lifetime ticks, both reserve returns, source removal, capped/terminal/last-night recovery and persistent Main using the listed deterministic fixtures.
- **Pending View owner:** provide consumer integration, scene, PlayMode and manual visual evidence. No such results were supplied to this Core task; whole-game integration/visual acceptance is not complete.

StageKey2101 and native growth serialization remain with their production-content owners. External delivery is **not delivered**; this file is prepared for the user to share. Commit/push remains user-owned. Do not treat this handoff as authorization to repair unrelated content or modify another worker's area.