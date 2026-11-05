# Phase D — Rest Site and Mercenary Office

Completed on 2026-11-02. Scope: Phase D only of [the implementation plan](../plans/2026-11-01-run-gold-facilities-and-trait-ranks-implementation-plan.md). Phases 0–C were already present as uncommitted work at f99d2ca4394158f24e5a16e3a253ea3d9367050b. Phase D preserved those changes. No commits, pushes or external messages were made.

## Result

The active Rest Site offers option 2 (5 gold, 30% heal) and new option 4 (10 gold, +10 persistent maximum HP). At 40/60, they produce 58/60 and 40/70. At 60/60, healing is unavailable and maximum-HP growth produces 60/70. Growth leaves current HP, soul count, attack, defense and owned trait/rank records unchanged. The effective maximum still uses the persistent Main record plus the existing soul rule. Both effects share the existing one-purchase allowance, receipt and Run spending owner. Repeating the accepted option returns AlreadyApplied without another charge; a different option is rejected. Completion remains available without purchase.

The active Mercenary Office now has no item candidates. Its existing ShotGun candidate, 600-gold recruit price, weights, offer cap, eligibility and one-purchase limit are unchanged. Existing general item contracts, item definitions and historic receipts remain valid. No Shop service or planner code was changed.

## Owning changes and contracts

- Core Commands/Common: RecoveryOptionSpec now distinguishes Heal (0) from MaxHealthGrowth (1). Old heal constructors remain supported. Heal is null for the growth variant; callers inspect EffectKind before reading it. MaxHealthIncrease is independently authored.
- RecoveryOptionEvaluator and CharacterRecoveryCalculator reuse the existing heal rounding/cap. Growth changes only MainCharacterPersistentState.PersistentMaxHealth through the existing immutable With path. The decision validates the exact effect-specific deltas and rejects unrelated character/owned changes.
- RecoveryOptionState and RecoveryOptionSnapshot expose ProjectedMaxHealthIncrease. RecoveryReceiptSnapshot and CurrentRecoveryData expose the actual MaxHealthIncrease. Appended optional constructor arguments default to zero for legacy callers. RecoveryStateSnapshot validates receipt/effect consistency.
- RecoveryDefinitionAdapter converts the new SO effect to the common spec. RecoveryStateProjection and PreparedRecoveryMutation capture roster revision alongside Main identity/state, souls, wallet, visit and node facts. StageServiceTransactions verifies effect kind and amount against the installed option before preparing the common character/currency/receipt mutation. The existing currency prepare check is repeated before the first write to prevent a changed RunSpent value from causing a partial transaction.
- SO inputs: RecoveryOptionDataSO and RecoveryOptionData add the effect kind and positive growth amount. The active InGameGroup/Recovery table registers new Recovery/Item/RecoveryOption_4.asset. Stage/Event/Event_Recovery.asset references only options 2 and 4. Existing options 1 and 3 remain registered for other references and history; they are no longer offered here. Stage/Shop/Shop_Default.asset changes only its items list to empty. Existing GUIDs remain intact.

The detailed Phase-D-only file list and before/current diff are in .superpowers/sdd/2026-11-01-run-gold-facilities-and-trait-ranks-implementation-plan/phase-d-changes.json and phase-d-review.diff. The before files include earlier sessions' uncommitted changes; HEAD alone is not this phase's baseline.

## Change independence

Before D, recovery decisions and receipts admitted positive healing with an unchanged persistent maximum. D adds one effect branch to that same calculation and publication path. No new facility service, mutable state copy, registration or Framework/Job owner was introduced. Later rest prices and amounts are SO edits; understanding trait ranks, combat movement, shop internals or other facilities is unnecessary for those tuning edits. Maximum/current HP, payment and receipt remain necessarily verified together at the existing Buffer transaction. The Mercenary Office behavior is a data-only change with no new unit acquisition rules.

## Verification

The supplied Unity 6000.0.73f1 runner used approved AppData access, one Editor at a time. Initial relevant Core baseline:244/244 passed. New growth assertions first failed against the heal-only path. Fixture and active-catalog tests reproduced missing effect publication/content support before the changes.

Final full Core:4598/4599 passed; CoreIntegration:763/766 passed; zero skips in both. Relevant recovery/shop Core cases selected from the final XML:264/264. Focused Phase D integration:31/31 passed, zero skips. Tests exercise full/injured Main, flooring/capping, insufficient funds, unchanged souls/stats, effect-specific receipts, duplicate command numbers, stale soul/roster/Main/wallet facts, altered option kind/amount, failed result publication and RunSpent overflow before any write. Production content tests check the active table/event references, both rest visits, and a hire using the production shop/unit definitions with an eligible controlled roster.

Evidence under .superpowers/position-swap-recon-undo/:

- gold-d-core-verified-20261102-000800395.xml (matching log and exit file).
- gold-d-integration-verified-20261102-000912216.xml (matching log and exit file).
- The gold-d-review-fixed XML records the focused31/31 run.

All four remaining failures match Phase C by both name and message (automated comparison found zero differences):

1. Core.TraitActivationOutcomeTests.Taunt_OnlyAnAppliedForcedDecisionActivates(True): expected1, observed2.
2. CoreIntegration.CombatCameraZoomPoseTests.PostProcessingExtensionIsAllowedButPoseWriterIsRejected(Cinemachine.CinemachineCameraOffset,False): TypeLoadException.
3. CoreIntegration.DataBaseAssetTests.CategoryDefaultBattle_ProductionMainAndStagePrepareSpecializedTraits(Rider,Leap): initial placement conflicts with board bounds or occupancy.
4. The same production placement case for Heavy,SteadfastWill.

A fresh read-only Phase D review found one Important edge: at the integer maximum, projection of an unselected growth option blocked a new visit's state, heal and exit. Three service regressions reproduced it. Growth beyond representable capacity now projects zero/NoEffect for an open visit, so valid healing and completion still work; an actual growth purchase remains rejected before writes. A real second visit after a cap-reaching purchase is covered. The fix passed the focused and full regressions. No deferred Phase D review findings.

One hundred and one other modified paths were compared against the phase-start snapshots and remained unchanged. Unity-generated FMOD local-path and dynamic-font-cache changes were backed up and restored.

## View and content follow-up

Display names are Rest Site and Mercenary Office. View needs a visual/text entry for recovery option4 and should display ProjectedMaxHealthIncrease / receipt.MaxHealthIncrease separately from healing. No View UI, visual SO, scene or PlayMode acceptance was implemented or claimed.

The production shop still authors only ShotGun, and the current production starting roster already owns ShotGun. Existing uniqueness rules can therefore produce an empty office. Candidate expansion or loadout changes are out of this phase and were not hidden by relaxing eligibility. The positive hiring test uses the same production stage/unit data with a controlled Run that does not own the candidate.

Phase E training remains unstarted here. Production training prices remain undecided. Stop for review before E.
