# Shielder and Intimidation View Handoff

**Date:** 2026-10-15
**Recipient:** Combat View implementer
**Status:** SI1-SI4 Core support remains complete. SI5 was implemented/verified locally on 2026-10-18, then its generated formatter/tests and Presenter seam were removed by the user's explicit retention cleanup. Only the four documentation changes and Colored model/texture/material/prefab assets are retained; no SI5 presentation code is delivered. SI6 production content remains unregistered. See the latest cleanup record in the Colored verification document section13.

If SI5 is separately authorized again, start from this document and recheck the receiving branch. Core already applies, consumes and publishes Intimidation; the removed View connection must be provided again before claiming inflicted-state HUD delivery. The former SI5 task block was moved here from the Core and content plan (Git history: `2026-10-15-shielder-intimidation-plan.md`). This document is the single task source for SI5; do not execute a second SI5 from an older copy of that plan.

The approved design (Git history: `2026-10-15-shielder-intimidation-design.md`) remains the source for gameplay policies and character values. The original plan retains SI1-SI4 implementation history, key reservations, the verification command and the SI6 content checklist. This handoff changes documentation ownership, not the approved implementation scope.

## 1 Starting state and ownership

The inspected checkout is at `41544a1d` (`[천명] 위압, 재생 특성 및 유닛 SO 구현`). Recheck the receiving branch for the contracts below before implementation; do not recreate completed Core work.

| Phase | Delivered behavior | Current owner and next action |
|---|---|---|
| SI1-SI2 | Immutable target debuffs, pure calculations, snapshot preservation, stale equality, Buffer exposure and death/completion cleanup | Core complete; read the published contract |
| SI3 | Delivered-hit application, reduced whole attack, reaction expiry, immediate pending-action rescheduling, preview/AI/publication integration | Core complete; View does not reproduce these rules |
| SI4 | Existing Unit SO trait input, fresh immutable export and shared UnitSpecAdapter conversion | DB/Core complete; no production balance values selected |
| SI5 | Distinct inflicted-status HUD entry, icon and stage description | Historical local implementation removed by user request; future View work requires separate authorization and revalidation |
| SI6 | Shielder content, visual registration, entry route and real gameplay acceptance | Joint DB/View/Core integration; use section 6 before starting |

The character display name is **쉴더**, the trait display name is **위압**, and the category is **Heavy**. Character numbers and attack targeting remain in design section 2. Production Shielder/Intimidation assets and a playable entry route have not been registered by SI1-SI4.

## 2 Published contract consumed by View

Repository-relative source paths:

| Source | Contract relevant to SI5 |
|---|---|
| [CharacterData](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Character/CharacterData.cs) | `Debuffs` exposes the immutable published value; `ToFacts()` preserves it |
| [CharacterDebuffState](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/CharacterDebuffState.cs) | `Intimidation == null` means no inflicted Intimidation; inspect the value rather than relying on Empty reference identity |
| [IntimidationDebuffState](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/IntimidationDebuffState.cs) | `AttackReductionPercent` is the applied percentage; `PriorityPenalty` is the applied flat penalty, with zero representing the first stage |
| [FieldUnitPresenter](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/FieldUnitPresenter.cs) | `RenderUnitHud` already receives CharacterCombatFacts, whose `Debuffs` is available; it currently lists only `Stats.TraitStatuses` |
| [TraitVisualTable](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Queries/ViewDataBase/ViewTable/TraitVisualTable.cs) | Reuse the presenter's existing `ITraitVisualTableQuery` and `TryGetTraitVisual(TraitKey.Intimidation, out visual)` icon lookup |
| [UnitHUD and UnitHUDStatus](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/UnitHUD.cs) | Existing `SetStatuses` consumes unique keys, Sprite icons and descriptions; it owns slot reuse, hover and pinned descriptions |

`Stats.TraitStatuses` describes abilities owned by the unit. `Debuffs.Intimidation` describes an inflicted effect on this instance. A unit may own Intimidation and also be intimidated. Its owned trait uses the current key `Intimidation`; the inflicted entry must use `debuff:Intimidation` so both can coexist.

The authored caster parameter is named `PriorityReduction` and must be positive. The published victim field is named `PriorityPenalty` and may be zero. SI5 displays the victim's published fields, including when another caster supplied the second-stage penalty. It must not look up a caster or infer values from the victim's owned trait.

| Published victim state | HUD result |
|---|---|
| `Debuffs.Intimidation == null` | No inflicted Intimidation entry |
| Percentage20, PriorityPenalty0 | `공격력 20% 감소. 다음 공격 종료 후 해제.` |
| Percentage20, PriorityPenalty3 | `공격력 20% 감소, 우선도 3 감소. 다음 공격 종료 후 해제.` |

These are test examples, not production balance defaults. Percentage and penalty in the displayed description come directly from the current published state.

Core resolves the entire next accepted attack with the reduction, then clears both reductions. Reactions also consume it. Preview, rejected attacks, movement, waiting, turn completion and night recall do not consume it. Death and battle completion clear it. View refreshes from the resulting state; it never counts hits, starts an expiry timer, clears a Buffer field or predicts expiry from a future turn slot.

## 3 SI5 file scope and boundaries

Create:

- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/CombatDebuffStatusFormatter.cs`
- `Assets/Tests/EditMode/Framework/IntimidationStatusDisplayTests.cs`
- Matching new `.meta` files with unique GUIDs.

Modify `FieldUnitPresenter.cs` at its existing HUD status assembly boundary. Reuse `UnitHUD` and `UnitHUDStatus`; modify them only if a test exposes an integration defect, preserving existing click, pin and input contracts.

The planned formatter contract is:

```csharp
CombatDebuffStatusFormatter.Create(CharacterDebuffState debuffs, Sprite icon) -> UnitHUDStatus?
```

It returns null when no Intimidation is present and otherwise returns the stable `debuff:Intimidation` entry with the description for the published stage. The presenter obtains the existing TraitVisual icon, appends the returned entry to its reused `_hudStatuses` list, and calls the existing `hud.SetStatuses` once. Owned trait keys and their current presentation remain intact.

Preserve SSOT, SOLID, JMH Framework and DB boundaries: Buffer owns accepted status, Core owns evaluated damage/Priority and scheduling, the formatter owns text, and UnitHUD owns interaction/presentation state. No persistent status service, second victim-state collection, new event subscription, per-frame rule calculation, InGame DB read, or new command route is needed. The existing HUD timeline already consumes Core forecast values; SI5 does not sort or recalculate them. A new numeric effective-stat panel is outside SI5.

Follow repository conventions: English implementation documents, Korean code/XML comments, UTF-8 LF, Allman braces, four spaces and existing namespaces. Use UIInputBinding/pointer handlers and EventSystem dispatch in interaction tests; do not add Button.onClick listeners. Preserve FieldCamera, Architecture, unrelated work and legacy Missing Script assets. Core changes outside the supplied read contract require a Core request and discussion under AGENTS.md. No commits or pushes; stop after the Phase.

## 4 SI5 implementation tasks

These four tasks were transferred from the original SI5 block and were implemented in Colored test Phase C2. The checked boxes below record that historical verification, not code currently delivered: the user later requested removal of the formatter/test/Presenter changes. The execution record is in section8 and the actual cleanup outcome is linked below.

- [x] Add tests for Empty/no debuff entry, `(20,0)` attack-only text, `(20,3)` combined text, and expiry/removal. A holder who is also intimidated must show its owned trait and a separate inflicted status; reuse on another entity must clear old status/pin state.
- [x] Format published parameters directly. Use descriptions equivalent to the examples in section 2. Do not recompute effective AttackPower or Priority in View. No new numeric stat panel is included; a future panel must consume explicit Core-evaluated values.
- [x] Connect the formatter and existing TraitVisual lookup. Verify statusRoot, description input and reused slots with UnitHUDTests; use the existing UIInputBinding/pointer handlers, not new Button listeners. Keep per-frame calculations and new subscriptions out.
- [x] Run SI5 verification with synthetic visual data, record what still needs an actual prefab/icon in SI6, and stop. This does not claim the production character is registered.

When checking reuse, include stage changes under the same stable key, removal of a pinned inflicted entry, and rebinding the HUD to another entity. Keep legitimate same-entity pin behavior and verify the description updates from the current data. Synthetic visual data lets SI5 proceed before production icons and prefabs are chosen.

## 5 SI5 verification and completion record

Use native Unity EditMode tests. First check whether this project is already open; do not start a second Editor against a locked project or terminate the user's Editor. The original verification protocol (Git history: `2026-10-15-shielder-intimidation-plan.md#verification-protocol`) contains the batch command, paths and evidence conventions. A compilation-only check is not an EditMode pass.

| Stage | Fixtures |
|---|---|
| Fresh baseline | `ProjectL.Tests.Framework.UnitHUDTests`; `ProjectL.Tests.Framework.CombatDisplayReuseTests` |
| SI5 final selection | Both baseline fixtures plus `ProjectL.Tests.Framework.IntimidationStatusDisplayTests` |

Capture meaningful failing behavior before implementation, then fresh final results. Verify that the planned fixtures were discovered. New tests should exercise the formatter and the real presenter/HUD boundary, including coexisting owned/inflicted entries and reuse; only testing string creation is insufficient. Retain any additional mandatory regression/integration checks required by the actual dependency change.

At the Phase stop, record changed files/contracts, exact test counts and log paths, SSOT/SOLID/FW checks, remaining prefab/icon wiring and any manual Unity checks separately. Compare unrelated failures against a fresh baseline; do not silently fix them in SI5. Update SI5 checkboxes and append the SI5 execution record here. The Core/content plan retains SI1-SI4 history; do not add a second SI5 task list there.

Core verification inherited from SI4, not rerun for this documentation handoff: new18 cases all passed; required selection212/213 retained the exact existing Stage1101 terrain assertion; whole suite6,748 total with6,658 passed,89 pre-existing failures and1 explicit manual skip. Fresh independent SI4 review found no actionable issue. See the SI4 execution record (Git history: `2026-10-15-shielder-intimidation-plan.md#si4-execution-record--2026-10-15`) for exact comparisons and the local evidence directory. `.superpowers/sdd/` logs are local ignored artifacts and may not accompany another checkout. These results do not establish SI5 correctness or production gameplay acceptance.

## 6 SI6 ownership and unresolved content

SI6 remains in the Core and content plan (Git history: `2026-10-15-shielder-intimidation-plan.md#phase-si6--shielder-content-and-gameplay-acceptance`); its task list has not been moved or duplicated. Finish and review SI5 before beginning SI6. Confirm the existing SI6 input checklist: production reduction percent and Priority reduction, owner eligibility, movement range, Regular/1x1 acceptance, prefab/icon choices and the exact acquisition/spawn/loadout entry route. Test values do not fill these decisions.

| SI6 responsibility | Owner boundary |
|---|---|
| Unit and targeting SOs/table registration | DB input work; use existing SI4 authoring/export contracts |
| UnitVisual and TraitVisual SOs, matching tables, prefab/icon wiring and HUD acceptance | View/content work |
| `EntityKey.Unit_Shielder` enum allocation or another CoreSystem source change | Core responsibility; coordinate with the Core programmer rather than treating all SI6 as View-only |
| Real initialization and attack/reapplication/expiry sequence | Joint integration; distinguish code tests, saved-content checks and manual gameplay evidence |

The original plan reserves EntityKey23 and targeting key6; these are reservations, not proof of registered content. TraitKey.Intimidation110 is already implemented. Recheck collisions on the receiving branch before dependent edits and retain the original plan as the allocation source. Preserve Heavy's existing category behavior; ally-protection redesign and configurable critical-hit support are separate features.

## 7 Handoff references

- Approved Shielder and Intimidation design (Git history: `2026-10-15-shielder-intimidation-design.md`): gameplay rules, character values and acceptance criteria.
- Core and content plan (Git history: `2026-10-15-shielder-intimidation-plan.md`): completed SI1-SI4 history, reservations, SI6 tasks and shared verification command.
- [Core boundary handoff](2026-09-24-view-core-data-boundary-handoff.md#16-shielder-and-intimidation-design-request-2026-10-15): cross-worker index; Core implementation notes are sections5.27-5.30.
- [Project design principles](2026-09-30-combat-view-design-session-handoff.md#4-반드시-지킬-설계-원칙): SSOT, SOLID and Framework boundaries.
- [Assets input conventions](../../../Assets/AGENTS.md): existing HUD input conventions.

## 8 SI5 execution record — 2026-10-18

Executed as C2.1 of the [Colored five combat plan](../plans/2026-10-18-colored-five-combat-test-plan-Hojin.md#phase-c2--hud-implementation-and-isolated-presentation-verification-si5-first), following the user's instruction to reuse the existing UI worker's HUD. `CombatDebuffStatusFormatter.Create(CharacterDebuffState, Sprite)` returns the nullable `debuff:Intimidation` entry using the victim's `AttackReductionPercent` and `PriorityPenalty`. The existing presenter resolves the optional TraitVisual icon, appends the entry and calls `SetStatuses` once. `UnitHUD`, its saved prefab and input/layout implementation are unchanged. C2.2 separately adds `CombatFrenzyStatusFormatter`/`debuff:Frenzy`; it does not replace this SI5 task source.

New native tests: `IntimidationStatusDisplayTests`8 and `FrenzyStatusDisplayTests`8, with a shared synthetic `CombatDebuffHudTestFixture`. They exercise absent/new-empty values, current victim parameters differing from the owned trait, same-entity stage changes while pinned, removal, pool reuse on another entity, owned/inflicted coexistence, TraitVisual and default icons, pointer handlers and Submit. The Frenzy cases also preserve long values and percentages above100. No running Buffer was edited to manufacture a result.

Verification evidence is local ignored scratch under `.superpowers/sdd/2026-10-18-colored-five-combat-test-plan/c2/`:

| Native EditMode selection | Result | Evidence |
|---|---|---|
| Fresh whole baseline, before product edits | 8275 total;8036 passed;238 failed;1 skipped | `baseline-all.xml`, full names/labels/messages in `baseline-all-failures.json` |
| New presentation tests before implementation, with null-return formatter contracts | 16 total;2 passed;14 expected missing-status/slot failures | `red-hud.xml`, `red-hud-failures.json` |
| New16 + existing UnitHUD, objective, presenter/preview, hover, body and publication checks | 75/75 passed | `green-hud-regression.xml`;59 existing cases were Passed->Passed in `focused-baseline-comparison.json` |
| Required `CombatDisplayReuseTests` after the change | 29 total;23 passed;6 failed | `green-reuse-regression.xml`; all6 names, labels and messages exactly match the fresh baseline in `reuse-baseline-comparison.json` |

Final selected total:104 cases,98 passed and6 exact inherited failures,0 new selected failures. Those inherited cases are three ActionPanel/Return tests and `HudFeedback_WarmRepeatedValues_DoNotAllocateStrings` (NullReferenceException), plus two HandCards tests (InvalidCastException). They were not repaired in SI5. The whole suite was not rerun after the narrow change; the fresh whole baseline is not a whole-suite passing claim. Independent read-only review found no product defect and identified the required reuse selection, which was then executed and compared.

SSOT/SOLID/FW/change independence: Core/Buffer remain state and rule owners; separate stateless formatters own each description; the existing presenter composes entries; the existing HUD owns interaction. No service/Factory/Builder, subscription, timer, second debuff store, InGame query, timeline sorting or input-route change was introduced. Core, FieldCamera, Architecture and production DB assets were untouched. No package installation, commit or push.

Remaining SI6/C3-C4 work: the Combat scene references `Assets/ProjectL/Prefabs/UI/CombatOnly/HUD/HUD.prefab`, whose statusRoot/icon references exist but `descriptionPanel` and `descriptionText` are currently unset. C2's synthetic HUD wires these explicitly for the behavior checks. Connect actual saved description references and TraitVisual content during content integration, then verify real attacks and removal through Core->Buffer->HUD. No new UI layout was authored here, and no screenshot/manual gameplay acceptance is claimed for SI5.

**C4 follow-up / overriding user scope (2026-10-18):** the user explicitly chose no HUD file modification and icon-only acceptance. Saved description/pin acceptance above is therefore excluded from this Colored test, not completed. C3 supplies separate test Shielder23/Intimidation content; C4 native saved-DB/public-command tests verify20% attack reduction, flat Priority3/nonstack, actual AI damage10->8 and expiry, with published facts driving the unchanged saved HUD prefab's inflicted icon creation/reuse/removal. These Shielder checks are EditMode, not full Boot mouse gameplay. Final C4 new18 cases pass; total selected187=184passed/3exact inherited DB failures. See the self-contained [Colored verification record](../plans/2026-10-18-colored-five-combat-test-plan-Hojin.md#2부-colored-5종-특성hud-검증-기록) for all five traits, actual Play boundaries and MD-only discard consequences. Production SI6 balance/acquisition/portrait/icon art decisions remain separate. No additional Core/HUD file change, commit or push.

**MD-only delivery preparation (2026-10-18):** the verification record sections8-12 now list the four documentation changes to retain and exact product changes to exclude or optionally discard. These SI5 checkboxes/results are the tested local implementation history, not delivery of formatter/Presenter code in an MD-only commit. If the product changes are discarded, remove the new9-line Presenter seam together with both formatters and dependent new tests, preserving the existing `SetStatuses`, owned/objective entries, saved HUD, pin/hover/Submit and UIInputBinding contracts. No actual discard, UI change, staging, commit or push was performed by this documentation follow-up.

**Authorized retention cleanup (2026-10-18):** the subsequent user instruction retained the four MDs plus models/texture/material/prefabs and explicitly removed experiment code/SOs. Both formatters, their new tests and the9-line Presenter seam are now removed; original `UnitHUD`/prefab/Binding behavior remains. Shielder23/test SO registration and the development entry are also removed, while existing Core calculations remain. Models and the shared PNG/material stay at their valid existing paths; they do not register playable content. See [verification section13](../plans/2026-10-18-colored-five-combat-test-plan-Hojin.md#13-모델-보존코드so-정리-실행-기록) for post-cleanup verification. No staging/commit/push.
