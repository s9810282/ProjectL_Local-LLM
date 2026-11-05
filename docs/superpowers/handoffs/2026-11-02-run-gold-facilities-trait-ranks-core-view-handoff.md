# Run Gold Facilities and Trait Ranks: Core-to-View Handoff

Date: 2026-11-02. Scope: the approved Phases 0–F. This is a local handoff; it has not been sent to another worker.

## Delivery and activation boundary

Core acquisition, ranks, combat effects, rest and recruitment are implemented. Training purchases are verified with fixture prices **10 / 20**, which are not balance decisions. The owner approved making the existing stage SO training-only on 2026-11-02: the `training` toggle and legacy `candidates` input are removed. Production prices remain **0 / 0**, meaning not configured. Entering approved prices in `price1To2` and `price2To3` configures training automatically; no mode checkbox or Core code change is required. Reauthoring applies to the next visit; an active visit retains its published definition and prices.

The default ordinary shop has no item candidates. Its existing ShotGun candidate, 600-gold price and eligibility rules remain. A Run already owning that candidate can see an empty office. Do not bypass uniqueness or add candidates as part of a UI adaptation.

Reference: [approved design](../specs/2026-11-01-run-gold-facilities-and-trait-ranks-core-design.md), [implementation/evidence plan](../plans/2026-11-01-run-gold-facilities-and-trait-ranks-implementation-plan.md), [rest/recruitment result](2026-11-02-run-gold-phase-d-result.md).

## Configure the production Training Ground prices

The View/content worker can perform this authoring step together with the screen/input integration. No additional Core code change or separate Core unlock is required. Editing this InGame SO in the Editor is content authoring; runtime View code must continue to use the published Buffer contracts.

1. Obtain the approved prices for both rank transitions. The test values **10 / 20** are fixtures only; do not copy them into production without a balance decision.
2. In Unity's Project window, select `Assets/ProjectL/DataBaseSO/InGameGroup/Stage/Enhancement/Enhancement_Default.asset`. Its existing `EnhancementStageData` script now defines only training. The asset, script GUID and stage1202 reference are preserved. If a different stage detail is assigned to the intended node, configure that assigned asset instead; editing an unused copy does not configure the node.
3. Fill both price fields in the Inspector. There is **no Training checkbox** and no legacy candidate list.

   | Serialized field | Meaning | Required condition for a usable visit |
   |---|---|---|
   | `price1To2` | Run gold for rank 1 to rank 2 | `price1To2 > 0` |
   | `price2To3` | Run gold for rank 2 to rank 3 | `price2To3 > price1To2` |

   Both fields at zero represent **prices not configured**, allowing the catalog to load while new training entry returns `ChapterProgressionFailureReason.EnhancementShopUnavailable`. They do not allow free training. A partly filled pair, negative value, or positive equal/decreasing pair throws during definition construction. Every configured `TrainingPrices` object enforces the two conditions above. Core builds choices from owned characters and trainable trait definitions, rather than an authored product list.
4. Save the asset. Start a fresh runtime/database session and enter a new visit to verify the authored definition. An already-open visit keeps its captured prices and definition, including if both authored prices are later cleared to zero. Re-run `DataBaseAssetTests` after changing the production asset.
5. Verify that the published visit has `CurrentShopData.VisitKind == ShopVisitKind.Training`, displays the published `TrainingChoices` and prices, and allows completion without buying. Confirm exact payment and receipt on a purchase. Finish the View/scene manual acceptance listed below.

The previous instructions to enable `training` are superseded by this owner-approved cleanup. The SO no longer selects between old enhancement and training formats. Restored old purchase snapshots are still rejected at the existing purchase/transaction boundaries. Production balance is still undecided; this cleanup does not assign prices or claim production gameplay acceptance.

## State ownership and refresh

Use `IBufferQuery` read sinks for rules/results and ViewGroup tables for names, icons and localized descriptions. Submit intent to existing input buffers and let the existing Lifecycle/Job execution process it. Do not mutate CharacterBuffer, currency, visit state or rank history from View, or read InGame SOs to recreate rules.

- Run money: `CurrencyReadSink.Amount` and `RunSpent`. Combat StageGold is separate; purchases must not use it.
- Identity: `CharacterInstanceId` / `OwnedId` identifies an owned card; `EntityKey` identifies its definition/visuals. Recapture identities after a fresh Run.
- Roster: request publication with `IBufferQuery.CharacterStatsRequestSink.RequestRefresh()` on open and after a handled change. The existing `CharacterPresentationPublisher` calculates on Core Tick. Read `IBufferQuery.CharacterStatsReadSink.ReadOwned()`; handle `Status == Pending` while publication is outstanding and `BattleActive` with no owned items while battle is retained. A Ready result exposes `Items` of `OwnedCharacterStatsSnapshot`: `OwnedId`, `CharacterKey`, `Level`, `SoulCount`, `Stats`, `TraitRanks` and existing typed trait descriptions. Core.CharacterStats is the internal calculation source, not the View display dependency. Do not calculate in a View read or poll-refresh every frame.
- `OwnedCharacterRecord.Growth.Selections` is level-choice history. `Growth.Purchases` stores per-trait training counts. Neither collection length is the effective rank. Use `TraitRanks[traitKey]` for native, default, acquired and compatible historic Enhance sources.
- After a purchase/result: refresh Run money, affected owned card, rank labels, receipt and visit allowance together. After rest also refresh current/max HP. After recruit refresh the owned list and deck editor; recruitment does not automatically equip the card. Rebuild from the new Run after reset and discard old context/offer/rank selections.

The shared owned-spec resolver supplies both roster descriptions and battle initialization. View must not apply training bonuses a second time. Combat consumers continue to use current combat snapshots/results for remaining shields, status duration and damage. The balance table and expiration rules are in design §4; the Phase C record documents swap shields, Taunt and Ground Compaction lifetime verification.

## Training: existing shop transport, separate choices

Entry remains `Core.Shop.EnterShop(chapterKey, targetNodeId, seed)`. Configured training definitions use the existing shop flow and publish `CurrentProgressReadSink.Shop`:

| Public member | Meaning |
|---|---|
| `CurrentShopData.VisitKind` | Distinguishes `ShopVisitKind.Training` from ordinary shopping |
| `EntryId` | Identity of this visit, not just a node key |
| `TrainingRosterRevision` | Revision captured for this training visit |
| `TrainingChoices` | All eligible owned-card/trait pairs, including Main and off-deck Subs |
| `TrainingReceipt` | Null before purchase; accepted `TrainingReceiptSnapshot` afterward |
| `HasPurchase` | The shared single-purchase allowance, including a training receipt |
| `Offers` | Existing ordinary offers; not the training choice list |

Each `TrainingChoiceSnapshot` carries `OwnedId`, `TraitKey`, `CurrentRank`, `NextRank`, `Price`, `CanPurchase`. Render the published list without truncating to the ordinary three-card limit. Selection needs both owned identity and trait key. Rank 3, absent traits and nontrainable definitions are excluded. Empty and unaffordable lists are valid visits.

Construct `ShopVisitContext(chapterKey, nodeId, stageKey, shop.EntryId)` from the active selection and visit. Submit:

`ShopViewCommandData.PurchaseTraining(context, ownedId, traitKey, expectedRank)`

through `ShopViewWriteSink.Submit`. `expectedRank` is the displayed `CurrentRank`; it is not the requested final rank. The existing `ShopCommandJob` calls `Core.StageCommands.ProcessNextShopCommand(number)`, which reaches `IShopService.PurchaseTraining`. Keep the normal request lifetime and result-consumption pattern.

Read `StageCommandResults.TryGetShopResult(out ShopCommandResult result)` and match `RequestNumber`, `Context`, `Kind`, `OwnedId`, `TraitKey` and `ExpectedRank`. For `PurchaseTraining`, `Accepted` reflects `PurchaseResult.Succeeded`. It does not establish node completion. The executor publishes the purchase result first, then attempts completion; leave only when `CompletionResult?.Accepted == true`. If purchase succeeded and completion is absent/rejected, retain the receipt and retry **Complete**, never the payment.

The accepted financial receipt is both `PurchaseResult.Training` and `CurrentShopData.TrainingReceipt`: `OwnedId`, `TraitKey`, `PreviousRank`, `FinalRank`, `Price`. A repeated processed request number does not charge again. Dispose/remove commands through the existing View lifetime after consuming their result.

Relevant `ShopFailureReason` values include `InsufficientFunds`, `PurchaseAlreadyUsed`, `TargetNotOwned`, `TraitNotTrainable`, `RankLimitReached`, `StaleRank`, `StaleRoster`, `EntryMismatch`, `StaleEntry`, `RunMismatch`, `WrongPhase` and `RunEnded`. Local argument errors and invariant exceptions remain exceptions; do not show them as a successful purchase. Re-read authoritative state after rejection rather than adjusting balances/ranks optimistically.

Rank preview is currently the published **current rank → next rank and exact price**. Current effective values are available through owned stats and existing typed descriptions. There is no generic public next-rank numeric-effect preview payload on `TrainingChoiceSnapshot`. If UI requires one, request a Core projection contract; do not query InGame definitions or duplicate tuning formulas in View.

## Optional purchase and explicit completion

Training and recruitment may be left without buying, even when a choice is affordable. Submit `ShopViewCommandData.Complete(context)` through the same shop command path; Core calls `CompleteShop`. Completion clears that node without granting a purchase or spending gold. Do not auto-buy a sole remaining option, treat an empty list as a fault, or require an affordable selection before allowing exit.

Ordinary recruitment remains `ShopViewCommandData.Purchase(context, offerId)` using the published `ShopPurchaseSnapshot`. It shares the one-purchase visit allowance and purchase-then-completion result contract. Training uses no fabricated `ShopOfferId` or ordinary product slot.

## Rest: effect-specific preview and receipt

Enter through `Core.Recovery.EnterRecovery(chapterKey, targetNodeId)`. Read `Core.RecoveryState.TryGetCurrent(out RecoveryStateSnapshot state)` (also exposed through the existing recovery state read service). Use `state.Context` in `RecoveryCommandData.Select(context, optionId)` or `RecoveryCommandData.Complete(context)`, submitted to `RecoveryViewWriteSink`. `RecoveryCommandJob` processes the numbered request; `RecoveryResultReadSink.TryGetLatest` returns `RecoveryProcessedResult`. Acknowledge and remove it using the existing recovery request lifetime.

| Production option | Published rule | View source |
|---|---|---|
| 2 | 5 Run gold, heal 30% of effective maximum, clamped to missing HP | `EffectKind == Heal`, `ProjectedHealAmount` |
| 4 | 10 Run gold, persistent maximum HP +10, current HP unchanged | `EffectKind == MaxHealthGrowth`, `ProjectedMaxHealthIncrease` |

Read `RecoveryOptionState.Option` for `Id`, `Cost`, `EffectKind`, `MaxHealthIncrease`; `Heal` is null for max-HP growth. Branch on the effect kind before reading heal fields. Use `CanSelect` / `UnavailableReason` and the published projections; do not calculate 30% or effective maximum in View.

Both options share one selection per visit. Full HP blocks ineffective healing, but permits max-HP growth. An unrepresentable growth at the HP cap is `NoEffect`, so healing or completion can still work. Complete is valid without selecting either option.

Consume `RecoveryOperationResult.Succeeded`, `FailureReason`, `AlreadyApplied`, `StateAfter` and `Progression`. A selected result has an open state and receipt; completion has `StateAfter.IsCompleted` plus accepted progression. Use `StateAfter.Receipt.AppliedOptionId`, `SpentAmount`, `HealedAmount`, `MaxHealthIncrease` for exact feedback. `CurrentRecoveryData` retains corresponding `PaidAmount`, `HealedAmount` and `MaxHealthIncrease`. A handled request is not automatically an accepted trade. Relevant rejections include `InsufficientFunds`, `NoEffect`, `AlreadyResolved` and stale/context/phase mismatches.

## Level growth: variable count, mandatory decision

Production categories acquire at step10/level2 and step30/level3. Main specialization merges its approved pools without duplicates. `Core.Characters.ReadTraitGrowthProgress()` reports `Unavailable`, `AwaitingAcknowledgement`, `ChoiceRequired` or `ReadyToContinue`. `PrepareNextTraitGrowthOffer()` provides `ActiveOffer` with `RunId`, `OfferId`, `OwnedId`, `StepId`, `Kind`, `Choices`.

Render exactly **1–3** choices. A single candidate still requires explicit selection. Level acquisition has no skip. Do not reuse optional facility exit semantics here. Each `TraitGrowthChoice` carries `TraitKey`, `Kind`, `Name`, `Description`; use ViewGroup visuals/localized content under the current presentation convention.

Submit `TraitGrowthChoiceRequest(runId, offerId, ownedId, stepId, traitKey)` through `TraitGrowthViewWriteSink.Submit`. Existing `TraitGrowthCommandJob` reaches `Core.TraitGrowthCommands.ProcessNextTraitGrowthCommand(number)`. Match `TraitGrowthResultReadSink.LatestResult` to the request, inspect `Result.Succeeded` / `FailureReason`, then `AcknowledgeResult(runId, number)`. Read progress again for the next offer or continuation. Preserve a frozen offer on rejection; no silent random selection or repeated stat settlement.

## View/content work remaining

Inspection of the current View source found concrete adaptation points; none were edited in this phase:

- `ViewSystem/StageMap/Recovery/RecoveryPopupModelBuilder.cs` currently forwards only `ProjectedHealAmount` to text. Extend its model/text/receipt handling for `EffectKind` and max-HP growth. Add option4 display name, description and icon in the RecoveryOptionVisual table. Audit existing option2 wording against 5 gold/30%.
- `ViewSystem/StageMap/Shop/ShopPopupModelBuilder.cs` iterates `shop.Offers` only. Add a Training presentation/input path using `VisitKind`, `TrainingChoices`, owned-card selection and the training command. Ordinary three-offer rendering cannot show training by itself. Adapt controller/result retry policy and popup capacity/scrolling to the unbounded list.
- `ViewSystem/StageMap/TraitGrowth/` already loops over `offer.Choices.Count`; verify the screen/card layout for one, two and three options and mandatory single-option confirmation. Verify trait visuals for all newly eligible keys, including PositionSwap440 and ReconSquad450.
- Add rank labels, current→next/price copy, success receipts and localized rejection text to the applicable roster/deck/shop screens. Prefer existing ViewGroup content; do not put balance or acquisition logic into label builders. Training availability must not be inferred from a hardcoded trait list.
- Verify StageSelect scene/popup wiring and facility names (“Rest”, “Mercenary Office”, “Training Ground” are handoff labels, not a finalized translation), entry/exit buttons, empty/unaffordable states and fresh-Run refresh. Scene, prefab, FieldCamera, Architecture and View assets were not changed. Production Akimbo's earlier seven-common-trait presentation test payload was removed by owner approval in Phase A; View fixtures should be explicit rather than restoring those traits to production.

Manual acceptance remains with the View owner: show real variable offers; distinguish level choice from optional spending; display max-HP growth without a fake heal; recruit only once; train an off-deck identity; permit an affordable visit to be skipped; recover purchase-success/completion-failure without another charge; carry the rank into battle; refresh/reset the UI on a new Run. Production training acceptance additionally requires approved prices and activation. No PlayMode or visual acceptance is claimed by the Core EditMode results.

## Verification and remaining failures

Fresh Unity 6000.0.73f1 EditMode runs used the approved AppData execution path, one Editor at a time. XML files below have matching logs and exit files under `.superpowers/position-swap-recon-undo/`.

| Verification | Passed / total | Skipped | Evidence |
|---|---:|---:|---|
| Production DataBaseAssetTests | 74 / 76 | 0 | `gold-f-production-assets-20261102-005530177.xml` |
| Full Core | 4598 / 4599 | 0 | `gold-f-core-final-20261102-005627363.xml` |
| Full CoreIntegration | 801 / 804 | 0 | `gold-f-integration-verified-20261102-005946553.xml` |

The new `RunGoldJourneyTests.GrowthRestRecruitTrainingSkipAndBattle_PreserveRunStateUntilFreshRun` passed in the final full integration run. It starts a real Run and acquires an owned card, explicitly seeds reached levels2/3 through the existing settlement store, submits both actual growth choices and acknowledges their results, buys +10 max HP, recruits at600, trains an off-deck acquired ReconSquad at10, skips the next affordable training visit, restores a legal four-card deck and enters battle. Ordered step10/30 history and unrelated recruitment identity remain intact; the published rank becomes2, the combat snapshot has15% exposure, gold1000 becomes380 and RunSpent increases620. Abandon/teardown preserves growth; a fresh Run changes RunId and clears purchases, choices, recruitment, spending and persistent max-HP growth. The View-facing publication request/Tick/read path is checked after training and reset, including BattleActive during battle. The fixture supplies the existing deck timing authority; actual scene interaction timing is a View acceptance item. Earned-XP math is covered by the existing victory-growth tests, not simulated by this journey.

Failure names and messages were compared programmatically against Phase0; differences are zero. The full assemblies retain exactly these four failures:

1. `ProjectL.Tests.Core.TraitActivationOutcomeTests.Taunt_OnlyAnAppliedForcedDecisionActivates(True)`: expected1, actual2.
2. `ProjectL.Tests.CoreIntegration.CombatCameraZoomPoseTests.PostProcessingExtensionIsAllowedButPoseWriterIsRejected("Cinemachine.CinemachineCameraOffset",False)`: TypeLoadException loading CinemachineCameraOffset.
3. `ProjectL.Tests.CoreIntegration.DataBaseAssetTests.CategoryDefaultBattle_ProductionMainAndStagePrepareSpecializedTraits(Rider,Leap)`: initial placement outside board/occupancy.
4. `ProjectL.Tests.CoreIntegration.DataBaseAssetTests.CategoryDefaultBattle_ProductionMainAndStagePrepareSpecializedTraits(Heavy,SteadfastWill)`: the same initial-placement validation error.

The production-only run includes the last two failures; they are not additional failures. Comparison inputs: `gold-baseline-core-20261101-220046323.xml` and `gold-baseline-integration-20261101-220336175.xml`. The machine-readable current results are saved in this plan's local `phase-f-verification.json`. A fresh independent review found no Critical/Important/Minor findings in the reviewed contracts. Final publication-buffer assertions were then added and passed in the final full run. Production training activation and View/manual acceptance remain the explicit boundaries above. No commits or pushes were performed.

## Change independence

Phase F adds one integration journey, a default-preserving optional deck-timing dependency on its existing test runtime, and this handoff/plan record. Product state ownership and public behavior are unchanged in F. Facility balance edits remain in the owning SOs, effect tuning in trait definitions, and purchases in the existing transaction. The journey intentionally verifies the necessary shared contracts across roster, wallet, visit, deck and battle. It does not require each future trait author to change unrelated facility implementations.
