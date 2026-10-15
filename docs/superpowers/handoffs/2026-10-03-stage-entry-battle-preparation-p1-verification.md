# Stage Entry and Battle Preparation: P1 Verification

Date: 2026-10-03.

Scope: P1 of the [implementation plan](../plans/2026-10-03-stage-entry-battle-preparation-implementation-plan.md), following the [approved design](../specs/2026-10-03-stage-entry-battle-preparation-contract-design.md). P1 is implemented and verified in the existing `feature/Cheon/refactor/core-system` checkout. No commit, push or production caller migration was performed.

## Delivered contract

- Public immutable `BattleEntrySelection` in the existing Current Buffer namespace carries expected chapter/selection and typed target node/stage/name. Its constructor rejects malformed proposal facts.
- Internal `ICurrentProgressBufferStore.PrepareSelection` compares the complete origin and prepares final selection/shop/recovery values. `InstallSelection` installs those values without another public selection call.
- `IBattleBufferWriteSink.PrepareBattle(BattlePreparation, BattleEntrySelection)` validates canonical node/stage binding, prepares Current/Character/Combat, then installs Character, Current and Combat. The existing one-argument overload shares Character/Combat preparation and remains compatible.

Current remains the selection owner; existing Character and Combat stores retain their state. The group coordinates specific publication. Buffer performs no navigation eligibility, Combat DB policy, source lookup, Job registration, input acknowledgement or rollback. Existing factories, exports and physical store registrations are unchanged. The guarantee assumes serialized synchronous execution and does not cover concurrent/reentrant writers, process failure or out-of-memory recovery.

## Verification evidence

Evidence directory: `.superpowers/sdd/2026-10-03-stage-entry-battle-preparation-implementation-plan/`.

| Run | Result | Exit | Meaning |
| --- | --- | --- | --- |
| `p1-baseline` | 1307/1312; five failures | 2 | Fresh baseline; failures exactly match C5 names/messages/stacks. |
| `p1-red-symbol` | Compilation rejected missing contract | 1 | Initial missing API witness; an import error was subsequently corrected. |
| `p1-red-behavior` | 60/89 | 2 | Invalid Artifact fixture cells blocked setup; not used as behavioral RED evidence. |
| `p1-red-behavior-valid-fixture` | 60/89; all 29 new cases fail | 2 | Valid fixtures expose the unimplemented publication and proposal validation. |
| `p1-green` | 89/89; no skips/inconclusive | 0 | All 29 new and 60 existing focused cases pass. |
| `p1-regression` | 1336/1341; no skips/inconclusive | 2 | 29 cases added, no prior cases missing; same five baseline failures. |

Full regression: Core 918/918, Framework 412/417, CombatViewBoundary 6/6. Commands and licensed Unity 6000.0.73f1 execution are recorded in `run-tests.ps1`, per-run argument files, XML, logs and exit-code files. `regression-summary.json` and `regression-failures.json` record case-set and exact failure comparisons.

The unchanged failures are:

- `ProjectL.Tests.Framework.ArtifactDataTableTests.Catalog_RegistersOnlyMigratedRulesAndExplicitlyPendingViews`
- `ProjectL.Tests.Framework.DataBaseAssetTests.Catalog_CreatesAllDataBaseGroups`
- `ProjectL.Tests.Framework.DataBaseAssetTests.CombatStages_ReferenceExistingInGameData`
- `ProjectL.Tests.Framework.DataBaseAssetTests.EveryStage_HasNodeVisual`
- `ProjectL.Tests.Framework.DataBaseAssetTests.EveryUnitAndStructure_HasUnitVisual`

New tests use the real Buffer factory and stores. They cover first/previous selection, same/different target noncombat data, stale chapter/flag/node/stage/name, changed/missing canonical binding, distinct Current/Character/Combat validation failures, retained battle, proposal/null validation and storage-only eligibility. Failed attempts preserve previous selection/shop/recovery, persistent roster/main/souls, Artifact/currency and retained completion/result/counters. Corrected retry verifies battle and owned allocator identities. History setup supplies completion facts directly to the real internal Combat store; victory/defeat settlement remains covered by existing C4 regression, not claimed by this setup.

Both new `.meta` GUIDs are unique. Scoped whitespace checks pass. The incoming FMOD modification was preserved byte-for-byte: Unity changed only `lastScanTime`, and restoration used a backup whose SHA256 exactly matches the fresh P1 incoming hash. No unrelated source or execution file changed.

## Independent review and remaining gates

A fresh read-only reviewer inspected production/test changes and XML. No Critical or Important issue was found. One minor test-evidence improvement is deferred to P3: seed nonzero preview request/answered values to strengthen preview lifetime assertions; existing P1 cases compare them at zero/null. Inspection found no associated production defect. This item is explicitly tracked in P3.

| Condition | Status |
| --- | --- |
| P1 Buffer publication capability | Complete |
| P2 explicit Core entry/source and fixture migration | Pending |
| P3 final Core package regression/handoff | Pending |
| C: Core and Buffer implementation complete | Pending; P1 alone does not close C |
| V: View/Flow caller migration complete | Pending; no Job/Plan/Flow/LifeCycle production edit |
| D: Authored DB readiness | Pending; default Method_Key gate unchanged |
| A: Production activation | Pending |

Next authorized implementation may consume the P1 contract in P2. Existing parameterless service/Job preparation still follows its prior route. No `EnterBattle` implementation or actual DB/scene success is claimed by P1.
