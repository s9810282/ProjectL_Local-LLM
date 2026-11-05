# Enhancement Shop Code Review

Date: 2026-10-23  
Status: ES01/ES02 implementation delivered through separately authorized Phases A/B/C; full manual acceptance and existing failing gates remain open. ES03/ES04 are unchanged.

Execution plan and current evidence for ES01/ES02: [Enhanced Trait Display Fix](../plans/2026-10-23-enhanced-trait-display-fix-plan.md#53-phase-c-implementation-and-verification--2026-10-23). The review findings below describe the original defects; delivery status is recorded separately under each finding.

## 1. Scope and conclusion

Reviewed the enhancement shop's candidate generation, entry/purchase validation, per-instance enhancement ownership, atomic publication, request/receipt handling, popup lifecycle, and purchased-unit presentation. Combat trait behavior itself is covered by the [movement/attack review backlog](2026-10-23-cell-movement-attack-review-backlog.md).

**No confirmed correctness defect was found in the inspected Core purchase/charge/completion-retry path. Two P2 presentation defects were identified after purchase.** These were initially source/data findings. The subsequent authorized fix has Unity EditMode, actual-asset rendering and a startup Play check; it does not yet have complete purchase-to-next-battle manual acceptance.

The separate [Lobby Main selection design](2026-10-23-lobby-main-selection-run-lock-design.md) does not change enhancement eligibility or pricing. A selected Main's immutable instance identity can remain fixed while its enhancement changes.

## 2. Prioritized findings

| ID | Priority | Finding | Evidence level |
|---|---|---|---|
| ES01 | P2 | Added enhancement traits disappear from unit information when their TraitVisual entry is missing | Fix delivered; operating assets/tests/render checks pass, full manual route pending |
| ES02 | P2 | PoisonThorn keeps the base ThornField description, including damage 5 instead of 7 | Fix delivered for owned/combat consumers; identity/refresh tests pass, full manual route pending |
| ES03 | Deferred | Empty-candidate visit cannot yet display the last successful enhancement purchase in the Run | Explicitly deferred existing requirement; not a new regression |
| ES04 | Policy confirmation | Offers include reserve owned units as well as the selected deck | Current planner behavior confirmed; not classified as a defect |

There is no evidence from this review for a P0/P1 transaction rewrite. ES01 and ES02 can be selected independently, but they share the unit-trait presentation boundary and should use compatible solutions.

### ES01 — Missing TraitVisual entries hide purchased capabilities

**Delivery (2026-10-23).** Registered keys 130/160 with the approved names/descriptions and optional null icons. The actual operating-recipe coverage fixture passes 4/4. Combat consumer tests preserve original Shotgun text alongside each added trait; deck/combat actual-asset render checks pass after local ability-text autosizing. The shared missing-visual policy for unrelated traits remains unchanged. Actual purchase and subsequent battle inspection still require manual acceptance; required DB validation retains 21 exact baseline failures.

**Trigger.** Purchase DoubleShot or DoorBreaching for ShotGun, then inspect that instance in the deck card or combat unit information.

**Cause.** `UnitCardTraitContent.Append` silently returns when no TraitVisual is found. The operating TraitVisual table lacks the DoubleShot (160) and DoorBreaching (130) entries. The shared renderer is used by both `UnitCardUI` and `CombatUnitInfo`. The new trait can be present in Core stats yet absent from the displayed ability list.

**Minimum improvement.** Author/register the View definitions required by operating enhancement recipes, and add an asset-to-recipe coverage check. Preserve the existing card/popup lifecycle. Decide how required-content failures should be surfaced at a configuration boundary; do not rely on silent omission to make missing production data appear complete.

**Change / understanding / validation scope.** Trait presentation data and its existing shared converter are the relevant scope; purchase rules, currency and Core damage do not need to change. Test real registered data plus a purchased instance's deck/combat display. A unit test with a synthetic complete TraitVisual table would not detect the operating-table omission.

Sources: [UnitCardTraitContent.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Global/UnitCardTraitContent.cs) lines 43-45; [TraitVisualDataTable.asset](../../../Assets/ProjectL/DataBaseSO/ViewGroup/TraitVisual/TraitVisualDataTable.asset); [CombatUnitInfo.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/Hud/CombatUnitInfo.cs) lines 191-192.

### ES02 — Same-key enhancement parameters do not reach the description

**Delivery (2026-10-23).** Phase B publishes immutable per-instance `ThornFieldDescription`; Phase C passes it from owned `UnitCardUI` and preserves the existing full combat snapshot route. Both use one View formatter for published damage/lifetime/reduction. Added tests cover base versus enhanced same-key identities, non-production 17/9/40 values, pinned revision refresh, reopen, selection change and new-Run reset. Phase C's 19 new cases pass; its selected run is 101/123 with 22 exact baseline failures. Render checks cover base5 and enhanced7/30 in both operating layouts. Complete manual purchase-to-next-combat acceptance remains open.

**Trigger.** Purchase PoisonThorn and inspect the enhanced Cactus Cannon's ability description.

**Cause.** `UnitCardTraitContent.Create(ReadOnlySpan<TraitStatusSnapshot>)` reduces every snapshot to its `TraitKey`. It then prints the static description attached to that key. PoisonThorn retains ThornField key 40 but changes the effective payload to damage 7 and defense reduction 30%. The base TraitVisual text still describes damage 5 and has no corresponding enhanced effect description.

**Planning follow-up.** Combat already supplies this typed payload, but the out-of-combat owned card receives only `CharacterBaselineStatsSnapshot.TraitKeys`. A complete fix therefore also needs the narrow owned-read projection described in Plan Phase B; changing the View converter alone would fix only the combat route.

**Minimum improvement.** Preserve the individual unit's published effective trait parameters when formatting its description. View may format Core-provided numbers with View-owned text/templates; it must not reread InGame enhancement recipes or calculate the rule itself. Alternatively, if a dedicated published enhancement presentation fact is chosen, make its identity/lifetime explicit and keep the effective numeric facts authoritative.

Changing the global base ThornField text to 7 would make unenhanced instances wrong. Separate units with the same key must be able to display different effective descriptions at the same time.

**Change / understanding / validation scope.** The shared card/combat trait-content conversion is the narrow change boundary. Test enhanced and unenhanced instances together, hover/pinned refresh, next-battle display, and new-Run reset. Keep trait data publication and presentation distinct; no shop transaction rewrite is required.

Sources: [UnitCardTraitContent.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Global/UnitCardTraitContent.cs) lines 19-21, 53; [TraitVisual_ThornField.asset](../../../Assets/ProjectL/DataBaseSO/ViewGroup/TraitVisual/Item/TraitVisual_ThornField.asset); [Enhancement_PoisonThorn.asset](../../../Assets/ProjectL/DataBaseSO/InGameGroup/Enhancement/Item/Enhancement_PoisonThorn.asset).

### ES03 — Last successful enhancement history remains explicitly deferred

The existing enhancement View design requires a disabled history card when a new enhancement visit has zero actual offers and the current Run has a previous successful enhancement purchase. Current owned records do not record purchase order, and the latest command receipt is not a durable last-success history source.

This requirement is already marked as waiting for a Core read contract. Do not infer purchase order from owned-list order, treat zero offers as proof of no history, create a View purchase ledger, or inject a fake offer into the current visit.

If selected later, define a Run-scoped authoritative last-success fact that updates only on committed enhancement purchase and resets only with successful Run replacement. Failure, skip, normal shop purchase and rejected new-Run preparation must not corrupt it. An earlier Core handoff already exists; update that contract rather than creating a competing source.

Source: [Enhancement View design](2026-10-19-enhancement-view-integration-design-Hojin.md), section 4.4; its Core dependency is explicitly deferred.

### ES04 — Current offers use the full owned roster

`EnhancementOfferPlanner` combines all unenhanced owned instances with matching recipes. It does not restrict candidates to the committed Run deck or exclude Main-role records. Therefore selected Main, reserve Main and reserve Sub instances can all be eligible if a corresponding recipe exists.

No requirement supplied in this task changes that rule. Preserve it in the Main-lock work. If selected-deck-only or selected-Main-only enhancement becomes a requirement, treat it as a separate policy change with eligibility, preview and stale-context tests.

## 3. Confirmed strengths of the current path

| Boundary | Checked behavior |
|---|---|
| Offer identity | Candidates are `(CharacterInstanceId, EnhancementKey)` pairs; up to three distinct pairs. Alternative branches for the same instance are possible. |
| Empty candidates | No random draw is consumed for an empty pool. |
| Entry coherence | RunId, roster revision, owned facts and committed deck are captured and revalidated around definition reads. |
| Frozen offer | Target identity, enhancement identity/level, price and displayed metadata belong to the visit offer. Purchase validates the current definition against that snapshot. |
| Purchase rules | Requires a live captured visit Main, unused purchase, owned exact target, no existing enhancement, supported level 1 and sufficient funds. Growth/role/identity are preserved. |
| Atomic install | Currency, roster enhancement and purchased-state proposals are prepared before the first write. The checked install tail has no external callbacks between these writes. |
| Completion retry | The purchase receipt is published before completion. Retrying completion does not purchase or charge again. |
| Per-instance composition | `OwnedUnitSpecResolver` builds a new effective definition without mutating the shared catalog or same-type siblings. |
| View command identity | ShopUI submits the displayed OfferId and ShopVisitContext. The controller matches request number, context, kind and offer, rather than deriving a target from character type or slot position. |
| Popup and Framework | ShopUI uses UIBase, UISystem registration and PopupViewScope. Inspected consumers use the existing Global UI service path; no new root re-export is required. |

Relevant Core sources: [EnhancementOfferPlanner.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Shop/EnhancementOfferPlanner.cs), [ShopService.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/ShopSystem/ShopService.cs), [EnhancementPurchaseRules.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Shop/EnhancementPurchaseRules.cs), [ShopTransactions.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Core/ShopTransactions.cs), [ShopCommandExecutor.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/ShopSystem/ShopCommandExecutor.cs), [OwnedUnitSpecResolver.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Enhancements/OwnedUnitSpecResolver.cs).

## 4. Verification and remaining checks

Initial review evidence, before the separately authorized A/B/C implementation, used a pure C#/.NET 10 harness:

- `EnhancementShopRuleTests`
- `OwnedEnhancementStateTests`
- `OwnedEnhancementResolutionTests`
- `RunDeckRuleTests`
- `RunDeckContractTests`

Combined result: **48 passed, 0 failed, 0 skipped**. These are five focused fixture groups, not a repository-wide green result. The local runner/XML are under ignored `.utmp/main-shop-review/`.

At that review point, Framework/Unity tests were inspected but not executed. Subsequent fix evidence is recorded in plan sections 5.1-5.3: Phase A 4 new cases, Phase B 6, and Phase C 19 all pass. Phase C's graphics-enabled selected run is 101/123, with exact failure comparison, and eight actual-asset render scenarios fit. Play reached Boot -> Lobby -> StageSelect -> Combat with the local startup override disabled; the full enhancement purchase/display round-trip remains unverified manually. The earlier 48-case result is not evidence for the UI fix.

Useful next checks for a selected implementation:

1. Real registered TraitVisual coverage for each operating recipe's added traits.
2. Correct enhanced and unenhanced descriptions for identical CharacterKey/TraitKey values.
3. Selected Main enhancement purchase preserves MainId; reserve eligibility follows the explicit current policy.
4. Same-type instances: only the offered OwnedId changes and is displayed as changed.
5. Stale/reentrant roster, deck, price or gold changes reject without partial publication.
6. Purchase succeeds but completion fails: retry completion without a second debit.
7. Hide/reopen and old visit receipts cannot act on a new visit or Run.
8. Failed new-Run preparation preserves the old enhancement; successful new Run resets owned enhancement state.

Relevant maintained suites include `EnhancementShopServiceTests`, `EnhancementPurchaseTransactionTests`, `ShopCommandExecutorTests`, `ShopVisitIdentityTests`, `SelectedMainInteractionTests`, `EnhancementStageRegistrationTests`, and `EnhancementEndToEndTests`. Run the affected subset and required project checks when changing product code.

## 5. Refactoring boundary

ES01/ES02 concern the boundary between effective Core facts and unit presentation. Retain transaction ownership and the current one-purchase/receipt protocol. Keep text/icon composition in View, effective gameplay values in Core, and installed identity/state in Buffer. The smallest useful improvement makes purchased abilities visible and numerically correct without requiring future UI changes to understand shop transaction internals.

No new acquisition policy, Main candidate, enhancement level, price, save system, or broad Framework refactor is authorized by this review.
