# Attack Choice Angle — View Handoff

**Date:** 2026-10-16 (Asia/Seoul)

**Recipient:** Combat View implementer

**Status:** Core angle implementation and available production data are complete. View acceptance has not been performed. Two character-data assignments remain conditional on their separate content being supplied.

**Source:** Approved design (Git history: `2026-10-15-attack-choice-angle-design.md`), implementation plan and verification record (Git history: `2026-10-15-attack-choice-angle-plan.md`), especially sections 9–11.

This handoff documents delivered contracts and the receiving work. It does not claim that View code changes are required. First verify the existing presentation against Core responses; retain it if it already behaves correctly. No View implementation, asset creation, commit, push, or external message was performed while writing this document.

## 1. Ownership and completion boundary

The term **InGame data** here means database rules/configuration. It does **not** mean the `RuntimeSystem/InGameSystem/CoreSystem` implementation folder.

| Area | Location | Status / responsibility |
|---|---|---|
| Attack rule implementation | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/` | Complete: angle admission, range intersection, shared query/preview/execution/AI integration, and adapter transfer |
| Authoring schema and immutable DB export | `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Targeting/Item/` | Complete: `TargetingMethodDataSO.choiceAngle` exports `TargetingMethodData.ChoiceAngle` |
| Saved gameplay configuration | `Assets/ProjectL/DataBaseSO/InGameGroup/` | ShotGun, Gravekeeper, and Ballista complete; remaining character assignments are described in section 5 |
| Presentation and interaction | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/` | Not changed by this feature; receiving View worker verifies the existing result consumers |

There is **no outstanding Core angle-code implementation request**. Do not describe the conditional SO/reference work as unfinished `CoreSystem` logic. Missing character identities and definitions belong to their separate character-content work; this handoff does not authorize View to allocate Core EntityKeys or invent balance/trait values.

The inspected checkout is `4e92adfd` plus uncommitted Phase 1–3 work. HEAD alone does not contain the delivered feature. Before work on another branch or machine, confirm the delivered enum, SO field, adapter transfer, assets, and tests are present. The user handles commits and transfer; do not commit or push.

## 2. Delivered gameplay contract

| ChoiceAngle | Permitted sector relative to the unit's faction |
|---|---|
| 0 — Unrestricted | Existing range unchanged |
| 1 — Front | Front ±45 degrees, including exact boundaries |
| 2 — FrontAndSides | Front ±90 degrees, including left and right |
| 3 — FrontAndRear | Front and rear ±45-degree sectors, including boundaries |

Player forward is world-grid **+Y**; Enemy forward is **−Y**. Owner fixes the orientation. Movement, footprint rotation, model facing, and the last selected attack do not turn the allowed sector.

The result is the intersection with the existing shape and range. It never adds cells. In particular, Fan diagonals absent from the original shape remain absent even though the sector boundary is inclusive.

Restriction applies to the primary designation. For Direction attacks, the direction intent itself must be permitted: a Front-only EightDirectionLine attack rejects Left even if that group's front-left ray overlaps the permitted sector. For an accepted primary, existing splash remains valid outside the attacker's sector and original designation range, subject to existing bounds, walls, and other delivery rules.

## 3. View consumes existing responses

The result contract did not gain a View-owned angle/facing state. View must not read InGame tables to reconstruct the sector or filter the final hits a second time.

| Existing consumer | Delivered contract and receiving responsibility |
|---|---|
| [AttackTargetingInteraction.ApplyRangeAnswer](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/AttackTargetingInteraction.cs) | Already reads `AttackRangeResult.AttackAims` and displays `RangeCells`. Use these published values for designation and highlighting. Preserve `CanAffordAttack` as the separate cost decision. |
| `AttackTargetingInteraction.ApplyAttackAnswer` in the same file | Already displays `AttackPreview.AffectedCells`, targets, cost, and blocker results. Preserve valid splash outside the designation sector. |
| [CombatAttackQueries](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatAttackQueries.cs) | Core supplies actor Owner to the common range/selection rules. This is read-only contract evidence for View, not a place to add a second filter. |

A model may have its own visual facing behavior; it is not the gameplay orientation source. No new model rotation, animation, VFX, HUD label, direction-arrow widget, or camera change is required by this handoff. Any such product decision needs its own scope.

### Development source matters

[DevBattleInitializationSource](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Dev/DevBattleInitializationSource.cs) still builds its own methods with the legacy four-argument constructor, which defaults to angle 0. It was deliberately left unchanged. Testing that Fixture source is not proof that the persisted angle settings work.

For production-data acceptance, use the existing **Database** battle-definition option and a valid combat node/entry route. [GameBootstrap](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Boot/GameBootstrap.cs) already distinguishes Fixture and Database. Do not alter Core or production assets merely because the old Fixture remains unrestricted. A future fixture update is View-owned follow-up work, not completed work in this handoff.

## 4. Receiving View acceptance tasks

- [ ] Confirm the transferred Phase 1–3 files are present; record the receiving revision and selected battle-definition source.
- [ ] With existing available units, verify the range highlight and allowed aims match the Core response. ShotGun/Gravekeeper are Front; Ballista permits front and side rays. Keep original shape, range, and Ballista's existing adjacent exclusion.
- [ ] Verify forbidden primary/direction input is not presented as selectable; stale hover highlights clear through existing interaction handling. Do not widen `AttackAims` locally.
- [ ] Verify accepted splash/affected-cell presentation is not clipped to the designation sector. Use an appropriate Core/test fixture when a production unit does not supply the needed shape/splash combination; do not change production balance for this check.
- [ ] Verify orientation remains faction-relative after movement or footprint rotation. Inspect an Enemy-facing case through a valid existing route or test fixture; model facing must not drive the result.
- [ ] Retain existing AP/blocker feedback and response sequencing. Run relevant interaction/display tests if a presentation defect requires a View fix, then report that fix separately from Core completion.
- [ ] Report acceptance results and stop. If existing consumers already satisfy the contract, record verification without manufacturing a View refactor.

These are receiving tasks, not claims of manual play validation. This document does not transfer rule calculation, Buffer writes, lifecycle ownership, or AI behavior to View. If a Core response itself violates the delivered contract, record a reproducible Core request rather than masking it with View filtering.

## 5. Saved configuration and conditional content follow-up

All paths in this section are relative to `Assets/ProjectL/DataBaseSO/InGameGroup/`.

| Unit | Required angle | Current saved mapping / next action |
|---|---:|---|
| ShotGun | 1 | Complete. Existing unit reference uses `Targeting/Item/TargetingMethod_2.asset`, key 2, Fan/Direction/None/0. |
| Gravekeeper | 1 | Complete. Shares method 2; its unit reference and range are unchanged. |
| Ballista | 2 | Complete. `Unit/Item/Unit_Ballista.asset` references new `Targeting/Item/TargetingMethod_BallistaAngle.asset`, key 7, EightDirectionLine/Single/None/0. Registered in `Targeting/TargetingMethodDataTable.asset`. |
| Shielder | 2 | Pending separate character content. No UnitDataSO or EntityKey exists in the inspected checkout. When supplied, its dedicated CardinalLine/Single/None/0 method uses reserved key 6 and receives angle 2. |
| Undead Heavy | 1 | Pending separate character content. No UnitDataSO or EntityKey exists in the inspected checkout. When supplied, use a distinct EightDirectionLine/Single/None/0 variant with reserved key 8; do not share Ballista's angle-2 method. |

Method 2 retains GUID `4b69de16e50a408fa8cca60d6db58d46`; new method 7 uses GUID `e5e42fa120684bc899e2050a79576e8a`. Original method 3, GUID `fed398bfcedb40f6912dd5d8d61ed661`, stays registered and unrestricted. Other existing methods remain angle 0. Saved but unregistered boss method 5 is an existing unrelated issue and was not changed.

The remaining work, once real character content exists, is an **SO setting/reference and table-registration task**, followed by persisted-data tests. Its exact conditional checklist remains in the original plan, P3-B (Git history: `2026-10-15-attack-choice-angle-plan.md#p3-b-connect-shielderundead-heavy-angles-when-their-core-content-exists`); do not create a competing copy of that implementation plan. Recheck actual filenames, keys, consumers, and reservations before editing. Preserve each character's supplied range, traits, balance, and entry route. Coordinate the content owner if prerequisites are absent; do not silently mark those units complete.

## 6. Evidence, verification environment, and locality

Native Unity 6000.0.73f1 verification finished on 2026-10-16 KST:

- Persisted angle tests: **9/9 passed**.
- Core suite: **2702/2702 passed**.
- Focused Phase 3 group: **313/315 passed**; the two DB failures already existed.
- Final Core + Framework suite: **7061 total, 6964 passed, 97 failed**, zero skipped/inconclusive. Failed-case names exactly match the fresh baseline; **zero new failures**. The full suite is not green.
- Independent Phase 3 review: no Critical, Important, or Minor findings. View manual acceptance remains unperformed.

The two focused pre-existing failures concern a terrain coordinate mismatch and a missing LineShader material. Full counts, filters, and evidence locations are in plan section 11. Existing failures are not assigned to this View task merely because the full Framework suite includes them.

For a later data edit, run `TargetingAngleAssetTests`, `TargetingAngleAuthoringTests`, `TargetingMethodDataTests`, `BattleDefinitionAdapterTests`, `DataBaseAssetTests`, and the relevant Core angle regressions according to the original plan. Run native Unity through the approved AppData-access path; a sandboxed `CurlRequestCache.db` failure before project load is an execution-environment failure, not a project test result. No Unity upgrade or cache deletion is required. Check for an existing Editor lock and preserve the user's Editor process.

**Feature locality / SSOT / SOLID / Framework:** Method SOs own authored configuration; Core owns geometry and admission; existing results carry evaluated facts; View owns presentation. A data assignment needs the method-to-unit reference/export contract, not damage, AI, or rendering internals. A presentation adjustment needs the range/preview response contract, not a duplicate sector calculation. Existing table registration is a legitimate composition change. Preserve state ownership, Framework/Buffer/Job contracts, and shared-response regression coverage; add no separate angle cache, runtime service, or consumer-specific rule.
