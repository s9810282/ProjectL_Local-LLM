# Task 8R6 — independent Core closure and View handoff

Date: 2026-10-02. Scope: final audit of Tasks 8R1–8R6 and master Task 8 Steps 6–7. This closes independently executable Core retirement, not full Task 8 or production activation. Runtime sources, tests, assets, public APIs and assembly definitions are unchanged by 8R6.

## Ownership and source closure

The [source manifest](2026-10-02-core-combat-retirement-source-manifest.csv) contains 335 historical rows; the [method matrix](2026-10-02-core-combat-retirement-test-matrix.csv) contains 606. Their 8R5 closure fields remain the source of individual classifications. Fresh 8R6 verification found all 336 distinct witness methods passing, with no missing matrix witness. No declaration of StageSessionFactory, StageSession, StageServices, StageCombatFacade, UnitState, CombatEntityState, StageMapRunState, RunCharacterDeckState or IEnemyAiCombatGateway remains in production sources.

8R5 removed 90 old source/meta pairs and 52 legacy-only test/helper pairs, and trimmed 17 mixed sources. Two original candidate files retain only existing immutable public values: AttackExecutionResult and MovementExecutionResult. SoulFieldState.PlanCountdown, StageSoulService.PlanDeaths, fact-based EnemyAiTurnService.Calculate, timeline interval/tie calculation, effect rules/codec and shared Contracts remain legitimate pure responsibilities. SoulGrowthState remains immutable calculation data. Six separate Artifact editing feature files remain; they are not a combat state host. No obsolete owner group is blocked pending production DB. Current missing DB data does not require reinstating a deleted session.

Task 8P already removed duplicate graph/progress owners. CurrentProgressBuffer, StageProgressBuffer and MapProgressBuffer remain authoritative. CharacterBuffer owns roster, persistent main state and battle character state; CombatStateBuffer owns battle state, completion and retained results; CombatViewBuffer owns input and presentation acknowledgement. Neither retirement nor this audit adds Buffer state, gameplay formulas, scheduling responsibility, a Core combat Plan/Job, a Runner, a dispatcher or a new execution route.

The user-authorized Stage merge at 83f485d1 is part of the execution history. Changes brought by that merge are not attributed to Core retirement. Incoming 8R1–8R5 work is preserved; this checkpoint does not commit or push it.

## Actual processing and execution boundary

Source paths below are relative to Assets/ProjectL/Script/Systems/.

1. Factory/Runtime/InGame/Core/InGameCoreSystemFactory registers DatabaseBattleInitializationSource as an existing lifecycle module and exports CombatProcessingService through InGameCoreSystemGroup. Parent groups propagate InitData, PostBindService and shutdown. The source obtains current/character Buffer read services through the existing root query.
2. RuntimeSystem/InGameSystem/CoreSystem/CombatSystem/CombatProcessingService.cs reads command/current character/combat/currency/artifact facts into a command-local input. Definition/profile caches are static-definition data, not a duplicate runtime HP, graph or battle state owner. Core.Application/Domain consume Contracts; their asmdefs do not reference Systems. Framework has no reverse reference.
3. Calculate/Preview/Advance use the existing pure calculators. Core never calls CombatViewBuffer Submit, Clear, RemoveCommandsThrough or AcknowledgePresented. Preview publishes request-numbered display data without installing HP, costs, occupancy, phase or a terminal result.
4. BufferSystems/Core/BufferSystemGroup.Apply prepares character changes, soul-field changes, command/result metadata, optional draw currency and optional stage/map completion before the first installation. It validates terminal agreement, then installs prepared data and publishes the command/result last. Preparation similarly prepares both character and combat stores before installation. This is the approved common publication boundary, not a new transaction system.
5. View reads existing Buffer interfaces. Input removal and acknowledgement remain View-owned. Normal rejection can publish handling/result metadata while preserving gameplay state. Exceptions are not converted into accepted commands.

### Existing View-owned Jobs: exact next work

| Job | Current source | Existing Core call / action |
| --- | --- | --- |
| CombatPrepareJob.Execute | Already calls CoreGroup.Combat.PrepareBattle and completes after normal return | Retain existing guard and lifecycle route; real default DB preparation is still gated |
| CombatCommandJob.Execute | Placeholder throws; no processor call yet | Wire CoreGroup.Combat.ProcessNextCommand() |
| CombatPreviewJob.Execute | Placeholder throws; no processor call yet | Wire CoreGroup.Combat.ProcessLatestPreview() |
| CombatAdvanceJob.Execute | Placeholder throws; no processor call yet | Wire CoreGroup.Combat.AdvanceCombat() |

These Jobs, CombatPlan, CombatFlow and CombatLifeCycle belong to View/Flow despite their Systems directory. Preserve each Job's completed guard and mark completion after normal synchronous return, including a false return; false does not indicate a fault or authorize repeated execution until success. ProcessNextCommand returns true when it handled a command, including a published rejection; false means no eligible command. Preview/Advance false means no eligible request/automatic work. Read accepted/rejected outcome from Buffer results, not the boolean. A thrown exception must remain a fault and must not be published as success.

The existing JobRunnerSystem clears its current Job only after IsCompleted, with an immediate execution limit of 32. It does not latch a fault; Execute exceptions leave the Job available on later ticks. Framework F1 must establish fault termination before activation. Core retirement does not fix that separate owner contract by creating another Runner.

## Lifetime and replay audit

Actual Buffer/service tests cover final HP/AP/structure/completion/progress publication together, duplicate command handling, rejected commands without Core acknowledgement/removal, boss/stage clear records, voluntary defeat, Chalice settlement once, and cleanup/next preparation retention. Chapter entry preserves prior stage/map completion and persistent roster/main values. The audited ordinary progression path does not dispatch ResetRunProgress; the existing explicit NewRun branch remains separate and is not invoked by chapter entry or battle cleanup.

CombatStateBuffer.EndBattle clears current battle/soul field/preview and keeps completion, latest result and counters. Next preparation clears completion/preview while preserving latest result and numbering. CombatProcessingService.EndBattle rejects an unfinished battle; it does not enforce presentation acknowledgement. Flow must request voluntary defeat when needed, observe/present/acknowledge the final result, then invoke cleanup and permit transition. Current CombatLifeCycle.OnExit stops Flow and clears View input but does not implement this full F2 ordering.

Handled command numbers prevent already handled commands from being reapplied. View must clear remaining old input at its battle lifetime boundary; Core does not discard unhandled input. Existing actual service tests establish result/counter retention, while Flow tests use doubles for acknowledgement/re-registration decisions. They do not establish a production Job/Runner exit sequence or stale-unhandled-input safety by themselves.

## Fresh verification

Actual Unity 6000.0.73f1 EditMode run, using the approved AppData-access route:

```powershell
& 'D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'D:/My_ProjectWork/Github_desktop/Project_L' -runTests -testPlatform EditMode -assemblyNames 'ProjectL.Tests.Core;ProjectL.Tests.Framework;ProjectL.Tests.CombatViewBoundary' -testResults 'D:/My_ProjectWork/Github_desktop/Project_L/.superpowers/sdd/2026-10-02-core-combat-legacy-retirement-plan/8r6-final.xml' -logFile 'D:/My_ProjectWork/Github_desktop/Project_L/.superpowers/sdd/2026-10-02-core-combat-legacy-retirement-plan/8r6-final.log'
```

| Assembly | Passed / total | Failed |
| --- | --- | --- |
| ProjectL.Tests.Core | 860 / 860 | 0 |
| ProjectL.Tests.Framework | 319 / 324 | 5 |
| ProjectL.Tests.CombatViewBoundary | 6 / 6 | 0 |
| Total | 1185 / 1190 | 5 |

Unity exit code 2; zero skipped/inconclusive. Fresh failure names **and full messages** equal 8R1 and final 8R5. Each reports `System.InvalidOperationException : [CombatStageDetail] Combat stage requires Economy.`:

- ProjectL.Tests.Framework.ArtifactDataTableTests.Catalog_RegistersOnlyMigratedRulesAndExplicitlyPendingViews
- ProjectL.Tests.Framework.DataBaseAssetTests.Catalog_CreatesAllDataBaseGroups
- ProjectL.Tests.Framework.DataBaseAssetTests.CombatStages_ReferenceExistingInGameData
- ProjectL.Tests.Framework.DataBaseAssetTests.EveryStage_HasNodeVisual
- ProjectL.Tests.Framework.DataBaseAssetTests.EveryUnitAndStructure_HasUnitVisual

8R1 had 1602 cases; current case-name sets differ by 117 additions and 529 removals, yielding 1190. This set comparison spans migrations and the authorized Stage merge; it is not a claim that every difference was a retirement edit. The 8R5 incoming-to-final retirement ledger specifically records 527 removed cases and eight additions (1709 - 527 + 8 = 1190). The final 8R6 case set equals final 8R5 exactly. No RED replay or new tests were added for this documentation/audit step.

Current passing fixture counts: ChapterGraphPreparation 40/40, CharacterBattleSettlement 18/18, CombatCommandPublication 18/18, CharacterBattlePreparation 12/12, CombatFlow 27/27 and JobSystem 8/8. Pure/query-double tests prove calculation/definition conversion; actual Buffer/service tests prove binding/publication/lifetime; the injected-definition CombatPrepareJob test proves that existing preparation Job call. Flow/Runner doubles do not prove all four production Jobs. No separate managed harness, PlayMode or actual playable production preparation was run or claimed.

Local evidence: .superpowers/sdd/2026-10-02-core-combat-legacy-retirement-plan/8r6-final.xml/log, 8r6-evidence.json, 8r6-guid-audit.json, incoming hashes and preservation evidence. The fresh GUID audit confirms all 90 removed source/meta pairs are absent and finds no remaining reference to their GUIDs in Assets .unity/.prefab/.asset/.asmdef/.meta files. Python evidence checks compare NUnit case names/full failure messages, matrix witnesses, production owner declarations and actual asmdef references; they are static/XML audits, not another managed test run. Prior checkpoints remain historical evidence. Importer-only files are restored to incoming bytes; git diff --check and preservation are checked before closure.

## Remaining work by owner

| Dependency | Concrete next action | Owner |
| --- | --- | --- |
| Method_Key/table/query/unit reference | Supply approved schema/data; adapt actual queries in existing DatabaseBattleInitializationSource/BattleDefinitionAdapter and validate required profiles before installation | DB supplies; Core adapts/verifies after availability |
| Main/unit/Economy authoring | Supply actual definitions/references; resolve the five real-asset failures | DB, then Core verifies |
| G9 Soul/Chalice | Validate actual DB -> compiler -> command -> Buffer behavior; fixtures do not satisfy authoring acceptance | DB/designer and Core |
| Startup roster/wallet | Agree real loadout key and caller timing; call existing InitializeRoster without reacquisition/reset on battle entry | Existing execution owner; Core checks service boundary |
| Three combat Job placeholders | Wire the table above; exercise Register -> Plan -> Runner -> service -> Buffer, including rejection/preview/automatic results | View/Flow |
| F1/F2 | Prove faulted Jobs stop, and final defeat/result/ack/cleanup precede transition | Framework and View/Flow, with Core service support |
| R7/R8 | Current supported turn index is Current.CommandState.Turn; no extra revision added. CurrentFieldData has no blocked-cell authoring input; confirm the needed terrain slice before implementing it | Actual consumer/DB/designer, then relevant owner |
| Explicit gameplay deferrals | Multicell units, custom movement paths, ThornHeart/reflection ordering, deferred Rider/profile and temporary max-HP authoring remain separate decisions | Designer and assigned owner |

## Completion decision

Master Task 8 Steps 6–7 can close for current authorized ownership/retirement criteria; Task 8P is already complete. No retained obsolete owner blocks this slice. Remaining Task 8 production/joint checks stay open. View can proceed with existing processing APIs and Buffer reads now; future actual DB adaptation remains Core work once the dependency arrives.

Final independent read-only whole-execution review found no actionable correctness, ownership or misleading completion issue. The reviewer independently checked actual retained sources, migrated behavior, publication/replay/lifetime boundaries, merge attribution, the restored M249 guard, all 336 passing witness methods and exact baseline failure names/full messages. Approval is limited to independent Core Steps 6–7; all production/joint/deferred gates above remain open. The reviewer changed no files and ran no Unity tests. No additional fix pass or second review was needed.
