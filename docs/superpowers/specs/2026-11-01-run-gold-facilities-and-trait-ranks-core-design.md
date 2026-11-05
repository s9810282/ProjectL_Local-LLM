# Run gold facilities and trait ranks

**Core design for review · 2026-11-01**

Players spend Run gold to heal the Main, hire a unit or improve one trait on an owned character. Unit levels unlock new traits. Training spends gold to improve a trait the character already owns. The two decisions share the same trait identity but have different eligibility rules.

Scope covers Core rules, InGame ScriptableObjects (SOs), their adapters and the Buffer contracts required to expose results. The intended presentation names are Rest Site, Training Ground and Mercenary Office. The View owner handles displayed names, selection UI and scene wiring. This document records a proposed implementation and acceptance criteria. No implementation or Unity test run was performed for this design.

## 1. Stage purchases

| Facility | Choice and initial offer | Settlement |
|---|---|---|
| Rest Site | Choose either a heal for **5 gold**, restoring **30% of maximum HP**, or a persistent maximum-HP increase of **10** for **10 gold**. Author the two prices, percentage and increase independently in recovery SO data. | At most one successful purchase per visit. Heal uses the existing integer calculation, rounded down and capped at missing HP. At 40/60 the results are 58/60 or 40/70, respectively. At full health the heal is unavailable and maximum-HP growth remains eligible. Retire the flat and full heals from this visit's offered options. |
| Mercenary Office | The existing ordinary shop offers units only. Keep the authored recruitment price and existing offer generation, level and acquisition eligibility. | At most one hire per visit. Use the existing roster purchase and receipt. Remove consumables from the office's production candidate data. Other item acquisition and usage are outside this change. |
| Training Ground | Choose any owned Main or Sub instance in this Run, regardless of deck membership, then one of its eligible traits. Show all eligible choices for direct selection. | One visit permits one character to raise one trait by exactly one rank. Selecting a character or trait is free. Leaving is always available, including with sufficient funds, no eligible trait, or insufficient funds. Exit completes the visit and allows map progression. |

Training prices are two independent fields on the training facility's InGame SO. Require a positive price for 1→2 and a higher price for 2→3. Production amounts are **undecided**. Use explicit fixture prices for tests. Both Main and Sub use the same stage prices. Only an accepted purchase spends gold or consumes the visit.

**Owning paths.** Keep recovery in the existing recovery event, evaluator and transaction. Extend the heal-only option/receipt contract to distinguish healing from maximum-HP growth. Apply growth to `MainCharacterPersistentState.PersistentMaxHealth` while preserving `CurrentHealth` and reusing the existing effective maximum calculation. Keep other stats and soul modifiers intact. Both offers compete for the same visit allowance.

Keep mercenary generation in `ShopDefinitionAdapter` and `ShopOfferPlanner`. Expose training through the existing shop node, stage entry, command and transaction boundaries. The existing enhancement stage SO now represents training only (owner-approved simplification, 2026-11-02). Preserve its asset/script/stage identity and remove the legacy product-list input and mode toggle. Both prices at zero mean not yet configured: catalog loading succeeds, but new entry remains unavailable until the price pair is valid. A partially configured or invalid pair is an authoring error. Continue to reject old enhancement offers, including restored snapshots, at the existing purchase boundaries. Training has a different target contract from a random ordinary shop card. Enumerate eligible instance/trait pairs without applying the ordinary shop card-count limit. An empty eligible set still permits entry and exit.

On purchase, carry visit identity, owned instance ID, trait key and expected current rank. Resolve price and effective ownership in Core. Revalidate against the active visit, roster revision, balance and unspent allowance at publication. Commit the updated character, debit, receipt and visit consumption together. Stale, repeated, unaffordable or invalid requests change none of them. Preserve existing Run spending accounting. The existing Job dispatches commands and the existing Buffer publishes snapshots.

## 2. Unit levels and category ownership

- Keep `CategoryTraitProgressionDataSO.pool` as the sole authoring source for category membership and `steps` as the separate schedule on that same SO. Main, Heavy, Rider and Cannon keep the current level-2 Acquire step and its ID. Append a new Acquire step with its own stable ID at unit level 3. Each choice grants a new **rank-1** trait. If multiple steps are pending, resolve them in their existing order.
- Reference one trait definition from every eligible pool. Common traits are candidates across all four player categories. A category-exclusive trait appears only in its pool. Preserve the specialized Main rule in `EffectiveTraitProgression.Resolve`, which unions Main and the selected category and deduplicates keys. Category defaults and native traits still apply under their existing ownership rules.
- Reuse `TraitGrowthRules.EligibleTraits` for effective ownership, availability and attack compatibility. A previously shown but unchosen trait can return. An already owned trait cannot. From the remaining eligible keys, sample three distinct keys when possible. With one or two, present all valid keys and let the player select. Zero is an authoring error. Keep deterministic sampling and stable offers. Use the same limits in `TraitGrowthCatalogValidator.Visit`, `TraitGrowthOfferSampler.Sample` and the `TraitGrowthOffer` contract published by the Buffer.

**Content reconciliation.** The inspected Main/Heavy pools each contain the same 14 definitions. Rider/Cannon each contain the same 13, omitting Knockback. Taunt and Stationary Fire are among the entries shared by all four. The working content baseline below comes from the earlier 24-trait classification. Check it against current production assignments when preparing the SO edits. It covers acquisition, including traits that receive no training ranks.

| Candidate group | Working assignments |
|---|---|
| Common | Vanguard, Clean Sweep, Solo Action, Call of Blood, Morale Boost, Disarm, Ankle Breaker |
| Heavy | Position Swap, Cohesion, Sacrifice, Taunt, Entrenchment, Steadfast Will |
| Rider | Ground Compaction, Dust Screen, Leap, Acceleration, Recon Squad |
| Cannon | Stationary Fire, Armor Breaker, Finishing Shot, Tracking Shot, Hunter, Focused Bombardment |

Record exceptions explicitly during the production edit. **Proposed compatibility default:** preserve existing pool assignments for uncategorized entries such as Knockback until the designer reassigns them. Preserve the acquisition and use of native, enemy-only, deferred and retired traits under their respective existing contracts. Registration in combat alone does not imply level-up eligibility. The 17-row training table below must never be used to reconstruct these pools.

## 3. Trait rank authoring and Run state

The existing `TraitGrowthDefinitionDataSO` owns the typed normal definition and an optional legacy enhanced form. Extend this authoring route to represent ranks up to 3 on the same key. Use the existing rank-1 value where the brief specifies **n**. Each higher rank stores the actual final value, editable independently in the Inspector. The offsets in the brief seed the initial values once. For Vanguard, changing rank 1 later from 1 to 2 leaves ranks 2 and 3 at 4 and 9 until edited. For explicit triplets, use the brief's numbers even where the current normal SO differs. Preserve each trait's unchanged geometry, rounding, cadence and targeting parameters in its existing typed definition.

Register the required definitions in the active growth table, including missing rank-1 definitions for eligible pool entries. Reuse existing keys and normal effects for Position Swap and Recon Squad. Express training eligibility by the presence of a complete authored rank progression for the 17 approved keys. Validate the approved production set in content tests. Other traits retain their existing acquisition/use rules, and their legacy enhanced assets alone do not make them trainable.

Use the owned character's existing Run growth state as the owner of purchased rank increments keyed by trait. A rank purchase belongs to a character instance, never to all copies of a unit or to the shared SO. It persists across stages and reserve returns, and clears with that Run. Keep level-up step selections distinct from purchases so a purchase cannot consume a level-up choice. An absent purchase record means no paid increment. Resolve the owned trait and its rank through the existing `OwnedUnitSpecResolver` path used by roster queries and battle preparation. Preserve effective native/default ownership, acquisition history, Hunter growth and Main specialization on record replacement. A rank record alone grants no missing trait.

**Native traits and tuning.** Use the same authored values for the rank-controlled fields in section 4 regardless of how the unit obtained the key. Preserve native geometry, targeting and other unmodified fields. For example, the current Akimbo asset supplies native Morale Boost at 20% and Clean Sweep with duration 3. At rank 1 the new design requires 10% and expiry at turn end respectively, even before the first level-up or training purchase. Resolve those fields from the shared definition when composing the effective typed spec for roster and battle, including the no-growth path. Do not require a second edit in each unit SO when designers tune a ranked value. Test a native, category-default and acquired source for precedence, duplicate prevention and preservation of unrelated parameters.

**A2 owner decision (2026-11-01).** The owner authorized cleaning the shared Akimbo presentation-test trait setup. Its production native trait is restored to the pre-test KillChainAttack; the above Akimbo Morale Boost/Clean Sweep values describe the prior test payload. B2 must exercise those native tuning cases with explicit fixtures, without reintroducing the presentation-test traits into the production unit. Native/default/acquired rank resolution requirements remain unchanged.

**Proposed compatibility policy.** Preserve legitimate acquisitions made before a pool edit. For example, a Cannon that previously chose Taunt keeps it for that Run, and may train it if eligible, even after new Cannon offers stop including it. The current resolver checks current pool membership and assumes positional correspondence between selections and steps. Handle existing history at the roster/definition boundary using the prior valid step, pool and trait identity. Continue validating new choices against the current pool.

Preserve valid legacy Enhance records and their old effect where no new training ranks exist. For an old enhanced record on a trainable trait, count the old enhancement once as rank 2, then permit at most the remaining paid increase. Keep any prior Acquire step consumed. An old Enhance must coexist with the new level-3 Acquire without consuming it. Identify old choices by their stable step IDs and original meanings during normalization. Invalid or unrecognizable history remains an explicit error. Never charge gold or replay a purchase on conversion. Production level-up steps going forward are Acquire only.

## 4. Initial tuning for the 17 trainable traits

Triplets are **trait rank 1 / 2 / 3**. Percentage changes are percentage points between ranks. Each row refers to one existing trait key. The rank table changes the effective definition used by the ordinary combat path. It does not introduce alternate attacks, movement or settlement.

| Trait | Initial values | Trigger and effect |
|---|---|---|
| Vanguard | +1 / +4 / +9 attack | If the holder makes the side's first allied attack, grant attack to the other eligible allies. Clear the grant at turn end. |
| Clean Sweep | +1 / +4 / +7 attack | Add attack to the holder for each other ally's attack action. Sum grants and clear them at turn end. A multi-hit action is one attack action. |
| Solo Action | 10 / 19 / 30 shield | Active only with no other unit in the eight neighboring cells. Suppress immediately on occupancy. Re-entering isolation restores access to the remaining budget. Reset the budget at each side's turn end. |
| Call of Blood | 10 / 13 / 17% | For each enemy killed by the holder, heal from that enemy's maximum HP, round to nearest using the existing positive half-up rule and cap at the holder's maximum. An area attack credits each kill. |
| Morale Boost | 10 / 20 / 30% | On a credited kill, grant eligible allies attack based on the defeated enemy's base attack. Keep per-kill rounding, accumulation and existing turn expiry. |
| Position Swap | Shield per participant: 0 / 10 / 18 | Keep legal allied swaps within movement range and paired movement undo. Apply the grant to both participants on a successful swap. See below. |
| Cohesion | Defense per ally: 1 / 4 / 7. Shield per adjacent ally: 1 / 6 / 9 | Count other field allies in the eight neighboring cells for the holder's defense. Each eligible neighbor receives its own shield through the existing presence rule. |
| Taunt | Incoming reduction: 0 / 10 / 15% | Preserve the merged taunt area, forced target and pursuit behavior. Reduce only damage the holder takes from an enemy currently taunted by that holder. |
| Entrenchment | 50 / 56 / 62% | Preserve the existing incoming-damage reduction area for allies in the eight neighboring cells. |
| Ground Compaction | +1 / +2 / +3 movement range | The actual moved path creates compacted ground. Any unit starting movement there gets the range bonus. The stated expiry is the end of the originator's next turn. See timing below. |
| Recon Squad | +10 / +15 / +20% damage received | Check orthogonal radius 1 from the start and actual reached cells. Apply once per enemy per move. Preserve existing exposure, rollback and reserve-return rules. |
| Stationary Fire | +100 / +120 / +150% damage | Use the existing unmoved-attack condition. Values express additional damage, so +100% doubles the qualifying base damage. |
| Armor Breaker | 10 / 20 / 30% defense reduction per hit | Apply to each enemy hit through the existing after-hit rule. Successive reductions multiply the remaining effective defense. |
| Finishing Shot | 15 / 20 / 30% of attacker attack | Preserve the existing strike timing. If a living target's HP is strictly below the threshold, execute through the existing defense-bypassing instant-defeat path. Equality does not qualify. |
| Tracking Shot | HP threshold: 50 / 55 / 60%. Extra damage: 20 / 25 / 33% | Check the target's current HP percentage at each hit. Equality qualifies. Both parameters are independently tunable per rank. |
| Hunter | +3 / +5 / +8 permanent attack per target kill | Mark a living enemy with highest current HP, using existing RNG for ties. Only the holder's credited marked kills award attack. Keep the existing retarget flow and three-award stage limit. |
| Focused Bombardment | +70 / +80 / +100% damage | Exactly one enemy in the area of attack activates the existing multiplicative bonus. |

**Position Swap.** Attribute shields to the swap source and the side whose turn produced the grant. Add incoming amounts to the remaining balance, even for the same recipient or source. Two rank-2 grants yield 20. If damage consumes 7, expire only the remaining 13 at the start of that side's next turn. Apply symmetrically to player and enemy. Use the shared shield absorption/order and effect lifecycle. Extend the current shield origin contract, which only accepts Solo Action and Cohesion, and the movement receipt so a valid undo restores both positions and the touched shield origins to their exact pre-move states. Undoing the second swap preserves the first grant. Consumed or subsequently changed state must pass existing effect-aware undo checks. Do not reset unrelated shields or replay a forward swap during undo.

**Taunt.** Use the same source-aware eligibility as `TauntTargeting.Sources` and the merged `TauntedStatus`. Apply the reduction once in the existing incoming damage pipeline for each qualifying recipient. If one area attack hits A (rank 2), B (rank 3) and C, and only A and B taunted the attacker, their respective reductions are 10%, 15% and 0%. Preserve the shared damage order, rounding and shield settlement, and use the same rule for preview.

**Recon and turn boundaries.** Keep the current exposure stacking and reserve cleanup. An unused move can be undone with its exposures restored. Once the applied increase affected damage, the move can no longer be undone. An enemy returning to its waiting area loses the debuff and re-entry does not recreate it. Use side-turn boundaries for shields and terrain. No new day/night rules are needed. Ground Compaction currently authors an opposing-turn-end countdown. The requested next-originator-turn-end expiry is an explicit timing change. Express it through the existing terrain lifetime owner, skip the creation turn's end and verify expiry on both sides.

## 5. Ownership and cost of change

**Approach.** Extend the current category pools, growth definitions, recovery and shop transactions. This keeps a category edit in its SO, a price edit in the relevant facility SO, and an effect change in the trait's definition/rule. A separate training inventory would duplicate owned traits and need synchronization with specialization, purchases and battle preparation. A second common/exclusive classification would duplicate pool membership. Neither is needed. Factories may register definitions and bind services under the existing Framework contracts. They do not settle purchases or calculate combat effects.

| Example | Present coupling and proposed edit | Knowledge and verification required |
|---|---|---|
| Move Taunt from Heavy to Rider | Edit the two pool references. Keep the shared effect SO and training availability. | Understand category ownership, specialized Main union and candidate filtering. Verify eligible/excluded units, default overlap and 1–3-choice validation. No taunt AI edit. |
| Tune the second training price | Old enhancement offers identify retired products. Add the training transaction once. Subsequent price edits affect only training SO data. | Verify displayed quote, ability to afford, exact debit, one-purchase limit and stale-command rejection through Core/Buffer. Combat internals are independent of price. |
| Tune a swap shield | Existing movement and shield systems meet at movement settlement, receipts and turn expiry. Keep this necessary connection explicit. Later amount edits affect only the ranked SO values. | Verify shield absorption, two recipients, stacking, expiry and undo together. Other facility rules are independent. |

**View handoff.** Publish immutable facts via the existing Buffer families: facility kind, visit identity, available offers and prices, action availability/failure, owned instance identity, effective trait rank, next rank and receipt. Publish actual variable-length level-up choices. Let View submit selection/exit through existing command ownership. The downstream View owner can implement the facility name, selection and refresh from these contracts. InGame SO access stays on the Core side.

## 6. Acceptance before implementation is called complete

1. Verify production SO references and each eligible player category at levels 2 and 3, including specialized Main, native/default exclusions, repeated unchosen candidates and sequential pending steps. Exercise valid counts 1, 2 and 3+, and a reachable zero-candidate configuration that fails DB initialization with unit, step and path.
2. Exercise each facility through the real Core service/Buffer boundary. Verify exact Run gold, receipt, visit identity and one-purchase limit. Cover duplicate/stale commands, insufficient funds and no partial state change. Test full-health/injured Main, current HP preservation, ordinary unit-only offers and legacy enhancement rejection.
3. Train an off-deck Sub and a Main, choose among all eligible traits, isolate duplicate unit instances, advance 1→2 and 2→3 on separate visits, exclude rank 3 and unlisted traits, exit without spending, and progress with no eligible units. Check query/preview/battle agreement, an old legal acquisition removed from its category pool and a valid old Enhance alongside the new level-3 Acquire.
4. Validate each row's effective spec at all ranks, plus representative combat outcomes for each changed trigger, amount or lifetime. Include both sides, area kills, conditional damage, shield budget retention, taunt provenance, terrain expiry, recon rollback, swap rollback and reserve cleanup. Preserve deterministic RNG and publication order.
5. Run required EditMode Core and CoreIntegration regressions, including production `DataBaseAssetTests` after SO edits, through the approved Unity AppData execution path. Keep View/PlayMode acceptance as a separate owner handoff. The checks above are planned verification.

**Review items.** Exact training prices await balance input. The content baseline and the proposed treatment of uncategorized legacy entries are reviewable authoring decisions. No new production price or extra source of consumables is assumed.

### Source anchors

- [Category progression SO](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/TraitGrowth/Item/CategoryTraitProgressionDataSO.cs) and [production growth assets](../../../Assets/ProjectL/DataBaseSO/InGameGroup/TraitGrowth).
- [Core growth validator, rules and sampler](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Roster) and [owned spec resolution](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Enhancements/OwnedUnitSpecResolver.cs).
- [Shop transactions](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Core/ShopTransactions.cs) and [recovery transaction contract](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Core/IRecoveryOperationTransaction.cs).
- [Trait category inventory](2026-10-28-trait-system-design.md), [default trait composition](2026-11-01-category-default-traits-design.md) and [source-aware taunt targeting](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/EnemyAi/TauntTargeting.cs).
