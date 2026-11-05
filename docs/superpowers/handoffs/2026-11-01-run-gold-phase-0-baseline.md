# Phase 0 — Run gold facilities and trait ranks

Date: 2026-11-01. Authorized continuation: Phase A.

## Checkout and verification

Branch: feature/Cheon/Core/NewCombatSytem. HEAD: f99d2ca4394158f24e5a16e3a253ea3d9367050b. Initial changes: only the untracked design and implementation plan named by the owner. Unity: 6000.0.73f1 (a166abc3bf0e). Tests used the supplied runner with AppData access, one Editor at a time.

Core: 4531 total, 4530 passed, 1 failed, 0 skipped. Existing failure: TraitActivationOutcomeTests.Taunt_OnlyAnAppliedForcedDecisionActivates(True), expected 1, actual 2.

CoreIntegration: 656 total, 653 passed, 3 failed, 0 skipped. Existing failures: CombatCameraZoomPoseTests.PostProcessingExtensionIsAllowedButPoseWriterIsRejected("Cinemachine.CinemachineCameraOffset",False), TypeLoadException; DataBaseAssetTests.CategoryDefaultBattle_ProductionMainAndStagePrepareSpecializedTraits for Rider/Leap and Heavy/SteadfastWill, initial placement conflicts with board bounds or occupancy. BossStagePreparationTests no longer reports StageKey 2101 failure in this run. These are baseline observations, not fixes or successful full-suite claims.

Evidence: `.superpowers/position-swap-recon-undo/gold-baseline-core-20261101-220046323.{xml,log,exit.txt}` and `gold-baseline-integration-20261101-220336175.{xml,log,exit.txt}`.

## Active data and compatibility inventory

DatabaseCatalogSO → InGameGroup (d6768fc9b2be498f8368ab9578fbad8e) → CategoryTraitProgressionTable (5866ce892b684126b51485dba527a332) and TraitGrowthDefinitionTable (820a2d5e80c241fe9f78348db26d8616).

All four categories have step 10, level 2, Acquire (serialized value 1). Enhance retains serialized value 2. Production asset history: 24c21b3a3 had empty schedules; 6b0755be2 introduced step 10 Acquire with broader pools (22 entries for Cannon; Main also carried the subsequently retired cost traits); 4e0fc4a51 reduced the pools. No shipped Enhance step was found in category asset history. Do not reinterpret an unknown ID as Enhance. Existing test schemas explicitly establish step 20 Enhance; new production level-3 step 30 does not reuse it.

### CategoryTraitProgression_Cannon.asset

GUID: 78f793129fba468c856cf63a7b247c63. steps:;   - stepId: 10;     unlockLevel: 2;     kind: 1.

SoloAction, CallOfBlood, Disarm, AnkleBreaker, Sacrifice, Taunt, SteadfastWill, GroundCompaction, DustScreen, Leap, StationaryFire, FinishingShot, FocusedBombardment.

### CategoryTraitProgression_Heavy.asset

GUID: 7bb8ec50e9254f8ba49cf1207b2f162b. steps:;   - stepId: 10;     unlockLevel: 2;     kind: 1.

Knockback, SoloAction, CallOfBlood, Disarm, AnkleBreaker, Sacrifice, Taunt, SteadfastWill, GroundCompaction, DustScreen, Leap, StationaryFire, FinishingShot, FocusedBombardment.

### CategoryTraitProgression_Main.asset

GUID: 6c6219f7ec754eae83de00cd1d3db183. steps:;   - stepId: 10;     unlockLevel: 2;     kind: 1.

Knockback, SoloAction, CallOfBlood, Disarm, AnkleBreaker, Sacrifice, Taunt, SteadfastWill, GroundCompaction, DustScreen, Leap, StationaryFire, FinishingShot, FocusedBombardment.

### CategoryTraitProgression_Rider.asset

GUID: a45fb7d388c64a7688041d0d9e874bd9. steps:;   - stepId: 10;     unlockLevel: 2;     kind: 1.

SoloAction, CallOfBlood, Disarm, AnkleBreaker, Sacrifice, Taunt, SteadfastWill, GroundCompaction, DustScreen, Leap, StationaryFire, FinishingShot, FocusedBombardment.

Current membership is authored only in pool. Historical membership can be frozen separately as a prior schema on the same category SO for replay, never for future offers. A2 is pending: no production pool or schema has been changed. When authored, include proven still-supported entries from the broader pre-4e0fc4a51 pools, not only the immediate 13/14-entry snapshot; retired cost traits remain retired. Main/Heavy retain Knockback; Rider/Cannon do not gain it. Rider default is Leap; Heavy default is SteadfastWill. Specialized Main unions its own and selected category pools and deduplicates by key. Akimbo native Morale Boost is 20%; native Clean Sweep duration is 3, unlike the requested future rank-1 values. Phase A preserves this tuning; B/C own its replacement.

Existing normal asset definitions missing from the active table: TraitGrowth_Acceleration.asset, TraitGrowth_ArmorBreaker.asset, TraitGrowth_CleanSweep.asset, TraitGrowth_Cohesion.asset, TraitGrowth_Entrenchment.asset, TraitGrowth_Hunter.asset, TraitGrowth_MoraleBoost.asset, TraitGrowth_TrackingShot.asset, TraitGrowth_Vanguard.asset. Position Swap (440) and Recon Squad (450) already have typed Core/Data authoring and combat rules, but lack growth assets and registrations. All 17 training rank definitions are absent: current schema has normal and optional legacy enhanced only. Missing rank resolution and paid growth belong to B; swap shields, source-aware Taunt reduction and next-originator-turn-end compaction timing belong to C.

| Existing asset | Current typed values |
|---|---|
| Acceleration | ;         attackPerMovedCell: 1; |
| AnkleBreaker | ;         durationAffectedSideTurnEnds: 1; |
| ArmorBreaker | ;         defenseReductionPercentPerHit: 10; |
| CallOfBlood | ;         enemyMaxHealthHealPercent: 10; |
| CleanSweep | ;         attackBonusPerAllyAttack: 1;         durationControllerTurnEnds: 1; |
| Cohesion | ;         defensePerAdjacentAlly: 1;         shieldAmountPerAlly: 1;         radiusCells: 1;         resetEveryControllerTurnEnds: 1; |
| Disarm | ;         durationAffectedSideTurnEnds: 1; |
| DustScreen | ;         attackRangeReduction: 1;         attackDamageReductionPercent: 20;         opposingTurnEndsToExpire: 1; |
| Entrenchment | ;         radiusCells: 1;         damageReductionPercent: 50; |
| FinishingShot | ;         attackThresholdPercent: 20; |
| FocusedBombardment | ;         requiredEnemyCount: 1;         extraDamagePercent: 50; |
| GroundCompaction | ;         movementRangeBonus: 1;         opposingTurnEndsToExpire: 1; |
| Hunter | ;         attackPerTargetKill: 1;         maxRewardsPerStage: 3; |
| Knockback | ;         distanceCells: 1;     - rid: 1001;       type: {class: KnockbackTraitAuthoring, ns: ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait, asm: ProjectL.Systems}; ;         distanceCells: 3; |
| Leap | {}; |
| MoraleBoost | ;         defeatedEnemyBaseAttackPercent: 20;         durationControllerTurnEnds: 1; |
| Sacrifice | ;         radiusCells: 2;         maxTriggersPerEnemyTurn: 1; |
| SoloAction | ;         shieldAmount: 10;         radiusCells: 1;         requiredOccupiedNeighborCount: 0;         resetEveryControllerTurnEnds: 1; |
| StationaryFire | ;         extraDamagePercent: 100; |
| SteadfastWill | {}; |
| Taunt | ;         radiusCells: 2; |
| TrackingShot | ;         targetHealthThresholdPercent: 50;         extraDamagePercent: 20; |
| Vanguard | ;         attackBonus: 1;         durationControllerTurnEnds: 1; |

## Facilities and state boundaries

Active recovery table: InGameGroup/Recovery/RecoveryOptionDataTable.asset, GUID 6550147f8b6b4fef823413ee7389c120. Event_Recovery references 7de3a63a4d9c4e0f9eba92ac29cf08e4, e33851a5dbc049d9ab187a159f1209d2 and 270b926b8f4b41649bc38df02a076a18, resolving under InGameGroup/Recovery/Item. The similarly named Stage/Event/Recovery subtree is not that active table. Active options are flat 10 HP for 2 gold, 30% for 5 gold, and full heal for 10 gold. Ordinary Stage_Shop 1201 uses Shop_Default (3216d793bf9f45da91cd7833bd71770f): one unit candidate, four consumables, recruit price 600. Stage_Enhancement 1202 uses Enhancement_Default (e3f7484242520064e94b6416331db66b), candidates empty and retired entry contract. Training prices remain undecided; production training activation in E/F must wait for both values. No facility content changes are authorized in Phase A.

CharacterBuffer owns new roster creation, prepared growth installation and prepared command replacement. OwnedCharacterRecord → OwnedTraitGrowth → ordered TraitGrowthSelection stores existing Run history. CharacterRosterService.ReadValidatedGrowthCatalog, OwnedUnitSpecResolver.Resolve/BuildProfiles, stat queries and battle preparation replay it. No separate persistent roster restore API was found in the CharacterBuffer/service paths; reconstruction uses the immutable record constructors and prepared replacements. Keep Buffer state ownership and publication unchanged.

## Scope of change

Before: sampler/offer/validator independently require three Acquire choices; history interpretation relies on current positional steps and current pools. After A: one-to-three choices through the same boundaries; one history interpretation helper validates a proven prior schema or current selections, preserving old Acquire/Enhance meanings. Category authors change current pools/schedules in their SO; the frozen schema is compatibility evidence, not a second current membership source. Verification covers growth, actual offer publication/selection, specialization and DB initialization. Unrelated camera, AI activation, combat placement and facility rules remain outside implementation scope.

## Phase A2 implementation — 2026-11-01

The owner subsequently authorized changing the shared SO assets directly and will coordinate with View workers. This supersedes the temporary A2 decision hold recorded in the plan. Akimbo's trait block was restored to the proven pre-presentation-test KillChainAttack definition from 4c88d2900^; its current maxHealth 5 and every other unit field were preserved. The replaced test setup had Clean Sweep, Acceleration and the other common-trait presentation fixtures. View workers must account for the changed default Akimbo trait set when reproducing those visual tests. No messages were sent externally.

All four category schedules preserve step10 at level2 and add step30 at level3, both Acquire. Current pools implement the approved common/exclusive matrix: Main8, Heavy14, Rider12, Cannon13; Knockback remains only Main/Heavy. Prior still-supported pools from 4e0fc4a51^ (23/23/22/22) are frozen for valid historical replay, with only original step10. No production Enhance ID was invented. The 25-definition active table now includes the nine previously unregistered normal assets plus new typed Position Swap440 and Recon Squad450. New assets include matching metadata and explicit null SerializeReference entries consistent with the existing project format. Existing amounts, rank definitions and facility prices remain outside Phase A.

Fresh A2 acceptance:188/188 passed, zero skips. Production tests exercise both sequential selections after a level jump, reuse of an unchosen presented candidate, native/default/prior ownership exclusion, all specialized Main unions, and historical Cannon Taunt/Acceleration retention without reopening illegal current offers. Full regressions and independent A2 review are recorded in the plan after completion.
