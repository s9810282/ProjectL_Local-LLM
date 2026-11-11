# Level-up Trait Group Weights Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans for inline execution, or superpowers:subagent-driven-development if the user chooses delegation. Steps use checkbox syntax. Execute only the authorized phase and stop at its review gate. Do not commit or push.

**Goal:** Let designers increase category-trait frequency through group weights while preserving level-up eligibility, offer ownership, and existing public behavior outside acquisition sampling.

**Architecture:** Trait definitions own common/category-specific classification; category progression definitions own two weights. Existing eligibility produces legal keys, the pure sampler draws without replacement, and the roster service publishes through the existing Buffer transaction. Main specialization composes existing pools and explicitly selects the specialized category's weights.

**Tech Stack:** Unity 6000.0.73f1, C#, existing ScriptableObject DB pipeline, existing NUnit EditMode Core/CoreIntegration assemblies. No new package or service registration.

**Spec:** [Approved design](../specs/2026-11-06-level-up-trait-group-weights-design.md).

**Status** — Implemented through Phase 2. Phase 3 regression, structural review, and handoff are recorded below. The all-green and user Unity acceptance gates remain open.

## Global constraints

- Keep up to three distinct choices, ownership and compatibility filtering, existing progression history, frozen offers, and Buffer ownership.
- Apply group weights to `Acquire`; preserve legacy `Enhance` sampling and restored history.
- Initial production weights: common **1**, category-specific **2**. Both are positive integers; reject an overflowing sum.
- Knockback is common. Preserve current category allowlists, including Knockback's Main/Heavy membership.
- Specialized Main uses the selected category's weights and Main's progression steps. Unspecialized Main uses Main's settings.
- No View implementation/tests, combat View MSBuild, combat-effect changes, training changes, Framework changes, or commits/pushes.
- Use English implementation documents and Korean code/XML comments; preserve existing paths, GUIDs, public service signatures, and typo identifiers.
- New `.cs` files require `.meta` files. Do not delete old assets or regenerate unrelated metadata.
- Core tests use test-owned inputs. Only explicit production-authoring checks read production assets.
- Existing test assertions must not be weakened. Fixture changes are limited to supplying the new authoring input. Check authorship before repairing a runtime failure in another person's test; follow the repository's permission rule when it applies.
- Any Unity execution must use the approved AppData-access path. Do not delete caches or upgrade Unity to bypass sandbox access. Report compilation as unverified until a fresh run establishes otherwise.

## Review focus

1. A new or old serialized trait with no group must fail with an asset-specific configuration error, not silently become common. Covered in Task 1.
2. Adding common candidates must not dilute the category group's configured probability while both groups remain available. Covered in Task 2.
3. Main specialization must not lose selected weights or duplicate a shared common trait. Covered in Tasks 1 and 3.
4. Changing weights while an offer is open must not replace its cards or identity, and the valid frozen choice must still be accepted. Covered in Task 3.
5. A category group exhausted by ownership/compatibility filtering must fall back without stalling progression; legacy enhancement must remain uniform. Covered in Tasks 2 and 3.

## File and responsibility map

Path aliases below expand to exact repository-relative paths:

| Alias | Path |
| --- | --- |
| `C` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator` |
| `R` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CharacterSystem` |
| `D` | `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/TraitGrowth/Item` |
| `A` | `Assets/ProjectL/DataBaseSO/InGameGroup/TraitGrowth/Item` |
| `T` | `Assets/Tests/EditMode` |
| `I` | `Assets/Tests/EditMode/CoreIntegration` |

| Create/modify | Responsibility |
| --- | --- |
| Create `C/Contracts/Enhancements/TraitGrowthGroup.cs` + `.meta` | `Unspecified = 0`, `Common = 1`, `CategorySpecific = 2`; no category/trait-key mapping in code |
| Modify `C/Contracts/Enhancements/TraitGrowthDefinition.cs`, `D/TraitGrowthDefinitionData.cs`, `D/TraitGrowthDefinitionDataSO.cs` | Carry and validate immutable `Group`; SO field `group` starts unspecified |
| Modify `C/Contracts/Enhancements/CategoryTraitProgressionSpec.cs`, `D/CategoryTraitProgressionData.cs`, `D/CategoryTraitProgressionDataSO.cs` | Carry and validate `CommonWeight`/`CategoryWeight`; SO fields `commonWeight`/`categoryWeight` |
| Modify `R/TraitGrowthDefinitionAdapter.cs` | Explicitly carry authored metadata through the DB-to-Core conversion |
| Modify `C/Application/Roster/EffectiveTraitProgression.cs` | Preserve Main steps/union and carry selected-category weights |
| Modify `C/Contracts/Roster/TraitGrowthOffer.cs` | Own `public const int MaximumChoices = 3` and use it in validation |
| Modify `C/Application/Roster/TraitGrowthOfferSampler.cs` | Group-first acquisition sampling; old uniform overload preserved |
| Modify `R/CharacterRosterService.cs` | Supply resolved weights and eligible-key group metadata to the sampler |
| Modify the 25 `A/TraitGrowth_*.asset` and four `A/CategoryTraitProgression_*.asset` listed below | Explicit production migration; references/effects/steps stay unchanged |
| Create `T/TraitGrowthWeightedOfferTests.cs`, `I/TraitGrowthWeightAuthoringTests.cs`, `I/TraitGrowthWeightedPublicationTests.cs` + `.meta` | Focused Core, authoring, and publication regression |
| Minimal fixture wiring in existing tests listed under Task 1 | Supply required serialized metadata without changing prior assertions |

Do not modify `TraitGrowthRules`, `TraitGrowthOfferState`, `TraitGrowthTransactions`, Jobs, UI, or service factories to implement the approved behavior. If a verified issue requires such a change, explain it and obtain a scope/design decision first.

## Verification procedure used by all phases

At execution start, inspect `git status --short`, current branch, applicable instructions, and the current owning sources. Preserve unrelated work. Use an already isolated checkout if present; consult the worktree skill before implementation if isolation is needed. Do not move work or create commits merely to satisfy a skill example.

Use the installed Unity 6000.0.73f1 executable discovered at execution time; its path has not been verified during planning. Read the Unity CLI skill before running Unity. If this project is already open, use its supported Editor test route rather than opening a second batch instance against the same directory.

For an idle project, the equivalent PowerShell batch invocation is below. `$unityEditor` must be the verified absolute executable path. Run through the approved AppData-access execution route. Put outputs in a new scratch directory under `$env:TEMP`, not in tracked assets.

```powershell
$testOutput = Join-Path $env:TEMP ('ProjectL-trait-weights-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testOutput
& $unityEditor -batchmode -nographics -projectPath 'D:\My_ProjectWork\Github_desktop\Project_L' -runTests -testPlatform EditMode -testFilter $testFilter -testResults (Join-Path $testOutput 'results.xml') -logFile (Join-Path $testOutput 'editor.log')
```

The filters are specified per task below. Do not add `-quit` to the asynchronous test launch. Observe completion and inspect both XML test counts/results and the Editor log; launch success alone is not passing evidence. A compile/import/infrastructure failure before test execution is not the intended red test. Record it separately and do not change test assertions to hide it.

A phase can claim verified completion only with passing relevant tests and required regressions, a scoped diff, and an explicit distinction between Core evidence and user Unity UI/PlayMode checks. If execution is unavailable, report the phase's code status and exact unverified checks; do not silently skip the gate.

---

## Phase 1 — Authoring contracts and asset migration

### Task 1: Add explicit groups and weights without activating weighted offers

**Files:** The definition/progression SO/Data/contract chain, enum, adapter, effective progression, production assets, and `I/TraitGrowthWeightAuthoringTests.cs`. New pure contract assertions belong in `T/TraitGrowthWeightedOfferTests.cs`. Existing fixture-input sites: `I/TraitGrowthAuthoringTests.cs`, `I/TraitDefinitionAuthoringTests.cs`, `I/TraitTurnConfigurationTests.cs`, and `I/DataBaseAssetTests.cs`.

**Interfaces:**

- `TraitGrowthDefinition.Group` and `TraitGrowthDefinitionData.Group`: get-only `TraitGrowthGroup`.
- Retain the existing definition constructors exactly. Add overloads with leading `TraitGrowthGroup group`, followed by the existing parameters, including optional rank arguments. The existing constructors delegate explicitly with `Common` for source/API compatibility; this is for existing programmatic definitions, not a missing-authoring-data fallback.
- SO `GetData()` always calls the explicit-group overload. `Unspecified` and undefined enum values are rejected in both explicit Data/Core construction paths; the SO adds its asset name to the error. Use `System.Enum.IsDefined` in InGame Data namespaces.
- `CategoryTraitProgressionSpec.CommonWeight` and `.CategoryWeight`: get-only `int`; expose corresponding Data properties.
- Retain the existing three-argument progression constructors. Add overloads `(CategoryKey category, IReadOnlyList<TraitGrowthStepSpec> steps, IReadOnlyList<TraitKey> pool, int commonWeight, int categoryWeight)`. Keep initial constants `DefaultCommonWeight = 1` and `DefaultCategoryWeight = 2` on `CategoryTraitProgressionSpec`; old constructors and new SO field initializers refer to them. Explicit serialized production values remain authored data.
- Validate both weights greater than zero and `(long)commonWeight + categoryWeight <= int.MaxValue` in the progression contract. Data delegates to this contract as it does today.
- `EffectiveTraitProgression.Resolve` retains its signature and returns selected weights when specialization is present.

- [x] **1.1 Add failing contract tests.** `ExplicitGroup_RejectsUnspecifiedAndUnknown` asserts both explicit constructors reject `0` and `999`; `LegacyConstructors_PreserveExistingCalls` asserts old signatures still construct valid definitions; `Weights_RejectInvalidValues` covers `(0,2)`, `(1,0)`, `(-1,2)`, `(int.MaxValue,1)` and accepts `(1,int.MaxValue-1)`.
- [x] **1.2 Add failing authoring tests.** `Authoring_RequiresGroupAndPreservesItAcrossSerialization` asserts an unclassified SO fails with its name, an explicit group round-trips, and a captured Data object stays unchanged when the source SO changes. `Authoring_PropagatesWeightsToCore` round-trips `3:7` through SO -> Data -> adapter -> catalog. `MainSpecialization_UsesSelectedWeightsAndMainSteps` covers Rider/Heavy/Cannon with deliberately different ratios, unchanged Main step IDs, and a distinct pool union.
- [x] **1.3 Run the new focused checks red.** Filter `ProjectL.Tests.Core.TraitGrowthWeightedOfferTests;ProjectL.Tests.CoreIntegration.TraitGrowthWeightAuthoringTests`. Missing new types/members initially establish the unimplemented API; after compiling the API, confirm the behavior assertions fail before adding the behavior. Do not count unrelated compile failures as evidence.
- [x] **1.4 Implement the interfaces above and metadata forwarding.** Keep the sampler and roster sampling call unchanged in this phase. Carry classification explicitly through `TraitGrowthDefinitionAdapter`; carry selected weights through `EffectiveTraitProgression` without changing eligibility.
- [x] **1.5 Migrate production assets as one complete set.** Set all four `CategoryTraitProgression_{Main,Rider,Heavy,Cannon}.asset` weights to `1:2`. Set trait `group` values according to the exact list below. Do not edit `pool`, `steps`, trait effects/ranks/text, file IDs, or GUIDs.
- [x] **1.6 Supply explicit group metadata in affected test SO setup only.** Inspect authorship before changing existing runtime-failing tests; stop for permission where required. Preserve original assertions. Avoid copying production pools or classification inference into shared fixtures; each fixture declares its own small test input.
- [ ] **1.7 Run Phase 1 checks green.** Filter the two new classes plus `TraitGrowthAuthoringTests`, `TraitDefinitionAuthoringTests`, `TraitTurnConfigurationTests`, `DataBaseAssetTests`, and `MainSpecializationGrowthTests` in their existing namespaces. Verify the newly authored classes have been discovered. Confirm all 25 production definitions have valid explicit groups and all four category rows have `1:2`.
- [x] **1.8 Inspect the Phase 1 diff.** Compare production pools, step data, effect data, and all existing `.meta` files against the pre-phase version; only approved fields may differ. Report results and **stop for Phase 1 review**. Weighted sampling is not active yet.

Production migration under `A/`, with filenames `TraitGrowth_<name>.asset`:

| Group | Exact names |
| --- | --- |
| Common (8) | Vanguard, CleanSweep, SoloAction, CallOfBlood, MoraleBoost, Disarm, AnkleBreaker, Knockback |
| CategorySpecific (17) | Acceleration, ArmorBreaker, Cohesion, DustScreen, Entrenchment, FinishingShot, FocusedBombardment, GroundCompaction, Hunter, Leap, PositionSwap, ReconSquad, Sacrifice, StationaryFire, SteadfastWill, Taunt, TrackingShot |

## Phase 2 — Weighted sampling and runtime activation

### Task 2: Draw distinct traits using authored group weights

**Files:** `C/Application/Roster/TraitGrowthOfferSampler.cs`, `C/Contracts/Roster/TraitGrowthOffer.cs`, `T/TraitGrowthWeightedOfferTests.cs`.

**Interfaces:**

- Keep `Sample(IReadOnlyList<TraitKey> candidates, TraitGrowthKind kind, int seed)` and `Seed(RunId runId, CharacterInstanceId ownedId, int stepId)` unchanged. The old overload retains uniform behavior and deterministic output for both kinds.
- Add `Sample(IReadOnlyList<TraitKey> candidates, TraitGrowthKind kind, int seed, IReadOnlyDictionary<TraitKey, TraitGrowthGroup> groups, int commonWeight, int categoryWeight)` returning `IReadOnlyList<TraitKey>`.
- Add `TraitGrowthOffer.MaximumChoices = 3`; use it in both offer validation and sampling count calculation.
- The new overload delegates `Enhance` to the existing uniform path. For `Acquire`, validate keys, candidate group coverage, group values, and weights; use sorted local group lists and `Random(seed)`. It must not retain input dictionaries or mutate caller collections. Additional dictionary entries do not become eligible candidates.

- [x] **2.1 Add failing sampling checks.** `WeightedSample_IsDistinctSortedAndInputOrderIndependent` asserts at most `MaximumChoices`, exact same output for reordered inputs and the same seed, and unchanged caller collections. `WeightedSample_RejectsInvalidInputs` covers empty/duplicate/invalid keys, missing or invalid candidate group, null metadata, and invalid/overflowing weights. `OfferMaximum_IsShared` asserts 1-3 choices accepted and 4 rejected by the offer contract.
- [x] **2.2 Add seeded distribution and exhaustion checks.** Across seeds `0..4095`, use at least three candidates per group so no group can exhaust before the last draw. With `1:2`, assert category cards / all drawn cards is within `0.04` of `2.0/3`; run once with 4 common + 4 category and again with 8 common + 4 category. With 4 + 4 and `1:1`, assert each trait's observed count is within 12% of the expected equal count. These deterministic aggregate checks must reject the old uniform-across-all-keys behavior on the unequal pool and a sampler always choosing the first key within a group.
- [x] **2.3 Add boundary and legacy checks.** Assert common-only/category-only input works; one common + two category always returns all three; one/two total candidates return all; extreme valid weights never throw or produce duplicates. `Enhance_IgnoresAcquisitionWeights` compares the new overload with the old uniform overload across fixed seeds. Capture a small pre-change old-overload golden result before editing it, and assert it remains unchanged for legacy callers.
- [x] **2.4 Run the sampler checks red**, then implement group-first draws: choose the nonempty group directly if the other is empty; otherwise use `random.Next(commonWeight + categoryWeight) < commonWeight`; draw uniformly within that list and remove the selected key. Repeat up to the shared maximum, then return sorted keys. Keep this in the existing sampler; no shop dependency or generic sampler service.
- [x] **2.5 Run sampler checks green**, including existing `ProjectL.Tests.Core.TraitGrowthOfferTests`, then inspect that the maximum literal has only the single intended contract owner in the acquisition sampler/offer path.

### Task 3: Connect the roster and verify frozen-offer behavior

**Files:** `R/CharacterRosterService.cs`, `I/TraitGrowthWeightedPublicationTests.cs`. Reuse `I/TraitGrowthTestRuntime.cs` and its `GrowthTables.ReplaceCategory` / `ReplaceDefinition` helpers; change this fixture only if new inputs cannot be represented through its existing methods.

**Interfaces:** Consume the explicit group/weight contracts from Task 1 and the new `Sample` overload from Task 2. Public roster services, choice requests, offer DTO shape, and Buffer APIs remain unchanged.

- [x] **3.1 Add failing runtime checks.** `PrepareOffer_UsesAuthoredGroupsAndWeights` uses test-owned rows with at least four eligible traits across both groups and a fixed Run ID; over a fixed set of Run IDs compare emitted aggregate group frequencies for `1:9` versus `9:1`, with a broad, deterministic separation assertion. This must fail if the runtime still calls the old uniform overload. Check all emitted keys are eligible and the roster is unchanged before selection.
- [x] **3.2 Add frozen and exhaustion checks.** `OpenOffer_WeightChangePreservesIdentityAndSelection` opens an offer, replaces only weights, asserts the same offer reference/ID/cards on repeated preparation, and accepts a frozen choice. `NextOffer_UsesUpdatedWeights` acknowledges the selection and verifies the next step uses the current policy. `OwnedCategoryPoolExhausted_FallsBackToCommon` and a compatibility-filtered fixture assert the runtime uses the remaining eligible group without broadening the allowlist.
- [x] **3.3 Add Main integration checks.** Cover unselected Main and each specialization with different Main/selected ratios and a shared common key. Assert effective sampling uses selected weights, retains Main step IDs, never duplicates the shared key, and never reclassifies Knockback. Keep Main definition validity and default traits in the existing fixture contract.
- [x] **3.4 Run the new publication checks red.** Filter `ProjectL.Tests.CoreIntegration.TraitGrowthWeightedPublicationTests`.
- [x] **3.5 Connect sampling in `PrepareNextTraitGrowthOffer`.** After reading the profile and eligible candidates, resolve effective progression and project a dictionary only for eligible keys from `catalog.GetTrait(key).Group`. Pass its weights to the new sampler overload. Preserve existing active-offer early return, seed, revision/default/source checks, choice text generation, and publication transaction unchanged.
- [x] **3.6 Run Phase 2 checks green.** Run all three new classes plus existing `TraitGrowthOfferTests`, `TraitGrowthRulesTests`, `TraitGrowthChoiceServiceTests`, `TraitGrowthCommandTests`, `TraitGrowthTransactionTests`, `TraitGrowthProgressContractTests`, and `MainSpecializationGrowthPublicationTests` in their current namespaces. No new selection validation should depend on today's weights after the offer is frozen.
- [x] **3.7 Inspect the Phase 2 diff and report.** State whether actual modified/understood/verified boundaries match the design. Confirm sampling is active only for acquisition and **stop for Phase 2 review**.

## Phase 3 — Regression, structural review, and designer handoff

### Task 4: Verify shared-contract consumers and document the final result

**Files:** Read the implemented files and existing Core/CoreIntegration suites; update this plan's execution record and the approved spec's status. Create `docs/superpowers/handoffs/2026-11-06-level-up-trait-group-weights-handoff.md`. No new product change is planned in this phase.

**Interfaces:** Verify the unchanged roster/Buffer/Job/View boundaries and the new authoring fields, not a new public integration API.

- [x] **4.1 Run full Core and CoreIntegration regression once.** Use `-assemblyNames 'ProjectL.Tests.Core;ProjectL.Tests.CoreIntegration'` instead of the focused `-testFilter` argument in the verification command. Inspect XML and logs. Include production `DataBaseAssetTests`, legacy history/Enhance, Main specialization, training, character stats, and battle preparation because they consume the shared definition construction path. Attribute failures with current evidence; do not assume historical failures are still present or fix unrelated features.
- [x] **4.2 Review SSOT/SOLID/FW and the actual diff.** Trace SO -> immutable Data -> adapter -> effective progression -> sampler -> roster -> Buffer -> existing popup consumer. Confirm one authored group per trait, one weight pair per category, no enum-key classification switch, one maximum, no duplicated active offer, no new service registration, no direct View DB access, and no unrelated implementation split. Record actual change/understanding/verification scope alongside the design's expected scope.
- [x] **4.3 Write the English designer/Core-to-View handoff.** Explain exact SO paths and fields, the 1:2 default, positive-integer validation, group-first probability versus guaranteed slots, fallback, classification versus availability, Main specialization policy, frozen-offer behavior, and seed-output changes for newly generated offers. List test evidence and limitations separately.
- [x] **4.4 Supply user Unity checks.** Change a category ratio, trigger a fresh level-up offer, confirm 1-3 distinct cards and successful acquisition, repeat after specialization, and confirm existing offers stay unchanged. Visual checks and PlayMode acceptance remain user-owned; automated Core results do not establish them.
- [x] **4.5 Verify delivery scope.** Run `git diff --check` and `git status --short`; inspect every modified/untracked file. Confirm no unexpected FMOD/assets, cache changes, build configuration edits, commits, or pushes. Report the phase and **stop**.

## Execution record

| Phase | State | Evidence / remaining gate |
| --- | --- | --- |
| Planning | Approved | The spec was mapped to three separately authorized phases. |
| 1 — Authoring | Implemented and reviewed | 25 definitions and four category rows migrated. All 17 new cases passed. The focused run passed 172 of 179 cases, and its seven failures matched the pre-change `DataBaseAssetTests` baseline. Step 1.7 remains open for an all-green run. |
| 2 — Sampling | Implemented and reviewed | All 39 sampler and offer cases passed. The final scoped regression passed 97 of 97 cases, including nine publication cases. |
| 3 — Verification | Regression and handoff recorded | Core passed 4,885 of 4,910 cases. CoreIntegration passed 870 of 917 cases. All 72 failures reproduced with identical names and messages across the corresponding 34 pre-change fixtures at `77db0f19b` (450 of 522 passed). The [designer and Core-to-View handoff](../handoffs/2026-11-06-level-up-trait-group-weights-handoff.md) records the structural review and user Unity checks. |

## Post-implementation review

The user authorized review and immediate remediation after Phase 3. Invalid serialized category weights now report the asset name and authored values while retaining the underlying argument error. The progression constructor and public sampler share the internal Core weight guard. Five diagnostic regression cases failed before the fix, and all 188 selected cases passed afterward. Evidence is in `C:/Temp/ProjectL-trait-weights-01a1acbd-phase2-review-green`. This follow-up changes three existing product files and the feature's authoring test fixture. The Phase 3 full-suite failures and pending user Unity checks remain recorded above.

## Plan self-review

- Coverage: group weights, designer control, classification migration, Knockback, Main, frozen offers, legacy paths, shared maximum, validation, and scoped structural review each have an owning task.
- Type consistency: definitions use `Group`; category rows use `CommonWeight`/`CategoryWeight`; the sampler accepts eligible keys plus their group dictionary, never the DB or Buffer context.
- Compatibility: old constructors and uniform sampler overload remain callable; production SO conversion explicitly supplies metadata and cannot take the legacy constructor fallback.
- Scope: three sequential phases, each with a stop gate. No speculative refactor, generic framework, View implementation, or automated commit steps.
- Execution recommendation: inline implementation suits this tightly connected contract chain. User may choose delegated execution; neither method authorizes running multiple phases without the required review stops.
