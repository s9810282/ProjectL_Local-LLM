# Attack Target Policy Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Restrict empty-ground attacks and friendly fire to Cannon while retaining hostile-unit, hostile-structure, and authored-obstacle attacks for every category.

**Architecture:** Derive an immutable target policy from existing combat effect bindings. SafeBombardment supplies Cannon permissions; shared attack selection applies them consistently to range, preview, execution, and AI. Geometry and public View/DB contracts remain unchanged.

**Tech Stack:** Unity 6000.0.73f1, C#, existing Core assemblies, NUnit EditMode tests.

**Spec:** [Attack Target Policy Design](../specs/2026-10-07-attack-target-policy-design.md). Tasks 1-4 are implemented and verified as of 2026-10-07. Automated Core-to-View smoke checks passed; real-scene manual/PlayMode acceptance was not run.

## Global Constraints

- Implementation documentation is English; new code comments are Korean.
- Commits and pushes remain user-owned.
- Single remains a cell aim. Direction and All remain group aims.
- Range queries, previews, execution, and AI must not implement separate faction or empty-target rules.
- Do not change AttackAim, TargetingMethodSpec, AttackRangeResult, View command payloads, or DB assets.
- Do not store policy flags in CharacterBuffer or unit authoring.
- Multi-cell units remain deferred. Existing structure footprints and structure ray blocking remain supported.
- Preserve independent environmental damage and KillChainAttack's stricter hostile-character-only selection.

## Review Focus

- A hostile splash neighbor must not rescue an empty/friendly non-Cannon primary cell: Task 2.
- A filtered allied structure must still block pierce; a filtered character must not consume falloff budget: Task 2.
- Pending movement, armed discounts, and refunds must not settle on rejected aim: Task 2.
- Neutral attackable obstacles remain legal; ordinary neutral occupants remain illegal for non-Cannon: Task 2.
- AI pursuit and immediate ThornField damage can bypass ordinary target collection: Tasks 2 and 3.

## Paths and verification

`CORE` means `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/`. `TESTS` means `Assets/Tests/EditMode/`. These abbreviations identify exact existing directories.

At execution time, use an isolated worktree if needed. Do not launch a second Editor against a project already open in Unity; use its EditMode runner instead. For an available project, run the following command with the task's filter and a unique result filename:

```powershell
& 'D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'D:/My_ProjectWork/Github_desktop/Project_L' -runTests -testPlatform EditMode -assemblyNames ProjectL.Tests.Core -testFilter 'ProjectL.Tests.Core.AttackTargetPolicyTests' -testResults 'TestResults/attack-target-policy-task1.xml' -logFile 'TestResults/attack-target-policy-task1.log'
```

Use the worktree's absolute projectPath when applicable. Omit `-quit` for this asynchronous test run. Read XML totals and failures plus compile logs: exit code alone is not proof. New red tests must fail on the intended assertion, not missing compilation or invalid fixtures. Record test counts and zero failures after each green run. A temporary .NET harness does not replace Unity assembly/View verification.

## Task 1: Evaluate category permissions through the effect engine

**Create:**

- `CORE/Domain/Combat/AttackTargetingPolicy.cs`
- `CORE/Domain/CombatEffects/Evaluations/EffectAttackTargetingContribution.cs`
- `CORE/Domain/CombatEffects/Evaluations/EffectAttackTargetingEvaluation.cs`
- `TESTS/AttackTargetPolicyTests.cs`

**Modify:**

- `CORE/Domain/CombatEffects/Rules/CombatEffectCapabilities.cs`
- `CORE/Domain/CombatEffects/ICombatEffectEngine.cs`
- `CORE/Domain/CombatEffects/CombatEffectEngine.cs`
- `CORE/Domain/Categories/Rules/SafeBombardmentCategoryRule.cs`
- `CORE/Application/Combat/CombatCommandEffects.cs`

**Interfaces:**

- `AttackTargetingPolicy(bool allowsEmptySingle, bool allowsFriendlyTargets, bool allowsNeutralTargets, bool excludesAdjacentDirectCells)` with the spec's four read-only properties; default is all false.
- `EffectAttackTargetingContribution(EffectOriginKey origin, AttackTargetingPolicy policy)` exposes Origin and Policy.
- `IEffectAttackTargetingRule.EvaluateAttackTargeting(in EffectRuleInput input)` returns that contribution.
- `ICombatEffectEngine.EvaluateAttackTargeting(IReadOnlyList<EffectRuleBinding> bindings)` returns an evaluation exposing Policy, Contributions, and Expectations.
- Existing `CombatCommandEffects.Capture(CharacterCombatFacts character, CombatAttackProfile profile)` remains the composition entry point.

- [x] Add `NoContribution_UsesRestrictiveBaseline`, `CannonBinding_GrantsAllFourCapabilities`, and `OtherCategories_DoNotGrantCapabilities` for Base/Rider/Heavy. Assert exact booleans and unchanged state. Run red.
- [x] Add deterministic OR-combination, duplicate-origin, mismatched-origin, and expectation-origin parity tests using fake capability rules independent of CategoryKey.
- [x] Implement immutable values and the pure evaluator using existing binding validation/sorting and defensive-copy conventions. Add Korean comments for policy semantics.
- [x] Bind SafeBombardment through the existing factory and `StatelessEffectStateSnapshot.Instance`; grant all four properties in the rule. Preserve Heavy/Rider snapshots. Do not add a new policy registry or per-category policy class family.
- [x] Run `AttackTargetPolicyTests`, `CombatEffectEngineTests`, and `CharacterCategoryBootstrapTests`. Policy evaluation must create no activation or state transition.
- [x] Review the diff; retain it uncommitted. Include Unity metadata for new scripts through the normal project workflow.

**Deliverable:** Production bindings expose Cannon policy; attack consumers are unchanged until Task 2.

## Task 2: Share eligibility across selection, range, execution, and pierce

**Create:**

- `CORE/Domain/Combat/AttackTargetEligibility.cs`
- `CORE/Application/Combat/ResolvedAttackSelection.cs`
- `TESTS/AttackTargetAdmissionTests.cs`

**Modify:**

- `CORE/Application/Combat/CombatAttackQueries.cs`
- `CORE/Application/Combat/CombatCommandCalculator.cs`
- `CORE/Application/Combat/EnemyAi/EnemyMovementPathfinder.cs`

**Interfaces:**

- `AttackTargetEligibility.CanTargetCharacter(CharacterCombatFacts actor, CharacterCombatFacts target, in AttackTargetingPolicy policy)` returns bool.
- `AttackTargetEligibility.CanTargetStructure(CharacterCombatFacts actor, StructureCombatFacts target, bool isAttackableObstacle, in AttackTargetingPolicy policy)` returns bool.
- `ResolvedAttackSelection` exposes Cells (`AttackCellSelection`), Targets (`IReadOnlyList<KeyValuePair<EntityId, AttackTargetRole>>`), EligibleCharacters (`IReadOnlyList<CharacterCombatFacts>`), EligibleStructures (`IReadOnlyList<StructureCombatFacts>`), Policy, and IsPiercing. Copy incoming collections.
- Change internal `CombatAttackQueries.Selection(CombatCommandInput input, CharacterCombatFacts actor, CombatAttackProfile profile, out ResolvedAttackSelection selection, AttackAim? aim = null, MovementEnvironmentFacts environment = null)` to return AttackFailureReason. Failure sets selection to null.
- Add required `AttackTargetingPolicy policy` after `budget` in `CollectPierce`; keep optional effectiveRange last. All callers use resolved candidates and policy; convert read-only collections at the existing array API boundary.

- [x] Write red tests for Base/Rider/Heavy empty, friendly, dead, empty-with-hostile-splash, and friendly-with-hostile-splash primary cells. Assert absent aim, rejected preview/execution, and unchanged AP/HP/checkpoint/trait/timeline. Build matching category profiles and stats.
- [x] Add hostile-unit, hostile-structure, authored neutral obstacle, and zero-damage-hostile positive cases. Add Cannon empty/friendly success, AP 5 to 3 once, near rejection, self exclusion, and Player/Enemy symmetry.
- [x] Implement shared predicates and resolved selection. Restrictive Single requires an eligible Primary before settlement. Preserve geometry/bounds/near failure precedence; return InvalidPrimaryTarget for an empty/dead primary and NonHostile for a living friendly/neutral non-obstacle primary. Retain empty Direction/All rejection.
- [x] Derive Range's direct target IDs and AttackAims from the same policy/admission. Keep RangeCells geometric and CanAffordAttack independent. Out-of-range splash victims remain preview-only indirect targets.
- [x] Consume resolved facts in Calculate and AI pursuit; remove duplicated broad candidate collection. Gate near exclusion by policy. Check physical living structure occupancy separately so an excluded structure still blocks pierce without taking damage.
- [x] Add `FriendlyBlocker_StopsPierceWithoutTakingDamage`, `FilteredCharacter_DoesNotConsumePierceEncounter`, `CannonPierce_RetainsNearExclusionAndFalloff`, and `NonCannonPierce_DoesNotInheritCannonNearExclusion`. Preserve finite budgets, encounter ordering, ray aggregation, and structure footprint behavior.
- [x] Run `AttackTargetAdmissionTests` and `AttackTargetPolicyTests`; compile all migrated callers. Handle obsolete legacy expectations explicitly in Task 4 rather than weakening these tests.
- [x] Review rejection metadata versus gameplay state and retain the diff uncommitted.

**Deliverable:** Admission and ordinary/pierce damage share one policy; immediate effects still require Task 3 before shipping.

## Task 3: Close effect and consumer bypasses

**Create:** `TESTS/AttackTargetPolicyIntegrationTests.cs`.

**Modify:**

- `CORE/Application/Combat/CombatCommandCalculator.cs` (immediate ThornField occupant damage)
- `TESTS/StageEnemyAiTurnTests.cs`
- `TESTS/EnemyMovementPathTests.cs`
- `TESTS/KillChainAttackTraitTests.cs`
- `TESTS/Framework/CombatInteractionTests.cs`

**Interfaces:** Reuse Task 2 predicates and resolved selection; no new public API. AI keeps consuming GetAttackRange/Preview. KillChainAttack retains its narrower eligibility.

- [x] Write a red non-Cannon ThornField test with a legal hostile primary and allied secondary occupant: neither direct nor immediate field damage may hit the ally. Add the Cannon counterpart and self-exclusion cases.
- [x] Apply the shared predicate before immediate ThornField occupant damage and hit/role publication. Do not alter later movement through persistent fields or obstacle explosion rules.
- [x] Add AI regressions: non-Cannon friendly-only direction is not selectable; mixed directions hit only eligible direct targets; Cannon ally collateral still affects tactical penalties; pursuit feasibility agrees with finite-budget pierce preview.
- [x] Add a KillChain regression proving Cannon permissions do not widen its hostile-character-only selection. Add an obstacle explosion regression distinguishing legal targeting from independent environmental collateral damage.
- [x] Add a View test supplying a Core response without the invalid cell aim, dispatch existing interaction input, and assert no attack command. Supply Cannon's legal empty-cell aim and assert submission. Keep category checks out of View; preserve RecordInputOnly's recording behavior.
- [x] Run `AttackTargetPolicyIntegrationTests`, `StageEnemyAiTurnTests`, `EnemyMovementPathTests`, and `KillChainAttackTraitTests`. Run `ProjectL.Tests.Framework.CombatInteractionTests` using `-assemblyNames ProjectL.Tests.Framework` with a separate XML/log.
- [x] Review effect origins and retain the diff uncommitted.

**Deliverable:** Alternate damage and consumer paths respect policy without granting immunity from environmental hazards.

## Task 4: Migrate old expectations and verify the full contract

**Modify:**

- `TESTS/CombatCommandTestData.cs` (an explicit matching category/profile helper only if needed)
- `TESTS/CombatCommandCalculationTests.cs`
- `TESTS/TargetingPreviewParityTests.cs`
- Other existing targeting/obstacle/effect tests only where their previous permission or fixture is invalidated
- `docs/superpowers/plans/2026-10-01-targeting-method-owner-inventory.md`
- This plan and its spec with actual verification outcomes

**Interfaces:** No production API additions. Preserve the shared fixture's Base default; construct Cannon explicitly where required.

- [x] Split `EmptySingle_IsAcceptedWithoutInventingPrimary_AndPaysNormalCost`, `EmptySinglePreview_HasSelectedCenterAndNoInventedPrimary`, `CasterInSplash_IsExcludedWhileFriendlyTargetsStillReceiveDamage`, and `GroupSelection_HitsFriendlyUnitsAndOwnStructuresAtFullDamage` into Cannon-positive and non-Cannon-negative coverage as appropriate.
- [x] Migrate empty-center splash and dead-cell expectations. Give unrelated tests legal primary targets so they still exercise their original purpose. Do not mass-replace Base with Cannon or remove assertions merely to recover passing results. Geometry-only tests remain faction-independent.
- [x] Run all `ProjectL.Tests.Core` EditMode tests without a testFilter and affected Framework interaction/input tests. Read XML counts/failures and compile logs; report environment limits separately from test outcomes.
- [x] Audit queries, execution, AI pursuit, and View for dispersed CategoryKey.Cannon checks, duplicated broad candidate arrays, or policy persistence in Buffer. Mapping CategoryKey to a rule in Capture/composition is expected.
- [x] Verify that the owner inventory links to the superseding requirements; record only implemented and verified outcomes. Keep unrun manual checks explicitly unverified.
- [x] Smoke-test Base empty/ally rejection, Base enemy/obstacle success, Cannon empty cost once, Cannon allied damage, Cannon near rejection, and ShotGun hostile-only hits. Verify hover/preview/execution agreement through automated real Core-to-View tests; real-scene manual checks remain unverified.
- [x] Run `git diff --check`, review the final diff, and hand it to the user without commit or push.

**Completion:** All four tasks and required regressions must pass before claiming the gameplay change is complete. This planning request does not start implementation automatically. Native execution in the current chat is recommended because the tasks share tightly coupled internal interfaces; review the complete behavioral change before integration.

## Task 1 verification record (2026-10-07)

- Implemented only the immutable policy, origin-tracked contribution/evaluation, effect capability, pure evaluator, and stateless SafeBombardment binding. Attack consumers remain unchanged; the gameplay restriction belongs to Tasks 2-3.
- Kept the existing composition boundary and factories. Avoided an additional registry and per-category policy hierarchy. Evaluation validates sorted contribution/expectation pairs in one pass instead of duplicating HashSets.
- RED: the production Cannon binding test failed (expected one binding, actual zero) while all 18 pre-existing focused tests passed. The remaining new policy tests failed against compile-valid unimplemented API bodies.
- GREEN: the .NET NUnit harness passed 32/32 cases. Unity 6000.0.73f1 EditMode independently passed the same 32/32 cases (14 new policy cases, 18 existing engine/category cases), with zero failed or skipped tests and process exit code 0.
- Checked independent permissions, deterministic aggregation, restrictive baseline, immutable result collections, invalid/duplicate/foreign origins, source-state preservation, and Cannon's lack of attack-success mutations.
- Fresh read-only review found no actionable issues. No commit or push was made. Unity's generated FMOD scan timestamp/path changes were restored; no asset behavior was changed.
## Task 2 verification record (2026-10-07)

- Implemented shared character/structure eligibility, immutable resolved selections, policy-driven range/admission, and execution/AI pursuit consumption of validated candidates.
- Cannon direct aims exclude the actor cell and all eight neighbors, including diagonals. Legal splash retains indirect near-cell damage. Non-Cannon Single requires an eligible primary even when a hostile splash neighbor exists.
- Physically blocking structures are distinct from damage candidates; ineligible characters do not consume pierce encounters. ThornField structure occupancy still sees every living structure. Its immediate character damage policy remains Task 3.
- RED: 72 focused cases, 59 passed and 13 failed for the intended previous permissions after fixing the enemy automatic-command fixture number.
- GREEN: .NET focused suite 79/79 passed. Unity EditMode 158/158 passed (zero failed/skipped), including new admission tests, Task 1 tests, existing engine/category tests, targeting geometry/selection tests, and the existing multi-ray obstacle damage regression. Process exit code was 0.
- Coverage includes Player/Enemy ownership symmetry, Base/Rider/Heavy restrictions, authored neutral obstacles, Cannon empty/friendly aims, all nine forbidden center cells, boundary cells, AP affordability independence, zero-damage targets, pending movement/discount preservation, finite pierce budgets, multi-cell structure aggregation, and immutable selection results.
- Ruling: neutral character facts are forbidden by the existing constructor. Neutral policy coverage therefore uses native obstacle facts and explicit classification, rather than invalid character fixtures.
- Ruling: legacy non-Cannon SingleCell impact profiles do not define a pierce budget (Range=0). Non-Cannon pierce policy isolation is verified at CollectPierce with an explicit budget; this task does not change impact-category authoring or claim to add non-Cannon piercing attacks.
- Full legacy expectation migration and complete Core regression remain Task 4. Task 3 effect/consumer tests have not started. No commit or push was made.
- Independent read-only code review found no actionable issues. Generated FMOD scan fields were restored to their pre-test values.

## Task 3 verification record (2026-10-07)

- Immediate attack-triggered ThornField damage checks the projected occupant with `AttackTargetEligibility.CanTargetCharacter` and the resolved selection policy before creating a damage origin, applying damage, or publishing a hit/role. No new public API or category branch was added to consumers.
- Added 20 integration cases covering Base/Rider/Heavy allied secondary occupants with and without direct splash, Enemy ownership symmetry, hostile immediate damage and preview parity, Cannon allied damage and legal indirect near-cell damage, actor/dead-occupant exclusion, and allied structure occupancy.
- Independent environmental damage remains intact: persistent fields still hurt an entering actor, and a legal non-Cannon attack on a barrel can still cause allied explosion damage. Explosions retain the credited attacker, the barrel's physical source, and their separate attack group; immediate ThornField damage retains the actor credit and occupant-cell source.
- Added 12 AI/pursuit/KillChain cases: friendly-only directions cannot be selected by non-Cannon AI; mixed directions filter allies; Cannon collateral affects scoring; finite pierce range/falloff budgets agree with pursuit, arrival preview, and execution; Cannon permissions do not widen KillChain to allies or structures.
- Added three View cases for omitted empty/allied cell aims and a returned legal empty Cannon aim. Strengthened RecordInputOnly coverage. Tests exercise the existing router, interaction, and command-buffer path; View does not infer category or attack eligibility.
- RED: 18 integration cases, 8 passed and 10 failed for unwanted immediate allied ThornField damage. The persistent-field test was first corrected to supply the existing ThornMovementTileRule. GREEN: integration 20/20 and required AI/pursuit/KillChain suites 56/56 in the .NET harness.
- Unity EditMode: Core 190/190 passed, including all Task 3 suites, Task 1-2 policy/admission/engine/category suites, ThornField domain/rule tests, Knockback tests, and the complete ObstacleReactionTests fixture. Framework CombatInteractionTests independently passed 28/28. Both runs had zero failed/skipped tests and logged completion with exit code 0.
- Ruling: migrated only two existing AI scoring fixtures to explicit matching Cannon profiles so their original empty-primary and zero-damage allied-collateral purpose remains testable. Other legacy permission expectations and full Core regression remain Task 4.
- Independent read-only review found no actionable issues. Generated FMOD scan timestamp/path changes were restored after verifying the remaining asset content. No commit or push was made. Manual gameplay smoke checks and the complete Core suite remain unverified until Task 4.

## Task 4 verification record (2026-10-07)

- Migrated the legacy universal empty/friendly/dead-cell expectations into explicit Cannon-positive and Base/Rider/Heavy-negative cases. Added an optional matching category to the shared Input fixture and the local ActorEffect helper; both retain Base defaults.
- Unrelated clipping, AP, movement, refund, thorn refresh, overflow, mine immunity, and effect settlement tests now have legal hostile primaries. Intentional friendly damage uses Cannon. Existing geometry assertions remain unchanged.
- Kept the purpose of the structure footprint/death test by using an authored hostile barrel with zero explosion damage, avoiding enemy-headquarters victory while checking occupancy release. After a primary dies, LastStand retargets a living enemy: its second attack is 25 primary plus 17 splash damage. Automatic Base AI now advances two cells to land a 20-damage primary hit instead of selecting empty-center splash for 14 damage.
- Added eight real Core-to-View smoke cases in CombatInteractionTests. Actual GetAttackRange and Preview results drive the existing router/interaction, hover feedback, and submitted command; execution verifies the same targets, HP, and one AP charge. Cases cover Base empty/ally rejection, enemy/neutral authored obstacle success, Cannon empty/ally success and diagonal near rejection, and Unit_ShotGun mixed-direction hostile-only damage.
- Full initial Unity Core run: 1,472 total, 1,443 passed, 29 failed, zero skipped. These failures identified obsolete permissions or fixtures. Final full Unity Core run without a testFilter: **1,490/1,490 passed**, zero failed/skipped, exit 0. The complete pure .NET Core harness independently passed 1,490/1,490.
- Final related Unity Framework run: **250/250 passed**, zero failed/skipped, exit 0. Fixtures: CombatInteractionTests (36), CombatInputRouterTests (20), CombatInputPolicyTests (48), CombatPathInputCleanupTests (1), CombatPreviewPublicationTests (3), CombatCommandPublicationTests (29), CombatCommonPublicationTests (13), CoreStageCombatCycleTests (41), CombatFlowTests (37), and CombatMovementViewTests (22).
- Ruling: the registered path-cycle fixture used obsolete Drag input. Existing CombatPathInput requires a MoveMode request to begin planning and SelectPressed at the destination to commit; SelectReleased intentionally does nothing. The fixture now routes that sequence, retains every path/health/AP/checkpoint/return/attack/AI assertion, and is named RegisteredPathCycle. Its focused run also passed 1/1. No production input behavior was changed.
- Source audit found no targeting permission fields in Buffer and no dispersed category decisions in attack queries, execution, pursuit, or View targeting. Existing Capture, category snapshot, and bootstrap category mappings remain at their composition boundaries. Physical occupancy and environmental damage are intentionally distinct from eligible attack candidates.
- A fresh independent read-only review of all Tasks 1-4, including untracked scripts and metadata, found no actionable issues. `git diff --check` passed. No commit or push was made.
- Evidence: `.superpowers/sdd/2026-10-07-attack-target-policy/task4-results/` contains core-before.xml/log, core-final.xml/log, framework-final.xml/log, and focused cycle diagnostics/results. No real-scene manual/PlayMode smoke was run; the automated smoke does not claim scene presentation or authored Cannon asset acceptance.
