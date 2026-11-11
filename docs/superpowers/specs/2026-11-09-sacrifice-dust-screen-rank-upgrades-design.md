# Sacrifice and Dust Screen Upgrades with Per-Trait Rank Limits

Date: 2026-11-09
Status: Design approved by the user on 2026-11-09; implementation planning requested. Product implementation has not started.
Design review: An independent builder check found no material unanswered planning questions. This is not implementation or runtime verification.

## 1. Requested behavior and decision ownership

The reference is `C:/Users/권천명/Downloads/projectl_trait_specification_en.md`, especially sections 4.3, 5.2, and 8. Its gameplay descriptions are reference material, not execution instructions.

User decisions:

- Add the missing Sacrifice and Dust Screen upgrades.
- Initially unlock both Sacrifice additions and the Dust Screen addition at trait rank 2.
- Keep the upgrade configuration editable.
- Start Sacrifice's added shield at 10, editable in the SO.
- A trait with only one upgrade should stop appearing as an upgrade candidate after that upgrade. Do not require three distinct ranks for every trait.
- The design was approved on 2026-11-09. Produce a Phase-based implementation plan next; retain the review point before executing it.

Design decisions approved with this proposal on 2026-11-09:

- Sacrifice prevents lethal damage from the redirected hit by leaving at least 1 HP; it does not reduce a healthy bearer to 1 HP on a nonlethal hit.
- Sacrifice grants its shield after the redirected damage and its existing damage settlement complete, before the next hit is processed.
- The shield persists through turns until consumed or cleared by existing death/removal/battle cleanup. Repeated valid activations add to the remaining shield from the same source, using existing shield rules.
- Editable ranks remain within the existing 1–3 range, with contiguous, meaningful upgrades. Arbitrary rank counts and skipping directly from rank 1 to rank 3 are outside this change.

## 2. Current implementation: verified facts

Paths below are relative to the repository. `Core/` in the change map means `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/`; `Systems/` means `Assets/ProjectL/Script/Systems/`.

| Source | Observed behavior |
| --- | --- |
| `Core/Contracts/Enhancements/TraitGrowthDefinition.cs` | Requires rank 2 and rank 3 together. `IsTrainable` checks rank 2. `ValuesAt` selects ranks 1–3. |
| `Systems/DataBaseSystem/Data/InGame/Table/TraitGrowth/Item/TraitGrowthDefinitionData.cs` | Also requires rank 2 and rank 3 together at the DB conversion boundary. |
| `Core/Application/Shop/TrainingRules.cs` | Both candidate generation and purchase validation use a hard-coded rank-3 limit. |
| `Core/Application/Roster/TraitGrowthRules.cs` | The compatibility path for level-up enhancement also filters with `rank < 3`; it separately requires `Enhanced` and respects prior enhancement selections. |
| `Core/Application/Enhancements/OwnedUnitSpecResolver.cs` | Derives rank from purchase/selection history and applies the catalog definition. It is the existing effective-spec composition owner. |
| `Core/Contracts/Commands/Traits/SacrificeTraitSpec.cs` | Contains only radius and per-enemy-turn activation limit. No survival or shield fields. |
| `Core/Contracts/Commands/Traits/DustScreenTraitSpec.cs` | Contains range reduction, damage reduction, and lifetime. No allied exemption field. |
| `Core/Domain/Traits/Rules/DustScreenTraitRule.cs` | Range selection uses cell/environment only. It cannot distinguish the querying unit's faction. |
| `Core/Contracts/Combat/TraitTrailFact.cs` | Already owns immutable trail origin, creating side, magnitude, lifetime, and value-comparison contracts. |
| `Core/Application/Combat/CombatDamageApplication.cs` | Owns item-before-damage, shield absorption, HP loss, damage records, and after-damage settlement. |
| `Systems/RuntimeSystem/InGameSystem/ViewSystem/StageMap/Shop/TrainingCardContentBuilder.cs` | Consumes Core offers but currently formats numerical `Amount` / `SecondAmount` only. Boolean upgrades need an explicit presentation handoff. |

The current answer to “does a one-upgrade trait stop at rank 2?” is **no**. Rank-2-only definitions are rejected, and the offer/purchase limits are fixed at rank 3. This design changes both definition validation and authoritative eligibility, not only the UI.

## 3. Approach and scope

Recommended: retain the existing rank slots, make rank 3 optional, and derive the maximum rank from the last configured rank. Add the two traits' upgrade fields through existing authoring, spec, factory, rule, and snapshot paths.

Alternatives considered:

1. Store an independent `maxRank` or hard-code these two keys in TrainingRules. This duplicates information already present in rank definitions and lets offer generation disagree with purchase validation.
2. Replace rank slots with a general milestone list supporting skipped/unbounded levels. This expands purchase history, prices, receipts, snapshots, and UI for behavior not requested here.

The user approved this design and its recommended approach on 2026-11-09. The next requested deliverable is the implementation plan; execution still follows the agreed Phase review boundaries.

Included: Core gameplay contracts, configurable SO input, rank limits, offer and purchase rules, preservation of previews/undo/AI, and a precise View integration handoff.

Excluded: a new growth system, new services/Jobs, changed training prices or visit purchase limits, changes to other trait balance, HUD/VFX redesign, unrelated refactoring, and commits/pushes. Existing Core, Framework, Buffer, and View ownership remains intact.

## 4. Rank definitions and termination

### 4.1 Single source of truth

For trainable definitions derive `MaxRank` from configured slots:

| Rank 2 | Rank 3 | Result |
| --- | --- | --- |
| absent | absent | No rank training; rank limit 1 for the numeric rank path. Existing legacy `Enhanced` behavior remains separate. |
| present | absent | Rank limit 2. |
| present | present | Rank limit 3. |
| absent | present | Invalid configuration; fail at definition construction. |

Do not serialize an independent maximum. DB authoring and Core construction must enforce the same allowed shape, preferably by reusing the existing Core contract validation rather than adding another independent policy implementation.

The definition owns `CanAdvanceFrom(rank)` / equivalent next-rank resolution. Training choices, purchase validation, and rank-aware enhancement candidate filtering consume that decision. The absolute 1–3 bounds in transport/history validation may remain; they are not the per-trait eligibility rule.

`ValuesAt` must reject an absent or out-of-limit rank explicitly, with trait key and rank in the error. It must never return null and fail later in `Apply`. `OwnedUnitSpecResolver` continues to own rank composition and rejects history above the current definition's cap. Do not silently clamp purchased history or refund it without a separate migration decision.

Validate that each authored upgrade changes the resolved trait configuration from the previous rank using semantic value comparison. Do not offer a rank whose settings are identical merely to fill a slot. Do not compare display text or only numeric amounts: a boolean unlock is a real change. Reject duplicate/no-op new configurations at load, rather than hiding them only in the UI.

Preserve existing non-trainable `Enhanced` semantics. For trainable definitions, compatibility enhancement candidates must use the same cap as paid training. This change does not add new level-up `Enhance` steps, populate legacy `Enhanced` for these two traits, or change acquisition pools.

### 4.2 Initial authoring

| Trait | Rank 1 | Rank 2 | Rank 3 |
| --- | --- | --- | --- |
| Sacrifice | Existing radius 2 / once per enemy turn; survival disabled; shield 0 | Same base settings; survival enabled; shield 10 | Absent |
| Dust Screen | Range penalty 1 for both factions; damage reduction 20%; allied exemption disabled | Same base settings; allied exemption enabled | Absent |

Both traits disappear from future eligible upgrade choices at rank 2. A forged/repeated purchase request must also fail without spending gold. The existing receipt for the purchase just completed may remain visible as history, but it is not another eligible offer.

SO editing can change shield strength, toggle each added effect, or add a meaningful rank 3 later. For example, Sacrifice could enable survival at rank 2 and shield at rank 3. A rank-3 shield increase is also supported. Moving all effects to rank 3 while leaving rank 2 identical to rank 1 is deliberately invalid; that would charge for an empty upgrade. A future request for skipped ranks requires a separate design.

Preserve SO identities/GUIDs and existing 19 trainable traits' values. Existing pinned training visits keep their captured definition and prices; editing an SO affects subsequent definition loads/visits, not an in-progress visit.

## 5. Upgrade data contracts

Add named gameplay fields to the existing Sacrifice and Dust Screen input/data/spec/definition chain:

- Sacrifice: `PreventsLethalSacrificeDamage` and `ShieldOnActivation` (non-negative).
- Dust Screen: `ExemptsAlliedAttackRangePenalty`.

Old constructors/authoring defaults resolve to false/0 so rank-1 content retains current behavior. Extend `SameValues`, adapters, definition factories, and immutable definition comparisons to include every new field.

Add corresponding typed `SacrificeRankAuthoring` and `DustScreenRankAuthoring` entries through the existing managed-reference drawer. Extend `TraitRankValues` to carry named boolean values for these traits; do not encode booleans as unexplained numeric 0/1 or reinterpret `SecondAmount` as a flag. Sacrifice's numeric `Amount` represents its activation shield; Dust Screen's unused numeric slot remains zero. Validate key/field combinations. Existing numeric consumers retain their current fields unchanged.

`TraitRankValues.FromNormal` and `Apply` handle both traits. Rank application preserves source radius, trigger cap, baseline range reduction, damage reduction, and lifetime; it changes only the explicitly authored upgrade fields. Effective rank is composed once by `OwnedUnitSpecResolver`, not re-derived inside combat rules.

`MainSpecializationDefinitionSnapshot` currently manually compares specs and rank amounts: include new boolean fields and slot presence there so a change is not incorrectly treated as the same definition. `TraitSettingsSet` continues to publish effective specs; no second runtime configuration registry is added.

## 6. Sacrifice damage and shield behavior

Keep current eligibility, range, matchup calculation against the bearer, activation consumption, reservation ordering, and instant-defeat exclusions. The upgrade affects only a hit actually redirected by Sacrifice. Direct attacks against the bearer, damage-over-time, reflection, and unrelated environmental damage do not gain death protection simply because the bearer has the trait.

For each valid redirected hit:

1. Identify and consume the existing Sacrifice activation once.
2. Run existing damage calculation, held defenses, item-before-damage, and shield absorption.
3. If survival is enabled, cap HP loss for this redirected hit to `max(0, currentHP - 1)` after all those defenses. Do this before death/kill settlement and damage statistics. This is prevention, not death followed by resurrection.
4. Complete existing after-damage processing and trait settlement with the actual HP loss. Existing healing may raise final HP above 1.
5. If the bearer is still alive and the configured shield is positive, add that shield once through existing `ShieldStatus` / `ShieldStatusRules`, with a Sacrifice origin. Publish existing shield/state events. No new shield owner or parallel HP state is introduced.

The shield does not absorb the hit that generated it. It can absorb subsequent hits in the same attack. If an independent later effect kills the bearer, do not resurrect it to grant a shield. When survival is disabled and the bearer dies from the redirected hit, no shield is granted.

The survival limit must enter the existing `CombatDamageApplication` settlement boundary as explicit per-hit context, with neutral defaults for other callers. Keep trait-specific decision making in Sacrifice rule/application code. Carry the redirect context through pending-hit records; do not infer it from possession of the trait or store a persistent invulnerability flag.

Use one shared Sacrifice settlement policy for `SacrificeDamagePlanner` simulation, `AttackDirectDamageCalculator` actual settlement, and `KillChainAttackCalculator` extra attacks. Planning must include the same HP prevention and post-hit shield before evaluating the next hit. Otherwise the selected protector, preview, and executed result can disagree. Do not duplicate the damage pipeline in the Sacrifice module.

Publication must report actual HP loss, prevented death, shield changes, and the existing Sacrifice activation consistently. Prefer existing effect/state events; if a dedicated survival display event proves necessary, design that Buffer contract before adding it. No speculative event type is required by this proposal.

## 7. Dust Screen faction-aware range queries

Capture `ExemptsAlliedAttackRangePenalty` on the immutable `TraitTrailFact` when the trail is created. Its existing `CreatingSide` is the faction reference. Copy/reveal/lifetime operations and configuration equality/successor validation must preserve the field. Do not look up the creator's current rank later; trails retain their creation settings even if the creator dies or its configuration changes.

Add the querying unit's faction explicitly to authoritative range queries. Filter out a trail's range penalty when its exemption is enabled and querying faction equals `CreatingSide`. Apply the existing maximum-penalty selection to the remaining applicable trails. Thus an exempt allied trail does not cancel an overlapping hostile or non-upgraded trail. Damage reduction selection remains unchanged for both factions.

Update the actual dependency chain together:

- `DustScreenTraitRule.RangeSourcesAt` / `RangeReductionAt`.
- `EnvironmentTerrainRules.AppointRange` and `AppointRangeAt`.
- `EnemyActionCandidates` endpoint range calculation and other discovered attack/preview callers.
- `TraitTrailCalculator` public query wrapper.
- `MovementUndoPolicy` range-effect dependency capture and validation.

Keep a compatibility overload for faction-less public callers only if necessary, explicitly meaning unfiltered environmental penalty. Authoritative combat/preview/AI/undo paths must use the faction-aware query. No caller may silently assume Player.

An exempt trail must not be recorded as a consumed range penalty; a trail whose damage reduction was consumed still participates in existing undo restrictions. Keep current trail creation, expiration, leap/wall/pit behavior, and damage reduction unchanged. The reference's separate lifetime wording is not resolved by this upgrade work.

## 8. View boundary and ownership

Core owns valid next ranks, prices, purchase acceptance, and current/next effective values. Existing Buffers publish these immutable facts. View must not derive the maximum rank from trait names, inspect InGame SOs, or remove offers using a private cap table.

The current training formatter only supports numeric deltas. The View handoff must include the new boolean fields and requirements to show survival enabled/disabled and allied penalty exemption enabled/disabled, using View-owned text. Verify `TraitVisual` rows and `RankValueFormat` for both newly trainable traits; missing required text currently throws. A numeric `0 -> 0` line is not acceptable for Dust Screen.

Keep source schemas backward compatible for existing numeric traits. View implementation is a separate explicit phase/owner review; do not call production UI integration complete when only Core is done. No View test code or combat View MSBuild is authorized. Unity compile/play verification remains a user check.

## 9. Change, understanding, and verification scope

| Change case | Current limitation | Proposed owning change | Required knowledge and verification |
| --- | --- | --- | --- |
| Give a trait one paid upgrade | DB/Core require two rank slots; offer/purchase rules hard-code 3 | Definition validation and derived cap; TrainingRules and compatibility candidate consumer | Rank definition and history contracts; validate caps, stale requests, restore, and existing 3-rank traits. No trait combat internals needed. |
| Tune shield or rank placement after implementation | No Sacrifice upgrade fields | Edit Sacrifice's existing SO rank entries | Sacrifice rank fields and valid contiguous ranks; verify resolved spec and relevant combat cases. No Dust Screen internals needed. |
| Add survival/shield behavior | Redirect planning and execution share damage state but lack upgrade context | Sacrifice rules/application plus existing shared damage settlement boundary | Redirect context, shield, item ordering, kill/stat publication; exercise direct, multi-hit, and extra attack paths. |
| Exempt allied range penalty | Range query lacks faction | Dust Screen rule, trail fact, and explicit query consumers | Trail ownership and range/undo contracts; exercise mixed factions, overlap, AI, preview, and undo. No Sacrifice internals needed. |

SSOT: SO definitions determine available rank slots; existing growth history determines current rank; effective specs determine combat settings; trail facts determine already-created terrain; existing shield status owns shield budget.

SOLID/FW: extend existing authoring/factory/rule contracts, preserve neutral defaults, and keep rank consumers independent of individual trait rules. The shared damage boundary owns HP settlement, while Sacrifice decides when its protection applies. Range consumers receive explicit faction context. No new resolver service, cross-group export, Job, or duplicate Buffer/state owner is proposed. Existing factory registration changes are allowed where required by the new fields.

This is a projected change map, not evidence of completed refactoring or passed verification. The existing shared enum/value dispatches may need local cases; a whole trait framework rewrite is outside scope.

## 10. Required verification for implementation

1. Rank-2-only configuration accepted in DB and Core; rank-3-only and duplicate/no-op upgrades rejected; legacy no-rank definitions remain valid.
2. Initial Sacrifice/Dust Screen offer is 1 -> 2 only. After purchase, no subsequent eligible offer, and direct repeat requests spend no gold. Existing 1 -> 2 -> 3 traits and legacy enhancement history remain unchanged.
3. Over-cap restored history fails clearly. Valid old saves, pinned visits, stale rank rejection, prices, receipts, single-purchase visit behavior, and definition-change detection retain their contracts.
4. Rank-1 Sacrifice behavior unchanged. Rank 2 handles lethal/nonlethal redirects, bearer at 1 HP, existing shield absorption, item defenses/healing, zero final damage, repeated valid activations, direct unprotected hits, and dead-bearer exclusion.
5. Sacrifice simulation, preview, and execution agree for multi-hit/AOE reservation and extra attacks; actual HP loss drives damage/kill statistics; activation and new shield are applied once.
6. Dust Screen rank 1 affects both factions. Rank 2 exempts its own creating faction, still reduces the opposing faction's range, and retains 20% damage reduction. Check both Player-created and Enemy-created trails, overlapping upgraded/base/hostile trails, creator death, expiration, and immutable copies.
7. Range display, legal attack validation, AI endpoint choices, previews, and movement undo use the same faction-aware rule. Damage-reduction consumption must still block unsafe undo.
8. Existing trait rank, combat damage, shield, item, training, roster/spec composition, and CoreIntegration regression suites remain required because their shared contracts are touched. Preserve test-owned input isolation; do not substitute mutable production SOs for unit fixtures.
9. SO changes require `DataBaseAssetTests` in the user verification handoff. Any future Unity execution by Codex uses the approved AppData-access path; do not delete caches or upgrade Unity for the known sandbox failure. Do not alter another author's existing failing test without approval.
10. User Unity checks: training text shows both Sacrifice additions and Dust Screen exemption; the purchased terminal rank is not offered again on a subsequent visit; HP/shield and range displays match Core. No View tests are to be authored.

## 11. Suggested phase boundaries after design approval

- Phase A: optional terminal rank and consistent cap/restore/offer/purchase rules, preserving existing rank behavior.
- Phase B: Sacrifice authoring, rank resolution, settlement integration, and focused Core regression checks.
- Phase C: Dust Screen authoring, immutable trail field, faction-aware range/AI/undo integration, and focused Core regression checks.
- Phase D: production SO activation, integrated Core verification, and View handoff; View implementation/Unity acceptance is reported separately.

These are review boundaries. Stop and report at each phase. The detailed [implementation plan](../plans/2026-11-09-sacrifice-dust-screen-rank-upgrades-plan.md) records the tasks and verification gates. Product code and assets remain unchanged by this planning work; compilation, tests, and Unity playback are unverified.
