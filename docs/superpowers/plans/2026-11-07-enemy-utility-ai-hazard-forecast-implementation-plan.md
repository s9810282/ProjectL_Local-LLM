# Enemy Utility AI and Hazard Forecast Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:executing-plans` for inline execution, or `superpowers:subagent-driven-development` only when delegation is authorized. Execute one approved Phase at a time. Steps use checkbox tracking. This document is a plan, not permission to implement or commit.

**Goal:** Add bounded, frame-sliced Utility AI and observable turn-boundary hazard assessment without reproducing the former enemy-pathfinding stall.

**Architecture:** Core.Application owns resumable decision work and preferences; existing combat/effect/gimmick owners supply rule facts. CombatAdvanceService owns preparation lifetime and frame scheduling; CombatPlan schedules one-shot preparation/execution Jobs. Authoritative changes still pass through the existing calculator, transition publisher, and Buffer transaction.

**Tech Stack:** Unity 6000.0.73f1, existing C# Core.Contracts/Core.Domain/Core.Application/Systems assemblies, existing NUnit Core and CoreIntegration assemblies, Unity CPU/GC Profiler. No new packages.

**Spec:** [Approved design](../specs/2026-11-07-enemy-utility-ai-hazard-forecast-design.md).

**Planning baseline:** `feature/Cheon/Core/NewCombatSytem`, HEAD `9d8c9ce45`, inspected 2026-11-07. The original planning delivery and this amendment perform no implementation, tests or profiling; execution between those document deliveries is recorded separately below. Preserve the current uncommitted prerequisite changes.

**Execution status on 2026-11-08.** The [Phase 7 candidate scope follow-up](../reports/2026-11-07-enemy-utility-ai/phase-7-allied-exposure-remediation.md) resolves excessive Hold on the 15 frozen A8/X8/H8 inputs. Item 1 Core 5,238/5,205/25/8 and CoreIntegration 1,016/967/47/2 retained the exact 72 baseline failures. The [Phase 7 item 2 investigation](../reports/2026-11-07-enemy-utility-ai/phase-7-item2-performance.md) reduced the repeated U8 normalization cost and measured N5 first-use JIT and N8 preparation allocations. The [N8 allocation follow-up](../reports/2026-11-07-enemy-utility-ai/phase-7-n8-allocation.md) reduced measured preparation allocation from 8.86 MiB to 6.18–6.30 MiB. The [residual N8 allocation follow-up](../reports/2026-11-07-enemy-utility-ai/phase-7-n8-residual-allocation.md) measured 5.03 MiB in the same service harness and retained the N5 first-use delay at 28.3714 ms in EditMode. G-atom, G-frame and G-allocation remain unmet. The [item 3 rendered validation](../reports/2026-11-07-enemy-utility-ai/phase-7-item3-live-validation.md) adds matched five-enemy Editor traces and larger stress traces. The authored Utility opening produced no unit actions and the cold preparation frame and readiness targets were unmet. F1 was partial, F4 was unverified and F5 remained mixed. See the report for the observed F2/F3/F6/F7/F8 evidence and separate failed setups. Formal user View acceptance remains pending. Default gameplay uses immediate Advance.

## Global constraints

- **Latest user instruction (2026-11-07):** Missing Core owners needed by the approved Phase may be included and implemented autonomously. Update this plan and report verified scope; do not stop for each omitted filename. Keep View changes outside this work. Preserve Framework/Job foundations: execution structure, lifecycle, responsibilities and base contracts. Necessary external changes are reported with Phase results rather than redesigning those foundations.

- Preserve current Core paths, namespace spelling including `CoreCaculator`, Framework contracts, state ownership, existing public behavior except the explicitly approved AI choice changes, and one actual action per publication.
- The user is the Core programmer. The revised Phase 2 touches only `C`/`A`/`AI`, applicable Core tests and documentation. `S`, `F`, `CF`, View/UI, Buffer, scene and asset changes are not part of Phase 2. Future Phase 3 integration requires its own scope approval; a Core-only instruction does not authorize Flow/Job implementation.
- **No commits or pushes by Codex.** No task below contains a commit step. Stop and report after each Phase; a Phase approval does not authorize later Phases.
- Design and implementation documentation is English. New code comments/XML documentation are Korean; use Allman braces, four spaces, UTF-8/LF, explicit accessibility and sealed classes by default. New C# files/folders need Unity `.meta` files; preserve existing GUIDs.
- Beam search, actor-order coordination, worker threads, Burst migration, new skills, UI, production assets, camera changes, new SO/table chains and cross-turn caches are excluded.
- Preserve ascending EntityId actor order, move-then-attack, attack-lock, mandatory taunt, hidden information, required reactions, and terminal processing order.
- **No new View tests and no combat-View MSBuild.** Existing other-author runtime tests cannot be rewritten without the user's permission. Core permission does not waive this rule. Flow/Job changes have explicit user-owned Unity checks below; do not relabel a new View test as CoreIntegration to evade the restriction.
- Core fixtures own their input data; do not load mutable production catalogs/SOs to make tests pass. Do not delete old Missing Script assets, fix unrelated failures, or alter FMOD assets.
- Unity execution in a later authorized Phase must use the AppData-capable approved path. Do not fix a sandbox CurlRequestCache.db failure by deleting caches or upgrading Unity.
- Supplied locality guidance is a review criterion, not extra implementation authority: [feature-change-locality-guidelines.md](<D:/My_ProjectWork/학교 관련 문서/졸업작품_프로젝트L/2학기 내용/AI 관련 문서/feature-change-locality-guidelines.md>). Evaluate modification, understanding and verification scope together. Necessary shared-rule regression remains required.
- A measured failure, unsupported assessment, incomplete mandatory scan, and normal no-action outcome are distinct. Never turn one into another to satisfy a time limit or a passing report.

## Review focus

1. A hidden-fact rejection can loop or drain fallback in one frame. Pin rejection progression and retained budgets in Tasks 2.3, 3.2, 6.2 and manual check F4.
2. Readiness can be stale even when battle ID is unchanged. Pin every assembler dependency and pre-execution validation in Tasks 3.1–3.2 and check F3.
3. A random displacement can contaminate a supposedly certain kill or later hazard prediction. Pin transitive coverage and seed invariance in Tasks 4.1, 5.2 and 6.2.
4. An always-zero Hold can defeat useful escape, and cap exhaustion can wrongly mean no legal action. Pin common baseline and mandatory/optional work separation in Tasks 6.1–6.2.
5. A small slice deadline cannot interrupt an expensive atom; nested flush/arrival, observation barriers, result sealing and Dispose can hide full work. Pin cursor progress/order/cancellation and complete cost accounting in Tasks 2.0a–2.0d; preserve frame stamps in Task 3.2 and final stress acceptance in Phase 7.

---

## 1. Execution rules and repository map

### 1.1 Scope of this document

This is one dependent implementation project. Assessment, search and scheduling share contracts and cannot be shipped as unrelated rewrites. The Phase boundaries below are review checkpoints, not separate global AI frameworks.

At every Phase start: read the spec, this plan's contracts, the selected Phase and previous Phase report; inspect current git status/diff and affected source. If code drift changes a named owning contract, report the exact mismatch before changing the plan. Do not infer a new architecture from an old session summary.

Within Core tasks, each listed test case means: write the case, run it to observe the expected failure, implement the minimum change, rerun that case and the named regression set. If execution is unavailable, leave the verification unchecked. A red test for missing API is expected only in the initial step, never an acceptable Phase exit. Documentation-only Phase 0 tasks do not require invented tests.

### 1.2 Exact path aliases

Expand these aliases literally; namespaces follow the resulting folder path. In a Files sentence, an unprefixed sibling filename inherits the folder of the immediately preceding explicitly prefixed file. No wildcard authoring destinations are used.

| Alias | Repository-relative path |
|---|---|
| `C` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator` |
| `A` | `C/Application/Combat` |
| `AI` | `A/EnemyAi` |
| `G` | `A/StageGimmicks` |
| `S` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CombatSystem` |
| `F` | `Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat` |
| `CF` | `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/Core/CombatSystemFactory.cs` |
| `T` | `Assets/Tests/EditMode` |
| `TI` | `T/CoreIntegration` |
| `E` | `docs/superpowers/reports/2026-11-07-enemy-utility-ai` |
| `RAW` | `Logs/EnemyAi/<run-id>`; ignored local evidence, not production content |

`E` files are created by the future Phase that produces actual evidence. Never prefill measured numbers or checkboxes now.

### 1.3 File responsibility map

| Files to create | Responsibility / visibility |
|---|---|
| `C/Contracts/Combat/EnemyAi/EnemyDecision.cs` | Public immutable selected intent, decision kind and stable identity; no simulation state |
| `C/Contracts/Combat/EnemyAi/EnemyDecisionSettings.cs` | Public immutable candidate/work caps; validated defaults; no Unity references |
| `C/Contracts/CombatEffects/CombatAssessmentCoverage.cs` | Rule-owner coverage flags; not Utility weights |
| `AI/EnemyDecisionSession.cs` | Public Core.Application entry for resumable work; private/internal implementation state |
| `AI/EnemyDecisionDiagnostics.cs` | Compact counters and bounded explanation records used by session/benchmarks |
| `AI/EnemyDecisionContext.cs` | Internal immutable input references and session-local lookup/geometry caches |
| `A/CombatAttackCalculation.cs` | Internal lifetime/progress coordinator for one attack calculation; owns existing projection/ledger and feature child cursors, not rule implementations |
| `AI/EnemyPursuitSearch.cs` | Internal resumable version of existing pursuit; old TryFind drains it |
| `AI/EnemyActionCandidates.cs` | Internal incremental attack/position candidates, shortlist and route reconstruction |
| `AI/EnemyUtilityScorer.cs` | Internal preference evaluation over facts, with a shared Hold baseline |
| `AI/EnemyThreatField.cs` | Internal public-geometry player threat approximation; no player-reply simulation |
| `A/CombatActionAssessment.cs` | Internal feature-owned legality/coverage/preview adapter; no Buffer writes |
| `G/StageHazardForecast.cs` | Internal ordered resumable forecast builder and compact cell/time facts |
| `S/EnemyAdvancePreparation.cs` | Public service preparation/result enums; Systems boundary, not a new service |
| `S/EnemyDecisionSourceStamp.cs` | Internal source validity record; values/references obtained through assembler-owned inputs |
| `F/Jobs/CombatPrepareAdvanceJob.cs` | One slice, one completed Job, zero result publication |
| `F/Jobs/CombatPreparedAdvanceJob.cs` | One prepared execution attempt; explicit Published/Stale/Retry/Cancelled handling |

Use existing files for narrow helpers: `CombatAttackQueries`, `CombatMovementCalculator`, `CombatCommandCalculator`, `CombatTurnCalculator`, `EnemyAiPolicy`, `EnemyAiTurnService`, `EnemyMovementPathfinder`, `CombatEffectCapabilities`, the four gimmick rules/composition, diagnostics and existing service/Flow files. Do not create a helper file for each scoring term or wrap each helper in a service.

Test-file destinations are listed under Tasks. No new test framework or asmdef is needed. Existing `ProjectL.Tests.Core` can see Core.Application/Domain internals; Systems receives only public Application/Contracts types. Do not broaden InternalsVisibleTo to expose the whole calculator to Systems.

## 2. Locked cross-task contracts

The following names/signatures are planning decisions. Implementations may choose private field names, but cannot independently rename or reinterpret these boundaries. Tests in later Tasks use this catalog.

### 2.1 Selected action and deterministic work

`EnemyDecision.cs`, namespace ending `Contracts.Combat.EnemyAi`:

- `public enum EnemyDecisionKind { Attack, Move, EndEnemyPhase }`.
- `public sealed class EnemyDecision`: get-only `Kind`, `ActorId`, `Aim`, `Path`, `TauntSourceId`. Factory methods `Attack(EntityId actorId, AttackAim aim, EntityId tauntSourceId)`, `Move(EntityId actorId, MovementPath path, EntityId tauntSourceId)`, `EndEnemyPhase()`.
- Attack requires a nonempty actor and no Path. Move requires a nonempty actor and a legal-shaped non-null MovementPath. End has empty actor/taunt and no path. No public Hold decision. Identity includes kind/actor/aim or complete route/taunt; do not use a hash alone as equality.

`EnemyDecisionSession.cs`, namespace ending `Application.Combat.EnemyAi`:

```csharp
public EnemyDecisionSession(CombatCommandInput input,
    IReadOnlyList<IMovementTileRule> movementRules, EnemyDecisionSettings settings);
public bool IsReady { get; }
public long WorkVersion { get; }
public EnemyDecision Decision { get; }
public EnemyDecisionDiagnostics Diagnostics { get; }
public void Step();
public void RejectSelected();
public void Dispose();
```

`Step()` performs one progress unit, then returns: one frontier expansion, target/aim item, forecast item, score-extraction item, or one substep of an owned precise calculation. It must not execute a whole Preview as an atomic unit. Large child enumerations keep their own cursors. It never calls itself, sleeps, reads a clock or drains another search/calculation. `WorkVersion` increases on completed work, state transition or finishing a finite cursor; Ready is terminal until RejectSelected. Reading Decision before Ready throws. Step after Ready is a no-op; Step after Dispose throws ObjectDisposedException; Dispose is idempotent and releases retained snapshots/projections without finishing them.

Session constructor performs argument/shape validation and stores references, not full board traversal, range queries or full profile compilation. Incremental preparation occurs in Step. `RejectSelected()` accepts only a Ready Attack/Move, preserves optional counters and rejection identities, and returns to the next permitted fallback stage.

Extend `CombatCommandCalculator`:

```csharp
public static bool RequiresEnemyDecision(CombatCommandInput input);
public static CombatCommandTransition AdvancePrepared(CombatCommandInput input,
    EnemyDecision decision, IReadOnlyList<IMovementTileRule> rules);
```

`RequiresEnemyDecision` is false for starting-controller/required-reaction work. Invalid processing/phase is still an error. `AdvancePrepared` does not choose another action; it checks normal admission and returns accepted/rejected transition for the supplied decision. Keep pending roar and post-action reaction/undo ordering identical to existing Advance. Existing `Advance(input,rules)` drains the same session for synchronous/offline callers and uses the same execution helper; the production prepared path must not call that draining branch.

### 2.2 Settings, score facts and limits

`EnemyDecisionSettings.Default` is one immutable singleton, not a fresh object on every property read. Its get-only property names are `AttackCandidateLimit=4`, `MoveCandidateLimit=4`, `RefinedMoveLimit=2`, `FollowupAimLimit=1`, `ActorPreviewLimit=8`, `DecisionPreviewLimit=16`, and `AlternateRouteNodeLimit=256`. The public constructor accepts those seven camelCase-named integer parameters with the same defaults. Reject nonpositive sizes, refined moves greater than movement shortlist, follow-up limit other than 1 in this release, and per-actor preview limit greater than the total decision limit. Scoring constants in the table below are named constants in EnemyUtilityScorer, with no new authoring/configuration surface. Caps are immutable for one decision; replacing settings invalidates the session.

The 16-call counter includes failed previews, refined move previews and follow-up previews; increment once immediately before creating a logical precise-evaluation work object, after coverage/budget admission. Substeps/resumed frames do not charge again. Started evaluations are not refunded on rejection/cancellation within a retained decision. Actor counters reset only on first entering a new actor. Decision counters never reset on fallback/actor switch. Capability checks and mandatory legality are counted separately and do not consume a fictitious full-preview allowance. Phase 2 defines these caps but does not truncate the legacy scan.

Define internal `EnemyScoreFacts` in `EnemyUtilityScorer.cs`: opposing HP-loss permille, opposing eliminations, allied HP-loss permille, allied eliminations, favorable/unfavorable action-restriction delta permille, actor hazard-loss permille, player-threat permille, engagement progress, optional subsequent-attack value, and coverage. Each HP term is capped by actual HP remaining and normalized by that victim's max HP before aggregation. Do not count one effect again as a trait-name bonus.

Initial preference constants, expressed as integer points (tuning, not new combat rules):

| Feature | Initial contribution |
|---|---:|
| Opposing HP loss | +1 per permille |
| Opposing elimination | +2,000 each |
| Allied HP loss, including actor | -3 per permille |
| Allied elimination | -4,000 each |
| Favorable/unfavorable restriction delta | +/-1 per permille |
| Actor future hazard loss | -4 per permille, capped at remaining actor life |
| Player threat | -1 per permille |
| Useful engagement progress | +20 per shortened route step, clamp [-100,+100] |
| Subsequent same-actor attack | one half of nonnegative estimated benefit; exclude position/risk already counted |

Use checked `long` arithmetic. HP normalization uses floor integer division `loss * 1000L / maxHP`. Known loss interval risk uses its upper bound for the conservative first version; no whole random outcome enumeration. Attack/Move and Hold share the same positional exposure horizon. A candidate must strictly beat Hold unless mandatory taunt applies. Equal-action ties: Attack before Move, then aim cell Y/X/direction or destination Y/X/full route order; actor order remains EntityId. A zero-gain tie prefers Hold. Confirmed enemy terminal success outranks ordinary scores only when coverage is complete; player terminal victory is the inverse. Approximate terminal guesses use ordinary features, never the terminal tier.

Restriction delta means a change in voluntary move/attack permission attributable to a supported shared status query: each newly blocked opponent capability contributes 500 permille; each newly blocked allied capability subtracts 500. Maximum one move and one attack capability per affected unit, no duration multiplier in the first version. Unsupported future expiration/secondary effects contribute no invented benefit and add coverage flags. Keep effective stat/damage mechanics in their existing owners.

### 2.3 Assessment and hazard boundaries

`CombatAssessmentCoverage.cs` defines `[Flags] public enum CombatAssessmentGap`: None=0, HiddenEnvironment=1, RandomOutcome=2, ActualOnlyReaction=4, UnsupportedEffect=8, FuturePlayerChoice=16, FutureWarning=32, UnknownPosition=64. Flags combine by OR; a downstream operation cannot clear an upstream gap. None means complete for the explicitly assessed operation, not omniscient knowledge of future player turns.

Add optional internal `IEffectAssessmentCoverageRule` to `C/Domain/CombatEffects/Rules/CombatEffectCapabilities.cs`: `CombatAssessmentGap AssessmentGaps { get; }`. It describes the supported rule's own behavior. A rule without the capability is UnsupportedEffect. Composite attack/entry assessment scans all relevant bindings, entry callbacks and actual-only pipeline hooks; checking only the attacker's traits is invalid. Owner-specific capability additions require deterministic preview/actual comparison evidence in Phase 4; do not bulk opt every trait in.

`CombatActionAssessment` internal surface:

```csharp
internal static CombatAssessmentGap GetCoverage(CombatCommandInput input,
    EnemyDecision candidate, EnemyDecisionContext context);
internal static Work Begin(CombatCommandInput input, EnemyDecision candidate,
    IReadOnlyList<IMovementTileRule> rules, EnemyDecisionContext context);
internal static bool TryAssess(CombatCommandInput input, EnemyDecision candidate,
    IReadOnlyList<IMovementTileRule> rules, EnemyDecisionContext context,
    out EnemyScoreFacts facts, out CombatCommandInput movedFacts);
```

Nested `CombatActionAssessment.Work` exposes internal `IsComplete`, `WorkVersion`, `Admitted`, `Facts`, `MovedFacts`, `void Step()` and public `Dispose()`. Result properties require completion; lifecycle semantics match section 2.5. `Begin` stores references only. Work incrementally checks coverage, starts an allowed precise child, then incrementally extracts facts. Charge its context's logical Preview allowance once immediately before child creation. Budget exhaustion retains coarse supported facts and gaps. Attack children use section 2.5; move children use Task 4.1's owner-local RouteWork and shared completion cursors. Scheduled decision work calls only Begin/Step, never draining GetCoverage/TryAssess/ProjectRoute wrappers.

TryAssess remains a synchronous adapter draining the same Work: public-facts admission failure returns false, configuration errors propagate, partial assessment returns true with gap flags. `movedFacts` is non-null only for a completed deterministic moved position and its single follow-up aim. GetCoverage is the synchronous adapter over the same incremental coverage logic. Random/unknown chains must be rejected for precise scoring **before** child creation; safe lower-detail features remain eligible. These Phase 4 APIs are not implemented by Phase 2.

Phase 4 user-confirmed interpretation: HiddenEnvironment is always retained because the current input cannot prove hidden hazards absent. IsPrecise means deterministic observed-world projection, not an actual terminal guarantee. HiddenEnvironment alone does not prevent a public deterministic child; other unmodelled observable chains do. A dead or terminal moved actor exposes no follow-up input. This preserves seed/hidden-world invariance without a second knowledge store.

`StageHazardForecast` is an internal immutable finished fact collection plus a resumable nested Builder. Builder constructor stores source references; `Step()` processes one registered-rule/forecast cell item; `IsComplete`, `Build()` expose completion. `Evaluate(CharacterCombatFacts actor, IReadOnlyList<CellCoord> path, CellCoord endpoint)` returns internal `HazardExposure` with known/possible HP loss, action restriction, gap flags and source/boundary evidence. Build checks board bounds and time horizon; Evaluate performs lookup over route/short horizon, not live settlement.

Extend `IStageGimmickRule` with `IEnumerator<HazardCellFact?> EnumerateForecast(CombatCommandInput input, StageGimmickSpec definition, StageGimmickId id, long lastBoundaryCount)`. `HazardCellFact` is a value type. Each MoveNext performs one bounded source/cell item; yield a null value for a progress item that emits no fact. False means enumeration finished. This distinction prevents an iterator from scanning an entire no-match board before its first yield. Do not prebuild a full list on first MoveNext. HazardCellFact and HazardExposure live with StageHazardForecast; facts hold rule ID, nullable cell, controller-end count, phase/contact kind, severity parameters and gaps. Execution order comes only from StageGimmickComposition's existing registry. No service per hazard.

### 2.4 Systems preparation API

`S/EnemyAdvancePreparation.cs`:

- `public enum AdvancePreparationState { NotRequired, Required, Preparing, Ready, FrameDeferred }`.
- `public enum PreparedAdvanceResult { Published, Stale, RetryPreparation, Cancelled }`.

Extend existing `ICombatAdvanceService` (no new root service):

```csharp
AdvancePreparationState ReadPreparationState();
AdvancePreparationState PrepareAdvanceSlice();
PreparedAdvanceResult AdvancePreparedCombat();
void CancelPreparation();
```

Readiness is side-effect-free and uses source stamps. It must not Assemble input or run PendingBosses on every Flow read. For starting-controller work return NotRequired directly. For enemy work with no captured input, return Required; the first preparation unit assembles once and can discover a required roar, then report NotRequired for that captured source. No AI preference work runs before that reaction.

FrameDeferred means preparation or execution already spent this frame's allowance; the Plan chooses Nothing. Ready becomes visible no earlier than the frame after the last preparation work. Prepared execution with no ready/current session returns Stale (no drain). PrepareAdvanceSlice called again in an already-used frame returns FrameDeferred without work. The prepared-execution Job rechecks FrameDeferred and completes without invoking execution if necessary. A direct caller invoking AdvancePreparedCombat in an already-used frame violates its precondition and receives InvalidOperationException before calculation; do not mislabel that programming error Stale or discard valid work.

Published means exactly one accepted Advance result. Stale discards a mismatching source session. RetryPreparation retains the rejected identity/counters and resumes permitted fallback later. Cancelled releases work for completed/ended/inactive battle/Run. Unexpected missing bindings, malformed data and calculator exceptions still fault. Never return Published without a new accepted result.

Systems time dependency is three constructor-injected values: `Func<int> readFrame`, `Func<long> readTimestamp`, `long timestampFrequency`. Keep the current constructor as an overload wired to `Time.frameCount`, `Stopwatch.GetTimestamp`, `Stopwatch.Frequency`; tests use a fake clock. Do not register a general clock service. Per-slice budget is 1.0 ms; at least one eligible progress unit may execute, then elapsed time is checked. No second unit starts after deadline, but one atom can overrun and must be reported. No scaled-time budget.

### 2.5 Resumable attack calculation (Core-only amendment)

Create `A/CombatAttackCalculation.cs`, namespace ending `Application.Combat`; add this internal factory on `CombatCommandCalculator`:

```csharp
internal static CombatAttackCalculation BeginPreview(CombatCommandInput input,
    IReadOnlyList<IMovementTileRule> rules);

internal sealed class CombatAttackCalculation : IDisposable
{
    internal bool IsComplete { get; }
    internal long WorkVersion { get; }
    internal AttackPreview Preview { get; }
    internal CombatCommandTransition Transition { get; }
    internal void Step();
    public void Dispose();
}
```

- Factory accepts Attack intent only, checks null/shape, and stores references. It does no collection traversal, projection construction, geometry, callback execution or eager result calculation. Deep validation is deterministic incremental work in the same order as the current attack path. Public Preview's existing non-Attack dispatch is unchanged.
- Each Step processes one bounded owner item, one child Step, or one phase transition; it does not loop until a child finishes/yields a useful result. Before completion every successful Step increases WorkVersion. After completion Step is a no-op. Preview/Transition before completion throw InvalidOperationException; after completion the stored results require no further calculation.
- The first calculation exception propagates unchanged; the worker becomes faulted, releases private work, and cannot resume. Later access except Dispose throws InvalidOperationException for faulted state. After Dispose all access except repeated Dispose throws ObjectDisposedException. Disposal never flushes, settles or publishes pending work.
- Preserve normal rejected transition/preview reason and blocked-contact evidence. Do not convert an exception or incomplete scan into a rejected result. No partially built result is exposed.
- Existing attack Calculate/Preview/Reaction wrappers drain the same private machinery with their existing mode/admission differences. No new public calculator/service contract. Actual-only branches remain outside preparation and execute only through actual mode.
- Private nested continuation types, phase enum and storage layout are implementation choices inside the named owners. The externally consumed factory/type/properties above are fixed. Child result handoffs retain original projection, ledger and event identity; no duplicate authoritative state.

## 3. Verification and evidence protocol

### 3.1 Verification codes used by Tasks

| Code | Exact action / passing evidence |
|---|---|
| `R0` | Read-only `git status --short`, `git branch --show-current`, `git rev-parse HEAD`; record changed paths and baseline commit |
| `RC` | Unity Test Runner, EditMode, assembly `ProjectL.Tests.Core`, run all; export NUnit XML and log. Required: no new failures versus Phase 0, and every changed/new AI test passes |
| `RI` | Same procedure, assembly `ProjectL.Tests.CoreIntegration`; same comparison rule; distinguish environment/TypeLoad failures from evaluated tests |
| `RF(name)` | In the appropriate assembly, filter by the exact new/existing test class named in the Task; run and export XML. Required: test count > 0, no failures, no unexpected skips |
| `PF` | Performance matrix and raw run evidence below; no assertion based solely on FPS |
| `UF` | User Unity compile and manual Flow/playback checks in section 12; record pass/fail individually |
| `RD` | Read-only diff check: only approved paths, Korean comments, `.meta` pairs, no Core Unity reference, no unrelated asset modifications |

Optional future batch command, only when Core test execution is authorized and the approved AppData-capable runner is available (do not run now):

```powershell
$unityEditor = 'ABSOLUTE_PATH_TO_VERIFIED_6000.0.73f1/Editor/Unity.exe'
$projectPath = 'D:/My_ProjectWork/Github_desktop/Project_L'
$runFolder = Join-Path $projectPath 'Logs/EnemyAi/PHASE-RUN-ID'
New-Item -ItemType Directory -Force -Path $runFolder | Out-Null
& $unityEditor -batchmode -nographics -projectPath $projectPath -runTests -testPlatform EditMode -assemblyNames ProjectL.Tests.Core -testResults (Join-Path $runFolder 'core.xml') -logFile (Join-Path $runFolder 'core.log')
```

Resolve the Editor path before execution; never guess a different version. For RI substitute `ProjectL.Tests.CoreIntegration` and separate output names. Do not run a second Editor against an already-open project. The real local entry route may be Test Runner instead of batch. Inspect XML `result`, counts, individual failures and log; exit code alone is insufficient. Batch `-nographics` is not a render-frame performance benchmark.

If the sandbox prevents the approved run, request the appropriate execution path rather than modifying Unity. If View checks are outstanding, state **compilation unverified / 컴파일 미확인**. Core pass does not clear UF.

### 3.2 Reproducible performance matrix

Task 1.1's recorded test-owned fixtures are the continuation baseline. All use fixed definitions and legal initial positions, separate from production SO data. The Phase 1 manifest contains 55 fixture/seed inputs, including N5 as the normal five-enemy reference. Preserve those concrete facts/hashes rather than rebuilding a different workload from this summary table. Every manifest records full geometry, unit facts, profiles, environment, gimmick definitions/state, and seed.

| Fixture ID | Required workload |
|---|---|
| N5 | Frozen normal five-enemy reference from Phase 1; same open Core setup as N6/N8, differing in enemy count |
| N6 | 16x16, 6 enemies, 4 player units, ordinary single attacks and open approach |
| N8 | Same base with 8 enemies |
| W8 | 32x32, 8 enemies, wall/corridor detours, far target; exercise old pursuit bottleneck |
| U8 | 32x32, 8 enemies, unreachable target and occupied attack positions; complete mandatory scan |
| A8 | 16x16, 8 enemies, mixed direction/all/splash/pierce profiles and friendly collateral |
| T8 | 16x16, 8 enemies, multiple taunt sources and supported damage/restriction traits |
| R8 | 16x16, 8 enemies, railway due at next side end plus known rockfall warning |
| X8 | 16x16, 8 enemies, warned tornado intersecting railway/rockfall; uncertainty and immunity |
| H8 | 16x16, 8 enemies, haboob plus known/unknown entry hazards; public-information parity |
| G16 | 32x32, reinforcement growth reaching 16 enemies; label as stress above the nominal 6–8 target |

For each fixture use seeds 1, 7, 19, 41, 97; warm up 10 identical-input decisions per fixture/seed, then measure 20 fresh-session decisions = 1,000 for the original ten plus 100 N5 decisions, **1,100 total**. Report the original ten and N5 separately as well as aggregate. Keep fixed-input microbenchmarks separate from full-side runs in which actions change the board. Record at least 20 full sides for each frozen fixture, with source reset between sequences. Do not drain the corpus in one rendered frame or include fixture setup/CSV output in the decision timer. Record cold-start work separately, not discard it.

Manifest includes commit, fixture hash/version, Unity version, Editor/player mode, scripting backend, build flags, CPU/GPU, resolution, VSync/targetFrameRate, Profiler/Deep Profile state, GC settings, warmup/sample counts. Baseline and comparison must match. Use Deep Profile OFF for primary timing; a diagnostic deep capture is separate evidence. Record per fixture and aggregate nearest-rank p95/p99 (`sorted[ceil(p*n)-1]`), maximum and mean; never average percentiles together.

Raw files: `manifest.json`, `decisions.csv`, `slices.csv`, `sides.csv`, `core.xml`, `integration.xml`, Unity logs, and original Profiler capture/export. Rows include run/fixture/seed/decision ID, actor, outcome, coverage gaps, counts, CPU ticks/ms, bytes, slices, elapsed latency, cap hits and invalidations. Include configuration fingerprints and unobserved-randomness variants separately from correctness baselines.

Reuse the existing `ProjectL/Profiler/기록 내보내기 (Claude 분석용)` menu after stopping capture. Its `Logs/ProfilerExport/<timestamp>/` output provides frame and aggregate marker evidence, **not per-decision p95/p99 by itself**. Add correlated decision/slice records in Task 1.2; never calculate per-decision percentile from marker averages.

### 3.3 Gates

| Gate | Initial criterion |
|---|---|
| G-baseline | Fresh current-source traces and raw costs exist; prior 226 ms history is not substituted |
| G-atom | Non-preemptible preparation substep target <=0.5 ms; every >1 ms item investigated and reduced/covered by explicit reviewed limits before activation. Include constructor/seal/dispose and nested work. Full Preview totals are reported separately; no pass inferred by changing labels |
| G-frame | 1 ms slice target; p95 <=1.5 ms, p99 <=2 ms; any >4 ms preparation frame investigated before acceptance |
| G-latency | Normal p95 <=150 ms, stress p95 <=300 ms; show mandatory-scan exceptions and maximum |
| G-allocation | Provisional <=256 KiB/decision and <=2 MiB/8-enemy side; bytes measured around actual preparation and separately actual execution |
| G-correctness | All new relevant cases pass; no unexplained regression; manual Flow checks pass where required |
| G-isolation | No hypothetical Buffer/RNG/reward mutation; same intent under different slicing budgets; no stale publication |

These are pending measurements, not promises. If current execution cost alone exceeds a gate, report it separately. Do not raise targets, reduce workload, drop uncertain cases, or replace exact legality with a guess without a reviewed design update. A slow atomic operation cannot be rescued solely by reducing the number of calls per slice.

## 4. Phase index and review stops

| Phase | Tasks | Deliverable | Required stop |
|---|---|---|---|
| 0 | 0.1–0.2 | Source/test/performance baseline and exact budget feasibility | Review evidence before production changes |
| 1 | 1.1–1.2 | Reproducible test-owned corpus and low-allocation diagnostics | Review measurement validity |
| 2 | 2.0a–2.0d, 2.1–2.3 | Core-only resumable Preview, decision and pursuit with current behavior preserved | Review parity and progress for development entry. Resolve atom cost before gameplay activation |
| 3 | 3.1–3.3 | Stale-safe frame preparation + Flow connection | Review Core integration and user Unity checks |
| 4 | 4.1–4.2 | Shared assessment coverage and railway/rockfall forecast | Review semantics and atomic costs |
| 5 | 5.1–5.2 | Bounded position/threat candidates and tornado/haboob assessment | Review uncertainty, work limits and locality |
| 6 | 6.1–6.2 | Utility choice, cap enforcement and synchronous/prepared parity | Review behavioral change and full regression |
| 7 | 7.1–7.2 | End-to-end performance acceptance, handoff and final evidence | User acceptance; no automatic merge/commit |

Each Phase is individually authorized at execution time. Sections below specify the work, not its completion.

## 5. Phase 0 — baseline before changing behavior

### Task 0.1: Record source, test and ownership baseline

**Files:** Read `AI/EnemyAiTurnService.cs`, `EnemyAiPolicy.cs`, `EnemyMovementPathfinder.cs`; `A/CombatCommandCalculator.cs`, `CombatCommandComposition.cs`; `S/CombatAdvanceService.cs`, `CombatInputAssembler.cs`; `F/Plans/CombatPlan.cs`, `CombatStep.cs`; `F/Jobs/CombatNextJob.cs`, `CombatAdvanceJob.cs`; the two test asmdefs. Create `E/phase-0-baseline.md` only when this Task is executed.

**Interfaces:** Consumes current public calculator/service calls and existing XML reports. Produces the verified commit/path map, test baseline, and owner-reviewed list of tests whose policy expectations may intentionally change in Phase 6.

- [ ] Run R0; record the initial dirty-file list and the design/plan revisions. Preserve unrelated work.
- [ ] Trace calculator -> advance service -> publisher -> Buffer and Plan -> NextJob -> AdvanceJob. Record which call is synchronous and which result facts establish success. Compare with spec sections 2 and 7.
- [ ] Run RC and RI through an authorized route. Record exact totals, failed test names, first exception and whether each test executed. A known old Cinemachine TypeLoadException is not assumed present or ignored without current evidence.
- [ ] Inventory the authorship and assertions of `StageEnemyAiTurnTests`, `EnemyMoveThenAttackTests`, `EnemyMovementPathTests`, `EnemyPursuitEquivalenceTests`, `TauntTraitTests`, `WallAttackAiTests`, `PiercingShotAiTests`, `ThornHeartReflectionPreviewAiTests`, and `TI/CombatAdvanceServiceTests`. Mark attack-first/zero-damage preference expectations separately from legality, taunt, movement-use and publication invariants. Do not edit them.
- [ ] Record unavailable checks as NotRun. If external test edits will be required, prepare the exact assertion change and reason for the Phase review; do not presume permission from this plan.

**Done when:** Every named current entry point resolves; baseline failures and test ownership are recorded; no code/asset changes. **Stop if:** source architecture differs, an owner restriction prevents required work, or no meaningful current verification can run.

### Task 0.2: Capture current costs and approve measurement feasibility

**Files:** Read `C/Application/Diagnostics/CoreProfiling.cs`, `S/CoreProfilerMarkers.cs`, `Assets/ProjectL/Script/Tool/Editor/ProfilerFrameExporter.cs`, `ProfilerFrameScan.cs`; append evidence to `E/phase-0-baseline.md`. Raw evidence goes under RAW.

**Interfaces:** Consumes existing `Core.Combat.*` and `Core.Calc.*` markers. Produces a baseline budget table with assembly, pathfinding, range, aim preview, calculation, publish, GC and frame cost separated.

- [ ] Ask the user to reproduce the previously problematic enemy-turn situation in Unity using the current code, plus an ordinary 6–8 enemy encounter. Record exact scene/configuration and capture settings. This user capture is execution-phase work, not a request to implement a View tool.
- [ ] Export original capture and existing exporter CSV/summary. Record measured frame and marker time/Calls/GC; distinguish exclusive/self and inclusive parent times to avoid double counting.
- [ ] Identify the slowest individual preview/range/path-position/assembly/publish operation. If only aggregate calls are available, mark atomic maxima Unknown and require Task 1.2 to measure them; do not divide total by calls and call it the worst atom.
- [ ] Compare current costs with gates in section 3.3. Record which targets are feasible, unknown or already exceeded before AI changes. Document that frame slicing alone cannot fix an oversized atom.
- [ ] Freeze the recorded current-scene baseline; Phase 1 adds the standardized fixture baseline without replacing or misrepresenting this capture. No optimization is performed in this Task.

**Phase 0 exit:** R0, available RC/RI and current profiler evidence reviewed. Unknown per-atom evidence may proceed only to measurement Phase 1. No behavior/scheduling implementation starts until G-baseline and Phase 1's atom assessment are reviewed. Report baseline failures and measurement blockers separately.

## 6. Phase 1 — repeatable measurements and diagnostics

### Task 1.1: Build test-owned workload manifests and parity traces

**Files:** Create `T/EnemyAiBenchmarkFixtures.cs`, `T/EnemyAiBaselineTests.cs`, `TI/EnemyAiPreparationTestData.cs`, `TI/EnemyAiBaselineServiceTests.cs`; create `E/fixture-manifest.json`, `E/baseline-traces.tsv`, `E/phase-1-measurement.md`. Read existing `T/CombatCommandTestData.cs`, `EnemyMovementPathTests.cs`, `CombatTraitTestFixtures.cs`, `RailwayGimmickTests.cs`, `TornadoGimmickPreparationTests.cs`, and `TI/CoreIntegrationTestData.cs`. Do not alter production content.

**Interfaces:** Test-only `EnemyAiBenchmarkFixtures.Build(string fixtureId, int seed)` returns CombatCommandInput; fixture IDs are section 3.2. Test-only `EnemyAiPreparationTestData.Create(string fixtureId, int seed)` returns a disposable Systems composition with explicit Buffer handles/service access, following existing CoreIntegration composition. These helpers live in different test assemblies: no cross-test-assembly dependency is added.

- [ ] Define each fixture's concrete coordinates/profiles/hazards using the existing builders. Save the complete generated input manifest and stable hash before collecting results. N6/N8 differ only in declared enemy count; W8/U8 differ in reachability; seeds do not alter specified visible initial facts for seed-invariance cases.
- [ ] Add `FixtureManifest_RebuildsIdenticalVisibleFacts`: two builds with same fixture/seed have equal public facts, profile configuration and serialized manifest hash. Add `Fixtures_HaveDeclaredActorsAndValidBoard`: counts match, all occupied cells fit, actor IDs are unique, phase is Automatic/Enemy and normal calculator admission succeeds where intended.
- [ ] Run RF(EnemyAiBaselineTests) red/green for fixture validity. Never make a broken fixture valid by weakening production validation.
- [ ] Capture the old policy's actor/kind/aim/path/taunt result and accepted final action-use/event order for every seed. Export baseline traces with source commit/hash; manually inspect N6, W8, T8 and R8 representative rows. Preserve the old rule outputs as immutable evidence through Phase 3. Do not copy the entire AI implementation into a new production/reference framework.
- [ ] Build CoreIntegration fixtures independently with the same manifest-defined facts through existing test-owned database stubs. Assert observable source facts match the manifest, rather than referencing Core test internals. Add service baseline cases for one accepted Advance, normal no-op, pending input preservation and terminal suppression.
- [ ] Run RC/RI. Confirm changes are test-only and existing checks are not altered.

**Done when:** Fixture generation is reproducible, current policy traces exist, no production SO dependency. **Stop if:** fixture setup silently changes initial combat rules or cannot reproduce its hash.

### Task 1.2: Add cheap counters and measure individual atoms

**Files:** Create `AI/EnemyDecisionDiagnostics.cs`, `T/EnemyAiPerformanceMeasurements.cs`, `TI/EnemyAiServicePerformanceMeasurements.cs`. Modify `C/Application/Diagnostics/CoreProfiling.cs`, `S/CoreProfilerMarkers.cs`; add only necessary sample boundaries to existing AI/calculator entry points. Create raw CSV/manifest under RAW and update `E/phase-1-measurement.md`.

**Interfaces:** Public `EnemyDecisionDiagnostics` contains numeric counts (`WorkUnits`, `ExpandedNodes`, `GeneratedAttacks`, `GeneratedMoves`, `PreviewCalls`, `CacheHits`, `RejectedAttempts`, `CapHits`) and a bounded explanation of at most 4 alternatives. No Stopwatch/Unity type lives in Core diagnostics. Systems/test harness owns timestamp/byte samples and output.

- [ ] Append CoreProfileSection values without renumbering existing entries: EnemyPreparation, EnemyCandidates, EnemyHazardForecast, EnemyCheapScore, EnemyPreciseAssessment, EnemySourceValidation. Existing marker adapter must map every enum value. Preserve old markers for comparison.
- [ ] Add `Counters_DoNotAffectSelectedAction` and `ExplanationStorage_IsBoundedAtFourAlternatives`; disabled explanation formatting must allocate no per-candidate strings. Counters must not advance actual game RNG or time.
- [ ] Measure each full preview using before/after monotonic timestamp and current-thread allocated-byte counter if supported. Verify the counter's availability in this exact runtime; if unavailable, use correlated Profiler allocation samples and mark missing per-call precision. Do not install a performance package to obtain it.
- [ ] Run section 3.2's 1,000-decision baseline corpus and separate full-side sequences. Construct fixtures outside timers; allocate sample storage before warmup; write CSV after measurement. Track maximum atomic cost, not only average.
- [ ] The CoreIntegration harness may yield frames between Core-service calls to measure scheduling later; it must not instantiate UI/presentation or new View tests. Pure-loop CPU measurements and actual rendered-frame user capture are reported as different evidence.
- [ ] Review G-atom and G-allocation feasibility. For each oversized operation list its owner, inputs, sample, minimal proposed remedy and unchanged semantics. If a remedy requires design changes, stop for that decision; do not use an unbounded operation as a supposedly bounded Step.
- [ ] Run RC/RI and RD. Record instrumentation overhead with explanation disabled/enabled separately.

**Phase 1 exit:** Reproducible raw measurements, valid percentiles, matched source hashes and approved atom strategy. Workload and target revisions require explicit evidence; no performance improvement is claimed yet.

## 7. Phase 2 — resumability with current decisions preserved

This Phase changes execution granularity, not Utility preferences. Full current attack scan behavior remains until Phase 6 replaces it; the new caps are defined but must not truncate the legacy-parity path. This temporary migration state is documented and removed in Task 6.2, not exposed as a permanent gameplay mode.

**Phase 2 entry history.** The user approved the §12 remediation strategy. The original whole-Preview measurement remains a historical failure. Tasks 2.1–2.3 were implemented against frozen oracles while 2.0d remained open. The current development entry and gameplay activation rules are in §8.

**Current implementation checkpoint.** The Phase 2 Core decision, execution and pursuit changes are present. Correctness work continued alongside the unresolved atom investigation under the user's Phase 2 authorization. Task 2.0d still blocks gameplay activation. Use the [decision verification report](../reports/2026-11-07-enemy-utility-ai/phase-2-decision-resumption.md) for exact verified and outstanding evidence.

### Task 2.0a: Freeze complete Preview semantics and substep measurement rules

**Files:** Create `T/AttackPreviewParityTests.cs`, `E/phase-2-preview-reference.json`; extend `T/EnemyAiPerformanceMeasurements.cs` for matched instrumentation and later substep records. Read the prerequisite report, frozen fixture/trace files, existing target/trait/reflection/movement/terminal test builders. Preserve every existing oracle.

**Interfaces:** Test-only semantic capture in AttackPreviewParityTests serializes accepted/reason, Preview fields, ordered event kind and meaningful payloads, expected/final character/structure/status/item facts, action-use, outcome, environment/gimmick/random state, soul/owned/reward/inventory changes and dependency evidence. Ordered collections retain order; only unordered maps get canonical keys. No object-identity or duration assertions enter semantic comparison. Unknown event/fact kinds fail the serializer explicitly rather than being omitted.

- [ ] Capture from the still-current synchronous calculator before changing its execution path. Hash the reference and the exact input/source manifest; inspect representative normal, rejected, forced-movement and terminal records. Preserve the 500 Phase 1 side traces independently.
- [ ] Add test-owned vectors using existing builders for empty/out-of-range/hidden-rejected aims, single/direction/all/splash/pierce, ordered defense/Sacrifice, reflection attacker death, Pull interruption, knockback/ImpactBolt intermediate versus final arrivals, obstacle explosion, tornado/random contact, taunt/shield presence, Acceleration/Hunter scope completion, death/rewards and terminal cleanup. Include multiple units sharing one profile but different current states.
- [ ] Run RF(AttackPreviewParityTests) on the old path to validate the fixture/reference (this is baseline capture, not a fabricated red test). Later resumption tests must fail against absent/incomplete work and pass against the implementation. Do not rewrite the reference to match new output.
- [ ] Specify `preview-work.csv`: fixture/seed/decision/preview ID, owner/phase/item, work version, parent ID, Step/constructor/seal/dispose ticks and supported bytes. Record total Preview CPU, Step count, cold/warm identity, and a no-profiler matched end-to-end series. Preallocate measurement storage outside timers.

**Done when:** a new shared worker cannot pass merely by agreeing with its own draining wrapper, and every reported work cost has an attributable owner/phase.

### Task 2.0b: Resume projection observation and completion in their current owners

**Files:** Modify `A/CommandAttackProjection.cs`, `MovementProjection.cs`, `TraitPresenceReconciler.cs`, `CombatCommandCalculator.cs` (Finish only), `AttackResultFactory.cs`; create `T/AttackProjectionResumptionTests.cs`. Read `C/Contracts/Combat/CombatCommandInput.cs`, `CombatCommandState.cs`, `CombatCommandTransition.cs`, `CharacterChanges.cs`, `C/Contracts/Results/AttackResult.cs`, `AttackPreview.cs`. If measured copying/sealing needs internal incremental construction, those exact contract files may receive owner-local builders; preserve public signatures, validation and immutability. No unchecked public constructors.

**Interfaces:** Internal nested construction/observation/completion work in existing owners uses Step/IsComplete/WorkVersion and completion-only typed results. Exact private helper names/storage are left to this task. Existing synchronous owner methods drain the same work; root factory/lifecycle remains section 2.5, implemented in 2.0c. Preserve the original source input plus one private projection; these are source/proposal, not mirrored authoritative stores.

- [ ] Add `ProjectionConstructionDoesNotTraverseInConstructor`, `CurrentInputDoesNotExposePartialReconciliation`, `PresenceRecipientsReadOneCapturedPopulation`, `MutationBetweenObservationsRebuildsCurrentFacts`, `FinalResultDoesNotAliasMutableBuilder`, and `CancelledObservationDoesNotApplyProposals`. Run red before implementation, then verify ordered expected/final facts against 2.0a.
- [ ] Incrementally capture/index source facts. Resume CurrentInput's reconciliation and snapshot creation as an exclusive observation barrier; retain current objects while suspended, not a fresh copy on each Step. Invalidate/discard its read context at the next dependent mutation; no global cache or new Buffer revision.
- [ ] Separate presence evaluation from record/application without changing source population, shield/taunt event ordering or per-recipient expected/final identity. Yield during candidate enumeration and event assembly too. Preserve each original reconciliation point; do not coalesce across damage/movement boundaries.
- [ ] Resume result/completion traversal in original order: weak-ground/death/Hunter work, final input, Finish outcome and cleanup, presence, kills/rewards/experience, main-health/soul settlement, final state/event assembly. Calls into rule owners remain shared. Measure their remaining leaves before calling them bounded; carry their nested cursor if needed under 2.0c's owner list.
- [ ] Run RF(AttackProjectionResumptionTests), RF(AttackPreviewParityTests), RC/RI, and per-owner measurements including the final result seal and Dispose. No fast path may skip required validation or silently drop fields because no change was observed in an earlier Step.

**Done when:** these owner results and observation barriers match the old semantics. Nested attack/arrival leaf costs may still be provisional here; 2.0c supplies their continuations and 2.0d accepts the complete reachable call graph. Do not declare whole-preview boundedness from this intermediate task.

### Task 2.0c: Drive the shared attack pipeline as finite Core work

**Files:** Create `A/CombatAttackCalculation.cs`, `T/AttackPreviewResumptionTests.cs`; modify `A/CombatCommandCalculator.cs` (internal BeginPreview/attack adapters), `CombatAttackCalculator.cs`, `CombatAttackQueries.cs`, `CombatAttackDelivery.cs`, `CommandAttackPlan.cs`, `CommandAttackLedger.cs`, `AttackDeliveryExecutor.cs`, `AttackDirectDamageCalculator.cs`, `CombatDamageApplication.cs`, `SacrificeDamagePlanner.cs`, `ForcedDisplacementPathCalculator.cs`, `WeakGroundCalculator.cs`, `AttackKnockbackCalculator.cs`, `AttackForcedMovement.cs`, `PullAttackCalculator.cs`. Child cursor extraction is permitted only as needed along this Preview call graph in existing `A/MovementEnvironmentCalculator.cs`, `ObjectArrivalCalculator.cs`, `ObstacleReactionCalculator.cs`, `TornadoCalculator.cs`, `HunterApplication.cs`, `CommandDeathSettlement.cs`, `TraitKillSettlement.cs`, `BattleCompletionCalculator.cs`, `Experience/CombatStageRecordSettlement.cs`, `Experience/CombatExperienceSettlement.cs`. Keep rule mathematics and public behavior unchanged; unrelated rules/turn-end redesign are outside scope.

**Interfaces:** Section 2.5; consumes the observation/completion workers from 2.0b. Owner helpers remain in their owning files, using nested/private finite cursors. The latest user instruction permits necessary omitted Core owners: record their dependency and include them without another permission pause. Preserve Framework/Job foundations and do not implement View changes.

**Core owners added during execution.** `C/Contracts/Combat/MovementEnvironmentFacts.cs` retains the public visibility projection while yielding per source. The existing `C/Domain/Combat` owners `AppointShapeCalculator.cs`, `AttackSelectionResolver.cs`, `AttackTargetResolver.cs`, `AttackOcclusionResolver.cs`, `WallGeometryBuilder.cs`, `AttackElevation.cs` and `PierceTargetCollector.cs` retain their ordering and rule checks in shared iterators. `C/Domain/Movement/EnvironmentTerrainRules.cs` supplies the same terrain filter to elevation's synchronous and resumable reads. Construction and final collection copies still need the Task 2.0d evidence.

- [ ] Add `BeginStoresReferencesWithoutExecuting`, `ResultBeforeCompleteThrows`, `EveryStepMakesFiniteProgress`, `CompletedStepIsIdempotent`, `DisposedWorkerCannotResume`, `FaultPropagatesWithoutRetry`, and `NoPartialResultBeforeSeal`. Make lifecycle tests red before implementing the factory/coordinator.
- [ ] Add `AllPartitionsMatchFrozenPreview` for partitions 1/2/31 and all 2.0a vectors; compare full ordered semantic output. Add `NestedNoMatchScanYields`, `HealthBatchFlushOrderIsPreserved`, `ReflectionDeathStillCompletesLaunchedPierce`, `PullArrivalPrecedesRebuiltDelivery`, `FinalObjectsFollowActionUseAndHitCapture`, `NoHuntersStillConsumesAccelerationAtScopeFinish`, and `TerminalSuppressesOnlyOriginalLaterWork`.
- [ ] Extract the existing attack sequence into owner continuations. Preserve pending batches, hit/arrival indices, deferred deaths, original event insertion indices, captured attack groups, scope ancestry and local random cursor. A parent advances only one child unit. Do not call old draining Resolve/Complete/Preview from a supposedly bounded Step or create a copied preview-only damage path.
- [ ] Route existing synchronous attack Calculate/Preview/Reaction through this shared machinery. Preserve public mode-specific admission, empty-area/null reaction output, actual-only behavior and non-Attack dispatch. Final actual execution is still synchronous and separately measured.
- [ ] Add `CancelAtEveryObservedBoundaryLeavesSourceUntouched`, `FaultDuringChildDropsRetainedWork`, `TwoSuspendedPreviewsDoNotShareMutableState`, `RandomDrawAndEventHappenExactlyOnceAfterResume`, and `DisposeNeverFinishesSettlement`. Compare original input/RNG/owned/soul/inventory/rewards throughout suspension, including late seal cancellation.
- [ ] Run RF(AttackPreviewResumptionTests), RF(AttackProjectionResumptionTests), RF(AttackPreviewParityTests), RC/RI and existing trait, reflection, movement, damage-order and publication suites. All new cases must pass; old known failures remain separately identified.

### Task 2.0d: Establish the revised atom gate for gameplay activation

**Files:** Extend `T/EnemyAiPerformanceMeasurements.cs`, `AI/EnemyDecisionDiagnostics.cs` and `C/Application/Diagnostics/CoreProfiling.cs` only for necessary counters/markers; update `E/phase-2-resumable-preview.md` and evidence manifests. No Systems/Flow edits or new authoring assets.

**Interfaces:** Uses BeginPreview/Step with an external test harness; records logical Preview calls separately from child work. Matched source hashes and normalized full results accompany each measurement. Existing marker ordinals remain stable.

- [ ] Measure the frozen corpus and new cascade vectors with partitions 1/2/31. Include constructor, first Step, filtered items, child fan-out, final seal and release; record maximum atom and total work, not just average Step time. Measure a matched no-profiler end-to-end run before/after integration.
- [ ] Verify the target <=0.5 ms per non-preemptible preparation item and investigate every >1 ms sample. A remaining slow callback/constructor gets an owner/input/sample/disposition entry. Refine it within the named owner scope or stop with a concrete contract issue; never return partial results to meet the budget.
- [ ] Check complete Preview CPU/allocations/Step count and cold latency alongside atom distributions. Unsupported byte probes remain unavailable. No unmeasured allocation, scheduler or GC guarantee; no claims about real frames from batch loops.
- [ ] Run RF(AttackPreviewResumptionTests), RF(AttackPreviewParityTests), RC/RI and RD; record independent review of mutation barriers/order, full failure identities and the next entry condition. Do not mark the atom strategy verified merely because this document authorized its implementation.

**Done when:** the original outputs, finite per-call work and safe cancellation are verified, and the atom measurements have the required disposition. Record implementation, G-atom and allocation evidence separately. The §8 development entry rule permits Phase 3 work while this activation gate remains open.

### Task 2.1: Define decision/session contracts and execution adapter

**Files:** Create `C/Contracts/Combat/EnemyAi/EnemyDecision.cs`, `EnemyDecisionSettings.cs`, `AI/EnemyDecisionContext.cs`, `AI/EnemyDecisionSession.cs`, `T/EnemyDecisionSessionTests.cs`. Modify `A/CombatCommandCalculator.cs`, `AI/EnemyAiTurnService.cs` only at the selection/execution seam.

**Interfaces:** Produces section 2.1 contracts and immutable settings; uses existing CombatCommandInput/MovementPath/AttackAim. `EnemyDecisionContext` stores the session's source and constructs indexes incrementally; it never rereads Buffer or a live DB.

- [ ] Add `DecisionFactories_RejectInconsistentPayload`, `DecisionBeforeReady_Throws`, `DisposedSessionCannotResume`, `StepReadyIsIdempotent` and `ConstructionDoesNotEnumerateBoardOrPreview`. Assert Settings caps equal 4/4/2/1/8/16 exactly; invalid cap relationships throw ArgumentException/ArgumentOutOfRangeException.
- [ ] Implement the payload/settings classes and session lifecycle; use Korean contract comments. Make WorkVersion monotonic and Ready readable only with a complete decision. Store no clock in Core.
- [ ] Extract a supplied-intent execution seam from EnemyAiTurnService: attack and movement still call existing command calculation; taunt recording remains the same event/origin ordering; EndEnemyPhase uses CombatTurnCalculator. Expose it through AdvancePrepared. Do not add a separate damage path.
- [ ] Add `PreparedAttackMatchesExistingActionSettlement`, `PreparedMoveHasOneSettlement`, `PreparedEndUsesExistingHazardOrder`, `PendingRoarPrecedesSuppliedOrdinaryIntent` and `PostActionReactionsAreNotDuplicated`. Compare outcome, action-use, expected/final facts and ordered meaningful event fields, not object-reference equality.
- [ ] Run RF(EnemyDecisionSessionTests), RC and RI. Keep the live synchronous Advance path unchanged until the following Tasks provide parity.

**Done when:** Payload semantics and execution seam are exact; no product behavior changes. **Locality:** understands only action lifecycle/admission/publication contracts, not individual trait implementations; shared effects still require regression because the seam executes them.

### Task 2.2: Make pursuit and aim enumeration resumable

**Files:** Create `AI/EnemyPursuitSearch.cs`, `T/EnemyPursuitResumptionTests.cs`; modify `AI/EnemyMovementPathfinder.cs`, `AI/EnemyAiPolicy.cs`, `A/CombatAttackQueries.cs` only for bounded enumeration/shared reusable query context. Read `T/EnemyPursuitReference.cs` and `EnemyPursuitEquivalenceTests.cs`; do not change their expected behavior.

**Core owner added during execution.** `C/Domain/Movement/MovementRouteValidator.cs` builds the pursuit-local traversal index from the same public terrain and occupancy rules and supplies incremental final-route validation. `C/Domain/Combat/AttackSelectionResolver.cs` shares its aim shape validation with the target-directed cursor.

**Interfaces:** Internal `EnemyPursuitSearch(CombatCommandInput input, EntityId targetId)`, `bool IsComplete`, `void Step()`, `bool TryGetPath(out MovementPath path)`. Existing `EnemyMovementPathfinder.TryFind` drains this same search. `TryGetPath` before completion throws; after completion returns original success/path semantics. Original `CanAttackFrom` keeps its existing signature.

- [ ] Add `OneNodeStepsMatchDrainedSearch`, `EveryResumePartitionMatchesReference`, `FirstGoalLayerIsCompleted`, `NoGoalTerminates`, `KnownMineAndLeapSemanticsRemain`, and `StepDoesNotRebuildWallsOrProfilePerCell`. Use original equivalence scenes and fixed neighbor order Right, Up, Left, Down; goal ties remain cost/distance/Y/X.
- [ ] Move frontier, labels, first goal distance, AttackReach and route-prefix state into the search instance. One Step consumes one resumable item; if a position has many aims, preserve an aim cursor so one node cannot drain an unbounded attack scan. Reuse original admission/range/selection conditions.
- [ ] Keep target-directed prefilter before WithWorld/selection, cache static geometry per search/context, and stop only after completing the first goal-distance layer. Do not reinstate full GetAttackRange at every BFS position.
- [ ] Add `internal static IEnumerator<AttackAim?> EnumerateAimsThatCanHit(CombatCommandInput input, CharacterCombatFacts actor, CombatAttackProfile profile, IReadOnlyCollection<CellCoord> targetCells, MovementEnvironmentFacts environment)` to CombatAttackQueries. One MoveNext is one candidate-generation/check item; null is progress without an emitted aim, false is completion. Build target-derived candidates incrementally into stable ordered storage (one target/offset insertion per item), not one monolithic sort/ToArray. Emit aims in the old order. Existing AimsThatCanHit drains non-null results for compatibility; the session never drains the iterator in one Step.
- [ ] Run RF(EnemyPursuitResumptionTests), RF(EnemyPursuitEquivalenceTests), RC and PF W8/U8. Require same routes and no additional full range query underneath per-cell pursuit. Check single Step maxima against the atom strategy, not only total runtime.

**Done when:** Every partition produces the old pursuit result; incremental state actually advances. **Stop if:** shared selection no longer matches original behavior or a Step hides the original whole-board operation.

### Task 2.3: Convert current attack-first decision into the session state machine

**Files:** Modify `AI/EnemyDecisionSession.cs`, `EnemyDecisionContext.cs`, `EnemyAiPolicy.cs`, `EnemyAiTurnService.cs`, `A/CombatCommandCalculator.cs`; create `T/EnemyDecisionParityTests.cs`, `T/EnemyDecisionRejectionTests.cs`.

**Interfaces:** Session stages are Initialize -> Actor -> TauntAdmission -> CurrentAttacks -> Pursuit -> Ready, with NextActor and EndEnemyPhase outcomes. Stores cursor/provisional best aim, not a List of speculative world transitions. Advance's synchronous path drains the session and invokes AdvancePrepared; prepared runtime is connected in Phase 3.

- [ ] Add `CapturedBaselineTraceMatches`, `SliceSizesOneTwoAndThirtyOneChooseSameIntent`, `MandatoryTauntScanDoesNotMeanNoPathWhenYielded`, `LowestIdMovesThenAttacks`, `UnknownFactsDoNotEnterCheapSelection`, and `NoIntermediateStateOrRandomMutation`. Use Task 1.1 traces and existing taunt/movement tests.
- [ ] Transfer the current lexicographic rank/pursuit preference without introducing Utility/caps. Keep one BeginPreview child across CurrentAttacks Steps; advance it once per session Step and propagate progress. Increment logical PreviewCalls once at creation, not on each child Step. Incrementally read its completed events/targets into the existing damage/rank facts, then dispose it; retain only the best intent/rank. Context construction, taunter scans, target-directed aim enumeration and score extraction remain resumable.
- [ ] Add `DecisionStepNeverDrainsPreview`, `PreviewCallCountIndependentOfPartition`, `SourceCancelledDuringPreviewDropsChild`, `LargeProjectionScoreExtractionYields`, and `RejectedExecutionRetainsLogicalPreviewCount`. Partition sizes 1/2/31 must produce identical selected intent, final logical preview count and frozen side traces.
- [ ] Implement exact rejection progression: when the chosen attack is rejected due to hidden facts, suppress every other attack attempt by that actor for this source snapshot and continue to existing pursuit fallback. When that movement is rejected, skip that actor. Preserve rejection identity/counters. Other exceptions are not hidden-fact rejections. If all actors are exhausted, return EndEnemyPhase once.
- [ ] Preserve the current returned-rejection contract: after a publicly admitted attack returns `Accepted=false`, advance to that actor's pursuit; after an admitted move returns `Accepted=false`, advance to the next actor. The scorer never branches on the hidden reason. Record the Reason only in development evidence. Source mismatch is handled separately by Systems before execution, and thrown configuration/calculator exceptions still propagate. Do not create a new reason-name whitelist that silently changes legacy fallback behavior.
- [ ] Add `RejectedAttackNeverTriesAnotherAimOfSameActor`, `RejectedMoveAdvancesActor`, `RetryDoesNotResetCounters`, `EachRetryRemovesAnAttempt`, and `TerminalReactionStopsSelection`. Assert same final accepted transition as the old baseline for covered rejection fixtures.
- [ ] Switch the pure synchronous Advance to drain the session only after parity passes. Remove duplicate selection loops from the old turn service; retain narrow wrappers needed by existing callers/tests.
- [ ] Run RC/RI and PF; report any total-time/allocation overhead introduced by resumable state. Do not claim frame protection before Phase 3 runtime scheduling exists.

**Phase 2 record.** Preview, decision and pursuit parity, progress, private-state isolation and cancellation evidence support Phase 3 development entry. Keep Task 2.0d open until the measured substep disposition is accepted. Include setup, seal, release and score extraction along with full Preview totals. The legacy scan keeps its original uncapped candidate set.

## 8. Phase 3 — frame preparation, validity and Flow wiring

**Development entry approved and resumed on 2026-11-07.** The user's explicit request to proceed in this chat supersedes the earlier documentation-only stop. Execute Tasks 3.1–3.3 and stop for review; gameplay activation remains a separate gate.

**Implementation scope clarification.** `InGameCoreSystemFactory.cs` passes the explicit diagnostic opt-in to `CombatSystemFactory`; both default to immediate Advance. The existing CoreIntegration asmdef references the already installed `UnityEngine.TestRunner` assembly only to allow a Core-service measurement to yield between Editor updates. No new package, View/Flow test, or Framework/Job foundation change is introduced. `TI/EnemyAdvancePreparationMeasurements.cs` contains that explicit measurement; it does not establish gameplay rendering or user Flow acceptance.

**Gameplay activation gate.** During Tasks 3.2 and 3.3, exercise preparation and prepared execution through explicit test or diagnostic composition. The default game composition continues through the existing immediate Advance route. Keep one advance service and the existing Buffer publisher and one-shot Job contracts. Before selecting the prepared route in normal gameplay, require the existing G-atom disposition and Phase 3 frame, correctness, isolation and Flow evidence, including the existing performance criteria. Record failed or missing measurements under their actual status.

### Task 3.1: Capture and validate the full decision source

**Verified checkpoint:** implementation and equivalent parameterized behavioral groups are recorded in the Phase 3 report. The final suite passed all 53 new cases. Case labels below are planning labels; source identity and replacement are exercised at their owning boundaries. Measurement limits and Pending user Flow acceptance remain explicit.

**Files:** Create `S/EnemyDecisionSourceStamp.cs`, `TI/EnemyDecisionSourceStampTests.cs`; modify `S/CombatInputAssembler.cs`, `ArtifactLoadoutReader.cs` only for necessary source identity access. Read `BattleLifetime.cs`, Buffer read contracts and existing revisions. No new Buffer/revision service.

**Interfaces:** Internal assembler methods `AssembleDecision(CombatActionRequest request, out EnemyDecisionSourceStamp stamp)` and `IsCurrent(EnemyDecisionSourceStamp stamp)`. Existing Assemble remains unchanged for other consumers. A stamp is immutable; it contains actual read-source identities/values, not newly allocated equivalent objects such as a rebuilt ArtifactLoadoutSnapshot.

- [x] Enumerate every Assemble dependency in a source-stamp table: battle ID/current command state/query revision/result sequence/controller sequence; character IDs and immutable facts; owned roster revision/records/main/soul counts; membership; soul field; installed definitions/profile inputs; artifact source snapshot plus immutable definition inputs; inventory; gold/fame; configuration identity. Record why each invalidates. Do not assume QueryRevision covers artifacts/currency.
- [x] Add one test case per dependency in `ChangedDependencyInvalidates_...`, plus `UnchangedReadIsCurrentWithoutReassembly`, `PreviewAnswerDoesNotInvalidateWorld`, `BattleIdChangeCannotReuseSession` and `SourceChangesDuringAssemblyDiscardCapture`. Compare before/after read stamps around assembly; inconsistent assembly is not published.
- [x] Implement currentness checks using existing revision/value comparisons and captured immutable references. Character identities may be checked in a loop without making new arrays every slice. Value-type snapshot identity uses source-owned immutable fields/content, not boxing ReferenceEquals. Definition changes must be covered by the installed-definition contract; runtime DB reads with mutable meaning must be pinned or detected explicitly.
- [x] Add `ValidationDoesNotHashOrCopyBoard` and a measured repeated-read allocation case. Fresh source validation at execution compares the actual dependencies of the decision; it must not compare only actor position/HP.
- [x] Run RF(EnemyDecisionSourceStampTests), RI and PF source-validation/assembly atoms. Report any uncovered dependency as a blocker, not a future optimization.

**Locality:** Assembler knows source locations; decision/scorer knows only immutable Core input. Changing a source binding later affects assembler/stamp tests, not hazard preference code.

### Task 3.2: Own one session and enforce frame budgets

**Files:** Create `S/EnemyAdvancePreparation.cs`, `TI/EnemyAdvancePreparationTests.cs`; modify `S/ICombatAdvanceService.cs`, `CombatAdvanceService.cs`, `CF`. Existing constructor remains as a compatibility overload; fake clock is passed through the new overload.

**Interfaces:** Implements section 2.4. Register the same ICombatAdvanceService once in the existing Core builder; export and consumer route remains CoreGroup.CombatAdvance. Add ISystemShutdown and an ISystemTick cleanup-only role to this same service, registered through its existing module. Tick must not run AI preparation or consume frame work.

- [x] Add `OneFrameSpendsAtMostOneSlice`, `ReadyIsVisibleOnlyNextFrame`, `PrepareAfterDeadlineYields`, `AtomicOverrunIsRecorded`, `FrameDeferredDoesNotDiscardSession`, `StaleDoesNotDrain`, `RetryWaitsAnotherFrame`, `UnchangedSourceAssembledOnlyOnceDuringPreparation`, `FreshInputCheckedBeforeExecute` and `SynchronousDrainMatchesPreparedExecution` with a fake clock. A clock jump controls yielding, not intent selection.
- [x] Implement source capture and session creation as the first measured preparation work. Continue Step while time remains; check elapsed time after each atomic step. Stamp work includes all preparation overhead. A status getter does not do heavy work. Starting-controller work bypasses AI; captured pending roar causes NotRequired on a subsequent eligible frame.
- [x] Implement prepared execution outcomes exactly as section 2.4. Calculate only the selected action, revalidate source immediately before publication, publish once, then dispose. A hidden rejection returns RetryPreparation without publishing a rejected intermediate result or reading hidden cause into scores.
- [x] For elapsed-time math use Stopwatch ticks/frequency without rounding the entire budget to zero; reject nonpositive frequency. Do not use Time.deltaTime/timeScale. Record a completed atomic overrun even when it exhausts the budget.
- [x] Implement idempotent CancelPreparation/Shutdown. Cleanup Tick drops retained work when battle/run/phase/identity/source becomes invalid; it does not select actions or change combat state. Flow cancellation in Task 3.3 releases immediately on its higher-priority exit routes; cleanup Tick covers external completion/replacement even if no next enemy turn occurs. The service's last-spent frame stamp survives session disposal/replacement so a same-frame restart cannot obtain another budget.
- [x] Add `ExternalTerminalOrReplacementReleasesOnNextCleanupTick`, `ShutdownReleases`, `RepeatedCancelHasNoSideEffects`, `CancelAndRestartCannotSpendSecondFrameBudget`, `StaleRejectedIdentityDoesNotLeakToNewBattle`, and `NoNewServiceOrBufferRegistration`. Verify factory -> local registration -> CoreGroup export -> existing consumer lookup, not only duplicate-interface counts.
- [x] Verify the production factory selects the existing immediate path by default and that only explicit test or diagnostic composition selects preparation. Verify both paths use the same service registration and publication contract.
- [x] Run RF(EnemyAdvancePreparationTests), RI/RC, and the frame-yielding Core-service performance harness. Record preparation CPU, constructor/source overhead, latency and final synchronous execution separately.

**Done when:** the prepared service performs a single slice or one supplied intent, with cancellation and freshness verified. A hidden full drain or incorrect freshness is a correctness blocker. Unresolved performance overruns remain activation blockers. Record those overruns and use owner-local remedies.

### Task 3.3: Add one-shot preparation and prepared execution to CombatPlan

**Files:** Create `F/Jobs/CombatPrepareAdvanceJob.cs`, `CombatPreparedAdvanceJob.cs`; modify `F/CombatProgressReader.cs`, `CombatProgressFacts.cs`, `F/Plans/CombatPlan.cs`, `CombatStep.cs`. Read `CombatStepContext.cs`; its existing Advance dependency should suffice. No new View/Flow test files.

**Interfaces:** Add `AdvancePreparationState Preparation` to CombatProgressFacts. Reader obtains it from the already-resolved CoreGroup.CombatAdvance while continuing to read actual world facts from Buffer. Add StepKind PrepareAdvance and PreparedAdvance; keep old enum values stable. Both new Jobs consume existing ICombatAdvanceService and ICombatProgressReader; prepared Job also receives base result sequence.

- [x] Update the manual acceptance sheet section 12 with current user-run results left Pending. For existing test doubles broken by an interface/constructor addition, make compile-only signature adaptations where permitted; record authorship before runtime assertion edits.
- [x] Preserve Decide's completion/abandon/input/presentation priorities. Only in ordinary Automatic progress: NotRequired -> existing Advance; Required/Preparing -> PrepareAdvance; Ready -> PreparedAdvance; FrameDeferred -> Nothing. Invalid enum states throw rather than silently stall.
- [x] Prepare Job checks no result sequence changed and completes after one slice. It accepts Preparing/Ready/FrameDeferred/NotRequired as ordinary preparation outcomes. Do not require publication or leave IsCompleted false.
- [x] Prepared Job rechecks FrameDeferred before attempting execution. Published requires exactly base sequence+1, matching latest result kind Advance and Accepted=true. Stale/RetryPreparation/Cancelled require no new result and complete normally; any inconsistent publication faults. It must not call fallback itself.
- [x] In BuildStep, call CancelPreparation before the chosen completion/abandon/leave cleanup step when a higher-priority route supersedes planning. Retain existing action/presentation ordering. Direct external lifecycle changes are covered by service cleanup, not a new UI event bus.
- [x] Keep CombatNextJob's one-shot child guard and JobRunner's shared implementation unchanged. No long-running child Job, no recursive Next registration, no wait loop.
- [ ] Run permitted RC/RI and RD; request user Unity compilation and manual F1–F8. Record compilation/playback unverified until confirmed. Do not introduce tests in another assembly to bypass the no-View-test rule.

**Phase 3 development exit.** In test or diagnostic composition, verify the same actions, sliced work, service validity, cancellation and user Flow checks. Record gameplay activation readiness separately under the gate above. Pause for review before changing Utility preferences.

## 9. Phase 4 — assessment coverage and scheduled deterministic hazards

### Task 4.1: Expose observable action consequences without a second effect engine

**Files:** Create `C/Contracts/CombatEffects/CombatAssessmentCoverage.cs`, `A/CombatActionAssessment.cs`, `AI/EnemyUtilityScorer.cs` (score-fact type only in this Task), `T/EnemyActionAssessmentTests.cs`. Modify `C/Domain/CombatEffects/Rules/CombatEffectCapabilities.cs`, `A/CombatCommandEffects.cs`, `CombatAttackQueries.cs`, `CombatMovementCalculator.cs` only for shared assessment access. Initial owner files eligible for coverage annotations: `C/Domain/Traits/Effects/ArmorBreakerEffectRule.cs`, `DistanceDamageScaleEffectRule.cs`, `TauntEffectRule.cs`, `SteadfastWillEffectRule.cs`, `HunterEffectRule.cs`, `KillChainAttackEffectRule.cs`, and `C/Domain/Categories/Rules/SafeBombardmentCategoryRule.cs`. Read related attack/entry/reaction pipelines; do not rewrite their mechanics.

**Interfaces:** Produces section 2.3 coverage and assessment methods. Capability annotations are internal rule contracts. Annotation None is allowed only after the relevant operation's deterministic preview/live parity passes. Unannotated rules remain UnsupportedEffect; do not require dozens of unrelated rule edits just to start this AI.

**Verified required owner additions under standing user authorization:** CombatCommandComposition.ProjectSteps (same projection as the synchronous adapter), CombatCommandEffects.CaptureSteps, MovementProjection arrival/completion adapter, ReconSquadApplication.ScanSteps, MovementEnvironmentCalculator and TornadoCalculator coverage, EnemyDecisionContext logical allowances, and RoarEffectRule's actual-only declaration. Add EnemyAssessmentMeasurements for the explicit Core measurement. These remain Core-owned; no View/Framework/Job foundation changes are part of Phase 4.

This later task also adds `internal static RouteWork BeginProjectRoute(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules, bool preview, CombatProfileReadSet profileReads = null)` to CombatMovementCalculator. Nested RouteWork has Step/IsComplete/WorkVersion/Dispose with section 2.5 lifecycle semantics and completion-only `MovementProjection Projection`, `MovementFailureReason Failure`, `int ValidStepCount`. It resumes the existing route path and reuses Phase 2 entry/observation children. Existing ProjectRoute drains it and preserves its signature/out values. Assessment uses preview=true, then shared completion/snapshot work; it never asks the Projection getter to finish work. No new top-level movement service or public API. This is a Phase 4 dependency update, not Phase 2 implementation scope.

- [x] Create an assessment-support table in `E/phase-4-assessment.md` with exact rule/entry-hook names, full/partial support and reason. Include attacker's and defender's effects, categories, held environments, items/artifacts, movement arrival and pending reactions. Document existing omissions such as kill chain and boss roar. This table is evidence, not a second runtime registry.
- [x] Add `UnknownRuleIsPartial`, `RandomGapPropagatesToEndpointAndKill`, `ActualOnlyReactionPreventsGuaranteedTerminal`, `DifferentUnobservedSeedsGiveSameAssessment`, and `AssessmentDoesNotConsumeLiveRandomOrPublishRewards`.
- [x] Before any full preview, derive conservative coverage from all reachable relevant rule bindings/hooks. Hunter/random landing receives RandomOutcome; kill-chain/roar omissions receive ActualOnlyReaction; unsupported callbacks stay UnsupportedEffect. If a downstream random callback cannot be excluded, skip full consequence projection for scoring rather than reading RNG and masking its output afterward.
- [x] Reuse existing installed/effective stats and narrow target queries for cheaper partial estimates. Estimates can be inaccurate and must carry coverage; hard admission cannot be guessed. Do not copy damage formulas or fake defense consumption into AI. A completely unassessed optional effect gives zero speculative benefit and conservative possible harm.
- [x] Use BeginPreview only after incremental coverage admits supported observable deterministic consequences. Use BeginProjectRoute(preview:true) for supported moves; keep route-entry/completion/score extraction resumable. GetCoverage, TryAssess and ProjectRoute are synchronous adapters for direct callers, not scheduled work. Release the temporary projection after score extraction and at most one same-actor follow-up; never use full Calculate to score a move.
- [x] Add `AssessmentStepNeverDrainsAttackOrMove`, `CoverageScanYieldsBeforePreciseCreation`, `LogicalBudgetChargedOnceAfterCoverage`, `MoveRoutePartitionsMatchPreview`, and `MoveCompletionAndFactExtractionAreBounded`. Unsupported/random coverage must avoid creating a precise child, not merely suppress its output afterward.
- [x] Add `MoveAssessmentHidesUnknownMine`, `KnownMinePolicyUnchanged`, `AttackHealthLossIsActualCappedLoss`, `HealingIsNotDamageCredit`, `TraitActivationDoesNotDoubleCountDamage`, `NoDamageContactStillAdmitted`, and `DeterministicMovementEndpointUsesActualProjectedArrival`. Check meaningful consequences, not incidental list allocations.
- [x] Add the supported rule annotations only after their tests pass; preserve partial fallback for everything else. If supporting a rule requires changing live mechanics, record a scope issue instead.
- [x] Run RF(EnemyActionAssessmentTests), RC/RI, relevant existing reflection/trait/targeting/movement tests and PF largest preview atoms. Report percentage of candidates receiving precise versus partial assessment so low cost cannot hide that every candidate became Unknown.

**Done when:** Complete/partial meaning is honest, transitive and seed-safe; mandatory restrictions still work with unsupported value assessment. **Locality:** a rule owner describes observable support; the scorer consumes facts and does not understand the rule's internals.

### Task 4.2: Share the hazard clock, railway and rockfall facts

**Files:** Create `G/StageHazardForecast.cs`, `T/EnemyHazardForecastTests.cs`; modify `G/IStageGimmickRule.cs`, `StageGimmickComposition.cs`, `RailwayGimmickRule.cs`, `RockfallGimmickRule.cs`, `HaboobGimmickRule.cs`, `TornadoGimmickRule.cs`, and `A/CombatTurnCalculator.cs` for a shared night-end predicate. Make only the required compile adaptation to `T/StageGimmickProgressionTests.cs`'s ObservingRule implementation of the extended interface; leave existing assertions unchanged. The haboob/tornado rules initially produce explicit partial facts until Task 5.2; do not represent them as absent/safe.

**Interfaces:** Section 2.3 forecast builder/facts. Add `RailwayGimmickRule.IsDue(RailwayGimmickSpec spec, long completedCount)`, `RockfallGimmickRule.HealthLoss(int maxHealth, RockfallGimmickSpec spec)`, `RockfallGimmickRule.BindEndCount(Owner victimOwner, Owner endedSide, RockfallGimmickSpec spec)`, all internal static, called by both existing execution and assessment. Add `CombatTurnCalculator.EndsNightAfterEnemyPhase(CombatTurnFacts turn)` shared by live end and forecast horizon.

- [x] Add `RailwayScheduleMatchesExecution`: first=5, repeat=2 gives due at 5/7 and not 4/6; argument validation stays with existing spec. `IndividualActionsDoNotAdvanceBoundary` asserts forecast count unchanged after a move/attack within the same side.
- [x] Add `RailCrossingBeforeBoundaryIsNotInstantDeath`, `RailEndpointDueAtNextEndIsLethal`, `DueAtFollowingPlayerEndStillPrecedesNextEnemyAction`, and `LastNightStopsAfterImmediateHazardBeforeRecall`. Evaluate Hold through the same endpoint logic.
- [x] Extract the shared arithmetic exactly: floor(maxHP * percent /100), BindAffectedSideTurns plus one when victim owner equals ended side. Test maxHP=125, percent=25 ->31; bind=2 ->3 at own-side end and 2 at other-side end. Keep overflow checks and zero-damage Bind behavior.
- [x] Build current known forecast only from registered definitions/states. Warning rockfall yields known impact cells/time; Cooldown never draws a random warning location. Add `UnknownFutureWarningDoesNotRevealRandomCells` and `WarningStateAndSeedUnchanged`.
- [x] Registry enumeration order remains Tornado -> Railway -> Haboob -> Rockfall from one source. Unknown-region facts use `Cell=null`, explicit FutureWarning/UnsupportedEffect and no fabricated coordinate; known facts use valid board cells. One Builder.Step visits one finite rule/cell item, not an entire region.
- [x] Normal enemy horizon covers completedCount+1 and +2; final-night response covers +1 before recall. If action restrictions prevent a next ordinary opportunity and no shared exact query supports the extension, flag UnsupportedEffect/FutureWarning for the uncovered later exposure; do not claim safety at +2 or iterate unbounded future turns. No fake phase transitions or EndEnemyPhase calls per candidate.
- [x] Add `ForecastAndLiveRailRockfallAgreeForKnownFixedOccupancy`, `NoDuplicateClockIncrement`, `HazardLossClampedAfterDeath`, `TerminalStopsLaterKnownConsequences`, and `ResumePartitionsProduceSameFacts`. Use existing gimmick tests as live-semantic references, not new copied mechanics.
- [x] Run RF(EnemyHazardForecastTests), RF(RailwayGimmickTests), RF(RockfallGimmickTests), RF(StageGimmickProgressionTests), RC/RI and PF R8 forecast/lookup costs. Keep forecast dormant in the legacy scorer until Phase 6.

**Phase 4 exit:** Timing/health/Bind source is shared; partial support is explicit; no live hazard behavior changes. Report verified versus conservative assessment coverage separately.

Final-review corrections: forecast evaluates fixed-occupancy Main/objective mortality with the same exposure helper and the existing BattleOutcomeCalculator.Resolve contract, cutting off only after all direct consequences of the terminating gimmick/boundary group. It does not manufacture live transitions. A predicted Bind combined with pre-existing attack restriction marks unmodelled longer exposure; final-night recall does not invent another action opportunity. Exact evidence is in the Phase 4 report.

## 10. Phase 5 — bounded movement/threat candidates and remaining hazards

**Verified checkpoint.** The [Phase 5 report](../reports/2026-11-07-enemy-utility-ai/phase-5-candidates-and-hazards.md) records the bounded Core implementation and raw evidence. Test labels below describe planned cases. The focused run verifies equivalent parameterized cases and the separate refinement-cap regression. No Utility gameplay activation was performed.

### Task 5.1: Enumerate attack/position candidates without a move-by-aim product

**Files:** Create `AI/EnemyActionCandidates.cs`, `AI/EnemyThreatField.cs`, `T/EnemyActionCandidateTests.cs`, `T/EnemyThreatFieldTests.cs`; modify `AI/EnemyDecisionContext.cs`, `C/Contracts/Combat/EnemyAi/EnemyDecisionSettings.cs`, and narrow shared traversal hooks in `A/CombatMovementCalculator.cs` / `C/Domain/Movement/MovementRouteValidator.cs` only if necessary. Phase 5 reused the settings and movement contracts as installed. It added narrow shared selection/range helpers in `A/CombatAttackQueries.cs` and `C/Domain/Movement/EnvironmentTerrainRules.cs`. Reuse Task 2.2 pursuit and attack query cursor. Do not change original pursuit goal semantics.

**Interfaces:** Internal `EnemyActionCandidates(EnemyDecisionContext context, CharacterCombatFacts actor, StageHazardForecast hazards)` with `void Step()`, `bool IsComplete`, `IReadOnlyList<EnemyDecision> Attacks` and `Moves` after completion; lists contain at most 4 each. Internal `EnemyThreatField.Builder` has Step/IsComplete/Build; finished `Evaluate(CharacterCombatFacts actor, CellCoord endpoint)` returns estimated loss permille with FuturePlayerChoice gap. Candidate generation owns route reconstruction; scorer never invents a path.

- [x] Add `ShortlistNeverExceedsFour`, `EscapeCanMoveAwayFromTarget`, `TauntAndEscapeReservationsFitInsideCap`, `SingleRouteReconstructionPerRetainedDestination`, `ReachedCellsUseSameAdmissionAsLiveMove`, and `FarPursuitUsesExistingOptimizedSearch`.
- [x] Incrementally enumerate legal target-directed aims for hostile units and hostile headquarters. Include both kinds for Utility comparison; do not add an unrelated destroy-obstacles planning feature. Preserve full aim/delivery identity; equal target sets alone do not justify merging.
- [x] Perform one actor reach traversal within its installed movement allowance. Retain predecessors and hazard exposure. Use the existing traversal, landing, allowance and Leap rules; origin is the Hold option, not a zero-length Move candidate. Scan traversed cells for entry exposure; boundary-only railway risk applies at endpoint, not every rail crossing.
- [x] Rank cheap destinations by lower known terminal risk, lower possible HP loss upper bound, lower known loss and Bind exposure, then entry exposure, player threat, ability to establish a permitted attack, useful approach progress and route length/Y/X. Select at most four, reserving the best known hazard-reducing route and required taunt-compliant route inside that count; if both reserve the same route, one slot is used. Unknown taunt reachability cannot remove the constraint.
- [x] Add optional safer-route refinement for the selected escape destination only: `AlternateRouteNodeLimit=256` in immutable settings. Search labels include cell and used steps; dominance must preserve longer-but-safer legal prefixes. Order by known lethal contacts, estimated entry loss, used steps, stable cell/neighbor order. Stop after 256 expansions with explicit truncated coverage; retain the best admitted route found, never claim no safe path exists from truncation. Actual movement step cost remains 1.
- [x] Add `SaferLongerRouteWithinAllowanceBeatsLethalShortestRoute`, `RefinementCapCannotMeanUnreachable`, `LeapMayCrossButNotLandOnKnownMine`, and `MovedActorHasNoSecondMoveCandidate`. Counter assertions distinguish full mandatory reach from optional alternate search.
- [x] Build the player-threat field once per source using shared attack geometry/occlusion and the information visible to the enemy. Do not call full attack Preview per cell or simulate player movement/replies. Store at most the maximum plausible current-position attack exposure per cell rather than summing every mutually exclusive attack; mark FuturePlayerChoice, and use UnsupportedEffect where consequences cannot be estimated. Ignore the current player's spent action-use when describing the next turn's geometric possibility, while retaining intrinsic attack restrictions.
- [x] Add `ThreatFieldReusesGeometry`, `NoFullPreviewInThreatConstruction`, `HiddenEnemyUnknownEnvironmentDoesNotLeak`, `WallBlocksKnownThreat`, and `PotentialThreatIsNotCertainDamage`. Note movement-enabled future player attacks are not exhaustively modeled.
- [x] Run RF(EnemyActionCandidateTests), RF(EnemyThreatFieldTests), RC/RI and PF W8/U8/A8. Prove there is no full-preview count proportional to all reached cells multiplied by all aims. New candidate path is still dormant in legacy selection.

**Locality:** movement legality stays with movement rules, pursuit retains its objective, and tactical ranking stays in AI. Shared traversal changes require movement regression; threat approximations do not require reading every trait implementation.

### Task 5.2: Forecast tornado/haboob and propagate combined uncertainty

**Files:** Modify `G/StageHazardForecast.cs`, `TornadoGimmickRule.cs`, `HaboobGimmickRule.cs`, `HaboobTargetingRules.cs` only where shared queries need access; read `C/Contracts/Combat/TornadoGeometry.cs`, `A/TornadoCalculator.cs`, `UnitBehaviorRules.cs`; create `T/EnemyCombinedHazardAssessmentTests.cs`. Phase 5 also shared Tornado geometry, preparation and timing from their existing owners, and updated the Phase 4 forecast placeholder expectation to cover the concrete facts.

**Interfaces:** Completes the existing EnumerateForecast implementations; no second registry/order table. Actor immunity is evaluated by UnitBehaviorRules.IsForcedMovementImmune, not a TraitKey name check. Geometry reuses TornadoGeometry rather than new sweep arithmetic.

- [x] Add `WarnedSweepIncludesIntermediateCenters`, `CurrentTornadoEntryIsRiskEvenBeforeNextSweep`, `ImmuneActorDoesNotReceiveThrowRisk`, `RandomLandingIsNotKnownSafe`, `CooldownDoesNotPeekNextDirection`, and `NoSourceRandomMutation`.
- [x] Emit incremental cells for current tornado contact and locked-direction warned travel. A possible throw yields UnknownPosition|RandomOutcome; do not call GroupedLandingCandidates and simulate every landing. If immunity rules remove displacement, preserve known position for later hazards.
- [x] Combine rules in the shared live order. After unknown displacement, later railway/rockfall contact is possible, not a certified miss at the old cell. Keep damage intervals capped by remaining life, and do not add two guaranteed death penalties for the same life. Support deterministic action displacement already proven by assessment at its resulting endpoint.
- [x] Haboob facts carry region/next boundary and targeting relevance, not an invented damage value. Before relocation, call the same CanDesignate logic as attacks. Unknown relocation produces FutureWarning; no RNG-derived region. If the same known region remains by rule because no alternative exists, use the shared rule's established condition rather than assuming relocation always changes cells.
- [x] Add `TornadoBeforeRailMakesRailContactUncertain`, `DeterministicEndpointThenRailUsesNewCell`, `HaboobCrossBoundaryDesignationMatchesLiveQuery`, `TwoHazardsCannotOvercountLife`, `KnownTerminalStopsForecast`, and `UncertainKillDoesNotSuppressRemainingHazards`.
- [x] Run RF(EnemyCombinedHazardAssessmentTests), existing TornadoGimmickPreparation/Resolution/Evidence, HaboobGimmick and StageGimmickProgression tests, RC/RI and PF X8/H8. Log coverage limitations rather than misreporting complete simulation.

**Phase 5 exit:** All four gimmicks are represented. Known and possible consequences stay distinct. Candidate counts and sparse threat storage are bounded, with exact allocation bytes unavailable. Utility gameplay remains inactive. G-atom and user Unity F1–F8 stay open.

## 11. Phase 6 — activate bounded Utility choice

**Development checkpoint.** The [Phase 6 report](../reports/2026-11-07-enemy-utility-ai/phase-6-utility-choice.md) records the implementation, reviewed behavior and exact verification. The explicit diagnostic and prepared paths share the Utility session. The existing immediate gameplay route is retained until the activation gate is satisfied.

### Task 6.1: Implement the shared Hold-relative scorer

**Files:** Complete `AI/EnemyUtilityScorer.cs`, including section 2.2's named preference constants; create `T/EnemyUtilityScoreTests.cs`. Candidate settings were already defined in Task 2.1. No combat formula changes.

**Interfaces:** Internal `long Score(in EnemyScoreFacts facts)`; internal `bool BeatsHold(in EnemyScoreFacts candidate, in EnemyScoreFacts hold)`; mandatory-rule eligibility is a caller precondition, not a soft score feature. Terminal tier is evaluated only when coverage certifies it.

- [x] Write exact numeric tests from section 2.2: 250 opposing-loss permille -> +250; one elimination adds 2,000; 250 allied-loss permille -> -750; 1,000 actor hazard permille -> -4,000; +3 useful approach steps ->+60; 200 subsequent-attack benefit ->+100; favorable 500 restriction permille ->+500.
- [x] Add `LessRiskyEscapeBeatsRiskyHold`: Hold hazard=1,000, move hazard=250 and no other terms -> -1,000 > -4,000. Add `SafeProgressBeatsEndlessHold`: progress=1/no extra harm ->+20 >0. Add `ZeroGainDoesNotMove`, `OverkillCannotInflateDamage`, `DamageAndTraitEventAreNotDoubleCredit`, and checked-overflow failure cases.
- [x] Add `ConfirmedPlayerDefeatIsEnemySuccess`, `PlayerVictoryIsEnemyFailure`, and `IncompletePreviewCannotClaimTerminalTier`. Do not reverse outcome direction by treating Victory as the enemy's victory.
- [x] Implement deterministic integer weights and ties exactly. Restriction deltas use supported shared capability queries; never simulate guessed status expiry to obtain an extra bonus. Preserve explicit gaps when using coarse features.
- [ ] Run RF(EnemyUtilityScoreTests), RC and score-only allocation checks. RF and RC executed. Exact allocation remains unavailable after the Mono calibration failed. Weights may be tuned only by an approved evidence-backed update with amended numeric tests; implementations must not choose their own constants.

### Task 6.2: Replace legacy preference stages and enforce all work limits

**Files:** Modify `AI/EnemyDecisionSession.cs`, `EnemyAiPolicy.cs`, `EnemyAiTurnService.cs`, `EnemyDecisionContext.cs`, `EnemyDecisionDiagnostics.cs`, `A/CombatTurnCalculator.cs` comment about end-phase admission; create `T/EnemyUtilityDecisionTests.cs`, `T/EnemyDecisionBudgetTests.cs`, `TI/EnemyUtilityPublicationTests.cs`. Existing policy-dependent tests are changed only with the authorship/permission process recorded in Phase 0.

**Interfaces:** Final session stages: Initialize -> Forecast/Threat -> Actor -> MandatoryAdmission -> CheapCandidates -> RefineAttacks/RefineMoves -> CompareWithHold -> Ready/NextActor. Both synchronous and prepared callers use this one path. No permanent Legacy/Utility mode switch or duplicate runtime policy.

- [x] Add scenario assertions: attack available on due rail chooses an admitted escape when escape's score is higher; moving through rail to safe endpoint is allowed; a confirmed immediate enemy win may outweigh sacrifice; moved actor never moves again; mandatory taunt remains enforced even when another action scores higher; lower EntityId selects first preferable action.
- [x] Wire shared forecast/threat preparation once per source, then actor-specific candidate enumeration. Hold includes the same hazard/threat horizon. When an actor chooses Hold, move to the next actor without a combat event, action-use mutation or counter reset. If all actors Hold/no legal action, select EndEnemyPhase and publish it exactly once.
- [x] Refine at most four current attacks and two move routes. For each refined deterministic move, choose only one follow-up aim via cheap geometry and preview it at most once; stochastic/unsupported moved positions cannot produce a precise follow-up claim. All these calls share 8-per-actor/16-per-decision counters. Reserving escape/taunt candidates must not add extra slots/calls.
- [x] Before starting each logical precise child check coverage and remaining budget; charge once at creation. Drive its assessment Work incrementally without calling synchronous TryAssess/GetCoverage/Preview/ProjectRoute. If no budget remains, retain coarse admitted scores with gaps. Mandatory admission continues until it can choose/skip an actor correctly; budget exhaustion alone never means EndEnemyPhase. Unavailable precise evaluation is not an exception and not fictitious zero risk.
- [x] Preserve Task 2.3 rejection progression and remaining counters. Actual rejected attempts are finite because each removes a permitted candidate/action-family step; no rejected attempt publishes, resets the decision or loops on the same low-ID actor. Actual accepted movement changes source and starts a fresh later decision, permitting its attack normally.
- [x] Add `PreviewCapIsEightPerActorAndSixteenPerDecision`, `FollowupConsumesSameBudget`, `FailedPreviewCounts`, `RejectedAttemptDoesNotResetBudget`, `MandatoryTauntScanSurvivesOptionalExhaustion`, `AllHoldEndsOnce`, `HoldDoesNotWriteActionUse`, `ReplanAfterActualMoveUsesNewFacts`, and `AllSliceSchedulesChooseSameIntent` for work partitions 1/2/31 and fake frame budgets.
- [x] Add integration cases asserting one actual publication, no speculative inventory/soul/reward/RNG changes, untouched pending View command, synchronous/prepared same intent/outcome, and cancellation/staleness after the winning candidate is selected.
- [ ] Remove legacy lexicographic scoring and attack-first runtime branching only after the new preference tests pass. The default route still requires the immediate compatibility selector while activation is gated. Preserve reusable taunt/aim query logic and all existing execution/calculator contracts. Keep original pursuit equivalence tests meaningful; do not delete them because overall AI behavior changed.
- [ ] Run RC/RI, PF full matrix and user UF F1–F8. RC/RI and Core measurements are recorded. User Unity compilation/playback and F1–F8 remain Pending. Classify each changed old expectation as approved preference change or unintended regression. Never update all failed expected values to current output.

**Phase 6 development exit:** One Utility engine is verified through explicit diagnostic and prepared execution. Caps, seed/slice determinism, unchanged immediate behavior and diagnostic preference differences are recorded. V4's complete terminal assumption is verified with certified score facts because the public input always retains HiddenEnvironment. Gameplay activation, G-atom, allocation and user Unity acceptance remain open. Pause before Phase 7.

### Fixed behavioral vectors for Tasks 4.2, 5.1 and 6.2

These are small acceptance fixtures in addition to the benchmark manifests. Reuse existing test-owned builders to make valid command/battle definitions; they do not set production balance. Common defaults: 5x5 board, one-cell units, actor enemy ID=1 at (1,1), player main ID=100 at (4,4), all HP/maxHP=100, physical defense=0, ordinary single-cell orthogonal attack range=1, attack power=10, movement allowance=2, no traits/artifacts/entry effects, no extra reactions, Automatic/Enemy, Available. Use stable valid test keys/profiles. Keep a living enemy objective so a nonterminal test cannot accidentally end at setup. A test changes only the fields stated below; manifest helper must record these overrides.

| Vector | Override | Required assertion |
|---|---|---|
| V1 escape before attack lock | Additional non-main player ID101 at (2,1); rails cover all (1,y); completed count=4, first=5, repeat=2 | Select Move to a non-rail endpoint; no requirement to pick a particular equally safe cell; actual movement is legal and attack has not been consumed |
| V2 crossing | Prescribed legal path (0,0)->(1,0)->(2,0), actor starts (0,0), same rails/due boundary | Forecast contains no railway death for endpoint (2,0); the intermediate rail does not become an entry-trigger death |
| V3 later boundary | Same rail endpoint, count=4, first=6, repeat=2, not last night | Exposure before next enemy opportunity includes railway death at count=6 even though count=5 is safe |
| V4 immediate enemy success | Main moved to (2,1), HP=10, no other player target; actor remains on V1 rail; complete deterministic assessment | Select the main-killing attack; actual player Defeat is resolved before a later turn-end rail event |
| V5 moved actor | V1 actor ActionUse=Moved | No second Move candidate; permitted attack/Hold decision follows rules without inventing escape movement |
| V6 useful approach | Actor (0,0), additional non-main target ID101 at (4,0), main (4,4), no hazards | Some legal approach Move beats Hold; subsequent redecision uses the actual moved position |
| V7 less dangerous endpoint | Score-only facts: Hold known hazard=1,000 permille; admitted Move hazard=250; every other term=0 | Move score -1,000 beats Hold -4,000 |
| V8 known warning | Rockfall Warning at (1,1), impact next boundary; actor maxHP=125, HP=125; loss percent=25, BindAffectedSideTurns=2 | Known loss=31, own-side Bind end count=3; forecast arithmetic equals live rule |
| V9 no useful action | All actors have no legal target and cannot make useful movement; Hold/action score tie=0 | EndEnemyPhase once; no movement event, no actor deferred flag or intermediate publication |

Do not enforce a chosen cell where the contract only requires a class of legal safe destinations; separate deterministic tie tests pin Y/X/direction/full-route ordering. Taunt/immunity/random contact cases reuse their existing richer test fixtures so statuses and definitions remain valid rather than hand-creating inconsistent trait state.

## 12. Phase 7 — acceptance, locality review and handoff

**Acceptance checkpoint.** The [Phase 7 report](../reports/2026-11-07-enemy-utility-ai/phase-7-acceptance.md) and [final handoff](../handoffs/2026-11-07-enemy-utility-ai-hazard-forecast-handoff.md) record the completed diagnostic work. Gameplay acceptance is unmet. Phase 7 stops before production remediation.

### Task 7.1: Run the final measurement/behavior matrix

**Files:** No planned product changes. Update `E/phase-7-acceptance.md`, manifests/raw evidence, and Phase ledger. If measured optimization is needed, identify exact owning files and seek approval for that bounded remediation before proceeding; do not combine unrelated refactors with acceptance.

**Interfaces:** Consumes Tasks 1.1/1.2 fixtures/counters, final session/service, and user-run real-scene capture. Produces measured pass/fail tables for every gate, including differences from baseline and measurement uncertainty.

- [x] Re-run the identical frozen 1,100-decision corpus (original 1,000 plus N5's 100) and full-side sequences. Use separate data for cold starts, steady-state, Core microbenchmarks, service frame harness and actual Unity scene capture.
- [x] Report per fixture and aggregate CPU, p95/p99/max frame preparation, largest atom, allocated bytes, total work, decision latency, complete side latency, rejection/invalidations, candidate coverage and cap-hit rates. Include zero-action/unreachable cases and 16-enemy stress; do not hide failures in a combined average.
- [ ] Ensure per-frame totals include all AI-associated source validation, construction, cleanup, candidate work and preparation Jobs, even when multiple cheap status reads occur. Keep final action/reaction/gimmick execution and publication separately visible. Yielded service totals were measured with separate execution time. Actual Flow Job and rendered frame totals remain NotRun.
- [ ] Compare choices and outcome traces across slice schedules and public-identical/random-cursor variants. Behavioral improvements must include known hazard survival, no illegal actions, useful engagement, and no endless Hold; Slice and public RNG parity passed. A8/X8/H8 failed behavioral acceptance because the broad allied-loss bound suppresses useful candidates.
- [x] Run RC/RI; compare failure identities to baseline and every Phase report. Existing unrelated failures remain reported. Gate-critical new tests cannot be waived as unrelated.
- [ ] Obtain user UF checks and real-scene before/after captures. Report compilation/playback unverified if missing; do not mark complete from Core measurements alone.

**Stop if:** any correctness/isolation gate fails; any >4 ms preparation spike is unexplained; a >1 ms atom remains unexamined; missing raw evidence makes percentiles unverifiable; latency/alloc targets were only met by changing the workload. Report a scoped remediation proposal rather than claiming completion.

### Task 7.2: Verify feature-local boundaries and publish the handoff document

**Files:** Create `docs/superpowers/handoffs/2026-11-07-enemy-utility-ai-hazard-forecast-handoff.md`; update design/plan status and `E/phase-7-acceptance.md` with actual results. No external sending, commit or push.

**Interfaces:** Handoff states approved behavior, public service methods, source ownership, candidate/assessment limitations, measured platform/configuration, and remaining user verification.

- [x] Perform the three representative locality reviews below using final source paths/methods. Distinguish actual evidence from intended structure. Fixing an unrelated issue requires a separate request.
- [x] Validate Factory creation -> local registration -> CoreGroup export -> Reader/Job consumption. No new root service, Buffer mirror, event bus or generic planner may have appeared during implementation.
- [x] Verify all new `.cs` and directories have `.meta`, existing GUIDs remain, no runtime Core Unity references, comments are Korean, and no UI/scene/FMOD/content changes are included. Preserve unrelated user edits rather than automatically restoring them.
- [x] Record final counters/settings, exact test totals/failure lists, manual checks, performance gates and coverage gaps. Keep raw evidence locators. State any remaining risk without describing unmeasured targets as achieved.
- [x] Update the Phase ledger and handoff only to the verified status. Stop for user review; commits/pushes remain user-owned.

### Manual Unity acceptance sheet (UF)

These are user-owned checks, not instructions to write new View tests. Use test-owned/dev setup where available without editing production scenes/assets as part of this feature. Record configuration and observable evidence for each row.

| ID | Setup / action | Required observable result |
|---|---|---|
| F1 | Standard five-enemy turn with multi-frame thinking, plus larger stress encounters | UI/rendering continues; only one actual action/result at a time; next action waits for presentation |
| F2 | Repeated scheduling/status reads in the same rendered frame | Counters show at most one preparation budget; newly ready action executes on a later frame; no recursive drain |
| F3 | Source changes or battle ends while preparation is pending | Old intent never publishes; pending memory is released; fresh decision uses current source |
| F4 | Publicly valid attack rejected by hidden environment | No rejected intermediate playback; bounded later-frame pursuit/skip fallback; no repeating stall |
| F5 | Abandon requested while thinking or after Ready | Existing abandon precedence wins; no enemy attack slips in; normal Run return policy remains |
| F6 | Pending boss roar or terminal action during automatic progression | Required reaction/terminal cleanup order preserved; no AI action after completed battle |
| F7 | Last night enemy response, railway/tornado/rockfall due | Hazards settle before recall, in existing order; terminal outcome suppresses inappropriate subsequent cleanup |
| F8 | All actors Hold / no useful legal action | One enemy-side end, normal following phase, no indefinite thinking or repeated no-op results |

### Locality review sheet

| Representative change | Necessary shared knowledge | Expected final modification scope | Verification that cannot be omitted |
|---|---|---|---|
| Adjust survival/aggression weights | Score facts, units, Hold baseline, hard-rule precedence | EnemyUtilityScorer/settings + preference cases; no gimmick or publisher internals | Utility behavioral cases, cap counts and a matched performance sample |
| Change railway interval or rockfall loss/Bind | Existing spec/state and shared clock/severity query | Feature rule/spec/shared query + forecast/live parity; registration only if dependency changes | Gimmick execution and forecast parity, ordering/terminal and publication integration |
| Change frame budget / stale-input dependency | Session Step contract, complete assembler input, frame/Job lifecycle | Advance service/stamp/assembler + diagnostics; no attack damage or trait policy rewrite | Slice determinism, stale/cancel/publication, user Flow checks and full frame-time evidence |

If a feature change still requires reading unrelated private implementations, explain the concrete leak and smallest correction. More classes/files are not proof of improvement. Shared clocks, mutation boundaries and legitimate Factory changes remain necessary common knowledge.

## 13. Continuation ledger and per-Phase report format

| Phase | Status | Evidence / user review |
|---|---|---|
| 0 | Evidence recorded; review stop | [Baseline report](../reports/2026-11-07-enemy-utility-ai/phase-0-baseline.md); source/test/current-scene evidence, with stated measurement limitations |
| 1 | Measurement work recorded; exit gate open | [Measurement report](../reports/2026-11-07-enemy-utility-ai/phase-1-measurement.md); G-atom failed, approved atom strategy outstanding, precise allocation unavailable |
| 2 | Core decision and pursuit implemented; G-atom unmet | [Current Phase 2 atom follow-up](../reports/2026-11-07-enemy-utility-ai/phase-2-decision-resumption.md#g-atom-follow-up). Verified wall and pursuit refinements retain the frozen behavior. Task 2.0d blocks gameplay activation. |
| 3 | Implementation and Core verification complete; review stop | [Phase 3 report](../reports/2026-11-07-enemy-utility-ai/phase-3-preparation.md). User compilation/F1–F8 Pending; G-atom and gameplay activation remain gated. |
| 4 | Implementation and Core verification complete; review stop | [Phase 4 report](../reports/2026-11-07-enemy-utility-ai/phase-4-assessment.md). New 49 cases pass; all 72 inherited failures match. G-atom and gameplay activation remain gated. |
| 5 | Implementation and Core verification complete | [Phase 5 report](../reports/2026-11-07-enemy-utility-ai/phase-5-candidates-and-hazards.md). Focused 128/128. All 72 baseline failures match. Two warmed Steps exceeded 1 ms. Exact allocation and user Flow acceptance remain open. |
| 6 | Development implementation and Core verification complete | [Phase 6 report](../reports/2026-11-07-enemy-utility-ai/phase-6-utility-choice.md). Diagnostic/prepared Utility, 134/134 Core and 62/62 focused integration. Existing 72 failures match. G-atom, exact allocation, behavior and user Flow acceptance remain open. |
| 7 | Diagnostic review complete. Gameplay acceptance unmet | [Phase 7 report](../reports/2026-11-07-enemy-utility-ai/phase-7-acceptance.md). 55 matched inputs, 1,100 warmed decisions, 220 sides and exact baseline failure comparison. The authorized follow-up corrected Hold on A8/X8/H8. The item 3 report records the rendered capture and per-check F1–F8 dispositions. The remaining behavior, performance and formal user checks gate activation. |

Each report must contain: authorized Phase; start/end source hashes; changed files; task checkboxes completed versus pending; exact Core/Integration test counts and failure identities; UF status; raw performance references and gate table; observed allocation/atomic costs; modification/understanding/verification scope; deviations requiring a decision; and the next Phase's entry condition. Preserve 'NotRun', 'Blocked by environment', 'Known baseline failure', and 'New failure' as different states.

For a later model: do not restart completed Phases, infer test success from a prose summary, change constants to personal preference, skip author restrictions, or treat this plan as authorization for the whole project. Read the latest report and current source before resuming. Record failed gates and concrete bounded remedies. The §8 exception allows Phase 3 development under test or diagnostic composition while the gameplay activation gate remains open. Other acceptance gates retain their stated scope.

## 14. Spec coverage and planning self-review

| Design requirement | Owning Task(s) |
|---|---|
| Baseline and old pursuit optimization preservation | 0.1–1.2, 2.2, 7.1 |
| Core resumable Preview, ordered mutation/observation barriers, cancellation and sealing | 2.0a–2.0d, 2.3; spec §12 |
| Immutable decision context, one actual publication | 2.1–2.3, 3.1–3.3, 6.2 |
| Hard admission, taunt, actor order, Hold | 2.3, 5.1, 6.1–6.2 |
| Cheap shortlist, precise caps, single same-actor follow-up | 5.1, 6.2 |
| Trait consequence reuse, partial/seed-safe coverage | 4.1, 5.2, 6.2 |
| Turn horizon, railway/rockfall, night-end | 4.2 |
| Tornado/haboob/entry hazards/combined uncertainty | 4.1, 5.1–5.2 |
| Shared Hold exposure and player threat | 5.1, 6.1 |
| One-shot scheduling, valid readiness, cancellation | 3.1–3.3, F1–F8 |
| Atomic cost, allocations, determinism and latency | 1.2, 2.2, 3.2, 7.1 |
| SSOT/SOLID/Framework and feature locality | 3.1–3.3, 4.1–4.2, 7.2 |

Planning self-review checks: all selected names have an owning file/Task; forward references resolve to the contract catalog; phase order builds dependencies before consumers; no new View test is requested; every review-focus risk has a named case/manual check; no implementation is represented as done. Concrete private data layouts and fixture coordinates are left to the builder only where the manifest/hash and acceptance cases freeze their observable meaning before measurement. Initial preference weights and the optional alternate-route node cap are explicit plan-level tuning decisions, not measured optimal values.

Historical amendment checkpoint: the user approved the amendment and resumed inline Phase 2 execution. The additional SacrificeDamagePlanner owner scope was approved after that checkpoint report. Its Plan/ResolveSingle compatibility wrappers share owner-local resumable reservation/simulation work; no rule approximation or copied AI implementation is allowed. At that earlier checkpoint Phase 3–7 implementation was not authorized. The later Phase 3 authorization and verified status above supersede that restriction for Phase 3 only. Commits/pushes remain user-owned.

Amendment review: an independent read-only builder check found no important unanswered questions after comparing spec §12, contracts and Tasks 2.0a–2.0d with current owners. Self-review checked exact path existence, BeginPreview/assessment/RouteWork references, logical budget charging, phase dependencies and removal of the former full-Preview-as-one-atom instruction. At amendment delivery all new implementation/verification checkboxes were pending. Subsequent implementation and test evidence are recorded in the checkpoint report; remaining compound checkboxes are intentionally not marked complete from partial evidence.
