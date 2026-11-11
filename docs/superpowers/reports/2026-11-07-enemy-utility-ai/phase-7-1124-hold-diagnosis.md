# Phase 7 stage 1124 no-action diagnosis

Run date 2026-11-08. Checkout `feature/Cheon/Core/NewCombatSytem` at `9d8c9ce45` with the existing Phase 7 worktree changes. Behavior traces came from Unity 6000.0.73f1 Play Mode. The temporary trace records shortlist counts, admission, score components, effect coverage and published results. Timing in these diagnostic runs is outside the performance comparison.

## Observations

All cases load stage 1124 with its authored terrain and gimmicks, seed 1, five enemies and only the Main character deployed on the player side. The adjacent case moves one Demon from its authored cell (3,5) to (4,3) in a copied runtime definition and places Main at (4,2). The movement control uses the original enemy placements and submits actual player move commands through the View buffer, including move preview and normal Core settlement.

| Case | Attack candidates | Move candidates | Public admission failures | Candidates above Hold | Unit actions published |
|---|---:|---:|---:|---:|---:|
| Authored opening, no player move, side 1 | 0 | 20 | 0 | 0 | 0 |
| Authored opening, no player move, side 2 | 0 | 20 | 0 | 0 | 0 |
| Adjacent Demon, one side | 1 | 20 | 0 | 0 | 0 |
| Authored opening, Main moved to (4,2), side 1 | 0 | 20 | 0 | 0 | 0 |
| Authored opening, tornado relocated Main to (0,6), side 2 | 0 | 20 | 0 | 0 | 0 |

The initial moving control published a successful first move and completed the first enemy side. Its second move was accepted and triggered the authored tornado. The diagnostic driver then stopped because it assumed that a successful move always finishes at the requested endpoint. The repeated control accepts recorded relocation and reads the actual final position. In the repeated run, the first move was presented as result 4 at (4,2). The second was presented as result 9. The accepted path requested (3,4), and the tornado relocated Main to (0,6). Movement changed the candidate destinations. For example, the first side offered a Demon move to (1,4), absent from the wait shortlist. Both enemy sides still reached `EndEnemyPhase` with no unit action. The authored wait, adjacent and repeated moving runs each exited successfully. The attempted moving run remains separate and has exit 1.

## Why all candidates lost to Hold

1. `CombatCommandEffects.AssessmentCoverageSteps` evaluates every live field character. In the trace, entity 3 (Undead Heavy) yields `UnsupportedEffect` for `RegenerationEffectRule`. Entity 5 (Gravekeeper) yields the same gap for `KnockbackEffectRule` and `LeapEffectRule`. The Demon inherits these gaps through the battle-wide coverage scan even when they are unrelated to its proposed target.
2. After public admission, `CombatActionAssessment` treats any such coverage gap as unbounded allied harm. Every living field ally contributes its current HP as a fraction of maximum HP. All five were at full health when scored, so the bound was 5,000 permille. The unsupported gap also skipped precise preview. The traces have `precise=False` and `budget=False`. This decision did not come from preview-budget exhaustion.
3. `EnemyUtilityScorer` weights unresolved allied loss at −3 per permille. That charges −15,000 points to every scored candidate in these cases. Hold uses position risk without this action-level allied damage bound. `EnemyDecisionSession.SelectUtility` retains only ranks strictly greater than Hold when no mandatory action applies. None qualified, so the session selected `EndEnemyPhase` and never handed an attack or move to the actual executor.

In the first authored enemy side, the Demon move to (4,3) had pursuit progress +60 and no endpoint hazard penalty. Its score was −14,940 against Hold −4,000. In the adjacent case, the Demon generated one cell attack targeting Main at (4,2). It passed public admission and recorded an estimated 240 permille opponent HP loss (6 of 25 HP), zero estimated allied loss, a 5,000-permille unresolved bound, and −466 player threat. The final attack score was 240 − 15,000 − 466 = −15,226. Hold was −466. An offline score check with only that bound's penalty omitted gives −226 for the attack. This isolates the decisive term in the recorded score. It does not test the outcome of changing assessment coverage or establish that unsupported effects are safe to ignore.

Earlier A8/X8/H8 candidate-scope work narrowed route and environmental exposure. The separate battle-wide effect coverage branch still sets `unboundedHarm` before route-level exposure is evaluated. The stage 1124 evidence identifies that surviving branch.

## Stage authoring and restoration

The previous item 3 `1124-short-night` and `1124-short-night-terminal` diagnostics added railway and rockfall to copied runtime definitions and temporarily made the first enemy response the last turn of night. The terminal variant put a rail across the Main character to verify the cleanup short circuit. Those are authored-stage derivatives for F7. The matched normal captures and this diagnosis use the original gimmicks. Both stage assets retained their entry hashes, and the authored combat asset has railway and rockfall disabled.

Raw evidence is under `Logs/EnemyAi/phase-7-1124-decision-trace/`, with `decisions.tsv` and `flow.tsv` for each condition. `authored-move` retains the partial failed driver run. The temporary instrumentation and adjacent placement wrapper were removed. The three backed-up source files and both stage assets match their entry hashes, and the 121 pre-existing changed C# hashes still match the previous manifest. Immediate Advance remains the default. No AI policy or stage content change was applied.

