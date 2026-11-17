# Combat Health Execution Evidence Design

**Status:** H1–H3 implementation is recorded in the plan. H4 verification and remaining acceptance checks are recorded in §13.

**Date:** 2026-11-12.

**Task plan:** [Combat Health Execution Evidence Implementation Plan](../plans/2026-11-12-combat-health-execution-evidence-plan.md), prepared at the user's request. This authorizes planning, not implementation of the phases.

**Scope:** Core health execution and validation: actual HP loss, recovery, cap normalization, and causal evidence. Preserve the current View event stream and gameplay rules.

**Basis:** [Boundary handoff §5.88](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#588-shared-health-evidence-and-lethal-reflection-reconstruction-2026-11-12-user-authorized-core-change) and [the completed shared-interpreter change](../plans/2026-11-12-combat-health-evidence-consistency.md). Their runtime verification is still pending.

## 1. Problem and intended outcome

Two investigated failures illustrate different forms of context loss:

1. Ground Compaction departure evidence depended on an outer `Move` request even though AI composition restores a `CompleteEnemyTurn` request. The implemented fix reads the actual movement event instead. This design preserves that fix; it does not redesign movement rules.
2. Lethal reflection followed by Reconstruction records actual loss, but the visible character event contains pre-hit HP and restored HP. The implemented common interpreter now reads the same event consistently for attack, artifact, and healing checks.

The second fix still reconstructs health operations from event kinds. Moreover, artifact/item checks can accept sufficient HP change elsewhere in the same recipient's result stream. Consistent arithmetic does not prove that the claimed effect produced that change.

**Target:** calculate once, record the executed operation once, and validate the relationships between that record, its claims, and the proposed state. A new trait using an existing health operation must not require a new trait-specific health-validation branch. An operation with genuinely new semantics must define those semantics at its owning boundary; this design cannot safely infer an undefined future rule.

## 2. Current owning sources

All paths below are relative to `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/` unless stated otherwise.

| Source | Current responsibility / relevant limit |
|---|---|
| `Application/Combat/CombatDamageApplication.cs` | Applies numeric damage, item reactions, reconstruction, and direct defeat; already knows actual loss and recovery. Owns command-local damage executions and derived stage/recovery totals. |
| `Contracts/Combat/MovementEffectReceipt.cs` | `MovementDamageExecution` preserves exact evidence events and movement uses. Its constructor is internal to the Core application through the existing friend-assembly boundary. |
| `Contracts/Combat/CombatCommandTransition.cs` | Carries execution evidence alongside proposed changes; copy methods must retain it. |
| `Application/Combat/CombatCommandComposition.cs` | Merges child transitions and preserves event/execution object references. Rejects more than one ordinary action settlement per publication. |
| `Contracts/Combat/CombatChangeEvidenceReader.cs` | Classifies events and validates health claims. Its new private interpreter gives consistent amounts, but recipient-wide amount checks do not establish cause. |
| `Application/Combat/MovementUndoPolicy.cs` | Consumes change evidence and numeric movement uses; must not become a second health-rule engine. |
| `Application/Artifact/ThornHeartAttackScope.cs` | Uses the actual damage outcome for the artifact's applied amount; inserts the artifact event around the same execution. |
| `Application/Combat/CommandAttackLedger.cs` | Captures attack scopes and actual recovery at defined cut points. Those cut points must survive migration. |
| `Contracts/Combat/CombatResult.cs`, `CombatEvent.cs` | Published View result and presentation event contracts. Keep event kinds, values, ordering, and reconstruction presentation unchanged. |

Buffer remains the authoritative installed state. In `Systems/RuntimeSystem/BufferSystems/Combat/CombatStateBuffer.cs`, publication delegates existing Core validation before installation. `CombatTransitionPublisher` adds completion information and does not recalculate damage or healing.

## 3. Approaches considered

| Approach | Benefit | Cost / decision |
|---|---|---|
| Keep only the shared event interpreter | Smallest current change; already fixes reflection/reconstruction arithmetic | Cannot distinguish two causes acting on the same target; every novel composite event still needs interpretation. Keep as the current baseline, not the final causal contract. |
| Add execution receipts directly to the View-facing events | Receipts travel with the event and could later simplify View numeric display | Expands shared presentation constructors and consumers; unnecessary for the current Core validation objective. Not recommended for this phase. |
| Evolve the existing Core execution-evidence record, carried by `CombatCommandTransition` | Uses an existing boundary, preserves View events, and supports exact claim-to-execution links | Requires migration of all health producers, composition, and publication consumers. **Recommended for review.** |

Do not implement a parallel mutable health ledger alongside `MovementDamageExecution`. Evolve/replace that narrow execution record with a health-capable record, retaining movement-use evidence. If a compatibility property is temporarily needed, it must be a projection of the same records, never an independently populated list.

## 4. Proposed contract

Names in this section describe proposed roles; the Task plan will settle exact signatures after this design is approved.

### 4.1 Immutable execution record

Evolve `MovementDamageExecution` into a Core-owned `CombatHealthExecution` record. It contains:

- Ordered immutable health steps for one actual invocation, potentially involving multiple recipients when the existing operation requires that.
- The exact existing evidence event references and numeric movement uses associated with that invocation.
- Explicit execution/source context using existing `EffectOriginKey`, `CombatDamageOrigin`, and the owning operation's lifecycle/growth context where applicable. Do not manufacture an attacker for a non-attack operation.

Each health step contains its recipient, before/after health snapshots, actual HP loss, actual recovery, normalization loss, and the source/context of that step. Preserve maximum HP as well as current HP so cap adjustment is not confused with healing. Numeric damage and direct defeat retain their existing cause distinction. Requested damage and configured healing magnitudes are not substitutes for actual applied amounts.

Constructors validate nonnegative amounts and conservation with wide arithmetic:

`after.CurrentHP = before.CurrentHP - actualLoss - normalizationLoss + actualRecovery`

An ordered record may represent Reconstruction as a damage step followed by a restoration step, with the latter's own effect origin. It must not infer those two operations later from the final HP difference. Partial healing, overheal clamping, full absorption, and a maximum-HP-only change are explicit outcomes of their existing owners.

Actual variables already computed by the execution boundary populate the records. Do not run trait rules, target selection, or damage formulas again to produce evidence. Existing stage totals and recovery counters must use these same operation values; retain their established accounting semantics and scope cut points.

### 4.2 Claim-to-execution association

Carry a small immutable association in the Core transition between an existing claim occurrence and the specific execution steps that support it. The association contains references, not another copy of the HP amounts or character state.

- A claim occurrence identifies the exact existing event plus the entry/hit index when that event contains multiple artifact activations, traits, or attack hits. Recipient identity is checked against that selected entry.
- The association references canonical execution steps. A matching target, amount, trait key, or attack-group number by itself is insufficient.
- Reflection can claim only its applied loss steps; reconstruction restoration is not extra reflection damage. An item heal cannot borrow another effect's restoration on the same unit.
- Attack claims use the same scopes and capture points as `CommandAttackLedger`; damage, recovery, and normalization totals remain separate and exact.
- Do not globally interpret `TraitActivationSnapshot.Magnitude` as actual HP change. Each existing health-claim adapter must preserve its defined meaning: exact actual amount where the producer supplies it, or the existing occurrence/positive-recovery guarantee where magnitude is not such an amount.
- A step can support related summaries, such as an attack hit and a specific effect, but a single claim must not count the same step twice. Shared summary support is not duplicate execution.

Use immutable record/reference identity within the existing in-memory transition, as current evidence already does. AI input numbers can be zero, and child phases can reuse local ordinals; do not derive execution identity from those values. Merge preserves the same instances. Cross-process serialization or persistent replay IDs are outside this design and would need an explicit identity contract.

### 4.3 Zero-effect execution

An executed attack may have zero HP loss because of shields, held defense, or zero damage. Preserve its real execution/movement-use evidence without inventing a positive health change. A rejected action or preview must not produce publishable executed claims. No-op health outcomes may be retained where needed to distinguish execution from absence; they cannot satisfy a positive-loss or positive-recovery claim.

## 5. Ownership and data flow

```text
Existing rule / operation owner
    -> computes and applies the result to the existing command projection
    -> seals actual health execution facts + existing presentation events
    -> associates effect / attack claims with the exact facts
CombatCommandTransition + composition
    -> preserves immutable references and operation order
Core validation
    -> checks completeness, conservation, scope, source links and proposed state
Existing Buffer transaction
    -> installs the accepted changes atomically
Existing CombatResult.Events
    -> View playback unchanged
```

There is no new Framework system/service or global registry. Temporary construction belongs to the existing command projection; sealed receipts are evidence, not another state authority. A compact stateless Core validator may be separated if needed to keep health validation out of movement undo, but it must not duplicate formulas or introduce a service interface.

## 6. Producer coverage and migration boundary

Migrating only `CombatDamageApplication.Apply` would leave legitimate health paths uncovered. The Task plan must account for these observed paths before strict publication validation is enabled:

| Producer family | Existing owning code | Required migration |
|---|---|---|
| Numeric damage, reflection, item post-damage healing, Reconstruction, direct defeat | `CombatDamageApplication`, `ThornHeartAttackScope`, `AttackDirectDamageCalculator` | Seal actual loss/restoration/item-recovery steps with distinct causal links; preserve shield and movement-use evidence. |
| Regeneration and kill-triggered recovery | `RegenerationTurnApplication`, `TraitKillSettlement` | Record actual clamped recovery and link the matching trait occurrence. |
| Movement/environment damage or recovery, soul arrival | `MovementProjection`, existing object/soul owners | Use the actual operation result, including health changes carried by soul events; no requirement to invent a new View health event. |
| Position/cap cleanup and growth changes | `PositionTileModifierCalculator`, `CombatExperienceSettlement`, soul-growth/completion paths | Record max-HP transitions and distinguish normalization from recovery. |
| Battle-completion artifact healing | `BattleCompletionCalculator` and existing artifact owner | Keep completion timing and use the actual heal result; preserve facts through `WithCompletion`. |
| Structures and obstacle chains | `MovementProjection.MergeStructure`, `ObstacleReactionCalculator` and their callers | Distinguish actual health changes from movement-only structure events and preserve causal origin. |
| Direct stage outcomes: railway, rockfall, fall/environment death | `RailwayGimmickRule`, `RockfallGimmickRule`, `MovementProjection` | Preserve each rule's current damage/defeat/reconstruction semantics instead of rerouting it through a damage policy that changes gameplay. |

Registration remains ordinary existing assembly/composition work. Contracts already grant the Core application internal construction access; keep that boundary. Avoid public builders that let unrelated consumers fabricate authoritative execution facts.

## 7. Validation responsibilities

Validation checks executed facts, not whether a particular trait is on an allowlist:

1. Every submitted health claim resolves to execution steps in the same transition and the right recipient/source scope.
2. Step amounts conserve health, their sequence agrees with the corresponding before/after facts, and the proposed final state is consistent with all relevant steps. Maximum-HP changes and normalization retain their own meaning.
3. Every actual health mutation covered by this migration has execution evidence, including completion, soul/growth, and structure paths. Evidence-event references must belong to the submitted result.
4. Copy, merge, reaction composition, final reconciliation, and publication preserve claim/execution identity and ordering. Revalidation is idempotent.
5. Missing, duplicate, wrong-recipient, foreign-step, or inconsistent evidence fails before Buffer installation. Do not catch and ignore such failures, skip validation for AI, or reopen undo after a consumed effect.
6. Error messages identify the operation/claim type, recipient, and relevant source context. They must not substitute logging for a valid contract.

Existing event classification still recognizes supported event types. This work removes per-trait health interpretation from consumers; it does not remove legitimate registration for genuinely new event contracts.

## 8. View compatibility

Keep `CombatResult.Events`, `CombatCharacterEvent.Expected/Final`, `Reconstructed`, event ordering, and result/preview fields unchanged. The execution records and claim associations stop at the Core transition/publication boundary; View does not need to consume them in this phase.

`CombatCharacterHits` currently uses Reconstruction to derive displayed damage and to apply final facts after hit reaction. Preserve that behavior. Converting Reconstruction into separate visible death/heal events would change playback and is prohibited by this design.

Adding a field in Core Contracts can still affect compilation of constructors and fixtures even if View event APIs are stable. Verify actual callers; do not equate no planned View edits with a verified build. Exposing actual amounts for View display later is a separate, additive consumer migration.

## 9. Change independence and design principles

| Scenario | Before | Intended after | Necessary verification remains |
|---|---|---|---|
| Add a trait using ordinary damage/healing | Understand its execution path plus how multiple validators reconstruct outcomes | Understand the trait rule and the existing operation/receipt contract; no new health-consumer trait branch | Trait behavior, real combinations, publication, required Core/CoreIntegration regression |
| Add a composite health operation | Teach consumers how an event compresses multiple operations | Emit the supported primitive steps at the owner and bind claims; consumers use the same facts | Conservation, causal links, operation ordering, composition and final-state consistency |
| Change AI wrapper/composition | Risk using outer input to recover an inner action | Preserve immutable execution/claim identities; wrapper kind does not redefine health meaning | Direct/prepared AI parity, revalidation and stale-input rejection |
| Add a new primitive or published presentation event | Implicitly change multiple consumers | Make the new contract explicit at the common boundary, with normal registration changes | All affected consumers and View compatibility when its contract changes |

- **SSOT / SRP:** Buffer owns state, operation owners own rules, one execution record owns executed facts, validators own consistency checks, and View owns presentation.
- **OCP / LSP:** new traits reuse supported operations; every producer must satisfy the same receipt contract, including zero and composite outcomes. Novel semantics require an explicit contract extension rather than an exception in each validator.
- **ISP / DIP:** consumers receive immutable facts and narrow claim links; no new dependency from pure Core contracts to Unity, View, Buffer implementations, or Framework services.
- Together-changing receipt interpretation belongs at one boundary. Independent feature rules stay with their existing owners. More files/classes are not the success criterion.

## 10. Proposed phases and review gates

These are design-level phases, not authorization to implement them. Write the Task plan only after design approval; stop and report at each implementation phase as required by the repository instructions.

| Phase | Deliverable | Gate |
|---|---|---|
| H1: Contract and propagation | Finalize canonical execution/step/claim shapes; migrate the existing execution carrier and all copy/merge paths; no new gameplay behavior | Review identity, ownership, compatibility and a complete producer/claim matrix. Do not enable strict incomplete coverage. |
| H2: Health producers and claims | Migrate the observed producer families and claim adapters, retaining existing rule ownership and displayed events | Review completeness and concrete compound scenarios. A missing producer is unfinished migration, not a permanent fallback. |
| H3: Validation cutover | Consume actual execution facts and causal links; retire authoritative event-delta inference and redundant execution storage | Strict validation only after all producers/consumers are connected; verify atomic rejection, scope totals and idempotence. |
| H4: Integration handoff | Verify the original defects, existing regressions and unchanged player-build presentation | User Unity/build verification and results recorded; no completion claim based only on static review. |

Intermediate compatibility must be clearly phase-scoped. Do not permanently allow missing receipts to fall back to recipient-wide event sums. Keep the current safe common-interpreter baseline until the migration can be cut over coherently.

## 11. Acceptance and test constraints

The required evidence includes:

- Ground Compaction enemy departure through direct and prepared AI, retaining the existing fix.
- Lethal reflection with Reconstruction at low/full HP and with shields; nonlethal/lethal ordinary reflection and full absorption.
- Same recipient receiving unrelated healing/damage in one result: a claim cannot borrow those steps; repeated same-source invocations remain distinct.
- Ordinary and chained attacks, redirection, reaction attacks, item healing, regeneration, souls, growth/cap changes, structures, hazards, and completion.
- Correct distinction between actual loss, requested/absorbed damage, restoration, normalization, and direct defeat; preserve current game-specific accounting.
- Foreign/missing/duplicate steps, wrong recipients/sources, copy/merge identity, zero effects, previews/rejections, stale publication, and Buffer atomicity.
- Unchanged View event order, damage display, Reconstruction reaction timing, and final state refresh.

Current task restrictions remain: no test authoring, no Unity/MSBuild runs, no commits/push. Existing test changes are limited by the user's fixture ownership and compile/runtime rules. If migration requires changes outside those permissions, identify the exact tests and required changes and obtain authorization before that phase; do not remove coverage or mark integration complete without it. User-run existing Core/CoreIntegration suites and Unity/player-build checks remain necessary.

## 12. Original design review record

- Source-grounded draft only; no production code changes belong to this design task.
- Independent builder review found no important unresolved question preventing Task planning. This is a design-readiness result, not approval or runtime verification. Referenced local documents were checked to exist.
- The already implemented Ground Compaction and shared health-interpreter fixes remain unchanged; their runtime checks are still pending.
- The user requested the H1-H4 Task plan; the linked plan uses the recommended Core-transition approach and preserves View events. Implementation still requires phase authorization.
- The Task plan specifies type/property names, claim selectors, compatibility migration, and the fixture-authorization gate. H1 must finish the source/fixture inventory before the relevant code changes. These details are not permission to add a second state authority or change gameplay rules.

## 13. Implementation and verification status

H1–H3 are implemented on the current branch. H4 Unity compilation passed, and the authorized fixture repairs resolved 20 failures. The full rerun returned 6,592 passed, 36 failed, and 11 ignored. The [execution plan](../plans/2026-11-12-combat-health-execution-evidence-plan.md) records the runs and coverage. The [boundary handoff §5.92](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#592-health-evidence-integration-verification-h4-2026-11-12) records how producers extend the implemented contract and what evidence still gates acceptance.

The authoritative reader is `CombatChangeEvidenceReader.ReadAll(CombatCommandTransition)`. The existing reconciliation and Buffer publication paths call it before installation. The validator receives canonical execution and claim references. `ReadEventShapes` remains available for standalone event fixtures. The H1 compatibility type, property, and constructor adapter were removed in H3.
