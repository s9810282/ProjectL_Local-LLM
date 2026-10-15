# Shop Stage Core Connection — P1 Verification

Date: 2026-10-03  
Base: `e8977adc` plus the existing uncommitted temporary Core runtime package.  
Status: Historical P1 checkpoint, implemented, verified and independently reviewed; uncommitted. P2 is now complete; see the [P2 verification report](2026-10-03-shop-stage-core-connection-p2-verification.md). P3 is now complete; see the [consumer handoff](2026-10-03-shop-stage-core-connection-handoff.md). Statements below describe the original P1 delivery boundary.

## Delivered contract

`ICharacterShopService.EnterShop(int chapterKey, StageNodeId targetNodeId, int seed)` prepares a legal node selection and exactly three frozen DB-derived offers together. The existing Shop service receives the existing progression coordinator through its Factory. The Core Context and service keys are unchanged.

Entry validates navigation, root identity/map/type/role, all candidate character definitions and live graph metadata. After the repeated progression preflight completes its own DB query, internal `ChapterProgressionCoordinator.RevalidateStageEntry` checks current Buffer navigation without another external lookup. Captured origin/target comparison and the new narrow `IShopStageBufferWriteSink` guard precede one Current selection installation.

Failures do not publish a partial selection/menu or change currency, owned characters, persistent HP/souls, progress or combat results/counters. Query-fixture mutations themselves are retained. Active/terminal retained battles block entry without automatic cleanup. Same-node re-entry cannot refresh purchased slots; another node using the same StageKey receives independent offers. A successful new shop entry replaces the previous recovery selection; a failed entry preserves its applied receipt.

The existing `PrepareShop(int)` and `Purchase(ShopOfferId)` retain their rules and compatibility behavior. A shared offer-conversion helper preserves the existing seeded algorithm. P1 tests use legacy purchases only to seed previous consumed receipts.

## Consumer adoption condition

New consumers must use `EnterShop` → `Purchase(chapterKey, nodeId, offerId)` → `CompleteShop`. Zero-purchase visits omit the purchase call. The legacy methods do not provide the new path's stale-node request guarantees; retaining them is intentional compatibility, not a missing protection.

Only `EnterShop` is delivered in P1. The identity-aware purchase overload and `CompleteShop` are P2 work, so P1 alone is not the complete production consumer sequence. The temporary shop-to-next-battle scenario is P3 work. Caller-owned battle settlement/`EndBattle`, UI/Job/Flow adoption and scene transitions remain separate acceptance.

## Verification

Unity 6000.0.73f1 ran through the approved AppData-capable path. Test evidence and the execution ledger are outside the repository at `C:/Temp/ProjectL-shop-core-p1-20261003`.

| Run | Result | Meaning |
| --- | --- | --- |
| `baseline.xml` | 1532/1537, exit 2 | Fresh pre-change baseline; five existing catalog/Economy failures. |
| `red.xml` | 0/57, exit 2 | Compiled fixtures observe absent entry/preparation/publication contracts through assertions. |
| `late-query-red.xml` | 57/61, exit 2 | Four added cases expose navigation/key/clear changes during the final preflight root query. |
| `graph-role-red.xml` | 11/12, exit 2 | Undefined graph role was accepted; shop publication now requires `Regular`. |
| `focused-final-3.xml` | 265/265, exit 0 | All final P1 cases plus existing shop/roster, recovery, composition, graph, artifact-edit and temporary-runtime regressions pass. |
| `regression-final.xml` | 1594/1599, exit 2 | 62 cases added; no missing tests or changed existing outcomes. |

Final tests use typed APIs. New coverage exercises selection/offer publication, detached collections, invalid arguments/root/candidates, stale storage, query failures and mutations, prior purchased/applied receipts, retained battles, independent visits and both supported InitData/PostBind orders through real Factory/group composition. Persistent state snapshots reuse the existing recovery test utility. Existing offer RNG, purchase/payment/identity and roster rules remain covered by their original fixtures.

`comparison.json` confirms that all five baseline failures keep identical names, messages and stacks:

- `ArtifactDataTableTests.Catalog_RegistersOnlyMigratedRulesAndExplicitlyPendingViews`
- `DataBaseAssetTests.Catalog_CreatesAllDataBaseGroups`
- `DataBaseAssetTests.CombatStages_ReferenceExistingInGameData`
- `DataBaseAssetTests.EveryStage_HasNodeVisual`
- `DataBaseAssetTests.EveryUnitAndStructure_HasUnitVisual`

All report `Combat stage requires Economy`. There are no skipped cases in baseline/final runs. The whole project suite remains failing on this pre-existing authored-data issue.

A fresh read-only reviewer found no Critical, Important or Minor findings and inspected the saved focused/broad results; the reviewer did not execute Unity. New metadata GUIDs are unique. Whitespace verification passed. The Unity-generated FMOD catalog timestamp was restored, and no XML/log/launcher scratch files were added to the worktree.

## Implementation ruling and remaining scope

The plan originally placed coordinator changes in P2. Source inspection showed that `PrepareStageEntry` performs an external DB query after its navigation decision. A focused RED case reproduced an incorrectly accepted disconnected target. P1 therefore adds the small internal query-free revalidation method; it preserves the public progression interface and existing callers. The design and file map now reflect this additional P1 file.

Work continued in the existing feature checkout to preserve its dependent uncommitted temporary runtime package. Raw evidence stayed outside the repo as requested. No commit or push was performed.

Next deliverable: P2 guarded purchase, explicit zero-through-three-purchase completion and completion-only retry after a committed purchase. P3 then proves the full shop/battle cycle and documents caller adoption. Production DB readiness, scene wiring, rewards and final Run policy are not completed by this P1 result.

References: [design](../specs/2026-10-03-shop-stage-core-connection-design.md), [implementation plan](../plans/2026-10-03-shop-stage-core-connection-implementation-plan.md).
