# Attack Targeting Methods Design

**Date:** 2026-10-01

**Status:** Documentation proposal requested by the user after Task 5. Confirmed targeting rules are distinguished from implementation proposals and open gameplay decisions. This document does not claim code, DB assets, View integration, or Unity tests have changed.

**Goal:** Implement the supplied designation rules during the existing Buffer combat migration, preserving CharacterBuffer/Soul ownership and the Framework Plan/Job execution route.

**Execution plan:** [CharacterBuffer and Effect migration plan](../plans/2026-09-29-character-buffer-effect-migration-plan.md), Task 7A–7E and Task 8. Task 6 can proceed on its existing persistent-state/turn/cleanup scope. Completed Tasks 0–5 remain historical results.

## 1. Sources, precedence, and inspection

| Source | Role and inspection result |
|---|---|
| User-supplied `C:/Users/권천명/Dropbox/Project L/기획 문서/지정 방식 기획서.pptx` | 22 slides, revision slide 22 says Ver0.1 / 2026-09-29. Read slide text and native shape coordinates, fills, and outlines. SHA-256: `ef7e08eade2eaf52248daa729f194ea69eb4e567e77724b478034dd505614948`. |
| User-supplied `D:/My_ProjectWork/학교 관련 문서/졸업작품_프로젝트L/2학기 내용/AI 관련 문서/project-l-targeting-methods-codex-handoff-v5.md` | Read the design explanation, confirmed corrections, damage rules, and unresolved questions. A [repository snapshot](../handoffs/2026-10-01-targeting-methods-source-handoff.md) preserves its wording and all 18 ASCII diagrams. SHA-256 of original: `e721ccd82701096333efebb6032f923e02272ad1fd2ce530597db3f0dc1247b5`. |
| Current repository | Reviewed at `e597f8d2a2a4cbe2c666768b0cbdda60ea29c59e`; working tree initially clean. Source facts below describe this checkout, not the historical investigation base in the migration plan. |
| Framework and ownership references | [Programmer guide](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md), [architecture](../../../Assets/ProjectL/Script/Systems/Documentation/framework-architecture.md), [development guidelines](../framework/Framework%20Development%20Guidelines.md), [Script README](../../../Assets/ProjectL/Script/README.md), [Core/View allocation](../handoffs/2026-09-30-combat-core-response-to-view.md), and [Task 5 handoff](../handoffs/2026-10-01-character-soul-task5-handoff.md). |

Apply the current user's scope and ownership constraints first. For targeting semantics, use the PPTX together with the explicit confirmations recorded in the supplied MD, including its corrections to the slides. Use current code as evidence of existing behavior and as the regression baseline where the design does not replace it. Proposed types and algorithms below are implementation choices, not claims made by the PPTX. Embedded worker instructions in reference documents do not independently authorize implementation or delegation.

The MD filename says v5, its internal revision says v3, and it names a different PPTX filename. Preserve those facts rather than inventing a revision history. The newly supplied PPTX has the same 18 diagram grids as the MD: all 49 cells in each slide 4–21 matched after mapping native fill/outline styles to the MD legend. This is source-data inspection, not a rendered-slide or gameplay test.

Two printed discrepancies are already resolved by the MD: slide 14 fan is **Type 4**, and slide 15 eight-direction straight is **Type 3**, despite their printed 3/4 values. The fan excludes exact diagonal boundaries. Eight-direction straight with directional choice selects a cardinal three-ray group. Primary damage is 100%; each additional splash target receives its own truncated percentage of pre-defense original damage. Do not reopen these as missing design decisions.

Framework documents contain historical implementation snapshots. For example, the architecture's None/Start-only Job description predates the current CombatPlan, and the programmer guide's assembly summary predates the Systems reference to Core.Application. Preserve their ownership/composition rules while using current source and the later Core/View agreement for actual members and execution status.

## 2. Scope and integration constraints

This change covers attack designation, affected-target resolution, splash damage, attack previews, and the enemy attack-choice consumer. The PPTX describes designation more broadly, but it does not authorize changing movement/healing/effect targeting or scaling non-damage values. Reusable pure geometry may serve future consumers without introducing a generic targeting engine now.

| Owner | Responsibility that remains unchanged |
|---|---|
| CharacterBuffer | Owned card IDs, soul counts, main persistent values, active CharacterData and battle links. No targeting-specific duplicate character state. |
| CombatStateBuffer | Board/turn/structure/field state and the agreed Core result/preview lifetime. Extend concrete result values where required; do not add an AttackBuffer or second result store. |
| CombatViewBuffer / View | Intent submission, preview requests, presentation acknowledgement, and handled-input removal. Core only reads this Buffer. |
| View/Flow | CombatLifeCycle, CombatFlow, CombatPlan, CombatPrepareJob, CombatCommandJob, CombatPreviewJob, CombatAdvanceJob, and their service call sites. |
| Core-owned Systems service | Bind DB via InitData and Buffer interfaces via PostBindService; assemble immutable facts, invoke Core, and publish validated changes through Buffer APIs. |
| Core.Application / Domain | Pure command composition, geometry, target resolution, damage/effect calculations, and typed changes. No Systems, Framework, Unity SO, StageSession, or UnitState ownership on the migrated route. |
| Existing Framework / Systems infrastructure | Factory registration, service groups/keys, Register/Runner, lifecycle and faults. No new dispatcher, Runner, root service, generic transaction, event bus, or lifecycle stage. |

Planned execution route:

```text
View intent -> CombatViewBuffer
CombatFlow -> existing JobRegister -> CombatPlan -> existing JobRunner
CombatCommandJob / CombatPreviewJob / CombatAdvanceJob
  -> existing CoreGroup.Combat processing service
  -> immutable Contracts facts -> Core calculation
  -> existing Buffer-owned validated Apply -> state/result reads -> View
```

The calculation steps are helpers within one command, not separate Jobs. A Job carries the existing service dependency, calls the agreed processing entry once, and becomes complete only after normal return. Game-rule rejection and no-input behavior follow the numbered-result contract; exceptions are not converted into acceptance or defeat. Preserve F1 fault handling and F2 final-result/cleanup ordering.

At the inspected HEAD, `ICombatProcessingService` exposes only `PrepareBattle()`. Command/preview/advance Jobs still contain unconnected exceptions. Proposed processing member names in earlier handoffs are not implemented APIs. Task 7A must update the migration plan's section 4.1 before production View binding.

## 3. Required designation semantics

### 3.1 Independent dimensions

| Source field | Meaning | Constraints |
|---|---|---|
| Method_Key | Unique authored method identity | Actual IDs and storage representation are not supplied. Do not use slide numbers or invent approved IDs. |
| Method_Type | 1 cardinal rays, 2 diagonal rays, 3 eight rays, 4 fan union, 5 square radial | Values describe shape only. They do not encode selection, faction, impact, or range. |
| Method_ChoiceRange | 1 single tile, 2 direction group, 3 full mask | Full mask does not mean all factions. Direction choice does not require an arbitrary primary entity. |
| Method_Splash | 0 none, 1 cross, 2 diagonal, 3 eight-neighbor radial | Splash is used only with single choice. The examples use a fixed one-cell neighborhood of the selected tile. |
| Method_SplashValue | Integer percentage for each additional damage target | Unused when splash is none. 50 is an example, not a default. Allowed authoring bounds remain open. |
| Owner-provided range | Distance supplied by character/building/other owner | Keep range outside the reusable method definition. Displayed 3/2 values are examples, not global constants. |
| Attack eligibility policy | Which entities may be selected/affected | Separate attack policy from Method_ChoiceRange. Preserve confirmed existing profiles until their replacement is explicitly mapped. |
| Existing impact/trait policy | Cannon impact, pierce rays, falloff, and other effects | Not redefined by the new five shapes. See decisions T2/T4/T5. |

### 3.2 Base masks and direction subsets

Coordinates below are relative to the casting cell `C=(0,0)`, with positive y up, as in the source MD. Map them to existing CellCoord/AttackDirection semantics explicitly. Exclude C from these base masks; this does not decide whether splash may damage the acting unit.

Let `r = max(abs(x), abs(y))`, with `0 < r <= R`.

| Type | Base-mask membership | Direction choice | Example size |
|---|---|---|---|
| 1 | `x == 0 or y == 0` | One of Up/Right/Down/Left; one ray | R=3: base 12, selected ray 3 |
| 2 | `abs(x) == abs(y)` | One of four diagonals; one ray | R=3: base 12, selected ray 3 |
| 3 | `x == 0 or y == 0 or abs(x) == abs(y)` | One cardinal direction; forward ray plus its two adjacent diagonal rays | R=3: base 24, group 9 |
| 4 | `abs(x) != abs(y)` | One cardinal filled fan; depth k has lateral offsets `-(k-1)..k-1` | R=3: base 36, fan 9 |
| 5 | All offsets satisfying `0 < r <= R` | One cardinal side of the square, excluding the center axis | R=2: base 24, side 10 |

For type 5 Left, select `-R <= x <= -1` and `-R <= y <= R`. Rotate for other cardinal directions. This is a rectangular half-side of the square, not a triangle and not a recentered square. Type 4 is a filled fan, not the three rays of type 3. Full choice designates the whole corresponding base mask. Single choice designates one selected cell in that mask.

### 3.3 Splash

With primary selected cell p, the additional offsets are:

| Splash | Offsets from p | Count |
|---|---|---:|
| Cross | `(-1,0), (1,0), (0,-1), (0,1)` | 4 |
| Diagonal | `(-1,-1), (-1,1), (1,-1), (1,1)` | 4 |
| Radial | All eight neighbors around p | 8 |

The splash center is the selected cell, not the attacker and not an automatically substituted unit anchor. Additional cells may extend outside the primary selection range. Keep origin p separate from the additional set. Board-edge, obstacle, empty-cell, and footprint rules need the decisions in section 8; they cannot be inferred from these diagrams.

For the confirmed pre-defense reference D and integer percentage m:

```text
Primary splash-stage damage = D
Each additional target's splash-stage damage = truncate(D * m / 100)
```

Examples: D=20, m=50 gives primary 20 and each additional target 10. D=7, m=50 gives 7 and 3. D=13, m=30 gives 13 and 3. The percentage is not shared among targets and the primary does not receive a second splash application. These are splash-stage values, not a promise about final HP loss after target defenses/effects.

Current damage order is outgoing bonuses, rounded pierce falloff when applicable, target Category matchup using half-up rounding, physical-defense pass-through, then incoming reduction. `TargetDamagePipeline` has no physical-defense formula today. The MD fixes splash before defense but does not locate D relative to matchup, pierce falloff, or target-dependent outgoing effects. Task 7C must resolve T4 before composing these stages. Preserve existing half-up stages separately from splash truncation. Do not add critical hits or a defense formula.

**Ordinary T4 decision — user-confirmed 2026-10-01:** Capture one common D after attacker outgoing additions/effects. Primary and Designated use D; Additional uses trunc(D*m/100). Then apply each target's existing half-up Category matchup, physical-defense pass-through and incoming reduction, followed by HP subtraction. Primary HP loss and target-adjusted values never become D. This decision supersedes the ordinary-order uncertainty above. Pierce falloff combined with new splash and special-profile mapping remain undecided under T2/T4; retain their existing behavior until separately resolved.

Implementation proposal: calculate percentage products in checked/widened integer arithmetic, validate the result before converting to the existing damage type, and reject unsupported data before publication. Never wrap overflow, silently clamp authoring values, or convert a runtime fault into a normal game rejection. Percentage-domain validation is T6; 0/100 can be calculation test inputs without declaring every percentage an approved asset value.

## 4. Comparison with current code

Paths in sections 4–7 use `Core/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/`, `Systems/` = `Assets/ProjectL/Script/Systems/`, and `Tests/` = `Assets/Tests/EditMode/`.

| Current code / evidence | Reuse or required change |
|---|---|
| `Core/Contracts/Common/AttackTargetMode.cs`: SingleTarget=1, AllHostilesInArea=2, AllDamageablesInArea=3 | Mixed selection/faction semantics. Never cast Method_ChoiceRange into this enum. |
| `Core/Contracts/Common/AppointMethod.cs`: ForwardLine=1, ForwardLineWithDiagonal=2, Cross=3, Queen=4, ForwardExpanding=5, RadialSquare=6, ForwardCone=8 | Numbers are incompatible with Method_Type. Preserve serialized meanings until explicit data migration; map by semantics, not integers. |
| `Core/Domain/Combat/AppointShapeCalculator.cs` | Reuse pure CreateRay, CreateSquare, and CreateExpanding. CreateExpanding already excludes diagonal boundaries. |
| `Core/Domain/Combat/AppointRangeCalculator.cs`, `AttackGeometryCalculator.cs` | Reuse verified math and cardinal three-ray grouping. Extract/adapt state-bound inputs. Add diagonal-only base and square-side directional selection. Current per-method branching is not the new independent choice contract. |
| `Core/Contracts/Commands/AttackAim.cs`, `Domain/Combat/AttackSelectionResolver.cs` | Entity/cell aims with optional directions exist. Pure direction and full-area intent require explicit representation; do not invent a target ID/cell just to satisfy old validation. |
| `Core/Contracts/Commands/AttackProfileCompatibility.cs` | Current Category/shape/target/impact whitelist rejects many new legal combinations. Replace affected validation alongside contracts, DB adapters and fixtures. Do not broaden it indiscriminately for unsupported legacy effects. |
| `Core/Domain/Combat/AttackTargetResolver.cs` | Reuse footprint intersection, EntityId deduplication and deterministic sorting. It currently excludes self, dead and undeployed entities, and resolves hostile/all-damageable policy. These are code facts, not proof of new designer decisions. Replace UnitState/CombatEntityState inputs. |
| `Core/Application/Combat/UnitAttackService.cs`, `Domain/Combat/TargetDamagePipeline.cs` | Extract attack settlement/damage relationships and retain downstream effects. Add explicit primary/additional roles and scaling stage after T4. Do not call the legacy stateful service from migrated commands. |
| `Core/Domain/Combat/CannonAppointExclusionCalculator.cs`, `CannonPierceTargetResolver.cs`; `Tests/BallistaCombatIntegrationTests.cs` | Cannon has footprint-based near-aim exclusion and pierce-specific behavior. Multi-ray hits can aggregate for one entity. Ordinary target deduplication must not silently erase this behavior; retain or replace only through T2/T5. |
| `Core/Contracts/Results/AttackTargetPreview.cs`, `AttackHitResult.cs`, `AttackPreview.cs`, `AttackRangeResult.cs` | Reuse result IDs, cells, damage breakdown and existing ray-hit detail where applicable. Add role/scaling facts needed by View without inventing a single primary entity for directional/full attacks. |
| `Core/Application/Combat/EnemyAi/EnemyAiPolicy.cs` | Currently consumes attack aims and preview outcomes through old stage/gateway inputs. Migrate candidate generation to the same choice legality/calculation as player attacks; preserve AI priorities where still meaningful. |
| `Systems/DataBaseSystem/Data/InGame/Table/Unit/Item/UnitDataSO.cs`, `UnitData.cs`; `Core/Contracts/Commands/UnitSpec.cs`, `Domain/Entities/UnitDefinition.cs` | Carry current serialized pattern/target/impact settings. Introduce explicit method definition/reference mapping with the DB owner; keep range on the owner. Do not edit existing asset numbers in place. |
| `Systems/RuntimeSystem/BufferSystems/CombatView/CombatViewCommand.cs`, `CombatViewPreview.cs` | Existing attack wrappers already carry AttackAim. Prefer a narrow extension of this shared payload and its format guard; View owns those changes and input writes. |

Observed assets under `Assets/ProjectL/DataBaseSO/InGameGroup/Unit/Item/`: Akimbo and Demon use legacy RadialSquare, range 1, SingleTarget; Sniper uses RadialSquare, range 4, SingleTarget; ShotGun uses ForwardCone, range 2, AllHostilesInArea. These are an initial inventory, not a complete roster or approved new Method_Key assignment. Record all main/sub/enemy/structure consumers and test definitions in T1 before migration.

## 5. Proposed contract and calculation structure

### 5.1 Contracts and static data

Use small immutable Contracts values. Proposed names below are not existing APIs:

- `TargetingShape`: CardinalLine=1, DiagonalLine=2, EightDirectionLine=3, Fan=4, RadialSquare=5, with invalid/default zero.
- `TargetingChoiceMode`: Single=1, Direction=2, All=3, with invalid/default zero.
- `TargetingSplash`: None=0, Cross=1, Diagonal=2, Radial=3.
- `TargetingMethodSpec`: Shape, ChoiceMode, Splash, SplashPercent. Range stays in owner attack facts. Method_Key resolution belongs to static DB/definition assembly and does not require a new runtime ID service.
- Extend existing `AttackAim`/`AttackAimKind` for direction-only and full-area choices. Proposed factories: `ForDirection(AttackDirection)` and `ForAll()`. Single uses the selected cell; retain Entity convenience only with an explicit resolution rule for multi-cell entities (T5). Default/None remains invalid. Do not repurpose old serialized enum values.
- Keep target eligibility and impact settings separate from `TargetingMethodSpec`. Reuse existing hostility checks through immutable facts. Introduce only the concrete policy fields required by approved mappings, not a configurable predicate language.
- Add role and scaling facts to the existing attack result family. Proposed `AttackTargetRole` values: Primary, Additional, Designated. Direction/full hits are Designated without a fabricated center. Preserve pierce hit details when T2 retains them.

Task 7A records exact file locations, constructors, query/processing signatures, and the View contract in plan section 4.1 before consumers bind. These choices are a proposed implementation of the supplied semantics; final authored method keys, mappings, and unresolved gameplay rules cannot be filled by naming a type.

### 5.2 One attack calculation

1. Read the pending intent once at the Systems boundary. Gather current character/soul/effect, board/structure and static method facts through the existing read services. Validate actor, turn, resources, definition and choice format.
2. Build the base mask using the owner's range and the agreed origin/footprint policy. Apply single, direction, or full choice to obtain designated cells. Keep selectable cells distinct from affected cells.
3. For single+splash, expand around the selected cell. Apply the agreed map/obstruction and primary-versus-additional eligibility rules. No additional cell is rejected merely because it exceeds primary selection range.
4. Resolve entity/structure footprints against the cells and assign target roles. Deduplicate ordinary shape overlaps by EntityId. Resolve primary/additional overlap and retained pierce multi-hits using T5. Maintain deterministic target/hit ordering and preserve death/effect ordering from Task 5.
5. Evaluate per-target damage with the agreed D boundary and existing damage/effect stages. Calculate movement settlement, damage/death, reflection/forced movement, soul changes and outcome within Task 7's planned complete-command projection, preserving established settlement ordering. Use Task 5 SoulEffectCalculator inputs/results. Preview may calculate projected soul consequences but must not publish collection/spawn or count changes to live Buffers; never reapply counts as cumulative bonuses.
6. Return the migration plan's command transition with the required attack facts; this transition remains a Task 7 deliverable, not an implemented API at the reviewed HEAD. Before any live write, validate every affected character/field/resource/progress slice and result metadata. Apply through the Buffer group and publish results last. Separate a validation rejection from an accepted action whose settlement kills the actor.

Avoid a class per slide and avoid an abstract pipeline/handler registry for these steps. Reuse pure helpers and add focused functions where a responsibility is missing. No transient reconstruction of StageSession, UnitState, old services, or a mutable equivalent is allowed.

### 5.3 Preview, View, and AI

Preview uses the same calculation and the same input facts as execution, but publishes only its request-numbered preview. It does not change HP, Action/movement resources, soul field/counts, effect consumption, phase or terminal result sequence. Actual execution revalidates against current facts and does not commit a stale preview object.

Provide the legal choice mode/directions, selectable cells, designated/affected cells, target roles, percentage and damage breakdown needed for display. UI gestures remain View-owned: a directional button or clicked cell can produce the same direction intent after an agreed mapping. Full-area choice still uses the existing attack command and does not require a dummy aim entity.

Enemy AI enumerates the same legal single cells, direction groups, or one full-area candidate and scores actual previews. It cannot treat Type 3 as eight independent direction choices. Preserve applicable target priority and deterministic tie-break behavior; do not introduce a new AI scheduler or duplicate damage formulas.

Result metadata and lifetime follow the migration plan: handled input number, terminal result sequence, request-numbered preview, retained latest result for Flow after View acknowledgement, and no Core writes to CombatViewBuffer. Input-number gaps and no-input behavior remain explicit. View changes are delivered through the existing coordination contract, not silently assigned to the Core implementer.

## 6. Source example acceptance matrix

All entries use an unobstructed sufficiently large board with a one-cell caster, so unresolved board/footprint decisions cannot alter the source comparison. The [source snapshot](../handoffs/2026-10-01-targeting-methods-source-handoff.md) section 6 supplies exact expected cell sets. Count checks supplement exact coordinate assertions.

| Slide | Type | Choice | R | Selected example | Designated cells | Additional cells |
|---:|---:|---|---:|---|---:|---:|
| 4 | 1 | Single | 3 | (0,2) | 1 | 0 |
| 5 | 2 | Single | 3 | (-1,1) | 1 | 0 |
| 6 | 3 | Single | 3 | (1,-1) | 1 | 0 |
| 7 | 4 | Single | 3 | (-3,0) | 1 | 0 |
| 8 | 5 | Single | 2 | (-1,1) | 1 | 0 |
| 9 | 5 | Single | 2 | (-1,2), Cross | 1 | 4 |
| 10 | 5 | Single | 2 | (-1,2), Diagonal | 1 | 4 |
| 11 | 5 | Single | 2 | (-1,2), Radial | 1 | 8 |
| 12 | 1 | Direction | 3 | Up | 3 | 0 |
| 13 | 2 | Direction | 3 | UpLeft | 3 | 0 |
| 14 | 4 | Direction | 3 | Left | 9 | 0 |
| 15 | 3 | Direction | 3 | Right | 9 | 0 |
| 16 | 5 | Direction | 2 | Left | 10 | 0 |
| 17 | 1 | All | 3 | Entire mask | 12 | 0 |
| 18 | 2 | All | 3 | Entire mask | 12 | 0 |
| 19 | 3 | All | 3 | Entire mask | 24 | 0 |
| 20 | 4 | All | 3 | Entire mask | 36 | 0 |
| 21 | 5 | All | 2 | Entire mask | 24 | 0 |

Additional required checks:

- Rotations of every directional shape; exact diagonal rejection for fan; explicit rejection of invalid direction groups; type 3 Up/Left produce the MD's confirmed three-ray groups.
- Two owner ranges using one method definition; no hard-coded 3/2 or method-specific range storage. Validate zero/negative/excessive range behavior at the contract boundary without inventing a game cap.
- Splash cell sets retain cells beyond the primary mask and do not include p; directional/full definitions with active splash are rejected by the proposed authoring validation.
- Splash arithmetic examples in section 3.3, independent per-additional-target scaling, no primary rescaling, and widened arithmetic. Add target matchup/falloff/defense cases only after T4 specifies the exact order.
- Multi-cell overlaps, primary/additional role precedence, Cannon exclusions/retained multi-rays, off-board/blocked/empty/self/ally cases after the corresponding decisions. Record expected outcomes explicitly rather than asserting the old implementation by default.
- Preview/execute parity on identical facts; zero preview gameplay writes; rejected commands leave every gameplay Buffer unchanged; invalid final proposals publish no partial changes.
- Player/AI candidate parity; real Register -> Runner -> Job -> service -> Buffer route; View input-writer spy remains untouched by Core; acknowledgement preserves Flow reads.

## 7. File responsibilities and implementation slices

The existing migration plan is the sole execution checklist; do not create a parallel attack migration that first preserves and then rewrites the old targeting flow.

| Slice | Main files / responsibilities | Verification |
|---|---|---|
| Task 7A: contracts and mapping | Proposed `Core/Contracts/Common/TargetingShape.cs`, `TargetingChoiceMode.cs`, `TargetingSplash.cs`, `AttackTargetRole.cs`; `Core/Contracts/Commands/TargetingMethodSpec.cs`; extend AttackAim/AttackAimKind and affected profile/UnitSpec/UnitDefinition/UnitData validation. Review View wrappers without transferring ownership. | Proposed `Tests/TargetingMethodContractTests.cs`: exact values, invalid combinations, independent faction/choice, owner range, direction/full intent and no numeric legacy casts. |
| Task 7B: geometry and target roles | Reuse AppointShapeCalculator; adapt AppointRangeCalculator, AttackGeometryCalculator, AttackSelectionResolver, AttackTargetResolver to facts. Add missing operations in those responsibility areas. | Proposed `Tests/TargetingMethodGeometryTests.cs`, `TargetingMethodSelectionTests.cs`: 18 exact fixtures, rotations, legal intent, resolved T3/T5 cases. |
| Task 7C: splash and damage | TargetDamagePipeline and the attack calculation extracted from UnitAttackService; existing preview/hit breakdown types; preserve impact and trait helpers selected by T2. | Proposed `Tests/TargetingSplashDamageTests.cs`; existing CannonPierceTests, CannonBombardmentTests, BallistaCombatIntegrationTests, PiercingFalloffPlanningTests as applicable. |
| Task 7D: one Buffer command | Existing planned CombatCommandCalculator, CombatCommandInput/Transition, CombatProcessingService, Buffer group Apply, Task 5 SoulEffectCalculator. | Planned CombatCommandCalculationTests and Framework/CombatCommandPublicationTests: damage/effect/death/soul consistency and no partial publication. |
| Task 7E: preview and AI consumers | Existing attack range/preview/result types and EnemyAiPolicy/EnemyAiTurnService extraction; service adapters; View-owned input guard/presentation integration agreement. | UnitAttackPreviewTests, StageEnemyAiTurnTests and proposed TargetingPreviewParityTests; test metadata, parity, no mutation and candidate choices. |
| Task 8: production connection | Existing DB table/query/definition assembly, Core factory/service binding, and View-owned CombatPlan/Jobs/Flow. Actual SO authoring stays with the DB owner. | Actual authored mappings and validation through real DB reads; Register/Runner integration; existing F1/F2/F3/G7/G9 gates plus section 8 targeting gates. |

New files listed here are proposals. Keep existing .meta GUIDs and asmdef names. Generate new .meta files with Unity during implementation; do not edit generated csproj files. No new assembly is required. Existing Framework algorithms, queue priority, and lifecycle code are not planned change targets. If a concrete integration problem requires a shared change, first identify its files, reason and impact under the existing notification rule.

## 8. Scoped decisions before affected implementation

These questions do not block writing the documents, Task 6, or source-mask/arithmetic tests. They block only the implementation or production profile whose behavior depends on the missing answer. Record answers here and in the migration plan's targeting gate, with concrete expected tests.

| ID | Decision still needed | Existing evidence / safe scope while open | Blocks |
|---|---|---|---|
| T1 | Partially resolved owner matching; missing production definitions, actual Method_Key list and DB reference representation | The user's 2026-10-01 decisions in [inventory section 1.1](../plans/2026-10-01-targeting-method-owner-inventory.md#11-t1-owner-matching-decisions-and-remaining-parameters) confirm CactusCannon range 4 / initially authored adjustable 70% radial splash, and Gravekeeper range 1 / knockback 1 cell. Missing production definitions remain missing; do not silently convert legacy numeric fields. | Production definition/asset migration. |
| T2 | Multi-cell unit ThornField treatment is user-deferred; current one-cell profile rules are resolved | Ballista Direction retains existing 30-point per-ray falloff/aggregation. Cannon near direct-aim exclusion remains. CactusCannon replaces old Square damage with single/radial splash and retains existing one-cell ThornField rules with own attack damage excluded. See [inventory section 1.3](../plans/2026-10-01-targeting-method-owner-inventory.md#13-retained-cannon-rules-board-boundary-and-thornfield-clarification). | Deferred or separately affected profiles only; one-cell adaptation can proceed. |
| T3 | Partially resolved: neutral eligibility and empty All remain open | User confirms empty Single including no surrounding targets, allied-unit/allied-structure hits, acting-unit own attack damage exclusion, and empty Direction rejection without attack AP charge. Empty Single follows normal accepted-command AP cost. A friendly-only group is nonempty. See [inventory section 1.2](../plans/2026-10-01-targeting-method-owner-inventory.md#12-t3-admission-and-target-policy-decisions). Preserve dead/undeployed/damageability checks and existing traversal/external damage. | Remaining affected admission/eligibility; confirmed cases can be implemented. |
| T4 | Ordinary order confirmed by the user 2026-10-01: outgoing capture, role scaling, per-target matchup, defense, incoming reduction, HP. Pierce/new-splash composition remains open | Primary/Designated100%; Additional truncates independently. Preserve half-up matchup, defense pass-through and bounded incoming reduction. No primary HP-loss reference, critical-hit or new defense rule. | Only unresolved combined special profiles; ordinary composition can proceed. |
| T5 | Multi-cell unit origin, aim convenience, overlapping hits and ThornField treatment are deferred at the user's request | Current Task 7D covers one-cell units; keep existing structure footprint contracts and the approved Ballista per-ray aggregation. Preserve footprint-capable pure helpers and legacy regressions without activating undefined multi-cell unit behavior. | Deferred unit support only; does not block one-cell Task 7D. |
| T6 | Obstacles/blocking units and remaining range/percentage authoring domain | User confirms the board edge ends combat space: discard off-board splash/effect cells with no damage, field or combat-cell output. Do not reject legal in-board aims or wrap/clamp outside cells to the edge. Raw masks remain independent. Follow terrain source gate R7/R8; do not invent obstacles. | Remaining affected blocking/authoring; board-aware clipping can proceed. |
| T7 | View gesture-to-direction/full intent mapping, exact shared payload/processing API and result detail | Preserve View's Plan/Job/input ownership. Prefer direction/full variants on existing AttackAim and existing command kinds. Share concrete contracts before implementation binding. | Production View input/preview/AI handoff where a consumer contract changes. |

## 9. Documentation verification and completion limits

This revision inspected PPTX text/native shapes, the supplied MD, current relevant code, and Framework/ownership references. Source comparison verified all 18 MD grids against PPTX native shapes. It did not render/export the deck, change either external source, run Unity, or implement an attack.

Before handing off these documents, verify links, the 18-row matrix, corrected type numbers, confirmed damage semantics, Task 7/8 coverage, and the unchanged Buffer/View/Job boundaries. Use `git diff --check` and inspect the changed-file list. Implementation must later run the focused Core/Framework fixtures and applicable full suites using migration plan section 7; historical Task 5 results and this source comparison are not new gameplay test evidence.
