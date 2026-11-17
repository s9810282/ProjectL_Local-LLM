# Pull Range and Attack Continuation Design

Date: 2026-11-14
Status: Behavior confirmed; Phase 1 and Phase 2 source implementation authorized separately by the user and completed with static review. Owner runtime validation remains pending; see the plan for evidence. This document alone does not authorize Phase 3.
Revision: 2 — locality and single-path review, 2026-11-14.
Source baseline: `e4281a38e` plus the existing uncommitted Haboob continuation change in `PullAttackCalculator.cs` and `CombatAttackCalculator.cs`.

## 1. Intent and confirmed decisions

An environmental restriction is a normal combat outcome, not necessarily an invalid program state. Pull currently passes an environmentally reduced range of zero to a positive-range geometry helper. Its post-movement delivery also converts ordinary selection failures into an exception, except for a recent Haboob-specific branch.

The user confirmed these requirements:

1. When DustScreen reduces effective basic range to zero, Pull is unavailable normally, including its extra selection range. Do not pull to the attacker's cell or clamp the range to one.
2. Replace the Haboob-specific continuation exception with a common proceed/skip contract that reuses existing combat rules.
3. When the follow-up attack is skipped, skip its additional attack effects as well. Preserve completed Pull movement, environmental effects, and action-use settlement.
4. Prepare a design and phased plan before implementation, and discuss the proposal with the user.
5. For successful Pull as well, evaluate attack-start effects from the surviving state after Pull and arrival effects finish. Confirmed by the user on 2026-11-14.
6. Normal gameplay restrictions must not crash or fault the combat flow. In this document, a rejected/failed selection result is data returned normally, not a thrown exception. The user explicitly clarified this resilience goal on 2026-11-14.

The detailed lifecycle treatment below is the proposed interpretation of decision 3 and remains subject to document review.

## 2. Scope and constraints

- Unity 6000.0.73f1; existing pure C# Core assemblies and paths.
- Implementation documents are English; new code comments and XML documentation are Korean.
- No commits or pushes by Codex. Stop and report at every implementation phase boundary.
- Under the current project instructions, Codex does not author or modify test code, run MSBuild, or perform Unity compile/play/test validation. Core edit authorization does not waive these restrictions. Static inspection and owner-executed validation are separate evidence.
- Preserve public commands, results, Buffer ownership, Job completion, event formats, and normal attack policies.
- No View, scene, production asset, Factory registration, or Framework changes are planned.
- Existing unrelated changes and the earlier local Haboob fix must be preserved until explicitly replaced by the approved implementation.

The user requested applying [feature-change-locality-guidelines.md](<D:/My_ProjectWork/학교 관련 문서/졸업작품_프로젝트L/2학기 내용/AI 관련 문서/feature-change-locality-guidelines.md>) to these documents. Its criteria are applied to this design review; the attachment does not authorize product-code edits. The source file is unchanged. Sections 6 and 8 contain the project-specific decisions, so a receiving session can follow them even if the external file is unavailable on its machine.

In scope: effective-zero Pull range; post-Pull attacker/target/delivery revalidation; attack-start and attack-effect gating; movement-only result publication; focused and shared regression acceptance criteria.

Out of scope: ordinary movement cancellation after Reconstruction, new movement-rule registration architecture, changing initial Haboob designation rules, changing KillChain's independent targeting rules or Pull environmental-kill attribution, enabling deferred traits, and new UI rejection text.

## 3. Current owning code and evidence

All abbreviated Core paths below are relative to `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/`.

| Owner | Current responsibility / relevant observation |
|---|---|
| `Domain/Movement/EnvironmentTerrainRules.cs` | Computes effective range, including `Math.Max(0, range - DustScreen reduction)`. |
| `Domain/Traits/Rules/PullTraitRule.cs` | Positive-range selection/destination geometry. Direct zero-range calls currently throw. |
| `Contracts/Combat/CombatAttackProfile.cs` | Validates authored Pull range and targeting-method compatibility before combat. |
| `Application/Combat/PullAttackCalculator.cs` | Config validation, selection extension, movement execution, and currently delivery reconstruction. |
| `Application/Combat/CombatAttackQueries.cs` | Shared attacker readiness, `SelectionPreparation`, `SelectionSteps`, Haboob, range, occlusion, and delivery validation. Existing AI reach checks reuse preparations with reference-identity validation. |
| `Application/Combat/CombatAttackCalculator.cs` | Command admission, attack notifications, pre-hit movement, delivery, action settlement, and publication. |
| `Application/Combat/AttackDeliveryExecutor.cs` | Direct damage and attack follow-up ordering. LavaCannon currently runs even when direct delivery is disabled. |
| `Application/Combat/AttackSelectionEvaluation.cs` | Existing success/failure result with selection and blocked-contact evidence. |
| `Application/Combat/MovementProjection.cs` | Shared current characters/environment, arrival effects, and finalization. |

The operational Cowboy asset has basic range 1 and Pull extra range 2. DustScreen's base rank reduces range by 1, including allied units, and is referenced by the Rider progression and growth-definition table. This establishes a configured zero-range path; no runtime reproduction was performed.

`TraitAttackNotifications.Apply` currently runs before Pull movement. `SettleActionUse` combines success effects with marking the action used. `CombatDebuffApplication.CompleteAttack` also consumes the attacker's pending Intimidation, and `HunterApplication.FinishAction` consumes reserved Acceleration. These calls must be classified explicitly; disabling direct damage alone is insufficient.

## 4. Approach

Use the existing selection evaluation as the normal continuation decision. Add one shared post-movement query boundary, and let the existing command orchestrator decide whether the attack lifecycle starts. Pull continues to own Pull movement, while ordinary delivery preparation belongs to the command orchestrator.

This follows the user's requested reuse direction. Alternatives considered by the author:

| Approach | Assessment |
|---|---|
| Add more failure-reason branches inside Pull | Small immediate diff, but every new environmental restriction requires Pull-specific maintenance. Does not meet the requested direction. |
| Reuse selection results and introduce an explicit attack-start boundary | Recommended. Existing rules remain authoritative; expected refusal and invariant failure remain distinct. |
| Introduce pluggable exception/recovery handlers for all traits | Not justified by the current problem. Adds composition and recovery-policy ownership without eliminating the need for combat-stage decisions. |

No generic exception-catching layer, policy registry, duplicated character state, or persistent continuation state is introduced.

## 5. Effective range contract

Keep authored configuration validation and runtime range availability separate.

**No-crash acceptance rule:** A valid environmental reduction to zero must not throw `ArgumentOutOfRangeException`, fault the calculation, or enter the runtime fault boundary. Range queries return an empty result normally. A submitted attack returns a normal rejection, without movement, attack effects, or action consumption; combat remains available for subsequent legal commands. The word `Failure` in an existing result factory or enum describes this normal rejection, not a .NET exception.

- `CombatAttackProfile` continues to reject an authored Pull basic range below 1.
- `PullTraitRule.SelectionRange` and `Destination` keep their positive-range preconditions. Their existing invalid-argument checks remain meaningful.
- `PullAttackCalculator.SelectionRange` validates configuration first, then returns zero when the effective basic range is zero. Extra range does not restore availability. Otherwise it uses the existing geometry helper.
- `PullAttackCalculator.EvaluateSteps` returns `AttackFailureReason.InvalidPrimaryTarget` for effective basic range zero before calling `Destination`. It must not produce a movement path.
- Existing zero-radius masks produce empty range cells and candidate aims. `GetAttackRange`/unit-hover range queries may successfully return an empty range, consistent with their current query contract; this is not an executable attack.
- A correctly shaped attack command or preview against a target receives the existing normal selection failure (`InvalidPrimaryTarget`). Other malformed-input failures keep their existing precedence. No action, movement, attack notification, or attack effect occurs.
- No new public failure enum or Buffer field is required. General action availability remains distinct from the existence of a currently legal target.

The change is based on the resulting range, not on the presence of a DustScreen key. Future range modifiers therefore use the same contract.

## 6. Common continuation query

Add this internal entry point to `CombatAttackQueries.cs`:

```csharp
internal static IEnumerable<string> ContinuationSelectionSteps(
    CombatCommandInput input, CharacterCombatFacts actor, CombatAttackProfile profile,
    EntityId expectedTargetId, MovementEnvironmentFacts environment, WallGeometry walls,
    AttackElevation elevation, Action<AttackSelectionEvaluation> completed)
```

`input` is a completed observation of the current movement projection. The caller supplies the current actor and the original, non-empty target ID. Public-preview observations retain the existing observer/environment visibility policy; wall geometry remains the original public geometry, as in the current Pull implementation.

Evaluation order:

1. Validate API/configuration invariants. Missing required references, malformed internal aim, missing originally captured entity records, or inconsistent actor/profile identity remain errors. Do not catch exceptions from lower layers and turn them into skips.
2. The caller checks the existing `arrival.Terminal` before and after observing current facts and skips continuation if combat has ended. `BeginCurrentInput` can retain the original state's `Phase`, so `input.State.Phase` is not a replacement for projected terminal truth. Inside the query, reuse `IntrinsicUnitReadiness` for a dead/off-field/reconstructing/disarmed attacker or disabled basic attack. Do not create another outcome formula or re-run turn, command-admission, or action-cost settlement.
3. Read the original target from current facts. A dead target or a target that has left the field produces a normal failure. Check field/anchor availability before dereferencing the anchor.
4. Form the aim from that target's current anchor and create the existing `SelectionPreparation` for that exact current input/actor/profile/environment, with `includePreHitMovement: false`. This selects the current **basic** effective range and excludes pre-hit movement planning. Do not run a separate basic-range `ValidateAim` before the shared selection pipeline.
5. Invoke `SelectionSteps` once with that preparation. Its existing body owns shape/range validation, eligibility, Haboob, walls/elevation, and delivery restrictions. Its normal failure is returned unchanged, including blocked-contact evidence. The continuation preparation prevents Pull extra range and a second Pull evaluation; no selection body is copied into the continuation method.
6. On success, require the selected target ID to equal the captured target ID. A different selected identity remains an invariant exception with a precise message; it must not silently retarget.

The query returns the existing `AttackSelectionEvaluation`; no parallel success/failure enum is added. There is no Haboob-specific branch in Pull or the caller. Gameplay restrictions belong to the existing query owners. Unsupported configurations continue to fail at their existing validation boundaries.

### 6.1 Reuse the existing preparation, not a parallel selector

Extend the existing preparation contract with an immutable `IncludesPreHitMovement` boolean and an optional `bool includePreHitMovement = true` argument. Carry this optional argument through the existing `SelectionPreparation` constructor, `PrepareSelection`, `ReadSelectionParameters`, `CombatAttackQueries.SelectionRange`, and `PullAttackCalculator.SelectionRange`. Existing calls retain `true` and their current behavior.

- The preparation's policy, range, and stage choice are evaluated together against one observed input. `RequireSource` continues to reject reuse with a different input, actor, profile, or environment. A continuation creates a fresh preparation; it never reuses the AI's initial-selection cache.
- `PullAttackCalculator.SelectionRange` still validates its configuration in both modes. If no Pull configuration exists, return the basic range. With Pull, effective zero returns zero; `includePreHitMovement: false` returns the validated nonnegative basic range without extending it; `true` retains the existing positive-range helper. Do not introduce a second definition/status validator just for continuation.
- `SelectionSteps` uses the prepared range in its existing `SelectionCellSteps` call. Its existing final call to `PullAttackCalculator.EvaluateSteps` runs only when pre-hit movement is included; null preparation retains the original `true` behavior.
- The boolean describes two actual stages of the same selection contract. It is not a configurable bypass of Haboob, walls, target eligibility, or other rules. Do not add a flag per terrain or failure reason.

This replaces the draft's earlier two-pass proposal (a separate basic-range check followed by extended-range selection). Both stages use one selection algorithm; only the permitted range extension and pre-hit movement planning differ. No new class, interface, pipeline, or persistent state is required.

The first consumer is the post-Pull direct attack. Roar, KillChain, and unrelated future effects are not automatically routed through this query: their target/range contracts differ. Shared result semantics do not mean identical targeting rules.

## 7. Attack lifecycle and settlement

Use one command-local `attackStarted` decision. It means that readiness and delivery selection have succeeded and the attack lifecycle has begun. It does **not** mean that a hit dealt positive damage. A zero-damage attack or an attack interrupted later by reflection still started.

For commands requiring pre-hit Pull movement:

1. Create the existing empty-delivery plan and shared movement projection. Capturing the existing `AttackTraitScope` is read-only and does not itself spend Acceleration or send attack notifications.
2. Execute Pull, resolve its arrival/environment effects, and preserve its movement and pre-hit damage records.
3. Respect existing movement outcomes: friendly movement-only Pull, interrupted movement, death, or terminal combat do not begin a follow-up attack.
4. If movement permits a follow-up, observe current facts and run `ContinuationSelectionSteps`.
5. On refusal, retain completed movement and the empty delivery. Set `attackStarted = false`; proceed to command finalization.
6. On success, set `attackStarted = true`, emit the attack-start notifications once, refresh actor/input facts after those notifications, then prepare damage and delivery using the validated selection. Current notifications grant attack stats only; selection geometry and target identity are not changed by them.

For ordinary attacks without pre-hit movement, keep the existing successful selection and notification order. They enter the same started-attack delivery path. Preserve captured enemy-count and Acceleration scope rules unless explicitly described here.

This deliberately changes notification timing for successful Pull as well: Vanguard/CleanSweep sources and recipients are evaluated after arrival, so a source killed by a pre-hit mine does not grant a new attack-start bonus. The old behavior could grant that bonus before the source died. The user confirmed this timing on 2026-11-14; it is an intentional behavior change, not a claim of behavior equivalence. Do not simulate notifications and roll them back on refusal.

Move delivery reconstruction out of `PullAttackCalculator` into a private helper in `CombatAttackCalculator`, used by the started-attack paths:

```csharp
private static IEnumerable<string> PrepareDeliverySteps(
    CommandAttackPlan originalPlan, CommandAttackProjection world,
    CombatCommandInput currentInput, ResolvedAttackSelection selection,
    AttackElevation elevation, Action<CommandAttackPlan> completed)
```

The helper reuses `CommonAttackDamage`, `CombatAttackDelivery.ResolveSteps`, and existing plan construction. It preserves the original command input, requested cells, action expectations, scope, and attack group while using current actor/environment facts for damage. It does not choose whether to start the attack. Move the existing delivery-preparation logic rather than copying it: remove `PullAttackCalculator.RebuildDeliverySteps` after its only caller migrates, and replace the corresponding inline ordinary-attack preparation in `CombatAttackCalculator` with the same helper. An empty placeholder plan before Pull is permitted; duplicate delivery preparation against the same facts is not. Preserve legitimate current-stat recalculation between ordered hits and initial-versus-post-movement revalidation; these observe different stages and are not duplicate rule implementations.

### 7.1 Stage ownership matrix

| Operation | Follow-up skipped before attack start | Attack started |
|---|---|---|
| Completed Pull movement, terrain/object effects, pre-hit damage | Preserve | Preserve |
| Environmental death/reconstruction, kill rewards and normal battle finalization | Preserve through existing owners | Preserve through existing owners |
| Attack ordinal, Vanguard/CleanSweep notifications | Do not increment/emit | Existing behavior, exactly once |
| Common attack damage, direct hits, reflection, on-hit effects, attack knockback | Do not execute | Existing behavior |
| LavaCannon and other attack-associated effects | Do not execute | Preserve zero-damage/zero-percent splash and reflection semantics |
| Attack-success trait/category callbacks | Do not execute | Existing behavior |
| `CombatDebuffApplication.CompleteAttack`, including Intimidation consumption | Do not execute | Existing behavior |
| Attack-completed item callbacks | Do not execute | Existing behavior |
| Attack-scope Acceleration consumption / `HunterApplication.FinishAction` | Do not execute; discard the unused transient scope | Existing behavior |
| Hunter/presence maintenance caused by movement, damage, or death | Preserve under those existing owners | Preserve |
| Action-use settlement for an accepted ordinary command | Mark used exactly once | Mark used exactly once |
| Reaction-command action cost | No ordinary action-use settlement | Preserve existing reaction semantics |

Split the current private `SettleActionUse` responsibilities into `ApplyAttackSucceededEffects` and `SettleActionUse`. The latter only performs ordinary action-use mutation. The existing executor callback remains the one settlement point.

Rename the executor's internal `deliverDirectDamage` parameter to `attackStarted`, preserving its default for existing normal callers. Gate all attack-associated work, including LavaCannon and attack-scope completion, with this stage decision. Keep existing reflection-specific conditions inside the started branch.

The existing KillChain policy is preserved: a movement-only Pull does not start an attack-linked KillChain. Death/reward settlement from the environment is still performed. Changing attribution or granting a new chain for Pull environmental kills is a separate proposal.

### 7.2 Publication and preview

Preserve `AttackPullSnapshot`, pre-hit health evidence, final character positions, action-use settlement, and the existing accepted result/event envelope. Movement-only results have no direct hits or attack affected cells. The existing `CombatAttackEvent` envelope remains for compatibility; it does not itself authorize attack effects.

View already recognizes `Pull != null` with no direct hits as movement-only. No new View branch or public skip-reason field is planned. Expected reasons remain available to the internal decision and verification. Exposing them to players would be a separate contract change.

Public preview and actual execution use the same continuation/lifecycle boundary. Existing actual-only effects and hidden-environment policies remain distinct; this change must not claim identical results when the existing visibility contract intentionally differs.

## 8. Change independence and architecture checks

The source review confirmed the current ownership/call relationships below. Improvements in edit/understanding scope are design expectations until implementation and acceptance, not measured results.

| Actual change case | Current burden and cause | Minimum change and expected edit/understanding scope | Verification that remains necessary |
|---|---|---|---|
| DustScreen makes effective range zero | `EnvironmentTerrainRules.AppointRange` allows zero; the Pull adapter forwards it to positive-range geometry. A modifier author must know an unrelated helper's precondition. | Keep the environmental formula and geometry helper. Change the two Pull adapter boundaries only. The modifier's contract remains nonnegative effective range; no DustScreen-specific check is introduced. | All shared range consumers: normal/Pull range, unit-hover range, preview, command, AI, and rank-based exemption. These share range rules, so this coverage is necessary. |
| The pulled target becomes impossible to attack | `RebuildDeliverySteps` mixes current observation, target reselection, a named Haboob exception, damage calculation, and plan reconstruction. The first draft also specified a separate geometry pass. | Keep one `SelectionSteps` body and reuse its preparation/result. The continuation wrapper owns current-actor/target checks and stage choice only. Pull stops owning ordinary delivery reconstruction. A Haboob change stays in the existing designation rule. | Initial vs continuation selection, existing AI preparations, target identity, visibility, walls/elevation, and no repeat Pull. Their shared selection contract makes this coverage necessary. |
| A skipped attack must not trigger bonuses or completion effects | `SettleActionUse` combines success effects and action use; `AttackDeliveryExecutor` and command completion use different gates. Timing is a real dependency, while combining effects with action usage creates unnecessary coupling. | Split the two existing private responsibilities, preserve the single settlement callback, and gate existing effect owners using the same command-local attack-start decision. No extra dispatcher or feature-specific Bootstrap is added. | Pull/non-Pull/reaction paths, attack counters, actual vs skipped attack effects, action use, ledger, and publication. These share lifecycle state and ordering; reducing this verification would be unsafe. |

### 8.1 Ownership and migration proof required at review

| Responsibility | Authoritative implementation after the change | Evidence required from the implementation diff |
|---|---|---|
| Effective range and environmental reduction | `EnvironmentTerrainRules` plus the existing Pull range adapter | No copied DustScreen/Sandstorm formula; configuration validation still precedes the zero-range return. |
| Selection geometry, designation, eligibility, and occlusion | Existing `CombatAttackQueries.SelectionSteps` and its current rule callees | Initial and continuation calls reach the same body; no independent mask/Haboob/wall/target-filter loop exists in the new wrapper. |
| Stage-specific range and pre-hit planning | Existing `SelectionPreparation` / `ReadSelectionParameters` | One explicit immutable stage choice; source identity checks remain; existing AI calls default to initial selection. |
| Damage/delivery preparation | Existing orchestrator's private `PrepareDeliverySteps` | Old Pull rebuild method removed and old ordinary inline implementation replaced. Both started paths call the helper; skipped paths do not. |
| Damage, displacement, and attack-associated effects | Existing `AttackDeliveryExecutor` and existing effect owners | Every branch consumes the orchestrator's `attackStarted` decision; no separate per-feature start flag or copied effect rule is added. |
| Ordinary action use | Existing executor callback calling the command's `SettleActionUse` | Exactly one mutation/settlement path for accepted ordinary commands, including movement-only results. Success effects are not a prerequisite for settlement. |
| World facts, terminal outcome, damage ledger, finalization, and publication | Existing shared `CommandAttackProjection`, `MovementProjection.Terminal` / `BattleOutcomeCalculator`, `CommandAttackLedger`, `AttackResultFactory`, and Buffer/Job path | No second mutable world, phase-derived replacement outcome, replacement ledger, manual Buffer write, or alternate completion path. |

Creating a helper is allowed only when it consolidates an existing responsibility or expresses the specified stage boundary. A helper name, smaller file, or identical return type is not evidence of reuse. Follow its callers through to the owning rule and record which old implementation was replaced. Keep ordinary runtime orchestration in `CombatAttackCalculator`; it is not Factory composition that needs to be extracted.

The movement callback's ability to attempt a follow-up and the final `attackStarted` decision describe different stages. Neither becomes persistent state, and the final decision is made once. Immutable current-input observations remain snapshots of the existing projection, not independently synchronized state owners.

SSOT: characters and environment stay in the shared projection, published through existing command/Buffer ownership. Continuation evaluation is transient. No duplicate costs, state stores, or Framework services.

SRP: Pull owns Pull geometry/movement; queries own legality; the command owns stage ordering and settlement; the executor owns attack delivery effects. OCP: new gameplay failure reasons do not require Pull branches. LSP: refusal remains a normal result while invalid inputs remain exceptions. ISP/DIP: reuse existing narrow facts/query/result contracts; no new service registration is needed.

Understanding scope is reduced by making the boundary explicit, not by avoiding shared dependencies. Verification must still cover range consumers, preview, actual commands, AI, attack-start effects, action settlement, and result publication.

No new C# type/file, public result field, service, registry, generic recovery interface, or parallel execution pipeline is planned. A discovered need for any of these must be reported with an actual change case and its edit/understanding/verification impact before extending the approved scope. Do not route around a difficult existing contract to make a local check pass.

## 9. Acceptance and review boundaries

- Zero-range source example: Cowboy range 1, extra 2, base DustScreen reduction 1. Range and AI candidates are empty; preview/command refuse normally; no movement/effects/action usage occurs.
- Positive-range control: Sandstorm 3 -> 2 and ordinary Pull still use the existing effective destination and damage rules.
- Haboob controls: same-side membership at initial selection and destination allows attack; a legal initial selection whose destination crosses membership yields movement only. Initial cross-boundary designation remains rejected.
- A current attacker that becomes reconstructing during pre-hit environmental damage does not start the attack. A reconstructing **target** remains targetable under existing rules.
- Successful Pull begins its attack notifications once after arrival. Skipped Pull does not change attack counters, Vanguard/CleanSweep grants, Acceleration, pending Intimidation, or attack-completed item state merely because an attack was requested.
- Pull with LavaCannon and a refused continuation adds no LavaCannon status; a started attack retains existing reflection and zero-percent-splash LavaCannon behavior.
- Movement/environment damage, death rewards, and command completion still publish consistently with no double action use.
- Config/state mismatch and wrong target identity remain errors. No catch-all conversion to success or skip is accepted.

Owner validation must include existing Pull, Haboob, terrain/trail, Reconstruction, attack-trait lifecycle, item, and CoreIntegration publication suites, plus the cases above. Existing tests are evidence of coverage intent only until executed. In particular, `PullRangeTests.InvalidConfiguration_RejectsWithoutOrdinaryAttackFallback` must retain the positive-range helper's argument validation.

Review this design and its lifecycle interpretation before implementation. The companion plan is provisional until this document is accepted. No additional product code was changed while preparing this draft.

Builder check (2026-11-14): an independent read-only review found no additional material planning questions, public-contract changes, or conflict with the test/compile restrictions. The user subsequently confirmed the successful-Pull notification timing in section 7. No behavioral question raised during drafting remains unanswered. This review is not compilation or execution evidence, and the timing decision alone does not authorize implementation.

Locality review revision (2026-11-14): the user's guideline prompted a second source-level review. It identified unnecessary two-pass selection and insufficiently explicit removal/ownership criteria in the first draft. Sections 6.1 and 8.1 now replace that proposal with the existing preparation/pipeline and require caller-to-owner migration evidence. The earlier builder check did not establish that the first draft was optimal; this revision is still a design, not executed code.

Independent revision-2 review found no material additional architecture issue. Its source-level clarification about observed `Phase` versus `MovementProjection.Terminal` was incorporated into section 6. No compile, play, or test result is implied by this review.
