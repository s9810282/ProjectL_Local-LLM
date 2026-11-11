# Chapter Stage Repeat Reduction — Design (2026-11-06)

Status: approved on 2026-11-06. Both phases are implemented, and Chapter 1/2 enable facility repeat reduction for newly generated graphs. Related Core tests (191) and chapter integration tests (40) pass. Full Core retains 25 confirmed pre-existing failures; production DataBaseAssetTests retain 7 confirmed pre-existing failures. See the implementation plan for evidence and names. PlayMode/visual acceptance remains unverified.

## Intent and agreed behavior

Reduce consecutive occurrences of the same Shop, Recovery, or Enhancement (Training Ground) kind along actual traversable chapter connections. Consecutive Combat nodes remain allowed. This is a soft preference, not a generation constraint: unavoidable repetitions remain valid.

Different facility kinds may follow each other. This does not prohibit Shop -> Recovery -> Training Ground, repeated combat stage keys, or equal kinds on the same depth without a connection.

## Current behavior and ownership

`ChapterGraphGenerator.Generate` builds topology, places fixed nodes, assigns kinds using minimum counts and weighted draws, then selects stage keys. `AssignKinds` currently ignores graph connections. Minimum counts apply to the whole graph, not each playable route.

`StageGeneratorSystem` adapts immutable DB data into `ChapterGraphSpec` and returns generated progress data. Buffer owns installation and runtime progress; View consumes the published graph. Repeat reduction belongs inside the existing pure generation calculation, before stage-key selection.

## Approaches

1. **Selected: deterministic local improvement after kind assignment.** Swap kinds between eligible nodes when this reduces repetition. Preserve the topology and exact per-kind counts produced by the existing distribution algorithm. This is bounded and may leave a local minimum.
2. **Not selected: hard consecutive limits during assignment.** These require resolving conflicts with fixed placements, minimum counts, and depth eligibility, potentially through backtracking or generation failure. The user explicitly selected reduction rather than prohibition.

## Proposed calculation

The following details are implementation proposals, not additional user requirements.

Insert one reduction step between `AssignKinds` and `AssignStageKeys`:

1. Evaluate only existing directed connections between middle nodes. Include fixed middle nodes in evaluation, but never move their kinds or stage keys. Exclude the start and boss nodes.
2. For each kind with reduction enabled, count same-kind connections and same-kind three-node chains. The total score is the sum of both counts. A straight run of two nodes contributes 1, three contributes 3, and four contributes 5. The extra three-node count discourages longer streaks. No complete route enumeration is needed: each middle node contributes its same-kind incoming count multiplied by its same-kind outgoing count to the three-node-chain count.
3. Consider pairs of non-fixed middle nodes of different kinds in ascending node-ID order. A pair is eligible only when each kind meets its destination node's `MinDepth`. Minimum-count placements may move; they are not fixed placements.
4. Evaluate the whole graph after each temporary kind swap, then restore it. Choose the pair with the greatest strict score reduction; resolve equal improvements using the first pair in the stable order.
5. Apply that swap. Repeat until the score is zero, no pair improves it, or the number of accepted swaps reaches the number of movable middle nodes. This provides a finite work bound. No equal-score or worse swap is accepted.
6. Select stage keys through the existing stage-key RNG after the final kind arrangement, then retain the existing minimum-count assertion.

The score measures graph patterns, not visitation probabilities. All connections are considered, including merges and branches; player route prediction is not introduced. The score never increases, but individual paths or individual component counts can worsen while the total improves. Global optimality and elimination of all repeats are not promised.

## Authoring and compatibility

Add a single `ReduceConsecutive` boolean to `ChapterStageDistributionRule` and its corresponding serialized `DistributionInput.reduceConsecutive` field. A boolean is sufficient; no configurable penalty weights, transition matrix, or policy framework is needed.

- Preserve existing constructor calls with a trailing optional argument defaulting to `false`.
- With all flags false, skip reduction entirely and preserve the existing golden outputs and RNG call sequence.
- New SO defaults and the existing Chapter 1/2 map assets explicitly set Combat to false and Shop/Recovery/Enhancement to true.
- New SO defaults use Unity's `Reset` callback; serialized nested-field initializers remain false. A real legacy YAML import test confirmed that true field initializers incorrectly activate omitted fields, while this separation preserves both new-asset defaults and legacy compatibility.
- Other existing serialized inputs that omit the field remain disabled until deliberately enabled. Audit the map assets and test-owned authoring helpers for this distinction.
- The flag does not affect `IsActive`, minimum capacity validation, or relative draw weights.
- Enabling it can change kind positions and stage keys for the same seed. Topology, node IDs, connections, and exact per-kind counts remain unchanged relative to the disabled version with otherwise identical inputs.
- The reducer consumes no random numbers. Identical inputs remain deterministic. Existing installed graphs are not regenerated or migrated.

## Boundaries and change impact

| Area | Before | After / required understanding |
| --- | --- | --- |
| Kind assignment | Minimum counts, weights, depth eligibility, fixed placements | Same contracts plus graph adjacency and a local repetition score |
| Configuration | SO input -> immutable rule -> pool/spec | One boolean follows the same route; existing adapters already pass the rules |
| Framework / Buffer / Job / View | Existing graph creation, installation, navigation, presentation | No new service, registration, state owner, or published graph field |
| Verification | Graph, distribution, seed, authoring, navigation and integration contracts | Preserve those checks and add reduction behavior, disabled compatibility, and width-six cases |

Expected production changes are limited to `ChapterStageDistributionRule.cs`, `StageMapDataSO.cs`, `ChapterGraphGenerator.cs`, and the two existing chapter map assets. Prefer private calculation helpers in the generator; do not introduce a new service or interface for this one algorithm. `StageMapData`, pools, and `StageGeneratorSystem` already carry the immutable rule objects and should not need duplicated flags or policy logic.

This maintains SSOT (SO input and immutable rule), SRP (generation calculation versus installation and presentation), and existing public service contracts. No interface hierarchy changes are needed for OCP/LSP/ISP/DIP; behavior is selected through existing rule data rather than dependencies on facility implementations. Framework registration and lifecycle contracts remain unchanged.

The straightforward search costs O(M^2 * (V + E)) per accepted-swap round, with at most M rounds, where M is the number of movable nodes. Existing maps are small; width-six measurements are required before claiming acceptable runtime. Larger-map optimization is outside scope unless measurements expose a problem.

## Verification and acceptance

- Demonstrate an existing repeat-heavy case that improves, and an already clean graph that stays unchanged.
- Verify that combat-only graphs receive no penalty and mixed facility kinds are not treated as repetitions.
- Cover branches, merges, three-node runs, fixed nodes, late `MinDepth`, zero-weight minimum placements, and a graph with no legal improving swap.
- Compare enabled and disabled runs over a fixed seed set and every connection mode: identical topology and per-kind counts, non-increasing total repetition score, deterministic enabled output, and preserved fixed placements.
- Preserve the existing disabled golden outputs without rewriting expected values to accommodate regressions.
- Check authoring propagation and explicit Chapter 1/2 activation. Use test-owned inputs for algorithm and integration tests; production asset checks remain separate.
- Cover `widthMin = widthMax = 6`, including `singleFirstLayer`, and record generation cost. This does not add depth-specific width authoring or claim visual acceptance.
- Run the required existing Core and relevant CoreIntegration regression checks. Unity execution must use the approved AppData-access path described in AGENTS.md. Do not add View tests or run combat View MSBuild checks.
- If existing tests authored by others would require behavioral changes, follow AGENTS.md and ask before changing them. Disabled compatibility should avoid that need.
- Production asset edits require user-side `DataBaseAssetTests` and Unity map presentation checks. Compile, test, and PlayMode status must be reported separately.

## Scope and next review point

This is a small Core generation enhancement with authoring propagation, not a map topology or facility implementation redesign. Node widths, connection algorithms, camera framing, facility gameplay, and runtime graph ownership are outside this change.

Both approved phases are implemented. After reviewing completion, the user explicitly authorized a commit and push on the current feature branch. SO input and the two chapter assets are updated; View code, published graph contracts, and installed runtime graphs are unchanged. See `../plans/2026-11-06-chapter-stage-repeat-reduction-plan.md` for executed checks, existing failures, and the remaining user-side PlayMode/visual check.
