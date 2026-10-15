# Shop Stage Core Connection Design

Date: 2026-10-03  
Status: P1–P3 implemented, verified and independently reviewed. See the [consumer handoff](../handoffs/2026-10-03-shop-stage-core-connection-handoff.md). See the [P1 verification report](../handoffs/2026-10-03-shop-stage-core-connection-p1-verification.md) and [P2 verification report](../handoffs/2026-10-03-shop-stage-core-connection-p2-verification.md).  
Source baseline: `e8977adc` plus the existing, uncommitted temporary Core runtime package.

## 1. Goal and scope

Complete the Core service path from an explicit shop-node request through optional character purchases and node completion to the next battle. This is a medium-sized integration package across existing services and Buffers, not a replacement shop or deck system.

Reuse the existing offer rules, character acquisition, currency publication and Framework composition. The consumer may complete a shop after zero, one, two or three purchases, including when no remaining offer is affordable. Completion stays an explicit call; buying the third offer does not automatically advance progress.

The scope includes safe entry, an identity-aware purchase API, outcome-free completion, failure/retry contracts and a temporary-data integration scenario. It excludes deck synergy, new character growth rules, consumables, rerolls, selling, refunds, reward aggregation, next-chapter/final-Run policy, Excel importer work, production assets and Job/Flow/scene changes.

## 2. References and present evidence

Apply these Framework references:

- [Framework Development Guidelines](../framework/Framework%20Development%20Guidelines.md)
- [Framework Overview](../framework/01_Framework_Overview.md)
- [Framework Services](../framework/02_Framework_Services.md)
- [Creation, Registration and Query](../framework/03_System_Creation_Registration_Query.md)
- [Programmer guide](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md)
- [Architecture](../../../Assets/ProjectL/Script/Systems/Documentation/framework-architecture.md)

Use the [recovery design](2026-10-03-recovery-stage-core-connection-design.md) as the entry/publication and completion precedent, and the [temporary runtime handoff](../handoffs/2026-10-03-temporary-core-runtime.md) for existing development composition. The architecture pipeline document is not a prerequisite.

Current source already provides `ICharacterShopService.PrepareShop(int seed)` and `Purchase(ShopOfferId)`. `CharacterShopRules` creates exactly three offers using a local seeded `Random`, permits duplicate character kinds, validates all candidates and plans acquisition/payment. `BufferSystemGroup.PublishPurchase` prepares the character, currency and purchased-slot changes before installing them. Runtime ownership remains in existing Buffer stores.

`CharacterRosterRuleTests`, `CharacterRosterServiceTests`, `CharacterRosterBattleCycleTests` and draw-command tests already cover offer determinism, invalid candidates, individual slot consumption, insufficient funds, distinct owned identities, publication failures and persistent roster/combat behavior. Reuse these tests rather than restating all their assertions in new fixtures. The temporary-runtime handoff records earlier passing focused verification and five unchanged catalog/Economy failures; no tests were executed for this documentation change.

What is absent is explicit-target shop entry and shop-node completion. Existing `PrepareShop` requires an already selected stage. The new path must prepare selection and offers together. The existing temporary scenario contains recovery but no shop.

The earlier shop design (Git 이력: `2026-09-19-sub-character-shop-stage-design.md`) records zero-purchase completion policy. Its former session/revision architecture and duplicate-purchase success proposal are not the current contracts: retain Buffer ownership and `SoldOut` for a consumed slot.

## 3. Chosen structure

Extend the existing `ICharacterShopService` and its implementation. Keep the current `CoreGroup.Shop` property and service key. Inject the already constructed `ChapterProgressionCoordinator` into `CharacterShopService` from `InGameCoreSystemFactory`; do not construct another coordinator or introduce a second shop service.

Add these public operations alongside the existing methods:

```csharp
ChapterProgressionResult EnterShop(int chapterKey, StageNodeId targetNodeId, int seed);
CharacterPurchaseResult Purchase(int chapterKey, StageNodeId nodeId, ShopOfferId offerId);
ChapterProgressionResult CompleteShop(int chapterKey, StageNodeId nodeId);
```

New consumers must use `EnterShop` → `Purchase(chapterKey, nodeId, offerId)` → `CompleteShop`; no-purchase visits omit the purchase call. Keep `PrepareShop(int seed)` and `Purchase(ShopOfferId offerId)` compatible for existing callers/tests. Share definition conversion and purchase rules where useful, but do not impose new graph prerequisites on those legacy methods. New stage consumers use the identity-aware sequence above. Legacy methods do not promise its stale-node protections.

This avoids a duplicate stage service and avoids a generic noncombat framework. The service owns dependencies and orchestration only. No new Buffer store, session, revision counter, generic transaction or runtime registration is introduced.

## 4. Entry contract

`EnterShop` follows the existing progression entry preflight without selecting the target:

1. Validate the requested chapter and reachable, uncleared target using the existing coordinator. Capture the expected current chapter/selection and resolved target node/key.
2. Reject a retained battle, including a terminal battle whose settlement has finished. The caller owns legitimate `Combat.EndBattle()` cleanup.
3. Read the target root from `IStageTableQuery`. Require a matching key/map, regular nonboss shop root and matching live graph metadata. Convert every candidate through `CharacterDefinitionAdapter` and use `CharacterShopRules.TryPlanOffers` with the requested seed. Preserve the existing three-slot algorithm and its random-call order.
4. After all DB queries, repeat progression preflight and compare the captured origin and target. Because `PrepareStageEntry` itself queries the DB, finish with the internal query-free `ChapterProgressionCoordinator.RevalidateStageEntry` against live Buffers. A changed origin, cleared node/map, changed key/type/role or newly installed battle rejects entry before publication.
5. Publish through a narrow `IShopStageBufferWriteSink.PrepareShop(ShopEntryPreparation preparation)` on the existing Buffer group. It performs live storage checks, prepares a new `CurrentShopData` and installs selection plus offers once using `PreparedCurrentSelection`/`InstallSelection`.

`ShopEntryPreparation` mirrors the recovery preparation shape: expected chapter, expected selection presence/value, target node/key/name, and a defensive copy of exactly three `ShopPurchaseSnapshot` offers. `CurrentProgressBuffer.PrepareShopSelection` prepares the shop and clears the previous recovery selection only as part of successful installation.

The Buffer boundary checks expected origin, target binding, regular shop graph metadata, uncleared map/node and absence of battle. It must not trust DB validation alone. Re-entering the same selected shop is rejected; entry cannot refresh consumed slots. A different node with the same StageKey receives its own newly prepared offers.

Rejected/failed entry makes no writes of its own: selection, prior shop/recovery receipts, roster, wallet, progress and combat data remain as they were. Bad/missing definitions fail explicitly before publication; no fallback offers are installed. Progression rejections retain existing `ChapterProgressionFailureReason` values. Invalid arguments/definitions and incompatible battle/type state use the established recovery-style exception contract.

## 5. Identity-aware purchase

The new purchase overload requires the requested chapter/node to match a prepared, uncleared current shop with no retained battle. Check both the graph and DB root as for completion. Offers and prices come from the installed `CurrentShopData`; do not reload the candidate pool or reroll offers during purchase.

Capture the node/key and shop snapshot before external queries. After the final unit-definition lookup, revalidate identity, the captured shop reference, progress, stage metadata and battle absence. This guard must follow the unit lookup, not merely the stage-root lookup: the existing `CurrentProgressReadSink` is live, and test queries can change it. Then read current main-character/currency facts, call the existing `CharacterShopRules.PlanPurchase`, and publish once through the existing `ICharacterPurchaseBufferWriteSink.PublishPurchase`, using the captured node/key. Do not add a second acquisition/payment implementation.

For the new overload, use existing failure values:

| Condition | Result |
| --- | --- |
| Requested chapter/node or captured node/key no longer matches | `EntryMismatch` |
| Matching node/map already cleared, or captured shop snapshot replaced | `StaleEntry` |
| No selection/prepared shop, retained battle, or nonshop/boss metadata | `WrongPhase` |
| Unknown slot, consumed slot, insufficient gold, invalid character, identity overflow | Existing `UnknownOffer`, `SoldOut`, `InsufficientFunds`, `InvalidCharacter`, `IdentityOverflow` behavior |

Evaluate phase/identity/progress guards before the normal offer rules. Preserve existing invalid-argument and missing/invalid DB-definition exceptions. These are synchronous operations; no concurrency or Run-reset identity-reuse guarantee is added. A query's own injected mutation is not rolled back, but the rejected operation must not add a payment, acquisition or slot write.

## 6. Completion and retry

`CompleteShop` checks the explicit identity, prepared shop, graph/root type and role, node/map progress and absence of battle. Recheck after the stage-root query before forwarding to `ChapterProgressionCoordinator.CompleteShopStage(chapterKey, nodeId, stageKey)`.

Add the pure `ChapterProgressionCommands.CompleteShopStage`/`ChapterProgressionRules.EvaluateShopCompletion` path, matching the recovery completion inputs and identity/progress validation. A private shared completion helper is acceptable where behavior is identical; existing public recovery semantics remain unchanged. The coordinator performs one `SetClear` only for an accepted result.

Completion does not synthesize `StageOutcome.Victory`, settle a battle, clear a chapter, increment map-end attempts, reset the run or erase shop receipts. The current selection and purchased mask remain readable until a later legitimate selection. Duplicate completion returns `StageAlreadyCleared`; a guarded purchase after completion returns `StaleEntry`. Chapter/node mismatch uses existing progression rejection reasons. Invalid definitions or incompatible phase/type fail without a stage-clear write.

Purchase and completion are separate committed operations. If a purchase succeeds but completion fails before its stage-clear write, retain the acquired character/identity, paid currency and purchased mask, leaving the node uncleared. Correct the failure and retry **only `CompleteShop`**. The retry makes exactly one clear transition and no further purchase, payment or healing. Do not restart entry, reset offers or introduce rollback/refunds. A consumed offer remains `SoldOut`, not an idempotent success.

## 7. Temporary execution and acceptance

Keep `TemporaryCoreRuntimeFactory.Create()` and its recovery scenario unchanged. Add the explicitly named development-only `CreateWithShop()` returning a separately initialized `FrameworkRuntime`, using the same real composition and existing temporary battle source. Preserve the normal production bootstrap and the unique `Create` name used by existing reflective callers.

The additional scenario is chapter 1: start node 1 → combat node 2 (stage 101) → shop node 5 (stage 30) → boss node 4 (stage 102). Its depth values are 0–3; node IDs need not be ordered. Supply a regular shop root with a single Akimbo candidate costing 5. Existing selection rules produce three independent slots of the same character kind. These are disposable execution values, not balance approval or a new DB schema. The original recovery scenario remains a separate regression path.

Use one runtime, its registered services and real Buffers throughout:

`battle settlement → retained terminal battle rejects EnterShop → caller EndBattle → EnterShop → zero/one/two/three purchases → CompleteShop → next battle`.

Drive victories through existing combat commands; do not replace settlement with direct stage-clear writes. Assert shop entry/purchase/completion preserve persistent HP and existing souls, and preserve combat results/counters according to their established lifecycle contracts. No shop operation calls `EndBattle` or reset APIs, or replays settlement. Each purchased character has its own identity and zero initial souls. At next battle preparation, acquired identities appear exactly once in the full draw order; do not require every acquired character to fit in the initial hand.

With initial gold 50, expected gold after buying `n` slots is `50 - 5*n`, owned count is `4 + n`, and the current node clears only after explicit completion. Include a failed unaffordable purchase followed by zero-purchase completion in the service tests. Verify no-purchase completion and purchase-success/completion-failure using existing query fixtures; no production fault hooks.

Core completion requires focused tests passing and a broad baseline comparison showing no new regressions. Authored production data, real Job/Flow calls, button/result display and scene transitions require separate acceptance; a passing Core scenario does not claim those are connected.

## 8. Delivery boundaries

P1 delivers prepared entry and real Buffer publication. P2 delivers guarded purchasing, node completion and retry behavior. P3 delivers temporary shop-to-battle evidence and the consumer handoff.

Follow interface-only service exports, Factory build-once composition, narrow group lookup, supported InitData/PostBind ordering and Buffer-owned facts. Keep Framework/Core assembly dependency boundaries unchanged. No new service key or Core Context property is needed.

Implementation documentation is English; new code comments are Korean. Raw test XML, logs and launcher files belong outside the repository. Existing uncommitted temporary-runtime work is an explicit prerequisite to preserve, not cleanup material. The user authorized P1, P2 and P3 implementation on 2026-10-03 and confirmed temporary data will be removed when the real DB replaces it. Committing/pushing requires separate user instruction.
