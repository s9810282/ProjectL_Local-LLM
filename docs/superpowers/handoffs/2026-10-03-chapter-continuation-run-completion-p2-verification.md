# Chapter Continuation and Run Completion — P2 Verification

Date: 2026-10-03. P2 implementation, verification and independent review are complete. P3 remains pending. No commit or push.

References: [implementation plan](../plans/2026-10-03-chapter-continuation-run-completion-implementation-plan.md), [selected design](../specs/2026-10-03-chapter-continuation-run-completion-design.md), [P1 verification](2026-10-03-chapter-continuation-run-completion-p1-verification.md).

## Implemented behavior

`IChapterProgressionCoordinator.CompleteChapter(int chapterKey, StageNodeId bossNodeId)` now returns a typed ChapterContinuationResult. Pure commands decide between an explicitly configured successor and an explicitly configured final chapter. Missing/null/default unavailable data returns DefinitionUnavailable; it never implies Victory or a numeric successor.

Admission requires InProgress, no retained battle, current chapter/selected boss identity, matching clear facts and Combat/Boss graph/root metadata, and the retained real Victory completion for that chapter/node/key. Completion/selection/source and target graph values are captured before external lookups, then checked again before publication. The group also verifies those facts before installing prepared values. No external query follows publication admission.

NextChapterSelected publishes the target graph, if absent, and Current map selection together. A valid registered target is reused without rerolling; its map/root metadata and edges/depths are still validated. Changing maps clears selected node/shop/recovery data through the existing Current ownership contract. It does not enter the first battle. RunCompleted installs one Victory receipt, preserving the final selected boss and all final facts. Duplicate final completion returns RunEnded; a stale source request after next-chapter selection returns ChapterMismatch.

StageGeneratorSystem.PrepareGraph returns detached nodes using the same generation algorithm and random-call order. Generate remains a prepare-and-publish wrapper. Stage/Current prepare/install helpers stay beside their existing owners. No new Buffer, session, revision, generic transaction, rollback, independent outcome setter or production Job/Flow change was added.

Cross-chapter EnterChapter uses the same continuation boundary with an expected target. A different successor maps to ChapterMismatch; Final cannot be converted into a cross-chapter selection. Other continuation failures map to appended ContinuationRejected, except RunEnded. Initial/no-map and same-chapter behavior remain unchanged.

## Consumer handoff

1. Resolve genuine boss victory through the existing combat settlement API.
2. Present/acknowledge results and call Combat.EndBattle according to the caller's lifecycle. Core does not force cleanup or clear pending View inputs.
3. Call CompleteChapter with the source chapter key and selected boss node ID.
4. On NextChapterSelected, read NextChapterKey and show its prepared map. Request a normal stage entry later.
5. On RunCompleted, read coordinator Outcome/Completion for the committed final Victory and source identity. Core does not switch scenes or reset facts.

For a route/query/preparation failure, retain the committed settlement and correct the dependency, then retry only CompleteChapter. Do not replay victory, re-enter the boss, reset the Run or reroll an installed graph. Rejected operations perform no own writes; deliberate query-fixture mutations are not rolled back. The existing serialized execution and cross-Run identity limit still apply.

The P1 parameterless preparation guard remains: after boss settlement/EndBattle, InProgress does not allow preparing the cleared selected boss again. Both Next and Final tests preserve completion, selection, persistent facts, results/counters and the null definition cache across this rejection, then successfully call CompleteChapter. Supported defeat still uses full battle settlement; PlanStageResolution remains read-only and direct ResolveStage(Defeat) remains unsupported. EndBattle, reporting, input cleanup and explicit reset retain their existing ownership.

## Composition and production replacement

`InGameSystemFactory.CreateWithSources(battleSource, continuationSource)` explicitly injects both dependencies into one registered runtime. Existing Create/CreateWithBattleSource delegate to the same composition with the unavailable default continuation source. Create names remain unique. InitData/PostBindService work in either supported order. Normal root access is `SystemGroupsService.InGame.CoreGroup`; internal Core and owner keys are not newly exported at root.

The permanent changes are pure contracts/rules/commands, CompleteChapter/legacy admission, detached generation and prepared Buffer publication. P2 test-only ChapterContinuationTestDatabase and ChapterContinuationTestSource reuse the supported Cycle combat fixture and controlled query callbacks; they do not add a production fault hook or temporary fallback.

P3 must add the separately named disposable two-chapter runtime/scenario and prove the full battle/shop/chapter1 → chapter2 → final Run sequence. Existing temporary Create/CreateWithShop paths are preserved; they do not acquire an implicit continuation source in P2. Production still requires the authored Chapter_NextChapter/final-definition representation and adapter, Method_Key targeting definitions/query connection and valid Economy assets. The architecture pipeline document is not a prerequisite. Real adapters and equivalent authored-data acceptance precede removal of disposable definitions/scenario paths; permanent Core APIs and service/rule tests remain.

## Executed evidence

Unity 6000.0.73f1 EditMode ran with approved AppData access. Raw XML, logs, arguments, launcher, incoming baseline patch/status, review package and ledger are external in `C:/Temp/ProjectL-chapter-run-p2-20261003`. No cache deletion or Unity upgrade was needed.

The launcher invokes `-batchmode -nographics -projectPath D:/My_ProjectWork/Github_desktop/Project_L -runTests -testPlatform EditMode -assemblyNames "ProjectL.Tests.Core;ProjectL.Tests.Framework;ProjectL.Tests.CombatViewBoundary"`, with external testResults/logFile paths. Focused runs select the P2 commands/publication/service fixtures, ChapterGraphPreparation/CoreStageCombatCycle/FrameworkRuntimeComposition, existing ChapterProgressionCommands and P1 RunProgress/RunTermination fixtures.

| Evidence | Result | Coverage |
| --- | --- | --- |
| baseline.xml | 1760/1765; exit 2, 0 skips | Fresh P2 baseline after P1. |
| pure-red.xml → pure-green.xml | 0/15 → 15/15; exits 2 → 0 | Missing contracts/decisions, configured Next, explicit Final and rejection reasons. |
| detached-red.xml → detached-green.xml | 0/3 → 28/28; exits 2 → 0 | Detached preparation plus pure and existing three-algorithm graph cases. |
| publication-red-3.xml → publication-green.xml | 0/2 → 5/5; exits 2 → 0 | Missing prepared publication; next/final and detached witnesses. |
| service-api-red.xml | 0/1; exit 2 | Genuine boss settlement/EndBattle exposed missing CompleteChapter. Bootstrap was removed after typed service coverage replaced it. |
| service-behavior-red-3.xml → service-green-2.xml | 0/24 → 41/41; exits 2 → 0 | Missing service behavior and unconstrained legacy entry; GREEN includes commands/publication. |
| publication-identity-red.xml | 11/14; exit 2 | Exact cleared node/key identity guard was missing. Two additional failures were test-only collection identity expectations. |
| p2-integration-green.xml | 257/257; exit 0 | Identity guard and both cross-chapter fixture migrations. |
| p2-focused-delivery.xml | 276/276; exit 0, 0 skips | Final P2 acceptance, metadata faults, both initialization orders and P1 terminal coverage. |
| broad-delivery.xml | 1835/1840; exit 2, 0 skips | Whole three-assembly regression comparison. |

Final broad comparison has zero changed/new failure signatures using exact full name, message and stack. There are 75 added case names and no missing baseline case. The whole suite is not green: the same five baseline failures report `[CombatStageDetail] Combat stage requires Economy.`

Those failures are ArtifactDataTableTests.Catalog_RegistersOnlyMigratedRulesAndExplicitlyPendingViews and DataBaseAssetTests.Catalog_CreatesAllDataBaseGroups, CombatStages_ReferenceExistingInGameData, EveryStage_HasNodeVisual and EveryUnitAndStructure_HasUnitVisual. P2 did not modify authored Economy assets.

Intermediate compile failures were test-only namespace/private-helper mistakes, corrected before behavioral RED. An automatic retained-battle fixture initially supplied an empty character list and was corrected to valid owned-main facts. A named-composition test initially queried a nested Core key at root; it now follows the existing root InGame → CoreGroup contract and asserts root-key absence. None is counted as successful runtime verification or missing-behavior RED.

## Existing coverage and intentional migrations

Existing seeded graph algorithms, settlement, health/soul/Artifact/currency/result retention, duplicate completion, reset and P1 terminal tests are reused. Prepared publication tests add malformed edges/duplicates, changed source/target/selection, terminal Run, retained battle and map clear rejection. Service tests use genuine command victories, caller EndBattle, unavailable/mismatched definitions, invalid source/target metadata, registered-target reuse, final/next retries and route/root/final graph callback mutation.

Two existing cross-chapter fixtures now inject explicit Next definitions and matching Combat/Boss source and target metadata; their original persistent facts, prior clears, counter, cleanup and no-reset assertions remain. The old graph fixture's in-memory continuation root query is supplied through its existing StageServiceManager InitData seam. No authored asset or production execution path was changed for fixture data.

## Execution rulings and review

- Continue in the current feature checkout with incoming P1/recovery/shop dependencies and external evidence. Copying uncommitted dependencies into another checkout would duplicate work; the tradeoff is checkout isolation.
- Reuse Cycle fixtures via internal test-only access and a named derived database. This preserves genuine combat commands; the tradeoff is test fixture coupling.
- Add internal StageServiceManager.GetStageMap alongside the planned GetStage. Target metadata needs that query; the tradeoff is one internal accessor rather than a coordinator-owned DB lifecycle.
- Migrate existing cross-chapter fixture routes and Combat/Boss metadata. This preserves their original retention/reset purpose under tightened gameplay admission; the tradeoff is fixture compatibility.

Independent read-only review found no Critical, Important or Minor findings. It independently inspected the final focused/broad XML, exact unchanged failure signatures, 75 added/no removed baseline cases and clean diff check. No corrective pass or deferred minor was required.

The reviewer considered the following boundaries and declined to judge them as P2 delivery. Each remains an explicit execution ruling, not an unreported finding:

| Boundary | Ruling and reason | Cost if the boundary changes |
| --- | --- | --- |
| P3 two-chapter combat/deck/defeat integration | Pending P3; P2 does not claim this integrated runtime is delivered. | Run P3's genuine multi-chapter acceptance. |
| Production schema/adapters, Method_Key, authored/nondevelopment acceptance | Outside P2; default data unavailability stays explicit. | Implement real adapters and authored-data acceptance. |
| Job/Flow/scene timing, acknowledgement and input cleanup | Remain consumer-owned; Core supplies decisions/publication. | Consumer integration and scene acceptance. |
| Save/restart/rewards and identity after explicit reset | Excluded policies; no inferred restart or cross-Run protection. | Separate policy and identity contracts. |
| Forged decisions/setup through trusted Buffer ports | Infrastructure is trusted; coordinator is the supported gameplay consumer. | A different trust boundary would require separate admission design. |
| Concurrent mutation, restored ABA changes, installation allocation failure | Serialized contract; no revision/general rollback guarantee. | Concurrency/versioning or failure-atomicity work beyond this increment. |
| Nonterminal query reentrancy of initial/same-chapter EnterChapter | Preserve existing behavior; P2 constrains cross-chapter continuation. | Separate review of the retained legacy path. |
| Malformed-schema/lookup exception mapping | Existing exceptions may propagate with no own continuation write or inferred Victory. | Define additional typed failure mappings if consumers need them. |
| Extra visual restrictions such as duplicate width positions | P2 validates playable graph identity/connectivity; additional layout policy belongs to separately agreed validation. | Specify and test further visual layout rules. |
| Incoming recovery/shop/temporary work, existing Economy failures, generated timestamp | Preserve incoming behavior; no unrelated changes. Generated FMOD timestamp is restored after Unity runs. | Separate authored-content or subsystem work. |
| Handoff/status documents written after review dispatch | The code verdict excludes later prose; executor verifies documentation consistency and links. | A separate documentation review only if requested. |

Branch integration was not requested. Changes remain in place, uncommitted; the five baseline failures are reported rather than treated as whole-suite success. Final git diff --check passed and generated Unity processes exited. Raw evidence remains external.