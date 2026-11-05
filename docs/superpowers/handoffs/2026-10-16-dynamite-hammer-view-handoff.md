# Dynamite Hammer — View Integration Handoff

**Date:** 2026-10-16 (Asia/Seoul)  
**Recipient:** Combat View implementer  
**Status:** Core support and the saved production Unit SO are delivered locally. View resources, paired table activation, and availability remain unconnected. The user requested creation of the Unit SO in the Core session and handoff of only the remaining View connections. No external message, commit, or push has been made.

## 1. Already delivered — do not recreate

- Production Unit SO: [Unit_DynamiteHammer.asset](../../../Assets/ProjectL/DataBaseSO/InGameGroup/Unit/Item/Unit_DynamiteHammer.asset), GUID `b8b7fa1d0a184ded941b09423dc9ec24`.
- Identity: `EntityKey.Unit_DynamiteHammer = 24`, present in the local working tree. Receiving branches must include the existing uncommitted Core change; preserve all old values and reserved Shielder23.
- The SO contains the confirmed balance values and existing `KnockbackTraitAuthoring` with distance1. It references existing targeting method7, GUID `e5e42fa120684bc899e2050a79576e8a`: shape3 / Single1 / FrontAndSides2 / None0 / percent0. Do not create another Unit SO or targeting method for this handoff.
- The unit is PlayerOnly / Regular / 1×1, Rider, with health20, power6, defense0, priority30, delay10, move range99/cost2, attack range1/cost4. Maximum6 comes from the shared rule. Critical0 requires no new field or subsystem.
- Core already handles normal damage, one-cell diagonal/cardinal Knockback, movement failure/immunity without canceling damage, Rider refund, and Buffer publication. Design (Git history: `2026-10-16-dynamite-hammer-design.md`) and plan evidence (Git history: `2026-10-16-dynamite-hammer-plan.md`) carry the contracts and tests.

The SO is saved but intentionally **not yet in UnitDataTable**. Every registered unit must have corresponding valid UnitVisual data; activation is paired with the View entry below. This is a registration dependency, not missing Core authoring. Do not report the unit as available in normal play until the selected entry route works.

## 2. Remaining View connections

- [ ] Select a field prefab, info/hand icon, timeline icon, and desired attack/reaction presentation settings. No dedicated Hammer resource or approved placeholder was found during the Core session. A valid non-null field prefab is required; optional-null icons/presentation can use existing fallbacks only when deliberately selected.
- [ ] Create `Assets/ProjectL/DataBaseSO/ViewGroup/UnitVisual/Item/UnitVisual_DynamiteHammer.asset` and unique `.meta`. Set key24, display name `다이너마이트 해머`, and the selected references. Reuse existing prefab component and presentation contracts.
- [ ] Append the **existing delivered Unit SO** once to `Assets/ProjectL/DataBaseSO/InGameGroup/Unit/UnitDataTable.asset` and the new visual once to `Assets/ProjectL/DataBaseSO/ViewGroup/UnitVisual/UnitVisualDataTable.asset` in the same integration change. Unit GUID is `b8b7fa1d0a184ded941b09423dc9ec24`. Do not duplicate a table type or bypass the cross-table visual check.
- [ ] Reuse `Assets/ProjectL/DataBaseSO/ViewGroup/TraitVisual/Item/TraitVisual_Knockback.asset`. No new trait, status behavior, or HUD rule is requested.
- [ ] Choose and connect the intended acquisition/test route with the user. Record the exact files and any starter replacement/count. Neither initial deck, shop nor test entry was selected in the Core session; do not infer a change to `PlayerLoadout_1.asset`.
- [ ] Play the published attack/damage/movement results through existing View presentation and cleanup. No new Core calculator, Factory, input controller, or unit-name branch is needed.

Resource and entry choices are the remaining inputs for this View phase. They do not require changing the confirmed Unit SO values. Follow normal repository Task/Phase review, preserve existing GUIDs and old assets, and leave FieldCamera/Architecture to their owners. Implementation documents are English; code/XML comments are Korean.

## 3. Contracts the presentation must preserve

For player forward `+Y`, attack range1 includes front-left/front/front-right and left/right; rear3 are excluded, and only one eligible cell is selected. For attacker `(3,3)`, a target at `(4,4)` moves to `(5,5)` when free. There is no extra explosion or splash implied by the name.

View consumes Core AttackAims/preview/results and published Character facts. It must not recompute targeting, damage, displacement direction, refund state, or final position from the SO or model rotation. Preview `TargetOccupiedCells` describes the original footprint; movement/final facts provide the new position. A directly killed target has no Knockback movement and its field anchor is cleared. Blocked/immune movement retains the damage already applied.

Keep gameplay balance in the delivered InGame SO and presentation references in UnitVisual. Shared method7 also serves Ballista; preserve it and Hammer's unit-owned range1. If targeting later changes independently, propose a distinct method rather than mutating this shared asset.

## 4. Receiving-side acceptance

- [ ] Load the actual catalog after paired registration and resolve key24, the delivered Unit SO, method7, and the matching non-null visual prefab. Run mandatory `DataBaseAssetTests`, `TargetingAngleAssetTests`, and `DynamiteHammerAuthoringTests` after DB edits.
- [ ] Preserve the historical targeting assertions. The Core SO-delivery change already scopes `UnchangedUnits_KeepOriginalMethodsAndZeroAngle` to its explicitly named historical assets and adds a separate saved-Hammer assertion; do not recreate that adjustment or classify Hammer as unrestricted.
- [ ] Confirm name, field model, hand/info/timeline presentation, selected acquisition/test route, and actual availability. Check aiming, ordinary damage, both front diagonals, cardinal/side push, blocked/immune push, death and one successful hazard landing in Unity.
- [ ] Verify presentation completion/cancellation and normal resource cleanup. Reuse Scope/Handle and applicable FW contracts; any new UI input follows `Assets/AGENTS.md`.

Keep modification and verification in the selected resources, UnitVisual, paired registration lists, agreed route, and their integration tests. Preserve SSOT/SOLID and the existing Core/Buffer ownership; do not add unrelated camera, trait or flow changes.

## 5. Evidence and delivery limits

DH0's native component baseline162/162, DH1's temporary authoring5/5, and DH2's combat/publication22/22 plus one actual wall regression are recorded in plan sections9–11. Those are prior results. The saved Unit SO delivery and its native asset/DB acceptance are recorded separately in plan section13; use that record for the operational asset, not the earlier temporary-SO evidence.

Evidence is local/ignored under `.superpowers/sdd/2026-10-16-dynamite-hammer-plan/`; source and assets are still uncommitted in the inspected checkout (`de8e2586` base). No final artwork, paired production registration, acquisition, or manual View gameplay acceptance has been completed. The remaining work is the View integration checklist above.