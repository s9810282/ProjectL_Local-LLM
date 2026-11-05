# Legacy Enhancement Retirement R1 — View Content Handoff

**Date:** 2026-10-29. **Status:** request prepared, not externally delivered. R1 verification is recorded in the [retirement plan §7.2](../plans/2026-10-29-legacy-enhancement-retirement-plan.md). This is a phase-local owner note, not completion of R3 or View work.

## Current Core boundary

Seven old Enhancement product identities are reserved and have no active product content: LuckyBullet1, DoubleShot3, DoorBreaching4, ContemptForWeak5, ChainExplosion6, PoisonThorn7 and PrecisionShot8. ChangeRider2 remains reserved for the separate future Main specialization work; no replacement Main shop is enabled here.

The six dedicated trait implementations (DoorBreaching130, ContemptForWeak140, PrecisionShot150, DoubleShot160, LuckyBullet170, ChainExplosion180) were removed. Their numbers/names remain compatibility reservations and availability reports `Retired`. Core supports31 executable trait identities: all22 new traits plus nine surviving legacy traits. Five other deferred traits remain deferred. This count is implementation support, not production-pool activation.

The empty enhancement table, shop stage root/detail/node, shared shop/publication contracts and legacy purchase rejection remain. Do not reopen old Sub enhancement purchases while adding new content.

## Temporary Heavy progression

The user explicitly left new SO/pool authoring to View and authorized temporary cleanup. R1 removed `TraitGrowth_DoubleShot.asset` and `TraitGrowth_DoorBreaching.asset` with their metas and growth-table references. Their old Heavy pool had only Knockback plus these two retired identities, so removing them could not satisfy three distinct Acquire choices.

`CategoryTraitProgression_Heavy.asset` therefore temporarily has `steps: []` and `pool: []`. The Heavy category row, Knockback definition/table entry, ordinary XP/stat growth and general growth contracts are preserved. Main/Rider/Cannon already had empty progression steps/pools and were not changed. No replacement production traits or balance numbers were invented. R1 test-only payloads are not approved production defaults.

Core already supports the five new Heavy traits: Cohesion, Sacrifice, Taunt, Entrenchment and SteadfastWill. Position Swap remains deferred until completed-movement undo is designed/implemented. See the [trait design coverage and tuning](../specs/2026-10-28-trait-system-design.md). Category membership remains SO-configurable, not a Core allowlist.

View/content follow-up:

- Author approved normal and enhanced TraitGrowthDefinition SOs and register them in the existing table. Cohesion DEF/shield magnitudes and unapproved enhanced values still need content decisions; do not silently copy test numbers.
- Connect the intended common/Heavy candidate pool and chosen step sequence. The former temporary Heavy sequence was Acquire step10 then Enhance step20, both unlocked at level2; restoration or a different production sequence is a content decision.
- Preserve three distinct valid choices at every reachable Acquire, meaningful eligible Enhance choices, native-trait overlap rules, deterministic offers and the existing exact result-acknowledgement contract. Validate the actual unit profiles, not only a list of candidate counts.
- Run production catalog/authoring and growth integration checks after activation. UI/manual acceptance belongs to the owning workers. No View/Flow/UI implementation is included in R1.

## Unused View rows to retire

The following display assets remain intentionally untouched under Core ownership:

- `Assets/ProjectL/DataBaseSO/ViewGroup/TraitVisual/Item/TraitVisual_DoubleShot.asset` and its meta/table entry.
- `Assets/ProjectL/DataBaseSO/ViewGroup/TraitVisual/Item/TraitVisual_DoorBreaching.asset` and its meta/table entry.

Remove these rows with their exact GUID references through the View owner. Preserve common TraitVisual/card/playback handling, ThornField text and the existing new-trait presentation work. New Heavy content does not imply that its playback/UI is already accepted.

## Remaining checkpoints

R2 follow-up hook cleanup, R3 final retirement coverage/handoff and R4 combat-cost removal remain separate unauthorized phases. The existing trait F1 historical37-producer evidence was not rewritten; its current-coverage update belongs to the coordinated R3 task. Main specialization is separate. No message to another worker, commit or push was performed.
