# Yonggeun Stage Merge — 2026-10-30

The user explicitly authorized merging `origin/feature/Yonggeun/Refector-Stage` into `feature/Cheon/Core/NewCombatSytem`, resolving conflicts, committing and pushing. This overrides the normal no-commit rule for this operation.

## Source checkpoints

- Ours: `98fd0c64251fe3584dec263c71d84b552e75923d` (accepted R0–R4 retirement/cost removal).
- Incoming: `dedaffab92b85032f86d47937969a446c38c6b18` (View/grid UX, new information/trait-slot UI, deck entry and authored trait content).
- Merge base: `7f4e1d9dcaa5d5248a635d23c7ad9fcd5b1789fb`.

There was no active merge at task start. The latest refs were fetched before starting a normal two-parent merge. Three pre-existing local FMOD/font modifications were byte-backed up and temporarily removed from the working tree; they are excluded from the merge commit and restored afterward. No force push, rebase, new Unity project or unrelated feature implementation.

## Resolution decisions

Seven files had textual conflicts. Each resolution preserves the relevant contracts from both parents:

- `CombatMovementCalculator`: retain incoming shared `Reach`/hover `UnitMoveCells`, remove automatically reintroduced obsolete funding/price calls. Ordinary movement keeps R4 distance/action admission; hover range reads Core movement rules without consuming/resetting actions.
- `CategoryTraitProgression_Heavy` and `TraitGrowthDefinitionTable`: accept View-owned authored steps/content, removing only retired DoubleShot/DoorBreaching references. The same retired references were removed from automatically merged Main content. Heavy/Main pools and the table retain23 valid definitions: Knockback plus22 incoming traits. This supersedes R1's temporary empty Heavy pool; no new tuning was invented during resolution.
- `CombatUnitInfo`: accept the incoming registered UI plus `CombatUnitInfoView` layout. The new layout has no legacy cost widgets; retain its required-reference, selection and UI-service lifetime contracts.
- `DeckEditUI`: retain new trait arrays/slots and required View data, together with R4's cost-free stat signatures.
- `UnitCardView`: keep the existing multiline null guard, incoming trait-slot behavior and cost-free detail signatures.
- `CombatSceneInput`: accept equivalent expression-bodied tracker clearing without restoring the removed cost-display callback/cache.

No retired products/abilities, budget types, affordability previews or pricing adapters were restored. Existing Core/Buffer authority, Framework services and publication transaction ownership remain. Upstream grid/overlap/hover, scene/prefab and deck/content changes were retained rather than replaced with a whole-branch side selection.

## Validation

Actual original-project Unity6000.0.73f1 tests ran sequentially through the AppData-enabled runner:

| Gate | Result | XML in `.superpowers/sdd/2026-10-30-yonggeun-stage-merge/` |
|---|---|---|
| Core | 4,042/4,042 | `merge-core-first.xml` |
| CoreIntegration | 350/351 | `merge-integration-first.xml` |
| Framework/View | 16/16 | `merge-view-first.xml` |

Zero skips. The sole integration failure is the unchanged pre-merge `BossStagePreparationTests.ProductionBossStage_PreparesAndPlacesBoss`: missing StageKey2101. Actual production catalog/authoring tests pass with the filtered new pools.

Three Core cases compare hover movement against ordinary distance ranges for zero/positive allowance and an already-attacked actor, checking unchanged action/state. Result-copy coverage now includes the new MoveCells array. Two obsolete legacy cost-widget layout cases were replaced by two owner/view reference-contract cases, with an additional real Combat scene validation of the new layout. Existing card numbers, targeting, pending/playback and end-turn assertions remain. No tests were skipped or blanket-deleted.

An independent read-only reviewer compared16 key files against both parents, checked the new23-entry pool references and test migration, and found no actionable resolution issue. Incoming Unity YAML whitespace and a historical handoff EOF blank line were identified as formatting-only upstream content; no global asset normalization was performed.

Manual/PlayMode acceptance remains for hover overlap/ranges, information-panel transitions/tooltips, deck departure and growth presentation. Automated tests do not establish visual acceptance. This merge does not claim to diagnose or fix the previously reported missing physical floor tiles.
