# Legacy Enhancement Retirement — R0–R3 Core Handoff

Prepared: 2026-10-29. **Prepared; not externally delivered or accepted.**

R0–R3 of the [retirement plan](../plans/2026-10-29-legacy-enhancement-retirement-plan.md) complete Core retirement. This supersedes the phase-local [R1 View note](2026-10-29-legacy-enhancement-retirement-r1-view-handoff.md) for current status. The [original trait F1 handoff](2026-10-28-trait-system-core-view-handoff.md) retains the common playback contract and now identifies the surviving producers. Its older 37-producer evidence remains historical.

View asset cleanup, new production SO/pool activation, View/Flow integration and PlayMode/manual acceptance remain pending their owners. [R4 combat cost removal](../plans/2026-10-29-legacy-enhancement-retirement-plan.md#6-phase-r4--remove-combat-costs-retain-distance-and-action-use) and [Main specialization](../plans/2026-10-29-main-specialization-plan.md) are separate unimplemented phases. Current costs/action contracts have not been removed. No commit, push or external message occurred.

## 1. Accepted source and fresh evidence

Workspace: `D:/My_ProjectWork/Github_desktop/Project_L`; branch `feature/Cheon/Core/NewCombatSytem`; HEAD `7f4e1d9dcaa5d5248a635d23c7ad9fcd5b1789fb` **plus uncommitted R0–R3 changes**. HEAD alone does not contain this delivery. The feature checkout was retained to preserve current inputs.

The final 6,948-file source/asset/meta/assembly manifest is `.superpowers/sdd/2026-10-29-legacy-enhancement-retirement-plan/final-source-manifest.json`, SHA256 `aa89588a38f01451ad8dd3ea8a91be9b36558738e79d3221aa45d7af7d1a5c02`. R3 changes one existing test file only, adding two equivalent-ratio characterization cases. No runtime source, public contract, DB asset/meta, View, Flow, Framework, UI, scene, prefab, FieldCamera or Architecture change occurs in R3. All 682 DB files and the five unrelated initial dirty inputs retain their R3-start bytes.

| Gate | Passed / total | Failed | Skipped | Evidence |
|---|---:|---:|---:|---|
| R0 fresh Core baseline | 4,154 / 4,154 | 0 | 0 | `retire-r0-core.xml` |
| R0 fresh CoreIntegration baseline | 337 / 338 | 1 | 0 | `retire-r0-integration.xml` |
| R3 final Core | **4,089 / 4,089** | 0 | 0 | `retire-final-core.xml` |
| R3 final CoreIntegration | **344 / 345** | 1 | 0 | `retire-final-integration.xml` |

Final gates ran sequentially through Unity 6000.0.73f1 with approved AppData access. XML lives under `.superpowers/trait-system/`. Final Core XML SHA256: `9344cdbb4efa139836bfc81b964e02fa2d41a1587c2ee7869d48109904913c15`; integration XML: `a61b6319e02bdfc02de325801370649af98d75b69eda26e5459d3eeba9138c79`. R2 had 4,087 Core cases; R3 adds exactly two passing ratio cases and removes none. Integration's 345 fullnames are unchanged from R1/R2.

**The integration suite is not all green.** The sole failure is unchanged from R0 by exact fullname and message:

`ProjectL.Tests.CoreIntegration.BossStagePreparationTests.ProductionBossStage_PreparesAndPlacesBoss`

`System.Collections.Generic.KeyNotFoundException : Stage data not found. StageKey: 2101.`

Its stack reaches `StageTable.GetStage` through `BattleDefinitionAdapter.Read` and `DatabaseBattleInitializationSource.Read`, before battle preparation. `Stage_Boss.asset` still has key2101/GUID `d722b1ff04d4492ca5f9eb97142e2f99`; `StageRootDataTable.asset` does not register that GUID. Both assets and the failing test remain unchanged. This is the existing catalog omission, not a changed retirement failure or a repaired defect. No new failure or skip was accepted.

The `.superpowers/` manifests, frozen originals, source overlays and XML are ignored local evidence; they are **not automatically included** in a future commit or external delivery. Include the required evidence bundle when delivery is authorized. Core gates do not establish View playback or manual acceptance.

## 2. Current support and serialized reservations

**31 executable identities = all 22 scoped new traits + nine surviving legacy traits.** `LegacyEnhancementRetirementTests.SurvivingRegistry_PreservesAllNewAndDeferredIdentities` checks the exact 31/22/5 registry counts; existing per-trait registry/codec/rule and integration cases remain in the final gates.

| Group | Identities |
|---|---|
| 22 new, supported in Core | Vanguard, CleanSweep, CallOfBlood, MoraleBoost, Disarm, AnkleBreaker, SoloAction, Cohesion, Entrenchment, Sacrifice, Taunt, SteadfastWill, GroundCompaction, DustScreen, Leap, Acceleration, StationaryFire, ArmorBreaker, FinishingShot, TrackingShot, FocusedBombardment, Hunter |
| Nine surviving legacy producers | PiercingFalloff, Knockback, DistanceDamageScale, KillChainAttack, Roar, Intimidation, ImpactBolt, Pull, HuntersMark |
| Five independent deferred registrations | Focus, MoveAttackDiscount, ThornField, Necromancy, Regeneration |
| Retired Trait reservations | DoorBreaching=130, ContemptForWeak=140, PrecisionShot=150, DoubleShot=160, LuckyBullet=170, ChainExplosion=180 |
| Retired Enhancement product reservations | LuckyBullet=1, DoubleShot=3, DoorBreaching=4, ContemptForWeak=5, ChainExplosion=6, PoisonThorn=7, PrecisionShot=8 |
| Future specialization seam | EnhancementKey.ChangeRider=2 remains reserved; no ChangeRider TraitKey or working specialization shop is introduced |

ThornField=40, DefenseReduction and ThornFieldDescription remain independent; PoisonThorn retirement neither deletes nor enables base ThornField. Frenzy120 is not reused. Position Swap/Recon Squad are not enabled. Intimidation's priority component remains deferred. No serialized event, activation or identity number is reassigned.

`DeferredCombatFeatures.IsTraitRetired` is Core's single six-key retirement policy. Availability returns `IsAvailable=false`, reason `Retired`; default/custom registry construction, active growth catalog validation and DB adaptation reject these six keys early. The final gates pass all six availability cases, all six registry/catalog rejection cases and all six authoring-before-payload-validation cases. Unknown/None behavior remains invalid input.

View consumes the existing immutable `TraitAvailabilitySnapshot` through Buffer. It must not duplicate the policy, read InGameGroup rules or infer executable support from `Enum.IsDefined`. Core support does not mean category production content is enabled.

## 3. Removed implementation and retained contracts

R1 removed 48 dedicated C#/meta pairs for the six traits, four enhancement-product SO/meta pairs and two growth-definition SO/meta pairs. Common owners were updated in place; the table/schema/shop/growth infrastructure remains. R2 proved zero surviving implementations of the three exclusive request capabilities and zero production request constructors before removing five C#/meta pairs: `FollowUpAttackExecutor`, `AttackResolvedEffectContext` (including exclusive `EffectAttributedKill`), `KillChainActivatedEffectContext`, `DisplacementResolvedEffectContext`, `EffectFollowUpRequest` (including exclusive internal `FollowUpDeliveryKind`). Only their matching declarations/evaluators/parent calls and unused internal arguments were pruned. The [R2 caller inventory](../plans/2026-10-29-legacy-enhancement-retirement-r2-hook-inventory.json) preserves all 68 checkpoint reference lines.

Native KillChain repetition, RNG, credited deaths/rewards, remaining counters, terminal cancellation and both Hunter completion calls retain their order and owners. Root/Roar/native KillChain interception, actual-recipient outcomes, damage/contribution/held-defense/shield arithmetic, ordinary knockback, ImpactBolt, arrivals, Pull, growth, publication and acknowledgment remain supported. No general replacement executor, state copy, transaction layer, service or Builder registration was introduced.

**Native KillChain still uses its start activation and `ChildAttack`.** Preserve child-origin/group/creditor routing and Sacrifice reactions. `TraitActivationKind.FollowUpAttack=80` is retained for serialized/public-contract compatibility, but has no current runtime producer; `KillChainAttack=70` remains produced. Constructor acceptance of a reserved kind is not a new empty or targetless child-shot producer. Do not delete shared child/outcome handling because the three old follow-up traits retired.

## 4. Tests and surviving guarantees

The [final obligation ledger](../plans/2026-10-29-legacy-enhancement-retirement-final-obligations.csv) closes the original [R0 ledger](../plans/2026-10-29-legacy-enhancement-retirement-r0-obligations.csv), [R1 dispositions](../plans/2026-10-29-legacy-enhancement-retirement-r1-dispositions.csv) and [R2 dispositions](../plans/2026-10-29-legacy-enhancement-retirement-r2-dispositions.csv) against current passing fullnames. Historical ledgers remain unchanged.

The original baseline has 4,492 cases; the final source has 4,434. There are **110 removed/renamed original fullnames and 52 added final fullnames**. The final ledger has 1,491 rows: 1,378 retained fullnames, 52 retired-only scenarios and 61 retired scenarios with an explicitly limited surviving guarantee. Three rows are R1 intermediate names, so 113 absent ledger names are not 113 removed original cases. Every named current equivalent exists and passes (1,412 distinct evidence fullnames); no final mapping points to the removed R2 hooks.

Retired producer triggers/selection/independent-child behavior have no claimed equivalent. In particular, fixed/random child variants, DoubleShot direction repetition, LuckyBullet union/structure policy and ChainExplosion timing are not represented as native KillChain behavior. Shared damage, reward, action/strike, interception and recovery obligations are stated separately with their actual current assertions.

- Native KillChain retains two credited deaths/20 XP/14 gold/deterministic events and one action; its two real strikes retain recovery40->49 with10, then49->48 with0/damage1. Synthetic independent sibling delivery is not retained.
- Ordinary two-actor cases preserve independent notification counts, Focused/CleanSweep qualification and Acceleration capture boundaries; native repeat strikes remain one action.
- Three surviving factors2 x2 x1.5 retain damage60/18 and ordered deltas10/20/20 or3/6/6. Root Sacrifice keeps target5/guardian940. The two new R3 tests explicitly retain ratio400/100 versus4/1, prefix40 and final18. Old fourth-factor damage240 is retired. Existing copy, half-up, prefix, overflow and cancellation cases remain.
- Armor/post-hit re-evaluation, exact terrain truncation3.5->3 before x2=6, held defenses, shields, ImpactBolt splash70/40/0 and preview/arrival/publication guarantees remain in the final gates.

**Growth-specific check: 84/84 cases pass.** This includes:

| Obligation | Concrete passing evidence |
|---|---|
| Main/Sub common path, immutable identity/level/XP, enhancement and completion | `TraitGrowthChoiceServiceTests.ConfiguredMain_UsesTheCommonChoiceAndCompositionPath(False/True)` |
| Three distinct Acquire choices, ordered history, no fixed count | `TraitGrowthOfferTests.SelectionHistory_AllowsFourAcquisitionsAndTwoEnhancements`, `Sampler_IsDeterministicDistinctSortedAndUsesKindCounts` |
| Defensive copies, SO conversion, same-definition copies isolated by owned ID | `OwnedTraitGrowthTests.History_IsDefensiveOrderedAndRejectsDuplicateSteps`, `TraitGrowthAuthoringTests.SOConversion_CopiesStepsAndConvertsReferencesToKeys`, `TraitGrowthBoundaryIntegrationTests.SameSubCopies_StatsDeckPublicationProfilesAndNextBattleAgree` |
| Receipt barrier, exact acknowledgment, retained failure/rebind and unchanged RNG | `TraitGrowthProgressContractTests.LastSelection_WaitsForAcknowledgement`, `PrepareNextOffer_CannotCrossReceiptBarrier`, `FailureReceipt_RetainsOfferForRebind`, `Reopen_DoesNotConsumeRandomness` |
| Fresh Run/stale request/acknowledgment, stage gate and atomic failure | `TraitGrowthCommandTests.OldRunCommand_CannotAffectNewRun`, `TraitGrowthTransactionTests.OldRunRequest_CannotBePackagedAsCurrentRunSuccess`, `Reset_PreservesAllocationAndRejectsOldRunAcknowledgement`, existing StageGate/Boundary/Transaction suites |
| Ordinary stat growth with/without trait steps; no replay on retry | `TraitGrowthStatCoexistenceTests.StatLevelUps_ArePreservedWithAndWithoutTraitSteps`, `TraitGrowthStatSettlementTests.VictoryGrowth_ChoiceFailureRetryAndAcknowledgementNeverReplayStatSettlement(False/True)` |

## 5. Catalog and dormant shop seam

`LegacyEnhancementRetirementAuthoringTests.ProductionCatalogBuildsWithoutLegacyProducts` builds the **actual** catalog, requires an empty enhancement table, converts/validates `Enhancement_Default.asset` with explicitly empty candidates, and rejects retired growth definitions/pool members. The table/detail remain valid schema objects. No current `DataBaseAssetTests` class exists; this native integration case and the existing authoring suites exercise actual conversion.

`Stage_Enhancement.asset` retains stageKey1202/mapKey1 and its detail GUID; its GUID is `c9722a616f3e3f84992a76d82da812ef`. The View-owned `NodeVisual_Shop_Enhancement.asset` still reserves1202. Both remain byte-identical in R3. **The current production StageRootDataTable does not register this root**, and `StageMap_Chapter1.asset` has enhancement weight0/minimum0; do not claim operational node availability or change registration during retirement. Fixture-backed service tests intentionally supply1202 to verify rejection.

`SubTraitShopRetirementTests` verifies service and direct-transaction purchase rejection without currency/roster/Main/purchase-state changes, enhancement node admission rejection and empty/nonempty legacy entry transaction rejection. `OrdinaryUnitShop_StillEntersAndPurchasesOnce` passes, as do existing ordinary item/unit offer/purchase, frozen-price, inventory, receipt and atomic publication cases. Shared Shop DTOs/query/service/Flow and the Enhancements directory remain. Empty retired content does not authorize deleting shared UI routing or enabling a replacement Main shop.

## 6. View-owned cleanup and production authoring request

**Prepared only; no external message, recipient receipt or acceptance is recorded.** This section specifies owner work, not authorization for this Core task to edit View assets.

| View cleanup target | GUID / observed reference users |
|---|---|
| `Assets/ProjectL/DataBaseSO/ViewGroup/TraitVisual/Item/TraitVisual_DoubleShot.asset` + `.meta` | `e76ae27ba9394163aa6a171e512e3bad`; TraitVisualDataTable row26 |
| `Assets/ProjectL/DataBaseSO/ViewGroup/TraitVisual/Item/TraitVisual_DoorBreaching.asset` + `.meta` | `37538d104da04d798bfe54388ac92022`; TraitVisualDataTable row27 |
| `Assets/ProjectL/DataBaseSO/ViewGroup/TraitVisual/TraitVisualDataTable.asset` | Remove the two corresponding entries with asset/meta cleanup in the same owner checkpoint |

A fresh `Assets` scan of `.asset`, `.prefab` and `.unity` GUID users finds only those two table rows. The owner must recheck references before deleting under its own authorization, compile/validate the actual View catalog and test its consumers. These two SO/meta pairs and table are untouched by Core retirement. Historical producer tests/evidence remain historical, rather than rewritten as if those identities never existed.

**Temporary Heavy configuration, explicitly requested by the user:** `CategoryTraitProgression_Heavy.asset` has empty steps and pool after removing obsolete DoubleShot/DoorBreaching growth definitions. Existing Knockback growth definition remains registered. Ordinary XP/stat growth is preserved; no replacement production numbers were invented. Main/Rider/Cannon were already empty and remain so.

The five new Heavy implementations **Cohesion, Sacrifice, Taunt, Entrenchment and SteadfastWill already exist in Core**. View/content owns valid SO definitions, approved normal/enhanced parameters and a satisfiable category pool/step configuration. Core availability does not fill a production pool. Validate three-choice Acquire and reachable Enhance paths with the existing validator; do not enable incomplete content using fixtures, zero/default/unlimited fallbacks or guessed balancing. Position Swap remains deferred. Production activation and manual playback need separate evidence.

## 7. Ownership, locality and review boundary

R3 coordinated with the source documents through read-only app status: this was the only active Project_L chat; the trait/F1 owning chat's latest turn had completed and it was idle. Three owner documents were frozen and hashed before editing. Current support/status in the trait plan and F1 was amended without concurrent writes. No owner approval, external delivery or View acceptance is inferred from idle status.

Before retirement, changing shared attacks/knockback required tracing six obsolete producers, shared test fixtures and the dead request hooks. After R1/R2, feature rules still own their contributions, existing attack/arrival/lifecycle owners retain necessary ordering, and consumers need the immutable Buffer facts rather than old effect internals. R3 changes only one test owner and current documentation. No state source, Framework contract or public transaction ownership was duplicated; normal wiring changes stayed at existing owners. Necessary shared damage, movement, growth and integration regression was retained. File-count reduction alone is not the improvement claim.

The independent R3 review and final stop are recorded in retirement plan §7.4. Core retirement completion is distinct from the pending View cleanup/delivery/acceptance, F2 values/pools, Flow integration, Main specialization, R4 and PlayMode/manual/device acceptance.
