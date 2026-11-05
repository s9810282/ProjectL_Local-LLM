# Cowboy and Frenzy — DB / View Handoff

**Date:** 2026-10-16 (Asia/Seoul)  
**Recipient:** Combat View / content implementer  
**Status:** AC1–AC2 and CF0–CF6 logic acceptance complete; CF7 Core key registration complete. Remaining CF7 content registration and manual gameplay acceptance are pending.  
**Delivery:** Prepared locally for the user to forward; not sent externally.

Continue with the remaining CF7 tasks in section 5. Core already resolves Frenzy; no additional Core feature implementation is requested. This document is the task source for the remaining CF7 DB/View work. The Cowboy plan (Git history: `2026-10-16-cowboy-frenzy-plan.md`) retains implementation history and evidence; the approved design (Git history: `2026-10-16-cowboy-frenzy-design.md`) remains the policy/balance source.

## 1. Receiving state

The inspected checkout is HEAD `4481c2f8a67eb7314f488d8d9f532c07990d061c` with uncommitted implementation changes. That commit alone does **not** contain the completed work. The user handles commit/push/branch transfer. On receipt, verify the contracts below exist before starting; preserve unrelated working changes.

| Area | Delivered |
|---|---|
| Universal action maximum | `CharacterActionCostRules.Maximum = 6`; shared creation/validation/reset/refund consumers migrated. Current AP remains variable. |
| Frenzy authoring | Existing Unit SO Traits list accepts `FrenzyTraitAuthoring`; `priorityReductionPercent` defaults to5, valid range1..100. Fresh Data export and shared UnitSpecAdapter conversion are complete. |
| Core hit/wave rules | Each delivered living-hostile hit adds its source percentage, including zero HP damage. Each direct basic/wave kill grants one free area wave; wave kills grant more. Indirect deaths grant no credit. |
| Priority / expiry | Additive total applied once, floor, minimum1. Pending scheduling refresh and next-own-turn-start expiry are implemented; Intimidation remains independent. |
| Prediction / publication | Public preview and AI exclude earned waves. Committed timeline forecast models known expiry. One accepted result publishes all waves atomically. |
| Keys | `EntityKey.Unit_Cowboy = 19`, `TraitKey.Frenzy = 120`, `CombatEventKind.FrenzyWave = 30` are already defined. Do not add duplicate keys. |
| Production content | No Cowboy Unit SO, UnitVisual or Frenzy TraitVisual asset/table registration was performed. No acquisition/loadout/spawn route was selected. |
| Presentation | Existing ordered playback can skip unsupported wave context and continue ordinary health/death events. Dedicated wave animation and inflicted-status HUD were not implemented. |

The character display name is **카우보이**; the trait display name is **광분**. Gameplay details remain in design sections 2.1–2.3.1; do not reopen them as missing content decisions.

## 2. Content settings to apply

This is an authoring checklist copied from the approved design, not an independent balance source. If changed later, update the design first.

| Unit SO field / setting | Confirmed value |
|---|---|
| `key` | `Unit_Cowboy` (19) |
| `maxHealth` / `physicalDefense` |25 /0 |
| `attackPower` / `attackRange` / `attackCost` |10 /1 /4 |
| `priority` / `delay` |15 /10 |
| `category` | Rider |
| `moveRange` / `moveCost` |99 /2 |
| Maximum action cost | Shared6; no editable per-unit maximum field |
| `targetingMethod` | Existing registered method1, linked below |
| `wallDelivery` / `wallPropagation` | RequireClear / FromImpact |
| `traits` | One Frenzy entry, `priorityReductionPercent = 5` |

Reuse [TargetingMethod_1.asset](../../../Assets/ProjectL/DataBaseSO/InGameGroup/Targeting/Item/TargetingMethod_1.asset), GUID `370b1888064844bc8421c2bf81b89916`: radial-square5 / single1 / unrestricted angle0 / splash0 / splashPercent0. Range1 includes diagonals under existing geometry. Ordinary targeting stays Single; free all-target waves do not require another method asset. Preserve normal wall blocking; do not enable Lobbed.

MoveRange99 uses the existing movement rules. Ordinary movement costs2 per step; Rider can refund its eligible movement cost once under its existing rules. Initial attack4 and free waves do not repeat refunds. Delay10 is confirmed; the earlier request for0 is superseded.

Before activating production content, obtain the still-unspecified **owner eligibility, UnitRole/footprint, actual prefab/icons and acquisition/spawn destination** from the user/content owner. Inspector defaults and isolated test fixtures are not production approval. No need to ask again for the confirmed stats above. An absent entry-route decision does not prevent preparing other supplied assets, but do not silently replace a loadout or add a shop/spawn entry.

## 3. Public contracts for View

Repository-relative source links:

| Source | Use |
|---|---|
| [CharacterData](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Character/CharacterData.cs) | Read `Debuffs`; `ToFacts()` preserves the immutable committed snapshot. |
| [CharacterDebuffState](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/CharacterDebuffState.cs) / [FrenzyDebuffState](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/FrenzyDebuffState.cs) | `Debuffs.Frenzy == null` means no inflicted Frenzy. Otherwise `AccumulatedReductionPercent` is the victim's accumulated **long** percentage; it can exceed100. |
| [FrenzyTraitStatusSnapshot](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Snapshots/Traits/FrenzyTraitStatusSnapshot.cs) | Owned trait settings, not the inflicted victim total. Keep ability ownership and inflicted status distinct. |
| [CombatFrenzyWaveEvent](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/CombatFrenzyWaveEvent.cs) | Read-only `ActorId`, one-based `WaveIndex`, `AttackGroup`, ordered `Targets`. Context precedes that wave's ordinary health events. |
| [CombatCharacterEvent](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/CombatEvent.cs) | Expected/final facts and damage origins carry actual health/death presentation. Wave context is not a substitute for these events. |
| [CombatTimelineQuery](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CombatSystem/CombatTimelineQuery.cs) | Existing forecast query already incorporates expiry. Consume returned entries; no View-side reordering or expiry simulation. |

Important consumption rules:

- `AttackResult.Hits` describes the initial attack. Do not expect it to enumerate every free-wave hit; consume ordered result events and final Buffer facts.
- All waves belong to one result/command. Do not submit extra attacks, spend AP, count kills or calculate new targets from View.
- Do not derive effective Priority from `Stats.Priority` and the victim percentage. Core owns the calculation and schedule. Forecast entries report restored Priority for a future turn that starts after expiry, even while the current victim snapshot remains debuffed.
- Expiry occurs after the reduced schedule selects that victim's next turn, before its actions. Do not clear status when it merely appears in a forecast slot. Reserve/night transitions preserve it; death/battle cleanup use existing Core owners.
- A hit may leave the rounded scheduling interval unchanged. Do not force a visible timeline displacement after every hit.
- Initial-only attack preview and expiry-aware timeline forecast are different contracts; do not add predicted wave damage to the hover preview.

## 4. Presentation scope

Remaining CF7 requires actual unit/trait visual registration and working existing health/death/final-state playback. Current `OrderedCombatPresentation` logs its existing unsupported-event warning once per event type/attachment for `CombatFrenzyWaveEvent`, skips the marker, and continues health events. This fallback was tested through the real presenter capability predicate; dedicated gameplay visuals have not been manually accepted.

A dedicated wave animation or inflicted-Frenzy HUD is **optional follow-up presentation**, not an already-authorized redesign. If requested, plan it at existing event/HUD assembly seams. For an inflicted entry, consume the victim's committed total and distinguish its stable identity from an owned Frenzy ability; reuse the current Intimidation status formatter if that work exists on the receiving branch. Do not assume the separate [Shielder View handoff](2026-10-15-shielder-intimidation-view-handoff.md) has been implemented. If animation is added, preserve ordered completion/cancel/resource-release contracts and allow later health events/finalization to proceed.

## 5. Remaining CF7 task list and file scope

1. Confirm the missing content inputs in section2 and inspect the receiving branch for completed contracts/parallel View work. Record which inputs remain absent.
2. Create `Assets/ProjectL/DataBaseSO/InGameGroup/Unit/Item/Unit_Cowboy.asset` with supplied role/eligibility/footprint and the confirmed settings. Use the existing [UnitDataSO](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Unit/Item/UnitDataSO.cs) Traits authoring path; no new trait SO/table/converter.
3. Create matching Cowboy UnitVisual and Frenzy TraitVisual items with the supplied actual references. Existing schemas: [UnitVisualDataSO](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/Table/Unit/Item/UnitVisualDataSO.cs), [TraitVisualDataSO](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/Table/Trait/Item/TraitVisualDataSO.cs). Use keys19 and120 respectively.
4. Register items in existing tables below; preserve GUIDs and unrelated entries. Do not re-add Core keys or modify the targeting table.
5. Add focused asset acceptance to `Assets/Tests/EditMode/Framework/CowboyAssetTests.cs` with its `.meta`. Verify the new SO exports the confirmed values/trait and resolves the shared method and visual keys through existing queries/conversion. Apply only an explicitly supplied acquisition/spawn instruction.
6. Perform the focused checks in section6. Record exact completion/evidence and unresolved manual/content inputs here; stop for CF-P4 review.

Existing tables to edit:

- [UnitDataTable.asset](../../../Assets/ProjectL/DataBaseSO/InGameGroup/Unit/UnitDataTable.asset)
- [UnitVisualDataTable.asset](../../../Assets/ProjectL/DataBaseSO/ViewGroup/UnitVisual/UnitVisualDataTable.asset)
- [TraitVisualDataTable.asset](../../../Assets/ProjectL/DataBaseSO/ViewGroup/TraitVisual/TraitVisualDataTable.asset)

The separate key-only work already changed `EntityKey.cs` and the undefined-key fixture from19 to9; these are completed, not remaining tasks. New/renamed assets and C# require matching `.meta` files; preserve existing GUIDs. Do not delete legacy Missing Script assets or edit FieldCamera/Architecture.

## 6. Verification and evidence to reuse

The user's latest policy is focused verification: no deliberate RED, full-suite or unnecessary repeated checks. This documentation handoff runs no Unity/tests. Receiving implementation should verify new asset/reference/conversion behavior and the directly changed presentation path only. Run the required `DataBaseAssetTests` for DB asset changes, record any known baseline failures separately, and reuse earlier pure Core evidence. Expand only for a concrete failure or unresolved changed contract.

Prior native evidence, **not new handoff execution**:

| Slice | Result / source |
|---|---|
| CF1–CF2 |45 new plus12 directly affected existing cases passed; plan section12 (Git history: `2026-10-16-cowboy-frenzy-plan.md#12-cf1cf2-execution--2026-10-16`). |
| CF3–CF6 | Latest21/21 unique cases pass across scoped runs,20 new plus1 existing; plan section13 (Git history: `2026-10-16-cowboy-frenzy-plan.md#13-cf3cf6-execution--2026-10-16`). Includes preview/AI, free-wave costs/refund, direct credit, expiry/query rollout, failure atomicity and marker-to-health playback. |
| CF7 key only | Native compilation and directly changed rejection method7/7, exit0; [XML](../../../.superpowers/sdd/2026-10-16-cowboy-frenzy/CF7-key-only.xml). |

Local `.superpowers` evidence may not travel with a branch; plan execution records retain counts and limitations. Ask the sender for raw evidence only if needed, instead of assuming missing local XML means implementation is untested.

Manual acceptance after content/entry references are available: Inspector percentage editing; single ordinary aim; kill→successive local area damage; repeated Priority reduction; delayed next own turn and expiry; later restored ordering; initial-only preview; one Rider refund and later movement; maximum6 HUD. Integer rounding may keep a wait unchanged. These manual checks are still pending, not implied by the passing Core tests.

Unity6000.0.73f1 on this machine requires the approved AppData-access execution path. The prior `CurlRequestCache.db` startup failure was a Codex sandbox restriction; do not upgrade Unity or delete caches for it. Check for an existing open project/lock before batch execution, and do not terminate the user's Editor. The existing local runner is `.superpowers/sdd/2026-10-16-cowboy-frenzy/Run-CfTests.ps1`; choose a new run name and a focused filter.

## 7. Architecture and completion record

SSOT: Unit SO owns authored settings, Core owns accumulated state/rules/scheduling, Buffer owns committed read snapshots, View owns presentation. ViewGroup may contain Unity references; View does not query InGame DB for runtime rules or write Core/Buffer state. Do not add a debuff timer, second kill/stack counter, manager, event bus, factory or movement mode. Reuse existing FW registration, input, publication and lifecycle contracts. Presentation work should require the public contracts above, not private wave or scheduler internals.

Apply all five SOLID principles and the [project change-locality criteria](2026-09-30-combat-view-design-session-handoff.md): keep content assembly apart from feature rule implementation; check modification, understanding and verification scope. Core defects go into the [Core boundary handoff](2026-09-24-view-core-data-boundary-handoff.md), rather than being patched in View. English implementation documents; Korean code/XML comments; UTF-8/LF and existing namespace/style conventions. The user performs commits/pushes.

**Receiving execution record:** Pending. Record supplied content decisions, changed files, focused native/manual results and any remaining dedicated-visual work here. This handoff itself changes documentation only and does not activate content or complete CF7.
