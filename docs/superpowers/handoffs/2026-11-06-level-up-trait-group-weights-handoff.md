# Level-up Trait Group Weights — Designer and Core-to-View Handoff

Date: 2026-11-06

## Authoring

| Asset path | Fields |
| --- | --- |
| `Assets/ProjectL/DataBaseSO/InGameGroup/TraitGrowth/Item/TraitGrowth_<name>.asset` | `group` is `Common` (1) or `CategorySpecific` (2). Set it explicitly for every growth definition. The default `Unspecified` (0) and undefined values produce a configuration error that identifies the asset. |
| `Assets/ProjectL/DataBaseSO/InGameGroup/TraitGrowth/Item/CategoryTraitProgression_Main.asset` | `commonWeight` and `categoryWeight` |
| `Assets/ProjectL/DataBaseSO/InGameGroup/TraitGrowth/Item/CategoryTraitProgression_Rider.asset` | `commonWeight` and `categoryWeight` |
| `Assets/ProjectL/DataBaseSO/InGameGroup/TraitGrowth/Item/CategoryTraitProgression_Heavy.asset` | `commonWeight` and `categoryWeight` |
| `Assets/ProjectL/DataBaseSO/InGameGroup/TraitGrowth/Item/CategoryTraitProgression_Cannon.asset` | `commonWeight` and `categoryWeight` |

All four production rows start at **1 common to 2 category-specific**. Each weight is a positive integer and their sum must be at most `int.MaxValue`. The `pool` is the category's allowlist. A trait's group controls how it is sampled when eligible. For example, Knockback is common and belongs to the Main and Heavy pools. Moving a trait between groups takes one definition edit. Adding it to a category's eligible pool takes an explicit edit to that category row.

The initial common definitions are Vanguard, CleanSweep, SoloAction, CallOfBlood, MoraleBoost, Disarm, AnkleBreaker, and Knockback. The other 17 migrated definitions are category-specific.

## Offer behavior

- Each acquisition draw picks a group by weight while both groups have eligible candidates. At 1:2, each such draw has a 2/3 chance of selecting the category group. The final mix varies because the sampler then selects a trait uniformly within that group and removes it from the remaining candidates.
- When one group is exhausted, the sampler draws from the other. It presents all remaining candidates when there are one or two in total. The maximum is three distinct choices. The existing ownership and compatibility rules determine eligibility before sampling.
- Main uses its own weights until specialization. After specialization, it keeps the Main step IDs and uses the selected category's weights for the distinct union of the Main and selected pools. Group identity comes from the trait definition even when the same trait appears in both pools.
- New acquisition offers remain deterministic for the same Run ID, owned instance ID, step ID, candidates, groups, and weights. A fixed seed can produce different cards than under the old uniform acquisition algorithm. Legacy enhancement continues to use the old uniform sampler and seed behavior.
- Once an offer is published, valid weight changes apply to later offers. The current offer keeps its identity and choices. Selection still checks the current step and eligibility in addition to membership in the published offer.

## Runtime boundary

The growth table constructors snapshot the SOs into immutable Data. `TraitGrowthDefinitionAdapter` converts those rows into the Core catalog. `EffectiveTraitProgression.Resolve` carries the policy for the current category or specialization. `TraitGrowthRules.Candidates` supplies legal keys and `CharacterRosterService.PrepareNextTraitGrowthOffer` sends only those keys, their group metadata, the weights, and the existing seed to the sampler. `TraitGrowthOffer.MaximumChoices` owns the cap shared by validation and sampling.

`InGameCoreSystemFactory` creates the roster and registers its interface and lifecycle module with the Core builder before its single Build. The Core and InGame groups export their group interfaces. The runtime factory exports InGame to the root builder and `JobRegisterSystem` resolves that group. `StagePlan` passes `CoreGroup.Characters` into `TraitGrowthPrepareJob`, which calls the existing prepare API. The sampler is a pure calculation on passed inputs and uses the existing assembly boundary.

`TraitGrowthTransactions` validates and publishes the offer through `TraitGrowthOfferState.ActiveOffer`. `TraitGrowthPopupController` reads that offer through the existing Buffer sink and submits a `TraitGrowthChoiceRequest` with the published IDs and selected key. `TraitGrowthPopupModelBuilder` renders the published choice order and reads its presentation assets from View tables. This lets the popup continue to use the same DTO and input route. A trait shown by the popup still needs its existing trait visual row.

## Change and verification scope

| Scenario | Before | Actual edit and understanding scope | Verification |
| --- | --- | --- | --- |
| Tune the category ratio | Uniform sampling was fixed in code. | Edit one progression row and understand group probabilities, eligible pools, and DB initialization. | Validate the authored values and check representative distributions with equal and unequal pool sizes. |
| Reclassify a trait | Growth definitions had no explicit group. | Edit one growth definition and understand which pools reference its key. | Verify the mapping, affected offers, and existing eligibility. |
| Change acquisition sampling | Sampling and offer validation had separate cap literals. | Edit the sampler and use its candidate, group, weight, and shared cap contracts. The roster owns input selection and publication. | Check determinism, sorted distinct choices, invalid inputs, exhaustion, and the legacy seed results. |
| Change Main specialization policy | Effective progression composed Main steps and merged keys. | Review effective progression, Main history, and selected-category weights. | Check unspecialized Main and all three specializations, shared keys, and history and publication regressions. |

The implemented scope matches the planned ownership split. The authored group, authored weight pair, offer cap, and active offer each have a single owner. The public sampler and the progression constructor both call the internal `CategoryTraitProgressionSpec.ValidateWeights` guard. A future bounds change has one implementation owner and requires verification at both entry points. The roster resolves effective progression for scheduling, eligibility, and weights through the same resolver and immutable catalog. Integration verification still reaches shared growth consumers including character stats, training, legacy history, and battle preparation.

## Verification evidence

Phase 1 added the authoring contracts and migrated 25 trait definitions and four category rows. Its focused run passed 172 of 179 cases, including all 17 new cases. The seven `DataBaseAssetTests` failures matched the pre-change baseline.

Phase 2 passed all 97 focused Core and CoreIntegration cases. The new publication checks covered weighted acquisition, frozen selection, fresh policy, fallback, compatibility, and Main specialization.

Phase 3 ran both EditMode assemblies in Unity 6000.0.73f1.

| Assembly | Total | Passed | Failed |
| --- | ---: | ---: | ---: |
| `ProjectL.Tests.Core` | 4,910 | 4,885 | 25 |
| `ProjectL.Tests.CoreIntegration` | 917 | 870 | 47 |
| **Total** | **5,827** | **5,755** | **72** |

All 46 cases in the three feature test classes passed. The Main growth and publication, training, legacy enhancement, owned character stats, and battle preparation fixtures were exercised. The 34 fixtures containing failures were run against pre-change HEAD `77db0f19b`. That comparison passed 450 of 522 cases and reproduced all 72 failures with matching case names and messages. The failures include night transition and recall expectations, Taunt activation, production data expectations, deck and combat inspection UI setup, and Cinemachine type lookup. The overall all-green gate remains open.

The run artifacts are `results.xml` and `editor.log` in `C:/Temp/ProjectL-trait-weights-01a1acbd-phase3-full` and `C:/Temp/ProjectL-trait-weights-01a1acbd-phase3-baseline`. The static review confirmed the planned data, eligibility, state ownership, and Framework boundaries. The exact asset diff adds group fields to 25 definitions and two weight fields to each of four rows. Existing YAML values and meta files match the pre-change version.

## Review follow-up

The subsequent code review consolidated the weight guard and added category asset identity and the two authored values to invalid-weight errors. The SO preserves the underlying argument error as its inner exception. Five regression cases reproduced the missing authoring context before the fix. After the fix, all 188 selected authoring, sampler, publication, Main growth, command, and transaction cases passed. Evidence is in `C:/Temp/ProjectL-trait-weights-01a1acbd-phase2-review-green`. The earlier full-suite results remain the evidence for shared-consumer coverage.

## Unity acceptance checklist

1. With Play Mode stopped, change one category's `Common Weight` and `Category Weight` to a contrasting ratio such as 1:9. Save and start from Boot so the database captures the edited values. Trigger a fresh level-up acquisition. Verify that the popup shows one to three distinct cards, accepts a choice, and advances after acknowledgement. Use repeated fresh offers with both groups available when checking the trend.
2. Repeat with Main before and after specialization. Give Main and the selected category different weight ratios and make both groups eligible. Verify that the Main growth flow still advances and that the selected category's ratio governs newly prepared offers. Check the card titles, icons, and choice interaction in the popup.
3. Leave a published popup open through a normal redraw. Verify that the same cards remain available and that the selected trait is awarded once. For the actual policy replacement while an offer is open, run the EditMode tests `OpenOffer_WeightChangePreservesIdentityAndSelection` and `NextOffer_UsesUpdatedWeights` in `TraitGrowthWeightedPublicationTests`. They replace the runtime test row and verify the frozen and subsequent offers. Production SO edits reach the runtime through DB initialization, so an Inspector edit during Play Mode cannot exercise that replacement.
4. Restore temporary tuning values after the manual check. Record the category, ratio, observed choices, and selection result for review.

The Unity checklist is user-owned. The EditMode results cover the automated contracts and publication path. Popup appearance and PlayMode acceptance remain pending.
