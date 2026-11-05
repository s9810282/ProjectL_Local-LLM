# Combat Cost Retirement — R4 Consumer Cutover

Updated: 2026-10-29. **Core/Buffer/DB and authorized View migration are implemented. Actual-project automated gates: Core4039/4039, CoreIntegration350/351 (unchanged StageKey2101 baseline failure), Framework/View15/15, zero skips. Manual/PlayMode acceptance remains pending. No commit/push or external message.**

Owner boundary: [retirement plan R4.2](../plans/2026-10-29-legacy-enhancement-retirement-plan.md#r42-consumer-integration-and-review-checkpoint) states: "Coordinate these changes with the View owner; Core authorization does not transfer View/Flow ownership." The user initially chose separate View-owner handoff, then explicitly authorized this chat to perform the R4 View migration if judged safe. That authorization was accepted after the app inventory showed no other active same-project writer. Scope remains the existing cost consumers below; unrelated UI/Flow/scene, FieldCamera and Architecture work is excluded.

## Removed and replacement public contracts

| Removed | Replacement |
|---|---|
| PlayerActionBudget, EnemyActionBudget, ActionFundingKind, shared refill/spend/affordability | No replacement resource. CharacterCombatFacts.ActionUse remains authoritative |
| ActionCostSettlementSnapshot(actor,funding,spent,before,final,beforeActionUse,finalActionUse) | UnitActionUseSettlementSnapshot(actorId,beforeActionUse,finalActionUse); existing meta GUID retained |
| Transition/CombatResult.ActionCost, AttackResult/AttackPreview.ActionCostSettlement | ActionUseSettlement; actor and before/final action evidence remain |
| Stats/definition MoveCost/AttackCost, availability budget/prices, AttackRangeResult.CanAffordAttack | Existing movement range/allowance, ActionUse, CanMove/CanAttack and non-cost failure facts |
| Movement cost/quote/delta and attack cost/spent/final-balance scalar fields | Existing paths, traversed distance, stop/valid-step, aim/contacts/hits and action-use evidence |

Serialized old failure/activation numbers remain reserved. They have no live affordability/refund producer. No zero-cost compatibility API or unlimited budget is supplied. Shop/recruit/item prices, draw/deployment gold, attack range and maximum category movement are unchanged. Main3/Rider4/Heavy3/Cannon2/Boss0 remain authored;0 disables voluntary movement.

## Implemented consumer migration

| Owner / source | Delivered change |
|---|---|
| CombatSceneDisplay, removed CombatTurnCostPresenter/meta, CombatSceneDisplayFactory and CombatSceneRoot | Removed shared AP rendering/preview calls. Optional legacy TurnCost reference is excluded from readiness checks; when present the factory closes it through existing UISystem. Existing teardown/finalization and turn display remain |
| ICombatInteractionFeedback and AttackTargetingInteraction | Removed cost feedback and CanAffordAttack cache/branches. Preserved actual CanAttack, Core range/aim response, slot ownership, generation, ordinary failures and input/playback gates |
| CombatActionNotice and CombatActionStateText | Removed AP-insufficient messages; retained non-cost rejection/action-use diagnostics |
| CombatUnitInfo | Removed price rendering and cost-only availability cache. Optional serialized legacy cost roots/values are hidden; independent unit action HUD and category/stat presentation remain |
| UnitCardUI, UnitCardView and DeckEditUI | Removed AP arguments and text; retained HP/ATK/DEF/level/category and existing detail/health lifetime |
| ICombatBattleDisplay, CombatSceneInput, both View factories and CombatViewSystem | Removed only the exclusively cost-display callback/cache path. Actual action-availability tracker Sync/Read/Stop and submission gates remain |
| CombatLayoutWindow.Panels and DevBattleInitializationSource | Removed obsolete price rows/constructor arguments; retained category rules and configured movement distances |
| End-turn policy/gate | Existing policy already allows unused actions/no selection. Preserved pending, unhandled, playback, finalizing and terminal restrictions; covered by actual consumer tests |

These edits are the R4 contract migration. New screens, Main specialization, undo, new traits, FieldCamera/Architecture, new Heavy SO/pools and R3's TraitVisual asset deletions remain outside it. Existing registered TurnCost screen/prefab remains dormant; no production call opens it. Scene/prefab bytes are unchanged. Three owner documents mark their former price contracts as historical.

## Verification and remaining manual acceptance

Actual original-project Unity6000.0.73f1 gates ran sequentially through the AppData-enabled runner. `cost-r4-core-project3.xml` is4039/4039; `cost-r4-integration-project.xml` is350/351; `cost-r4-view-project.xml` is15/15. All have zero skips. The integration failure's fullname/message exactly match accepted R3: BossStagePreparation missing StageKey2101. All seven new action-evidence/actual-category-SO tests and four existing Sacrifice publication cases pass. This branch has no prior Framework/View fixtures or DataBaseAssetTests source; new focused consumers exercise real View buffers/gates, and the production category SO test checks all5 distances and removed serialized prices.

Independent Core and View/source-fix reviews are recorded in [plan §7.5](../plans/2026-10-29-legacy-enhancement-retirement-plan.md#75-r4--combat-cost-removal-and-consumer-cutover-2026-10-29), together with the exact source manifest and obligation ledger. The original rejected-transition test exception boundary was corrected and then executed successfully. No source changed after the gates.

Remaining manual/PlayMode checks:

1. Enter Combat with existing scene configuration; AP bar and move/attack cost icons/backgrounds/tooltips stay absent.
2. Confirm movement range/action-used indicators, short move consumption, move-then-attack and attack-first lockout.
3. End the turn with unused actions; pending commands/playback complete first, the next side progresses and the next own turn resets actions.
4. Confirm HP/stat/category/card/deck information, normal battle exit and teardown. Automated results do not establish visual/manual acceptance.

The earlier separate-consumer stop was superseded by explicit user authorization. Code and automated consumer delivery are complete; manual acceptance remains open. Final independent cross-boundary review has no remaining actionable findings. The one reused isolated Core project and regenerable helper-build caches were removed after review, freeing103.05MiB; source checkpoints, manifests, XML and review records remain. Main Unity Library/AppData were untouched. No commit/push.
