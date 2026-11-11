# Level-up Trait Group Weights and Focused Structure Review

Date: 2026-11-06
Status: Implemented and reviewed through Phase 3 on 2026-11-06. All feature cases passed in EditMode. The full Core and CoreIntegration run reproduced 72 pre-change failures. User Unity acceptance remains pending.

## Intent and confirmed decisions

Designers need to make category traits appear more often than common traits in level-up acquisition offers. The user selected group-first sampling and explicitly classified Knockback as common. Review and improvements are limited to this acquisition path, its data contracts, and necessary integration boundaries.

Keep up to three distinct choices, ownership and compatibility filtering, existing progression history, frozen offers, and Buffer ownership. Do not change combat trait effects, training prices/ranks, View behavior, or Framework architecture. Do not commit or push.

## Approaches

1. **Group-first sampling (selected):** choose common versus category-specific using designer weights, then choose uniformly within that group. Candidate counts do not change the group probability while both groups are nonempty.
2. **Per-trait multipliers (rejected):** simpler flat weighted sampling, but large common pools can outweigh the desired category preference.
3. **Guaranteed category slots (not selected):** predictable composition, but changes the request from random preference to a quota.

Use the existing shop sampler's two-group algorithm shape as a reference, without depending on shop types or introducing a generic sampling framework.

## Proposed behavior

- For each slot, filter eligible traits through the existing `TraitGrowthRules` first. Partition only that eligible set by the authored group.
- If both groups contain candidates, choose a group by its relative weight and then choose one trait uniformly within it. Remove that trait before the next draw.
- If one group is empty, use the other. If fewer than three eligible traits remain overall, show all available traits. Zero eligible traits remains an invalid configuration/progression condition.
- Proposed initial values: common **1**, category-specific **2**. This means a 2/3 category-group probability per draw while both groups have candidates; it does not guarantee two category cards or a 2/3 final-card share after a group is exhausted.
- Weights are positive integers. Reject zero, negative values, and a sum exceeding the supported random integer range at the data boundary. Disabling a group is outside this change.
- Preserve stable key ordering, existing Run/owned-instance/step seed generation, and sorted output. The weighted algorithm intentionally changes newly generated offers for a seed; existing published offers remain frozen and are not resampled when settings change.
- Apply group weights to `Acquire`. Preserve the legacy `Enhance` behavior and restored history support; do not expand this task into training changes.

## Authoring and ownership

**Trait classification:** add an explicit common/category-specific group to each `TraitGrowthDefinitionDataSO`, carried through immutable Data and Core definition contracts. Each trait has one classification source, independent of the number of category pools referencing it. Reject unspecified or invalid serialized groups rather than silently interpreting missing data as common.

Proposed migration based on the current production pools: Vanguard, CleanSweep, SoloAction, CallOfBlood, MoraleBoost, Disarm, AnkleBreaker, and Knockback are common. Remaining production growth definitions are category-specific. Knockback's common classification is user-confirmed; the remaining mapping is inferred from the existing pools and is part of this proposal.

**Eligibility:** existing category `pool` lists remain the authoritative allowlists. Common classification does not automatically grant eligibility to every category. In particular, this design preserves Knockback's current Main/Heavy membership. Do not derive classification at runtime from pool intersections, Main membership, enum ranges, or hard-coded trait-key lists.

**Weights:** add `commonWeight` and `categoryWeight` to each existing `CategoryTraitProgressionDataSO`. Carry them through Data, `CategoryTraitProgressionSpec`, and `TraitGrowthDefinitionAdapter`. Designers can tune each category independently without editing code or repeating weights on individual trait definitions. No new DB table or global service is needed.

**Main specialization:** retain Main's progression steps and the distinct union of Main/selected-category pools. A specialized Main uses the selected category's sampling weights; an unspecialized Main uses its own settings. Classification remains trait-owned, so a common trait referenced by both pools is counted once and stays common.

**State:** `TraitGrowthOfferState.ActiveOffer` remains the offer source of truth. `CharacterRosterService` provides eligible candidates and sampling inputs, and publishes through the existing `TraitGrowthOperations`. The sampler stays pure and knows nothing about DB queries, Buffer, Jobs, or UI.

## Pre-implementation findings and approved changes

Paths below are relative to `Assets/ProjectL/Script/Systems/`. This is a static review of the inspected path, not a claim that the entire project satisfies SOLID or all Framework contracts.

| Finding | Current evidence | Decision |
| --- | --- | --- |
| Distribution policy is fixed in code | `RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Roster/TraitGrowthOfferSampler.cs:21-28` uses uniform partial shuffle | Replace the acquisition sampling decision with explicit group inputs and authored weights. |
| Offer maximum is duplicated | Sampler uses `Math.Min(3, ...)`; `Contracts/Roster/TraitGrowthOffer.cs:23` separately validates `<= 3` | Put the unchanged maximum in one Core contract constant and use it at both sites. Do not make card count configurable. |
| Group meaning is absent from the runtime input | `DataBaseSystem/Data/InGame/Table/TraitGrowth/Item/CategoryTraitProgressionDataSO.cs:26` stores a flat list; growth definitions have no classification | Add trait-owned metadata. Keep allowlists separate because availability and group identity are different facts. |
| Specialization currently carries only steps and merged keys | `Application/Roster/EffectiveTraitProgression.cs:33-34` constructs a new progression | Explicitly propagate the selected category's sampling policy when composing effective progression. |
| Existing useful responsibility boundaries | `TraitGrowthRules` determines legal candidates; sampler draws; roster service coordinates; Buffer stores the offer; popup model consumes offer choices | Preserve these boundaries. Do not split services solely to reduce file size. |
| Compatibility checks inspect combat profiles | `TraitGrowthRules.EligibleTraits` validates projected traits with `CombatAttackProfile` and distance coverage | This is a necessary eligibility dependency. Sampling weights must not reimplement it or bypass it. |
| Fixed supported categories and legacy rank rules exist | `CategoryTraitProgressionSpec`, `TraitGrowthCatalog`, and legacy Enhance paths | These are broader domain contracts, not automatically defects. Leave unchanged and record as outside this focused improvement. |

### SSOT, SOLID, and Framework assessment

- **SSOT:** authored classification and weights each have one owner; immutable conversions are boundary representations, not independently mutable sources. Consolidate the duplicated offer maximum. Keep Buffer as the only active-offer owner.
- **SRP:** retain the existing eligibility/sampling/orchestration/state/presentation split. The roster service has acquisition and growth responsibilities, but this sampling change does not require reorganizing its unrelated operations.
- **OCP:** tuning weights and classifying newly authored existing trait types require data edits. Adding an entirely new combat trait still requires its legitimate registration and behavior implementation.
- **LSP:** preserve offer invariants, unavailable-state behavior, seed determinism for identical new inputs, and published-offer identity. No new polymorphic implementation is required; runtime compatibility remains to be verified.
- **ISP:** pure sampling receives only candidates and sampling policy/metadata. Do not give it the DB context or Buffer query interface. Keep existing public service contracts unless implementation evidence requires an approved change.
- **DIP:** Core calculations remain pure; the existing adapter converts authoring data. Do not add a service interface or registration merely for this sampler.
- **FW-001/002/003:** inspected creation route is `InGameCoreSystemFactory` -> `CharacterRosterService.ExportServices` -> the Core group's internal reader -> `InGameCoreSystemGroup` export -> `InGameSystemGroup`. The roster registers its provided interface and module; groups export their group interfaces. `StagePlan` supplies `service.CoreGroup.Characters` to `TraitGrowthPrepareJob`. No new registration is proposed.
- **State boundaries:** publish and selection continue through existing Buffer transactions. No alternate write path, cached offer owner, or View rule calculation is introduced.

## Expected change, understanding, and verification scope

| Change scenario | Before | After (expected, not yet measured) | Required verification |
| --- | --- | --- | --- |
| Tune category preference | Cannot express it in data; must change sampler code | Edit two weights in the affected category SO | Authoring validation and representative distribution check |
| Reclassify a trait | No explicit group contract | Edit the single trait definition | Mapping validation, candidate preservation, and affected sampling |
| Change acquisition sampling algorithm | Sampler plus offer-size constraint and caller | Sampler plus explicit input contract; maximum shared | Determinism, uniqueness, empty-group fallback, offer constraints |
| Specialize Main | Steps and pools are composed | Steps/pools plus explicit selected-category weights | All three specializations, duplicate common keys, history compatibility |

Implementation touches the growth SO/Data/contract/adapter chain, effective progression, sampler, roster sampling call, the offer maximum, and corresponding production assets. It does not require understanding unrelated shop purchasing or combat-effect implementations to tune weights. Shared growth-definition consumers still require integration regression because their construction contracts change.

## Validation and next review point

Plan Core checks with test-owned inputs for group selection independent of pool size, within-group uniform selection, no duplicates, stable results under reordered inputs, fallback after exhaustion, one/two candidates, invalid weights/groups, and unchanged candidate eligibility. Include specialized Main, old growth history, frozen offers, stale requests, and existing Core/CoreIntegration regression. Review production mappings with `DataBaseAssetTests`.

No View tests or combat View MSBuild runs are proposed. UI presentation and selection remain user Unity checks. Any Unity execution must use the approved AppData-access path described in the repository instructions; do not delete caches or upgrade Unity to work around sandbox access.

The [Phase 3 handoff](../handoffs/2026-11-06-level-up-trait-group-weights-handoff.md) records the observed change, understanding, and verification scope alongside the pre-implementation expectations above. It separates automated verification, baseline failures, and the remaining Unity acceptance checks.
